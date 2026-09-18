using System;
using System.IO;

namespace AutoAndroid
{
    // Log bền vững cho bước đổi thiết bị, ghi ra đường dẫn TUYỆT ĐỐI cạnh exe
    // (AppContext.BaseDirectory) để không phụ thuộc thư mục làm việc (CWD) của
    // process. LogHelper.SUCCESS/Log/ERROR chỉ ghi đè ô trạng thái tạm thời trên
    // UI (bước đổi proxy ghi đè mất ngay), nên cần file này để truy vết bước đổi
    // thiết bị có chạy và chạy trước đổi proxy hay không.
    public static class DeviceChangeLog
    {
        private const long MaxLogFileSizeBytes = 5 * 1024 * 1024; // 5MB

        // Đổi giá trị này mỗi lần build để file log tự khai bản binary đang chạy.
        // Nếu DeviceChange.txt không xuất hiện hoặc thiếu tag mới nhất => process
        // đang chạy KHÔNG phải exe vừa build.
        public const string BuildTag = "build-2026-09-18-v25";

        public static void Write(string serial, string message)
        {
            try
            {
                string dir = Path.Combine(AppContext.BaseDirectory, "Logs",
                    DateTime.Now.ToString("dd-MM-yyyy"));
                Directory.CreateDirectory(dir);
                string filePath = Path.Combine(dir, "DeviceChange.txt");

                bool append = true;
                var info = new FileInfo(filePath);
                if (info.Exists && info.Length >= MaxLogFileSizeBytes)
                    append = false; // file quá lớn thì ghi đè

                using (var writer = new StreamWriter(filePath, append, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{serial}] {message}");
                }
            }
            catch
            {
                // Không bao giờ để việc ghi log làm hỏng luồng job.
            }
        }
    }
}
