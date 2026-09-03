
using AntdUI;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Facebook_Farm_NewFeed_PostStory.Utils.Design;
using Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Models;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    public partial class fActions : Facebook_Farm_NewFeed_PostStory.Utils.BaseForm
    {
        private string scriptId = "";
        public fActions(string scriptId)
        {
            InitializeComponent();
            this.scriptId = scriptId;
            windowBar.Text += " " + windowBar.ProductVersion;
            txt_search.PrefixClick += txt_search_PrefixClick;
            txt_search.TextChanged += txt_search_TextChanged;
            virtualPanel.ItemClick += ItemClick;
            windowBar.BackClick += btn_back_Click;
            FontUtil.ApplyFontToAllControls(this); Facebook_Farm_NewFeed_PostStory.Utils.Design.VietnameseFont.Enforce(this);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            DraggableMouseDown();
            base.OnMouseDown(e);
        }

        private void ItemClick(object sender, AntdUI.VirtualItemEventArgs e) => OpenPage(e.Item.Tag.ToString());
        public void OpenPage(string id)
        {
            var oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.Enabled = false;
            try
            {
                // Project mới chỉ hỗ trợ 6 action — switch ngắn gọn lại.
                Form form = id switch
                {
                    FacebookFarmingType.HDDoiMatKhau => new fHDDoiMatKhau(this.scriptId),
                    FacebookFarmingType.HDOnOff2FA => new fHDOnOff2FA(this.scriptId),
                    FacebookFarmingType.HDTuongTacNewfeed => new fHDTuongTacNewfeed(this.scriptId),
                    FacebookFarmingType.HDDangStory => new fHDDangStory(this.scriptId),
                    FacebookFarmingType.HDUpAvatar => new fHDUpAvatar(this.scriptId),
                    FacebookFarmingType.HDUpCover => new fHDUpCover(this.scriptId),
                    _ => null
                };
                if(form == null)
                {
                    AntdHelper.NotifyError(this, "Thao tác thất bại", "Chức năng đang được cập nhật!");
                    return;
                }
                this.Cursor = oldCursor;
                this.Enabled = true;
                var result = form.ShowDialog();
                if (result == DialogResult.OK)
                {
                    DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            finally
            {
                this.Cursor = oldCursor;
                this.Enabled = true;
            }
        }

        private void btn_back_Click(object sender, EventArgs e)
        {
            BeginInvoke(new Action(() =>
            {
                if (windowBar.Tag is Control control)
                {
                    control.Dispose();
                    Controls.Remove(control);
                }
                windowBar.ShowBack = false;
                virtualPanel.Visible = true;
                windowBar.SubText = "Overview";
            }));
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


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            windowBar.Loading = true;
            LoadList();
            FontUtil.ApplyFontToAllControls(this); Facebook_Farm_NewFeed_PostStory.Utils.Design.VietnameseFont.Enforce(this);
        }

        void LoadList()
        {
            // Project mới: chỉ 6 action — chia 2 nhóm (Nuôi tương tác / Đổi thông tin).
            IList[] dir_Nuoi = new IList[]
            {
                new IList(FacebookFarmingType.HDTuongTacNewfeed,
                    FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDTuongTacNewfeed],
                    Properties.Resources.IconTuongTacNewFeed,
                    FacebookFarmingType.DescriptionAction[FacebookFarmingType.HDTuongTacNewfeed]),
                new IList(FacebookFarmingType.HDDangStory,
                    FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDDangStory],
                    Properties.Resources.IconStory,
                    FacebookFarmingType.DescriptionAction[FacebookFarmingType.HDDangStory]),
            };
            IList[] dir_DoiThongTin = new IList[]
            {
                new IList(FacebookFarmingType.HDDoiMatKhau,
                    FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDDoiMatKhau],
                    Properties.Resources.IconDocThongBao,
                    FacebookFarmingType.DescriptionAction[FacebookFarmingType.HDDoiMatKhau]),
                new IList(FacebookFarmingType.HDOnOff2FA,
                    FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDOnOff2FA],
                    Properties.Resources.IconDocThongBao,
                    FacebookFarmingType.DescriptionAction[FacebookFarmingType.HDOnOff2FA]),
                new IList(FacebookFarmingType.HDUpAvatar,
                    FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDUpAvatar],
                    Properties.Resources.IconDocThongBao,
                    FacebookFarmingType.DescriptionAction[FacebookFarmingType.HDUpAvatar]),
                new IList(FacebookFarmingType.HDUpCover,
                    FacebookFarmingType.DictionariesAction[FacebookFarmingType.HDUpCover],
                    Properties.Resources.IconDocThongBao,
                    FacebookFarmingType.DescriptionAction[FacebookFarmingType.HDUpCover]),
            };
            var dir = new Dictionary<string, IList[]>
            {
                { "Nuôi tài khoản", dir_Nuoi },
                { "Đổi thông tin", dir_DoiThongTin },
            };

            var list = new List<AntdUI.VirtualItem>(dir.Count + dir_Nuoi.Length + dir_DoiThongTin.Length);

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
            public IList(string _id, string _key, string _img, string _documain)
            {
                id = _id;
                key = _key;
                keyword = _id.ToLower() + AntdUI.Pinyin.GetPinyin(_key).ToLower();
                keywordmini = AntdUI.Pinyin.GetInitials(_key).ToLower();
                imgs = new Image[] { AntdUI.SvgExtend.SvgToBmp(_img) };
                documain = _documain;
            }
            public string id { get; set; }
            public string documain { get; set; }
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

                // Lấy icon
                Image bmp = null;
                try { bmp = AntdUI.Config.IsDark ? data.imgs[1] : data.imgs[0]; } catch { }

                int iconSize = 24;
                int margin = 10;
                int textOffsetX = margin + (bmp != null ? iconSize + 8 : 0);

                // Title
                using (var fore = new SolidBrush(AntdUI.Style.Db.Text))
                using (var font_title = new Font(FontScale.FamilyName, 11F, FontStyle.Bold))
                {
                    var rectTitle = new Rectangle(e.Rect.X + textOffsetX, e.Rect.Y, e.Rect.Width - textOffsetX, title_height);
                    g.String(name, font_title, fore, rectTitle, s_f);
                }

                // Icon
                if (bmp != null)
                {
                    int y = e.Rect.Y + (title_height - iconSize) / 2;
                    g.Image(bmp, e.Rect.X + margin, y, iconSize, iconSize);
                }

                // Gạch ngang phân cách
                int lineY = e.Rect.Y + title_height;
                using (var brush = new SolidBrush(AntdUI.Style.Db.Split))
                {
                    g.Fill(brush, new RectangleF(e.Rect.X + size, lineY, e.Rect.Width - size2, thickness));
                }

                // documain nằm dưới gạch, cách ra một đoạn
                using (var foreDesc = new SolidBrush(AntdUI.Style.Db.TextSecondary))
                using (var font_desc = new Font(FontScale.FamilyName, System.Math.Max(FontScale.Body, e.Panel.Font.Size * 0.75f), FontStyle.Regular))
                {
                    int padding = 2; // giảm khoảng trắng, cho nó sát hơn với gạch
                    var rectDesc = new Rectangle(
                        e.Rect.X + margin,
                        lineY + thickness + padding,
                        e.Rect.Width - margin * 2,
                        e.Rect.Height - (lineY + padding)
                    );
                    g.String(data.documain, font_desc, foreDesc, rectDesc, s_f);
                }
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
            Close();
        }

        private void btn_global_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
                this.WindowState = FormWindowState.Normal;
            else
                this.WindowState = FormWindowState.Maximized;
        }
    }
}