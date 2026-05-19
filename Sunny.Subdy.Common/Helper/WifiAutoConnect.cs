using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sunny.Subdy.Common.Helper
{
    /// <summary>
    /// Auto-reconnect Wifi khi host PC mất internet.
    /// Đọc danh sách wifi (ssid|password) từ file <c>Config/wifi-credentials-input.txt</c>
    /// (cùng file do <c>fInputWifiCredentials</c> ghi).
    ///
    /// Cách hoạt động:
    /// 1. <see cref="IsInternetAvailable"/> ping nhanh để check.
    /// 2. Nếu mất: thử từng wifi trong danh sách qua <c>netsh</c>:
    ///    a) Tạo profile XML tạm
    ///    b) <c>netsh wlan add profile</c>
    ///    c) <c>netsh wlan connect</c>
    ///    d) Đợi 5s → re-check internet
    /// 3. Trả về true nếu kết nối lại được, false nếu thất bại với mọi wifi.
    ///
    /// Throttle: chỉ chạy lại sau 30s kể từ lần thử trước (tránh spam netsh
    /// khi loop MainService gọi mỗi vòng).
    /// </summary>
    public static class WifiAutoConnect
    {
        private static readonly string CredentialsPath =
            Path.Combine(System.AppContext.BaseDirectory, "Config", "wifi-credentials-input.txt");

        private static readonly TimeSpan _minRetryInterval = TimeSpan.FromSeconds(30);
        private static DateTime _lastAttempt = DateTime.MinValue;
        private static readonly SemaphoreSlim _gate = new(1, 1);

        /// <summary>Quick internet check — ping 1.1.1.1 với timeout 1.5s.</summary>
        public static bool IsInternetAvailable()
        {
            if (!NetworkInterface.GetIsNetworkAvailable()) return false;
            try
            {
                using var ping = new Ping();
                var reply = ping.Send("1.1.1.1", 1500);
                return reply != null && reply.Status == IPStatus.Success;
            }
            catch { return false; }
        }

        /// <summary>
        /// Nếu mất internet → thử reconnect lần lượt từng wifi đã cấu hình.
        /// Throttled: tối đa 1 lần mỗi 30s.
        /// Returns true nếu đã có internet (sau khi reconnect, hoặc vẫn còn).
        /// </summary>
        public static async Task<bool> EnsureInternetAsync(CancellationToken ct = default)
        {
            if (IsInternetAvailable()) return true;

            // Throttle
            if (DateTime.UtcNow - _lastAttempt < _minRetryInterval) return false;
            if (!await _gate.WaitAsync(0, ct)) return false;
            try
            {
                _lastAttempt = DateTime.UtcNow;
                if (IsInternetAvailable()) return true; // recheck inside lock

                var wifis = LoadWifiList();
                if (wifis.Count == 0)
                {
                    Sunny.Subdy.Common.Logs.LogManager.Warning("[WifiAutoConnect] Mất internet nhưng chưa cấu hình wifi nào. Vào 'Cấu hình wifi' để nhập.");
                    return false;
                }

                foreach (var (ssid, password) in wifis)
                {
                    if (ct.IsCancellationRequested) return false;
                    try
                    {
                        Sunny.Subdy.Common.Logs.LogManager.Info($"[WifiAutoConnect] Thử kết nối wifi '{ssid}'…");
                        if (await TryConnectAsync(ssid, password, ct))
                        {
                            await Task.Delay(3000, ct).ConfigureAwait(false);
                            if (IsInternetAvailable())
                            {
                                Sunny.Subdy.Common.Logs.LogManager.Success($"[WifiAutoConnect] Đã kết nối lại internet qua '{ssid}'.");
                                return true;
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Sunny.Subdy.Common.Logs.LogManager.Warning($"[WifiAutoConnect] Lỗi khi connect '{ssid}': {ex.Message}");
                    }
                }
                Sunny.Subdy.Common.Logs.LogManager.Warning("[WifiAutoConnect] Không thể kết nối lại với bất kỳ wifi nào đã cấu hình.");
                return false;
            }
            finally
            {
                _gate.Release();
            }
        }

        // ── Internals ────────────────────────────────────────
        private static System.Collections.Generic.List<(string ssid, string password)> LoadWifiList()
        {
            var result = new System.Collections.Generic.List<(string, string)>();
            try
            {
                if (!File.Exists(CredentialsPath)) return result;
                foreach (var raw in File.ReadAllLines(CredentialsPath))
                {
                    var line = raw?.Trim() ?? "";
                    if (string.IsNullOrEmpty(line) || !line.Contains('|')) continue;
                    var parts = line.Split('|', 2);
                    var ssid = parts[0].Trim();
                    var pass = parts.Length > 1 ? parts[1].Trim() : "";
                    if (string.IsNullOrEmpty(ssid)) continue;
                    result.Add((ssid, pass));
                }
            }
            catch { /* ignore */ }
            return result;
        }

        private static async Task<bool> TryConnectAsync(string ssid, string password, CancellationToken ct)
        {
            // 1. Tạo profile XML tạm
            var profileXml = BuildWpa2ProfileXml(ssid, password);
            var tmpPath = Path.Combine(Path.GetTempPath(), $"wifi-{System.Guid.NewGuid():N}.xml");
            try
            {
                await File.WriteAllTextAsync(tmpPath, profileXml, Encoding.UTF8, ct).ConfigureAwait(false);

                // 2. Add profile
                if (!RunNetsh($"wlan add profile filename=\"{tmpPath}\"", out var err1))
                {
                    Sunny.Subdy.Common.Logs.LogManager.Warning($"[WifiAutoConnect] add profile fail: {err1}");
                    return false;
                }

                // 3. Connect
                if (!RunNetsh($"wlan connect name=\"{ssid}\"", out var err2))
                {
                    Sunny.Subdy.Common.Logs.LogManager.Warning($"[WifiAutoConnect] connect fail: {err2}");
                    return false;
                }
                return true;
            }
            finally
            {
                try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
            }
        }

        private static bool RunNetsh(string args, out string stderr)
        {
            stderr = "";
            try
            {
                var psi = new ProcessStartInfo("netsh", args)
                {
                    CreateNoWindow         = true,
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                };
                using var p = Process.Start(psi);
                if (p == null) return false;
                if (!p.WaitForExit(8000)) { try { p.Kill(true); } catch { } return false; }
                stderr = p.StandardError.ReadToEnd();
                return p.ExitCode == 0;
            }
            catch (System.Exception ex)
            {
                stderr = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Profile XML cho WPA2-PSK (hệ thống phổ biến nhất).
        /// Nếu password rỗng → mạng mở (authentication=open, encryption=none).
        /// </summary>
        private static string BuildWpa2ProfileXml(string ssid, string password)
        {
            string ssidHex = ToHex(ssid);
            if (string.IsNullOrEmpty(password))
            {
                return $@"<?xml version=""1.0""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
  <name>{ssid}</name>
  <SSIDConfig>
    <SSID>
      <hex>{ssidHex}</hex>
      <name>{ssid}</name>
    </SSID>
  </SSIDConfig>
  <connectionType>ESS</connectionType>
  <connectionMode>auto</connectionMode>
  <MSM>
    <security>
      <authEncryption>
        <authentication>open</authentication>
        <encryption>none</encryption>
        <useOneX>false</useOneX>
      </authEncryption>
    </security>
  </MSM>
</WLANProfile>";
            }

            return $@"<?xml version=""1.0""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
  <name>{ssid}</name>
  <SSIDConfig>
    <SSID>
      <hex>{ssidHex}</hex>
      <name>{ssid}</name>
    </SSID>
  </SSIDConfig>
  <connectionType>ESS</connectionType>
  <connectionMode>auto</connectionMode>
  <MSM>
    <security>
      <authEncryption>
        <authentication>WPA2PSK</authentication>
        <encryption>AES</encryption>
        <useOneX>false</useOneX>
      </authEncryption>
      <sharedKey>
        <keyType>passPhrase</keyType>
        <protected>false</protected>
        <keyMaterial>{System.Security.SecurityElement.Escape(password)}</keyMaterial>
      </sharedKey>
    </security>
  </MSM>
</WLANProfile>";
        }

        private static string ToHex(string s)
        {
            var sb = new StringBuilder(s.Length * 2);
            foreach (var b in Encoding.UTF8.GetBytes(s))
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
    }
}
