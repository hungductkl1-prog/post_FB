namespace Facebook_Farm_NewFeed_PostStory.Views
{
    partial class fQuanLyKichBan
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            windowBar = new AntdUI.PageHeader();
            button1 = new AntdUI.Button();
            txt_search = new AntdUI.Input();
            btn_mode = new AntdUI.Button();
            btn_global = new AntdUI.Button();
            btn_setting = new AntdUI.Button();
            panel4 = new AntdUI.Panel();
            // Left column
            radioButton1 = new RadioButton();
            radioButton2 = new RadioButton();
            radioButton3 = new RadioButton();
            radioButton4 = new RadioButton();
            lblSoLanLap = new Label();
            nudSoLanLap = new NumericUpDown();
            lblLuot = new Label();
            lblChoLuot = new Label();
            nudChoLuotFrom = new NumericUpDown();
            lblDenLuot = new Label();
            nudChoLuotTo = new NumericUpDown();
            lblPhutLuot = new Label();
            // Right column
            checkBox1 = new CheckBox();
            checkBox2 = new CheckBox();
            nudTaiKhoanFrom = new NumericUpDown();
            lblDenTaiKhoan = new Label();
            nudTaiKhoanTo = new NumericUpDown();
            lblPhutTaiKhoan = new Label();
            checkBox3 = new CheckBox();
            nudKichBanFrom = new NumericUpDown();
            lblDenKichBan = new Label();
            nudKichBanTo = new NumericUpDown();
            lblPhutKichBan = new Label();
            checkBox4 = new CheckBox();
            lblThoiGianBatDau = new Label();
            timepickerFrom = new AntdUI.TimePicker();
            lblDenNgay = new Label();
            timepickerTo = new AntdUI.TimePicker();
            // Info
            llbHuongDan = new LinkLabel();
            lblCanhBao1 = new Label();
            lblCanhBao2 = new Label();
            // VirtualPanel
            virtualPanel = new AntdUI.VirtualPanel();
            // hidden
            input6 = new AntdUI.Input();
            button16 = new AntdUI.Button();

            windowBar.SuspendLayout();
            panel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudSoLanLap).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudChoLuotFrom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudChoLuotTo).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudTaiKhoanFrom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudTaiKhoanTo).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudKichBanFrom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudKichBanTo).BeginInit();
            SuspendLayout();

            // windowBar
            windowBar.BackColor = Color.White;
            windowBar.CloseSize = 30;
            windowBar.Controls.Add(button1);
            windowBar.Controls.Add(txt_search);
            windowBar.Controls.Add(btn_mode);
            windowBar.Controls.Add(btn_global);
            windowBar.Controls.Add(btn_setting);
            windowBar.Cursor = Cursors.Hand;
            windowBar.DividerMargin = 1;
            windowBar.Dock = DockStyle.Top;
            windowBar.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            windowBar.ForeColor = Color.Black;
            windowBar.Icon = Properties.Resources.logo_lamtool_v3_dark_16;
            windowBar.MDI = true;
            windowBar.Name = "windowBar";
            windowBar.ShowIcon = true;
            windowBar.Size = new Size(1300, 35);
            windowBar.SubFont = new Font("Microsoft Sans Serif", Facebook_Farm_NewFeed_PostStory.Utils.Design.FontScale.Caption, FontStyle.Regular, GraphicsUnit.Point, 0);
            windowBar.SubGap = 1;
            windowBar.SubText = "";
            windowBar.TabIndex = 10;
            windowBar.Text = "Quản lý kịch bản";
            windowBar.UseSystemStyleColor = true;
            windowBar.UseTextBold = false;

            button1.Dock = DockStyle.Right;
            button1.Ghost = true;
            button1.Icon = Properties.Resources.icons8_add_16;
            button1.IconRatio = 1F;
            button1.IconSvg = "";
            button1.Name = "button1";
            button1.Radius = 0;
            button1.Size = new Size(32, 35);
            button1.TabIndex = 12;
            button1.ToggleIconSvg = "MoonOutlined";
            button1.WaveSize = 0;
            button1.Click += button1_Click;

            txt_search.Dock = DockStyle.Right;
            txt_search.LocalizationPlaceholderText = "Overview.{id}";
            txt_search.Name = "txt_search";
            txt_search.Padding = new Padding(0, 2, 0, 2);
            txt_search.PlaceholderText = "Tìm kiếm...";
            txt_search.PrefixSvg = "SearchOutlined";
            txt_search.Size = new Size(242, 35);
            txt_search.TabIndex = 11;

            btn_mode.Dock = DockStyle.Right;
            btn_mode.Ghost = true;
            btn_mode.Icon = Properties.Resources.icons8_circle_16_Green;
            btn_mode.IconSvg = "";
            btn_mode.Name = "btn_mode";
            btn_mode.Radius = 0;
            btn_mode.Size = new Size(26, 35);
            btn_mode.TabIndex = 6;
            btn_mode.ToggleIconSvg = "MoonOutlined";
            btn_mode.WaveSize = 0;

            btn_global.Dock = DockStyle.Right;
            btn_global.Ghost = true;
            btn_global.Icon = Properties.Resources.icons8_circle_16_Yellow;
            btn_global.IconSvg = "";
            btn_global.Name = "btn_global";
            btn_global.Radius = 0;
            btn_global.Size = new Size(24, 35);
            btn_global.TabIndex = 7;
            btn_global.WaveSize = 0;
            btn_global.Click += btn_global_Click;

            btn_setting.Dock = DockStyle.Right;
            btn_setting.Ghost = true;
            btn_setting.Icon = Properties.Resources.icons8_circle_16_Red;
            btn_setting.IconSvg = "";
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(30, 35);
            btn_setting.TabIndex = 8;
            btn_setting.WaveSize = 0;
            btn_setting.Click += btn_setting_Click;

            // ── panel4 ──
            panel4.Back = Color.White;
            panel4.BackColor = Color.Transparent;
            panel4.Dock = DockStyle.Top;
            panel4.Font = new Font("Microsoft YaHei UI", 9F);
            panel4.Location = new Point(0, 35);
            panel4.Name = "panel4";
            panel4.padding = new Padding(16);
            panel4.Radius = 16;
            panel4.Size = new Size(1300, 200);
            panel4.TabIndex = 11;
            panel4.Text = "panel4";

            // ── LEFT COLUMN: Radio buttons ──
            // radioButton1
            radioButton1.AutoSize = true;
            radioButton1.Checked = true;
            radioButton1.Location = new Point(20, 18);
            radioButton1.Name = "radioButton1";
            radioButton1.TabIndex = 0;
            radioButton1.TabStop = true;
            radioButton1.Text = "Chạy hết tương tác thì kết thúc";
            radioButton1.UseVisualStyleBackColor = true;

            radioButton2.AutoSize = true;
            radioButton2.Location = new Point(20, 44);
            radioButton2.Name = "radioButton2";
            radioButton2.TabIndex = 1;
            radioButton2.Text = "Chạy hết tương tác thì lướt newfeed";
            radioButton2.UseVisualStyleBackColor = true;

            radioButton3.AutoSize = true;
            radioButton3.Location = new Point(20, 70);
            radioButton3.Name = "radioButton3";
            radioButton3.TabIndex = 2;
            radioButton3.Text = "Chạy hết tương tác thì like newfeed";
            radioButton3.UseVisualStyleBackColor = true;

            radioButton4.AutoSize = true;
            radioButton4.Location = new Point(20, 96);
            radioButton4.Name = "radioButton4";
            radioButton4.TabIndex = 3;
            radioButton4.Text = "Chạy hết tương tác thì lặp lại toàn bộ tài khoản";
            radioButton4.UseVisualStyleBackColor = true;

            // Số lần lặp lại (row dưới radio4, indent thêm)
            lblSoLanLap.AutoSize = true;
            lblSoLanLap.Enabled = false;
            lblSoLanLap.Location = new Point(40, 122);
            lblSoLanLap.Name = "lblSoLanLap";
            lblSoLanLap.Text = "Số lần lặp lại:";

            nudSoLanLap.Enabled = false;
            nudSoLanLap.Location = new Point(122, 119);
            nudSoLanLap.Minimum = 1;
            nudSoLanLap.Maximum = 9999;
            nudSoLanLap.Value = 1;
            nudSoLanLap.Name = "nudSoLanLap";
            nudSoLanLap.Size = new Size(46, 23);
            nudSoLanLap.TabIndex = 4;

            lblLuot.AutoSize = true;
            lblLuot.Enabled = false;
            lblLuot.Location = new Point(172, 122);
            lblLuot.Name = "lblLuot";
            lblLuot.Text = "lượt";

            lblChoLuot.AutoSize = true;
            lblChoLuot.Enabled = false;
            lblChoLuot.Location = new Point(210, 122);
            lblChoLuot.Name = "lblChoLuot";
            lblChoLuot.Text = "Chờ lượt kế tiếp:";

            nudChoLuotFrom.Enabled = false;
            nudChoLuotFrom.Location = new Point(310, 119);
            nudChoLuotFrom.Minimum = 1;
            nudChoLuotFrom.Maximum = 9999;
            nudChoLuotFrom.Value = 300;
            nudChoLuotFrom.Name = "nudChoLuotFrom";
            nudChoLuotFrom.Size = new Size(50, 23);
            nudChoLuotFrom.TabIndex = 5;

            lblDenLuot.AutoSize = true;
            lblDenLuot.Enabled = false;
            lblDenLuot.Location = new Point(364, 122);
            lblDenLuot.Name = "lblDenLuot";
            lblDenLuot.Text = "đến";

            nudChoLuotTo.Enabled = false;
            nudChoLuotTo.Location = new Point(390, 119);
            nudChoLuotTo.Minimum = 1;
            nudChoLuotTo.Maximum = 9999;
            nudChoLuotTo.Value = 600;
            nudChoLuotTo.Name = "nudChoLuotTo";
            nudChoLuotTo.Size = new Size(50, 23);
            nudChoLuotTo.TabIndex = 6;

            lblPhutLuot.AutoSize = true;
            lblPhutLuot.Enabled = false;
            lblPhutLuot.Location = new Point(444, 122);
            lblPhutLuot.Name = "lblPhutLuot";
            lblPhutLuot.Text = "phút";

            // ── RIGHT COLUMN: Checkboxes (x=560) ──
            // checkBox1 - Random
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(560, 18);
            checkBox1.Name = "checkBox1";
            checkBox1.TabIndex = 10;
            checkBox1.Text = "Random thứ tự các hành động";
            checkBox1.UseVisualStyleBackColor = true;

            // checkBox2 + numeric inline
            checkBox2.AutoSize = true;
            checkBox2.Location = new Point(560, 44);
            checkBox2.Name = "checkBox2";
            checkBox2.TabIndex = 11;
            checkBox2.Text = "Giới hạn thời gian chạy mỗi tài khoản";
            checkBox2.UseVisualStyleBackColor = true;

            nudTaiKhoanFrom.Enabled = false;
            nudTaiKhoanFrom.Location = new Point(800, 41);
            nudTaiKhoanFrom.Minimum = 1;
            nudTaiKhoanFrom.Maximum = 9999;
            nudTaiKhoanFrom.Value = 40;
            nudTaiKhoanFrom.Name = "nudTaiKhoanFrom";
            nudTaiKhoanFrom.Size = new Size(46, 23);
            nudTaiKhoanFrom.TabIndex = 12;

            lblDenTaiKhoan.AutoSize = true;
            lblDenTaiKhoan.Enabled = false;
            lblDenTaiKhoan.Location = new Point(850, 44);
            lblDenTaiKhoan.Name = "lblDenTaiKhoan";
            lblDenTaiKhoan.Text = "đến";

            nudTaiKhoanTo.Enabled = false;
            nudTaiKhoanTo.Location = new Point(878, 41);
            nudTaiKhoanTo.Minimum = 1;
            nudTaiKhoanTo.Maximum = 9999;
            nudTaiKhoanTo.Value = 60;
            nudTaiKhoanTo.Name = "nudTaiKhoanTo";
            nudTaiKhoanTo.Size = new Size(46, 23);
            nudTaiKhoanTo.TabIndex = 13;

            lblPhutTaiKhoan.AutoSize = true;
            lblPhutTaiKhoan.Enabled = false;
            lblPhutTaiKhoan.Location = new Point(928, 44);
            lblPhutTaiKhoan.Name = "lblPhutTaiKhoan";
            lblPhutTaiKhoan.Text = "phút";

            // checkBox3 + numeric inline
            checkBox3.AutoSize = true;
            checkBox3.Location = new Point(560, 70);
            checkBox3.Name = "checkBox3";
            checkBox3.TabIndex = 14;
            checkBox3.Text = "Giới hạn thời gian chạy mỗi kịch bản";
            checkBox3.UseVisualStyleBackColor = true;

            nudKichBanFrom.Enabled = false;
            nudKichBanFrom.Location = new Point(800, 67);
            nudKichBanFrom.Minimum = 1;
            nudKichBanFrom.Maximum = 9999;
            nudKichBanFrom.Value = 5;
            nudKichBanFrom.Name = "nudKichBanFrom";
            nudKichBanFrom.Size = new Size(46, 23);
            nudKichBanFrom.TabIndex = 15;

            lblDenKichBan.AutoSize = true;
            lblDenKichBan.Enabled = false;
            lblDenKichBan.Location = new Point(850, 70);
            lblDenKichBan.Name = "lblDenKichBan";
            lblDenKichBan.Text = "đến";

            nudKichBanTo.Enabled = false;
            nudKichBanTo.Location = new Point(878, 67);
            nudKichBanTo.Minimum = 1;
            nudKichBanTo.Maximum = 9999;
            nudKichBanTo.Value = 10;
            nudKichBanTo.Name = "nudKichBanTo";
            nudKichBanTo.Size = new Size(46, 23);
            nudKichBanTo.TabIndex = 16;

            lblPhutKichBan.AutoSize = true;
            lblPhutKichBan.Enabled = false;
            lblPhutKichBan.Location = new Point(928, 70);
            lblPhutKichBan.Name = "lblPhutKichBan";
            lblPhutKichBan.Text = "phút";

            // checkBox4 + timepicker inline
            checkBox4.AutoSize = true;
            checkBox4.Location = new Point(560, 96);
            checkBox4.Name = "checkBox4";
            checkBox4.TabIndex = 17;
            checkBox4.Text = "Reset và chạy lại kịch bản khi qua ngày mới";
            checkBox4.UseVisualStyleBackColor = true;

            // timepicker row (dưới checkBox4)
            lblThoiGianBatDau.AutoSize = true;
            lblThoiGianBatDau.Enabled = false;
            lblThoiGianBatDau.Location = new Point(580, 122);
            lblThoiGianBatDau.Name = "lblThoiGianBatDau";
            lblThoiGianBatDau.Text = "Thời gian bắt đầu chạy lại ngẫu nhiên từ:";

            timepickerFrom.Enabled = false;
            timepickerFrom.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            timepickerFrom.Location = new Point(815, 117);
            timepickerFrom.Name = "timepickerFrom";
            timepickerFrom.PlaceholderText = "";
            timepickerFrom.SelectionStart = 8;
            timepickerFrom.Size = new Size(88, 28);
            timepickerFrom.TabIndex = 18;
            timepickerFrom.Text = "09:46:23";
            timepickerFrom.ValueTimeHorizontal = true;

            lblDenNgay.AutoSize = true;
            lblDenNgay.Enabled = false;
            lblDenNgay.Location = new Point(907, 122);
            lblDenNgay.Name = "lblDenNgay";
            lblDenNgay.Text = "đến";

            timepickerTo.Enabled = false;
            timepickerTo.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            timepickerTo.Location = new Point(932, 117);
            timepickerTo.Name = "timepickerTo";
            timepickerTo.PlaceholderText = "";
            timepickerTo.SelectionStart = 8;
            timepickerTo.Size = new Size(88, 28);
            timepickerTo.TabIndex = 19;
            timepickerTo.Text = "09:46:23";
            timepickerTo.ValueTimeHorizontal = true;

            // ── Info row ──
            llbHuongDan.AutoSize = true;
            llbHuongDan.Location = new Point(20, 158);
            llbHuongDan.Name = "llbHuongDan";
            llbHuongDan.TabIndex = 20;
            llbHuongDan.TabStop = true;
            llbHuongDan.Text = "[ Hướng Dẫn Sử Dụng ]";
            llbHuongDan.LinkClicked += llbHuongDan_LinkClicked;

            lblCanhBao1.AutoSize = true;
            lblCanhBao1.ForeColor = Color.Red;
            lblCanhBao1.Location = new Point(200, 158);
            lblCanhBao1.Name = "lblCanhBao1";
            lblCanhBao1.TabIndex = 21;
            lblCanhBao1.Text = "Lưu ý: Kiểm tra kịch bản của tài khoản trước khi chạy";

            lblCanhBao2.AutoSize = true;
            lblCanhBao2.ForeColor = Color.Red;
            lblCanhBao2.Location = new Point(570, 158);
            lblCanhBao2.Name = "lblCanhBao2";
            lblCanhBao2.TabIndex = 22;
            lblCanhBao2.Text = "Lưu ý: Nếu không chọn kịch bản cho tài khoản thì auto sẽ chạy kịch bản đầu tiên";

            // Add all controls to panel4
            panel4.Controls.Add(radioButton1);
            panel4.Controls.Add(radioButton2);
            panel4.Controls.Add(radioButton3);
            panel4.Controls.Add(radioButton4);
            panel4.Controls.Add(lblSoLanLap);
            panel4.Controls.Add(nudSoLanLap);
            panel4.Controls.Add(lblLuot);
            panel4.Controls.Add(lblChoLuot);
            panel4.Controls.Add(nudChoLuotFrom);
            panel4.Controls.Add(lblDenLuot);
            panel4.Controls.Add(nudChoLuotTo);
            panel4.Controls.Add(lblPhutLuot);
            panel4.Controls.Add(checkBox1);
            panel4.Controls.Add(checkBox2);
            panel4.Controls.Add(nudTaiKhoanFrom);
            panel4.Controls.Add(lblDenTaiKhoan);
            panel4.Controls.Add(nudTaiKhoanTo);
            panel4.Controls.Add(lblPhutTaiKhoan);
            panel4.Controls.Add(checkBox3);
            panel4.Controls.Add(nudKichBanFrom);
            panel4.Controls.Add(lblDenKichBan);
            panel4.Controls.Add(nudKichBanTo);
            panel4.Controls.Add(lblPhutKichBan);
            panel4.Controls.Add(checkBox4);
            panel4.Controls.Add(lblThoiGianBatDau);
            panel4.Controls.Add(timepickerFrom);
            panel4.Controls.Add(lblDenNgay);
            panel4.Controls.Add(timepickerTo);
            panel4.Controls.Add(llbHuongDan);
            panel4.Controls.Add(lblCanhBao1);
            panel4.Controls.Add(lblCanhBao2);
            panel4.Controls.Add(input6);
            panel4.Controls.Add(button16);

            // hidden
            input6.Location = new Point(9999, 0);
            input6.Name = "input6";
            input6.Size = new Size(100, 23);
            input6.TabIndex = 98;

            button16.Location = new Point(9999, 0);
            button16.Name = "button16";
            button16.Size = new Size(100, 30);
            button16.TabIndex = 99;
            button16.Text = "button16";

            // ── virtualPanel ──
            virtualPanel.BackColor = Color.FromArgb(236, 240, 241);
            virtualPanel.Dock = DockStyle.Fill;
            virtualPanel.JustifyContent = AntdUI.TJustifyContent.SpaceEvenly;
            virtualPanel.Name = "virtualPanel";
            virtualPanel.Shadow = 20;
            virtualPanel.ShadowOpacityAnimation = true;
            virtualPanel.TabIndex = 12;
            virtualPanel.Waterfall = true;

            // ── fQuanLyKichBan ──
            BackColor = Color.FromArgb(236, 240, 241);
            ClientSize = new Size(1300, 720);
            Controls.Add(virtualPanel);
            Controls.Add(panel4);
            Controls.Add(windowBar);
            Font = new Font("Microsoft YaHei UI", 12F);
            ForeColor = Color.Black;
            MinimumSize = new Size(660, 400);
            Name = "fQuanLyKichBan";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "AntdUI Overview";

            windowBar.ResumeLayout(false);
            panel4.ResumeLayout(false);
            panel4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudSoLanLap).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudChoLuotFrom).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudChoLuotTo).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudTaiKhoanFrom).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudTaiKhoanTo).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudKichBanFrom).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudKichBanTo).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private AntdUI.PageHeader windowBar;
        private AntdUI.Button button1;
        private AntdUI.Input txt_search;
        private AntdUI.Button btn_mode;
        private AntdUI.Button btn_global;
        private AntdUI.Button btn_setting;
        private AntdUI.Panel panel4;
        private RadioButton radioButton1;
        private RadioButton radioButton2;
        private RadioButton radioButton3;
        private RadioButton radioButton4;
        private Label lblSoLanLap;
        private NumericUpDown nudSoLanLap;
        private Label lblLuot;
        private Label lblChoLuot;
        private NumericUpDown nudChoLuotFrom;
        private Label lblDenLuot;
        private NumericUpDown nudChoLuotTo;
        private Label lblPhutLuot;
        private CheckBox checkBox1;
        private CheckBox checkBox2;
        private NumericUpDown nudTaiKhoanFrom;
        private Label lblDenTaiKhoan;
        private NumericUpDown nudTaiKhoanTo;
        private Label lblPhutTaiKhoan;
        private CheckBox checkBox3;
        private NumericUpDown nudKichBanFrom;
        private Label lblDenKichBan;
        private NumericUpDown nudKichBanTo;
        private Label lblPhutKichBan;
        private CheckBox checkBox4;
        private Label lblThoiGianBatDau;
        private AntdUI.TimePicker timepickerFrom;
        private Label lblDenNgay;
        private AntdUI.TimePicker timepickerTo;
        private LinkLabel llbHuongDan;
        private Label lblCanhBao1;
        private Label lblCanhBao2;
        private AntdUI.VirtualPanel virtualPanel;
        private AntdUI.Input input6;
        private AntdUI.Button button16;
    }
}
