using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SubdyAutoUpdate;

internal static class UpdateService
{
    private const string MainExeName = "GolikePhoneFarm.exe";
    private const string MainProcessName = "GolikePhoneFarm";

    public static string AppDir => AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
    public static string MainExePath => Path.Combine(AppDir, MainExeName);

    // ── Kill process chính nếu đang chạy ───────────────────────────────────

    public static void KillMainProcessIfRunning()
    {
        var procs = Process.GetProcessesByName(MainProcessName);
        foreach (var p in procs)
        {
            try
            {
                p.Kill();
                p.WaitForExit(5000);
            }
            catch { }
            finally { p.Dispose(); }
        }
    }

    // ── Lấy version hiện tại của GolikePhoneFarm.exe ───────────────────────

    public static string GetLocalVersion()
    {
        if (!File.Exists(MainExePath)) return string.Empty;
        try
        {
            var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(MainExePath);
            return info.FileVersion ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    // ── Download file với progress callback ────────────────────────────────

    /// <param name="onProgress">percent 0-100, hoặc -1 nếu indeterminate</param>
    public static async Task DownloadFileAsync(
        string url,
        string destPath,
        Action<int, string> onProgress,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        long total = response.Content.Headers.ContentLength ?? -1L;

        using var src = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var dst = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);

        byte[] buf = new byte[8192];
        long read = 0;
        int last = -1;
        var sw = Stopwatch.StartNew();

        if (total <= 0) onProgress(-1, "Đang tải...");
        else onProgress(0, "Đang tải... 0%");

        int n;
        while ((n = await src.ReadAsync(buf.AsMemory(0, buf.Length), ct).ConfigureAwait(false)) > 0)
        {
            await dst.WriteAsync(buf.AsMemory(0, n), ct).ConfigureAwait(false);
            read += n;

            if (total > 0)
            {
                int pct = (int)(read * 100 / total);
                if (pct != last && (pct - last >= 1 || sw.ElapsedMilliseconds > 400))
                {
                    sw.Restart();
                    last = pct;
                    onProgress(pct, $"Đang tải... {pct}%");
                }
            }
        }

        onProgress(100, "Tải xong, đang chuẩn bị cập nhật...");
    }

    // ── Tạo BAT và restart ─────────────────────────────────────────────────

    public static void CreateUpdateBatAndExit(string zipPath, string oldVersion)
    {
        string exeName = MainExeName;
        string renamedExe = Path.GetFileNameWithoutExtension(MainExeName)
                            + (string.IsNullOrWhiteSpace(oldVersion) ? "-old" : $"-{oldVersion}")
                            + ".exe";
        string updateFolder = Path.GetDirectoryName(zipPath)!;
        string batPath = Path.Combine(Path.GetTempPath(), $"subdy_update_{Guid.NewGuid():N}.bat");

        string bat =
$"""
@echo off
cd /d "{AppDir}"
timeout /t 2 >nul

rem Backup old exe
if exist "{exeName}" (
    rename "{exeName}" "{renamedExe}"
)

rem Extract update
powershell -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive -LiteralPath '{zipPath}' -DestinationPath '{AppDir}' -Force"

rem Start new exe
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '{Path.Combine(AppDir, exeName)}' -Verb RunAs"

rem Cleanup
if exist "{updateFolder}" rd /s /q "{updateFolder}"

rem Self-delete
del "%~f0"
""";

        File.WriteAllText(batPath, bat, new System.Text.UTF8Encoding(false));

        Process.Start(new ProcessStartInfo
        {
            FileName = batPath,
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        });

        Environment.Exit(0);
    }
}
