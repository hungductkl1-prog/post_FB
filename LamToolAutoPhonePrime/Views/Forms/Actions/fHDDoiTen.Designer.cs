using System.ComponentModel;

namespace LamToolAutoPhonePrime.Views.Forms.Actions
{
    partial class fHDDoiTen
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
            button1 = new Button();
            CA09732E = new PictureBox();
            A18B8922 = new Panel();
            windowBar = new AntdUI.PageHeader();
            btn_mode = new AntdUI.Button();
            btn_global = new AntdUI.Button();
            btn_setting = new AntdUI.Button();
            EB16A51C = new Panel();
            rdTenTuDat = new RadioButton();
            rdTenRandom = new RadioButton();
            CBA7C20B = new Panel();
            DB3142A7 = new RadioButton();
            rdTenRandomViet = new RadioButton();
            txtTenHanhDong = new TextBox();
            label1 = new Label();
            btnCancel = new Button();
            btnSave = new Button();
            D397662B = new Label();
            txtLinks = new TextBox();
            label2 = new Label();
            textBox1 = new TextBox();
            label3 = new Label();
            textBox2 = new TextBox();
            ((ISupportInitialize)CA09732E).BeginInit();
            A18B8922.SuspendLayout();
            windowBar.SuspendLayout();
            EB16A51C.SuspendLayout();
            CBA7C20B.SuspendLayout();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button1.Cursor = Cursors.Hand;
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button1.ForeColor = Color.White;
            button1.Location = new Point(289, 1);
            button1.Name = "button1";
            button1.Size = new Size(30, 30);
            button1.TabIndex = 77;
            button1.TextImageRelation = TextImageRelation.ImageBeforeText;
            button1.UseVisualStyleBackColor = true;
            // 
            // CA09732E
            // 
            CA09732E.Location = new Point(3, 2);
            CA09732E.Name = "CA09732E";
            CA09732E.Size = new Size(34, 27);
            CA09732E.SizeMode = PictureBoxSizeMode.Zoom;
            CA09732E.TabIndex = 76;
            CA09732E.TabStop = false;
            // 
            // A18B8922
            // 
            A18B8922.BackColor = Color.White;
            A18B8922.BorderStyle = BorderStyle.FixedSingle;
            A18B8922.Controls.Add(windowBar);
            A18B8922.Controls.Add(EB16A51C);
            A18B8922.Controls.Add(rdTenTuDat);
            A18B8922.Controls.Add(rdTenRandom);
            A18B8922.Controls.Add(CBA7C20B);
            A18B8922.Controls.Add(txtTenHanhDong);
            A18B8922.Controls.Add(label1);
            A18B8922.Controls.Add(btnCancel);
            A18B8922.Controls.Add(btnSave);
            A18B8922.Dock = DockStyle.Fill;
            A18B8922.Location = new Point(0, 0);
            A18B8922.Name = "A18B8922";
            A18B8922.Size = new Size(458, 568);
            A18B8922.TabIndex = 0;
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
            windowBar.Size = new Size(456, 35);
            windowBar.SubFont = new Font("Microsoft Sans Serif", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            windowBar.SubGap = 1;
            windowBar.SubText = "";
            windowBar.TabIndex = 140;
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
            btn_mode.Location = new Point(376, 0);
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
            btn_global.Location = new Point(402, 0);
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
            btn_setting.Location = new Point(426, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(30, 35);
            btn_setting.TabIndex = 8;
            btn_setting.WaveSize = 0;
            // 
            // EB16A51C
            // 
            EB16A51C.BorderStyle = BorderStyle.FixedSingle;
            EB16A51C.Controls.Add(textBox2);
            EB16A51C.Controls.Add(label3);
            EB16A51C.Controls.Add(textBox1);
            EB16A51C.Controls.Add(label2);
            EB16A51C.Controls.Add(txtLinks);
            EB16A51C.Controls.Add(D397662B);
            EB16A51C.Location = new Point(67, 165);
            EB16A51C.Name = "EB16A51C";
            EB16A51C.Size = new Size(368, 342);
            EB16A51C.TabIndex = 136;
            // 
            // rdTenTuDat
            // 
            rdTenTuDat.AutoSize = true;
            rdTenTuDat.Checked = true;
            rdTenTuDat.Cursor = Cursors.Hand;
            rdTenTuDat.Location = new Point(51, 139);
            rdTenTuDat.Name = "rdTenTuDat";
            rdTenTuDat.Size = new Size(155, 20);
            rdTenTuDat.TabIndex = 138;
            rdTenTuDat.TabStop = true;
            rdTenTuDat.Text = "Tên do người dùng đặt";
            rdTenTuDat.UseVisualStyleBackColor = true;
            // 
            // rdTenRandom
            // 
            rdTenRandom.AutoSize = true;
            rdTenRandom.Cursor = Cursors.Hand;
            rdTenRandom.Location = new Point(51, 78);
            rdTenRandom.Name = "rdTenRandom";
            rdTenRandom.Size = new Size(114, 20);
            rdTenRandom.TabIndex = 137;
            rdTenRandom.Text = "Tên ngẫu nhiên";
            rdTenRandom.UseVisualStyleBackColor = true;
            // 
            // CBA7C20B
            // 
            CBA7C20B.BorderStyle = BorderStyle.FixedSingle;
            CBA7C20B.Controls.Add(DB3142A7);
            CBA7C20B.Controls.Add(rdTenRandomViet);
            CBA7C20B.Location = new Point(67, 104);
            CBA7C20B.Name = "CBA7C20B";
            CBA7C20B.Size = new Size(368, 25);
            CBA7C20B.TabIndex = 139;
            // 
            // DB3142A7
            // 
            DB3142A7.AutoSize = true;
            DB3142A7.Cursor = Cursors.Hand;
            DB3142A7.Location = new Point(208, 1);
            DB3142A7.Name = "DB3142A7";
            DB3142A7.Size = new Size(82, 20);
            DB3142A7.TabIndex = 134;
            DB3142A7.Text = "Tên ngoại";
            DB3142A7.UseVisualStyleBackColor = true;
            // 
            // rdTenRandomViet
            // 
            rdTenRandomViet.AutoSize = true;
            rdTenRandomViet.Checked = true;
            rdTenRandomViet.Cursor = Cursors.Hand;
            rdTenRandomViet.Location = new Point(57, 1);
            rdTenRandomViet.Name = "rdTenRandomViet";
            rdTenRandomViet.Size = new Size(71, 20);
            rdTenRandomViet.TabIndex = 134;
            rdTenRandomViet.TabStop = true;
            rdTenRandomViet.Text = "Tên việt";
            rdTenRandomViet.UseVisualStyleBackColor = true;
            // 
            // txtTenHanhDong
            // 
            txtTenHanhDong.Location = new Point(130, 49);
            txtTenHanhDong.Name = "txtTenHanhDong";
            txtTenHanhDong.Size = new Size(283, 23);
            txtTenHanhDong.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(26, 52);
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
            btnCancel.Location = new Point(236, 523);
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
            btnSave.Location = new Point(129, 523);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(92, 29);
            btnSave.TabIndex = 6;
            btnSave.Text = "Thêm";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // D397662B
            // 
            D397662B.AutoSize = true;
            D397662B.Location = new Point(5, 4);
            D397662B.Name = "D397662B";
            D397662B.Size = new Size(110, 16);
            D397662B.TabIndex = 1;
            D397662B.Text = "Danh sách họ (0):";
            // 
            // txtLinks
            // 
            txtLinks.BorderStyle = BorderStyle.FixedSingle;
            txtLinks.Location = new Point(5, 23);
            txtLinks.MaxLength = int.MaxValue;
            txtLinks.Multiline = true;
            txtLinks.Name = "txtLinks";
            txtLinks.ScrollBars = ScrollBars.Both;
            txtLinks.Size = new Size(358, 89);
            txtLinks.TabIndex = 202;
            txtLinks.WordWrap = false;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(5, 115);
            label2.Name = "label2";
            label2.Size = new Size(143, 16);
            label2.TabIndex = 203;
            label2.Text = "Danh sách tên đệm (0):";
            // 
            // textBox1
            // 
            textBox1.BorderStyle = BorderStyle.FixedSingle;
            textBox1.Location = new Point(5, 134);
            textBox1.MaxLength = int.MaxValue;
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ScrollBars = ScrollBars.Both;
            textBox1.Size = new Size(358, 89);
            textBox1.TabIndex = 204;
            textBox1.WordWrap = false;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(5, 226);
            label3.Name = "label3";
            label3.Size = new Size(114, 16);
            label3.TabIndex = 205;
            label3.Text = "Danh sách tên (0):";
            // 
            // textBox2
            // 
            textBox2.BorderStyle = BorderStyle.FixedSingle;
            textBox2.Location = new Point(5, 245);
            textBox2.MaxLength = int.MaxValue;
            textBox2.Multiline = true;
            textBox2.Name = "textBox2";
            textBox2.ScrollBars = ScrollBars.Both;
            textBox2.Size = new Size(358, 89);
            textBox2.TabIndex = 206;
            textBox2.WordWrap = false;
            // 
            // fHDDoiTen
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(458, 568);
            Controls.Add(A18B8922);
            Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "fHDDoiTen";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Cấu hình tương tác";
            ((ISupportInitialize)CA09732E).EndInit();
            A18B8922.ResumeLayout(false);
            A18B8922.PerformLayout();
            windowBar.ResumeLayout(false);
            EB16A51C.ResumeLayout(false);
            EB16A51C.PerformLayout();
            CBA7C20B.ResumeLayout(false);
            CBA7C20B.PerformLayout();
            ResumeLayout(false);
        }
        internal Panel A18B8922;

        internal TextBox txtTenHanhDong;

        internal Label label1;

        internal Button btnCancel;

        internal Button btnSave;

        internal Panel E31158AA;

        internal Button button1;

        internal PictureBox CA09732E;

        internal Panel EB16A51C;

        internal RadioButton rdTenTuDat;

        internal RadioButton rdTenRandom;

        internal Panel CBA7C20B;

        internal RadioButton DB3142A7;

        internal RadioButton rdTenRandomViet;
        #endregion

        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_mode;
        private AntdUI.Button btn_global;
        private AntdUI.Button btn_setting;
        internal Label D397662B;
        private TextBox textBox2;
        internal Label label3;
        private TextBox textBox1;
        internal Label label2;
        private TextBox txtLinks;
    }
}