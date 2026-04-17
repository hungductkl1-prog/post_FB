using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace AutoAndroid
{
    public class UADeviceInfo
    {
        [JsonPropertyName("currentPackageName")]
        public string CurrentPackageName { get; set; }

        [JsonPropertyName("displayRotation")]
        public int DisplayRotation { get; set; }

        [JsonPropertyName("displayHeight")]
        public int DisplayHeight { get; set; }
        [JsonPropertyName("displayWidth")]
        public int DisplayWidth { get; set; }

        [JsonPropertyName("displaySizeDpX")]
        public int DisplaySizeDpX { get; set; }
        [JsonPropertyName("displaySizeDpY")]
        public int DisplaySizeDpY { get; set; }

        [JsonPropertyName("productName")]
        public string ProductName { get; set; }

        [JsonPropertyName("screenOn")]
        public bool ScreenOn { get; set; }

        [JsonPropertyName("sdkInt")]
        public int SdkInt { get; set; }

        [JsonPropertyName("naturalOrientation")]
        public bool NaturalOrientation { get; set; }

    }
}
