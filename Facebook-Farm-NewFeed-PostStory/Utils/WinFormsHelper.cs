using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using Facebook_Farm_NewFeed_PostStory.Utils.Design;

namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    /// <summary>
    /// Thay các API tĩnh của AntdUI (Message / Notification / Modal / Spin) bằng WinForms thuần.
    /// Giữ signature gần với AntdHelper cũ để caller đổi tối thiểu.
    /// </summary>
    public static class WinFormsHelper
    {
        public enum ToastType { Success, Warn, Error, Info }

        // ── Message (toast góc trên, không chặn UI) ─────────────────────────────
        public static void MsgSuccess(Form form, string text) => ShowToast(form, text, ToastType.Success);
        public static void MsgWarn(Form form, string text) => ShowToast(form, text, ToastType.Warn);
        public static void MsgError(Form form, string text) => ShowToast(form, text, ToastType.Error);
        public static void MsgInfo(Form form, string text) => ShowToast(form, text, ToastType.Info);

        // ── Notification (toast có tiêu đề + mô tả) ─────────────────────────────
        public static void NotifySuccess(Form form, string title, string desc) => ShowToast(form, Combine(title, desc), ToastType.Success);
        public static void NotifyError(Form form, string title, string desc) => ShowToast(form, Combine(title, desc), ToastType.Error);
        public static void NotifyWarn(Form form, string title, string desc) => ShowToast(form, Combine(title, desc), ToastType.Warn);

        private static string Combine(string title, string desc) =>
            string.IsNullOrEmpty(desc) ? title : $"{title}\n{desc}";

        // ── Modal confirm ───────────────────────────────────────────────────────
        public static bool Confirm(Form form, string title, string content) =>
            MessageBox.Show(form, content, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK;

        // ── Spin (loading overlay trên một control) ─────────────────────────────
        public static async Task Spin(Control control, string text, Func<Task> action)
        {
            Panel overlay = null;
            try
            {
                if (control != null && !control.IsDisposed)
                    overlay = LoadingOverlay.Create(control, text);
                await action();
            }
            finally
            {
                if (overlay != null && control != null && !control.IsDisposed)
                    LoadingOverlay.Destroy(control, overlay);
            }
        }

        // ── Debounce (giữ tương thích với AntdHelper.Debounce) ──────────────────
        public static System.Windows.Forms.Timer Debounce(Action action, int ms = 300)
        {
            var t = new System.Windows.Forms.Timer { Interval = ms };
            t.Tick += (_, __) =>
            {
                t.Stop();
                t.Dispose();
                action();
            };
            return t;
        }

        // ── Toast implementation ─────────────────────────────────────────────────
        private static (Color back, Color fore) Palette(ToastType type) => type switch
        {
            ToastType.Success => (ColorPalette.Success, Color.White),
            ToastType.Warn => (ColorPalette.Warning, Color.White),
            ToastType.Error => (ColorPalette.Error, Color.White),
            _ => (ColorPalette.Info, Color.White),
        };

        private static System.Drawing.Region RoundedRegion(Size size, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(0, 0, d, d, 180, 90);
            path.AddArc(size.Width - d, 0, d, d, 270, 90);
            path.AddArc(size.Width - d, size.Height - d, d, d, 0, 90);
            path.AddArc(0, size.Height - d, d, d, 90, 90);
            path.CloseFigure();
            return new System.Drawing.Region(path);
        }

        private static void ShowToast(Form owner, string text, ToastType type, int autoCloseMs = 3000)
        {
            if (owner == null || owner.IsDisposed)
            {
                MessageBox.Show(text);
                return;
            }

            if (owner.InvokeRequired)
            {
                owner.BeginInvoke(new Action(() => ShowToast(owner, text, type, autoCloseMs)));
                return;
            }

            var (back, fore) = Palette(type);

            var toast = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                BackColor = back,
                Owner = owner,
                TopMost = true
            };

            var lbl = new Label
            {
                Text = text,
                AutoSize = false,
                Dock = DockStyle.Fill,
                ForeColor = fore,
                Font = FontScale.Body9Bold,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(16, 10, 16, 10)
            };
            toast.Controls.Add(lbl);

            using (var g = toast.CreateGraphics())
            {
                var sz = g.MeasureString(text, lbl.Font, 360);
                toast.ClientSize = new Size(
                    Math.Max(160, (int)sz.Width + 40),
                    Math.Max(44, (int)sz.Height + 24));
            }

            // Rounded corners via Region
            const int toastRadius = 8;
            toast.Region = RoundedRegion(toast.ClientSize, toastRadius);
            toast.Resize += (_, __) => toast.Region = RoundedRegion(toast.ClientSize, toastRadius);

            // Position: bottom-right of owner, 16px margin
            var screen = Screen.FromControl(owner).WorkingArea;
            toast.Location = new Point(
                screen.Right - toast.Width - 16,
                screen.Bottom - toast.Height - 60);

            var timer = new System.Windows.Forms.Timer { Interval = autoCloseMs };
            timer.Tick += (_, __) =>
            {
                timer.Stop();
                timer.Dispose();
                if (!toast.IsDisposed) toast.Close();
            };

            toast.Shown += (_, __) => timer.Start();
            toast.FormClosed += (_, __) => { if (!owner.IsDisposed) owner.Activate(); };

            try { toast.Show(owner); }
            catch { toast.Dispose(); MessageBox.Show(owner, text); }
        }
    }
}
