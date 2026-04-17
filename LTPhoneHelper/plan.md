1) Mục tiêu

1 file LTPhoneHelper.exe tự động:

tải LTHelper.zip
giải nén
cài ADB Environment
cài Visual C++ Redistributable (x86 + x64 tùy máy)
cài Node.js
cài Appium
cài .NET 9
verify toàn bộ
2) Thứ tự cài đặt CHUẨN (QUAN TRỌNG)
1. Visual C++
2. LTHelper (ADB)
3. Node.js
4. Appium
5. .NET 9
6. Verify

👉 Lý do:

VC++ là dependency nền → phải cài trước
Node/Appium/.NET có thể cần runtime này
3) Logic kiểm tra Visual C++
✔ Detect kiến trúc máy

Python:

import platform

arch = platform.architecture()[0]

if "64" in arch:
    print("Máy x64")
else:
    print("Máy x86")
4) Strategy cài VC++ (chuẩn nhất)
KHÔNG nên:
chỉ cài theo kiến trúc (sai nhiều case)
NÊN:

👉 LUÔN cài cả x64 + x86

Vì:

nhiều app vẫn cần x86 trên máy x64
Appium / Node / tool cũ hay dùng x86 runtime
5) Cách kiểm tra đã cài chưa

Check registry:

import winreg

def is_vc_installed():
    paths = [
        r"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64",
        r"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86"
    ]

    installed = {"x64": False, "x86": False}

    for path in paths:
        try:
            key = winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, path)
            value, _ = winreg.QueryValueEx(key, "Installed")
            if value == 1:
                if "x64" in path:
                    installed["x64"] = True
                else:
                    installed["x86"] = True
        except:
            pass

    return installed
6) Cài Visual C++ tự động
Dùng winget (đơn giản nhất)
winget install --id Microsoft.VCRedist.2015+.x64 -e --silent
winget install --id Microsoft.VCRedist.2015+.x86 -e --silent
7) Update PLAN FULL
Luồng chạy FINAL:
Xin chào DESKTOP-XXXXX

Đang kiểm tra Visual C++...
→ thiếu x64 → cài x64
→ thiếu x86 → cài x86
Visual C++ đã sẵn sàng

Đang tải LTHelper...
Đã tải xong

Đang giải nén thư viện...
Đã cài đặt LTHelper

Đang cài đặt Environment...
ADB đã được cài đặt

Đang cài đặt Node.js...
Node.js đã được cài đặt

Đang cài đặt Appium...
Appium đã được cài đặt

Đang cài đặt .NET 9...
.NET 9 đã được cài đặt

Đang kiểm tra môi trường...
ADB OK
Node OK
Appium OK
.NET OK

Cài đặt hoàn tất!
8) Update cấu trúc project
modules/
├─ vc_setup.py        👈 NEW
├─ env_setup.py
├─ node_setup.py
├─ appium_setup.py
├─ dotnet_setup.py
9) Logic module vc_setup.py
def setup_vcredist():
    status = is_vc_installed()

    if not status["x64"]:
        install_x64()

    if not status["x86"]:
        install_x86()
10) Best Practice (rất quan trọng)

✔ Luôn cài:

VC++ x64
VC++ x86

✔ Luôn cài trước các thứ khác

✔ Silent install:

/quiet /norestart

✔ Retry nếu fail

11) Tổng kết FINAL

👉 Plan đầy đủ của bạn gồm:

Visual C++ (x64 + x86) ✅
ADB Environment từ LTHelper ✅
Node.js ✅
Appium ✅
.NET 9 ✅

👉 Build bằng:

PyInstaller (nhanh)
Nuitka (pro hơn)

LINK LTHelper.zip: https://www.dropbox.com/scl/fi/gg761sb3yzm3omjl3nzsi/LTHelper.zip?rlkey=zwqm6fduaci5ywbjf10p96s7c&st=urw4pwah&dl=0