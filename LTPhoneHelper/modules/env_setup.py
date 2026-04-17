import os
import zipfile
import shutil
import winreg
from modules.utils import print_step, print_status, print_info, print_warn, run_cmd, download_with_progress, GREEN, YELLOW, RESET

# LTHelper base directory
LTHELPER_DIR = r"C:\LTHelper"

# Download URLs
PLATFORM_TOOLS_URL = "https://dl.google.com/android/repository/platform-tools-latest-windows.zip"
JDK17_URL = "https://api.adoptium.net/v3/binary/latest/17/ga/windows/x64/jdk/hotspot/normal/eclipse?project=jdk"

# Paths after extraction
SDK_DIR = os.path.join(LTHELPER_DIR, "sdk")
PLATFORM_TOOLS_DIR = os.path.join(SDK_DIR, "platform-tools")
JAVA_DIR = os.path.join(LTHELPER_DIR, "java")
JAVA_BIN_DIR = os.path.join(JAVA_DIR, "bin")

# Environment variable values
ANDROID_HOME = SDK_DIR
JAVA_HOME = JAVA_DIR


def kill_adb():
    """Kill ADB server to release file locks."""
    run_cmd("taskkill /F /IM adb.exe", check=False)
    run_cmd("adb kill-server", check=False)


def download_and_extract_platform_tools():
    """Download Google platform-tools and extract to C:\\LTHelper\\sdk\\platform-tools."""
    print_info("Đang tải Android SDK platform-tools...")

    os.makedirs(SDK_DIR, exist_ok=True)
    temp_zip = os.path.join(os.environ.get("TEMP", "."), "platform-tools.zip")

    try:
        download_with_progress(PLATFORM_TOOLS_URL, temp_zip, label="platform-tools")
        print_status("Đã tải xong platform-tools")
    except Exception as e:
        print_status(f"Tải platform-tools thất bại: {e}", ok=False)
        return False

    # Extract
    print_info("Đang giải nén platform-tools...")
    try:
        if os.path.exists(PLATFORM_TOOLS_DIR):
            kill_adb()
            import time
            time.sleep(1)
            shutil.rmtree(PLATFORM_TOOLS_DIR)

        with zipfile.ZipFile(temp_zip, "r") as zf:
            zf.extractall(SDK_DIR)
        print_status("Đã giải nén platform-tools")
    except Exception as e:
        print_status(f"Giải nén platform-tools thất bại: {e}", ok=False)
        return False
    finally:
        try:
            os.remove(temp_zip)
        except OSError:
            pass

    # Verify adb.exe exists
    adb_exe = os.path.join(PLATFORM_TOOLS_DIR, "adb.exe")
    if not os.path.exists(adb_exe):
        print_status("Không tìm thấy adb.exe sau khi giải nén", ok=False)
        return False

    print_status("platform-tools đã sẵn sàng")
    return True


def download_and_extract_jdk17():
    """Download Adoptium JDK 17 and extract to C:\\LTHelper\\java."""
    print_info("Đang tải JDK 17 (Eclipse Temurin)...")

    temp_zip = os.path.join(os.environ.get("TEMP", "."), "jdk17.zip")

    try:
        download_with_progress(JDK17_URL, temp_zip, label="JDK 17")
        print_status("Đã tải xong JDK 17")
    except Exception as e:
        print_status(f"Tải JDK 17 thất bại: {e}", ok=False)
        return False

    # Extract
    print_info("Đang giải nén JDK 17...")
    try:
        if os.path.exists(JAVA_DIR):
            shutil.rmtree(JAVA_DIR)

        # JDK zip extracts to a subfolder like "jdk-17.0.x+y"
        # We need to move its contents to C:\LTHelper\java
        temp_extract = os.path.join(os.environ.get("TEMP", "."), "jdk17_extract")
        if os.path.exists(temp_extract):
            shutil.rmtree(temp_extract)

        with zipfile.ZipFile(temp_zip, "r") as zf:
            zf.extractall(temp_extract)

        # Find the JDK root folder (e.g., jdk-17.0.11+9)
        extracted_items = os.listdir(temp_extract)
        jdk_root = None
        for item in extracted_items:
            item_path = os.path.join(temp_extract, item)
            if os.path.isdir(item_path) and item.startswith("jdk-"):
                jdk_root = item_path
                break

        if jdk_root:
            shutil.move(jdk_root, JAVA_DIR)
        else:
            # If no jdk- folder found, move everything directly
            shutil.move(temp_extract, JAVA_DIR)

        # Cleanup temp extract
        if os.path.exists(temp_extract):
            shutil.rmtree(temp_extract)

        print_status("Đã giải nén JDK 17")
    except Exception as e:
        print_status(f"Giải nén JDK 17 thất bại: {e}", ok=False)
        return False
    finally:
        try:
            os.remove(temp_zip)
        except OSError:
            pass

    # Verify java.exe exists
    java_exe = os.path.join(JAVA_BIN_DIR, "java.exe")
    if not os.path.exists(java_exe):
        print_status("Không tìm thấy java.exe sau khi giải nén", ok=False)
        return False

    print_status("JDK 17 đã sẵn sàng")
    return True


def cleanup_old_adb_paths():
    """Remove old/stale ADB and Java paths from system PATH."""
    print_info("Đang dọn dẹp ADB/Java paths cũ...")
    try:
        key = winreg.OpenKey(
            winreg.HKEY_LOCAL_MACHINE,
            r"SYSTEM\CurrentControlSet\Control\Session Manager\Environment",
            0,
            winreg.KEY_ALL_ACCESS,
        )
        current_path, _ = winreg.QueryValueEx(key, "Path")
        parts = current_path.split(";")

        # Keywords to detect old ADB/Java-related paths
        adb_keywords = ["platform-tools", "adb", "android-sdk", "lthelper", "tlchelper"]
        cleaned = []
        removed = []

        for p in parts:
            p_stripped = p.strip()
            if not p_stripped:
                continue
            p_lower = p_stripped.lower()
            is_old_path = any(kw in p_lower for kw in adb_keywords)
            if is_old_path:
                removed.append(p_stripped)
            else:
                cleaned.append(p_stripped)

        if removed:
            for r in removed:
                print(f"  {YELLOW}  Loại bỏ: {r}{RESET}")
            new_value = ";".join(cleaned)
            winreg.SetValueEx(key, "Path", 0, winreg.REG_EXPAND_SZ, new_value)
            os.environ["PATH"] = new_value
            print_status(f"Đã dọn dẹp {len(removed)} path cũ")
        else:
            print_status("Không có path cũ cần dọn")

        winreg.CloseKey(key)
        return True
    except PermissionError:
        print_status("Cần quyền Admin để dọn PATH", ok=False)
        return False
    except Exception as e:
        print_status(f"Lỗi dọn PATH: {e}", ok=False)
        return False


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
    except PermissionError:
        print_status("Cần quyền Admin để thêm vào PATH", ok=False)
        return False
    except Exception as e:
        print_status(f"Lỗi thêm PATH: {e}", ok=False)
        return False


def set_system_env_var(name, value):
    """Set a system environment variable permanently."""
    try:
        key = winreg.OpenKey(
            winreg.HKEY_LOCAL_MACHINE,
            r"SYSTEM\CurrentControlSet\Control\Session Manager\Environment",
            0,
            winreg.KEY_ALL_ACCESS,
        )
        winreg.SetValueEx(key, name, 0, winreg.REG_EXPAND_SZ, value)
        winreg.CloseKey(key)
        os.environ[name] = value
        return True
    except PermissionError:
        print_status(f"Cần quyền Admin để set {name}", ok=False)
        return False
    except Exception as e:
        print_status(f"Lỗi set {name}: {e}", ok=False)
        return False


def setup_environment_variables():
    """Set JAVA_HOME, ANDROID_HOME and add ADB + Java to system PATH."""
    cleanup_old_adb_paths()
    print_info("Đang cấu hình biến môi trường...")

    success = True

    # Set JAVA_HOME
    if os.path.exists(JAVA_DIR):
        if set_system_env_var("JAVA_HOME", JAVA_HOME):
            print_status(f"JAVA_HOME = {JAVA_HOME}")
        else:
            success = False

    # Set ANDROID_HOME
    if os.path.exists(SDK_DIR):
        if set_system_env_var("ANDROID_HOME", ANDROID_HOME):
            print_status(f"ANDROID_HOME = {ANDROID_HOME}")
        else:
            success = False

    # Add platform-tools to PATH (for adb)
    if os.path.exists(PLATFORM_TOOLS_DIR):
        if add_to_system_path(PLATFORM_TOOLS_DIR):
            print_status(f"PATH += {PLATFORM_TOOLS_DIR}")
        else:
            success = False

    # Add Java bin to PATH
    if os.path.exists(JAVA_BIN_DIR):
        if add_to_system_path(JAVA_BIN_DIR):
            print_status(f"PATH += {JAVA_BIN_DIR}")
        else:
            success = False

    # Broadcast environment change
    run_cmd('setx /M __DUMMY__ ""', check=False)
    run_cmd('reg delete "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment" /v __DUMMY__ /f', check=False)

    return success


def setup_env():
    """Download SDK platform-tools + JDK 17, setup environment."""
    print_step("Tải và cài đặt ADB + Java (C:\\LTHelper)")

    # Create base directory
    os.makedirs(LTHELPER_DIR, exist_ok=True)

    # 1. Download platform-tools
    ok_sdk = True
    if os.path.exists(os.path.join(PLATFORM_TOOLS_DIR, "adb.exe")):
        print_status("platform-tools đã có sẵn, bỏ qua tải")
    else:
        ok_sdk = download_and_extract_platform_tools()

    # 2. Download JDK 17
    ok_java = True
    if os.path.exists(os.path.join(JAVA_BIN_DIR, "java.exe")):
        print_status("JDK 17 đã có sẵn, bỏ qua tải")
    else:
        ok_java = download_and_extract_jdk17()

    # 3. Setup PATH and environment variables
    print_step("Cấu hình môi trường Windows")
    ok_env = setup_environment_variables()

    if ok_sdk and ok_java and ok_env:
        print(f"\n  {GREEN}✔ ADB + Java đã sẵn sàng tại C:\\LTHelper{RESET}")
        return True
    else:
        print_warn("Một số bước cài đặt thất bại, vui lòng kiểm tra lại")
        return False
