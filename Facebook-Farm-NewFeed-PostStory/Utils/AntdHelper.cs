using AntdUI;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Utils
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

        // ── Notification (toast góc dưới phải, không chặn UI) ───────────

        public static void NotifySuccess(Form form, string title, string desc) =>
            AntdUI.Notification.success(form, title, desc, AntdUI.TAlignFrom.BR);

        public static void NotifyError(Form form, string title, string desc) =>
            AntdUI.Notification.error(form, title, desc, AntdUI.TAlignFrom.BR);

        public static void NotifyWarn(Form form, string title, string desc) =>
            AntdUI.Notification.warn(form, title, desc, AntdUI.TAlignFrom.BR);

        // ── WithLoading: async wrapper cho Spin (không cần xử lý Config) ─
        // Dùng: await AntdHelper.WithLoading(this, "Đang import...", async () => { await DoWork(); });
        public static System.Threading.Tasks.Task WithLoading(
            Control control,
            string text,
            Func<System.Threading.Tasks.Task> action) =>
            AntdUI.Spin.open(control, text, async _ => { await action(); });

        // ── Debounce: trả về Timer; caller gọi Stop()+Start() mỗi lần gõ ──
        // Dùng:
        //   _searchTimer?.Stop();
        //   _searchTimer = AntdHelper.Debounce(() => DoSearch(txt.Text), 300);
        //   _searchTimer.Start();
        public static System.Windows.Forms.Timer Debounce(Action action, int ms = 300)
        {
            var t = new System.Windows.Forms.Timer { Interval = ms };
            t.Tick += (_, __) =>
            {
                t.Stop();
                t.Dispose();
                action();
            };
            return t;
        }
    }
}
