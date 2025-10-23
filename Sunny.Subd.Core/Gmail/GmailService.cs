using AutoAndroid;
using FFmpeg.AutoGen;
using Sunny.Subd.Core.Facebook;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sunny.Subd.Core.Gmail
{
    public class GmailService
    {
        private ADBClient _client;
        public static string PackageGmail = "com.google.android.gm";
        public GmailService(ADBClient client)
        {
            _client = client;
        }
        private FacebookRegsiner facebook;
        public GmailService(FacebookRegsiner client)
        {
            facebook = client;
            _client = client._client;
        }
        public List<string> GetAccount()
        {
            try
            {
                string input = string.Empty;
                for (int i = 0; i < 10; i++)
                {
                    input = _client.Shell("dumpsys account");
                    if (!string.IsNullOrEmpty(input)) break;
                }

                string pattern = @"Account\s+\{name=([^\s,]+),\s*type=com\.google\}";

                MatchCollection matches = Regex.Matches(input, pattern);
                List<string> emails = new List<string>();

                foreach (Match match in matches)
                {
                    emails.Add(match.Groups[1].Value);
                }
                return emails;
            }
            catch
            {

            }
            return new List<string>();
        }
        public async Task<bool> RemoveAccount()
        {

            if (!GetAccount().Any())
            {
                return true;
            }
            facebook.SetStatus("Xóa tài khoản google", 2);
            bool check = false;
            _client.Shell("am force-stop com.android.settings");
             _client.Delay(3);
            _client.Shell("am start -a android.settings.SYNC_SETTINGS");
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < 60000)
            {
                if (!_client.ElementWithAttributes("//*[@text=\"Google\"]", 10, click: false))
                {
                    break;
                }
                if (_client.ElementWithAttributes("//*[@text=\"Google\"]", 10))
                {
                    _client.ElementWithAttributes(new List<string> { "//*[@text=\"Xóa tài khoản\"]", "//*[@text=\"Remove account\"]" }, 10);
                    _client.ElementWithAttributes(new List<string> { "//*[@resource-id=\"android:id/button1\"]", "//*[@text=\"Xóa tài khoản\"]", "//*[@text=\"Remove account\"]" }, 10);
                }
                if (!GetAccount().Any())
                {
                    return true;
                }
            }
            return GetAccount().Any();
        }
        public async Task<string> GetCode()
        {
            _client.AppStart(PackageGmail, true, true, true);

            Stopwatch stopwatch = Stopwatch.StartNew();
             _client.Delay(2);

            string[] patterns =
            {
        @"your security code is:\s*(\d+)",
        @"facebook,\s*(\d+)\s+is",
        @"\s*(\d+)\s+is",
        @"(\d+)\s+là mã bảo mật của bạn",
        @"mã bảo mật của bạn là:\s*(\d+)",
         @"\b(\d{5})\b.*mã xác nhận"
    };

            while (stopwatch.ElapsedMilliseconds < 60000)
            {
                try
                {
                     _client.SwipeByPercent(56, 82, 56, 16, 1000, 3);
                     _client.Delay(5);

                    var nodes = _client.FindElements(5, "", "//*[contains(@text, 'Facebook')]");
                    if (!nodes.Any())
                    {
                        nodes = _client.FindElements(5, "", "//*[contains(@content-desc, 'Facebook')]");
                    }
                    if (!nodes.Any())
                    {
                        continue;
                    }


                    foreach (var item in nodes)
                    {
                        string value = item.OuterXml;
                        if (string.IsNullOrEmpty(value)) continue;

                        foreach (var pattern in patterns)
                        {
                            Match match = Regex.Match(value, pattern);
                            if (match.Success)
                            {
                                return match.Groups[1].Value;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in GetCode: {ex.Message}");
                }
            }

            return string.Empty;
        }
        public async Task<bool> Login(string email, string pass)
        {
            bool check = false;
            var emails = GetAccount();
            var list = new List<string>
{
    "//*[@text=\"Accept\"]",
    "//*[@text=\"Later\"]",
    "//*[@text=\"Turn on backup\"]",
    "//*[@text=\"I agree\"]",
    "//*[@text=\"Add phone number?\"]",
    "//*[@resource-id=\"password\"]",
    "//*[@text=\"Create account\"]",
    "//*[@text=\"Welcome\"]",
    "//*[@text=\"Google\"]",
    "//*[@text=\"Add account\"]",
};
            list.Add($"//*[@text=\"{email}\"]");
            for (int i = 0; i < 5; i++)
            {

                _client.Shell("am start -a android.settings.SYNC_SETTINGS");
                 _client.Delay(3);
                Stopwatch stopwatch = Stopwatch.StartNew();
                while (stopwatch.ElapsedMilliseconds < 180000)
                {
                    facebook._sate = $"Đang đăng nhập tài khoản google [{i + 1}]";
                    string _case = _client.FindElement("", list, 30);
                    if (string.IsNullOrEmpty(_case))
                    {
                        _client.Shell("am start -a android.settings.SYNC_SETTINGS");
                        break;
                    }
                    if (_case == $"//*[@text=\"{email}\"]")
                    {
                        break;
                    }
                    facebook.SetStatus($"Xử lý [{_case}]...", 2);
                    switch (_case)
                    {
                        case "//*[@text=\"This account already exists on your device\"]":
                            {
                                break;
                            }
                        case "//*[@text=\"Accept\"]":
                        case "//*[@text=\"Later\"]":
                        case "//*[@text=\"Turn on backup\"]":
                        case "//*[@text=\"I agree\"]":
                            {
                                _client.ElementWithAttributes(_case);
                                break;
                            }
                        case "//*[@text=\"Add account\"]":
                        case "//*[@text=\"Google\"]":
                            {
                                _client.ElementWithAttributes(_case);
                                await facebook.DelayMessageAsync(5, $"Đã xử lý [{_case}]...", 2);
                                break;
                            }
                        case "//*[@text=\"Create account\"]":
                            {
                                _client.SendTextADB("//*[@class=\"android.widget.EditText\"]", email);
                                _client.ElementWithAttributes(new List<string> { "//*[@resource-id=\"identifierNext\"]", "//*[@text=\"Next\"]" });
                                break;
                            }
                        case "//*[@resource-id=\"password\"]":
                            {
                                _client.SendTextADB("//*[@resource-id=\"password\"]", pass, timeout: 10);
                                _client.ElementWithAttributes(new List<string> { "//*[@resource-id=\"passwordNext\"]", "//*[@text=\"Next\"]" });
                                break;
                            }
                        case "//*[@text=\"Welcome\"]":
                            {
                                 _client.SwipeByPercent(56, 82, 56, 16, 1000, 5);
                                // _client.Swipe(478, 1399, 531, 784, 10, 5);
                                _client.ElementWithAttributes(new List<string> { "//*[@text=\"I UNDERSTAND\"]" });
                                break;
                            }
                        case "//*[@text=\"Add phone number?\"]":
                            {
                                 _client.SwipeByPercent(56, 82, 56, 16, 1000, 5);
                                _client.ElementWithAttributes(new List<string> { "//*[@text=\"I agree\"]" }, click: true);
                                break;
                            }
                    }
                    await facebook.DelayMessageAsync(5, $"Đã xử lý [{_case}]...", 2);
                    emails = GetAccount();
                    if (emails.Any())
                    {
                        break;
                    }
                }
                emails = GetAccount();
                if (emails.Any() && emails.Contains(email.ToLower()))
                {
                    check = true;
                    break;
                }
            }
            if (check)
            {
                _client.AppStart("com.google.android.gm", true, true, true);
                await facebook.DelayMessageAsync(5, $"Đang mở app gmail", 2);
                _client.ElementWithAttributes("//*[@text=\"GOT IT\"]");
                _client.ElementWithAttributes("//*[@text=\"TAKE ME TO GMAIL\"]");
                 _client.Delay(5);
                _client.ElementWithAttributes("//*[@content-desc=\"Close\"]");
                _client.ElementWithAttributes("//*[@resource-id=\"com.google.android.gm:id/selected_account_disc_gmail\"]");
                return true;

            }

            return false;
        }
    }
}
