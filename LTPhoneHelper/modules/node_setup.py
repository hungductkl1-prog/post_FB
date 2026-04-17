import os
import zipfile
import shutil
import winreg
from modules.utils import print_step, print_status, print_info, run_cmd, run_cmd_live, refresh_env_path, download_with_progress, GREEN, YELLOW, RESET

# Node.js LTS download (x64)
NODE_URL = "https://nodejs.org/dist/v20.18.1/node-v20.18.1-win-x64.zip"
NODE_DIR = r"C:\LTHelper\nodejs"


def is_node_installed():
    """Check if Node.js is installed and available."""
    ok, output = run_cmd("node --version", check=False)
    if ok and output.startswith("v"):
        return True, output
    return False, ""


def add_to_system_path(new_path):
    """Add a directory to the system PATH permanently."""
    try:
        key = winreg.OpenKey(
            winreg.HKEY_LOCAL_MACHINE,
            r"SYSTEM\CurrentControlSet\Control\Session Manager\Environment",
            0,
            winreg.KEY_ALL_ACCESS,
        )
        current_path, _ = winreg.QueryValueEx(key, "Path")
        if new_path.lower() in current_path.lower():
            winreg.CloseKey(key)
            return True
        new_value = current_path.rstrip(";") + ";" + new_path
        winreg.SetValueEx(key, "Path", 0, winreg.REG_EXPAND_SZ, new_value)
        winreg.CloseKey(key)
        os.environ["PATH"] = new_value
        return True
    except Exception as e:
        print_status(f"Loi them PATH: {e}", ok=False)
        return False


def install_node():
    """Download and install Node.js from official zip."""
    print_info("Dang tai Node.js LTS...")

    temp_zip = os.path.join(os.environ.get("TEMP", "."), "nodejs.zip")

    try:
        download_with_progress(NODE_URL, temp_zip, label="Node.js")
        print_status("Da tai xong Node.js")
    except Exception as e:
        print_status(f"Tai Node.js that bai: {e}", ok=False)
        # Fallback: try winget
        return install_node_winget()

    # Extract
    print_info("Dang giai nen Node.js...")
    try:
        if os.path.exists(NODE_DIR):
            shutil.rmtree(NODE_DIR)

        temp_extract = os.path.join(os.environ.get("TEMP", "."), "nodejs_extract")
        if os.path.exists(temp_extract):
            shutil.rmtree(temp_extract)

        with zipfile.ZipFile(temp_zip, "r") as zf:
            zf.extractall(temp_extract)

        # Find the node root folder (e.g., node-v20.18.1-win-x64)
        extracted_items = os.listdir(temp_extract)
        node_root = None
        for item in extracted_items:
            item_path = os.path.join(temp_extract, item)
            if os.path.isdir(item_path) and item.startswith("node-"):
                node_root = item_path
                break

        if node_root:
            shutil.move(node_root, NODE_DIR)
        else:
            shutil.move(temp_extract, NODE_DIR)

        if os.path.exists(temp_extract):
            shutil.rmtree(temp_extract)

        print_status("Da giai nen Node.js")
    except Exception as e:
        print_status(f"Giai nen Node.js that bai: {e}", ok=False)
        return False
    finally:
        try:
            os.remove(temp_zip)
        except OSError:
            pass

    # Add to PATH
    if os.path.exists(NODE_DIR):
        add_to_system_path(NODE_DIR)
        # Also update current process PATH
        os.environ["PATH"] = NODE_DIR + ";" + os.environ.get("PATH", "")
        print_status(f"PATH += {NODE_DIR}")

    # Verify
    refresh_env_path()
    ok, output = run_cmd(f'"{os.path.join(NODE_DIR, "node.exe")}" --version', check=False)
    if ok and output.startswith("v"):
        print_status(f"Node.js {output} da san sang")
        return True

    print_status("Node.js cai dat that bai", ok=False)
    return False


def install_node_winget():
    """Fallback: Install Node.js via winget."""
    print_info("Thu cai bang winget...")
    run_cmd_live(
        'winget install --id OpenJS.NodeJS.LTS -e --silent --accept-package-agreements --accept-source-agreements',
        label="Cai dat Node.js (winget)",
        timeout=600,
    )
    refresh_env_path()
    installed, _ = is_node_installed()
    if installed:
        print_status("Node.js da san sang (winget)")
        return True
    print_status("Node.js cai dat that bai", ok=False)
    return False


def setup_node():
    """Check and install Node.js."""
    print_step("Cai dat Node.js")

    installed, version = is_node_installed()
    if installed:
        print_status(f"Node.js da co san ({version})")
        return True

    if not install_node():
        return False

    # Verify after install
    refresh_env_path()
    installed, version = is_node_installed()
    if installed:
        print(f"\n  {GREEN}[OK] Node.js {version} da san sang{RESET}")
        return True

    print_status("Node.js cai dat nhung khong tim thay trong PATH", ok=False)
    return False
