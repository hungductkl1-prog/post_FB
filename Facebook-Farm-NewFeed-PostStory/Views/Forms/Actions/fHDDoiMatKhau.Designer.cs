using System.ComponentModel;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions
{
    partial class fHDDoiMatKhau
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
            A11EA82A = new Button();
            B9804D95 = new PictureBox();
            panel1 = new Panel();
            panel2 = new Panel();
            label4 = new Label();
            nudInteractTo = new NumericUpDown();
            label3 = new Label();
            nudInteractFrom = new NumericUpDown();
            windowBar = new AntdUI.PageHeader();
            btn_mode = new AntdUI.Button();
            btn_global = new AntdUI.Button();
            btn_setting = new AntdUI.Button();
            B197EAA2 = new CheckBox();
            A12E5D8C = new RadioButton();
            rbMatKhauRandom = new RadioButton();
            txtMatKhauChiDinh = new TextBox();
            lblMatKhauChiDinh = new Label();
            btnHelpMatKhau = new Button();
            ckbAccountCenter = new CheckBox();
            lnkCauHinhAC = new LinkLabel();
            txtTenHanhDong = new TextBox();
            label1 = new Label();
            btnCancel = new Button();
            btnSave = new Button();
            pnlHeader.SuspendLayout();
            ((ISupportInitialize)B9804D95).BeginInit();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            ((ISupportInitialize)nudInteractTo).BeginInit();
            ((ISupportInitialize)nudInteractFrom).BeginInit();
            windowBar.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlHeader.BackColor = Color.White;
            pnlHeader.Controls.Add(A11EA82A);
            pnlHeader.Controls.Add(B9804D95);
            pnlHeader.Cursor = Cursors.SizeAll;
            pnlHeader.Location = new Point(0, 3);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(457, 31);
            pnlHeader.TabIndex = 9;
            // 
            // A11EA82A
            // 
            A11EA82A.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            A11EA82A.Cursor = Cursors.Hand;
            A11EA82A.FlatAppearance.BorderSize = 0;
            A11EA82A.FlatStyle = FlatStyle.Flat;
            A11EA82A.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            A11EA82A.ForeColor = Color.White;
            A11EA82A.Location = new Point(426, 1);
            A11EA82A.Name = "A11EA82A";
            A11EA82A.Size = new Size(30, 30);
            A11EA82A.TabIndex = 77;
            A11EA82A.TextImageRelation = TextImageRelation.ImageBeforeText;
            A11EA82A.UseVisualStyleBackColor = true;
            // 
            // B9804D95
            // 
            B9804D95.Location = new Point(3, 2);
            B9804D95.Name = "B9804D95";
            B9804D95.Size = new Size(34, 27);
            B9804D95.SizeMode = PictureBoxSizeMode.Zoom;
            B9804D95.TabIndex = 76;
            B9804D95.TabStop = false;
            // 
            // panel1
            // 
            panel1.BackColor = Color.White;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(panel2);
            panel1.Controls.Add(windowBar);
            panel1.Controls.Add(B197EAA2);
            panel1.Controls.Add(A12E5D8C);
            panel1.Controls.Add(rbMatKhauRandom);
            panel1.Controls.Add(txtMatKhauChiDinh);
            panel1.Controls.Add(lblMatKhauChiDinh);
            panel1.Controls.Add(btnHelpMatKhau);
            panel1.Controls.Add(ckbAccountCenter);
            panel1.Controls.Add(lnkCauHinhAC);
            panel1.Controls.Add(txtTenHanhDong);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(btnCancel);
            panel1.Controls.Add(btnSave);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(459, 296);
            panel1.TabIndex = 0;
            //
            // panel2
            // 
            panel2.Controls.Add(label4);
            panel2.Controls.Add(nudInteractFrom);
            panel2.Location = new Point(140, 102);
            panel2.Name = "panel2";
            panel2.Size = new Size(243, 23);
            panel2.TabIndex = 190;
            // 
            // label4
            // 
            label4.Dock = DockStyle.Left;
            label4.Location = new Point(56, 0);
            label4.Name = "label4";
            label4.Size = new Size(180, 23);
            label4.TabIndex = 41;
            label4.Text = "ký tự ngẫu nhiên";
            label4.TextAlign = ContentAlignment.MiddleLeft;
            //
            // nudInteractTo (hidden)
            //
            nudInteractTo.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            nudInteractTo.Minimum = new decimal(new int[] { 6, 0, 0, 0 });
            nudInteractTo.Name = "nudInteractTo";
            nudInteractTo.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // label3 (hidden)
            //
            label3.Name = "label3";
            label3.TabIndex = 39;
            label3.Text = "đến";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // nudInteractFrom
            // 
            nudInteractFrom.Dock = DockStyle.Left;
            nudInteractFrom.Location = new Point(0, 0);
            nudInteractFrom.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            nudInteractFrom.Minimum = new decimal(new int[] { 6, 0, 0, 0 });
            nudInteractFrom.Name = "nudInteractFrom";
            nudInteractFrom.Size = new Size(56, 23);
            nudInteractFrom.TabIndex = 4;
            nudInteractFrom.Value = new decimal(new int[] { 10, 0, 0, 0 });
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
            windowBar.Size = new Size(457, 35);
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
            btn_mode.Location = new Point(374, 0);
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
            btn_global.Location = new Point(400, 0);
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
            btn_setting.Location = new Point(424, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(30, 35);
            btn_setting.TabIndex = 8;
            btn_setting.WaveSize = 0;
            // 
            // B197EAA2
            // 
            B197EAA2.AutoSize = true;
            B197EAA2.Cursor = Cursors.Hand;
            B197EAA2.Location = new Point(44, 222);
            B197EAA2.Name = "B197EAA2";
            B197EAA2.Size = new Size(144, 20);
            B197EAA2.TabIndex = 114;
            B197EAA2.Text = "Đăng xuất thiết bị cũ";
            B197EAA2.UseVisualStyleBackColor = true;
            // 
            // A12E5D8C
            // 
            A12E5D8C.AutoSize = true;
            A12E5D8C.Cursor = Cursors.Hand;
            A12E5D8C.Location = new Point(44, 140);
            A12E5D8C.Name = "A12E5D8C";
            A12E5D8C.Size = new Size(80, 20);
            A12E5D8C.TabIndex = 47;
            A12E5D8C.Text = "Tự nhập";
            A12E5D8C.UseVisualStyleBackColor = true;
            // 
            // rbMatKhauRandom
            // 
            rbMatKhauRandom.AutoSize = true;
            rbMatKhauRandom.Checked = true;
            rbMatKhauRandom.Cursor = Cursors.Hand;
            rbMatKhauRandom.Location = new Point(44, 104);
            rbMatKhauRandom.Name = "rbMatKhauRandom";
            rbMatKhauRandom.Size = new Size(80, 20);
            rbMatKhauRandom.TabIndex = 45;
            rbMatKhauRandom.TabStop = true;
            rbMatKhauRandom.Text = "Random";
            rbMatKhauRandom.UseVisualStyleBackColor = true;
            //
            // txtMatKhauChiDinh
            //
            txtMatKhauChiDinh.Location = new Point(140, 140);
            txtMatKhauChiDinh.Name = "txtMatKhauChiDinh";
            txtMatKhauChiDinh.Size = new Size(213, 23);
            txtMatKhauChiDinh.TabIndex = 48;
            //
            // lblMatKhauChiDinh
            //
            lblMatKhauChiDinh.AutoSize = true;
            lblMatKhauChiDinh.ForeColor = Color.Gray;
            lblMatKhauChiDinh.Location = new Point(140, 121);
            lblMatKhauChiDinh.Name = "lblMatKhauChiDinh";
            lblMatKhauChiDinh.Size = new Size(140, 16);
            lblMatKhauChiDinh.TabIndex = 49;
            lblMatKhauChiDinh.Text = "Nhập Mật Khẩu (0)";
            lblMatKhauChiDinh.Visible = false;
            //
            // btnHelpMatKhau
            //
            btnHelpMatKhau.Cursor = Cursors.Help;
            btnHelpMatKhau.Font = new Font("Tahoma", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnHelpMatKhau.Location = new Point(357, 140);
            btnHelpMatKhau.Name = "btnHelpMatKhau";
            btnHelpMatKhau.Size = new Size(23, 23);
            btnHelpMatKhau.TabIndex = 50;
            btnHelpMatKhau.Text = "?";
            btnHelpMatKhau.UseVisualStyleBackColor = true;
            //
            // ckbAccountCenter
            //
            ckbAccountCenter.AutoSize = true;
            ckbAccountCenter.Cursor = Cursors.Hand;
            ckbAccountCenter.Location = new Point(44, 188);
            ckbAccountCenter.Name = "ckbAccountCenter";
            ckbAccountCenter.Size = new Size(160, 20);
            ckbAccountCenter.TabIndex = 51;
            ckbAccountCenter.Text = "Sử dụng Account Center";
            ckbAccountCenter.UseVisualStyleBackColor = true;
            //
            // lnkCauHinhAC
            //
            lnkCauHinhAC.AutoSize = true;
            lnkCauHinhAC.Cursor = Cursors.Hand;
            lnkCauHinhAC.Location = new Point(207, 190);
            lnkCauHinhAC.Name = "lnkCauHinhAC";
            lnkCauHinhAC.Size = new Size(56, 16);
            lnkCauHinhAC.TabIndex = 52;
            lnkCauHinhAC.TabStop = true;
            lnkCauHinhAC.Text = "Cấu hình";
            //
            // txtTenHanhDong
            // 
            txtTenHanhDong.Location = new Point(131, 49);
            txtTenHanhDong.Name = "txtTenHanhDong";
            txtTenHanhDong.Size = new Size(249, 23);
            txtTenHanhDong.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(27, 52);
            label1.Name = "label1";
            label1.Size = new Size(98, 16);
            label1.TabIndex = 31;
            label1.Text = "Tên hành động:";
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom;
            btnCancel.BackColor = Color.Maroon;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Tahoma", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(236, 254);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(92, 29);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Đóng";
            btnCancel.UseVisualStyleBackColor = false;
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Bottom;
            btnSave.BackColor = Color.FromArgb(53, 120, 229);
            btnSave.Cursor = Cursors.Hand;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Font = new Font("Tahoma", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(129, 254);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(92, 29);
            btnSave.TabIndex = 6;
            btnSave.Text = "Thêm";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // fHDDoiMatKhau
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(459, 296);
            Controls.Add(panel1);
            Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "fHDDoiMatKhau";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Cấu hình tương tác";
            rbMatKhauRandom.CheckedChanged += ckbDefault_CheckedChanged;
            A12E5D8C.CheckedChanged += ckbDefault_CheckedChanged;
            ckbAccountCenter.CheckedChanged += ckbDefault_CheckedChanged;
            txtMatKhauChiDinh.TextChanged += txtMatKhauChiDinh_TextChanged;
            btnCancel.Click += btnCancel_Click;
            btnSave.Click += btnSave_Click;
            btn_setting.Click += btnCancel_Click;
            pnlHeader.ResumeLayout(false);
            ((ISupportInitialize)B9804D95).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            ((ISupportInitialize)nudInteractTo).EndInit();
            ((ISupportInitialize)nudInteractFrom).EndInit();
            windowBar.ResumeLayout(false);
            ResumeLayout(false);
        }
        internal Panel panel1;

        internal TextBox txtTenHanhDong;

        internal Label label1;

        internal Button btnCancel;

        internal Button btnSave;

        internal Panel pnlHeader;

        internal Button A11EA82A;

        internal PictureBox B9804D95;

        internal RadioButton A12E5D8C;

        internal RadioButton rbMatKhauRandom;

        internal CheckBox B197EAA2;

        internal TextBox txtMatKhauChiDinh;
        internal Label lblMatKhauChiDinh;
        internal Button btnHelpMatKhau;
        internal CheckBox ckbAccountCenter;
        internal LinkLabel lnkCauHinhAC;
        #endregion

        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_mode;
        private AntdUI.Button btn_global;
        private AntdUI.Button btn_setting;
        private Panel panel2;
        internal Label label4;
        internal NumericUpDown nudInteractTo;
        internal Label label3;
        internal NumericUpDown nudInteractFrom;
    }
}