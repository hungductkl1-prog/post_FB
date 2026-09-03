using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Facebook_Farm_NewFeed_PostStory.Utils.Design;

namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    /// <summary>
    /// Thanh tiêu đề WinForms thuần thay cho AntdUI.PageHeader (windowBar) trên các form
    /// borderless (FormBorderStyle.None). Vẽ icon + tiêu đề, cho kéo di chuyển form, và có
    /// sẵn nút đóng bên phải. Giữ các property Text/Icon để designer tương thích.
    /// </summary>
    public class TitleBarPanel : Panel
    {
        private Image _icon;
        private string _subText = "";
        private bool _loading;
        private readonly Button _btnClose;
        private readonly Button _btnBack;

        /// <summary>Phát khi bấm nút back (thay PageHeader.Back).</summary>
        public event EventHandler BackClick;

        public TitleBarPanel()
        {
            Height = 35;
            BackColor = Color.White;
            ForeColor = Color.Black;
            Font = FontScale.Body9Bold;
            DoubleBuffered = true;

            _btnClose = new Button
            {
                Dock = DockStyle.Right,
                Width = 46,
                FlatStyle = FlatStyle.Flat,
                Text = "✕",
                Font = new Font(FontScale.FamilyName, 11F, FontStyle.Regular),
                ForeColor = Color.FromArgb(90, 90, 90),
                TabStop = false,
                Cursor = Cursors.Hand,
                Visible = false // mặc định ẩn: phần lớn form đã có nút đóng riêng (btn_setting)
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
            _btnClose.Click += (_, __) => FindForm()?.Close();
            Controls.Add(_btnClose);

            _btnBack = new Button
            {
                Dock = DockStyle.Left,
                Width = 36,
                FlatStyle = FlatStyle.Flat,
                Text = "←",
                Font = new Font(FontScale.FamilyName, 12F, FontStyle.Regular),
                ForeColor = Color.FromArgb(90, 90, 90),
                TabStop = false,
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnBack.FlatAppearance.BorderSize = 0;
            _btnBack.Click += (_, __) => BackClick?.Invoke(this, EventArgs.Empty);
            Controls.Add(_btnBack);

            MouseDown += TitleBar_MouseDown;
        }

        /// <summary>Hiện/ẩn nút back bên trái (thay PageHeader.ShowBack).</summary>
        public bool ShowBack
        {
            get => _btnBack.Visible;
            set => _btnBack.Visible = value;
        }

        /// <summary>Trạng thái loading — đổi con trỏ chờ (thay PageHeader.Loading).</summary>
        public bool Loading
        {
            get => _loading;
            set { _loading = value; Cursor = value ? Cursors.WaitCursor : Cursors.Default; }
        }

        /// <summary>Icon hiển thị bên trái tiêu đề.</summary>
        public Image Icon
        {
            get => _icon;
            set { _icon = value; Invalidate(); }
        }

        /// <summary>Phụ đề nhỏ bên phải tiêu đề (vd version). Thay PageHeader.SubText.</summary>
        public string SubText
        {
            get => _subText;
            set { _subText = value ?? ""; Invalidate(); }
        }

        /// <summary>Ẩn/hiện nút đóng mặc định (một số form tự wire nút riêng).</summary>
        public bool ShowCloseButton
        {
            get => _btnClose.Visible;
            set => _btnClose.Visible = value;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            int x = 8;
            int iconSize = Math.Min(20, Height - 10);
            if (_icon != null)
            {
                e.Graphics.DrawImage(_icon, new Rectangle(x, (Height - iconSize) / 2, iconSize, iconSize));
                x += iconSize + 8;
            }

            if (!string.IsNullOrEmpty(Text))
            {
                Size titleSize = TextRenderer.MeasureText(Text, Font);
                TextRenderer.DrawText(
                    e.Graphics, Text, Font,
                    new Rectangle(x, 0, Width - x - _btnClose.Width, Height),
                    ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                x += titleSize.Width + 8;
            }

            if (!string.IsNullOrEmpty(_subText))
            {
                using var subFont = new Font(Font.FontFamily, Font.Size - 1.5f, FontStyle.Regular);
                TextRenderer.DrawText(
                    e.Graphics, _subText, subFont,
                    new Rectangle(x, 0, Width - x - _btnClose.Width, Height),
                    Color.FromArgb(140, 140, 140),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        // ── Drag-to-move form ────────────────────────────────────────────────────
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var form = FindForm();
            if (form == null) return;
            ReleaseCapture();
            SendMessage(form.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }
    }
}
