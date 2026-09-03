namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    /// <summary>
    /// Bản Farm không có màn hình đăng nhập — các thao tác thêm tài khoản / nhóm luôn được phép.
    /// (Bản Prime dùng fLogin + Globals.User; copy code sang Farm không nên chặn theo User.)
    /// </summary>
    public static class LoginGuard
    {
        public static bool EnsureLoggedIn(IWin32Window owner = null) => true;
    }
}
