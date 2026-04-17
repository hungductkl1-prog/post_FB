"""
Template matching port logic từ imageMatcher.ts (view-phone-js-desktop).

Logic giống hệt JS:
  1. Scale SCREEN xuống các tỉ lệ dựa trên độ phân giải smartphone phổ biến (0.2-1.0).
  2. Match template gốc vào screen đã scale → OpenCV TM_CCOEFF_NORMED.
  3. Top-6 screen scales tốt nhất → thử thêm biến thể scale template (0.25-3.5).
  4. Tọa độ map về screen gốc rồi trả về.
"""

import sys
from typing import Optional

import cv2
import numpy as np

from image_utils import b64_to_cv2, scale_point


# ── Scale candidates (port từ JS) ───────────────────────────────────────────

def _screen_scales(sw: int, sh: int) -> list[float]:
    scales: set[float] = {1.0}
    for w in [960,900,860,820,800,760,720,680,640,620,600,580,560,540,520,500,480,460,440,432,420,410,400,390]:
        s = w / max(1, sw)
        if 0.2 <= s <= 1.0:
            scales.add(round(s, 3))
    for h in [1700,1600,1500,1440,1360,1280,1200,1120,1040,1000,960,920,900,860,820,800]:
        s = h / max(1, sh)
        if 0.2 <= s <= 1.0:
            scales.add(round(s, 3))
    return sorted(scales, reverse=True)


def _template_scales(tw: int, th: int, sw: int, sh: int) -> list[float]:
    scales: set[float] = set()
    for s in [0.98,0.95,0.92,0.90,0.88,0.85,0.82,0.80,0.75,0.70,0.65,
              0.60,0.55,0.50,0.45,0.40,0.35,0.25,
              1.02,1.05,1.10,1.15,1.20,1.25,1.30,1.40,1.50,1.70,2.0,2.3,2.6,3.0,3.5]:
        scales.add(round(s, 3))
    max_fit = min(sw / max(1, tw), sh / max(1, th))
    if 0.25 <= max_fit <= 3.5:
        scales.add(round(max_fit, 3))
    # loại scale=1.0 vì bước 1 đã thử rồi, sắp xếp gần 1.0 trước
    return sorted({s for s in scales if abs(s - 1.0) > 0.01}, key=lambda x: abs(x - 1.0))


def _prioritize(scales: list[float], tw: int, th: int) -> list[float]:
    area = tw * th
    anchors = [0.333,0.35,0.3,0.4,0.5,0.667,1.0] if area <= 50_000 else [0.5,0.667,1.0,0.4,0.35,0.333]
    def dist(s): return min(abs(s - a) for a in anchors)
    return sorted(set(scales), key=lambda s: (dist(s), abs(s - 1.0)))


# ── Grayscale (luminosity, giống JS) ────────────────────────────────────────

def _gray(img: np.ndarray) -> np.ndarray:
    b, g, r = img[:,:,0].astype(np.float32), img[:,:,1].astype(np.float32), img[:,:,2].astype(np.float32)
    return (0.299*r + 0.587*g + 0.114*b).astype(np.uint8)


# ── Match một cặp (screen_gray, tpl_gray) bằng OpenCV ──────────────────────

def _match(screen_g: np.ndarray, tpl_g: np.ndarray) -> tuple[float, int, int]:
    """Trả về (score, x, y) góc trên-trái. score trong [0,1]."""
    if tpl_g.shape[0] > screen_g.shape[0] or tpl_g.shape[1] > screen_g.shape[1]:
        return 0.0, 0, 0
    result = cv2.matchTemplate(screen_g, tpl_g, cv2.TM_CCOEFF_NORMED)
    _, max_val, _, max_loc = cv2.minMaxLoc(result)
    # TM_CCOEFF_NORMED ∈ [-1,1] → chuẩn hóa về [0,1] như JS
    score = float(max((max_val + 1.0) / 2.0, 0.0))
    return score, max_loc[0], max_loc[1]


# ── Public API ───────────────────────────────────────────────────────────────

def find_template(
    screenshot_b64: str,
    template_b64: str,
    threshold: float = 0.7,
    android_width: int = 1080,
    android_height: int = 1920,
) -> Optional[dict]:

    screen   = b64_to_cv2(screenshot_b64)
    template = b64_to_cv2(template_b64)

    scr_h, scr_w = screen.shape[:2]
    tpl_h, tpl_w = template.shape[:2]

    print(f"[tm] screen={scr_w}x{scr_h} tpl={tpl_w}x{tpl_h} threshold={threshold}",
          file=sys.stderr, flush=True)

    if tpl_w > scr_w or tpl_h > scr_h:
        raise ValueError(f"Template ({tpl_w}x{tpl_h}) > screenshot ({scr_w}x{scr_h})")

    screen_g   = _gray(screen)
    template_g = _gray(template)

    all_screen_scales = _prioritize(_screen_scales(scr_w, scr_h), tpl_w, tpl_h)[:18]

    best = {"score": 0.0, "x": 0, "y": 0, "tw": tpl_w, "th": tpl_h, "ss": 1.0}
    scored: list[tuple[float, float]] = []  # (screen_scale, score)

    # ── Bước 1: scale screen, template gốc — duyệt HẾT, không early-return ──
    for ss in all_screen_scales:
        if ss < 1.0:
            sw2 = max(1, round(scr_w * ss))
            sh2 = max(1, round(scr_h * ss))
            scr_s = cv2.resize(screen_g, (sw2, sh2), interpolation=cv2.INTER_LINEAR)
        else:
            scr_s = screen_g
            sw2, sh2 = scr_w, scr_h

        if tpl_w > sw2 or tpl_h > sh2:
            continue

        score, lx, ly = _match(scr_s, template_g)
        scored.append((ss, score))

        ox, oy = round(lx / ss), round(ly / ss)
        print(f"[tm] step1 ss={ss:.3f} score={score:.4f} loc=({ox},{oy})", file=sys.stderr, flush=True)

        if score > best["score"]:
            best = {"score": score, "x": ox, "y": oy, "tw": tpl_w, "th": tpl_h, "ss": ss}

    # ── Bước 2: top-6 screen scales + biến thể template scale ───────────────
    top6 = [ss for ss, _ in sorted(scored, key=lambda t: -t[1])[:6]]

    for ss in top6:
        if ss < 1.0:
            sw2 = max(1, round(scr_w * ss))
            sh2 = max(1, round(scr_h * ss))
            scr_s = cv2.resize(screen_g, (sw2, sh2), interpolation=cv2.INTER_LINEAR)
        else:
            scr_s = screen_g
            sw2, sh2 = scr_w, scr_h

        for ts in _template_scales(tpl_w, tpl_h, sw2, sh2)[:10]:
            tw2 = max(1, round(tpl_w * ts))
            th2 = max(1, round(tpl_h * ts))
            if tw2 > sw2 or th2 > sh2:
                continue

            tpl_v = cv2.resize(template_g, (tw2, th2), interpolation=cv2.INTER_LINEAR)
            score, lx, ly = _match(scr_s, tpl_v)

            ox, oy = round(lx / ss), round(ly / ss)
            print(f"[tm] step2 ss={ss:.3f} ts={ts:.3f} score={score:.4f} loc=({ox},{oy})",
                  file=sys.stderr, flush=True)

            if score > best["score"]:
                best = {"score": score, "x": ox, "y": oy, "tw": tw2, "th": th2, "ss": ss}

    # ── Kết quả ──────────────────────────────────────────────────────────────
    print(f"[tm] BEST score={best['score']:.4f} ss={best['ss']:.3f} threshold={threshold}",
          file=sys.stderr, flush=True)

    if best["score"] < threshold:
        return None

    tw_final = best["tw"]
    th_final = best["th"]
    ss_final = best["ss"]
    cx = best["x"] + round(tw_final / (2 * ss_final))
    cy = best["y"] + round(th_final / (2 * ss_final))
    ax, ay = scale_point(cx, cy, scr_w, scr_h, android_width, android_height)
    print(f"[tm] FOUND android=({ax},{ay})", file=sys.stderr, flush=True)
    return {"x": ax, "y": ay, "confidence": round(best["score"], 4)}
