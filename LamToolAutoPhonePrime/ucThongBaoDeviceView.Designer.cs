namespace LamToolAutoPhonePrime
{
    partial class ucThongBaoDeviceView
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panel1 = new AntdUI.Panel();
            button4 = new AntdUI.Button();
            button5 = new AntdUI.Button();
            label1 = new Label();
            label2 = new AntdUI.Label();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.ArrowSize = 20;
            panel1.Back = Color.FromArgb(232, 239, 223);
            panel1.BackColor = Color.Transparent;
            panel1.BackExtend = "";
            panel1.BorderColor = Color.RoyalBlue;
            panel1.BorderWidth = 7F;
            panel1.ColorScheme = AntdUI.TAMode.Light;
            panel1.Controls.Add(label2);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(button4);
            panel1.Controls.Add(button5);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Radius = 10;
            panel1.Size = new Size(275, 401);
            panel1.TabIndex = 3;
            panel1.Text = "panel3";
            // 
            // button4
            // 
            button4.Anchor = AnchorStyles.None;
            button4.BackColor = Color.FromArgb(60, 60, 60);
            button4.BackHover = Color.FromArgb(60, 60, 60);
            button4.BadgeSize = 0F;
            button4.ColorScheme = AntdUI.TAMode.Light;
            button4.Cursor = Cursors.Hand;
            button4.DefaultBack = Color.FromArgb(60, 60, 60);
            button4.DefaultBorderColor = Color.Transparent;
            button4.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            button4.ForeColor = Color.White;
            button4.IconGap = 0F;
            button4.IconHoverSvg = "";
            button4.IconSvg = "ExclamationCircleOutlined";
            button4.Location = new Point(1155, 1385);
            button4.Name = "button4";
            button4.OriginalBackColor = Color.FromArgb(60, 60, 60);
            button4.Size = new Size(39, 34);
            button4.TabIndex = 9;
            button4.ToggleBack = Color.FromArgb(60, 60, 60);
            button4.ToggleFore = Color.FromArgb(60, 60, 60);
            // 
            // button5
            // 
            button5.Anchor = AnchorStyles.None;
            button5.BackColor = Color.FromArgb(60, 60, 60);
            button5.BadgeSize = 0F;
            button5.ColorScheme = AntdUI.TAMode.Light;
            button5.Cursor = Cursors.Hand;
            button5.DefaultBack = Color.FromArgb(60, 60, 60);
            button5.DefaultBorderColor = Color.FromArgb(60, 60, 60);
            button5.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            button5.ForeColor = Color.White;
            button5.IconGap = 0F;
            button5.IconHoverSvg = "";
            button5.IconSvg = "PlusCircleOutlined";
            button5.Location = new Point(1155, 1345);
            button5.Name = "button5";
            button5.OriginalBackColor = Color.FromArgb(60, 60, 60);
            button5.Radius = 2;
            button5.Size = new Size(39, 34);
            button5.TabIndex = 8;
            // 
            // label1
            // 
            label1.Dock = DockStyle.Top;
            label1.FlatStyle = FlatStyle.Flat;
            label1.Font = new Font("Segoe UI", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.FromArgb(247, 125, 38);
            label1.Location = new Point(7, 7);
            label1.Name = "label1";
            label1.Size = new Size(261, 61);
            label1.TabIndex = 10;
            label1.Text = "01\r\nSM-G930F";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            label2.Dock = DockStyle.Fill;
            label2.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.FromArgb(247, 125, 38);
            label2.Location = new Point(7, 68);
            label2.Name = "label2";
            label2.Size = new Size(261, 326);
            label2.TabIndex = 11;
            label2.Text = "Điện thoại đã ngắt kết nối, vui lòng kiểm tra...";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // ucThongBaoDeviceView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Transparent;
            Controls.Add(panel1);
            Name = "ucThongBaoDeviceView";
            Size = new Size(275, 401);
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private AntdUI.Panel panel1;
        private AntdUI.Button button4;
        private AntdUI.Button button5;
        private AntdUI.Label label2;
        private Label label1;
    }
}
