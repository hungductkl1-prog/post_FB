using System.Windows.Forms;

namespace AntdUI
{
    public class CommonMethod
    {
        public static void ShowMessageSuccess(string message, string title = "Thông báo")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        public static void ShowMessageError(string message, string title = "Thông báo")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        public static void ShowMessageWarning(string message, string title = "Thông báo")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        public static bool ShowConfirmWarning(string message, string title = "Xác nhận")
        {
            var result = MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            return result == DialogResult.Yes;
        }
    }
}
