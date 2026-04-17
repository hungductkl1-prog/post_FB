using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoAndroid
{
    public static class AutomationEnvironmentService
    {
        private static readonly object WindowsSetupLock = new object();
        private static readonly HashSet<AutomationType> WindowsSetupDone = new HashSet<AutomationType>();
        private static readonly ConcurrentDictionary<string, byte> DeviceSetupDone = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, AutomationType> DeviceAutomationTypes = new ConcurrentDictionary<string, AutomationType>(StringComparer.OrdinalIgnoreCase);

        public static void EnsureWindowsReady(AutomationType type)
        {
            lock (WindowsSetupLock)
            {
                if (WindowsSetupDone.Contains(type))
                {
                    return;
                }

                // ADB là phần chung cho cả ui2automation và appium.
                ADBHelper.InitADB();

                if (type == AutomationType.Appium)
                {
                    EnsureNodeInstalled();
                    EnsureAppiumInstalled();
                    EnsureUiAutomator2DriverInstalled();
                }

                WindowsSetupDone.Add(type);
            }
        }

        public static async Task<bool> EnsureDeviceReadyAsync(ADBClient client, AutomationType type)
        {
            if (client?.Device == null || string.IsNullOrWhiteSpace(client.Device.Serial))
            {
                return false;
            }

            SetDeviceAutomationType(client.Device.Serial, type);

            string cacheKey = $"{type}:{client.Device.Serial}";
            if (DeviceSetupDone.ContainsKey(cacheKey))
            {
                return true;
            }

            try
            {
                bool success;
                if (type == AutomationType.Appium)
                {
                    success = EnsureAppiumDeviceReady(client);
                }
                else
                {
                    success = await client.ATX.SetupATX();
                }

                if (success)
                {
                    DeviceSetupDone.TryAdd(cacheKey, 0);
                }

                return success;
            }
            catch (Exception ex)
            {
                client.LogHelper.Log($"Setup {AutomationTypeResolver.Normalize(type)} fail: {ex.Message}");
                return false;
            }
        }

        public static void SetDeviceAutomationType(string serial, AutomationType type)
        {
            if (string.IsNullOrWhiteSpace(serial))
            {
                return;
            }

            DeviceAutomationTypes[serial] = type;
        }

        public static AutomationType GetDeviceAutomationType(string serial, AutomationType fallback = AutomationType.Ui2Automation)
        {
            if (string.IsNullOrWhiteSpace(serial))
            {
                return fallback;
            }

            if (DeviceAutomationTypes.TryGetValue(serial, out AutomationType type))
            {
                return type;
            }

            return fallback;
        }

        private static void EnsureNodeInstalled()
        {
            if (IsCommandReady("node --version"))
            {
                return;
            }

            ProcessHelper.RunRawCmdWithResult(
                "winget install --id OpenJS.NodeJS.LTS -e --silent --accept-package-agreements --accept-source-agreements",
                600);

            if (!IsCommandReady("node --version"))
            {
                throw new InvalidOperationException("Không thể cài Node.js để setup Appium.");
            }
        }

        private static void EnsureAppiumInstalled()
        {
            if (IsCommandReady("appium --version"))
            {
                return;
            }

            if (!IsCommandReady("npm --version"))
            {
                throw new InvalidOperationException("Không tìm thấy npm để cài Appium.");
            }

            ProcessHelper.CommandExecutionResult installResult = ProcessHelper.RunRawCmdWithResult("npm install -g appium", 900);
            if (!installResult.Success)
            {
                string message = string.IsNullOrWhiteSpace(installResult.Error) ? installResult.Output : installResult.Error;
                throw new InvalidOperationException($"Cài Appium thất bại: {message}");
            }

            if (!IsCommandReady("appium --version"))
            {
                throw new InvalidOperationException("Cài Appium xong nhưng chưa gọi được lệnh appium.");
            }
        }

        private static void EnsureUiAutomator2DriverInstalled()
        {
            ProcessHelper.CommandExecutionResult listResult = ProcessHelper.RunRawCmdWithResult("appium driver list --installed", 120);
            if (ContainsUiAutomator2Driver(listResult.Output) || ContainsUiAutomator2Driver(listResult.Error))
            {
                return;
            }

            ProcessHelper.CommandExecutionResult installResult = ProcessHelper.RunRawCmdWithResult("appium driver install uiautomator2", 600);
            if (!installResult.Success)
            {
                string message = string.IsNullOrWhiteSpace(installResult.Error) ? installResult.Output : installResult.Error;
                throw new InvalidOperationException($"Không thể cài driver uiautomator2 cho Appium: {message}");
            }

            ProcessHelper.CommandExecutionResult verifyResult = ProcessHelper.RunRawCmdWithResult("appium driver list --installed", 120);
            if (!ContainsUiAutomator2Driver(verifyResult.Output) && !ContainsUiAutomator2Driver(verifyResult.Error))
            {
                throw new InvalidOperationException("Đã chạy cài driver uiautomator2 nhưng không verify được driver đã sẵn sàng.");
            }
        }

        private static bool EnsureAppiumDeviceReady(ADBClient client)
        {
            string serial = client.Device.Serial;

            ProcessHelper.RunAdbWithTimeout($"-s {serial} wait-for-device", 30);
            ProcessHelper.RunAdbWithTimeout($"-s {serial} shell settings put global hidden_api_policy_pre_p_apps 1", 10);
            ProcessHelper.RunAdbWithTimeout($"-s {serial} shell settings put global hidden_api_policy_p_apps 1", 10);
            ProcessHelper.RunAdbWithTimeout($"-s {serial} shell settings put global hidden_api_policy 1", 10);

            foreach (string apk in ResolveAppiumAndroidApks())
            {
                try
                {
                    client.InstallApp(apk);
                }
                catch (Exception ex)
                {
                    client.LogHelper.Log($"Install appium helper apk fail: {Path.GetFileName(apk)} - {ex.Message}");
                }
            }

            // Appium mode chỉ cần chắc chắn ADB ready; session Appium sẽ hoàn tất phần còn lại nếu cần.
            return true;
        }

        private static IEnumerable<string> ResolveAppiumAndroidApks()
        {
            ProcessHelper.CommandExecutionResult npmRootResult = ProcessHelper.RunRawCmdWithResult("npm root -g", 30);
            if (!npmRootResult.Success || string.IsNullOrWhiteSpace(npmRootResult.Output))
            {
                return Array.Empty<string>();
            }

            string npmRoot = npmRootResult.Output.Trim();
            List<string> apkFiles = new List<string>();
            List<string> roots = new List<string>
            {
                Path.Combine(npmRoot, "appium-uiautomator2-driver"),
                Path.Combine(npmRoot, "appium", "node_modules", "appium-uiautomator2-driver"),
            };

            foreach (string root in roots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                AddApks(apkFiles, Path.Combine(root, "node_modules", "io.appium.settings", "apks"), "*settings*.apk");
                AddApks(apkFiles, Path.Combine(root, "node_modules", "appium-uiautomator2-server", "apks"), "*.apk");
            }

            return apkFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void AddApks(List<string> files, string folderPath, string pattern)
        {
            if (!Directory.Exists(folderPath))
            {
                return;
            }

            foreach (string file in Directory.GetFiles(folderPath, pattern, SearchOption.TopDirectoryOnly))
            {
                files.Add(file);
            }
        }

        private static bool IsCommandReady(string command)
        {
            ProcessHelper.CommandExecutionResult result = ProcessHelper.RunRawCmdWithResult(command, 30);
            if (result.TimedOut || ContainsCommandNotFound(result))
            {
                return false;
            }

            return result.Success || !string.IsNullOrWhiteSpace(result.Output);
        }

        private static bool ContainsCommandNotFound(ProcessHelper.CommandExecutionResult result)
        {
            string all = $"{result.Output}\n{result.Error}";
            return all.Contains("is not recognized", StringComparison.OrdinalIgnoreCase)
                || all.Contains("not found", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsUiAutomator2Driver(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return Regex.IsMatch(text, @"\buiautomator2\b", RegexOptions.IgnoreCase);
        }
    }
}
