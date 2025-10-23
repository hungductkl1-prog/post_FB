using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Data.Models;

namespace Sunny.Subd.Core.Models
{
    public class ConfigModel
    {
        public JsonHelper SettingGeneral { get; set; }
        public JsonHelper SettingJob { get; set; }
        public string JobService { get; set; }
        public string Platform { get; set; }
        public SortableBindingList<Account> Accounts { get; set; }

    }
}
