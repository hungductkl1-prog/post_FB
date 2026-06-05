using AntdUI;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    public partial class fRegister : AntdUI.Window
    {
        private const string QNRegisterUrl = "https://app.golike.net/register";

        public fRegister()
        {
            InitializeComponent();
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            // Đăng ký được chuyển sang QN: mở trình duyệt tới trang đăng ký chính thức.
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = QNRegisterUrl,
                    UseShellExecute = true
                });
                AntdUI.Notification.success(this, "QN Thông Báo",
                    "Đã mở trang đăng ký QN trên trình duyệt. Sau khi có tài khoản, hãy quay lại đăng nhập.",
                    TAlignFrom.TR, Font);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                AntdUI.Notification.error(this, "QN Thông Báo",
                    "Không thể mở trang đăng ký QN: " + ex.Message, TAlignFrom.TR, Font);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
