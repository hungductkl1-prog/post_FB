using System.Text.Json.Serialization;

namespace Sunny.Subdy.Server
{
    [JsonSerializable(typeof(ApiResponse<object>))]
    [JsonSerializable(typeof(ApiResponse<List<DeviceRespone>>))]
    [JsonSerializable(typeof(ApiResponse<DeviceRespone>))] // 👈 Thêm dòng này
    [JsonSerializable(typeof(ApiResponse<object>))]
    [JsonSerializable(typeof(DeviceRespone))]
    [JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    public partial class MyJsonContext : JsonSerializerContext
    {
    }
}
