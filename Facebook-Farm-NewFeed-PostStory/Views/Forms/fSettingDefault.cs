using Sunny.Subd.Core.Models;
using Sunny.Subd.Core.Services;
using AntdUI;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Sunny.Subd.Core.Facebook;
using Sunny.Subd.Core.Proxies;
using Sunny.Subdy.Common.API.Captchas;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Services;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Text.RegularExpressions;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

using CommonMethod = Sunny.Subdy.Common.ControlMethod.CommonMethod;
namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    public partial class fSettingDefault : AntdUI.Window
    {
        private string _platform = "";
        private System.Windows.Forms.Timer _txtLinesDebounceTimer;
        private string _label9BaseRaw = "";
        private Sunny.Subdy.Common.Json.ConfigHelper _configHelper;

        public fSettingDefault(string platform)
        {
            SuspendLayout();

            InitializeComponent();

            _platform = platform;

            // Batch populate controls
            cbb_ListTypeProxy.Items.AddRange(ProxyService.ProxyTypes.ToArray());
            cbbScript.Items.AddRange(SubdyHelper.Countries.ToArray());
            comboBox1.Items.AddRange(FacebookHander.TypeLogin.ToArray());

            // Batch attach repeated CheckedChanged handlers to reduce code duplication
            var boxes = new CheckBox[]
            {
                    checkBox9, checkBox11, checkBox13, checkBox1, checkBox2, checkBox3,
                    checkBox4, checkBox5, checkBox6, checkBox8, checkBox15, checkBox17
            };
            foreach (var cb in boxes)
            {
                if (cb != null && cb != checkBox9) // checkBox9 already has its own event (keeps existing call)
                {
                    cb.CheckedChanged += checkBox9_CheckedChanged;
                }
            }
            // Some radio buttons also trigger the same UI enable/disable logic
            radioButton3.CheckedChanged += checkBox9_CheckedChanged;
            radioButton1.CheckedChanged += checkBox9_CheckedChanged;

            // Timer used to debounce txtLines updates for smoother UI
            _txtLinesDebounceTimer = new System.Windows.Forms.Timer
            {
                Interval = 300 // ms
            };
            _txtLinesDebounceTimer.Tick += (s, e) =>
            {
                _txtLinesDebounceTimer.Stop();
                UpdateLabelCount(txtLines.Lines.Length);
            };

            // Wire TextChanged to debounce only (fast)
            txtLines.TextChanged -= txtLines_TextChanged;
            txtLines.TextChanged += txtLines_TextChanged;

            // Keep initial small UI updates synchronous
            checkBox9_CheckedChanged(null, null);
            cbb_ListTypeProxy.SelectedIndexChanged += cbb_ListTypeProxy_SelectedIndexChanged;

            // Defer heavier configuration load until form is shown to avoid blocking constructor/UI thread
            _configHelper = new Sunny.Subdy.Common.Json.ConfigHelper(this, $"{nameof(fSettingDefault)}_{_platform}", onLoad: new System.Action(() =>
            {
                LoadForm();
                cbb_ListTypeProxy_SelectedIndexChanged(null, null);
                // Update label count (debounced timer not used for initial load)
                UpdateLabelCount(txtLines.Lines.Length);
            }), shouldExit: false);

            // Apply fonts after initial rendering to avoid layout jank
            FontUtil.ApplyFontToAllControls(this);

            // Finalize layout resume
            ResumeLayout(false);

            // Ensure initial selected tab
            tabs3.SelectedIndex = 0;

            this.Load += (_, __) => Facebook_Farm_NewFeed_PostStory.Utils.Design.SsaTheme.ApplyFSettingDefault(this);
        }

        private void LoadForm()
        {
            if (string.IsNullOrEmpty(textBox1.Text.Trim()))
            {
                textBox1.Text = DeviceServices.Brands;
            }
            if (string.IsNullOrEmpty(textBox2.Text.Trim()))
            {
                textBox2.Text = Path.Combine(AppContext.BaseDirectory, "Backup", "Device", _platform);
            }
            if (string.IsNullOrEmpty(textBox3.Text.Trim()))
            {
                textBox3.Text = Path.Combine(AppContext.BaseDirectory, "Backup", "Profile", _platform);
            }
            if (string.IsNullOrEmpty(textBox4.Text.Trim()))
            {
                // Keep this lazy/eager depending on cost; it's called after Form shown to reduce constructor work
                textBox4.Text = FacebookHander.FilePath(_platform);
            }

            checkBox7.Enabled = checkBox6.Checked;
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
            // Only update enabled states - keep logic minimal and fast
            groupBox1.Enabled = radioButton3.Checked;
            panel8.Enabled = checkBox11.Checked;
            numericUpDown4.Enabled = checkBox13.Checked;
            panel3.Enabled = checkBox1.Checked;
            cbbScript.Enabled = checkBox1.Checked;
            panel9.Enabled = checkBox2.Checked;
            panel4.Enabled = checkBox3.Checked;
            panel5.Enabled = checkBox4.Checked;
            panel6.Enabled = checkBox5.Checked;
            checkBox7.Enabled = checkBox6.Checked;
            panel7.Enabled = checkBox8.Checked;
            timePicker1.Enabled = timePicker2.Enabled = checkBox15.Checked;
            panel11.Enabled = checkBox17.Checked;
        }

        private void txtLines_TextChanged(object sender, EventArgs e)
        {
            // Debounce updates to avoid frequent UI work (especially when typing or pasting many lines)
            if (_txtLinesDebounceTimer.Enabled)
            {
                _txtLinesDebounceTimer.Stop();
            }
            _txtLinesDebounceTimer.Start();
        }

        /// <summary>
        /// Update label9 using cached base text to avoid repeated Regex.Replace on every change.
        /// </summary>
        private void UpdateLabelCount(int newCount)
        {
            if (string.IsNullOrEmpty(_label9BaseRaw))
            {
                // Cache the base (strip any existing "(n)" and trailing colon from label9)
                _label9BaseRaw = StripCountFromLabel(label9.Text);
            }

            // Compose label once
            label9.Text = $"{_label9BaseRaw} ({newCount.ToMoneyString()}):";
        }

        private static string StripCountFromLabel(string label)
        {
            if (string.IsNullOrEmpty(label)) return label ?? "";
            // Remove patterns like " (123):", " (0):", or trailing colon
            return Regex.Replace(label, @"\s*\(\d+\)\s*:?\s*$", "").Trim();
        }

        private void cbb_ListTypeProxy_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cbb_ListTypeProxy.Text))
            {
                return;
            }

            // Enable group box for specific proxy types
            int idx = cbb_ListTypeProxy.SelectedIndex;
            groupBox2.Enabled = !(idx == 0 || idx == 1 || idx == 2);

            // Use switch to set placeholders and labels; update cached base for label9
            switch (idx)
            {
                case 3:
                case 4:
                case 5:
                    txtLines.PlaceholderText = "   Ví dụ: key";
                    label7.Text = "Mỗi key 1 dòng";
                    label9.Text = "Danh sách key (0):";
                    break;
                case 6:
                    txtLines.PlaceholderText = "   Ví dụ: ip:port|Link hoặc ip:port:user:password|Link";
                    label7.Text = "Mỗi proxy 1 dòng";
                    label9.Text = "Danh sách proxy (0):";
                    break;
                case 7:
                    txtLines.PlaceholderText = "   Ví dụ: ip:port hoặc ip:port:user:password";
                    label7.Text = "Mỗi proxy 1 dòng";
                    label9.Text = "Danh sách proxy (0):";
                    break;
                default:
                    // Default behavior for types 0,1,2 or others
                    txtLines.PlaceholderText = "";
                    label7.Text = "Mỗi proxy 1 dòng";
                    label9.Text = "Danh sách proxy (0):";
                    break;
            }

            // Reset cached base so UpdateLabelCount composes correctly
            _label9BaseRaw = StripCountFromLabel(label9.Text);

            // Immediately update count display (no debounce here because this is a control action)
            UpdateLabelCount(txtLines.Lines.Length);
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
            openFileDialog.Title = "QN.net Chọn file APK";
            openFileDialog.Filter = "File APK (*.apk)|*.apk";
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBox4.Text = openFileDialog.FileName;
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            _configHelper.ControlClosing(null, EventArgs.Empty);
            this.Close();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog f = new FolderBrowserDialog();
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
            FolderBrowserDialog f = new FolderBrowserDialog();
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

        private async void buttonCheckCaptcha_Click(object sender, EventArgs e)
        {
            string key = textBoxCaptchaKey.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(key))
            {
                labelCaptchaStatus.ForeColor = Color.IndianRed;
                labelCaptchaStatus.Text = "Vui lòng nhập key cap.guru trước khi check.";
                return;
            }

            buttonCheckCaptcha.Enabled = false;
            labelCaptchaStatus.ForeColor = Color.FromArgb(100, 100, 100);
            labelCaptchaStatus.Text = "Đang kiểm tra số dư...";

            try
            {
                string result = await CaptchaService.Getbalance(GuruCaptchaClient.Url, key);
                if (string.IsNullOrEmpty(result) || result.Contains("error", StringComparison.OrdinalIgnoreCase))
                {
                    labelCaptchaStatus.ForeColor = Color.IndianRed;
                    labelCaptchaStatus.Text = $"Lỗi: {result}";
                }
                else
                {
                    labelCaptchaStatus.ForeColor = Color.SeaGreen;
                    labelCaptchaStatus.Text = $"Số dư: {result}$";
                }
            }
            catch (Exception ex)
            {
                labelCaptchaStatus.ForeColor = Color.IndianRed;
                labelCaptchaStatus.Text = $"Lỗi: {ex.Message}";
            }
            finally
            {
                buttonCheckCaptcha.Enabled = true;
            }
        }
    }
}
