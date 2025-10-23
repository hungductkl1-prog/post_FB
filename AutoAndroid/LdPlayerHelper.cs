using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System.Diagnostics;

namespace AutoAndroid
{
    public class LdPlayerHelper
    {
        public static string GetPathFolder()
        {
            try
            {
                var processes = Process.GetProcessesByName("dnplayer");

                if (processes.Length == 0)
                {

                    return "";
                }

                foreach (var proc in processes)
                {
                    try
                    {
                        return proc.MainModule.FileName;
                    }
                    catch 
                    {
                       
                    }
                }
            }
            catch
            {

            }
            return "";
        }

        public static bool Config(string configPath)
        {
            if (!File.Exists(configPath))
            {
                Console.WriteLine("File không tồn tại.");
                return true;
            }

            string json = File.ReadAllText(configPath);
            JObject obj = JObject.Parse(json);

            // Giá trị mong muốn
            int expectedDpi = 560;
            int expectedWidth = 1440;
            int expectedHeight = 2560;

            // Lấy giá trị hiện tại
            int? currentDpi = (int?)obj["advancedSettings.resolutionDpi"];
            int? currentWidth = (int?)obj["advancedSettings.resolution"]?["width"];
            int? currentHeight = (int?)obj["advancedSettings.resolution"]?["height"];

            // Kiểm tra có cần update không
            bool needUpdate =
                currentDpi != expectedDpi ||
                currentWidth != expectedWidth ||
                currentHeight != expectedHeight;

            if (!needUpdate)
            {
                Console.WriteLine("Config đã đúng, không cần ghi lại.");
                return false;
            }

            // Cập nhật giá trị
            obj["advancedSettings.resolutionDpi"] = expectedDpi;
            obj["advancedSettings.resolution"] = new JObject
            {
                ["width"] = expectedWidth,
                ["height"] = expectedHeight
            };

            // Ghi lại file
            File.WriteAllText(configPath, obj.ToString(Formatting.Indented));
            return true;
        }
        public static void Close(string ldconsolePath, string index)
        {
            string cmd = $"\"{ldconsolePath}\" quit  --index {index}";
            ProcessHelper.RunRawCmd(cmd);
        }
        public static void Open(string ldconsolePath, string index)
        {
            string cmd = $"\"{ldconsolePath}\" launch  --index {index}";
            ProcessHelper.RunRawCmd(cmd);
        }
        public static void SortWnd(string ldconsolePath)
        {
            string cmd = $"\"{Path.Combine(ldconsolePath, "ldconsole.exe")}\" sortWnd";
            ProcessHelper.RunRawCmd(cmd);
        }
        public static List<int> GetIndex(string ldconsolePath)
        {
            List<int> runningIndexes = new List<int>();

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = ldconsolePath,
                Arguments = "list2",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            using (var reader = process.StandardOutput)
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');
                    if (parts.Length >= 5 && parts[4] == "1") // isRunning == 1
                    {
                        if (int.TryParse(parts[0], out int index))
                        {
                            runningIndexes.Add(index);
                        }
                    }
                }
            }
            return runningIndexes;
        }
    }
}
