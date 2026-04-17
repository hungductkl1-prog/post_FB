"""
Tests cho image_utils.py
"""

import base64
import sys
import os

import numpy as np
import cv2
import pytest

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "src"))

from image_utils import b64_to_cv2, scale_point, crop_into_parts


def _make_b64_img(w=100, h=100) -> str:
    img = np.zeros((h, w, 3), dtype=np.uint8)
    _, buf = cv2.imencode(".png", img)
    return base64.b64encode(buf.tobytes()).decode("utf-8")


class TestB64ToCv2:
    def test_valid_png(self):
        b64 = _make_b64_img(200, 300)
        img = b64_to_cv2(b64)
        assert img.shape == (300, 200, 3)

    def test_invalid_b64(self):
        with pytest.raises(ValueError, match="Base64 decode failed"):
            b64_to_cv2("!!!not_base64!!!")

    def test_invalid_image_data(self):
        # Valid base64 nhưng không phải ảnh
        b64 = base64.b64encode(b"hello world").decode()
        with pytest.raises(ValueError, match="imdecode returned None"):
            b64_to_cv2(b64)


class TestScalePoint:
    def test_same_size(self):
        x, y = scale_point(100, 200, 1080, 1920, 1080, 1920)
        assert x == 100 and y == 200

    def test_scale_up_2x(self):
        # img 540x960, android 1080x1920
        x, y = scale_point(270, 480, 540, 960, 1080, 1920)
        assert x == 540 and y == 960

    def test_scale_down(self):
        # img 2160x3840, android 1080x1920
        x, y = scale_point(2160, 3840, 2160, 3840, 1080, 1920)
        assert x == 1080 and y == 1920

    def test_non_uniform_scale(self):
        # img 600x800 -> android 1200x1600 (2x uniform)
        x, y = scale_point(300, 400, 600, 800, 1200, 1600)
        assert x == 600 and y == 800


class TestCropIntoParts:
    def test_returns_n_parts(self):
        img = np.zeros((1000, 500, 3), dtype=np.uint8)
        parts = crop_into_parts(img, n=5)
        assert len(parts) == 5

    def test_y_offsets_increasing(self):
        img = np.zeros((1000, 500, 3), dtype=np.uint8)
        parts = crop_into_parts(img, n=5, overlap_ratio=0.0)
        offsets = [y for _, y in parts]
        assert offsets == sorted(offsets)
        assert offsets[0] == 0

    def test_parts_cover_full_height(self):
        img = np.zeros((1000, 500, 3), dtype=np.uint8)
        parts = crop_into_parts(img, n=5, overlap_ratio=0.0)
        # Part cuối phải chạm đến y=1000
        last_img, last_offset = parts[-1]
        assert last_offset + last_img.shape[0] == 1000

    def test_overlap_makes_parts_taller(self):
        img = np.zeros((1000, 500, 3), dtype=np.uint8)
        parts_no_overlap = crop_into_parts(img, n=5, overlap_ratio=0.0)
        parts_overlap = crop_into_parts(img, n=5, overlap_ratio=0.1)

        for (p_no, _), (p_ov, _) in zip(parts_no_overlap[1:-1], parts_overlap[1:-1]):
            assert p_ov.shape[0] >= p_no.shape[0]
