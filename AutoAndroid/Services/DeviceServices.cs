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

            // Toàn bộ phần adb-shell (LoadDeviceInfo: device_name, version, ForwardPort,
            // ForcePortrait — ~6 lệnh shell/máy) chạy trên thread-pool để KHÔNG block UI
            // khi user mở tab Thiết bị / bấm "Tải lại". Mỗi máy được probe song song.
            await Task.Run(() =>
            {
                var lines = ADBHelper.GetDevices();
                var seen = new HashSet<string>();
                var ordered = new List<string>();
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || !seen.Add(line)) continue;
                    ordered.Add(line);
                }

                // Load info song song — mỗi serial 1 task. Giữ thứ tự bằng index trong array.
                var models = new DeviceModel?[ordered.Count];
                Parallel.For(0, ordered.Count, i =>
                {
                    try { models[i] = LoadDeviceInfo(ordered[i]); }
                    catch { models[i] = null; }
                });

                lock (_lock)
                {
                    DeviceModels.Clear();
                    int index = 1;
                    foreach (var m in models)
                    {
                        if (m == null) continue;
                        m.IsLive = false;
                        m.Index = index++;
                        DeviceModels.Add(m);
                    }
                }
            });

            // Check ADB online (parallel, background, outside lock)
            await Task.Run(() => CheckAllDevicesAdbOnline());

            await Task.Run(() =>
            {
                lock (_lock)
                {
                    RestoreDeviceState();
                    SaveDeviceState();
                }
            });

            // Health-check (internet/app/lang/...) KHÔNG fire ở đây nữa — caller (ucManagerDevices)
            // sẽ chạy ConnectAll trước, fail-fast 10s, rồi mới start health-check cho các máy
            // ATX live. Lý do: user muốn thấy lỗi "Không connect được ATX" ngay, không bị status
            // "<OK>Internet|<FAIL>App Fb|..." từ health-check ghi đè trong khi ATX đang treo.
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
        /// Đồng thời force portrait NGAY để scrcpy stream không hiển thị landscape
        /// trước rồi mới xoay lại — tránh "xoay ngang rồi xoay dọc" khó chịu.
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

            ForcePortrait(serial);
            return model;
        }

        /// <summary>
        /// Khoá màn hình về portrait. Best-effort: lệnh nào không hỗ trợ trên ROM
        /// hiện tại sẽ silently fail, không ảnh hưởng các lệnh còn lại.
        /// Gọi 1 lần khi detect device để stream scrcpy không bị flash landscape.
        /// </summary>
        private static void ForcePortrait(string serial)
        {
            try { ProcessHelper.RunAdbWithTimeout($"-s {serial} shell settings put system accelerometer_rotation 0", 5); } catch { }
            try { ProcessHelper.RunAdbWithTimeout($"-s {serial} shell settings put system user_rotation 0", 5); } catch { }
            try { ProcessHelper.RunAdbWithTimeout($"-s {serial} shell content insert --uri content://settings/system --bind name:s:accelerometer_rotation --bind value:i:0", 5); } catch { }
            try { ProcessHelper.RunAdbWithTimeout($"-s {serial} shell content insert --uri content://settings/system --bind name:s:user_rotation --bind value:i:0", 5); } catch { }
            try { ProcessHelper.RunAdbWithTimeout($"-s {serial} shell wm user-rotation lock 0", 5); } catch { }
            try { ProcessHelper.RunAdbWithTimeout($"-s {serial} shell cmd window set-ignore-orientation-request true", 5); } catch { }
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

                            // Device vừa online lại — re-force portrait phòng trường hợp
                            // user xoay ngang khi đang offline hoặc cắm lại USB.
                            try { ForcePortrait(dev.Serial); } catch { }
                        }
                    }));
                }
                Task.WaitAll(tasks.ToArray());

                // Add only truly new devices (serial not in list)
                var existingSerials = new HashSet<string>(DeviceModels.Select(d => d.Serial));
                var newlyAdded = new List<DeviceModel>();
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
                        newlyAdded.Add(model);
                    }
                    catch { }
                }

                // Device vừa được phát hiện online lần đầu / vừa khôi phục kết nối: chạy health-check nền.
                foreach (var dev in newlyAdded)
                {
                    DeviceHealthCheckService.StartAsync(dev);
                }
                foreach (var dev in snapshot)
                {
                    if (dev.IsAdbOnline && dev.Status == "Đã khôi phục kết nối")
                    {
                        DeviceHealthCheckService.StartAsync(dev);
                    }
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

        /// <summary>
        /// Auto-connect ATX cho TẤT CẢ device đang ADB online (không cần checkbox).
        /// Gọi khi LoadDevices xong để cột "Live" hiển thị đầy đủ trạng thái ngay,
        /// đỡ phải bắt user bấm "Kết nối" thủ công chỉ để xem.
        /// Failure → IsLive=false, IsRowEnabled=false (UI disable row).
        /// </summary>
        // Timeout cứng cho bước connect ATX của mỗi device khi user mở tab / bấm Tải lại.
        // Yêu cầu: nếu ~10s mà ATX không lên thì coi như fail và báo lỗi ngay, không
        // để user chờ vô định khi máy bị tắt USB-debug / chưa cài com.github.uiautomator / port chiếm.
        private const int ATX_CONNECT_TIMEOUT_MS = 10_000;

        public static async Task ConnectAll(string? type = null)
        {
            AutomationType automationType = AutomationTypeResolver.Parse(type ?? CurrentAutomationType);
            string resolvedType = AutomationTypeResolver.Normalize(automationType);
            try { AutomationEnvironmentService.EnsureWindowsReady(automationType); } catch { }

            // Snapshot ngay để tránh race khi list bị refresh giữa chừng.
            var targets = DeviceModels.Where(d => d != null
                                              && !string.IsNullOrEmpty(d.Serial)
                                              && d.IsAdbOnline)
                                      .ToList();

            var tasks = new List<Task>(targets.Count);
            foreach (var device in targets)
            {
                tasks.Add(Task.Run(async () =>
                {
                    bool atxOk = false;
                    try
                    {
                        ADBClient client = new ADBClient(device);

                        // Race connect ATX vs timeout 10s. EnsureDeviceReady + Connect là blocking
                        // (nhiều adb shell + http probe), nên đẩy vào Task.Run để có thể abort sớm.
                        var connectTask = Task.Run(() =>
                        {
                            AutomationEnvironmentService.EnsureDeviceReadyAsync(client, automationType)
                                .GetAwaiter().GetResult();
                            return client.Connect(resolvedType);
                        });

                        var winner = await Task.WhenAny(connectTask, Task.Delay(ATX_CONNECT_TIMEOUT_MS));
                        if (winner == connectTask)
                        {
                            // Connect xong trong hạn → lấy kết quả thật.
                            atxOk = await connectTask;
                            // ADBClient.Connect đã set IsLive=true + TypeColor=2 khi thành công.
                            if (!atxOk)
                            {
                                device.IsLive = false;
                                device.TypeColor = 1;
                                device.Status = "Không connect được ATX";
                            }
                        }
                        else
                        {
                            // Timeout — connect vẫn còn chạy nền (best-effort, không cancel được),
                            // nhưng UI báo lỗi luôn để user biết máy này không khả dụng.
                            device.IsLive = false;
                            device.TypeColor = 1;
                            device.Status = $"Không connect được ATX (quá {ATX_CONNECT_TIMEOUT_MS / 1000}s)";
                        }
                    }
                    catch
                    {
                        device.IsLive = false;
                        device.TypeColor = 1;
                        device.Status = "Không connect được ATX";
                    }
                    finally
                    {
                        // Row enabled = vừa có internet vừa ATX live.
                        // HasInternet được DeviceHealthCheckService set (chạy nền song song).
                        device.IsRowEnabled = device.HasInternet && device.IsLive;
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
