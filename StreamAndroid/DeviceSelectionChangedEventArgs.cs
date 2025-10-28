using Sunny.Subdy.Data.Models;

namespace StreamAndroid
{
    public class DeviceSelectionChangedEventArgs : EventArgs
    {
        public List<DeviceModel> SelectedDevices { get; set; }
        public int SelectionCount { get; set; }
    }

}
