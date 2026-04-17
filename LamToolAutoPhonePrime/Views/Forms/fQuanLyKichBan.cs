using AntdUI;
using LamToolAutoPhonePrime.Utils;
using LamToolAutoPhonePrime.Views.Forms;
using LamToolAutoPhonePrime.Views.Forms.Actions;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace LamToolAutoPhonePrime.Views
{
    public partial class fQuanLyKichBan : AntdUI.Window
    {
        private ScriptContext _scriptContext;
        private ScriptActionContext _scriptActionContext;
        private string _platform = "";
        private Sunny.Subdy.Common.Json.ConfigHelper _configHelper;
        public fQuanLyKichBan(string platform)
        {
            InitializeComponent();
            _platform = platform;
            _scriptContext = new ScriptContext();
            _scriptActionContext = new ScriptActionContext();
            txt_search.PrefixClick += txt_search_PrefixClick;
            txt_search.TextChanged += txt_search_TextChanged;
            virtualPanel.ItemClick += ItemClick;

            radioButton4.CheckedChanged += UpdateConfigPanels;
            checkBox2.CheckedChanged += UpdateConfigPanels;
            checkBox3.CheckedChanged += UpdateConfigPanels;
            checkBox4.CheckedChanged += UpdateConfigPanels;
            _configHelper = new Sunny.Subdy.Common.Json.ConfigHelper(this, $"{nameof(fQuanLyKichBan)}_{_platform}", onLoad: new System.Action(() =>
            {
                UpdateConfigPanels(null, null);

            }), shouldExit: false);
            EnsureDefaultScripts();
            LoadList();
        }

        void EnsureDefaultScripts()
        {
            if (_platform != "Facebook") return;
            if (_scriptContext.GetByName("FarmXu", "Facebook") != null) return;

            var script = new Script
            {
                Id = Guid.NewGuid(),
                Platform = "Facebook",
                Name = "FarmXu",
                DateCreate = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            };
            _scriptContext.Add(script);

            var actions = new List<ScriptAction>
            {
                new ScriptAction { Id = Guid.NewGuid(), ScriptId = script.Id, Platform = "Facebook",
                    Name = "Đọc thông báo", Type = FacebookFarmingType.HDDocThongBao, ByOrder = 1, Json = "{}" },
                new ScriptAction { Id = Guid.NewGuid(), ScriptId = script.Id, Platform = "Facebook",
                    Name = "Xem Watch", Type = FacebookFarmingType.HDXemWatch, ByOrder = 2, Json = "{}" },
                new ScriptAction { Id = Guid.NewGuid(), ScriptId = script.Id, Platform = "Facebook",
                    Name = "Tương tác newfeed", Type = FacebookFarmingType.HDTuongTacNewfeed, ByOrder = 3, Json = "{}" },
                new ScriptAction { Id = Guid.NewGuid(), ScriptId = script.Id, Platform = "Facebook",
                    Name = "Nghỉ giải lao", Type = FacebookFarmingType.HDNghiGiaiLao, ByOrder = 4, Json = "{}" },
            };
            _scriptActionContext.AddRange(actions);
        }

        

        public void OpenPage(string id)
        {
            fChiTietKichBan form = new fChiTietKichBan(Guid.Parse(id));
            form.ShowDialog();
            LoadList();
        }
        private void txt_search_PrefixClick(object sender, MouseEventArgs e) => LoadSearchList();

        private void txt_search_TextChanged(object sender, EventArgs e) => LoadSearchList();
        void LoadSearchList()
        {
            string search = txt_search.Text;
            windowBar.Loading = true;
            BeginInvoke(new Action(() =>
            {
                virtualPanel.PauseLayout = true;
                if (string.IsNullOrEmpty(search))
                {
                    foreach (var it in virtualPanel.Items) it.Visible = true;
                    virtualPanel.Empty = false;
                }
                else
                {
                    virtualPanel.Empty = true;
                    string searchLower = search.ToLower();
                    var titles = new List<TItem>(virtualPanel.Items.Count);
                    foreach (var it in virtualPanel.Items)
                    {
                        if (it is VItem item) it.Visible = item.data.id.Contains(search) || item.data.key.Contains(search) || item.data.keyword.Contains(searchLower) || item.data.keywordmini.Contains(searchLower);
                        else if (it is TItem itemTitle) titles.Add(itemTitle);
                    }
                    foreach (var it in titles)
                    {
                        int count = 0;
                        foreach (var item in it.data)
                        {
                            if (item.Visible) count++;
                        }
                        it.Visible = count > 0;
                    }
                }
                virtualPanel.PauseLayout = false;
                windowBar.Loading = false;
            }));
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            DraggableMouseDown();
            base.OnMouseDown(e);
        }
        private void ItemClick(object sender, AntdUI.VirtualItemEventArgs e) => OpenPage(e.Item.Tag.ToString());
        private void button1_Click(object sender, EventArgs e)
        {
            fFolder folder = new fFolder("AddScript", _platform);
            folder.ShowDialog();
            LoadList();
        }

        void LoadList()
        {
            virtualPanel.Items.Clear();
            IList[] dir_General = new IList[]
            {


            };
            var scripts = _scriptContext.GetByPlatform(_platform);
            foreach (var script in scripts)
            {
                dir_General = dir_General.Append(new IList(script.Id.ToString(), script.Name, Properties.Resources.IconDocThongBao)).ToArray();
            }

            var dir = new Dictionary<string, IList[]>
            {
                { $"Danh sách kịch bản {_platform}", dir_General }
            };

            var list = new List<AntdUI.VirtualItem>(dir.Count + dir_General.Length);

            foreach (var it in dir)
            {
                var list_sub = new List<AntdUI.VirtualItem>(it.Value.Length);
                foreach (var item in it.Value) list_sub.Add(new VItem(item));
                list.Add(new TItem(it.Key, list_sub));
                list.AddRange(list_sub);
            }
            virtualPanel.Items.AddRange(list);
            windowBar.Loading = false;
            virtualPanel.BlurBar = windowBar;
        }
        class IList
        {
            public IList(string _id, string _key, string _img)
            {
                id = _id;
                key = _key;
                keyword = _id.ToLower() + AntdUI.Pinyin.GetPinyin(_key).ToLower();
                keywordmini = AntdUI.Pinyin.GetInitials(_key).ToLower();
                imgs = new Image[] { AntdUI.SvgExtend.SvgToBmp(_img) };
            }
            public string id { get; set; }
            public string keyword { get; set; }
            public string keywordmini { get; set; }
            public string key { get; set; }
            public Image[] imgs { get; set; }
        }

        class TItem : AntdUI.VirtualItem
        {
            string title, count;
            public List<AntdUI.VirtualItem> data;
            public TItem(string t, List<AntdUI.VirtualItem> d)
            {
                CanClick = false;
                data = d;
                title = t;
                count = d.Count.ToString();
            }

            StringFormat s_f = AntdUI.Helper.SF_NoWrap(lr: StringAlignment.Near);
            StringFormat s_c = AntdUI.Helper.SF_NoWrap();
            public override void Paint(AntdUI.Canvas g, AntdUI.VirtualPanelArgs e)
            {
                using (var font_title = new Font(e.Panel.Font, FontStyle.Bold))
                using (var font_count = new Font(e.Panel.Font.FontFamily, e.Panel.Font.Size * .74F, e.Panel.Font.Style))
                {
                    var size = AntdUI.Helper.Size(g.MeasureString(title, font_title));
                    g.String(title, font_title, AntdUI.Style.Db.Text, new Rectangle(e.Rect.X + x, e.Rect.Y, e.Rect.Width, e.Rect.Height), s_f);

                    var rect_count = new Rectangle(e.Rect.X + x + size.Width + gap, e.Rect.Y + (e.Rect.Height - size.Height) / 2, size.Height, size.Height);
                    using (var path = AntdUI.Helper.RoundPath(rect_count, e.Radius))
                    {
                        g.Fill(AntdUI.Style.Db.TagDefaultBg, path);
                        g.Draw(AntdUI.Style.Db.DefaultBorder, sp, path);
                    }
                    g.String(count, font_count, AntdUI.Style.Db.Text, rect_count, s_c);
                }
            }

            int gap = 8, sp = 1, x = 30;
            public override Size Size(AntdUI.Canvas g, AntdUI.VirtualPanelArgs e)
            {
                var dpi = AntdUI.Config.Dpi;
                gap = (int)(8 * dpi);
                sp = (int)(1 * dpi);
                x = (int)(30 * dpi);
                return new Size(e.Rect.Width, (int)(44 * dpi));
            }
        }

        class VItem : AntdUI.VirtualShadowItem
        {
            public IList data;
            string name;
            public VItem(IList d)
            {
                data = d;
                Tag = d.id;
                name = data.key;
            }

            StringFormat s_f = AntdUI.Helper.SF(lr: StringAlignment.Near);
            public override void Paint(AntdUI.Canvas g, AntdUI.VirtualPanelArgs e)
            {
                using (var brush = new SolidBrush(AntdUI.Style.Db.BgContainer))
                {
                    using (var path = AntdUI.Helper.RoundPath(e.Rect, e.Radius))
                    {
                        g.Fill(brush, path);
                        using (var brush_bor = new Pen(Hover ? AntdUI.Style.Db.BorderColorDisable : AntdUI.Style.Db.BorderColor, thickness))
                        {
                            g.Draw(brush_bor, path);
                        }
                    }
                }
                using (var fore = new SolidBrush(AntdUI.Style.Db.Text))
                {
                    using (var font_title = new Font(e.Panel.Font.FontFamily, 11F, FontStyle.Bold))
                    {
                        g.String(name, font_title, fore, new Rectangle(e.Rect.X + size2, e.Rect.Y, e.Rect.Width - size2, title_height), s_f);
                    }
                }
                using (var brush = new SolidBrush(AntdUI.Style.Db.Split))
                {
                    g.Fill(brush, new RectangleF(e.Rect.X + size, e.Rect.Y + title_height - thickness / 2F, e.Rect.Width - size2, thickness));
                }
                try
                {
                    var bmp = AntdUI.Config.IsDark ? data.imgs[1] : data.imgs[0];
                    g.Image(bmp, e.Rect.X + (e.Rect.Width - bmp.Width) / 2, (e.Rect.Y + title_height) + ((e.Rect.Height - title_height) - bmp.Height) / 2, bmp.Width, bmp.Height);
                }
                catch { }
            }

            int title_height = 44, thickness = 1, size = 10, size2 = 20;
            public override Size Size(AntdUI.Canvas g, AntdUI.VirtualPanelArgs e)
            {
                var dpi = AntdUI.Config.Dpi;
                title_height = (int)(44 * dpi);
                thickness = (int)(1 * dpi);
                size = (int)(10 * dpi);
                size2 = size * 2;
                return new Size((int)(258 * dpi), (int)(244 * dpi));
            }
        }

        private void btn_setting_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btn_global_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
                this.WindowState = FormWindowState.Normal;
            else
                this.WindowState = FormWindowState.Maximized;
        }

        private void UpdateConfigPanels(object sender, EventArgs e)
        {
            bool lap = radioButton4.Checked;
            lblSoLanLap.Enabled = nudSoLanLap.Enabled = lblLuot.Enabled =
                lblChoLuot.Enabled = nudChoLuotFrom.Enabled = lblDenLuot.Enabled =
                nudChoLuotTo.Enabled = lblPhutLuot.Enabled = lap;

            bool taiKhoan = checkBox2.Checked;
            nudTaiKhoanFrom.Enabled = lblDenTaiKhoan.Enabled =
                nudTaiKhoanTo.Enabled = lblPhutTaiKhoan.Enabled = taiKhoan;

            bool kichBan = checkBox3.Checked;
            nudKichBanFrom.Enabled = lblDenKichBan.Enabled =
                nudKichBanTo.Enabled = lblPhutKichBan.Enabled = kichBan;

            bool ngayMoi = checkBox4.Checked;
            lblThoiGianBatDau.Enabled = timepickerFrom.Enabled =
                lblDenNgay.Enabled = timepickerTo.Enabled = ngayMoi;
        }

        private void llbHuongDan_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://www.youtube.com/watch?v=CFpL_YVw3q4") { UseShellExecute = true }); } catch { }
        }
    }
}
