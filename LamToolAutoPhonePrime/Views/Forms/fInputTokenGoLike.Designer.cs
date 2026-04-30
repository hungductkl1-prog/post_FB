namespace LamToolAutoPhonePrime.Views.Forms
{
    partial class fInputTokenGoLike
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
            windowBar = new AntdUI.PageHeader();
            btn_mode = new AntdUI.Button();
            btn_global = new AntdUI.Dropdown();
            btn_setting = new AntdUI.Button();
            panel1 = new AntdUI.Panel();
            btnCancel = new AntdUI.Button();
            btnOk = new AntdUI.Button();
            panel2 = new AntdUI.Panel();
            textBox1 = new TextBox();
            label1 = new Label();
            windowBar.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
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
            windowBar.Size = new Size(460, 36);
            windowBar.SubText = "";
            windowBar.TabIndex = 9;
            windowBar.Text = "Token GoLike";
            windowBar.UseSystemStyleColor = true;
            windowBar.UseTextBold = false;
            //
            // btn_mode
            //
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Ghost = true;
            btn_mode.Icon = Properties.Resources.icons8_circle_16_Green;
            btn_mode.IconSvg = "";
            btn_mode.Location = new Point(380, 0);
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
            btn_global.Location = new Point(406, 0);
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
            btn_setting.Location = new Point(430, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(30, 36);
            btn_setting.TabIndex = 9;
            btn_setting.WaveSize = 0;
            btn_setting.Click += btn_setting_Click;
            //
            // panel1
            //
            panel1.Controls.Add(btnCancel);
            panel1.Controls.Add(btnOk);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 131);
            panel1.Name = "panel1";
            panel1.padding = new Padding(12);
            panel1.Radius = 12;
            panel1.Size = new Size(460, 80);
            panel1.TabIndex = 10;
            //
            // btnCancel
            //
            btnCancel.Anchor = AnchorStyles.Bottom;
            btnCancel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnCancel.IconRatio = 1F;
            btnCancel.IconSvg = "CloseOutlined";
            btnCancel.IconToggleAnimation = 400;
            btnCancel.Location = new Point(250, 20);
            btnCancel.Name = "btnCancel";
            btnCancel.Shape = AntdUI.TShape.Round;
            btnCancel.Size = new Size(130, 40);
            btnCancel.TabIndex = 16;
            btnCancel.Text = "Hủy";
            btnCancel.Type = AntdUI.TTypeMini.Error;
            btnCancel.Click += btnCancel_Click;
            //
            // btnOk
            //
            btnOk.Anchor = AnchorStyles.Bottom;
            btnOk.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnOk.IconRatio = 1.1F;
            btnOk.IconSvg = "SaveOutlined";
            btnOk.IconToggleAnimation = 400;
            btnOk.Location = new Point(80, 20);
            btnOk.Name = "btnOk";
            btnOk.Shape = AntdUI.TShape.Round;
            btnOk.Size = new Size(130, 40);
            btnOk.TabIndex = 15;
            btnOk.Text = "OK";
            btnOk.Type = AntdUI.TTypeMini.Success;
            btnOk.Click += btnOk_Click;
            //
            // panel2
            //
            panel2.Controls.Add(textBox1);
            panel2.Controls.Add(label1);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(0, 36);
            panel2.Name = "panel2";
            panel2.padding = new Padding(12);
            panel2.Radius = 12;
            panel2.Size = new Size(460, 95);
            panel2.TabIndex = 11;
            //
            // textBox1
            //
            textBox1.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            textBox1.Location = new Point(90, 32);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(340, 27);
            textBox1.TabIndex = 18;
            //
            // label1
            //
            label1.AutoSize = true;
            label1.BackColor = Color.Transparent;
            label1.Location = new Point(25, 38);
            label1.Name = "label1";
            label1.Size = new Size(50, 15);
            label1.TabIndex = 17;
            label1.Text = "Token:";
            //
            // fInputTokenGoLike
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            BackColor = Color.FromArgb(236, 240, 241);
            ClientSize = new Size(460, 211);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Controls.Add(windowBar);
            MaximumSize = new Size(460, 211);
            MinimumSize = new Size(460, 211);
            Name = "fInputTokenGoLike";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Token GoLike";
            windowBar.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_mode;
        private AntdUI.Dropdown btn_global;
        private AntdUI.Button btn_setting;
        private AntdUI.Panel panel1;
        private AntdUI.Button btnOk;
        private AntdUI.Button btnCancel;
        private AntdUI.Panel panel2;
        private Label label1;
        private TextBox textBox1;
    }
}
