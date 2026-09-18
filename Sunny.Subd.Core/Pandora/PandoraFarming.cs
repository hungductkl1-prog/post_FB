using AutoAndroid;
using Sunny.Subd.Core.Models;
using Sunny.Subd.Core.Services;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace Sunny.Subd.Core.Pandora
{
    /// <summary>
    /// Pandora music farming. Cùng kiến trúc với <see cref="Facebook.FacebookFarming"/> /
    /// <see cref="Instagram.InstagramFarming"/>:
    /// - Lấy Script theo Account.NameScript, sequence ScriptAction theo ByOrder.
    /// - Tôn trọng cấu hình fQuanLyKichBan (checkBox1..4, timeout, delay giữa các hành động).
    /// - Mọi thao tác device đi qua API dùng chung của <see cref="ADBClient"/>
    ///   (GetXMLSource / ElementWithAttributes / SendTextSlow / Package / AppStart / Click / Delay).
    /// Proxy theo account do MainService.ConnectAndPrepareDeviceAsync xử lý trước khi vào đây.
    ///
    /// XPath được lấy trực tiếp trên máy thật (5200d004b263c49d, Pandora free account):
    ///   Welcome        : welcome_log_in_button
    ///   LogIn          : email_editText / password_editText / cta
    ///   FTUE           : create_station_fab
    ///   BottomNav      : tab_search, search_src_text, tab kết quả desc="Songs"
    ///   Kết quả tìm    : collection_data_holder + auxiliary_button (desc="Play")
    ///   Gate free user : "Play This Song?" -> "Start Station" (bỏ qua "Watch Ad")
    ///   Now playing    : now_playing_toolbar (full) / mini_player_handle (thu nhỏ),
    ///                    play (desc="Pause"/"Play")
    /// </summary>
    public class PandoraFarming
    {
        private readonly ADBClient _client;
        private readonly Account _account;
        private readonly ConfigModel _config;
        private readonly MainService _mainService;
        private readonly ScriptContext _scriptContext;
        private readonly ScriptActionContext _scriptActionContext;
        private readonly JsonHelper _configKichBan;
        private readonly Dictionary<string, object> _setting = new();
        // [TIME-LIMIT v25] Hạn mức thời gian đo từ MỐC MainService bấm lúc BẮT ĐẦU CHUẨN BỊ tài
        // khoản nên TÍNH CẢ thời gian chuẩn bị acc. Xem AccountLimitElapsed trong MainService.
        private TimeSpan LimitElapsed => _mainService.AccountLimitElapsed;
        private Script _script;

        /// <summary>Bật log chi tiết ra Debug output (PandoraDebugSetup dùng cờ này).</summary>
        public static bool DebugMode { get; set; } = false;

        private const string PKG = "com.pandora.android";

        // ================= Ngân sách thời gian =================
        // Mọi vòng lặp state-machine ở đây tự dump XML 1 lần rồi so khớp TRONG BỘ NHỚ
        // (Exists/MatchCase với timeout 0). Lý do: FindElement/ElementWithAttributes của
        // ADBClient khi KHÔNG match sẽ bỏ snapshot đang có rồi dump lại liên tục cho tới
        // hết timeout. Mỗi dump trên màn Pandora (carousel động, video quảng cáo) tốn
        // vài giây, nên 1 lượt miss với timeout=20 ngốn cả 20 giây. Cộng dồn qua login
        // + gate + nhiều keyword là ra 3-10 phút chết đứng như báo lỗi.

        /// <summary>Nhịp nghỉ giữa 2 snapshot trong các vòng chờ (giây).</summary>
        private const int PollSeconds = 2;

        /// <summary>Trần thời gian đưa app từ trạng thái hiện tại về ô tìm kiếm.</summary>
        private const int SearchReadyTimeout = 30;

        /// <summary>Trần chờ gate "Play This Song?" hiện đủ nút để bấm (trước khi xem ads).</summary>
        private const int GateBeforeAdTimeout = 30;

        /// <summary>Trần chờ quảng cáo reward chạy xong (thực đo ~20-25s).</summary>
        private const int AdWatchTimeout = 75;

        /// <summary>Trần cho toàn bộ quá trình từ lúc bấm Play tới lúc nhạc chạy.</summary>
        private const int GateTimeout = 120;

        /// <summary>Trần chờ danh sách kết quả THẬT của keyword hiện tại xuất hiện (giây).</summary>
        private const int ResultWaitTimeout = 25;

        /// <summary>Số lần gõ lại keyword trong 1 lượt SearchAndPlay khi kết quả không về / không khớp.</summary>
        private const int MaxSearchAttempts = 2;

        /// <summary>
        /// Sau khi bấm Play, chờ tối đa bấy nhiêu giây để player hiện ĐÚNG bài đã chọn.
        /// Quá mốc này mà vẫn là bài khác (bài cũ còn chạy, Pandora tự nhảy bài) thì coi như
        /// phát sai bài -> trả false để keyword được tính là lỗi, thay vì nghe nhầm 120s.
        /// </summary>
        private const int WrongTrackTimeout = 30;

        // ---------- XPath đã verify trên thiết bị thật ----------
        private const string X_WELCOME_LOGIN = "//*[@resource-id=\"com.pandora.android:id/welcome_log_in_button\"]";
        private const string X_SECONDARY_CTA = "//*[@resource-id=\"com.pandora.android:id/secondary_cta\"]";
        private const string X_EMAIL = "//*[@resource-id=\"com.pandora.android:id/email_editText\"]";
        private const string X_PASSWORD = "//*[@resource-id=\"com.pandora.android:id/password_editText\"]";
        private const string X_CTA = "//*[@resource-id=\"com.pandora.android:id/cta\"]";
        private const string X_FTUE_FAB = "//*[@resource-id=\"com.pandora.android:id/create_station_fab\"]";
        private const string X_BOTTOM_NAV = "//*[@resource-id=\"com.pandora.android:id/bottom_navigation\"]";
        private const string X_TAB_SEARCH = "//*[@resource-id=\"com.pandora.android:id/tab_search\"]";
        private const string X_SEARCH_INPUT = "//*[@resource-id=\"com.pandora.android:id/search_src_text\"]";
        private const string X_SEARCH_CLEAR = "//*[@resource-id=\"com.pandora.android:id/search_close_btn\"]";
        private const string X_TAB_SONGS_UNSELECTED = "//*[@content-desc=\"Songs\" and @clickable=\"true\"]";
        private const string X_RESULT_ROW = "//*[@resource-id=\"com.pandora.android:id/collection_data_holder\"]";

        // ── Màn Search có 2 layout dùng CHUNG collection_data_holder + auxiliary_button ──
        //   :id/search_history -> tab "RECENT" (lịch sử tìm kiếm), hiện NGAY khi mở tab Search
        //   :id/search_results -> kết quả thật của query đang gõ (kèm tab ALL/ARTISTS/SONGS/...)
        // Dump thật trên 5200d004b263c49d: vừa bấm tab Search, ô tìm kiếm còn rỗng
        // (search_src_text text="Search" = hint) mà ĐÃ có 3 row RECENT chính là 3 bài của các
        // keyword trước (STAY / Dance Monkey / Blinding Lights). Vì vậy điều kiện "X_RESULT_ROW
        // tồn tại" đúng ngay lập tức -> tap auxiliary_button[0] = phát lại bài của keyword
        // TRƯỚC. Đây chính là lỗi keyword 5 "Someone Like You" phát lại "STAY" của keyword 4.
        private const string X_SEARCH_RESULTS = "//*[@resource-id=\"com.pandora.android:id/search_results\"]";
        private const string X_SEARCH_HISTORY = "//*[@resource-id=\"com.pandora.android:id/search_history\"]";

        // Node con trong 1 row kết quả (dump thật, đường dẫn tương đối để chạy trên node row):
        //   collection_item_title_text     text="Someone Like You"
        //   collection_item_subtitle_text1 text="Adele"
        //   collection_item_subtitle_text2 text="Song - 4:45"   (row album: "Album - 12 songs")
        //   auxiliary_button               content-desc="Play"  (row đang phát: "Pause")
        private const string X_ROW_TITLE = ".//*[@resource-id=\"com.pandora.android:id/collection_item_title_text\"]";
        private const string X_ROW_ARTIST = ".//*[@resource-id=\"com.pandora.android:id/collection_item_subtitle_text1\"]";
        private const string X_ROW_KIND = ".//*[@resource-id=\"com.pandora.android:id/collection_item_subtitle_text2\"]";
        private const string X_ROW_PLAY = ".//*[@resource-id=\"com.pandora.android:id/auxiliary_button\"]";
        private const string X_NOW_PLAYING = "//*[@resource-id=\"com.pandora.android:id/mini_player_content\"]";
        private const string X_NOW_PLAYING_BAR = "//*[@resource-id=\"com.pandora.android:id/now_playing_toolbar\"]";
        private const string X_PLAY_PAUSE = "//*[@resource-id=\"com.pandora.android:id/play\"]";
        private const string X_PAUSED = "//*[@resource-id=\"com.pandora.android:id/play\" and @content-desc=\"Play\"]";
        private const string X_COACHMARK = "//*[@resource-id=\"com.pandora.android:id/webview_coachmark\"]";

        // ── In-app message / promo Pandora Plus phủ TOÀN MÀN ─────────────────────────
        // Dump thật trên máy 5200d004b263c49d sau vài bài (adb dumpsys window):
        //   com.pandora.android/com.urbanairship.iam.html.HtmlActivity
        // Đây là Activity RIÊNG của Urban Airship đè lên Pandora, nên cây view CHỈ còn
        // webview promo, không còn bottom nav / mini player / ô tìm kiếm:
        //   com.pandora.android:id/dismiss   content-desc="Cancel"  <- nút đóng duy nhất
        //   u_body / u_row_* / u_column_* / u_content_image_*  (id webview, KHÔNG prefix package)
        //   4 ô ảnh giữa màn (Mellow Pop, Y2 Pop, Pop Cafe, Get Happy) đều clickable="true"
        //   -> tap bừa vào giữa màn là rơi thẳng vào luồng mua Pandora Plus.
        // Bảng xpath cũ KHÔNG có case nào khớp màn này: X_DISMISS tìm "Close" còn nút đóng
        // ở đây là "Cancel", X_COACHMARK là webview_coachmark. Hậu quả: HandlePlayGate /
        // ListenAsync / EnsureSearchScreen đều rơi vào nhánh "không match case nào" và nằm
        // chờ hết timeout — đúng hiện tượng kẹt khi chuyển sang keyword kế tiếp.
        private const string X_PROMO_DISMISS = "//*[@resource-id=\"com.pandora.android:id/dismiss\"]";
        private static readonly List<string> X_PROMO = new()
        {
            X_PROMO_DISMISS,
            // Thân webview của template Airship — nhận diện cả biến thể không có nút đóng
            // trong cây view (lúc đó chỉ còn cách Back).
            "//*[@resource-id=\"u_body\"]",
        };
        // Chỉ dùng sau khi đã nhận diện đúng cấu trúc promo qua X_PROMO. Giữ "Cancel"
        // ngoài X_DISMISS/X_AD_CLOSE để không bấm nhầm ở gate hoặc quảng cáo reward.
        private static readonly List<string> X_PROMO_CLOSE = new()
        {
            X_PROMO_DISMISS,
            "//*[@text=\"Cancel\"]",
            "//*[contains(@content-desc,\"Cancel\")]",
        };

        /// <summary>Số lần tối đa chịu đóng promo trong 1 lượt mở bài trước khi nhường keyword kế.</summary>
        private const int MaxPromoPerPlay = 3;

        /// <summary>Số lần tối đa chịu đóng promo trong 1 bài đang nghe.</summary>
        private const int MaxPromoPerSong = 3;

        /// <summary>Sau khi đóng promo, chờ bấy nhiêu giây mà không thấy gate/player thì coi như mất lượt phát.</summary>
        private const int PromoRecoverSeconds = 20;

        // ── Gate free user (webview, resource-id KHÔNG có prefix package) ──────────────
        // Đã dump thật trên máy 5200d004b263c49d:
        //   offer-header        text="Play This Album?"  (biến thể khác: "Play This Song?")
        //   reward              text="Watch Ad"      <- BẮT BUỘC bấm
        //   start-station-button text="No, Thanks"   <- CÙNG id nhưng 2 vai trò tùy biến thể!
        //   upsell-copy         text="Start Free Trial"
        //   close-button        text="Close"
        // Vì vậy KHÔNG match gate bằng @text (đổi theo Song/Album) mà bằng resource-id.
        private const string X_OFFER_HEADER = "//*[@resource-id=\"offer-header\"]";

        // start-station-button mang text "Start Station" (mở radio gợi ý — fallback dùng được)
        // HOẶC "No, Thanks" (hủy lượt phát — TUYỆT ĐỐI KHÔNG bấm). Cùng resource-id nên
        // phải lọc theo text; chỉ xpath dưới đây mới an toàn để tap.
        private const string X_START_STATION = "//*[@resource-id=\"start-station-button\" and contains(@text,\"Start Station\")]";

        // Các nút trên gate làm mất lượt phát — chỉ dùng để nhận diện, không bao giờ tap.
        private static readonly List<string> X_GATE_DECLINE = new()
        {
            "//*[@resource-id=\"start-station-button\" and contains(@text,\"Thanks\")]",
            "//*[@resource-id=\"close-button\"]",
            "//*[@resource-id=\"upsell-copy\"]",
        };

        // Gate free user: BẮT BUỘC bấm "Watch Ad" (reward) để được phát đúng bài đã tìm.
        private static readonly List<string> X_WATCH_AD = new()
        {
            "//*[@resource-id=\"reward\"]",
            "//*[@text=\"Watch Ad\"]",
            "//*[contains(@text,\"Watch Ad\")]",
            "//*[contains(@text,\"Watch a video\")]",
            "//*[contains(@content-desc,\"Watch Ad\")]",
        };

        // Trong lúc quảng cáo chạy: nút đóng/skip CHỈ bấm được khi đã hết thời lượng bắt buộc,
        // nên bắt buộc có @clickable="true" — lúc đếm ngược nút vẫn tồn tại nhưng disabled.
        // Thực tế trên máy: quảng cáo reward tự hết rồi vào bài luôn, không cần bấm gì.
        // Giữ nhóm này cho biến thể quảng cáo có nút đóng.
        private static readonly List<string> X_AD_CLOSE = new()
        {
            "//*[@resource-id=\"com.pandora.android:id/close_ad\" and @clickable=\"true\"]",
            "//*[@resource-id=\"com.pandora.android:id/l1_close_button\" and @clickable=\"true\"]",
            "//*[@content-desc=\"Skip Ad\" and @clickable=\"true\"]",
            "//*[@content-desc=\"Close ad\" and @clickable=\"true\"]",
            "//*[@content-desc=\"Close advertisement\" and @clickable=\"true\"]",
            "//*[@text=\"Skip Ad\" and @clickable=\"true\"]",
        };

        // Một số SDK quảng cáo không expose clickable trên nút Skip. Chỉ xét nhóm raw này
        // SAU khi chính luồng hiện tại đã bấm Watch Ad, không dùng ở gate hoặc overlay chung.
        // Loại mọi node thuộc Pandora player: nút tua lùi của player có content-desc chứa
        // "Skip" nhưng resource-id mang prefix com.pandora.android:id/.
        private static readonly List<string> X_AD_SKIP_FALLBACK = new()
        {
            "//*[@text=\"Skip\" and not(contains(@resource-id,\"com.pandora.android:id/\"))]",
            "//*[contains(@content-desc,\"Skip\") and not(contains(@resource-id,\"com.pandora.android:id/\"))]",
        };

        // Dấu hiệu đang ở màn quảng cáo (chờ, KHÔNG tap bừa).
        // Đã dump thật khi bấm Watch Ad: sl_video_ad_view_wrapper / sl_video_ad_view /
        // video_view_pandora / sl_banner_ad, sau đó là dialog "Loading…"/"Please wait…".
        private static readonly List<string> X_AD_PLAYING = new()
        {
            "//*[@resource-id=\"com.pandora.android:id/sl_video_ad_view_wrapper\"]",
            "//*[@resource-id=\"com.pandora.android:id/sl_video_ad_view\"]",
            "//*[@resource-id=\"com.pandora.android:id/video_view_pandora\"]",
            "//*[@resource-id=\"com.pandora.android:id/sl_banner_ad\"]",
            // KHÔNG dùng :id/audio_ad_display_view — dump thật cho thấy nó có sẵn ở màn Home
            // khi KHÔNG có quảng cáo nào, sẽ làm HandlePlayGate chờ vô ích hết 180s.
            // Dùng contains: text thật là "Please wait…" với ký tự ellipsis U+2026,
            // so khớp tuyệt đối dễ lệch do encoding.
            "//*[contains(@text,\"Please wait\")]",
            "//*[contains(@text,\"Your song will play after\")]",
            "//*[contains(@text,\"seconds remaining\")]",
            "//*[contains(@content-desc,\"Advertisement\")]",
        };

        // Pandora có 2 layout Now Playing khác nhau:
        //   layout/track_view          (radio/station) -> :id/title,       :id/artist
        //   layout/premium_track_view  (on-demand)     -> :id/track_title, :id/track_artist
        // Khi player thu nhỏ, cả 2 đều rỗng và tên bài nằm ở mini_player_handle content-desc.
        // Lưu ý: :id/track_title là TextSwitcher, tên bài nằm ở content-desc (text rỗng).
        // KHÔNG thêm :id/toolbar_title: nó cũng là tiêu đề trang backstage/tìm kiếm.
        // BẮT BUỘC lọc class TextSwitcher: dump thật trên màn tìm kiếm có node
        // :id/title class=TextView text="The latest Wednesday picks" (tiêu đề mục, KHÔNG
        // phải tên bài). ReadAttr lấy node[0] nên nếu không lọc sẽ đọc nhầm tiêu đề đó
        // -> ListenAsync tưởng đã đổi bài và cắt bài giữa chừng.
        // Tên bài thật luôn nằm trong TextSwitcher (track_title / title của player).
        private static readonly List<string> X_TRACK_TITLE = new()
        {
            "//*[@resource-id=\"com.pandora.android:id/track_title\" and @class=\"android.widget.TextSwitcher\"]",
            "//*[@resource-id=\"com.pandora.android:id/track_title\"]",
            "//*[@resource-id=\"com.pandora.android:id/title\" and @class=\"android.widget.TextSwitcher\"]",
        };
        private const string X_MINI_HANDLE = "//*[@resource-id=\"com.pandora.android:id/mini_player_handle\"]";

        // Thanh tiến trình bài hát trên player full — nguồn tin cậy nhất để biết hết bài.
        // Dump thật: progress_elapsed_text="1:09", progress_remaining_text="3:29" (tổng thời lượng).
        private const string X_PROGRESS_ELAPSED = "//*[@resource-id=\"com.pandora.android:id/progress_elapsed_text\"]";
        private const string X_PROGRESS_TOTAL = "//*[@resource-id=\"com.pandora.android:id/progress_remaining_text\"]";

        // Các popup/coachmark 1 lần cần dismiss.
        // CẢNH BÁO: "Close"/"content-desc=Close" cũng khớp nút close-button của gate và
        // nút đóng quảng cáo -> DismissOverlays phải tự chặn khi đang ở 2 màn đó.
        private static readonly List<string> X_DISMISS = new()
        {
            "//*[@resource-id=\"com.pandora.android:id/voice_callout_dismiss\"]",
            "//*[@resource-id=\"com.pandora.android:id/callout_dismiss\"]",
            "//*[@resource-id=\"com.pandora.android:id/mini_coachmark\"]",
            "//*[@content-desc=\"Close\"]",
            "//*[@text=\"Close\"]",
        };

        // ── Skip / Skip Anyway ────────────────────────────────────────────────
        // Theo yêu cầu: bất cứ khi nào gặp text/content-desc chứa "skip" hay "skip anyway"
        // (vd dialog xác nhận skip của Pandora chen ngang) thì bấm. FindElements hạ lowercase
        // cả xml lẫn xpath nên chữ hoa/thường đều khớp. KHÔNG bấm khi đang ở gate phát bài
        // hay quảng cáo reward — nút Skip Ad là của quảng cáo, chỉ HandlePlayGate được bấm
        // sau khi hết thời lượng bắt buộc.
        private static readonly List<string> X_SKIP_ANYWAY = new()
        {
            "//*[contains(@text,\"skip anyway\")]",
            "//*[contains(@content-desc,\"skip anyway\")]",
        };
        private static readonly List<string> X_SKIP_ANY = new()
        {
            "//*[contains(@text,\"skip\")]",
            "//*[contains(@content-desc,\"skip\")]",
        };

        public PandoraFarming(MainService mainService)
        {
            _client = mainService._client;
            _account = mainService._account;
            _mainService = mainService;
            _config = mainService._config;
            _scriptContext = new ScriptContext();
            _scriptActionContext = new ScriptActionContext();
            _configKichBan = SettingsTool.GetSettings($"fQuanLyKichBan_{mainService._platform}", true);
        }

        private static void Log(string message)
        {
            if (DebugMode) Debug.WriteLine($"[Pandora] {message}");
        }

        private bool Stop()
        {
            if (_setting.ContainsKey("timeoutTaiKhoan")
                && LimitElapsed >= TimeSpan.FromMinutes(Convert.ToInt32(_setting["timeoutTaiKhoan"])))
            {
                _mainService.SetStatus($"Đã quá {_setting["timeoutTaiKhoan"]} phút cho tài khoản này!", 2);
                AutoAndroid.RunHistoryLog.Note(_client?.Device?.Serial ?? "?",
                    $"[TIMEOUT] uid={_account?.Uid} | giới hạn thời gian mỗi tài khoản ({_setting["timeoutTaiKhoan"]} phút, TÍNH CẢ chuẩn bị acc) đã hết sau {Math.Round(LimitElapsed.TotalMinutes, 1)} phút -> đổi tài khoản khác.");
                return true;
            }
            if (_setting.ContainsKey("timeoutKichBan")
                && LimitElapsed >= TimeSpan.FromMinutes(Convert.ToInt32(_setting["timeoutKichBan"])))
            {
                _mainService.SetStatus($"Đã quá {_setting["timeoutKichBan"]} phút cho kịch bản này!", 2);
                AutoAndroid.RunHistoryLog.Note(_client?.Device?.Serial ?? "?",
                    $"[TIMEOUT] uid={_account?.Uid} | giới hạn thời gian mỗi kịch bản ({_setting["timeoutKichBan"]} phút, TÍNH CẢ chuẩn bị acc) đã hết sau {Math.Round(LimitElapsed.TotalMinutes, 1)} phút -> đổi tài khoản khác.");
                return true;
            }
            return false;
        }

        // ================= Entry point =================

        public async Task ExecuteAsync()
        {
            string scriptName = _account?.NameScript?.Trim() ?? "";
            if (string.IsNullOrEmpty(scriptName))
            {
                var all = _scriptContext.GetByPlatform(_mainService._platform) ?? new List<Script>();
                _script = all.FirstOrDefault(s => !ScriptNames.IsBuiltIn(s.Name));
            }
            else
            {
                _script = _scriptContext.GetByName(scriptName, _mainService._platform);
            }

            if (_script == null)
            {
                _mainService.SetStatus("Không tồn tại kịch bản.", 1);
                return;
            }

            var actions = _scriptActionContext.GetByScriptId(_script.Id);
            if (actions == null || !actions.Any())
            {
                _mainService.SetStatus("Không có hành động nào cả.", 1);
                return;
            }

            actions = actions.OrderBy(x => x.ByOrder).ToList();
            if (_configKichBan.GetBooleanValue("checkBox1"))
            {
                actions = actions.OrderBy(_ => Guid.NewGuid()).ToList();
            }
            // ── GIỚI HẠN THỜI GIAN CHẠY (fix 2026-09-16: mapping lệch sau khi UI đổi tên control) ──
            // checkBox2 = "Giới hạn thời gian chạy mỗi tài khoản" (nudTaiKhoanFrom/To),
            // checkBox3 = "Giới hạn thời gian chạy mỗi kịch bản" (nudKichBanFrom/To). Backend CŨ đọc
            // sai tên (checkBox3/4 + numericUpDown6..9) nên time limit KHÔNG BAO GIỜ áp, và nhánh
            // checkBox2 cũ cắt cụt actions còn RandomValue(1,5). Stop() ép limit ở đầu mỗi vòng action.
            // RandomValue cận trên LOẠI TRỪ nên +1 để [from..to] BAO GỒM cả 'to'.
            if (_configKichBan.GetBooleanValue("checkBox2"))
            {
                int tkFrom = _configKichBan.GetIntType("nudTaiKhoanFrom", _configKichBan.GetIntType("numericUpDown7", 40));
                int tkTo = _configKichBan.GetIntType("nudTaiKhoanTo", _configKichBan.GetIntType("numericUpDown6", 60));
                _setting["timeoutTaiKhoan"] = SubdyHelper.RandomValue(tkFrom, tkTo + 1);
            }
            if (_configKichBan.GetBooleanValue("checkBox3"))
            {
                int kbFrom = _configKichBan.GetIntType("nudKichBanFrom", _configKichBan.GetIntType("numericUpDown9", 5));
                int kbTo = _configKichBan.GetIntType("nudKichBanTo", _configKichBan.GetIntType("numericUpDown8", 10));
                _setting["timeoutKichBan"] = SubdyHelper.RandomValue(kbFrom, kbTo + 1);
            }

            // [TIME-LIMIT v25] Không Restart đồng hồ ở đây: mốc do MainService đặt lúc bắt đầu
            // chuẩn bị acc, nên hạn mức tính cả thời gian chuẩn bị. Logging-only dòng dưới đây.
            AutoAndroid.RunHistoryLog.Note(_client?.Device?.Serial ?? "?",
                $"[TIMEOUT] uid={_account?.Uid} | bắt đầu chạy kịch bản; chuẩn bị acc đã dùng {Math.Round(LimitElapsed.TotalMinutes, 1)} phút"
                + (_setting.ContainsKey("timeoutTaiKhoan") ? $" trong hạn mức {_setting["timeoutTaiKhoan"]} phút mỗi tài khoản." : "."));
            _mainService._sate = "Tải kịch bản";

            // Mở app + đăng nhập một lần trước khi chạy chuỗi hành động.
            _client.ForcePortraitOrientation();
            _client.AppClear(PKG);
            EnsurePandoraOpen();
            if (!await EnsureLoggedIn())
            {
                _mainService.SetStatus("Đăng nhập Pandora thất bại.", 1);
                return;
            }

            for (int i = 1; i <= actions.Count; i++)
            {
                var action = actions[i - 1];
                if (Stop()) return;

                _mainService._sate = $"Thực hiện {i}/{actions.Count}: {action.Name}";
                _mainService.SetStatus("Đang thực hiện...", 0);
                try
                {
                    await StartAction(action);
                }
                catch
                {
                    // Lỗi đã được StartAction ghi log; vẫn chạy tiếp action kế.
                }
            }
        }

        public async Task StartAction(ScriptAction action)
        {
            string error = "Thành công";
            try
            {
                var json = new JsonHelper(action.Json, true);
                EnsurePandoraOpen();

                switch (action.Type)
                {
                    case PandoraFarmingType.HDNgheNhac:
                        await HDNgheNhac(json, action);
                        break;
                    default:
                        error = $"Action type [{action.Type}] chưa được hỗ trợ trên Pandora.";
                        break;
                }
            }
            catch (Exception ex)
            {
                SubdyExtension extension = ex is SubdyExtension sx
                    ? sx
                    : new SubdyExtension(SubdyEnum.Error, ex.Message);

                if (extension.SubdyEnum == SubdyEnum.LogOut)
                {
                    await EnsureLoggedIn();
                }
                error = $"Thất bại ({extension.Message})";
                if (extension.SubdyEnum != SubdyEnum.JobFail)
                {
                    throw extension;
                }
            }
            finally
            {
                int wait = SubdyHelper.RandomValue(
                    _configKichBan.GetIntType("numericUpDown2", 5),
                    _configKichBan.GetIntType("numericUpDown1", 30));
                await _mainService.DelayMessageAsync(
                    wait,
                    $"Đã chạy hành động {action.Name}.{error}." + " Đợi {time} giây để qua hành động tiếp theo...",
                    2);
                _mainService.SetStatus($"Đã chạy xong hành động {action.Name} - {error}", 0);
            }
        }

        // ================= Helper dùng chung =================

        private void EnsurePandoraOpen()
        {
            if (!_client.Package(PKG, 1))
            {
                _client.AppStart(PKG, true, false, true);
                _client.Delay(5, 8);
            }
        }

        private string Xml() => _client.GetXMLSource();

        /// <summary>
        /// Kiểm tra xpath có tồn tại trong snapshot hay không — trả lời NGAY, không chờ.
        /// BẮT BUỘC timeout=0: FindElements khi không match sẽ XÓA xmlContent rồi dump lại
        /// liên tục cho tới hết timeout (kể cả khi caller đã truyền sẵn snapshot). Để 15 như
        /// trước thì mỗi lần miss tốn trọn 15 giây; DismissOverlays + SearchAndPlay miss
        /// khoảng 10 lần cho mỗi hành động -> cộng dồn ~2.5 phút, chạy vài hành động là
        /// mất ~10 phút trước khi bấm được ô tìm kiếm.
        /// Muốn CHỜ element xuất hiện thì dùng Tap()/FindElement() với timeout, không dùng hàm này.
        /// </summary>
        private bool Exists(string xpath, string xml = "")
        {
            if (string.IsNullOrEmpty(xml)) xml = Xml();
            if (string.IsNullOrEmpty(xml)) return false;
            try { return _client.FindElements(0, xml, xpath).Count > 0; }
            catch { return false; }
        }

        /// <summary>
        /// Tap element theo xpath, CÓ CHỜ element xuất hiện tối đa <paramref name="timeout"/> giây.
        /// Chỉ dùng khi thật sự cần chờ: miss là tốn trọn timeout vì ADBClient sẽ dump lại
        /// liên tục. Đã có snapshot trong tay thì dùng <see cref="TapIn"/>.
        /// </summary>
        private bool Tap(string xpath, int timeout = 5, string xml = "")
            => _client.ElementWithAttributes(xpath, timeout, xml);

        /// <summary>
        /// Tap element NẾU nó có trong snapshot truyền vào — miss thì trả false ngay,
        /// không dump lại, không chờ. Đây là hàm tap mặc định của mọi vòng state-machine.
        /// Dùng chung engine <see cref="ADBClient.FindElements"/>(timeout 0) với
        /// <see cref="Exists"/> nên "thấy" và "tap được" luôn khớp nhau.
        /// </summary>
        private bool TapIn(string xpath, string xml) => TapNth(xpath, 0, xml);

        /// <summary>
        /// So khớp snapshot với danh sách case theo THỨ TỰ ƯU TIÊN, trả về xpath match đầu tiên
        /// (rỗng nếu không case nào khớp). Thay cho <c>_client.FindElement("", cases, timeout)</c>:
        /// chỉ tốn đúng 1 lần dump của caller, miss thì biết ngay thay vì spin hết timeout.
        /// </summary>
        private string MatchCase(string xml, params string[] cases)
        {
            if (string.IsNullOrEmpty(xml)) return "";
            foreach (var xpath in cases)
            {
                if (Exists(xpath, xml)) return xpath;
            }
            return "";
        }

        /// <summary>Bản nhận List cho các nhóm xpath sẵn có (X_WATCH_AD, X_AD_CLOSE...).</summary>
        private string MatchCase(string xml, IEnumerable<string> cases)
            => MatchCase(xml, cases.ToArray());

        /// <summary>Tap node thứ index (0-based) trong danh sách match — dùng cho list kết quả.</summary>
        private bool TapNth(string xpath, int index, string xml = "")
        {
            if (string.IsNullOrEmpty(xml)) xml = Xml();
            if (string.IsNullOrEmpty(xml)) return false;
            try
            {
                var nodes = _client.FindElements(0, xml, xpath);
                if (nodes.Count <= index) return false;
                string bounds = nodes[index].Attributes?["bounds"]?.Value ?? "";
                if (string.IsNullOrEmpty(bounds)) return false;
                var point = new RectangleArea(bounds).GetCenterPoint();
                Log($"TapNth[{index}] {xpath} -> ({point.X},{point.Y})");
                _client.Click(point.X, point.Y);
                return true;
            }
            catch { return false; }
        }

        private void Back()
        {
            _client.Shell("input keyevent 4");
            _client.Delay(1, 2);
        }

        /// <summary>Đóng các coachmark/callout 1 lần của Pandora nếu đang che màn hình.</summary>
        private void DismissOverlays()
        {
            string xml = Xml();
            if (string.IsNullOrEmpty(xml)) return;

            // Promo Pandora Plus (Activity riêng của Urban Airship) che toàn màn -> đóng trước
            // mọi thứ khác: lúc này cây view KHÔNG còn quảng cáo/gate/player nào để xét.
            if (DismissPromo(xml))
            {
                xml = Xml();
                if (string.IsNullOrEmpty(xml)) return;
            }

            // Đang chạy quảng cáo -> KHÔNG tap "Close" vì sẽ hủy lượt phát bài đã tìm.
            // Việc đóng quảng cáo do HandlePlayGate xử lý sau khi hết thời lượng bắt buộc.
            if (X_AD_PLAYING.Any(xp => Exists(xp, xml)))
            {
                Log("Đang có quảng cáo -> không dismiss.");
                return;
            }

            // Đang ở gate "Play This Song?/Album?" -> KHÔNG dismiss. Gate có close-button
            // text="Close", trùng đúng X_DISMISS, bấm vào là mất lượt phát.
            // Gate do HandlePlayGate xử lý (bắt buộc đi đường Watch Ad).
            if (Exists(X_OFFER_HEADER, xml))
            {
                Log("Đang ở gate phát bài -> không dismiss.");
                return;
            }

            // Dialog "Skip Anyway" chen ngang -> bấm luôn (yêu cầu: bất cứ lúc nào).
            if (DismissSkipPrompt(xml)) return;

            // Coachmark webview full-screen chỉ đóng được bằng Back.
            if (Exists(X_COACHMARK, xml))
            {
                Log("Dismiss webview_coachmark bằng Back");
                Back();
                xml = Xml();
            }

            // Tap theo snapshot: chỉ dump lại khi vừa đóng được 1 overlay (cây view đổi).
            // Trước đây dùng ElementWithAttributes(xp, 2, xml) cho từng xpath -> mỗi xpath
            // không match vẫn tốn 2 giây dump lại, 5 xpath × mọi lần gọi = phút chờ vô ích.
            foreach (var xp in X_DISMISS)
            {
                if (TapIn(xp, xml))
                {
                    _client.Delay(1, 2);
                    xml = Xml();
                    if (string.IsNullOrEmpty(xml)) return;
                }
            }
        }

        /// <summary>
        /// Đóng in-app message / promo Pandora Plus phủ toàn màn (Airship HtmlActivity).
        /// Ưu tiên bấm ĐÚNG nút đóng của nó (<see cref="X_PROMO_DISMISS"/>, content-desc="Cancel");
        /// template nào không đưa nút vào cây view thì lùi bằng Back — đã kiểm chứng trên máy
        /// 5200d004b263c49d: Back đưa app về BottomNavActivity ngay lập tức.
        /// TUYỆT ĐỐI không tap vào giữa màn: 4 ô ảnh promo đều clickable và mở luồng mua gói.
        /// Trả về true nếu vừa xử lý một màn promo.
        /// </summary>
        private bool DismissPromo(string xml = "")
        {
            if (string.IsNullOrEmpty(xml)) xml = Xml();
            if (string.IsNullOrEmpty(xml)) return false;

            // Gate phát bài và quảng cáo reward có luồng xử lý riêng trong HandlePlayGate -
            // không đụng vào (bấm nhầm ở 2 màn đó là mất lượt phát bài đã tìm).
            if (Exists(X_OFFER_HEADER, xml)) return false;
            if (X_AD_PLAYING.Any(xp => Exists(xp, xml))) return false;

            if (string.IsNullOrEmpty(MatchCase(xml, X_PROMO))) return false;

            _mainService.SetStatus("Đóng quảng cáo Pandora Plus chen ngang...", 3);
            string closeCase = MatchCase(xml, X_PROMO_CLOSE);
            if (!string.IsNullOrEmpty(closeCase) && TapIn(closeCase, xml))
            {
                Log($"Đóng promo bằng selector {closeCase}.");
            }
            else
            {
                Log("Promo không có nút đóng trong cây view -> Back.");
                Back();
            }
            _client.Delay(2, 3);
            return true;
        }

        /// <summary>
        /// Bấm nút "Skip"/"Skip Anyway" nếu đang có trong snapshot — theo yêu cầu: gặp là bấm,
        /// bất cứ lúc nào. Trả true nếu vừa bấm.
        ///
        /// Khi đang có player (full/mini) chỉ bấm "Skip Anyway": nút "Skip" trên player full
        /// là nút phát bài KẾ TIẾP, bấm nó là nghe nhầm bài. Dialog "Skip Anyway" chen ngang
        /// nằm đè lên player nên vẫn match được dù player có trong cây view hay không.
        /// </summary>
        private bool DismissSkipPrompt(string xml = "")
        {
            if (string.IsNullOrEmpty(xml)) xml = Xml();
            if (string.IsNullOrEmpty(xml)) return false;

            // Đang ở gate phát bài hoặc quảng cáo reward: không đụng nút Skip (bấm nhầm là
            // mất lượt phát bài đã tìm). Luồng đó do HandlePlayGate xử lý.
            if (Exists(X_OFFER_HEADER, xml)) return false;
            if (X_AD_PLAYING.Any(xp => Exists(xp, xml))) return false;

            bool onPlayer = Exists(X_NOW_PLAYING_BAR, xml) || Exists(X_MINI_HANDLE, xml);
            string _case = onPlayer
                ? MatchCase(xml, X_SKIP_ANYWAY)
                : MatchCase(xml, X_SKIP_ANYWAY.Concat(X_SKIP_ANY).ToArray());
            if (string.IsNullOrEmpty(_case)) return false;

            _mainService.SetStatus("Gặp nút Skip/Skip Anyway -> bấm.", 3);
            Log($"Bấm skip ({_case}).");
            if (TapIn(_case, xml))
            {
                _client.Delay(1, 2);
                return true;
            }
            return false;
        }

        // ================= Đăng nhập =================

        /// <summary>Timeout tổng cho toàn bộ quá trình đăng nhập (giây) — giống FacebookService.Login.</summary>
        private const int LoginTimeout = 210;

        private bool IsLoggedIn(string xml = "")
        {
            if (string.IsNullOrEmpty(xml)) xml = Xml();
            return Exists(X_BOTTOM_NAV, xml) || Exists(X_NOW_PLAYING, xml);
        }

        /// <summary>
        /// Mọi trạng thái có thể gặp khi đăng nhập, xếp theo thứ tự ưu tiên.
        /// FindElement trả về xpath nào match trước thì switch xử lý case đó — chỉ 1 lần
        /// dump cho mỗi lượt, không dò tuần tự từng khả năng như code cũ.
        /// </summary>
        private static readonly List<string> LoginCases = new List<string>
        {
            // Thành công (ưu tiên cao nhất để thoát sớm)
            X_BOTTOM_NAV,
            X_NOW_PLAYING,
            // Lỗi credential
            "//*[contains(@text,\"Invalid\")]",
            "//*[contains(@text,\"incorrect\")]",
            // Màn hình cần thao tác
            X_FTUE_FAB,
            X_EMAIL,
            X_WELCOME_LOGIN,
            X_SECONDARY_CTA,
            // Overlay chặn đường
            X_COACHMARK,
        }
        // Promo Airship có thể bung ngay sau khi đăng nhập xong, che mất bottom_navigation
        // -> không có case này thì IsLoggedIn không bao giờ true và vòng lặp đốt hết
        // LoginTimeout (210s) dù đã đăng nhập thành công.
        // Nối từ X_PROMO thay vì chép lại chuỗi: X_PROMO khai báo phía trên nên đã khởi tạo
        // xong khi tới đây, và switch bên dưới nhận diện case bằng chính X_PROMO.Contains.
        .Concat(X_PROMO).ToList();

        /// <summary>
        /// Vòng lặp state-machine giống FacebookService.Login: mỗi lượt chỉ dump 1 lần,
        /// ra case nào xử lý case đó. Thoát khi đăng nhập xong, sai credential,
        /// hoặc quá <see cref="LoginTimeout"/> giây.
        /// </summary>
        private async Task<bool> EnsureLoggedIn()
        {
            string uid = _account?.Uid?.Trim() ?? "";
            string pwd = _account?.Password?.Trim() ?? "";

            var sw = Stopwatch.StartNew();
            bool emailTyped = false, passwordTyped = false, submitted = false;

            while (true)
            {
                _client.ThrowIfStopped();
                if (sw.Elapsed.TotalSeconds > LoginTimeout)
                {
                    _mainService.SetStatus($"Quá {LoginTimeout}s cho việc đăng nhập Pandora.", 1);
                    return false;
                }
                await _mainService.Stop();

                // App bị đóng/crash giữa quá trình -> mở lại rồi dò tiếp.
                if (!_client.Package(PKG, 1))
                {
                    _client.AppStart(PKG, true, false, true);
                    _client.Delay(5, 8);
                    continue;
                }

                // 1 dump / 1 lượt. KHÔNG dùng _client.FindElement(..., 20): khi màn hình
                // đang loading (không case nào match) nó sẽ dump lại suốt 20 giây rồi mới
                // trả rỗng, cộng thêm Delay(3) của nhánh dưới -> mỗi lượt chờ hơn 20s.
                string xml = Xml();
                if (DismissSkipPrompt(xml)) continue;
                string _case = MatchCase(xml, LoginCases);
                if (string.IsNullOrEmpty(_case))
                {
                    // Chưa nhận ra màn hình nào (đang loading) -> chờ ngắn rồi dò lại.
                    _client.Delay(PollSeconds);
                    continue;
                }

                Log($"login case={_case}");
                switch (_case)
                {
                    // ---- Thành công ----
                    case var c when c == X_BOTTOM_NAV || c == X_NOW_PLAYING:
                        _mainService.SetStatus("Đăng nhập Pandora thành công.", 2);
                        DismissOverlays();
                        return true;

                    // ---- Sai tài khoản/mật khẩu ----
                    // Chỉ tin case này SAU khi đã bấm submit: Pandora hiển thị sẵn hint
                    // ở textinput_error nên bắt sớm sẽ báo sai oan.
                    case var c when c.Contains("Invalid") || c.Contains("incorrect"):
                        if (!submitted)
                        {
                            _client.Delay(PollSeconds);
                            break;
                        }
                        _mainService.SetStatus("Sai tài khoản hoặc mật khẩu.", 1);
                        throw new SubdyExtension(SubdyEnum.WrongPassword,
                            "Sai tài khoản hoặc mật khẩu Pandora.");

                    // ---- Màn hình khởi tạo sau đăng nhập lần đầu ----
                    case var c when c == X_FTUE_FAB:
                        _mainService.SetStatus("Bỏ qua màn hình khởi tạo...", 3);
                        TapIn(X_FTUE_FAB, xml);
                        _client.Delay(3, 5);
                        break;

                    // ---- Form đăng nhập: email -> password -> submit ----
                    case var c when c == X_EMAIL:
                        if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(pwd))
                        {
                            _mainService.SetStatus("Tài khoản thiếu Uid/Password.", 1);
                            return false;
                        }
                        if (!emailTyped)
                        {
                            _mainService.SetStatus("Nhập email...", 3);
                            _client.SendTextSlow(X_EMAIL, uid, 10, 120, 5, xml, true);
                            _client.Delay(1, 2);
                            emailTyped = true;
                            break;
                        }
                        if (!passwordTyped)
                        {
                            _mainService.SetStatus("Nhập mật khẩu...", 3);
                            _client.SendTextSlow(X_PASSWORD, pwd, 10, 120, 5, "", true);
                            _client.Delay(1, 2);
                            passwordTyped = true;
                            break;
                        }
                        _mainService.SetStatus("Đăng nhập...", 3);
                        if (!Tap(X_CTA, 5))
                        {
                            _mainService.SetStatus("Không tìm thấy nút đăng nhập.", 1);
                            return false;
                        }
                        submitted = true;
                        _client.Delay(4, 6);
                        break;

                    // ---- Welcome ----
                    case var c when c == X_WELCOME_LOGIN:
                        _mainService.SetStatus("Mở màn hình đăng nhập...", 3);
                        TapIn(X_WELCOME_LOGIN, xml);
                        _client.Delay(2, 4);
                        break;

                    // ---- Đang ở màn Sign Up -> chuyển sang Log In ----
                    case var c when c == X_SECONDARY_CTA:
                        TapIn(X_SECONDARY_CTA, xml);
                        _client.Delay(2, 4);
                        break;

                    // ---- Overlay che màn hình ----
                    case var c when c == X_COACHMARK:
                        DismissOverlays();
                        break;

                    // ---- Promo Pandora Plus che màn đăng nhập/Home ----
                    case var c when X_PROMO.Contains(c):
                        DismissPromo(xml);
                        break;
                }
            }
        }

        // ================= HDNgheNhac =================

        /// <summary>
        /// Chặn trên cho 1 bài, CHỈ dùng khi không phát hiện được điểm hết bài
        /// (mất thanh tiến trình và tên bài cũng không đổi).
        /// Đa số bài 120-240s, nhưng KHÔNG được để 240: có bài dài hơn nhiều
        /// (vd "Bohemian Rhapsody" 5:55 = 355s trong danh sách keyword mặc định),
        /// cắt ở 240 là nghe chưa hết bài. Điểm hết bài thật do ListenAsync tự nhận
        /// qua thanh tiến trình / đổi tên bài, nên số này chỉ là van an toàn.
        /// </summary>
        private const int MaxListenSeconds = 420;

        /// <summary>Số giây nghe tối thiểu trước khi công nhận đã "hết bài" (1 bài thường 120-240s).</summary>
        private const int MinSongSeconds = 120;

        /// <summary>
        /// Trần cho trường hợp KHÔNG đọc được thanh tiến trình (player lỗi layout, quảng cáo
        /// banner che, Pandora đổi id). Không có mốc này thì ListenAsync nằm nghe tới hết
        /// maxSeconds — với ngân sách mặc định 300s là ăn trọn ngân sách cho ĐÚNG 1 bài,
        /// đúng hiện tượng "chưa chuyển bài hát".
        /// </summary>
        private const int NoProgressMaxSeconds = 240;

        private async Task HDNgheNhac(JsonHelper json, ScriptAction action)
        {
            // txtPandoraUrl: DANH SÁCH URL, mỗi dòng một URL playlist/bài hát.
            // Tương thích ngược: cấu hình cũ chỉ 1 URL -> parse ra đúng 1 phần tử.
            var urls = ParsePandoraUrls(json.GetValue("txtPandoraUrl", ""));
            if (urls.Count == 0)
            {
                throw new SubdyExtension(SubdyEnum.JobFail, "Chưa cấu hình URL bài hát/playlist.");
            }

            // Khoảng phút là NGÂN SÁCH CHO TỪNG URL: mỗi URL tự bốc thăm một số phút
            // trong khoảng này, nghe hết rồi mới sang URL kế. Tài khoản chỉ đổi khi
            // đã đi hết danh sách.
            int minMinutes = Math.Clamp(json.GetIntType("nudListenMinutesFrom", 30), 1, 1440);
            int maxMinutes = Math.Clamp(json.GetIntType("nudListenMinutesTo", 180), 1, 1440);
            if (minMinutes > maxMinutes) (minMinutes, maxMinutes) = (maxMinutes, minMinutes);

            _mainService.SetStatus(
                $"Danh sách {urls.Count} URL, mỗi URL nghe ngẫu nhiên {minMinutes}-{maxMinutes} phút.",
                2);

            var swAll = Stopwatch.StartNew();
            int done = 0, failed = 0;
            bool stoppedEarly = false;

            for (int i = 0; i < urls.Count; i++)
            {
                _client.ThrowIfStopped();
                if (Stop()) { stoppedEarly = true; break; }
                await _mainService.Stop();

                string url = urls[i];
                int selectedMinutes = Random.Shared.Next(minMinutes, maxMinutes + 1);
                int totalSeconds = selectedMinutes * 60;

                _mainService.SetStatus(
                    $"[{i + 1}/{urls.Count}] Mở URL: {url}, chọn {selectedMinutes} phút trong khoảng " +
                    $"{minMinutes}-{maxMinutes} phút ({totalSeconds} giây).",
                    2);

                if (await ListenOneUrlAsync(url, i + 1, urls.Count, selectedMinutes,
                                           minMinutes, maxMinutes, totalSeconds))
                    done++;
                else
                    failed++;

                // Nghỉ ngắn giữa hai URL cho tự nhiên (không nghỉ sau URL cuối).
                if (i < urls.Count - 1) _client.Delay(5, 12);
            }

            // Cả danh sách đều không mở được => coi như hành động thất bại.
            // Nếu thoát sớm vì timeout tài khoản/kịch bản (stoppedEarly) thì KHÔNG phải
            // lỗi mở URL — không ném, chỉ báo cáo số đã nghe được.
            if (done == 0 && !stoppedEarly)
            {
                throw new SubdyExtension(SubdyEnum.JobFail,
                    $"Không thể mở URL bài hát/playlist trên Pandora ({failed}/{urls.Count} URL lỗi).");
            }

            _mainService.SetStatus(
                $"Hoàn tất nghe nhạc: {done}/{urls.Count} URL, tổng {(int)swAll.Elapsed.TotalSeconds} giây " +
                $"({failed} URL lỗi).",
                2);
        }

        /// <summary>
        /// Tách ô "URL Playlist/Bài hát" thành danh sách URL. Mỗi dòng một URL;
        /// cho phép cả phân cách '|' và tự loại dòng trùng/rỗng. Dòng không có
        /// "://" bị bỏ (dán cả đoạn văn bản vào cũng không sinh ra URL vô nghĩa).
        /// </summary>
        private static List<string> ParsePandoraUrls(string raw)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(raw)) return result;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var piece in raw.Split(new[] { '\r', '\n', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string url = piece.Trim();
                if (url.Length == 0) continue;
                if (!url.Contains("://"))
                {
                    Log($"Bỏ dòng không phải URL trong danh sách: {url}");
                    continue;
                }
                if (seen.Add(url)) result.Add(url);
            }
            return result;
        }

        /// <summary>
        /// Mở 1 URL và nghe hết ngân sách thời gian của nó.
        /// Trả về false nếu không mở được URL (lỗi đã được log + báo trạng thái,
        /// để luồng trên quyết định bỏ qua và đi tiếp URL sau).
        /// </summary>
        private async Task<bool> ListenOneUrlAsync(string url, int index, int count,
                                                  int selectedMinutes, int minMinutes, int maxMinutes,
                                                  int totalSeconds)
        {
            // Mở URL và phát nhạc
            if (!OpenUrlAndPlay(url))
            {
                // Nếu mở URL không được, thử lại 1 lần
                _mainService.SetStatus($"[{index}/{count}] Mở URL thất bại, thử lại...", 1);
                await RecoverAsync(1);
                if (!OpenUrlAndPlay(url))
                {
                    _mainService.SetStatus($"[{index}/{count}] Không mở được URL, bỏ sang URL tiếp theo.", 1);
                    Log($"[{index}/{count}] Không mở được URL: {url}");
                    return false;
                }
            }

            int played = 1, listenedTotal = 0;
            var sw = Stopwatch.StartNew();

            // Nghe nhạc trong ngân sách thời gian đã chọn
            while (sw.Elapsed.TotalSeconds < totalSeconds)
            {
                _client.ThrowIfStopped();
                if (Stop()) break;
                await _mainService.Stop();

                int remain = (int)(totalSeconds - sw.Elapsed.TotalSeconds);
                if (remain <= 0) break;

                var (listened, stalled) = await ListenAsync(Math.Min(remain, MaxListenSeconds));
                listenedTotal += listened;
                _mainService.SetStatus(
                    $"[{index}/{count}] Đã nghe {listened}s - tổng {played} bài / {listenedTotal}s nhạc, " +
                    $"phát \"{_lastPlayedTrack}\", {(int)sw.Elapsed.TotalSeconds}/{totalSeconds}s " +
                    $"(đã chọn {selectedMinutes} phút từ {minMinutes}-{maxMinutes} phút).",
                    2);

                // Mất player giữa bài: khôi phục rồi tiếp tục nghe
                if (stalled)
                {
                    await RecoverAsync(1);
                    // Sau khi khôi phục, mở lại URL nếu player không tự quay lại
                    _client.Delay(3, 5);
                    if (!Exists(X_NOW_PLAYING, Xml()) && !Exists(X_NOW_PLAYING_BAR, Xml()))
                    {
                        _mainService.SetStatus("Mất kết nối player, mở lại URL...", 3);
                        OpenUrlAndPlay(url);
                    }
                }
            }

            _mainService.SetStatus(
                $"[{index}/{count}] Xong URL: {played} bài, {listenedTotal}s nhạc trong " +
                $"{(int)sw.Elapsed.TotalSeconds}/{totalSeconds} giây " +
                $"(đã chọn {selectedMinutes} phút, cấu hình {minMinutes}-{maxMinutes} phút).",
                2);

            return true;
        }

        /// <summary>
        /// Mở URL Pandora qua Android Intent và chờ player phát nhạc.
        /// Xử lý gate free user (quảng cáo, Watch Ad, promo) nếu có.
        /// Nếu lần đầu không được, thử tìm nút Play trên trang playlist rồi thử lại.
        /// Trả về true khi player đang phát.
        /// </summary>
        private bool OpenUrlAndPlay(string url)
        {
            EnsurePandoraOpen();

            _mainService.SetStatus("Đang mở URL trên Pandora...", 3);
            // Dùng -n để force mở trong Pandora app (tránh ResolverActivity khi có nhiều app xử lý https)
            _client.Shell($"am start -a android.intent.action.VIEW -d \"{url}\" -n com.pandora.android/.LauncherActivity");
            _client.Delay(4, 6);

            // Xử lý gate và chờ player — empty expectedTrack = chấp nhận mọi bài hát
            if (HandlePlayGate("")) return true;

            // Nếu player chưa xuất hiện, có thể Pandora chỉ mở trang playlist
            // (không auto-play). Tìm nút Play/Shuffle Play trên trang rồi thử lại.
            _mainService.SetStatus("Đang tìm nút phát trên trang playlist...", 3);
            TryTapPagePlay();
            _client.Delay(3, 5);

            return HandlePlayGate("");
        }

        /// <summary>
        /// Thử tìm và tap nút Play/Shuffle Play trên trang playlist/album.
        /// Dùng khi HandlePlayGate không thấy player ngay (Pandora mở trang playlist
        /// thay vì auto-play).
        /// </summary>
        private bool TryTapPagePlay()
        {
            string xml = Xml();
            if (string.IsNullOrEmpty(xml)) return false;

            // Thử các selector phổ biến cho nút Play trên trang nội dung Pandora
            var selectors = new[]
            {
                "//*[@content-desc=\"Shuffle Play\"]",
                "//*[@content-desc=\"Play\"]",
                "//*[@text=\"Shuffle Play\"]",
                "//*[@resource-id=\"com.pandora.android:id/auxiliary_button\"]",
            };

            foreach (var xp in selectors)
            {
                if (TapIn(xp, xml))
                {
                    Log($"Đã tap nút Play: {xp}");
                    return true;
                }
            }

            Log("Không tìm thấy nút Play trên trang.");
            return false;
        }

        /// <summary>
        /// Đưa app về trạng thái dùng được sau khi phát lỗi.
        /// Lỗi càng nhiều lần liên tiếp thì khôi phục càng sâu (mở lại app + kiểm tra đăng nhập).
        /// </summary>
        private async Task RecoverAsync(int failCount)
        {
            // Promo Airship: đóng bằng nút của chính nó là app đã về Home. Back thêm ở đây
            // là Back TỪ Home -> thoát app, lần sau phải mở lại + đăng nhập từ đầu.
            if (!DismissPromo())
            {
                DismissOverlays();
                Back();
            }
            if (failCount % 3 == 0)
            {
                EnsurePandoraOpen();
                await EnsureLoggedIn();
            }
            _client.Delay(2, 4);
        }

        /// <summary>
        /// Đưa app về ô tìm kiếm trong tối đa <see cref="SearchReadyTimeout"/> giây.
        /// Vòng lặp 1 dump / 1 lượt: xét ô tìm kiếm -> tab Search -> player full -> bottom nav,
        /// case nào có thì xử lý case đó. Trả về snapshot đang có ô tìm kiếm (rỗng nếu quá hạn).
        ///
        /// Bản cũ gọi Tap(X_TAB_SEARCH, 5, xml) rồi Back() + Tap(X_TAB_SEARCH, 5): mỗi lần
        /// tab_search chưa có trên cây view là 2 × 5 giây dump lại, chưa kể DismissOverlays
        /// phía trước cũng dump theo từng xpath -> cộng dồn thành nhiều phút mới bấm được
        /// vào ô tìm kiếm sau khi đăng nhập.
        /// </summary>
        private string EnsureSearchScreen()
        {
            var sw = Stopwatch.StartNew();
            bool dismissed = false;

            while (sw.Elapsed.TotalSeconds < SearchReadyTimeout)
            {
                _client.ThrowIfStopped();

                string xml = Xml();
                if (string.IsNullOrEmpty(xml))
                {
                    _client.Delay(PollSeconds);
                    continue;
                }

                // Dialog "Skip Anyway" chen ngang -> bấm luôn trước khi xét màn hình tìm kiếm.
                if (DismissSkipPrompt(xml)) continue;

                // Đã ở màn tìm kiếm -> xong.
                if (Exists(X_SEARCH_INPUT, xml)) return xml;

                // Promo Pandora Plus (Airship) là Activity riêng, đè hết bottom nav lẫn player
                // -> phải đóng trước, nếu không mọi nhánh dưới đều miss và vòng lặp chỉ còn
                // Back() vô nghĩa cho tới hết SearchReadyTimeout.
                if (DismissPromo(xml)) continue;

                // Có bottom nav -> bấm tab Search.
                if (TapIn(X_TAB_SEARCH, xml))
                {
                    _client.Delay(PollSeconds);
                    continue;
                }

                // Player mở full che bottom nav -> thu nhỏ.
                if (Exists(X_NOW_PLAYING_BAR, xml))
                {
                    Log("Thu nhỏ Now Playing để về bottom nav.");
                    Back();
                    continue;
                }

                // Overlay/coachmark che bottom nav -> đóng 1 lần rồi thử lại.
                if (!dismissed)
                {
                    dismissed = true;
                    DismissOverlays();
                    continue;
                }

                // Đang ở trang con (backstage, station...) -> lùi ra.
                Back();
            }

            Log($"Quá {SearchReadyTimeout}s không về được ô tìm kiếm.");
            return "";
        }

        /// <summary>Tìm keyword ở tab Search, mở tab SONGS và phát kết quả khớp keyword.</summary>
        private bool SearchAndPlay(string keyword)
        {
            // Mỗi lượt gõ 1 biến thể query khác nhau của CÙNG keyword (xem BuildSearchQueries).
            // Việc chấm điểm row vẫn theo keyword GỐC để không mất phần tên ca sĩ.
            var queries = BuildSearchQueries(keyword);
            while (queries.Count < MaxSearchAttempts) queries.Add(queries[0]);

            for (int attempt = 0; attempt < queries.Count; attempt++)
            {
                _client.ThrowIfStopped();
                string query = queries[attempt];

                string xml = EnsureSearchScreen();
                if (string.IsNullOrEmpty(xml)) return false;

                // Xóa query cũ (nút X chỉ có khi ô đang có chữ).
                if (TapIn(X_SEARCH_CLEAR, xml))
                {
                    _client.Delay(1, 2);
                    xml = Xml();
                }

                // Nhập từ khóa qua ADB Keyboard (base64) — chịu được khoảng trắng/ký tự đặc biệt.
                _client.SendTextSlow(X_SEARCH_INPUT, query, 10, 120, 5, xml, true);
                _client.Delay(1, 2);

                // Ô tìm kiếm phải THẬT SỰ mang query trước khi bấm ENTER. Lần tìm đầu ngay
                // sau login hay trượt: màn search vừa mở, EditText chưa nhận focus nên
                // SendTextSlow gõ vào hư không, ENTER không tạo query nào và danh sách RECENT
                // (bài của keyword trước) bị tính là kết quả.
                if (!SearchBoxHasKeyword(query))
                {
                    Log($"Ô tìm kiếm chưa nhận \"{query}\" (lượt {attempt + 1}/{queries.Count}) -> gõ lại.");
                    continue;
                }

                _client.Shell("input keyevent 66"); // ENTER
                _client.Delay(3, 5);

                // Tab SONGS để chỉ lấy bài hát (bỏ artist/station/podcast).
                // Tab đang chọn có selected="true" + clickable="false" nên chỉ tap khi chưa chọn.
                xml = Xml();
                if (TapIn(X_TAB_SONGS_UNSELECTED, xml))
                {
                    _client.Delay(2, 3);
                    xml = Xml();
                }

                // Chờ đúng row của keyword hiện tại (không chấp nhận row RECENT/keyword cũ).
                var row = WaitResultRow(keyword, ref xml);
                if (row == null)
                {
                    Log($"Không có kết quả khớp \"{keyword}\" với query \"{query}\" " +
                        $"(lượt {attempt + 1}/{queries.Count}).");
                    continue;
                }

                // Bài sẽ phát = tên đọc từ CHÍNH row sắp bấm; HandlePlayGate đối chiếu tên này
                // với tên bài trên player để biết có phát đúng bài không.
                string expected = RowText(row, X_ROW_TITLE);
                if (string.IsNullOrEmpty(expected)) expected = SongPart(keyword);
                Log($"Chọn kết quả: \"{expected}\" - {RowText(row, X_ROW_ARTIST)} ({RowText(row, X_ROW_KIND)})");

                // Bấm nút Play NẰM TRONG row đã chọn; không có thì mở luôn row đó.
                // Bản cũ tap auxiliary_button đầu tiên TRÊN MÀN, không liên quan gì tới
                // row vừa tìm được (và row đầu màn thường là row RECENT của keyword trước).
                if (!TapNode(FindInRow(row, X_ROW_PLAY)) && !TapNode(row))
                    return false;
                _client.Delay(2, 4);

                return HandlePlayGate(expected);
            }
            return false;
        }

        /// <summary>
        /// Các chuỗi sẽ lần lượt gõ vào ô tìm kiếm cho 1 keyword.
        ///
        /// User nhập keyword kèm tên ca sĩ theo nhiều kiểu, nên thử lần lượt từ chuỗi
        /// "sạch" nhất tới chuỗi nguyên bản:
        ///  1. Phần tên bài đã bỏ dấu phân cách ("Shape of You - Ed Sheeran" -> "Shape of
        ///     You"). Pandora chắc chắn trả về bài này, còn việc chọn ĐÚNG bản của ca sĩ
        ///     user muốn là việc của <see cref="ScoreRow"/>.
        ///  2. Cả bài lẫn ca sĩ, đã bỏ dấu phân cách và bỏ cụm lặp
        ///     ("Partly Thao Sam Thao Sam" -> "Partly Thao Sam").
        ///  3. Chuỗi nguyên bản (chỉ khi bước bỏ cụm lặp có đổi gì) — phòng khi tên bài
        ///     vốn có phần lặp thật.
        /// Trùng nhau thì bỏ; còn thiếu so với <see cref="MaxSearchAttempts"/> thì
        /// SearchAndPlay tự gõ lại chuỗi đầu.
        /// </summary>
        private static List<string> BuildSearchQueries(string keyword)
        {
            var list = new List<string>();
            void Add(string q)
            {
                if (!string.IsNullOrEmpty(q)
                    && !list.Any(x => x.Equals(q, StringComparison.OrdinalIgnoreCase)))
                    list.Add(q);
            }

            Add(NormalizeQuery(SongPart(keyword)));
            Add(NormalizeQuery(keyword));
            Add(NormalizeQuery(keyword, collapseRepeat: false));

            if (list.Count == 0) list.Add((keyword ?? "").Trim());
            return list;
        }

        /// <summary>Ký tự user hay dùng để ngăn tên bài với tên ca sĩ.</summary>
        private static readonly char[] QuerySeparators = { '-', '–', '—', '|', '_', '/', '\\', '~' };

        /// <summary>Bỏ ký tự phân cách, gộp khoảng trắng thừa và (mặc định) bỏ cụm bị lặp.</summary>
        private static string NormalizeQuery(string value, bool collapseRepeat = true)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            var sb = new StringBuilder(value.Length);
            foreach (char ch in value)
                sb.Append(Array.IndexOf(QuerySeparators, ch) >= 0 ? ' ' : ch);

            var words = sb.ToString()
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            if (collapseRepeat) words = CollapseTrailingRepeat(words);
            return string.Join(" ", words);
        }

        /// <summary>
        /// Bỏ cụm bị lặp ở cuối chuỗi. Danh sách keyword của user hay bị dán đôi tên ca sĩ
        /// ("Partly Thao Sam Thao Sam" -> "Partly Thao Sam"): truy vấn càng dài Pandora
        /// càng dễ trả về rỗng, mà phần lặp không thêm thông tin gì.
        ///
        /// BẮT BUỘC còn ít nhất 1 từ đứng trước bản lặp đầu tiên, nếu không thì chính những
        /// tên bài vốn đã lặp ("New York New York", "Mahna Mahna") bị cắt mất một nửa.
        /// </summary>
        private static List<string> CollapseTrailingRepeat(List<string> words)
        {
            bool changed = true;
            while (changed && words.Count >= 3)
            {
                changed = false;
                for (int len = words.Count / 2; len >= 1; len--)
                {
                    int start = words.Count - len;
                    if (start - len < 1) continue;

                    bool same = true;
                    for (int i = 0; i < len && same; i++)
                        same = words[start + i].Equals(words[start - len + i],
                                                      StringComparison.OrdinalIgnoreCase);
                    if (!same) continue;

                    words.RemoveRange(start, len);
                    changed = true;
                    break;
                }
            }
            return words;
        }

        /// <summary>
        /// Phần TÊN BÀI của keyword = đoạn trước dấu phân cách đầu tiên
        /// ("Shape of You - Ed Sheeran" -> "Shape of You"). Không có dấu phân cách thì
        /// trả về nguyên keyword (không đoán bừa tên bài kết thúc ở đâu).
        /// </summary>
        private static string SongPart(string keyword)
        {
            string value = (keyword ?? "").Trim();
            if (value.Length == 0) return "";

            foreach (string sep in new[] { " - ", " – ", " — ", " | " })
            {
                int at = value.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
                if (at > 0) return value.Substring(0, at).Trim();
            }

            // Fallback: separator không có khoảng trắng chuẩn (vd "Tên Bài-Ca Sĩ").
            // Duyệt từ PHẢI sang để lấy vị trí tách CUỐI CÙNG — tên ca sĩ thường nằm
            // cuối keyword, tránh cắt nhầm dấu gạch ngang trong tên bài ("DDU-DU").
            // Điều kiện bên trái >= 2 token để không cắt nhầm "U-N-I" / "DDU-DU DDU-DU".
            for (int i = value.Length - 1; i > 0; i--)
            {
                if (Array.IndexOf(QuerySeparators, value[i]) >= 0)
                {
                    string left = value.Substring(0, i).Trim();
                    if (Tokenize(left).Count >= 2)
                        return left;
                }
            }

            // " by " chỉ là dấu phân cách khi phía trước đã đủ dài để là một tên bài.
            // Nếu không có điều kiện này thì "Stand by Me" bị cắt thành "Stand" và tool
            // đi tìm sai bài hoàn toàn.
            int by = value.IndexOf(" by ", StringComparison.OrdinalIgnoreCase);
            if (by > 0 && Tokenize(value.Substring(0, by)).Count >= 2)
                return value.Substring(0, by).Trim();

            return value;
        }

        /// <summary>
        /// Từ nối user hay chèn giữa tên bài và tên ca sĩ. Bỏ qua khi đối chiếu vì
        /// Pandora không đưa chúng vào tiêu đề row lẫn cột ca sĩ
        /// ("Shape of You ft Ed Sheeran" -> row chỉ có "Shape of You" + "Ed Sheeran").
        /// </summary>
        private static readonly HashSet<string> JoinWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "by", "ft", "feat", "featuring", "with", "cua", "của",
        };

        /// <summary>Các từ dùng để đối chiếu (đã bỏ từ nối).</summary>
        private static HashSet<string> MatchWords(string value)
        {
            var set = Tokenize(value);
            set.ExceptWith(JoinWords);
            return set;
        }

        /// <summary>
        /// Ô tìm kiếm có đang mang query không. Khi ô rỗng, Pandora để text="Search" (hint)
        /// nên phải so nội dung chứ không chỉ kiểm tra "khác rỗng".
        /// </summary>
        private bool SearchBoxHasKeyword(string query)
        {
            string typed = ReadAttr(Xml(), X_SEARCH_INPUT, "text");
            if (string.IsNullOrWhiteSpace(typed)) return false;
            typed = typed.Trim();
            query = (query ?? "").Trim();

            if (typed.Equals(query, StringComparison.OrdinalIgnoreCase)
                || typed.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                return true;

            // Bàn phím ADB có thể nuốt dấu câu ("M-TP" -> "M TP", "Don't" -> "Dont").
            // So theo TỪ để không bắt gõ lại một chuỗi thực chất đã đúng.
            var want = Tokenize(query);
            return want.Count > 0 && want.All(w => Tokenize(typed).Contains(w));
        }

        /// <summary>
        /// Chờ tới khi có row kết quả THẬT khớp keyword hiện tại, tối đa
        /// <see cref="ResultWaitTimeout"/> giây. Trả về node row đó (null nếu quá hạn).
        ///
        /// Hai lớp bảo vệ chống "kết quả cũ tính là kết quả mới":
        ///  1. Chỉ nhận row nằm trong :id/search_results — bỏ hẳn :id/search_history (tab
        ///     RECENT vẫn dùng đúng collection_data_holder + auxiliary_button).
        ///  2. Tên row phải khớp keyword theo TỪ (token) — kết quả của keyword trước còn sót
        ///     lại trên màn trong lúc RecyclerView chưa refresh sẽ bị loại.
        /// Ưu tiên row bài hát (subtitle "Song - m:ss") hơn row album/playlist.
        /// </summary>
        private XmlNode? WaitResultRow(string keyword, ref string xml)
        {
            var sw = Stopwatch.StartNew();
            while (true)
            {
                var row = PickResultRow(keyword, xml);
                if (row != null) return row;

                if (sw.Elapsed.TotalSeconds >= ResultWaitTimeout) return null;
                _client.ThrowIfStopped();
                _client.Delay(PollSeconds);
                xml = Xml();
            }
        }

        /// <summary>Chọn row kết quả khớp keyword trong snapshot (null nếu chưa có).</summary>
        private XmlNode? PickResultRow(string keyword, string xml)
        {
            if (string.IsNullOrEmpty(xml)) return null;

            // Còn đang ở tab RECENT = query chưa chạy xong -> chưa có kết quả nào để tin.
            if (!Exists(X_SEARCH_RESULTS, xml))
            {
                if (Exists(X_SEARCH_HISTORY, xml)) Log("Vẫn đang ở danh sách RECENT -> chờ kết quả thật.");
                return null;
            }

            List<XmlNode> rows;
            try { rows = _client.FindElementsNotToLower(0, xml, X_RESULT_ROW); }
            catch { return null; }
            if (rows == null || rows.Count == 0) return null;

            // Chấm điểm từng row rồi lấy row điểm cao nhất, thay vì "row đầu tiên khớp".
            // Keyword của user hay là "tên bài - ca sĩ" nên phải xét cả cột ca sĩ
            // (collection_item_subtitle_text1); bản cũ chỉ so với tiêu đề row.
            XmlNode? best = null;
            int bestScore = 0;
            bool bestIsSong = false;
            int scanned = 0;

            foreach (var row in rows)
            {
                // Row phải thuộc cây search_results (không phải search_history).
                if (!IsUnderSearchResults(row)) continue;

                string title = RowText(row, X_ROW_TITLE);
                if (string.IsNullOrEmpty(title)) continue;

                scanned++;
                string artist = RowText(row, X_ROW_ARTIST);
                int score = ScoreRow(title, artist, keyword);
                if (score <= 0) continue;

                // "Song - 4:45" = bài hát; "Album - 12 songs"/"Playlist" thì để dành làm
                // phương án cuối (bấm vào là mở trang, không phát đúng 1 bài).
                bool isSong = RowText(row, X_ROW_KIND).StartsWith("Song", StringComparison.OrdinalIgnoreCase);

                // Row bài hát luôn thắng row album/playlist dù điểm thấp hơn; trong cùng
                // nhóm thì điểm cao thắng. Điểm bằng nhau -> giữ row đứng trên (Pandora đã
                // xếp theo độ liên quan).
                if (best == null
                    || (isSong && !bestIsSong)
                    || (isSong == bestIsSong && score > bestScore))
                {
                    best = row;
                    bestScore = score;
                    bestIsSong = isSong;
                }
            }

            if (best == null)
                Log($"Có {scanned} row kết quả nhưng không row nào khớp \"{keyword}\".");
            else
                Log($"Row khớp \"{keyword}\": \"{RowText(best, X_ROW_TITLE)}\" - " +
                    $"{RowText(best, X_ROW_ARTIST)} (điểm {bestScore}, {scanned} row đã xét).");
            return best;
        }

        /// <summary>
        /// Mức khớp giữa 1 row kết quả và keyword của user. 0 = loại hẳn.
        ///
        /// LỖI ĐÃ SỬA: keyword user nhập thường là "tên bài - tên ca sĩ"
        /// ("Shape of You - Ed Sheeran"), trong khi tiêu đề row chỉ có TÊN BÀI
        /// ("Shape of You") còn tên ca sĩ nằm ở dòng subtitle riêng
        /// (collection_item_subtitle_text1). Điều kiện cũ "MỌI từ của keyword phải có
        /// trong tiêu đề row" nên không bao giờ đúng: "ed"/"sheeran" không nằm trong tiêu
        /// đề -> mọi row bị loại, WaitResultRow hết hạn và tool KHÔNG bấm gì dù màn hình
        /// đang hiện đúng bài.
        ///
        /// Cách chấm mới đối chiếu keyword với CẢ tiêu đề lẫn cột ca sĩ:
        ///   3 = khớp cả tên bài lẫn tên ca sĩ (đúng bản user muốn)
        ///   2 = khớp trọn keyword trong tiêu đề
        ///   1 = khớp đủ tên bài, ca sĩ không khớp (catalogue ghi tên khác)
        ///   0 = thiếu tên bài -> loại
        /// </summary>
        private static int ScoreRow(string title, string artist, string keyword)
        {
            var titleWords = MatchWords(title);
            if (titleWords.Count == 0) return 0;

            var rowWords = new HashSet<string>(titleWords, StringComparer.OrdinalIgnoreCase);
            rowWords.UnionWith(MatchWords(artist));

            var allWords = MatchWords(keyword);
            if (allWords.Count == 0) return 0;

            var songWords = MatchWords(SongPart(keyword));

            // Keyword KHÔNG tách được phần tên bài (viết liền, không có dấu phân cách):
            // "Shape of You Ed Sheeran", "Partly Thao Sam Thao Sam". Ưu tiên row khớp trọn
            // trong tiêu đề, rồi tới row phải mượn thêm cột ca sĩ mới đủ từ.
            if (songWords.Count == 0 || songWords.SetEquals(allWords))
            {
                if (allWords.All(titleWords.Contains)) return 3;
                if (allWords.All(rowWords.Contains)) return 2;
                return IsPartialTitleOf(titleWords, allWords, rowWords) ? 1 : 0;
            }

            // Tách được: tên bài BẮT BUỘC khớp, tên ca sĩ chỉ để cộng điểm.
            // Chấp nhận khớp qua rowWords vì có row đưa hết "tên bài (feat. X)" vào tiêu đề.
            if (!songWords.All(titleWords.Contains) && !songWords.All(rowWords.Contains))
                return IsPartialTitleOf(titleWords, allWords, rowWords) ? 1 : 0;

            var artistWords = new HashSet<string>(allWords, StringComparer.OrdinalIgnoreCase);
            artistWords.ExceptWith(songWords);
            if (artistWords.Count == 0) return 2;

            return artistWords.All(rowWords.Contains) ? 3 : 1;
        }

        /// <summary>
        /// Cứu cánh khi tiêu đề row NGẮN HƠN keyword: mọi từ của tiêu đề đều nằm trong
        /// keyword và keyword đã được row giải thích quá nửa.
        ///
        /// Cần vì user dán tên ca sĩ (đôi khi cả người hát cùng) vào sau tên bài:
        /// "Music And Boxper Thao Sam Thao Sam" trong khi row chỉ ghi title "Music" +
        /// ca sĩ "Thao Sam". Đòi đủ MỌI từ thì row đúng bị loại và tool không bấm gì —
        /// đúng lỗi user báo. Điểm 1 là mức thấp nhất nên row khớp đẹp vẫn được chọn trước.
        /// </summary>
        private static bool IsPartialTitleOf(HashSet<string> titleWords,
                                             HashSet<string> allWords,
                                             HashSet<string> rowWords)
        {
            if (!titleWords.All(allWords.Contains)) return false;
            int matched = allWords.Count(rowWords.Contains);
            return matched * 2 >= allWords.Count;
        }

        /// <summary>
        /// Row có nằm trong :id/search_results (kết quả thật) hay không.
        /// CẢNH BÁO đã kiểm chứng bằng dump thật: tab RECENT dùng LẠI cả
        /// :id/search_results_recycler_view, chỉ khác ở tổ tiên xa hơn:
        ///   RECENT     : ...recycler_view &lt;- history_pager &lt;- search_history
        ///   Kết quả thật: ...recycler_view &lt;- search_results_view &lt;- results_pager &lt;- search_results
        /// Nên phải duyệt HẾT chuỗi tổ tiên: gặp search_history là loại, và chỉ nhận khi
        /// thấy search_results/search_results_view. Nhận theo recycler_view là dính lại đúng
        /// lỗi cũ (row của keyword trước bị tính là kết quả mới).
        /// </summary>
        private static bool IsUnderSearchResults(XmlNode? row)
        {
            bool underResults = false;
            for (var p = row?.ParentNode; p != null; p = p.ParentNode)
            {
                string id = p.Attributes?["resource-id"]?.Value ?? "";
                if (id.EndsWith(":id/search_history", StringComparison.OrdinalIgnoreCase)
                    || id.EndsWith(":id/history_pager", StringComparison.OrdinalIgnoreCase))
                    return false;
                if (id.EndsWith(":id/search_results", StringComparison.OrdinalIgnoreCase)
                    || id.EndsWith(":id/search_results_view", StringComparison.OrdinalIgnoreCase)
                    || id.EndsWith(":id/results_pager", StringComparison.OrdinalIgnoreCase))
                    underResults = true;
            }
            return underResults;
        }

        /// <summary>
        /// Tách chuỗi thành các từ chữ-số viết thường (bỏ dấu câu, dấu ngoặc...).
        ///
        /// Dấu nháy đơn KHÔNG phải ký tự ngắt từ: keyword của user viết "Don't" còn Pandora
        /// có thể hiện "Dont"/"Don’t" (nháy cong). Nếu ngắt thì "don" + "t" sẽ không bao giờ
        /// khớp "dont" -> row đúng bị chấm 0 điểm và tool không bấm gì.
        /// </summary>
        private static HashSet<string> Tokenize(string value)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(value)) return set;

            var sb = new StringBuilder();
            foreach (char ch in value)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
                else if (ch == '\'' || ch == '’' || ch == 'ʼ') continue;
                else if (sb.Length > 0) { set.Add(sb.ToString()); sb.Clear(); }
            }
            if (sb.Length > 0) set.Add(sb.ToString());
            return set;
        }

        /// <summary>Đọc text (hoặc content-desc) của node con trong 1 row kết quả.</summary>
        private static string RowText(XmlNode? row, string relativeXpath)
        {
            var node = FindInRow(row, relativeXpath);
            if (node == null) return "";
            string text = node.Attributes?["text"]?.Value ?? "";
            if (string.IsNullOrWhiteSpace(text))
                text = node.Attributes?["content-desc"]?.Value ?? "";
            return text.Trim();
        }

        /// <summary>
        /// Tìm node con theo xpath TƯƠNG ĐỐI trong 1 row. Không dùng ADBClient.FindElements
        /// được vì hàm đó chạy trên cả tài liệu; ở đây phải giới hạn trong đúng row đã chọn.
        /// </summary>
        private static XmlNode? FindInRow(XmlNode? row, string relativeXpath)
        {
            if (row == null) return null;
            try { return row.SelectSingleNode(relativeXpath); }
            catch { return null; }
        }

        /// <summary>Tap vào tâm node đã có sẵn trong snapshot (không dump lại).</summary>
        private bool TapNode(XmlNode? node)
        {
            string bounds = node?.Attributes?["bounds"]?.Value ?? "";
            if (string.IsNullOrEmpty(bounds)) return false;
            try
            {
                var point = new RectangleArea(bounds).GetCenterPoint();
                Log($"TapNode {node?.Attributes?["resource-id"]?.Value} -> ({point.X},{point.Y})");
                _client.Click(point.X, point.Y);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Vòng lặp state-machine sau khi bấm Play: mỗi lượt dump 1 lần, ra case nào xử lý case đó.
        /// Các trạng thái thật đã gặp trên máy:
        ///  1. Đang chạy quảng cáo -> CHỜ, chỉ bấm close khi nút đã bấm được.
        ///  2. Gate free user "Play This Song?" -> BẮT BUỘC bấm "Watch Ad" để phát đúng bài
        ///     đã tìm (nằm CHỒNG trên mini player nên phải xét TRƯỚC).
        ///     Chỉ khi không có Watch Ad mới fallback "Start Station" (radio gợi ý).
        ///  3. Player mở full: có now_playing_toolbar.
        ///  4. Phát luôn nhưng player vẫn thu nhỏ: chỉ có mini_player_handle
        ///     với content-desc "Mini Player &lt;bài&gt; &lt;ca sĩ&gt;".
        ///
        /// <paramref name="expectedTrack"/> = tên bài đọc từ ĐÚNG row kết quả vừa bấm.
        /// Chỉ trả true khi player đang phát ĐÚNG bài đó.
        ///
        /// Bản cũ nhận previousTrack và coi "tên bài khác lúc trước" là thành công. Cách đó
        /// sai vì Pandora TỰ NHẢY SANG BÀI KHÁC khi bài cũ kết thúc: chạy xong "STAY" app tự
        /// qua "Heat Waves", nên khi keyword kế bấm nhầm vào row cũ và phát lại "STAY" thì
        /// "STAY" != "Heat Waves" -> code tưởng đã phát bài mới và tính là thành công
        /// (đúng hiện tượng keyword 5 "Someone Like You" nghe nhầm "STAY" 120s).
        /// So với bài MONG ĐỢI thì việc app tự nhảy bài không còn ảnh hưởng gì.
        /// </summary>
        private bool HandlePlayGate(string expectedTrack)
        {
            // Thứ tự case = thứ tự ưu tiên (MatchCase trả về xpath nào match TRƯỚC).
            // 1. X_AD_CLOSE trước X_AD_PLAYING: lúc quảng cáo hết, cả container quảng cáo
            //    và nút close đều còn trên cây view; nếu X_AD_PLAYING match trước thì chờ
            //    mãi không bao giờ bấm close. Nút close chỉ clickable khi hết thời lượng
            //    bắt buộc nên đặt trước cũng không làm skip quảng cáo sớm.
            // 2. Gate (Watch Ad / offer-header / Start Station) phải đứng TRƯỚC X_AD_PLAYING:
            //    màn gate của tài khoản free ĐÃ CÓ SẴN container quảng cáo (sl_banner_ad,
            //    "Please wait"…) nên nếu để X_AD_PLAYING trước thì mọi lần vào gate đều bị
            //    nhận nhầm là "đang xem quảng cáo" -> chờ hết timeout rồi mới bấm Watch Ad.
            // Chưa bấm Watch Ad: KHÔNG xét X_AD_PLAYING chút nào.
            var casesBeforeAd = new List<string>();
            casesBeforeAd.AddRange(X_AD_CLOSE);
            casesBeforeAd.AddRange(X_WATCH_AD);
            casesBeforeAd.Add(X_OFFER_HEADER);
            casesBeforeAd.Add(X_START_STATION);
            casesBeforeAd.Add(X_NOW_PLAYING_BAR);
            casesBeforeAd.Add(X_COACHMARK);
            casesBeforeAd.Add(X_MINI_HANDLE);
            // Promo Airship xếp CUỐI: nó là Activity riêng nên khi hiện thì không case nào
            // khác match được, còn khi đã có gate/quảng cáo thật thì ưu tiên xử lý gate trước.
            casesBeforeAd.AddRange(X_PROMO);

            // Đã bấm Watch Ad: quảng cáo che player nên X_AD_PLAYING phải đứng trước
            // các case "đang phát", tránh nhận nhầm mini player nằm dưới lớp quảng cáo.
            // Khi quảng cáo không còn, player phải được ưu tiên trước fallback Skip: nút tua lùi
            // của player cũng có content-desc chứa "Skip", không được tap như nút quảng cáo.
            var casesDuringAd = new List<string>();
            casesDuringAd.AddRange(X_AD_CLOSE);
            casesDuringAd.AddRange(X_AD_PLAYING);
            casesDuringAd.Add(X_NOW_PLAYING_BAR);
            casesDuringAd.Add(X_MINI_HANDLE);
            casesDuringAd.AddRange(X_AD_SKIP_FALLBACK);
            casesDuringAd.AddRange(X_WATCH_AD);
            casesDuringAd.Add(X_OFFER_HEADER);
            casesDuringAd.Add(X_START_STATION);
            casesDuringAd.Add(X_COACHMARK);
            casesDuringAd.AddRange(X_PROMO);

            var sw = Stopwatch.StartNew();
            bool adRequested = false;
            var adSw = new Stopwatch();
            int promoClosed = 0;
            var promoSw = new Stopwatch();

            // Đồng hồ riêng cho lúc player đã hiện: dùng để chốt "sai bài" sau
            // WrongTrackTimeout thay vì đợi hết GateTimeout (120s).
            var playSw = new Stopwatch();
            _wrongTrack = false;

            // Quảng cáo reward đo thật trên máy: bấm Watch Ad -> ~10s video -> dialog
            // "Loading…" -> vào bài ở giây ~20-25.
            while (sw.Elapsed.TotalSeconds < GateTimeout)
            {
                _client.ThrowIfStopped();
                if (_wrongTrack) return false;

                // 1 dump / 1 lượt, so khớp toàn bộ case trong bộ nhớ. Bản cũ dùng
                // _client.FindElement("", cases, 10) rồi bên trong các case còn gọi
                // FindElement/Exists KHÔNG truyền snapshot -> tới 5 lần dump lồng nhau
                // cho mỗi lượt. Nút Watch Ad đã hiện trên màn hình vẫn phải đợi hết
                // các lượt miss đó (~3 phút) mới được bấm.
                string xml = Xml();
                if (DismissSkipPrompt(xml)) continue;
                string _case = MatchCase(xml, adRequested ? casesDuringAd : casesBeforeAd);
                if (string.IsNullOrEmpty(_case))
                {
                    // Promo chen ngang thường ăn luôn lượt phát: đóng xong app về Home, không
                    // còn gate cũng không có player. Chờ thêm PromoRecoverSeconds cho Pandora
                    // kịp bung player; hết mốc đó thì trả sớm cho keyword kế thay vì nằm hết
                    // GateTimeout (120s) — đây là phần "chưa mượt" khi sang keyword tiếp theo.
                    if (promoSw.IsRunning && promoSw.Elapsed.TotalSeconds > PromoRecoverSeconds)
                    {
                        Log("Sau khi đóng promo không thấy gate/player -> bỏ qua keyword này.");
                        return false;
                    }
                    _client.Delay(PollSeconds);
                    continue;
                }

                // Gate hiện nhưng suốt GateBeforeAdTimeout giây không ra nút bấm được
                // -> nhường keyword kế thay vì giữ trọn GateTimeout cho 1 bài không mở được.
                if (!adRequested && _case == X_OFFER_HEADER
                    && sw.Elapsed.TotalSeconds > GateBeforeAdTimeout)
                {
                    Log($"Gate không ra nút sau {GateBeforeAdTimeout}s -> bỏ qua keyword này.");
                    return false;
                }

                // Đã ra lại gate/quảng cáo/player thật -> promo không còn là lý do đứng hình,
                // tắt đồng hồ chờ để nhánh trên không cắt sớm một lượt phát đang tiến triển.
                if (!X_PROMO.Contains(_case)) promoSw.Reset();

                // Quay lại gate/quảng cáo (bấm Watch Ad xong, promo chen ngang...) nghĩa là
                // bài chưa thật sự bắt đầu -> đặt lại đồng hồ đối chiếu tên bài, tránh tính
                // luôn cả thời gian xem quảng cáo vào WrongTrackTimeout.
                if (_case != X_NOW_PLAYING_BAR && _case != X_MINI_HANDLE) playSw.Reset();

                Log($"gate case={_case}");
                switch (_case)
                {
                    // ---- Promo Pandora Plus chen ngang (Airship HtmlActivity) ----
                    // Đè lên cả gate lẫn player nên nếu không đóng thì mọi case khác đều miss.
                    // Đóng nhiều lần vẫn hiện lại = Pandora đang ép mua gói, không phát được
                    // bài đã tìm -> nhường keyword kế thay vì đốt hết GateTimeout.
                    case var c when X_PROMO.Contains(c):
                        if (++promoClosed > MaxPromoPerPlay)
                        {
                            Log($"Promo hiện lại {promoClosed} lần -> bỏ qua keyword này.");
                            return false;
                        }
                        DismissPromo(xml);
                        promoSw.Restart();
                        break;

                    // ---- Quảng cáo đang chạy: chờ hết, KHÔNG tap bừa ----
                    // Case này chỉ nằm trong casesDuringAd (sau khi đã bấm Watch Ad) vì
                    // container quảng cáo tồn tại sẵn trên màn gate/Home của tài khoản free.
                    // Quảng cáo reward thật chỉ ~20-25s; quá AdWatchTimeout là kẹt (không có
                    // nút close, container quảng cáo còn treo) -> thoát để keyword kế thử lại.
                    case var c when X_AD_PLAYING.Contains(c):
                        if (adSw.Elapsed.TotalSeconds > AdWatchTimeout)
                        {
                            Log($"Quảng cáo treo quá {AdWatchTimeout}s -> bỏ qua keyword này.");
                            return false;
                        }
                        _mainService.SetStatus($"Đang xem quảng cáo để mở bài ({(int)adSw.Elapsed.TotalSeconds}s)...", 3);
                        _client.Delay(4, 6);
                        break;

                    // ---- Quảng cáo xong: đóng để về bài hát ----
                    case var c when X_AD_CLOSE.Contains(c) || X_AD_SKIP_FALLBACK.Contains(c):
                        _mainService.SetStatus("Quảng cáo xong - đóng để phát bài.", 3);
                        TapIn(c, xml);
                        _client.Delay(3, 5);
                        break;

                    // ---- Gate free user: BẮT BUỘC bấm Watch Ad ----
                    case var c when X_WATCH_AD.Contains(c):
                        _mainService.SetStatus("Tài khoản free - bấm Watch Ad để phát đúng bài.", 3);
                        // Tap ngay trên snapshot vừa dump: nút đã nằm trong đó nên không cần
                        // chờ thêm giây nào. Đây là chỗ trước đây mất ~3 phút.
                        if (!TapIn(c, xml)) break;
                        adRequested = true;
                        adSw.Restart();
                        _client.Delay(4, 6);
                        break;

                    // ---- Có header gate nhưng chưa match được nút Watch Ad ----
                    case var c when c == X_OFFER_HEADER:
                        // TUYỆT ĐỐI không bấm "No, Thanks"/"Close"/"Start Free Trial"
                        // (X_GATE_DECLINE) vì sẽ hủy lượt phát bài đã tìm.
                        // Nếu gate CHỈ còn nút hủy (không có Watch Ad, không có Start Station)
                        // thì coi như không phát được -> thoát để keyword kế thử lại.
                        // Xét trên CÙNG snapshot: bản cũ gọi FindElement(3s) + 4 lần Exists()
                        // không truyền xml -> 5 lần dump lồng trong 1 lượt.
                        if (X_GATE_DECLINE.Any(xp => Exists(xp, xml)))
                        {
                            Log("Gate chỉ có nút hủy -> bỏ qua keyword này.");
                            return false;
                        }
                        _client.Delay(PollSeconds);
                        break;

                    // ---- Không có Watch Ad -> fallback radio station ----
                    case var c when c == X_START_STATION:
                        // MatchCase xét X_WATCH_AD trước X_START_STATION nên vào được đây
                        // nghĩa là snapshot này KHÔNG có Watch Ad, không cần dò lại.
                        _mainService.SetStatus("Không có Watch Ad - chuyển sang Start Station.", 3);
                        TapIn(X_START_STATION, xml);
                        _client.Delay(6, 9);
                        break;

                    // ---- Player mở full ----
                    case var c when c == X_NOW_PLAYING_BAR:
                        if (TapIn(X_PAUSED, xml))
                        {
                            Log("Player full đang pause -> bấm phát.");
                            _client.Delay(2, 4);
                            break;
                        }
                        if (!AcceptPlayingTrack(xml, expectedTrack, playSw, "player full")) break;
                        return true;

                    // ---- Overlay che màn hình ----
                    case var c when c == X_COACHMARK:
                        DismissOverlays();
                        break;

                    // ---- Player thu nhỏ ----
                    case var c when c == X_MINI_HANDLE:
                        if (TapIn(X_PAUSED, xml))
                        {
                            Log("Mini player đang pause -> bấm phát.");
                            _client.Delay(2, 4);
                            break;
                        }
                        if (!AcceptPlayingTrack(xml, expectedTrack, playSw, "mini player")) break;
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Player đã hiện — kiểm tra có đang phát ĐÚNG bài mong đợi không.
        /// Trả true = nhận (SearchAndPlay tính là thành công).
        /// Trả false = chưa chắc, gọi lại ở lượt sau; quá <see cref="WrongTrackTimeout"/> giây
        /// vẫn sai bài thì ném cờ để vòng gate thoát sớm với kết quả thất bại.
        /// </summary>
        private bool AcceptPlayingTrack(string xml, string expectedTrack, Stopwatch playSw, string where)
        {
            if (!playSw.IsRunning) playSw.Restart();

            string current = GetNowPlayingTrack(xml);

            // Chưa đọc được tên bài (metadata đang nạp) -> chờ thêm, đừng vội tính thành công.
            if (string.IsNullOrEmpty(current))
            {
                if (playSw.Elapsed.TotalSeconds > WrongTrackTimeout)
                {
                    Log($"Không đọc được tên bài trên {where} sau {WrongTrackTimeout}s -> coi như không phát được.");
                    _wrongTrack = true;
                    return false;
                }
                _client.Delay(PollSeconds);
                return false;
            }

            // Nếu không có expectedTrack (mở URL), chấp nhận mọi bài hát đang phát
            if (string.IsNullOrEmpty(expectedTrack))
            {
                Log($"Đang phát bài từ URL ({where}): {current}");
                _lastPlayedTrack = current;
                return true;
            }

            if (IsExpectedTrack(current, expectedTrack))
            {
                Log($"Đang phát ĐÚNG bài ({where}): {current}");
                _lastPlayedTrack = current;
                return true;
            }

            // Bài trên player KHÔNG phải bài vừa bấm: hoặc bài cũ còn chạy (Pandora chưa kịp
            // đổi), hoặc app tự nhảy bài. Chờ trong hạn rồi mới kết luận.
            if (playSw.Elapsed.TotalSeconds > WrongTrackTimeout)
            {
                Log($"SAI BÀI: đang phát \"{current}\" trong khi cần \"{expectedTrack}\" -> bỏ keyword này.");
                _wrongTrack = true;
                return false;
            }

            Log($"Player đang là \"{current}\", chờ đổi sang \"{expectedTrack}\" ({(int)playSw.Elapsed.TotalSeconds}s).");
            _client.Delay(PollSeconds);
            return false;
        }

        /// <summary>Cờ nội bộ: HandlePlayGate đã kết luận phát sai bài -> thoát vòng ngay.</summary>
        private bool _wrongTrack;

        /// <summary>Tên bài THẬT SỰ đang phát (đã đối chiếu khớp keyword) — để báo trong status.</summary>
        private string _lastPlayedTrack = "";

        /// <summary>
        /// Bài đang phát có phải bài mong đợi không.
        /// Chuỗi trên player khác nhau tùy trạng thái (đã dump thật):
        ///   player full  -> "Someone Like You"
        ///   mini player  -> "Someone Like You Adele"  (content-desc kèm ca sĩ)
        /// còn expected đọc từ row kết quả -> "Someone Like You".
        /// Vì vậy so theo TỪ: mọi từ của tên bài mong đợi phải có trong tên bài đang phát.
        /// (StartsWith như bản cũ không chịu được biến thể "(feat. ...)" hay thứ tự khác.)
        /// </summary>
        private static bool IsExpectedTrack(string current, string expected)
        {
            if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(expected)) return false;
            var want = Tokenize(expected);
            if (want.Count == 0) return false;
            var have = Tokenize(current);
            return want.All(w => have.Contains(w));
        }

        /// <summary>
        /// Nghe bài hiện tại cho đến khi HẾT BÀI, tối đa <paramref name="maxSeconds"/> giây.
        /// Hai dấu hiệu hết bài, đều đã kiểm chứng trên máy thật (Dance Monkey 3:29):
        ///  1. progress_elapsed_text chạy tới gần progress_remaining_text (tổng thời lượng).
        ///     Đây là nguồn CHÍNH VÌ CHÍNH XÁC: biết luôn bài dài bao nhiêu nên không phải
        ///     đoán trong khoảng 120-240s.
        ///  2. Pandora tự nhảy sang bài kế -> tên bài đổi (đã thấy: "Dance Monkey" 3:29
        ///     -> "Sweet Dreams (Are Made of This)" 3:37). Dùng làm phương án dự phòng khi
        ///     player thu nhỏ và không đọc được thanh tiến trình.
        /// Trả về (số giây đã nghe, có bị mất player giữa bài hay không) — cờ thứ 2 để
        /// HDNgheNhac biết cần khôi phục app trước keyword kế tiếp, chứ không kết thúc hành động.
        /// </summary>
        /// <summary>Nhịp poll khi đang nghe (giây). Phải nhỏ hơn EndOfSongWindow * 2 để không lố sang bài kế.</summary>
        private const int ListenPollSeconds = 6;

        /// <summary>Còn bao nhiêu giây cuối bài thì coi như hết bài (theo thanh tiến trình).</summary>
        private const int EndOfSongWindow = 5;

        private async Task<(int listened, bool stalled)> ListenAsync(int maxSeconds)
        {
            int elapsed = 0;
            int stalled = 0;
            int lastPos = -1;
            int frozen = 0;
            int noProgress = 0;
            int promoClosed = 0;

            // Mở to player NGAY từ đầu: chỉ player full mới có progress_elapsed_text /
            // progress_remaining_text — nguồn duy nhất biết chính xác điểm hết bài. Nếu để
            // player thu nhỏ thì chỉ còn cách đoán qua tên bài (chậm và dễ sai).
            _fullPlayerFails = 0;   // mỗi bài được thử lại từ đầu
            EnsureFullPlayer();
            string startedTrack = GetNowPlayingTrack();

            while (elapsed < maxSeconds)
            {
                _client.ThrowIfStopped();
                if (Stop()) break;
                await _mainService.Stop();

                int step = Math.Min(ListenPollSeconds, maxSeconds - elapsed);
                _client.Delay(step);
                elapsed += step;

                string xml = Xml();

                // Dialog "Skip Anyway" chen ngang giữa bài -> bấm luôn rồi nghe tiếp.
                if (DismissSkipPrompt(xml)) continue;

                // Vẫn đang phát? (player full hoặc mini player thu nhỏ đều tính là đang nghe)
                if (Exists(X_PLAY_PAUSE, xml))
                {
                    stalled = 0;

                    // Nút hiển thị "Play" nghĩa là đang tạm dừng -> bấm phát tiếp.
                    if (TapIn(X_PAUSED, xml))
                    {
                        Log("Đang pause -> resume.");
                        _client.Delay(2, 3);
                    }

                    string title = GetNowPlayingTrack(xml);

                    // Player bị thu nhỏ giữa bài (bấm Back, coachmark, Pandora tự thu) ->
                    // mất thanh tiến trình. Mở to lại rồi dump lại để vòng này vẫn đọc được.
                    if (!Exists(X_NOW_PLAYING_BAR, xml) && EnsureFullPlayer(xml))
                    {
                        xml = Xml();
                        title = GetNowPlayingTrack(xml);
                    }

                    // ── Dấu hiệu 1 (chính xác): đọc thanh tiến trình của player full ──
                    // elapsed/total dạng "1:09" / "3:29". Còn <= EndOfSongWindow giây là coi
                    // như hết bài: nhịp poll ListenPollSeconds nên chờ thêm 1 vòng sẽ lố sang
                    // bài kế (cửa sổ phải >= nửa nhịp poll để không bao giờ miss nhịp).
                    int posSec = ParseClock(ReadAttr(xml, X_PROGRESS_ELAPSED, "text"));
                    int totalSec = ParseClock(ReadAttr(xml, X_PROGRESS_TOTAL, "text"));
                    if (posSec >= 0 && totalSec > 0 && posSec >= totalSec - EndOfSongWindow)
                    {
                        Log($"Hết bài \"{title}\" ({posSec}/{totalSec}s) sau {elapsed}s nghe -> tìm keyword kế.");
                        return (elapsed, false);
                    }

                    // ── Dấu hiệu 2 (dự phòng): Pandora tự nhảy bài nên tên bài đổi ──
                    // Chỉ tin sau MinSongSeconds để không cắt sớm lúc metadata chưa ổn định.
                    // Dùng IsSameTrack (không phải Equals) vì player full và mini player
                    // cho 2 dạng chuỗi khác nhau cho CÙNG một bài.
                    if (elapsed >= MinSongSeconds
                        && !string.IsNullOrEmpty(title)
                        && !string.IsNullOrEmpty(startedTrack)
                        && !IsSameTrack(title, startedTrack))
                    {
                        Log($"Đã sang bài mới \"{title}\" sau {elapsed}s -> hết bài \"{startedTrack}\".");
                        return (elapsed, false);
                    }

                    // ── Player còn đó nhưng nhạc KHÔNG chạy ──
                    // Bài hết mà Pandora không tự phát tiếp (hết lượt skip/gate mới) thì
                    // player vẫn hiện, chỉ có tiến trình đứng im. Không bắt trường hợp này
                    // thì vòng lặp nằm chờ tới hết maxSeconds mà chẳng nghe được gì.
                    if (posSec >= 0 && posSec == lastPos)
                    {
                        frozen++;
                        if (frozen >= 3)
                        {
                            Log($"Tiến trình đứng ở {posSec}s qua {frozen} lượt -> coi như hết bài.");
                            return (elapsed, true);
                        }
                    }
                    else
                    {
                        frozen = 0;
                    }
                    lastPos = posSec;

                    // ── KHÔNG đọc được tiến trình: bài vẫn nghe, chỉ thiếu thước đo ──
                    // Đếm liên tiếp (đã xét lồng vào nhịp poll). Quá NoProgressMaxSeconds thì
                    // cắt bài, KHÔNG nghe tới hết maxSeconds (đó là lý do 1 bài chiếm trọn
                    // ngân sách, danh sách keyword không bao giờ chuyển bài).
                    if (posSec < 0)
                    {
                        noProgress += ListenPollSeconds;
                        if (noProgress >= NoProgressMaxSeconds)
                        {
                            Log($"Không đọc được tiến trình {noProgress}s -> hết bài \"{title}\".");
                            return (elapsed, false);
                        }
                    }
                    else
                    {
                        noProgress = 0;
                    }

                    _mainService.SetStatus(
                        string.IsNullOrEmpty(title)
                            ? $"Đang nghe nhạc ({elapsed}/{maxSeconds}s)"
                            : totalSec > 0
                                ? $"Đang nghe: {title} [{posSec}/{totalSec}s] ({elapsed}/{maxSeconds}s)"
                                : $"Đang nghe: {title} ({elapsed}/{maxSeconds}s)",
                        2);
                }
                else
                {
                    // Promo Pandora Plus chen ngang giữa bài: Activity riêng của Airship che
                    // player nên X_PLAY_PAUSE biến mất. KHÔNG phải mất player - đóng đúng nút
                    // của nó rồi nghe tiếp. Trước đây X_DISMISS không khớp màn này nên phải
                    // đợi đủ 3 lượt (~18s) mới thoát qua RecoverAsync, vừa hụt bài vừa trễ
                    // sang keyword kế.
                    if (promoClosed < MaxPromoPerSong && DismissPromo(xml))
                    {
                        promoClosed++;
                        _mainService.SetStatus(
                            $"Đã đóng promo chen ngang ({promoClosed}/{MaxPromoPerSong}) - nghe tiếp.", 3);
                        continue;
                    }

                    stalled++;
                    // Có thể bị quảng cáo/coachmark chen ngang.
                    DismissOverlays();
                    if (stalled >= 3)
                    {
                        Log("Mất player 3 lần liên tiếp -> trả quyền cho vòng lặp keyword.");
                        return (elapsed, true);
                    }
                }
            }

            return (elapsed, false);
        }

        /// <summary>
        /// Đổi chuỗi thời gian của player ("3:29", "1:02:11") thành số giây.
        /// Trả về -1 nếu không đọc được — caller phải kiểm tra trước khi so sánh.
        /// </summary>
        private static int ParseClock(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return -1;

            var parts = value.Trim().Split(':');
            if (parts.Length < 2 || parts.Length > 3) return -1;

            int total = 0;
            foreach (var part in parts)
            {
                if (!int.TryParse(part.Trim(), out int n) || n < 0) return -1;
                total = total * 60 + n;
            }
            return total;
        }

        /// <summary>Đọc attribute đầu tiên tìm được, giữ nguyên chữ hoa/thường để hiện log.</summary>
        private string ReadAttr(string xml, string xpath, params string[] attrs)        {
            try
            {
                var nodes = _client.FindElementsNotToLower(0, xml, xpath);
                if (nodes.Count == 0) return "";
                foreach (var attr in attrs)
                {
                    string value = nodes[0].Attributes?[attr]?.Value ?? "";
                    if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
                }
            }
            catch { }
            return "";
        }

        private string GetTrackTitle(string xml)
        {
            // premium_track_view để text rỗng và đặt tên bài ở content-desc.
            foreach (var xpath in X_TRACK_TITLE)
            {
                string title = ReadAttr(xml, xpath, "text", "content-desc");
                if (!string.IsNullOrEmpty(title)) return title;
            }
            return "";
        }

        /// <summary>
        /// Đảm bảo player đang mở FULL (có now_playing_toolbar). Chỉ ở trạng thái này mới
        /// đọc được progress_elapsed_text / progress_remaining_text — nguồn chính xác nhất
        /// để biết bài đã hết. Player thu nhỏ chỉ còn mini_player_handle, không có tiến trình.
        /// Trả về true nếu sau khi gọi player đang ở dạng full.
        ///
        /// TẠI SAO KHÔNG TAP TÂM mini_player_handle: dump thật cho bounds
        /// [0,2000][1440,2392] -> tâm là (720,2196), mà bottom_navigation nằm
        /// [0,2196][1440,2392] và tab_search chiếm [720,2196][1080,2392]. Tap tâm rơi ĐÚNG
        /// vào tab Search chứ không vào handle, nên player không bao giờ bung — log in
        /// "Player đang thu nhỏ -> mở to" hàng chục lần trong 1 lần chạy và chỉ bài nào tự
        /// mở full mới đọc được thanh tiến trình.
        ///
        /// Cách làm mới, đã kiểm chứng trên máy 5200d004b263c49d:
        ///  1. Tap trong phần handle NẰM TRÊN bottom nav (khoảng giữa mép trên handle và
        ///     mép trên bottom nav), lệch trái để tránh nút play [1254,2000][1422,2168].
        ///  2. Không được thì swipe từ đó lên giữa màn (SlidingUpPanelLayout: kéo lên là
        ///     cử chỉ tự nhiên để bung panel).
        /// </summary>
        private bool EnsureFullPlayer(string snapshot = "")
        {
            // Người gọi trong ListenAsync đã dump và biết chắc không có now_playing_toolbar
            // -> dùng lại snapshot đó, khỏi dump thêm 1 lần mỗi nhịp poll.
            string xml = string.IsNullOrEmpty(snapshot) ? Xml() : snapshot;
            if (Exists(X_NOW_PLAYING_BAR, xml)) return true;
            if (!Exists(X_MINI_HANDLE, xml))
            {
                _fullPlayerFails = 0;
                return false;
            }

            // Mở không được nhiều lần liên tiếp thì thôi hẳn cho tới hết bài: mỗi lần thử
            // tốn 1 dump + 1 tap, lặp mỗi nhịp poll là rất nhiều thao tác vô ích.
            if (_fullPlayerFails >= MaxFullPlayerTries) return false;

            var handle = FirstNode(xml, X_MINI_HANDLE);
            if (handle == null) return false;
            var area = ParseBounds(handle);
            if (area == null)
            {
                _fullPlayerFails++;
                return false;
            }

            int top = area.Value.top;
            int bottom = area.Value.bottom;
            int navTop = NavTop(xml, bottom);

            // Điểm tap: giữa mép trên handle và mép trên bottom nav (nếu bottom nav nằm trong
            // vùng handle), lệch sang trái 45% chiều ngang để không đụng nút play/pause.
            int x = area.Value.left + (int)((area.Value.right - area.Value.left) * 0.45);
            int y = navTop > top ? (top + navTop) / 2 : (top + bottom) / 2;

            Log($"Player đang thu nhỏ -> mở to (tap {x},{y}).");
            _client.Click(x, y);
            _client.Delay(2, 3);
            if (Exists(X_NOW_PLAYING_BAR))
            {
                _fullPlayerFails = 0;
                return true;
            }

            // Fallback: kéo panel lên. SlidingUpPanelLayout bung theo cử chỉ này.
            Log("Tap không bung được player -> swipe kéo lên.");
            _client.Swipe(x, y, x, Math.Max(200, y - 1200), 400);
            _client.Delay(2, 3);
            if (Exists(X_NOW_PLAYING_BAR))
            {
                _fullPlayerFails = 0;
                return true;
            }

            _fullPlayerFails++;
            if (_fullPlayerFails >= MaxFullPlayerTries)
                Log($"Không mở được player full sau {_fullPlayerFails} lần -> nghe theo tên bài, thôi thử lại.");
            return false;
        }

        /// <summary>Số lần thử mở player full liên tiếp trước khi bỏ hẳn (cho bài hiện tại).</summary>
        private const int MaxFullPlayerTries = 3;

        private int _fullPlayerFails;

        /// <summary>Mép trên của bottom_navigation nếu nó nằm trong vùng mini player.</summary>
        private int NavTop(string xml, int fallback)
        {
            var nav = FirstNode(xml, X_BOTTOM_NAV);
            var area = ParseBounds(nav);
            return area?.top ?? fallback;
        }

        /// <summary>Node đầu tiên khớp xpath trong snapshot (null nếu không có).</summary>
        private XmlNode? FirstNode(string xml, string xpath)
        {
            if (string.IsNullOrEmpty(xml)) return null;
            try
            {
                var nodes = _client.FindElementsNotToLower(0, xml, xpath);
                return nodes.Count > 0 ? nodes[0] : null;
            }
            catch { return null; }
        }

        /// <summary>Đọc bounds "[l,t][r,b]" của node thành 4 số.</summary>
        private static (int left, int top, int right, int bottom)? ParseBounds(XmlNode? node)
        {
            string bounds = node?.Attributes?["bounds"]?.Value ?? "";
            var nums = System.Text.RegularExpressions.Regex.Matches(bounds, @"-?\d+");
            if (nums.Count < 4) return null;
            return (int.Parse(nums[0].Value), int.Parse(nums[1].Value),
                    int.Parse(nums[2].Value), int.Parse(nums[3].Value));
        }

        /// <summary>
        /// Tên bài đang phát, đọc được ở cả 3 trạng thái player (full track_view,
        /// full premium_track_view, mini player). Rỗng nghĩa là chưa phát gì.
        /// </summary>
        private string GetNowPlayingTrack(string xml = "")
        {
            if (string.IsNullOrEmpty(xml)) xml = Xml();
            if (string.IsNullOrEmpty(xml)) return "";

            string title = GetTrackTitle(xml);
            if (!string.IsNullOrEmpty(title)) return title;

            // Player thu nhỏ: content-desc dạng "Mini Player <bài> <ca sĩ>".
            // Bỏ tiền tố để chuỗi gần với tên bài của player full nhất có thể.
            string mini = ReadAttr(xml, X_MINI_HANDLE, "content-desc");
            const string prefix = "Mini Player ";
            if (mini.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                mini = mini.Substring(prefix.Length).Trim();
            return mini;
        }

        /// <summary>
        /// So 2 snapshot tên bài xem có phải CÙNG một bài không.
        /// Không so bằng Equals được vì cùng một bài nhưng 2 trạng thái player cho 2 chuỗi
        /// khác nhau (đã dump thật): player full -> "STAY", còn mini player ->
        /// "STAY The Kid LAROI &amp; Justin Bieber" (kèm ca sĩ). Nếu player thu nhỏ giữa bài
        /// mà so bằng Equals thì ListenAsync sẽ tưởng đã sang bài mới và cắt bài giữa chừng.
        /// Vì tên bài luôn nằm ở ĐẦU chuỗi mini ("&lt;bài&gt; &lt;ca sĩ&gt;"), ta so bằng
        /// StartsWith chứ không phải Contains — chặt hơn, tránh 2 bài khác nhau bị coi là một
        /// chỉ vì tên bài này lọt vào giữa tên bài kia.
        /// </summary>
        private static bool IsSameTrack(string a, string b)
        {
            a = (a ?? "").Trim();
            b = (b ?? "").Trim();
            if (a.Length == 0 || b.Length == 0) return false;
            return a.StartsWith(b, StringComparison.OrdinalIgnoreCase)
                || b.StartsWith(a, StringComparison.OrdinalIgnoreCase);
        }
    }
}
