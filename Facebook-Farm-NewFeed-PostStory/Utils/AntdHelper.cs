using System;
using System.Windows.Forms;
using Facebook_Farm_NewFeed_PostStory.Utils;

namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    /// <summary>
    /// Helper UI — đã chuyển sang WinForms thuần (WinFormsHelper). Giữ tên/method cũ
    /// để các caller không phải đổi.
    /// </summary>
    public static class AntdHelper
    {
        // ── Message (toast góc trên) ────────────────────────────────────────────
        public static void MsgSuccess(Form form, string text) => WinFormsHelper.MsgSuccess(form, text);
        public static void MsgWarn(Form form, string text) => WinFormsHelper.MsgWarn(form, text);
        public static void MsgError(Form form, string text) => WinFormsHelper.MsgError(form, text);
        public static void MsgInfo(Form form, string text) => WinFormsHelper.MsgInfo(form, text);

        // ── Modal confirm ──────────────────────────────────────────────────────
        public static bool Confirm(Form form, string title, string content) =>
            WinFormsHelper.Confirm(form, title, content);

        // ── Spin (loading overlay trên một control) ────────────────────────────
        public static System.Threading.Tasks.Task Spin(Control control, string text, Func<System.Threading.Tasks.Task> action) =>
            WinFormsHelper.Spin(control, text, action);

        // ── Notification (toast góc dưới phải, không chặn UI) ───────────
        public static void NotifySuccess(Form form, string title, string desc) =>
            WinFormsHelper.NotifySuccess(form, title, desc);

        public static void NotifyError(Form form, string title, string desc) =>
            WinFormsHelper.NotifyError(form, title, desc);

        public static void NotifyWarn(Form form, string title, string desc) =>
            WinFormsHelper.NotifyWarn(form, title, desc);

        // ── WithLoading: async wrapper cho Spin ─────────────────────────
        public static System.Threading.Tasks.Task WithLoading(
            Control control,
            string text,
            Func<System.Threading.Tasks.Task> action) =>
            WinFormsHelper.Spin(control, text, action);

        // ── Debounce ────────────────────────────────────────────────────
        public static System.Windows.Forms.Timer Debounce(Action action, int ms = 300) =>
            WinFormsHelper.Debounce(action, ms);
    }
}
