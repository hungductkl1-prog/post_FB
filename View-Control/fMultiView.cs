using LamToolAutoPhonePrime;
using ScrcpyNet;
using Sunny.Subdy.Data.Models;
using System.Reflection;

namespace ViewControl;

public partial class fMultiView : AntdUI.Window
{
    private readonly ScrcpyManager manager = new();
    private readonly List<DeviceModel> devices;

    public bool SyncEnabled { get; private set; }

    public fMultiView(List<DeviceModel> devices)
    {
        this.devices = devices;
        InitializeComponent();
        TryEnableDoubleBuffering(flowLayout);
        this.Load += OnLoad;
        this.FormClosed += OnFormClosed;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        AppBranding.ApplyTo(this, windowBar);
        windowBar.Text = $"Golike Android View - 0/{devices.Count} thiết bị";

        // Step 1: Add tất cả tile ngay lập tức (optimistic rendering)
        // Mỗi tile hiện placeholder — connect async sau
        foreach (var device in devices)
        {
            var tile = new DeviceTile(device, sliderSize.Value);
            tile.NavKeyClicked += OnTileNavKeyClicked;
            tile.UserTouchOnTile += OnTileUserTouch;
            tile.UserScrollOnTile += OnTileUserScroll;
            tile.UserKeyOnTile += OnTileUserKey;
            flowLayout.Controls.Add(tile);
        }

        // Step 2: Connect từng device song song trên background
        _ = ConnectAllAsync();
    }

    private Task ConnectAllAsync()
    {
        var tiles = flowLayout.Controls.OfType<DeviceTile>().ToList();
        return Task.WhenAll(tiles.Select(tile => ConnectTileAsync(tile)));
    }

    private async Task ConnectTileAsync(DeviceTile tile)
    {
        var serial = tile.Device.Serial;
        try
        {
            Program.LogLine($"[{serial}] ConnectTile begin");
            // Tạo + Start scrcpy hoàn toàn trên thread pool
            var scrcpy = await Task.Run(() =>
            {
                try
                {
                    var s = manager.CreateForSerial(serial);
                    if (s == null)
                    {
                        Program.LogLine($"[{serial}] CreateForSerial returned null (device not found in adb?)");
                        return null;
                    }
                    Program.LogLine($"[{serial}] CreateForSerial OK, calling StartCreated...");
                    manager.StartCreated(serial); // blocking 15s max
                    Program.LogLine($"[{serial}] StartCreated done. Width={s.Width} Height={s.Height} Connected={s.Connected}");
                    return s;
                }
                catch (Exception ex)
                {
                    Program.LogLine($"[{serial}] StartCreated EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                    throw;
                }
            }).ConfigureAwait(false);

            if (scrcpy == null)
            {
                tile.SetError("Không tìm thấy thiết bị trong ADB");
                return;
            }
            if (!IsHandleCreated || IsDisposed) return;

            // Connect xong → attach scrcpy vào tile trên UI thread
            Invoke((System.Windows.Forms.MethodInvoker)delegate
            {
                if (IsDisposed || tile.IsDisposed) return;
                tile.AttachScrcpy(scrcpy);
                tile.SetOverlayAlpha(sliderOpacity.Value);
                int done = flowLayout.Controls.OfType<DeviceTile>()
                                              .Count(t => t.IsConnected);
                windowBar.Text = $"Golike Android View - {done}/{devices.Count} thiết bị";
                Program.LogLine($"[{serial}] AttachScrcpy done. {done}/{devices.Count} tiles connected");
            });
        }
        catch (Exception ex)
        {
            Program.LogLine($"[{serial}] ConnectTile EXCEPTION: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            tile.SetError(LocalizeError(ex.Message));
        }
    }

    private static string LocalizeError(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Lỗi không xác định";
        if (raw.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("device not found", StringComparison.OrdinalIgnoreCase))
            return "Không tìm thấy thiết bị";
        if (raw.Contains("Timeout while waiting for server", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
            return "Hết thời gian chờ kết nối tới scrcpy server";
        if (raw.Contains("An error occurred while reading a response from ADB", StringComparison.OrdinalIgnoreCase))
            return "Lỗi khi đọc phản hồi từ ADB";
        if (raw.Contains("offline", StringComparison.OrdinalIgnoreCase))
            return "Thiết bị đang offline";
        if (raw.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
            return "Thiết bị chưa cấp quyền USB debug";
        if (raw.Contains("Connection refused", StringComparison.OrdinalIgnoreCase))
            return "Kết nối bị từ chối";
        return raw;
    }

    private void OnTileNavKeyClicked(object? sender, AndroidKeycode keycode)
    {
        if (!SyncEnabled || sender is not DeviceTile origin) return;
        foreach (var t in flowLayout.Controls.OfType<DeviceTile>())
        {
            if (ReferenceEquals(t, origin)) continue;
            t.View?.SendKeycode(keycode);
        }
    }

    private void OnTileUserTouch(DeviceTile origin, LamToolAutoPhonePrime.ucDeviceView.NormalizedTouch touch)
    {
        if (!SyncEnabled) return;
        foreach (var t in flowLayout.Controls.OfType<DeviceTile>())
        {
            if (ReferenceEquals(t, origin)) continue;
            t.View?.ReplayTouch(touch);
        }
    }

    private void OnTileUserScroll(DeviceTile origin, LamToolAutoPhonePrime.ucDeviceView.NormalizedScroll scroll)
    {
        if (!SyncEnabled) return;
        foreach (var t in flowLayout.Controls.OfType<DeviceTile>())
        {
            if (ReferenceEquals(t, origin)) continue;
            t.View?.ReplayScroll(scroll);
        }
    }

    private void OnTileUserKey(DeviceTile origin, ScrcpyNet.KeycodeControlMessage key)
    {
        if (!SyncEnabled) return;
        foreach (var t in flowLayout.Controls.OfType<DeviceTile>())
        {
            if (ReferenceEquals(t, origin)) continue;
            t.View?.ReplayKey(key);
        }
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        manager.StopAll();
    }

    private void SetRenderSize(int size)
    {
        foreach (Control c in flowLayout.Controls)
        {
            if (c is DeviceTile tile)
                tile.SetSize(size);
        }
        flowLayout.Refresh();
    }

    private void sliderSize_ValueChanged(object sender, AntdUI.IntEventArgs e)
    {
        SetRenderSize(e.Value);
    }

    private void sliderOpacity_ValueChanged(object sender, AntdUI.IntEventArgs e)
    {
        foreach (var t in flowLayout.Controls.OfType<DeviceTile>())
            t.SetOverlayAlpha(e.Value);
    }

    private void switchSync_CheckedChanged(object sender, AntdUI.BoolEventArgs e)
    {
        SyncEnabled = e.Value;
        int connected = flowLayout.Controls.OfType<DeviceTile>().Count(t => t.IsConnected);
        windowBar.Text = $"Golike Android View - {connected}/{devices.Count} thiết bị" + (SyncEnabled ? "  •  Đồng bộ BẬT" : "");
    }

    [System.Diagnostics.CodeAnalysis.DynamicDependency("DoubleBuffered", typeof(Control))]
    private static void TryEnableDoubleBuffering(Control ctrl)
    {
        if (ctrl == null) return;
        try
        {
            var prop = typeof(Control).GetProperty("DoubleBuffered",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            prop?.SetValue(ctrl, true);
        }
        catch { }
    }
}
