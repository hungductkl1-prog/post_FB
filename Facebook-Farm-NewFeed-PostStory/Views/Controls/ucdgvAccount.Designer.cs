using AntdUI;
using Facebook_Farm_NewFeed_PostStory.Utils;
using System.Reflection;
using System.Windows.Forms;
using Facebook_Farm_NewFeed_PostStory.Utils;

namespace Facebook_Farm_NewFeed_PostStory.Views.Controls
{
    partial class ucdgvAccount
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
            if (disposing)
            {
                _countsTimer?.Stop();
                _countsTimer?.Dispose();
                _countsTimer = null;
                try { FlushStatusesToDb(sync: true); } catch { }
                _statusFlushTimer?.Stop();
                _statusFlushTimer?.Dispose();
                _statusFlushTimer = null;
                _diagSnapshotTimer?.Stop();
                _diagSnapshotTimer?.Dispose();
                _diagSnapshotTimer = null;
                _gridMetrics?.Dispose();
                _gridMetrics = null;
                Facebook_Farm_NewFeed_PostStory.Utils.UiThreadProfiler.Stop(); // flush final operational window
                _reloadDebounceTimer?.Stop();
                _reloadDebounceTimer?.Dispose();
                _reloadDebounceTimer = null;
                _liveRepaintTimer?.Stop();
                _liveRepaintTimer?.Dispose();
                _liveRepaintTimer = null;
                _loadCts?.Cancel();
                _loadCts?.Dispose();
                _loadCts = null;
                _searchCts?.Cancel();
                _searchCts?.Dispose();
                _searchCts = null;
                _cache?.Dispose();
                _cache = null;
                _cachedNativeMenu?.Dispose();
                _cachedNativeMenu = null;
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            tableLayoutPanel1 = new TableLayoutPanel();
            panel2 = new AntdUI.Panel();
            select1 = new Select();
            button3 = new System.Windows.Forms.Button();
            button2 = new System.Windows.Forms.Button();
            button1 = new System.Windows.Forms.Button();
            label2 = new System.Windows.Forms.Label();
            panel3 = new AntdUI.Panel();
            cboFilterAccount = new AntdUI.SelectMultiple();
            button4 = new AntdUI.Button();
            button6 = new AntdUI.Button();
            button5 = new AntdUI.Button();
            panel4 = new AntdUI.Panel();
            input6 = new Input();
            button16 = new AntdUI.Button();
            button9 = new AntdUI.Button();
            button8 = new AntdUI.Button();
            button7 = new AntdUI.Button();
            panel5 = new AntdUI.Panel();
            dataGridView1 = new DoubleBufferedDataGridView();
            dataGridViewCheckBoxColumn1 = new DataGridViewCheckBoxColumn();
            dataGridViewTextBoxColumn1 = new DataGridViewTextBoxColumn();
            toolStrip1 = new ToolStrip();
            toolStripLabel3 = new ToolStripLabel();
            toolStripLabel4 = new ToolStripLabel();
            toolStripLabel5 = new ToolStripLabel();
            toolStripLabel6 = new ToolStripLabel();
            toolStripLabel1 = new ToolStripLabel();
            toolStripLabel2 = new ToolStripLabel();
            toolStripLabel15 = new ToolStripLabel();
            toolStripLabel16 = new ToolStripLabel();
            toolStripDropDownButton1 = new ToolStripDropDownButton();
            Like_toolStripMenuItem = new ToolStripMenuItem();
            Love_toolStripMenuItem = new ToolStripMenuItem();
            Care_toolStripMenuItem = new ToolStripMenuItem();
            Haha_toolStripMenuItem = new ToolStripMenuItem();
            Wow_toolStripMenuItem = new ToolStripMenuItem();
            Sad_toolStripMenuItem = new ToolStripMenuItem();
            Angry_toolStripMenuItem = new ToolStripMenuItem();
            Share_toolStripMenuItem = new ToolStripMenuItem();
            Follow_toolStripMenuItem = new ToolStripMenuItem();
            LikePage_toolStripMenuItem = new ToolStripMenuItem();
            JoinGroup_toolStripMenuItem = new ToolStripMenuItem();
            JobTotal_toolStripMenuItem = new ToolStripMenuItem();
            panel6 = new AntdUI.Panel();
            toolStrip2 = new ToolStrip();
            toolStripLabel7 = new ToolStripLabel();
            toolStripLabel8 = new ToolStripLabel();
            toolStripLabel9 = new ToolStripLabel();
            toolStripLabel10 = new ToolStripLabel();
            toolStripLabel11 = new ToolStripLabel();
            toolStripLabel12 = new ToolStripLabel();
            toolStripLabel13 = new ToolStripLabel();
            toolStripLabel14 = new ToolStripLabel();
            panel7 = new AntdUI.Panel();
            select2 = new Select();
            button17 = new System.Windows.Forms.Button();
            label3 = new System.Windows.Forms.Label();
            button13 = new AntdUI.Button();
            button14 = new AntdUI.Button();
            button15 = new AntdUI.Button();
            button10 = new AntdUI.Button();
            button11 = new AntdUI.Button();
            button12 = new AntdUI.Button();
            tableLayoutPanel1.SuspendLayout();
            panel2.SuspendLayout();
            panel3.SuspendLayout();
            panel4.SuspendLayout();
            panel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            toolStrip1.SuspendLayout();
            panel6.SuspendLayout();
            toolStrip2.SuspendLayout();
            panel7.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            tableLayoutPanel1.Controls.Add(panel2, 0, 0);
            tableLayoutPanel1.Controls.Add(panel3, 1, 0);
            tableLayoutPanel1.Dock = DockStyle.Top;
            tableLayoutPanel1.Location = new Point(24, 24);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(828, 100);
            tableLayoutPanel1.TabIndex = 1;
            // 
            // panel2
            // 
            panel2.Back = Color.White;
            panel2.BackColor = Color.Transparent;
            panel2.Controls.Add(select1);
            panel2.Controls.Add(button3);
            panel2.Controls.Add(button2);
            panel2.Controls.Add(button1);
            panel2.Controls.Add(label2);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(3, 3);
            panel2.Name = "panel2";
            panel2.padding = new Padding(5);
            panel2.Radius = 16;
            panel2.Size = new Size(449, 94);
            panel2.TabIndex = 1;
            panel2.Text = "panel2";
            // 
            // select1
            // 
            select1.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            select1.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            select1.ForeColor = Color.Black;
            select1.List = true;
            select1.LocalizationPlaceholderText = "Select.{id}";
            select1.Location = new Point(15, 37);
            select1.Name = "select1";
            select1.PlaceholderText = "";
            select1.Size = new Size(348, 30);
            select1.TabIndex = 12;
            select1.SelectedIndexChanged += select1_SelectedIndexChanged;
            // 
            // button3
            // 
            button3.Anchor = AnchorStyles.Right;
            button3.AutoSize = true;
            button3.BackColor = Color.White;
            button3.Cursor = Cursors.Hand;
            button3.FlatAppearance.BorderSize = 0;
            button3.FlatStyle = FlatStyle.Flat;
            button3.Image = Properties.Resources.icons8_add_16;
            button3.Location = new Point(363, 41);
            button3.Name = "button3";
            button3.Size = new Size(22, 23);
            button3.TabIndex = 9;
            button3.UseVisualStyleBackColor = false;
            button3.Click += button3_Click_1;
            // 
            // button2
            // 
            button2.Anchor = AnchorStyles.Right;
            button2.AutoSize = true;
            button2.BackColor = Color.White;
            button2.Cursor = Cursors.Hand;
            button2.FlatAppearance.BorderSize = 0;
            button2.FlatStyle = FlatStyle.Flat;
            button2.Image = Properties.Resources.icons8_rename_16;
            button2.Location = new Point(390, 41);
            button2.Name = "button2";
            button2.Size = new Size(22, 23);
            button2.TabIndex = 8;
            button2.UseVisualStyleBackColor = false;
            button2.Click += button2_Click;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Right;
            button1.AutoSize = true;
            button1.BackColor = Color.White;
            button1.Cursor = Cursors.Hand;
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Image = Properties.Resources.icons8_remove_16;
            button1.Location = new Point(415, 41);
            button1.Name = "button1";
            button1.Size = new Size(22, 23);
            button1.TabIndex = 7;
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click_1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 8.25F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.DarkGray;
            label2.Location = new Point(15, 21);
            label2.Name = "label2";
            label2.Size = new Size(74, 13);
            label2.TabIndex = 1;
            label2.Text = "Quản lý nhóm";
            // 
            // panel3
            // 
            panel3.Back = Color.White;
            panel3.BackColor = Color.Transparent;
            panel3.Controls.Add(button4);
            panel3.Controls.Add(button6);
            panel3.Controls.Add(button5);
            panel3.Dock = DockStyle.Fill;
            panel3.Location = new Point(458, 3);
            panel3.Name = "panel3";
            panel3.padding = new Padding(15, 10, 15, 10);
            panel3.Radius = 16;
            panel3.Size = new Size(367, 94);
            panel3.TabIndex = 2;
            panel3.Text = "panel3";
            // 
            // button4
            // 
            button4.Anchor = AnchorStyles.Left;
            button4.BorderWidth = 1F;
            button4.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button4.ForeColor = Color.FromArgb(80, 80, 80);
            button4.Ghost = true;
            button4.IconRatio = 0.85F;
            button4.IconSvg = "ToolOutlined";
            button4.Location = new Point(15, 12);
            button4.Name = "button4";
            button4.Radius = 8;
            button4.Size = new Size(230, 34);
            button4.TabIndex = 9;
            button4.Text = "Cài đặt jobs";
            button4.Click += button4_Click;
            // 
            // button6
            // 
            button6.Anchor = AnchorStyles.Left;
            button6.BorderWidth = 1F;
            button6.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button6.ForeColor = Color.FromArgb(80, 80, 80);
            button6.Ghost = true;
            button6.IconRatio = 0.85F;
            button6.IconSvg = "UnorderedListOutlined";
            button6.Location = new Point(132, 52);
            button6.Name = "button6";
            button6.Radius = 8;
            button6.Size = new Size(130, 34);
            button6.TabIndex = 11;
            button6.Text = "Tương tác";
            button6.Visible = true;
            button6.Click += button6_Click;
            // 
            // button5
            // 
            button5.Anchor = AnchorStyles.Left;
            button5.BorderWidth = 1F;
            button5.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button5.ForeColor = Color.FromArgb(80, 80, 80);
            button5.Ghost = true;
            button5.IconRatio = 0.85F;
            button5.IconSvg = "SettingOutlined";
            button5.Location = new Point(15, 52);
            button5.Name = "button5";
            button5.Radius = 8;
            button5.Size = new Size(115, 34);
            button5.TabIndex = 10;
            button5.Text = "Cài đặt chung";
            button5.Click += button5_Click;
            // 
            // panel4
            //
            panel4.Back = Color.White;
            panel4.BackColor = Color.Transparent;
            panel4.Controls.Add(input6);
            panel4.Controls.Add(button16);
            panel4.Controls.Add(button8);
            panel4.Controls.Add(button7);
            panel4.Dock = DockStyle.Top;
            panel4.Location = new Point(24, 124);
            panel4.Name = "panel4";
            panel4.padding = new Padding(5);
            panel4.Radius = 16;
            panel4.Size = new Size(828, 76);
            panel4.TabIndex = 2;
            panel4.Text = "panel4";
            // 
            // input6
            // 
            input6.AllowClear = true;
            input6.Anchor = AnchorStyles.Right;
            input6.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            input6.Location = new Point(429, 20);
            input6.Name = "input6";
            input6.PlaceholderText = "Tìm kiếm...";
            input6.PrefixSvg = "SearchOutlined";
            input6.Size = new Size(200, 40);
            input6.TabIndex = 2;
            input6.TextChanged += input6_TextChanged;
            // 
            // button16
            // 
            button16.Anchor = AnchorStyles.Right;
            button16.DefaultBack = Color.DodgerBlue;
            button16.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            button16.ForeColor = Color.White;
            button16.IconHoverSvg = "";
            button16.IconRatio = 0.9F;
            button16.IconSvg = "PlusOutlined";
            button16.Location = new Point(635, 20);
            button16.Name = "button16";
            button16.Radius = 10;
            button16.Shape = TShape.Round;
            button16.Size = new Size(158, 40);
            button16.TabIndex = 6;
            button16.Text = "Thêm tài khoản";
            button16.Type = TTypeMini.Info;
            button16.Click += button16_Click;
            // 
            // button9
            // 
            button9.Anchor = AnchorStyles.Right;
            button9.DefaultBack = Color.DodgerBlue;
            button9.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button9.ForeColor = Color.White;
            button9.IconHoverSvg = "";
            button9.IconRatio = 0.9F;
            button9.IconSvg = "ReloadOutlined";
            button9.Location = new Point(222, 38);
            button9.Name = "button9";
            button9.Radius = 10;
            button9.Shape = TShape.Round;
            button9.Size = new Size(90, 30);
            button9.TabIndex = 5;
            button9.Text = "Tải lại";
            button9.Type = TTypeMini.Primary;
            button9.Click += button9_Click;
            // 
            // button8
            // 
            button8.Anchor = AnchorStyles.Left;
            button8.DefaultBack = Color.FromArgb(220, 53, 69);
            button8.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            button8.ForeColor = Color.White;
            button8.IconHoverSvg = "";
            button8.IconSvg = "PauseCircleOutlined";
            button8.Location = new Point(15, 20);
            button8.Name = "button8";
            button8.Radius = 10;
            button8.Shape = TShape.Round;
            button8.Size = new Size(110, 40);
            button8.TabIndex = 4;
            button8.Text = "Dừng";
            button8.Type = TTypeMini.Error;
            button8.Visible = false;
            button8.Click += button8_Click;
            // 
            // button7
            // 
            button7.Anchor = AnchorStyles.Left;
            button7.DefaultBack = Color.FromArgb(40, 167, 69);
            button7.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button7.ForeColor = Color.White;
            button7.IconHoverSvg = "";
            button7.IconRatio = 1F;
            button7.IconSvg = "PlayCircleOutlined";
            button7.Location = new Point(15, 20);
            button7.Name = "button7";
            button7.Radius = 10;
            button7.Shape = TShape.Round;
            button7.Size = new Size(110, 40);
            button7.TabIndex = 3;
            button7.Text = "Chạy";
            button7.Type = TTypeMini.Success;
            button7.Click += button7_Click;
            // 
            // panel5
            // 
            panel5.Back = Color.White;
            panel5.BackColor = Color.Transparent;
            panel5.Controls.Add(dataGridView1);
            panel5.Controls.Add(toolStrip1);
            panel5.Controls.Add(panel6);
            panel5.Controls.Add(button10);
            panel5.Controls.Add(button11);
            panel5.Controls.Add(button12);
            panel5.Dock = DockStyle.Fill;
            panel5.Location = new Point(24, 200);
            panel5.Name = "panel5";
            panel5.padding = new Padding(5);
            panel5.Padding = new Padding(14, 10, 14, 10);
            panel5.Radius = 16;
            panel5.Size = new Size(828, 336);
            panel5.TabIndex = 3;
            panel5.Text = "panel5";
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(250, 250, 250);
            dataGridView1.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.BackgroundColor = Color.White;
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridView1.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            dataGridView1.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = Color.WhiteSmoke;
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dataGridViewCellStyle2.ForeColor = Color.DimGray;
            dataGridViewCellStyle2.SelectionBackColor = Color.WhiteSmoke;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
            dataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { dataGridViewCheckBoxColumn1, dataGridViewTextBoxColumn1 });
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.EditMode = DataGridViewEditMode.EditOnEnter;
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.GridColor = Color.FromArgb(230, 230, 230);
            dataGridView1.Location = new Point(14, 86);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RightToLeft = RightToLeft.No;
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = Color.White;
            dataGridViewCellStyle5.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle5.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle5.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dataGridViewCellStyle5.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = DataGridViewTriState.True;
            dataGridView1.RowHeadersDefaultCellStyle = dataGridViewCellStyle5;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.RowTemplate.DefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
            dataGridView1.RowTemplate.Height = 36;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.Size = new Size(800, 215);
            dataGridView1.TabIndex = 12;
            // 
            // dataGridViewCheckBoxColumn1
            // 
            dataGridViewCheckBoxColumn1.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dataGridViewCheckBoxColumn1.DataPropertyName = "Checked";
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            dataGridViewCellStyle3.NullValue = false;
            dataGridViewCheckBoxColumn1.DefaultCellStyle = dataGridViewCellStyle3;
            dataGridViewCheckBoxColumn1.FillWeight = 43.8844223F;
            dataGridViewCheckBoxColumn1.FlatStyle = FlatStyle.System;
            dataGridViewCheckBoxColumn1.HeaderText = "Chọn";
            dataGridViewCheckBoxColumn1.MinimumWidth = 60;
            dataGridViewCheckBoxColumn1.Name = "dataGridViewCheckBoxColumn1";
            dataGridViewCheckBoxColumn1.ToolTipText = "Chọn tài khoản để chạy chức năng";
            dataGridViewCheckBoxColumn1.Width = 60;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dataGridViewCellStyle4.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            dataGridViewTextBoxColumn1.DefaultCellStyle = dataGridViewCellStyle4;
            dataGridViewTextBoxColumn1.FillWeight = 60F;
            dataGridViewTextBoxColumn1.HeaderText = "#";
            dataGridViewTextBoxColumn1.MinimumWidth = 60;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 60;
            // 
            // toolStrip1
            // 
            toolStrip1.BackColor = Color.White;
            toolStrip1.Dock = DockStyle.Bottom;
            toolStrip1.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            toolStrip1.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip1.Items.AddRange(new ToolStripItem[] { toolStripLabel3, toolStripLabel4, toolStripLabel5, toolStripLabel6, toolStripLabel1, toolStripLabel2, toolStripLabel15, toolStripLabel16, toolStripDropDownButton1 });
            toolStrip1.Location = new Point(14, 301);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.RenderMode = ToolStripRenderMode.System;
            toolStrip1.Size = new Size(800, 25);
            toolStrip1.TabIndex = 11;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel3
            // 
            toolStripLabel3.ForeColor = Color.DarkGray;
            toolStripLabel3.Name = "toolStripLabel3";
            toolStripLabel3.Size = new Size(49, 22);
            toolStripLabel3.Text = "Bôi đen:";
            // 
            // toolStripLabel4
            // 
            toolStripLabel4.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            toolStripLabel4.ForeColor = Color.Black;
            toolStripLabel4.Name = "toolStripLabel4";
            toolStripLabel4.Size = new Size(14, 22);
            toolStripLabel4.Text = "0";
            // 
            // toolStripLabel5
            // 
            toolStripLabel5.ForeColor = Color.DarkGray;
            toolStripLabel5.Name = "toolStripLabel5";
            toolStripLabel5.Size = new Size(54, 22);
            toolStripLabel5.Text = "Đã chọn:";
            // 
            // toolStripLabel6
            // 
            toolStripLabel6.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            toolStripLabel6.ForeColor = Color.Green;
            toolStripLabel6.Name = "toolStripLabel6";
            toolStripLabel6.Size = new Size(14, 22);
            toolStripLabel6.Text = "0";
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.ForeColor = Color.DarkGray;
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new Size(54, 22);
            toolStripLabel1.Text = "Đã chạy:";
            // 
            // toolStripLabel2
            // 
            toolStripLabel2.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            toolStripLabel2.ForeColor = Color.MediumBlue;
            toolStripLabel2.Name = "toolStripLabel2";
            toolStripLabel2.Size = new Size(14, 22);
            toolStripLabel2.Text = "0";
            // 
            // toolStripLabel15
            // 
            toolStripLabel15.ForeColor = Color.DarkGray;
            toolStripLabel15.Name = "toolStripLabel15";
            toolStripLabel15.Size = new Size(58, 22);
            toolStripLabel15.Text = "Hôm nay:";
            // 
            // toolStripLabel16
            // 
            toolStripLabel16.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            toolStripLabel16.ForeColor = Color.Orange;
            toolStripLabel16.Name = "toolStripLabel16";
            toolStripLabel16.Size = new Size(14, 22);
            toolStripLabel16.Text = "0";
            // 
            // toolStripDropDownButton1
            // 
            toolStripDropDownButton1.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripDropDownButton1.DropDownItems.AddRange(new ToolStripItem[] { Like_toolStripMenuItem, Love_toolStripMenuItem, Care_toolStripMenuItem, Haha_toolStripMenuItem, Wow_toolStripMenuItem, Sad_toolStripMenuItem, Angry_toolStripMenuItem, Share_toolStripMenuItem, Follow_toolStripMenuItem, LikePage_toolStripMenuItem, JoinGroup_toolStripMenuItem, JobTotal_toolStripMenuItem });
            toolStripDropDownButton1.Image = Properties.Resources.bar_chart_50dp_F19E39_FILL0_wght400_GRAD0_opsz48;
            toolStripDropDownButton1.ImageTransparentColor = Color.Magenta;
            toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            toolStripDropDownButton1.Size = new Size(29, 22);
            toolStripDropDownButton1.Text = "toolStripDropDownButton1";
            toolStripDropDownButton1.ToolTipText = "Thống kê tất cả số job đã làm";
            // 
            // Like_toolStripMenuItem
            // 
            Like_toolStripMenuItem.BackColor = Color.White;
            Like_toolStripMenuItem.Image = Properties.Resources.facebook_reactions_1;
            Like_toolStripMenuItem.Name = "Like_toolStripMenuItem";
            Like_toolStripMenuItem.Size = new Size(152, 22);
            Like_toolStripMenuItem.Text = "Like: 0/0";
            // 
            // Love_toolStripMenuItem
            // 
            Love_toolStripMenuItem.BackColor = Color.White;
            Love_toolStripMenuItem.Image = Properties.Resources.thumbs_up;
            Love_toolStripMenuItem.Name = "Love_toolStripMenuItem";
            Love_toolStripMenuItem.Size = new Size(152, 22);
            Love_toolStripMenuItem.Text = "Love: 0/0";
            // 
            // Care_toolStripMenuItem
            // 
            Care_toolStripMenuItem.BackColor = Color.White;
            Care_toolStripMenuItem.Image = Properties.Resources.facebook;
            Care_toolStripMenuItem.Name = "Care_toolStripMenuItem";
            Care_toolStripMenuItem.Size = new Size(152, 22);
            Care_toolStripMenuItem.Text = "Care: 0/0";
            // 
            // Haha_toolStripMenuItem
            // 
            Haha_toolStripMenuItem.BackColor = Color.White;
            Haha_toolStripMenuItem.Image = Properties.Resources.facebook_reactions_2;
            Haha_toolStripMenuItem.Name = "Haha_toolStripMenuItem";
            Haha_toolStripMenuItem.Size = new Size(152, 22);
            Haha_toolStripMenuItem.Text = "Haha: 0/0";
            // 
            // Wow_toolStripMenuItem
            // 
            Wow_toolStripMenuItem.BackColor = Color.White;
            Wow_toolStripMenuItem.Image = Properties.Resources.facebook_reactions_3;
            Wow_toolStripMenuItem.Name = "Wow_toolStripMenuItem";
            Wow_toolStripMenuItem.Size = new Size(152, 22);
            Wow_toolStripMenuItem.Text = "Wow: 0/0";
            // 
            // Sad_toolStripMenuItem
            // 
            Sad_toolStripMenuItem.BackColor = Color.White;
            Sad_toolStripMenuItem.Image = Properties.Resources.facebook_reactions_4;
            Sad_toolStripMenuItem.Name = "Sad_toolStripMenuItem";
            Sad_toolStripMenuItem.Size = new Size(152, 22);
            Sad_toolStripMenuItem.Text = "Sad: 0/0";
            // 
            // Angry_toolStripMenuItem
            // 
            Angry_toolStripMenuItem.BackColor = Color.White;
            Angry_toolStripMenuItem.Image = Properties.Resources.facebook_reactions_5;
            Angry_toolStripMenuItem.Name = "Angry_toolStripMenuItem";
            Angry_toolStripMenuItem.Size = new Size(152, 22);
            Angry_toolStripMenuItem.Text = "Angry: 0/0";
            // 
            // Share_toolStripMenuItem
            // 
            Share_toolStripMenuItem.BackColor = Color.White;
            Share_toolStripMenuItem.Image = Properties.Resources.icons8_share_40;
            Share_toolStripMenuItem.Name = "Share_toolStripMenuItem";
            Share_toolStripMenuItem.Size = new Size(152, 22);
            Share_toolStripMenuItem.Text = "Share: 0/0";
            // 
            // Follow_toolStripMenuItem
            // 
            Follow_toolStripMenuItem.BackColor = Color.White;
            Follow_toolStripMenuItem.Image = Properties.Resources.icons8_follow_40;
            Follow_toolStripMenuItem.Name = "Follow_toolStripMenuItem";
            Follow_toolStripMenuItem.Size = new Size(152, 22);
            Follow_toolStripMenuItem.Text = "Follow: 0/0";
            // 
            // LikePage_toolStripMenuItem
            // 
            LikePage_toolStripMenuItem.BackColor = Color.White;
            LikePage_toolStripMenuItem.Image = Properties.Resources.icons8_facebook_like_48;
            LikePage_toolStripMenuItem.Name = "LikePage_toolStripMenuItem";
            LikePage_toolStripMenuItem.Size = new Size(152, 22);
            LikePage_toolStripMenuItem.Text = "Like Page: 0/0";
            // 
            // JoinGroup_toolStripMenuItem
            // 
            JoinGroup_toolStripMenuItem.BackColor = Color.White;
            JoinGroup_toolStripMenuItem.Image = Properties.Resources.groups_50dp_5985E1_FILL0_wght400_GRAD0_opsz48;
            JoinGroup_toolStripMenuItem.Name = "JoinGroup_toolStripMenuItem";
            JoinGroup_toolStripMenuItem.Size = new Size(152, 22);
            JoinGroup_toolStripMenuItem.Text = "Join Group: 0/0";
            // 
            // JobTotal_toolStripMenuItem
            // 
            JobTotal_toolStripMenuItem.BackColor = Color.White;
            JobTotal_toolStripMenuItem.Image = Properties.Resources.monitoring_54dp_314D1C_FILL0_wght400_GRAD0_opsz48;
            JobTotal_toolStripMenuItem.Name = "JobTotal_toolStripMenuItem";
            JobTotal_toolStripMenuItem.Size = new Size(152, 22);
            JobTotal_toolStripMenuItem.Text = "Job Total: 0/0";
            // 
            // panel6
            // 
            panel6.Back = Color.White;
            panel6.BackColor = Color.Transparent;
            panel6.Controls.Add(toolStrip2);
            panel6.Controls.Add(panel7);
            panel6.Controls.Add(label3);
            panel6.Controls.Add(button13);
            panel6.Controls.Add(button14);
            panel6.Controls.Add(button15);
            panel6.Dock = DockStyle.Top;
            panel6.Location = new Point(14, 10);
            panel6.Name = "panel6";
            panel6.padding = new Padding(5);
            panel6.Padding = new Padding(10, 0, 10, 0);
            panel6.Radius = 16;
            panel6.Size = new Size(800, 76);
            panel6.TabIndex = 6;
            panel6.Text = "panel6";
            // 
            // toolStrip2
            // 
            toolStrip2.BackColor = Color.White;
            toolStrip2.Dock = DockStyle.Bottom;
            toolStrip2.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            toolStrip2.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip2.Items.AddRange(new ToolStripItem[] { toolStripLabel7, toolStripLabel8, toolStripLabel9, toolStripLabel10, toolStripLabel11, toolStripLabel12, toolStripLabel13, toolStripLabel14 });
            toolStrip2.Location = new Point(10, 51);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.RenderMode = ToolStripRenderMode.System;
            toolStrip2.Size = new Size(439, 25);
            toolStrip2.TabIndex = 10;
            toolStrip2.Text = "toolStrip2";
            // 
            // toolStripLabel7
            // 
            toolStripLabel7.ForeColor = Color.DarkGray;
            toolStripLabel7.Image = Properties.Resources.icons8_circle_7_Blue;
            toolStripLabel7.ImageScaling = ToolStripItemImageScaling.None;
            toolStripLabel7.Name = "toolStripLabel7";
            toolStripLabel7.Size = new Size(49, 22);
            toolStripLabel7.Text = "Tất cả:";
            // 
            // toolStripLabel8
            // 
            toolStripLabel8.ForeColor = Color.Blue;
            toolStripLabel8.Name = "toolStripLabel8";
            toolStripLabel8.Size = new Size(13, 22);
            toolStripLabel8.Text = "0";
            // 
            // toolStripLabel9
            // 
            toolStripLabel9.ForeColor = Color.DarkGray;
            toolStripLabel9.Image = Properties.Resources.icons8_circle_7_Green;
            toolStripLabel9.ImageAlign = ContentAlignment.MiddleRight;
            toolStripLabel9.ImageScaling = ToolStripItemImageScaling.None;
            toolStripLabel9.Name = "toolStripLabel9";
            toolStripLabel9.Padding = new Padding(10, 0, 0, 0);
            toolStripLabel9.Size = new Size(50, 22);
            toolStripLabel9.Text = "LIVE:";
            toolStripLabel9.TextAlign = ContentAlignment.MiddleRight;
            // 
            // toolStripLabel10
            // 
            toolStripLabel10.ForeColor = Color.Green;
            toolStripLabel10.Name = "toolStripLabel10";
            toolStripLabel10.Size = new Size(13, 22);
            toolStripLabel10.Text = "0";
            // 
            // toolStripLabel11
            // 
            toolStripLabel11.ForeColor = Color.DarkGray;
            toolStripLabel11.Image = Properties.Resources.icons8_circle_7_Red;
            toolStripLabel11.ImageAlign = ContentAlignment.MiddleRight;
            toolStripLabel11.ImageScaling = ToolStripItemImageScaling.None;
            toolStripLabel11.Name = "toolStripLabel11";
            toolStripLabel11.Padding = new Padding(10, 0, 0, 0);
            toolStripLabel11.Size = new Size(45, 22);
            toolStripLabel11.Text = "DIE:";
            toolStripLabel11.TextAlign = ContentAlignment.MiddleRight;
            // 
            // toolStripLabel12
            // 
            toolStripLabel12.ForeColor = Color.Red;
            toolStripLabel12.Name = "toolStripLabel12";
            toolStripLabel12.Size = new Size(13, 22);
            toolStripLabel12.Text = "0";
            // 
            // toolStripLabel13
            // 
            toolStripLabel13.ForeColor = Color.DarkGray;
            toolStripLabel13.Image = Properties.Resources.icons8_circle_7_Orange;
            toolStripLabel13.ImageAlign = ContentAlignment.MiddleRight;
            toolStripLabel13.ImageScaling = ToolStripItemImageScaling.None;
            toolStripLabel13.Name = "toolStripLabel13";
            toolStripLabel13.Padding = new Padding(10, 0, 0, 0);
            toolStripLabel13.Size = new Size(117, 22);
            toolStripLabel13.Text = "Trường hợp khác:";
            toolStripLabel13.TextAlign = ContentAlignment.MiddleRight;
            // 
            // toolStripLabel14
            // 
            toolStripLabel14.ForeColor = Color.Orange;
            toolStripLabel14.Name = "toolStripLabel14";
            toolStripLabel14.Size = new Size(13, 22);
            toolStripLabel14.Text = "0";
            // 
            // panel7
            // 
            panel7.Controls.Add(cboFilterAccount);
            panel7.Controls.Add(button9);
            panel7.Controls.Add(button17);
            panel7.Dock = DockStyle.Right;
            panel7.Location = new Point(449, 0);
            panel7.Name = "panel7";
            panel7.padding = new Padding(10);
            panel7.Size = new Size(341, 76);
            panel7.TabIndex = 7;
            panel7.Text = "panel7";
            //
            // cboFilterAccount
            //
            cboFilterAccount.Anchor = AnchorStyles.Right;
            cboFilterAccount.CheckMode = true;
            cboFilterAccount.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            cboFilterAccount.Location = new Point(10, 38);
            cboFilterAccount.Name = "cboFilterAccount";
            cboFilterAccount.PlaceholderText = "Lọc tài khoản";
            cboFilterAccount.Size = new Size(150, 30);
            cboFilterAccount.TabIndex = 13;
            //
            // button9 (Tải lại)
            //
            button9.Anchor = AnchorStyles.Right;
            button9.Location = new Point(166, 38);
            button9.Size = new Size(100, 30);
            //
            // button17 (Hiển thị)
            //
            button17.Anchor = AnchorStyles.Right;
            button17.AutoSize = true;
            button17.BackColor = Color.White;
            button17.Cursor = Cursors.Hand;
            button17.FlatAppearance.BorderSize = 0;
            button17.FlatStyle = FlatStyle.Flat;
            button17.Image = Properties.Resources.icons8_eye_161;
            button17.Location = new Point(316, 42);
            button17.Name = "button17";
            button17.Size = new Size(22, 23);
            button17.TabIndex = 10;
            button17.UseVisualStyleBackColor = false;
            button17.Click += button17_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(10, 17);
            label3.Name = "label3";
            label3.Size = new Size(132, 17);
            label3.TabIndex = 6;
            label3.Text = "Danh sách tài khoản";
            // 
            // button13
            // 
            button13.Anchor = AnchorStyles.Right;
            button13.DefaultBack = Color.DodgerBlue;
            button13.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button13.ForeColor = Color.White;
            button13.IconHoverSvg = "";
            button13.IconRatio = 0.9F;
            button13.IconSvg = "RedoOutlined";
            button13.Location = new Point(1453, 57);
            button13.Name = "button13";
            button13.Radius = 10;
            button13.Size = new Size(116, 40);
            button13.TabIndex = 5;
            button13.Text = "Làm mới";
            // 
            // button14
            // 
            button14.Anchor = AnchorStyles.Right;
            button14.DefaultBack = Color.Red;
            button14.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button14.ForeColor = Color.White;
            button14.IconHoverSvg = "";
            button14.IconSvg = "XFilled";
            button14.Location = new Point(1345, 57);
            button14.Name = "button14";
            button14.Radius = 10;
            button14.Size = new Size(102, 40);
            button14.TabIndex = 4;
            button14.Text = "Dừng";
            // 
            // button15
            // 
            button15.Anchor = AnchorStyles.Right;
            button15.DefaultBack = Color.Green;
            button15.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button15.ForeColor = Color.White;
            button15.IconHoverSvg = "";
            button15.IconRatio = 1F;
            button15.IconSvg = "CaretRightFilled";
            button15.Location = new Point(1240, 57);
            button15.Name = "button15";
            button15.Radius = 10;
            button15.Size = new Size(102, 40);
            button15.TabIndex = 3;
            button15.Text = "Bắt đầu";
            // 
            // button10
            // 
            button10.Anchor = AnchorStyles.Right;
            button10.DefaultBack = Color.DodgerBlue;
            button10.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button10.ForeColor = Color.White;
            button10.IconHoverSvg = "";
            button10.IconRatio = 0.9F;
            button10.IconSvg = "RedoOutlined";
            button10.Location = new Point(1477, 187);
            button10.Name = "button10";
            button10.Radius = 10;
            button10.Size = new Size(116, 40);
            button10.TabIndex = 5;
            button10.Text = "Làm mới";
            // 
            // button11
            // 
            button11.Anchor = AnchorStyles.Right;
            button11.DefaultBack = Color.Red;
            button11.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button11.ForeColor = Color.White;
            button11.IconHoverSvg = "";
            button11.IconSvg = "XFilled";
            button11.Location = new Point(1369, 187);
            button11.Name = "button11";
            button11.Radius = 10;
            button11.Size = new Size(102, 40);
            button11.TabIndex = 4;
            button11.Text = "Dừng";
            // 
            // button12
            // 
            button12.Anchor = AnchorStyles.Right;
            button12.DefaultBack = Color.Green;
            button12.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button12.ForeColor = Color.White;
            button12.IconHoverSvg = "";
            button12.IconRatio = 1F;
            button12.IconSvg = "CaretRightFilled";
            button12.Location = new Point(1264, 187);
            button12.Name = "button12";
            button12.Radius = 10;
            button12.Size = new Size(102, 40);
            button12.TabIndex = 3;
            button12.Text = "Bắt đầu";
            // 
            // ucdgvAccount
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(236, 240, 241);
            Controls.Add(panel5);
            Controls.Add(panel4);
            Controls.Add(tableLayoutPanel1);
            Name = "ucdgvAccount";
            Padding = new Padding(24);
            Size = new Size(876, 560);
            Load += ucdgvAccount_Load;
            tableLayoutPanel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            panel3.ResumeLayout(false);
            panel4.ResumeLayout(false);
            panel5.ResumeLayout(false);
            panel5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            panel6.ResumeLayout(false);
            panel6.PerformLayout();
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            panel7.ResumeLayout(false);
            panel7.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private TableLayoutPanel tableLayoutPanel1;
        private AntdUI.Panel panel3;
        private AntdUI.Panel panel2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label2;
        private AntdUI.Button button4;
        private AntdUI.Button button5;
        private AntdUI.Button button6;
        private AntdUI.Panel panel4;
        private AntdUI.Button button7;
        private AntdUI.Button button8;
        private AntdUI.Button button9;
        private AntdUI.Panel panel5;
        private AntdUI.Panel panel6;
        private System.Windows.Forms.Label label3;
        private AntdUI.Button button13;
        private AntdUI.Button button14;
        private AntdUI.Button button15;
        private AntdUI.Button button10;
        private AntdUI.Button button11;
        private AntdUI.Button button12;
        private ToolStrip toolStrip2;
        private ToolStripLabel toolStripLabel7;
        private ToolStripLabel toolStripLabel8;
        private ToolStripLabel toolStripLabel9;
        private ToolStripLabel toolStripLabel10;
        private ToolStripLabel toolStripLabel11;
        private ToolStripLabel toolStripLabel12;
        private AntdUI.Panel panel7;
        private AntdUI.Button button16;
        private AntdUI.Input input6;
        private ToolStrip toolStrip1;
        private ToolStripLabel toolStripLabel3;
        private ToolStripLabel toolStripLabel4;
        private ToolStripLabel toolStripLabel5;
        private ToolStripLabel toolStripLabel6;
        public DoubleBufferedDataGridView dataGridView1;
        private ToolStripLabel toolStripLabel13;
        private ToolStripLabel toolStripLabel14;
        private System.Windows.Forms.Button button17;
        private AntdUI.Select select1;
        private AntdUI.Select select2;
        private AntdUI.SelectMultiple cboFilterAccount;
        private ToolStripLabel toolStripLabel1;
        private ToolStripLabel toolStripLabel2;
        private ToolStripLabel toolStripLabel15;
        private ToolStripLabel toolStripLabel16;
        private ToolStripDropDownButton toolStripDropDownButton1;
        private ToolStripMenuItem Like_toolStripMenuItem;
        private ToolStripMenuItem Love_toolStripMenuItem;
        private ToolStripMenuItem Care_toolStripMenuItem;
        private ToolStripMenuItem Haha_toolStripMenuItem;
        private ToolStripMenuItem Wow_toolStripMenuItem;
        private ToolStripMenuItem Sad_toolStripMenuItem;
        private ToolStripMenuItem Angry_toolStripMenuItem;
        private ToolStripMenuItem Share_toolStripMenuItem;
        private ToolStripMenuItem Follow_toolStripMenuItem;
        private ToolStripMenuItem LikePage_toolStripMenuItem;
        private ToolStripMenuItem JoinGroup_toolStripMenuItem;
        private ToolStripMenuItem JobTotal_toolStripMenuItem;
        private DataGridViewCheckBoxColumn dataGridViewCheckBoxColumn1;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
    }
}
