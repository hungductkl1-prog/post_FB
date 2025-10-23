using Newtonsoft.Json.Linq;
using RestSharp;
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
                var client = RestShapService.RestClientUrl("https://graph.facebook.com", null, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36 Edg/136.0.0.0");

                var request = new RestRequest($"/{uid}/picture?redirect=false", Method.Get);
                RestResponse response = await client.ExecuteAsync(request);

                if (string.IsNullOrEmpty(response.Content))
                {
                    throw new Exception($"Response content is empty or null. [{response.ErrorMessage}]");
                }
                JObject content = JObject.Parse(response.Content!);
                bool isLive = false;
                if (content.ContainsKey("data"))
                {
                    isLive = content["data"]!["height"] != null;
                }
                return isLive;
            }
            catch(Exception ex)
            {
                LogManager.Error(ex);
            }
            return false;
        }
    }
}
