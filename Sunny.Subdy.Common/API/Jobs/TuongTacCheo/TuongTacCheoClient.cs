using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Sunny.Subdy.Common.API.Captchas;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Logs;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Sunny.Subdy.Common.API.Jobs.TuongTacCheo
{
    public class TuongTacCheoClient
    {
        private readonly HttpClient _httpClient;

        public string SiteUrl = $"https://tuongtaccheo.com/cauhinh/facebook.php";

        public TuongTacCheoClient()
        {
            var handler = new HttpClientHandler { UseCookies = false };
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://tuongtaccheo.com/")
            };
        }

        public async Task<string> GetSiteKey(string cookie)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://tuongtaccheo.com/cauhinh/facebook.php");
            request.Headers.Add("Cookie", cookie);

            var response = await _httpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();
            string pattern = @"data-sitekey=""([^""]*)""";
            Match match = Regex.Match(responseBody, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                return null;
            }
        }

        public async Task<string> AddAccount(string cookie, string uid, string token_Recaptcha)
        {
            string result = string.Empty;
            var formData = new MultipartFormDataContent
            {
                { new StringContent(uid), "link" },
                { new StringContent("fb"), "loainick" },
                { new StringContent(token_Recaptcha), "recaptcha" }
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "https://tuongtaccheo.com/cauhinh/nhapnick.php");
            request.Headers.Add("Cookie", cookie);
            request.Content = formData;

            try
            {
                var response = await _httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseBody) && responseBody == "1")
                {
                    return "success";
                }
                else if (!string.IsNullOrEmpty(responseBody) && responseBody == "3")
                {
                    result = $"error: Tài khoản chưa đủ điều kiện để thêm vào tuongtaccheo.";
                }
                else
                {
                    result = $"error: {responseBody}";
                }
            }
            catch (Exception ex)
            {
                result = $"error: {ex.Message}";
            }
            return result;
        }

        public async Task<string> GetCookie(string token)
        {
            try
            {
                var handler = new HttpClientHandler { UseCookies = false };
                var client = new HttpClient(handler);
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("access_token", token)
                });
                var request = new HttpRequestMessage(HttpMethod.Post, "https://tuongtaccheo.com/logintoken.php");
                request.Content = formData;
                var response = await client.SendAsync(request);
                var repont = await response.Content.ReadAsStringAsync();
                if (!repont.Contains("sodu"))
                {
                    return "";
                }

                var data = JsonNode.Parse(repont)!.AsObject();
                try
                {
                    // Extract Set-Cookie header values
                    if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                    {
                        return cookies.First();
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Error(ex);
                }

                return "";
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
            }
            return null;
        }

        public async Task<string> GetCoin(string token)
        {
            try
            {
                var handler = new HttpClientHandler { UseCookies = false };
                var client = new HttpClient(handler);
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("access_token", token)
                });
                var request = new HttpRequestMessage(HttpMethod.Post, "https://tuongtaccheo.com/logintoken.php");
                request.Content = formData;
                var response = await client.SendAsync(request);
                var repont = await response.Content.ReadAsStringAsync();
                if (!repont.Contains("sodu"))
                {
                    throw new Exception(repont);
                }

                var data = JsonNode.Parse(repont)!.AsObject();
                try
                {
                    return data["data"]["sodu"].ToString();
                }
                catch (Exception ex)
                {
                    LogManager.Error(ex);
                }

                return "";
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
            }
            return null;
        }

        public async Task<bool> DatNick(string cookie, string uid)
        {
            try
            {
                var handler = new HttpClientHandler { UseCookies = false };
                var client = new HttpClient(handler);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                var request = new HttpRequestMessage(HttpMethod.Post, "https://tuongtaccheo.com/cauhinh/datnick.php");
                request.Headers.Add("authority", "tuongtaccheo.com");
                request.Headers.Add("accept", "*/*");
                request.Headers.Add("accept-language", "vi,en;q=0.9,en-US;q=0.8,ja;q=0.7");
                request.Headers.Add("cookie", cookie);
                request.Headers.Add("origin", "https://tuongtaccheo.com");
                request.Headers.Add("referer", "https://tuongtaccheo.com/cauhinh/facebook.php");
                request.Headers.Add("sec-ch-ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"");
                request.Headers.Add("sec-ch-ua-mobile", "?0");
                request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
                request.Headers.Add("sec-fetch-dest", "empty");
                request.Headers.Add("sec-fetch-mode", "cors");
                request.Headers.Add("sec-fetch-site", "same-origin");
                request.Headers.Add("x-requested-with", "XMLHttpRequest");

                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("iddat[]", uid),
                    new KeyValuePair<string, string>("loai", "fb")
                });
                request.Content = formData;

                var response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody == "1";
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
            }
            return false;
        }

        public static string GetJobPrefix(string job_type)
        {
            switch (job_type)
            {
                case JobTypes.Like: return SubdyHelper.GetStringRandom(new List<string> { "likepostvipre", "likepostvipcheo" });
                case JobTypes.Love:
                case JobTypes.Care:
                case JobTypes.Haha:
                case JobTypes.Wow:
                case JobTypes.Sad:
                case JobTypes.Angry:
                    {
                        return SubdyHelper.GetStringRandom(new List<string> { "camxucvipcheo", "camxucvipre" });
                    }
                case JobTypes.Share: return "sharecheo";
                case JobTypes.JoinGroup: return "thamgianhomcheo";
                case JobTypes.LikePage: return "likepagecheo";
                case JobTypes.LikeComment: return "camxuccheobinhluan";
                default: throw new Exception("TTC không hỗ trợ JobPrefix");
            }
        }

        public async Task<string> GetTokenByUsername(string username, string password)
        {
            // First request - get initial cookies
            var formParams = new[]
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("submit", "ĐĂNG NHẬP")
            };

            var firstRequest = new HttpRequestMessage(HttpMethod.Post, "https://tuongtaccheo.com/login.php");
            firstRequest.Content = new FormUrlEncodedContent(formParams);
            var firstResponse = await _httpClient.SendAsync(firstRequest);
            string responseContent = await firstResponse.Content.ReadAsStringAsync();
            Debug.WriteLine(responseContent);

            var cookieHeaders = firstResponse.Headers
                                             .Where(h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                                             .SelectMany(h => h.Value);
            string cookie = string.Join("; ", cookieHeaders);

            try
            {
                // Second request - login with cookie
                var secondRequest = new HttpRequestMessage(HttpMethod.Post, "https://tuongtaccheo.com/login.php");
                secondRequest.Headers.Add("Cookie", cookie);
                secondRequest.Content = new FormUrlEncodedContent(formParams);
                var secondResponse = await _httpClient.SendAsync(secondRequest);
                await secondResponse.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(cookie))
                {
                    throw new Exception($"Login TuongTacCheo ERROR: [Không thể get cookie {responseContent}]");
                }

                // Third request - get API token page
                var apiRequest = new HttpRequestMessage(HttpMethod.Get, "https://tuongtaccheo.com/api/");
                apiRequest.Headers.Add("Cookie", cookie);
                var apiResponse = await _httpClient.SendAsync(apiRequest);
                string apiContent = await apiResponse.Content.ReadAsStringAsync();

                string pattern = @"<input[^>]*\bname=""ttc_access_token""[^>]*\bvalue=""([^""]*)""";
                Match match = Regex.Match(apiContent, pattern);
                string token = string.Empty;
                if (match.Success)
                {
                    token = match.Groups[1].Value;
                }
                if (!string.IsNullOrEmpty(token))
                {
                    return token;
                }
                else
                {
                    throw new Exception($"TuongTacCheo Token: thất bại");
                }
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                throw new Exception($"Login TuongTacCheo ERROR: [Không thể get cookie {ex.Message}]");
            }
            return null;
        }

        public async Task<JsonNode?> GetFacebookJob(string cookie, string job_type = "", string prefix = "")
        {
            try
            {
                var handler = new HttpClientHandler { UseCookies = false };
                var client = new HttpClient(handler);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                var request = new HttpRequestMessage(HttpMethod.Get, $"https://tuongtaccheo.com/kiemtien/{prefix}/getpost.php");
                request.Headers.Add("authority", "tuongtaccheo.com");
                request.Headers.Add("accept", "application/json, text/javascript, */*; q=0.01");
                request.Headers.Add("accept-language", "vi,en;q=0.9,en-US;q=0.8,ja;q=0.7");
                request.Headers.Add("cookie", cookie);
                request.Headers.Add("referer", $"https://tuongtaccheo.com/kiemtien/{prefix}/");
                request.Headers.Add("sec-ch-ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"");
                request.Headers.Add("sec-ch-ua-mobile", "?0");
                request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
                request.Headers.Add("sec-fetch-dest", "empty");
                request.Headers.Add("sec-fetch-mode", "cors");
                request.Headers.Add("sec-fetch-site", "same-origin");
                request.Headers.Add("x-requested-with", "XMLHttpRequest");

                var response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();
                return JsonNode.Parse(responseBody!);
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
            }
            return null;
        }

        public async Task<JsonNode?> ReportFacebookJob(string cookie, JobModel job, string prefix)
        {
            try
            {
                var handler = new HttpClientHandler { UseCookies = false };
                var client = new HttpClient(handler);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                var request = new HttpRequestMessage(HttpMethod.Post, $"https://tuongtaccheo.com/kiemtien/{prefix}/nhantien.php");
                request.Headers.Add("authority", "tuongtaccheo.com");
                request.Headers.Add("accept", "*/*");
                request.Headers.Add("accept-language", "vi,en;q=0.9,en-US;q=0.8,ja;q=0.7");
                request.Headers.Add("cookie", cookie);
                request.Headers.Add("origin", "https://tuongtaccheo.com");
                request.Headers.Add("referer", $"https://tuongtaccheo.com/kiemtien/{prefix}/");
                request.Headers.Add("sec-ch-ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"");
                request.Headers.Add("sec-ch-ua-mobile", "?0");
                request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
                request.Headers.Add("sec-fetch-dest", "empty");
                request.Headers.Add("sec-fetch-mode", "cors");
                request.Headers.Add("sec-fetch-site", "same-origin");
                request.Headers.Add("x-requested-with", "XMLHttpRequest");

                var body = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("id", job.JobId)
                });
                request.Content = body;

                var response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();
                return JsonNode.Parse(responseBody!);
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
            }
            return null;
        }
    }
}
