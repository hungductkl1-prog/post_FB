using System.Text.Json.Nodes;
using Sunny.Subd.Core.Utils;
using Sunny.Subdy.Common.Logs;

namespace Sunny.Subd.Core.Facebook
{
    public class FacebookRequest
    {
        public static async Task<bool> CheckLive(string uid)
        {
            try
            {
                if (string.IsNullOrEmpty(uid))
                {
                    return false;
                }
                var client = RestShapService.CreateClient(
                    "https://graph.facebook.com",
                    userAgent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36 Edg/136.0.0.0");

                var request = new HttpRequestMessage(HttpMethod.Get, $"/{uid}/picture?redirect=false");
                var response = await client.SendAsync(request);
                string content = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(content))
                {
                    throw new Exception($"Response content is empty or null.");
                }
                JsonObject jsonContent = JsonNode.Parse(content!)!.AsObject();
                bool isLive = false;
                if (jsonContent.ContainsKey("data"))
                {
                    isLive = jsonContent["data"]!["height"] != null;
                }
                return isLive;
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
            }
            return false;
        }
    }
}
