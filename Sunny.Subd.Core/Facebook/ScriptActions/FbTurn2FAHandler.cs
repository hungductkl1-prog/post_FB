using AutoAndroid;
using Sunny.Subd.Core.Models;
using Sunny.Subd.Core.Utils;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Sunny.Subd.Core.Facebook.ScriptActions
{
    public class FbTurn2FAHandler : IActionHandler
    {
        private List<string> xpaths = new List<string>
        {
        "//*[@text=\"Check your email\"]",
        "//*[@content-desc=\"Done\"]",
        "//*[@text=\"Done\"]",
        "//*[@text=\"You can't make this change right now\"]",
        "//*[@text=\"Get a code from an authentication app\"]",
        "//*[@text=\"Copy key\"]",
        "//*[@text=\"2. Scan this barcode/QR code or copy the key\"]",
        "//*[@text=\"Please re-enter your password\"]",
        "//*[contains(@text, \"Authentication app (Recommended)\")]",
        "//*[@text=\"Two-factor authentication\"]"
        };
        private Stopwatch stopwatch = Stopwatch.StartNew();
        public string TypeAction => Sunny.Subdy.Common.Models.TypeAction.FB_Turn2FA;

        public async Task<SubdyExtension> ExecuteAsync(Account account, ADBClient device, JsonHelper settingScript, JsonHelper settingAction, JsonHelper settingGeneral)
        {
            xpaths.AddRange(XpathManagerFacebook.Combine(XpathType.CP282, XpathType.Captcha, XpathType.NavigationButton));
            stopwatch.Restart();
            while (stopwatch.ElapsedMilliseconds < 300000)
            {
                string _case = device.FindElement("", xpaths, 30);
                if (string.IsNullOrEmpty(_case))
                {
                    device.StopApp(FacebookHander.Package(PlatformModel.Facebook));
                    device.Shell("am start -a android.intent.action.VIEW -d \"fb://facewebmodal/f?href=https://accountscenter.facebook.com/password_and_security/two_factor\"");
                     device.Delay(5);
                    continue;
                }
                switch (_case)
                {
                    case var c when XpathManagerFacebook.Get(XpathType.NavigationButton).Contains(c):
                        device.ElementWithAttributes(c);
                        break;
                    case var c when XpathManagerFacebook.Get(XpathType.CP282).Contains(c):
                    case var x when XpathManagerFacebook.Get(XpathType.Captcha).Contains(x):
                        {
                            return new SubdyExtension(SubdyEnum.CP_282, $"Tài khoản bị. [{_case}]");
                        }
                    case "//*[@text=\"Two-factor authentication\"]":
                        {
                            var xml = device.FindElements(10, "", "//*[contains(@text, 'Facebook')]");
                            device.ElementWithAttributes("//*[contains(@text, 'Facebook')]", 10, xml.Last().OuterXml);
                            break;
                        }
                    case "//*[contains(@text, \"Authentication app (Recommended)\")]":
                        {
                            device.ElementWithAttributes(_case, 5);
                            device.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), 5);
                            break;
                        }
                    case "//*[@text=\"Please re-enter your password\"]":
                        {
                             device.SendTextSlow("//*[@text=\"Password\"]", account.Password, timeout: 10);
                            device.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), 15);
                            break;
                        }
                    case "//*[@text=\"Copy key\"]":
                    case "//*[@text=\"2. Scan this barcode/QR code or copy the key\"]":
                        {
                            string _2FA = Get2FA(device, account.TowFA);
                            device.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), 15);
                            account.TowFA = _2FA;
                            break;
                        }
                    case "//*[@text=\"You can't make this change right now\"]":
                        {
                            return new SubdyExtension(SubdyEnum.Stop, $"Đã bị chặn khi bật 2FA. [{_case}]");
                        }
                    case "//*[@text=\"Get a code from an authentication app\"]":
                        {
                            string code = FacebookHander.GetCodeTowFA(account.TowFA);
                             device.SendTextSlow("//*[@class=\"android.widget.EditText\"]", code, timeout: 10);
                            device.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), 15);
                             device.Delay(10);
                            break;
                        }
                    case "//*[@text=\"Check your email\"]":
                        {
                            return new SubdyExtension(SubdyEnum.Error, "ImportCodeMail");
                        }
                    case "//*[@content-desc=\"Done\"]":
                    case "//*[@text=\"Done\"]":
                        {
                            device.ElementWithAttributes(_case, 10);
                            new AccountContext().Update(account);
                            return new SubdyExtension(SubdyEnum.Success, "Bật 2FA thành công.");
                        }
                }
            }
            return new SubdyExtension(SubdyEnum.Stop, "Đã xảy ra lỗi khi bật 2FA");
        }

        private string Get2FA(ADBClient device, string _2fa)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < 60000)
            {
                string xml = device.GetXMLSource();
                if (string.IsNullOrEmpty(xml)) continue;
                string pattern = @"\b([A-Z0-9]+\s[A-Z0-9]+\s[A-Z0-9]+\s[A-Z0-9]+\s[A-Z0-9]+\s[A-Z0-9]+\s[A-Z0-9]+\s[A-Z0-9]+)\b";

                // Tìm chuỗi khớp
                Match match = Regex.Match(xml, pattern);

                if (match.Success)
                {
                    if (!string.IsNullOrEmpty(FacebookHander.GetCodeTowFA(_2fa)))
                    {
                        return match.Value;
                    }
                }
            }
            return string.Empty;
        }
    }
}
