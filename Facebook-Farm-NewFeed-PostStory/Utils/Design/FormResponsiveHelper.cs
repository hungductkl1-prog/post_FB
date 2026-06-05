using System;
using System.Drawing;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Utils.Design
{
    /// <summary>
    /// Giúp form lớn vừa được mở trên màn hình nhỏ (1366x768 trở xuống):
    ///  - Clamp Size về tối đa workingArea * 0.92.
    ///  - Bật AutoScroll trên client area để user scroll khi nội dung tràn.
    /// Dùng sau InitializeComponent() trong constructor form.
    /// </summary>
    public static class FormResponsiveHelper
    {
        const double MaxRatio = 0.92;

        public static void MakeScrollable(Form form)
        {
            if (form == null) return;

            var workingArea = Screen.FromControl(form).WorkingArea;
            int maxW = (int)(workingArea.Width * MaxRatio);
            int maxH = (int)(workingArea.Height * MaxRatio);

            int targetW = Math.Min(form.Width, maxW);
            int targetH = Math.Min(form.Height, maxH);

            if (targetW != form.Width || targetH != form.Height)
            {
                form.Size = new Size(targetW, targetH);
                form.StartPosition = FormStartPosition.CenterParent;
            }

            // Trick: bật AutoScroll ở form để khi nội dung lớn hơn ClientArea
            // thì WinForms tự thêm scrollbar. AutoScrollMinSize giữ nguyên kích
            // thước nội dung gốc để không bị cắt.
            var originalClient = new Size(
                form.ClientSize.Width,
                form.ClientSize.Height);
            form.AutoScroll = true;
            form.AutoScrollMinSize = originalClient;
        }
    }
}
