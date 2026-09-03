using System.Drawing;
using System.Windows.Forms;
using Facebook_Farm_NewFeed_PostStory.Utils.Design;

namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    /// <summary>
    /// Loading overlay WinForms thuần thay cho AntdUI.Spin.
    /// Panel phủ full + Label text + ProgressBar Marquee.
    /// </summary>
    public static class LoadingOverlay
    {
        public static Panel Create(Control parent, string text)
        {
            var overlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(220, 255, 255, 255),
                Cursor = Cursors.WaitCursor
            };

            var lbl = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(FontScale.FamilyName, 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 70, 70)
            };

            var progress = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                Dock = DockStyle.Bottom,
                Height = 4,
                MarqueeAnimationSpeed = 30
            };

            overlay.Controls.Add(lbl);
            overlay.Controls.Add(progress);
            parent.Controls.Add(overlay);
            overlay.BringToFront();
            return overlay;
        }

        public static void Destroy(Control parent, Panel overlay)
        {
            if (overlay == null) return;
            if (parent != null && !parent.IsDisposed && parent.Controls.Contains(overlay))
                parent.Controls.Remove(overlay);
            overlay.Dispose();
        }
    }
}
