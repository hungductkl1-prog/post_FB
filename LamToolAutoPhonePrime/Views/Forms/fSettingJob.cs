using AntdUI;
using LamToolAutoPhonePrime.Utils;
using Sunny.Subdy.Common.API;
using Sunny.Subdy.Common.API.Captchas;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Models;
using System.Threading.Tasks;

namespace LamToolAutoPhonePrime.Views.Forms
{
    public partial class fSettingJob : AntdUI.Window
    {
        private string _platform = "";
        private string _server = "";
        public fSettingJob(string platform, string server)
        {
            InitializeComponent();
            switch (platform)
            {
                case PlatformModel.TikTok:
                case PlatformModel.Instagram:
                    {
                        panel4.Visible = checkBox8.Visible = false;
                        panel9.Visible = checkBox2.Visible = false;
                        panel11.Visible = checkBox3.Visible = false;
                        panel10.Visible = checkBox4.Visible = false;
                        panel5.Visible = checkBox5.Visible = false;
                        panel17.Visible = checkBox15.Visible = false;
                        panel18.Visible = checkBox18.Visible = false;
                        panel7.Visible = checkBox17.Visible = false;
                        panel19.Visible = checkBox7.Visible = false;
                        break;
                    }
            }
            _platform = platform;
            check_AddAccount.Visible = true;
            if (server != JobServices.VipIG)
            {
                check_AddAccount.Visible = false;
            }
            _server = server;
            new Sunny.Subdy.Common.Json.ConfigHelper(this, $"{nameof(fSettingJob)}_{_platform}_{_server}", onLoad: new System.Action(() =>
            {
                btn_Click(null, null);

            }), shouldExit: false);
            tabs3.SelectedIndex = 0;
            tabPageSettingCaptcha.Visible = server != JobServices.GoLike;
            cbb_ListTypeProxy.Items.AddRange(CaptchaService.SitesV2.ToArray());
            if (string.IsNullOrEmpty(cbb_ListTypeProxy.Text))
            {
                cbb_ListTypeProxy.SelectedIndex = 0;
            }
            FontUtil.ApplyFontToAllControls(this);
        }
        private void btn_Click(object sender, EventArgs e)
        {
            panel16.Enabled = checkBox14.Checked;
            panel15.Enabled = checkBox13.Checked;
            panel14.Enabled = checkBox12.Checked;
            panel13.Enabled = checkBox11.Checked;
            panel12.Enabled = checkBox10.Checked;
            panel8.Enabled = checkBox9.Checked;
            panel20.Enabled = checkBox20.Checked;
            panel3.Enabled = checkBox1.Checked;
            panel4.Enabled = checkBox8.Checked;
            panel9.Enabled = checkBox2.Checked;
            panel11.Enabled = checkBox3.Checked;
            panel10.Enabled = checkBox4.Checked;
            panel5.Enabled = checkBox5.Checked;
            panel17.Enabled = checkBox15.Checked;
            panel6.Enabled = checkBox6.Checked;
            panel18.Enabled = checkBox18.Checked;
            panel7.Enabled = checkBox17.Checked;
            panel19.Enabled = checkBox7.Checked;
            groupBox1.Enabled = checkBox19.Checked;
        }
        private void btn_setting_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            groupBox1.Text = $"({textBox1.Lines.Count()}) Token";
        }

        private void button9_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            button5.Enabled = false;
            CommonMethod.ShowMessageWarning(await CaptchaService.Getbalance(cbb_ListTypeProxy.Text, textBox2.Text.Trim()));
            button5.Enabled = true;
        }
    }
}
