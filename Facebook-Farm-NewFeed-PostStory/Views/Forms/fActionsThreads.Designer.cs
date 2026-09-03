using System.Drawing;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    partial class fActionsThreads
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
            windowBar = new Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel();
            txt_search = new AntdUI.Input();
            btn_mode = new Button();
            btn_global = new Button();
            btn_setting = new Button();
            virtualPanel = new AntdUI.VirtualPanel();
            windowBar.SuspendLayout();
            SuspendLayout();
            //
            // windowBar
            //
            windowBar.BackColor = Color.White;
            windowBar.Controls.Add(txt_search);
            windowBar.Controls.Add(btn_mode);
            windowBar.Controls.Add(btn_global);
            windowBar.Controls.Add(btn_setting);
            windowBar.Cursor = Cursors.Hand;
            windowBar.Dock = DockStyle.Top;
            windowBar.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            windowBar.ForeColor = Color.Black;
            windowBar.Icon = Properties.Resources.logo_lamtool_v3_dark_16;
            windowBar.Location = new Point(0, 0);
            windowBar.Name = "windowBar";
            windowBar.Size = new Size(1300, 35);
            windowBar.TabIndex = 10;
            windowBar.Text = "Hành động Threads";
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
            btn_mode.Image = Properties.Resources.icons8_circle_16_Green;
            btn_mode.Location = new Point(1220, 0);
            btn_mode.Name = "btn_mode";
            btn_mode.Size = new Size(26, 35);
            btn_mode.TabIndex = 6;
            //
            // btn_global
            //
            btn_global.Dock = DockStyle.Right;
            btn_global.Image = Properties.Resources.icons8_circle_16_Yellow;
            btn_global.Location = new Point(1246, 0);
            btn_global.Name = "btn_global";
            btn_global.Size = new Size(24, 35);
            btn_global.TabIndex = 7;
            //
            // btn_setting
            //
            btn_setting.Dock = DockStyle.Right;
            btn_setting.Image = Properties.Resources.icons8_circle_16_Red;
            btn_setting.Location = new Point(1270, 0);
            btn_setting.Name = "btn_setting";
            btn_setting.Size = new Size(30, 35);
            btn_setting.TabIndex = 8;
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
            // fActionsThreads
            //
            BackColor = Color.White;
            ClientSize = new Size(1300, 720);
            Controls.Add(virtualPanel);
            Controls.Add(windowBar);
            Font = new Font("Microsoft YaHei UI", 12F);
            ForeColor = Color.Black;
            MinimumSize = new Size(660, 400);
            Name = "fActionsThreads";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Threads Overview";
            windowBar.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private Facebook_Farm_NewFeed_PostStory.Utils.TitleBarPanel windowBar;
        private AntdUI.Input txt_search;
        private Button btn_mode;
        private Button btn_global;
        private Button btn_setting;
        private AntdUI.VirtualPanel virtualPanel;
    }
}
