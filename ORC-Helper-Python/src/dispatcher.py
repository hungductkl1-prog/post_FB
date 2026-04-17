"""
Routing: đọc field 'action' trong request, gọi handler tương ứng.
"""

import traceback

from protocol import (
    make_error,
    make_pong,
    make_success,
    validate_ocr_find_params,
    validate_template_match_params,
)


def dispatch(request: dict) -> dict:
    """
    Nhận request dict, trả về response dict.
    Không bao giờ raise exception — mọi lỗi được bắt và trả về make_error().
    """
    req_id = str(request.get("id", ""))
    action = str(request.get("action", "")).strip()
    params = request.get("params", {}) or {}

    try:
        if action == "ping":
            return make_pong(req_id)

        elif action == "template_match":
            validated = validate_template_match_params(params, req_id)
            from template_matcher import find_template
            result = find_template(**validated)
            if result is None:
                return make_error(
                    req_id,
                    f"No match found above threshold {validated['threshold']}",
                )
            return make_success(req_id, result)

        elif action == "ocr_find":
            validated = validate_ocr_find_params(params, req_id)
            from ocr_finder import find_text
            result = find_text(**validated)
            if result is None:
                return make_error(
                    req_id,
                    f"Text '{validated['search_text']}' not found in any region",
                )
            return make_success(req_id, result)

        else:
            return make_error(req_id, f"Unknown action: '{action}'")

    except ValueError as e:
        return make_error(req_id, str(e))
    except Exception:
        return make_error(req_id, traceback.format_exc())
