using System.ComponentModel;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions
{
    partial class fHDMoiBanBeLikePage
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
            C0A443BE = new Panel();
            E3BF1729 = new Button();
            pictureBox1 = new PictureBox();
            E18B7F29 = new Panel();
            txtComments = new TextBox();
            windowBar = new Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel();
            btn_mode = new Button();
            btn_global = new Button();
            btn_setting = new Button();
            label8 = new Label();
            C92DE629 = new Label();
            nudSoLuongTo = new NumericUpDown();
            nudSoLuongFrom = new NumericUpDown();
            txtTenHanhDong = new TextBox();
            D3175802 = new Label();
            label4 = new Label();
            AA95AF20 = new Label();
            label1 = new Label();
            btnCancel = new Button();
            btnSave = new Button();
            C0A443BE.SuspendLayout();
            ((ISupportInitialize)pictureBox1).BeginInit();
            E18B7F29.SuspendLayout();
            windowBar.SuspendLayout();
            ((ISupportInitialize)nudSoLuongTo).BeginInit();
            ((ISupportInitialize)nudSoLuongFrom).BeginInit();
            SuspendLayout();
            // 
            // C0A443BE
            // 
            C0A443BE.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            C0A443BE.BackColor = Color.White;
            C0A443BE.Controls.Add(E3BF1729);
            C0A443BE.Controls.Add(pictureBox1);
            C0A443BE.Cursor = Cursors.SizeAll;
            C0A443BE.Location = new Point(0, 3);
            C0A443BE.Name = "C0A443BE";
            C0A443BE.Size = new Size(359, 31);
            C0A443BE.TabIndex = 9;
            // 
            // E3BF1729
            // 
            E3BF1729.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            E3BF1729.Cursor = Cursors.Hand;
            E3BF1729.FlatAppearance.BorderSize = 0;
            E3BF1729.FlatStyle = FlatStyle.Flat;
            E3BF1729.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            E3BF1729.ForeColor = Color.White;
            E3BF1729.Location = new Point(328, 1);
            E3BF1729.Name = "E3BF1729";
            E3BF1729.Size = new Size(30, 30);
            E3BF1729.TabIndex = 77;
            E3BF1729.TextImageRelation = TextImageRelation.ImageBeforeText;
            E3BF1729.UseVisualStyleBackColor = true;
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
            // E18B7F29
            // 
            E18B7F29.BackColor = Color.White;
            E18B7F29.BorderStyle = BorderStyle.FixedSingle;
            E18B7F29.Controls.Add(txtComments);
            E18B7F29.Controls.Add(windowBar);
            E18B7F29.Controls.Add(label8);
            E18B7F29.Controls.Add(C92DE629);
            E18B7F29.Controls.Add(nudSoLuongTo);
            E18B7F29.Controls.Add(nudSoLuongFrom);
            E18B7F29.Controls.Add(txtTenHanhDong);
            E18B7F29.Controls.Add(D3175802);
            E18B7F29.Controls.Add(label4);
            E18B7F29.Controls.Add(AA95AF20);
            E18B7F29.Controls.Add(label1);
            E18B7F29.Controls.Add(btnCancel);
            E18B7F29.Controls.Add(btnSave);
            E18B7F29.Dock = DockStyle.Fill;
            E18B7F29.Location = new Point(0, 0);
            E18B7F29.Name = "E18B7F29";
            E18B7F29.Size = new Size(362, 371);
            E18B7F29.TabIndex = 0;
            // 
            // txtComments
            // 
            txtComments.BorderStyle = BorderStyle.FixedSingle;
            txtComments.Location = new Point(28, 125);
            txtComments.MaxLength = int.MaxValue;
            txtComments.Multiline = true;
            txtComments.Name = "txtComments";
            txtComments.ScrollBars = ScrollBars.Both;
            txtComments.Size = new Size(321, 168);
            txtComments.TabIndex = 198;
            txtComments.WordWrap = false;
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
            windowBar.Icon = Properties.Resources.logo_lamtool_v3_dark_16;
            windowBar.Location = new Point(0, 0);
            windowBar.Name = "windowBar";
            windowBar.Size = new Size(360, 35);
            windowBar.TabIndex = 119;
            windowBar.Text = "Cấu hình tương tác";
            // 
            // btn_mode
            // 
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Image = Properties.Resources.icons8_circle_16_Green;
            btn_mode.Location = new Point(280, 0);
            btn_mode.Name = "btn_mode";
            btn_mode.Size = new Size(26, 35);
            btn_mode.TabIndex = 6;
            // 
            // btn_global
            // 
            btn_global.Dock = DockStyle.Right;
            btn_global.Image = Properties.Resources.icons8_circle_16_Yellow;
            btn_global.Location = new Point(306, 0);
            btn_global.Name = "btn_global";
            btn_global.Size = new Size(24, 35);
            btn_global.TabIndex = 7;
            // 
            // btn_setting
            // 
            btn_setting.Dock = DockStyle.Right;
            btn_setting.Image = Properties.Resources.icons8_circle_16_Red;
            btn_setting.Location = new Point(330, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Size = new Size(30, 35);
            btn_setting.TabIndex = 8;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(28, 296);
            label8.Name = "label8";
            label8.Size = new Size(128, 16);
            label8.TabIndex = 0;
            label8.Text = "(Mỗi ID Page 1 dòng)";
            // 
            // C92DE629
            // 
            C92DE629.AutoSize = true;
            C92DE629.Location = new Point(27, 106);
            C92DE629.Name = "C92DE629";
            C92DE629.Size = new Size(195, 16);
            C92DE629.TabIndex = 0;
            C92DE629.Text = "Danh sách Uid page cần mời (0):";
            // 
            // nudSoLuongTo
            // 
            nudSoLuongTo.Location = new Point(240, 78);
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
            // D3175802
            // 
            D3175802.Location = new Point(194, 80);
            D3175802.Name = "D3175802";
            D3175802.Size = new Size(40, 16);
            D3175802.TabIndex = 37;
            D3175802.Text = "đến";
            D3175802.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(298, 80);
            label4.Name = "label4";
            label4.Size = new Size(28, 16);
            label4.TabIndex = 35;
            label4.Text = "bạn";
            // 
            // AA95AF20
            // 
            AA95AF20.AutoSize = true;
            AA95AF20.Location = new Point(27, 80);
            AA95AF20.Name = "AA95AF20";
            AA95AF20.Size = new Size(88, 16);
            AA95AF20.TabIndex = 32;
            AA95AF20.Text = "Số lượng mời:";
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
            btnCancel.Location = new Point(193, 329);
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
            btnSave.Location = new Point(82, 329);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(92, 29);
            btnSave.TabIndex = 6;
            btnSave.Text = "Thêm";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // fHDMoiBanBeLikePage
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(362, 371);
            Controls.Add(E18B7F29);
            Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "fHDMoiBanBeLikePage";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Cấu hình tương tác";
            C0A443BE.ResumeLayout(false);
            ((ISupportInitialize)pictureBox1).EndInit();
            E18B7F29.ResumeLayout(false);
            E18B7F29.PerformLayout();
            windowBar.ResumeLayout(false);
            ((ISupportInitialize)nudSoLuongTo).EndInit();
            ((ISupportInitialize)nudSoLuongFrom).EndInit();
            ResumeLayout(false);
        }
        internal Panel E18B7F29;

        internal NumericUpDown nudSoLuongTo;

        internal NumericUpDown nudSoLuongFrom;

        internal TextBox txtTenHanhDong;

        internal Label D3175802;

        internal Label label4;

        internal Label AA95AF20;

        internal Label label1;

        internal Button btnCancel;

        internal Button btnSave;

        internal Panel C0A443BE;

        internal Button E3BF1729;

        internal PictureBox pictureBox1;

        internal Label label8;

        internal Label C92DE629;

        internal ToolTip D88BC61D;
        #endregion

        private Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel windowBar;
        private Button btn_mode;
        private Button btn_global;
        private Button btn_setting;
        private TextBox txtComments;
    }
}