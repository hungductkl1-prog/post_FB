namespace Sunny.Subdy.Data.Models
{
    public class FormatAccount
    {
        [AppDbContext.SqlKey]
        public Guid Id { get; set; }
        public string? Name { get; set; } = "";
        public string? Fields { get; set; } = "";
    }
}
