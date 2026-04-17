using System.Text.Json.Serialization;

namespace AutoAndroid
{
    public class AppInfo
    {
        [JsonPropertyName("data")]
        public DataInfo Data { get; set; }
        [JsonPropertyName("success")]
        public bool Success { get; set; } = false;
        [JsonPropertyName("description")]
        public string Description { get; set; }

        public class DataInfo
        {
            [JsonPropertyName("packageName")]
            public string PackageName { get; set; }
            [JsonPropertyName("mainActivity")]
            public string MainActivity { get; set; }
            [JsonPropertyName("label")]
            public string Label { get; set; }
            [JsonPropertyName("versionName")]
            public string VersionName { get; set; }
            [JsonPropertyName("versionCode")]
            public long VersionCode { get; set; }
            [JsonPropertyName("size")]
            public long Size { get; set; }
        }
    }
}
