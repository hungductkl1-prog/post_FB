using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace AutoAndroid
{
    public class AppCurrentInfo
    {
        [JsonPropertyName("package")]
        public string Package { get; set; }
        [JsonPropertyName("activity")]
        public string Activity { get; set; }
        [JsonPropertyName("pid")]
        public int Pid { get; set; } = -1;
    }
}
