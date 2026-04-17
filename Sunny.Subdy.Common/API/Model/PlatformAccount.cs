namespace Sunny.Subdy.Common.API.Model
{
    public class PlatformAccount
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PlatformId { get; set; }
        public string Uid { get; set; }
        public string DisplayName { get; set; }
        public bool IsActive { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
    }
}
