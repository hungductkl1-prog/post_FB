using System.Net;
using System.Xml;

namespace Sunny.Subdy.Data.Context.OtherTools
{
    public class FPlusReader
    {
        public string Folder { get; }
        public string AccountsPath => Path.Combine(Folder, "lstaccountcookies.txt");
        public string CategoriesPath => Path.Combine(Folder, "list_category_manager.xml");

        public FPlusReader(string folder)
        {
            Folder = folder;
        }

        public bool Exists() => File.Exists(AccountsPath) && File.Exists(CategoriesPath);

        public List<string> GetFolders()
        {
            var list = new List<string>();
            if (!File.Exists(CategoriesPath)) return list;
            try
            {
                var doc = new XmlDocument();
                doc.Load(CategoriesPath);
                var nodes = doc.SelectNodes("//CategoryName");
                if (nodes == null) return list;
                foreach (XmlNode node in nodes)
                {
                    var name = node?.InnerText;
                    if (!string.IsNullOrWhiteSpace(name)) list.Add(name!);
                }
            }
            catch { }
            return list;
        }

        public List<OtherToolAccount> GetAllAccounts()
        {
            var list = new List<OtherToolAccount>();
            if (!File.Exists(AccountsPath)) return list;
            string[] lines;
            try { lines = File.ReadAllLines(AccountsPath); }
            catch { return list; }
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split('|');
                if (parts.Length == 0) continue;
                var acc = new OtherToolAccount
                {
                    Uid = parts[0],
                    Password = parts.Length > 1 ? WebUtility.UrlDecode(parts[1]) : "",
                    Token = parts.Length > 4 ? parts[4] : "",
                    Cookie = parts.Length > 5 ? WebUtility.UrlDecode(parts[5]) : "",
                    Proxy = parts.Length > 10 ? WebUtility.UrlDecode(parts[10]) : "",
                    FolderName = parts.Length > 25 ? SafeUnescape(parts[25]) : "",
                    TwoFA = parts.Length > 52 ? parts[52] : "",
                    Email = parts.Length > 55 ? parts[55] : "",
                    PassMail = parts.Length > 57 ? WebUtility.UrlDecode(parts[57]) : "",
                };
                acc.Proxy = NormalizeProxy(acc.Proxy);
                list.Add(acc);
            }
            return list;
        }

        public List<OtherToolAccount> GetAccountsByFolder(string folderName)
        {
            var encoded = Uri.EscapeDataString(folderName);
            return GetAllAccounts()
                .Where(a => string.Equals(a.FolderName, folderName, StringComparison.Ordinal)
                            || string.Equals(a.FolderName, encoded, StringComparison.Ordinal))
                .ToList();
        }

        private static string SafeUnescape(string value)
        {
            try { return Uri.UnescapeDataString(value); }
            catch { return value; }
        }

        public OtherToolAccount? GetAccountByUid(string uid)
            => GetAllAccounts().FirstOrDefault(x => x.Uid == uid);

        private static string NormalizeProxy(string proxy) =>
            string.IsNullOrEmpty(proxy) ? "" : (proxy.EndsWith("*0") ? proxy.Replace("*0", "") : proxy);
    }
}
