using Facebook_Farm_NewFeed_PostStory.Utils;
using Facebook_Farm_NewFeed_PostStory.Utils.Design;
using System.Drawing.Drawing2D;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    /// <summary>
    /// Splash hiển thị trong Program.Main trong lúc kiểm tra môi trường (ADB, Node, license...).
    /// Tự đóng khi Close() được gọi. Chạy trong UI thread riêng để không bị block bởi sync IO.
    /// </summary>
    public class fSplashLoading : System.Windows.Forms.Form
    {
        private readonly System.Windows.Forms.Label _lblStatus;
        private readonly System.Windows.Forms.Timer _spinTimer;
        private float _angle;
        private readonly System.Windows.Forms.Panel _spinner;

        public fSplashLoading()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(380, 220);
            BackColor = Color.White;
            ShowInTaskbar = true;
            TopMost = true;
            DoubleBuffered = true;
            Text = "QNAutoPhone";

            try { Icon = AppIconHelper.AppIcon; } catch { }

            var titleLogo = new System.Windows.Forms.PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(64, 64),
                Location = new Point((Width - 64) / 2, 28),
                Image = Properties.Resources.logo_lamtool_v3_dark_16
            };
            Controls.Add(titleLogo);

            var lblTitle = new System.Windows.Forms.Label
            {
                AutoSize = false,
                Text = "QNAutoPhone",
                Font = new Font(FontScale.FamilyName, 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 100),
                Size = new Size(Width, 24)
            };
            Controls.Add(lblTitle);

            _spinner = new System.Windows.Forms.Panel
            {
                Size = new Size(28, 28),
                Location = new Point((Width / 2) - 80, 144),
                BackColor = Color.Transparent
            };
            _spinner.Paint += SpinnerPaint;
            Controls.Add(_spinner);

            _lblStatus = new System.Windows.Forms.Label
            {
                AutoSize = false,
                Text = "Đang kiểm tra môi trường...",
                Font = new Font(FontScale.FamilyName, 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(90, 90, 90),
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point((Width / 2) - 40, 144),
                Size = new Size(220, 28)
            };
            Controls.Add(_lblStatus);

            // Border mềm quanh window
            Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(220, 220, 220), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            };

            _spinTimer = new System.Windows.Forms.Timer { Interval = 30 };
            _spinTimer.Tick += (s, e) =>
            {
                _angle = (_angle + 12) % 360;
                if (!IsDisposed && _spinner.IsHandleCreated) _spinner.Invalidate();
            };
            _spinTimer.Start();
        }

        private void SpinnerPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(2, 2, _spinner.Width - 4, _spinner.Height - 4);
            using var penBg = new Pen(Color.FromArgb(230, 230, 230), 3);
            g.DrawEllipse(penBg, rect);
            using var pen = new Pen(Color.FromArgb(64, 158, 255), 3) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawArc(pen, rect, _angle, 90);
        }

        public void SetStatus(string text)
        {
            if (IsDisposed) return;
            try
            {
                if (_lblStatus.InvokeRequired)
                    _lblStatus.BeginInvoke(new Action(() => _lblStatus.Text = text));
                else
                    _lblStatus.Text = text;
            }
            catch { }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try { _spinTimer.Stop(); } catch { }
            try { _spinTimer.Dispose(); } catch { }
            base.OnFormClosed(e);
        }

        // Bỏ activate flicker khi splash hiện
        protected override bool ShowWithoutActivation => false;
    }

    /// <summary>
    /// Chạy splash trong UI thread riêng (background STA) để Program.Main vẫn block
    /// việc kiểm tra môi trường (sync IO) mà splash vẫn animate mượt.
    /// </summary>
    public sealed class SplashHandle : IDisposable
    {
        private readonly Thread _thread;
        private fSplashLoading? _form;
        private readonly ManualResetEventSlim _ready = new(false);

        public SplashHandle()
        {
            _thread = new Thread(() =>
            {
                _form = new fSplashLoading();
                _form.Shown += (_, __) => _ready.Set();
                Application.Run(_form);
            });
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.IsBackground = true;
            _thread.Start();
            _ready.Wait(3000);
        }

        public void SetStatus(string text) => _form?.SetStatus(text);

        public void Close()
        {
            try
            {
                _form?.BeginInvoke(new Action(() =>
                {
                    try { _form?.Close(); } catch { }
                }));
            }
            catch { }
            try { _thread.Join(3000); } catch { }
        }

        public void Dispose() => Close();
    }
}
