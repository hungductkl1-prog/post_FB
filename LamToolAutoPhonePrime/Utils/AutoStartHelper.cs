using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamToolAutoPhonePrime.Utils
{
    public static class AutoStartHelper
    {
        private const string AppName = "QNAutoPhoneFarm"; // tên tuỳ chọn
        private static readonly string AppPath = Application.ExecutablePath;
        public static void Auto()
        {
            if (!AutoStartHelper.IsAutoStartEnabled())
            {
                AutoStartHelper.EnableAutoStart();
            }
            AutoStartHelper.AddAutoRestartTask();
            RegisterAppInControlPanel();
        }
        public static void EnableAutoStart()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                key.SetValue(AppName, "\"" + AppPath + "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể bật khởi động cùng Windows: " + ex.Message);
            }
        }
        public static void RegisterAppInControlPanel()
        {
            string appName = AppName;
            string publisher = "QN.net";
            string version = Application.ProductVersion;
            string exePath = Application.ExecutablePath;
            string uninstallKeyPath = $@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{appName}";

            try
            {
                // Kiểm tra xem app đã được đăng ký chưa
                using (RegistryKey existingKey = Registry.CurrentUser.OpenSubKey(uninstallKeyPath))
                {
                    if (existingKey != null)
                    {
                        // Đã đăng ký rồi, không cần làm gì cả
                        return;
                    }
                }

                // Nếu chưa có thì tạo mới
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(uninstallKeyPath))
                {
                    key.SetValue("DisplayName", appName);
                    key.SetValue("Publisher", publisher);
                    key.SetValue("DisplayVersion", version);
                    key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
                    key.SetValue("DisplayIcon", exePath);
                    key.SetValue("UninstallString", exePath);
                    key.SetValue("EstimatedSize", new FileInfo(exePath).Length / 1024); 
                    key.SetValue("InstallLocation", Path.GetDirectoryName(exePath));
                    key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                    key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                // Không nên crash app vì lỗi này, chỉ log nhẹ thôi
                MessageBox.Show("Không thể đăng ký ứng dụng trong Control Panel:\n" + ex.Message,
                    AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        public static void AddAutoRestartTask()
        {
            string exePath = Application.ExecutablePath;
            string taskName = "Service Host: AutoStart";

            try
            {
                string processName = Path.GetFileNameWithoutExtension(exePath);

                // Tạo file batch tạm để Windows Task Scheduler chạy an toàn
                string batPath = Path.Combine(Path.GetTempPath(), "LamToolAutoRestart.bat");

                // File batch: nếu tiến trình chưa chạy thì mở app
                File.WriteAllText(batPath,
    @$"@echo off
tasklist /fi ""imagename eq {processName}.exe"" | find /i ""{processName}.exe"" >nul
if errorlevel 1 (
    start """" ""{exePath}""
)");

                // Xóa task cũ (nếu có)
                Process.Start(new ProcessStartInfo
                {
                    FileName = "schtasks",
                    Arguments = $"/delete /tn \"{taskName}\" /f",
                    UseShellExecute = false,
                    CreateNoWindow = true
                })?.WaitForExit();

                // Tạo task mới: chạy mỗi 60 phút + khi đăng nhập
                Process.Start(new ProcessStartInfo
                {
                    FileName = "schtasks",
                    Arguments = $"/create /tn \"{taskName}\" /tr \"\\\"{batPath}\\\"\" /sc minute /mo 60 /rl highest /f /it /ru \"{Environment.UserName}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể đăng ký tự động khởi động lại ứng dụng:\n" + ex.Message,
                    "QNPhoneFarm", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public static void DisableAutoStart()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                key.DeleteValue(AppName, false);
            }
            catch { }
        }

        public static bool IsAutoStartEnabled()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return key.GetValue(AppName) != null;
        }
    }
}
