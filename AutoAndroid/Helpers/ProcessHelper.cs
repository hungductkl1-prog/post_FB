using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace AutoAndroid
{
    public class ProcessHelper
    {
        public const int MaxConcurrentCmdProcesses = 20;
        public static string ADBPath = "C:\\LTHelper\\sdk\\platform-tools\\";
        private static readonly SemaphoreSlim CmdSemaphore = new SemaphoreSlim(MaxConcurrentCmdProcesses, MaxConcurrentCmdProcesses);

        public sealed class CommandExecutionResult
        {
            public string Output { get; init; } = string.Empty;
            public string Error { get; init; } = string.Empty;
            public int ExitCode { get; init; } = -1;
            public bool TimedOut { get; init; }
            public bool Success => !TimedOut && ExitCode == 0;
        }

        /// <summary>
        /// Chạy lệnh adb với đối số truyền theo dạng object[].
        /// </summary>
        public static string RunAdbCommand(params object[] args)
        {
            string command = string.Join(" ", args);
            return RunAdbWithTimeout(command);
        }

        public static string RunAdbCommand(string adbCommand, int timeoutSeconds)
        {
            return RunAdbWithTimeout(adbCommand, timeoutSeconds);
        }

        public static CommandExecutionResult RunAdbWithResult(string adbCommand, int timeoutSeconds = 10)
        {
            return RunCmdWithResult($"/C \"{ADBPath}adb {adbCommand}\"", timeoutSeconds);
        }

        public static CommandExecutionResult RunRawCmdWithResult(string cmd, int timeoutSeconds = 0)
        {
            return RunCmdWithResult($"/C {cmd}", timeoutSeconds);
        }

        /// <summary>
        /// Chạy lệnh adb có timeout và retry nếu lỗi.
        /// </summary>
        /// <summary>
        /// Chạy lệnh adb một lần, KHÔNG retry. Dùng cho lệnh probe nhanh (service check, online check).
        /// Trả về chuỗi rỗng nếu timeout hoặc lỗi.
        /// </summary>
        public static string RunAdbNoRetry(string adbCommand, int timeoutSeconds = 5)
        {
            try
            {
                CommandExecutionResult result = RunAdbWithResult(adbCommand, timeoutSeconds);
                if (result.TimedOut) return string.Empty;
                if (!string.IsNullOrWhiteSpace(result.Error) && string.IsNullOrWhiteSpace(result.Output))
                    return string.Empty;
                return result.Output.Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        // Theo dõi số lần 'devices' bị timeout liên tiếp để tự restart ADB khi server chết hoàn toàn
        private static int _consecutiveDevicesTimeout = 0;
        private static readonly object _adbRestartLock = new object();
        private static DateTime _lastAdbRestart = DateTime.MinValue;

        public static string RunAdbWithTimeout(string adbCommand, int timeoutSeconds = 10)
        {
            // Kiểm tra connection leak định kỳ (throttled 30s), bỏ qua nếu đang dùng netstat để tránh đệ quy
            if (!adbCommand.Contains("start-server") && !adbCommand.Contains("kill-server"))
                ADBHelper.CheckAndFixConnectionLeak();

            const int maxRetries = 3;
            const int retryDelayMs = 1000;
            int retryCount = 0;
            int timeoutsThisCall = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    CommandExecutionResult result = RunAdbWithResult(adbCommand, timeoutSeconds);
                    string output = result.Output.Trim();
                    string error = result.Error.Trim();

                    if (result.TimedOut)
                    {
                        LogError($"[ADB Timeout] '{adbCommand}' timeout sau {timeoutSeconds}s. Thử lại {retryCount + 1}/{maxRetries}");
                        timeoutsThisCall++;
                        retryCount++;

                        // Nếu lệnh 'devices' bị timeout liên tục → ADB server chết, cần restart ngay
                        if (adbCommand.Trim() == "devices")
                        {
                            int total = Interlocked.Increment(ref _consecutiveDevicesTimeout);
                            if (total >= 3)
                            {
                                bool shouldRestart = false;
                                lock (_adbRestartLock)
                                {
                                    if ((DateTime.Now - _lastAdbRestart).TotalSeconds > 60)
                                    {
                                        _lastAdbRestart = DateTime.Now;
                                        shouldRestart = true;
                                    }
                                }
                                if (shouldRestart)
                                {
                                    LogError("[ADB] Server bị treo hoàn toàn, đang restart...");
                                    Interlocked.Exchange(ref _consecutiveDevicesTimeout, 0);
                                    ADBHelper.Restart();
                                    Thread.Sleep(2000);
                                }
                            }
                        }

                        Thread.Sleep(retryDelayMs);
                        continue;
                    }

                    // Reset bộ đếm timeout khi lệnh 'devices' thành công
                    if (adbCommand.Trim() == "devices")
                        Interlocked.Exchange(ref _consecutiveDevicesTimeout, 0);

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        if (error.Contains("daemon not running", StringComparison.OrdinalIgnoreCase))
                        {
                            LogError("[ADB] Daemon không chạy, đang khởi động lại.");
                            ADBHelper.StartServer();
                            retryCount++;
                            Thread.Sleep(retryDelayMs);
                            continue;
                        }

                        if (error.Contains("device offline", StringComparison.OrdinalIgnoreCase)
                            || error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                        {
                            LogError("[ADB] Thiết bị không sẵn sàng.");
                            retryCount++;
                            Thread.Sleep(retryDelayMs);
                            continue;
                        }

                        // Nếu là warning không nghiêm trọng, vẫn return output
                        if (output.Length > 0)
                        {
                            return output;
                        }

                        LogError($"[ADB ERROR] {error}");
                        return string.Empty;
                    }

                    if (!result.Success && string.IsNullOrWhiteSpace(output))
                    {
                        retryCount++;
                        Thread.Sleep(retryDelayMs);
                        continue;
                    }

                    return output;
                }
                catch (Exception ex)
                {
                    LogError($"[EXCEPTION] {ex.Message}");
                    retryCount++;
                    Thread.Sleep(retryDelayMs);
                }
            }

            LogError($"[ADB FAILED] Không thể chạy lệnh: {adbCommand}");
            return string.Empty;
        }

        /// <summary>
        /// Chạy lệnh CMD bình thường, không prefix "adb", không timeout.
        /// </summary>
        public static string RunRawCmd(string cmd)
        {
            CommandExecutionResult result = RunRawCmdWithResult(cmd);
            if (!result.Success)
            {
                string err = string.IsNullOrWhiteSpace(result.Error) ? $"ExitCode: {result.ExitCode}" : result.Error;
                return $"ERROR: {err.Trim()}";
            }

            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                return $"ERROR: {result.Error.Trim()}";
            }

            return result.Output.Trim();
        }

        private static CommandExecutionResult RunCmdWithResult(string cmdArguments, int timeoutSeconds)
        {
            CmdSemaphore.Wait();
            try
            {
                using Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = cmdArguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    }
                };

                StringBuilder outputBuilder = new StringBuilder();
                StringBuilder errorBuilder = new StringBuilder();

                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        errorBuilder.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                bool exited = timeoutSeconds <= 0 || process.WaitForExit(timeoutSeconds * 1000);
                if (!exited)
                {
                    TryKillProcess(process);
                    return new CommandExecutionResult
                    {
                        TimedOut = true,
                        ExitCode = -1,
                        Output = outputBuilder.ToString(),
                        Error = errorBuilder.ToString()
                    };
                }

                process.WaitForExit();
                return new CommandExecutionResult
                {
                    TimedOut = false,
                    ExitCode = process.ExitCode,
                    Output = outputBuilder.ToString(),
                    Error = errorBuilder.ToString()
                };
            }
            finally
            {
                CmdSemaphore.Release();
            }
        }

        private static void TryKillProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                    process.WaitForExit();
                }
            }
            catch
            {
            }
        }

        private static void LogError(string msg)
        {
            LogHelper.Error(msg);
        }
    }
}
