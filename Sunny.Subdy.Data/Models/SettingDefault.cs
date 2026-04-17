using System.Diagnostics.CodeAnalysis;

namespace Sunny.Subdy.Data.Models
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class SettingDefault
    {
        [AppDbContext.SqlKey]
        public Guid Id { get; set; }
        public string Platform { get; set; }
        public string Name { get; set; } = "";
        public string Json { get; set; } = "";
    }
}
