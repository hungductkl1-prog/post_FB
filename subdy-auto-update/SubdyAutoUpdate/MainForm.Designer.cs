namespace SubdyAutoUpdate;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        panelRoot      = new System.Windows.Forms.Panel();
        panelHeader    = new System.Windows.Forms.Panel();
        picLogo        = new System.Windows.Forms.PictureBox();
        panelHeaderText = new System.Windows.Forms.Panel();
        lblAppName     = new AntdUI.Label();
        lblSubtitle    = new AntdUI.Label();
        btnClose       = new AntdUI.Button();
        panelDivTop    = new System.Windows.Forms.Panel();
        panelCenter    = new System.Windows.Forms.Panel();
        steps          = new AntdUI.Steps();
        panelBottom    = new System.Windows.Forms.Panel();
        progressBar    = new AntdUI.Progress();
        lblStatus      = new AntdUI.Label();
        lblVersion     = new AntdUI.Label();

        panelRoot.SuspendLayout();
        panelHeader.SuspendLayout();
        panelHeaderText.SuspendLayout();
        panelCenter.SuspendLayout();
        panelBottom.SuspendLayout();
        SuspendLayout();

        // ── panelRoot (nền toàn form) ──────────────────────────────────────
        panelRoot.Dock = DockStyle.Fill;
        panelRoot.BackColor = Color.FromArgb(18, 18, 24);
        panelRoot.Controls.Add(panelCenter);
        panelRoot.Controls.Add(panelDivTop);
        panelRoot.Controls.Add(panelHeader);
        panelRoot.Controls.Add(panelBottom);

        // ── panelHeader ────────────────────────────────────────────────────
        panelHeader.Dock = DockStyle.Top;
        panelHeader.BackColor = Color.Transparent;
        panelHeader.Height = 80;
        panelHeader.Padding = new Padding(16, 14, 12, 10);
        panelHeader.Controls.Add(panelHeaderText);
        panelHeader.Controls.Add(picLogo);
        panelHeader.Controls.Add(btnClose);

        // ── picLogo ────────────────────────────────────────────────────────
        picLogo.Dock = DockStyle.Left;
        picLogo.Width = 96;
        picLogo.SizeMode = PictureBoxSizeMode.Zoom;
        picLogo.BackColor = Color.Transparent;

        // ── panelHeaderText (title + subtitle) ────────────────────────────
        panelHeaderText.Dock = DockStyle.Fill;
        panelHeaderText.BackColor = Color.Transparent;
        panelHeaderText.Padding = new Padding(10, 0, 0, 0);
        panelHeaderText.Controls.Add(lblSubtitle);
        panelHeaderText.Controls.Add(lblAppName);

        // ── lblAppName ─────────────────────────────────────────────────────
        lblAppName.Dock = DockStyle.Top;
        lblAppName.AutoSize = false;
        lblAppName.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
        lblAppName.ForeColor = Color.White;
        lblAppName.Text = "QN Auto Update";
        lblAppName.TextAlign = ContentAlignment.BottomLeft;
        lblAppName.Height = 32;

        // ── lblSubtitle ────────────────────────────────────────────────────
        lblSubtitle.Dock = DockStyle.Top;
        lblSubtitle.AutoSize = false;
        lblSubtitle.Font = new Font("Segoe UI", 8.5F);
        lblSubtitle.ForeColor = Color.FromArgb(130, 130, 150);
        lblSubtitle.Text = "QNPhoneFarm — Cập nhật tự động";
        lblSubtitle.TextAlign = ContentAlignment.TopLeft;
        lblSubtitle.Height = 18;

        // ── btnClose ───────────────────────────────────────────────────────
        btnClose.Dock = DockStyle.Right;
        btnClose.Text = "✕";
        btnClose.Font = new Font("Segoe UI", 11F);
        btnClose.Type = AntdUI.TTypeMini.Error;
        btnClose.Ghost = true;
        btnClose.Width = 36;
        btnClose.Cursor = Cursors.Hand;
        btnClose.Click += (_, _) => Application.Exit();

        // ── panelDivTop (đường kẻ ngang) ──────────────────────────────────
        panelDivTop.Dock = DockStyle.Top;
        panelDivTop.BackColor = Color.FromArgb(38, 38, 50);
        panelDivTop.Height = 1;

        // ── panelCenter (Steps) ────────────────────────────────────────────
        panelCenter.Dock = DockStyle.Fill;
        panelCenter.BackColor = Color.Transparent;
        panelCenter.Padding = new Padding(28, 16, 28, 0);
        panelCenter.Controls.Add(steps);

        // ── steps ──────────────────────────────────────────────────────────
        steps.Dock = DockStyle.Fill;
        steps.Vertical = true;
        steps.Current = 0;
        steps.Font = new Font("Segoe UI", 10F);
        steps.ForeColor = Color.FromArgb(200, 200, 215);
        steps.BackColor = Color.Transparent;
        steps.Items.Add(new AntdUI.StepsItem("Dừng phần mềm",    "Kiểm tra và tắt QNPhoneFarm"));
        steps.Items.Add(new AntdUI.StepsItem("Kiểm tra phiên bản", "Kết nối máy chủ cập nhật"));
        steps.Items.Add(new AntdUI.StepsItem("Tải xuống",          "Đang tải bản cập nhật mới"));
        steps.Items.Add(new AntdUI.StepsItem("Cài đặt",            "Cài đặt và khởi động lại"));

        // ── panelBottom ────────────────────────────────────────────────────
        panelBottom.Dock = DockStyle.Bottom;
        panelBottom.BackColor = Color.FromArgb(24, 24, 32);
        panelBottom.Height = 80;
        panelBottom.Padding = new Padding(28, 10, 28, 10);
        panelBottom.Controls.Add(lblVersion);
        panelBottom.Controls.Add(lblStatus);
        panelBottom.Controls.Add(progressBar);

        // ── progressBar ────────────────────────────────────────────────────
        progressBar.Dock = DockStyle.Top;
        progressBar.Shape = AntdUI.TShapeProgress.Round;
        progressBar.Loading = true;
        progressBar.Height = 14;
        progressBar.Radius = 6;

        // ── lblStatus ──────────────────────────────────────────────────────
        lblStatus.Dock = DockStyle.Top;
        lblStatus.AutoSize = false;
        lblStatus.Font = new Font("Segoe UI", 9F);
        lblStatus.ForeColor = Color.FromArgb(160, 160, 180);
        lblStatus.Text = "Đang khởi động...";
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        lblStatus.Height = 22;

        // ── lblVersion ─────────────────────────────────────────────────────
        lblVersion.Dock = DockStyle.Bottom;
        lblVersion.AutoSize = false;
        lblVersion.Font = new Font("Segoe UI", 8.5F);
        lblVersion.ForeColor = Color.FromArgb(90, 90, 110);
        lblVersion.Text = string.Empty;
        lblVersion.TextAlign = ContentAlignment.MiddleRight;
        lblVersion.Height = 18;

        // ── MainForm ───────────────────────────────────────────────────────
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(440, 380);
        FormBorderStyle = FormBorderStyle.None;
        MaximumSize = new Size(440, 380);
        MinimumSize = new Size(440, 380);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "QN Auto Update";
        BackColor = Color.FromArgb(18, 18, 24);

        Controls.Add(panelRoot);

        panelBottom.ResumeLayout(false);
        panelCenter.ResumeLayout(false);
        panelHeaderText.ResumeLayout(false);
        panelHeader.ResumeLayout(false);
        panelRoot.ResumeLayout(false);
        ResumeLayout(false);
    }

    private System.Windows.Forms.Panel      panelRoot;
    private System.Windows.Forms.Panel      panelHeader;
    private System.Windows.Forms.Panel      panelHeaderText;
    private System.Windows.Forms.PictureBox picLogo;
    private System.Windows.Forms.Panel      panelDivTop;
    private System.Windows.Forms.Panel      panelCenter;
    private System.Windows.Forms.Panel      panelBottom;
    private AntdUI.Label    lblAppName;
    private AntdUI.Label    lblSubtitle;
    private AntdUI.Button   btnClose;
    private AntdUI.Steps    steps;
    private AntdUI.Progress progressBar;
    private AntdUI.Label    lblStatus;
    private AntdUI.Label    lblVersion;
}
