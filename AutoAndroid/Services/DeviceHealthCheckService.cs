using AutoAndroid;
using Sunny.Subdy.Data.Models;
using System.Collections.Concurrent;

namespace Sunny.Subdy.Common.Services
{
    /// <summary>
    /// Chạy nền (background) probe internet cho 1 device sau khi vừa online.
    /// Set DeviceModel.HasInternet/IsRowEnabled để gate row trong grid;
    /// không ghi segment status (App Fb/Tiếng Anh/QNHelper/GPS/Âm thanh) lên UI.
    /// </summary>
    public static class DeviceHealthCheckService
    {
        // Mỗi serial chỉ chạy 1 lần tại 1 thời điểm (tránh trùng task khi track-devices fire dồn).
        private static readonly ConcurrentDictionary<string, byte> _inFlight = new();

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

            // Chỉ check internet để set HasInternet/IsRowEnabled (gate chức năng farming).
            // Không probe App/Lang/GPS/Volume và không ghi segment status lên grid theo yêu cầu.
            var tNet = Task.Run(() => CheckInternet(serial));
            bool finished = tNet.Wait(OVERALL_TIMEOUT_MS);
            bool internet = finished && tNet.Result;

            device.HasInternet = internet;
            device.IsRowEnabled = internet && device.IsLive;
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

    }
}
