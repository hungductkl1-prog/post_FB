import subprocess
import sys
import os
import platform
import socket
import ctypes
import threading
import time
import urllib.request

# ANSI color codes
GREEN = "\033[92m"
RED = "\033[91m"
YELLOW = "\033[93m"
CYAN = "\033[96m"
WHITE = "\033[97m"
DIM = "\033[2m"
RESET = "\033[0m"


def enable_ansi_colors():
    """Enable ANSI color support on Windows CMD."""
    try:
        kernel32 = ctypes.windll.kernel32
        # STD_OUTPUT_HANDLE = -11
        handle = kernel32.GetStdHandle(-11)
        # ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004
        mode = ctypes.c_ulong()
        kernel32.GetConsoleMode(handle, ctypes.byref(mode))
        kernel32.SetConsoleMode(handle, mode.value | 0x0004)
    except Exception:
        pass


# Enable on import
enable_ansi_colors()


def get_computer_name():
    return socket.gethostname()


def get_arch():
    return "x64" if "64" in platform.architecture()[0] else "x86"


def print_step(msg):
    print(f"\n{CYAN}{'='*50}")
    print(f"  {msg}")
    print(f"{'='*50}{RESET}")


def print_status(msg, ok=True):
    if ok:
        print(f"  {GREEN}[OK] {msg}{RESET}")
    else:
        print(f"  {RED}[FAIL] {msg}{RESET}")


def print_info(msg):
    print(f"  {WHITE}{msg}{RESET}")


def print_warn(msg):
    print(f"  {YELLOW}{msg}{RESET}")


def run_cmd(cmd, shell=True, check=True, capture=True, timeout=600):
    """Run a command and return (success, stdout)."""
    try:
        result = subprocess.run(
            cmd,
            shell=shell,
            check=check,
            capture_output=capture,
            text=True,
            timeout=timeout,
        )
        return True, result.stdout.strip() if result.stdout else ""
    except subprocess.CalledProcessError as e:
        return False, e.stderr.strip() if e.stderr else str(e)
    except subprocess.TimeoutExpired:
        return False, "Timeout"
    except Exception as e:
        return False, str(e)


def run_cmd_visible(cmd, shell=True, timeout=600):
    """Run a command with output visible to user."""
    try:
        result = subprocess.run(
            cmd,
            shell=shell,
            timeout=timeout,
        )
        return result.returncode == 0
    except Exception:
        return False


def run_cmd_live(cmd, label="Đang cài đặt", shell=True, timeout=600, hint=""):
    """Run command via subprocess.run (blocking, output to console) + timer thread."""
    if hint:
        print(f"  {YELLOW}{hint}{RESET}")

    print(f"  {CYAN}▶{RESET} {WHITE}{label}...{RESET}")
    sys.stdout.flush()

    stop_event = threading.Event()
    start_time = time.time()
    spinner_chars = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"]

    def timer():
        i = 0
        while not stop_event.is_set():
            stop_event.wait(5)
            if stop_event.is_set():
                break
            elapsed = int(time.time() - start_time)
            mins, secs = divmod(elapsed, 60)
            frame = spinner_chars[i % len(spinner_chars)]
            print(f"  {CYAN}{frame}{RESET} {DIM}Đang xử lý... {mins:02d}:{secs:02d}{RESET}")
            sys.stdout.flush()
            i += 1

    timer_thread = threading.Thread(target=timer, daemon=True)
    timer_thread.start()

    try:
        result = subprocess.run(cmd, shell=True, timeout=timeout)
        stop_event.set()
        timer_thread.join(timeout=2)

        elapsed = int(time.time() - start_time)
        mins, secs = divmod(elapsed, 60)
        print(f"  {DIM}Hoàn tất trong {mins:02d}:{secs:02d}{RESET}")

        return result.returncode == 0, ""

    except subprocess.TimeoutExpired:
        stop_event.set()
        timer_thread.join(timeout=2)
        print(f"  {RED}Timeout sau {timeout}s{RESET}")
        return False, "Timeout"
    except Exception as e:
        stop_event.set()
        timer_thread.join(timeout=2)
        return False, str(e)


def download_with_progress(url, dest, label="Đang tải"):
    """Download a file with progress bar."""
    try:
        import ssl
        # Try with default SSL first, fallback to unverified if cert fails
        ctx = ssl._create_unverified_context()

        req = urllib.request.Request(url, headers={"User-Agent": "LTPhoneHelper/1.0"})
        resp = urllib.request.urlopen(req, timeout=300, context=ctx)
        total = int(resp.headers.get("Content-Length", 0))
        downloaded = 0
        block_size = 8192
        bar_width = 30

        with open(dest, "wb") as f:
            while True:
                chunk = resp.read(block_size)
                if not chunk:
                    break
                f.write(chunk)
                downloaded += len(chunk)

                if total > 0:
                    pct = downloaded / total
                    filled = int(bar_width * pct)
                    bar = f"{'█' * filled}{'░' * (bar_width - filled)}"
                    mb_done = downloaded / (1024 * 1024)
                    mb_total = total / (1024 * 1024)
                    sys.stdout.write(
                        f"\r  {CYAN}{label}{RESET} {bar} {WHITE}{pct*100:.0f}%{RESET}"
                        f" {DIM}({mb_done:.1f}/{mb_total:.1f} MB){RESET}  "
                    )
                else:
                    mb_done = downloaded / (1024 * 1024)
                    sys.stdout.write(
                        f"\r  {CYAN}{label}{RESET} {WHITE}{mb_done:.1f} MB{RESET} tải...  "
                    )
                sys.stdout.flush()

        sys.stdout.write(f"\r{' ' * 80}\r")
        sys.stdout.flush()
        return True
    except Exception as e:
        sys.stdout.write(f"\r{' ' * 80}\r")
        sys.stdout.flush()
        raise e


def is_command_available(cmd):
    """Check if a command is available in PATH."""
    ok, output = run_cmd(f"{cmd} --version", check=False)
    if ok and output:
        return True
    ok, output = run_cmd(f"where {cmd}", check=False)
    return ok and output != ""


def refresh_env_path():
    """Refresh PATH from registry so newly installed tools are found."""
    try:
        import winreg
        env_keys = [
            (winreg.HKEY_LOCAL_MACHINE, r"SYSTEM\CurrentControlSet\Control\Session Manager\Environment"),
            (winreg.HKEY_CURRENT_USER, r"Environment"),
        ]
        new_path_parts = []
        for root, subkey in env_keys:
            try:
                key = winreg.OpenKey(root, subkey)
                value, _ = winreg.QueryValueEx(key, "Path")
                winreg.CloseKey(key)
                new_path_parts.append(value)
            except OSError:
                pass
        if new_path_parts:
            os.environ["PATH"] = ";".join(new_path_parts)
    except Exception:
        pass
