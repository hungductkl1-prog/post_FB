namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    partial class fAddUsercontrol
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
            panel1 = new Panel();
            windowBar.SuspendLayout();
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
            windowBar.Size = new Size(1000, 36);
            windowBar.SubText = "";
            windowBar.TabIndex = 9;
            windowBar.Text = "Cấu hình thêm tài khoản vào nhóm";
            windowBar.UseSystemStyleColor = true;
            windowBar.UseTextBold = false;
            // 
            // btn_mode
            // 
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Ghost = true;
            btn_mode.Icon = Properties.Resources.icons8_circle_16_Green;
            btn_mode.IconSvg = "";
            btn_mode.Location = new Point(920, 0);
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
            btn_global.Location = new Point(946, 0);
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
            btn_setting.Location = new Point(970, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(30, 36);
            btn_setting.TabIndex = 9;
            btn_setting.WaveSize = 0;
            btn_setting.Click += btn_setting_Click;
            // 
            // panel1
            // 
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 36);
            panel1.Name = "panel1";
            panel1.Size = new Size(1000, 564);
            panel1.TabIndex = 10;
            // 
            // fAddUsercontrol
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            BackColor = Color.FromArgb(236, 240, 241);
            ClientSize = new Size(1000, 600);
            Controls.Add(panel1);
            Controls.Add(windowBar);
            MinimumSize = new Size(1000, 600);
            Name = "fAddUsercontrol";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "fFolder";
            windowBar.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_mode;
        private AntdUI.Dropdown btn_global;
        private AntdUI.Button btn_setting;
        private Panel panel1;
    }
}