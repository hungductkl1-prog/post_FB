using SDL2;
using System.Drawing.Imaging;

namespace LamToolAutoPhonePrime
{
    public partial class ucThongBaoDeviceView : UserControl
    {
        public ucThongBaoDeviceView()
        {
            InitializeComponent();
        }
        public void SetRenderSize(int height)
        {
            int width = (int)(height * 9.0 / 16.0);
            this.Size = new Size(width, height);
        }
    }
}
