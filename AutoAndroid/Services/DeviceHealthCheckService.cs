using AutoAndroid;
using AutoAndroid.Monitoring;
using Sunny.Subdy.Data.Models;
using System.Collections.Concurrent;

namespace Sunny.Subdy.Common.Services
{
    /// <summary>
    /// Chạy nền (background) probe internet cho 1 device sau khi vừa online.
    /// Đã tối ưu:
    ///   - Cache kết quả 60s → bỏ qua nếu đã check gần đây
    ///   - Bỏ qua device đang offline (IsLive=false)
    ///   - Chỉ ping 1 lần (không fallback) — giảm 50% ADB commands
    /// </summary>
    public static class DeviceHealthCheckService
    {
        private static readonly ConcurrentDictionary<string, byte> _inFlight = new();
        private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

        private const int OVERALL_TIMEOUT_MS = 10_000;
        private const int CACHE_TTL_SECONDS = 60;
        private const int PING_TIMEOUT_SECONDS = 4; // giảm từ 6 → 4

        public static void StartAsync(DeviceModel device)
        {
            if (device == null || string.IsNullOrWhiteSpace(device.Serial)) return;

            // Bỏ qua nếu ATX fail — không có ích gì khi check internet
            if (!device.IsLive) return;

            // Cache: nếu đã check trong vòng CACHE_TTL giây, bỏ qua
            if (_cache.TryGetValue(device.Serial, out var cached))
            {
                if ((DateTime.Now - cached.CheckedAt).TotalSeconds < CACHE_TTL_SECONDS)
                {
                    // Dùng kết quả cache
                    device.HasInternet = cached.HasInternet;
                    device.IsRowEnabled = true;
                    return;
                }
            }

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

        /// <summary>Xoá cache cho 1 serial (gọi khi device state thay đổi).</summary>
        public static void InvalidateCache(string serial)
        {
            _cache.TryRemove(serial, out _);
        }

        /// <summary>Xoá toàn bộ cache (gọi khi ADB restart).</summary>
        public static void InvalidateAllCache()
        {
            _cache.Clear();
        }

        private static void Run(DeviceModel device)
        {
            string serial = device.Serial;
            MetricsCollector.Increment("healthcheck.total");

            using var _ = MetricsCollector.Measure("healthcheck.duration.ms");
            bool internet = CheckInternet(serial);

            // Cache kết quả
            _cache[serial] = new CacheEntry { HasInternet = internet, CheckedAt = DateTime.Now };

            device.HasInternet = internet;
            device.IsRowEnabled = true;
            if (internet)
                MetricsCollector.Increment("device.internet.online.count");
            else
                MetricsCollector.Increment("device.internet.offline.count");
        }

        private static bool CheckInternet(string serial)
        {
            // Tối ưu: chỉ ping 1 lần tới 8.8.8.8, không fallback.
            // Lý do: nếu 8.8.8.8 không reachable nhưng google.com reachable là
            // trường hợp cực hiếm, không đáng trả giá 1 ADB command thêm cho mỗi device.
            string ping = ProcessHelper.RunAdbNoRetry(
                $"-s {serial} shell ping -c 1 -W 2 8.8.8.8", PING_TIMEOUT_SECONDS) ?? "";
            return ping.Contains("1 received", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Probe ATX agent qua TCP connect toi port. Nhanh (<100ms), tin cay —
        /// port mo = ATX dang chay. Khong can HTTP request phuc tap.
        /// </summary>
        public static bool PingAtx(int port, int timeoutMs = 2000)
        {
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                var ar = client.BeginConnect("127.0.0.1", port, null, null);
                using (ar.AsyncWaitHandle)
                {
                    if (ar.AsyncWaitHandle.WaitOne(timeoutMs))
                    {
                        client.EndConnect(ar);
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private class CacheEntry
        {
            public bool HasInternet { get; init; }
            public DateTime CheckedAt { get; init; }
        }
    }
}
