using AntdUI;
using AutoAndroid;
using System.Windows.Forms;

namespace StreamAndroid
{
    public partial class fShowDevice : Form
    {
        public event EventHandler? SettingsButtonClicked;
        private readonly Form parentForm;
        public ucControlAndroid ucDevice;
        private ucMenuscripDevice menu;
        public bool isDragging = false;
        public fShowDevice(Form form)
        {
            InitializeComponent();
            parentForm = form;

        }

        public void SetRenderSize(int height)
        {
            int width = (int)(height * 9.0 / 16.0);
            ucDevice.SetRenderSize(height);
            Size = new Size(width + menu.Width + 20, height);
            this.Refresh();
        }
        public void Load(ucControlAndroid device)
        {
            ucDevice = device;
            menu = new ucMenuscripDevice(true, this, parentForm)
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
