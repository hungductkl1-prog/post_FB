using System.Text;
using System.Text.Json.Nodes;

namespace Sunny.Subdy.Common.API.Captchas
{
    public class GuruCaptchaClient
    {
        public const string Url = "https://cap.guru/";

        public static async Task<string> GetIdCaptchaV2(string key, string sitekey, string siteurl)
        {
            var client = new HttpClient();
            var jsonBody = $"{{\"key\":\"{key}\",\"method\":\"userrecaptcha\",\"googlekey\":\"{sitekey}\",\"pageurl\":\"{siteurl}\",\"json\":1}}";
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("http://api2.cap.guru/in.php", content);

            try
            {
                var result = await response.Content.ReadAsStringAsync();
                var data = JsonNode.Parse(result)!.AsObject();
                if (data != null && data["status"]?.GetValue<int>() == 1)
                {
                    return data["request"]!.ToString();
                }
                else
                {
                    return $"error: {data?.ToString() ?? "Unknown error"}";
                }
            }
            catch (Exception ex)
            {
                return $"error: {ex.Message}";
            }
        }

        public static async Task<string> GetTokenCaptchaV2(string key, string id)
        {
            var client = new HttpClient();
            var jsonBody = $"{{\"key\":\"{key}\",\"action\":\"get\",\"id\":\"{id}\",\"json\":1}}";
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("http://api2.cap.guru/res.php", content);

            try
            {
                var result = await response.Content.ReadAsStringAsync();
                var data = JsonNode.Parse(result)!.AsObject();
                if (data != null && data["status"]?.GetValue<int>() == 1 && !string.IsNullOrEmpty(data["request"]?.ToString()))
                {
                    return data["request"]!.ToString();
                }

                return $"error: {result}";
            }
            catch (Exception ex)
            {
                return $"error: {ex.Message}";
            }
        }

        /// <summary>
        /// Gửi ảnh captcha (base64) lên cap.guru theo method=base64 và trả về captcha id.
        /// </summary>
        public static async Task<string> GetIdImageCaptcha(string key, string base64Image)
        {
            var client = new HttpClient();
            var jsonBody = $"{{\"key\":\"{key}\",\"method\":\"base64\",\"body\":\"{base64Image}\",\"json\":1}}";
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("http://api2.cap.guru/in.php", content);

            try
            {
                var result = await response.Content.ReadAsStringAsync();
                var data = JsonNode.Parse(result)!.AsObject();
                if (data != null && data["status"]?.GetValue<int>() == 1)
                {
                    return data["request"]!.ToString();
                }
                return $"error: {data?.ToString() ?? "Unknown error"}";
            }
            catch (Exception ex)
            {
                return $"error: {ex.Message}";
            }
        }

        public static async Task<string> Getbalance(string key)
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"http://api2.cap.guru/res.php?action=getbalance&key={key}");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return $"error: {ex.Message}";
            }
        }
    }
}
