using Facebook_Farm_NewFeed_PostStory.Utils;
using Facebook_Farm_NewFeed_PostStory.Utils.Design;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    public partial class fLicenseCheck : BaseForm
    {
        private TextBox _txtDeviceId;
        private Button _btnCopy;
        private Button _btnRetry;
        private Label _lblStatus;

        public fLicenseCheck()
        {
            InitializeControls();
            FontUtil.ApplyFontToAllControls(this);
            VietnameseFont.Enforce(this);

            this.Shown += OnShown;
        }

        private void OnShown(object? sender, EventArgs e)
        {
            _ = CheckLicenseAsync();
        }

        private void InitializeControls()
        {
            Text = "Kích hoạt thiết bị";
            Size = new Size(520, 280);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = true;
            BackColor = Color.FromArgb(245, 247, 250);

            var lblTitle = new Label
            {
                Text = "Thiết bị chưa được kích hoạt",
                Font = FontScale.HeadingBold,
                ForeColor = Color.FromArgb(48, 48, 48),
                Location = new Point(28, 24),
                Size = new Size(464, 28),
                TextAlign = ContentAlignment.MiddleLeft,
            };

            var lblDesc = new Label
            {
                Text = "Vui lòng gửi mã thiết bị bên dưới cho admin để được kích hoạt:",
                Font = FontScale.Body9,
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(28, 56),
                Size = new Size(464, 20),
            };

            var lblDeviceId = new Label
            {
                Text = "Mã thiết bị (DeviceId):",
                Font = FontScale.Body9Bold,
                ForeColor = Color.FromArgb(48, 48, 48),
                Location = new Point(28, 92),
                Size = new Size(200, 20),
            };

            _txtDeviceId = new TextBox
            {
                Text = Globals.DeviceId,
                ReadOnly = true,
                Location = new Point(28, 114),
                Size = new Size(360, 24),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 10f, FontStyle.Regular),
                Cursor = Cursors.IBeam,
            };

            _btnCopy = new Button
            {
                Text = "Sao chép",
                Location = new Point(396, 112),
                Size = new Size(90, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(82, 196, 26),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
            };
            _btnCopy.FlatAppearance.BorderSize = 0;
            _btnCopy.Click += BtnCopy_Click;

            _lblStatus = new Label
            {
                Text = "Đang kiểm tra...",
                Font = FontScale.Body9,
                ForeColor = Color.Gray,
                Location = new Point(28, 155),
                Size = new Size(464, 20),
            };

            _btnRetry = new Button
            {
                Text = "Thử lại",
                Location = new Point(205, 195),
                Size = new Size(110, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(24, 144, 255),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Enabled = false,
            };
            _btnRetry.FlatAppearance.BorderSize = 0;
            _btnRetry.Click += BtnRetry_Click;

            Controls.Add(lblTitle);
            Controls.Add(lblDesc);
            Controls.Add(lblDeviceId);
            Controls.Add(_txtDeviceId);
            Controls.Add(_btnCopy);
            Controls.Add(_lblStatus);
            Controls.Add(_btnRetry);
        }

        private void BtnCopy_Click(object? sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(Globals.DeviceId);
                _btnCopy.Text = "✓ Đã sao chép";
            }
            catch
            {
                _btnCopy.Text = "❌ Sao chép thất bại";
            }
        }

        private async void BtnRetry_Click(object? sender, EventArgs e)
        {
            _btnRetry.Enabled = false;
            _lblStatus.Text = "Đang kiểm tra...";
            _lblStatus.ForeColor = Color.Gray;
            await CheckLicenseAsync();
        }

        private async System.Threading.Tasks.Task CheckLicenseAsync()
        {
            try
            {
                var (valid, message) = await System.Threading.Tasks.Task.Run(
                    () => LicenseHelper.CheckDeviceActivation());

                if (valid)
                {
                    _lblStatus.Text = "✓ Kích hoạt thành công!";
                    _lblStatus.ForeColor = Color.FromArgb(82, 196, 26);
                    // Đợi 500ms để user thấy status rồi tự đóng
                    await System.Threading.Tasks.Task.Delay(500);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    _lblStatus.Text = message;
                    _lblStatus.ForeColor = Color.FromArgb(245, 34, 45);
                    _btnRetry.Enabled = true;
                    _btnRetry.Focus();
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"Lỗi: {ex.Message}";
                _lblStatus.ForeColor = Color.FromArgb(245, 34, 45);
                _btnRetry.Enabled = true;
            }
        }
    }
}
