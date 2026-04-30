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

        /// <summary>
        /// Ném OperationCanceledException nếu cờ Running bị tắt.
        /// Gọi ở đầu/trong mọi vòng lặp dài để tool thoát ngay khi người dùng nhấn Dừng.
        /// </summary>
        public void ThrowIfStopped()
        {
            if (!Running)
            {
                throw new OperationCanceledException("Tool đã dừng (ADBClient.Running = false).");
            }
        }

        /// <summary>
        /// Sleep có thể hủy: chia nhỏ 100ms và kiểm tra Running liên tục.
        /// </summary>
        private void InterruptibleSleep(int milliseconds)
        {
            const int step = 100;
            int elapsed = 0;
            while (elapsed < milliseconds)
            {
                ThrowIfStopped();
                int wait = Math.Min(step, milliseconds - elapsed);
                Thread.Sleep(wait);
                elapsed += wait;
            }
        }
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
        public OcrHelperService OcrHelper
        {
            get => _ocrHelper ??= new OcrHelperService(this);
            set => _ocrHelper = value;
        }
        private OcrHelperService _ocrHelper;
        private AutomationType _currentAutomationType = AutomationType.Ui2Automation;
        public MaxChangeService maxChange;
        public readonly ADBKeyboardService ADBKeyboardService;
        private readonly ClipboardService _clipboardService;
        public string CurrentAutomationType => AutomationTypeResolver.Normalize(_currentAutomationType);
        private bool IsAppiumMode => _currentAutomationType == AutomationType.Appium;
        public DeviceModel Create(string serial)
        {
            DeviceModel model = null;
            using var _client = new ADBSocket(serial);
            try
            {
                model = new DeviceModelContext().GetBySerial(serial);
                if (model == null)
                {
                    model = new DeviceModel();
                    model.Serial = serial;

                }
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
            // Rotation đã được force tại DeviceServices.LoadDeviceInfo khi detect device.
            return model;
        }
        public DeviceModel Device { get; set; }
        public ADBClient(DeviceModel model)
        {
            Device = model;
            _currentAutomationType = AutomationEnvironmentService.GetDeviceAutomationType(Device.Serial, AutomationType.Ui2Automation);
            _atx = new ATXService(this);
            _logHelper = new LogHelper(Device);
            maxChange = new MaxChangeService(this);
            ADBKeyboardService = new ADBKeyboardService(this);
            _clipboardService = new ClipboardService(this);
            // Rotation đã được force tại DeviceServices.LoadDeviceInfo khi detect device.
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
            _currentAutomationType = AutomationEnvironmentService.GetDeviceAutomationType(Device.Serial, AutomationType.Ui2Automation);
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
                    ThrowIfStopped();
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
                ThrowIfStopped();
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
                    ThrowIfStopped();
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
                    ThrowIfStopped();
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

        /// <summary>
        /// Tìm vị trí ảnh template trong màn hình hiện tại bằng Python OCR-Helper.
        /// Tự chụp screenshot nếu không truyền vào.
        /// Trả về tọa độ android (x, y) theo tỉ lệ màn hình, hoặc null nếu không tìm thấy.
        /// </summary>
        public async Task<OcrHelperService.TemplateMatchResult?> FindTemplateAsync(
            Bitmap templateBitmap,
            double threshold = 0.85,
            Bitmap? screenshot = null)
            => await OcrHelper.FindTemplateAsync(templateBitmap, threshold, screenshot);

        /// <summary>
        /// Tìm vị trí text trong màn hình hiện tại bằng Tesseract OCR song song.
        /// Tự chụp screenshot nếu không truyền vào.
        /// Trả về tọa độ android (x, y), hoặc null nếu không tìm thấy.
        /// </summary>
        public async Task<OcrHelperService.OcrFindResult?> FindTextAsync(
            string searchText,
            string lang = "eng",
            double confidenceThreshold = 60,
            Bitmap? screenshot = null)
            => await OcrHelper.FindTextAsync(searchText, lang, confidenceThreshold, screenshot);

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
                ThrowIfStopped();
                LogHelper.SUCCESS("Khởi động máy thành công!");
            }
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < 15000)
            {
                ThrowIfStopped();
                if (Connect(CurrentAutomationType))
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
            if (IsAppiumMode)
            {
                return AppiumSessionService.Swipe(this, x1, y1, x2, y2, duration);
            }

            try
            {
                return _atx.Swipe(x1, y1, x2, y2, duration);
            }
            catch
            {
                Connect(CurrentAutomationType);
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
        public bool Connect(string? type = null)
        {
            AutomationType automationType = string.IsNullOrWhiteSpace(type)
                ? _currentAutomationType
                : AutomationTypeResolver.Parse(type);
            string resolvedType = AutomationTypeResolver.Normalize(automationType);
            _currentAutomationType = automationType;
            AutomationEnvironmentService.SetDeviceAutomationType(Device.Serial, automationType);

            for (int attempt = 0; attempt < 3; attempt++)
            {
                ThrowIfStopped();
                try
                {
                    if (!ConnectAdb())
                    {
                        Device.TypeColor = 1;
                        LogHelper.Log("Không thể kết nối với thiết bị");
                        return false;
                    }

                    if (automationType == AutomationType.Appium)
                    {
                        bool appiumReady = AutomationEnvironmentService.EnsureDeviceReadyAsync(this, automationType).GetAwaiter().GetResult();
                        if (!appiumReady)
                        {
                            Device.TypeColor = 1;
                            LogHelper.Log("Không thể setup Appium trên thiết bị");
                            return false;
                        }

                        if (!AppiumSessionService.EnsureSession(this))
                        {
                            Device.TypeColor = 1;
                            LogHelper.Log("Không thể tạo Appium session cho thiết bị");
                            return false;
                        }
                    }
                    else
                    {
                        if (!_atx.Connect())
                        {
                            Device.TypeColor = 1;
                            LogHelper.Log("Không thể kết nối với thiết bị với UI2");
                            return false;
                        }
                    }

                    LogHelper.Log($"adb start [{Device.Port}] ({resolvedType})");
                    Device.IsLive = true;
                    Device.TypeColor = 2;
                    return true;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Connect Exception lần {attempt + 1}: {ex.Message}");
                    InterruptibleSleep(500);
                }
            }

            Device.TypeColor = 1;
            return false;
        }
        /// <summary>
        /// Quick one-shot check: is ADB service reachable on this device?
        /// Sets Device.IsAdbOnline and returns result. Does NOT loop.
        /// </summary>
        public bool CheckAdbOnline()
        {
            try
            {
                string text = ProcessHelper.RunAdbWithTimeout($"-s {Device.Serial} shell service check settings", 3);
                bool online = !string.IsNullOrEmpty(text) && !text.Contains("not found");
                Device.IsAdbOnline = online;
                return online;
            }
            catch
            {
                Device.IsAdbOnline = false;
                return false;
            }
        }

        public bool ConnectAdb()
        {
            LogHelper.Log($"Đang connect");
            int index = 0;
            while (Running)
            {
                ThrowIfStopped();
                index++;
                string text = ProcessHelper.RunAdbWithTimeout($"-s {Device.Serial} shell service check settings", 5);
                // Phải có output hợp lệ (không phải empty/timeout) và không chứa "not found"
                bool isOnline = !string.IsNullOrWhiteSpace(text) && !text.Contains("not found");
                if (isOnline)
                {
                    Device.IsAdbOnline = true;
                    LogHelper.Log($"Đã connect");
                    return true;
                }
                Device.IsLive = false;
                Device.IsAdbOnline = false;
                string reason = string.IsNullOrWhiteSpace(text) ? "timeout/no response" : "Can't find service: settings";
                LogHelper.Log($"Mất kết nối, chờ ExecuteAdb [{index}] cmd: {reason}");
                ThrowIfStopped();
                ProcessHelper.RunAdbCommand($"-s {Device.Serial} shell reconnect");
                if (index > 10_000)
                {
                    index = 0;
                }
            }
            ThrowIfStopped();
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

                if (package == "com.facebook.katana" || package == "com.facebook.lite" || package == "com.facebook.orca")
                {
                    DeleteAccounts();
                    Shell("pm disable-user --user 0 " + package, 3);
                    Shell("pm enable --user 0 " + package, 3);
                }
            }
            catch
            {

            }

        }

        private void DeleteAccounts()
        {
            try
            {
                string output = Shell("su -c \"sqlite3 /data/system_ce/0/accounts_ce.db 'SELECT _id FROM accounts;'\"", 5);
                string[] rows = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string row in rows)
                {
                    try
                    {
                        int id = Convert.ToInt32(row.Trim());
                        Shell($"su -c \"sqlite3 /data/system_de/0/accounts_de.db 'DELETE FROM accounts WHERE _id = {id};'\"", 5);
                        Shell($"su -c \"sqlite3 /data/system_de/0/accounts_de.db 'DELETE FROM debug_table;'\"", 5);
                        Shell($"su -c \"sqlite3 /data/system_de/0/accounts_de.db 'DELETE FROM meta;'\"", 5);
                        Shell($"su -c \"sqlite3 /data/system_ce/0/accounts_ce.db 'DELETE FROM accounts WHERE _id = {id};'\"", 5);
                        Shell($"su -c \"sqlite3 /data/system_ce/0/accounts_ce.db 'DELETE FROM sqlite_sequence WHERE seq = {id};'\"", 5);
                    }
                    catch { }
                }
            }
            catch { }
        }
        public string Shell(params object[] argv)
        {
            ThrowIfStopped();
            const int maxRetry = 3;
            int retry = 0;
            string result = "";

            while (retry < maxRetry)
            {
                ThrowIfStopped();
                try
                {
                    return ADBSocket.Shell(Device.Serial, argv);


                }
                catch (Exception ex)
                {
                    LogHelper.Log($"[Shell] Exception lần {retry + 1}: {ex.Message}");
                    ThrowIfStopped();
                    Connect(CurrentAutomationType);
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
    Shell("wm size reset");
    Shell("wm density reset");
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
                    if (IsAppiumMode)
                    {
                        bool success = AppiumSessionService.Swipe(this, dx, dy, ux, uy, durationMs);
                        if (!success)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        _atx.Swipe(dx, dy, ux, uy, durationMs);
                    }
                }
                catch
                {
                    Connect(CurrentAutomationType);
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
                    Connect(CurrentAutomationType);
                    return;
                }
                if (i < repeat - 1 && delayBetweenSwipes > 0)
                {
                    Thread.Sleep(delayBetweenSwipes);
                }
            }

        }
        public void SwipeUp(int repeat = 1, int duration = 800, int delayBetweenSwipes = 500)
        {
            var rnd = new Random();
            System.Drawing.Point screen = GetScreenResolutionSafe();
            for (int i = 0; i < repeat; i++)
            {
                int x = (int)(screen.X * (0.30 + rnd.NextDouble() * 0.40)); // 30–70% ngang
                int startY = (int)(screen.Y * (0.60 + rnd.NextDouble() * 0.25)); // 60–85% dọc
                int endY = (int)(screen.Y * (0.15 + rnd.NextDouble() * 0.20)); // 15–35% dọc
                Swipe(x, startY, x, endY, duration);
                if (i < repeat - 1 && delayBetweenSwipes > 0)
                    Thread.Sleep(delayBetweenSwipes);
            }
        }

        public void SwipeDown(int repeat = 1, int duration = 800, int delayBetweenSwipes = 500)
        {
            var rnd = new Random();
            System.Drawing.Point screen = GetScreenResolutionSafe();
            for (int i = 0; i < repeat; i++)
            {
                int x = (int)(screen.X * (0.30 + rnd.NextDouble() * 0.40)); // 30–70% ngang
                int startY = (int)(screen.Y * (0.15 + rnd.NextDouble() * 0.20)); // 15–35% dọc
                int endY = (int)(screen.Y * (0.60 + rnd.NextDouble() * 0.25)); // 60–85% dọc
                Swipe(x, startY, x, endY, duration);
                if (i < repeat - 1 && delayBetweenSwipes > 0)
                    Thread.Sleep(delayBetweenSwipes);
            }
        }

        public System.Drawing.Point GetScreenResolution()
        {
            string text = ADB.Shell("dumpsys display | Find \"mCurrentDisplayRect\"");
            int dashIdx = text.IndexOf("- ");
            if (dashIdx < 0) return GetScreenResolutionSafe();
            text = text.Substring(dashIdx);
            int spaceIdx = text.IndexOf(' ');
            int parenIdx = text.IndexOf(')');
            if (spaceIdx < 0 || parenIdx <= spaceIdx) return GetScreenResolutionSafe();
            text = text.Substring(spaceIdx, parenIdx - spaceIdx);
            string[] array = text.Split(',');
            if (array.Length < 2) return GetScreenResolutionSafe();
            if (!int.TryParse(array[0].Trim(), out int x)) return GetScreenResolutionSafe();
            if (!int.TryParse(array[1].Trim(), out int y)) return GetScreenResolutionSafe();
            return new System.Drawing.Point(x, y);
        }
        private System.Drawing.Point GetScreenResolutionSafe()
        {
            try
            {
                string wmSize = Shell("wm", "size");
                Match match = Regex.Match(wmSize, "(?<w>\\d+)x(?<h>\\d+)");
                if (match.Success)
                {
                    return new System.Drawing.Point(
                        int.Parse(match.Groups["w"].Value),
                        int.Parse(match.Groups["h"].Value));
                }
            }
            catch
            {
            }

            try
            {
                return GetScreenResolution();
            }
            catch
            {
                return new System.Drawing.Point(1080, 1920);
            }
        }
        internal System.Drawing.Point GetScreenResolutionForInput()
        {
            return GetScreenResolutionSafe();
        }

        private System.Drawing.Point ResolveAbsolutePoint(float x, float y)
        {
            System.Drawing.Point size = GetScreenResolutionSafe();
            int absX = x > 1 ? (int)x : (int)(x * size.X);
            int absY = y > 1 ? (int)y : (int)(y * size.Y);
            return new System.Drawing.Point(absX, absY);
        }
        public bool Click(float x, float y)
        {
            LogHelper.SUCCESS($"Đang click");
            try
            {
                if (IsAppiumMode)
                {
                    System.Drawing.Point point = ResolveAbsolutePoint(x, y);
                    return AppiumSessionService.Tap(this, point.X, point.Y);
                }

                return _atx.Click(x, y);
            }
            catch
            {
                Connect(CurrentAutomationType);
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
                Connect(CurrentAutomationType);
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
            if (IsAppiumMode)
            {
                return AppiumSessionService.Screenshot(this);
            }

            return _atx.Screenshot();
        }
        private Bitmap ScreenshotByAdb()
        {
            string safeSerial = Device.Serial.Replace(":", "_");
            string remotePath = "/sdcard/__autoandroid_screen.png";
            string localPath = Path.Combine(Path.GetTempPath(), $"aa_{safeSerial}_{Guid.NewGuid():N}.png");

            try
            {
                Shell("screencap", "-p", remotePath);
                ProcessHelper.RunAdbWithTimeout($"-s {Device.Serial} pull \"{remotePath}\" \"{localPath}\"", 20);
                if (!File.Exists(localPath))
                {
                    return null;
                }

                using FileStream fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using Bitmap bmp = new Bitmap(fs);
                return new Bitmap(bmp);
            }
            catch (Exception ex)
            {
                LogHelper.Log($"ScreenshotByAdb lỗi: {ex.Message}");
                return null;
            }
            finally
            {
                try
                {
                    Shell("rm", "-f", remotePath);
                }
                catch
                {
                }

                try
                {
                    if (File.Exists(localPath))
                    {
                        File.Delete(localPath);
                    }
                }
                catch
                {
                }
            }
        }
        public List<RegionResult> FindColorCoordinates(Color targetColor, int tolerance = 10, int regionWidth = 120, int regionHeight = 120, int timeout = 10)
        {
            List<RegionResult> results = new List<RegionResult>();
            try
            {
                int tickCount = Environment.TickCount;
                while (true)
                {
                    ThrowIfStopped();
                    try
                    {
                        // 1. Chụp ảnh màn hình theo engine hiện tại
                        Bitmap screen = Screenshot();
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
            ADBKeyboardService.Input(text.ToString(), false);

        }
        public string GetTextFromScreenShotByATX(int timeout = 60000)
        {
            try
            {
                _logHelper.Log("Lấy nội dung từ ảnh chụp màn hình");
                int tickCount = Environment.TickCount;
                while (true)
                {
                    ThrowIfStopped();
                    try
                    {
                        // 1. Chụp ảnh màn hình theo engine hiện tại
                        Bitmap screen = Screenshot();
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
        public string GetXMLSource(string? type = null)
        {
            if (!string.IsNullOrWhiteSpace(type))
            {
                _currentAutomationType = AutomationTypeResolver.Parse(type);
                AutomationEnvironmentService.SetDeviceAutomationType(Device.Serial, _currentAutomationType);
            }

            LogHelper.SUCCESS("Đang lấy XML source");
            const int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                ThrowIfStopped();
                try
                {
                    string xml = _currentAutomationType == AutomationType.Appium
                        ? GetXmlSourceByAppium()
                        : GetXmlSourceByATX();

                    if (IsValidHierarchyXml(xml))
                    {
                        return xml;
                    }
                    ThrowIfStopped();
                    Connect(CurrentAutomationType);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"GetXMLSource lỗi lần {attempt + 1}: {ex.Message}");
                    ThrowIfStopped();
                    Connect(CurrentAutomationType);
                }
            }

            return string.Empty;
        }

        private string GetXmlSourceByATX()
        {
            return _atx.DumpHierarchy();
        }

        private string GetXmlSourceByAppium()
        {
            return AppiumSessionService.GetPageSource(this);
        }

        private static bool IsValidHierarchyXml(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return false;
            }

            return xml.Contains("<hierarchy", StringComparison.OrdinalIgnoreCase)
                   && xml.Contains("</hierarchy>", StringComparison.OrdinalIgnoreCase);
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

        /// <summary>
        /// Mở trang đổi tên Facebook qua deep link rồi dump XML để lấy fullname.
        /// Cấu trúc XML: EditText (index 0) và label View text="First name" (index 1) là sibling cùng cha.
        /// </summary>
        public string GetFacebookFullName(string uid)
        {
            try
            {
                var urls = new List<string> { $"\"https://accountscenter.facebook.com/profiles/{uid}/name\"", $"\"fb://faceweb/f?href=https://accountscenter.facebook.com/profiles/{uid}/name\"" };
                string xml = string.Empty;
                foreach (var url in urls)
                {
                    Shell("am", "start", "-n",
                   "com.facebook.katana/.IntentUriHandler",
                   url);

                    // Retry đến khi thấy EditText trong XML

                    for (int i = 0; i < 5; i++)
                    {
                        System.Threading.Thread.Sleep(5000);
                        xml = GetXMLSource();
                        if (!string.IsNullOrEmpty(xml) &&
                            xml.Contains("android.widget.EditText", StringComparison.OrdinalIgnoreCase))
                            break;
                    }
                    if (!string.IsNullOrEmpty(xml))
                        break;
                }
                if (string.IsNullOrEmpty(xml))
                    return string.Empty;


                // Dùng Linq to XML thay vì XmlDocument/XPath để tránh namespace issue
                // Parse thủ công: tìm tất cả EditText, với mỗi EditText kiểm tra NextSibling có text = label
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);

                // Lấy tất cả node EditText bằng cách duyệt toàn bộ cây
                var allNodes = xmlDoc.SelectNodes("//*[@class='android.widget.EditText']");

                string firstName = string.Empty;
                string middleName = string.Empty;
                string lastName = string.Empty;

                if (allNodes != null)
                {
                    foreach (XmlNode editText in allNodes)
                    {
                        string value = editText.Attributes?["text"]?.Value ?? string.Empty;

                        // Label nằm ở NextSibling của EditText trong cùng cha
                        XmlNode? next = editText.NextSibling;
                        string label = next?.Attributes?["text"]?.Value ?? string.Empty;

                        LogHelper.Log($"[GetFacebookFullName] label='{label}' value='{value}'");

                        if (label.Equals("First name", StringComparison.OrdinalIgnoreCase))
                            firstName = value.Trim();
                        else if (label.Equals("Middle name", StringComparison.OrdinalIgnoreCase))
                            middleName = value.Trim();
                        else if (label.Equals("Last name", StringComparison.OrdinalIgnoreCase))
                            lastName = value.Trim();
                    }
                }

                string fullName = string.Join(" ",
                    new[] { firstName, middleName, lastName }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

                LogHelper.Log($"[GetFacebookFullName] uid={uid} → '{fullName}'");
                return fullName;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[GetFacebookFullName] Lỗi: {ex.Message}");
                return string.Empty;
            }
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
                Connect(CurrentAutomationType);

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
            if (!File.Exists(path_APK))
            {
                LogHelper.Error($"File APK không tồn tại: {path_APK}");
                return false;
            }

            string fileName = Path.GetFileName(path_APK);
            string remotePath = InitHelper.ANDROID_LOCAL_TMP_PATH + fileName;

            LogHelper.SUCCESS($"Cài đặt [{fileName}]");

            // Tắt xác minh cài đặt nếu có thể (tùy thiết bị)
            try { Shell("settings", "put", "global", "verifier_verify_adb_installs", "0"); } catch { }

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    string result = "";
                    if (Push(path_APK, remotePath))
                    {
                        result = Shell("pm", "install", "-r", remotePath);

                        if (result.Contains("Success", StringComparison.OrdinalIgnoreCase))
                        {
                            CleanupRemoteFile(remotePath);
                            return true;
                        }

                        // Fallback sang adb install trực tiếp
                        result = ProcessHelper.RunAdbCommand($"-s {Device.Serial} install -r \"{path_APK}\"", 120);
                        if (result.Contains("Success", StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                    else
                    {
                        result = ProcessHelper.RunAdbCommand($"-s {Device.Serial} install -r \"{path_APK}\"", 120);
                        if (result.Contains("Success", StringComparison.OrdinalIgnoreCase))
                            return true;
                    }

                    LogHelper.Error($"Cài đặt [{fileName}] lần {i + 1} thất bại: {result}");
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"Cài đặt [{fileName}] lần {i + 1} lỗi: {ex.Message}");
                }
            }

            CleanupRemoteFile(remotePath);
            return false;
        }

        private void CleanupRemoteFile(string remotePath)
        {
            try { Shell("rm", "-f", remotePath); } catch { }
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
                if (IsAppiumMode)
                {
                    activity = ResolveMainActivityByAdb(package);
                }
                else
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
            }

            if (string.IsNullOrWhiteSpace(activity))
            {
                Shell("monkey", "-p", package, "-c", "android.intent.category.LAUNCHER", "1");
                if (wait)
                {
                    AppWait(package);
                }
                return;
            }

            LogHelper.Log($"AppStart: {package}/{activity}");
            Shell("am", "start", "-a", "android.intent.action.MAIN", "-c", "android.intent.category.LAUNCHER", "-n", $"{package}/{activity}");
            if (wait)
            {
                AppWait(package);
            }
        }
        private string ResolveMainActivityByAdb(string package)
        {
            try
            {
                string output = Shell("cmd", "package", "resolve-activity", "--brief", package);
                var lines = output
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

                string activityLine = lines.LastOrDefault(x => x.Contains("/"));
                if (string.IsNullOrWhiteSpace(activityLine))
                {
                    return string.Empty;
                }

                string[] parts = activityLine.Split('/');
                if (parts.Length < 2)
                {
                    return string.Empty;
                }

                string activity = parts[1].Trim();
                if (string.IsNullOrWhiteSpace(activity))
                {
                    return string.Empty;
                }

                if (!activity.StartsWith(".") && !activity.StartsWith(package, StringComparison.OrdinalIgnoreCase))
                {
                    activity = "." + activity;
                }

                return activity;
            }
            catch
            {
                return string.Empty;
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
                ThrowIfStopped();
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
                Debug.WriteLine("ExtractNodeInfo error: " + ex.Message);
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
            Connect(CurrentAutomationType);

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
        public async Task<string> GetIp(string state = "")
        {
            try
            {
                return await maxChange.GetIP(state);
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
                ThrowIfStopped();
                LogHelper.Log($"Đang chờ {i + 1} giây");
                InterruptibleSleep(1000);
            }

        }
        public void Delay(int min, int max)
        {
            int value = random.Next(min, max);
            for (int i = 0; i < value; i++)
            {
                ThrowIfStopped();
                LogHelper.Log($"Đang chờ {i + 1} giây");
                InterruptibleSleep(1000);
            }

        }
    }
}
