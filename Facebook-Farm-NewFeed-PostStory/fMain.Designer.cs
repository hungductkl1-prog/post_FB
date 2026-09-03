using Facebook_Farm_NewFeed_PostStory.Views.Controls;
using Sunny.Subd.Core.Models;
using Sunny.Subdy.UI.View.Pages;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;
using Font = System.Drawing.Font;

namespace Facebook_Farm_NewFeed_PostStory
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
            windowBar = new Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel();
            label1 = new Label();
            toolStrip1 = new ToolStrip();
            uiLabel6 = new ToolStripLabel();
            toolStripLabel2 = new ToolStripLabel();
            uiLabel5 = new ToolStripLabel();
            toolStripLabel4 = new ToolStripLabel();
            toolStripLabel5 = new ToolStripLabel();
            btn_mode = new Button();
            btn_global = new Button();
            btn_setting = new Button();
            panel1 = new System.Windows.Forms.Panel();
            pMenu = new System.Windows.Forms.Panel();
            pContent = new System.Windows.Forms.Panel();
            windowBar.SuspendLayout();
            toolStrip1.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // windowBar
            // 
            windowBar.BackColor = Color.White;
            windowBar.Controls.Add(label1);
            windowBar.Controls.Add(toolStrip1);
            windowBar.Controls.Add(btn_mode);
            windowBar.Controls.Add(btn_global);
            windowBar.Controls.Add(btn_setting);
            windowBar.Cursor = Cursors.Default;
            windowBar.Dock = DockStyle.Top;
            windowBar.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            windowBar.ForeColor = Color.Black;
            windowBar.Icon = Properties.Resources.logo_lamtool_v3_dark_16;
            windowBar.Location = new Point(0, 0);
            windowBar.Name = "windowBar";
            windowBar.Size = new Size(1300, 35);
            windowBar.TabIndex = 7;
            windowBar.Text = "QN Phone Farm";
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
            btn_mode.Location = new Point(1192, 0);
            btn_mode.Name = "btn_mode";
            btn_mode.Size = new Size(36, 35);
            btn_mode.TabIndex = 6;
            btn_mode.Click += btn_mode_Click;
            // 
            // btn_global
            // 
            btn_global.Dock = DockStyle.Right;
            btn_global.Location = new Point(1228, 0);
            btn_global.Name = "btn_global";
            btn_global.Size = new Size(36, 35);
            btn_global.TabIndex = 7;
            btn_global.Click += btn_global_SelectedValueChanged;
            // 
            // btn_setting
            // 
            btn_setting.Dock = DockStyle.Right;
            btn_setting.Location = new Point(1264, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Size = new Size(36, 35);
            btn_setting.TabIndex = 8;
            btn_setting.Click += btn_setting_Click;
            // 
            // panel1
            // 
            panel1.BackColor = Color.White;
            panel1.Controls.Add(pMenu);
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
            pMenu.Location = new Point(0, 0);
            pMenu.Name = "pMenu";
            pMenu.Size = new Size(281, 710);
            pMenu.TabIndex = 10;
            //
            // pContent
            // 
            pContent.AutoScroll = true;
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
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ForeColor = Color.Black;
            FormBorderStyle = FormBorderStyle.None;
            KeyPreview = true;
            MinimumSize = new Size(1280, 720);
            Name = "fMain";
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterScreen;
            windowBar.ResumeLayout(false);
            windowBar.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel windowBar;
        private Button btn_mode;
        private Button btn_setting;
        private Button btn_global;
        private ToolStrip toolStrip1;
        private ToolStripLabel uiLabel6;
        private ToolStripLabel toolStripLabel2;
        private ToolStripLabel uiLabel5;
        private ToolStripLabel toolStripLabel4;
        private ToolStripLabel toolStripLabel5;

        private ucdgvAccount _ucFacebook;
        private ucdgvAccount _ucPandora;
        public ucManagerDevices _ucDevices;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel pMenu;
        private Label label1;
        public System.Windows.Forms.Panel pContent;
    }
}