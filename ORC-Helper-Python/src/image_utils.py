"""
Tiện ích xử lý ảnh: decode base64, crop, scale tọa độ.
"""

import base64
from typing import List, Tuple

import cv2
import numpy as np


def b64_to_cv2(b64_str: str) -> np.ndarray:
    """
    Decode base64 string thành ảnh OpenCV (BGR numpy array).
    Raise ValueError nếu decode hoặc parse ảnh thất bại.
    """
    try:
        raw = base64.b64decode(b64_str)
    except Exception as e:
        raise ValueError(f"Base64 decode failed: {e}")

    arr = np.frombuffer(raw, dtype=np.uint8)
    img = cv2.imdecode(arr, cv2.IMREAD_COLOR)
    if img is None:
        raise ValueError("cv2.imdecode returned None — invalid image data")
    return img


def scale_point(
    px: float,
    py: float,
    img_w: int,
    img_h: int,
    android_w: int,
    android_h: int,
) -> Tuple[int, int]:
    """
    Chuyển tọa độ pixel trong ảnh sang tọa độ màn hình Android thực tế.

    Ví dụ: screenshot 540x960, android thực 1080x1920
        scale_x = 1080/540 = 2.0
        pixel (270, 480) -> android (540, 960)
    """
    scale_x = android_w / img_w
    scale_y = android_h / img_h
    return (int(round(px * scale_x)), int(round(py * scale_y)))


def crop_into_parts(
    img: np.ndarray,
    n: int = 5,
    overlap_ratio: float = 0.10,
) -> List[Tuple[np.ndarray, int]]:
    """
    Cắt ảnh thành n phần theo chiều dọc với overlap.

    Trả về list[(cropped_img, y_offset_in_original)].
    y_offset dùng để tính lại tọa độ tuyệt đối sau khi OCR.

    Overlap đảm bảo text nằm ở ranh giới giữa 2 phần không bị bỏ sót.
    """
    h, w = img.shape[:2]
    part_h = h / n
    overlap_px = int(part_h * overlap_ratio)

    parts: List[Tuple[np.ndarray, int]] = []
    for i in range(n):
        y_start = max(0, int(i * part_h) - overlap_px)
        y_end = min(h, int((i + 1) * part_h) + overlap_px)
        cropped = img[y_start:y_end, 0:w]
        parts.append((cropped, y_start))

    return parts


def cv2_to_pil(img: np.ndarray):
    """Chuyển OpenCV BGR -> PIL Image (dùng cho pytesseract)."""
    from PIL import Image
    rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    return Image.fromarray(rgb)
