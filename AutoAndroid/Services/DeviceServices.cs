using AutoAndroid;
using AutoAndroid.Monitoring;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Collections.Concurrent;

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
        // Cache cho UpdateDeviceOnlineStatus: tránh check ADB quá thường xuyên
        private static readonly ConcurrentDictionary<string, DateTime> _lastAdbCheck = new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private static readonly TimeSpan ADB_CHECK_CACHE_TTL = TimeSpan.FromSeconds(30);
        // Track số lần failure liên tiếp của service check settings: chỉ đánh dấu "Mất kết nối"
        // khi thất bại >= 2 lần liên tiếp, tránh transient timeout do semaphore bão hòa.
        private static readonly ConcurrentDictionary<string, int> _consecutiveAdbFailures = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private const int MIN_CONSECUTIVE_FAILURES = 2;

        public static async Task GetDeviceModels(string type = AutomationTypeResolver.DefaultType)
        {
           await GetDeviceModelsAsync(type);
        }

        /// <summary>
        /// Phase 1: chỉ gọi "adb devices", tạo DeviceModel minimal (Serial + restore DB state),
        /// populate DeviceModels ngay để UI có thể show grid. Không gọi adb shell nào cả.
        /// Trả về danh sách serial theo thứ tự ADB.
        /// </summary>
        public static List<string> GetSerialsFast(string type = AutomationTypeResolver.DefaultType)
        {
            AutomationType automationType = AutomationTypeResolver.Parse(type);
            CurrentAutomationType = AutomationTypeResolver.Normalize(automationType);

            var lines = ADBHelper.GetDevices();
            var seen = new HashSet<string>();
            var ordered = new List<string>();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || !seen.Add(line)) continue;
                ordered.Add(line);
            }

            lock (_lock)
            {
                DeviceModels.Clear();
                int index = 1;
                foreach (var serial in ordered)
                {
                    DeviceModel model;
                    try { model = DeviceDb.GetBySerial(serial) ?? new DeviceModel { Serial = serial }; }
                    catch { model = new DeviceModel { Serial = serial }; }
                    model.Serial = serial;
                    model.IsLive = false;
                    model.IsAdbOnline = true;
                    model.Index = index++;
                    model.Status = "Đang kết nối...";
                    model.TypeColor = 0;
                    DeviceModels.Add(model);
                }
            }

            return ordered;
        }

        /// <summary>
        /// Phase 2: load detail (name/OS/port/ForcePortrait) + connect ATX cho 1 device.
        /// Gọi song song cho mỗi device SAU KHI grid đã show. Cập nhật model in-place.
        /// </summary>
        public static async Task SetupDeviceAsync(DeviceModel device, string? type = null)
        {
            AutomationType automationType = AutomationTypeResolver.Parse(type ?? CurrentAutomationType);
            string resolvedType = AutomationTypeResolver.Normalize(automationType);

            await Task.Run(() =>
            {
                // Load detail: name, OS, port, ForcePortrait
                try
                {
                    using var socket = new ADBSocket(device.Serial);
                    device.Port = socket.ForwardPort(7912);
                }
                catch { }

                string name = ProcessHelper.RunAdbWithTimeout($"-s {device.Serial} shell settings get global device_name");
                string version = ProcessHelper.RunAdbWithTimeout($"-s {device.Serial} shell getprop ro.build.version.release");
                if (!string.IsNullOrWhiteSpace(name)) device.NameDevice = name;
                if (!string.IsNullOrWhiteSpace(version)) device.OS = version;

                // Check ADB online nhanh
                try
                {
                    string text = ProcessHelper.RunAdbMonitorCommand($"-s {device.Serial} shell service check settings", 5);
                    device.IsAdbOnline = !string.IsNullOrEmpty(text) && !text.Contains("not found");
                }
                catch { device.IsAdbOnline = false; }

                ForcePortrait(device.Serial);
            });

            if (!device.IsAdbOnline)
            {
                device.IsLive = false;
                device.TypeColor = 1;
                device.Status = "Offline";
                device.IsRowEnabled = true;
                return;
            }

            // Connect ATX
            bool atxOk = false;
            try
            {
                try { AutomationEnvironmentService.EnsureWindowsReady(automationType); } catch { }
                var client = new ADBClient(device);
                var connectTask = Task.Run(() =>
                {
                    AutomationEnvironmentService.EnsureDeviceReadyAsync(client, automationType).GetAwaiter().GetResult();
                    return client.Connect(resolvedType);
                });
                var winner = await Task.WhenAny(connectTask, Task.Delay(ATX_CONNECT_TIMEOUT_MS));
                if (winner == connectTask)
                {
                    atxOk = await connectTask;
                    if (!atxOk)
                    {
                        device.IsLive = false;
                        device.TypeColor = 1;
                        device.Status = "Không connect được ATX";
                    }
                }
                else
                {
                    device.IsLive = false;
                    device.TypeColor = 1;
                    device.Status = $"ATX timeout ({ATX_CONNECT_TIMEOUT_MS / 1000}s)";
                }
            }
            catch
            {
                device.IsLive = false;
                device.TypeColor = 1;
                device.Status = "Không connect được ATX";
            }

            device.IsRowEnabled = true;
        }

        public static async Task GetDeviceModelsAsync(string type = AutomationTypeResolver.DefaultType)
        {
            AutomationType automationType = AutomationTypeResolver.Parse(type);
            CurrentAutomationType = AutomationTypeResolver.Normalize(automationType);

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

            await Task.Run(() => CheckAllDevicesAdbOnline());

            await Task.Run(() =>
            {
                lock (_lock)
                {
                    RestoreDeviceState();
                    SaveDeviceState();
                }
            });
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
                        string text = ProcessHelper.RunAdbMonitorCommand(
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
            // Snapshot bên ngoài lock để không giữ lock trong suốt quá trình probe ADB (blocking I/O)
            List<DeviceModel> snapshot;
            lock (_lock) { snapshot = DeviceModels.ToList(); }

            bool anyChanged = false;
            var adbSet = new HashSet<string>(adbSerials);

            var tasks = new List<Task>(snapshot.Count);
            foreach (var dev in snapshot)
            {
                tasks.Add(Task.Run(() =>
                {
                    bool wasAdbOnline = dev.IsAdbOnline;
                    bool nowAdbOnline = false;
                    var devMetrics = DeviceMetricsRegistry.GetOrCreate(dev.Serial);

                    if (adbSet.Contains(dev.Serial))
                    {
                        // Cache: không check ADB nếu đã check trong vòng 30s
                        if (_lastAdbCheck.TryGetValue(dev.Serial, out var lastChecked)
                            && (DateTime.Now - lastChecked) < ADB_CHECK_CACHE_TTL
                            && wasAdbOnline == dev.IsAdbOnline
                            && wasAdbOnline) // chỉ dùng cache khi device đang online
                        {
                            // ATX probe nhanh: nếu port 7912 mở = ATX alive = device thực sự hoạt động
                            // Nhẹ hơn ADB shell rất nhiều (<100ms vs 500ms-5s)
                            bool atxAlive = dev.Port > 0 && DeviceHealthCheckService.PingAtx(dev.Port, 2000);
                            if (atxAlive)
                            {
                                nowAdbOnline = true;
                            }
                            else
                            {
                                // ATX ping fail → fallback ADB service check settings
                                try
                                {
                                    string text = ProcessHelper.RunAdbMonitorCommand(
                                        $"-s {dev.Serial} shell service check settings", 5);
                                    nowAdbOnline = !string.IsNullOrEmpty(text)
                                        && !text.Contains("not found");
                                    _lastAdbCheck[dev.Serial] = DateTime.Now;
                                }
                                catch { }
                            }
                        }
                        else
                        {
                            try
                            {
                                string text = ProcessHelper.RunAdbMonitorCommand(
                                    $"-s {dev.Serial} shell service check settings", 5);
                                nowAdbOnline = !string.IsNullOrEmpty(text)
                                    && !text.Contains("not found");
                                _lastAdbCheck[dev.Serial] = DateTime.Now;
                            }
                            catch { }
                        }
                    }

                    dev.IsAdbOnline = nowAdbOnline;

                    if (wasAdbOnline == nowAdbOnline)
                    {
                        // Reset bộ đếm failure khi device vẫn ổn định
                        if (nowAdbOnline)
                            _consecutiveAdbFailures.TryRemove(dev.Serial, out _);
                        return;
                    }
                    anyChanged = true;
                    devMetrics.StateTransitionCount++;

                    if (wasAdbOnline && !nowAdbOnline)
                    {
                        // Yêu cầu MIN_CONSECUTIVE_FAILURES lần thất bại liên tiếp trước khi
                        // đánh dấu "Mất kết nối" — tránh transient timeout do semaphore bão hòa.
                        int failCount = _consecutiveAdbFailures.AddOrUpdate(dev.Serial, 1, (_, c) => c + 1);
                        if (failCount < MIN_CONSECUTIVE_FAILURES)
                            return;
                        _consecutiveAdbFailures.TryRemove(dev.Serial, out _);
                        dev.IsLive = false;
                        dev.TypeColor = 1;
                        dev.Status = "Mất kết nối";
                        devMetrics.LastError = "ADB timeout/offline";
                        devMetrics.LastErrorTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        devMetrics.LastDisconnectedAt = DateTime.Now;
                        MetricsCollector.Increment("device.state.offline");
                        MetricsCollector.Increment($"device.state.offline.{dev.Serial}");
                    }
                    else if (!wasAdbOnline && nowAdbOnline)
                    {
                        _consecutiveAdbFailures.TryRemove(dev.Serial, out _);
                        if (dev.Status == "Mất kết nối")
                        {
                            dev.Status = "Đã khôi phục kết nối";
                            dev.TypeColor = 2;
                            dev.IsLive = true;
                            devMetrics.LastConnectedAt = DateTime.Now;
                            MetricsCollector.Increment("device.state.online");
                            MetricsCollector.Increment($"device.state.online.{dev.Serial}");
                        }
                        else
                        {
                            dev.TypeColor = 0;
                        }

                        try { ForcePortrait(dev.Serial); } catch { }
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());

            // Thêm device mới phát hiện — lock ngắn khi modify DeviceModels
            var newlyAdded = new List<DeviceModel>();
            lock (_lock)
            {
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
                        newlyAdded.Add(model);
                    }
                    catch { }
                }
            }

            foreach (var dev in newlyAdded)
                DeviceHealthCheckService.StartAsync(dev);
            foreach (var dev in snapshot)
            {
                if (dev.IsAdbOnline && dev.Status == "Đã khôi phục kết nối")
                    DeviceHealthCheckService.StartAsync(dev);
            }

            return anyChanged;
        }

        public static async Task ADBKill()
        {
            ADBHelper.KillServer();
           await GetDeviceModels(CurrentAutomationType);
        }

        /// <summary>Sync version: chỉ kill server, không reload — dùng trước GetSerialsFast().</summary>
        public static void ADBKillSync()
        {
            ADBHelper.KillServer();
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
        /// Failure → IsLive=false (UI disable row).
        /// </summary>
        // Timeout cứng cho bước connect ATX của mỗi device khi user mở tab / bấm Tải lại.
        // Yêu cầu: nếu ~10s mà ATX không lên thì coi như fail và báo lỗi ngay, không
        // để user chờ vô định khi máy bị tắt USB-debug / chưa cài com.github.uiautomator / port chiếm.
        private const int ATX_CONNECT_TIMEOUT_MS = 30_000; // 30s: nếu ATX không lên trong 30s coi như fail

        public static async Task ConnectAll(string? type = null)
        {
            AutomationType automationType = AutomationTypeResolver.Parse(type ?? CurrentAutomationType);
            string resolvedType = AutomationTypeResolver.Normalize(automationType);
            try { AutomationEnvironmentService.EnsureWindowsReady(automationType); } catch { }


            var tasks = new List<Task>(DeviceModels.Count);
            foreach (var device in DeviceModels)
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
                        device.IsRowEnabled = true;
                    }
                }));
            }
            await Task.WhenAll(tasks);

            // Sau khi ATX connect xong, force probe internet cho device ATX alive
            // để HasInternet + IsRowEnabled được cập nhật ngay, không phải đợi
            // DeviceHealthCheckService tự chạy sau.
            foreach (var device in DeviceModels)
            {
                if (device.IsLive && device.Port > 0)
                    DeviceHealthCheckService.StartAsync(device);
            }
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
