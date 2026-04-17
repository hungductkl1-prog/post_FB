using AutoAndroid;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;

namespace Sunny.Subdy.Common.Services
{
    public class DeviceServices
    {
        public static string Brands = "xiaomi|google|OPPO|Redmi|samsung|vivo|Sharp|ZTE|Sony|Huawei|HTC|Kyocera|HUAWEI|Xiaomi|Nokia|realme|Google|lge|Asus|Verizon|Lenovo|Fujitsu|HONOR|motorola|LGE|YuLong|Micromax|asus|Honor|NEC|micromax|Panasonic|Essential|MetroPCS|VAIO|SHARP|vsmart|FREETEL";
        public static List<DeviceModel> DeviceModels = new List<DeviceModel>();
        public static string CurrentAutomationType { get; private set; } = AutomationTypeResolver.DefaultType;
        private static DeviceModelContext? DeviceDbInstance;
        private static DeviceModelContext DeviceDb => DeviceDbInstance ??= new DeviceModelContext();
        private static readonly object _lock = new object();

        public static async Task GetDeviceModels(string type = AutomationTypeResolver.DefaultType)
        {
           await GetDeviceModelsAsync(type);
        }

        public static async Task GetDeviceModelsAsync(string type = AutomationTypeResolver.DefaultType)
        {
            AutomationType automationType = AutomationTypeResolver.Parse(type);
            CurrentAutomationType = AutomationTypeResolver.Normalize(automationType);

            lock (_lock)
            {
                var lines = ADBHelper.GetDevices();
                DeviceModels.Clear();

                var seen = new HashSet<string>();
                int index = 1;
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || !seen.Add(line)) continue;
                    try
                    {
                        var model = LoadDeviceInfo(line);
                        if (model == null) continue;
                        model.IsLive = false;
                        model.Index = index++;
                        DeviceModels.Add(model);
                    }
                    catch { }
                }
            }

            // Check ADB online (parallel, background, outside lock)
            await Task.Run(() => CheckAllDevicesAdbOnline());

            lock (_lock)
            {
                RestoreDeviceState();
                SaveDeviceState();
            }
        }

        /// <summary>
        /// Run 'service check settings' for all devices in parallel.
        /// Sets IsAdbOnline per device.
        /// </summary>
        private static void CheckAllDevicesAdbOnline()
        {
            var tasks = new List<Task>();
            foreach (var device in DeviceModels)
            {
                var dev = device;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        // Dùng NoRetry: nếu thiết bị không phản hồi trong 5s thì offline, không cần retry
                        string text = ProcessHelper.RunAdbNoRetry(
                            $"-s {dev.Serial} shell service check settings", 5);
                        dev.IsAdbOnline = !string.IsNullOrEmpty(text) && !text.Contains("not found");
                    }
                    catch
                    {
                        dev.IsAdbOnline = false;
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// Load device basic info (name, OS, port) WITHOUT connecting ATX/Appium.
        /// </summary>
        private static DeviceModel? LoadDeviceInfo(string serial)
        {
            DeviceModel? model = null;
            try
            {
                model = DeviceDb.GetBySerial(serial);
            }
            catch { }

            if (model == null)
            {
                model = new DeviceModel { Serial = serial };
            }

            try
            {
                using var socket = new ADBSocket(serial);
                model.Port = socket.ForwardPort(7912);
            }
            catch { }

            string name = ProcessHelper.RunAdbWithTimeout($"-s {serial} shell settings get global device_name");
            string version = ProcessHelper.RunAdbWithTimeout($"-s {serial} shell getprop ro.build.version.release");
            model.NameDevice = name;
            model.OS = version;
            model.TypeColor = 0;
            return model;
        }

        /// <summary>
        /// Restore Checked state from SQLite DB so user sees previously selected devices.
        /// </summary>
        public static void RestoreDeviceState()
        {
            try
            {
                var savedDevices = DeviceDb.GetAll();
                LogManager.Info($"[RestoreDeviceState] DB has {savedDevices.Count} devices, memory has {DeviceModels.Count} devices");
                foreach (var device in DeviceModels)
                {
                    var saved = savedDevices.FirstOrDefault(d => d.Serial == device.Serial);
                    if (saved != null)
                    {
                        LogManager.Info($"[RestoreDeviceState] {device.Serial}: DB Checked={saved.Checked}, before={device.Checked}");
                        device.Checked = saved.Checked;
                    }
                    else
                    {
                        LogManager.Info($"[RestoreDeviceState] {device.Serial}: NOT found in DB");
                    }
                }
            }
            catch (Exception ex) { LogManager.Error(ex); }
        }

        /// <summary>
        /// Save all device info to SQLite DB for persistence across sessions.
        /// Uses Serial as unique key to prevent duplicates.
        /// </summary>
        public static void SaveDeviceState()
        {
            try
            {
                var savedDevices = DeviceDb.GetAll();
                // Build lookup by serial (take first if duplicates exist in DB)
                var savedBySerial = new Dictionary<string, DeviceModel>();
                foreach (var s in savedDevices)
                {
                    if (!string.IsNullOrEmpty(s.Serial) && !savedBySerial.ContainsKey(s.Serial))
                        savedBySerial[s.Serial] = s;
                }

                // Clean up DB duplicates first
                var seenSerials = new HashSet<string>();
                foreach (var s in savedDevices)
                {
                    if (string.IsNullOrEmpty(s.Serial) || !seenSerials.Add(s.Serial))
                    {
                        // Duplicate or empty serial - delete
                        try { DeviceDb.DeleteById(s.Id); } catch { }
                    }
                }

                foreach (var device in DeviceModels)
                {
                    if (string.IsNullOrEmpty(device.Serial)) continue;

                    if (savedBySerial.TryGetValue(device.Serial, out var existing))
                    {
                        existing.Checked = device.Checked;
                        existing.NameDevice = device.NameDevice;
                        existing.OS = device.OS;
                        existing.Status = device.Status;
                        existing.State = device.State;
                        existing.Model = device.Model;
                        existing.Port = device.Port;
                        DeviceDb.Update(existing);
                    }
                    else
                    {
                        DeviceDb.Add(device);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Save only the Checked state for all devices (lightweight).
        /// </summary>
        public static void SaveCheckedState()
        {
            try
            {
                LogManager.Info($"[SaveCheckedState] Saving {DeviceModels.Count} devices");
                foreach (var device in DeviceModels)
                {
                    if (string.IsNullOrEmpty(device.Serial)) continue;
                    LogManager.Info($"[SaveCheckedState] {device.Serial}: Checked={device.Checked}");
                    var existing = DeviceDb.GetBySerial(device.Serial);
                    if (existing != null)
                    {
                        existing.Checked = device.Checked;
                        DeviceDb.Update(existing);
                    }
                    else
                    {
                        DeviceDb.Add(device);
                    }
                }
            }
            catch (Exception ex) { LogManager.Error(ex); }
        }

        /// <summary>
        /// Run 'service check settings' per device on background threads (parallel).
        /// Returns true if any device status changed.
        /// MUST be called from background thread, NOT UI thread.
        /// </summary>
        public static bool UpdateDeviceOnlineStatus(List<string> adbSerials)
        {
            lock (_lock)
            {
                bool anyChanged = false;

                // Snapshot current devices to check in parallel
                var snapshot = DeviceModels.ToList();
                var tasks = new List<Task>();

                foreach (var dev in snapshot)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        bool wasAdbOnline = dev.IsAdbOnline;
                        bool nowAdbOnline = false;

                        if (adbSerials.Contains(dev.Serial))
                        {
                            try
                            {
                                // Dùng NoRetry: probe nhanh, không retry để tránh block semaphore
                                string text = ProcessHelper.RunAdbNoRetry(
                                    $"-s {dev.Serial} shell service check settings", 5);
                                nowAdbOnline = !string.IsNullOrEmpty(text)
                                    && !text.Contains("not found");
                            }
                            catch { }
                        }

                        dev.IsAdbOnline = nowAdbOnline;

                        if (wasAdbOnline == nowAdbOnline) return;
                        anyChanged = true;

                        if (wasAdbOnline && !nowAdbOnline)
                        {
                            dev.IsLive = false;
                            dev.TypeColor = 1;
                            dev.Status = "Mất kết nối";
                        }
                        else if (!wasAdbOnline && nowAdbOnline)
                        {
                            if (dev.Status == "Mất kết nối")
                            {
                                dev.Status = "Đã khôi phục kết nối";
                                dev.TypeColor = 2;
                                dev.IsLive = true;
                            }
                            else
                            {
                                dev.TypeColor = 0;
                            }
                        }
                    }));
                }
                Task.WaitAll(tasks.ToArray());

                // Add only truly new devices (serial not in list)
                var existingSerials = new HashSet<string>(DeviceModels.Select(d => d.Serial));
                foreach (var serial in adbSerials)
                {
                    if (string.IsNullOrWhiteSpace(serial) || existingSerials.Contains(serial)) continue;
                    anyChanged = true;
                    try
                    {
                        var model = LoadDeviceInfo(serial);
                        if (model == null) continue;
                        model.IsAdbOnline = true;
                        model.IsLive = false;
                        model.Index = DeviceModels.Count + 1;
                        DeviceModels.Add(model);
                        existingSerials.Add(serial);
                    }
                    catch { }
                }

                return anyChanged;
            }
        }

        public static async Task ADBKill()
        {
            ADBHelper.KillServer();
           await GetDeviceModels(CurrentAutomationType);
        }
        public static void SelectAll()
        {
            DeviceModels.ForEach(device => device.Checked = true);
        }
        public static void UnSelectAll()
        {
            DeviceModels.ForEach(device => device.Checked = false);
        }
        public static async Task Connect(string? type = null)
        {
            AutomationType automationType = AutomationTypeResolver.Parse(type ?? CurrentAutomationType);
            string resolvedType = AutomationTypeResolver.Normalize(automationType);
            AutomationEnvironmentService.EnsureWindowsReady(automationType);

            List<Task> tasks = new List<Task>();
            foreach (var device in DeviceModels)
            {
                if (!device.Checked || string.IsNullOrEmpty(device.Serial))
                {
                    continue;
                }
                tasks.Add(Task.Run(async () =>
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        ADBClient client = new ADBClient(device);
                        await AutomationEnvironmentService.EnsureDeviceReadyAsync(client, automationType);
                        client.Connect(resolvedType);
                        sw.Stop();
                        device.IsLive = true;
                        device.TypeColor = 2; // green
                        device.Status = $"Kết nối thành công [{sw.ElapsedMilliseconds} ms]";
                    }
                    catch
                    {
                        sw.Stop();
                        device.IsLive = false;
                        device.TypeColor = 1; // red
                        device.Status = "Kết nối thất bại";
                    }
                }));
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
                                try
                                {
                                    bool success = new ADBClient(device).InstallApp(apkPath);
                                    device.Status = success ? $"{fileName} thành công" : $"{fileName} thất bại";
                                    device.TypeColor = success ? 0 : 1;
                                }
                                catch (Exception ex)
                                {
                                    device.Status = $"{fileName} lỗi: {ex.Message}";
                                    device.TypeColor = 1;
                                }
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
