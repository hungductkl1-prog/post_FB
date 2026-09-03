using Microsoft.Data.Sqlite;
using Sunny.Subdy.Common.Logs;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Sunny.Subdy.Data
{
    public class AppDbContext
    {
        [AttributeUsage(AttributeTargets.Property)]
        public sealed class SqlKeyAttribute : Attribute { }

        private static readonly ConcurrentDictionary<Type, (PropertyInfo[] allProps, PropertyInfo? keyProp)> _propertyCache = new();

        private static (PropertyInfo[] allProps, PropertyInfo? keyProp) GetCachedTypeInfo(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
        {
            return _propertyCache.GetOrAdd(type, t =>
            {
                var all = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                           .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
                           .ToArray();
                var key = all.FirstOrDefault(p => p.GetCustomAttribute<SqlKeyAttribute>() != null);
                return (all, key);
            });
        }

        private readonly string _connectionString;
        private readonly string _dbPath;

        // WAL/journal mode persists in the DB file — only needs setting once per file per process.
        // Without this guard every `new XxxContext()` opened a connection + ran PRAGMA, and contexts
        // are constructed in farming hot loops (per-account Update), adding 1 connection-open each.
        private static readonly ConcurrentDictionary<string, bool> _walEnsured = new(StringComparer.OrdinalIgnoreCase);
        // (dbPath|typeName) → schema already ensured this process. EnsureTable<T> is idempotent but
        // expensive (connection open + PRAGMA table_info + possible ALTERs); skip after first run.
        private static readonly ConcurrentDictionary<string, bool> _tableEnsured = new(StringComparer.OrdinalIgnoreCase);

        public AppDbContext(string databaseName)
        {
            var baseDir = AppContext.BaseDirectory;
            var dataDir = Path.Combine(baseDir, "data");
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);

            _dbPath = Path.Combine(dataDir, $"{databaseName}.db");
            _connectionString = $"Data Source={_dbPath}";
            if (_walEnsured.TryAdd(_dbPath, true))
                EnsureWalMode();
        }

        /// <summary>WAL cho phép đọc song song lúc đang ghi — fetch trang nền của lưới ảo không bị
        /// chặn bởi write lock (triệu chứng "đơ"/row trắng). Chạy 1 lần, persist vào file DB.</summary>
        private void EnsureWalMode()
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();
                using var cmd = new SqliteCommand(
                    "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;", conn);
                cmd.ExecuteNonQuery();
            }
            catch { /* file mới / quyền ghi — bỏ qua, vẫn chạy được ở chế độ mặc định */ }
        }

        public object ExecuteScalar(string query, Dictionary<string, object>? parameters = null)
        {
            using var conn = GetConnection();
            using var cmd = new SqliteCommand(query, conn);

            if (parameters != null)
            {
                foreach (var (key, value) in parameters)
                {
                    cmd.Parameters.AddWithValue(key, value ?? DBNull.Value);
                }
            }
            return cmd.ExecuteScalar();
        }

        private string? MapTypeToSqlite(Type type)
        {
            // Nếu là Nullable<T>, lấy kiểu T
            if (Nullable.GetUnderlyingType(type) is Type underlyingType)
                type = underlyingType;

            if (type == typeof(int) || type == typeof(long)) return "INTEGER";
            if (type == typeof(string)) return "TEXT";
            if (type == typeof(bool)) return "INTEGER";
            if (type == typeof(double) || type == typeof(float)) return "REAL";
            if (type == typeof(DateTime)) return "TEXT";
            if (type == typeof(Guid)) return "TEXT";
            return null;
        }

        public SqliteConnection GetConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            try
            {
                conn.Open();
                // busy_timeout: nếu DB đang bị khóa (ghi), fetch nền chờ tối đa 5s rồi mới lỗi —
                // tránh trang lưới rơi về placeholder ("đơ"/trắng) khi có ghi đồng thời.
                using (var cmd = new SqliteCommand("PRAGMA busy_timeout=5000;", conn))
                    cmd.ExecuteNonQuery();
                return conn;
            }
            catch
            {
                conn.Dispose();
                throw;
            }
        }

        public void EnsureTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
        {
            var type = typeof(T);
            var tableName = type.Name;

            // Schema is migrated at most once per (db file, type) per process. The DDL below is
            // idempotent, but running it on every context construction cost a connection-open +
            // PRAGMA table_info scan each time — wasteful when contexts are created in tight loops.
            if (!_tableEnsured.TryAdd($"{_dbPath}|{tableName}", true))
                return;

            var (props, _) = GetCachedTypeInfo(type);

            using var conn = GetConnection();

            // Check existing table
            var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = new SqliteCommand($"PRAGMA table_info({tableName})", conn))
            using (var reader = cmd.ExecuteReader())
                while (reader.Read())
                    existingCols.Add(reader["name"].ToString());

            if (existingCols.Count == 0)
            {
                var columns = new List<string>();
                foreach (var prop in props)
                {
                    var sqliteType = MapTypeToSqlite(prop.PropertyType);
                    if (sqliteType == null) continue;
                    bool isKey = prop.GetCustomAttribute<SqlKeyAttribute>() != null;
                    columns.Add($"  {prop.Name} {sqliteType}{(isKey ? " PRIMARY KEY" : "")}");
                }

                if (columns.Count == 0)
                    throw new InvalidOperationException($"Type {type.Name} has no valid properties.");

                string createSql = $"CREATE TABLE IF NOT EXISTS {tableName} (\n{string.Join(",\n", columns)}\n);";
                using var createCmd = new SqliteCommand(createSql, conn);
                createCmd.ExecuteNonQuery();
            }
            else
            {
                foreach (var prop in props)
                {
                    if (existingCols.Contains(prop.Name)) continue;
                    var typeStr = MapTypeToSqlite(prop.PropertyType);
                    if (typeStr == null) continue;

                    string alterSql = $"ALTER TABLE {tableName} ADD COLUMN {prop.Name} {typeStr};";
                    using var alterCmd = new SqliteCommand(alterSql, conn);
                    alterCmd.ExecuteNonQuery();
                }
            }
        }

        public bool UpdateEntities<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(List<T> entities)
        {
            if (entities == null || entities.Count == 0)
                return false;

            var type = typeof(T);
            var tableName = type.Name;
            var (props, keyProp) = GetCachedTypeInfo(type);

            if (keyProp == null)
                throw new InvalidOperationException($"Type '{type.Name}' does not contain a property marked with [SqlKey].");

            using var conn = GetConnection();
            using var transaction = conn.BeginTransaction();

            foreach (var entity in entities)
            {
                if (entity == null) continue;

                var setClauses = new List<string>();
                var parameters = new List<SqliteParameter>();

                foreach (var prop in props)
                {
                    if (prop == keyProp) continue;

                    object? value = prop.GetValue(entity);
                    if (value is Guid g) value = g.ToString();

                    string paramName = $"@{prop.Name}";
                    setClauses.Add($"{prop.Name} = {paramName}");
                    parameters.Add(new SqliteParameter(paramName, value ?? DBNull.Value));
                }

                var keyValue = keyProp.GetValue(entity);
                if (keyValue == null)
                    throw new InvalidOperationException("Primary key value cannot be null.");

                if (keyValue is Guid kg) keyValue = kg.ToString();
                parameters.Add(new SqliteParameter("@Id", keyValue));

                string sql = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {keyProp.Name} = @Id;";
                using var cmd = new SqliteCommand(sql, conn, transaction);
                cmd.Parameters.AddRange(parameters.ToArray());
                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
            return true;
        }

        //public bool InsertEntity<T>(T entity)
        //{
        //    if (entity == null) return false;

        //    var type = typeof(T);
        //    var tableName = type.Name;
        //    var props = type.GetProperties();

        //    var columnNames = new List<string>();
        //    var paramNames = new List<string>();
        //    var parameters = new List<SqliteParameter>();

        //    foreach (var prop in props)
        //    {
        //        var value = prop.GetValue(entity);
        //        if (value == null) continue;

        //        // Lọc chỉ lấy các kiểu đơn giản: primitive, string, Guid
        //        var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        //        if (!(propType.IsPrimitive || propType == typeof(string) || propType == typeof(Guid) || propType.IsEnum))
        //            continue;

        //        if (value is Guid g)
        //            value = g.ToString();

        //        columnNames.Add(prop.Name);
        //        string paramName = $"@{prop.Name}";
        //        paramNames.Add(paramName);
        //        parameters.Add(new SqliteParameter(paramName, value));
        //    }

        //    if (columnNames.Count == 0) return false;

        //    string sql = $"INSERT INTO {tableName} ({string.Join(",", columnNames)}) VALUES ({string.Join(",", paramNames)});";

        //    using var conn = GetConnection();
        //    using var cmd = new SqliteCommand(sql, conn);
        //    cmd.Parameters.AddRange(parameters.ToArray());
        //    return cmd.ExecuteNonQuery() > 0;
        //}
        public bool InsertEntity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(T entity)
        {
            if (entity == null) return false;

            var type = typeof(T);
            var tableName = type.Name;
            var (props, keyProp) = GetCachedTypeInfo(type);

            var columnNames = new List<string>();
            var paramNames = new List<string>();
            var parameters = new List<SqliteParameter>();

            foreach (var prop in props)
            {
                var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (!(propType.IsPrimitive || propType == typeof(string) || propType == typeof(Guid) || propType.IsEnum))
                    continue;

                object? value = prop.GetValue(entity);

                // Khóa chính Guid: tự sinh nếu rỗng, INSERT kèm; khóa chính int auto-increment thì bỏ qua
                if (prop == keyProp)
                {
                    if (prop.PropertyType == typeof(Guid))
                    {
                        var guid = (Guid?)value ?? Guid.Empty;
                        if (guid == Guid.Empty)
                        {
                            guid = Guid.NewGuid();
                            prop.SetValue(entity, guid);
                            value = guid;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                if (value == null) continue;
                if (value is Guid g) value = g.ToString();

                columnNames.Add(prop.Name);
                string paramName = $"@{prop.Name}";
                paramNames.Add(paramName);
                parameters.Add(new SqliteParameter(paramName, value));
            }

            if (columnNames.Count == 0) return false;

            string sql = $"INSERT INTO {tableName} ({string.Join(",", columnNames)}) VALUES ({string.Join(",", paramNames)});";

            using var conn = GetConnection();
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());
            return cmd.ExecuteNonQuery() > 0;
        }

        public List<T> GetAllEntities<T>(string query, Func<SqliteDataReader, T> map, Dictionary<string, object>? parameters = null)
        {
            var resultList = new List<T>();
            using var conn = GetConnection();
            using var cmd = new SqliteCommand(query, conn);

            if (parameters != null)
                foreach (var kv in parameters)
                    cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                resultList.Add(map(reader));

            return resultList;
        }

        public bool UpdateEntity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(T entity)
        {
            if (entity == null) return false;

            var type = typeof(T);
            var tableName = type.Name;

            // Lọc các property public, readable, và không bị [NotMapped]
            var (allProps, keyProp) = GetCachedTypeInfo(type);
            var props = allProps.Where(p => p.CanRead).ToList();

            if (keyProp == null)
                throw new InvalidOperationException($"Type '{type.Name}' is missing [SqlKey] property.");

            var keyValue = keyProp.GetValue(entity);
            if (keyValue == null)
                throw new InvalidOperationException("Primary key value cannot be null.");

            // Tạo danh sách set và parameter
            var setClauses = new List<string>();
            var parameters = new List<SqliteParameter>();

            foreach (var prop in props)
            {
                if (prop == keyProp) continue;

                var value = prop.GetValue(entity);
                var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                // Chuyển Guid và enum sang string hoặc int tương ứng
                if (value is Guid guid) value = guid.ToString();
                else if (propType.IsEnum) value = (int)value;

                string paramName = $"@{prop.Name}";
                setClauses.Add($"{prop.Name} = {paramName}");
                parameters.Add(new SqliteParameter(paramName, value ?? DBNull.Value));
            }

            // Add parameter khóa chính
            parameters.Add(new SqliteParameter("@Id", keyValue is Guid g ? g.ToString() : keyValue));

            string sql = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {keyProp.Name} = @Id;";

            using var conn = GetConnection();
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());

            return cmd.ExecuteNonQuery() > 0;
        }

        public bool ExecuteNonQuery(string sql, Dictionary<string, object>? parameters = null)
        {
            using var conn = GetConnection();
            using var cmd = new SqliteCommand(sql, conn);

            if (parameters != null)
                foreach (var kv in parameters)
                    cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);

            return cmd.ExecuteNonQuery() > 0;
        }

        public bool InsertEntities<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(List<T> entities)
        {
            if (entities == null || entities.Count == 0)
                return false;

            var type = typeof(T);
            var tableName = type.Name;
            var (props, keyProp) = GetCachedTypeInfo(type);

            // Cột cố định: lọc 1 lần các property kiểu đơn giản để có thể prepare câu lệnh & tái dùng.
            var cols = new List<PropertyInfo>();
            foreach (var prop in props)
            {
                var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (propType.IsPrimitive || propType == typeof(string) || propType == typeof(Guid) || propType.IsEnum)
                    cols.Add(prop);
            }
            if (cols.Count == 0) return false;

            using var conn = GetConnection();

            // Tăng tốc ghi hàng loạt (50k+ dòng): WAL + giảm fsync trong phạm vi transaction.
            using (var pragma = new SqliteCommand("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;", conn))
                pragma.ExecuteNonQuery();

            using var transaction = conn.BeginTransaction();

            // Chuẩn bị câu lệnh + tham số MỘT LẦN rồi tái sử dụng cho mọi dòng.
            string sql = $"INSERT INTO {tableName} ({string.Join(",", cols.Select(c => c.Name))}) " +
                         $"VALUES ({string.Join(",", cols.Select(c => "@" + c.Name))});";

            using var cmd = new SqliteCommand(sql, conn, transaction);
            var cmdParams = new SqliteParameter[cols.Count];
            for (int i = 0; i < cols.Count; i++)
            {
                cmdParams[i] = new SqliteParameter("@" + cols[i].Name, DBNull.Value);
                cmd.Parameters.Add(cmdParams[i]);
            }
            cmd.Prepare();

            foreach (var entity in entities)
            {
                if (entity == null) continue;

                for (int i = 0; i < cols.Count; i++)
                {
                    var prop = cols[i];
                    object? value = prop.GetValue(entity);

                    // Tự tạo Guid nếu là khóa chính và rỗng
                    if (prop == keyProp && prop.PropertyType == typeof(Guid))
                    {
                        var guid = (Guid?)value ?? Guid.Empty;
                        if (guid == Guid.Empty)
                        {
                            guid = Guid.NewGuid();
                            prop.SetValue(entity, guid);
                            value = guid;
                        }
                    }

                    if (value is Guid g) value = g.ToString();

                    cmdParams[i].Value = value ?? (object)DBNull.Value;
                }

                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
            return true;
        }

        public List<T> ExecuteReader<T>(string query, Func<SqliteDataReader, T> map, Dictionary<string, object>? parameters = null)
        {
            var result = new List<T>();

            using var conn = GetConnection();
            using var cmd = new SqliteCommand(query, conn);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(map(reader));
            }

            return result;
        }
    }
}
