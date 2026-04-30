using System.Net;
using System.Net.Http;

namespace ViewControl;

/// <summary>
/// Tự động tải scrcpy-server.jar từ GitHub về thư mục ScrcpyNet/
/// nếu file chưa tồn tại tại runtime. Dùng cho bản publish AOT
/// đã được copy đầy đủ DLL native nhưng có thể thiếu jar.
/// </summary>
internal static class ScrcpyServerDownloader
{
    private const string DownloadUrl =
        "https://github.com/LamLe2001/changer/raw/main/scrcpy-server.jar";

    private const long MinExpectedSize = 10_000; // ~41KB thực tế, chặn file rỗng/lỗi

    /// <summary>
    /// Đảm bảo scrcpy-server.jar tồn tại tại {BaseDir}/ScrcpyNet/.
    /// Trả về true nếu file đã có sẵn hoặc tải thành công.
    /// </summary>
    public static bool EnsureScrcpyServerJar()
    {
        try
        {
            string scrcpyDir = Path.Combine(AppContext.BaseDirectory, "ScrcpyNet");
            string jarPath = Path.Combine(scrcpyDir, "scrcpy-server.jar");

            if (File.Exists(jarPath))
            {
                var existing = new FileInfo(jarPath);
                if (existing.Length >= MinExpectedSize)
                {
                    Program.LogLine($"scrcpy-server.jar đã tồn tại ({existing.Length} bytes).");
                    return true;
                }
                Program.LogLine($"scrcpy-server.jar tồn tại nhưng size bất thường ({existing.Length}), tải lại.");
                try { File.Delete(jarPath); } catch { }
            }

            Directory.CreateDirectory(scrcpyDir);

            Program.LogLine($"Đang tải scrcpy-server.jar từ {DownloadUrl}...");

            // TLS 1.2 cho Windows cũ
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13; }
            catch { }

            string tmpPath = jarPath + ".downloading";
            try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }

            using (var handler = new HttpClientHandler { AllowAutoRedirect = true })
            using (var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) })
            {
                http.DefaultRequestHeaders.UserAgent.ParseAdd("ViewControl/1.0");
                using var resp = http.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
                resp.EnsureSuccessStatusCode();

                using var src = resp.Content.ReadAsStream();
                using (var dst = File.Create(tmpPath))
                {
                    src.CopyTo(dst);
                }
            }

            var fi = new FileInfo(tmpPath);
            if (fi.Length < MinExpectedSize)
            {
                try { File.Delete(tmpPath); } catch { }
                Program.LogLine($"Tải scrcpy-server.jar thất bại: file quá nhỏ ({fi.Length} bytes).");
                return false;
            }

            File.Move(tmpPath, jarPath, overwrite: true);
            Program.LogLine($"Tải scrcpy-server.jar thành công ({fi.Length} bytes) -> {jarPath}");
            return true;
        }
        catch (Exception ex)
        {
            Program.LogLine($"Lỗi tải scrcpy-server.jar: {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }
}
