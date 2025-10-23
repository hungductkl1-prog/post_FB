using System.Diagnostics;
using System;
using System.Net;
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
        }
        public static void KillServer()
        {
            ProcessHelper.RunAdbWithTimeout($"kill-server", 5);
        }
        public static void Restart()
        {
            ProcessHelper.RunAdbWithTimeout($"kill-server", 5);
            ProcessHelper.RunAdbWithTimeout($"start-server", 5);
        }
        public static List<string> GetDevices()
        {
            // Chạy lệnh adb devices và lấy kết quả trả về
            var devicesOutput = ProcessHelper.RunAdbWithTimeout($"devices", 20);

            // Chia kết quả theo dòng
            var lines = devicesOutput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Bỏ dòng đầu tiên vì đó là tiêu đề "List of devices attached"
            return lines
                .Skip(1) // Bỏ qua dòng đầu tiên
                .Where(line => line.Contains("\tdevice")) // Chỉ chọn các dòng chứa "device"
                .Select(line => line.Split('\t')[0]) // Lấy ID thiết bị (phần trước "\t")
                .ToList();
        }
        public static void InitADB()
        {
            string dtaHelperPath = @"C:\DTAHelper";
            string linkDTA = "https://www.dropbox.com/scl/fi/3bediza9mih9gmekxqi4n/DTAHelper.zip?rlkey=0igvcdpqde4j9lnl1kvpe1qa2&st=41hq907c&dl=1";
            string pathDown = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DTAHelper.zip");

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
            string result = "";
            int retryCount = 0;
            const int maxRetries = 3;

            while (retryCount < maxRetries)
            {
                try
                {
                    using (Process process = new Process())
                    {
                        string command = " -s " + _client.Device.Serial + " shell " + cmd;
                        process.StartInfo.FileName = "cmd.exe";
                        //process.StartInfo.Arguments = "/c " + Path.Combine(
                        //    Environment.GetEnvironmentVariable("ANDROID_HOME", EnvironmentVariableTarget.Machine),
                        //    "platform-tools",
                        //    "adb") + command;
                        process.StartInfo.Arguments = $"/C \"{ProcessHelper.ADBPath}adb {command}\"";


                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                        process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                        process.Start();

                        // Đọc dữ liệu đồng bộ
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        // Đợi tiến trình hoàn thành trong thời gian quy định
                        if (!process.WaitForExit(timeout * 1000))
                        {
                            process.Kill();
                            retryCount++;
                            continue;
                        }

                        if (!string.IsNullOrEmpty(error))
                        {
                            // Xử lý các lỗi cụ thể
                            if (error.Contains("daemon not running") && !error.Contains("daemon started successfully"))
                            {
                                StartServer();
                                retryCount++;
                                continue;
                            }

                            if (Regex.IsMatch(error, "device (.*?) not found") || error.Contains("device offline"))
                            {
                                retryCount++;
                                if (!cmd.Contains("reconnect"))
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

                        return output.Trim(); // Trả về kết quả nếu không có lỗi
                    }
                }
                catch (Exception ex)
                {
                    _client.LogHelper.ERROR(ex.Message);
                    retryCount++;
                }
            }
            return result;
        }
        public string CMD(string cmd, int timeout = 10)
        {
            string result = "";
            int retryCount = 0;
            const int maxRetries = 3;

            while (retryCount < maxRetries)
            {
                try
                {
                    using (Process process = new Process())
                    {
                        string command = " -s " + _client.Device.Serial + " " + cmd;
                        process.StartInfo.FileName = "cmd.exe";
                        //process.StartInfo.Arguments = "/c " + Path.Combine(
                        //    Environment.GetEnvironmentVariable("ANDROID_HOME", EnvironmentVariableTarget.Machine),
                        //    "platform-tools",
                        //    "adb") + command;
                        process.StartInfo.Arguments = $"/C \"{ProcessHelper.ADBPath}adb {command}\"";


                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                        process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                        process.Start();

                        // Đọc dữ liệu đồng bộ
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        // Đợi tiến trình hoàn thành trong thời gian quy định
                        if (!process.WaitForExit(timeout * 1000))
                        {
                            process.Kill();
                            retryCount++;
                            continue;
                        }

                        if (!string.IsNullOrEmpty(error))
                        {
                            // Xử lý các lỗi cụ thể
                            if (error.Contains("daemon not running") && !error.Contains("daemon started successfully"))
                            {
                                StartServer();
                                retryCount++;
                                continue;
                            }

                            if (Regex.IsMatch(error, "device (.*?) not found") || error.Contains("device offline"))
                            {
                                retryCount++;
                                if (!cmd.Contains("reconnect"))
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

                        return output.Trim(); // Trả về kết quả nếu không có lỗi
                    }
                }
                catch (Exception ex)
                {
                    _client.LogHelper.ERROR(ex.Message);
                    retryCount++;
                }
            }
            return result;
        }
    }
}
