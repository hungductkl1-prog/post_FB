"""
Tests cho dispatcher.py — kiểm tra routing và xử lý lỗi.
"""

import sys
import os

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "src"))

from dispatcher import dispatch


class TestDispatchPing:
    def test_ping_returns_pong(self):
        req = {"id": "test-1", "action": "ping"}
        resp = dispatch(req)
        assert resp["success"] is True
        assert resp["result"]["pong"] is True
        assert resp["id"] == "test-1"

    def test_ping_no_id(self):
        resp = dispatch({"action": "ping"})
        assert resp["success"] is True


class TestDispatchUnknownAction:
    def test_unknown_action(self):
        resp = dispatch({"id": "x", "action": "fly_to_moon"})
        assert resp["success"] is False
        assert "Unknown action" in resp["error"]

    def test_empty_action(self):
        resp = dispatch({"id": "x", "action": ""})
        assert resp["success"] is False


class TestDispatchValidation:
    def test_template_match_missing_screenshot(self):
        resp = dispatch({
            "id": "t1",
            "action": "template_match",
            "params": {
                "template_b64": "abc",
                "android_width": 1080,
                "android_height": 1920,
            }
        })
        assert resp["success"] is False
        assert "screenshot_b64" in resp["error"]

    def test_ocr_find_missing_text(self):
        resp = dispatch({
            "id": "o1",
            "action": "ocr_find",
            "params": {
                "screenshot_b64": "abc",
                "android_width": 1080,
                "android_height": 1920,
            }
        })
        assert resp["success"] is False
        assert "search_text" in resp["error"]

    def test_invalid_threshold(self):
        resp = dispatch({
            "id": "t2",
            "action": "template_match",
            "params": {
                "screenshot_b64": "abc",
                "template_b64": "abc",
                "threshold": 1.5,  # invalid
                "android_width": 1080,
                "android_height": 1920,
            }
        })
        assert resp["success"] is False
        assert "threshold" in resp["error"]
