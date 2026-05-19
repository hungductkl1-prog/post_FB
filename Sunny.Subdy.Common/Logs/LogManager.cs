using System;
using System.Diagnostics;
using System.Text;

namespace Sunny.Subdy.Common.Logs
{
    public enum LogLevel { Info, Debug, Warning, Error, Success }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public static class LogManager
    {
        private static readonly string BaseLogPath = Path.Combine(AppContext.BaseDirectory, "logs");
        public static List<string> LogRegsiner = new List<string>();

        /// <summary>Live UI sink — UI subscribes để hiển thị log realtime trong console panel.</summary>
        public static event Action<LogEntry>? Emitted;

        private static void Emit(LogLevel level, string message)
        {
            try
            {
                Emitted?.Invoke(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level     = level,
                    Message   = message,
                });
            }
            catch { /* UI handler không được làm gãy logging core */ }
        }

        public static void Warning(string message)
        {
            var sb = new StringBuilder();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            sb.AppendLine($"------------------ {timestamp} ----------------------------");
            sb.AppendLine($"Message   : {message}");
            sb.AppendLine();
            WriteLog("warning", sb.ToString());
            Emit(LogLevel.Warning, message);
        }

        public static void Success(string message)
        {
            var sb = new StringBuilder();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            sb.AppendLine($"------------------ {timestamp} ----------------------------");
            sb.AppendLine($"Message   : {message}");
            sb.AppendLine();
            WriteLog("success", sb.ToString());
            Emit(LogLevel.Success, message);
        }
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
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
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
            try { Emit(LogLevel.Error, exception?.Message ?? "(no message)"); } catch { }
        }

        public static void Info(string message)
        {
            var sb = new StringBuilder();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            sb.AppendLine($"------------------ {timestamp} ----------------------------");
            sb.AppendLine($"Message   : {message}");
            sb.AppendLine();
            WriteLog("info", sb.ToString());
            Emit(LogLevel.Info, message);
        }

        public static void Debug(string message)
        {
            var sb = new StringBuilder();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            sb.AppendLine($"------------------ {timestamp} ----------------------------");
            sb.AppendLine($"Message   : {message}");
            sb.AppendLine();
            WriteLog("debug", sb.ToString());
            Emit(LogLevel.Debug, message);
        }
    }
}
