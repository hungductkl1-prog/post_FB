using AutoAndroid;
using SharpAdbClient;
using StreamAndroid.Services;
using System.IO.Compression;

namespace StreamAndroid.Helper
{
    public class SetupHelper
    {
        public static async Task Setup()
        {
            ProcessHelper.ADBPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs\\");
            string fileip = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs.zip");
            for (int i = 0; i < 3; i++)
            {
                if (!File.Exists(Path.Combine(ProcessHelper.ADBPath, "adb.exe")))
                {
                    InitHelper.GithubDown("https://raw.githubusercontent.com/LamLe2001/changer/main/Libs.zip", fileip);
                    if (File.Exists(fileip))
                    {
                        ZipFile.ExtractToDirectory(fileip, Path.Combine(AppDomain.CurrentDomain.BaseDirectory));
                        File.Delete(fileip);
                    }
                    continue;
                }
                break;
            }
            if (!File.Exists(Path.Combine(ProcessHelper.ADBPath, "adb.exe")))
            {
                throw new FileNotFoundException("Không tìm thấy adb.exe trong thư mục Libs.");
            }
            for (int i = 0; i < 3; i++)
            {
                if (!File.Exists(Path.Combine(Scrcpy.ScrcpyServerFile)))
                {
                    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App"));
                    InitHelper.GithubDown("https://raw.githubusercontent.com/LamLe2001/changer/main/scrcpy-server.jar", Scrcpy.ScrcpyServerFile);
                    continue;
                }
                break;
            }
            if (!File.Exists(Path.Combine(Scrcpy.ScrcpyServerFile)))
            {
                throw new FileNotFoundException("Không tìm thấy scrcpy-server.jar trong thư mục App.");
            }
            AdbServer.Instance.StartServer(Path.Combine(ProcessHelper.ADBPath, "adb.exe"), false);
            FFmpeg.AutoGen.ffmpeg.RootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs");

        }
    }
}
