using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using LamToolAutoPhonePrime.Utils;

namespace Sunny.Subdy.UI.View.Pages
{
    partial class ucManagerDevices
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private bool isDraggingPanel = false;
        private int panel1OriginalWidth;
        private Point lastMousePosition;
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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            button2 = new AntdUI.Button();
            button53 = new AntdUI.Button();
            panel6 = new AntdUI.Panel();
            cboFilter = new AntdUI.Select();
            button1 = new AntdUI.Button();
            input1 = new AntdUI.Input();
            panel1 = new AntdUI.Panel();
            dataGridView1 = new DoubleBufferedDataGridView();
            dataGridViewCheckBoxColumn1 = new DataGridViewCheckBoxColumn();
            dataGridViewTextBoxColumn1 = new DataGridViewTextBoxColumn();
            panel5 = new AntdUI.Panel();
            toolStrip2 = new ToolStrip();
            toolStripLabel7 = new ToolStripLabel();
            toolStripLabel8 = new ToolStripLabel();
            toolStripLabelOnline = new ToolStripLabel();
            toolStripLabelOnlineCount = new ToolStripLabel();
            toolStripLabel9 = new ToolStripLabel();
            toolStripLabel10 = new ToolStripLabel();
            toolStripLabel11 = new ToolStripLabel();
            toolStripLabel12 = new ToolStripLabel();
            panelRight = new AntdUI.Panel();
            splitContainer1 = new SplitContainer();
            panel6.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            panel5.SuspendLayout();
            toolStrip2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // button2
            // 
            button2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button2.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button2.IconRatio = 1.2F;
            button2.IconSvg = "CaretRightFilled";
            button2.IconToggleAnimation = 400;
            button2.Location = new Point(914, 22);
            button2.Name = "button2";
            button2.Shape = AntdUI.TShape.Round;
            button2.Size = new Size(137, 39);
            button2.TabIndex = 12;
            button2.Text = "Bắt đầu";
            button2.Type = AntdUI.TTypeMini.Success;
            button2.Visible = false;
            // 
            // button53
            // 
            button53.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button53.IconSvg = "ReloadOutlined";
            button53.Location = new Point(267, 22);
            button53.Name = "button53";
            button53.Shape = AntdUI.TShape.Round;
            button53.Size = new Size(137, 39);
            button53.TabIndex = 11;
            button53.Text = "Tải thiết bị";
            button53.Type = AntdUI.TTypeMini.Success;
            // 
            // panel6
            // 
            panel6.BackColor = Color.Transparent;
            panel6.Controls.Add(button2);
            panel6.Controls.Add(cboFilter);
            panel6.Controls.Add(button1);
            panel6.Controls.Add(input1);
            panel6.Controls.Add(button53);
            panel6.Dock = DockStyle.Top;
            panel6.Location = new Point(24, 24);
            panel6.Margin = new Padding(10, 3, 3, 3);
            panel6.Name = "panel6";
            panel6.padding = new Padding(10);
            panel6.Padding = new Padding(10);
            panel6.Radius = 16;
            panel6.Size = new Size(1082, 88);
            panel6.TabIndex = 9;
            panel6.Text = "panel6";
            // 
            // cboFilter
            // 
            cboFilter.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            cboFilter.Location = new Point(560, 22);
            cboFilter.Name = "cboFilter";
            cboFilter.PlaceholderText = "Lọc thiết bị";
            cboFilter.Size = new Size(180, 40);
            cboFilter.TabIndex = 14;
            // 
            // button1
            // 
            button1.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button1.IconSvg = "StopOutlined";
            button1.Location = new Point(410, 23);
            button1.Name = "button1";
            button1.Shape = AntdUI.TShape.Round;
            button1.Size = new Size(137, 39);
            button1.TabIndex = 13;
            button1.Text = "Dừng ADB";
            button1.Type = AntdUI.TTypeMini.Error;
            // 
            // input1
            // 
            input1.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            input1.LocalizationPlaceholderText = "Overview.{id}";
            input1.Location = new Point(20, 22);
            input1.Name = "input1";
            input1.Padding = new Padding(0, 2, 0, 2);
            input1.PlaceholderText = "Tìm kiếm";
            input1.PrefixSvg = "SearchOutlined";
            input1.Size = new Size(241, 40);
            input1.TabIndex = 11;
            // 
            // panel1
            // 
            panel1.Controls.Add(dataGridView1);
            panel1.Controls.Add(panel5);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Margin = new Padding(10, 3, 3, 3);
            panel1.Name = "panel1";
            panel1.padding = new Padding(10);
            panel1.Padding = new Padding(20, 30, 20, 10);
            panel1.Radius = 12;
            panel1.Size = new Size(978, 519);
            panel1.TabIndex = 10;
            panel1.Text = "panel1";
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
            dataGridView1.Location = new Point(20, 30);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RightToLeft = RightToLeft.No;
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = Color.White;
            dataGridViewCellStyle3.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle3.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dataGridViewCellStyle3.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.True;
            dataGridView1.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.RowTemplate.DefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
            dataGridView1.RowTemplate.Height = 36;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.Size = new Size(938, 442);
            dataGridView1.TabIndex = 12;
            // 
            // dataGridViewCheckBoxColumn1
            // 
            dataGridViewCheckBoxColumn1.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dataGridViewCheckBoxColumn1.DataPropertyName = "Checked";
            dataGridViewCheckBoxColumn1.HeaderText = "Chọn";
            dataGridViewCheckBoxColumn1.MinimumWidth = 60;
            dataGridViewCheckBoxColumn1.Name = "dataGridViewCheckBoxColumn1";
            dataGridViewCheckBoxColumn1.Width = 60;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dataGridViewTextBoxColumn1.HeaderText = "#";
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 60;
            // 
            // panel5
            // 
            panel5.BackColor = Color.Transparent;
            panel5.Controls.Add(toolStrip2);
            panel5.Dock = DockStyle.Bottom;
            panel5.Location = new Point(20, 472);
            panel5.Margin = new Padding(10, 3, 3, 3);
            panel5.Name = "panel5";
            panel5.padding = new Padding(10);
            panel5.Padding = new Padding(10);
            panel5.Radius = 12;
            panel5.Size = new Size(938, 37);
            panel5.TabIndex = 9;
            panel5.Text = "panel5";
            // 
            // toolStrip2
            // 
            toolStrip2.BackColor = Color.White;
            toolStrip2.Dock = DockStyle.Bottom;
            toolStrip2.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip2.Items.AddRange(new ToolStripItem[] { toolStripLabel7, toolStripLabel8, toolStripLabelOnline, toolStripLabelOnlineCount, toolStripLabel9, toolStripLabel10, toolStripLabel11, toolStripLabel12 });
            toolStrip2.Location = new Point(10, 2);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new Size(918, 25);
            toolStrip2.TabIndex = 9;
            toolStrip2.Text = "toolStrip2";
            // 
            // toolStripLabel7
            // 
            toolStripLabel7.Name = "toolStripLabel7";
            toolStripLabel7.Size = new Size(41, 22);
            toolStripLabel7.Text = "Tất cả:";
            // 
            // toolStripLabel8
            // 
            toolStripLabel8.ForeColor = Color.Blue;
            toolStripLabel8.Name = "toolStripLabel8";
            toolStripLabel8.Size = new Size(13, 22);
            toolStripLabel8.Text = "0";
            // 
            // toolStripLabelOnline
            // 
            toolStripLabelOnline.Name = "toolStripLabelOnline";
            toolStripLabelOnline.Size = new Size(47, 22);
            toolStripLabelOnline.Text = "Kết nối:";
            // 
            // toolStripLabelOnlineCount
            // 
            toolStripLabelOnlineCount.ForeColor = Color.Green;
            toolStripLabelOnlineCount.Name = "toolStripLabelOnlineCount";
            toolStripLabelOnlineCount.Size = new Size(13, 22);
            toolStripLabelOnlineCount.Text = "0";
            // 
            // toolStripLabel9
            // 
            toolStripLabel9.Name = "toolStripLabel9";
            toolStripLabel9.Size = new Size(54, 22);
            toolStripLabel9.Text = "Đã chọn:";
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
            toolStripLabel11.Name = "toolStripLabel11";
            toolStripLabel11.Size = new Size(50, 22);
            toolStripLabel11.Text = "Bôi đen:";
            // 
            // toolStripLabel12
            // 
            toolStripLabel12.ForeColor = Color.Green;
            toolStripLabel12.Name = "toolStripLabel12";
            toolStripLabel12.Size = new Size(13, 22);
            toolStripLabel12.Text = "0";
            // 
            // panelRight
            // 
            panelRight.BackColor = Color.Transparent;
            panelRight.Dock = DockStyle.Fill;
            panelRight.Location = new Point(0, 0);
            panelRight.Name = "panelRight";
            panelRight.padding = new Padding(10);
            panelRight.Padding = new Padding(10);
            panelRight.Radius = 12;
            panelRight.Size = new Size(98, 519);
            panelRight.TabIndex = 16;
            panelRight.Text = "panelRight";
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.FixedPanel = FixedPanel.Panel2;
            splitContainer1.Location = new Point(24, 112);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(panel1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(panelRight);
            splitContainer1.Size = new Size(1082, 519);
            splitContainer1.SplitterDistance = 978;
            splitContainer1.SplitterWidth = 6;
            splitContainer1.TabIndex = 15;
            // 
            // ucManagerDevices
            // 
            BackColor = Color.FromArgb(236, 240, 241);
            Controls.Add(splitContainer1);
            Controls.Add(panel6);
            Margin = new Padding(5, 3, 3, 3);
            Name = "ucManagerDevices";
            Padding = new Padding(24);
            Size = new Size(1130, 655);
            panel6.ResumeLayout(false);
            panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            panel5.ResumeLayout(false);
            panel5.PerformLayout();
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
        }

        /// <summary>
        /// Helper to enable double buffering on DataGridView to reduce flicker and make scrolling smooth.
        /// Uses reflection to set the protected DoubleBuffered property.
        /// </summary>
        /// <param name="dgv">Target DataGridView</param>
        // MakeDoubleBuffered không còn cần thiết — DoubleBufferedDataGridView đã bật sẵn qua subclass
        [DynamicDependency("DoubleBuffered", typeof(DataGridView))]
        private static void MakeDoubleBuffered(DataGridView dgv)
        {
            // DoubleBufferedDataGridView tự bật DoubleBuffered trong constructor.
            // Method này giữ lại cho tương thích nếu có nơi nào khác gọi.
            if (dgv is DoubleBufferedDataGridView) return;
            try
            {
                var prop = typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                prop?.SetValue(dgv, true, null);
            }
            catch { }
        }

        #endregion
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private AntdUI.Panel panel6;
        private AntdUI.Button button1;
        private AntdUI.Input input1;
        private AntdUI.Panel panel1;
        public DoubleBufferedDataGridView dataGridView1;
        private AntdUI.Panel panel5;
        private ToolStrip toolStrip2;
        private ToolStripLabel toolStripLabel7;
        private ToolStripLabel toolStripLabel8;
        private ToolStripLabel toolStripLabelOnline;
        private ToolStripLabel toolStripLabelOnlineCount;
        private ToolStripLabel toolStripLabel9;
        private ToolStripLabel toolStripLabel10;
        private ToolStripLabel toolStripLabel11;
        private ToolStripLabel toolStripLabel12;
        public AntdUI.Button button2;
        private DataGridViewCheckBoxColumn dataGridViewCheckBoxColumn1;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private AntdUI.Button button53;
        private AntdUI.Select cboFilter;
        private AntdUI.Panel panelRight;
        public SplitContainer splitContainer1;
    }
}
