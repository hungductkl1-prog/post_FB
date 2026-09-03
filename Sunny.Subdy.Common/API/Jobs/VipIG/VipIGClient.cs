using System.Text.Json.Nodes;
using Sunny.Subdy.Common.Logs;
using System.Net;
using System.Text.RegularExpressions;

namespace Sunny.Subdy.Common.API.Jobs.VipIG
{
    public class VipIGClient
    {
        // Shared singleton — HttpClient được thiết kế để tái dùng; tạo mới mỗi lần làm rò socket (TIME_WAIT).
        private static readonly HttpClient _client = new HttpClient();
        private string _cookie = "";
        private readonly string _userAgent = "Mozilla/5.0";

        public VipIGClient()
        {
        }
        public VipIGClient(string cookie)
        {
            _cookie = cookie;
        }
        private void ApplyDefaultHeaders(HttpRequestMessage request, string? referer = null)
        {
            request.Headers.TryAddWithoutValidation("x-requested-with", "XMLHttpRequest");
            request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);

            if (!string.IsNullOrEmpty(_cookie))
                request.Headers.TryAddWithoutValidation("Cookie", _cookie);

            if (!string.IsNullOrEmpty(referer))
                request.Headers.TryAddWithoutValidation("Referer", referer);
        }

        public bool IsLoggedIn => !string.IsNullOrWhiteSpace(_cookie);

        public async Task<string> LoginByToken(string token)
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer()
            };

            using var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://vipig.net/logintoken.php");
            request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);

            var content = new MultipartFormDataContent();
            content.Add(new StringContent(token), "access_token");
            request.Content = content;

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            JsonObject jObject = JsonNode.Parse(responseBody)!.AsObject();
            string name = jObject["data"]?["user"]?.ToString();
            string coin = jObject["data"]?["sodu"]?.ToString();

            Uri uri = new Uri("https://vipig.net/");
            var cookies = handler.CookieContainer.GetCookies(uri);
            _cookie = string.Join("; ", cookies.Cast<Cookie>().Select(c => $"{c.Name}={c.Value}"));
            if (string.IsNullOrWhiteSpace(name))
            {
                coin = responseBody;
            }
            return $"{name}|{coin}|{_cookie}";
        }
        public async Task<string> LoginByUsername(string username, string password)
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer()
            };

            using var client = new HttpClient(handler);
            var loginUrl = "https://vipig.net/login.php";

            var loginContent = new MultipartFormDataContent
    {
        { new StringContent(username), "username" },
        { new StringContent(password), "password" },
        { new StringContent("ĐĂNG NHẬP"), "submit" }
    };

            var loginResponse = await client.PostAsync(loginUrl, loginContent);
            string loginHtml = await loginResponse.Content.ReadAsStringAsync();

            // Lấy cookie từ handler
            var uri = new Uri("https://vipig.net/");
            var cookies = handler.CookieContainer.GetCookies(uri);
            string cookieString = string.Join("; ", cookies.Cast<Cookie>().Select(c => $"{c.Name}={c.Value}"));

            if (string.IsNullOrWhiteSpace(cookieString))
            {
                throw new Exception($"Login vipig.net ERROR: Không thể lấy cookie. HTML: {loginHtml}");
            }

            // Gửi request đến API để lấy token
            var request = new HttpRequestMessage(HttpMethod.Get, "https://vipig.net/api/");
            request.Headers.Add("Cookie", cookieString);
            request.Headers.Add("User-Agent", "Mozilla/5.0");

            var apiResponse = await client.SendAsync(request);
            string apiHtml = await apiResponse.Content.ReadAsStringAsync();

            string pattern = @"<input[^>]*name\s*=\s*""vipig_access_token""[^>]*value\s*=\s*""([^""]*)""";
            var match = Regex.Match(apiHtml, pattern);

            if (match.Success)
            {
                string token = match.Groups[1].Value;
                _cookie = string.Join("; ", cookies.Cast<Cookie>().Select(c => $"{c.Name}={c.Value}"));

                return await LoginByToken(token) + "|" + token;
            }
            else
            {
                throw new Exception("Token not found in API HTML response.");
            }
        }
        public async Task<string> GetBalance()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://vipig.net/home.php");
            ApplyDefaultHeaders(request);
            var response = await _client.SendAsync(request);
            string html = await response.Content.ReadAsStringAsync();

            var match = Regex.Match(html, @"id=""soduchinh"">(.+?)<");
            return match.Success ? match.Groups[1].Value : "";
        }

        public async Task<JsonObject> ClaimLikeReward(string id)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://vipig.net/kiemtien/nhantien.php")
            {
                Content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("id", id) })
            };
            ApplyDefaultHeaders(request);
            var response = await _client.SendAsync(request);
            return JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        }

        public async Task<JsonObject> ClaimFollowReward(string id)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://vipig.net/kiemtien/subcheo/nhantien2.php")
            {
                Content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("id", id) })
            };
            ApplyDefaultHeaders(request);
            var response = await _client.SendAsync(request);
            return JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        }

        public async Task<List<JobModel>> GetJobInstagram(string type)
        {
            string url = type == "like"
                ? "https://vipig.net/kiemtien/getpost.php"
                : $"https://vipig.net/kiemtien/subcheo/getpost.php";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            ApplyDefaultHeaders(request, referer: type == "tym"
                ? "https://vipig.net/kiemtien/"
                : $"https://vipig.net/kiemtien/subcheo");

            var response = await _client.SendAsync(request);
            string content = await response.Content.ReadAsStringAsync();

            var jsonArray = JsonNode.Parse(content)!.AsArray();
            var jobs = new List<JobModel>();

            foreach (var item in jsonArray)
            {
                jobs.Add(new JobModel
                {
                    JobId = item["soID"]?.ToString(),
                    ObjectId = item["idpost"]?.ToString(),
                    FromId = item["mediaid"]?.ToString(),
                    Coin = type == "like" ? 300 : 700,
                    Type =type
                });
            }

            return jobs;
        }

        public async Task<bool> CauHinh(string username)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://vipig.net/cauhinh/nhapnick.php")
            {
                Content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("link", username) })
            };
            ApplyDefaultHeaders(request);
            var response = await _client.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();
            return body.Contains("1");
        }

        public async Task<int> DatNick(string idfb)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://vipig.net/cauhinh/datnick.php")
            {
                Content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("iddat[]", idfb) })
            };
            ApplyDefaultHeaders(request);
            var response = await _client.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();

            return int.TryParse(json, out var result) ? result : 0;
        }
        public async Task<bool> CauHinhNhanh(string username)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://vipig.net/cauhinh/addnhanh.php?link={username}&nickchay={username}");
            ApplyDefaultHeaders(request);
            var response = await _client.SendAsync(request);
            string text = await response.Content.ReadAsStringAsync();
            return text.Contains("11");
        }
        public async Task<string> GetSiteKey()
        {
            try
            {
                using var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, "https://vipig.net/index.php");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                string pattern = @"data-sitekey=\""([^\""]*)\""";
                Match match = Regex.Match(responseBody, pattern);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);

            }
            return "";

        }
        public async Task<string> Register(string username, string password, string token)
        {
            try
            {
                using var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://vipig.net/register.php");
                request.Headers.Add("x-requested-with", "XMLHttpRequest");
                request.Headers.Add("Cookie", "PHPSESSID=h7o8aca4fvjdqc22k3hvshvel5");
                var content = new MultipartFormDataContent();
                content.Add(new StringContent(username), "dkusername");
                content.Add(new StringContent(password), "dkpassword");
                content.Add(new StringContent(password), "rdkpassword");
                content.Add(new StringContent(token), "recaptcha");
                content.Add(new StringContent(""), "ref");
                request.Content = content;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                return ex.Message;
            }
        }
        public async Task<string> GetIdByUsername(string username)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://vipig.net/cauhinh/index.php");
                ApplyDefaultHeaders(request);
                var response = await _client.SendAsync(request);
                string html = await response.Content.ReadAsStringAsync();
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);

                // Tìm tất cả các thẻ <li>
                var listItems = doc.DocumentNode.SelectNodes("//ul[@id='dsnick']//li");

                foreach (var li in listItems)
                {
                    var aTag = li.Descendants("a").FirstOrDefault();
                    if (aTag != null && aTag.InnerText.Trim() == username)
                    {
                        var inputTag = li.Descendants("input").FirstOrDefault(i => i.Attributes["type"]?.Value == "checkbox");
                        return inputTag?.GetAttributeValue("value", null);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);

            }
            return string.Empty;
        }
    }
}
