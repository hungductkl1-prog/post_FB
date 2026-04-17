using Sunny.Subdy.Data.Models;
using System.Diagnostics;
using System.Text;

namespace AutoAndroid
{
    public class LogHelper
    {
        public static string GetLogcat(string serial = null)
        {
            return ADBSocket.Shell(serial, "logcat", "-d", "-v", "threadtime");
        }
        private const long MaxLogFileSizeBytes = 5 * 1024 * 1024; // 5MB

        public static void WriteFile(string filename, string message)
        {
            string datePart = DateTime.Now.ToString("dd-MM-yyyy");
            string directoryPath = $"Logs\\{datePart}";
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            string filePath = Path.Combine(directoryPath, $"{filename}.txt");
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                bool append = true;
                if (fileInfo.Exists && fileInfo.Length >= MaxLogFileSizeBytes)
                {
                    append = false; // clear file bằng cách overwrite
                }
                using (var streamWriter = new StreamWriter(filePath, append, Encoding.UTF8))
                {
                    streamWriter.WriteLine(message);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }
            catch (Exception e)
            {

            }
        }
        public static void Error(string message)
        {
            WriteFile(string.Format("Error_{0:yyyy-MM-dd}", DateTime.Now), message);
        }

        public string State = string.Empty;
        public DeviceModel Device { get; set; }
        public LogHelper(DeviceModel device)
        {
            Device = device;
        }

        public void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            if (!string.IsNullOrEmpty(State))
            {
                message = $"{State} - " + message;
            }
            Device.Status = message+ "...";
        }
        public void ERROR(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            if (!string.IsNullOrEmpty(State))
            {
                message = $"{State} - " + message;
            }
            Device.Status = message + "...";
            Device.TypeColor = 1;
        }
        public void SUCCESS(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            if (!string.IsNullOrEmpty(State))
            {
                message = $"{State} - " + message + "...";
            }
            Device.Status = message;
            Device.TypeColor = 2;
        }
        public T RunWithLog<T>(string message, Func<T> func)
        {
            var watch = Stopwatch.StartNew();
            T result = func.Invoke();
            watch.Stop();
            Log($"{message}: {watch.ElapsedMilliseconds}ms");
            return result;
        }


    }
}
