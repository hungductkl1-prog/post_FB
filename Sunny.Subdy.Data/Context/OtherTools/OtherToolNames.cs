namespace Sunny.Subdy.Data.Context.OtherTools
{
    public static class OtherToolNames
    {
        public const string MaxCare = "MaxCare";
        public const string FPlus = "FPlus";
        public const string MetaMax = "MetaMax";
    }

    public class OtherToolAccount
    {
        public string Uid { get; set; } = "";
        public string Password { get; set; } = "";
        public string TwoFA { get; set; } = "";
        public string Cookie { get; set; } = "";
        public string Token { get; set; } = "";
        public string Proxy { get; set; } = "";
        public string Email { get; set; } = "";
        public string PassMail { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public string FolderName { get; set; } = "";
    }
}
