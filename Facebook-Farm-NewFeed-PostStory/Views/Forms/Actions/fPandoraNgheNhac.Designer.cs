namespace Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions
{
    partial class fPandoraNgheNhac
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
            btnSave = new AntdUI.Button();
            btnCancel = new AntdUI.Button();
            txtTenHanhDong = new AntdUI.Input();
            label1 = new Label();
            label2 = new Label();
            txtPandoraUrl = new TextBox();
            label3 = new Label();
            label4 = new Label();
            nudListenMinutesFrom = new NumericUpDown();
            label5 = new Label();
            nudListenMinutesTo = new NumericUpDown();
            label6 = new Label();
            groupBox1 = new GroupBox();
            groupBox2 = new GroupBox();
            ((System.ComponentModel.ISupportInitialize)nudListenMinutesFrom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudListenMinutesTo).BeginInit();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();

            //
            // btnSave
            //
            btnSave.Location = new Point(497, 320);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(100, 36);
            btnSave.TabIndex = 3;
            btnSave.Text = "Lưu";
            btnSave.Type = AntdUI.TTypeMini.Primary;
            //
            // btnCancel
            //
            btnCancel.Location = new Point(607, 320);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(100, 36);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Hủy";
            //
            // txtTenHanhDong
            //
            txtTenHanhDong.Location = new Point(142, 20);
            txtTenHanhDong.Name = "txtTenHanhDong";
            txtTenHanhDong.Size = new Size(550, 36);
            txtTenHanhDong.TabIndex = 0;
            //
            // label1
            //
            label1.AutoSize = true;
            label1.Location = new Point(20, 28);
            label1.Name = "label1";
            label1.Size = new Size(116, 20);
            label1.TabIndex = 0;
            label1.Text = "Tên hành động:";
            //
            // label2
            //
            label2.AutoSize = true;
            label2.Location = new Point(20, 70);
            label2.Name = "label2";
            label2.Size = new Size(150, 20);
            label2.TabIndex = 0;
            label2.Text = "URL Playlist/Bài hát:";
            //
            // txtPandoraUrl
            //
            txtPandoraUrl.Location = new Point(20, 96);
            txtPandoraUrl.Name = "txtPandoraUrl";
            txtPandoraUrl.Size = new Size(672, 36);
            txtPandoraUrl.TabIndex = 1;
            //
            // label3
            //
            label3.AutoSize = true;
            label3.ForeColor = Color.Gray;
            label3.Location = new Point(20, 140);
            label3.Name = "label3";
            label3.Size = new Size(350, 16);
            label3.TabIndex = 0;
            label3.Text = "Nhập URL playlist hoặc bài hát trên Pandora (vd: https://www.pandora.com/playlist/...)";
            //
            // label4
            //
            label4.AutoSize = true;
            label4.Location = new Point(20, 28);
            label4.Name = "label4";
            label4.Size = new Size(105, 20);
            label4.TabIndex = 0;
            label4.Text = "Ngẫu nhiên từ";
            //
            // nudListenMinutesFrom
            //
            nudListenMinutesFrom.Location = new Point(131, 24);
            nudListenMinutesFrom.Maximum = new decimal(new int[] { 1440, 0, 0, 0 });
            nudListenMinutesFrom.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudListenMinutesFrom.Name = "nudListenMinutesFrom";
            nudListenMinutesFrom.Size = new Size(100, 27);
            nudListenMinutesFrom.TabIndex = 0;
            nudListenMinutesFrom.Value = new decimal(new int[] { 30, 0, 0, 0 });
            //
            // label5
            //
            label5.AutoSize = true;
            label5.Location = new Point(237, 28);
            label5.Name = "label5";
            label5.Size = new Size(32, 20);
            label5.TabIndex = 0;
            label5.Text = "đến";
            //
            // nudListenMinutesTo
            //
            nudListenMinutesTo.Location = new Point(275, 24);
            nudListenMinutesTo.Maximum = new decimal(new int[] { 1440, 0, 0, 0 });
            nudListenMinutesTo.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudListenMinutesTo.Name = "nudListenMinutesTo";
            nudListenMinutesTo.Size = new Size(100, 27);
            nudListenMinutesTo.TabIndex = 1;
            nudListenMinutesTo.Value = new decimal(new int[] { 180, 0, 0, 0 });
            //
            // label6
            //
            label6.AutoSize = true;
            label6.Location = new Point(381, 28);
            label6.Name = "label6";
            label6.Size = new Size(39, 20);
            label6.TabIndex = 0;
            label6.Text = "phút";
            //
            // groupBox1
            //
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(txtTenHanhDong);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(txtPandoraUrl);
            groupBox1.Controls.Add(label3);
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(710, 180);
            groupBox1.TabIndex = 0;
            groupBox1.Text = "Cài đặt hành động";
            //
            // groupBox2
            //
            groupBox2.Controls.Add(label4);
            groupBox2.Controls.Add(nudListenMinutesFrom);
            groupBox2.Controls.Add(label5);
            groupBox2.Controls.Add(nudListenMinutesTo);
            groupBox2.Controls.Add(label6);
            groupBox2.Location = new Point(12, 200);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(710, 100);
            groupBox2.TabIndex = 1;
            groupBox2.Text = "Cài đặt thời gian";
            //
            // fPandoraNgheNhac
            //
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(734, 380);
            Controls.Add(groupBox1);
            Controls.Add(groupBox2);
            Controls.Add(btnSave);
            Controls.Add(btnCancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "fPandoraNgheNhac";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Nghe nhạc (URL)";
            ((System.ComponentModel.ISupportInitialize)nudListenMinutesFrom).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudListenMinutesTo).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private AntdUI.Button btnSave;
        private AntdUI.Button btnCancel;
        private AntdUI.Input txtTenHanhDong;
        private Label label1;
        private Label label2;
        private TextBox txtPandoraUrl;
        private Label label3;
        private Label label4;
        private NumericUpDown nudListenMinutesFrom;
        private Label label5;
        private NumericUpDown nudListenMinutesTo;
        private Label label6;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
    }
}
