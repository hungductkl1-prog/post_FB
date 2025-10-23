using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Data.Models;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Sunny.Subd.Core.Instagram
{
    public class InstagramRequest
    {
        private readonly Account _account;
        private readonly HttpClient session;
        private readonly string baseUrl = "https://i.instagram.com/api/v1";
        private readonly string deviceId;
        private readonly string guid;
        private readonly string _proxy = string.Empty;
        private string _identifier;
        private readonly CookieContainer _cookies = new();
        [assembly: System.Diagnostics.CodeAnalysis.DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Org.BouncyCastle.Security.DigestUtilities))]
        public InstagramRequest(Account account, string proxy)
        {
            _proxy = proxy;
            _account = account;
            session = CreateClient(proxy);
            deviceId = "android-" + Guid.NewGuid().ToString("N").Substring(0, 16);
            guid = Guid.NewGuid().ToString();
        }
        private async Task<(string csrf, string deviceId, string ua)> FetchCsrfAndDeviceAsync()
        {
            string ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0";

            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.instagram.com/");
            request.Headers.Add("User-Agent", ua);
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
            request.Headers.Add("Connection", "keep-alive");

            var response = await session.SendAsync(request);
            string content = await response.Content.ReadAsStringAsync();

            string csrf = _cookies.GetCookies(new Uri("https://www.instagram.com"))["csrftoken"]?.Value;

            var match = Regex.Match(content, @"""device_id"":""([^""]+)""");
            string deviceId = match.Success ? match.Groups[1].Value : null;

            return (csrf, deviceId, ua);
        }
        public async Task<string> Login()
        {
            var (csrf, deviceId, ua) = await FetchCsrfAndDeviceAsync();
            if (string.IsNullOrEmpty(csrf) || string.IsNullOrEmpty(deviceId))
            {
                throw new Exception("Không lấy được CSRF token hoặc device_id");
            }

            var headers = new Dictionary<string, string>
        {
            { "User-Agent", ua },
            { "X-CSRFToken", csrf },
            { "X-Requested-With", "XMLHttpRequest" },
            { "Referer", "https://www.instagram.com/accounts/login/" },
            { "Content-Type", "application/x-www-form-urlencoded" },
        };

            string encPassword = $"#PWD_INSTAGRAM_BROWSER:0:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:{_account.Password}";

            var payload = new Dictionary<string, string>
        {
            { "username", _account.Uid },
            { "enc_password", encPassword },
            { "optIntoOneTap", "false" }
        };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://www.instagram.com/api/v1/web/accounts/login/ajax/")
            {
                Content = new FormUrlEncodedContent(payload)
            };
            foreach (var h in headers)
                request.Headers.TryAddWithoutValidation(h.Key, h.Value);

            var response = await session.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            var result = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (result != null && result.ContainsKey("two_factor_required"))
            {
                if (result["two_factor_required"] is JsonElement elem && elem.ValueKind == JsonValueKind.True)
                {
                    var twoFactorInfoJson = result["two_factor_info"].ToString();
                    var info = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(twoFactorInfoJson);
                    _identifier = info["two_factor_identifier"].ToString();
                    string cookies = await Handle2FAAsync();
                    string token = await GetCFT(cookies);
                    await Challenge(cookies, token);
                    await ValidateInstagramCookie(cookies);
                    return cookies + "| ";
                }
            }

            if (result != null)
            {
                string cookieStr = GetCookieString();
                string token = await GetCFT(cookieStr);
                await Challenge(cookieStr, token);
                await ValidateInstagramCookie(cookieStr);
                return cookieStr + "| ";
            }

            throw new Exception($"Đăng nhập thất bại. [{result?["message"]?.ToString() ?? "Login thất bại"}]");
        }
        private async Task<string> Handle2FAAsync()
        {
            string verificationCode = await Get2FACodeAsync();
            if (verificationCode == null)
                throw new Exception($"Đăng nhập thất bại. [{"Không lấy được mã 2FA"}]");
            if (string.IsNullOrEmpty(_identifier))
                throw new Exception($"Đăng nhập thất bại. [{"Thiếu identifier cho 2FA"}]");

            string csrf = _cookies.GetCookies(new Uri("https://www.instagram.com"))["csrftoken"]?.Value;

            var headers = new Dictionary<string, string>
        {
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0" },
            { "X-CSRFToken", csrf },
            { "X-Requested-With", "XMLHttpRequest" },
            { "Referer", "https://www.instagram.com/accounts/login/two_factor?next=%2F" },
            { "Content-Type", "application/x-www-form-urlencoded" },
        };
            var data = new Dictionary<string, string>
        {
             { "isPrivacyPortalReq", "false" },
             { "queryParams", "{\"next\":\"/\"}" },
             { "trust_signal", "true" },
             { "username", _account.Uid },
             { "verification_method", "3" },
            { "identifier", _identifier },
            { "verificationCode", verificationCode }
        };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://www.instagram.com/api/v1/web/accounts/login/ajax/two_factor/")
            {
                Content = new FormUrlEncodedContent(data)
            };
            foreach (var h in headers)
                request.Headers.TryAddWithoutValidation(h.Key, h.Value);

            var resp = await session.SendAsync(request);
            string json = await resp.Content.ReadAsStringAsync();
            var result = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (result != null && result.ContainsKey("authenticated"))
            {
                return GetCookieString();
            }
            throw new Exception($"Đăng nhập thất bại. [{result?["message"]?.ToString() ?? "2FA thất bại"}]");
        }
        private string GetCookieString()
        {
            var cookies = _cookies.GetCookies(new Uri("https://www.instagram.com"));
            var list = new List<string>();
            foreach (Cookie c in cookies)
            {
                list.Add($"{c.Name}={c.Value}");
            }
            return string.Join("; ", list);
        }

        private Dictionary<string, string> GetCookieDict()
        {
            var cookies = _cookies.GetCookies(new Uri("https://www.instagram.com"));
            var dict = new Dictionary<string, string>();
            foreach (Cookie c in cookies)
            {
                dict[c.Name] = c.Value;
            }
            return dict;
        }

        private async Task<string> Get2FACodeAsync()
        {
            string token = _account.TowFA;
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new Exception("Không có secret 2FA, không thể tiếp tục đăng nhập.");
            }

            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, $"https://2fa.live/tok/{token.Trim()}");
                req.Headers.Add("Accept", "application/json");
                req.Headers.Add("X-Requested-With", "XMLHttpRequest");

                var resp = await session.SendAsync(req);
                if (resp.IsSuccessStatusCode)
                {
                    string json = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("token", out var val))
                        return val.GetString();
                }
            }
            catch { }

            return null;
        }

        public static async Task<Dictionary<string, string>> GetInfo(string username)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://i.instagram.com/api/v1/users/web_profile_info/?username={username}");
                request.Headers.Add("User-Agent", "Instagram 361.0.0.0.84 Android (28/9; 480dpi; 1080x1920; samsung; SM-G930F; herolte; samsungexynos8890; en_US; 673256705)");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                if (!json.Contains("user"))
                {
                    throw new Exception(json);
                }
                var node = JsonNode.Parse(json);

                string biography = node["data"]?["user"]?["biography"]?.ToString() ?? "";
                string followingCount = node["data"]?["user"]?["edge_follow"]?["count"]?.ToString() ?? "0";
                string fullName = node["data"]?["user"]?["full_name"]?.ToString() ?? "";
                result["bio"] = biography;
                result["following"] = followingCount.ToString();
                result["fullname"] = fullName;
                return result;
            }
            catch (Exception ex)
            {
                result["error"] = ex.Message;
            }
            return result;
        }





































        private HttpClient CreateClient(string proxy = null)
        {
            var handler = new HttpClientHandler
            {
                CookieContainer = _cookies,
                UseCookies = true
            };

            if (!string.IsNullOrWhiteSpace(proxy))
            {
                var proxyUri = new Uri(proxy);
                var webProxy = new WebProxy(proxyUri.Host, proxyUri.Port);

                if (!string.IsNullOrWhiteSpace(proxyUri.UserInfo))
                {
                    var userInfo = proxyUri.UserInfo.Split(':');
                    if (userInfo.Length == 2)
                    {
                        webProxy.Credentials = new NetworkCredential(userInfo[0], userInfo[1]);
                    }
                }

                handler.UseProxy = true;
                handler.Proxy = webProxy;
            }

            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
        }
        private (int publicKeyId, string publicKey) GetPublicKey()
        {
            _account.Status = "Đang lấy public key...";
            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/qe/sync/");
            request.Headers.Add("User-Agent", "Instagram 370.10.43.96 Android");
            request.Headers.Add("X-IG-Device-ID", deviceId);
            request.Headers.Add("X-IG-App-ID", "567067343352427");
            request.Headers.Add("X-IG-Capabilities", "3brTvw==");
            request.Headers.Add("X-IG-Connection-Type", "WIFI");

            var response = session.SendAsync(request).Result;
            string responseContent = response.Content.ReadAsStringAsync().Result;

            _account.Status = $"Status: {(int)response.StatusCode} - {response.ReasonPhrase}";
            _account.Status = "Response Content: " + responseContent;

            _account.Status = "Headers từ response:";
            foreach (var header in response.Headers)
            {
                _account.Status = $"{header.Key}: {string.Join(", ", header.Value)}";
            }

            string publicKeyIdStr = response.Headers.Contains("ig-set-password-encryption-key-id") ?
                response.Headers.GetValues("ig-set-password-encryption-key-id").FirstOrDefault() ?? "1" : "1";
            string publicKey = response.Headers.Contains("ig-set-password-encryption-pub-key") ?
                response.Headers.GetValues("ig-set-password-encryption-pub-key").FirstOrDefault() ?? "" : "";

            int publicKeyId = int.Parse(publicKeyIdStr);
            _account.Status = $"Debug - Public Key ID: {publicKeyId} - Public Key: {publicKey}";

            return (publicKeyId, publicKey);
        }
        private string EncryptPassword()
        {
            var (publicKeyId, publicKey) = GetPublicKey();
            if (string.IsNullOrEmpty(publicKey))
                throw new Exception("Không lấy được khóa công khai.");

            byte[] sessionKey = new byte[32];
            byte[] iv = new byte[12];
            var rng = new SecureRandom();
            rng.NextBytes(sessionKey);
            rng.NextBytes(iv);

            string timestamp = ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();

            _account.Status = "Public Key trước khi parse (base64):\r\n" + publicKey;

            string pemKey;
            try
            {
                pemKey = Encoding.UTF8.GetString(Convert.FromBase64String(publicKey)).Trim();
                _account.Status = "Public Key sau khi giải mã base64:\r\n" + pemKey;
            }
            catch (Exception ex)
            {
                _account.Status = $"Lỗi giải mã base64: {ex.Message}";
                throw;
            }

            try
            {
                RsaKeyParameters publicKeyParams;
                using (var reader = new StringReader(pemKey))
                {
                    var pemReader = new PemReader(reader);
                    var keyObject = pemReader.ReadObject();
                    if (keyObject == null)
                    {
                        _account.Status = "PemReader trả về null, định dạng PEM không đúng.";
                        throw new Exception("Không thể parse public key từ PEM.");
                    }

                    if (keyObject is RsaKeyParameters rsaKey)
                    {
                        publicKeyParams = rsaKey;
                    }
                    else if (keyObject is AsymmetricKeyParameter asymKey && !asymKey.IsPrivate)
                    {
                        publicKeyParams = (RsaKeyParameters)asymKey;
                    }
                    else
                    {
                        _account.Status = "Key không phải RSA public key.";
                        throw new Exception("Public key không phải định dạng RSA hợp lệ.");
                    }

                    _account.Status = "Parse PEM thành công!";
                }

                var rsaEngine = new Pkcs1Encoding(new RsaEngine());
                rsaEngine.Init(true, publicKeyParams);
                byte[] rsaEncrypted = rsaEngine.ProcessBlock(sessionKey, 0, sessionKey.Length);

                var gcmBlockCipher = new GcmBlockCipher(new AesEngine());
                var parameters = new AeadParameters(new KeyParameter(sessionKey), 128, iv, Encoding.UTF8.GetBytes(timestamp));
                gcmBlockCipher.Init(true, parameters);

                byte[] passwordBytes = Encoding.UTF8.GetBytes(_account.Password);
                byte[] outputBytes = new byte[gcmBlockCipher.GetOutputSize(passwordBytes.Length)];
                int length = gcmBlockCipher.ProcessBytes(passwordBytes, 0, passwordBytes.Length, outputBytes, 0);
                gcmBlockCipher.DoFinal(outputBytes, length);

                byte[] aesEncrypted = new byte[outputBytes.Length - 16];
                byte[] tag = new byte[16];
                Array.Copy(outputBytes, 0, aesEncrypted, 0, aesEncrypted.Length);
                Array.Copy(outputBytes, aesEncrypted.Length, tag, 0, tag.Length);

                byte[] payload = new byte[1 + 1 + iv.Length + 2 + rsaEncrypted.Length + tag.Length + aesEncrypted.Length];
                int offset = 0;
                payload[offset++] = 1;
                payload[offset++] = (byte)publicKeyId;
                Array.Copy(iv, 0, payload, offset, iv.Length);
                offset += iv.Length;
                byte[] sizeBuffer = BitConverter.GetBytes((ushort)rsaEncrypted.Length);
                if (!BitConverter.IsLittleEndian) Array.Reverse(sizeBuffer);
                Array.Copy(sizeBuffer, 0, payload, offset, 2);
                offset += 2;
                Array.Copy(rsaEncrypted, 0, payload, offset, rsaEncrypted.Length);
                offset += rsaEncrypted.Length;
                Array.Copy(tag, 0, payload, offset, tag.Length);
                offset += tag.Length;
                Array.Copy(aesEncrypted, 0, payload, offset, aesEncrypted.Length);

                string base64Payload = Convert.ToBase64String(payload);
                return $"#PWD_INSTAGRAM:4:{timestamp}:{base64Payload}";
            }
            catch (Exception ex)
            {
                _account.Status = $"Lỗi khi parse hoặc mã hóa RSA: {ex.Message}";
                throw;
            }
        }

        //public async Task<string> Login()
        //{
        //    try
        //    {
        //        _account.Status = "Bắt đầu đăng nhập...";
        //        _account.Status = "Đang mã hóa mật khẩu...";
        //        string encPassword = EncryptPassword();

        //        var data = new List<KeyValuePair<string, string>>
        //        {
        //            new KeyValuePair<string, string>("jazoest", "22500"),
        //            new KeyValuePair<string, string>("country_codes", "[{\"country_code\":\"84\",\"source\":[\"default\"]}]"),
        //            new KeyValuePair<string, string>("phone_id", Guid.NewGuid().ToString()),
        //            new KeyValuePair<string, string>("enc_password", encPassword),
        //            new KeyValuePair<string, string>("username", _account.Uid),
        //            new KeyValuePair<string, string>("adid", Guid.NewGuid().ToString()),
        //            new KeyValuePair<string, string>("guid", guid),
        //            new KeyValuePair<string, string>("device_id", deviceId),
        //            new KeyValuePair<string, string>("google_tokens", "[]"),
        //            new KeyValuePair<string, string>("login_attempt_count", "0")
        //        };

        //        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/accounts/login/")
        //        {
        //            Content = new FormUrlEncodedContent(data)
        //        };
        //        request.Headers.Add("User-Agent", "Instagram 370.10.43.96 Android");
        //        request.Headers.Add("X-IG-Device-ID", deviceId);
        //        request.Headers.Add("X-IG-App-ID", "567067343352427");
        //        request.Headers.Add("X-IG-Capabilities", "3brTvw==");
        //        request.Headers.Add("X-IG-Connection-Type", "WIFI");
        //        var contentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
        //        contentType.CharSet = "UTF-8";
        //        request.Content.Headers.ContentType = contentType;

        //        _account.Status = "Đang gửi request đăng nhập...";
        //        var response = await session.SendAsync(request);
        //        string responseContent = await response.Content.ReadAsStringAsync();

        //        _account.Status = $"Response đăng nhập: {responseContent}";
        //        if (!response.IsSuccessStatusCode)
        //        {
        //            _account.Status = $"Lỗi đăng nhập: {(int)response.StatusCode} - {response.ReasonPhrase} - Chi tiết: {responseContent}";
        //            if (responseContent.Contains("two_factor_required"))
        //            {
        //                _account.Status = "Instagram yêu cầu xác thực 2FA!";
        //                return await Handle2FA(responseContent);
        //            }
        //        }
        //        if (responseContent.Contains("\"authenticated\": true"))
        //        {
        //            _account.Status = "Đăng nhập thành công!";
        //            string cookies = string.Join(", ", response.Headers.GetValues("Set-Cookie"));
        //            if (string.IsNullOrEmpty(cookies))
        //            {
        //                throw new Exception($"Đăng nhập thất bại. Không có cookie - {responseContent}");
        //            }
        //            string authToken = request.Headers.GetValues("ig-set-authorization").FirstOrDefault();
        //            return cookies + "|" + authToken;
        //        }
        //        else
        //        {
        //            _account.Status = "Đăng nhập thất bại! - Chi tiết: " + responseContent;
        //            throw new Exception("Đăng nhập thất bại! - Chi tiết: " + responseContent);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogManager.Error(ex);
        //        throw ex;
        //    }

        //}
        private string GenerateOTP()
        {
            if (string.IsNullOrEmpty(_account.TowFA))
                throw new Exception("Không có secret 2FA.");

            byte[] keyBytes = Base32Decode(_account.TowFA.Replace(" ", ""));
            long timeStep = 30;
            long unixTimestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            long counter = unixTimestamp / timeStep;

            byte[] counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);

            using (var hmac = new HMACSHA1(keyBytes))
            {
                byte[] hash = hmac.ComputeHash(counterBytes);
                int offset = hash[hash.Length - 1] & 0x0F;
                int binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
                int otp = binary % 1000000;
                return otp.ToString("D6");
            }
        }

        private byte[] Base32Decode(string base32)
        {
            const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            string cleanBase32 = base32.ToUpper().Replace("=", "").Replace(" ", "");
            int byteCount = cleanBase32.Length * 5 / 8;
            byte[] output = new byte[byteCount];
            int buffer = 0, bitsLeft = 0, outputIndex = 0;

            foreach (char c in cleanBase32)
            {
                int val = base32Chars.IndexOf(c);
                if (val < 0) throw new ArgumentException("Invalid base32 character");

                buffer = (buffer << 5) | val;
                bitsLeft += 5;
                if (bitsLeft >= 8)
                {
                    output[outputIndex++] = (byte)(buffer >> (bitsLeft - 8));
                    bitsLeft -= 8;
                }
            }
            return output;
        }

        private async Task<string> Handle2FA(string responseJson)
        {
            if (string.IsNullOrEmpty(_account.TowFA))
            {
                _account.Status = "Không có secret 2FA, không thể tiếp tục đăng nhập.";
                throw new Exception("Không có secret 2FA, không thể tiếp tục đăng nhập.");
            }

            string twoFactorIdentifier = responseJson.Split(new[] { "\"two_factor_identifier\":\"" }, StringSplitOptions.None)[1].Split('"')[0];
            string verificationCode = GenerateOTP();

            _account.Status = $"Mã 2FA tự động tạo: {verificationCode}";

            var data2fa = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("verification_code", verificationCode),
                    new KeyValuePair<string, string>("phone_id", Guid.NewGuid().ToString()),
                    new KeyValuePair<string, string>("two_factor_identifier", twoFactorIdentifier),
                    new KeyValuePair<string, string>("username", _account.Uid),
                    new KeyValuePair<string, string>("trust_this_device", "1"),
                    new KeyValuePair<string, string>("guid", guid),
                    new KeyValuePair<string, string>("device_id", deviceId),
                    new KeyValuePair<string, string>("waterfall_id", Guid.NewGuid().ToString()),
                    new KeyValuePair<string, string>("verification_method", "3") // Thử khớp Python
                };

            _account.Status = "Dữ liệu 2FA gửi đi:";
            var request2fa = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/accounts/two_factor_login/")
            {
                Content = new FormUrlEncodedContent(data2fa)
            };
            request2fa.Headers.Add("User-Agent", "Instagram 370.10.43.96 Android");
            request2fa.Headers.Add("X-IG-Device-ID", deviceId);
            request2fa.Headers.Add("X-IG-App-ID", "567067343352427");
            request2fa.Headers.Add("X-IG-Capabilities", "3brTvw==");
            request2fa.Headers.Add("X-IG-Connection-Type", "WIFI");
            var contentType2fa = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
            contentType2fa.CharSet = "UTF-8";
            request2fa.Content.Headers.ContentType = contentType2fa;

            _account.Status = "Đang gửi request 2FA...";
            var response2fa = await session.SendAsync(request2fa);
            string response2faContent = await response2fa.Content.ReadAsStringAsync();

            _account.Status = $"Response 2FA: {response2faContent}";

            if (!response2fa.IsSuccessStatusCode)
            {
                _account.Status = $"Lỗi 2FA: {(int)response2fa.StatusCode} - {response2fa.ReasonPhrase} - Chi tiết: {response2faContent}";
                throw new Exception($"Lỗi 2FA: {(int)response2fa.StatusCode} - {response2fa.ReasonPhrase} - Chi tiết: {response2faContent}");
            }

            if (response2faContent.Contains("\"logged_in_user\""))
            {
                _account.Status = "Đăng nhập 2FA thành công!";

                string authToken = response2fa.Headers.GetValues("ig-set-authorization").FirstOrDefault();
                if (string.IsNullOrEmpty(authToken))
                {
                    throw new Exception("Xác thực 2FA thất bại! - Không lấy được token.");
                }
                _account.Token = authToken;
                string cookies = await GetCookiesUsingToken(authToken);
                if (string.IsNullOrEmpty(cookies))
                {
                    throw new Exception("Xác thực 2FA thất bại! - Không lấy được cookie.");
                }
                _account.Cookie = cookies;
                return cookies + "|" + authToken;
            }
            else
            {
                _account.Status = "Xác thực 2FA thất bại! - Chi tiết: " + response2faContent;
                throw new Exception("Xác thực 2FA thất bại! - Chi tiết: " + response2faContent);
            }



        }
        public static string ConvertCookie(string token)
        {
            // Loại bỏ prefix nếu có
            if (token.StartsWith("Bearer IGT:2:"))
                token = token.Substring("Bearer IGT:2:".Length);

            // Giải mã Base64
            byte[] data = Convert.FromBase64String(token);
            string json = Encoding.UTF8.GetString(data);

            // Giải mã JSON dùng System.Text.Json (thân thiện AOT)
            var cookieDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            // Tạo chuỗi cookie
            StringBuilder cookieString = new StringBuilder();
            foreach (var kvp in cookieDict)
            {
                string value = WebUtility.UrlDecode(kvp.Value);
                cookieString.Append($"{kvp.Key}={value}; ");
            }

            return cookieString.ToString();
        }
        private async Task<string> GetCookiesUsingToken(string authToken)
        {
            // Thêm token vào header Authorization
            string cookie = ConvertCookie(authToken);
            if (string.IsNullOrEmpty(cookie))
            {
                throw new Exception("Đăng nhập thất bại. Không lấy được cookie temp");
            }
            try
            {
                string token = await GetCFT(cookie);
                await Challenge(cookie, token);
                Thread.Sleep(1000);
                var (isValid, username, normalizedCookie) = await ValidateInstagramCookie(cookie);
                return await GetCookie(cookie);

            }
            catch (Exception ex)
            {
                throw ex;
            }



            return "";
        }
        private async Task<string> GetCFT(string cookie)
        {
            try
            {
                var client = CreateClient(_proxy);
                var request = new HttpRequestMessage(HttpMethod.Get, "https://www.instagram.com");
                request.Headers.Add("Cookie", cookie);
                request.Headers.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                request.Headers.Add("accept-language", "en-US,en;q=0.9");
                request.Headers.Add("dpr", "1");
                request.Headers.Add("priority", "u=0, i");
                request.Headers.Add("sec-ch-prefers-color-scheme", "light");
                request.Headers.Add("sec-ch-ua", "\"Not)A;Brand\";v=\"8\", \"Chromium\";v=\"138\", \"Microsoft Edge\";v=\"138\"");
                request.Headers.Add("sec-ch-ua-full-version-list", "\"Not)A;Brand\";v=\"8.0.0.0\", \"Chromium\";v=\"138.0.7204.158\", \"Microsoft Edge\";v=\"138.0.3351.95\"");
                request.Headers.Add("sec-ch-ua-mobile", "?0");
                request.Headers.Add("sec-ch-ua-model", "\"\"");
                request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
                request.Headers.Add("sec-ch-ua-platform-version", "\"10.0.0\"");
                request.Headers.Add("sec-fetch-dest", "document");
                request.Headers.Add("sec-fetch-mode", "navigate");
                request.Headers.Add("sec-fetch-site", "none");
                request.Headers.Add("sec-fetch-user", "?1");
                request.Headers.Add("upgrade-insecure-requests", "1");
                request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0");
                request.Headers.Add("viewport-width", "817");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string context = await response.Content.ReadAsStringAsync();
                string pattern = @"""csrf_token""\s*:\s*""([^""]+)""";
                var match = Regex.Match(context, pattern);

                if (match.Success)
                {
                    string csrfToken = match.Groups[1].Value;
                    return csrfToken;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi get csrf_token: {ex.Message} ");
            }
            return "";
        }
        private async Task Challenge(string cookie, string token)
        {
            try
            {
                using (var client = CreateClient(_proxy))
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://www.instagram.com/api/v1/challenge/web/action/");

                    // Form data
                    var content = new FormUrlEncodedContent(new[]
                    {
        new KeyValuePair<string, string>("choice", "0"),
        new KeyValuePair<string, string>("next", "https://www.instagram.com/accounts/onetap/?next=%2F&__coig_challenged=1")
    });

                    request.Content = content;

                    // Headers
                    request.Headers.Add("Cookie", cookie);
                    request.Headers.Add("x-ig-app-id", "936619743392459");
                    request.Headers.Add("x-requested-with", "XMLHttpRequest");
                    request.Headers.Add("x-csrftoken", token);
                    var response = await client.SendAsync(request);
                    string text = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi challenge: {ex.Message} ");
            }

        }
        public async Task<string> GetCookie(string cookie)
        {
            using (var client = CreateClient(_proxy))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://www.instagram.com/data/shared_data/");
                request.Headers.Add("Cookie", cookie);
                request.Headers.Add("x-requested-with", "XMLHttpRequest");

                var response = await client.SendAsync(request);

                if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
                {
                    var cookies = setCookies
                        .Select(s => s.Split(';')[0].Trim()) // chỉ lấy phần key=value
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Distinct();

                    return string.Join("; ", cookies) + ";" + cookie;
                }

                return null; // không có cookie
            }
        }
        public async Task<(bool isValid, string username, string normalizedCookie)> ValidateInstagramCookie(string cookie)
        {
            if (string.IsNullOrWhiteSpace(cookie))
                return (false, null, null);
            string result = string.Empty;
            try
            {
                using (var client = CreateClient(_proxy))
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, "https://www.instagram.com/api/v1/accounts/edit/web_form_data/");
                    request.Headers.Add("Cookie", cookie);
                    request.Headers.Add("x-ig-app-id", "1217981644879628");
                    request.Headers.Add("x-requested-with", "XMLHttpRequest");
                    request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");

                    var response = await client.SendAsync(request);
                    result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var json = JObject.Parse(result);
                        var value = json["form_data"]?["username"]?.ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            string username = value.ToString();
                            string normalizedCookie = NormalizeCookie(cookie);
                            return (true, username, normalizedCookie);
                        }

                    }
                    throw new Exception($"Lỗi khi xác thực cookie: [{result}]");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xác thực cookie: [{result}] - " + ex.Message);
                Console.WriteLine("Lỗi khi xác thực cookie: " + ex.Message);
            }

            return (false, null, null);
        }
        public string GetCsrfToken(string cookie)
        {
            return Regex.Match(cookie, "csrftoken=([^;]+)").Groups[1].Value;
        }
        private string NormalizeCookie(string cookie)
        {
            // Cắt bỏ trùng, loại bỏ khoảng trắng dư thừa v.v. nếu cần
            var parts = cookie.Split(';')
                              .Select(part => part.Trim())
                              .Where(part => !string.IsNullOrWhiteSpace(part))
                              .Distinct();

            return string.Join("; ", parts);
        }
        public async Task<string> Follow(string userId, string cookie, string csrf)
        {
            using (var client = CreateClient(_proxy))
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"https://www.instagram.com/api/v1/friendships/create/{userId}/");
                request.Headers.Add("Cookie", cookie);
                request.Headers.Add("x-csrftoken", csrf);
                request.Headers.Add("x-ig-app-id", "936619743392459");
                request.Headers.Add("x-requested-with", "XMLHttpRequest");

                request.Content = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("user_id", userId)
            });

                var response = await client.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
        }
        public async Task<string> Like(string mediaId, string cookie, string csrf)
        {
            using (var client = CreateClient(_proxy))
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"https://www.instagram.com/api/v1/web/likes/{mediaId}/like/");
                request.Headers.Add("Cookie", cookie);
                request.Headers.Add("x-csrftoken", csrf);
                request.Headers.Add("x-ig-app-id", "936619743392459");
                request.Headers.Add("x-requested-with", "XMLHttpRequest");

                var response = await client.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
