using Newtonsoft.Json.Linq;

namespace Sunny.Subd.Core.Proxies
{
    public class ProxyKiot
    {
        public static async Task<string> GetProxy(string token)
        {
            string phoneNumber = string.Empty;
            string session = string.Empty;
            string error = string.Empty;
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.kiotproxy.com/api/v1/proxies/current?key={token}");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                error = json.Trim();
                try
                {
                    JObject jObject = JObject.Parse(json);
                    return phoneNumber = jObject["data"]?["http"]?.ToString();
                }
                catch
                {

                }

            }
            catch (Exception ex)
            {
                error = ex.Message;
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
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.kiotproxy.com/api/v1/proxies/new?key={token}");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                error = json.Trim();
                try
                {
                    JObject jObject = JObject.Parse(json);
                    return phoneNumber = jObject["data"]?["http"]?.ToString();
                }
                catch
                {

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
