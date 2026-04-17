from modules.utils import print_step, print_status, run_cmd, refresh_env_path, GREEN, RED, YELLOW, RESET


def verify_adb():
    ok, output = run_cmd("adb version", check=False)
    if ok and "Android Debug Bridge" in output:
        version = output.splitlines()[0] if output else ""
        print_status(f"ADB OK - {version}")
        return True
    print_status("ADB FAIL", ok=False)
    return False


def verify_node():
    ok, output = run_cmd("node --version", check=False)
    if ok and output.startswith("v"):
        print_status(f"Node OK - {output}")
        return True
    print_status("Node FAIL", ok=False)
    return False


def verify_npm():
    ok, output = run_cmd("npm --version", check=False)
    if ok and output:
        print_status(f"NPM OK - v{output}")
        return True
    print_status("NPM FAIL", ok=False)
    return False


def verify_java():
    ok, output = run_cmd("java -version", check=False)
    if ok and output:
        version = output.splitlines()[0] if output else ""
        print_status(f"Java OK - {version}")
        return True
    # java -version outputs to stderr on some JDKs
    ok2, output2 = run_cmd("java -version 2>&1", check=False)
    if ok2 and output2:
        version = output2.splitlines()[0] if output2 else ""
        print_status(f"Java OK - {version}")
        return True
    print_status("Java FAIL", ok=False)
    return False


def verify_all():
    """Verify all installed components."""
    print_step("Kiểm tra môi trường")
    refresh_env_path()

    results = {
        "ADB": verify_adb(),
        "Java": verify_java(),
        "Node": verify_node(),
        "NPM": verify_npm(),
    }

    passed = sum(1 for v in results.values() if v)
    total = len(results)

    print(f"\n  {YELLOW}Kết quả: {passed}/{total} thành phần OK{RESET}")

    if all(results.values()):
        print(f"\n  {GREEN}✔ Tất cả đã sẵn sàng!{RESET}")
        return True
    else:
        failed = [k for k, v in results.items() if not v]
        print(f"\n  {RED}✘ Thất bại: {', '.join(failed)}{RESET}")
        return False
