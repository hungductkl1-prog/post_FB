"""
Entry point của OCR-Helper-Python exe.

Vòng lặp: đọc một dòng JSON từ stdin -> xử lý -> ghi một dòng JSON ra stdout.
Mỗi dòng kết thúc bằng newline, encoding UTF-8.

C# sử dụng:
    Process proc = new Process();
    proc.StartInfo.FileName = "ocr_helper.exe";
    proc.StartInfo.UseShellExecute = false;
    proc.StartInfo.RedirectStandardInput = true;
    proc.StartInfo.RedirectStandardOutput = true;
    proc.StartInfo.StandardInputEncoding  = Encoding.UTF8;
    proc.StartInfo.StandardOutputEncoding = Encoding.UTF8;
    proc.Start();
    // Warmup
    proc.StandardInput.WriteLine("{\"id\":\"0\",\"action\":\"ping\"}");
    proc.StandardInput.Flush();
    string pong = proc.StandardOutput.ReadLine();
"""

import json
import sys


def _configure_streams():
    """Đảm bảo stdin/stdout dùng UTF-8, không buffer stdout."""
    if hasattr(sys.stdin, "reconfigure"):
        sys.stdin.reconfigure(encoding="utf-8", errors="replace")
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")


def _write_response(response: dict):
    """Ghi response JSON ra stdout, flush ngay."""
    line = json.dumps(response, ensure_ascii=False)
    sys.stdout.write(line + "\n")
    sys.stdout.flush()


def main():
    _configure_streams()

    # Import dispatcher sau khi stream đã cấu hình xong
    from dispatcher import dispatch

    for raw_line in sys.stdin:
        raw_line = raw_line.strip()
        if not raw_line:
            continue

        # Parse JSON request
        try:
            request = json.loads(raw_line)
        except json.JSONDecodeError as e:
            _write_response({
                "id": "",
                "success": False,
                "result": None,
                "error": f"Invalid JSON: {e}",
            })
            continue

        if not isinstance(request, dict):
            _write_response({
                "id": "",
                "success": False,
                "result": None,
                "error": "Request must be a JSON object",
            })
            continue

        # Dispatch và ghi response
        response = dispatch(request)
        _write_response(response)


if __name__ == "__main__":
    # PyInstaller multiprocessing support trên Windows
    import multiprocessing
    multiprocessing.freeze_support()
    main()
