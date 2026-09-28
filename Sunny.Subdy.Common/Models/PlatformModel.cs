namespace Sunny.Subdy.Common.Models
{
    public class PlatformModel
    {
        public const string Facebook = "Facebook";
        public const string Instagram = "Instagram";
        public const string Threads = "Threads";
        public const string Pandora = "Pandora";
        // Module đăng ký Facebook tách riêng (kiểu Pandora). Nhãn này CHỈ dùng để
        // cách ly DB (Account.Platformt), khóa config và thư mục backup. Mọi thao tác
        // ứng dụng vẫn nhắm Facebook (com.facebook.katana) — xem RegFacebookRegsiner.
        public const string RegFacebook = "Reg Facebook";
    }
}
