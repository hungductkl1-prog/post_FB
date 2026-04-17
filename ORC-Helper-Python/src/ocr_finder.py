"""
Tìm vị trí text trong ảnh bằng Tesseract OCR.

Flow:
  1. Scale ảnh xuống ~720px width.
  2. Tạo nhiều preprocessing variants để bắt text trên mọi nền màu.
  3. Chạy song song nhiều (psm, variant) combinations.
  4. Fuzzy match text, tính tọa độ trung tâm, map về screen gốc.
"""

import difflib
import os
import sys
from concurrent.futures import ThreadPoolExecutor, as_completed
from typing import Optional

import cv2
import numpy as np
import pytesseract

from config import (
    OCR_DEFAULT_CONFIDENCE,
    OCR_DEFAULT_LANG,
    OCR_TIMEOUT,
    get_tessdata_dir,
    get_tesseract_path,
)
from image_utils import b64_to_cv2, scale_point


# ── Init Tesseract ───────────────────────────────────────────────────────────

def _init_tesseract():
    tess = get_tesseract_path()
    if tess != "tesseract":
        pytesseract.pytesseract.tesseract_cmd = tess
    tdir = get_tessdata_dir()
    if tdir:
        os.environ["TESSDATA_PREFIX"] = tdir


_init_tesseract()


# ── Preprocessing variants ───────────────────────────────────────────────────

def _make_variants(img: np.ndarray, target_width: int = 1440) -> tuple[dict[str, np.ndarray], float]:
    """
    Trả về dict {variant_name: gray_img} và scale factor.
    Nhiều variants để bắt text trên mọi loại nền.
    """
    h, w = img.shape[:2]
    # Scale giữ nguyên nếu target_width >= w, chỉ scale xuống khi cần
    scale = min(1.0, target_width / max(w, 1))
    if scale < 1.0:
        nw = max(1, round(w * scale))
        nh = max(1, round(h * scale))
        img = cv2.resize(img, (nw, nh), interpolation=cv2.INTER_AREA)

    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

    variants: dict[str, np.ndarray] = {}

    # 1. Grayscale gốc — text tối trên nền sáng
    variants["gray"] = gray

    # 2. Inverted — text sáng trên nền tối (chữ trắng trên nền xanh/tối)
    variants["inv"] = cv2.bitwise_not(gray)

    # 3. CLAHE — tăng contrast cục bộ
    clahe = cv2.createCLAHE(clipLimit=3.0, tileGridSize=(8, 8))
    variants["clahe"] = clahe.apply(gray)

    # 4. Thresh Otsu — binarize tự động
    _, otsu = cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
    variants["otsu"] = otsu

    # 5. Inverted Otsu — text sáng
    variants["otsu_inv"] = cv2.bitwise_not(otsu)

    # 6. Thresh high luminance (>180) — bắt chữ TRẮNG trên nền màu (xanh, tối, v.v.)
    #    Facebook "Log in" button: chữ trắng L=255 trên nền xanh L≈80
    _, thresh_hi = cv2.threshold(gray, 180, 255, cv2.THRESH_BINARY)
    variants["thresh_hi"] = thresh_hi

    # 7. Thresh low luminance (<80) inverted — bắt chữ tối trên nền trung bình
    _, thresh_lo = cv2.threshold(gray, 80, 255, cv2.THRESH_BINARY_INV)
    variants["thresh_lo"] = thresh_lo

    return variants, scale


# ── Fuzzy match ──────────────────────────────────────────────────────────────

def _similarity(a: str, b: str) -> float:
    return difflib.SequenceMatcher(None, a.lower().strip(), b.lower().strip()).ratio()


# ── OCR một ảnh với (psm, variant) ──────────────────────────────────────────

def _ocr_one(
    proc_img: np.ndarray,
    variant: str,
    search_text: str,
    lang: str,
    psm: int,
    conf_threshold: float,
) -> Optional[dict]:
    try:
        from PIL import Image
        pil = Image.fromarray(proc_img)
        data = pytesseract.image_to_data(
            pil,
            lang=lang,
            output_type=pytesseract.Output.DICT,
            config=f"--psm {psm} --oem 1",
        )
    except Exception as e:
        print(f"[ocr] {variant}/psm={psm} error: {e}", file=sys.stderr, flush=True)
        return None

    n = len(data["text"])
    words   = [str(data["text"][i]).strip()   for i in range(n)]
    confs   = [float(data["conf"][i])         for i in range(n)]
    lefts   = [int(data["left"][i])           for i in range(n)]
    tops    = [int(data["top"][i])            for i in range(n)]
    widths  = [int(data["width"][i])          for i in range(n)]
    heights = [int(data["height"][i])         for i in range(n)]

    # Debug log
    detected = [w for w in words if w]
    print(f"[ocr] {variant}/psm={psm} words={detected}", file=sys.stderr, flush=True)

    search_words = search_text.strip().split()
    sw_len = len(search_words)

    best_score = -1.0
    best_box   = None

    for i in range(n - sw_len + 1):
        ww = words[i: i + sw_len]
        cc = confs[i: i + sw_len]

        if any(c < 0 for c in cc):
            continue

        window_text = " ".join(ww)
        sim = _similarity(window_text, search_text)
        if sim < 0.45:
            continue

        avg_conf = sum(cc) / len(cc)
        if avg_conf < conf_threshold:
            continue

        score = sim * (avg_conf / 100.0)
        if score > best_score:
            best_score = score
            x1 = min(lefts[i + j]                for j in range(sw_len))
            y1 = min(tops[i + j]                 for j in range(sw_len))
            x2 = max(lefts[i + j] + widths[i + j]   for j in range(sw_len))
            y2 = max(tops[i + j] + heights[i + j]   for j in range(sw_len))
            best_box = (x1, y1, x2 - x1, y2 - y1)

    if best_box is None:
        return None

    bx, by, bw, bh = best_box
    return {
        "img_x":   bx + bw // 2,
        "img_y":   by + bh // 2,
        "score":   best_score,
        "variant": variant,
        "psm":     psm,
    }


# ── Public API ───────────────────────────────────────────────────────────────

# (psm, variant_key) — dùng psm 6 và 11 với mọi variant
_PSM_LIST = [6, 11, 3]


def find_text(
    screenshot_b64: str,
    search_text: str,
    android_width:  int   = 1080,
    android_height: int   = 1920,
    lang:           str   = OCR_DEFAULT_LANG,
    confidence_threshold: float = OCR_DEFAULT_CONFIDENCE,
) -> Optional[dict]:

    img = b64_to_cv2(screenshot_b64)
    img_h, img_w = img.shape[:2]

    print(
        f"[ocr] screen={img_w}x{img_h} search='{search_text}' lang={lang}",
        file=sys.stderr, flush=True,
    )

    # Giữ nguyên resolution gốc (1440px) — không scale xuống
    # Tesseract cần ít nhất 30-40px chiều cao chữ để đọc tốt
    variants, scale = _make_variants(img, target_width=img_w)
    print(f"[ocr] proc scale={scale:.3f} variants={list(variants.keys())}", file=sys.stderr, flush=True)

    # DEBUG: save variants ra file để kiểm tra
    import tempfile, os as _os
    _dbg = _os.path.join(tempfile.gettempdir(), "ocr_debug")
    _os.makedirs(_dbg, exist_ok=True)
    cv2.imwrite(_os.path.join(_dbg, "orig.png"), img)
    for _vn, _vi in variants.items():
        cv2.imwrite(_os.path.join(_dbg, f"variant_{_vn}.png"), _vi)
    print(f"[ocr] DEBUG images saved to {_dbg}", file=sys.stderr, flush=True)

    best: Optional[dict] = None

    # Tạo tất cả tasks: mỗi variant × mỗi psm
    tasks = []
    for v_name, v_img in variants.items():
        for psm in _PSM_LIST:
            tasks.append((v_img, v_name, psm))

    with ThreadPoolExecutor(max_workers=min(len(tasks), 10)) as executor:
        futures = {
            executor.submit(
                _ocr_one, v_img, v_name,
                search_text, lang, psm, confidence_threshold,
            ): (v_name, psm)
            for v_img, v_name, psm in tasks
        }

        try:
            for future in as_completed(futures, timeout=OCR_TIMEOUT):
                v_name, psm = futures[future]
                result = future.result()
                if result is None:
                    continue

                print(
                    f"[ocr] HIT {v_name}/psm={psm} score={result['score']:.3f} "
                    f"loc=({result['img_x']},{result['img_y']})",
                    file=sys.stderr, flush=True,
                )

                if best is None or result["score"] > best["score"]:
                    best = result

                if best["score"] >= 0.85:
                    for f in futures:
                        f.cancel()
                    break
        except TimeoutError:
            pass

    if best is None:
        print("[ocr] not found", file=sys.stderr, flush=True)
        return None

    orig_x = round(best["img_x"] / scale)
    orig_y = round(best["img_y"] / scale)
    android_x, android_y = scale_point(orig_x, orig_y, img_w, img_h, android_width, android_height)

    print(
        f"[ocr] FOUND score={best['score']:.3f} {best['variant']}/psm={best['psm']} "
        f"orig=({orig_x},{orig_y}) android=({android_x},{android_y})",
        file=sys.stderr, flush=True,
    )

    return {
        "x":             android_x,
        "y":             android_y,
        "confidence":    round(best["score"], 3),
        "found_in_part": best["psm"],
    }
