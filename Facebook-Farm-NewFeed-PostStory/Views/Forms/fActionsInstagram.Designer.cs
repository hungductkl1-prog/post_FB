using System.Drawing;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    partial class fActionsInstagram
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            windowBar = new AntdUI.PageHeader();
            txt_search = new AntdUI.Input();
            btn_mode = new AntdUI.Button();
            btn_global = new AntdUI.Button();
            btn_setting = new AntdUI.Button();
            virtualPanel = new AntdUI.VirtualPanel();
            windowBar.SuspendLayout();
            SuspendLayout();
            //
            // windowBar
            //
            windowBar.BackColor = Color.White;
            windowBar.BackgroundImageLayout = ImageLayout.Stretch;
            windowBar.CloseSize = 30;
            windowBar.Controls.Add(txt_search);
            windowBar.Controls.Add(btn_mode);
            windowBar.Controls.Add(btn_global);
            windowBar.Controls.Add(btn_setting);
            windowBar.Cursor = Cursors.Hand;
            windowBar.DividerMargin = 1;
            windowBar.Dock = DockStyle.Top;
            windowBar.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            windowBar.ForeColor = Color.Black;
            windowBar.Icon = Properties.Resources.logo_lamtool_v3_dark_16;
            windowBar.Location = new Point(0, 0);
            windowBar.MDI = true;
            windowBar.Name = "windowBar";
            windowBar.ShowIcon = true;
            windowBar.Size = new Size(1300, 35);
            windowBar.SubFont = new Font("Microsoft Sans Serif", Facebook_Farm_NewFeed_PostStory.Utils.Design.FontScale.Caption, FontStyle.Regular, GraphicsUnit.Point, 0);
            windowBar.SubGap = 1;
            windowBar.SubText = "";
            windowBar.TabIndex = 10;
            windowBar.Text = "Hành động Instagram";
            windowBar.UseSystemStyleColor = true;
            windowBar.UseTextBold = false;
            //
            // txt_search
            //
            txt_search.Dock = DockStyle.Right;
            txt_search.LocalizationPlaceholderText = "Overview.{id}";
            txt_search.Location = new Point(978, 0);
            txt_search.Name = "txt_search";
            txt_search.Padding = new Padding(0, 2, 0, 2);
            txt_search.PlaceholderText = "Tìm kiếm...";
            txt_search.PrefixSvg = "SearchOutlined";
            txt_search.Size = new Size(242, 35);
            txt_search.TabIndex = 11;
            //
            // btn_mode
            //
            btn_mode.Dock = DockStyle.Right;
            btn_mode.Ghost = true;
            btn_mode.Icon = Properties.Resources.icons8_circle_16_Green;
            btn_mode.IconSvg = "";
            btn_mode.Location = new Point(1220, 0);
            btn_mode.Name = "btn_mode";
            btn_mode.Radius = 0;
            btn_mode.Size = new Size(26, 35);
            btn_mode.TabIndex = 6;
            btn_mode.ToggleIconSvg = "MoonOutlined";
            btn_mode.WaveSize = 0;
            //
            // btn_global
            //
            btn_global.Dock = DockStyle.Right;
            btn_global.Ghost = true;
            btn_global.Icon = Properties.Resources.icons8_circle_16_Yellow;
            btn_global.IconSvg = "";
            btn_global.Location = new Point(1246, 0);
            btn_global.Name = "btn_global";
            btn_global.Radius = 0;
            btn_global.Size = new Size(24, 35);
            btn_global.TabIndex = 7;
            btn_global.WaveSize = 0;
            //
            // btn_setting
            //
            btn_setting.Dock = DockStyle.Right;
            btn_setting.Ghost = true;
            btn_setting.Icon = Properties.Resources.icons8_circle_16_Red;
            btn_setting.IconSvg = "";
            btn_setting.Location = new Point(1270, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Radius = 0;
            btn_setting.Size = new Size(30, 35);
            btn_setting.TabIndex = 8;
            btn_setting.WaveSize = 0;
            btn_setting.Click += btn_setting_Click;
            //
            // virtualPanel
            //
            virtualPanel.BackColor = Color.FromArgb(236, 240, 241);
            virtualPanel.Dock = DockStyle.Fill;
            virtualPanel.JustifyContent = AntdUI.TJustifyContent.SpaceEvenly;
            virtualPanel.Location = new Point(0, 35);
            virtualPanel.Name = "virtualPanel";
            virtualPanel.Shadow = 20;
            virtualPanel.ShadowOpacityAnimation = true;
            virtualPanel.Size = new Size(1300, 685);
            virtualPanel.TabIndex = 11;
            virtualPanel.Waterfall = true;
            //
            // fActionsInstagram
            //
            BackColor = Color.White;
            ClientSize = new Size(1300, 720);
            Controls.Add(virtualPanel);
            Controls.Add(windowBar);
            Font = new Font("Microsoft YaHei UI", 12F);
            ForeColor = Color.Black;
            MinimumSize = new Size(660, 400);
            Name = "fActionsInstagram";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Instagram Overview";
            windowBar.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private AntdUI.PageHeader windowBar;
        private AntdUI.Input txt_search;
        private AntdUI.Button btn_mode;
        private AntdUI.Button btn_global;
        private AntdUI.Button btn_setting;
        private AntdUI.VirtualPanel virtualPanel;
    }
}
