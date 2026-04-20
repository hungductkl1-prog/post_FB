using Microsoft.Data.Sqlite;

namespace Sunny.Subdy.Data.Context.OtherTools
{
    public class MaxCareReader
    {
        private readonly string _connectionString;
        public string DbPath { get; }

        public MaxCareReader(string folder)
        {
            DbPath = Path.Combine(folder, "database", "db_maxcare.sqlite");
            _connectionString = $"Data Source={DbPath}";
        }

        public bool Exists() => File.Exists(DbPath);

        public List<string> GetFolders()
        {
            var list = new List<string>();
            if (!Exists()) return list;
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM files ORDER BY name";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var name = reader["name"]?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(name)) list.Add(name);
            }
            return list;
        }

        public List<OtherToolAccount> GetAccountsByFolder(string folderName)
        {
            var list = new List<OtherToolAccount>();
            if (!Exists()) return list;
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            int? id = null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT id FROM files WHERE name = $name LIMIT 1";
                cmd.Parameters.AddWithValue("$name", folderName);
                var v = cmd.ExecuteScalar();
                if (v != null && int.TryParse(v.ToString(), out var parsed)) id = parsed;
            }
            if (id == null) return list;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT uid, pass, fa2, cookie1, token, proxy, email, passmail, useragent FROM accounts WHERE idfile = $id";
                cmd.Parameters.AddWithValue("$id", id.Value);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new OtherToolAccount
                    {
                        Uid = reader["uid"]?.ToString() ?? "",
                        Password = reader["pass"]?.ToString() ?? "",
                        TwoFA = reader["fa2"]?.ToString() ?? "",
                        Cookie = NormalizeCookie(reader["cookie1"]?.ToString() ?? ""),
                        Token = reader["token"]?.ToString() ?? "",
                        Proxy = NormalizeProxy(reader["proxy"]?.ToString() ?? ""),
                        Email = reader["email"]?.ToString() ?? "",
                        PassMail = reader["passmail"]?.ToString() ?? "",
                        UserAgent = reader["useragent"]?.ToString() ?? "",
                        FolderName = folderName,
                    });
                }
            }
            return list;
        }

        public OtherToolAccount? GetAccountByUid(string uid)
        {
            if (!Exists()) return null;
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT uid, pass, fa2, cookie1, token, proxy, email, passmail, useragent FROM accounts WHERE uid = $uid LIMIT 1";
            cmd.Parameters.AddWithValue("$uid", uid);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;
            return new OtherToolAccount
            {
                Uid = reader["uid"]?.ToString() ?? "",
                Password = reader["pass"]?.ToString() ?? "",
                TwoFA = reader["fa2"]?.ToString() ?? "",
                Cookie = NormalizeCookie(reader["cookie1"]?.ToString() ?? ""),
                Token = reader["token"]?.ToString() ?? "",
                Proxy = NormalizeProxy(reader["proxy"]?.ToString() ?? ""),
                Email = reader["email"]?.ToString() ?? "",
                PassMail = reader["passmail"]?.ToString() ?? "",
                UserAgent = reader["useragent"]?.ToString() ?? "",
            };
        }

        private static string NormalizeProxy(string proxy) =>
            string.IsNullOrEmpty(proxy) ? "" : (proxy.EndsWith("*0") ? proxy.Replace("*0", "") : proxy);

        private static string NormalizeCookie(string cookie) =>
            string.IsNullOrEmpty(cookie) ? "" : System.Net.WebUtility.UrlDecode(cookie);
    }
}
