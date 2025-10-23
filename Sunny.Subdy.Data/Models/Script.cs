namespace Sunny.Subdy.Data.Models
{
    public class Script
    {
        [AppDbContext.SqlKey]
        public Guid Id { get; set; }
        public string Platform { get; set; }
        public string Name { get; set; } = "";
        public string DateCreate { get; set; } = "";
    }
}
