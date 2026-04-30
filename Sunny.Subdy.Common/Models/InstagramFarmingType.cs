namespace Sunny.Subdy.Common.Models
{
    public class InstagramFarmingType
    {
        public const string IGXemReel = "IGXemReel";
        public const string IGXemStory = "IGXemStory";
        public const string IGTuongTacNewfeed = "IGTuongTacNewfeed";
        public const string IGDangBai = "IGDangBai";
        public const string IGDangReel = "IGDangReel";
        public const string IGDangStory = "IGDangStory";
        public const string IGFollow = "IGFollow";
        public const string IGUnfollow = "IGUnfollow";
        public const string IGNhanTin = "IGNhanTin";
        public const string IGCapNhatThongTin = "IGCapNhatThongTin";

        public readonly static Dictionary<string, string> DictionariesAction = new Dictionary<string, string>
        {
            { IGXemReel, "Xem reel" },
            { IGXemStory, "Xem story" },
            { IGTuongTacNewfeed, "Tương tác newfeed" },
            { IGDangBai, "Đăng bài" },
            { IGDangReel, "Đăng reel" },
            { IGDangStory, "Đăng story" },
            { IGFollow, "Follow" },
            { IGUnfollow, "Bỏ follow" },
            { IGNhanTin, "Nhắn tin (Direct Message)" },
            { IGCapNhatThongTin, "Cập nhật thông tin (Avatar/Bio)" },
        };

        public readonly static Dictionary<string, string> DescriptionAction = new Dictionary<string, string>
        {
            { IGXemReel, "Lướt và xem reel ngẫu nhiên trên Instagram." },
            { IGXemStory, "Xem story của người mà tài khoản đang follow." },
            { IGTuongTacNewfeed, "Tương tác newfeed: like, comment, lưu bài." },
            { IGDangBai, "Đăng bài viết kèm ảnh/video lên trang cá nhân." },
            { IGDangReel, "Đăng reel kèm caption và hashtag." },
            { IGDangStory, "Đăng story (ảnh/video) lên Instagram." },
            { IGFollow, "Follow tài khoản theo UID, username hoặc keyword gợi ý." },
            { IGUnfollow, "Bỏ follow tài khoản đang theo dõi theo bộ lọc." },
            { IGNhanTin, "Gửi tin nhắn Direct Message tới danh sách user." },
            { IGCapNhatThongTin, "Cập nhật avatar, bio, tên hiển thị tài khoản." },
        };
    }
}
