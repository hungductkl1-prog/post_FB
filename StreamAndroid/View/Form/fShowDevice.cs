using AntdUI;
using AutoAndroid;
using System.Windows.Forms;

namespace StreamAndroid
{
    public partial class fShowDevice : Form
    {
        public event EventHandler? SettingsButtonClicked;
        private readonly Control parentForm;
        public ucControlAndroid ucDevice;
        private ucMenuscripDevice menu;
        public bool isDragging = false;
        public fShowDevice(Control form)
        {
            InitializeComponent();
            parentForm = form;

        }
        private int angle = 0;

        public void SetRenderSize(int height, int rotationAngle = 0)
        {
            ucDevice.SetRenderSize(height, rotationAngle);
            angle = rotationAngle;

            int rotationValue = angle switch
            {
                0 => 0,
                90 => 1,
                180 => 2,
                270 => 3,
                _ => 0
            };
            int width;
            if (angle == 90 || angle == 270)
            {
                width = (int)(height * 16.0 / 9.0);
            }
            else
            {
                width = (int)(height * 9.0 / 16.0);
            }

            this.Size = new Size(width + menu.Width + 10, height);


            this.Refresh();
        }
        public void Load(ucControlAndroid device)
        {
            tableLayoutPanel1.Controls.Clear();
            ucDevice = device;
            ucDevice.showOverlayText =false;
            menu = new ucMenuscripDevice(ucDevice.device, this, parentForm)
            {
                Dock = DockStyle.Right
            };
            menu.SettingsButtonClicked += ucMenuscripDevice_HideForm;
            tableLayoutPanel1.Controls.Add(menu, 2, 0);
            device.Dock = DockStyle.Fill;
            tableLayoutPanel1.Controls.Add(device, 0, 0);
        }
      
        private void ucMenuscripDevice_HideForm(object? sender, EventArgs e)
        {
            SettingsButtonClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
