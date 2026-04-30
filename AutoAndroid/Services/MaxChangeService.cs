using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace AutoAndroid
{
    public class MaxChangeService
    {
        ADBClient service;
        private readonly string path_MaxChange = Path.Combine(AppContext.BaseDirectory, "App", "GolikeHelper.apk");
        private readonly string path_DeviceInfoHW = Path.Combine(AppContext.BaseDirectory, "App", "DeviceInfoHW.apk");
        public static string package_MaxChange = "com.golike.helper";
        private readonly string package_Deviceinfohw = "ru.andr7e.deviceinfohw";
        public MaxChangeService(ADBClient service)
        {
            this.service = service;
        }
        private void Close()
        {
            service.StopApp(package_MaxChange);

        }
        public void Open()
        {
            for (int i = 0; i < 5; i++)
            {
                service.LogHelper.SUCCESS("Đang mở ứng dụng GolikeHelper");
                service.AppStart(package_MaxChange, true, true, wait: true);
                service.SetSize();
                if (service.AppWait(package_MaxChange))
                {
                    service.LogHelper.SUCCESS("Đã mở ứng dụng GolikeHelper");
                    break;
                }
            }

        }
        public async Task<bool> Install()
        {
            List<string> list = service.AppList();
            for (int i = 0; i < 5; i++)
            {
                if (list.Contains(package_Deviceinfohw) && list.Contains(package_MaxChange))
                {
                    break;
                }
                service.LogHelper.SUCCESS("Cài đặt GolikeHelper");
                list = service.AppList();
                if (!list.Contains(package_Deviceinfohw))
                {
                    if (!File.Exists(path_DeviceInfoHW))
                    {
                        string dirPath = Path.GetDirectoryName(path_DeviceInfoHW) ?? Path.GetDirectoryName(AppContext.BaseDirectory)!;
                        Directory.CreateDirectory(dirPath);
                        InitHelper.GithubDown("https://raw.githubusercontent.com/LamLe2001/changer/main/app-deviceinfohw.apk", path_DeviceInfoHW);
                    }
                    service.InstallApp(path_DeviceInfoHW);
                }
                if (!list.Contains(package_MaxChange))
                {
                    if (!File.Exists(path_MaxChange))
                    {
                        string dirPath = Path.GetDirectoryName(path_MaxChange) ?? Path.GetDirectoryName(AppContext.BaseDirectory)!;
                        Directory.CreateDirectory(dirPath);
                        InitHelper.GithubDown("https://raw.githubusercontent.com/LamLe2001/changer/main/GolikeHelper.apk", path_MaxChange);
                    }
                    service.InstallApp(path_MaxChange);
                }
                service.Delay(1);
            }
            service.Shell($"pm grant {package_MaxChange} android.permission.READ_EXTERNAL_STORAGE");
            service.Shell($"pm grant {package_MaxChange} android.permission.WRITE_EXTERNAL_STORAGE");
            SetEnableModule();
            ResetWallpaperDefault();
            return true;
        }
        public async Task<bool> Change(string filePath, bool backup, string brand, string country)
        {
            await Install();
            bool flag = false;
            if (Restore(filePath))
            {
                string text = GetInfoDeviceName(10);
                flag = text != "";
            }
            if (!flag)
            {
                ChangeDeviceName(brand, country);
            }
            return flag;
        }
        private bool Restore(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return false;
            }
            string fileName = Path.GetFileName(filePath);
            service.LogHelper.SUCCESS($"Restore device: {filePath}");
            service.ADB.Shell($"pm grant {package_MaxChange} android.permission.READ_EXTERNAL_STORAGE");
            service.ADB.Shell($"pm grant {package_MaxChange} android.permission.WRITE_EXTERNAL_STORAGE");
            bool flag = false;
            for (int i = 0; i < 10; i++)
            {
                service.Push(filePath, "/sdcard");
                string text2 = service.ADB.Shell($"su -c cp /sdcard/{fileName} /data/data/{package_MaxChange}/{fileName}");
                text2 = service.ADB.Shell($"su -c tar -xzvf /data/data/{package_MaxChange}/{fileName}");
                string text = "awk '{print $3\\\":\\\"$4}'\"";
                text2 = service.ADB.Shell($"su -c \"ls -l /data/data | grep {package_MaxChange} | {text}");
                flag = text2 != "";
                text2 = service.ADB.Shell($"su -c chown -R " + text2 + $" /data/data/{package_MaxChange}");
                if (!flag)
                {
                    Thread.Sleep(2000);
                    continue;
                }
                return true;
            }
            return false;
        }
        public string GetInfoDeviceName(int waitTimeInSeconds = 0)
        {
            //AppendLog("Get device name fake...");
            int startTime = Environment.TickCount;

            do
            {
                // Execute shell command to read the content of Device.xml,
                // which located within the shared_prefs directory of the {package_MaxChange} application.
                string fileContent = service.Shell($" su -c 'cat /data/data/{package_MaxChange}/shared_prefs/Device.xml'");

                if (fileContent != "")
                {
                    try
                    {
                        // Load the content of Device.xml into an XmlDocument
                        XmlDocument xmlDocument = new XmlDocument();
                        xmlDocument.LoadXml(fileContent);

                        // Extract the values of 'fingerprint' and 'time_check' from the loaded XML
                        XmlNode fingerprintNode = xmlDocument.SelectSingleNode("//*[@name='fingerprint']");
                        XmlNode timeCheckNode = xmlDocument.SelectSingleNode("//*[@name='time_check']");

                        // If both nodes are found and they have values, concatenate those values 
                        if (fingerprintNode != null && fingerprintNode.InnerText != "" &&
                            timeCheckNode != null && timeCheckNode.InnerText != "")
                        {
                            string deviceInfo = fingerprintNode.InnerText + timeCheckNode.InnerText;
                            service.LogHelper.SUCCESS($"{deviceInfo}");
                            return deviceInfo;
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore exceptions, which might be due to incorrect XML format or missing elements.
                    }
                }

                if (waitTimeInSeconds == 0)
                {
                    break;
                }

                Thread.Sleep(2000);
            } while (Environment.TickCount - startTime < waitTimeInSeconds * 1000);

            return "";
        }

        public bool ChangeDeviceName(string brand, string country)
        {
            string countryCode = string.Empty;
            string countryName = string.Empty;
            if (string.IsNullOrEmpty(country) || country == "Random")
            {
                countryCode = "84";
                countryName = "VN";
            }
            try
            {
                service.ADB.Shell($"pm grant {package_MaxChange} android.permission.READ_EXTERNAL_STORAGE");
                service.ADB.Shell($"pm grant {package_MaxChange} android.permission.WRITE_EXTERNAL_STORAGE");

                string text2 = $"am broadcast -a {package_MaxChange}.CHANGE -n {package_MaxChange}/.AdbCaller --ez on true";
                bool flag2 = service.ADB.Shell(text2).Contains("Broadcast completed");
                if (flag2)
                {
                    string text4 = GetInfoDeviceName(10);
                    service.LogHelper.SUCCESS($"Thay đổi thành công [{text4}]");
                    return flag2;
                }
            }
            catch (Exception ex)
            {
                service.LogHelper.ERROR(ex.Message);
            }

            return true;
        }
        public async Task<bool> BackupDeviceInfoChange(string pathDevice)
        {
            string fileName = Path.GetFileName(pathDevice);
            service.LogHelper.SUCCESS($"Backup change device: {pathDevice}");
            string directoryPath = Path.GetDirectoryName(pathDevice);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            FileHelper.DeleteFile(pathDevice);
            //  Close();
            for (int i = 0; i < 10; i++)
            {
                service.ADB.Shell($" su -c tar -czvf /data/data/{package_MaxChange}/{fileName} /data/data/{package_MaxChange}/shared_prefs/*");
                service.ADB.Shell($" su -c cp /data/data/{package_MaxChange}/{fileName} /sdcard/{fileName}");
                ProcessHelper.RunAdbWithTimeout($" -s {service.Device.Serial} pull /sdcard/{fileName} \"" + pathDevice + "\"");
                service.ADB.Shell($"su -c rm -rf /data/data/{package_MaxChange}/*.tar.gz");
                service.ADB.Shell($" su -c rm -rf /sdcard/*.tar.gz");
                if (!File.Exists(pathDevice))
                {
                    await Task.Delay(3000);
                    continue;
                }
                return true;
            }
            return false;
        }
        private List<Modules> GetModules()
        {
            string command = "su -c \"sqlite3 /data/adb/lspd/config/modules_config.db 'SELECT mid, module_pkg_name, enabled FROM modules;'\"";
            string output = service.Shell(command);

            List<Modules> modules = new List<Modules>();
            output = service.Shell(command);
            // Phân tách chuỗi kết quả và tạo đối tượng Module
            string[] rows = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string row in rows)
            {
                var columns = row.Split('|');
                if (columns.Length == 3)
                {
                    Modules module = new Modules
                    {
                        mid = int.Parse(columns[0].Trim()),
                        modulePkgName = columns[1].Trim(),
                        enabled = int.Parse(columns[2].Trim())
                    };
                    modules.Add(module);
                }
            }

            return modules;
        }
        private List<Scope> GetScopes()
        {
            string command = "su -c \"sqlite3 /data/adb/lspd/config/modules_config.db 'SELECT mid, app_pkg_name, user_id FROM scope;'\"";
            string output = service.Shell(command);

            List<Scope> modules = new List<Scope>();

            // Phân tách chuỗi kết quả và tạo đối tượng Module
            string[] rows = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string row in rows)
            {
                var columns = row.Split('|');
                if (columns.Length == 3)
                {
                    Scope module = new Scope
                    {
                        mid = int.Parse(columns[0].Trim()),
                        app_pkg_name = columns[1].Trim(),
                        user_id = int.Parse(columns[2].Trim())
                    };
                    modules.Add(module);
                }
            }

            return modules;
        }
        private void UpdateModules(Modules module)
        {
            string command = $"su -c \"sqlite3 /data/adb/lspd/config/modules_config.db 'UPDATE modules SET enabled = 1 WHERE module_pkg_name = \\\"{module.modulePkgName}\\\";'\"\r\n";
            string output = service.Shell(command);
        }
        private void addMissingScopes(Modules module)
        {
            string[] requiredApps = { MaxChangeService.package_MaxChange, "com.facebook.katana", "com.instagram.android", "ru.andr7e.deviceinfohw" };
            foreach (string requiredApp in requiredApps)
            {
                string command = $"su -c \"sqlite3 /data/adb/lspd/config/modules_config.db 'INSERT INTO scope (mid, app_pkg_name, user_id) VALUES ({module.mid}, \\\"{requiredApp}\\\", 0);'\"";
                string output = service.Shell(command);
            }
        }
        public void SetEnableModule()
        {
            List<Modules> modules = GetModules();
            if (!modules.Any())
            {
                modules = GetModules();
                service.LogHelper.ERROR("Chưa cài LSPosed");
                return;
            }
            Modules? targetModule = modules.FirstOrDefault(x => x.modulePkgName == package_MaxChange);
            if (targetModule == null)
            {
                service.LogHelper.ERROR("Chưa cài DTAChange");
                return;
            }
            if (targetModule.enabled == 0)
            {
                UpdateModules(targetModule);
            }
            addMissingScopes(targetModule);
        }
        public async Task<string> GetIP(string state = "")
        {
           service.LogHelper.State = state;
            string ip = "";
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    await Install();
                    service.LogHelper.SUCCESS($"Đang kiểm tra IP [{i + 1}]");
                    service.AppStart(package_MaxChange);
                    string result = ProcessHelper.RunAdbWithTimeout($"-s {service.Device.Serial} shell am broadcast -a {package_MaxChange}.GET_DEVICE_IP -n {package_MaxChange}/.AdbCaller");
                    if (result.Contains("result=1"))
                    {
                        Match match = Regex.Match(result, @"data=""(?<json>\{.*?\})""");
                        if (match.Success)
                        {
                            var json = match.Groups[1].Value;
                            var doc = System.Text.Json.JsonDocument.Parse(json);

                            string message = doc.RootElement.GetProperty("ip").GetString();
                            service.LogHelper.SUCCESS($"IP: {message}");
                            return message;
                        }

                    }
                    ip = "";
                    service.LogHelper.ERROR("Không lấy được IP");
                }
                catch (Exception ex)
                {
                    service.LogHelper.ERROR("Lỗi khi check IP: " + ex.Message);
                }
            }
            return ip;
        }
        public bool ChangeWallpaper(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                service.LogHelper.ERROR($"File hình nền không tồn tại: {filePath}");
                return false;
            }
            try
            {
                string fileName = Path.GetFileName(filePath);
                string remotePath = $"/sdcard/{fileName}";

                service.LogHelper.SUCCESS($"Đổi hình nền: {fileName}");
                service.Push(filePath, "/sdcard");

                string command = $"am broadcast -a {package_MaxChange}.WALLPAPER -n {package_MaxChange}/.AdbCaller --es path {remotePath}";
                string result = service.ADB.Shell(command);

                bool success = result.Contains("Broadcast completed");
                if (success)
                {
                    service.LogHelper.SUCCESS("Đã đổi hình nền thành công");
                }
                else
                {
                    service.LogHelper.ERROR($"Đổi hình nền thất bại: {result}");
                }
                return success;
            }
            catch (Exception ex)
            {
                service.LogHelper.ERROR("Lỗi khi đổi hình nền: " + ex.Message);
                return false;
            }
        }
        public bool ResetWallpaperDefault()
        {
            try
            {
                service.LogHelper.SUCCESS("Reset hình nền mặc định");

                string command = $"am broadcast -a {package_MaxChange}.WALLPAPER_DEFAULT -n {package_MaxChange}/.AdbCaller";
                string result = service.ADB.Shell(command);

                bool success = result.Contains("Broadcast completed");
                if (success)
                {
                    service.LogHelper.SUCCESS("Đã reset hình nền mặc định thành công");
                }
                else
                {
                    service.LogHelper.ERROR($"Reset hình nền thất bại: {result}");
                }
                return success;
            }
            catch (Exception ex)
            {
                service.LogHelper.ERROR("Lỗi khi reset hình nền: " + ex.Message);
                return false;
            }
        }
    }
    public class Modules
    {
        public int mid { get; set; }
        public string modulePkgName { get; set; }
        public int enabled { get; set; }
    }
    public class Scope
    {
        public int mid;
        public string app_pkg_name;
        public int user_id;
    }
}
