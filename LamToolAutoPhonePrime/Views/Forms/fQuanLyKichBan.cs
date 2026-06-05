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
            virtualPanel.MouseDoubleClick += virtualPanel_MouseDoubleClick;

            AddCreateScriptBar();

            radioButton4.CheckedChanged += UpdateConfigPanels;
            checkBox2.CheckedChanged += UpdateConfigPanels;
            checkBox3.CheckedChanged += UpdateConfigPanels;
            checkBox4.CheckedChanged += UpdateConfigPanels;
            _configHelper = new Sunny.Subdy.Common.Json.ConfigHelper(this, $"{nameof(fQuanLyKichBan)}_{_platform}", onLoad: new System.Action(() =>
            {
                UpdateConfigPanels(null, null);

            }), shouldExit: false);
            _scriptContext.FixMissingIds();
            _scriptContext.PurgeFarmXu(); // Xoá sạch kịch bản FarmXu legacy.
            _scriptContext.RemapLegacyFarmXuVipName(); // "Farm-Xu-VIP" → "Làm Job QN"
            RemapLegacyFarmXuAccounts();
            if (_platform == Sunny.Subdy.Common.Models.PlatformModel.Facebook
                || _platform == Sunny.Subdy.Common.Models.PlatformModel.Instagram
                || _platform == Sunny.Subdy.Common.Models.PlatformModel.Threads)
            {
                _scriptContext.EnsureFarmXuVip(_platform);
            }
            LoadList();

            this.Load += (_, __) => LamToolAutoPhonePrime.Utils.Design.SsaTheme.ApplyFQuanLyKichBan(this);
        }

        /// <summary>
        /// Account legacy NameScript = "FarmXu" hoặc "Farm-Xu-VIP" → remap sang tên mới ("Làm Job QN").
        /// Idempotent.
        /// </summary>
        private void RemapLegacyFarmXuAccounts()
        {
            try
            {
                var ctx = new AccountContext();
                var all = ctx.GetAll(new List<string>(), _platform, true) ?? new List<Account>();
                var changed = all.Where(a =>
                    string.Equals(a.NameScript, ScriptNames.FarmXu, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(a.NameScript, ScriptNames.FarmXuVipLegacy, StringComparison.OrdinalIgnoreCase)
                ).ToList();
                if (changed.Count == 0) return;
                foreach (var a in changed) a.NameScript = ScriptNames.FarmXuVip;
                ctx.Update(changed);
            }
            catch (Exception ex)
            {
                Sunny.Subdy.Common.Logs.LogManager.Error(ex);
            }
        }

        

        public void OpenPage(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                AntdUI.Message.warn(this, "Không xác định được kịch bản (id rỗng).", autoClose: 3);
                return;
            }
            if (!Guid.TryParse(id, out var guid))
            {
                AntdUI.Message.error(this, $"Id kịch bản không hợp lệ: {id}", autoClose: 3);
                return;
            }
            try
            {
                var script = _scriptContext.GetById(guid);
                if (script != null && string.Equals(script.Name, ScriptNames.FarmXuVip, StringComparison.OrdinalIgnoreCase))
                {
                    // "Làm Job QN" không còn popup token riêng — token lấy từ phiên login QN.
                    AntdUI.Message.info(this, "\"Làm Job QN\" dùng token đăng nhập QN, không cần cấu hình thêm.", autoClose: 3);
                    return;
                }
                fChiTietKichBan form = new fChiTietKichBan(guid);
                form.ShowDialog();
                LoadList();
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(this, "Mở chi tiết kịch bản lỗi: " + ex.Message, autoClose: 5);
            }
        }

        private void AddCreateScriptBar()
        {
            var bar = new System.Windows.Forms.Panel
            {
                Height = 48,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(236, 240, 241)
            };
            var btnCreate = new AntdUI.Button
            {
                Text = "  + Tạo kịch bản mới",
                Type = AntdUI.TTypeMini.Primary,
                Size = new Size(190, 36),
                Location = new Point(12, 6),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                Radius = 8
            };
            btnCreate.Click += (s, e) => button1_Click(s, e);
            bar.Controls.Add(btnCreate);

            virtualPanel.Parent.Controls.Add(bar);
            bar.BringToFront();
            virtualPanel.BringToFront();
            // ensure bar is above virtualPanel in docking order
            Controls.SetChildIndex(bar, Controls.IndexOf(virtualPanel));
        }

        private void virtualPanel_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int x = e.X, y = e.Y + virtualPanel.ScrollBar.Value;
            foreach (var it in virtualPanel.Items)
            {
                if (it is VItem vi && it.SHOW && it.RECT.Contains(x, y))
                {
                    OpenPage(vi.Tag.ToString());
                    return;
                }
            }
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
                // FarmXu: ẩn hoàn toàn (mặc định Subdy, không cấu hình)
                if (string.Equals(script.Name, ScriptNames.FarmXu, StringComparison.OrdinalIgnoreCase))
                    continue;
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
