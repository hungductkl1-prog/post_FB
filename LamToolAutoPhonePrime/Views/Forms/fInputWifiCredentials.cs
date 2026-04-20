using AntdUI;
using LamToolAutoPhonePrime.Utils;
using Sunny.Subdy.Common.Helper;
using System.Windows.Forms;
using CommonMethod = Sunny.Subdy.Common.ControlMethod.CommonMethod;

namespace LamToolAutoPhonePrime.Views.Forms
{
    public partial class fInputWifiCredentials : AntdUI.Window
    {
        private const string ConfigFileName = "wifi-credentials-input.txt";
        private static readonly string ConfigPath =
            Path.Combine(AppContext.BaseDirectory, "Config", ConfigFileName);

        private AntdUI.PageHeader windowBar = null!;
        private AntdUI.Input txtLines = null!;
        private AntdUI.Button btnOk = null!;
        private AntdUI.Button btnCancel = null!;
        private System.Windows.Forms.Label lblHint = null!;

        public List<string> Lines { get; private set; } = new();

        public fInputWifiCredentials()
        {
            BuildUi();
            FontUtil.ApplyFontToAllControls(this);
            txtLines.Text = SafeReadConfig();
        }

        private void BuildUi()
        {
            Text = "Danh sách Wifi (username|password)";
            ClientSize = new Size(520, 460);
            MinimumSize = new Size(520, 360);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(245, 247, 250);

            windowBar = new AntdUI.PageHeader
            {
                Dock = DockStyle.Top,
                Text = "Nhập danh sách Wifi",
                MDI = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                Size = new Size(520, 36),
            };
            Controls.Add(windowBar);

            var bottom = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.White,
                Padding = new Padding(12, 10, 12, 10),
            };
            Controls.Add(bottom);

            btnOk = new AntdUI.Button
            {
                Dock = DockStyle.Right,
                Width = 120,
                Text = "Áp dụng",
                Type = AntdUI.TTypeMini.Success,
                Shape = AntdUI.TShape.Round,
                IconSvg = "CheckOutlined",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            };
            btnOk.Click += BtnOk_Click;
            bottom.Controls.Add(btnOk);

            var spacer = new System.Windows.Forms.Panel { Dock = DockStyle.Right, Width = 8, BackColor = Color.Transparent };
            bottom.Controls.Add(spacer);

            btnCancel = new AntdUI.Button
            {
                Dock = DockStyle.Right,
                Width = 110,
                Text = "Đóng",
                Type = AntdUI.TTypeMini.Error,
                Shape = AntdUI.TShape.Round,
                IconSvg = "CloseOutlined",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            };
            btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };
            bottom.Controls.Add(btnCancel);

            var content = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 12),
                BackColor = Color.Transparent,
            };
            Controls.Add(content);
            Controls.SetChildIndex(content, 0);

            lblHint = new System.Windows.Forms.Label
            {
                Text = "Mỗi dòng 1 wifi theo định dạng:  ssid|password",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
            };
            content.Controls.Add(lblHint);

            txtLines = new AntdUI.Input
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                PlaceholderText = "VD:\nMyWifi|12345678\nGuest|abcdef",
            };
            content.Controls.Add(txtLines);
            content.Controls.SetChildIndex(txtLines, 0);
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            var raw = txtLines.Text ?? "";
            var lines = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(l => l.Trim())
                           .Where(l => !string.IsNullOrEmpty(l) && l.Contains('|'))
                           .ToList();

            if (lines.Count == 0)
            {
                CommonMethod.ShowMessageWarning("Vui lòng nhập ít nhất 1 dòng theo định dạng username|password.");
                return;
            }

            Lines = lines;
            SafeWriteConfig(raw);
            DialogResult = DialogResult.OK;
            Close();
        }

        private static string SafeReadConfig()
        {
            try { return File.Exists(ConfigPath) ? File.ReadAllText(ConfigPath) : ""; }
            catch { return ""; }
        }

        private static void SafeWriteConfig(string content)
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(ConfigPath, content ?? "");
            }
            catch { }
        }
    }
}
