using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AutoAndroid
{
    public class AtxDeviceInfo
    {
        [JsonProperty("udid")]
        public string udid { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("serial")]
        public string Serial { get; set; }
        [JsonProperty("brand")]
        public string Brand { get; set; }
        [JsonProperty("model")]
        public string Model { get; set; }
        [JsonProperty("hwaddr")]
        public string Hwaddr { get; set; }
        [JsonProperty("sdk")]
        public int Sdk { get; set; }
        [JsonProperty("agentVersion")]
        public string AgentVersion { get; set; }
        [JsonProperty("display")]
        public DisplayInfo Display { get; set; }
        [JsonProperty("battery")]
        public BatteryInfo Battery { get; set; }
        [JsonProperty("memory")]
        public MemoryInfo Memory { get; set; }
        [JsonProperty("cpu")]
        public CpuInfo Cpu { get; set; }
        [JsonProperty("arch")]
        public object Arch { get; set; }
        [JsonProperty("owner")]
        public object Owner { get; set; }
        [JsonProperty("presenceChangedAt")]
        public object PresenceChangedAt { get; set; }
        [JsonProperty("usingBeganAt")]
        public object UsingBeganAt { get; set; }
        [JsonProperty("product")]
        public object Product { get; set; }
        [JsonProperty("provider")]
        public object Provider { get; set; }

        public class DisplayInfo
        {
            [JsonProperty("width")]
            public int Width { get; set; }
            [JsonProperty("height")]
            public int Height { get; set; }
        }
        public class BatteryInfo
        {
            [JsonProperty("acPowered")]
            public bool AcPowered { get; set; }
            [JsonProperty("usbPowered")]
            public bool UsbPowered { get; set; }
            [JsonProperty("wirelessPowered")]
            public bool WirelessPowered { get; set; }
            [JsonProperty("present")]
            public bool Present { get; set; }
            [JsonProperty("status")]
            public int Status { get; set; }
            [JsonProperty("health")]
            public int Health { get; set; }
            [JsonProperty("level")]
            public int Level { get; set; }
            [JsonProperty("scale")]
            public int Scale { get; set; }
            [JsonProperty("voltage")]
            public int Voltage { get; set; }
            [JsonProperty("temperature")]
            public int Temperature { get; set; }
            [JsonProperty("technology")]
            public string Technology { get; set; }
        }
        public class MemoryInfo
        {
            [JsonProperty("total")]
            public long Total { get; set; }

            [JsonProperty("around")]
            public string Around { get; set; }
        }
        public class CpuInfo
        {
            [JsonProperty("cores")]
            public int Cores { get; set; }
            [JsonProperty("hardware")]
            public string Hardware { get; set; }
        }
    }
}
