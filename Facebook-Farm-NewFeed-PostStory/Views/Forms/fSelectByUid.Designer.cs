namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
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
            windowBar = new Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel();
            btn_setting = new Button();
            btn_global = new Button();
            btn_mode = new Button();
            panelBottom = new Panel();
            btnSave = new Button();
            btnClose = new Button();
            panelContent = new Panel();
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
            windowBar.Controls.Add(btn_mode);
            windowBar.Controls.Add(btn_global);
            windowBar.Controls.Add(btn_setting);
            windowBar.Dock = DockStyle.Top;
            windowBar.Font = new Font("Microsoft YaHei UI", 11.25F);
            windowBar.ForeColor = Color.Black;
            windowBar.Name = "windowBar";
            windowBar.Size = new Size(380, 36);
            windowBar.TabIndex = 0;
            windowBar.Text = "Chọn load tài khoản theo uid";
            //
            // btn_mode
            //
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Image = Properties.Resources.icons8_circle_16_Green;
            btn_mode.Name = "btn_mode";
            btn_mode.Size = new Size(26, 36);
            btn_mode.TabIndex = 3;
            //
            // btn_global
            //
            btn_global.Dock = DockStyle.Right;
            btn_global.Image = Properties.Resources.icons8_circle_16_Yellow;
            btn_global.Name = "btn_global";
            btn_global.Size = new Size(24, 36);
            btn_global.TabIndex = 2;
            //
            // btn_setting
            //
            btn_setting.Dock = DockStyle.Right;
            btn_setting.Image = Properties.Resources.icons8_circle_16_Red;
            btn_setting.Name = "btn_setting";
            btn_setting.Size = new Size(30, 36);
            btn_setting.TabIndex = 1;
            btn_setting.Click += btn_setting_Click;
            //
            // panelBottom
            //
            panelBottom.Controls.Add(btnSave);
            panelBottom.Controls.Add(btnClose);
            panelBottom.Dock = DockStyle.Bottom;
            panelBottom.Name = "panelBottom";
            panelBottom.Size = new Size(380, 75);
            panelBottom.TabIndex = 2;
            panelBottom.Text = "";
            //
            // btnSave
            //
            btnSave.Anchor = AnchorStyles.Bottom;
            btnSave.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnSave.Location = new Point(50, 17);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(120, 40);
            btnSave.TabIndex = 0;
            btnSave.Text = "Lưu";
            btnSave.FlatStyle = FlatStyle.Flat; btnSave.FlatAppearance.BorderSize = 0; btnSave.BackColor = Color.FromArgb(82, 196, 26); btnSave.ForeColor = Color.White; btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += btnSave_Click;
            //
            // btnClose
            //
            btnClose.Anchor = AnchorStyles.Bottom;
            btnClose.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnClose.Location = new Point(210, 17);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(120, 40);
            btnClose.TabIndex = 1;
            btnClose.Text = "Đóng";
            btnClose.FlatStyle = FlatStyle.Flat; btnClose.FlatAppearance.BorderSize = 0; btnClose.BackColor = Color.FromArgb(255, 77, 79); btnClose.ForeColor = Color.White; btnClose.UseVisualStyleBackColor = false;
            btnClose.Click += btnClose_Click;
            //
            // panelContent
            //
            panelContent.Controls.Add(lblHint);
            panelContent.Controls.Add(txtUids);
            panelContent.Controls.Add(lblCount);
            panelContent.Dock = DockStyle.Fill;
            panelContent.Name = "panelContent";
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
            txtUids.MaxLength = int.MaxValue;
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
            lblHint.Font = new Font("Segoe UI", Facebook_Farm_NewFeed_PostStory.Utils.Design.FontScale.Body);
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

        private Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel windowBar;
        private Button btn_setting;
        private Button btn_global;
        private Button btn_mode;
        private Panel panelBottom;
        private Button btnSave;
        private Button btnClose;
        private Panel panelContent;
        private Label lblCount;
        private TextBox txtUids;
        private Label lblHint;
    }
}
