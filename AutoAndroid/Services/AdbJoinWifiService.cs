using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoAndroid
{
    public class AdbJoinWifiService
    {
        public static string PathApk = Path.Combine(AppContext.BaseDirectory, "App", "adbjoinwifi.apk");
        public static string Package = "com.steinwurf.adbjoinwifi";
        private ADBClient _service;
        public AdbJoinWifiService(ADBClient service)
        {
            _service = service;
        }
        public bool Install()
        {
            for (int i = 0; i < 20; i++)
            {
                var result = _service.AppList();
                if (result.Contains(Package))
                {
                    break;
                }
                _service.InstallApp(PathApk);
            }
            return true;
        }
        public async Task<bool> ConnectToWifiNetwork(string wifiSSID, string wifiPassword)
        {
            Install();
            _service.Shell("su -c 'svc wifi enable'");
            _service.StopApp("com.steinwurf.adbjoinwifi");
            var s = _service.Shell("am start -n com.steinwurf.adbjoinwifi/.MainActivity -e ssid '" + wifiSSID + "' -e password_type WPA -e password '" + wifiPassword + "'");
            return true;
        }
        public async Task DisableWifi()
        {
            _service.Shell("su -c 'svc wifi disable'");
        }
        public async Task EnableWifi()
        {
            _service.Shell("su -c 'svc wifi enable'");
        }

    }
}
