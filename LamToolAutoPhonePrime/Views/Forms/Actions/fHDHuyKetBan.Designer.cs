using System.ComponentModel;

namespace LamToolAutoPhonePrime.Views.Forms.Actions
{
    partial class fHDHuyKetBan
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
            pictureBox1 = new PictureBox();
            panel1 = new Panel();
            windowBar = new AntdUI.PageHeader();
            btn_mode = new AntdUI.Button();
            btn_global = new AntdUI.Button();
            btn_setting = new AntdUI.Button();
            C807351D = new Panel();
            txtLinks = new TextBox();
            DA352D8D = new Label();
            F3B4CEA2 = new Panel();
            B8049630 = new Label();
            label4 = new Label();
            DFB4FB3D = new Label();
            nudSoLuongFrom = new NumericUpDown();
            nudSoLuongTo = new NumericUpDown();
            C2854635 = new RadioButton();
            FD1B0825 = new RadioButton();
            nudDelayTo = new NumericUpDown();
            nudDelayFrom = new NumericUpDown();
            label7 = new Label();
            label6 = new Label();
            label8 = new Label();
            label5 = new Label();
            txtTenHanhDong = new TextBox();
            label1 = new Label();
            btnCancel = new Button();
            btnSave = new Button();
            pnlHeader.SuspendLayout();
            ((ISupportInitialize)pictureBox1).BeginInit();
            panel1.SuspendLayout();
            windowBar.SuspendLayout();
            C807351D.SuspendLayout();
            F3B4CEA2.SuspendLayout();
            ((ISupportInitialize)nudSoLuongFrom).BeginInit();
            ((ISupportInitialize)nudSoLuongTo).BeginInit();
            ((ISupportInitialize)nudDelayTo).BeginInit();
            ((ISupportInitialize)nudDelayFrom).BeginInit();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlHeader.BackColor = Color.White;
            pnlHeader.Controls.Add(button1);
            pnlHeader.Controls.Add(pictureBox1);
            pnlHeader.Cursor = Cursors.SizeAll;
            pnlHeader.Location = new Point(0, 3);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(645, 31);
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
            button1.Location = new Point(614, 1);
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
            panel1.Controls.Add(C807351D);
            panel1.Controls.Add(F3B4CEA2);
            panel1.Controls.Add(C2854635);
            panel1.Controls.Add(FD1B0825);
            panel1.Controls.Add(nudDelayTo);
            panel1.Controls.Add(nudDelayFrom);
            panel1.Controls.Add(label7);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(label8);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(txtTenHanhDong);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(btnCancel);
            panel1.Controls.Add(btnSave);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(362, 411);
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
            // C807351D
            // 
            C807351D.BorderStyle = BorderStyle.FixedSingle;
            C807351D.Controls.Add(txtLinks);
            C807351D.Controls.Add(DA352D8D);
            C807351D.Location = new Point(59, 208);
            C807351D.Name = "C807351D";
            C807351D.Size = new Size(267, 134);
            C807351D.TabIndex = 118;
            // 
            // txtLinks
            // 
            txtLinks.BorderStyle = BorderStyle.FixedSingle;
            txtLinks.Location = new Point(0, 22);
            txtLinks.MaxLength = int.MaxValue;
            txtLinks.Multiline = true;
            txtLinks.Name = "txtLinks";
            txtLinks.ScrollBars = ScrollBars.Both;
            txtLinks.Size = new Size(266, 111);
            txtLinks.TabIndex = 201;
            txtLinks.WordWrap = false;
            // 
            // DA352D8D
            // 
            DA352D8D.AutoSize = true;
            DA352D8D.Location = new Point(3, 3);
            DA352D8D.Name = "DA352D8D";
            DA352D8D.Size = new Size(208, 16);
            DA352D8D.TabIndex = 116;
            DA352D8D.Text = "Danh sách Uid cần hủy kết bạn (0):";
            // 
            // F3B4CEA2
            // 
            F3B4CEA2.BorderStyle = BorderStyle.FixedSingle;
            F3B4CEA2.Controls.Add(B8049630);
            F3B4CEA2.Controls.Add(label4);
            F3B4CEA2.Controls.Add(DFB4FB3D);
            F3B4CEA2.Controls.Add(nudSoLuongFrom);
            F3B4CEA2.Controls.Add(nudSoLuongTo);
            F3B4CEA2.Location = new Point(59, 155);
            F3B4CEA2.Name = "F3B4CEA2";
            F3B4CEA2.Size = new Size(267, 27);
            F3B4CEA2.TabIndex = 118;
            // 
            // B8049630
            // 
            B8049630.AutoSize = true;
            B8049630.Location = new Point(3, 3);
            B8049630.Name = "B8049630";
            B8049630.Size = new Size(63, 16);
            B8049630.TabIndex = 32;
            B8049630.Text = "Số lượng:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(225, 3);
            label4.Name = "label4";
            label4.Size = new Size(28, 16);
            label4.TabIndex = 35;
            label4.Text = "bạn";
            // 
            // DFB4FB3D
            // 
            DFB4FB3D.Location = new Point(135, 3);
            DFB4FB3D.Name = "DFB4FB3D";
            DFB4FB3D.Size = new Size(29, 16);
            DFB4FB3D.TabIndex = 37;
            DFB4FB3D.Text = "đến";
            DFB4FB3D.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // nudSoLuongFrom
            // 
            nudSoLuongFrom.Location = new Point(73, 1);
            nudSoLuongFrom.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudSoLuongFrom.Name = "nudSoLuongFrom";
            nudSoLuongFrom.Size = new Size(56, 23);
            nudSoLuongFrom.TabIndex = 1;
            nudSoLuongFrom.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // nudSoLuongTo
            // 
            nudSoLuongTo.Location = new Point(167, 1);
            nudSoLuongTo.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudSoLuongTo.Name = "nudSoLuongTo";
            nudSoLuongTo.Size = new Size(56, 23);
            nudSoLuongTo.TabIndex = 2;
            nudSoLuongTo.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // C2854635
            // 
            C2854635.AutoSize = true;
            C2854635.Cursor = Cursors.Hand;
            C2854635.Location = new Point(46, 185);
            C2854635.Name = "C2854635";
            C2854635.Size = new Size(145, 20);
            C2854635.TabIndex = 47;
            C2854635.Text = "Hủy kết bạn theo UID";
            C2854635.UseVisualStyleBackColor = true;
            // 
            // FD1B0825
            // 
            FD1B0825.AutoSize = true;
            FD1B0825.Checked = true;
            FD1B0825.Cursor = Cursors.Hand;
            FD1B0825.Location = new Point(46, 132);
            FD1B0825.Name = "FD1B0825";
            FD1B0825.Size = new Size(176, 20);
            FD1B0825.TabIndex = 47;
            FD1B0825.TabStop = true;
            FD1B0825.Text = "Ngẫu nhiên danh sách bạn";
            FD1B0825.UseVisualStyleBackColor = true;
            // 
            // nudDelayTo
            // 
            nudDelayTo.Location = new Point(226, 78);
            nudDelayTo.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudDelayTo.Name = "nudDelayTo";
            nudDelayTo.Size = new Size(56, 23);
            nudDelayTo.TabIndex = 4;
            nudDelayTo.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // nudDelayFrom
            // 
            nudDelayFrom.Location = new Point(132, 78);
            nudDelayFrom.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudDelayFrom.Name = "nudDelayFrom";
            nudDelayFrom.Size = new Size(56, 23);
            nudDelayFrom.TabIndex = 3;
            nudDelayFrom.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // label7
            // 
            label7.Location = new Point(192, 80);
            label7.Name = "label7";
            label7.Size = new Size(29, 16);
            label7.TabIndex = 46;
            label7.Text = "đến";
            label7.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(285, 80);
            label6.Name = "label6";
            label6.Size = new Size(30, 16);
            label6.TabIndex = 45;
            label6.Text = "giây";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(27, 110);
            label8.Name = "label8";
            label8.Size = new Size(134, 16);
            label8.TabIndex = 44;
            label8.Text = "Tùy chọn hủy kết bạn:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(27, 80);
            label5.Name = "label5";
            label5.Size = new Size(89, 16);
            label5.TabIndex = 44;
            label5.Text = "Thời gian chờ:";
            // 
            // txtTenHanhDong
            // 
            txtTenHanhDong.Location = new Point(132, 49);
            txtTenHanhDong.Name = "txtTenHanhDong";
            txtTenHanhDong.Size = new Size(194, 23);
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
            btnCancel.Location = new Point(208, 364);
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
            btnSave.Location = new Point(101, 364);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(92, 29);
            btnSave.TabIndex = 6;
            btnSave.Text = "Thêm";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // fHDHuyKetBan
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(362, 411);
            Controls.Add(panel1);
            Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "fHDHuyKetBan";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Cấu hình tương tác";
            pnlHeader.ResumeLayout(false);
            ((ISupportInitialize)pictureBox1).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            windowBar.ResumeLayout(false);
            C807351D.ResumeLayout(false);
            C807351D.PerformLayout();
            F3B4CEA2.ResumeLayout(false);
            F3B4CEA2.PerformLayout();
            ((ISupportInitialize)nudSoLuongFrom).EndInit();
            ((ISupportInitialize)nudSoLuongTo).EndInit();
            ((ISupportInitialize)nudDelayTo).EndInit();
            ((ISupportInitialize)nudDelayFrom).EndInit();
            ResumeLayout(false);
        }
        internal Panel panel1;

        internal NumericUpDown nudSoLuongTo;

        internal NumericUpDown nudSoLuongFrom;

        internal TextBox txtTenHanhDong;

        internal Label DFB4FB3D;

        internal Label label4;

        internal Label B8049630;

        internal Label label1;

        internal Button btnCancel;

        internal Button btnSave;

        internal Panel pnlHeader;

        internal Button button1;

        internal PictureBox pictureBox1;

        internal NumericUpDown nudDelayTo;

        internal NumericUpDown nudDelayFrom;

        internal Label label7;

        internal Label label6;

        internal Label label5;

        internal RadioButton C2854635;

        internal RadioButton FD1B0825;

        internal Label label8;

        internal Panel C807351D;

        internal Label DA352D8D;

        internal Panel F3B4CEA2;

        #endregion

        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_mode;
        private AntdUI.Button btn_global;
        private AntdUI.Button btn_setting;
        private TextBox txtLinks;
    }
}