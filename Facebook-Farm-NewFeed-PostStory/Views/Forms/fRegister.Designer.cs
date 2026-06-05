namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    partial class fRegister
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
            panel1 = new Panel();
            label1 = new Label();
            label2 = new Label();
            panel2 = new AntdUI.Panel();
            divider1 = new AntdUI.Divider();
            linkLabel1 = new LinkLabel();
            btnClose = new AntdUI.Button();
            btnRegister = new AntdUI.Button();
            txtUsername = new AntdUI.Input();
            txtEmail = new AntdUI.Input();
            txtFullName = new AntdUI.Input();
            txtPassword = new AntdUI.Input();
            txtConfirmPassword = new AntdUI.Input();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            //
            // panel1
            //
            panel1.BackColor = Color.FromArgb(4, 60, 44);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(label2);
            panel1.Dock = DockStyle.Left;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(252, 550);
            panel1.TabIndex = 0;
            //
            // label1
            //
            label1.AutoSize = true;
            label1.BackColor = Color.Transparent;
            label1.Font = new Font("Segoe UI Semibold", 26F);
            label1.ForeColor = Color.White;
            label1.Location = new Point(23, 150);
            label1.Name = "label1";
            label1.Size = new Size(213, 47);
            label1.TabIndex = 0;
            label1.Text = "QN.net";
            //
            // label2
            //
            label2.BackColor = Color.Transparent;
            label2.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.White;
            label2.Location = new Point(129, 204);
            label2.Name = "label2";
            label2.Size = new Size(107, 29);
            label2.TabIndex = 1;
            label2.Text = "Giải pháp MMO";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            //
            // panel2
            //
            panel2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel2.BackColor = Color.White;
            panel2.BackExtend = "";
            panel2.Controls.Add(divider1);
            panel2.Controls.Add(linkLabel1);
            panel2.Controls.Add(btnClose);
            panel2.Controls.Add(btnRegister);
            panel2.Controls.Add(txtUsername);
            panel2.Controls.Add(txtEmail);
            panel2.Controls.Add(txtFullName);
            panel2.Controls.Add(txtPassword);
            panel2.Controls.Add(txtConfirmPassword);
            panel2.Location = new Point(320, 30);
            panel2.Name = "panel2";
            panel2.Size = new Size(400, 490);
            panel2.TabIndex = 1;
            panel2.Text = "panel2";
            //
            // divider1
            //
            divider1.Location = new Point(0, 10);
            divider1.Name = "divider1";
            divider1.Size = new Size(400, 23);
            divider1.TabIndex = 0;
            divider1.Text = "Đăng ký tài khoản";
            //
            // txtFullName
            //
            txtFullName.AllowClear = true;
            txtFullName.Location = new Point(30, 55);
            txtFullName.Name = "txtFullName";
            txtFullName.Padding = new Padding(0, 2, 0, 2);
            txtFullName.PlaceholderText = "Họ và tên";
            txtFullName.PrefixSvg = "UserOutlined";
            txtFullName.Size = new Size(340, 39);
            txtFullName.TabIndex = 1;
            //
            // txtUsername
            //
            txtUsername.AllowClear = true;
            txtUsername.Location = new Point(30, 110);
            txtUsername.Name = "txtUsername";
            txtUsername.Padding = new Padding(0, 2, 0, 2);
            txtUsername.PlaceholderText = "Tên đăng nhập";
            txtUsername.PrefixSvg = "UserOutlined";
            txtUsername.Size = new Size(340, 39);
            txtUsername.TabIndex = 2;
            //
            // txtEmail
            //
            txtEmail.AllowClear = true;
            txtEmail.Location = new Point(30, 165);
            txtEmail.Name = "txtEmail";
            txtEmail.Padding = new Padding(0, 2, 0, 2);
            txtEmail.PlaceholderText = "Email";
            txtEmail.PrefixSvg = "MailOutlined";
            txtEmail.Size = new Size(340, 39);
            txtEmail.TabIndex = 3;
            //
            // txtPassword
            //
            txtPassword.AllowClear = true;
            txtPassword.Location = new Point(30, 220);
            txtPassword.Name = "txtPassword";
            txtPassword.Padding = new Padding(0, 2, 0, 2);
            txtPassword.PlaceholderText = "Mật khẩu";
            txtPassword.PrefixSvg = "EyeInvisibleOutlined";
            txtPassword.Size = new Size(340, 39);
            txtPassword.TabIndex = 4;
            txtPassword.UseSystemPasswordChar = true;
            //
            // txtConfirmPassword
            //
            txtConfirmPassword.AllowClear = true;
            txtConfirmPassword.Location = new Point(30, 275);
            txtConfirmPassword.Name = "txtConfirmPassword";
            txtConfirmPassword.Padding = new Padding(0, 2, 0, 2);
            txtConfirmPassword.PlaceholderText = "Xác nhận mật khẩu";
            txtConfirmPassword.PrefixSvg = "EyeInvisibleOutlined";
            txtConfirmPassword.Size = new Size(340, 39);
            txtConfirmPassword.TabIndex = 5;
            txtConfirmPassword.UseSystemPasswordChar = true;
            //
            // btnRegister
            //
            btnRegister.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnRegister.IconRatio = 1F;
            btnRegister.IconSvg = "UserAddOutlined";
            btnRegister.IconToggleAnimation = 400;
            btnRegister.Location = new Point(55, 340);
            btnRegister.Name = "btnRegister";
            btnRegister.Shape = AntdUI.TShape.Round;
            btnRegister.Size = new Size(130, 45);
            btnRegister.TabIndex = 6;
            btnRegister.Text = "Đăng ký";
            btnRegister.Type = AntdUI.TTypeMini.Primary;
            btnRegister.Click += btnRegister_Click;
            //
            // btnClose
            //
            btnClose.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnClose.IconRatio = 1F;
            btnClose.IconSvg = "CloseOutlined";
            btnClose.IconToggleAnimation = 400;
            btnClose.Location = new Point(210, 340);
            btnClose.Name = "btnClose";
            btnClose.Shape = AntdUI.TShape.Round;
            btnClose.Size = new Size(130, 45);
            btnClose.TabIndex = 7;
            btnClose.Text = "Quay lại";
            btnClose.Type = AntdUI.TTypeMini.Error;
            btnClose.Click += btnClose_Click;
            //
            // linkLabel1
            //
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(85, 410);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(220, 15);
            linkLabel1.TabIndex = 8;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "Đã có tài khoản? Đăng nhập ngay.";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            //
            // fRegister
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(243, 244, 246);
            ClientSize = new Size(800, 550);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "fRegister";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Đăng ký tài khoản";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Label label1;
        private Label label2;
        private AntdUI.Panel panel2;
        private AntdUI.Divider divider1;
        private AntdUI.Input txtFullName;
        private AntdUI.Input txtUsername;
        private AntdUI.Input txtEmail;
        private AntdUI.Input txtPassword;
        private AntdUI.Input txtConfirmPassword;
        private AntdUI.Button btnRegister;
        private AntdUI.Button btnClose;
        private LinkLabel linkLabel1;
    }
}
