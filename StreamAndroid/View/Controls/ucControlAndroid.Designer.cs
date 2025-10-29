namespace StreamAndroid
{
    partial class ucControlAndroid
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
            pictureBox1 = new PictureBox();
            button4 = new AntdUI.Button();
            button5 = new AntdUI.Button();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.ArrowSize = 20;
            panel1.BackColor = Color.Transparent;
            panel1.BackExtend = "";
            panel1.BorderColor = Color.RoyalBlue;
            panel1.BorderWidth = 7F;
            panel1.ColorScheme = AntdUI.TAMode.Light;
            panel1.Controls.Add(pictureBox1);
            panel1.Controls.Add(button4);
            panel1.Controls.Add(button5);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Radius = 10;
            panel1.Size = new Size(896, 1250);
            panel1.TabIndex = 2;
            panel1.Text = "panel3";
            // 
            // pictureBox1
            // 
            pictureBox1.Cursor = Cursors.Hand;
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.Location = new Point(7, 7);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(882, 1236);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
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
            button4.Location = new Point(1018, 1185);
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
            button5.Location = new Point(1018, 1145);
            button5.Name = "button5";
            button5.OriginalBackColor = Color.FromArgb(60, 60, 60);
            button5.Radius = 2;
            button5.Size = new Size(39, 34);
            button5.TabIndex = 8;
            // 
            // ucControlAndroid
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Transparent;
            Controls.Add(panel1);
            Name = "ucControlAndroid";
            Size = new Size(896, 1250);
            panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);

            // Apply initial proportional layout

        }
        private PictureBox pictureBox1;
        private AntdUI.Button button4;
        private AntdUI.Button button5;
        public AntdUI.Panel panel1;

        #endregion
    }
}
