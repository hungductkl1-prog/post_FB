using LamToolAutoPhonePrime;
using ScrcpyNet;
using Sunny.Subdy.Data.Models;
using Point = System.Drawing.Point;

namespace ViewControl;

/// <summary>
/// Wrapper Panel cho mỗi device trong multi-view.
/// Bố cục: tile = stack header (deviceId chip) + body (ucDeviceView khi connected,
/// label trạng thái khi đang connect/lỗi) + footer (3 nút Back/Home/Switch).
/// </summary>
public class DeviceTile : Panel
{
    public DeviceModel Device { get; }
    public bool IsConnected => uc != null;
    public ucDeviceView? View => uc;

    private readonly Label labelStatus;
    private readonly Panel bodyPanel;
    private readonly Panel footerPanel;
    private readonly AntdUI.Button btnBack;
    private readonly AntdUI.Button btnHome;
    private readonly AntdUI.Button btnSwitch;
    private ucDeviceView? uc;

    /// <summary>
    /// Fire khi user click 3 button nav (Back/Home/Switch) trên tile.
    /// fMultiView lắng nghe event này để broadcast keycode khi sync enabled.
    /// </summary>
    public event EventHandler<AndroidKeycode>? NavKeyClicked;

    /// <summary>
    /// Fire khi user touch/scroll/key trên ucDeviceView của tile này.
    /// fMultiView replay sang tile khác khi sync ON.
    /// </summary>
    public event Action<DeviceTile, ucDeviceView.NormalizedTouch>? UserTouchOnTile;
    public event Action<DeviceTile, ucDeviceView.NormalizedScroll>? UserScrollOnTile;
    public event Action<DeviceTile, ScrcpyNet.KeycodeControlMessage>? UserKeyOnTile;

    private const int FooterHeight = 36;

    public DeviceTile(DeviceModel device, int size)
    {
        Device = device;
        BackColor = Color.FromArgb(245, 245, 245);
        Padding = new Padding(0);
        Margin = new Padding(6);

        // Footer (Back/Home/Switch) — Dock Bottom, AntdUI buttons
        footerPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = FooterHeight,
            BackColor = Color.FromArgb(40, 40, 40)
        };

        btnBack = MakeNavButton("ArrowLeftOutlined");
        btnHome = MakeNavButton("HomeOutlined");
        btnSwitch = MakeNavButton("AppstoreOutlined");

        btnBack.Click += (s, e) => SendNavKey(AndroidKeycode.AKEYCODE_BACK);
        btnHome.Click += (s, e) => SendNavKey(AndroidKeycode.AKEYCODE_HOME);
        btnSwitch.Click += (s, e) => SendNavKey(AndroidKeycode.AKEYCODE_APP_SWITCH);

        footerPanel.Controls.Add(btnSwitch);
        footerPanel.Controls.Add(btnHome);
        footerPanel.Controls.Add(btnBack);
        footerPanel.Resize += (s, e) => LayoutFooter();

        // Body — Dock Fill, chứa ucDeviceView hoặc placeholder
        bodyPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(225, 225, 225)
        };

        labelStatus = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(90, 90, 90),
            Text = $"{device.Serial}\nĐang kết nối...",
            TextAlign = ContentAlignment.MiddleCenter
        };
        bodyPanel.Controls.Add(labelStatus);

        Controls.Add(bodyPanel);
        Controls.Add(footerPanel);

        SetSize(size);
        LayoutFooter();
    }

    private static AntdUI.Button MakeNavButton(string iconSvg)
    {
        return new AntdUI.Button
        {
            Type = AntdUI.TTypeMini.Default,
            BackColor = Color.FromArgb(40, 40, 40),
            DefaultBack = Color.FromArgb(40, 40, 40),
            BackHover = Color.FromArgb(70, 70, 70),
            BackActive = Color.FromArgb(90, 90, 90),
            DefaultBorderColor = Color.Transparent,
            BorderWidth = 0,
            ForeColor = Color.White,
            IconSvg = iconSvg,
            Cursor = Cursors.Hand,
            Radius = 6,
            WaveSize = 0,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
    }

    private void LayoutFooter()
    {
        int w = footerPanel.ClientSize.Width;
        if (w <= 0) return;
        int h = footerPanel.ClientSize.Height;
        int btnH = h - 8;
        int btnW = Math.Min(60, (w - 16) / 3);
        int gap = (w - btnW * 3) / 4;
        int y = (h - btnH) / 2;

        btnBack.Size = new Size(btnW, btnH);
        btnHome.Size = new Size(btnW, btnH);
        btnSwitch.Size = new Size(btnW, btnH);

        btnBack.Location = new Point(gap, y);
        btnHome.Location = new Point(gap * 2 + btnW, y);
        btnSwitch.Location = new Point(gap * 3 + btnW * 2, y);
    }

    public void SetSize(int height)
    {
        int videoH = height;
        int width = (int)(videoH * 9.0 / 16.0);
        Size = new Size(width, videoH + FooterHeight);
    }

    public void SetOverlayAlpha(int alpha) => uc?.SetOverlayAlpha(alpha);

    private void SendNavKey(AndroidKeycode keycode)
    {
        uc?.SendKeycode(keycode);
        NavKeyClicked?.Invoke(this, keycode);
    }

    /// <summary>
    /// Gọi từ UI thread sau khi scrcpy.Start() thành công.
    /// Replace placeholder bằng ucDeviceView thực.
    /// </summary>
    public void AttachScrcpy(Scrcpy scrcpy)
    {
        bodyPanel.Controls.Remove(labelStatus);
        labelStatus.Dispose();

        uc = new ucDeviceView(Device, true, "", scrcpy)
        {
            Dock = DockStyle.Fill
        };
        uc.UserTouch += t => UserTouchOnTile?.Invoke(this, t);
        uc.UserScroll += s => UserScrollOnTile?.Invoke(this, s);
        uc.UserKey += k => UserKeyOnTile?.Invoke(this, k);
        bodyPanel.Controls.Add(uc);
    }

    public void SetError(string msg)
    {
        if (IsHandleCreated && !IsDisposed)
            Invoke(() =>
            {
                if (labelStatus.IsDisposed) return;
                labelStatus.ForeColor = Color.FromArgb(180, 50, 50);
                labelStatus.Text = $"{Device.Serial}\nX {msg}";
            });
    }
}
