using System.Text.RegularExpressions;

namespace AutoAndroid
{
    public class ClipboardService
    {
        public const string Package = "ca.zgrs.clipper";
        public static string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App", "clipper.apk");
        ADBClient _client;
        public ClipboardService(ADBClient client)
        {
            _client = client;
        }
        public async Task<string> GetText()
        {
            if (!_client.AppList().Contains(Package))
            {
                if (!File.Exists(path))
                {
                    string folderName = Path.GetFileName(Path.GetDirectoryName(path));
                    Directory.CreateDirectory(folderName);
                    InitHelper.GithubDown("https://raw.githubusercontent.com/LamLe2001/changer/main/clipper.apk", path);
                }
                _client.InstallApp(path);
            }
            _client.Delay(3);
            _client.Shell($"am startservice ca.zgrs.clipper/.ClipboardService");
            _client.Delay(1);
            string adbOutput = _client.ADB.Shell($"am broadcast -a clipper.get");
            var match = Regex.Match(adbOutput, @"data=""(.*)""$");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
    }
}
