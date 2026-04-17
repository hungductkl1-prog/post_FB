using System.Text.Json.Nodes;

namespace SubdyAutoUpdate;

internal static class LamToolApiClient
{
    private const string ToolSlug = "subdyfarm";

    /// <summary>
    /// Gọi API lamtool.net để lấy version mới nhất và link download.
    /// Trả về ("", "") nếu lỗi hoặc không có update.
    /// </summary>
    public static async Task<(string version, string url)> GetUpdateInfoAsync(string hwid)
    {
        try
        {
            string apiUrl = $"https://lamtool.net/api/license/check?tool_slug={ToolSlug}&device_code={Uri.EscapeDataString(hwid)}";
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            string json = await http.GetStringAsync(apiUrl).ConfigureAwait(false);

            var obj = JsonNode.Parse(json)?.AsObject();
            if (obj is null) return (string.Empty, string.Empty);

            string version = obj["license"]?["tool"]?["version"]?.ToString() ?? string.Empty;
            string url = obj["license"]?["tool"]?["updateUrl"]?.ToString() ?? string.Empty;
            return (version, url);
        }
        catch
        {
            return (string.Empty, string.Empty);
        }
    }

    /// <summary>
    /// Trả về true nếu remoteVersion mới hơn localVersion.
    /// Dùng logic đảo chiều giống LamToolClient.IsNewerVersion trong project chính.
    /// </summary>
    public static bool IsNewerVersion(string localVersion, string remoteVersion)
    {
        if (string.IsNullOrWhiteSpace(localVersion) || string.IsNullOrWhiteSpace(remoteVersion))
            return false;

        string[] local = localVersion.Split('.');
        string[] remote = remoteVersion.Split('.');

        Array.Reverse(local);
        Array.Reverse(remote);

        int len = Math.Max(local.Length, remote.Length);
        for (int i = 0; i < len; i++)
        {
            int lv = i < local.Length && int.TryParse(local[i], out var lp) ? lp : 0;
            int rv = i < remote.Length && int.TryParse(remote[i], out var rp) ? rp : 0;

            if (lv < rv) return true;
            if (lv > rv) return false;
        }
        return false;
    }
}
