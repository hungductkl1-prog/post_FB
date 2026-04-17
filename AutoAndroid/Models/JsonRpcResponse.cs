using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AutoAndroid
{
    public class JsonRpcResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string Version;

        [JsonPropertyName("id")]
        public string Id;

        [JsonPropertyName("error")]
        public JsonObject? Error = null;

        [JsonPropertyName("result")]
        public JsonNode? Data;

    }
}
