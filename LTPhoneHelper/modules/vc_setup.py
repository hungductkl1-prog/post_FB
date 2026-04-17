import winreg
from modules.utils import print_step, print_status, run_cmd_live, GREEN, RED, RESET


def is_vc_installed():
    """Check if Visual C++ Redistributable is installed (x64 and x86)."""
    # On 64-bit Windows, x86 entries may be under WOW6432Node
    paths = {
        "x64": [
            r"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64",
        ],
        "x86": [
            r"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86",
            r"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x86",
        ],
    }
    installed = {"x64": False, "x86": False}

    for arch, reg_paths in paths.items():
        for path in reg_paths:
            try:
                key = winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, path)
                value, _ = winreg.QueryValueEx(key, "Installed")
                winreg.CloseKey(key)
                if value == 1:
                    installed[arch] = True
                    break
            except OSError:
                pass

    return installed


def install_vc(arch):
    """Install Visual C++ Redistributable for given architecture via winget."""
    pkg_id = f"Microsoft.VCRedist.2015+.{arch}"
    run_cmd_live(
        f'winget install --id {pkg_id} -e --silent --accept-package-agreements --accept-source-agreements',
        label=f"Cài Visual C++ {arch}",
        timeout=300,
    )
    # Verify by checking registry (winget exit code unreliable for "already installed")
    status = is_vc_installed()
    if status[arch]:
        print_status(f"Visual C++ {arch} đã sẵn sàng")
        return True
    else:
        print_status(f"Visual C++ {arch} cài đặt thất bại", ok=False)
        return False


def setup_vcredist():
    """Check and install Visual C++ Redistributable (x64 + x86)."""
    print_step("Kiểm tra Visual C++ Redistributable")

    status = is_vc_installed()
    all_ok = True

    if status["x64"]:
        print_status("Visual C++ x64 đã có sẵn")
    else:
        if not install_vc("x64"):
            all_ok = False

    if status["x86"]:
        print_status("Visual C++ x86 đã có sẵn")
    else:
        if not install_vc("x86"):
            all_ok = False

    if all_ok:
        print(f"\n  {GREEN}✔ Visual C++ đã sẵn sàng{RESET}")
    else:
        print(f"\n  {RED}✘ Visual C++ cài đặt chưa hoàn tất{RESET}")

    return all_ok
