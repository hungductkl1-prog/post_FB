using Microsoft.Data.Sqlite;
using Sunny.Subdy.Data.Models;
using System;
using System.Threading;

namespace Sunny.Subdy.Data.Context
{
    public class AccountContext
    {
        private readonly AppDbContext _db;
        private readonly JobHistoryContext _jobHistoryContext;
        private const string TableName = nameof(Account);
        // Index DDL is idempotent but costs a connection-open each; run it once per process.
        private static int _indexesEnsured;
        /// <summary>Cá»™t nháº¹ cho load danh sÃ¡ch â€” bá» Cookie/Token/UserAgent/MailRefreshToken/DeviceInfo/Note.</summary>
        public const string ListSelectColumns =
            "Id,Platformt,Uid,FullName,Password,TowFA,Cookie,Token,Gender,Bio,Friends,PagePro5,Groups,Follow,Birthday,DateCreate,Avatar," +
            "Phone,Email,PassMail,MailClientId,MailRefreshToken,EmailAddress,UserAgent,PassPrivateEmailAddress,DeviceInfo," +
            "NameFolder,NameScript,Proxy,Note,JobTotal,RecentInteraction,Serial,TokenJob,State,Status,Checked,IsView";

        public AccountContext()
        {
            _db = new AppDbContext("LT_Account");
            _db.EnsureTable<Account>();
            EnsureListIndexes();
            _jobHistoryContext = new JobHistoryContext();
        }

        private void EnsureListIndexes()
        {
            if (Interlocked.Exchange(ref _indexesEnsured, 1) == 1) return;
            _db.ExecuteNonQuery($"CREATE INDEX IF NOT EXISTS idx_account_platform_view ON {TableName} ({nameof(Account.Platformt)}, {nameof(Account.IsView)})");
            _db.ExecuteNonQuery($"CREATE INDEX IF NOT EXISTS idx_account_folder ON {TableName} ({nameof(Account.NameFolder)})");
            _db.ExecuteNonQuery($"CREATE INDEX IF NOT EXISTS idx_account_uid ON {TableName} ({nameof(Account.Uid)})");
            _db.ExecuteNonQuery($"CREATE INDEX IF NOT EXISTS idx_account_checked ON {TableName} ({nameof(Account.Checked)})");

            // Composite (filter + ORDER BY Uid, Id) â€” khÃ´ng cÃ³ nÃ³, má»—i trang windowed pháº£i SORT láº¡i
            // toÃ n bá»™ táº­p khá»›p WHERE â†’ fetch ná»n cháº­m â†’ row giá»¯ placeholder â†’ lÆ°á»›i "Ä‘Æ¡" khi cuá»™n
            // á»Ÿ 50kâ€“500k dÃ²ng. CÃ³ index nÃ y, phÃ¢n trang LIMIT/OFFSET + keyset seek Ä‘i theo thá»© tá»±
            // index (khÃ´ng sort). Tiebreaker Id á»Ÿ cuá»‘i Ä‘á»ƒ keyset (ORDER BY Uid, Id) cÅ©ng index-only.
            _db.ExecuteNonQuery($"CREATE INDEX IF NOT EXISTS idx_account_platform_view_uid ON {TableName} ({nameof(Account.Platformt)}, {nameof(Account.IsView)}, {nameof(Account.Uid)}, {nameof(Account.Id)})");
            _db.ExecuteNonQuery($"CREATE INDEX IF NOT EXISTS idx_account_folder_uid ON {TableName} ({nameof(Account.NameFolder)}, {nameof(Account.Uid)}, {nameof(Account.Id)})");
        }

        /// <summary>Column name â†’ ordinal map for the active reader. Built once per query,
        /// not per row â€” avoids the O(columns Ã— fields) name scan that dominated 30k loads.</summary>
        private static Dictionary<string, int> BuildOrdinalMap(SqliteDataReader reader)
        {
            var map = new Dictionary<string, int>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
                map[reader.GetName(i)] = i;
            return map;
        }

        private static string ReadString(SqliteDataReader reader, Dictionary<string, int> ord, string columnName)
            => ord.TryGetValue(columnName, out int i) && !reader.IsDBNull(i)
                ? reader.GetValue(i)?.ToString() ?? string.Empty
                : string.Empty;

        private static bool ReadBool(SqliteDataReader reader, Dictionary<string, int> ord, string columnName, bool defaultValue = false)
            => ord.TryGetValue(columnName, out int i) && !reader.IsDBNull(i)
                ? Convert.ToBoolean(reader.GetValue(i))
                : defaultValue;

        private static int ReadInt(SqliteDataReader reader, Dictionary<string, int> ord, string columnName, int defaultValue = 0)
            => ord.TryGetValue(columnName, out int i) && !reader.IsDBNull(i)
                ? Convert.ToInt32(reader.GetValue(i))
                : defaultValue;

        private Account MapToAccount(SqliteDataReader reader, Dictionary<string, int> ord)
        {
            Guid.TryParse(ReadString(reader, ord, "Id"), out var id);

            var account = new Account();
            account.BeginBulkLoad();
            try
            {
                account.Checked = ReadBool(reader, ord, nameof(Account.Checked));
                account.Running = ReadBool(reader, ord, nameof(Account.Running));
                account.ColorType = 0;
                account.Uid_Email = "";
                account.Id = id;
                account.Platformt = ReadString(reader, ord, nameof(Account.Platformt));
                account.Uid = ReadString(reader, ord, nameof(Account.Uid));
                account.Password = ReadString(reader, ord, nameof(Account.Password));
                account.TowFA = ReadString(reader, ord, nameof(Account.TowFA));
                account.Cookie = ReadString(reader, ord, nameof(Account.Cookie));
                account.Token = ReadString(reader, ord, nameof(Account.Token));
                account.Proxy = ReadString(reader, ord, nameof(Account.Proxy));
                account.Email = ReadString(reader, ord, nameof(Account.Email));
                account.Phone = ReadString(reader, ord, nameof(Account.Phone));
                account.UserAgent = ReadString(reader, ord, nameof(Account.UserAgent));
                account.TokenJob = ReadString(reader, ord, nameof(Account.TokenJob));
                account.FullName = ReadString(reader, ord, nameof(Account.FullName));
                account.State = ReadString(reader, ord, nameof(Account.State));
                account.Status = ReadString(reader, ord, nameof(Account.Status));
                account.Result = ReadString(reader, ord, nameof(Account.Result));
                account.Serial = ReadString(reader, ord, nameof(Account.Serial));
                account.IP = ReadString(reader, ord, nameof(Account.IP));
                account.UserName = ReadString(reader, ord, nameof(Account.UserName));
                account.NameFolder = ReadString(reader, ord, nameof(Account.NameFolder));
                account.Gender = ReadString(reader, ord, nameof(Account.Gender));
                account.Friends = ReadString(reader, ord, nameof(Account.Friends));
                account.Groups = ReadString(reader, ord, nameof(Account.Groups));
                account.Follow = ReadString(reader, ord, nameof(Account.Follow));
                account.Birthday = ReadString(reader, ord, nameof(Account.Birthday));
                account.PagePro5 = ReadString(reader, ord, nameof(Account.PagePro5));
                account.DateCreate = ReadString(reader, ord, nameof(Account.DateCreate));
                account.Avatar = ReadString(reader, ord, nameof(Account.Avatar));
                account.Note = ReadString(reader, ord, nameof(Account.Note));
                account.DeviceInfo = ReadString(reader, ord, nameof(Account.DeviceInfo));
                account.EmailAddress = ReadString(reader, ord, nameof(Account.EmailAddress));
                account.PassMail = ReadString(reader, ord, nameof(Account.PassMail));
                account.MailClientId = ReadString(reader, ord, nameof(Account.MailClientId));
                account.MailRefreshToken = ReadString(reader, ord, nameof(Account.MailRefreshToken));
                account.PassPrivateEmailAddress = ReadString(reader, ord, nameof(Account.PassPrivateEmailAddress));
                account.MailRecovery = ReadString(reader, ord, nameof(Account.MailRecovery));
                account.RecentInteraction = ReadString(reader, ord, nameof(Account.RecentInteraction));
                account.IsView = ReadBool(reader, ord, nameof(Account.IsView), defaultValue: true);
                account.JobTotal = ReadInt(reader, ord, nameof(Account.JobTotal));
                account.NameScript = ReadString(reader, ord, nameof(Account.NameScript));
            }
            finally
            {
                account.EndBulkLoad();
            }

            return account;
        }

        public List<Account> GetAll(string query, Dictionary<string, object>? parameters = null)
        {
            var result = new List<Account>();
            using var conn = _db.GetConnection();
            using var cmd = new SqliteCommand(query, conn);
            if (parameters != null)
                foreach (var kv in parameters)
                    cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            var ord = BuildOrdinalMap(reader);
            while (reader.Read())
                result.Add(MapToAccount(reader, ord));
            return result;
        }

        public sealed class ListStats
        {
            public int Total;
            public int Live;
            public int Die;
            public int Other;
            public int Checked;
        }

        public int GetListCount(string whereClause, Dictionary<string, object>? parameters = null)
        {
            string query = $"SELECT COUNT(*) FROM {TableName} WHERE {whereClause}";
            var result = _db.ExecuteScalar(query, parameters);
            return int.TryParse(result?.ToString(), out var n) ? n : 0;
        }

        public ListStats GetListStats(string whereClause, Dictionary<string, object>? parameters = null)
        {
            string query = $@"
SELECT
  COUNT(*) AS Total,
  COALESCE(SUM(CASE WHEN UPPER({nameof(Account.State)}) = 'LIVE' THEN 1 ELSE 0 END), 0) AS Live,
  COALESCE(SUM(CASE WHEN UPPER({nameof(Account.State)}) = 'DIE' OR {nameof(Account.State)} IN ('CP_282','CP_956') THEN 1 ELSE 0 END), 0) AS Die,
  COALESCE(SUM(CASE WHEN {nameof(Account.Checked)} = 1 THEN 1 ELSE 0 END), 0) AS Checked
FROM {TableName} WHERE {whereClause}";

            var rows = _db.ExecuteReader(query, r => new ListStats
            {
                Total = Convert.ToInt32(r.GetValue(0)),
                Live = Convert.ToInt32(r.GetValue(1)),
                Die = Convert.ToInt32(r.GetValue(2)),
                Checked = Convert.ToInt32(r.GetValue(3)),
            }, parameters);

            if (rows.Count == 0) return new ListStats();
            var stats = rows[0];
            stats.Other = stats.Total - stats.Live - stats.Die;
            return stats;
        }

        public List<Account> GetListPage(string whereClause, Dictionary<string, object>? parameters, int offset, int limit, string orderBy = "Uid")
        {
            // Tiebreaker Id Ä‘á»ƒ cÃ³ Tá»”NG thá»© tá»± á»•n Ä‘á»‹nh (Uid cÃ³ thá»ƒ trÃ¹ng) â€” biÃªn trang keyset vÃ 
            // trang OFFSET pháº£i khá»›p nhau, náº¿u khÃ´ng sáº½ láº·p/thiáº¿u dÃ²ng khi 2 path cÃ¹ng náº¡p.
            string tie = orderBy.TrimEnd().EndsWith("DESC", StringComparison.OrdinalIgnoreCase) ? "Id DESC" : "Id";
            string query = $"SELECT {ListSelectColumns} FROM {TableName} WHERE {whereClause} ORDER BY {orderBy}, {tie} LIMIT @__limit OFFSET @__offset";
            var p = new Dictionary<string, object>(parameters ?? new Dictionary<string, object>())
            {
                ["@__limit"] = limit,
                ["@__offset"] = offset
            };
            return GetAll(query, p);
        }

        /// <summary>
        /// Keyset pagination (seek method) â€” O(log n) thay vÃ¬ OFFSET O(offset).
        /// Láº¥y trang ngay sau/trÆ°á»›c 1 dÃ²ng má»‘c theo (orderByCol, Id). DÃ¹ng cho cuá»™n tuáº§n tá»±:
        /// trang ká» Ä‘Ã£ náº¡p cung cáº¥p dÃ²ng má»‘c, khÃ´ng cáº§n SCAN bá» qua offset dÃ²ng Ä‘áº§u.
        /// after=true: cÃ¡c dÃ²ng Ä‘á»©ng SAU má»‘c theo thá»© tá»± ORDER BY (cuá»™n xuá»‘ng).
        /// after=false: cÃ¡c dÃ²ng Ä‘á»©ng TRÆ¯á»šC má»‘c (cuá»™n lÃªn) â€” tráº£ vá» Ä‘Ã£ Ä‘áº£o láº¡i Ä‘Ãºng thá»© tá»± hiá»ƒn thá»‹.
        /// </summary>
        public List<Account> GetListPageKeyset(
            string whereClause, Dictionary<string, object>? parameters,
            string orderByCol, bool orderDesc, int limit,
            string anchorValue, string anchorId, bool after)
        {
            // HÆ°á»›ng scan: after â†’ theo chiá»u ORDER BY; !after â†’ ngÆ°á»£c chiá»u rá»“i Ä‘áº£o káº¿t quáº£.
            bool scanDesc = after ? orderDesc : !orderDesc;
            string cmp = scanDesc ? "<" : ">";
            string dir = scanDesc ? "DESC" : "ASC";
            // (col cmp @a) OR (col = @a AND Id cmp @aid): tuple compare cÃ³ tiebreaker Id.
            string seek = $"({orderByCol} {cmp} @__anchor OR ({orderByCol} = @__anchor AND Id {cmp} @__anchorId))";
            string query = $"SELECT {ListSelectColumns} FROM {TableName} WHERE ({whereClause}) AND {seek} " +
                           $"ORDER BY {orderByCol} {dir}, Id {dir} LIMIT @__limit";
            var p = new Dictionary<string, object>(parameters ?? new Dictionary<string, object>())
            {
                ["@__anchor"] = anchorValue ?? string.Empty,
                ["@__anchorId"] = anchorId ?? string.Empty,
                ["@__limit"] = limit
            };
            var list = GetAll(query, p);
            if (!after) list.Reverse();   // Ä‘Æ°a vá» thá»© tá»± hiá»ƒn thá»‹
            return list;
        }

        /// <summary>Load full scope in batches â€” projection columns only, no SELECT *.</summary>
        public List<Account> GetListAll(string whereClause, Dictionary<string, object>? parameters, int batchSize = 10000, string orderBy = "Uid")
        {
            batchSize = Math.Max(500, batchSize);
            var result = new List<Account>(8192);
            for (int offset = 0; ; offset += batchSize)
            {
                var batch = GetListPage(whereClause, parameters, offset, batchSize, orderBy);
                if (batch.Count == 0) break;
                result.AddRange(batch);
                if (batch.Count < batchSize) break;
            }
            return result;
        }

        public List<Guid> GetIdsInScope(string whereClause, Dictionary<string, object>? parameters, HashSet<Guid>? exclude = null, string? orderBy = null)
        {
            string orderClause = orderBy != null ? $" ORDER BY {orderBy}, Id" : "";
            string query = $"SELECT Id FROM {TableName} WHERE {whereClause}{orderClause}";
            var ids = _db.GetAllEntities(query, r =>
            {
                Guid.TryParse(r["Id"]?.ToString(), out var id);
                return id;
            }, parameters);
            if (exclude == null || exclude.Count == 0)
                return ids.Where(id => id != Guid.Empty).ToList();
            var list = new List<Guid>(Math.Max(0, ids.Count - exclude.Count));
            for (int i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (id != Guid.Empty && !exclude.Contains(id))
                    list.Add(id);
            }
            return list;
        }

        /// <summary>Láº¥y Uid theo pháº¡m vi query â€” khÃ´ng hydrate full Account.</summary>
        public List<string> GetUidsInScope(string whereClause, Dictionary<string, object>? parameters)
        {
            string query = $"SELECT {nameof(Account.Uid)} FROM {TableName} WHERE {whereClause}";
            var uids = _db.GetAllEntities(query, r => r[nameof(Account.Uid)]?.ToString() ?? string.Empty, parameters);
            return uids.Where(u => !string.IsNullOrEmpty(u)).ToList();
        }

        /// <summary>Id cÃ¡c tÃ i khoáº£n trÃ¹ng Uid trong pháº¡m vi (giá»¯ báº£n MIN(rowid), tráº£ pháº§n dÆ°).</summary>
        public List<Guid> GetDuplicateIds(string whereClause, Dictionary<string, object>? parameters)
        {
            string query = $@"
SELECT Id FROM {TableName}
WHERE {whereClause}
  AND Uid IN (SELECT Uid FROM {TableName} WHERE {whereClause} GROUP BY Uid HAVING COUNT(*) > 1)
  AND rowid NOT IN (SELECT MIN(rowid) FROM {TableName} WHERE {whereClause} GROUP BY Uid)";
            var ids = _db.GetAllEntities(query, r =>
            {
                Guid.TryParse(r["Id"]?.ToString(), out var id);
                return id;
            }, parameters);
            return ids.Where(id => id != Guid.Empty).ToList();
        }

        /// <summary>LÆ°u Checked theo pháº¡m vi query â€” khÃ´ng cáº§n 50k Account trong RAM.</summary>
        public bool SaveCheckedStatesScoped(string whereClause, Dictionary<string, object>? parameters, bool checkAllActive, HashSet<Guid>? uncheckedWhenAll, HashSet<Guid>? explicitCheckedIds)
        {
            string clear = $"UPDATE {TableName} SET Checked = 0 WHERE {whereClause}";
            if (!_db.ExecuteNonQuery(clear, parameters)) return false;

            if (checkAllActive)
            {
                string setAll = $"UPDATE {TableName} SET Checked = 1 WHERE {whereClause}";
                if (!_db.ExecuteNonQuery(setAll, parameters)) return false;
                if (uncheckedWhenAll == null || uncheckedWhenAll.Count == 0) return true;
                const int batchSize = 400;
                var exList = uncheckedWhenAll.ToList();
                for (int i = 0; i < exList.Count; i += batchSize)
                {
                    var chunk = exList.Skip(i).Take(batchSize).ToList();
                    if (!ExecuteCheckedBatch(chunk, 0)) return false;
                }
                return true;
            }

            if (explicitCheckedIds == null || explicitCheckedIds.Count == 0) return true;
            const int batch = 400;
            var ids = explicitCheckedIds.ToList();
            for (int i = 0; i < ids.Count; i += batch)
            {
                var chunk = ids.Skip(i).Take(batch).ToList();
                if (!ExecuteCheckedBatch(chunk, 1)) return false;
            }
            return true;
        }

        public List<Account> GetListSlimForFilter(string whereClause, Dictionary<string, object>? parameters)
        {
            string cols = $"Id,{nameof(Account.Uid)},{nameof(Account.FullName)},{nameof(Account.State)},{nameof(Account.Status)},{nameof(Account.Running)},{nameof(Account.RecentInteraction)},{nameof(Account.NameFolder)}";
            string query = $"SELECT {cols} FROM {TableName} WHERE {whereClause} ORDER BY {nameof(Account.Uid)}";
            return GetAll(query, parameters);
        }

        public bool UpdateNameScriptScoped(string whereClause, Dictionary<string, object>? parameters, string scriptName)
        {
            var p = new Dictionary<string, object>(parameters ?? new Dictionary<string, object>())
            {
                ["@script"] = scriptName
            };
            string query = $"UPDATE {TableName} SET {nameof(Account.NameScript)} = @script WHERE {whereClause}";
            return _db.ExecuteNonQuery(query, p);
        }

        public bool UpdateNameScriptByIds(IReadOnlyList<Guid> ids, string scriptName, int batchSize = 400)
        {
            if (ids == null || ids.Count == 0) return false;
            bool ok = true;
            for (int i = 0; i < ids.Count; i += batchSize)
            {
                var chunk = ids.Skip(i).Take(batchSize).ToList();
                var p = new Dictionary<string, object> { ["@script"] = scriptName };
                var placeholders = new System.Text.StringBuilder();
                for (int j = 0; j < chunk.Count; j++)
                {
                    string key = $"@id{j}";
                    p[key] = chunk[j].ToString();
                    if (j > 0) placeholders.Append(',');
                    placeholders.Append(key);
                }
                string query = $"UPDATE {TableName} SET {nameof(Account.NameScript)} = @script WHERE Id IN ({placeholders})";
                ok &= _db.ExecuteNonQuery(query, p);
            }
            return ok;
        }

        public List<Account> GetByIdsBatched(IReadOnlyList<Guid> ids, int batchSize = 400)
        {
            if (ids == null || ids.Count == 0) return new List<Account>();
            var result = new List<Account>(ids.Count);
            for (int i = 0; i < ids.Count; i += batchSize)
            {
                var chunk = ids.Skip(i).Take(batchSize).ToList();
                result.AddRange(GetByIds(chunk));
            }
            return result;
        }

        /// <summary>Chá»‰ láº¥y Uid Ä‘Ã£ tá»“n táº¡i â€” dÃ¹ng khi import hÃ ng loáº¡t, trÃ¡nh load full Account.</summary>
        public HashSet<string> GetExistingUids(List<string> nameFolders, string platform, bool? isView = true)
        {
            var parameters = new Dictionary<string, object>();
            var whereClauses = new List<string>();

            if (isView.HasValue)
            {
                whereClauses.Add("IsView = @isView");
                parameters["@isView"] = isView.Value ? 1 : 0;
            }

            if (nameFolders?.Any() == true)
            {
                var paramNames = nameFolders.Select((f, i) =>
                {
                    string key = $"@folder{i}";
                    parameters[key] = f;
                    return key;
                }).ToList();

                whereClauses.Add($"NameFolder IN ({string.Join(", ", paramNames)})");
            }

            whereClauses.Add("Platformt = @platformt");
            parameters["@platformt"] = platform;

            string query = $"SELECT Uid FROM {TableName}" +
                           (whereClauses.Any() ? $" WHERE {string.Join(" AND ", whereClauses)}" : "");

            var uids = _db.GetAllEntities(query, r => r["Uid"]?.ToString() ?? string.Empty, parameters);
            return new HashSet<string>(
                uids.Where(u => !string.IsNullOrEmpty(u)),
                StringComparer.OrdinalIgnoreCase);
        }

        public List<Account> GetAll(List<string> nameFolders, string platform,bool? isView = true)
        {
            var parameters = new Dictionary<string, object>();
            var whereClauses = new List<string>();

            if (isView.HasValue)
            {
                whereClauses.Add("IsView = @isView");
                parameters["@isView"] = isView.Value ? 1 : 0;
            }

            if (nameFolders?.Any() == true)
            {
                var paramNames = nameFolders.Select((f, i) =>
                {
                    string key = $"@folder{i}";
                    parameters[key] = f;
                    return key;
                }).ToList();

                whereClauses.Add($"NameFolder IN ({string.Join(", ", paramNames)})");
            }
            whereClauses.Add("Platformt = @platformt");
            parameters["@platformt"] = platform;

            string query = $"SELECT * FROM {TableName}" +
                           (whereClauses.Any() ? $" WHERE {string.Join(" AND ", whereClauses)}" : "");

            return GetAll(query, parameters);
        }
        public bool UpdateIsViewFalse(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0) return false;

            var parameters = new Dictionary<string, object>();
            var paramNames = ids.Select((id, i) =>
            {
                string key = $"@id{i}";
                parameters[key] = id.ToString();
                return key;
            }).ToList();

            string query = $"UPDATE {TableName} SET IsView = 0 WHERE Id IN ({string.Join(", ", paramNames)})";
            return _db.ExecuteNonQuery(query, parameters);
        }
        public bool UpdateIsViewTrue(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0) return false;

            var parameters = new Dictionary<string, object>();
            var paramNames = ids.Select((id, i) =>
            {
                string key = $"@id{i}";
                parameters[key] = id.ToString();
                return key;
            }).ToList();

            string query = $"UPDATE {TableName} SET IsView = 1 WHERE Id IN ({string.Join(", ", paramNames)})";
            return _db.ExecuteNonQuery(query, parameters);
        }
        public Account? Get(Guid id)
        {
            const string query = $"SELECT * FROM {TableName} WHERE Id = @id";
            var parameters = new Dictionary<string, object> { ["@id"] = id.ToString() };
            return GetAll(query, parameters).FirstOrDefault();
        }

        public Account? GetByUid(string uid)
        {
            const string query = $"SELECT * FROM {TableName} WHERE Uid = @uid";
            var parameters = new Dictionary<string, object> { ["@uid"] = uid };
            return GetAll(query, parameters).FirstOrDefault();
        }
        public List<Account> GetByIds(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
                return new List<Account>();
            var parameters = new Dictionary<string, object>();
            var paramNames = new List<string>();

            for (int i = 0; i < ids.Count; i++)
            {
                string paramName = $"@id{i}";
                paramNames.Add(paramName);
                parameters[paramName] = ids[i].ToString();
            }

            string inClause = string.Join(", ", paramNames);
            string query = $"SELECT * FROM {TableName} WHERE Id IN ({inClause})";

            return GetAll(query, parameters);
        }
        public int CountByFolder(string folderName)
        {
            const string query = $"SELECT COUNT(*) FROM {TableName} WHERE NameFolder = @folder";
            var parameters = new Dictionary<string, object> { ["@folder"] = folderName };
            var result = _db.ExecuteScalar(query, parameters);
            return int.TryParse(result?.ToString(), out var count) ? count : 0;
        }

        public bool Add(Account account) => _db.InsertEntity(account);
        public bool AddRange(List<Account> accounts) => _db.InsertEntities(accounts);

        public bool Update(Account account) => _db.UpdateEntity(account);
        public bool Update(List<Account> accounts) => _db.UpdateEntities(accounts);


        /// <summary>LÆ°u tráº¡ng thÃ¡i Checked â€” chá»‰ UPDATE cá»™t Checked, khÃ´ng ghi full 50k row.</summary>
        public bool SaveCheckedStates(IReadOnlyList<Account> accounts)
            => SaveCheckedStates(accounts, false, null);

        public bool SaveCheckedStates(IReadOnlyList<Account> accounts, bool checkAllActive, HashSet<Guid>? uncheckedWhenAll)
            => SaveCheckedStates(accounts, checkAllActive, uncheckedWhenAll, null);

        public bool SaveCheckedStates(IReadOnlyList<Account> accounts, bool checkAllActive, HashSet<Guid>? uncheckedWhenAll, HashSet<Guid>? explicitCheckedIds)
        {
            if (accounts == null || accounts.Count == 0) return true;

            var allIds = new List<Guid>(accounts.Count);
            for (int i = 0; i < accounts.Count; i++)
                allIds.Add(accounts[i].Id);

            List<Guid> checkedIds;
            if (checkAllActive && (uncheckedWhenAll == null || uncheckedWhenAll.Count == 0))
            {
                checkedIds = allIds;
            }
            else if (checkAllActive)
            {
                checkedIds = new List<Guid>(accounts.Count - uncheckedWhenAll!.Count);
                for (int i = 0; i < accounts.Count; i++)
                {
                    if (!uncheckedWhenAll.Contains(accounts[i].Id))
                        checkedIds.Add(accounts[i].Id);
                }
            }
            else if (explicitCheckedIds != null)
            {
                checkedIds = new List<Guid>(explicitCheckedIds.Count);
                foreach (var id in explicitCheckedIds)
                    checkedIds.Add(id);
            }
            else
            {
                checkedIds = new List<Guid>();
                for (int i = 0; i < accounts.Count; i++)
                {
                    if (accounts[i].Checked)
                        checkedIds.Add(accounts[i].Id);
                }
            }

            const int batchSize = 400;
            for (int i = 0; i < allIds.Count; i += batchSize)
            {
                var chunk = allIds.Skip(i).Take(batchSize).ToList();
                if (!ExecuteCheckedBatch(chunk, 0)) return false;
            }
            for (int i = 0; i < checkedIds.Count; i += batchSize)
            {
                var chunk = checkedIds.Skip(i).Take(batchSize).ToList();
                if (!ExecuteCheckedBatch(chunk, 1)) return false;
            }
            return true;
        }

        private bool ExecuteCheckedBatch(List<Guid> ids, int value)
        {
            if (ids.Count == 0) return true;
            var parameters = new Dictionary<string, object> { ["@checked"] = value };
            var paramNames = ids.Select((id, i) =>
            {
                string key = $"@id{i}";
                parameters[key] = id.ToString();
                return key;
            }).ToList();

            string query = $"UPDATE {TableName} SET Checked = @checked WHERE Id IN ({string.Join(", ", paramNames)})";
            return _db.ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// MaxPG-style: flush tráº¡ng thÃ¡i theo LÃ”, chá»‰ ghi cá»™t Status/State/RecentInteraction
        /// theo Id (khÃ´ng UPDATE full row). DÃ¹ng prepared statement trong 1 transaction.
        /// </summary>
        public bool UpdateStatusBatch(IReadOnlyList<Account> accounts)
        {
            if (accounts == null || accounts.Count == 0) return true;
            using var conn = _db.GetConnection();
            using (var pragma = new SqliteCommand("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;", conn))
                pragma.ExecuteNonQuery();
            using var tx = conn.BeginTransaction();
            string sql = $"UPDATE {TableName} SET {nameof(Account.Status)}=@status, " +
                         $"{nameof(Account.State)}=@state, {nameof(Account.RecentInteraction)}=@recent WHERE Id=@id";
            using var cmd = new SqliteCommand(sql, conn, tx);
            var pStatus = cmd.Parameters.Add(new SqliteParameter("@status", DBNull.Value));
            var pState = cmd.Parameters.Add(new SqliteParameter("@state", DBNull.Value));
            var pRecent = cmd.Parameters.Add(new SqliteParameter("@recent", DBNull.Value));
            var pId = cmd.Parameters.Add(new SqliteParameter("@id", DBNull.Value));
            cmd.Prepare();
            foreach (var a in accounts)
            {
                if (a == null) continue;
                pStatus.Value = (object)a.Status ?? DBNull.Value;
                pState.Value = (object)a.State ?? DBNull.Value;
                pRecent.Value = (object)a.RecentInteraction ?? DBNull.Value;
                pId.Value = a.Id.ToString();
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
            return true;
        }

        public bool UpdateAccountsFolderName(string oldFolder, string newFolder)
        {
            const string query = $"UPDATE {TableName} SET NameFolder = @newFolder WHERE NameFolder = @oldFolder";
            var parameters = new Dictionary<string, object>
            {
                ["@newFolder"] = newFolder,
                ["@oldFolder"] = oldFolder
            };
            return _db.ExecuteNonQuery(query, parameters);
        }

        public bool DeleteById(Guid id)
        {
            const string query = $"DELETE FROM {TableName} WHERE Id = @id";
            var parameters = new Dictionary<string, object> { ["@id"] = id.ToString() };
            return _db.ExecuteNonQuery(query, parameters);
        }

        public bool DeleteByIds(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0) return false;

            var parameters = new Dictionary<string, object>();
            var paramNames = ids.Select((id, i) =>
            {
                string key = $"@id{i}";
                parameters[key] = id.ToString();
                return key;
            }).ToList();

            string query = $"DELETE FROM {TableName} WHERE Id IN ({string.Join(", ", paramNames)})";
            return _db.ExecuteNonQuery(query, parameters);
        }

        /// <summary>MaxPG UpdateStatus.LoadStatusFromDb: náº¡p toÃ n bá»™ (Id -> Status) vÃ o dictionary.</summary>
        public Dictionary<string, string> GetStatusMap()
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            var rows = _db.ExecuteReader(
                $"SELECT Id, Status FROM {TableName}",
                r => new KeyValuePair<string, string>(
                    r.IsDBNull(0) ? "" : r.GetString(0),
                    r.IsDBNull(1) ? "" : r.GetString(1)));
            foreach (var row in rows)
                if (!string.IsNullOrEmpty(row.Key))
                    map[row.Key] = row.Value;
            return map;
        }

        /// <summary>MaxPG UpdateStatus.FlushStatusToDb / BulkInsertProxies("status"):
        /// chá»‰ UPDATE cá»™t Status theo Id, khÃ´ng Ä‘á»¥ng State/RecentInteraction.</summary>
        public bool UpdateStatusOnly(IReadOnlyList<KeyValuePair<string, string>> entries)
        {
            if (entries == null || entries.Count == 0) return true;
            using var conn = _db.GetConnection();
            using (var pragma = new SqliteCommand("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;", conn))
                pragma.ExecuteNonQuery();
            using var tx = conn.BeginTransaction();
            string sql = $"UPDATE {TableName} SET {nameof(Account.Status)}=@status WHERE Id=@id";
            using var cmd = new SqliteCommand(sql, conn, tx);
            var pStatus = cmd.Parameters.Add(new SqliteParameter("@status", DBNull.Value));
            var pId = cmd.Parameters.Add(new SqliteParameter("@id", DBNull.Value));
            cmd.Prepare();
            foreach (var kv in entries)
            {
                if (string.IsNullOrEmpty(kv.Key)) continue;
                pStatus.Value = (object)kv.Value ?? DBNull.Value;
                pId.Value = kv.Key;
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
            return true;
        }
    }

}
