using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AutoAndroid
{
    public class AppCurrentInfo
    {
        [JsonProperty("package")]
        public string Package { get; set; }
        [JsonProperty("activity")]
        public string Activity { get; set; }
        [JsonProperty("pid")]
        public int Pid { get; set; } = -1;
    }
}
