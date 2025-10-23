using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xNet;

namespace Sunny.Subdy.Common.Services
{
    public class RequestXNet
    {
        internal xNet.HttpRequest Http;

        public RequestXNet(string cookies, string userAgent, string proxy, int proxyType)
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                            "AppleWebKit/537.36 (KHTML, like Gecko) " +
                            "Chrome/74.0.3729.131 Safari/537.36";
            }

            Http = new xNet.HttpRequest
            {
                KeepAlive = true,
                AllowAutoRedirect = true,
                Cookies = new CookieDictionary(),
                UserAgent = userAgent
            };

            Http.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            Http.AddHeader("Accept-Language", "en-US,en;q=0.9");

            if (!string.IsNullOrEmpty(cookies))
            {
                SetCookies(cookies);
            }

            if (!string.IsNullOrEmpty(proxy))
            {
                string[] parts = proxy.Split(':');

                switch (parts.Length)
                {
                    case 1:
                        Http.Proxy = proxyType == 0
                            ? HttpProxyClient.Parse("127.0.0.1:" + proxy)
                            : Socks5ProxyClient.Parse("127.0.0.1:" + proxy);
                        break;

                    case 2:
                        Http.Proxy = proxyType == 0
                            ? HttpProxyClient.Parse(proxy)
                            : Socks5ProxyClient.Parse(proxy);
                        break;

                    case 4:
                        string host = parts[0];
                        int port = Convert.ToInt32(parts[1]);
                        string user = parts[2];
                        string pass = parts[3];

                        Http.Proxy = proxyType == 0
                            ? new HttpProxyClient(host, port, user, pass)
                            : new Socks5ProxyClient(host, port, user, pass);
                        break;
                }
            }
        }

        public string RequestGet(string url)
        {
            try
            {
                Http.AddHeader("Cache-Control", "no-cache");
                return Http.Get(url).ToString();
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        public string RequestPost(string url, string data = "", string contentType = "application/x-www-form-urlencoded")
        {
            try
            {
                Http.AddHeader("Cache-Control", "no-cache");
                return string.IsNullOrEmpty(data) || string.IsNullOrEmpty(contentType)
                    ? Http.Post(url).ToString()
                    : Http.Post(url, data, contentType).ToString();
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        public byte[] RequestBytes(string url)
        {
            return Http.Get(url).ToBytes();
        }

        public void SetCookies(string cookies)
        {
            foreach (string part in cookies.Split(';'))
            {
                string[] kv = part.Split('=');
                if (kv.Length > 1)
                {
                    try
                    {
                        string key = kv[0];
                        string value = part.Substring(part.IndexOf("=") + 1);
                        Http.Cookies.Add(key, value);
                    }
                    catch
                    {
                        // ignore bad cookie
                    }
                }
            }
        }

        public string GetCookies()
        {
            return Http.Cookies.ToString();
        }

        private string HandleException(Exception ex)
        {
            string message = ex.ToString();

            if (message.Contains("Thread was being aborted."))
                return "";

            if (message.Contains("Не удалось соединиться с HTTP-сервером"))
                return "cannot_connect";

            return Http.Response?.ToString() ?? "";
        }
    }

}
