@echo off
setlocal EnableDelayedExpansion

echo ============================================================
echo  OCR-Helper-Python - Build EXE
echo ============================================================

:: Di chuyển về thư mục root project (build\ nằm trong root)
cd /d "%~dp0.."

:: Kiểm tra Python
python --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Python not found in PATH
    pause & exit /b 1
)

:: Kiểm tra PyInstaller
python -m pyinstaller --version >nul 2>&1
if errorlevel 1 (
    echo [INFO] Installing PyInstaller...
    pip install pyinstaller
)

:: Kiểm tra dependencies
echo [INFO] Installing dependencies...
pip install -r requirements.txt
if errorlevel 1 (
    echo [ERROR] Failed to install dependencies
    pause & exit /b 1
)

:: Xóa build cũ
if exist "dist\ocr_helper" (
    echo [INFO] Cleaning old dist...
    rmdir /s /q "dist\ocr_helper"
)
if exist "build\__pycache__" rmdir /s /q "build\__pycache__"
if exist "src\__pycache__"   rmdir /s /q "src\__pycache__"

:: Build
echo [INFO] Building with PyInstaller...
python -m pyinstaller build\ocr_helper.spec --clean --distpath dist\
if errorlevel 1 (
    echo [ERROR] PyInstaller build failed
    pause & exit /b 1
)

:: Kiểm tra tesseract.exe bên cạnh exe
if exist "Tesseract-OCR\tesseract.exe" (
    echo [INFO] Copying Tesseract-OCR to dist...
    xcopy /e /i /y "Tesseract-OCR" "dist\ocr_helper\Tesseract-OCR"
) else (
    echo [WARN] Tesseract-OCR folder not found next to project root.
    echo [WARN] Make sure tesseract.exe is available at runtime.
)

:: Copy tessdata nếu chưa được bundle
if exist "tessdata" (
    if not exist "dist\ocr_helper\tessdata" (
        echo [INFO] Copying tessdata...
        xcopy /e /i /y "tessdata" "dist\ocr_helper\tessdata"
    )
)

echo.
echo ============================================================
echo  BUILD SUCCESS
echo  Output: dist\ocr_helper.exe
echo ============================================================
echo.

:: Smoke test - warmup ping
echo [INFO] Running smoke test (ping)...
echo {"id":"smoke","action":"ping"} | "dist\ocr_helper.exe"
if errorlevel 1 (
    echo [WARN] Smoke test may have failed. Check output above.
) else (
    echo [INFO] Smoke test passed.
)

pause
