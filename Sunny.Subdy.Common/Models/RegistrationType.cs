namespace Sunny.Subdy.Common.Models
{
    public class RegistrationType
    {
        public const string Domain = "Domain";
        public const string Gmail = "Gmail";
        public const string PhoneNumber = "Số điện thoại";
        public const string Domain_BaitPhoneNumber = "Domain mồi số điện thoại";
        public const string Gmail_BaitPhoneNumber = "Gmail mồi số điện thoại";
        public static List<string> RegFacebook_AllTypes = new List<string>
        {
            Domain,
            Gmail,
            PhoneNumber,
            Domain_BaitPhoneNumber,
            Gmail_BaitPhoneNumber,
        };


        public const string Domain_TempMail = "https://temp-mail.io/";
        public const string Domain_Getnada = "https://inboxes.com";
        public const string Domain_MailTM = "https://mail.tm/";
        public const string Domain_Shopgmail9999 = "https://api.shopgmail9999.com/";
        public const string Domain_TheLoi = "https://mail.theloi.io.vn/";
        public static List<string> EmailTypes = new List<string>
        {
            Domain_TempMail,
            Domain_Getnada,
            Domain_MailTM,
            Domain_TheLoi,
            Domain_Shopgmail9999,
        };

        public const string IronSim = "https://ironsim.com/";
        public const string FunOTP = "https://funotp.com";
        public static List<string> PhoneNumberTypes = new List<string>
        {
            IronSim,
            FunOTP,
        };

    }
}
