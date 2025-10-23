namespace Sunny.Subdy.Data.Models
{
    public class ApiType
    {
        [AppDbContext.SqlKey]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
