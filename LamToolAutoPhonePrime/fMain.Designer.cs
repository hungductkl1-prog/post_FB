using AntdUI;
using LamToolAutoPhonePrime.Views.Controls;
using Sunny.Subd.Core.Models;
using Sunny.Subdy.UI.View.Pages;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;
using Font = System.Drawing.Font;

namespace LamToolAutoPhonePrime
{
    partial class fMain
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
            windowBar = new PageHeader();
            label1 = new AntdUI.Label();
            toolStrip1 = new ToolStrip();
            uiLabel6 = new ToolStripLabel();
            toolStripLabel2 = new ToolStripLabel();
            uiLabel5 = new ToolStripLabel();
            toolStripLabel4 = new ToolStripLabel();
            toolStripLabel5 = new ToolStripLabel();
            btn_mode = new AntdUI.Button();
            btn_global = new AntdUI.Button();
            btn_setting = new AntdUI.Button();
            panel1 = new System.Windows.Forms.Panel();
            pMenu = new System.Windows.Forms.Panel();
            panel2 = new System.Windows.Forms.Panel();
            panel4 = new AntdUI.Panel();
            button9 = new AntdUI.Button();
            label9 = new System.Windows.Forms.Label();
            label8 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            pictureBox2 = new PictureBox();
            pContent = new System.Windows.Forms.Panel();
            windowBar.SuspendLayout();
            toolStrip1.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            panel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            SuspendLayout();
            // 
            // windowBar
            // 
            windowBar.BackColor = Color.White;
            windowBar.BackgroundImageLayout = ImageLayout.Stretch;
            windowBar.CloseSize = 30;
            windowBar.Controls.Add(label1);
            windowBar.Controls.Add(toolStrip1);
            windowBar.Controls.Add(btn_mode);
            windowBar.Controls.Add(btn_global);
            windowBar.Controls.Add(btn_setting);
            windowBar.Cursor = Cursors.Hand;
            windowBar.DividerMargin = 1;
            windowBar.Dock = DockStyle.Top;
            windowBar.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            windowBar.ForeColor = Color.Black;
            windowBar.Icon = Properties.Resources.logo_lamtool_v3_dark_16;
            windowBar.Location = new Point(0, 0);
            windowBar.MDI = true;
            windowBar.Name = "windowBar";
            windowBar.ShowIcon = true;
            windowBar.Size = new Size(1300, 35);
            windowBar.SubFont = new Font("Microsoft Sans Serif", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            windowBar.SubGap = 1;
            windowBar.SubText = "v18.12.08.2025";
            windowBar.TabIndex = 7;
            windowBar.Text = "Golike Phone Farm";
            windowBar.UseSystemStyleColor = true;
            windowBar.UseTextBold = false;
            // 
            // label1
            // 
            label1.Dock = DockStyle.Fill;
            label1.Location = new Point(217, 0);
            label1.Name = "label1";
            label1.Padding = new Padding(20, 0, 0, 0);
            label1.Size = new Size(779, 35);
            label1.TabIndex = 9;
            label1.Text = "Quản lý thiết bị";
            // 
            // toolStrip1
            // 
            toolStrip1.BackColor = Color.White;
            toolStrip1.Dock = DockStyle.Right;
            toolStrip1.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            toolStrip1.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip1.Items.AddRange(new ToolStripItem[] { uiLabel6, toolStripLabel2, uiLabel5, toolStripLabel4, toolStripLabel5 });
            toolStrip1.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            toolStrip1.Location = new Point(996, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Padding = new Padding(0, 0, 5, 0);
            toolStrip1.RenderMode = ToolStripRenderMode.System;
            toolStrip1.Size = new Size(196, 35);
            toolStrip1.TabIndex = 8;
            toolStrip1.Text = "toolStrip1";
            // 
            // uiLabel6
            // 
            uiLabel6.Alignment = ToolStripItemAlignment.Right;
            uiLabel6.ForeColor = Color.DarkGreen;
            uiLabel6.Name = "uiLabel6";
            uiLabel6.Padding = new Padding(0, 0, 0, 15);
            uiLabel6.Size = new Size(16, 32);
            uiLabel6.Text = "...";
            // 
            // toolStripLabel2
            // 
            toolStripLabel2.Alignment = ToolStripItemAlignment.Right;
            toolStripLabel2.ForeColor = Color.DimGray;
            toolStripLabel2.Name = "toolStripLabel2";
            toolStripLabel2.Padding = new Padding(10, 0, 0, 0);
            toolStripLabel2.Size = new Size(45, 32);
            toolStripLabel2.Text = "RAM:";
            // 
            // uiLabel5
            // 
            uiLabel5.Alignment = ToolStripItemAlignment.Right;
            uiLabel5.ForeColor = Color.FromArgb(0, 0, 192);
            uiLabel5.Name = "uiLabel5";
            uiLabel5.Padding = new Padding(10, 0, 0, 0);
            uiLabel5.Size = new Size(26, 32);
            uiLabel5.Text = "...";
            // 
            // toolStripLabel4
            // 
            toolStripLabel4.Alignment = ToolStripItemAlignment.Right;
            toolStripLabel4.ForeColor = Color.DimGray;
            toolStripLabel4.Name = "toolStripLabel4";
            toolStripLabel4.Padding = new Padding(10, 0, 0, 0);
            toolStripLabel4.Size = new Size(43, 32);
            toolStripLabel4.Text = "CPU:";
            // 
            // toolStripLabel5
            // 
            toolStripLabel5.Alignment = ToolStripItemAlignment.Right;
            toolStripLabel5.ForeColor = Color.DarkOrange;
            toolStripLabel5.Name = "toolStripLabel5";
            toolStripLabel5.Padding = new Padding(10, 0, 0, 0);
            toolStripLabel5.Size = new Size(59, 32);
            toolStripLabel5.Text = "00:00:00";
            // 
            // btn_mode
            // 
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Ghost = true;
            btn_mode.IconSvg = "MinusOutlined";
            btn_mode.Location = new Point(1192, 0);
            btn_mode.Name = "btn_mode";
            btn_mode.Radius = 0;
            btn_mode.Size = new Size(36, 35);
            btn_mode.TabIndex = 6;
            btn_mode.ToggleIconSvg = "";
            btn_mode.WaveSize = 0;
            btn_mode.Click += btn_mode_Click;
            // 
            // btn_global
            // 
            btn_global.Dock = DockStyle.Right;
            btn_global.Ghost = true;
            btn_global.IconSvg = "ExpandOutlined";
            btn_global.Location = new Point(1228, 0);
            btn_global.Name = "btn_global";
            btn_global.Radius = 0;
            btn_global.Size = new Size(36, 35);
            btn_global.TabIndex = 7;
            btn_global.WaveSize = 0;
            btn_global.Click += btn_global_SelectedValueChanged;
            // 
            // btn_setting
            // 
            btn_setting.Dock = DockStyle.Right;
            btn_setting.Ghost = true;
            btn_setting.IconSvg = "CloseOutlined";
            btn_setting.Location = new Point(1264, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(36, 35);
            btn_setting.TabIndex = 8;
            btn_setting.WaveSize = 0;
            btn_setting.Click += btn_setting_Click;
            // 
            // panel1
            // 
            panel1.BackColor = Color.White;
            panel1.Controls.Add(pMenu);
            panel1.Controls.Add(panel2);
            panel1.Dock = DockStyle.Left;
            panel1.ForeColor = Color.Transparent;
            panel1.Location = new Point(0, 35);
            panel1.Margin = new Padding(0);
            panel1.Name = "panel1";
            panel1.Size = new Size(281, 710);
            panel1.TabIndex = 6;
            // 
            // pMenu
            // 
            pMenu.BackColor = Color.White;
            pMenu.Dock = DockStyle.Fill;
            pMenu.Location = new Point(0, 142);
            pMenu.Name = "pMenu";
            pMenu.Size = new Size(281, 568);
            pMenu.TabIndex = 10;
            // 
            // panel2
            // 
            panel2.BackColor = Color.White;
            panel2.Controls.Add(panel4);
            panel2.Dock = DockStyle.Top;
            panel2.Location = new Point(0, 0);
            panel2.Name = "panel2";
            panel2.Padding = new Padding(10);
            panel2.Size = new Size(281, 142);
            panel2.TabIndex = 7;
            // 
            // panel4
            // 
            panel4.Back = Color.Orange;
            panel4.BackColor = Color.White;
            panel4.BackExtend = "";
            panel4.Controls.Add(button9);
            panel4.Controls.Add(label9);
            panel4.Controls.Add(label8);
            panel4.Controls.Add(label7);
            panel4.Controls.Add(label6);
            panel4.Controls.Add(label5);
            panel4.Controls.Add(label4);
            panel4.Controls.Add(pictureBox2);
            panel4.Dock = DockStyle.Fill;
            panel4.ForeColor = Color.White;
            panel4.Location = new Point(10, 10);
            panel4.Name = "panel4";
            panel4.Radius = 16;
            panel4.Size = new Size(261, 122);
            panel4.TabIndex = 4;
            panel4.Text = "panel4";
            // 
            // button9
            // 
            button9.Anchor = AnchorStyles.Right;
            button9.DefaultBack = Color.Orange;
            button9.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            button9.ForeColor = Color.White;
            button9.IconHoverSvg = "";
            button9.IconRatio = 0.9F;
            button9.IconSvg = "LogoutOutlined";
            button9.Location = new Point(224, 14);
            button9.Name = "button9";
            button9.Radius = 10;
            button9.Size = new Size(33, 33);
            button9.TabIndex = 6;
            button9.Click += button9_Click;
            // 
            // label9
            // 
            label9.Anchor = AnchorStyles.None;
            label9.BackColor = Color.Transparent;
            label9.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label9.ForeColor = Color.White;
            label9.Location = new Point(63, 90);
            label9.Name = "label9";
            label9.Size = new Size(173, 16);
            label9.TabIndex = 17;
            label9.Text = "15/03/2024";
            label9.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label8
            // 
            label8.Anchor = AnchorStyles.Right;
            label8.BackColor = Color.Transparent;
            label8.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label8.ForeColor = Color.White;
            label8.Location = new Point(63, 63);
            label8.Name = "label8";
            label8.Size = new Size(173, 15);
            label8.TabIndex = 16;
            label8.Text = "1.000.000 xu";
            label8.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.BackColor = Color.Transparent;
            label7.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label7.ForeColor = Color.White;
            label7.Location = new Point(17, 92);
            label7.Name = "label7";
            label7.Size = new Size(37, 13);
            label7.TabIndex = 15;
            label7.Text = "Email:";
            label7.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.BackColor = Color.Transparent;
            label6.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label6.ForeColor = Color.White;
            label6.Location = new Point(17, 69);
            label6.Name = "label6";
            label6.Size = new Size(40, 13);
            label6.TabIndex = 14;
            label6.Text = "Số dư:";
            label6.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.BackColor = Color.Transparent;
            label5.Font = new Font("Segoe UI", 8.25F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label5.ForeColor = Color.White;
            label5.Location = new Point(62, 39);
            label5.Name = "label5";
            label5.Size = new Size(83, 13);
            label5.TabIndex = 11;
            label5.Text = "Tài khoản chính";
            label5.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.Left;
            label4.AutoSize = true;
            label4.BackColor = Color.Transparent;
            label4.Font = new Font("Segoe UI", 12.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label4.ForeColor = Color.White;
            label4.Location = new Point(61, 15);
            label4.Name = "label4";
            label4.Size = new Size(91, 23);
            label4.TabIndex = 10;
            label4.Text = "Golike.net";
            // 
            // pictureBox2
            // 
            pictureBox2.BackColor = Color.Transparent;
            pictureBox2.Image = Properties.Resources.icons8_profile_40;
            pictureBox2.Location = new Point(15, 14);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(40, 40);
            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox2.TabIndex = 1;
            pictureBox2.TabStop = false;
            // 
            // pContent
            // 
            pContent.BackColor = Color.FromArgb(236, 240, 241);
            pContent.Dock = DockStyle.Fill;
            pContent.Location = new Point(281, 35);
            pContent.Name = "pContent";
            pContent.Size = new Size(1019, 710);
            pContent.TabIndex = 8;
            // 
            // fMain
            // 
            BackColor = Color.FromArgb(245, 247, 250);
            ClientSize = new Size(1500, 800);
            Controls.Add(pContent);
            Controls.Add(panel1);
            Controls.Add(windowBar);
            Dark = true;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ForeColor = Color.Black;
            FormBorderStyle = FormBorderStyle.None;
            MinimumSize = new Size(1500, 750);
            Mode = TAMode.Dark;
            Name = "fMain";
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterScreen;
            windowBar.ResumeLayout(false);
            windowBar.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel4.ResumeLayout(false);
            panel4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private AntdUI.PageHeader windowBar;
        private AntdUI.Button btn_mode;
        private AntdUI.Button btn_setting;
        private AntdUI.Button btn_global;
        private ToolStrip toolStrip1;
        private ToolStripLabel uiLabel6;
        private ToolStripLabel toolStripLabel2;
        private ToolStripLabel uiLabel5;
        private ToolStripLabel toolStripLabel4;
        private ToolStripLabel toolStripLabel5;

        private ucdgvAccount _ucFacebook;
        private ucdgvAccount _ucInstagram;
        private ucdgvAccount _ucThreads;
        private ucHistoriesJob _ucHistoriesJob;
        public ucManagerDevices _ucDevices;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel pMenu;
        private AntdUI.Panel panel4;
        private PictureBox pictureBox2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private AntdUI.Label label1;
        private AntdUI.Button button9;
        public System.Windows.Forms.Panel pContent;
    }
}