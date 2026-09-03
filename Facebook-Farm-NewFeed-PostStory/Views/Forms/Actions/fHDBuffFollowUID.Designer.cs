using Facebook_Farm_NewFeed_PostStory.Properties;
using System.ComponentModel;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions
{
    partial class fHDBuffFollowUID
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
            pnlHeader = new Panel();
            AE920B11 = new Button();
            pictureBox1 = new PictureBox();
            BEA5D185 = new Panel();
            windowBar = new Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel();
            btn_mode = new Button();
            btn_global = new Button();
            btn_setting = new Button();
            C8B7F627 = new TextBox();
            FF1F8332 = new Label();
            txtTenHanhDong = new TextBox();
            label1 = new Label();
            btnCancel = new Button();
            btnSave = new Button();
            pnlHeader.SuspendLayout();
            ((ISupportInitialize)pictureBox1).BeginInit();
            BEA5D185.SuspendLayout();
            windowBar.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlHeader.BackColor = Color.White;
            pnlHeader.Controls.Add(AE920B11);
            pnlHeader.Controls.Add(pictureBox1);
            pnlHeader.Cursor = Cursors.SizeAll;
            pnlHeader.Location = new Point(0, 3);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(343, 31);
            pnlHeader.TabIndex = 9;
            // 
            // AE920B11
            // 
            AE920B11.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            AE920B11.Cursor = Cursors.Hand;
            AE920B11.FlatAppearance.BorderSize = 0;
            AE920B11.FlatStyle = FlatStyle.Flat;
            AE920B11.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            AE920B11.ForeColor = Color.White;
            AE920B11.Location = new Point(310, 0);
            AE920B11.Name = "AE920B11";
            AE920B11.Size = new Size(32, 32);
            AE920B11.TabIndex = 78;
            AE920B11.TextImageRelation = TextImageRelation.ImageBeforeText;
            AE920B11.UseVisualStyleBackColor = true;
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
            // BEA5D185
            // 
            BEA5D185.BackColor = Color.White;
            BEA5D185.BorderStyle = BorderStyle.FixedSingle;
            BEA5D185.Controls.Add(windowBar);
            BEA5D185.Controls.Add(C8B7F627);
            BEA5D185.Controls.Add(FF1F8332);
            BEA5D185.Controls.Add(txtTenHanhDong);
            BEA5D185.Controls.Add(label1);
            BEA5D185.Controls.Add(btnCancel);
            BEA5D185.Controls.Add(btnSave);
            BEA5D185.Dock = DockStyle.Fill;
            BEA5D185.Location = new Point(0, 0);
            BEA5D185.Name = "BEA5D185";
            BEA5D185.Size = new Size(346, 159);
            BEA5D185.TabIndex = 0;
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
            windowBar.Icon = Resources.logo_lamtool_v3_dark_16;
            windowBar.Location = new Point(0, 0);
            windowBar.Name = "windowBar";
            windowBar.Size = new Size(344, 35);
            windowBar.TabIndex = 130;
            windowBar.Text = "Cấu hình tương tác";
            // 
            // btn_mode
            // 
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Image = Resources.icons8_circle_16_Green;
            btn_mode.Location = new Point(264, 0);
            btn_mode.Name = "btn_mode";
            btn_mode.Size = new Size(26, 35);
            btn_mode.TabIndex = 6;
            // 
            // btn_global
            // 
            btn_global.Dock = DockStyle.Right;
            btn_global.Image = Resources.icons8_circle_16_Yellow;
            btn_global.Location = new Point(290, 0);
            btn_global.Name = "btn_global";
            btn_global.Size = new Size(24, 35);
            btn_global.TabIndex = 7;
            // 
            // btn_setting
            // 
            btn_setting.Dock = DockStyle.Right;
            btn_setting.Image = Resources.icons8_circle_16_Red;
            btn_setting.Location = new Point(314, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Size = new Size(30, 35);
            btn_setting.TabIndex = 8;
            // 
            // C8B7F627
            // 
            C8B7F627.Location = new Point(132, 78);
            C8B7F627.Name = "C8B7F627";
            C8B7F627.Size = new Size(188, 23);
            C8B7F627.TabIndex = 129;
            // 
            // FF1F8332
            // 
            FF1F8332.AutoSize = true;
            FF1F8332.Location = new Point(21, 81);
            FF1F8332.Name = "FF1F8332";
            FF1F8332.Size = new Size(30, 16);
            FF1F8332.TabIndex = 126;
            FF1F8332.Text = "Uid:";
            // 
            // txtTenHanhDong
            // 
            txtTenHanhDong.Location = new Point(132, 49);
            txtTenHanhDong.Name = "txtTenHanhDong";
            txtTenHanhDong.Size = new Size(188, 23);
            txtTenHanhDong.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(21, 52);
            label1.Name = "label1";
            label1.Size = new Size(98, 16);
            label1.TabIndex = 31;
            label1.Text = "Tên hành động:";
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
            btnCancel.Location = new Point(177, 117);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(92, 29);
            btnCancel.TabIndex = 12;
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
            btnSave.Location = new Point(79, 117);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(92, 29);
            btnSave.TabIndex = 11;
            btnSave.Text = "Thêm";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // fHDBuffFollowUID
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(346, 159);
            Controls.Add(BEA5D185);
            Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "fHDBuffFollowUID";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Cấu hình tương tác";
            pnlHeader.ResumeLayout(false);
            ((ISupportInitialize)pictureBox1).EndInit();
            BEA5D185.ResumeLayout(false);
            BEA5D185.PerformLayout();
            windowBar.ResumeLayout(false);
            ResumeLayout(false);
        }

        internal Panel BEA5D185;

        internal TextBox txtTenHanhDong;

        internal Label label1;

        internal Button btnCancel;

        internal Button btnSave;

        internal Panel pnlHeader;

        internal PictureBox pictureBox1;

        internal Button AE920B11;

        internal Label FF1F8332;

        internal TextBox C8B7F627;
        #endregion

        private Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel windowBar;
        private Button btn_mode;
        private Button btn_global;
        private Button btn_setting;
    }
}