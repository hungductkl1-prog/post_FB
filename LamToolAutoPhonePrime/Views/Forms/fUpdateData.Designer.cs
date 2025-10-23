using System.Drawing;
using System.Windows.Forms;

namespace LamToolAutoPhonePrime.Views.Forms
{
    partial class fUpdateData
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(fUpdateData));
            panel2 = new Panel();
            btn_Close = new Button();
            btn_Ok = new Button();
            cb_NoProxyAccount = new CheckBox();
            rdb_Random = new RadioButton();
            rdb_LanLuot = new RadioButton();
            label4 = new Label();
            nudAccount_Proxy = new NumericUpDown();
            label3 = new Label();
            cbbTypeProxy = new ComboBox();
            label2 = new Label();
            label1 = new Label();
            panel3 = new Panel();
            groupBox1 = new GroupBox();
            txtLines = new TextBox();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudAccount_Proxy).BeginInit();
            panel3.SuspendLayout();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // panel2
            // 
            panel2.Controls.Add(btn_Close);
            panel2.Controls.Add(btn_Ok);
            panel2.Controls.Add(cb_NoProxyAccount);
            panel2.Controls.Add(rdb_Random);
            panel2.Controls.Add(rdb_LanLuot);
            panel2.Controls.Add(label4);
            panel2.Controls.Add(nudAccount_Proxy);
            panel2.Controls.Add(label3);
            panel2.Controls.Add(cbbTypeProxy);
            panel2.Controls.Add(label2);
            panel2.Controls.Add(label1);
            panel2.Dock = DockStyle.Bottom;
            panel2.Location = new Point(0, 261);
            panel2.Name = "panel2";
            panel2.Padding = new Padding(7);
            panel2.Size = new Size(870, 224);
            panel2.TabIndex = 1;
            // 
            // btn_Close
            // 
            btn_Close.Anchor = AnchorStyles.Right;
            btn_Close.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btn_Close.ForeColor = Color.Red;
            btn_Close.Location = new Point(442, 180);
            btn_Close.Name = "btn_Close";
            btn_Close.Size = new Size(94, 32);
            btn_Close.TabIndex = 83;
            btn_Close.Text = "Đóng";
            btn_Close.UseVisualStyleBackColor = true;
            btn_Close.Click += btn_Close_Click;
            // 
            // btn_Ok
            // 
            btn_Ok.Anchor = AnchorStyles.Left;
            btn_Ok.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btn_Ok.ForeColor = Color.Green;
            btn_Ok.Location = new Point(319, 180);
            btn_Ok.Name = "btn_Ok";
            btn_Ok.Size = new Size(94, 32);
            btn_Ok.TabIndex = 82;
            btn_Ok.Text = "Xác nhận";
            btn_Ok.UseVisualStyleBackColor = true;
            btn_Ok.Click += btn_Ok_Click;
            // 
            // cb_NoProxyAccount
            // 
            cb_NoProxyAccount.AutoSize = true;
            cb_NoProxyAccount.Checked = true;
            cb_NoProxyAccount.CheckState = CheckState.Checked;
            cb_NoProxyAccount.Location = new Point(22, 135);
            cb_NoProxyAccount.Name = "cb_NoProxyAccount";
            cb_NoProxyAccount.Size = new Size(268, 19);
            cb_NoProxyAccount.TabIndex = 81;
            cb_NoProxyAccount.Text = "Không nhập vào những tài khoản đã có proxy";
            cb_NoProxyAccount.UseVisualStyleBackColor = true;
            // 
            // rdb_Random
            // 
            rdb_Random.AutoSize = true;
            rdb_Random.Location = new Point(234, 97);
            rdb_Random.Name = "rdb_Random";
            rdb_Random.Size = new Size(87, 19);
            rdb_Random.TabIndex = 80;
            rdb_Random.Text = "Ngẫu nhiên";
            rdb_Random.UseVisualStyleBackColor = true;
            // 
            // rdb_LanLuot
            // 
            rdb_LanLuot.AutoSize = true;
            rdb_LanLuot.Checked = true;
            rdb_LanLuot.Location = new Point(143, 97);
            rdb_LanLuot.Name = "rdb_LanLuot";
            rdb_LanLuot.Size = new Size(68, 19);
            rdb_LanLuot.TabIndex = 79;
            rdb_LanLuot.TabStop = true;
            rdb_LanLuot.Text = "Lần lượt";
            rdb_LanLuot.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(22, 99);
            label4.Name = "label4";
            label4.Size = new Size(92, 15);
            label4.TabIndex = 78;
            label4.Text = "Tùy chọn nhập :";
            // 
            // nudAccount_Proxy
            // 
            nudAccount_Proxy.Location = new Point(176, 53);
            nudAccount_Proxy.Maximum = new decimal(new int[] { 99999999, 0, 0, 0 });
            nudAccount_Proxy.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudAccount_Proxy.Name = "nudAccount_Proxy";
            nudAccount_Proxy.Size = new Size(89, 23);
            nudAccount_Proxy.TabIndex = 77;
            nudAccount_Proxy.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(22, 55);
            label3.Name = "label3";
            label3.Size = new Size(110, 15);
            label3.TabIndex = 3;
            label3.Text = "Số tài khoản/proxy:";
            // 
            // cbbTypeProxy
            // 
            cbbTypeProxy.DropDownStyle = ComboBoxStyle.DropDownList;
            cbbTypeProxy.FormattingEnabled = true;
            cbbTypeProxy.Items.AddRange(new object[] { "HTTP" });
            cbbTypeProxy.Location = new Point(176, 16);
            cbbTypeProxy.Name = "cbbTypeProxy";
            cbbTypeProxy.Size = new Size(129, 23);
            cbbTypeProxy.TabIndex = 76;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(22, 24);
            label2.Name = "label2";
            label2.Size = new Size(65, 15);
            label2.TabIndex = 1;
            label2.Text = "Loại Proxy:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Dock = DockStyle.Right;
            label1.Location = new Point(529, 7);
            label1.Name = "label1";
            label1.Size = new Size(334, 15);
            label1.TabIndex = 0;
            label1.Text = "( Mỗi proxy 1 dòng [ ip:port hoặc ip:port:uername:password] )";
            // 
            // panel3
            // 
            panel3.Controls.Add(groupBox1);
            panel3.Dock = DockStyle.Fill;
            panel3.Location = new Point(0, 0);
            panel3.Name = "panel3";
            panel3.Size = new Size(870, 261);
            panel3.TabIndex = 2;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(txtLines);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Location = new Point(0, 0);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(870, 261);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Danh sách proxy (0):";
            // 
            // txtLines
            // 
            txtLines.Dock = DockStyle.Fill;
            txtLines.Location = new Point(3, 19);
            txtLines.MaxLength = int.MaxValue;
            txtLines.Multiline = true;
            txtLines.Name = "txtLines";
            txtLines.ScrollBars = ScrollBars.Both;
            txtLines.Size = new Size(864, 239);
            txtLines.TabIndex = 6;
            txtLines.TextChanged += txtLines_TextChanged;
            // 
            // fUpdateData
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(870, 485);
            Controls.Add(panel3);
            Controls.Add(panel2);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "fUpdateData";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Nhập Proxy";
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudAccount_Proxy).EndInit();
            panel3.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private Panel panel2;
        private Panel panel3;
        private GroupBox groupBox1;
        private TextBox txtLines;
        private Label label1;
        private Label label2;
        private ComboBox cbbTypeProxy;
        private NumericUpDown nudAccount_Proxy;
        private Label label3;
        private Button btn_Close;
        private Button btn_Ok;
        private CheckBox cb_NoProxyAccount;
        private RadioButton rdb_Random;
        private RadioButton rdb_LanLuot;
        private Label label4;
    }
}