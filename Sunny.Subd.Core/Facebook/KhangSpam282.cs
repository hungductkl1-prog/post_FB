using AutoAndroid;
using Microsoft.Data.Sqlite;
using Sunny.Subdy.Common.API.Captchas;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using File = System.IO.File;

namespace Sunny.Subd.Core.Facebook
{
    /// <summary>
    /// Cơ chế thiết bị cho hành động "Kháng spam" (gỡ checkpoint 282) chạy qua Chrome.
    /// Tách khỏi FacebookFarming.cs để giữ using System.Drawing / Microsoft.Data.Sqlite gọn
    /// trong một file nhỏ (FacebookFarming.cs ~14k dòng, PublishAot, không import Drawing).
    ///
    /// Luồng đã verify end-to-end trên Chrome (device 520080d5ec5715b7, 2026-09-14):
    ///   inject cookie -> mở m.facebook.com trong Chrome -> giải captcha (cap.guru)
    ///   -> màn selfie -> drive picker upload ảnh -> nút "Gửi" enabled -> tap Gửi
    ///   -> màn xác nhận "đã gửi đơn kháng nghị".
    /// Marker trên màn 282 localise TIẾNG VIỆT (repo EN xpath KHÔNG khớp) nên mọi detect
    /// ở đây thử cả marker VI lẫn EN. Toạ độ picker PHỤ THUỘC resolution -> LUÔN re-dump
    /// bounds tươi trước mỗi tap, KHÔNG hard-code toạ độ chết.
    /// </summary>
    /// <summary>
    /// Kết quả luồng kháng nghị 282, để caller (HDKhangSpam) phân biệt 3 nhánh và ghi chú
    /// tài khoản đúng (user 2026-09-15 #2/#3). bool cũ không phân biệt được "cookie die"
    /// với "timeout/lỗi khác" -> cả hai đều là false -> không ghi chú chính xác được.
    /// </summary>
    public enum AppealOutcome
    {
        /// <summary>Up ảnh + gửi đơn kháng nghị THÀNH CÔNG (thấy marker xác nhận "đã gửi đơn kháng nghị").</summary>
        Success,
        /// <summary>Cookie CHẾT / bị Facebook vô hiệu — hiện màn đăng nhập, không vào được acc.</summary>
        CookieDie,
        /// <summary>Không hoàn tất vì lý do khác (timeout 360s, captcha sai nhiều, picker lỗi...).</summary>
        Failed,
    }

    internal static class KhangSpam282
    {
        private const string ChromePkg = "com.android.chrome";
        private const string ChromeMain = "com.android.chrome/com.google.android.apps.chrome.Main";
        // URL mở đầu luồng kháng 282. FB tự redirect tài khoản đang bị 282 sang màn checkpoint.
        private const string AppealUrl = "https://m.facebook.com/";
        // Đường dẫn cookie tương đối trong data dir của Chrome.
        private const string ProfileRel = "app_chrome/Default";
        private const string CookiesRel = "app_chrome/Default/Cookies";
        private const string CookiesJournalRel = "app_chrome/Default/Cookies-journal";
        // File trung gian trên shared storage (adb shell user ghi/đọc được, su cp qua lại).
        private const string SdcardCookies = "/sdcard/Cookies_khangspam282";

        // Các thư mục site-data của profile Chrome chứa session/localStorage/IndexedDB/cache.
        // XOÁ hết khi kháng nick KHÁC để session Facebook của nick trước không rò sang nick sau.
        // KHÔNG xoá "Preferences"/"Local State"/skeleton profile -> Chrome KHÔNG hiện First-Run/
        // consent/"didn't shut down correctly" (đã verify live 2026-09-14 device 520080d5ec5715b7).
        // KHÔNG xoá file "Cookies" ở đây -> giữ làm TEMPLATE schema để inject (dòng FB cũ bị
        // DELETE trong WriteFacebookCookies trước khi INSERT cookie nick mới).
        private static readonly string[] SiteDataDirs =
        {
            "Local Storage", "Session Storage", "IndexedDB", "Service Worker",
            "databases", "File System", "blob_storage", "WebStorage",
            "Application Cache", "Cache", "Code Cache", "GPUCache",
            "DawnCache", "DawnGraphiteCache", "DawnWebGPUCache",
        };

        // =====================================================================
        // 1. INJECT COOKIE VÀO CHROME (login bằng token/cookie đã lưu)
        // =====================================================================
        /// <summary>
        /// Ghi 4 cookie Facebook (c_user, xs, fr, datr) vào DB cookie của Chrome trên thiết bị.
        /// sqlite3-on-device HỎNG trên Android 9 (TLS underaligned) nên pull DB về host, sửa bằng
        /// Microsoft.Data.Sqlite, push lại rồi su cp đè + chown/chmod về chủ Chrome.
        /// </summary>
        /// <param name="cookieString">Chuỗi "c_user=…;xs=…;fr=…;datr=…" từ Account.Cookie.</param>
        public static bool InjectChromeCookies(ADBClient client, string cookieString, Action<string, int, string> report, string expectedUid = "")
        {
            if (string.IsNullOrEmpty(cookieString))
            {
                report("Tài khoản không có cookie để đăng nhập Chrome.", 3, null);
                return false;
            }

            // Parse cookie string -> dictionary (giữ thứ tự gặp, key case-sensitive như FB dùng).
            var cookies = ParseCookieString(cookieString);
            if (cookies.Count == 0)
            {
                report("Không parse được cookie nào từ chuỗi cookie tài khoản.", 3, null);
                return false;
            }

            // (User 2026-09-15 #2) MINH BẠCH cookie theo NICK đang chạy: log c_user (=uid) để user
            // đối chiếu đúng nick, và CẢNH BÁO nếu c_user KHÁC uid tài khoản (cookie cũ/test trong DB).
            string cuser = cookies.TryGetValue("c_user", out var cuVal) ? (cuVal ?? "").Trim() : "";
            if (!string.IsNullOrEmpty(cuser))
                report($"Đang login Chrome bằng cookie của nick c_user={cuser}.", 2, null);
            if (!string.IsNullOrEmpty(expectedUid) && !string.IsNullOrEmpty(cuser) &&
                !string.Equals(cuser, expectedUid.Trim(), StringComparison.Ordinal))
            {
                report($"CẢNH BÁO: cookie c_user={cuser} KHÁC uid nick đang chạy ({expectedUid.Trim()}) " +
                       "— cookie trong DB có thể là cookie cũ/test. Hãy cập nhật cookie MỚI cho nick này.", 3, null);
            }

            string serial = client.Device.Serial;
            string dataDir = $"/data/data/{ChromePkg}";
            string cookieFile = $"{dataDir}/{CookiesRel}";
            string journalFile = $"{dataDir}/{CookiesJournalRel}";
            string localTmp = Path.Combine(Path.GetTempPath(), $"chrome_cookies_{serial}_{Guid.NewGuid():N}.db");

            try
            {
                report("Đang dừng Chrome trước khi inject cookie...", 2, null);
                client.Shell($"am force-stop {ChromePkg}");
                client.Delay(2);

                // (User 2026-09-15 #1 — CHUYỂN XUỐNG CUỐI) KHÔNG `pm clear` ở ĐẦU luồng nữa.
                // Lý do: full-wipe ở đầu khiến Chrome về như mới -> First-Run "Welcome to Chrome"
                // MỖI LẦN chạy, phải bấm Accept/No-thanks mới vào được FB -> thêm điểm kẹt.
                // Vệ sinh giữa các nick giờ do 2 lớp: (a) DELETE FROM cookies trong
                // WriteFacebookCookies bên dưới (cookie nick trước biến mất khỏi DB), và
                // (b) `FullResetChrome` gọi ở CUỐI hành động kháng (HDKhangSpam) — xoá sạch
                // Chrome "như mới" sau khi đã kháng xong, để nick kế tiếp bắt đầu từ Chrome trắng.
                //
                // Relaunch vẫn cần: tạo lại profile + file Cookies (template schema) để pull về
                // host. Nếu data dir đã tồn tại từ lần chạy trước, Chrome chỉ mở lại profile cũ.
                report("Đang khởi động Chrome để lấy profile cookie...", 2, null);
                client.Shell($"am start -n {ChromeMain}");
                client.Delay(6);
                client.Shell($"am force-stop {ChromePkg}");
                client.Delay(2);

                // Đọc uid của Chrome (chủ sở hữu file cookie) để chown lại sau khi ghi.
                string chromeUid = ReadChromeUid(client);

                // Copy DB cookie ra shared storage (qua su), xoá journal để tránh ghi chồng.
                client.Shell($"su -c 'cp {cookieFile} {SdcardCookies}'");
                client.Shell($"su -c 'rm -f {journalFile}'");
                client.Shell($"su -c 'chmod 666 {SdcardCookies}'");
                client.Delay(1);

                // Pull về host (ADBClient KHÔNG có Pull -> dùng ProcessHelper chạy adb pull thô).
                if (File.Exists(localTmp)) File.Delete(localTmp);
                string pullOut = ProcessHelper.RunAdbCommand($"-s {serial} pull {SdcardCookies} \"{localTmp}\"", 30);
                if (!File.Exists(localTmp))
                {
                    report($"Không pull được DB cookie Chrome về host. adb: {pullOut}", 3, null);
                    return false;
                }

                // Sửa DB bằng sqlite HOST theo schema cookie MỚI của Chrome.
                int written = WriteFacebookCookies(localTmp, cookies);
                if (written == 0)
                {
                    report("Không ghi được cookie nào vào DB Chrome (schema không khớp?).", 3, null);
                    return false;
                }
                report($"Đã ghi {written} cookie Facebook vào DB Chrome, đẩy lại thiết bị...", 2, null);

                // Push lại rồi su cp đè file gốc, dọn journal, chown/chmod về chủ Chrome.
                client.Push(localTmp, SdcardCookies);
                client.Delay(1);
                client.Shell($"su -c 'cp {SdcardCookies} {cookieFile}'");
                client.Shell($"su -c 'rm -f {journalFile}'");
                if (!string.IsNullOrEmpty(chromeUid))
                {
                    client.Shell($"su -c 'chown {chromeUid}:{chromeUid} {cookieFile}'");
                }
                client.Shell($"su -c 'chmod 600 {cookieFile}'");
                // Dọn file trung gian.
                client.Shell($"rm -f {SdcardCookies}");
                client.Delay(1);
                return true;
            }
            catch (Exception ex)
            {
                report($"Lỗi inject cookie Chrome: {ex.Message}", 3, $"[KhangSpam282.InjectChromeCookies] {ex}");
                return false;
            }
            finally
            {
                try { if (File.Exists(localTmp)) File.Delete(localTmp); } catch { /* bỏ qua */ }
            }
        }

        private static string ReadChromeUid(ADBClient client)
        {
            try
            {
                string raw = client.Shell($"su -c 'stat -c %u /data/data/{ChromePkg}'");
                if (string.IsNullOrEmpty(raw)) return "";
                var m = Regex.Match(raw, @"\d+");
                return m.Success ? m.Value : "";
            }
            catch { return ""; }
        }

        /// <summary>
        /// XOÁ site-data của profile Chrome (Local/Session Storage, IndexedDB, Service Worker,
        /// databases, File System, blob_storage, các loại Cache) để session Facebook của NICK
        /// TRƯỚC không rò sang nick kế tiếp. GIỮ "Preferences"/"Local State"/skeleton để Chrome
        /// KHÔNG hiện First-Run/consent. KHÔNG xoá file "Cookies" (giữ làm template schema —
        /// mọi dòng cookie cũ bị DELETE trong WriteFacebookCookies trước khi INSERT nick mới).
        ///
        /// Lệnh: MỘT `su -c '...'` gộp, mỗi path có khoảng trắng bọc nháy đôi, biến $D expand
        /// trong subshell của su (single-quote chặn shell ngoài; path profile KHÔNG có khoảng
        /// trắng nên gán D=... không cần nháy). Đã verify live 2026-09-14 device 520080d5ec5715b7.
        /// Best-effort: thất bại KHÔNG abort inject (cookie cũ vẫn bị DELETE trong DB -> không rò).
        /// </summary>
        private static void WipeChromeSiteData(ADBClient client)
        {
            try
            {
                string profileDir = $"/data/data/{ChromePkg}/{ProfileRel}";
                string rmCmds = "";
                foreach (string dir in SiteDataDirs)
                {
                    rmCmds += $" rm -rf \"$D/{dir}\";";
                }
                // echo WIPE_RC=$? để biết lệnh cuối trả về gì (rm -rf path không tồn tại vẫn rc=0).
                string cmd = $"su -c 'D={profileDir};{rmCmds} echo WIPE_RC=$?;'";
                client.Shell(cmd);
            }
            catch (Exception ex)
            {
                // Không ném: wipe site-data là lớp phòng vệ phụ; lớp chính là DELETE cookie trong DB.
                client.LogHelper?.Log($"[KhangSpam282.WipeChromeSiteData] bỏ qua lỗi wipe: {ex.Message}");
            }
        }

        /// <summary>
        /// (User 2026-09-15 #1) XOÁ TOÀN BỘ Chrome "sạch sẽ như mới" — `pm clear` xoá cả data dir
        /// (app_chrome/Default: Cookies, Preferences, Local State, cache, session...) lẫn shared_prefs.
        /// Khác WipeChromeSiteData (chỉ xoá site-data, GIỮ skeleton để tránh First-Run), hàm này
        /// chủ đích đưa Chrome về trạng thái cài mới.
        ///
        /// THỜI ĐIỂM (user 2026-09-15, CHUYỂN XUỐNG CUỐI): gọi ở CUỐI hành động kháng trong
        /// HDKhangSpam — SAU khi up ảnh kháng nghị THÀNH CÔNG và delay 10s — chứ KHÔNG gọi ở đầu
        /// InjectChromeCookies nữa. Mục đích: xoá sạch session/cookie nick vừa kháng để nick KẾ TIẾP
        /// bắt đầu từ Chrome trắng, đồng thời KHÔNG tạo First-Run ở đầu mỗi luồng (First-Run giữa
        /// lúc đang drive dễ kẹt). public để HDKhangSpam (FacebookFarming.cs) gọi được.
        /// Đã verify live 2026-09-15 device 520080d5ec5715b7: pm clear trả "Success", app_chrome biến
        /// mất, relaunch tạo lại profile + file Cookies rỗng. Best-effort: thất bại KHÔNG ném.
        /// </summary>
        public static void FullResetChrome(ADBClient client)
        {
            try
            {
                client.Shell($"am force-stop {ChromePkg}");
                client.Delay(1);
                client.Shell($"pm clear {ChromePkg}");
            }
            catch (Exception ex)
            {
                client.LogHelper?.Log($"[KhangSpam282.FullResetChrome] bỏ qua lỗi pm clear: {ex.Message}");
            }
        }

        private static Dictionary<string, string> ParseCookieString(string cookieString)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string part in cookieString.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(part)) continue;
                int eq = part.IndexOf('=');
                if (eq <= 0) continue;
                string name = part.Substring(0, eq).Trim();
                string value = part.Substring(eq + 1).Trim();
                if (string.IsNullOrEmpty(name)) continue;
                result[name] = value;
            }
            return result;
        }

        /// <summary>
        /// INSERT OR REPLACE 4 cookie Facebook vào bảng cookies theo schema Chrome mới.
        /// Unique index = (host_key, top_frame_site_key, name, path) nên OR REPLACE cập nhật đúng dòng.
        /// </summary>
        private static int WriteFacebookCookies(string dbPath, Dictionary<string, string> cookies)
        {
            // Chrome epoch = 1601-01-01; Unix epoch = 1970. Lệch 11644473600 giây, tính bằng micro-giây.
            const long EpochDeltaSec = 11644473600L;
            long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long nowWinUs = (nowUnix + EpochDeltaSec) * 1_000_000L;
            long expiresWinUs = (nowUnix + 365 * 86400 + EpochDeltaSec) * 1_000_000L;

            // Chỉ giữ 4 cookie cần cho login; bỏ qua cookie lạ khác.
            string[] wanted = { "c_user", "xs", "fr", "datr" };
            int written = 0;

            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();

            // ĐỌC SCHEMA THẬT của bảng cookies TRƯỚC (PRAGMA chạy ngoài transaction cho an toàn).
            // Schema Chrome KHÁC NHAU theo phiên bản: Chrome 106 có 19 cột NOT NULL KHÔNG default,
            // gồm cả last_access_utc/has_expires/is_persistent mà INSERT tĩnh cũ THIẾU -> ném
            // "NOT NULL constraint failed". Vì vậy build INSERT ĐỘNG theo đúng cột có trong DB:
            // cột nào ta biết giá trị -> cấp; cột NOT NULL lạ -> cấp default theo kiểu (chống vỡ
            // khi gặp phiên bản Chrome khác trên máy farm khác).
            var colTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var notNullNoDefault = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var pragma = new SqliteCommand("PRAGMA table_info(cookies);", conn))
            using (var rd = pragma.ExecuteReader())
            {
                while (rd.Read())
                {
                    string cname = rd.GetString(1);
                    string ctype = rd.IsDBNull(2) ? "" : rd.GetString(2);
                    bool notNull = !rd.IsDBNull(3) && rd.GetInt32(3) != 0;
                    bool hasDefault = !rd.IsDBNull(4);
                    colTypes[cname] = ctype;
                    if (notNull && !hasDefault) notNullNoDefault.Add(cname);
                }
            }

            using var tx = conn.BeginTransaction();

            // XOÁ MỌI cookie cũ (đặc biệt c_user/xs/datr của NICK TRƯỚC còn sót trong DB) trước
            // khi INSERT cookie nick mới -> session cũ KHÔNG thể rò sang nick khác. Đây là lớp
            // phòng vệ CHÍNH (chắc chắn chạy trên DB host), bổ sung cho wipe site-data trên device.
            try
            {
                using var del = new SqliteCommand("DELETE FROM cookies;", conn, tx);
                del.ExecuteNonQuery();
            }
            catch { /* bảng rỗng/không tồn tại -> bỏ qua, INSERT OR REPLACE vẫn tạo dòng mới */ }

            foreach (string name in wanted)
            {
                if (!cookies.TryGetValue(name, out string value)) continue;

                // is_secure: c_user/xs/fr = 1, datr = 0.  is_httponly: c_user = 0, còn lại = 1.
                int isSecure = name == "datr" ? 0 : 1;
                int isHttpOnly = name == "c_user" ? 0 : 1;
                int sourceScheme = isSecure == 1 ? 2 : 1;

                // Giá trị chuẩn cho MỌI cột cookie Chrome ta biết (đối chiếu row FB thật trên máy:
                // value = plaintext, encrypted_value = blob rỗng, has_expires = is_persistent = 1,
                // last_access_utc = now).
                var known = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["creation_utc"] = nowWinUs,
                    ["host_key"] = ".facebook.com",
                    ["top_frame_site_key"] = "",
                    ["name"] = name,
                    ["value"] = value,
                    ["encrypted_value"] = new byte[0],
                    ["path"] = "/",
                    ["expires_utc"] = expiresWinUs,
                    ["is_secure"] = isSecure,
                    ["is_httponly"] = isHttpOnly,
                    ["last_access_utc"] = nowWinUs,
                    ["has_expires"] = 1,
                    ["is_persistent"] = 1,
                    ["priority"] = 1,
                    ["samesite"] = -1,
                    ["source_scheme"] = sourceScheme,
                    ["source_port"] = -1,
                    ["is_same_party"] = 0,
                    ["last_update_utc"] = nowWinUs,
                };

                // Ghép danh sách cột = cột CÓ THẬT trong DB ∩ (cột ta biết ∪ cột NOT NULL cần default).
                var cols = new List<string>();
                var paramNames = new List<string>();
                var paramVals = new List<object>();
                int idx = 0;
                foreach (var kv in colTypes)
                {
                    string col = kv.Key;
                    object v;
                    if (known.TryGetValue(col, out var kv2)) v = kv2;
                    else if (notNullNoDefault.Contains(col)) v = DefaultForColumnType(kv.Value);
                    else continue; // cột nullable không biết -> bỏ qua (DB tự NULL)
                    string p = "@p" + (idx++);
                    cols.Add(col); paramNames.Add(p); paramVals.Add(v);
                }

                string sql = $"INSERT OR REPLACE INTO cookies ({string.Join(",", cols)}) VALUES ({string.Join(",", paramNames)});";
                using var cmd = new SqliteCommand(sql, conn, tx);
                for (int k = 0; k < cols.Count; k++)
                    cmd.Parameters.AddWithValue(paramNames[k], paramVals[k] ?? DBNull.Value);
                cmd.ExecuteNonQuery();
                written++;
            }

            tx.Commit();
            return written;
        }

        /// <summary>Giá trị mặc định theo kiểu SQLite cho cột NOT NULL lạ (không nằm trong map known).</summary>
        private static object DefaultForColumnType(string sqliteType)
        {
            switch ((sqliteType ?? "").ToUpperInvariant())
            {
                case "BLOB": return new byte[0];
                case "TEXT":
                case "VARCHAR":
                case "CHAR":
                case "CLOB": return "";
                default: return 0; // INTEGER / NUMERIC / REAL
            }
        }

        // =====================================================================
        // 2. MỞ CHECKPOINT TRONG CHROME
        // =====================================================================
        public static void OpenFacebookInChrome(ADBClient client, string url)
        {
            // Device test KHÔNG có default browser -> phải launch Chrome tường minh.
            // KHÔNG dùng DeplinkFacebook (nó mở app Facebook .IntentUriHandler, không phải Chrome).
            client.Shell($"am start -a android.intent.action.VIEW -d \"{url}\" -n {ChromeMain}");
        }

        // =====================================================================
        // 3. LUỒNG KHÁNG NGHỊ: giải captcha (nếu có) -> drive picker -> Gửi
        // =====================================================================
        /// <summary>
        /// Chạy toàn bộ luồng trên màn 282 trong Chrome: nếu gặp captcha thì giải qua cap.guru,
        /// gặp màn selfie/upload thì drive picker chọn ảnh, đợi nút "Gửi" enabled rồi tap,
        /// xác nhận màn "đã gửi đơn kháng nghị". Trả về <see cref="AppealOutcome"/>:
        /// Success (thấy marker xác nhận) / CookieDie (màn đăng nhập -> acc không vào được) /
        /// Failed (timeout hoặc lỗi khác) — để caller ghi chú tài khoản đúng (user 2026-09-15 #2/#3).
        /// </summary>
        /// <param name="preferredTile">Tên file ảnh TRÊN DEVICE đã push (vd "aB3xYz9kLm.jpg").
        /// DocumentsUI hiển thị tile với text = tên file -> tap ĐÚNG tile này, tránh tap nhầm
        /// hamburger 'Show roots' (live 2026-09-15: TapFirstImageTile cũ khớp hamburger).</param>
        public static async Task<AppealOutcome> RunAppealFlow(ADBClient client, string captchaKey, Action<string, int, string> report, string preferredTile = "")
        {
            var sw = Stopwatch.StartNew();
            bool pickerDone = false;
            bool sendTapped = false;
            int loginStreak = 0; // đếm số dump LIÊN TIẾP thấy màn đăng nhập (tránh bail vì render thoáng qua)

            while (sw.Elapsed.TotalSeconds < 360)
            {
                string xml = client.GetXMLSource();
                if (string.IsNullOrEmpty(xml))
                {
                    client.Delay(2);
                    continue;
                }

                // (a) Đã tới màn xác nhận -> thành công. Marker THẬT (live 2026-09-15, EN):
                // "<Tên>, you submitted an appeal" + "Check back here for the result."
                if (Contains(xml, "đã gửi đơn kháng nghị", "quay lại đây để xem kết quả",
                                  "you submitted an appeal", "submitted an appeal",
                                  "check back here for the result",
                                  "sent your appeal", "we've received your appeal", "come back here to check"))
                {
                    report("Facebook đã nhận đơn kháng nghị 282.", 0, null);
                    return AppealOutcome.Success;
                }

                // (a2) Màn ĐĂNG NHẬP = cookie CHẾT (FB đã vô hiệu c_user/xs) -> BỎ QUA nick này ngay,
                // đừng đốt đủ 360s. (User 2026-09-15 #3.)
                // BẪY ĐÃ SỬA (live 2026-09-15): detector CŨ đòi literal "mobile number or email" nhưng
                // chuỗi đó KHÔNG BAO GIỜ có trong dump ATX (placeholder WebView không export ra text) ->
                // detector KHÔNG fire -> kẹt đủ 360s ("đang xử lý màn kháng nghị 282"). Marker THẬT có
                // trong dump: resource-id "m_login_email"/"m_login_password", "Create new account",
                // "Forgot password?", title "Facebook - log in or sign up". Dùng resource-id làm tín
                // hiệu CHÍNH (độc nhất cho trang login FB mobile, không lẫn trang checkpoint/kháng nghị).
                bool looksLikeLogin =
                    Contains(xml, "m_login_email", "m_login_password") ||
                    Contains(xml, "facebook - log in or sign up", "log in or sign up",
                                  "đăng nhập hoặc đăng ký") ||
                    (Contains(xml, "create new account", "tạo tài khoản mới", "create account") &&
                     Contains(xml, "forgot password", "quên mật khẩu"));
                if (looksLikeLogin)
                {
                    loginStreak++;
                    // Yêu cầu 2 dump LIÊN TIẾP (~4s) để chắc chắn đây là màn login ổn định,
                    // không phải khung login render thoáng qua giữa lúc chuyển trang.
                    if (loginStreak >= 2)
                    {
                        report("Cookie die / bị Facebook vô hiệu (hiện màn đăng nhập, không vào được acc). " +
                               "BỎ QUA nick này, chuyển nick khác. Hãy cập nhật cookie MỚI nếu muốn kháng lại.", 3, null);
                        return AppealOutcome.CookieDie;
                    }
                    client.Delay(2);
                    continue;
                }
                loginStreak = 0; // trang không phải login -> reset streak

                // (a3) Chrome First-Run sau khi `pm clear` đưa Chrome về như mới (XẢY RA MỖI LẦN KHÁNG).
                // First-Run là CHUỖI TRANG liên tiếp (verify LIVE 2026-09-16 trên 20 device):
                //   TRANG 1: "Welcome to Chrome" + nút "Accept & continue" (rid terms_accept)
                //   TRANG 2a (sign-in): "Add account to device" (signin_fre_continue_button) +
                //            "Use without an account" (signin_fre_dismiss_button)
                //   TRANG 2b (SYNC — verify LIVE 2026-09-16, 11 device KẸT đúng màn này):
                //            "Turn on sync?" + "No thanks" (negative_button) + "Add account"
                //            (positive_button) — biến thể này KHÔNG có 2 nút signin_fre_*.
                // GỐC RỄ KẸT: gate CŨ khớp marker chung ("fre_pager"/"welcome to chrome" — có ở MỌI
                // trang) nhưng CHỈ tap nút trang 1 -> sang trang 2a/2b gate fire mà tap TRƯỢT ->
                // vòng lặp tap hụt -> KẸT First-Run mãi mãi. Gate sync cũ nằm DƯỚI gate (a3) nên
                // là DEAD CODE: (a3) khớp trước qua fre_pager và NUỐT màn sync trước khi tới nó.
                // FIX: gate CHUNG mọi trang, tap TUẦN TỰ 1 -> 2a -> 2b, re-dump sau mỗi bước để
                // biết trang kế tiếp là biến thể nào (bỏ sign-in/sync để KHÔNG gắn Google account
                // vào Chrome farm). BẪY raw dump giữ nguyên: "&" escape thành "&amp;".
                if (Contains(xml, "terms_accept", "fre_pager", "signin_fre_dismiss_button",
                                  "signin_fre_continue_button", "welcome to chrome", "accept &amp; continue",
                                  "chào mừng bạn đến với chrome", "chấp nhận và tiếp tục",
                                  "use without an account", "dùng mà không cần tài khoản",
                                  "add account to device", "thêm tài khoản vào thiết bị",
                                  "turn on sync", "bật đồng bộ", "no thanks", "không, cảm ơn"))
                {
                    // TRANG 1: Accept & continue (đang ở trang 2 thì terms_accept không có -> bỏ qua).
                    if (Contains(xml, "terms_accept", "accept &amp; continue", "chấp nhận và tiếp tục"))
                    {
                        report("Chrome First-Run: bấm Accept & continue...", 2, null);
                        if (!TapByResourceId(client, xml, "com.android.chrome:id/terms_accept"))
                            TapByMarkers(client, xml, "Accept & continue", "Chấp nhận và tiếp tục");
                        client.Delay(3);
                        xml = client.GetXMLSource();
                    }
                    bool hitSignIn = false, hitSync = false;
                    // TRANG 2a (sign-in): dump lại vì trang 2 có thể chưa render ở dump trên.
                    if (!string.IsNullOrEmpty(xml) &&
                        Contains(xml, "signin_fre_dismiss_button", "signin_fre_continue_button",
                                 "use without an account", "add account to device",
                                 "dùng mà không cần tài khoản", "thêm tài khoản vào thiết bị"))
                    {
                        hitSignIn = true;
                        report("Chrome First-Run: bấm Use without an account (bỏ qua sign-in)...", 2, null);
                        if (!TapByResourceId(client, xml, "com.android.chrome:id/signin_fre_dismiss_button"))
                            TapByMarkers(client, xml, "Use without an account",
                                         "Dùng mà không cần tài khoản", "Sử dụng mà không có tài khoản");
                        client.Delay(3);
                        xml = client.GetXMLSource();
                    }
                    // TRANG 2b (sync): No thanks — biến thể này CHỈ có negative_button/positive_button.
                    if (!string.IsNullOrEmpty(xml) &&
                        Contains(xml, "turn on sync", "bật đồng bộ", "no thanks", "không, cảm ơn"))
                    {
                        hitSync = true;
                        report("Chrome First-Run (sync): bấm No thanks (không bật sync)...", 2, null);
                        if (!TapByResourceId(client, xml, "com.android.chrome:id/negative_button"))
                            TapByMarkers(client, xml, "No thanks", "Không, cảm ơn");
                        client.Delay(3);
                    }
                    // LƯỚI CUỐI cho prompt cùng họ nút "No thanks" (vd default-browser) mà 2 sub-gate
                    // trên không nhận ra. CHỈ chạy khi cả 2 sub-gate KHÔNG fire — nếu không sẽ tap
                    // THÊM một lần theo toạ độ dump CŨ lên màn đã chuyển (dễ tap nhầm trang FB).
                    if (!hitSignIn && !hitSync && !string.IsNullOrEmpty(xml) &&
                        Contains(xml, "no thanks", "không, cảm ơn"))
                    {
                        report("Chrome prompt: bấm No thanks...", 2, null);
                        TapByMarkers(client, xml, "No thanks", "Không, cảm ơn");
                        client.Delay(2);
                    }
                    // Chrome có thể ĐÁNH RƠI intent URL khi hiện First-Run trước -> mở lại FB để chắc
                    // chắn trang checkpoint/login được tải sau khi accept.
                    OpenFacebookInChrome(client, AppealUrl);
                    client.Delay(4);
                    continue;
                }

                // (a4) Dialog xin notification của Chrome trên trang checkpoint -> Block.
                // VERIFY LIVE 2026-09-16: node THẬT text="m.facebook.com wants to send you notifications"
                // + 2 Button "Block"/"Allow" (dump ATX). Thêm marker "wants to send" (bao trùm mọi host
                // vd "facebook.com wants to send...") + vi + cặp nút vi để gate fire cả khi localise.
                if (Contains(xml, "wants to send you notifications", "wants to send",
                                  "muốn gửi thông báo", "gửi thông báo cho bạn"))
                {
                    report("Chrome hỏi notification: bấm Block...", 2, null);
                    if (!TapByMarkers(client, xml, "Block", "Chặn", "Không cho phép"))
                        TapByResourceId(client, xml, "com.android.chrome:id/negative_button");
                    client.Delay(2);
                    continue;
                }

                // (a5) Màn vào checkpoint ("..., confirm you're human to use your account" /
                // "Xác nhận bạn là người thật" + nút Continue) -> bấm Continue để sang captcha/selfie.
                if (Contains(xml, "to use your account", "để sử dụng tài khoản") &&
                    Contains(xml, "confirm", "xác nhận"))
                {
                    TapByMarkers(client, xml, "Continue", "Tiếp tục");
                    client.Delay(4);
                    continue;
                }

                // (b) Màn captcha -> giải. KHÔNG chặn bằng captchaDone: cap.guru trả SAI là chuyện
                // thường (live: lần 1 sai, lần 2 đúng) -> vòng lặp tự giải lại khi màn captcha còn đó.
                if (Contains(xml, "nhập văn bản từ hình ảnh", "enter the text from the image",
                                  "enter the characters you see", "xác nhận bạn là người thật"))
                {
                    report("Đang giải captcha 282 qua cap.guru...", 2, null);
                    bool ok = await SolveCaptcha(client, captchaKey, report);
                    client.Delay(ok ? 4 : 2);
                    continue;
                }

                // (c) Màn selfie/upload -> drive picker chọn ảnh (chỉ khi chưa xong picker).
                if (!pickerDone && Contains(xml, "tải ảnh selfie", "tải hình ảnh lên hoặc chụp ảnh",
                                                 "upload a selfie", "upload image or take photo", "upload photo"))
                {
                    report("Đang mở picker và chọn ảnh kháng nghị...", 2, null);
                    if (DriveUploadPicker(client, report, preferredTile))
                    {
                        pickerDone = true;
                        client.Delay(2);
                    }
                    continue;
                }

                // (d) Ảnh đã attach -> nút "Gửi" enabled -> tap.
                if (pickerDone && !sendTapped && TryTapSend(client, xml, report))
                {
                    sendTapped = true;
                    client.Delay(6);
                    continue;
                }

                client.Delay(2);
            }

            report("Hết thời gian chờ hoàn tất đơn kháng nghị 282.", 3, null);
            return AppealOutcome.Failed;
        }

        // ---------------------------------------------------------------------
        // 3a. Giải captcha ảnh (port từ FacebookService.HandleCaptchaAsync, marker VI)
        // ---------------------------------------------------------------------
        public static async Task<bool> SolveCaptcha(ADBClient client, string key, Action<string, int, string> report)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                {
                    report("Chưa cấu hình key captcha (cap.guru).", 3, null);
                    return false;
                }

                string site = GuruCaptchaClient.Url;
                string xml = client.GetXMLSource();
                string base64Image = CropCaptchaImage(client, xml);
                if (string.IsNullOrEmpty(base64Image))
                {
                    report("Không xác định được vùng ảnh captcha 282.", 3, null);
                    return false;
                }

                report("Đang gửi ảnh captcha lên cap.guru...", 2, null);
                string id = await CaptchaService.GetIdImageCaptcha(site, key, base64Image);
                if (string.IsNullOrEmpty(id) || id.Contains("error"))
                {
                    report($"Tạo id captcha thất bại: {id}", 3, null);
                    return false;
                }

                int timeoutSec = 180;
                var sw = Stopwatch.StartNew();
                string token = string.Empty;
                while (sw.ElapsedMilliseconds < timeoutSec * 1000)
                {
                    string result = await CaptchaService.GetTokenCaptchaV2(site, key, id);
                    // cap.guru đôi khi trả chuỗi KHÔNG phải mã captcha (trạng thái/lỗi lẫn vào) —
                    // nhập chuỗi đó vào ô captcha là đốt một lần thử sai. Chỉ chấp nhận token
                    // đúng hình dạng mã 282, còn lại coi như chưa có kết quả và poll tiếp.
                    if (!string.IsNullOrEmpty(result) && !result.Contains("error") && LooksLikeCaptchaToken(result))
                    {
                        token = result;
                        break;
                    }
                    report($"Đợi kết quả captcha ({sw.Elapsed.TotalSeconds:F0}/{timeoutSec}s)...", 2, null);
                    client.Delay(3);
                }

                if (string.IsNullOrEmpty(token))
                {
                    report("Hết thời gian chờ kết quả captcha.", 3, null);
                    return false;
                }

                report($"Đang nhập captcha: {token}", 2, null);
                // Focus TRỰC TIẾP EditText của ô captcha (node EditText nằm dưới thanh Chrome,
                // y0>300 trên màn 1440x2560) rồi mới input — xpath EditText KHÔNG điều kiện sẽ
                // focus nhầm ô URL của Chrome (SendTextADB focus node khớp đầu tiên).
                if (!FocusCaptchaEditText(client, xml))
                {
                    client.SendTextADB("//*[@class='android.widget.EditText']", token);
                }
                else
                {
                    client.Shell("input", "text", token);
                }
                client.Delay(2);
                // Nút tiếp tục trên màn 282 localise "Tiếp tục"; thử cả content-desc lẫn text, lẫn EN "Continue".
                if (!client.ElementWithAttributes("//*[@content-desc=\"Tiếp tục\" or @text=\"Tiếp tục\"]", 5))
                {
                    client.ElementWithAttributes("//*[@content-desc=\"Continue\" or @text=\"Continue\"]", 5);
                }
                client.Delay(5);
                return true;
            }
            catch (Exception ex)
            {
                report($"Lỗi giải captcha 282: {ex.Message}", 3, $"[KhangSpam282.SolveCaptcha] {ex}");
                return false;
            }
        }

        /// <summary>
        /// Mã captcha 282 quan sát được trên màn thật là chuỗi alphanumeric NGẮN
        /// (vd "153308", "498407", "07078"). cap.guru thỉnh thoảng trả về chuỗi KHÔNG phải
        /// mã (trạng thái xử lý, thông báo lỗi, HTML...) — nhập những chuỗi đó vào ô captcha
        /// chắc chắn sai và đốt một lần thử. Hàm này lọc hình dạng token trước khi nhập.
        /// </summary>
        private static bool LooksLikeCaptchaToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            string t = token.Trim();
            if (t.Length < 4 || t.Length > 12) return false;
            foreach (char c in t)
            {
                if (!char.IsLetterOrDigit(c)) return false;
            }
            return true;
        }

        /// <summary>
        /// Tap vào EditText của ô nhập captcha (node EditText có y0 > 300 — dưới thanh Chrome).
        /// Trả về false nếu không tìm thấy (caller fallback sang SendTextADB).
        /// </summary>
        private static bool FocusCaptchaEditText(ADBClient client, string xml)
        {
            if (string.IsNullOrEmpty(xml)) return false;
            foreach (Match m in Regex.Matches(xml, @"<node\b[^>]*?/>|<node\b[^>]*?>"))
            {
                string node = m.Value;
                if (!node.Contains("android.widget.EditText")) continue;
                var b = Regex.Match(node, @"\[(\d+),(\d+)\]\[(\d+),(\d+)\]");
                if (!b.Success) continue;
                int y0 = int.Parse(b.Groups[2].Value);
                int y1 = int.Parse(b.Groups[4].Value);
                int x0 = int.Parse(b.Groups[1].Value);
                int x1 = int.Parse(b.Groups[3].Value);
                if (y0 <= 300) continue; // ô URL / thanh tìm kiếm của Chrome
                client.Click((x0 + x1) / 2, (y0 + y1) / 2);
                return true;
            }
            return false;
        }

        private static string CropCaptchaImage(ADBClient client, string xml)
        {
            Bitmap screen = null;
            Bitmap cropped = null;
            try
            {
                screen = client.Screenshot();
                if (screen == null || screen.Width <= 0 || screen.Height <= 0) return null;

                Rectangle? rect = TryGetCaptchaBoundsFromXml(client, xml);
                if (rect.HasValue) rect = ScaleRectToScreen(rect.Value, screen.Width, screen.Height);

                // Fallback hình học (live 2026-09-15): node class android.widget.Image rộng gần
                // hết màn (>=900/1440) nằm dải giữa — chính là ảnh captcha trong WebView.
                if (!rect.HasValue) rect = TryGetBigImageBounds(xml);
                if (rect.HasValue) rect = ScaleRectToScreen(rect.Value, screen.Width, screen.Height);

                // Fallback: dải giữa màn (vùng ảnh captcha thường nằm ~1/6 -> 1/4 chiều cao).
                Rectangle crop = rect ?? new Rectangle(0, screen.Height / 6, screen.Width, screen.Height / 4);
                crop.Intersect(new Rectangle(0, 0, screen.Width, screen.Height));
                if (crop.Width <= 1 || crop.Height <= 1) return null;

                cropped = new Bitmap(crop.Width, crop.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(cropped))
                {
                    g.DrawImage(screen, new Rectangle(0, 0, crop.Width, crop.Height), crop, GraphicsUnit.Pixel);
                }
                using var ms = new MemoryStream();
                cropped.Save(ms, ImageFormat.Png);
                return Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                client?.LogHelper?.Log($"[KhangSpam282.CropCaptchaImage] {ex.Message}");
                return null;
            }
            finally
            {
                cropped?.Dispose();
                screen?.Dispose();
            }
        }

        private static Rectangle? TryGetCaptchaBoundsFromXml(ADBClient client, string xml)
        {
            if (string.IsNullOrEmpty(xml)) return null;
            // Marker màn captcha 282: VI "Nhập văn bản từ hình ảnh." / EN THẬT (live 2026-09-15)
            // "Enter the text from the image.". Node ảnh captcha class='android.widget.Image'
            // (KHÔNG phải ImageView — ImageView chỉ là icon thanh Chrome).
            string[] candidates =
            {
                "//*[contains(@text,'Nhập văn bản')]/following::*[@class='android.widget.Image'][1]",
                "//*[contains(@content-desc,'Nhập văn bản')]/following::*[@class='android.widget.Image'][1]",
                "//*[contains(@text,'Enter the text from the image')]/following::*[@class='android.widget.Image'][1]",
                "//*[contains(@content-desc,'Enter the text from the image')]/following::*[@class='android.widget.Image'][1]",
                "//*[contains(@text,'Nhập văn bản')]/following::*[@class='android.widget.ImageView'][1]",
                "//*[contains(@text,'Enter the characters you see')]/following::*[@class='android.widget.ImageView'][1]",
                "//*[@class='android.widget.ImageView' and (contains(@resource-id,'captcha') or contains(@content-desc,'captcha'))]",
            };
            foreach (string xpath in candidates)
            {
                try
                {
                    var bounds = client.FindBounds(xml, xpath, 0);
                    if (bounds != null && bounds.Count > 0)
                    {
                        var rect = ParseBounds(bounds[0]);
                        if (rect.HasValue) return rect;
                    }
                }
                catch { /* thử candidate kế tiếp */ }
            }
            return null;
        }

        /// <summary>
        /// Quét node class android.widget.Image có chiều rộng >=900 và tâm nằm dải giữa màn
        /// (300..1700 trên 1440x2560) — chữ ký hình học của ảnh captcha 282 trong WebView.
        /// </summary>
        private static Rectangle? TryGetBigImageBounds(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return null;
            foreach (Match m in Regex.Matches(xml, @"<node\b[^>]*?/>|<node\b[^>]*?>"))
            {
                string node = m.Value;
                if (!node.Contains("android.widget.Image\"")) continue;
                var b = Regex.Match(node, @"\[(\d+),(\d+)\]\[(\d+),(\d+)\]");
                if (!b.Success) continue;
                int x0 = int.Parse(b.Groups[1].Value);
                int y0 = int.Parse(b.Groups[2].Value);
                int x1 = int.Parse(b.Groups[3].Value);
                int y1 = int.Parse(b.Groups[4].Value);
                int cy = (y0 + y1) / 2;
                if (x1 - x0 >= 900 && cy > 300 && cy < 1700)
                    return new Rectangle(x0, y0, x1 - x0, y1 - y0);
            }
            return null;
        }

        private static Rectangle ScaleRectToScreen(Rectangle rect, int screenWidth, int screenHeight)
        {
            if (rect.Right <= screenWidth && rect.Bottom <= screenHeight) return rect;
            int deviceWidth = Math.Max(rect.Right, screenWidth);
            int deviceHeight = Math.Max(rect.Bottom, screenHeight);
            if (deviceWidth <= 0 || deviceHeight <= 0) return rect;
            double sx = (double)screenWidth / deviceWidth;
            double sy = (double)screenHeight / deviceHeight;
            return new Rectangle((int)(rect.X * sx), (int)(rect.Y * sy), (int)(rect.Width * sx), (int)(rect.Height * sy));
        }

        private static Rectangle? ParseBounds(string bounds)
        {
            if (string.IsNullOrEmpty(bounds)) return null;
            var match = Regex.Match(bounds, @"\[(\d+),(\d+)\]\[(\d+),(\d+)\]");
            if (!match.Success) return null;
            int x1 = int.Parse(match.Groups[1].Value);
            int y1 = int.Parse(match.Groups[2].Value);
            int x2 = int.Parse(match.Groups[3].Value);
            int y2 = int.Parse(match.Groups[4].Value);
            return new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }

        // ---------------------------------------------------------------------
        // 3b. Drive picker upload ảnh của Chrome (marker-driven, re-dump mỗi tap)
        // ---------------------------------------------------------------------
        /// <summary>
        /// Chuỗi: tap nút upload -> dialog quyền Chrome "Allow" -> picker "Browse" ->
        /// DocumentsUI root 'Images' (-> folder 'pictures') -> tile file (text = tên file đã push).
        /// Mỗi bước re-dump XML tươi (layout drift theo resolution), tap theo bounds tìm được.
        /// Trả về true khi nút "Gửi"/"Submit" chuyển enabled (ảnh đã attach).
        ///
        /// BÀI HỌC LIVE 2026-09-15 (appeal282.py thành công): (i) tile được nhận diện bằng
        /// TEXT = TÊN FILE (preferredTile), KHÔNG phải "ImageView clickable đầu tiên" — node đó
        /// là hamburger 'Show roots' [0,84][196,280] -> tap nhầm mở/đóng drawer lặp vô hạn;
        /// (ii) tap root 'Images' TRƯỚC, chỉ mở 'Show roots' khi chưa thấy 'Images';
        /// (iii) sau khi tap tile phải POLL ~8-30s đến khi Submit enabled (dump ngay sau tap
        /// vẫn thấy enabled=false -> kết luận sai "ảnh chưa attach").
        /// </summary>
        private static bool DriveUploadPicker(ADBClient client, Action<string, int, string> report, string preferredTile = "")
        {
            var sw = Stopwatch.StartNew();
            bool tappedUpload = false;
            bool tileTapped = false;

            while (sw.Elapsed.TotalSeconds < 120)
            {
                string xml = client.GetXMLSource();
                if (string.IsNullOrEmpty(xml)) { client.Delay(2); continue; }

                // Ảnh đã attach? -> nút Gửi enabled. (Poll tự nhiên: mỗi vòng re-dump.)
                if (IsSendEnabled(xml)) return true;

                // (1) Nút upload trên màn selfie.
                if (!tappedUpload && TapByMarkers(client, xml,
                        "Tải hình ảnh lên hoặc chụp ảnh", "Upload image or take photo", "Upload photo", "Tải ảnh lên"))
                {
                    tappedUpload = true;
                    client.Delay(3);
                    continue;
                }

                // (2) Dialog quyền runtime của Chrome "Allow Chrome to access photos…" -> Allow.
                if (TapPermissionAllow(client, xml)) { client.Delay(3); continue; }

                // (3) Chrome photo picker "Select an image" -> tab Browse.
                if (TapByMarkers(client, xml, "Browse", "Duyệt", "BROWSE")) { client.Delay(2); continue; }

                // (T) Tile ảnh = node có text/content-desc ĐÚNG bằng tên file đã push. Kiểm tra
                //     TRƯỚC điều hướng: root 'Images' (MediaStore) thường hiện file mới push ngay,
                //     không cần chui vào folder 'pictures'. Tap 1 LẦN rồi poll Submit (bài học iii).
                if (!tileTapped && !string.IsNullOrEmpty(preferredTile) &&
                    Contains(xml, preferredTile) &&
                    TapByMarkers(client, xml, preferredTile))
                {
                    tileTapped = true;
                    client.Delay(3);
                    continue;
                }

                // (5) Root 'Images' (tap TRƯỚC hamburger — bài học ii). Sau khi đã tap tile thì
                //     BỎ điều hướng: chỉ poll Submit (dump giai đoạn chuyển tiếp có thể còn sót
                //     DocumentsUI -> re-tap 'Images' sẽ đi lạc khỏi Chrome).
                if (!tileTapped && TapByMarkers(client, xml, "Images", "Hình ảnh", "Ảnh")) { client.Delay(2); continue; }

                // (4) Chỉ mở drawer 'Show roots' khi chưa thấy root 'Images'.
                if (!tileTapped && TapByContentDesc(client, xml, "Show roots", "Hiển thị gốc")) { client.Delay(2); continue; }

                // (6) Folder 'pictures' (nơi UploadMediaFiles đẩy ảnh vào).
                if (!tileTapped && TapByMarkers(client, xml, "pictures", "Pictures")) { client.Delay(2); continue; }

                // (7) Fallback cuối: tile ảnh đầu tiên NẰM DƯỚI thanh công cụ (y0>300), tránh hamburger.
                if (!tileTapped && TapFirstImageTile(client, xml)) { tileTapped = true; client.Delay(3); continue; }

                client.Delay(2);
            }
            report("Không drive được picker upload ảnh 282 (timeout).", 3, null);
            return false;
        }

        private static bool IsSendEnabled(string xml)
        {
            return HasEnabledSendButton(xml);
        }

        private static bool HasEnabledSendButton(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return false;
            // Hierarchy ATX/u2 dùng thẻ <node ... class="android.widget.Button" text="Gửi"
            // enabled="true" .../> (KHÔNG phải <Button> của Appium). Quét mọi <node> enabled
            // và so text/content-desc ĐÚNG BẰNG "Gửi"/"Submit" (trim, hạ chữ) — tránh khớp câu
            // dài có chứa "gửi" (vd "Đã gửi đơn kháng nghị"). Khớp logic find() của appeal282.py.
            foreach (Match m in Regex.Matches(xml, @"<node\b[^>]*?>"))
            {
                string node = m.Value;
                if (!node.Contains("enabled=\"true\"")) continue;
                string text = AttrOf(node, "text");
                string desc = AttrOf(node, "content-desc");
                if (IsSendLabel(text) || IsSendLabel(desc)) return true;
            }
            return false;
        }

        private static bool IsSendLabel(string label)
        {
            if (string.IsNullOrEmpty(label)) return false;
            string t = label.Trim().ToLowerInvariant();
            return t == "gửi" || t == "submit";
        }

        /// <summary>Đọc giá trị một attribute từ chuỗi thẻ <node ...> (rỗng nếu không có).</summary>
        private static string AttrOf(string node, string attr)
        {
            var m = Regex.Match(node, @"\b" + Regex.Escape(attr) + @"=""([^""]*)""");
            return m.Success ? m.Groups[1].Value : "";
        }

        private static bool TryTapSend(ADBClient client, string xml, Action<string, int, string> report)
        {
            string[] xpaths =
            {
                "//*[@enabled='true' and (@content-desc='Gửi' or @text='Gửi')]",
                "//*[@enabled='true' and (@content-desc='Submit' or @text='Submit')]",
                "//*[@content-desc='Gửi' or @text='Gửi']",
                "//*[@content-desc='Submit' or @text='Submit']",
            };
            foreach (string xpath in xpaths)
            {
                try
                {
                    var bounds = client.FindBounds(xml, xpath, 0);
                    if (bounds != null && bounds.Count > 0)
                    {
                        var p = new RectangleArea(bounds[0]).GetCenterPoint();
                        if (p.X > 0 && p.Y > 0)
                        {
                            report("Đang bấm Gửi đơn kháng nghị...", 2, null);
                            client.Click(p.X, p.Y);
                            return true;
                        }
                    }
                }
                catch { /* thử xpath kế tiếp */ }
            }
            return false;
        }

        // -- Các helper tap theo marker (re-dump đã có xml; trả về true nếu tap được) --

        private static bool TapByMarkers(ADBClient client, string xml, params string[] markers)
        {
            foreach (string marker in markers)
            {
                string escaped = marker.Replace("'", "\\'");
                string[] xpaths =
                {
                    $"//*[@content-desc='{escaped}']",
                    $"//*[@text='{escaped}']",
                    $"//*[contains(@content-desc,'{escaped}')]",
                    $"//*[contains(@text,'{escaped}')]",
                };
                if (TapFirstBound(client, xml, xpaths)) return true;
            }
            return false;
        }

        /// <summary>
        /// Tap node theo resource-id CHÍNH XÁC (ổn định hơn text khi text bị escape/localise —
        /// vd nút First-Run "Accept &amp; continue" có resource-id com.android.chrome:id/terms_accept).
        /// </summary>
        private static bool TapByResourceId(ADBClient client, string xml, string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId)) return false;
            string escaped = resourceId.Replace("'", "\\'");
            string[] xpaths =
            {
                $"//*[@resource-id='{escaped}']",
                $"//*[@clickable='true' and @resource-id='{escaped}']",
            };
            return TapFirstBound(client, xml, xpaths);
        }

        private static bool TapByContentDesc(ADBClient client, string xml, params string[] descs)
        {
            foreach (string d in descs)
            {
                string escaped = d.Replace("'", "\\'");
                string[] xpaths = { $"//*[@content-desc='{escaped}']", $"//*[contains(@content-desc,'{escaped}')]" };
                if (TapFirstBound(client, xml, xpaths)) return true;
            }
            return false;
        }

        private static bool TapPermissionAllow(ADBClient client, string xml)
        {
            // Dialog quyền Chrome: "Allow" / "Cho phép". Tránh "Allow all the time" bằng cách ưu tiên
            // node có content-desc/text ĐÚNG "Allow"/"CHO PHÉP" và clickable.
            string[] xpaths =
            {
                "//*[@clickable='true' and (@content-desc='Allow' or @text='Allow')]",
                "//*[@clickable='true' and (@content-desc='Cho phép' or @text='Cho phép')]",
                "//*[@content-desc='Allow' or @text='Allow']",
                "//*[@content-desc='Cho phép' or @text='Cho phép']",
            };
            return TapFirstBound(client, xml, xpaths);
        }

        private static bool TapFirstImageTile(ADBClient client, string xml)
        {
            // Fallback khi KHÔNG có preferredTile: chọn tile ảnh trong lưới DocumentsUI.
            // BẮT BUỘC y0 > 300 để LOẠI hamburger 'Show roots' [0,84][196,280] và thanh công cụ
            // (live 2026-09-15: xpath "ImageView clickable đầu tiên" khớp hamburger -> kẹt drawer).
            // Ưu tiên node có text/content-desc chứa phần mở rộng ảnh (.jpg/.png) rồi mới tới
            // ImageView thường nằm trong vùng lưới.
            Rectangle? best = null;
            bool bestHasExt = false;
            foreach (Match m in Regex.Matches(xml, @"<node\b[^>]*?>"))
            {
                string node = m.Value;
                string cls = AttrOf(node, "class");
                bool isImage = cls.Contains("ImageView") || cls.Contains("Image\"") || cls.EndsWith("Image");
                string text = AttrOf(node, "text");
                string desc = AttrOf(node, "content-desc");
                bool hasExt = HasImageExtension(text) || HasImageExtension(desc);
                if (!isImage && !hasExt) continue;

                var b = Regex.Match(node, @"\[(\d+),(\d+)\]\[(\d+),(\d+)\]");
                if (!b.Success) continue;
                int x0 = int.Parse(b.Groups[1].Value);
                int y0 = int.Parse(b.Groups[2].Value);
                int x1 = int.Parse(b.Groups[3].Value);
                int y1 = int.Parse(b.Groups[4].Value);
                if (y0 <= 300) continue;          // thanh công cụ / hamburger
                if (x1 - x0 < 40 || y1 - y0 < 40) continue; // icon quá nhỏ

                var rect = new Rectangle(x0, y0, x1 - x0, y1 - y0);
                if (hasExt) { best = rect; bestHasExt = true; break; }
                if (!bestHasExt && best == null) best = rect;
            }
            if (best.HasValue)
            {
                int cx = best.Value.X + best.Value.Width / 2;
                int cy = best.Value.Y + best.Value.Height / 2;
                if (cx > 0 && cy > 0) { client.Click(cx, cy); return true; }
            }
            return false;
        }

        private static bool HasImageExtension(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            string t = s.ToLowerInvariant();
            return t.Contains(".jpg") || t.Contains(".jpeg") || t.Contains(".png");
        }

        private static bool TapFirstBound(ADBClient client, string xml, string[] xpaths)
        {
            foreach (string xpath in xpaths)
            {
                try
                {
                    var bounds = client.FindBounds(xml, xpath, 0);
                    if (bounds != null && bounds.Count > 0)
                    {
                        var p = new RectangleArea(bounds[0]).GetCenterPoint();
                        if (p.X > 0 && p.Y > 0)
                        {
                            client.Click(p.X, p.Y);
                            return true;
                        }
                    }
                }
                catch { /* thử xpath kế tiếp */ }
            }
            return false;
        }

        // ---------------------------------------------------------------------
        // util
        // ---------------------------------------------------------------------
        private static bool Contains(string xml, params string[] keywords)
        {
            if (string.IsNullOrEmpty(xml)) return false;
            string lower = xml.ToLowerInvariant();
            foreach (string k in keywords)
            {
                if (!string.IsNullOrEmpty(k) && lower.Contains(k.ToLowerInvariant())) return true;
            }
            return false;
        }
    }
}
