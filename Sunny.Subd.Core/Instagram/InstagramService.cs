using AutoAndroid;
using Sunny.Subd.Core.Facebook;
using Sunny.Subd.Core.Models;
using Sunny.Subd.Core.Services;
using Sunny.Subd.Core.Utils;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Xml;

namespace Sunny.Subd.Core.Instagram
{
    public class InstagramService : IFacebookService
    {
        private Stopwatch Stopwatch = new Stopwatch();
        private CancellationToken _ct;
        private ADBClient _client;
        private Account _account; private string _sate = string.Empty;
        private void CheckStop(int second)
        {
            if (Stopwatch.ElapsedMilliseconds > second * 1000)
            {
                throw new SubdyExtension(SubdyEnum.Stop, "Đã quá thời gian thực hiện thao tác.");
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
            string message = "Đã xảy ra lỗi đang nhặp tài khoản!";
            try
            {
                _sate = "Đăng nhập Instagram";
                _client = client ?? throw new ArgumentNullException(nameof(client), "ADBClient cannot be null");
                _account = account ?? throw new ArgumentNullException(nameof(account), "Account cannot be null");
                _ct = ct;

                Stopwatch.Restart();
                string _case = string.Empty;
                while (true)
                {
                    CheckStop(timeout);
                    SetStatus("Đang tìm cửa sổ đăng nhập...", 2);
                    _case = client.FindElement("", FacebookHander.GetActiAccountInstagram(), 120);
                    if (string.IsNullOrEmpty(_case))
                    {
                        client.AppStart(FacebookHander.Package(PlatformModel.Facebook), true, true, true);
                        continue;
                    }
                    SetStatus("Đang xử lý...", 2, logDetail: $"[InstagramService.Login] case={_case}");
                    switch (_case)
                    {
                        case var c when XpathManagerInstagram.Get(XpathType.Loading).Contains(c): continue;
                        case var c when XpathManagerInstagram.Get(XpathType.CP282).Contains(c):
                            subyEnum = SubdyEnum.CP_282;
                            message = $"Tài khoản bị 282. [{c}]";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerInstagram.Get(XpathType.CP956).Contains(c):
                            subyEnum = SubdyEnum.CP_956;
                            message = $"Tài khoản bị 956. [{c}]";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerInstagram.Get(XpathType.Captcha).Contains(c):
                            subyEnum = SubdyEnum.Captcha;
                            message = $"Tài khoản dính captcha. [{c}]";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerInstagram.Get(XpathType.Block).Contains(c):
                            subyEnum = SubdyEnum.Block;
                            message = $"Tài khoản bị block. [{c}]";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerInstagram.Get(XpathType.Logout).Contains(c):
                            subyEnum = SubdyEnum.LogOut;
                            message = $"Tài khoản bị đăng xuất. [{c}]";
                            throw new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerInstagram.Get(XpathType.Success).Contains(c):
                            subyEnum = SubdyEnum.Success;
                            message = $"Tài khoản đăng nhập thành công. [{c}]";
                            return new SubdyExtension(subyEnum, message);
                        case var c when XpathManagerInstagram.Get(XpathType.InputUserName).Contains(c):
                            await ImportUid();
                            break;
                        case var c when XpathManagerInstagram.Get(XpathType.InputPassword).Contains(c):
                            await ImportPassword();
                            break;
                        case var c when XpathManagerInstagram.Get(XpathType.TowFA).Contains(c):
                            await Import2FA();
                            break;
                        case var c when XpathManagerInstagram.Get(XpathType.NavigationButton).Contains(c):
                            client.ElementWithAttributes(c, 1);
                            break;
                        case var c when XpathManagerInstagram.Get(XpathType.CashApp).Contains(c):
                            await main.RestoreFacebookAsync();
                            return await Login(client, account, ct, timeout, main);

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
            _client.ElementWithAttributes(new List<string> { "//*[@class='android.widget.EditText']", "//*[@content-desc=\"Log into another account\"]" });
            string uid = _account.Uid_Email;
            var elements = _client.FindElements(10, "", "//*[@class='android.widget.EditText']");
            if (!elements.Any() || elements.Count != 2) return;
            SetStatus("Đang nhập tên đăng nhập...", 2);
            _client.SendTextADB("//*[@class='android.widget.EditText']", uid, xml: elements[0].OuterXml);
            SetStatus("Đang nhập mật khẩu...", 2);
            _client.SendTextADB("//*[@class='android.widget.EditText']", _account.Password, xml: elements[1].OuterXml);
            _client.ElementWithAttributes("//*[@content-desc=\"Log in\"]", 10);
            return;
        }
        private async Task ImportPassword()
        {
            _sate = "Nhập mật khẩu";
            SetStatus("Đang nhập mật khẩu...", 2);
            _client.SendTextSlow("//*[@class='android.widget.EditText']", _account.Password);
            _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton));
            return;
        }
        private async Task Import2FA()
        {
            if (string.IsNullOrEmpty(_account.TowFA))
            {
                SetStatus("Không có mã 2FA để nhập.", 2);
                throw new SubdyExtension(SubdyEnum.LogOut, "Tài khoản không có 2fa...");
            }
            _sate = "Xác thực 2 bước";
            SetStatus("Đang nhập mã xác thực...", 2);

            string element = _client.FindElement("", new List<string> { "//*[contains(@text, \"Check your notifications on another device\")]", "//*[@content-desc=\"Go to your authentication app\"]" }, 10);
            if (element == "//*[contains(@text, \"Check your notifications on another device\")]")
            {
                _client.SwipeByPercent(56, 82, 56, 16, 1000, 3);
                _client.ElementWithAttributes("//*[contains(@text, \"Try another way\")]", 10);
                _client.ElementWithAttributes("//*[contains(@text, \"Authentication app\")]", 10);
                _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton));
            }
            string code = FacebookHander.GetCodeTowFA(_account.TowFA);
            _client.SendTextSlow("//*[@class='android.widget.EditText']", code);
            _client.SwipeByPercent(56, 82, 56, 16, 1000, 3);
            _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton));
            return;
        }
        public async Task<SubdyExtension> HanderAccount(ADBClient client,Account account, int timeout, CancellationToken ct, MainService main)
        {
            Stopwatch.Restart();
            SetStatus($"Kiểm tra tài khoản...", 2);
            string _case = string.Empty;
            SubdyEnum subyEnum = SubdyEnum.None;
            string message = "Đã xảy ra lỗi đang nhặp tài khoản!";
            while (true)
            {
                CheckStop(180);
                _case = client.FindElement("", FacebookHander.GetActiAccountFacebook(), timeout);
                if (string.IsNullOrEmpty(_case))
                {
                    subyEnum = SubdyEnum.Success;
                    message = $"Không tìm thấy case phù hợp...";
                    return new SubdyExtension(subyEnum, message);
                }
                SetStatus("Đang kiểm tra...", 2, logDetail: $"[InstagramService.HanderAccount] case={_case}");
                switch (_case)
                {
                    case var c when XpathManagerInstagram.Get(XpathType.Loading).Contains(c): continue;
                    case var c when XpathManagerInstagram.Get(XpathType.CP282).Contains(c):
                        subyEnum = SubdyEnum.CP_282;
                        message = $"Tài khoản bị 282. [{c}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerInstagram.Get(XpathType.CP956).Contains(c):
                        subyEnum = SubdyEnum.CP_956;
                        message = $"Tài khoản bị 956. [{c}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerInstagram.Get(XpathType.Captcha).Contains(c):
                        subyEnum = SubdyEnum.Captcha;
                        message = $"Tài khoản dính captcha. [{c}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerInstagram.Get(XpathType.Block).Contains(c):
                        subyEnum = SubdyEnum.Block;
                        message = $"Tài khoản bị block. [{c}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerInstagram.Get(XpathType.Logout).Contains(c):
                        subyEnum = SubdyEnum.LogOut;
                        message = $"Tài khoản bị đăng xuất. [{c}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerInstagram.Get(XpathType.Success).Contains(c):
                        subyEnum = SubdyEnum.Success;
                        message = $"Tài khoản đăng nhập thành công. [{c}]";
                        return new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerInstagram.Get(XpathType.InputUserName).Contains(c):
                        subyEnum = SubdyEnum.LogOut;
                        message = $"Tài khoản bị đăng xuất. [{c}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerInstagram.Get(XpathType.InputPassword).Contains(c):
                        subyEnum = SubdyEnum.LogOut;
                        message = $"Tài khoản bị đăng xuất. [{c}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerInstagram.Get(XpathType.TowFA).Contains(c):
                        subyEnum = SubdyEnum.LogOut;
                        message = $"Tài khoản bị đăng xuất. [{c}]";
                        throw new SubdyExtension(subyEnum, message);
                    case var c when XpathManagerInstagram.Get(XpathType.NavigationButton).Contains(c):
                        client.ElementWithAttributes(c, 1);
                        break;
                    case var c when XpathManagerFacebook.Get(XpathType.CashApp).Contains(c):
                        await main.RestoreFacebookAsync();
                        return await Login(client, account, ct, timeout, main);
                }

            }
        }

        public async Task<Dictionary<string, string>> GetInfo(ADBClient client)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            try
            {
                var xpaths = new List<string>
            {
                "//*[@resource-id=\"com.instagram.android:id/profile_tab\"]",
                "//*[@content-desc=\"Profile\"]",
                   "//*[@resource-id=\"com.instagram.android:id/bio\"]",
                "//*[@resource-id=\"com.instagram.android:id/username\"]",
                "//*[@resource-id=\"com.instagram.android:id/full_name\"]",
            };
                xpaths.AddRange(XpathManagerInstagram.Combine(XpathType.CP956, XpathType.CP282, XpathType.Loading, XpathType.NavigationButton));
                SetStatus("Đang khởi động Instagram...", 2);
                client.AppStart(FacebookHander.Package("Instagram"));
                client.Delay(5);
                Stopwatch.Restart();
                string _case = string.Empty;
                while (Stopwatch.ElapsedMilliseconds < 120000)
                {
                    _sate = $"Lấy thông tin tài khoản {_account.Uid}";
                    _case = client.FindElement("", xpaths, 120);
                    if (string.IsNullOrEmpty(_case))
                    {
                        client.AppStart(FacebookHander.Package(PlatformModel.Facebook), true, true, true);
                        continue;
                    }
                    SetStatus("Đang đọc thông tin...", 2, logDetail: $"[InstagramService.GetInfo] case={_case}");
                    switch (_case)
                    {
                        case "//*[@resource-id=\"com.instagram.android:id/profile_tab\"]":
                        case "//*[@content-desc=\"Profile\"]":
                            {
                                client.ElementWithAttributes(_case);
                                client.ElementWithAttributes("//*[@text=\"Close\"]");
                                string follow = string.Empty, following = string.Empty, post = string.Empty;
                                var nodesFullname = client.FindElements(5, "", "//*[@resource-id=\"com.instagram.android:id/profile_header_familiar_post_count_value\"]");
                                if (nodesFullname != null && nodesFullname.Any())
                                {
                                    var editTextNode = nodesFullname[0].SelectSingleNode("//*[@resource-id=\"com.instagram.android:id/profile_header_familiar_post_count_value\"]");
                                    post = editTextNode?.Attributes?["text"]?.Value;
                                }
                                nodesFullname = client.FindElements(5, "", "//*[@resource-id=\"com.instagram.android:id/profile_header_familiar_followers_value\"]");
                                if (nodesFullname != null && nodesFullname.Any())
                                {
                                    var editTextNode = nodesFullname[0].SelectSingleNode("//*[@resource-id=\"com.instagram.android:id/profile_header_familiar_followers_value\"]");
                                    follow = editTextNode?.Attributes?["text"]?.Value;
                                }
                                nodesFullname = client.FindElements(5, "", "//*[@resource-id=\"com.instagram.android:id/profile_header_following_stacked_familiar\"]");
                                if (nodesFullname != null && nodesFullname.Any())
                                {
                                    var editTextNode = nodesFullname[0].SelectSingleNode("//*[@resource-id=\"com.instagram.android:id/profile_header_familiar_following_value\"]");
                                    following = editTextNode?.Attributes?["text"]?.Value;
                                }
                                result["post"] = post;
                                result["follow"] = follow;
                                result["following"] = following;

                                client.ElementWithAttributes(new List<string> { "//*[@content-desc=\"Edit profile\"]", "//*[@text=\"Edit profile\"]" }, 15);
                                break;
                            }
                        case "//*[@resource-id=\"com.instagram.android:id/bio\"]":
                        case "//*[@resource-id=\"com.instagram.android:id/username\"]":
                        case "//*[@resource-id=\"com.instagram.android:id/full_name\"]":
                            {
                                string fullname = string.Empty, username = string.Empty, bio = string.Empty;
                                client.ElementWithAttributes("//*[@resource-id=\"com.instagram.android:id/profile_tab\"]");
                                client.ElementWithAttributes("//*[@text=\"Close\"]");
                                var nodesFullname = client.FindElements(5, "", "//*[@class='android.widget.EditText']");
                                if (nodesFullname != null && nodesFullname.Count >= 3)
                                {
                                    for (int i = 0; i < nodesFullname.Count; i++)
                                    {
                                        if (i == 1)
                                        {
                                            var match = Regex.Match(nodesFullname[i].OuterXml, "text=\"(.*?)\"");
                                            if (match.Success)
                                            {
                                                fullname = match.Groups[1].Value;
                                            }

                                        }
                                        if (i == 3)
                                        {
                                            var match = Regex.Match(nodesFullname[i].OuterXml, "text=\"(.*?)\"");
                                            if (match.Success)
                                            {
                                                username = match.Groups[1].Value;
                                            }
                                        }
                                        if (i == 7)
                                        {
                                            var match = Regex.Match(nodesFullname[i].OuterXml, "text=\"(.*?)\"");
                                            if (match.Success)
                                            {
                                                bio = match.Groups[1].Value;
                                            }
                                        }
                                        try
                                        {
                                            var match = Regex.Match(nodesFullname[i].OuterXml, "text=\"(.*?)\"");
                                            if (match.Success)
                                            {
                                                Debug.WriteLine("Text là: " + match.Groups[1].Value);
                                            }
                                            XmlDocument doc = new XmlDocument();
                                            doc.LoadXml(nodesFullname[i].InnerXml);
                                            XmlNode node = doc.DocumentElement;
                                            string text = node.Attributes["text"]?.Value;
                                            Debug.WriteLine("Text là: " + text);
                                        }
                                        catch
                                        {

                                        }

                                    }
                                }

                                if (string.IsNullOrEmpty(username))
                                {
                                    continue;
                                }
                                result["fullname"] = fullname;
                                result["username"] = username;
                                result["bio"] = bio;
                                return result;
                            }
                        case var x when XpathManagerInstagram.Get(XpathType.NavigationButton).Contains(_case):
                            {
                                client.ElementWithAttributes(_case);
                                break;
                            }
                        default:
                            {
                               
                                break;
                            }
                    }
                    client.Delay(2);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
            }

            return result;
        }

        public async Task<Dictionary<string, string>> UpateInfo(ADBClient client, string fullename, string bio, string username)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            var xpaths = new List<string>
            {
                "//*[@resource-id=\"com.instagram.android:id/bio\"]",
                "//*[@resource-id=\"com.instagram.android:id/username\"]",
                "//*[@resource-id=\"com.instagram.android:id/full_name\"]",
                "//*[@resource-id=\"com.instagram.android:id/profile_tab\"]",
                "//*[@content-desc=\"Profile\"]",
            };
            xpaths.AddRange(XpathManagerInstagram.Combine(XpathType.CP956, XpathType.CP282, XpathType.Loading, XpathType.NavigationButton));
            client.AppStart(FacebookHander.Package("Instagram"));
            client.Delay(5);
            Stopwatch.Restart();
            string _case = string.Empty;
            while (Stopwatch.ElapsedMilliseconds < 60000)
            {
                _sate = $"Cập nhật thông tin tài khoản {_account.Uid}";
                _case = client.FindElement("", FacebookHander.GetActiAccountFacebook(), 120);
                if (string.IsNullOrEmpty(_case))
                {
                    client.AppStart(FacebookHander.Package(PlatformModel.Facebook), true, true, true);
                    continue;
                }
                SetStatus("Đang xử lý...", 2, logDetail: $"[InstagramService.UpateInfo] case={_case}");
                switch (_case)
                {
                    case "//*[@resource-id=\"com.instagram.android:id/profile_tab\"]":
                    case "//*[@content-desc=\"Profile\"]":
                        {
                            client.ElementWithAttributes(_case);
                            SetStatus("Đang mở trang chỉnh sửa...", 2);
                            client.ElementWithAttributes(new List<string> { "//*[@content-desc=\"Edit profile\"]", "//*[@text=\"Edit profile\"]" }, 15);
                            if (!string.IsNullOrEmpty(fullename))
                            {
                                _sate = $"Đổi tên hiển thị cho {_account.Uid}";
                                SetStatus("Đang cập nhật tên hiển thị...", 2);
                                client.ElementWithAttributes("//*[@resource-id=\"com.instagram.android:id/full_name\"]");
                                client.Delay(2);
                                client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", fullename, timeout: 15);
                                client.Delay(2);
                                client.ElementWithAttributes("//*[@content-desc=\"Done\"]");
                                client.Delay(2);
                                result["fullname"] = fullename;
                            }
                            if (!string.IsNullOrEmpty(username))
                            {
                                _sate = $"Đổi username cho {_account.Uid}";
                                SetStatus("Đang cập nhật username...", 2);
                                client.ElementWithAttributes("//*[@resource-id=\"com.instagram.android:id/username\"]");
                                client.Delay(2);
                                client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", username, timeout: 15);
                                client.Delay(2);
                                client.ElementWithAttributes("//*[@content-desc=\"Done\"]");
                                client.Delay(2);
                                result["username"] = username;
                            }
                            if (!string.IsNullOrEmpty(bio))
                            {
                                _sate = $"Cập nhật bio cho {_account.Uid}";
                                SetStatus("Đang cập nhật bio...", 2);
                                client.ElementWithAttributes("//*[@resource-id=\"com.instagram.android:id/bio\"]");
                                client.Delay(2);
                                client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", bio, timeout: 15);
                                client.Delay(2);
                                client.ElementWithAttributes("//*[@content-desc=\"Done\"]");
                                client.Delay(2);
                                result["bio"] = bio;
                            }

                            return result;
                        }
                    default:
                        {
                            break;
                        }
                }
                client.Delay(2);
            }
            return result;
        }
    }
}
