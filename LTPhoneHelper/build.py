"""Build script - tạo LTPhoneHelper.exe bằng PyInstaller."""

import subprocess
import sys


def build():
    cmd = [
        sys.executable, "-m", "PyInstaller",
        "--onefile",
        "--name", "LTPhoneHelper",
        "--console",
        "--icon", "NONE",
        "--clean",
        "--runtime-hook", "runtime_hook.py",
        "main.py",
    ]

    print("Building LTPhoneHelper.exe...")
    subprocess.run(cmd, check=True)
    print("\nBuild done! File: dist/LTPhoneHelper.exe")


if __name__ == "__main__":
    build()
