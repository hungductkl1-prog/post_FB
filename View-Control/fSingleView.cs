using LamToolAutoPhonePrime;
using ScrcpyNet;
using Sunny.Subdy.Data.Models;

namespace ViewControl;

public partial class fSingleView : AntdUI.Window
{
    private readonly ScrcpyManager manager = new();
    private readonly DeviceModel device;
    private Scrcpy? scrcpy;

    public fSingleView(DeviceModel device)
    {
        this.device = device;
        InitializeComponent();
        this.Load += OnLoad;
        this.FormClosed += OnFormClosed;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        AppBranding.ApplyTo(this, windowBar);
        windowBar.Text = $"QN Android View - {device.Serial}";
        labelStatus.Text = "Đang kết nối...";
        _ = ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        try
        {
            // Tạo + Start scrcpy hoàn toàn trên thread pool
            // Chỉ add UI sau khi TCP connect thành công
            var s = await Task.Run(() =>
            {
                var sc = manager.CreateForSerial(device.Serial);
                if (sc == null) return null;
                manager.StartCreated(device.Serial); // blocking
                return sc;
            }).ConfigureAwait(false);

            if (s == null)
            {
                SetStatus("Không tìm thấy thiết bị trong ADB.");
                return;
            }
            if (!IsHandleCreated || IsDisposed) return;

            // Add ucDeviceView lên UI thread sau khi connect xong
            Invoke((System.Windows.Forms.MethodInvoker)delegate
            {
                if (IsDisposed) return;
                scrcpy = s;
                labelStatus.Visible = false;
                var uc = new ucDeviceView(device, false, device.Serial, s) { Dock = DockStyle.Fill };
                panelDevice.Controls.Add(uc);
            });
        }
        catch (Exception ex)
        {
            SetStatus($"Lỗi kết nối: {ex.Message}");
        }
    }

    private void SetStatus(string msg)
    {
        if (IsHandleCreated && !IsDisposed)
            Invoke(() => { labelStatus.Visible = true; labelStatus.Text = msg; });
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        manager.StopAll();
    }

    private void SendKey(AndroidKeycode keycode)
    {
        if (scrcpy == null) return;
        try
        {
            scrcpy.SendControlCommand(new KeycodeControlMessage
            {
                Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_DOWN,
                KeyCode = keycode
            });
            scrcpy.SendControlCommand(new KeycodeControlMessage
            {
                Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_UP,
                KeyCode = keycode
            });
        }
        catch { }
    }

    private void btnBack_Click(object sender, EventArgs e) => SendKey(AndroidKeycode.AKEYCODE_BACK);
    private void btnHome_Click(object sender, EventArgs e) => SendKey(AndroidKeycode.AKEYCODE_HOME);
    private void btnRecent_Click(object sender, EventArgs e) => SendKey(AndroidKeycode.AKEYCODE_APP_SWITCH);
    private void btnPower_Click(object sender, EventArgs e) => SendKey(AndroidKeycode.AKEYCODE_POWER);
    private void btnVolumeUp_Click(object sender, EventArgs e) => SendKey(AndroidKeycode.AKEYCODE_VOLUME_UP);
    private void btnVolumeDown_Click(object sender, EventArgs e) => SendKey(AndroidKeycode.AKEYCODE_VOLUME_DOWN);
}
