using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AutoAndroid
{
    /// <summary>
    /// CHẶN TẬN GỐC màn hình đen sau đổi thiết bị: một mirror NGOÀI tool (xiaowei.exe = scrcpy 2.4,
    /// /data/local/tmp/XWCaptureScreen.jar, chạy cờ <c>cleanup=false</c>) treo một VirtualDisplay trên
    /// SurfaceFlinger. Khi bộ mã hóa OMX h264 của nó crash (media.codec tombstone CHECK_EQ OMX_StateIdle),
    /// vì cleanup=false nên VirtualDisplay bị MỒ CÔI -> SF giữ weakref hỏng -> createSurface kế tiếp
    /// SIGSEGV (fault addr 0x4, tombstone surfaceflinger _02/_04/_10/_39/_40) -> SF CHẾT -> binder chết
    /// -> MÀN HÌNH ĐEN. (KHÔNG phải OOM: SF oom_score_adj=-1000 không thể bị LMKD giết.)
    ///
    /// MirrorSuppressor là một WATCHDOG chạy NỀN trên thiết bị suốt cửa sổ đổi thiết bị.
    /// v13 ORPHAN-AWARE: nó KHÔNG blanket-kill mirror nữa (đó là nguyên nhân NHẤP NHÁY ở
    /// v11/v12/v12.1). Nó theo dõi PID của bộ mã hóa media.codec/media.swcodec: encoder KHỎE
    /// (PID không đổi) -> GIỮ mirror sống để user xem xiaowei mượt; encoder vừa CRASH (PID đổi/
    /// mất) -> SIGKILL mirror ĐÚNG 1 LẦN. Cái chết binder sạch khiến SF teardown VirtualDisplay
    /// đúng cách -> KHÔNG mồ côi -> KHÔNG SIGSEGV -> KHÔNG màn hình đen, mà KHÔNG giết bừa mirror
    /// khỏe. Nhắm đúng sự kiện crash HIẾM (tombstone đo được vài lần/ngày) thay vì kill↔respawn
    /// liên tục.
    ///
    /// Phạm vi HẸP có chủ đích:
    ///   - CHỈ kill tiến trình cmdline chứa CẢ HAI "scrcpy.Server" VÀ "cleanup=false", và CHỈ lúc
    ///     encoder media.codec vừa đổi PID => đúng mirror ngoài đang sinh mồ côi.
    ///   - ScrcpyNet của tool (scrcpy 1.23, cleanup=true) KHÔNG bị chạm => tool vẫn tự xem màn hình được.
    ///   - Mirror khỏe KHÔNG bị đụng => người dùng xem xiaowei liên tục, hết nhấp nháy.
    ///
    /// Watchdog chạy detached (init/adbd là cha, KHÔNG thuộc framework) nên sống sót qua cả
    /// RecoverFrameworkIfBlackScreen kill system_server của v10, và tiếp tục bảo vệ KEYCODE_HOME
    /// mà recovery gửi sau đó. Vòng lặp TỰ HẾT sau MaxSweeps (~360s) kể cả khi Stop() không chạy.
    ///
    /// Cơ chế adb-on-device đã kiểm chứng: base64 -d ghi file (né ký tự shell/quote); script CHẠY TỪ FILE
    /// (cmdline của chính nó không chứa pattern nên không tự kill); nohup + redirect I/O để adb trả về
    /// (không treo WaitForExit); dừng bằng PID FILE (chính xác, không khớp nhầm).
    /// </summary>
    internal sealed class MirrorSuppressor
    {
        private const string ScriptPath = "/data/local/tmp/qn_mg.sh";
        private const string PidPath = "/data/local/tmp/qn_mg.pid";
        // Lưới an toàn tự hết nếu Stop() không bao giờ chạy (tool crash).
        //
        // v13 ORPHAN-AWARE: khôi phục 600 (~5ph của v12.1) -> 7200 (~60ph, bao trọn vòng account
        // KỂ CẢ farming). Lý do an toàn: watchdog v13 KHÔNG blanket-kill mirror khỏe nữa (xem
        // BuildGuardScript) nên chạy dài KHÔNG gây nhấp nháy; nó chỉ kill ĐÚNG lúc encoder
        // media.codec crash (PID đổi/mất) — thời điểm mồ côi VirtualDisplay sắp hình thành. Giữ
        // guard suốt account (kể cả farming) để bắt cả crash GIỮA farming, không để mồ côi sót sang
        // createSurface của account kế (DẠNG 1). 7200 chỉ là TRẦN tự hết nếu Stop() bị lỡ (tool
        // crash): watchdog chạy thêm rồi tự dừng, KHÔNG kill mirror khỏe => KHÔNG nhấp nháy.
        private const int MaxSweeps = 7200;

        // ── REF-COUNT THEO SERIAL (v12) ───────────────────────────────────────────────
        // Guard giờ có THỂ LỒNG NHAU: RunAsync giữ một guard suốt vòng account, và Change()
        // (chạy bên trong) vẫn Start/Stop guard riêng của nó. Cả hai dùng CHUNG một cặp
        // ScriptPath/PidPath trên device, nên nếu Stop() của guard TRONG kill pidfile + xóa
        // script thì guard NGOÀI chết theo (trong khi _started của nó vẫn true) -> phần còn
        // lại của account (proxy + mở Facebook) KHÔNG còn được bảo vệ -> đúng khoảng hở gây
        // đen màn hình mà ta đang vá.
        //
        // Ref-count đảm bảo: watchdog nền chỉ THẬT SỰ bật ở lần Start ĐẦU TIÊN (count 0->1)
        // và chỉ THẬT SỰ tắt ở lần Stop CUỐI CÙNG (count 1->0). Các lần Start/Stop lồng nhau
        // ở giữa chỉ tăng/giảm bộ đếm, KHÔNG đụng pidfile/script -> guard ngoài sống xuyên
        // suốt. Chuẩn backward-compatible: Change() chạy ĐỘC LẬP (DeviceServices) vẫn đi
        // 0->1 rồi 1->0 y như cũ.
        private static readonly object _refLock = new object();
        private static readonly Dictionary<string, int> _refCount = new Dictionary<string, int>();

        private readonly string _serial;
        private bool _started;

        internal MirrorSuppressor(string serial)
        {
            _serial = serial ?? string.Empty;
        }

        private int AcquireRef()
        {
            lock (_refLock)
            {
                _refCount.TryGetValue(_serial, out int n);
                n++;
                _refCount[_serial] = n;
                return n;
            }
        }

        private int ReleaseRef()
        {
            lock (_refLock)
            {
                _refCount.TryGetValue(_serial, out int n);
                n--;
                if (n < 0) n = 0;
                _refCount[_serial] = n;
                return n;
            }
        }

        /// <summary>
        /// Bật watchdog (best-effort, không bao giờ ném). Ghi script qua base64 rồi chạy nền detached,
        /// SAU ĐÓ chạy chế độ "once" ĐỒNG BỘ (rào chắn) — v13 "once" KHÔNG kill mirror, chỉ xác nhận
        /// watchdog đọc được PID encoder media.codec và trả về sạch. Mồ côi TÍCH LŨY từ trước do
        /// RecoverFrameworkIfBlackScreenPublic (đầu account, v10 kill system_server) dọn; từ đó watchdog
        /// nền chỉ kill mirror ĐÚNG lúc encoder crash. Mọi lỗi adb đều bị nuốt (không chặn đổi thiết bị).
        /// </summary>
        internal void Start()
        {
            if (_started || string.IsNullOrWhiteSpace(_serial)) return;
            // Tăng ref TRƯỚC. Nếu đây là guard LỒNG (count > 1) thì watchdog nền ĐÃ chạy rồi
            // (guard ngoài bật), nên KHÔNG cần ghi/nohup/sweep lại -> chỉ đánh dấu _started
            // và trả về. Điều này giữ guard ngoài sống xuyên suốt Change().
            //
            // _started=true NGAY SAU AcquireRef (trước try): Start/Stop phải LUÔN cân bằng
            // ref-count. Nếu đặt _started ở cuối try, một lần adb throw giữa chừng sẽ để count
            // đã tăng mà _started vẫn false -> Stop() return sớm KHÔNG ReleaseRef -> count kẹt
            // vĩnh viễn -> Start() lần sau thấy count>1 nên không thật sự bật watchdog (hỏng
            // guard im lặng). Đặt sớm ở đây đảm bảo Stop() luôn ReleaseRef tương ứng.
            int count = AcquireRef();
            _started = true;
            if (count > 1)
            {
                DeviceChangeLog.Write(_serial,
                    $"MIRROR-GUARD: guard lồng (ref={count}) — watchdog nền đã chạy, giữ nguyên.");
                return;
            }
            try
            {
                var script = BuildGuardScript();
                var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(script));
                // 1) Ghi script qua base64 (né ký tự shell/quote), tạo bởi root để mọi user đọc được.
                ProcessHelper.RunAdbCommand($"-s {_serial} shell \"su -c 'echo {b64} | base64 -d > {ScriptPath}'\"", 10);
                // 2) Chạy NỀN detached, redirect I/O để adb trả về (KHÔNG treo WaitForExit);
                //    nohup + nền => sống sau khi adb shell trả về.
                ProcessHelper.RunAdbCommand($"-s {_serial} shell \"su -c 'nohup sh {ScriptPath} >/dev/null 2>&1 &'\"", 10);
                // 3) RÀO CHẮN ĐỒNG BỘ (v13): chạy chế độ "once" ngay và CHỜ nó xong. "once" KHÔNG
                //    kill mirror — chỉ xác nhận watchdog đọc được PID encoder media.codec và trả về
                //    sạch (script/pidfile hợp lệ). Mồ côi TÍCH LŨY từ phiên trước do
                //    RecoverFrameworkIfBlackScreenPublic (đầu account, v10 kill system_server) dọn,
                //    nên ở đây KHÔNG cần blanket-kill (sẽ giết nhầm mirror khỏe => nhấp nháy). Từ
                //    giờ watchdog nền chỉ kill ĐÚNG lúc encoder crash. Best-effort, nuốt lỗi.
                ProcessHelper.RunAdbCommand($"-s {_serial} shell \"su -c 'sh {ScriptPath} once'\"", 15);
                DeviceChangeLog.Write(_serial,
                    "MIRROR-GUARD v13: đã bật watchdog orphan-aware (theo dõi PID media.codec; chỉ kill mirror scrcpy cleanup=false lúc encoder crash; giữ mirror khỏe => không nhấp nháy).");
            }
            catch (Exception ex)
            {
                DeviceChangeLog.Write(_serial, $"MIRROR-GUARD: bật thất bại (không chặn đổi thiết bị): {ex.Message}");
            }
        }

        /// <summary>
        /// Dừng watchdog (best-effort). Kill đúng PID từ pidfile rồi xóa file. Sau khi dừng, mirror ngoài
        /// tự nối lại (xiaowei respawn) => người dùng lại xem được màn hình.
        ///
        /// v12 ref-count: chỉ THẬT SỰ kill watchdog nền khi đây là lần Stop CUỐI CÙNG (count 1->0).
        /// Nếu còn guard ngoài đang giữ (count > 0 sau khi giảm) thì KHÔNG đụng pidfile/script ->
        /// guard ngoài tiếp tục bảo vệ phần còn lại của account (proxy + mở Facebook).
        /// </summary>
        internal void Stop()
        {
            if (!_started) return;
            try
            {
                int count = ReleaseRef();
                if (count > 0)
                {
                    DeviceChangeLog.Write(_serial,
                        $"MIRROR-GUARD: guard lồng dừng (ref còn {count}) — watchdog nền vẫn chạy cho guard ngoài.");
                    return;
                }
                ProcessHelper.RunAdbCommand(
                    $"-s {_serial} shell \"su -c 'kill -9 $(cat {PidPath}) 2>/dev/null; rm -f {PidPath} {ScriptPath}'\"", 10);
                DeviceChangeLog.Write(_serial,
                    "MIRROR-GUARD: đã tắt chặn, mirror ngoài sẽ tự nối lại.");
            }
            catch (Exception ex)
            {
                DeviceChangeLog.Write(_serial, $"MIRROR-GUARD: tắt lỗi (watchdog sẽ tự hết): {ex.Message}");
            }
            finally
            {
                _started = false;
            }
        }

        private static string BuildGuardScript()
        {
            // ── v13 ORPHAN-AWARE ──────────────────────────────────────────────────────────
            // v11/v12/v12.1 BLANKET-KILL mọi mirror scrcpy cleanup=false mỗi ~0.5s bất kể nó
            // khỏe hay không => mirror xiaowei bị kill↔respawn liên tục => NGƯỜI DÙNG THẤY
            // NHẤP NHÁY. Nhưng mồ côi VirtualDisplay (gốc DẠNG 1) CHỈ hình thành lúc bộ mã hóa
            // OMX h264 của mirror (media.codec / media.swcodec) CRASH — sự kiện HIẾM (tombstone
            // đo được: vài lần trong nhiều ngày). Blanket-kill là quá tay và là NGUYÊN NHÂN DUY
            // NHẤT của nhấp nháy.
            //
            // v13: watchdog theo dõi PID của media.codec/media.swcodec (encoder). Baseline lấy
            // TRƯỚC vòng lặp. Mỗi ~0.5s đọc lại PID:
            //   • PID KHÔNG ĐỔI (encoder khỏe = trạng thái bình thường, 99.99% thời gian)
            //     -> GIỮ mirror sống -> user xem xiaowei MƯỢT, HẾT nhấp nháy.
            //   • PID ĐỔI hoặc MẤT (encoder vừa crash = mồ côi VirtualDisplay sắp hình thành)
            //     -> kill mirror ĐÚNG 1 LẦN để dọn mồ côi -> chống SIGSEGV/đen DẠNG 1; xiaowei
            //        respawn sạch ngay sau, encoder PID mới thành baseline mới (không kill lặp).
            // Nhắm ĐÚNG sự kiện crash thật thay vì giết bừa một mirror khỏe.
            //
            // Dùng `pidof media.codec media.swcodec` (đã verify live: trả PID, rc=0, RẺ hơn quét
            // /proc 278 tiến trình mỗi lượt). mc() trả PID hiện tại (rỗng nếu encoder đã chết).
            //
            // Chế độ "once" (đối số 1 = once): KHÔNG kill gì — chỉ in baseline encoder rồi thoát.
            //   Rào chắn Start() chỉ cần xác nhận watchdog đọc được encoder; mồ côi TỪ TRƯỚC do
            //   RecoverFrameworkIfBlackScreenPublic (đầu account, v10 kill system_server) dọn, nên
            //   "once" KHÔNG blanket-kill để tránh giết nhầm mirror khỏe ngay lúc bắt đầu.
            // Không đối số: vòng watchdog orphan-aware chạy tới MaxSweeps rồi tự hết.
            var sb = new StringBuilder();
            sb.Append("#!/system/bin/sh\n");
            // mc(): PID encoder hiện tại (rỗng nếu media.codec/media.swcodec đã chết).
            sb.Append("mc() { pidof media.codec media.swcodec 2>/dev/null; }\n");
            // killmirror(): SIGKILL tiến trình scrcpy-server cờ cleanup=false (mirror ngoài xiaowei).
            sb.Append("killmirror() {\n");
            sb.Append("  for p in /proc/[0-9]*; do\n");
            sb.Append("    c=$(cat $p/cmdline 2>/dev/null | tr \"\\0\" \" \")\n");
            sb.Append("    case \"$c\" in\n");
            sb.Append("      *scrcpy.Server*)\n");
            sb.Append("        case \"$c\" in\n");
            sb.Append("          *cleanup=false*) kill -9 ${p#/proc/} 2>/dev/null ;;\n");
            sb.Append("        esac ;;\n");
            sb.Append("    esac\n");
            sb.Append("  done\n");
            sb.Append("}\n");
            sb.Append("if [ x$1 = xonce ]; then\n");
            sb.Append("  mc >/dev/null 2>&1\n");
            sb.Append("  exit 0\n");
            sb.Append("fi\n");
            sb.Append("echo $$ > " + PidPath + "\n");
            // baseline lấy TRƯỚC vòng lặp để sweep đầu không báo động giả.
            sb.Append("prev=$(mc)\n");
            sb.Append("i=0\n");
            sb.Append("while [ $i -lt " + MaxSweeps.ToString(CultureInfo.InvariantCulture) + " ]; do\n");
            sb.Append("  sleep 0.5 2>/dev/null || sleep 1\n");
            sb.Append("  cur=$(mc)\n");
            // Encoder khỏe (PID không đổi, kể cả cùng rỗng) -> giữ mirror. Encoder vừa đổi PID
            // hoặc biến mất (prev có, cur khác) -> kill mirror một lần dọn mồ côi.
            sb.Append("  if [ -n \"$prev\" ] && [ x$cur != x$prev ]; then\n");
            sb.Append("    killmirror\n");
            sb.Append("  fi\n");
            sb.Append("  prev=$cur\n");
            sb.Append("  i=$((i+1))\n");
            sb.Append("done\n");
            sb.Append("rm -f " + PidPath + "\n");
            return sb.ToString();
        }
    }
}
