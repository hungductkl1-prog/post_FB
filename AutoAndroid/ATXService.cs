using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sunny.Subdy.Data.Models;

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
            Stopwatch stopwatch = Stopwatch.StartNew();
            _initer = new InitHelper(_client);
            int i = 1;
            try
            {
                i++;
                _initer.Reinstall();
            }
            catch (Exception ex)
            {
                _client.LogHelper.Log(ex.Message);
            }
            _client.RunTime($"Connect: UIAUTOMATOR", () => Connect());
            _client.RunTime($"Connect: UIAUTOMATOR ", () => RunUiautomator());
            return RunUiautomator();
        }
        public bool Connect()
        {
            if (_client != null && _client.Device.Port != 0)
            {
                using var client = new ADBSocket(_serial);
                int port = client.ForwardPort(7912);
                if (port == -1)
                {
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
                _client.LogHelper.Log($"DeviceInfo: {json.Error.ToString(Formatting.None)}");
                return null;
            }
            JObject data = (JObject)json.Data;

            // Parse the JObject manually
            var deviceInfo = new UADeviceInfo
            {
                CurrentPackageName = data["currentPackageName"]?.ToString(),
                DisplayRotation = data["displayRotation"]?.ToObject<int>() ?? 0,
                DisplayHeight = data["displayHeight"]?.ToObject<int>() ?? 0,
                DisplayWidth = data["displayWidth"]?.ToObject<int>() ?? 0,
                DisplaySizeDpX = data["displaySizeDpX"]?.ToObject<int>() ?? 0,
                DisplaySizeDpY = data["displaySizeDpY"]?.ToObject<int>() ?? 0,
                ProductName = data["productName"]?.ToString(),
                ScreenOn = data["screenOn"]?.ToObject<bool>() ?? false,
                SdkInt = data["sdkInt"]?.ToObject<int>() ?? 0,
                NaturalOrientation = data["naturalOrientation"]?.ToObject<bool>() ?? false,
            };

            return deviceInfo;
        }
        public JsonRpcResponse JsonRpc(string method, params object[] argv)
        {
            string url = $"{_url}/jsonrpc/0";
            JArray array = new JArray();
            foreach (var obj in argv)
            {
                if (obj is By)
                {
                    array.Add((obj as By).ToJson());
                    continue;
                }
                array.Add(obj);
            }
            string id = Guid.NewGuid().ToString().Replace("-", "");
            JObject json = new JObject {
        { "jsonrpc", "2.0" },
        { "id", id },
        { "method", method },
        { "params", array }
    };

            try
            {
                using (var socket = SocketHelper.Create(_url))
                {
                    var result = socket.HttpPost("/jsonrpc/0", json);

                    if (result == null || result.Code != 200)
                    {
                        return null;
                    }
                    if (string.IsNullOrWhiteSpace(result.Content))
                    {
                        return null;
                    }

                    // Parse JSON response manually
                    var jsonResponse = JObject.Parse(result.Content);
                    JsonRpcResponse response = new JsonRpcResponse();

                    response.Version = jsonResponse["jsonrpc"]?.ToString();
                    response.Id = jsonResponse["id"]?.ToString();
                    response.Error = jsonResponse["error"]?.ToObject<JObject>();
                    response.Data = jsonResponse["result"]?.ToObject<object>();

                    return response;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Failed to connect to "))
                {
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
            using (SocketHelper socket = SocketHelper.Create(_url))
            {
                var result = socket.HttpGet("/info");
                if (result.Code == 200)
                {
                    JObject data = JObject.Parse(result.Content);

                    // Parse the root-level properties
                    var deviceInfo = new AtxDeviceInfo
                    {
                        udid = data["udid"]?.ToString(),
                        Version = data["version"]?.ToString(),
                        Serial = data["serial"]?.ToString(),
                        Brand = data["brand"]?.ToString(),
                        Model = data["model"]?.ToString(),
                        Hwaddr = data["hwaddr"]?.ToString(),
                        Sdk = data["sdk"]?.ToObject<int>() ?? 0,
                        AgentVersion = data["agentVersion"]?.ToString(),
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
                            Width = display["width"]?.ToObject<int>() ?? 0,
                            Height = display["height"]?.ToObject<int>() ?? 0
                        };
                    }

                    if (data["battery"] != null)
                    {
                        var battery = data["battery"];
                        deviceInfo.Battery = new AtxDeviceInfo.BatteryInfo
                        {
                            AcPowered = battery["acPowered"]?.ToObject<bool>() ?? false,
                            UsbPowered = battery["usbPowered"]?.ToObject<bool>() ?? false,
                            WirelessPowered = battery["wirelessPowered"]?.ToObject<bool>() ?? false,
                            Present = battery["present"]?.ToObject<bool>() ?? false,
                            Status = battery["status"]?.ToObject<int>() ?? 0,
                            Health = battery["health"]?.ToObject<int>() ?? 0,
                            Level = battery["level"]?.ToObject<int>() ?? 0,
                            Scale = battery["scale"]?.ToObject<int>() ?? 0,
                            Voltage = battery["voltage"]?.ToObject<int>() ?? 0,
                            Temperature = battery["temperature"]?.ToObject<int>() ?? 0,
                            Technology = battery["technology"]?.ToString()
                        };
                    }

                    if (data["memory"] != null)
                    {
                        var memory = data["memory"];
                        deviceInfo.Memory = new AtxDeviceInfo.MemoryInfo
                        {
                            Total = memory["total"]?.ToObject<long>() ?? 0,
                            Around = memory["around"]?.ToString()
                        };
                    }

                    if (data["cpu"] != null)
                    {
                        var cpu = data["cpu"];
                        deviceInfo.Cpu = new AtxDeviceInfo.CpuInfo
                        {
                            Cores = cpu["cores"]?.ToObject<int>() ?? 0,
                            Hardware = cpu["hardware"]?.ToString()
                        };
                    }

                    return deviceInfo;
                }
            }
            return null;
        }
        public string DumpHierarchy()
        {
            using (SocketHelper socket = SocketHelper.Create(_url))
            {
                //socket.SetTimeout(1000);
                var result = socket.HttpGet("/dump/hierarchy");
                if (result == null || result.Code != 200)
                {
                    return null;
                }
                JObject json = JObject.Parse(result.Content);
                return json.Value<string>("result");
            }

        }
        public bool Start()
        {
            return UIService.Start();

        }
        public bool Running()
        {
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
                o = DeviceInfo().DisplayRotation;
            }
            return OrientationDict[(Orientation)o];
        }
        public AppInfo GetAppInfo(string package)
        {
            using (SocketHelper socket = SocketHelper.Create(_url))
            {
                var result = socket.HttpGet($"/packages/{package}/info");
                if (result == null || string.IsNullOrWhiteSpace(result.Content))
                {
                    return new AppInfo();
                }

                try
                {
                    JObject data = JObject.Parse(result.Content);

                    // Parse the root-level properties
                    var appInfo = new AppInfo
                    {
                        Success = data["success"]?.ToObject<bool>() ?? false,
                        Description = data["description"]?.ToString()
                    };

                    // Parse nested DataInfo object
                    if (data["data"] != null)
                    {
                        var dataInfo = data["data"];
                        appInfo.Data = new AppInfo.DataInfo
                        {
                            PackageName = dataInfo["packageName"]?.ToString(),
                            MainActivity = dataInfo["mainActivity"]?.ToString(),
                            Label = dataInfo["label"]?.ToString(),
                            VersionName = dataInfo["versionName"]?.ToString(),
                            VersionCode = dataInfo["versionCode"]?.ToObject<long>() ?? 0,
                            Size = dataInfo["size"]?.ToObject<long>() ?? 0
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
            JsonRpc("freezeRotation", freezed);
        }
        #endregion

        #region 获取分辨率
        /// <summary>
        /// Lấy kích thước màn hình hiện tại dưới dạng Width x Height.
        /// </summary>
        public Size GetWindowSize()
        {
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
            JsonRpc("wakeUp");
        }

        /// <summary>
        /// Tắt màn hình.
        /// </summary>
        public void ScreenOff()
        {
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
            var pos = Rel2Abs(x, y);
            var result = JsonRpc("click", new int[] { pos.X, pos.Y });
            if (result == null) return false;
            if (result.Error != null) return false;
            return result.Data is bool s && s;
        }

        /// <summary>
        /// Click nhanh 2 lần vào cùng vị trí, cách nhau 'wait' milliseconds.
        /// </summary>
        public bool DoubleClick(float x, float y, int wait = 60)
        {
            var pos = Rel2Abs(x, y);
            AtxTouch.Down(this, pos.X, pos.Y).Up(pos.X, pos.Y);
            Thread.Sleep(wait);
            Click(x, y);
            return false;
        }

        /// <summary>
        /// Nhấn giữ vào vị trí x/y trong khoảng thời gian time (ms).
        /// </summary>
        public void LongClick(float x, float y, int time = 500)
        {
            Thread.Sleep(UINodeClickExistDelay);
            var pos = Rel2Abs(x, y);
            AtxTouch.Down(this, pos.X, pos.Y).Wait(time).Up(pos.X, pos.Y);
        }
        #endregion

        #region 屏幕滑动
        /// <summary>
        /// Vuốt từ điểm (fx, fy) đến (lx, ly) trong khoảng thời gian 'duration' (ms).
        /// </summary>
        public bool Swipe(float fx, float fy, float lx, float ly, int duration = 55)
        {
            if (duration < 2) duration = 2;
            var fpos = Rel2Abs(fx, fy);
            var lpos = Rel2Abs(lx, ly);
            var result = JsonRpc("swipe", fpos.X, fpos.Y, lpos.X, lpos.Y, duration);
            if (result == null) return false;
            return result.Data is bool s && s;
        }
        #endregion

        #region 屏幕操作
        public void TouchDown(float x, float y)
        {
            var pos = Rel2Abs(x, y);
            AtxTouch.Down(this, pos.X, pos.Y);
        }

        public void TouchMove(float x, float y)
        {
            var pos = Rel2Abs(x, y);
            AtxTouch.Move(this, pos.X, pos.Y);
        }

        public void TouchUp(float x, float y)
        {
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
            if (duration < 2) duration = 2;
            duration *= 200;
            var fpos = Rel2Abs(fx, fy);
            var lpos = Rel2Abs(lx, ly);
            var result = JsonRpc("drag", fpos.X, fpos.Y, lpos.X, lpos.Y, duration);
            if (result == null) return false;
            return result.Data is bool s && s;
        }
        #endregion

        #region 按下按钮
        /// <summary>
        /// Gửi tên phím (string) để nhấn trên thiết bị (ví dụ: "home").
        /// </summary>
        public void Press(string key)
        {
             JsonRpc("pressKey", key);
        }
        public void SendText(string text)
        {
             JsonRpc("input", text.ToString());
        }
        public Bitmap Screenshot()
        {
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
