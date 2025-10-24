using System;
using System.Diagnostics;
using System.Text;

namespace Sunny.Subdy.Common.Logs
{
    public static class LogManager
    {
        private static readonly string BaseLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        public static List<string> LogRegsiner = new List<string>();
        private static void WriteLog(string nameLog, string message)
        {
            try
            {
                string dateFolder = DateTime.Now.ToString("dd-MM-yyyy");
                string logFolder = Path.Combine(BaseLogPath, dateFolder);
                Directory.CreateDirectory(logFolder);

                string logPath = Path.Combine(logFolder, nameLog + ".txt");

                using (var writer = new StreamWriter(logPath, true, Encoding.UTF8))
                {
                    writer.WriteLine($"{message}");
                }

                CleanupOldestFolderIfExceedsLimit();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Logging failed: " + ex.Message);
            }
        }

        private static void CleanupOldestFolderIfExceedsLimit()
        {
            try
            {
                var directories = new DirectoryInfo(BaseLogPath)
                    .GetDirectories()
                    .OrderBy(d => d.CreationTimeUtc)
                    .ToList();

                while (directories.Count > 8)
                {
                    var oldest = directories.First();
                    try
                    {
                        oldest.Delete(true);
                        directories.RemoveAt(0);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Failed to delete folder '{oldest.Name}': {ex.Message}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Cleanup failed: " + ex.Message);
            }
        }

        public static void Error(Exception exception)
        {
            if (exception == null) return;

            var sb = new StringBuilder();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            sb.AppendLine($"------------------ {timestamp} ----------------------------");

            int level = 0;
            while (exception != null)
            {
                try
                {
                    sb.AppendLine($"Level {level}: {exception.GetType().FullName}");
                    sb.AppendLine($"Message   : {exception.Message}");

                    var trace = new StackTrace(exception, true); // true => lấy thông tin file và line
                    foreach (var frame in trace.GetFrames() ?? Array.Empty<StackFrame>())
                    {
                        var method = frame.GetMethod();
                        var className = method?.DeclaringType?.FullName ?? "<UnknownClass>";
                        var methodName = method?.Name ?? "<UnknownMethod>";
                        var fileName = frame.GetFileName() ?? "<NoFile>";
                        var line = frame.GetFileLineNumber();

                        sb.AppendLine($"  at {className}.{methodName} in {fileName}:line {line}");
                    }

                    sb.AppendLine();
                    exception = exception.InnerException;
                    level++;
                }
                catch
                {

                }
              
            }

            WriteLog("error", sb.ToString());
        }

        public static void Info(string message)
        {
            var sb = new StringBuilder();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            sb.AppendLine($"------------------ {timestamp} ----------------------------");
            sb.AppendLine($"Message   : {message}");
            sb.AppendLine();
            WriteLog("info", sb.ToString());
        }

        public static void Debug(string message)
        {
            var sb = new StringBuilder();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            sb.AppendLine($"------------------ {timestamp} ----------------------------");
            sb.AppendLine($"Message   : {message}");
            sb.AppendLine();
            WriteLog("debug", sb.ToString());
        }
    }
}
