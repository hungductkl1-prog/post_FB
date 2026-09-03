using System.Collections.Generic;
using Sunny.Subdy.Data.Context;

namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    /// <summary>
    /// Port nguyên cơ chế quản lý "Trạng thái" của MaxPhoneFarm (class UpdateStatus).
    /// - Trạng thái chạy được giữ trong 1 dictionary (Id -> status) ngay trên RAM.
    /// - Khi set cell "Trạng thái" trên lưới → cập nhật dictionary (SetStatusEntry).
    /// - Lúc nạp lưới → đọc lại từ dictionary (GetStatusEntry).
    /// - LoadStatusFromDb() nạp toàn bộ từ DB khi mở; FlushStatusToDb() ghi hàng loạt xuống DB.
    /// Khác MaxPhoneFarm: DB là Microsoft.Data.Sqlite (AccountContext) thay cho System.Data.SQLite,
    /// nhưng luồng dictionary + isSaveSettings giữ nguyên 1:1.
    /// </summary>
    public static class UpdateStatus
    {
        // Tương đương dictionary_0 của MaxPhoneFarm.
        private static Dictionary<string, string> _map = new Dictionary<string, string>();

        // Tương đương isSaveSettings của MaxPhoneFarm (bật/tắt lưu trạng thái xuống DB).
        public static bool isSaveSettings = true;

        public static string GetStatusEntry(string id)
        {
            if (!isSaveSettings) return "";
            if (id != null && _map.TryGetValue(id, out var v)) return v;
            return "";
        }

        public static void SetStatusEntry(string id, string status)
        {
            if (!isSaveSettings || string.IsNullOrEmpty(id)) return;
            _map[id] = status ?? "";
        }

        /// <summary>MaxPG: nạp toàn bộ (id -> status) từ DB vào dictionary khi mở tool.</summary>
        public static void LoadStatusFromDb()
        {
            if (!isSaveSettings) return;
            try
            {
                _map = new AccountContext().GetStatusMap();
            }
            catch
            {
                _map = new Dictionary<string, string>();
            }
        }

        /// <summary>MaxPG: ghi hàng loạt dictionary xuống DB (chỉ những entry có giá trị).</summary>
        public static void FlushStatusToDb()
        {
            if (!isSaveSettings) return;
            try
            {
                var entries = new List<KeyValuePair<string, string>>(_map.Count);
                foreach (var kv in _map)
                {
                    if (!string.IsNullOrEmpty(kv.Value) && kv.Value.Trim() != "")
                        entries.Add(kv);
                }
                if (entries.Count == 0) return;
                new AccountContext().UpdateStatusOnly(entries);
            }
            catch
            {
            }
        }
    }
}
