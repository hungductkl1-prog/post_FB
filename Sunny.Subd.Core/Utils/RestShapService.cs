using System.Net;

namespace Sunny.Subd.Core.Utils
{
    public static class RestShapService
    {
        public static HttpClient CreateClient(string baseUrl, WebProxy? proxy = null, string? userAgent = null, bool useProxy = true)
        {
            var handler = new HttpClientHandler();
            if (proxy != null && useProxy)
                handler.Proxy = proxy;

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(60)
            };

            if (!string.IsNullOrEmpty(userAgent))
                client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

            return client;
        }
    }
}
