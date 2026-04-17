using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AutoAndroid
{
    /// <summary>
    /// Giao tiếp với ocr_helper.exe (Python) để xử lý ảnh qua base64 JSON.
    ///
    /// - Giữ một process Python sống suốt vòng đời object.
    /// - Queue: mỗi lúc chỉ xử lý 1 request, các request từ nhiều luồng
    ///   AutoAndroid sẽ xếp hàng chờ tuần tự.
    /// - Ảnh lớn lấy từ _client.Screenshot() (ATX hoặc Appium tùy mode).
    /// </summary>
    public sealed class OcrHelperService : IDisposable
    {
        // ── Singleton process ────────────────────────────────────────────────
        private Process? _proc;
        private readonly SemaphoreSlim _queue = new(1, 1);  // queue 1 lần 1 luồng
        private readonly object _procLock = new();
        private bool _disposed;

        // Đường dẫn tới ocr_helper.exe (mặc định: cạnh exe đang chạy)
        private readonly string _exePath;
        private readonly ADBClient _client;

        // Thời gian timeout chờ response từ Python (ms)
        private const int ResponseTimeoutMs = 60_000;

        /// <summary>Lưu lý do null gần nhất — dùng để debug.</summary>
        public string? LastError { get; private set; }

        // ── Kết quả trả về ──────────────────────────────────────────────────

        public sealed class TemplateMatchResult
        {
            public int X { get; init; }
            public int Y { get; init; }
            public double Confidence { get; init; }
        }

        public sealed class OcrFindResult
        {
            public int X { get; init; }
            public int Y { get; init; }
            public double Confidence { get; init; }
            public int FoundInPart { get; init; }
        }

        // ── Constructor ──────────────────────────────────────────────────────

        public OcrHelperService(ADBClient client, string? exePath = null)
        {
            _client = client;
            _exePath = exePath
                ?? Path.Combine(AppContext.BaseDirectory, "ocr_helper.exe");
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Tìm vị trí ảnh nhỏ (template) trong màn hình hiện tại của device.
        /// Tự chụp screenshot qua _client.Screenshot().
        /// </summary>
        /// <param name="templateBitmap">Ảnh template cần tìm.</param>
        /// <param name="threshold">Độ chính xác tối thiểu (0.0 – 1.0), mặc định 0.85.</param>
        /// <param name="screenshot">Nếu null sẽ tự chụp màn hình.</param>
        public async Task<TemplateMatchResult?> FindTemplateAsync(
            Bitmap templateBitmap,
            double threshold = 0.85,
            Bitmap? screenshot = null)
        {
            Bitmap screen = screenshot ?? _client.Screenshot()
                ?? throw new InvalidOperationException("Screenshot() trả về null");
            try
            {
                Debug.WriteLine($"[OcrHelper] screen={screen.Width}x{screen.Height} template={templateBitmap.Width}x{templateBitmap.Height} threshold={threshold}");

                string screenB64 = BitmapToBase64(screen);
                string templateB64 = BitmapToBase64(templateBitmap);

                // Gửi threshold=0 để Python luôn trả về best match kèm confidence thực
                var request = new JsonObject
                {
                    ["id"] = NewId(),
                    ["action"] = "template_match",
                    ["params"] = new JsonObject
                    {
                        ["screenshot_b64"] = screenB64,
                        ["template_b64"] = templateB64,
                        ["threshold"] = 0.01,          // luôn trả về, C# tự lọc
                        ["android_width"] = screen.Width,
                        ["android_height"] = screen.Height,
                    }
                };

                var resp = await SendRequestAsync(request);
                if (resp is null) return null;

                var root = resp.Value;
                if (!root.GetProperty("success").GetBoolean())
                {
                    LastError = root.TryGetProperty("error", out var e) ? e.GetString() : "no match";
                    Debug.WriteLine($"[OcrHelper] no match: {LastError}");
                    return null;
                }

                var result = root.GetProperty("result");
                double confidence = result.GetProperty("confidence").GetDouble();
                Debug.WriteLine($"[OcrHelper] best confidence={confidence:F4} threshold={threshold}");

                // Lọc theo threshold thực ở C#
                if (confidence < threshold)
                {
                    LastError = $"Best confidence {confidence:F4} < threshold {threshold}";
                    return null;
                }

                return new TemplateMatchResult
                {
                    X = result.GetProperty("x").GetInt32(),
                    Y = result.GetProperty("y").GetInt32(),
                    Confidence = confidence,
                };
            }
            catch (Exception exx)
            {

            }
           return null;
        }

        /// <summary>
        /// Tìm vị trí text trong màn hình hiện tại của device bằng OCR song song 5 luồng.
        /// Tự chụp screenshot qua _client.Screenshot().
        /// </summary>
        /// <param name="searchText">Đoạn text cần tìm.</param>
        /// <param name="lang">Ngôn ngữ Tesseract, ví dụ "vie+eng".</param>
        /// <param name="confidenceThreshold">Confidence tối thiểu (0–100), mặc định 60.</param>
        /// <param name="screenshot">Nếu null sẽ tự chụp màn hình.</param>
        public async Task<OcrFindResult?> FindTextAsync(
            string searchText,
            string lang = "eng",
            double confidenceThreshold = 60,
            Bitmap? screenshot = null)
        {
            Bitmap screen = screenshot ?? _client.Screenshot()
                ?? throw new InvalidOperationException("Screenshot() trả về null");

            string screenB64 = BitmapToBase64(screen);

            var request = new JsonObject
            {
                ["id"] = NewId(),
                ["action"] = "ocr_find",
                ["params"] = new JsonObject
                {
                    ["screenshot_b64"] = screenB64,
                    ["search_text"] = searchText,
                    ["lang"] = lang,
                    ["confidence_threshold"] = confidenceThreshold,
                    ["android_width"] = screen.Width,
                    ["android_height"] = screen.Height,
                }
            };

            var resp = await SendRequestAsync(request);
            if (resp is null || !resp.Value.GetProperty("success").GetBoolean())
                return null;

            var result = resp.Value.GetProperty("result");
            return new OcrFindResult
            {
                X = result.GetProperty("x").GetInt32(),
                Y = result.GetProperty("y").GetInt32(),
                Confidence = result.GetProperty("confidence").GetDouble(),
                FoundInPart = result.GetProperty("found_in_part").GetInt32(),
            };
        }

        // ── Internal: Process management ────────────────────────────────────

        private Process EnsureProcess()
        {
            lock (_procLock)
            {
                if (_proc != null && !_proc.HasExited)
                    return _proc;

                _proc?.Dispose();
                _proc = StartProcess();
                Warmup(_proc);
                return _proc;
            }
        }

        private Process StartProcess()
        {
            if (!File.Exists(_exePath))
                throw new FileNotFoundException($"ocr_helper.exe không tồn tại tại: {_exePath}");

            var psi = new ProcessStartInfo
            {
                FileName = _exePath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardInputEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            var proc = new Process { StartInfo = psi };
            proc.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.WriteLine($"[OcrHelper][py] {e.Data}");
            };
            proc.Start();
            proc.BeginErrorReadLine();
            return proc;
        }

        private static void Warmup(Process proc)
        {
            // Gửi ping và đọc pong — phải drain hết response ra khỏi buffer
            // trước khi SendRequestAsync dùng stream, nếu không sẽ đọc nhầm pong
            string ping = new JsonObject { ["id"] = "warmup", ["action"] = "ping" }.ToJsonString();
            proc.StandardInput.WriteLine(ping);
            proc.StandardInput.Flush();

            // Dùng Task.Run + Wait để tránh deadlock, timeout 10s
            try
            {
                var task = Task.Run(() => proc.StandardOutput.ReadLine());
                bool done = task.Wait(10_000);
                string? pong = done ? task.Result : null;
                System.Diagnostics.Debug.WriteLine($"[OcrHelper] warmup pong: {pong}");
            }
            catch { /* bỏ qua */ }
        }

        // ── Internal: Queue + send ───────────────────────────────────────────

        private async Task<JsonElement?> SendRequestAsync(JsonObject request)
        {
            await _queue.WaitAsync();
            try
            {
                Process proc = EnsureProcess();
                string json = request.ToJsonString();

                System.Diagnostics.Debug.WriteLine($"[OcrHelper] >> {json[..Math.Min(200, json.Length)]}...");

                proc.StandardInput.WriteLine(json);
                proc.StandardInput.Flush();

                using var cts = new CancellationTokenSource(ResponseTimeoutMs);
                string? line = await proc.StandardOutput.ReadLineAsync(cts.Token);

                System.Diagnostics.Debug.WriteLine($"[OcrHelper] << {line}");

                if (string.IsNullOrWhiteSpace(line))
                {
                    LastError = "Python trả về dòng trống (process crash?)";
                    return null;
                }

                var doc = JsonDocument.Parse(line).RootElement;

                // Nếu success=false, lưu error để caller biết lý do
                if (!doc.GetProperty("success").GetBoolean())
                {
                    LastError = doc.TryGetProperty("error", out var err) ? err.GetString() : "unknown error";
                    System.Diagnostics.Debug.WriteLine($"[OcrHelper] error: {LastError}");
                }

                return doc;
            }
            catch (OperationCanceledException)
            {
                LastError = $"Timeout sau {ResponseTimeoutMs}ms";
                System.Diagnostics.Debug.WriteLine($"[OcrHelper] TIMEOUT");
                lock (_procLock)
                {
                    try { _proc?.Kill(); } catch { }
                    _proc = null;
                }
                return null;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[OcrHelper] EXCEPTION: {ex.Message}");
                return null;
            }
            finally
            {
                _queue.Release();
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string BitmapToBase64(Bitmap bmp)
        {
            // Clone sang Bitmap mới độc lập (tránh lỗi khi backing MemoryStream của bmp đã bị dispose)
            using var clone = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = System.Drawing.Graphics.FromImage(clone))
                g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);

            using var ms = new MemoryStream();
            clone.Save(ms, ImageFormat.Png);
            return Convert.ToBase64String(ms.ToArray());
        }

        private static string NewId() => Guid.NewGuid().ToString("N")[..8];

        // ── IDisposable ───────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _queue.Dispose();
            lock (_procLock)
            {
                try
                {
                    _proc?.StandardInput.Close();
                    _proc?.WaitForExit(2000);
                    _proc?.Kill();
                }
                catch { }
                _proc?.Dispose();
                _proc = null;
            }
        }
    }
}
