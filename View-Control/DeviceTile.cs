using LamToolAutoPhonePrime;
using ScrcpyNet;
using Sunny.Subdy.Data.Models;

namespace ViewControl;

/// <summary>
/// Wrapper Panel cho mỗi device trong multi-view.
/// Hiện placeholder xám khi đang connect, replace bằng ucDeviceView khi done.
/// </summary>
public class DeviceTile : Panel
{
    public DeviceModel Device { get; }
    public bool IsConnected { get; private set; }

    private readonly Label labelStatus;

    public DeviceTile(DeviceModel device, int size)
    {
        Device = device;
        BackColor = Color.FromArgb(210, 210, 210);

        labelStatus = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(90, 90, 90),
            Text = $"{device.Serial}\nĐang kết nối...",
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(labelStatus);

        SetSize(size);
    }

    public void SetSize(int height)
    {
        int width = (int)(height * 9.0 / 16.0);
        Size = new Size(width, height);
    }

    /// <summary>
    /// Gọi từ UI thread sau khi scrcpy.Start() thành công.
    /// Replace placeholder bằng ucDeviceView thực.
    /// </summary>
    public void AttachScrcpy(Scrcpy scrcpy)
    {
        Controls.Remove(labelStatus);
        labelStatus.Dispose();

        var uc = new ucDeviceView(Device, true, Device.Serial, scrcpy)
        {
            Dock = DockStyle.Fill
        };
        Controls.Add(uc);
        IsConnected = true;
    }

    public void SetError(string msg)
    {
        if (IsHandleCreated && !IsDisposed)
            Invoke(() =>
            {
                labelStatus.ForeColor = Color.FromArgb(180, 50, 50);
                labelStatus.Text = $"{Device.Serial}\n❌ {msg}";
            });
    }
}
