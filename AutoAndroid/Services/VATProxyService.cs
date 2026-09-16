using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace AutoAndroid
{
    public class VATProxyService
    {
        public const string Package_Proxy = "com.vat.vpn";
        public static string path_VATProxy = Path.Combine(AppContext.BaseDirectory, "App", "VATProxy.apk");
        private const int BroadcastTimeoutSeconds = 15;
        private const int ConnectMaxAttempts = 2;
        ADBClient _client;
        public VATProxyService(ADBClient client)
        {
            _client = client;
        }
        private void Close()
        {
            _client.StopApp(Package_Proxy);
        }
        private bool Open()
        {
            for (int i = 0; i < 5; i++)
            {
                _client.AppStart(Package_Proxy, true, true, wait: true);
                _client.SetSize();
                if (_client.AppWait(Package_Proxy))
                {
                    return true;
                }
            }
            return false;
        }

        public bool ConnectProxy(string proxys, bool installIfMissing = true)
        {
            try
            {
                _client.LogHelper.State = "Kết nối proxy";
                string serial = _client.Device?.Serial ?? "?";
                // KHÔNG ghi address/user/pass vào log bền vững (thông tin nhạy cảm).
                ProxyChangeLog.Write(serial, $"ConnectProxy: BẮT ĐẦU.");

                // Không kiểm tra danh sách package ở đây: _client.AppList() đi qua Shell
                // với vòng retry + Connect lại đầy đủ, là điểm treo im nhiều phút khi
                // adb/device chập chờn. Thiếu VAT Proxy sẽ tự lộ ra ở broadcast bên dưới
                // (receiver không tồn tại -> không có "successful") và trả false.
                string[] proxy = (proxys ?? string.Empty).Split(':');
                if (proxy.Length < 2 || string.IsNullOrWhiteSpace(proxy[0]) || string.IsNullOrWhiteSpace(proxy[1]))
                {
                    _client.LogHelper.Log("Proxy không hợp lệ, cần dạng ip:port hoặc ip:port:user:password.");
                    ProxyChangeLog.Write(serial, "ConnectProxy: chuỗi proxy KHÔNG hợp lệ (cần ip:port[:user:pass]).");
                    return false;
                }

                string host = proxy[0].Trim();
                string port = proxy[1].Trim();
                string username = proxy.Length > 2 ? proxy[2].Trim() : string.Empty;
                string password = proxy.Length > 3 ? string.Join(":", proxy.Skip(3)).Trim() : string.Empty;

                bool isHostname = !System.Net.IPAddress.TryParse(host, out _);

                // Kiến trúc 3 TẦNG — broadcast hostname trước, lùi dần về đường an toàn
                // hơn. Không device nào bị break: bản vá dừng ở tầng 1, bản gốc ở tầng 2,
                // device mới cài (chưa cấp consent VPN) / DNS chập chờn ở tầng 3.
                //
                // TẦNG 1 — broadcast thẳng host (hostname hoặc IP số).
                // Bản VAT ĐÃ VÁ (StrictMode permitAll ở đầu ProxyReceiver.onReceive) tự
                // phân giải DNS ngay trên main thread nên nhận trực tiếp hostname ->
                // nhanh nhất (~0.4s), KHÔNG cần ping. Bản GỐC crash
                // NetworkOnMainThreadException với hostname -> tầng 1 fail, rơi xuống
                // tầng 2. Host là IP số thì cả bản vá lẫn bản gốc đều chạy ẩn ngay ở
                // đây (không có DNS, không crash).
                //
                // Với hostname chỉ thử 1 lần: crash của bản gốc là TẤT ĐỊNH (cứ hostname
                // là throw) nên retry tầng 1 là vô ích; với IP số giữ ConnectMaxAttempts
                // để chống broadcast chập chờn thoáng qua.
                ProxyChangeLog.Write(serial, $"ConnectProxy: TẦNG 1 broadcast host gốc ({(isHostname ? "hostname" : "IP số")}).");
                if (TryBroadcast(serial, host, port, username, password,
                        isHostname ? 1 : ConnectMaxAttempts, "tầng1"))
                    return true;

                // TẦNG 2 — chỉ khi host là hostname (bản GỐC không tự phân giải được).
                // Phân giải hostname->IP NGAY TRÊN THIẾT BỊ bằng `ping -c 1` (DNS của
                // mạng/VPN đang dùng có hiệu lực) rồi broadcast IP số -> giữ đường ẩn
                // (~1s), nhanh hơn hẳn fallback UI. Host vốn là IP thì bỏ qua tầng này
                // (tầng 1 đã thử chính IP đó).
                string resolvedIp = string.Empty;
                if (isHostname)
                {
                    resolvedIp = ResolveOnDevice(serial, host);
                    if (!string.IsNullOrEmpty(resolvedIp))
                    {
                        ProxyChangeLog.Write(serial, "ConnectProxy: TẦNG 2 đã phân giải hostname trên thiết bị -> broadcast IP.");
                        if (TryBroadcast(serial, resolvedIp, port, username, password,
                                ConnectMaxAttempts, "tầng2"))
                            return true;
                    }
                    else
                    {
                        ProxyChangeLog.Write(serial, "ConnectProxy: TẦNG 2 KHÔNG phân giải được hostname trên thiết bị (DNS fail) -> bỏ qua broadcast IP.");
                    }
                }

                // TẦNG 3 — fallback nhập giao diện (ConnectViaUi). Dùng khi:
                //  - device MỚI CÀI apk (chưa cấp consent VPN lần đầu): broadcast luôn
                //    trả "Please enable VPN for the first time..."; mở UI bấm CONNECT
                //    để hệ thống tự cấp consent, các account sau chạy ẩn lại được.
                //  - DNS device chập chờn khiến cả hostname lẫn IP broadcast không lên.
                // Nhập IP đã phân giải nếu có (ổn định hơn); không thì nhập hostname gốc.
                string uiAddress = !string.IsNullOrEmpty(resolvedIp) ? resolvedIp : host;
                ProxyChangeLog.Write(serial, "ConnectProxy: TẦNG 3 chuyển sang fallback nhập giao diện (ConnectViaUi).");
                return ConnectViaUi(uiAddress, port, username, password);
            }
            catch (Exception ex)
            {
                _client.LogHelper.Log($"Lỗi kết nối proxy: {ex.Message}");
                ProxyChangeLog.Write(_client.Device?.Serial ?? "?", $"ConnectProxy: lỗi ngoài dự kiến: {ex.Message}");
                return false;
            }
        }

        // Broadcast CONNECT_PROXY tới receiver của VAT và xác nhận bằng chuỗi
        // "successful" trong kết quả. address có thể là hostname (bản VÁ tự phân giải
        // DNS) hoặc IP số (cả bản vá lẫn bản gốc đều chạy ẩn). Thử tối đa attempts lần;
        // tag ("tầng1"/"tầng2") chỉ để ghi log phân biệt tầng nào đang chạy.
        // Trả true ngay khi một lần broadcast trả "successful".
        private bool TryBroadcast(string serial, string address, string port, string username, string password, int attempts, string tag)
        {
            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    Close();
                    // -f 32 (FLAG_INCLUDE_STOPPED_PACKAGES): receiver vẫn khởi động
                    // được ngay sau force-stop, không bị "process is bad".
                    string cmd = $"am broadcast -f 32 -a com.vat.vpn.CONNECT_PROXY -n com.vat.vpn/.ui.ProxyReceiver --es address {address} --es port {port}";
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                        cmd += $" --es username {username} --es password {password}";

                    // Broadcast qua adb với timeout cứng thay vì Shell (Shell retry 3
                    // lần và lần cuối Connect lại đầy đủ — có thể treo im nhiều phút).
                    string connectResult = AutoAndroid.ProcessHelper.RunAdbCommand(
                        $"-s {serial} shell {cmd}", BroadcastTimeoutSeconds) ?? string.Empty;
                    bool ok = connectResult.Contains("successful", StringComparison.OrdinalIgnoreCase);
                    ProxyChangeLog.Write(serial, $"ConnectProxy[{tag}]: broadcast lần {i + 1}/{attempts} -> {(ok ? "successful" : "KHÔNG successful")}");
                    if (ok)
                        return true;

                    _client.LogHelper.Log("VAT Proxy không xác nhận kết nối qua broadcast.");
                }
                catch (Exception ex)
                {
                    _client.LogHelper.Log($"Lỗi kết nối proxy ({tag}) lần {i + 1}: {ex.Message}");
                    ProxyChangeLog.Write(serial, $"ConnectProxy[{tag}]: broadcast lần {i + 1} lỗi: {ex.Message}");
                }

                Close();
            }
            return false;
        }

        // Phân giải hostname sang IP ngay trên thiết bị bằng `ping -c 1` (toybox Android
        // không có getent). Dòng đầu output dạng "PING host (1.2.3.4) 56(84) bytes..." —
        // lấy IP trong ngoặc đơn đầu tiên. Trả chuỗi rỗng nếu không phân giải được.
        private static string ResolveOnDevice(string serial, string hostname)
        {
            try
            {
                string output = ProcessHelper.RunAdbCommand(
                    $"-s {serial} shell ping -c 1 -W 2 {hostname}", 8) ?? string.Empty;
                var match = System.Text.RegularExpressions.Regex.Match(
                    output, @"\((\d{1,3}(?:\.\d{1,3}){3})\)");
                if (match.Success && System.Net.IPAddress.TryParse(match.Groups[1].Value, out var ip))
                    return ip.ToString();
            }
            catch
            {
                // Lỗi phân giải không được coi là lỗi chết: caller sẽ broadcast hostname
                // gốc và cuối cùng vẫn còn fallback nhập giao diện.
            }
            return string.Empty;
        }

        // Nhập address/port/user/pass thẳng vào màn hình VAT Proxy (qua ADB Keyboard)
        // và bấm nút CONNECT — giống thao tác thủ công; xác nhận bằng sự xuất hiện
        // của interface tun0 (VPN đã lên). Dùng khi broadcast không trả "successful"
        // (bản VAT này crash khi tự phân giải DNS hostname trên main thread).
        //
        // Toàn bộ thao tác UI đi qua "adb shell input tap/swipe" với tọa độ đọc thẳng
        // từ dump (bounds hệ landscape) thay vì ATX Click/Swipe: ATX Rel2Abs phụ thuộc
        // GetWindowSize, lệch hệ tọa độ portrait/landscape làm click TRƯỢT ô. Click trượt
        // -> ô không focus -> BACK sau đó (tưởng là đóng bàn phím) ĐÓNG LUÔN ProxyActivity
        // rơi về launcher -> các ô sau gõ vào launcher -> không tìm thấy nút CONNECT.
        private bool ConnectViaUi(string address, string port, string username, string password)
        {
            string serial = _client.Device?.Serial ?? "?";
            try
            {
                _client.LogHelper.Log("Broadcast không xác nhận; thử nhập trực tiếp vào giao diện VAT Proxy.");

                // Mở thẳng ProxyActivity bằng adb (timeout cứng, không qua Shell retry).
                ProcessHelper.RunAdbCommand(
                    $"-s {serial} shell am start -n {Package_Proxy}/.ui.ProxyActivity", 10);

                string dump = string.Empty;
                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(2000);
                    dump = _client.GetXMLSource();
                    if (!string.IsNullOrEmpty(dump) && dump.Contains("com.vat.vpn:id/edtAddress"))
                        break;
                    dump = string.Empty;
                }
                if (string.IsNullOrEmpty(dump))
                {
                    _client.LogHelper.Log("Không đọc được giao diện VAT Proxy.");
                    ProxyChangeLog.Write(serial, "ConnectViaUi: KHÔNG đọc được giao diện VAT (thiếu VAT Proxy hoặc ATX dump lỗi).");
                    return false;
                }
                ProxyChangeLog.Write(serial, "ConnectViaUi: đã mở được giao diện VAT Proxy (thấy edtAddress).");

                // Cả 4 ô đều hiện trên màn hình ngang KHÔNG cần cuộn — nhập hết trước.
                // Kích hoạt ADB Keyboard MỘT lần (cài nếu thiếu + đặt làm IME mặc định);
                // còn việc GÕ từng ô đi qua broadcast adb timeout cứng trong InputField
                // thay vì keyboard.Input — Input dùng _service.Shell (retry+Connect, điểm
                // treo im nhiều phút khi adb chập chờn).
                var keyboard = new ADBKeyboardService(_client);
                bool imeReady = keyboard.TurnOnADBKeyboard().GetAwaiter().GetResult();
                ProxyChangeLog.Write(serial, $"ConnectViaUi: ADB Keyboard sẵn sàng = {imeReady}.");
                if (!imeReady)
                {
                    _client.LogHelper.Log("Không bật được ADB Keyboard để nhập proxy.");
                    ProxyChangeLog.Write(serial, "ConnectViaUi: FAIL — không kích hoạt được ADB Keyboard (thiếu apk/ime set lỗi).");
                    return false;
                }

                if (!InputField("com.vat.vpn:id/edtAddress", address, dump))
                {
                    _client.LogHelper.Log("Không nhập được địa chỉ proxy vào giao diện VAT.");
                    ProxyChangeLog.Write(serial, "ConnectViaUi: FAIL ở ô edtAddress.");
                    return false;
                }
                if (!InputField("com.vat.vpn:id/edtPort", port, dump))
                {
                    _client.LogHelper.Log("Không nhập được cổng proxy vào giao diện VAT.");
                    ProxyChangeLog.Write(serial, "ConnectViaUi: FAIL ở ô edtPort.");
                    return false;
                }
                if (!string.IsNullOrEmpty(username) && !InputField("com.vat.vpn:id/edtUsername", username, dump))
                {
                    _client.LogHelper.Log("Không nhập được tên đăng nhập proxy vào giao diện VAT.");
                    ProxyChangeLog.Write(serial, "ConnectViaUi: FAIL ở ô edtUsername.");
                    return false;
                }
                if (!string.IsNullOrEmpty(password) && !InputField("com.vat.vpn:id/edtPassword", password, dump))
                {
                    _client.LogHelper.Log("Không nhập được mật khẩu proxy vào giao diện VAT.");
                    ProxyChangeLog.Write(serial, "ConnectViaUi: FAIL ở ô edtPassword.");
                    return false;
                }
                ProxyChangeLog.Write(serial, "ConnectViaUi: nhập xong các ô proxy.");

                // Nút CONNECT (btDefaultConnection) nằm DƯỚI nếp gấp màn hình: vuốt lên.
                // Dùng "adb shell input swipe" tọa độ landscape thô — đã kiểm chứng làm
                // nút CONNECT hiện ra (ATX Swipe qua Rel2Abs cuộn không đúng ô này).
                int cx = -1, cy = -1;
                for (int swipe = 0; swipe < 3 && cx < 0; swipe++)
                {
                    ProcessHelper.RunAdbCommand($"-s {serial} shell input swipe 1200 1250 1200 450 400", 10);
                    Thread.Sleep(1500);
                    string dump2 = _client.GetXMLSource();
                    var m = System.Text.RegularExpressions.Regex.Match(
                        dump2 ?? string.Empty,
                        "resource-id=\"com.vat.vpn:id/btDefaultConnection\"[^>]*bounds=\"\\[(\\d+),(\\d+)\\]\\[(\\d+),(\\d+)\\]\"");
                    if (m.Success)
                    {
                        cx = (int.Parse(m.Groups[1].Value) + int.Parse(m.Groups[3].Value)) / 2;
                        cy = (int.Parse(m.Groups[2].Value) + int.Parse(m.Groups[4].Value)) / 2;
                    }
                }
                if (cx < 0)
                {
                    _client.LogHelper.Log("Không thấy nút CONNECT trên giao diện VAT Proxy.");
                    ProxyChangeLog.Write(serial, "ConnectViaUi: KHÔNG thấy nút btDefaultConnection sau 3 lần vuốt.");
                    return false;
                }
                ProxyChangeLog.Write(serial, $"ConnectViaUi: thấy nút CONNECT ({cx},{cy}), bấm kết nối.");

                // Bấm CONNECT bằng adb thô theo tâm nút (không qua ATX Click/Rel2Abs).
                ProcessHelper.RunAdbCommand($"-s {serial} shell input tap {cx} {cy}", 10);

                // VPN lên sẽ tạo interface tun0 — kiểm tra bằng adb timeout cứng.
                for (int i = 0; i < 10; i++)
                {
                    Thread.Sleep(2000);
                    string links = ProcessHelper.RunAdbCommand(
                        $"-s {serial} shell ip link show", 5) ?? string.Empty;
                    if (links.Contains("tun0"))
                    {
                        _client.LogHelper.SUCCESS("VAT Proxy đã kết nối (tun0 lên).");
                        ProxyChangeLog.Write(serial, $"ConnectViaUi: tun0 LÊN sau ~{(i + 1) * 2}s -> kết nối proxy THÀNH CÔNG.");
                        return true;
                    }
                }

                _client.LogHelper.Log("VAT Proxy chưa lên tun0 sau khi nhập giao diện.");
                ProxyChangeLog.Write(serial, "ConnectViaUi: KHÔNG thấy tun0 sau 20s chờ -> kết nối proxy THẤT BẠI.");
                return false;
            }
            catch (Exception ex)
            {
                _client.LogHelper.Log($"Lỗi nhập giao diện VAT Proxy: {ex.Message}");
                ProxyChangeLog.Write(serial, $"ConnectViaUi: lỗi ngoài dự kiến: {ex.Message}");
                return false;
            }
        }

        // Nhập một ô của giao diện VAT: đọc tâm ô từ dump -> tap thô bằng adb ->
        // XÁC NHẬN ô đã focus (focused="true") rồi mới gõ phím. Trả false nếu 2 lần
        // thử vẫn không focus được ô.
        //
        // Vì sao phải kiểm tra focus: gõ ADB_INPUT_B64 luôn gửi vào ô ĐANG focus; nếu
        // tap trượt (hoặc dump lệch) thì chữ gõ vào ô sai/không vào đâu cả. Và BACK
        // (keyevent 4) chỉ ĐÓNG bàn phím khi đang có ô focus + bàn phím hiện; nếu
        // không ô nào focus thì BACK ĐÓNG LUÔN ProxyActivity -> launcher.
        private bool InputField(string resourceId, string value, string xmlSource)
        {
            string serial = _client.Device?.Serial ?? "?";
            string name = resourceId.Substring(resourceId.LastIndexOf('/') + 1);
            string dump = xmlSource ?? string.Empty;

            for (int attempt = 0; attempt < 2; attempt++)
            {
                (int x, int y) = GetNodeCenter(dump, resourceId);
                if (x >= 0)
                {
                    ProcessHelper.RunAdbCommand($"-s {serial} shell input tap {x} {y}", 10);
                    Thread.Sleep(1200);

                    string check = _client.GetXMLSource();
                    if (IsNodeFocused(check, resourceId))
                    {
                        // Ô đã focus: clear rồi gõ qua broadcast base64 (ADB_INPUT_B64),
                        // timeout cứng thay vì keyboard.Input/_service.Shell. Base64 giữ
                        // nguyên dấu '=' và ':' trong mật khẩu proxy.
                        ProcessHelper.RunAdbCommand($"-s {serial} shell am broadcast -a ADB_CLEAR_TEXT", 10);
                        string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
                        string typeOut = ProcessHelper.RunAdbCommand(
                            $"-s {serial} shell am broadcast -a ADB_INPUT_B64 --es msg '{b64}'", 10) ?? string.Empty;
                        bool typed = typeOut.Contains("result=0", StringComparison.OrdinalIgnoreCase);

                        // BACK ở đây AN TOÀN: bàn phím đang hiện -> Android chỉ đóng bàn
                        // phím, ProxyActivity vẫn sống (đã kiểm chứng trên device). Đóng
                        // IME còn giúp lần tap ô KẾ TIẾP focus được (focus không tự nhảy
                        // giữa các ô khi IME đang hiện).
                        ProcessHelper.RunAdbCommand($"-s {serial} shell input keyevent 4", 10);
                        Thread.Sleep(500);

                        if (typed)
                            return true;

                        ProxyChangeLog.Write(serial, $"InputField({name}): focus OK nhưng broadcast gõ KHÔNG trả result=0.");
                        continue; // thử lại lần nữa
                    }

                    ProxyChangeLog.Write(serial, $"InputField({name}): tap ({x},{y}) lần {attempt + 1} nhưng ô CHƯA focus.");
                }
                else
                {
                    ProxyChangeLog.Write(serial, $"InputField({name}): dump KHÔNG chứa {name} (lần {attempt + 1}).");
                }

                // Dump lại cho lần thử sau (UI có thể đã dịch chuyển).
                dump = _client.GetXMLSource() ?? string.Empty;
            }

            return false;
        }

        // Đọc tâm node có resource-id từ chuỗi dump (bounds dạng [x1,y1][x2,y2]).
        private static (int x, int y) GetNodeCenter(string xmlSource, string resourceId)
        {
            if (string.IsNullOrEmpty(xmlSource))
                return (-1, -1);

            var m = System.Text.RegularExpressions.Regex.Match(
                xmlSource,
                System.Text.RegularExpressions.Regex.Escape($"resource-id=\"{resourceId}\"")
                    + "[^>]*bounds=\"\\[(\\d+),(\\d+)\\]\\[(\\d+),(\\d+)\\]\"");
            if (!m.Success)
                return (-1, -1);

            int cx = (int.Parse(m.Groups[1].Value) + int.Parse(m.Groups[3].Value)) / 2;
            int cy = (int.Parse(m.Groups[2].Value) + int.Parse(m.Groups[4].Value)) / 2;
            return (cx, cy);
        }

        // Kiểm tra node có resource-id này đang focused="true" không. Trong dump của
        // ATX, resource-id và focused KHÔNG kề nhau (ngăn cách bởi class/package/...
        // focusable) nên phải match cả node bằng regex, không dùng Contains kề nhau.
        private static bool IsNodeFocused(string xmlSource, string resourceId)
        {
            if (string.IsNullOrEmpty(xmlSource))
                return false;

            return System.Text.RegularExpressions.Regex.IsMatch(
                xmlSource,
                "<node[^>]*" + System.Text.RegularExpressions.Regex.Escape($"resource-id=\"{resourceId}\"")
                    + "[^>]*focused=\"true\"");
        }

        private bool IsNumber(string str)
        {
            return int.TryParse(str, out _); // Kiểm tra chuỗi có phải là số nguyên hay không
        }
    }
}
