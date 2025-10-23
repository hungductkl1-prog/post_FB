namespace Sunny.Subd.Core.Proxies
{
    public class ProxyService
    {
        public const string NoIP = "Không đổi IP";
        public const string Mobile4G = "4G Mobile";
        public const string ProxyAssigned = "Proxy đã gán cho tài khoản";
        public const string KiotProxy = "https://kiotproxy.com/";
        public const string ProxyMart = "https://proxymart.net/";
        public const string WWProxy = "https://wwproxy.com";
        public const string CustomProxy = "Proxy xoay custom (Proxy|Link Reset IP)";
        public const string ProxyFile = "Proxy file";
        public static List<string> Proxies = new List<string>();
        public static List<string> ProxyTypes = new List<string>
        {
           NoIP,
           Mobile4G,
           ProxyAssigned,
           KiotProxy,
           ProxyMart,
           WWProxy,
           CustomProxy,
           ProxyFile,
        };

        public static string GetProxy()
        {
            lock (Proxies)
            {
                if (Proxies.Count == 0) return string.Empty;
                var proxy = Proxies[0];
                Proxies.RemoveAt(0);
                Proxies.Add(proxy);
                return proxy;
            }
        }
    }
}
