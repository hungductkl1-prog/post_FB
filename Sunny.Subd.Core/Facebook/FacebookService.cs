using AutoAndroid;
using Sunny.Subd.Core.Models;
using Sunny.Subd.Core.Services;
using Sunny.Subd.Core.Utils;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;

namespace Sunny.Subd.Core.Facebook
{
    public class FacebookService : IFacebookService
    {
        private Stopwatch Stopwatch = new Stopwatch();
        private CancellationToken _ct;
        private ADBClient _client;
        private Account _account; private string _sate = string.Empty;
        private void CheckStop(int second)
        {
            if (Stopwatch.ElapsedMilliseconds > second * 1000)
            {
                throw new SubdyExtension(SubdyEnum.Stop, "Đã quá thời gian thực hiện thao tác đăng nhập.");
            }
            if (_ct.IsCancellationRequested)
            {
                throw new SubdyExtension(SubdyEnum.Stop, "Bạn đã dừng thực hiện việc thao tác.");
            }
        }

        private void SetStatus(string status, int color, string logDetail = null)
        {
            if (!string.IsNullOrEmpty(_sate))
            {
                status = $"[{_sate}] - ({status})";
            }
            if (_account != null)
            {
                _account.Status = status;
                _account.RecentInteraction = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
                _account.ColorType = color;
            }
            if (_client?.Device != null)
            {
                _client.Device.Status = status;
                _client.Device.TypeColor = color;
            }
            if (!string.IsNullOrEmpty(logDetail) && _client != null)
            {
                _client.LogHelper.SUCCESS(logDetail);
            }
        }
        public async Task<SubdyExtension> Login(ADBClient client, Account account, CancellationToken ct, int timeout, MainService main)
        {
            SubdyEnum subyEnum = SubdyEnum.None;
            string message = "Lỗi trong quá trình đăng nhập tài khoản!";
            try
            {
                _sate = "Đăng nhập Facebook";
                _client = client ?? throw new ArgumentNullException(nameof(client), "ADBClient cannot be null");
                _account = account ?? throw new ArgumentNullException(nameof(account), "Account cannot be null");
                _ct = ct;
                Stopwatch.Restart();
                string _case = string.Empty;
                while (true)
                {
                    CheckStop(timeout);
                    SetStatus("Đang tìm cửa sổ đăng nhập...", 2);
                    _case = client.FindElement("", FacebookHander.GetActiAccountFacebook(), 60);
                    if (string.IsNullOrEmpty(_case))
                    {
                        client.AppStart(FacebookHander.Package(PlatformModel.Facebook), true, true, true);
                        continue;
                    }
                    SetStatus("Đang xử lý...", 2, logDetail: $"[FacebookService.Login] case={_case}");
                    switch (_case)
                    {
                        case var c when XpathManagerFacebook.Get(XpathType.Loading).Contains(c): continue;
                        case var c when XpathManagerFacebook.Get(XpathType.CP282).Contains(c):
                            subyEnum = SubdyEnum.CP_282;
                            message = "Tài khoản bị checkpoint 282.";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerFacebook.Get(XpathType.CP956).Contains(c):
                            subyEnum = SubdyEnum.CP_956;
                            message = "Tài khoản bị checkpoint 956.";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerFacebook.Get(XpathType.Captcha).Contains(c):
                            subyEnum = SubdyEnum.Captcha;
                            message = "Tài khoản bị yêu cầu captcha.";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerFacebook.Get(XpathType.Block).Contains(c):
                            subyEnum = SubdyEnum.Block;
                            message = "Tài khoản bị chặn.";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerFacebook.Get(XpathType.Logout).Contains(c):
                            subyEnum = SubdyEnum.LogOut;
                            message = "Tài khoản bị đăng xuất.";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerFacebook.Get(XpathType.Success).Contains(c):
                            subyEnum = SubdyEnum.Success;
                            message = "Đăng nhập thành công.";
                            return new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerFacebook.Get(XpathType.InputUserName).Contains(c):
                            await ImportUid();
                            break;
                        case var c when XpathManagerFacebook.Get(XpathType.InputPassword).Contains(c):
                            await ImportPassword();
                            break;
                        case var c when XpathManagerFacebook.Get(XpathType.TowFA).Contains(c):
                            await Import2FA();
                            break;
                        case var c when XpathManagerFacebook.Get(XpathType.NavigationButton).Contains(c):
                            client.ElementWithAttributes(c, 1);
                            break;
                        case var c when XpathManagerFacebook.Get(XpathType.CashApp).Contains(c):

                            await main.SessionExpired();
                            break;
                        case var c when XpathManagerFacebook.Get(XpathType.No_Internet).Contains(c):
                            await HandleNoInternet();
                            break;
                    }

                }
            }
            finally
            {
                Stopwatch.Stop();
            }




            throw new SubdyExtension(subyEnum, message);
        }
        private async Task ImportUid()
        {
            _sate = "Nhập tài khoản";
            SetStatus("Đang chọn đăng nhập tài khoản khác...", 2);
            _client.ElementWithAttributes(new List<string> { "//*[@text=\"Log into another account\"]", "//*[@text=\"Use another profile\"]" }, 3);
            string uid = _account.Uid_Email;
            var elements = _client.FindElements(10, "", "//*[@class='android.widget.EditText']");
            if (!elements.Any() || elements.Count != 2) return;
            SetStatus("Đang nhập tên đăng nhập...", 2);
            _client.SendTextSlow("//*[@class='android.widget.EditText']", uid, xml: elements[0].OuterXml);
            SetStatus("Đang nhập mật khẩu...", 2);
            _client.SendTextSlow("//*[@class='android.widget.EditText']", _account.Password, xml: elements[1].OuterXml);
            SetStatus("Đang xác nhận đăng nhập...", 2);
            _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton));
            SetStatus("Đợi phản hồi từ Facebook...", 2);
            _client.Delay(7);
            return;
        }
        private async Task ImportPassword()
        {
            _sate = "Nhập mật khẩu";
            SetStatus("Đang nhập mật khẩu...", 2);
            _client.SendTextSlow("//*[@class='android.widget.EditText']", _account.Password);
            SetStatus("Đang xác nhận...", 2);
            _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton));
            SetStatus("Đợi phản hồi từ Facebook...", 2);
            _client.Delay(7);
            return;
        }
        private async Task Import2FA()
        {
            if (string.IsNullOrEmpty(_account.TowFA))
            {
                SetStatus("Tài khoản không có mã 2FA để nhập.", 2);
                throw new SubdyExtension(SubdyEnum.LogOut, "[FacebookService.Import2FA] Tài khoản không có mã 2FA, không thể xác thực.");
            }
            _sate = "Xác thực 2 bước";
            SetStatus("Đang chuẩn bị xác thực 2 bước...", 2);

            string element = _client.FindElement("", new List<string> { "//*[@content-desc='Try another way']", "//*[@text=\"OK\"]", "//*[@class='android.widget.EditText']" }, 10);
            if (element == "//*[@content-desc='Try another way']")
            {
                _client.ElementWithAttributes(element, 10);
                _client.ElementWithAttributes(new List<string> {
                    "//*[@content-desc='Authentication app, Get a code from your authentication app.']",
                    "//*[@text=\"Authentication app\"]",
                    "//*[@text=\"Get a code from your authentication app.\"]"
                }, 10);
                _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton));
            }
            else if (element == "//*[@text=\"OK\"]")
            {
                _client.ElementWithAttributes("//*[@text=\"OK\"]", 10);
            }
            SetStatus("Đang lấy mã xác thực...", 2);
            string code = FacebookHander.GetCodeTowFA(_account.TowFA);
            SetStatus("Đang nhập mã xác thực...", 2);
            _client.SendTextSlow("//*[@class='android.widget.EditText']", code);
            SetStatus("Đang xác nhận mã...", 2);
            _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton));
            SetStatus("Đợi phản hồi từ Facebook...", 2);
            _client.Delay(7);
            return;
        }
        public Task<SubdyExtension> Reaction(ADBClient client, Account account, string type, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
        public async Task<SubdyExtension> HanderAccount(ADBClient client, Account account, int timeout, CancellationToken ct, MainService main)
        {
            SetStatus("Đang kiểm tra trạng thái tài khoản...", 2);
            _sate = "Kiểm tra trạng thái tài khoản";
            string _case = string.Empty;
            SubdyEnum subyEnum = SubdyEnum.None;
            string message = "Lỗi trong quá trình kiểm tra tài khoản!";
            while (true)
            {
                if (client.IsRunningApp(FacebookHander.Package(PlatformModel.Facebook)) == false && !client.ElementWithAttributes("//*[@text=\"Close app\"]"))
                {
                    client.AppStart(FacebookHander.Package(PlatformModel.Facebook), true, true, true);
                    client.Delay(5);
                    continue;
                }
                _case = client.FindElement("", FacebookHander.GetActiAccountFacebook(), timeout);
                if (string.IsNullOrEmpty(_case))
                {
                    subyEnum = SubdyEnum.Success;
                    message = $"Không tìm thấy case phù hợp...";
                    return new SubdyExtension(subyEnum, message);
                }
                SetStatus("Đang kiểm tra...", 2, logDetail: $"[FacebookService.HanderAccount] case={_case}");
                switch (_case)
                {
                    case var c when XpathManagerFacebook.Get(XpathType.Loading).Contains(c): continue;
                    case var c when XpathManagerFacebook.Get(XpathType.CP282).Contains(c):
                        subyEnum = SubdyEnum.CP_282;
                        message = "Tài khoản bị checkpoint 282.";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.CP956).Contains(c):
                        subyEnum = SubdyEnum.CP_956;
                        message = "Tài khoản bị checkpoint 956.";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.Captcha).Contains(c):
                        subyEnum = SubdyEnum.Captcha;
                        message = "Tài khoản bị yêu cầu captcha.";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.Block).Contains(c):
                        subyEnum = SubdyEnum.Block;
                        message = "Tài khoản bị chặn.";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.Logout).Contains(c):
                        subyEnum = SubdyEnum.LogOut;
                        message = "Tài khoản bị đăng xuất.";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.Success).Contains(c):
                        subyEnum = SubdyEnum.Success;
                        message = "Đăng nhập thành công.";
                        return new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.InputUserName).Contains(c):
                        subyEnum = SubdyEnum.LogOut;
                        message = "Tài khoản bị đăng xuất.";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.InputPassword).Contains(c):
                        subyEnum = SubdyEnum.LogOut;
                        message = "Tài khoản bị đăng xuất.";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.TowFA).Contains(c):
                        subyEnum = SubdyEnum.LogOut;
                        message = "Tài khoản bị đăng xuất.";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.NavigationButton).Contains(c):
                        client.ElementWithAttributes(c, 1);
                        break;
                    case var c when XpathManagerFacebook.Get(XpathType.CashApp).Contains(c):
                        await main.SessionExpired();
                        await Login(client, account, ct, timeout, main);
                        await main.ExtractAndUpdateAuthenticationInfoAsync();
                        break;
                    case var c when XpathManagerFacebook.Get(XpathType.No_Internet).Contains(c):
                        await HandleNoInternet();
                        break;
                }

            }
        }

        public Task<Dictionary<string, string>> GetInfo(ADBClient client)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, string>> UpateInfo(ADBClient client, string fullename, string bio, string username)
        {
            throw new NotImplementedException();
        }


        private async Task HandleNoInternet()
        {
            _sate = "Xử lý mất kết nối internet";
            SetStatus("Phát hiện mất kết nối internet, đang xử lý...", 2);
            int maxRetry = 5;
            for (int i = 1; i <= maxRetry; i++)
            {
                SetStatus($"Tắt wifi, thử lần {i}/{maxRetry}...", 2);
                await _client.DisableWifi();
                _client.Delay(10);

                SetStatus($"Bật wifi, thử lần {i}/{maxRetry}...", 2);
                await _client.EnableWifi();
                _client.Delay(20);

                SetStatus($"Kiểm tra IP lần {i}/{maxRetry}...", 2);
                string ip = await _client.GetIp();
                if (!string.IsNullOrEmpty(ip))
                {
                    SetStatus($"Đã kết nối lại internet, IP: {ip}", 2);
                    return;
                }
            }

            SetStatus("Thử wifi 5 lần thất bại, đang khởi động lại thiết bị...", 2);
            _client.RebootAndWaitForDeviceReady();
            _client.Delay(30);

            string ipAfterReboot = await _client.GetIp();
            if (!string.IsNullOrEmpty(ipAfterReboot))
            {
                SetStatus($"Sau khởi động lại đã có internet, IP: {ipAfterReboot}", 2);
                return;
            }

            SetStatus("Không có internet sau khởi động lại, dừng luồng thiết bị.", 3);
            throw new SubdyExtension(SubdyEnum.No_Internet, "[FacebookService.HandleNoInternet] Thiết bị không có internet sau khi thử wifi 5 lần và khởi động lại.");
        }
    }
}
