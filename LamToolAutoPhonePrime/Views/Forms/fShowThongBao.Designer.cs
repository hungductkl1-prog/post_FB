namespace LamToolAutoPhonePrime
{
    partial class fShowThongBao
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
            windowBar = new AntdUI.PageHeader();
            panel3 = new Panel();
            button6 = new AntdUI.Button();
            button5 = new AntdUI.Button();
            button4 = new AntdUI.Button();
            panel1 = new AntdUI.Panel();
            button9 = new AntdUI.Button();
            panel2 = new AntdUI.Panel();
            alert10 = new AntdUI.Alert();
            button2 = new AntdUI.Button();
            button3 = new AntdUI.Button();
            windowBar.SuspendLayout();
            panel3.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // windowBar
            // 
            windowBar.BackColor = Color.White;
            windowBar.BackgroundImageLayout = ImageLayout.Stretch;
            windowBar.CloseSize = 30;
            windowBar.Controls.Add(panel3);
            windowBar.Cursor = Cursors.Hand;
            windowBar.DividerMargin = 1;
            windowBar.DividerShow = true;
            windowBar.Dock = DockStyle.Top;
            windowBar.Font = new Font("Microsoft YaHei UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            windowBar.ForeColor = Color.Black;
            windowBar.Location = new Point(0, 0);
            windowBar.MDI = true;
            windowBar.Name = "windowBar";
            windowBar.Size = new Size(411, 36);
            windowBar.SubText = "";
            windowBar.TabIndex = 9;
            windowBar.Text = "LamTool Auto Phone Farm";
            windowBar.UseSystemStyleColor = true;
            windowBar.UseTextBold = false;
            // 
            // panel3
            // 
            panel3.Controls.Add(button6);
            panel3.Controls.Add(button5);
            panel3.Controls.Add(button4);
            panel3.Dock = DockStyle.Right;
            panel3.Location = new Point(341, 0);
            panel3.Name = "panel3";
            panel3.Padding = new Padding(5);
            panel3.Size = new Size(70, 36);
            panel3.TabIndex = 22;
            // 
            // button6
            // 
            button6.BadgeSize = 1.5F;
            button6.Cursor = Cursors.Hand;
            button6.Dock = DockStyle.Right;
            button6.IconRatio = 5F;
            button6.Location = new Point(2, 5);
            button6.Name = "button6";
            button6.Radius = 30;
            button6.Shape = AntdUI.TShape.Circle;
            button6.Size = new Size(21, 26);
            button6.TabIndex = 23;
            button6.Type = AntdUI.TTypeMini.Success;
            // 
            // button5
            // 
            button5.BadgeSize = 1.5F;
            button5.Cursor = Cursors.Hand;
            button5.Dock = DockStyle.Right;
            button5.IconRatio = 5F;
            button5.Location = new Point(23, 5);
            button5.Name = "button5";
            button5.Radius = 30;
            button5.Shape = AntdUI.TShape.Circle;
            button5.Size = new Size(21, 26);
            button5.TabIndex = 22;
            button5.Type = AntdUI.TTypeMini.Warn;
            // 
            // button4
            // 
            button4.BadgeSize = 1.5F;
            button4.Cursor = Cursors.Hand;
            button4.Dock = DockStyle.Right;
            button4.IconRatio = 5F;
            button4.Location = new Point(44, 5);
            button4.Name = "button4";
            button4.Radius = 30;
            button4.Shape = AntdUI.TShape.Circle;
            button4.Size = new Size(21, 26);
            button4.TabIndex = 21;
            button4.Type = AntdUI.TTypeMini.Error;
            button4.Click += button4_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(button9);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 207);
            panel1.Name = "panel1";
            panel1.padding = new Padding(12);
            panel1.Radius = 12;
            panel1.Size = new Size(411, 89);
            panel1.TabIndex = 10;
            panel1.Text = "panel1";
            // 
            // button9
            // 
            button9.Anchor = AnchorStyles.Bottom;
            button9.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button9.IconRatio = 1.1F;
            button9.IconSvg = "CheckOutlined";
            button9.IconToggleAnimation = 400;
            button9.Location = new Point(124, 23);
            button9.Name = "button9";
            button9.Shape = AntdUI.TShape.Round;
            button9.Size = new Size(182, 42);
            button9.TabIndex = 15;
            button9.Text = "Xác nhận";
            button9.Type = AntdUI.TTypeMini.Success;
            button9.Click += button9_Click;
            // 
            // panel2
            // 
            panel2.Controls.Add(alert10);
            panel2.Controls.Add(button2);
            panel2.Controls.Add(button3);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(0, 36);
            panel2.Name = "panel2";
            panel2.padding = new Padding(12);
            panel2.Padding = new Padding(12);
            panel2.Radius = 12;
            panel2.Size = new Size(411, 171);
            panel2.TabIndex = 11;
            panel2.Text = "panel2";
            // 
            // alert10
            // 
            alert10.BorderWidth = 1F;
            alert10.Dock = DockStyle.Fill;
            alert10.Icon = AntdUI.TType.Info;
            alert10.Location = new Point(12, 12);
            alert10.Name = "alert10";
            alert10.Size = new Size(387, 147);
            alert10.TabIndex = 17;
            alert10.Text = "sadsa";
            alert10.TextTitle = "Info Text";
            // 
            // button2
            // 
            button2.Anchor = AnchorStyles.Bottom;
            button2.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button2.IconRatio = 1F;
            button2.IconSvg = "CloseOutlined";
            button2.IconToggleAnimation = 400;
            button2.Location = new Point(423, 182);
            button2.Name = "button2";
            button2.Shape = AntdUI.TShape.Round;
            button2.Size = new Size(139, 42);
            button2.TabIndex = 16;
            button2.Text = "Đóng";
            button2.Type = AntdUI.TTypeMini.Error;
            // 
            // button3
            // 
            button3.Anchor = AnchorStyles.Bottom;
            button3.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button3.IconRatio = 1.1F;
            button3.IconSvg = "SaveOutlined";
            button3.IconToggleAnimation = 400;
            button3.Location = new Point(274, 182);
            button3.Name = "button3";
            button3.Shape = AntdUI.TShape.Round;
            button3.Size = new Size(131, 42);
            button3.TabIndex = 15;
            button3.Text = "Lưu";
            button3.Type = AntdUI.TTypeMini.Success;
            // 
            // fShowThongBao
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            BackColor = Color.FromArgb(236, 240, 241);
            ClientSize = new Size(411, 296);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Controls.Add(windowBar);
            MaximumSize = new Size(411, 296);
            MinimumSize = new Size(411, 296);
            Name = "fShowThongBao";
            StartPosition = FormStartPosition.CenterScreen;
            windowBar.ResumeLayout(false);
            panel3.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private AntdUI.PageHeader windowBar;
        private AntdUI.Panel panel1;
        private AntdUI.Button button9;
        private AntdUI.Panel panel2;
        private AntdUI.Button button2;
        private AntdUI.Button button3;
        private AntdUI.Button button4;
        private Panel panel3;
        private AntdUI.Button button6;
        private AntdUI.Button button5;
        private AntdUI.Alert alert10;
    }
}