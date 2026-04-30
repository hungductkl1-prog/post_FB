using System.Text.Json.Nodes;

namespace SubdyAutoUpdate;

internal static class LamToolApiClient
{
    private const string LatestApiUrl = "https://auto.golike.net/api/public/tools/auto-phone-farm/latest?platform=WINDOWS";

    /// <summary>
    /// Gọi API auto.golike.net để lấy version mới nhất và link download.
    /// Trả về ("", "") nếu lỗi hoặc không có update.
    /// </summary>
    public static async Task<(string version, string url)> GetUpdateInfoAsync(string hwid)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            string json = await http.GetStringAsync(LatestApiUrl).ConfigureAwait(false);

            var obj = JsonNode.Parse(json)?.AsObject();
            if (obj is null) return (string.Empty, string.Empty);

            string version = obj["version"]?.ToString() ?? string.Empty;
            string url = obj["updateFile"]?["url"]?.ToString() ?? string.Empty;
            return (version, url);
        }
        catch
        {
            return (string.Empty, string.Empty);
        }
    }

    /// <summary>
    /// Trả về true nếu remoteVersion mới hơn localVersion.
    /// Format: yy.MM.dd.build (vd 26.04.28.1) — so sánh trái→phải: year, month, day, build.
    /// </summary>
    public static bool IsNewerVersion(string localVersion, string remoteVersion)
    {
        if (string.IsNullOrWhiteSpace(localVersion) || string.IsNullOrWhiteSpace(remoteVersion))
            return false;

        string[] local = localVersion.Trim().Split('.');
        string[] remote = remoteVersion.Trim().Split('.');
        int len = Math.Max(local.Length, remote.Length);

        for (int i = 0; i < len; i++)
        {
            int lv = i < local.Length && int.TryParse(local[i], out var lp) ? lp : 0;
            int rv = i < remote.Length && int.TryParse(remote[i], out var rp) ? rp : 0;

            if (rv > lv) return true;
            if (rv < lv) return false;
        }
        return false;
    }
}
