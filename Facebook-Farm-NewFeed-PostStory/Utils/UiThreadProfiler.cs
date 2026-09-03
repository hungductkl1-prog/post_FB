using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    /// <summary>
    /// Lightweight UI-thread profiler. Aggregates named scopes (calls / total ticks / max ticks)
    /// and emits a Top-N "by UI time" table every 30s. Also runs a dispatch-latency probe that
    /// posts a sentinel to the UI thread on a fixed cadence and measures how long it waits to run
    /// → a direct proxy for UI Busy% (if the UI thread is churning, the sentinel is delayed).
    ///
    /// All hooks are gated behind <see cref="AccountGridPerf.DiagnosticsEnabled"/> → zero prod cost.
    /// Hot-path usage: <c>long t = UiThreadProfiler.Begin(); ... UiThreadProfiler.End("Name", t);</c>
    /// </summary>
    public static class UiThreadProfiler
    {
        private sealed class Bucket
        {
            public long Calls;
            public long Ticks;
            public long MaxTicks;
        }

        private static readonly ConcurrentDictionary<string, Bucket> _buckets = new();
        private static readonly Func<string, Bucket> _factory = _ => new Bucket();

        private static System.Threading.Timer? _reportTimer;
        private static System.Threading.Timer? _probeTimer;
        private static ISynchronizeInvoke? _uiTarget;
        private static long _lastReportTick;

        // Dispatch-latency probe state (UI busy proxy).
        private static long _probeCount;
        private static long _probeLatencyTicks;
        private static long _probeMaxLatencyTicks;
        private const int ProbeIntervalMs = 200;

        public static bool Enabled => AccountGridPerf.DiagnosticsEnabled;

        public static long Begin() => Enabled ? Stopwatch.GetTimestamp() : 0;

        public static void End(string name, long start)
        {
            if (start == 0 || !Enabled) return;
            long delta = Stopwatch.GetTimestamp() - start;
            var b = _buckets.GetOrAdd(name, _factory);
            Interlocked.Increment(ref b.Calls);
            Interlocked.Add(ref b.Ticks, delta);
            long prev = Interlocked.Read(ref b.MaxTicks);
            while (delta > prev)
            {
                long was = Interlocked.CompareExchange(ref b.MaxTicks, delta, prev);
                if (was == prev) break;
                prev = was;
            }
        }

        /// <summary>Increment a pure call counter (no timing) — e.g. WM_PAINT, Invoke posts.</summary>
        public static void Count(string name)
        {
            if (!Enabled) return;
            var b = _buckets.GetOrAdd(name, _factory);
            Interlocked.Increment(ref b.Calls);
        }

        /// <summary>Start the 30s report + UI-busy probe. Call once from the grid ctor (UI thread).</summary>
        public static void Start(ISynchronizeInvoke uiTarget)
        {
            if (!Enabled || _reportTimer != null) return;
            _uiTarget = uiTarget;
            _lastReportTick = Stopwatch.GetTimestamp();
            _reportTimer = new System.Threading.Timer(_ => Report(), null, 10_000, 10_000);
            _probeTimer = new System.Threading.Timer(_ => Probe(), null, ProbeIntervalMs, ProbeIntervalMs);
        }

        public static void Stop()
        {
            // Emit a final window so the last (often un-flushed) operational activity is captured.
            try { if (Enabled) Report(); } catch { }
            _reportTimer?.Dispose(); _reportTimer = null;
            _probeTimer?.Dispose(); _probeTimer = null;
            _uiTarget = null;
        }

        private static void Probe()
        {
            var target = _uiTarget;
            if (target == null) return;
            try
            {
                if (target is Control c && (c.IsDisposed || !c.IsHandleCreated)) return;
                long scheduled = Stopwatch.GetTimestamp();
                target.BeginInvoke(new Action(() =>
                {
                    long waited = Stopwatch.GetTimestamp() - scheduled;
                    Interlocked.Increment(ref _probeCount);
                    Interlocked.Add(ref _probeLatencyTicks, waited);
                    long prev = Interlocked.Read(ref _probeMaxLatencyTicks);
                    while (waited > prev)
                    {
                        long was = Interlocked.CompareExchange(ref _probeMaxLatencyTicks, waited, prev);
                        if (was == prev) break;
                        prev = was;
                    }
                }), null);
            }
            catch { /* handle race during shutdown */ }
        }

        private static double TicksToMs(long ticks) => (double)ticks / Stopwatch.Frequency * 1000.0;

        private static void Report()
        {
            if (!Enabled) return;
            long now = Stopwatch.GetTimestamp();
            double elapsedSec = (double)(now - _lastReportTick) / Stopwatch.Frequency;
            _lastReportTick = now;
            if (elapsedSec <= 0) elapsedSec = 30;
            double windowMs = elapsedSec * 1000.0;

            // Snapshot + reset all buckets.
            var rows = new System.Collections.Generic.List<(string name, long calls, long ticks, long maxTicks)>(_buckets.Count);
            foreach (var kv in _buckets)
            {
                var b = kv.Value;
                long calls = Interlocked.Exchange(ref b.Calls, 0);
                long ticks = Interlocked.Exchange(ref b.Ticks, 0);
                long maxT = Interlocked.Exchange(ref b.MaxTicks, 0);
                if (calls == 0 && ticks == 0) continue;
                rows.Add((kv.Key, calls, ticks, maxT));
            }
            rows.Sort((a, b) => b.ticks.CompareTo(a.ticks));

            // UI busy probe.
            long pCount = Interlocked.Exchange(ref _probeCount, 0);
            long pLat = Interlocked.Exchange(ref _probeLatencyTicks, 0);
            long pMax = Interlocked.Exchange(ref _probeMaxLatencyTicks, 0);
            double avgLatMs = pCount <= 0 ? 0 : TicksToMs(pLat) / pCount;
            double maxLatMs = TicksToMs(pMax);
            // Busy proxy: total measured UI time across scopes / wall window.
            long totalUiTicks = 0;
            foreach (var r in rows) totalUiTicks += r.ticks;
            double busyPct = TicksToMs(totalUiTicks) / windowMs * 100.0;

            var sb = new StringBuilder(1024);
            sb.Append("[UiProfiler] window=").Append(elapsedSec.ToString("0.0")).Append("s")
              .Append(" UIBusy~=").Append(busyPct.ToString("0.0")).Append('%')
              .Append(" UIIdle~=").Append((100.0 - busyPct).ToString("0.0")).Append('%')
              .Append(" probeDispatchAvg=").Append(avgLatMs.ToString("0.00")).Append("ms")
              .Append(" probeDispatchMax=").Append(maxLatMs.ToString("0.0")).Append("ms")
              .Append(" (n=").Append(pCount).Append(')');
            AccountGridPerf.LogLine(sb.ToString());

            // Top-10 table.
            AccountGridPerf.LogLine("[UiProfiler] Top by UI time | Function | Calls/s | Avg ms | Max ms | Total ms/s | % UI");
            int top = Math.Min(10, rows.Count);
            for (int i = 0; i < top; i++)
            {
                var r = rows[i];
                double totalMs = TicksToMs(r.ticks);
                double avgMs = r.calls <= 0 ? 0 : totalMs / r.calls;
                double maxMs = TicksToMs(r.maxTicks);
                double callsPerSec = r.calls / elapsedSec;
                double totalMsPerSec = totalMs / elapsedSec;
                double pctUi = totalMs / windowMs * 100.0;
                var line = new StringBuilder(160);
                line.Append("[UiProfiler]  #").Append(i + 1).Append(' ')
                    .Append(r.name)
                    .Append(" | ").Append(callsPerSec.ToString("0"))
                    .Append(" | ").Append(avgMs.ToString("0.000"))
                    .Append(" | ").Append(maxMs.ToString("0.000"))
                    .Append(" | ").Append(totalMsPerSec.ToString("0.00"))
                    .Append(" | ").Append(pctUi.ToString("0.0")).Append('%');
                AccountGridPerf.LogLine(line.ToString());
            }
        }
    }
}
