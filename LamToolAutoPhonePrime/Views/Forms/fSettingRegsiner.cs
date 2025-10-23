using AntdUI;
using LamToolAutoPhonePrime.Utils;
using Sunny.Subd.Core.Facebook;
using Sunny.Subd.Core.Proxies;
using Sunny.Subd.Core.Telegram;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Common.Services;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace LamToolAutoPhonePrime.Views.Forms
{
    public partial class fSettingRegsiner : AntdUI.Window
    {
        private string _platform = "";
        public fSettingRegsiner(string platform)
        {
            InitializeComponent();
            _platform = platform;
            cbb_ListTypeProxy.Items.AddRange(ProxyService.ProxyTypes.ToArray());
            cbbScript.Items.AddRange(SubdyHelper.Countries.ToArray());
            comboBox1.Items.AddRange(RegistrationType.RegFacebook_AllTypes.ToArray());
            comboBox3.Items.Clear();
            comboBox3.Items.AddRange(RegistrationType.EmailTypes.ToArray());
            if (string.IsNullOrEmpty(comboBox1.Text))
            {
                comboBox1.SelectedIndex = 0;
            }
            cbb_Email.Items.Clear();
            cbb_Email.Items.AddRange(RegistrationType.EmailTypes.ToArray());
            if (string.IsNullOrEmpty(cbb_Email.Text))
            {
                cbb_Email.SelectedIndex = 0;
            }
            comboBox2.Items.Clear();
            comboBox2.Items.AddRange(RegistrationType.PhoneNumberTypes.ToArray());
            if (string.IsNullOrEmpty(comboBox2.Text))
            {
                comboBox2.SelectedIndex = 0;
            }
            if (string.IsNullOrEmpty(comboBox3.Text))
            {
                comboBox3.SelectedIndex = 0;
            }
            checkBox9_CheckedChanged(null, null);
            checkBox11.CheckedChanged += checkBox9_CheckedChanged;
            checkBox1.CheckedChanged += checkBox9_CheckedChanged;
            checkBox2.CheckedChanged += checkBox9_CheckedChanged;
            checkBox3.CheckedChanged += checkBox9_CheckedChanged;
            checkBox4.CheckedChanged += checkBox9_CheckedChanged;
            checkBox5.CheckedChanged += checkBox9_CheckedChanged;
            checkBox6.CheckedChanged += checkBox9_CheckedChanged;
            checkBox8.CheckedChanged += checkBox9_CheckedChanged;

            new Sunny.Subdy.Common.Json.ConfigHelper(this, $"{nameof(fSettingRegsiner)}_{_platform}", onLoad: new System.Action(() =>
            {
                LoadForm();
                cbb_ListTypeProxy_SelectedIndexChanged(null, null);
                txtLines_TextChanged(null, null);
                comboBox1_SelectedIndexChanged(null, null);

            }), shouldExit: false);
            tabs3.SelectedIndex = 0;
            FontUtil.ApplyFontToAllControls(this);
        }
        private void LoadForm()
        {
            if (string.IsNullOrEmpty(textBox1.Text.Trim()))
            {
                textBox1.Text = DeviceServices.Brands;
            }
            if (string.IsNullOrEmpty(textBox2.Text.Trim()))
            {
                textBox2.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup", "Device", _platform);
            }
            if (string.IsNullOrEmpty(textBox3.Text.Trim()))
            {
                textBox3.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup", "Profile", _platform);
            }
            if (string.IsNullOrEmpty(textBox4.Text.Trim()))
            {
                textBox4.Text = FacebookHander.FilePath(_platform);
            }
            nud_IndexFailProxy.Enabled = chk_CheckProxy.Checked;
            if (string.IsNullOrEmpty(cbb_ListTypeProxy.Text))
            {
                cbb_ListTypeProxy.SelectedIndex = 0;
            }
            if (string.IsNullOrEmpty(comboBox1.Text))
            {
                comboBox1.SelectedIndex = 0;
            }
            if (string.IsNullOrEmpty(cbbScript.Text))
            {
                cbbScript.SelectedIndex = 0;
            }
        }
        private void btn_setting_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            panel8.Enabled = checkBox11.Checked;
            panel3.Enabled = checkBox1.Checked;
            cbbScript.Enabled = checkBox1.Checked;
            panel9.Enabled = checkBox2.Checked;
            panel4.Enabled = checkBox3.Checked;
            panel5.Enabled = checkBox4.Checked;
            panel6.Enabled = checkBox5.Checked;
            panel7.Enabled = checkBox8.Checked;
        }

        private void txtLines_TextChanged(object sender, EventArgs e)
        {
            int newCount = txtLines.Lines.Length;
            label9.Text = Regex.Replace(label9.Text, @"\(\d+\)", $"({newCount.ToMoneyString()}):");
        }

        private void cbb_ListTypeProxy_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cbb_ListTypeProxy.Text))
            {
                return;
            }
            if (cbb_ListTypeProxy.SelectedIndex == 0 || cbb_ListTypeProxy.SelectedIndex == 1 || cbb_ListTypeProxy.SelectedIndex == 2)
            {
                groupBox2.Enabled = false;
            }
            else
            {
                groupBox2.Enabled = true;
            }
            if (cbb_ListTypeProxy.SelectedIndex == 3 || cbb_ListTypeProxy.SelectedIndex == 4 || cbb_ListTypeProxy.SelectedIndex == 5)
            {
                txtLines.PlaceholderText = "   Ví dụ: key";
                label7.Text = "Mỗi key 1 dòng";
                label9.Text = "Danh sách key (0):";
            }
            if (cbb_ListTypeProxy.SelectedIndex == 6)
            {
                txtLines.PlaceholderText = "   Ví dụ: ip:port|Link hoặc ip:port:user:password|Link";
                label7.Text = "Mỗi proxy 1 dòng";
                label9.Text = "Danh sách proxy (0):";
            }
            if (cbb_ListTypeProxy.SelectedIndex == 7)
            {
                txtLines.PlaceholderText = "   Ví dụ: ip:port hoặc ip:port:user:password";
                label7.Text = "Mỗi proxy 1 dòng";
                label9.Text = "Danh sách proxy (0):";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            fSelectBrandModel fSelectBrand = new fSelectBrandModel(textBox1.Text.Trim());
            if (fSelectBrand.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = fSelectBrand.Brands;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "LamTool.net Chọn file APK";
            openFileDialog.Filter = "File APK (*.apk)|*.apk";
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBox4.Text = openFileDialog.FileName;
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog f = new System.Windows.Forms.FolderBrowserDialog();
            if (f.ShowDialog() == DialogResult.OK)
            {
                if (f.SelectedPath.Trim() == textBox3.Text.Trim())
                {
                    CommonMethod.ShowConfirmWarning("Vui lòng chọn thư mục khác với thư mục profile");
                    return;
                }
                textBox2.Text = f.SelectedPath;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog f = new System.Windows.Forms.FolderBrowserDialog();
            if (f.ShowDialog() == DialogResult.OK)
            {
                if (f.SelectedPath.Trim() == textBox2.Text.Trim())
                {
                    CommonMethod.ShowConfirmWarning("Vui lòng chọn thư mục khác với thư mục device");
                    return;
                }
                textBox3.Text = f.SelectedPath;
            }
        }

        private void tabPageSettingDefault_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string text = comboBox1.Text.Trim();
            if (text == RegistrationType.Gmail || text == RegistrationType.Gmail_BaitPhoneNumber)
            {
                groupBoxGmail.Visible = true;
                groupBoxGmail.Dock = DockStyle.Fill;
                groupBoxDomain.Visible = false;
                groupBoxPhone.Visible = false;
            }
            else if (text == RegistrationType.Domain || text == RegistrationType.Domain_BaitPhoneNumber)
            {
                groupBoxGmail.Visible = false;
                groupBoxDomain.Dock = DockStyle.Fill;
                groupBoxDomain.Visible = true;
                groupBoxPhone.Visible = false;
            }
            else if (text == RegistrationType.PhoneNumber)
            {
                groupBoxGmail.Visible = false;
                groupBoxPhone.Dock = DockStyle.Fill;
                groupBoxDomain.Visible = false;
                groupBoxPhone.Visible = true;
            }
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            panel8.Enabled = checkBox11.Checked;
        }

        private void checkBox9_CheckedChanged_1(object sender, EventArgs e)
        {
            groupBox4.Enabled = checkBox9.Checked;
            groupBox9.Enabled = checkBox9.Checked;
        }

        private void checkBox12_CheckedChanged(object sender, EventArgs e)
        {
            panel21.Enabled = checkBox12.Checked;
        }

        private void check_Avatar_CheckedChanged(object sender, EventArgs e)
        {
            panel24.Enabled = check_Avatar.Checked;
        }

        private void check_2FA_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void check_Bia_CheckedChanged(object sender, EventArgs e)
        {
            panel23.Enabled = check_Bia.Checked;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog f = new System.Windows.Forms.FolderBrowserDialog();
            if (f.ShowDialog() == DialogResult.OK)
            {
                txtAvatar.Text = f.SelectedPath;

            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog f = new System.Windows.Forms.FolderBrowserDialog();
            if (f.ShowDialog() == DialogResult.OK)
            {
                txtBia.Text = f.SelectedPath;

            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Chọn một tệp văn bản"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txt_Ho.Text = openFileDialog.FileName;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Chọn một tệp văn bản"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txt_Ten.Text = openFileDialog.FileName;
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            txtPass.Enabled = radioButton2.Checked;
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            panel11.Enabled = checkBox6.Checked;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "LamTool.net Chọn file Gmail";
            openFileDialog.Filter = "File text (*.txt)|*.txt";
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtGmail.Text = openFileDialog.FileName;
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            string text = comboBox3.Text.Trim();
            if (text == RegistrationType.Domain_Shopgmail9999 || text == RegistrationType.Domain_TheLoi)
            {
                textBox9.Enabled = true;
            }
            else
            {
                textBox9.Enabled = false;
            }
        }

        private async void button12_Click(object sender, EventArgs e)
        {
            string message = "";
            try
            {
                TelegramBotServices telegramBotServices = new TelegramBotServices(textBox7.Text.Trim());
                message = await telegramBotServices.SendMessageAsync(Convert.ToInt64(textBox8.Text.Trim()), "LamTool.net gửi tin nhắn test message thông báo!");
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            CommonMethod.ShowMessageSuccess(message);
        }

        private void cbb_Email_SelectedIndexChanged(object sender, EventArgs e)
        {
            string text = cbb_Email.Text.Trim();
            if (text == RegistrationType.Domain_Shopgmail9999 || text == RegistrationType.Domain_TheLoi)
            {
                textBox6.Enabled = true;
            }
            else
            {
                textBox6.Enabled = false;
            }
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            panel17.Enabled = radioButton3.Checked;
            panel15.Enabled = radioButton3.Checked;
        }
    }
}
