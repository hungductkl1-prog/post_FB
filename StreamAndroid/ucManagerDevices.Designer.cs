using System.Reflection;
using System.Windows.Forms;

namespace StreamAndroid
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
        /// Improved visual style and smoothness for DataGridView and panels.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ucManagerDevices));
            tableLayoutPanel1 = new TableLayoutPanel();
            tableLayoutPanel4 = new TableLayoutPanel();
            panel9 = new AntdUI.Panel();
            button1 = new AntdUI.Button();
            select4 = new AntdUI.Select();
            button9 = new AntdUI.Button();
            tableLayoutPanel2 = new TableLayoutPanel();
            panel4 = new AntdUI.Panel();
            panel3 = new AntdUI.Panel();
            panel2 = new AntdUI.Panel();
            panel1 = new AntdUI.Panel();
            label1 = new AntdUI.Label();
            tableLayoutPanel3 = new TableLayoutPanel();
            panel8 = new AntdUI.Panel();
            panel5 = new Panel();
            panel7 = new AntdUI.Panel();
            panel6 = new AntdUI.Panel();
            label3 = new AntdUI.Label();
            label2 = new AntdUI.Label();
            label5 = new AntdUI.Label();
            label4 = new AntdUI.Label();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel4.SuspendLayout();
            panel9.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            panel4.SuspendLayout();
            panel3.SuspendLayout();
            panel2.SuspendLayout();
            panel1.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            panel5.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(tableLayoutPanel4, 0, 0);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 0, 1);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel3, 0, 2);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(24, 24);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 69F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 471F));
            tableLayoutPanel1.Size = new Size(1082, 607);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel4
            // 
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel4.Controls.Add(panel9, 0, 0);
            tableLayoutPanel4.Dock = DockStyle.Fill;
            tableLayoutPanel4.Location = new System.Drawing.Point(3, 3);
            tableLayoutPanel4.Name = "tableLayoutPanel4";
            tableLayoutPanel4.RowCount = 1;
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.Size = new Size(1076, 63);
            tableLayoutPanel4.TabIndex = 0;
            // 
            // panel9
            // 
            panel9.Back = Color.White;
            panel9.BackColor = Color.Transparent;
            panel9.Controls.Add(button1);
            panel9.Controls.Add(select4);
            panel9.Controls.Add(button9);
            panel9.Dock = DockStyle.Fill;
            panel9.Location = new System.Drawing.Point(3, 3);
            panel9.Name = "panel9";
            panel9.padding = new Padding(5);
            panel9.Padding = new Padding(10);
            panel9.Radius = 16;
            panel9.Size = new Size(1070, 57);
            panel9.TabIndex = 5;
            panel9.Text = "panel9";
            // 
            // button1
            // 
            button1.BadgeSize = 1.5F;
            button1.BorderWidth = 1F;
            button1.DefaultBorderColor = Color.Black;
            button1.Dock = DockStyle.Right;
            button1.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button1.ForeColor = Color.Black;
            button1.IconHoverSvg = "";
            button1.IconRatio = 0.9F;
            button1.IconSvg = "MenuUnfoldOutlined";
            button1.Location = new System.Drawing.Point(572, 10);
            button1.Name = "button1";
            button1.Radius = 10;
            button1.Shape = AntdUI.TShape.Round;
            button1.Size = new Size(154, 37);
            button1.TabIndex = 13;
            button1.Text = "Ẩn điều khiển";
            // 
            // select4
            // 
            select4.Dock = DockStyle.Right;
            select4.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            select4.ForeColor = Color.Black;
            select4.List = true;
            select4.LocalizationPlaceholderText = "Select.{id}";
            select4.Location = new System.Drawing.Point(726, 10);
            select4.Name = "select4";
            select4.PlaceholderText = "";
            select4.Size = new Size(215, 37);
            select4.TabIndex = 12;
            // 
            // button9
            // 
            button9.BadgeSize = 1.5F;
            button9.BorderWidth = 1F;
            button9.DefaultBorderColor = Color.Black;
            button9.Dock = DockStyle.Right;
            button9.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button9.ForeColor = Color.Green;
            button9.IconHoverSvg = "";
            button9.IconRatio = 0.9F;
            button9.IconSvg = "PlusOutlined";
            button9.Location = new System.Drawing.Point(941, 10);
            button9.Name = "button9";
            button9.Radius = 10;
            button9.Shape = AntdUI.TShape.Round;
            button9.Size = new Size(119, 37);
            button9.TabIndex = 6;
            button9.Text = "Thêm thẻ";
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 4;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24.814127F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25.185873F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanel2.Controls.Add(panel4, 3, 0);
            tableLayoutPanel2.Controls.Add(panel3, 2, 0);
            tableLayoutPanel2.Controls.Add(panel2, 1, 0);
            tableLayoutPanel2.Controls.Add(panel1, 0, 0);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new System.Drawing.Point(3, 72);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Size = new Size(1076, 61);
            tableLayoutPanel2.TabIndex = 0;
            // 
            // panel4
            // 
            panel4.Back = Color.White;
            panel4.BackColor = Color.Transparent;
            panel4.Controls.Add(label4);
            panel4.Dock = DockStyle.Fill;
            panel4.Location = new System.Drawing.Point(810, 3);
            panel4.Name = "panel4";
            panel4.padding = new Padding(5);
            panel4.Padding = new Padding(5);
            panel4.Radius = 16;
            panel4.Size = new Size(263, 55);
            panel4.TabIndex = 4;
            panel4.Text = "panel4";
            // 
            // panel3
            // 
            panel3.Back = Color.White;
            panel3.BackColor = Color.Transparent;
            panel3.Controls.Add(label5);
            panel3.Controls.Add(label3);
            panel3.Dock = DockStyle.Fill;
            panel3.Location = new System.Drawing.Point(539, 3);
            panel3.Name = "panel3";
            panel3.padding = new Padding(5);
            panel3.Padding = new Padding(5);
            panel3.Radius = 16;
            panel3.Size = new Size(265, 55);
            panel3.TabIndex = 3;
            panel3.Text = "panel3";
            // 
            // panel2
            // 
            panel2.Back = Color.White;
            panel2.BackColor = Color.Transparent;
            panel2.Controls.Add(label2);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new System.Drawing.Point(272, 3);
            panel2.Name = "panel2";
            panel2.padding = new Padding(5);
            panel2.Padding = new Padding(5);
            panel2.Radius = 16;
            panel2.Size = new Size(261, 55);
            panel2.TabIndex = 2;
            panel2.Text = "panel2";
            // 
            // panel1
            // 
            panel1.Back = Color.White;
            panel1.BackColor = Color.Transparent;
            panel1.Controls.Add(label1);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new System.Drawing.Point(3, 3);
            panel1.Name = "panel1";
            panel1.padding = new Padding(5);
            panel1.Padding = new Padding(5);
            panel1.Radius = 16;
            panel1.Size = new Size(263, 55);
            panel1.TabIndex = 1;
            panel1.Text = "panel1";
            // 
            // label1
            // 
            label1.Dock = DockStyle.Fill;
            label1.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.Blue;
            label1.HandCursor = Cursors.Arrow;
            label1.HandDragFolder = false;
            label1.IconGap = 10;
            label1.IconRatio = 0.6F;
            label1.ImeMode = ImeMode.NoControl;
            label1.LocalizationPrefix = "";
            label1.LocalizationSuffix = "";
            label1.LocalizationText = "";
            label1.Location = new System.Drawing.Point(5, 5);
            label1.Name = "label1";
            label1.Padding = new Padding(10, 5, 10, 5);
            label1.Prefix = "";
            label1.PrefixSvg = "TabletOutlined";
            label1.RightToLeft = RightToLeft.Yes;
            label1.ShadowOpacity = 0.5F;
            label1.ShowTooltip = false;
            label1.Size = new Size(253, 45);
            label1.Suffix = "";
            label1.SuffixSvg = "";
            label1.TabIndex = 0;
            label1.Text = "Tổng thiết bị\r\n12";
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.ColumnCount = 2;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanel3.Controls.Add(panel8, 1, 0);
            tableLayoutPanel3.Controls.Add(panel5, 0, 0);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new System.Drawing.Point(3, 139);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 1;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel3.Size = new Size(1076, 465);
            tableLayoutPanel3.TabIndex = 1;
            // 
            // panel8
            // 
            panel8.Back = Color.White;
            panel8.BackColor = Color.Transparent;
            panel8.Dock = DockStyle.Fill;
            panel8.Location = new System.Drawing.Point(810, 3);
            panel8.Name = "panel8";
            panel8.padding = new Padding(5);
            panel8.Radius = 16;
            panel8.Size = new Size(263, 459);
            panel8.TabIndex = 4;
            panel8.Text = "panel8";
            // 
            // panel5
            // 
            panel5.Controls.Add(panel7);
            panel5.Controls.Add(panel6);
            panel5.Dock = DockStyle.Fill;
            panel5.Location = new System.Drawing.Point(3, 3);
            panel5.Name = "panel5";
            panel5.Size = new Size(801, 459);
            panel5.TabIndex = 0;
            // 
            // panel7
            // 
            panel7.Back = Color.White;
            panel7.BackColor = Color.Transparent;
            panel7.Dock = DockStyle.Fill;
            panel7.Location = new System.Drawing.Point(0, 76);
            panel7.Name = "panel7";
            panel7.padding = new Padding(5);
            panel7.Radius = 16;
            panel7.Size = new Size(801, 383);
            panel7.TabIndex = 3;
            panel7.Text = "panel7";
            // 
            // panel6
            // 
            panel6.Back = Color.White;
            panel6.BackColor = Color.Transparent;
            panel6.Dock = DockStyle.Top;
            panel6.Location = new System.Drawing.Point(0, 0);
            panel6.Name = "panel6";
            panel6.padding = new Padding(5);
            panel6.Radius = 16;
            panel6.Size = new Size(801, 76);
            panel6.TabIndex = 2;
            panel6.Text = "panel6";
            // 
            // label3
            // 
            label3.Dock = DockStyle.Fill;
            label3.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.ForeColor = SystemColors.HotTrack;
            label3.HandCursor = Cursors.Arrow;
            label3.HandDragFolder = false;
            label3.IconRatio = 1.2F;
            label3.ImeMode = ImeMode.NoControl;
            label3.LocalizationPrefix = "";
            label3.LocalizationSuffix = "";
            label3.LocalizationText = "";
            label3.Location = new System.Drawing.Point(5, 5);
            label3.Name = "label3";
            label3.Padding = new Padding(10, 5, 10, 5);
            label3.Prefix = "";
            label3.PrefixColor = Color.Blue;
            label3.PrefixSvg = resources.GetString("label3.PrefixSvg");
            label3.RightToLeft = RightToLeft.Yes;
            label3.ShadowOpacity = 0.5F;
            label3.ShowTooltip = false;
            label3.Size = new Size(255, 45);
            label3.Suffix = "";
            label3.SuffixSvg = "";
            label3.TabIndex = 1;
            label3.Text = "Đã chọn\r\n12";
            // 
            // label2
            // 
            label2.Dock = DockStyle.Fill;
            label2.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.Green;
            label2.HandCursor = Cursors.Arrow;
            label2.HandDragFolder = false;
            label2.IconGap = 10;
            label2.IconRatio = 0.6F;
            label2.ImeMode = ImeMode.NoControl;
            label2.LocalizationPrefix = "";
            label2.LocalizationSuffix = "";
            label2.LocalizationText = "";
            label2.Location = new System.Drawing.Point(5, 5);
            label2.Name = "label2";
            label2.Padding = new Padding(10, 5, 10, 5);
            label2.Prefix = "";
            label2.PrefixSvg = "SafetyOutlined";
            label2.RightToLeft = RightToLeft.Yes;
            label2.ShadowOpacity = 0.5F;
            label2.ShowTooltip = false;
            label2.Size = new Size(251, 45);
            label2.Suffix = "";
            label2.SuffixSvg = "";
            label2.TabIndex = 1;
            label2.Text = "Đã kết nối\r\n12";
            // 
            // label5
            // 
            label5.Dock = DockStyle.Fill;
            label5.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label5.ForeColor = Color.DarkBlue;
            label5.HandCursor = Cursors.Arrow;
            label5.HandDragFolder = false;
            label5.IconGap = 10;
            label5.IconRatio = 0.6F;
            label5.ImeMode = ImeMode.NoControl;
            label5.LocalizationPrefix = "";
            label5.LocalizationSuffix = "";
            label5.LocalizationText = "";
            label5.Location = new System.Drawing.Point(5, 5);
            label5.Name = "label5";
            label5.Padding = new Padding(10, 5, 10, 5);
            label5.Prefix = "";
            label5.PrefixSvg = "CheckSquareOutlined";
            label5.RightToLeft = RightToLeft.Yes;
            label5.ShadowOpacity = 0.5F;
            label5.ShowTooltip = false;
            label5.Size = new Size(255, 45);
            label5.Suffix = "";
            label5.SuffixSvg = "";
            label5.TabIndex = 2;
            label5.Text = "Đã chọn\r\n12";
            // 
            // label4
            // 
            label4.Dock = DockStyle.Fill;
            label4.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label4.ForeColor = Color.Red;
            label4.HandCursor = Cursors.Arrow;
            label4.HandDragFolder = false;
            label4.IconGap = 10;
            label4.IconRatio = 0.6F;
            label4.ImeMode = ImeMode.NoControl;
            label4.LocalizationPrefix = "";
            label4.LocalizationSuffix = "";
            label4.LocalizationText = "";
            label4.Location = new System.Drawing.Point(5, 5);
            label4.Name = "label4";
            label4.Padding = new Padding(10, 5, 10, 5);
            label4.Prefix = "";
            label4.PrefixSvg = "InfoCircleOutlined";
            label4.RightToLeft = RightToLeft.Yes;
            label4.ShadowOpacity = 0.5F;
            label4.ShowTooltip = false;
            label4.Size = new Size(253, 45);
            label4.Suffix = "";
            label4.SuffixSvg = "";
            label4.TabIndex = 3;
            label4.Text = "Ngắt kết nối\r\n12";
            // 
            // ucManagerDevices
            // 
            BackColor = Color.FromArgb(236, 240, 241);
            Controls.Add(tableLayoutPanel1);
            Margin = new Padding(5, 3, 3, 3);
            Name = "ucManagerDevices";
            Padding = new Padding(24);
            Size = new Size(1130, 655);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel4.ResumeLayout(false);
            panel9.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            panel4.ResumeLayout(false);
            panel3.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel1.ResumeLayout(false);
            tableLayoutPanel3.ResumeLayout(false);
            panel5.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel2;
        private TableLayoutPanel tableLayoutPanel4;
        private AntdUI.Panel panel4;
        private AntdUI.Panel panel3;
        private AntdUI.Panel panel2;
        private AntdUI.Panel panel1;
        private TableLayoutPanel tableLayoutPanel3;
        private Panel panel5;
        private AntdUI.Panel panel7;
        private AntdUI.Panel panel6;
        private AntdUI.Panel panel8;
        private AntdUI.Panel panel9;
        private AntdUI.Button button9;
        private AntdUI.Select select4;
        private AntdUI.Button button1;
        private AntdUI.Label label1;
        private AntdUI.Label label3;
        private AntdUI.Label label2;
        private AntdUI.Label label4;
        private AntdUI.Label label5;
    }
}
