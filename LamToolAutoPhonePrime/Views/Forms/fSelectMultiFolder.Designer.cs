namespace LamToolAutoPhonePrime.Views.Forms
{
    partial class fSelectMultiFolder
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            windowBar = new AntdUI.PageHeader();
            btn_setting = new AntdUI.Button();
            btn_global = new AntdUI.Dropdown();
            btn_mode = new AntdUI.Button();
            panelBottom = new AntdUI.Panel();
            btnSave = new AntdUI.Button();
            btnClose = new AntdUI.Button();
            panelContent = new AntdUI.Panel();
            lblCount = new Label();
            chkAll = new CheckBox();
            dataGridView1 = new DataGridView();
            colCheck = new DataGridViewCheckBoxColumn();
            colNo = new DataGridViewTextBoxColumn();
            colName = new DataGridViewTextBoxColumn();
            windowBar.SuspendLayout();
            panelBottom.SuspendLayout();
            panelContent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            //
            // windowBar
            //
            windowBar.BackColor = Color.White;
            windowBar.CloseSize = 30;
            windowBar.Controls.Add(btn_mode);
            windowBar.Controls.Add(btn_global);
            windowBar.Controls.Add(btn_setting);
            windowBar.DividerMargin = 1;
            windowBar.DividerShow = true;
            windowBar.Dock = DockStyle.Top;
            windowBar.Font = new Font("Microsoft YaHei UI", 11.25F);
            windowBar.ForeColor = Color.Black;
            windowBar.MDI = true;
            windowBar.Name = "windowBar";
            windowBar.Size = new Size(400, 36);
            windowBar.SubText = "";
            windowBar.TabIndex = 0;
            windowBar.Text = "Chọn danh sách nhóm";
            windowBar.UseSystemStyleColor = true;
            windowBar.UseTextBold = false;
            //
            // btn_mode
            //
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Ghost = true;
            btn_mode.Icon = Properties.Resources.icons8_circle_16_Green;
            btn_mode.IconSvg = "";
            btn_mode.Name = "btn_mode";
            btn_mode.Radius = 0;
            btn_mode.Size = new Size(26, 36);
            btn_mode.TabIndex = 3;
            btn_mode.WaveSize = 0;
            //
            // btn_global
            //
            btn_global.Dock = DockStyle.Right;
            btn_global.Ghost = true;
            btn_global.Icon = Properties.Resources.icons8_circle_16_Yellow;
            btn_global.IconSvg = "";
            btn_global.Name = "btn_global";
            btn_global.Radius = 0;
            btn_global.Size = new Size(24, 36);
            btn_global.TabIndex = 2;
            btn_global.WaveSize = 0;
            //
            // btn_setting
            //
            btn_setting.Dock = DockStyle.Right;
            btn_setting.Ghost = true;
            btn_setting.Icon = Properties.Resources.icons8_circle_16_Red;
            btn_setting.IconSvg = "";
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(30, 36);
            btn_setting.TabIndex = 1;
            btn_setting.WaveSize = 0;
            btn_setting.Click += btn_setting_Click;
            //
            // panelBottom
            //
            panelBottom.Controls.Add(btnSave);
            panelBottom.Controls.Add(btnClose);
            panelBottom.Dock = DockStyle.Bottom;
            panelBottom.Name = "panelBottom";
            panelBottom.padding = new Padding(12);
            panelBottom.Radius = 12;
            panelBottom.Size = new Size(400, 75);
            panelBottom.TabIndex = 2;
            panelBottom.Text = "";
            //
            // btnSave
            //
            btnSave.Anchor = AnchorStyles.Bottom;
            btnSave.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnSave.IconRatio = 1.1F;
            btnSave.IconSvg = "SaveOutlined";
            btnSave.Location = new Point(60, 17);
            btnSave.Name = "btnSave";
            btnSave.Shape = AntdUI.TShape.Round;
            btnSave.Size = new Size(120, 40);
            btnSave.TabIndex = 0;
            btnSave.Text = "Lưu";
            btnSave.Type = AntdUI.TTypeMini.Success;
            btnSave.Click += btnSave_Click;
            //
            // btnClose
            //
            btnClose.Anchor = AnchorStyles.Bottom;
            btnClose.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnClose.IconRatio = 1F;
            btnClose.IconSvg = "CloseOutlined";
            btnClose.Location = new Point(220, 17);
            btnClose.Name = "btnClose";
            btnClose.Shape = AntdUI.TShape.Round;
            btnClose.Size = new Size(120, 40);
            btnClose.TabIndex = 1;
            btnClose.Text = "Đóng";
            btnClose.Type = AntdUI.TTypeMini.Error;
            btnClose.Click += btnClose_Click;
            //
            // panelContent
            //
            panelContent.Controls.Add(dataGridView1);
            panelContent.Controls.Add(chkAll);
            panelContent.Controls.Add(lblCount);
            panelContent.Dock = DockStyle.Fill;
            panelContent.Name = "panelContent";
            panelContent.padding = new Padding(12);
            panelContent.Radius = 12;
            panelContent.TabIndex = 1;
            panelContent.Text = "";
            //
            // lblCount
            //
            lblCount.AutoSize = true;
            lblCount.BackColor = Color.Transparent;
            lblCount.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblCount.Location = new Point(15, 12);
            lblCount.Name = "lblCount";
            lblCount.TabIndex = 0;
            lblCount.Text = "Đã chọn 0/0";
            //
            // chkAll
            //
            chkAll.AutoSize = true;
            chkAll.BackColor = Color.Transparent;
            chkAll.Font = new Font("Segoe UI", 9F);
            chkAll.Location = new Point(330, 12);
            chkAll.Name = "chkAll";
            chkAll.TabIndex = 1;
            chkAll.Text = "Chọn tất cả";
            chkAll.CheckedChanged += chkAll_CheckedChanged;
            //
            // dataGridView1
            //
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { colCheck, colNo, colName });
            dataGridView1.Location = new Point(15, 38);
            dataGridView1.MultiSelect = false;
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ReadOnly = false;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.Size = new Size(368, 290);
            dataGridView1.TabIndex = 2;
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dataGridView1.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(26, 26, 26),
                SelectionBackColor = Color.FromArgb(0, 120, 215),
                SelectionForeColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 255);
            dataGridView1.CellValueChanged += dataGridView1_CellValueChanged;
            dataGridView1.CurrentCellDirtyStateChanged += dataGridView1_CurrentCellDirtyStateChanged;
            //
            // colCheck
            //
            colCheck.HeaderText = "";
            colCheck.Name = "colCheck";
            colCheck.Width = 40;
            colCheck.MinimumWidth = 40;
            colCheck.Resizable = DataGridViewTriState.False;
            colCheck.DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter };
            //
            // colNo
            //
            colNo.HeaderText = "#";
            colNo.Name = "colNo";
            colNo.Width = 50;
            colNo.ReadOnly = true;
            colNo.DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter };
            //
            // colName
            //
            colName.HeaderText = "Nhóm";
            colName.Name = "colName";
            colName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colName.ReadOnly = true;
            //
            // fSelectMultiFolder
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(236, 240, 241);
            ClientSize = new Size(400, 470);
            Controls.Add(panelContent);
            Controls.Add(panelBottom);
            Controls.Add(windowBar);
            MaximumSize = new Size(400, 470);
            MinimumSize = new Size(400, 470);
            Name = "fSelectMultiFolder";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "fSelectMultiFolder";
            windowBar.ResumeLayout(false);
            panelBottom.ResumeLayout(false);
            panelContent.ResumeLayout(false);
            panelContent.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
        }

        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_setting;
        private AntdUI.Dropdown btn_global;
        private AntdUI.Button btn_mode;
        private AntdUI.Panel panelBottom;
        private AntdUI.Button btnSave;
        private AntdUI.Button btnClose;
        private AntdUI.Panel panelContent;
        private Label lblCount;
        private CheckBox chkAll;
        private DataGridView dataGridView1;
        private DataGridViewCheckBoxColumn colCheck;
        private DataGridViewTextBoxColumn colNo;
        private DataGridViewTextBoxColumn colName;
    }
}
