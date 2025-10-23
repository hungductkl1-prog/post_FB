using AutoAndroid;
using Sunny.Subdy.Common.Models;

namespace Sunny.Subdy.Common.Services
{
    public class DeviceServices
    {
        public static string Brands = "xiaomi|google|OPPO|Redmi|samsung|vivo|Sharp|ZTE|Sony|Huawei|HTC|Kyocera|HUAWEI|Xiaomi|Nokia|realme|Google|lge|Asus|Verizon|Lenovo|Fujitsu|HONOR|motorola|LGE|YuLong|Micromax|asus|Honor|NEC|micromax|Panasonic|Essential|MetroPCS|VAIO|SHARP|vsmart|FREETEL";
        public static List<DeviceModel> DeviceModels = new List<DeviceModel>();

        public static void GetDeviceModels()
        {
            var lines = ADBHelper.GetDevices();
            DeviceModels.Clear();
            foreach (var line in lines)
            {
                var client = new ADBClient(line);
                if (client?.Device == null) continue;

                client.Device.Index = DeviceModels.Count + 1;
                DeviceModels.Add(client.Device);
            }


        }
        public static void ADBKill()
        {
            ADBHelper.KillServer();
            GetDeviceModels();
        }
        public static void SelectAll()
        {
            DeviceModels.ForEach(device => device.Checked = true);
        }
        public static void UnSelectAll()
        {
            DeviceModels.ForEach(device => device.Checked = false);
        }
        public static async Task Connect()
        {
            List<Task> tasks = new List<Task>();
            foreach (var device in DeviceModels)
            {
                if (!device.Checked || string.IsNullOrEmpty(device.Serial))
                {
                    continue;
                }
                tasks.Add(Task.Run(() => new ADBClient(device).Connect()));
            }
            await Task.WhenAll(tasks);
        }


        public static async Task HandleEmulators(List<DeviceModel> devices, EmuAction action, string? apkPath = null)
        {
            List<Task> tasks = new List<Task>();

            foreach (var device in devices)
            {
                if (!device.Checked || string.IsNullOrEmpty(device.Serial))
                    continue;

                switch (action)
                {
                    case EmuAction.InstallApk:
                        if (!string.IsNullOrEmpty(apkPath))
                        {
                            tasks.Add(Task.Run(() =>
                            {
                                string fileName = Path.GetFileName(apkPath);
                                new ADBClient(device).InstallApp(apkPath);
                                device.Status = $"{fileName} thành công";
                            }));
                        }
                        break;

                    case EmuAction.EnableWifi:
                        tasks.Add(Task.Run(() =>
                        {
                            new ADBClient(device).EnableWifi();
                        }));
                        break;
                    case EmuAction.DisableWifi:
                        tasks.Add(Task.Run(() =>
                        {
                            new ADBClient(device).DisableWifi();
                        }));
                        break;
                    case EmuAction.ConnectWifi:
                        tasks.Add(Task.Run(() =>
                        {
                            device.Status = "Đang kết nối WiFi...";
                            AdbJoinWifiService wifiService = new AdbJoinWifiService(new ADBClient(device));
                            wifiService.ConnectToWifiNetwork(apkPath.Split('|').First(), apkPath.Split('|').Last());
                            device.Status = $"Đã kết nối wifi {apkPath.Split('|').First()}";
                        }));
                        break;
                    case EmuAction.UninstallApp:
                        tasks.Add(Task.Run(() =>
                        {
                            new ADBClient(device).UninstallApp(apkPath);
                        }));
                        break;
                    case EmuAction.Reboot:
                        tasks.Add(Task.Run(() =>
                        {
                            new ADBClient(device).RebootAndWaitForDeviceReady();
                        }));
                        break;
                    case EmuAction.ChangeInfo:
                        tasks.Add(Task.Run(() =>
                        {
                            new ADBClient(device).ChangInfo("", false, "samsung", "Random");
                        }));
                        break;
                    case EmuAction.BackupFB:
                        tasks.Add(Task.Run(() =>
                        {
                            apkPath = apkPath + $"\\{device.Serial}.tar.gz";
                            BackupRestoreHelper apiPhone = new BackupRestoreHelper(device);
                            apiPhone.BackupFacebook(apkPath);
                        }));
                        break;
                    case EmuAction.RestoreFB:
                        tasks.Add(Task.Run(() =>
                        {
                            BackupRestoreHelper apiPhone = new BackupRestoreHelper(device);
                            apiPhone.RestoreFacebook(apkPath);
                        }));
                        break;
                    case EmuAction.BackupIG:
                        tasks.Add(Task.Run(() =>
                        {
                            apkPath = Path.Combine(apkPath, $"{device.Serial}.tar.gz");
                            BackupRestoreHelper apiPhone = new BackupRestoreHelper(device);
                            apiPhone.BackupInstagram(apkPath);
                        }));
                        break;
                    case EmuAction.RestoreIG:
                        tasks.Add(Task.Run(async () =>
                        {
                            BackupRestoreHelper apiPhone = new BackupRestoreHelper(device);
                            await apiPhone.RestoreInstagram(apkPath);
                        }));
                        break;
                    case EmuAction.BackupTikTok:
                        tasks.Add(Task.Run(async () =>
                        {
                            apkPath = apkPath + $"{device.Serial}.tar.gz";
                            BackupRestoreHelper apiPhone = new BackupRestoreHelper(device);
                            await apiPhone.BackupTikTok(apkPath);
                        }));
                        break;
                    case EmuAction.RestoreTikTok:
                        tasks.Add(Task.Run(async () =>
                        {
                            BackupRestoreHelper apiPhone = new BackupRestoreHelper(device);
                            await apiPhone.RestoreTikTok(apkPath);
                        }));
                        break;
                }
            }

            await Task.WhenAll(tasks);
        }
    }
}
