using StreamAndroid.Services;
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
        private System.Drawing.Point lastMousePosition;

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
        /// Optimized visual style and smoothness for DataGridView and panels.
        /// </summary>
        private void InitializeComponent()
        {
            AntdUI.SegmentedItem segmentedItem1 = new AntdUI.SegmentedItem();
            AntdUI.SegmentedItem segmentedItem2 = new AntdUI.SegmentedItem();
            tableLayoutPanel1 = new TableLayoutPanel();
            tableLayoutPanel4 = new TableLayoutPanel();
            panel9 = new AntdUI.Panel();
            button6 = new AntdUI.Button();
            button5 = new AntdUI.Button();
            button1 = new AntdUI.Button();
            select4 = new AntdUI.Select();
            button9 = new AntdUI.Button();
            tableLayoutPanel2 = new TableLayoutPanel();
            panel7 = new AntdUI.Panel();
            label3 = new AntdUI.Label();
            panel4 = new AntdUI.Panel();
            label4 = new AntdUI.Label();
            panel3 = new AntdUI.Panel();
            label5 = new AntdUI.Label();
            panel2 = new AntdUI.Panel();
            label2 = new AntdUI.Label();
            panel1 = new AntdUI.Panel();
            label1 = new AntdUI.Label();
            tableLayoutPanel3 = new TableLayoutPanel();
            panel8 = new AntdUI.Panel();
            groupBox2 = new GroupBox();
            panel11 = new Panel();
            slider1 = new AntdUI.Slider();
            label7 = new Label();
            panel10 = new Panel();
            slider3 = new AntdUI.Slider();
            label9 = new Label();
            panel13 = new Panel();
            slider4 = new AntdUI.Slider();
            label10 = new Label();
            switch1 = new AntdUI.Switch();
            button4 = new AntdUI.Button();
            button3 = new AntdUI.Button();
            button2 = new AntdUI.Button();
            label6 = new Label();
            panel5 = new Panel();
            pMain = new AntdUI.Panel();
            panel6 = new AntdUI.Panel();
            input6 = new AntdUI.Input();
            select1 = new AntdUI.Select();
            segmented5 = new AntdUI.Segmented();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel4.SuspendLayout();
            panel9.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            panel7.SuspendLayout();
            panel4.SuspendLayout();
            panel3.SuspendLayout();
            panel2.SuspendLayout();
            panel1.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            panel8.SuspendLayout();
            groupBox2.SuspendLayout();
            panel11.SuspendLayout();
            panel10.SuspendLayout();
            panel13.SuspendLayout();
            panel5.SuspendLayout();
            panel6.SuspendLayout();
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
            tableLayoutPanel1.Location = new System.Drawing.Point(20, 20);
            tableLayoutPanel1.Margin = new Padding(0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(1138, 633);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel4
            // 
            tableLayoutPanel4.ColumnCount = 1;
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.Controls.Add(panel9, 0, 0);
            tableLayoutPanel4.Dock = DockStyle.Fill;
            tableLayoutPanel4.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel4.Margin = new Padding(0, 0, 0, 8);
            tableLayoutPanel4.Name = "tableLayoutPanel4";
            tableLayoutPanel4.RowCount = 1;
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.Size = new Size(1138, 52);
            tableLayoutPanel4.TabIndex = 0;
            // 
            // panel9
            // 
            panel9.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel9.Back = Color.White;
            panel9.BackColor = Color.Transparent;
            panel9.Controls.Add(button6);
            panel9.Controls.Add(button5);
            panel9.Controls.Add(button1);
            panel9.Controls.Add(select4);
            panel9.Controls.Add(button9);
            panel9.Location = new System.Drawing.Point(0, 0);
            panel9.Margin = new Padding(0);
            panel9.Name = "panel9";
            panel9.Padding = new Padding(12, 8, 12, 8);
            panel9.Radius = 12;
            panel9.Shadow = 2;
            panel9.Size = new Size(1138, 52);
            panel9.TabIndex = 5;
            // 
            // button6
            // 
            button6.BorderWidth = 1F;
            button6.DefaultBorderColor = Color.FromArgb(217, 217, 217);
            button6.Dock = DockStyle.Left;
            button6.Font = new Font("Segoe UI", 9F);
            button6.IconRatio = 0.75F;
            button6.IconSvg = "InteractionOutlined";
            button6.Location = new System.Drawing.Point(134, 10);
            button6.Margin = new Padding(4, 0, 0, 0);
            button6.Name = "button6";
            button6.Size = new Size(120, 32);
            button6.TabIndex = 15;
            button6.Text = "Kill ADB";
            // 
            // button5
            // 
            button5.BorderWidth = 1F;
            button5.DefaultBorderColor = Color.FromArgb(217, 217, 217);
            button5.Dock = DockStyle.Left;
            button5.Font = new Font("Segoe UI", 9F);
            button5.IconRatio = 0.75F;
            button5.IconSvg = "ReloadOutlined";
            button5.Location = new System.Drawing.Point(14, 10);
            button5.Margin = new Padding(0);
            button5.Name = "button5";
            button5.Size = new Size(120, 32);
            button5.TabIndex = 14;
            button5.Text = "Tải lại";
            // 
            // button1
            // 
            button1.BorderWidth = 1F;
            button1.DefaultBorderColor = Color.FromArgb(217, 217, 217);
            button1.Dock = DockStyle.Right;
            button1.Font = new Font("Segoe UI", 9F);
            button1.IconRatio = 0.75F;
            button1.IconSvg = "MenuUnfoldOutlined";
            button1.Location = new System.Drawing.Point(706, 10);
            button1.Margin = new Padding(0, 0, 4, 0);
            button1.Name = "button1";
            button1.Size = new Size(140, 32);
            button1.TabIndex = 13;
            button1.Text = "Ẩn điều khiển";
            button1.Click += button1_Click;
            // 
            // select4
            // 
            select4.Dock = DockStyle.Right;
            select4.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            select4.List = true;
            select4.Location = new System.Drawing.Point(846, 10);
            select4.Margin = new Padding(0, 0, 4, 0);
            select4.Name = "select4";
            select4.PlaceholderText = "Chọn nhóm";
            select4.Size = new Size(160, 32);
            select4.TabIndex = 12;
            select4.SelectedIndexChanged += select4_SelectedIndexChanged;
            // 
            // button9
            // 
            button9.DefaultBack = Color.FromArgb(22, 119, 255);
            button9.Dock = DockStyle.Right;
            button9.Font = new Font("Segoe UI", 9F);
            button9.ForeColor = Color.White;
            button9.IconRatio = 0.75F;
            button9.IconSvg = "PlusOutlined";
            button9.Location = new System.Drawing.Point(1006, 10);
            button9.Margin = new Padding(0);
            button9.Name = "button9";
            button9.Size = new Size(118, 32);
            button9.TabIndex = 6;
            button9.Text = "Thêm";
            button9.Click += button9_Click;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 5;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel2.Controls.Add(panel7, 4, 0);
            tableLayoutPanel2.Controls.Add(panel4, 3, 0);
            tableLayoutPanel2.Controls.Add(panel3, 2, 0);
            tableLayoutPanel2.Controls.Add(panel2, 1, 0);
            tableLayoutPanel2.Controls.Add(panel1, 0, 0);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new System.Drawing.Point(0, 60);
            tableLayoutPanel2.Margin = new Padding(0, 0, 0, 8);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Size = new Size(1138, 62);
            tableLayoutPanel2.TabIndex = 0;
            // 
            // panel7
            // 
            panel7.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel7.Back = Color.White;
            panel7.BackColor = Color.Transparent;
            panel7.Controls.Add(label3);
            panel7.Location = new System.Drawing.Point(908, 0);
            panel7.Margin = new Padding(0, 0, 4, 0);
            panel7.Name = "panel7";
            panel7.Padding = new Padding(12);
            panel7.Radius = 12;
            panel7.Shadow = 2;
            panel7.Size = new Size(226, 62);
            panel7.TabIndex = 5;
            // 
            // label3
            // 
            label3.Dock = DockStyle.Fill;
            label3.Font = new Font("Segoe UI", 9F);
            label3.ForeColor = Color.FromArgb(245, 34, 45);
            label3.IconGap = 10;
            label3.Location = new System.Drawing.Point(14, 14);
            label3.Name = "label3";
            label3.PrefixSvg = "InfoCircleOutlined";
            label3.RightToLeft = RightToLeft.No;
            label3.Size = new Size(198, 34);
            label3.TabIndex = 2;
            label3.Text = "Ngắt kết nối\r\n12";
            // 
            // panel4
            // 
            panel4.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel4.Back = Color.White;
            panel4.BackColor = Color.Transparent;
            panel4.Controls.Add(label4);
            panel4.Location = new System.Drawing.Point(681, 0);
            panel4.Margin = new Padding(0);
            panel4.Name = "panel4";
            panel4.Padding = new Padding(12);
            panel4.Radius = 12;
            panel4.Shadow = 2;
            panel4.Size = new Size(227, 62);
            panel4.TabIndex = 4;
            // 
            // label4
            // 
            label4.Dock = DockStyle.Fill;
            label4.Font = new Font("Segoe UI", 9F);
            label4.ForeColor = Color.DarkOrange;
            label4.IconGap = 10;
            label4.Location = new System.Drawing.Point(14, 14);
            label4.Name = "label4";
            label4.PrefixSvg = "InfoCircleOutlined";
            label4.RightToLeft = RightToLeft.No;
            label4.Size = new Size(199, 34);
            label4.TabIndex = 3;
            label4.Text = "Bôi đen\r\n12";
            // 
            // panel3
            // 
            panel3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel3.Back = Color.White;
            panel3.BackColor = Color.Transparent;
            panel3.Controls.Add(label5);
            panel3.Location = new System.Drawing.Point(454, 0);
            panel3.Margin = new Padding(0, 0, 4, 0);
            panel3.Name = "panel3";
            panel3.Padding = new Padding(12);
            panel3.Radius = 12;
            panel3.Shadow = 2;
            panel3.Size = new Size(223, 62);
            panel3.TabIndex = 3;
            // 
            // label5
            // 
            label5.Dock = DockStyle.Fill;
            label5.Font = new Font("Segoe UI", 9F);
            label5.ForeColor = Color.FromArgb(47, 84, 235);
            label5.IconGap = 10;
            label5.Location = new System.Drawing.Point(14, 14);
            label5.Name = "label5";
            label5.PrefixSvg = "CheckSquareOutlined";
            label5.RightToLeft = RightToLeft.No;
            label5.Size = new Size(195, 34);
            label5.TabIndex = 2;
            label5.Text = "Đã chọn\r\n12";
            // 
            // panel2
            // 
            panel2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel2.Back = Color.White;
            panel2.BackColor = Color.Transparent;
            panel2.Controls.Add(label2);
            panel2.Location = new System.Drawing.Point(227, 0);
            panel2.Margin = new Padding(0, 0, 4, 0);
            panel2.Name = "panel2";
            panel2.Padding = new Padding(12);
            panel2.Radius = 12;
            panel2.Shadow = 2;
            panel2.Size = new Size(223, 62);
            panel2.TabIndex = 2;
            // 
            // label2
            // 
            label2.Dock = DockStyle.Fill;
            label2.Font = new Font("Segoe UI", 9F);
            label2.ForeColor = Color.FromArgb(82, 196, 26);
            label2.IconGap = 10;
            label2.Location = new System.Drawing.Point(14, 14);
            label2.Name = "label2";
            label2.PrefixSvg = "SafetyOutlined";
            label2.RightToLeft = RightToLeft.No;
            label2.Size = new Size(195, 34);
            label2.TabIndex = 1;
            label2.Text = "Đã kết nối\r\n12";
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel1.Back = Color.White;
            panel1.BackColor = Color.Transparent;
            panel1.Controls.Add(label1);
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Margin = new Padding(0, 0, 4, 0);
            panel1.Name = "panel1";
            panel1.Padding = new Padding(12);
            panel1.Radius = 12;
            panel1.Shadow = 2;
            panel1.Size = new Size(223, 62);
            panel1.TabIndex = 1;
            // 
            // label1
            // 
            label1.Dock = DockStyle.Fill;
            label1.Font = new Font("Segoe UI", 9F);
            label1.ForeColor = Color.FromArgb(24, 144, 255);
            label1.IconGap = 10;
            label1.IconRatio = 0.6F;
            label1.Location = new System.Drawing.Point(14, 14);
            label1.Name = "label1";
            label1.PrefixSvg = "TabletOutlined";
            label1.RightToLeft = RightToLeft.No;
            label1.Size = new Size(195, 34);
            label1.TabIndex = 0;
            label1.Text = "Tổng thiết bị\r\n12";
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.ColumnCount = 2;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270F));
            tableLayoutPanel3.Controls.Add(panel8, 1, 0);
            tableLayoutPanel3.Controls.Add(panel5, 0, 0);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new System.Drawing.Point(0, 130);
            tableLayoutPanel3.Margin = new Padding(0);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 1;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.Size = new Size(1138, 503);
            tableLayoutPanel3.TabIndex = 1;
            // 
            // panel8
            // 
            panel8.Back = Color.White;
            panel8.BackColor = Color.Transparent;
            panel8.Controls.Add(groupBox2);
            panel8.Controls.Add(button4);
            panel8.Controls.Add(button3);
            panel8.Controls.Add(button2);
            panel8.Controls.Add(label6);
            panel8.Dock = DockStyle.Fill;
            panel8.Location = new System.Drawing.Point(868, 0);
            panel8.Margin = new Padding(0);
            panel8.Name = "panel8";
            panel8.Padding = new Padding(12);
            panel8.Radius = 12;
            panel8.Shadow = 2;
            panel8.Size = new Size(270, 503);
            panel8.TabIndex = 4;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(panel11);
            groupBox2.Controls.Add(panel10);
            groupBox2.Controls.Add(panel13);
            groupBox2.Controls.Add(switch1);
            groupBox2.Dock = DockStyle.Top;
            groupBox2.Font = new Font("Segoe UI", 9F);
            groupBox2.Location = new System.Drawing.Point(14, 140);
            groupBox2.Margin = new Padding(0);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(8);
            groupBox2.Size = new Size(242, 141);
            groupBox2.TabIndex = 19;
            groupBox2.TabStop = false;
            groupBox2.Text = "Điều khiển màn hình";
            // 
            // panel11
            // 
            panel11.Controls.Add(slider1);
            panel11.Controls.Add(label7);
            panel11.Dock = DockStyle.Top;
            panel11.Location = new System.Drawing.Point(8, 108);
            panel11.Margin = new Padding(0);
            panel11.Name = "panel11";
            panel11.Size = new Size(226, 28);
            panel11.TabIndex = 20;
            // 
            // slider1
            // 
            slider1.Dock = DockStyle.Fill;
            slider1.Location = new System.Drawing.Point(91, 0);
            slider1.MinValue = 10;
            slider1.Name = "slider1";
            slider1.ShowValue = true;
            slider1.Size = new Size(135, 28);
            slider1.TabIndex = 19;
            slider1.Value = 100;
            slider1.ValueChanged += slider1_ValueChanged;
            // 
            // label7
            // 
            label7.Dock = DockStyle.Left;
            label7.Font = new Font("Segoe UI", 8.5F);
            label7.Location = new System.Drawing.Point(0, 0);
            label7.Name = "label7";
            label7.Size = new Size(91, 28);
            label7.TabIndex = 17;
            label7.Text = "Mã điện thoại";
            label7.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panel10
            // 
            panel10.Controls.Add(slider3);
            panel10.Controls.Add(label9);
            panel10.Dock = DockStyle.Top;
            panel10.Location = new System.Drawing.Point(8, 80);
            panel10.Margin = new Padding(0);
            panel10.Name = "panel10";
            panel10.Size = new Size(226, 28);
            panel10.TabIndex = 19;
            // 
            // slider3
            // 
            slider3.Dock = DockStyle.Fill;
            slider3.Location = new System.Drawing.Point(91, 0);
            slider3.MaxValue = 840;
            slider3.MinValue = 192;
            slider3.Name = "slider3";
            slider3.ShowValue = true;
            slider3.Size = new Size(135, 28);
            slider3.TabIndex = 19;
            slider3.Value = 192;
            slider3.ValueChanged += slider3_ValueChanged;
            // 
            // label9
            // 
            label9.Dock = DockStyle.Left;
            label9.Font = new Font("Segoe UI", 8.5F);
            label9.Location = new System.Drawing.Point(0, 0);
            label9.Name = "label9";
            label9.Size = new Size(91, 28);
            label9.TabIndex = 17;
            label9.Text = "Kích thước nhỏ";
            label9.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panel13
            // 
            panel13.Controls.Add(slider4);
            panel13.Controls.Add(label10);
            panel13.Dock = DockStyle.Top;
            panel13.Location = new System.Drawing.Point(8, 52);
            panel13.Margin = new Padding(0, 0, 0, 4);
            panel13.Name = "panel13";
            panel13.Size = new Size(226, 28);
            panel13.TabIndex = 18;
            // 
            // slider4
            // 
            slider4.Dock = DockStyle.Fill;
            slider4.Location = new System.Drawing.Point(91, 0);
            slider4.MaxValue = 1240;
            slider4.MinValue = 480;
            slider4.Name = "slider4";
            slider4.ShowValue = true;
            slider4.Size = new Size(135, 28);
            slider4.TabIndex = 19;
            slider4.Value = 1024;
            slider4.ValueChanged += slider4_ValueChanged;
            // 
            // label10
            // 
            label10.Dock = DockStyle.Left;
            label10.Font = new Font("Segoe UI", 8.5F);
            label10.Location = new System.Drawing.Point(0, 0);
            label10.Name = "label10";
            label10.Size = new Size(91, 28);
            label10.TabIndex = 17;
            label10.Text = "Kích thước lớn";
            label10.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // switch1
            // 
            switch1.Checked = true;
            switch1.CheckedText = "Điều khiển màn hình nhỏ";
            switch1.Dock = DockStyle.Top;
            switch1.Fill = Color.FromArgb(82, 196, 26);
            switch1.Location = new System.Drawing.Point(8, 24);
            switch1.Margin = new Padding(0, 0, 0, 4);
            switch1.Name = "switch1";
            switch1.Size = new Size(226, 28);
            switch1.TabIndex = 17;
            switch1.UnCheckedText = "Điều khiển màn hình nhỏ";
            switch1.CheckedChanged += switch1_CheckedChanged;
            // 
            // button4
            // 
            button4.BorderWidth = 1F;
            button4.DefaultBorderColor = Color.FromArgb(217, 217, 217);
            button4.Dock = DockStyle.Top;
            button4.Font = new Font("Segoe UI", 9F);
            button4.IconHoverSvg = "";
            button4.IconRatio = 0.75F;
            button4.IconSvg = "ScanOutlined";
            button4.LoadingValue = -1F;
            button4.Location = new System.Drawing.Point(14, 106);
            button4.Margin = new Padding(0, 0, 0, 8);
            button4.Name = "button4";
            button4.Size = new Size(242, 34);
            button4.TabIndex = 18;
            button4.Text = "OTG";
            // 
            // button3
            // 
            button3.BorderWidth = 1F;
            button3.DefaultBorderColor = Color.FromArgb(217, 217, 217);
            button3.Dock = DockStyle.Top;
            button3.Font = new Font("Segoe UI", 9F);
            button3.IconRatio = 0.75F;
            button3.IconSvg = "SettingOutlined";
            button3.Location = new System.Drawing.Point(14, 72);
            button3.Margin = new Padding(0, 0, 0, 4);
            button3.Name = "button3";
            button3.Size = new Size(242, 34);
            button3.TabIndex = 15;
            button3.Text = "Cài đặt";
            // 
            // button2
            // 
            button2.BorderWidth = 1F;
            button2.DefaultBorderColor = Color.FromArgb(217, 217, 217);
            button2.Dock = DockStyle.Top;
            button2.Font = new Font("Segoe UI", 9F);
            button2.IconHoverSvg = "";
            button2.IconRatio = 0.75F;
            button2.IconSvg = "RotateRightOutlined";
            button2.Location = new System.Drawing.Point(14, 38);
            button2.Margin = new Padding(0, 0, 0, 4);
            button2.Name = "button2";
            button2.Size = new Size(242, 34);
            button2.TabIndex = 14;
            button2.Text = "Xoay màn hình";
            button2.Click += button2_Click;
            // 
            // label6
            // 
            label6.Dock = DockStyle.Top;
            label6.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            label6.Location = new System.Drawing.Point(14, 14);
            label6.Margin = new Padding(0, 0, 0, 8);
            label6.Name = "label6";
            label6.Size = new Size(242, 24);
            label6.TabIndex = 0;
            label6.Text = "Hành động hàng loạt";
            // 
            // panel5
            // 
            panel5.Controls.Add(pMain);
            panel5.Controls.Add(panel6);
            panel5.Dock = DockStyle.Fill;
            panel5.Location = new System.Drawing.Point(0, 0);
            panel5.Margin = new Padding(0, 0, 8, 0);
            panel5.Name = "panel5";
            panel5.Size = new Size(860, 503);
            panel5.TabIndex = 0;
            // 
            // pMain
            // 
            pMain.Back = Color.White;
            pMain.BackColor = Color.Transparent;
            pMain.Dock = DockStyle.Fill;
            pMain.Location = new System.Drawing.Point(0, 60);
            pMain.Margin = new Padding(0);
            pMain.Name = "pMain";
            pMain.Padding = new Padding(12);
            pMain.Radius = 12;
            pMain.Shadow = 2;
            pMain.Size = new Size(860, 443);
            pMain.TabIndex = 3;
            // 
            // panel6
            // 
            panel6.Back = Color.White;
            panel6.BackColor = Color.Transparent;
            panel6.Controls.Add(input6);
            panel6.Controls.Add(select1);
            panel6.Controls.Add(segmented5);
            panel6.Dock = DockStyle.Top;
            panel6.Location = new System.Drawing.Point(0, 0);
            panel6.Margin = new Padding(0, 0, 0, 8);
            panel6.Name = "panel6";
            panel6.Padding = new Padding(12);
            panel6.Radius = 12;
            panel6.Shadow = 2;
            panel6.Size = new Size(860, 60);
            panel6.TabIndex = 2;
            // 
            // input6
            // 
            input6.AllowClear = true;
            input6.Dock = DockStyle.Fill;
            input6.Font = new Font("Segoe UI", 9F);
            input6.Location = new System.Drawing.Point(14, 14);
            input6.Name = "input6";
            input6.PlaceholderText = "Tìm kiếm theo tên hoặc serial...";
            input6.PrefixSvg = "SearchOutlined";
            input6.Size = new Size(594, 32);
            input6.TabIndex = 14;
            input6.TextChanged += input6_TextChanged;
            // 
            // select1
            // 
            select1.Dock = DockStyle.Right;
            select1.Font = new Font("Segoe UI", 9F);
            select1.List = true;
            select1.Location = new System.Drawing.Point(608, 14);
            select1.Margin = new Padding(0, 0, 4, 0);
            select1.Name = "select1";
            select1.PlaceholderText = "Bộ lọc";
            select1.PrefixSvg = "FilterOutlined";
            select1.Size = new Size(150, 32);
            select1.TabIndex = 13;
            select1.SelectedIndexChanged += select4_SelectedIndexChanged;
            // 
            // segmented5
            // 
            segmented5.BackActive = Color.FromArgb(24, 144, 255);
            segmented5.BackColor = Color.White;
            segmented5.Dock = DockStyle.Right;
            segmented5.ForeActive = Color.White;
            segmented5.IconRatio = 1.1F;
            segmentedItem1.IconSvg = "AppstoreOutlined";
            segmentedItem2.IconSvg = "BarsOutlined";
            segmented5.Items.Add(segmentedItem1);
            segmented5.Items.Add(segmentedItem2);
            segmented5.Location = new System.Drawing.Point(758, 14);
            segmented5.Name = "segmented5";
            segmented5.Padding = new Padding(3);
            segmented5.SelectIndex = 0;
            segmented5.Size = new Size(88, 32);
            segmented5.TabIndex = 5;
            segmented5.SelectIndexChanged += segmented5_SelectIndexChanged;
            // 
            // ucManagerDevices
            // 
            BackColor = Color.FromArgb(240, 242, 245);
            Controls.Add(tableLayoutPanel1);
            DoubleBuffered = true;
            Name = "ucManagerDevices";
            Padding = new Padding(20);
            RightToLeft = RightToLeft.No;
            Size = new Size(1178, 673);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel4.ResumeLayout(false);
            panel9.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            panel7.ResumeLayout(false);
            panel4.ResumeLayout(false);
            panel3.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel1.ResumeLayout(false);
            tableLayoutPanel3.ResumeLayout(false);
            panel8.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            panel11.ResumeLayout(false);
            panel10.ResumeLayout(false);
            panel13.ResumeLayout(false);
            panel5.ResumeLayout(false);
            panel6.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel2;
        private TableLayoutPanel tableLayoutPanel3;
        private TableLayoutPanel tableLayoutPanel4;
        private Panel panel5;
        private AntdUI.Panel panel1;
        private AntdUI.Panel panel2;
        private AntdUI.Panel panel3;
        private AntdUI.Panel panel4;
        private AntdUI.Panel panel6;
        private AntdUI.Panel pMain;
        private AntdUI.Panel panel8;
        private AntdUI.Panel panel9;
        public AntdUI.Label label4;
        private Label label6;
        private Label label9;
        private Label label10;
        private AntdUI.Input input6;
        private AntdUI.Segmented segmented5;
        private AntdUI.Button button1;
        private AntdUI.Button button2;
        private AntdUI.Button button3;
        private AntdUI.Button button4;
        private AntdUI.Button button5;
        private AntdUI.Button button6;
        private AntdUI.Button button9;
        private AntdUI.Switch switch1;
        public AntdUI.Slider slider3;
        public AntdUI.Slider slider4;
        private GroupBox groupBox2;
        private Panel panel10;
        private Panel panel13;
        private AntdUI.Panel panel7;
        public AntdUI.Label label1;
        public AntdUI.Label label5;
        public AntdUI.Label label2;
        public AntdUI.Label label3;
        public AntdUI.Select select4;
        public AntdUI.Select select1;
        private Panel panel11;
        public AntdUI.Slider slider1;
        private Label label7;
    }
}
