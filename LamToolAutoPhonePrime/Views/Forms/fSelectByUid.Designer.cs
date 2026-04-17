namespace LamToolAutoPhonePrime.Views.Forms
{
    partial class fSelectByUid
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
            txtUids = new TextBox();
            lblHint = new Label();
            windowBar.SuspendLayout();
            panelBottom.SuspendLayout();
            panelContent.SuspendLayout();
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
            windowBar.Size = new Size(380, 36);
            windowBar.SubText = "";
            windowBar.TabIndex = 0;
            windowBar.Text = "Chọn load tài khoản theo uid";
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
            panelBottom.Size = new Size(380, 75);
            panelBottom.TabIndex = 2;
            panelBottom.Text = "";
            //
            // btnSave
            //
            btnSave.Anchor = AnchorStyles.Bottom;
            btnSave.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnSave.IconRatio = 1.1F;
            btnSave.IconSvg = "SaveOutlined";
            btnSave.Location = new Point(50, 17);
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
            btnClose.Location = new Point(210, 17);
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
            panelContent.Controls.Add(lblHint);
            panelContent.Controls.Add(txtUids);
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
            lblCount.Font = new Font("Segoe UI", 9.5F);
            lblCount.Location = new Point(15, 15);
            lblCount.Name = "lblCount";
            lblCount.TabIndex = 0;
            lblCount.Text = "Danh sách uid (0):";
            //
            // txtUids
            //
            txtUids.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtUids.Font = new Font("Segoe UI", 9.5F);
            txtUids.Location = new Point(15, 38);
            txtUids.Multiline = true;
            txtUids.Name = "txtUids";
            txtUids.ScrollBars = ScrollBars.Both;
            txtUids.Size = new Size(350, 220);
            txtUids.TabIndex = 1;
            //
            // lblHint
            //
            lblHint.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblHint.AutoSize = true;
            lblHint.BackColor = Color.Transparent;
            lblHint.Font = new Font("Segoe UI", 8.5F);
            lblHint.ForeColor = Color.FromArgb(0, 153, 51);
            lblHint.Location = new Point(15, 265);
            lblHint.Name = "lblHint";
            lblHint.TabIndex = 2;
            lblHint.Text = "Mỗi uid tương đương 1 dòng";
            //
            // fSelectByUid
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(236, 240, 241);
            ClientSize = new Size(380, 420);
            Controls.Add(panelContent);
            Controls.Add(panelBottom);
            Controls.Add(windowBar);
            MaximumSize = new Size(380, 420);
            MinimumSize = new Size(380, 420);
            Name = "fSelectByUid";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "fSelectByUid";
            windowBar.ResumeLayout(false);
            panelBottom.ResumeLayout(false);
            panelContent.ResumeLayout(false);
            panelContent.PerformLayout();
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
        private TextBox txtUids;
        private Label lblHint;
    }
}
