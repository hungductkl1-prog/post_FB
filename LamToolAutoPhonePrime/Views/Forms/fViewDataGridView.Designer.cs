namespace LamToolAutoPhonePrime.Views.Forms
{
    partial class fViewDataGridView
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
            windowBar = new AntdUI.PageHeader();
            btn_mode = new AntdUI.Button();
            btn_global = new AntdUI.Dropdown();
            btn_setting = new AntdUI.Button();
            panel1 = new AntdUI.Panel();
            button1 = new AntdUI.Button();
            button9 = new AntdUI.Button();
            panel2 = new AntdUI.Panel();
            button2 = new AntdUI.Button();
            button3 = new AntdUI.Button();
            btnShowAll = new AntdUI.Button();
            btnShowOptimal = new AntdUI.Button();
            label2 = new Label();
            flowLayoutPanel1 = new FlowLayoutPanel();
            windowBar.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
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
            windowBar.DividerShow = true;
            windowBar.Dock = DockStyle.Top;
            windowBar.Font = new Font("Microsoft YaHei UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            windowBar.ForeColor = Color.Black;
            windowBar.Location = new Point(0, 0);
            windowBar.MDI = true;
            windowBar.Name = "windowBar";
            windowBar.Size = new Size(918, 36);
            windowBar.SubText = "";
            windowBar.TabIndex = 9;
            windowBar.Text = "Cấu hình hiển thị bảng";
            windowBar.UseSystemStyleColor = true;
            windowBar.UseTextBold = false;
            // 
            // btn_mode
            // 
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Ghost = true;
            btn_mode.Icon = Properties.Resources.icons8_circle_16_Green;
            btn_mode.IconSvg = "";
            btn_mode.Location = new Point(838, 0);
            btn_mode.Name = "btn_mode";
            btn_mode.Radius = 0;
            btn_mode.Size = new Size(26, 36);
            btn_mode.TabIndex = 11;
            btn_mode.ToggleIconSvg = "MoonOutlined";
            btn_mode.WaveSize = 0;
            // 
            // btn_global
            // 
            btn_global.Dock = DockStyle.Right;
            btn_global.Ghost = true;
            btn_global.Icon = Properties.Resources.icons8_circle_16_Yellow;
            btn_global.IconSvg = "";
            btn_global.Location = new Point(864, 0);
            btn_global.Name = "btn_global";
            btn_global.Radius = 0;
            btn_global.Size = new Size(24, 36);
            btn_global.TabIndex = 10;
            btn_global.WaveSize = 0;
            // 
            // btn_setting
            // 
            btn_setting.Dock = DockStyle.Right;
            btn_setting.Ghost = true;
            btn_setting.Icon = Properties.Resources.icons8_circle_16_Red;
            btn_setting.IconSvg = "";
            btn_setting.Location = new Point(888, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(30, 36);
            btn_setting.TabIndex = 9;
            btn_setting.WaveSize = 0;
            btn_setting.Click += btn_setting_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(button1);
            panel1.Controls.Add(button9);
            panel1.Controls.Add(btnShowAll);
            panel1.Controls.Add(btnShowOptimal);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 314);
            panel1.Name = "panel1";
            panel1.padding = new Padding(12);
            panel1.Radius = 12;
            panel1.Size = new Size(918, 89);
            panel1.TabIndex = 10;
            panel1.Text = "panel1";
            // 
            // button1 (Đóng)
            //
            button1.Anchor = AnchorStyles.Bottom;
            button1.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button1.IconRatio = 1F;
            button1.IconSvg = "CloseOutlined";
            button1.IconToggleAnimation = 400;
            button1.Location = new Point(634, 23);
            button1.Name = "button1";
            button1.Shape = AntdUI.TShape.Round;
            button1.Size = new Size(139, 42);
            button1.TabIndex = 16;
            button1.Text = "Đóng";
            button1.Type = AntdUI.TTypeMini.Error;
            button1.Click += button1_Click;
            //
            // button9 (Lưu)
            //
            button9.Anchor = AnchorStyles.Bottom;
            button9.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button9.IconRatio = 1.1F;
            button9.IconSvg = "SaveOutlined";
            button9.IconToggleAnimation = 400;
            button9.Location = new Point(485, 23);
            button9.Name = "button9";
            button9.Shape = AntdUI.TShape.Round;
            button9.Size = new Size(131, 42);
            button9.TabIndex = 15;
            button9.Text = "Lưu";
            button9.Type = AntdUI.TTypeMini.Success;
            button9.Click += button9_Click;
            // 
            // panel2
            // 
            panel2.Controls.Add(flowLayoutPanel1);
            panel2.Controls.Add(label2);
            panel2.Controls.Add(button2);
            panel2.Controls.Add(button3);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(0, 36);
            panel2.Name = "panel2";
            panel2.padding = new Padding(12);
            panel2.Padding = new Padding(20);
            panel2.Radius = 12;
            panel2.Size = new Size(918, 278);
            panel2.TabIndex = 11;
            panel2.Text = "panel2";
            // 
            // button2
            //
            button2.Anchor = AnchorStyles.Bottom;
            button2.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button2.IconRatio = 1F;
            button2.IconSvg = "CloseOutlined";
            button2.IconToggleAnimation = 400;
            button2.Location = new Point(677, 281);
            button2.Name = "button2";
            button2.Shape = AntdUI.TShape.Round;
            button2.Size = new Size(139, 42);
            button2.TabIndex = 16;
            button2.Text = "Đóng";
            button2.Type = AntdUI.TTypeMini.Error;
            button2.Click += button1_Click;
            //
            // button3
            //
            button3.Anchor = AnchorStyles.Bottom;
            button3.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button3.IconRatio = 1.1F;
            button3.IconSvg = "SaveOutlined";
            button3.IconToggleAnimation = 400;
            button3.Location = new Point(528, 281);
            button3.Name = "button3";
            button3.Shape = AntdUI.TShape.Round;
            button3.Size = new Size(131, 42);
            button3.TabIndex = 15;
            button3.Text = "Lưu";
            button3.Type = AntdUI.TTypeMini.Success;
            button3.Click += button9_Click;
            //
            // btnShowAll
            //
            btnShowAll.Anchor = AnchorStyles.Bottom;
            btnShowAll.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnShowAll.IconRatio = 1F;
            btnShowAll.IconSvg = "AppstoreOutlined";
            btnShowAll.IconToggleAnimation = 400;
            btnShowAll.Location = new Point(230, 23);
            btnShowAll.Name = "btnShowAll";
            btnShowAll.Shape = AntdUI.TShape.Round;
            btnShowAll.Size = new Size(139, 42);
            btnShowAll.TabIndex = 17;
            btnShowAll.Text = "Hiển thị tất cả";
            btnShowAll.Type = AntdUI.TTypeMini.Primary;
            btnShowAll.Click += btnShowAll_Click;
            //
            // btnShowOptimal
            //
            btnShowOptimal.Anchor = AnchorStyles.Bottom;
            btnShowOptimal.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnShowOptimal.IconRatio = 1F;
            btnShowOptimal.IconSvg = "FilterOutlined";
            btnShowOptimal.IconToggleAnimation = 400;
            btnShowOptimal.Location = new Point(81, 23);
            btnShowOptimal.Name = "btnShowOptimal";
            btnShowOptimal.Shape = AntdUI.TShape.Round;
            btnShowOptimal.Size = new Size(139, 42);
            btnShowOptimal.TabIndex = 18;
            btnShowOptimal.Text = "Hiển thị tối ưu";
            btnShowOptimal.Type = AntdUI.TTypeMini.Warn;
            btnShowOptimal.Click += btnShowOptimal_Click;
            // 
            // label2
            // 
            label2.BackColor = Color.White;
            label2.Dock = DockStyle.Top;
            label2.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.DodgerBlue;
            label2.Location = new Point(20, 20);
            label2.Name = "label2";
            label2.Size = new Size(878, 29);
            label2.TabIndex = 18;
            label2.Text = "Vui lòng chọn những cột cần hiển thị";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.BackColor = Color.White;
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.Location = new Point(20, 49);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(878, 209);
            flowLayoutPanel1.TabIndex = 19;
            // 
            // fViewDataGridView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            BackColor = Color.FromArgb(236, 240, 241);
            ClientSize = new Size(918, 403);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Controls.Add(windowBar);
            MaximumSize = new Size(918, 403);
            MinimumSize = new Size(918, 403);
            Name = "fViewDataGridView";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "fFolder";
            windowBar.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_mode;
        private AntdUI.Dropdown btn_global;
        private AntdUI.Button btn_setting;
        private AntdUI.Panel panel1;
        private AntdUI.Button button9;
        private AntdUI.Button button1;
        private AntdUI.Panel panel2;
        private AntdUI.Button button2;
        private AntdUI.Button button3;
        private AntdUI.Button btnShowAll;
        private AntdUI.Button btnShowOptimal;
        private Label label2;
        private FlowLayoutPanel flowLayoutPanel1;
    }
}