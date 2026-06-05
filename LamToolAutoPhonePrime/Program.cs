using AntdUI;
using AutoAndroid;
using DeviceId;
using LamToolAutoPhonePrime.Utils;
using LamToolAutoPhonePrime.Views.Forms;
using Microsoft.Win32;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace LamToolAutoPhonePrime
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
            Localization.Provider = new VietnameseLocalization();

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

            Globals.AutoLoginTask = Task.Run(() =>
            {
                try { TempLoginStorage.TryAutoLogin(); }
                catch (Exception ex) { Trace.TraceWarning("Auto-login skipped: " + ex); }
            });

            splash?.SetStatus("Đang mở giao diện...");

            // Giữ splash đến khi fMain thực sự Shown (đã render frame đầu).
            // Tạo fMain ở đây có thể mất vài giây (constructor + InitializeComponent),
            // và fMain_Load tạo 4 UserControl đồng bộ trước await đầu tiên — nếu close
            // splash trước Application.Run, user sẽ thấy màn hình trống ~10s như app cash.
            var main = new fMain();
            if (splash != null)
            {
                void OnMainShown(object? s, EventArgs e)
                {
                    main.Shown -= OnMainShown;
                    try { splash.Close(); } catch { }
                }
                main.Shown += OnMainShown;
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
    }
}