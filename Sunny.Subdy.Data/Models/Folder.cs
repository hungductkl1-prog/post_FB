namespace Sunny.Subdy.Data.Models
{
    public class Folder
    {
        [AppDbContext.SqlKey]
        public Guid Id { get; set; }
        public string? Name { get; set; } = "";
        public string? DateCreate { get; set; } = "";
        public string? Count { get; set; } = "";
        public string? Type { get; set; } = "";
        public bool IsView { get; set; } = true;
    }
}
