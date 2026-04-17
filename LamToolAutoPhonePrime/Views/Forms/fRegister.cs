using AntdUI;
using Sunny.Subdy.Common.API;
using System;
using System.Windows.Forms;

namespace LamToolAutoPhonePrime.Views.Forms
{
    public partial class fRegister : AntdUI.Window
    {
        public fRegister()
        {
            InitializeComponent();
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            string fullName = txtFullName.Text.Trim();
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text.Trim();
            string confirmPassword = txtConfirmPassword.Text.Trim();

            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                AntdUI.Notification.warn(this, "Subdy Thông Báo", "Vui lòng điền đầy đủ thông tin.", TAlignFrom.TR, Font);
                return;
            }

            if (password != confirmPassword)
            {
                AntdUI.Notification.warn(this, "Subdy Thông Báo", "Mật khẩu xác nhận không khớp.", TAlignFrom.TR, Font);
                return;
            }

            btnRegister.Enabled = false;
            try
            {
                SubdyClient.Register(username, email, password, fullName);
                AntdUI.Notification.success(this, "Subdy Thông Báo", "Đăng ký thành công! Vui lòng đăng nhập.", TAlignFrom.TR, Font);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                AntdUI.Notification.error(this, "Subdy Thông Báo", ex.Message, TAlignFrom.TR, Font);
                btnRegister.Enabled = true;
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
