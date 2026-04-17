"""
Tests cho template_matcher.py
Chạy từ thư mục root: python -m pytest tests/ -v
"""

import base64
import sys
import os

import numpy as np
import cv2
import pytest

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "src"))

from template_matcher import find_template


def _img_to_b64(img: np.ndarray) -> str:
    _, buf = cv2.imencode(".png", img)
    return base64.b64encode(buf.tobytes()).decode("utf-8")


def _make_screenshot(w=540, h=960, color=(200, 200, 200)) -> np.ndarray:
    img = np.full((h, w, 3), color, dtype=np.uint8)
    return img


def _draw_template_at(screenshot: np.ndarray, tx: int, ty: int, tw: int, th: int, color=(50, 100, 200)) -> np.ndarray:
    """Vẽ một hình chữ nhật màu đặc vào screenshot tại vị trí (tx, ty)."""
    result = screenshot.copy()
    result[ty:ty+th, tx:tx+tw] = color
    return result


class TestFindTemplate:
    def test_exact_match(self):
        """Template xuất hiện chính xác trong screenshot -> tìm thấy."""
        screenshot = _make_screenshot()
        tx, ty, tw, th = 200, 300, 80, 60
        screenshot = _draw_template_at(screenshot, tx, ty, tw, th, color=(50, 100, 200))

        # Template là crop chính xác từ screenshot
        template = screenshot[ty:ty+th, tx:tx+tw].copy()

        result = find_template(
            _img_to_b64(screenshot),
            _img_to_b64(template),
            threshold=0.95,
            android_width=1080,
            android_height=1920,
        )

        assert result is not None, "Should find exact match"
        assert result["confidence"] >= 0.95

        # Tọa độ trung tâm trong ảnh gốc (540x960) -> android (1080x1920)
        expected_x = (tx + tw // 2) * 2   # scale x2
        expected_y = (ty + th // 2) * 2   # scale x2
        assert abs(result["x"] - expected_x) <= 4, f"x mismatch: {result['x']} vs {expected_x}"
        assert abs(result["y"] - expected_y) <= 4, f"y mismatch: {result['y']} vs {expected_y}"

    def test_below_threshold_returns_none(self):
        """Không tìm thấy với threshold cao khi ảnh khác nhau."""
        screenshot = _make_screenshot(color=(200, 200, 200))
        template = np.full((40, 40, 3), (10, 10, 10), dtype=np.uint8)  # ảnh tối

        result = find_template(
            _img_to_b64(screenshot),
            _img_to_b64(template),
            threshold=0.95,
        )
        assert result is None

    def test_template_larger_than_screenshot_raises(self):
        """Template lớn hơn screenshot phải raise ValueError."""
        screenshot = _make_screenshot(w=100, h=100)
        template = _make_screenshot(w=200, h=200)

        with pytest.raises(ValueError, match="larger than screenshot"):
            find_template(_img_to_b64(screenshot), _img_to_b64(template))

    def test_invalid_b64_raises(self):
        """Base64 không hợp lệ phải raise ValueError."""
        with pytest.raises(ValueError):
            find_template("not_valid_base64!!!", _img_to_b64(_make_screenshot()))

    def test_coordinate_scaling(self):
        """Kiểm tra scale tọa độ đúng với nhiều tỉ lệ khác nhau."""
        # Screenshot 270x480, android 1080x1920 -> scale 4x
        screenshot = _make_screenshot(w=270, h=480)
        tx, ty, tw, th = 100, 150, 50, 40
        screenshot = _draw_template_at(screenshot, tx, ty, tw, th, color=(0, 200, 100))
        template = screenshot[ty:ty+th, tx:tx+tw].copy()

        result = find_template(
            _img_to_b64(screenshot),
            _img_to_b64(template),
            threshold=0.90,
            android_width=1080,
            android_height=1920,
        )

        assert result is not None
        expected_x = (tx + tw // 2) * 4
        expected_y = (ty + th // 2) * 4
        assert abs(result["x"] - expected_x) <= 8
        assert abs(result["y"] - expected_y) <= 8
