using System;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace Facebook_Farm_NewFeed_PostStory.Utils.Design
{
    /// <summary>
    /// Map exception → thông điệp tiếng Việt thân thiện. Hiện qua AntdUI.Notification.error
    /// (non-blocking, góc dưới phải). Stack trace log ra Debug cho dev.
    /// </summary>
    public static class ErrorHandler
    {
        public static string ToUserMessage(Exception ex) => ex switch
        {
            null                          => "Đã xảy ra lỗi không xác định.",
            ArgumentNullException         => "Vui lòng điền đầy đủ thông tin bắt buộc.",
            FileNotFoundException f       => $"Không tìm thấy file: {Path.GetFileName(f.FileName ?? string.Empty)}. Kiểm tra đường dẫn.",
            DirectoryNotFoundException    => "Không tìm thấy thư mục. Kiểm tra đường dẫn.",
            UnauthorizedAccessException   => "Không có quyền truy cập. Thử chạy với quyền Administrator.",
            TimeoutException              => "Hết thời gian chờ. Kiểm tra kết nối mạng và thử lại.",
            HttpRequestException h        => $"Lỗi kết nối: {h.Message}. Kiểm tra mạng hoặc proxy.",
            SqliteException               => "Không lưu được dữ liệu vào database. Thử lại hoặc khởi động lại app.",
            DbException                   => "Không lưu được dữ liệu vào database. Thử lại hoặc khởi động lại app.",
            InvalidOperationException     => "Thao tác không hợp lệ trong trạng thái hiện tại.",
            FormatException               => "Dữ liệu nhập sai định dạng. Kiểm tra lại.",
            OutOfMemoryException          => "Thiếu bộ nhớ. Thử giảm số lượng xử lý cùng lúc.",
            _                             => $"Đã xảy ra lỗi: {ex.Message}"
        };

        /// <summary>Show lỗi dạng toast. KHÔNG chặn UI.</summary>
        public static void Show(Form form, Exception ex, string? context = null)
        {
            if (ex == null) return;
            var msg   = ToUserMessage(ex);
            var title = string.IsNullOrWhiteSpace(context) ? "Lỗi" : context!;

            try
            {
                if (form != null && !form.IsDisposed)
                    AntdUI.Notification.error(form, title, msg, AntdUI.TAlignFrom.BR);
            }
            catch { /* ignore notification render errors */ }

            Debug.WriteLine($"[ERROR] {title} | {ex}");
        }

        /// <summary>Bọc action sync, tự Show lỗi. Trả về true nếu chạy xong không exception.</summary>
        public static bool SafeRun(Form form, Action action, string? context = null)
        {
            try { action(); return true; }
            catch (Exception ex) { Show(form, ex, context); return false; }
        }

        /// <summary>Bọc action async, tự Show lỗi. Trả về true nếu chạy xong không exception.</summary>
        public static async Task<bool> SafeRunAsync(Form form, Func<Task> action, string? context = null)
        {
            try { await action(); return true; }
            catch (Exception ex) { Show(form, ex, context); return false; }
        }
    }
}
