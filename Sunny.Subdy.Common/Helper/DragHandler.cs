using System.Drawing;
using System.Windows.Forms;

namespace Sunny.Subdy.Common.Helper
{
    public class DragHandler
    {
        private bool dragging;
        private Point dragOffset;
        private Form targetForm;
        private Control parentForm;


        public DragHandler(Control triggerControl, Form formToMove)
        {
            targetForm = formToMove;

            triggerControl.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    dragging = true;
                    // Lấy khoảng cách giữa chuột và góc trên trái form
                    dragOffset = new Point(e.X + triggerControl.Left, e.Y + triggerControl.Top);
                }
            };

            triggerControl.MouseMove += (s, e) =>
            {
                if (dragging)
                {
                    Point currentScreenPos = triggerControl.PointToScreen(e.Location);
                    // Di chuyển form theo offset
                    targetForm.Location = new Point(
                        currentScreenPos.X - dragOffset.X,
                        currentScreenPos.Y - dragOffset.Y
                    );
                }
            };

            triggerControl.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    dragging = false;
            };
        }

        public DragHandler(List<Control> triggerControls, Form formToMove)
        {
            targetForm = formToMove;
            foreach (Control control in triggerControls)
            {
                control.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        dragging = true;
                        dragOffset = new Point(e.X + control.Left, e.Y + control.Top);
                    }
                };

                control.MouseMove += (s, e) =>
                {
                    if (dragging)
                    {
                        Point currentScreenPos = control.PointToScreen(e.Location);
                        targetForm.Location = new Point(
                            currentScreenPos.X - dragOffset.X,
                            currentScreenPos.Y - dragOffset.Y
                        );
                    }
                };

                control.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                        dragging = false;
                };
            }
        }

        public DragHandler(Control triggerControl, Form formToMove, Control formCha)
        {
            targetForm = formToMove;
            parentForm = formCha;

            triggerControl.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    dragging = true;
                    // Lưu vị trí chuột so với form (tọa độ màn hình)
                    var mouseScreenPos = triggerControl.PointToScreen(e.Location);
                    dragOffset = new Point(mouseScreenPos.X - targetForm.Left, mouseScreenPos.Y - targetForm.Top);
                }
            };

            triggerControl.MouseMove += (s, e) =>
            {
                if (!dragging) return;

                Point currentScreenPos = triggerControl.PointToScreen(e.Location);

                // Tính vị trí mới của form
                int newX = currentScreenPos.X - dragOffset.X;
                int newY = currentScreenPos.Y - dragOffset.Y;

                // Nếu parentForm là control, cần lấy tọa độ thật trên màn hình
                Rectangle parentBounds = parentForm is Form f
                    ? f.Bounds
                    : new Rectangle(parentForm.PointToScreen(Point.Empty), parentForm.Size);

                // Giới hạn form trong vùng hiển thị của parent
                if (newX < parentBounds.Left)
                    newX = parentBounds.Left;
                if (newY < parentBounds.Top)
                    newY = parentBounds.Top;
                if (newX + targetForm.Width > parentBounds.Right)
                    newX = parentBounds.Right - targetForm.Width;
                if (newY + triggerControl.Height > parentBounds.Bottom)
                    newY = parentBounds.Bottom - triggerControl.Height;

                // Cập nhật vị trí
                targetForm.Location = new Point(newX, newY);
            };

            triggerControl.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    dragging = false;
            };
        }
    }
}
