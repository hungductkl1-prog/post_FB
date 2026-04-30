using System.ComponentModel;

namespace LamToolAutoPhonePrime.Views.Forms.Actions
{
    partial class fHDRoiNhom
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
            BA8519BE = new PictureBox();
            panel1 = new Panel();
            windowBar = new AntdUI.PageHeader();
            btn_mode = new AntdUI.Button();
            btn_global = new AntdUI.Button();
            btn_setting = new AntdUI.Button();
            plUidChiDinh = new Panel();
            plDieuKienTuKhoa = new Panel();
            lblStatusUid = new Label();
            label10 = new Label();
            txtTuKhoa = new TextBox();
            ckbDieuKienTuKhoa = new CheckBox();
            F317BDBE = new CheckBox();
            C7178894 = new CheckBox();
            nudThanhVienToiDa = new NumericUpDown();
            rbRoiTheoDieuKien = new RadioButton();
            rbNgauNhien = new RadioButton();
            label9 = new Label();
            B8092819 = new NumericUpDown();
            nudDelayFrom = new NumericUpDown();
            label7 = new Label();
            B290B7A0 = new Label();
            FB87AB17 = new Label();
            nudSoLuongTo = new NumericUpDown();
            nudSoLuongFrom = new NumericUpDown();
            txtTenHanhDong = new TextBox();
            D62ABD9D = new Label();
            label4 = new Label();
            DB0E3E1D = new Label();
            label1 = new Label();
            btnCancel = new Button();
            btnSave = new Button();
            pnlHeader.SuspendLayout();
            ((ISupportInitialize)BA8519BE).BeginInit();
            panel1.SuspendLayout();
            windowBar.SuspendLayout();
            plUidChiDinh.SuspendLayout();
            plDieuKienTuKhoa.SuspendLayout();
            ((ISupportInitialize)nudThanhVienToiDa).BeginInit();
            ((ISupportInitialize)B8092819).BeginInit();
            ((ISupportInitialize)nudDelayFrom).BeginInit();
            ((ISupportInitialize)nudSoLuongTo).BeginInit();
            ((ISupportInitialize)nudSoLuongFrom).BeginInit();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlHeader.BackColor = Color.White;
            pnlHeader.Controls.Add(button1);
            pnlHeader.Controls.Add(BA8519BE);
            pnlHeader.Cursor = Cursors.SizeAll;
            pnlHeader.Location = new Point(0, 3);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(644, 31);
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
            button1.Location = new Point(613, 1);
            button1.Name = "button1";
            button1.Size = new Size(30, 30);
            button1.TabIndex = 77;
            button1.TextImageRelation = TextImageRelation.ImageBeforeText;
            button1.UseVisualStyleBackColor = true;
            // 
            // BA8519BE
            // 
            BA8519BE.Location = new Point(3, 2);
            BA8519BE.Name = "BA8519BE";
            BA8519BE.Size = new Size(34, 27);
            BA8519BE.SizeMode = PictureBoxSizeMode.Zoom;
            BA8519BE.TabIndex = 76;
            BA8519BE.TabStop = false;
            // 
            // panel1
            // 
            panel1.BackColor = Color.White;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(windowBar);
            panel1.Controls.Add(plUidChiDinh);
            panel1.Controls.Add(rbRoiTheoDieuKien);
            panel1.Controls.Add(rbNgauNhien);
            panel1.Controls.Add(label9);
            panel1.Controls.Add(B8092819);
            panel1.Controls.Add(nudDelayFrom);
            panel1.Controls.Add(label7);
            panel1.Controls.Add(B290B7A0);
            panel1.Controls.Add(FB87AB17);
            panel1.Controls.Add(nudSoLuongTo);
            panel1.Controls.Add(nudSoLuongFrom);
            panel1.Controls.Add(txtTenHanhDong);
            panel1.Controls.Add(D62ABD9D);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(DB0E3E1D);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(btnCancel);
            panel1.Controls.Add(btnSave);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(357, 528);
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
            windowBar.Size = new Size(355, 35);
            windowBar.SubFont = new Font("Microsoft Sans Serif", LamToolAutoPhonePrime.Utils.Design.FontScale.Caption, FontStyle.Regular, GraphicsUnit.Point, 0);
            windowBar.SubGap = 1;
            windowBar.SubText = "";
            windowBar.TabIndex = 122;
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
            btn_mode.Location = new Point(275, 0);
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
            btn_global.Location = new Point(301, 0);
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
            btn_setting.Location = new Point(325, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(30, 35);
            btn_setting.TabIndex = 8;
            btn_setting.WaveSize = 0;
            // 
            // plUidChiDinh
            // 
            plUidChiDinh.BorderStyle = BorderStyle.FixedSingle;
            plUidChiDinh.Controls.Add(plDieuKienTuKhoa);
            plUidChiDinh.Controls.Add(ckbDieuKienTuKhoa);
            plUidChiDinh.Controls.Add(F317BDBE);
            plUidChiDinh.Controls.Add(C7178894);
            plUidChiDinh.Controls.Add(nudThanhVienToiDa);
            plUidChiDinh.Location = new Point(31, 206);
            plUidChiDinh.Name = "plUidChiDinh";
            plUidChiDinh.Size = new Size(295, 244);
            plUidChiDinh.TabIndex = 49;
            // 
            // plDieuKienTuKhoa
            // 
            plDieuKienTuKhoa.BorderStyle = BorderStyle.FixedSingle;
            plDieuKienTuKhoa.Controls.Add(lblStatusUid);
            plDieuKienTuKhoa.Controls.Add(label10);
            plDieuKienTuKhoa.Controls.Add(txtTuKhoa);
            plDieuKienTuKhoa.Location = new Point(23, 79);
            plDieuKienTuKhoa.Name = "plDieuKienTuKhoa";
            plDieuKienTuKhoa.Size = new Size(265, 160);
            plDieuKienTuKhoa.TabIndex = 50;
            // 
            // lblStatusUid
            // 
            lblStatusUid.AutoSize = true;
            lblStatusUid.Location = new Point(3, 3);
            lblStatusUid.Name = "lblStatusUid";
            lblStatusUid.Size = new Size(139, 16);
            lblStatusUid.TabIndex = 0;
            lblStatusUid.Text = "Danh sách từ khóa (0):";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(4, 139);
            label10.Name = "label10";
            label10.Size = new Size(127, 16);
            label10.TabIndex = 0;
            label10.Text = "(Mỗi từ khóa 1 dòng)";
            // 
            // txtTuKhoa
            // 
            txtTuKhoa.Location = new Point(7, 25);
            txtTuKhoa.Multiline = true;
            txtTuKhoa.Name = "txtTuKhoa";
            txtTuKhoa.ScrollBars = ScrollBars.Both;
            txtTuKhoa.Size = new Size(253, 111);
            txtTuKhoa.TabIndex = 1;
            txtTuKhoa.WordWrap = false;
            // 
            // ckbDieuKienTuKhoa
            // 
            ckbDieuKienTuKhoa.AutoSize = true;
            ckbDieuKienTuKhoa.Cursor = Cursors.Hand;
            ckbDieuKienTuKhoa.Location = new Point(5, 53);
            ckbDieuKienTuKhoa.Name = "ckbDieuKienTuKhoa";
            ckbDieuKienTuKhoa.Size = new Size(209, 20);
            ckbDieuKienTuKhoa.TabIndex = 2;
            ckbDieuKienTuKhoa.Text = "Tên nhóm có chứa từ khóa sau:";
            ckbDieuKienTuKhoa.UseVisualStyleBackColor = true;
            // 
            // F317BDBE
            // 
            F317BDBE.AutoSize = true;
            F317BDBE.Cursor = Cursors.Hand;
            F317BDBE.Location = new Point(5, 3);
            F317BDBE.Name = "F317BDBE";
            F317BDBE.Size = new Size(191, 20);
            F317BDBE.TabIndex = 2;
            F317BDBE.Text = "Rời nhóm kiểm duyệt bài viết";
            F317BDBE.UseVisualStyleBackColor = true;
            // 
            // C7178894
            // 
            C7178894.AutoSize = true;
            C7178894.Cursor = Cursors.Hand;
            C7178894.Location = new Point(5, 27);
            C7178894.Name = "C7178894";
            C7178894.Size = new Size(181, 20);
            C7178894.TabIndex = 2;
            C7178894.Text = "Số lượng thành viên ít hơn:";
            C7178894.UseVisualStyleBackColor = true;
            // 
            // nudThanhVienToiDa
            // 
            nudThanhVienToiDa.Location = new Point(196, 26);
            nudThanhVienToiDa.Maximum = new decimal(new int[] { 999999999, 0, 0, 0 });
            nudThanhVienToiDa.Name = "nudThanhVienToiDa";
            nudThanhVienToiDa.Size = new Size(92, 23);
            nudThanhVienToiDa.TabIndex = 1;
            nudThanhVienToiDa.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // rbRoiTheoDieuKien
            // 
            rbRoiTheoDieuKien.AutoSize = true;
            rbRoiTheoDieuKien.Cursor = Cursors.Hand;
            rbRoiTheoDieuKien.Location = new Point(31, 182);
            rbRoiTheoDieuKien.Name = "rbRoiTheoDieuKien";
            rbRoiTheoDieuKien.Size = new Size(163, 20);
            rbRoiTheoDieuKien.TabIndex = 48;
            rbRoiTheoDieuKien.Text = "Rời nhóm theo điều kiện";
            rbRoiTheoDieuKien.UseVisualStyleBackColor = true;
            // 
            // rbNgauNhien
            // 
            rbNgauNhien.AutoSize = true;
            rbNgauNhien.Checked = true;
            rbNgauNhien.Cursor = Cursors.Hand;
            rbNgauNhien.Location = new Point(31, 157);
            rbNgauNhien.Name = "rbNgauNhien";
            rbNgauNhien.Size = new Size(187, 20);
            rbNgauNhien.TabIndex = 48;
            rbNgauNhien.TabStop = true;
            rbNgauNhien.Text = "Ngẫu nhiên danh sách nhóm";
            rbNgauNhien.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(28, 135);
            label9.Name = "label9";
            label9.Size = new Size(137, 16);
            label9.TabIndex = 47;
            label9.Text = "Tùy chọn nhóm để rời:";
            // 
            // B8092819
            // 
            B8092819.Location = new Point(228, 107);
            B8092819.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            B8092819.Name = "B8092819";
            B8092819.Size = new Size(56, 23);
            B8092819.TabIndex = 4;
            B8092819.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // nudDelayFrom
            // 
            nudDelayFrom.Location = new Point(131, 107);
            nudDelayFrom.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudDelayFrom.Name = "nudDelayFrom";
            nudDelayFrom.Size = new Size(56, 23);
            nudDelayFrom.TabIndex = 3;
            nudDelayFrom.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // label7
            // 
            label7.Location = new Point(193, 109);
            label7.Name = "label7";
            label7.Size = new Size(29, 16);
            label7.TabIndex = 46;
            label7.Text = "đến";
            label7.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // B290B7A0
            // 
            B290B7A0.AutoSize = true;
            B290B7A0.Location = new Point(286, 109);
            B290B7A0.Name = "B290B7A0";
            B290B7A0.Size = new Size(30, 16);
            B290B7A0.TabIndex = 45;
            B290B7A0.Text = "giây";
            // 
            // FB87AB17
            // 
            FB87AB17.AutoSize = true;
            FB87AB17.Location = new Point(26, 109);
            FB87AB17.Name = "FB87AB17";
            FB87AB17.Size = new Size(89, 16);
            FB87AB17.TabIndex = 44;
            FB87AB17.Text = "Thời gian chờ:";
            // 
            // nudSoLuongTo
            // 
            nudSoLuongTo.Location = new Point(229, 78);
            nudSoLuongTo.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudSoLuongTo.Name = "nudSoLuongTo";
            nudSoLuongTo.Size = new Size(56, 23);
            nudSoLuongTo.TabIndex = 2;
            nudSoLuongTo.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // nudSoLuongFrom
            // 
            nudSoLuongFrom.Location = new Point(132, 78);
            nudSoLuongFrom.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudSoLuongFrom.Name = "nudSoLuongFrom";
            nudSoLuongFrom.Size = new Size(56, 23);
            nudSoLuongFrom.TabIndex = 1;
            nudSoLuongFrom.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // txtTenHanhDong
            // 
            txtTenHanhDong.Location = new Point(132, 49);
            txtTenHanhDong.Name = "txtTenHanhDong";
            txtTenHanhDong.Size = new Size(194, 23);
            txtTenHanhDong.TabIndex = 0;
            // 
            // D62ABD9D
            // 
            D62ABD9D.Location = new Point(194, 80);
            D62ABD9D.Name = "D62ABD9D";
            D62ABD9D.Size = new Size(29, 16);
            D62ABD9D.TabIndex = 37;
            D62ABD9D.Text = "đến";
            D62ABD9D.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(287, 80);
            label4.Name = "label4";
            label4.Size = new Size(39, 16);
            label4.TabIndex = 35;
            label4.Text = "nhóm";
            // 
            // DB0E3E1D
            // 
            DB0E3E1D.AutoSize = true;
            DB0E3E1D.Location = new Point(27, 80);
            DB0E3E1D.Name = "DB0E3E1D";
            DB0E3E1D.Size = new Size(99, 16);
            DB0E3E1D.TabIndex = 32;
            DB0E3E1D.Text = "Số lượng nhóm:";
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
            btnCancel.Location = new Point(186, 486);
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
            btnSave.Location = new Point(79, 486);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(92, 29);
            btnSave.TabIndex = 6;
            btnSave.Text = "Thêm";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // fHDRoiNhom
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(357, 528);
            Controls.Add(panel1);
            Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "fHDRoiNhom";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Cấu hình tương tác";
            pnlHeader.ResumeLayout(false);
            ((ISupportInitialize)BA8519BE).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            windowBar.ResumeLayout(false);
            plUidChiDinh.ResumeLayout(false);
            plUidChiDinh.PerformLayout();
            plDieuKienTuKhoa.ResumeLayout(false);
            plDieuKienTuKhoa.PerformLayout();
            ((ISupportInitialize)nudThanhVienToiDa).EndInit();
            ((ISupportInitialize)B8092819).EndInit();
            ((ISupportInitialize)nudDelayFrom).EndInit();
            ((ISupportInitialize)nudSoLuongTo).EndInit();
            ((ISupportInitialize)nudSoLuongFrom).EndInit();
            ResumeLayout(false);
        }
        internal Panel panel1;

        internal NumericUpDown nudSoLuongTo;

        internal NumericUpDown nudSoLuongFrom;

        internal TextBox txtTenHanhDong;

        internal Label D62ABD9D;

        internal Label label4;

        internal Label DB0E3E1D;

        internal Label label1;

        internal Button btnCancel;

        internal Button btnSave;

        internal Panel pnlHeader;

        internal Button button1;

        internal PictureBox BA8519BE;

        internal NumericUpDown B8092819;

        internal NumericUpDown nudDelayFrom;

        internal Label label7;

        internal Label B290B7A0;

        internal Label FB87AB17;

        internal RadioButton rbRoiTheoDieuKien;

        internal RadioButton rbNgauNhien;

        internal Label label9;

        internal Panel plUidChiDinh;

        internal TextBox txtTuKhoa;

        internal Label label10;

        internal Label lblStatusUid;

        internal Panel plDieuKienTuKhoa;

        internal CheckBox ckbDieuKienTuKhoa;

        internal CheckBox C7178894;

        internal NumericUpDown nudThanhVienToiDa;

        internal CheckBox F317BDBE;
        #endregion

        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_mode;
        private AntdUI.Button btn_global;
        private AntdUI.Button btn_setting;
    }
}