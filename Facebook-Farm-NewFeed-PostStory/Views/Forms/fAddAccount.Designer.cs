using System.Drawing;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    partial class fAddAccount
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
            flowLayoutPanel1 = new FlowLayoutPanel();
            uiLabel1 = new Label();
            uiLabel2 = new Label();
            windowBar = new Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel();
            btn_mode = new Button();
            btn_global = new Button();
            btn_setting = new Button();
            panel4 = new Panel();
            txtLines = new TextBox();
            panel5 = new Panel();
            label4 = new Label();
            label3 = new Label();
            panel3 = new Panel();
            button2 = new Button();
            select8 = new AntdUI.Select();
            panel1 = new Panel();
            button1 = new Button();
            button9 = new Button();
            windowBar.SuspendLayout();
            panel4.SuspendLayout();
            panel5.SuspendLayout();
            panel3.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            flowLayoutPanel1.Location = new Point(105, 43);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(1087, 31);
            flowLayoutPanel1.TabIndex = 11;
            // 
            // uiLabel1
            // 
            uiLabel1.AutoSize = true;
            uiLabel1.BackColor = Color.White;
            uiLabel1.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            uiLabel1.ForeColor = Color.DimGray;
            uiLabel1.Location = new Point(24, 50);
            uiLabel1.Name = "uiLabel1";
            uiLabel1.Size = new Size(67, 15);
            uiLabel1.TabIndex = 10;
            uiLabel1.Text = "Định dạng:";
            // 
            // uiLabel2
            // 
            uiLabel2.AutoSize = true;
            uiLabel2.BackColor = Color.White;
            uiLabel2.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            uiLabel2.ForeColor = Color.DimGray;
            uiLabel2.Location = new Point(24, 14);
            uiLabel2.Name = "uiLabel2";
            uiLabel2.Size = new Size(74, 15);
            uiLabel2.TabIndex = 2;
            uiLabel2.Text = "Chọn nhóm:";
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
            windowBar.Size = new Size(1265, 36);
            windowBar.TabIndex = 8;
            windowBar.Text = "Cấu hình thêm tài khoản vào nhóm";
            // 
            // btn_mode
            // 
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Image = Properties.Resources.icons8_circle_16_Green;
            btn_mode.Location = new Point(1185, 0);
            btn_mode.Name = "btn_mode";
            btn_mode.Size = new Size(26, 36);
            btn_mode.TabIndex = 11;
            // 
            // btn_global
            // 
            btn_global.Dock = DockStyle.Right;
            btn_global.Image = Properties.Resources.icons8_circle_16_Yellow;
            btn_global.Location = new Point(1211, 0);
            btn_global.Name = "btn_global";
            btn_global.Size = new Size(24, 36);
            btn_global.TabIndex = 10;
            btn_global.Click += btn_global_Click_1;
            // 
            // btn_setting
            // 
            btn_setting.Dock = DockStyle.Right;
            btn_setting.Image = Properties.Resources.icons8_circle_16_Red;
            btn_setting.Location = new Point(1235, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Size = new Size(30, 36);
            btn_setting.TabIndex = 9;
            btn_setting.Click += btn_setting_Click_2;
            // 
            // panel4
            // 
            panel4.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel4.Controls.Add(txtLines);
            panel4.Location = new Point(28, 110);
            panel4.Name = "panel4";
            panel4.Size = new Size(1216, 282);
            panel4.TabIndex = 9;
            panel4.Text = "panel4";
            // 
            // txtLines
            // 
            txtLines.BorderStyle = BorderStyle.FixedSingle;
            txtLines.Dock = DockStyle.Fill;
            txtLines.Location = new Point(0, 0);
            txtLines.MaxLength = int.MaxValue;
            txtLines.Multiline = true;
            txtLines.Name = "txtLines";
            txtLines.PlaceholderText = "     Ví dụ: uid|password|2fa";
            txtLines.ScrollBars = ScrollBars.Both;
            txtLines.Size = new Size(1216, 282);
            txtLines.TabIndex = 7;
            txtLines.WordWrap = false;
            txtLines.TextChanged += txtLines_TextChanged_1;
            // 
            // panel5
            // 
            panel5.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel5.Controls.Add(label4);
            panel5.Controls.Add(label3);
            panel5.Location = new Point(28, 61);
            panel5.Name = "panel5";
            panel5.Size = new Size(1216, 32);
            panel5.TabIndex = 10;
            panel5.Text = "panel5";
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.Right;
            label4.AutoSize = true;
            label4.BackColor = Color.White;
            label4.Location = new Point(961, 5);
            label4.Name = "label4";
            label4.Padding = new Padding(20, 5, 5, 5);
            label4.Size = new Size(219, 25);
            label4.TabIndex = 2;
            label4.Text = "Một tài khoản tương đương 1 dòng";
            label4.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Left;
            label3.AutoSize = true;
            label3.BackColor = Color.White;
            label3.Location = new Point(24, 4);
            label3.Name = "label3";
            label3.Padding = new Padding(20, 5, 5, 5);
            label3.Size = new Size(159, 25);
            label3.TabIndex = 1;
            label3.Text = "Danh sách tài khoản (0):";
            label3.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panel3
            // 
            panel3.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel3.Controls.Add(button2);
            panel3.Controls.Add(select8);
            panel3.Controls.Add(flowLayoutPanel1);
            panel3.Controls.Add(uiLabel1);
            panel3.Controls.Add(uiLabel2);
            panel3.Location = new Point(28, 409);
            panel3.Name = "panel3";
            panel3.Size = new Size(1207, 82);
            panel3.TabIndex = 11;
            panel3.Text = "panel3";
            // 
            // button2
            // 
            button2.Anchor = AnchorStyles.Left;
            button2.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button2.Location = new Point(362, 5);
            button2.Name = "button2";
            button2.Size = new Size(119, 32);
            button2.TabIndex = 15;
            button2.Text = "Thêm nhóm";
            button2.FlatStyle = FlatStyle.Flat; button2.FlatAppearance.BorderSize = 0; button2.BackColor = Color.FromArgb(22, 119, 255); button2.ForeColor = Color.White; button2.UseVisualStyleBackColor = false;
            button2.Click += button2_Click;
            // 
            // select8
            // 
            select8.Anchor = AnchorStyles.Left;
            select8.DropDownArrow = true;
            select8.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            select8.ForeColor = Color.Black;
            select8.Items.AddRange(new object[] { "QN.net", "Tuongtaccheo.com", "Traodoisub.com", "Vipig.net" });
            select8.List = true;
            select8.ListAutoWidth = true;
            select8.LocalizationPlaceholderText = "Select.{id}";
            select8.Location = new Point(105, 5);
            select8.Margin = new Padding(2, 3, 2, 3);
            select8.Name = "select8";
            select8.PlaceholderText = "";
            select8.Placement = AntdUI.TAlignFrom.Bottom;
            select8.Size = new Size(252, 32);
            select8.TabIndex = 12;
            select8.SelectedIndexChanged += select8_SelectedIndexChanged;
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel1.Controls.Add(button1);
            panel1.Controls.Add(button9);
            panel1.Location = new Point(28, 508);
            panel1.Name = "panel1";
            panel1.Size = new Size(1216, 48);
            panel1.TabIndex = 12;
            panel1.Text = "panel1";
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom;
            button1.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button1.Location = new Point(624, 3);
            button1.Name = "button1";
            button1.Size = new Size(139, 42);
            button1.TabIndex = 15;
            button1.Text = "Đóng";
            button1.FlatStyle = FlatStyle.Flat; button1.FlatAppearance.BorderSize = 0; button1.BackColor = Color.FromArgb(255, 77, 79); button1.ForeColor = Color.White; button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // button9
            // 
            button9.Anchor = AnchorStyles.Bottom;
            button9.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button9.Location = new Point(453, 3);
            button9.Name = "button9";
            button9.Size = new Size(139, 42);
            button9.TabIndex = 14;
            button9.Text = "Lưu";
            button9.FlatStyle = FlatStyle.Flat; button9.FlatAppearance.BorderSize = 0; button9.BackColor = Color.FromArgb(82, 196, 26); button9.ForeColor = Color.White; button9.UseVisualStyleBackColor = false;
            button9.Click += button9_Click;
            // 
            // fAddAccount
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(236, 240, 241);
            ClientSize = new Size(1265, 568);
            Controls.Add(panel1);
            Controls.Add(panel3);
            Controls.Add(panel5);
            Controls.Add(panel4);
            Controls.Add(windowBar);
            MinimumSize = new Size(1265, 568);
            Name = "fAddAccount";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            windowBar.ResumeLayout(false);
            panel4.ResumeLayout(false);
            panel4.PerformLayout();
            panel5.ResumeLayout(false);
            panel5.PerformLayout();
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private Label uiLabel2;
        private FlowLayoutPanel flowLayoutPanel1;
        private Label uiLabel1;
        private Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel windowBar;
        private Panel panel4;
        private Panel panel5;
        private TextBox txtLines;
        private Label label4;
        private Label label3;
        private Panel panel3;
        private Panel panel1;
        private Button button1;
        private Button button9;
        private Button button2;
        private AntdUI.Select select8;
        private Button btn_setting;
        private Button btn_global;
        private Button btn_mode;
    }
}