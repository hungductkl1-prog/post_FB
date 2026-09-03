using AntdUI;
using Facebook_Farm_NewFeed_PostStory.Views.Controls;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;

namespace Facebook_Farm_NewFeed_PostStory.Utils.Design
{
    /// <summary>
    /// SSA visual redesign. KHÔNG đổi business logic, KHÔNG sửa Designer.
    /// Mọi chỉnh sửa chỉ đụng property visual/layout.
    /// </summary>
    internal static class SsaTheme
    {
        // Font dùng chung cho badge painter — KHÔNG dispose. Tránh cấp phát Font mỗi ô khi
        // scroll grid 30k dòng (FontScale.Body9Bold tạo Font mới mỗi lần gọi → GDI churn → lag).
        private static readonly Font _badgeFont = new Font(FontScale.FamilyName, FontScale.Body, FontStyle.Bold);

        // Cache brush badge theo màu — bg lấy từ tập palette cố định (~7 màu), dùng lại thay vì
        // new SolidBrush mỗi ô khi cuộn. Badge painter chỉ chạy trên UI thread nên dùng chung an toàn.
        private static readonly System.Collections.Generic.Dictionary<Color, SolidBrush> _badgeBrushCache = new();
        private static SolidBrush BadgeBrush(Color c)
        {
            if (!_badgeBrushCache.TryGetValue(c, out var b))
            {
                b = new SolidBrush(c);
                _badgeBrushCache[c] = b;
            }
            return b;
        }
        // GraphicsPath dùng lại 1 instance (Reset mỗi lần) — tránh cấp phát path + mảng điểm mỗi ô.
        private static readonly GraphicsPath _badgePath = new GraphicsPath();

        // ══════════════════════════════════════════════════════════════════
        //  fMain
        // ══════════════════════════════════════════════════════════════════

        public static void ApplyFMain(fMain form)
        {
            if (form == null) return;

            // Anti-alias toàn app: ClearType grid-fit cho mọi AntdUI control
            AntdUI.Config.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            form.BackColor = ColorPalette.Background;

            TuneWindowBar(form);
            TuneSidebar(form);
            TuneContentArea(form);
            MountSidebarMenu(form);
        }

        private static void TuneWindowBar(fMain form)
        {
            var bar = GetField<PageHeader>(form, "windowBar");
            if (bar == null) return;

            bar.BackColor = ColorPalette.Surface;
            bar.ForeColor = ColorPalette.TextPrimary;
            bar.Font      = FontScale.SectionBold;
            bar.SubFont   = FontScale.Caption8;
            bar.DividerMargin = 0;
            bar.UseSystemStyleColor = true;
            bar.UseTextBold = true;

            // Inner title label: chuyển sang hierarchy SSA
            var label1 = GetField<AntdUI.Label>(form, "label1");
            if (label1 != null)
            {
                label1.Font      = FontScale.HeadingBold;
                label1.ForeColor = ColorPalette.TextPrimary;
                label1.Padding   = new Padding(Spacing.Lg, 0, 0, 0);
            }

            // CPU/RAM/Clock toolstrip
            var ts = GetField<ToolStrip>(form, "toolStrip1");
            if (ts != null)
            {
                ts.BackColor = ColorPalette.Surface;
                ts.Font      = FontScale.Body9;
                foreach (ToolStripItem it in ts.Items)
                {
                    it.Font = FontScale.Body9;
                    // giữ nguyên ForeColor semantic (cpu/ram/clock)
                }
            }

            // Window buttons
            foreach (var name in new[] { "btn_mode", "btn_global", "btn_setting" })
            {
                var b = GetField<AntdUI.Button>(form, name);
                if (b == null) continue;
                b.Ghost  = true;
                b.Radius = 0;
            }
        }

        private static void TuneSidebar(fMain form)
        {
            // panel1 = sidebar wrapper
            var panel1 = GetField<System.Windows.Forms.Panel>(form, "panel1");
            if (panel1 != null)
            {
                panel1.BackColor = ColorPalette.Surface;
                panel1.Width     = 260;
                panel1.Padding   = new Padding(0);
            }

            // panel2 = profile card wrapper
            var panel2 = GetField<System.Windows.Forms.Panel>(form, "panel2");
            if (panel2 != null)
            {
                panel2.BackColor = ColorPalette.Surface;
                panel2.Padding   = new Padding(Spacing.Md, Spacing.Md, Spacing.Md, Spacing.Sm);
                panel2.Height    = 132;
            }

            // pMenu = placeholder panel chứa AntdUI.Menu
            var pMenu = GetField<System.Windows.Forms.Panel>(form, "pMenu");
            if (pMenu != null)
            {
                pMenu.BackColor = ColorPalette.Surface;
                pMenu.Padding   = new Padding(Spacing.Sm, Spacing.Sm, Spacing.Sm, Spacing.Sm);
            }
        }

        private static void TuneContentArea(fMain form)
        {
            var pContent = GetField<System.Windows.Forms.Panel>(form, "pContent");
            if (pContent == null) return;

            pContent.BackColor = ColorPalette.Background;
            pContent.Padding   = new Padding(0); // UserControls tự padding

            MountSystemStatusBar(form, pContent);

            // ucHistoriesJob được tạo sau (trong fMain_Load async). Hook ControlAdded
            // để inject Operations Dashboard cards lên top khi nó join pContent.
            pContent.ControlAdded += (s, e) =>
            {
                if (e.Control is ucHistoriesJob hj) MountOperationsDashboard(hj);
            };
            // Trường hợp đã add trước khi hook (idempotent):
            foreach (Control c in pContent.Controls)
            {
                if (c is ucHistoriesJob hj) MountOperationsDashboard(hj);
            }
        }

        /// <summary>
        /// Inject Operations Dashboard cards (Dock=Top) vào trong ucHistoriesJob.
        /// Idempotent — chỉ tạo 1 lần.
        /// </summary>
        private static void MountOperationsDashboard(ucHistoriesJob hj)
        {
            const string key = "ssaOperationsDashboard";
            if (hj.Controls.Find(key, true).Length > 0) return;

            var dash = new ucOperationsDashboard { Name = key };
            hj.Controls.Add(dash);
            dash.BringToFront();
        }

        /// <summary>
        /// Mount ucSystemStatusBar lên trên cùng pContent (Dock=Top, height 32).
        /// Idempotent — chỉ tạo 1 lần.
        /// </summary>
        private static void MountSystemStatusBar(fMain form, System.Windows.Forms.Panel pContent)
        {
            const string key = "ssaSystemStatusBar";
            if (pContent.Controls.Find(key, true).Length > 0) return;

            var bar = new ucSystemStatusBar { Name = key };
            pContent.Controls.Add(bar);
            bar.BringToFront();
        }

        /// <summary>
        /// Mount AntdUI.Menu vào trong pMenu, bridge click sang MenuButton_Click cũ
        /// bằng cách invoke trực tiếp handler với sender là Button tương ứng.
        /// Button cũ giữ nguyên trong cây (để PerformClick/reference code cũ vẫn chạy),
        /// chỉ move sang panel ẩn kích thước 0 thay vì Visible=false.
        /// </summary>
        private static void MountSidebarMenu(fMain form)
        {
            var pMenu = GetField<System.Windows.Forms.Panel>(form, "pMenu");
            if (pMenu == null) return;

            // Thu thập các Button cũ do CreateMenu() đã khởi tạo trong pMenu
            var oldContainers = pMenu.Controls.OfType<System.Windows.Forms.Panel>().ToList();
            var oldButtons = oldContainers
                .SelectMany(p => p.Controls.OfType<System.Windows.Forms.Button>())
                .ToList();
            if (oldButtons.Count == 0) return;

            // Move container cũ xuống dưới nhưng giữ Visible=true + kích thước 0,
            // để PerformClick() vẫn hoạt động (CanSelect yêu cầu tổ tiên Visible=true).
            foreach (var cont in oldContainers)
            {
                cont.Dock    = DockStyle.None;
                cont.Bounds  = new Rectangle(-4000, -4000, 1, 1); // đẩy ra ngoài màn hình
                cont.Visible = true;
            }

            // Font: Segoe UI Bold — chuẩn Windows desktop, crisp, không răng cưa
            var menuFont = new Font(FontScale.FamilyName, 11F, FontStyle.Bold);
            var groupFont = new Font(FontScale.FamilyName, 8.5F, FontStyle.Bold);

            var menu = new AntdUI.Menu
            {
                Dock        = DockStyle.Fill,
                BackColor   = ColorPalette.SidebarBg,
                ForeColor   = ColorPalette.TextPrimary,
                BackHover   = ColorPalette.SidebarItemHover,
                BackActive  = ColorPalette.SidebarItemActive,
                Radius      = Radius.Md,
                Font        = menuFont,
                ShowSubBack = false,
                Indent      = false,
                Unique      = true,
            };

            // SSA: group structure — Operations / Automation / Infrastructure
            // Group headers là MenuItem disabled, chỉ render text uppercase tracking
            // (vẫn nằm trong Items để giữ thứ tự render).
            var byName = oldButtons.ToDictionary(b => b.Name, b => b);

            void AddGroup(string title)
            {
                // Group header = MenuItem disabled (không click được, dim color tự apply).
                var header = new AntdUI.MenuItem(title.ToUpperInvariant())
                {
                    Enabled = false,
                    Font    = groupFont,
                    ID      = $"grp_{title}"
                };
                menu.Items.Add(header);
            }

            void AddItem(string btnName)
            {
                if (!byName.TryGetValue(btnName, out var btn)) return;
                var iconSvg = btnName switch
                {
                    "btn_android"   => "MobileOutlined",
                    "btn_facebook"  => "FacebookOutlined",
                    "btn_instagram" => "InstagramOutlined",
                    "btn_threads"   => "CommentOutlined",
                    "btn_history"   => "DashboardOutlined",
                    _               => "AppstoreOutlined"
                };
                var item = new AntdUI.MenuItem(btn.Text, iconSvg) { Tag = btn, ID = btn.Name };
                menu.Items.Add(item);
            }

            // Project mới: bỏ group header (Tổng quan/Tự động hoá/Hạ tầng), chỉ render
            // 2 item Facebook + Thiết bị flat trong sidebar — gọn hơn cho scope chỉ-FB.
            AddItem("btn_facebook");
            AddItem("btn_android");

            // Fallback: any button không match group nào → push xuống cuối "Khác"
            var grouped = new HashSet<string> {
                "btn_history", "btn_facebook", "btn_instagram", "btn_threads", "btn_android"
            };
            var others = oldButtons.Where(b => !grouped.Contains(b.Name)).ToList();
            if (others.Count > 0)
            {
                AddGroup("Khác");
                foreach (var b in others)
                {
                    var item = new AntdUI.MenuItem(b.Text, "AppstoreOutlined") { Tag = b, ID = b.Name };
                    menu.Items.Add(item);
                }
            }

            // Invoke trực tiếp MenuButton_Click của fMain (sender = Button cũ)
            // → bảo đảm BringToFront + label1 + _currentButton tracking chạy đúng,
            // không phụ thuộc CanSelect của PerformClick.
            var miClick = form.GetType().GetMethod(
                "MenuButton_Click",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            menu.SelectChanged += (s, e) =>
            {
                if (e.Value?.Tag is not System.Windows.Forms.Button srcBtn) return;
                if (miClick != null)
                    miClick.Invoke(form, new object[] { srcBtn, EventArgs.Empty });
                else
                    srcBtn.PerformClick();
            };

            pMenu.Controls.Add(menu);
            menu.BringToFront();

            // Tab Tài khoản là tab active mặc định khi mở phần mềm
            void SelectDefault()
            {
                var first = menu.Items.FirstOrDefault(i => i.ID == "btn_facebook") ?? menu.Items.FirstOrDefault();
                if (first != null) first.Select = true;
            }
            if (form.IsHandleCreated) SelectDefault();
            else form.HandleCreated += (_, __) => SelectDefault();
        }

        // ══════════════════════════════════════════════════════════════════
        //  ucdgvAccount
        // ══════════════════════════════════════════════════════════════════

        public static void ApplyUcAccount(ucdgvAccount uc)
        {
            if (uc == null) return;

            // Anti-alias toàn app (idempotent — đã được fMain set sẵn)
            AntdUI.Config.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            uc.BackColor = ColorPalette.Background;
            uc.Padding   = new Padding(Spacing.Lg);
            uc.Font      = FontScale.Body9;

            TuneAccountCards(uc);
            TuneAccountToolbar(uc);
            TuneAccountListHeader(uc);
            TuneAccountGrid(uc);
            TuneAccountFooter(uc);
            StyleEmptyStatePanel(uc);
            AttachStatusBadgePainter(uc);
            ForceCrispTextOnLegacyControls(uc);
            RelayoutAccountTopBar(uc);
        }

        /// <summary>
        /// Gom layout trên đầu thành 1 toolbar duy nhất (giống Figma / ảnh mẫu):
        /// - Ẩn tableLayoutPanel1 (block "Quản lý nhóm" + "Cài đặt jobs/Tương tác")
        /// - Giữ panel4 làm toolbar chính, chuyển các control vào đó:
        ///   [Chạy] [Dừng] [Select Nhóm] [Search] ... [Thêm tài khoản]
        /// - Giữ panel6 (List header) + panel5 (Grid)
        /// </summary>
        private static void RelayoutAccountTopBar(ucdgvAccount uc)
        {
            var tableTop = GetField<TableLayoutPanel>(uc, "tableLayoutPanel1");
            var panel4   = GetField<AntdUI.Panel>(uc, "panel4");
            var select1  = GetField<Select>(uc, "select1");
            var btnRun   = GetField<AntdUI.Button>(uc, "button7");
            var btnStop  = GetField<AntdUI.Button>(uc, "button8");
            var btnAdd   = GetField<AntdUI.Button>(uc, "button16");
            var input6   = GetField<Input>(uc, "input6");
            var btnJobs      = GetField<AntdUI.Button>(uc, "button4"); // Cài đặt jobs
            var btnSettings  = GetField<AntdUI.Button>(uc, "button5"); // Cài đặt chung
            var btnInteract  = GetField<AntdUI.Button>(uc, "button6"); // Tương tác

            // Extra buttons SSA: chỉ còn Quản lý nhóm + Cột hiển thị (Bộ lọc đã xóa)
            var btnFolderMgr = EnsureToolbarButton(uc, "ssaBtnFolderMgr", "FolderOpenOutlined", null);
            var btnColumns   = EnsureToolbarButton(uc, "ssaBtnColumns",   "InsertRowAboveOutlined", null);

            // Combobox "Kịch bản" giữa Run và Select nhóm
            var cboScript    = EnsureScriptSelect(uc);

            // Nếu lần chạy trước đã tạo ssaBtnFilter thì gỡ
            var panel4Parent = GetField<AntdUI.Panel>(uc, "panel4");
            if (panel4Parent != null)
            {
                var staleFilter = panel4Parent.Controls.Find("ssaBtnFilter", true).FirstOrDefault();
                if (staleFilter != null) staleFilter.Visible = false;
            }

            // Ẩn block "Quản lý nhóm / Cài đặt jobs" cũ
            if (tableTop != null) tableTop.Visible = false;

            // Ẩn icon buttons nhóm (add/rename/delete) để toolbar gọn
            foreach (var name in new[] { "button1", "button2", "button3" })
            {
                var b = GetField<System.Windows.Forms.Button>(uc, name);
                if (b != null) b.Visible = false;
            }
            var label2 = GetField<System.Windows.Forms.Label>(uc, "label2");
            if (label2 != null) label2.Visible = false;

            if (panel4 == null) return;

            // Toolbar container setup
            panel4.Location = new Point(Spacing.Lg, Spacing.Lg);
            panel4.Dock     = DockStyle.Top;
            panel4.Height   = 72;
            panel4.Padding  = new Padding(Spacing.Md, Spacing.Md, Spacing.Md, Spacing.Md);

            // Re-parent select1 (nhóm) vào panel4
            if (select1 != null && select1.Parent != panel4)
                panel4.Controls.Add(select1);

            // Re-parent 3 button action (Jobs / Cài đặt chung / Tương tác) vào panel4
            foreach (var b in new[] { btnJobs, btnSettings, btnInteract })
            {
                if (b != null && b.Parent != panel4)
                    panel4.Controls.Add(b);
            }

            // ── TOP toolbar (panel4) ────────────────────────────────────
            // Layout: [Run] [Stop] [Select nhóm] [Folder icon] [Jobs] [Cài đặt chung] [Tương tác] ... [Thêm TK]
            const int btnH = 36;
            int y = (panel4.Height - btnH) / 2;
            int x = Spacing.Md;

            // Run & Stop: overlap cùng vị trí (chỉ 1 button visible tại 1 thời điểm — do business logic toggle)
            if (btnRun != null)
            {
                btnRun.Anchor   = AnchorStyles.Left;
                btnRun.Location = new Point(x, y);
                btnRun.Size     = new Size(96, btnH);
            }
            if (btnStop != null)
            {
                btnStop.Anchor   = AnchorStyles.Left;
                btnStop.Location = new Point(x, y);
                btnStop.Size     = new Size(96, btnH);
                // Visible giữ nguyên theo business logic (ẩn khi chưa chạy)
            }
            if (btnRun != null) x += btnRun.Width + Spacing.Xs;
            if (cboScript != null)
            {
                cboScript.Anchor           = AnchorStyles.Left;
                cboScript.Location         = new Point(x, y);
                cboScript.Size             = new Size(180, btnH);
                cboScript.PlaceholderText  = "[ Chọn kịch bản ]";
                x += cboScript.Width + Spacing.Xs;
            }
            if (select1 != null)
            {
                select1.Anchor    = AnchorStyles.Left;
                select1.Location  = new Point(x, y);
                select1.Size      = new Size(180, btnH);
                select1.PlaceholderText = "[ Chọn nhóm ]";
                x += select1.Width + Spacing.Xs;
            }
            if (btnFolderMgr != null)
            {
                btnFolderMgr.Anchor   = AnchorStyles.Left;
                btnFolderMgr.Size     = new Size(38, btnH);
                btnFolderMgr.Location = new Point(x, y);
                x += btnFolderMgr.Width + Spacing.Lg;
            }

            // Secondary buttons (outline ghost) cho Settings/Tương tác.
            // Project mới ẩn hẳn "Cài đặt jobs" (btnJobs/button4) — không còn dùng
            // job-runner cho scope chỉ-Facebook newfeed/story.
            if (btnJobs != null) btnJobs.Visible = false;
            foreach (var (b, w) in new[]
            {
                (btnSettings, 140),
                (btnInteract, 110),
            })
            {
                if (b == null) continue;
                ButtonStyle.ApplySecondary(b);
                b.Anchor  = AnchorStyles.Top | AnchorStyles.Right;
                b.Size    = new Size(w, btnH);
                b.Visible = true;
            }

            // Right-anchored flow: từ phải sang trái
            // [... Jobs] [Cài đặt chung] [Tương tác] [Thêm TK →|]
            int rxTop = panel4.Width - Spacing.Md;

            if (btnAdd != null)
            {
                btnAdd.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
                btnAdd.Size     = new Size(160, btnH);
                rxTop -= btnAdd.Width;
                btnAdd.Location = new Point(rxTop, y);
                rxTop -= Spacing.Sm;
            }
            if (btnInteract != null)
            {
                rxTop -= btnInteract.Width;
                btnInteract.Location = new Point(rxTop, y);
                rxTop -= Spacing.Sm;
            }
            if (btnSettings != null)
            {
                rxTop -= btnSettings.Width;
                btnSettings.Location = new Point(rxTop, y);
                rxTop -= Spacing.Sm;
            }
            // btnJobs đã bị ẩn ở khối trên — không chiếm chỗ trong flow right-anchored.

            // ── LIST header (panel6) — hàng dưới: filter + search + cột hiển thị ──
            RelayoutListHeader(uc, input6, btnColumns);

            // Kéo panel5 (grid) lên sát toolbar
            var panel5 = GetField<AntdUI.Panel>(uc, "panel5");
            if (panel5 != null) panel5.Dock = DockStyle.Fill;
        }

        /// <summary>
        /// Sắp xếp lại panel6 (list header) theo Figma:
        /// [Danh sách tài khoản] ... [cboFilterAccount] [Search] [Reload] [Columns icon thay button17]
        /// </summary>
        private static void RelayoutListHeader(ucdgvAccount uc, Input? input6, AntdUI.Button? btnColumns)
        {
            var panel6 = GetField<AntdUI.Panel>(uc, "panel6");
            var panel7 = GetField<AntdUI.Panel>(uc, "panel7");
            var cboFilter = GetField<AntdUI.SelectMultiple>(uc, "cboFilterAccount");
            var btnReload = GetField<AntdUI.Button>(uc, "button9");
            var btnEye    = GetField<System.Windows.Forms.Button>(uc, "button17");
            var btnDensity = EnsureDensityButton(uc);

            if (panel7 == null) return;

            // Re-parent tất cả control vào panel7 (fix: cboFilter trước đó không được re-parent)
            if (cboFilter != null && cboFilter.Parent != panel7)
                panel7.Controls.Add(cboFilter);

            if (input6 != null && input6.Parent != panel7)
                panel7.Controls.Add(input6);

            if (btnColumns != null && btnColumns.Parent != panel7)
                panel7.Controls.Add(btnColumns);

            if (btnDensity != null && btnDensity.Parent != panel7)
                panel7.Controls.Add(btnDensity);

            // Ẩn button17 "con mắt" cũ (giữ để reference code, handler click sẽ được forward)
            if (btnEye != null) btnEye.Visible = false;

            const int h = 36;
            int py = Spacing.Md;

            // Tổng chiều rộng cần cho 5 control + 4 gap
            // [cboFilter 180] [search 240] [reload 100] [density 38] [columns 120]
            const int colsW    = 180;
            const int searchW  = 240;
            const int reloadW  = 100;
            const int densityW = 38;
            const int columnsW = 120;
            int needW = colsW + searchW + reloadW + densityW + columnsW + Spacing.Sm * 4 + Spacing.Md * 2;

            // Panel7 Dock=Right; mở rộng Width đủ chỗ cho toàn bộ controls
            if (panel7.Width < needW) panel7.Width = needW;

            // Layout right-anchored: [cboFilter] [search] [reload] [columns]
            int rx = panel7.Width - Spacing.Md;

            if (btnColumns != null)
            {
                btnColumns.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
                btnColumns.Size     = new Size(120, h);
                btnColumns.IconSvg  = "InsertRowAboveOutlined";
                btnColumns.Text     = "Hiển thị";
                rx -= btnColumns.Width;
                btnColumns.Location = new Point(rx, py);
                rx -= Spacing.Sm;
            }
            if (btnReload != null)
            {
                btnReload.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
                btnReload.Size     = new Size(100, h);
                rx -= btnReload.Width;
                btnReload.Location = new Point(rx, py);
                rx -= Spacing.Sm;
            }
            if (btnDensity != null)
            {
                btnDensity.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
                btnDensity.Size     = new Size(38, h);
                rx -= btnDensity.Width;
                btnDensity.Location = new Point(rx, py);
                rx -= Spacing.Sm;
            }
            if (input6 != null)
            {
                input6.Anchor            = AnchorStyles.Top | AnchorStyles.Right;
                input6.Size              = new Size(240, h);
                input6.PlaceholderText   = "Tìm kiếm theo UID, tên…";
                input6.PrefixSvg         = "SearchOutlined";
                rx -= input6.Width;
                input6.Location          = new Point(rx, py);
                rx -= Spacing.Sm;
            }
            if (cboFilter != null)
            {
                cboFilter.Anchor          = AnchorStyles.Top | AnchorStyles.Right;
                cboFilter.Size            = new Size(180, h);
                cboFilter.Visible         = true;
                cboFilter.PlaceholderText = "Lọc tài khoản";
                rx -= cboFilter.Width;
                cboFilter.Location        = new Point(rx, py);
            }
        }

        private static void TuneAccountCards(ucdgvAccount uc)
        {
            // panel2: Quản lý nhóm
            var panel2 = GetField<AntdUI.Panel>(uc, "panel2");
            StyleCardPanel(panel2);

            // panel3: Cài đặt jobs / chung / tương tác
            var panel3 = GetField<AntdUI.Panel>(uc, "panel3");
            StyleCardPanel(panel3);

            // panel4: Run / Stop / Search / Add
            var panel4 = GetField<AntdUI.Panel>(uc, "panel4");
            StyleCardPanel(panel4);

            // panel5: Grid card
            var panel5 = GetField<AntdUI.Panel>(uc, "panel5");
            StyleCardPanel(panel5);

            // panel6: List header card
            var panel6 = GetField<AntdUI.Panel>(uc, "panel6");
            StyleCardPanel(panel6, subtle: true);

            // panel7: Filter area (inside panel6)
            var panel7 = GetField<AntdUI.Panel>(uc, "panel7");
            if (panel7 != null)
            {
                panel7.Back      = ColorPalette.Surface;
                panel7.BackColor = Color.Transparent;
            }

            // label2 "Quản lý nhóm"
            var label2 = GetField<System.Windows.Forms.Label>(uc, "label2");
            if (label2 != null)
            {
                label2.Font      = FontScale.Caption8;
                label2.ForeColor = ColorPalette.TextTertiary;
            }
        }

        private static void StyleCardPanel(AntdUI.Panel p, bool subtle = false)
        {
            if (p == null) return;
            p.Back      = ColorPalette.Surface;
            p.BackColor = Color.Transparent;
            p.Radius    = subtle ? Radius.Md : Radius.Lg;
            p.ForeColor = ColorPalette.TextPrimary;
        }

        private static void TuneAccountToolbar(ucdgvAccount uc)
        {
            // select1 (Folder select)
            var select1 = GetField<Select>(uc, "select1");
            if (select1 != null)
            {
                select1.Font   = FontScale.Body9;
                select1.Radius = Radius.Md;
            }

            // panel3 inner buttons: cài đặt jobs / chung / tương tác — secondary outline
            foreach (var name in new[] { "button4", "button5", "button6" })
            {
                var b = GetField<AntdUI.Button>(uc, name);
                ButtonStyle.ApplySecondary(b);
            }

            // panel4: Run / Stop / Search / Add-Account
            var btnRun  = GetField<AntdUI.Button>(uc, "button7");
            var btnStop = GetField<AntdUI.Button>(uc, "button8");
            var btnAdd  = GetField<AntdUI.Button>(uc, "button16");
            var input6  = GetField<Input>(uc, "input6");

            // Visual hierarchy: Run = Success (primary action), Stop = Danger, Add = Accent (secondary CTA)
            ButtonStyle.ApplySuccess(btnRun);
            ButtonStyle.ApplyDanger(btnStop);
            ButtonStyle.ApplyAccent(btnAdd);
            if (input6 != null)
            {
                input6.Radius      = Radius.Md;
                input6.Font        = FontScale.Body9;
                input6.PlaceholderColor = ColorPalette.TextTertiary;
            }
        }

        private static void TuneAccountListHeader(ucdgvAccount uc)
        {
            // label3 "Danh sách tài khoản"
            var label3 = GetField<System.Windows.Forms.Label>(uc, "label3");
            if (label3 != null)
            {
                label3.Font      = FontScale.SectionBold;
                label3.ForeColor = ColorPalette.TextPrimary;
            }

            // Combo lọc tài khoản
            var cbo = GetField<AntdUI.SelectMultiple>(uc, "cboFilterAccount");
            if (cbo != null)
            {
                cbo.Font   = FontScale.Body9;
                cbo.Radius = Radius.Md;
            }

            // button9 "Tải lại" — primary blue
            ButtonStyle.ApplyPrimary(GetField<AntdUI.Button>(uc, "button9"));

            // Count chips toolStrip2 (Live/Die/Khác)
            var ts2 = GetField<ToolStrip>(uc, "toolStrip2");
            if (ts2 != null)
            {
                ts2.BackColor = ColorPalette.Surface;
                ts2.Font      = FontScale.Body9;
                ts2.Padding   = new Padding(Spacing.Sm, 0, Spacing.Sm, 0);
                ts2.AutoSize  = true;

                // Ép AutoSize cho từng label để không bị truncate "Trường hợp khác:"
                foreach (ToolStripItem it in ts2.Items)
                {
                    it.AutoSize = true;
                }
            }

            // button13/14/15 legacy (ngoài màn hình) – ẩn theo SSA (duplicate actions)
            foreach (var name in new[] { "button13", "button14", "button15", "button10", "button11", "button12" })
            {
                var b = GetField<AntdUI.Button>(uc, name);
                if (b != null) b.Visible = false;
            }
        }

        private static void TuneAccountGrid(ucdgvAccount uc)
        {
            var dgv = GetField<DataGridView>(uc, "dataGridView1");
            if (dgv == null) return;

            // GridStyleHelper.Apply() đã được ctor gọi; đây là các tinh chỉnh bổ sung
            // Figma-style: bỏ hẳn cell border (không vertical + không horizontal)
            dgv.CellBorderStyle        = DataGridViewCellBorderStyle.None;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.RowTemplate.Height     = TableDensity.RowHeight(TableDensity.Current);
            dgv.GridColor              = ColorPalette.BorderLight;
            dgv.ColumnHeadersHeight    = TableDensity.HeaderHeight(TableDensity.Current);
            dgv.BorderStyle            = BorderStyle.None;

            // Subscribe ModeChanged để re-apply khi user toggle
            EventHandler<TableDensityMode> handler = (_, mode) =>
            {
                if (dgv.IsDisposed) return;
                TableDensity.Apply(dgv, mode);
            };
            TableDensity.ModeChanged += handler;
            dgv.Disposed += (_, __) => TableDensity.ModeChanged -= handler;
            dgv.BackgroundColor        = ColorPalette.Surface;
            dgv.EnableHeadersVisualStyles = false;
            // Bỏ alternating rows — Figma dùng flat white background
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle();
            // GDI TextRenderer → text crisp, Segoe UI hết răng cưa
            dgv.RowTemplate.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

            // Header style — Figma: uppercase tracking, gray-7 text, no selection tint
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor          = ColorPalette.Surface,
                ForeColor          = ColorPalette.TextTertiary,
                Font               = FontScale.Body9Bold,
                Alignment          = DataGridViewContentAlignment.MiddleLeft,
                Padding            = new Padding(Spacing.Md, 0, Spacing.Md, 0),
                SelectionBackColor = ColorPalette.Surface,
                SelectionForeColor = ColorPalette.TextTertiary,
                WrapMode           = DataGridViewTriState.False
            };

            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor          = ColorPalette.Surface,
                ForeColor          = ColorPalette.TextPrimary,
                Font               = FontScale.Body9,
                Alignment          = DataGridViewContentAlignment.MiddleLeft,
                Padding            = new Padding(Spacing.Md, 0, Spacing.Md, 0),
                SelectionBackColor = ColorPalette.RowSelected,
                SelectionForeColor = ColorPalette.TextPrimary,
                WrapMode           = DataGridViewTriState.False
            };

            // Uppercase header text (Figma style: STT / UID / HỌ TÊN / KỊCH BẢN / XU / TRẠNG THÁI)
            UppercaseHeaders(dgv);

            // Figma parity adjustments: widen UID, hide duplicate State column
            TuneAccountGridColumns(dgv);
        }

        /// <summary>
        /// Column-level tweaks theo Figma:
        /// - UID: widen tối thiểu 160px để không truncate.
        /// - State (Tình trạng): ẩn do trùng ý nghĩa với Status (Trạng thái).
        /// - Groups: đổi header "Nhóm" → "Số nhóm" để tránh trùng với NameFolder.
        /// - Id/ColorType/Running: force ẩn cứng (luôn hidden, không cho user bật).
        ///   User vẫn có thể bật lại State qua dialog "Cột hiển thị".
        /// Idempotent — chỉ áp dụng khi column tồn tại.
        /// </summary>
        private static void TuneAccountGridColumns(DataGridView dgv)
        {
            // Internal columns luôn ẩn — user không nên thấy
            var forceHidden = new HashSet<string>
            {
                "col_Id", "col_ColorType", "col_Running"
            };

            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col == null) continue;

                // UID column: "col_Uid" (pattern từ CreateColumnsDataGridView)
                if (col.Name == "col_Uid")
                {
                    col.MinimumWidth = 160;
                    if (col.Width < 160) col.Width = 160;
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                }

                // State column (Tình trạng) — trùng với Status
                if (col.Name == "col_State")
                {
                    col.Visible = false;
                }

                // Groups "Nhóm" trùng với NameFolder "Nhóm" → rename
                if (col.Name == "col_Groups")
                {
                    col.HeaderText = "SỐ NHÓM";
                }

                // Force-hide internal columns
                if (forceHidden.Contains(col.Name))
                {
                    col.Visible = false;
                }
            }
        }

        /// <summary>
        /// Chuyển header text sang UPPERCASE (chỉ display — original HeaderText giữ nguyên
        /// để không ảnh hưởng logic export/copy dựa trên HeaderText).
        /// </summary>
        private static void UppercaseHeaders(DataGridView dgv)
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col == null || col.HeaderText == null) continue;
                // Skip columns có header là tên property thuần (ẩn) hoặc ký hiệu ngắn
                if (col.HeaderText == "#") continue;
                string original = col.HeaderText;
                string upper = original.ToUpperInvariant();
                if (upper != original) col.HeaderText = upper;
            }
        }

        private static void TuneAccountFooter(ucdgvAccount uc)
        {
            var ts1 = GetField<ToolStrip>(uc, "toolStrip1");
            if (ts1 == null) return;

            ts1.BackColor = ColorPalette.Surface;
            ts1.Font      = FontScale.Body9;
            ts1.Padding   = new Padding(Spacing.Lg, Spacing.Sm, Spacing.Lg, Spacing.Sm);
            ts1.ImageScalingSize = new Size(14, 14);

            // Consistent label styling — pair label (gray) + value (bold colored)
            foreach (ToolStripItem it in ts1.Items)
            {
                if (it is ToolStripLabel lbl)
                {
                    // Labels ending with ':' are descriptor labels → gray tertiary
                    if (lbl.Text?.TrimEnd().EndsWith(':') == true)
                    {
                        lbl.Font      = FontScale.Body9;
                        lbl.ForeColor = ColorPalette.TextTertiary;
                        lbl.Margin    = new Padding(Spacing.Sm, 0, Spacing.Xs, 0);
                    }
                    else
                    {
                        // Value label → bold, preserve semantic color if already colored
                        lbl.Font   = FontScale.Body9Bold;
                        lbl.Margin = new Padding(0, 0, Spacing.Md, 0);
                    }
                }
                else
                {
                    it.Font = FontScale.Body9;
                }
            }

            // Insert divider separators between stat groups (Bôi đen | Đã chọn | Đã chạy | Hôm nay)
            InsertFooterSeparators(ts1);
        }

        /// <summary>
        /// Chèn ToolStripSeparator (visual divider) giữa các cặp label-value trong footer.
        /// Idempotent — không chèn nếu đã có.
        /// </summary>
        private static void InsertFooterSeparators(ToolStrip ts)
        {
            // Nếu đã chèn separator rồi thì skip (idempotent khi Apply chạy lại)
            if (ts.Items.OfType<ToolStripSeparator>().Any()) return;

            var items = ts.Items.Cast<ToolStripItem>().ToList();
            ts.Items.Clear();

            // Heuristic: mỗi khi gặp label kết thúc bằng ':' (descriptor mới) và
            // đã có ít nhất 1 label trước đó → chèn separator
            bool sawValue = false;
            foreach (var it in items)
            {
                if (it is ToolStripLabel lbl && lbl.Text?.TrimEnd().EndsWith(':') == true && sawValue)
                {
                    var sep = new ToolStripSeparator
                    {
                        ForeColor = ColorPalette.BorderLight,
                        Margin    = new Padding(Spacing.Sm, 0, Spacing.Sm, 0)
                    };
                    ts.Items.Add(sep);
                    sawValue = false;
                }
                ts.Items.Add(it);
                if (it is ToolStripLabel l2 && l2.Text?.TrimEnd().EndsWith(':') != true)
                    sawValue = true;
            }
        }

        /// <summary>
        /// Mount EmptyStateView overlay lên grid card (panel5).
        /// Toggle visibility theo dgv.Rows.Count: > 0 thì ẩn, = 0 thì hiển thị.
        /// CTA → click vào btnAdd (button16) — tận dụng handler cũ.
        /// </summary>
        private static void StyleEmptyStatePanel(ucdgvAccount uc)
        {
            var dgv = GetField<DataGridView>(uc, "dataGridView1");
            var panel5 = GetField<AntdUI.Panel>(uc, "panel5");
            var btnAdd = GetField<AntdUI.Button>(uc, "button16");
            if (dgv == null || panel5 == null) return;

            const string key = "ssaEmptyState";
            if (panel5.Controls.Find(key, true).Length > 0) return;

            var es = new EmptyStateView
            {
                Name     = key,
                Title    = "Chưa có tài khoản nào",
                Subtitle = "Bắt đầu farm tự động trong 4 bước:",
                Steps    = new[]
                {
                    "Kết nối thiết bị Android",
                    "Thêm tài khoản Facebook/Instagram/Threads",
                    "Chọn kịch bản farm",
                    "Nhấn \"Chạy\" để bắt đầu",
                },
                CtaText  = "Thêm tài khoản",
                Visible  = false,
            };
            es.CtaClicked += (_, __) =>
            {
                if (btnAdd != null) btnAdd.PerformClick();
            };

            panel5.Controls.Add(es);
            es.BringToFront();

            void Toggle()
            {
                if (dgv.IsDisposed) return;
                // VirtualMode: bind RowCount tăng dần / không fire RowsAdded đúng lúc —
                // ucdgvAccount.SyncEmptyStateOverlay() là nguồn sự thật, tránh overlay che grid chặn click.
                if (dgv.VirtualMode) return;
                es.Visible = dgv.RowCount == 0;
                if (es.Visible)
                    es.BringToFront();
                else
                    dgv.BringToFront();
            }

            if (!dgv.VirtualMode)
            {
                dgv.RowsAdded += (_, __) => Toggle();
                dgv.RowsRemoved += (_, __) => Toggle();
                dgv.DataBindingComplete += (_, __) => Toggle();
                dgv.HandleCreated += (_, __) => Toggle();
                dgv.VisibleChanged += (_, __) => Toggle();
                Toggle();
            }
            else
            {
                es.Visible = false;
                dgv.BringToFront();
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  Toolbar helpers: ensure extra SSA buttons exist + wire handlers
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tạo/reuse AntdUI.Select "Kịch bản" trên toolbar panel4.
        /// Items: "Chạy theo kịch bản" + danh sách Name script từ DB.
        /// Idempotent — chạy Apply nhiều lần không duplicate control.
        /// </summary>
        private const string CustomScriptOption = "Chạy theo kịch bản";
        private const string CustomScriptOptionLegacy = "Tùy chọn";
        private const string LegacyQNScript = "Làm Job QN";
        private const string LegacyQNScriptOld = "Farm-Xu-VIP";

        private static AntdUI.Select? EnsureScriptSelect(ucdgvAccount uc)
        {
            var panel4 = GetField<AntdUI.Panel>(uc, "panel4");
            if (panel4 == null) return null;

            const string key = "ssaCboScript";
            var existing = panel4.Controls.Find(key, true).FirstOrDefault() as AntdUI.Select;
            if (existing != null)
            {
                ReloadScriptSelectItems(uc, existing);
                return existing;
            }

            var cbo = new AntdUI.Select
            {
                Name   = key,
                Font   = FontScale.Body9,
                Radius = Radius.Md,
                List   = true,
            };
            panel4.Controls.Add(cbo);
            ReloadScriptSelectItems(uc, cbo);

            // Default "Chạy theo kịch bản" khi chưa có selection
            SelectDefaultCustomIfAvailable(cbo);

            // Flag: chỉ apply khi user thực sự thay đổi (bỏ qua initial set sau Load)
            bool ucLoaded = false;
            uc.Load += (_, __) =>
            {
                SelectDefaultCustomIfAvailable(cbo);
                ucLoaded = true;
            };

            // Mass-apply NameScript khi đổi selection:
            //  - "Chạy theo kịch bản" → RestoreScriptsFromDb (reload giá trị gốc cho từng row)
            //  - Script khác          → ApplyScriptToAll(name). Token QN lấy từ login (bỏ popup).
            cbo.SelectedIndexChanged += (_, __) =>
            {
                if (!ucLoaded) return;
                var text = cbo.Text?.Trim();
                if (string.IsNullOrEmpty(text)) return;

                if (string.Equals(text, CustomScriptOption, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(text, CustomScriptOptionLegacy, StringComparison.OrdinalIgnoreCase))
                {
                    InvokeUcMethod(uc, "RestoreScriptsFromDb");
                    return;
                }

                InvokeUcMethod(uc, "ApplyScriptToAll", text);
            };

            return cbo;
        }

        private static void SelectDefaultCustomIfAvailable(AntdUI.Select cbo)
        {
            if (cbo.SelectedIndex >= 0) return;
            if (cbo.Items.Contains(CustomScriptOption))
                cbo.SelectedIndex = cbo.Items.IndexOf(CustomScriptOption);
        }

        private static void InvokeUcMethod(ucdgvAccount uc, string methodName, params object[] args)
        {
            try
            {
                var mi = uc.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
                mi?.Invoke(uc, args);
            }
            catch { /* silent */ }
        }

        /// <summary>
        /// Nạp items: defaults + danh sách Name script lấy qua _scriptContext.GetByPlatform().
        /// Reflection để tránh thêm dependency từ SsaTheme sang Sunny.Subdy.Data.
        /// </summary>
        private static void ReloadScriptSelectItems(ucdgvAccount uc, AntdUI.Select cbo)
        {
            cbo.Items.Clear();
            cbo.Items.Add(CustomScriptOption);

            try
            {
                var ctx = uc.GetType()
                    .GetField("_scriptContext", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(uc);
                var platform = GetField<string>(uc, "_platform") ?? "";
                if (ctx == null) return;

                var mi = ctx.GetType().GetMethod("GetByPlatform");
                if (mi == null) return;
                var scripts = mi.Invoke(ctx, new object[] { platform }) as System.Collections.IEnumerable;
                if (scripts == null) return;

                foreach (var s in scripts)
                {
                    var name = s?.GetType().GetProperty("Name")?.GetValue(s) as string;
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    // Ẩn kịch bản legacy "Làm Job QN" / "Farm-Xu-VIP" khỏi dropdown.
                    if (string.Equals(name, LegacyQNScript, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(name, LegacyQNScriptOld, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!cbo.Items.Contains(name)) cbo.Items.Add(name);
                }
            }
            catch
            {
                // silent — dropdown vẫn có "Chạy theo kịch bản"
            }
        }

        /// <summary>
        /// Reload lại danh sách kịch bản trong combobox toolbar (ssaCboScript) mà không cần
        /// re-apply toàn bộ theme. Gọi sau khi tạo/sửa/xóa kịch bản để combobox cập nhật ngay
        /// (không phải tắt mở lại tool). Giữ nguyên selection hiện tại nếu vẫn còn trong list.
        /// </summary>
        public static void RefreshScriptSelect(ucdgvAccount uc)
        {
            if (uc == null) return;
            try
            {
                var panel4 = GetField<AntdUI.Panel>(uc, "panel4");
                var cbo = panel4?.Controls.Find("ssaCboScript", true).FirstOrDefault() as AntdUI.Select;
                if (cbo == null) return;

                var prev = cbo.Text?.Trim();
                ReloadScriptSelectItems(uc, cbo);

                if (!string.IsNullOrEmpty(prev) && cbo.Items.Contains(prev))
                    cbo.SelectedIndex = cbo.Items.IndexOf(prev);
                else
                    SelectDefaultCustomIfAvailable(cbo);
            }
            catch { /* silent */ }
        }

        /// <summary>
        /// Ghost button chuẩn SSA — dùng cho các hành động phụ trên toolbar.
        /// Tạo 1 lần, attach vào panel4; re-apply style nếu đã tồn tại.
        /// </summary>
        private static AntdUI.Button EnsureToolbarButton(ucdgvAccount uc, string key, string iconSvg, string? text)
        {
            var panel4 = GetField<AntdUI.Panel>(uc, "panel4");
            if (panel4 == null) return null!;

            // Reuse nếu đã có từ lần trước (trường hợp Apply chạy 2 lần)
            var existing = panel4.Controls.Find(key, true).FirstOrDefault() as AntdUI.Button;
            if (existing != null) return ConfigureGhostToolbarButton(existing, iconSvg, text);

            var btn = new AntdUI.Button
            {
                Name = key,
            };
            ConfigureGhostToolbarButton(btn, iconSvg, text);
            panel4.Controls.Add(btn);

            // Wire default handlers
            if (key == "ssaBtnFolderMgr") btn.Click += (_, __) => OpenFolderManager(uc);
            if (key == "ssaBtnColumns")   btn.Click += (_, __) => ForwardToEyeButton(uc);

            return btn;
        }

        private static AntdUI.Button ConfigureGhostToolbarButton(AntdUI.Button btn, string iconSvg, string? text)
        {
            btn.Ghost       = true;
            btn.BorderWidth = 1F;
            btn.Radius      = Radius.Md;
            btn.Shape       = TShape.Default;
            btn.Font        = FontScale.Body9Bold;
            btn.ForeColor   = ColorPalette.TextSecondary;
            btn.IconSvg     = iconSvg;
            if (!string.IsNullOrEmpty(text)) btn.Text = text;
            return btn;
        }

        /// <summary>
        /// Density toggle button — cycle Normal → Compact → Ultra → Normal.
        /// Tooltip shows current mode; icon-only (icon-only ghost = tertiary hierarchy).
        /// </summary>
        private static AntdUI.Button? EnsureDensityButton(ucdgvAccount uc)
        {
            const string key = "ssaBtnDensity";
            var panel7 = GetField<AntdUI.Panel>(uc, "panel7");
            if (panel7 == null) return null;

            var existing = panel7.Controls.Find(key, true).FirstOrDefault() as AntdUI.Button;
            if (existing != null)
            {
                UpdateDensityButtonVisual(existing);
                return existing;
            }

            var btn = new AntdUI.Button { Name = key };
            ConfigureGhostToolbarButton(btn, "ColumnHeightOutlined", null);
            UpdateDensityButtonVisual(btn);

            var tip = new ToolTip { AutoPopDelay = 4000, InitialDelay = 250 };
            tip.SetToolTip(btn, $"Mật độ bảng: {TableDensity.Label(TableDensity.Current)}\n(click để chuyển)");

            btn.Click += (_, __) =>
            {
                TableDensity.Current = TableDensity.Cycle(TableDensity.Current);
                UpdateDensityButtonVisual(btn);
                tip.SetToolTip(btn, $"Mật độ bảng: {TableDensity.Label(TableDensity.Current)}\n(click để chuyển)");
            };

            return btn;
        }

        private static void UpdateDensityButtonVisual(AntdUI.Button btn)
        {
            // Icon thay đổi theo mode để feedback rõ ràng
            btn.IconSvg = TableDensity.Current switch
            {
                TableDensityMode.Compact => "VerticalAlignMiddleOutlined",
                TableDensityMode.Ultra   => "MinusOutlined",
                _                        => "ColumnHeightOutlined",
            };
        }

        /// <summary>
        /// Mở dialog Quản lý nhóm (Add/Edit/Delete). Sau khi đóng, reload select1 của uc.
        /// </summary>
        private static void OpenFolderManager(ucdgvAccount uc)
        {
            var form = uc.FindForm();
            if (form == null) return;

            // Platform lấy từ field _platform của uc
            var platform = GetField<string>(uc, "_platform") ?? "";

            Func<Task> reload = async () =>
            {
                var mi = uc.GetType().GetMethod("LoadFolders",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (mi != null)
                {
                    var task = mi.Invoke(uc, new object[] { true }) as Task;
                    if (task != null) await task;
                }
            };

            using var panel = new FolderManagerPanel(platform, reload, form);
            AntdUI.Modal.open(form, panel);
        }

        /// <summary>
        /// Forward click "Cột hiển thị" sang button17 (con mắt cũ) — dùng lại dialog gốc.
        /// button17 đã bị ẩn nhưng vẫn Visible=true ở tổ tiên; nếu PerformClick no-op thì
        /// invoke trực tiếp handler qua reflection.
        /// </summary>
        private static void ForwardToEyeButton(ucdgvAccount uc)
        {
            var eye = GetField<System.Windows.Forms.Button>(uc, "button17");
            if (eye == null) return;

            // Invoke trực tiếp handler button17_Click (không phụ thuộc CanSelect)
            var mi = uc.GetType().GetMethod("button17_Click",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (mi != null)
                mi.Invoke(uc, new object[] { eye, EventArgs.Empty });
            else
                eye.PerformClick();
        }

        // ══════════════════════════════════════════════════════════════════
        //  Status badge painter + text-crisp fix
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Vẽ badge cho cột State / Status (pill rounded, color theo semantic).
        /// Không đụng business data — chỉ override CellPainting để đổi visual.
        /// </summary>
        private static void AttachStatusBadgePainter(ucdgvAccount uc)
        {
            uc.EnsureStatusBadgePainter();
        }

        internal static void PaintAccountStatusBadge(DataGridView dgv, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var col = dgv.Columns[e.ColumnIndex];
            var propName = col?.DataPropertyName;
            if (propName != "State" && propName != "Status") return;

            string text = e.Value?.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(text))
            {
                // để default painter vẽ cell trống
                return;
            }

            bool selected = e.State.HasFlag(DataGridViewElementStates.Selected);

            // Paint full cell background (bao gồm cả vùng trailing khi cột Fill)
            e.PaintBackground(e.CellBounds, selected);

            // Khi row được chọn → vẽ text plain (không badge) trên nền selection
            if (selected)
            {
                var textRectSel = e.CellBounds;
                textRectSel.Inflate(-Spacing.Md, 0);
                TextRenderer.DrawText(e.Graphics, text, _badgeFont, textRectSel, Color.White,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                e.Handled = true;
                return;
            }

            // Semantic mapping cho badge pill (chỉ khi không selected)
            var (bg, fg) = MapBadgeColor(text);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var textSize = TextRenderer.MeasureText(e.Graphics, text, _badgeFont, Size.Empty, TextFormatFlags.NoPadding);

            int padX = Spacing.Sm;
            int padY = 3;
            int badgeW = textSize.Width + padX * 2;
            int badgeH = textSize.Height + padY * 2;

            var cellRect = e.CellBounds;
            var badgeRect = new Rectangle(
                cellRect.Left + Spacing.Md,
                cellRect.Top + (cellRect.Height - badgeH) / 2,
                badgeW,
                badgeH);

            e.Graphics.FillPath(BadgeBrush(bg), RoundedRectShared(badgeRect, Radius.Md));

            TextRenderer.DrawText(e.Graphics, text, _badgeFont, badgeRect, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            e.Handled = true;
        }

        private static (Color bg, Color fg) MapBadgeColor(string text)
        {
            string t = text.ToUpperInvariant();

            // LIVE / hoạt động → success (green pill)
            if (t == "LIVE" || t.Contains("HOẠT ĐỘNG") || t.Contains("ACTIVE") || t.Contains("ĐANG CHẠY"))
                return (ColorPalette.SuccessBg, ColorPalette.Success);

            // DIE → error strong (red pill)
            if (t == "DIE" || t.Contains("DIE"))
                return (ColorPalette.ErrorBg, ColorPalette.Error);

            // Checkpoint / CP_xxx / bị chặn → error (red pill)
            if (t.StartsWith("CP_") || t.Contains("CHECKPOINT") || t.Contains("BỊ CHẶN"))
                return (ColorPalette.ErrorBg, ColorPalette.Error);

            // Captcha / cảnh báo → warning (yellow pill)
            if (t.Contains("CAPTCHA") || t.Contains("CẢNH BÁO") || t.Contains("WARN"))
                return (ColorPalette.WarningBg, ColorPalette.Warning);

            // Lỗi chung → error
            if (t.Contains("LỖI") || t.Contains("ERROR") || t.Contains("FAIL"))
                return (ColorPalette.ErrorBg, ColorPalette.Error);

            // Đăng xuất / logout → violet pill (đồng bộ với row foreColor)
            if (t.Contains("ĐĂNG XUẤT") || t.Contains("LOGOUT"))
                return (Color.FromArgb(243, 232, 255), Color.FromArgb(139, 92, 246));

            // Đã dừng / chưa xác định → neutral
            if (t.Contains("ĐÃ DỪNG") || t.Contains("CHƯA") || t.Contains("DỪNG"))
                return (ColorPalette.BorderLight, ColorPalette.TextSecondary);

            // default → info (blue pill)
            return (ColorPalette.PrimaryBg, ColorPalette.Primary);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>Như RoundedRect nhưng dùng lại 1 GraphicsPath dùng chung (chỉ gọi trên UI thread).
        /// KHÔNG dispose giá trị trả về. Tránh cấp phát path mỗi ô khi vẽ badge lúc cuộn.</summary>
        private static GraphicsPath RoundedRectShared(Rectangle r, int radius)
        {
            var path = _badgePath;
            path.Reset();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Ép các Label/Button legacy dùng GDI TextRenderer (UseCompatibleTextRendering=false)
        /// → text crispy, không bị GDI+ smoothing làm mờ.
        /// </summary>
        private static void ForceCrispTextOnLegacyControls(Control root)
        {
            if (root == null) return;
            if (root is System.Windows.Forms.Label lbl)
                lbl.UseCompatibleTextRendering = false;
            if (root is System.Windows.Forms.Button btn)
                btn.UseCompatibleTextRendering = false;
            if (root is System.Windows.Forms.CheckBox cb)
                cb.UseCompatibleTextRendering = false;

            foreach (Control c in root.Controls)
                ForceCrispTextOnLegacyControls(c);
        }

        // ══════════════════════════════════════════════════════════════════
        //  fQuanLyKichBan — reskin tokens, zero business-logic impact
        // ══════════════════════════════════════════════════════════════════
        public static void ApplyFQuanLyKichBan(Form form)
        {
            if (form == null) return;
            AntdUI.Config.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            form.BackColor = ColorPalette.Background;
            form.Font      = FontScale.Body9;

            // PageHeader
            var bar = GetField<PageHeader>(form, "windowBar");
            if (bar != null)
            {
                bar.BackColor = ColorPalette.Surface;
                bar.ForeColor = ColorPalette.TextPrimary;
                bar.Font      = FontScale.SectionBold;
                bar.SubFont   = FontScale.Caption8;
                bar.UseTextBold = true;
            }

            // panel4 settings card + relayout 2-column responsive
            var panel4 = GetField<AntdUI.Panel>(form, "panel4");
            if (panel4 != null)
            {
                panel4.Back    = ColorPalette.Surface;
                panel4.Radius  = Radius.Lg + 4;
                panel4.padding = new Padding(Spacing.Xl, Spacing.Lg, Spacing.Xl, Spacing.Lg);
                panel4.Height  = 280;

                RelayoutQuanLyKichBanPanel4(form, panel4);
            }

            // Warning labels → token Error
            foreach (var name in new[] { "lblCanhBao1", "lblCanhBao2" })
            {
                var l = GetField<System.Windows.Forms.Label>(form, name);
                if (l == null) continue;
                l.ForeColor = ColorPalette.Error;
                l.Font      = FontScale.Body9;
            }

            var link = GetField<LinkLabel>(form, "llbHuongDan");
            if (link != null)
            {
                link.Font            = FontScale.Body9Bold;
                link.LinkColor       = ColorPalette.Primary;
                link.ActiveLinkColor = ColorPalette.PrimaryActive;
            }

            var vp = GetField<VirtualPanel>(form, "virtualPanel");
            if (vp != null) vp.BackColor = ColorPalette.Background;

            // Hidden controls → Visible=false (thay vì toạ độ 9999)
            foreach (var name in new[] { "input6", "button16" })
            {
                var c = GetField<Control>(form, name);
                if (c != null) c.Visible = false;
            }

            NormalizeLegacyControls(form);
            ForceCrispTextOnLegacyControls(form);
        }

        /// <summary>
        /// Rebuild panel4 layout bằng TableLayoutPanel 2 cột (idempotent).
        ///  Cột trái: 4 radio + dòng "Số lần lặp lại: [from]–[to] phút"
        ///  Cột phải: 4 checkbox, mỗi cái kèm dải cấu hình inline
        ///  Hàng cuối: warning labels + link hướng dẫn
        /// </summary>
        private static void RelayoutQuanLyKichBanPanel4(Form form, AntdUI.Panel panel4)
        {
            const string marker = "ssaQlkbGrid";
            if (panel4.Controls.Find(marker, false).FirstOrDefault() != null) return;

            // Resolve controls
            var rb1 = GetField<RadioButton>(form, "radioButton1");
            var rb2 = GetField<RadioButton>(form, "radioButton2");
            var rb3 = GetField<RadioButton>(form, "radioButton3");
            var rb4 = GetField<RadioButton>(form, "radioButton4");

            var lblSoLanLap = GetField<System.Windows.Forms.Label>(form, "lblSoLanLap");
            var nudSoLanLap = GetField<NumericUpDown>(form, "nudSoLanLap");
            var lblLuot     = GetField<System.Windows.Forms.Label>(form, "lblLuot");
            var lblChoLuot  = GetField<System.Windows.Forms.Label>(form, "lblChoLuot");
            var nudChoFrom  = GetField<NumericUpDown>(form, "nudChoLuotFrom");
            var lblDenLuot  = GetField<System.Windows.Forms.Label>(form, "lblDenLuot");
            var nudChoTo    = GetField<NumericUpDown>(form, "nudChoLuotTo");
            var lblPhutLuot = GetField<System.Windows.Forms.Label>(form, "lblPhutLuot");

            var cb1 = GetField<System.Windows.Forms.CheckBox>(form, "checkBox1");
            var cb2 = GetField<System.Windows.Forms.CheckBox>(form, "checkBox2");
            var cb3 = GetField<System.Windows.Forms.CheckBox>(form, "checkBox3");
            var cb4 = GetField<System.Windows.Forms.CheckBox>(form, "checkBox4");

            var nudTKFrom = GetField<NumericUpDown>(form, "nudTaiKhoanFrom");
            var lblDenTK  = GetField<System.Windows.Forms.Label>(form, "lblDenTaiKhoan");
            var nudTKTo   = GetField<NumericUpDown>(form, "nudTaiKhoanTo");
            var lblPhutTK = GetField<System.Windows.Forms.Label>(form, "lblPhutTaiKhoan");

            var nudKBFrom = GetField<NumericUpDown>(form, "nudKichBanFrom");
            var lblDenKB  = GetField<System.Windows.Forms.Label>(form, "lblDenKichBan");
            var nudKBTo   = GetField<NumericUpDown>(form, "nudKichBanTo");
            var lblPhutKB = GetField<System.Windows.Forms.Label>(form, "lblPhutKichBan");

            var lblThoiGian = GetField<System.Windows.Forms.Label>(form, "lblThoiGianBatDau");
            var tpFrom      = GetField<TimePicker>(form, "timepickerFrom");
            var lblDenNgay  = GetField<System.Windows.Forms.Label>(form, "lblDenNgay");
            var tpTo        = GetField<TimePicker>(form, "timepickerTo");

            var lblCanhBao1 = GetField<System.Windows.Forms.Label>(form, "lblCanhBao1");
            var lblCanhBao2 = GetField<System.Windows.Forms.Label>(form, "lblCanhBao2");
            var llb         = GetField<LinkLabel>(form, "llbHuongDan");

            // Root grid: 2 cột equal + hàng warning dưới (mỗi warning 1 dòng riêng)
            var grid = new TableLayoutPanel
            {
                Name        = marker,
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 4,
                BackColor   = Color.Transparent,
                Padding     = new Padding(0),
                Margin      = new Padding(0),
            };
            grid.ColumnStyles.Clear();
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grid.RowStyles.Clear();
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // 2-col content
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // divider
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // link + warning1
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // warning2

            // ── Cột trái: 4 radio + dòng "Số lần lặp lại" ──
            var left = new TableLayoutPanel
            {
                Dock         = DockStyle.Fill,
                ColumnCount  = 1,
                AutoSize     = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor    = Color.Transparent,
                Padding      = new Padding(Spacing.Xl, 0, Spacing.Xl, 0),
                Margin       = new Padding(0),
            };
            foreach (var rb in new[] { rb1, rb2, rb3, rb4 })
            {
                if (rb == null) continue;
                rb.AutoSize = true;
                rb.Location = Point.Empty;
                rb.Margin   = new Padding(0, Spacing.Sm, 0, Spacing.Sm);
                left.Controls.Add(rb);
            }

            // Row: "Số lần lặp lại: [n] lượt   Chờ lượt kế tiếp: [a]–[b] phút"
            var loopRow = BuildInlineRow(
                lblSoLanLap, nudSoLanLap, lblLuot,
                Spacer(Spacing.Xl),
                lblChoLuot, nudChoFrom, lblDenLuot, nudChoTo, lblPhutLuot
            );
            loopRow.Margin = new Padding(Spacing.Xl, Spacing.Sm, 0, Spacing.Sm);
            left.Controls.Add(loopRow);

            // ── Cột phải: 4 checkbox + inline config ──
            var right = new TableLayoutPanel
            {
                Dock         = DockStyle.Fill,
                ColumnCount  = 1,
                AutoSize     = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor    = Color.Transparent,
                Padding      = new Padding(Spacing.Lg, 0, Spacing.Xl, 0),
                Margin       = new Padding(0),
            };

            if (cb1 != null) { cb1.AutoSize = true; cb1.Location = Point.Empty; cb1.Margin = new Padding(0, Spacing.Sm, 0, Spacing.Sm); right.Controls.Add(cb1); }

            var rowTK = BuildInlineRow(cb2, Spacer(Spacing.Sm), nudTKFrom, lblDenTK, nudTKTo, lblPhutTK);
            rowTK.Margin = new Padding(0, Spacing.Sm, 0, Spacing.Sm);
            right.Controls.Add(rowTK);

            var rowKB = BuildInlineRow(cb3, Spacer(Spacing.Sm), nudKBFrom, lblDenKB, nudKBTo, lblPhutKB);
            rowKB.Margin = new Padding(0, Spacing.Sm, 0, Spacing.Sm);
            right.Controls.Add(rowKB);

            if (cb4 != null) { cb4.AutoSize = true; cb4.Location = Point.Empty; cb4.Margin = new Padding(0, Spacing.Sm, 0, Spacing.Sm); right.Controls.Add(cb4); }

            var rowTime = BuildInlineRow(lblThoiGian, tpFrom, lblDenNgay, tpTo);
            rowTime.Margin = new Padding(Spacing.Xl, Spacing.Sm, 0, Spacing.Sm);
            right.Controls.Add(rowTime);

            // ── Divider mảnh giữa content và warnings ──
            var divider = new System.Windows.Forms.Panel
            {
                Height    = 1,
                Dock      = DockStyle.Fill,
                BackColor = ColorPalette.BorderLight,
                Margin    = new Padding(0, Spacing.Md, 0, Spacing.Md),
            };

            // ── Hàng warning 1 (link + cảnh báo 1) ──
            var warnRow1 = BuildInlineRow(llb, Spacer(Spacing.Xl), lblCanhBao1);
            warnRow1.Margin = new Padding(0, 0, 0, Spacing.Xs);
            warnRow1.Dock   = DockStyle.Fill;

            // ── Hàng warning 2 (cảnh báo 2 — tách riêng để không vỡ layout khi text dài) ──
            var warnRow2 = BuildInlineRow(lblCanhBao2);
            warnRow2.Margin = new Padding(0, 0, 0, 0);
            warnRow2.Dock   = DockStyle.Fill;

            grid.Controls.Add(left,    0, 0);
            grid.Controls.Add(right,   1, 0);
            grid.Controls.Add(divider, 0, 1);
            grid.SetColumnSpan(divider, 2);
            grid.Controls.Add(warnRow1, 0, 2);
            grid.SetColumnSpan(warnRow1, 2);
            grid.Controls.Add(warnRow2, 0, 3);
            grid.SetColumnSpan(warnRow2, 2);

            panel4.Controls.Add(grid);
            grid.BringToFront();
        }

        /// <summary> FlowLayoutPanel 1 hàng — các control tự kế tiếp trái→phải, center dọc. </summary>
        private static FlowLayoutPanel BuildInlineRow(params Control[] children)
        {
            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                BackColor     = Color.Transparent,
                Margin        = new Padding(0),
                Padding       = new Padding(0),
            };
            foreach (var c in children)
            {
                if (c == null) continue;
                c.Location = Point.Empty;
                if (c is System.Windows.Forms.Label || c is System.Windows.Forms.CheckBox || c is RadioButton)
                {
                    c.Margin = new Padding(0, 6, Spacing.Xs, 0);
                    if (c is System.Windows.Forms.Label lbl) lbl.AutoSize = true;
                }
                else
                {
                    c.Margin = new Padding(0, 2, Spacing.Sm, 2);
                }
                flow.Controls.Add(c);
            }
            return flow;
        }

        private static Control Spacer(int width) => new System.Windows.Forms.Label
        {
            AutoSize = false,
            Width    = width,
            Height   = 1,
            Margin   = new Padding(0),
            BackColor = Color.Transparent,
        };

        // ══════════════════════════════════════════════════════════════════
        //  fSettingDefault — reskin tokens, remove fixed size lock
        // ══════════════════════════════════════════════════════════════════
        public static void ApplyFSettingDefault(Form form)
        {
            if (form == null) return;
            AntdUI.Config.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            form.BackColor = ColorPalette.Background;
            form.Font      = FontScale.Body9;

            // Bỏ khoá resize: MaximumSize = 0 cho phép resize, giữ MinimumSize hợp lý
            form.MaximumSize = Size.Empty;
            if (form.MinimumSize.Width > 900) form.MinimumSize = new Size(900, 600);

            // Translate form title
            if (form.Text == "fSettingDefault") form.Text = "Cài đặt mặc định";

            TuneSettingsShell(form);

            // tabs3 default tab
            var tabs = GetField<Tabs>(form, "tabs3");
            if (tabs != null && tabs.SelectedIndex > 0)
                tabs.SelectedIndex = 0;

            // groupBox1 empty title → meaningful label
            var gb1 = GetField<GroupBox>(form, "groupBox1");
            if (gb1 != null && string.IsNullOrWhiteSpace(gb1.Text?.Trim()))
                gb1.Text = "Tuỳ chọn thiết bị";

            // Typo fix: "Chọn bard device" → "Chọn backup device"
            var lbl1 = GetField<System.Windows.Forms.Label>(form, "label1");
            if (lbl1 != null && lbl1.Text?.Contains("bard") == true)
                lbl1.Text = lbl1.Text.Replace("bard device", "backup device");

            // label10 warning → token Error
            var l10 = GetField<System.Windows.Forms.Label>(form, "label10");
            if (l10 != null) l10.ForeColor = ColorPalette.Error;

            // checkBox12, checkBox13 cảnh báo → token
            foreach (var name in new[] { "checkBox12", "checkBox13" })
            {
                var cb = GetField<System.Windows.Forms.CheckBox>(form, name);
                if (cb != null) cb.ForeColor = ColorPalette.Error;
            }
            // numericUpDown4 warning red → token
            var nud4 = GetField<NumericUpDown>(form, "numericUpDown4");
            if (nud4 != null) nud4.ForeColor = ColorPalette.Error;

            RelayoutSettingDefaultAndroidTab(form);

            NormalizeLegacyControls(form);
            ForceCrispTextOnLegacyControls(form);
        }

        /// <summary>
        /// Tab "Cài đặt thiết bị" có 4 dòng "[cb] [textbox readonly] [button chọn]" lệch nhau
        /// về toạ độ tuyệt đối. Gom lại thành TableLayoutPanel 3 cột chuẩn.
        /// Idempotent.
        /// </summary>
        private static void RelayoutSettingDefaultAndroidTab(Form form)
        {
            var tab = GetField<AntdUI.TabPage>(form, "tabPageSettingAndroid");
            if (tab == null) return;

            const string marker = "ssaAndroidGrid";
            if (tab.Controls.Find(marker, false).FirstOrDefault() != null) return;

            // Project mới: xóa hẳn group "Change device (root)" (groupBox1 chứa
            // checkBox1 + Chọn backup device + Quốc gia + cbbScript) — chỉ-FB scope
            // không dùng đổi thiết bị root. Reflow các control bên dưới lên trên.
            var gb1 = GetField<GroupBox>(form, "groupBox1");
            if (gb1 != null && gb1.Visible)
            {
                int yShift = gb1.Top + gb1.Height; // bao nhiêu pixel sẽ được "thu hồi"
                gb1.Visible = false;
                // Đẩy các sibling phía dưới groupBox1 lên trên để không để khoảng trống.
                foreach (Control c in tab.Controls)
                {
                    if (ReferenceEquals(c, gb1)) continue;
                    if (c.Top >= gb1.Top + gb1.Height)
                        c.Top -= gb1.Height + Spacing.Sm;
                }
            }

            // 4 dòng backup: [cb] [textbox] [button]
            var rows = new (string cb, string txt, string btn)[]
            {
                ("checkBox1", "textBox1", "button2"), // Change device (root) — đã ẩn ở trên (groupBox1)
                ("checkBox2", "textBox2", "button5"), // Backup device (root)
                ("checkBox3", "textBox3", "button3"), // Backup profile (root)
                ("checkBox8", "textBox4", "button4"), // Cài lại app khi crash
            };

            // Không re-layout row trong groupBox1 (phức tạp, để nguyên layout hiện tại).
            // Chỉ normalize margin + alignment cho 3 dòng còn lại.
            for (int i = 1; i < rows.Length; i++)
            {
                var cb  = GetField<System.Windows.Forms.CheckBox>(form, rows[i].cb);
                var txt = GetField<System.Windows.Forms.TextBox>(form, rows[i].txt);
                var btn = GetField<System.Windows.Forms.Button>(form, rows[i].btn);
                if (cb == null || txt == null || btn == null) continue;

                int y = cb.Top;
                cb.AutoSize = true;
                cb.Location = new Point(Spacing.Xl, y + 2);

                int txtX = Spacing.Xl + 180;
                txt.Location = new Point(txtX, y);
                txt.Size     = new Size(260, 26);
                txt.BorderStyle = BorderStyle.FixedSingle;

                btn.Location = new Point(txtX + txt.Width + Spacing.Sm, y - 1);
                btn.Size     = new Size(80, 28);
                btn.FlatStyle = FlatStyle.Flat;
                btn.BackColor = ColorPalette.Primary;
                btn.ForeColor = Color.White;
                btn.Text      = "Chọn...";
                btn.FlatAppearance.BorderSize = 0;
            }

            // Row "Reboot mất mạng: [n] lần" + "Tự reboot sau: [n] phút" — căn thẳng
            var rbCb4 = GetField<System.Windows.Forms.CheckBox>(form, "checkBox4");
            var rbCb5 = GetField<System.Windows.Forms.CheckBox>(form, "checkBox5");
            if (rbCb4 != null && rbCb5 != null)
            {
                int yR = rbCb4.Top;
                rbCb4.Location = new Point(Spacing.Xl, yR + 2);
                rbCb5.Location = new Point(Spacing.Xl + 280, yR + 2);
            }

            // Row cbb6 "Xóa backup profile" + cb7 "đồng thời xóa backup device"
            var cb6 = GetField<System.Windows.Forms.CheckBox>(form, "checkBox6");
            var cb7 = GetField<System.Windows.Forms.CheckBox>(form, "checkBox7");
            if (cb6 != null && cb7 != null)
            {
                int y = cb6.Top;
                cb6.Location = new Point(Spacing.Xl, y + 2);
                cb7.Location = new Point(Spacing.Xl + 280, y + 2);
            }

            // Marker để tránh chạy lại
            tab.Controls.Add(new System.Windows.Forms.Label
            {
                Name = marker, Visible = false, Size = new Size(0, 0), Location = new Point(-1, -1)
            });
        }

        // ══════════════════════════════════════════════════════════════════
        //  fSettingJob — reskin tokens
        // ══════════════════════════════════════════════════════════════════
        public static void ApplyFSettingJob(Form form)
        {
            if (form == null) return;
            AntdUI.Config.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            form.BackColor = ColorPalette.Background;
            form.Font      = FontScale.Body9;

            TuneSettingsShell(form);

            // Tabs default index=0 (đã set trong ctor; ở đây đảm bảo)
            var tabs = GetField<Tabs>(form, "tabs3");
            if (tabs != null && tabs.SelectedIndex > 0) tabs.SelectedIndex = 0;

            NormalizeLegacyControls(form);
            ForceCrispTextOnLegacyControls(form);
        }

        /// <summary>
        /// Common tune cho shell của fSettingDefault + fSettingJob:
        /// windowBar, panel1 (footer actions), panel2 (content), button Save/Close.
        /// </summary>
        private static void TuneSettingsShell(Form form)
        {
            var bar = GetField<PageHeader>(form, "windowBar");
            if (bar != null)
            {
                bar.BackColor = ColorPalette.Surface;
                bar.ForeColor = ColorPalette.TextPrimary;
                bar.Font      = FontScale.SectionBold;
                bar.UseTextBold = true;
            }

            var panel2 = GetField<AntdUI.Panel>(form, "panel2");
            if (panel2 != null)
            {
                panel2.Back    = ColorPalette.Surface;
                panel2.Radius  = Radius.Lg + 4;
                panel2.padding = new Padding(Spacing.Xl);
            }

            var panel1 = GetField<AntdUI.Panel>(form, "panel1");
            if (panel1 != null)
            {
                panel1.Back    = ColorPalette.Surface;
                panel1.Radius  = Radius.Lg + 4;
                panel1.padding = new Padding(Spacing.Lg);
            }

            // Save / Close buttons
            var btnSave  = GetField<AntdUI.Button>(form, "button9");
            var btnClose = GetField<AntdUI.Button>(form, "button1");
            if (btnSave != null)
            {
                btnSave.DefaultBack = ColorPalette.Success;
                btnSave.ForeColor   = Color.White;
                btnSave.Radius      = Radius.Md;
                btnSave.Font        = FontScale.Body9Bold;
            }
            if (btnClose != null)
            {
                btnClose.DefaultBack = ColorPalette.Error;
                btnClose.ForeColor   = Color.White;
                btnClose.Radius      = Radius.Md;
                btnClose.Font        = FontScale.Body9Bold;
            }

            // Tabs styling
            var tabs = GetField<Tabs>(form, "tabs3");
            if (tabs != null)
            {
                tabs.BackColor = ColorPalette.Surface;
                tabs.Font      = FontScale.Body9Bold;
            }
        }

        /// <summary>
        /// Đi đệ quy toàn bộ control tree:
        /// - Label/CheckBox/RadioButton: ép FontScale.Body9, ForeColor (nếu là Color.FromArgb(48,48,48) default)
        /// - TextBox / NumericUpDown / ComboBox: font + border consistent
        /// - GroupBox: gray border color
        /// - BackColor Transparent / (236,240,241) → ColorPalette.Surface hoặc Transparent (tuỳ parent)
        /// Idempotent — gọi nhiều lần không tích luỹ state.
        /// </summary>
        private static void NormalizeLegacyControls(Control root)
        {
            if (root == null) return;

            var legacyBg = Color.FromArgb(236, 240, 241);

            foreach (Control c in root.Controls)
            {
                // Recurse trước để child được normalize theo token
                NormalizeLegacyControls(c);

                switch (c)
                {
                    case LinkLabel link:
                        if (link.Font?.FontFamily.Name != FontScale.FamilyName)
                            link.Font = FontScale.EnsureMinBody(link.Font);
                        link.LinkColor = ColorPalette.Primary;
                        link.ActiveLinkColor = ColorPalette.PrimaryActive;
                        break;

                    case System.Windows.Forms.Label lbl:
                        // Chỉ override font nếu chưa đúng Segoe UI — preserve explicit red/firebrick/other
                        if (lbl.Font?.FontFamily.Name != FontScale.FamilyName)
                            lbl.Font = FontScale.EnsureMinBody(lbl.Font);
                        // ForeColor neutral (48,48,48) / default ControlText → TextPrimary
                        if (lbl.ForeColor == Color.FromArgb(48, 48, 48) || lbl.ForeColor == SystemColors.ControlText || lbl.ForeColor == SystemColors.ActiveCaptionText)
                            lbl.ForeColor = ColorPalette.TextPrimary;
                        break;

                    case System.Windows.Forms.CheckBox cb:
                        if (cb.Font?.FontFamily.Name != FontScale.FamilyName)
                            cb.Font = FontScale.EnsureMinBody(cb.Font);
                        if (cb.ForeColor == Color.FromArgb(48, 48, 48) || cb.ForeColor == SystemColors.ControlText)
                            cb.ForeColor = ColorPalette.TextPrimary;
                        break;

                    case System.Windows.Forms.RadioButton rb:
                        if (rb.Font?.FontFamily.Name != FontScale.FamilyName)
                            rb.Font = FontScale.EnsureMinBody(rb.Font);
                        if (rb.ForeColor == SystemColors.ControlText)
                            rb.ForeColor = ColorPalette.TextPrimary;
                        break;

                    case NumericUpDown nud:
                        if (nud.Font?.FontFamily.Name != FontScale.FamilyName)
                            nud.Font = FontScale.EnsureMinBody(nud.Font);
                        nud.BorderStyle = BorderStyle.FixedSingle;
                        break;

                    case System.Windows.Forms.TextBox tb:
                        if (tb.Font?.FontFamily.Name != FontScale.FamilyName)
                            tb.Font = FontScale.EnsureMinBody(tb.Font);
                        break;

                    case System.Windows.Forms.ComboBox cbo:
                        if (cbo.Font?.FontFamily.Name != FontScale.FamilyName)
                            cbo.Font = FontScale.EnsureMinBody(cbo.Font);
                        cbo.FlatStyle = FlatStyle.Flat;
                        cbo.BackColor = ColorPalette.Surface;
                        break;

                    case System.Windows.Forms.Button btn:
                        if (btn.Font?.FontFamily.Name != FontScale.FamilyName)
                            btn.Font = FontScale.EnsureMinBody(btn.Font);
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = ColorPalette.Border;
                        btn.BackColor = ColorPalette.Surface;
                        btn.ForeColor = ColorPalette.TextPrimary;
                        break;

                    case GroupBox gb:
                        if (gb.Font?.FontFamily.Name != FontScale.FamilyName)
                            gb.Font = FontScale.EnsureMinBody(gb.Font);
                        gb.ForeColor = ColorPalette.TextSecondary;
                        break;

                    case System.Windows.Forms.Panel p:
                        // Legacy background 236,240,241 → Surface (AntdUI.Panel parent sẽ lo radius)
                        if (p.BackColor == legacyBg) p.BackColor = ColorPalette.Surface;
                        break;
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  Reflection helper
        // ══════════════════════════════════════════════════════════════════
        private static T GetField<T>(object owner, string name) where T : class
        {
            var f = owner.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return f?.GetValue(owner) as T;
        }
    }
}
