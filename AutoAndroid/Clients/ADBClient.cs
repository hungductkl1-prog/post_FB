using OpenCvSharp;
using OpenCvSharp.Extensions;
using Sunny.Subdy.Common.Services;
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
using AutoAndroid.Monitoring;
namespace AutoAndroid
{
    public class ADBClient
    {
        private static readonly Regex AndroidClassXPathStepRegex = new Regex(
            @"(?<axis>//?)(?<class>(?:android|androidx)\.[A-Za-z0-9_.$]+(?:\.[A-Za-z0-9_.$]+)*)(?<predicates>(?:\[[^\]]*\])*)",
            RegexOptions.Compiled);

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
            // Thiếu 2 dòng dưới thì SendTextSlow()/CLearText()/GetClipboardText() ném
            // NullReferenceException với client tạo bằng serial (ctor DeviceModel có đủ).
            ADBKeyboardService = new ADBKeyboardService(this);
            _clipboardService = new ClipboardService(this);
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
            bool ownsSource = sourceImage == null;   // chỉ dispose ảnh do hàm tự chụp, không đụng ảnh caller truyền vào
            List<Bitmap> referenceImages = new List<Bitmap>();
            try
            {
                // Load all bitmaps from the directory
                string resolvedFolder = ResolveAssetDirectory(imageFolder);
                if (string.IsNullOrWhiteSpace(resolvedFolder) || !Directory.Exists(resolvedFolder))
                {
                    LogHelper.Log($"Không tìm thấy thư mục ảnh mẫu: {imageFolder}");
                    return "";
                }
                DirectoryInfo dir = new DirectoryInfo(resolvedFolder);
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
                        if (ownsSource) sourceImage?.Dispose();   // giải phóng ảnh chụp vòng trước
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
            finally
            {
                if (ownsSource) sourceImage?.Dispose();
                foreach (var reference in referenceImages)
                    reference.Dispose();
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
            bool ownsScreen = screenBitmap == null;   // chỉ dispose ảnh tự chụp, giữ ảnh caller truyền vào
            List<Bitmap> templates = new List<Bitmap>();
            try
            {
                // Load toàn bộ ảnh mẫu trong thư mục
                string resolvedDirectory = ResolveAssetDirectory(imageDirectory);
                if (string.IsNullOrWhiteSpace(resolvedDirectory) || !Directory.Exists(resolvedDirectory))
                {
                    LogHelper.Log($"Không tìm thấy thư mục ảnh mẫu: {imageDirectory}");
                    return "";
                }
                DirectoryInfo dir = new DirectoryInfo(resolvedDirectory);
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
                        if (ownsScreen) screenBitmap?.Dispose();   // giải phóng ảnh chụp vòng trước
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
            finally
            {
                if (ownsScreen) screenBitmap?.Dispose();
                foreach (var template in templates)
                    template.Dispose();
            }

            return "";
        }
        private string ResolveAssetDirectory(string relativeOrAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath))
            {
                return relativeOrAbsolutePath;
            }

            var candidates = new List<string>();
            if (Path.IsPathRooted(relativeOrAbsolutePath))
            {
                candidates.Add(relativeOrAbsolutePath);
            }
            else
            {
                candidates.Add(Path.GetFullPath(relativeOrAbsolutePath));
                candidates.Add(Path.Combine(AppContext.BaseDirectory, relativeOrAbsolutePath));

                string? processDirectory = Path.GetDirectoryName(Environment.ProcessPath);
                if (!string.IsNullOrWhiteSpace(processDirectory))
                {
                    candidates.Add(Path.Combine(processDirectory, relativeOrAbsolutePath));
                }
            }

            return candidates
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(Directory.Exists) ?? relativeOrAbsolutePath;
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
            LogHelper.SUCCESS("Đang change proxy");
            VATProxyService proxyService = new VATProxyService(this);
            return proxyService.ConnectProxy(proxy);
        }

        public bool ConnectProxyPreinstalled(string proxy)
        {
            LogHelper.SUCCESS("Đang change proxy đã cài sẵn");
            VATProxyService proxyService = new VATProxyService(this);
            return proxyService.ConnectProxy(proxy, installIfMissing: false);
        }
        public bool ConnectProxyADB(string proxy)
        {
            LogHelper.SUCCESS("Đang connect proxy");
            Shell($"settings put global http_proxy {proxy}");
            return true;
        }
        public bool DisconetProxyADB()
        {
            LogHelper.SUCCESS($"Đang disconet proxy");
            Shell($"settings put global http_proxy :0");
            return true;
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
            var devMetrics = DeviceMetricsRegistry.GetOrCreate(Device.Serial);
            LogHelper.Log($"Đang connect");
            int index = 0;
            const int MAX_RETRY = 10; // Không reconnect vô hạn — tránh cascade failure
            while (Running && index < MAX_RETRY)
            {
                ThrowIfStopped();
                index++;
                devMetrics.ConnectAdbLoopCount = index;
                MetricsCollector.GaugeSet($"adb.connect.loop.{Device.Serial}", index);
                string text = ProcessHelper.RunAdbMonitorCommand($"-s {Device.Serial} shell service check settings", 5);
                // Phải có output hợp lệ (không phải empty/timeout) và không chứa "not found"
                bool isOnline = !string.IsNullOrWhiteSpace(text) && !text.Contains("not found");
                if (isOnline)
                {
                    Device.IsAdbOnline = true;
                    devMetrics.ConnectAdbSuccessCount++;
                    LogHelper.Log($"Đã connect");
                    return true;
                }
                Device.IsAdbOnline = false;
                string reason = string.IsNullOrWhiteSpace(text) ? "timeout/no response" : "Can't find service: settings";
                devMetrics.LastError = reason;
                devMetrics.LastErrorTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                // Log condensed: chỉ log mỗi 5 lần để tránh spam
                if (index % 5 == 1 || index == MAX_RETRY)
                    LogHelper.Log($"Mất kết nối, chờ ExecuteAdb [{index}/{MAX_RETRY}] cmd: {reason}");
                ThrowIfStopped();
                if (index < MAX_RETRY)
                    ProcessHelper.RunAdbCommand($"-s {Device.Serial} shell reconnect");
                // Exponential backoff: 1s, 2s, 4s, 8s, ... tối đa 10s
                int backoffMs = Math.Min(1000 * (1 << Math.Min(index - 1, 4)), 10_000);
                InterruptibleSleep(backoffMs);
            }
            ThrowIfStopped();
            // Hết MAX_RETRY lần. Kiểm tra ATX trước khi đánh dấu chết:
            // nếu ATX còn sống (port 7912 mở) thì device vẫn hoạt động,
            // chỉ là ADB đang chậm/treo tạm thời.
            if (Device.Port > 0 && DeviceHealthCheckService.PingAtx(Device.Port, 2000))
            {
                LogHelper.Log($"Kết nối ADB thất bại nhưng ATX vẫn alive — giữ Live=true.");
                return false;
            }
            Device.IsLive = false;
            LogHelper.Log($"Kết nối ADB thất bại sau {MAX_RETRY} lần thử.");
            devMetrics.LastError = $"Failed after {MAX_RETRY} retries";
            devMetrics.LastErrorTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            return false;
        }
        public void AppClear(string package)
        {
            try
            {
                LogHelper.SUCCESS($"Xóa dữ liệu app [{package}]");

                // Force-stop app trước khi clear — app đang chạy có thể giữ lock trên data/accounts DB
                try { Shell("am force-stop " + package); } catch { }
                InterruptibleSleep(500);

                bool isFacebook = package == "com.facebook.katana"
                    || package == "com.facebook.lite"
                    || package == "com.facebook.orca"
                    || package == "com.facebook.messenger";

                if (isFacebook)
                {
                    // Disable app TRƯỚC để authenticator không thể re-register accounts khi framework restart
                    try { Shell($"pm disable-user --user 0 {package}"); } catch { }

                    // Xóa external storage data (pm clear không đụng tới /sdcard/Android/data/)
                    try { Shell($"su -c \"rm -rf /data/media/0/Android/data/{package}\""); } catch { }
                    try { Shell($"su -c \"rm -rf /sdcard/Android/data/{package}\""); } catch { }
                }

                for (int i = 0; i < 5; i++)
                {
                    Shell("pm clear " + package);
                    ADB.Shell("pm clear " + package, 5);
                }

                if (isFacebook)
                {
                    DeleteAccounts();

                    // Chỉ enable lại app SAU KHI đã xóa accounts thành công
                    try { Shell($"pm enable --user 0 {package}"); } catch { }
                }
            }
            catch
            {

            }

        }

        public void ClearFacebookData()
        {
            string[] facebookPackages =
            {
                "com.facebook.katana",
                "com.facebook.lite",
                "com.facebook.services",
                "com.facebook.appmanager",
                "com.facebook.system",
                "com.facebook.systemservice",
                "com.facebook.orca",
                "com.facebook.messenger"
            };

            try
            {
                LogHelper.SUCCESS("Xóa dữ liệu phiên Facebook cũ");

                // Dừng và vô hiệu hóa tạm thời toàn bộ thành phần Facebook để
                // không đăng ký lại Account Manager trong lúc đang dọn dữ liệu.
                foreach (string package in facebookPackages)
                {
                    try { Shell("am", "force-stop", package); } catch { }
                    try { Shell("pm", "disable-user", "--user", "0", package); } catch { }
                }
                InterruptibleSleep(500);

                foreach (string package in facebookPackages)
                {
                    try { Shell("su", "-c", $"rm -rf /data/media/0/Android/data/{package}"); } catch { }
                    try { Shell("su", "-c", $"rm -rf /sdcard/Android/data/{package}"); } catch { }
                    try { Shell("pm", "clear", package); } catch { }
                }

                // Xóa một lần duy nhất cho mỗi account, thay vì lặp lại khi
                // AppClear được gọi cho từng package Facebook phụ.
                bool accountsCleared = DeleteAccounts();

                foreach (string package in facebookPackages)
                {
                    try { Shell("pm", "enable", "--user", "0", package); } catch { }
                }

                if (!accountsCleared)
                    throw new InvalidOperationException("Không thể xác nhận đã xóa dữ liệu tài khoản Facebook.");
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[ClearFacebookData] Lỗi: {ex.Message}");
                throw;
            }
        }

        private bool DeleteAccounts()
        {
            try
            {
                // Force-stop tất cả Facebook apps trước để authenticator không giữ lock trên accounts DB
                string[] allFbPkgs = { "com.facebook.katana", "com.facebook.lite", "com.facebook.orca", "com.facebook.messenger" };
                foreach (var pkg in allFbPkgs)
                {
                    try { Shell($"am force-stop {pkg}"); } catch { }
                }
                InterruptibleSleep(1000);

                string sqlite = ResolveSqlite3();
                if (string.IsNullOrEmpty(sqlite))
                {
                    LogHelper.Log("[DeleteAccounts] Không tìm thấy/không push được sqlite3 — không thể xác nhận xóa account.");
                    return false;
                }

                // Nếu lần chạy trước đang giữ path push nhưng binary hỏng giữa chừng,
                // xóa cache để lần sau resolve lại và chỉ push lại khi thật sự cần.
                if (string.Equals(sqlite, PUSHED_SQLITE3_PATH, StringComparison.Ordinal) &&
                    !IsPushedSqlite3Usable())
                {
                    _sqlite3Resolved = null;
                    sqlite = ResolveSqlite3();
                }
                if (string.IsNullOrEmpty(sqlite))
                {
                    LogHelper.Log("[DeleteAccounts] sqlite3 không còn khả dụng — không thể xác nhận xóa account.");
                    return false;
                }

                // Query accounts từ CẢ HAI database CE và DE
                string query = "SELECT _id, name, type FROM accounts WHERE type LIKE 'com.facebook%';";
                string output = Shell($"su -c \"{sqlite} /data/system_ce/0/accounts_ce.db \\\"{query}\\\"\"");
                string outputDe = Shell($"su -c \"{sqlite} /data/system_de/0/accounts_de.db \\\"{query}\\\"\"");

                // Gộp kết quả từ cả 2 DB
                var allRows = new List<string>();
                if (!string.IsNullOrWhiteSpace(output))
                    allRows.AddRange(output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));
                if (!string.IsNullOrWhiteSpace(outputDe))
                    allRows.AddRange(outputDe.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));

                if (allRows.Count == 0)
                {
                    LogHelper.Log("[DeleteAccounts] Không tìm thấy Facebook accounts nào (hoặc CE storage chưa unlock).");
                    return true;
                }

                var ids = new List<int>();
                foreach (string row in allRows)
                {
                    string[] parts = row.Split('|');
                    if (parts.Length < 1 || !int.TryParse(parts[0].Trim(), out int id))
                        continue;
                    string accountName = parts.Length > 1 ? parts[1] : "unknown";
                    string accountType = parts.Length > 2 ? parts[2] : "unknown";
                    LogHelper.Log($"[DeleteAccounts] Sẽ xóa account: {accountType} ({accountName}) - ID={id}");
                    ids.Add(id);
                }

                if (ids.Count == 0)
                {
                    LogHelper.Log("[DeleteAccounts] Không parse được account id nào từ output.");
                    return false;
                }

                // Clear WebView cache TRƯỚC khi stop framework (pm cần system_server còn sống).
                try
                {
                    Shell("pm clear com.android.webview");
                    Shell("pm clear com.google.android.webview");
                }
                catch { }

                // Stop framework để đóng toàn bộ connection tới accounts_*.db
                bool stopped = StopFramework();
                InterruptibleSleep(1000);

                int deletedCount = 0;
                foreach (int id in ids)
                {
                    try
                    {
                        // Xóa từ accounts_de.db (device-encrypted)
                        Shell($"su -c \"{sqlite} /data/system_de/0/accounts_de.db 'DELETE FROM accounts WHERE _id = {id};'\"");

                        // Xóa cascade từ accounts_ce.db (credential-encrypted)
                        Shell($"su -c \"{sqlite} /data/system_ce/0/accounts_ce.db 'DELETE FROM authtokens WHERE accounts_id = {id};'\"");
                        Shell($"su -c \"{sqlite} /data/system_ce/0/accounts_ce.db 'DELETE FROM extras WHERE accounts_id = {id};'\"");
                        Shell($"su -c \"{sqlite} /data/system_ce/0/accounts_ce.db 'DELETE FROM grants WHERE accounts_id = {id};'\"");
                        Shell($"su -c \"{sqlite} /data/system_ce/0/accounts_ce.db 'DELETE FROM accounts WHERE _id = {id};'\"");

                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Log($"[DeleteAccounts] Lỗi xóa account ID={id}: {ex.Message}");
                    }
                }

                if (deletedCount > 0)
                {
                    // Reset sequence counter
                    Shell($"su -c \"{sqlite} /data/system_ce/0/accounts_ce.db 'DELETE FROM sqlite_sequence WHERE name = \\\"accounts\\\";'\"");

                    // Checkpoint WAL (TRUNCATE)
                    foreach (string db in new[] { "/data/system_ce/0/accounts_ce.db", "/data/system_de/0/accounts_de.db" })
                        Shell($"su -c \"{sqlite} {db} 'PRAGMA wal_checkpoint(TRUNCATE);'\"");

                    // Xóa trực tiếp file WAL/SHM/journal để chặn mọi khả năng phục hồi
                    foreach (string db in new[] { "/data/system_ce/0/accounts_ce.db", "/data/system_de/0/accounts_de.db" })
                    {
                        try { Shell($"su -c \"rm -f {db}-wal {db}-shm {db}-journal\""); } catch { }
                    }
                }

                // Start lại framework
                if (stopped)
                    StartFramework();
                else
                    KillSystemServer();

                // VERIFY: kiểm tra accounts đã thực sự bị xóa sau khi framework restart
                InterruptibleSleep(2000);
                string verifyOutput = Shell($"su -c \"{sqlite} /data/system_ce/0/accounts_ce.db \\\"{query}\\\"\"");
                bool accountsRemain = !string.IsNullOrWhiteSpace(verifyOutput) && verifyOutput.Contains("com.facebook", StringComparison.OrdinalIgnoreCase);
                if (accountsRemain)
                {
                    LogHelper.Log($"[DeleteAccounts] CẢNH BÁO: Accounts vẫn còn sau lần xóa đầu tiên! Thử xóa lại lần 2...");
                    // Retry lần 2 với framework đã chạy (không cần stop nữa)
                    foreach (string row in verifyOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] parts = row.Split('|');
                        if (parts.Length < 1 || !int.TryParse(parts[0].Trim(), out int retryId))
                            continue;
                        try
                        {
                            Shell($"su -c \"{sqlite} /data/system_ce/0/accounts_ce.db 'DELETE FROM authtokens WHERE accounts_id = {retryId};'\"");
                            Shell($"su -c \"{sqlite} /data/system_ce/0/accounts_ce.db 'DELETE FROM extras WHERE accounts_id = {retryId};'\"");
                            Shell($"su -c \"{sqlite} /data/system_ce/0/accounts_ce.db 'DELETE FROM grants WHERE accounts_id = {retryId};'\"");
                            Shell($"su -c \"{sqlite} /data/system_ce/0/accounts_ce.db 'DELETE FROM accounts WHERE _id = {retryId};'\"");
                            Shell($"su -c \"{sqlite} /data/system_de/0/accounts_de.db 'DELETE FROM accounts WHERE _id = {retryId};'\"");
                            LogHelper.Log($"[DeleteAccounts] Retry xóa account ID={retryId}");
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Log($"[DeleteAccounts] Retry lỗi ID={retryId}: {ex.Message}");
                        }
                    }
                    // Xóa WAL lần nữa
                    foreach (string db in new[] { "/data/system_ce/0/accounts_ce.db", "/data/system_de/0/accounts_de.db" })
                    {
                        try { Shell($"su -c \"{sqlite} {db} 'PRAGMA wal_checkpoint(TRUNCATE);'\""); } catch { }
                        try { Shell($"su -c \"rm -f {db}-wal {db}-shm {db}-journal\""); } catch { }
                    }

                    verifyOutput = Shell($"su -c \"{sqlite} /data/system_ce/0/accounts_ce.db \\\"{query}\\\"\"");
                    accountsRemain = !string.IsNullOrWhiteSpace(verifyOutput) && verifyOutput.Contains("com.facebook", StringComparison.OrdinalIgnoreCase);
                }

                if (accountsRemain)
                {
                    LogHelper.Log("[DeleteAccounts] Vẫn còn Facebook account sau khi retry.");
                    return false;
                }

                if (deletedCount > 0)
                    LogHelper.SUCCESS($"[DeleteAccounts] Đã xóa {deletedCount} Facebook accounts.");
                else
                    LogHelper.Log("[DeleteAccounts] Không có Facebook accounts nào được xóa.");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[DeleteAccounts] Lỗi: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Dừng Android framework (`stop`) để system_server đóng connection tới accounts_*.db
        /// trước khi sửa DB. Nhẹ hơn kill: giữ nguyên adbd/zygote, chỉ dừng các service Java.
        /// Trả về true nếu đã phát lệnh stop thành công.
        /// </summary>
        private bool StopFramework()
        {
            try
            {
                LogHelper.Log("[DeleteAccounts] stop framework để đóng DB account...");
                Shell("su -c \"stop\"");
                Thread.Sleep(2000); // Đợi service Java tắt, DB được đóng.
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[DeleteAccounts] Không stop được framework: {ex.Message}");
                return false;
            }
        }

        /// <summary>Khởi động lại framework sau khi đã stop + sửa DB.</summary>
        private void StartFramework()
        {
            try
            {
                LogHelper.Log("[DeleteAccounts] start lại framework...");
                Shell("su -c \"start\"");
                Thread.Sleep(3000); // Đợi system_server + AccountManagerService lên lại.
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[DeleteAccounts] Không start lại được framework (có thể cần reboot thủ công): {ex.Message}");
            }
        }

        /// <summary>
        /// Fallback khi không stop được framework: kill system_server để zygote respawn,
        /// buộc AccountManagerService đọc lại DB từ đĩa (xóa cache account cũ trong RAM).
        /// Dùng pidof thay killall vì nhiều ROM tối giản không có killall.
        /// </summary>
        private void KillSystemServer()
        {
            try
            {
                LogHelper.Log("[DeleteAccounts] kill system_server để reload account database...");
                // pidof có mặt trên hầu hết ROM (toybox); fallback sang killall nếu pidof rỗng.
                Shell("su -c \"kill $(pidof system_server) 2>/dev/null || killall system_server\"");
                Thread.Sleep(3000); // Đợi system_server respawn.
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[DeleteAccounts] Không kill được system_server (device có thể cần reboot thủ công): {ex.Message}");
            }
        }

        // Đường dẫn sqlite3 đã push lên device (dùng khi ROM không kèm sẵn binary).
        private const string PUSHED_SQLITE3_PATH = "/data/local/tmp/sqlite3";

        // Cache kết quả resolve sqlite3 theo serial để không phải dò/push lại mỗi account.
        private string _sqlite3Resolved;

        /// <summary>
        /// Trả về lệnh sqlite3 chạy được trên device:
        /// - Nếu ROM đã có sqlite3 trong PATH → trả về "sqlite3".
        /// - Nếu chưa có → push binary tĩnh (bundle theo tool, theo ABI) lên
        ///   /data/local/tmp/sqlite3, chmod +x rồi trả về đường dẫn tuyệt đối.
        /// - Không resolve được → chuỗi rỗng.
        /// </summary>
        private string ResolveSqlite3()
        {
            // Bản ROM sqlite3 không cần cache validation; bản đã push phải được
            // kiểm tra lại để nếu file bị xóa giữa hai account thì chỉ resolve/push
            // lại đúng lúc cần thiết.
            if (string.Equals(_sqlite3Resolved, PUSHED_SQLITE3_PATH, StringComparison.Ordinal) &&
                !IsPushedSqlite3Usable())
            {
                _sqlite3Resolved = null;
            }

            // Đã resolve trong phiên này rồi thì dùng lại (kể cả kết quả rỗng — tránh dò/push lại
            // cho từng account khi máy vốn không có root/sqlite3).
            if (_sqlite3Resolved != null)
                return _sqlite3Resolved;

            _sqlite3Resolved = ResolveSqlite3Core();
            return _sqlite3Resolved;
        }

        private string ResolveSqlite3Core()
        {
            try
            {
                // 0) Có root không? Không root thì mọi bước dưới đều vô nghĩa — báo rõ để khỏi mơ hồ.
                string rootCheck = Shell("su -c \"id -u\"");
                if (string.IsNullOrWhiteSpace(rootCheck) || !rootCheck.Trim().StartsWith("0"))
                {
                    LogHelper.Log($"[ResolveSqlite3] Máy KHÔNG có quyền root (su trả về: '{rootCheck?.Trim()}'). " +
                                  "Không thể xóa account bằng sqlite3. Cần device đã root + cấp quyền su cho shell.");
                    return string.Empty;
                }

                // 1) sqlite3 có sẵn trên device (ROM kèm sẵn)?
                string which = Shell("su -c \"command -v sqlite3 || which sqlite3\"");
                if (!string.IsNullOrWhiteSpace(which) && which.Contains("sqlite3"))
                {
                    LogHelper.Log("[ResolveSqlite3] Dùng sqlite3 có sẵn trên ROM.");
                    return "sqlite3";
                }

                // 2) Bản đã push trước đó còn chạy được không?
                if (IsPushedSqlite3Usable())
                {
                    LogHelper.Log($"[ResolveSqlite3] Dùng lại bản đã push: {PUSHED_SQLITE3_PATH}");
                    return PUSHED_SQLITE3_PATH;
                }

                // 3) Push binary bundle theo ABI (có retry — ADB sync có thể fail transient).
                string local = LocalSqlite3BinaryPath();
                if (string.IsNullOrEmpty(local) || !System.IO.File.Exists(local))
                {
                    LogHelper.Log("[ResolveSqlite3] Không có binary bundle để push (xem log LocalSqlite3BinaryPath ở trên).");
                    return string.Empty;
                }

                LogHelper.Log($"[ResolveSqlite3] Push {local} → {PUSHED_SQLITE3_PATH}");
                bool pushed = false;
                const int maxPushRetry = 3;
                for (int pushAttempt = 0; pushAttempt < maxPushRetry && !pushed; pushAttempt++)
                {
                    if (pushAttempt > 0)
                    {
                        LogHelper.Log($"[ResolveSqlite3] Retry push lần {pushAttempt + 1}/{maxPushRetry}...");
                        InterruptibleSleep(1000);
                        // Reconnect ADB trước khi retry — kết nối sync có thể đã bị đứt.
                        ConnectAdb();
                    }
                    RunTime($"PUSH sqlite3 (attempt {pushAttempt + 1})", () => { pushed = Push(local, PUSHED_SQLITE3_PATH); });
                }
                if (!pushed)
                {
                    LogHelper.Log($"[ResolveSqlite3] Push thất bại sau {maxPushRetry} lần thử (không ghi được vào {PUSHED_SQLITE3_PATH}).");
                    return string.Empty;
                }
                Shell($"su -c \"chmod 755 {PUSHED_SQLITE3_PATH}\"");

                if (IsPushedSqlite3Usable())
                {
                    LogHelper.SUCCESS($"[ResolveSqlite3] Push + chạy OK: {PUSHED_SQLITE3_PATH}");
                    return PUSHED_SQLITE3_PATH;
                }

                LogHelper.Log("[ResolveSqlite3] Đã push nhưng binary không chạy được. " +
                              "Thường do: (a) sai ABI so với CPU device, hoặc (b) SELinux/noexec chặn thực thi ở /data/local/tmp.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[ResolveSqlite3] Lỗi: {ex.Message}");
                return string.Empty;
            }
        }

        private bool IsPushedSqlite3Usable()
        {
            try
            {
                string ver = Shell($"su -c \"{PUSHED_SQLITE3_PATH} -version\"");
                return !string.IsNullOrWhiteSpace(ver) && char.IsDigit(ver.TrimStart().FirstOrDefault());
            }
            catch { return false; }
        }

        /// <summary>
        /// Đường dẫn binary sqlite3 bundle kèm tool, sắp theo ABI:
        ///   {BaseDirectory}\resources\sqlite3\{abi}\sqlite3
        /// Đặt sẵn file cho các ABI cần dùng (arm64-v8a, armeabi-v7a, x86, x86_64).
        /// </summary>
        private string LocalSqlite3BinaryPath()
        {
            try
            {
                string abi = GetProp("ro.product.cpu.abi").Trim();
                if (string.IsNullOrWhiteSpace(abi))
                {
                    LogHelper.Log("[LocalSqlite3BinaryPath] Không detect được ABI.");
                    return string.Empty;
                }

                // Map ABI → subfolder (x86_64 dùng chung binary 386 nếu thiếu).
                string abiFolder = abi;
                string fallbackFolder = null;

                switch (abi)
                {
                    case "x86_64":
                        abiFolder = "x86_64";
                        fallbackFolder = "x86"; // fallback về x86 32-bit nếu thiếu 64-bit
                        break;
                    case "x86":
                        abiFolder = "x86";
                        break;
                    case "arm64-v8a":
                        abiFolder = "arm64-v8a";
                        fallbackFolder = "armeabi-v7a";
                        break;
                    case "armeabi-v7a":
                    case "armeabi":
                        abiFolder = "armeabi-v7a";
                        break;
                    default:
                        LogHelper.Log($"[LocalSqlite3BinaryPath] ABI '{abi}' chưa được support.");
                        return string.Empty;
                }

                string resourcesRoot = System.IO.Path.Combine(AppContext.BaseDirectory, "resources", "sqlite3");
                string primaryPath = System.IO.Path.Combine(resourcesRoot, abiFolder, "sqlite3");
                if (System.IO.File.Exists(primaryPath))
                    return primaryPath;

                // Thử fallback nếu có.
                if (!string.IsNullOrEmpty(fallbackFolder))
                {
                    string fallbackPath = System.IO.Path.Combine(resourcesRoot, fallbackFolder, "sqlite3");
                    if (System.IO.File.Exists(fallbackPath))
                    {
                        LogHelper.Log($"[LocalSqlite3BinaryPath] Dùng fallback binary: {fallbackPath}");
                        return fallbackPath;
                    }
                }

                // Binary phải được bundle sẵn theo tool (build/publish copy vào resources\sqlite3\{abi}).
                // KHÔNG auto-download nữa: bản Termux là dynamic-linked (cần libz/libreadline không có
                // trên Android gốc) nên tải về cũng không chạy; ngoài ra GetAsync blocking 5 phút làm
                // treo cả luồng job. Thiếu file ở đây = lỗi đóng gói, cần bổ sung binary vào resources.
                LogHelper.Log($"[LocalSqlite3BinaryPath] Thiếu binary bundle cho ABI '{abi}'. Mong đợi: {primaryPath}. " +
                              $"Kiểm tra thư mục resources\\sqlite3\\{abiFolder}\\sqlite3 cạnh file .exe.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[LocalSqlite3BinaryPath] Lỗi: {ex.Message}");
                return string.Empty;
            }
        }

        public string Shell(params object[] argv)
        {
            ThrowIfStopped();
            var devMetrics = DeviceMetricsRegistry.GetOrCreate(Device.Serial);
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
                    devMetrics.AdbShellRetryCount++;
                    MetricsCollector.Increment("adb.shell.retry");
                    LogHelper.Log($"[Shell] Exception lần {retry + 1}: {ex.Message}");
                    ThrowIfStopped();

                    // Chỉ gọi Connect (reconnect ADB+ATX) sau lần retry cuối cùng.
                    // Trước đó chỉ delay ngắn rồi retry Shell — tránh gọi full reconnect
                    // mỗi khi gặp transient error (semaphore bận, ATX chậm...).
                    if (retry >= maxRetry - 1)
                    {
                        Connect(CurrentAutomationType);
                    }
                    else
                    {
                        InterruptibleSleep(500);
                    }
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
            if(! ADBSocket.Push(Device.Serial, file, path, mode))
            {
                _adb.CMD($"push \"{file}\" \"{path}\"", 60);
            }
            return true;
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
            ForcePortraitOrientation();

            if (width > 0 && height > 0)
                TryShell("wm", "size", $"{width}x{height}");

            if (density > 0)
                TryShell("wm", "density", density.ToString());

            ResetWindowManagerScaling();
            TryShell("am", "broadcast", "-a", "android.intent.action.CONFIGURATION_CHANGED");
        }

        /// <summary>
        /// Force screen orientation to portrait mode.
        /// This prevents apps like Facebook from rotating to landscape.
        /// </summary>
        public void ForcePortraitOrientation()
        {
            try { Shell("settings", "put", "system", "accelerometer_rotation", "0"); } catch { }
            try { Shell("settings", "put", "system", "user_rotation", "0"); } catch { }
            try { Shell("content", "insert", "--uri", "content://settings/system", "--bind", "name:s:accelerometer_rotation", "--bind", "value:i:0"); } catch { }
            try { Shell("content", "insert", "--uri", "content://settings/system", "--bind", "name:s:user_rotation", "--bind", "value:i:0"); } catch { }
            try { Shell("wm", "user-rotation", "lock", "0"); } catch { }
            try { Shell("cmd", "window", "set-ignore-orientation-request", "true"); } catch { }
        }

        private void PrepareFullscreenAppLaunch(string package)
        {
            SetSize();

            // Best-effort: ROM nào không hỗ trợ sẽ bỏ qua, nhưng giúp thoát trạng thái
            // freeform/pop-up khiến Facebook mở thành cửa sổ nhỏ trong màn hình thiết bị.
            TryShell("settings", "put", "global", "force_resizable_activities", "0");
            TryShell("settings", "put", "global", "enable_freeform_support", "0");
            TryShell("settings", "put", "global", "freeform_window_management", "0");
            TryShell("settings", "put", "global", "multi_window_enabled", "0");
            TryShell("settings", "put", "secure", "multi_window_enabled", "0");
            TryShell("settings", "put", "system", "multi_window_enabled", "0");
            TryShell("settings", "put", "global", "sem_multi_window_enabled", "0");
            TryShell("settings", "put", "global", "sem_freeform_window_enabled", "0");
            TryShell("input", "keyevent", "KEYCODE_HOME");
            TryResetPackageTaskBounds(package);
        }

        private void NormalizeFullscreenAfterLaunch(string package)
        {
            ForcePortraitOrientation();
            ResetWindowManagerScaling();
            TryResetPackageTaskBounds(package);
        }

        private void ResetDisplayOverridesIfNeeded()
        {
            try
            {
                string size = Shell("wm", "size");
                if (size.Contains("Override size", StringComparison.OrdinalIgnoreCase))
                    TryShell("wm", "size", "reset");
            }
            catch
            {
            }

            try
            {
                string density = Shell("wm", "density");
                if (density.Contains("Override density", StringComparison.OrdinalIgnoreCase))
                    TryShell("wm", "density", "reset");
            }
            catch
            {
            }

            ResetWindowManagerScaling();
        }

        private void ResetWindowManagerScaling()
        {
            TryShell("wm", "scaling", "auto");
            TryShell("wm", "overscan", "reset");
        }

        private void TryResetPackageTaskBounds(string package)
        {
            if (string.IsNullOrWhiteSpace(package)) return;

            var size = GetScreenResolutionSafe();
            foreach (string taskId in FindPackageTaskIds(package))
            {
                TryShell("am", "stack", "move-task", taskId, "1", "true");
                TryShell("cmd", "activity", "stack", "move-task", taskId, "1", "true");
                TryShell("cmd", "activity", "task", "resize", taskId, "0", "0", size.X.ToString(), size.Y.ToString());
                TryShell("am", "task", "resize", taskId, "0", "0", size.X.ToString(), size.Y.ToString());
                TryShell("am", "stack", "resize", "1", "0", "0", size.X.ToString(), size.Y.ToString());
                TryShell("cmd", "activity", "task", "move-top", taskId);
            }
        }

        private IEnumerable<string> FindPackageTaskIds(string package)
        {
            var result = new HashSet<string>();
            foreach (string command in new[] { "activities", "recents" })
            {
                try
                {
                    string dumpsys = Shell("dumpsys", "activity", command);
                    string lastTaskId = "";
                    foreach (string line in dumpsys.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string trimmed = line.Trim();
                        foreach (Match taskMatch in Regex.Matches(trimmed, @"(?:TaskRecord|Task)\{[^#]*#(?<id>\d+)\b|taskId=(?<id>\d+)\b", RegexOptions.IgnoreCase))
                        {
                            string id = taskMatch.Groups["id"].Value;
                            if (!string.IsNullOrWhiteSpace(id))
                                lastTaskId = id;
                        }

                        if (!trimmed.Contains(package, StringComparison.OrdinalIgnoreCase)) continue;

                        bool foundOnLine = false;
                        foreach (Match match in Regex.Matches(trimmed, @"#(?<id>\d+)\b|taskId=(?<id>\d+)\b", RegexOptions.IgnoreCase))
                        {
                            string id = match.Groups["id"].Value;
                            if (!string.IsNullOrWhiteSpace(id))
                            {
                                result.Add(id);
                                foundOnLine = true;
                            }
                        }

                        if (!foundOnLine && !string.IsNullOrWhiteSpace(lastTaskId))
                            result.Add(lastTaskId);
                    }
                }
                catch
                {
                }
            }

            return result;
        }

        private void TryShell(params object[] argv)
        {
            try
            {
                Shell(argv);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
            }
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
                var lines = wmSize.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var preferredLines = lines
                    .Where(line => line.Contains("Override size", StringComparison.OrdinalIgnoreCase))
                    .Concat(lines.Where(line => !line.Contains("Override size", StringComparison.OrdinalIgnoreCase)));

                foreach (string line in preferredLines)
                {
                    Match match = Regex.Match(line, "(?<w>\\d+)x(?<h>\\d+)");
                    if (match.Success)
                    {
                        return new System.Drawing.Point(
                            int.Parse(match.Groups["w"].Value),
                            int.Parse(match.Groups["h"].Value));
                    }
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
            LogHelper.SUCCESS("Đang gửi nội dung nhập liệu");
            Shell("input", "text", text);
        }
        public void SendTextADB(string xpath, string text, int timeout = 10, string xml = "", bool clear = true)
        {
            if (string.IsNullOrEmpty(text)) return;
            ElementWithAttributes(xpath, timeout, xml, true);
            if (clear)
            {
                CLearText();
            }
            LogHelper.SUCCESS("Đang gửi nội dung nhập liệu");
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
                        using Bitmap screen = Screenshot();
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
            LogHelper.SUCCESS("Đang gửi nội dung nhập liệu");
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
                        using Bitmap screen = Screenshot();
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
                        XmlNodeList nodeList = SelectNodesWithCandidates(xmlDoc, xpath);

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
                        XmlNodeList nodeList = SelectNodesWithCandidates(xmlDoc, xpath);
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
                            XmlNodeList nodeList = SelectNodesWithCandidates(xmlDoc, xpathValue);
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
                                    var nodes = SelectWithCandidates(nav, xpath.ToLower());
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

                    XmlNodeList nodeList = SelectNodesWithCandidates(xmlDoc, xpath);
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
                                var nodes = SelectWithCandidates(nav, xpath);
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
                                if (TryFindBounds(navigator, xpath, out var bounds))
                                {
                                    if (click)
                                    {
                                        var point = new RectangleArea(bounds).GetCenterPoint();
                                        return Click(point.X, point.Y);
                                    }
                                    return true;
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
                                    if (TryFindBounds(navigator, xpath, out var bounds))
                                    {
                                        if (click)
                                        {
                                            var point = new RectangleArea(bounds).GetCenterPoint();
                                            return Click(point.X, point.Y);
                                        }
                                        return true;
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

                    XmlNodeList nodeList = SelectNodesWithCandidates(xmlDoc, xpath);
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
            PrepareFullscreenAppLaunch(package);

            if (stop)
            {
                StopApp(package);
            }
            if (monkey)
            {
                string launchActivity = string.IsNullOrWhiteSpace(activity) ? ResolveMainActivityByAdb(package) : activity;
                if (!string.IsNullOrWhiteSpace(launchActivity))
                {
                    StartLauncherActivity(package, launchActivity);
                }
                else
                {
                    Shell("monkey", "-p", package, "-c", "android.intent.category.LAUNCHER", "1");
                }

                if (wait)
                {
                    AppWait(package);
                }
                NormalizeFullscreenAfterLaunch(package);
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
                NormalizeFullscreenAfterLaunch(package);
                return;
            }

            LogHelper.Log($"AppStart: {package}/{activity}");
            StartLauncherActivity(package, activity);
            if (wait)
            {
                AppWait(package);
            }
            NormalizeFullscreenAfterLaunch(package);
        }

        private void StartLauncherActivity(string package, string activity)
        {
            string component = $"{package}/{activity}";
            string result = Shell(
                "am", "start",
                "--activity-new-task",
                "--activity-clear-task",
                "--activity-clear-top",
                "--activity-reset-task-if-needed",
                "-a", "android.intent.action.MAIN",
                "-c", "android.intent.category.LAUNCHER",
                "-n", component);

            if (IsAmStartFailure(result))
            {
                Shell("am", "start", "-a", "android.intent.action.MAIN", "-c", "android.intent.category.LAUNCHER", "-n", component);
            }
        }

        private static bool IsAmStartFailure(string result)
        {
            if (string.IsNullOrWhiteSpace(result)) return false;
            return result.Contains("Error", StringComparison.OrdinalIgnoreCase)
                || result.Contains("Exception", StringComparison.OrdinalIgnoreCase)
                || result.Contains("Unknown option", StringComparison.OrdinalIgnoreCase)
                || result.Contains("not found", StringComparison.OrdinalIgnoreCase);
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
        private IEnumerable<string> ExpandXPathCandidates(string xpath)
        {
            if (string.IsNullOrWhiteSpace(xpath))
            {
                yield break;
            }

            yield return xpath;

            string classAttributeXPath = ConvertAndroidClassTagsToClassPredicates(xpath);
            if (!string.Equals(classAttributeXPath, xpath, StringComparison.Ordinal))
            {
                yield return classAttributeXPath;
            }
        }

        private static string ConvertAndroidClassTagsToClassPredicates(string xpath)
        {
            return AndroidClassXPathStepRegex.Replace(xpath, match =>
            {
                string axis = match.Groups["axis"].Value;
                string className = match.Groups["class"].Value;
                string predicates = match.Groups["predicates"].Value;
                return $"{axis}*[@class='{className}']{predicates}";
            });
        }

        private XmlNodeList SelectNodesWithCandidates(XmlDocument xmlDoc, string xpath)
        {
            foreach (string candidate in ExpandXPathCandidates(xpath))
            {
                try
                {
                    XmlNodeList nodes = xmlDoc.SelectNodes(candidate);
                    if (nodes != null && nodes.Count > 0)
                    {
                        return nodes;
                    }
                }
                catch
                {
                    // Try the next compatible XPath form.
                }
            }

            return xmlDoc.SelectNodes("//*[false()]");
        }

        private XPathNodeIterator SelectWithCandidates(XPathNavigator navigator, string xpath)
        {
            foreach (string candidate in ExpandXPathCandidates(xpath))
            {
                try
                {
                    XPathNodeIterator nodes = navigator.Select(candidate);
                    if (nodes.Count > 0)
                    {
                        return nodes;
                    }
                }
                catch
                {
                    // Try the next compatible XPath form.
                }
            }

            return navigator.Select("//*[false()]");
        }

        private bool TryFindBounds(XPathNavigator navigator, string xpath, out string bounds)
        {
            bounds = "";

            foreach (string candidate in ExpandXPathCandidates(xpath))
            {
                try
                {
                    XPathNodeIterator nodeIterator = navigator.Select(candidate);
                    while (nodeIterator.MoveNext())
                    {
                        string value = nodeIterator.Current?.GetAttribute("bounds", "") ?? "";
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            bounds = value;
                            return true;
                        }
                    }
                }
                catch
                {
                    // Try the next compatible XPath form.
                }
            }

            foreach (string candidate in ExpandXPathCandidates(xpath))
            {
                if (TryFindBoundsCaseInsensitive(navigator, candidate, out bounds))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryFindBoundsCaseInsensitive(XPathNavigator navigator, string xpath, out string bounds)
        {
            bounds = "";
            if (!CanUseManualAttributeFallback(xpath))
            {
                return false;
            }

            var conditions = ExtractAttributeConditions(xpath);
            if (conditions.Count == 0)
            {
                return false;
            }

            string pathWithoutConditions = RemoveAttributeConditions(xpath);
            try
            {
                XPathNodeIterator nodeIterator = navigator.Select(pathWithoutConditions);
                while (nodeIterator.MoveNext())
                {
                    bool matchAll = true;
                    foreach (var kv in conditions)
                    {
                        string actualValue = nodeIterator.Current?.GetAttribute(kv.Key, "") ?? "";
                        if (!string.Equals(actualValue.Trim(), kv.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            matchAll = false;
                            break;
                        }
                    }

                    if (!matchAll)
                    {
                        continue;
                    }

                    string value = nodeIterator.Current?.GetAttribute("bounds", "") ?? "";
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        bounds = value;
                        return true;
                    }
                }
            }
            catch
            {
                // Fallback only; ignore invalid XPath here.
            }

            return false;
        }

        private bool CanUseManualAttributeFallback(string xpath)
        {
            string lowered = xpath.ToLowerInvariant();
            string[] blockedPatterns =
            {
                " or ",
                " and ",
                "contains(",
                "starts-with(",
                "translate(",
                "parent::",
                "child::",
                "following",
                "preceding",
                "last()",
                ")["
            };

            if (blockedPatterns.Any(pattern => lowered.Contains(pattern)))
            {
                return false;
            }

            string pathWithoutConditions = RemoveAttributeConditions(xpath).Trim();
            return pathWithoutConditions == "//*" || pathWithoutConditions == "//node";
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
                                    if (TryFindBounds(navigator, xpath, out _))
                                    {
                                        return xpath;
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
        public bool AreAppPermissionsGranted(string package)
        {
            try
            {
                string output = Shell("dumpsys", "package", package);
                if (string.IsNullOrWhiteSpace(output)) return false;

                // dumpsys package thay đổi format theo phiên bản Android. Chỉ kiểm
                // tra các quyền runtime thật sự cần; MANAGE_EXTERNAL_STORAGE là
                // app-op đặc biệt và không có dòng granted ổn định.
                string[] permissions =
                {
                    "android.permission.READ_CONTACTS",
                    "android.permission.READ_EXTERNAL_STORAGE",
                    "android.permission.WRITE_EXTERNAL_STORAGE",
                    "android.permission.CAMERA",
                    "android.permission.RECORD_AUDIO",
                    "android.permission.CALL_PHONE"
                };

                foreach (string permission in permissions)
                {
                    bool granted = Regex.IsMatch(
                        output,
                        $@"{Regex.Escape(permission)}\\s*:\\s*granted(?:=|\\s+)?true|{Regex.Escape(permission)}\\s*=\\s*granted|{Regex.Escape(permission)}\\s+granted=true",
                        RegexOptions.IgnoreCase);
                    if (!granted) return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[AreAppPermissionsGranted] Lỗi: {ex.Message}");
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
