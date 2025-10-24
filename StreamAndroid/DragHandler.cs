namespace StreamAndroid
{
    public class DragHandler
    {
        private bool dragging;
        private Point dragOffset;
        private Form targetForm;
        private Form parentForm;

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

        public DragHandler(Control triggerControl, Form formToMove, Form formCha)
        {
            targetForm = formToMove;
            parentForm = formCha;

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

                    // Tính toán vị trí mới
                    int newX = currentScreenPos.X - dragOffset.X;
                    int newY = currentScreenPos.Y - dragOffset.Y;

                    // Giới hạn trong ranh giới của formCha
                    // Giới hạn trái
                    if (newX < parentForm.Left)
                        newX = parentForm.Left;

                    // Giới hạn trên - ĐẢM BẢO triggerControl LUÔN HIỂN THỊ
                    // Không cho form đi lên cao quá, phải để lại ít nhất chiều cao của triggerControl
                    if (newY < parentForm.Top)
                        newY = parentForm.Top;

                    // Giới hạn phải
                    if (newX + targetForm.Width > parentForm.Right)
                        newX = parentForm.Right - targetForm.Width;

                    // Giới hạn dưới - ĐẢM BẢO triggerControl KHÔNG BỊ CHE MẤT
                    // Form không được đi xuống quá, phải để lại ít nhất chiều cao của triggerControl trong vùng nhìn thấy
                    int minVisibleHeight = triggerControl.Height + triggerControl.Top; // Chiều cao tối thiểu cần hiển thị
                    if (newY + minVisibleHeight > parentForm.Bottom)
                        newY = parentForm.Bottom - minVisibleHeight;

                    // Di chuyển form với vị trí đã được giới hạn
                    targetForm.Location = new Point(newX, newY);
                }
            };

            triggerControl.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    dragging = false;
            };
        }
    }
}
