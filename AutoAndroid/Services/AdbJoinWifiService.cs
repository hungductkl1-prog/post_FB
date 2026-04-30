using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AutoAndroid
{
    public class AdbJoinWifiService
    {
        public static string PathApk = Path.Combine(AppContext.BaseDirectory, "App", "adbjoinwifi.apk");
        public static string Package = "com.steinwurf.adbjoinwifi";

        // Official release of steinwurf/adb-join-wifi v1.0.1 (~1.5 MB).
        private const string DownloadUrl =
            "https://github.com/steinwurf/adb-join-wifi/releases/download/1.0.1/adb-join-wifi.apk";

        private static readonly SemaphoreSlim _downloadLock = new(1, 1);

        private ADBClient _service;
        public AdbJoinWifiService(ADBClient service)
        {
            _service = service;
        }

        // Ensure the APK exists locally; download from GitHub if missing.
        public static async Task<bool> EnsureApkAvailableAsync()
        {
            if (File.Exists(PathApk) && new FileInfo(PathApk).Length > 100_000)
                return true;

            await _downloadLock.WaitAsync();
            try
            {
                if (File.Exists(PathApk) && new FileInfo(PathApk).Length > 100_000)
                    return true;

                var dir = Path.GetDirectoryName(PathApk);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var tmp = PathApk + ".downloading";
                using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) })
                {
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("LamToolAutoPhonePrime");
                    using var resp = await http.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    resp.EnsureSuccessStatusCode();
                    await using var fs = File.Create(tmp);
                    await resp.Content.CopyToAsync(fs);
                }

                if (new FileInfo(tmp).Length < 100_000)
                {
                    File.Delete(tmp);
                    return false;
                }

                if (File.Exists(PathApk)) File.Delete(PathApk);
                File.Move(tmp, PathApk);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[AdbJoinWifi] Tải APK thất bại: {ex.Message}");
                return false;
            }
            finally { _downloadLock.Release(); }
        }

        public bool Install()
        {
            if (!File.Exists(PathApk))
            {
                // Best-effort sync wait for download if someone forgot to pre-fetch.
                EnsureApkAvailableAsync().GetAwaiter().GetResult();
            }
            if (!File.Exists(PathApk))
            {
                LogHelper.Error($"[AdbJoinWifi] Không có file APK tại: {PathApk}");
                return false;
            }

            for (int i = 0; i < 5; i++)
            {
                var pkgs = _service.AppList();
                if (pkgs.Contains(Package)) return true;
                _service.InstallApp(PathApk);
            }

            var final = _service.AppList();
            return final.Contains(Package);
        }

        public async Task<bool> ConnectToWifiNetwork(string wifiSSID, string wifiPassword)
        {
            await EnsureApkAvailableAsync();

            if (!Install())
            {
                LogHelper.Error($"[AdbJoinWifi] Cài {Package} thất bại trên {_service.Device.Serial}");
                return false;
            }

            _service.Shell("su", "-c", "svc wifi enable");
            _service.StopApp(Package);

            var ssid = EscapeShellArg(wifiSSID);
            var pass = EscapeShellArg(wifiPassword);
            _service.Shell($"am start -n {Package}/.MainActivity -e ssid {ssid} -e password_type WPA -e password {pass}");
            return true;
        }

        public async Task DisableWifi()
        {
            _service.Shell("su -c 'svc wifi disable'");
            await Task.CompletedTask;
        }

        public async Task EnableWifi()
        {
            _service.Shell("su -c 'svc wifi enable'");
            await Task.CompletedTask;
        }

        // Single-quote escape for adb shell argv: wrap in '...' and escape internal '.
        private static string EscapeShellArg(string value)
        {
            value ??= "";
            return "'" + value.Replace("'", "'\\''") + "'";
        }
    }
}
