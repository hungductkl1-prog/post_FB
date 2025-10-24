using SharpAdbClient;
using StreamAndroid.Services;
using Sunny.Subdy.Data.Models;

namespace StreamAndroid.Models
{
    public class DeviceView
    {
        public DeviceModel DeviceModel { get; set; }
        public DeviceData DeviceData { get; set; }
        public Scrcpy Scrcpy { get; set; }
        public DataGridViewRow DataGridViewRow { get; set; }
        public ucControlAndroid UCControlAndroid { get; set; }
        public ucThongBaoDeviceView ucThongBaoDeviceView { get; set; }

    }
}
