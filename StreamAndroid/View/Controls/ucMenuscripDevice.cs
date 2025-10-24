namespace StreamAndroid
{
    public partial class ucMenuscripDevice : UserControl
    {
        private Form? targetForm;
        private Form? parentForm; 
        public event EventHandler? SettingsButtonClicked;
        public ucMenuscripDevice(bool check, Form form = null, Form formCha = null)
        {
            InitializeComponent();
            button9.Visible = check;
            if (check)
            {
                targetForm = form;
                parentForm = formCha;
                new DragHandler(label2, targetForm, parentForm);
            }
            // ToolStrip Buttons Config
            ConfigureToolStripButton(toolStripButton1, "Khởi động lại");
            ConfigureToolStripButton(toolStripButton6, "Cài đặt APK");
            ConfigureToolStripButton(toolStripButton5, "Xuất tệp");
            ConfigureToolStripButton(toolStripButton4, "Xuất bảng nhớ");
            ConfigureToolStripDropDown(toolStripButton2, "Lệnh ADB", quảnLýLệnhToolStripMenuItem, "Quản lý lệnh");
            ConfigureToolStripButton(toolStripButton12, "Vuốt tự động");
            ConfigureToolStripButton(toolStripButton11, "Ghi lại hành động");
            ConfigureToolStripButton(toolStripButton10, "Thực hiện hành động");
            ConfigureToolStripButton(toolStripButton9, "Thực hiện nhiệm vụ");
            ConfigureToolStripButton(toolStripButton8, "Kết thúc nhiệm vụ");
            ConfigureToolStripDropDown(toolStripDropDownButton1, "Chuyển đổi phương", toolStripMenuItem1, "Quản lý lệnh");
            ConfigureToolStripButton(toolStripButton3, "Xoay phải");
            // Buttons bottom
            ConfigureAntdButton(button1, "BorderOutlined");
            ConfigureAntdButton(button2, "HomeOutlined");
            ConfigureAntdButton(button3, "DoubleLeftOutlined");
        }


        private void ConfigureToolStripButton(ToolStripButton btn, string text)
        {
            btn.Font = new Font("Segoe UI", 9.75F);
            btn.ForeColor = Color.Black;
           // btn.Image = Properties.Resources.facebook;
            btn.ImageAlign = ContentAlignment.MiddleLeft;
            btn.ImageTransparentColor = Color.Magenta;
            btn.Text = text;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(2);
            btn.AutoSize = true;
        }

        private void ConfigureToolStripDropDown(ToolStripDropDownButton btn, string text, ToolStripMenuItem item, string subText)
        {
            btn.Font = new Font("Segoe UI", 9.75F);
            btn.ForeColor = Color.Black;
          //  btn.Image = Properties.Resources.facebook;
            btn.ImageAlign = ContentAlignment.MiddleLeft;
            btn.Text = text;
            item.Text = subText;
            btn.DropDownItems.Add(item);
        }

        private void ConfigureAntdButton(AntdUI.Button btn, string icon)
        {
            btn.Dock = DockStyle.Fill;
            btn.Cursor = Cursors.Hand;
            btn.DefaultBack = Color.Transparent;
            btn.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            btn.ForeColor = Color.Black;
            btn.IconSvg = icon;
            btn.IconRatio = 0.9F;
            btn.Radius = 10;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if(targetForm != null && parentForm != null)
            {
                SettingsButtonClicked?.Invoke(this, EventArgs.Empty);

            }
        }
    }
}
