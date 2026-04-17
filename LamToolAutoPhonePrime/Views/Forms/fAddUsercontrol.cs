using AntdUI;
using LamToolAutoPhonePrime.Utils;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using Sunny.Subdy.UI.View.Pages;

namespace LamToolAutoPhonePrime.Views.Forms
{
    public partial class fAddUsercontrol : AntdUI.Window
    {
        private FolderContext _folderContext;
        private string _type = "";
        private string _platform = "";
        ucManagerDevices _control;
        public fAddUsercontrol(string type, string platform, ucManagerDevices control)
        {
            InitializeComponent();
            _type = type;
            _platform = platform;
            switch (_type)
            {
                case "SelectDevices":
                    {
                        windowBar.Text = $"Chọn thiết bị cần chạy {_platform}";
                        break;
                    }
            }
            _control = control;
            _control.Dock = DockStyle.Fill;
            panel1.Controls.Add(_control);

            FontUtil.ApplyFontToAllControls(this);
        }

        private void btn_setting_Click(object sender, EventArgs e)
        {
            _control.button2.Visible = false;
            this.Close();
        }

    }
}
