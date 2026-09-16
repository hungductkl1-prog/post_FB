using System;
using System.IO;
using System.Text;

namespace AutoAndroid
{
    // ─────────────────────────────────────────────────────────────────────────────
    // LỊCH SỬ CHẠY (RunHistory) — "file ghi lịch sử chạy để theo dõi CHÍNH XÁC bước
    // nào làm máy reboot".
    //
    // Ghi MỖI BƯỚC của vòng account (backup profile, backup device, đổi thiết bị, đổi
    // proxy, restore, login, farming) và MỖI LỆNH REBOOT (kèm lý do tường minh + stack
    // trace best-effort) ra file:   Logs\<dd-MM-yyyy>\RunHistory.txt   (cạnh exe).
    //
    // CÁCH ĐỌC:
    //  • Khi máy reboot do TOOL, sẽ có dòng [REBOOT] nêu CHÍNH XÁC nguồn
    //    (checkBox4 "Reboot khi mất mạng" / checkBox5 "Tự reboot sau N phút" /
    //     nút bấm tay / ...). Các dòng [STEP] ngay TRƯỚC [REBOOT] cho biết tài khoản
    //     đang ở bước nào.
    //  • Nếu KHÔNG có dòng [REBOOT] nào quanh thời điểm máy sập => reboot KHÔNG do
    //    tool (nguồn ngoài: kernel panic, cạn pin, quá nhiệt, ROM, phần cứng).
    //  • BuildTag trong mỗi dòng để biết máy đang chạy BẢN NÀO (v15 = có log này).
    //
    // Ghi log KHÔNG BAO GIỜ ném (mọi method nuốt exception) để không ảnh hưởng job.
    // ─────────────────────────────────────────────────────────────────────────────
    public static class RunHistoryLog
    {
        private const long MaxLogFileSizeBytes = 10 * 1024 * 1024; // 10MB thì ghi đè

        // Dùng chung BuildTag với DeviceChangeLog để cả 2 file tự khai cùng bản binary.
        public const string BuildTag = DeviceChangeLog.BuildTag;

        // Một bước bình thường của vòng account.
        public static void Step(string serial, string step) => Write(serial, "STEP", step);

        // Ghi ngay TRƯỚC khi phát lệnh reboot. reason = mô tả tường minh nguồn reboot
        // (deterministic, KHÔNG phụ thuộc stack trace — quan trọng vì NativeAOT có thể
        // không resolve được tên method).
        public static void Reboot(string serial, string reason)
        {
            Write(serial, "REBOOT", reason + " | caller=" + ExtractCaller());
        }

        // Sự kiện đáng chú ý khác (lỗi, ngoại lệ, điểm quyết định).
        public static void Note(string serial, string message) => Write(serial, "NOTE", message);

        public static void Write(string serial, string kind, string message)
        {
            try
            {
                string dir = Path.Combine(AppContext.BaseDirectory, "Logs",
                    DateTime.Now.ToString("dd-MM-yyyy"));
                Directory.CreateDirectory(dir);
                string filePath = Path.Combine(dir, "RunHistory.txt");

                bool append = true;
                var info = new FileInfo(filePath);
                if (info.Exists && info.Length >= MaxLogFileSizeBytes)
                    append = false; // file quá lớn thì ghi đè

                using (var writer = new StreamWriter(filePath, append, Encoding.UTF8))
                {
                    writer.WriteLine(
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{BuildTag}] [{serial}] [{kind}] {message}");
                }
            }
            catch
            {
                // Không bao giờ để việc ghi log làm hỏng luồng job.
            }
        }

        // Stack trace best-effort. NativeAOT/ILC thường KHÔNG có tên file/số dòng (không
        // nhúng PDB) nhưng tên Type.Method thường vẫn resolve. Bỏ 2 frame đầu
        // (ExtractCaller + Reboot). Trả về "Type.Method <- Type.Method <- ...".
        private static string ExtractCaller()
        {
            try
            {
                var st = new System.Diagnostics.StackTrace(true);
                var sb = new StringBuilder();
                int max = Math.Min(st.FrameCount, 8);
                for (int i = 2; i < max; i++)
                {
                    var f = st.GetFrame(i);
                    var m = f?.GetMethod();
                    if (m == null) continue;
                    if (sb.Length > 0) sb.Append(" <- ");
                    sb.Append(m.DeclaringType?.Name ?? "?").Append('.').Append(m.Name);
                }
                return sb.Length > 0 ? sb.ToString() : "(no-stack)";
            }
            catch
            {
                return "(no-stack)";
            }
        }
    }
}
