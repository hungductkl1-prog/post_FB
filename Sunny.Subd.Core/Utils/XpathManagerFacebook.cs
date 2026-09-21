using Sunny.Subd.Core.Models;
using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace Sunny.Subd.Core.Utils
{
    public class XpathManagerFacebook
    {
        private static readonly ConcurrentDictionary<XpathType, List<string>> _xpathGroups = new();
        private static readonly object _loadLock = new();
        private static Task? _loadTask;
        static XpathManagerFacebook()
        {
            _xpathGroups.TryAdd(XpathType.Captcha, new List<string>
      {
          $"//*[contains(@text, \"Enter the characters you see\")]",
      });
            _xpathGroups.TryAdd(XpathType.No_Internet, new List<string>
      {
          $"//*[@text=\"Page isn't available right now\"]",
          // FIX B1: đã XOÁ xpath generic "Refresh" khỏi nhóm No_Internet. "Refresh" khớp MỌI
          // màn FB có nút Refresh -> dương tính giả -> kích hoạt HandleNoInternet oan (trước
          // đây dẫn tới reboot cứng = mất view phone). Màn mất-mạng THẬT đã được bắt bởi
          // "Page isn't available right now" ở trên.
      });
            _xpathGroups.TryAdd(XpathType.CP282, new List<string>
          {
              "//*[@content-desc=\"Start security steps\"]",
              "//*[@text=\"Start video selfie\"]",
             "//*[@text=\"Your selfie will only be used to confirm your identity and to keep our community safe.\"]",
              "//*[@text=\"It will be deleted within 30 days.\"]",
             "//*[@text=\"Type the text\"]",
             "//*[contains(@content-desc, \"confirm you're human to use your account\")]",
             $"//*[contains(@text, \"Record a video of yourself\")]",
             $"//*[contains(@text, \"Vietnam (+84)\")]",
             $"//*[contains(@text, \"United States of America (+1)\")]",
             $"//*[contains(@text, \"Type the text\")]",
             $"//*[contains(@text, \"We disabled your account\")]",
             $"//*[contains(@text, \"Access Denied\")]",
             $"//*[contains(@text, \"Appeal\")]",
             $"//*[contains(@text, \"Send by SMS\")]",
             "//*[@text=\"Enter text\"]",
             "//*[@text=\"This page is currently not displayed\"]",
             $"//*[contains(@text, \"we suspended your account\")]",
             $"//*[contains(@text, \"read more about this rule\")]",
             $"//*[contains(@text, \"Upload image or take photo\")]",
             $"//*[contains(@text, \"Account Temporarily Unavailable\")]",
             // Bug 5: Màn hình "Kiểm tra tài khoản" / Srcool khi đăng story lần 2
             $"//*[contains(@text, \"Review Recent Login\")]",
             $"//*[contains(@text, \"Confirm Your Identity\")]",
             $"//*[contains(@text, \"Verify your identity\")]",
             $"//*[contains(@text, \"We need to verify your account\")]",
             $"//*[contains(@text, \"Help us confirm\")]",
             $"//*[contains(@text, \"Confirm your account\")]",
             $"//*[contains(@text, \"Review Your Account\")]",
             $"//*[contains(@text, \"Secure Your Account\")]",
             $"//*[contains(@text, \"Protect Your Account\")]",
             $"//*[contains(@text, \"Account Review\")]",
          });
            _xpathGroups.TryAdd(XpathType.CP956, new List<string>
          {
             $"//*[contains(@text, \"Check your WhatsApp messages\")]",
             $"//*[contains(@text, \"check your email\")]",
          });
            _xpathGroups.TryAdd(XpathType.Block, new List<string>
          {
             // ĐÃ GỠ "contains(@text,Dismiss)" khỏi Block. "Dismiss" là nút bấm chung của
             // MỌI hộp thoại cảnh báo dismiss-được (vd "We suspect automated behavior on your
             // account"), KHÔNG phải marker block thật. Nằm ở Block (vị trí 7 trong flat order,
             // TRƯỚC NavigationButton vị trí 14) nó chặn đường 4 entry Dismiss của NavigationButton
             // và khiến HanderAccount ném nhầm "Tài khoản bị chặn". Block thật vẫn được bắt bởi
             // 3 marker cụ thể bên dưới; dialog dismiss-được rơi xuống NavigationButton để click.
             $"//*[contains(@text, \"We limit how often you can post\")]",
             $"//*[contains(@text, \"Your account is restricted\")]",
             $"//*[contains(@text, \"we added restrictions to your account\")]",
          });
            _xpathGroups.TryAdd(XpathType.Success, new List<string>
          {
            "//*[contains(@content-desc, 'Go to profile')]",
            //"//*[@text=\"Add a profile picture\"]",
            //"//*[@text=\"Add a mobile number to your account\"]",
            //"//*[@content-desc=\"News Feed\"]",
            //"//*[@content-desc=\"Home\"]",
            //"//*[contains(@content-desc, 'Newsfeed')]",
            //"//*[@content-desc=\"Marketplace\"]",
            //"//*[@content-desc=\"Notifications\"]",
            //"//*[@content-desc=\"Watch\"]",
            //"//*[@content-desc=\"Menu\"]",
            //"//*[@content-desc=\"Search Facebook\"]",
            //"//*[contains(@content-desc, 'What')]",
          });
            _xpathGroups.TryAdd(XpathType.Loading, new List<string>
      {
          "//*[@content-desc=\"Đang tải\"]",
      });
            _xpathGroups.TryAdd(XpathType.Logout, new List<string>
      {
          "//*[contains(@text, \"You've been logged out\")]",
          $"//*[contains(@text, \"Please log in again\")]",
          $"//*[contains(@text, \"Log in to continue\")]",
          $"//*[contains(@text, \"Your session has expired\")]",
      });
            _xpathGroups.TryAdd(XpathType.WrongPassword, new List<string>
      {
          "//*[contains(@text, \"Unable to log in\")]",
          $"//*[contains(@text, \"Wrong Credentials\")]",
          $"//*[contains(@text, \"Invalid username or password\")]",
          $"//*[contains(@text, \"The password you entered is incorrect\")]",
          $"//*[contains(@text, \"incorrect password\")]",
          $"//*[contains(@text, \"password is incorrect\")]",
      });
            _xpathGroups.TryAdd(XpathType.NavigationButton, new List<string>
      {
                "//*[@content-desc=\"Agree\"]",
                // ── DISMISS: ĐỨNG ĐẦU NHÓM (dời lên 2026-09-08; trước đây nằm giữa nhóm, sau
                // "End quiet mode"). ADBClient.FindElement trả về XPATH KHỚP ĐẦU TIÊN THEO THỨ TỰ
                // LIST (ADBClient.cs:3514), nên entry đứng càng sớm càng dễ được chọn. Dialog
                // Facebook (vd "We suspect automated behavior on your account") là cửa sổ CHỒNG LÊN
                // màn nền và uiautomator dump bắt được node của CẢ màn nền; nếu Dismiss đứng sau,
                // một entry khác khớp node nền sẽ thắng, tool bấm vào nền, dialog nuốt cú chạm rồi
                // vòng lặp quay lại -> "đã thêm Dismiss mà vẫn kẹt".
                // CỐ Ý KHÔNG dời Dismiss XUỐNG cạnh Save/Skip: đứng sau = ưu tiên THẤP hơn.
                // Save/Skip bấm được chỉ vì trên màn đó không có entry nào đứng trước chúng khớp.
                // Dạng exact có fallback hoa/thường (TryFindBoundsCaseInsensitive) nên khớp cả
                // DISMISS/dismiss; contains() chỉ case-sensitive -> xếp sau làm lưới an toàn.
                // Ngoài ra FacebookHander.TryClickAnyDismiss còn quét Dismiss TRƯỚC cả vòng lặp
                // nhận diện, để bao trùm những list xpath cục bộ không chứa NavigationButton.
                "//*[@text=\"Dismiss\"]",
                "//*[@content-desc=\"Dismiss\"]",
                "//*[contains(@content-desc, \"Dismiss\")]",
                "//*[contains(@text, \"Dismiss\")]",
                // ── Nút consent COOKIE của Facebook ("Allow all cookies") — rất hay gặp
                // ngay sau khi đăng nhập. ĐÃ SỬA TYPO cũ "Alow all" (thiếu 1 chữ "l") khiến
                // KHÔNG BAO GIỜ khớp nút thật -> tool kẹt ở màn cookie. Dạng exact
                // @text=/@content-desc= được engine thử lại KHÔNG phân biệt hoa/thường
                // (TryFindBoundsCaseInsensitive chỉ áp dụng cho dạng [@attr='value'] đơn giản,
                // KHÔNG cho contains()), nên khớp cả "allow all cookies"/"ALLOW ALL COOKIES".
                // contains() thì CHỈ case-sensitive -> giữ làm phụ cho nhãn dài/nhúng.
                "//*[@text=\"Allow all cookies\"]",
                "//*[@content-desc=\"Allow all cookies\"]",
                "//*[contains(@text, \"Allow all cookies\")]",
                "//*[contains(@content-desc, \"Allow all cookies\")]",
                // Biến thể EU chỉ hiện nút "chấp nhận cookie thiết yếu" -> cũng đóng được màn.
                // LƯU Ý (đã cân nhắc kỹ): KHÔNG dùng contains(@text,"Allow all") chung chung, và
                // KHÔNG dùng @text="Allow" chung — cả hai sẽ khớp "Allow all the time"/"Allow" của
                // hộp thoại xin quyền Android TRƯỚC nút Deny/Don't allow bên dưới (FindElement trả
                // xpath khớp ĐẦU TIÊN), làm LẬT hành vi từ chối quyền (camera/danh bạ/vị trí) mà
                // danh sách này cố ý giữ. Chỉ khớp nhãn cookie ĐẦY ĐỦ "Allow all cookies".
                "//*[@text=\"Only allow essential cookies\"]",
                "//*[@content-desc=\"Only allow essential cookies\"]",
          // ── "Use for free with ads" ĐÃ DỜI XUỐNG cạnh khối Skip/Save ở cuối nhóm
          // (2026-09-08, theo yêu cầu) — xem ghi chú chi tiết tại đó.
          "//*[@content-desc=\"Manage quiet mode\"]",
          "//*[@text=\"Manage quiet mode\"]",

          "//*[@content-desc=\"End quiet mode\"]",
          "//*[@text=\"End quiet mode\"]",

          // ── DISMISS đã được DỜI LÊN ĐẦU nhóm NavigationButton (xem ghi chú ở đầu nhóm):
          // FindElement trả xpath khớp ĐẦU TIÊN theo thứ tự list nên đứng đầu = ưu tiên cao nhất.
         "//*[@text=\"I already have a profile\"]",
         "//*[@text=\"Use another profile\"]",
          "//*[@content-desc=\"I already have an account\"]",
          "//*[@text=\"Continue using English (US)\"]",
          "//*[@text=\"Get started\"]",
          "//*[@text=\"Log in\"]",
          "//*[@content-desc=\"Log in\"]",
          "//*[@text=\"Deny\"]",
          "//*[@content-desc=\"Next\"]",
          "//*[@text=\"Tiếp\"]",
          "//*[@text=\"Next\"]",
          // ── SKIP: exact (fallback hoa/thường khớp skip/SKIP) + các wording phụ + tiếng Việt.
          "//*[@text=\"Skip\"]",
          "//*[@content-desc=\"Skip\"]",
          "//*[@text=\"Skip for now\"]",
          "//*[@content-desc=\"Skip for now\"]",
          "//*[contains(@text, \"Skip\")]",
          "//*[contains(@content-desc, \"Skip\")]",
          "//*[@text=\"Bỏ qua\"]",
          "//*[@text=\"Lúc khác\"]",
          "//*[@text=\"Later\"]",
          "//*[@text=\"Not now\"]",
          "//*[@text=\"Don't allow\"]",
          "//*[@content-desc=\"Not now\"]",
          // ── SAVE: exact @text + @content-desc (fallback hoa/thường khớp Save/SAVE/save).
          "//*[@text=\"Save\"]",
          "//*[@content-desc=\"Save\"]",
          "//*[@text=\"SAVE\"]",
          // LƯỚI contains() bắt nút confirm "Save info"/"Save login info" của dialog
          // "Save your login info?" — CHỈ contains mới khớp nhãn dài; exact @text="Save"
          // ở trên KHÔNG bắt được, và trong repo KHÔNG có entry exact nào cho "Save info".
          // 2026-09-21: THÊM not(contains(@text,"post")) vì trên newsfeed node action-bar có
          // text="Save post" (nút bookmark nhỏ cạnh Like/Comment/Share, verify LIVE trên
          // 5200ef68feda15cf: Button clickable=true bounds=[1106,1570][1440,1724]) ->
          // contains(@text,"Save") CŨ khớp và BẤM NHẦM nó. Nhóm NavigationButton được nạp
          // TOÀN CỤC ở mọi vòng farming (FacebookFarming.cs:9653, FacebookService.cs, …) nên
          // lỗi lan khắp nơi. Loại chữ "post" nhưng GIỮ "Save info" (dialog login-info không
          // chứa "post"). contains()+and → CanUseManualAttributeFallback=false (ADBClient.cs:4094)
          // → chỉ chạy XPath thật, contains() case-SENSITIVE, not() được tôn trọng, không bị
          // fallback hoa/thường phá luật loại trừ.
          "//*[contains(@text, \"Save\") and not(contains(@text, \"post\"))]",
          // ── META "USE FOR FREE WITH ADS": DỜI XUỐNG ĐÂY (cạnh Skip/Save) 2026-09-08 theo
          // yêu cầu. Trước đây 2 entry này đứng ở ĐẦU nhóm (ngay sau "Only allow essential
          // cookies") -> với luật XPATH-KHỚP-ĐẦU-TIÊN của ADBClient.FindElement chúng THẮNG và
          // bị bấm THƯỜNG (plain tap), mà plain tap KHÔNG chọn được radio của thẻ option — phải
          // HOLD-TAP ~200ms lên THẺ (xem FacebookHander.MetaFreeOptionXpaths) -> tool kẹt.
          // Màn consent THẬT do nhóm MetaAdsConsent nhận diện (đứng TRƯỚC NavigationButton
          // trong GetActiAccountFacebook) và được xử lý đúng trình tự trong
          // FacebookHander.TryHandleMetaAdsConsentAsync. Hai entry contains() ở đây chỉ là
          // LƯỚI AN TOÀN cho những vòng lặp cục bộ không nạp MetaAdsConsent; đặt SAU Skip/Save
          // để không cướp lượt của những nút bấm-thật-sự-được. CỐ Ý KHÔNG thêm
          // contains("Use free of charge with ads") vào nhóm này: plain tap cũng vô ích y hệt,
          // thêm vào chỉ tăng nguy cơ khớp nhầm node nền.
          "//*[contains(@content-desc, \"Use for free with\")]",
          "//*[contains(@text, \"Use for free with\")]",
          "//*[@text=\"No thanks\"]",
          "//*[@content-desc=\"No thanks\"]",
          "//*[@text=\"OK\"]",
          "//*[@content-desc=\"Continue in English (US)\"]",
          "//*[@content-desc=\"Continue\"]",
          "//*[@text=\"Continue\"]",
          "//*[@text=\"Close app\"]",
      });
            // Hộp thoại Meta "pay or consent" (EU): màn bắt chọn giữa trả phí
            // ("Subscribe to use without ads") hoặc dùng miễn phí có quảng cáo
            // ("Use free of charge with ads"). Nút Continue CHỈ bật sau khi đã chọn
            // một option, nên KHÔNG được gộp các xpath này vào NavigationButton:
            // phần tử khớp ĐẦU TIÊN của nhóm đó trên màn này chính là Continue mờ
            // (click vào nút disabled không làm gì) -> tool kẹt mãi.
            // Các vòng lặp nhận diện màn qua nhóm này rồi gọi
            // FacebookHander.TryHandleMetaAdsConsentAsync (2 bước: chọn option miễn
            // phí -> bấm Continue).
            _xpathGroups.TryAdd(XpathType.MetaAdsConsent, new List<string>
      {
          "//*[contains(@text, \"Want to subscribe or continue\")]",
          "//*[contains(@content-desc, \"Want to subscribe or continue\")]",
          "//*[contains(@text, \"Subscribe to use without ads\")]",
          "//*[contains(@content-desc, \"Subscribe to use without ads\")]",
          "//*[contains(@text, \"Use free of charge with ads\")]",
          "//*[contains(@content-desc, \"Use free of charge with ads\")]",
          "//*[contains(@text, \"Use for free with\")]",
          "//*[contains(@content-desc, \"Use for free with\")]",
          // ── BƯỚC 2 của consent: màn "To use our Products for free with ads, AGREE to
          // Meta using your info…" với nút xanh "Agree" ở đáy. Tiêu đề dùng chữ "agree"
          // thường nên contains(@text,"Use for free with") KHÔNG khớp -> phải nhận diện
          // thẳng bằng nút Agree. Dạng exact @content-desc=/@text= được engine fallback
          // hoa/thường; còn việc tránh node View 1px text="Agree" (decoy) do
          // TryHandleMetaAdsConsentAsync chọn bounds LỚN NHẤT đảm nhiệm.
          "//*[@content-desc=\"Agree\"]",
          "//*[@text=\"Agree\"]",
          // ── BƯỚC 3 của consent (xác minh LIVE 2026-09-08, device 52003dc05fab94ab) ──
          // Sau khi bấm Agree hiện màn "Your current experience" / "You can manage your ad
          // experience": 2 nút trong nội dung ("Continue with personalized ads"
          // [56,1066][1384,1194] area=169984, "Switch to less-personalized ads") + nút OK ở
          // footer. Trên màn này "Agree"=0 và "Radio button"=0 nên nhóm MetaAdsConsent cũ
          // KHÔNG khớp bất cứ xpath nào -> TryHandleMetaAdsConsentAsync break sớm, trả true,
          // và màn bị phó mặc cho NavigationButton. Trong 127 xpath phẳng của
          // GetActiAccountFacebook chỉ có ĐÚNG 1 xpath khớp là //*[@text="OK"] của
          // NavigationButton, và node đó là View decoy clickable="false" [681,2377][759,2392]
          // area=1170 — Button thật content-desc="OK" [56,2336][1384,2392] area=74368 KHÔNG
          // đặt @text nên @text="OK" không bao giờ thấy nó. Qua được màn này chỉ vì tâm decoy
          // (720,2384) tình cờ nằm lọt trong bounds Button thật: ăn may về hình học, vỡ ngay
          // khi layout/độ phân giải đổi. => Nhận diện thẳng màn này để handler tự xử lý.
          //
          // CỐ Ý KHÔNG dùng "OK" hay "Continue" chung chung: cả hai xuất hiện ở rất nhiều
          // dialog khác, thêm vào sẽ khiến handler consent bị kích hoạt nhầm trên màn không
          // phải consent. Chỉ dùng nhãn ĐẶC HIỆU của riêng màn này.
          "//*[contains(@content-desc, \"Continue with personalized ads\")]",
          "//*[contains(@content-desc, \"Switch to less-personalized ads\")]",
          "//*[contains(@content-desc, \"You can manage your ad experience\")]",
          "//*[contains(@text, \"Your current experience\")]",
          "//*[contains(@content-desc, \"Your current experience\")]",
      });
            _xpathGroups.TryAdd(XpathType.TowFA, new List<string>
      {
           "//*[@content-desc=\"Check your email\"]",
          "//*[@text=\"Check your notifications on another device\"]",
          $"//*[contains(@text, \"Generate a code from your authentication app and enter it to log in\")]",
          $"//*[contains(@text, \"Check your notifications on another device\")]",
          $"//*[contains(@text, \"Authentication app, Get a code from your authentication app.\")]",
          $"//*[contains(@text, \"Go to your authentication app\")]",
      });
            _xpathGroups.TryAdd(XpathType.CashApp, new List<string>
      {
          $"//*[contains(@text, \"Session Expired\")]",
          "//*[@text=\"Facebook keeps stopping\"]",
      });
            _xpathGroups.TryAdd(XpathType.InputUserName, new List<string>
      {
          "//*[@text=\"Use another profile\"]",
          $"//*[contains(@text, \"Phone or email\")]",
          $"//*[contains(@text, \"Mobile number or email\")]",
          $"//*[@text=\"Log into another account\"]",
          "//*[@text=\"Mobile number or email\"]",
          $"//*[contains(@text, \"Create new account\")]",
      });
            _xpathGroups.TryAdd(XpathType.InputPassword, new List<string>
      {
                 "//*[@content-desc=\"Password\"]",
          $"//*[contains(@text, \"Enter Password\")]",
      });
            _xpathGroups.TryAdd(XpathType.Regsiner_Facebook, new List<string>
            {

                "//*[@text=\"Male\"]",
                "//*[@text=\"Female\"]",
                "//*[@text=\"Add a mobile number to your account\"]",
                "//*[@text=\"Add a profile picture\"]",
                "//*[@text=\"Save login info?\"]",
                // LIVE 2026-09-10 trên 520058f34d7c947b: dialog thực tế là "Save your login
                // info?" (CÓ "your") -> entry exact "Save login info?" ở trên KHÔNG bao giờ
                // khớp. Giữ cả hai vì Meta đổi wording theo locale/version.
                "//*[@text=\"Save your login info?\"]",
                "//*[@content-desc=\"Save your login info?\"]",
                "//*[@text=\"Add friends\"]",
                "//*[@text=\"Turn on contact uploading\"]",
                "//*[@text=\"What is your mobile number?\"]",
                "//*[@text=\"Sign up with email\"]",
                "//*[@text=\"Choose your name\"]",
                "//*[@content-desc=\"Go to profile\"]",
                "//*[@text=\"This page is currently not displayed\"]",
                "//*[@content-desc=\"Appeal\"]",
                "//*[@text=\"Enter text\"]",
                "//*[@text=\"I agree\"]",
                "//*[@text=\"Select your name\"]",
                "//*[@text=\"Couldn't create account\"]",
                "//*[@text=\"Enter email\"]",
                "//*[@text=\"Confirm with email\"]",
                "//*[@text=\"I didn't receive a code\"]",
                "//*[@text=\"Please log in again.\"]",
                "//*[@text=\"IMPORT CONTACTS\"]",
                "//*[@text=\"Sign up\"]",
                "//*[@text=\"Get started\"]",
                "//*[@text=\"Create new account\"]",
                "//*[@content-desc=\"Create new account\"]",
                "//*[@content-desc=\"Join Facebook\"]",
                "//*[@content-desc=\"No, create new account\"]",
                "//*[@content-desc=\"Create new Facebook account\"]",
                "//*[@text=\"Continue creating account\"]",
                "//*[@content-desc=\"Continue creating account\"]",
                "//*[@text=\"No, create account\"]",
                "//*[@text=\"First name\"]",
                "//*[@text=\"What's your name?\"]",
                "//*[@text=\"When is your date of birth?\"]",
                "//*[@text=\"SET\"]",
                "//*[@text=\"What is your gender?\"]",
                "//*[@text=\"What is your email?\"]",
                "//*[@text=\"Sign up with mobile number\"]",
                "//*[@text=\"Create a password\"]",
                "//*[contains(@text, \"sent to\")]",

            });
            _xpathGroups.TryAdd(XpathType.ExistEmail, new List<string>
            {
                "//*[@text=\"This Page Isn't Available Right Now\"]",
                "//*[@text=\"There is already an account linked to this email address.\"]",
                "//*[@text=\"An account already exists linked to this email.\"]",
            });
            _xpathGroups.TryAdd(XpathType.Confim_Register, new List<string>
            {
                "//*[@text=\"Continue creating account\"]",
 "//*[@text=\"Confirm by email\"]",
 "//*[@text=\"Enter an email\"]",
 "//*[@text=\"I didn’t get the code\"]",
 "//*[@text=\"Continue using English (US)\"]",

 "//*[@text=\"Enter email\"]",
 "//*[@text=\"Confirm with email\"]",
 "//*[@text=\"I didn't receive a code\"]",
 "//*[@text=\"I agree\"]",
 "//*[@text=\"Please log in again.\"]",
 "//*[@text=\"Sign up\"]",
 "//*[@text=\"Create new account\"]",
 "//*[@text=\"Create new account\"]",
 "//*[@content-desc=\"Create new account\"]",
      "//*[@text=\"Get started\"]",
 "//*[@content-desc=\"Join Facebook\"]",
 "//*[@text=\"Get started\"]",
 "//*[@content-desc=\"No, create new account\"]",
            });
        }
        public static List<string> Get(XpathType group)
        {
            if (!_xpathGroups.TryGetValue(group, out var list))
                return new List<string>();
            // Snapshot dưới lock vì LoadFromApiCoreAsync có thể merge vào list
            // đồng thời với lúc reader đọc (List<string> không thread-safe).
            lock (list)
                return new List<string>(list);
        }

        public static List<string> Combine(params object[] groupsOrXpaths)
        {
            var result = new List<string>();
            foreach (var item in groupsOrXpaths)
            {
                if (item is XpathType groupName)
                    result.AddRange(Get(groupName));
                else if (item is IEnumerable<string> list)
                    result.AddRange(list);
            }
            return result;
        }

        public static void AddCustomGroup(XpathType key, List<string> xpaths)
        {
            _xpathGroups[key] = xpaths;
        }

        public static Task LoadFromApiAsync(string apiUrl = "https://dev.subdy.net/api/case")
        {
            lock (_loadLock)
            {
                return _loadTask ??= LoadFromApiCoreAsync(apiUrl);
            }
        }

        private static async Task LoadFromApiCoreAsync(string apiUrl)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                var json = await client.GetStringAsync(apiUrl);
                var root = JsonNode.Parse(json);

                if (root?["success"]?.GetValue<bool>() != true) return;

                var dataArray = root["data"]?.AsArray();
                if (dataArray == null) return;

                foreach (var item in dataArray)
                {
                    var casename = item?["casename"]?.GetValue<string>();
                    var listcase = item?["listcase"]?.AsArray();

                    if (casename == null || listcase == null) continue;
                    if (!Enum.TryParse<XpathType>(casename, true, out var xpathType)) continue;

                    var incoming = listcase
                        .Select(x => x?.GetValue<string>())
                        .Where(x => x != null)
                        .Select(x => x!)
                        .ToList();

                    if (_xpathGroups.TryGetValue(xpathType, out var existing))
                    {
                        lock (existing)
                        {
                            foreach (var xpath in incoming)
                                if (!existing.Contains(xpath))
                                    existing.Add(xpath);
                        }
                    }
                    else
                    {
                        _xpathGroups.TryAdd(xpathType, incoming);
                    }
                }
            }
            catch { }
        }
    }
}
