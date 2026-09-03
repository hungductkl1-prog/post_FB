using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions
{
    partial class fIGActionStub
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
            panel1 = new Panel();
            windowBar = new Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel();
            btn_mode = new Button();
            btn_global = new Button();
            btn_setting = new Button();
            nudDelayTo = new NumericUpDown();
            nudSoLuongTo = new NumericUpDown();
            nudDelayFrom = new NumericUpDown();
            nudSoLuongFrom = new NumericUpDown();
            txtTenHanhDong = new TextBox();
            label7 = new Label();
            label3 = new Label();
            label5 = new Label();
            label2 = new Label();
            label1 = new Label();
            btnCancel = new Button();
            btnSave = new Button();
            panel1.SuspendLayout();
            windowBar.SuspendLayout();
            ((ISupportInitialize)nudDelayTo).BeginInit();
            ((ISupportInitialize)nudSoLuongTo).BeginInit();
            ((ISupportInitialize)nudDelayFrom).BeginInit();
            ((ISupportInitialize)nudSoLuongFrom).BeginInit();
            SuspendLayout();
            //
            // panel1
            //
            panel1.BackColor = Color.White;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(windowBar);
            panel1.Controls.Add(nudDelayTo);
            panel1.Controls.Add(nudSoLuongTo);
            panel1.Controls.Add(nudDelayFrom);
            panel1.Controls.Add(nudSoLuongFrom);
            panel1.Controls.Add(txtTenHanhDong);
            panel1.Controls.Add(label7);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(btnCancel);
            panel1.Controls.Add(btnSave);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(317, 213);
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
            windowBar.Icon = Facebook_Farm_NewFeed_PostStory.Properties.Resources.logo_lamtool_v3_dark_16;
            windowBar.Location = new Point(0, 0);
            windowBar.Name = "windowBar";
            windowBar.Size = new Size(315, 35);
            windowBar.TabIndex = 119;
            windowBar.Text = "Cấu hình hành động";
            //
            // btn_mode
            //
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Image = Facebook_Farm_NewFeed_PostStory.Properties.Resources.icons8_circle_16_Green;
            btn_mode.Location = new Point(235, 0);
            btn_mode.Name = "btn_mode";
            btn_mode.Size = new Size(26, 35);
            btn_mode.TabIndex = 6;
            //
            // btn_global
            //
            btn_global.Dock = DockStyle.Right;
            btn_global.Image = Facebook_Farm_NewFeed_PostStory.Properties.Resources.icons8_circle_16_Yellow;
            btn_global.Location = new Point(261, 0);
            btn_global.Name = "btn_global";
            btn_global.Size = new Size(24, 35);
            btn_global.TabIndex = 7;
            //
            // btn_setting
            //
            btn_setting.Dock = DockStyle.Right;
            btn_setting.Image = Facebook_Farm_NewFeed_PostStory.Properties.Resources.icons8_circle_16_Red;
            btn_setting.Location = new Point(285, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Size = new Size(30, 35);
            btn_setting.TabIndex = 8;
            //
            // nudDelayTo
            //
            nudDelayTo.Location = new Point(226, 107);
            nudDelayTo.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudDelayTo.Name = "nudDelayTo";
            nudDelayTo.Size = new Size(56, 23);
            nudDelayTo.TabIndex = 6;
            //
            // nudSoLuongTo
            //
            nudSoLuongTo.Location = new Point(226, 78);
            nudSoLuongTo.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudSoLuongTo.Name = "nudSoLuongTo";
            nudSoLuongTo.Size = new Size(56, 23);
            nudSoLuongTo.TabIndex = 2;
            //
            // nudDelayFrom
            //
            nudDelayFrom.Location = new Point(132, 107);
            nudDelayFrom.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudDelayFrom.Name = "nudDelayFrom";
            nudDelayFrom.Size = new Size(56, 23);
            nudDelayFrom.TabIndex = 5;
            //
            // nudSoLuongFrom
            //
            nudSoLuongFrom.Location = new Point(132, 78);
            nudSoLuongFrom.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            nudSoLuongFrom.Name = "nudSoLuongFrom";
            nudSoLuongFrom.Size = new Size(56, 23);
            nudSoLuongFrom.TabIndex = 1;
            //
            // txtTenHanhDong
            //
            txtTenHanhDong.Location = new Point(132, 49);
            txtTenHanhDong.Name = "txtTenHanhDong";
            txtTenHanhDong.Size = new Size(150, 23);
            txtTenHanhDong.TabIndex = 0;
            //
            // label7
            //
            label7.Font = new Font("Tahoma", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label7.Location = new Point(192, 109);
            label7.Name = "label7";
            label7.Size = new Size(29, 16);
            label7.TabIndex = 38;
            label7.Text = ">";
            label7.TextAlign = ContentAlignment.MiddleCenter;
            //
            // label3
            //
            label3.Font = new Font("Tahoma", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(192, 80);
            label3.Name = "label3";
            label3.Size = new Size(29, 16);
            label3.TabIndex = 37;
            label3.Text = ">";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            //
            // label5
            //
            label5.AutoSize = true;
            label5.Location = new Point(17, 109);
            label5.Name = "label5";
            label5.Size = new Size(109, 16);
            label5.TabIndex = 34;
            label5.Text = "Thời gian chờ (s):";
            //
            // label2
            //
            label2.AutoSize = true;
            label2.Location = new Point(17, 80);
            label2.Name = "label2";
            label2.Size = new Size(63, 16);
            label2.TabIndex = 32;
            label2.Text = "Số lượng:";
            //
            // label1
            //
            label1.AutoSize = true;
            label1.Location = new Point(17, 52);
            label1.Name = "label1";
            label1.Size = new Size(98, 16);
            label1.TabIndex = 31;
            label1.Text = "Tên hành động:";
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
            btnCancel.Location = new Point(167, 169);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(92, 29);
            btnCancel.TabIndex = 8;
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
            btnSave.Location = new Point(63, 169);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(92, 29);
            btnSave.TabIndex = 7;
            btnSave.Text = "Thêm";
            btnSave.UseVisualStyleBackColor = false;
            //
            // fIGActionStub
            //
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(317, 213);
            Controls.Add(panel1);
            Font = new Font("Tahoma", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "fIGActionStub";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Cấu hình hành động";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            windowBar.ResumeLayout(false);
            ((ISupportInitialize)nudDelayTo).EndInit();
            ((ISupportInitialize)nudSoLuongTo).EndInit();
            ((ISupportInitialize)nudDelayFrom).EndInit();
            ((ISupportInitialize)nudSoLuongFrom).EndInit();
            ResumeLayout(false);
        }

        #endregion

        protected internal Panel panel1;
        protected internal NumericUpDown nudDelayTo;
        protected internal NumericUpDown nudSoLuongTo;
        protected internal NumericUpDown nudDelayFrom;
        protected internal NumericUpDown nudSoLuongFrom;
        protected internal TextBox txtTenHanhDong;
        protected internal Label label7;
        protected internal Label label3;
        protected internal Label label5;
        protected internal Label label2;
        protected internal Label label1;
        protected internal Button btnCancel;
        protected internal Button btnSave;
        protected internal Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel windowBar;
        protected internal Button btn_mode;
        protected internal Button btn_global;
        protected internal Button btn_setting;
    }
}
