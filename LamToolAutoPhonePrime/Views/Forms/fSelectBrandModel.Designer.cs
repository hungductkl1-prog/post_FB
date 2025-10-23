namespace LamToolAutoPhonePrime.Views.Forms
{
    partial class fSelectBrandModel
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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            windowBar = new AntdUI.PageHeader();
            btn_mode = new AntdUI.Button();
            btn_global = new AntdUI.Dropdown();
            btn_setting = new AntdUI.Button();
            panel1 = new AntdUI.Panel();
            button1 = new AntdUI.Button();
            button9 = new AntdUI.Button();
            panel2 = new AntdUI.Panel();
            checkBox1 = new CheckBox();
            dgvDevices = new DataGridView();
            colCheckbox_Device = new DataGridViewCheckBoxColumn();
            tIndex = new DataGridViewTextBoxColumn();
            colStatus = new DataGridViewTextBoxColumn();
            label1 = new Label();
            button3 = new AntdUI.Button();
            windowBar.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDevices).BeginInit();
            SuspendLayout();
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
            windowBar.Size = new Size(398, 36);
            windowBar.SubText = "";
            windowBar.TabIndex = 9;
            windowBar.Text = "Chọn danh sách brand device";
            windowBar.UseSystemStyleColor = true;
            windowBar.UseTextBold = false;
            // 
            // btn_mode
            // 
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Ghost = true;
            btn_mode.Icon = Properties.Resources.icons8_circle_16_Green;
            btn_mode.IconSvg = "";
            btn_mode.Location = new Point(318, 0);
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
            btn_global.Location = new Point(344, 0);
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
            btn_setting.Location = new Point(368, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(30, 36);
            btn_setting.TabIndex = 9;
            btn_setting.WaveSize = 0;
            btn_setting.Click += btn_setting_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(button1);
            panel1.Controls.Add(button9);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 540);
            panel1.Name = "panel1";
            panel1.padding = new Padding(12);
            panel1.Radius = 12;
            panel1.Size = new Size(398, 89);
            panel1.TabIndex = 10;
            panel1.Text = "panel1";
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom;
            button1.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button1.IconRatio = 1F;
            button1.IconSvg = "CloseOutlined";
            button1.IconToggleAnimation = 400;
            button1.Location = new Point(212, 23);
            button1.Name = "button1";
            button1.Shape = AntdUI.TShape.Round;
            button1.Size = new Size(139, 42);
            button1.TabIndex = 16;
            button1.Text = "Đóng";
            button1.Type = AntdUI.TTypeMini.Error;
            button1.Click += button1_Click;
            // 
            // button9
            // 
            button9.Anchor = AnchorStyles.Bottom;
            button9.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button9.IconRatio = 1.1F;
            button9.IconSvg = "SaveOutlined";
            button9.IconToggleAnimation = 400;
            button9.Location = new Point(63, 23);
            button9.Name = "button9";
            button9.Shape = AntdUI.TShape.Round;
            button9.Size = new Size(131, 42);
            button9.TabIndex = 15;
            button9.Text = "Lưu";
            button9.Type = AntdUI.TTypeMini.Success;
            button9.Click += button9_Click;
            // 
            // panel2
            // 
            panel2.Controls.Add(checkBox1);
            panel2.Controls.Add(dgvDevices);
            panel2.Controls.Add(label1);
            panel2.Controls.Add(button3);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(0, 36);
            panel2.Name = "panel2";
            panel2.padding = new Padding(12);
            panel2.Padding = new Padding(20);
            panel2.Radius = 12;
            panel2.Size = new Size(398, 504);
            panel2.TabIndex = 11;
            panel2.Text = "panel2";
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(40, 54);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(15, 14);
            checkBox1.TabIndex = 17;
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // dgvDevices
            // 
            dgvDevices.AllowUserToAddRows = false;
            dgvDevices.AllowUserToDeleteRows = false;
            dgvDevices.AllowUserToResizeRows = false;
            dgvDevices.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dgvDevices.BackgroundColor = SystemColors.ButtonHighlight;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Control;
            dataGridViewCellStyle1.Font = new Font("Tahoma", 9.75F);
            dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = Color.Teal;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dgvDevices.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvDevices.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDevices.Columns.AddRange(new DataGridViewColumn[] { colCheckbox_Device, tIndex, colStatus });
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = SystemColors.Window;
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = Color.FromArgb(48, 48, 48);
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            dgvDevices.DefaultCellStyle = dataGridViewCellStyle2;
            dgvDevices.Dock = DockStyle.Fill;
            dgvDevices.EditMode = DataGridViewEditMode.EditOnEnter;
            dgvDevices.Location = new Point(20, 48);
            dgvDevices.Name = "dgvDevices";
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = SystemColors.Control;
            dataGridViewCellStyle3.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle3.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.True;
            dgvDevices.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dgvDevices.RowHeadersVisible = false;
            dgvDevices.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDevices.Size = new Size(358, 436);
            dgvDevices.TabIndex = 17;
            // 
            // colCheckbox_Device
            // 
            colCheckbox_Device.FillWeight = 50F;
            colCheckbox_Device.HeaderText = "";
            colCheckbox_Device.MinimumWidth = 50;
            colCheckbox_Device.Name = "colCheckbox_Device";
            colCheckbox_Device.Width = 50;
            // 
            // tIndex
            // 
            tIndex.HeaderText = "#";
            tIndex.Name = "tIndex";
            tIndex.ReadOnly = true;
            tIndex.Width = 41;
            // 
            // colStatus
            // 
            colStatus.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colStatus.HeaderText = "Brand";
            colStatus.Name = "colStatus";
            colStatus.ReadOnly = true;
            // 
            // label1
            // 
            label1.BackColor = Color.White;
            label1.Dock = DockStyle.Top;
            label1.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.Blue;
            label1.Location = new Point(20, 20);
            label1.Name = "label1";
            label1.Size = new Size(358, 28);
            label1.TabIndex = 16;
            label1.Text = "Đã chọn 38/38";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // button3
            // 
            button3.Anchor = AnchorStyles.Bottom;
            button3.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button3.IconRatio = 1.1F;
            button3.IconSvg = "SaveOutlined";
            button3.IconToggleAnimation = 400;
            button3.Location = new Point(268, 507);
            button3.Name = "button3";
            button3.Shape = AntdUI.TShape.Round;
            button3.Size = new Size(131, 42);
            button3.TabIndex = 15;
            button3.Text = "Lưu";
            button3.Type = AntdUI.TTypeMini.Success;
            // 
            // fSelectBrandModel
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            BackColor = Color.FromArgb(236, 240, 241);
            ClientSize = new Size(398, 629);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Controls.Add(windowBar);
            Name = "fSelectBrandModel";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "fFolder";
            windowBar.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDevices).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_mode;
        private AntdUI.Dropdown btn_global;
        private AntdUI.Button btn_setting;
        private AntdUI.Panel panel1;
        private AntdUI.Button button9;
        private AntdUI.Button button1;
        private AntdUI.Panel panel2;
        private AntdUI.Button button3;
        private Label label1;
        private CheckBox checkBox1;
        public DataGridView dgvDevices;
        private DataGridViewCheckBoxColumn colCheckbox_Device;
        private DataGridViewTextBoxColumn tIndex;
        private DataGridViewTextBoxColumn colStatus;
    }
}