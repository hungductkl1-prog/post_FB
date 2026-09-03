namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    partial class fFolder
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
            btn_mode = new Button();
            btn_global = new Button();
            btn_setting = new Button();
            panel1 = new Panel();
            button1 = new Button();
            button9 = new Button();
            panel2 = new Panel();
            textBox2 = new TextBox();
            label2 = new Label();
            textBox1 = new TextBox();
            label1 = new Label();
            button2 = new Button();
            button3 = new Button();
            windowBar.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // windowBar
            // 
            windowBar.BackColor = Color.White;
            windowBar.Controls.Add(btn_mode);
            windowBar.Controls.Add(btn_global);
            windowBar.Controls.Add(btn_setting);
            windowBar.Cursor = Cursors.Hand;
            windowBar.Dock = DockStyle.Top;
            windowBar.Font = new Font("Microsoft YaHei UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            windowBar.ForeColor = Color.Black;
            windowBar.Location = new Point(0, 0);
            windowBar.Name = "windowBar";
            windowBar.Size = new Size(411, 36);
            windowBar.TabIndex = 9;
            windowBar.Text = "Cấu hình thêm tài khoản vào nhóm";
            // 
            // btn_mode
            // 
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Image = Properties.Resources.icons8_circle_16_Green;
            btn_mode.Location = new Point(331, 0);
            btn_mode.Name = "btn_mode";
            btn_mode.Size = new Size(26, 36);
            btn_mode.TabIndex = 11;
            // 
            // btn_global
            // 
            btn_global.Dock = DockStyle.Right;
            btn_global.Image = Properties.Resources.icons8_circle_16_Yellow;
            btn_global.Location = new Point(357, 0);
            btn_global.Name = "btn_global";
            btn_global.Size = new Size(24, 36);
            btn_global.TabIndex = 10;
            // 
            // btn_setting
            // 
            btn_setting.Dock = DockStyle.Right;
            btn_setting.Image = Properties.Resources.icons8_circle_16_Red;
            btn_setting.Location = new Point(381, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Size = new Size(30, 36);
            btn_setting.TabIndex = 9;
            btn_setting.Click += btn_setting_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(button1);
            panel1.Controls.Add(button9);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 207);
            panel1.Name = "panel1";
            panel1.Size = new Size(411, 89);
            panel1.TabIndex = 10;
            panel1.Text = "panel1";
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom;
            button1.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button1.Location = new Point(218, 23);
            button1.Name = "button1";
            button1.Size = new Size(139, 42);
            button1.TabIndex = 16;
            button1.Text = "Đóng";
            button1.FlatStyle = FlatStyle.Flat; button1.FlatAppearance.BorderSize = 0; button1.BackColor = Color.FromArgb(255, 77, 79); button1.ForeColor = Color.White; button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // button9
            // 
            button9.Anchor = AnchorStyles.Bottom;
            button9.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button9.Location = new Point(69, 23);
            button9.Name = "button9";
            button9.Size = new Size(131, 42);
            button9.TabIndex = 15;
            button9.Text = "Lưu";
            button9.FlatStyle = FlatStyle.Flat; button9.FlatAppearance.BorderSize = 0; button9.BackColor = Color.FromArgb(82, 196, 26); button9.ForeColor = Color.White; button9.UseVisualStyleBackColor = false;
            button9.Click += button9_Click;
            // 
            // panel2
            // 
            panel2.Controls.Add(textBox2);
            panel2.Controls.Add(label2);
            panel2.Controls.Add(textBox1);
            panel2.Controls.Add(label1);
            panel2.Controls.Add(button2);
            panel2.Controls.Add(button3);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(0, 36);
            panel2.Name = "panel2";
            panel2.Size = new Size(411, 171);
            panel2.TabIndex = 11;
            panel2.Text = "panel2";
            // 
            // textBox2
            // 
            textBox2.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            textBox2.Location = new Point(105, 97);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(260, 27);
            textBox2.TabIndex = 20;
            textBox2.Visible = false;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = Color.Transparent;
            label2.Location = new Point(33, 103);
            label2.Name = "label2";
            label2.Size = new Size(66, 15);
            label2.TabIndex = 19;
            label2.Text = "Tên nhóm: ";
            label2.Visible = false;
            // 
            // textBox1
            // 
            textBox1.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            textBox1.Location = new Point(105, 49);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(260, 27);
            textBox1.TabIndex = 18;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.Transparent;
            label1.Location = new Point(33, 55);
            label1.Name = "label1";
            label1.Size = new Size(66, 15);
            label1.TabIndex = 17;
            label1.Text = "Tên nhóm: ";
            // 
            // button2
            // 
            button2.Anchor = AnchorStyles.Bottom;
            button2.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button2.Location = new Point(423, 194);
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
            button3.Location = new Point(274, 194);
            button3.Name = "button3";
            button3.Size = new Size(131, 42);
            button3.TabIndex = 15;
            button3.Text = "Lưu";
            button3.FlatStyle = FlatStyle.Flat; button3.FlatAppearance.BorderSize = 0; button3.BackColor = Color.FromArgb(82, 196, 26); button3.ForeColor = Color.White; button3.UseVisualStyleBackColor = false;
            // 
            // fFolder
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
            Name = "fFolder";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "fFolder";
            windowBar.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel windowBar;
        private Button btn_mode;
        private Button btn_global;
        private Button btn_setting;
        private Panel panel1;
        private Button button9;
        private Button button1;
        private Panel panel2;
        private Button button2;
        private Button button3;
        private Label label1;
        private TextBox textBox1;
        private TextBox textBox2;
        private Label label2;
    }
}