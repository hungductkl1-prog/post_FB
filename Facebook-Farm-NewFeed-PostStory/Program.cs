using AutoAndroid;
using DeviceId;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Facebook_Farm_NewFeed_PostStory.Views.Forms;
using Microsoft.Win32;
using Sunny.Subd.Core.Facebook;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace Facebook_Farm_NewFeed_PostStory
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // MUST be first: re-enable WinForms data binding before any WinForms type is loaded
            // Binding.cctor reads this switch once; if set after first access it's too late.
            AppContext.SetSwitch("System.Windows.Forms.Binding.IsSupported", true);

            // ── UTF-8 cho output process con (sửa tiếng Việt bị "?") ─────────────────
            // Status thiết bị/tài khoản như "Kiểm tra tài khoản còn sống..." do thư viện
            // chạy job (AutoAndroid/Sunny.Subd.Core) đọc stdout của process con (node/adb/
            // script). Khi ProcessStartInfo không set StandardOutputEncoding, .NET dùng code
            // page output của console → ký tự có dấu thành "?". Đặt console code page = UTF-8
            // ở đây để các process con đọc output đúng tiếng Việt. Bọc try/catch vì app GUI
            // có thể không có console attach (setter sẽ ném IOException — bỏ qua an toàn).
            try { System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); } catch { }
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }
            try { Console.InputEncoding = System.Text.Encoding.UTF8; } catch { }

            // ── Global crash/exception logging ──────────────────────────────────────
            // Trước đây KHÔNG có handler nào → mọi exception chưa bắt ở thread nền giết
            // tiến trình im lặng (exit 0xffffffff) mà không ghi stack. Gắn handler để:
            //  1) Ghi đầy đủ stack vào logs\crash_*.txt (định vị chính xác lỗi).
            //  2) CatchException: exception trên UI thread KHÔNG làm crash app nữa.
            //  3) Quan sát Task lỗi (UnobservedTaskException) để không kết thúc tiến trình.
            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (_, e) => LogCrash("UI.ThreadException", e.Exception);
                AppDomain.CurrentDomain.UnhandledException += (_, e) => LogCrash("AppDomain.Unhandled", e.ExceptionObject as Exception);
                TaskScheduler.UnobservedTaskException += (_, e) => { LogCrash("UnobservedTask", e.Exception); e.SetObserved(); };
                AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
            }
            catch { }

            // ── Nối dây hook popup TOÀN CỤC (thẻ Meta consent "pay or consent") ────────
            // AutoAndroid KHÔNG reference Sunny.Subd.Core (chiều phụ thuộc ngược lại), nên
            // hai delegate NÀY PHẢI được gán ở đây lúc khởi động — nếu thiếu,
            // RunGlobalPopupInterceptor thấy detector=null và trả XML nguyên trạng, thẻ
            // consent hiện ngoài luồng login sẽ làm 65 vòng lặp FindElement poll vô hạn mà
            // KHÔNG bấm gì, KHÔNG log gì (xác minh LIVE 2026-09-10 trên 520058f34d7c947b:
            // 159 dump/30s, 0 click, activity không đổi). Hook đặt trong
            // ADBClient.GetXMLSource — ĐIỂM THẮT mọi vòng lặp đều đi qua — nên vá được tất
            // cả mà không phải sửa từng chỗ. Detector chỉ đọc chuỗi (rẻ); Interceptor mới
            // tap/swipe và tự chống đệ quy bằng [ThreadStatic] _popupInterceptorBusy.
            ADBClient.GlobalPopupDetector = FacebookHander.IsGlobalPopup;
            ADBClient.GlobalPopupInterceptor = FacebookHander.TryHandleGlobalPopup;

            // Phase 5 grid diagnostics: set ACCOUNT_GRID_DIAG=1 before launch,
            // OR drop an empty marker file named "diag.on" next to the .exe (reliable for
            // non-VS launches where env vars don't propagate).
            bool diagEnv =
                string.Equals(Environment.GetEnvironmentVariable("ACCOUNT_GRID_DIAG"), "1", StringComparison.Ordinal)
                || string.Equals(Environment.GetEnvironmentVariable("ACCOUNT_GRID_DIAG"), "true", StringComparison.OrdinalIgnoreCase);
            bool diagFile = false;
            try { diagFile = File.Exists(Path.Combine(AppContext.BaseDirectory, "diag.on")); } catch { }
            if (diagEnv || diagFile)
            {
                AccountGridPerf.EnableDiagnostics(true);
            }

            // A/B render bypass: set ACCOUNT_GRID_NO_PAINT=1 to disable custom render handlers
            // (custom checkbox painting, CellFormatting, status-badge) and compare FPS/CPU.
            if (string.Equals(Environment.GetEnvironmentVariable("ACCOUNT_GRID_NO_PAINT"), "1", StringComparison.Ordinal)
                || string.Equals(Environment.GetEnvironmentVariable("ACCOUNT_GRID_NO_PAINT"), "true", StringComparison.OrdinalIgnoreCase))
            {
                AccountGridPerf.EnableNoPaintMode(true);
            }
#if DEBUG
            else
            {
                // DEBUG builds: opt-in via env only (no always-on overhead).
            }
#endif

            // WinForms config phải set trước khi tạo splash form (splash là Form thường).
            ComWrappers.RegisterForMarshalling(WinFormsComInterop.WinFormsComWrappers.Instance);
            ApplicationConfiguration.Initialize();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            // Splash hiển thị xuyên suốt giai đoạn kiểm tra môi trường + init.
            // Chạy trong UI thread riêng (STA) để spinner mượt khi main thread block I/O.
            SplashHandle splash = null;
            try { splash = new SplashHandle(); } catch (Exception ex) { Trace.TraceWarning("Splash init failed: " + ex); }

            // Kiểm tra môi trường trước khi khởi động
            try
            {
                splash?.SetStatus("Đang kiểm tra môi trường...");
                if (!IsEnvironmentReady())
                {
                    splash?.SetStatus("Đang cài đặt môi trường (QNHelper)...");
                    RunLTPhoneHelper();

                    // Re-check sau khi helper kết thúc. Nếu vẫn chưa ready
                    // (user huỷ UAC, AppLocker chặn, helper crash...) thì báo
                    // rõ thay vì RestartApp im lặng để rồi loop vô hạn.
                    if (!IsEnvironmentReady())
                    {
                        splash?.Close();
                        MessageBox.Show(
                            "Cài đặt môi trường chưa hoàn tất.\n" +
                            "Vui lòng chạy QNHelper.exe (Right-click > Run as administrator) rồi thử lại.",
                            "Thiếu môi trường",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    splash?.Close();
                    RestartApp();
                    return;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Environment check failed: " + ex);
            }

            splash?.SetStatus("Đang tải cấu hình giao diện...");

            // Wire up SortableBindingList to auto-unregister items from ThrottledPropertyNotifier
            // on removal, preventing stale PropertyChanged events from crashing DataGridView.
            SortableBindingList<DeviceModel>.OnBeforeRemove = ThrottledPropertyNotifier.Unregister;
            SortableBindingList<Account>.OnBeforeRemove = ThrottledPropertyNotifier.Unregister;
            SortableBindingList<JobHistory>.OnBeforeRemove = ThrottledPropertyNotifier.Unregister;

            // Font phải sync vì InitializeComponent của fMain dùng FamilyName ngay khi tạo control.
            splash?.SetStatus("Đang tải font...");
            FontUtil.LoadCustomFonts();

            // Gắn icon QN cho toàn bộ form (cả dialog mở sau).
            AppIconHelper.Install();

            // Background I/O: DeviceId WMI + auto-login HTTP đều chậm nhưng KHÔNG cần
            // xong trước khi fMain hiện. UI thread cứ tạo form, các task này finish async.
            // - DeviceId: chỉ cần trước LicenseCheck (chạy trong fMain.LoadData).
            // - TryAutoLogin: fMain_Load.UpdateUserInfo + LicenseCheck đều đợi via task.
            //
            // ADB: KHÔNG warm-up lúc startup. fMain load lên chưa get devices ngay,
            // việc kill toàn bộ adb + start-server cold rất tốn (~2-3s) và còn làm chết
            // các adb session khác user đang chạy. ADBHelper.GetDevices() đã có
            // EnsureServerStarted() lazy nên lần đầu mở tab Thiết bị mới start-server.
            // Giữ AdbReadyTask = completed task để các await Globals.AdbReadyTask hiện
            // có (fMain.cs trong LoadDevices) vẫn chạy bình thường mà không NPE.
            Globals.AdbReadyTask = Task.CompletedTask;

            Globals.DeviceIdTask = Task.Run(() =>
            {
                try
                {
                    Globals.DeviceId = new DeviceIdBuilder()
                        .OnWindows(windows => windows.AddWindowsDeviceId())
                        .ToString();
                }
                catch (Exception ex) { Trace.TraceWarning("DeviceId build failed: " + ex); }
            });

            Globals.AutoLoginTask = Task.CompletedTask;

            // ── Kiểm tra kích hoạt thiết bị (Google Sheets license) ──────────
            // Đợi DeviceIdTask hoàn tất, mở form kích hoạt nếu thiết bị chưa đc
            // active, chỉ cho vào fMain khi license hợp lệ.
            splash?.SetStatus("Đang kiểm tra kích hoạt...");
            try { Globals.DeviceIdTask.GetAwaiter().GetResult(); } catch { }

            splash?.Close();

            using (var licenseForm = new fLicenseCheck())
            {
                if (licenseForm.ShowDialog() != DialogResult.OK)
                    return;
            }

            // Tạo fMain — license đã OK, có thể mở giao diện chính.
            var main = new fMain();
            if (splash != null)
            {
                // splash đã close ở trên — chỉ giữ block cũ cho an toàn (re-check dispose).
                try { splash.Close(); } catch { }
            }
            Application.Run(main);
        }

        public static bool IsEnvironmentReady()
        {
            // Kiểm tra ADB tồn tại ở đường dẫn cố định
            if (!File.Exists(Path.Combine(ProcessHelper.ADBPath, "adb.exe")))
                return false;
            // Kiểm tra Node — ưu tiên tìm trong PATH (instant, không phải spawn process 5s).
            // Fallback sang IsCommandAvailable khi không thấy trong PATH để xử lý trường
            // hợp Node được install ở chỗ khác (PortableApps, scoop) nhưng vẫn invoke được.
            if (!IsNodeInPath() && !IsCommandAvailable("node --version"))
                return false;
            return true;
        }

        private static bool IsNodeInPath()
        {
            try
            {
                string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
                foreach (var dir in pathEnv.Split(Path.PathSeparator))
                {
                    if (string.IsNullOrWhiteSpace(dir)) continue;
                    try
                    {
                        if (File.Exists(Path.Combine(dir, "node.exe"))) return true;
                    }
                    catch { }
                }
                return false;
            }
            catch { return false; }
        }

        private static bool IsCommandAvailable(string command)
        {
            try
            {
                string[] parts = command.Split(' ', 2);
                var psi = new ProcessStartInfo
                {
                    FileName = parts[0],
                    Arguments = parts.Length > 1 ? parts[1] : "",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                using var p = Process.Start(psi);
                if (p == null) return false;
                p.WaitForExit(5000);
                return p.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public static void RunLTPhoneHelper()
        {
            string helperPath = Path.Combine(
                AppContext.BaseDirectory, "QNHelper.exe");

            if (!File.Exists(helperPath))
            {
                MessageBox.Show(
                    "Môi trường chưa được cài đặt.\nVui lòng chạy QNHelper.exe để thiết lập.",
                    "Thiếu môi trường",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = helperPath,
                UseShellExecute = true,
                Verb = "runas",
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(10 * 60 * 1000); // tối đa 10 phút
        }

        public static void RestartApp()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? Environment.GetCommandLineArgs()[0];
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to restart app: " + ex);
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private static string StartupAppName => Application.ProductName ?? "QNPhoneFarm";

        public static bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
                return key?.GetValue(StartupAppName) != null;
            }
            catch
            {
                return false;
            }
        }

        public static void SetStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
                if (key == null) return;

                if (enable)
                {
                    string exePath = Process.GetCurrentProcess().MainModule?.FileName
                        ?? Environment.GetCommandLineArgs()[0];
                    key.SetValue(StartupAppName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(StartupAppName, throwOnMissingValue: false);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("SetStartup failed: " + ex);
            }
        }

        // ── Crash logging helpers ───────────────────────────────────────────────
        private static readonly object _crashLogLock = new();
        private static int _firstChanceLogged;

        private static void OnFirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            // Chỉ log IndexOutOfRange / ArgumentOutOfRange (thủ phạm crash nền lặp lại),
            // throttle tối đa 30 lần để không làm chậm + không spam file.
            if (e.Exception is IndexOutOfRangeException || e.Exception is ArgumentOutOfRangeException)
            {
                if (Interlocked.Increment(ref _firstChanceLogged) <= 30)
                    LogCrash("FirstChance." + e.Exception.GetType().Name, e.Exception);
            }
        }

        private static void LogCrash(string source, Exception? ex)
        {
            try
            {
                string dir = Path.Combine(AppContext.BaseDirectory, "logs");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, $"crash_{DateTime.Now:yyyy-MM-dd}.txt");
                string text =
                    $"------------------ {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{source}] ----------------------------"
                    + Environment.NewLine + ex + Environment.NewLine + Environment.NewLine;
                lock (_crashLogLock) File.AppendAllText(file, text);
                Trace.TraceError($"[{source}] {ex}");
            }
            catch { }
        }
    }
}