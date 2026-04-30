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
        panelBody = new AntdUI.Panel();
        panelDevice = new AntdUI.Panel();
        panelControls = new AntdUI.Panel();
        labelGroupNav = new AntdUI.Label();
        btnBack = new AntdUI.Button();
        btnHome = new AntdUI.Button();
        btnRecent = new AntdUI.Button();
        dividerControls = new AntdUI.Divider();
        labelGroupSystem = new AntdUI.Label();
        btnPower = new AntdUI.Button();
        btnVolumeUp = new AntdUI.Button();
        btnVolumeDown = new AntdUI.Button();
        labelStatus = new AntdUI.Label();
        windowBar.SuspendLayout();
        panelBody.SuspendLayout();
        panelControls.SuspendLayout();
        SuspendLayout();

        // windowBar
        windowBar.CloseSize = 32;
        windowBar.Dock = DockStyle.Top;
        windowBar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        windowBar.MDI = true;
        windowBar.ShowButton = true;
        windowBar.MaximizeBox = true;
        windowBar.MinimizeBox = true;
        windowBar.Name = "windowBar";
        windowBar.ShowIcon = true;
        windowBar.Size = new Size(620, 40);
        windowBar.TabIndex = 0;
        windowBar.Text = "Golike Android View";

        // panelBody — wrap toàn bộ phía dưới windowBar
        panelBody.Back = Color.FromArgb(245, 246, 250);
        panelBody.BorderWidth = 0;
        panelBody.Dock = DockStyle.Fill;
        panelBody.Name = "panelBody";
        panelBody.Padding = new Padding(0);
        panelBody.Controls.Add(panelDevice);
        panelBody.Controls.Add(panelControls);

        // panelDevice — body chứa ucDeviceView
        panelDevice.Back = Color.FromArgb(245, 246, 250);
        panelDevice.BorderWidth = 0;
        panelDevice.Dock = DockStyle.Fill;
        panelDevice.Name = "panelDevice";
        panelDevice.Padding = new Padding(12);
        panelDevice.TabIndex = 1;
        panelDevice.Controls.Add(labelStatus);

        // labelStatus — shown while connecting
        labelStatus.AutoSizeMode = AntdUI.TAutoSize.None;
        labelStatus.Dock = DockStyle.Fill;
        labelStatus.Font = new Font("Segoe UI", 10F);
        labelStatus.ForeColor = Color.FromArgb(100, 100, 110);
        labelStatus.Name = "labelStatus";
        labelStatus.Text = "Đang kết nối...";
        labelStatus.TextAlign = ContentAlignment.MiddleCenter;

        // panelControls — right sidebar đẹp hơn
        panelControls.Back = Color.FromArgb(28, 30, 40);
        panelControls.BorderWidth = 0;
        panelControls.Radius = 0;
        panelControls.Dock = DockStyle.Right;
        panelControls.Name = "panelControls";
        panelControls.Padding = new Padding(8, 14, 8, 14);
        panelControls.Size = new Size(74, 600);
        panelControls.TabIndex = 2;
        panelControls.Controls.Add(btnVolumeDown);
        panelControls.Controls.Add(btnVolumeUp);
        panelControls.Controls.Add(btnPower);
        panelControls.Controls.Add(labelGroupSystem);
        panelControls.Controls.Add(dividerControls);
        panelControls.Controls.Add(btnRecent);
        panelControls.Controls.Add(btnHome);
        panelControls.Controls.Add(btnBack);
        panelControls.Controls.Add(labelGroupNav);

        int btnX = 11;
        int btnW = 52;
        int btnH = 44;

        // labelGroupNav
        labelGroupNav.AutoSizeMode = AntdUI.TAutoSize.None;
        labelGroupNav.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
        labelGroupNav.ForeColor = Color.FromArgb(140, 145, 160);
        labelGroupNav.Location = new Point(0, 8);
        labelGroupNav.Name = "labelGroupNav";
        labelGroupNav.Size = new Size(74, 14);
        labelGroupNav.Text = "ĐIỀU HƯỚNG";
        labelGroupNav.TextAlign = ContentAlignment.MiddleCenter;

        StyleNavButton(btnBack, "ArrowLeftOutlined");
        btnBack.Location = new Point(btnX, 28);
        btnBack.Size = new Size(btnW, btnH);
        btnBack.TabIndex = 0;
        btnBack.Click += btnBack_Click;

        StyleNavButton(btnHome, "HomeOutlined");
        btnHome.Location = new Point(btnX, 80);
        btnHome.Size = new Size(btnW, btnH);
        btnHome.TabIndex = 1;
        btnHome.Click += btnHome_Click;

        StyleNavButton(btnRecent, "AppstoreOutlined");
        btnRecent.Location = new Point(btnX, 132);
        btnRecent.Size = new Size(btnW, btnH);
        btnRecent.TabIndex = 2;
        btnRecent.Click += btnRecent_Click;

        // dividerControls
        dividerControls.Location = new Point(8, 188);
        dividerControls.Name = "dividerControls";
        dividerControls.Size = new Size(58, 12);
        dividerControls.Vertical = false;
        dividerControls.Thickness = 1F;
        dividerControls.ColorSplit = Color.FromArgb(60, 64, 80);

        // labelGroupSystem
        labelGroupSystem.AutoSizeMode = AntdUI.TAutoSize.None;
        labelGroupSystem.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
        labelGroupSystem.ForeColor = Color.FromArgb(140, 145, 160);
        labelGroupSystem.Location = new Point(0, 204);
        labelGroupSystem.Name = "labelGroupSystem";
        labelGroupSystem.Size = new Size(74, 14);
        labelGroupSystem.Text = "HỆ THỐNG";
        labelGroupSystem.TextAlign = ContentAlignment.MiddleCenter;

        StyleNavButton(btnPower, "PoweroffOutlined", isDanger: true);
        btnPower.Location = new Point(btnX, 224);
        btnPower.Size = new Size(btnW, btnH);
        btnPower.TabIndex = 3;
        btnPower.Click += btnPower_Click;

        StyleNavButton(btnVolumeUp, "SoundOutlined");
        btnVolumeUp.Location = new Point(btnX, 276);
        btnVolumeUp.Size = new Size(btnW, btnH);
        btnVolumeUp.TabIndex = 4;
        btnVolumeUp.Click += btnVolumeUp_Click;

        StyleNavButton(btnVolumeDown, "MinusOutlined");
        btnVolumeDown.Location = new Point(btnX, 328);
        btnVolumeDown.Size = new Size(btnW, btnH);
        btnVolumeDown.TabIndex = 5;
        btnVolumeDown.Click += btnVolumeDown_Click;

        // fSingleView
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(620, 980);
        Controls.Add(panelBody);
        Controls.Add(windowBar);
        FormBorderStyle = FormBorderStyle.None;
        MinimumSize = new Size(380, 460);
        Name = "fSingleView";
        Text = "Golike Android View";

        windowBar.ResumeLayout(false);
        panelBody.ResumeLayout(false);
        panelControls.ResumeLayout(false);
        ResumeLayout(false);
    }

    private static void StyleNavButton(AntdUI.Button btn, string iconSvg, bool isDanger = false)
    {
        btn.Type = AntdUI.TTypeMini.Default;
        btn.IconSvg = iconSvg;
        btn.IconRatio = 0.5F;
        btn.Radius = 10;
        btn.WaveSize = 0;
        btn.BorderWidth = 0;
        btn.Cursor = Cursors.Hand;
        btn.DefaultBack = Color.FromArgb(44, 47, 60);
        btn.BackHover = isDanger ? Color.FromArgb(180, 60, 60) : Color.FromArgb(70, 130, 230);
        btn.BackActive = isDanger ? Color.FromArgb(150, 40, 40) : Color.FromArgb(50, 100, 200);
        btn.DefaultBorderColor = Color.Transparent;
        btn.ForeColor = isDanger ? Color.FromArgb(240, 130, 130) : Color.FromArgb(225, 230, 240);
    }

    private AntdUI.PageHeader windowBar;
    private AntdUI.Panel panelBody;
    private AntdUI.Panel panelDevice;
    private AntdUI.Panel panelControls;
    private AntdUI.Label labelGroupNav;
    private AntdUI.Button btnBack;
    private AntdUI.Button btnHome;
    private AntdUI.Button btnRecent;
    private AntdUI.Divider dividerControls;
    private AntdUI.Label labelGroupSystem;
    private AntdUI.Button btnPower;
    private AntdUI.Button btnVolumeUp;
    private AntdUI.Button btnVolumeDown;
    private AntdUI.Label labelStatus;
}
