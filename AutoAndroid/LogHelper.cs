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
                using (var streamWriter = new StreamWriter(filePath, true, Encoding.UTF8))
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

        public string Sate = string.Empty;
        public DeviceModel Device { get; set; }
        public LogHelper(DeviceModel device)
        {
            Device = device;
        }

        public void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            if (!string.IsNullOrEmpty(Sate))
            {
                message = $"{Sate} - " + message;
            }
            Device.Status = message+ "...";
        }
        public void ERROR(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            if (!string.IsNullOrEmpty(Sate))
            {
                message = $"{Sate} - " + message;
            }
            Device.Status = message + "...";
            Device.TypeColor = 1;
        }
        public void SUCCESS(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            if (!string.IsNullOrEmpty(Sate))
            {
                message = $"{Sate} - " + message + "...";
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
