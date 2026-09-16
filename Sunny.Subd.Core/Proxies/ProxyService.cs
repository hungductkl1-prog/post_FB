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

        // HttpClient dùng chung cho mọi lời gọi API nhà cung cấp proxy, timeout ngắn (15s):
        // trước đây mỗi lời gọi dùng new HttpClient() với timeout mặc định ~100s, nên một
        // nhà cung cấp chết/không reachable đốt ~200s im lặng mỗi account trước khi bỏ qua.
        public static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

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

        // Chỉ hiện host:port khi cần log/status; tuyệt đối không lộ
        // username/password của proxy ra log.
        public static string Mask(string? proxy)
        {
            if (string.IsNullOrWhiteSpace(proxy)) return "(trống)";
            var parts = proxy.Split(':');
            return parts.Length >= 2 ? $"{parts[0]}:{parts[1]}" : proxy;
        }
    }
}
