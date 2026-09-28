using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace AutoAndroid
{
    public class MaxChangeService
    {
        ADBClient service;
        private readonly string path_MaxChange = Path.Combine(AppContext.BaseDirectory, "App", "LamToolChanger.apk");
        private readonly string path_DeviceInfoHW = Path.Combine(AppContext.BaseDirectory, "App", "DeviceInfoHW.apk");
        public static string package_MaxChange = "com.lamtool.changer";
        private readonly string package_Deviceinfohw = "ru.andr7e.deviceinfohw";
        // Nhịp poll khi chờ fingerprint/Device.xml xuất hiện (trước đây 2000ms).
        private const int PollIntervalMs = 500;

        // ── NGƯỠNG RÒ RỈ TaskRecord (build v11.5) ───────────────────────────────
        // Máy KHỎE: 6–9 TaskRecord (đã đo trên cả 8 SM-J730G lúc idle). Khi system_server
        // cạn SurfaceControl, SystemUI crash-loop ("InputChannel is not initialized" +
        // "crashed too many times: killing!") và AMS liên tục resumeHomeStackTask -> đẻ
        // hàng trăm→hàng nghìn TaskRecord launcher RỖNG (sz=0), mỗi cái giữ một
        // WindowContainer/SurfaceControl. Đã đo LIVE: 1460 task lúc đen, và leak TỰ TĂNG
        // (1223->1232 trong 6s) NGAY CẢ KHI TOOL ĐÃ TẮT -> vòng lặp tự khuếch đại.
        // 80 = trên hẳn recents stack bình thường (thường <=50) nhưng thấp hơn nhiều lần
        // ngưỡng chết, để BẮT SỚM và reset framework TRƯỚC khi vòng lặp kịp tuyết lở.
        private const int TaskLeakThreshold = 80;
        public MaxChangeService(ADBClient service)
        {
            this.service = service;
        }
        private void Close()
        {
            service.StopApp(package_MaxChange);

        }

        // Đóng app changer + về Home ngay sau khi đổi thiết bị xong.
        //
        // Vì sao cần: GenerateNewDevice mở com.lamtool.changer lên FOREGROUND
        // (AppStart) để phát broadcast CHANGE. Trên máy yếu (SM-J730G ~1.5GB,
        // LSPosed hook lại mọi process khi đổi identity), process changer bị hệ
        // thống kill ngay sau đó NHƯNG ActivityRecord của nó vẫn là mFocusedApp
        // với mCurrentFocus=null -> KHÔNG cửa sổ nào vẽ -> MÀN HÌNH ĐEN, treo
        // cho tới khi app kế tiếp (proxy/Facebook) dựng xong window (1-2 phút).
        //
        // Force-stop sạch (xóa task record) + KEYCODE_HOME (đưa launcher lên,
        // set mCurrentFocus hợp lệ) xóa "focus ma" NGAY, nên bước kế tiếp hiển
        // thị tức thì. KHÔNG ảnh hưởng identity: fingerprint đã lưu trong
        // shared_prefs/Device.xml (force-stop không xóa data), và module LSPosed
        // chạy trong zygote/process bị hook chứ không phải process UI của app.
        private void CleanupChangerForeground()
        {
            // Dùng RunAdbCommand (timeout cứng) thay vì service.Shell (vòng retry +
            // Connect lại) để bước dọn dẹp KHÔNG bao giờ tự treo — nếu adb chập chờn
            // thì nó fail nhanh chứ không treo im nhiều phút, đúng triết lý của v7.
            string serial = service.Device?.Serial;
            if (string.IsNullOrEmpty(serial)) return;
            try
            {
                // HOME TRƯỚC, force-stop SAU (đảo lại thứ tự cũ — build v11.4).
                //
                // Thứ tự cũ (force-stop RỒI HOME) đi thẳng vào đường dẫn làm cạn surface,
                // đã xác minh từ Error log 2026-09-05 14:49 trên 52006f60f4be7475:
                //   am force-stop -> AMS.forceStopPackageLocked -> resumeHomeStackTask
                //     -> startHomeActivityLocked -> ActivityStarter.setTaskFromReuseOrCreateNewTask
                //     -> TaskRecord.createWindowContainer -> WindowContainer.onParentSet
                //     -> SurfaceControl.nativeCreate
                //     -> android.view.Surface$OutOfResourcesException
                // Force-stop XÓA task record của app changer nên AMS buộc phải DỰNG TASK
                // HOME MỚI ngay trong lúc system_server đang kiệt surface sau bước đổi
                // thiết bị nặng (LSPosed re-hook mọi process). Gửi HOME TRƯỚC thì task Home
                // được dựng/resume khi resource còn, và force-stop sau đó không phải tạo
                // SurfaceControl mới -> tránh đúng exception trên.
                ProcessHelper.RunAdbCommand($"-s {serial} shell input keyevent KEYCODE_HOME", 8);
                ProcessHelper.RunAdbCommand($"-s {serial} shell am force-stop {package_MaxChange}", 8);
                DeviceChangeLog.Write(serial, "Đã về Home + đóng app changer (chống màn hình đen 'focus ma').");
            }
            catch { }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // PHỤC HỒI MÀN HÌNH ĐEN DO SurfaceFlinger CHẾT GIỮA CHỪNG (build v10).
        //
        // Đây mới là NGUYÊN NHÂN GỐC của màn hình đen còn sót lại sau v8/v9, đã
        // xác minh LIVE trên 52006f60f4be7475 lúc 21:22–21:35 (log dumpsys/logcat):
        //
        //   Bước đổi thiết bị rất nặng — module LSPosed re-hook MỌI process khi swap
        //   identity trên máy yếu (~1.5GB RAM). Thỉnh thoảng SurfaceFlinger bị kill
        //   và TỰ RESTART MỘT MÌNH, còn system_server thì KHÔNG. system_server cũ giữ
        //   binder proxy tới SurfaceFlinger đã chết:
        //       BpSurfaceComposerClient: Failed to transact (-32)        (EPIPE)
        //       WindowManager: android.view.Surface$OutOfResourcesException
        //       com.android.systemui ... InputChannel is not initialized (crash-loop)
        //   -> KHÔNG app nào (kể cả launcher) dựng được window
        //   -> mCurrentFocus=null, SurfaceFlinger chỉ còn 1 layer "BootAnimation"
        //   -> MÀN HÌNH ĐEN, KHÔNG tự khỏi (hàng phút).
        //
        //   CleanupChangerForeground() KHÔNG cứu được: chính lệnh HOME cũng fail
        //   (OutOfResourcesException khi tạo surface). `stop; start` cũng KHÔNG được:
        //   nó chỉ restart SurfaceFlinger + SystemUI, KHÔNG restart system_server nên
        //   binder chết vẫn còn (đã thử live, vẫn transact -32).
        //
        //   Cách phục hồi DUY NHẤT đã xác minh: KILL system_server -> zygote dựng lại
        //   TOÀN BỘ Java framework ĐỒNG BỘ với SurfaceFlinger mới -> binder nối lại ->
        //   window vẽ được (mCurrentFocus=launcher3, 30 layer, logcat sạch -32).
        //   Đây là soft-restart framework (KHÔNG reboot kernel), giữ nguyên data,
        //   ~30–60s. An toàn khi đặt ở Change() vì bước này chạy TRƯỚC khi nối proxy
        //   (tun0/com.vat.vpn chưa có gì để mà rớt).
        // ─────────────────────────────────────────────────────────────────────────

        // mCurrentFocus hợp lệ = có window thật đang được focus (launcher/changer/...).
        // null = chưa/không window nào vẽ được -> đen. Dùng RunAdbCommand (timeout cứng)
        // để probe này KHÔNG bao giờ tự treo.
        private bool IsDisplayFocusOk(string serial)
        {
            string focus = ProcessHelper.RunAdbCommand(
                $"-s {serial} shell dumpsys window 2>/dev/null | grep mCurrentFocus", 10) ?? string.Empty;
            return focus.Contains("mCurrentFocus=Window", StringComparison.OrdinalIgnoreCase);
        }

        // Đếm TaskRecord hiện có trong AMS. Trả -1 nếu không đọc được (adb chập chờn)
        // để caller KHÔNG hiểu nhầm là leak. Máy khỏe 6–9; đen đo được 1460.
        private int CountTaskRecords(string serial)
        {
            string n = (ProcessHelper.RunAdbCommand(
                $"-s {serial} shell dumpsys activity activities 2>/dev/null | grep -c TaskRecord", 10) ?? string.Empty).Trim();
            return int.TryParse(n, out int c) ? c : -1;
        }

        // Kiểm tra + phục hồi màn hình đen sau khi đổi thiết bị. Trả về true nếu đã
        // phải restart framework (tức có phát hiện đen/rò rỉ và đã xử lý).
        //
        // v11.5: gate theo HAI điều kiện thay vì chỉ mCurrentFocus=null:
        //   (A) focus null kéo dài (đen tức thì — như cũ), HOẶC
        //   (B) TaskRecord > ngưỡng (rò rỉ tích lũy, framework CHƯA đen hẳn nhưng sắp
        //       cạn surface). (B) là gốc rễ thật: nó bắt leak từ acc này sang acc khác
        //       TRƯỚC khi SystemUI crash-loop -> tránh đúng ca acc-7 đen sau 7 lần đổi
        //       thiết bị "thành công" mà recovery cũ bỏ sót vì focus vẫn OK lúc đổi.
        /// <summary>
        /// FACADE PUBLIC cho luồng job (MainService/FacebookRegsiner) — assembly khác gọi
        /// <c>RecoverFrameworkIfBlackScreen</c> (private) không được. v12: luồng job gọi hàm này
        /// ở ĐẦU mỗi vòng account để BẮT SỚM rò rỉ TaskRecord tích lũy từ Facebook của account
        /// TRƯỚC (đo live: leak TỰ TĂNG cả khi tool tắt; đen xảy ra LÚC MỞ FB chứ không phải lúc
        /// đổi thiết bị, nên recovery cuối Change() của account trước bỏ sót).
        ///
        /// VPN-SAFE: gọi ở ĐẦU account, TRƯỚC khi nối proxy -> kill system_server (nếu leak) KHÔNG
        /// làm rớt tun0/VPN đang cần (lúc này proxy account mới chưa nối). Gated: chỉ can thiệp khi
        /// !focusOk HOẶC TaskRecord>ngưỡng; framework khỏe thì trả về false ngay (chi phí ~1s).
        /// </summary>
        public bool RecoverFrameworkIfBlackScreenPublic() => RecoverFrameworkIfBlackScreen();

        private bool RecoverFrameworkIfBlackScreen()
        {
            string serial = service.Device?.Serial;
            if (string.IsNullOrEmpty(serial)) return false;

            // Đo một lần cả hai chỉ số.
            bool focusOk = IsDisplayFocusOk(serial);
            int tasks = CountTaskRecords(serial);
            bool leak = tasks > TaskLeakThreshold;

            // Fast path: framework hiển thị bình thường VÀ không rò rỉ -> KHÔNG can thiệp
            // (đa số lần chạy). Chi phí 2 lệnh dumpsys (~1s), bỏ qua được ngay.
            if (focusOk && !leak) return false;

            // ── FIX C: COOLDOWN GATE ──
            // Đặt SAU fast-path (máy khỏe vẫn trả ngay, không tốn lock) và TRƯỚC settle-poll
            // (đang cooldown thì khỏi poll thừa). Nếu vừa kill system_server trong vòng
            // RecoverCooldownSeconds thì BỎ QUA lần này — framework còn đang dựng lại, kill
            // tiếp chỉ làm mất view phone mà không ích gì. Chặn đúng ca 2 lần recover/account.
            lock (_recoverLock)
            {
                if (_lastRecoverAt.TryGetValue(serial, out DateTime last)
                    && (DateTime.UtcNow - last).TotalSeconds < RecoverCooldownSeconds)
                {
                    DeviceChangeLog.Write(serial,
                        $"Recovery: BỎ QUA (cooldown) — vừa restart framework {((DateTime.UtcNow - last).TotalSeconds):F0}s trước (< {RecoverCooldownSeconds}s). focusOk={focusOk} TaskRecord={tasks}.");
                    return false;
                }
            }

            // focus null có thể chỉ thoáng qua lúc chuyển app trên máy yếu. Poll lại ~6s
            // (4 lần × 1.5s): máy khoẻ settle focus trong 2–3s. Nhưng nếu ĐÃ rò rỉ task
            // thì không cần chờ — leak không tự hết, xử lý ngay.
            if (!leak)
            {
                for (int i = 0; i < 4; i++)
                {
                    Thread.Sleep(1500);
                    if (IsDisplayFocusOk(serial)) return false; // tự settle -> bỏ qua
                }
            }
            else
            {
                // ── FIX C: SETTLE-POLL cho nhánh LEAK ──
                // Trước đây nhánh leak BỎ QUA settle-poll (chỉ `if (!leak)` mới poll) -> kill
                // system_server NGAY theo một lần đo duy nhất. Nhưng `dumpsys activity activities
                // | grep -c TaskRecord` có thể trả số cao CHẬP CHỜN (đúng lúc SystemUI đang đẻ
                // task tạm, hoặc adb nghẽn) -> kill oan -> mất view phone không đáng. Đo lại 2
                // lần × 1.5s: chỉ kết luận leak THẬT khi số cao LẶP LẠI ổn định. CountTaskRecords
                // trả -1 khi không đọc được -> KHÔNG bao giờ hiểu nhầm là leak.
                bool stableLeak = false;
                for (int i = 0; i < 2; i++)
                {
                    Thread.Sleep(1500);
                    int again = CountTaskRecords(serial);
                    if (again > TaskLeakThreshold) { stableLeak = true; break; }
                }
                if (!stableLeak)
                {
                    DeviceChangeLog.Write(serial,
                        $"Recovery: BỎ QUA — TaskRecord={tasks} không ổn định (đo lại 2 lần đều <= ngưỡng {TaskLeakThreshold}), không kill oan system_server (giữ view phone).");
                    return false;
                }
            }

            // Chụp chữ ký lỗi để ghi log chẩn đoán (không dùng làm điều kiện gate — nếu
            // logcat chập chờn thì việc focus null kéo dài sau đổi thiết bị đã đủ kết luận).
            string sig = ProcessHelper.RunAdbCommand(
                $"-s {serial} shell logcat -d -t 150 2>/dev/null | grep -iE 'Failed to transact|OutOfResourcesException|InputChannel is not initialized'",
                12) ?? string.Empty;
            bool hasSig = sig.Contains("transact", StringComparison.OrdinalIgnoreCase)
                       || sig.Contains("OutOfResources", StringComparison.OrdinalIgnoreCase)
                       || sig.Contains("InputChannel", StringComparison.OrdinalIgnoreCase);

            DeviceChangeLog.Write(serial,
                $"PHÁT HIỆN framework sắp/vừa cạn surface. focusOk={focusOk} TaskRecord={tasks} (ngưỡng {TaskLeakThreshold}). " +
                $"Lý do={(leak ? "RÒ RỈ TaskRecord tích lũy (SystemUI crash-loop đẻ task launcher rỗng)" : "mCurrentFocus=null kéo dài")}. " +
                $"Chữ ký logcat={(hasSig ? sig.Trim() : "không đọc được logcat")}. " +
                $"Đang kill system_server để restart framework runtime (xóa toàn bộ TaskRecord rò rỉ)...");

            try
            {
                // ── FIX C: ghi timestamp TRƯỚC khi kill ── để cooldown gate ở đầu hàm chặn
                // mọi lần recover lặp trong RecoverCooldownSeconds kể từ giây phút này (kể cả
                // lần gọi thứ 2 ở cuối Change() của CÙNG account). Đặt trong try, ngay trước
                // RestartSystemServer: chỉ tính cooldown khi THẬT SỰ tiến hành kill.
                lock (_recoverLock) { _lastRecoverAt[serial] = DateTime.UtcNow; }

                // Phase A — kill system_server: zygote dựng lại toàn bộ Java framework
                // ĐỒNG BỘ với SurfaceFlinger mới (thường SF đã tự restart trước đó).
                // Đây là đòn quyết định đã xác minh live. `stop;start` KHÔNG thay được:
                // nó chỉ restart SF + SystemUI, để nguyên system_server cũ với binder chết.
                RestartSystemServer(serial);
                if (WaitForDisplayOk(serial, 80))
                {
                    ProcessHelper.RunAdbCommand($"-s {serial} shell input keyevent KEYCODE_HOME", 8);
                    DeviceChangeLog.Write(serial, "ĐÃ PHỤC HỒI framework runtime — màn hình hiển thị lại bình thường (mCurrentFocus hợp lệ).");
                    return true;
                }

                // Phase B — SF có thể vẫn đang crash-loop/stale. Làm mới SF + SystemUI
                // bằng `stop;start`, rồi kill system_server lần 2 để cả stack về đồng bộ.
                DeviceChangeLog.Write(serial,
                    "Phase A chưa đủ (mCurrentFocus vẫn null) -> stop;start làm mới SurfaceFlinger rồi kill system_server lần 2.");
                ProcessHelper.RunAdbCommand($"-s {serial} shell su -c 'stop'", 10);
                Thread.Sleep(2000);
                ProcessHelper.RunAdbCommand($"-s {serial} shell su -c 'start'", 10);
                Thread.Sleep(2000);
                RestartSystemServer(serial);
                if (WaitForDisplayOk(serial, 80))
                {
                    ProcessHelper.RunAdbCommand($"-s {serial} shell input keyevent KEYCODE_HOME", 8);
                    DeviceChangeLog.Write(serial, "ĐÃ PHỤC HỒI framework runtime (Phase B: stop;start + kill system_server) — màn hình hiển thị lại.");
                    return true;
                }

                DeviceChangeLog.Write(serial, "Restart framework CHƯA xác nhận hiển thị lại (mCurrentFocus vẫn null) — bước kế tiếp có thể vẫn đen.");
            }
            catch { }
            return false;
        }

        // Kill system_server để zygote dựng lại framework đồng bộ với SurfaceFlinger.
        // Nếu không lấy được pid (đã biến mất) thì fallback `stop; start`.
        private void RestartSystemServer(string serial)
        {
            string pid = (ProcessHelper.RunAdbCommand($"-s {serial} shell pidof system_server", 8) ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(pid))
                ProcessHelper.RunAdbCommand($"-s {serial} shell su -c 'kill -9 {pid}'", 10);
            else
                ProcessHelper.RunAdbCommand($"-s {serial} shell su -c 'stop; start'", 10);
        }

        // Chờ framework dựng lại: boot_completed=1 VÀ có window thật được focus.
        // budgetSeconds chia thành nhịp 3s. Trả về true ngay khi hiển thị OK.
        private bool WaitForDisplayOk(string serial, int budgetSeconds)
        {
            int polls = Math.Max(1, budgetSeconds / 3);
            for (int i = 0; i < polls; i++)
            {
                Thread.Sleep(3000);
                string bc = (ProcessHelper.RunAdbCommand($"-s {serial} shell getprop sys.boot_completed", 5) ?? string.Empty).Trim();
                if (bc == "1" && IsDisplayFocusOk(serial)) return true;
            }
            return false;
        }
        public void Open()
        {
            for (int i = 0; i < 5; i++)
            {
                service.LogHelper.SUCCESS("Đang mở ứng dụng QNHelper");
                service.AppStart(package_MaxChange, true, true, wait: true);
                service.SetSize();
                if (service.AppWait(package_MaxChange))
                {
                    service.LogHelper.SUCCESS("Đã mở ứng dụng QNHelper");
                    break;
                }
            }

        }
        public async Task<bool> Install()
        {
            // Danh sách package chỉ đổi khi cài/gỡ app — cache theo serial cho cả tiến
            // trình để mỗi account không phải gọi `pm list packages` (~0.5-1s) nữa.
            List<string> list = GetCachedPackages();
            for (int i = 0; i < 5; i++)
            {
                if (list.Contains(package_Deviceinfohw) && list.Contains(package_MaxChange))
                {
                    break;
                }
                service.LogHelper.SUCCESS("Cài đặt QNHelper");
                list = ListPackagesDirect();
                CachePackages(list);
                if (!list.Contains(package_Deviceinfohw))
                {
                    if (!File.Exists(path_DeviceInfoHW))
                    {
                        string dirPath = Path.GetDirectoryName(path_DeviceInfoHW) ?? Path.GetDirectoryName(AppContext.BaseDirectory)!;
                        Directory.CreateDirectory(dirPath);
                        InitHelper.GithubDown("https://raw.githubusercontent.com/LamLe2001/changer/main/app-deviceinfohw.apk", path_DeviceInfoHW);
                    }
                    service.InstallApp(path_DeviceInfoHW);
                }
                if (!list.Contains(package_MaxChange))
                {
                    if (!File.Exists(path_MaxChange))
                    {
                        string dirPath = Path.GetDirectoryName(path_MaxChange) ?? Path.GetDirectoryName(AppContext.BaseDirectory)!;
                        Directory.CreateDirectory(dirPath);
                        InitHelper.GithubDown("https://raw.githubusercontent.com/LamLe2001/changer/main/LamToolChanger.apk", path_MaxChange);
                    }
                    service.InstallApp(path_MaxChange);
                }
                service.Delay(1);
            }
            // Hai quyền gộp vào một phiên shell (trước đây 2 lời gọi tuần tự).
            service.Shell($"pm grant {package_MaxChange} android.permission.READ_EXTERNAL_STORAGE; pm grant {package_MaxChange} android.permission.WRITE_EXTERNAL_STORAGE");
            SetEnableModule();
            //ResetWallpaperDefault();
            return true;
        }

        // Cache danh sách package + trạng thái module LSPosed theo serial: danh sách
        // package và cấu hình module không đổi giữa các account trên cùng thiết bị,
        // nên mỗi account không cần gọi lại `pm list packages` và truy vấn sqlite
        // modules_config.db (mỗi thứ ~0.5-1s) nữa.
        private static readonly Dictionary<string, List<string>> _packageCache = new Dictionary<string, List<string>>();
        private static readonly HashSet<string> _moduleReadySerials = new HashSet<string>();

        // ── FIX C (cooldown chống kill system_server lặp) ─────────────────────────────────
        // RecoverFrameworkIfBlackScreen được gọi tới 2 LẦN trong MỘT vòng account: đầu account
        // (MainService.RunAsync -> RecoverFrameworkIfBlackScreenPublic) VÀ cuối account (Change()
        // finally). Không có cooldown thì một máy leak thật sẽ bị kill system_server 2 lần/account
        // = mất view phone 2 lần. Cooldown theo serial: sau khi ĐÃ recover (kill), mọi lần gọi kế
        // tiếp trong RecoverCooldownSeconds sẽ trả false ngay (framework vừa mới dựng lại, cho nó
        // thời gian ổn định + để TaskRecord tự reset về mức khỏe). Account kế (sau vài phút farming)
        // cooldown hết -> vẫn recover được nếu leak THẬT quay lại. Cùng pattern lock+Dictionary
        // với _packageCache ở trên.
        private const int RecoverCooldownSeconds = 180; // 3 phút
        private static readonly object _recoverLock = new object();
        private static readonly Dictionary<string, DateTime> _lastRecoverAt = new Dictionary<string, DateTime>();

        private List<string> GetCachedPackages()
        {
            string serial = service.Device?.Serial ?? "?";
            lock (_packageCache)
            {
                if (_packageCache.TryGetValue(serial, out var cached)) return cached;
            }
            var list = ListPackagesDirect();
            CachePackages(list);
            return list;
        }

        private void CachePackages(List<string> list)
        {
            string serial = service.Device?.Serial ?? "?";
            lock (_packageCache) { _packageCache[serial] = list; }
        }
        public async Task<bool> Change(string filePath, bool backup, string brand, string country)
        {
            await Install();

            // Ghi log bền vững ra file (Logs\<ngày>\DeviceChange.txt) vì LogHelper
            // chỉ ghi đè ô trạng thái tạm thời — bước đổi proxy ngay sau đó sẽ xóa
            // mất dòng đổi thiết bị trên UI, khiến người dùng không thấy gì.
            Trace($"BẮT ĐẦU đổi thiết bị. uid-file=[{filePath}] backup={backup}");

            // ── CHẶN TẬN GỐC MÀN HÌNH ĐEN (build v11) ──────────────────────────────
            // Bật watchdog kill mirror ngoài (scrcpy `cleanup=false` — xiaowei.exe) SUỐT
            // bước đổi thiết bị. Mirror đó để VirtualDisplay MỒ CÔI mỗi khi OMX encoder
            // crash -> SurfaceFlinger SIGSEGV ở createSurface kế tiếp (app changer / HOME
            // / Facebook) -> đen màn hình. Xem MirrorSuppressor để có phân tích đầy đủ.
            // Start ở ĐÂY (trước mọi AppStart(changer)) để cover toàn bộ cửa sổ nguy hiểm.
            var mirrorGuard = new MirrorSuppressor(service.Device?.Serial ?? string.Empty);
            mirrorGuard.Start();

            // finally: app changer phải LUÔN bị đóng + về Home trước khi trả về cho
            // bước kế tiếp (đổi proxy), kể cả khi Change ném exception giữa chừng.
            // Đây là chỗ chặn màn hình đen — xem CleanupChangerForeground().
            try
            {
                // Hai trường hợp tài khoản:
                //  - ĐÃ có profile (<uid>.tar.gz tồn tại): nạp lại đúng thông tin thiết bị
                //    đã lưu để giữ danh tính thiết bị ổn định giữa các lần chạy. KHÔNG
                //    sinh mới (trước đây code luôn broadcast CHANGE làm mất profile này).
                //  - CHƯA có profile: sinh thông tin thiết bị MỚI và xác nhận fingerprint
                //    thực sự thay đổi (không chỉ tin vào chuỗi "Broadcast completed").
                // File rác "....\.tar.gz" (caller nối path với uid rỗng) tồn tại thật nhưng
                // filename không có phần tên -> coi như KHÔNG có profile, nếu không mọi acc
                // mới nạp lại cùng một fingerprint cũ. Caller hợp lệ (farming) luôn truyền
                // <uid>.tar.gz hoặc chuỗi rỗng nên không bị ảnh hưởng.
                // LƯU Ý: Path.GetFileNameWithoutExtension(".tar.gz") trả về ".tar" (KHÔNG
                // rỗng) nên phải soi fileName trực tiếp: chấm ĐẦU TIÊN phải đứng sau ít
                // nhất 1 ký tự tên (uid) thì file profile mới hợp lệ.
                string fileName = string.IsNullOrEmpty(filePath) ? string.Empty : Path.GetFileName(filePath);
                bool validProfileFile = fileName.IndexOf('.') > 0;
                if (!string.IsNullOrEmpty(filePath) && !validProfileFile)
                {
                    Trace($"BỎ qua file rác filename không có tên [{filePath}] -> coi như chưa có profile.");
                }
                bool hasProfile = validProfileFile && File.Exists(filePath);

                if (hasProfile)
                {
                    Trace("Tài khoản CÓ profile -> nạp lại (Restore).");
                    if (Restore(filePath))
                    {
                        string restored = GetInfoDeviceName(10);
                        if (!string.IsNullOrEmpty(restored))
                        {
                            service.LogHelper.SUCCESS($"Đã nạp lại profile thiết bị [{restored}]");
                            Trace($"KẾT THÚC: nạp lại profile thành công [{restored}]");
                            return true;
                        }
                    }
                    service.LogHelper.Log("Nạp profile thất bại -> chuyển sang sinh thiết bị mới.");
                    Trace("Nạp profile thất bại -> chuyển sang sinh thiết bị mới.");
                }
                else
                {
                    Trace("Tài khoản CHƯA có profile -> sinh thiết bị mới.");
                }

                bool ok = GenerateNewDevice(brand, country);
                Trace($"KẾT THÚC: sinh thiết bị mới = {(ok ? "thành công" : "thất bại")}");
                return ok;
            }
            finally
            {
                // 1) Dọn "focus ma" của app changer (force-stop + HOME) — đủ cho đa số
                //    trường hợp app UI bị kill nhưng framework còn sống.
                CleanupChangerForeground();
                // 2) Nếu màn hình VẪN đen (mCurrentFocus=null kéo dài) => SurfaceFlinger
                //    đã chết giữa chừng, binder của system_server chết theo. Đây là nguyên
                //    nhân gốc v8/v9 bỏ sót. Phục hồi bằng cách restart framework runtime
                //    (kill system_server). Đặt ở Change() — chạy TRƯỚC khi nối proxy — nên
                //    soft-restart KHÔNG làm rớt tun0/VPN của com.vat.vpn.
                //    Mirror-guard VẪN chạy trong lúc này (watchdog là process của init/adbd,
                //    không thuộc framework nên sống qua kill system_server) -> lệnh HOME của
                //    bước recovery cũng được bảo vệ khỏi mồ côi mới.
                RecoverFrameworkIfBlackScreen();
                // 3) Tắt mirror-guard SAU CÙNG (sau cả recovery) để toàn bộ cửa sổ đổi thiết
                //    bị + phục hồi đều không có mồ côi VirtualDisplay. Sau khi tắt, mirror
                //    ngoài (xiaowei) tự kết nối lại -> người dùng xem màn hình như thường.
                mirrorGuard.Stop();
            }
        }

        // Ghi một dòng log bền vững (kèm serial + timestamp) ra file để truy vết
        // bước đổi thiết bị độc lập với ô trạng thái tạm thời trên UI.
        private void Trace(string message)
        {
            string serial = service.Device?.Serial ?? "?";
            DeviceChangeLog.Write(serial, message);
        }

        // Sinh thông tin thiết bị mới, thử tối đa 3 lần, mỗi lần xác nhận fingerprint
        // trong Device.xml thực sự khác trước khi broadcast. Trả về true khi đọc được
        // thông tin thiết bị hợp lệ.
        private bool GenerateNewDevice(string brand, string country)
        {
            string before = GetInfoDeviceName(0);
            Trace($"Sinh thiết bị mới. fingerprint TRƯỚC = [{before}]");
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                // Khởi động app changer trước khi broadcast để receiver + module
                // LSPosed sẵn sàng (giống luồng GetIP); -f 32 để receiver vẫn chạy
                // được nếu process từng bị dừng.
                service.AppStart(package_MaxChange);
                bool sent = ChangeDeviceName(brand, country);
                string after = GetInfoDeviceName(10);
                Trace($"  lần {attempt}: broadcast={sent} fingerprint SAU = [{after}]");

                if (sent && !string.IsNullOrEmpty(after) && after != before)
                {
                    service.LogHelper.SUCCESS($"Đã đổi thiết bị mới [{after}]");
                    return true;
                }
                service.LogHelper.Log(
                    $"Đổi thiết bị lần {attempt} chưa xác nhận (trước=[{before}] sau=[{after}]).");
                Thread.Sleep(1500);
            }

            // Broadcast có thể đã hoàn tất nhưng fingerprint trùng (hiếm). Nếu vẫn đọc
            // được thông tin thiết bị hợp lệ thì coi như thành công, log rõ để theo dõi.
            string final = GetInfoDeviceName(0);
            if (!string.IsNullOrEmpty(final))
            {
                service.LogHelper.SUCCESS($"Thiết bị hiện tại [{final}]");
                Trace($"  fingerprint không đổi sau 3 lần nhưng vẫn hợp lệ [{final}] -> coi như OK.");
                return true;
            }
            service.LogHelper.ERROR("Không đổi được thiết bị (không đọc được Device.xml).");
            Trace("  THẤT BẠI: không đọc được Device.xml sau 3 lần.");
            return false;
        }

        // Đọc danh sách package bằng lệnh adb trực tiếp với timeout cứng.
        // KHÔNG dùng service.AppList(): nó đi qua Shell với vòng retry + Connect
        // lại đầy đủ, là điểm treo im nhiều phút khi adb/device chập chờn.
        private List<string> ListPackagesDirect()
        {
            var list = new List<string>();
            string raw = ProcessHelper.RunAdbCommand(
                $"-s {service.Device.Serial} shell pm list packages", 15) ?? string.Empty;
            foreach (var line in raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("package:"))
                    list.Add(trimmed.Substring("package:".Length).Trim());
            }
            return list;
        }
        private bool Restore(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return false;
            }
            string fileName = Path.GetFileName(filePath);
            service.LogHelper.SUCCESS($"Restore device: {filePath}");
            service.ADB.Shell($"pm grant {package_MaxChange} android.permission.READ_EXTERNAL_STORAGE");
            service.ADB.Shell($"pm grant {package_MaxChange} android.permission.WRITE_EXTERNAL_STORAGE");
            bool flag = false;
            for (int i = 0; i < 10; i++)
            {
                service.Push(filePath, "/sdcard/");
                string text2 = service.ADB.Shell($"su -c cp /sdcard/{fileName} /data/data/{package_MaxChange}/{fileName}");
                text2 = service.ADB.Shell($"su -c tar -xzvf /data/data/{package_MaxChange}/{fileName}");
                string text = "awk '{print $3\\\":\\\"$4}'\"";
                text2 = service.ADB.Shell($"su -c \"ls -l /data/data | grep {package_MaxChange} | {text}");
                flag = text2 != "";
                text2 = service.ADB.Shell($"su -c chown -R " + text2 + $" /data/data/{package_MaxChange}");
                if (!flag)
                {
                    Thread.Sleep(2000);
                    continue;
                }
                return true;
            }
            return false;
        }
        public string GetInfoDeviceName(int waitTimeInSeconds = 0)
        {
            int startTime = Environment.TickCount;

            do
            {
                // Đọc Device.xml bằng lệnh adb trực tiếp với timeout cứng, tránh
                // service.Shell() (vòng retry + Connect lại) treo im nhiều phút.
                string fileContent = ProcessHelper.RunAdbCommand(
                    $"-s {service.Device.Serial} shell su -c 'cat /data/data/{package_MaxChange}/shared_prefs/Device.xml'",
                    10) ?? string.Empty;
                fileContent = fileContent.Trim();

                if (fileContent != "")
                {
                    try
                    {
                        XmlDocument xmlDocument = new XmlDocument();
                        xmlDocument.LoadXml(fileContent);

                        XmlNode fingerprintNode = xmlDocument.SelectSingleNode("//*[@name='fingerprint']");
                        XmlNode timeCheckNode = xmlDocument.SelectSingleNode("//*[@name='build_time']");

                        if (fingerprintNode != null && fingerprintNode.InnerText != "" &&
                            timeCheckNode != null && timeCheckNode.InnerText != "")
                        {
                            string deviceInfo = fingerprintNode.InnerText + timeCheckNode.InnerText;
                            return deviceInfo;
                        }
                    }
                    catch (Exception)
                    {
                        // Bỏ qua: nội dung chưa hợp lệ / chưa ghi xong.
                        if (waitTimeInSeconds == 0) break;
                        Thread.Sleep(PollIntervalMs);
                        continue;
                    }
                }

                if (waitTimeInSeconds == 0)
                {
                    break;
                }

                // Poll mỗi 500ms thay vì 2s: fingerprint thường có sau ~1-2s, bước chờ
                // 2s/lần từng cộng thêm trung bình ~1-2s vào mỗi lần đổi thiết bị.
                Thread.Sleep(PollIntervalMs);
            } while (Environment.TickCount - startTime < waitTimeInSeconds * 1000);

            return "";
        }

        public bool ChangeDeviceName(string brand, string country)
        {
            try
            {
                service.ADB.Shell($"pm grant {package_MaxChange} android.permission.READ_EXTERNAL_STORAGE");
                service.ADB.Shell($"pm grant {package_MaxChange} android.permission.WRITE_EXTERNAL_STORAGE");

                // -f 32 (FLAG_INCLUDE_STOPPED_PACKAGES): receiver AdbCaller vẫn nhận
                // được broadcast kể cả khi process changer đang bị dừng.
                string text2 = $"am broadcast -f 32 -a {package_MaxChange}.CHANGE -n {package_MaxChange}/.AdbCaller --ez on true";
                string output = service.ADB.Shell(text2);
                bool ok = output.Contains("Change SUCCESS", StringComparison.OrdinalIgnoreCase)
                          || output.Contains("Broadcast completed", StringComparison.OrdinalIgnoreCase);
                if (ok)
                {
                    string text4 = GetInfoDeviceName(10);
                    service.LogHelper.SUCCESS($"Đã gửi lệnh đổi thiết bị [{text4}]");
                }
                else
                {
                    service.LogHelper.ERROR($"Broadcast đổi thiết bị không xác nhận: {output}");
                }
                return ok;
            }
            catch (Exception ex)
            {
                service.LogHelper.ERROR("Lỗi đổi thiết bị: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> BackupDeviceInfoChange(string pathDevice)
        {
            string fileName = Path.GetFileName(pathDevice);
            service.LogHelper.SUCCESS($"Backup change device: {pathDevice}");
            string directoryPath = Path.GetDirectoryName(pathDevice);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            FileHelper.DeleteFile(pathDevice);
            //  Close();
            for (int i = 0; i < 10; i++)
            {
                service.ADB.Shell($" su -c tar -czvf /data/data/{package_MaxChange}/{fileName} /data/data/{package_MaxChange}/shared_prefs/*");
                service.ADB.Shell($" su -c cp /data/data/{package_MaxChange}/{fileName} /sdcard/{fileName}");
                ProcessHelper.RunAdbWithTimeout($" -s {service.Device.Serial} pull /sdcard/{fileName} \"" + pathDevice + "\"");
                service.ADB.Shell($"su -c rm -rf /data/data/{package_MaxChange}/*.tar.gz");
                service.ADB.Shell($" su -c rm -rf /sdcard/*.tar.gz");
                if (!File.Exists(pathDevice))
                {
                    await Task.Delay(3000);
                    continue;
                }
                return true;
            }
            return false;
        }
        private List<Modules> GetModules()
        {
            string command = "su -c \"sqlite3 /data/adb/lspd/config/modules_config.db 'SELECT mid, module_pkg_name, enabled FROM modules;'\"";
            string output = service.Shell(command);

            List<Modules> modules = new List<Modules>();
            output = service.Shell(command);
            // Phân tách chuỗi kết quả và tạo đối tượng Module
            string[] rows = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string row in rows)
            {
                var columns = row.Split('|');
                if (columns.Length == 3)
                {
                    Modules module = new Modules
                    {
                        mid = int.Parse(columns[0].Trim()),
                        modulePkgName = columns[1].Trim(),
                        enabled = int.Parse(columns[2].Trim())
                    };
                    modules.Add(module);
                }
            }

            return modules;
        }
        private List<Scope> GetScopes()
        {
            string command = "su -c \"sqlite3 /data/adb/lspd/config/modules_config.db 'SELECT mid, app_pkg_name, user_id FROM scope;'\"";
            string output = service.Shell(command);

            List<Scope> modules = new List<Scope>();

            // Phân tách chuỗi kết quả và tạo đối tượng Module
            string[] rows = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string row in rows)
            {
                var columns = row.Split('|');
                if (columns.Length == 3)
                {
                    Scope module = new Scope
                    {
                        mid = int.Parse(columns[0].Trim()),
                        app_pkg_name = columns[1].Trim(),
                        user_id = int.Parse(columns[2].Trim())
                    };
                    modules.Add(module);
                }
            }

            return modules;
        }
        private void UpdateModules(Modules module)
        {
            string command = $"su -c \"sqlite3 /data/adb/lspd/config/modules_config.db 'UPDATE modules SET enabled = 1 WHERE module_pkg_name = \\\"{module.modulePkgName}\\\";'\"\r\n";
            string output = service.Shell(command);
        }
        private void addMissingScopes(Modules module)
        {
            string[] requiredApps = { MaxChangeService.package_MaxChange, "com.facebook.katana", "com.instagram.android", "ru.andr7e.deviceinfohw" };
            foreach (string requiredApp in requiredApps)
            {
                string command = $"su -c \"sqlite3 /data/adb/lspd/config/modules_config.db 'INSERT INTO scope (mid, app_pkg_name, user_id) VALUES ({module.mid}, \\\"{requiredApp}\\\", 0);'\"";
                string output = service.Shell(command);
            }
        }
        public void SetEnableModule()
        {
            string serial = service.Device?.Serial ?? "?";
            // Module LSPosed đã bật + scope đủ trên thiết bị này rồi thì bỏ qua: mỗi lần
            // kiểm tra là 4 lời gọi sqlite3 qua su (~1-2s) mà kết quả không đổi giữa
            // các account.
            lock (_moduleReadySerials)
            {
                if (_moduleReadySerials.Contains(serial)) return;
            }

            List<Modules> modules = GetModules();
            if (!modules.Any())
            {
                modules = GetModules();
                service.LogHelper.ERROR("Chưa cài LSPosed");
                return;
            }
            Modules? targetModule = modules.FirstOrDefault(x => x.modulePkgName == package_MaxChange);
            if (targetModule == null)
            {
                service.LogHelper.ERROR("Chưa cài DTAChange");
                return;
            }
            if (targetModule.enabled == 0)
            {
                UpdateModules(targetModule);
            }
            addMissingScopes(targetModule);

            lock (_moduleReadySerials) { _moduleReadySerials.Add(serial); }
        }
        public async Task<string> GetIP(string state = "")
        {
            await Install();
            service.LogHelper.State = state;
            string ip = "";
            // finally: GetIP cũng AppStart(package_MaxChange) lên foreground để phát
            // broadcast GET_DEVICE_IP, và có tới 3 điểm return. Bọc try/finally để
            // đóng app + về Home trên MỌI đường thoát, không để lại "focus ma".
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {

                        service.LogHelper.SUCCESS($"Đang kiểm tra IP [{i + 1}]");
                        service.AppStart(package_MaxChange);
                        string result = ProcessHelper.RunAdbWithTimeout($"-s {service.Device.Serial} shell am broadcast -a {package_MaxChange}.GET_DEVICE_IP -n {package_MaxChange}/.AdbCaller");
                        if (result.Contains("result=1"))
                        {
                            Match match = Regex.Match(result, @"data=""(?<json>\{.*?\})""");
                            if (match.Success)
                            {
                                var json = match.Groups[1].Value;
                                var doc = System.Text.Json.JsonDocument.Parse(json);

                                string message = doc.RootElement.GetProperty("ip").GetString();
                                service.LogHelper.SUCCESS($"IP: {message}");
                                return message;
                            }

                        }
                        else
                        {
                            result = service.Shell("am", "broadcast", "-a", $"{package_MaxChange}.GET_DEVICE_IP", "-n", $"{package_MaxChange}/.AdbCaller");
                            if (result.Contains("result=1"))
                            {
                                Match match = Regex.Match(result, @"data=""(?<json>\{.*?\})""");
                                if (match.Success)
                                {
                                    var json = match.Groups[1].Value;
                                    var doc = System.Text.Json.JsonDocument.Parse(json);

                                    string message = doc.RootElement.GetProperty("ip").GetString();
                                    service.LogHelper.SUCCESS($"IP: {message}");
                                    return message;
                                }

                            }
                        }
                        ip = "";
                        service.LogHelper.ERROR("Không lấy được IP");
                    }
                    catch (Exception ex)
                    {
                        service.LogHelper.ERROR("Lỗi khi check IP: " + ex.Message);
                    }
                }
                return ip;
            }
            finally
            {
                CleanupChangerForeground();
            }
        }
        public bool ChangeWallpaper(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                service.LogHelper.ERROR($"File hình nền không tồn tại: {filePath}");
                return false;
            }
            try
            {
                string fileName = Path.GetFileName(filePath);
                string remotePath = $"/sdcard/{fileName}";

                service.LogHelper.SUCCESS($"Đổi hình nền: {fileName}");
                service.Push(filePath, "/sdcard");

                string command = $"am broadcast -a {package_MaxChange}.WALLPAPER -n {package_MaxChange}/.AdbCaller --es path {remotePath}";
                string result = service.ADB.Shell(command);

                bool success = result.Contains("Broadcast completed");
                if (success)
                {
                    service.LogHelper.SUCCESS("Đã đổi hình nền thành công");
                }
                else
                {
                    service.LogHelper.ERROR($"Đổi hình nền thất bại: {result}");
                }
                return success;
            }
            catch (Exception ex)
            {
                service.LogHelper.ERROR("Lỗi khi đổi hình nền: " + ex.Message);
                return false;
            }
        }
        public bool ResetWallpaperDefault()
        {
            try
            {
                service.LogHelper.SUCCESS("Reset hình nền mặc định");

                string command = $"am broadcast -a {package_MaxChange}.WALLPAPER_DEFAULT -n {package_MaxChange}/.AdbCaller";
                string result = service.ADB.Shell(command);

                bool success = result.Contains("Broadcast completed");
                if (success)
                {
                    service.LogHelper.SUCCESS("Đã reset hình nền mặc định thành công");
                }
                else
                {
                    service.LogHelper.ERROR($"Reset hình nền thất bại: {result}");
                }
                return success;
            }
            catch (Exception ex)
            {
                service.LogHelper.ERROR("Lỗi khi reset hình nền: " + ex.Message);
                return false;
            }
        }
    }
    public class Modules
    {
        public int mid { get; set; }
        public string modulePkgName { get; set; }
        public int enabled { get; set; }
    }
    public class Scope
    {
        public int mid;
        public string app_pkg_name;
        public int user_id;
    }
}
