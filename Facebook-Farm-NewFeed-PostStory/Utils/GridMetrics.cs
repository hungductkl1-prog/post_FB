using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    /// <summary>GDI / USER object counts for the current process (Priority 5 leak watch).</summary>
    internal static class NativeGuiResources
    {
        private const uint GR_GDIOBJECTS = 0;
        private const uint GR_USEROBJECTS = 1;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);

        // Pseudo-handle for the current process — no Process object / handle to leak.
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        public static int GdiObjects() => Read(GR_GDIOBJECTS);
        public static int UserObjects() => Read(GR_USEROBJECTS);

        private static int Read(uint flag)
        {
            try
            {
                return (int)GetGuiResources(GetCurrentProcess(), flag);
            }
            catch { return -1; }
        }
    }

    /// <summary>
    /// Consolidated grid performance metrics. Outputs every 30 seconds when diagnostics are enabled.
    /// Tracks: CellValueNeeded rate, display cache hit/miss, page fetch timing,
    /// scroll coalescing stats, and memory pressure indicators.
    /// </summary>
    public sealed class GridMetrics : IDisposable
    {
        private readonly System.Threading.Timer _reportTimer;
        private readonly Func<MetricsSnapshot> _snapshotProvider;
        private long _lastReportTick;

        public GridMetrics(Func<MetricsSnapshot> snapshotProvider)
        {
            _snapshotProvider = snapshotProvider;
            _lastReportTick = Stopwatch.GetTimestamp();
            // Report every 30 seconds.
            _reportTimer = new System.Threading.Timer(OnReport, null, 30_000, 30_000);
        }

        private void OnReport(object? state)
        {
            if (!AccountGridPerf.DiagnosticsEnabled) return;

            long now = Stopwatch.GetTimestamp();
            double elapsedSec = (double)(now - _lastReportTick) / Stopwatch.Frequency;
            _lastReportTick = now;

            MetricsSnapshot snap;
            try { snap = _snapshotProvider(); }
            catch { return; }

            var sb = new StringBuilder(512);
            sb.Append("[GridMetrics] Interval=").Append(elapsedSec.ToString("0.0")).Append("s");
            sb.Append(" | CVN: count=").Append(snap.CellValueNeededCount);
            if (elapsedSec > 0)
                sb.Append(" rate=").Append((snap.CellValueNeededCount / elapsedSec).ToString("0")).Append("/s");

            sb.Append(" | CellPaint: count=").Append(snap.CellPaintingCount);
            if (elapsedSec > 0)
                sb.Append(" rate=").Append((snap.CellPaintingCount / elapsedSec).ToString("0")).Append("/s");
            sb.Append(" | CellFormat: count=").Append(snap.CellFormattingCount);
            if (elapsedSec > 0)
                sb.Append(" rate=").Append((snap.CellFormattingCount / elapsedSec).ToString("0")).Append("/s");

            sb.Append(" | DisplayCache: hits=").Append(snap.DisplayCacheHits);
            sb.Append(" misses=").Append(snap.DisplayCacheMisses);
            long total = snap.DisplayCacheHits + snap.DisplayCacheMisses;
            if (total > 0)
                sb.Append(" hitRate=").Append(((double)snap.DisplayCacheHits / total * 100).ToString("0.0")).Append('%');
            sb.Append(" evictions=").Append(snap.DisplayCacheEvictions);
            sb.Append(" entries=").Append(snap.DisplayCacheEntries);

            sb.Append(" | PageFetch: avg=").Append(snap.AvgPageFetchMs.ToString("0")).Append("ms");
            sb.Append(" last=").Append(snap.LastPageFetchMs).Append("ms");

            sb.Append(" | Scroll: requests=").Append(snap.ScrollRequestCount);
            if (elapsedSec > 0)
                sb.Append(" rate=").Append((snap.ScrollRequestCount / elapsedSec).ToString("0.0")).Append("/s");
            sb.Append(" cancelled=").Append(snap.CancelledFetchCount);

            sb.Append(" | Render: wmPaint=").Append(snap.WmPaintCount);
            if (elapsedSec > 0)
                sb.Append(" wmPaint/s=").Append((snap.WmPaintCount / elapsedSec).ToString("0.0"));
            sb.Append(" paintAvg=").Append(snap.AvgPaintMs.ToString("0.000")).Append("ms");
            sb.Append(" scroll=").Append(snap.ScrollEventCount);
            if (elapsedSec > 0)
                sb.Append(" scroll/s=").Append((snap.ScrollEventCount / elapsedSec).ToString("0.0"));
            sb.Append(" scrollAvg=").Append(snap.AvgScrollMs.ToString("0.000")).Append("ms");

            sb.Append(" | Memory: managed=").Append(snap.ManagedHeapMB).Append("MB");
            sb.Append(" working=").Append(snap.WorkingSetMB).Append("MB");
            sb.Append(" gdi=").Append(snap.GdiObjects);
            sb.Append(" user=").Append(snap.UserObjects);
            sb.Append(" cacheRows=").Append(snap.ActiveCacheRows);
            sb.Append(" cachePages=").Append(snap.ActiveCachePages);

            // Emit via the existing perf logger.
            Debug.WriteLine(sb.ToString());
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "grid_metrics.log");
                var dir = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.AppendAllText(logPath,
                    $"{DateTime.Now:HH:mm:ss.fff} {sb}{Environment.NewLine}",
                    Encoding.UTF8);
            }
            catch { /* logging must never break UI */ }
        }

        public void Dispose()
        {
            _reportTimer.Dispose();
        }

        public readonly struct MetricsSnapshot
        {
            public readonly long CellValueNeededCount;
            public readonly long DisplayCacheHits;
            public readonly long DisplayCacheMisses;
            public readonly long DisplayCacheEvictions;
            public readonly int DisplayCacheEntries;
            public readonly long AvgPageFetchMs;
            public readonly long LastPageFetchMs;
            public readonly long ScrollRequestCount;
            public readonly long CancelledFetchCount;
            public readonly long ManagedHeapMB;
            public readonly long WorkingSetMB;
            public readonly int ActiveCacheRows;
            public readonly int ActiveCachePages;
            public readonly long WmPaintCount;
            public readonly double AvgPaintMs;
            public readonly long ScrollEventCount;
            public readonly double AvgScrollMs;
            public readonly int GdiObjects;
            public readonly int UserObjects;
            public readonly long CellPaintingCount;
            public readonly long CellFormattingCount;

            public MetricsSnapshot(
                long cellValueNeededCount,
                long displayCacheHits,
                long displayCacheMisses,
                long displayCacheEvictions,
                int displayCacheEntries,
                long avgPageFetchMs,
                long lastPageFetchMs,
                long scrollRequestCount,
                long cancelledFetchCount,
                long managedHeapMB,
                long workingSetMB,
                int activeCacheRows,
                int activeCachePages,
                long wmPaintCount = 0,
                double avgPaintMs = 0,
                long scrollEventCount = 0,
                double avgScrollMs = 0,
                int gdiObjects = -1,
                int userObjects = -1,
                long cellPaintingCount = 0,
                long cellFormattingCount = 0)
            {
                CellValueNeededCount = cellValueNeededCount;
                DisplayCacheHits = displayCacheHits;
                DisplayCacheMisses = displayCacheMisses;
                DisplayCacheEvictions = displayCacheEvictions;
                DisplayCacheEntries = displayCacheEntries;
                AvgPageFetchMs = avgPageFetchMs;
                LastPageFetchMs = lastPageFetchMs;
                ScrollRequestCount = scrollRequestCount;
                CancelledFetchCount = cancelledFetchCount;
                ManagedHeapMB = managedHeapMB;
                WorkingSetMB = workingSetMB;
                ActiveCacheRows = activeCacheRows;
                ActiveCachePages = activeCachePages;
                WmPaintCount = wmPaintCount;
                AvgPaintMs = avgPaintMs;
                ScrollEventCount = scrollEventCount;
                AvgScrollMs = avgScrollMs;
                GdiObjects = gdiObjects;
                UserObjects = userObjects;
                CellPaintingCount = cellPaintingCount;
                CellFormattingCount = cellFormattingCount;
            }
        }
    }
}
