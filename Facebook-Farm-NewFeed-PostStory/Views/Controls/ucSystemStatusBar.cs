using Facebook_Farm_NewFeed_PostStory.Utils.Design;
using Sunny.Subdy.Common.Services;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Views.Controls
{
    /// <summary>
    /// Operations status bar — strip mỏng hiển thị health hệ thống realtime.
    /// Pattern: [Devices: N] | [Running: N] | [Errors: N] | [Queue: N] | [Proxy: %]
    ///
    /// Refresh interval: 2s. Lazy-init: chỉ enable timer khi handle created.
    /// </summary>
    public class ucSystemStatusBar : UserControl
    {
        // UI-thread timer: only triggers a (now-cheap) repaint. Paint reads cached ints.
        private readonly System.Windows.Forms.Timer _timer;
        // Background timer: recomputes the DB-backed account metrics OFF the UI thread.
        private System.Threading.Timer? _metricsTimer;
        private int _metricsRefreshing;            // Interlocked guard against overlapping scans
        private volatile bool _isVisible = true;   // mirrors Visible, updated on the UI thread
        private volatile int _cachedErrors;        // written on bg thread, read on UI paint
        private volatile int _cachedQueue;
        private readonly List<MetricChip> _chips = new();

        // DB-backed metrics are expensive (full account scan) → refresh on a slow cadence.
        // Device metrics are in-memory and stay live via the 2s repaint.
        private const int MetricsRefreshMs = 5000;

        public ucSystemStatusBar()
        {
            DoubleBuffered = true;
            Height        = 32;
            Dock          = DockStyle.Top;
            BackColor     = ColorPalette.Surface;
            Font          = FontScale.Body9;
            Padding       = new Padding(Spacing.Lg, 0, Spacing.Lg, 0);

            // Devices/Running/Online: cheap in-memory list scans → computed inline in paint.
            // Errors/Queue: DB-backed → read from cache populated by the background timer.
            _chips.Add(new MetricChip("Devices",  () => DeviceServices.DeviceModels?.Count ?? 0, ColorPalette.TextSecondary));
            _chips.Add(new MetricChip("Running",  CountRunningDevices, ColorPalette.StateRunning));
            _chips.Add(new MetricChip("Errors",   () => _cachedErrors,  ColorPalette.StateError));
            _chips.Add(new MetricChip("Queue",    () => _cachedQueue,   ColorPalette.StateWaitingOtp));
            _chips.Add(new MetricChip("Online",   CountOnlineDevices,  ColorPalette.Primary));

            _timer = new System.Windows.Forms.Timer { Interval = 2000 };
            _timer.Tick += (_, __) => Invalidate();
            HandleCreated += (_, __) =>
            {
                _timer.Start();
                // Background metrics refresh — never touches the UI thread except a final Invalidate.
                _metricsTimer ??= new System.Threading.Timer(_ => RefreshAccountMetrics(), null, 0, MetricsRefreshMs);
            };
            HandleDestroyed += (_, __) => _timer.Stop();
            VisibleChanged += (_, __) => _isVisible = Visible;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Dispose();
                _metricsTimer?.Dispose();
                _metricsTimer = null;
            }
            base.Dispose(disposing);
        }

        // ── Metric sources ────────────────────────────────────────
        private static int CountRunningDevices()
        {
            var list = DeviceServices.DeviceModels;
            if (list == null) return 0;
            int n = 0;
            foreach (var d in list)
            {
                if (d == null) continue;
                if (d.IsLive || d.IsAdbOnline) n++;
            }
            return n;
        }

        private static int CountOnlineDevices()
        {
            var list = DeviceServices.DeviceModels;
            if (list == null) return 0;
            int n = 0;
            foreach (var d in list)
            {
                if (d == null) continue;
                if (d.IsAdbOnline) n++;
            }
            return n;
        }

        /// <summary>
        /// Recompute the DB-backed Errors/Queue counts on a background thread.
        /// Runs on the <see cref="_metricsTimer"/> threadpool thread — NEVER on the UI thread,
        /// so the message pump is never blocked by the account-table scan. One <c>GetAll</c> per
        /// platform (3 total) counts both metrics in a single pass (was 6 full scans before).
        /// </summary>
        private void RefreshAccountMetrics()
        {
            // Skip while hidden (another tab) and prevent overlapping scans.
            if (!_isVisible || IsDisposed) return;
            if (Interlocked.Exchange(ref _metricsRefreshing, 1) == 1) return;
            try
            {
                int errors = 0, queue = 0;
                var ctx = new AccountContext();
                foreach (var platform in new[] {
                    Sunny.Subdy.Common.Models.PlatformModel.Facebook,
                    Sunny.Subdy.Common.Models.PlatformModel.Instagram,
                    Sunny.Subdy.Common.Models.PlatformModel.Threads })
                {
                    var all = ctx.GetAll(new List<string>(), platform, true);
                    if (all == null) continue;
                    foreach (var a in all)
                    {
                        if (a == null) continue;
                        if (a.Running) queue++;
                        var s = a.Status;
                        if (!string.IsNullOrEmpty(s) &&
                            (s.IndexOf("err", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             s.IndexOf("fail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             s.IndexOf("ban", StringComparison.OrdinalIgnoreCase) >= 0))
                            errors++;
                    }
                }

                bool changed = errors != _cachedErrors || queue != _cachedQueue;
                _cachedErrors = errors;
                _cachedQueue = queue;

                if (changed && !IsDisposed && IsHandleCreated)
                {
                    try { BeginInvoke((Action)Invalidate); } catch { /* handle race during shutdown */ }
                }
            }
            catch { /* metrics are best-effort; never crash the UI */ }
            finally { Interlocked.Exchange(ref _metricsRefreshing, 0); }
        }

        private static readonly Font _labelFont = new Font(FontScale.FamilyName, 8.5F, FontStyle.Regular);
        private static readonly Font _valueFont = TryMonoFont(9F);

        private static Font TryMonoFont(float size)
        {
            try { return new Font("Cascadia Mono", size, FontStyle.Bold); }
            catch { return new Font("Consolas", size, FontStyle.Bold); }
        }

        // ── Paint ─────────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode    = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Bottom divider
            using (var pen = new Pen(ColorPalette.BorderLight, 1))
                g.DrawLine(pen, 0, Height - 1, Width, Height - 1);

            using var labelBrush = new SolidBrush(ColorPalette.TextTertiary);

            int x = Padding.Left;
            int y = Height / 2;

            for (int i = 0; i < _chips.Count; i++)
            {
                var chip = _chips[i];
                int value = SafeGet(chip);

                // Label
                string label = chip.Label + ":";
                var labelSize = g.MeasureString(label, _labelFont);
                g.DrawString(label, _labelFont, labelBrush,
                    x, y - labelSize.Height / 2);
                x += (int)labelSize.Width + Spacing.Xs;

                // Value (colored)
                string valStr = value.ToString();
                var valSize = g.MeasureString(valStr, _valueFont);
                using (var b = new SolidBrush(chip.AccentColor))
                    g.DrawString(valStr, _valueFont, b, x, y - valSize.Height / 2);
                x += (int)valSize.Width + Spacing.Lg;

                // Divider chip
                if (i < _chips.Count - 1)
                {
                    using var divPen = new Pen(ColorPalette.BorderLight, 1);
                    g.DrawLine(divPen, x - Spacing.Sm, 8, x - Spacing.Sm, Height - 8);
                }
            }

            // Right-side "live" pulse dot
            DrawLivePulse(g);
        }

        private void DrawLivePulse(Graphics g)
        {
            int r = 5;
            int cx = Width - Padding.Right - r;
            int cy = Height / 2;
            using var dotBrush = new SolidBrush(ColorPalette.StateRunning);
            g.FillEllipse(dotBrush, cx - r, cy - r, r * 2, r * 2);

            using var brush = new SolidBrush(ColorPalette.TextTertiary);
            string txt = "LIVE";
            var sz = g.MeasureString(txt, _labelFont);
            g.DrawString(txt, _labelFont, brush,
                cx - r - Spacing.Xs - sz.Width, cy - sz.Height / 2);
        }

        private static int SafeGet(MetricChip chip)
        {
            try { return chip.Getter(); }
            catch { return 0; }
        }

        private sealed class MetricChip
        {
            public string Label { get; }
            public Func<int> Getter { get; }
            public Color AccentColor { get; }
            public MetricChip(string label, Func<int> getter, Color accent)
            {
                Label = label; Getter = getter; AccentColor = accent;
            }
        }
    }
}
