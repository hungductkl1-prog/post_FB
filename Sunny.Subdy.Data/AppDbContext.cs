using Sunny.Subdy.Common.Logs;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Sunny.Subdy.Data
{
    public class AppDbContext
    {
        [AttributeUsage(AttributeTargets.Property)]
        public sealed class SqlKeyAttribute : Attribute { }

        private readonly string _connectionString;
        private readonly string _dbPath;

        public AppDbContext(string databaseName)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dataDir = Path.Combine(baseDir, "data");
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);

            _dbPath = Path.Combine(dataDir, $"{databaseName}.db");
            _connectionString = $"Data Source={_dbPath};Version=3;";
            EnsureDatabaseFile();
        }

        private void EnsureDatabaseFile()
        {
            if (!File.Exists(_dbPath))
                SQLiteConnection.CreateFile(_dbPath);
        }
        public object ExecuteScalar(string query, Dictionary<string, object>? parameters = null)
        {
            using var conn = GetConnection();
            using var cmd = new SQLiteCommand(query, conn);

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

        public SQLiteConnection GetConnection()
        {
            var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        public void EnsureTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
        {
            var type = typeof(T);
            var tableName = type.Name;
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            using var conn = GetConnection();

            // Check existing table
            var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = new SQLiteCommand($"PRAGMA table_info({tableName})", conn))
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
                using var createCmd = new SQLiteCommand(createSql, conn);
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
                    using var alterCmd = new SQLiteCommand(alterSql, conn);
                    alterCmd.ExecuteNonQuery();
                }
            }
        }
        public bool UpdateEntities<T>(List<T> entities)
        {
            if (entities == null || entities.Count == 0)
                return false;

            var type = typeof(T);
            var tableName = type.Name;
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var keyProp = props.FirstOrDefault(p => p.GetCustomAttribute<SqlKeyAttribute>() != null);

            if (keyProp == null)
                throw new InvalidOperationException($"Type '{type.Name}' does not contain a property marked with [SqlKey].");

            using var conn = GetConnection();
            using var transaction = conn.BeginTransaction();

            foreach (var entity in entities)
            {
                if (entity == null) continue;

                var setClauses = new List<string>();
                var parameters = new List<SQLiteParameter>();

                foreach (var prop in props)
                {
                    if (prop == keyProp) continue;

                    object? value = prop.GetValue(entity);
                    if (value is Guid g) value = g.ToString();

                    string paramName = $"@{prop.Name}";
                    setClauses.Add($"{prop.Name} = {paramName}");
                    parameters.Add(new SQLiteParameter(paramName, value ?? DBNull.Value));
                }

                var keyValue = keyProp.GetValue(entity);
                if (keyValue == null)
                    throw new InvalidOperationException("Primary key value cannot be null.");

                if (keyValue is Guid kg) keyValue = kg.ToString();
                parameters.Add(new SQLiteParameter("@Id", keyValue));

                string sql = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {keyProp.Name} = @Id;";
                using var cmd = new SQLiteCommand(sql, conn, transaction);
                cmd.Parameters.AddRange(parameters.ToArray());
                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
            return true;
        }
        public bool InsertEntity<T>(T entity)
        {
            if (entity == null) return false;

            var type = typeof(T);
            var tableName = type.Name;
            var props = type.GetProperties();

            var columnNames = new List<string>();
            var paramNames = new List<string>();
            var parameters = new List<SQLiteParameter>();

            foreach (var prop in props)
            {
                var value = prop.GetValue(entity);
                if (value == null) continue;

                // Lọc chỉ lấy các kiểu đơn giản: primitive, string, Guid
                var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (!(propType.IsPrimitive || propType == typeof(string) || propType == typeof(Guid) || propType.IsEnum))
                    continue;

                if (value is Guid g)
                    value = g.ToString();

                columnNames.Add(prop.Name);
                string paramName = $"@{prop.Name}";
                paramNames.Add(paramName);
                parameters.Add(new SQLiteParameter(paramName, value));
            }

            if (columnNames.Count == 0) return false;

            string sql = $"INSERT INTO {tableName} ({string.Join(",", columnNames)}) VALUES ({string.Join(",", paramNames)});";

            using var conn = GetConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());
            return cmd.ExecuteNonQuery() > 0;
        }

        public List<T> GetAllEntities<T>(string query, Func<SQLiteDataReader, T> map, Dictionary<string, object>? parameters = null)
        {
            var resultList = new List<T>();
            using var conn = GetConnection();
            using var cmd = new SQLiteCommand(query, conn);

            if (parameters != null)
                foreach (var kv in parameters)
                    cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                resultList.Add(map(reader));

            return resultList;
        }

        public bool UpdateEntity<T>(T entity)
        {
            if (entity == null) return false;

            var type = typeof(T);
            var tableName = type.Name;

            // Lọc các property public, readable, và không bị [NotMapped]
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.CanRead && p.GetCustomAttribute<NotMappedAttribute>() == null)
                            .ToList();

            // Tìm khóa chính
            var keyProp = props.FirstOrDefault(p => p.GetCustomAttribute<SqlKeyAttribute>() != null);
            if (keyProp == null)
                throw new InvalidOperationException($"Type '{type.Name}' is missing [SqlKey] property.");

            var keyValue = keyProp.GetValue(entity);
            if (keyValue == null)
                throw new InvalidOperationException("Primary key value cannot be null.");

            // Tạo danh sách set và parameter
            var setClauses = new List<string>();
            var parameters = new List<SQLiteParameter>();

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
                parameters.Add(new SQLiteParameter(paramName, value ?? DBNull.Value));
            }

            // Add parameter khóa chính
            parameters.Add(new SQLiteParameter("@Id", keyValue is Guid g ? g.ToString() : keyValue));

            string sql = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {keyProp.Name} = @Id;";

            using var conn = GetConnection();
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());

            return cmd.ExecuteNonQuery() > 0;
        }

        public bool ExecuteNonQuery(string sql, Dictionary<string, object>? parameters = null)
        {
            using var conn = GetConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            if (parameters != null)
                foreach (var kv in parameters)
                    cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);

            return cmd.ExecuteNonQuery() > 0;
        }
        public bool InsertEntities<T>(List<T> entities)
        {
            if (entities == null || entities.Count == 0)
                return false;

            var type = typeof(T);
            var tableName = type.Name;
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var keyProp = props.FirstOrDefault(p => p.GetCustomAttribute<SqlKeyAttribute>() != null);

            using var conn = GetConnection();
            using var transaction = conn.BeginTransaction();

            foreach (var entity in entities)
            {
                if (entity == null) continue;

                var columnNames = new List<string>();
                var paramNames = new List<string>();
                var parameters = new List<SQLiteParameter>();

                foreach (var prop in props)
                {
                    // Bỏ qua nếu không phải kiểu đơn giản
                    var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    if (!(propType.IsPrimitive || propType == typeof(string) || propType == typeof(Guid) || propType.IsEnum))
                        continue;

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

                    if (value == null) continue;
                    if (value is Guid g) value = g.ToString();

                    string name = prop.Name;
                    string param = $"@{name}";

                    columnNames.Add(name);
                    paramNames.Add(param);
                    parameters.Add(new SQLiteParameter(param, value));
                }

                if (columnNames.Count == 0) continue;

                string sql = $"INSERT INTO {tableName} ({string.Join(",", columnNames)}) VALUES ({string.Join(",", paramNames)});";
                using var cmd = new SQLiteCommand(sql, conn, transaction);
                cmd.Parameters.AddRange(parameters.ToArray());
                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
            return true;
        }

        public List<T> ExecuteReader<T>(string query, Func<SQLiteDataReader, T> map, Dictionary<string, object>? parameters = null)
        {
            var result = new List<T>();

            using var conn = GetConnection();
            using var cmd = new SQLiteCommand(query, conn);

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
