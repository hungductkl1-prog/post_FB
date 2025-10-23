using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Sunny.Subdy.Common.Logs;

namespace Sunny.Subd.Core.Email
{
    public class TempMailService
    {
        public static async Task<string> GetEmail()
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.internal.temp-mail.io/api/v3/email/new");
                var content = new StringContent("{\r\n    \"min_name_length\": 10,\r\n    \"max_name_length\": 10\r\n}", null, "application/json");
                request.Content = content;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var contentData = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(contentData);
                string token = Convert.ToString(data["token"]);
                string mail = Convert.ToString(data["email"]);
                if (data != null && !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(mail))
                {
                    return mail;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                return string.Empty;
            }
        }
        public static async Task<string> GetCode(string email)
        {

            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.internal.temp-mail.io/api/v3/email/{email}/messages");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync();

                var dataV2 = JArray.Parse(content); // Sử dụng JArray thay vì JObject
                string firstId = dataV2.Last?["id"]?.ToString(); // Lấy id của phần tử đầu tiên
                if (dataV2 != null && !string.IsNullOrEmpty(firstId))
                {
                    request = new HttpRequestMessage(HttpMethod.Get, $"https://api.internal.temp-mail.io/api/v3/message/{firstId}");
                    response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    content = await response.Content.ReadAsStringAsync();
                    var data = JObject.Parse(content);
                    string textContent = data["body_text"]?.ToString();
                    string cleanedText = Regex.Replace(textContent, @"[^\d\s]", "");
                    string pattern = @"\b\d{4,8}(?!\S)";
                    MatchCollection matches = Regex.Matches(cleanedText, pattern);

                    foreach (Match match in matches)
                    {
                        if (match.Value != Regex.Replace(email, @"[^\d\s]", ""))
                        {
                            return match.Value;
                        }
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
