using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAndroid
{
    public class ChangeLanguageService
    {
        public const string Package = "net.sanapeli.adbchangelanguage";
        public static string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App", "changelanguage.apk");
        ADBClient _client;
        public ChangeLanguageService(ADBClient client)
        {
            _client = client;
        }
        public async Task Change(string language, string country)
        {
            if (!_client.AppList().Contains(Package))
            {
                if (!File.Exists(path))
                {
                    string folderName = Path.GetFileName(Path.GetDirectoryName(path));
                    Directory.CreateDirectory(folderName);
                    InitHelper.GithubDown("https://raw.githubusercontent.com/LamLe2001/changer/main/app-changelanguage.apk", path);
                }
                _client.InstallApp(path);
            }
             _client.Delay(3);
            _client.Shell($"pm grant net.sanapeli.adbchangelanguage android.permission.CHANGE_CONFIGURATION");
             _client.Delay(1);
            _client.Shell($"am start -n net.sanapeli.adbchangelanguage/.AdbChangeLanguage -e language {language} -e country {country}");
        }
    }
}
