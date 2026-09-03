using Sunny.Subd.Core.Models;
using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace Sunny.Subd.Core.Utils
{
    public class XpathManagerFacebook
    {
        private static readonly ConcurrentDictionary<XpathType, List<string>> _xpathGroups = new();
        private static readonly object _loadLock = new();
        private static Task? _loadTask;
        static XpathManagerFacebook()
        {
            _xpathGroups.TryAdd(XpathType.Captcha, new List<string>
      {
          $"//*[contains(@text, \"Enter the characters you see\")]",
      });
            _xpathGroups.TryAdd(XpathType.No_Internet, new List<string>
      {
          $"//*[@text=\"Page isn't available right now\"]",
          $"//*[@text=\"Refresh\"]",
      });
            _xpathGroups.TryAdd(XpathType.CP282, new List<string>
          {
              "//*[@content-desc=\"Start security steps\"]",
              "//*[@text=\"Start video selfie\"]",
             "//*[@text=\"Your selfie will only be used to confirm your identity and to keep our community safe.\"]",
              "//*[@text=\"It will be deleted within 30 days.\"]",
             "//*[@text=\"Type the text\"]",
             "//*[contains(@content-desc, \"confirm you're human to use your account\")]",
             $"//*[contains(@text, \"Record a video of yourself\")]",
             $"//*[contains(@text, \"Vietnam (+84)\")]",
             $"//*[contains(@text, \"United States of America (+1)\")]",
             $"//*[contains(@text, \"Type the text\")]",
             $"//*[contains(@text, \"We disabled your account\")]",
             $"//*[contains(@text, \"Access Denied\")]",
             $"//*[contains(@text, \"Appeal\")]",
             $"//*[contains(@text, \"Send by SMS\")]",
             "//*[@text=\"Enter text\"]",
             "//*[@text=\"This page is currently not displayed\"]",
             $"//*[contains(@text, \"we suspended your account\")]",
             $"//*[contains(@text, \"read more about this rule\")]",
             $"//*[contains(@text, \"Upload image or take photo\")]",
             $"//*[contains(@text, \"Account Temporarily Unavailable\")]",
             // Bug 5: Màn hình "Kiểm tra tài khoản" / Srcool khi đăng story lần 2
             $"//*[contains(@text, \"Review Recent Login\")]",
             $"//*[contains(@text, \"Confirm Your Identity\")]",
             $"//*[contains(@text, \"Verify your identity\")]",
             $"//*[contains(@text, \"We need to verify your account\")]",
             $"//*[contains(@text, \"Help us confirm\")]",
             $"//*[contains(@text, \"Confirm your account\")]",
             $"//*[contains(@text, \"Review Your Account\")]",
             $"//*[contains(@text, \"Secure Your Account\")]",
             $"//*[contains(@text, \"Protect Your Account\")]",
             $"//*[contains(@text, \"Account Review\")]",
          });
            _xpathGroups.TryAdd(XpathType.CP956, new List<string>
          {
             $"//*[contains(@text, \"Check your WhatsApp messages\")]",
             $"//*[contains(@text, \"check your email\")]",
          });
            _xpathGroups.TryAdd(XpathType.Block, new List<string>
          {
             $"//*[contains(@text, \"Dismiss\")]",
             $"//*[contains(@text, \"We limit how often you can post\")]",
             $"//*[contains(@text, \"Your account is restricted\")]",
             $"//*[contains(@text, \"we added restrictions to your account\")]",
          });
            _xpathGroups.TryAdd(XpathType.Success, new List<string>
          {
            "//*[contains(@content-desc, 'Go to profile')]",
            //"//*[@text=\"Add a profile picture\"]",
            //"//*[@text=\"Add a mobile number to your account\"]",
            //"//*[@content-desc=\"News Feed\"]",
            //"//*[@content-desc=\"Home\"]",
            //"//*[contains(@content-desc, 'Newsfeed')]",
            //"//*[@content-desc=\"Marketplace\"]",
            //"//*[@content-desc=\"Notifications\"]",
            //"//*[@content-desc=\"Watch\"]",
            //"//*[@content-desc=\"Menu\"]",
            //"//*[@content-desc=\"Search Facebook\"]",
            //"//*[contains(@content-desc, 'What')]",
          });
            _xpathGroups.TryAdd(XpathType.Loading, new List<string>
      {
          "//*[@content-desc=\"Đang tải\"]",
      });
            _xpathGroups.TryAdd(XpathType.Logout, new List<string>
      {
          "//*[contains(@text, \"You've been logged out\")]",
          $"//*[contains(@text, \"Please log in again\")]",
          $"//*[contains(@text, \"Log in to continue\")]",
          $"//*[contains(@text, \"Your session has expired\")]",
      });
            _xpathGroups.TryAdd(XpathType.WrongPassword, new List<string>
      {
          "//*[contains(@text, \"Unable to log in\")]",
          $"//*[contains(@text, \"Wrong Credentials\")]",
          $"//*[contains(@text, \"Invalid username or password\")]",
          $"//*[contains(@text, \"The password you entered is incorrect\")]",
          $"//*[contains(@text, \"incorrect password\")]",
          $"//*[contains(@text, \"password is incorrect\")]",
      });
            _xpathGroups.TryAdd(XpathType.NavigationButton, new List<string>
      {
                "//*[contains(@text, \"Alow all\")]",
              "//*[contains(@content-desc, \"Alow all\")]",
             "//*[contains(@text, \"Use for free with\")]",
              "//*[contains(@content-desc, \"Use for free with\")]",
          "//*[@content-desc=\"Manage quiet mode\"]",
          "//*[@text=\"Manage quiet mode\"]",

          "//*[@content-desc=\"End quiet mode\"]",
          "//*[@text=\"End quiet mode\"]",

          "//*[@text=\"Dismiss\"]",
         "//*[@text=\"I already have a profile\"]",
         "//*[@text=\"Use another profile\"]",
          "//*[@content-desc=\"I already have an account\"]",
          "//*[@text=\"Continue using English (US)\"]",
          "//*[@text=\"Get started\"]",
          "//*[@text=\"Log in\"]",
          "//*[@content-desc=\"Log in\"]",
          "//*[@text=\"Deny\"]",
          "//*[@content-desc=\"Next\"]",
          "//*[@text=\"Tiếp\"]",
          "//*[@text=\"Next\"]",
          "//*[@text=\"Bỏ qua\"]",
          "//*[@text=\"Skip\"]",
          "//*[@text=\"Lúc khác\"]",
          "//*[@text=\"Later\"]",
          "//*[@text=\"Not now\"]",
          "//*[@text=\"Later\"]",
          "//*[@text=\"Don't allow\"]",
          "//*[@content-desc=\"Not now\"]",
          "//*[@content-desc=\"Save\"]",
          "//*[@content-desc=\"Dismiss\"]",
          "//*[@text=\"OK\"]",
          "//*[@content-desc=\"Continue in English (US)\"]",
          "//*[@content-desc=\"Continue\"]",
          "//*[@text=\"Continue\"]",
          "//*[@text=\"Close app\"]",
          "//*[@text=\"SAVE\"]",
      });
            _xpathGroups.TryAdd(XpathType.TowFA, new List<string>
      {
           "//*[@content-desc=\"Check your email\"]",
          "//*[@text=\"Check your notifications on another device\"]",
          $"//*[contains(@text, \"Generate a code from your authentication app and enter it to log in\")]",
          $"//*[contains(@text, \"Check your notifications on another device\")]",
          $"//*[contains(@text, \"Authentication app, Get a code from your authentication app.\")]",
          $"//*[contains(@text, \"Go to your authentication app\")]",
      });
            _xpathGroups.TryAdd(XpathType.CashApp, new List<string>
      {
          $"//*[contains(@text, \"Session Expired\")]",
          "//*[@text=\"Facebook keeps stopping\"]",
      });
            _xpathGroups.TryAdd(XpathType.InputUserName, new List<string>
      {
          "//*[@text=\"Use another profile\"]",
          $"//*[contains(@text, \"Phone or email\")]",
          $"//*[contains(@text, \"Mobile number or email\")]",
          $"//*[@text=\"Log into another account\"]",
          "//*[@text=\"Mobile number or email\"]",
          $"//*[contains(@text, \"Create new account\")]",
      });
            _xpathGroups.TryAdd(XpathType.InputPassword, new List<string>
      {
                 "//*[@content-desc=\"Password\"]",
          $"//*[contains(@text, \"Enter Password\")]",
      });
            _xpathGroups.TryAdd(XpathType.Regsiner_Facebook, new List<string>
            {

                "//*[@text=\"Male\"]",
                "//*[@text=\"Female\"]",
                "//*[@text=\"Add a mobile number to your account\"]",
                "//*[@text=\"Add a profile picture\"]",
                "//*[@text=\"Save login info?\"]",
                "//*[@text=\"Add friends\"]",
                "//*[@text=\"Turn on contact uploading\"]",
                "//*[@text=\"What is your mobile number?\"]",
                "//*[@text=\"Sign up with email\"]",
                "//*[@text=\"Choose your name\"]",
                "//*[@content-desc=\"Go to profile\"]",
                "//*[@text=\"This page is currently not displayed\"]",
                "//*[@content-desc=\"Appeal\"]",
                "//*[@text=\"Enter text\"]",
                "//*[@text=\"I agree\"]",
                "//*[@text=\"Select your name\"]",
                "//*[@text=\"Couldn't create account\"]",
                "//*[@text=\"Enter email\"]",
                "//*[@text=\"Confirm with email\"]",
                "//*[@text=\"I didn't receive a code\"]",
                "//*[@text=\"Please log in again.\"]",
                "//*[@text=\"IMPORT CONTACTS\"]",
                "//*[@text=\"Sign up\"]",
                "//*[@text=\"Get started\"]",
                "//*[@text=\"Create new account\"]",
                "//*[@content-desc=\"Create new account\"]",
                "//*[@content-desc=\"Join Facebook\"]",
                "//*[@content-desc=\"No, create new account\"]",
                "//*[@content-desc=\"Create new Facebook account\"]",
                "//*[@text=\"Continue creating account\"]",
                "//*[@content-desc=\"Continue creating account\"]",
                "//*[@text=\"No, create account\"]",
                "//*[@text=\"First name\"]",
                "//*[@text=\"What's your name?\"]",
                "//*[@text=\"When is your date of birth?\"]",
                "//*[@text=\"SET\"]",
                "//*[@text=\"What is your gender?\"]",
                "//*[@text=\"What is your email?\"]",
                "//*[@text=\"Sign up with mobile number\"]",
                "//*[@text=\"Create a password\"]",
                "//*[contains(@text, \"sent to\")]",

            });
            _xpathGroups.TryAdd(XpathType.ExistEmail, new List<string>
            {
                "//*[@text=\"This Page Isn't Available Right Now\"]",
                "//*[@text=\"There is already an account linked to this email address.\"]",
                "//*[@text=\"An account already exists linked to this email.\"]",
            });
            _xpathGroups.TryAdd(XpathType.Confim_Register, new List<string>
            {
                "//*[@text=\"Continue creating account\"]",
 "//*[@text=\"Confirm by email\"]",
 "//*[@text=\"Enter an email\"]",
 "//*[@text=\"I didn’t get the code\"]",
 "//*[@text=\"Continue using English (US)\"]",

 "//*[@text=\"Enter email\"]",
 "//*[@text=\"Confirm with email\"]",
 "//*[@text=\"I didn't receive a code\"]",
 "//*[@text=\"I agree\"]",
 "//*[@text=\"Please log in again.\"]",
 "//*[@text=\"Sign up\"]",
 "//*[@text=\"Create new account\"]",
 "//*[@text=\"Create new account\"]",
 "//*[@content-desc=\"Create new account\"]",
      "//*[@text=\"Get started\"]",
 "//*[@content-desc=\"Join Facebook\"]",
 "//*[@text=\"Get started\"]",
 "//*[@content-desc=\"No, create new account\"]",
            });
        }
        public static List<string> Get(XpathType group)
        {
            return _xpathGroups.TryGetValue(group, out var list) ? new List<string>(list) : new List<string>();
        }

        public static List<string> Combine(params object[] groupsOrXpaths)
        {
            var result = new List<string>();
            foreach (var item in groupsOrXpaths)
            {
                if (item is XpathType groupName)
                    result.AddRange(Get(groupName));
                else if (item is IEnumerable<string> list)
                    result.AddRange(list);
            }
            return result;
        }

        public static void AddCustomGroup(XpathType key, List<string> xpaths)
        {
            _xpathGroups[key] = xpaths;
        }

        public static Task LoadFromApiAsync(string apiUrl = "https://dev.subdy.net/api/case")
        {
            lock (_loadLock)
            {
                return _loadTask ??= LoadFromApiCoreAsync(apiUrl);
            }
        }

        private static async Task LoadFromApiCoreAsync(string apiUrl)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                var json = await client.GetStringAsync(apiUrl);
                var root = JsonNode.Parse(json);

                if (root?["success"]?.GetValue<bool>() != true) return;

                var dataArray = root["data"]?.AsArray();
                if (dataArray == null) return;

                foreach (var item in dataArray)
                {
                    var casename = item?["casename"]?.GetValue<string>();
                    var listcase = item?["listcase"]?.AsArray();

                    if (casename == null || listcase == null) continue;
                    if (!Enum.TryParse<XpathType>(casename, true, out var xpathType)) continue;

                    var incoming = listcase
                        .Select(x => x?.GetValue<string>())
                        .Where(x => x != null)
                        .Select(x => x!)
                        .ToList();

                    if (_xpathGroups.TryGetValue(xpathType, out var existing))
                    {
                        foreach (var xpath in incoming)
                            if (!existing.Contains(xpath))
                                existing.Add(xpath);
                    }
                    else
                    {
                        _xpathGroups.TryAdd(xpathType, incoming);
                    }
                }
            }
            catch { }
        }
    }
}
