using System.ComponentModel;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions
{
    partial class fHDNghiGiaiLao
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
            pnlHeader = new Panel();
            button1 = new Button();
            D9929802 = new PictureBox();
            panel1 = new Panel();
            windowBar = new AntdUI.PageHeader();
            btn_mode = new AntdUI.Button();
            btn_global = new AntdUI.Button();
            btn_setting = new AntdUI.Button();
            BCAAD506 = new NumericUpDown();
            nudDelayFrom = new NumericUpDown();
            txtTenHanhDong = new TextBox();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            A52270B5 = new Label();
            btnCancel = new Button();
            btnSave = new Button();
            pnlHeader.SuspendLayout();
            ((ISupportInitialize)D9929802).BeginInit();
            panel1.SuspendLayout();
            windowBar.SuspendLayout();
            ((ISupportInitialize)BCAAD506).BeginInit();
            ((ISupportInitialize)nudDelayFrom).BeginInit();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlHeader.BackColor = Color.White;
            pnlHeader.Controls.Add(button1);
            pnlHeader.Controls.Add(D9929802);
            pnlHeader.Cursor = Cursors.SizeAll;
            pnlHeader.Location = new Point(0, 3);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(359, 31);
            pnlHeader.TabIndex = 9;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button1.Cursor = Cursors.Hand;
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button1.ForeColor = Color.White;
            button1.Location = new Point(328, 1);
            button1.Name = "button1";
            button1.Size = new Size(30, 30);
            button1.TabIndex = 77;
            button1.TextImageRelation = TextImageRelation.ImageBeforeText;
            button1.UseVisualStyleBackColor = true;
            // 
            // D9929802
            // 
            D9929802.Location = new Point(3, 2);
            D9929802.Name = "D9929802";
            D9929802.Size = new Size(34, 27);
            D9929802.SizeMode = PictureBoxSizeMode.Zoom;
            D9929802.TabIndex = 76;
            D9929802.TabStop = false;
            // 
            // panel1
            // 
            panel1.BackColor = Color.White;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(windowBar);
            panel1.Controls.Add(BCAAD506);
            panel1.Controls.Add(nudDelayFrom);
            panel1.Controls.Add(txtTenHanhDong);
            panel1.Controls.Add(label7);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(A52270B5);
            panel1.Controls.Add(btnCancel);
            panel1.Controls.Add(btnSave);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(362, 160);
            panel1.TabIndex = 0;
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
            windowBar.Size = new Size(360, 35);
            windowBar.SubFont = new Font("Microsoft Sans Serif", Facebook_Farm_NewFeed_PostStory.Utils.Design.FontScale.Caption, FontStyle.Regular, GraphicsUnit.Point, 0);
            windowBar.SubGap = 1;
            windowBar.SubText = "";
            windowBar.TabIndex = 119;
            windowBar.Text = "Cấu hình tương tác";
            windowBar.UseSystemStyleColor = true;
            windowBar.UseTextBold = false;
            // 
            // btn_mode
            // 
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Ghost = true;
            btn_mode.Icon = Properties.Resources.icons8_circle_16_Green;
            btn_mode.IconSvg = "";
            btn_mode.Location = new Point(280, 0);
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
            btn_global.Location = new Point(306, 0);
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
            btn_setting.Location = new Point(330, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(30, 35);
            btn_setting.TabIndex = 8;
            btn_setting.WaveSize = 0;
            // 
            // BCAAD506
            // 
            BCAAD506.Location = new Point(235, 80);
            BCAAD506.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            BCAAD506.Name = "BCAAD506";
            BCAAD506.Size = new Size(56, 23);
            BCAAD506.TabIndex = 4;
            BCAAD506.Value = new decimal(new int[] { 120, 0, 0, 0 });
            // 
            // nudDelayFrom
            // 
            nudDelayFrom.Location = new Point(132, 80);
            nudDelayFrom.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudDelayFrom.Name = "nudDelayFrom";
            nudDelayFrom.Size = new Size(56, 23);
            nudDelayFrom.TabIndex = 3;
            nudDelayFrom.Value = new decimal(new int[] { 60, 0, 0, 0 });
            // 
            // txtTenHanhDong
            // 
            txtTenHanhDong.Location = new Point(132, 49);
            txtTenHanhDong.Name = "txtTenHanhDong";
            txtTenHanhDong.Size = new Size(194, 23);
            txtTenHanhDong.TabIndex = 0;
            // 
            // label7
            // 
            label7.Location = new Point(189, 82);
            label7.Name = "label7";
            label7.Size = new Size(43, 16);
            label7.TabIndex = 38;
            label7.Text = "đến";
            label7.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(293, 82);
            label6.Name = "label6";
            label6.Size = new Size(30, 16);
            label6.TabIndex = 36;
            label6.Text = "giây";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(27, 82);
            label5.Name = "label5";
            label5.Size = new Size(89, 16);
            label5.TabIndex = 34;
            label5.Text = "Thời gian chờ:";
            // 
            // A52270B5
            // 
            A52270B5.AutoSize = true;
            A52270B5.Location = new Point(27, 52);
            A52270B5.Name = "A52270B5";
            A52270B5.Size = new Size(98, 16);
            A52270B5.TabIndex = 31;
            A52270B5.Text = "Tên hành động:";
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.Maroon;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Tahoma", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(189, 116);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(92, 29);
            btnCancel.TabIndex = 10;
            btnCancel.Text = "Đóng";
            btnCancel.UseVisualStyleBackColor = false;
            // 
            // btnSave
            // 
            btnSave.BackColor = Color.FromArgb(53, 120, 229);
            btnSave.Cursor = Cursors.Hand;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Font = new Font("Tahoma", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(82, 116);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(92, 29);
            btnSave.TabIndex = 9;
            btnSave.Text = "Thêm";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // fHDNghiGiaiLao
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(362, 160);
            Controls.Add(panel1);
            Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "fHDNghiGiaiLao";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Cấu hình tương tác";
            pnlHeader.ResumeLayout(false);
            ((ISupportInitialize)D9929802).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            windowBar.ResumeLayout(false);
            ((ISupportInitialize)BCAAD506).EndInit();
            ((ISupportInitialize)nudDelayFrom).EndInit();
            ResumeLayout(false);
        }
        internal Panel panel1;

        internal NumericUpDown BCAAD506;

        internal NumericUpDown nudDelayFrom;

        internal TextBox txtTenHanhDong;

        internal Label label7;

        internal Label label6;

        internal Label label5;

        internal Label A52270B5;

        internal Button btnCancel;

        internal Button btnSave;

        internal Panel pnlHeader;

        internal Button button1;

        internal PictureBox D9929802;

        #endregion

        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_mode;
        private AntdUI.Button btn_global;
        private AntdUI.Button btn_setting;
    }
}