using Microsoft.Data.Sqlite;

namespace Sunny.Subdy.Data.Context.OtherTools
{
    public class MetaMaxReader
    {
        private readonly string _connectionString;
        public string DbPath { get; }

        public MetaMaxReader(string folder)
        {
            DbPath = Path.Combine(folder, "DATA", "database.db");
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
            cmd.CommandText = "SELECT Name FROM folders ORDER BY Name";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var name = reader["Name"]?.ToString() ?? "";
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
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT UID, Password, TwoFA, Cookie, Token, Proxy, Email, PassMail, UserAgent FROM accounts WHERE Folder = $folder";
            cmd.Parameters.AddWithValue("$folder", folderName);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new OtherToolAccount
                {
                    Uid = reader["UID"]?.ToString() ?? "",
                    Password = reader["Password"]?.ToString() ?? "",
                    TwoFA = reader["TwoFA"]?.ToString() ?? "",
                    Cookie = reader["Cookie"]?.ToString() ?? "",
                    Token = reader["Token"]?.ToString() ?? "",
                    Proxy = NormalizeProxy(reader["Proxy"]?.ToString() ?? ""),
                    Email = reader["Email"]?.ToString() ?? "",
                    PassMail = reader["PassMail"]?.ToString() ?? "",
                    UserAgent = reader["UserAgent"]?.ToString() ?? "",
                    FolderName = folderName,
                });
            }
            return list;
        }

        public OtherToolAccount? GetAccountByUid(string uid)
        {
            if (!Exists()) return null;
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT UID, Password, TwoFA, Cookie, Token, Proxy, Email, PassMail, UserAgent FROM accounts WHERE UID = $uid LIMIT 1";
            cmd.Parameters.AddWithValue("$uid", uid);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;
            return new OtherToolAccount
            {
                Uid = reader["UID"]?.ToString() ?? "",
                Password = reader["Password"]?.ToString() ?? "",
                TwoFA = reader["TwoFA"]?.ToString() ?? "",
                Cookie = reader["Cookie"]?.ToString() ?? "",
                Token = reader["Token"]?.ToString() ?? "",
                Proxy = NormalizeProxy(reader["Proxy"]?.ToString() ?? ""),
                Email = reader["Email"]?.ToString() ?? "",
                PassMail = reader["PassMail"]?.ToString() ?? "",
                UserAgent = reader["UserAgent"]?.ToString() ?? "",
            };
        }

        private static string NormalizeProxy(string proxy) =>
            string.IsNullOrEmpty(proxy) ? "" : (proxy.EndsWith("*0") ? proxy.Replace("*0", "") : proxy);
    }
}
