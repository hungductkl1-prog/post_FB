using System.ComponentModel;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions
{
    partial class fHDOnOff2FA
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
            BBA72113 = new Panel();
            BFB4AF83 = new Button();
            pictureBox1 = new PictureBox();
            A33868AD = new Panel();
            windowBar = new AntdUI.PageHeader();
            btn_mode = new AntdUI.Button();
            btn_global = new AntdUI.Button();
            btn_setting = new AntdUI.Button();
            DDBBFF9D = new Panel();
            rbXoa2FACu = new RadioButton();
            rbGiu2FACu = new RadioButton();
            F12647B1 = new RadioButton();
            F5092432 = new Label();
            rbBat2FA = new RadioButton();
            rbTat2FA = new RadioButton();
            txtTenHanhDong = new TextBox();
            C91674A6 = new Label();
            btnCancel = new Button();
            btnSave = new Button();
            ckbAccountCenter = new CheckBox();
            lnkCauHinhAC = new LinkLabel();
            BBA72113.SuspendLayout();
            ((ISupportInitialize)pictureBox1).BeginInit();
            A33868AD.SuspendLayout();
            windowBar.SuspendLayout();
            DDBBFF9D.SuspendLayout();
            SuspendLayout();
            // 
            // BBA72113
            // 
            BBA72113.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            BBA72113.BackColor = Color.White;
            BBA72113.Controls.Add(BFB4AF83);
            BBA72113.Controls.Add(pictureBox1);
            BBA72113.Cursor = Cursors.SizeAll;
            BBA72113.Location = new Point(0, 3);
            BBA72113.Name = "BBA72113";
            BBA72113.Size = new Size(417, 31);
            BBA72113.TabIndex = 9;
            // 
            // BFB4AF83
            // 
            BFB4AF83.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            BFB4AF83.Cursor = Cursors.Hand;
            BFB4AF83.FlatAppearance.BorderSize = 0;
            BFB4AF83.FlatStyle = FlatStyle.Flat;
            BFB4AF83.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            BFB4AF83.ForeColor = Color.White;
            BFB4AF83.Location = new Point(386, 1);
            BFB4AF83.Name = "BFB4AF83";
            BFB4AF83.Size = new Size(30, 30);
            BFB4AF83.TabIndex = 77;
            BFB4AF83.TextImageRelation = TextImageRelation.ImageBeforeText;
            BFB4AF83.UseVisualStyleBackColor = true;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(3, 2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(34, 27);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 76;
            pictureBox1.TabStop = false;
            // 
            // A33868AD
            // 
            A33868AD.BackColor = Color.White;
            A33868AD.BorderStyle = BorderStyle.FixedSingle;
            A33868AD.Controls.Add(windowBar);
            A33868AD.Controls.Add(DDBBFF9D);
            A33868AD.Controls.Add(rbBat2FA);
            A33868AD.Controls.Add(rbTat2FA);
            A33868AD.Controls.Add(ckbAccountCenter);
            A33868AD.Controls.Add(lnkCauHinhAC);
            A33868AD.Controls.Add(txtTenHanhDong);
            A33868AD.Controls.Add(C91674A6);
            A33868AD.Controls.Add(btnCancel);
            A33868AD.Controls.Add(btnSave);
            A33868AD.Dock = DockStyle.Fill;
            A33868AD.Location = new Point(0, 0);
            A33868AD.Name = "A33868AD";
            A33868AD.Size = new Size(420, 340);
            A33868AD.TabIndex = 0;
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
            windowBar.Size = new Size(418, 35);
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
            btn_mode.Location = new Point(338, 0);
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
            btn_global.Location = new Point(364, 0);
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
            btn_setting.Location = new Point(388, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(30, 35);
            btn_setting.TabIndex = 8;
            btn_setting.WaveSize = 0;
            //
            // DDBBFF9D
            //
            DDBBFF9D.BorderStyle = BorderStyle.FixedSingle;
            DDBBFF9D.Controls.Add(rbXoa2FACu);
            DDBBFF9D.Controls.Add(rbGiu2FACu);
            DDBBFF9D.Controls.Add(F12647B1);
            DDBBFF9D.Controls.Add(F5092432);
            DDBBFF9D.Location = new Point(120, 146);
            DDBBFF9D.Name = "DDBBFF9D";
            DDBBFF9D.Size = new Size(260, 97);
            DDBBFF9D.TabIndex = 48;
            //
            // rbXoa2FACu
            //
            rbXoa2FACu.AutoSize = true;
            rbXoa2FACu.Cursor = Cursors.Hand;
            rbXoa2FACu.Location = new Point(10, 72);
            rbXoa2FACu.Name = "rbXoa2FACu";
            rbXoa2FACu.Size = new Size(193, 20);
            rbXoa2FACu.TabIndex = 34;
            rbXoa2FACu.Text = "Xóa 2FA cũ và thêm 2FA mới";
            rbXoa2FACu.UseVisualStyleBackColor = true;
            //
            // rbGiu2FACu
            //
            rbGiu2FACu.AutoSize = true;
            rbGiu2FACu.Cursor = Cursors.Hand;
            rbGiu2FACu.Location = new Point(10, 47);
            rbGiu2FACu.Name = "rbGiu2FACu";
            rbGiu2FACu.Size = new Size(188, 20);
            rbGiu2FACu.TabIndex = 33;
            rbGiu2FACu.Text = "Giữ 2FA cũ và thêm 2FA mới";
            rbGiu2FACu.UseVisualStyleBackColor = true;
            //
            // F12647B1
            //
            F12647B1.AutoSize = true;
            F12647B1.Checked = true;
            F12647B1.Cursor = Cursors.Hand;
            F12647B1.Location = new Point(10, 22);
            F12647B1.Name = "F12647B1";
            F12647B1.Size = new Size(134, 20);
            F12647B1.TabIndex = 32;
            F12647B1.TabStop = true;
            F12647B1.Text = "Sẽ không bật 2FA";
            F12647B1.UseVisualStyleBackColor = true;
            //
            // F5092432
            //
            F5092432.AutoSize = true;
            F5092432.Location = new Point(6, 4);
            F5092432.Name = "F5092432";
            F5092432.Size = new Size(169, 16);
            F5092432.TabIndex = 31;
            F5092432.Text = "Nếu tài khoản đã có 2FA thì:";
            //
            // rbTat2FA
            //
            rbTat2FA.AutoSize = true;
            rbTat2FA.Checked = true;
            rbTat2FA.Cursor = Cursors.Hand;
            rbTat2FA.Location = new Point(187, 88);
            rbTat2FA.Name = "rbTat2FA";
            rbTat2FA.Size = new Size(70, 20);
            rbTat2FA.TabIndex = 46;
            rbTat2FA.TabStop = true;
            rbTat2FA.Text = "Tắt 2FA";
            rbTat2FA.UseVisualStyleBackColor = true;
            //
            // rbBat2FA
            //
            rbBat2FA.AutoSize = true;
            rbBat2FA.Cursor = Cursors.Hand;
            rbBat2FA.Location = new Point(187, 117);
            rbBat2FA.Name = "rbBat2FA";
            rbBat2FA.Size = new Size(69, 20);
            rbBat2FA.TabIndex = 47;
            rbBat2FA.Text = "Bật 2FA";
            rbBat2FA.UseVisualStyleBackColor = true;
            //
            // ckbAccountCenter
            //
            ckbAccountCenter.AutoSize = true;
            ckbAccountCenter.Cursor = Cursors.Hand;
            ckbAccountCenter.Location = new Point(120, 254);
            ckbAccountCenter.Name = "ckbAccountCenter";
            ckbAccountCenter.Size = new Size(160, 20);
            ckbAccountCenter.TabIndex = 49;
            ckbAccountCenter.Text = "Sử dụng Account Center";
            ckbAccountCenter.UseVisualStyleBackColor = true;
            //
            // lnkCauHinhAC
            //
            lnkCauHinhAC.AutoSize = true;
            lnkCauHinhAC.Cursor = Cursors.Hand;
            lnkCauHinhAC.Location = new Point(284, 256);
            lnkCauHinhAC.Name = "lnkCauHinhAC";
            lnkCauHinhAC.Size = new Size(56, 16);
            lnkCauHinhAC.TabIndex = 50;
            lnkCauHinhAC.TabStop = true;
            lnkCauHinhAC.Text = "Cấu hình";
            // 
            // txtTenHanhDong
            // 
            txtTenHanhDong.Location = new Point(131, 49);
            txtTenHanhDong.Name = "txtTenHanhDong";
            txtTenHanhDong.Size = new Size(257, 23);
            txtTenHanhDong.TabIndex = 0;
            // 
            // C91674A6
            // 
            C91674A6.AutoSize = true;
            C91674A6.Location = new Point(26, 52);
            C91674A6.Name = "C91674A6";
            C91674A6.Size = new Size(98, 16);
            C91674A6.TabIndex = 31;
            C91674A6.Text = "Tên hành động:";
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
            btnCancel.Location = new Point(216, 295);
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
            btnSave.Location = new Point(109, 295);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(92, 29);
            btnSave.TabIndex = 6;
            btnSave.Text = "Thêm";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // fHDOnOff2FA
            //
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(420, 340);
            Controls.Add(A33868AD);
            Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "fHDOnOff2FA";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Cấu hình tương tác";
            BBA72113.ResumeLayout(false);
            ((ISupportInitialize)pictureBox1).EndInit();
            A33868AD.ResumeLayout(false);
            A33868AD.PerformLayout();
            windowBar.ResumeLayout(false);
            DDBBFF9D.ResumeLayout(false);
            DDBBFF9D.PerformLayout();
            ResumeLayout(false);
        }
        internal Panel A33868AD;

        internal TextBox txtTenHanhDong;

        internal Label C91674A6;

        internal Button btnCancel;

        internal Button btnSave;

        internal Panel BBA72113;

        internal Button BFB4AF83;

        internal PictureBox pictureBox1;

        internal RadioButton rbBat2FA;

        internal RadioButton rbTat2FA;

        internal Panel DDBBFF9D;

        internal RadioButton rbXoa2FACu;

        internal RadioButton rbGiu2FACu;

        internal RadioButton F12647B1;

        internal Label F5092432;

        internal CheckBox ckbAccountCenter;

        internal LinkLabel lnkCauHinhAC;
        #endregion

        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_mode;
        private AntdUI.Button btn_global;
        private AntdUI.Button btn_setting;
    }
}