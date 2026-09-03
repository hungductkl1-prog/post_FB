using Sunny.Subdy.Data.Models;

namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    /// <summary>
    /// In-memory cache helpers: search/filter on loaded scope without hitting DB per keystroke.
    /// </summary>
    public static class AccountListCache
    {
        public static List<Account> FilterBySearch(IReadOnlyList<Account> source, string? searchTerm)
        {
            if (source.Count == 0)
                return new List<Account>();

            if (string.IsNullOrWhiteSpace(searchTerm))
                return source is List<Account> list ? list : source.ToList();

            searchTerm = searchTerm.Trim();
            var result = new List<Account>(Math.Min(source.Count, 4096));
            for (int i = 0; i < source.Count; i++)
            {
                if (MatchesSearch(source[i], searchTerm))
                    result.Add(source[i]);
            }
            return result;
        }

        public static void ReindexStt(IList<Account> accounts, int start = 1)
        {
            for (int i = 0; i < accounts.Count; i++)
                accounts[i].STT = start + i;
        }

        private static bool MatchesSearch(Account account, string term)
        {
            return Contains(account.Uid, term)
                || Contains(account.FullName, term)
                || Contains(account.Note, term)
                || Contains(account.Status, term)
                || Contains(account.NameScript, term)
                || Contains(account.TokenJob, term);
        }

        private static bool Contains(string? value, string term)
            => !string.IsNullOrEmpty(value)
               && value.Contains(term, StringComparison.OrdinalIgnoreCase);
    }
}
