using AntdUI;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Sunny.Subdy.Common.API;
using Sunny.Subdy.Common.API.Captchas;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Models;
using System.Threading.Tasks;

using CommonMethod = Sunny.Subdy.Common.ControlMethod.CommonMethod;
namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    public partial class fSettingJob : Facebook_Farm_NewFeed_PostStory.Utils.BaseForm
    {
        private string _platform = "";
        private string _server = "";
        private Sunny.Subdy.Common.Json.ConfigHelper _configHelper;
        public fSettingJob(string platform, string server)
        {
            InitializeComponent();
            _platform = platform;
            _server = server;
            _configHelper = new Sunny.Subdy.Common.Json.ConfigHelper(this, $"{nameof(fSettingJob)}_{_platform}", onLoad: new System.Action(() =>
            {
                btn_Click(null, null);

            }), shouldExit: false);
            tabs3.SelectedIndex = 0;
            cbb_ListTypeProxy.Items.AddRange(CaptchaService.SitesV2.ToArray());
            if (string.IsNullOrEmpty(cbb_ListTypeProxy.Text))
            {
                cbb_ListTypeProxy.SelectedIndex = 0;
            }
            FontUtil.ApplyFontToAllControls(this); Facebook_Farm_NewFeed_PostStory.Utils.Design.VietnameseFont.Enforce(this);

            this.Load += (_, __) => Facebook_Farm_NewFeed_PostStory.Utils.Design.SsaTheme.ApplyFSettingJob(this);
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
        }
        private void btn_setting_Click(object sender, EventArgs e)
        {
            this.Close();
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
