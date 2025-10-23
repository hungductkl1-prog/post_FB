using AntdUI;
using LamToolAutoPhonePrime.Utils;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;

namespace LamToolAutoPhonePrime.Views.Forms
{
    public partial class fViewDataGridView : AntdUI.Window
    {
        private FolderContext _folderContext;
        private string _type = "";
        private string _platform = "";
        public fViewDataGridView(List<string> cases, string name)
        {
            InitializeComponent();
            AddCheckBoxesToFlowLayoutPanel(cases, name);
            FontUtil.ApplyFontToAllControls(this);
        }
        private void AddCheckBoxesToFlowLayoutPanel(List<string> cases, string name)
        {
            flowLayoutPanel1.Controls.Clear();
            flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;

            var configFile = $"configs\\{name}.txt";
            Dictionary<string, bool> configLines = new();

            // Đọc cấu hình hiện có và loại bỏ key trùng
            if (File.Exists(configFile))
            {
                foreach (var line in File.ReadAllLines(configFile))
                {
                    if (!line.Contains("|")) continue;
                    var parts = line.Split('|');
                    if (parts.Length != 2) continue;

                    var key = parts[0].Trim();
                    var value = parts[1].Trim().ToLower() == "true";

                    if (!configLines.ContainsKey(key))
                        configLines[key] = value;
                }
            }

            foreach (var item in cases)
            {
                bool isChecked = configLines.TryGetValue(item.Trim(), out bool val) ? val : true;

                CheckBox checkBox = new CheckBox
                {
                    Text = item,
                    AutoSize = true,
                    Size = new Size(247, 19),
                    Checked = isChecked
                };

                // Gắn sự kiện thay đổi để ghi file
                checkBox.CheckedChanged += (s, e) => UpdateConfigFile(name);

                flowLayoutPanel1.Controls.Add(checkBox);
            }
        }
        private void UpdateConfigFile(string name)
        {
            var configFile = $"configs\\{name}.txt";
            var config = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            // Ghi cấu hình hiện tại từ checkbox
            foreach (Control ctrl in flowLayoutPanel1.Controls)
            {
                if (ctrl is CheckBox cb)
                {
                    config[cb.Text.Trim()] = cb.Checked;
                }
            }

            var lines = config.Select(kvp => $"{kvp.Key}|{kvp.Value.ToString().ToLower()}");
            File.WriteAllLines(configFile, lines);
        }
        private void button9_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_setting_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
