using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Sunny.Subd.Core.Utils;
using Sunny.Subdy.Common.Helper;

namespace Sunny.Subd.Core.Email
{
    public class MailTMService
    {
        private static async Task<string> GetDomain()
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.mail.tm/domains");
                request.Headers.Add("Accept", "application/json");

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return "";
                }
                string content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(content))
                {
                    return "";
                }

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("hydra:member", out JsonElement members) && members.GetArrayLength() > 0)
                {
                    var domains = members.EnumerateArray()
                                         .Select(x => x.GetProperty("domain").GetString())
                                         .Where(d => !string.IsNullOrEmpty(d))
                                         .ToList();

                    if (domains.Count > 0)
                    {
                        var random = new Random();
                        return domains[random.Next(domains.Count)];
                    }
                }

                return "";
            }
            catch
            {
                return "";
            }
        }

        public static async Task<string> GetEmail()
        {
            try
            {
                string domain = await GetDomain();
                if (string.IsNullOrEmpty(domain))
                {
                    return "ERROR:NO get domain.";
                }
                string email = SubdyHelper.RandomString(length: SubdyHelper.RandomValue(6, 20)) + domain;

                var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
                var body = $"{{\"address\":\"{email}\",\"password\":\"{email}\"}}";
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.mail.tm/accounts", content);
                string responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseText);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("id", out var idProp))
                        return $"Success|{email}";
                }
                else if ((int)response.StatusCode == 422 && responseText.Contains("This value is already used"))
                {
                    return $"Error: Email '{email}' already used.";
                }
                else
                {
                    return $"Error: {response.StatusCode} - {responseText}";
                }
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
            return string.Empty;
        }

        public static async Task<string> GetOTP(string email, int timeOut = 120)
        {
            try
            {
                int tickCount = Environment.TickCount;
                while (Environment.TickCount - tickCount <= timeOut * 1000)
                {
                    try
                    {
                        try
                        {
                            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

                            // Step 1: get token
                            var tokenBody = $"{{\"address\":\"{email}\",\"password\":\"{email}\"}}";
                            var tokenContent = new StringContent(tokenBody, Encoding.UTF8, "application/json");
                            var tokenResponse = await client.PostAsync("https://api.mail.tm/token", tokenContent);
                            if (!tokenResponse.IsSuccessStatusCode)
                            {
                                continue;
                            }

                            string tokenResponseText = await tokenResponse.Content.ReadAsStringAsync();
                            var tokenDoc = JsonDocument.Parse(tokenResponseText);
                            var tokenRoot = tokenDoc.RootElement;

                            if (tokenRoot.TryGetProperty("token", out var tokenProp))
                            {
                                string bearerToken = tokenProp.GetString();

                                // Step 2: list messages
                                var messagesRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.mail.tm/messages");
                                messagesRequest.Headers.Add("Authorization", $"Bearer {bearerToken}");
                                var messagesResponse = await client.SendAsync(messagesRequest);

                                if (!messagesResponse.IsSuccessStatusCode)
                                {
                                    continue;
                                }

                                string messagesText = await messagesResponse.Content.ReadAsStringAsync();
                                var messagesDoc = JsonDocument.Parse(messagesText);
                                var messagesRoot = messagesDoc.RootElement;

                                if (messagesRoot.TryGetProperty("hydra:member", out var messagesArray))
                                {
                                    foreach (var message in messagesArray.EnumerateArray())
                                    {
                                        if (message.TryGetProperty("id", out var idProp))
                                        {
                                            // Step 3: fetch individual message
                                            var msgRequest = new HttpRequestMessage(HttpMethod.Get, $"https://api.mail.tm/messages/{idProp.GetString()}");
                                            msgRequest.Headers.Add("Authorization", $"Bearer {bearerToken}");
                                            var msgResponse = await client.SendAsync(msgRequest);

                                            if (!msgResponse.IsSuccessStatusCode)
                                            {
                                                continue;
                                            }

                                            string msgText = await msgResponse.Content.ReadAsStringAsync();
                                            var msgDoc = JsonDocument.Parse(msgText);
                                            var msgRoot = msgDoc.RootElement;

                                            if (msgRoot.TryGetProperty("text", out var text))
                                            {
                                                string cleanedText = Regex.Replace(text.GetString(), @"[^\d\s]", "");
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
                                    }
                                }

                                return tokenProp.GetString(); // Trả về token
                            }
                        }
                        catch (Exception ex)
                        {
                            return $"Exception: {ex.Message}";
                        }
                    }
                    catch (Exception ex)
                    {
                        return null;
                    }
                    await Task.Delay(2000); // Delay 2 giây trước khi lặp lại
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
