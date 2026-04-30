using System.ComponentModel;

namespace LamToolAutoPhonePrime.Views.Forms.Actions
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
            groupBox2 = new GroupBox();
            panel10 = new Panel();
            label20 = new Label();
            txtLinks = new TextBox();
            panel7 = new Panel();
            label18 = new Label();
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
            C0158C1B = new Label();
            txtTenHanhDong = new TextBox();
            label1 = new Label();
            btnCancel = new Button();
            btnSave = new Button();
            pnlHeader.SuspendLayout();
            ((ISupportInitialize)B9804D95).BeginInit();
            panel1.SuspendLayout();
            groupBox2.SuspendLayout();
            panel10.SuspendLayout();
            panel7.SuspendLayout();
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
            pnlHeader.Size = new Size(331, 31);
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
            A11EA82A.Location = new Point(300, 1);
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
            panel1.Controls.Add(groupBox2);
            panel1.Controls.Add(panel2);
            panel1.Controls.Add(windowBar);
            panel1.Controls.Add(B197EAA2);
            panel1.Controls.Add(A12E5D8C);
            panel1.Controls.Add(rbMatKhauRandom);
            panel1.Controls.Add(C0158C1B);
            panel1.Controls.Add(txtTenHanhDong);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(btnCancel);
            panel1.Controls.Add(btnSave);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(456, 395);
            panel1.TabIndex = 0;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(panel10);
            groupBox2.Controls.Add(txtLinks);
            groupBox2.Controls.Add(panel7);
            groupBox2.Location = new Point(63, 171);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(380, 137);
            groupBox2.TabIndex = 197;
            groupBox2.TabStop = false;
            // 
            // panel10
            // 
            panel10.Controls.Add(label20);
            panel10.Dock = DockStyle.Bottom;
            panel10.Location = new Point(3, 112);
            panel10.Name = "panel10";
            panel10.Size = new Size(374, 22);
            panel10.TabIndex = 200;
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Dock = DockStyle.Left;
            label20.Location = new Point(0, 0);
            label20.Name = "label20";
            label20.Size = new Size(365, 16);
            label20.TabIndex = 1;
            label20.Text = "Mỗi mật khẩu 1 dòng (có thể dùng * để thay ký tự ngẫu nhiên)";
            // 
            // txtLinks
            // 
            txtLinks.BorderStyle = BorderStyle.FixedSingle;
            txtLinks.Dock = DockStyle.Top;
            txtLinks.Location = new Point(3, 41);
            txtLinks.MaxLength = int.MaxValue;
            txtLinks.Multiline = true;
            txtLinks.Name = "txtLinks";
            txtLinks.ScrollBars = ScrollBars.Both;
            txtLinks.Size = new Size(374, 68);
            txtLinks.TabIndex = 200;
            txtLinks.WordWrap = false;
            // 
            // panel7
            // 
            panel7.Controls.Add(label18);
            panel7.Dock = DockStyle.Top;
            panel7.Location = new Point(3, 19);
            panel7.Name = "panel7";
            panel7.Size = new Size(374, 22);
            panel7.TabIndex = 197;
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.Dock = DockStyle.Left;
            label18.Location = new Point(0, 0);
            label18.Name = "label18";
            label18.Size = new Size(149, 16);
            label18.TabIndex = 1;
            label18.Text = "Danh sách mật khẩu (0):";
            // 
            // panel2
            // 
            panel2.Controls.Add(label4);
            panel2.Controls.Add(nudInteractTo);
            panel2.Controls.Add(label3);
            panel2.Controls.Add(nudInteractFrom);
            panel2.Location = new Point(139, 106);
            panel2.Name = "panel2";
            panel2.Size = new Size(299, 23);
            panel2.TabIndex = 190;
            // 
            // label4
            // 
            label4.Dock = DockStyle.Left;
            label4.Location = new Point(153, 0);
            label4.Name = "label4";
            label4.Size = new Size(143, 23);
            label4.TabIndex = 41;
            label4.Text = "ký tự ngẫu nhiên";
            label4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // nudInteractTo
            // 
            nudInteractTo.Dock = DockStyle.Left;
            nudInteractTo.Location = new Point(97, 0);
            nudInteractTo.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            nudInteractTo.Minimum = new decimal(new int[] { 6, 0, 0, 0 });
            nudInteractTo.Name = "nudInteractTo";
            nudInteractTo.Size = new Size(56, 23);
            nudInteractTo.TabIndex = 40;
            nudInteractTo.Value = new decimal(new int[] { 6, 0, 0, 0 });
            // 
            // label3
            // 
            label3.Dock = DockStyle.Left;
            label3.Location = new Point(56, 0);
            label3.Name = "label3";
            label3.Size = new Size(41, 23);
            label3.TabIndex = 39;
            label3.Text = "đến";
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
            nudInteractFrom.Value = new decimal(new int[] { 6, 0, 0, 0 });
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
            windowBar.Size = new Size(454, 35);
            windowBar.SubFont = new Font("Microsoft Sans Serif", LamToolAutoPhonePrime.Utils.Design.FontScale.Caption, FontStyle.Regular, GraphicsUnit.Point, 0);
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
            B197EAA2.Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 163);
            B197EAA2.Location = new Point(44, 317);
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
            A12E5D8C.Location = new Point(44, 145);
            A12E5D8C.Name = "A12E5D8C";
            A12E5D8C.Size = new Size(130, 20);
            A12E5D8C.TabIndex = 47;
            A12E5D8C.Text = "Mật khẩu chỉ định:";
            A12E5D8C.UseVisualStyleBackColor = true;
            // 
            // rbMatKhauRandom
            // 
            rbMatKhauRandom.AutoSize = true;
            rbMatKhauRandom.Checked = true;
            rbMatKhauRandom.Cursor = Cursors.Hand;
            rbMatKhauRandom.Location = new Point(44, 109);
            rbMatKhauRandom.Name = "rbMatKhauRandom";
            rbMatKhauRandom.Size = new Size(89, 20);
            rbMatKhauRandom.TabIndex = 47;
            rbMatKhauRandom.TabStop = true;
            rbMatKhauRandom.Text = "Ngẫu nhiên";
            rbMatKhauRandom.UseVisualStyleBackColor = true;
            // 
            // C0158C1B
            // 
            C0158C1B.AutoSize = true;
            C0158C1B.Location = new Point(27, 80);
            C0158C1B.Name = "C0158C1B";
            C0158C1B.Size = new Size(146, 16);
            C0158C1B.TabIndex = 44;
            C0158C1B.Text = "Tùy chọn mật khẩu mới:";
            // 
            // txtTenHanhDong
            // 
            txtTenHanhDong.Location = new Point(131, 49);
            txtTenHanhDong.Name = "txtTenHanhDong";
            txtTenHanhDong.Size = new Size(269, 23);
            txtTenHanhDong.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(27, 52);
            label1.Name = "label1";
            label1.Size = new Size(98, 16);
            label1.TabIndex = 31;
            label1.Text = "Tên hành động:";
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
            btnCancel.Location = new Point(235, 353);
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
            btnSave.Location = new Point(128, 353);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(92, 29);
            btnSave.TabIndex = 6;
            btnSave.Text = "Thêm";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // fHDDoiMatKhau
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(456, 395);
            Controls.Add(panel1);
            Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "fHDDoiMatKhau";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Cấu hình tương tác";
            rbMatKhauRandom.CheckedChanged += ckbDefault_CheckedChanged;
            A12E5D8C.CheckedChanged += ckbDefault_CheckedChanged;
            txtLinks.TextChanged += txtLinks_TextChanged;
            btnCancel.Click += btnCancel_Click;
            btnSave.Click += btnSave_Click;
            btn_setting.Click += btnCancel_Click;
            pnlHeader.ResumeLayout(false);
            ((ISupportInitialize)B9804D95).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            panel10.ResumeLayout(false);
            panel10.PerformLayout();
            panel7.ResumeLayout(false);
            panel7.PerformLayout();
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

        internal Label C0158C1B;

        internal CheckBox B197EAA2;
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
        private GroupBox groupBox2;
        private Panel panel10;
        internal Label label20;
        private TextBox txtLinks;
        private Panel panel7;
        internal Label label18;
    }
}