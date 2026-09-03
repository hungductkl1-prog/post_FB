using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sunny.Subdy.Data.Models;
using AutoAndroid.Monitoring;

namespace AutoAndroid
{
    public class ATXService
    {
        /// <summary>
        /// Thời gian tối đa (đơn vị: mili giây) để chờ một node UI xuất hiện.
        /// Nếu quá thời gian này mà node chưa xuất hiện → thao tác thất bại.
        /// Mặc định: 2000ms (2 giây).
        /// </summary>
        public int UINodeMaxWaitTime { get; set; } = 2000;

        /// <summary>
        /// Khoảng thời gian nghỉ (đơn vị: mili giây) giữa các lần kiểm tra sự tồn tại của node UI trong quá trình chờ.
        /// Mặc định: 60ms.
        /// </summary>
        public int UINodeClickExistDelay { get; set; } = 60;

        /// <summary>
        /// Độ trễ (đơn vị: mili giây) sau khi click vào node UI, để đợi hệ thống xử lý phản hồi.
        /// Mặc định: 100ms.
        /// </summary>
        public int UINodeClickDelay { get; set; } = 100;
        private readonly string _serial;
        private int _port = 7912;
        public string _url = null;
        private DeviceModel _device;
        private ADBClient _client;
        InitHelper _initer = null;
        private bool IsAppiumMode => string.Equals(_client.CurrentAutomationType, "appium", StringComparison.OrdinalIgnoreCase);
        public ATXService(ADBClient client)
        {
            _client = client;
            _device = _client.Device;
            _serial = _device.Serial;
            _port = _device.Port;
            _url = $"http://127.0.0.1:{_port}";
        }
        public async Task<bool> SetupATX()
        {
            if (IsAppiumMode)
            {
                _client.LogHelper.Log("Device đang ở mode appium, bỏ qua SetupATX.");
                return false;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            _initer = new InitHelper(_client);
            int i = 1;
            try
            {
                i++;
                // Idempotent: only Install (has built-in outdated checks).
                // Do NOT Uninstall a healthy ATX install on every connect.
                _initer.Install();
            }
            catch (Exception ex)
            {
                _client.LogHelper.Log(ex.Message);
            }
            _client.RunTime($"Connect: UIAUTOMATOR", () => Connect());
            _client.RunTime($"Connect: UIAUTOMATOR ", () => RunUiautomator());
            if (RunUiautomator())
            {
                return true;
            }

            // Fallback: only when Install + Connect still cannot bring up
            // UIAutomator do a full Reinstall (Uninstall + Install) + retry.
            try
            {
                _initer.Reinstall();
            }
            catch (Exception ex2)
            {
                _client.LogHelper.Log(ex2.Message);
            }
            _client.RunTime($"Connect: UIAUTOMATOR (retry)", () => Connect());
            return RunUiautomator();
        }
        public bool Connect()
        {
            if (IsAppiumMode)
            {
                _client.LogHelper.Log("Device đang ở mode appium, bỏ qua ATX Connect.");
                return false;
            }

            if (_client != null)
            {
                int port = 0;
                try
                {
                    using var client = new ADBSocket(_serial);
                    port = client.ForwardPort(7912);
                }
                catch (Exception ex)
                {
                    _client.LogHelper.Log($"ForwardPort exception cho {_serial}: {ex.Message}");
                    return false;
                }
                if (port <= 0)
                {
                    _client.LogHelper.Log($"ForwardPort thất bại cho {_serial}");
                    return false;
                }
                _client.Device.Port = port;
            }

            _port = _client.Device.Port;
            _url = $"http://127.0.0.1:{_port}";
            return RunUiautomator();
        }
        public string AtxAgentUrl
        {
            get
            {
                if (IsAppiumMode)
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(_url))
                {
                    if (!Connect())
                    {
                        _client.LogHelper.Log($"Connect Error");
                        return null;
                    }
                }
                return _url;
            }
        }
        public string AtxAgentWs
        {
            get
            {
                if (IsAppiumMode)
                {
                    return null;
                }

                if (_port == -1)
                {
                    if (!Connect())
                    {
                        _client.LogHelper.Log($"Connect Error");
                        return null;
                    }
                }
                return $"ws://127.0.0.1:{_port}";
            }
        }
        private string GrantAppPermissions()
        {
            var argv = new string[] {
                "pm",
                "grant",
                "com.github.uiautomator",
                "android.permission.SYSTEM_ALERT_WINDOW",
                "android.permission.ACCESS_FINE_LOCATION",
                "android.permission.READ_PHONE_STATE"
            };
            return _client.Shell(argv);
        }
        public bool RunUiautomator(int timeout = 20)
        {
            if (IsAppiumMode)
            {
                _client.LogHelper.Log("Device đang ở mode appium, bỏ qua RunUiautomator.");
                return false;
            }

            bool service = UIService.Running();
            if (service)
            {
                _client.LogHelper.Log($"[RunUiautomator] -start{_client.Device.Port}");
                return true;
            }
            GrantAppPermissions();
            var argv = new string[] {
                "am",
                "start",
                "-a",
                "android.intent.action.MAIN",
                "-c",
                "android.intent.category.LAUNCHER",
                "-n",
                "com.github.uiautomator/.ToastActivity",
            };
            _client.Shell(argv);
            service = UIService.Start();

            service = UIService.Running();
            if (service)
            {
                _client.LogHelper.Log($"[RunUiautomator] -start{_client.Device.Port}");
                return true;
            }
            while (timeout-- > 0)
            {
                if (!UIService.Running())
                {
                    continue;
                }
                if (IsAlive())
                {
                    _client.Shell("am", "start", "-n", "com.github.uiautomator/.ToastActivity", "-e", "showFloatWindow", true.ToString().ToLower());
                    return true;
                }
            }
            UIService.Stop();
            InitHelper initer = new InitHelper(_client);
            initer.Install();
            UIService.Start();
            return UIService.Running();
        }
        public bool IsAlive()
        {
            int size = 10;
            while (size-- > 0)
            {
                var device = DeviceInfo();
                if (device == null)
                {
                    continue;
                }
                return true;
            }
            return false;
        }
        public UADeviceInfo DeviceInfo()
        {
            var json = JsonRpc("deviceInfo");
            if (json == null)
            {
                return null;
            }
            if (json.Error != null)
            {
                _client.LogHelper.Log($"DeviceInfo: {json.Error.ToJsonString()}");
                return null;
            }
            JsonObject data = json.Data!.AsObject();

            // Parse the JsonObject manually
            var deviceInfo = new UADeviceInfo
            {
                CurrentPackageName = data["currentPackageName"]?.GetValue<string>(),
                DisplayRotation = data["displayRotation"]?.GetValue<int>() ?? 0,
                DisplayHeight = data["displayHeight"]?.GetValue<int>() ?? 0,
                DisplayWidth = data["displayWidth"]?.GetValue<int>() ?? 0,
                DisplaySizeDpX = data["displaySizeDpX"]?.GetValue<int>() ?? 0,
                DisplaySizeDpY = data["displaySizeDpY"]?.GetValue<int>() ?? 0,
                ProductName = data["productName"]?.GetValue<string>(),
                ScreenOn = data["screenOn"]?.GetValue<bool>() ?? false,
                SdkInt = data["sdkInt"]?.GetValue<int>() ?? 0,
                NaturalOrientation = data["naturalOrientation"]?.GetValue<bool>() ?? false,
            };

            return deviceInfo;
        }
        public JsonRpcResponse JsonRpc(string method, params object[] argv)
        {
            if (IsAppiumMode)
            {
                return null;
            }

            string url = $"{_url}/jsonrpc/0";
            JsonArray array = new JsonArray();
            foreach (var obj in argv)
            {
                if (obj is By)
                {
                    array.Add((obj as By).ToJson());
                    continue;
                }
                array.Add(JsonValue.Create(obj));
            }
            string id = Guid.NewGuid().ToString().Replace("-", "");
            JsonObject json = new JsonObject {
                { "jsonrpc", "2.0" },
                { "id", id },
                { "method", method },
                { "params", array }
            };

            var devMetrics = DeviceMetricsRegistry.GetOrCreate(_serial);
            MetricsCollector.Increment("atx.jsonrpc.total");
            using var _ = MetricsCollector.Measure("atx.jsonrpc.execution.ms");
            try
            {
                using (var socket = SocketHelper.Create(_url))
                {
                    var result = socket.HttpPost("/jsonrpc/0", json);

                    if (result == null || result.Code != 200)
                    {
                        MetricsCollector.Increment("atx.jsonrpc.failed");
                        devMetrics.JsonRpcFailCount++;
                        return null;
                    }
                    if (string.IsNullOrWhiteSpace(result.Content))
                    {
                        MetricsCollector.Increment("atx.jsonrpc.failed");
                        devMetrics.JsonRpcFailCount++;
                        return null;
                    }

                    // Parse JSON response manually
                    var jsonResponse = JsonNode.Parse(result.Content)!.AsObject();
                    JsonRpcResponse response = new JsonRpcResponse();

                    response.Version = jsonResponse["jsonrpc"]?.GetValue<string>();
                    response.Id = jsonResponse["id"]?.GetValue<string>();
                    response.Error = jsonResponse["error"]?.AsObject();
                    response.Data = jsonResponse["result"];

                    return response;
                }
            }
            catch (Exception ex)
            {
                MetricsCollector.Increment("atx.jsonrpc.failed");
                devMetrics.JsonRpcFailCount++;
                if (ex.Message.Contains("Failed to connect to "))
                {
                    devMetrics.AtxReconnectCount++;
                    MetricsCollector.Increment("atx.reconnect.count");
                    Connect();
                    return JsonRpc(method, argv);
                }
                return null;
            }
        }
        private UIAutomatorService UIService
        {
            get
            {
                return new UIAutomatorService(this);
            }
        }
        public bool ATXSwipe(int startX, int startY, int endX, int endY, int durationMs = 500)
        {
            if (IsAppiumMode)
            {
                return AppiumSessionService.Swipe(_client, startX, startY, endX, endY, durationMs);
            }

            try
            {
                // Tạo JSON array actions
                var actions = new object[]
                {
            new { action = "press", options = new { x = startX, y = startY } },
            new { action = "wait", options = new { ms = durationMs } },
            new { action = "moveTo", options = new { x = endX, y = endY } },
            new { action = "release", options = new { } }
                };

                // Gọi JsonRPC
                JsonRpcResponse resp = JsonRpc("touch/perform", new { actions });

                // Kiểm tra kết quả
                if (resp != null && resp.Error == null)
                {
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        public AtxDeviceInfo Info()
        {
            if (IsAppiumMode)
            {
                int sdk = 0;
                _ = int.TryParse(_client.Shell("getprop", "ro.build.version.sdk").Trim(), out sdk);
                Point size = _client.GetScreenResolutionForInput();
                return new AtxDeviceInfo
                {
                    udid = _serial,
                    Version = "appium",
                    Serial = _serial,
                    Brand = _client.Shell("getprop", "ro.product.brand").Trim(),
                    Model = _client.Shell("getprop", "ro.product.model").Trim(),
                    Sdk = sdk,
                    AgentVersion = "uiautomator2",
                    Display = new AtxDeviceInfo.DisplayInfo
                    {
                        Width = size.X,
                        Height = size.Y
                    }
                };
            }

            using (SocketHelper socket = SocketHelper.Create(_url))
            {
                var result = socket.HttpGet("/info");
                if (result != null && result.Code == 200)
                {
                    JsonObject data = JsonNode.Parse(result.Content)!.AsObject();

                    // Parse the root-level properties
                    var deviceInfo = new AtxDeviceInfo
                    {
                        udid = data["udid"]?.GetValue<string>(),
                        Version = data["version"]?.GetValue<string>(),
                        Serial = data["serial"]?.GetValue<string>(),
                        Brand = data["brand"]?.GetValue<string>(),
                        Model = data["model"]?.GetValue<string>(),
                        Hwaddr = data["hwaddr"]?.GetValue<string>(),
                        Sdk = data["sdk"]?.GetValue<int>() ?? 0,
                        AgentVersion = data["agentVersion"]?.GetValue<string>(),
                        Arch = data["arch"],
                        Owner = data["owner"],
                        PresenceChangedAt = data["presenceChangedAt"],
                        UsingBeganAt = data["usingBeganAt"],
                        Product = data["product"],
                        Provider = data["provider"]
                    };

                    // Parse nested objects
                    if (data["display"] != null)
                    {
                        var display = data["display"];
                        deviceInfo.Display = new AtxDeviceInfo.DisplayInfo
                        {
                            Width = display["width"]?.GetValue<int>() ?? 0,
                            Height = display["height"]?.GetValue<int>() ?? 0
                        };
                    }

                    if (data["battery"] != null)
                    {
                        var battery = data["battery"];
                        deviceInfo.Battery = new AtxDeviceInfo.BatteryInfo
                        {
                            AcPowered = battery["acPowered"]?.GetValue<bool>() ?? false,
                            UsbPowered = battery["usbPowered"]?.GetValue<bool>() ?? false,
                            WirelessPowered = battery["wirelessPowered"]?.GetValue<bool>() ?? false,
                            Present = battery["present"]?.GetValue<bool>() ?? false,
                            Status = battery["status"]?.GetValue<int>() ?? 0,
                            Health = battery["health"]?.GetValue<int>() ?? 0,
                            Level = battery["level"]?.GetValue<int>() ?? 0,
                            Scale = battery["scale"]?.GetValue<int>() ?? 0,
                            Voltage = battery["voltage"]?.GetValue<int>() ?? 0,
                            Temperature = battery["temperature"]?.GetValue<int>() ?? 0,
                            Technology = battery["technology"]?.GetValue<string>()
                        };
                    }

                    if (data["memory"] != null)
                    {
                        var memory = data["memory"];
                        deviceInfo.Memory = new AtxDeviceInfo.MemoryInfo
                        {
                            Total = memory["total"]?.GetValue<long>() ?? 0,
                            Around = memory["around"]?.GetValue<string>()
                        };
                    }

                    if (data["cpu"] != null)
                    {
                        var cpu = data["cpu"];
                        deviceInfo.Cpu = new AtxDeviceInfo.CpuInfo
                        {
                            Cores = cpu["cores"]?.GetValue<int>() ?? 0,
                            Hardware = cpu["hardware"]?.GetValue<string>()
                        };
                    }

                    return deviceInfo;
                }
            }
            return null;
        }
        public string DumpHierarchy()
        {
            if (IsAppiumMode)
            {
                return _client.GetXMLSource("appium");
            }

            using (SocketHelper socket = SocketHelper.Create(_url))
            {
                //socket.SetTimeout(1000);
                var result = socket.HttpGet("/dump/hierarchy");
                if (result == null || result.Code != 200)
                {
                    return null;
                }
                JsonObject json = JsonNode.Parse(result.Content)!.AsObject();
                return json["result"]?.GetValue<string>();
            }

        }
        public bool Start()
        {
            if (IsAppiumMode)
            {
                return true;
            }
            return UIService.Start();

        }
        public bool Running()
        {
            if (IsAppiumMode)
            {
                return true;
            }
            return UIService.Running();
        }
        private readonly static Regex DumpsysDisplayScreenRegex = new Regex(".*DisplayViewport\\{.*?orientation=(?<orientation>.*?),.*?deviceWidth=(?<width>.*?),.*deviceHeight=(?<height>.*?)\\}");
        private readonly static Dictionary<Orientation, object[]> OrientationDict = new Dictionary<Orientation, object[]>() {
            { Orientation.Natural, new object[] { 0, "natural", "n", 0 } },
            { Orientation.Left, new object[] { 1, "left", "l", 90 } },
            { Orientation.Upsidedown, new object[] { 2, "upsidedown", "u", 180 } },
            { Orientation.Right, new object[] { 3, "right", "r", 270 } }
        };

        public enum Orientation
        {
            Natural = 0,
            Left,
            Upsidedown,
            Right
        }
        #region 获取屏幕方向
        /// <summary>
        /// Lấy hướng xoay hiện tại của màn hình thiết bị.
        /// Nếu không lấy được từ dumpsys thì fallback qua DeviceInfo().
        /// Trả về object[] chứa tên và giá trị hướng.
        /// </summary>
        public async Task<object[]> GetOrientation()
        {
            string result = _client.Shell("dumpsys", "display");
            Match match = DumpsysDisplayScreenRegex.Match(result);
            int o;
            if (match.Success)
            {
                o = int.Parse(match.Groups["orientation"].Value);
            }
            else
            {
                if (IsAppiumMode)
                {
                    o = 0;
                }
                else
                {
                    var info = DeviceInfo();
                    o = info?.DisplayRotation ?? 0;
                }
            }
            return OrientationDict[(Orientation)o];
        }
        public AppInfo GetAppInfo(string package)
        {
            if (IsAppiumMode)
            {
                string output = _client.Shell("cmd", "package", "resolve-activity", "--brief", package);
                var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                string activityLine = lines.LastOrDefault(x => x.Contains("/"))?.Trim() ?? string.Empty;
                string mainActivity = string.Empty;
                if (!string.IsNullOrWhiteSpace(activityLine))
                {
                    string[] parts = activityLine.Split('/');
                    if (parts.Length >= 2)
                    {
                        mainActivity = parts[1].Trim();
                    }
                }

                return new AppInfo
                {
                    Success = !string.IsNullOrWhiteSpace(mainActivity),
                    Description = string.IsNullOrWhiteSpace(mainActivity) ? "resolve-activity fail" : "ok",
                    Data = new AppInfo.DataInfo
                    {
                        PackageName = package,
                        MainActivity = mainActivity,
                        Label = package
                    }
                };
            }

            using (SocketHelper socket = SocketHelper.Create(_url))
            {
                var result = socket.HttpGet($"/packages/{package}/info");
                if (result == null || string.IsNullOrWhiteSpace(result.Content))
                {
                    return new AppInfo();
                }

                try
                {
                    JsonObject data = JsonNode.Parse(result.Content)!.AsObject();

                    // Parse the root-level properties
                    var appInfo = new AppInfo
                    {
                        Success = data["success"]?.GetValue<bool>() ?? false,
                        Description = data["description"]?.GetValue<string>()
                    };

                    // Parse nested DataInfo object
                    if (data["data"] != null)
                    {
                        var dataInfo = data["data"];
                        appInfo.Data = new AppInfo.DataInfo
                        {
                            PackageName = dataInfo["packageName"]?.GetValue<string>(),
                            MainActivity = dataInfo["mainActivity"]?.GetValue<string>(),
                            Label = dataInfo["label"]?.GetValue<string>(),
                            VersionName = dataInfo["versionName"]?.GetValue<string>(),
                            VersionCode = dataInfo["versionCode"]?.GetValue<long>() ?? 0,
                            Size = dataInfo["size"]?.GetValue<long>() ?? 0
                        };
                    }

                    return appInfo;
                }
                catch (JsonException ex)
                {
                    //  Debug.WriteLine($"Error parsing JSON: {ex.Message}");
                    return new AppInfo();
                }
            }
        }
        #endregion

        #region 设置屏幕方向
        /// <summary>
        /// Gửi lệnh RPC để đặt hướng màn hình (portrait, landscape, v.v.).
        /// </summary>
        public void SetOrientation(Orientation orientation)
        {
            if (IsAppiumMode)
            {
                _client.Shell("settings", "put", "system", "accelerometer_rotation", "0");
                _client.Shell("settings", "put", "system", "user_rotation", ((int)orientation).ToString());
                return;
            }
            JsonRpc("setOrientation", OrientationDict[orientation][1]);
        }

        #endregion

        #region 锁定屏幕方向
        /// <summary>
        /// freeze = true: Khóa xoay màn hình hiện tại.
        /// freeze = false: Cho phép xoay tự động.
        /// </summary>
        public void FreezeRotation(bool freezed = true)
        {
            if (IsAppiumMode)
            {
                _client.Shell("settings", "put", "system", "accelerometer_rotation", freezed ? "0" : "1");
                return;
            }
            JsonRpc("freezeRotation", freezed);
        }
        #endregion

        #region 获取分辨率
        /// <summary>
        /// Lấy kích thước màn hình hiện tại dưới dạng Width x Height.
        /// </summary>
        public Size GetWindowSize()
        {
            if (IsAppiumMode)
            {
                var size = _client.GetScreenResolutionForInput();
                return new Size(size.X, size.Y);
            }

            var info = Info();
            if (info == null) return new Size();
            return new Size(info.Display.Width, info.Display.Height);
        }
        #endregion

        #region 息屏/亮屏
        /// <summary>
        /// Bật sáng màn hình.
        /// </summary>
        public void ScreenOn()
        {
            if (IsAppiumMode)
            {
                _client.Shell("input", "keyevent", "KEYCODE_WAKEUP");
                return;
            }

            JsonRpc("wakeUp");
        }

        /// <summary>
        /// Tắt màn hình.
        /// </summary>
        public void ScreenOff()
        {
            if (IsAppiumMode)
            {
                _client.Shell("input", "keyevent", "KEYCODE_POWER");
                return;
            }

            JsonRpc("sleep");
        }
        #endregion
        internal Point Rel2Abs(float x, float y)
        {
            Point pos = new Point();
            Size size = GetWindowSize();
            if (x > 1)
            {
                pos.X = (int)x;
            }
            else
            {
                pos.X = (int)(x * size.Width);
            }
            if (y > 1)
            {
                pos.Y = (int)y;
            }
            else
            {
                pos.Y = (int)(y * size.Height);
            }
            return pos;
        }
        #region 屏幕点击
        /// <summary>
        /// Click vào vị trí x/y trên màn hình.
        /// Trả về true nếu click thành công.
        /// </summary>
        public bool Click(float x, float y)
        {
            if (IsAppiumMode)
            {
                return _client.Click(x, y);
            }

            var pos = Rel2Abs(x, y);
            var result = JsonRpc("click", pos.X, pos.Y);
            if (result == null)
            {
                // JsonRpc null = ATX agent không phản hồi (timeout/crash/port chưa forward)
                // Fallback sang adb input tap — không phụ thuộc ATX agent
                _client.LogHelper.Log($"[Click] JsonRpc null tại ({pos.X},{pos.Y}), fallback adb input tap");
                var tapResult = _client.Shell("input", "tap", pos.X.ToString(), pos.Y.ToString());
                return tapResult != null && !tapResult.Contains("error", StringComparison.OrdinalIgnoreCase);
            }
            if (result.Error != null)
            {
                _client.LogHelper.Log($"[Click] JsonRpc error: {result.Error.ToJsonString()}, fallback adb input tap");
                var tapResult = _client.Shell("input", "tap", pos.X.ToString(), pos.Y.ToString());
                return tapResult != null && !tapResult.Contains("error", StringComparison.OrdinalIgnoreCase);
            }
            return result.Data is JsonValue s && s.GetValue<bool>();
        }

        /// <summary>
        /// Click nhanh 2 lần vào cùng vị trí, cách nhau 'wait' milliseconds.
        /// </summary>
        public bool DoubleClick(float x, float y, int wait = 60)
        {
            if (IsAppiumMode)
            {
                bool first = _client.Click(x, y);
                Thread.Sleep(wait);
                bool second = _client.Click(x, y);
                return first && second;
            }

            var pos = Rel2Abs(x, y);
            AtxTouch.Down(this, pos.X, pos.Y).Up(pos.X, pos.Y);
            Thread.Sleep(wait);
            return Click(x, y);
        }

        /// <summary>
        /// Nhấn giữ vào vị trí x/y trong khoảng thời gian time (ms).
        /// </summary>
        public void LongClick(float x, float y, int time = 500)
        {
            if (IsAppiumMode)
            {
                _client.LongClick(x, y, time);
                return;
            }

            Thread.Sleep(UINodeClickExistDelay);
            var pos = Rel2Abs(x, y);
            // Dùng ADB input swipe từ điểm đến chính điểm đó với duration để long press
            _client.Shell("input", "swipe", pos.X, pos.Y, pos.X, pos.Y, time);
        }
        #endregion

        #region 屏幕滑动
        /// <summary>
        /// Vuốt từ điểm (fx, fy) đến (lx, ly) trong khoảng thời gian 'duration' (ms).
        /// </summary>
        public bool Swipe(float fx, float fy, float lx, float ly, int duration = 55)
        {
            if (IsAppiumMode)
            {
                var start = Rel2Abs(fx, fy);
                var end = Rel2Abs(lx, ly);
                return AppiumSessionService.Swipe(_client, start.X, start.Y, end.X, end.Y, duration);
            }

            if (duration < 2) duration = 2;
            var fpos = Rel2Abs(fx, fy);
            var lpos = Rel2Abs(lx, ly);
            var result = JsonRpc("swipe", fpos.X, fpos.Y, lpos.X, lpos.Y, duration);
            if (result == null)
            {
                // ATX agent không phản hồi — fallback sang adb input swipe
                _client.LogHelper.Log($"[Swipe] JsonRpc null ({fpos.X},{fpos.Y})->({lpos.X},{lpos.Y}), fallback adb input swipe");
                var swipeResult = _client.Shell("input", "swipe", fpos.X.ToString(), fpos.Y.ToString(), lpos.X.ToString(), lpos.Y.ToString(), duration.ToString());
                return swipeResult != null && !swipeResult.Contains("error", StringComparison.OrdinalIgnoreCase);
            }
            if (result.Error != null)
            {
                _client.LogHelper.Log($"[Swipe] JsonRpc error: {result.Error.ToJsonString()}, fallback adb input swipe");
                var swipeResult = _client.Shell("input", "swipe", fpos.X.ToString(), fpos.Y.ToString(), lpos.X.ToString(), lpos.Y.ToString(), duration.ToString());
                return swipeResult != null && !swipeResult.Contains("error", StringComparison.OrdinalIgnoreCase);
            }
            return result.Data is JsonValue sv && sv.GetValue<bool>();
        }
        #endregion

        #region 屏幕操作
        public void TouchDown(float x, float y)
        {
            if (IsAppiumMode)
            {
                _client.LogHelper.Log("TouchDown chưa hỗ trợ riêng trên appium mode, dùng Swipe/LongClick thay thế.");
                return;
            }
            var pos = Rel2Abs(x, y);
            AtxTouch.Down(this, pos.X, pos.Y);
        }

        public void TouchMove(float x, float y)
        {
            if (IsAppiumMode)
            {
                _client.LogHelper.Log("TouchMove chưa hỗ trợ riêng trên appium mode, dùng Swipe thay thế.");
                return;
            }
            var pos = Rel2Abs(x, y);
            AtxTouch.Move(this, pos.X, pos.Y);
        }

        public void TouchUp(float x, float y)
        {
            if (IsAppiumMode)
            {
                _client.LogHelper.Log("TouchUp chưa hỗ trợ riêng trên appium mode.");
                return;
            }
            var pos = Rel2Abs(x, y);
            AtxTouch.Up(this, pos.X, pos.Y);
        }
        #endregion

        #region 拖
        /// <summary>
        /// Kéo từ điểm (fx, fy) đến (lx, ly), thời gian kéo là duration (ms x 200).
        /// </summary>
        public bool Drag(float fx, float fy, float lx, float ly, int duration = 55)
        {
            if (IsAppiumMode)
            {
                var start = Rel2Abs(fx, fy);
                var end = Rel2Abs(lx, ly);
                return AppiumSessionService.Swipe(_client, start.X, start.Y, end.X, end.Y, duration * 200);
            }

            if (duration < 2) duration = 2;
            duration *= 200;
            var fpos = Rel2Abs(fx, fy);
            var lpos = Rel2Abs(lx, ly);
            var result = JsonRpc("drag", fpos.X, fpos.Y, lpos.X, lpos.Y, duration);
            if (result == null) return false;
            return result.Data is JsonValue dv && dv.GetValue<bool>();
        }
        #endregion

        #region 按下按钮
        /// <summary>
        /// Gửi tên phím (string) để nhấn trên thiết bị (ví dụ: "home").
        /// </summary>
        public void Press(string key)
        {
            if (IsAppiumMode)
            {
                _client.Shell("input", "keyevent", $"KEYCODE_{key.ToUpperInvariant()}");
                return;
            }
            JsonRpc("pressKey", key);
        }
        public void SendText(string text)
        {
            if (IsAppiumMode)
            {
                _client.Shell("input", "text", text.Replace(" ", "%s"));
                return;
            }
            JsonRpc("input", text.ToString());
        }
        public Bitmap Screenshot()
        {
            if (IsAppiumMode)
            {
                return _client.Screenshot();
            }

            try
            {
                string url = $"{_url}/screenshot/0";

                using (WebClient client = new WebClient())
                {
                    // Tải dữ liệu ảnh về dưới dạng byte[]
                    byte[] imageBytes = client.DownloadData(url);

                    // Chuyển byte[] sang Bitmap
                    using (var ms = new System.IO.MemoryStream(imageBytes))
                    {
                        return new Bitmap(ms);
                    }
                }
            }
            catch (Exception ex)
            {
                _client.LogHelper.Log($"[Screenshot] Lỗi: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// Gửi enum PressKey để nhấn các phím hệ thống.
        /// </summary>
        public void Press(PressKey key)
        {
            if (IsAppiumMode)
            {
                Press(PressKeyDict[key]);
                return;
            }
            JsonRpc("pressKey", PressKeyDict[key]);
        }
        #endregion
        private readonly static Dictionary<PressKey, string> PressKeyDict = new Dictionary<PressKey, string>() {
            { PressKey.Home, "home" },
            { PressKey.Back, "back" },
            { PressKey.Left, "left" },
            { PressKey.Right, "right" },
            { PressKey.Up, "up" },
            { PressKey.Down, "down" },
            { PressKey.Center, "center" },
            { PressKey.Menu, "menu" },
            { PressKey.Search, "search" },
            { PressKey.Enter, "enter" },
            { PressKey.Delete, "delete" },
            { PressKey.Recent, "recent" },
            { PressKey.VolumeUp, "volume_up" },
            { PressKey.VolumeDown, "volume_down" },
            { PressKey.VolumeMute, "volume_mute" },
            { PressKey.Camera, "camera" },
            { PressKey.Power, "power" },
        };
    }
}
