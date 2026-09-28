using AutoAndroid;
using Sunny.Subd.Core.Email;
using Sunny.Subd.Core.Gmail;
using Sunny.Subd.Core.Models;
using Sunny.Subd.Core.Phone;
using Sunny.Subd.Core.Proxies;
using Sunny.Subd.Core.Telegram;
using Sunny.Subd.Core.Utils;
using Sunny.Subdy.Common;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Common.Services;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Sunny.Subd.Core.Facebook
{
    // ─────────────────────────────────────────────────────────────────────────────
    // RegFacebookRegsiner — module "Reg Facebook" tách riêng (kiểu Pandora).
    //
    // Đây là BẢN SAO của FacebookRegsiner với các khác biệt có chủ đích, để KHÔNG
    // đụng chạm gì đến luồng chạy Facebook gốc (FacebookRegsiner.cs giữ nguyên):
    //
    //   1. _platform LUÔN = PlatformModel.Facebook cho MỌI thao tác ứng dụng
    //      (package com.facebook.katana, FacebookHander, các switch(_platform),
    //      BackupFacebook). Nhờ vậy ba switch `case PlatformModel.Facebook:` không
    //      bị no-op và đường chạy app giống hệt bản gốc.
    //   2. _regLabel = "Reg Facebook" CHỈ dùng để cách ly:
    //         - DB: _account.Platformt = _regLabel  (acc hiện ở mục Reg Facebook)
    //         - thư mục backup mặc định: Backup/Profile|Device/<_regLabel>
    //      Khóa config do caller (ucdgvAccount) đọc từ `fSettingRegFacebook_Reg Facebook`.
    //   3. Đổi proxy dùng com.scheler.superproxy (SuperProxyService) giống gmailus.py,
    //      thay cho VATProxy/com.vat.proxyconnector của bản gốc.
    //   4. Đổi thiết bị GIỮ nguyên của mình: _client.ChangInfo(...) (MaxChangeService).
    //   5. Nguồn mail/OTP: shopmailmmo (ShopMailMmoService) khi bật trong form riêng;
    //      funotp vẫn đi qua _phoneService sẵn có (comboBox2 = FunOTP, type = Số điện thoại).
    // ─────────────────────────────────────────────────────────────────────────────
    public class RegFacebookRegsiner
    {
        // Các trường dữ liệu private
        public readonly ADBClient _client;
        public Account _account;
        public readonly ConfigModel _config;
        public readonly CancellationToken _ct;
        public readonly AccountContext _accountContext = new();

        // NHÃN ỨNG DỤNG: luôn là Facebook để mọi thao tác app nhắm com.facebook.katana.
        public readonly string _platform;
        // NHÃN MODULE: "Reg Facebook" — chỉ dùng cho DB Platformt + thư mục backup.
        public readonly string _regLabel;

        public readonly Stopwatch _stopwatch = new();
        public int Timeout_Script = 0;
        public JsonHelper _settingGeneral;
        public string _sate = string.Empty;
        public Stopwatch _swTotal = new Stopwatch();
        private BackupRestoreHelper _backupRestoreHelper;
        private readonly string _typeRegister;
        private int _indexGmail = 0;
        private int _recoGmail = 0;
        private GmailService _gmailService;
        private readonly PhoneService _phoneService;
        private readonly EmailService _emailService;

        // Nguồn shopmailmmo (mail + OTP) cho module Reg Facebook (decision #3).
        // KHÔNG nằm trong RegistrationType.EmailTypes (bẫy đọc theo chỉ số) — bật/tắt
        // bằng checkBox riêng trong fSettingRegFacebook, gọi thẳng service.
        private readonly bool _useShopMailMmo;
        private readonly ShopMailMmoService _shopMailMmo;

        private bool _isNVR = false;
        private readonly int _timeOut;

        // Mốc chống BẤM LẶP cho nhánh "I agree" (màn "Agree to Facebook's terms and policies"
        // — FbExperimentalLoggedOutBloksActivity). Sau khi bấm nút thật, FB cần vài giây đổi
        // màn; nếu vòng lặp quét lại thấy node cũ và bấm tiếp vào nút đã MỜ thì FB chỉ báo
        // "We couldn't create an account for you". Vì vậy chỉ bấm lại khi đã qua COOLDOWN.
        // Dùng Environment.TickCount (không phải cờ bool) để TỰ HỒI PHỤC: instance được dùng
        // lại cho lần đăng ký sau vẫn bấm được, không cần reset thủ công.
        private int _iAgreeLastClickTick = 0;
        private const int IAgreeClickCooldownMs = 15000;

        // Số lần đã bấm "I agree" cho TÀI KHOẢN HIỆN TẠI. Có những acc Facebook chặn hẳn ở
        // bước này: nút vẫn xanh, vẫn enabled, tap (kể cả tap TAY) không có tác dụng gì và
        // không hiện thông báo lỗi nào — xác minh LIVE 2026-09-26 trên 5200d7ad5a6315d5
        // (acc #4: dump cho thấy Button desc="I agree" clickable=true enabled=true, tap tay
        // tại (720,2235) cũng KHÔNG đổi màn; trong khi tap nút "Learn more" cùng màn thì ăn
        // ngay → tap không hỏng, chính nút đó bị FB vô hiệu). Với acc như vậy, vòng lặp cũ
        // quét lại "I agree" cho tới hết _timeOut (30 phút) trong khi MAIL ĐÃ HẾT HẠN.
        // Bỏ acc sau IAgreeMaxAttempts lần bấm để vòng ngoài sang tài khoản khác.
        //
        // NGƯỠNG PHẢI RỘNG: đo được trong lần chạy thử đầu→cuối 2026-09-26 (3 acc đầu đều
        // THÀNH CÔNG) số lần bấm cho acc TỐT là 2 / 2 / 4 — acc #3 cần tới 4 lần mới qua
        // (Facebook hiện animation rồi mới chuyển màn, mỗi lần bấm lại cách nhau ≥15s cooldown).
        // Đặt 3 là bỏ oan acc #3. Lấy 6 = gấp rưỡi mức quan sát được, vẫn cắt rất sớm so với
        // 30 phút timeout, mà acc bị chặn cứng thì bấm 6 lần cũng không bao giờ qua.
        private int _iAgreeAttempts = 0;
        private const int IAgreeMaxAttempts = 6;

        // Cờ: đã nhập MÃ XÁC NHẬN MAIL cho tài khoản hiện tại hay chưa (HandleConfirmationCode
        // bật = true ngay khi bắt đầu nhập). Dùng cho TrySaveCheckpointAccountAsync: acc đã
        // nhập mã mail (tức đã có uid/pass thật trên server FB) nhưng kẹt ở CP_282/lỗi/kẹt màn
        // thì vẫn PHẢI lưu thông tin + backup — theo yêu cầu của user.
        private bool _mailCodeEntered = false;

        // Bộ đếm cho màn "How old are you?" (biến thể NHẬP TUỔI TRỰC TIẾP — KHÔNG có trong
        // XpathManagerFacebook). Nếu để rơi vào case NavigationButton "Next" chung thì bấm Next
        // với ô tuổi RỖNG → "Input Age is invalid." → lặp vô ích tới hết timeout. Xem
        // TryHandleAgeInput. Yêu cầu user 2026-09-28: tuổi PHẢI > 33 (nhập 34-50).
        private int _ageScreenAttempts = 0;
        private int _ageScreenLastClickTick = 0;
        private const int AgeScreenClickCooldownMs = 15000;
        private const int AgeScreenMaxAttempts = 4;

        // ─────────────────────────────────────────────────────────────────────────
        // NGÀY SINH > 33 TUỔI (2026-09-28) — bản v36.
        //
        // Bản v35 tua bánh xe ngày sinh bằng `Swipe(..., RandomValue(7, 12), n)` — thời
        // lượng 7-12ms là vùng FLING: quán tính cuốn số bước KHÔNG đoán được, và code cũ
        // KHÔNG hề đọc lại giá trị sau khi tua, `elementsDate.Count != 3` thì `return` IM
        // LẶNG rồi vẫn bấm SET với ngày mặc định → tuổi ≤ 33 → màn "How old are you?" →
        // nhập 34-51 không khớp ngày sinh → 4 lần → bỏ acc.
        //
        // Bản v36 đọc 3 bánh xe từ XML MỚI, xác định giá trị đích, rồi TUA TỪNG BƯỚC có
        // XÁC MINH: mỗi bước bấm hàng kề (android.widget.Button không có resource-id) —
        // bấm hàng DƯỚI = +1, bấm hàng TRÊN = -1 — rồi dump lại đọc giá trị thật. Tap không
        // ăn thì mới lùi về vuốt CHẬM 240-330ms (dưới ngưỡng fling). CHỈ bấm SET khi CẢ 3
        // giá trị đã khớp.
        // ─────────────────────────────────────────────────────────────────────────
        private int _dobReopenAttempts = 0;
        // Ngân sách thử cho việc đặt ngày sinh của MỘT acc. Mỗi CHU KỲ thất bại tiêu 2 đơn vị
        // (1 cho lần bấm mở lại picker ở HandleDateOfBirth + 1 cho lượt chỉnh trong picker ở
        // HandleDatePicker) → 6 đơn vị = 3 chu kỳ. Cạn ngân sách thì bỏ acc, KHÔNG để vòng lặp
        // quét lại picker tới hết _timeOut 30 phút. Bình thường chỉ tốn 1 đơn vị vì lượt đầu
        // đã đặt đúng (khi đó HandleDateOfBirth không bấm gì nên không tiêu gì).
        private const int DobReopenMaxAttempts = 6;

        // Đo LIVE 2026-09-28 trên 5200d7ad5a6315d5: ô input [314,1176][538,1344] cao 168,
        // hàng kề trên [314,945][538,1176] và dưới [314,1344][538,1575] cao ~231 → tâm hàng
        // kề cách MÉP ô input ~115px. Dùng để bấm đúng hàng kề theo hình học.
        private const int DobWheelNeighborOffset = 115;

        private static readonly string[] DobMonths =
        {
            "Jan", "Feb", "Mar", "Apr", "May", "Jun",
            "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
        };

        // Một bánh xe của NumberPicker. Toạ độ đọc lại từ XML mỗi lần nên luôn là dữ liệu mới.
        private sealed class DobWheel
        {
            public string Kind = "";   // "month" | "day" | "year"
            public int Value = 0;      // 1..12 | 1..31 | năm
            public int XCenter = 0;
            public int Top = 0;
            public int Bottom = 0;
        }

        public RegFacebookRegsiner(string platform, ADBClient device, ConfigModel config, CancellationToken ct)
        {
            _client = device ?? throw new ArgumentNullException(nameof(device));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // QUAN TRỌNG: bỏ qua nhãn truyền vào — app LUÔN chạy Facebook. Nhãn module
            // lấy từ tham số (caller truyền PlatformModel.RegFacebook) chỉ để cách ly DB/backup.
            _platform = PlatformModel.Facebook;
            _regLabel = string.IsNullOrWhiteSpace(platform) ? PlatformModel.RegFacebook : platform;

            _ct = ct;
            _settingGeneral = config.SettingGeneral;
            _backupRestoreHelper = new BackupRestoreHelper(_client.Device);
            int index = _settingGeneral.GetIntType("comboBox1", 0);
            _typeRegister = RegistrationType.RegFacebook_AllTypes[index];

            string sitePhone = RegistrationType.PhoneNumberTypes[_settingGeneral.GetIntType("comboBox2", 0)];
            string tokenPhone = _settingGeneral.GetValuesFromInputString("textBox5", string.Empty).Trim();
            _phoneService = new PhoneService(sitePhone, tokenPhone);

            string siteEmail = RegistrationType.EmailTypes[_settingGeneral.GetIntType("cbb_Email", 0)];
            _emailService = new EmailService(siteEmail);
            _timeOut = _settingGeneral.GetIntType("numericUpDown4", 30) * 60000;
            _isNVR = _settingGeneral.GetBooleanValue("checkBox9");

            // shopmailmmo: bật bằng checkBox riêng, API key từ textBox riêng của form.
            _useShopMailMmo = _settingGeneral.GetBooleanValue("checkBoxShopMailMmo", false);
            string shopMailKey = _settingGeneral.GetValuesFromInputString("textBoxShopMailMmoKey", string.Empty).Trim();
            _shopMailMmo = new ShopMailMmoService(shopMailKey);
        }

        public event EventHandler<Account> AccountAdded;
        private void OnAccountAdded(Account acc)
        {
            AccountAdded?.Invoke(this, acc);
        }

        // Phương thức trì hoãn với thông báo trạng thái
        public async Task DelayMessageAsync(int second, string message, int color)
        {
            for (int i = 1; i <= second; i++)
            {
                SetStatus($"[{i}/{second}]... {message}", color);
                await Task.Delay(1000);
            }
        }

        public void SetStatus(string status, int color, string logDetail = null)
        {
            if (!string.IsNullOrEmpty(_sate) && !status.Contains(_sate))
            {
                status = $"[{_sate}] - ({status})";
            }
            if (!string.IsNullOrEmpty(logDetail))
            {
                LogManager.Info($"[DEBUG] {logDetail}");
            }
            if (_account != null)
            {
                _account.Status = status;
                _account.RecentInteraction = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                _account.ColorType = color;
            }
            if (_client?.Device != null)
            {
                _client.Device.Status = status;
                _client.Device.TypeColor = color;
            }
        }

        public async Task Stop()
        {
            if (_ct.IsCancellationRequested)
            {
                throw new OperationCanceledException("Bạn đã dừng tài khoản.");
            }
            if (Timeout_Script != 0 && _stopwatch.IsRunning && _stopwatch.ElapsedMilliseconds > Timeout_Script)
            {
                _stopwatch.Restart();
                SetStatus("Đã quá thời gian thực hiện thao tác, dừng tài khoản.", 1);
                throw new TimeoutException("Đã quá thời gian thực hiện thao tác.");
            }
        }

        private async Task ExtractAndUpdateAuthenticationInfoAsync()
        {
            if (!_client.IsRoot()) return;
            // "Lưu cookie, token (root)" (checkBox10): khi bỏ tích thì KHÔNG ghi Cookie/Token.
            // Uid luôn phải lấy (account đăng ký mới) nên vẫn gọi GetAuthenticationInfo.
            bool saveCookieToken = _settingGeneral.GetBooleanValue("checkBox10", true);
            switch (_platform)
            {
                case PlatformModel.Facebook:
                    {
                        string value = FacebookHander.GetAuthenticationInfo(_client);
                        if (string.IsNullOrWhiteSpace(value))
                            throw new Exception("Không thể lấy thông tin xác thực.");

                        var parts = value.Split('|');
                        if (parts.Length < 3)
                            throw new Exception("Chuỗi xác thực không hợp lệ.");

                        _account.Uid = parts[0];
                        if (saveCookieToken)
                        {
                            _account.Cookie = parts[2];
                            _account.Token = parts[1];
                        }

                        break;
                    }
            }
            if (_settingGeneral.GetBooleanValue("checkBox3", true))
            {
                string profileDir = _settingGeneral.GetValuesFromInputString("textBox3", Path.Combine(AppContext.BaseDirectory, "Backup", "Profile", _regLabel));
                Directory.CreateDirectory(profileDir);
                string fileProfile = Path.Combine(profileDir, $"{_account.Uid}.tar.gz");
                switch (_platform)
                {
                    case PlatformModel.Facebook:
                        {
                            _backupRestoreHelper.BackupFacebook(fileProfile);
                            break;
                        }
                    case "Instagram":
                        {
                            _backupRestoreHelper.BackupInstagram(fileProfile);
                            break;
                        }
                }
            }
            if (_settingGeneral.GetBooleanValue("checkBox2", true))
            {
                string profileDir = _settingGeneral.GetValuesFromInputString("textBox2", Path.Combine(AppContext.BaseDirectory, "Backup", "Device", _regLabel));
                Directory.CreateDirectory(profileDir);
                string fileProfile = Path.Combine(profileDir, $"{_account.Uid}.tar.gz");
                await _client.BackupDevice(fileProfile);
            }
            _account.State = "LIVE";
            _accountContext.Add(_account);
        }

        /// <summary>
        /// LƯU ACC BỊ CHECKPOINT/KHÓA/KẸT: gọi trong nhánh catch của RunAsync khi acc ĐÃ nhập
        /// mã xác nhận mail (_mailCodeEntered = true — tức uid/pass thật đã tồn tại trên server
        /// FB) nhưng kẹt CP_282/lỗi/màn nào đó. Yêu cầu user 2026-09-28: "miễn sao khi nhập mã
        /// mail vào có uid pass rồi sẽ lưu lại thông tin và backup file lại".
        ///
        /// Khác ExtractAndUpdateAuthenticationInfoAsync:
        /// - KHÔNG ném lỗi, KHÔNG đổi State (giữ State thật như CP_282/DIE do HanderCase đã gán)
        /// - KHÔNG Add (finally của RunAsync đã Update — upsert — nên chỉ cần set field)
        /// - Không ghi đè Cookie/Token/Uid đã có bằng giá trị rỗng.
        /// Toàn thân bọc try/catch im lặng để không bao giờ giết vòng lặp chính.
        ///
        private async Task TrySaveCheckpointAccountAsync()
        {
            try
            {
                if (!_client.IsRoot()) return;
                if (_account == null || string.IsNullOrWhiteSpace(_account.Uid) == false) return;

                string value = FacebookHander.GetAuthenticationInfo(_client);
                if (string.IsNullOrWhiteSpace(value))
                {
                    // Phiên 282/pending có thể CHƯA ghi session vào file auth của app FB
                    // → đọc rỗng là HỢP LỆ, không phải lỗi. Bỏ qua cho acc này.
                    _client.LogHelper.Log("[REGFB-AUTH] acc kẹt nhưng file auth FB rỗng/ chưa có session — bỏ qua lưu cookie/token.");
                    return;
                }
                var parts = value.Split('|');
                if (parts.Length < 3)
                {
                    _client.LogHelper.Log("[REGFB-AUTH] chuỗi auth không hợp lệ — bỏ qua lưu.");
                    return;
                }

                _account.Uid = parts[0];
                if (_settingGeneral.GetBooleanValue("checkBox10", true))
                {
                    if (!string.IsNullOrWhiteSpace(parts[2])) _account.Cookie = parts[2];
                    if (!string.IsNullOrWhiteSpace(parts[1])) _account.Token = parts[1];
                }
                _client.LogHelper.SUCCESS($"[REGFB-AUTH] acc kẹt ({_account.State}) vẫn còn auth — đã lấy uid {_account.Uid}.");

                // Backup profile + device theo đúng gate của bản lưu thường (checkBox3/checkBox2).
                if (_settingGeneral.GetBooleanValue("checkBox3", true))
                {
                    string profileDir = _settingGeneral.GetValuesFromInputString("textBox3", Path.Combine(AppContext.BaseDirectory, "Backup", "Profile", _regLabel));
                    Directory.CreateDirectory(profileDir);
                    _backupRestoreHelper.BackupFacebook(Path.Combine(profileDir, $"{_account.Uid}.tar.gz"));
                }
                if (_settingGeneral.GetBooleanValue("checkBox2", true))
                {
                    string deviceDir = _settingGeneral.GetValuesFromInputString("textBox2", Path.Combine(AppContext.BaseDirectory, "Backup", "Device", _regLabel));
                    Directory.CreateDirectory(deviceDir);
                    await _client.BackupDevice(Path.Combine(deviceDir, $"{_account.Uid}.tar.gz"));
                }
                // KHÔNG set State="LIVE", KHÔNG Add — finally của RunAsync Update (upsert) tự lưu.
            }
            catch (Exception exAuth)
            {
                // Im lặng theo chủ đích: việc lưu acc kẹt KHÔNG được phép làm hỏng luồng chính.
                _client.LogHelper.ERROR($"[REGFB-AUTH] lưu acc kẹt thất bại (bỏ qua): {exAuth.Message}");
            }
        }

        // Kết quả bước đổi thiết bị của account hiện tại (ô trạng thái UI chỉ giữ dòng
        // cuối cùng nên lưu lại để nhúng vào dòng trạng thái của bước đổi proxy).
        private string _lastDeviceChange = string.Empty;

        // Thay đổi thông tin thiết bị — GIỮ NGUYÊN của mình (MaxChangeService qua ChangInfo).
        // Luôn chạy và LUÔN chạy trước đổi proxy.
        private async Task ChangeInfoAsync()
        {
            _sate = "Thay đổi thông tin thiết bị";
            AutoAndroid.DeviceChangeLog.Write(_client.Device?.Serial ?? "?",
                $"[{AutoAndroid.DeviceChangeLog.BuildTag}] RegFacebookRegsiner: BẮT ĐẦU (uid={_account?.Uid}).");
            _client.LogHelper.SUCCESS(">>> BƯỚC ĐỔI THIẾT BỊ (chạy trước đổi proxy)");
            _client.LogHelper.State = _sate;
            try
            {
                SetStatus("Đang thay đổi thông tin thiết bị...", 2);
                string filezip = string.Empty;
                List<string> brands = _settingGeneral.GetValuesFromInputString("textBox1", DeviceServices.Brands).Split('|').ToList();
                bool backup = _settingGeneral.GetBooleanValue("checkBox2", true);
                if (backup)
                {
                    string profileDir = _settingGeneral.GetValuesFromInputString("textBox2", Path.Combine(AppContext.BaseDirectory, "Backup", "Device", _regLabel));
                    profileDir = Path.Combine(profileDir);
                    Directory.CreateDirectory(profileDir);
                    // Acc MỚI chưa có uid (GetAccount chưa gán — uid chỉ có sau khi reg xong).
                    // Nếu vẫn nối path thì filename chỉ còn ".tar.gz" — trùng file rác cũ chứa
                    // fingerprint của một acc khác, Change() sẽ Restore nó ra => KHÔNG sinh
                    // thiết bị mới. Giữ filezip rỗng để acc mới LUÔN GenerateNewDevice.
                    // Acc đã có uid (checkpoint v35) vẫn nạp lại đúng profile của chính nó.
                    if (!string.IsNullOrEmpty(_account?.Uid))
                    {
                        filezip = Path.Combine(profileDir, $"{_account.Uid}.tar.gz");
                    }
                }
                if (await _client.ChangInfo(filezip, backup, "", "VN"))
                {
                    _lastDeviceChange = $"Thiết bị OK [{_client.GetDeviceName()}]";
                    SetStatus($"Thành công. [{_client.GetDeviceName()}]", 2);
                }
                else
                {
                    _lastDeviceChange = $"Thiết bị LỖI [{_client.GetDeviceName()}]";
                    SetStatus($"Thất bại. [{_client.GetDeviceName()}]", 1);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                _lastDeviceChange = $"Thiết bị LỖI NGOẠI LỆ: {ex.Message}";
                SetStatus(ex.Message, 1);
            }
        }

        // Thay đổi proxy — dùng com.scheler.superproxy (SuperProxyService) giống gmailus.py.
        // Thay hoàn toàn cho VATProxy/com.vat.proxyconnector của FacebookRegsiner gốc.
        //
        // ĐỔI IP MỖI ACC (user yêu cầu 26-09): công tắc trong app là ON/OFF của cùng một
        // cấu hình proxy, KHÔNG tự cấp IP mới. Nếu chỉ "bật nếu đang tắt" thì từ acc thứ 2
        // trở đi mọi acc đi chung một IP ra → Facebook chặn đăng ký (đúng hiện tượng acc #4
        // bị kẹt ở màn "I agree"). Vì vậy MỖI acc đều gọi RestartAsync() = tắt proxy rồi
        // bật lại để xin endpoint mới. RestartAsync tự xử lý cả trường hợp proxy đang tắt
        // (chỉ bật) nên gọi cho acc đầu tiên cũng đúng — acc đầu cũng phải có IP mới, không
        // dùng lại IP còn sót của phiên chạy trước.
        // Đo LIVE 2026-09-26/27 trên 5200d7ad5a6315d5, 3 lần liên tiếp:
        //   57.138.33.237 (proxy tắt) → 104.61.181.235 → 97.248.39.169 → 38.62.129.202
        //   — IP đổi thật sau mỗi lần tắt/bật, không lần nào trùng.
        //
        // LƯU Ý: bản Python gốc KHÔNG làm việc này (clear_superproxy / stop_superproxy /
        // reset_network_on_device định nghĩa nhưng không gọi; chỗ reset IP trong auto_loop
        // chỉ là stub in thông báo). Đây là cải tiến theo yêu cầu, không phải bug port thiếu.
        private async Task ChangeProxyAsync()
        {
            _sate = "Thay đổi IP/Proxy (SuperProxy)";
            SetStatus("Đang tắt/bật lại proxy com.scheler.superproxy để đổi IP...", 2);
            bool ok = false;
            string ipBefore = string.Empty;
            try
            {
                ipBefore = await _client.GetIp();
                var superProxy = new SuperProxyService(_client);
                ok = await superProxy.RestartAsync();
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                _client.LogHelper?.ERROR($"[SuperProxy] lỗi: {ex.Message}");
            }

            // Đọc lại IP sau khi bật để biết proxy có thật sự ra mạng ngoài hay không.
            string ipAfter = string.Empty;
            try { ipAfter = await _client.GetIp(); } catch { }

            string ipNote = string.IsNullOrEmpty(ipAfter)
                ? "IP: (không đọc được)"
                : (string.IsNullOrEmpty(ipBefore) || ipBefore == ipAfter
                    ? $"IP: {ipAfter}"
                    : $"IP: {ipBefore} → {ipAfter}");

            SetStatus(ok
                ? $"Đổi IP proxy thành công - {ipNote} - {_lastDeviceChange}"
                : $"Đổi IP proxy THẤT BẠI - {ipNote} - {_lastDeviceChange}", ok ? 2 : 1);
            _client.LogHelper?.Log($"[SuperProxy] {(ok ? "OK" : "THẤT BẠI")} - {ipNote}");

            int timeDelay = _settingGeneral.GetIntType("numericUpDown3", 10);
            await DelayMessageAsync(timeDelay, "Delay sau khi bật proxy.", 2);
        }

        // Kiểm tra kết nối internet
        private async Task<bool> IsInternetAsync()
        {
            _sate = "Kiểm tra kết nối internet";
            int attempts = _settingGeneral.GetIntType("nud_IndexFailProxy", 5);
            for (int i = 1; i <= attempts; i++)
            {
                string ip = await _client.GetIp();
                if (string.IsNullOrEmpty(ip))
                {
                    SetStatus($"[{i}/{attempts}] Không có internet.", 1);
                    await _client.DisableWifi();
                    await Task.Delay(5000);
                    await _client.EnableWifi();
                    await Task.Delay(15000);
                    continue;
                }
                SetStatus($"[{i}/{attempts}] IP:[{ip}].", 2);
                if (_account != null)
                {
                    _account.IP = ip;
                    _account.Serial = $"[{_client.Device.NameDevice} - {_client.Device.Serial}]";
                }
                return true;
            }
            return false;
        }

        // Mở ứng dụng Facebook
        private async Task<bool> OpenFacebookAsync()
        {
            _sate = $"Mở ứng dụng {_platform}";
            string fileAPK = string.Empty;
            if (_settingGeneral.GetBooleanValue("checkBox8", true))
            {
                fileAPK = _settingGeneral.GetValuesFromInputString("textBox4", FacebookHander.FilePath(_platform));
            }
            for (int i = 1; i <= 10; i++)
            {
                SetStatus($"[{i}/10] Đang khởi động ứng dụng...", 2);
                _client.AppStart(FacebookHander.Package(_platform), true, true, true);
                if (_client.ElementWithAttributes($"//*[@text=\"{_platform} keeps stopping\"]", 5, click: false))
                {
                    SetStatus($"[{i}/{10}] Bị crash. Cài lại ứng dụng {_platform}.", 1);
                    if (!File.Exists(fileAPK))
                    {
                        SetStatus($"[{i}/{10}] Bị crash. Cài lại ứng dụng {_platform}. Không tìm thấy apk [{fileAPK}]", 1);
                        return false;
                    }
                    _client.UninstallApp(FacebookHander.Package(_platform));
                    _client.InstallApp(fileAPK);
                    continue;
                }
                if (_client.AppWait(FacebookHander.Package(_platform))) return true;
            }
            return _client.AppWait(FacebookHander.Package(_platform));
        }

        // Kết nối và chuẩn bị thiết bị.
        // QUY TRÌNH REG (đúng thứ tự, khớp gmailus.py auto_loop → run_registration):
        //   1. Xóa data        (ClearAppData)
        //   2. Change device   (ChangeInfoAsync — dùng _account.Uid nên PHẢI chạy sau GetAccount)
        //   3. Bật SuperProxy  (ChangeProxyAsync — com.scheler.superproxy)
        //   4. Mở Facebook     (OpenFacebookAndSizeAsync) → reg
        // Phase 1 (changeProxy=false, chạy TRƯỚC GetAccount): CHỈ connect + kiểm tra internet.
        //   KHÔNG xóa data, KHÔNG mở Facebook — tránh mở FB trên thiết bị cũ/không proxy.
        // Phase 2 (changeProxy=true, chạy SAU GetAccount): đầy đủ 4 bước theo đúng thứ tự trên.
        private async Task<bool> ConnectAndPrepareDeviceAsync(bool changeProxy)
        {
            if (!await ConnectDeviceAsync())
                return false;

            // Phase 1 (changeProxy=false, chạy TRƯỚC GetAccount): CHỈ kết nối thiết bị rồi
            // trả true. KHÔNG kiểm tra internet ở đây — proxy còn chưa bật nên kiểm tra
            // sớm chỉ tổ bật/tắt wifi + đi tới change device oan mấy lần trước khi mở
            // Facebook (đúng bug user thấy 26-09: "kiểm tra ip với change device mấy lần
            // mới mở ứng dụng"). Việc kiểm tra mạng nằm ở phase 2, sau khi đã bật proxy.
            if (!changeProxy) return true;

            // 1. Xóa data dữ liệu
            ClearAppData();
            // 2. Change device
            await ChangeInfoAsync();
            // 3. Bật SuperProxy (vào app com.scheler.superproxy bấm start)
            await ChangeProxyAsync();
            // 4. Mở Facebook để bắt đầu reg acc
            await OpenFacebookAndSizeAsync();

            if (_settingGeneral.GetBooleanValue("checkBox4", false))
            {
                int retryCount = _settingGeneral.GetIntType("numericUpDown1", 1);
                bool triedJoinWifi = false;
                for (int i = 0; i < retryCount; i++)
                {
                    if (await IsInternetAsync())
                        return true;

                    if (!triedJoinWifi)
                    {
                        triedJoinWifi = true;
                        if (await TryJoinConfiguredWifiAsync() && await IsInternetAsync())
                            return true;
                    }
                }

                SetStatus($"Reboot khi mất mạng quá {retryCount} lần", 2);
                _client.RebootAndWaitForDeviceReady(
                    $"checkBox4 'Reboot khi mất mạng' = BẬT, đã thử {retryCount} lần không có internet (job ĐĂNG KÝ RegFacebookRegsiner).");
                return false;
            }

            if (await IsInternetAsync()) return true;

            return await TryJoinConfiguredWifiAsync() && await IsInternetAsync();
        }

        private async Task<bool> TryJoinConfiguredWifiAsync()
        {
            try
            {
                var serial = _client.Device?.Serial;
                if (string.IsNullOrEmpty(serial)) return false;
                var cred = WifiCredentialsStore.GetBySerial(serial);
                if (cred == null || string.IsNullOrEmpty(cred.UserName)) return false;

                SetStatus($"Kết nối lại Wifi '{cred.UserName}'…", 2);
                var wifi = new AdbJoinWifiService(_client);
                bool sent = await wifi.ConnectToWifiNetwork(cred.UserName, cred.Password);
                if (!sent) return false;

                await Task.Delay(5000);
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Warning($"[TryJoinConfiguredWifi] {ex.Message}");
                return false;
            }
        }

        // Kết nối thiết bị
        private async Task<bool> ConnectDeviceAsync()
        {
            _sate = "Kết nối thiết bị";
            return _client.Connect();
        }

        // (1) XÓA DATA — ĐỒNG BỘ, tách riêng khỏi mở Facebook để chạy TRƯỚC đổi thiết bị
        // và bật proxy, đúng quy trình reg: xóa data → change device → super proxy → mở FB.
        private void ClearAppData()
        {
            _sate = "Xóa dữ liệu ứng dụng";
            var check = _settingGeneral.GetBooleanValue("checkBox12", false);
            if (check) return;

            List<string> packages = new List<string>();
            switch (_platform)
            {
                case PlatformModel.Facebook:
                    {
                        packages = new List<string>
                        {
                            "com.facebook.katana",
                            "com.facebook.lite",
                            "com.facebook.services",
                            "com.facebook.appmanager",
                            "com.facebook.system",
                            "com.facebook.systemservice",
                        };
                        break;
                    }
                case "Instagram":
                    {
                        packages.Add(FacebookHander.Package(_platform));
                        break;
                    }
            }

            SetStatus("Đang xóa cache ứng dụng cũ...", 2);
            foreach (var package in packages)
            {
                _client.AppClear(package);
            }

            SetStatus("Đang cấp quyền ứng dụng...", 2);
            _client.GrantAppPermissions(FacebookHander.Package(_platform));
        }

        // (4) MỞ FACEBOOK + set kích thước — chạy SAU CÙNG, chỉ khi thiết bị và proxy đã sẵn sàng.
        private async Task OpenFacebookAndSizeAsync()
        {
            if (!await OpenFacebookAsync()) throw new Exception($"Không thể mở {_platform}.");
            _client.SetSize();
        }

        // Kiểm tra trạng thái tài khoản
        private async Task<bool> HandleInitialLogin()
        {
            _sate = "Đăng nhập Gmail";
            switch (_typeRegister)
            {
                case RegistrationType.Gmail_BaitPhoneNumber:
                case RegistrationType.Gmail:
                    _client.StopApp(FacebookHander.Package(PlatformModel.Facebook));
                    return await LoginGmailAsync();
                default:
                    return true;
            }
        }

        // GmailService (dùng cho type Gmail / Gmail mồi số) CHỈ có ctor nhận
        // FacebookRegsiner để ghi trạng thái (_sate/SetStatus/DelayMessageAsync) và lấy
        // _client; ctor nhận ADBClient để facebook=null sẽ NullReference khi gọi các hàm đó.
        // Ta KHÔNG sửa GmailService.cs (thuộc đường chạy Facebook), nên tái dùng một
        // instance FacebookRegsiner làm "bồn trạng thái": KHÔNG bao giờ chạy (không
        // RunAsync), chỉ đồng bộ _account để SetStatus/DelayMessageAsync ghi đúng chỗ và
        // hiện đúng trên UI. _client/_config trùng với bản sao nên thao tác thiết bị y hệt.
        private FacebookRegsiner _gmailStatusSink;

        private GmailService BuildGmailService()
        {
            _gmailStatusSink ??= new FacebookRegsiner(_platform, _client, _config, _ct);
            _gmailStatusSink._account = _account; // cùng Account object -> status ghi đúng
            return new GmailService(_gmailStatusSink);
        }

        private async Task<bool> LoginGmailAsync()
        {
            if (Globals.Gmails.Count == 0) return false;
            _gmailService = BuildGmailService();
            if (_indexGmail >= _recoGmail)
            {
                await _gmailService.RemoveAccount();
                _indexGmail = 0;
            }

            string value;
            lock (Globals.Gmails)
            {
                value = Globals.Gmails[0];
                Globals.Gmails.RemoveAt(0);
            }

            if (string.IsNullOrEmpty(value)) return false;

            string[] parts = value.Split('|');
            string email = parts[0].Trim();
            string password = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            _account.Email = email;
            _account.PassMail = password;
            _indexGmail++;
            return await _gmailService.Login(email, password);
        }

        // Xử lý các trường hợp ngoại lệ
        private void HanderCase(Exception ex)
        {
            SubdyExtension subdyExtension = ex as SubdyExtension ?? new SubdyExtension(SubdyEnum.Error, ex.Message);
            switch (subdyExtension.SubdyEnum)
            {
                case SubdyEnum.Stop:
                    _account.Status = string.IsNullOrEmpty(subdyExtension.Message)
                        ? "Đã dừng lại."
                        : "Đã dừng: " + subdyExtension.Message;
                    break;
                case SubdyEnum.Error:
                    _account.Status = "Lỗi: " + subdyExtension.Message;
                    break;
                case SubdyEnum.CP_282:
                    _account.Status = string.IsNullOrEmpty(subdyExtension.Message)
                        ? "Tài khoản bị checkpoint 282."
                        : (subdyExtension.Message.Contains("checkpoint", StringComparison.OrdinalIgnoreCase) || subdyExtension.Message.Contains("282")
                            ? subdyExtension.Message
                            : "Lỗi CP_282: " + subdyExtension.Message);
                    _account.State = "CP_282";
                    _account.ColorType = 1;
                    break;
                case SubdyEnum.CP_956:
                    _account.Status = string.IsNullOrEmpty(subdyExtension.Message)
                        ? "Tài khoản bị checkpoint 956."
                        : (subdyExtension.Message.Contains("checkpoint", StringComparison.OrdinalIgnoreCase) || subdyExtension.Message.Contains("956")
                            ? subdyExtension.Message
                            : "Lỗi CP_956: " + subdyExtension.Message);
                    _account.State = "CP_956";
                    _account.ColorType = 1;
                    break;
                case SubdyEnum.LogOut:
                    _account.Status = string.IsNullOrEmpty(subdyExtension.Message)
                        ? "Tài khoản bị đăng xuất."
                        : (subdyExtension.Message.Contains("đăng xuất", StringComparison.OrdinalIgnoreCase)
                            ? subdyExtension.Message
                            : "Đăng xuất: " + subdyExtension.Message);
                    _account.State = "Logout";
                    _account.ColorType = 1;
                    break;
                case SubdyEnum.Captcha:
                    _account.Status = string.IsNullOrEmpty(subdyExtension.Message)
                        ? "Tài khoản bị yêu cầu captcha."
                        : (subdyExtension.Message.Contains("captcha", StringComparison.OrdinalIgnoreCase)
                            ? subdyExtension.Message
                            : "Captcha: " + subdyExtension.Message);
                    _account.State = "Captcha";
                    _account.ColorType = 1;
                    break;
                case SubdyEnum.Block:
                    _account.Status = string.IsNullOrEmpty(subdyExtension.Message)
                        ? "Tài khoản bị chặn."
                        : (subdyExtension.Message.Contains("bị chặn", StringComparison.OrdinalIgnoreCase)
                            ? subdyExtension.Message
                            : "Tài khoản bị chặn: " + subdyExtension.Message);
                    _account.State = "Block";
                    _account.ColorType = 1;
                    break;
                case SubdyEnum.DIE:
                    _account.Status = subdyExtension.Message;
                    _account.State = "DIE";
                    _account.ColorType = 1;
                    break;
            }
            new AccountContext().Update(_account);
        }

        private bool IsReboot()
        {
            bool isReboot = _settingGeneral.GetBooleanValue("checkBox5", false);
            if (!isReboot) return false;
            double timeout = _settingGeneral.GetIntType("numericUpDown2", 30) * 60000;
            if (timeout != 0 && _swTotal.IsRunning && _swTotal.ElapsedMilliseconds > timeout)
            {
                SetStatus($"Tự reboot sau {_settingGeneral.GetIntType("numericUpDown2", 30)} phút.", 2);
                int mins = _settingGeneral.GetIntType("numericUpDown2", 30);
                _client.RebootAndWaitForDeviceReady(
                    $"checkBox5 'Tự reboot sau N phút' = BẬT, đã chạy {mins} phút (job ĐĂNG KÝ RegFacebookRegsiner).");
                _stopwatch.Restart();
                return isReboot;
            }
            return false;
        }

        public async Task RunAsync()
        {
            _swTotal.Start();
            while (!_ct.IsCancellationRequested)
            {
                _account = null;

                if (IsReboot()) continue;

                _sate = "Kết nối thiết bị";
                if (!await ConnectAndPrepareDeviceAsync(false)) continue;

                _sate = "Tạo tài khoản mới";
                _account = GetAccount();

                if (_account == null) continue;
                OnAccountAdded(_account);
                _account.Running = true;
                try
                {
                    // ── CHẶN MÀN HÌNH ĐEN SUỐT VÒNG ACCOUNT (v12) ──────────────────────────
                    _client.MirrorGuardStart();

                    try { _client.maxChange?.RecoverFrameworkIfBlackScreenPublic(); }
                    catch (Exception exR)
                    {
                        AutoAndroid.DeviceChangeLog.Write(_client.Device?.Serial ?? "?",
                            $"[{AutoAndroid.DeviceChangeLog.BuildTag}] RegFacebookRegsiner: recovery đầu account lỗi (bỏ qua): {exR.Message}");
                    }

                    _sate = "Chuẩn bị thiết bị và proxy";
                    if (!await ConnectAndPrepareDeviceAsync(true)) continue;

                    _sate = "Đăng nhập Gmail";
                    if (!await HandleInitialLogin()) continue;

                    _sate = "Đăng ký tài khoản Facebook mới";
                    var message = await ImportInfo();

                    if (message.SubdyEnum == SubdyEnum.Success)
                    {
                        if (_settingGeneral.GetBooleanValue("checkBox11", true))
                        {
                            int second = SubdyHelper.RandomValue(_settingGeneral.GetIntType("numericUpDown25", 10), _settingGeneral.GetIntType("numericUpDown24", 20));
                            await DelayMessageAsync(second, "Delay sau khi đăng kí thành công.", 2);
                        }
                    }
                    _sate = "Lấy thông tin xác thực";
                    await ExtractAndUpdateAuthenticationInfoAsync();
                }
                catch (Exception ex)
                {
                    HanderCase(ex);
                    // Yêu cầu user 2026-09-28: acc ĐÃ nhập mã mail (uid/pass đã tồn tại trên
                    // server FB) nhưng bị checkpoint/khóa/kẹt thì vẫn PHẢI lưu thông tin + backup
                    // file. HanderCase đã gán State thật (CP_282/DIE/...) nên ở đây chỉ lấy auth
                    // + backup; bọc try/catch để không bao giờ giết finally của RunAsync.
                    if (_mailCodeEntered && _account != null && string.IsNullOrEmpty(_account.Uid))
                    {
                        try { await TrySaveCheckpointAccountAsync(); }
                        catch { }
                    }
                }
                finally
                {
                    _client.MirrorGuardStop();
                    if (_account != null)
                    {
                        _accountContext.Update(_account);
                        _account.Running = false;
                        if (TelegramBotServices.BotTelegram != null)
                        {
                            string id = _settingGeneral.GetValuesFromInputString("textBox8");
                            string message = $"[{DateTime.Now.ToString("dd-MM-yyyy")}] {_account.Uid} {_account.State}";
                            if (_settingGeneral.GetBooleanValue("radioButton7") && _account.State != "LIVE")
                            {
                                message = "";
                            }
                            if (!string.IsNullOrEmpty(message))
                            {
                                await TelegramBotServices.BotTelegram.SendMessageAsync(Convert.ToInt64(id), message);
                            }
                        }
                    }
                }
            }
            _stopwatch.Stop();
        }

        private Account GetAccount()
        {
            _account = new Account();
            // Bộ đếm chống kẹt "I agree" phải về 0 cho MỖI tài khoản — nếu không, acc thứ 4
            // trở đi sẽ bị bỏ oan vì thừa hưởng số lần bấm của acc trước.
            _iAgreeAttempts = 0;
            _iAgreeLastClickTick = 0;
            // Cùng lý do: 2 bộ đếm màn MỚI (passkey / lỗi FB) cũng phải về 0 cho mỗi acc.
            _passkeyCloseAttempts = 0;
            _passkeyLastClickTick = 0;
            _fbErrorRetryAttempts = 0;
            _fbErrorLastClickTick = 0;
            // Cùng lý do: cờ mã mail + bộ đếm màn "How old are you?" phải về 0 cho mỗi acc —
            // nếu không, acc sau sẽ thừa hưởng trạng thái của acc trước (cờ bật oan, hết lượt bấm).
            _mailCodeEntered = false;
            _ageScreenAttempts = 0;
            _ageScreenLastClickTick = 0;
            // Cùng lý do: ngân sách xử lý ngày sinh (mở lại picker + tua bánh xe) phải về 0.
            _dobReopenAttempts = 0;
            List<string> firstnames = GetFirstnames();
            List<string> lastnames = GetLastnames();
            _account.Password = GetPassword();
            _account.NameFolder = _config.JobService;
            _account.FullName = $"{SubdyHelper.GetStringRandom(firstnames)} {SubdyHelper.GetStringRandom(lastnames)}";
            // CÁCH LY DB: acc đăng ký ở module này gắn nhãn "Reg Facebook" để hiện
            // riêng trong mục Reg Facebook, không lẫn với acc Facebook thường.
            _account.Platformt = _regLabel;
            return _account;
        }

        private List<string> GetFirstnames()
        {
            if (_settingGeneral.GetBooleanValue("radioButton1", false))
                return SubdyHelper.FirstnameRandom;
            if (_settingGeneral.GetBooleanValue("radioButton3", false))
                return File.Exists(_settingGeneral.GetValuesFromInputString("txt_Ho", string.Empty))
                    ? File.ReadAllLines(_settingGeneral.GetValuesFromInputString("txt_Ho", string.Empty)).ToList()
                    : SubdyHelper.FirstnameVN;
            return SubdyHelper.FirstnameVN;
        }

        private string GetPassword()
        {
            if (_settingGeneral.GetBooleanValue("radioButton2", false) && !string.IsNullOrEmpty(_settingGeneral.GetValuesFromInputString("txtPass", "")))
                return _settingGeneral.GetValuesFromInputString("txtPass", "").Trim();
            return SubdyHelper.RandomPassword(SubdyHelper.RandomValue(7, 18), digit: false);
        }

        private List<string> GetLastnames()
        {
            if (_settingGeneral.GetBooleanValue("radioButton1", false))
                return SubdyHelper.LastnameRandom;
            if (_settingGeneral.GetBooleanValue("radioButton3", false))
                return File.Exists(_settingGeneral.GetValuesFromInputString("txt_Ten", string.Empty))
                    ? File.ReadAllLines(_settingGeneral.GetValuesFromInputString("txt_Ten", string.Empty)).ToList()
                    : SubdyHelper.LastnameVN;
            return SubdyHelper.LastnameVN;
        }

        private async Task<bool> EnsureAppRunning()
        {
            if (!_client.IsRunningApp(FacebookHander.Package(PlatformModel.Facebook)))
            {
                _client.AppStart(FacebookHander.Package(PlatformModel.Facebook), true, false, true);
                await DelayMessageAsync(5, _account.Status, 2);
                return false;
            }
            return true;
        }

        private bool IsConfirmationCase(string currentCase)
        {
            return currentCase == $"//*[contains(@text, \"confirm your account\") and contains(@text, \"{_account.Email}\")]" ||
                   currentCase == $"//*[contains(@text, \"confirm your account\") and contains(@text, \"{_account.Phone?.Split("|")[0]}\")]";
        }

        private async Task HandleConfirmationCode()
        {
            // Đã tới bước nhập MÃ XÁC NHẬN MAIL = tài khoản đã tạo được uid/pass thật trên
            // server FB. Bật cờ để RunAsync (nhánh catch) biết acc này cần được LƯU + BACKUP
            // kể cả khi sau đó kẹt CP_282 / lỗi / Stop — xem TrySaveCheckpointAccountAsync.
            _mailCodeEntered = true;
            await DelayMessageAsync(10, _account.Status, 2);
            string code = await GetCode();
            if (string.IsNullOrEmpty(code))
            {
                _client.LogHelper.ERROR("Không nhận được mã xác nhận.");
                throw new SubdyExtension(SubdyEnum.Stop, "Không nhận được mã xác nhận.");
            }

            if (_typeRegister == RegistrationType.Gmail_BaitPhoneNumber || _typeRegister == RegistrationType.Gmail)
            {
                _client.Shell("input keyevent KEYCODE_APP_SWITCH");
                _client.ElementWithAttributes("//*[@content-desc=\"Facebook\"]");
            }

            await DelayMessageAsync(2, _account.Status, 2);
            _client.SendTextADB("//*[@class=\"android.widget.EditText\"]", code, timeout: 10);
            _client.ElementWithAttributes(new List<string> { "//*[@text=\"Next\"]", "//*[@content-desc=\"Next\"]" }, 5);
            await DelayMessageAsync(10, _account.Status, 2);
        }

        private async Task HandleEmailInput()
        {
            if (!_client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), timeoutInSeconds: 1, click: false)) return;
            switch (_typeRegister)
            {
                case RegistrationType.Domain_BaitPhoneNumber:
                case RegistrationType.Gmail_BaitPhoneNumber:
                case RegistrationType.PhoneNumber:
                    _client.ElementWithAttributes(new List<string> { "//*[@text=\"Sign up with mobile number\"]" }, 5);
                    break;
                default:
                    await GetEmail();
                    if (string.IsNullOrEmpty(_account.Email)) return;
                    _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", _account.Email.Split('|')[0], timeout: 5);
                    var xpaths = XpathManagerFacebook.Get(XpathType.NavigationButton);
                    xpaths.Remove("//*[@content-desc=\"I already have an account\"]");
                    _client.ElementWithAttributes(xpaths, 5);
                    await DelayMessageAsync(10, _account.Status, 2);
                    break;
            }
        }

        private async Task HandlePhoneInput()
        {
            await DelayMessageAsync(5, _account.Status, 2);
            if (!_client.ElementWithAttributes(new List<string> { "//*[@text=\"What is your mobile number?\"]", "//*[@text=\"Sign up with email\"]" }, click: false) || !_client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), timeoutInSeconds: 1, click: false)) return;
            switch (_typeRegister)
            {
                case RegistrationType.Domain_BaitPhoneNumber:
                case RegistrationType.Gmail_BaitPhoneNumber:
                case RegistrationType.PhoneNumber:
                    await GetPhone();
                    if (string.IsNullOrEmpty(_account.Phone)) return;
                    _client.ElementWithAttributes(new List<string> { "//*[@text=\"Sign up with mobile number\"]" }, 5);

                    if (_account.Phone.Contains("ERROR"))
                    {
                        _client.LogHelper.ERROR(_account.Phone);
                        await Task.Delay(5000);
                        return;
                    }
                    string rawPhone = _account.Phone;
                    if (_account.Phone.Contains("|"))
                    {
                        rawPhone = _account.Phone.Split('|')[1].Trim();
                    }
                    if (!rawPhone.StartsWith("1") && !rawPhone.StartsWith("0") && !rawPhone.StartsWith("84") && !rawPhone.StartsWith("+84"))
                    {
                        rawPhone = "+84" + rawPhone;
                    }
                    else if (!rawPhone.StartsWith("1") && !rawPhone.StartsWith("0") && !rawPhone.StartsWith("+")) // Đã có 84 nhưng thiếu "+"
                    {
                        rawPhone = "+" + rawPhone;
                    }
                    else if (rawPhone.StartsWith("1") && !rawPhone.StartsWith("+")) // Đã có 84 nhưng thiếu "+"
                    {
                        rawPhone = "+" + rawPhone;
                    }

                    _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", rawPhone, timeout: 5);
                    var xpaths = XpathManagerFacebook.Get(XpathType.NavigationButton);
                    xpaths.Remove("//*[@content-desc=\"I already have an account\"]");
                    _client.ElementWithAttributes(xpaths, 5);
                    break;
                default:
                    _client.ElementWithAttributes(new List<string> { "//*[@text=\"Sign up with email\"]" }, 5);
                    break;
            }
        }

        private void HandleNameSelection()
        {
            if (!_client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), timeoutInSeconds: 1, click: false)) return;
            _client.ElementWithAttributes("//*[@class=\"android.widget.RadioButton\"]", 5);
            var xpaths = XpathManagerFacebook.Get(XpathType.NavigationButton);
            xpaths.Remove("//*[@content-desc=\"I already have an account\"]");
            _client.ElementWithAttributes(xpaths, 5);
        }

        private async Task HandleNameInput()
        {
            if (!_client.ElementWithAttributes("//*[@class=\"android.widget.EditText\"]", click: false) || !_client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), click: false)) return;

            bool swap = SubdyHelper.RandomValue(0, 2) == 1;
            string[] nameParts = _account.FullName.Split(' ');
            string firstName = nameParts[0];
            string lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";
            if (swap) (firstName, lastName) = (lastName, firstName);

            var elements = _client.FindElements(10, "", "//*[@class=\"android.widget.EditText\"]");
            if (!elements.Any()) return;

            if (SubdyHelper.RandomValue(0, 2) == 1)
            {
                _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", firstName, timeout: 5, xml: elements[0].OuterXml);
                if (elements.Count > 1)
                    _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", lastName, timeout: 5, xml: elements[1].OuterXml);
            }
            else
            {
                _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", lastName, timeout: 5, xml: elements[0].OuterXml);
                if (elements.Count > 1)
                    _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", firstName, timeout: 5, xml: elements[1].OuterXml);
            }
            var xpaths = XpathManagerFacebook.Get(XpathType.NavigationButton);
            xpaths.Remove("//*[@content-desc=\"I already have an account\"]");
            _client.ElementWithAttributes(xpaths, 5);
        }

        private void HandleDateOfBirth()
        {
            var elementsBirth = _client.FindElements(10, "", "//*[contains(@text, 'Date of birth') and contains(@text, 'years old')]");
            if (!elementsBirth.Any()) return;

            string dateText = elementsBirth.First().Attributes["text"].Value;
            int age = ExtractAgeFromText(dateText);
            // Yêu cầu 2026-09-28: tuổi PHẢI > 33. Nếu tuổi hiện tại <= 33 thì bấm lại dòng
            // "Date of birth" để mở lại NumberPicker cho HandleDatePicker chọn năm sinh lớn hơn.
            if (age > 33) return;

            // Mỗi lần bấm mở lại picker là MỘT lượt tiêu ngân sách. Không có trần thì acc bị
            // Facebook trả tuổi ≤ 33 mãi sẽ mở lại picker cho tới hết _timeOut (30 phút) rồi
            // mới chết — giống hệt cái bẫy đã gặp ở màn "I agree". Cạn ngân sách thì bỏ acc
            // cho vòng ngoài sang tài khoản khác.
            if (_dobReopenAttempts >= DobReopenMaxAttempts)
            {
                _client.LogHelper.ERROR(
                    $"Ngày sinh vẫn ≤ 33 tuổi sau {_dobReopenAttempts} lượt đặt ngày sinh. Bỏ tài khoản để sang acc khác.");
                throw new SubdyExtension(SubdyEnum.Stop, "Không đặt được ngày sinh cho tuổi > 33.");
            }

            _client.ElementWithAttributes("//*[contains(@text, 'Date of birth') and contains(@text, 'years old')]", 5);
            _dobReopenAttempts++;
            _client.Delay(2);
            SetStatus(string.Empty, 2, logDetail: $"[DOB] Ngày sinh đang là {age} tuổi (≤ 33) — đã bấm mở lại picker (lượt {_dobReopenAttempts}).");
        }

        private int ExtractAgeFromText(string text)
        {
            Regex regex = new Regex(@"\d+");
            Match match = regex.Match(text);
            return match.Success ? Convert.ToInt32(match.Value) : 0;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ĐỌC 3 BÁNH XE NGÀY SINH TỪ XML MỚI.
        // XML raw (không hạ chữ) để còn thấy "Nov" chứ không phải "nov".
        // Sắp xếp 3 ô input theo TÂM X tăng dần = tháng / ngày / năm (KHÔNG hardcode toạ độ
        // vì độ phân giải máy khác nhau). Đọc hỏng thì trả null để caller thử lại.
        // ─────────────────────────────────────────────────────────────────────────
        private List<DobWheel> ReadDobWheels()
        {
            string xml = _client.GetXMLSource();
            if (string.IsNullOrWhiteSpace(xml)) return null;

            List<XmlNode> nodes = _client.FindElementsNotToLower(5, xml, "//*[@resource-id=\"android:id/numberpicker_input\"]");
            if (nodes == null || nodes.Count != 3) return null;

            List<DobWheel> inputs = new List<DobWheel>();
            foreach (XmlNode node in nodes)
            {
                try
                {
                    var rect = new RectangleArea(node.Attributes["bounds"].Value);
                    string text = (node.Attributes["text"]?.Value ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(text)) return null;

                    int value;
                    int monthIndex = Array.FindIndex(DobMonths, m => m.Equals(text, StringComparison.OrdinalIgnoreCase));
                    if (monthIndex >= 0)
                    {
                        value = monthIndex + 1;
                    }
                    else if (!int.TryParse(text, out value))
                    {
                        // Ô đang ở giữa animation: text chưa phải tên tháng / số → đọc lại sau.
                        return null;
                    }

                    inputs.Add(new DobWheel
                    {
                        Value = value,
                        XCenter = rect.GetCenterPoint().X,
                        Top = rect.Top,
                        Bottom = rect.Bottom
                    });
                }
                catch
                {
                    return null;
                }
            }

            inputs = inputs.OrderBy(w => w.XCenter).ToList();
            inputs[0].Kind = "month";
            inputs[1].Kind = "day";
            inputs[2].Kind = "year";
            return inputs;
        }

        /// <summary>
        /// Tua MỘT bánh xe tới giá trị đích. Hai pha:
        ///   • PHA NHANH — khi còn xa đích (|delta| &gt; 3): bấm liên tiếp hàng kề nhiều nhịp
        ///     (mỗi nhịp đúng 1 nấc vì đây là Button clickable thật), rồi ĐỌC LẠI một lần để
        ///     biết còn lệch bao nhiêu. Bấm hàng kề là bước ĐO ĐƯỢC (không quán tính như vuốt)
        ///     nên gộp nhịp vẫn chính xác, mà tránh phải dump XML 1 lần cho mỗi nấc — bánh NĂM
        ///     có thể phải đi vài chục nấc, dump từng nấc sẽ ăn hết _timeOut.
        ///   • PHA CHÍNH XÁC — khi còn ≤ 3 nấc: từng bước một, đọc lại xác minh, tap không ăn
        ///     thì lùi về vuốt chậm.
        /// Mỗi vòng đều tính lại delta từ giá trị ĐỌC ĐƯỢC nên tự sửa sai, không tích luỹ lỗi.
        /// </summary>
        private List<DobWheel> StepDobWheelToTarget(List<DobWheel> wheels, int kindIndex, int target, int maxSteps)
        {
            if (wheels == null || kindIndex < 0 || kindIndex >= wheels.Count) return wheels;

            int spent = 0;
            while (spent < maxSteps)
            {
                DobWheel wheel = wheels[kindIndex];
                int delta = DeltaToTarget(wheel.Kind, wheel.Value, target);
                if (delta == 0)
                {
                    SetDobStatus($"[DOB] {wheel.Kind} đã đúng ({wheel.Value}) sau {spent} nhịp.");
                    return wheels;
                }

                int direction = Math.Sign(delta);
                int remaining = Math.Abs(delta);
                int taps = remaining > 3 ? Math.Min(remaining - 1, 10) : 1;

                List<DobWheel> after;
                if (taps > 1)
                {
                    after = TapDobWheelBulk(wheel, direction, taps);
                    spent += taps;
                }
                else
                {
                    after = StepDobWheelOnce(wheel, direction);
                    spent++;
                }

                if (after == null)
                {
                    // Đọc lại không được (ô đang chạy animation) — để vòng sau tính lại từ đầu.
                    DobSettle();
                    continue;
                }

                DobWheel now = after[WheelIndexOf(after, wheel.Kind)];
                SetDobStatus($"[DOB] {wheel.Kind}: {wheel.Value} → {now.Value} ({(taps > 1 ? $"bấm {taps} nhịp" : "1 bước")}, còn {Math.Abs(DeltaToTarget(wheel.Kind, now.Value, target))} nấc)");
                wheels = after;
            }

            return wheels;
        }

        /// <summary>
        /// PHA NHANH: bấm liên tiếp hàng kề của đúng bánh xe này <paramref name="taps"/> nhịp
        /// (mỗi nhịp +1 nếu direction &gt; 0, −1 nếu &lt; 0), rồi đọc lại một lần.
        /// </summary>
        private List<DobWheel> TapDobWheelBulk(DobWheel wheel, int direction, int taps)
        {
            int neighborY = direction > 0
                ? wheel.Bottom + DobWheelNeighborOffset
                : wheel.Top - DobWheelNeighborOffset;

            for (int i = 0; i < taps; i++)
            {
                _client.Click(wheel.XCenter, neighborY);
                // Nhịp NGẮN để cú bấm không bị tính thành long-press (hàng kề long-clickable).
                System.Threading.Thread.Sleep(140);
            }

            DobSettle();
            return ReadDobWheels();
        }

        /// <summary>
        /// MỘT bước tua: bấm hàng kề đúng HÌNH HỌC (hàng kề nằm ngay trên/dưới ô input, cùng
        /// tâm X — bấm hàng DƯỚI = +1, hàng TRÊN = −1). Tap không ăn thì lùi về vuốt chậm
        /// dưới ngưỡng fling (~180-200px trong 240-330ms) — vì đo LIVE 2026-09-28: vuốt
        /// NHANH/NGẮN (7-12ms của bản v35) bị quán tính cuốn nhiều bước không đoán được.
        /// Trả về giá trị các bánh xe ĐỌC LẠI SAU bước này (null nếu không đọc được).
        /// </summary>
        private List<DobWheel> StepDobWheelOnce(DobWheel wheel, int direction)
        {
            int neighborY;
            int swipeStartY;
            if (direction > 0)
            {
                // Tăng 1 đơn vị: bấm hàng kề DƯỚI ô input; vuốt dự phòng kéo TỪ DƯỚI LÊN.
                neighborY = wheel.Bottom + DobWheelNeighborOffset;
                swipeStartY = wheel.Bottom + 22;
            }
            else
            {
                // Giảm 1 đơn vị: bấm hàng kề TRÊN ô input; vuốt dự phòng kéo TỪ TRÊN XUỐNG.
                neighborY = wheel.Top - DobWheelNeighborOffset;
                swipeStartY = wheel.Top - 22;
            }

            _client.Click(wheel.XCenter, neighborY);
            DobSettle();

            List<DobWheel> after = ReadDobWheels();
            if (after != null && IsDobStepDone(wheel.Kind, wheel.Value, after[WheelIndexOf(after, wheel.Kind)].Value, direction))
            {
                return after;
            }

            // Tap không ăn → vuốt chậm 1 bước (1 lần vuốt chậm ≈ đúng 1 nấc, đo LIVE
            // 2026-09-28: 190px trong 900ms = 1 nấc; 240-330ms vẫn dưới ngưỡng fling).
            // Vuốt XUỐNG = −1 (đầu ngón đi xuống), vuốt LÊN = +1.
            int distance = (wheel.Bottom - wheel.Top) + SubdyHelper.RandomValue(15, 40);
            int x1 = wheel.XCenter + SubdyHelper.RandomValue(3, 30);
            int x2 = wheel.XCenter - SubdyHelper.RandomValue(3, 30);
            int y2 = direction > 0
                ? Math.Max(wheel.Top - 60, swipeStartY - distance)
                : Math.Min(wheel.Bottom + 60, swipeStartY + distance);

            _client.Swipe(x1, swipeStartY, x2, y2, SubdyHelper.RandomValue(240, 330));
            DobSettle();
            return ReadDobWheels();
        }

        /// <summary>
        /// Chờ bánh xe ổn định sau một bước — NGẮN thôi, vì giá trị luôn được ĐỌC LẠI để xác
        /// minh chứ không tin vào thời gian chờ. Dùng DelayMessageAsync(1) thì mỗi bước tốn
        /// nguyên 1 giây, mà bánh NĂM có thể cần vài chục bước (mặc định → 1976..1991).
        /// </summary>
        private static void DobSettle()
        {
            System.Threading.Thread.Sleep(350);
        }

        private static int WheelIndexOf(List<DobWheel> wheels, string kind)
        {
            for (int i = 0; i < wheels.Count; i++)
            {
                if (wheels[i].Kind == kind) return i;
            }
            return 0;
        }

        /// <summary>
        /// Bước vừa rồi có tiến ĐÚNG HƯỚNG mong muốn không. Tháng là bánh xe VÒNG nên
        /// 12 → 1 (đi tới) và 1 → 12 (đi lùi) đều phải tính là tiến — nếu chỉ so `&gt;`/`&lt;`
        /// thì 2 lần vòng/năm sẽ bị coi là "không ăn" rồi bấm thừa một nấc.
        /// </summary>
        private static bool IsDobStepDone(string kind, int before, int after, int direction)
        {
            if (kind == "month")
            {
                int forward = ((after - before) % 12 + 12) % 12;
                if (forward == 0) return false;
                return direction > 0 ? forward <= 6 : forward >= 6;
            }
            return direction > 0 ? after > before : after < before;
        }

        /// <summary>Số nấc ngắn nhất từ giá trị hiện tại tới đích (tháng vòng 12; ngày/năm đi thẳng).</summary>
        private static int DeltaToTarget(string kind, int current, int target)
        {
            int raw = target - current;
            if (kind == "month")
            {
                int forward = ((raw % 12) + 12) % 12;
                return forward <= 6 ? forward : forward - 12;
            }
            return raw;
        }

        private void SetDobStatus(string detail)
        {
            SetStatus(string.Empty, 2, logDetail: detail);
        }

        private async Task HandleDatePicker()
        {
            if (!_client.ElementWithAttributes(_client.FindElement("", new List<string> { "//*[@text=\"SET\"]", "//*[@text=\"Next\"]" }, 5), 1, click: false)) return;

            // Trần CỨNG cho cả lượt chỉnh này: mỗi lần mở picker là một lượt, cộng dồn tối đa
            // DobReopenMaxAttempts lượt cho mỗi acc. Cạn thì BÁO LỖI RÕ RÀNG rồi bỏ acc —
            // nhất quyết KHÔNG bấm SET với ngày sai (v35 bấm SET dù chưa tua được gì, và
            // còn `return` IM LẶNG khi đọc không đủ 3 ô).
            if (_dobReopenAttempts >= DobReopenMaxAttempts)
            {
                _client.LogHelper.ERROR(
                    $"Không đặt được ngày sinh sau {_dobReopenAttempts} lượt chỉnh. Bỏ tài khoản để sang acc khác.");
                throw new SubdyExtension(SubdyEnum.Stop, "Không đặt được ngày sinh cho tuổi > 33.");
            }
            _dobReopenAttempts++;

            // Yêu cầu 2026-09-28: năm sinh phải cho tuổi > 33. RandomValue là [min, max)
            // → 1976..1991, tệ nhất là 1991 (2026 − 1991 = 35 tuổi) nên LUÔN > 33 kể cả khi
            // chưa tới sinh nhật. Tháng 1..12, ngày 1..28 để an toàn cho mọi tháng.
            int targetMonth = SubdyHelper.RandomValue(1, 13);
            int targetDay = SubdyHelper.RandomValue(1, 29);
            int targetYear = SubdyHelper.RandomValue(1976, 1992);
            SetDobStatus($"[DOB] Đích ngày sinh: {targetDay}/{targetMonth}/{targetYear}");

            List<DobWheel> wheels = null;
            for (int read = 0; read < 5 && wheels == null; read++)
            {
                wheels = ReadDobWheels();
                if (wheels == null)
                {
                    DobSettle();
                }
            }
            if (wheels == null)
            {
                SetDobStatus("[DOB] Không đọc được 3 bánh xe ngày sinh (numberpicker_input) — để vòng ngoài xử lý lại.");
                return;
            }

            SetDobStatus($"[DOB] Hiện tại: {wheels[1].Value}/{wheels[0].Value}/{wheels[2].Value} | X tâm = {wheels[0].XCenter}/{wheels[1].XCenter}/{wheels[2].XCenter}");

            // Trần bước mỗi bánh: tháng vòng nên tối đa 6 nấc, ngày tối đa 28 nấc, năm tối đa
            // ~92 nấc (1976..1991 là khoảng rộng nhất có thể gặp) + biên an toàn.
            wheels = StepDobWheelToTarget(wheels, 0, targetMonth, 8);
            wheels = StepDobWheelToTarget(wheels, 1, targetDay, 32);
            wheels = StepDobWheelToTarget(wheels, 2, targetYear, 100);

            // CHỈ bấm SET khi CẢ 3 giá trị đã khớp — còn lệch thì bỏ lượt này, vòng ngoài sẽ
            // mở lại picker (bộ đếm _dobReopenAttempts chặn ở DobReopenMaxAttempts lượt).
            List<DobWheel> final = ReadDobWheels();
            if (final == null || final.Count != 3)
            {
                SetDobStatus("[DOB] Không đọc lại được ngày sinh sau khi tua — KHÔNG bấm SET.");
                return;
            }

            int gotMonth = final[0].Value;
            int gotDay = final[1].Value;
            int gotYear = final[2].Value;
            if (gotMonth != targetMonth || gotDay != targetDay || gotYear != targetYear)
            {
                SetDobStatus($"[DOB] Lệch đích: được {gotDay}/{gotMonth}/{gotYear}, cần {targetDay}/{targetMonth}/{targetYear} — KHÔNG bấm SET.");
                return;
            }

            SetDobStatus($"[DOB] Khớp đích {gotDay}/{gotMonth}/{gotYear} — bấm SET.");
            _client.ElementWithAttributes(new List<string> { "//*[@text=\"SET\"]" }, 5);
            await DelayMessageAsync(2, _account.Status, 2);
            var xpaths = XpathManagerFacebook.Get(XpathType.NavigationButton);
            xpaths.Remove("//*[@content-desc=\"I already have an account\"]");
            _client.ElementWithAttributes(xpaths, 5);
        }

        private void HandleGenderSelection()
        {
            if (!_client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), click: false)) return;

            List<string> genderOptions = new List<string> { "//*[@text=\"Male\"]", "//*[@text=\"Female\"]" };
            if (_client.ElementWithAttributes(genderOptions[SubdyHelper.RandomValue(0, genderOptions.Count)], 5))
            {
                var xpaths = XpathManagerFacebook.Get(XpathType.NavigationButton);
                xpaths.Remove("//*[@content-desc=\"I already have an account\"]");
                _client.ElementWithAttributes(xpaths, 5);
            }
        }

        private async Task HandlePasswordInput()
        {
            await DelayMessageAsync(2, _account.Status, 2);
            if (!_client.ElementWithAttributes("//*[@class=\"android.widget.EditText\"]", timeoutInSeconds: 1, click: false) || !_client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), timeoutInSeconds: 1, click: false)) return;

            _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", _account.Password, timeout: 5);
            var xpaths = XpathManagerFacebook.Get(XpathType.NavigationButton);
            xpaths.Remove("//*[@content-desc=\"I already have an account\"]");
            _client.ElementWithAttributes(xpaths, 5);
        }

        private async Task<string> GetCode()
        {
            // shopmailmmo: OTP lấy qua ShopMailMmoService (đã tự lặp 30 lần x 5s nội bộ).
            if (_useShopMailMmo)
            {
                return await _shopMailMmo.GetCode(ct: _ct);
            }

            int timeout = 180;
            int interval = 2000;
            DateTime startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalSeconds < timeout)
            {
                string code = _typeRegister switch
                {
                    RegistrationType.Gmail_BaitPhoneNumber or RegistrationType.Gmail => await _gmailService.GetCode(),
                    RegistrationType.PhoneNumber => await _phoneService.GetCode(_account.Phone?.Split("|")[0]),
                    RegistrationType.Domain or RegistrationType.Domain_BaitPhoneNumber => await _emailService.GetCode(_account.Email, _settingGeneral.GetValuesFromInputString("textBox6")),
                    _ => string.Empty
                };

                if (!string.IsNullOrEmpty(code)) return code;

                _client.LogHelper.ERROR("Không nhận được mã xác nhận.");
                await Task.Delay(interval);
            }

            return string.Empty;
        }

        private async Task<string> GetEmail()
        {
            // shopmailmmo: cấp hộp thư Gmail cho luồng Reg Facebook (decision #3).
            // CHỜ VÔ HẠN tới khi có mail (shopmailmmo hết mail tạm thời rồi lại có);
            // chỉ thoát khi user bấm Dừng (_ct). Nếu shopmailmmo báo LỖI CỨNG (sai key /
            // hết số dư / service không tồn tại — retryable=false) thì đổi thiết bị cũng
            // vô ích → DỪNG TOÀN BỘ TOOL + ghi rõ thông báo (decision user 26-09:
            // "sai key hết số dư thì dừng tool", "dừng tool hay lỗi gì phải ghi rõ").
            if (_useShopMailMmo)
            {
                ShopMailMmoEmailResult result;
                try
                {
                    result = await _shopMailMmo.GetEmailUntilAvailableAsync(_ct);
                }
                catch (OperationCanceledException)
                {
                    // User bấm Dừng khi đang chờ mail vô hạn — báo rõ thay vì "task was canceled".
                    throw new SubdyExtension(SubdyEnum.Stop, "Đã dừng theo yêu cầu khi đang chờ mail shopmailmmo.");
                }
                if (result.Status == ShopMailMmoEmailStatus.FatalError)
                {
                    string notice = $"DỪNG TOOL — shopmailmmo: {result.Message}";
                    LogManager.Warning(notice);
                    SetStatus(notice, 1);
                    // Hủy token dùng chung → dừng MỌI thiết bị (toàn bộ tool), không chỉ acc này.
                    try { Globals.CancellationTokenSource?.Cancel(); } catch { }
                    throw new SubdyExtension(SubdyEnum.Stop, result.Message);
                }
                _account.Email = result.Email;
                // Đã chờ mail (có thể rất lâu) — reset đồng hồ để thời gian chờ KHÔNG ăn
                // vào _timeOut của bước đăng ký, tránh timeout oan ngay khi vừa có mail.
                _stopwatch.Restart();
                return _account.Email;
            }

            if (_typeRegister is RegistrationType.Domain_BaitPhoneNumber or RegistrationType.Domain)
            {
                string token = _settingGeneral.GetValuesFromInputString("textBox6", string.Empty).Trim();
                _account.Email = await _emailService.GetEmail(token);
                return _account.Email;
            }
            return string.Empty;
        }

        private async Task<string> GetPhone()
        {
            string phone = _typeRegister switch
            {
                RegistrationType.PhoneNumber => await _phoneService.GetPhone("facebook"),
                RegistrationType.Gmail_BaitPhoneNumber or RegistrationType.Domain_BaitPhoneNumber => GetRandomPhoneNumber(),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(phone))
            {
                _account.Phone = phone;
                return phone;
            }

            _client.LogHelper.ERROR("Không nhận được số điện thoại.");
            return string.Empty;
        }

        private string GetRandomPhoneNumber()
        {
            string country = SubdyHelper.RandomPhoneVN();
            if (_settingGeneral.GetBooleanValue("radioButton1"))
            {
                country = SubdyHelper.RandomPhoneUS();
            }
            return country;
        }

        private async Task<SubdyExtension> Agreement()
        {
            List<string> xpaths = BuildAgreementXPaths();
            var listLogin = BuildLoginList(xpaths);

            while (_stopwatch.ElapsedMilliseconds < _timeOut && !_ct.IsCancellationRequested)
            {
                // Quét & bấm nút Dismiss TRƯỚC khi khớp nhóm — xem ghi chú ở FacebookHander.
                if (FacebookHander.TryClickAnyDismiss(_client))
                {
                    _client.Delay(1);
                    continue;
                }
                // Màn MỚI không có trong gmailus.py: sheet "Create a passkey" và màn lỗi
                // "Something went wrong". Không xpath nào trong listLogin khớp 2 màn này nên
                // FindElement hết 120s trả "" -> nhánh dự phòng AppStart chạy lặp im lặng
                // (đã quan sát LIVE: đứng 20 phút không log). Phải quét TRƯỚC FindElement.
                if (TryHandlePasskeySheet())
                {
                    continue;
                }
                if (TryHandleFbErrorPage())
                {
                    continue;
                }
                string currentCase = _client.FindElement("", listLogin, 120);
                if (string.IsNullOrEmpty(currentCase))
                {
                    _client.AppStart(FacebookHander.Package(PlatformModel.Facebook), true, false, true);
                    await DelayMessageAsync(15, _account.Status, 2);
                    continue;
                }
                SetStatus("Đang xử lý...", 2, logDetail: currentCase);
                if (IsConfirmationCase(currentCase))
                {
                    if (currentCase == $"//*[contains(@text, \"confirm your account\") and contains(@text, \"{_account.Phone?.Split("|")[0]}\")]")
                    {
                        switch (_typeRegister)
                        {
                            case RegistrationType.Gmail_BaitPhoneNumber:
                            case RegistrationType.Domain_BaitPhoneNumber:
                                {
                                    _client.ElementWithAttributes("//*[@text=\"I didn’t get the code\"]", 5);
                                    await DelayMessageAsync(5, _account.Status, 2);
                                    if (!_client.ElementWithAttributes("//*[@text=\"Confirm by email\"]", 25)) continue;
                                    await GetEmail();
                                    if (string.IsNullOrEmpty(_account.Email))
                                    {
                                        _client.LogHelper.ERROR("Không nhận được email.");
                                        return new SubdyExtension(SubdyEnum.Error, "Không nhận được email.");
                                    }
                                    _client.SendTextADB("//*[@class=\"android.widget.EditText\"]", _account.Email.Split('|')[0], timeout: 25);
                                    _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), 5);
                                    await DelayMessageAsync(10, _account.Status, 2);
                                    break;
                                }
                        }
                    }
                    if (_isNVR)
                    {
                        return new SubdyExtension(SubdyEnum.Success, "Đăng ký thành công!");
                    }

                    await HandleConfirmationCode();
                    continue;
                }

                switch (currentCase)
                {
                    case "//*[@text=\"I agree\"]":
                        // Màn "Agree to Facebook's terms and policies"
                        // (FbExperimentalLoggedOutBloksActivity) — KHÁC HOÀN TOÀN màn Meta
                        // "pay or consent": ở đây KHÔNG có radio/thẻ option, chỉ có MỘT nút
                        // duy nhất. TRƯỚC ĐÂY case này bị gộp chung với MetaAdsConsent nên chỉ
                        // gọi TryHandleMetaAdsConsentAsync — hàm đó gate bằng nhóm xpath consent,
                        // không khớp gì trên màn này rồi return false, KHÔNG hề bấm -> vòng lặp
                        // quét lại "I agree" mãi tới hết _timeOut (30 phút) -> MAIL HẾT HẠN.
                        HandleIAgreeConsent();
                        break;
                    case var x when XpathManagerFacebook.Get(XpathType.MetaAdsConsent).Contains(x):
                        await FacebookHander.TryHandleMetaAdsConsentAsync(_client);
                        break;
                    case var x when XpathManagerFacebook.Get(XpathType.NavigationButton).Contains(x):
                        _client.ElementWithAttributes(currentCase, 5);
                        await DelayMessageAsync(1, _account.Status, 2);
                        break;
                    case var x when new List<string> { "//*[@text=\"I didn’t get the code\"]", "//*[@text=\"Confirm by email\"]" }.Contains(x):
                        if (_typeRegister is RegistrationType.Domain_BaitPhoneNumber or RegistrationType.Gmail_BaitPhoneNumber)
                        {
                            _client.ElementWithAttributes(currentCase, 5);
                            await DelayMessageAsync(5, _account.Status, 2);
                            break;
                        }
                        await HandleConfirmationCode();
                        break;
                    case "//*[@text=\"Enter an email\"]":
                        await HandleEmailInputForAgreement();
                        break;
                    case "//*[@content-desc=\"We couldn't create an account for you\"]":
                    case "//*[@text=\"We couldn't create an account for you\"]":
                    case var x when XpathManagerFacebook.Combine(XpathType.CP282, XpathType.CP956, XpathType.Captcha, XpathType.ExistEmail).Contains(x):
                        throw new SubdyExtension(SubdyEnum.CP_282, $"Tài khoản bị - [{currentCase}]");
                    case var x when XpathManagerFacebook.Get(XpathType.Success).Contains(x):
                        await DelayMessageAsync(5, _account.Status, 2);
                        if (!_client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.Success), click: false)) continue;
                        return new SubdyExtension(SubdyEnum.Success, "LIVE");
                    case "//*[@text=\"Enter the confirmation code\"]":
                        {
                            switch (_typeRegister)
                            {
                                case RegistrationType.Gmail_BaitPhoneNumber:
                                case RegistrationType.Domain_BaitPhoneNumber:
                                    {
                                        _client.ElementWithAttributes("//*[@text=\"I didn’t get the code\"]", 5);
                                        await DelayMessageAsync(5, _account.Status, 2);
                                        if (!_client.ElementWithAttributes("//*[@text=\"Confirm by email\"]", 25)) continue;
                                        await GetEmail();
                                        if (string.IsNullOrEmpty(_account.Email))
                                        {
                                            _client.LogHelper.ERROR("Không nhận được email.");
                                            return new SubdyExtension(SubdyEnum.Error, "Không nhận được email.");
                                        }
                                        _client.SendTextADB("//*[@class=\"android.widget.EditText\"]", _account.Email.Split('|')[0], timeout: 25);
                                        _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), 5);
                                        await DelayMessageAsync(10, _account.Status, 2);
                                        break;
                                    }
                            }
                            if (_isNVR)
                            {
                                return new SubdyExtension(SubdyEnum.Success, "Đăng ký thành công!");
                            }
                            await HandleConfirmationCode();
                            break;
                        }
                }
            }

            return new SubdyExtension(SubdyEnum.Error, "Đã xảy ra lỗi khi đăng ký.");
        }

        // ── MÀN "I agree" (2026-09-26) ────────────────────────────────────────────
        // Cấu trúc LIVE xác minh trên 5200d7ad5a6315d5 (dump qua ATX /dump/hierarchy vì
        // `uiautomator dump` bị SIGKILL rc=137 trên màn Bloks này):
        //   Button  content-desc="I agree"  text=""  clickable=true   [56,1297][1384,1451]
        //   View    text="I agree"          clickable=false           [638,1338][802,1411]
        // Hai node TRÙNG tâm (720,1374) nên toạ độ KHÔNG phải vấn đề — vấn đề là code cũ
        // KHÔNG hề bấm. Đã kiểm chứng sống: `input tap 720 1374` -> màn đi tiếp ngay.
        // Bấm bằng xpath chỉ nhắm node clickable=true (đúng tinh thần toidongy() của gmailus.py:
        // bấm NÚT THẬT, không bấm node chữ), rồi CHỜ màn biến mất như cho_room_cu_bien_mat /
        // choromotp — để vòng lặp ngoài kịp nhận diện màn kế tiếp thay vì quét lại "I agree".
        private void HandleIAgreeConsent()
        {
            // Vừa bấm xong thì vài giây sau XML có thể vẫn còn node cũ -> bấm lặp vào nút đã MỜ
            // chỉ khiến FB trả "We couldn't create an account for you" (đã quan sát LIVE).
            // Bỏ qua trong COOLDOWN; hết cooldown mà màn vẫn còn thì cứ bấm lại.
            int now = Environment.TickCount;
            if (_iAgreeLastClickTick != 0 && unchecked(now - _iAgreeLastClickTick) < IAgreeClickCooldownMs)
            {
                return;
            }

            var xpaths = new List<string>
            {
                "//*[@clickable=\"true\" and @content-desc=\"I agree\"]",
                "//*[@clickable=\"true\" and @text=\"I agree\"]",
                // Lưới cuối: engine có fallback hoa/thường cho exact @attr= nên bắt được cả
                // "I Agree"/"I AGREE". Đặt CUỐI để không cướp lượt của node clickable.
                "//*[@content-desc=\"I agree\"]",
                "//*[@text=\"I agree\"]",
            };

            bool clicked = _client.ElementWithAttributes(xpaths, 5);
            if (!clicked)
            {
                // Không còn node nào để bấm = màn đã đi tiếp -> thoát NGAY, KHÔNG chờ.
                return;
            }

            _iAgreeLastClickTick = Environment.TickCount;
            _iAgreeAttempts++;
            SetStatus("Đã bấm 'I agree', chờ Facebook xử lý...", 2);

            // Chờ màn biến mất: đòi 2 LẦN LIÊN TIẾP không còn khớp mới coi là đã qua
            // (mirror cho_room_cu_bien_mat), tối đa ~20s rồi trả về cho vòng lặp chính tự quyết.
            int miss = 0;
            for (int i = 0; i < 20 && !_ct.IsCancellationRequested; i++)
            {
                System.Threading.Thread.Sleep(1000);
                if (_client.ElementWithAttributes("//*[@text=\"I agree\"]", 1, click: false))
                {
                    miss = 0;
                    continue;
                }
                if (++miss >= 2) return;
            }

            // Màn VẪN còn "I agree" sau khi bấm + chờ đủ. Thử lại vài lần; quá
            // IAgreeMaxAttempts thì dừng acc này NGAY thay vì quét tới hết _timeOut — mail
            // đã tiêu rồi, càng kẹt càng vô ích và chặn luôn các acc sau trong hàng đợi.
            if (_iAgreeAttempts >= IAgreeMaxAttempts)
            {
                _client.LogHelper.ERROR(
                    $"Kẹt màn 'I agree' sau {_iAgreeAttempts} lần bấm (nút không phản hồi — Facebook chặn acc này). Bỏ tài khoản để sang acc khác.");
                throw new SubdyExtension(SubdyEnum.CP_282, "Kẹt màn 'I agree' — Facebook không cho tạo tài khoản.");
            }
        }

        // ── MÀN "Create a passkey" + MÀN LỖI "Something went wrong" (2026-09-27) ────
        // Đây là 2 màn MỚI của Facebook, KHÔNG có trong gmailus.py (grep 'passkey' = 0 hit,
        // 'Something went wrong' = 0 hit, 'Retry' = 0 hit — tool Python cũ chưa từng gặp).
        //
        // TRIỆU CHỨNG LIVE (acc #5 Dung Perry, thiết bị 5200d7ad5a6315d5, run3):
        //   12:35:13 khớp "Skip" -> RỒI ĐỨNG IM 20 PHÚT, log KHÔNG có dòng nào.
        //   12:55:54 bấm TAY nút X của sheet -> engine chạy lại NGAY ("Skip").
        // => nguyên nhân là KHÔNG xpath nào trong listLogin khớp màn này, nên
        //    FindElement hết 120s trả "" -> nhánh dự phòng AppStart + 15s chạy LẶP MÀ KHÔNG
        //    GHI LOG (đó là lý do khoảng trống 20 phút im lặng hoàn toàn).
        //
        // Cấu trúc sheet (dump qua ATX /dump/hierarchy, `uiautomator dump` bị SIGKILL rc=137):
        //   View   text/desc="Create a passkey"                  [56,182][1384,304]
        //   View   text="Next time, log in with your face scan..."
        //   Button text="Learn how passkeys work"                [706,782][1252,843]
        //   Button desc="Create passkey"                         [56,2350][1384,2392]  <-- KHÔNG bấm
        //   Button desc="Close"                                  [42,14][126,168]       <-- BẤM CÁI NÀY
        // Nút "Create passkey" ở đáy và nút "Close" ở góc trên-trái là 2 nút KHÁC NHAU; chỉ
        // bấm Close. Tuyệt đối KHÔNG dùng contains(@content-desc,"passkey") để bấm vì sẽ
        // trúng nút Create passkey (đăng ký passkey thật, không phải thứ mình muốn).
        //
        // Màn thứ 2 gặp NGAY SAU khi đóng sheet: "Something went wrong. Please try again."
        // với Button desc="Retry" + Button desc="Back" (cũng không khớp xpath nào). Bấm Retry
        // vài lần; nếu vẫn quay lại thì bấm Back để thoát về màn trước.
        //
        // CẢ HAI đều nằm trong RegFacebookRegsiner (module Reg Facebook) — KHÔNG sửa
        // FacebookHander/XpathManagerFacebook. FacebookHander.TryClickAnyDismiss KHÔNG cứu
        // được 2 màn này vì nó return sớm trừ khi XML có literal "Dismiss"/"automated behavior".
        private int _passkeyCloseAttempts = 0;
        private int _passkeyLastClickTick = 0;
        private const int PasskeyClickCooldownMs = 8000;
        private const int PasskeyMaxAttempts = 4;

        private int _fbErrorRetryAttempts = 0;
        private int _fbErrorLastClickTick = 0;
        private const int FbErrorClickCooldownMs = 8000;
        private const int FbErrorMaxAttempts = 6;

        /// <summary>
        /// Đóng sheet "Create a passkey" bằng nút X (content-desc="Close") ở góc trên-trái.
        /// Trả về true nếu ĐÃ bấm. Chỉ chạy khi XML có literal "Create a passkey" nên không
        /// thể bấm nhầm "Close" của màn khác.
        /// </summary>
        private bool TryHandlePasskeySheet(string xml = "")
        {
            try
            {
                if (string.IsNullOrEmpty(xml)) xml = _client.GetXMLSource();
                if (string.IsNullOrEmpty(xml)) return false;

                // Cổng rẻ + CHẶT: chỉ sheet passkey mới có cụm này.
                if (xml.IndexOf("Create a passkey", StringComparison.OrdinalIgnoreCase) < 0) return false;

                int now = Environment.TickCount;
                if (_passkeyLastClickTick != 0
                    && unchecked(now - _passkeyLastClickTick) < PasskeyClickCooldownMs)
                {
                    return false;
                }

                var xpaths = new List<string>
                {
                    "//*[@clickable=\"true\" and @content-desc=\"Close\"]",
                    "//*[@clickable=\"true\" and @text=\"Close\"]",
                    "//*[@content-desc=\"Close\"]",
                    "//*[@text=\"Close\"]",
                };

                if (!_client.ElementWithAttributes(xpaths, 3, xml)) return false;

                _passkeyLastClickTick = Environment.TickCount;
                _passkeyCloseAttempts++;
                SetStatus("Đã đóng màn 'Create a passkey'...", 2);

                // Chờ sheet biến mất: đòi 2 lần liên tiếp không còn khớp (giống HandleIAgreeConsent).
                int miss = 0;
                for (int i = 0; i < 12 && !_ct.IsCancellationRequested; i++)
                {
                    System.Threading.Thread.Sleep(1000);
                    if (_client.ElementWithAttributes("//*[contains(@content-desc, \"passkey\")]", 1, click: false))
                    {
                        miss = 0;
                        continue;
                    }
                    if (++miss >= 2) break;
                }

                if (_passkeyCloseAttempts >= PasskeyMaxAttempts)
                {
                    _client.LogHelper.ERROR(
                        $"Đã đóng màn 'Create a passkey' {_passkeyCloseAttempts} lần mà Facebook vẫn hiện lại. Bỏ tài khoản để sang acc khác.");
                    throw new SubdyExtension(SubdyEnum.Stop, "Kẹt màn 'Create a passkey' — Facebook không cho bỏ qua.");
                }

                return true;
            }
            catch (SubdyExtension)
            {
                throw;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Màn lỗi "Something went wrong. Please try again." — bấm Retry vài lần, sau đó Back.
        /// Trả về true nếu ĐÃ bấm.
        /// </summary>
        private bool TryHandleFbErrorPage(string xml = "")
        {
            try
            {
                if (string.IsNullOrEmpty(xml)) xml = _client.GetXMLSource();
                if (string.IsNullOrEmpty(xml)) return false;

                if (xml.IndexOf("Something went wrong", StringComparison.OrdinalIgnoreCase) < 0) return false;

                int now = Environment.TickCount;
                if (_fbErrorLastClickTick != 0
                    && unchecked(now - _fbErrorLastClickTick) < FbErrorClickCooldownMs)
                {
                    return false;
                }

                // 2 lần đầu bấm Retry (lỗi mạng thoáng qua); sau đó Bấm Back để thoát hẳn
                // khỏi màn lỗi thay vì Retry mãi.
                var xpaths = _fbErrorRetryAttempts < 2
                    ? new List<string> { "//*[@clickable=\"true\" and @content-desc=\"Retry\"]", "//*[@content-desc=\"Retry\"]", "//*[@text=\"Retry\"]" }
                    : new List<string> { "//*[@clickable=\"true\" and @content-desc=\"Back\"]", "//*[@content-desc=\"Back\"]" };

                if (!_client.ElementWithAttributes(xpaths, 3, xml)) return false;

                _fbErrorLastClickTick = Environment.TickCount;
                _fbErrorRetryAttempts++;
                SetStatus($"Màn lỗi Facebook — đã bấm {(_fbErrorRetryAttempts <= 2 ? "Retry" : "Back")} (lần {_fbErrorRetryAttempts})...", 2);

                if (_fbErrorRetryAttempts >= FbErrorMaxAttempts)
                {
                    _client.LogHelper.ERROR(
                        $"Màn 'Something went wrong' lặp {_fbErrorRetryAttempts} lần không thoát được. Bỏ tài khoản để sang acc khác.");
                    throw new SubdyExtension(SubdyEnum.Stop, "Kẹt màn lỗi Facebook — không tạo được tài khoản.");
                }

                _client.Delay(2);
                return true;
            }
            catch (SubdyExtension)
            {
                throw;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Màn "How old are you?" — biến thể NHẬP TUỔI TRỰC TIẾP (KHÔNG có trong
        /// XpathManagerFacebook, KHÔNG có trong gmailus.py). Trên màn này chỉ có MỘT
        /// EditText (desc "Age,") + nút Next. Nếu để rơi vào case NavigationButton
        /// "Next" chung thì Next được bấm với ô tuổi RỖNG -> "Input Age is invalid."
        /// -> lặp vô ích tới hết timeout (đã quan sát LIVE trên thiết bị 5200d).
        /// Xử lý: nhập tuổi ngẫu nhiên 34-50 (yêu cầu user 2026-09-28: tuổi > 33)
        /// rồi bấm Next. Trả về true nếu ĐÃ xử lý.
        /// </summary>
        private bool TryHandleAgeInput(string xml = "")
        {
            try
            {
                if (string.IsNullOrEmpty(xml)) xml = _client.GetXMLSource();
                if (string.IsNullOrEmpty(xml)) return false;

                // Cổng rẻ + CHẶT: tiêu đề "How old are you?" chỉ xuất hiện trên màn này.
                if (xml.IndexOf("How old are you?", StringComparison.OrdinalIgnoreCase) < 0
                    && xml.IndexOf("Enter your real age.", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }

                int now = Environment.TickCount;
                if (_ageScreenLastClickTick != 0
                    && unchecked(now - _ageScreenLastClickTick) < AgeScreenClickCooldownMs)
                {
                    return false;
                }

                // Nhập tuổi 34-50 vào ô tuổi (màn này chỉ có 1 EditText).
                int age = SubdyHelper.RandomValue(34, 50);
                _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", age.ToString(), timeout: 5);
                SetStatus($"Màn 'How old are you?' — đã nhập tuổi {age}...", 2);

                // Bấm Next riêng của màn này (KHÔNG dùng list NavigationButton chung để
                // tránh rơi lại đúng cái bẫy case Next-với-ô-tuổi-rỗng).
                if (!_client.ElementWithAttributes(new List<string>
                    {
                        "//*[@clickable=\"true\" and @content-desc=\"Next\"]",
                        "//*[@clickable=\"true\" and @text=\"Next\"]",
                    }, 5))
                {
                    return false;
                }

                _ageScreenLastClickTick = Environment.TickCount;
                _ageScreenAttempts++;

                if (_ageScreenAttempts >= AgeScreenMaxAttempts)
                {
                    _client.LogHelper.ERROR(
                        $"Màn 'How old are you?' lặp {_ageScreenAttempts} lần không qua được (tuổi bị từ chối?). Bỏ tài khoản để sang acc khác.");
                    throw new SubdyExtension(SubdyEnum.Stop, "Kẹt màn 'How old are you?' — Facebook không chấp nhận tuổi nhập.");
                }

                _client.Delay(2);
                return true;
            }
            catch (SubdyExtension)
            {
                throw;
            }
            catch
            {
                return false;
            }
        }

        private List<string> BuildAgreementXPaths()
        {
            List<string> xpaths = new List<string>();
            if (!string.IsNullOrEmpty(_account.Email))
            {
                xpaths.Add($"//*[contains(@text, \"confirm your account\") and contains(@text, \"{_account.Email}\")]");
            }
            if (!string.IsNullOrEmpty(_account.Phone))
            {
                xpaths.Add($"//*[contains(@text, \"confirm your account\") and contains(@text, \"{_account.Phone?.Split("|")[0]}\")]");
            }
            xpaths.Add("//*[@text=\"We couldn't create an account for you\"]");
            xpaths.AddRange(new List<string>
            {
                "//*[@text=\"We couldn't create an account for you\"]",
                "//*[@text=\"Enter an email\"]",
                "//*[@text=\"Couldn't create account\"]",
                "//*[@text=\"Enter email\"]",
                "//*[@text=\"Confirm with email\"]",
                "//*[@text=\"I didn't receive a code\"]",
                "//*[@content-desc=\"We couldn't create an account for you\"]",
                "//*[@text=\"Please log in again.\"]",
                "//*[@text=\"Sign up\"]",
                "//*[@text=\"Create new account\"]",
                "//*[@content-desc=\"Create new account\"]",
                "//*[@content-desc=\"Join Facebook\"]",
                "//*[@text=\"Get started\"]",
                "//*[@content-desc=\"No, create new account\"]",
                "//*[@text=\"Enter the confirmation code\"]",
            });
            return xpaths;
        }

        private List<string> BuildLoginList(List<string> xpaths)
        {
            var listLogin = new List<string>();
            listLogin.AddRange(XpathManagerFacebook.Combine(XpathType.CP282, XpathType.CP956, XpathType.ExistEmail, XpathType.Success));
            listLogin.AddRange(xpaths);
            // Đứng TRƯỚC NavigationButton: trên màn "pay or consent" phần tử khớp đầu
            // của NavigationButton là nút Continue mờ (click vô tác dụng) -> kẹt.
            listLogin.AddRange(XpathManagerFacebook.Get(XpathType.MetaAdsConsent));
            listLogin.AddRange(XpathManagerFacebook.Get(XpathType.NavigationButton));
            listLogin.Add("//*[@text=\"I agree\"]");
            return listLogin;
        }

        private async Task HandleEmailInputForAgreement()
        {
            if (!_client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), timeoutInSeconds: 1, click: false)) return;
            if (_typeRegister is RegistrationType.Domain or RegistrationType.Gmail_BaitPhoneNumber or RegistrationType.Domain_BaitPhoneNumber or RegistrationType.Gmail)
            {
                await GetEmail();
                if (string.IsNullOrEmpty(_account.Email))
                {
                    _client.LogHelper.ERROR("Không nhận được email.");
                    return;
                }
                _client.ElementWithAttributes(_client.FindElement("", new List<string> { "//*[@text=\"Next\"]" }, 1), click: false);
                _client.SendTextSlow("//*[@class=\"android.widget.EditText\"]", _account.Email.Split('|')[0], timeout: 5);
            }
            _client.ElementWithAttributes(XpathManagerFacebook.Get(XpathType.NavigationButton), 5);
        }

        private async Task<SubdyExtension> ImportInfo()
        {
            _sate = "Nhập thông tin đăng ký";
            string currentCase = string.Empty;
            List<string> caseFacebooks = FacebookHander.Regsiner_Facebook();
            caseFacebooks.Remove("//*[@content-desc=\"I already have an account\"]");
            _stopwatch.Restart();
            while (_stopwatch.ElapsedMilliseconds < _timeOut && !_ct.IsCancellationRequested)
            {
                if (!await EnsureAppRunning()) continue;
                // Hai màn MỚI (passkey / lỗi FB) cũng chặn ở bước nhập thông tin — xem ghi
                // chú ở TryHandlePasskeySheet. Quét TRƯỚC FindElement.
                if (TryHandlePasskeySheet())
                {
                    continue;
                }
                if (TryHandleFbErrorPage())
                {
                    continue;
                }
                // Màn "How old are you?" (NHẬP TUỔI TRỰC TIẾP — biến thể KHÔNG có trong
                // XpathManagerFacebook): nếu rơi vào case NavigationButton "Next" chung
                // thì Next sẽ được bấm với ô tuổi RỖNG -> "Input Age is invalid." -> lặp
                // tới hết timeout. Phải quét TRƯỚC FindElement như 2 màn trên.
                if (TryHandleAgeInput())
                {
                    continue;
                }
                currentCase = _client.FindElement("", caseFacebooks, 120);
                if (string.IsNullOrEmpty(currentCase))
                {
                    _client.AppStart(FacebookHander.Package(PlatformModel.Facebook), true, false, true);
                    await DelayMessageAsync(5, _account.Status, 2);
                    continue;
                }

                SetStatus("Đang xử lý...", 2, logDetail: currentCase);

                if (IsConfirmationCase(currentCase))
                {
                    if (_isNVR)
                    {
                        return new SubdyExtension(SubdyEnum.Success, "Đăng ký thành công!");
                    }

                    await HandleConfirmationCode();
                    continue;
                }

                switch (currentCase)
                {
                    case var c when XpathManagerFacebook.Get(XpathType.Loading).Contains(c):
                        break;
                    case var c when XpathManagerFacebook.Get(XpathType.ExistEmail).Contains(c):
                        return new SubdyExtension(SubdyEnum.EmailExist, "Đã có tài khoản thêm mail này rồi.");
                    case "//*[@text=\"Sign up with mobile number\"]":
                    case "//*[@text=\"What's your email?\"]":
                        _sate = "Nhập email";
                        await HandleEmailInput();
                        break;
                    case "//*[@text=\"What is your mobile number?\"]":
                    case "//*[@text=\"Sign up with email\"]":
                        _sate = "Nhập số điện thoại";
                        await HandlePhoneInput();
                        break;
                    case "//*[@text=\"Select your name\"]":
                    case "//*[@text=\"Choose your name\"]":
                        HandleNameSelection();
                        break;
                    case var c when XpathManagerFacebook.Get(XpathType.Success).Contains(c):
                        return new SubdyExtension(SubdyEnum.Success, "Đăng ký thành công!");
                    case "//*[@text=\"I agree\"]":
                        // Màn "I agree" cũng xuất hiện ngay ở bước nhập thông tin (không chỉ
                        // trong Agreement()). Chuyển thẳng sang Agreement() — ở đó
                        // HandleIAgreeConsent() lo việc bấm ĐÚNG nút clickable + chống bấm lặp.
                        _sate = "Chờ xác nhận từ Facebook";
                        return await Agreement();
                    case var c when XpathManagerFacebook.Get(XpathType.MetaAdsConsent).Contains(c):
                        await FacebookHander.TryHandleMetaAdsConsentAsync(_client);
                        break;
                    case var c when XpathManagerFacebook.Get(XpathType.NavigationButton).Contains(c):
                    case var x when XpathManagerFacebook.Get(XpathType.Confim_Register).Contains(x):
                        _client.ElementWithAttributes(currentCase, 5);
                        break;
                    case "//*[@text=\"Enter the confirmation code\"]":
                        _sate = "Nhập mã xác nhận";
                        await HandleConfirmationCode();
                        break;
                    case "//*[@text=\"First name\"]":
                    case "//*[@text=\"What's your name?\"]":
                        _sate = "Nhập họ tên";
                        await HandleNameInput();
                        break;
                    case "//*[@text=\"When is your date of birth?\"]":
                        _sate = "Chọn ngày sinh";
                        HandleDateOfBirth();
                        break;
                    case "//*[@text=\"SET\"]":
                    case "//*[@text=\"Set date\"]":
                        _sate = "Chọn ngày sinh";
                        await HandleDatePicker();
                        break;
                    case "//*[@text=\"Male\"]":
                    case "//*[@text=\"Female\"]":
                    case "//*[@text=\"What is your gender?\"]":
                        _sate = "Chọn giới tính";
                        HandleGenderSelection();
                        break;
                    case "//*[@text=\"Create a password\"]":
                        _sate = "Tạo mật khẩu";
                        await HandlePasswordInput();
                        break;
                }
                await DelayMessageAsync(2, _account.Status, 2);
            }

            return new SubdyExtension(SubdyEnum.Error, "Đã xảy ra lỗi khi đăng ký.");
        }
    }
}
