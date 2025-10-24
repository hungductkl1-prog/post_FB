using OpenCvSharp;
using OpenCvSharp.Extensions;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
namespace AutoAndroid
{
    public class ADBClient
    {
        Random random = new Random();
        Stopwatch stopwatch = new Stopwatch();
        public bool Running { get; set; } = true;
        public LogHelper LogHelper
        {
            get => _logHelper ??= new LogHelper(Device);
            set => _logHelper = value;
        }
        private LogHelper _logHelper;
        public ATXService ATX
        {
            get => _atx ??= new ATXService(this);
            set => _atx = value;
        }
        private ATXService _atx;
        public ADBHelper ADB
        {
            get => _adb ??= new ADBHelper(this);
            set => _adb = value;
        }
        private ADBHelper _adb;
        public MaxChangeService maxChange;
        public readonly ADBKeyboardService ADBKeyboardService;
        private readonly ClipboardService _clipboardService;
        public DeviceModel Create(string serial)
        {
            DeviceModel model = new DeviceModel();
            model.Serial = serial;
            using var _client = new ADBSocket(serial);
            try
            {
                model.Id = new DeviceModelContext().GetBySerial(serial).Id;
            }
            catch
            {
            }
           
            model.Port = _client.ForwardPort(7912);
            string name = ProcessHelper.RunAdbWithTimeout($"-s {serial} shell settings get global device_name");
            string version = ProcessHelper.RunAdbWithTimeout($"-s {serial} shell getprop ro.build.version.release");
            model.NameDevice = name;
            model.OS = version;
            model.TypeColor = 0;
            model.Checked = false;
            ProcessHelper.RunAdbWithTimeout($"-s {serial} shell settings put system user_rotation 0");
            ProcessHelper.RunAdbWithTimeout($"-s {serial} shell settings put system accelerometer_rotation 0");
            return model;
        }
        public DeviceModel Device { get; set; }
        public ADBClient(DeviceModel model)
        {
            Device = model;
            _atx = new ATXService(this);
            _logHelper = new LogHelper(Device);
            maxChange = new MaxChangeService(this);
            ADBKeyboardService = new ADBKeyboardService(this);
            _clipboardService = new ClipboardService(this);
            ProcessHelper.RunAdbWithTimeout($"-s {Device.Serial} shell settings put system user_rotation 0");
            ProcessHelper.RunAdbWithTimeout($"-s {Device.Serial} shell settings put system accelerometer_rotation 0");
        }
        public async Task<bool> TurnOnADBKeyboard()
        {
            return await ADBKeyboardService.TurnOnADBKeyboard();
        }
        public async Task EnableWifi()
        {
            _logHelper.SUCCESS($"Bật wiffi");
            Shell("su -c 'svc wifi enable'");
            _logHelper.SUCCESS($"Đã bật wiffi");
        }
        public async Task DisableWifi()
        {
            _logHelper.SUCCESS($"Tắt wiffi");
            Shell("su -c 'svc wifi disable'");
            _logHelper.SUCCESS($"Đã tắt wiffi");
        }
        public ADBClient(string serial)
        {
            Device = Create(serial);
            _atx = new ATXService(this);
            _logHelper = new LogHelper(Device);
            maxChange = new MaxChangeService(this);
        }
        public async Task<string> GetClipboardText()
        {
            return await _clipboardService.GetText();
        }
        public List<string> ExtractBoundsFromXml(int timeoutSeconds, string xmlSource)
        {
            List<string> boundsList = new List<string>();
            try
            {
                int startTick = Environment.TickCount;
                while (true)
                {
                    if (xmlSource == "")
                    {
                        xmlSource = GetXMLSource();
                    }
                    xmlSource = xmlSource.ToLower();
                    MatchCollection matches = Regex.Matches(xmlSource, "bounds=\"(.*?)\"");
                    for (int i = 0; i < matches.Count; i++)
                    {
                        boundsList.Add(matches[i].Groups[1].Value);
                    }
                    if (boundsList.Count <= 0 && timeoutSeconds != 0)
                    {
                        xmlSource = "";
                        if (Environment.TickCount - startTick >= timeoutSeconds * 1000)
                        {
                            break;
                        }
                        continue;
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
            }
            return boundsList.Distinct().ToList();
        }
        public bool FindAndClickImage(string imageDirectory, Bitmap screenBitmap = null, int timeoutSeconds = 0)
        {
            try
            {
                string coordinates = FindImageOnScreen(imageDirectory, screenBitmap, timeoutSeconds);
                if (!string.IsNullOrEmpty(coordinates))
                {
                    var point = new RectangleArea(coordinates).GetCenterPoint();
                    return Click(point.X, point.Y);
                }
            }
            catch
            {
                // Ignore errors
            }

            return false;
        }
        public string FindMatchingImage(int timeoutSeconds, ref Bitmap bitmap, List<string> imageNames)
        {
            int startTick = Environment.TickCount;
            while (true)
            {
                if (bitmap == null)
                {
                    bitmap = Screenshot();
                }
                for (int i = 0; i < imageNames.Count; i++)
                {
                    if (IsImageMatch(imageNames[i], bitmap))
                    {
                        return imageNames[i];
                    }
                }
                if (timeoutSeconds == 0 || Environment.TickCount - startTick > timeoutSeconds * 1000)
                {
                    break;
                }
                Delay(1);
                bitmap = Screenshot();
                continue;
            }
            return "";
        }
        public string FindImageRegion(string imageFolder, Bitmap sourceImage = null, int timeoutSeconds = 0)
        {
            try
            {
                // Load all bitmaps from the directory
                List<Bitmap> referenceImages = new List<Bitmap>();
                DirectoryInfo dir = new DirectoryInfo(imageFolder);
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    Bitmap bmp = (Bitmap)Image.FromFile(file.FullName);
                    referenceImages.Add(bmp);
                }

                int startTick = Environment.TickCount;
                while (true)
                {
                    if (sourceImage == null)
                    {
                        sourceImage = Screenshot();
                    }

                    foreach (Bitmap reference in referenceImages)
                    {
                        Rect matchRegion = FindMatchingRegion(sourceImage, reference);
                        if (matchRegion != new Rect())
                        {
                            return $"[{matchRegion.Left},{matchRegion.Top}][{matchRegion.Right},{matchRegion.Bottom}]";
                        }
                    }

                    if (Environment.TickCount - startTick < timeoutSeconds * 1000)
                    {
                        Delay(1);
                        sourceImage = Screenshot();
                        continue;
                    }
                    break;
                }
            }
            catch (Exception)
            {
                // Consider logging error if needed
            }
            return "";
        }
        public Rect FindMatchingRegion(Bitmap sourceImage, Bitmap templateImage, double threshold = 0.95)
        {
            try
            {
                using Mat sourceMat = sourceImage.ToMat();
                using Mat templateMat = templateImage.ToMat();
                using Mat searchMat = sourceMat.Clone();
                using Mat matchMat = templateMat.Clone();
                using Mat resultMat = new Mat(searchMat.Rows - matchMat.Rows + 1, searchMat.Cols - matchMat.Cols + 1, MatType.CV_32FC1);

                Cv2.MatchTemplate(searchMat, matchMat, resultMat, TemplateMatchModes.CCoeffNormed);
                Cv2.Threshold(resultMat, resultMat, threshold, 1.0, ThresholdTypes.Tozero);

                Cv2.MinMaxLoc(resultMat, out _, out var maxVal, out _, out var maxLoc);
                if (maxVal >= threshold)
                {
                    return new Rect(maxLoc.X, maxLoc.Y, matchMat.Width, matchMat.Height);
                }
            }
            catch (Exception)
            {
                // Optionally log error
            }
            return new Rect();
        }
        public bool IsImageMatch(string imageName, Bitmap bitmap = null, int int_3 = 0)
        {
            try
            {
                string result = FindImageOnScreen(imageName, bitmap, int_3);
                return !string.IsNullOrEmpty(result);
            }
            catch (Exception)
            {
            }
            return false;
        }
        public string FindImageOnScreen(string imageDirectory, Bitmap screenBitmap = null, int timeoutSeconds = 0)
        {
            try
            {
                // Load toàn bộ ảnh mẫu trong thư mục
                List<Bitmap> templates = new List<Bitmap>();
                DirectoryInfo dir = new DirectoryInfo(imageDirectory);
                foreach (FileInfo file in dir.GetFiles())
                {
                    Bitmap template = (Bitmap)System.Drawing.Image.FromFile(file.FullName);
                    templates.Add(template);
                }

                int startTime = Environment.TickCount;

                while (true)
                {
                    if (screenBitmap == null)
                    {
                        screenBitmap = Screenshot();
                    }

                    // So sánh từng template với screenBitmap
                    foreach (Bitmap template in templates)
                    {
                        Rect rect = FindTemplate(screenBitmap, template);
                        if (rect != new Rect())
                        {
                            return $"[{rect.Left},{rect.Top}][{rect.Right},{rect.Bottom}]";
                        }
                    }

                    // Nếu chưa hết timeout thì tiếp tục chụp màn hình mới và retry
                    if (Environment.TickCount - startTime < timeoutSeconds * 1000)
                    {
                        Delay(1);
                        screenBitmap = Screenshot();
                        continue;
                    }

                    // Hết timeout -> không tìm thấy
                    break;
                }
            }
            catch (Exception)
            {
                // Ignore errors
            }

            return "";
        }
        public Rect FindTemplate(Bitmap sourceImage, Bitmap templateImage, double threshold = 0.95)
        {
            try
            {
                using Mat sourceMat = BitmapConverter.ToMat(sourceImage);
                using Mat templateMat = BitmapConverter.ToMat(templateImage);
                using Mat result = new Mat(sourceMat.Rows - templateMat.Rows + 1, sourceMat.Cols - templateMat.Cols + 1, MatType.CV_32FC1);

                // So khớp ảnh
                Cv2.MatchTemplate(sourceMat, templateMat, result, TemplateMatchModes.CCoeffNormed);

                // Áp ngưỡng: chỉ lấy kết quả >= threshold
                Cv2.Threshold(result, result, threshold, 1.0, ThresholdTypes.Tozero);

                // Tìm điểm khớp tốt nhất
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                if (maxVal >= threshold)
                {
                    return new Rect(maxLoc.X, maxLoc.Y, templateMat.Width, templateMat.Height);
                }
            }
            catch (Exception)
            {
                // ignore errors
            }

            return new Rect();
        }
        public int ForwardPort(int remote, int port)
        {
            using var adb = new ADBSocket(Device.Serial);
            return adb.ForwardPort(remote);
        }
        public bool IsRunningApp(string package)
        {
            string dump = string.Empty;
            for (int i = 0; i < 5; i++)
            {
                dump = GetXMLSource();
                if (string.IsNullOrEmpty(dump)) continue;
                if (dump.Contains(package))
                {
                    return true;
                }
            }
            return false;
        }
        public bool RebootAndWaitForDeviceReady()
        {
            LogHelper.SUCCESS("Đang khởi động lại máy!");
            ADB.Shell("reboot");
            LogHelper.SUCCESS("Đang chờ khởi động máy!");
            ADB.Shell("wait-for-device", 120);
            while (!ADB.Shell("getprop sys.boot_completed").Equals("1"))
            {
                LogHelper.SUCCESS("Khởi động máy thành công!");
            }
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < 15000)
            {

                if (Connect())
                {
                    return true;
                }
            }
            return false;
        }
        public void EnabePlane()
        {
            LogHelper.SUCCESS($"Đang bật máy bay");
            Shell("settings put global airplane_mode_on 1");
            Shell("am broadcast -a android.intent.action.AIRPLANE_MODE --ez state true");
            LogHelper.SUCCESS($"Đã bật máy bay");
        }
        public void DisablePlane()
        {
            LogHelper.SUCCESS($"Đang tắt máy bay");
            Shell("settings put global airplane_mode_on 0");
            Shell("am broadcast -a android.intent.action.AIRPLANE_MODE --ez state true");
            LogHelper.SUCCESS($"Đã tắt máy bay");
        }
        public void Enabel4G()
        {
            LogHelper.SUCCESS($"Đang bật 4G");
            Shell("svc data enable");
            LogHelper.SUCCESS($"Đã bật 4G");
        }
        public bool ATXSwipe(int x1, int y1, int x2, int y2, int duration = 100)
        {
            try
            {

                return _atx.Swipe(x1, y1, x2, y2, duration);
            }
            catch
            {
                Connect();
                return ATXSwipe(x1, y1, x2, y2, duration);
            }
        }
        public void Disable4G()
        {
            LogHelper.SUCCESS($"Đang tắt 4G");
            Shell("svc data disable");
            LogHelper.SUCCESS($"Đã tắt 4G");
        }
        public async Task<bool> ChangInfo(string filePath, bool backup, string brand, string country)
        {
            var result = Shell("su -c \"whoami\"");
            if (result.Trim() != "root")
            {
                LogHelper.ERROR("Không phải root");
                return false;
            }
            LogHelper.SUCCESS($"Đang thay đổi thiết bị!");
            MaxChangeService maxChangeService = new MaxChangeService(this);
            return await maxChangeService.Change(filePath, backup, brand, country);
        }
        public async Task<bool> BackupDevice(string filePath)
        {
            return await maxChange.BackupDeviceInfoChange(filePath);
        }
        public string GetDeviceName()
        {
            MaxChangeService maxChangeService = new MaxChangeService(this);
            return maxChangeService.GetInfoDeviceName(10);
        }
        public bool ConnectProxy(string proxy)
        {
            LogHelper.SUCCESS($"Đang change proxy: {proxy}");
            VATProxyService proxyService = new VATProxyService(this);
            return proxyService.ConnectProxy(proxy);
        }
        public bool Connect()
        {
            try
            {
                if (!ConnectAdb())
                {
                    Device.TypeColor = 1;
                    LogHelper.Log("Không thể kết nối với thiết bị");
                    return false;
                }

                if (!_atx.Connect())
                {
                    Device.TypeColor = 1;
                    LogHelper.Log("Không thể kết nối với thiết bị với UI2");
                    return false;
                }
                LogHelper.Log($"adb start [{Device.Port}]");
                Device.TypeColor = 2;
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Connect Exception: {ex.Message}");
                return Connect(); // gọi lại, và phải return kết quả
            }
        }
        public bool ConnectAdb()
        {
            stopwatch.Start();
            LogHelper.Log($"Đang connect");
            while (Running)
            {
                string text = ProcessHelper.RunAdbWithTimeout($"-s {Device.Serial} shell service check settings", 1);
                if (!text.Contains("not found"))
                {
                    stopwatch.Stop();
                    LogHelper.Log($"Đã connect [{stopwatch.ElapsedMilliseconds} ms]");
                    return true;
                }
                LogHelper.Log($"Mất kết nối, chờ ExecuteAdb [{stopwatch.ElapsedMilliseconds / 1000.0:0.00}] cmd: Can't find service: settings");
                ProcessHelper.RunAdbCommand($"-s {Device.Serial} shell reconnect");
            }
            return false;
        }
        public void AppClear(string package)
        {
            try
            {
                LogHelper.SUCCESS($"Xóa dữ liệu app [{package}]");
                for (int i = 0; i < 5; i++)
                {
                    Shell("pm clear " + package, 3);
                    ADB.Shell("pm clear " + package, 5);
                }
            }
            catch
            {

            }

        }
        public string Shell(params object[] argv)
        {
            const int maxRetry = 3;
            int retry = 0;
            string result = "";

            while (retry < maxRetry)
            {
                try
                {
                    return ADBSocket.Shell(Device.Serial, argv);


                }
                catch (Exception ex)
                {
                    LogHelper.Log($"[Shell] Exception lần {retry + 1}: {ex.Message}");
                    Connect();
                }

                retry++;
            }

            LogHelper.Log("[Shell] Thất bại sau 3 lần thử");
            return result;
        }
        public AndroidAppInfo AppInfo(string package)
        {
            string result = ADBSocket.Shell(Device.Serial, "pm", "path", package);

            if (string.IsNullOrWhiteSpace(result) || !result.StartsWith("package:"))
                return null;

            var lines = result.Split('\n');
            var info = new AndroidAppInfo
            {
                PackageName = package,
                Path = lines[0].Split(':')[1].Strip()
            };

            if (lines.Length > 1)
            {
                for (int i = 1; i < lines.Length; i++)
                {
                    info.SubApkPaths.Add(lines[i].Split(':')[1].Strip());
                }
            }

            info.Dumpsys = ADBSocket.Shell(Device.Serial, "dumpsys", "package", package);
            if (string.IsNullOrWhiteSpace(info.Dumpsys))
                return null;

            Match match;

            match = Regex.Match(info.Dumpsys, "versionName=(?<name>[^\\s]+)");
            if (match.Success) info.VersionName = match.Groups["name"].Value;

            match = Regex.Match(info.Dumpsys, "versionCode=(?<code>\\d+)");
            if (match.Success) info.VersionCode = match.Groups["code"].Value;

            match = Regex.Match(info.Dumpsys, "PackageSignatures\\{.*?\\[(?<signatures>.*?)\\]");
            if (match.Success) info.Signature = match.Groups["signatures"].Value;

            match = Regex.Match(info.Dumpsys, "firstInstallTime=(?<time>[-\\d]+\\s[:\\d]+)");
            if (match.Success) info.FirstInstallTime = match.Groups["time"].Value;

            match = Regex.Match(info.Dumpsys, "lastUpdateTime=(?<time>[-\\d]+\\s[:\\d]+)");
            if (match.Success) info.LastUpdateTime = match.Groups["time"].Value;

            match = Regex.Match(info.Dumpsys, "pkgFlags=\\[(?<flags>\\s.*?\\s*)\\]");
            if (match.Success) info.Flags = match.Groups["flags"].Value.Strip();

            return info;
        }
        public List<AndroidProcessItem> GetProcessList()
        {
            string result = ADB.Shell("ps"); // <-- Hoặc thử "ps -ef"

            var list = new List<AndroidProcessItem>();
            if (string.IsNullOrWhiteSpace(result)) return list;

            var lines = result.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var pids = new HashSet<string>();

            foreach (var line in lines)
            {
                if (line.StartsWith("USER")) continue; // Bỏ dòng tiêu đề

                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 9) continue; // Tránh lỗi

                string user = parts[0];
                string pidStr = parts[1];
                string name = parts.Last(); // Tên process là cột cuối

                if (!pids.Add(pidStr)) continue;

                list.Add(new AndroidProcessItem
                {
                    User = user,
                    Pid = int.TryParse(pidStr, out var pid) ? pid : -1,
                    Name = name
                });
            }

            return list;
        }
        public void KillProcessByName(string name)
        {
            var list = GetProcessList();
            foreach (var proc in list)
            {
                if (proc.Name.Equals(name) && proc.User == "shell")
                {
                    Shell("kill", "-9", proc.Pid.ToString());
                }
            }
        }

        public string GetProp(string prop)
        {
            return Shell("getprop", prop);
        }

        public bool Push(string file, string path, int mode = 493)
        {
            return ADBSocket.Push(Device.Serial, file, path, mode);
        }

        public bool IsScreenOn()
        {
            string result = Shell("dumpsys", "power");
            return result.Contains("mHoldingDisplaySuspendBlocker=true");
        }

        public List<string> AppList(string filter = "")
        {
            var result = Shell("pm", "list", "packages", filter);
            var list = new List<string>();

            if (!string.IsNullOrWhiteSpace(result))
            {
                var lines = result.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.StartsWith("package:"))
                        list.Add(line.Split(':')[1]);
                }
            }

            return list;
        }
        public void SetSize(int width = 1440, int height = 2560, int density = 560)
        {
            Shell("settings put system accelerometer_rotation 0");
            Shell("settings put system user_rotation 0");
            Shell("content insert --uri content://settings/system --bind name:s:accelerometer_rotation --bind value:i:0");
        }

        public List<string> AppRunningList()
        {
            var running = new List<string>();
            var apps = AppList();
            var processes = GetProcessList();

            foreach (var app in apps)
            {
                if (processes.Any(p => p.Name.Contains(app)))
                {
                    running.Add(app);
                }
            }

            return running;
        }

        public bool Swipe(int dx, int dy, int ux, int uy, int durationMs = 55, int repeat = 1, int delayBetweenSwipes = 100)
        {
            for (int i = 0; i < repeat; i++)
            {
                try
                {
                    LogHelper.SUCCESS($"Đang vuốt [{i + 1}]");
                    _atx.Swipe(dx, dy, ux, uy, durationMs);
                }
                catch
                {
                    Connect();
                    return Swipe(dx, dy, ux, uy, durationMs, repeat, delayBetweenSwipes);
                }
                if (i < repeat - 1 && delayBetweenSwipes > 0)
                {
                    Thread.Sleep(delayBetweenSwipes);
                }
            }
            return true;
        }

        public void SwipeByPercent(double x1, double y1, double x2, double y2, int duration = 100, int repeat = 1, int delayBetweenSwipes = 100)
        {
            System.Drawing.Point screenResolution = GetScreenResolution();
            int num = (int)(x1 * ((double)screenResolution.X * 1.0 / 100.0));
            int num2 = (int)(y1 * ((double)screenResolution.Y * 1.0 / 100.0));
            int num3 = (int)(x2 * ((double)screenResolution.X * 1.0 / 100.0));
            int num4 = (int)(y2 * ((double)screenResolution.Y * 1.0 / 100.0));
            for (int i = 0; i < repeat; i++)
            {
                try
                {
                    LogHelper.SUCCESS($"Đang vuốt [{i + 1}]");
                    string text = Shell($"input swipe {num} {num2} {num3} {num4} {duration}");
                }
                catch
                {
                    Connect();
                    return;
                }
                if (i < repeat - 1 && delayBetweenSwipes > 0)
                {
                    Thread.Sleep(delayBetweenSwipes);
                }
            }

        }
        public System.Drawing.Point GetScreenResolution()
        {
            string text = ADB.Shell("dumpsys display | Find \"mCurrentDisplayRect\"");
            text = text.Substring(text.IndexOf("- "));
            text = text.Substring(text.IndexOf(' '), text.IndexOf(')') - text.IndexOf(' '));
            string[] array = text.Split(',');
            int x = Convert.ToInt32(array[0].Trim());
            int y = Convert.ToInt32(array[1].Trim());
            return new System.Drawing.Point(x, y);
        }
        public bool Click(float x, float y)
        {
            LogHelper.SUCCESS($"Đang click");
            try
            {
                return _atx.Click(x, y);
            }
            catch
            {
                Connect();
                return Click(x, y);
            }

        }

        public void LongClick(float x, float y, int durationMs = 1000)
        {
            LogHelper.SUCCESS($"Đang long click");
            try
            {
                _atx.LongClick(x, y, durationMs);
            }
            catch
            {
                Connect();
                LongClick(x, y, durationMs);
            }

        }

        public void CLearText()
        {
            LogHelper.SUCCESS($"Đang clear text");
            ADBKeyboardService.ClearInputWithADBKeyboard();
        }


        public bool ImeCurrent(out string ime)
        {
            var result = Shell("dumpsys", "input_method");
            Regex regex = new Regex("mCurMethodId=([-_./\\w]+)");
            Match match = regex.Match(result);
            if (match.Success)
            {
                ime = match.Groups[0].Value;
            }
            else
            {
                ime = "";
            }
            return result.Contains("mInputShown=true");
        }

        public void SendText(string xpath, string text, int timeout = 10, string xml = "", bool clear = true)
        {
            if (string.IsNullOrEmpty(text)) return;
            ElementWithAttributes(xpath, timeout, xml, true);
            if (clear)
            {
                CLearText(); // Xóa text trước khi nhập
            }
            LogHelper.SUCCESS($"Đang send text : {text}");
            string data = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
            string type = "ADB_INPUT_TEXT";
            Shell("am", "broadcast", "-a", type, "--es", "text", data);
        }
        public void SendTextADB(string xpath, string text, int timeout = 10, string xml = "", bool clear = true)
        {
            if (string.IsNullOrEmpty(text)) return;
            ElementWithAttributes(xpath, timeout, xml, true);
            if (clear)
            {
                CLearText();
            }
            LogHelper.SUCCESS($"Đang send text : {text}");
            Shell("input", "text", text);
        }

        public void SetEnableModuleMaxChange()
        {
            maxChange.SetEnableModule();
        }
        public Bitmap Screenshot()
        {
            return _atx.Screenshot();
        }
        public List<RegionResult> FindColorCoordinates(Color targetColor, int tolerance = 10, int regionWidth = 120, int regionHeight = 120, int timeout = 10)
        {
            List<RegionResult> results = new List<RegionResult>();
            try
            {
                int tickCount = Environment.TickCount;
                while (true)
                {
                    try
                    {
                        // 1. Dùng ATX để chụp ảnh màn hình
                        Bitmap screen = _atx.Screenshot();
                        if (screen != null)
                        {
                            results = ImageScanOpenCV.FindColorCoordinates(screen, targetColor, tolerance, regionWidth, regionHeight);

                        }
                        if (results.Any()) return results;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Log(ex.Message);
                        LogHelper.WriteFile(Device.Serial, ex.ToString());
                    }

                    if (timeout != 0)
                    {
                        if (Environment.TickCount - tickCount >= timeout * 1000)
                        {
                            break;
                        }
                        continue;
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"{nameof(ADBClient)}; {nameof(FindColorCoordinates)}; {ex.ToString()}");
            }


            return results;
        }


        /// <summary>
        /// Gửi từng ký tự trong chuỗi một cách chậm rãi, hỗ trợ tiếng Việt, emoji, v.v.
        /// </summary>
        /// <param name="text">Chuỗi cần nhập</param>
        /// <param name="min">Thời gian delay tối thiểu (ms)</param>
        /// <param name="max">Thời gian delay tối đa (ms)</param>
        public void SendTextSlow(string xpath, string text, int min = 10, int max = 300, int timeout = 10, string xml = "", bool clear = true)
        {
            if (string.IsNullOrEmpty(text)) return;
            ElementWithAttributes(xpath, timeout, xml, true);
            if (clear)
            {
                CLearText(); // Xóa text trước khi nhập
            }
            LogHelper.SUCCESS($"Đang send text : {text}");
            foreach (char c in text)
            {
                ADBKeyboardService.Input(c.ToString(), false);
                Thread.Sleep(random.Next(min, max));

            }
        }
        public string GetTextFromScreenShotByATX(int timeout = 60000)
        {
            try
            {
                _logHelper.Log("Lấy nội dung từ ảnh chụp màn hình");
                int tickCount = Environment.TickCount;
                while (true)
                {
                    try
                    {
                        // 1. Dùng ATX để chụp ảnh màn hình
                        Bitmap screen = _atx.Screenshot();
                        // 2. Dùng OpenCV OCR để trích xuất text từ ảnh
                        string text = ImageScanOpenCV.GetTextFromImage(screen);

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            stopwatch.Stop();
                            return text;
                        }

                    }
                    catch (Exception ex)
                    {
                        LogHelper.Log(ex.Message);
                        LogHelper.WriteFile(Device.Serial, ex.ToString());
                    }

                    if (timeout != 0)
                    {
                        if (Environment.TickCount - tickCount >= timeout * 1000)
                        {
                            break;
                        }
                        continue;
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(ex.Message);
                LogHelper.WriteFile(Device.Serial, ex.ToString());
            }

            return string.Empty;
        }
        public string Devices()
        {
            using var adb = new ADBSocket(Device.Serial);
            return adb.Command("host", "devices");
        }

        public T RunTime<T>(string message, Func<T> action, bool showResult = true)
        {
            LogHelper.Log($"{message}");
            int ms;
            T result = RunTimeHelper.Time(action, out ms);
            if (showResult)
                LogHelper.Log($"{message}: {result} ({ms}ms)");
            else
                LogHelper.Log($"{message}: ({ms}ms)");
            LogHelper.Log($"{message}: {result} ({ms}ms)");
            return result;
        }

        public void RunTime(string message, Action action)
        {
            LogHelper.Log($"{message}");
            int ms;
            RunTimeHelper.Time(action, out ms);
            LogHelper.Log($"{message}: ({ms}ms)");
            return;
        }
        public string GetXMLSource()
        {
            try
            {
                LogHelper.SUCCESS($"Đang lấy XML source");
                string value = _atx.DumpHierarchy();
                if (string.IsNullOrEmpty(value))
                {
                    Connect();
                    return GetXMLSource();
                }
                return value;
            }
            catch (Exception ex)
            {
                LogHelper.Log(ex.Message);
                Connect();
                return GetXMLSource();
            }
        }
        public List<XmlNode> FindElementsNotToLower(int timeout, string xmlContent, string xpath)
        {
            LogHelper.Log($"Find elements [{xpath}]");
            List<XmlNode> attributeValues = new List<XmlNode>();
            int tickCount = Environment.TickCount;
            while (true)
            {
                try
                {
                    if (string.IsNullOrEmpty(xmlContent))
                    {
                        xmlContent = GetXMLSource();
                    }
                    if (!string.IsNullOrEmpty(xmlContent))
                    {
                        xmlContent = xmlContent.Trim().Replace("\0", "");

                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(xmlContent);
                        XmlNodeList nodeList = xmlDoc.SelectNodes(xpath);

                        if (nodeList == null || nodeList.Count == 0)
                        {
                            if (timeout != 0)
                            {
                                xmlContent = "";
                                if (Environment.TickCount - tickCount >= timeout * 1000)
                                {
                                    break;
                                }
                                continue;
                            }
                            break;
                        }
                        for (int i = 0; i < nodeList.Count; i++)
                        {
                            try
                            {
                                attributeValues.Add(nodeList[i]);
                            }
                            catch
                            {
                            }
                        }
                        if (attributeValues.Any())
                        {
                            return attributeValues;
                        }

                    }

                }
                catch (Exception ex)
                {
                    LogHelper.Log(ex.Message);
                }
                if (timeout != 0)
                {
                    xmlContent = "";
                    if (Environment.TickCount - tickCount >= timeout * 1000)
                    {
                        break;
                    }
                    continue;
                }
                break;
            }


            return attributeValues;
        }
        public List<XmlNode> FindElements(int timeout, string xmlContent, string xpath)
        {
            LogHelper.Log($"Find elements [{xpath}]");
            List<XmlNode> attributeValues = new List<XmlNode>();
            int tickCount = Environment.TickCount;
            while (true)
            {
                try
                {
                    if (string.IsNullOrEmpty(xmlContent))
                    {
                        xmlContent = GetXMLSource();
                    }
                    if (!string.IsNullOrEmpty(xmlContent))
                    {
                        xmlContent = xmlContent.ToLower();
                        xpath = xpath.ToLower();

                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(xmlContent);
                        XmlNodeList nodeList = xmlDoc.SelectNodes(xpath);
                        if (nodeList == null || nodeList.Count == 0)
                        {
                            xmlContent = string.Empty;
                            if (timeout != 0)
                            {
                                xmlContent = "";
                                if (Environment.TickCount - tickCount >= timeout * 1000)
                                {
                                    break;
                                }
                                continue;
                            }
                            else
                            {
                                return attributeValues;
                            }
                        }
                        for (int i = 0; i < nodeList.Count; i++)
                        {
                            try
                            {
                                attributeValues.Add(nodeList[i]);
                            }
                            catch
                            {
                            }
                        }
                        if (attributeValues.Any())
                        {
                            return attributeValues;
                        }
                        if (timeout != 0)
                        {
                            xmlContent = "";
                            if (Environment.TickCount - tickCount >= timeout * 1000)
                            {
                                break;
                            }
                            continue;
                        }
                        break;
                    }

                }
                catch (Exception ex)
                {
                    LogHelper.Log(ex.Message);
                }
            }


            return attributeValues;
        }
        public List<XmlNode> FindElements(int timeout, string xmlContent, List<string> xpaths)
        {
            LogHelper.Log($"Find elements");
            List<XmlNode> attributeValues = new List<XmlNode>();
            int tickCount = Environment.TickCount;
            while (true)
            {
                try
                {
                    if (string.IsNullOrEmpty(xmlContent))
                    {
                        xmlContent = GetXMLSource();
                    }
                    if (!string.IsNullOrEmpty(xmlContent))
                    {
                        xmlContent = xmlContent.ToLower();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(xmlContent);
                        foreach (string xpath in xpaths)
                        {
                            if (string.IsNullOrEmpty(xpath)) continue;

                            string xpathValue = xpath.ToLower();
                            XmlNodeList nodeList = xmlDoc.SelectNodes(xpathValue);
                            if (nodeList == null || nodeList.Count == 0)
                            {
                                continue;
                            }
                            for (int i = 0; i < nodeList.Count; i++)
                            {
                                try
                                {
                                    attributeValues.Add(nodeList[i]);
                                }
                                catch
                                {
                                }
                            }

                        }
                        if (attributeValues.Any())
                        {
                            return attributeValues;
                        }
                    }
                    if (timeout != 0)
                    {
                        xmlContent = "";
                        if (Environment.TickCount - tickCount >= timeout * 1000)
                        {
                            break;
                        }
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log(ex.Message);
                }
            }


            return attributeValues;
        }
        public List<string> FilterElementsByMaxLeftCoordinate(List<string> elementBounds)
        {
            List<string> leftCoordinates = new List<string>();
            foreach (string bounds in elementBounds)
            {
                leftCoordinates.Add(new RectangleArea(bounds).Left.ToString());
            }

            // Find the left coordinate that appears most frequently
            string maxLeftCoord = "";
            int maxCount = 0;
            foreach (var group in leftCoordinates.GroupBy(coord => coord))
            {
                if (group.Count() > maxCount)
                {
                    maxLeftCoord = group.Key;
                    maxCount = group.Count();
                }
            }

            // Filter all elements that have the most common left coordinate
            List<string> filteredElements = new List<string>();
            foreach (string bounds in elementBounds)
            {
                if (new RectangleArea(bounds).Left == int.Parse(maxLeftCoord))
                {
                    filteredElements.Add(bounds);
                }
            }

            return filteredElements;
        }
        public List<string> FindBounds(string xmlContent, List<string> xpaths, int timout = 10)
        {
            List<string> attributeValues = new List<string>();
            try
            {
                int tickCount = Environment.TickCount;
                while (true)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(xmlContent))
                        {
                            xmlContent = GetXMLSource();
                        }
                        if (!string.IsNullOrEmpty(xmlContent))
                        {
                            using (var reader = XmlReader.Create(new StringReader(xmlContent.ToLower())))
                            {
                                var doc = new XPathDocument(reader);
                                var nav = doc.CreateNavigator();
                                foreach (string xpath in xpaths)
                                {
                                    if (string.IsNullOrEmpty(xpath)) continue;
                                    var nodes = nav.Select(xpath.ToLower());
                                    while (nodes.MoveNext())
                                    {
                                        var bounds = nodes.Current.GetAttribute("bounds", "");
                                        if (!string.IsNullOrEmpty(bounds))
                                            attributeValues.Add(bounds);
                                    }
                                }
                            }
                            if (attributeValues.Count > 0)
                                return attributeValues;
                        }
                        if (timout != 0)
                        {
                            xmlContent = "";
                            if (Environment.TickCount - tickCount >= timout * 1000)
                            {
                                break;
                            }
                            continue;
                        }
                    }
                    catch
                    {

                    }

                }


            }
            catch (Exception ex)
            {
                LogHelper.Log(ex.Message);
            }
            return attributeValues;
        }

        public List<string> GetChildNodeValuesFromXml(string xmlContent, string xpath, string attributeName = "bounds", int timeoutInSeconds = 1)
        {
            List<string> values = new List<string>();

            try
            {
                int tickCount = Environment.TickCount;
                while (true)
                {
                    if (string.IsNullOrEmpty(xmlContent))
                    {
                        xmlContent = GetXMLSource();
                    }
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xmlContent);

                    XmlNodeList nodeList = xmlDoc.SelectNodes(xpath);
                    if (nodeList != null)
                    {
                        foreach (XmlNode node in nodeList)
                        {
                            if (node.HasChildNodes)
                            {
                                foreach (XmlNode child in node.ChildNodes)
                                {
                                    if (child.Attributes != null)
                                    {
                                        foreach (XmlAttribute attr in child.Attributes)
                                        {
                                            if (string.Equals(attr.Name, attributeName, StringComparison.OrdinalIgnoreCase))
                                            {
                                                values.Add(attr.Value);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (values.Count <= 0 && timeoutInSeconds != 0)
                    {
                        xmlContent = "";
                        if (Environment.TickCount - tickCount >= timeoutInSeconds * 1000)
                        {
                            break;
                        }
                        continue;
                    }
                    break;
                }

            }
            catch (Exception ex)
            {
                LogHelper.ERROR($"{nameof(GetChildNodeValuesFromXml)} Error: {ex.Message}");
            }

            return values;
        }


        public List<string> FindBounds(string xmlContent, string xpath, int timout = 10)
        {
            List<string> attributeValues = new List<string>();
            try
            {
                int tickCount = Environment.TickCount;
                while (true)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(xmlContent))
                        {
                            xmlContent = GetXMLSource();
                        }
                        if (!string.IsNullOrEmpty(xmlContent))
                        {
                            xmlContent = xmlContent.ToLower();
                            xpath = xpath.ToLower();
                            using (var reader = XmlReader.Create(new StringReader(xmlContent)))
                            {
                                var doc = new XPathDocument(reader);
                                var nav = doc.CreateNavigator();
                                var nodes = nav.Select(xpath);
                                while (nodes.MoveNext())
                                {
                                    var bounds = nodes.Current.GetAttribute("bounds", "");
                                    if (!string.IsNullOrEmpty(bounds))
                                        attributeValues.Add(bounds);
                                }
                            }
                            if (attributeValues.Count > 0)
                                return attributeValues;
                        }
                        if (timout != 0)
                        {
                            xmlContent = "";
                            if (Environment.TickCount - tickCount >= timout * 1000)
                            {
                                break;
                            }
                            continue;
                        }
                        break;
                    }
                    catch
                    {

                    }

                }


            }
            catch (Exception ex)
            {
                LogHelper.Log(ex.Message);
            }
            return attributeValues;
        }
        public bool ElementWithAttributes(string xpath, int timeoutInSeconds = 5, string xmlSource = "", bool click = true)
        {
            try
            {
                int tickCount = Environment.TickCount;
                while (true)
                {
                    if (string.IsNullOrEmpty(xmlSource))
                        xmlSource = GetXMLSource();

                    if (!string.IsNullOrEmpty(xmlSource))
                    {
                        using (var stringReader = new StringReader(xmlSource))
                        {
                            var xpathDoc = new XPathDocument(stringReader);
                            var navigator = xpathDoc.CreateNavigator();

                            try
                            {
                                // Tách điều kiện ra để so sánh thủ công không phân biệt hoa thường
                                var conditions = ExtractAttributeConditions(xpath);
                                var pathWithoutConditions = RemoveAttributeConditions(xpath);

                                var nodeIterator = navigator.Select(pathWithoutConditions);
                                while (nodeIterator.MoveNext())
                                {
                                    bool matchAll = true;
                                    foreach (var kv in conditions)
                                    {
                                        var actualValue = nodeIterator.Current?.GetAttribute(kv.Key, "");
                                        if (!string.Equals(actualValue?.Trim(), kv.Value, StringComparison.OrdinalIgnoreCase))
                                        {
                                            matchAll = false;
                                            break;
                                        }
                                    }

                                    if (matchAll)
                                    {
                                        var bounds = nodeIterator.Current?.GetAttribute("bounds", "");
                                        if (!string.IsNullOrEmpty(bounds))
                                        {
                                            if (click)
                                            {
                                                var point = new RectangleArea(bounds).GetCenterPoint();
                                                return Click(point.X, point.Y);
                                            }
                                            return true;
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // Ignore invalid XPath
                            }
                        }
                    }

                    if (timeoutInSeconds != 0)
                    {
                        xmlSource = "";
                        if (Environment.TickCount - tickCount >= timeoutInSeconds * 1000)
                        {
                            break;
                        }
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.ERROR($"ElementWithAttributes: {ex.Message}");
            }

            return false;
        }

        public bool ElementWithAttributes(List<string> xpaths, int timeoutInSeconds = 5, string xmlSource = "", bool click = true)
        {
            try
            {
                int startTick = Environment.TickCount;

                while (true)
                {
                    if (string.IsNullOrEmpty(xmlSource))
                        xmlSource = GetXMLSource();

                    if (!string.IsNullOrEmpty(xmlSource) && xpaths?.Count > 0)
                    {
                        using (var stringReader = new StringReader(xmlSource))
                        {
                            var xpathDoc = new XPathDocument(stringReader);
                            var navigator = xpathDoc.CreateNavigator();

                            foreach (var xpath in xpaths)
                            {
                                try
                                {
                                    var conditions = ExtractAttributeConditions(xpath);
                                    var pathWithoutConditions = RemoveAttributeConditions(xpath);

                                    var nodeIterator = navigator.Select(pathWithoutConditions);
                                    while (nodeIterator.MoveNext())
                                    {
                                        bool matchAll = true;
                                        foreach (var kv in conditions)
                                        {
                                            string actualValue = nodeIterator.Current?.GetAttribute(kv.Key, "");
                                            if (!string.Equals(actualValue?.Trim(), kv.Value, StringComparison.OrdinalIgnoreCase))
                                            {
                                                matchAll = false;
                                                break;
                                            }
                                        }

                                        if (matchAll)
                                        {
                                            var bounds = nodeIterator.Current?.GetAttribute("bounds", "");
                                            if (!string.IsNullOrEmpty(bounds))
                                            {
                                                if (click)
                                                {
                                                    var point = new RectangleArea(bounds).GetCenterPoint();
                                                    return Click(point.X, point.Y);
                                                }
                                                return true;
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    // ignore XPath errors
                                }
                            }
                        }
                    }

                    if (Environment.TickCount - startTick >= timeoutInSeconds * 1000)
                        break;

                    xmlSource = "";
                }
            }
            catch (Exception ex)
            {
                LogHelper.ERROR(ex.Message);
            }


            return false;
        }
        public List<string> GetBoundsValues(int timeoutInSeconds, string XMLString, string xpath)
        {
            List<string> list = new List<string>();
            try
            {
                int tickCount = Environment.TickCount;
                while (true)
                {
                    if (string.IsNullOrEmpty(XMLString))
                    {
                        XMLString = GetXMLSource();
                    }
                    list = GetAttributeValuesFromXmlNodes(XMLString, xpath);
                    if (list.Count <= 0 && timeoutInSeconds != 0)
                    {
                        XMLString = "";
                        if (Environment.TickCount - tickCount >= timeoutInSeconds * 1000)
                        {
                            break;
                        }
                        continue;
                    }
                    break;
                }
            }
            catch (Exception exception_)
            {
                LogHelper.ERROR(exception_.Message);
            }
            return list.Distinct().ToList();
        }
        public List<string> GetAttributeValuesFromXmlNodes(string xmlContent, string xpath, string attributeName = "bounds")
        {
            List<string> attributeValues = new List<string>();

            try
            {
                if (string.IsNullOrEmpty(xmlContent))
                {
                    xmlContent = GetXMLSource();
                }

                if (!string.IsNullOrEmpty(xmlContent))
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xmlContent);

                    XmlNodeList nodeList = xmlDoc.SelectNodes(xpath);
                    if (nodeList != null)
                    {
                        foreach (XmlNode node in nodeList)
                        {
                            if (node.Attributes != null)
                            {
                                // Tìm attribute không phân biệt hoa thường
                                foreach (XmlAttribute attr in node.Attributes)
                                {
                                    if (string.Equals(attr.Name, attributeName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        attributeValues.Add(attr.Value);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.ERROR($"{nameof(GetAttributeValuesFromXmlNodes)} Error: {ex.Message}");
            }

            return attributeValues;
        }
        public bool UninstallApp(string packageName)
        {
            try
            {
                string result = Shell("pm", "uninstall", packageName);
                if (result.Contains("Success"))
                {
                    LogHelper.Log($"Đã gỡ cài đặt {packageName}");
                    return true;
                }
                else
                {
                    LogHelper.Log($"Gỡ cài đặt thất bại: [{packageName}] - Kết quả: {result}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Lỗi khi gỡ cài đặt {packageName}: {ex.Message}");
                Connect();

                return UninstallApp(packageName);
            }
            return false;
        }
        public bool IsRoot()
        {
            for (int i = 0; i <= 10; i++)
            {
                string value = ADB.Shell("su -c id");
                if (string.IsNullOrEmpty(value)) continue;
                if (value.Contains("magisk") || value.Contains("not found"))
                {
                    return true;
                }
                return value.ToLower().Contains("not found");
            }
            return false;
        }
        public void StopApp(string packageName)
        {
            try
            {
                string result = Shell("am", "force-stop", packageName);

                if (!string.IsNullOrWhiteSpace(result) &&
                    (result.Contains("Error") || result.Contains("Unknown package")))
                {
                    LogHelper.Log($"Không thể dừng app: {result}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Lỗi khi dừng app {packageName}: {ex.Message}");
            }
        }

        public bool InstallApp(string path_APK)
        {

            string fileName = Path.GetFileName(path_APK);
            string remotePath = InitHelper.ANDROID_LOCAL_TMP_PATH + fileName;

            LogHelper.SUCCESS($"Cài đặt [{fileName}]");

            // Tắt xác minh cài đặt nếu có thể (tùy thiết bị)
            Shell("settings", "put", "global", "verifier_verify_adb_installs", "0");

            for (int i = 0; i < 5; i++)
            {
                string result = "";
                if (Push(path_APK, remotePath))
                {
                    result = Shell("pm", "install", remotePath);

                    if (result.Contains("Success"))
                    {
                        return true;
                    }
                    else
                    {
                        result = ProcessHelper.RunAdbCommand($"-s {Device.Serial} install '{path_APK}'", 60);
                    }
                    if (result.Contains("success"))
                        return true;
                }
                else
                {
                    result = ProcessHelper.RunAdbCommand($"-s {Device.Serial} install '{path_APK}'", 60);
                }
                if (result.Contains("success"))
                    return true;
            }

            return false;
        }
        public bool FindElementIsExistOrClickByPackage(string name, string package, int timeout = 3, bool isClick = false, string XMLString = "")
        {
            try
            {
                int tickCount = Environment.TickCount;
                while (true)
                {
                    if (string.IsNullOrEmpty(XMLString))
                    {
                        XMLString = GetXMLSource();
                    }
                    if (!string.IsNullOrEmpty(XMLString))
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(XMLString);
                        //Appium
                        XmlNodeList elements = xmlDoc.SelectNodes($"//node[@package='{package}']");

                        if (elements != null && elements.Count > 0)
                        {
                            foreach (XmlNode element in elements)
                            {
                                // Kiểm tra thuộc tính "text" hoặc "content-desc"
                                string textValue = element.Attributes["text"]?.Value;
                                string contentDescValue = element.Attributes["content-desc"]?.Value;
                                string resourceId = element.Attributes["resource-id"]?.Value;
                                if (textValue != null && textValue.Contains(name) ||
                                    contentDescValue != null && contentDescValue.Contains(name) ||
                                    resourceId != null && resourceId.Contains(name))
                                {
                                    if (isClick)
                                    {
                                        if (element.Attributes["bounds"] != null)
                                        {
                                            string bounds = element.Attributes["bounds"].Value;
                                            var point = new RectangleArea(bounds).GetCenterPoint();
                                            Click(point.X, point.Y);
                                        }
                                        else
                                        {
                                            LogHelper.ERROR("Bounds attribute not found for element.");
                                        }
                                    }
                                    return true;
                                }
                            }
                        }
                    }
                    if (timeout != 0)
                    {
                        XMLString = "";
                        if (Environment.TickCount - tickCount >= timeout * 1000)
                        {
                            break;
                        }
                        continue;
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                LogHelper.ERROR($"{nameof(FindElementIsExistOrClickByPackage)}, Error; {ex.Message}, Exception; {ex}");

            }
            return false;

        }
        public bool Package(string package, int timeout = 3, string XMLString = "")
        {
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                while (stopwatch.ElapsedMilliseconds < timeout * 1000)
                {
                    if (string.IsNullOrEmpty(XMLString))
                    {
                        XMLString = GetXMLSource();
                    }
                    if (!string.IsNullOrEmpty(XMLString))
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(XMLString);
                        //Appium
                        XmlNodeList elements = xmlDoc.SelectNodes($"//node[@package='{package}']");

                        if (elements != null && elements.Count > 0)
                        {
                            return true;
                        }
                    }
                    if (timeout != 0)
                    {
                        XMLString = "";
                        continue;
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                LogHelper.ERROR($"{nameof(Package)}, Error; {ex.Message}, Exception; {ex}");

            }
            return false;

        }
        public void AppStart(string package, bool monkey = false, bool stop = false, bool wait = false, string activity = null)
        {
            if (stop)
            {
                StopApp(package);
            }
            if (monkey)
            {
                Shell("monkey", "-p", package, "-c", "android.intent.category.LAUNCHER", "1");
                if (wait)
                {
                    AppWait(package);
                }
                return;
            }
            if (string.IsNullOrWhiteSpace(activity))
            {
                var info = _atx.GetAppInfo(package);
                if (info.Success)
                {
                    activity = info.Data.MainActivity;
                    if (activity.IndexOf('.') == -1)
                    {
                        activity = "." + activity;
                    }
                }
            }
            LogHelper.Log($" AppStart: {package}/{activity}");
            Shell("am", "start", "-a", "android.intent.action.MAIN", "-c", "android.intent.category.LAUNCHER", "-n", $"{package}/{activity}");
            if (wait)
            {
                AppWait(package);
            }
        }
        public AppCurrentInfo AppCurrent()
        {
            AppCurrentInfo info = new AppCurrentInfo();
            var result = Shell("dumpsys", "window", "windows");
            Regex focus = new Regex("mCurrentFocus=Window\\{.*?\\s+(?<package>[^\\s]+)/(?<activity>[^\\s]+)\\}");
            Match match = focus.Match(result);
            if (match.Success)
            {
                info.Package = match.Groups["package"].Value;
                info.Activity = match.Groups["activity"].Value;
                return info;
            }
            result = Shell("dumpsys", "activity", "activities");
            Regex record = new Regex("mResumedActivity: ActivityRecord\\{.*?\\s+(?<package>[^\\s]+)/(?<activity>[^\\s]+)\\s.*?\\}");
            match = record.Match(result);
            if (match.Success)
            {
                info.Package = match.Groups["package"].Value;
                result = Shell("dumpsys", "activity", "top");
                Regex activity = new Regex("ACTIVITY (?<package>[^\\s]+)/(?<activity>[^/\\s]+) \\w+ pid=(?<pid>\\d+)");
                var matchs = activity.Matches(result);
                if (matchs.Count > 0)
                {
                    for (int i = 0; i < matchs.Count; i++)
                    {
                        if (matchs[i].Groups["package"].Value == info.Package)
                        {
                            info.Activity = matchs[i].Groups["activity"].Value;
                            info.Pid = int.TryParse(matchs[i].Groups["pid"].Value, out int pid) ? pid : 0;
                            return info;
                        }
                    }
                }
            }
            return null;
        }
        public bool AppWait(string package, int timeout = 20000, string activity = null, bool front = false)
        {
            LogHelper.SUCCESS($"Đang chờ ứng dụng [{package}]");
            long deadline = DateTimeOffset.Now.ToUnixTimeMilliseconds() + timeout;
            while (DateTimeOffset.Now.ToUnixTimeMilliseconds() < deadline)
            {
                try
                {
                    if (front)
                    {
                        var info = AppCurrent();
                        if (info == null)
                        {
                            continue;
                        }
                        if (info.Package == package)
                        {
                            if (!string.IsNullOrWhiteSpace(activity))
                            {
                                if (activity == info.Activity)
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        var list = Shell("pidof", package);
                        if (!string.IsNullOrEmpty(list))
                        {
                            return true;
                        }
                    }
                }
                catch (Exception)
                {

                }
                finally
                {
                }
            }
            return false;
        }
        public Dictionary<string, string> ExtractAttributeConditions(string xpath)
        {
            var conditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var matches = Regex.Matches(xpath, @"@([\w\-]+)\s*=\s*['""]([^'""]+)['""]");

            foreach (Match match in matches)
            {
                if (match.Groups.Count == 3)
                {
                    string attr = match.Groups[1].Value.Trim();
                    string value = match.Groups[2].Value.Trim();
                    conditions[attr] = value;
                }
            }

            return conditions;
        }
        public Dictionary<string, string> ExtractNodeInfo(string nodeXml)
        {
            var info = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(nodeXml))
                return info;

            try
            {
                // Tạo tài liệu XML tạm để load node string
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(nodeXml.Trim());

                XmlNode node = doc.DocumentElement;
                if (node?.Attributes != null)
                {
                    string text = node.Attributes["text"]?.Value ?? string.Empty;
                    string contentDesc = node.Attributes["content-desc"]?.Value ?? string.Empty;
                    string bounds = node.Attributes["bounds"]?.Value ?? string.Empty;

                    info["text"] = text;
                    info["content-desc"] = contentDesc;
                    info["bounds"] = bounds;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ExtractNodeInfo error: " + ex.Message);
            }

            return info;
        }
        private string RemoveAttributeConditions(string xpath)
        {
            // Xóa các đoạn điều kiện [@attr='value'] để lấy phần thô của XPath
            return Regex.Replace(xpath, @"\[\s*@[\w\-]+\s*=\s*['""][^'""]+['""]\s*\]", "");
        }
        public string FindElement(string xmlContent, List<string> xpaths, int timeoutInSeconds)
        {
            try
            {
                int startTick = Environment.TickCount;

                while (true)
                {
                    if (string.IsNullOrEmpty(xmlContent))
                        xmlContent = GetXMLSource();

                    if (!string.IsNullOrEmpty(xmlContent) && xpaths?.Count > 0)
                    {
                        using (var stringReader = new StringReader(xmlContent))
                        {
                            var xpathDoc = new XPathDocument(stringReader);
                            var navigator = xpathDoc.CreateNavigator();

                            foreach (var xpath in xpaths)
                            {
                                try
                                {
                                    var conditions = ExtractAttributeConditions(xpath);
                                    var pathWithoutConditions = RemoveAttributeConditions(xpath);

                                    var nodeIterator = navigator.Select(pathWithoutConditions);
                                    while (nodeIterator.MoveNext())
                                    {
                                        bool matchAll = true;
                                        foreach (var kv in conditions)
                                        {
                                            string actualValue = nodeIterator.Current?.GetAttribute(kv.Key, "");
                                            if (!string.Equals(actualValue?.Trim(), kv.Value, StringComparison.OrdinalIgnoreCase))
                                            {
                                                matchAll = false;
                                                break;
                                            }
                                        }

                                        if (matchAll)
                                        {
                                            var bounds = nodeIterator.Current?.GetAttribute("bounds", "");
                                            if (!string.IsNullOrEmpty(bounds))
                                                return xpath;
                                        }
                                    }
                                }
                                catch
                                {
                                    // ignore XPath errors
                                }
                            }
                        }
                    }

                    if (Environment.TickCount - startTick >= timeoutInSeconds * 1000)
                        break;

                    xmlContent = "";
                }
            }
            catch (Exception ex)
            {
                LogHelper.ERROR(ex.Message);
            }

            return "";
        }
        public void AppStopAll(params string[] excludes)
        {
            List<string> list = new List<string>() {
                "com.cell47.College_Proxy",
                "com.github.uiautomator",
                    "com.android.shell",
                     "com.android.systemui",
                "com.github.uiautomator.test",
            };
            string its = string.Empty;
            list.AddRange(excludes);
            List<string> apps = AppRunningList();
            foreach (string app in apps)
            {
                if (list.Contains(app))
                {
                    continue;
                }
                its = its + app + "\n";
                StopApp(app);
            }
            Connect();

        }
        public System.Drawing.Point FindPoint(string element, int timeoutInSeconds = 5, string xmlsoucre = "")
        {
            try
            {
                LogHelper.SUCCESS("FindElement " + element + "");
                var attributeValues = GetBoundsValues(timeoutInSeconds, xmlsoucre, element);
                string attributeValue = attributeValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(attributeValue))
                {
                    return new RectangleArea(attributeValue).GetCenterPoint();
                }
            }
            catch (Exception ex)
            {
                LogHelper.ERROR(ex.Message);
            }

            return new System.Drawing.Point();
        }
        /// <summary>
        /// Kiểm tra xem thiết bị Android có kết nối Internet không (bằng cách ping 8.8.8.8).
        /// </summary>
        /// <param name="deviceId">ID của thiết bị ADB (ví dụ: "ce031603b4f5a13703")</param>
        /// <returns>true nếu có mạng, false nếu không</returns>
        public async Task<bool> IsDeviceConnectedToInternet()
        {
            try
            {
                string result = await maxChange.GetIP();
                return string.IsNullOrEmpty(result);
            }
            catch
            {
                return false;
            }
        }
        public void GrantAppPermissions(string package)
        {
            this.Shell(" pm grant " + package + " android.permission.READ_CONTACTS");
            this.Shell(" pm grant " + package + " android.permission.READ_EXTERNAL_STORAGE");
            this.Shell(" pm grant " + package + " android.permission.WRITE_EXTERNAL_STORAGE");
            this.Shell(" pm grant " + package + " android.permission.CAMERA");
            this.Shell(" pm grant " + package + " android.permission.RECORD_AUDIO");
            this.Shell(" pm grant " + package + " android.permission.CALL_PHONE");
            this.Shell("pm grant " + package + " android.permission.MANAGE_EXTERNAL_STORAGE");
            this.Shell("pm grant " + package + " android.permission.MANAGE_EXTERNAL_STORAGE");

        }
        public async Task<string> GetIp()
        {
            try
            {
                return await maxChange.GetIP();
            }
            catch
            {
                return "";
            }
        }
        public void Delay(int delay)
        {
            for (int i = 0; i < delay; i++)
            {
                LogHelper.Log($"Đang chờ {i + 1} giây");
                Thread.Sleep(1000);
            }

        }
        public void Delay(int min, int max)
        {
            int value = random.Next(min, max);
            for (int i = 0; i < value; i++)
            {
                LogHelper.Log($"Đang chờ {i + 1} giây");
                Thread.Sleep(1000);
            }

        }
    }
}
