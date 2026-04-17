using Sunny.Subdy.Common.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sunny.Subd.Core.Email
{
    public class MailTheLoiService
    {
        private static async Task<string> GetDomain(string token)
        {
            try
            {
                var client = new HttpClient();
                var json = await client.GetStringAsync($"https://mail.theloi.io.vn/api/get_domains.php?api={token}");

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var domains = new List<string>();
                foreach (var item in root.GetProperty("data").EnumerateArray())
                {
                    domains.Add(item.GetProperty("domain").GetString());
                }
                if (domains.Count > 0)
                {
                    var random = new Random();
                    return domains[random.Next(domains.Count)];
                }
                return "";
            }
            catch
            {
                return "";
            }
        }
        public static async Task<string> GetEmail(string token)
        {
            try
            {
                string domain = await GetDomain(token);
                if (string.IsNullOrEmpty(domain))
                {
                    return "";
                }
                string email = SubdyHelper.RandomString(length: SubdyHelper.RandomValue(6, 20)) +"@"+ domain;
                return email;
            }
            catch (Exception ex)
            {
                return $"";
            }
        }
        public static async Task<string> GetOTP(string email, string token,int timeOut = 120)
        {
            try
            {
                string urlId = string.Empty;
                int tickCount = Environment.TickCount;
                while (Environment.TickCount - tickCount <= timeOut * 1000)
                {
                    try
                    {
                        var client = new HttpClient();
                        var response = await client.GetStringAsync("https://mail.theloi.io.vn/api/get_mail.php?apikey=" + token + "&email=" + email);
                        var jsonResponse = JsonNode.Parse(response)!.AsObject();

                        if (jsonResponse["status"]?.GetValue<bool>() == true)
                        {
                            var latestEmail = jsonResponse["Data"]?[0];
                            var code = latestEmail?["Code"];
                            return code?.GetValue<string>();
                        }
                    }
                    catch
                    {
                        
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
