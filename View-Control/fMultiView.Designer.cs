namespace ViewControl;

partial class fMultiView
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        windowBar = new AntdUI.PageHeader();
        panelTop = new AntdUI.Panel();
        labelSize = new AntdUI.Label();
        sliderSize = new AntdUI.Slider();
        labelOpacity = new AntdUI.Label();
        sliderOpacity = new AntdUI.Slider();
        labelSync = new AntdUI.Label();
        switchSync = new AntdUI.Switch();
        flowLayout = new FlowLayoutPanel();
        windowBar.SuspendLayout();
        panelTop.SuspendLayout();
        SuspendLayout();

        // windowBar — bật min/max/close button của Window
        windowBar.CloseSize = 30;
        windowBar.Dock = DockStyle.Top;
        windowBar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        windowBar.MDI = true;
        windowBar.ShowButton = true;
        windowBar.MaximizeBox = true;
        windowBar.MinimizeBox = true;
        windowBar.Name = "windowBar";
        windowBar.ShowIcon = true;
        windowBar.Size = new Size(1200, 38);
        windowBar.TabIndex = 0;
        windowBar.Text = "QN Android View";

        // panelTop — toolbar
        panelTop.Back = Color.FromArgb(248, 248, 250);
        panelTop.BorderColor = Color.FromArgb(220, 220, 225);
        panelTop.BorderWidth = 0;
        panelTop.Dock = DockStyle.Top;
        panelTop.Height = 48;
        panelTop.Name = "panelTop";
        panelTop.Padding = new Padding(12, 8, 12, 8);
        panelTop.TabIndex = 1;
        panelTop.Controls.Add(switchSync);
        panelTop.Controls.Add(labelSync);
        panelTop.Controls.Add(sliderOpacity);
        panelTop.Controls.Add(labelOpacity);
        panelTop.Controls.Add(sliderSize);
        panelTop.Controls.Add(labelSize);

        // labelSize
        labelSize.AutoSizeMode = AntdUI.TAutoSize.Auto;
        labelSize.Font = new Font("Segoe UI", 9F);
        labelSize.ForeColor = Color.FromArgb(70, 70, 75);
        labelSize.Location = new Point(12, 14);
        labelSize.Name = "labelSize";
        labelSize.Size = new Size(40, 22);
        labelSize.Text = "Cỡ";
        labelSize.TabIndex = 0;

        // sliderSize
        sliderSize.Location = new Point(56, 14);
        sliderSize.MinValue = 200;
        sliderSize.MaxValue = 900;
        sliderSize.Name = "sliderSize";
        sliderSize.Size = new Size(220, 22);
        sliderSize.TabIndex = 1;
        sliderSize.Value = 400;
        sliderSize.ValueChanged += sliderSize_ValueChanged;

        // labelOpacity
        labelOpacity.AutoSizeMode = AntdUI.TAutoSize.Auto;
        labelOpacity.Font = new Font("Segoe UI", 9F);
        labelOpacity.ForeColor = Color.FromArgb(70, 70, 75);
        labelOpacity.Location = new Point(296, 14);
        labelOpacity.Name = "labelOpacity";
        labelOpacity.Size = new Size(70, 22);
        labelOpacity.Text = "Lớp phủ";
        labelOpacity.TabIndex = 2;

        // sliderOpacity — 0..255 độ trong suốt overlay text deviceId+name
        sliderOpacity.Location = new Point(372, 14);
        sliderOpacity.MinValue = 0;
        sliderOpacity.MaxValue = 255;
        sliderOpacity.Name = "sliderOpacity";
        sliderOpacity.Size = new Size(180, 22);
        sliderOpacity.TabIndex = 3;
        sliderOpacity.Value = 255;
        sliderOpacity.ValueChanged += sliderOpacity_ValueChanged;

        // labelSync
        labelSync.AutoSizeMode = AntdUI.TAutoSize.Auto;
        labelSync.Font = new Font("Segoe UI", 9F);
        labelSync.ForeColor = Color.FromArgb(70, 70, 75);
        labelSync.Location = new Point(572, 14);
        labelSync.Name = "labelSync";
        labelSync.Size = new Size(120, 22);
        labelSync.Text = "Đồng bộ điều khiển";
        labelSync.TabIndex = 4;

        // switchSync — đồng bộ điều khiển: chạm/scroll trên 1 device sẽ broadcast ra tất cả
        switchSync.Location = new Point(692, 12);
        switchSync.Name = "switchSync";
        switchSync.Size = new Size(50, 26);
        switchSync.TabIndex = 5;
        switchSync.Checked = false;
        switchSync.CheckedChanged += switchSync_CheckedChanged;

        // flowLayout
        flowLayout.AutoScroll = true;
        flowLayout.BackColor = Color.FromArgb(235, 236, 240);
        flowLayout.Dock = DockStyle.Fill;
        flowLayout.Name = "flowLayout";
        flowLayout.Padding = new Padding(8);
        flowLayout.TabIndex = 2;

        // fMultiView
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1200, 800);
        Controls.Add(flowLayout);
        Controls.Add(panelTop);
        Controls.Add(windowBar);
        FormBorderStyle = FormBorderStyle.None;
        MinimumSize = new Size(640, 400);
        Name = "fMultiView";
        Text = "QN Android View";

        windowBar.ResumeLayout(false);
        panelTop.ResumeLayout(false);
        ResumeLayout(false);
    }

    private AntdUI.PageHeader windowBar;
    private AntdUI.Panel panelTop;
    private AntdUI.Label labelSize;
    private AntdUI.Slider sliderSize;
    private AntdUI.Label labelOpacity;
    private AntdUI.Slider sliderOpacity;
    private AntdUI.Label labelSync;
    private AntdUI.Switch switchSync;
    private FlowLayoutPanel flowLayout;
}
