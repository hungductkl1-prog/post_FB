"""Build script - tạo QNHelper.exe bằng PyInstaller."""

import subprocess
import sys


def build():
    cmd = [
        sys.executable, "-m", "PyInstaller",
        "--onefile",
        "--name", "QNHelper",
        "--console",
        "--icon", "logo.ico",
        "--uac-admin",
        "--clean",
        "--runtime-hook", "runtime_hook.py",
        "main.py",
    ]

    print("Building QNHelper.exe...")
    subprocess.run(cmd, check=True)
    print("\nBuild done! File: dist/QNHelper.exe")


if __name__ == "__main__":
    build()
