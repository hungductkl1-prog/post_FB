using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Common.Services;
using Sunny.Subdy.Data.Models;

namespace AutoAndroid
{
    /// <summary>
    /// Điểm gọi chung cho các project khác dùng AutoAndroid.
    /// Mục tiêu: gọi 1 class thay vì phải biết nhiều service nội bộ.
    /// </summary>
    public static class AutoAndroidBridge
    {
        public static string DefaultType => AutomationTypeResolver.DefaultType;
        public static string CurrentType => DeviceServices.CurrentAutomationType;
        public static IReadOnlyList<DeviceModel> Devices => DeviceServices.DeviceModels;

        public static string NormalizeType(string? type)
        {
            var automationType = AutomationTypeResolver.Parse(type);
            return AutomationTypeResolver.Normalize(automationType);
        }

        public static async Task RefreshDevices(string type = AutomationTypeResolver.DefaultType)
        {
            await DeviceServices.GetDeviceModels(type);
        }

        public static Task RefreshDevicesAsync(string type = AutomationTypeResolver.DefaultType)
        {
            return DeviceServices.GetDeviceModelsAsync(type);
        }

        public static Task ConnectCheckedDevicesAsync(string? type = null)
        {
            return DeviceServices.Connect(type);
        }

        public static async Task RestartAdbAndRefresh(string? type = null)
        {
            ADBHelper.Restart();
            await DeviceServices.GetDeviceModels(type ?? DeviceServices.CurrentAutomationType);
        }

        public static Task HandleDevicesAsync(List<DeviceModel> devices, EmuAction action, string? apkPath = null)
        {
            return DeviceServices.HandleEmulators(devices, action, apkPath);
        }

        public static async Task<bool> EnsureDeviceEnvironmentAsync(DeviceModel device, string type = AutomationTypeResolver.DefaultType)
        {
            if (device == null || string.IsNullOrWhiteSpace(device.Serial))
            {
                return false;
            }

            AutomationType automationType = AutomationTypeResolver.Parse(type);
            AutomationEnvironmentService.EnsureWindowsReady(automationType);

            ADBClient client = new ADBClient(device);
            return await AutomationEnvironmentService.EnsureDeviceReadyAsync(client, automationType);
        }

        public static async Task<bool> ConnectDeviceAsync(DeviceModel device, string type = AutomationTypeResolver.DefaultType)
        {
            if (device == null || string.IsNullOrWhiteSpace(device.Serial))
            {
                return false;
            }

            AutomationType automationType = AutomationTypeResolver.Parse(type);
            string normalizedType = AutomationTypeResolver.Normalize(automationType);

            AutomationEnvironmentService.EnsureWindowsReady(automationType);
            ADBClient client = new ADBClient(device);
            await AutomationEnvironmentService.EnsureDeviceReadyAsync(client, automationType);
            return client.Connect(normalizedType);
        }
    }
}
