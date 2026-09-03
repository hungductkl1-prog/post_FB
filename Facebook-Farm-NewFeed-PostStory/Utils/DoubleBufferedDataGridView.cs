using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    /// <summary>
    /// DataGridView subclass with DoubleBuffered enabled via protected property override.
    /// Use this instead of reflection (InvokeMember/GetProperty NonPublic) which breaks in AOT.
    ///
    /// Also carries lightweight render instrumentation (Priority 2 hot-path audit):
    /// WM_PAINT count, full-grid OnPaint duration, and Scroll event count/duration.
    /// All hooks are gated behind <see cref="AccountGridPerf.DiagnosticsEnabled"/> so there
    /// is zero overhead in production.
    /// </summary>
    public class DoubleBufferedDataGridView : DataGridView
    {
        private const int WM_PAINT = 0x000F;

        private long _wmPaintCount;
        private long _paintCalls;
        private long _paintTicks;
        private long _scrollCount;
        private long _scrollTicks;

        public DoubleBufferedDataGridView()
        {
            DoubleBuffered = true;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_PAINT && AccountGridPerf.DiagnosticsEnabled)
                Interlocked.Increment(ref _wmPaintCount);
            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!AccountGridPerf.DiagnosticsEnabled)
            {
                base.OnPaint(e);
                return;
            }
            long start = Stopwatch.GetTimestamp();
            base.OnPaint(e);
            Interlocked.Add(ref _paintTicks, Stopwatch.GetTimestamp() - start);
            Interlocked.Increment(ref _paintCalls);
            UiThreadProfiler.End("Grid.OnPaint(full)", start);
        }

        protected override void OnScroll(ScrollEventArgs e)
        {
            if (!AccountGridPerf.DiagnosticsEnabled)
            {
                base.OnScroll(e);
                return;
            }
            long start = Stopwatch.GetTimestamp();
            base.OnScroll(e);
            Interlocked.Add(ref _scrollTicks, Stopwatch.GetTimestamp() - start);
            Interlocked.Increment(ref _scrollCount);
            UiThreadProfiler.End("Grid.OnScroll", start);
        }

        public readonly struct RenderStats
        {
            public readonly long WmPaintCount;
            public readonly long PaintCalls;
            public readonly double AvgPaintMs;
            public readonly long ScrollCount;
            public readonly double AvgScrollMs;

            public RenderStats(long wmPaintCount, long paintCalls, double avgPaintMs, long scrollCount, double avgScrollMs)
            {
                WmPaintCount = wmPaintCount;
                PaintCalls = paintCalls;
                AvgPaintMs = avgPaintMs;
                ScrollCount = scrollCount;
                AvgScrollMs = avgScrollMs;
            }
        }

        /// <summary>Read+reset render counters for periodic metrics reporting.</summary>
        public RenderStats TakeRenderStats()
        {
            long wmPaint = Interlocked.Exchange(ref _wmPaintCount, 0);
            long paintCalls = Interlocked.Exchange(ref _paintCalls, 0);
            long paintTicks = Interlocked.Exchange(ref _paintTicks, 0);
            long scrollCount = Interlocked.Exchange(ref _scrollCount, 0);
            long scrollTicks = Interlocked.Exchange(ref _scrollTicks, 0);

            double avgPaintMs = paintCalls <= 0 ? 0 : (double)paintTicks / paintCalls / Stopwatch.Frequency * 1000.0;
            double avgScrollMs = scrollCount <= 0 ? 0 : (double)scrollTicks / scrollCount / Stopwatch.Frequency * 1000.0;
            return new RenderStats(wmPaint, paintCalls, avgPaintMs, scrollCount, avgScrollMs);
        }
    }
}
