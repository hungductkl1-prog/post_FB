using AutoAndroid;
using FFmpeg.AutoGen;
using LamToolAutoPhonePrime;
using SharpAdbClient;
using Sunny.Subdy.Data.Models;
using System.Runtime.InteropServices;
using ViewControl;

namespace ViewControl;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // MUST be first: re-enable WinForms data binding before any WinForms type is loaded
        AppContext.SetSwitch("System.Windows.Forms.Binding.IsSupported", true);

        System.Runtime.InteropServices.ComWrappers.RegisterForMarshalling(WinFormsComInterop.WinFormsComWrappers.Instance);
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        // Init ADB server
        try
        {
            AdbServer.Instance.StartServer(Path.Combine(ProcessHelper.ADBPath, "adb.exe"), false);
        }
        catch
        {
            // ADB may already be running or path not found — continue
        }

        // Init FFmpeg path (ScrcpyNet)
        ffmpeg.RootPath = Path.Combine(AppContext.BaseDirectory, "ScrcpyNet");

        // Parse device IDs: support both space-separated args and comma-separated values
        var deviceIds = args
            .SelectMany(a => a.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToList();

        if (deviceIds.Count == 0)
        {
            MessageBox.Show(
                "Usage:\n  ViewControl.exe <deviceId>\n  ViewControl.exe <deviceId1>,<deviceId2>,...\n  ViewControl.exe <deviceId1> <deviceId2> ...",
                "ViewControl - Android View",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var devices = deviceIds
            .Select(id => new DeviceModel { Serial = id })
            .ToList();

        if (devices.Count == 1)
            Application.Run(new fSingleView(devices[0]));
        else
            Application.Run(new fMultiView(devices));
    }
}
