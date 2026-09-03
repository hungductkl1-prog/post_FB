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

        // Tap cot mong muon trong bang "accounts" cua MaxPhoneFarm. Doc phong thu
        // theo cot thuc co de tuong thich nhieu phien ban db_maxcare.sqlite.
        private static readonly string[] DesiredColumns =
        {
            "uid","pass","fa2","cookie","cookie1","token","proxy","email","passmail",
            "mailrecovery","useragent","name","phone","birthday","gender","avatar",
            "info","friends","groups","follow","status","device","dateCreateAcc",
            "dateImport","ghiChu","interactEnd"
        };

        private static HashSet<string> GetAccountColumns(SqliteConnection conn)
        {
            var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(accounts)";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var name = r["name"]?.ToString();
                if (!string.IsNullOrEmpty(name)) cols.Add(name);
            }
            return cols;
        }

        private static string BuildSelectColumns(HashSet<string> available)
            => string.Join(", ", DesiredColumns.Where(available.Contains));

        private static Dictionary<string, int> OrdMap(SqliteDataReader r)
        {
            var m = new Dictionary<string, int>(r.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < r.FieldCount; i++) m[r.GetName(i)] = i;
            return m;
        }

        private static string S(SqliteDataReader r, Dictionary<string, int> m, string name)
            => m.TryGetValue(name, out int i) && !r.IsDBNull(i) ? (r.GetValue(i)?.ToString() ?? "") : "";

        private static OtherToolAccount MapRow(SqliteDataReader r, Dictionary<string, int> m, string folderName)
        {
            string cookie = S(r, m, "cookie1");
            if (string.IsNullOrEmpty(cookie)) cookie = S(r, m, "cookie");

            string dateCreate = S(r, m, "dateCreateAcc");
            if (string.IsNullOrEmpty(dateCreate)) dateCreate = S(r, m, "dateImport");

            return new OtherToolAccount
            {
                Uid = S(r, m, "uid"),
                Password = S(r, m, "pass"),
                TwoFA = S(r, m, "fa2"),
                Cookie = NormalizeCookie(cookie),
                Token = S(r, m, "token"),
                Proxy = NormalizeProxy(S(r, m, "proxy")),
                Email = S(r, m, "email"),
                PassMail = S(r, m, "passmail"),
                MailRecovery = S(r, m, "mailrecovery"),
                UserAgent = S(r, m, "useragent"),
                FullName = S(r, m, "name"),
                Phone = S(r, m, "phone"),
                Birthday = S(r, m, "birthday"),
                Gender = S(r, m, "gender"),
                Avatar = S(r, m, "avatar"),
                Bio = S(r, m, "info"),
                Friends = S(r, m, "friends"),
                Groups = S(r, m, "groups"),
                Follow = S(r, m, "follow"),
                Status = S(r, m, "status"),
                Device = S(r, m, "device"),
                DateCreate = dateCreate,
                Note = S(r, m, "ghiChu"),
                RecentInteraction = S(r, m, "interactEnd"),
                FolderName = folderName,
            };
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

            var available = GetAccountColumns(conn);
            string select = BuildSelectColumns(available);
            if (string.IsNullOrEmpty(select)) return list;

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT {select} FROM accounts WHERE idfile = $id";
                cmd.Parameters.AddWithValue("$id", id.Value);
                using var reader = cmd.ExecuteReader();
                var m = OrdMap(reader);
                while (reader.Read())
                    list.Add(MapRow(reader, m, folderName));
            }
            return list;
        }

        public OtherToolAccount? GetAccountByUid(string uid)
        {
            if (!Exists()) return null;
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var available = GetAccountColumns(conn);
            string select = BuildSelectColumns(available);
            if (string.IsNullOrEmpty(select)) return null;

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT {select} FROM accounts WHERE uid = $uid LIMIT 1";
            cmd.Parameters.AddWithValue("$uid", uid);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;
            var m = OrdMap(reader);
            return MapRow(reader, m, "");
        }

        private static string NormalizeProxy(string proxy) =>
            string.IsNullOrEmpty(proxy) ? "" : (proxy.EndsWith("*0") ? proxy.Replace("*0", "") : proxy);

        private static string NormalizeCookie(string cookie) =>
            string.IsNullOrEmpty(cookie) ? "" : System.Net.WebUtility.UrlDecode(cookie);
    }
}
