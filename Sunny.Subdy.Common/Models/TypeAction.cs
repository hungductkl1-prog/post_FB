namespace Sunny.Subdy.Common.Models
{
    public class TypeAction
    {
        public const string FB_SpamXu = "FB_SpamXu";
        public const string FB_Turn2FA = "FB_Turn2FA";
        public const string FB_ChangeAvatar= "FB_ChangeAvatar";
        public const string FB_ChangeCover = "FB_ChangeCover";
        public const string FB_ChangeMail = "FB_ChangeMail";
        public const string FB_RemoveMail = "FB_RemoveMail";
        public static string GetNameAction(string actionType)
        {
            return actionType switch
            {
                FB_SpamXu => "Spam Xu Facebook",
                // Add other action types here as needed
                _ => "Unknown Action"
            };
        }
    }
}
