using AutoAndroid;
using Sunny.Subd.Core.Email;
using Sunny.Subd.Core.Gmail;
using Sunny.Subd.Core.Models;
using Sunny.Subd.Core.Phone;
using Sunny.Subd.Core.Proxies;
using Sunny.Subd.Core.Telegram;
using Sunny.Subd.Core.Utils;
using Sunny.Subdy.Common;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Common.Services;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sunny.Subd.Core.Facebook
{
    public class FacebookRegsiner
    {
        // Các trường dữ liệu private
        public readonly ADBClient _client;
        public Account _account;
        public readonly ConfigModel _config;
        public readonly CancellationToken _ct;
        public readonly AccountContext _accountContext = new();
        public readonly string _platform;
        public readonly Stopwatch _stopwatch = new();
        public int Timeout_Script = 0;
        public JsonHelper _settingGeneral;
        public string _sate = string.Empty;
        public Stopwatch _swTotal = new Stopwatch();
        private BackupRestoreHelper _backupRestoreHelper;
        private readonly string _typeRegister;
        private int _indexGmail = 0;
        private int _recoGmail = 0;
        private  GmailService _gmailService;
        private readonly PhoneService _phoneService;
        private readonly EmailService _emailService;
        private bool _isNVR = false;
        private readonly int _timeOut;
        public FacebookRegsiner(string platform, ADBClient device, ConfigModel config, CancellationToken ct)
        {
            _client = device ?? throw new ArgumentNullException(nameof(device));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));

            _ct = ct;
            _settingGeneral = config.SettingGeneral;
            _backupRestoreHelper = new BackupRestoreHelper(_client.Device);
            int index = _settingGeneral.GetIntType("comboBox1", 0);
            _typeRegister = RegistrationType.RegFacebook_AllTypes[index];

           
            string sitePhone = RegistrationType.PhoneNumberTypes[_settingGeneral.GetIntType("comboBox2", 0)];
            string tokenPhone = _settingGeneral.GetValuesFromInputString("textBox5", string.Empty).Trim();
            _phoneService = new PhoneService(sitePhone, tokenPhone);

            string siteEmail = RegistrationType.EmailTypes[_settingGeneral.GetIntType("cbb_Email", 0)];
            _emailService = new EmailService(siteEmail);
            _timeOut = _settingGeneral.GetIntType("numericUpDown4", 30) * 60000;
            _isNVR = _settingGeneral.GetBooleanValue("checkBox9");
        }
        public event EventHandler<Account> AccountAdded;
        private void OnAccountAdded(Account acc)
        {
            AccountAdded?.Invoke(this, acc);
        }
        // Phương thức trì hoãn với thông báo trạng thái
        public async Task DelayMessageAsync(int second, string message, int color)
        {

            for (int i = 1; i <= second; i++)
            {
                SetStatus($"[{i}/{second}]... {message}", color);
                await Task.Delay(1000);
            }
        }
        public void SetStatus(string status, int color, string logDetail = null)
        {
            if (!string.IsNullOrEmpty(_sate) && !status.Contains(_sate))
            {
                status = $"[{_sate}] - ({status})";
            }
            if (!string.IsNullOrEmpty(logDetail))
            {
                LogManager.Info($"[DEBUG] {logDetail}");
            }
            if (_account != null)
            {
                _account.Status = status;
                _account.RecentInteraction = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                _account.ColorType = color;
            }
            if (_client?.Device != null)
            {
                _client.Device.Status = status;
                _client.Device.TypeColor = color;
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

        }

        private async Task ExtractAndUpdateAuthenticationInfoAsync()
        {
            if (!_client.IsRoot()) return;
            // "Lưu cookie, token (root)" (checkBox10 của fSettingRegsiner): khi bỏ tích thì
            // KHÔNG ghi Cookie/Token vào account/DB. Uid luôn phải lấy (account đăng ký mới),
            // nên vẫn gọi GetAuthenticationInfo nhưng chỉ gán Uid.
            bool saveCookieToken = _settingGeneral.GetBooleanValue("checkBox10", true);
            switch (_platform)
            {
                case PlatformModel.Facebook:
                    {
                        string value = FacebookHander.GetAuthenticationInfo(_client);
                        if (string.IsNullOrWhiteSpace(value))
                            throw new Exception("Không thể lấy thông tin xác thực.");

                        var parts = value.Split('|');
                        if (parts.Length < 3)
                            throw new Exception("Chuỗi xác thực không hợp lệ.");

                        _account.Uid = parts[0];
                        if (saveCookieToken)
                        {
                            _account.Cookie = parts[2];
                            _account.Token = parts[1];
                        }

                        break;
                    }
            }
            if (_settingGeneral.GetBooleanValue("checkBox3", true))
            {
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
                        {
                            _backupRestoreHelper.BackupInstagram(fileProfile);
                            break;
                        }
                }
            }
            if (_settingGeneral.GetBooleanValue("checkBox2", true))
            {
                string profileDir = _settingGeneral.GetValuesFromInputString("textBox2", Path.Combine(AppContext.BaseDirectory, "Backup", "Device", _platform));
                Directory.CreateDirectory(profileDir);
                string fileProfile = Path.Combine(profileDir, $"{_account.Uid}.tar.gz");
                await _client.BackupDevice(fileProfile);
            }
            _account.State = "LIVE";
            _accountContext.Add(_account);
        }

        // Thay đổi thông tin thiết bị
        // Kết quả bước đổi thiết bị của account hiện tại (ô trạng thái UI chỉ giữ dòng
        // cuối cùng nên lưu lại để nhúng vào dòng trạng thái của bước đổi proxy).
        private string _lastDeviceChange = string.Empty;

        // Thay đổi thông tin thiết bị
        // Luôn chạy và LUÔN chạy trước đổi proxy (ConnectAndPrepareDeviceAsync gọi
        // ChangeInfoAsync rồi mới ChangeProxyAsync). Không gate theo checkBox1 nữa:
        // toggle đó bị ẩn trong UI, giá trị lưu có thể false khiến bước đổi thiết bị
        // bị bỏ qua im lặng (không in ra dòng log nào) rồi nhảy thẳng sang proxy.
        private async Task ChangeInfoAsync()
        {
            _sate = "Thay đổi thông tin thiết bị";
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
        private async Task ChangeProxyAsync()
        {
            _sate = "Thay đổi IP/Proxy";
            SetStatus("Đang thay đổi IP/Proxy...", 2);
            _client.Shell("settings put global http_proxy :0");
            _client.Shell("am broadcast -a com.vat.proxyconnector.STOP_PROXY -n com.vat.proxyconnector/.ProxyReceiver");
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
            // Nguồn proxy được cấu hình không trả về gì -> thử proxy đã gán cho tài khoản
            // (cách bản cũ vẫn chạy) để luồng đăng ký không bỏ trống bước đổi proxy.
            if (string.IsNullOrWhiteSpace(proxy))
            {
                string assigned = _account?.Proxy;
                if (!string.IsNullOrWhiteSpace(assigned))
                {
                    proxy = assigned;
                    SetStatus($"Nguồn [{proxyType}] trống, dùng proxy đã gán: {ProxyService.Mask(proxy)}.", 2);
                }
            }
            SetStatus($"Loại: [{proxyType}] - {_lastDeviceChange} - [{ProxyService.Mask(proxy)}]", 2);
            if (!string.IsNullOrEmpty(proxy))
            {
                _client.ConnectProxy(proxy);
            }
            int timeDelay = _settingGeneral.GetIntType("numericUpDown3", 10);
            await DelayMessageAsync(timeDelay, "Delay kết nối.", 2);
            if (proxyType == ProxyService.Mobile4G)
            {
                _client.DisablePlane();
                _client.Enabel4G();
                await DelayMessageAsync(5, "Delay kết nối 4G.", 2);
            }
        }

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
            // Không có key (txtLines trống) thì trả về ngay thay vì gọi API với key rỗng.
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
                string ip = await _client.GetIp();
                if (string.IsNullOrEmpty(ip))
                {
                    SetStatus($"[{i}/{attempts}] Không có internet.", 1);
                        await _client.DisableWifi();
                        await Task.Delay(5000);
                        await _client.EnableWifi();
                         await Task.Delay(15000);
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
                SetStatus($"[{i}/10] Đang khởi động ứng dụng...", 2);
                _client.AppStart(FacebookHander.Package(_platform), true, true, true);
                if (_client.ElementWithAttributes($"//*[@text=\"{_platform} keeps stopping\"]", 5, click: false))
                {
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
                return false;

            await PrepareDeviceAsync();

            if (changeProxy)
            {
                await ChangeInfoAsync();
                await ChangeProxyAsync();
            }

            if (_settingGeneral.GetBooleanValue("checkBox4", false))
            {
                int retryCount = _settingGeneral.GetIntType("numericUpDown1", 1);
                bool triedJoinWifi = false;
                for (int i = 0; i < retryCount; i++)
                {
                    if (await IsInternetAsync())
                        return true;

                    // Mất mạng → nếu user đã cấu hình ssid/pass cho serial này thì
                    // join wifi qua adb-join-wifi rồi thử lại. Chỉ chạy 1 lần để
                    // tránh spam install/launch APK trong vòng lặp.
                    if (!triedJoinWifi)
                    {
                        triedJoinWifi = true;
                        if (await TryJoinConfiguredWifiAsync() && await IsInternetAsync())
                            return true;
                    }
                }

                SetStatus($"Reboot khi mất mạng quá {retryCount} lần", 2);
                _client.RebootAndWaitForDeviceReady(
                    $"checkBox4 'Reboot khi mất mạng' = BẬT, đã thử {retryCount} lần không có internet (job ĐĂNG KÝ FacebookRegsiner).");
                return false;
            }

            if (await IsInternetAsync()) return true;

            // Single-attempt branch: nếu fail và có ssid/pass cấu hình → thử join và re-check.
            return await TryJoinConfiguredWifiAsync() && await IsInternetAsync();
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

        // Chuẩn bị thiết bị
        private async Task PrepareDeviceAsync()
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

                            packages = new List<string>
                {
                    "com.facebook.katana",
                    "com.facebook.lite",
                    "com.facebook.services",
                    "com.facebook.appmanager",
                    "com.facebook.system",
                    "com.facebook.systemservice",
                };
                            break;
                        }
                    case "Instagram":
                        {
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
        private async Task<bool> HandleInitialLogin()
        {
            _sate = "Đăng nhập Gmail";
            switch (_typeRegister)
            {
                case RegistrationType.Gmail_BaitPhoneNumber:
                case RegistrationType.Gmail:
                    _client.StopApp(FacebookHander.Package(PlatformModel.Facebook));
                    return await LoginGmailAsync();
                default:
                    return true;
            }
        }
        private async Task<bool> LoginGmailAsync()
        {
            if (Globals.Gmails.Count == 0) return false;
            _gmailService = new GmailService(this);
            if (_indexGmail >= _recoGmail)
            {
                await _gmailService.RemoveAccount();
                _indexGmail = 0;
            }


            string value;
            lock (Globals.Gmails)
            {
                value = Globals.Gmails[0];
                Globals.Gmails.RemoveAt(0);
            }

            if (string.IsNullOrEmpty(value)) return false;

            string[] parts = value.Split('|');
            string email = parts[0].Trim();
            string password = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            _account.Email = email;
            _account.PassMail = password;
            _indexGmail++;
            return await _gmailService.Login(email, password);
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
                SetStatus($"Tự reboot sau {_settingGeneral.GetIntType("numericUpDown2", 30)} phút.", 2);
                int mins = _settingGeneral.GetIntType("numericUpDown2", 30);
                _client.RebootAndWaitForDeviceReady(
                    $"checkBox5 'Tự reboot sau N phút' = BẬT, đã chạy {mins} phút (job ĐĂNG KÝ FacebookRegsiner).");
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

                if (IsReboot()) continue;

                _sate = "Kết nối thiết bị";
                if (!await ConnectAndPrepareDeviceAsync(false)) continue;

                _sate = "Tạo tài khoản mới";
                _account = GetAccount();

                if (_account == null) continue;
                OnAccountAdded(_account);
                _account.Running = true;
                try
                {
                    // ── CHẶN MÀN HÌNH ĐEN SUỐT VÒNG ACCOUNT (v12) ──────────────────────────
                    // Mirror-guard chạy NỀN suốt cửa sổ nguy hiểm (đổi thiết bị -> proxy ->
                    // mở Facebook đăng ký). Trước đây guard chỉ sống trong Change() rồi Stop,
                    // để hở đúng lúc mở FB (createSurface) khi mồ côi VirtualDisplay còn sót.
                    // Guard KHÔNG kill system_server nên KHÔNG rớt tun0/VPN — an toàn cho FB.
                    _client.MirrorGuardStart();

                    // Recovery leak (DẠNG 2) ở ĐẦU account = điểm VPN-safe: proxy account MỚI
                    // chưa nối (ConnectAndPrepareDeviceAsync(true) bên dưới mới đổi proxy),
                    // proxy account trước không còn cần -> nếu phải kill system_server cũng
                    // không mất VPN đang dùng. Bắt sớm TaskRecord rò rỉ tích lũy từ vòng trước.
                    try { _client.maxChange?.RecoverFrameworkIfBlackScreenPublic(); }
                    catch (Exception exR)
                    {
                        AutoAndroid.DeviceChangeLog.Write(_client.Device?.Serial ?? "?",
                            $"[{AutoAndroid.DeviceChangeLog.BuildTag}] FacebookRegsiner: recovery đầu account lỗi (bỏ qua): {exR.Message}");
                    }

                    _sate = "Chuẩn bị thiết bị và proxy";
                    if (!await ConnectAndPrepareDeviceAsync(true)) continue;

                    _sate = "Đăng nhập Gmail";
                    if (!await HandleInitialLogin()) continue;

                    _sate = "Đăng ký tài khoản Facebook mới";
                    var message = await ImportInfo();

                    if (message.SubdyEnum == SubdyEnum.Success)
                    {
                        if (_settingGeneral.GetBooleanValue("checkBox11", true))
                        {
                            int second = SubdyHelper.RandomValue(_settingGeneral.GetIntType("numericUpDown25", 10), _settingGeneral.GetIntType("numericUpDown24", 20));

                            await DelayMessageAsync(second, "Delay sau khi đăng kí thành công.", 2);
                        }
                        
                    }
                    _sate = "Lấy thông tin xác thực";
                    await ExtractAndUpdateAuthenticationInfoAsync();

                }
                catch (Exception ex)
                {
                    HanderCase(ex);
                }
                finally
                {
                    // Tắt mirror-guard của vòng account (ref-count về 0 -> watchdog nền tắt).
                    // Best-effort, không ném; đặt trước để luôn chạy kể cả khi phần dưới lỗi.
                    _client.MirrorGuardStop();
                    if (_account != null)
                    {
                        _accountContext.Update(_account);
                        _account.Running = false;
                        if (TelegramBotServices.BotTelegram != null)
                        {
                            string id = _settingGeneral.GetValuesFromInputString("textBox8");
                            string message = $"[{DateTime.Now.ToString("dd-MM-yyyy")}] {_account.Uid} {_account.State}";
                            if (_settingGeneral.GetBooleanValue("radioButton7") && _account.State != "LIVE")
                            {
                                message = "";
                            }
                            if (!string.IsNullOrEmpty(message))
                            {
                                await TelegramBotServices.BotTelegram.SendMessageAsync(Convert.ToInt64(id), message);
                            }
                        }
                    }
                }
            }
            _stopwatch.Stop();
        }

        private Account GetAccount()
        {
            _account = new Account();
            List<string> firstnames = GetFirstnames();
            List<string> lastnames = GetLastnames();
            _account.Password = GetPassword();
            _account.NameFolder = _config.JobService;
            _account.FullName = $"{SubdyHelper.GetStringRandom(firstnames)} {SubdyHelper.GetStringRandom(lastnames)}";
            _account.Platformt = _platform;
            return _account;
        }
        private List<string> GetFirstnames()
        {
            if (_settingGeneral.GetBooleanValue("radioButton1", false))
                return SubdyHelper.FirstnameRandom;
            if (_settingGeneral.GetBooleanValue("radioButton3", false))
                return File.Exists(_settingGeneral.GetValuesFromInputString("txt_Ho", string.Empty))
                    ? File.ReadAllLines(_settingGeneral.GetValuesFromInputString("txt_Ho", string.Empty)).ToList()
                    : SubdyHelper.FirstnameVN;
            return SubdyHelper.FirstnameVN;
        }
        private string GetPassword()
        {
            if (_settingGeneral.GetBooleanValue("radioButton2", false) && !string.IsNullOrEmpty(_settingGeneral.GetValuesFromInputString("txtPass", "")))
                return _settingGeneral.GetValuesFromInputString("txtPass", "").Trim();
            return SubdyHelper.RandomPassword(SubdyHelper.RandomValue(7, 18), digit: false);
        }
        private List<string> GetLastnames()
        {
            if (_settingGeneral.GetBooleanValue("radioButton1", false))
                return SubdyHelper.LastnameRandom;
            if (_settingGeneral.GetBooleanValue("radioButton3", false))
                return File.Exists(_settingGeneral.GetValuesFromInputString("txt_Ten", string.Empty))
                    ? File.ReadAllLines(_settingGeneral.GetValuesFromInputString("txt_Ten", string.Empty)).ToList()
                    : SubdyHelper.LastnameVN;
            return SubdyHelper.LastnameVN;
        }
        private async Task<bool> EnsureAppRunning()
        {
            if (!_client.IsRunningApp(FacebookHander.Package(PlatformModel.Facebook)))
            {
                _client.AppStart(FacebookHander.Package(PlatformModel.Facebook), true, true, true);
                await DelayMessageAsync(5, _account.Status, 2);
                return false;
            }
            return true;
        }
        private bool IsConfirmationCase(string currentCase)
        {
            return currentCase == $"//*[contains(@text, \"confirm your account\") and contains(@text, \"{_account.Email}\")]" ||
                   currentCase == $"//*[contains(@text, \"confirm your account\") and contains(@text, \"{_account.Phone?.Split("|")[0]}\")]";
        }

        private async Task HandleConfirmationCode()
        {
            await DelayMessageAsync(10, _account.Status, 2);
            string code = await GetCode();
            if (string.IsNullOrEmpty(code))
            {
                _client.LogHelper.ERROR("Không nhận được mã xác nhận.");
                throw new SubdyExtension(SubdyEnum.Stop, "Không nhận được mã xác nhận.");
            }

            if (_typeRegister == RegistrationType.Gmail_BaitPhoneNumber || _typeRegister == RegistrationType.Gmail)
            {
                _client.Shell("input keyevent KEYCODE_APP_SWITCH");
                _client.ElementWithAttributes("//*[@content-desc=\"Facebook\"]");
            }

            await DelayMessageAsync(2, _account.Status, 2);
             _client.SendTextADB("//*[@class=\"android.widget.EditText\"]", code, timeout: 10);
            _client.ElementWithAttributes(new List<string> { "//*[@text=\"Next\"]", "//*[@content-desc=\"Next\"]" }, 5);
            await DelayMessageAsync(10, _account.Status, 2);
        }

        private async Task HandleEmailInput()
        {
            if (!_client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), timeoutInSeconds: 1, click: false)) return;
            switch (_typeRegister)
            {
                case RegistrationType.Domain_BaitPhoneNumber:
                case RegistrationType.Gmail_BaitPhoneNumber:
                case RegistrationType.PhoneNumber:
                    _client.ElementWithAttributes(new List<string> { "//*[@text=\"Sign up with mobile number\"]" }, 5);
                    break;
                default:
                    await GetEmail();
                    if (string.IsNullOrEmpty(_account.Email)) return;
                     _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", _account.Email.Split('|')[0], timeout: 5);
                    var xpaths = XpathManagerFacebook.Get(XpathType.NavigationButton);
                    xpaths.Remove("//*[@content-desc=\"I already have an account\"]");
                    _client.ElementWithAttributes(xpaths, 5);
                    await DelayMessageAsync(10, _account.Status, 2);
                    break;
            }
        }

        private async Task HandlePhoneInput()
        {
            await DelayMessageAsync(5, _account.Status, 2);
            if (!_client.ElementWithAttributes(new List<string> { "//*[@text=\"What is your mobile number?\"]", "//*[@text=\"Sign up with email\"]" }, click: false) || !_client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), timeoutInSeconds: 1, click: false)) return;
            switch (_typeRegister)
            {
                case RegistrationType.Domain_BaitPhoneNumber:
                case RegistrationType.Gmail_BaitPhoneNumber:
                case RegistrationType.PhoneNumber:
                    await GetPhone();
                    if (string.IsNullOrEmpty(_account.Phone)) return;
                    _client.ElementWithAttributes(new List<string> { "//*[@text=\"Sign up with mobile number\"]" }, 5);

                    if (_account.Phone.Contains("ERROR"))
                    {
                        _client.LogHelper.ERROR(_account.Phone);
                        await Task.Delay(5000);
                        return;
                    }
                    string rawPhone = _account.Phone;
                    if (_account.Phone.Contains("|"))
                    {
                        rawPhone = _account.Phone.Split('|')[1].Trim();
                    }
                    if (!rawPhone.StartsWith("1") && !rawPhone.StartsWith("0") && !rawPhone.StartsWith("84") && !rawPhone.StartsWith("+84"))
                    {
                        rawPhone = "+84" + rawPhone;
                    }
                    else if (!rawPhone.StartsWith("1") && !rawPhone.StartsWith("0") && !rawPhone.StartsWith("+")) // Đã có 84 nhưng thiếu "+"
                    {
                        rawPhone = "+" + rawPhone;
                    }
                    else if (rawPhone.StartsWith("1") && !rawPhone.StartsWith("+")) // Đã có 84 nhưng thiếu "+"
                    {
                        rawPhone = "+" + rawPhone;
                    }

                     _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", rawPhone, timeout: 5);
                    var xpaths = XpathManagerFacebook.Get(XpathType.NavigationButton);
                    xpaths.Remove("//*[@content-desc=\"I already have an account\"]");
                    _client.ElementWithAttributes(xpaths, 5);
                    break;
                default:
                    _client.ElementWithAttributes(new List<string> { "//*[@text=\"Sign up with email\"]" }, 5);
                    break;
            }
            //DelayMessageAsync(10);
        }

        private void HandleNameSelection()
        {
            if (!_client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), timeoutInSeconds: 1, click: false)) return;
            _client.ElementWithAttributes("//*[@class=\"android.widget.RadioButton\"]", 5);
            var xpaths = XpathManagerFacebook.Get(XpathType.NavigationButton);
            xpaths.Remove("//*[@content-desc=\"I already have an account\"]");
            _client.ElementWithAttributes(xpaths, 5);
        }

        private async Task HandleNameInput()
        {
            if (!_client.ElementWithAttributes("//*[@class=\"android.widget.EditText\"]", click: false) || !_client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), click: false)) return;

            bool swap = SubdyHelper.RandomValue(0, 2) == 1;
            string[] nameParts = _account.FullName.Split(' ');
            string firstName = nameParts[0];
            string lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";
            if (swap) (firstName, lastName) = (lastName, firstName);

            var elements = _client.FindElements(10, "", "//*[@class=\"android.widget.EditText\"]");
            if (!elements.Any()) return;

            if (SubdyHelper.RandomValue(0, 2) == 1)
            {
                 _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", firstName, timeout: 5, xml: elements[0].OuterXml);
                if (elements.Count > 1)
                     _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", lastName, timeout: 5, xml: elements[1].OuterXml);
            }
            else
            {
                 _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", lastName, timeout: 5, xml: elements[0].OuterXml);
                if (elements.Count > 1)
                     _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", firstName, timeout: 5, xml: elements[1].OuterXml);
            }
            var xpaths = XpathManagerFacebook.Get(XpathType.NavigationButton);
            xpaths.Remove("//*[@content-desc=\"I already have an account\"]");
            _client.ElementWithAttributes(xpaths, 5);
        }

        private void HandleDateOfBirth()
        {
            var elementsBirth = _client.FindElements(10, "", "//*[contains(@text, 'Date of birth') and contains(@text, 'years old')]");
            if (!elementsBirth.Any()) return;

            string dateText = elementsBirth.First().Attributes["text"].Value;
            int age = ExtractAgeFromText(dateText);
            if (age < 18)
                _client.ElementWithAttributes("//*[contains(@text, 'Date of birth') and contains(@text, 'years old')]", 5);
        }

        private int ExtractAgeFromText(string text)
        {
            Regex regex = new Regex(@"\d+");
            Match match = regex.Match(text);
            return match.Success ? Convert.ToInt32(match.Value) : 0;
        }

        private async Task HandleDatePicker()
        {
            if (!_client.ElementWithAttributes(_client.FindElement("", new List<string> { "//*[@text=\"SET\"]", "//*[@text=\"Next\"]" }, 5), 1, click: false)) return;

            var elementsDate = _client.FindBounds("", "//*[@resource-id=\"android:id/numberpicker_input\"]");
            if (elementsDate.Count != 3) return;

            for (int i = 0; i < elementsDate.Count; i++)
            {
                string element = elementsDate[i];
                int indexRandom = i == 2 ? 42 : 12;
                int indexMin = i == 2 ? 18 : 1;
                var point = new RectangleArea(element).GetCenterPoint();
                //var point = _client.FindPoint("//*[@resource-id=\"android:id/numberpicker_input\"]", 1, element);
                 _client.Swipe(point.X, point.Y - 100, point.X, point.Y + 100, SubdyHelper.RandomValue(7, 12), SubdyHelper.RandomValue(indexMin, indexRandom));
            }

            _client.ElementWithAttributes(new List<string> { "//*[@text=\"SET\"]" }, 5);
            await DelayMessageAsync(2, _account.Status, 2);
            var xpaths = XpathManagerFacebook.Get(XpathType.NavigationButton);
            xpaths.Remove("//*[@content-desc=\"I already have an account\"]");
            _client.ElementWithAttributes(xpaths, 5);
        }

        private void HandleGenderSelection()
        {
            if (!_client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), click: false)) return;

            List<string> genderOptions = new List<string> { "//*[@text=\"Male\"]", "//*[@text=\"Female\"]" };
            if (_client.ElementWithAttributes(genderOptions[SubdyHelper.RandomValue(0, genderOptions.Count)], 5))
            {
                var xpaths = XpathManagerFacebook.Get(XpathType.NavigationButton);
                xpaths.Remove("//*[@content-desc=\"I already have an account\"]");
                _client.ElementWithAttributes(xpaths, 5);
            }

        }

        private async Task HandlePasswordInput()
        {
            await DelayMessageAsync(2, _account.Status, 2);
            if (!_client.ElementWithAttributes("//*[@class=\"android.widget.EditText\"]", timeoutInSeconds: 1, click: false) || !_client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), timeoutInSeconds: 1, click: false)) return;

             _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", _account.Password, timeout: 5);
            var xpaths = XpathManagerFacebook.Get(XpathType.NavigationButton);
            xpaths.Remove("//*[@content-desc=\"I already have an account\"]");
            _client.ElementWithAttributes(xpaths, 5);
        }

        private async Task<string> GetCode()
        {
            int timeout = 180;
            int interval = 2000;
            DateTime startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalSeconds < timeout)
            {
                string code = _typeRegister switch
                {
                    RegistrationType.Gmail_BaitPhoneNumber or RegistrationType.Gmail => await _gmailService.GetCode(),
                    RegistrationType.PhoneNumber => await _phoneService.GetCode(_account.Phone?.Split("|")[0]),
                    RegistrationType.Domain or RegistrationType.Domain_BaitPhoneNumber => await _emailService.GetCode(_account.Email, _settingGeneral.GetValuesFromInputString("textBox6")),
                    _ => string.Empty
                };

                if (!string.IsNullOrEmpty(code)) return code;

                _client.LogHelper.ERROR("Không nhận được mã xác nhận.");
                await Task.Delay(interval);
            }

            return string.Empty;
        }

        private async Task<string> GetEmail()
        {
            if (_typeRegister is RegistrationType.Domain_BaitPhoneNumber or RegistrationType.Domain)
            {
                string token = _settingGeneral.GetValuesFromInputString("textBox6", string.Empty).Trim();
                _account.Email = await _emailService.GetEmail(token);
                return _account.Email;
            }
            return string.Empty;
        }

        private async Task<string> GetPhone()
        {
            string phone = _typeRegister switch
            {
                RegistrationType.PhoneNumber => await _phoneService.GetPhone("facebook"),
                RegistrationType.Gmail_BaitPhoneNumber or RegistrationType.Domain_BaitPhoneNumber => GetRandomPhoneNumber(),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(phone))
            {
                _account.Phone = phone;
                return phone;
            }

            _client.LogHelper.ERROR("Không nhận được số điện thoại.");
            return string.Empty;
        }

        private string GetRandomPhoneNumber()
        {
            string country = SubdyHelper.RandomPhoneVN();
            if (_settingGeneral.GetBooleanValue("radioButton1"))
            {
                country = SubdyHelper.RandomPhoneUS();
            }
            return country;
        }

        private async Task<SubdyExtension> Agreement()
        {
            List<string> xpaths = BuildAgreementXPaths();
            var listLogin = BuildLoginList(xpaths);

            while (_stopwatch.ElapsedMilliseconds < _timeOut && !_ct.IsCancellationRequested)
            {
                // Quét & bấm nút Dismiss TRƯỚC khi khớp nhóm — xem ghi chú ở FacebookHander.
                if (FacebookHander.TryClickAnyDismiss(_client))
                {
                    _client.Delay(1);
                    continue;
                }
                string currentCase = _client.FindElement("", listLogin, 120);
                if (string.IsNullOrEmpty(currentCase))
                {
                    _client.AppStart(FacebookHander.Package(PlatformModel.Facebook), true, true, true);
                    await DelayMessageAsync(15, _account.Status, 2);
                    continue;
                }
                SetStatus("Đang xử lý...", 2, logDetail: currentCase);
                if (IsConfirmationCase(currentCase))
                {
                    if (currentCase == $"//*[contains(@text, \"confirm your account\") and contains(@text, \"{_account.Phone?.Split("|")[0]}\")]")
                    {
                        switch (_typeRegister)
                        {
                            case RegistrationType.Gmail_BaitPhoneNumber:
                            case RegistrationType.Domain_BaitPhoneNumber:
                                {
                                    _client.ElementWithAttributes("//*[@text=\"I didn’t get the code\"]", 5);
                                    await DelayMessageAsync(5, _account.Status, 2);
                                    if (!_client.ElementWithAttributes("//*[@text=\"Confirm by email\"]", 25)) continue;
                                    await GetEmail();
                                    if (string.IsNullOrEmpty(_account.Email))
                                    {
                                        _client.LogHelper.ERROR("Không nhận được email.");
                                        return new SubdyExtension(SubdyEnum.Error, "Không nhận được email.");
                                    }
                                    _client.SendTextADB("//*[@class=\"android.widget.EditText\"]", _account.Email.Split('|')[0], timeout: 25);
                                    _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), 5);
                                    await DelayMessageAsync(10, _account.Status, 2);
                                    break;
                                }
                        }
                    }
                    if (_isNVR)
                    {
                        return new SubdyExtension(SubdyEnum.Success, "Đăng ký thành công!");
                    }


                    await HandleConfirmationCode();
                    continue;
                }

                switch (currentCase)
                {
                    case "//*[@text=\"I agree\"]":
                    case var x when XpathManagerFacebook.Get(XpathType.MetaAdsConsent).Contains(x):
                        await FacebookHander.TryHandleMetaAdsConsentAsync(_client);
                        break;
                    case var x when XpathManagerFacebook.Get(XpathType.NavigationButton).Contains(x):
                        _client.ElementWithAttributes(currentCase, 5);
                        await DelayMessageAsync(1, _account.Status, 2);
                        break;
                    case var x when new List<string> { "//*[@text=\"I didn’t get the code\"]", "//*[@text=\"Confirm by email\"]" }.Contains(x):
                        if (_typeRegister is RegistrationType.Domain_BaitPhoneNumber or RegistrationType.Gmail_BaitPhoneNumber)
                        {
                            _client.ElementWithAttributes(currentCase, 5);
                            await DelayMessageAsync(5, _account.Status, 2);
                            break;
                        }
                        await HandleConfirmationCode();
                        break;
                    case "//*[@text=\"Enter an email\"]":
                        await HandleEmailInputForAgreement();
                        break;
                    case "//*[@content-desc=\"We couldn't create an account for you\"]":
                    case "//*[@text=\"We couldn't create an account for you\"]":
                    case var x when XpathManagerFacebook.Combine(XpathType.CP282, XpathType.CP956, XpathType.Captcha, XpathType.ExistEmail).Contains(x):
                        throw new SubdyExtension(SubdyEnum.CP_282, $"Tài khoản bị - [{currentCase}]");
                    case var x when XpathManagerFacebook.Get(XpathType.Success).Contains(x):
                        await DelayMessageAsync(5, _account.Status, 2);
                        if (!_client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.Success), click: false)) continue;
                        return new SubdyExtension(SubdyEnum.Success, "LIVE");
                    case "//*[@text=\"Enter the confirmation code\"]":
                        {
                            switch (_typeRegister)
                            {
                                case RegistrationType.Gmail_BaitPhoneNumber:
                                case RegistrationType.Domain_BaitPhoneNumber:
                                    {
                                        _client.ElementWithAttributes("//*[@text=\"I didn’t get the code\"]", 5);
                                        await DelayMessageAsync(5, _account.Status, 2);
                                        if (!_client.ElementWithAttributes("//*[@text=\"Confirm by email\"]", 25)) continue;
                                        await GetEmail();
                                        if (string.IsNullOrEmpty(_account.Email))
                                        {
                                            _client.LogHelper.ERROR("Không nhận được email.");
                                            return new SubdyExtension(SubdyEnum.Error, "Không nhận được email.");
                                        }
                                        _client.SendTextADB("//*[@class=\"android.widget.EditText\"]", _account.Email.Split('|')[0], timeout: 25);
                                        _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), 5);
                                        await DelayMessageAsync(10, _account.Status, 2);
                                        break;
                                    }
                            }
                            if (_isNVR)
                            {
                                return new SubdyExtension(SubdyEnum.Success, "Đăng ký thành công!");
                            }
                            await HandleConfirmationCode();
                            break;
                        }
                }
            }

            return new SubdyExtension(SubdyEnum.Error, "Đã xảy ra lỗi khi đăng ký.");
        }

        private List<string> BuildAgreementXPaths()
        {
            List<string> xpaths = new List<string>();
            if (!string.IsNullOrEmpty(_account.Email))
            {
                xpaths.Add($"//*[contains(@text, \"confirm your account\") and contains(@text, \"{_account.Email}\")]");

            }
            if (!string.IsNullOrEmpty(_account.Phone))
            {
                xpaths.Add($"//*[contains(@text, \"confirm your account\") and contains(@text, \"{_account.Phone?.Split("|")[0]}\")]");
            }
            xpaths.Add("//*[@text=\"We couldn't create an account for you\"]");
            xpaths.AddRange(new List<string>
        {
            "//*[@text=\"We couldn't create an account for you\"]",
            "//*[@text=\"Enter an email\"]",
            "//*[@text=\"Couldn't create account\"]",
            "//*[@text=\"Enter email\"]",
            "//*[@text=\"Confirm with email\"]",
            "//*[@text=\"I didn't receive a code\"]",
            "//*[@content-desc=\"We couldn't create an account for you\"]",
            "//*[@text=\"Please log in again.\"]",
            "//*[@text=\"Sign up\"]",
            "//*[@text=\"Create new account\"]",
            "//*[@content-desc=\"Create new account\"]",
            "//*[@content-desc=\"Join Facebook\"]",
            "//*[@text=\"Get started\"]",
            "//*[@content-desc=\"No, create new account\"]",
            "//*[@text=\"Enter the confirmation code\"]",
        });
            return xpaths;
        }

        private List<string> BuildLoginList(List<string> xpaths)
        {
            var listLogin = new List<string>();
            listLogin.AddRange(XpathManagerFacebook.Combine(XpathType.CP282, XpathType.CP956, XpathType.ExistEmail, XpathType.Success));
            listLogin.AddRange(xpaths);
            // Đứng TRƯỚC NavigationButton: trên màn "pay or consent" phần tử khớp đầu
            // của NavigationButton là nút Continue mờ (click vô tác dụng) -> kẹt.
            listLogin.AddRange(XpathManagerFacebook.Get(XpathType.MetaAdsConsent));
            listLogin.AddRange(XpathManagerFacebook.Get(XpathType.NavigationButton));
            listLogin.Add("//*[@text=\"I agree\"]");
            return listLogin;
        }

        private async Task HandleEmailInputForAgreement()
        {
            if (!_client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), timeoutInSeconds: 1, click: false)) return;
            if (_typeRegister is RegistrationType.Domain or RegistrationType.Gmail_BaitPhoneNumber or RegistrationType.Domain_BaitPhoneNumber or RegistrationType.Gmail)
            {
                await GetEmail();
                if (string.IsNullOrEmpty(_account.Email))
                {
                    _client.LogHelper.ERROR("Không nhận được email.");
                    return;
                }
                _client.ElementWithAttributes(_client.FindElement("", new List<string> { "//*[@text=\"Next\"]" }, 1), click: false);
                _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", _account.Email.Split('|')[0], timeout: 5);
            }
            _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), 5);
        }
        private async Task<SubdyExtension> ImportInfo()
        {
            _sate = "Nhập thông tin đăng ký";
            string currentCase = string.Empty;
            List<string> caseFacebooks = FacebookHander.Regsiner_Facebook();
            caseFacebooks.Remove("//*[@content-desc=\"I already have an account\"]");
            _stopwatch.Restart();
            while (_stopwatch.ElapsedMilliseconds < _timeOut && !_ct.IsCancellationRequested)
            {
                if (!await EnsureAppRunning()) continue;
                currentCase = _client.FindElement("", caseFacebooks, 120);
                if (string.IsNullOrEmpty(currentCase))
                {
                    _client.AppStart(FacebookHander.Package(PlatformModel.Facebook), true, true, true);
                    await DelayMessageAsync(5, _account.Status, 2);
                    continue;
                }

                SetStatus("Đang xử lý...", 2, logDetail: currentCase);

                if (IsConfirmationCase(currentCase))
                {
                    if (_isNVR)
                    {
                        return new SubdyExtension(SubdyEnum.Success, "Đăng ký thành công!");
                    }

                    await HandleConfirmationCode();
                    continue;
                }

                switch (currentCase)
                {
                    case var c when XpathManagerFacebook.Get(XpathType.Loading).Contains(c):
                        break;
                    case var c when XpathManagerFacebook.Get(XpathType.ExistEmail).Contains(c):
                        return new SubdyExtension(SubdyEnum.EmailExist, "Đã có tài khoản thêm mail này rồi.");
                    case "//*[@text=\"Sign up with mobile number\"]":
                    case "//*[@text=\"What's your email?\"]":
                        _sate = "Nhập email";
                        await HandleEmailInput();
                        break;
                    case "//*[@text=\"What is your mobile number?\"]":
                    case "//*[@text=\"Sign up with email\"]":
                        _sate = "Nhập số điện thoại";
                        await HandlePhoneInput();
                        break;
                    case "//*[@text=\"Select your name\"]":
                    case "//*[@text=\"Choose your name\"]":
                        HandleNameSelection();
                        break;
                    case var c when XpathManagerFacebook.Get(XpathType.Success).Contains(c):
                        return new SubdyExtension(SubdyEnum.Success, "Đăng ký thành công!");
                    case "//*[@text=\"I agree\"]":
                        _sate = "Chờ xác nhận từ Facebook";
                        _client.ElementWithAttributes(currentCase, 5);
                        return await Agreement();
                    case var c when XpathManagerFacebook.Get(XpathType.MetaAdsConsent).Contains(c):
                        await FacebookHander.TryHandleMetaAdsConsentAsync(_client);
                        break;
                    case var c when XpathManagerFacebook.Get(XpathType.NavigationButton).Contains(c):
                    case var x when XpathManagerFacebook.Get(XpathType.Confim_Register).Contains(x):
                        _client.ElementWithAttributes(currentCase, 5);
                        break;
                    case "//*[@text=\"Enter the confirmation code\"]":
                        _sate = "Nhập mã xác nhận";
                        await HandleConfirmationCode();
                        break;
                    case "//*[@text=\"First name\"]":
                    case "//*[@text=\"What's your name?\"]":
                        _sate = "Nhập họ tên";
                        await HandleNameInput();
                        break;
                    case "//*[@text=\"When is your date of birth?\"]":
                        _sate = "Chọn ngày sinh";
                        HandleDateOfBirth();
                        break;
                    case "//*[@text=\"SET\"]":
                    case "//*[@text=\"Set date\"]":
                        _sate = "Chọn ngày sinh";
                        await HandleDatePicker();
                        break;
                    case "//*[@text=\"Male\"]":
                    case "//*[@text=\"Female\"]":
                    case "//*[@text=\"What is your gender?\"]":
                        _sate = "Chọn giới tính";
                        HandleGenderSelection();
                        break;
                    case "//*[@text=\"Create a password\"]":
                        _sate = "Tạo mật khẩu";
                        await HandlePasswordInput();
                        break;
                }
                await DelayMessageAsync(2, _account.Status, 2);
            }

            return new SubdyExtension(SubdyEnum.Error, "Đã xảy ra lỗi khi đăng ký.");
        }
    }

}
