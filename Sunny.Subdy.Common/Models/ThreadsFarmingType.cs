namespace Sunny.Subdy.Common.Models
{
    public class ThreadsFarmingType
    {
        public const string TRXemReel = "TRXemReel";
        public const string TRXemStory = "TRXemStory";
        public const string TRTuongTacNewfeed = "TRTuongTacNewfeed";
        public const string TRDangBai = "TRDangBai";
        public const string TRDangReel = "TRDangReel";
        public const string TRDangStory = "TRDangStory";
        public const string TRFollow = "TRFollow";
        public const string TRUnfollow = "TRUnfollow";
        public const string TRNhanTin = "TRNhanTin";
        public const string TRCapNhatThongTin = "TRCapNhatThongTin";

        public readonly static Dictionary<string, string> DictionariesAction = new Dictionary<string, string>
        {
            { TRXemReel, "Xem reel" },
            { TRXemStory, "Xem story" },
            { TRTuongTacNewfeed, "Tương tác newfeed" },
            { TRDangBai, "Đăng bài" },
            { TRDangReel, "Đăng reel" },
            { TRDangStory, "Đăng story" },
            { TRFollow, "Follow" },
            { TRUnfollow, "Bỏ follow" },
            { TRNhanTin, "Nhắn tin (Direct Message)" },
            { TRCapNhatThongTin, "Cập nhật thông tin (Avatar/Bio)" },
        };

        public readonly static Dictionary<string, string> DescriptionAction = new Dictionary<string, string>
        {
            { TRXemReel, "Lướt và xem reel ngẫu nhiên trên Threads." },
            { TRXemStory, "Xem story của người mà tài khoản đang follow." },
            { TRTuongTacNewfeed, "Tương tác newfeed: like, comment, lưu bài." },
            { TRDangBai, "Đăng bài viết kèm ảnh/video lên trang cá nhân." },
            { TRDangReel, "Đăng reel kèm caption và hashtag." },
            { TRDangStory, "Đăng story (ảnh/video) lên Threads." },
            { TRFollow, "Follow tài khoản theo UID, username hoặc keyword gợi ý." },
            { TRUnfollow, "Bỏ follow tài khoản đang theo dõi theo bộ lọc." },
            { TRNhanTin, "Gửi tin nhắn Direct Message tới danh sách user." },
            { TRCapNhatThongTin, "Cập nhật avatar, bio, tên hiển thị tài khoản." },
        };
    }
}
