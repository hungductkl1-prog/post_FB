import os
from modules.utils import print_step, print_status, print_info, run_cmd, run_cmd_live, refresh_env_path, GREEN, RESET


def is_dotnet9_installed():
    """Check if .NET 9 runtime is installed."""
    ok, output = run_cmd("dotnet --list-runtimes", check=False)
    if ok and output:
        for line in output.splitlines():
            if line.startswith("Microsoft.NETCore.App 9."):
                version = line.split()[1]
                return True, version
    ok, output = run_cmd("dotnet --version", check=False)
    if ok and output.startswith("9."):
        return True, output
    return False, ""


def install_dotnet_via_powershell():
    """Install .NET 9 by running dotnet-install.ps1 directly from PowerShell (no pre-download)."""
    print_info("Cai dat .NET 9 bang PowerShell...")

    # PowerShell one-liner: download and execute install script in memory
    ps_cmd = (
        'powershell -ExecutionPolicy Bypass -Command "'
        "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; "
        "&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) "
        "-Channel 9.0 -Runtime windowsdesktop -InstallDir 'C:\\Program Files\\dotnet'"
        '"'
    )
    run_cmd_live(ps_cmd, label="Cai dat .NET 9 Desktop Runtime", timeout=300)

    refresh_env_path()
    ensure_dotnet_in_path()
    installed, _ = is_dotnet9_installed()
    if installed:
        return True

    # Try runtime only (smaller)
    print_info("Thu cai .NET 9 Runtime...")
    ps_cmd2 = (
        'powershell -ExecutionPolicy Bypass -Command "'
        "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; "
        "&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) "
        "-Channel 9.0 -Runtime dotnet -InstallDir 'C:\\Program Files\\dotnet'"
        '"'
    )
    run_cmd_live(ps_cmd2, label="Cai dat .NET 9 Runtime", timeout=300)

    refresh_env_path()
    ensure_dotnet_in_path()
    installed, _ = is_dotnet9_installed()
    return installed


def install_dotnet_via_winget():
    """Fallback: Install .NET 9 via winget."""
    print_info("Thu cai bang winget...")
    run_cmd_live(
        'winget install --id Microsoft.DotNet.DesktopRuntime.9 -e --silent --accept-package-agreements --accept-source-agreements',
        label="Cai dat .NET 9 (winget)",
        timeout=600,
    )
    refresh_env_path()
    installed, _ = is_dotnet9_installed()
    if installed:
        print_status(".NET 9 da san sang (winget)")
        return True
    return False


def ensure_dotnet_in_path():
    """Ensure dotnet directory is in current process PATH."""
    dotnet_dir = r"C:\Program Files\dotnet"
    if os.path.exists(dotnet_dir):
        current_path = os.environ.get("PATH", "")
        if dotnet_dir.lower() not in current_path.lower():
            os.environ["PATH"] = dotnet_dir + ";" + current_path


def setup_dotnet():
    """Check and install .NET 9."""
    print_step("Cai dat .NET 9")

    installed, version = is_dotnet9_installed()
    if installed:
        print_status(f".NET 9 da co san ({version})")
        return True

    # Try PowerShell install script
    if install_dotnet_via_powershell():
        refresh_env_path()
        installed, version = is_dotnet9_installed()
        if installed:
            print(f"\n  {GREEN}[OK] .NET 9 ({version}) da san sang{RESET}")
            return True

    # Fallback to winget
    if install_dotnet_via_winget():
        refresh_env_path()
        installed, version = is_dotnet9_installed()
        if installed:
            print(f"\n  {GREEN}[OK] .NET 9 ({version}) da san sang{RESET}")
            return True

    print_status(".NET 9 cai dat that bai", ok=False)
    return False
