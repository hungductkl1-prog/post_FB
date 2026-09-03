using Facebook_Farm_NewFeed_PostStory.Utils.Design;
using Sunny.Subdy.Common.Services;
using Sunny.Subdy.Data.Context;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Views.Controls
{
    /// <summary>
    /// Operations Dashboard widget strip — 6 KPI cards dock Top.
    /// Designed to mount above existing ucHistoriesJob trên tab Dashboard.
    ///
    /// Cards:
    /// 1. Today Jobs       — jobs completed today
    /// 2. Running Devices  — adb-online devices
    /// 3. Alerts           — accounts in error/banned state
    /// 4. Recent Errors    — last N errors trong queue
    /// 5. Success Rate     — running / total accounts
    /// 6. Total Accounts   — toàn bộ accounts FB+IG+TH
    /// </summary>
    public class ucOperationsDashboard : UserControl
    {
        private const int CardHeight = 96;
        private const int CardGap    = 12;
        private const int CardCount  = 6;

        private static readonly Font _labelFont = new Font(FontScale.FamilyName, 8.5F, FontStyle.Regular);
        private static readonly Font _valueFont = TryMonoFont(22F);

        private readonly System.Windows.Forms.Timer _timer;
        private readonly DashCard[] _cards;

        public ucOperationsDashboard()
        {
            DoubleBuffered = true;
            Dock           = DockStyle.Top;
            Height         = CardHeight + Spacing.Lg * 2;
            BackColor      = ColorPalette.Background;
            Padding        = new Padding(Spacing.Lg);

            _cards = new[]
            {
                new DashCard("Active Queue",     ColorPalette.Primary,        "RocketOutlined",        () => CountTodayJobs()),
                new DashCard("Running Devices", ColorPalette.StateRunning,   "MobileOutlined",        () => CountRunningDevices()),
                new DashCard("Alerts",          ColorPalette.Error,          "WarningOutlined",       () => CountAlerts()),
                new DashCard("Queue",           ColorPalette.StateWaitingOtp,"ClockCircleOutlined",   () => CountQueue()),
                new DashCard("Success Rate",    ColorPalette.Success,        "CheckCircleOutlined",   () => SuccessRatePercent(),     isPercent: true),
                new DashCard("Total Accounts",  ColorPalette.TextPrimary,    "TeamOutlined",          () => CountTotalAccounts()),
            };

            _timer = new System.Windows.Forms.Timer { Interval = 3000 };
            _timer.Tick += (_, __) => Invalidate();
            HandleCreated += (_, __) => _timer.Start();
            HandleDestroyed += (_, __) => _timer.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer?.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode    = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var inner = new Rectangle(
                Padding.Left, Padding.Top,
                Width  - Padding.Horizontal,
                Height - Padding.Vertical);

            int totalGap = CardGap * (CardCount - 1);
            int cardW    = (inner.Width - totalGap) / CardCount;
            int x = inner.Left;
            int y = inner.Top;

            for (int i = 0; i < CardCount; i++)
            {
                var rect = new Rectangle(x, y, cardW, CardHeight);
                DrawCard(g, rect, _cards[i]);
                x += cardW + CardGap;
            }
        }

        private static void DrawCard(Graphics g, Rectangle rect, DashCard card)
        {
            // Surface với rounded corners + subtle shadow
            using (var path = RoundedRect(rect, Radius.Lg))
            {
                using var shadowBrush = new SolidBrush(Color.FromArgb(22, 0, 0, 0));
                var shadow = new Rectangle(rect.X, rect.Y + 2, rect.Width, rect.Height);
                using var shadowPath = RoundedRect(shadow, Radius.Lg);
                g.FillPath(shadowBrush, shadowPath);

                using var bg = new SolidBrush(ColorPalette.Surface);
                g.FillPath(bg, path);

                using var border = new Pen(ColorPalette.BorderLight, 1);
                g.DrawPath(border, path);
            }

            // Top accent bar (4px) — màu theo card
            var accent = new Rectangle(rect.X, rect.Y, rect.Width, 4);
            using (var path = RoundedRectTop(accent, Radius.Lg))
            using (var brush = new SolidBrush(card.Accent))
                g.FillPath(brush, path);

            int padX = Spacing.Md;
            int padY = Spacing.Md + 4;

            // Label (caption)
            using var labelBrush = new SolidBrush(ColorPalette.TextTertiary);
            g.DrawString(card.Label.ToUpperInvariant(), _labelFont, labelBrush,
                rect.X + padX, rect.Y + padY);

            // Value (big number, monospace)
            int val = SafeGet(card);
            string valueStr = card.IsPercent ? $"{val}%" : val.ToString("N0");

            using var valueBrush = new SolidBrush(card.Accent);
            g.DrawString(valueStr, _valueFont, valueBrush,
                rect.X + padX, rect.Y + padY + 18);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static GraphicsPath RoundedRectTop(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddLine(r.Right, r.Bottom, r.X, r.Bottom);
            path.CloseFigure();
            return path;
        }

        private static Font TryMonoFont(float size)
        {
            try { return new Font("Cascadia Mono", size, FontStyle.Bold); }
            catch { return new Font("Consolas", size, FontStyle.Bold); }
        }


        // ── Data sources ──────────────────────────────────────
        private static int CountRunningDevices()
        {
            var list = DeviceServices.DeviceModels;
            if (list == null) return 0;
            int n = 0;
            foreach (var d in list) if (d != null && d.IsAdbOnline) n++;
            return n;
        }

        private static int CountTotalAccounts()
        {
            try
            {
                var ctx = new AccountContext();
                int total = 0;
                foreach (var p in new[] {
                    Sunny.Subdy.Common.Models.PlatformModel.Facebook,
                    Sunny.Subdy.Common.Models.PlatformModel.Instagram,
                    Sunny.Subdy.Common.Models.PlatformModel.Threads })
                {
                    var all = ctx.GetAll(new List<string>(), p, true);
                    if (all != null) total += all.Count;
                }
                return total;
            }
            catch { return 0; }
        }

        private static int CountQueue()
        {
            try
            {
                var ctx = new AccountContext();
                int n = 0;
                foreach (var p in new[] {
                    Sunny.Subdy.Common.Models.PlatformModel.Facebook,
                    Sunny.Subdy.Common.Models.PlatformModel.Instagram,
                    Sunny.Subdy.Common.Models.PlatformModel.Threads })
                {
                    var all = ctx.GetAll(new List<string>(), p, true);
                    if (all == null) continue;
                    foreach (var a in all) if (a != null && a.Running) n++;
                }
                return n;
            }
            catch { return 0; }
        }

        private static int CountAlerts()
        {
            try
            {
                var ctx = new AccountContext();
                int n = 0;
                foreach (var p in new[] {
                    Sunny.Subdy.Common.Models.PlatformModel.Facebook,
                    Sunny.Subdy.Common.Models.PlatformModel.Instagram,
                    Sunny.Subdy.Common.Models.PlatformModel.Threads })
                {
                    var all = ctx.GetAll(new List<string>(), p, true);
                    if (all == null) continue;
                    foreach (var a in all)
                    {
                        var s = a?.Status;
                        if (!string.IsNullOrEmpty(s) &&
                            (s.IndexOf("err", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             s.IndexOf("fail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             s.IndexOf("ban", StringComparison.OrdinalIgnoreCase) >= 0))
                            n++;
                    }
                }
                return n;
            }
            catch { return 0; }
        }

        private static int SuccessRatePercent()
        {
            int total = CountTotalAccounts();
            int alerts = CountAlerts();
            if (total <= 0) return 0;
            return (int)Math.Round((total - alerts) * 100.0 / total);
        }

        private static int CountTodayJobs()
        {
            // Placeholder: chưa có job history table; dùng running count làm proxy.
            // TODO: tích hợp ucHistoriesJob data source khi available.
            return CountQueue();
        }

        private static int SafeGet(DashCard c)
        {
            try { return c.Getter(); }
            catch { return 0; }
        }

        private sealed class DashCard
        {
            public string Label { get; }
            public Color Accent { get; }
            public string IconSvg { get; }
            public Func<int> Getter { get; }
            public bool IsPercent { get; }

            public DashCard(string label, Color accent, string iconSvg, Func<int> getter, bool isPercent = false)
            {
                Label = label; Accent = accent; IconSvg = iconSvg; Getter = getter; IsPercent = isPercent;
            }
        }
    }
}
