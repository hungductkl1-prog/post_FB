using LamToolAutoPhonePrime;
using Sunny.Subdy.Data.Models;
using System.Reflection;

namespace ViewControl;

public partial class fMultiView : AntdUI.Window
{
    private readonly ScrcpyManager manager = new();
    private readonly List<DeviceModel> devices;

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
        windowBar.Text = $"Android View - 0/{devices.Count} devices";

        // Step 1: Add tất cả tile ngay lập tức (optimistic rendering)
        // Mỗi tile hiện placeholder — connect async sau
        foreach (var device in devices)
        {
            var tile = new DeviceTile(device, sliderSize.Value);
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
        try
        {
            // Tạo + Start scrcpy hoàn toàn trên thread pool
            var scrcpy = await Task.Run(() =>
            {
                var s = manager.CreateForSerial(tile.Device.Serial);
                if (s == null) return null;
                manager.StartCreated(tile.Device.Serial); // blocking 15s max
                return s;
            }).ConfigureAwait(false);

            if (scrcpy == null)
            {
                tile.SetError("Không tìm thấy trong ADB");
                return;
            }
            if (!IsHandleCreated || IsDisposed) return;

            // Connect xong → attach scrcpy vào tile trên UI thread
            Invoke((System.Windows.Forms.MethodInvoker)delegate
            {
                if (IsDisposed || tile.IsDisposed) return;
                tile.AttachScrcpy(scrcpy);
                int done = flowLayout.Controls.OfType<DeviceTile>()
                                              .Count(t => t.IsConnected);
                windowBar.Text = $"Android View - {done}/{devices.Count} devices";
            });
        }
        catch (Exception ex)
        {
            tile.SetError(ex.Message);
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
