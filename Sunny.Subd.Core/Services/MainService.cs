using AutoAndroid;
using Sunny.Subd.Core.Facebook;
using Sunny.Subd.Core.Facebook.ScriptActions;
using Sunny.Subd.Core.Instagram;
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
            if (startDateTime == null && endDateTime == null) return;
            TimeSpan now = DateTime.Now.TimeOfDay;
            TimeSpan startTime = startDateTime.Value.TimeOfDay;
            TimeSpan endTime = endDateTime.Value.TimeOfDay;

            if (startTime <= endTime && now >= startTime && now <= endTime)
            {
                TimeSpan remaining = endTime - now;
                int totalSecondsInt = (int)remaining.TotalSeconds;
                await DelayMessageAsync(totalSecondsInt, "Đã tới giờ nghỉ giải lao, phần mềm sẽ ngủ đông. Đợi {time} giây", 2);
            }


        }
        public async Task ExtractAndUpdateAuthenticationInfoAsync()
        {
            if (string.IsNullOrEmpty(_account.FullName))
            {
                _account.FullName = _client.GetFacebookFullName(_account.Uid); 
            }
            if (!_client.IsRoot()) return;
            _sate = "Lấy thông tin xác thực";
            if (string.IsNullOrEmpty(_account.Cookie) || string.IsNullOrEmpty(_account.Token))
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
                        _account.Cookie = parts[2];
                        _account.Token = parts[1];
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

            }
            SetStatus("Đang sao lưu thiết bị...", 2);
            if (_settingGeneral.GetBooleanValue("checkBox2", true))
            {
                string profileDir = _settingGeneral.GetValuesFromInputString("textBox2", Path.Combine(AppContext.BaseDirectory, "Backup", "Device", _platform));
                Directory.CreateDirectory(profileDir);
                string fileProfile = Path.Combine(profileDir, $"{_account.Uid}.tar.gz");
                await _client.BackupDevice(fileProfile);
            }
            SetStatus("Cập nhật trạng thái tài khoản", 2);
            _account.State = "LIVE";
            _accountContext.Update(_account);
        }

        // Thay đổi thông tin thiết bị
        private async Task ChangeInfoAsync()
        {
            if (!_settingGeneral.GetBooleanValue("checkBox1", true)) return;
            _sate = "Thay đổi thông tin thiết bị";
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
                    SetStatus($"Thành công. [{_client.GetDeviceName()}]", 2);
                }
                else
                {
                    SetStatus($"Thất bại. [{_client.GetDeviceName()}]", 1);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                SetStatus(ex.Message, 1);
            }
        }

        // Thay đổi proxy
        private async Task ChangeProxyAsync()
        {
            _sate = "Thay đổi IP/Proxy";
            SetStatus("Đang thay đổi IP/Proxy...", 2);
            _client.Shell("settings put global http_proxy :0");
            _client.StopApp(VATProxyService.Package_Proxy);
            string proxy = string.Empty;
            var proxyType = GetProxyType();
            SetStatus($"Loại: [{proxyType}] - ", 2);
            switch (proxyType)
            {
                case ProxyService.NoIP:
                    return;
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
                    proxy = _account.Proxy;
                    break;
            }
            SetStatus($"Loại: [{proxyType}] - [{proxy}]", 2);
            if (!string.IsNullOrEmpty(proxy))
            {
                _client.ConnectProxy(proxy);
            }
            int timeDelay = _settingGeneral.GetIntType("numericUpDown3", 10);
            await DelayMessageAsync(timeDelay, "Đợi {time} giây kết nối.", 2);
            if (proxyType == ProxyService.Mobile4G)
            {
                _client.DisablePlane();
                _client.Enabel4G();
                await DelayMessageAsync(5, "Đợi {time} giây kết nối 4G.", 2);
            }
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
            string fileAPK = string.Empty;
            if (_settingGeneral.GetBooleanValue("checkBox8", true))
            {
                fileAPK = _settingGeneral.GetValuesFromInputString("textBox4", FacebookHander.FilePath(_platform));
            }
            for (int i = 1; i <= 10; i++)
            {
                SetStatus($"[{i}/10] Đang khởi động ứng dụng {_platform}...", 2);
                _client.AppStart(FacebookHander.Package(_platform), true, true, true);
                if (_client.ElementWithAttributes($"//*[@text=\"{_platform} keeps stopping\"]", 5, click: false))
                {
                    var s = _client.ElementWithAttributes("//*[@text=\"Close app\"]");
                    SetStatus($"[{i}/{10}] Bị crash. Cài lại ứng dụng {_platform}.", 1);
                    if (!File.Exists(fileAPK))
                    {
                        SetStatus($"[{i}/{10}] Bị crash. Cài lại ứng dụng {_platform}. Không tìm thấy apk [{fileAPK}]", 1);
                        return false;
                    }
                    _client.UninstallApp(FacebookHander.Package(_platform));
                    _client.InstallApp(fileAPK);
                    continue;
                }
                if (_client.AppWait(FacebookHander.Package(_platform))) return true;
            }
            return _client.AppWait(FacebookHander.Package(_platform));
        }

        // Kết nối và chuẩn bị thiết bị
        private async Task<bool> ConnectAndPrepareDeviceAsync(bool changeProxy)
        {
            if (!await ConnectDeviceAsync())
            {
                SetStatus("Không thể kết nối thiết bị.", 1);
                return false;
            }

            if (changeProxy)
            {
                await ChangeInfoAsync();
                await ChangeProxyAsync();
            }

            if (_settingGeneral.GetBooleanValue("checkBox4", false))
            {
                int retryCount = _settingGeneral.GetIntType("numericUpDown1", 1);
                for (int i = 0; i < retryCount; i++)
                {
                    if (await IsInternetAsync())
                        return true;
                }

                SetStatus($"Reboot khi mất mạng quá {retryCount} lần", 2);
                _client.RebootAndWaitForDeviceReady();
                return false;
            }

            bool hasNet = await IsInternetAsync();
            if (!hasNet) SetStatus("Không có kết nối internet sau khi thử lại.", 1);
            return hasNet;
        }

        // Kết nối thiết bị
        private async Task<bool> ConnectDeviceAsync()
        {
            _sate = "Kết nối thiết bị";
            return _client.Connect();
        }
        private async Task<bool> InstallFacebook()
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
            for (int i = 1; i <= 10; i++)
            {

                SetStatus($"[{i}/10] Bị crash. Cài lại ứng dụng {_platform}.", 1);
                if (!File.Exists(fileAPK))
                {
                    SetStatus($"[{i}/10] Đang tải APK...", 2);
                    InitHelper.GithubDown(FacebookHander.DownloadUrl(_platform), fileAPK);
                }
                if (!File.Exists(fileAPK))
                {
                    throw new Exception($"Không thể tải APK [{fileAPK}]");
                }
                _client.InstallApp(fileAPK);
                if (!_client.AppList().Contains(FacebookHander.Package(_platform))) continue;
                SetStatus($"[{i}/10] Đang khởi động ứng dụng {_platform}...", 2);
                _client.AppStart(FacebookHander.Package(_platform), true, true, true);
                if (_client.AppWait(FacebookHander.Package(_platform))) return true;
                continue;
            }
            throw new Exception($"Không thể khởi động {_platform} sau 10 lần thử.");
        }
        // Chuẩn bị thiết bị
        private async Task PrepareDeviceAsync(bool isContainNoDelete = true)
        {
            _sate = "Chuẩn bị ứng dụng";
            var check = _settingGeneral.GetBooleanValue("checkBox12", false);
            if (!check)
            {
                List<string> packages = new List<string>();
                switch (_platform)
                {
                    case PlatformModel.Facebook:
                        {
                            if (!_client.AppList().Contains(FacebookHander.Package(_platform)))
                            {
                                _client.ElementWithAttributes("//*[@text=\"Close app\"]");
                                _client.StopApp(FacebookHander.Package(_platform));
                                _client.ElementWithAttributes("//*[@text=\"Close app\"]");
                                _client.UninstallApp(FacebookHander.Package(_platform));
                                await InstallFacebook();
                            }
                            packages = new List<string>
                {
                    "com.facebook.katana",
                    "com.facebook.lite",
                    "com.facebook.services",
                    "com.facebook.appmanager",
                    "com.facebook.system",
                    "com.facebook.systemservice",
                };
                            if (FacebookHander.GetAuthenticationInfo(_client).Contains(_account.Uid) && isContainNoDelete)
                            {
                                packages.Clear();
                            }
                            break;
                        }
                    case "Instagram":
                    case PlatformModel.Threads:
                        {
                            if (!_client.AppList().Contains(FacebookHander.Package(_platform)))
                            {
                                await InstallFacebook();
                            }
                            packages.Add(FacebookHander.Package(_platform));
                            break;
                        }
                }

                SetStatus("Đang xóa cache ứng dụng cũ...", 2);
                foreach (var package in packages)
                {
                    _client.AppClear(package);
                }

                SetStatus("Đang cấp quyền ứng dụng...", 2);
                _client.GrantAppPermissions(FacebookHander.Package(_platform));
            }
            if (!await OpenFacebookAsync()) throw new Exception($"Không thể mở {_platform}.");
            _client.SetSize();
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
            }
            if (check)
            {
                _account.State = "LIVE";
                SetStatus("Tài khoản facebook: LIVE", 2);
            }
            else
            {
                _account.State = "DIE";
                SetStatus("Tài khoản facebook: DIE", 1);
                throw new SubdyExtension(SubdyEnum.DIE, "Tài khoản facebook: DIE");
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
            await PrepareDeviceAsync(false);
        }
        // Khôi phục dữ liệu Facebook
        public async Task<bool> RestoreFacebookAsync()
        {
            await PrepareDeviceAsync();

            _client.StopApp(FacebookHander.Package(_platform));
            if (!_settingGeneral.GetBooleanValue("checkBox3", true) || !_client.IsRoot()) return await OpenFacebookAsync();
            string filezip = string.Empty;
            string profileDir = _settingGeneral.GetValuesFromInputString("textBox3", Path.Combine(AppContext.BaseDirectory, "Backup", "Profile", _platform));
            profileDir = Path.Combine(profileDir);
            Directory.CreateDirectory(profileDir);
            filezip = Path.Combine(profileDir, $"{_account.Uid}.tar.gz");
            if (!File.Exists(filezip)) return await OpenFacebookAsync();
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
                        ? "Thành công."
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

        public async Task RunAsync()
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

                if (IsReboot()) continue;

                _sate = "Kết nối thiết bị";
                if (!await ConnectAndPrepareDeviceAsync(false)) continue;

                _sate = "Chuẩn bị tài khoản";
                _account = AccountServices.GetAccount();

                if (_account == null) continue;
                _account.Running = true;
                try
                {
                    if (!await CheckLiveAsync()) continue;

                    _sate = "Chuẩn bị thiết bị và proxy";
                    if (!await ConnectAndPrepareDeviceAsync(true)) continue;

                    
                    if (!await RestoreFacebookAsync()) continue;

                    int index = _settingGeneral.GetIntType("comboBox1", 0);

                    _account.Uid_Email = index == 1 ? _account.Email : _account.Uid;
                    _account.Uid_Email ??= _account.Email ?? _account.Uid;

                    await _facebookService.Login(_client, _account, _ct, 180, this);

                    _sate = "Đợi sau đăng nhập";
                    if (_settingGeneral.GetBooleanValue("checkBox11", true))
                    {
                        int second = SubdyHelper.RandomValue(_settingGeneral.GetIntType("numericUpDown25", 10), _settingGeneral.GetIntType("numericUpDown24", 20));

                        await DelayMessageAsync(second, "Đợi {time} giây sau khi đăng nhập trước khi thực hiện thao tác.", 2);
                    }

                    _sate = "Lấy thông tin xác thực";
                    await ExtractAndUpdateAuthenticationInfoAsync();
                    _sate = "Thực hiện kịch bản";
                    if (_platform == PlatformModel.Facebook)
                    {
                        // Legacy ("FarmXu", "Farm-Xu-VIP") đã đổi tên hiển thị → "Làm Job Golike".
                        // Match cả tên cũ để account legacy chưa migrate vẫn chạy được.
                        if (string.Equals(_account.NameScript, ScriptNames.FarmXuVip, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(_account.NameScript, ScriptNames.FarmXuVipLegacy, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(_account.NameScript, "FarmXu", StringComparison.OrdinalIgnoreCase))
                        {
                            _config.JobService = "https://app.golike.net/";
                            var farmxuVip = new SpamXuHandler(_platform, _client, _config, _ct, _config.SettingJob, _account);
                            await farmxuVip.ExecuteAsync();
                        }
                        else
                        {
                            _config.JobService = "https://app.golike.net/";
                            FacebookFarming farming = new FacebookFarming(this);
                            await farming.ExecuteAsync();
                        }
                    }
                    else if (_platform == PlatformModel.Instagram || _platform == PlatformModel.Threads)
                    {
                        // Mirror Facebook flow: script "Làm Job Golike" → SpamXuHandler (Golike API).
                        // Các kịch bản custom IG → InstagramFarming.
                        if (string.Equals(_account.NameScript, ScriptNames.FarmXuVip, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(_account.NameScript, ScriptNames.FarmXuVipLegacy, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(_account.NameScript, "FarmXu", StringComparison.OrdinalIgnoreCase))
                        {
                            _config.JobService = "https://app.golike.net/";
                            var farmxuVip = new SpamXuHandler(_platform, _client, _config, _ct, _config.SettingJob, _account);
                            await farmxuVip.ExecuteAsync();
                        }
                        else
                        {
                            _config.JobService = "https://app.golike.net/";
                            var farming = new Instagram.InstagramFarming(this);
                            await farming.ExecuteAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    HanderCase(ex);
                }
                finally
                {
                    if (_account != null)
                    {
                        _accountContext.Update(_account);
                        _account.Running = false;
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