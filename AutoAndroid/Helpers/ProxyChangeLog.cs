using System;
using System.IO;

namespace AutoAndroid
{
    // Log bền vững cho bước ĐỔI PROXY (VAT Proxy), ghi ra đường dẫn TUYỆT ĐỐI cạnh exe
    // (AppContext.BaseDirectory) -> file Logs\<dd-MM-yyyy>\ProxyChange.txt.
    //
    // Lý do: bước proxy trước đây chỉ dùng LogHelper.Log/SUCCESS/ERROR, mà các hàm đó chỉ
    // ghi đè ô trạng thái TẠM THỜI trên UI (bước sau ghi đè mất ngay), nên khi proxy lỗi
    // không để lại dấu vết để chẩn đoán. File này truy vết được broadcast có "successful"
    // không, UI fallback điền ô / vuốt / tìm nút CONNECT / tun0 lên thế nào.
    public static class ProxyChangeLog
    {
        private const long MaxLogFileSizeBytes = 5 * 1024 * 1024; // 5MB

        public static void Write(string serial, string message)
        {
            try
            {
                string dir = Path.Combine(AppContext.BaseDirectory, "Logs",
                    DateTime.Now.ToString("dd-MM-yyyy"));
                Directory.CreateDirectory(dir);
                string filePath = Path.Combine(dir, "ProxyChange.txt");

                bool append = true;
                var info = new FileInfo(filePath);
                if (info.Exists && info.Length >= MaxLogFileSizeBytes)
                    append = false; // file quá lớn thì ghi đè

                using (var writer = new StreamWriter(filePath, append, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{serial}] [ProxyChange/{DeviceChangeLog.BuildTag}] {message}");
                }
            }
            catch
            {
                // Không bao giờ để việc ghi log làm hỏng luồng job.
            }
        }
    }
}
