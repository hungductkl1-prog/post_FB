using AutoAndroid;
using Sunny.Subdy.Data.Models;
using System.Collections.Concurrent;

namespace Sunny.Subdy.Common.Services
{
    /// <summary>
    /// Chạy nền (background) các check cấu hình cơ bản của 1 device sau khi
    /// vừa được phát hiện online: Wifi / App Facebook / Tiếng Anh / GolikeHelper / GPS / Âm thanh.
    /// Kết quả ghi vào DeviceModel.Status dạng:
    /// "Ok (✅ Wifi, ✅ App Fb, ✅ Tiếng Anh, ✅ GoLikeHelper, GPS: tắt, Âm thanh: tắt)"
    ///
    /// Why: yêu cầu hiển thị nhanh trạng thái thiết lập máy ngay khi máy hiện trên grid,
    /// không block UI và không lặp lại check khi đã từng check trong session.
    /// </summary>
    public static class DeviceHealthCheckService
    {
        // Mỗi serial chỉ chạy 1 lần tại 1 thời điểm (tránh trùng task khi track-devices fire dồn).
        private static readonly ConcurrentDictionary<string, byte> _inFlight = new();

        public const string FacebookKatana = "com.facebook.katana";
        public const string FacebookLite = "com.facebook.lite";
        public const string GolikeHelper = "com.golike.helper";

        // Overall timeout cho TẤT CẢ các probe của 1 device. Nếu vượt thì hiển thị
        // status "Kiểm tra timeout" thay vì kẹt mãi ở "Đang kiểm tra...".
        // Worst case 1 probe: ping 2x6s + pm 8s + getprop 2x5s + settings 2x5s + dumpsys 6s = ~50s tuần tự
        // nhưng đang chạy song song → max ≈ 12s. Cho thêm buffer → 15s.
        private const int OVERALL_TIMEOUT_MS = 15_000;

        public static void StartAsync(DeviceModel device)
        {
            if (device == null || string.IsNullOrWhiteSpace(device.Serial)) return;

            // Bỏ qua nếu ATX fail — máy này user đã thấy status "Không connect được ATX"
            // ở ConnectAll, không có nghĩa khi probe tiếp app/lang/gps.
            if (!device.IsLive) return;

            if (!_inFlight.TryAdd(device.Serial, 0)) return;

            _ = Task.Run(() =>
            {
                try
                {
                    Run(device);
                }
                catch
                {
                    // best-effort: không ảnh hưởng pipeline khác
                }
                finally
                {
                    _inFlight.TryRemove(device.Serial, out _);
                }
            });
        }

        private static void Run(DeviceModel device)
        {
            string serial = device.Serial;
            device.Status = "Đang kiểm tra...";

            // Chạy song song các probe ADB ngắn để tiết kiệm thời gian.
            var tNet = Task.Run(() => CheckInternet(serial));
            var tApps = Task.Run(() => CheckApps(serial));
            var tLang = Task.Run(() => CheckEnglish(serial));
            var tGps = Task.Run(() => CheckGpsOff(serial));
            var tVol = Task.Run(() => CheckMuted(serial));

            // Overall timeout: nếu tổng các probe vượt 15s → bail out, hiển thị lỗi
            // thay vì kẹt ở "Đang kiểm tra..." vô thời hạn (xảy ra khi máy busy nặng,
            // pm list packages treo, hoặc adb server đang queue lệnh từ ConnectAll).
            bool finished = Task.WaitAll(new Task[] { tNet, tApps, tLang, tGps, tVol }, OVERALL_TIMEOUT_MS);
            if (!finished)
            {
                device.Status = $"<FAIL>Kiểm tra timeout ({OVERALL_TIMEOUT_MS / 1000}s)";
                return;
            }

            bool internet = tNet.Result;
            var (hasFb, hasGolike) = tApps.Result;
            bool english = tLang.Result;
            bool gpsOff = tGps.Result;
            bool muted = tVol.Result;

            // Expose internet flag để ucManagerDevices disable row khi không có mạng.
            device.HasInternet = internet;
            // Re-evaluate IsRowEnabled = internet && ATX live.
            device.IsRowEnabled = internet && device.IsLive;

            // Format mới: dùng marker text-only để DataGridView custom-paint
            // tô màu xanh/đỏ cho từng segment. Format:
            //   "<OK>Internet|<FAIL>App Fb|<OK>Tiếng Anh|<OK>GolikeHelper|<OK>GPS: tắt|<OK>Âm thanh: tắt"
            // Renderer trong ucManagerDevices.cs parse separator '|' và prefix
            // "<OK>"/"<FAIL>" để chọn màu. Người dùng nhìn 1 phát biết check nào lỗi.
            string seg(bool ok, string label) => (ok ? "<OK>" : "<FAIL>") + label;

            string status = string.Join("|",
                seg(internet,  "Internet"),
                seg(hasFb,     "App Fb"),
                seg(english,   "Tiếng Anh"),
                seg(hasGolike, "GolikeHelper"),
                seg(gpsOff,    "GPS: " + (gpsOff ? "tắt" : "bật")),
                seg(muted,     "Âm thanh: " + (muted ? "tắt" : "bật"))
            );

            device.Status = status;
        }

        private static bool CheckInternet(string serial)
        {
            // Ping nhẹ 1 gói tới Google DNS (8.8.8.8), timeout 2s.
            // Why: yêu cầu kiểm tra "có internet" thật chứ không chỉ wifi enabled — có máy bật
            //      wifi nhưng router không có WAN, hoặc đang ở chế độ data di động.
            string ping = ProcessHelper.RunAdbNoRetry(
                $"-s {serial} shell ping -c 1 -W 2 8.8.8.8", 6) ?? "";
            if (ping.Contains("1 received", StringComparison.OrdinalIgnoreCase)
                || ping.Contains("1 packets received", StringComparison.OrdinalIgnoreCase))
                return true;

            // Fallback: ping DNS theo hostname để chắc cả DNS resolver hoạt động.
            string ping2 = ProcessHelper.RunAdbNoRetry(
                $"-s {serial} shell ping -c 1 -W 2 google.com", 6) ?? "";
            return ping2.Contains("1 received", StringComparison.OrdinalIgnoreCase)
                || ping2.Contains("1 packets received", StringComparison.OrdinalIgnoreCase);
        }

        private static (bool hasFb, bool hasGolike) CheckApps(string serial)
        {
            string list = ProcessHelper.RunAdbNoRetry($"-s {serial} shell pm list packages", 8) ?? "";
            bool hasFb = list.Contains(FacebookKatana, StringComparison.OrdinalIgnoreCase)
                      || list.Contains(FacebookLite, StringComparison.OrdinalIgnoreCase);
            bool hasGolike = list.Contains(GolikeHelper, StringComparison.OrdinalIgnoreCase);
            return (hasFb, hasGolike);
        }

        private static bool CheckEnglish(string serial)
        {
            // `getprop persist.sys.locale` trả về "en-US" / "en" ... hoặc rỗng nếu chưa set.
            string locale = ProcessHelper.RunAdbNoRetry($"-s {serial} shell getprop persist.sys.locale", 5)?.Trim() ?? "";
            if (string.IsNullOrEmpty(locale))
                locale = ProcessHelper.RunAdbNoRetry($"-s {serial} shell getprop ro.product.locale", 5)?.Trim() ?? "";
            return locale.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        }

        private static bool CheckGpsOff(string serial)
        {
            // 2 cách: location_providers_allowed (deprecated nhưng đa số ROM vẫn còn),
            // và location_mode (0 = off).
            string providers = ProcessHelper.RunAdbNoRetry(
                $"-s {serial} shell settings get secure location_providers_allowed", 5)?.Trim() ?? "";
            if (!string.IsNullOrEmpty(providers) && providers != "null")
            {
                // Có chứa "gps" hoặc "network" nghĩa là đang BẬT.
                bool on = providers.Contains("gps", StringComparison.OrdinalIgnoreCase)
                       || providers.Contains("network", StringComparison.OrdinalIgnoreCase);
                if (on) return false;
            }

            string mode = ProcessHelper.RunAdbNoRetry(
                $"-s {serial} shell settings get secure location_mode", 5)?.Trim() ?? "";
            if (int.TryParse(mode, out int m)) return m == 0;

            // Không xác định được → coi như đang bật (an toàn hơn cho user).
            return false;
        }

        private static bool CheckMuted(string serial)
        {
            // Kiểm tra stream 3 (MUSIC) — nếu volume = 0 coi là đã tắt âm.
            string vol = ProcessHelper.RunAdbNoRetry(
                $"-s {serial} shell media volume --stream 3 --get", 5) ?? "";
            // Output dạng: "volume is 0 in range [0..15]"
            var match = System.Text.RegularExpressions.Regex.Match(vol, @"volume is (\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int v))
                return v == 0;

            // Fallback: dumpsys audio — tìm "STREAM_MUSIC ... muted: true" hoặc "Current: ... 3 (MUSIC): ... 0".
            string audio = ProcessHelper.RunAdbNoRetry($"-s {serial} shell dumpsys audio", 6) ?? "";
            if (audio.Contains("Muted: true", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
