using AntdUI;
using LamToolAutoPhonePrime.Utils.Design;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LamToolAutoPhonePrime.Views.Controls
{
    /// <summary>
    /// Empty-state component — icon lớn + title + steps + primary CTA.
    /// Dùng overlay trên grid khi chưa có data (theo doc UX: tránh "Chưa có tài khoản nào" cụt).
    ///
    /// Usage:
    ///   var es = new EmptyStateView {
    ///       IconSvg = "InboxOutlined",
    ///       Title   = "Chưa có tài khoản nào",
    ///       Subtitle = "Bắt đầu farm tự động trong 4 bước:",
    ///       Steps = new[] { "Kết nối thiết bị", "Thêm tài khoản", "Chọn kịch bản", "Bắt đầu" },
    ///       CtaText = "Thêm tài khoản",
    ///   };
    ///   es.CtaClicked += (_, __) => OpenAddAccount();
    /// </summary>
    public class EmptyStateView : UserControl
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string IconSvg { get; set; } = "InboxOutlined";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title { get; set; } = "Chưa có dữ liệu";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Subtitle { get; set; } = "";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string[] Steps { get; set; } = Array.Empty<string>();
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CtaText { get; set; } = "";

        public event EventHandler? CtaClicked;

        private AntdUI.Button? _btnCta;

        public EmptyStateView()
        {
            DoubleBuffered = true;
            BackColor      = Color.Transparent;
            Dock           = DockStyle.Fill;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            BuildCtaButton();
            LayoutCta();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutCta();
        }

        private void BuildCtaButton()
        {
            if (_btnCta != null || string.IsNullOrEmpty(CtaText)) return;
            _btnCta = new AntdUI.Button { Text = CtaText, AutoSizeMode = TAutoSize.Auto };
            ButtonStyle.ApplyPrimary(_btnCta);
            _btnCta.IconSvg = "PlusOutlined";
            _btnCta.Click += (s, e) => CtaClicked?.Invoke(this, EventArgs.Empty);
            Controls.Add(_btnCta);
        }

        private void LayoutCta()
        {
            if (_btnCta == null) return;
            // Center horizontally, place below steps block
            _btnCta.Size = new Size(180, ButtonStyle.HeightPrimary);
            int cx = (ClientSize.Width - _btnCta.Width) / 2;
            int cy = ClientSize.Height / 2 + 90; // dưới text block
            _btnCta.Location = new Point(cx, cy);
            _btnCta.BringToFront();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode    = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = ClientRectangle;
            int cx = rect.Width / 2;
            int cy = rect.Height / 2;

            // Icon disc
            const int discSize = 80;
            var discRect = new RectangleF(cx - discSize / 2f, cy - 110, discSize, discSize);
            using (var brush = new SolidBrush(ColorPalette.BorderLight))
                g.FillEllipse(brush, discRect);
            using (var pen = new Pen(ColorPalette.TextDisabled, 3f))
            {
                // Inbox-ish glyph: U shape
                float left  = discRect.Left + 22;
                float right = discRect.Right - 22;
                float top   = discRect.Top + 26;
                float bot   = discRect.Bottom - 24;
                g.DrawLine(pen, left, top, left, bot - 10);
                g.DrawLine(pen, right, top, right, bot - 10);
                g.DrawLine(pen, left, bot - 10, right, bot - 10);
                // tray line
                g.DrawLine(pen, left + 6, top + 10, right - 6, top + 10);
            }

            // Title
            using var titleFont = new Font(FontScale.FamilyName, 14F, FontStyle.Bold);
            using var titleBrush = new SolidBrush(ColorPalette.TextPrimary);
            var titleSize = g.MeasureString(Title, titleFont);
            g.DrawString(Title, titleFont, titleBrush,
                cx - titleSize.Width / 2, cy - 18);

            // Subtitle
            if (!string.IsNullOrEmpty(Subtitle))
            {
                using var subFont = FontScale.Body9;
                using var subBrush = new SolidBrush(ColorPalette.TextSecondary);
                var subSize = g.MeasureString(Subtitle, subFont);
                g.DrawString(Subtitle, subFont, subBrush,
                    cx - subSize.Width / 2, cy + 12);
            }

            // Steps (numbered)
            if (Steps != null && Steps.Length > 0)
            {
                using var stepFont = FontScale.Body9;
                using var numFont = new Font(FontScale.FamilyName, 9F, FontStyle.Bold);
                using var stepBrush = new SolidBrush(ColorPalette.TextSecondary);
                using var numBrush = new SolidBrush(ColorPalette.Primary);

                int stepY = cy + 40;
                for (int i = 0; i < Steps.Length; i++)
                {
                    string num = $"{i + 1}.";
                    string txt = Steps[i];
                    var fullStr = $"{num}  {txt}";
                    var size = g.MeasureString(fullStr, stepFont);
                    float startX = cx - size.Width / 2;

                    var numSize = g.MeasureString(num, numFont);
                    g.DrawString(num, numFont, numBrush, startX, stepY);
                    g.DrawString(txt, stepFont, stepBrush,
                        startX + numSize.Width + 6, stepY);
                    stepY += (int)(size.Height + 4);
                }
            }
        }
    }
}
