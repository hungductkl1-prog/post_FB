namespace Sunny.Subdy.Data.Models
{
    public class ApiEndpoint
    {
        [AppDbContext.SqlKey]
        public long Id { get; set; }
        public long ApiTypeId { get; set; }
        public string? Name { get; set; } = "";
        public string? DateCreate { get; set; } = "";
        public string? Method { get; set; } = "";
        public string? Url { get; set; } = "";
        public string? JsonBody { get; set; } = "";
        public string? Header { get; set; } = "";
        public string? CheckKey { get; set; } = "";
        public string? CheckValue { get; set; } = "";
        public string? ResultKey { get; set; } = "";
    }
}
