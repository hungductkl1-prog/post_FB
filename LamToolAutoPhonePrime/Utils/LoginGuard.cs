using LamToolAutoPhonePrime.Views.Forms;
using Sunny.Subdy.Common.Models;

namespace LamToolAutoPhonePrime.Utils
{
    /// <summary>
    /// Lazy-login gate: chỉ yêu cầu user đăng nhập tại điểm thực sự cần
    /// (Thêm tài khoản / Tạo nhóm / chạy job ...). Trả về true nếu sau đó
    /// đã có Globals.User, false nếu user huỷ login.
    /// </summary>
    public static class LoginGuard
    {
        public static bool EnsureLoggedIn(IWin32Window owner = null)
        {
            if (Globals.User != null) return true;
            using var frm = new fLogin();
            var result = owner != null ? frm.ShowDialog(owner) : frm.ShowDialog();
            return result == DialogResult.OK && Globals.User != null;
        }
    }
}
