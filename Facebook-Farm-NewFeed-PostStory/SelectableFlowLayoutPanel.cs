using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Facebook_Farm_NewFeed_PostStory
{
    public class SelectableFlowLayoutPanel : FlowLayoutPanel
    {
        private bool isSelecting = false;
        private Point startPoint;
        private Rectangle selectionRect = Rectangle.Empty;
        private readonly SolidBrush fillBrush = new SolidBrush(Color.FromArgb(80, 0, 255, 0));
        private readonly Pen borderPen = new Pen(Color.FromArgb(180, 0, 255, 0), 2f);

        public event Action<Rectangle>? SelectionChanged;
        public event Action? SelectionFinished;

        public SelectableFlowLayoutPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                isSelecting = true;
                startPoint = e.Location;
                selectionRect = new Rectangle(e.Location, Size.Empty);
                Invalidate();
            }
            else if (e.Button == MouseButtons.Right)
            {
                // Hủy vùng chọn
                isSelecting = false;
                selectionRect = Rectangle.Empty;
                SelectionChanged?.Invoke(Rectangle.Empty);
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!isSelecting) return;

            int x = Math.Min(startPoint.X, e.X);
            int y = Math.Min(startPoint.Y, e.Y);
            int w = Math.Abs(startPoint.X - e.X);
            int h = Math.Abs(startPoint.Y - e.Y);
            selectionRect = new Rectangle(x, y, w, h);

            SelectionChanged?.Invoke(selectionRect);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (isSelecting && e.Button == MouseButtons.Left)
            {
                isSelecting = false;
                selectionRect = Rectangle.Empty; // <--- xoá vùng phủ ngay
                SelectionFinished?.Invoke();
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (selectionRect.Width > 0 && selectionRect.Height > 0)
            {
                e.Graphics.FillRectangle(fillBrush, selectionRect);
                e.Graphics.DrawRectangle(borderPen, selectionRect);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                fillBrush.Dispose();
                borderPen.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
