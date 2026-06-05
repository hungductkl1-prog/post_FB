using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http;

namespace Facebook_Farm_NewFeed_PostStory.Utils;

/// <summary>
/// Đảm bảo folder ScrcpyNet (chứa FFmpeg/SDL2/scrcpy native deps) tồn tại
/// tại {BaseDir}/ScrcpyNet/. Nếu thiếu sẽ tự tải ZIP từ GitHub và giải nén.
/// Dùng chung cho cả LamToolAutoPhonePrime và View-Control trước khi gọi
/// <c>FFmpeg.AutoGen.ffmpeg.RootPath = ...</c>.
/// </summary>
internal static class ScrcpyNetFolderEnsurer
{
    private const string ZipUrl =
        "https://github.com/LamLe2001/changer/raw/main/ScrcpyNet.zip";

    private const long MinExpectedZipSize = 100_000; // chặn zip rỗng/lỗi

    /// <summary>
    /// Đảm bảo folder ScrcpyNet tồn tại và có file native bên trong.
    /// Trả về đường dẫn folder. Không throw — log lỗi qua Trace nếu fail.
    /// </summary>
    public static string EnsureScrcpyNetFolder()
    {
        string scrcpyDir = Path.Combine(AppContext.BaseDirectory, "ScrcpyNet");

        try
        {
            if (HasNativeFiles(scrcpyDir))
            {
                Trace.WriteLine($"[ScrcpyNetFolderEnsurer] folder đã tồn tại và có file native: {scrcpyDir}");
                return scrcpyDir;
            }

            Trace.WriteLine($"[ScrcpyNetFolderEnsurer] folder thiếu hoặc rỗng -> tải ZIP từ {ZipUrl}");
            Directory.CreateDirectory(scrcpyDir);

            // TLS 1.2/1.3 cho Windows cũ
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13; }
            catch { }

            string tmpZip = Path.Combine(Path.GetTempPath(), $"ScrcpyNet_{Guid.NewGuid():N}.zip");

            try
            {
                using (var handler = new HttpClientHandler { AllowAutoRedirect = true })
                using (var http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) })
                {
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("LamTool/1.0");
                    using var resp = http.GetAsync(ZipUrl, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
                    resp.EnsureSuccessStatusCode();

                    using var src = resp.Content.ReadAsStream();
                    using var dst = File.Create(tmpZip);
                    src.CopyTo(dst);
                }

                var fi = new FileInfo(tmpZip);
                if (fi.Length < MinExpectedZipSize)
                {
                    Trace.WriteLine($"[ScrcpyNetFolderEnsurer] ZIP quá nhỏ ({fi.Length} bytes) — bỏ qua.");
                    return scrcpyDir;
                }

                Trace.WriteLine($"[ScrcpyNetFolderEnsurer] tải ZIP thành công ({fi.Length} bytes), giải nén...");
                ExtractZipToFolder(tmpZip, scrcpyDir);
                Trace.WriteLine($"[ScrcpyNetFolderEnsurer] giải nén xong vào {scrcpyDir}");
            }
            finally
            {
                try { if (File.Exists(tmpZip)) File.Delete(tmpZip); } catch { }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[ScrcpyNetFolderEnsurer] lỗi: {ex.GetType().Name}: {ex.Message}");
        }

        return scrcpyDir;
    }

    /// <summary>
    /// Folder được coi là "đã đầy đủ" nếu chứa ít nhất 1 file ngoài
    /// scrcpy-server.jar (thường là DLL FFmpeg/SDL2). scrcpy-server.jar
    /// được handle riêng bởi ScrcpyServerDownloader nên không tính ở đây.
    /// </summary>
    private static bool HasNativeFiles(string dir)
    {
        if (!Directory.Exists(dir)) return false;
        try
        {
            foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(f);
                if (string.Equals(name, "scrcpy-server.jar", StringComparison.OrdinalIgnoreCase))
                    continue;
                return true;
            }
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Giải nén zip vào targetDir. Nếu zip có 1 folder root duy nhất tên
    /// "ScrcpyNet" thì flatten vào targetDir thay vì tạo ScrcpyNet/ScrcpyNet/.
    /// Bỏ qua entry đã tồn tại để idempotent.
    /// </summary>
    private static void ExtractZipToFolder(string zipPath, string targetDir)
    {
        using var archive = ZipFile.OpenRead(zipPath);

        // Phát hiện folder root chung (vd: "ScrcpyNet/") để flatten
        string? commonRoot = DetectCommonRoot(archive);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith("/"))
                continue; // pure directory entry

            string relative = entry.FullName.Replace('\\', '/');
            if (commonRoot != null && relative.StartsWith(commonRoot, StringComparison.OrdinalIgnoreCase))
                relative = relative.Substring(commonRoot.Length);

            if (string.IsNullOrEmpty(relative)) continue;

            string destPath = Path.GetFullPath(Path.Combine(targetDir, relative));
            string fullTarget = Path.GetFullPath(targetDir);
            // Chống Zip Slip
            if (!destPath.StartsWith(fullTarget, StringComparison.OrdinalIgnoreCase))
            {
                Trace.WriteLine($"[ScrcpyNetFolderEnsurer] bỏ qua entry nghi Zip Slip: {entry.FullName}");
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

            if (entry.Name.Length == 0) continue; // chỉ là folder

            try
            {
                entry.ExtractToFile(destPath, overwrite: false);
            }
            catch (IOException)
            {
                // file đã tồn tại — bỏ qua để idempotent
            }
        }
    }

    private static string? DetectCommonRoot(ZipArchive archive)
    {
        string? root = null;
        foreach (var entry in archive.Entries)
        {
            string full = entry.FullName.Replace('\\', '/');
            int slash = full.IndexOf('/');
            if (slash <= 0) return null; // có entry ở root -> không flatten
            string first = full.Substring(0, slash + 1);
            if (root == null) root = first;
            else if (!string.Equals(root, first, StringComparison.OrdinalIgnoreCase)) return null;
        }
        return root;
    }
}
