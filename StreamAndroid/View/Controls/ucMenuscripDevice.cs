using AntdUI;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Data.Models;

namespace StreamAndroid
{
    public partial class ucMenuscripDevice : UserControl
    {
        private Form? targetForm;
        private Control? parentForm;
        public event EventHandler? SettingsButtonClicked;
        private DeviceModel Device;
        public ucMenuscripDevice(DeviceModel device, Form form , Control formCha )
        {
            InitializeComponent();
            Device = device;
            button9.Visible = true;
            targetForm = form;
            parentForm = formCha;
            new DragHandler(label2, targetForm, parentForm);
            this.Load += UcMenuscripDevice_Load;
        }
        public ucMenuscripDevice(DeviceModel device)
        {
            InitializeComponent();
            Device = device;
            button9.Visible = false;
            label2.Cursor = Cursors.Default;
            this.Load += UcMenuscripDevice_Load;
        }

        private void UcMenuscripDevice_Load(object? sender, EventArgs e)
        {
           
            label2.Suffix = $" {Device.NameDevice}";
            label2.Text = Device.Id.ToString();
            CreateMenu();
            // Buttons bottom
            ConfigureAntdButton(button1, "BorderOutlined");
            ConfigureAntdButton(button2, "HomeOutlined");
            ConfigureAntdButton(button3, "DoubleLeftOutlined");
        }

        private void CreateMenu()
        {
            menu3.Items.Clear();
            MenuItem itemReboot = CreateMenuItem("Khởi động lại", "PoweroffOutlined");
            MenuItem itemInstallApk = CreateMenuItem("Cài đặt APK", "AppstoreAddOutlined");
            MenuItem itemImportFile = CreateMenuItem("Xoay phải", "RotateRightOutlined");
            MenuItem itemExportClipboard = CreateMenuItem("Xuất Clipboard", "CopyOutlined");
            menu3.Items.AddRange(new MenuItem[] { itemReboot, itemInstallApk, itemImportFile, itemExportClipboard });
        }
        private MenuItem CreateMenuItem(string text, string icon)
        {
            MenuItem menuItem = new MenuItem(text);
            menuItem.Text = text;
            menuItem.IconSvg = icon;

            return menuItem;
        }




        private void ConfigureAntdButton(AntdUI.Button btn, string icon)
        {
            btn.Dock = DockStyle.Fill;
            btn.Cursor = Cursors.Hand;
            btn.DefaultBack = Color.Transparent;
            btn.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            btn.ForeColor = Color.Black;
            btn.IconSvg = icon;
            btn.IconRatio = 0.8F;
            btn.Radius = 10;
            btn.BackHover = Color.FromArgb(233, 247, 239);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (targetForm != null && parentForm != null)
            {
                SettingsButtonClicked?.Invoke(this, EventArgs.Empty);

            }
        }

        private void menu3_SelectChanged(object sender, MenuSelectEventArgs e)
        {

        }
    }
}
