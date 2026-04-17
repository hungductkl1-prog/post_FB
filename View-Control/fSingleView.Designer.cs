namespace ViewControl;

partial class fSingleView
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
        panelDevice = new Panel();
        panelControls = new AntdUI.Panel();
        btnBack = new AntdUI.Button();
        btnHome = new AntdUI.Button();
        btnRecent = new AntdUI.Button();
        btnPower = new AntdUI.Button();
        btnVolumeUp = new AntdUI.Button();
        btnVolumeDown = new AntdUI.Button();
        labelStatus = new Label();
        windowBar.SuspendLayout();
        panelControls.SuspendLayout();
        SuspendLayout();

        // windowBar — light mode
        windowBar.CloseSize = 30;
        windowBar.Dock = DockStyle.Top;
        windowBar.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
        windowBar.MDI = true;
        windowBar.Name = "windowBar";
        windowBar.ShowIcon = false;
        windowBar.Size = new Size(580, 35);
        windowBar.TabIndex = 0;
        windowBar.Text = "Android View";

        // panelDevice
        panelDevice.BackColor = Color.FromArgb(230, 230, 230);
        panelDevice.Dock = DockStyle.Fill;
        panelDevice.Name = "panelDevice";
        panelDevice.TabIndex = 1;
        panelDevice.Controls.Add(labelStatus);

        // labelStatus — shown while connecting
        labelStatus.AutoSize = false;
        labelStatus.Dock = DockStyle.Fill;
        labelStatus.Font = new Font("Segoe UI", 10F);
        labelStatus.ForeColor = Color.FromArgb(100, 100, 100);
        labelStatus.Name = "labelStatus";
        labelStatus.Text = "Đang kết nối...";
        labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // panelControls — right sidebar
        panelControls.BorderColor = Color.FromArgb(210, 210, 210);
        panelControls.BorderWidth = 1F;
        panelControls.Dock = DockStyle.Right;
        panelControls.Name = "panelControls";
        panelControls.Size = new Size(60, 600);
        panelControls.TabIndex = 2;
        panelControls.Controls.Add(btnVolumeDown);
        panelControls.Controls.Add(btnVolumeUp);
        panelControls.Controls.Add(btnPower);
        panelControls.Controls.Add(btnRecent);
        panelControls.Controls.Add(btnHome);
        panelControls.Controls.Add(btnBack);

        int btnX = 8;
        var btnSize = new Size(44, 38);

        btnBack.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        btnBack.IconSvg = "ArrowLeftOutlined";
        btnBack.Location = new Point(btnX, 20);
        btnBack.Name = "btnBack";
        btnBack.Radius = 6;
        btnBack.Size = btnSize;
        btnBack.TabIndex = 0;
        btnBack.WaveSize = 0;
        btnBack.Click += btnBack_Click;

        btnHome.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        btnHome.IconSvg = "HomeOutlined";
        btnHome.Location = new Point(btnX, 68);
        btnHome.Name = "btnHome";
        btnHome.Radius = 6;
        btnHome.Size = btnSize;
        btnHome.TabIndex = 1;
        btnHome.WaveSize = 0;
        btnHome.Click += btnHome_Click;

        btnRecent.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        btnRecent.IconSvg = "AppstoreOutlined";
        btnRecent.Location = new Point(btnX, 116);
        btnRecent.Name = "btnRecent";
        btnRecent.Radius = 6;
        btnRecent.Size = btnSize;
        btnRecent.TabIndex = 2;
        btnRecent.WaveSize = 0;
        btnRecent.Click += btnRecent_Click;

        btnPower.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        btnPower.IconSvg = "PoweroffOutlined";
        btnPower.Location = new Point(btnX, 180);
        btnPower.Name = "btnPower";
        btnPower.Radius = 6;
        btnPower.Size = btnSize;
        btnPower.TabIndex = 3;
        btnPower.WaveSize = 0;
        btnPower.Click += btnPower_Click;

        btnVolumeUp.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        btnVolumeUp.IconSvg = "SoundOutlined";
        btnVolumeUp.Location = new Point(btnX, 244);
        btnVolumeUp.Name = "btnVolumeUp";
        btnVolumeUp.Radius = 6;
        btnVolumeUp.Size = btnSize;
        btnVolumeUp.TabIndex = 4;
        btnVolumeUp.WaveSize = 0;
        btnVolumeUp.Click += btnVolumeUp_Click;

        btnVolumeDown.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        btnVolumeDown.IconSvg = "MinusOutlined";
        btnVolumeDown.Location = new Point(btnX, 292);
        btnVolumeDown.Name = "btnVolumeDown";
        btnVolumeDown.Radius = 6;
        btnVolumeDown.Size = btnSize;
        btnVolumeDown.TabIndex = 5;
        btnVolumeDown.WaveSize = 0;
        btnVolumeDown.Click += btnVolumeDown_Click;

        // fSingleView
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(580, 960);
        Controls.Add(panelDevice);
        Controls.Add(panelControls);
        Controls.Add(windowBar);
        FormBorderStyle = FormBorderStyle.None;
        MinimumSize = new Size(340, 400);
        Name = "fSingleView";
        Text = "Android View";

        windowBar.ResumeLayout(false);
        panelControls.ResumeLayout(false);
        ResumeLayout(false);
    }

    private AntdUI.PageHeader windowBar;
    private Panel panelDevice;
    private AntdUI.Panel panelControls;
    private AntdUI.Button btnBack;
    private AntdUI.Button btnHome;
    private AntdUI.Button btnRecent;
    private AntdUI.Button btnPower;
    private AntdUI.Button btnVolumeUp;
    private AntdUI.Button btnVolumeDown;
    private Label labelStatus;
}
