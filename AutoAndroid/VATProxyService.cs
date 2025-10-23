using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAndroid
{
    public class VATProxyService
    {
        public const string Package_Proxy = "com.vat.proxyconnector";
        public static string path_VATProxy = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App", "VATProxyConnector.apk");
        public static string path_College = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App", "collegeproxy.apk");
        ADBClient _client;
        public VATProxyService(ADBClient client)
        {
            _client = client;
        }
        private void Close()
        {
            _client.StopApp(Package_Proxy);
        }
        private bool Open()
        {
            for (int i = 0; i < 5; i++)
            {
                _client.AppStart(Package_Proxy, true, true, wait: true);
                _client.SetSize();
                if (_client.AppWait(Package_Proxy))
                {
                    return true;
                }
            }
            return false;
        }
        private async Task<bool> College_Proxy(string proxy)
        {
            if (string.IsNullOrEmpty(proxy)) return false;
            string[] proxyParts = proxy.Split(':');
            if (proxyParts.Length < 2) return false;
            if (!_client.AppList().Contains("com.cell47.College_Proxy"))
            {
                _client.InstallApp(path_College);
                 _client.Delay(6);
            }
            List<string> proxyList = new List<string>
            {
                "//*[@text=\"OK\"]",
                "//*[@text=\"START PROXY SERVICE\"]",
                "//*[@text=\"STOP PROXY SERVICE\"]",
            };

            for (int i = 0; i < 5; i++)
            {
                _client.AppClear("com.cell47.College_Proxy");
                _client.AppStart("com.cell47.College_Proxy", true, true, wait: true);
                Stopwatch stopwatch = Stopwatch.StartNew();
                while (stopwatch.ElapsedMilliseconds < 60000)
                {

                    var element = _client.FindElement("", proxyList, 10);
                    if (string.IsNullOrEmpty(element)) break;
                    switch (element)
                    {
                        case "//*[@text=\"STOP PROXY SERVICE\"]":
                            {
                                return true;
                            }
                        case "//*[@text=\"OK\"]":
                            {
                                _client.ElementWithAttributes(element, 1, "", true);
                                break;
                            }
                        case "//*[@text=\"START PROXY SERVICE\"]":
                            {
                                _client.SendTextADB("//*[@resource-id=\"com.cell47.College_Proxy:id/editText_address\"]", proxyParts[0], 1, "", true);
                                _client.SendTextADB("//*[@resource-id=\"com.cell47.College_Proxy:id/editText_port\"]", proxyParts[1], 1, "", true);
                                if (proxyParts.Count() > 2)
                                {
                                    _client.SendTextADB("//*[@resource-id=\"com.cell47.College_Proxy:id/editText_username\"]", proxyParts[2], 1, "", true);
                                    _client.SendTextADB("//*[@resource-id=\"com.cell47.College_Proxy:id/editText_password\"]", proxyParts[3], 1, "", true);
                                }
                                _client.ElementWithAttributes(element, 1, "", true);
                                _client.ElementWithAttributes("//*[@text=\"OK\"]", 5, "", true);
                                break;
                            }
                    }

                }
            }


            return false;
        }
        //public bool ConnectProxy(string proxys)
        //{
        //    if (string.IsNullOrEmpty(proxys))
        //        return false;

        //    var parts = proxys.Split(':');
        //    if (parts.Length == 2)
        //    {
        //        string ip = parts[0];
        //        string port = parts[1];

        //        if (IsValidIPv4(ip) && int.TryParse(port, out int portNum))
        //        {
        //            string command = $"settings put global http_proxy {ip}:{port}";
        //            _client.Shell(command);
        //            return true;
        //        }
        //    }

        //    return College_Proxy(proxys);
        //}
        private bool IsValidIPv4(string ip)
        {
            return System.Net.IPAddress.TryParse(ip, out var addr) && addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        }
        public bool ConnectProxy(string proxys)
        {
            try
            {
                _client.LogHelper.Sate = "ConnectProxy";
                if (!_client.AppList().Contains(Package_Proxy))
                {
                    if (!File.Exists(path_VATProxy))
                    {
                        string folderName = Path.GetFileName(Path.GetDirectoryName(path_VATProxy));
                        Directory.CreateDirectory(folderName);
                        InitHelper.GithubDown("https://raw.githubusercontent.com/vietdungdev0106/VAT-ProxyConnector/main/VAT-ProxyConnector_v1.0.0.apk", path_VATProxy);
                    }
                    _client.InstallApp(path_VATProxy);
                }
                string[] proxy = proxys.Split(':');
                string ip = string.Empty, port = string.Empty, username = string.Empty, password = string.Empty;
                if (proxy.Length > 0)
                {
                    ip = proxy[0];
                    port = proxy[1];
                    if (proxy.Length > 3)
                    {
                        username = proxy[2];
                        password = proxy[3];
                    }
                    if (!string.IsNullOrEmpty(ip) && !string.IsNullOrEmpty(port))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            try
                            {
                                Close();
                                string cmd = $"am broadcast -a com.vat.proxyconnector.CONNECT_PROXY -n com.vat.proxyconnector/.ProxyReceiver --es protocol 'http' --es address {ip} --ei port {port}";
                                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                                {
                                    cmd = $"am broadcast -a com.vat.proxyconnector.CONNECT_PROXY -n com.vat.proxyconnector/.ProxyReceiver --es protocol 'http' --es address {ip} --ei port {port}  --es username {username} --es password {password}";
                                }
                                var connectProxie = _client.Shell(cmd);

                                if (string.IsNullOrEmpty(connectProxie) || !connectProxie.Contains("successful"))
                                {
                                    if (!string.IsNullOrEmpty(ip) && !string.IsNullOrEmpty(port))
                                    {
                                        if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
                                        {
                                            username = ""; password = "";
                                        }
                                        int j = 6;
                                        while (j > 0)
                                        {
                                            if (Open() && _client.ElementWithAttributes("//*[@resource-id=\"com.vat.proxyconnector:id/edtAddress\"]", 30))
                                            {
                                                string dump = _client.GetXMLSource();
                                                _client.SendText("//*[@resource-id=\"com.vat.proxyconnector:id/edtAddress\"]", ip, xml: dump);
                                                _client.SendText("//*[@resource-id=\"com.vat.proxyconnector:id/edtPortContainer\"]", port, xml: dump);
                                                _client.SendText("//*[@resource-id=\"com.vat.proxyconnector:id/edtUsernameContainer\"]", username, xml: dump);
                                                _client.SendText("//*[@resource-id=\"com.vat.proxyconnector:id/edtPasswordContainer\"]", password, xml: dump);
                                                _client.ElementWithAttributes("//*[@text=\"CONNECT\"]", 5);
                                                if (_client.ElementWithAttributes("//*[@text=\"STOP\"]", 5, click: false))
                                                {
                                                    Thread.Sleep(8000);
                                                    return true;
                                                }
                                                else
                                                {
                                                    _client.ElementWithAttributes("//*[@text=\"OK\"]", 5);
                                                    if (_client.ElementWithAttributes("//*[@text=\"STOP\"]", 5, click: false))
                                                    {
                                                        Thread.Sleep(8000);
                                                        return true;
                                                    }
                                                }

                                            }
                                            j--;
                                            Close();
                                        }
                                    }
                                }
                                else
                                {
                                    return true;
                                }

                                return false;
                            }
                            catch (Exception e)
                            {

                            }

                        }

                        if (IsNumber(ip.Replace(".", "")))
                        {

                        }

                    }

                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private bool IsNumber(string str)
        {
            return int.TryParse(str, out _); // Kiểm tra chuỗi có phải là số nguyên hay không
        }
    }
}
