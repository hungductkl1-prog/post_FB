namespace Facebook_Farm_NewFeed_PostStory
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            AntdUI.Tabs.StyleCard styleCard1 = new AntdUI.Tabs.StyleCard();
            windowBar = new AntdUI.PageHeader();
            btn_mode = new AntdUI.Button();
            btn_global = new AntdUI.Button();
            btn_setting = new AntdUI.Button();
            panel1 = new Panel();
            flowLayoutPanel1 = new SelectableFlowLayoutPanel();
            panel2 = new AntdUI.Panel();
            tabs3 = new AntdUI.Tabs();
            tabPageSettingDefault = new AntdUI.TabPage();
            panel5 = new Panel();
            panel8 = new Panel();
            flowLayoutPanel3 = new AntdUI.In.FlowLayoutPanel();
            panel9 = new Panel();
            button4 = new AntdUI.Button();
            button3 = new AntdUI.Button();
            checkBox1 = new CheckBox();
            panel7 = new Panel();
            button2 = new AntdUI.Button();
            label5 = new Label();
            button1 = new AntdUI.Button();
            panel4 = new Panel();
            slider2 = new AntdUI.Slider();
            slider1 = new AntdUI.Slider();
            label2 = new Label();
            label4 = new Label();
            flowLayoutPanel2 = new AntdUI.In.FlowLayoutPanel();
            tabPageSettingAndroid = new AntdUI.TabPage();
            panel3 = new AntdUI.Panel();
            button9 = new AntdUI.Button();
            windowBar.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            tabs3.SuspendLayout();
            tabPageSettingDefault.SuspendLayout();
            panel5.SuspendLayout();
            panel8.SuspendLayout();
            panel9.SuspendLayout();
            panel7.SuspendLayout();
            panel4.SuspendLayout();
            panel3.SuspendLayout();
            SuspendLayout();
            // 
            // windowBar
            // 
            windowBar.BackColor = Color.White;
            windowBar.BackgroundImageLayout = ImageLayout.Stretch;
            windowBar.CloseSize = 30;
            windowBar.Controls.Add(btn_mode);
            windowBar.Controls.Add(btn_global);
            windowBar.Controls.Add(btn_setting);
            windowBar.Cursor = Cursors.Hand;
            windowBar.DividerMargin = 1;
            windowBar.Dock = DockStyle.Top;
            windowBar.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            windowBar.ForeColor = Color.Black;
            windowBar.Icon = Properties.Resources.logo_lamtool_v3_dark_16;
            windowBar.Location = new Point(0, 0);
            windowBar.MDI = true;
            windowBar.Name = "windowBar";
            windowBar.ShowIcon = true;
            windowBar.Size = new Size(997, 35);
            windowBar.SubFont = new Font("Microsoft Sans Serif", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            windowBar.SubGap = 1;
            windowBar.SubText = "v18.12.08.2025";
            windowBar.TabIndex = 8;
            windowBar.Text = "QN Phone Farm";
            windowBar.UseSystemStyleColor = true;
            windowBar.UseTextBold = false;
            // 
            // btn_mode
            // 
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Ghost = true;
            btn_mode.Icon = Properties.Resources.icons8_circle_16_Green;
            btn_mode.IconSvg = "";
            btn_mode.Location = new Point(917, 0);
            btn_mode.Name = "btn_mode";
            btn_mode.Radius = 0;
            btn_mode.Size = new Size(26, 35);
            btn_mode.TabIndex = 6;
            btn_mode.ToggleIconSvg = "MoonOutlined";
            btn_mode.WaveSize = 0;
            // 
            // btn_global
            // 
            btn_global.Dock = DockStyle.Right;
            btn_global.Ghost = true;
            btn_global.Icon = Properties.Resources.icons8_circle_16_Yellow;
            btn_global.IconSvg = "";
            btn_global.Location = new Point(943, 0);
            btn_global.Name = "btn_global";
            btn_global.Radius = 0;
            btn_global.Size = new Size(24, 35);
            btn_global.TabIndex = 7;
            btn_global.WaveSize = 0;
            // 
            // btn_setting
            // 
            btn_setting.Dock = DockStyle.Right;
            btn_setting.Ghost = true;
            btn_setting.Icon = Properties.Resources.icons8_circle_16_Red;
            btn_setting.IconSvg = "";
            btn_setting.Location = new Point(967, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(30, 35);
            btn_setting.TabIndex = 8;
            btn_setting.WaveSize = 0;
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(235, 238, 241);
            panel1.Controls.Add(flowLayoutPanel1);
            panel1.Controls.Add(panel2);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 35);
            panel1.Name = "panel1";
            panel1.Padding = new Padding(15);
            panel1.Size = new Size(997, 530);
            panel1.TabIndex = 9;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.AutoScroll = true;
            flowLayoutPanel1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.Location = new Point(334, 15);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Padding = new Padding(10, 0, 0, 0);
            flowLayoutPanel1.Size = new Size(648, 500);
            flowLayoutPanel1.TabIndex = 1;
            // 
            // panel2
            // 
            panel2.BadgeSize = 2.2F;
            panel2.Controls.Add(tabs3);
            panel2.Controls.Add(panel3);
            panel2.Dock = DockStyle.Left;
            panel2.Location = new Point(15, 15);
            panel2.Name = "panel2";
            panel2.Size = new Size(319, 500);
            panel2.TabIndex = 0;
            panel2.Text = "panel2";
            // 
            // tabs3
            // 
            tabs3.BackColor = Color.White;
            tabs3.BadgeAlign = AntdUI.TAlign.None;
            tabs3.Controls.Add(tabPageSettingDefault);
            tabs3.Controls.Add(tabPageSettingAndroid);
            tabs3.Dock = DockStyle.Fill;
            tabs3.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold);
            tabs3.Gap = 12;
            tabs3.Location = new Point(0, 74);
            tabs3.Name = "tabs3";
            tabs3.Pages.Add(tabPageSettingDefault);
            tabs3.Pages.Add(tabPageSettingAndroid);
            tabs3.RightToLeft = RightToLeft.No;
            tabs3.Size = new Size(319, 426);
            tabs3.Style = styleCard1;
            tabs3.TabIndex = 1;
            tabs3.Type = AntdUI.TabType.Card;
            tabs3.TypExceed = AntdUI.TabTypExceed.None;
            // 
            // tabPageSettingDefault
            // 
            tabPageSettingDefault.Controls.Add(panel5);
            tabPageSettingDefault.Controls.Add(panel4);
            tabPageSettingDefault.Controls.Add(flowLayoutPanel2);
            tabPageSettingDefault.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            tabPageSettingDefault.IconSvg = "SettingOutlined";
            tabPageSettingDefault.Location = new Point(3, 35);
            tabPageSettingDefault.Name = "tabPageSettingDefault";
            tabPageSettingDefault.Padding = new Padding(10);
            tabPageSettingDefault.Size = new Size(313, 388);
            tabPageSettingDefault.TabIndex = 0;
            tabPageSettingDefault.Text = "Cài đặt";
            // 
            // panel5
            // 
            panel5.Controls.Add(panel8);
            panel5.Controls.Add(panel7);
            panel5.Dock = DockStyle.Fill;
            panel5.Location = new Point(10, 179);
            panel5.Name = "panel5";
            panel5.Size = new Size(293, 199);
            panel5.TabIndex = 2;
            // 
            // panel8
            // 
            panel8.Controls.Add(flowLayoutPanel3);
            panel8.Controls.Add(panel9);
            panel8.Dock = DockStyle.Top;
            panel8.Location = new Point(0, 28);
            panel8.Name = "panel8";
            panel8.Size = new Size(293, 89);
            panel8.TabIndex = 1;
            // 
            // flowLayoutPanel3
            // 
            flowLayoutPanel3.Dock = DockStyle.Fill;
            flowLayoutPanel3.Location = new Point(0, 27);
            flowLayoutPanel3.Name = "flowLayoutPanel3";
            flowLayoutPanel3.Size = new Size(293, 62);
            flowLayoutPanel3.TabIndex = 1;
            // 
            // panel9
            // 
            panel9.Controls.Add(button4);
            panel9.Controls.Add(button3);
            panel9.Controls.Add(checkBox1);
            panel9.Dock = DockStyle.Top;
            panel9.Location = new Point(0, 0);
            panel9.Name = "panel9";
            panel9.Size = new Size(293, 27);
            panel9.TabIndex = 0;
            // 
            // button4
            // 
            button4.DefaultBack = Color.White;
            button4.DisplayStyle = AntdUI.TButtonDisplayStyle.Text;
            button4.Dock = DockStyle.Right;
            button4.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button4.ForeColor = Color.Green;
            button4.IconHoverSvg = "";
            button4.IconRatio = 0.9F;
            button4.IconSvg = "";
            button4.Location = new Point(81, 0);
            button4.Name = "button4";
            button4.Radius = 10;
            button4.Size = new Size(100, 27);
            button4.TabIndex = 12;
            button4.Text = "Hiển thị tất cả";
            // 
            // button3
            // 
            button3.DefaultBack = Color.White;
            button3.DisplayStyle = AntdUI.TButtonDisplayStyle.Text;
            button3.Dock = DockStyle.Right;
            button3.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button3.ForeColor = Color.Green;
            button3.IconHoverSvg = "";
            button3.IconRatio = 0.9F;
            button3.IconSvg = "";
            button3.Location = new Point(181, 0);
            button3.Name = "button3";
            button3.Radius = 10;
            button3.Size = new Size(112, 27);
            button3.TabIndex = 11;
            button3.Text = "Hiển thị đã chọn";
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Dock = DockStyle.Left;
            checkBox1.Location = new Point(0, 0);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(63, 27);
            checkBox1.TabIndex = 0;
            checkBox1.Text = "Tất cả";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // panel7
            // 
            panel7.Controls.Add(button2);
            panel7.Controls.Add(label5);
            panel7.Controls.Add(button1);
            panel7.Dock = DockStyle.Top;
            panel7.Location = new Point(0, 0);
            panel7.Name = "panel7";
            panel7.Size = new Size(293, 28);
            panel7.TabIndex = 0;
            // 
            // button2
            // 
            button2.DefaultBack = Color.White;
            button2.Dock = DockStyle.Right;
            button2.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button2.ForeColor = Color.Green;
            button2.IconHoverSvg = "";
            button2.IconRatio = 0.9F;
            button2.IconSvg = "PlusSquareOutlined";
            button2.Location = new Point(202, 0);
            button2.Name = "button2";
            button2.Radius = 10;
            button2.Size = new Size(91, 28);
            button2.TabIndex = 10;
            button2.Text = "Thêm thẻ";
            // 
            // label5
            // 
            label5.Dock = DockStyle.Left;
            label5.Location = new Point(31, 0);
            label5.Name = "label5";
            label5.Size = new Size(98, 28);
            label5.TabIndex = 9;
            label5.Text = "Thẻ điện thoại";
            label5.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // button1
            // 
            button1.DefaultBack = Color.White;
            button1.Dock = DockStyle.Left;
            button1.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            button1.ForeColor = Color.Black;
            button1.IconHoverSvg = "";
            button1.IconRatio = 0.9F;
            button1.IconSvg = "CaretDownOutlined";
            button1.IconToggleAnimation = 10;
            button1.Location = new Point(0, 0);
            button1.Name = "button1";
            button1.Radius = 10;
            button1.Size = new Size(31, 28);
            button1.TabIndex = 8;
            button1.WaveSize = 3;
            // 
            // panel4
            // 
            panel4.Controls.Add(slider2);
            panel4.Controls.Add(slider1);
            panel4.Controls.Add(label2);
            panel4.Controls.Add(label4);
            panel4.Dock = DockStyle.Top;
            panel4.Location = new Point(10, 121);
            panel4.Name = "panel4";
            panel4.Size = new Size(293, 58);
            panel4.TabIndex = 1;
            // 
            // slider2
            // 
            slider2.Anchor = AnchorStyles.Left;
            slider2.Location = new Point(109, 32);
            slider2.MaxValue = 840;
            slider2.MinValue = 192;
            slider2.Name = "slider2";
            slider2.ShowValue = true;
            slider2.Size = new Size(171, 21);
            slider2.TabIndex = 4;
            slider2.Value = 192;
            slider2.ValueChanged += slider2_ValueChanged;
            // 
            // slider1
            // 
            slider1.Anchor = AnchorStyles.Left;
            slider1.Location = new Point(109, 6);
            slider1.MaxValue = 1240;
            slider1.MinValue = 480;
            slider1.Name = "slider1";
            slider1.ShowValue = true;
            slider1.Size = new Size(171, 21);
            slider1.TabIndex = 2;
            slider1.Value = 480;
            slider1.ValueChanged += slider1_ValueChanged;
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Left;
            label2.AutoSize = true;
            label2.Location = new Point(8, 7);
            label2.Name = "label2";
            label2.Size = new Size(95, 17);
            label2.TabIndex = 0;
            label2.Text = "Kích thước lớn";
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.Left;
            label4.AutoSize = true;
            label4.Location = new Point(8, 32);
            label4.Name = "label4";
            label4.Size = new Size(100, 17);
            label4.TabIndex = 3;
            label4.Text = "Kích thước nhỏ";
            // 
            // flowLayoutPanel2
            // 
            flowLayoutPanel2.Dock = DockStyle.Top;
            flowLayoutPanel2.Location = new Point(10, 10);
            flowLayoutPanel2.Name = "flowLayoutPanel2";
            flowLayoutPanel2.Size = new Size(293, 111);
            flowLayoutPanel2.TabIndex = 0;
            // 
            // tabPageSettingAndroid
            // 
            tabPageSettingAndroid.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            tabPageSettingAndroid.IconSvg = "AndroidOutlined";
            tabPageSettingAndroid.Location = new Point(-995, -404);
            tabPageSettingAndroid.Name = "tabPageSettingAndroid";
            tabPageSettingAndroid.Size = new Size(995, 404);
            tabPageSettingAndroid.TabIndex = 1;
            tabPageSettingAndroid.Text = "OTG";
            // 
            // panel3
            // 
            panel3.BackgroundImage = Properties.Resources.Gemini_Generated_Image_8w7n8o8w7n8o8w7n;
            panel3.Controls.Add(button9);
            panel3.Dock = DockStyle.Top;
            panel3.Location = new Point(0, 0);
            panel3.Name = "panel3";
            panel3.Size = new Size(319, 74);
            panel3.TabIndex = 0;
            panel3.Text = "panel3";
            // 
            // button9
            // 
            button9.Cursor = Cursors.Hand;
            button9.DefaultBack = Color.FromArgb(53, 119, 92);
            button9.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            button9.ForeColor = Color.White;
            button9.IconHoverSvg = "";
            button9.IconRatio = 0.9F;
            button9.IconSvg = "DoubleLeftOutlined";
            button9.Location = new Point(3, 3);
            button9.Name = "button9";
            button9.Radius = 10;
            button9.Size = new Size(30, 25);
            button9.TabIndex = 7;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(997, 565);
            Controls.Add(panel1);
            Controls.Add(windowBar);
            Name = "Form1";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            WindowState = FormWindowState.Maximized;
            FormClosing += Form1_FormClosing;
            windowBar.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            tabs3.ResumeLayout(false);
            tabPageSettingDefault.ResumeLayout(false);
            panel5.ResumeLayout(false);
            panel8.ResumeLayout(false);
            panel9.ResumeLayout(false);
            panel9.PerformLayout();
            panel7.ResumeLayout(false);
            panel4.ResumeLayout(false);
            panel4.PerformLayout();
            panel3.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_mode;
        private AntdUI.Button btn_global;
        private AntdUI.Button btn_setting;
        private Panel panel1;
        private AntdUI.Panel panel2;
        private AntdUI.Panel panel3;
        private AntdUI.Button button9;
        private AntdUI.Tabs tabs3;
        private AntdUI.TabPage tabPageSettingDefault;
        private AntdUI.TabPage tabPageSettingAndroid;
        private SelectableFlowLayoutPanel flowLayoutPanel1;
        private AntdUI.In.FlowLayoutPanel flowLayoutPanel2;
        private Panel panel4;
        private Label label2;
        private AntdUI.Slider slider1;
        private Label label4;
        private AntdUI.Slider slider2;
        private Panel panel5;
        private Panel panel7;
        private Label label5;
        private AntdUI.Button button1;
        private AntdUI.Button button2;
        private Panel panel8;
        private Panel panel9;
        private CheckBox checkBox1;
        private AntdUI.Button button4;
        private AntdUI.Button button3;
        private AntdUI.In.FlowLayoutPanel flowLayoutPanel3;
    }
}