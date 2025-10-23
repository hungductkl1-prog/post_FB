namespace Sunny.Subdy.Common.Models
{
    public class FacebookFarmingType
    {
        public const string HDDocThongBao = "HDDocThongBao";
        public const string HDXemReel = "HDXemReel";
        public const string HDXemStory = "HDXemStory";
        public const string HDXemWatch = "HDXemWatch";
        public const string HDTuongTacNewfeed = "HDTuongTacNewfeed";
        public const string HDTuongTacBanBe = "HDTuongTacBanBe";
        public const string HDTuongTacNhom = "HDTuongTacNhom";
        public const string HDTuongTacPage = "HDTuongTacPage";
        public const string HDTuongTacWall = "HDTuongTacWall";
        public const string HDTuongTacEvent = "HDTuongTacEvent";
        public const string HDTuongTacBaiViet = "HDTuongTacBaiViet";
        public const string HDTuongTacVideoLivestream = "HDTuongTacVideoLivestream";
        public const string HDGuiLoiMoiKetBan = "HDGuiLoiMoiKetBan";
        public const string HDXacNhanKetBan = "HDXacNhanKetBan";
        public const string HDHuyKetBan = "HDHuyKetBan"; 
        public const string HDThamGiaNhom = "HDThamGiaNhom";
        public const string HDRoiNhom = "HDRoiNhom";
        public const string HDTaoNhom = "HDTaoNhom";
        public const string HDTaoPage = "HDTaoPage";
        public const string HDDangBaiTuong = "HDDangBaiTuong";
        public const string HDDangBaiNhom = "HDDangBaiNhom";
        public const string HDDangBaiPage = "HDDangBaiPage";
        public const string HDShareBaiNangCao = "HDShareBaiNangCao";
        public const string HDDangReel = "HDDangReel";
        public const string HDDangStory = "HDDangStory";
        public const string HDDanhGiaPage = "HDDanhGiaPage";
        public const string HDBuffLikePage = "HDBuffLikePage";
        public const string HDBuffFollowUID = "HDBuffFollowUID";
        public const string HDMoiBanBeLikePage = "HDMoiBanBeLikePage";
        public const string HDMoiBanBeVaoNhom = "HDMoiBanBeVaoNhom";
        public const string HDDoiTen = "HDDoiTen";
        public const string HDDoiMatKhau = "HDDoiMatKhau";
        public const string HDUpAvatar = "HDUpAvatar";
        public const string HDUpCover = "HDUpCover";
        public const string HDOnOff2FA = "HDOnOff2FA";
        public const string HDXoaSdt = "HDXoaSdt";
        public const string HDAddMail = "HDAddMail";
        public const string HDCapNhatThongTin = "HDCapNhatThongTin";
        public const string HDDangXuatThietBiCu = "HDDangXuatThietBiCu";
        public const string HDXoaThietBiTinCay = "HDXoaThietBiTinCay";
        public const string HDBatCheDoChuyenNghiep = "HDBatCheDoChuyenNghiep";
        public const string HDNghiGiaiLao = "HDNghiGiaiLao";
        public const string HDDongBoDanhBa = "HDDongBoDanhBa";
        public const string HDTimKiemGoogle = "HDTimKiemGoogle";
        public const string HDNhanTinBanBe = "HDNhanTinBanBe";
        public readonly static Dictionary<string, string> DictionariesAction = new Dictionary<string, string>
        {
             { HDDocThongBao, "Đọc thông báo" },
             { HDXemReel, "Tương tác reel" },
             { HDXemStory, "Tương tác story" },
             { HDXemWatch, "Tương tác watch" },
             { HDTuongTacNewfeed, "Tương tác newfeed" },
             { HDTuongTacBanBe, "Tương tác bạn bè" },
             { HDTuongTacNhom, "Tương tác nhóm" },
             { HDTuongTacPage, "Tương tác page" },
             { HDTuongTacWall, "Tương tác wall" },
             { HDTuongTacEvent, "Tương tác event" },
             { HDTuongTacBaiViet, "Tương tác bài viết" },
             { HDTuongTacVideoLivestream, "Tương tác video - livestream" },
             { HDGuiLoiMoiKetBan, "Gửi lời mời kết bạn" },
             { HDXacNhanKetBan, "Xác nhận kết bạn" },
             { HDHuyKetBan, "Hủy kết bạn" },
             { HDThamGiaNhom, "Tham gia nhóm" },
             { HDRoiNhom, "Rời nhóm" },
             { HDTaoNhom, "Tạo nhóm" },
             { HDTaoPage, "Tạo page hoặc profile" },
             { HDDangBaiTuong, "Đăng bài lên tường" },
             { HDDangBaiNhom, "Đăng bài lên nhóm" },
             { HDDangBaiPage, "Đăng bài lên page" },
             { HDShareBaiNangCao, "Share bài" },
             { HDDangReel, "Đăng reel" },
             { HDDangStory, "Đăng story" },
             { HDDanhGiaPage, "Đánh giá page" },
             { HDBuffLikePage, "Buff like page" },
             { HDBuffFollowUID, "Buff follow" },
             { HDMoiBanBeLikePage, "Mời bạn bè like page" },
             { HDMoiBanBeVaoNhom, "Mời bạn bè vào nhóm" },
             { HDDoiTen, "Đổi tên" },
             { HDDoiMatKhau, "Đổi mật khẩu" },
             { HDUpAvatar, "Cập nhật ảnh avatar" },
             { HDUpCover, "Cập nhật ảnh bìa" },
             { HDOnOff2FA, "Bật - tắt 2FA" },
             { HDXoaSdt, "Xóa số điện thoại" },
             { HDAddMail, "Thêm - xóa email" },
             { HDCapNhatThongTin, "Cập nhật thông tin" },
             { HDDangXuatThietBiCu, "Đăng xuất thiết bị cũ" },
             { HDXoaThietBiTinCay, "Xóa thiết bị tin cậy" },
             { HDBatCheDoChuyenNghiep, "Bật chế độ chuyên nghiệp" },
             { HDNghiGiaiLao, "Nghỉ giải lao" },
             { HDDongBoDanhBa, "Đồng bộ danh bạ" },
             { HDTimKiemGoogle, "Tìm kiếm google" },
             { HDNhanTinBanBe, "Nhắn tin" },
        };
        public readonly static Dictionary<string, string> DescriptionAction = new Dictionary<string, string>
{
    // General
    { HDDocThongBao, "Tài khoản sẽ mở và đọc thông báo, có delay ngẫu nhiên..." },
    { HDXemReel, "Tài khoản xem reel (video ngắn) ngẫu nhiên hoặc chỉ định, có thể like/bình luận..." },
    { HDXemStory, "Tài khoản xem story của bạn bè/ngẫu nhiên và tương tác (like, trả lời)..." },
    { HDXemWatch, "Xem video trong mục Facebook Watch, theo từ khóa hoặc random..." },
    { HDTuongTacNewfeed, "Like, comment, share các bài viết trên bảng tin..." },
    { HDTuongTacBanBe, "Truy cập trang cá nhân bạn bè, like/bình luận bài viết..." },
    { HDTuongTacNhom, "Xem bài trong nhóm, tương tác bài viết (like/comment)..." },
    { HDTuongTacPage, "Like, comment, share bài viết trên Page..." },
    { HDTuongTacWall, "Like, comment các bài viết trên tường tài khoản khác..." },
    { HDTuongTacEvent, "Quan tâm, tham gia, like hoặc bình luận sự kiện..." },
    { HDTuongTacBaiViet, "Tìm bài theo keyword/chỉ định và tương tác..." },
    { HDTuongTacVideoLivestream, "Xem và tương tác video/live được chỉ định..." },

    // General1
    { HDGuiLoiMoiKetBan, "Gửi kết bạn ngẫu nhiên hoặc theo chỉ định..." },
    { HDXacNhanKetBan, "Chấp nhận lời mời kết bạn..." },
    { HDHuyKetBan, "Hủy kết bạn theo danh sách hoặc ngẫu nhiên..." },
    { HDThamGiaNhom, "Join nhóm theo keyword/chỉ định..." },
    { HDRoiNhom, "Thoát nhóm đã tham gia..." },
    { HDTaoNhom, "Tạo nhóm mới theo thông tin chỉ định..." },
    { HDTaoPage, "Tạo fanpage mới..." },

    // General2
    { HDDangBaiTuong, "Đăng status, ảnh, video lên tường cá nhân..." },
    { HDDangBaiNhom, "Đăng nội dung vào nhóm đã tham gia..." },
    { HDDangBaiPage, "Đăng nội dung trên fanpage quản lý..." },
    { HDShareBaiNangCao, "Chia sẻ bài viết kèm caption tùy chỉnh..." },
    { HDDangReel, "Đăng video ngắn dạng reel..." },
    { HDDangStory, "Đăng story ảnh/video..." },
    { HDDanhGiaPage, "Đánh giá (review) fanpage..." },
    { HDBuffLikePage, "Tăng lượt like cho page..." },
    { HDBuffFollowUID, "Tăng lượt follow tài khoản..." },
    { HDMoiBanBeLikePage, "Mời bạn bè like page..." },
    { HDMoiBanBeVaoNhom, "Mời bạn bè tham gia nhóm..." },

    // General3
    { HDDoiTen, "Thay đổi tên tài khoản..." },
    { HDDoiMatKhau, "Đổi mật khẩu tài khoản..." },
    { HDUpAvatar, "Thay ảnh đại diện tài khoản..." },
    { HDUpCover, "Đổi ảnh cover tài khoản..." },
    { HDOnOff2FA, "Quản lý xác thực 2 bước..." },
    { HDXoaSdt, "Gỡ số điện thoại khỏi tài khoản..." },
    { HDAddMail, "Quản lý email liên kết..." },
    { HDCapNhatThongTin, "Chỉnh sửa thông tin cá nhân bio, ngày sinh..." },
    { HDDangXuatThietBiCu, "Đăng xuất khỏi thiết bị cũ..." },
    { HDXoaThietBiTinCay, "Gỡ thiết bị tin cậy..." },
    { HDBatCheDoChuyenNghiep, "Chuyển sang chế độ chuyên nghiệp..." },
    { HDNghiGiaiLao, "Cho phép tài khoản dừng hoạt động trong thời gian ngẫu nhiên..." },

    // General4
    { HDDongBoDanhBa, "Đồng bộ danh bạ điện thoại lên Facebook..." },
    { HDTimKiemGoogle, "Tìm kiếm Google theo keyword, lướt xem website..." },
    { HDNhanTinBanBe, "Gửi tin nhắn đến bạn bè/người dùng..." },
};

    }
}
