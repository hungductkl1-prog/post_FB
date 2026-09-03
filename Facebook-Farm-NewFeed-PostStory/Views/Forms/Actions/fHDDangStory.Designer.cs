using Facebook_Farm_NewFeed_PostStory.Properties;
using System.ComponentModel;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions
{
    partial class fHDDangStory
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
            D316AB1E = new Panel();
            button1 = new Button();
            E51CE89C = new PictureBox();
            panel1 = new Panel();
            // Header / hành động
            txtTenHanhDong = new TextBox();
            label1 = new Label();
            E9B44A22 = new Label();
            C913DC8A = new NumericUpDown();
            label17 = new Label();
            F391713F = new NumericUpDown();
            FD150C04 = new Label();
            lblKhoangCach = new Label();
            nudKhoangCachFrom = new NumericUpDown();
            lblKhoangCachDen = new Label();
            nudKhoangCachTo = new NumericUpDown();
            lblKhoangCachGiay = new Label();
            windowBar = new Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel();
            btn_mode = new Button();
            btn_global = new Button();
            btn_setting = new Button();
            // Cột trái: chọn loại story
            rbDangText = new RadioButton();
            rbDangAnhVideo = new RadioButton();
            rbDangNhac = new RadioButton();
            plAnh = new Panel();
            checkBox4 = new CheckBox();
            button5 = new Button();
            txtPathAnh = new TextBox();
            label3 = new Label();
            panel3 = new Panel();
            radioButton3 = new RadioButton();
            radioButton5 = new RadioButton();
            panel2 = new Panel();
            label6 = new Label();
            textBox1 = new TextBox();
            label5 = new Label();
            ckbCoAnh = new CheckBox();
            plAnhNhac = new Panel();
            ckbXoaAnhDaDang = new CheckBox();
            btnChonAnhNhac = new Button();
            txtPathAnhNhac = new TextBox();
            lblThuMucAnh = new Label();
            // Cột phải: nội dung text
            plVanBan = new Panel();
            D397662B = new Label();
            txtLinks = new TextBox();
            B1A8A1BB = new Label();
            EE36BE15 = new LinkLabel();
            E69CEB83 = new Label();
            EBA4AC3C = new RadioButton();
            rbNganCachKyTu = new RadioButton();
            ckbXoaNguyenLieuDaDung = new CheckBox();
            checkBox1 = new CheckBox();
            ckbSuDungBackground = new CheckBox();
            ckbRandomStickers = new CheckBox();
            ckbRandomEffects = new CheckBox();
            ckbTagNeuBat = new CheckBox();
            ckbTagMoiNguoi = new CheckBox();
            ckbPublic = new CheckBox();
            button2 = new Button();
            button3 = new Button();
            button4 = new Button();
            // Nút Lưu/Đóng
            btnSave = new Button();
            btnCancel = new Button();
            D316AB1E.SuspendLayout();
            ((ISupportInitialize)E51CE89C).BeginInit();
            panel1.SuspendLayout();
            panel3.SuspendLayout();
            panel2.SuspendLayout();
            windowBar.SuspendLayout();
            plAnh.SuspendLayout();
            plAnhNhac.SuspendLayout();
            ((ISupportInitialize)F391713F).BeginInit();
            ((ISupportInitialize)C913DC8A).BeginInit();
            ((ISupportInitialize)nudKhoangCachFrom).BeginInit();
            ((ISupportInitialize)nudKhoangCachTo).BeginInit();
            plVanBan.SuspendLayout();
            SuspendLayout();
            //
            // D316AB1E
            //
            D316AB1E.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            D316AB1E.BackColor = Color.White;
            D316AB1E.Controls.Add(button1);
            D316AB1E.Controls.Add(E51CE89C);
            D316AB1E.Cursor = Cursors.SizeAll;
            D316AB1E.Location = new Point(0, 3);
            D316AB1E.Name = "D316AB1E";
            D316AB1E.Size = new Size(848, 31);
            D316AB1E.TabIndex = 9;
            //
            // button1
            //
            button1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button1.Cursor = Cursors.Hand;
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button1.ForeColor = Color.White;
            button1.Location = new Point(817, 1);
            button1.Name = "button1";
            button1.Size = new Size(30, 30);
            button1.TabIndex = 0;
            button1.TextImageRelation = TextImageRelation.ImageBeforeText;
            button1.UseVisualStyleBackColor = true;
            //
            // E51CE89C
            //
            E51CE89C.Location = new Point(3, 2);
            E51CE89C.Name = "E51CE89C";
            E51CE89C.Size = new Size(34, 27);
            E51CE89C.SizeMode = PictureBoxSizeMode.Zoom;
            E51CE89C.TabIndex = 76;
            E51CE89C.TabStop = false;
            //
            // panel1
            //
            panel1.BackColor = Color.White;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(windowBar);
            panel1.Controls.Add(txtTenHanhDong);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(E9B44A22);
            panel1.Controls.Add(C913DC8A);
            panel1.Controls.Add(label17);
            panel1.Controls.Add(F391713F);
            panel1.Controls.Add(FD150C04);
            panel1.Controls.Add(lblKhoangCach);
            panel1.Controls.Add(nudKhoangCachFrom);
            panel1.Controls.Add(lblKhoangCachDen);
            panel1.Controls.Add(nudKhoangCachTo);
            panel1.Controls.Add(lblKhoangCachGiay);
            panel1.Controls.Add(rbDangText);
            panel1.Controls.Add(rbDangAnhVideo);
            panel1.Controls.Add(rbDangNhac);
            panel1.Controls.Add(plAnh);
            panel1.Controls.Add(panel3);
            panel1.Controls.Add(ckbCoAnh);
            panel1.Controls.Add(plAnhNhac);
            panel1.Controls.Add(plVanBan);
            panel1.Controls.Add(btnSave);
            panel1.Controls.Add(btnCancel);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(840, 670);
            panel1.TabIndex = 0;
            //
            // windowBar
            //
            windowBar.BackColor = Color.White;
            windowBar.Controls.Add(btn_mode);
            windowBar.Controls.Add(btn_global);
            windowBar.Controls.Add(btn_setting);
            windowBar.Cursor = Cursors.Hand;
            windowBar.Dock = DockStyle.Top;
            windowBar.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            windowBar.ForeColor = Color.Black;
            windowBar.Icon = Resources.logo_lamtool_v3_dark_16;
            windowBar.Location = new Point(0, 0);
            windowBar.Name = "windowBar";
            windowBar.Size = new Size(838, 35);
            windowBar.TabIndex = 202;
            windowBar.Text = "Thêm tương tác đăng story";
            //
            // btn_mode
            //
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Image = Resources.icons8_circle_16_Green;
            btn_mode.Location = new Point(758, 0);
            btn_mode.Name = "btn_mode";
            btn_mode.Size = new Size(26, 35);
            btn_mode.TabIndex = 6;
            //
            // btn_global
            //
            btn_global.Dock = DockStyle.Right;
            btn_global.Image = Resources.icons8_circle_16_Yellow;
            btn_global.Location = new Point(784, 0);
            btn_global.Name = "btn_global";
            btn_global.Size = new Size(24, 35);
            btn_global.TabIndex = 7;
            //
            // btn_setting
            //
            btn_setting.Dock = DockStyle.Right;
            btn_setting.Image = Resources.icons8_circle_16_Red;
            btn_setting.Location = new Point(808, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Size = new Size(30, 35);
            btn_setting.TabIndex = 8;
            //
            // txtTenHanhDong
            //
            txtTenHanhDong.Location = new Point(150, 49);
            txtTenHanhDong.Name = "txtTenHanhDong";
            txtTenHanhDong.Size = new Size(285, 23);
            txtTenHanhDong.TabIndex = 0;
            //
            // label1
            //
            label1.AutoSize = true;
            label1.Location = new Point(20, 52);
            label1.Name = "label1";
            label1.Size = new Size(98, 16);
            label1.TabIndex = 31;
            label1.Text = "Tên hành động:";
            //
            // E9B44A22
            //
            E9B44A22.AutoSize = true;
            E9B44A22.Location = new Point(20, 84);
            E9B44A22.Name = "E9B44A22";
            E9B44A22.Size = new Size(95, 16);
            E9B44A22.TabIndex = 34;
            E9B44A22.Text = "Số lượng story:";
            //
            // C913DC8A
            //
            C913DC8A.Location = new Point(150, 81);
            C913DC8A.Name = "C913DC8A";
            C913DC8A.Size = new Size(51, 23);
            C913DC8A.TabIndex = 36;
            C913DC8A.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // label17
            //
            label17.Location = new Point(204, 82);
            label17.Name = "label17";
            label17.Size = new Size(40, 16);
            label17.TabIndex = 38;
            label17.Text = "đến";
            label17.TextAlign = ContentAlignment.MiddleCenter;
            //
            // F391713F
            //
            F391713F.Location = new Point(250, 81);
            F391713F.Name = "F391713F";
            F391713F.Size = new Size(51, 23);
            F391713F.TabIndex = 37;
            F391713F.Value = new decimal(new int[] { 3, 0, 0, 0 });
            //
            // FD150C04
            //
            FD150C04.AutoSize = true;
            FD150C04.Location = new Point(305, 84);
            FD150C04.Name = "FD150C04";
            FD150C04.Size = new Size(30, 16);
            FD150C04.TabIndex = 39;
            FD150C04.Text = "story";
            //
            // lblKhoangCach
            //
            lblKhoangCach.AutoSize = true;
            lblKhoangCach.Location = new Point(453, 52);
            lblKhoangCach.Name = "lblKhoangCach";
            lblKhoangCach.Size = new Size(140, 16);
            lblKhoangCach.TabIndex = 240;
            lblKhoangCach.Text = "Cách nhau giữa 2 story:";
            //
            // nudKhoangCachFrom
            //
            nudKhoangCachFrom.Location = new Point(600, 49);
            nudKhoangCachFrom.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudKhoangCachFrom.Name = "nudKhoangCachFrom";
            nudKhoangCachFrom.Size = new Size(56, 23);
            nudKhoangCachFrom.TabIndex = 241;
            nudKhoangCachFrom.Value = new decimal(new int[] { 5, 0, 0, 0 });
            //
            // lblKhoangCachDen
            //
            lblKhoangCachDen.Location = new Point(657, 51);
            lblKhoangCachDen.Name = "lblKhoangCachDen";
            lblKhoangCachDen.Size = new Size(30, 21);
            lblKhoangCachDen.TabIndex = 242;
            lblKhoangCachDen.Text = "đến";
            lblKhoangCachDen.TextAlign = ContentAlignment.MiddleCenter;
            //
            // nudKhoangCachTo
            //
            nudKhoangCachTo.Location = new Point(690, 49);
            nudKhoangCachTo.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudKhoangCachTo.Name = "nudKhoangCachTo";
            nudKhoangCachTo.Size = new Size(56, 23);
            nudKhoangCachTo.TabIndex = 243;
            nudKhoangCachTo.Value = new decimal(new int[] { 10, 0, 0, 0 });
            //
            // lblKhoangCachGiay
            //
            lblKhoangCachGiay.Location = new Point(748, 51);
            lblKhoangCachGiay.Name = "lblKhoangCachGiay";
            lblKhoangCachGiay.Size = new Size(40, 21);
            lblKhoangCachGiay.TabIndex = 244;
            lblKhoangCachGiay.Text = "giây";
            lblKhoangCachGiay.TextAlign = ContentAlignment.MiddleLeft;
            //
            // rbDangText
            //
            rbDangText.AutoSize = true;
            rbDangText.Cursor = Cursors.Hand;
            rbDangText.Location = new Point(20, 122);
            rbDangText.Name = "rbDangText";
            rbDangText.Size = new Size(81, 20);
            rbDangText.TabIndex = 223;
            rbDangText.Text = "Đăng text";
            rbDangText.UseVisualStyleBackColor = true;
            //
            // rbDangAnhVideo
            //
            rbDangAnhVideo.AutoSize = true;
            rbDangAnhVideo.Cursor = Cursors.Hand;
            rbDangAnhVideo.Location = new Point(20, 158);
            rbDangAnhVideo.Name = "rbDangAnhVideo";
            rbDangAnhVideo.Size = new Size(116, 20);
            rbDangAnhVideo.TabIndex = 224;
            rbDangAnhVideo.Text = "Đăng ảnh/video";
            rbDangAnhVideo.UseVisualStyleBackColor = true;
            //
            // rbDangNhac
            //
            rbDangNhac.AutoSize = true;
            rbDangNhac.Checked = true;
            rbDangNhac.Cursor = Cursors.Hand;
            rbDangNhac.Location = new Point(20, 252);
            rbDangNhac.Name = "rbDangNhac";
            rbDangNhac.Size = new Size(87, 20);
            rbDangNhac.TabIndex = 225;
            rbDangNhac.TabStop = true;
            rbDangNhac.Text = "Đăng nhạc";
            rbDangNhac.UseVisualStyleBackColor = true;
            //
            // plAnh (panel media cho rbDangAnhVideo)
            //
            plAnh.Controls.Add(checkBox4);
            plAnh.Controls.Add(button5);
            plAnh.Controls.Add(txtPathAnh);
            plAnh.Controls.Add(label3);
            plAnh.Location = new Point(40, 182);
            plAnh.Name = "plAnh";
            plAnh.Size = new Size(360, 65);
            plAnh.TabIndex = 45;
            //
            // checkBox4
            //
            checkBox4.AutoSize = true;
            checkBox4.Cursor = Cursors.Hand;
            checkBox4.Location = new Point(15, 40);
            checkBox4.Name = "checkBox4";
            checkBox4.Size = new Size(132, 20);
            checkBox4.TabIndex = 210;
            checkBox4.Text = "Xóa media đã đăng";
            checkBox4.UseVisualStyleBackColor = true;
            //
            // button5
            //
            button5.Location = new Point(298, 8);
            button5.Name = "button5";
            button5.Size = new Size(50, 25);
            button5.TabIndex = 193;
            button5.Text = "Chọn";
            button5.UseVisualStyleBackColor = true;
            button5.Click += button5_Click;
            //
            // txtPathAnh
            //
            txtPathAnh.Location = new Point(106, 9);
            txtPathAnh.Name = "txtPathAnh";
            txtPathAnh.ReadOnly = true;
            txtPathAnh.Size = new Size(187, 23);
            txtPathAnh.TabIndex = 1;
            //
            // label3
            //
            label3.AutoSize = true;
            label3.Location = new Point(15, 12);
            label3.Name = "label3";
            label3.Size = new Size(97, 16);
            label3.TabIndex = 0;
            label3.Text = "Thư mục media:";
            //
            // panel3 (chứa radio bài hát ngẫu nhiên / chỉ định + danh sách bài hát)
            //
            panel3.Controls.Add(panel2);
            panel3.Controls.Add(radioButton5);
            panel3.Controls.Add(radioButton3);
            panel3.Location = new Point(40, 275);
            panel3.Name = "panel3";
            panel3.Size = new Size(390, 240);
            panel3.TabIndex = 220;
            //
            // panel2 (chứa textarea danh sách bài hát)
            //
            panel2.BorderStyle = BorderStyle.FixedSingle;
            panel2.Controls.Add(label6);
            panel2.Controls.Add(textBox1);
            panel2.Controls.Add(label5);
            panel2.Location = new Point(20, 56);
            panel2.Name = "panel2";
            panel2.Size = new Size(360, 180);
            panel2.TabIndex = 221;
            //
            // label6
            //
            label6.AutoSize = true;
            label6.Location = new Point(213, 4);
            label6.Name = "label6";
            label6.Size = new Size(123, 16);
            label6.TabIndex = 203;
            label6.Text = "Mỗi nội dung 1 dòng";
            //
            // textBox1
            //
            textBox1.BorderStyle = BorderStyle.FixedSingle;
            textBox1.Location = new Point(8, 24);
            textBox1.MaxLength = int.MaxValue;
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ScrollBars = ScrollBars.Both;
            textBox1.Size = new Size(346, 150);
            textBox1.TabIndex = 202;
            textBox1.WordWrap = false;
            //
            // label5
            //
            label5.AutoSize = true;
            label5.Location = new Point(8, 4);
            label5.Name = "label5";
            label5.Size = new Size(135, 16);
            label5.TabIndex = 1;
            label5.Text = "Danh sách bài hát (0):";
            //
            // radioButton5
            //
            radioButton5.AutoSize = true;
            radioButton5.Cursor = Cursors.Hand;
            radioButton5.Location = new Point(20, 30);
            radioButton5.Name = "radioButton5";
            radioButton5.Size = new Size(112, 20);
            radioButton5.TabIndex = 220;
            radioButton5.Text = "Bài hát chỉ định";
            radioButton5.UseVisualStyleBackColor = true;
            //
            // radioButton3
            //
            radioButton3.AutoSize = true;
            radioButton3.Checked = true;
            radioButton3.Cursor = Cursors.Hand;
            radioButton3.Location = new Point(20, 5);
            radioButton3.Name = "radioButton3";
            radioButton3.Size = new Size(131, 20);
            radioButton3.TabIndex = 218;
            radioButton3.TabStop = true;
            radioButton3.Text = "Bài hát ngẫu nhiên";
            radioButton3.UseVisualStyleBackColor = true;
            //
            // ckbCoAnh (checkbox Ảnh trong nhánh Đăng nhạc)
            //
            ckbCoAnh.AutoSize = true;
            ckbCoAnh.Checked = true;
            ckbCoAnh.Cursor = Cursors.Hand;
            ckbCoAnh.Location = new Point(40, 520);
            ckbCoAnh.Name = "ckbCoAnh";
            ckbCoAnh.Size = new Size(50, 20);
            ckbCoAnh.TabIndex = 226;
            ckbCoAnh.Text = "Ảnh";
            ckbCoAnh.UseVisualStyleBackColor = true;
            //
            // plAnhNhac
            //
            plAnhNhac.Controls.Add(ckbXoaAnhDaDang);
            plAnhNhac.Controls.Add(btnChonAnhNhac);
            plAnhNhac.Controls.Add(txtPathAnhNhac);
            plAnhNhac.Controls.Add(lblThuMucAnh);
            plAnhNhac.Location = new Point(40, 542);
            plAnhNhac.Name = "plAnhNhac";
            plAnhNhac.Size = new Size(390, 65);
            plAnhNhac.TabIndex = 227;
            //
            // ckbXoaAnhDaDang
            //
            ckbXoaAnhDaDang.AutoSize = true;
            ckbXoaAnhDaDang.Cursor = Cursors.Hand;
            ckbXoaAnhDaDang.Location = new Point(106, 40);
            ckbXoaAnhDaDang.Name = "ckbXoaAnhDaDang";
            ckbXoaAnhDaDang.Size = new Size(125, 20);
            ckbXoaAnhDaDang.TabIndex = 210;
            ckbXoaAnhDaDang.Text = "Xóa ảnh đã đăng";
            ckbXoaAnhDaDang.UseVisualStyleBackColor = true;
            //
            // btnChonAnhNhac
            //
            btnChonAnhNhac.Location = new Point(322, 8);
            btnChonAnhNhac.Name = "btnChonAnhNhac";
            btnChonAnhNhac.Size = new Size(50, 25);
            btnChonAnhNhac.TabIndex = 194;
            btnChonAnhNhac.Text = "Chọn";
            btnChonAnhNhac.UseVisualStyleBackColor = true;
            btnChonAnhNhac.Click += btnChonAnhNhac_Click;
            //
            // txtPathAnhNhac
            //
            txtPathAnhNhac.Location = new Point(106, 9);
            txtPathAnhNhac.Name = "txtPathAnhNhac";
            txtPathAnhNhac.ReadOnly = true;
            txtPathAnhNhac.Size = new Size(211, 23);
            txtPathAnhNhac.TabIndex = 1;
            //
            // lblThuMucAnh
            //
            lblThuMucAnh.AutoSize = true;
            lblThuMucAnh.Location = new Point(15, 12);
            lblThuMucAnh.Name = "lblThuMucAnh";
            lblThuMucAnh.Size = new Size(87, 16);
            lblThuMucAnh.TabIndex = 0;
            lblThuMucAnh.Text = "Thư mục ảnh:";
            //
            // plVanBan (cột phải - nội dung text story)
            //
            plVanBan.BorderStyle = BorderStyle.FixedSingle;
            plVanBan.Controls.Add(D397662B);
            plVanBan.Controls.Add(txtLinks);
            plVanBan.Controls.Add(B1A8A1BB);
            plVanBan.Controls.Add(EE36BE15);
            plVanBan.Controls.Add(E69CEB83);
            plVanBan.Controls.Add(EBA4AC3C);
            plVanBan.Controls.Add(rbNganCachKyTu);
            plVanBan.Controls.Add(ckbXoaNguyenLieuDaDung);
            plVanBan.Controls.Add(checkBox1);
            plVanBan.Controls.Add(ckbSuDungBackground);
            plVanBan.Controls.Add(ckbRandomStickers);
            plVanBan.Controls.Add(ckbRandomEffects);
            plVanBan.Controls.Add(ckbTagNeuBat);
            plVanBan.Controls.Add(ckbTagMoiNguoi);
            plVanBan.Controls.Add(ckbPublic);
            plVanBan.Controls.Add(button2);
            plVanBan.Controls.Add(button3);
            plVanBan.Controls.Add(button4);
            plVanBan.Location = new Point(450, 113);
            plVanBan.Name = "plVanBan";
            plVanBan.Size = new Size(376, 494);
            plVanBan.TabIndex = 33;
            //
            // D397662B
            //
            D397662B.AutoSize = true;
            D397662B.Location = new Point(8, 8);
            D397662B.Name = "D397662B";
            D397662B.Size = new Size(120, 16);
            D397662B.TabIndex = 0;
            D397662B.Text = "Nội dung Story (0):";
            //
            // txtLinks
            //
            txtLinks.BorderStyle = BorderStyle.FixedSingle;
            txtLinks.Location = new Point(8, 28);
            txtLinks.MaxLength = int.MaxValue;
            txtLinks.Multiline = true;
            txtLinks.Name = "txtLinks";
            txtLinks.ScrollBars = ScrollBars.Both;
            txtLinks.Size = new Size(360, 260);
            txtLinks.TabIndex = 201;
            txtLinks.WordWrap = false;
            //
            // B1A8A1BB
            //
            B1A8A1BB.AutoSize = true;
            B1A8A1BB.Location = new Point(8, 296);
            B1A8A1BB.Name = "B1A8A1BB";
            B1A8A1BB.Size = new Size(133, 16);
            B1A8A1BB.TabIndex = 0;
            B1A8A1BB.Text = "Spin nội dung {a|b|c}";
            //
            // EE36BE15
            //
            EE36BE15.AutoSize = true;
            EE36BE15.Cursor = Cursors.Hand;
            EE36BE15.Location = new Point(284, 296);
            EE36BE15.Name = "EE36BE15";
            EE36BE15.Size = new Size(80, 16);
            EE36BE15.TabIndex = 195;
            EE36BE15.TabStop = true;
            EE36BE15.Text = "Random Icon";
            //
            // E69CEB83
            //
            E69CEB83.AutoSize = true;
            E69CEB83.Location = new Point(8, 322);
            E69CEB83.Name = "E69CEB83";
            E69CEB83.Size = new Size(64, 16);
            E69CEB83.TabIndex = 35;
            E69CEB83.Text = "Tùy chọn:";
            //
            // EBA4AC3C
            //
            EBA4AC3C.AutoSize = true;
            EBA4AC3C.Checked = true;
            EBA4AC3C.Cursor = Cursors.Hand;
            EBA4AC3C.Location = new Point(80, 320);
            EBA4AC3C.Name = "EBA4AC3C";
            EBA4AC3C.Size = new Size(155, 20);
            EBA4AC3C.TabIndex = 36;
            EBA4AC3C.TabStop = true;
            EBA4AC3C.Text = "Mỗi nội dung 1 dòng";
            EBA4AC3C.UseVisualStyleBackColor = true;
            //
            // rbNganCachKyTu
            //
            rbNganCachKyTu.AutoSize = true;
            rbNganCachKyTu.Cursor = Cursors.Hand;
            rbNganCachKyTu.Location = new Point(220, 320);
            rbNganCachKyTu.Name = "rbNganCachKyTu";
            rbNganCachKyTu.Size = new Size(159, 20);
            rbNganCachKyTu.TabIndex = 37;
            rbNganCachKyTu.Text = "Nội dung có nhiều dòng";
            rbNganCachKyTu.UseVisualStyleBackColor = true;
            //
            // ckbXoaNguyenLieuDaDung
            //
            ckbXoaNguyenLieuDaDung.AutoSize = true;
            ckbXoaNguyenLieuDaDung.Cursor = Cursors.Hand;
            ckbXoaNguyenLieuDaDung.Location = new Point(8, 350);
            ckbXoaNguyenLieuDaDung.Name = "ckbXoaNguyenLieuDaDung";
            ckbXoaNguyenLieuDaDung.Size = new Size(151, 20);
            ckbXoaNguyenLieuDaDung.TabIndex = 32;
            ckbXoaNguyenLieuDaDung.Text = "Xóa nội dung đã đăng";
            ckbXoaNguyenLieuDaDung.UseVisualStyleBackColor = true;
            //
            // checkBox1 (Trùng lặp nội dung)
            //
            checkBox1.AutoSize = true;
            checkBox1.Checked = true;
            checkBox1.CheckState = CheckState.Checked;
            checkBox1.Cursor = Cursors.Hand;
            checkBox1.Location = new Point(220, 350);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(134, 20);
            checkBox1.TabIndex = 202;
            checkBox1.Text = "Trùng lặp nội dung";
            checkBox1.UseVisualStyleBackColor = true;
            //
            // ckbSuDungBackground
            //
            ckbSuDungBackground.AutoSize = true;
            ckbSuDungBackground.Cursor = Cursors.Hand;
            ckbSuDungBackground.Location = new Point(8, 380);
            ckbSuDungBackground.Name = "ckbSuDungBackground";
            ckbSuDungBackground.Size = new Size(150, 20);
            ckbSuDungBackground.TabIndex = 38;
            ckbSuDungBackground.Text = "Sử dụng background";
            ckbSuDungBackground.UseVisualStyleBackColor = true;
            //
            // ckbRandomStickers
            //
            ckbRandomStickers.AutoSize = true;
            ckbRandomStickers.Cursor = Cursors.Hand;
            ckbRandomStickers.Location = new Point(8, 410);
            ckbRandomStickers.Name = "ckbRandomStickers";
            ckbRandomStickers.Size = new Size(128, 20);
            ckbRandomStickers.TabIndex = 39;
            ckbRandomStickers.Text = "Random Stickers";
            ckbRandomStickers.UseVisualStyleBackColor = true;
            //
            // ckbRandomEffects
            //
            ckbRandomEffects.AutoSize = true;
            ckbRandomEffects.Cursor = Cursors.Hand;
            ckbRandomEffects.Location = new Point(220, 410);
            ckbRandomEffects.Name = "ckbRandomEffects";
            ckbRandomEffects.Size = new Size(122, 20);
            ckbRandomEffects.TabIndex = 40;
            ckbRandomEffects.Text = "Random Effects";
            ckbRandomEffects.UseVisualStyleBackColor = true;
            //
            // ckbTagNeuBat
            //
            ckbTagNeuBat.AutoSize = true;
            ckbTagNeuBat.Cursor = Cursors.Hand;
            ckbTagNeuBat.Location = new Point(8, 440);
            ckbTagNeuBat.Name = "ckbTagNeuBat";
            ckbTagNeuBat.Size = new Size(148, 20);
            ckbTagNeuBat.TabIndex = 41;
            ckbTagNeuBat.Text = "Tag nêu bật/highlight";
            ckbTagNeuBat.UseVisualStyleBackColor = true;
            //
            // ckbTagMoiNguoi
            //
            ckbTagMoiNguoi.AutoSize = true;
            ckbTagMoiNguoi.Cursor = Cursors.Hand;
            ckbTagMoiNguoi.Location = new Point(220, 440);
            ckbTagMoiNguoi.Name = "ckbTagMoiNguoi";
            ckbTagMoiNguoi.Size = new Size(166, 20);
            ckbTagMoiNguoi.TabIndex = 42;
            ckbTagMoiNguoi.Text = "Tag mọi người/followers";
            ckbTagMoiNguoi.UseVisualStyleBackColor = true;
            //
            // ckbPublic
            //
            ckbPublic.AutoSize = true;
            ckbPublic.Cursor = Cursors.Hand;
            ckbPublic.Location = new Point(8, 470);
            ckbPublic.Name = "ckbPublic";
            ckbPublic.Size = new Size(140, 20);
            ckbPublic.TabIndex = 43;
            ckbPublic.Text = "đăng public story";
            ckbPublic.UseVisualStyleBackColor = true;
            //
            // button2 (?)
            //
            button2.Cursor = Cursors.Help;
            button2.Font = new Font("Tahoma", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button2.Location = new Point(345, 348);
            button2.Name = "button2";
            button2.Size = new Size(21, 23);
            button2.TabIndex = 194;
            button2.Text = "?";
            button2.UseVisualStyleBackColor = true;
            //
            // button3 (?)
            //
            button3.Cursor = Cursors.Help;
            button3.Font = new Font("Tahoma", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button3.Location = new Point(345, 318);
            button3.Name = "button3";
            button3.Size = new Size(21, 23);
            button3.TabIndex = 193;
            button3.Text = "?";
            button3.UseVisualStyleBackColor = true;
            //
            // button4 (?)
            //
            button4.Cursor = Cursors.Help;
            button4.Font = new Font("Tahoma", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button4.Location = new Point(155, 320);
            button4.Name = "button4";
            button4.Size = new Size(21, 23);
            button4.TabIndex = 203;
            button4.Text = "?";
            button4.UseVisualStyleBackColor = true;
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
            btnSave.Location = new Point(326, 622);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(92, 29);
            btnSave.TabIndex = 3;
            btnSave.Text = "Thêm";
            btnSave.UseVisualStyleBackColor = false;
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
            btnCancel.Location = new Point(425, 622);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(92, 29);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Đóng";
            btnCancel.UseVisualStyleBackColor = false;
            //
            // fHDDangStory
            //
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(840, 670);
            Controls.Add(panel1);
            Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "fHDDangStory";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Thêm tương tác đăng story";
            rbDangText.CheckedChanged += ckbDefault_CheckedChanged;
            rbDangAnhVideo.CheckedChanged += ckbDefault_CheckedChanged;
            rbDangNhac.CheckedChanged += ckbDefault_CheckedChanged;
            radioButton5.CheckedChanged += ckbDefault_CheckedChanged;
            ckbCoAnh.CheckedChanged += ckbDefault_CheckedChanged;
            txtLinks.TextChanged += txtLinks_TextChanged;
            textBox1.TextChanged += txtLinks_TextChanged;
            btnCancel.Click += btnCancel_Click;
            btnSave.Click += btnSave_Click;
            btn_setting.Click += btnCancel_Click;
            D316AB1E.ResumeLayout(false);
            ((ISupportInitialize)E51CE89C).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            windowBar.ResumeLayout(false);
            plAnh.ResumeLayout(false);
            plAnh.PerformLayout();
            plAnhNhac.ResumeLayout(false);
            plAnhNhac.PerformLayout();
            ((ISupportInitialize)F391713F).EndInit();
            ((ISupportInitialize)C913DC8A).EndInit();
            ((ISupportInitialize)nudKhoangCachFrom).EndInit();
            ((ISupportInitialize)nudKhoangCachTo).EndInit();
            plVanBan.ResumeLayout(false);
            plVanBan.PerformLayout();
            ResumeLayout(false);
        }

        internal Panel panel1;
        internal TextBox txtTenHanhDong;
        internal Label label1;
        internal Button btnCancel;
        internal Button btnSave;
        internal Panel D316AB1E;
        internal Button button1;
        internal PictureBox E51CE89C;
        internal Panel plVanBan;
        internal Label B1A8A1BB;
        internal Label D397662B;
        internal RadioButton rbNganCachKyTu;
        internal RadioButton EBA4AC3C;
        internal Label E69CEB83;
        internal NumericUpDown F391713F;
        internal NumericUpDown C913DC8A;
        internal Label label17;
        internal Label FD150C04;
        internal Label E9B44A22;
        internal CheckBox ckbXoaNguyenLieuDaDung;
        internal LinkLabel EE36BE15;
        internal Button button3;
        internal Button button2;
        internal Panel plAnh;
        internal TextBox txtPathAnh;
        internal Label label3;
        #endregion

        private Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel windowBar;
        private Button btn_mode;
        private Button btn_global;
        private Button btn_setting;
        internal Button button4;
        internal CheckBox checkBox1;
        private TextBox txtLinks;
        internal CheckBox checkBox4;
        private Button button5;
        internal Panel panel3;
        internal Panel panel2;
        internal Label label6;
        private TextBox textBox1;
        internal Label label5;
        internal RadioButton radioButton5;
        internal RadioButton radioButton3;
        internal RadioButton rbDangText;
        internal RadioButton rbDangNhac;
        internal RadioButton rbDangAnhVideo;
        internal CheckBox ckbCoAnh;
        internal Panel plAnhNhac;
        internal CheckBox ckbXoaAnhDaDang;
        internal Button btnChonAnhNhac;
        internal TextBox txtPathAnhNhac;
        internal Label lblThuMucAnh;
        internal CheckBox ckbSuDungBackground;
        internal CheckBox ckbRandomStickers;
        internal CheckBox ckbRandomEffects;
        internal CheckBox ckbTagNeuBat;
        internal CheckBox ckbTagMoiNguoi;
        internal CheckBox ckbPublic;
        internal Label lblKhoangCach;
        internal NumericUpDown nudKhoangCachFrom;
        internal Label lblKhoangCachDen;
        internal NumericUpDown nudKhoangCachTo;
        internal Label lblKhoangCachGiay;
    }
}
