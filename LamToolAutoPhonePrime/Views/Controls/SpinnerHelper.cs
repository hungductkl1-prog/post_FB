using LamToolAutoPhonePrime.Utils;
using LamToolAutoPhonePrime.Utils.Design;
using Sunny.Subdy.Common.Helper;
using System.ComponentModel;
using Timer = System.Windows.Forms.Timer;

[DesignerCategory("Code")]
public class SpinnerHelper : Control
{
    private Timer timer = new Timer();
    private TimeSpan remainingTime;

    public SpinnerHelper(string message, int seconds)
    {
        Message = message;
        remainingTime = TimeSpan.FromSeconds(seconds);
        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);

        Size = new Size(200, 100); // Adjust size to fit the timer display
        BackColor = Color.Transparent;

        timer.Interval = 1000; // Update every second
        timer.Tick += (s, e) => UpdateRemainingTime();
        if (!DesignMode)
        {
            timer.Enabled = true;
        }
    }

    [Browsable(true)]
    [Category("Appearance")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Message { get; set; } = "thời gian còn {0}";

    [Browsable(true)]
    [Category("Appearance")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int DurationInSeconds
    {
        get => (int)remainingTime.TotalSeconds;
        set
        {
            remainingTime = TimeSpan.FromSeconds(value);
            Invalidate();
        }
    }

    private void UpdateRemainingTime()
    {
        if (remainingTime.TotalSeconds > 0)
        {
            remainingTime = remainingTime.Subtract(TimeSpan.FromSeconds(1));
            Invalidate();
        }
        else
        {
            timer.Stop();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (null != Parent && (BackColor.A != 255 || BackColor == Color.Transparent))
        {
            using (Bitmap bmp = new Bitmap(Parent.Width, Parent.Height))
            {
                foreach (Control control in GetIntersectingControls(Parent))
                {
                    control.DrawToBitmap(bmp, control.Bounds);
                }

                e.Graphics.DrawImage(bmp, -Left, -Top);

                if (BackColor != Color.Transparent)
                {
                    using (Brush brush = new SolidBrush(BackColor))
                    {
                        e.Graphics.FillRectangle(brush, 0, 0, Width, Height);
                    }
                }
            }
        }

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

        // Format the message with the remaining time
        string elapsedTimeStr = remainingTime.ToString(@"hh\:mm\:ss");
        string displayMessage = string.Format(Message, elapsedTimeStr);

        // Draw the message and elapsed time
        using (Font font = new Font(FontScale.FamilyName, 11, FontStyle.Bold))
        {
            SizeF messageSize = e.Graphics.MeasureString(displayMessage, font);
            PointF messageLocation = new PointF((Width - messageSize.Width) / 2, (Height - messageSize.Height) / 2);

            // Define the padding for the text background
            int padding = 10;

            // Calculate the rectangle for the text background
            RectangleF backgroundRect = new RectangleF(
                messageLocation.X - padding / 2,
                messageLocation.Y - padding / 2,
                messageSize.Width + padding,
                messageSize.Height + padding
            );

            // Draw the background rectangle
            using (Brush backgroundBrush = new SolidBrush(Color.DodgerBlue))
            {
                e.Graphics.FillRectangle(backgroundBrush, backgroundRect);
            }

            // Draw the text
            using (Brush brush = new SolidBrush(Color.White))
            {
                e.Graphics.DrawString(displayMessage, font, brush, messageLocation);
            }
        }
    }


    protected override void OnVisibleChanged(EventArgs e)
    {
        timer.Enabled = Visible;
        base.OnVisibleChanged(e);
    }

    private IOrderedEnumerable<Control> GetIntersectingControls(Control parent)
    {
        return parent.Controls.Cast<Control>()
            .Where(c => parent.Controls.GetChildIndex(c) > parent.Controls.GetChildIndex(this))
            .Where(c => c.Bounds.IntersectsWith(Bounds))
            .OrderByDescending(c => parent.Controls.GetChildIndex(c));
    }
}
