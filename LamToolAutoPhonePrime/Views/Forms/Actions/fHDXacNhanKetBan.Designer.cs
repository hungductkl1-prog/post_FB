using System.ComponentModel;

namespace LamToolAutoPhonePrime.Views.Forms.Actions
{
    partial class fHDXacNhanKetBan
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
            C9BD70A9 = new Panel();
            button1 = new Button();
            pictureBox1 = new PictureBox();
            panel1 = new Panel();
            windowBar = new AntdUI.PageHeader();
            btn_mode = new AntdUI.Button();
            btn_global = new AntdUI.Button();
            btn_setting = new AntdUI.Button();
            ckbOnlyAddFriendWithMutualFriends = new CheckBox();
            FD14093E = new CheckBox();
            nudDelayTo = new NumericUpDown();
            nudDelayFrom = new NumericUpDown();
            label7 = new Label();
            B935B4AC = new Label();
            CABCDE8D = new Label();
            nudSoLuongTo = new NumericUpDown();
            nudSoLuongFrom = new NumericUpDown();
            txtTenHanhDong = new TextBox();
            FBA90CA9 = new Label();
            CEBED51B = new Label();
            B92AE901 = new Label();
            AE2E443D = new Label();
            btnCancel = new Button();
            btnSave = new Button();
            C9BD70A9.SuspendLayout();
            ((ISupportInitialize)pictureBox1).BeginInit();
            panel1.SuspendLayout();
            windowBar.SuspendLayout();
            ((ISupportInitialize)nudDelayTo).BeginInit();
            ((ISupportInitialize)nudDelayFrom).BeginInit();
            ((ISupportInitialize)nudSoLuongTo).BeginInit();
            ((ISupportInitialize)nudSoLuongFrom).BeginInit();
            SuspendLayout();
            // 
            // C9BD70A9
            // 
            C9BD70A9.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            C9BD70A9.BackColor = Color.White;
            C9BD70A9.Controls.Add(button1);
            C9BD70A9.Controls.Add(pictureBox1);
            C9BD70A9.Cursor = Cursors.SizeAll;
            C9BD70A9.Location = new Point(0, 3);
            C9BD70A9.Name = "C9BD70A9";
            C9BD70A9.Size = new Size(359, 31);
            C9BD70A9.TabIndex = 9;
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
            // pictureBox1
            // 
            pictureBox1.Location = new Point(3, 2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(34, 27);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 76;
            pictureBox1.TabStop = false;
            // 
            // panel1
            // 
            panel1.BackColor = Color.White;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(windowBar);
            panel1.Controls.Add(ckbOnlyAddFriendWithMutualFriends);
            panel1.Controls.Add(FD14093E);
            panel1.Controls.Add(nudDelayTo);
            panel1.Controls.Add(nudDelayFrom);
            panel1.Controls.Add(label7);
            panel1.Controls.Add(B935B4AC);
            panel1.Controls.Add(CABCDE8D);
            panel1.Controls.Add(nudSoLuongTo);
            panel1.Controls.Add(nudSoLuongFrom);
            panel1.Controls.Add(txtTenHanhDong);
            panel1.Controls.Add(FBA90CA9);
            panel1.Controls.Add(CEBED51B);
            panel1.Controls.Add(B92AE901);
            panel1.Controls.Add(AE2E443D);
            panel1.Controls.Add(btnCancel);
            panel1.Controls.Add(btnSave);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(362, 192);
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
            windowBar.SubFont = new Font("Microsoft Sans Serif", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            windowBar.SubGap = 1;
            windowBar.SubText = "";
            windowBar.TabIndex = 186;
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
            // ckbOnlyAddFriendWithMutualFriends
            // 
            ckbOnlyAddFriendWithMutualFriends.AutoSize = true;
            ckbOnlyAddFriendWithMutualFriends.Cursor = Cursors.Hand;
            ckbOnlyAddFriendWithMutualFriends.Location = new Point(30, 231);
            ckbOnlyAddFriendWithMutualFriends.Name = "ckbOnlyAddFriendWithMutualFriends";
            ckbOnlyAddFriendWithMutualFriends.Size = new Size(226, 20);
            ckbOnlyAddFriendWithMutualFriends.TabIndex = 49;
            ckbOnlyAddFriendWithMutualFriends.Text = "Chỉ kết bạn với người có bạn chung";
            ckbOnlyAddFriendWithMutualFriends.UseVisualStyleBackColor = true;
            // 
            // FD14093E
            // 
            FD14093E.AutoSize = true;
            FD14093E.Cursor = Cursors.Hand;
            FD14093E.Location = new Point(30, 207);
            FD14093E.Name = "FD14093E";
            FD14093E.Size = new Size(303, 20);
            FD14093E.TabIndex = 48;
            FD14093E.Text = "Chỉ kết bạn với tên có dấu (Kết bạn với nick Việt)";
            FD14093E.UseVisualStyleBackColor = true;
            // 
            // nudDelayTo
            // 
            nudDelayTo.Location = new Point(229, 112);
            nudDelayTo.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudDelayTo.Name = "nudDelayTo";
            nudDelayTo.Size = new Size(56, 23);
            nudDelayTo.TabIndex = 4;
            // 
            // nudDelayFrom
            // 
            nudDelayFrom.Location = new Point(132, 112);
            nudDelayFrom.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudDelayFrom.Name = "nudDelayFrom";
            nudDelayFrom.Size = new Size(56, 23);
            nudDelayFrom.TabIndex = 3;
            // 
            // label7
            // 
            label7.Location = new Point(194, 114);
            label7.Name = "label7";
            label7.Size = new Size(29, 16);
            label7.TabIndex = 46;
            label7.Text = "đến";
            label7.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // B935B4AC
            // 
            B935B4AC.AutoSize = true;
            B935B4AC.Location = new Point(287, 114);
            B935B4AC.Name = "B935B4AC";
            B935B4AC.Size = new Size(30, 16);
            B935B4AC.TabIndex = 45;
            B935B4AC.Text = "giây";
            // 
            // CABCDE8D
            // 
            CABCDE8D.AutoSize = true;
            CABCDE8D.Location = new Point(27, 114);
            CABCDE8D.Name = "CABCDE8D";
            CABCDE8D.Size = new Size(89, 16);
            CABCDE8D.TabIndex = 44;
            CABCDE8D.Text = "Thời gian chờ:";
            // 
            // nudSoLuongTo
            // 
            nudSoLuongTo.Location = new Point(229, 81);
            nudSoLuongTo.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudSoLuongTo.Name = "nudSoLuongTo";
            nudSoLuongTo.Size = new Size(56, 23);
            nudSoLuongTo.TabIndex = 2;
            // 
            // nudSoLuongFrom
            // 
            nudSoLuongFrom.Location = new Point(132, 81);
            nudSoLuongFrom.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudSoLuongFrom.Name = "nudSoLuongFrom";
            nudSoLuongFrom.Size = new Size(56, 23);
            nudSoLuongFrom.TabIndex = 1;
            // 
            // txtTenHanhDong
            // 
            txtTenHanhDong.Location = new Point(132, 49);
            txtTenHanhDong.Name = "txtTenHanhDong";
            txtTenHanhDong.Size = new Size(194, 23);
            txtTenHanhDong.TabIndex = 0;
            // 
            // FBA90CA9
            // 
            FBA90CA9.Location = new Point(194, 83);
            FBA90CA9.Name = "FBA90CA9";
            FBA90CA9.Size = new Size(29, 16);
            FBA90CA9.TabIndex = 37;
            FBA90CA9.Text = "đến";
            FBA90CA9.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // CEBED51B
            // 
            CEBED51B.AutoSize = true;
            CEBED51B.Location = new Point(287, 83);
            CEBED51B.Name = "CEBED51B";
            CEBED51B.Size = new Size(28, 16);
            CEBED51B.TabIndex = 35;
            CEBED51B.Text = "bạn";
            // 
            // B92AE901
            // 
            B92AE901.AutoSize = true;
            B92AE901.Location = new Point(27, 83);
            B92AE901.Name = "B92AE901";
            B92AE901.Size = new Size(88, 16);
            B92AE901.TabIndex = 32;
            B92AE901.Text = "Số lượng bạn:";
            // 
            // AE2E443D
            // 
            AE2E443D.AutoSize = true;
            AE2E443D.Location = new Point(27, 52);
            AE2E443D.Name = "AE2E443D";
            AE2E443D.Size = new Size(98, 16);
            AE2E443D.TabIndex = 31;
            AE2E443D.Text = "Tên hành động:";
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
            btnCancel.Location = new Point(189, 148);
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
            btnSave.Location = new Point(82, 148);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(92, 29);
            btnSave.TabIndex = 6;
            btnSave.Text = "Thêm";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // fHDXacNhanKetBan
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(362, 192);
            Controls.Add(panel1);
            Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "fHDXacNhanKetBan";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Cấu hình tương tác";
            C9BD70A9.ResumeLayout(false);
            ((ISupportInitialize)pictureBox1).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            windowBar.ResumeLayout(false);
            ((ISupportInitialize)nudDelayTo).EndInit();
            ((ISupportInitialize)nudDelayFrom).EndInit();
            ((ISupportInitialize)nudSoLuongTo).EndInit();
            ((ISupportInitialize)nudSoLuongFrom).EndInit();
            ResumeLayout(false);
        }
        internal Panel panel1;

        internal NumericUpDown nudSoLuongTo;

        internal NumericUpDown nudSoLuongFrom;

        internal TextBox txtTenHanhDong;

        internal Label FBA90CA9;

        internal Label CEBED51B;

        internal Label B92AE901;

        internal Label AE2E443D;

        internal Button btnCancel;

        internal Button btnSave;

        internal Panel C9BD70A9;

        internal Button button1;

        internal PictureBox pictureBox1;

        internal NumericUpDown nudDelayTo;

        internal NumericUpDown nudDelayFrom;

        internal Label label7;

        internal Label B935B4AC;

        internal Label CABCDE8D;

        internal CheckBox FD14093E;

        internal CheckBox ckbOnlyAddFriendWithMutualFriends;
        #endregion

        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_mode;
        private AntdUI.Button btn_global;
        private AntdUI.Button btn_setting;
    }
}