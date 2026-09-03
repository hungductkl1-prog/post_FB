using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace AutoAndroid.Monitoring
{
    /// <summary>
    /// Thread-safe metrics collector cho toàn bộ hệ thống.
    /// Dùng để đo điểm nghẽn (bottleneck) trước và sau khi tối ưu.
    /// </summary>
    public static class MetricsCollector
    {
        private static long _s_totalSeed = 0;
        private static readonly ConcurrentDictionary<string, long> _counters = new();
        private static readonly ConcurrentDictionary<string, long> _gauges = new();
        private static readonly ConcurrentDictionary<string, TimingBucket> _timings = new();

        // --- Counters (chỉ tăng, không giảm) ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Increment(string name, long value = 1)
        {
            _counters.AddOrUpdate(name, value, (_, old) => old + value);
        }

        public static long GetCounter(string name)
        {
            return _counters.TryGetValue(name, out var val) ? val : 0;
        }

        // --- Gauges (có thể tăng/giảm, phản ánh giá trị hiện tại) ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GaugeAdd(string name, long value)
        {
            _gauges.AddOrUpdate(name, value, (_, old) => old + value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GaugeSet(string name, long value)
        {
            _gauges.AddOrUpdate(name, value, (_, _) => value);
        }

        public static long GetGauge(string name)
        {
            return _gauges.TryGetValue(name, out var val) ? val : 0;
        }

        // --- Timing ---

        /// <summary>Bắt đầu 1 measurement. Dùng với Dispose(): using var t = Measure("name");</summary>
        public static TimingSession Measure(string name)
        {
            return new TimingSession(name);
        }

        public static void RecordTiming(string name, long elapsedMs)
        {
            var bucket = _timings.GetOrAdd(name, _ => new TimingBucket());
            bucket.Add(elapsedMs);
        }

        public static TimingStats GetTimingStats(string name)
        {
            if (_timings.TryGetValue(name, out var bucket))
                return bucket.GetStats();
            return TimingStats.Zero;
        }

        // --- Utility: unique ID for correlating events ---

        public static long NextSequence() => Interlocked.Increment(ref _s_totalSeed);

        // --- Snapshot ---

        /// <summary>Lấy toàn bộ metric dạng text để log định kỳ (1 phút/lần).</summary>
        public static string DumpToText()
        {
            var sb = new StringBuilder(2048);
            sb.AppendLine("=== METRICS SNAPSHOT ===");

            sb.AppendLine("-- Counters --");
            foreach (var kv in _counters.OrderBy(k => k.Key))
                sb.AppendLine($"  {kv.Key}: {kv.Value}");

            sb.AppendLine("-- Gauges --");
            foreach (var kv in _gauges.OrderBy(k => k.Key))
                sb.AppendLine($"  {kv.Key}: {kv.Value}");

            sb.AppendLine("-- Timings (avg/95th/max/count) --");
            foreach (var kv in _timings.OrderBy(k => k.Key))
            {
                var s = kv.Value.GetStats();
                sb.AppendLine($"  {kv.Key}: avg={s.AverageMs:F1}ms p95={s.P95Ms:F0}ms max={s.MaxMs}ms n={s.Count}");
            }

            sb.AppendLine("-- Environment --");
            sb.AppendLine($"  Threads: {Process.GetCurrentProcess().Threads.Count}");
            sb.AppendLine($"  Handles: {Process.GetCurrentProcess().HandleCount}");
            sb.AppendLine($"  Memory: {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MB");

            sb.AppendLine("=== END ===");
            return sb.ToString();
        }

        /// <summary>Reset toàn bộ metrics (dùng giữa các phase test).</summary>
        public static void Reset()
        {
            _counters.Clear();
            _gauges.Clear();
            _timings.Clear();
        }

        // --- Inner types ---

        public readonly struct TimingSession : IDisposable
        {
            private readonly string _name;
            private readonly long _startTicks;

            internal TimingSession(string name)
            {
                _name = name;
                _startTicks = Stopwatch.GetTimestamp();
            }

            public void Dispose()
            {
                var elapsed = (Stopwatch.GetTimestamp() - _startTicks) * 1000 / Stopwatch.Frequency;
                RecordTiming(_name, elapsed);
            }
        }

        private class TimingBucket
        {
            private readonly ConcurrentBag<long> _samples = new();
            private long _count;
            private long _sum;
            private long _max;

            public void Add(long ms)
            {
                _samples.Add(ms);
                Interlocked.Increment(ref _count);
                Interlocked.Add(ref _sum, ms);
                // max: simple CAS loop
                long oldMax;
                do { oldMax = _max; }
                while (ms > oldMax && Interlocked.CompareExchange(ref _max, ms, oldMax) != oldMax);
            }

            public TimingStats GetStats()
            {
                var count = Volatile.Read(ref _count);
                if (count == 0) return TimingStats.Zero;

                var sorted = _samples.OrderBy(v => v).ToArray();
                var avg = (double)Volatile.Read(ref _sum) / count;
                var max = Volatile.Read(ref _max);
                var p95 = sorted.Length > 0 ? sorted[(int)(sorted.Length * 0.95)] : 0;
                return new TimingStats(avg, p95, max, count);
            }
        }

        public readonly record struct TimingStats(double AverageMs, long P95Ms, long MaxMs, long Count)
        {
            public static readonly TimingStats Zero = new(0, 0, 0, 0);
        }
    }

    /// <summary>
    /// Per-device metrics. Lưu trong ConcurrentDictionary theo Serial.
    /// </summary>
    public class DeviceMetrics
    {
        public string Serial { get; }
        public long ConnectAdbLoopCount;
        public long ConnectAdbSuccessCount;
        public long AdbShellRetryCount;
        public long AtxReconnectCount;
        public long JsonRpcFailCount;
        public long StateTransitionCount;
        public string? LastError;
        public string? LastErrorTimestamp;
        public DateTime? LastConnectedAt;
        public DateTime? LastDisconnectedAt;

        public DeviceMetrics(string serial)
        {
            Serial = serial;
        }

        public override string ToString()
        {
            return $"[{Serial}] connects={ConnectAdbSuccessCount} retries={AdbShellRetryCount} "
                 + $"atxRecon={AtxReconnectCount} jsonRpcFail={JsonRpcFailCount} "
                 + $"transitions={StateTransitionCount} lastError={LastError}";
        }
    }

    /// <summary>
    /// Quản lý tất cả DeviceMetrics. Singleton.
    /// </summary>
    public static class DeviceMetricsRegistry
    {
        private static readonly ConcurrentDictionary<string, DeviceMetrics> _metrics = new(StringComparer.OrdinalIgnoreCase);

        public static DeviceMetrics GetOrCreate(string serial)
        {
            return _metrics.GetOrAdd(serial, s => new DeviceMetrics(s));
        }

        public static DeviceMetrics? Get(string serial)
        {
            return _metrics.TryGetValue(serial, out var m) ? m : null;
        }

        public static IReadOnlyCollection<DeviceMetrics> GetAll() => _metrics.Values.ToList();

        public static void Remove(string serial)
        {
            _metrics.TryRemove(serial, out _);
        }

        public static string DumpAll()
        {
            var sb = new StringBuilder(4096);
            sb.AppendLine("=== DEVICE METRICS ===");
            foreach (var m in _metrics.Values.OrderBy(d => d.Serial))
                sb.AppendLine($"  {m}");
            sb.AppendLine("=== END ===");
            return sb.ToString();
        }

        public static void Clear() => _metrics.Clear();
    }
}
