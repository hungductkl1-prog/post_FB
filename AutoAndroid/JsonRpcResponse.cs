using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace AutoAndroid
{
    public class JsonRpcResponse
    {
        [JsonProperty("jsonrpc")]
        public string Version;

        [JsonProperty("id")]
        public string Id;

        [JsonProperty("error")]
        public JObject Error = null;

        [JsonProperty("result")]
        public object Data;

    }
}
