using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RestSharp;

namespace Sunny.Subd.Core.Utils
{
    internal class RestShapService
    {
        public static RestClient RestClientUrl(string baseUrl, WebProxy? proxy, string userAgent, bool useProxy = true)
        {
            var options = new RestClientOptions(baseUrl)
            {
                Timeout = TimeSpan.FromSeconds(60),
                UserAgent = userAgent,
            };
            if (proxy != null && useProxy)
            {
                options.Proxy = proxy;
            }
            return new RestClient(options);
        }
    }
}
