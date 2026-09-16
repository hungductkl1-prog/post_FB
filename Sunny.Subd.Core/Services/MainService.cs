using AutoAndroid;
using Sunny.Subd.Core.Facebook;
using Sunny.Subd.Core.Facebook.ScriptActions;
using Sunny.Subd.Core.Instagram;
using Sunny.Subd.Core.Pandora;
using Sunny.Subd.Core.Models;
using Sunny.Subd.Core.Proxies;
using Sunny.Subd.Core.Utils;
using Sunny.Subdy.Common;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Common.Services;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System;
using System.Diagnostics;

namespace Sunny.Subd.Core.Services
{
    public class MainService
    {
        // Các trường dữ liệu private
        public readonly ADBClient _client; // Đối tượng điều khiển thiết bị Android qua ADB
        public Account _account; // Tài khoản đang xử lý
        public readonly ConfigModel _config; // Cấu hình của dịch vụ
        public readonly CancellationToken _ct; // Token để hủy tác vụ
        public readonly IFacebookService _facebookService; // Dịch vụ xử lý Facebook
        public readonly AccountContext _accountContext = new(); // Context để quản lý tài khoản
        public readonly string _platform; // Nền tảng đang sử dụng
        public readonly Stopwatch _stopwatch = new(); // Đồng hồ bấm giờ để theo dõi thời gian
        public int Timeout_Script = 0; // Thời gian tối đa cho thao tác
        public JsonHelper _settingGeneral, _settingScript, _settingScriptAction, _settingJob; // Cấu hình chung từ JSON
        public string _sate = string.Empty; // Trạng thái hiện tại của quá trình
        public Stopwatch _swTotal = new Stopwatch();
        private BackupRestoreHelper _backupRestoreHelper;
        private bool _devicePrepared;
        private bool _facebookPermissionsReady;
        private bool _facebookReinstallAttempted;
        private const int FacebookCrashReinstallThreshold = 3;
        // Constructor khởi tạo dịch vụ
        public MainService(string platform, ADBClient device, ConfigModel config, CancellationToken ct)
        {
            _client = device ?? throw new ArgumentNullException(nameof(device));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));

            _ct = ct;
            if (platform == PlatformModel.Facebook)
            {
                _facebookService = new FacebookService();
            }
            else if (platform == PlatformModel.Instagram || platform == PlatformModel.Threads)
            {
                _facebookService = new InstagramService();
            }
            // Pandora: no FacebookService needed
            _settingJob = config.SettingJob;
            _settingGeneral = config.SettingGeneral;
            _settingScriptAction = config.SettingJob;
            _backupRestoreHelper = new BackupRestoreHelper(_client.Device);

        }

        // Phương thức trì hoãn với thông báo trạng thái
        public async Task DelayMessageAsync(int second, string message, int color)
        {
            for (int i = 1; i <= second; i++)
            {
                int remain = second - i + 1;
                string formattedMessage = message.Replace("{time}", remain.ToString());
                SetStatus($"{formattedMessage}", color);
                await Task.Delay(1000);
            }
        }
        public void SetStatus(string status, int color, string logDetail = null)
        {
            if (!string.IsNullOrEmpty(_sate))
            {
                _client.LogHelper.State = _sate;
            }
            if (_account != null)
            {
                _account.Status = $"[{_sate}] - ({status})";
                _account.RecentInteraction = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                _account.ColorType = color;
            }
            if (_client?.Device != null)
            {
                _client.Device.Status = status;
                _client.Device.TypeColor = color;
            }
            if (!string.IsNullOrEmpty(logDetail))
            {
                _client.LogHelper.SUCCESS(logDetail);
            }
        }
        public async Task Stop()
        {
            if (_ct.IsCancellationRequested)
            {
                throw new OperationCanceledException("Bạn đã dừng tài khoản.");
            }
            if (Timeout_Script != 0 && _stopwatch.IsRunning && _stopwatch.ElapsedMilliseconds > Timeout_Script)
            {
                _stopwatch.Restart();
                SetStatus("Đã quá thời gian thực hiện thao tác, dừng tài khoản.", 1);
                throw new TimeoutException("Đã quá thời gian thực hiện thao tác.");
            }
            await SleepAuto();

        }
        public async Task SleepAuto()
        {
            if (!_settingGeneral.GetBooleanValue("checkBox15", false))
            {
                return;
            }

            DateTime? startDateTime = _settingGeneral.GetValueDateTime("uiTimePicker1");
            DateTime? endDateTime = _settingGeneral.GetValueDateTime("uiTimePicker2");
            if (startDateTime == null || endDateTime == null) return;
            TimeSpan now = DateTime.Now.TimeOfDay;
            TimeSpan startTime = startDateTime.Value.TimeOfDay;
            TimeSpan endTime = endDateTime.Value.TimeOfDay;

            bool inSleepWindow;
            TimeSpan remaining;
            if (startTime <= endTime)
            {
                // Ví dụ: 08:00 - 12:00
                inSleepWindow = now >= startTime && now <= endTime;
                remaining = endTime - now;
            }
            else
            {
                // Qua nửa đêm: ví dụ 23:00 - 06:00
                inSleepWindow = now >= startTime || now <= endTime;
                remaining = now <= endTime
                    ? endTime - now
                    : TimeSpan.FromHours(24) - now + endTime;
            }

            if (inSleepWindow && remaining.TotalSeconds > 0)
            {
                int totalSecondsInt = (int)remaining.TotalSeconds;
                await DelayMessageAsync(totalSecondsInt, "Đã tới giờ nghỉ giải lao, phần mềm sẽ ngủ đông. Đợi {time} giây", 2);
            }
        }
        public async Task ExtractAndUpdateAuthenticationInfoAsync()
        {
            //if (string.IsNullOrEmpty(_account.FullName) && _platform == PlatformModel.Facebook)
            //{
            //    _account.FullName = _client.GetFacebookFullName(_account.Uid);
            //}
            if (!_client.IsRoot()) return;
            _sate = "Lấy thông tin xác thực";
            // "Lưu cookie, token" (checkBox10 của fSettingDefault): khi bỏ tích thì KHÔNG
            // ghi Cookie/Token vào account/DB. Vẫn gọi GetAuthenticationInfo nếu thiếu Uid
            // (uid dùng cho tên file backup / hiển thị), nhưng chỉ gán Uid.
            bool saveCookieToken = _settingGeneral.GetBooleanValue("checkBox10", true);
            bool needAuth = saveCookieToken
                ? (string.IsNullOrEmpty(_account.Cookie) || string.IsNullOrEmpty(_account.Token))
                : string.IsNullOrEmpty(_account.Uid);
            if (needAuth)
            {
                try
                {
                    string value = FacebookHander.GetAuthenticationInfo(_client);
                    if (string.IsNullOrWhiteSpace(value))
                        throw new Exception("Không thể lấy thông tin xác thực.");

                    var parts = value.Split('|');
                    if (parts.Length < 3)
                        throw new Exception("Chuỗi xác thực không hợp lệ.");
                    if (!string.IsNullOrEmpty(parts[0]))
                    {
                        _account.Uid = parts[0];
                        if (saveCookieToken)
                        {
                            _account.Cookie = parts[2];
                            _account.Token = parts[1];
                        }
                    }
                    else
                    {
                        throw new Exception("Chuỗi xác thực không hợp lệ.");
                    }
                }
                catch
                {
                }

            }
            if (string.IsNullOrEmpty(_account.FullName))
            {
                SetStatus("Đang lấy cookie và token...", 2);
                switch (_platform)
                {
                    case PlatformModel.Facebook:
                        {


                            break;
                        }
                    case "Instagram":
                    case PlatformModel.Threads:
                        {
                            Dictionary<string, string> info = await _facebookService.GetInfo(_client);
                            if (info.ContainsKey("username"))
                            {
                                _account.Uid = info["username"];
                            }
                            if (info.ContainsKey("bio"))
                            {
                                _account.Bio = info["bio"];
                            }
                            if (info.ContainsKey("fullname"))
                            {
                                _account.FullName = info["fullname"];
                            }
                            if (info.ContainsKey("following"))
                            {
                                _account.Friends = info["following"];
                            }
                            if (info.ContainsKey("follow"))
                            {
                                _account.Follow = info["follow"];
                            }
                            if (info.ContainsKey("post"))
                            {
                                _account.Groups = info["post"];
                            }

                            break;
                        }
                }
            }

            _sate = "Sao lưu dữ liệu";
            if (_settingGeneral.GetBooleanValue("checkBox3", true))
            {
                SetStatus("Đang sao lưu profile...", 2);
                string profileDir = _settingGeneral.GetValuesFromInputString("textBox3", Path.Combine(AppContext.BaseDirectory, "Backup", "Profile", _platform));
                Directory.CreateDirectory(profileDir);
                string fileProfile = Path.Combine(profileDir, $"{_account.Uid}.tar.gz");
                AutoAndroid.RunHistoryLog.Step(_client.Device?.Serial ?? "?",
                    $"[BACKUP] uid={_account?.Uid} | BackupProfile {_platform} -> {fileProfile} (chỉ tar/cp/pull qua su, KHÔNG reboot)");
                switch (_platform)
                {
                    case PlatformModel.Facebook:
                        {
                            _backupRestoreHelper.BackupFacebook(fileProfile);
                            break;
                        }
                    case "Instagram":
                    case PlatformModel.Threads:
                        {
                            _backupRestoreHelper.BackupInstagram(fileProfile);
                            break;
                        }
                }
                AutoAndroid.RunHistoryLog.Step(_client.Device?.Serial ?? "?",
                    $"[BACKUP] uid={_account?.Uid} | BackupProfile XONG (không có lệnh reboot nào trong backup)");

            }
            SetStatus("Đang sao lưu thiết bị...", 2);
            if (_settingGeneral.GetBooleanValue("checkBox2", true))
            {
                string profileDir = _settingGeneral.GetValuesFromInputString("textBox2", Path.Combine(AppContext.BaseDirectory, "Backup", "Device", _platform));
                Directory.CreateDirectory(profileDir);
                string fileProfile = Path.Combine(profileDir, $"{_account.Uid}.tar.gz");
                AutoAndroid.RunHistoryLog.Step(_client.Device?.Serial ?? "?",
                    $"[BACKUP] uid={_account?.Uid} | BackupDevice -> {fileProfile} (chỉ tar/cp/pull qua su, KHÔNG reboot)");
                await _client.BackupDevice(fileProfile);
                AutoAndroid.RunHistoryLog.Step(_client.Device?.Serial ?? "?",
                    $"[BACKUP] uid={_account?.Uid} | BackupDevice XONG (không có lệnh reboot nào trong backup)");
            }
            SetStatus("Cập nhật trạng thái tài khoản", 2);
            _account.State = "LIVE";
            _accountContext.Update(_account);
        }

        // Thay đổi thông tin thiết bị
        // Luôn chạy trong luồng job: thứ tự đã chốt là change device TRƯỚC change
        // proxy. Không gate theo checkBox1 nữa vì toggle đó bị ẩn trong UI và giá
        // trị lưu có thể false khiến bước này bị bỏ qua im lặng.
        // Kết quả bước đổi thiết bị của account hiện tại. Ô trạng thái trên UI chỉ giữ
        // dòng CUỐI CÙNG (bước đổi proxy ghi đè mất dòng đổi thiết bị), nên lưu lại để
        // nhúng vào dòng trạng thái của bước đổi proxy — đảm bảo người dùng luôn thấy
        // bước đổi thiết bị đã chạy (và chạy trước đổi proxy).
        private string _lastDeviceChange = string.Empty;

        private async Task ChangeInfoAsync()
        {
            _sate = "Thay đổi thông tin thiết bị";
            AutoAndroid.DeviceChangeLog.Write(_client.Device?.Serial ?? "?",
                $"[{AutoAndroid.DeviceChangeLog.BuildTag}] ChangeInfoAsync: BẮT ĐẦU (uid={_account?.Uid}).");
            _client.LogHelper.SUCCESS(">>> BƯỚC ĐỔI THIẾT BỊ (chạy trước đổi proxy)");
            _client.LogHelper.State = _sate;
            try
            {
                SetStatus("Đang thay đổi thông tin thiết bị...", 2);
                string filezip = string.Empty;
                List<string> brands = _settingGeneral.GetValuesFromInputString("textBox1", DeviceServices.Brands).Split('|').ToList();
                bool backup = _settingGeneral.GetBooleanValue("checkBox2", true);
                if (backup)
                {
                    string profileDir = _settingGeneral.GetValuesFromInputString("textBox2", Path.Combine(AppContext.BaseDirectory, "Backup", "Device", _platform));
                    profileDir = Path.Combine(profileDir);
                    Directory.CreateDirectory(profileDir);
                    filezip = Path.Combine(profileDir, $"{_account.Uid}.tar.gz");
                }
                if (await _client.ChangInfo(filezip, backup, "", "VN"))
                {
                    _lastDeviceChange = $"Thiết bị OK [{_client.GetDeviceName()}]";
                    SetStatus($"Thành công. [{_client.GetDeviceName()}]", 2);
                }
                else
                {
                    _lastDeviceChange = $"Thiết bị LỖI [{_client.GetDeviceName()}]";
                    SetStatus($"Thất bại. [{_client.GetDeviceName()}]", 1);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                _lastDeviceChange = $"Thiết bị LỖI NGOẠI LỆ: {ex.Message}";
                SetStatus(ex.Message, 1);
            }
        }

        // Thay đổi proxy
        private async Task<bool> ChangeProxyAsync()
        {
            _sate = "Thay đổi IP/Proxy";
            SetStatus("Đang thay đổi IP/Proxy...", 2);
            _client.Shell("settings", "put", "global", "http_proxy", ":0");
            _client.StopApp(VATProxyService.Package_Proxy);
            string proxy = string.Empty;
            var proxyType = GetProxyType();
            SetStatus($"Loại: [{proxyType}] - ", 2);
            switch (proxyType)
            {
                case ProxyService.NoIP:
                    return true;
                case ProxyService.Mobile4G:
                    await HandleMobile4GProxyAsync();
                    break;
                case ProxyService.KiotProxy:
                    proxy = await GetProxyFromServiceAsync(ProxyKiot.NewProxy, ProxyKiot.GetProxy);
                    break;
                case ProxyService.WWProxy:
                    proxy = await GetProxyFromServiceAsync(ProxyWWW.NewProxy, ProxyWWW.GetProxy);
                    break;
                case ProxyService.ProxyMart:
                    proxy = await GetProxyFromServiceAsync(ProxyMart.NewProxy, ProxyMart.GetProxy);
                    break;
                case ProxyService.CustomProxy:
                    proxy = await GetCustomProxyAsync();
                    break;
                case ProxyService.ProxyAssigned:
                    proxy = _account?.Proxy;
                    break;
            }

            SetStatus($"Loại: [{proxyType}] - {_lastDeviceChange} - proxy đã nhận", 2);
            if (proxyType == ProxyService.Mobile4G)
            {
                _client.DisablePlane();
                _client.Enabel4G();
                await DelayMessageAsync(5, "Đợi {time} giây kết nối 4G.", 2);
                return true;
            }

            if (string.IsNullOrWhiteSpace(proxy))
            {
                // Nguồn proxy được cấu hình không trả về gì (hết key, API lỗi...). Trước khi
                // bỏ qua tài khoản, thử proxy ĐÃ GÁN cho chính tài khoản đó trong DB — đúng
                // cách bản cũ (loại "Proxy đã gán") vẫn chạy — để job không chết im mà vẫn
                // đi đủ quy trình: đổi thiết bị -> đổi proxy -> restore -> đăng nhập.
                string assigned = _account?.Proxy;
                if (!string.IsNullOrWhiteSpace(assigned))
                {
                    proxy = assigned;
                    SetStatus($"Nguồn [{proxyType}] trống, dùng proxy đã gán cho tài khoản: {ProxyService.Mask(proxy)}.", 2);
                    AutoAndroid.DeviceChangeLog.Write(_client.Device?.Serial ?? "?",
                        $"ChangeProxyAsync: nguồn [{proxyType}] KHÔNG trả proxy -> fallback proxy đã gán ({ProxyService.Mask(proxy)}).");
                }
            }

            if (string.IsNullOrWhiteSpace(proxy))
            {
                SetStatus("Không lấy được proxy, bỏ qua tài khoản.", 1);
                return false;
            }

            // Không kiểm tra danh sách package ở đây (_client.AppList() đi qua Shell
            // với vòng retry + Connect lại đầy đủ, là điểm treo im nhiều phút khi
            // adb/device chập chờn). Kết nối proxy chỉ dựa vào xác nhận "successful"
            // từ chính broadcast VAT; thất bại thì bỏ qua tài khoản.
            if (!_client.ConnectProxyPreinstalled(proxy))
            {
                SetStatus($"Kết nối proxy thất bại ({_lastDeviceChange}), bỏ qua tài khoản.", 1);
                return false;
            }

            int timeDelay = _settingGeneral.GetIntType("numericUpDown3", 10);
            if (timeDelay > 0)
                await DelayMessageAsync(timeDelay, "Đợi {time} giây kết nối.", 2);
            return true;
        }

        // Lấy loại proxy từ cấu hình
        private string GetProxyType()
        {
            try
            {
                int index = _settingGeneral.GetIntType("cbb_ListTypeProxy", 0);
                return index >= 0 ? ProxyService.ProxyTypes[index] : ProxyService.NoIP;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                SetStatus(ex.Message, 1);
                return ProxyService.NoIP;
            }
        }

        // Xử lý proxy 4G
        private async Task HandleMobile4GProxyAsync()
        {
            _client.EnabePlane();
            _client.Disable4G();
        }

        // Lấy proxy từ dịch vụ
        private async Task<string> GetProxyFromServiceAsync(Func<string, Task<string>> newProxyFunc, Func<string, Task<string>> getProxyFunc)
        {
            string key = ProxyService.GetProxy();
            // Không có key (txtLines trống) thì trả về ngay: gọi API với key rỗng chỉ
            // đốt thời gian chờ rồi vẫn nhận chuỗi rỗng, và lỗi bị nuốt im trong provider.
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            return await newProxyFunc(key) ?? await getProxyFunc(key);
        }

        // Lấy proxy tùy chỉnh
        private async Task<string> GetCustomProxyAsync()
        {
            string line = ProxyService.GetProxy();
            if (string.IsNullOrEmpty(line)) return string.Empty;
            var parts = line.Split('|');
            string proxy = parts[0].Trim();
            string link = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            if (!string.IsNullOrEmpty(link)) await RequestService.Get(link);
            return proxy;
        }

        // Kiểm tra kết nối internet
        private async Task<bool> IsInternetAsync()
        {
            _sate = "Kiểm tra kết nối internet";
            int attempts = _settingGeneral.GetIntType("nud_IndexFailProxy", 5);
            for (int i = 1; i <= attempts; i++)
            {
                string ip = await _client.GetIp(_sate);
                if (string.IsNullOrEmpty(ip))
                {
                    SetStatus($"[{i}/{attempts}] Không có internet.", 1);
                    //continue;
                    await _client.DisableWifi();
                    _client.Delay(5);
                    await _client.EnableWifi();
                    _client.Delay(10);
                    continue;
                }
                SetStatus($"[{i}/{attempts}] IP:[{ip}].", 2);
                if (_account != null)
                {
                    _account.IP = ip;
                    _account.Serial = $"[{_client.Device.NameDevice} - {_client.Device.Serial}]";
                }
                return true;
            }
            return false;
        }

        // Mở ứng dụng Facebook
        private async Task<bool> OpenFacebookAsync()
        {
            _sate = $"Mở ứng dụng {_platform}";
            string package = FacebookHander.Package(_platform);
            int crashCount = 0;
            for (int attempt = 1; attempt <= 10; attempt++)
            {
                SetStatus($"[{attempt}/10] Đang khởi động ứng dụng {_platform}...", 2);
                _client.AppStart(package, true, true, true);
                if (_client.AppWait(package)) return true;

                bool crashed = _client.ElementWithAttributes($"//*[@text=\"{_platform} keeps stopping\"]", 3, click: false);
                if (!crashed) continue;

                _client.ElementWithAttributes("//*[@text=\"Close app\"]");
                crashCount++;
                SetStatus($"[{crashCount}/{FacebookCrashReinstallThreshold}] Facebook crash; thử mở lại, chưa cài lại.", 1);

                if (crashCount < FacebookCrashReinstallThreshold || _facebookReinstallAttempted)
                    continue;

                _facebookReinstallAttempted = true;
                SetStatus("Facebook crash nhiều lần, đang cài lại APK...", 1);
                if (!ReinstallFacebook()) return false;
                if (!await PrepareDeviceOnceAsync()) return false;
                crashCount = 0;
            }

            return _client.AppWait(package);
        }

        // Kết nối và chuẩn bị thiết bị.
        // Trả về (ok, noInternet). noInternet=true khi fail VÌ thiết bị không lên được mạng
        // (caller dùng để đếm chu kỳ liên tiếp và bail out nếu quá ngưỡng). false nếu fail
        // vì lý do khác (không connect được adb, v.v.) hoặc khi thành công.
        public async Task<(bool ok, bool noInternet)> ConnectAndPrepareDeviceAsync(bool changeProxy)
        {
            if (!await PrepareDeviceOnceAsync())
            {
                _client.Device.IsLive = false;
                _client.Running = false;
                return (false, false);
            }

            if (changeProxy)
            {
                await ChangeInfoAsync();
                if (!await ChangeProxyAsync())
                    return (false, false);
            }

            return (true, false);
        }

        // Đọc wifi-credentials.json theo serial, dùng adb-join-wifi để thiết bị
        // tự kết nối vào ssid/pass người dùng đã cấu hình. Trả về true nếu đã
        // gửi lệnh thành công (chưa xác minh internet — caller sẽ IsInternetAsync lại).
        private async Task<bool> TryJoinConfiguredWifiAsync()
        {
            try
            {
                var serial = _client.Device?.Serial;
                if (string.IsNullOrEmpty(serial)) return false;
                var cred = WifiCredentialsStore.GetBySerial(serial);
                if (cred == null || string.IsNullOrEmpty(cred.UserName)) return false;

                SetStatus($"Kết nối lại Wifi '{cred.UserName}'…", 2);
                var wifi = new AdbJoinWifiService(_client);
                bool sent = await wifi.ConnectToWifiNetwork(cred.UserName, cred.Password);
                if (!sent) return false;

                // Cho thiết bị vài giây để DHCP cấp IP và app join wifi hoàn tất.
                await Task.Delay(5000);
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Warning($"[TryJoinConfiguredWifi] {ex.Message}");
                return false;
            }
        }

        // Kết nối thiết bị
        private async Task<bool> ConnectDeviceAsync()
        {
            _sate = "Kết nối thiết bị";
            return _client.Connect();
        }
        private bool ReinstallFacebook()
        {
            string fileAPK = string.Empty;
            if (_settingGeneral.GetBooleanValue("checkBox8", true))
            {
                fileAPK = _settingGeneral.GetValuesFromInputString("textBox4", FacebookHander.FilePath(_platform));
            }
            if (string.IsNullOrEmpty(fileAPK))
            {
                fileAPK = FacebookHander.FilePath(_platform);
            }

            if (!File.Exists(fileAPK))
            {
                SetStatus($"Không tìm thấy APK Facebook để khôi phục: [{fileAPK}]", 1);
                return false;
            }

            _client.StopApp(FacebookHander.Package(_platform));
            if (!_client.UninstallApp(FacebookHander.Package(_platform)))
            {
                SetStatus("Không thể gỡ Facebook để khôi phục sau nhiều lần crash.", 1);
                return false;
            }

            if (!_client.InstallApp(fileAPK))
            {
                SetStatus("Không thể cài lại Facebook sau nhiều lần crash.", 1);
                return false;
            }

            _facebookPermissionsReady = false;
            _devicePrepared = false;
            return true;
        }

        // Theo yêu cầu: bỏ qua hoàn toàn bước kiểm tra package (Facebook/VAT Proxy)
        // khi khởi động worker. Mọi truy vấn danh sách package qua adb đều là điểm
        // treo im khi adb/device chập chờn, nên không đọc gì ở đây. Thiếu package
        // sẽ tự lộ ra ở bước thao tác thực tế (xóa dữ liệu / broadcast proxy thất
        // bại) và dừng device ở đó.
        private Task<bool> PrepareDeviceOnceAsync()
        {
            _devicePrepared = true;
            return Task.FromResult(true);
        }

        // Chuẩn bị thiết bị. Hàm này chỉ giữ tương thích cho các luồng cũ;
        // cleanup dữ liệu tài khoản được thực hiện riêng trong RunAsync.
        private async Task PrepareDeviceAsync(bool isContainNoDelete = true)
        {
            if (!await PrepareDeviceOnceAsync())
            {
                _client.Device.IsLive = false;
                _client.Running = false;
            }
        }

        private async Task<bool> ClearPreviousAccountDataAsync()
        {
            if (_platform != PlatformModel.Facebook) return true;

            _sate = "Xóa dữ liệu tài khoản Facebook cũ";
            SetStatus("Đang xóa toàn bộ phiên Facebook cũ...", 2);
            try
            {
                _client.ClearFacebookData();
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                SetStatus("Không thể xác nhận đã xóa phiên Facebook cũ; dừng thiết bị.", 1);
                _client.Device.IsLive = false;
                _client.Running = false;
                return false;
            }
        }

        // Kiểm tra trạng thái tài khoản
        private async Task<bool> CheckLiveAsync()
        {
            if (!_settingGeneral.GetBooleanValue("checkBox9", true)) return true;
            _sate = "Kiểm tra tài khoản còn sống";
            bool check = false;
            switch (_platform)
            {
                case PlatformModel.Facebook:
                    {
                        check = await FacebookRequest.CheckLive(_account.Uid);
                        break;
                    }
                case "Instagram":
                case PlatformModel.Threads:
                    {
                        var value = await InstagramRequest.GetInfo(_account.Uid);
                        if (!value.ContainsKey("error"))
                        {
                            _account.Bio = value["bio"];
                            _account.Friends = value["following"];
                            _account.FullName = value["fullname"];
                            check = true;
                        }
                        break;
                    }
                case PlatformModel.Pandora:
                    {
                        // Pandora xác thực khi login app — bỏ qua check live HTTP
                        check = true;
                        break;
                    }
            }
            if (check)
            {
                _account.State = "LIVE";
                SetStatus($"Tài khoản {_platform}: LIVE", 2);
            }
            else
            {
                _account.State = "DIE";
                SetStatus($"Tài khoản {_platform}: DIE", 1);
                throw new SubdyExtension(SubdyEnum.DIE, $"Tài khoản {_platform}: DIE");
            }
            return check;
        }
        public async Task SessionExpired()
        {
            _sate = "Phiên đăng nhập hết hạn";
            SetStatus("Đang xóa profile cũ và đăng nhập lại...", 2);
            string filezip = string.Empty;
            string profileDir = _settingGeneral.GetValuesFromInputString("textBox3", Path.Combine(AppContext.BaseDirectory, "Backup", "Profile", _platform));
            profileDir = Path.Combine(profileDir);
            Directory.CreateDirectory(profileDir);
            filezip = Path.Combine(profileDir, $"{_account.Uid}.tar.gz");
            if (File.Exists(filezip))
            {
                File.Delete(filezip);
            }
            await ClearPreviousAccountDataAsync();
            if (!await PrepareDeviceOnceAsync())
            {
                _client.Device.IsLive = false;
                _client.Running = false;
                return;
            }
        }
        // Khôi phục dữ liệu Facebook
        public async Task<bool> RestoreFacebookAsync()
        {
            string filezip = string.Empty;
            string profileDir = _settingGeneral.GetValuesFromInputString("textBox3", Path.Combine(AppContext.BaseDirectory, "Backup", "Profile", _platform));
            profileDir = Path.Combine(profileDir);
            Directory.CreateDirectory(profileDir);
            filezip = Path.Combine(profileDir, $"{_account.Uid}.tar.gz");
            _sate = $"Khôi phục dữ liệu {_platform}";
            try
            {
                SetStatus($"Đang khôi phục dữ liệu {_platform}...", 2);
                switch (_platform)
                {
                    case PlatformModel.Facebook:
                        {
                            if (FacebookHander.GetAuthenticationInfo(_client).Contains(_account.Uid))
                            {
                                SetStatus("Tài khoản đã tồn tại trên thiết bị, không cần restore.", 2);
                                break;
                            }
                            _backupRestoreHelper.RestoreFacebook(filezip);
                            break;
                        }
                    case "Instagram":
                    case PlatformModel.Threads:
                        {
                            await _backupRestoreHelper.RestoreInstagram(filezip);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                SetStatus(ex.Message, 1);
            }
            return await OpenFacebookAsync();
        }

        // Xử lý các trường hợp ngoại lệ
        private void HanderCase(Exception ex)
        {
            SubdyExtension subdyExtension = ex as SubdyExtension ?? new SubdyExtension(SubdyEnum.Error, ex.Message);
            switch (subdyExtension.SubdyEnum)
            {
                case SubdyEnum.Stop:
                    _account.Status = string.IsNullOrEmpty(subdyExtension.Message)
                        ? "Đã dừng lại."
                        : "Đã dừng: " + subdyExtension.Message;
                    break;
                case SubdyEnum.Error:
                    _account.Status = "Lỗi: " + subdyExtension.Message;
                    break;
                case SubdyEnum.CP_282:
                    _account.Status = string.IsNullOrEmpty(subdyExtension.Message)
                        ? "Tài khoản bị checkpoint 282."
                        : (subdyExtension.Message.Contains("checkpoint", StringComparison.OrdinalIgnoreCase) || subdyExtension.Message.Contains("282")
                            ? subdyExtension.Message
                            : "Lỗi CP_282: " + subdyExtension.Message);
                    _account.State = "CP_282";
                    _account.ColorType = 1;
                    break;
                case SubdyEnum.CP_956:
                    _account.Status = string.IsNullOrEmpty(subdyExtension.Message)
                        ? "Tài khoản bị checkpoint 956."
                        : (subdyExtension.Message.Contains("checkpoint", StringComparison.OrdinalIgnoreCase) || subdyExtension.Message.Contains("956")
                            ? subdyExtension.Message
                            : "Lỗi CP_956: " + subdyExtension.Message);
                    _account.State = "CP_956";
                    _account.ColorType = 1;
                    break;
                case SubdyEnum.LogOut:
                    _account.Status = string.IsNullOrEmpty(subdyExtension.Message)
                        ? "Tài khoản bị đăng xuất."
                        : (subdyExtension.Message.Contains("đăng xuất", StringComparison.OrdinalIgnoreCase)
                            ? subdyExtension.Message
                            : "Đăng xuất: " + subdyExtension.Message);
                    _account.State = "Logout";
                    _account.ColorType = 1;
                    break;
                case SubdyEnum.Captcha:
                    _account.Status = string.IsNullOrEmpty(subdyExtension.Message)
                        ? "Tài khoản bị yêu cầu captcha."
                        : (subdyExtension.Message.Contains("captcha", StringComparison.OrdinalIgnoreCase)
                            ? subdyExtension.Message
                            : "Captcha: " + subdyExtension.Message);
                    _account.State = "Captcha";
                    _account.ColorType = 1;
                    break;
                case SubdyEnum.Block:
                    _account.Status = string.IsNullOrEmpty(subdyExtension.Message)
                        ? "Tài khoản bị chặn."
                        : (subdyExtension.Message.Contains("bị chặn", StringComparison.OrdinalIgnoreCase)
                            ? subdyExtension.Message
                            : "Tài khoản bị chặn: " + subdyExtension.Message);
                    _account.State = "Block";
                    _account.ColorType = 1;
                    break;
                case SubdyEnum.DIE:
                    _account.Status = subdyExtension.Message;
                    _account.State = "DIE";
                    _account.ColorType = 1;
                    break;
                case SubdyEnum.No_Internet:
                    _account.Status = string.IsNullOrEmpty(subdyExtension.Message)
                        ? "Thiết bị mất kết nối internet."
                        : (subdyExtension.Message.Contains("internet", StringComparison.OrdinalIgnoreCase)
                            ? subdyExtension.Message
                            : "Mất internet: " + subdyExtension.Message);
                    _account.State = "No_Internet";
                    _account.ColorType = 1;
                    break;
                case SubdyEnum.Success:
                    _account.Status = string.IsNullOrEmpty(subdyExtension.Message)
                        ? "Đã chạy ✔"
                        : subdyExtension.Message;
                    _account.ColorType = 2;
                    break;
                case SubdyEnum.JobFail:
                    _account.Status = string.IsNullOrEmpty(subdyExtension.Message)
                        ? "Thực hiện hành động thất bại."
                        : (subdyExtension.Message.Contains("thất bại", StringComparison.OrdinalIgnoreCase) || subdyExtension.Message.StartsWith("Không tìm thấy"))
                            ? subdyExtension.Message
                            : "Thất bại: " + subdyExtension.Message;
                    _account.ColorType = 1;
                    break;
            }
            new AccountContext().Update(_account);
        }

        // Bug 1: Phát hiện và giải phóng acc bị lag/đơ (Running=true quá 60 phút)
        private void AutoRecoverStuckAccounts()
        {
            int stuckMinutes = _settingGeneral.GetIntType("numericUpDown_StuckTimeout", 60);
            if (stuckMinutes <= 0) stuckMinutes = 60;

            var context = new AccountContext();
            var allAccounts = context.GetAll("SELECT * FROM Account");
            var now = DateTime.Now;
            var toRecover = new List<Account>();

            foreach (var acc in allAccounts)
            {
                if (!acc.Running) continue;
                if (string.IsNullOrEmpty(acc.RecentInteraction)) continue;
                if (!DateTime.TryParse(acc.RecentInteraction, out DateTime lastTime)) continue;

                double minutesPassed = (now - lastTime).TotalMinutes;
                if (minutesPassed >= stuckMinutes)
                {
                    acc.Running = false;
                    acc.Status = $"[Lag/Đơ] Tự động giải phóng sau {stuckMinutes} phút không hoạt động.";
                    acc.ColorType = 1;
                    toRecover.Add(acc);
                }
            }

            if (!toRecover.Any()) return;
            context.Update(toRecover);
            _client.LogHelper.SUCCESS($"Đã giải phóng {toRecover.Count} tài khoản bị lag/đơ.");
        }

        private bool IsReboot()
        {
            bool isReboot = _settingGeneral.GetBooleanValue("checkBox5", false);
            if (!isReboot) return false;
            double timeout = _settingGeneral.GetIntType("numericUpDown2", 30) * 60000;
            if (timeout != 0 && _swTotal.IsRunning && _swTotal.ElapsedMilliseconds > timeout)
            {
                _sate = "Kiểm tra tự động khởi động lại";
                SetStatus($"Tự reboot sau {_settingGeneral.GetIntType("numericUpDown2", 30)} phút.", 2);
                _client.RebootAndWaitForDeviceReady();
                _stopwatch.Restart();
                return isReboot;
            }
            return false;
        }

        // Bug 3: Tự động reset trạng thái - chạy định kỳ, đưa acc đã reset về queue
        private void AutoResetAccountStates()
        {
            if (!_settingGeneral.GetBooleanValue("checkBox17", false)) return;

            int hoursFrom = _settingGeneral.GetIntType("numericUpDown6", 12);
            int hoursTo = _settingGeneral.GetIntType("numericUpDown5", 24);

            var context = new AccountContext();
            var allAccounts = context.GetAll("SELECT * FROM Account");
            var now = DateTime.Now;
            var toReset = new List<Account>();

            foreach (var acc in allAccounts)
            {
                if (string.IsNullOrEmpty(acc.State)) continue;
                if (acc.Running) continue;
                if (string.IsNullOrEmpty(acc.RecentInteraction)) continue;
                if (!DateTime.TryParse(acc.RecentInteraction, out DateTime lastTime)) continue;

                double hoursPassed = (now - lastTime).TotalHours;
                int threshold = SubdyHelper.RandomValue(hoursFrom, hoursTo);
                if (hoursPassed >= threshold)
                {
                    acc.State = "";
                    toReset.Add(acc);
                }
            }

            if (!toReset.Any()) return;
            context.Update(toReset);
        }

        public async Task RunAsync()
        {
            _swTotal.Start();
            if (!await PrepareDeviceOnceAsync())
            {
                _stopwatch.Stop();
                return;
            }

            while (!_ct.IsCancellationRequested)
            {
                _account = null;
                if (!AccountServices.Accounts.Any())
                {
                    _client.LogHelper.SUCCESS("Đã hoàn thành!");
                    break;
                }

                // Chỉ cleanup session/account state theo từng account; không cài lại APK,
                // không push lại sqlite3 và không cấp quyền lặp lại.
                AutoResetAccountStates();
                AutoRecoverStuckAccounts();
                _sate = "Chuẩn bị tài khoản";
                _account = AccountServices.GetAccount();

                if (_account == null) continue;
                _account.Running = true;
                try
                {
                    AutoAndroid.DeviceChangeLog.Write(_client.Device?.Serial ?? "?",
                        $"[{AutoAndroid.DeviceChangeLog.BuildTag}] RunAsync: vào vòng account uid={_account?.Uid} (thứ tự: xóa dữ liệu -> đổi thiết bị -> đổi proxy).");

                    // ── CHẶN MÀN HÌNH ĐEN SUỐT VÒNG ACCOUNT (v12) ──────────────────────────
                    // Trước đây mirror-guard + recovery chỉ sống bên trong Change() (vài giây
                    // đổi thiết bị) rồi Stop, NHƯNG đen màn hình lại xảy ra SAU đó — đúng lúc
                    // MỞ Facebook (createSurface) khi VirtualDisplay mồ côi còn sót / TaskRecord
                    // rò rỉ tích lũy. v12 giữ mirror-guard chạy NỀN SUỐT account (đổi thiết bị
                    // -> proxy -> mở FB) để không còn khoảng hở. Guard KHÔNG kill system_server
                    // nên KHÔNG rớt tun0/VPN — an toàn tuyệt đối cho quy trình FB.
                    _client.MirrorGuardStart();

                    // Recovery leak (DẠNG 2) ở ĐẦU account = điểm VPN-safe: proxy account này
                    // CHƯA nối, proxy account trước sắp bị thay ở bước 3, nên nếu phải kill
                    // system_server cũng không mất VPN đang cần. Bắt sớm leak tích lũy từ
                    // farming của account trước (đo live: leak TỰ TĂNG cả khi tool tắt).
                    // Gated: framework khỏe thì trả về ngay (~1s), không can thiệp.
                    try { _client.maxChange?.RecoverFrameworkIfBlackScreenPublic(); }
                    catch (Exception exR)
                    {
                        AutoAndroid.DeviceChangeLog.Write(_client.Device?.Serial ?? "?",
                            $"[{AutoAndroid.DeviceChangeLog.BuildTag}] RunAsync: recovery đầu account lỗi (bỏ qua): {exR.Message}");
                    }

                    // 1. Dọn sạch cache/session/account Facebook cũ.
                    AutoAndroid.RunHistoryLog.Step(_client.Device?.Serial ?? "?",
                        $"[FARM] uid={_account?.Uid} | bước 1/6: ClearPreviousAccountDataAsync (xóa cache/session/account FB cũ)");
                    if (!await ClearPreviousAccountDataAsync()) break;

                    // 2. Thay đổi thông tin thiết bị sau khi đã dọn sạch phiên cũ.
                    AutoAndroid.RunHistoryLog.Step(_client.Device?.Serial ?? "?",
                        $"[FARM] uid={_account?.Uid} | bước 2/6: ChangeInfoAsync (đổi thông tin thiết bị)");
                    await ChangeInfoAsync();

                    // 3. Kết nối proxy qua VAT Proxy bằng broadcast, không mở giao diện.
                    AutoAndroid.RunHistoryLog.Step(_client.Device?.Serial ?? "?",
                        $"[FARM] uid={_account?.Uid} | bước 3/6: ChangeProxyAsync (đổi IP/proxy VAT)");
                    if (!await ChangeProxyAsync())
                    {
                        if (!_client.Running) break;
                        continue;
                    }

                    _sate = "Thực hiện kịch bản";
                    _config.JobService = "https://app.golike.net/";
                    FacebookFarming farming = new FacebookFarming(this);

                    // ── BỎ QUA ĐĂNG NHẬP QUA APP CHO KỊCH BẢN "KHÁNG SPAM" (gỡ checkpoint 282) ──
                    // Tài khoản trong kịch bản này ĐANG bị 282. Nếu mở app Facebook rồi Login(),
                    // Login() khớp nhóm xpath CP282 và NÉM SubdyExtension(CP_282)
                    // (FacebookService.cs:111-114) -> ngoại lệ thoát ra try của vòng account ->
                    // HanderCase gán State="CP_282" rồi `continue` sang TÀI KHOẢN KHÁC ->
                    // farming.ExecuteAsync() KHÔNG BAO GIỜ chạy -> HDKhangSpam không bao giờ
                    // inject cookie / mở Chrome. (Live 2026-09-15: "tool không chuyển sang chrome
                    // để login cookie fb, mà chỉ login bằng facebook app rồi dừng".)
                    // HDKhangSpam tự đăng nhập bằng cookie của _account TRONG Chrome, nên bước
                    // restore+login qua app là vừa thừa vừa chặn đường chạy.
                    if (farming.ScriptHasKhangSpam())
                    {
                        _sate = "Kháng spam 282 (bỏ qua đăng nhập app)";
                        SetStatus("Kịch bản kháng spam: bỏ qua đăng nhập app Facebook, sẽ login bằng cookie trong Chrome.", 2);
                        AutoAndroid.DeviceChangeLog.Write(_client.Device?.Serial ?? "?",
                            $"[{AutoAndroid.DeviceChangeLog.BuildTag}] RunAsync: script có HDKhangSpam -> SKIP RestoreFacebook+Login (uid={_account?.Uid}), vào thẳng farming.");
                    }
                    else
                    {
                        // 4. Restore và mở Facebook.
                        AutoAndroid.RunHistoryLog.Step(_client.Device?.Serial ?? "?",
                            $"[FARM] uid={_account?.Uid} | bước 4/6: RestoreFacebookAsync (load file backup profile acc)");
                        if (!await RestoreFacebookAsync()) continue;

                        int index = _settingGeneral.GetIntType("comboBox1", 0);
                        _account.Uid_Email = index == 1 ? _account.Email : _account.Uid;
                        _account.Uid_Email ??= _account.Email ?? _account.Uid;

                        AutoAndroid.RunHistoryLog.Step(_client.Device?.Serial ?? "?",
                            $"[FARM] uid={_account?.Uid} | bước 5/6: Login (đăng nhập app Facebook)");
                        await _facebookService.Login(_client, _account, _ct, 400, this);

                        _sate = "Đợi sau đăng nhập";
                        if (_settingGeneral.GetBooleanValue("checkBox11", true))
                        {
                            int second = SubdyHelper.RandomValue(_settingGeneral.GetIntType("numericUpDown25", 10), _settingGeneral.GetIntType("numericUpDown24", 20));
                            await DelayMessageAsync(second, "Đợi {time} giây sau khi đăng nhập trước khi thực hiện thao tác.", 2);
                        }

                        _sate = "Lấy thông tin xác thực";
                        await ExtractAndUpdateAuthenticationInfoAsync();
                    }

                    AutoAndroid.RunHistoryLog.Step(_client.Device?.Serial ?? "?",
                        $"[FARM] uid={_account?.Uid} | bước 6/6: farming.ExecuteAsync (thực hiện kịch bản)");
                    await farming.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    AutoAndroid.DeviceChangeLog.Write(_client.Device?.Serial ?? "?",
                        $"[{AutoAndroid.DeviceChangeLog.BuildTag}] RunAsync: NGOẠI LỆ ở bước [{_sate}] uid={_account?.Uid}: {ex.Message}");
                    HanderCase(ex);
                }
                finally
                {
                    // Tắt mirror-guard của vòng account (ref-count: guard lồng của Change()
                    // đã tự giảm khi nó kết thúc, đây là lần giảm CUỐI -> watchdog nền tắt).
                    // Best-effort, không ném; đặt trước để luôn chạy kể cả khi Update lỗi.
                    _client.MirrorGuardStop();
                    if (_account != null)
                    {
                        _account.Running = false;
                        _accountContext.Update(_account);
                    }
                }
            }
            _stopwatch.Stop();
        }

        /// <summary>
        /// Pandora-specific run loop — music app automation.
        /// </summary>
        public async Task RunPandoraAsync()
        {
            _swTotal.Start();
            while (!_ct.IsCancellationRequested)
            {
                _account = null;
                if (!AccountServices.Accounts.Any())
                {
                    _client.LogHelper.SUCCESS("Đã hoàn thành!");
                    break;
                }

                await PrepareDeviceAsync();
                AutoResetAccountStates();
                AutoRecoverStuckAccounts();

                _sate = "Chuẩn bị tài khoản";
                _account = AccountServices.GetAccount();
                if (_account == null) continue;

                _account.Running = true;
                try
                {
                    if (!await CheckLiveAsync()) continue;

                    _sate = "Chuẩn bị thiết bị và proxy";
                    await ConnectAndPrepareDeviceAsync(true);

                    _sate = "Thực hiện kịch bản Pandora";
                    PandoraFarming farming = new PandoraFarming(this);
                    await farming.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    HanderCase(ex);
                }
                finally
                {
                    if (_account != null)
                    {
                        _account.Running = false;
                        _accountContext.Update(_account);
                    }
                }
            }
            _stopwatch.Stop();
        }

        // Cuộn news feed
        private async Task ScrollNewsFeedAsync()
        {
            string doneScript = GetDoneScriptFlag();
            if (string.IsNullOrEmpty(doneScript)) return;
            int maxScrolls = 10;
            for (int i = 0; i < maxScrolls; i++)
            {
                await Stop();
            }
        }

        // Lấy cờ hoàn thành kịch bản
        private string GetDoneScriptFlag()
        {
            if (_settingScript.GetBooleanValue("check_Interaction_2"))
                return "Swipe";
            if (_settingScript.GetBooleanValue("check_Interaction_3"))
                return "LIKE";
            return string.Empty;
        }
    }
}