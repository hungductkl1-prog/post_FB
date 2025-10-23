using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RestSharp;
using Sunny.Subd.Core.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Logs;

namespace Sunny.Subd.Core.Email
{
    public class GetnadaService
    {
        public static async Task<string> GetEmail()
        {
            try
            {
                string domain = await GetDomain();
                if (string.IsNullOrEmpty(domain))
                {
                    return string.Empty;
                }
                string email = (SubdyHelper.RandomString(length: SubdyHelper.RandomValue(6, 20)) + "@" + domain).ToLower();
                var options = new RestClientOptions("https://inboxes.com")
                {
                    Timeout = TimeSpan.FromSeconds(60),
                };
                var client = new RestClient(options);
                var request = new RestRequest($"/api/v2/inbox/{email}", Method.Get);
                RestResponse response = await client.ExecuteAsync(request);
                if (response.Content.Contains("msgs"))
                {
                    return email;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        private static async Task<string> GetDomain()
        {
            try
            {
                for (var i = 0; i < 10; i++)
                {
                    var options = new RestClientOptions("https://inboxes.com")
                    {
                        Timeout = TimeSpan.FromSeconds(60),
                    };
                    var client = new RestClient(options);
                    var request = new RestRequest($"/api/v2/domain", Method.Get);
                    RestResponse response = await client.ExecuteAsync(request);
                    if (response.Content.Contains("domains"))
                    {
                        string pattern = @"""qdn"":\s*""([^""]+)""";

                        // Find all matches
                        MatchCollection matches = Regex.Matches(response.Content, pattern);

                        if (matches.Count > 0)
                        {
                            // Select a random match
                            Random random = new Random();
                            int randomIndex = random.Next(matches.Count);

                            string randomDomain = matches[randomIndex].Groups[1].Value;

                            return randomDomain;
                        }
                    }
                    return "";
                }

            }
            catch (Exception ex)
            {
                return "";
            }
            return "";
        }
        public static async Task<string> GetCode(string email)
        {
            try
            {
                string urlId = string.Empty;

                var repont = await RequestService.Get($"https://inboxes.com/api/v2/inbox/{email}");
                var data = JObject.Parse(repont);
                JArray messages = (JArray)data["msgs"];
                urlId = messages
.FirstOrDefault(msg => msg["f"]?.ToString() == "Facebook")?["uid"]?.ToString();
                if (string.IsNullOrEmpty(urlId))
                {
                    return string.Empty;
                }
                var content = await RequestService.Get($"https://inboxes.com/api/v2/message/{urlId}");
                data = JObject.Parse(content);
                string textContent = data["text"]?.ToString();
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
                return null;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                return null;
            }
        }
    }
}
