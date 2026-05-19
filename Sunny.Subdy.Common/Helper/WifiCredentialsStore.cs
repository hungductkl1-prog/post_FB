using System.Text.Json.Nodes;

namespace Sunny.Subdy.Common.Helper
{
    public class WifiCredential
    {
        public string DeviceId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
    }

    // Uses JsonNode (not reflection-based JsonSerializer) because the app
    // disables reflection-based serialization (PublishTrimmed/AOT settings).
    public static class WifiCredentialsStore
    {
        private static readonly string FilePath =
            Path.Combine(AppContext.BaseDirectory, "Config", "wifi-credentials.json");

        private static readonly object _lock = new();

        public static Dictionary<string, WifiCredential> LoadAll()
        {
            lock (_lock)
            {
                if (!File.Exists(FilePath)) return new Dictionary<string, WifiCredential>();
                try
                {
                    var json = File.ReadAllText(FilePath);
                    if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, WifiCredential>();

                    var node = JsonNode.Parse(json);
                    var arr = node as JsonArray;
                    if (arr == null) return new Dictionary<string, WifiCredential>();

                    var dict = new Dictionary<string, WifiCredential>();
                    foreach (var item in arr)
                    {
                        if (item is not JsonObject obj) continue;
                        var id = obj["DeviceId"]?.GetValue<string>() ?? "";
                        if (string.IsNullOrEmpty(id)) continue;
                        dict[id] = new WifiCredential
                        {
                            DeviceId = id,
                            UserName = obj["UserName"]?.GetValue<string>() ?? "",
                            Password = obj["Password"]?.GetValue<string>() ?? "",
                        };
                    }
                    return dict;
                }
                catch { return new Dictionary<string, WifiCredential>(); }
            }
        }

        public static WifiCredential? GetBySerial(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return null;
            var all = LoadAll();
            return all.TryGetValue(deviceId, out var c) ? c : null;
        }

        public static void Save(IEnumerable<WifiCredential> credentials)
        {
            lock (_lock)
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var arr = new JsonArray();
                foreach (var c in credentials)
                {
                    if (c == null || string.IsNullOrEmpty(c.DeviceId)) continue;
                    arr.Add(new JsonObject
                    {
                        ["DeviceId"] = c.DeviceId,
                        ["UserName"] = c.UserName ?? "",
                        ["Password"] = c.Password ?? "",
                    });
                }
                File.WriteAllText(FilePath, arr.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
        }

        public static void Upsert(string deviceId, string userName, string password)
        {
            if (string.IsNullOrEmpty(deviceId)) return;
            var all = LoadAll();
            all[deviceId] = new WifiCredential { DeviceId = deviceId, UserName = userName ?? "", Password = password ?? "" };
            Save(all.Values);
        }
    }
}
