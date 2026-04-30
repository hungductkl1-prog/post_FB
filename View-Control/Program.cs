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
    private static readonly string LogFile = Path.Combine(AppContext.BaseDirectory, "ViewControl.log");

    public static void LogLine(string msg)
    {
        try
        {
            File.AppendAllText(LogFile, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}{Environment.NewLine}");
        }
        catch { }
    }

    [STAThread]
    static void Main(string[] args)
    {
        // MUST be first: re-enable WinForms data binding before any WinForms type is loaded
        AppContext.SetSwitch("System.Windows.Forms.Binding.IsSupported", true);

        // Catch & log mọi unhandled exception (cả UI thread và background)
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            LogLine($"[UNHANDLED] {e.ExceptionObject}");
        };
        Application.ThreadException += (s, e) =>
        {
            LogLine($"[UI-THREAD-EX] {e.Exception}");
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            LogLine($"[TASK-EX] {e.Exception}");
            e.SetObserved();
        };

        // Reset log mỗi lần mở để dễ đọc
        try { File.WriteAllText(LogFile, $"=== ViewControl started {DateTime.Now} args=[{string.Join(",", args)}] ===\n"); } catch { }

        // Pipe Debug.WriteLine / Trace.WriteLine vào log file để bắt log từ ucDeviceView/ScrcpyManager
        try
        {
            var writer = new System.IO.StreamWriter(LogFile, append: true) { AutoFlush = true };
            System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(writer));
            System.Diagnostics.Trace.AutoFlush = true;
        }
        catch { }

        // WinFormsComInterop chỉ cần thiết khi NativeAOT (JIT runtime đã có sẵn COM marshalling).
        // Trên .NET 10 + single-file (không AOT), WinFormsComInterop 0.5.0 throw TypeLoadException
        // trong WinFormsComWrappers.CreatePrimitivesIServiceProviderProxyVtbl → app crash silent.
        if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)
        {
            try
            {
                System.Runtime.InteropServices.ComWrappers.RegisterForMarshalling(WinFormsComInterop.WinFormsComWrappers.Instance);
            }
            catch (Exception ex)
            {
                LogLine($"[WinFormsComInterop] register fail (bỏ qua, JIT runtime fallback): {ex.GetType().Name}: {ex.Message}");
            }
        }
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        // Init ADB server
        try
        {
            AdbServer.Instance.StartServer(Path.Combine(ProcessHelper.ADBPath, "adb.exe"), false);
            LogLine($"ADB server started from {ProcessHelper.ADBPath}");
        }
        catch (Exception ex)
        {
            LogLine($"ADB start warning: {ex.Message}");
        }

        // Init FFmpeg path (ScrcpyNet) — đảm bảo folder + native deps tồn tại trước
        ffmpeg.RootPath = LamToolAutoPhonePrime.Utils.ScrcpyNetFolderEnsurer.EnsureScrcpyNetFolder();
        LogLine($"FFmpeg root: {ffmpeg.RootPath}");

        // Đảm bảo scrcpy-server.jar tồn tại — nếu thiếu sẽ auto tải từ GitHub
        if (!ScrcpyServerDownloader.EnsureScrcpyServerJar())
        {
            MessageBox.Show(
                "Không thể tải scrcpy-server.jar.\nVui lòng kiểm tra kết nối mạng rồi thử lại.",
                "Golike Android View",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        // Parse device IDs: support both space-separated args and comma-separated values
        var deviceIds = args
            .SelectMany(a => a.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToList();

        if (deviceIds.Count == 0)
        {
            MessageBox.Show(
                "Cách dùng:\n  Golike-Android-View.exe <mã thiết bị>\n  Golike-Android-View.exe <mã thiết bị 1>,<mã thiết bị 2>,...\n  Golike-Android-View.exe <mã thiết bị 1> <mã thiết bị 2> ...",
                "Golike Android View",
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
