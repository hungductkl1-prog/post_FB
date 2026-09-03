using System.Windows.Forms;
using Facebook_Farm_NewFeed_PostStory.Utils.Design;

namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    /// <summary>
    /// Base class WinForms thuần thay cho AntdUI.Window.
    /// Dùng native title bar / border của Windows (ổn định với NativeAOT).
    /// </summary>
    public class BaseForm : Form
    {
        public BaseForm()
        {
            Font = FontScale.Body9;
            StartPosition = FormStartPosition.CenterScreen;
        }

        /// <summary>
        /// Stub tương thích: AntdUI.Window cũ gọi DraggableMouseDown() để kéo form khi
        /// borderless. Với native title bar việc kéo đã do Windows xử lý → no-op.
        /// </summary>
        protected void DraggableMouseDown() { }
    }
}
