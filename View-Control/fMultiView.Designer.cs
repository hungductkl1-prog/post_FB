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
        panelTop = new Panel();
        labelSize = new Label();
        sliderSize = new AntdUI.Slider();
        flowLayout = new FlowLayoutPanel();
        windowBar.SuspendLayout();
        panelTop.SuspendLayout();
        SuspendLayout();

        // windowBar — light mode
        windowBar.CloseSize = 30;
        windowBar.Dock = DockStyle.Top;
        windowBar.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
        windowBar.MDI = true;
        windowBar.Name = "windowBar";
        windowBar.ShowIcon = false;
        windowBar.Size = new Size(1200, 35);
        windowBar.TabIndex = 0;
        windowBar.Text = "Android View";

        // panelTop — toolbar
        panelTop.BackColor = Color.FromArgb(240, 240, 240);
        panelTop.Dock = DockStyle.Top;
        panelTop.Height = 36;
        panelTop.Name = "panelTop";
        panelTop.TabIndex = 1;
        panelTop.Controls.Add(sliderSize);
        panelTop.Controls.Add(labelSize);

        // labelSize
        labelSize.AutoSize = true;
        labelSize.Font = new Font("Segoe UI", 9F);
        labelSize.ForeColor = Color.FromArgb(60, 60, 60);
        labelSize.Location = new Point(8, 10);
        labelSize.Name = "labelSize";
        labelSize.Text = "Size:";
        labelSize.TabIndex = 0;

        // sliderSize
        sliderSize.Location = new Point(50, 8);
        sliderSize.MinValue = 150;
        sliderSize.MaxValue = 800;
        sliderSize.Name = "sliderSize";
        sliderSize.Size = new Size(200, 20);
        sliderSize.TabIndex = 1;
        sliderSize.Value = 300;
        sliderSize.ValueChanged += sliderSize_ValueChanged;

        // flowLayout
        flowLayout.AutoScroll = true;
        flowLayout.BackColor = Color.FromArgb(220, 220, 220);
        flowLayout.Dock = DockStyle.Fill;
        flowLayout.Name = "flowLayout";
        flowLayout.TabIndex = 2;

        // fMultiView
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1200, 800);
        Controls.Add(flowLayout);
        Controls.Add(panelTop);
        Controls.Add(windowBar);
        FormBorderStyle = FormBorderStyle.None;
        MinimumSize = new Size(400, 300);
        Name = "fMultiView";
        Text = "Android View";

        windowBar.ResumeLayout(false);
        panelTop.ResumeLayout(false);
        panelTop.PerformLayout();
        ResumeLayout(false);
    }

    private AntdUI.PageHeader windowBar;
    private Panel panelTop;
    private Label labelSize;
    private AntdUI.Slider sliderSize;
    private FlowLayoutPanel flowLayout;
}
