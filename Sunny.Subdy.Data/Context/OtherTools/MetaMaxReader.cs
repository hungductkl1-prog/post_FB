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

        // Cot mong muon trong bang "accounts" cua MetaMax. Doc phong thu theo cot
        // thuc co (PRAGMA) de tuong thich nhieu phien ban database.db.
        private static readonly string[] DesiredColumns =
        {
            "UID","Password","TwoFA","Cookie","Token","Proxy","Email","PassMail",
            "MailRecovery","UserAgent","FullName","Name","Phone","Birthday","Gender",
            "Avatar","Bio","Info","Friends","Groups","Follow","Status","Device",
            "DateCreate","Note","RecentInteraction"
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
            string fullName = S(r, m, "FullName");
            if (string.IsNullOrEmpty(fullName)) fullName = S(r, m, "Name");

            string bio = S(r, m, "Bio");
            if (string.IsNullOrEmpty(bio)) bio = S(r, m, "Info");

            return new OtherToolAccount
            {
                Uid = S(r, m, "UID"),
                Password = S(r, m, "Password"),
                TwoFA = S(r, m, "TwoFA"),
                Cookie = S(r, m, "Cookie"),
                Token = S(r, m, "Token"),
                Proxy = NormalizeProxy(S(r, m, "Proxy")),
                Email = S(r, m, "Email"),
                PassMail = S(r, m, "PassMail"),
                MailRecovery = S(r, m, "MailRecovery"),
                UserAgent = S(r, m, "UserAgent"),
                FullName = fullName,
                Phone = S(r, m, "Phone"),
                Birthday = S(r, m, "Birthday"),
                Gender = S(r, m, "Gender"),
                Avatar = S(r, m, "Avatar"),
                Bio = bio,
                Friends = S(r, m, "Friends"),
                Groups = S(r, m, "Groups"),
                Follow = S(r, m, "Follow"),
                Status = S(r, m, "Status"),
                Device = S(r, m, "Device"),
                DateCreate = S(r, m, "DateCreate"),
                Note = S(r, m, "Note"),
                RecentInteraction = S(r, m, "RecentInteraction"),
                FolderName = folderName,
            };
        }

        public List<OtherToolAccount> GetAccountsByFolder(string folderName)
        {
            var list = new List<OtherToolAccount>();
            if (!Exists()) return list;
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var available = GetAccountColumns(conn);
            string select = BuildSelectColumns(available);
            if (string.IsNullOrEmpty(select)) return list;

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT {select} FROM accounts WHERE Folder = $folder";
            cmd.Parameters.AddWithValue("$folder", folderName);
            using var reader = cmd.ExecuteReader();
            var m = OrdMap(reader);
            while (reader.Read())
                list.Add(MapRow(reader, m, folderName));
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
            cmd.CommandText = $"SELECT {select} FROM accounts WHERE UID = $uid LIMIT 1";
            cmd.Parameters.AddWithValue("$uid", uid);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;
            var m = OrdMap(reader);
            return MapRow(reader, m, "");
        }

        private static string NormalizeProxy(string proxy) =>
            string.IsNullOrEmpty(proxy) ? "" : (proxy.EndsWith("*0") ? proxy.Replace("*0", "") : proxy);
    }
}
