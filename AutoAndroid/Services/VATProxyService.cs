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
        public const string Package_Proxy = "com.vat.vpn";
        public static string path_VATProxy = Path.Combine(AppContext.BaseDirectory, "App", "VATProxy.apk");
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

        public bool ConnectProxy(string proxys, bool installIfMissing = true)
        {
            try
            {
                _client.LogHelper.State = "Kết nối proxy";
                _client.LogHelper.SUCCESS("Đang kiểm tra ứng dụng proxy...");
                if (!_client.AppList().Contains(Package_Proxy))
                {
                    if (!installIfMissing)
                    {
                        _client.LogHelper.Log("Thiết bị không có APK VAT Proxy đã cài sẵn.");
                        return false;
                    }

                    if (!File.Exists(path_VATProxy))
                    {
                        string folderName = Path.GetDirectoryName(path_VATProxy);
                        if (!string.IsNullOrEmpty(folderName))
                            Directory.CreateDirectory(folderName);
                        InitHelper.GithubDown(
                            "https://raw.githubusercontent.com/LamLe2001/changer/main/VATVpnProxy.apk",
                            path_VATProxy);
                    }

                    if (!_client.InstallApp(path_VATProxy)) return false;
                }

                string[] proxy = (proxys ?? string.Empty).Split(':');
                if (proxy.Length < 2 || string.IsNullOrWhiteSpace(proxy[0]) || string.IsNullOrWhiteSpace(proxy[1]))
                {
                    _client.LogHelper.Log("Proxy không hợp lệ, cần dạng ip:port hoặc ip:port:user:password.");
                    return false;
                }

                string ip = proxy[0].Trim();
                string port = proxy[1].Trim();
                string username = proxy.Length > 2 ? proxy[2].Trim() : string.Empty;
                string password = proxy.Length > 3 ? string.Join(":", proxy.Skip(3)).Trim() : string.Empty;

                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        Close();
                        string cmd = $"am broadcast -a com.vat.vpn.CONNECT_PROXY -n com.vat.vpn/.ui.ProxyReceiver --es address {ip} --es port {port}";
                        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                            cmd += $" --es username {username} --es password {password}";

                        string connectResult = _client.Shell(cmd);
                        if (!string.IsNullOrEmpty(connectResult) && connectResult.Contains("successful", StringComparison.OrdinalIgnoreCase))
                            return true;

                        _client.LogHelper.Log("VAT Proxy không xác nhận kết nối qua broadcast.");
                    }
                    catch (Exception ex)
                    {
                        _client.LogHelper.Log($"Lỗi kết nối proxy lần {i + 1}: {ex.Message}");
                    }

                    Close();
                }

                return false;
            }
            catch (Exception ex)
            {
                _client.LogHelper.Log($"Lỗi kết nối proxy: {ex.Message}");
                return false;
            }
        }
        private bool IsNumber(string str)
        {
            return int.TryParse(str, out _); // Kiểm tra chuỗi có phải là số nguyên hay không
        }
    }
}
