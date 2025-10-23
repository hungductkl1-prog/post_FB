using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunny.Subdy.Data.Models
{
    public class SettingDefault
    {
        [AppDbContext.SqlKey]
        public Guid Id { get; set; }
        public string Platform { get; set; }
        public string Name { get; set; } = "";
        public string Json { get; set; } = "";
    }
}
