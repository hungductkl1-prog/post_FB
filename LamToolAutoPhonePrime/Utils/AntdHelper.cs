using AntdUI;
using System.Collections.Generic;
using System.Windows.Forms;

namespace LamToolAutoPhonePrime.Utils
{
    /// <summary>
    /// Localization provider: dịch các string mặc định của AntdUI sang tiếng Việt.
    /// </summary>
    public class VietnameseLocalization : ILocalization
    {
        private static readonly Dictionary<string, string> _map = new()
        {
            ["OK"] = "Xác nhận",
            ["Cancel"] = "Hủy",
        };

        public string? GetLocalizedString(string key) =>
            _map.TryGetValue(key, out var val) ? val : null;
    }

    /// <summary>
    /// Helper dùng AntdUI thay thế MessageBox và Form progress thô sơ.
    /// </summary>
    public static class AntdHelper
    {
        // ── Message (toast góc trên) ────────────────────────────────────────────

        public static void MsgSuccess(Form form, string text) =>
            AntdUI.Message.success(form, text, autoClose: 3);

        public static void MsgWarn(Form form, string text) =>
            AntdUI.Message.warn(form, text, autoClose: 3);

        public static void MsgError(Form form, string text) =>
            AntdUI.Message.error(form, text, autoClose: 4);

        public static void MsgInfo(Form form, string text) =>
            AntdUI.Message.info(form, text, autoClose: 3);

        // ── Modal confirm ──────────────────────────────────────────────────────

        public static bool Confirm(Form form, string title, string content) =>
            AntdUI.Modal.open(form, title, content, TType.Warn) == DialogResult.OK;

        // ── Spin (loading overlay trên một control) ────────────────────────────
        // Dùng: await AntdHelper.Spin(control, "Đang xử lý...", cfg => { ... });

        public static System.Threading.Tasks.Task Spin(Control control, string text, Action<AntdUI.Spin.Config> action) =>
            AntdUI.Spin.open(control, text, action);
    }
}
