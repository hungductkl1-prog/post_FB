import os
from modules.utils import print_step, print_status, print_info, run_cmd, run_cmd_live, refresh_env_path, GREEN, RESET


def find_appium_cmd():
    """Find appium executable without running it (no hang risk)."""
    # Check npm global prefix
    ok, prefix = run_cmd("npm prefix -g", check=False, timeout=10)
    if ok and prefix:
        prefix = prefix.strip()
        # Windows: npm global installs to prefix/appium.cmd
        candidates = [
            os.path.join(prefix, "appium.cmd"),
            os.path.join(prefix, "appium"),
            os.path.join(prefix, "node_modules", ".bin", "appium.cmd"),
        ]
        for c in candidates:
            if os.path.exists(c):
                return c

    # Fallback: search common npm paths
    appdata = os.environ.get("APPDATA", "")
    if appdata:
        pattern = os.path.join(appdata, "npm", "appium.cmd")
        if os.path.exists(pattern):
            return pattern

    # Last resort: where command (fast, no hang)
    ok, output = run_cmd("where appium", check=False, timeout=5)
    if ok and output:
        return output.splitlines()[0].strip()

    return None


def is_appium_installed():
    """Check if Appium is installed by finding the file (never runs appium)."""
    path = find_appium_cmd()
    if path:
        return True, path
    return False, ""


def install_appium():
    """Install Appium globally via npm."""
    run_cmd_live(
        "npm install -g appium",
        label="Cài đặt Appium",
        timeout=900,
        hint="Appium có nhiều packages, quá trình này mất 3-10 phút...",
    )
    refresh_env_path()
    installed, _ = is_appium_installed()
    if installed:
        print_status("Appium đã cài đặt thành công")
        return True
    print_status("Appium cài đặt thất bại", ok=False)
    return False


def setup_appium():
    """Check and install Appium."""
    print_step("Cài đặt Appium")

    print_info("Kiểm tra Appium...")
    installed, path = is_appium_installed()
    if installed:
        print_status(f"Appium đã có sẵn ({path})")
        print(f"\n  {GREEN}✔ Appium đã sẵn sàng{RESET}")
        return True

    # Check Node.js is available (required for npm)
    ok, _ = run_cmd("node --version", check=False, timeout=10)
    if not ok:
        print_status("Node.js chưa cài đặt - cần Node.js để cài Appium", ok=False)
        return False

    if not install_appium():
        return False

    print(f"\n  {GREEN}✔ Appium đã sẵn sàng{RESET}")
    return True
