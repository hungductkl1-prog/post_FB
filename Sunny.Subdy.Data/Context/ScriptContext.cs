using System.Data;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Models;

namespace Sunny.Subdy.Data.Context
{
    public class ScriptContext
    {
        private readonly AppDbContext _db;
        private const string TableName = nameof(Script);

        public ScriptContext()
        {
            _db = new AppDbContext("LT_Scipt");
            _db.EnsureTable<Script>();
        }

        private Script MapScript(IDataReader reader)
        {
            Guid id;

            try
            {
                var idStr = reader["Id"]?.ToString();
                id = Guid.TryParse(idStr, out var parsedGuid) ? parsedGuid : Guid.Empty;
            }
            catch
            {
                id = Guid.Empty;
            }

            return new Script
            {
                Id = id,
                Platform = reader["Platform"]?.ToString() ?? "",
                Name = reader["Name"]?.ToString() ?? "",
                DateCreate = reader["DateCreate"]?.ToString() ?? "",
            };
        }

        public List<Script> GetAll()
        {
            string query = $"SELECT * FROM {TableName}";
            return _db.GetAllEntities(query, MapScript);
        }
        public List<Script>? GetByPlatform(string platform)
        {
            string query = $"SELECT * FROM {TableName} WHERE Platform = @platform";
            var parameters = new Dictionary<string, object> { { "@platform", platform } };
            return _db.GetAllEntities(query, MapScript, parameters);
        }
        public Script? GetById(Guid id)
        {
            string query = $"SELECT * FROM {TableName} WHERE Id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id.ToString() } };
            return _db.GetAllEntities(query, MapScript, parameters).FirstOrDefault();
        }

        public Script? GetByName(string name, string platform)
        {
            string query = $"SELECT * FROM {TableName} WHERE Name = @name AND Platform = @platform";
            var parameters = new Dictionary<string, object> { { "@name", name }, { "@platform", platform } };
            return _db.GetAllEntities(query, MapScript, parameters).FirstOrDefault();
        }

        public bool Add(Script script) => _db.InsertEntity(script);

        public bool AddRange(List<Script> scripts) => _db.InsertEntities(scripts);

        public bool Update(Script script) => _db.UpdateEntity(script);

        public bool Update(List<Script> scripts) => _db.UpdateEntities(scripts);

        public bool DeleteById(Guid id)
        {
            string query = $"DELETE FROM {TableName} WHERE Id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id.ToString() } };
            return _db.ExecuteNonQuery(query, parameters);
        }

        public int FixMissingIds()
        {
            int fixedCount = 0;
            // Xử lý từng row một vì mỗi row cần Guid khác nhau
            while (true)
            {
                var query = $"UPDATE {TableName} SET Id = @newId WHERE (Id = '' OR Id IS NULL) AND rowid = (SELECT rowid FROM {TableName} WHERE Id = '' OR Id IS NULL LIMIT 1)";
                var parameters = new Dictionary<string, object> { { "@newId", Guid.NewGuid().ToString() } };
                if (!_db.ExecuteNonQuery(query, parameters)) break;
                fixedCount++;
                if (fixedCount > 1000) break;
            }
            return fixedCount;
        }

        /// <summary>
        /// Xoá mọi script tên "FarmXu" trên mọi platform (Farm-Xu cũ đã bị loại khỏi UI).
        /// Idempotent — gọi khi khởi tạo fQuanLyKichBan.
        /// </summary>
        public int PurgeFarmXu()
        {
            string query = $"DELETE FROM {TableName} WHERE Name = @name";
            var parameters = new Dictionary<string, object> { { "@name", ScriptNames.FarmXu } };
            return _db.ExecuteNonQuery(query, parameters) ? 1 : 0;
        }

        /// <summary>
        /// Đổi tên script legacy "Farm-Xu-VIP" → "Làm Job QN".
        /// Nếu đã tồn tại bản mới cùng platform thì xoá bản legacy để tránh trùng.
        /// Idempotent.
        /// </summary>
        public int RemapLegacyFarmXuVipName()
        {
            int affected = 0;
            try
            {
                var delDup = $"DELETE FROM {TableName} WHERE Name = @oldName AND Platform IN (SELECT Platform FROM {TableName} WHERE Name = @newName)";
                _db.ExecuteNonQuery(delDup, new Dictionary<string, object> {
                    { "@oldName", ScriptNames.FarmXuVipLegacy },
                    { "@newName", ScriptNames.FarmXuVip },
                });

                var renameQuery = $"UPDATE {TableName} SET Name = @newName WHERE Name = @oldName";
                var renameParams = new Dictionary<string, object> {
                    { "@oldName", ScriptNames.FarmXuVipLegacy },
                    { "@newName", ScriptNames.FarmXuVip },
                };
                if (_db.ExecuteNonQuery(renameQuery, renameParams)) affected++;
            }
            catch { /* silent */ }
            return affected;
        }

        public Script EnsureFarmXuVip(string platform = PlatformModel.Facebook)
        {
            var existing = GetByName(ScriptNames.FarmXuVip, platform);
            if (existing != null) return existing;

            var script = new Script
            {
                Id = Guid.NewGuid(),
                Platform = platform,
                Name = ScriptNames.FarmXuVip,
                DateCreate = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            };
            Add(script);
            return script;
        }
    }

    public static class ScriptNames
    {
        // FarmXu (không VIP) đã bỏ — giữ const để so sánh legacy cho đến khi remap xong.
        public const string FarmXu = "FarmXu";
        // Tên hiển thị mới của script QN (đổi từ "Farm-Xu-VIP" → "Làm Job QN").
        // Const cũ giữ lại để remap account legacy đã persist với tên cũ.
        public const string FarmXuVipLegacy = "Farm-Xu-VIP";
        public const string FarmXuVip = "Làm Job QN";

        public static bool IsBuiltIn(string name) =>
            string.Equals(name, FarmXuVip, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, FarmXuVipLegacy, StringComparison.OrdinalIgnoreCase);
    }
}
