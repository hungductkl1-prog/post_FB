using AutoAndroid;
using Sunny.Subd.Core.Email;
using Sunny.Subd.Core.Models;
using Sunny.Subd.Core.Utils;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;

namespace Sunny.Subd.Core.Facebook.ScriptActions
{
    internal class FbChangeMail
    {
        private List<string> xpaths = new List<string>
        {
            "//*[@text=\"Enter your confirmation code\"]",
            "//*[@text=\"Add an email address\"]",
            "//*[@text=\"Add email\"]",
            "//*[@text=\"Add new contact Collapsed\"]"
        };
        private Stopwatch stopwatch = Stopwatch.StartNew();
        public string TypeAction => Sunny.Subdy.Common.Models.TypeAction.FB_ChangeAvatar;
        private string _email;
        private string siteEmail;
        private string tokenMail;
        private string _emailTemp;
        private EmailService _emailService;
        private ADBClient _client;
        private async Task<string> GetMail()
        {
            return await _emailService.GetEmail(tokenMail);

        }
        private async Task<string> GetCode()
        {
            int timeout = 180;
            int interval = 2000;
            DateTime startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalSeconds < timeout)
            {
                string code = await _emailService.GetCode(_emailTemp, "");

                if (!string.IsNullOrEmpty(code)) return code;

                _client.LogHelper.ERROR("Chưa nhận được mã xác nhận.");
                await Task.Delay(interval);
            }

            return string.Empty;
        }
        public async Task<SubdyExtension> ExecuteAsync(Account account, ADBClient device, string typeMail, string tokenMail)
        {
            siteEmail = typeMail;
            this.tokenMail = tokenMail;
            _client = device;
            _emailService = new EmailService(siteEmail);


            xpaths.AddRange(XpathManagerFacebook.Combine(XpathType.CP282, XpathType.Captcha, XpathType.NavigationButton));
            device.StopApp(FacebookHander.Package(PlatformModel.Facebook));
            device.Shell("am start -a android.intent.action.VIEW -d \"fb://facewebmodal/f?href=https://accountscenter.facebook.com/personal_info/contact_points\"");
            stopwatch.Restart();
            while (stopwatch.ElapsedMilliseconds < 300000)
            {
                if(device.ElementWithAttributes("//*[@text=\"You’ve added your email to the accounts selected\"]"))
                {
                    var context = new AccountContext();
                    account.Email = _emailTemp;
                    context.Update(account);
                    return new SubdyExtension(SubdyEnum.Success, "Thêm mail thành công");
                }
                string _case = device.FindElement("", xpaths, 30);
                if (string.IsNullOrEmpty(_case))
                {
                    device.StopApp(FacebookHander.Package(PlatformModel.Facebook));
                    device.Shell("am start -a android.intent.action.VIEW -d \"fb://facewebmodal/f?href=https://accountscenter.facebook.com/personal_info/contact_points\"");
                     device.Delay(5);
                    continue;
                }
                switch (_case)
                {
                    case "//*[@text=\"Add email\"]":
                    case "//*[@text=\"Add new contact Collapsed\"]":
                    case "//*[contains(@content-desc, 'Profile picture')]":
                    case var c when XpathManagerFacebook.Get(XpathType.NavigationButton).Contains(c):
                        device.ElementWithAttributes(_case);
                        break;
                    case "//*[@text=\"Add an email address\"]":
                        {
                            _emailTemp = await GetMail();
                            if (string.IsNullOrEmpty(_emailTemp))
                            {
                                account.Status = "Chưa get được mail...";
                                device.LogHelper.ERROR("Chưa get được mail...");

                                continue;
                            }
                            device.SendTextADB("//*[@class=\"android.widget.EditText\"]", _emailTemp, timeout: 10);
                            device.ElementWithAttributes(new List<string>
                            {
                                "//*[@class=\"android.widget.CheckBox\"]",
                                "//*[@content-desc=\"Facebook\"]"
                            }, 10);
                            device.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), 10);
                             device.Delay(5);
                            break;
                        }
                    case var c when XpathManagerFacebook.Get(XpathType.CP282).Contains(c):
                    case var x when XpathManagerFacebook.Get(XpathType.Captcha).Contains(x):
                        {
                            return new SubdyExtension(SubdyEnum.CP_282, $"Tài khoản bị. [{_case}]");
                        }
                    case "//*[@text=\"Enter your confirmation code\"]":
                        {
                            string code = await GetCode();
                            if (string.IsNullOrEmpty(code))
                            {

                            }
                            device.SendTextADB("//*[@class=\"android.widget.EditText\"]", code, timeout: 10);
                            device.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), 10);
                             device.Delay(10);
                            break;
                        }
                }
            }
            return new SubdyExtension(SubdyEnum.Stop, "Đã xảy ra lỗi khi thay ảnh đại diện.");
        }
    }
}
