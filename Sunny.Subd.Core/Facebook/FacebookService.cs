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

        private void SetStatus(string status, int color)
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
        }
        public async Task<SubdyExtension> Login(ADBClient client, Account account, CancellationToken ct, int timeout, MainService main)
        {
            SubdyEnum subyEnum = SubdyEnum.None;
            string message = "Đã xảy ra lỗi đang nhặp tài khoản!";
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
                    SetStatus($"Đang đăng nhập.", 2);
                    _case = client.FindElement("", FacebookHander.GetActiAccountFacebook(), 60);
                    if (string.IsNullOrEmpty(_case))
                    {
                        client.AppStart(FacebookHander.Package(PlatformModel.Facebook), true, true, true);
                        continue;
                    }
                    SetStatus($"Xử lý case [{_case}]...", 2);
                    switch (_case)
                    {
                        case var c when XpathManagerFacebook.Get(XpathType.Loading).Contains(c): continue;
                        case var c when XpathManagerFacebook.Get(XpathType.CP282).Contains(c):
                            subyEnum = SubdyEnum.CP_282;
                            message = $"Tài khoản bị 282. [{c}]";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerFacebook.Get(XpathType.CP956).Contains(c):
                            subyEnum = SubdyEnum.CP_956;
                            message = $"Tài khoản bị 956. [{c}]";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerFacebook.Get(XpathType.Captcha).Contains(c):
                            subyEnum = SubdyEnum.Captcha;
                            message = $"Tài khoản dính captcha. [{c}]";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerFacebook.Get(XpathType.Block).Contains(c):
                            subyEnum = SubdyEnum.Block;
                            message = $"Tài khoản bị block. [{c}]";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerFacebook.Get(XpathType.Logout).Contains(c):
                            subyEnum = SubdyEnum.LogOut;
                            message = $"Tài khoản bị đăng xuất. [{c}]";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerFacebook.Get(XpathType.Success).Contains(c):
                            subyEnum = SubdyEnum.Success;
                            message = $"Tài khoản đăng nhập thành công. [{c}]";
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
            _client.ElementWithAttributes(new List<string> { "//*[@text=\"Log into another account\"]", "//*[@text=\"Use another profile\"]" }, 3);
            string uid = _account.Uid_Email;
            var elements = _client.FindElements(10, "", "//*[@class='android.widget.EditText']");
            if (!elements.Any() || elements.Count != 2) return;
            SetStatus($"Đang nhập {uid}...", 2);
            _client.SendTextSlow("//*[@class='android.widget.EditText']", uid, xml: elements[0].OuterXml);
            SetStatus($"Đang nhập {_account.Password}...", 2);
            _client.SendTextSlow("//*[@class='android.widget.EditText']", _account.Password, xml: elements[1].OuterXml);
            _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton));
            _client.Delay(7);
            return;
        }
        private async Task ImportPassword()
        {
            SetStatus($"Đang nhập {_account.Password}...", 2);
            _client.SendTextSlow("//*[@class='android.widget.EditText']", _account.Password);
            _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton));
            _client.Delay(7);
            return;
        }
        private async Task Import2FA()
        {
            if (string.IsNullOrEmpty(_account.TowFA))
            {
                SetStatus("Không có mã 2FA để nhập.", 2);
                throw new SubdyExtension(SubdyEnum.LogOut, "Tài khoản không có 2fa...");
            }
            SetStatus($"Đang nhập 2FA {_account.TowFA}...", 2);

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
            string code = FacebookHander.GetCodeTowFA(_account.TowFA);
            _client.SendTextSlow("//*[@class='android.widget.EditText']", code);
            _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton));
            _client.Delay(7);
            return;
        }
        public Task<SubdyExtension> Reaction(ADBClient client, Account account, string type, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
        public async Task<SubdyExtension> HanderAccount(ADBClient client, Account account, int timeout, CancellationToken ct, MainService main)
        {
            SetStatus($"Kiểm tra tài khoản...", 2);
            string _case = string.Empty;
            SubdyEnum subyEnum = SubdyEnum.None;
            string message = "Đã xảy ra lỗi đang nhặp tài khoản!";
            while (true)
            {
                if (client.IsRunningApp(FacebookHander.Package(PlatformModel.Facebook)) == false)
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
                SetStatus($"Xử lý case [{_case}]...", 2);
                switch (_case)
                {
                    case var c when XpathManagerFacebook.Get(XpathType.Loading).Contains(c): continue;
                    case var c when XpathManagerFacebook.Get(XpathType.CP282).Contains(c):
                        subyEnum = SubdyEnum.CP_282;
                        message = $"Tài khoản bị 282. [{c}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.CP956).Contains(c):
                        subyEnum = SubdyEnum.CP_956;
                        message = $"Tài khoản bị 956. [{c}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.Captcha).Contains(c):
                        subyEnum = SubdyEnum.Captcha;
                        message = $"Tài khoản dính captcha. [{c}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.Block).Contains(c):
                        subyEnum = SubdyEnum.Block;
                        message = $"Tài khoản bị block. [{c}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.Logout).Contains(c):
                        subyEnum = SubdyEnum.LogOut;
                        message = $"Tài khoản bị đăng xuất. [{c}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.Success).Contains(c):
                        subyEnum = SubdyEnum.Success;
                        message = $"Tài khoản đăng nhập thành công. [{c}]";
                        return new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.InputUserName).Contains(c):
                        subyEnum = SubdyEnum.LogOut;
                        message = $"Tài khoản bị đăng xuất. [{c}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.InputPassword).Contains(c):
                        subyEnum = SubdyEnum.LogOut;
                        message = $"Tài khoản bị đăng xuất. [{c}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.TowFA).Contains(c):
                        subyEnum = SubdyEnum.LogOut;
                        message = $"Tài khoản bị đăng xuất. [{c}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerFacebook.Get(XpathType.NavigationButton).Contains(c):
                        client.ElementWithAttributes(c, 1);
                        break;
                    case var c when XpathManagerFacebook.Get(XpathType.CashApp).Contains(c):
                        await main.SessionExpired();
                        await Login(client, account, ct, timeout, main);
                        await main.ExtractAndUpdateAuthenticationInfoAsync();
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


    }
}
