using Sunny.Subdy.Common.API.Model;
using System.Windows.Forms;

namespace Sunny.Subdy.Common.Models
{
    public class Globals
    {
        public static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
        public static List<string> GetFieldsToImportExport()
        {
            List<string> listField = new List<string>
            {
                Fields.Empty,
                Fields.Uid,
                Fields.Password,
                Fields._2FA,
                Fields.Token,
                Fields.Cookie,
                Fields.Proxy,
                Fields.Email,
                Fields.PassMail,
                Fields.MailAdress,
                Fields.MailRefreshToken,
                Fields.MailClientId,
                Fields.Username,
                Fields.Phone,
                Fields.UserAgent,
                Fields.PassMailRecover,
            };

            return listField;
        }
        public static string DeviceId = Guid.NewGuid().ToString();
        public static string NameApp = "auto-phone-farm";
        public static DataGridView DataGridView { get; set; } = new DataGridView();
        public static User User { get; set; }
        public static CancellationTokenSource CancellationTokenSource;
        public static ToolStripLabel ToolStripLabel16 { get; set; }
        public static ToolStripMenuItem JobTotal_toolStripMenuItem { get; set; }
        public static ToolStripDropDownButton ToolStripDropDownButton1 { get; set; }
        public static Label CoinLable { get; set; }
        public static Label PendingLable { get; set; }
        public static List<string> Gmails { get; set; } = new List<string>();
        public static readonly object Lock = new object();

    }

    /// <summary>
    /// Tích lũy tổng thời gian chạy trong ngày.
    /// Tự reset về 0 khi qua ngày mới (00:00).
    /// Thread-safe: dùng Interlocked cho counter.
    /// </summary>
    public static class DailyRunTimer
    {
        private static long _totalSecondsToday = 0;
        private static DateOnly _currentDay = DateOnly.FromDateTime(DateTime.Now);
        private static readonly object _dayLock = new object();

        /// <summary>Tổng giây đã chạy trong ngày hôm nay.</summary>
        public static long TotalSecondsToday
        {
            get
            {
                EnsureDayReset();
                return Interlocked.Read(ref _totalSecondsToday);
            }
        }

        /// <summary>
        /// Cộng thêm số giây vào tổng thời gian ngày hôm nay.
        /// Gọi sau mỗi lần claim/submit job xong.
        /// </summary>
        public static void AddSeconds(long seconds)
        {
            if (seconds <= 0) return;
            EnsureDayReset();
            Interlocked.Add(ref _totalSecondsToday, seconds);
        }

        /// <summary>Reset tổng thời gian về 0 (dùng cho ngày mới).</summary>
        public static void Reset()
        {
            Interlocked.Exchange(ref _totalSecondsToday, 0);
        }

        /// <summary>Kiểm tra và reset nếu đã sang ngày mới.</summary>
        private static void EnsureDayReset()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            if (today == _currentDay) return;
            lock (_dayLock)
            {
                if (today != _currentDay)
                {
                    _currentDay = today;
                    Interlocked.Exchange(ref _totalSecondsToday, 0);
                }
            }
        }

        /// <summary>Trả về chuỗi định dạng HH:mm:ss từ tổng giây.</summary>
        public static string FormatTime(long totalSeconds)
        {
            long h = totalSeconds / 3600;
            long m = (totalSeconds % 3600) / 60;
            long s = totalSeconds % 60;
            if (h > 0) return $"{h:D2}:{m:D2}:{s:D2}";
            return $"{m:D2}:{s:D2}";
        }
    }
}
