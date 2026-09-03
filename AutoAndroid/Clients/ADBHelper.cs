using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;
using System.Text;
using System.IO.Compression;
using System.Threading.Tasks;

namespace AutoAndroid
{
    public class ADBHelper
    {
        private static HashSet<int> UsedPorts = new HashSet<int>();
        public static void StartServer()
        {
            ProcessHelper.RunAdbWithTimeout($"start-server", 5);
            _serverStarted = true;
        }
        public static void KillServer()
        {
            ProcessHelper.RunAdbWithTimeout($"kill-server", 5);
            _serverStarted = false;
        }

        /// <summary>
        /// Kill toàn bộ tiến trình adb.exe đang chạy (tránh ngẽn khi phần mềm khởi động lại).
        /// Dùng khi kill-server bị treo do nhiều adb zombie process.
        /// </summary>
        public static void KillAllAdbProcesses()
        {
            try
            {
                foreach (var proc in Process.GetProcessesByName("adb"))
                {
                    try { proc.Kill(true); proc.WaitForExit(2000); } catch { }
                }
            }
            catch { }
        }

        /// <summary>
        /// Khởi động lại adb an toàn: kill toàn bộ process trước rồi start-server.
        /// Dùng khi phát hiện connection leak hoặc khi adb bị ngẽn.
        /// </summary>
        public static void Restart()
        {
            KillAllAdbProcesses();
            _serverStarted = false;
            Thread.Sleep(500);
            ProcessHelper.RunAdbWithTimeout("start-server", 10);
            _serverStarted = true;
        }

        private static int _lastLeakCount = 0;
        private static DateTime _lastLeakCheck = DateTime.MinValue;

        /// <summary>
        /// Kiểm tra connection leak trên port 5037.
        /// Nếu số CLOSE_WAIT + FIN_WAIT_2 vượt ngưỡng → tự động restart adb.
        /// Gọi định kỳ hoặc trước các thao tác quan trọng.
        /// </summary>
        public static bool CheckAndFixConnectionLeak(int threshold = 15)
        {
            // Throttle: chỉ check mỗi 30 giây
            if ((DateTime.Now - _lastLeakCheck).TotalSeconds < 30)
                return false;
            _lastLeakCheck = DateTime.Now;

            try
            {
                var result = ProcessHelper.RunRawCmdWithResult("netstat -ano | findstr :5037", 5);
                if (result.TimedOut || string.IsNullOrWhiteSpace(result.Output))
                    return false;

                int leakCount = result.Output
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Count(line => line.Contains("CLOSE_WAIT") || line.Contains("FIN_WAIT_2"));

                _lastLeakCount = leakCount;

                if (leakCount >= threshold)
                {
                    LogHelper.Error($"[ADB] Phát hiện {leakCount} connection leak trên port 5037, đang restart...");
                    Restart();
                    LogHelper.Error($"[ADB] Restart xong, leak đã được dọn sạch.");
                    return true;
                }
            }
            catch { }

            return false;
        }
        public static List<string> GetDevices()
        {
            // Cold start: nếu chưa từng start-server trong process này, gọi start-server trước.
            // Bảo đảm daemon đã sẵn sàng nhận connection khi chạy 'adb devices'.
            EnsureServerStarted();

            // Retry ngắn khi output rỗng: lần đầu sau start-server, daemon có thể còn
            // đang enumerate thiết bị USB (1-2s). Thử tối đa 3 lần × 5s.
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var devicesOutput = ProcessHelper.RunAdbMonitorCommand("devices", 5);
                var lines = devicesOutput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                var result = lines
                    .Skip(1)
                    .Where(line => line.Contains("\tdevice"))
                    .Select(line => line.Split('\t')[0])
                    .ToList();

                if (result.Count > 0) return result;

                // Rỗng có thể do: (a) thật sự không có thiết bị, (b) daemon chưa enumerate xong.
                // Chỉ retry nếu output hoàn toàn rỗng (timeout / server chưa up).
                // Nếu output có tiêu đề "List of devices attached" nhưng không có dòng device
                // thì coi như không có thiết bị thật, không retry.
                if (!string.IsNullOrWhiteSpace(devicesOutput) && lines.Length >= 1)
                    return result;

                if (attempt < 2) Thread.Sleep(800);
            }
            return new List<string>();
        }

        private static volatile bool _serverStarted = false;
        private static readonly object _serverStartLock = new object();

        /// <summary>
        /// Đảm bảo adb server đã start ít nhất 1 lần trong process này.
        /// Idempotent: các lần gọi sau no-op. Dùng trước GetDevices() để tránh
        /// cold-start trả rỗng vì daemon chưa up.
        /// </summary>
        public static void EnsureServerStarted()
        {
            if (_serverStarted) return;
            lock (_serverStartLock)
            {
                if (_serverStarted) return;
                try { ProcessHelper.RunAdbWithTimeout("start-server", 10); }
                catch { }
                _serverStarted = true;
            }
        }
        public static void InitADB()
        {
            string dtaHelperPath = @"C:\DTAHelper";
            string linkDTA = "https://www.dropbox.com/scl/fi/3bediza9mih9gmekxqi4n/DTAHelper.zip?rlkey=0igvcdpqde4j9lnl1kvpe1qa2&st=41hq907c&dl=1";
            string pathDown = Path.Combine(AppContext.BaseDirectory, "DTAHelper.zip");

            for (int i = 0; i < 5; i++)
            {
                if (Directory.Exists(dtaHelperPath))
                    break;

                try
                {
                    // Xoá file cũ nếu có
                    if (File.Exists(pathDown))
                        File.Delete(pathDown);

                    GithubDown(linkDTA, pathDown);

                    // Kiểm tra file có hợp lệ không
                    if (!IsValidZip(pathDown))
                        throw new InvalidDataException("File tải về không hợp lệ hoặc không phải file ZIP.");

                    // Giải nén
                    ZipFile.ExtractToDirectory(pathDown, "C:\\", Encoding.UTF8, true);

                    SetEnvironmentVariables(); // Tùy bạn định nghĩa
                }
                catch (Exception ex)
                {

                }
            }

            // Sau 5 lần thử vẫn không có thư mục DTAHelper
            if (!Directory.Exists(dtaHelperPath))
            {
                throw new Exception($"Chưa cài thư viện! Hãy tải bằng tay: {linkDTA} rồi giải nén vào C:\\DTAHelper");
            }
        }
        private static bool IsValidZip(string filePath)
        {
            try
            {
                using (var archive = ZipFile.OpenRead(filePath))
                {
                    return archive.Entries.Count > 0;
                }
            }
            catch
            {
                return false;
            }
        }
        private static void GithubDown(string url, string file)
        {
            try
            {
                // Kiểm tra tham số đầu vào
                if (string.IsNullOrWhiteSpace(url))
                {
                    LogHelper.Error($"URL cannot be null or empty. {nameof(url)}");
                    throw new ArgumentException("URL cannot be null or empty.", nameof(url));

                }
                if (string.IsNullOrWhiteSpace(file))
                {
                    LogHelper.Error("File path cannot be null or empty." + nameof(file));
                    throw new ArgumentException("File path cannot be null or empty.", nameof(file));
                }

                string path = Path.GetDirectoryName(file);

                // Kiểm tra đường dẫn hợp lệ
                if (string.IsNullOrEmpty(path))
                {
                    LogHelper.Error($"Invalid file path: {file}");
                    throw new InvalidOperationException($"Invalid file path: {file}");
                }

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                if (File.Exists(file))
                {
                    return;
                }

                using (var client = new WebClient())
                {
                    client.Headers.Add("user-agent", "Hell");
                    client.DownloadFile(url, file);
                }
            }
            catch (Exception e)
            {
                LogHelper.Error(e.ToString());
                // Xóa file nếu gặp lỗi
                if (!string.IsNullOrWhiteSpace(file) && File.Exists(file))
                {
                    File.Delete(file);
                }

                // Re-throw ngoại lệ để theo dõi
                throw;
            }
        }
        static void SetEnvironmentVariables()
        {
            // Thiết lập JAVA_HOME
            string javaHome = @"C:\DTAHelper\java"; // Đảm bảo bạn đã cài đặt JDK đúng vị trí
            Environment.SetEnvironmentVariable("JAVA_HOME", javaHome, EnvironmentVariableTarget.Machine);

            // Thiết lập ANDROID_HOME
            string androidHome = @"C:\DTAHelper\sdk"; // Đảm bảo SDK Tools nằm ở đây
            Environment.SetEnvironmentVariable("ANDROID_HOME", androidHome, EnvironmentVariableTarget.Machine);

            // Thêm vào Path
            string pathVariable = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
            if (!pathVariable.Contains(javaHome + @"\bin"))
            {
                Environment.SetEnvironmentVariable("Path", pathVariable + ";" + javaHome + @"\bin", EnvironmentVariableTarget.Machine);
            }

            string sdkPath = @"C:\DTAHelper\sdk\platform-tools";
            if (!pathVariable.Contains(sdkPath))
            {
                Environment.SetEnvironmentVariable("Path", pathVariable + ";" + sdkPath, EnvironmentVariableTarget.Machine);
            }
        }

        private ADBClient _client;
        public ADBHelper(ADBClient client)
        {
            _client = client;
        }
        public string Shell(string cmd, int timeout = 10)
        {
            int retryCount = 0;
            const int maxRetries = 3;

            while (retryCount < maxRetries)
            {
                try
                {
                    string command = $"-s {_client.Device.Serial} shell {cmd}";
                    ProcessHelper.CommandExecutionResult executionResult = ProcessHelper.RunAdbWithResult(command, timeout);
                    string output = executionResult.Output.Trim();
                    string error = executionResult.Error.Trim();

                    if (executionResult.TimedOut)
                    {
                        retryCount++;
                        continue;
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        // Xử lý các lỗi cụ thể
                        if (error.Contains("daemon not running", StringComparison.OrdinalIgnoreCase)
                            && !error.Contains("daemon started successfully", StringComparison.OrdinalIgnoreCase))
                        {
                            StartServer();
                            retryCount++;
                            continue;
                        }

                        if (Regex.IsMatch(error, "device (.*?) not found")
                            || error.Contains("device offline", StringComparison.OrdinalIgnoreCase))
                        {
                            retryCount++;
                            if (!cmd.Contains("reconnect", StringComparison.OrdinalIgnoreCase))
                            {
                                Shell("reconnect");
                            }
                            if (!GetDevices().Contains(_client.Device.Serial))
                            {
                                _client.Connect();
                            }
                            continue;
                        }

                        if (!GetDevices().Contains(_client.Device.Serial))
                        {
                            _client.Connect();
                        }
                    }

                    return output;
                }
                catch (Exception ex)
                {
                    _client.LogHelper.ERROR(ex.Message);
                    retryCount++;
                }
            }
            return string.Empty;
        }
        public string CMD(string cmd, int timeout = 10)
        {
            int retryCount = 0;
            const int maxRetries = 3;

            while (retryCount < maxRetries)
            {
                try
                {
                    string command = $"-s {_client.Device.Serial} {cmd}";
                    ProcessHelper.CommandExecutionResult executionResult = ProcessHelper.RunAdbWithResult(command, timeout);
                    string output = executionResult.Output.Trim();
                    string error = executionResult.Error.Trim();

                    if (executionResult.TimedOut)
                    {
                        retryCount++;
                        continue;
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        // Xử lý các lỗi cụ thể
                        if (error.Contains("daemon not running", StringComparison.OrdinalIgnoreCase)
                            && !error.Contains("daemon started successfully", StringComparison.OrdinalIgnoreCase))
                        {
                            StartServer();
                            retryCount++;
                            continue;
                        }

                        if (Regex.IsMatch(error, "device (.*?) not found")
                            || error.Contains("device offline", StringComparison.OrdinalIgnoreCase))
                        {
                            retryCount++;
                            if (!cmd.Contains("reconnect", StringComparison.OrdinalIgnoreCase))
                            {
                                Shell("reconnect");
                            }
                            if (!GetDevices().Contains(_client.Device.Serial))
                            {
                                _client.Connect();
                            }
                            continue;
                        }

                        if (!GetDevices().Contains(_client.Device.Serial))
                        {
                            _client.Connect();
                        }
                    }

                    return output;
                }
                catch (Exception ex)
                {
                    _client.LogHelper.ERROR(ex.Message);
                    retryCount++;
                }
            }
            return string.Empty;
        }
    }
}

