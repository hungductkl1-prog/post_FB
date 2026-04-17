using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAndroid
{
    public class ADBKeyboardService
    {
        public static string Path_Keyboard = Path.Combine(AppContext.BaseDirectory, "App", "ADBKeyboard.apk");
        public static string Package_Keyboard = "com.android.adbkeyboard";
        private ADBClient _service;
        public ADBKeyboardService(ADBClient service)
        {
            _service = service;
        }
        public async Task<bool> TurnOnADBKeyboard()
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    if (!_service.AppList().Contains(Package_Keyboard))
                    {
                        if (!File.Exists(Path_Keyboard))
                        {
                            string folderName = Path.GetFileName(Path.GetDirectoryName(Path_Keyboard));
                            Directory.CreateDirectory(folderName);
                            InitHelper.GithubDown("https://raw.githubusercontent.com/LamLe2001/changer/main/ADBKeyboard.apk", Path_Keyboard);
                        }
                        _service.InstallApp(Path_Keyboard);
                    }
                    string text = _service.Shell("ime set com.android.adbkeyboard/.AdbIME");
                    if (text.Contains("result=0"))
                    {
                        return true;
                    }
                    else if (text.Contains("selected"))
                    {
                        return true;
                    }
                    _service.Shell("am start -a android.settings.INPUT_METHOD_SETTINGS");
                    int tickCount = Environment.TickCount;
                    while (true)
                    {
                        string text2 = _service.GetXMLSource();
                        string text3 = _service.FindElement(text2, new List<string> { "//*[@text='ADB Keyboard']/parent::*/parent::*/child::*/child::*[@checked='true']", "//*[@text='ADB Keyboard']", "//node[@text='OK']" }, 10);
                        switch (text3)
                        {
                            case "//*[@text='ADB Keyboard']":
                            case "//node[@text='OK']":
                                _service.ElementWithAttributes(text3, 5, text2);
                                break;
                            case "//*[@text='ADB Keyboard']/parent::*/parent::*/child::*/child::*[@checked='true']":
                                text = _service.Shell("ime set com.android.adbkeyboard/.AdbIME");
                                return true;
                        }
                         _service.Delay(1);
                        if (Environment.TickCount - tickCount >= 10000)
                        {
                            break;
                        }
                    }
                }
                catch
                {
               
                }
            }

            return false;
        }

        public bool Input(string text, bool clear = true)
        {
            try
            {
                TurnOnADBKeyboard();
                if (clear)
                {
                    ClearInputWithADBKeyboard();
                }

                var convert = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
                string cmdCommand = $"am broadcast -a ADB_INPUT_B64 --es msg '{convert}'";
                if (_service.Shell(cmdCommand).Contains("result=0"))
                {
                    return true;
                }
            }
            catch
            {
               
            }
            return false;
        }

        public bool ClearInputWithADBKeyboard()
        {
            try
            {
                if (_service.Shell("am broadcast -a ADB_CLEAR_TEXT").Contains("result=0"))
                {
                    return true;
                }
            }
            catch 
            {
               
            }
            return false;
        }
    }
}
