using System.Drawing;
using System.Windows.Forms;

namespace LamToolAutoPhonePrime.Utils
{
    /// <summary>
    /// Gắn icon QN vào mọi Form — cả form chính lẫn dialog được mở sau đó.
    /// Designer không set Form.Icon nên Windows hiển thị icon WinForms mặc định; helper này
    /// lấp chỗ đó bằng cách quét Application.OpenForms trong Idle loop.
    /// </summary>
    public static class AppIconHelper
    {
        private static Icon? _cached;
        private static bool _installed;

        public static Icon AppIcon
        {
            get
            {
                if (_cached != null) return _cached;
                // Bitmap đã được swap nội dung sang logo QN (Resources/logo_lamtool_v3_dark_16.png).
                var bmp = Properties.Resources.logo_lamtool_v3_dark_16;
                var hicon = bmp.GetHicon();
                _cached = Icon.FromHandle(hicon);
                return _cached;
            }
        }

        /// <summary>Đăng ký hook Idle để áp icon lên mọi form đang mở (idempotent).</summary>
        public static void Install()
        {
            if (_installed) return;
            _installed = true;
            Application.Idle += OnIdle;
        }

        private static void OnIdle(object? sender, System.EventArgs e)
        {
            try
            {
                var icon = AppIcon;
                for (int i = 0; i < Application.OpenForms.Count; i++)
                {
                    var f = Application.OpenForms[i];
                    if (f == null || f.IsDisposed) continue;
                    if (!ReferenceEquals(f.Icon, icon))
                    {
                        // Chỉ set khi khác để tránh flicker.
                        f.Icon = icon;
                        f.ShowIcon = true;
                    }
                }
            }
            catch { /* silent */ }
        }
    }
}
