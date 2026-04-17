using System.Text.Json;
using System.Text.Json.Nodes;
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
                Debug.WriteLine("File không tồn tại.");
                return true;
            }

            string json = File.ReadAllText(configPath);
            JsonObject obj = JsonNode.Parse(json)!.AsObject();

            // Giá trị mong muốn
            int expectedDpi = 560;
            int expectedWidth = 1440;
            int expectedHeight = 2560;

            // Lấy giá trị hiện tại
            int? currentDpi = obj["advancedSettings.resolutionDpi"]?.GetValue<int>();
            int? currentWidth = obj["advancedSettings.resolution"]?["width"]?.GetValue<int>();
            int? currentHeight = obj["advancedSettings.resolution"]?["height"]?.GetValue<int>();

            // Kiểm tra có cần update không
            bool needUpdate =
                currentDpi != expectedDpi ||
                currentWidth != expectedWidth ||
                currentHeight != expectedHeight;

            if (!needUpdate)
            {
                Debug.WriteLine("Config đã đúng, không cần ghi lại.");
                return false;
            }

            // Cập nhật giá trị
            obj["advancedSettings.resolutionDpi"] = expectedDpi;
            obj["advancedSettings.resolution"] = new JsonObject
            {
                ["width"] = expectedWidth,
                ["height"] = expectedHeight
            };

            // Ghi lại file
            File.WriteAllText(configPath, obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return true;
        }
        public static void Close(string ldDebugPath, string index)
        {
            string cmd = $"\"{ldDebugPath}\" quit  --index {index}";
            ProcessHelper.RunRawCmd(cmd);
        }
        public static void Open(string ldDebugPath, string index)
        {
            string cmd = $"\"{ldDebugPath}\" launch  --index {index}";
            ProcessHelper.RunRawCmd(cmd);
        }
        public static void SortWnd(string ldDebugPath)
        {
            string cmd = $"\"{Path.Combine(ldDebugPath, "ldDebug.exe")}\" sortWnd";
            ProcessHelper.RunRawCmd(cmd);
        }
        public static List<int> GetIndex(string ldDebugPath)
        {
            List<int> runningIndexes = new List<int>();

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = ldDebugPath,
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
