using Newtonsoft.Json.Linq;
using RestSharp;

namespace Sunny.Subdy.Common.API.Captchas
{
    public class GuruCaptchaClient
    {
        public const string Url = "https://cap.guru/";
        public static async Task<string> GetIdCaptchaV2(string key, string sitekey, string siteurl)
        {
            var client = new RestClient("http://api2.cap.guru");
            var request = new RestRequest("in.php", Method.Post);

            //request.AddParameter("key", key);
            //request.AddParameter("method", "userrecaptcha");
            //request.AddParameter("googlekey", sitekey);
            //request.AddParameter("pageurl", siteurl);
            //request.AddParameter("json", 1);
            var body = new Dictionary<string, object>
    {
        { "key", key },
        { "method", "userrecaptcha" },
        { "googlekey", sitekey},
        { "pageurl", siteurl},
        { "json", 1 }
    };

            request.AddJsonBody(body);

            var response = await client.ExecuteAsync(request);

            try
            {
                var result = response.Content;
                var data = JObject.Parse(result);
                if (data != null && (int)data["status"] == 1)
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
            var client = new RestClient("http://api2.cap.guru");
            var request = new RestRequest("res.php", Method.Post);
            request.AddHeader("Content-Type", "application/json");

            var body = new Dictionary<string, object>
    {
        { "key", key },
        { "action", "get" },
        { "id", id },
        { "json", 1 }
    };

            request.AddJsonBody(body); 

            var response = await client.ExecuteAsync(request);
            try
            {
                var result = response.Content;
                var data = JObject.Parse(result);
                if (data != null && (int)data["status"] == 1 && !string.IsNullOrEmpty(data["request"]?.ToString()))
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
