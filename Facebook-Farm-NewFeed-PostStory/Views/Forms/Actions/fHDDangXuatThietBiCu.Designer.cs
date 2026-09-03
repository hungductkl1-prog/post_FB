using System.ComponentModel;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions
{
    partial class fHDDangXuatThietBiCu
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
            D33B07B4 = new Panel();
            button1 = new Button();
            pictureBox1 = new PictureBox();
            panel1 = new Panel();
            windowBar = new Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel();
            btn_mode = new Button();
            btn_global = new Button();
            btn_setting = new Button();
            txtTenHanhDong = new TextBox();
            A4A8DE3C = new Label();
            btnCancel = new Button();
            btnSave = new Button();
            D33B07B4.SuspendLayout();
            ((ISupportInitialize)pictureBox1).BeginInit();
            panel1.SuspendLayout();
            windowBar.SuspendLayout();
            SuspendLayout();
            // 
            // D33B07B4
            // 
            D33B07B4.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            D33B07B4.BackColor = Color.White;
            D33B07B4.Controls.Add(button1);
            D33B07B4.Controls.Add(pictureBox1);
            D33B07B4.Cursor = Cursors.SizeAll;
            D33B07B4.Location = new Point(0, 3);
            D33B07B4.Name = "D33B07B4";
            D33B07B4.Size = new Size(359, 31);
            D33B07B4.TabIndex = 9;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button1.Cursor = Cursors.Hand;
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button1.ForeColor = Color.White;
            button1.Location = new Point(328, 1);
            button1.Name = "button1";
            button1.Size = new Size(30, 30);
            button1.TabIndex = 77;
            button1.TextImageRelation = TextImageRelation.ImageBeforeText;
            button1.UseVisualStyleBackColor = true;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(3, 2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(34, 27);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 76;
            pictureBox1.TabStop = false;
            // 
            // panel1
            // 
            panel1.BackColor = Color.White;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(windowBar);
            panel1.Controls.Add(txtTenHanhDong);
            panel1.Controls.Add(A4A8DE3C);
            panel1.Controls.Add(btnCancel);
            panel1.Controls.Add(btnSave);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(362, 142);
            panel1.TabIndex = 0;
            // 
            // windowBar
            // 
            windowBar.BackColor = Color.White;
            windowBar.Controls.Add(btn_mode);
            windowBar.Controls.Add(btn_global);
            windowBar.Controls.Add(btn_setting);
            windowBar.Cursor = Cursors.Hand;
            windowBar.Dock = DockStyle.Top;
            windowBar.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            windowBar.ForeColor = Color.Black;
            windowBar.Icon = Properties.Resources.logo_lamtool_v3_dark_16;
            windowBar.Location = new Point(0, 0);
            windowBar.Name = "windowBar";
            windowBar.Size = new Size(360, 35);
            windowBar.TabIndex = 119;
            windowBar.Text = "Cấu hình tương tác";
            // 
            // btn_mode
            // 
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Image = Properties.Resources.icons8_circle_16_Green;
            btn_mode.Location = new Point(280, 0);
            btn_mode.Name = "btn_mode";
            btn_mode.Size = new Size(26, 35);
            btn_mode.TabIndex = 6;
            // 
            // btn_global
            // 
            btn_global.Dock = DockStyle.Right;
            btn_global.Image = Properties.Resources.icons8_circle_16_Yellow;
            btn_global.Location = new Point(306, 0);
            btn_global.Name = "btn_global";
            btn_global.Size = new Size(24, 35);
            btn_global.TabIndex = 7;
            // 
            // btn_setting
            // 
            btn_setting.Dock = DockStyle.Right;
            btn_setting.Image = Properties.Resources.icons8_circle_16_Red;
            btn_setting.Location = new Point(330, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Size = new Size(30, 35);
            btn_setting.TabIndex = 8;
            // 
            // txtTenHanhDong
            // 
            txtTenHanhDong.Location = new Point(132, 49);
            txtTenHanhDong.Name = "txtTenHanhDong";
            txtTenHanhDong.Size = new Size(194, 23);
            txtTenHanhDong.TabIndex = 0;
            // 
            // A4A8DE3C
            // 
            A4A8DE3C.AutoSize = true;
            A4A8DE3C.Location = new Point(27, 52);
            A4A8DE3C.Name = "A4A8DE3C";
            A4A8DE3C.Size = new Size(98, 16);
            A4A8DE3C.TabIndex = 31;
            A4A8DE3C.Text = "Tên hành động:";
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom;
            btnCancel.BackColor = Color.Maroon;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Tahoma", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(189, 98);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(92, 29);
            btnCancel.TabIndex = 10;
            btnCancel.Text = "Đóng";
            btnCancel.UseVisualStyleBackColor = false;
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Bottom;
            btnSave.BackColor = Color.FromArgb(53, 120, 229);
            btnSave.Cursor = Cursors.Hand;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Font = new Font("Tahoma", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(82, 98);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(92, 29);
            btnSave.TabIndex = 9;
            btnSave.Text = "Thêm";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // fHDDangXuatThietBiCu
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(362, 142);
            Controls.Add(panel1);
            Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "fHDDangXuatThietBiCu";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Cấu hình tương tác";
            D33B07B4.ResumeLayout(false);
            ((ISupportInitialize)pictureBox1).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            windowBar.ResumeLayout(false);
            ResumeLayout(false);
        }
        internal Panel panel1;

        internal TextBox txtTenHanhDong;

        internal Label A4A8DE3C;

        internal Button btnCancel;

        internal Button btnSave;

        internal Panel D33B07B4;

        internal Button button1;

        internal PictureBox pictureBox1;
        #endregion

        private Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel windowBar;
        private Button btn_mode;
        private Button btn_global;
        private Button btn_setting;
    }
}