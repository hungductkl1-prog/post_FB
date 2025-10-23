using System.Text.Json;
using System.Text.RegularExpressions;
using RestSharp;
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
                var client = new RestClient("https://api.mail.tm");
                var request = new RestRequest("/domains", Method.Get);
                request.AddHeader("Accept", "application/json"); // Thêm header đúng

                RestResponse response = await client.ExecuteAsync(request);
                if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
                {
                    return "";
                }

                using var doc = JsonDocument.Parse(response.Content);
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
                var options = new RestClientOptions("https://api.mail.tm")
                {
                    Timeout = TimeSpan.FromSeconds(60),
                };

                var client = new RestClient(options);
                var request = new RestRequest("/accounts", Method.Post);
                request.AddHeader("Content-Type", "application/json");

                var body = @$"{{
            ""address"": ""{email}"",
            ""password"": ""{email}""
        }}";
                request.AddStringBody(body, DataFormat.Json);

                RestResponse response = await client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    using var doc = JsonDocument.Parse(response.Content);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("id", out var idProp))
                        return $"Success|{email}";
                }
                else if ((int)response.StatusCode == 422 && response.Content.Contains("This value is already used"))
                {
                    return $"Error: Email '{email}' already used.";
                }
                else
                {
                    return $"Error: {response.StatusCode} - {response.Content}";
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
                string urlId = string.Empty;
                int tickCount = Environment.TickCount;
                while (Environment.TickCount - tickCount <= timeOut * 1000)
                {
                    try
                    {
                        try
                        {
                            var options = new RestClientOptions("https://api.mail.tm")
                            {
                                Timeout = TimeSpan.FromSeconds(60),
                            };
                            var client = new RestClient(options);
                            var request = new RestRequest("/token", Method.Post);
                            request.AddHeader("Content-Type", "application/json");

                            var body = @$"{{
            ""address"": ""{email}"",
            ""password"": ""{email}""
        }}";
                            request.AddStringBody(body, DataFormat.Json);

                            RestResponse response = await client.ExecuteAsync(request);
                            if (!response.IsSuccessful)
                            {
                                continue;
                            }

                            var doc = JsonDocument.Parse(response.Content);
                            var root = doc.RootElement;

                            if (root.TryGetProperty("token", out var tokenProp))
                            {
                                request = new RestRequest("/messages", Method.Get);
                                request.AddHeader("Authorization", $"Bearer {tokenProp.GetString()}");

                                response = await client.ExecuteAsync(request);

                                if (!response.IsSuccessful)
                                {
                                    continue;
                                }

                                doc = JsonDocument.Parse(response.Content);
                                root = doc.RootElement;

                                if (root.TryGetProperty("hydra:member", out var messagesArray))
                                {
                                    foreach (var message in messagesArray.EnumerateArray())
                                    {
                                        if (message.TryGetProperty("id", out var idProp))
                                        {
                                            request = new RestRequest($"/messages/{idProp.GetString()}", Method.Get);
                                            request.AddHeader("Authorization", $"Bearer {tokenProp.GetString()}");

                                            response = await client.ExecuteAsync(request);

                                            if (!response.IsSuccessful)
                                            {
                                                continue;
                                            }

                                            doc = JsonDocument.Parse(response.Content);
                                            root = doc.RootElement;
                                            if (root.TryGetProperty("text", out var text))
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
