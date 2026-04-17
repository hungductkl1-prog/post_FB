"""
LTPhoneHelper - Tự động cài đặt môi trường cho LamToolAutoPhonePrime

Thứ tự cài đặt:
  1. Visual C++ Redistributable (x64 + x86)
  2. LTHelper (ADB)
  3. Node.js
  4. Verify toàn bộ
"""

import sys
import os
import ctypes
import subprocess

# Force UTF-8 console output
os.environ["PYTHONUNBUFFERED"] = "1"
os.environ["PYTHONIOENCODING"] = "utf-8"
subprocess.run("chcp 65001", shell=True, capture_output=True)

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace", line_buffering=True)
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8", errors="replace", line_buffering=True)

from modules.utils import get_computer_name, print_step, GREEN, RED, YELLOW, CYAN, RESET
from modules.vc_setup import setup_vcredist
from modules.env_setup import setup_env
from modules.node_setup import setup_node
from modules.verify import verify_all


def is_admin():
    """Check if running with administrator privileges."""
    try:
        return ctypes.windll.shell32.IsUserAnAdmin() != 0
    except Exception:
        return False


def request_admin():
    """Re-launch script as administrator. Copies to local temp if on network/shared drive."""
    if is_admin():
        return False

    print("  Yeu cau quyen Administrator...")
    try:
        exe_path = os.path.abspath(sys.argv[0])
        args = " ".join(sys.argv[1:])

        # If running from network/shared drive, copy to local temp first
        drive = os.path.splitdrive(exe_path)[0].upper()
        is_network = exe_path.startswith("\\\\") or (drive and drive not in ("C:", "D:", "E:"))
        if is_network:
            import shutil
            local_copy = os.path.join(os.environ.get("TEMP", "C:\\Temp"), os.path.basename(exe_path))
            print(f"  Copy to local: {local_copy}")
            shutil.copy2(exe_path, local_copy)
            exe_path = local_copy

        ret = ctypes.windll.shell32.ShellExecuteW(
            None, "runas", exe_path, args, None, 1
        )
        # ShellExecuteW returns >32 on success
        if ret > 32:
            sys.exit(0)
        else:
            print(f"  [WARN] Khong the yeu cau quyen Admin (code={ret})")
            print("  Tiep tuc chay khong co quyen Admin...")
            return False
    except Exception as e:
        print(f"  [WARN] Loi yeu cau Admin: {e}")
        print("  Tiep tuc chay khong co quyen Admin...")
        return False


def main():
    computer_name = get_computer_name()

    print(f"\n{'#'*50}")
    print(f"  Xin chao {computer_name}")
    print(f"  LTPhoneHelper - Cai dat moi truong tu dong")
    print(f"{'#'*50}")

    # Check admin privileges
    if not is_admin():
        print(f"\n  {YELLOW}[!] Can quyen Administrator de cai dat.")
        print(f"  Dang yeu cau quyen Admin...{RESET}")
        request_admin()
        # If we get here, admin request failed - continue anyway

    print_step("Bắt đầu cài đặt")

    steps = [
        ("Visual C++", setup_vcredist),
        ("LTHelper (ADB)", setup_env),
        ("Node.js", setup_node),
    ]
    total = len(steps)

    results = {}
    for i, (name, setup_fn) in enumerate(steps, 1):
        print(f"\n  {CYAN}[{i}/{total}]{RESET} {name}")
        try:
            ok = setup_fn()
            results[name] = ok
            if ok:
                print(f"  {GREEN}>>> {name} - THÀNH CÔNG{RESET}")
            else:
                print(f"  {RED}>>> {name} - THẤT BẠI{RESET}")
                print(f"  {YELLOW}    Tiếp tục cài các thành phần khác...{RESET}")
        except Exception as e:
            results[name] = False
            print(f"  {RED}>>> {name} - LỖI: {e}{RESET}")
            print(f"  {YELLOW}    Tiếp tục cài các thành phần khác...{RESET}")

    # 6. Verify
    verify_all()

    # Summary
    passed = [k for k, v in results.items() if v]
    failed = [k for k, v in results.items() if not v]

    print_step("Tổng kết")
    print(f"  {CYAN}{'─'*40}{RESET}")
    for name, ok in results.items():
        if ok:
            print(f"  {GREEN}  ✔ {name}{RESET}")
        else:
            print(f"  {RED}  ✘ {name}{RESET}")
    print(f"  {CYAN}{'─'*40}{RESET}")
    print(f"  Kết quả: {GREEN}{len(passed)}{RESET} thành công / {RED}{len(failed)}{RESET} thất bại")

    if not failed:
        print(f"\n  {GREEN}{'='*40}")
        print(f"    CÀI ĐẶT HOÀN TẤT THÀNH CÔNG!")
        print(f"  {'='*40}{RESET}")
    else:
        print(f"\n  {YELLOW}⚠ Vui lòng kiểm tra và cài lại các thành phần lỗi.{RESET}")

    print(f"\n{CYAN}{'#'*50}{RESET}")
    input("\n  Nhấn Enter để thoát...")


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print(f"\n  [CRASH] {e}")
        import traceback
        traceback.print_exc()
        input("\n  Nhan Enter de thoat...")
