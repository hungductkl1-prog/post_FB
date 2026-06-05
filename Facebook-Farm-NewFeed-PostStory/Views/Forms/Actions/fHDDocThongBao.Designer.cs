using System.ComponentModel;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions
{
    partial class fHDDocThongBao
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
            DE80B53A = new Button();
            E62B6A03 = new PictureBox();
            panel1 = new Panel();
            windowBar = new AntdUI.PageHeader();
            btn_mode = new AntdUI.Button();
            btn_global = new AntdUI.Button();
            btn_setting = new AntdUI.Button();
            ckbXoaThongBaoSpam = new CheckBox();
            nudDelayTo = new NumericUpDown();
            nudSoLuongTo = new NumericUpDown();
            nudDelayFrom = new NumericUpDown();
            nudSoLuongFrom = new NumericUpDown();
            txtTenHanhDong = new TextBox();
            label7 = new Label();
            label3 = new Label();
            label5 = new Label();
            label2 = new Label();
            label1 = new Label();
            btnCancel = new Button();
            btnSave = new Button();
            pnlHeader.SuspendLayout();
            ((ISupportInitialize)E62B6A03).BeginInit();
            panel1.SuspendLayout();
            windowBar.SuspendLayout();
            ((ISupportInitialize)nudDelayTo).BeginInit();
            ((ISupportInitialize)nudSoLuongTo).BeginInit();
            ((ISupportInitialize)nudDelayFrom).BeginInit();
            ((ISupportInitialize)nudSoLuongFrom).BeginInit();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlHeader.BackColor = Color.White;
            pnlHeader.Controls.Add(DE80B53A);
            pnlHeader.Controls.Add(E62B6A03);
            pnlHeader.Cursor = Cursors.SizeAll;
            pnlHeader.Location = new Point(0, 3);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(314, 31);
            pnlHeader.TabIndex = 9;
            // 
            // DE80B53A
            // 
            DE80B53A.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            DE80B53A.Cursor = Cursors.Hand;
            DE80B53A.FlatAppearance.BorderSize = 0;
            DE80B53A.FlatStyle = FlatStyle.Flat;
            DE80B53A.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            DE80B53A.ForeColor = Color.White;
            DE80B53A.Location = new Point(283, 1);
            DE80B53A.Name = "DE80B53A";
            DE80B53A.Size = new Size(30, 30);
            DE80B53A.TabIndex = 77;
            DE80B53A.TextImageRelation = TextImageRelation.ImageBeforeText;
            DE80B53A.UseVisualStyleBackColor = true;
            // 
            // E62B6A03
            // 
            E62B6A03.Location = new Point(3, 2);
            E62B6A03.Name = "E62B6A03";
            E62B6A03.Size = new Size(34, 27);
            E62B6A03.SizeMode = PictureBoxSizeMode.Zoom;
            E62B6A03.TabIndex = 76;
            E62B6A03.TabStop = false;
            // 
            // panel1
            // 
            panel1.BackColor = Color.White;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(windowBar);
            panel1.Controls.Add(ckbXoaThongBaoSpam);
            panel1.Controls.Add(nudDelayTo);
            panel1.Controls.Add(nudSoLuongTo);
            panel1.Controls.Add(nudDelayFrom);
            panel1.Controls.Add(nudSoLuongFrom);
            panel1.Controls.Add(txtTenHanhDong);
            panel1.Controls.Add(label7);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(btnCancel);
            panel1.Controls.Add(btnSave);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(317, 213);
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
            windowBar.Size = new Size(315, 35);
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
            btn_mode.Location = new Point(235, 0);
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
            btn_global.Location = new Point(261, 0);
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
            btn_setting.Location = new Point(285, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(30, 35);
            btn_setting.TabIndex = 8;
            btn_setting.WaveSize = 0;
            // 
            // ckbXoaThongBaoSpam
            // 
            ckbXoaThongBaoSpam.AutoSize = true;
            ckbXoaThongBaoSpam.Location = new Point(132, 136);
            ckbXoaThongBaoSpam.Name = "ckbXoaThongBaoSpam";
            ckbXoaThongBaoSpam.Size = new Size(144, 20);
            ckbXoaThongBaoSpam.TabIndex = 39;
            ckbXoaThongBaoSpam.Text = "Xóa thông báo spam";
            ckbXoaThongBaoSpam.UseVisualStyleBackColor = true;
            // 
            // nudDelayTo
            // 
            nudDelayTo.Location = new Point(226, 107);
            nudDelayTo.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudDelayTo.Name = "nudDelayTo";
            nudDelayTo.Size = new Size(56, 23);
            nudDelayTo.TabIndex = 6;
            // 
            // nudSoLuongTo
            // 
            nudSoLuongTo.Location = new Point(226, 78);
            nudSoLuongTo.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudSoLuongTo.Name = "nudSoLuongTo";
            nudSoLuongTo.Size = new Size(56, 23);
            nudSoLuongTo.TabIndex = 2;
            // 
            // nudDelayFrom
            // 
            nudDelayFrom.Location = new Point(132, 107);
            nudDelayFrom.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudDelayFrom.Name = "nudDelayFrom";
            nudDelayFrom.Size = new Size(56, 23);
            nudDelayFrom.TabIndex = 5;
            // 
            // nudSoLuongFrom
            // 
            nudSoLuongFrom.Location = new Point(132, 78);
            nudSoLuongFrom.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudSoLuongFrom.Name = "nudSoLuongFrom";
            nudSoLuongFrom.Size = new Size(56, 23);
            nudSoLuongFrom.TabIndex = 1;
            // 
            // txtTenHanhDong
            // 
            txtTenHanhDong.Location = new Point(132, 49);
            txtTenHanhDong.Name = "txtTenHanhDong";
            txtTenHanhDong.Size = new Size(150, 23);
            txtTenHanhDong.TabIndex = 0;
            // 
            // label7
            // 
            label7.Font = new Font("Tahoma", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label7.Location = new Point(192, 109);
            label7.Name = "label7";
            label7.Size = new Size(29, 16);
            label7.TabIndex = 38;
            label7.Text = ">";
            label7.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            label3.Font = new Font("Tahoma", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(192, 80);
            label3.Name = "label3";
            label3.Size = new Size(29, 16);
            label3.TabIndex = 37;
            label3.Text = ">";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(17, 109);
            label5.Name = "label5";
            label5.Size = new Size(109, 16);
            label5.TabIndex = 34;
            label5.Text = "Thời gian chờ (s):";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(17, 80);
            label2.Name = "label2";
            label2.Size = new Size(63, 16);
            label2.TabIndex = 32;
            label2.Text = "Số lượng:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(17, 52);
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
            btnCancel.Location = new Point(167, 169);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(92, 29);
            btnCancel.TabIndex = 8;
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
            btnSave.Location = new Point(63, 169);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(92, 29);
            btnSave.TabIndex = 7;
            btnSave.Text = "Thêm";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // fHDDocThongBao
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(317, 213);
            Controls.Add(panel1);
            Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "fHDDocThongBao";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Cấu hình tương tác";
            Load += fHDDocThongBao_Load;
            btnCancel.Click += btnCancel_Click;
            btnSave.Click += btnSave_Click;
            btn_setting.Click += btnCancel_Click;
            pnlHeader.ResumeLayout(false);
            ((ISupportInitialize)E62B6A03).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            windowBar.ResumeLayout(false);
            ((ISupportInitialize)nudDelayTo).EndInit();
            ((ISupportInitialize)nudSoLuongTo).EndInit();
            ((ISupportInitialize)nudDelayFrom).EndInit();
            ((ISupportInitialize)nudSoLuongFrom).EndInit();
            ResumeLayout(false);
        }
        internal Panel panel1;

        internal NumericUpDown nudDelayTo;

        internal NumericUpDown nudSoLuongTo;

        internal NumericUpDown nudDelayFrom;

        internal NumericUpDown nudSoLuongFrom;

        internal TextBox txtTenHanhDong;

        internal Label label7;

        internal Label label3;

        internal Label label5;

        internal Label label2;

        internal Label label1;

        internal Button btnCancel;

        internal Button btnSave;

        internal Panel pnlHeader;

        internal Button DE80B53A;

        internal PictureBox E62B6A03;

        internal CheckBox ckbXoaThongBaoSpam;
        #endregion

        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_mode;
        private AntdUI.Button btn_global;
        private AntdUI.Button btn_setting;
    }
}