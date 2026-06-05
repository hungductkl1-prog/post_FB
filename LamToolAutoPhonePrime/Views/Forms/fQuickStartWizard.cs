using AntdUI;
using LamToolAutoPhonePrime.Utils.Design;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace LamToolAutoPhonePrime.Views.Forms
{
    /// <summary>
    /// Quick Start Wizard — 4 step onboarding cho user mới.
    /// Hiển thị lần đầu app start (lưu flag trong %LocalAppData%).
    /// Có thể trigger lại từ menu Help → Quick Start.
    /// </summary>
    public class fQuickStartWizard : AntdUI.Window
    {
        private static readonly string _flagFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LamToolAutoPhonePrime", "quick_start_done.txt");

        private static readonly (string Title, string Body, string IconSvg)[] _steps = new[]
        {
            ("Kết nối thiết bị Android",
             "Cắm điện thoại Android qua USB (đã bật USB debugging) hoặc khởi động giả lập như LDPlayer.\n" +
             "App sẽ tự động phát hiện thiết bị qua ADB. Bạn có thể quản lý nhiều thiết bị song song.",
             "MobileOutlined"),

            ("Thêm tài khoản",
             "Vào tab Facebook / Instagram / Threads, nhấn \"Thêm tài khoản\".\n" +
             "Hỗ trợ import từ file txt (định dạng uid|password|token|...).\n" +
             "Sau đó gán tài khoản vào nhóm để dễ quản lý.",
             "UserAddOutlined"),

            ("Chọn kịch bản farm",
             "Trên toolbar, chọn dropdown \"Kịch bản\" để chọn loại job:\n" +
             "  • Làm Job QN — farm tự động theo job QN\n" +
             "  • Chạy theo kịch bản — config riêng cho từng tài khoản\n" +
             "Bạn cũng có thể set kịch bản riêng cho từng nhóm.",
             "RocketOutlined"),

            ("Bắt đầu farm",
             "Nhấn nút \"Chạy\" (xanh) trên toolbar để khởi động bot.\n" +
             "Tab Dashboard hiển thị KPI realtime: jobs hoàn thành, devices online, alerts.\n" +
             "Live Log ở đáy app hiển thị log chi tiết khi mở rộng.",
             "PlayCircleOutlined"),
        };

        private int _currentStep = 0;
        private System.Windows.Forms.Label _lblTitle = null!;
        private System.Windows.Forms.Label _lblBody = null!;
        private System.Windows.Forms.Label _lblStepNum = null!;
        private AntdUI.Button _btnBack = null!;
        private AntdUI.Button _btnNext = null!;
        private AntdUI.Button _btnSkip = null!;
        private System.Windows.Forms.Panel _stepperPanel = null!;

        public fQuickStartWizard()
        {
            BuildUi();
            RenderStep();
        }

        private void BuildUi()
        {
            Text             = "Quick Start";
            Size             = new Size(640, 520);
            StartPosition    = FormStartPosition.CenterParent;
            FormBorderStyle  = FormBorderStyle.FixedDialog;
            MaximizeBox      = false;
            MinimizeBox      = false;
            ShowIcon         = false;
            BackColor        = ColorPalette.Surface;
            Font             = FontScale.Body9;

            // ── Body (Dock=Fill) — ADD ĐẦU TIÊN để Fill chừa đúng không gian sau khi Top/Bottom dock
            // (WinForms dock layout: Fill = client area còn lại SAU khi các dock khác đã chiếm chỗ).
            var body = new System.Windows.Forms.Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = ColorPalette.Surface,
                Padding   = new Padding(Spacing.Xl, Spacing.Lg, Spacing.Xl, Spacing.Lg),
            };

            _lblStepNum = new System.Windows.Forms.Label
            {
                AutoSize  = true,
                Font      = new Font(FontScale.FamilyName, 9F, FontStyle.Bold),
                ForeColor = ColorPalette.Primary,
                BackColor = Color.Transparent,
                Location  = new Point(Spacing.Xl, Spacing.Md),
            };

            _lblTitle = new System.Windows.Forms.Label
            {
                AutoSize  = true,
                Font      = new Font(FontScale.FamilyName, 16F, FontStyle.Bold),
                ForeColor = ColorPalette.TextPrimary,
                BackColor = Color.Transparent,
                Location  = new Point(Spacing.Xl, Spacing.Md + 22),
            };

            _lblBody = new System.Windows.Forms.Label
            {
                AutoSize  = false,
                Size      = new Size(560, 220),
                Font      = new Font(FontScale.FamilyName, 10F, FontStyle.Regular),
                ForeColor = ColorPalette.TextSecondary,
                BackColor = Color.Transparent,
                Location  = new Point(Spacing.Xl, Spacing.Md + 56),
            };

            body.Controls.Add(_lblStepNum);
            body.Controls.Add(_lblTitle);
            body.Controls.Add(_lblBody);
            Controls.Add(body);

            // ── Footer (Dock=Bottom)
            var footer = new System.Windows.Forms.Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 64,
                BackColor = ColorPalette.SurfaceAlt,
                Padding   = new Padding(Spacing.Xl, Spacing.Md, Spacing.Xl, Spacing.Md),
            };

            _btnSkip = new AntdUI.Button
            {
                Text     = "Bỏ qua",
                Size     = new Size(96, 36),
                Location = new Point(Spacing.Xl, 14),
            };
            ButtonStyle.ApplyTertiary(_btnSkip);
            _btnSkip.Click += (_, __) => Close();

            _btnBack = new AntdUI.Button
            {
                Text     = "← Quay lại",
                Size     = new Size(110, 36),
                Anchor   = AnchorStyles.Top | AnchorStyles.Right,
            };
            ButtonStyle.ApplySecondary(_btnBack);
            _btnBack.Click += (_, __) =>
            {
                if (_currentStep > 0) { _currentStep--; RenderStep(); }
            };

            _btnNext = new AntdUI.Button
            {
                Text     = "Tiếp →",
                Size     = new Size(130, 36),
                Anchor   = AnchorStyles.Top | AnchorStyles.Right,
            };
            ButtonStyle.ApplyPrimary(_btnNext);
            _btnNext.Click += (_, __) =>
            {
                if (_currentStep < _steps.Length - 1) { _currentStep++; RenderStep(); }
                else { MarkDone(); Close(); }
            };

            footer.Controls.Add(_btnSkip);
            footer.Controls.Add(_btnBack);
            footer.Controls.Add(_btnNext);
            footer.Resize += (_, __) => LayoutFooter(footer);
            Controls.Add(footer);

            LayoutFooter(footer);

            // ── Stepper bar (Dock=Top)
            _stepperPanel = new System.Windows.Forms.Panel
            {
                Dock      = DockStyle.Top,
                Height    = 48,
                BackColor = ColorPalette.SurfaceAlt,
                Padding   = new Padding(Spacing.Xl, Spacing.Md, Spacing.Xl, 0),
            };
            _stepperPanel.Paint += StepperOnPaint;
            Controls.Add(_stepperPanel);

            // ── Header (Dock=Top) — ADD CUỐI cùng để Top stack đúng thứ tự: header trên, stepper dưới
            var header = new System.Windows.Forms.Panel
            {
                Dock      = DockStyle.Top,
                Height    = 64,
                BackColor = ColorPalette.Primary,
                Padding   = new Padding(Spacing.Xl, Spacing.Md, Spacing.Xl, 0),
            };
            var lblHeader = new System.Windows.Forms.Label
            {
                Text      = "Welcome to QNAutoPhone",
                ForeColor = Color.White,
                Font      = new Font(FontScale.FamilyName, 14F, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(Spacing.Xl, Spacing.Md + 4),
                BackColor = Color.Transparent,
            };
            var lblSub = new System.Windows.Forms.Label
            {
                Text      = "4 bước để bắt đầu farm tự động",
                ForeColor = Color.FromArgb(220, 255, 255, 255),
                Font      = FontScale.Body9,
                AutoSize  = true,
                Location  = new Point(Spacing.Xl, Spacing.Md + 30),
                BackColor = Color.Transparent,
            };
            header.Controls.Add(lblHeader);
            header.Controls.Add(lblSub);
            Controls.Add(header);
        }

        private void LayoutFooter(System.Windows.Forms.Panel footer)
        {
            int rx = footer.Width - Spacing.Xl;
            _btnNext.Location = new Point(rx - _btnNext.Width, 14);
            rx -= _btnNext.Width + Spacing.Sm;
            _btnBack.Location = new Point(rx - _btnBack.Width, 14);
        }

        private void RenderStep()
        {
            var step = _steps[_currentStep];
            _lblStepNum.Text = $"BƯỚC {_currentStep + 1} / {_steps.Length}";
            _lblTitle.Text   = step.Title;
            _lblBody.Text    = step.Body;
            _btnBack.Enabled = _currentStep > 0;
            _btnNext.Text    = _currentStep == _steps.Length - 1 ? "Hoàn tất ✓" : "Tiếp →";
            _stepperPanel.Invalidate();
        }

        private void StepperOnPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int n     = _steps.Length;
            int padL  = _stepperPanel.Padding.Left;
            int padR  = _stepperPanel.Padding.Right;
            int avail = _stepperPanel.Width - padL - padR;
            int seg   = avail / (n - 1);
            int y     = 20;
            int r     = 12;

            for (int i = 0; i < n; i++)
            {
                int cx = padL + seg * i;
                if (i > 0)
                {
                    using var linePen = new Pen(
                        i <= _currentStep ? ColorPalette.Primary : ColorPalette.Border, 2);
                    g.DrawLine(linePen, padL + seg * (i - 1) + r, y, cx - r, y);
                }

                Color dotColor =
                    i < _currentStep ? ColorPalette.Primary :
                    i == _currentStep ? ColorPalette.Primary :
                    ColorPalette.Border;
                Color textColor = i <= _currentStep ? Color.White : ColorPalette.TextTertiary;

                using (var brush = new SolidBrush(dotColor))
                    g.FillEllipse(brush, cx - r, y - r, r * 2, r * 2);
                using (var font = new Font(FontScale.FamilyName, 8.5F, FontStyle.Bold))
                using (var b    = new SolidBrush(textColor))
                {
                    string num = (i + 1).ToString();
                    var sz = g.MeasureString(num, font);
                    g.DrawString(num, font, b, cx - sz.Width / 2, y - sz.Height / 2);
                }
            }
        }

        // ── Static helpers ────────────────────────────────────
        public static bool HasBeenShown()
        {
            try { return File.Exists(_flagFile); }
            catch { return false; }
        }

        public static void MarkDone()
        {
            try
            {
                var dir = Path.GetDirectoryName(_flagFile);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_flagFile, DateTime.UtcNow.ToString("o"));
            }
            catch { /* preference not critical */ }
        }

        public static void ResetFlag()
        {
            try { if (File.Exists(_flagFile)) File.Delete(_flagFile); }
            catch { }
        }

        /// <summary>Show wizard if first-run (call from fMain after Load).</summary>
        public static void ShowIfFirstRun(Form parent)
        {
            if (HasBeenShown()) return;
            try
            {
                var wiz = new fQuickStartWizard();
                wiz.ShowDialog(parent);
            }
            catch { /* never crash on UX */ }
        }
    }
}
