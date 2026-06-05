using AntdUI;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using Sunny.Subdy.UI.View.Pages;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    public partial class fAddUsercontrol : AntdUI.Window
    {
        private FolderContext _folderContext;
        private string _type = "";
        private string _platform = "";
        ucManagerDevices _control;
        public fAddUsercontrol(string type, string platform, ucManagerDevices control)
        {
            InitializeComponent();
            _type = type;
            _platform = platform;
            switch (_type)
            {
                case "SelectDevices":
                    {
                        windowBar.Text = $"Chọn thiết bị cần chạy {_platform}";
                        break;
                    }
            }
            _control = control;
            _control.Dock = DockStyle.Fill;
            panel1.Controls.Add(_control);

            // Detach _control khỏi panel1 TRƯỚC khi dialog dispose, để Form.Dispose()
            // không dispose lây sang ucManagerDevices (singleton dùng chung với fMain).
            // Nếu không detach: caller dùng `using` hoặc GC chạy sau Close() sẽ làm
            // toàn bộ control bị dispose → ObjectDisposedException ở lần dùng kế tiếp.
            FormClosing += (_, __) =>
            {
                if (panel1.Controls.Contains(_control))
                    panel1.Controls.Remove(_control);
            };

            FontUtil.ApplyFontToAllControls(this);

            // Force button2 ("Bắt đầu") hiển thị khi dialog show — tránh trường hợp
            // lần 2 vào form, button2 vẫn ở trạng thái Visible=false hoặc Location bị
            // đẩy ra ngoài bounds của panel6 từ lần trước (do Anchor Right không
            // re-apply khi control re-parent giữa các container có width khác).
            if (_type == "SelectDevices")
            {
                _control.button2.Visible = true;
                _control.button2.Enabled = true;
                _control.button2.BringToFront();
                ReanchorStartButton();
                Shown += (_, __) =>
                {
                    _control.button2.Visible = true;
                    _control.button2.Enabled = true;
                    _control.button2.BringToFront();
                    ReanchorStartButton();
                };
            }
        }

        // Đặt lại Location của nút "Bắt đầu" (button2) về sát mép phải panel chứa nó —
        // cần vì Anchor = Top|Right chỉ reflow khi parent RESIZE, không phải khi
        // control được REPARENT sang container có width khác (từ pContent → dialog).
        private void ReanchorStartButton()
        {
            var btn = _control.button2;
            var host = btn.Parent;
            if (host == null) return;
            // Giữ nguyên khoảng cách mép phải như designer: 1082 - 914 - 137 = 31px
            const int rightMargin = 31;
            int x = Math.Max(0, host.ClientSize.Width - btn.Width - rightMargin);
            btn.Location = new Point(x, btn.Location.Y);
        }

        private void btn_setting_Click(object sender, EventArgs e)
        {
            _control.button2.Visible = false;
            this.Close();
        }

    }
}
