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
        public bool RebootAndWaitForDeviceReady(string? reason = null)
        {
            // LỊCH SỬ CHẠY: mọi lệnh reboot của tool đều đi qua đây -> ghi 1 dòng REBOOT
            // nêu serial + lý do tường minh + stack trace best-effort. Dòng này là bằng
            // chứng "máy reboot do tool, ở bước nào". KHÔNG đổi hành vi reboot.
            RunHistoryLog.Reboot(Device?.Serial ?? "?",
                reason ?? "(không nêu lý do — gọi RebootAndWaitForDeviceReady trực tiếp)");
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
            string serial = Device?.Serial ?? "?";
            DeviceChangeLog.Write(serial, "ChangInfo: VÀO bước đổi thiết bị.");

            // Kiểm tra root bằng lệnh adb timeout cứng (tránh Shell() retry+Connect treo),
            // và so khớp lỏng (chứa "root") vì output su có thể kèm khoảng trắng/dòng thừa.
            string who = ProcessHelper.RunAdbCommand(
                $"-s {serial} shell su -c whoami", 8) ?? string.Empty;
            bool isRoot = who.IndexOf("root", StringComparison.OrdinalIgnoreCase) >= 0;
            DeviceChangeLog.Write(serial, $"ChangInfo: whoami=[{who.Trim()}] isRoot={isRoot}");

            if (!isRoot)
            {
                LogHelper.ERROR("Không phải root - bỏ qua đổi thiết bị");
                return false;
            }
            LogHelper.SUCCESS($"Đang thay đổi thiết bị!");
            MaxChangeService maxChangeService = new MaxChangeService(this);
            return await maxChangeService.Change(filePath, backup, brand, country);
        }

        // ── FACADE PUBLIC CHO MIRROR-GUARD (v12) ────────────────────────────────────────
        // MirrorSuppressor là `internal` của AutoAndroid; Sunny.Subd.Core (assembly khác) KHÔNG
        // instantiate được. Hai facade dưới đây để luồng job (MainService/FacebookRegsiner) giữ
        // mirror-guard SUỐT cả cửa sổ nguy hiểm (đổi thiết bị -> proxy -> mở Facebook) thay vì
        // chỉ trong vài giây Change() như trước — vá khoảng hở gây đen màn hình lúc mở FB.
        //
        // _mirrorGuard là MỘT instance duy nhất cho cặp Start/Stop của luồng job (vì _started là
        // per-instance). Ref-count THEO SERIAL nằm trong MirrorSuppressor (static) nên guard LỒNG
        // của Change() (Start/Stop riêng, instance riêng) KHÔNG kill watchdog nền của guard ngoài.
        private MirrorSuppressor? _mirrorGuard;

        /// <summary>
        /// Bật mirror-guard cho SUỐT vòng account (idempotent theo instance). Best-effort, không ném.
        /// Gọi ở ĐẦU vòng lặp account; tắt bằng <see cref="MirrorGuardStop"/> trong finally.
        /// </summary>
        public void MirrorGuardStart()
        {
            try
            {
                string serial = Device?.Serial ?? string.Empty;
                if (string.IsNullOrWhiteSpace(serial)) return;
                _mirrorGuard ??= new MirrorSuppressor(serial);
                _mirrorGuard.Start();
            }
            catch (Exception ex)
            {
                DeviceChangeLog.Write(Device?.Serial ?? "?",
                    $"MIRROR-GUARD(facade): Start lỗi (bỏ qua, không chặn job): {ex.Message}");
            }
        }

        /// <summary>
        /// Tắt mirror-guard của luồng job (chỉ thật sự kill watchdog khi ref-count về 0).
        /// Best-effort, không ném. Gọi trong finally của vòng lặp account.
        /// </summary>
        public void MirrorGuardStop()
        {
            try
            {
                _mirrorGuard?.Stop();
            }
            catch (Exception ex)
            {
                DeviceChangeLog.Write(Device?.Serial ?? "?",
                    $"MIRROR-GUARD(facade): Stop lỗi (watchdog tự hết): {ex.Message}");
            }
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

                // Gộp nhiều lệnh vào MỘT phiên shell thay vì ~48 lời gọi adb tuần tự
                // (8 package x 6 lệnh): mỗi lời gọi tốn ~120-150ms round-trip, nên bước
                // này trước đây mất vài giây đến vài chục giây; đo trên thiết bị thật
                // dạng gộp chỉ còn <1s.
                // Dừng và vô hiệu hóa tạm thời toàn bộ thành phần Facebook để
                // không đăng ký lại Account Manager trong lúc đang dọn dữ liệu.
                string stopBatch = string.Join("; ", facebookPackages.Select(p => $"am force-stop {p}"));
                string disableBatch = string.Join("; ", facebookPackages.Select(p => $"pm disable-user --user 0 {p}"));
                try { Shell(stopBatch + "; " + disableBatch); } catch { }
                InterruptibleSleep(500);

                // rm -rf dưới root: gộp mọi đường dẫn vào MỘT lệnh (đã kiểm chứng trên
                // device: cả dạng `su -c rm -rf <dir>` lẫn dạng quote đều xóa đúng).
                string rmPaths = string.Join(" ", facebookPackages.SelectMany(p => new[]
                {
                    $"/data/media/0/Android/data/{p}",
                    $"/sdcard/Android/data/{p}",
                }));
                try { Shell("su", "-c", $"\"rm -rf {rmPaths}\""); } catch { }

                // pm clear từng package nhưng trong cùng một phiên shell.
                string clearBatch = string.Join("; ", facebookPackages.Select(p => $"pm clear {p}"));
                try { Shell(clearBatch); } catch { }

                // Xóa một lần duy nhất cho mỗi account, thay vì lặp lại khi
                // AppClear được gọi cho từng package Facebook phụ.
                bool accountsCleared = DeleteAccounts();

                string enableBatch = string.Join("; ", facebookPackages.Select(p => $"pm enable --user 0 {p}"));
                try { Shell(enableBatch); } catch { }

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
                // Caller (ClearFacebookData) đã force-stop toàn bộ package Facebook ngay
                // trước đó, không lặp lại ở đây để khỏi tốn thêm ~1s ngủ + 4 lời gọi shell.

                string sqlite = ResolveSqlite3();
                if (string.IsNullOrEmpty(sqlite))
                {
                    // ── FIX B1 (v19) ── TRƯỚC ĐÂY trả `false`, khiến ClearFacebookData (:911) NÉM
                    // InvalidOperationException → MainService.ClearPreviousAccountDataAsync (:650-652)
                    // đặt `Device.IsLive = false; Running = false` → THIẾT BỊ BỊ LOẠI KHỎI JOB VĨNH
                    // VIỄN chỉ vì thiếu một binary chẩn đoán. sqlite3 KHÔNG phải thứ xoá account
                    // (`pm clear` ở :901-902 mới là thứ xoá); nó chỉ là bước XÁC NHẬN. Mất khả năng
                    // xác nhận thì ghi log bền vững rồi TRẢ TRUE (tin pm clear) — một acc thừa row
                    // account được login đè lên, tốt hơn nhiều so với mất cả thiết bị khỏi farm.
                    // Bằng chứng `pm clear` ĐÃ xoá row: median bước1 = 2.59s trên n=339 lần chuyển
                    // acc production (20 máy) — nếu đường stop/start chạy thì tối thiểu ~9.6s
                    // (2s StopFramework + 3s StartFramework + 2s sleep verify + ~17 lời gọi shell).
                    // Tức 332/339 (98%) LẦN CHUYỂN ACC ĐÃ VỀ SẠCH MÀ KHÔNG CẦN restart framework.
                    DeviceChangeLog.Write(Device?.Serial ?? "?",
                        $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][B1] sqlite3 không khả dụng → BỎ QUA bước verify, tin pm clear (không loại thiết bị khỏi job).");
                    return true;
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
                    // Như trên: KHÔNG để mất thiết bị vì thiếu binary chẩn đoán.
                    DeviceChangeLog.Write(Device?.Serial ?? "?",
                        $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][B1] sqlite3 push hỏng giữa chừng → BỎ QUA bước verify, tin pm clear (không loại thiết bị khỏi job).");
                    return true;
                }

                // Query accounts từ CẢ HAI database CE và DE.
                // ── FIX B1 ── Chuyển sang ĐƯỜNG PROCESS (`ADB.Shell(cmd, timeout)` = adb.exe +
                // WaitForExit + TryKillProcess, CÓ CHẶN TRÊN) thay vì đường socket (`Shell(...)` ở
                // :1352). Lý do giống hệt bug RestoreFacebook v17: đường socket chỉ có
                // ReceiveTimeout 30s KHÔNG có timeout tổng, và khi ném thì ADBClient.Shell RETRY 3
                // lần MỖI LẦN CHẠY LẠI TOÀN BỘ lệnh rồi gọi `Connect()` (reconnect storm). Với 20+
                // máy cùng vào bước này mỗi lần đổi acc, một lệnh sqlite đứng sẽ nhân lên thành bão.
                // sqlite3 in ra im lặng và nhanh nên 30s là rộng; lệnh đứng thật thì bị Kill.
                // ── FIX v22 ── Nối row mồi `__PROBE__` bằng UNION ALL: nếu lệnh sqlite3 CHẠY THẬT
                // thì output LUÔN chứa row này kể cả khi 0 row Facebook. Nhờ vậy phân biệt được
                // "sạch thật" với "lệnh hỏng trả stdout rỗng" — lớp bug v21 từng biến MỌI verify
                // thành "BẬC 1 OK" giả (xem comment SuShell/ReadAccountRows bên dưới).
                string query = "SELECT _id, name, type FROM accounts WHERE type LIKE 'com.facebook%' " +
                               "UNION ALL SELECT -1, '__PROBE__', '__PROBE__';";
                string cePath = "/data/system_ce/0/accounts_ce.db";
                string dePath = "/data/system_de/0/accounts_de.db";
                string output = ReadAccountRows(sqlite, cePath, query);
                string outputDe = ReadAccountRows(sqlite, dePath, query);

                // Gộp kết quả từ cả 2 DB, LOẠI row mồi __PROBE__ khỏi danh sách row thật (nó chỉ
                // chứng minh lệnh sqlite3 đã chạy; _id=-1 của nó sẽ làm hỏng parse ids).
                var allRows = new List<string>();
                foreach (string rawOutput in new[] { output, outputDe })
                {
                    foreach (string line in rawOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!line.Contains(ProbeMarker, StringComparison.Ordinal))
                            allRows.Add(line);
                    }
                }

                // ── FIX v22 ── GATE PROBE: phân biệt "sạch thật" với "verify mù". Row mồi __PROBE__
                // có trong output ⇒ lệnh sqlite3 CHẠY THẬT ⇒ allRows (đã lọc probe) là tập row
                // Facebook thật. Probe VẮNG ở cả hai DB ⇒ stdout rỗng do lệnh hỏng (bể quote ở tầng
                // Windows như v21, binary chết, DB khoá...) ⇒ KHÔNG được kết luận sạch và KHÔNG leo
                // thang mù: thoát an toàn, row thừa sẽ bị Login kế tiếp ghi đè (cùng triết lý không
                // loại thiết bị khỏi job).
                bool probeRan = output.Contains(ProbeMarker, StringComparison.Ordinal) ||
                                outputDe.Contains(ProbeMarker, StringComparison.Ordinal);
                if (!probeRan)
                {
                    DeviceChangeLog.Write(Device?.Serial ?? "?",
                        $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][v23] VERIFY MÙ: probe __PROBE__ vắng ở cả CE+DE (lệnh sqlite3 không chạy được) → không kết luận sạch, không leo thang; thoát an toàn.");
                    LogHelper.Log("[DeleteAccounts] Verify mù (probe vắng) — bỏ qua bước xoá account, không loại thiết bị.");
                    return true;
                }

                if (allRows.Count == 0)
                {
                    // ══════════════════════════════════════════════════════════════════════════
                    // ── FIX v22 (17-09): ĐÍNH CHÍNH GHI CHÚ B1 BÊN DƯỚI ──
                    // Ghi chú v19 khẳng định "`pm clear` một mình đã xoá sạch row Facebook" dựa trên
                    // median 2.59s của RunHistory. ĐO LIVE 17-09 BÁC BỎ: trên fleet Samsung Android 9
                    // này `pm clear` KHÔNG đụng /data/system_ce/0/accounts_ce.db (19/19 máy còn row,
                    // 1 máy tích 2 row); median 2.59s thực chất đo đường verify CHẾT (lệnh sqlite3 bể
                    // quote ở tầng Windows — xem SuShell) luôn trả stdout rỗng. Ý ĐỊNH của B1 (không
                    // stop/start framework mặc định) vẫn ĐÚNG và được giữ; v22 chỉ sửa thứ tự bậc và
                    // cách verify: sqlite DELETE đứng TRƯỚC mọi teardown, và mọi kết luận "sạch"
                    // phải qua gate probe __PROBE__.
                    // ══════════════════════════════════════════════════════════════════════════
                    // ── FIX B1 (v19): ĐÂY LÀ "BẬC 1" — VÀ NÓ ĐÃ LUÔN LUÔN MIỄN PHÍ ──
                    //
                    // Hai lệnh SELECT ở trên chạy SAU khi caller ClearFacebookData (:901-902) đã
                    // `pm clear` cả 8 package Facebook. Nên 0 row ở đây CHỨNG MINH `pm clear` một
                    // mình đã xoá sạch row Facebook khỏi accounts_ce.db/accounts_de.db — KHÔNG cần
                    // `stop`/`start` framework, KHÔNG cần sqlite DELETE. Return true ngay: không
                    // sleep, không teardown, mirror GIỮ NGUYÊN.
                    //
                    // ĐÂY LÀ ĐƯỜNG 98% — và số liệu production chứng minh nó đang chạy:
                    //  Logs/16-09-2026/RunHistory.txt, 339 lần chuyển acc trên 20 máy:
                    //  median bước1 = 2.59s, p90 = 2.81s, 332/339 mẫu trong bucket 2-3s.
                    //  Đường stop/start tốn TỐI THIỂU ~9.6s (2s StopFramework + 3s StartFramework
                    //  + 2s sleep verify + ~17 lời gọi shell). 2.59s ⇒ nhánh NÀY đang được đi.
                    //  Chỉ 7/339 (2%) rơi vào đường ~12s.
                    //
                    // KIẾN TRÚC CŨ (phần còn lại của hàm) coi `stop`/`start` là BẮT BUỘC cho mọi
                    // acc có row. VÌ SAO ĐÓ LÀ NGUỒN GỐC ĐEN MÀN: trong ~5-7s system_server chết,
                    // SurfaceFlinger/Zygote mất đối tác → mirror ngoài (xiaowei) chết theo → để lại
                    // VirtualDisplay MỒ CÔI (DẠNG 1: OMX h264 encoder crash → SF giữ weakref hỏng →
                    // createSurface kế tiếp SIGSEGV) và TaskRecord rò rỉ (DẠNG 2: cạn SurfaceControl).
                    // Chính vì vậy mới phải dựng MirrorSuppressor + RecoverFrameworkIfBlackScreen để
                    // chữa TRIỆU CHỨNG. B1 cắt nguồn sinh ra chúng.
                    //
                    // BẰNG CHỨNG THỨ HAI (độc lập) — TOOL ĐỐI THỦ AutoPhoneFarm.exe KHÔNG BAO GIỜ
                    // đụng DB account: accounts_ce=0, accounts_de=0, AccountManager=0, removeAccount=0,
                    // `cmd account`=0 hit. Nó đổi acc thuần bằng `pm clear` (@46262756) /
                    // `pm uninstall -k --user 0` (@46263956) + UI FB native ("Log into another
                    // account" @44846804 / "Switch account" @44859068). KHÔNG có `stop`/`start`.
                    // ⇒ existence proof: ROM class này KHÔNG cần teardown framework để đổi acc.
                    //
                    // CẤU TRÚC MỚI cho ~2% còn row — 2 bậc leo thang, CHỈ khi verify chứng minh:
                    //   BẬC 2: kill system_server (zygote respawn) → AccountManagerService đọc lại DB
                    //          từ đĩa, nhả cache RAM. KHÔNG `stop`: không có cửa sổ framework CHẾT
                    //          hoàn toàn (adbd/zygote/SurfaceFlinger vẫn sống) → mirror mất NGẮN hơn
                    //          hẳn stop+start.
                    //   BẬC 3: đường sqlite + stop/start CŨ giữ NGUYÊN VẸN làm lưới an toàn cuối
                    //          (DeleteAccountsWithFrameworkStop), chỉ khi Bậc 2 vẫn còn row.
                    // Mọi lời gọi shell đều đã chuyển sang ĐƯỜNG PROCESS (ADB.Shell có WaitForExit +
                    // TryKillProcess) thay vì socket — socket chỉ có ReceiveTimeout 30s KHÔNG có
                    // timeout tổng, và khi ném thì retry 3 lần CHẠY LẠI TOÀN BỘ lệnh rồi Connect()
                    // (reconnect storm) — đúng lớp bug RestoreFacebook v17.
                    // ══════════════════════════════════════════════════════════════════════════
                    DeviceChangeLog.Write(Device?.Serial ?? "?",
                        $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][B1] BẬC 1 OK: `pm clear` đã xoá sạch row Facebook (CE+DE = 0 row) → KHÔNG stop/start framework, mirror giữ nguyên.");
                    LogHelper.Log("[DeleteAccounts] Bậc 1: không còn Facebook account sau pm clear — không restart framework.");
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
                    // ── FIX B1 ── TRƯỚC ĐÂY `return false` → caller ClearFacebookData (:911) ném
                    // InvalidOperationException → MainService (:650-652) đặt Device.IsLive=false →
                    // MẤT THIẾT BỊ KHỎI JOB. Nhánh này bắn khi SELECT CÓ trả chữ nhưng không parse
                    // được `_id` (output lẫn dòng cảnh báo/header trên ROM lạ) — đó là lỗi ĐỊNH
                    // DẠNG OUTPUT, không phải "còn account cứng đầu", nên không đáng đổi lấy thiết bị.
                    DeviceChangeLog.Write(Device?.Serial ?? "?",
                        $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][B1] Không parse được _id từ {allRows.Count} dòng thô → bỏ qua sqlite DELETE (KHÔNG loại thiết bị khỏi job).");
                    LogHelper.Log("[DeleteAccounts] Không parse được account id nào từ output.");
                }

                // Clear WebView cache (pm cần system_server còn sống — chạy TRƯỚC mọi teardown).
                try
                {
                    Shell("pm clear com.android.webview");
                    Shell("pm clear com.google.android.webview");
                }
                catch { }

                // ══════════════════════════════════════════════════════════════════════════
                // ── FIX B1 (v19): KHÔNG CÒN BẮT BUỘC stop/start FRAMEWORK MỖI LẦN ĐỔI ACC ──
                // (Bằng chứng đầy đủ nằm ở khối comment "BẬC 1" ngay trên — không lặp lại ở đây.)
                //
                // Tới dòng này nghĩa là `pm clear` CHƯA xoá hết row Facebook. Kiến trúc cũ coi
                // `stop`/`start` là bắt buộc → ~9.6s system_server CHẾT mỗi lần, là nguồn sinh ra
                // cả hai dạng đen màn. Mới: 2 bậc leo thang, mỗi bậc VERIFY bằng SELECT thật nên
                // không bao giờ "tin mù"; mỗi lần leo thang ghi DeviceChangeLog (bền vững —
                // LogHelper.Log chỉ set Device.Status trên UI rồi bị bước kế ghi đè, LogHelper.cs:56).
                //
                //   BẬC 2: kill system_server (zygote respawn) → AccountManagerService đọc lại DB từ
                //          đĩa, nhả cache account cũ trong RAM, KHÔNG cần `stop`. Nhẹ hơn hẳn: không
                //          có cửa sổ framework CHẾT hoàn toàn (adbd/zygote/SurfaceFlinger vẫn sống) →
                //          mirror mất NGẮN hơn nhiều.
                //   BẬC 3: đường sqlite + stop/start CŨ giữ NGUYÊN VẸN làm lưới an toàn cuối
                //          (DeleteAccountsWithFrameworkStop), chỉ khi Bậc 2 vẫn còn row (~hiếm).
                // ══════════════════════════════════════════════════════════════════════════
                string serial = Device?.Serial ?? "?";

                // ── BẬC 1 (MIỄN PHÍ) ── soát lại CHÍNH output đã đọc ở trên, KHÔNG gọi thêm adb.
                // Query đã lọc `type LIKE 'com.facebook%'` nên MỌI row thật đều chứa "com.facebook".
                // Nếu không chuỗi nào chứa thì allRows chỉ là rác (dòng cảnh báo sqlite3 trên ROM
                // lạ) → không có gì để xoá, đừng đốt framework. Kiểm tra trong bộ nhớ = 0 round-trip.
                // (v22: gate probe ở trên đã bảo đảm output này đến từ lệnh sqlite3 CHẠY THẬT.)
                if (!output.Contains("com.facebook", StringComparison.OrdinalIgnoreCase) &&
                    !outputDe.Contains("com.facebook", StringComparison.OrdinalIgnoreCase))
                {
                    DeviceChangeLog.Write(serial,
                        $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][v23] BẬC 1 OK: {allRows.Count} dòng thô nhưng KHÔNG row Facebook thật → không teardown (mirror giữ nguyên).");
                    LogHelper.Log("[DeleteAccounts] Bậc 1: không còn Facebook account sau pm clear — không restart framework.");
                    return true;
                }

                DeviceChangeLog.Write(serial,
                    $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][v23] BẬC 1: pm clear KHÔNG xoá row AccountManager trên ROM này (ids={string.Join(",", ids)}) → BẬC 2 sqlite DELETE (không teardown).");

                // ── BẬC 2 (v22): sqlite DELETE trực tiếp, KHÔNG teardown. ──
                // Trên ROM này `pm clear` không đụng /data/system_ce nên "còn row" là trạng thái
                // THƯỜNG sau pm clear — xoá thẳng bằng sqlite là đường rẻ nhất (vài lời gọi shell,
                // framework sống, mirror giữ nguyên). v21 xếp Bậc 2 = kill system_server và Bậc 3 =
                // stop/start + DELETE: thứ tự NGƯỢC cho ROM này (gần như mọi lần đổi acc sẽ leo thang
                // lên teardown → đen màn quay lại).
                DeleteAccountRows(sqlite, cePath, dePath, ids);
                string stateAfterDelete = VerifyAccountState(sqlite, cePath, dePath, query);
                if (stateAfterDelete == "blind")
                {
                    DeviceChangeLog.Write(serial,
                        $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][v23] BẬC 2 verify MÙ (probe vắng) → dừng leo thang, không loại thiết bị (Login kế tiếp ghi đè).");
                    return true;
                }

                // ── FIX v23: ĐỦ HAI KÊNH MỚI ĐƯỢC TUYÊN BỐ THẮNG ──
                // Đĩa sạch chưa đủ: Settings→Accounts hiển thị cache RAM của AccountManagerService.
                // Nếu AMS còn giữ account thì acc cũ VẪN hiện trong Settings đúng như user báo,
                // và bậc leo thang kế tiếp (kill system_server → AMS nạp lại từ đĩa đã sạch) là
                // cách rẻ nhất để RAM đồng bộ với đĩa.
                bool amsStillHas = ReadAmsFacebookAccounts(out bool dumpsysOk);
                if (stateAfterDelete == "clean" && !(dumpsysOk && amsStillHas))
                {
                    DeviceChangeLog.Write(serial,
                        $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][v23] BẬC 2 OK: đĩa sạch (CE+DE 0 row Facebook)" +
                        (dumpsysOk ? " VÀ AMS (RAM) cũng sạch" : " — dumpsys không phản hồi nên chỉ xác nhận được đĩa") +
                        " → không teardown.");
                    LogHelper.SUCCESS("[DeleteAccounts] Bậc 2: sqlite DELETE đã xoá sạch account (không stop/start).");
                    return true;
                }

                DeviceChangeLog.Write(serial,
                    $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][v23] BẬC 2: đĩa={(stateAfterDelete == "clean" ? "SẠCH" : "CÒN ROW")}, " +
                    $"AMS(RAM)={(dumpsysOk ? (amsStillHas ? "CÒN account Facebook" : "sạch") : "không đọc được (dumpsys mù)")} " +
                    "→ BẬC 3 kill system_server để AccountManagerService nạp lại từ đĩa.");

                // ── BẬC 3: kill system_server để AccountManagerService nhả cache RAM, KHÔNG `stop`. ──
                // Cần khi DELETE không ăn trên đĩa (AMS giữ lock / ghi đè lại từ cache) HOẶC đĩa đã
                // sạch nhưng RAM của AMS vẫn còn (chính là thứ Settings→Accounts hiển thị).
                // SIGKILL không cho AMS chạy shutdown hook ⇒ không kịp flush cache RAM ngược lại đĩa,
                // nên DELETE ngay trước kill là trình tự AN TOÀN nhất có thể làm mà không stop framework.
                // KillSystemServer() tự Thread.Sleep(3000) chờ respawn; verify lại sau đó.
                KillSystemServer();
                InterruptibleSleep(1500); // cho AccountManagerService đọc lại DB xong hẳn
                string stateAfterKill = VerifyAccountState(sqlite, cePath, dePath, query);
                bool amsAfterKill = ReadAmsFacebookAccounts(out bool dumpsysOkAfterKill);
                if (stateAfterKill == "blind")
                {
                    DeviceChangeLog.Write(serial,
                        $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][v23] BẬC 3 verify MÙ (probe vắng) → dừng leo thang, không loại thiết bị.");
                    return true;
                }
                if (stateAfterKill == "clean" && !(dumpsysOkAfterKill && amsAfterKill))
                {
                    DeviceChangeLog.Write(serial,
                        $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][v23] BẬC 3 OK: kill system_server đủ — đĩa sạch" +
                        (dumpsysOkAfterKill ? " VÀ AMS(RAM) sạch" : " (dumpsys mù, chỉ xác nhận được đĩa)") +
                        " — KHÔNG dùng tới stop/start framework.");
                    LogHelper.SUCCESS("[DeleteAccounts] Bậc 3: kill system_server đã xoá sạch account (không stop/start).");
                    return true;
                }

                DeviceChangeLog.Write(serial,
                    $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][v23] BẬC 3 CHƯA đủ: đĩa={(stateAfterKill == "clean" ? "sạch" : "CÒN ROW")}, " +
                    $"AMS(RAM)={(dumpsysOkAfterKill ? (amsAfterKill ? "CÒN" : "sạch") : "mù")} " +
                    "→ BẬC 4 (stop framework RỒI mới DELETE: không process nào ghi đè lại được).");

                if (ids.Count == 0)
                {
                    // Không có _id nào để DELETE thì Bậc 4 (stop/start + sqlite DELETE) chẳng làm
                    // được gì ngoài đốt ~9.6s framework chết. Dừng ở đây, KHÔNG loại thiết bị: row
                    // thừa sẽ bị Login() kế tiếp ghi đè.
                    DeviceChangeLog.Write(serial,
                        $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][v23] BẬC 3 vẫn còn row nhưng KHÔNG có _id để xoá → bỏ qua Bậc 4, vẫn tiếp tục (Login kế tiếp ghi đè).");
                    return true;
                }

                // ── BẬC 4: lưới an toàn cuối — hành vi CŨ, giữ nguyên. ──
                return DeleteAccountsWithFrameworkStop(sqlite, cePath, dePath, query, ids);
            }
            catch (Exception ex)
            {
                // Vẫn không ném ra ngoài: một acc còn row account sẽ bị login đè lên, còn một
                // ngoại lệ ở đây sẽ loại THIẾT BỊ khỏi job (xem comment ở 2 exit ResolveSqlite3).
                DeviceChangeLog.Write(Device?.Serial ?? "?",
                    $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][B1] Lỗi (coi như đã dọn, không loại thiết bị): {ex.Message}");
                LogHelper.Log($"[DeleteAccounts] Lỗi: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// Chạy MỘT payload shell dưới quyền root qua ĐƯỜNG PROCESS (adb.exe + WaitForExit + Kill).
        /// ── FIX v23 ── payload đi qua BASE64: `su -c 'echo <b64>|base64 -d|sh'`.
        ///
        /// VÌ SAO CẦN: `ProcessHelper.RunProcessWithResult` gán Arguments THÔ cho CreateProcess, nên
        /// luật parse CRT của Windows áp lên TOÀN BỘ chuỗi lệnh trước khi adb.exe kịp thấy nó. CRT
        /// chỉ coi `"` là ký tự đặc biệt — nháy ĐƠN `'` KHÔNG phải. Vậy nên mọi cặp `"` bên trong
        /// payload đều bị nuốt/biến đổi bất kể ta bọc nó bằng `'` ở ngoài.
        ///
        /// v19-v21 dùng `\"`      → SQL cụt → `Error: incomplete input`.
        /// v22 đổi sang bọc nháy đơn `su -c 'sqlite3 /db "SQL"'` → CRT vẫn nuốt cặp `"` quanh SQL,
        ///      device sh thấy SQL TRẦN → token `-1,` của `UNION ALL SELECT -1,` bị sqlite3 nhận là
        ///      OPTION dòng lệnh → `sqlite3: Error: unknown option: -1,` → stdout RỖNG → verify mù →
        ///      gate probe thoát an toàn, DELETE KHÔNG BAO GIỜ CHẠY (đúng triệu chứng user báo 17-09).
        ///
        /// Base64 cắt đứt vấn đề tận gốc: alphabet chỉ gồm [A-Za-z0-9+/=], KHÔNG có `"`, KHÔNG có
        /// khoảng trắng, KHÔNG có ký tự shell nào ⇒ CRT và cả hai tầng sh đều coi nó là MỘT token
        /// nguyên văn. Payload được giải mã và chạy BỞI CHÍNH device (`base64 -d | sh`), nên quoting
        /// bên trong payload là chuyện của device sh, hoàn toàn cách ly khỏi Windows.
        /// Đã đo 17-09: 18/18 thiết bị live có `/system/bin/base64` và decode đúng.
        ///
        /// KỶ LUẬT KIỂM CHỨNG: mọi thử nghiệm dạng chuỗi lệnh PHẢI tái tạo đúng tầng CreateProcess
        /// (Popen với lpCommandLine thô). `adb --%` trong PowerShell và bash nháy đơn đều KHÔNG đi
        /// qua CRT theo cách của tool nên cho kết quả SAI — đó là lý do v22 "verify PASS" mà vẫn hỏng.
        /// </summary>
        private string SuShell(string payload, int timeout = 30)
        {
            string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
            return ADB.Shell($"su -c 'echo {b64}|base64 -d|sh'", timeout) ?? string.Empty;
        }

        /// <summary>Row mồi nối vào query verify bằng UNION ALL: có mặt ⇔ lệnh sqlite3 chạy thật.</summary>
        private const string ProbeMarker = "__PROBE__";

        /// <summary>
        /// Đọc row account Facebook từ MỘT database qua ĐƯỜNG PROCESS (adb.exe + WaitForExit + Kill).
        /// Trả chuỗi rỗng nếu database chưa tồn tại / chưa unlock / lệnh đứng — KHÔNG ném.
        /// (Thay cho `Shell(...)` socket ở code cũ: socket chỉ có ReceiveTimeout 30s KHÔNG có timeout
        /// tổng, và khi ném thì retry 3 lần CHẠY LẠI TOÀN BỘ lệnh rồi `Connect()` → reconnect storm.)
        /// </summary>
        private string ReadAccountRows(string sqlite, string dbPath, string query)
        {
            try
            {
                // `2>/dev/null` để accounts_ce.db chưa unlock (trước khi user mở khoá) không in
                // lỗi "unable to open database file" — lỗi đó từng bị hiểu nhầm thành "còn account".
                // Caller phân biệt "rỗng vì sạch" với "rỗng vì lệnh hỏng" bằng row mồi __PROBE__.
                return SuShell($"{sqlite} {dbPath} \"{query}\" 2>/dev/null");
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[DeleteAccounts] Đọc {dbPath} lỗi: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Verify trạng thái row Facebook account trên CẢ HAI database CE+DE, CÓ gate probe:
        ///   "clean" — probe có mặt và 0 row Facebook ⇒ kết luận sạch ĐÁNG TIN (TRÊN ĐĨA);
        ///   "rows"  — probe có mặt và còn row Facebook;
        ///   "blind" — probe VẮNG ⇒ lệnh sqlite3 không chạy được ⇒ KHÔNG được kết luận sạch và
        ///             KHÔNG được leo thang teardown (bài học DẠNG 2: teardown oan sinh đen màn).
        ///
        /// LƯU Ý v23: hàm này chỉ nói về ĐĨA. Settings→Accounts hiển thị danh sách AccountManagerService
        /// giữ TRONG RAM, nên "clean" ở đây CHƯA đủ — phải kèm ReadAmsFacebookAccounts.
        /// </summary>
        private string VerifyAccountState(string sqlite, string cePath, string dePath, string query)
        {
            string ce = ReadAccountRows(sqlite, cePath, query);
            string de = ReadAccountRows(sqlite, dePath, query);
            if (!ce.Contains(ProbeMarker, StringComparison.Ordinal) &&
                !de.Contains(ProbeMarker, StringComparison.Ordinal))
                return "blind";
            bool rows = ce.Contains("com.facebook", StringComparison.OrdinalIgnoreCase) ||
                        de.Contains("com.facebook", StringComparison.OrdinalIgnoreCase);
            return rows ? "rows" : "clean";
        }

        /// <summary>
        /// ── FIX v23 (kênh verify THỨ HAI, đúng như phương án đã duyệt) ──
        /// Đọc danh sách account TỪ RAM của AccountManagerService bằng `dumpsys account`.
        ///
        /// VÌ SAO BẮT BUỘC: Settings → Accounts KHÔNG đọc accounts_ce.db. Nó gọi
        /// AccountManager.getAccountsByType(), tức hỏi AccountManagerService — dịch vụ này cache toàn bộ
        /// account trong RAM từ lúc khởi động. Vậy nên một row bị DELETE thẳng trên đĩa VẪN hiện trong
        /// Settings cho tới khi AMS nạp lại. Đây là lý do user vẫn thấy acc cũ dù lệnh DELETE đã chạy.
        ///
        /// Đo 17-09 trên device live, hai kênh lệch nhau rõ:
        ///   dumpsys account  → "Accounts: 1 / Account {name=Facebook, type=com.facebook.auth.login}"
        ///   sqlite3 CE       → "3|Facebook|com.facebook.auth.login"
        ///
        /// Trả về true nếu AMS còn giữ account Facebook. KHÔNG cần root và KHÔNG qua tầng quote
        /// phức tạp (dumpsys không cần su, không có nháy trong đối số) → miễn nhiễm với cả lớp bug CRT.
        /// Trả về false nếu lệnh không chạy được (mù) — caller phải tự phân biệt mù/không qua
        /// dumpsysResponded.
        /// </summary>
        private bool ReadAmsFacebookAccounts(out bool dumpsysResponded)
        {
            dumpsysResponded = false;
            try
            {
                string dump = ADB.Shell("dumpsys account", 20) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(dump)) return false;
                dumpsysResponded = true;
                int count = 0;
                foreach (string line in dump.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!line.Contains("Account {", StringComparison.Ordinal)) continue;
                    if (line.Contains("com.facebook", StringComparison.OrdinalIgnoreCase)) count++;
                }
                return count > 0;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[DeleteAccounts] dumpsys account lỗi: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ── BẬC 2 ── Xoá row Facebook khỏi accounts_de.db + cascade trong accounts_ce.db
        /// bằng sqlite DELETE, KHÔNG teardown framework. Trên ROM này `pm clear` KHÔNG đụng
        /// /data/system_ce nên "còn row" là trạng thái THƯỜNG sau pm clear — xoá thẳng bằng sqlite
        /// là cách rẻ nhất.
        ///
        /// ── FIX v23: SỬA THEO SCHEMA THẬT (đo 17-09 trên SM-J730G Android 9, `.schema`) ──
        /// v22 DELETE `grants` trên **CE**: bảng đó KHÔNG tồn tại ở CE → `Error: no such table: grants`
        /// mỗi lần, và grants THẬT (nằm ở DE) không bao giờ được dọn. Hai bảng DE khác cũng bị bỏ sót:
        ///   CE: accounts, authtokens, extras                      (KHÔNG có grants)
        ///   DE: accounts, grants, shared_accounts, visibility, meta, debug_table
        /// `shared_accounts`/`visibility` keyed theo (name,type) chứ không theo _id nên xoá theo cặp
        /// name+type của chính row đang dọn, không theo id.
        ///
        /// LỆNH KHÔNG CÓ `2>/dev/null`: lỗi sqlite3 ("no such table", "database is locked", "attempt to
        /// write a readonly database") phải nổi lên log. Chính việc nuốt stderr là thứ đã che lỗi quote
        /// suốt v19→v22: stdout rỗng bị đọc thành "sạch".
        /// `PRAGMA busy_timeout=5000` nhường lock nếu AccountManagerService đang giữ DB.
        /// KHÔNG rm file WAL/SHM ở đây: AMS còn sống mà mất -shm = hỏng database.
        /// </summary>
        private void DeleteAccountRows(string sqlite, string cePath, string dePath, List<int> ids)
        {
            foreach (int id in ids)
            {
                try
                {
                    // Đọc name/type TRƯỚC khi xoá để dọn các bảng keyed theo (name,type).
                    string identity = SuShell($"{sqlite} {cePath} \"SELECT name, type FROM accounts WHERE _id = {id};\"");
                    string acctName = string.Empty, acctType = string.Empty;
                    string[] identityParts = identity.Split('|');
                    if (identityParts.Length >= 2)
                    {
                        acctName = identityParts[0].Trim();
                        acctType = identityParts[1].Trim();
                    }

                    // ── DE: bảng cha (AccountManager đọc DE để dựng danh sách account) ──
                    RunDelete(sqlite, dePath, $"DELETE FROM accounts WHERE _id = {id};", "DE.accounts");
                    RunDelete(sqlite, dePath, $"DELETE FROM grants WHERE accounts_id = {id};", "DE.grants");
                    RunDelete(sqlite, dePath, $"DELETE FROM visibility WHERE accounts_id = {id};", "DE.visibility");

                    // ── CE: cascade + row account ──
                    RunDelete(sqlite, cePath, $"DELETE FROM authtokens WHERE accounts_id = {id};", "CE.authtokens");
                    RunDelete(sqlite, cePath, $"DELETE FROM extras WHERE accounts_id = {id};", "CE.extras");
                    RunDelete(sqlite, cePath, $"DELETE FROM accounts WHERE _id = {id};", "CE.accounts");

                    // shared_accounts KHÔNG có accounts_id — keyed theo (name,type) UNIQUE.
                    if (!string.IsNullOrEmpty(acctName) && !string.IsNullOrEmpty(acctType))
                    {
                        string nm = acctName.Replace("'", "''"), tp = acctType.Replace("'", "''");
                        RunDelete(sqlite, dePath, $"DELETE FROM shared_accounts WHERE name = '{nm}' AND type = '{tp}';", "DE.shared_accounts");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"[DeleteAccounts] Lỗi xóa account ID={id}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Chạy MỘT câu DELETE qua SuShell và NÊU TÊN bảng trong log khi sqlite3 báo lỗi.
        /// Trả về true nếu sqlite3 không kêu gì (DELETE không match row nào vẫn là thành công).
        /// KHÔNG nuốt lỗi: đây là thay đổi cốt lõi so với v19-v22, nơi `2>/dev/null` biến mọi
        /// lệnh hỏng thành im lặng và khiến "stdout rỗng" bị đọc thành "DB đã sạch".
        /// </summary>
        private bool RunDelete(string sqlite, string dbPath, string statement, string label)
        {
            try
            {
                string result = SuShell($"{sqlite} {dbPath} 'PRAGMA busy_timeout=5000; {statement}' 2>&1");
                if (!string.IsNullOrWhiteSpace(result) &&
                    result.Contains("Error", StringComparison.OrdinalIgnoreCase))
                {
                    DeviceChangeLog.Write(Device?.Serial ?? "?",
                        $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][v23] DELETE {label} BÁO LỖI: {result.Replace("\r", " ").Replace("\n", " ").Trim()}");
                    LogHelper.Log($"[DeleteAccounts] DELETE {label} lỗi: {result.Trim()}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[DeleteAccounts] DELETE {label} ném lỗi: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Parse `_id` từ output dạng `id|name|type` của sqlite3. Bỏ qua dòng không parse được.
        /// </summary>
        private static List<int> ParseAccountIds(string output)
        {
            var ids = new List<int>();
            if (string.IsNullOrWhiteSpace(output)) return ids;
            foreach (string row in output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = row.Split('|');
                if (parts.Length >= 1 && int.TryParse(parts[0].Trim(), out int id))
                    ids.Add(id);
            }
            return ids;
        }

        /// <summary>
        /// ── BẬC 4 (lưới an toàn cuối) ── HÀNH VI CŨ CỦA DeleteAccounts, giữ NGUYÊN Ý:
        /// stop framework → sqlite DELETE từng row → checkpoint/rm WAL → start framework → verify →
        /// retry lần 2 nếu cần. Chỉ được gọi khi Bậc 2 (sqlite DELETE) VÀ Bậc 3 (kill system_server)
        /// đều đã verify là còn row — tức gần như không bao giờ. Khác v21: mọi lệnh sqlite ở đây đi
        /// qua SuShell (v21 bể quote ở tầng Windows nên cả khối DELETE này chưa từng chạy lần nào)
        /// và verify đi qua gate probe __PROBE__ thay vì tin stdout rỗng.
        /// </summary>
        private bool DeleteAccountsWithFrameworkStop(string sqlite, string cePath, string dePath, string query, List<int> ids)
        {
            try
            {
                LogHelper.Log("[DeleteAccounts] Bậc 4: stop/start framework sẽ làm mất view TẠM THỜI ~5s; mirror-guard đang chạy sẽ dọn mồ côi VirtualDisplay, mirror tự nối lại.");
                bool stopped = StopFramework();

                DeleteAccountRows(sqlite, cePath, dePath, ids);
                int deletedCount = ids.Count;

                if (deletedCount > 0)
                {
                    // Reset sequence counter.
                    // ── FIX v23 ── dạng cũ `''accounts''` SAI ở tầng sh: trong POSIX sh hai nháy đơn
                    // liền nhau `'a''b'` là NỐI chuỗi, nên `''accounts''` sụp thành `accounts` TRẦN →
                    // SQLite hiểu là TÊN CỘT → `Error: no such column: accounts` (đo 17-09). Lệnh reset
                    // vì vậy chưa từng chạy, và AUTOINCREMENT của CE.accounts giữ seq cũ.
                    // Đúng: nháy KÉP cho sh, nháy ĐƠN cho chuỗi SQL.
                    SuShell($"{sqlite} {cePath} \"DELETE FROM sqlite_sequence WHERE name = 'accounts';\"");
                    SuShell($"{sqlite} {dePath} \"DELETE FROM sqlite_sequence WHERE name = 'accounts';\"");

                    // Checkpoint WAL (TRUNCATE)
                    foreach (string db in new[] { cePath, dePath })
                        SuShell($"{sqlite} {db} 'PRAGMA wal_checkpoint(TRUNCATE);'");

                    // Xóa trực tiếp file WAL/SHM/journal để chặn mọi khả năng phục hồi.
                    // AN TOÀN ở đây vì framework ĐÃ stop (không process nào giữ -shm); KHÔNG làm
                    // việc này ở Bậc 2/3 khi AccountManagerService còn sống.
                    foreach (string db in new[] { cePath, dePath })
                    {
                        try { SuShell($"rm -f {db}-wal {db}-shm {db}-journal"); } catch { }
                    }
                }

                // Start lại framework
                if (stopped)
                    StartFramework();
                else
                    KillSystemServer();

                // VERIFY: kiểm tra accounts đã thực sự bị xóa sau khi framework restart
                InterruptibleSleep(2000);
                string state = VerifyAccountState(sqlite, cePath, dePath, query);
                if (state == "rows")
                {
                    DeviceChangeLog.Write(Device?.Serial ?? "?",
                        $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][v23] BẬC 4: accounts VẪN CÒN sau lần xóa đầu — thử xóa lại lần 2.");
                    LogHelper.Log($"[DeleteAccounts] CẢNH BÁO: Accounts vẫn còn sau lần xóa đầu tiên! Thử xóa lại lần 2...");
                    // Retry lần 2 với framework đã chạy (không cần stop nữa)
                    string verifyOutput = ReadAccountRows(sqlite, cePath, query);
                    List<int> retryIds = ParseAccountIds(verifyOutput);
                    if (retryIds.Count > 0)
                    {
                        DeleteAccountRows(sqlite, cePath, dePath, retryIds);
                        foreach (int retryId in retryIds)
                            LogHelper.Log($"[DeleteAccounts] Retry xóa account ID={retryId}");
                    }
                    // Xóa WAL lần nữa
                    foreach (string db in new[] { cePath, dePath })
                    {
                        try { SuShell($"{sqlite} {db} 'PRAGMA wal_checkpoint(TRUNCATE);'"); } catch { }
                        try { SuShell($"rm -f {db}-wal {db}-shm {db}-journal"); } catch { }
                    }

                    state = VerifyAccountState(sqlite, cePath, dePath, query);
                }

                if (state == "rows")
                {
                    // ── FIX B1 ── TRẢ TRUE thay vì false. Code cũ trả false → ClearFacebookData
                    // (:911) ném InvalidOperationException → MainService (:650-652) đặt
                    // `Device.IsLive = false` → MẤT THIẾT BỊ KHỎI JOB chỉ vì một row account cứng đầu
                    // mà đằng nào Login() kế tiếp cũng ghi đè. Đã leo thang đủ 4 bậc + retry; tiếp
                    // tục loại thiết bị là phạt nặng hơn lỗi. Ghi log bền vững để truy vết.
                    DeviceChangeLog.Write(Device?.Serial ?? "?",
                        $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][v23] BẬC 4: vẫn còn row sau retry → VẪN TIẾP TỤC (không loại thiết bị); Login kế tiếp sẽ ghi đè account.");
                    LogHelper.Log("[DeleteAccounts] Vẫn còn Facebook account sau khi retry — vẫn tiếp tục, không loại thiết bị.");
                    return true;
                }

                if (state == "blind")
                {
                    DeviceChangeLog.Write(Device?.Serial ?? "?",
                        $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][v23] BẬC 4 verify MÙ (probe vắng) → coi như đã dọn, không loại thiết bị.");
                    return true;
                }

                DeviceChangeLog.Write(Device?.Serial ?? "?",
                    $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][v23] BẬC 4 OK: đã xóa {deletedCount} account qua stop/start framework.");
                // Bằng chứng cuối cho user: đọc RAM AMS sau khi framework đã start lại (AMS vừa nạp
                // từ đĩa đã sạch). Chỉ để LOG — B4 luôn return true, không đổi quyết định ở đây.
                bool amsFinal = ReadAmsFacebookAccounts(out bool dumpsysOkFinal);
                DeviceChangeLog.Write(Device?.Serial ?? "?",
                    $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][v23] BẬC 4 chốt: AMS(RAM) — thứ Settings hiển thị — " +
                    (dumpsysOkFinal ? (amsFinal ? "VẪN CÒN Facebook account (cần kiểm tra thêm)" : "SẠCH (Settings sẽ không còn acc cũ)")
                                     : "dumpsys mù, không xác nhận được RAM"));
                if (deletedCount > 0)
                    LogHelper.SUCCESS($"[DeleteAccounts] Đã xóa {deletedCount} Facebook accounts.");
                else
                    LogHelper.Log("[DeleteAccounts] Không có Facebook accounts nào được xóa.");
                return true;
            }
            catch (Exception ex)
            {
                DeviceChangeLog.Write(Device?.Serial ?? "?",
                    $"[{DeviceChangeLog.BuildTag}] [DeleteAccounts][v23] BẬC 4 lỗi (coi như đã dọn, không loại thiết bị): {ex.Message}");
                LogHelper.Log($"[DeleteAccounts] Bậc 4 lỗi: {ex.Message}");
                return true;
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
        // ─── Hook TOÀN CỤC chặn popup "trôi nổi" (thẻ Meta consent) ──────────────────────
        // VẤN ĐỀ (xác minh LIVE 2026-09-10 trên 520058f34d7c947b): thẻ "Use free of charge
        // with ads" hiện BẤT CHỢT ở MỌI thời điểm của job, nhưng 65/66 vòng lặp FindElement
        // của FacebookFarming KHÔNG chứa XpathType.MetaAdsConsent trong danh sách xpath. Vòng
        // lặp đó không bao giờ khớp -> FindElement poll tight-loop (không delay giữa 2 lần
        // dump) đến hết timeout -> trả "" -> lặp lại vô hạn. Bằng chứng logcat: 159 lần
        // "dumpWindowHierarchy" trong 30 giây mà KHÔNG có một lệnh click/tap/am start nào,
        // mResumedActivity vẫn là ConsentFlowHostActivity sau 20 giây. KẸT IM LẶNG.
        //
        // Vá từng vòng lặp là bất khả thi (65 chỗ, và job mới sẽ lại quên). GetXMLSource là
        // ĐIỂM THẮT DUY NHẤT mà mọi vòng lặp đều đi qua -> đặt hook ở đây vá được tất cả.
        //
        // AutoAndroid KHÔNG reference Sunny.Subd.Core (chiều phụ thuộc ngược lại), nên hook
        // khai báo ở đây và Sunny.Subd.Core đăng ký lúc khởi động (Program.Main):
        //   ADBClient.GlobalPopupDetector    = FacebookHander.IsMetaConsentPopup;
        //   ADBClient.GlobalPopupInterceptor = FacebookHander.TryHandleMetaConsentPopup;
        //
        // HỢP ĐỒNG (cả hai đều ĐỒNG BỘ, vì GetXMLSource là hàm sync):
        //   Detector    (client, xml) -> true nếu màn hình CÓ popup. PHẢI RẺ và KHÔNG tác động
        //                               (chỉ đọc chuỗi) — nó chạy trên MỌI lần dump.
        //   Interceptor (client, xml) -> true nếu ĐÃ nhận diện và XỬ LÝ popup (tap/swipe).
        //                                Khi true, GetXMLSource dump lại để caller thấy màn mới.
        public static Func<ADBClient, string, bool>? GlobalPopupDetector;
        public static Func<ADBClient, string, bool>? GlobalPopupInterceptor;

        // Chống ĐỆ QUY: chính Interceptor cũng gọi GetXMLSource/FindElement.
        [ThreadStatic] private static bool _popupInterceptorBusy;
        // Van an toàn: Interceptor dọn không nổi (biến thể lạ) thì nghỉ, đừng chiếm trọn job.
        private DateTime _popupLastAttemptUtc = DateTime.MinValue;
        private int _popupFailStreak;
        private const int PopupMaxFailStreak = 3;
        private const int PopupCooldownSeconds = 90;

        /// <summary>
        /// Chạy hook popup toàn cục trên XML vừa dump, trả về XML caller nên dùng
        /// (XML MỚI nếu popup vừa được dọn, ngược lại giữ nguyên XML cũ).
        /// KHÔNG BAO GIỜ ném (trừ OperationCanceledException khi user bấm DỪNG).
        /// </summary>
        private string RunGlobalPopupInterceptor(string xml)
        {
            var detector = GlobalPopupDetector;
            var interceptor = GlobalPopupInterceptor;
            if (detector == null || interceptor == null) return xml;
            if (_popupInterceptorBusy) return xml;
            if (_popupFailStreak >= PopupMaxFailStreak
                && (DateTime.UtcNow - _popupLastAttemptUtc).TotalSeconds < PopupCooldownSeconds)
                return xml;

            try
            {
                // Detector rẻ (string search) nên KHÔNG trả về ngay ở đây là một sai lầm —
                // phải kiểm tra TRƯỚC để 99.9% lần dump không phải vào Interceptor.
                if (!detector(this, xml))
                {
                    _popupFailStreak = 0;
                    return xml;
                }

                _popupInterceptorBusy = true;
                _popupLastAttemptUtc = DateTime.UtcNow;
                LogHelper.Log("[PopupInterceptor] phát hiện popup ngoài luồng job -> dọn toàn cục");

                if (!interceptor(this, xml)) return xml;

                // Popup đã được xử lý -> dump LẠI (guard ở trên chặn đệ quy) lấy màn hình mới.
                ThrowIfStopped();
                string fresh = _currentAutomationType == AutomationType.Appium
                    ? GetXmlSourceByAppium()
                    : GetXmlSourceByATX();
                if (!IsValidHierarchyXml(fresh)) return xml;

                // Vẫn còn popup? -> một lần thất bại, để van an toàn đếm và hạ nhiệt.
                _popupFailStreak = detector(this, fresh) ? _popupFailStreak + 1 : 0;
                return fresh;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[PopupInterceptor] lỗi: {ex.Message}");
                return xml;
            }
            finally
            {
                _popupInterceptorBusy = false;
            }
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
                        return RunGlobalPopupInterceptor(xml);
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
            // Quyền danh bạ + lưu trữ legacy (Android 9-12).
            this.Shell(" pm grant " + package + " android.permission.READ_CONTACTS");
            this.Shell(" pm grant " + package + " android.permission.READ_EXTERNAL_STORAGE");
            this.Shell(" pm grant " + package + " android.permission.WRITE_EXTERNAL_STORAGE");
            this.Shell(" pm grant " + package + " android.permission.CAMERA");
            this.Shell(" pm grant " + package + " android.permission.RECORD_AUDIO");
            this.Shell(" pm grant " + package + " android.permission.CALL_PHONE");
            this.Shell("pm grant " + package + " android.permission.MANAGE_EXTERNAL_STORAGE");

            // Android 13+ (SDK 33): quyền "Storage" trên UI ánh xạ sang READ_MEDIA_*.
            // Lệnh grant cho permission không tồn tại sẽ báo lỗi nhưng KHÔNG gây hại -> bỏ qua.
            this.Shell("pm grant " + package + " android.permission.READ_MEDIA_IMAGES");
            this.Shell("pm grant " + package + " android.permission.READ_MEDIA_VIDEO");
            this.Shell("pm grant " + package + " android.permission.READ_MEDIA_AUDIO");
            this.Shell("pm grant " + package + " android.permission.READ_MEDIA_VISUAL_USER_SELECTED");
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
