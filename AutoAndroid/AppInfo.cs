using Newtonsoft.Json;

namespace AutoAndroid
{
    public class AppInfo
    {
        [JsonProperty("data")]
        public DataInfo Data { get; set; }
        [JsonProperty("success")]
        public bool Success { get; set; } = false;
        [JsonProperty("description")]
        public string Description { get; set; }

        public class DataInfo
        {
            [JsonProperty("packageName")]
            public string PackageName { get; set; }
            [JsonProperty("mainActivity")]
            public string MainActivity { get; set; }
            [JsonProperty("label")]
            public string Label { get; set; }
            [JsonProperty("versionName")]
            public string VersionName { get; set; }
            [JsonProperty("versionCode")]
            public long VersionCode { get; set; }
            [JsonProperty("size")]
            public long Size { get; set; }
        }
    }
}
