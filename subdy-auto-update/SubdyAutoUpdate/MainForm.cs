using AntdUI;
using DeviceId;
using DeviceId.Windows;

namespace SubdyAutoUpdate;

public partial class MainForm : AntdUI.Window
{
    private string _remoteVersion = string.Empty;
    private string _updateUrl     = string.Empty;
    private string _localVersion  = string.Empty;

    // Step index
    private const int STEP_KILL     = 0;
    private const int STEP_CHECK    = 1;
    private const int STEP_DOWNLOAD = 2;
    private const int STEP_INSTALL  = 3;

    public MainForm()
    {
        InitializeComponent();
        this.Load += MainForm_Load;
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        LoadLogo();
        _ = RunUpdateFlowAsync();
    }

    private void LoadLogo()
    {
        try
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("SubdyAutoUpdate.Resources.logo.png");
            if (stream is not null)
                picLogo.Image = Image.FromStream(stream);
        }
        catch { }
    }

    // ── Main auto flow ──────────────────────────────────────────────────────

    private async Task RunUpdateFlowAsync()
    {
        try
        {
            // ── Step 0: Kill ────────────────────────────────────────────────
            GoStep(STEP_KILL, "Đang dừng QNPhoneFarm...", -1);
            await Task.Run(UpdateService.KillMainProcessIfRunning).ConfigureAwait(false);
            await Task.Delay(400).ConfigureAwait(false);
            FinishStep(STEP_KILL);

            // ── Step 1: Check version ───────────────────────────────────────
            GoStep(STEP_CHECK, "Đang kiểm tra phiên bản...", -1);
            _localVersion = UpdateService.GetLocalVersion();

            string hwid = new DeviceIdBuilder()
                .OnWindows(w => w.AddWindowsDeviceId())
                .ToString();

            (_remoteVersion, _updateUrl) = await LamToolApiClient.GetUpdateInfoAsync(hwid).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(_remoteVersion) || string.IsNullOrWhiteSpace(_updateUrl))
            {
                ErrorStep(STEP_CHECK, "Không thể kết nối máy chủ");
                ShowMsg(false, "Không thể kết nối tới máy chủ qn.net.\nKiểm tra kết nối mạng và thử lại.");
                return;
            }

            SetVersionLabel(_localVersion, _remoteVersion);
            FinishStep(STEP_CHECK);

            if (!LamToolApiClient.IsNewerVersion(_localVersion, _remoteVersion))
            {
                SetStatus("Phần mềm đã là phiên bản mới nhất.", 100);
                // Đánh dấu tất cả hoàn thành
                FinishStep(STEP_DOWNLOAD);
                FinishStep(STEP_INSTALL);
                ShowMsg(true, $"Phiên bản {_remoteVersion} là mới nhất. Không cần cập nhật.");
                return;
            }

            // ── Step 2: Download ────────────────────────────────────────────
            GoStep(STEP_DOWNLOAD, "Đang tải bản cập nhật...", 0);

            string zipDir  = Path.Combine(Path.GetTempPath(), "SubdyUpdate");
            string zipPath = Path.Combine(zipDir, "update.zip");

            await UpdateService.DownloadFileAsync(
                _updateUrl,
                zipPath,
                (pct, text) => SetStatus(text, pct)
            ).ConfigureAwait(false);

            FinishStep(STEP_DOWNLOAD);

            // ── Step 3: Install ─────────────────────────────────────────────
            GoStep(STEP_INSTALL, "Đang cài đặt cập nhật...", 100);
            await Task.Delay(500).ConfigureAwait(false);

            UpdateService.CreateUpdateBatAndExit(zipPath, _localVersion);
        }
        catch (Exception ex)
        {
            SetStatus("Lỗi: " + ex.Message, 0);
            ShowMsg(false, "Cập nhật thất bại:\n" + ex.Message);
        }
    }

    // ── Steps helpers ───────────────────────────────────────────────────────

    /// <summary>Chuyển sang step, set trạng thái Process, cập nhật status text và progress.</summary>
    private void GoStep(int step, string statusText, int percent)
    {
        if (InvokeRequired) { Invoke(() => GoStep(step, statusText, percent)); return; }
        steps.Current = step;
        steps.Status  = AntdUI.TStepState.Process;
        SetStatus(statusText, percent);
    }

    private void FinishStep(int step)
    {
        if (InvokeRequired) { Invoke(() => FinishStep(step)); return; }
        // Đánh dấu step vừa xong → chuyển sang step tiếp theo ở trạng thái Wait
        steps.Current = step + 1;
        steps.Status  = AntdUI.TStepState.Wait;
    }

    private void ErrorStep(int step, string statusText)
    {
        if (InvokeRequired) { Invoke(() => ErrorStep(step, statusText)); return; }
        steps.Current = step;
        steps.Status  = AntdUI.TStepState.Error;
        SetStatus(statusText, 0);
    }

    // ── UI helpers ──────────────────────────────────────────────────────────

    private void SetStatus(string text, int percent)
    {
        if (InvokeRequired) { Invoke(() => SetStatus(text, percent)); return; }

        lblStatus.Text = text;

        if (percent < 0)
        {
            progressBar.Loading = true;
            progressBar.Value   = 0;
        }
        else
        {
            progressBar.Loading = false;
            progressBar.Value   = Math.Clamp(percent, 0, 100);
        }
    }

    private void SetVersionLabel(string local, string remote)
    {
        if (InvokeRequired) { Invoke(() => SetVersionLabel(local, remote)); return; }
        lblVersion.Text = string.IsNullOrEmpty(local)
            ? $"v{remote}"
            : $"v{local}  →  v{remote}";
    }

    private void ShowMsg(bool success, string msg)
    {
        if (InvokeRequired) { Invoke(() => ShowMsg(success, msg)); return; }
        if (success) AntdUI.Message.success(this, msg, autoClose: 4);
        else         AntdUI.Message.error(this, msg, autoClose: 6);
    }
}
