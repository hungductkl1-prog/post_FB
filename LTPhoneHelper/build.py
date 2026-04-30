"""Build script - tạo GolikeHelper.exe bằng PyInstaller."""

import subprocess
import sys


def build():
    cmd = [
        sys.executable, "-m", "PyInstaller",
        "--onefile",
        "--name", "GolikeHelper",
        "--console",
        "--icon", "logo.ico",
        "--uac-admin",
        "--clean",
        "--runtime-hook", "runtime_hook.py",
        "main.py",
    ]

    print("Building GolikeHelper.exe...")
    subprocess.run(cmd, check=True)
    print("\nBuild done! File: dist/GolikeHelper.exe")


if __name__ == "__main__":
    build()
