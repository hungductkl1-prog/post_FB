# -*- mode: python ; coding: utf-8 -*-
"""
PyInstaller spec file cho OCR-Helper-Python.

Build command:
    pyinstaller build/ocr_helper.spec --clean --distpath dist/

Output: dist/ocr_helper.exe  (onefile mode)
"""

import os

block_cipher = None

project_root = os.path.abspath(os.path.join(SPECPATH, ".."))
src_dir = os.path.join(project_root, "src")
tessdata_dir = os.path.join(project_root, "tessdata")

# Tìm tesseract.exe để bundle vào exe
_tess_candidates = [
    os.path.join(project_root, "Tesseract-OCR", "tesseract.exe"),
    r"C:\Program Files\Tesseract-OCR\tesseract.exe",
    r"C:\Program Files (x86)\Tesseract-OCR\tesseract.exe",
]
_tess_dir = None
for _c in _tess_candidates:
    if os.path.isfile(_c):
        _tess_dir = os.path.dirname(_c)
        break

# Bundle tessdata
_datas = []
if os.path.isdir(tessdata_dir):
    _datas.append((tessdata_dir, "tessdata"))
elif _tess_dir and os.path.isdir(os.path.join(_tess_dir, "tessdata")):
    _datas.append((os.path.join(_tess_dir, "tessdata"), "tessdata"))

# Bundle toàn bộ Tesseract-OCR folder (tesseract.exe + DLLs)
_binaries = []
if _tess_dir:
    for _fname in os.listdir(_tess_dir):
        _fpath = os.path.join(_tess_dir, _fname)
        if os.path.isfile(_fpath) and _fname.lower().endswith(('.exe', '.dll')):
            _binaries.append((_fpath, "Tesseract-OCR"))

a = Analysis(
    [os.path.join(src_dir, "__main__.py")],
    pathex=[src_dir],
    binaries=_binaries,
    datas=_datas,
    hiddenimports=[
        "cv2",
        "numpy",
        "PIL",
        "PIL.Image",
        "pytesseract",
        "difflib",
        "concurrent.futures",
    ],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[
        "tkinter",
        "matplotlib",
        "scipy",
        "pandas",
        "IPython",
        "jupyter",
        "notebook",
        "PyQt5",
        "PyQt6",
        "wx",
    ],
    win_no_prefer_redirects=False,
    win_private_assemblies=False,
    cipher=block_cipher,
    noarchive=False,
)

pyz = PYZ(a.pure, a.zipped_data, cipher=block_cipher)

# onefile: truyền a.binaries, a.zipfiles, a.datas vào EXE, không dùng COLLECT
exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.zipfiles,
    a.datas,
    name="ocr_helper",
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=True,   # PHẢI True: cần stdin/stdout
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
)
