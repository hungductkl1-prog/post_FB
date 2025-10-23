using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Models;

namespace Sunny.Subd.Core.Services
{
    public class AccountServices
    {
        public static List<Account> Accounts { get; set; } = new List<Account>();
        public static Account GetAccount()
        {
            Account account = null;
            if (!Accounts.Any()) return null;

            lock (Accounts)
            {
                account = Accounts.FirstOrDefault();
                Accounts.Remove(account);
            }

            return account;
        }

    }
}
