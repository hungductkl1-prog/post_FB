using AntdUI;
using AutoAndroid;
using Facebook_Farm_NewFeed_PostStory.Utils;
using Facebook_Farm_NewFeed_PostStory.Utils.Design;
using Facebook_Farm_NewFeed_PostStory.Views.Forms;
using Sunny.Subd.Core.Facebook;
using Sunny.Subd.Core.Utils;
using Sunny.Subd.Core.Models;
using Sunny.Subd.Core.Proxies;
using Sunny.Subd.Core.Services;
using Sunny.Subd.Core.Telegram;
using Sunny.Subdy.Common.API;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Common.Services;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CommonMethod = Sunny.Subdy.Common.ControlMethod.CommonMethod;
using File = System.IO.File;

namespace Facebook_Farm_NewFeed_PostStory.Views.Controls
{
    public partial class ucdgvAccount : UserControl
    {
        private readonly fMain _form;
        private readonly string _platform;
        AntdUI.IContextMenuStripItem[] menulist;
        private readonly FolderContext _folderContext;
        private readonly AccountContext _accountContext;
        private readonly ConfigHelper _configHelper;
        public List<Account> _accounts;
        // VirtualMode: grid đọc từ _accounts (view sau filter in-memory).
        private int _sortColumnIndex = -1;
        private SortOrder _sortOrder = SortOrder.None;
        private List<string> _folderNames = new List<string>();
        private readonly ScriptContext _scriptContext;
        private System.Windows.Forms.Timer _countsTimer;
        // MaxPG-style: timer flush status hàng loạt xuống DB (không ghi mỗi lần đổi).
        private System.Windows.Forms.Timer? _statusFlushTimer;
        // Gộp nhiều AddAccountThreadSafe trong lúc đăng ký (mỗi account → 1 reload) thành 1 reload.
        private System.Windows.Forms.Timer? _reloadDebounceTimer;
        // Realtime: repaint các dòng đang hiển thị trong lúc job chạy để cột Trạng thái/
        // Tình trạng cập nhật ngay (VirtualMode đọc lại qua CellValueNeeded), không phụ
        // thuộc hoàn toàn vào ThrottledPropertyNotifier (external).
        private System.Windows.Forms.Timer? _liveRepaintTimer;
        private int _cachedCheckedCount = 0;
        private int _cachedRunningCount = 0;
        private bool _menuStripDirty = true;
        private volatile bool _menuWarmupRunning;
        private List<Folder> _cachedFolders = new();
        private List<Script> _cachedScripts = new();
        private List<(string Serial, string Model)> _cachedDeviceMenuItems = new();
        private static readonly ConcurrentDictionary<string, Image> _menuIconCache = new();
        private readonly List<Account> _checkedAccounts = new();
        private HashSet<Guid>? _checkedAccountIds;
        // Chọn tất cả ảo — không duyệt/set 50k checkbox.
        private bool _checkAllActive;
        private HashSet<Guid>? _checkAllExceptions;
        private const int LargeListThreshold = 2000;
        private const int MaxDeviceMenuItems = 40;
        private int _bindGeneration;
        private string _listWhere = "";
        private Dictionary<string, object> _listParams = new();
        private string _stateFilterExtra = "";
        private string _cboFilterExtra = "";
        private string _searchTerm = "";
        private List<Guid>? _scopedIdList;
        /// <summary>Full scope loaded from DB (folder + state + cbo filters). Search filters in-memory only.</summary>
        private List<Account> _scopeAccounts = new();
        private List<Account> _fullView = new();
        private int _scopeCount;
        private CancellationTokenSource? _loadCts;
        private bool _suppressFolderSelect;
        private int _preserveScrollRow = -1;
        private bool _cellFormattingHooked = true;
        private bool _accountCheckboxPaintingHooked = false;
        private readonly HashSet<string> _columnsHiddenForPerf = new(StringComparer.Ordinal);
        private readonly HashSet<string> _columnsForceShownForPerf = new(StringComparer.Ordinal);
        private readonly HashSet<string> _columnsFillModeForPerf = new(StringComparer.Ordinal);
        private static readonly Color AlternatingRowBackColor = Color.FromArgb(250, 250, 250);
        // GDI cache cho checkbox painter — tránh new SolidBrush/Pen mỗi lần vẽ cell (scroll storm).
        // Màu lấy từ palette cố định nên dùng lại được; bút có round-cap dựng sẵn 1 lần.
        private static readonly Color CheckboxBorderUnselected = Color.FromArgb(120, 120, 120);
        private static readonly SolidBrush _cbFillChecked = new SolidBrush(ColorPalette.Primary);
        private static readonly SolidBrush _cbFillUnchecked = new SolidBrush(Color.White);
        private static readonly Pen _cbBorderChecked = new Pen(ColorPalette.Primary, 2F);
        private static readonly Pen _cbBorderSelected = new Pen(Color.White, 2F);
        private static readonly Pen _cbBorderNormal = new Pen(CheckboxBorderUnselected, 1.5F);
        private static readonly Pen _cbCheckMark = new Pen(Color.White, 2F)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round
        };
        private static readonly Point[] _cbCheckPts = new Point[3];
        private DataGridViewCellPaintingEventHandler? _statusBadgeCellPaintingHandler;
        private bool _statusBadgePaintHooked;
        private bool _gridDoubleBuffered;
        private bool _tableLayoutColumnsInitialized;
        private float _lastTableLayoutSpacing = -1f;
        private bool _resizeLayoutPending;
        internal bool LargeListPerfMode { get; private set; }
        private bool UseSparseCheckedLookup => _useWindowedCache || (_accounts != null && _accounts.Count > LargeListThreshold);

        // ── Windowed DB paging (30k+ accounts) ──────────────────────────────
        // Khi bật: lưới chỉ giữ vài trang trong RAM, dữ liệu lấy từ DB theo rowIndex.
        private AccountWindowCache? _cache;
        private bool _useWindowedCache = false;

        // Map Id → instance Account đang chạy job (live). Set khi bấm Bắt đầu, xóa khi kết thúc.
        // Cache windowed dùng resolver này để hiển thị/repaint trạng thái realtime.
        private System.Collections.Concurrent.ConcurrentDictionary<System.Guid, Account>? _liveRunningById;

        // Per-repaint row cache: CellValueNeeded fires once per column per row — avoid repeated GetRow.
        private int _accountCacheRow = -1;
        private Account? _accountCacheRef;

        // ── Display value cache: avoid repeated Guid.ToString(), string truncation, formatting ──
        private readonly DisplayValueCache _displayCache = new();

        private System.Windows.Forms.Timer? _diagSnapshotTimer;
        private long _uiMarshalLocal;
        private GridMetrics? _gridMetrics;

        // ── Perf caches (optimization) ──────────────────────────────────────
        // Cache ssaEmptyState control reference → tránh Controls.Find đệ quy mỗi lần SyncEmptyStateOverlay.
        private Control? _emptyStateControl;
        // Cache native context menu → tránh rebuild toàn cây ToolStripMenuItem mỗi lần right-click.
        private System.Windows.Forms.ContextMenuStrip? _cachedNativeMenu;
        private AntdUI.IContextMenuStripItem[]? _cachedNativeMenuSource;

        /// <summary>30k+ rows: avoid materializing DataGridViewRow via Rows[i].Selected.</summary>
        private bool UseVirtualSafeSelection => LargeListPerfMode || (_useWindowedCache && IsLargeVirtualList);

        /// <summary>Fast checkbox paint (ControlPaint) without hiding columns.</summary>
        private bool UseLightCellPaint => LargeListPerfMode || (_useWindowedCache && IsLargeVirtualList);

        private bool IsLargeVirtualList => ViewRowCount > LargeListThreshold;

        /// <summary>Cột [NotMapped] — không có trong DB nên không thể ORDER BY.</summary>
        private static readonly HashSet<string> _unsortableProps = new(StringComparer.Ordinal)
        {
            nameof(Account.STT), nameof(Account.JobToday), nameof(Account.Summary),
            nameof(Account.Summary_Skip), nameof(Account.XuToday), nameof(Account.ColorType)
        };

        /// <summary>Cột được phép sort ở tầng DB — phải nằm trong ListSelectColumns (query slim).</summary>
        private static readonly HashSet<string> _sortableProps = new(StringComparer.Ordinal)
        {
            nameof(Account.Uid), nameof(Account.FullName), nameof(Account.NameFolder),
            nameof(Account.NameScript), nameof(Account.Note), nameof(Account.State),
            nameof(Account.Status), nameof(Account.RecentInteraction), nameof(Account.TokenJob),
        };

        private string GetCurrentOrderBySql()
        {
            if (_sortColumnIndex < 0 || _sortColumnIndex >= dataGridView1.Columns.Count)
                return nameof(Account.Uid);
            var col = dataGridView1.Columns[_sortColumnIndex];
            string prop = col == dataGridViewTextBoxColumn1 ? nameof(Account.Uid) : col.DataPropertyName;
            return _sortableProps.Contains(prop) ? prop : nameof(Account.Uid);
        }

        private string FilterWhereExtra => _stateFilterExtra + _cboFilterExtra;
        private string ScopeWhere => _listWhere + FilterWhereExtra;
        private bool HasInMemorySearch => !_useWindowedCache && !string.IsNullOrWhiteSpace(_searchTerm);
        /// <summary>Scope for DB aggregates / persist. In windowed cache mode it includes the search LIKE clause.</summary>
        private string EffectiveWhere =>
            _useWindowedCache && !string.IsNullOrWhiteSpace(_searchTerm)
                ? ScopeWhere + SearchClause()
                : ScopeWhere;
        private int ViewCount => _useWindowedCache ? (_cache?.TotalCount ?? 0) : (_accounts?.Count ?? 0);

        private Dictionary<string, object> ScopeParams() => new Dictionary<string, object>(_listParams);

        private Dictionary<string, object> EffectiveParams()
        {
            var p = ScopeParams();
            if (_useWindowedCache && !string.IsNullOrWhiteSpace(_searchTerm))
                p["@__search"] = $"%{_searchTerm}%";
            return p;
        }

        private int SelectionScopeCount =>
            _useWindowedCache ? (_cache?.TotalCount ?? 0) : (HasInMemorySearch ? ViewCount : _scopeCount);

        private sealed class CheckedRestoreState
        {
            public bool CheckAllActive;
            public HashSet<Guid>? CheckAllExceptions;
            public HashSet<Guid>? CheckedAccountIds;
        }

        // ── MaxPG-style status flush ────────────────────────────────────────
        // Status đổi liên tục trong lúc chạy chỉ nằm trên model (live grid qua
        // ThrottledPropertyNotifier), KHÔNG ghi DB mỗi lần. Timer định kỳ + lúc Dừng +
        // lúc Dispose mới flush hàng loạt xuống DB (chỉ 3 cột Status/State/RecentInteraction)
        // — tương đương cơ chế UpdateStatus.FlushStatusToDb của MaxPhoneFarm.
        private void FlushStatusesToDb(bool sync = false)
        {
            try
            {
                if (_useWindowedCache) return; // windowed: core checkpoint tự persist
                var snap = _accounts;
                if (snap == null || snap.Count == 0) return;

                List<Account>? dirty = null;
                for (int i = 0; i < snap.Count; i++)
                {
                    var a = snap[i];
                    // Bỏ qua "Chưa chạy" — đây là placeholder runtime, không ghi DB.
                    bool hasRealStatus = !string.IsNullOrEmpty(a.Status) && a.Status != "Chưa chạy";
                    if (a != null && (a.Running || hasRealStatus))
                        (dirty ??= new List<Account>()).Add(a);
                }
                if (dirty == null || dirty.Count == 0) return;

                var ctx = _accountContext;
                if (sync) { try { ctx.UpdateStatusBatch(dirty); } catch { } }
                else Task.Run(() => { try { ctx.UpdateStatusBatch(dirty); } catch { } });
            }
            catch { }
        }

        private void RunOnUi(Action action)
        {
            if (IsDisposed) return;
            if (!IsHandleCreated)
            {
                action();
                return;
            }
            if (InvokeRequired)
            {
                AccountGridPerf.RecordUiMarshal();
                Interlocked.Increment(ref _uiMarshalLocal);
                BeginInvoke(action);
            }
            else
                action();
        }

        private Task RunOnUiAsync(Action action)
        {
            if (IsDisposed) return Task.CompletedTask;
            if (!IsHandleCreated)
            {
                action();
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Run()
            {
                try
                {
                    if (!IsDisposed) action();
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }

            if (InvokeRequired) BeginInvoke(Run);
            else Run();
            return tcs.Task;
        }

        // Cached column indices for performance
        private int _colIndexColorType = -1;
        private int _colIndexRunning = -1;
        private int _colIndexChecked = -1;

        private delegate object CellValueReader(Account acc, int rowIndex);
        private CellValueReader?[]? _cellValueReaders;

        /// <summary>Cột được phép hiện khi danh sách lớn — giữ tối thiểu để scroll/paint nhẹ.</summary>
        private static readonly HashSet<string> LargeListPerfVisibleColumns = new(StringComparer.Ordinal)
        {
            "dataGridViewCheckBoxColumn1",
            "dataGridViewTextBoxColumn1",
            "col_Uid",
            "col_FullName",
            "col_State",
            "col_Serial",
        };

        public ucdgvAccount(fMain form, string platform)
        {
            InitializeComponent();

            _accounts = new List<Account>();
            _scriptContext = new ScriptContext();

            // Visual setup (style chung do GridStyleHelper + SsaTheme quản lý)
            dataGridView1.AutoGenerateColumns = false;
            // VirtualMode: lưới chỉ render dòng đang hiển thị, lấy data từ _accounts qua
            // CellValueNeeded → KHÔNG tạo vật lý 30k+ DataGridViewRow (nguyên nhân đơ UI).
            // Kết hợp nạp full nhóm vào RAM (không windowed/async paging) → cuộn mượt như MaxPhoneFarm.
            dataGridView1.VirtualMode = true;
            dataGridView1.AllowUserToAddRows = false;
            // ShowCellToolTips: tắt để tránh đọc giá trị liên tục khi rê chuột trên list lớn.
            dataGridView1.ShowCellToolTips = false;
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.White;

            // Double buffering để giảm flicker với 20k rows
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null, dataGridView1, new object[] { true });

            // Font tiếng Việt: dùng Segoe UI (đủ bộ dấu) cho cell mặc định thay vì
            // FontUtil._fontSemiBold — font tuỳ biến này thiếu glyph tiếng Việt nên
            // họ tên/ghi chú có dấu bị vỡ chữ.
            var defaultFont = new Font(FontScale.FamilyName, 9F, FontStyle.Bold);
            dataGridView1.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = ColorTranslator.FromHtml("#1A1A1A"),
                SelectionBackColor = Color.FromArgb(0, 120, 215),
                SelectionForeColor = Color.White,
                Font = defaultFont
            };
            dataGridViewCheckBoxColumn1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCheckBoxColumn1.DataPropertyName = string.Empty;

            _form = form;
            _platform = platform;

            // Module "Reg Facebook": đổi nhãn nút "Cài đặt chung" thành "Hành động" cho
            // đúng luồng người dùng mô tả ("vào hành động điền key shopmailmmo.com").
            // Chỉ áp cho Reg Facebook — Facebook/Pandora giữ nguyên nhãn gốc.
            if (_platform == PlatformModel.RegFacebook)
            {
                button5.Text = "Hành động";
            }

            LoadColumnsDataGridView();
            // Fill ở cấp grid + VirtualMode + hàng chục nghìn dòng → layout/header hỏng, UI đơ.
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Events
            dataGridView1.SelectionChanged += DataGridView_SelectionChanged;
            dataGridView1.CellFormatting += uiDataGridView1_CellFormatting;
            dataGridView1.CellValueNeeded += DataGridView1_CellValueNeeded;
            dataGridView1.CellMouseDown += DataGridView1_CellMouseDown;
            dataGridView1.DataError += DataGridView1_DataError;
            dataGridView1.ColumnHeaderMouseClick += DataGridView1_ColumnHeaderMouseClick;
            dataGridView1.RowTemplate.Height = Math.Max(22, dataGridView1.RowTemplate.Height);

            _folderContext = new FolderContext();
            _accountContext = new AccountContext();

            _cache = new AccountWindowCache(
                _accountContext,
                invalidateRows: InvalidateRowRange,
                onRowChanged: (acc, rowIdx) => RunOnUi(() =>
                {
                    // Invalidate display cache for this specific account (property changed).
                    _displayCache.InvalidateAccount(acc.Id);
                    if (!dataGridView1.IsDisposed && rowIdx >= 0 && rowIdx < dataGridView1.RowCount)
                        dataGridView1.InvalidateRow(rowIdx);
                }),
                runOnUi: RunOnUi);

            // Cache lấy instance live (đang chạy job) theo Id → cột Trạng thái cập nhật realtime.
            _cache.SetLiveResolver(id =>
                (_liveRunningById != null && _liveRunningById.TryGetValue(id, out var a)) ? a : null);

            _configHelper = new Sunny.Subdy.Common.Json.ConfigHelper(
                this,
                this.Name + "_" + _platform,
                onLoad: () =>
                {
                    input6.Text = string.Empty;
                }
            );

            menulist = null;

            NormalizeAccountSelectorColumn();

            dataGridViewTextBoxColumn1.Width = 40;
            dataGridViewTextBoxColumn1.MinimumWidth = 40;
            dataGridViewTextBoxColumn1.Resizable = DataGridViewTriState.False;
            tableLayoutPanel1.Resize += tableLayoutPanel1_Resize;

            // Empty state: delegated to SsaTheme.ApplyUcAccount (SSA styled)

            // Update checked/running counts via timer - dùng cached values, không LINQ mỗi giây
            _countsTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _countsTimer.Tick += (s, e) =>
            {
                long _t = UiThreadProfiler.Begin();
                try
                {
                var snapshot = _accounts;
                if (snapshot == null || snapshot.Count == 0) return;
                if (snapshot.Count <= LargeListThreshold && !_checkAllActive)
                {
                    _cachedCheckedCount = CountChecked(snapshot);
                    _cachedRunningCount = CountRunning(snapshot);
                }
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel6, $"{_cachedCheckedCount}");
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel2, $"{_cachedRunningCount}");
                }
                finally { UiThreadProfiler.End("Timer.CountsTick(5s)", _t); }
            };
            _countsTimer.Start();

            // MaxPG-style status mechanism: status đổi liên tục lúc chạy chỉ nằm trên
            // model + live grid (ThrottledPropertyNotifier); timer này flush hàng loạt
            // xuống DB định kỳ (tương đương UpdateStatus.FlushStatusToDb của MaxPhoneFarm).
            _statusFlushTimer = new System.Windows.Forms.Timer { Interval = 7000 };
            _statusFlushTimer.Tick += (s, e) => FlushStatusesToDb();
            _statusFlushTimer.Start();

            if (AccountGridPerf.DiagnosticsEnabled)
            {
                _diagSnapshotTimer = new System.Windows.Forms.Timer { Interval = 30_000 };
                _diagSnapshotTimer.Tick += (_, _) => EmitDiagnosticsSnapshot();
                _diagSnapshotTimer.Start();

                // GridMetrics: consolidated 30s reporting for display cache + scroll coalescing.
                _gridMetrics = new GridMetrics(() =>
                {
                    var (dHits, dMisses, dEvictions) = _displayCache.TakeSnapshot();
                    var proc = System.Diagnostics.Process.GetCurrentProcess();
                    var render = dataGridView1.TakeRenderStats();
                    var (cvn, cellPaint, cellFormat) = AccountGridPerf.TakeUiEventCounts();
                    return new GridMetrics.MetricsSnapshot(
                        cellValueNeededCount: cvn,
                        displayCacheHits: dHits,
                        displayCacheMisses: dMisses,
                        displayCacheEvictions: dEvictions,
                        displayCacheEntries: _displayCache.Count,
                        avgPageFetchMs: _cache?.LastPageFetchMs ?? 0,
                        lastPageFetchMs: _cache?.LastPageFetchMs ?? 0,
                        scrollRequestCount: _cache?.ScrollRequestCount ?? 0,
                        cancelledFetchCount: _cache?.CancelledFetchCount ?? 0,
                        managedHeapMB: GC.GetTotalMemory(false) / (1024 * 1024),
                        workingSetMB: proc.WorkingSet64 / (1024 * 1024),
                        activeCacheRows: _cache?.CachedRowCount ?? 0,
                        activeCachePages: _cache?.PageCount ?? 0,
                        wmPaintCount: render.WmPaintCount,
                        avgPaintMs: render.AvgPaintMs,
                        scrollEventCount: render.ScrollCount,
                        avgScrollMs: render.AvgScrollMs,
                        gdiObjects: NativeGuiResources.GdiObjects(),
                        userObjects: NativeGuiResources.UserObjects(),
                        cellPaintingCount: cellPaint,
                        cellFormattingCount: cellFormat);
                });

                // UI-thread profiler: Top-10 by UI time + UI busy probe, emitted every 30s.
                UiThreadProfiler.Start(this);
            }

            // Nếu config cột chưa tồn tại (first run) → apply "Hiển thị tối ưu" làm mặc định:
            // chỉ show core (UID, Họ tên, Nhóm, Kịch bản, Trạng thái) + 5 cột tối ưu
            // (TOTAL, HÔM NAY, LẦN TƯƠNG TÁC CUỐI, XU, TÌNH TRẠNG). Sau khi user chọn qua
            // dialog "Hiển thị", config file sẽ override list này.
            // Include cả original-case lẫn UPPERCASE vì LoadConfigColums được gọi 2 lần:
            // trước SsaTheme.ApplyUcAccount (header còn original) và sau (header đã uppercase).
            var colConfigFile = $"configs\\{dataGridView1.Name}.txt";
            var hideList = new List<string> { nameof(Account.Id), nameof(Account.ColorType), nameof(Account.Running) };
            if (!System.IO.File.Exists(colConfigFile))
            {
                var hideHeaders = new[]
                {
                    "Mật khẩu", "2FA", "Cookie", "Token", "Giới tính", "Bio",
                    "Bạn bè", "Following", "Bạn bè/Following", "Page profile", "Số nhóm", "Follow",
                    "Ngày sinh", "Ngày tạo", "Avatar", "Số điện thoại", "Email", "Mật khẩu email",
                    "Mail client id", "Mail refresh token", "Email khôi phục", "User Agent",
                    "Mail Recover Pass", "Thông tin thiết bị", "Proxy", "Ghi chú",
                    "Job success", "Job fail",
                    "Thiết bị", "Token Job",
                    // Nhãn theo MaxPG (sau ApplyMaxPgColumnLayout) — giữ ẩn mặc định như cũ
                    "Mã 2FA", "Giới Tính", "Profile", "Phone", "Mật khẩu mail",
                    "Useragent", "Device Info"
                };
                hideList.AddRange(hideHeaders);
                hideList.AddRange(hideHeaders.Select(h => h.ToUpperInvariant()));
            }
            ControlHelper.LoadConfigColums(dataGridView1, hideList);
            ForceHideInternalColumns(dataGridView1);
            NormalizeAccountSelectorColumn();

            // Tooltip cho các icon button quản lý nhóm
            var toolTip = new ToolTip { AutoPopDelay = 3000, InitialDelay = 300, ReshowDelay = 200 };
            toolTip.SetToolTip(button3, "Thêm nhóm");
            toolTip.SetToolTip(button2, "Đổi tên nhóm");
            toolTip.SetToolTip(button1, "Xóa nhóm");
            toolTip.SetToolTip(button17, "Hiển thị / Ẩn tài khoản");

            // Setup combobox lọc tài khoản (multi-select)
            cboFilterAccount.Items.Clear();
            cboFilterAccount.Items.AddRange(new object[]
            {
                "LIVE", "DIE", "Chưa xác định",
                "Đang chạy", "Lỗi", "Đăng xuất", "Captcha", "Bị chặn", "Đã dừng",
                "Có trạng thái", "Chưa có trạng thái",
                "Tên tiếng Việt", "Tên tiếng Anh",
                "UID đầu 6", "UID đầu 1",
                "Tương tác hôm nay", "Tương tác hôm qua", "Chưa tương tác",
            });
            cboFilterAccount.SelectedValueChanged += CboFilterAccount_SelectedValueChanged;

            GridStyleHelper.Apply(dataGridView1);

            FontUtil.ApplyFontToAllControls(this); Facebook_Farm_NewFeed_PostStory.Utils.Design.VietnameseFont.Enforce(this);

            // SSA visual redesign — chỉ đụng UI, không đổi business logic
            SsaTheme.ApplyUcAccount(this);

            // SsaTheme đã uppercase headers → LoadConfigColums gọi ở dòng ~127 match sai
            // (config file chứa "UID" nhưng lúc đó header còn "Uid"). Re-apply config sau
            // khi headers đã final để visibility đúng theo lựa chọn của user.
            ControlHelper.LoadConfigColums(dataGridView1, hideList);
            ForceHideInternalColumns(dataGridView1);
            NormalizeAccountSelectorColumn();

            // Re-apply column visibility sau khi ConfigHelper restore (Load fire sau ctor)
            // → đảm bảo grid paint lần đầu đã có đúng cấu hình cột.
            this.Load += (_, __) =>
            {
                ControlHelper.LoadConfigColums(dataGridView1, hideList);
                ForceHideInternalColumns(dataGridView1);
                NormalizeAccountSelectorColumn();
                ApplyMaxPgColumnLayout();
                ApplyVietnameseSafeGridFont();
            };

            // Thứ tự + nhãn cột theo MaxPhoneFarm (áp dụng cuối, sau SsaTheme/LoadConfig).
            ApplyMaxPgColumnLayout();

            // Ép font tiếng Việt cho lưới sau khi mọi style (FontUtil/SsaTheme) đã apply.
            ApplyVietnameseSafeGridFont();

            // Timer repaint dòng hiển thị khi job đang chạy → trạng thái cập nhật realtime.
            _liveRepaintTimer = new System.Windows.Forms.Timer { Interval = 400 };
            _liveRepaintTimer.Tick += (s, e) => RepaintVisibleRowsWhileRunning();
        }

        // Font tiếng Việt cho lưới: FontUtil._fontSemiBold + ApplyFontToAllControls có thể
        // đặt font thiếu glyph tiếng Việt cho control/header → vỡ chữ có dấu. Ép lại Segoe UI
        // (đủ bộ dấu) cho cả cell lẫn header, gọi SAU khi mọi style khác đã apply để thắng.
        private void ApplyVietnameseSafeGridFont()
        {
            try
            {
                var gridFont = new Font(FontScale.FamilyName, 9F, FontStyle.Bold);
                dataGridView1.Font = gridFont;
                dataGridView1.DefaultCellStyle.Font = gridFont;
                dataGridView1.ColumnHeadersDefaultCellStyle.Font = gridFont;
            }
            catch { }
        }

        // Repaint các dòng đang hiển thị trong lúc job chạy. VirtualMode sẽ đọc lại giá trị
        // Status/State đã bị MainService mutate qua CellValueNeeded → cột Trạng thái/Tình trạng
        // cập nhật realtime mà không cần ghi DB.
        private void RepaintVisibleRowsWhileRunning()
        {
            if (_liveRunningById == null) return;
            if (dataGridView1.IsDisposed || !dataGridView1.IsHandleCreated) return;

            int rowCount = dataGridView1.RowCount;
            if (rowCount == 0) return;

            int first = dataGridView1.FirstDisplayedScrollingRowIndex;
            if (first < 0) first = 0;
            int displayed = dataGridView1.DisplayedRowCount(true);
            if (displayed <= 0) displayed = Math.Min(rowCount, 50);
            int last = Math.Min(rowCount - 1, first + displayed + 1);

            for (int i = first; i <= last; i++)
            {
                var acc = AccountAtRow(i);
                if (acc != null) _displayCache.InvalidateAccount(acc.Id);
                dataGridView1.InvalidateRow(i);
            }
        }

        private void tableLayoutPanel1_Resize(object sender, EventArgs e)
        {
            // Coalesce rapid resize bursts — AdjustTableLayoutColumns used to Controls.Clear()
            // on every tick and could recurse layout/resize until the UI thread stalled.
            if (_resizeLayoutPending) return;

            // ctor / SsaTheme.ApplyUcAccount can resize before handle exists — no BeginInvoke yet.
            if (!IsHandleCreated)
            {
                AdjustTableLayoutColumns();
                return;
            }

            _resizeLayoutPending = true;
            BeginInvoke(new Action(() =>
            {
                _resizeLayoutPending = false;
                if (IsDisposed || tableLayoutPanel1.IsDisposed) return;
                AdjustTableLayoutColumns();
            }));
        }

        private void LoadColumnsDataGridView()
        {
            var style = new DataGridViewCellStyle
            {
                Font = FontScale.Body9Bold,
                ForeColor = ColorPalette.Primary
            };

            dataGridViewCheckBoxColumn1.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = FontScale.Body9Bold,
                ForeColor = ColorPalette.Primary,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dataGridViewTextBoxColumn1.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = FontScale.Body9Bold,
                ForeColor = ColorPalette.Primary,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dataGridViewTextBoxColumn1.ToolTipText = "Số thứ tự trong bảng";
            dataGridViewTextBoxColumn1.DataPropertyName = nameof(Account.STT);

            var columns = new List<(string Name, string Header, string Tooltip, bool visible)>
                                                                                                                                                                                                                                                                                    {
                                                                                                                                                                                                                                                                                        (nameof(Account.Uid), nameof(Account.Uid), "Uid hoặc username tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.FullName), "Họ và tên", "Họ và tên tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Password), "Mật khẩu", "Mật khẩu tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.TowFA), "2FA", "Mã xác thực tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Cookie), "Cookie", "Cookie tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Token), "Token", "Token tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Gender), "Giới tính", "Giới tính tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Bio), "Bio", "Bio tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Friends), "Bạn bè/Following", "Số lượng bạn bè hoặc following", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.PagePro5), "Page profile", "Số lượng page profile", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Groups), "Nhóm", "Số lượng nhóm", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Follow), "Follow", "Số lượng follow", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Birthday), "Ngày sinh", "Ngày sinh tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.DateCreate), "Ngày tạo", "Ngày tạo tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Avatar), "Avatar", "Tài khoản có avatar không?", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Phone), "Số điện thoại", "Số điện thoại tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Email), "Email", "Email của tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.PassMail), "Mật khẩu email", "Mật khẩu email", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.MailClientId), "Mail client id", "Mail client id của hotmail", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.MailRefreshToken), "Mail refresh token", "Mail refresh token của hotmail", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.EmailAddress), "Email khôi phục", "Email khôi phục", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.UserAgent), "User Agent", "User Agent tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.PassPrivateEmailAddress), "Mail Recover Pass", "Mật khẩu email khôi phục", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.DeviceInfo), "Thông tin thiết bị", "Thông tin thiết bị", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.NameFolder), "Nhóm", "Tên nhóm chứa tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.NameScript), "Kịch bản", "Tên kịch bản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Proxy), "Proxy", "Proxy tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Note), "Ghi chú", "Ghi chú tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.JobToday), "Hôm nay", "Số job hôm nay", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.JobTotal), "Total", "Tổng số job", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Summary), "Job success", "Thống kê job thành công", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Summary_Skip), "Job fail", "Thống kê job thất bại", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.RecentInteraction), "Lần tương tác cuối", "Lần tương tác cuối", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Serial), "Thiết bị", "Thiết bị đang đăng nhập", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.TokenJob), "Token Job", "Token của server cần chạy job", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.XuToday), "Xu", "Xu kiếm được hôm nay", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.State), "Tình trạng", "Tình trạng tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Status), "Trạng thái", "Trạng thái tài khoản", true),
                                                                                                                                                                                                                                                                                        (nameof(Account.Id), nameof(Account.Id), "", false),
                                                                                                                                                                                                                                                                                        (nameof(Account.ColorType), nameof(Account.ColorType), "", false),
                                                                                                                                                                                                                                                                                        (nameof(Account.Running), nameof(Account.Running), "", false)
                                                                                                                                                                                                                                                                                    };

            var colDefs = new List<DataGridViewColumn>();

            foreach (var col in columns)
            {
                // Skip certain columns for non-Facebook platforms
                if (_platform != PlatformModel.Facebook &&
                    (col.Name == nameof(Account.Token) ||
                     col.Name == nameof(Account.PagePro5) ||
                     col.Name == nameof(Account.Groups)))
                    continue;

                string header = col.Header;
                string tooltip = col.Tooltip;

                // Change header of Friends column based on platform
                if (col.Name == nameof(Account.Friends))
                {
                    header = _platform == PlatformModel.Facebook ? "Bạn bè" : "Following";
                }

                colDefs.Add(CreateColumnsDataGridView(
                    col.Name,
                    header,
                    tooltip,
                    col.visible,
                    col.Name == nameof(Account.Status) ? 300 : 100,
                    col.Name == nameof(Account.Status) ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None,
                    style
                ));
            }

            dataGridView1.Columns.AddRange(colDefs.ToArray());

            // Cache commonly used column indices to avoid repeated string lookups in hot paths
            _colIndexColorType = GetColumnIndexSafe(nameof(Account.ColorType));
            _colIndexRunning = GetColumnIndexSafe(nameof(Account.Running));
            _colIndexChecked = GetColumnIndexSafe(dataGridViewCheckBoxColumn1.Name) != -1
                ? GetColumnIndexSafe(dataGridViewCheckBoxColumn1.Name)
                : GetColumnIndexSafe(nameof(Account.Checked)); // fallback

            RebuildCellValueReaders();
        }

        /// <summary>
        /// Sắp xếp lại thứ tự + đặt nhãn cột giống lưới tài khoản của MaxPhoneFarm (MaxPG).
        /// Chỉ đổi DisplayIndex + HeaderText (không tạo/xoá cột) nên không ảnh hưởng
        /// VirtualMode, windowed cache hay logic hiệu năng vốn tham chiếu cột theo Name/Index.
        /// Cột nào không có trong lưới hiện tại sẽ bỏ qua an toàn.
        /// </summary>
        private void ApplyMaxPgColumnLayout()
        {
            if (dataGridView1.Columns.Count == 0) return;

            var order = new (string Prop, string Header)[]
            {
                (nameof(Account.Uid), "UID"),
                (nameof(Account.Token), "Token"),
                (nameof(Account.Cookie), "Cookie"),
                (nameof(Account.Email), "Email"),
                (nameof(Account.Phone), "Phone"),
                (nameof(Account.FullName), "Tên"),
                (nameof(Account.Follow), "Theo dõi"),
                (nameof(Account.Friends), _platform == PlatformModel.Facebook ? "Bạn bè" : "Following"),
                (nameof(Account.Groups), "Nhóm"),
                (nameof(Account.Birthday), "Ngày sinh"),
                (nameof(Account.Gender), "Giới Tính"),
                (nameof(Account.Password), "Mật khẩu"),
                (nameof(Account.EmailAddress), "Email khôi phục"),
                (nameof(Account.PassMail), "Mật khẩu mail"),
                (nameof(Account.TowFA), "Mã 2FA"),
                (nameof(Account.UserAgent), "Useragent"),
                (nameof(Account.Proxy), "Proxy"),
                (nameof(Account.DateCreate), "Ngày tạo"),
                (nameof(Account.Avatar), "Avatar"),
                (nameof(Account.PagePro5), "Profile"),
                (nameof(Account.NameFolder), "Thư mục"),
                (nameof(Account.RecentInteraction), "Lần tương tác cuối"),
                (nameof(Account.DeviceInfo), "Device Info"),
                (nameof(Account.State), "Tình trạng"),
                (nameof(Account.Note), "Ghi chú"),
                (nameof(Account.Serial), "Thiết bị"),
                (nameof(Account.IP), "IP"),
                (nameof(Account.Status), "Trạng thái"),
            };

            try
            {
                // Cột chọn + STT luôn đứng đầu (giống MaxPG: "Chọn", "STT").
                dataGridViewCheckBoxColumn1.DisplayIndex = 0;
                dataGridViewTextBoxColumn1.DisplayIndex = 1;

                int display = 2;
                foreach (var (prop, header) in order)
                {
                    int idx = GetColumnIndexSafe(prop);
                    if (idx < 0) continue;
                    var col = dataGridView1.Columns[idx];
                    col.HeaderText = header;
                    if (display < dataGridView1.Columns.Count)
                        col.DisplayIndex = display++;
                }
            }
            catch
            {
                // DisplayIndex có thể ném khi cột đang ẩn/đang khởi tạo — bỏ qua, không chặn UI.
            }
        }

        private void RebuildCellValueReaders()
        {
            if (dataGridView1.Columns.Count == 0)
            {
                _cellValueReaders = null;
                return;
            }

            var readers = new CellValueReader?[dataGridView1.Columns.Count];
            for (int i = 0; i < readers.Length; i++)
            {
                if (IsAccountSelectorColumn(i))
                {
                    readers[i] = (acc, _) => IsAccountChecked(acc);
                    continue;
                }

                var col = dataGridView1.Columns[i];
                if (col == dataGridViewTextBoxColumn1)
                {
                    readers[i] = (_, row) => row + 1;
                    continue;
                }

                string prop = col.DataPropertyName;
                if (string.IsNullOrEmpty(prop))
                    readers[i] = (_, _) => string.Empty;
                else
                {
                    int colIdx = i; // capture for closure
                    readers[i] = (acc, _) => FormatCellValueCached(acc, colIdx, prop);
                }
            }

            _cellValueReaders = readers;
        }

        /// <summary>O(1) cached cell value lookup. No allocations on cache hit.</summary>
        private object FormatCellValueCached(Account acc, int columnIndex, string prop)
        {
            object raw = GetAccountValue(acc, prop);
            if (raw == null) return string.Empty;

            // Non-string primitives: no formatting needed, return directly.
            if (raw is int or bool)
            {
                if (raw is bool b) return AccountGridPerf.FormatBool(b);
                return raw;
            }

            // Guid: use single-slot cache (same row = same Id for all columns).
            if (raw is Guid g) return FormatGuidCached(g);

            // String values: check DisplayValueCache.
            if (raw is string s)
            {
                if (s.Length == 0) return string.Empty;
                int rawHash = s.GetHashCode();
                string? cached = _displayCache.TryGet(acc.Id, columnIndex, rawHash);
                if (cached != null) return cached;

                // Cache miss: format and store.
                string formatted = (LargeListPerfMode && s.Length > 96)
                    ? string.Concat(s.AsSpan(0, 96), "…")
                    : s;
                _displayCache.Put(acc.Id, columnIndex, rawHash, formatted);
                return formatted;
            }

            return raw;
        }

        private int GetColumnIndexSafe(string dataPropOrName)
        {
            // columns stored as "col_<dataPropertyName>"
            string colName = dataPropOrName.StartsWith("col_") ? dataPropOrName : "col_" + dataPropOrName;
            if (dataGridView1.Columns.Contains(colName))
                return dataGridView1.Columns[colName].Index;
            // fallback: try raw name
            if (dataGridView1.Columns.Contains(dataPropOrName))
                return dataGridView1.Columns[dataPropOrName].Index;
            return -1;
        }

        /// <summary>
        /// Force ẩn các cột nội bộ (Id / ColorType / Running) — chạy SAU LoadConfigColums
        /// vì helper đó dùng HeaderText để match, không bền khi header bị uppercase.
        /// </summary>
        private static void ForceHideInternalColumns(DataGridView dgv)
        {
            string[] hiddenNames = { "col_Id", "col_ColorType", "col_Running" };
            foreach (var name in hiddenNames)
            {
                var col = dgv.Columns[name];
                if (col != null) col.Visible = false;
            }
        }

        private void NormalizeAccountSelectorColumn()
        {
            if (dataGridView1 == null || dataGridViewCheckBoxColumn1 == null) return;

            dataGridViewCheckBoxColumn1.Visible = true;
            dataGridViewCheckBoxColumn1.ReadOnly = true;
            dataGridViewCheckBoxColumn1.DataPropertyName = string.Empty;
            dataGridViewCheckBoxColumn1.HeaderText = "Chọn";
            dataGridViewCheckBoxColumn1.ToolTipText = "Chọn tài khoản để chạy chức năng";
            dataGridViewCheckBoxColumn1.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dataGridViewCheckBoxColumn1.Width = 56;
            dataGridViewCheckBoxColumn1.MinimumWidth = 56;
            dataGridViewCheckBoxColumn1.Resizable = DataGridViewTriState.False;
            dataGridViewCheckBoxColumn1.ValueType = typeof(bool);
            dataGridViewCheckBoxColumn1.SortMode = DataGridViewColumnSortMode.NotSortable;
            dataGridViewCheckBoxColumn1.DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Padding = Padding.Empty,
                NullValue = false,
                ForeColor = Color.Transparent,
                SelectionForeColor = Color.Transparent
            };

            if (dataGridViewCheckBoxColumn1.Index >= 0)
                dataGridViewCheckBoxColumn1.DisplayIndex = 0;

            SetAccountCheckboxCustomPaint(true);
            dataGridView1.CellClick -= DataGridView1_AccountSelectorCellClick;
            dataGridView1.CellClick += DataGridView1_AccountSelectorCellClick;
            dataGridView1.CurrentCellDirtyStateChanged -= DataGridView1_CurrentCellDirtyStateChanged;
            dataGridView1.CurrentCellDirtyStateChanged += DataGridView1_CurrentCellDirtyStateChanged;
        }

        private void SetAccountCheckboxCustomPaint(bool enabled)
        {
            // A/B bypass: never attach the custom checkbox painter → framework default render.
            if (enabled && AccountGridPerf.DisableCustomRendering)
            {
                if (_accountCheckboxPaintingHooked)
                {
                    dataGridView1.CellPainting -= DataGridView1_AccountCheckboxCellPainting;
                    _accountCheckboxPaintingHooked = false;
                }
                return;
            }
            if (enabled)
            {
                if (_accountCheckboxPaintingHooked) return;
                dataGridView1.CellPainting += DataGridView1_AccountCheckboxCellPainting;
                _accountCheckboxPaintingHooked = true;
            }
            else
            {
                if (!_accountCheckboxPaintingHooked) return;
                dataGridView1.CellPainting -= DataGridView1_AccountCheckboxCellPainting;
                _accountCheckboxPaintingHooked = false;
            }
        }

        private void DataGridView1_AccountSelectorCellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (!IsAccountSelectorColumn(e.ColumnIndex)) return;
            long _h = UiThreadProfiler.Begin();
            try
            {
            // VirtualMode: DataBoundItem luôn null → map row index vào _view.
            var account = AccountAtRow(e.RowIndex);
            if (account == null) return;

            if (_checkAllActive)
            {
                _checkAllExceptions ??= new HashSet<Guid>();
                if (_checkAllExceptions.Contains(account.Id))
                {
                    _checkAllExceptions.Remove(account.Id);
                    _cachedCheckedCount++;
                }
                else
                {
                    _checkAllExceptions.Add(account.Id);
                    _cachedCheckedCount--;
                }
            }
            else
            {
                bool was = IsAccountChecked(account);
                if (!UseSparseCheckedLookup)
                    account.SetCheckedSilently(!was);
                SyncCheckedState(account, !was);
                _cachedCheckedCount = _checkedAccounts.Count;
            }
            dataGridView1.InvalidateCell(e.ColumnIndex, e.RowIndex);
            ControlHelper.SetToolStripLabelTextSafe(toolStripLabel6, $"{_cachedCheckedCount}");
            }
            finally { AccountGridPerf.LogHandler("CheckboxTick(CellClick)", _h); }
        }

        private void DataGridView1_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            // VirtualMode: checkbox không còn editable binding → no-op, giữ để Normalize unsub an toàn.
        }

        private void DataGridView1_AccountCheckboxCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (!IsAccountSelectorColumn(e.ColumnIndex)) return;
            AccountGridPerf.RecordCellPainting();

            // A/B bypass: let the framework paint the checkbox with its default renderer.
            if (AccountGridPerf.DisableCustomRendering) return;

            long paintStart = AccountGridPerf.DiagnosticsEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            try
            {
                if (UseLightCellPaint)
                {
                    bool rowSelected = e.State.HasFlag(DataGridViewElementStates.Selected);
                    e.PaintBackground(e.CellBounds, rowSelected);
                    bool checkedState = e.Value is bool b && b;
                    ControlPaint.DrawCheckBox(e.Graphics, e.CellBounds,
                        checkedState ? ButtonState.Checked : ButtonState.Normal);
                    e.Handled = true;
                    return;
                }

                bool selected = e.State.HasFlag(DataGridViewElementStates.Selected);
                e.PaintBackground(e.CellBounds, selected);

                bool isChecked = false;
                if (e.Value is bool value)
                {
                    isChecked = value;
                }
                else
                {
                    // VirtualMode: DataBoundItem null → map row index vào _view.
                    var account = AccountAtRow(e.RowIndex);
                    if (account != null) isChecked = IsAccountChecked(account);
                }

                int boxSize = Math.Max(16, Math.Min(20, Math.Min(e.CellBounds.Width, e.CellBounds.Height) - 10));
                int boxLeft = e.CellBounds.Left + (e.CellBounds.Width - boxSize) / 2;
                int boxTop = e.CellBounds.Top + (e.CellBounds.Height - boxSize) / 2;
                var box = new Rectangle(boxLeft, boxTop, boxSize, boxSize);

                var oldSmoothing = e.Graphics.SmoothingMode;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                SolidBrush fill = isChecked ? _cbFillChecked : _cbFillUnchecked;
                Pen border = isChecked ? _cbBorderChecked : (selected ? _cbBorderSelected : _cbBorderNormal);
                e.Graphics.FillRectangle(fill, box);
                e.Graphics.DrawRectangle(border, box);

                if (isChecked)
                {
                    _cbCheckPts[0] = new Point(box.Left + boxSize / 4, box.Top + boxSize / 2);
                    _cbCheckPts[1] = new Point(box.Left + boxSize / 2 - 1, box.Bottom - boxSize / 4 - 1);
                    _cbCheckPts[2] = new Point(box.Right - boxSize / 4, box.Top + boxSize / 3);
                    e.Graphics.DrawLines(_cbCheckMark, _cbCheckPts);
                }

                e.Graphics.SmoothingMode = oldSmoothing;
                e.Handled = true;
            }
            finally
            {
                if (AccountGridPerf.DiagnosticsEnabled)
                {
                    AccountGridPerf.RecordPaint(System.Diagnostics.Stopwatch.GetTimestamp() - paintStart);
                    UiThreadProfiler.End("CellPainting.Checkbox", paintStart);
                }
            }
        }

        private bool IsAccountSelectorColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= dataGridView1.Columns.Count) return false;
            var column = dataGridView1.Columns[columnIndex];
            return ReferenceEquals(column, dataGridViewCheckBoxColumn1)
                || column.Name == dataGridViewCheckBoxColumn1.Name
                || column.DataPropertyName == nameof(Account.Checked);
        }

        private DataGridViewColumn CreateColumnsDataGridView(string dataPropertyName, string header, string toolTip, bool visible, int miniWith, DataGridViewAutoSizeColumnMode size, DataGridViewCellStyle style)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.DefaultCellStyle = style;
            column.DataPropertyName = dataPropertyName;
            column.Name = "col_" + dataPropertyName;
            column.HeaderText = header;
            column.ToolTipText = toolTip;
            column.ReadOnly = true;
            column.MinimumWidth = miniWith;
            column.Visible = visible;
            column.AutoSizeMode = size;
            // Cột [NotMapped] không sort được ở tầng DB → ẩn sort glyph để tránh hiểu nhầm.
            if (_unsortableProps.Contains(dataPropertyName))
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            return column;
        }

        private void AdjustTableLayoutColumns()
        {
            const int panelCount = 2;
            const float fixedWidth = 380f;

            int totalWidth = tableLayoutPanel1.ClientSize.Width;
            if (totalWidth <= 0) return;

            float extraSpace = totalWidth - panelCount * fixedWidth;
            float spacing = Math.Max(0f, extraSpace / Math.Max(1, panelCount - 1));
            if (_tableLayoutColumnsInitialized && Math.Abs(spacing - _lastTableLayoutSpacing) < 1f)
                return;

            _lastTableLayoutSpacing = spacing;
            tableLayoutPanel1.SuspendLayout();
            try
            {
                if (!_tableLayoutColumnsInitialized)
                {
                    _tableLayoutColumnsInitialized = true;
                    tableLayoutPanel1.ColumnCount = 3;
                    tableLayoutPanel1.ColumnStyles.Clear();
                    tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, fixedWidth));
                    tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, spacing));
                    tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, fixedWidth));

                    // Designer starts with 2 percent columns — reposition without Controls.Clear().
                    if (!tableLayoutPanel1.Controls.Contains(panel2))
                        tableLayoutPanel1.Controls.Add(panel2, 0, 0);
                    else
                        tableLayoutPanel1.SetColumn(panel2, 0);

                    if (!tableLayoutPanel1.Controls.Contains(panel3))
                        tableLayoutPanel1.Controls.Add(panel3, 2, 0);
                    else
                        tableLayoutPanel1.SetColumn(panel3, 2);
                }
                else if (tableLayoutPanel1.ColumnStyles.Count >= 2)
                {
                    tableLayoutPanel1.ColumnStyles[1].Width = spacing;
                }
            }
            finally
            {
                tableLayoutPanel1.ResumeLayout(performLayout: true);
            }
        }

        public void SaveConfig()
        {
            _configHelper?.ControlClosing(null, null);
            SaveCheckedAccounts();
        }

        private void SaveCheckedAccounts()
        {
            try
            {
                if (string.IsNullOrEmpty(_listWhere)) return;
                bool allActive = _checkAllActive;
                HashSet<Guid>? ex = _checkAllExceptions == null
                    ? null
                    : new HashSet<Guid>(_checkAllExceptions);
                HashSet<Guid>? explicitChecked = !allActive && _checkedAccountIds != null && _checkedAccountIds.Count > 0
                    ? new HashSet<Guid>(_checkedAccountIds)
                    : null;
                if (allActive && HasInMemorySearch)
                {
                    allActive = false;
                    explicitChecked = new HashSet<Guid>(GetCheckAllIncludedIds());
                    ex = null;
                }
                var where = ScopeWhere;
                var p = ScopeParams();
                _ = Task.Run(() => _accountContext.SaveCheckedStatesScoped(where, p, allActive, ex, explicitChecked));
            }
            catch { }
        }

        private static int CountChecked(IReadOnlyList<Account> accounts)
        {
            int n = 0;
            for (int i = 0; i < accounts.Count; i++)
                if (accounts[i].Checked) n++;
            return n;
        }

        private static int CountRunning(IReadOnlyList<Account> accounts)
        {
            int n = 0;
            for (int i = 0; i < accounts.Count; i++)
                if (accounts[i].Running) n++;
            return n;
        }

        private bool IsAccountChecked(Account acc)
        {
            if (acc == null || acc.Id == Guid.Empty) return false;
            if (_checkAllActive)
                return _checkAllExceptions == null || !_checkAllExceptions.Contains(acc.Id);
            if ((_useWindowedCache || UseSparseCheckedLookup) && _checkedAccountIds != null)
                return _checkedAccountIds.Contains(acc.Id);
            return acc.Checked;
        }

        private bool HasAnyChecked()
        {
            if (_checkAllActive)
                return SelectionScopeCount > (_checkAllExceptions?.Count ?? 0);
            if (_useWindowedCache || UseSparseCheckedLookup)
                return _checkedAccountIds != null && _checkedAccountIds.Count > 0;
            for (int i = 0; i < _accounts.Count; i++)
                if (_accounts[i].Checked) return true;
            return false;
        }

        private void ClearCheckedSelectionState()
        {
            _checkAllActive = false;
            _checkAllExceptions?.Clear();
            _checkedAccounts.Clear();
            _checkedAccountIds = UseSparseCheckedLookup ? new HashSet<Guid>() : null;
        }

        private void SetSparseCheckedAccounts(IReadOnlyList<Account> accounts)
        {
            _checkedAccounts.Clear();
            if (accounts.Count == 0)
            {
                _checkedAccountIds = UseSparseCheckedLookup ? new HashSet<Guid>() : null;
                return;
            }

            _checkedAccounts.AddRange(accounts);
            if (UseSparseCheckedLookup)
            {
                _checkedAccountIds = new HashSet<Guid>(accounts.Count);
                for (int i = 0; i < accounts.Count; i++)
                    _checkedAccountIds.Add(accounts[i].Id);
            }
            else
            {
                for (int i = 0; i < accounts.Count; i++)
                    accounts[i].SetCheckedSilently(true);
            }
        }

        private void SyncCheckedState(Account acc, bool nowChecked)
        {
            if (nowChecked)
            {
                if (UseSparseCheckedLookup)
                {
                    _checkedAccountIds ??= new HashSet<Guid>();
                    if (_checkedAccountIds.Add(acc.Id))
                        _checkedAccounts.Add(acc);
                }
                else
                {
                    bool exists = false;
                    for (int i = 0; i < _checkedAccounts.Count; i++)
                    {
                        if (ReferenceEquals(_checkedAccounts[i], acc)) { exists = true; break; }
                    }
                    if (!exists) _checkedAccounts.Add(acc);
                }
            }
            else if (UseSparseCheckedLookup)
            {
                _checkedAccountIds?.Remove(acc.Id);
                for (int i = _checkedAccounts.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(_checkedAccounts[i], acc))
                    {
                        _checkedAccounts.RemoveAt(i);
                        break;
                    }
                }
            }
            else
            {
                for (int i = _checkedAccounts.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(_checkedAccounts[i], acc))
                    {
                        _checkedAccounts.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        private void ApplyCheckedToAll(bool value)
        {
            if (value)
            {
                _checkAllActive = true;
                _checkAllExceptions?.Clear();
                _checkedAccounts.Clear();
                _checkedAccountIds = UseSparseCheckedLookup ? new HashSet<Guid>() : null;
                _cachedCheckedCount = SelectionScopeCount;
            }
            else
            {
                ClearCheckedSelectionState();
                if (!UseSparseCheckedLookup)
                {
                    for (int i = 0; i < _accounts.Count; i++)
                        _accounts[i].SetCheckedSilently(false);
                }
                _cachedCheckedCount = 0;
            }
            RefreshCheckedVisual(cachedCountKnown: true, repaintGrid: true);
        }

        private void ApplyCheckedByPredicate(Func<Account, bool> predicate)
        {
            _checkAllActive = false;
            _checkAllExceptions?.Clear();
            _checkedAccounts.Clear();
            for (int i = 0; i < _accounts.Count; i++)
            {
                bool check = predicate(_accounts[i]);
                _accounts[i].SetCheckedSilently(check);
                if (check) _checkedAccounts.Add(_accounts[i]);
            }
            _cachedCheckedCount = _checkedAccounts.Count;
            RefreshCheckedVisual(cachedCountKnown: true, repaintGrid: _accounts.Count <= LargeListThreshold);
        }

        private async Task ApplyCheckedByPredicateAsync(Func<Account, bool> predicate)
        {
            if (_accounts.Count <= LargeListThreshold)
            {
                ApplyCheckedByPredicate(predicate);
                return;
            }

            var accounts = _accounts;
            var checkedList = await Task.Run(() =>
            {
                var list = new List<Account>();
                for (int i = 0; i < accounts.Count; i++)
                {
                    if (predicate(accounts[i]))
                        list.Add(accounts[i]);
                }
                return list;
            });

            ClearCheckedSelectionState();
            SetSparseCheckedAccounts(checkedList);
            _cachedCheckedCount = _checkedAccounts.Count;
            RefreshCheckedVisual(cachedCountKnown: true, repaintGrid: false);
        }

        private List<Guid> GetCheckAllIncludedIds()
        {
            long _t = UiThreadProfiler.Begin();
            try
            {
            if (_useWindowedCache)
            {
                string? orderBy = _cache?.OrderBySql;
                if (orderBy != null && (_cache?.OrderDesc ?? false))
                    orderBy += " DESC";
                return _accountContext.GetIdsInScope(EffectiveWhere, EffectiveParams(), _checkAllExceptions, orderBy);
            }
            if (HasInMemorySearch)
            {
                var ids = new List<Guid>(_accounts.Count);
                for (int i = 0; i < _accounts.Count; i++)
                {
                    var id = _accounts[i].Id;
                    if (_checkAllExceptions == null || !_checkAllExceptions.Contains(id))
                        ids.Add(id);
                }
                return ids;
            }
            return _accountContext.GetIdsInScope(ScopeWhere, ScopeParams(), _checkAllExceptions);
            }
            finally { AccountGridPerf.LogN("GetCheckAllIncludedIds", _checkAllExceptions?.Count ?? 0, _t); }
        }

        private IReadOnlyList<Account> AccountsSourceForChecked()
        {
            if (_accounts != null && _accounts.Count > 0) return _accounts;
            if (_scopeAccounts != null && _scopeAccounts.Count > 0) return _scopeAccounts;
            return Array.Empty<Account>();
        }

        private List<Account> ResolveAccountsByIds(IEnumerable<Guid> ids)
        {
            var idSet = ids as HashSet<Guid> ?? ids.ToHashSet();
            if (idSet.Count == 0) return new List<Account>();

            var source = AccountsSourceForChecked();
            var result = new List<Account>(idSet.Count);
            for (int i = 0; i < source.Count; i++)
            {
                var acc = source[i];
                if (idSet.Contains(acc.Id))
                    result.Add(acc);
            }
            return result;
        }

        private List<Account> MaterializeCheckedFromMemory()
        {
            if (_checkAllActive)
            {
                var source = AccountsSourceForChecked();
                int exCount = _checkAllExceptions?.Count ?? 0;
                var list = new List<Account>(Math.Max(0, source.Count - exCount));
                if (_checkAllExceptions == null || _checkAllExceptions.Count == 0)
                {
                    if (source is List<Account> existing)
                        return existing;
                    list.AddRange(source);
                    return list;
                }
                for (int i = 0; i < source.Count; i++)
                {
                    var acc = source[i];
                    if (!_checkAllExceptions.Contains(acc.Id))
                        list.Add(acc);
                }
                return list;
            }

            if (_checkedAccountIds != null && _checkedAccountIds.Count > 0)
                return ResolveAccountsByIds(_checkedAccountIds);

            if (_checkedAccounts.Count > 0)
                return new List<Account>(_checkedAccounts);

            var manual = new List<Account>();
            for (int i = 0; i < _accounts.Count; i++)
                if (_accounts[i].Checked) manual.Add(_accounts[i]);
            return manual;
        }

        private List<Account> MaterializeCheckedForUi()
        {
            long _t = UiThreadProfiler.Begin();
            var result = _useWindowedCache ? GetCheckedAccountsForJob() : MaterializeCheckedFromMemory();
            AccountGridPerf.LogN("MaterializeCheckedForUi", result.Count, _t);
            return result;
        }

        private List<Account> GetCheckedAccountsForJob()
        {
            List<Account> result;
            List<Guid>? orderedIds = null;

            if (_checkAllActive)
            {
                var ids = GetCheckAllIncludedIds();
                // Preserve source ID order: in windowed cache, SQLite rowid order;
                // in memory (non-windowed), _accounts grid order.
                orderedIds = ids as List<Guid> ?? ids.ToList();
                result = _accountContext.GetByIdsBatched(orderedIds);
            }
            else if (_checkedAccountIds != null && _checkedAccountIds.Count > 0)
            {
                orderedIds = _checkedAccountIds.ToList();
                result = _accountContext.GetByIdsBatched(orderedIds);
            }
            else if (_checkedAccounts.Count > 0)
            {
                result = new List<Account>(_checkedAccounts);
                // _checkedAccounts giữ thứ tự grid (add dần khi check), STT đã set đúng →
                // OrderByDisplayAscending sort được.
                OrderByDisplayAscending(result);
                return result;
            }
            else
                return new List<Account>();

            // STT là [NotMapped] → không persist trong DB → accounts từ GetByIdsBatched
            // có STT=0. OrderByDisplayAscending không sort được khi _useWindowedCache=true
            // (_accounts rỗng, STT=0). Do đó dùng thứ tự IDs đầu vào để sắp xếp lại.
            if (orderedIds != null && result.Count > 1)
            {
                var map = new Dictionary<Guid, Account>(result.Count);
                foreach (var a in result)
                    if (a != null && a.Id != Guid.Empty)
                        map[a.Id] = a;

                result.Clear();
                for (int i = 0; i < orderedIds.Count; i++)
                {
                    if (map.TryGetValue(orderedIds[i], out var a))
                        result.Add(a);
                }
            }

            return result;
        }

        // Sắp xếp tài khoản tăng dần theo vị trí hiển thị trên lưới (giống STT).
        // Windowed mode: _accounts luôn rỗng, dùng STT làm key thứ tự.
        private void OrderByDisplayAscending(List<Account> list)
        {
            if (list.Count <= 1) return;

            Dictionary<Guid, int>? pos = null;
            var view = _accounts;
            if (!_useWindowedCache && view != null && view.Count > 0)
            {
                pos = new Dictionary<Guid, int>(view.Count);
                for (int i = 0; i < view.Count; i++)
                    pos[view[i].Id] = i;
            }

            int KeyOf(Account a)
            {
                if (pos != null && pos.TryGetValue(a.Id, out int idx)) return idx;
                return a.STT >= 0 ? a.STT : int.MaxValue;
            }

            list.Sort((x, y) => KeyOf(x).CompareTo(KeyOf(y)));
        }

        /// <summary>Nạp full field (Cookie, Pass…) từ DB cho acc đã chọn trước khi chạy job.</summary>
        private async Task<List<Account>> HydrateCheckedAccountsAsync(List<Account> lite)
        {
            if (lite == null || lite.Count == 0) return lite ?? new List<Account>();
            if (lite.Count <= LargeListThreshold) return lite;

            return await Task.Run(() =>
            {
                const int batch = 400;
                var map = new Dictionary<Guid, Account>(lite.Count);
                for (int i = 0; i < lite.Count; i++)
                    map[lite[i].Id] = lite[i];

                var ids = lite.Select(a => a.Id).ToList();
                for (int i = 0; i < ids.Count; i += batch)
                {
                    var chunk = ids.Skip(i).Take(batch).ToList();
                    foreach (var full in _accountContext.GetByIds(chunk))
                    {
                        if (map.TryGetValue(full.Id, out var target))
                        {
                            target.BeginBulkLoad();
                            try
                            {
                                target.Password = full.Password;
                                target.TowFA = full.TowFA;
                                target.Cookie = full.Cookie;
                                target.Token = full.Token;
                                target.Proxy = full.Proxy;
                                target.Email = full.Email;
                                target.Phone = full.Phone;
                                target.UserAgent = full.UserAgent;
                                target.PassMail = full.PassMail;
                                target.MailClientId = full.MailClientId;
                                target.MailRefreshToken = full.MailRefreshToken;
                                target.EmailAddress = full.EmailAddress;
                                target.PassPrivateEmailAddress = full.PassPrivateEmailAddress;
                                target.DeviceInfo = full.DeviceInfo;
                            }
                            finally { target.EndBulkLoad(); }
                        }
                    }
                }
                return lite;
            });
        }

        private void DataGridView_SelectionChanged(object sender, EventArgs e)
        {
            long _h = UiThreadProfiler.Begin();
            try
            {
                if (_suppressSelectionChanged || LargeListPerfMode) return;
                if (ViewRowCount > LargeListThreshold && dataGridView1.SelectedRows.Count > 200)
                    return;
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel4, dataGridView1.SelectedRows.Count.ToMoneyString());
            }
            finally { AccountGridPerf.LogHandler("SelectionChanged", _h); }
        }

        /// <summary>Danh sách account đã tick — nạp từ DB khi cần full field.</summary>
        private IReadOnlyList<Account> CheckedAccounts => MaterializeCheckedForUi();

        /// <summary>Account ứng với row index hiện trên grid (VirtualMode → _accounts hoặc cache).</summary>
        private Account? AccountAtRow(int rowIndex)
        {
            if (rowIndex == _accountCacheRow)
                return _accountCacheRef;

            Account? acc;
            if (_useWindowedCache)
                acc = _cache?.GetRow(rowIndex);
            else
            {
                acc = rowIndex >= 0 && rowIndex < _accounts.Count ? _accounts[rowIndex] : null;
                // Realtime (non-windowed): job chạy trên instance fetch-từ-DB qua
                // GetCheckedAccountsForJob (GetByIdsBatched) — KHÁC instance đang nằm trong
                // _accounts mà grid hiển thị. MainService mutate Status/State trên instance
                // live đó nên nếu đọc thẳng _accounts thì cột Trạng thái/Tình trạng chỉ đổi
                // sau khi job xong (refetch DB). Swap sang instance live theo Id để cập nhật
                // ngay trong lúc chạy — tương đương cơ chế _cache.SetLiveResolver của windowed.
                if (acc != null && _liveRunningById != null
                    && _liveRunningById.TryGetValue(acc.Id, out var live) && live != null)
                    acc = live;
            }

            _accountCacheRow = rowIndex;
            _accountCacheRef = acc;
            return acc;
        }

        private void InvalidateAccountRowCache()
        {
            _accountCacheRow = -1;
            _accountCacheRef = null;
            // Clear display cache when data changes fundamentally (page eviction, reset).
            _displayCache.Clear();
        }

        private int ViewRowCount => _useWindowedCache ? (_cache?.TotalCount ?? 0) : (_accounts?.Count ?? 0);

        private int EstimateVisibleRowCount()
        {
            if (dataGridView1.IsDisposed || dataGridView1.RowCount <= 0) return 0;
            int rowHeight = Math.Max(1, dataGridView1.RowTemplate.Height);
            int header = dataGridView1.ColumnHeadersVisible ? dataGridView1.ColumnHeadersHeight : 0;
            int clientH = Math.Max(0, dataGridView1.ClientSize.Height - header);
            return Math.Min(dataGridView1.RowCount, Math.Max(1, clientH / rowHeight + 2));
        }

        private void EmitDiagnosticsSnapshot()
        {
            if (!AccountGridPerf.DiagnosticsEnabled || IsDisposed) return;

            long marshals = Interlocked.Exchange(ref _uiMarshalLocal, 0);
            var (hits, misses) = _cache?.TakeCacheStatsSnapshot() ?? (0L, 0L);

            var audit = new AccountGridPerf.MemoryAudit(
                CacheAccountInstances: _cache?.CachedRowCount ?? 0,
                ScopeAccountInstances: _scopeAccounts?.Count ?? 0,
                InMemoryViewInstances: _accounts?.Count ?? 0,
                CheckedMaterialized: _checkedAccounts.Count,
                CachePages: _cache?.PageCount ?? 0,
                InFlightPages: _cache?.InFlightPageCount ?? 0,
                MaxRetainedRows: _cache?.MaxRetainedRows ?? 0,
                ViewAliasesScope: ReferenceEquals(_accounts, _scopeAccounts));

            AccountGridPerf.EmitSnapshot(
                EstimateVisibleRowCount(),
                audit.CachePages,
                audit.CacheAccountInstances,
                hits,
                misses,
                _cache?.LastPageFetchMs ?? 0,
                audit,
                marshals);

            // Periodic memory/GC + retention — captures pressure during continuous scroll.
            AccountGridPerf.LogMemoryStats(_cache?.TotalCount ?? ViewRowCount, "Periodic30s");
            EmitAccountRetention("Periodic30s");
        }

        private void InvalidateRowRange(int startRow, int endRow)
        {
            RunOnUi(() =>
            {
                if (dataGridView1.IsDisposed || !dataGridView1.IsHandleCreated) return;
                long _t = UiThreadProfiler.Begin();
                InvalidateAccountRowCache();
                // Trang vừa nạp có thể nằm ngoài màn hình; WinForms chỉ vẽ dòng đang hiển thị,
                // nên 1 lệnh invalidate vùng hiển thị là đủ — KHÔNG loop InvalidateRow (gây đơ).
                // Khi grid chưa layout (lần đầu mở tab), DisplayRectangle = {0,0,0,0} → fallback Invalidate().
                var rect = dataGridView1.DisplayRectangle;
                if (rect.Width > 0 && rect.Height > 0)
                    dataGridView1.Invalidate(rect);
                else
                    dataGridView1.Invalidate();
                UiThreadProfiler.End("InvalidateRowRange", _t);
            });
        }

        /// <summary>Account của các row đang bôi đen (highlight). VirtualMode: map theo row.Index vào _view.</summary>
        private IEnumerable<Account> SelectedAccounts
        {
            get
            {
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    var acc = AccountAtRow(row.Index);
                    if (acc != null) yield return acc;
                }
            }
        }

        // Đọc tất cả acc trong range [minRow, maxRow] — tránh giới hạn của DataGridView.SelectedRows
        // khi user Shift+Click hoặc Ctrl+A trên list lớn (windowed cache chỉ giữ ~600 row trong bộ nhớ).
        private List<Account> SelectedAccountsByRange()
        {
            if (dataGridView1.SelectedRows.Count == 0) return new List<Account>();

            int minRow = int.MaxValue, maxRow = int.MinValue;
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                if (row.Index < minRow) minRow = row.Index;
                if (row.Index > maxRow) maxRow = row.Index;
            }

            // In-memory list: đọc trực tiếp từ _accounts (không bị giới hạn buffer SelectedRows)
            if (_accounts != null && !_useWindowedCache)
            {
                int from = Math.Max(0, minRow);
                int to   = Math.Min(_accounts.Count - 1, maxRow);
                var result = new List<Account>(to - from + 1);
                for (int i = from; i <= to; i++)
                {
                    var acc = _accounts[i];
                    if (acc != null) result.Add(acc);
                }
                return result;
            }

            // Windowed cache: SelectedRows chỉ trả về row trong viewport (~50 dòng).
            // Dùng DB query LIMIT/OFFSET để lấy đúng range [minRow, maxRow].
            if (_useWindowedCache && _cache != null)
            {
                int from  = Math.Max(0, minRow);
                int count = maxRow - from + 1;
                if (count <= 0) return new List<Account>();
                try
                {
                    return _accountContext.GetListPage(
                        _cache.WhereClause, _cache.Parameters,
                        from, count, _cache.OrderBySql + (_cache.OrderDesc ? " DESC" : " ASC"));
                }
                catch { /* fallthrough to SelectedRows */ }
            }

            // Fallback: list nhỏ hoặc lỗi DB
            return SelectedAccounts.ToList();
        }

        internal void EnsureStatusBadgePainter()
        {
            if (AccountGridPerf.DisableCustomRendering) return;
            if (_statusBadgePaintHooked || LargeListPerfMode || dataGridView1.IsDisposed) return;
            if (_useWindowedCache && IsLargeVirtualList) return;
            _statusBadgeCellPaintingHandler ??= (_, e) =>
            {
                long paintStart = AccountGridPerf.DiagnosticsEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
                try { SsaTheme.PaintAccountStatusBadge(dataGridView1, e); }
                finally
                {
                    if (AccountGridPerf.DiagnosticsEnabled)
                    {
                        AccountGridPerf.RecordPaint(System.Diagnostics.Stopwatch.GetTimestamp() - paintStart);
                        UiThreadProfiler.End("CellPainting.StatusBadge", paintStart);
                    }
                }
            };
            dataGridView1.CellPainting += _statusBadgeCellPaintingHandler;
            _statusBadgePaintHooked = true;
        }

        /// <summary>Windowed 30k+: plain text status, fast checkbox — giữ đủ cột, không perf mode.</summary>
        private void ApplyWindowedLargeListUiOptimizations()
        {
            if (!_useWindowedCache || !IsLargeVirtualList) return;
            DetachStatusBadgePainter();
        }

        internal void DetachStatusBadgePainter()
        {
            if (!_statusBadgePaintHooked || _statusBadgeCellPaintingHandler == null) return;
            dataGridView1.CellPainting -= _statusBadgeCellPaintingHandler;
            _statusBadgePaintHooked = false;
        }

        private void ApplyLargeListColumnLayout()
        {
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                if (col == null) continue;
                if (col.AutoSizeMode == DataGridViewAutoSizeColumnMode.Fill)
                {
                    _columnsFillModeForPerf.Add(col.Name);
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    if (col.Width < 120)
                        col.Width = Math.Max(col.Width, 180);
                }
            }

            HideNonEssentialColumnsForPerf();
            ApplyPerfTypography();

            int visible = 0;
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                if (col?.Visible == true) visible++;
            }

            System.Diagnostics.Debug.WriteLine(
                $"[AccountGrid] PerfColumns: {visible}/{dataGridView1.Columns.Count} visible, hidden={_columnsHiddenForPerf.Count}");
        }

        private void HideNonEssentialColumnsForPerf()
        {
            _columnsHiddenForPerf.Clear();
            _columnsForceShownForPerf.Clear();
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                if (col == null) continue;
                bool show = LargeListPerfVisibleColumns.Contains(col.Name);
                if (show)
                {
                    if (!col.Visible)
                    {
                        col.Visible = true;
                        _columnsForceShownForPerf.Add(col.Name);
                    }
                }
                else if (col.Visible)
                {
                    col.Visible = false;
                    _columnsHiddenForPerf.Add(col.Name);
                }
            }
        }

        private void ApplyPerfTypography()
        {
            dataGridView1.DefaultCellStyle.Font = FontScale.Body9;
            dataGridViewTextBoxColumn1.DefaultCellStyle.Font = FontScale.Body9;
            dataGridViewCheckBoxColumn1.DefaultCellStyle.Font = FontScale.Body9;
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                if (col?.DefaultCellStyle == null) continue;
                col.DefaultCellStyle.Font = FontScale.Body9;
            }
        }

        private void RestorePerfTypography()
        {
            dataGridView1.DefaultCellStyle.Font = FontScale.Body9Bold;
            dataGridViewTextBoxColumn1.DefaultCellStyle.Font = FontScale.Body9Bold;
            dataGridViewCheckBoxColumn1.DefaultCellStyle.Font = FontScale.Body9Bold;
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                if (col?.DefaultCellStyle == null) continue;
                col.DefaultCellStyle.Font = FontScale.Body9Bold;
            }
        }

        private static void SetGridDoubleBuffered(DataGridView dgv, bool enabled)
        {
            if (dgv.IsDisposed) return;
            typeof(DataGridView).InvokeMember(
                "DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null,
                dgv,
                new object[] { enabled });
        }

        private void EnterLargeListPerfMode()
        {
            if (LargeListPerfMode) return;
            LargeListPerfMode = true;
            if (_cellFormattingHooked)
            {
                dataGridView1.CellFormatting -= uiDataGridView1_CellFormatting;
                _cellFormattingHooked = false;
            }
            // Custom CellPainting trên hàng chục nghìn dòng → hàng trăm lần Paint/scroll, UI đơ.
            SetAccountCheckboxCustomPaint(false);
            GridStyleHelper.SetRowHoverEnabled(dataGridView1, false);
            dataGridView1.RowHeadersVisible = false;
            DetachStatusBadgePainter();
            ApplyLargeListColumnLayout();
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor =
                dataGridView1.DefaultCellStyle.BackColor;
            RebuildCellValueReaders();
            if (!_gridDoubleBuffered)
            {
                SetGridDoubleBuffered(dataGridView1, true);
                _gridDoubleBuffered = true;
            }
        }

        private void ExitLargeListPerfMode()
        {
            if (!LargeListPerfMode) return;
            LargeListPerfMode = false;
            if (!_cellFormattingHooked)
            {
                dataGridView1.CellFormatting += uiDataGridView1_CellFormatting;
                _cellFormattingHooked = true;
            }
            SetAccountCheckboxCustomPaint(true);
            GridStyleHelper.SetRowHoverEnabled(dataGridView1, true);
            RestoreColumnsAfterPerf();
            EnsureStatusBadgePainter();
        }

        private void RestoreColumnsAfterPerf()
        {
            foreach (var name in _columnsFillModeForPerf)
            {
                if (dataGridView1.Columns.Contains(name))
                    dataGridView1.Columns[name].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            _columnsFillModeForPerf.Clear();

            if (_columnsHiddenForPerf.Count > 0)
            {
                foreach (DataGridViewColumn col in dataGridView1.Columns)
                {
                    if (col != null && _columnsHiddenForPerf.Contains(col.Name))
                        col.Visible = true;
                }
                _columnsHiddenForPerf.Clear();
            }

            if (_columnsForceShownForPerf.Count > 0)
            {
                foreach (var name in _columnsForceShownForPerf)
                {
                    if (dataGridView1.Columns.Contains(name))
                        dataGridView1.Columns[name].Visible = false;
                }
                _columnsForceShownForPerf.Clear();
            }

            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = AlternatingRowBackColor;
            RestorePerfTypography();
        }

        private void UpdatePageLabel()
        {
            if (!HasInMemorySearch)
            {
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel8, _scopeCount.ToMoneyString());
            }
            else
            {
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel8,
                    $"{ViewCount.ToMoneyString()} / {_scopeCount.ToMoneyString()}");
            }

            SyncEmptyStateOverlay();
        }

        /// <summary>
        /// EmptyStateView (SsaTheme) chỉ toggle qua RowsAdded/Removed — VirtualMode gán RowCount trực tiếp
        /// không fire các event đó → overlay che grid dù đã có 50k dòng.
        /// </summary>
        private void SyncEmptyStateOverlay()
        {
            var es = GetEmptyStateControl();
            if (es == null) return;
            bool showEmpty = ViewRowCount == 0;
            if (es is EmptyStateView emptyView)
                emptyView.AllowHitTest = showEmpty;

            if (es.Visible != showEmpty)
                es.Visible = showEmpty;
            es.Enabled = showEmpty;

            if (showEmpty)
            {
                if (es.Dock != DockStyle.Fill)
                    es.Dock = DockStyle.Fill;
                es.BringToFront();
            }
            else
            {
                // Thu gọn overlay — Dock=Fill + Transparent vẫn có thể nuốt click trên một số theme.
                es.Dock = DockStyle.None;
                es.Size = Size.Empty;
                es.Location = Point.Empty;
                es.SendToBack();
                es.TabStop = false;
                if (!dataGridView1.IsDisposed)
                {
                    dataGridView1.Enabled = true;
                    dataGridView1.BringToFront();
                }
            }

            if (AccountGridPerf.DiagnosticsEnabled)
                AccountGridPerf.LogUiState(this, dataGridView1, es, ViewRowCount, LargeListPerfMode);
        }

        /// <summary>Cache the ssaEmptyState overlay control reference; avoids the recursive
        /// Controls.Find("ssaEmptyState", true) on every SyncEmptyStateOverlay call. Re-resolves
        /// until found (overlay is created during SsaTheme.ApplyUcAccount init), then sticks.</summary>
        private Control? GetEmptyStateControl()
        {
            if (_emptyStateControl != null && !_emptyStateControl.IsDisposed)
                return _emptyStateControl;
            if (panel5 == null || panel5.IsDisposed) return null;
            var matches = panel5.Controls.Find("ssaEmptyState", true);
            _emptyStateControl = matches.Length > 0 ? matches[0] : null;
            return _emptyStateControl;
        }

        private static void ReleaseScopeAccounts(IReadOnlyList<Account>? previousScope)
        {
            if (previousScope == null || previousScope.Count == 0) return;
            ThrottledPropertyNotifier.UnregisterMany(previousScope);
        }

        private void QueueBindViewWindow()
        {
            int gen = ++_bindGeneration;
            void schedule()
            {
                if (gen != _bindGeneration || IsDisposed) return;
                // VirtualMode: RowCount is metadata only (cheap) — bind once and paint visible rows.
                BindViewWindow();
            }

            if (!IsHandleCreated || !InvokeRequired)
            {
                schedule();
                return;
            }

            BeginInvoke(schedule);
        }

        private void RefreshLargeGridVisibleArea()
        {
            if (!dataGridView1.IsHandleCreated) return;
            var rect = dataGridView1.DisplayRectangle;
            if (rect.Width > 0 && rect.Height > 0)
                dataGridView1.Invalidate(rect);
            else
                dataGridView1.Invalidate();
        }

        /// <summary>Đổi nhóm/filter khi RowCount không đổi (vd 30k→30k) → grid không tự repaint.
        /// Cuộn về đầu + invalidate toàn bộ để CellValueNeeded đọc lại dữ liệu trang mới.</summary>
        private void ResetScrollAndRepaint()
        {
            if (dataGridView1.IsDisposed || !dataGridView1.IsHandleCreated) return;
            bool diag = AccountGridPerf.DiagnosticsEnabled;
            long r0 = diag ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            InvalidateAccountRowCache();
            long rInv = diag ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            try
            {
                if (dataGridView1.RowCount > 0)
                {
                    int targetRow = 0;
                    if (_preserveScrollRow >= 0)
                    {
                        targetRow = Math.Min(_preserveScrollRow, dataGridView1.RowCount - 1);
                        _preserveScrollRow = -1;
                    }
                    dataGridView1.FirstDisplayedScrollingRowIndex = targetRow;
                }
            }
            catch { /* RowCount có thể đang chuyển trạng thái */ }
            long rScroll = diag ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            dataGridView1.Invalidate();
            if (diag)
            {
                long rInvalidate = System.Diagnostics.Stopwatch.GetTimestamp();
                AccountGridPerf.LogLine(
                    "[BindBreakdown-Reset] RowCount=" + dataGridView1.RowCount +
                    " Total=" + TsMs(r0, rInvalidate).ToString("0.000") + "ms" +
                    " | InvalidateRowCache(+DisplayCache.Clear)=" + TsMs(r0, rInv).ToString("0.000") +
                    " FirstDisplayedScrollingRowIndex=" + TsMs(rInv, rScroll).ToString("0.000") +
                    " Invalidate(wholeGrid)=" + TsMs(rScroll, rInvalidate).ToString("0.000"));
                UiThreadProfiler.End("ResetScrollAndRepaint", r0);
            }
        }

        private void BindViewWindow()
        {
            _suppressSelectionChanged = true;
            InvalidateAccountRowCache();
            int count = ViewRowCount;
            if (count == dataGridView1.RowCount)
            {
                _suppressSelectionChanged = false;
                UpdatePageLabel();   // already syncs the empty-state overlay
                if (count > 0 && dataGridView1.IsHandleCreated)
                {
                    RefreshLargeGridVisibleArea();
                    var rect = dataGridView1.DisplayRectangle;
                    if (rect.Width <= 0 || rect.Height <= 0)
                        BeginInvoke(new Action(RefreshLargeGridVisibleArea));
                }
                return;
            }

            var sw = AccountGridPerf.Start();
            try
            {
                SetRedraw(dataGridView1, false);
                dataGridView1.SuspendLayout();
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                dataGridView1.RowCount = count > 0 ? count : 0;
            }
            finally
            {
                dataGridView1.ResumeLayout(performLayout: false);
                SetRedraw(dataGridView1, true);
                _suppressSelectionChanged = false;
            }
            AccountGridPerf.Log(sw, "BindViewWindow", count);
            UpdatePageLabel();
            if (count > 0 && dataGridView1.IsHandleCreated)
            {
                RefreshLargeGridVisibleArea();
                // Nếu grid chưa layout (DisplayRectangle trống), delay 1 frame để layout xong
                // rồi trigger CellValueNeeded → BeginFetch → data hiện sau debounce.
                var rect = dataGridView1.DisplayRectangle;
                if (rect.Width <= 0 || rect.Height <= 0)
                    BeginInvoke(new Action(RefreshLargeGridVisibleArea));
            }
        }

        private static double TsMs(long fromTs, long toTs)
            => (double)(toTs - fromTs) / System.Diagnostics.Stopwatch.Frequency * 1000.0;

        /// <summary>
        /// [AccountRetention]: how many live Account objects are held across each collection.
        /// Answers "are 30k Account objects retained / duplicated?" — in windowed mode _accounts,
        /// _scopeAccounts and _fullView should be ~0; only the window cache (~≤600) holds objects.
        /// </summary>
        private void EmitAccountRetention(string phase)
        {
            if (!AccountGridPerf.DiagnosticsEnabled) return;
            int accountsList = _accounts?.Count ?? 0;
            int scope = _scopeAccounts?.Count ?? 0;
            int fullView = _fullView?.Count ?? 0;
            int cacheRows = _cache?.CachedRowCount ?? 0;
            int cachePages = _cache?.PageCount ?? 0;
            int checkedList = _checkedAccounts.Count;
            int checkedIds = _checkedAccountIds?.Count ?? 0;
            int checkAllEx = _checkAllExceptions?.Count ?? 0;
            int scopedIds = _scopedIdList?.Count ?? 0;
            int displayCache = _displayCache.Count;
            bool viewAliasesScope = ReferenceEquals(_accounts, _scopeAccounts);
            bool fullViewAliasesAccounts = ReferenceEquals(_fullView, _accounts);
            // Distinct live Account objects = window-cache rows + scope (if not aliased) + accounts (if not aliased) + checked-list extras.
            int approxLiveAccountObjects = cacheRows
                + (viewAliasesScope ? 0 : scope)
                + (fullViewAliasesAccounts ? 0 : accountsList)
                + checkedList;
            AccountGridPerf.LogLine(
                "[AccountRetention] Phase=" + phase + " TotalCount=" + (_cache?.TotalCount ?? ViewRowCount) +
                " | _accounts=" + accountsList + " _scopeAccounts=" + scope + " _fullView=" + fullView +
                " WindowCacheRows=" + cacheRows + " WindowCachePages=" + cachePages +
                " _checkedAccounts=" + checkedList + " _checkedAccountIds=" + checkedIds +
                " _checkAllExceptions=" + checkAllEx + " _scopedIdList=" + scopedIds +
                " DisplayCacheEntries=" + displayCache +
                " | ViewAliasesScope=" + viewAliasesScope + " FullViewAliasesAccounts=" + fullViewAliasesAccounts +
                " | ApproxLiveAccountObjects=" + approxLiveAccountObjects);
        }

        private void EmitBindBreakdown(int count, long t0,
            long tInvalidateCache, long tSetRedrawOff, long tSuspend, long tAutoSize, long tRowCount,
            long tResume, long tSetRedrawOn, long tSuppress, long tPageLabel, long tEmptyOverlay, long tRefreshArea)
        {
            var sb = new System.Text.StringBuilder(360);
            sb.Append("[BindBreakdown] Accounts=").Append(count)
              .Append(" Total=").Append(TsMs(t0, tRefreshArea).ToString("0.000")).Append("ms")
              .Append(" | InvalidateRowCache(+DisplayCache.Clear)=").Append(TsMs(t0, tInvalidateCache).ToString("0.000"))
              .Append(" SetRedraw(off)=").Append(TsMs(tInvalidateCache, tSetRedrawOff).ToString("0.000"))
              .Append(" SuspendLayout=").Append(TsMs(tSetRedrawOff, tSuspend).ToString("0.000"))
              .Append(" AutoSizeColumnsMode=").Append(TsMs(tSuspend, tAutoSize).ToString("0.000"))
              .Append(" RowCount=").Append(TsMs(tAutoSize, tRowCount).ToString("0.000"))
              .Append(" ResumeLayout=").Append(TsMs(tRowCount, tResume).ToString("0.000"))
              .Append(" SetRedraw(on)=").Append(TsMs(tResume, tSetRedrawOn).ToString("0.000"))
              .Append(" SuppressFlag=").Append(TsMs(tSetRedrawOn, tSuppress).ToString("0.000"))
              .Append(" UpdatePageLabel=").Append(TsMs(tSuppress, tPageLabel).ToString("0.000"))
              .Append(" SyncEmptyStateOverlay=").Append(TsMs(tPageLabel, tEmptyOverlay).ToString("0.000"))
              .Append(" Invalidate(visibleArea)=").Append(TsMs(tEmptyOverlay, tRefreshArea).ToString("0.000"));
            AccountGridPerf.LogLine(sb.ToString());
        }

        /// <summary>
        /// Diagnostic: prove whether the grid is a true virtual grid or a materialized N-row UI grid.
        /// Logged immediately after the RowCount assignment in BindViewWindow.
        /// The decisive metric is SharedRowCount vs RowCount: in a true virtual grid almost all rows
        /// are shared, so Unshared = RowCount - SharedRowCount stays tiny (≈ visible rows). If Unshared
        /// approaches RowCount, the grid has materialized 30k DataGridViewRow objects.
        /// </summary>
        private void LogGridVirtualizationState(long gcBefore)
        {
            if (!AccountGridPerf.DiagnosticsEnabled) return;
            try
            {
                long gcAfter = GC.GetTotalMemory(false);
                int rowCount = dataGridView1.RowCount;
                int rowsCount = dataGridView1.Rows.Count;

                // Count materialized (unshared) rows the AOT-safe way: SharedRow(i) returns the
                // shared template (Index == -1) for shared rows, or the real row (Index >= 0) if
                // that row has been unshared/materialized. No unsharing side-effect.
                int materialized = 0;
                var rows = dataGridView1.Rows;
                for (int i = 0; i < rowCount; i++)
                {
                    if (rows.SharedRow(i).Index != -1) materialized++;
                }
                int shared = rowCount - materialized;
                int displayed = dataGridView1.IsHandleCreated ? dataGridView1.DisplayedRowCount(false) : 0;
                bool dataSourceNull = dataGridView1.DataSource == null;

                var sb = new System.Text.StringBuilder(320);
                sb.Append("[GridVirt] ")
                  .Append("VirtualMode=").Append(dataGridView1.VirtualMode)
                  .Append(" DataSourceNull=").Append(dataSourceNull)
                  .Append(" RowCount=").Append(rowCount)
                  .Append(" Rows.Count=").Append(rowsCount)
                  .Append(" SharedRows=").Append(shared)
                  .Append(" Materialized(unshared)=").Append(materialized)
                  .Append(" DisplayedRows=").Append(displayed)
                  .Append(" WindowCacheRows=").Append(_cache?.CachedRowCount ?? 0)
                  .Append(" WindowCachePages=").Append(_cache?.PageCount ?? 0)
                  .Append(" ViewCount=").Append(ViewRowCount)
                  .Append(" HeapDeltaKB=").Append((gcAfter - gcBefore) / 1024);
                System.Diagnostics.Debug.WriteLine(sb.ToString());
                AccountGridPerf.LogLine(sb.ToString());
            }
            catch { /* diagnostics must never break the UI */ }
        }

        private static AccountContext.ListStats ComputeStatsFromAccounts(IReadOnlyList<Account> accounts)
        {
            var stats = new AccountContext.ListStats { Total = accounts.Count };
            for (int i = 0; i < accounts.Count; i++)
            {
                var acc = accounts[i];
                string state = acc.State ?? "";
                if (state.Equals("LIVE", StringComparison.OrdinalIgnoreCase)) stats.Live++;
                else if (state.Equals("DIE", StringComparison.OrdinalIgnoreCase)
                         || state == "CP_282" || state == "CP_956") stats.Die++;
                else stats.Other++;

                if (acc.Checked) stats.Checked++;
            }
            return stats;
        }

        private static CheckedRestoreState ComputeCheckedRestoreInMemory(
            IReadOnlyList<Account> scope, string? searchTerm, int checkedCount, int scopeCount)
        {
            var state = new CheckedRestoreState();
            if (checkedCount == 0) return state;
            if (checkedCount == scopeCount && string.IsNullOrWhiteSpace(searchTerm))
            {
                state.CheckAllActive = true;
                state.CheckAllExceptions = new HashSet<Guid>();
                return state;
            }

            state.CheckedAccountIds = new HashSet<Guid>(checkedCount);
            for (int i = 0; i < scope.Count; i++)
            {
                if (scope[i].Checked)
                    state.CheckedAccountIds.Add(scope[i].Id);
            }
            return state;
        }

        private CheckedRestoreState ComputeCheckedRestoreForScope(
            IReadOnlyList<Account> scope,
            string scopeWhere,
            Dictionary<string, object> scopeParams,
            string? searchTerm,
            int checkedCount,
            int scopeCount)
        {
            if (checkedCount == 0) return new CheckedRestoreState();
            if (checkedCount == scopeCount && string.IsNullOrWhiteSpace(searchTerm))
            {
                return new CheckedRestoreState
                {
                    CheckAllActive = true,
                    CheckAllExceptions = new HashSet<Guid>()
                };
            }

            if (scopeCount > LargeListThreshold)
            {
                var ids = _accountContext.GetIdsInScope(
                    $"{scopeWhere} AND {nameof(Account.Checked)} = 1", scopeParams);
                return new CheckedRestoreState { CheckedAccountIds = new HashSet<Guid>(ids) };
            }

            return ComputeCheckedRestoreInMemory(scope, searchTerm, checkedCount, scopeCount);
        }

        private static void PreparePageAccounts(List<Account> accounts, int sttOffset)
        {
            for (int i = 0; i < accounts.Count; i++)
            {
                var acc = accounts[i];
                acc.BeginBulkLoad();
                try
                {
                    if (acc.State == "LIVE") acc.ColorType = 2;
                    else if (acc.State == "DIE" || acc.State == "CP_282" || acc.State == "CP_956") acc.ColorType = 1;
                    else acc.ColorType = 0;
                    acc.STT = sttOffset + i + 1;
                    if (string.IsNullOrEmpty(acc.NameScript)) acc.NameScript = ScriptNames.FarmXuVip;
                    acc.JobToday = "0/0";
                    acc.XuToday = "";
                    // Reset status runtime về "Chưa chạy" mỗi lần load để dễ nhận biết
                    // acc nào đã chạy trong phiên hiện tại (chỉ memory, không flush DB).
                    acc.Status = "Chưa chạy";
                }
                finally { acc.EndBulkLoad(); }
            }
        }

        private (string where, Dictionary<string, object> parameters) BuildListQuery(string selectedFolder)
        {
            var parameters = new Dictionary<string, object>
            {
                ["@platformt"] = _platform,
                ["@isView"] = 1,
            };
            var clauses = new List<string>
            {
                $"{nameof(Account.Platformt)} = @platformt",
                $"{nameof(Account.IsView)} = @isView"
            };

            if (selectedFolder == "[ Chọn theo uid ]")
            {
                string uidPath = Path.Combine(Path.GetTempPath(), $"uids_{_platform}.txt");
                if (File.Exists(uidPath))
                {
                    var uids = File.ReadLines(uidPath)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim())
                        .Where(x => x.All(char.IsDigit))
                        .Distinct()
                        .ToList();
                    if (uids.Any())
                    {
                        clauses.Add($"{nameof(Account.Uid)} IN ({string.Join(",", uids.Select(u => $"'{u}'"))})");
                    }
                }
            }
            else if (selectedFolder == "[ Chọn nhiều nhóm ]")
            {
                string folderPath = Path.Combine(Path.GetTempPath(), $"folders_{_platform}.txt");
                if (File.Exists(folderPath))
                {
                    var folders = File.ReadLines(folderPath).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                    if (folders.Any())
                    {
                        for (int i = 0; i < folders.Count; i++)
                            parameters[$"@folder{i}"] = folders[i];
                        clauses.Add($"{nameof(Account.NameFolder)} IN ({string.Join(",", folders.Select((_, i) => $"@folder{i}"))})");
                    }
                }
            }
            else if (selectedFolder == "[ Tài khoản đã xóa ]")
            {
                parameters["@isView"] = 0;
            }
            else if (selectedFolder != "[ Tất cả các nhóm ]")
            {
                parameters["@folder"] = selectedFolder;
                clauses.Add($"{nameof(Account.NameFolder)} = @folder");
            }

            return (string.Join(" AND ", clauses), parameters);
        }

        private async Task<List<Account>> LoadScopeAccountsAsync(CancellationToken token)
        {
            if (string.IsNullOrEmpty(_listWhere))
                return new List<Account>();

            var sw = AccountGridPerf.Start();
            var scopeWhere = ScopeWhere;
            var scopeParams = ScopeParams();
            var scopedIds = _scopedIdList;

            var loaded = await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                List<Account> accounts;
                if (scopedIds != null)
                {
                    accounts = _accountContext.GetByIdsBatched(scopedIds);
                    var order = new Dictionary<Guid, int>(scopedIds.Count);
                    for (int i = 0; i < scopedIds.Count; i++)
                        order[scopedIds[i]] = i;
                    accounts.Sort((a, b) => order[a.Id].CompareTo(order[b.Id]));
                }
                else
                {
                    accounts = _accountContext.GetListAll(scopeWhere, scopeParams);
                }

                PreparePageAccounts(accounts, 0);
                return accounts;
            }, token).ConfigureAwait(false);

            AccountGridPerf.Log(sw, "LoadScope", loaded.Count);
            return loaded;
        }

        private static List<Account> BuildViewList(IReadOnlyList<Account> scope, string? searchTerm)
        {
            List<Account> view = string.IsNullOrWhiteSpace(searchTerm)
                ? scope is List<Account> list ? list : scope.ToList()
                : AccountListCache.FilterBySearch(scope, searchTerm);
            if (view.Count <= LargeListThreshold)
                AccountListCache.ReindexStt(view);
            return view;
        }

        private void ApplyCheckedRestore(CheckedRestoreState state)
        {
            long _t = UiThreadProfiler.Begin();
            ClearCheckedSelectionState();
            if (state.CheckAllActive)
            {
                _checkAllActive = true;
                _checkAllExceptions = state.CheckAllExceptions ?? new HashSet<Guid>();
                _checkedAccountIds = UseSparseCheckedLookup ? new HashSet<Guid>() : null;
                AccountGridPerf.LogN("ApplyCheckedRestore", _checkAllExceptions.Count, _t);
                return;
            }

            if (state.CheckedAccountIds == null || state.CheckedAccountIds.Count == 0)
            {
                AccountGridPerf.LogN("ApplyCheckedRestore", 0, _t);
                return;
            }
            _checkedAccountIds = state.CheckedAccountIds;
            if (UseSparseCheckedLookup) { AccountGridPerf.LogN("ApplyCheckedRestore", _checkedAccountIds.Count, _t); return; }

            for (int i = 0; i < _accounts.Count; i++)
            {
                if (_checkedAccountIds.Contains(_accounts[i].Id))
                    _checkedAccounts.Add(_accounts[i]);
            }
            AccountGridPerf.LogN("ApplyCheckedRestore", _checkedAccountIds.Count, _t);
        }

        /// <summary>Clause LIKE tìm kiếm server-side (dùng @__search param).</summary>
        private static string SearchClause()
            => $" AND ({nameof(Account.Uid)} LIKE @__search OR {nameof(Account.FullName)} LIKE @__search"
             + $" OR {nameof(Account.Note)} LIKE @__search OR {nameof(Account.Status)} LIKE @__search"
             + $" OR {nameof(Account.NameScript)} LIKE @__search OR {nameof(Account.TokenJob)} LIKE @__search)";

        private async Task ReloadFromDbAsync()
        {
            var prevLoadCts = _loadCts;
            _loadCts = new CancellationTokenSource();
            var token = _loadCts.Token;
            prevLoadCts?.Cancel();
            prevLoadCts?.Dispose();

            if (string.IsNullOrEmpty(_listWhere))
            {
                List<Account>? previousScope = null;
                await RunOnUiAsync(() =>
                {
                    _useWindowedCache = true;
                    previousScope = _scopeAccounts;
                    _scopeAccounts = new List<Account>();
                    _accounts = new List<Account>();
                    _fullView = _accounts;
                    _scopeCount = 0;
                    _cache?.Reset("1=0", new Dictionary<string, object>(), "Uid", false, 0);
                    QueueBindViewWindow();
                }).ConfigureAwait(false);
                ReleaseScopeAccounts(previousScope);
                return;
            }

            // Lọc theo tên VN/EN không biểu diễn được bằng SQL → dùng path cũ (in-memory).
            if (_scopedIdList != null)
            {
                await ReloadFromDbLegacyAsync(token).ConfigureAwait(false);
                return;
            }

            // ── Windowed cache: KHÔNG nạp cả 30k, chỉ COUNT + stats rồi phân trang DB ──
            try
            {
                string searchTerm = _searchTerm;
                string scopeWhere = ScopeWhere;
                var scopeParams = ScopeParams();
                string effectiveWhere = scopeWhere;
                var effectiveParams = new Dictionary<string, object>(scopeParams);
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    effectiveWhere += SearchClause();
                    effectiveParams["@__search"] = $"%{searchTerm}%";
                }
                string orderBySql = GetCurrentOrderBySql();
                bool orderDesc = _sortOrder == SortOrder.Descending;

                var sw = AccountGridPerf.Start();
                var gcBeforeLoad = AccountGridPerf.GcCounts();
                var data = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    long q0 = System.Diagnostics.Stopwatch.GetTimestamp();
                    int count = _accountContext.GetListCount(effectiveWhere, effectiveParams);
                    long qCount = System.Diagnostics.Stopwatch.GetTimestamp();
                    var stats = _accountContext.GetListStats(scopeWhere, scopeParams);
                    long qStats = System.Diagnostics.Stopwatch.GetTimestamp();

                    CheckedRestoreState checkedRestore;
                    int checkedIdRows = 0;
                    if (stats.Checked == 0)
                        checkedRestore = new CheckedRestoreState();
                    else if (stats.Checked == count && string.IsNullOrWhiteSpace(searchTerm))
                        checkedRestore = new CheckedRestoreState { CheckAllActive = true, CheckAllExceptions = new HashSet<Guid>() };
                    else
                    {
                        var ids = _accountContext.GetIdsInScope(
                            $"{effectiveWhere} AND {nameof(Account.Checked)} = 1", effectiveParams);
                        checkedIdRows = ids.Count;
                        checkedRestore = new CheckedRestoreState { CheckedAccountIds = new HashSet<Guid>(ids) };
                    }
                    long qIds = System.Diagnostics.Stopwatch.GetTimestamp();

                    if (AccountGridPerf.DiagnosticsEnabled)
                        AccountGridPerf.LogLine(
                            "[LoadScopeBreakdown] Accounts=" + count +
                            " Total=" + TsMs(q0, qIds).ToString("0.0") + "ms" +
                            " | SQLite.GetListCount=" + TsMs(q0, qCount).ToString("0.0") +
                            " SQLite.GetListStats=" + TsMs(qCount, qStats).ToString("0.0") +
                            " SQLite.GetIdsInScope+BuildHashSet(checked=" + checkedIdRows + ")=" + TsMs(qStats, qIds).ToString("0.0") +
                            " | (background thread, no account materialization in windowed mode)");
                    return (count, stats, checkedRestore);
                }, token).ConfigureAwait(false);
                AccountGridPerf.Log(sw, "LoadScopeCount", data.count);
                AccountGridPerf.LogGc("LoadScopeCount", gcBeforeLoad);
                AccountGridPerf.LogMemoryStats(data.count, "AfterLoadScopeCount");
                if (token.IsCancellationRequested) return;

                List<Account>? previousScope = null;
                long _uiApply0 = UiThreadProfiler.Begin();
                await RunOnUiAsync(() =>
                {
                    if (token.IsCancellationRequested) return;
                    long _uiWork0 = AccountGridPerf.DiagnosticsEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
                    _useWindowedCache = true;
                    previousScope = _scopeAccounts;
                    _scopeAccounts = new List<Account>();
                    _accounts = new List<Account>();
                    _fullView = _accounts;
                    _scopeCount = data.count;
                    // Windowed cache chỉ giữ ~500 dòng → vẽ nhẹ, KHÔNG cần perf mode
                    // (perf mode ẩn cột Trạng thái + đổi cách vẽ checkbox → tick không hiện).
                    ExitLargeListPerfMode();
                    NormalizeAccountSelectorColumn();   // ép hiện lại cột checkbox + gắn custom paint
                    ApplyWindowedLargeListUiOptimizations();
                    _cache.Reset(effectiveWhere, effectiveParams, orderBySql, orderDesc, data.count);
                    _cachedCheckedCount = data.stats.Checked;
                    toolStripLabel10.Text = data.stats.Live.ToMoneyString();
                    toolStripLabel12.Text = data.stats.Die.ToMoneyString();
                    toolStripLabel14.Text = data.stats.Other.ToMoneyString();
                    ControlHelper.SetToolStripLabelTextSafe(toolStripLabel6, $"{data.stats.Checked}");
                    ApplyCheckedRestore(data.checkedRestore);
                    RefreshCheckedVisual(cachedCountKnown: true, repaintGrid: false);
                    SyncEmptyStateOverlay();
                    QueueBindViewWindow();
                    ResetScrollAndRepaint();
                    ScheduleDeferredMenuWarmupAfterLoad();
                    AccountGridPerf.LogN("LoadScope.UIApply(total=marshalWait+work)", data.count, _uiApply0);
                    AccountGridPerf.LogN("LoadScope.UIWork(onThreadOnly)", data.count, _uiWork0);
                    EmitAccountRetention("AfterGroupSwitch");
                }).ConfigureAwait(false);
                ReleaseScopeAccounts(previousScope);

                if (token.IsCancellationRequested) return;
                int runningCount = await Task.Run(() =>
                        _accountContext.GetListCount(
                            $"{effectiveWhere} AND {nameof(Account.Running)} = 1", effectiveParams),
                    token).ConfigureAwait(false);
                if (token.IsCancellationRequested) return;
                RunOnUi(() =>
                {
                    _cachedRunningCount = runningCount;
                    ControlHelper.SetToolStripLabelTextSafe(toolStripLabel2, $"{runningCount}");
                });
            }
            catch (OperationCanceledException)
            {
                // Superseded by a newer reload/search.
            }
        }

        /// <summary>Path cũ: nạp full scope vào RAM. Chỉ dùng cho lọc tên VN/EN (_scopedIdList).</summary>
        private async Task ReloadFromDbLegacyAsync(CancellationToken token)
        {
            try
            {
                _useWindowedCache = false;
                string searchTerm = _searchTerm;
                bool scopedIdMode = _scopedIdList != null;

                // Phase 1: load scope from DB (background).
                var scopeSnapshot = await LoadScopeAccountsAsync(token).ConfigureAwait(false);
                if (token.IsCancellationRequested) return;

                int scopeCount = scopeSnapshot.Count;
                var scopeWhere = ScopeWhere;
                var scopeParams = ScopeParams();
                bool largeScope = scopeCount > LargeListThreshold;

                if (largeScope)
                {
                    // Danh sách lớn: bind grid trước, stats/checkbox restore sau — tránh chặn UI chờ COUNT/SQL.
                    var view = await Task.Run(
                        () => BuildViewList(scopeSnapshot, searchTerm), token).ConfigureAwait(false);

                    List<Account>? previousScope = null;
                    await RunOnUiAsync(() =>
                    {
                        if (token.IsCancellationRequested) return;
                        previousScope = _scopeAccounts;
                        _scopeAccounts = scopeSnapshot;
                        _scopeCount = scopeCount;
                        EnterLargeListPerfMode();
                        _accounts = view;
                        _fullView = view;

                        if (scopedIdMode)
                        {
                            toolStripLabel10.Text = "-";
                            toolStripLabel12.Text = "-";
                            toolStripLabel14.Text = "-";
                        }
                        else
                        {
                            toolStripLabel10.Text = "…";
                            toolStripLabel12.Text = "…";
                            toolStripLabel14.Text = "…";
                        }

                        ControlHelper.SetToolStripLabelTextSafe(toolStripLabel6, "…");
                        SyncEmptyStateOverlay();
                        QueueBindViewWindow();
                    }).ConfigureAwait(false);
                    ReleaseScopeAccounts(previousScope);

                    if (token.IsCancellationRequested) return;

                    var swDeferred = AccountGridPerf.Start();
                    var deferred = await Task.Run(() =>
                    {
                        token.ThrowIfCancellationRequested();
                        var stats = string.IsNullOrWhiteSpace(searchTerm)
                            ? _accountContext.GetListStats(scopeWhere, scopeParams)
                            : ComputeStatsFromAccounts(scopeSnapshot);
                        var checkedRestore = ComputeCheckedRestoreForScope(
                            scopeSnapshot, scopeWhere, scopeParams, searchTerm, stats.Checked, scopeCount);
                        return (stats, checkedRestore);
                    }, token).ConfigureAwait(false);
                    AccountGridPerf.Log(swDeferred, "DeferredStats", scopeCount);

                    await RunOnUiAsync(() =>
                    {
                        if (token.IsCancellationRequested) return;
                        var swUi = AccountGridPerf.Start();
                        _cachedCheckedCount = deferred.stats.Checked;
                        if (!scopedIdMode)
                        {
                            toolStripLabel10.Text = deferred.stats.Live.ToMoneyString();
                            toolStripLabel12.Text = deferred.stats.Die.ToMoneyString();
                            toolStripLabel14.Text = deferred.stats.Other.ToMoneyString();
                        }
                        ControlHelper.SetToolStripLabelTextSafe(toolStripLabel6, $"{deferred.stats.Checked}");
                        ApplyCheckedRestore(deferred.checkedRestore);
                        RefreshCheckedVisual(cachedCountKnown: true, repaintGrid: false);
                        AccountGridPerf.Log(swUi, "ApplyDeferredUi");
                        AccountGridPerf.LogUiState(this, dataGridView1, panel5?.Controls.Find("ssaEmptyState", true).FirstOrDefault(), ViewRowCount, LargeListPerfMode);
                        ScheduleDeferredMenuWarmupAfterLoad();
                    }).ConfigureAwait(false);
                }
                else
                {
                    var bundle = await Task.Run(() =>
                    {
                        token.ThrowIfCancellationRequested();
                        var view = BuildViewList(scopeSnapshot, searchTerm);
                        var stats = ComputeStatsFromAccounts(scopeSnapshot);
                        var checkedRestore = ComputeCheckedRestoreForScope(
                            scopeSnapshot, scopeWhere, scopeParams, searchTerm, stats.Checked, scopeCount);
                        return (view, stats, checkedRestore);
                    }, token).ConfigureAwait(false);

                    List<Account>? previousScope = null;
                    await RunOnUiAsync(() =>
                    {
                        if (token.IsCancellationRequested) return;
                        previousScope = _scopeAccounts;
                        _scopeAccounts = scopeSnapshot;
                        _scopeCount = scopeCount;
                        _cachedCheckedCount = bundle.stats.Checked;
                        ExitLargeListPerfMode();
                        _accounts = bundle.view;
                        _fullView = bundle.view;

                        if (scopedIdMode)
                        {
                            toolStripLabel10.Text = "-";
                            toolStripLabel12.Text = "-";
                            toolStripLabel14.Text = "-";
                        }
                        else
                        {
                            toolStripLabel10.Text = bundle.stats.Live.ToMoneyString();
                            toolStripLabel12.Text = bundle.stats.Die.ToMoneyString();
                            toolStripLabel14.Text = bundle.stats.Other.ToMoneyString();
                        }

                        ControlHelper.SetToolStripLabelTextSafe(toolStripLabel6, $"{bundle.stats.Checked}");
                        ApplyCheckedRestore(bundle.checkedRestore);
                        QueueBindViewWindow();
                        AccountGridPerf.LogUiState(this, dataGridView1, panel5?.Controls.Find("ssaEmptyState", true).FirstOrDefault(), ViewRowCount, LargeListPerfMode);
                    }).ConfigureAwait(false);
                    ReleaseScopeAccounts(previousScope);
                }

                if (token.IsCancellationRequested) return;

                // Phase 3: running count (DB) — sau khi grid đã hiện, không chặn thao tác.
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    int runningCount = await Task.Run(() =>
                            _accountContext.GetListCount(
                                $"{scopeWhere} AND {nameof(Account.Running)} = 1", scopeParams),
                        token).ConfigureAwait(false);
                    if (token.IsCancellationRequested) return;
                    RunOnUi(() =>
                    {
                        _cachedRunningCount = runningCount;
                        ControlHelper.SetToolStripLabelTextSafe(toolStripLabel2, $"{runningCount}");
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // Superseded by a newer reload/search.
            }
        }

        private void SetSearchTerm(string searchValue)
        {
            _searchTerm = string.IsNullOrWhiteSpace(searchValue) ? "" : searchValue.Trim();
        }

        private static string BuildStateFilterSql(string state)
        {
            if (string.IsNullOrEmpty(state) || state.Contains("Tất cả")) return "";
            if (state.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                return $" AND ({nameof(Account.State)} IS NULL OR {nameof(Account.State)} = '')";
            return $" AND UPPER({nameof(Account.State)}) = '{state.ToUpperInvariant()}'";
        }

        private static string? BuildSingleCboFilterSql(string filter)
        {
            return filter switch
            {
                "LIVE" => $" AND UPPER({nameof(Account.State)}) = 'LIVE'",
                "DIE" => $" AND UPPER({nameof(Account.State)}) = 'DIE'",
                "Chưa xác định" => $" AND ({nameof(Account.State)} IS NULL OR {nameof(Account.State)} = '')",
                "Đang chạy" => $" AND {nameof(Account.Running)} = 1",
                "Lỗi" => $" AND {nameof(Account.Status)} LIKE '%Lỗi%'",
                "Đăng xuất" => $" AND {nameof(Account.Status)} LIKE '%Đăng xuất%'",
                "Captcha" => $" AND {nameof(Account.Status)} LIKE '%Captcha%'",
                "Bị chặn" => $" AND {nameof(Account.Status)} LIKE '%chặn%'",
                "Đã dừng" => $" AND {nameof(Account.Status)} LIKE '%Đã dừng%'",
                "Có trạng thái" => $" AND {nameof(Account.Status)} IS NOT NULL AND {nameof(Account.Status)} != ''",
                "Chưa có trạng thái" => $" AND ({nameof(Account.Status)} IS NULL OR {nameof(Account.Status)} = '')",
                "UID đầu 6" => $" AND {nameof(Account.Uid)} LIKE '6%'",
                "UID đầu 1" => $" AND {nameof(Account.Uid)} LIKE '1%'",
                "Tương tác hôm nay" => $" AND {nameof(Account.RecentInteraction)} LIKE '%{DateTime.Today:dd/MM/yyyy}%'",
                "Tương tác hôm qua" => $" AND {nameof(Account.RecentInteraction)} LIKE '%{DateTime.Today.AddDays(-1):dd/MM/yyyy}%'",
                "Chưa tương tác" => $" AND ({nameof(Account.RecentInteraction)} IS NULL OR {nameof(Account.RecentInteraction)} = '')",
                _ when filter.StartsWith("Nhóm: ", StringComparison.Ordinal) && filter.Length >= 6 =>
                    $" AND {nameof(Account.NameFolder)} = '{filter.Substring(6).Replace("'", "''")}'",
                // Tên tiếng Việt = có ít nhất 1 ký tự có dấu; tiếng Anh = có tên & không ký tự có dấu.
                "Tên tiếng Việt" => $" AND {VietnameseNameGlob}",
                "Tên tiếng Anh" => $" AND {nameof(Account.FullName)} IS NOT NULL AND {nameof(Account.FullName)} != '' AND NOT ({VietnameseNameGlob})",
                _ => null
            };
        }

        // Bộ ký tự tiếng Việt có dấu (chữ thường) — phải khớp IsVietnameseName.
        private const string VietnameseLowerChars =
            "àáảãạăắằẳẵặâấầẩẫậèéẻẽẹêếềểễệìíỉĩịòóỏõọôốồổỗộơớờởỡợùúủũụưứừửữựỳýỷỹỵđ";
        private const string VietnameseUpperChars =
            "ÀÁẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬÈÉẺẼẸÊẾỀỂỄỆÌÍỈĨỊÒÓỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÙÚỦŨỤƯỨỪỬỮỰỲÝỶỸỴĐ";
        // GLOB bracket-class khớp 1 ký tự có dấu bất kỳ (SQLite GLOB so khớp theo Unicode code point,
        // phân biệt hoa/thường → phải liệt kê cả hai). Không ký tự nào là ] - ^ nên an toàn trong [].
        private static readonly string VietnameseNameGlob =
            $"{nameof(Account.FullName)} GLOB '*[{VietnameseLowerChars}{VietnameseUpperChars}]*'";

        // Cả hai filter tên giờ biểu diễn được bằng SQL (GLOB) → KHÔNG còn path nào nạp toàn bộ
        // bảng vào RAM. Giữ chữ ký để các caller cũ vẫn biên dịch; luôn trả false.
        private static bool NeedsScopedIdList(IReadOnlyList<string> filters) => false;

        private static string? BuildCboFilterSql(IReadOnlyList<string> filters)
        {
            if (filters == null || filters.Count == 0) return "";
            var parts = new List<string>();
            for (int i = 0; i < filters.Count; i++)
            {
                var clause = BuildSingleCboFilterSql(filters[i]);
                if (clause == null) return null;
                parts.Add(clause.TrimStart());
            }
            return parts.Count == 0 ? "" : " AND " + string.Join(" AND ", parts);
        }

        /// <summary>Uid theo phạm vi hiện tại + điều kiện state (DB-scoped, đúng cả khi phân trang).</summary>
        private List<string> GetScopedUidsByState(string extraAnd)
            => _accountContext.GetUidsInScope(EffectiveWhere + extraAnd, EffectiveParams());

        private async Task ApplyCheckedBySqlAsync(string extraAnd)
        {            var ids = await Task.Run(() =>
                _accountContext.GetIdsInScope(EffectiveWhere + extraAnd, EffectiveParams()));
            ClearCheckedSelectionState();
            _checkedAccountIds = new HashSet<Guid>(ids);
            _cachedCheckedCount = ids.Count;
            RefreshCheckedVisual(cachedCountKnown: true, repaintGrid: false);
        }

        // Grid hỏi giá trị cell khi cần vẽ → trả về từ _view[rowIndex].
        private void DataGridView1_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            long cvnStart = AccountGridPerf.DiagnosticsEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            try
            {
                var readers = _cellValueReaders;
                if (readers != null && e.ColumnIndex >= 0 && e.ColumnIndex < readers.Length)
                {
                    var reader = readers[e.ColumnIndex];
                    if (reader != null)
                    {
                        var acc = AccountAtRow(e.RowIndex);
                        if (acc == null)
                        {
                            if (IsAccountSelectorColumn(e.ColumnIndex)) e.Value = false;
                            else if (dataGridView1.Columns[e.ColumnIndex] == dataGridViewTextBoxColumn1) e.Value = 0;
                            else e.Value = string.Empty;
                            return;
                        }

                        e.Value = reader(acc, e.RowIndex);
                        return;
                    }
                }

                var col = dataGridView1.Columns[e.ColumnIndex];
                var fallbackAcc = AccountAtRow(e.RowIndex);

                if (fallbackAcc == null)
                {
                    if (IsAccountSelectorColumn(e.ColumnIndex)) e.Value = false;
                    else if (col == dataGridViewTextBoxColumn1) e.Value = 0;
                    else e.Value = string.Empty;
                    return;
                }

                if (IsAccountSelectorColumn(e.ColumnIndex)) { e.Value = IsAccountChecked(fallbackAcc); return; }
                if (col == dataGridViewTextBoxColumn1) { e.Value = e.RowIndex + 1; return; }

                e.Value = FormatCellValue(GetAccountValue(fallbackAcc, col.DataPropertyName));
            }
            finally
            {
                if (AccountGridPerf.DiagnosticsEnabled)
                {
                    AccountGridPerf.RecordCellValueNeeded(System.Diagnostics.Stopwatch.GetTimestamp() - cvnStart);
                    UiThreadProfiler.End("CellValueNeeded", cvnStart);
                }
            }
        }

        private object FormatCellValue(object? value)
        {
            if (value == null) return string.Empty;
            return value switch
            {
                string s => s.Length <= 96 ? s : TruncatePerfCellText(s),
                bool b => AccountGridPerf.FormatBool(b),
                Guid g => FormatGuidCached(g),
                int _ => value,
                _ => value
            };
        }

        // ── Guid.ToString() cache: avoid allocation per paint cycle ──
        // Most grids show the same ~50 visible rows repeatedly; cache their Id strings.
        private string? _lastGuidStr;
        private Guid _lastGuid;

        private string FormatGuidCached(Guid g)
        {
            // Fast path: same Guid as previous call (cells in same row).
            if (g == _lastGuid && _lastGuidStr != null) return _lastGuidStr;
            _lastGuid = g;
            _lastGuidStr = g.ToString();
            return _lastGuidStr;
        }

        private string TruncatePerfCellText(string text)
        {
            if (!LargeListPerfMode || text.Length <= 96) return text;
            // Use Span-based approach to avoid intermediate allocations.
            return string.Concat(text.AsSpan(0, 96), "…");
        }

        private void DataGridView1_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
        }

        // Sau khi tick/bỏ tick hàng loạt: VirtualMode không tự vẽ lại checkbox
        // → Invalidate để CellValueNeeded đọc lại Checked, đồng thời cập nhật label đếm ngay.
        private void RefreshCheckedVisual(bool cachedCountKnown = false, bool repaintGrid = true)
        {
            if (!cachedCountKnown)
            {
                if (_checkAllActive)
                    _cachedCheckedCount = SelectionScopeCount - (_checkAllExceptions?.Count ?? 0);
                else if (_checkedAccountIds != null)
                    _cachedCheckedCount = _checkedAccountIds.Count;
                else
                    _cachedCheckedCount = _checkedAccounts.Count;
            }
            ControlHelper.SetToolStripLabelTextSafe(toolStripLabel6, $"{_cachedCheckedCount}");
            if (!repaintGrid) return;
            InvalidateCheckboxColumnIfNeeded();
        }

        private void InvalidateCheckboxColumnIfNeeded()
        {
            if (!dataGridView1.IsHandleCreated) return;
            // Windowed cache / list nhỏ: chỉ repaint vùng hiển thị (rẻ, không loop 30k).
            if (_useWindowedCache || ViewRowCount <= LargeListThreshold)
                dataGridView1.Invalidate(dataGridView1.DisplayRectangle);
            else if (_colIndexChecked >= 0)
                dataGridView1.InvalidateColumn(_colIndexChecked);
            else
                RefreshLargeGridVisibleArea();
        }

        // VirtualMode không tự sort được → sort thủ công trên _view rồi refresh.
        private async void DataGridView1_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            var col = dataGridView1.Columns[e.ColumnIndex];
            if (col == dataGridViewCheckBoxColumn1) return;
            string prop = col == dataGridViewTextBoxColumn1 ? nameof(Account.STT) : col.DataPropertyName;
            if (string.IsNullOrEmpty(prop)) return;

            // Windowed cache: chỉ sort được cột có trong DB (ListSelectColumns).
            if (_useWindowedCache && !_sortableProps.Contains(prop)) return;
            if (_unsortableProps.Contains(prop)) return;

            if (_sortColumnIndex == e.ColumnIndex)
                _sortOrder = _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            else
            {
                _sortColumnIndex = e.ColumnIndex;
                _sortOrder = SortOrder.Ascending;
            }

            // Windowed cache: sort ở tầng DB (ORDER BY), giữ TotalCount, chỉ tải lại trang.
            if (_useWindowedCache)
            {
                RunOnUi(() =>
                {
                    long _h = UiThreadProfiler.Begin();
                    _cache.Resort(prop, _sortOrder == SortOrder.Descending);
                    QueueBindViewWindow();
                    ResetScrollAndRepaint();
                    AccountGridPerf.LogHandler("SortColumn(windowed)", _h);
                });
                return;
            }

            int dir = _sortOrder == SortOrder.Ascending ? 1 : -1;
            var accounts = _accounts;
            if (accounts == null || accounts.Count == 0) return;

            var sw = AccountGridPerf.Start();
            var sorted = await Task.Run(() =>
            {
                var copy = accounts.ToList();
                copy.Sort((x, y) => dir * Comparer<object>.Default.Compare(
                    GetAccountValue(x, prop), GetAccountValue(y, prop)));
                return copy;
            }).ConfigureAwait(false);
            AccountGridPerf.Log(sw, "Sort", accounts.Count);

            RunOnUi(() =>
            {
                _accounts = sorted;
                _fullView = sorted;
                QueueBindViewWindow();
            });
        }

        // Đọc giá trị property theo tên cột (DataPropertyName). Tránh reflection trong hot path.
        private static object GetAccountValue(Account a, string prop) => prop switch
        {
            nameof(Account.Uid) => a.Uid,
            nameof(Account.FullName) => a.FullName,
            nameof(Account.Password) => a.Password,
            nameof(Account.TowFA) => a.TowFA,
            nameof(Account.Cookie) => a.Cookie,
            nameof(Account.Token) => a.Token,
            nameof(Account.Gender) => a.Gender,
            nameof(Account.Bio) => a.Bio,
            nameof(Account.Friends) => a.Friends,
            nameof(Account.PagePro5) => a.PagePro5,
            nameof(Account.Groups) => a.Groups,
            nameof(Account.Follow) => a.Follow,
            nameof(Account.Birthday) => a.Birthday,
            nameof(Account.DateCreate) => a.DateCreate,
            nameof(Account.Avatar) => a.Avatar,
            nameof(Account.Phone) => a.Phone,
            nameof(Account.Email) => a.Email,
            nameof(Account.PassMail) => a.PassMail,
            nameof(Account.MailClientId) => a.MailClientId,
            nameof(Account.MailRefreshToken) => a.MailRefreshToken,
            nameof(Account.EmailAddress) => a.EmailAddress,
            nameof(Account.UserAgent) => a.UserAgent,
            nameof(Account.PassPrivateEmailAddress) => a.PassPrivateEmailAddress,
            nameof(Account.DeviceInfo) => a.DeviceInfo,
            nameof(Account.NameFolder) => a.NameFolder,
            nameof(Account.NameScript) => a.NameScript,
            nameof(Account.Proxy) => a.Proxy,
            nameof(Account.Note) => a.Note,
            nameof(Account.JobToday) => a.JobToday,
            nameof(Account.JobTotal) => a.JobTotal,
            nameof(Account.Summary) => a.Summary,
            nameof(Account.Summary_Skip) => a.Summary_Skip,
            nameof(Account.RecentInteraction) => a.RecentInteraction,
            nameof(Account.Serial) => a.Serial,
            nameof(Account.TokenJob) => a.TokenJob,
            nameof(Account.XuToday) => a.XuToday,
            nameof(Account.State) => a.State,
            nameof(Account.Status) => a.Status,
            nameof(Account.STT) => a.STT,
            nameof(Account.Id) => a.Id,
            nameof(Account.ColorType) => a.ColorType,
            nameof(Account.Running) => a.Running,
            _ => ""
        };

        /// <summary>Warn khi user chưa tick checkbox nào — trả false để caller return sớm.</summary>
        private bool RequireChecked()
        {
            if (HasAnyChecked()) return true;
            AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox tài khoản cần thao tác.");
            return false;
        }

        private static readonly Color _defaultForeColor = ColorTranslator.FromHtml("#1A1A1A");
        private static readonly Color _selectionBackColor = Color.FromArgb(0, 120, 215);
        private static readonly Color _logoutForeColor = Color.FromArgb(139, 92, 246); // violet-500

        private static bool IsLogoutStatus(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            string u = s.ToUpperInvariant();
            return u.Contains("LOGOUT") || u.Contains("ĐĂNG XUẤT");
        }

        private void uiDataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            AccountGridPerf.RecordCellFormatting();
            if (AccountGridPerf.DisableCustomRendering) return;
            long _cfStart = UiThreadProfiler.Begin();
            try
            {
            if (LargeListPerfMode || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_useWindowedCache && IsLargeVirtualList) return;

            var col = dataGridView1.Columns[e.ColumnIndex];
            if (col == null) return;

            // Checkbox custom-painted: giữ bool — KHÔNG gán string (gây FormatException).
            if (IsAccountSelectorColumn(e.ColumnIndex))
                return;

            var acc = AccountAtRow(e.RowIndex);
            if (acc == null) return;

            // >2000 dòng: bỏ tô màu từng cell khi scroll (gây grid trắng/đơ).
            if (dataGridView1.RowCount > LargeListThreshold)
                return;

            e.CellStyle.BackColor = acc.Running ? Color.Khaki : Color.White;
            bool isLogout = IsLogoutStatus(acc.State) || IsLogoutStatus(acc.Status);
            e.CellStyle.ForeColor = isLogout
                ? _logoutForeColor
                : acc.ColorType switch
                {
                    1 => Color.Red,
                    2 => Color.Green,
                    _ => _defaultForeColor
                };
            e.CellStyle.SelectionBackColor = _selectionBackColor;
            e.CellStyle.SelectionForeColor = Color.White;

            if (col == dataGridViewTextBoxColumn1 || col.DataPropertyName == nameof(Account.STT))
            {
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            else if (col.DataPropertyName == nameof(Account.Password) || col.DataPropertyName == nameof(Account.TowFA))
            {
                if (e.Value != null && !string.IsNullOrEmpty(e.Value.ToString()))
                {
                    e.Value = "***";
                    e.FormattingApplied = true;
                }
            }
            }
            finally { UiThreadProfiler.End("CellFormatting", _cfStart); }
        }

        private async void ucdgvAccount_Load(object sender, EventArgs e)
        {
            await LoadJobService();
            await LoadFolders(reloadAccounts: false);
            select4_SelectedIndexChanged(null, null);
            // Hoãn load account 1 frame — menu/form nhận input trước khi bind grid nặng.
            BeginInvoke(new Action(() => _ = LoadAccounts()));
        }

        private async Task LoadFolders(bool reloadAccounts = true)
        {
            // Ghi nhớ nhóm đang chọn trước khi reload
            string previousSelected = select1.Text;

            var folders = await Task.Run(() => _folderContext.GetByType(_platform)) ?? new List<Sunny.Subdy.Data.Models.Folder>();
            _cachedFolders = folders;
            _folderNames = folders.Select(f => f.Name ?? "").Where(n => n != "").ToList();
            select1.Items.Clear();
            select1.Items.Add("[ Tất cả các nhóm ]");
            if (folders.Any())
            {
                select1.Items.Add("[ Chọn theo uid ]");
                foreach (var folder in folders)
                    select1.Items.Add(folder.Name);
                select1.Items.Add("[ Chọn nhiều nhóm ]");
                select1.Items.Add("[ Tài khoản đã xóa ]");
            }

            // Cập nhật combobox lọc: thêm nhóm tài khoản
            UpdateFilterFolders(folders.Select(f => f.Name).ToList());

            // Khôi phục nhóm đã chọn trước đó (hoặc mặc định index 0)
            int restoredIndex = -1;
            if (!string.IsNullOrEmpty(previousSelected))
            {
                for (int i = 0; i < select1.Items.Count; i++)
                {
                    if (select1.Items[i]?.ToString() == previousSelected)
                    {
                        restoredIndex = i;
                        break;
                    }
                }
            }
            _suppressFolderSelect = true;
            try
            {
                select1.SelectedIndex = restoredIndex >= 0 ? restoredIndex : 0;
            }
            finally
            {
                _suppressFolderSelect = false;
            }

            if (reloadAccounts)
                await LoadAccounts();
            else
                EnsureMenuStripWarmupScheduled();
        }

        private void EnsureMenuStripWarmupScheduled()
        {
            if (!_menuStripDirty && menulist != null) return;
            if (ViewRowCount > LargeListThreshold || _scopeCount > LargeListThreshold)
            {
                ScheduleDeferredMenuWarmupAfterLoad();
                return;
            }
            ScheduleMenuWarmup();
        }

        private void ScheduleMenuWarmup()
        {
            _menuStripDirty = true;
            _ = Task.Run(async () =>
            {
                await Task.Delay(2500);
                if (IsDisposed || !_menuStripDirty) return;
                try
                {
                    await WarmupMenuStripAsync().ConfigureAwait(false);
                }
                catch { }
            });
        }

        private void ScheduleDeferredMenuWarmupAfterLoad()
        {
            if (!_menuStripDirty && menulist != null) return;
            _menuStripDirty = true;
            _ = Task.Run(async () =>
            {
                await Task.Delay(4000).ConfigureAwait(false);
                if (IsDisposed || !_menuStripDirty) return;
                try
                {
                    await WarmupMenuStripAsync().ConfigureAwait(false);
                }
                catch { }
            });
        }

        private async Task LoadJobService()
        {
            var typejobs = await Task.Run(() => JobServices.GetTypeJobByPlatformt(_platform) ?? new List<string>());
            var typejobsLower = typejobs.Select(j => j.ToLowerInvariant()).ToHashSet();

            foreach (ToolStripItem item in toolStripDropDownButton1.DropDownItems)
            {
                if (item.Name.Contains("total", StringComparison.OrdinalIgnoreCase))
                    continue;

                var jobName = item.Name.Split('_')[0].ToLowerInvariant();
                item.Visible = typejobsLower.Contains(jobName);
            }
        }

        private void select1_SelectedIndexChanged(object sender, AntdUI.IntEventArgs e)
        {
            if (_suppressFolderSelect) return;
            if (select1.SelectedIndex == -1) return;
            long _h = UiThreadProfiler.Begin();
            try
            {
            switch (select1.Text)
            {
                case "[ Chọn theo uid ]":
                    button1.Enabled = button2.Enabled = false;
                    var fUid = new Facebook_Farm_NewFeed_PostStory.Views.Forms.fSelectByUid(_platform);
                    fUid.ShowDialog();
                    if (fUid.Saved)
                        _ = LoadAccounts();
                    break;
                case "[ Chọn nhiều nhóm ]":
                    button1.Enabled = button2.Enabled = false;
                    var fMulti = new Facebook_Farm_NewFeed_PostStory.Views.Forms.fSelectMultiFolder(_platform, _folderNames);
                    fMulti.ShowDialog();
                    if (fMulti.Saved)
                        _ = LoadAccounts();
                    break;
                case "[ Tài khoản đã xóa ]":
                case "[ Tất cả các nhóm ]":
                    button1.Enabled = button2.Enabled = false;
                    _ = LoadAccounts();
                    break;
                default:
                    button1.Enabled = button2.Enabled = true;
                    _ = LoadAccounts();
                    break;
            }
            }
            finally { AccountGridPerf.LogHandler("GroupChanged(select1)", _h); }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            await LoadFolders();
        }

        private async void button3_Click_1(object sender, EventArgs e)
        {
            if (!LoginGuard.EnsureLoggedIn(_form)) return;
            fFolder f = new fFolder("AddFolder", _platform);
            f.ShowDialog();
            await LoadFolders();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            fFolder f = new fFolder("EditFolder", select1.Text.Trim(), _platform);
            f.ShowDialog();
            await LoadFolders();
        }

        private async void button1_Click_1(object sender, EventArgs e)
        {
            if (AntdHelper.Confirm(_form, "Cảnh báo", $"Bạn có chắc chắn muốn xóa nhóm tài khoản [{select1.Text}] ?"))
            {
                var folder = _folderContext.GetByName(select1.Text, _platform);
                await Task.Run(() => _folderContext.DeleteById(folder.Id));
                await LoadFolders();
            }
        }

        private async Task LoadAccounts()
        {
            if (AccountGridPerf.DiagnosticsEnabled)
            {
                var st = new System.Diagnostics.StackTrace(false);
                var frames = string.Join(" <- ", Enumerable.Range(1, Math.Min(8, st.FrameCount - 1))
                    .Select(i =>
                    {
                        var f = st.GetFrame(i);
                        var m = f?.GetMethod();
                        var t = m?.DeclaringType?.Name ?? "?";
                        var n = m?.Name ?? "?";
                        return $"{t}.{n}";
                    }));
                AccountGridPerf.LogLine("[LoadAccounts.CALLER] " + frames);
            }
            try
            {
                bool shouldReload = false;
                await RunOnUiAsync(() => shouldReload = PrepareLoadAccounts()).ConfigureAwait(false);
                if (!shouldReload) return;

                await ReloadFromDbAsync().ConfigureAwait(false);

                await RunOnUiAsync(() =>
                {
                    ControlHelper.SetToolStripLabelTextSafe(toolStripLabel16, "0");
                    EnsureMenuStripWarmupScheduled();
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await RunOnUiAsync(() =>
                    MessageBox.Show($"Lỗi khi load accounts: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)).ConfigureAwait(false);
            }
            finally
            {
                RunOnUi(UpdatePageLabel);
            }
        }

        /// <returns>false when reload can be skipped (empty uid selection).</returns>
        private bool PrepareLoadAccounts()
        {
            ControlHelper.SetToolStripLabelTextSafe(toolStripLabel8, "Đang tải…");

            string selectedFolder = select1.Text.Trim();
            (_listWhere, _listParams) = BuildListQuery(selectedFolder);

            if (selectedFolder == "[ Chọn theo uid ]" && !_listWhere.Contains($"{nameof(Account.Uid)} IN"))
            {
                _useWindowedCache = false;
                _cache?.Reset("1=0", new Dictionary<string, object>(), "Uid", false, 0);
                _accounts = new List<Account>();
                _fullView = _accounts;
                _scopeAccounts = _accounts;
                _scopeCount = 0;
                ClearCheckedSelectionState();
                BindViewWindow();
                toolStripLabel8.Text = "0";
                toolStripLabel10.Text = "0";
                toolStripLabel12.Text = "0";
                toolStripLabel14.Text = "0";
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel6, "0");
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel2, "0");
                return false;
            }

            ClearCheckedSelectionState();
            _sortColumnIndex = -1;
            _sortOrder = SortOrder.None;
            _stateFilterExtra = "";
            _cboFilterExtra = "";
            _searchTerm = input6.Text.Trim();
            _scopedIdList = null;
            return true;
        }

        private void button16_Click(object sender, EventArgs e)
        {
            if (!LoginGuard.EnsureLoggedIn(_form)) return;
            fAddAccount fAdd = new fAddAccount(_platform, true);
            fAdd.ShowDialog();
            _ = LoadAccounts();
        }

        private void button17_Click(object sender, EventArgs e)
        {
            // Exclude list dựa trên column Name ("col_<Property>") — bền với việc HeaderText bị
            // transform (uppercase / i18n). CheckBox + STT column không có pattern col_ nên check riêng.
            var excludedNames = new HashSet<string>
            {
                "col_" + nameof(Account.Uid),
                "col_" + nameof(Account.Status),
                "col_" + nameof(Account.Id),
                "col_" + nameof(Account.ColorType),
                "col_" + nameof(Account.Running)
            };

            var remainingHeaders = dataGridView1.Columns
                .Cast<DataGridViewColumn>()
                .Where(c => c != dataGridViewCheckBoxColumn1 && c != dataGridViewTextBoxColumn1)
                .Where(c => !excludedNames.Contains(c.Name))
                .Select(c => c.HeaderText)
                .ToList();
            fViewDataGridView f = new fViewDataGridView(remainingHeaders, dataGridView1.Name);
            f.ShowDialog();
            ControlHelper.LoadConfigColums(dataGridView1, new List<string> { nameof(Account.Id), nameof(Account.ColorType), nameof(Account.Running) });
            ForceHideInternalColumns(dataGridView1);
            NormalizeAccountSelectorColumn();
        }

        private CancellationTokenSource _searchCts;
        private bool _suppressSelectionChanged;
        private const int WM_SETREDRAW = 0x000B;
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private static void SetRedraw(Control c, bool enable)
        {
            if (!c.IsHandleCreated) return;
            SendMessage(c.Handle, WM_SETREDRAW, enable ? (IntPtr)1 : IntPtr.Zero, IntPtr.Zero);
        }

        private async void input6_TextChanged(object sender, EventArgs e)
        {
            var prevSearchCts = _searchCts;
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;
            prevSearchCts?.Cancel();
            prevSearchCts?.Dispose();

            string searchValue = input6.Text.Trim();

            try
            {
                await Task.Delay(300, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested) return;
            SetSearchTerm(searchValue);

            // Windowed cache: search ở tầng DB (clause LIKE), không lọc in-memory.
            if (_useWindowedCache)
            {
                string scopeWhere = ScopeWhere;
                var effectiveParams = ScopeParams();
                string effectiveWhere = scopeWhere;
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    effectiveWhere += SearchClause();
                    effectiveParams["@__search"] = $"%{searchValue}%";
                }
                string orderBySql = GetCurrentOrderBySql();
                bool orderDesc = _sortOrder == SortOrder.Descending;

                var sw = AccountGridPerf.Start();
                int totalCountDb = 0, runningCountDb = 0;
                try
                {
                    (totalCountDb, runningCountDb) = await Task.Run(() =>
                    {
                        token.ThrowIfCancellationRequested();
                        int total = _accountContext.GetListCount(effectiveWhere, effectiveParams);
                        int running = _accountContext.GetListCount(
                            $"{effectiveWhere} AND {nameof(Account.Running)} = 1", effectiveParams);
                        return (total, running);
                    }, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { return; }
                if (token.IsCancellationRequested) return;

                RunOnUi(() =>
                {
                    if (token.IsCancellationRequested) return;
                    _cache.Reset(effectiveWhere, effectiveParams, orderBySql, orderDesc, totalCountDb);
                    _scopeCount = _cache.TotalCount;
                    _cachedRunningCount = runningCountDb;
                    ControlHelper.SetToolStripLabelTextSafe(toolStripLabel2, $"{runningCountDb}");
                    QueueBindViewWindow();
                    ResetScrollAndRepaint();
                });
                AccountGridPerf.Log(sw, "SearchDb", _cache.TotalCount);
                return;
            }

            var scopeSnapshot = _scopeAccounts;
            List<Account> view;
            var swSearch = AccountGridPerf.Start();
            try
            {
                view = await Task.Run(() => BuildViewList(scopeSnapshot, searchValue), token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            AccountGridPerf.Log(swSearch, "Search", scopeSnapshot?.Count ?? 0);

            if (token.IsCancellationRequested) return;
            int runningCount = view.Count <= LargeListThreshold
                ? CountRunning(view)
                : _cachedRunningCount;
            RunOnUi(() =>
            {
                if (token.IsCancellationRequested) return;
                _accounts = view;
                _fullView = view;
                QueueBindViewWindow();
                _cachedRunningCount = runningCount;
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel2, $"{runningCount}");
            });
        }

        private void ApplySelection(IReadOnlyList<int> indexes)
        {
            _suppressSelectionChanged = true;
            int count = 0;
            try
            {
                SetRedraw(dataGridView1, false);
                dataGridView1.SuspendLayout();
                dataGridView1.ClearSelection();
                const int MAX_SELECT = 200;

                if (UseVirtualSafeSelection)
                {
                    // VirtualMode: không gán Selected từng row — materialize hàng loạt row gây đơ.
                    foreach (var idx in indexes)
                    {
                        if (idx < 0 || idx >= dataGridView1.RowCount) continue;
                        if (count == 0)
                        {
                            int col = Math.Max(0, dataGridView1.CurrentCell?.ColumnIndex ?? 0);
                            if (col >= dataGridView1.ColumnCount) col = 0;
                            dataGridView1.CurrentCell = dataGridView1[col, idx];
                            dataGridView1.FirstDisplayedScrollingRowIndex = Math.Max(0, idx - 3);
                        }
                        if (++count >= MAX_SELECT) break;
                    }
                }
                else
                {
                    foreach (var idx in indexes)
                    {
                        if (idx < 0 || idx >= dataGridView1.RowCount) continue;
                        if (count == 0)
                        {
                            int col = Math.Max(0, dataGridView1.CurrentCell?.ColumnIndex ?? 0);
                            if (col >= dataGridView1.ColumnCount) col = 0;
                            dataGridView1.CurrentCell = dataGridView1[col, idx];
                        }
                        if (idx >= 0 && idx < dataGridView1.RowCount)
                        {
                            dataGridView1.Rows[idx].Selected = true;
                            if (++count >= MAX_SELECT) break;
                        }
                    }
                }
            }
            finally
            {
                ControlHelper.SetToolStripLabelTextSafe(toolStripLabel4,
                    UseVirtualSafeSelection ? count.ToMoneyString() : dataGridView1.SelectedRows.Count.ToMoneyString());
                dataGridView1.ResumeLayout(performLayout: false);
                SetRedraw(dataGridView1, true);
                if (dataGridView1.IsHandleCreated)
                {
                    if (ViewRowCount > LargeListThreshold)
                        RefreshLargeGridVisibleArea();
                    else
                        dataGridView1.Invalidate(dataGridView1.DisplayRectangle);
                }
                _suppressSelectionChanged = false;
            }
        }

        private async void button9_Click(object sender, EventArgs e)
        {
            await LoadAccounts();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Module "Reg Facebook": nút này mở form cài đặt RIÊNG (fSettingRegFacebook) —
            // nơi điền key shopmailmmo.com + toàn bộ cấu hình đăng ký (decision #2).
            // Facebook/Pandora vẫn mở fSettingDefault như cũ.
            if (_platform == PlatformModel.RegFacebook)
            {
                fSettingRegFacebook fReg = new fSettingRegFacebook(_platform);
                fReg.ShowDialog();
                return;
            }
            fSettingDefault fSetting = new fSettingDefault(_platform);
            fSetting.ShowDialog();
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            //if (SubdyHelper.JobServiceByPlatform(_platform).Contains(select4.Text.Trim()))
            //{
            this.Cursor = Cursors.WaitCursor;
            fSettingJob f = new fSettingJob(_platform, "Subdy");
            this.Cursor = Cursors.Default;
            f.ShowDialog();
            //}
            //else
            //{
            //    var script = _scriptContext.GetByName(select4.Text.Trim(), _platform);
            //    if (script != null)
            //    {
            //        this.Cursor = Cursors.WaitCursor;
            //        fChiTietKichBan f = new fChiTietKichBan(script.Id);
            //        this.Cursor = Cursors.Default;
            //        f.ShowDialog();
            //        await LoadJobService();
            //    }
            //    else
            //    {
            //        CommonMethod.ShowMessageWarning("Vui lòng chọn dịch vụ hoặc kịch bản để thiết lập");
            //    }
            //}
        }

        private void Enable(bool enable)
        {
            button7.Visible = enable;
            button8.Visible = !enable;
            button8.Enabled = !enable;
            button8.Text    = "Dừng"; // reset lại sau khi stop completed
            button9.Enabled = enable;
            panel2.Enabled = enable;
            panel3.Enabled = enable;
            button16.Enabled = enable;

            // Disable thêm các control SSA khi đang chạy:
            // cboScript / select1 (Nhóm) / ssaBtnFolderMgr / cboFilterAccount / Cài đặt jobs / chung / Tương tác
            var panel4 = this.Controls.Find("panel4", true).FirstOrDefault() as AntdUI.Panel;
            if (panel4 != null)
            {
                foreach (var name in new[] { "ssaCboScript", "select1", "ssaBtnFolderMgr", "button4", "button5", "button6" })
                {
                    var c = panel4.Controls.Find(name, true).FirstOrDefault();
                    if (c != null) c.Enabled = enable;
                }
            }
            var cboFilter = this.Controls.Find("cboFilterAccount", true).FirstOrDefault();
            if (cboFilter != null) cboFilter.Enabled = enable;
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            // Module "Reg Facebook": luồng ĐĂNG KÝ chạy theo THIẾT BỊ (mỗi thiết bị tạo
            // account mới cho tới khi Dừng), KHÁC luồng farm chạy theo account đã tick.
            // Nhánh riêng để không đụng vào logic farm của Facebook/Pandora bên dưới.
            if (_platform == PlatformModel.RegFacebook)
            {
                await RunRegFacebookAsync();
                return;
            }

            // Validate nhanh trên UI thread — chỉ check trạng thái in-memory, không IO.
            if (!HasAnyChecked())
            {
                AntdHelper.MsgWarn(_form, "Vui lòng chọn ít nhất 1 tài khoản để bắt đầu");
                return;
            }

            // Disable button + show dialog chọn device. Modal dialog vẫn pump message
            // nên các form khác không đơ; chỉ control này không nhận click.
            Enable(false);
            try
            {
                if (!SeleceterDevice()) return;

                // ── Toàn bộ pre-work IO + vòng lặp batch chạy trên thread pool ──
                // GetConfigModel() đọc registry + ReadAllLines(fileGmail) → block I/O nếu trên UI.
                // Đẩy off UI; nếu cần MessageBox lỗi thì marshal về UI thread.
                await Task.Run(async () =>
                {
                    ConfigModel model = null;
                    if (InvokeRequired)
                        Invoke(new Action(() => model = GetConfigModel()));
                    else
                        model = GetConfigModel();

                    if (model == null) return;

                    // Set UI refs phải marshal về UI thread (controls thuộc UI).
                    Invoke(new Action(() =>
                    {
                        FacebookFarming._data.Clear();
                        Globals.ToolStripDropDownButton1 = toolStripDropDownButton1;
                        Globals.JobTotal_toolStripMenuItem = JobTotal_toolStripMenuItem;
                        Globals.ToolStripLabel16 = toolStripLabel16;
                        fMain.StartTime = DateTime.Now;
                    }));

                    Globals.CancellationTokenSource = new CancellationTokenSource();
                    CancellationToken ct = Globals.CancellationTokenSource.Token;
                    AccountServices.Accounts.Clear();
                    var jobAccounts = GetCheckedAccountsForJob();
                    // Chẩn đoán "Accounts rỗng → dừng luồng ngay": log rõ selection state đi nhánh nào
                    // và đếm được bao nhiêu account. Nếu count=0 → do chưa tick / selection bị mất,
                    // KHÔNG phải lỗi thiết bị.
                    System.Diagnostics.Debug.WriteLine($"[Run] Chọn account để chạy: checkAll={_checkAllActive}, "
                        + $"checkedIds={(_checkedAccountIds?.Count ?? 0)}, checkedAccs={_checkedAccounts.Count}, "
                        + $"windowed={_useWindowedCache} → lấy được {jobAccounts.Count} account.");
                    // jobAccounts thứ tự tăng dần (STT 1 → N). GetAccount() lấy FirstOrDefault
                    // (FIFO) → acc đầu (STT nhỏ) chạy trước, acc cuối (STT lớn) chạy sau — đúng thứ tự grid.
                    AccountServices.Accounts = jobAccounts;

                    if (jobAccounts.Count == 0)
                    {
                        RunOnUi(() => AntdHelper.MsgWarn(_form,
                            "Chưa có tài khoản nào được chọn để chạy. Hãy tick checkbox tài khoản trước khi bấm Bắt đầu."));
                        return;
                    }

                    // Đăng ký instance live đang chạy để lưới windowed hiển thị trạng thái realtime.
                    // Cache sẽ thay bản fetch-từ-DB bằng instance live (cùng Id) khi build trang.
                    var liveMap = new System.Collections.Concurrent.ConcurrentDictionary<Guid, Account>();
                    foreach (var a in AccountServices.Accounts)
                        if (a != null) liveMap[a.Id] = a;
                    _liveRunningById = liveMap;
                    if (_useWindowedCache)
                        RunOnUi(() => _cache?.InvalidateAll());
                    // Bật repaint realtime cho cột Trạng thái/Tình trạng trong lúc job chạy.
                    RunOnUi(() => _liveRepaintTimer?.Start());

                    // Clear registry clients cũ trước khi start batch mới
                    while (_activeClients.TryTake(out _)) { }

                    // Tự động đăng nhập acc chưa login (chỉ khi cài đặt checkBox18 được bật).
                    bool autoLoginEnabled = model.SettingGeneral.GetBooleanValue("checkBox18", false);
                    if (autoLoginEnabled && !ct.IsCancellationRequested)
                        await AutoLoginAccountsAsync(AccountServices.Accounts, DeviceServices.DeviceModels.Where(x => x.Checked).ToList(), ct);

                    int indexRunning = 0;
                    while (HasAnyChecked())
                    {
                        if (ct.IsCancellationRequested) break;

                        // Khi chạy lặp lại (repeat mode), AccountServices.Accounts đã bị GetAccount()
                        // rút cạn ở batch trước → cần nạp lại danh sách account đã chọn trước khi
                        // tạo device thread mới, nếu không RunAsync() thấy rỗng sẽ thoát ngay.
                        if (!AccountServices.Accounts.Any())
                        {
                            var reloaded = GetCheckedAccountsForJob();
                            if (reloaded.Count == 0)
                            {
                                System.Diagnostics.Debug.WriteLine("[Run] Hết tài khoản đã chọn để chạy lại — thoát.");
                                break;
                            }
                            AccountServices.Accounts = reloaded;
                            System.Diagnostics.Debug.WriteLine($"[Run] Nạp lại {reloaded.Count} tài khoản cho lần chạy lặp thứ {indexRunning + 1}.");
                        }

                        await XpathManagerFacebook.LoadFromApiAsync();

                        var tasks = new List<Task>();
                        foreach (var device in DeviceServices.DeviceModels.Where(x => x.Checked))
                        {
                            if (ct.IsCancellationRequested) break;
                            tasks.Add(Task.Run(async () => await RunningThread(ct, device, model)));
                        }

                        // Chờ tất cả task — khi cancel, chờ tối đa 10s rồi abandon
                        try
                        {
                            var whenAll = Task.WhenAll(tasks);
                            var tcs = new TaskCompletionSource<bool>();
                            using var reg = ct.Register(() =>
                            {
                                Task.Delay(10000).ContinueWith(_ => tcs.TrySetResult(true));
                            });
                            await Task.WhenAny(whenAll, tcs.Task);
                        }
                        catch { }

                        if (ct.IsCancellationRequested) break;
                        if (model.SettingGeneral.GetBooleanValue("radioButton1", true)) break;
                        if (indexRunning >= model.SettingGeneral.GetIntType("numericUpDown9", 1)) break;
                        indexRunning++;
                        if (!await DelayAndRestartAccounts(SubdyHelper.RandomValue(
                            model.SettingGeneral.GetIntType("numericUpDown8", 30),
                            model.SettingGeneral.GetIntType("numericUpDown7", 30)) * 1000 * 60))
                        {
                            break;
                        }
                    }
                });
            }
            finally
            {
                // Gỡ đăng ký live + refetch để lưới hiển thị trạng thái cuối (đã persist xuống DB).
                _liveRunningById = null;
                RunOnUi(() => _liveRepaintTimer?.Stop());
                if (_useWindowedCache)
                    RunOnUi(() => _cache?.InvalidateAll());
                Enable(true);
                fMain.StartTime = null;
            }
        }

        async Task<bool> DelayAndRestartAccounts(int delayInSeconds)
        {
            bool isCheck = true;
            // Vì button7_Click giờ chạy trong Task.Run, method này được gọi off-UI.
            // Mọi thao tác Controls phải marshal về UI thread.
            SpinnerHelper spinnerDelay = null;
            if (InvokeRequired)
                Invoke(new Action(() =>
                {
                    spinnerDelay = new SpinnerHelper("\t\tVui Lòng Chờ...\r\n" +
                        "Còn {0} giây sẽ chạy lại số tài khoản đã chọn từ đầu.\r\n   Bạn có thể dừng nếu không muốn chạy lại từ đầu!", delayInSeconds);
                    spinnerDelay.Dock = DockStyle.Fill;
                    Controls.Add(spinnerDelay);
                    spinnerDelay.BringToFront();
                }));
            else
            {
                spinnerDelay = new SpinnerHelper("\t\tVui Lòng Chờ...\r\n" +
                    "Còn {0} giây sẽ chạy lại số tài khoản đã chọn từ đầu.\r\n   Bạn có thể dừng nếu không muốn chạy lại từ đầu!", delayInSeconds);
                spinnerDelay.Dock = DockStyle.Fill;
                Controls.Add(spinnerDelay);
                spinnerDelay.BringToFront();
            }

            while (delayInSeconds > 0)
            {
                delayInSeconds--;
                if (Globals.CancellationTokenSource.Token.IsCancellationRequested)
                {
                    isCheck = false;
                    break;
                }
                await Task.Delay(1000);
            }

            if (spinnerDelay != null)
            {
                if (InvokeRequired)
                    Invoke(new Action(() => Controls.Remove(spinnerDelay)));
                else
                    Controls.Remove(spinnerDelay);
            }

            return isCheck;
        }

        private ConfigModel GetConfigModel(bool isreg = false)
        {
            ConfigModel model = new ConfigModel();
            if (!isreg)
            {
                model.SettingGeneral = SettingsTool.GetSettings($"{nameof(fSettingDefault)}_{_platform}", true);
               model.SettingJob = SettingsTool.GetSettings($"{nameof(fSettingJob)}_{_platform}_Subdy", true);
                model.JobService = "Subdy";
            }
            else
            {
                // Module "Reg Facebook" đọc cài đặt từ form RIÊNG (fSettingRegFacebook) —
                // khóa config = "fSettingRegFacebook_Reg Facebook" — để cách ly hoàn toàn
                // với cài đặt đăng ký của Facebook thường. Các platform khác giữ nguyên.
                string regFormName = _platform == PlatformModel.RegFacebook
                    ? nameof(fSettingRegFacebook)
                    : nameof(fSettingRegsiner);
                model.SettingGeneral = SettingsTool.GetSettings($"{regFormName}_{_platform}", true);
                string text = select1.Text.Trim();
                // Windowed cache: _fullView rỗng → lấy account đã tick từ DB (theo Id).
                var regAccounts = _useWindowedCache ? GetCheckedAccountsForJob() : _fullView;
                model.Accounts = new SortableBindingList<Account>(regAccounts);
                if (text == "[ Tất cả các nhóm ]" || text == "[ Chọn theo uid ]" || text == "[ Chọn nhiều nhóm ]" || text == "[ Tài khoản đã xóa ]")
                {
                    model.JobService = "";
                }
                else
                {
                    model.JobService = select1.Text.Trim();
                }
                int index = model.SettingGeneral.GetIntType("comboBox1", 0);
                string type = RegistrationType.RegFacebook_AllTypes[index];
                if (type == RegistrationType.Gmail || type == RegistrationType.Gmail_BaitPhoneNumber)
                {
                    string fileGmail = model.SettingGeneral.GetValuesFromInputString("txtGmail", "");
                    if (!File.Exists(fileGmail))
                    {
                        AntdHelper.MsgError(_form, "File Gmail không tồn tại");
                        return null;
                    }
                    var lines = File.ReadAllLines(fileGmail);
                    if (!lines.Any())
                    {
                        AntdHelper.MsgError(_form, "File Gmail hết gmail");
                        return null;
                    }
                    Globals.Gmails.Clear();
                    Globals.Gmails.AddRange(lines);
                }
                TelegramBotServices.BotTelegram = null;
                if (model.SettingGeneral.GetBooleanValue("checkBox6") && !string.IsNullOrEmpty(model.SettingGeneral.GetValuesFromInputString("textBox7")) && !string.IsNullOrEmpty(model.SettingGeneral.GetValuesFromInputString("textBox8")))
                {
                    TelegramBotServices.BotTelegram = new TelegramBotServices(model.SettingGeneral.GetValuesFromInputString("textBox7"));
                }
            }
            ProxyService.Proxies.Clear();
            ProxyService.Proxies.AddRange(model.SettingGeneral.GetValuesList("txtLines"));
            return model;
        }

        // Registry các ADBClient đang chạy để có thể force-stop từ button Dừng
        private readonly System.Collections.Concurrent.ConcurrentBag<ADBClient> _activeClients = new();

        private async Task RunningThread(CancellationToken ct, DeviceModel device, ConfigModel model)
        {
            ADBClient client = new ADBClient(device);
            _activeClients.Add(client);
            device.Status = "Đang kiểm tra kết nối ADB...";
            device.TypeColor = 2;
            try
            {
                if (!client.Connect())
                {
                    device.Status = "Không thể kết nối ADB, dừng thiết bị.";
                    device.TypeColor = 1;
                    return;
                }

                device.Status = "Đang kiểm tra Facebook và VAT Proxy...";
                MainService service = new MainService(_platform, client, model, ct);
                if (_platform == PlatformModel.Pandora)
                    await service.RunPandoraAsync();
                else
                    await service.RunAsync();
            }
            catch (OperationCanceledException)
            {
                device.Status = "Đã dừng theo yêu cầu.";
                device.TypeColor = 1;
            }
            catch (Exception ex)
            {
                // Không để worker chết im: status phải phản ánh lỗi thay vì kẹt
                // ở dòng trạng thái cũ, khiến người dùng tưởng tool đang chạy.
                device.Status = $"Lỗi worker: {ex.Message}";
                device.TypeColor = 1;
                LogManager.Error(ex);
            }
            finally
            {
                try { client.AppClear(FacebookHander.Package(_platform)); } catch { }
                client.Running = false;
            }
        }

        private bool SeleceterDevice()
        {
            var uc = _form._ucDevices;

            // Save original state
            bool panelRightCollapsed = uc.splitContainer1.Panel2Collapsed;

            // Hide panelRight (thanh công cụ bên phải), chỉ hiện nút Bắt đầu sát lề phải
            uc.splitContainer1.Panel2Collapsed = true;
            uc.button2.Visible = true;
            uc.button2.Enabled = true;
            uc.button2.BringToFront();

            // Bỏ chọn toàn bộ + bật ràng buộc IsRowEnabled chỉ trong phạm vi dialog
            // SelectDevices: row không đủ điều kiện (mất internet hoặc ATX fail)
            // không tick được. Page Quản lý thiết bị đứng độc lập vẫn tick tự do.
            uc.BeginSelectionMode();

            // Refresh DataGridView để load đúng trạng thái checkbox
            uc.dataGridView1.Refresh();

            DialogResult dialogResult;
            try
            {
                using (var f = new fAddUsercontrol("SelectDevices", _platform, uc))
                {
                    dialogResult = f.ShowDialog(_form);
                }
            }
            finally
            {
                // Thoát selection mode dù dialog đóng kiểu gì (OK/Cancel/Exception)
                // → page Quản lý thiết bị sau đó không bị chặn tick.
                uc.EndSelectionMode();
            }

            // Reparent + restore layout trong 1 batch (SuspendLayout) để tránh
            // nhiều layout pass đồng bộ trên DataGridView + splitContainer + ~30 control con
            // → đây là root cause UI đơ vài giây sau khi click "Bắt đầu".
            _form.pContent.SuspendLayout();
            uc.SuspendLayout();
            try
            {
                // Reparent TRƯỚC khi thay đổi property layout — tránh trigger layout trên
                // parent cũ (dialog đang dispose) rồi lại layout lần nữa trên parent mới.
                uc.Dock = DockStyle.Fill;
                if (uc.Parent != _form.pContent)
                {
                    _form.pContent.Controls.Add(uc);
                }
                uc.splitContainer1.Panel2Collapsed = panelRightCollapsed;
                uc.button2.Visible = false;
            }
            finally
            {
                uc.ResumeLayout(false);
                _form.pContent.ResumeLayout(false);
                _form.pContent.PerformLayout();
            }

            // Đưa tab Facebook (this) lên front để user không thấy tab Thiết bị đè lên
            this.BringToFront();

            return dialogResult == DialogResult.OK;
        }

        private static Image MenuIcon(string svg)
        {
            return _menuIconCache.GetOrAdd(svg, s =>
            {
                var bmp = s.SvgToBmp(18, 18, null);
                return bmp ?? new Bitmap(1, 1);
            });
        }

        private void InvalidateMenuStrip()
        {
            ScheduleMenuWarmup();
        }

        private async Task WarmupMenuStripAsync()
        {
            if (!_menuStripDirty || _menuWarmupRunning || IsDisposed) return;
            _menuWarmupRunning = true;
            try
            {
                await Task.Run(() =>
                {
                    _cachedScripts = _scriptContext.GetByPlatform(_platform) ?? new List<Script>();
                    _cachedFolders = _folderContext.GetByType(_platform) ?? new List<Folder>();
                    _cachedDeviceMenuItems = DeviceServices.DeviceModels
                        .Where(x => !string.IsNullOrEmpty(x.Serial))
                        .Select(d => (d.Serial!, d.Model ?? ""))
                        .ToList();
                });

                void build()
                {
                    if (IsDisposed || !_menuStripDirty) return;
                    CreateMenuStrip();
                }

                if (InvokeRequired) BeginInvoke((Action)build);
                else build();
            }
            catch { }
            finally { _menuWarmupRunning = false; }
        }

        private void CreateMenuStrip()
        {
            if (!_menuStripDirty && menulist != null) return;
            var swMenu = AccountGridPerf.Start();
            // SVG icons — mỗi nhóm chức năng có màu riêng
            string svgTick      = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#1890ff\"><path d=\"m424-312 282-282-56-56-226 226-114-114-56 56 170 170ZM200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Zm0-560v560-560Z\"/></svg>";
            string svgSelectAll = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#1890ff\"><path d=\"M268-240 42-466l57-56 170 170 56 56-57 56Zm226 0L268-466l56-57 170 170 368-368 56 57-424 424Zm0-226-57-56 198-198 57 56-198 198Z\"/></svg>";
            string svgHighlight = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#1890ff\"><path d=\"M655-200 513-342l56-56 85 85 170-170 56 57-225 226Zm0-320L513-662l56-56 85 85 170-170 56 57-225 226ZM80-280v-80h360v80H80Zm0-320v-80h360v80H80Z\"/></svg>";
            string svgUncheck   = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#8c8c8c\"><path d=\"M200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Z\"/></svg>";
            string svgCopy      = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#13c2c2\"><path d=\"M760-200H320q-33 0-56.5-23.5T240-280v-560q0-33 23.5-56.5T320-920h280l240 240v400q0 33-23.5 56.5T760-200ZM560-640v-200H320v560h440v-360H560ZM160-40q-33 0-56.5-23.5T80-120v-560h80v560h440v80H160Zm160-800v200-200 560-560Z\"/></svg>";
            string svgUser      = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#1890ff\"><path d=\"M480-480q-66 0-113-47t-47-113q0-66 47-113t113-47q66 0 113 47t47 113q0 66-47 113t-113 47ZM160-160v-112q0-34 17.5-62.5T224-378q62-31 126-46.5T480-440q66 0 130 15.5T736-378q29 15 46.5 43.5T800-272v112H160Zm80-80h480v-32q0-11-5.5-20T700-306q-54-27-109-40.5T480-360q-56 0-111 13.5T260-306q-9 5-14.5 14t-5.5 20v32Zm240-320q33 0 56.5-23.5T560-640q0-33-23.5-56.5T480-720q-33 0-56.5 23.5T400-640q0 33 23.5 56.5T480-560Zm0-80Zm0 400Z\"/></svg>";
            string svgPass      = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#fa8c16\"><path d=\"M80-200v-80h800v80H80Zm46-242-52-30 34-60H40v-60h68l-34-58 52-30 34 58 34-58 52 30-34 58h68v60h-68l34 60-52 30-34-60-34 60Zm320 0-52-30 34-60h-68v-60h68l-34-58 52-30 34 58 34-58 52 30-34 58h68v60h-68l34 60-52 30-34-60-34 60Zm320 0-52-30 34-60h-68v-60h68l-34-58 52-30 34 58 34-58 52 30-34 58h68v60h-68l34 60-52 30-34-60-34 60Z\"/></svg>";
            string svgLock      = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#722ed1\"><path d=\"M240-160h480v-400H240v400Zm240-120q33 0 56.5-23.5T560-360q0-33-23.5-56.5T480-440q-33 0-56.5 23.5T400-360q0 33 23.5 56.5T480-280ZM240-160v-400 400Zm0 80q-33 0-56.5-23.5T160-160v-400q0-33 23.5-56.5T240-640h280v-80q0-83 58.5-141.5T720-920q83 0 141.5 58.5T920-720h-80q0-50-35-85t-85-35q-50 0-85 35t-35 85v80h120q33 0 56.5 23.5T800-560v400q0 33-23.5 56.5T720-80H240Z\"/></svg>";
            string svgMail      = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#52c41a\"><path d=\"M160-160q-33 0-56.5-23.5T80-240v-480q0-33 23.5-56.5T160-800h640q33 0 56.5 23.5T880-720v480q0 33-23.5 56.5T800-160H160Zm320-280L160-640v400h640v-400L480-440Zm0-80 320-200H160l320 200ZM160-640v-80 480-400Z\"/></svg>";
            string svgMailPass  = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#52c41a\"><path d=\"m658-127-64-47 61-85-99-32 24-77 100 33v-105h80v105l100-33 24 77-99 32 61 85-64 47-62-85-62 85Zm-458 7q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v226q-19-9-39-14.5t-41-8.5v-203H200v360h168q9 27 30 47t47 28q-3 20-4 40.5t2 40.5q-36-7-67.5-26.5T320-320H200v120h253q7 22 16 42t22 38H200Zm0-80h253-253Zm80-410h400v-80H280v80Zm0 140h237q27-29 60.5-49t72.5-31H280v80Z\"/></svg>";
            string svgCookie    = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#d48806\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-75 29-147t81-128.5q52-56.5 125-91T475-881q21 0 43 2t45 7q-9 45 6 85t45 66.5q30 26.5 71.5 36.5t85.5-5q-26 59 7.5 113t99.5 56q1 11 1.5 20.5t.5 20.5q0 82-31.5 154.5t-85.5 127q-54 54.5-127 86T480-80Zm-60-480q25 0 42.5-17.5T480-620q0-25-17.5-42.5T420-680q-25 0-42.5 17.5T360-620q0 25 17.5 42.5T420-560Zm-80 200q25 0 42.5-17.5T400-420q0-25-17.5-42.5T340-480q-25 0-42.5 17.5T280-420q0 25 17.5 42.5T340-360Zm260 40q17 0 28.5-11.5T640-360q0-17-11.5-28.5T600-400q-17 0-28.5 11.5T560-360q0 17 11.5 28.5T600-320ZM480-160q122 0 216.5-84T800-458q-50-22-78.5-60T683-603q-77-11-132-66t-68-132q-80-2-140.5 29t-101 79.5Q201-644 180.5-587T160-480q0 133 93.5 226.5T480-160Zm0-324Z\"/></svg>";
            string svgToken     = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#eb2f96\"><path d=\"M480-80 120-280v-400l360-200 360 200v400L480-80ZM364-590q23-24 53-37t63-13q33 0 63 13t53 37l120-67-236-131-236 131 120 67Zm76 396v-131q-54-14-87-57t-33-98q0-11 1-20.5t4-19.5l-125-70v263l240 133Zm40-206q33 0 56.5-23.5T560-480q0-33-23.5-56.5T480-560q-33 0-56.5 23.5T400-480q0 33 23.5 56.5T480-400Zm40 206 240-133v-263l-125 70q3 10 4 19.5t1 20.5q0 55-33 98t-87 57v131Z\"/></svg>";
            string svgProxy     = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#096dd9\"><path d=\"M480-80q-139-35-229.5-159.5T160-516v-244l320-120 320 120v244q0 152-90.5 276.5T480-80Zm0-84q97-30 162-118.5T718-480H480v-315l-240 90v207q0 7 2 18h238v316Z\"/></svg>";
            string svgPhone     = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#08979c\"><path d=\"M798-120q-125 0-247-54.5T329-329Q225-433 170.5-555T116-802q0-18 12-28t28-10h150q14 0 23 10t13 25l26 128q2 13-0.5 24T359-634L259-533q26 44 55 82t64 72q37 38 78 69.5t86 55.5l95-98q10-11 23-15t25-2l119 26q15 4 24 14t9 25v145q0 16-10 28t-28 12Z\"/></svg>";
            string svgStatus    = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#595959\"><path d=\"M280-320q17 0 28.5-11.5T320-360q0-17-11.5-28.5T280-400q-17 0-28.5 11.5T240-360q0 17 11.5 28.5T280-320Zm-40-120h80v-200h-80v200Zm160 80h320v-80H400v80Zm0-160h320v-80H400v80ZM160-160q-33 0-56.5-23.5T80-240v-480q0-33 23.5-56.5T160-800h640q33 0 56.5 23.5T880-720v480q0 33-23.5 56.5T800-160H160Zm0-80h640v-480H160v480Zm0 0v-480 480Z\"/></svg>";
            string svgFilter    = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#531dab\"><path d=\"M440-160q-17 0-28.5-11.5T400-200v-240L168-736q-15-20-4.5-42t36.5-22h560q26 0 36.5 22t-4.5 42L560-440v240q0 17-11.5 28.5T520-160h-80Zm40-308 198-252H282l198 252Zm0 0Z\"/></svg>";
            string svgUpdate    = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#0050b3\"><path d=\"M467-120q-73-1-136-14t-110-34.5q-47-21.5-74-50T120-280q0 33 27 61.5t74 50Q268-147 331-134t136 14Zm-15-200q-38-2-73.5-6.5t-67.5-12q-32-7.5-60-17.5t-51-23q23 13 51 23t60 17.5q32 7.5 67.5 12T452-320Zm28-279q89 0 179-26.5T760-679q-11-29-100.5-55T480-760q-91 0-178.5 25.5T200-679q14 27 101.5 53.5T480-599Zm220 479h40v-164l72 72 28-28-120-120-120 120 28 28 72-72v164Zm20 80q-83 0-141.5-58.5T520-240q0-83 58.5-141.5T720-440q83 0 141.5 58.5T920-240q0 83-58.5 141.5T720-40ZM443-201q3 22 9 42t15 39q-73-1-136-14t-110-34.5q-47-21.5-74-50T120-280v-400q0-66 105.5-113T480-840q149 0 254.5 47T840-680v187q-19-9-39-15t-41-9v-62q-52 29-124 44t-156 15q-85 0-157-15t-123-44v101q51 47 130.5 62.5T480-400h11q-13 18-22.5 38T452-320q-76-4-141-18.5T200-379v99q7 13 30 26.5t56 24q33 10.5 73.5 18T443-201Z\"/></svg>";
            string svgTrash     = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#fa8c16\"><path d=\"M280-720v520-520Zm170 600H280q-33 0-56.5-23.5T200-200v-520h-40v-80h200v-40h240v40h200v80h-40v172q-17-5-39.5-8.5T680-560v-160H280v520h132q6 21 16 41.5t22 38.5Zm-90-160h40q0-63 20-103.5l20-40.5v-216h-80v360Zm160-230q17-11 38.5-22t41.5-16v-92h-80v130ZM680-80q-83 0-141.5-58.5T480-280q0-83 58.5-141.5T680-480q83 0 141.5 58.5T880-280q0 83-58.5 141.5T680-80Zm66-106 28-28-74-74v-112h-40v128l86 86Z\"/></svg>";
            string svgDelete    = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#cf1322\"><path d=\"m376-300 104-104 104 104 56-56-104-104 104-104-56-56-104 104-104-104-56 56 104 104-104 104 56 56Zm-96 180q-33 0-56.5-23.5T200-200v-520h-40v-80h200v-40h240v40h200v80h-40v520q0 33-23.5 56.5T680-120H280Zm400-600H280v520h400v-520Zm-400 0v520-520Z\"/></svg>";
            string svgRestore   = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#389e0d\"><path d=\"M440-320h80v-166l64 62 56-56-160-160-160 160 56 56 64-62v166ZM280-120q-33 0-56.5-23.5T200-200v-520h-40v-80h200v-40h240v40h200v80h-40v520q0 33-23.5 56.5T680-120H280Zm400-600H280v520h400v-520Zm-400 0v520-520Z\"/></svg>";
            string svgScript    = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#d4380d\"><path d=\"M320-240 80-480l240-240 57 57-184 183 184 183-57 57Zm320 0-57-57 184-183-184-183 57-57 240 240-240 240Z\"/></svg>";
            string svgGroup     = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#08979c\"><path d=\"M0-240v-63q0-43 44-70t116-27q13 0 25 .5t23 2.5q-14 21-21 44t-7 48v65H0Zm240 0v-65q0-32 17.5-58.5T307-410q32-20 76.5-30t96.5-10q53 0 97.5 10t76.5 30q32 20 49 46.5t17 58.5v65H240Zm540 0v-65q0-26-6.5-49T754-398q11-2 22.5-2.5t23.5-.5q72 0 116 26.5t44 70.5v63H780Zm-480-80h360v-6q0-37-74.5-60.5T480-410q-70 0-145 23.5T260-326v6ZM160-440q-33 0-56.5-23.5T80-520q0-34 23.5-57t56.5-23q34 0 57 23t23 57q0 33-23 56.5T160-440Zm640 0q-33 0-56.5-23.5T720-520q0-34 23.5-57t56.5-23q34 0 57 23t23 57q0 33-23 56.5T800-440Zm-320-40q-50 0-85-35t-35-85q0-51 35-85.5t85-34.5q51 0 85.5 34.5T600-600q0 50-34.5 85T480-480Zm0-80q17 0 28.5-11.5T520-600q0-17-11.5-28.5T480-640q-17 0-28.5 11.5T440-600q0 17 11.5 28.5T480-560Zm1 240Zm-1-280Z\"/></svg>";
            string svgDevice    = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#434343\"><path d=\"M300-120q-58 0-99-41t-41-99v-520q0-58 41-99t99-41h360q58 0 99 41t41 99v520q0 58-41 99t-99 41H300Zm0-80h360q25 0 42.5-17.5T720-260v-520q0-25-17.5-42.5T660-840H300q-25 0-42.5 17.5T240-780v520q0 25 17.5 42.5T300-200Zm180-100q17 0 28.5-11.5T520-340q0-17-11.5-28.5T480-380q-17 0-28.5 11.5T440-340q0 17 11.5 28.5T480-300ZM240-780v520-520Z\"/></svg>";
            string svgBackup    = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#006d75\"><path d=\"M480-80q-75 0-140.5-28.5t-114-77q-48.5-48.5-77-114T120-440h80q0 117 81.5 198.5T480-160q117 0 198.5-81.5T760-440q0-117-81.5-198.5T480-720h-6l62 62-56 58-160-160 160-160 56 58-62 62h6q75 0 140.5 28.5t114 77q48.5 48.5 77 114T840-440q0 75-28.5 140.5t-77 114q-48.5 48.5-114 77T480-80Z\"/></svg>";
            string svgHistory   = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#d46b08\"><path d=\"M480-120q-138 0-240.5-91.5T122-440h82q14 104 92.5 172T480-200q117 0 198.5-81.5T760-480q0-117-81.5-198.5T480-760q-69 0-129 32t-101 88h110v80H120v-240h80v94q51-64 124.5-99T480-840q75 0 140.5 28.5t114 77q48.5 48.5 77 114T840-480q0 75-28.5 140.5t-77 114q-48.5 48.5-114 77T480-120Zm112-192L440-464v-216h80v184l128 128-56 56Z\"/></svg>";
            string svgCheckpoint = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#ad4e00\"><path d=\"M480-80q-83 0-141.5-58.5T280-280q0-48 18.5-91t52.5-75l149-148 149 148q34 32 52.5 75t18.5 91q0 83-58.5 141.5T480-80Zm0-80q50 0 85-35t35-85q0-29-11-57t-33-49l-76-75-76 75q-22 21-33 49t-11 57q0 50 35 85t85 35ZM200-440l-56-56 240-240 160 160 164-164H600v-80h240v240h-80v-112L596-440 436-600 200-440Z\"/></svg>";
            string svgBlock     = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#a8071a\"><path d=\"M480-80q-83 0-156-31.5T197-197q-54-54-85.5-127T80-480q0-83 31.5-156T197-763q54-54 127-85.5T480-880q83 0 156 31.5T763-763q54 54 85.5 127T880-480q0 83-31.5 156T763-197q-54 54-127 85.5T480-80Zm0-80q54 0 104-17.5t92-50.5L228-660q-33 42-50.5 92T160-480q0 134 93 227t227 93Zm252-168L252-800q-42 33-59.5 83T175-616l429 429q51-18 88.5-55t55-88.5L732-328Z\"/></svg>";
            string svgLoginPhone = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#1890ff\"><path d=\"M480-120v-80h280v-560H480v-80h280q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H480Zm-80-160-57-56 103-104H120v-80h326L343-624l57-56 200 200-200 200Z\"/></svg>";
            string svgDuplicate = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#531dab\"><path d=\"M200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h168q13-36 43.5-58t68.5-22q38 0 68.5 22t43.5 58h168q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm0-80h560v-560H200v560Zm280-560q17 0 28.5-11.5T520-800q0-17-11.5-28.5T480-840q-17 0-28.5 11.5T440-800q0 17 11.5 28.5T480-760ZM200-200v-560 560Z\"/></svg>";
            string svgName      = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#1890ff\"><path d=\"M160-160v-100l80-80v180h-80Zm160 0v-260l80-80v340h-80Zm160 0v-340l80 81v259h-80Zm160 0v-259l80-80v339h-80Zm160 0v-419l80-80v499h-80ZM160-440l280-280 160 160 200-200 80 80-280 280-160-160-280 280v-160Z\"/></svg>";
            string svgSync      = "<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"24px\" viewBox=\"0 -960 960 960\" width=\"24px\" fill=\"#13c2c2\"><path d=\"M160-160v-80h110l-16-14q-52-46-73-105t-21-119q0-111 66.5-197.5T400-790v84q-72 26-116 88.5T240-478q0 45 17 87.5t53 78.5l10 10v-98h80v240H160Zm400-10v-84q72-26 116-88.5T720-482q0-45-17-87.5T650-648l-10-10v98h-80v-240h240v80H690l16 14q49 49 71.5 106.5T800-482q0 111-66.5 197.5T560-170Z\"/></svg>";

            // Load dynamic items từ cache — không query DB khi chuột phải.
            var scripts = _cachedScripts;
            if (scripts.Count == 0)
                scripts = _scriptContext.GetByPlatform(_platform) ?? new List<Script>();
            var folders = _cachedFolders;
            if (folders.Count == 0)
                folders = _folderContext.GetByType(_platform) ?? new List<Folder>();

            // Build Kịch bản submenu
            var scriptSubItems = new List<AntdUI.IContextMenuStripItem>
            {
                new AntdUI.ContextMenuStripItem("[Không cần kịch bản]")
            };
            foreach (var s in (scripts ?? new List<Script>()).Where(x => !string.IsNullOrEmpty(x.Name)))
                scriptSubItems.Add(new AntdUI.ContextMenuStripItem(s.Name ?? ""));

            // Build Chuyển nhóm submenu
            var folderSubItems = new List<AntdUI.IContextMenuStripItem>();
            foreach (var f in (folders ?? new List<Folder>()).Where(x => !string.IsNullOrEmpty(x.Name)))
                folderSubItems.Add(new AntdUI.ContextMenuStripItem(f.Name ?? ""));

            // Build Định danh submenu
            var deviceSubItems = new List<AntdUI.IContextMenuStripItem>
            {
                new AntdUI.ContextMenuStripItem("Định danh nhanh"),
                new AntdUI.ContextMenuStripItem("Xóa định danh thiết bị"),
            };
            int deviceMenuCount = 0;
            foreach (var d in _cachedDeviceMenuItems)
            {
                if (deviceMenuCount >= MaxDeviceMenuItems) break;
                deviceSubItems.Add(new AntdUI.ContextMenuStripItem($"[ {d.Serial} ][ {d.Model} ]"));
                deviceMenuCount++;
            }
            if (_cachedDeviceMenuItems.Count > MaxDeviceMenuItems)
            {
                deviceSubItems.Add(new AntdUI.ContextMenuStripItem(
                    $"... {_cachedDeviceMenuItems.Count - MaxDeviceMenuItems} thiết bị khác"));
            }

            var items = new List<AntdUI.IContextMenuStripItem>
            {
                // 1. Chọn
                new AntdUI.ContextMenuStripItem("Chọn").SetSub(
                    new AntdUI.ContextMenuStripItem("Tất cả"),
                    new AntdUI.ContextMenuStripItem("Bôi đen"),
                    new AntdUI.ContextMenuStripItem("Bỏ chọn bôi đen"),
                    new AntdUI.ContextMenuStripItem("Chọn theo tình trạng").SetSub(
                        new AntdUI.ContextMenuStripItem("LIVE"),
                        new AntdUI.ContextMenuStripItem("DIE"),
                        new AntdUI.ContextMenuStripItem("Chưa xác định"),
                        new AntdUI.ContextMenuStripItem("Đang chạy")
                    )
                ),
                // 2. Bỏ chọn tất cả
                new AntdUI.ContextMenuStripItem("Bỏ chọn tất cả"),
                // 3. Copy
                new AntdUI.ContextMenuStripItem("Copy").SetSub(
                    new AntdUI.ContextMenuStripItem("Copy email"),
                    new AntdUI.ContextMenuStripItem("Copy pass"),
                    new AntdUI.ContextMenuStripItem("Copy code 2fa"),
                    new AntdUI.ContextMenuStripItem("Copy uid"),
                    new AntdUI.ContextMenuStripItem("Copy name"),
                    new AntdUI.ContextMenuStripItem("Copy mail"),
                    new AntdUI.ContextMenuStripItem("Copy pass mail"),
                    new AntdUI.ContextMenuStripItem("Copy mail recover"),
                    new AntdUI.ContextMenuStripItem("Copy pass mail recover"),
                    new AntdUI.ContextMenuStripItem("Copy phone"),
                    new AntdUI.ContextMenuStripItem("Copy cookie"),
                    new AntdUI.ContextMenuStripItem("Copy token"),
                    new AntdUI.ContextMenuStripItem("Copy proxy"),
                    new AntdUI.ContextMenuStripItem("Copy uid|pass"),
                    new AntdUI.ContextMenuStripItem("Copy uid|pass|2fa"),
                    new AntdUI.ContextMenuStripItem("Copy uid|pass|2fa|cookie"),
                    new AntdUI.ContextMenuStripItem("Copy uid|pass|cookie"),
                    new AntdUI.ContextMenuStripItem("Copy uid|pass|cookie|2fa"),
                    new AntdUI.ContextMenuStripItem("Copy tk bị checkpoint"),
                    new AntdUI.ContextMenuStripItem("Copy tk bị chặn tương tác"),
                    new AntdUI.ContextMenuStripItem("Copy định dạng tùy chọn")
                ),
                // 4. Chức năng
                new AntdUI.ContextMenuStripItem("Chức năng").SetSub(
                    new AntdUI.ContextMenuStripItem("Check live"),
                    new AntdUI.ContextMenuStripItem("Show password"),
                    new AntdUI.ContextMenuStripItem("Reset trạng thái"),
                    new AntdUI.ContextMenuStripItem("Clear trạng thái"),
                    new AntdUI.ContextMenuStripItem("Kiểm tra avatar"),
                    new AntdUI.ContextMenuStripItem("Kiểm tra cookie"),
                    new AntdUI.ContextMenuStripItem("Tải xuống avatar"),
                    new AntdUI.ContextMenuStripItem("Copy debug lỗi"),
                    new AntdUI.ContextMenuStripItem("Check name VN"),
                    new AntdUI.ContextMenuStripItem("Kiểm tra live proxy")
                ),
                // 5. Kịch bản
                new AntdUI.ContextMenuStripItem("Kịch bản").SetSub(scriptSubItems.ToArray()),
                // 6. Chuyển nhóm
                new AntdUI.ContextMenuStripItem("Chuyển nhóm").SetSub(folderSubItems.Count > 0 ? folderSubItems.ToArray() : new[] { new AntdUI.ContextMenuStripItem("(Chưa có nhóm)") }),
                // 7. Login phone
                new AntdUI.ContextMenuStripItem("Login phone"),
                // 8. Cập nhật dữ liệu
                new AntdUI.ContextMenuStripItem("Cập nhật dữ liệu").SetSub(
                    new AntdUI.ContextMenuStripItem("Theo uid hoặc email"),
                    new AntdUI.ContextMenuStripItem("Pass"),
                    new AntdUI.ContextMenuStripItem("2FA"),
                    new AntdUI.ContextMenuStripItem("Cookie"),
                    new AntdUI.ContextMenuStripItem("Proxy"),
                    new AntdUI.ContextMenuStripItem("Mail"),
                    new AntdUI.ContextMenuStripItem("Pass mail"),
                    new AntdUI.ContextMenuStripItem("Mail recover"),
                    new AntdUI.ContextMenuStripItem("Pass mail recover"),
                    new AntdUI.ContextMenuStripItem("Phone"),
                    new AntdUI.ContextMenuStripItem("Ngày sinh"),
                    new AntdUI.ContextMenuStripItem("User agent"),
                    new AntdUI.ContextMenuStripItem("Ghi chú"),
                    new AntdUI.ContextMenuStripItem("Xóa Name")
                ),
                // 9. Định danh thiết bị
                new AntdUI.ContextMenuStripItem("Định danh thiết bị").SetSub(deviceSubItems.ToArray()),
                // 10. Quản lý backup profile
                new AntdUI.ContextMenuStripItem("Quản lý backup profile").SetSub(
                    new AntdUI.ContextMenuStripItem("Check backup profile"),
                    new AntdUI.ContextMenuStripItem("Xóa backup profile"),
                    new AntdUI.ContextMenuStripItem("Copy backup profile"),
                    new AntdUI.ContextMenuStripItem("Check backup device"),
                    new AntdUI.ContextMenuStripItem("Xóa backup device"),
                    new AntdUI.ContextMenuStripItem("Copy backup device"),
                    new AntdUI.ContextMenuStripItem("Copy backup profile và device"),
                    new AntdUI.ContextMenuStripItem("Xóa backup profile và device"),
                    new AntdUI.ContextMenuStripItem("Don dẹp tất cả backup dư thừa")
                ),
                // 11. Lọc trùng
                new AntdUI.ContextMenuStripItem("Lọc trùng tài khoản"),
                // 11.5 Đồng bộ từ tool khác
                new AntdUI.ContextMenuStripItem("Đồng bộ từ tool khác").SetSub(
                    new AntdUI.ContextMenuStripItem("MaxCare"),
                    new AntdUI.ContextMenuStripItem("FPlus"),
                    new AntdUI.ContextMenuStripItem("MetaMax")
                ),
                // 12. Lịch sử
                new AntdUI.ContextMenuStripItem("Lịch sử hoạt động [ HOT ]"),
                // 13. Xóa checkpoint
                new AntdUI.ContextMenuStripItem("Xóa tk bị checkpoint"),
                // 14. Xóa bị chặn
                new AntdUI.ContextMenuStripItem("Xóa tk bị chặn tương tác"),
                // 15. Xóa vào thùng rác
                new AntdUI.ContextMenuStripItem("Xóa tk vào thùng rác"),
                // 16. Xóa vĩnh viễn
                new AntdUI.ContextMenuStripItem("Xóa tài khoản vĩnh viễn"),
            };

            menulist = items.ToArray();
            _menuStripDirty = false;
            AccountGridPerf.Log(swMenu, "CreateMenuStrip");
        }

        private void DataGridView1_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (menulist != null && !_menuStripDirty)
            {
                BeginInvoke(ShowContextMenu);
                return;
            }
            _ = WarmupAndShowContextMenuAsync();
        }

        private async Task WarmupAndShowContextMenuAsync()
        {
            await WarmupMenuStripAsync().ConfigureAwait(false);
            if (IsDisposed || !IsHandleCreated) return;
            BeginInvoke(ShowContextMenu);
        }

        private void ShowContextMenu()
        {
            if (IsDisposed || !IsHandleCreated) return;
            if (menulist == null) return;
            long _h = UiThreadProfiler.Begin();
            // Native WinForms ContextMenuStrip — cache & tái dùng; chỉ rebuild khi menulist đổi
            // (WarmupMenuStripAsync tạo mảng mới mỗi lần rebuild → ReferenceEquals phát hiện).
            if (_cachedNativeMenu == null || _cachedNativeMenu.IsDisposed
                || !ReferenceEquals(_cachedNativeMenuSource, menulist))
            {
                _cachedNativeMenu?.Dispose();
                _cachedNativeMenu = BuildNativeMenu(menulist);
                _cachedNativeMenuSource = menulist;
            }
            _cachedNativeMenu.Show(Cursor.Position);
            AccountGridPerf.LogHandler("ShowContextMenu(build+show)", _h);
        }

        /// <summary>Convert cây menu (định nghĩa bằng AntdUI item) sang ContextMenuStrip native.</summary>
        private System.Windows.Forms.ContextMenuStrip BuildNativeMenu(AntdUI.IContextMenuStripItem[] items)
        {
            var menu = new System.Windows.Forms.ContextMenuStrip { Font = FontScale.Body9 };
            menu.Items.AddRange(BuildNativeItems(items));
            return menu;
        }

        private ToolStripItem[] BuildNativeItems(AntdUI.IContextMenuStripItem[] items)
        {
            var result = new List<ToolStripItem>(items.Length);
            foreach (var item in items)
            {
                if (item is not AntdUI.ContextMenuStripItem ci)
                {
                    result.Add(new ToolStripSeparator());
                    continue;
                }
                var mi = new ToolStripMenuItem(ci.Text);
                if (ci.Sub != null && ci.Sub.Length > 0)
                    mi.DropDownItems.AddRange(BuildNativeItems(ci.Sub));
                else
                {
                    string text = ci.Text;
                    mi.Click += (_, __) => { _ = RightKey(text); };
                }
                result.Add(mi);
            }
            return result.ToArray();
        }

        private void UpdateCachedCounts()
        {
            var snapshot = _accounts;
            if (snapshot == null) return;
            if (_checkAllActive)
                _cachedCheckedCount = snapshot.Count - (_checkAllExceptions?.Count ?? 0);
            else
                _cachedCheckedCount = _checkedAccounts.Count;
            if (snapshot.Count <= LargeListThreshold)
                _cachedRunningCount = CountRunning(snapshot);
            ControlHelper.SetToolStripLabelTextSafe(toolStripLabel6, $"{_cachedCheckedCount}");
            ControlHelper.SetToolStripLabelTextSafe(toolStripLabel2, $"{_cachedRunningCount}");
            InvalidateCheckboxColumnIfNeeded();
        }

        /// <summary>
        /// Materialize danh sách đã tick + mutate + Update DB — TẤT CẢ trên thread pool,
        /// có loading overlay. Tránh block UI thread khi 30k tài khoản (root cause đơ).
        /// </summary>
        private async Task MutateCheckedAndSaveAsync(Action<Account> mutate, string emptyMsg, string successMsg = null)
        {
            await WinFormsHelper.Spin(this, "Đang xử lý...", async () =>
            {
                var toUpdate = await Task.Run(() =>
                {
                    var list = MaterializeCheckedForUi();
                    if (list.Count == 0) return list;
                    for (int i = 0; i < list.Count; i++) mutate(list[i]);
                    _accountContext.Update(list);
                    return list;
                });

                if (toUpdate.Count == 0)
                {
                    if (emptyMsg != null) AntdHelper.MsgWarn(_form, emptyMsg);
                    return;
                }

                await LoadAccounts();
                if (successMsg != null) AntdHelper.MsgSuccess(_form, string.Format(successMsg, toUpdate.Count));
            });
        }

        /// <summary>
        /// Materialize danh sách đã tick trên thread pool rồi copy format ra clipboard (UI thread).
        /// Tránh block UI khi 30k tài khoản.
        /// </summary>
        private async Task CopyCheckedAsync(string format)
        {
            await WinFormsHelper.Spin(this, "Đang sao chép...", async () =>
            {
                var list = await Task.Run(() => MaterializeCheckedForUi());
                if (list.Count == 0) { AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox tài khoản."); return; }
                ConvertHelper.CopyFormat(format, list);
            });
        }

        // Overload tương thích AntdUI (nếu còn caller cũ) — chuyển sang dispatch theo text.
        private void RightKey(AntdUI.ContextMenuStripItem it) => _ = RightKey(it.Text);

        private async Task RightKey(string menuText)
        {
            var it = (Text: menuText ?? "", _ignored: 0);
            // ── Chọn ──────────────────────────────────────────────────────────
            if (it.Text.Equals("Tất cả"))
            {
                ApplyCheckedToAll(true);
            }
            else if (it.Text.Equals("Bôi đen"))
            {
                ClearCheckedSelectionState();
                // Dùng range thay vì SelectedRows để lấy đủ acc khi bôi đen nhiều row.
                var selected = SelectedAccountsByRange();
                if (!UseSparseCheckedLookup)
                {
                    for (int i = 0; i < _accounts.Count; i++)
                        _accounts[i].SetCheckedSilently(false);
                }
                SetSparseCheckedAccounts(selected);
                _cachedCheckedCount = _checkedAccounts.Count;
                RefreshCheckedVisual(cachedCountKnown: true, repaintGrid: false);
            }
            else if (it.Text.Equals("Bỏ chọn tất cả"))
            {
                ApplyCheckedToAll(false);
            }
            else if (it.Text.Equals("Bỏ chọn bôi đen"))
            {
                if (_checkAllActive)
                {
                    _checkAllExceptions ??= new HashSet<Guid>();
                    foreach (var a in SelectedAccounts)
                        _checkAllExceptions.Add(a.Id);
                    _cachedCheckedCount = SelectionScopeCount - _checkAllExceptions.Count;
                    RefreshCheckedVisual(cachedCountKnown: true, repaintGrid: false);
                }
                else
                {
                    foreach (var a in SelectedAccounts)
                    {
                        if (!IsAccountChecked(a)) continue;
                        if (!UseSparseCheckedLookup)
                            a.SetCheckedSilently(false);
                        SyncCheckedState(a, false);
                    }
                    _cachedCheckedCount = _checkedAccounts.Count;
                    RefreshCheckedVisual(cachedCountKnown: true, repaintGrid: false);
                }
            }
            // ── Chọn theo tình trạng ─────────────────────────────────────────
            else if (it.Text.Equals("LIVE"))
            {
                _ = ApplyCheckedBySqlAsync($" AND UPPER({nameof(Account.State)}) = 'LIVE'");
            }
            else if (it.Text.Equals("DIE"))
            {
                _ = ApplyCheckedBySqlAsync($" AND UPPER({nameof(Account.State)}) = 'DIE'");
            }
            else if (it.Text.Equals("Chưa xác định"))
            {
                _ = ApplyCheckedBySqlAsync($" AND ({nameof(Account.State)} IS NULL OR {nameof(Account.State)} = '')");
            }
            else if (it.Text.Equals("Đang chạy"))
            {
                _ = ApplyCheckedBySqlAsync($" AND {nameof(Account.Running)} = 1");
            }
            // ── Copy ──────────────────────────────────────────────────────────
            else if (it.Text.Equals("Copy email"))
            {
                await CopyCheckedAsync(nameof(Account.Email));
            }
            else if (it.Text.Equals("Copy pass"))
            {
                if (!VerifySubdyPassword()) return;
                await CopyCheckedAsync(nameof(Account.Password));
            }
            else if (it.Text.Equals("Copy code 2fa"))
            {
                if (!VerifySubdyPassword()) return;
                await CopyCheckedAsync(nameof(Account.TowFA));
            }
            else if (it.Text.Equals("Copy uid"))
            {
                await CopyCheckedAsync(nameof(Account.Uid));
            }
            else if (it.Text.Equals("Copy name"))
            {
                await CopyCheckedAsync(nameof(Account.FullName));
            }
            else if (it.Text.Equals("Copy mail"))
            {
                await CopyCheckedAsync(nameof(Account.EmailAddress));
            }
            else if (it.Text.Equals("Copy pass mail"))
            {
                await CopyCheckedAsync(nameof(Account.PassMail));
            }
            else if (it.Text.Equals("Copy mail recover"))
            {
                await CopyCheckedAsync(nameof(Account.EmailAddress));
            }
            else if (it.Text.Equals("Copy pass mail recover"))
            {
                await CopyCheckedAsync(nameof(Account.PassPrivateEmailAddress));
            }
            else if (it.Text.Equals("Copy phone"))
            {
                await CopyCheckedAsync(nameof(Account.Phone));
            }
            else if (it.Text.Equals("Copy cookie"))
            {
                await CopyCheckedAsync(nameof(Account.Cookie));
            }
            else if (it.Text.Equals("Copy token"))
            {
                await CopyCheckedAsync(nameof(Account.Token));
            }
            else if (it.Text.Equals("Copy proxy"))
            {
                await CopyCheckedAsync(nameof(Account.Proxy));
            }
            else if (it.Text.Equals("Copy uid|pass"))
            {
                if (!VerifySubdyPassword()) return;
                await CopyCheckedAsync($"{nameof(Account.Uid)}|{nameof(Account.Password)}");
            }
            else if (it.Text.Equals("Copy uid|pass|2fa"))
            {
                if (!VerifySubdyPassword()) return;
                await CopyCheckedAsync($"{nameof(Account.Uid)}|{nameof(Account.Password)}|{nameof(Account.TowFA)}");
            }
            else if (it.Text.Equals("Copy uid|pass|2fa|cookie"))
            {
                if (!VerifySubdyPassword()) return;
                await CopyCheckedAsync($"{nameof(Account.Uid)}|{nameof(Account.Password)}|{nameof(Account.TowFA)}|{nameof(Account.Cookie)}");
            }
            else if (it.Text.Equals("Copy uid|pass|cookie"))
            {
                if (!VerifySubdyPassword()) return;
                await CopyCheckedAsync($"{nameof(Account.Uid)}|{nameof(Account.Password)}|{nameof(Account.Cookie)}");
            }
            else if (it.Text.Equals("Copy uid|pass|cookie|2fa"))
            {
                if (!VerifySubdyPassword()) return;
                await CopyCheckedAsync($"{nameof(Account.Uid)}|{nameof(Account.Password)}|{nameof(Account.Cookie)}|{nameof(Account.TowFA)}");
            }
            else if (it.Text.Equals("Copy tk bị checkpoint"))
            {
                var uids = GetScopedUidsByState($" AND {nameof(Account.State)} LIKE 'CP%'");
                if (!uids.Any()) { AntdHelper.MsgWarn(_form, "Không có tài khoản checkpoint."); return; }
                Clipboard.SetText(string.Join("\n", uids));
                AntdHelper.MsgSuccess(_form, $"Copy thành công {uids.Count} tài khoản checkpoint.");
            }
            else if (it.Text.Equals("Copy tk bị chặn tương tác"))
            {
                var uids = GetScopedUidsByState($" AND UPPER({nameof(Account.State)}) = 'DIE'");
                if (!uids.Any()) { AntdHelper.MsgWarn(_form, "Không có tài khoản bị chặn tương tác."); return; }
                Clipboard.SetText(string.Join("\n", uids));
                AntdHelper.MsgSuccess(_form, $"Copy thành công {uids.Count} tài khoản bị chặn.");
            }
            else if (it.Text.Equals("Copy định dạng tùy chọn"))
            {
                ShowCopyCustomFormatDialog();
            }
            // ── Chức năng ─────────────────────────────────────────────────────
            else if (it.Text.Equals("Show password"))
            {
                if (!VerifySubdyPassword()) return;
                var selected = CheckedAccounts.ToList();
                if (!selected.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox tài khoản cần xem."); return; }
                using var dlg = new Form
                {
                    Text = "Thông tin mật khẩu",
                    Width = 540,
                    Height = Math.Min(60 + selected.Count * 28 + 60, 600),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = false, MinimizeBox = false
                };
                var dgv = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    RowHeadersVisible = false,
                    AllowUserToAddRows = false
                };
                dgv.Columns.Add("uid", "UID/Email");
                dgv.Columns.Add("pass", "Password");
                dgv.Columns.Add("twofa", "2FA");
                foreach (var a in selected)
                    dgv.Rows.Add(a.Uid, a.Password, a.TowFA);
                dlg.Controls.Add(dgv);
                dlg.ShowDialog();
            }
            else if (it.Text.Equals("Reset trạng thái"))
            {
                await MutateCheckedAndSaveAsync(a => a.State = "",
                    "Vui lòng tick checkbox tài khoản cần reset.",
                    "Đã reset trạng thái {0} tài khoản.");
            }
            else if (it.Text.Equals("Clear trạng thái"))
            {
                // Clear cột "Trạng thái" (Account.Status). Status là field runtime → phải persist
                // qua UpdateStatusBatch (Update generic không ghi cột Status).
                await WinFormsHelper.Spin(this, "Đang xử lý...", async () =>
                {
                    var toUpdate = await Task.Run(() =>
                    {
                        var list = MaterializeCheckedForUi();
                        if (list.Count == 0) return list;
                        foreach (var a in list) a.Status = "";
                        try { _accountContext.UpdateStatusBatch(list); }
                        catch (Exception ex) { Sunny.Subdy.Common.Logs.LogManager.Error(ex); }
                        return list;
                    });

                    if (toUpdate.Count == 0)
                    {
                        AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox tài khoản cần clear trạng thái.");
                        return;
                    }

                    await LoadAccounts();
                    AntdHelper.MsgSuccess(_form, $"Đã clear trạng thái {toUpdate.Count} tài khoản.");
                });
            }
            else if (it.Text.Equals("Copy debug lỗi"))
            {
                var targets = CheckedAccounts
                    .Where(a => a.Running).ToList();
                if (!targets.Any()) { AntdHelper.MsgWarn(_form, "Không có tài khoản nào vừa tick vừa đang chạy."); return; }
                var sb = new System.Text.StringBuilder();
                foreach (var a in targets)
                    sb.AppendLine($"Uid:{a.Uid} | State:{a.State} | Status:{a.Status} | Result:{a.Result}");
                Clipboard.SetText(sb.ToString());
                AntdHelper.MsgSuccess(_form, $"Đã copy debug {targets.Count} tài khoản đang chạy.");
            }
            else if (it.Text.Equals("Xóa định danh thiết bị"))
            {
                await MutateCheckedAndSaveAsync(a => a.DeviceInfo = "", null);
            }
            else if (it.Text.Equals("Check live"))
            {
                _ = RunCheckLiveAsync(false);
            }
            else if (it.Text.Equals("Kiểm tra avatar"))
            {
                _ = RunCheckLiveAsync(true);
            }
            else if (it.Text.Equals("Kiểm tra cookie"))
            {
                _ = RunCheckCookieAsync();
            }
            else if (it.Text.Equals("Check name VN"))
            {
                RunCheckNameVN();
            }
            else if (it.Text.Equals("Kiểm tra live proxy"))
            {
                _ = RunCheckProxyAsync();
            }
            else if (it.Text.Equals("Tải xuống avatar") || it.Text.Equals("Định danh nhanh"))
            {
                AntdHelper.MsgWarn(_form, "Tính năng đang phát triển.");
            }
            else if (it.Text.Equals("Login phone"))
            {
                _ = LoginPhoneAsync();
            }
            // ── Quản lý backup profile ─────────────────────────────────────────
            else if (it.Text.Equals("Check backup profile"))
            {
                BackupAction(BackupType.Profile, BackupOp.Check);
            }
            else if (it.Text.Equals("Xóa backup profile"))
            {
                BackupAction(BackupType.Profile, BackupOp.Delete);
            }
            else if (it.Text.Equals("Copy backup profile"))
            {
                BackupAction(BackupType.Profile, BackupOp.Copy);
            }
            else if (it.Text.Equals("Check backup device"))
            {
                BackupAction(BackupType.Device, BackupOp.Check);
            }
            else if (it.Text.Equals("Xóa backup device"))
            {
                BackupAction(BackupType.Device, BackupOp.Delete);
            }
            else if (it.Text.Equals("Copy backup device"))
            {
                BackupAction(BackupType.Device, BackupOp.Copy);
            }
            else if (it.Text.Equals("Copy backup profile và device"))
            {
                BackupAction(BackupType.Both, BackupOp.Copy);
            }
            else if (it.Text.Equals("Xóa backup profile và device"))
            {
                BackupAction(BackupType.Both, BackupOp.Delete);
            }
            else if (it.Text.Equals("Don dẹp tất cả backup dư thừa"))
            {
                CleanupRedundantBackups();
            }
            // ── Kịch bản ──────────────────────────────────────────────────────
            else if (it.Text.Equals("[Không cần kịch bản]"))
            {
                await SetScriptForCheckedAsync("");
            }
            // ── Cập nhật dữ liệu ──────────────────────────────────────────────
            else if (it.Text.Equals("Theo uid hoặc email"))
            {
                if (!LoginGuard.EnsureLoggedIn(_form)) return;
                fAddAccount fAdd = new fAddAccount(_platform, false);
                fAdd.ShowDialog();
                _ = LoadAccounts();
            }
            else if (it.Text.Equals("Proxy"))
            {
                var ids = new List<Guid>();
                foreach (var a in CheckedAccounts) ids.Add(a.Id);
                if (!ids.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox dòng cần cập nhật."); return; }
                new fImportProxy(ids).ShowDialog();
                _ = LoadAccounts();
            }
            else if (it.Text.Equals("Xóa Name"))
            {
                if (!HasAnyChecked()) { AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox dòng cần xóa."); return; }
                if (!AntdHelper.Confirm(_form, "Xác nhận", $"Xóa Name của {_cachedCheckedCount} tài khoản?")) return;
                await MutateCheckedAndSaveAsync(a => a.FullName = "", null);
            }
            else if (it.Text.Equals("Pass") || it.Text.Equals("2FA") ||
                     it.Text.Equals("Cookie") || it.Text.Equals("Mail") || it.Text.Equals("Pass mail") ||
                     it.Text.Equals("Mail recover") || it.Text.Equals("Pass mail recover") ||
                     it.Text.Equals("Phone") || it.Text.Equals("Ngày sinh") || it.Text.Equals("User agent") ||
                     it.Text.Equals("Ghi chú"))
            {
                ShowUpdateFieldPopup(it.Text);
            }
            // ── Lọc trùng ─────────────────────────────────────────────────────
            else if (it.Text.Equals("Lọc trùng tài khoản"))
            {
                FilterDuplicateAccounts();
            }
            // ── Đồng bộ từ tool khác ──────────────────────────────────────────
            else if (it.Text.Equals("MaxCare") || it.Text.Equals("FPlus") || it.Text.Equals("MetaMax"))
            {
                OpenSyncFromOtherTool(it.Text);
            }
            // ── Lịch sử ───────────────────────────────────────────────────────
            else if (it.Text.Equals("Lịch sử hoạt động [ HOT ]"))
            {
                AntdHelper.MsgWarn(_form, "Tính năng đang phát triển.");
            }
            // ── Xóa theo tình trạng ───────────────────────────────────────────
            else if (it.Text.Equals("Xóa tk bị checkpoint"))
            {
                TrashCheckpointAccounts();
            }
            else if (it.Text.Equals("Xóa tk bị chặn tương tác"))
            {
                TrashBlockedAccounts();
            }
            // ── Xóa / Khôi phục ───────────────────────────────────────────────
            else if (it.Text.Equals("Xóa tk vào thùng rác") || it.Text.Equals("Xóa tài khoản vào thùng rác"))
            {
                TrashSelectedAccounts();
            }
            else if (it.Text.Equals("Xóa tài khoản vĩnh viễn"))
            {
                DeleteAccountsPermanently();
            }
            else if (it.Text.Equals("Khôi phục về nhóm cũ"))
            {
                var ids = new List<Guid>();
                foreach (var a in CheckedAccounts) if (!a.IsView) ids.Add(a.Id);
                if (!ids.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox dòng cần khôi phục."); return; }
                if (!AntdHelper.Confirm(_form, "Xác nhận", $"Bạn có chắc chắn muốn khôi phục {ids.Count} tài khoản?")) return;
                if (_accountContext.UpdateIsViewTrue(ids)) AntdHelper.MsgSuccess(_form, "Đã khôi phục thành công.");
                else AntdHelper.MsgError(_form, "Đã xảy ra lỗi.");
                _ = LoadAccounts();
            }
            else if (it.Text.Equals("Cập nhật token job"))
            {
                var ids = new List<string>();
                foreach (var a in CheckedAccounts) ids.Add(a.Id.ToString());
                if (!ids.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox dòng cần cập nhật."); return; }
                new fUpdateData(ids, fUpdateData.TokenJob, _platform).ShowDialog();
                _ = LoadAccounts();
            }
            else
            {
                // Dynamic: Kịch bản — kiểm tra trước để tránh nhầm với folder cùng tên
                var scripts = _scriptContext.GetByPlatform(_platform) ?? new List<Script>();
                var matchScript = scripts.FirstOrDefault(s => s.Name == it.Text);
                if (matchScript != null)
                {
                    await SetScriptForCheckedAsync(matchScript.Name ?? "");
                    return;
                }

                // Dynamic: Chuyển nhóm
                var folders = _folderContext.GetByType(_platform) ?? new List<Folder>();
                var matchFolder = folders.FirstOrDefault(f => f.Name == it.Text);
                if (matchFolder != null)
                {
                    string folderName = matchFolder.Name ?? "";
                    if (!HasAnyChecked())
                    {
                        AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox tài khoản cần chuyển nhóm.");
                        return;
                    }
                    if (!AntdHelper.Confirm(_form, "Xác nhận chuyển nhóm",
                        $"Bạn có chắc chắn muốn chuyển {_cachedCheckedCount} tài khoản đã tick sang nhóm [{folderName}]?"))
                        return;
                    await MutateCheckedAndSaveAsync(a => a.NameFolder = folderName, null,
                        $"Đã chuyển {{0}} tài khoản sang nhóm [{folderName}].");
                    return;
                }
            }
        }

        private async Task Reg()
        {
            fSettingRegsiner fSettingRegsiner = new fSettingRegsiner(_platform);
            if (fSettingRegsiner.ShowDialog() != DialogResult.OK) return;
            if (!SeleceterDevice())
            {
                return;
            }
            var model = GetConfigModel(true);
            if (model == null)
            {
                return;
            }
            try
            {
                Enable(false);
                fMain.StartTime = DateTime.Now;
                Globals.CancellationTokenSource = new CancellationTokenSource();
                CancellationToken ct = Globals.CancellationTokenSource.Token;
                List<Task> tasks = new List<Task>();
                foreach (var device in DeviceServices.DeviceModels.Where(x => x.Checked))
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        ADBClient client = new ADBClient(device);
                        ChangeLanguageService changeLanguage = new ChangeLanguageService(client);
                        await changeLanguage.Change("en", "US");
                        await client.TurnOnADBKeyboard();
                        FacebookRegsiner service = new FacebookRegsiner(_platform, client, model, ct);
                        service.AccountAdded += (s, acc) =>
                        {
                            AddAccountThreadSafe(acc);
                        };
                        await service.RunAsync();
                    }));
                }
                await Task.WhenAll(tasks);
            }
            finally
            {
                Enable(true);
                fMain.StartTime = null;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // RunRegFacebookAsync — luồng ĐĂNG KÝ cho module "Reg Facebook" (nút "Chạy").
        //
        // Khác luồng farm (button7_Click bên dưới chạy theo account đã tick): đăng ký
        // chạy theo THIẾT BỊ — mỗi thiết bị chọn chạy RegFacebookRegsiner, engine tự
        // tạo account mới vòng lặp cho tới khi người dùng bấm "Dừng" (CancellationToken).
        // Account mới được OnAccountAdded → AddAccountThreadSafe đẩy lên lưới mục
        // "Reg Facebook" (cách ly theo Platformt). Cấu hình đọc từ form "Hành động"
        // (fSettingRegFacebook) mà user đã điền + lưu trước đó qua GetConfigModel(true).
        //
        // KHÔNG đụng vào FacebookRegsiner/FacebookRegsiner.cs — dùng engine clone riêng
        // RegFacebookRegsiner (cùng chữ ký ctor, drop-in với call-site Reg() chết).
        // ─────────────────────────────────────────────────────────────────────────────
        private async Task RunRegFacebookAsync()
        {
            // Chọn thiết bị chạy reg (đúng dialog "chọn số lượng thiết bị" như luồng farm).
            Enable(false);
            try
            {
                if (!SeleceterDevice()) return;

                var model = GetConfigModel(true);
                if (model == null) return;

                fMain.StartTime = DateTime.Now;
                Globals.CancellationTokenSource = new CancellationTokenSource();
                CancellationToken ct = Globals.CancellationTokenSource.Token;

                // Xóa registry client cũ trước batch mới (giống button7_Click).
                while (_activeClients.TryTake(out _)) { }

                var tasks = new List<Task>();
                foreach (var device in DeviceServices.DeviceModels.Where(x => x.Checked))
                {
                    if (ct.IsCancellationRequested) break;
                    tasks.Add(Task.Run(async () =>
                    {
                        ADBClient client = new ADBClient(device);
                        // Đăng ký client để nút "Dừng" force-stop được (set Running=false).
                        _activeClients.Add(client);
                        device.Status = "Đang kiểm tra kết nối ADB...";
                        device.TypeColor = 2;
                        try
                        {
                            if (!client.Connect())
                            {
                                device.Status = "Không thể kết nối ADB, dừng thiết bị.";
                                device.TypeColor = 1;
                                return;
                            }

                            // Đổi ngôn ngữ + bật ADB keyboard (giống call-site Reg() gốc).
                            ChangeLanguageService changeLanguage = new ChangeLanguageService(client);
                            await changeLanguage.Change("en", "US");
                            await client.TurnOnADBKeyboard();

                            RegFacebookRegsiner service = new RegFacebookRegsiner(_platform, client, model, ct);
                            service.AccountAdded += (s, acc) => AddAccountThreadSafe(acc);
                            await service.RunAsync();
                        }
                        catch (OperationCanceledException)
                        {
                            device.Status = "Đã dừng theo yêu cầu.";
                            device.TypeColor = 1;
                        }
                        catch (Exception ex)
                        {
                            device.Status = $"Lỗi worker: {ex.Message}";
                            device.TypeColor = 1;
                            LogManager.Error(ex);
                        }
                        finally
                        {
                            try { client.AppClear(FacebookHander.Package(PlatformModel.Facebook)); } catch { }
                            client.Running = false;
                        }
                    }));
                }

                try { await Task.WhenAll(tasks); } catch { }
            }
            finally
            {
                Enable(true);
                fMain.StartTime = null;
            }
        }

        public void AddAccountThreadSafe(Account acc)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<Account>(AddAccountThreadSafe), acc);
                return;
            }
            if (IsDisposed) return;

            // Đăng ký hàng loạt: mỗi account 1 lần gọi → trước đây mỗi lần là 1 LoadAccounts()
            // đầy đủ (COUNT + stats + reset cache + rebind), reload N lần cho N account.
            // Debounce: dồn các lần gọi liên tiếp thành 1 reload sau khi ngừng 400ms.
            if (_reloadDebounceTimer == null)
            {
                _reloadDebounceTimer = new System.Windows.Forms.Timer { Interval = 400 };
                _reloadDebounceTimer.Tick += (_, __) =>
                {
                    _reloadDebounceTimer!.Stop();
                    if (!IsDisposed) _ = LoadAccounts();
                };
            }
            _reloadDebounceTimer.Stop();
            _reloadDebounceTimer.Start();
        }

        private void select4_SelectedIndexChanged(object sender, IntEventArgs e)
        {
    //        var rows = _jobHistoryContext.GetJobTotals(_platform, select4.Text.Trim(), DateTime.Now.ToString("dd/MM/yyyy"));

    //        if (!rows.Any())
    //        {
    //            return;
    //        }
    //        int today = 0;
    //        int success = 0;
    //        int fail = 0;
    //        foreach (var row in rows)
    //        {
    //            today += row.Value;
    //            if (row.Key.Contains("_skip"))
    //            {
    //                fail += row.Value;
    //            }
    //            else
    //            {
    //                success += row.Value;
    //            }
    //        }
    //        var grouped = rows
    //.GroupBy(kv => kv.Key.EndsWith("_skip")
    //                ? kv.Key.Replace("_skip", "")
    //                : kv.Key)
    //.ToDictionary(
    //    g => g.Key,
    //    g => new
    //    {
    //        Success = g.Where(x => !x.Key.EndsWith("_skip")).Sum(x => x.Value),
    //        Skip = g.Where(x => x.Key.EndsWith("_skip")).Sum(x => x.Value)
    //    });
    //        foreach (var kv in grouped)
    //        {
    //            string key = kv.Key;
    //            string displayKey = char.ToUpper(key[0]) + key.Substring(1).ToLower();
    //            string text = $"{displayKey}: {kv.Value.Success}/{kv.Value.Skip}";
    //            foreach (ToolStripMenuItem item in toolStripDropDownButton1.DropDownItems)
    //            {
    //                string name = item.Name.Split("_").First().ToLower();
    //                if (name != kv.Key) continue;
    //                item.Text = text;
    //                ControlHelper.SetToolStripMenuItemTextSafe(item, text);
    //                break;
    //            }
    //        }
    //        ControlHelper.SetToolStripLabelTextSafe(toolStripLabel16, today.ToMoneyString());
    //        ControlHelper.SetToolStripMenuItemTextSafe(JobTotal_toolStripMenuItem, $"Job Total: {success}/{fail}");
        }

        private void button8_Click(object sender, EventArgs e)
        {
            try { Globals.CancellationTokenSource?.Cancel(); } catch { }
            FlushStatusesToDb();

            // Force stop tất cả ADBClient đang chạy: set Running=false để ThrowIfStopped()
            // throw ở vòng lặp tiếp theo trong ADB sync calls → script bubble up và thoát ngay.
            foreach (var c in _activeClients)
            {
                try { c.Running = false; } catch { }
            }

            button8.Enabled = false;
            button8.Text    = "Đang dừng…";

            AntdHelper.MsgInfo(_form, "Đang dừng các tác vụ đang chạy…");
        }

        private async void select2_SelectedIndexChanged(object sender, IntEventArgs e)
        {
            _stateFilterExtra = BuildStateFilterSql(select2.Text);
            _scopedIdList = null;
            await ReloadFromDbAsync();
        }

        private async void CboFilterAccount_SelectedValueChanged(object sender, AntdUI.ObjectsEventArgs e)
        {
            var selected = e.Value?
                .Select(v => v?.ToString() ?? "")
                .Where(v => !string.IsNullOrEmpty(v))
                .ToList() ?? new List<string>();

            if (selected.Count == 0)
            {
                _cboFilterExtra = "";
                _scopedIdList = null;
                await ReloadFromDbAsync();
                return;
            }

            if (NeedsScopedIdList(selected))
            {
                string baseWhere = _listWhere + _stateFilterExtra;
                var slim = await Task.Run(() =>
                    _accountContext.GetListSlimForFilter(baseWhere, EffectiveParams()));
                var filtered = await Task.Run(() => ApplyAccountFilters(slim, selected));
                _scopedIdList = filtered.Select(a => a.Id).ToList();
                _cboFilterExtra = "";
            }
            else
            {
                _scopedIdList = null;
                _cboFilterExtra = BuildCboFilterSql(selected) ?? "";
            }
            await ReloadFromDbAsync();
        }

        private static List<Account> ApplyAccountFilters(IReadOnlyList<Account> source, IReadOnlyList<string> filters)
        {
            var result = new List<Account>(source);
            for (int f = 0; f < filters.Count; f++)
            {
                var filter = filters[f];
                result = filter switch
                {
                    "LIVE" => result.Where(x => x.State.Equals("live", StringComparison.OrdinalIgnoreCase)).ToList(),
                    "DIE" => result.Where(x => x.State.Equals("die", StringComparison.OrdinalIgnoreCase)).ToList(),
                    "Chưa xác định" => result.Where(x => string.IsNullOrEmpty(x.State)).ToList(),
                    "Đang chạy" => result.Where(x => x.Running).ToList(),
                    "Lỗi" => result.Where(x => x.Status.Contains("Lỗi", StringComparison.OrdinalIgnoreCase)).ToList(),
                    "Đăng xuất" => result.Where(x => x.Status.Contains("Đăng xuất", StringComparison.OrdinalIgnoreCase)).ToList(),
                    "Captcha" => result.Where(x => x.Status.Contains("Captcha", StringComparison.OrdinalIgnoreCase)).ToList(),
                    "Bị chặn" => result.Where(x => x.Status.Contains("chặn", StringComparison.OrdinalIgnoreCase)).ToList(),
                    "Đã dừng" => result.Where(x => x.Status.Contains("Đã dừng", StringComparison.OrdinalIgnoreCase)).ToList(),
                    "Có trạng thái" => result.Where(x => !string.IsNullOrEmpty(x.Status)).ToList(),
                    "Chưa có trạng thái" => result.Where(x => string.IsNullOrEmpty(x.Status)).ToList(),
                    "Tên tiếng Việt" => result.Where(x => IsVietnameseName(x.FullName)).ToList(),
                    "Tên tiếng Anh" => result.Where(x => !string.IsNullOrEmpty(x.FullName) && !IsVietnameseName(x.FullName)).ToList(),
                    "UID đầu 6" => result.Where(x => x.Uid.StartsWith("6")).ToList(),
                    "UID đầu 1" => result.Where(x => x.Uid.StartsWith("1")).ToList(),
                    "Tương tác hôm nay" => result.Where(x => GetInteractionDate(x.RecentInteraction) == DateTime.Today).ToList(),
                    "Tương tác hôm qua" => result.Where(x => GetInteractionDate(x.RecentInteraction) == DateTime.Today.AddDays(-1)).ToList(),
                    "Chưa tương tác" => result.Where(x => string.IsNullOrEmpty(x.RecentInteraction)).ToList(),
                    _ when filter.StartsWith("Nhóm: ") && filter.Length >= 6 => result.Where(x => x.NameFolder == filter.Substring(6)).ToList(),
                    _ => result
                };
            }
            return result;
        }

        private void ApplyFilterResult(List<Account> accounts)
        {
            _scopedIdList = accounts?.Select(a => a.Id).ToList();
            _cboFilterExtra = "";
            _ = ReloadFromDbAsync();
        }

        private async void ShowUpdateFieldPopup(string fieldLabel)
        {
            if (!HasAnyChecked())
            {
                AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox dòng cần cập nhật.");
                return;
            }
            int checkedCount = _cachedCheckedCount;

            var inputTxt = new AntdUI.Input
            {
                PlaceholderText = $"Nhập {fieldLabel} mới...",
                Height = 36,
                Dock = DockStyle.Bottom,
            };

            var contentPanel = new System.Windows.Forms.Panel { Width = 380, Height = 72, Padding = new Padding(0) };
            var lblInfo = new AntdUI.Label
            {
                Text = $"Cập nhật {fieldLabel} cho {checkedCount} tài khoản",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new System.Drawing.Font("Segoe UI", 9.5f)
            };
            contentPanel.Controls.Add(inputTxt);
            contentPanel.Controls.Add(lblInfo);

            var result = AntdUI.Modal.open(new AntdUI.Modal.Config(_form, $"Cập nhật {fieldLabel}", (Control)contentPanel)
            {
                OkText = "Cập nhật",
                CancelText = "Hủy",
                Width = 440,
            });

            if (result != DialogResult.OK) return;

            string value = inputTxt.Text;
            await MutateCheckedAndSaveAsync(acc =>
            {
                switch (fieldLabel)
                {
                    case "Pass":              acc.Password = value; break;
                    case "2FA":               acc.TowFA = value; break;
                    case "Cookie":            acc.Cookie = value; break;
                    case "Mail":              acc.Email = value; break;
                    case "Pass mail":         acc.PassMail = value; break;
                    case "Mail recover":      acc.EmailAddress = value; break;
                    case "Pass mail recover": acc.PassPrivateEmailAddress = value; break;
                    case "Phone":             acc.Phone = value; break;
                    case "Ngày sinh":         acc.Birthday = value; break;
                    case "User agent":        acc.UserAgent = value; break;
                    case "Ghi chú":           acc.Note = value; break;
                }
            }, null);
        }

        // Map: tên hiển thị trong combobox → DataPropertyName để truyền vào CopyFormat
        private static readonly (string Label, string PropName)[] _copyFields = new[]
        {
            ("Uid",               nameof(Account.Uid)),
            ("Pass",              nameof(Account.Password)),
            ("Token",             nameof(Account.Token)),
            ("2FA",               nameof(Account.TowFA)),
            ("Cookie",            nameof(Account.Cookie)),
            ("Proxy",             nameof(Account.Proxy)),
            ("Name",              nameof(Account.FullName)),
            ("Phone",             nameof(Account.Phone)),
            ("Mail",              nameof(Account.Email)),
            ("Pass Mail",         nameof(Account.PassMail)),
            ("Mail Client Id",    nameof(Account.MailClientId)),
            ("Mail Refresh Token",nameof(Account.MailRefreshToken)),
            ("Mail Recover",      nameof(Account.EmailAddress)),
            ("Pass Mail Recover", nameof(Account.PassPrivateEmailAddress)),
            ("UserAgent",         nameof(Account.UserAgent)),
        };

        private async void ShowCopyCustomFormatDialog()
        {
            const int SLOTS       = 10;
            const int COMBO_W     = 120;
            const int COMBO_H     = 24;
            const int SEP_W       = 16;
            const int PAD         = 24;

            // Form vừa đủ chứa 10 combo + 9 separator + padding 2 bên
            int formW = SLOTS * COMBO_W + (SLOTS - 1) * SEP_W + PAD * 2 + 16; // +16 border
            int formH = 185;

            using var dlg = new Form
            {
                Text            = "Cấu hình copy tài khoản",
                Width           = formW,
                Height          = formH,
                FormBorderStyle = FormBorderStyle.FixedSingle,
                StartPosition   = FormStartPosition.CenterParent,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = Color.White
            };

            // ── Header ──────────────────────────────────────────────
            var lblTitle = new System.Windows.Forms.Label
            {
                Text      = "Chọn định dạng cần copy",
                Left      = 0, Top = 12, Width = formW - 16, Height = 22,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var lblHint = new System.Windows.Forms.Label
            {
                Text      = "Auto sẽ tự động lưu lại định dạng copy sau cùng để sử dụng nhanh cho lần copy tiếp theo",
                Left      = 0, Top = 36, Width = formW - 16, Height = 18,
                Font      = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(120, 120, 120),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ── Divider ──────────────────────────────────────────────
            var div = new System.Windows.Forms.Panel
            {
                Left = 0, Top = 58, Width = formW - 16, Height = 1,
                BackColor = Color.FromArgb(220, 220, 220)
            };

            // ── ComboBox row ─────────────────────────────────────────
            var combos   = new List<ComboBox>();
            var labels   = _copyFields.Select(f => f.Label).Prepend("").ToArray(); // item rỗng ở đầu
            int comboTop = 70;
            int x        = PAD;

            // Load cấu hình copy đã lưu của nền tảng hiện tại (nếu có).
            // Giữ nguyên slot rỗng (None) ở giữa để khôi phục đúng vị trí trước đó.
            var savedRaw = SettingsTool.GetSettings(CopyFormatSettingName).GetValue(CopyFormatKey);
            var savedProps = string.IsNullOrEmpty(savedRaw)
                ? new List<string>()
                : savedRaw.Split('|').ToList();

            for (int i = 0; i < SLOTS; i++)
            {
                if (i > 0)
                {
                    var sep = new System.Windows.Forms.Label
                    {
                        Text      = "|",
                        Left      = x, Top = comboTop,
                        Width     = SEP_W, Height = COMBO_H,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font      = new Font("Segoe UI", 10f),
                        ForeColor = Color.FromArgb(180, 180, 180)
                    };
                    dlg.Controls.Add(sep);
                    x += SEP_W;
                }

                var cbo = new ComboBox
                {
                    Left          = x, Top = comboTop,
                    Width         = COMBO_W, Height = COMBO_H,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font          = new Font("Segoe UI", 8.5f)
                };
                cbo.Items.AddRange(labels.Cast<object>().ToArray());

                // Khôi phục lựa chọn cũ: slot i dùng prop thứ i trong savedProps
                string savedLabel = "";
                if (i < savedProps.Count)
                {
                    var match = _copyFields.FirstOrDefault(f => f.PropName == savedProps[i]);
                    if (!string.IsNullOrEmpty(match.Label)) savedLabel = match.Label;
                }
                int idx = string.IsNullOrEmpty(savedLabel) ? 0 : Array.IndexOf(labels, savedLabel);
                cbo.SelectedIndex = idx >= 0 ? idx : 0;

                combos.Add(cbo);
                dlg.Controls.Add(cbo);
                x += COMBO_W;
            }

            // Chống trùng: khi combo A chọn giá trị đã có ở combo B → reset combo B
            foreach (var cbo in combos)
            {
                cbo.SelectedIndexChanged += (s, _) =>
                {
                    if (s is not ComboBox changed) return;
                    string picked = changed.SelectedItem?.ToString() ?? "";
                    if (string.IsNullOrEmpty(picked)) return; // item rỗng — không reset combo khác
                    foreach (var other in combos)
                    {
                        if (other != changed && other.SelectedItem?.ToString() == picked)
                            other.SelectedIndex = -1;
                    }
                };
            }

            // ── Buttons ──────────────────────────────────────────────
            int btnTop = comboTop + COMBO_H + 16;
            int btnW   = 100, btnH = 30;
            int totalBtnW = btnW * 2 + 12;
            int btnLeft   = (formW - 16 - totalBtnW) / 2;

            var btnCopy = new System.Windows.Forms.Button
            {
                Text      = "Copy",
                Left      = btnLeft, Top = btnTop,
                Width     = btnW, Height = btnH,
                BackColor = Color.FromArgb(24, 144, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            btnCopy.FlatAppearance.BorderSize = 0;

            var btnClose = new System.Windows.Forms.Button
            {
                Text      = "Đóng",
                Left      = btnLeft + btnW + 12, Top = btnTop,
                Width     = btnW, Height = btnH,
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9f),
                DialogResult = DialogResult.Cancel
            };
            btnClose.FlatAppearance.BorderSize = 0;

            dlg.Controls.AddRange(new Control[] { lblTitle, lblHint, div, btnCopy, btnClose });
            dlg.AcceptButton = btnCopy;
            dlg.CancelButton = btnClose;

            if (dlg.ShowDialog() != DialogResult.OK) return;

            // Lấy DataPropertyName từ label đã chọn
            var selectedProps = combos
                .Select(c => c.SelectedItem?.ToString() ?? "")
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(label => _copyFields.FirstOrDefault(f => f.Label == label).PropName)
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            if (!selectedProps.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng chọn ít nhất một trường."); return; }

            // Lưu cấu hình ngay sau khi user xác nhận OK, trước cả auth check.
            // Lý do: định dạng cột không phải dữ liệu nhạy cảm — nếu user bấm Copy rồi
            // hủy ở popup xác thực, lần sau mở lại vẫn cần giữ nguyên lựa chọn của họ.
            SaveCopyFormat(combos);

            bool needsAuth = selectedProps.Any(p => p == nameof(Account.Password) || p == nameof(Account.TowFA));
            if (needsAuth && !VerifySubdyPassword()) return;

            await CopyCheckedAsync(string.Join("|", selectedProps));
        }

        // Lưu thứ tự 10 slot (kể cả slot rỗng) theo nền tảng hiện tại
        // để mở lại dialog lần sau giữ nguyên vị trí đã chọn.
        private void SaveCopyFormat(List<ComboBox> combos)
        {
            var slotProps = combos
                .Select(c => c.SelectedItem?.ToString() ?? "")
                .Select(label => string.IsNullOrEmpty(label)
                    ? ""
                    : _copyFields.FirstOrDefault(f => f.Label == label).PropName ?? "")
                .ToList();
            var cfg = SettingsTool.GetSettings(CopyFormatSettingName);
            cfg.AddOrUpdateProperty(CopyFormatKey, string.Join("|", slotProps));
            SettingsTool.UpdateSetting(CopyFormatSettingName);
        }

        private string CopyFormatSettingName => $"CopyFormat_{_platform}";
        private const string CopyFormatKey = "Slots";

        /// <summary>
        /// Hiển thị popup nhập mật khẩu Subdy để xác thực trước khi copy dữ liệu nhạy cảm.
        /// Trả về true nếu xác thực thành công.
        /// </summary>
        private bool VerifySubdyPassword()
        {
            var user = Globals.User;
            // Bỏ check "Chưa đăng nhập tài khoản QN": nếu chưa đăng nhập thì bỏ qua bước
            // xác thực và cho phép thao tác luôn (không chặn, không cảnh báo).
            if (user == null) return true;

            const int W = 370, H = 200;
            const int PAD = 20;

            using var dlg = new Form
            {
                Text            = "Xác thực",
                Width           = W,
                Height          = H,
                FormBorderStyle = FormBorderStyle.FixedSingle,
                StartPosition   = FormStartPosition.CenterParent,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = Color.White
            };

            // Header xanh
            var pnlTop = new System.Windows.Forms.Panel
            {
                Left = 0, Top = 0, Width = W, Height = 60,
                BackColor = Color.FromArgb(24, 144, 255)
            };
            var lblTitle = new System.Windows.Forms.Label
            {
                Text      = "🔐  Xác thực bảo mật",
                Left      = 0, Top = 0, Width = W, Height = 60,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlTop.Controls.Add(lblTitle);

            // Label
            var lblUser = new System.Windows.Forms.Label
            {
                Text      = $"Nhập mật khẩu tài khoản  [{user.UserName}]:",
                Left      = PAD, Top = 72, Width = W - PAD * 2, Height = 20,
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            // TextBox mật khẩu
            var txtPass = new TextBox
            {
                Left                  = PAD, Top = 96, Width = W - PAD * 2 - 16, Height = 24,
                UseSystemPasswordChar = true,
                Font                  = new Font("Segoe UI", 10f),
                BorderStyle           = BorderStyle.FixedSingle
            };

            // Buttons — tọa độ tuyệt đối, căn giữa
            int btnW = 110, btnH = 30, btnTop = 135, gap = 10;
            int totalW = btnW * 2 + gap;
            int btnLeft = (W - 16 - totalW) / 2;

            var btnOk = new System.Windows.Forms.Button
            {
                Text      = "Xác nhận",
                Left      = btnLeft, Top = btnTop, Width = btnW, Height = btnH,
                BackColor = Color.FromArgb(24, 144, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;

            var btnCancel = new System.Windows.Forms.Button
            {
                Text      = "Hủy",
                Left      = btnLeft + btnW + gap, Top = btnTop, Width = btnW, Height = btnH,
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9f),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            dlg.Controls.AddRange(new Control[] { pnlTop, lblUser, txtPass, btnOk, btnCancel });
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;
            dlg.Shown += (_, __) => txtPass.Focus();

            if (dlg.ShowDialog() != DialogResult.OK) return false;

            if (txtPass.Text.Trim() == user.Password) return true;

            AntdHelper.MsgError(_form, "Mật khẩu không đúng.");
            return false;
        }

        private static bool IsVietnameseName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            // Ký tự đặc trưng tiếng Việt (có dấu)
            const string vietnameseChars = "àáảãạăắằẳẵặâấầẩẫậèéẻẽẹêếềểễệìíỉĩịòóỏõọôốồổỗộơớờởỡợùúủũụưứừửữựỳýỷỹỵđ";
            return name.Any(c => vietnameseChars.Contains(char.ToLower(c)));
        }

        private static DateTime? GetInteractionDate(string recentInteraction)
        {
            if (string.IsNullOrEmpty(recentInteraction)) return null;
            // Chỉ lấy phần ngày, bỏ giờ phút giây
            string dateOnly = recentInteraction.Split(' ')[0];
            if (DateTime.TryParseExact(dateOnly, new[] { "dd/MM/yyyy", "yyyy-MM-dd", "d/M/yyyy", "MM/dd/yyyy" },
                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var date))
                return date.Date;
            if (DateTime.TryParse(recentInteraction, out var fallback))
                return fallback.Date;
            return null;
        }

        private void UpdateFilterFolders(List<string?> folderNames)
        {
            // Xóa nhóm cũ (items sau "Chưa tương tác")
            int cutIndex = -1;
            for (int i = 0; i < cboFilterAccount.Items.Count; i++)
            {
                if (cboFilterAccount.Items[i]?.ToString() == "Chưa tương tác")
                {
                    cutIndex = i + 1;
                    break;
                }
            }
            if (cutIndex > 0 && cutIndex < cboFilterAccount.Items.Count)
            {
                while (cboFilterAccount.Items.Count > cutIndex)
                    cboFilterAccount.Items.RemoveAt(cboFilterAccount.Items.Count - 1);
            }

            // Thêm nhóm tài khoản
            if (folderNames.Count > 0)
            {
                foreach (var name in folderNames)
                {
                    if (!string.IsNullOrEmpty(name))
                        cboFilterAccount.Items.Add("Nhóm: " + name);
                }
            }
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            new fQuanLyKichBan(_platform).ShowDialog();
            await LoadJobService();
            _cachedScripts.Clear();
            InvalidateMenuStrip();
            // Cập nhật ngay combobox kịch bản trên toolbar (không cần tắt/mở lại tool).
            Facebook_Farm_NewFeed_PostStory.Utils.Design.SsaTheme.RefreshScriptSelect(this);
        }
        public async void fMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                await LoadAccounts();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Space)
            {
                foreach (var account in SelectedAccounts)
                {
                    if (_checkAllActive)
                    {
                        _checkAllExceptions ??= new HashSet<Guid>();
                        if (_checkAllExceptions.Contains(account.Id))
                        {
                            _checkAllExceptions.Remove(account.Id);
                            _cachedCheckedCount++;
                        }
                        else
                        {
                            _checkAllExceptions.Add(account.Id);
                            _cachedCheckedCount--;
                        }
                    }
                    else
                    {
                        bool was = IsAccountChecked(account);
                        if (!UseSparseCheckedLookup)
                            account.SetCheckedSilently(!was);
                        SyncCheckedState(account, !was);
                        _cachedCheckedCount = _checkedAccounts.Count;
                    }
                }
                RefreshCheckedVisual(cachedCountKnown: true, repaintGrid: false);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _form.btn_setting_Click(null, null);
            }
        }

        // ── Chức năng: Check live / Kiểm tra avatar ────────────────────────────
        private async Task RunCheckLiveAsync(bool checkAvatarMode)
        {
            if (!HasAnyChecked()) { AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox tài khoản cần kiểm tra."); return; }
            var targets = await Task.Run(() => MaterializeCheckedForUi());
            if (targets.Count == 0) { AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox tài khoản cần kiểm tra."); return; }

            int live = 0, total = targets.Count, done = 0;
            string spinLabel = checkAvatarMode ? "Kiểm tra avatar..." : "Check live...";

            await AntdUI.Spin.open(this, spinLabel, async cfg =>
            {
                using var sem = new SemaphoreSlim(10, 10);
                var tasks = targets.Select(async acc =>
                {
                    await sem.WaitAsync();
                    try
                    {
                        bool result = await FacebookRequest.CheckLive(acc.Uid);
                        if (result)
                        {
                            if (checkAvatarMode) { acc.State = "Có avatar"; Interlocked.Increment(ref live); }
                            else { acc.State = "LIVE"; Interlocked.Increment(ref live); }
                        }
                        else
                        {
                            acc.State = checkAvatarMode ? "Không có avatar" : "DIE";
                        }
                        int d = Interlocked.Increment(ref done);
                        cfg.Text = $"{spinLabel} {d} / {total}";
                    }
                    finally { sem.Release(); }
                }).ToList();
                await Task.WhenAll(tasks);
            });

            await Task.Run(() => _accountContext.Update(targets));

            if (checkAvatarMode)
                AntdHelper.MsgSuccess(_form, $"Có avatar: {live} / {total}");
            else
                AntdHelper.MsgSuccess(_form, $"LIVE: {live} / {total}  |  DIE: {total - live}");
        }

        // ── Chức năng: Kiểm tra cookie ─────────────────────────────────────────
        private async Task RunCheckCookieAsync()
        {
            if (!HasAnyChecked()) { AntdHelper.MsgWarn(_form, "Không có tài khoản nào đã tick có cookie."); return; }
            var targets = await Task.Run(() => MaterializeCheckedForUi()
                .Where(a => !string.IsNullOrEmpty(a.Cookie)).ToList());
            if (targets.Count == 0) { AntdHelper.MsgWarn(_form, "Không có tài khoản nào đã tick có cookie."); return; }

            int live = 0, total = targets.Count, done = 0;

            await AntdUI.Spin.open(this, "Kiểm tra cookie...", async cfg =>
            {
                using var sem = new SemaphoreSlim(10, 10);
                var tasks = targets.Select(async acc =>
                {
                    await sem.WaitAsync();
                    try
                    {
                        bool result = await FacebookRequest.CheckLive(acc.Uid);
                        acc.State = result ? "LIVE" : "DIE";
                        if (result) Interlocked.Increment(ref live);
                        int d = Interlocked.Increment(ref done);
                        cfg.Text = $"Kiểm tra cookie... {d} / {total}";
                    }
                    finally { sem.Release(); }
                }).ToList();
                await Task.WhenAll(tasks);
            });

            await Task.Run(() => _accountContext.Update(targets));
            AntdHelper.MsgSuccess(_form, $"Cookie LIVE: {live} / {total}  |  DIE: {total - live}");
        }

        // ── Chức năng: Check name VN ───────────────────────────────────────────
        private async void RunCheckNameVN()
        {
            if (!HasAnyChecked()) { AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox tài khoản cần kiểm tra."); return; }
            var (vnCount, total) = await Task.Run(() =>
            {
                var targets = MaterializeCheckedForUi();
                return (targets.Count(a => IsVietnameseName(a.FullName)), targets.Count);
            });
            if (total == 0) { AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox tài khoản cần kiểm tra."); return; }
            AntdHelper.MsgSuccess(_form, $"Tên tiếng Việt: {vnCount} / {total}");
        }

        // ── Chức năng: Kiểm tra live proxy ────────────────────────────────────
        private async Task RunCheckProxyAsync()
        {
            if (!HasAnyChecked()) { AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox tài khoản cần kiểm tra."); return; }
            var targets = await Task.Run(() => MaterializeCheckedForUi());
            if (targets.Count == 0) { AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox tài khoản cần kiểm tra."); return; }

            var noProxy = targets.Where(a => string.IsNullOrEmpty(a.Proxy)).ToList();
            var withProxy = targets.Where(a => !string.IsNullOrEmpty(a.Proxy)).ToList();
            foreach (var a in noProxy) a.State = "Không có proxy";

            int live = 0, die = 0, total = withProxy.Count, done = 0;
            if (total > 0)
            {
                await AntdUI.Spin.open(this, "Kiểm tra proxy...", async cfg =>
                {
                    using var sem = new SemaphoreSlim(10, 10);
                    var tasks = withProxy.Select(async acc =>
                    {
                        await sem.WaitAsync();
                        try
                        {
                            bool isLive = await CheckProxyLiveAsync(acc.Proxy);
                            acc.State = isLive ? "Proxy LIVE" : "Proxy DIE";
                            if (isLive) Interlocked.Increment(ref live);
                            else Interlocked.Increment(ref die);
                            int d = Interlocked.Increment(ref done);
                            cfg.Text = $"Kiểm tra proxy... {d} / {total}";
                        }
                        finally { sem.Release(); }
                    }).ToList();
                    await Task.WhenAll(tasks);
                });
            }

            await Task.Run(() => _accountContext.Update(targets));
            string msg = $"Proxy LIVE: {live} / {total}  |  DIE: {die}";
            if (noProxy.Any()) msg += $"\nKhông có proxy: {noProxy.Count}";
            AntdHelper.MsgSuccess(_form, msg);
        }

        private static async Task<bool> CheckProxyLiveAsync(string proxy)
        {
            try
            {
                var handler = new System.Net.Http.HttpClientHandler();
                if (!string.IsNullOrWhiteSpace(proxy))
                {
                    var uri = new Uri(proxy.StartsWith("http") ? proxy : "http://" + proxy);
                    handler.Proxy = new System.Net.WebProxy(uri);
                    handler.UseProxy = true;
                }
                using var client = new System.Net.Http.HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
                var resp = await client.GetAsync("https://api64.ipify.org/?format=json");
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // ══════════════════════════════════════════════════════════════════════
        // 1. LOGIN PHONE
        // ══════════════════════════════════════════════════════════════════════
        // Giống RunningThread + MainService.RunAsync nhưng dừng trước bước chạy kịch bản:
        //   ChangeLanguage → TurnOnADBKeyboard → new MainService → ConnectAndPrepare(false)
        //   → [lấy account] → ConnectAndPrepare(true) → RestoreFacebook → Login → ExtractAuth
        private async Task LoginPhoneAsync()
        {
            if (fMain.StartTime != null)
            {
                AntdHelper.MsgWarn(_form, "Đang có job chạy. Vui lòng dừng job trước khi đăng nhập phone.");
                return;
            }

            var selected = await Task.Run(() => GetCheckedAccountsForJob());
            if (selected.Count == 0)
            {
                AntdHelper.MsgWarn(_form, "Vui lòng chọn ít nhất 1 tài khoản cần đăng nhập.");
                return;
            }
            if (!SeleceterDevice()) return;

            var devices = DeviceServices.DeviceModels.Where(x => x.Checked).ToList();
            if (!devices.Any()) { AntdHelper.MsgWarn(_form, "Chưa chọn thiết bị."); return; }

            ConfigModel model = null!;
            if (InvokeRequired)
                Invoke(new Action(() => model = GetConfigModel()));
            else
                model = GetConfigModel();
            if (model == null) return;

            try
            {
                Enable(false);
                fMain.StartTime = DateTime.Now;
                Globals.CancellationTokenSource = new CancellationTokenSource();
                var ct = Globals.CancellationTokenSource.Token;

                int accIndex = 0;
                var tasks = new List<Task>();

                foreach (var device in devices)
                {
                    if (accIndex >= selected.Count) break;
                    var acc = selected[accIndex++];
                    var dev = device;
                    tasks.Add(Task.Run(async () =>
                    {
                        // Giống RunningThread: tạo client, đăng ký activeClients
                        var client = new ADBClient(dev);
                        _activeClients.Add(client);
                        try
                        {
                            // Bước 1-2: giống RunningThread
                            var changeLanguage = new ChangeLanguageService(client);
                            await changeLanguage.Change("en", "US");
                            await client.TurnOnADBKeyboard();

                            // Bước 3: tạo MainService giống RunningThread
                            var svc = new MainService(_platform, client, model, ct);

                            // Bước 4: ConnectAndPrepare(false) — kết nối thiết bị + check internet
                            var r1 = await svc.ConnectAndPrepareDeviceAsync(false);
                            if (!r1.ok)
                            {
                                acc.Status = r1.noInternet ? "Lỗi: không có internet" : "Lỗi: không kết nối được thiết bị";
                                return;
                            }

                            // Bước 5: gán account (trong RunAsync gán sau GetAccount())
                            svc._account = acc;
                            acc.Running = true;

                            // Bước 6: ConnectAndPrepare(true) — đổi device info + proxy
                            var r2 = await svc.ConnectAndPrepareDeviceAsync(true);
                            if (!r2.ok)
                            {
                                acc.Status = r2.noInternet ? "Lỗi: không có internet sau đổi proxy" : "Lỗi: chuẩn bị thiết bị thất bại";
                                return;
                            }

                            // Bước 7: RestoreFacebook
                            if (!await svc.RestoreFacebookAsync())
                            {
                                acc.Status = "Lỗi: không restore được Facebook";
                                return;
                            }

                            // Bước 8: set Uid_Email giống RunAsync
                            int uidEmailIndex = model.SettingGeneral.GetIntType("comboBox1", 0);
                            acc.Uid_Email = uidEmailIndex == 1 ? acc.Email : acc.Uid;
                            acc.Uid_Email ??= acc.Email ?? acc.Uid;

                            // Bước 9: Login — throw SubdyExtension khi thất bại, return khi success
                            var ext = await svc._facebookService.Login(client, acc, ct, 400, svc);
                            acc.State  = ext.SubdyEnum == SubdyEnum.Success ? "LIVE" : ext.SubdyEnum.ToString();
                            acc.Status = ext.Message;

                            // Bước 10: ExtractAuth (lấy cookie/token) — dừng tại đây, không chạy kịch bản
                            if (ext.SubdyEnum == SubdyEnum.Success)
                                await svc.ExtractAndUpdateAuthenticationInfoAsync();
                        }
                        catch (SubdyExtension sex)
                        {
                            acc.State  = sex.SubdyEnum.ToString();
                            acc.Status = sex.Message;
                        }
                        catch (Exception ex)
                        {
                            acc.Status = $"Lỗi: {ex.Message}";
                        }
                        finally
                        {
                            acc.Running = false;
                            client.Running = false;
                        }
                        _accountContext.Update(acc);
                    }));
                }
                await Task.WhenAll(tasks);

                // Persist cột Status riêng qua UpdateStatusBatch — Update() generic ở trên
                // không ghi cột Status (Status là field runtime, chỉ flush qua UpdateStatusBatch
                // giống FlushStatusesToDb). Nếu thiếu bước này, LoadAccounts() nạp lại từ DB sẽ
                // thấy Status rỗng → cột "Trạng thái" trống sau khi đăng nhập/map account.
                try { _accountContext.UpdateStatusBatch(selected); }
                catch (Exception ex) { Sunny.Subdy.Common.Logs.LogManager.Error(ex); }

                _ = LoadAccounts();
                AntdHelper.MsgSuccess(_form, $"Đã đăng nhập {tasks.Count} tài khoản.");
            }
            finally
            {
                Enable(true);
                fMain.StartTime = null;
            }
        }

        // Tự động đăng nhập acc chưa có cookie/LIVE trước khi bắt đầu chạy job.
        // Phân phối acc lần lượt cho từng thiết bị (1 device - 1 acc cùng lúc).
        private async Task AutoLoginAccountsAsync(List<Account> accounts, List<DeviceModel> devices, CancellationToken ct)
        {
            if (accounts == null || accounts.Count == 0 || devices.Count == 0) return;
            var needLogin = accounts
                .Where(a => a != null && string.IsNullOrEmpty(a.Cookie) && !string.IsNullOrEmpty(a.Password))
                .ToList();
            if (needLogin.Count == 0) return;

            int accIdx = 0;
            while (accIdx < needLogin.Count && !ct.IsCancellationRequested)
            {
                var tasks = new List<Task>();
                foreach (var dev in devices)
                {
                    if (accIdx >= needLogin.Count || ct.IsCancellationRequested) break;
                    var acc = needLogin[accIdx++];
                    var device = dev;
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var client = new ADBClient(device);
                            var svc = new Sunny.Subd.Core.Facebook.FacebookService();
                            var ext = await svc.Login(client, acc, ct, 400, null!);
                            acc.State = ext.SubdyEnum == Sunny.Subd.Core.Models.SubdyEnum.Success ? "LIVE" : ext.SubdyEnum.ToString();
                            acc.Status = ext.SubdyEnum == Sunny.Subd.Core.Models.SubdyEnum.Success
                                ? "Đã tự đăng nhập"
                                : $"Auto login thất bại: {ext.Message}";
                            _accountContext.Update(acc);
                        }
                        catch (Exception ex) { acc.Status = $"Auto login lỗi: {ex.Message}"; }
                    }, ct));
                }
                if (tasks.Count > 0) await Task.WhenAll(tasks);
            }
            try { _accountContext.UpdateStatusBatch(needLogin); } catch { }
        }

        // ══════════════════════════════════════════════════════════════════════
        // 2. QUẢN LÝ BACKUP PROFILE / DEVICE
        // ══════════════════════════════════════════════════════════════════════
        private enum BackupType { Profile, Device, Both }
        private enum BackupOp   { Check, Delete, Copy }

        private string GetBackupDir(BackupType type)
        {
            var s = SettingsTool.GetSettings($"{nameof(fSettingDefault)}_{_platform}", true);
            if (type == BackupType.Profile)
                return s.GetValuesFromInputString("textBox3", System.IO.Path.Combine(AppContext.BaseDirectory, "Backup", "Profile", _platform));
            return s.GetValuesFromInputString("textBox2", System.IO.Path.Combine(AppContext.BaseDirectory, "Backup", "Device", _platform));
        }

        private string GetBackupFile(BackupType type, string uid)
            => System.IO.Path.Combine(GetBackupDir(type), $"{uid}.tar.gz");

        private void BackupAction(BackupType type, BackupOp op)
        {
            var selected = CheckedAccounts
                .Where(a => !string.IsNullOrEmpty(a.Uid))
                .ToList();

            if (!selected.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox tài khoản cần thực hiện."); return; }

            // Xác định danh sách (type, uid, path) cần xử lý
            var entries = new List<(BackupType t, string uid, string path)>();
            foreach (var acc in selected)
            {
                if (type == BackupType.Both)
                {
                    entries.Add((BackupType.Profile, acc.Uid, GetBackupFile(BackupType.Profile, acc.Uid)));
                    entries.Add((BackupType.Device,  acc.Uid, GetBackupFile(BackupType.Device,  acc.Uid)));
                }
                else
                {
                    entries.Add((type, acc.Uid, GetBackupFile(type, acc.Uid)));
                }
            }

            int total  = type == BackupType.Both ? selected.Count * 2 : selected.Count;
            int found  = entries.Count(e => System.IO.File.Exists(e.path));
            int notFound = total - found;

            switch (op)
            {
                case BackupOp.Check:
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"Tổng: {total} | Có: {found} | Không có: {notFound}");
                    sb.AppendLine(new string('─', 60));
                    foreach (var (t, uid, path) in entries)
                    {
                        bool exists = System.IO.File.Exists(path);
                        string label = type == BackupType.Both ? $"[{(t == BackupType.Profile ? "Profile" : "Device")}] " : "";
                        sb.AppendLine($"{(exists ? "✔" : "✘")} {label}{uid}");
                        if (exists) sb.AppendLine($"   {path}");
                    }
                    ShowBackupResultDialog($"Check backup {BackupTypeLabel(type)}", sb.ToString());
                    break;
                }

                case BackupOp.Delete:
                {
                    if (found == 0) { AntdHelper.MsgWarn(_form, $"Không có file backup nào để xóa ({notFound}/{total} không tồn tại)."); return; }
                    if (!AntdHelper.Confirm(_form, "Xác nhận xóa", $"Xóa {found}/{total} file backup?")) return;

                    int deleted = 0;
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"Đã xóa: {0} | Không tồn tại: {notFound}");
                    foreach (var (t, uid, path) in entries)
                    {
                        string label = type == BackupType.Both ? $"[{(t == BackupType.Profile ? "Profile" : "Device")}] " : "";
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                            deleted++;
                            sb.AppendLine($"✔ Đã xóa: {label}{uid}");
                        }
                        else
                        {
                            sb.AppendLine($"✘ Không tồn tại: {label}{uid}");
                        }
                    }
                    // Cập nhật dòng đầu
                    string result = sb.ToString();
                    result = result.Replace("Đã xóa: 0", $"Đã xóa: {deleted}");
                    ShowBackupResultDialog($"Xóa backup {BackupTypeLabel(type)}", result);
                    break;
                }

                case BackupOp.Copy:
                {
                    if (found == 0) { AntdHelper.MsgWarn(_form, $"Không có file backup nào để copy ({notFound}/{total} không tồn tại)."); return; }

                    using var fbd = new System.Windows.Forms.FolderBrowserDialog { Description = "Chọn thư mục đích để copy backup" };
                    if (fbd.ShowDialog() != DialogResult.OK) return;
                    string destDir = fbd.SelectedPath;

                    int copied = 0;
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"Đã copy: 0 | Không tồn tại: {notFound}");
                    foreach (var (t, uid, path) in entries)
                    {
                        string label = type == BackupType.Both ? $"[{(t == BackupType.Profile ? "Profile" : "Device")}] " : "";
                        if (System.IO.File.Exists(path))
                        {
                            string typeFolder = t == BackupType.Profile ? "Profile" : "Device";
                            string dest;
                            if (type == BackupType.Both)
                                dest = System.IO.Path.Combine(destDir, typeFolder, System.IO.Path.GetFileName(path));
                            else
                                dest = System.IO.Path.Combine(destDir, System.IO.Path.GetFileName(path));
                            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest)!);
                            System.IO.File.Copy(path, dest, overwrite: true);
                            copied++;
                            sb.AppendLine($"✔ Đã copy: {label}{uid}");
                        }
                        else
                        {
                            sb.AppendLine($"✘ Không tồn tại: {label}{uid}");
                        }
                    }
                    string result = sb.ToString();
                    result = result.Replace("Đã copy: 0", $"Đã copy: {copied}");
                    ShowBackupResultDialog($"Copy backup {BackupTypeLabel(type)}", result);
                    break;
                }
            }
        }

        private static string BackupTypeLabel(BackupType t) => t switch
        {
            BackupType.Profile => "profile",
            BackupType.Device  => "device",
            _                  => "profile và device"
        };

        private void ShowBackupResultDialog(string title, string content)
        {
            var txt = new AntdUI.Input
            {
                Multiline        = true,
                ReadOnly         = true,
                Text             = content,
                Dock             = DockStyle.Fill,
                Font             = new System.Drawing.Font("Consolas", 9f)
            };
            var panel = new System.Windows.Forms.Panel { Width = 520, Height = 340 };
            txt.Dock = DockStyle.Fill;
            panel.Controls.Add(txt);
            AntdUI.Modal.open(new AntdUI.Modal.Config(_form, title, (Control)panel)
            {
                OkText     = "Đóng",
                CancelText = "",
                Width      = 560,
            });
        }

        // ── Dọn dẹp backup dư thừa ───────────────────────────────────────────
        private void CleanupRedundantBackups()
        {
            var selectedUids = CheckedAccounts
                .Where(a => !string.IsNullOrEmpty(a.Uid))
                .Select(a => a.Uid!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!selectedUids.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox tài khoản cần giữ lại."); return; }

            string profileDir = GetBackupDir(BackupType.Profile);
            string deviceDir  = GetBackupDir(BackupType.Device);

            var toDelete = new List<string>();
            foreach (var dir in new[] { profileDir, deviceDir })
            {
                if (!System.IO.Directory.Exists(dir)) continue;
                foreach (var file in System.IO.Directory.GetFiles(dir, "*.tar.gz"))
                {
                    string uid = System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetFileNameWithoutExtension(file));
                    if (!selectedUids.Contains(uid))
                        toDelete.Add(file);
                }
            }

            if (!toDelete.Any()) { AntdHelper.MsgSuccess(_form, "Không có file dư thừa cần dọn."); return; }
            if (!AntdHelper.Confirm(_form, "Xác nhận dọn dẹp", $"Sẽ xóa {toDelete.Count} file backup dư thừa. Tiếp tục?")) return;

            int deleted = 0;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Đã xóa: 0/{toDelete.Count}");
            sb.AppendLine(new string('─', 60));
            foreach (var f in toDelete)
            {
                try { System.IO.File.Delete(f); deleted++; sb.AppendLine($"✔ Đã xóa: {f}"); }
                catch (Exception ex) { sb.AppendLine($"✘ Lỗi ({f}): {ex.Message}"); }
            }
            string result = sb.ToString().Replace("Đã xóa: 0", $"Đã xóa: {deleted}");
            ShowBackupResultDialog("Dọn dẹp backup dư thừa", result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // ĐỒNG BỘ TỪ TOOL KHÁC (MaxCare / FPlus / MetaMax)
        // ══════════════════════════════════════════════════════════════════════
        private void OpenSyncFromOtherTool(string tool)
        {
            try
            {
                var f = new fSyncOtherTool(tool, _platform);
                f.ShowDialog(_form);
                if (f.IsOk)
                {
                    _ = LoadFolders();
                    _ = LoadAccounts();
                }
            }
            catch (Exception ex)
            {
                AntdHelper.MsgError(_form, $"Lỗi mở đồng bộ {tool}: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // 3. LỌC TRÙNG TÀI KHOẢN (với xác nhận đưa vào thùng rác)
        // ══════════════════════════════════════════════════════════════════════
        private void FilterDuplicateAccounts()
        {
            var ids = _accountContext.GetDuplicateIds(EffectiveWhere, EffectiveParams());
            if (!ids.Any()) { AntdHelper.MsgSuccess(_form, "Không có tài khoản trùng uid."); return; }

            if (!AntdHelper.Confirm(_form, "Lọc trùng tài khoản",
                $"Tìm thấy {ids.Count} tài khoản trùng uid.\nĐưa {ids.Count} tài khoản này vào thùng rác?")) return;

            if (_accountContext.UpdateIsViewFalse(ids))
            {
                _ = LoadAccounts();
                AntdHelper.MsgSuccess(_form, $"Đã đưa {ids.Count} tài khoản trùng vào thùng rác.");
            }
            else AntdHelper.MsgError(_form, "Đã xảy ra lỗi khi cập nhật.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // 4. XÓA TK BỊ CHECKPOINT
        // ══════════════════════════════════════════════════════════════════════
        private void TrashCheckpointAccounts()
        {
            var ids = _accountContext.GetIdsInScope(
                EffectiveWhere + $" AND {nameof(Account.State)} LIKE 'CP%'", EffectiveParams());
            if (!ids.Any()) { AntdHelper.MsgWarn(_form, "Không có tài khoản bị checkpoint."); return; }
            if (!AntdHelper.Confirm(_form, "Xóa tk bị checkpoint",
                $"Đưa {ids.Count} tài khoản bị checkpoint vào thùng rác?")) return;
            if (_accountContext.UpdateIsViewFalse(ids))
            {
                _ = LoadAccounts();
                AntdHelper.MsgSuccess(_form, $"Đã đưa {ids.Count} tài khoản vào thùng rác.");
            }
            else AntdHelper.MsgError(_form, "Đã xảy ra lỗi.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // 5. XÓA TK BỊ CHẶN TƯƠNG TÁC
        // ══════════════════════════════════════════════════════════════════════
        private void TrashBlockedAccounts()
        {
            var ids = _accountContext.GetIdsInScope(
                EffectiveWhere + $" AND UPPER({nameof(Account.State)}) = 'BLOCK'", EffectiveParams());
            if (!ids.Any()) { AntdHelper.MsgWarn(_form, "Không có tài khoản bị chặn tương tác."); return; }
            if (!AntdHelper.Confirm(_form, "Xóa tk bị chặn tương tác",
                $"Đưa {ids.Count} tài khoản bị Block vào thùng rác?")) return;
            if (_accountContext.UpdateIsViewFalse(ids))
            {
                _ = LoadAccounts();
                AntdHelper.MsgSuccess(_form, $"Đã đưa {ids.Count} tài khoản vào thùng rác.");
            }
            else AntdHelper.MsgError(_form, "Đã xảy ra lỗi.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // 6. XÓA TK VÀO THÙNG RÁC (bôi đen)
        // ══════════════════════════════════════════════════════════════════════
        private void TrashSelectedAccounts()
        {
            var ids = CheckedAccounts
                .Where(a => a.IsView)
                .Select(a => a.Id)
                .ToList();
            if (!ids.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox tài khoản cần xóa vào thùng rác."); return; }
            int total = CheckedAccounts.Count();
            if (!AntdHelper.Confirm(_form, "Xóa vào thùng rác", $"Đưa {ids.Count}/{total} tài khoản vào thùng rác?")) return;
            if (_accountContext.UpdateIsViewFalse(ids))
            {
                _ = LoadAccounts();
                AntdHelper.MsgSuccess(_form, $"Đã đưa {ids.Count}/{total} tài khoản vào thùng rác.");
            }
            else AntdHelper.MsgError(_form, "Đã xảy ra lỗi.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // 7. XÓA TÀI KHOẢN VĨNH VIỄN (yêu cầu mật khẩu Subdy)
        // ══════════════════════════════════════════════════════════════════════
        private void DeleteAccountsPermanently()
        {
            var ids = CheckedAccounts
                .Select(a => a.Id)
                .ToList();
            if (!ids.Any()) { AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox tài khoản cần xóa vĩnh viễn."); return; }
            if (!AntdHelper.Confirm(_form, "Xóa vĩnh viễn",
                $"Bạn sắp xóa vĩnh viễn {ids.Count} tài khoản khỏi database.\nHành động này không thể hoàn tác. Tiếp tục?")) return;
            if (!VerifySubdyPassword()) return;
            if (_accountContext.DeleteByIds(ids))
            {
                _ = LoadAccounts();
                AntdHelper.MsgSuccess(_form, $"Đã xóa vĩnh viễn {ids.Count} tài khoản.");
            }
            else AntdHelper.MsgError(_form, "Đã xảy ra lỗi khi xóa.");
        }

        // Dropdown kịch bản (toolbar): apply NameScript cho các row đang được bôi đen (selected).
        // Nếu không có row nào được chọn thì áp dụng cho toàn bộ view hiện tại.
        public void ApplyScriptToAll(string scriptName)
        {
            if (AccountGridPerf.DiagnosticsEnabled)
                AccountGridPerf.LogLine($"[ApplyScriptToAll] script={scriptName}");
            if (string.IsNullOrEmpty(scriptName) || string.IsNullOrEmpty(_listWhere)) return;

            var selectedAccs = SelectedAccountsByRange();
            if (selectedAccs.Count > 0)
            {
                // Chỉ apply cho các row đang bôi đen
                var ids = selectedAccs.Select(a => a.Id).ToList();
                _accountContext.UpdateNameScriptByIds(ids, scriptName);
                if (_useWindowedCache)
                {
                    _cache?.InvalidateAll();
                    return;
                }
                var idSet = new HashSet<Guid>(ids);
                for (int i = 0; i < _accounts.Count; i++)
                {
                    if (idSet.Contains(_accounts[i].Id) && _accounts[i].NameScript != scriptName)
                        _accounts[i].NameScript = scriptName;
                }
            }
            else
            {
                // Không có row nào bôi đen → apply toàn bộ view
                _accountContext.UpdateNameScriptScoped(EffectiveWhere, EffectiveParams(), scriptName);
                if (_useWindowedCache)
                {
                    _cache?.InvalidateAll();
                    return;
                }
                for (int i = 0; i < _accounts.Count; i++)
                {
                    if (_accounts[i].NameScript != scriptName)
                        _accounts[i].NameScript = scriptName;
                }
            }

            if (dataGridView1.IsHandleCreated)
                dataGridView1.Invalidate(dataGridView1.DisplayRectangle);
        }

        // Dropdown "Tùy chọn": restore NameScript về giá trị gốc trong DB mà không reload toàn bộ grid.
        // Windowed cache: evict pages để grid repaint từ DB — không LoadAccounts() để tránh mất checked state.
        public void RestoreScriptsFromDb()
        {
            if (AccountGridPerf.DiagnosticsEnabled)
                AccountGridPerf.LogLine("[RestoreScriptsFromDb] windowed=" + _useWindowedCache);
            if (_useWindowedCache || _accounts == null)
            {
                // Windowed cache: không có list đầy đủ trong memory, chỉ evict cache hiển thị.
                _cache?.InvalidateAll();
                return;
            }

            // In-memory list: nạp lại NameScript từ DB cho từng account, không reload grid
            try
            {
                var ids = _accounts.Select(a => a.Id).ToList();
                var fromDb = _accountContext.GetByIdsBatched(ids);
                var map = fromDb.ToDictionary(a => a.Id, a => a.NameScript);
                for (int i = 0; i < _accounts.Count; i++)
                {
                    if (map.TryGetValue(_accounts[i].Id, out var name))
                        _accounts[i].NameScript = name;
                }
                if (dataGridView1.IsHandleCreated)
                    dataGridView1.Invalidate(dataGridView1.DisplayRectangle);
            }
            catch
            {
                // Fallback: reload toàn bộ
                if (dataGridView1.IsHandleCreated && dataGridView1.RowCount > 0)
                    _preserveScrollRow = dataGridView1.FirstDisplayedScrollingRowIndex;
                _ = LoadAccounts();
            }
        }

        // Context menu "Kịch bản": cập nhật NameScript cho các account đã tick checkbox,
        // không reload toàn bộ grid để tránh mất selection/checked state.
        private async Task SetScriptForCheckedAsync(string scriptName)
        {
            var list = await Task.Run(() => MaterializeCheckedForUi());
            if (list.Count == 0)
            {
                AntdHelper.MsgWarn(_form, "Vui lòng tick checkbox tài khoản cần đổi kịch bản.");
                return;
            }

            var ids = list.Select(a => a.Id).ToList();
            await Task.Run(() => _accountContext.UpdateNameScriptByIds(ids, scriptName));

            if (_useWindowedCache)
            {
                _cache?.InvalidateAll();
            }
            else if (_accounts != null)
            {
                var idSet = new HashSet<Guid>(ids);
                for (int i = 0; i < _accounts.Count; i++)
                {
                    if (idSet.Contains(_accounts[i].Id))
                        _accounts[i].NameScript = scriptName;
                }
                if (dataGridView1.IsHandleCreated)
                    dataGridView1.Invalidate(dataGridView1.DisplayRectangle);
            }

            string label = string.IsNullOrEmpty(scriptName) ? "[Không cần kịch bản]" : scriptName;
            AntdHelper.MsgSuccess(_form, $"Đã cập nhật kịch bản \"{label}\" cho {list.Count} tài khoản.");
        }

        #region ==== Tour targets ====
        // Nhóm (folder): nút quản lý nhóm là AntdUI.Button được tạo động bởi SsaTheme
        public Control TourBtnFolderManager => this.Controls.Find("ssaBtnFolderMgr", true).FirstOrDefault();
        public Control TourCboGroup => select1;                 // Dropdown chọn nhóm
        // Kịch bản & cài đặt
        public Control TourBtnJobSettings => button4;           // Cài đặt jobs
        public Control TourBtnGeneralSettings => button5;       // Cài đặt chung
        public Control TourBtnInteract => button6;              // Tương tác
        // Chạy / thêm tài khoản / tìm kiếm
        public Control TourBtnRun => button7;                   // Chạy
        public Control TourBtnStop => button8;                  // Dừng
        public Control TourBtnImportAccount => button16;        // + Thêm tài khoản
        public Control TourInputSearch => input6;               // Tìm kiếm
        // Danh sách
        public Control TourCboFilter => cboFilterAccount;       // Lọc tài khoản
        public Control TourBtnReload => button9;                // Tải lại
        public Control TourBtnToggleCols => this.Controls.Find("ssaBtnColumns", true).FirstOrDefault(); // "Hiển thị" (tạo động)
        public Control TourDataGrid => dataGridView1;           // Bảng tài khoản
        public ToolStrip TourToolStripStats => toolStrip1;      // Thanh thống kê
        #endregion
    }
}
