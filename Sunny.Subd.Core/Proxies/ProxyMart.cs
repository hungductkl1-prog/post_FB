using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Sunny.Subd.Core.Proxies
{
    internal class ProxyMart
    {
        public static async Task<string> GetProxy(string token)
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://proxymart.pro/key/get-current-ip?key={token}");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();

                try
                {
                    JObject jObject = JObject.Parse(json);
                    string host = jObject["data"]?["host"]?.ToString();
                    string port = jObject["data"]?["port"]?.ToString();

                    if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(port))
                    {
                        return $"{host}:{port}";
                    }
                }
                catch
                {
                    // Xử lý lỗi parse JSON nếu cần
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi HTTP request
            }
            return "";
        }
        public static async Task<string> NewProxy(string token)
        {
            string phoneNumber = string.Empty;
            string session = string.Empty;
            string error = string.Empty;
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://proxymart.pro/key/get-new-ip?key={token}");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                error = json.Trim();
                try
                {
                    JObject jObject = JObject.Parse(json);
                    string host = jObject["data"]?["host"]?.ToString();
                    string port = jObject["data"]?["port"]?.ToString();

                    if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(port))
                    {
                        return $"{host}:{port}";
                    }
                }
                catch
                {
                    // Xử lý lỗi parse JSON nếu cần
                }

            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            return "";
        }
    }
}
