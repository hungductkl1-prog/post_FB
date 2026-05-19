using LamToolAutoPhonePrime.Utils.Design;
using Sunny.Subdy.Common.Services;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LamToolAutoPhonePrime.Views.Controls
{
    /// <summary>
    /// Operations status bar — strip mỏng hiển thị health hệ thống realtime.
    /// Pattern: [Devices: N] | [Running: N] | [Errors: N] | [Queue: N] | [Proxy: %]
    ///
    /// Refresh interval: 2s. Lazy-init: chỉ enable timer khi handle created.
    /// </summary>
    public class ucSystemStatusBar : UserControl
    {
        private readonly System.Windows.Forms.Timer _timer;
        private readonly List<MetricChip> _chips = new();

        public ucSystemStatusBar()
        {
            DoubleBuffered = true;
            Height        = 32;
            Dock          = DockStyle.Top;
            BackColor     = ColorPalette.Surface;
            Font          = FontScale.Body9;
            Padding       = new Padding(Spacing.Lg, 0, Spacing.Lg, 0);

            _chips.Add(new MetricChip("Devices",  () => DeviceServices.DeviceModels?.Count ?? 0, ColorPalette.TextSecondary));
            _chips.Add(new MetricChip("Running",  CountRunningDevices, ColorPalette.StateRunning));
            _chips.Add(new MetricChip("Errors",   CountErrorAccounts,  ColorPalette.StateError));
            _chips.Add(new MetricChip("Queue",    CountQueueAccounts,  ColorPalette.StateWaitingOtp));
            _chips.Add(new MetricChip("Online",   CountOnlineDevices,  ColorPalette.Primary));

            _timer = new System.Windows.Forms.Timer { Interval = 2000 };
            _timer.Tick += (_, __) => Invalidate();
            HandleCreated += (_, __) => _timer.Start();
            HandleDestroyed += (_, __) => _timer.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer?.Dispose();
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

        private static int CountErrorAccounts()
        {
            try
            {
                var ctx = new AccountContext();
                int total = 0;
                foreach (var platform in new[] {
                    Sunny.Subdy.Common.Models.PlatformModel.Facebook,
                    Sunny.Subdy.Common.Models.PlatformModel.Instagram,
                    Sunny.Subdy.Common.Models.PlatformModel.Threads })
                {
                    var all = ctx.GetAll(new List<string>(), platform, true);
                    if (all == null) continue;
                    foreach (var a in all)
                    {
                        var s = a?.Status;
                        if (!string.IsNullOrEmpty(s) &&
                            (s.IndexOf("err", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             s.IndexOf("fail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             s.IndexOf("ban", StringComparison.OrdinalIgnoreCase) >= 0))
                            total++;
                    }
                }
                return total;
            }
            catch { return 0; }
        }

        private static int CountQueueAccounts()
        {
            try
            {
                var ctx = new AccountContext();
                int total = 0;
                foreach (var platform in new[] {
                    Sunny.Subdy.Common.Models.PlatformModel.Facebook,
                    Sunny.Subdy.Common.Models.PlatformModel.Instagram,
                    Sunny.Subdy.Common.Models.PlatformModel.Threads })
                {
                    var all = ctx.GetAll(new List<string>(), platform, true);
                    if (all == null) continue;
                    foreach (var a in all)
                    {
                        if (a != null && a.Running) total++;
                    }
                }
                return total;
            }
            catch { return 0; }
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

            using var labelFont = new Font(FontScale.FamilyName, 8.5F, FontStyle.Regular);
            using var valueFont = new Font("Cascadia Mono", 9F, FontStyle.Bold);
            using var labelBrush = new SolidBrush(ColorPalette.TextTertiary);

            int x = Padding.Left;
            int y = Height / 2;

            for (int i = 0; i < _chips.Count; i++)
            {
                var chip = _chips[i];
                int value = SafeGet(chip);

                // Label
                string label = chip.Label + ":";
                var labelSize = g.MeasureString(label, labelFont);
                g.DrawString(label, labelFont, labelBrush,
                    x, y - labelSize.Height / 2);
                x += (int)labelSize.Width + Spacing.Xs;

                // Value (colored)
                string valStr = value.ToString();
                var valSize = g.MeasureString(valStr, valueFont);
                using (var b = new SolidBrush(chip.AccentColor))
                    g.DrawString(valStr, valueFont, b, x, y - valSize.Height / 2);
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

            using var labelFont = new Font(FontScale.FamilyName, 8.5F, FontStyle.Regular);
            using var brush = new SolidBrush(ColorPalette.TextTertiary);
            string txt = "LIVE";
            var sz = g.MeasureString(txt, labelFont);
            g.DrawString(txt, labelFont, brush,
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
