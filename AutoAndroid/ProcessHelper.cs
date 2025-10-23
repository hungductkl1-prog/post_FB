using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAndroid
{
    public class ProcessHelper
    {
        public static string ADBPath = "C:\\DTAHelper\\sdk\\platform-tools\\";
        /// <summary>
        /// Chạy lệnh adb với đối số truyền theo dạng object[].
        /// </summary>
        public static string RunAdbCommand(params object[] args)
        {
            string command = string.Join(" ", args);
            return RunAdbWithTimeout(command);
        }

        /// <summary>
        /// Chạy lệnh adb có timeout và retry nếu lỗi.
        /// </summary>
        public static string RunAdbWithTimeout(string adbCommand, int timeoutSeconds = 10)
        {
            const int maxRetries = 3;
            const int retryDelayMs = 2000;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                using (Process process = new Process())
                {
                    try
                    {
                        process.StartInfo.FileName = "cmd.exe";
                        process.StartInfo.Arguments = $"/C \"{ADBPath}adb {adbCommand}\"";
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                        process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                        StringBuilder outputBuilder = new();
                        StringBuilder errorBuilder = new();

                        process.OutputDataReceived += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) outputBuilder.AppendLine(e.Data); };
                        process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) errorBuilder.AppendLine(e.Data); };

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        bool exited = process.WaitForExit(timeoutSeconds * 1000);
                        process.WaitForExit(); // chờ luồng async

                        string output = outputBuilder.ToString().Trim();
                        string error = errorBuilder.ToString().Trim();

                        if (!exited)
                        {
                            LogError($"[ADB Timeout] '{adbCommand}' timeout sau {timeoutSeconds}s. Thử lại {retryCount + 1}/{maxRetries}");
                            retryCount++;
                            Thread.Sleep(retryDelayMs);
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(error))
                        {
                            if (error.Contains("daemon not running"))
                            {
                                LogError("[ADB] Daemon không chạy, đang khởi động lại.");
                                ADBHelper.StartServer();
                                retryCount++;
                                Thread.Sleep(retryDelayMs);
                                continue;
                            }

                            if (error.Contains("device offline") || error.Contains("not found"))
                            {
                                LogError("[ADB] Thiết bị không sẵn sàng.");
                                retryCount++;
                                Thread.Sleep(retryDelayMs);
                                continue;
                            }

                            // Nếu là warning không nghiêm trọng, vẫn return output
                            if (output.Length > 0)
                                return output;

                            LogError($"[ADB ERROR] {error}");
                            return "";
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
            }

            LogError($"[ADB FAILED] Không thể chạy lệnh: {adbCommand}");
            return "";
        }


        /// <summary>
        /// Chạy lệnh CMD bình thường, không prefix "adb", không timeout.
        /// </summary>
        public static string RunRawCmd(string cmd)
        {
            var info = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C {cmd}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using Process process = Process.Start(info);
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(error))
                return $"ERROR: {error.Trim()}";

            return output.Trim();
        }

        private static void LogError(string msg)
        {
            LogHelper.Error(msg);
        }
    }
}
