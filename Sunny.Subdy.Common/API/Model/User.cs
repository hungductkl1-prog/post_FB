namespace Sunny.Subdy.Common.API.Model
{
    public class User
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
        public string ApiKey { get; set; }
        public double Balance { get; set; }
        public double PendingBalance { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsBanned { get; set; }
        public string Role { get; set; }
        public string? Token_QN { get; set; }
    }
}
