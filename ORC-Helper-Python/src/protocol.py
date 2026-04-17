"""
Định nghĩa JSON request/response schema cho giao tiếp C# <-> Python exe.

C# ghi một dòng JSON vào stdin, Python xử lý và ghi một dòng JSON ra stdout.
Mỗi dòng kết thúc bằng newline, encoding UTF-8.
"""

from typing import Any, Optional


def make_success(req_id: str, result: dict) -> dict:
    return {
        "id": req_id,
        "success": True,
        "result": result,
        "error": None,
    }


def make_error(req_id: str, message: str) -> dict:
    return {
        "id": req_id,
        "success": False,
        "result": None,
        "error": message,
    }


def make_pong(req_id: str) -> dict:
    return {
        "id": req_id,
        "success": True,
        "result": {"pong": True},
        "error": None,
    }


# ── Validate helpers ──────────────────────────────────────────────────────────

def require_param(params: dict, key: str, req_id: str) -> Any:
    """Raise ValueError nếu key không có trong params."""
    if key not in params or params[key] is None:
        raise ValueError(f"Missing required param: '{key}'")
    return params[key]


def validate_template_match_params(params: dict, req_id: str) -> dict:
    """
    Trả về dict đã validate cho action template_match.
    Raise ValueError nếu thiếu/sai tham số.

    Params:
        screenshot_b64  : str  - Base64 ảnh lớn (PNG/JPG)
        template_b64    : str  - Base64 ảnh nhỏ (template)
        threshold       : float (0.0-1.0), default 0.85
        android_width   : int
        android_height  : int
    """
    screenshot_b64 = require_param(params, "screenshot_b64", req_id)
    template_b64 = require_param(params, "template_b64", req_id)
    android_width = int(require_param(params, "android_width", req_id))
    android_height = int(require_param(params, "android_height", req_id))
    threshold = float(params.get("threshold", 0.85))

    if not (0.0 < threshold <= 1.0):
        raise ValueError(f"threshold must be in (0, 1], got {threshold}")
    if android_width <= 0 or android_height <= 0:
        raise ValueError("android_width and android_height must be positive")

    return {
        "screenshot_b64": screenshot_b64,
        "template_b64": template_b64,
        "threshold": threshold,
        "android_width": android_width,
        "android_height": android_height,
    }


def validate_ocr_find_params(params: dict, req_id: str) -> dict:
    """
    Trả về dict đã validate cho action ocr_find.

    Params:
        screenshot_b64       : str  - Base64 ảnh lớn
        search_text          : str  - Đoạn text cần tìm
        android_width        : int
        android_height       : int
        lang                 : str, default "eng"
        confidence_threshold : float (0-100), default 60
    """
    screenshot_b64 = require_param(params, "screenshot_b64", req_id)
    search_text = require_param(params, "search_text", req_id)
    if not search_text.strip():
        raise ValueError("search_text must not be empty")

    android_width = int(require_param(params, "android_width", req_id))
    android_height = int(require_param(params, "android_height", req_id))
    lang = str(params.get("lang", "eng"))
    confidence_threshold = float(params.get("confidence_threshold", 60))

    if android_width <= 0 or android_height <= 0:
        raise ValueError("android_width and android_height must be positive")
    if not (0 <= confidence_threshold <= 100):
        raise ValueError(f"confidence_threshold must be 0-100, got {confidence_threshold}")

    return {
        "screenshot_b64": screenshot_b64,
        "search_text": search_text,
        "android_width": android_width,
        "android_height": android_height,
        "lang": lang,
        "confidence_threshold": confidence_threshold,
    }
