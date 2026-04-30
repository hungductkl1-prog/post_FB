using AntdUI;
using AutoAndroid;
using DeviceId;
using LamToolAutoPhonePrime.Utils;
using LamToolAutoPhonePrime.Views;
using LamToolAutoPhonePrime.Views.Forms;
using Microsoft.Win32;
using Sunny.Subdy.Common.API;
using Sunny.Subdy.Common.API.Jobs;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;

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

            // Kiểm tra môi trường trước khi khởi động
            try
            {
                if (!IsEnvironmentReady())
                {
                    RunLTPhoneHelper();

                    // Re-check sau khi helper kết thúc. Nếu vẫn chưa ready
                    // (user huỷ UAC, AppLocker chặn, helper crash...) thì báo
                    // rõ thay vì RestartApp im lặng để rồi loop vô hạn.
                    if (!IsEnvironmentReady())
                    {
                        MessageBox.Show(
                            "Cài đặt môi trường chưa hoàn tất.\n" +
                            "Vui lòng chạy GolikeHelper.exe (Right-click > Run as administrator) rồi thử lại.",
                            "Thiếu môi trường",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    RestartApp();
                    return;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Environment check failed: " + ex);
            }

            // Clean slate ADB: diệt mọi adb.exe zombie từ session khác (có thể đã
            // được start dưới user thường, lock device khỏi context Admin của app).
            // Sau đó start-server mới dưới quyền Admin để adb thực sự bind 5037 ổn định.
            try
            {
                ADBHelper.KillAllAdbProcesses();
                Thread.Sleep(500);
                ADBHelper.EnsureServerStarted();
            }
            catch (Exception ex)
            {
                Trace.TraceError("ADB clean slate failed: " + ex);
            }

            // Kiểm tra VCpp trước khi load font (chỉ mất thời gian nếu cần cài)
          

            Localization.Provider = new VietnameseLocalization();

            ComWrappers.RegisterForMarshalling(WinFormsComInterop.WinFormsComWrappers.Instance);
            ApplicationConfiguration.Initialize();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            // Wire up SortableBindingList to auto-unregister items from ThrottledPropertyNotifier
            // on removal, preventing stale PropertyChanged events from crashing DataGridView.
            SortableBindingList<DeviceModel>.OnBeforeRemove = ThrottledPropertyNotifier.Unregister;
            SortableBindingList<Account>.OnBeforeRemove = ThrottledPropertyNotifier.Unregister;
            SortableBindingList<JobHistory>.OnBeforeRemove = ThrottledPropertyNotifier.Unregister;

            // Load font sau khi init WinForms để tránh lỗi GDI+
            FontUtil.LoadCustomFonts();
            Globals.DeviceId = new DeviceIdBuilder()
                   .OnWindows(windows => windows.AddWindowsDeviceId())
                   .ToString();
            // Gắn icon Golike cho toàn bộ form (cả dialog mở sau).
            AppIconHelper.Install();
            using (var frm = new fLogin())
            {
               frm.ShowDialog();
            }
            Application.Run(new fMain());
        }

        public static bool IsEnvironmentReady()
        {
          
            // Kiểm tra ADB tồn tại ở đường dẫn cố định
            if (!File.Exists(Path.Combine(ProcessHelper.ADBPath, "adb.exe")))
                return false;
            // Kiểm tra Node
            if (!IsCommandAvailable("node --version"))
                return false;
            return true;
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
                AppContext.BaseDirectory, "GolikeHelper.exe");

            if (!File.Exists(helperPath))
            {
                MessageBox.Show(
                    "Môi trường chưa được cài đặt.\nVui lòng chạy GolikeHelper.exe để thiết lập.",
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

        public static bool IsVCppInstalled(string arch)
        {
            try
            {
                RegistryView view = arch.Equals("x64", StringComparison.OrdinalIgnoreCase) ? RegistryView.Registry64 : RegistryView.Registry32;
                string keyPath = $@"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\{arch}";
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                using (var key = baseKey.OpenSubKey(keyPath))
                {
                    if (key == null) return false;
                    object val = key.GetValue("Installed");
                    if (val is int intVal) return intVal == 1;
                    if (val is string strVal && int.TryParse(strVal, out int parsed)) return parsed == 1;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Failed to read registry for VCpp: " + ex);
                return false;
            }
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

        public static void CheckAndInstallVCpp()
        {
            bool is64 = Environment.Is64BitOperatingSystem;

            if (is64)
            {
                if (!IsVCppInstalled("x64"))
                {
                    string url = "https://aka.ms/vs/17/release/vc_redist.x64.exe";
                    string file = Path.Combine(Path.GetTempPath(), "vc_redist.x64.exe");
                    DownloadFile(url, file);
                    InstallVCpp(file);
                    TryDeleteFileQuiet(file);
                    RestartApp();
                }
            }
            else
            {
                if (!IsVCppInstalled("x86"))
                {
                    string url = "https://aka.ms/vs/17/release/vc_redist.x86.exe";
                    string file = Path.Combine(Path.GetTempPath(), "vc_redist.x86.exe");
                    DownloadFile(url, file);
                    InstallVCpp(file);
                    TryDeleteFileQuiet(file);
                    RestartApp();
                }
            }
        }

        public static void InstallVCpp(string installerPath)
        {
            if (string.IsNullOrWhiteSpace(installerPath) || !File.Exists(installerPath))
                throw new FileNotFoundException("Installer not found", installerPath);
            var psi = new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "/install /quiet /norestart",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (var p = Process.Start(psi))
            {
                if (p != null)
                {
                    p.WaitForExit(5 * 60 * 1000);
                }
            }
        }

        public static void DownloadFile(string url, string filePath)
        {
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10)))
                {
                    DownloadFileAsync(url, filePath, cts.Token).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                TryDeleteFileQuiet(filePath);
                throw new InvalidOperationException($"Failed to download '{url}' to '{filePath}'", ex);
            }
        }

        private static async Task DownloadFileAsync(string url, string filePath, CancellationToken cancellationToken)
        {
            using (var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
            using (var client = new HttpClient(handler, disposeHandler: true) { Timeout = TimeSpan.FromMinutes(10) })
            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? Path.GetTempPath());

                using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                using (var destination = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    await source.CopyToAsync(destination, 81920, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static void TryDeleteFileQuiet(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private static string StartupAppName => Application.ProductName ?? "GolikePhoneFarm";

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