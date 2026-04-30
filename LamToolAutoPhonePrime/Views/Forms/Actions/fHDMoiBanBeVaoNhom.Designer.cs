using System.ComponentModel;

namespace LamToolAutoPhonePrime.Views.Forms.Actions
{
    partial class fHDMoiBanBeVaoNhom
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
            E52056A5 = new NumericUpDown();
            nudSoLuongTo = new NumericUpDown();
            A4902299 = new NumericUpDown();
            nudSoLuongFrom = new NumericUpDown();
            txtTenHanhDong = new TextBox();
            label9 = new Label();
            label3 = new Label();
            E69D4209 = new Label();
            label4 = new Label();
            label6 = new Label();
            FCB5EAA1 = new Label();
            B3050320 = new Label();
            btnCancel = new Button();
            btnSave = new Button();
            C92DE629 = new Label();
            txtComments = new TextBox();
            pnlHeader.SuspendLayout();
            ((ISupportInitialize)pictureBox1).BeginInit();
            panel1.SuspendLayout();
            windowBar.SuspendLayout();
            ((ISupportInitialize)E52056A5).BeginInit();
            ((ISupportInitialize)nudSoLuongTo).BeginInit();
            ((ISupportInitialize)A4902299).BeginInit();
            ((ISupportInitialize)nudSoLuongFrom).BeginInit();
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
            panel1.Controls.Add(txtComments);
            panel1.Controls.Add(C92DE629);
            panel1.Controls.Add(windowBar);
            panel1.Controls.Add(E52056A5);
            panel1.Controls.Add(nudSoLuongTo);
            panel1.Controls.Add(A4902299);
            panel1.Controls.Add(nudSoLuongFrom);
            panel1.Controls.Add(txtTenHanhDong);
            panel1.Controls.Add(label9);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(E69D4209);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(FCB5EAA1);
            panel1.Controls.Add(B3050320);
            panel1.Controls.Add(btnCancel);
            panel1.Controls.Add(btnSave);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(363, 392);
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
            windowBar.Size = new Size(361, 35);
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
            btn_mode.Location = new Point(281, 0);
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
            btn_global.Location = new Point(307, 0);
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
            btn_setting.Location = new Point(331, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(30, 35);
            btn_setting.TabIndex = 8;
            btn_setting.WaveSize = 0;
            // 
            // E52056A5
            // 
            E52056A5.Location = new Point(229, 109);
            E52056A5.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            E52056A5.Name = "E52056A5";
            E52056A5.Size = new Size(56, 23);
            E52056A5.TabIndex = 2;
            E52056A5.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // nudSoLuongTo
            // 
            nudSoLuongTo.Location = new Point(229, 80);
            nudSoLuongTo.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudSoLuongTo.Name = "nudSoLuongTo";
            nudSoLuongTo.Size = new Size(56, 23);
            nudSoLuongTo.TabIndex = 2;
            nudSoLuongTo.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // A4902299
            // 
            A4902299.Location = new Point(132, 109);
            A4902299.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            A4902299.Name = "A4902299";
            A4902299.Size = new Size(56, 23);
            A4902299.TabIndex = 1;
            A4902299.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // nudSoLuongFrom
            // 
            nudSoLuongFrom.Location = new Point(132, 80);
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
            // label9
            // 
            label9.Location = new Point(194, 111);
            label9.Name = "label9";
            label9.Size = new Size(29, 16);
            label9.TabIndex = 37;
            label9.Text = "đến";
            label9.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            label3.Location = new Point(194, 82);
            label3.Name = "label3";
            label3.Size = new Size(29, 16);
            label3.TabIndex = 37;
            label3.Text = "đến";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // E69D4209
            // 
            E69D4209.AutoSize = true;
            E69D4209.Location = new Point(287, 111);
            E69D4209.Name = "E69D4209";
            E69D4209.Size = new Size(30, 16);
            E69D4209.TabIndex = 35;
            E69D4209.Text = "giây";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(287, 82);
            label4.Name = "label4";
            label4.Size = new Size(28, 16);
            label4.TabIndex = 35;
            label4.Text = "bạn";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(27, 111);
            label6.Name = "label6";
            label6.Size = new Size(99, 16);
            label6.TabIndex = 32;
            label6.Text = "Thời gian delay:";
            // 
            // FCB5EAA1
            // 
            FCB5EAA1.AutoSize = true;
            FCB5EAA1.Location = new Point(27, 82);
            FCB5EAA1.Name = "FCB5EAA1";
            FCB5EAA1.Size = new Size(88, 16);
            FCB5EAA1.TabIndex = 32;
            FCB5EAA1.Text = "Số lượng mời:";
            // 
            // B3050320
            // 
            B3050320.AutoSize = true;
            B3050320.Location = new Point(27, 52);
            B3050320.Name = "B3050320";
            B3050320.Size = new Size(98, 16);
            B3050320.TabIndex = 31;
            B3050320.Text = "Tên hành động:";
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
            btnCancel.Location = new Point(191, 352);
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
            btnSave.Location = new Point(80, 352);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(92, 29);
            btnSave.TabIndex = 6;
            btnSave.Text = "Thêm";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // C92DE629
            // 
            C92DE629.AutoSize = true;
            C92DE629.Location = new Point(28, 146);
            C92DE629.Name = "C92DE629";
            C92DE629.Size = new Size(199, 16);
            C92DE629.TabIndex = 120;
            C92DE629.Text = "Danh sách Uid nhóm cần mời (0):";
            // 
            // txtComments
            // 
            txtComments.BorderStyle = BorderStyle.FixedSingle;
            txtComments.Location = new Point(27, 165);
            txtComments.MaxLength = int.MaxValue;
            txtComments.Multiline = true;
            txtComments.Name = "txtComments";
            txtComments.ScrollBars = ScrollBars.Both;
            txtComments.Size = new Size(321, 168);
            txtComments.TabIndex = 199;
            txtComments.WordWrap = false;
            // 
            // fHDMoiBanBeVaoNhom
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(363, 392);
            Controls.Add(panel1);
            Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "fHDMoiBanBeVaoNhom";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Cấu hình tương tác";
            pnlHeader.ResumeLayout(false);
            ((ISupportInitialize)pictureBox1).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            windowBar.ResumeLayout(false);
            ((ISupportInitialize)E52056A5).EndInit();
            ((ISupportInitialize)nudSoLuongTo).EndInit();
            ((ISupportInitialize)A4902299).EndInit();
            ((ISupportInitialize)nudSoLuongFrom).EndInit();
            ResumeLayout(false);
        }
        internal Panel panel1;

        internal NumericUpDown nudSoLuongTo;

        internal NumericUpDown nudSoLuongFrom;

        internal TextBox txtTenHanhDong;

        internal Label label3;

        internal Label label4;

        internal Label FCB5EAA1;

        internal Label B3050320;

        internal Button btnCancel;

        internal Button btnSave;

        internal Panel pnlHeader;

        internal Button button1;

        internal PictureBox pictureBox1;

        internal ToolTip toolTip_0;

        internal NumericUpDown E52056A5;

        internal NumericUpDown A4902299;

        internal Label label9;

        internal Label E69D4209;

        internal Label label6;
        #endregion

        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_mode;
        private AntdUI.Button btn_global;
        private AntdUI.Button btn_setting;
        internal Label C92DE629;
        private TextBox txtComments;
    }
}