using AntdUI;
using LamToolAutoPhonePrime.Utils;
using Sunny.Subd.Core.Utils;
using Sunny.Subdy.Common.Helper;

namespace LamToolAutoPhonePrime.Views.Forms
{
    public partial class fInputTokenQN : AntdUI.Window
    {
        public string Token { get; private set; } = "";

        public fInputTokenQN()
        {
            InitializeComponent();
            windowBar.Text = "Token QN (Làm Job QN)";
            label1.Text = "Token:";
            textBox1.Text = FarmXuVipHelper.GetToken();
            FontUtil.ApplyFontToAllControls(this);
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            string token = (textBox1.Text ?? "").Trim();
            if (string.IsNullOrEmpty(token))
            {
                AntdHelper.NotifyWarn(this, "Cảnh báo", "Token không được bỏ trống!");
                return;
            }
            FarmXuVipHelper.SaveToken(token);
            Token = token;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btn_setting_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
