import os
import sys

# Tesseract binary path
def _app_base() -> str:
    """Thư mục chứa exe (frozen) hoặc thư mục project root (dev)."""
    if getattr(sys, "frozen", False):
        return os.path.dirname(sys.executable)
    return os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def _meipass() -> str:
    """_MEIPASS khi chạy onefile PyInstaller, còn lại trả về app_base."""
    return getattr(sys, "_MEIPASS", _app_base())


def get_tesseract_path() -> str:
    """Tìm tesseract.exe: ưu tiên bundle trong exe, rồi cài hệ thống."""
    mp = _meipass()
    base = _app_base()
    candidates = [
        # bundled trong _MEIPASS (onefile PyInstaller)
        os.path.join(mp, "Tesseract-OCR", "tesseract.exe"),
        # cạnh exe
        os.path.join(base, "tesseract.exe"),
        os.path.join(base, "Tesseract-OCR", "tesseract.exe"),
        # cài hệ thống
        r"C:\Program Files\Tesseract-OCR\tesseract.exe",
        r"C:\Program Files (x86)\Tesseract-OCR\tesseract.exe",
    ]
    for p in candidates:
        if os.path.isfile(p):
            return p
    return "tesseract"  # fallback: dùng PATH


def get_tessdata_dir() -> str:
    """Trả về thư mục tessdata."""
    mp = _meipass()
    base = _app_base()
    candidates = [
        # bundled trong _MEIPASS
        os.path.join(mp, "tessdata"),
        os.path.join(mp, "Tesseract-OCR", "tessdata"),
        # cạnh exe
        os.path.join(base, "tessdata"),
        os.path.join(base, "Tesseract-OCR", "tessdata"),
        # hệ thống
        r"C:\Program Files\Tesseract-OCR\tessdata",
    ]
    for p in candidates:
        if os.path.isdir(p):
            return p
    return ""


# Template matching
TEMPLATE_MATCH_SCALES = [0.8, 0.9, 1.0, 1.1, 1.2]  # multi-scale levels
DEFAULT_THRESHOLD = 0.85

# OCR
OCR_NUM_PARTS = 5          # Số phần cắt ảnh song song
OCR_OVERLAP_RATIO = 0.10   # Overlap 10% giữa các phần
OCR_DEFAULT_LANG = "eng"
OCR_TIMEOUT = 30.0         # seconds, timeout mỗi task OCR
OCR_DEFAULT_CONFIDENCE = 60  # confidence threshold mặc định (0-100)
