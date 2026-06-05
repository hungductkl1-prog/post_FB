using System.ComponentModel;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    partial class fImportProxy
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
            A516E182 = new PictureBox();
            btnMinimize = new Button();
            btnCancel = new Button();
            btnAdd = new Button();
            label8 = new Label();
            lblStatus = new Label();
            E38C2636 = new Label();
            FE1FAE23 = new NumericUpDown();
            ckbKhongNhapTaiKhoanDaCo = new CheckBox();
            F5A329B5 = new Label();
            E82D5414 = new RadioButton();
            E8BE35A6 = new RadioButton();
            label3 = new Label();
            cbbTypeProxy = new ComboBox();
            F32C6BA9 = new Label();
            txtLines = new TextBox();
            windowBar = new AntdUI.PageHeader();
            btn_mode = new AntdUI.Button();
            btn_global = new AntdUI.Dropdown();
            btn_setting = new AntdUI.Button();
            pnlHeader.SuspendLayout();
            ((ISupportInitialize)A516E182).BeginInit();
            ((ISupportInitialize)FE1FAE23).BeginInit();
            windowBar.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            pnlHeader.BackColor = Color.White;
            pnlHeader.Controls.Add(A516E182);
            pnlHeader.Controls.Add(btnMinimize);
            pnlHeader.Location = new Point(0, 5);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(477, 32);
            pnlHeader.TabIndex = 9;
            // 
            // A516E182
            // 
            A516E182.Location = new Point(4, 1);
            A516E182.Name = "A516E182";
            A516E182.Size = new Size(34, 27);
            A516E182.SizeMode = PictureBoxSizeMode.Zoom;
            A516E182.TabIndex = 77;
            A516E182.TabStop = false;
            // 
            // btnMinimize
            // 
            btnMinimize.Cursor = Cursors.Hand;
            btnMinimize.Dock = DockStyle.Right;
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.FlatStyle = FlatStyle.Flat;
            btnMinimize.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnMinimize.ForeColor = Color.White;
            btnMinimize.Location = new Point(445, 0);
            btnMinimize.Name = "btnMinimize";
            btnMinimize.Size = new Size(32, 32);
            btnMinimize.TabIndex = 9;
            btnMinimize.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnMinimize.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.Maroon;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Tahoma", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(244, 386);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(92, 29);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Đóng";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnAdd
            // 
            btnAdd.BackColor = Color.FromArgb(53, 120, 229);
            btnAdd.Cursor = Cursors.Hand;
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.FlatStyle = FlatStyle.Flat;
            btnAdd.Font = new Font("Tahoma", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnAdd.ForeColor = Color.White;
            btnAdd.Location = new Point(135, 386);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(92, 29);
            btnAdd.TabIndex = 3;
            btnAdd.Text = "Xác nhận";
            btnAdd.UseVisualStyleBackColor = false;
            btnAdd.Click += btnAdd_Click;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label8.Location = new Point(331, 252);
            label8.Name = "label8";
            label8.Size = new Size(115, 16);
            label8.TabIndex = 5;
            label8.Text = "(Mỗi proxy 1 dòng)";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblStatus.Location = new Point(30, 47);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(127, 16);
            lblStatus.TabIndex = 6;
            lblStatus.Text = "Danh sách Proxy (0):";
            // 
            // E38C2636
            // 
            E38C2636.AutoSize = true;
            E38C2636.Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            E38C2636.Location = new Point(31, 298);
            E38C2636.Name = "E38C2636";
            E38C2636.Size = new Size(119, 16);
            E38C2636.TabIndex = 119;
            E38C2636.Text = "Số tài khoản/proxy:";
            // 
            // FE1FAE23
            // 
            FE1FAE23.Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FE1FAE23.Location = new Point(164, 296);
            FE1FAE23.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            FE1FAE23.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            FE1FAE23.Name = "FE1FAE23";
            FE1FAE23.Size = new Size(69, 23);
            FE1FAE23.TabIndex = 120;
            FE1FAE23.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // ckbKhongNhapTaiKhoanDaCo
            // 
            ckbKhongNhapTaiKhoanDaCo.AutoSize = true;
            ckbKhongNhapTaiKhoanDaCo.Cursor = Cursors.Hand;
            ckbKhongNhapTaiKhoanDaCo.Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ckbKhongNhapTaiKhoanDaCo.Location = new Point(33, 349);
            ckbKhongNhapTaiKhoanDaCo.Name = "ckbKhongNhapTaiKhoanDaCo";
            ckbKhongNhapTaiKhoanDaCo.Size = new Size(283, 20);
            ckbKhongNhapTaiKhoanDaCo.TabIndex = 121;
            ckbKhongNhapTaiKhoanDaCo.Text = "Không nhập vào những tài khoản đã có Proxy";
            ckbKhongNhapTaiKhoanDaCo.UseVisualStyleBackColor = true;
            // 
            // F5A329B5
            // 
            F5A329B5.AutoSize = true;
            F5A329B5.Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            F5A329B5.Location = new Point(30, 325);
            F5A329B5.Name = "F5A329B5";
            F5A329B5.Size = new Size(131, 16);
            F5A329B5.TabIndex = 119;
            F5A329B5.Text = "Tùy chọn nhập Proxy:";
            // 
            // E82D5414
            // 
            E82D5414.AutoSize = true;
            E82D5414.Cursor = Cursors.Hand;
            E82D5414.Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            E82D5414.Location = new Point(269, 323);
            E82D5414.Name = "E82D5414";
            E82D5414.Size = new Size(89, 20);
            E82D5414.TabIndex = 122;
            E82D5414.Text = "Ngẫu nhiên";
            E82D5414.UseVisualStyleBackColor = true;
            // 
            // E8BE35A6
            // 
            E8BE35A6.AutoSize = true;
            E8BE35A6.Checked = true;
            E8BE35A6.Cursor = Cursors.Hand;
            E8BE35A6.Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            E8BE35A6.Location = new Point(180, 323);
            E8BE35A6.Name = "E8BE35A6";
            E8BE35A6.Size = new Size(71, 20);
            E8BE35A6.TabIndex = 122;
            E8BE35A6.TabStop = true;
            E8BE35A6.Text = "Lần lượt";
            E8BE35A6.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.Location = new Point(31, 267);
            label3.Name = "label3";
            label3.Size = new Size(70, 16);
            label3.TabIndex = 119;
            label3.Text = "Loại Proxy:";
            // 
            // cbbTypeProxy
            // 
            cbbTypeProxy.Cursor = Cursors.Hand;
            cbbTypeProxy.DropDownStyle = ComboBoxStyle.DropDownList;
            cbbTypeProxy.Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            cbbTypeProxy.FormattingEnabled = true;
            cbbTypeProxy.Items.AddRange(new object[] { "HTTP" });
            cbbTypeProxy.Location = new Point(164, 264);
            cbbTypeProxy.Name = "cbbTypeProxy";
            cbbTypeProxy.Size = new Size(140, 24);
            cbbTypeProxy.TabIndex = 123;
            // 
            // F32C6BA9
            // 
            F32C6BA9.AutoSize = true;
            F32C6BA9.Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            F32C6BA9.ForeColor = Color.Red;
            F32C6BA9.Location = new Point(242, 47);
            F32C6BA9.Name = "F32C6BA9";
            F32C6BA9.Size = new Size(204, 16);
            F32C6BA9.TabIndex = 119;
            F32C6BA9.Text = "(Định dạng: IP:PORT:USER:PASS)";
            // 
            // txtLines
            // 
            txtLines.BorderStyle = BorderStyle.FixedSingle;
            txtLines.Location = new Point(30, 77);
            txtLines.MaxLength = int.MaxValue;
            txtLines.Multiline = true;
            txtLines.Name = "txtLines";
            txtLines.ScrollBars = ScrollBars.Both;
            txtLines.Size = new Size(426, 160);
            txtLines.TabIndex = 124;
            txtLines.WordWrap = false;
            txtLines.TextChanged += txtLines_TextChanged;
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
            windowBar.DividerShow = true;
            windowBar.Dock = DockStyle.Top;
            windowBar.Font = new Font("Microsoft YaHei UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            windowBar.ForeColor = Color.Black;
            windowBar.Location = new Point(0, 0);
            windowBar.MDI = true;
            windowBar.Name = "windowBar";
            windowBar.Size = new Size(477, 36);
            windowBar.SubText = "";
            windowBar.TabIndex = 125;
            windowBar.Text = "Cập nhật proxy vào tài khoản";
            windowBar.UseSystemStyleColor = true;
            windowBar.UseTextBold = false;
            // 
            // btn_mode
            // 
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Ghost = true;
            btn_mode.Icon = Properties.Resources.icons8_circle_16_Green;
            btn_mode.IconSvg = "";
            btn_mode.Location = new Point(397, 0);
            btn_mode.Name = "btn_mode";
            btn_mode.Radius = 0;
            btn_mode.Size = new Size(26, 36);
            btn_mode.TabIndex = 11;
            btn_mode.ToggleIconSvg = "MoonOutlined";
            btn_mode.WaveSize = 0;
            // 
            // btn_global
            // 
            btn_global.Dock = DockStyle.Right;
            btn_global.Ghost = true;
            btn_global.Icon = Properties.Resources.icons8_circle_16_Yellow;
            btn_global.IconSvg = "";
            btn_global.Location = new Point(423, 0);
            btn_global.Name = "btn_global";
            btn_global.Radius = 0;
            btn_global.Size = new Size(24, 36);
            btn_global.TabIndex = 10;
            btn_global.WaveSize = 0;
            // 
            // btn_setting
            // 
            btn_setting.Dock = DockStyle.Right;
            btn_setting.Ghost = true;
            btn_setting.Icon = Properties.Resources.icons8_circle_16_Red;
            btn_setting.IconSvg = "";
            btn_setting.Location = new Point(447, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(30, 36);
            btn_setting.TabIndex = 9;
            btn_setting.WaveSize = 0;
            btn_setting.Click += btn_setting_Click;
            // 
            // fImportProxy
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(477, 432);
            Controls.Add(windowBar);
            Controls.Add(txtLines);
            Controls.Add(lblStatus);
            Controls.Add(cbbTypeProxy);
            Controls.Add(E8BE35A6);
            Controls.Add(E82D5414);
            Controls.Add(ckbKhongNhapTaiKhoanDaCo);
            Controls.Add(FE1FAE23);
            Controls.Add(F5A329B5);
            Controls.Add(F32C6BA9);
            Controls.Add(label3);
            Controls.Add(E38C2636);
            Controls.Add(label8);
            Controls.Add(btnCancel);
            Controls.Add(btnAdd);
            Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            Name = "fImportProxy";
            StartPosition = FormStartPosition.CenterParent;
            Text = "fAddFile";
            pnlHeader.ResumeLayout(false);
            ((ISupportInitialize)A516E182).EndInit();
            ((ISupportInitialize)FE1FAE23).EndInit();
            windowBar.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        internal Panel pnlHeader;

        internal Button btnMinimize;

        internal Button btnCancel;

        internal Button btnAdd;

        internal PictureBox A516E182;

        internal Label label8;

        internal Label lblStatus;

        internal Label E38C2636;

        internal NumericUpDown FE1FAE23;

        internal CheckBox ckbKhongNhapTaiKhoanDaCo;

        internal Label F5A329B5;

        internal RadioButton E82D5414;

        internal RadioButton E8BE35A6;

        internal Label label3;

        internal ComboBox cbbTypeProxy;
        #endregion

        internal Label F32C6BA9;
        private TextBox txtLines;
        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_mode;
        private AntdUI.Dropdown btn_global;
        private AntdUI.Button btn_setting;
    }
}