using Microsoft.VisualBasic.Logging;
using Sunny.Subdy.Common.Logs;

namespace Sunny.Subd.Core.Utils
{
    public class RequestService
    {

        public static async Task<string> Get(string url)
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
            }
           return string.Empty;
        }
        
    }
}
