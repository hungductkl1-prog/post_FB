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
            FontUtil.LoadCustomFonts();

            // Perform VCpp check/install on startup but keep Main synchronous.
            // The helper will run download/install synchronously (streaming) and restart if needed.
            try
            {
                CheckAndInstallVCpp();
            }
            catch (Exception ex)
            {
                // Non-fatal: log and continue. If install fails, user can run manually.
                Trace.TraceError("VCpp check/install failed: " + ex);
            }

            ComWrappers.RegisterForMarshalling(WinFormsComInterop.WinFormsComWrappers.Instance);
            ApplicationConfiguration.Initialize();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Globals.DeviceId = new DeviceIdBuilder()
                   .OnWindows(windows => windows.AddWindowsDeviceId())
                   .ToString();

            using (var login = new fLogin())
            {
                if (login.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    Environment.Exit(0);
                }
            }

            Application.Run(new fMain());
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
    }
}