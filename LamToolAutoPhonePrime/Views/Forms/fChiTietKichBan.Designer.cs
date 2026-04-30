namespace LamToolAutoPhonePrime.Views.Forms
{
    partial class fChiTietKichBan
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle8 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle7 = new DataGridViewCellStyle();
            windowBar = new AntdUI.PageHeader();
            btn_mode = new AntdUI.Button();
            btn_global = new AntdUI.Button();
            btn_setting = new AntdUI.Button();
            panel4 = new AntdUI.Panel();
            button9 = new AntdUI.Button();
            button5 = new AntdUI.Button();
            button4 = new Button();
            button3 = new Button();
            textBox1 = new TextBox();
            label1 = new Label();
            input6 = new AntdUI.Input();
            button16 = new AntdUI.Button();
            panel2 = new AntdUI.Panel();
            dataGridView1 = new LamToolAutoPhonePrime.Utils.DoubleBufferedDataGridView();
            dataGridViewTextBoxColumn1 = new DataGridViewTextBoxColumn();
            label3 = new Label();
            input2 = new AntdUI.Input();
            button2 = new AntdUI.Button();
            windowBar.SuspendLayout();
            panel4.SuspendLayout();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();

            // windowBar
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
            windowBar.Padding = new Padding(20, 5, 20, 0);
            windowBar.ShowIcon = true;
            windowBar.Size = new Size(958, 35);
            windowBar.SubFont = new Font("Microsoft Sans Serif", LamToolAutoPhonePrime.Utils.Design.FontScale.Caption, FontStyle.Regular, GraphicsUnit.Point, 0);
            windowBar.SubGap = 1;
            windowBar.SubText = "";
            windowBar.TabIndex = 11;
            windowBar.Text = "Chi tiết kịch bản";
            windowBar.UseSystemStyleColor = true;
            windowBar.UseTextBold = false;

            btn_mode.Dock = DockStyle.Right;
            btn_mode.Ghost = true;
            btn_mode.Icon = Properties.Resources.icons8_circle_16_Green;
            btn_mode.IconSvg = "";
            btn_mode.Location = new Point(858, 5);
            btn_mode.Name = "btn_mode";
            btn_mode.Radius = 0;
            btn_mode.Size = new Size(26, 30);
            btn_mode.TabIndex = 6;
            btn_mode.ToggleIconSvg = "MoonOutlined";
            btn_mode.WaveSize = 0;

            btn_global.Dock = DockStyle.Right;
            btn_global.Ghost = true;
            btn_global.Icon = Properties.Resources.icons8_circle_16_Yellow;
            btn_global.IconSvg = "";
            btn_global.Location = new Point(884, 5);
            btn_global.Name = "btn_global";
            btn_global.Radius = 0;
            btn_global.Size = new Size(24, 30);
            btn_global.TabIndex = 7;
            btn_global.WaveSize = 0;
            btn_global.Click += btn_global_Click;

            btn_setting.Dock = DockStyle.Right;
            btn_setting.Ghost = true;
            btn_setting.Icon = Properties.Resources.icons8_circle_16_Red;
            btn_setting.IconSvg = "";
            btn_setting.Location = new Point(908, 5);
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(30, 30);
            btn_setting.TabIndex = 8;
            btn_setting.WaveSize = 0;
            btn_setting.Click += btn_setting_Click;

            // panel4 (header: script name + buttons)
            panel4.Back = Color.White;
            panel4.BackColor = Color.Transparent;
            panel4.Controls.Add(button9);
            panel4.Controls.Add(button5);
            panel4.Controls.Add(button4);
            panel4.Controls.Add(button3);
            panel4.Controls.Add(textBox1);
            panel4.Controls.Add(label1);
            panel4.Controls.Add(input6);
            panel4.Controls.Add(button16);
            panel4.Dock = DockStyle.Top;
            panel4.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            panel4.Location = new Point(0, 35);
            panel4.Name = "panel4";
            panel4.padding = new Padding(20);
            panel4.Radius = 16;
            panel4.Size = new Size(958, 142);
            panel4.TabIndex = 12;
            panel4.Text = "panel4";

            button9.Anchor = AnchorStyles.Left;
            button9.DefaultBack = Color.DodgerBlue;
            button9.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            button9.ForeColor = Color.White;
            button9.IconHoverSvg = "";
            button9.IconRatio = 0.9F;
            button9.IconSvg = "RedoOutlined";
            button9.Location = new Point(638, 72);
            button9.Name = "button9";
            button9.Radius = 10;
            button9.Shape = AntdUI.TShape.Round;
            button9.Size = new Size(116, 40);
            button9.TabIndex = 12;
            button9.Text = "Reload";
            button9.Type = AntdUI.TTypeMini.Primary;
            button9.Click += button9_Click;

            button5.Anchor = AnchorStyles.Right;
            button5.DefaultBack = Color.DodgerBlue;
            button5.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            button5.ForeColor = Color.White;
            button5.IconHoverSvg = "";
            button5.IconRatio = 0.9F;
            button5.IconSvg = "PlusOutlined";
            button5.Location = new Point(760, 72);
            button5.Name = "button5";
            button5.Radius = 10;
            button5.Shape = AntdUI.TShape.Round;
            button5.Size = new Size(159, 40);
            button5.TabIndex = 11;
            button5.Text = "Thêm hành động";
            button5.Type = AntdUI.TTypeMini.Info;
            button5.Click += button5_Click;

            button4.Anchor = AnchorStyles.Right;
            button4.AutoSize = true;
            button4.BackColor = Color.White;
            button4.Cursor = Cursors.Hand;
            button4.FlatAppearance.BorderSize = 0;
            button4.FlatStyle = FlatStyle.Flat;
            button4.Image = Properties.Resources.icons8_remove_16;
            button4.Location = new Point(397, 34);
            button4.Name = "button4";
            button4.Size = new Size(22, 23);
            button4.TabIndex = 10;
            button4.UseVisualStyleBackColor = false;
            button4.Click += button4_Click;

            button3.Anchor = AnchorStyles.Right;
            button3.AutoSize = true;
            button3.BackColor = Color.White;
            button3.Cursor = Cursors.Hand;
            button3.FlatAppearance.BorderSize = 0;
            button3.FlatStyle = FlatStyle.Flat;
            button3.Image = Properties.Resources.icons8_rename_16;
            button3.Location = new Point(370, 33);
            button3.Name = "button3";
            button3.Size = new Size(22, 23);
            button3.TabIndex = 9;
            button3.UseVisualStyleBackColor = false;
            button3.Click += button3_Click;

            textBox1.Anchor = AnchorStyles.Left;
            textBox1.Location = new Point(128, 33);
            textBox1.Name = "textBox1";
            textBox1.ReadOnly = true;
            textBox1.Size = new Size(238, 23);
            textBox1.TabIndex = 8;

            label1.Anchor = AnchorStyles.Left;
            label1.AutoSize = true;
            label1.Location = new Point(34, 36);
            label1.Name = "label1";
            label1.Size = new Size(85, 17);
            label1.TabIndex = 7;
            label1.Text = "Tên kịch bản:";

            input6.AllowClear = true;
            input6.Anchor = AnchorStyles.Right;
            input6.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            input6.Location = new Point(2674, 200);
            input6.Name = "input6";
            input6.PlaceholderText = "Tìm kiếm...";
            input6.PrefixSvg = "SearchOutlined";
            input6.Size = new Size(241, 40);
            input6.TabIndex = 2;

            button16.Anchor = AnchorStyles.Right;
            button16.DefaultBack = Color.DodgerBlue;
            button16.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            button16.ForeColor = Color.White;
            button16.IconHoverSvg = "";
            button16.IconRatio = 0.9F;
            button16.IconSvg = "PlusOutlined";
            button16.Location = new Point(2921, 200);
            button16.Name = "button16";
            button16.Radius = 10;
            button16.Shape = AntdUI.TShape.Round;
            button16.Size = new Size(146, 40);
            button16.TabIndex = 6;
            button16.Text = "Thêm tài khoản";
            button16.Type = AntdUI.TTypeMini.Info;

            // panel2 (action list)
            panel2.Back = Color.White;
            panel2.BackColor = Color.Transparent;
            panel2.Controls.Add(dataGridView1);
            panel2.Controls.Add(label3);
            panel2.Controls.Add(input2);
            panel2.Controls.Add(button2);
            panel2.Dock = DockStyle.Fill;
            panel2.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            panel2.Location = new Point(0, 177);
            panel2.Name = "panel2";
            panel2.padding = new Padding(20, 0, 20, 20);
            panel2.Padding = new Padding(25, 5, 25, 25);
            panel2.Radius = 16;
            panel2.Size = new Size(958, 376);
            panel2.TabIndex = 14;
            panel2.Text = "panel2";

            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridViewCellStyle5.BackColor = Color.Transparent;
            dataGridView1.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle5;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.ColumnHeader;
            dataGridView1.BackgroundColor = Color.White;
            dataGridView1.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            dataGridView1.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle6.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = Color.White;
            dataGridViewCellStyle6.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle6.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle6.SelectionBackColor = Color.Teal;
            dataGridViewCellStyle6.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle6.WrapMode = DataGridViewTriState.True;
            dataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle6;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { dataGridViewTextBoxColumn1 });
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.EditMode = DataGridViewEditMode.EditOnEnter;
            dataGridView1.GridColor = SystemColors.AppWorkspace;
            dataGridView1.Location = new Point(25, 31);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RightToLeft = RightToLeft.No;
            dataGridViewCellStyle8.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle8.BackColor = Color.White;
            dataGridViewCellStyle8.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle8.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle8.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle8.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle8.WrapMode = DataGridViewTriState.True;
            dataGridView1.RowHeadersDefaultCellStyle = dataGridViewCellStyle8;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.Size = new Size(908, 320);
            dataGridView1.TabIndex = 13;

            dataGridViewCellStyle7.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            dataGridViewTextBoxColumn1.DefaultCellStyle = dataGridViewCellStyle7;
            dataGridViewTextBoxColumn1.HeaderText = "#";
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 41;

            label3.Dock = DockStyle.Top;
            label3.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(25, 5);
            label3.Name = "label3";
            label3.Size = new Size(908, 26);
            label3.TabIndex = 6;
            label3.Text = "Danh sách hành động của kịch bản (0)";

            input2.AllowClear = true;
            input2.Anchor = AnchorStyles.Right;
            input2.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            input2.Location = new Point(3607, 372);
            input2.Name = "input2";
            input2.PlaceholderText = "Tìm kiếm...";
            input2.PrefixSvg = "SearchOutlined";
            input2.Size = new Size(241, 40);
            input2.TabIndex = 2;

            button2.Anchor = AnchorStyles.Right;
            button2.DefaultBack = Color.DodgerBlue;
            button2.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            button2.ForeColor = Color.White;
            button2.IconHoverSvg = "";
            button2.IconRatio = 0.9F;
            button2.IconSvg = "PlusOutlined";
            button2.Location = new Point(3854, 372);
            button2.Name = "button2";
            button2.Radius = 10;
            button2.Shape = AntdUI.TShape.Round;
            button2.Size = new Size(146, 40);
            button2.TabIndex = 6;
            button2.Text = "Thêm tài khoản";
            button2.Type = AntdUI.TTypeMini.Info;

            // fChiTietKichBan
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(236, 240, 241);
            ClientSize = new Size(958, 553);
            Controls.Add(panel2);
            Controls.Add(panel4);
            Controls.Add(windowBar);
            Name = "fChiTietKichBan";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "fChiTietKichBan";
            windowBar.ResumeLayout(false);
            panel4.ResumeLayout(false);
            panel4.PerformLayout();
            panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_mode;
        private AntdUI.Button btn_global;
        private AntdUI.Button btn_setting;
        private AntdUI.Panel panel4;
        private AntdUI.Input input6;
        private AntdUI.Button button16;
        private AntdUI.Panel panel2;
        private AntdUI.Input input2;
        private AntdUI.Button button2;
        private Label label3;
        public LamToolAutoPhonePrime.Utils.DoubleBufferedDataGridView dataGridView1;
        private TextBox textBox1;
        private Label label1;
        private Button button3;
        private Button button4;
        private AntdUI.Button button5;
        private AntdUI.Button button9;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
    }
}
