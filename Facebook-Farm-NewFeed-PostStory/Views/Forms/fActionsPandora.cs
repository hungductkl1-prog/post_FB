using AntdUI;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Facebook_Farm_NewFeed_PostStory.Utils.Design;
using Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Models;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    public partial class fActionsPandora : Facebook_Farm_NewFeed_PostStory.Utils.BaseForm
    {
        private string scriptId = "";

        public fActionsPandora(string scriptId)
        {
            InitializeComponent();
            this.scriptId = scriptId;
            windowBar.Text += " " + windowBar.ProductVersion;
            txt_search.PrefixClick += txt_search_PrefixClick;
            txt_search.TextChanged += txt_search_TextChanged;
            virtualPanel.ItemClick += ItemClick;
            FontUtil.ApplyFontToAllControls(this);
            Facebook_Farm_NewFeed_PostStory.Utils.Design.VietnameseFont.Enforce(this);
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
                Form form = null;
                switch (id)
                {
                    case PandoraFarmingType.HDNgheNhac:
                        form = new fPandoraNgheNhac(this.scriptId);
                        break;
                }
                if (form == null)
                {
                    AntdHelper.NotifyError(this, "Thao tác thất bại", "Chức năng đang được cập nhật!");
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

        private void btn_setting_Click(object sender, EventArgs e) => this.Close();

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
            FontUtil.ApplyFontToAllControls(this);
            Facebook_Farm_NewFeed_PostStory.Utils.Design.VietnameseFont.Enforce(this);
        }

        void LoadList()
        {
            IList[] dir_Music = new IList[]
            {
                new IList(PandoraFarmingType.HDNgheNhac, PandoraFarmingType.DictionariesAction[PandoraFarmingType.HDNgheNhac], "CustomerServiceOutlined", PandoraFarmingType.DescriptionAction[PandoraFarmingType.HDNgheNhac]),
            };

            var dir = new Dictionary<string, IList[]>
            {
                { "Nghe nhạc", dir_Music },
            };

            var list = new List<AntdUI.VirtualItem>(dir.Count + dir_Music.Length);

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
                name = d.key;
                Tag = d.id;
            }

            StringFormat s_f = AntdUI.Helper.SF_NoWrap(lr: StringAlignment.Near);
            StringFormat s_desc = AntdUI.Helper.SF_NoWrap(lr: StringAlignment.Near);

            public override void Paint(AntdUI.Canvas g, AntdUI.VirtualPanelArgs e)
            {
                using (var font_title = new Font(e.Panel.Font, FontStyle.Bold))
                using (var font_desc = new Font(e.Panel.Font.FontFamily, e.Panel.Font.Size * .82F, FontStyle.Regular))
                {
                    int pad = (int)(12 * AntdUI.Config.Dpi);
                    int icon = (int)(28 * AntdUI.Config.Dpi);
                    var rectIcon = new Rectangle(e.Rect.X + pad, e.Rect.Y + pad, icon, icon);
                    if (data.imgs != null && data.imgs.Length > 0 && data.imgs[0] != null)
                    {
                        g.Image(data.imgs[0], rectIcon);
                    }
                    var rectTitle = new Rectangle(rectIcon.Right + pad / 2, e.Rect.Y + pad, e.Rect.Width - rectIcon.Right - pad, icon);
                    g.String(name, font_title, AntdUI.Style.Db.Text, rectTitle, s_f);

                    var rectDesc = new Rectangle(e.Rect.X + pad, rectIcon.Bottom + pad / 2, e.Rect.Width - pad * 2, e.Rect.Height - rectIcon.Bottom - pad);
                    g.String(data.documain ?? "", font_desc, AntdUI.Style.Db.TextTertiary, rectDesc, s_desc);
                }
            }

            public override Size Size(AntdUI.Canvas g, AntdUI.VirtualPanelArgs e)
            {
                var dpi = AntdUI.Config.Dpi;
                int width = (int)(280 * dpi);
                int height = (int)(110 * dpi);
                return new Size(width, height);
            }
        }
    }
}
