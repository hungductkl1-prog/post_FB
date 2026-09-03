namespace Facebook_Farm_NewFeed_PostStory
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
            windowBar = new Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel();
            panel3 = new Panel();
            button6 = new Button();
            button5 = new Button();
            button4 = new Button();
            panel1 = new Panel();
            button9 = new Button();
            panel2 = new Panel();
            alert10 = new AntdUI.Alert();
            button2 = new Button();
            button3 = new Button();
            windowBar.SuspendLayout();
            panel3.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // windowBar
            // 
            windowBar.BackColor = Color.White;
            windowBar.Controls.Add(panel3);
            windowBar.Cursor = Cursors.Hand;
            windowBar.Dock = DockStyle.Top;
            windowBar.Font = new Font("Microsoft YaHei UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            windowBar.ForeColor = Color.Black;
            windowBar.Location = new Point(0, 0);
            windowBar.Name = "windowBar";
            windowBar.Size = new Size(411, 36);
            windowBar.TabIndex = 9;
            windowBar.Text = "Thông báo";
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
            button6.Cursor = Cursors.Hand;
            button6.Dock = DockStyle.Right;
            button6.Location = new Point(2, 5);
            button6.Name = "button6";
            button6.Size = new Size(21, 26);
            button6.TabIndex = 23;
            button6.FlatStyle = FlatStyle.Flat; button6.FlatAppearance.BorderSize = 0; button6.BackColor = Color.FromArgb(82, 196, 26); button6.ForeColor = Color.White; button6.UseVisualStyleBackColor = false;
            // 
            // button5
            // 
            button5.Cursor = Cursors.Hand;
            button5.Dock = DockStyle.Right;
            button5.Location = new Point(23, 5);
            button5.Name = "button5";
            button5.Size = new Size(21, 26);
            button5.TabIndex = 22;
            button5.FlatStyle = FlatStyle.Flat; button5.FlatAppearance.BorderSize = 0; button5.BackColor = Color.FromArgb(250, 173, 20); button5.ForeColor = Color.White; button5.UseVisualStyleBackColor = false;
            // 
            // button4
            // 
            button4.Cursor = Cursors.Hand;
            button4.Dock = DockStyle.Right;
            button4.Location = new Point(44, 5);
            button4.Name = "button4";
            button4.Size = new Size(21, 26);
            button4.TabIndex = 21;
            button4.FlatStyle = FlatStyle.Flat; button4.FlatAppearance.BorderSize = 0; button4.BackColor = Color.FromArgb(255, 77, 79); button4.ForeColor = Color.White; button4.UseVisualStyleBackColor = false;
            button4.Click += button4_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(button9);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 207);
            panel1.Name = "panel1";
            panel1.Padding = new Padding(12);
            panel1.Size = new Size(411, 89);
            panel1.TabIndex = 10;
            panel1.Text = "panel1";
            // 
            // button9
            // 
            button9.Anchor = AnchorStyles.Bottom;
            button9.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button9.Location = new Point(124, 23);
            button9.Name = "button9";
            button9.Size = new Size(182, 42);
            button9.TabIndex = 15;
            button9.Text = "Xác nhận";
            button9.FlatStyle = FlatStyle.Flat; button9.FlatAppearance.BorderSize = 0; button9.BackColor = Color.FromArgb(82, 196, 26); button9.ForeColor = Color.White; button9.UseVisualStyleBackColor = false;
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
            panel2.Padding = new Padding(12);
            panel2.Padding = new Padding(12);
            panel2.Size = new Size(411, 171);
            panel2.TabIndex = 11;
            panel2.Text = "panel2";
            // 
            // alert10
            // 
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
            button2.Location = new Point(423, 182);
            button2.Name = "button2";
            button2.Size = new Size(139, 42);
            button2.TabIndex = 16;
            button2.Text = "Đóng";
            button2.FlatStyle = FlatStyle.Flat; button2.FlatAppearance.BorderSize = 0; button2.BackColor = Color.FromArgb(255, 77, 79); button2.ForeColor = Color.White; button2.UseVisualStyleBackColor = false;
            // 
            // button3
            // 
            button3.Anchor = AnchorStyles.Bottom;
            button3.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button3.Location = new Point(274, 182);
            button3.Name = "button3";
            button3.Size = new Size(131, 42);
            button3.TabIndex = 15;
            button3.Text = "Lưu";
            button3.FlatStyle = FlatStyle.Flat; button3.FlatAppearance.BorderSize = 0; button3.BackColor = Color.FromArgb(82, 196, 26); button3.ForeColor = Color.White; button3.UseVisualStyleBackColor = false;
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

        private Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel windowBar;
        private Panel panel1;
        private Button button9;
        private Panel panel2;
        private Button button2;
        private Button button3;
        private Button button4;
        private Panel panel3;
        private Button button6;
        private Button button5;
        private AntdUI.Alert alert10;
    }
}