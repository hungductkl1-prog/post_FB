using System.ComponentModel;
using System.Threading;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;

namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    /// <summary>
    /// Cửa sổ trang trượt cho VirtualMode grid: chỉ giữ vài trang (~MaxPages*PageSize dòng)
    /// trong RAM thay vì cả 30k+ Account. Lưới set RowCount = TotalCount (trải hết),
    /// dữ liệu lấy theo rowIndex qua GetRow → tải trang từ DB theo nhu cầu.
    ///
    /// Mọi mutate _pages/_lru/_inFlight chỉ chạy trên UI thread. DB query chạy trên Task.Run
    /// rồi marshal về UI qua _runOnUi. Generation counter loại bỏ kết quả trang cũ sau Reset.
    ///
    /// Scroll coalescing: rapid scroll events within ScrollDebounceMs collapse into a single
    /// fetch for the latest requested page. Obsolete fetches are cancelled via CancellationToken.
    /// </summary>
    public sealed class AccountWindowCache : IDisposable
    {
        // 50 dòng/trang (yêu cầu spec: nạp 50 bản ghi mỗi request). PageSize nhỏ hơn → mỗi lần
        // fetch DB nhẹ hơn; bù lại tăng MaxPages để cửa sổ trượt giữ nguyên ~600 dòng (cuộn mượt).
        public const int PageSize = 50;
        private const int MaxPages = 12;       // ~600 dòng RAM tối đa (12 × 50) — giữ nguyên trần RAM cũ
        private const int PrefetchRadius = 1;  // tải thêm 1 trang trước/sau trang đang xem
        private const int ScrollDebounceMs = 30; // coalesce scroll requests within this window

        private readonly AccountContext _accountContext;
        private readonly Action<int, int> _invalidateRows;      // (startRow, endRow) → grid.InvalidateRow
        private readonly Action<Account, int> _onRowChanged;     // account đổi prop → invalidate đúng row
        private readonly Action<Action> _runOnUi;

        // Live-instance resolver: khi 1 account đang chạy job, model live (bị MainService mutate
        // Status/State/ColorType realtime) nằm trong AccountServices.Accounts — KHÁC instance với
        // bản fetch từ DB. Resolver trả về instance live theo Id để page giữ đúng object đang chạy,
        // nhờ đó PropertyChanged của nó kích InvalidateRow → cột Trạng thái cập nhật realtime.
        private Func<Guid, Account?>? _liveResolver;

        private string _whereClause = "1=0";
        private Dictionary<string, object> _parameters = new();
        private string _orderBySql = "Uid";
        private bool _orderDesc;
        private int _generation;

        public int TotalCount { get; private set; }
        public string WhereClause => _whereClause;
        public Dictionary<string, object> Parameters => new(_parameters);
        public string OrderBySql => _orderBySql;
        public bool OrderDesc => _orderDesc;

        // ── Phase 5 instrumentation ────────────────────────────────────────
        // GetRow hit = trang đã có trong RAM; miss = phải BeginFetch từ DB.
        // Mutate chỉ trên UI thread (GetRow) nên long thường + Volatile đủ cho snapshot đọc.
        private long _cacheHits;
        private long _cacheMisses;
        private long _lastPageFetchMs;

        public long CacheHits => Volatile.Read(ref _cacheHits);
        public long CacheMisses => Volatile.Read(ref _cacheMisses);
        public long LastPageFetchMs => Volatile.Read(ref _lastPageFetchMs);
        public int PageCount => _pages.Count;
        public int CachedRowCount
        {
            get
            {
                int n = 0;
                foreach (var p in _pages.Values) n += p.Accounts.Count;
                return n;
            }
        }

        public int InFlightPageCount => _inFlight.Count;
        public int MaxRetainedRows => MaxPages * PageSize;

        /// <summary>Reset hit/miss counters and return values since last snapshot (UI thread).</summary>
        public (long hits, long misses) TakeCacheStatsSnapshot()
        {
            long hits = _cacheHits;
            long misses = _cacheMisses;
            _cacheHits = 0;
            _cacheMisses = 0;
            return (hits, misses);
        }

        private sealed class CachePage
        {
            public required List<Account> Accounts;
            public int StartIndex;
        }

        private readonly Dictionary<int, CachePage> _pages = new();
        private readonly LinkedList<int> _lru = new();           // front = mới dùng nhất
        private readonly HashSet<int> _inFlight = new();
        private readonly Dictionary<Guid, int> _idToRow = new();

        // GetRow chạy mỗi cell (rows × cols) mỗi lần repaint — mọi cell cùng 1 dòng (và các dòng
        // liền nhau cùng trang) map về cùng pageIndex. Nhớ trang vừa phục vụ để bỏ qua TouchLru
        // (LinkedList.Find O(MaxPages)) + TriggerPrefetch lặp lại cho cùng trang.
        private int _lastServedPage = -1;
        private CachePage? _lastServedPageRef;

        // ── Scroll coalescing ──────────────────────────────────────────────
        // Debounce rapid scroll → only fetch the latest requested page.
        private readonly HashSet<int> _pendingFetchPages = new();
        private CancellationTokenSource? _scrollCts;
        private System.Threading.Timer? _scrollDebounceTimer;
        private long _cancelledFetchCount;
        private long _scrollRequestCount;
        public long CancelledFetchCount => Volatile.Read(ref _cancelledFetchCount);
        public long ScrollRequestCount => Volatile.Read(ref _scrollRequestCount);

        private static readonly Account _placeholder = new();    // Id = Guid.Empty → IsAccountChecked bỏ qua

        public AccountWindowCache(
            AccountContext accountContext,
            Action<int, int> invalidateRows,
            Action<Account, int> onRowChanged,
            Action<Action> runOnUi)
        {
            _accountContext = accountContext;
            _invalidateRows = invalidateRows;
            _onRowChanged = onRowChanged;
            _runOnUi = runOnUi;
        }

        /// <summary>Đăng ký resolver trả về instance Account đang chạy job theo Id (hoặc null).
        /// Gọi trên UI thread. Truyền null để gỡ.</summary>
        public void SetLiveResolver(Func<Guid, Account?>? resolver) => _liveResolver = resolver;

        /// <summary>Đặt lại scope (đổi nhóm/filter/search). TotalCount truyền vào từ COUNT đã chạy nền
        /// → KHÔNG query DB trên UI thread. Trang tải bất đồng bộ khi grid paint (GetRow).</summary>
        public void Reset(string whereClause, Dictionary<string, object> parameters, string orderBySql, bool orderDesc, int totalCount)
        {
            _generation++;
            _pendingFetchPages.Clear();
            _scrollCts?.Cancel();
            _scrollCts?.Dispose();
            _scrollCts = null;
            EvictAll();
            _whereClause = string.IsNullOrEmpty(whereClause) ? "1=0" : whereClause;
            _parameters = new Dictionary<string, object>(parameters ?? new());
            _orderBySql = string.IsNullOrEmpty(orderBySql) ? "Uid" : orderBySql;
            _orderDesc = orderDesc;
            TotalCount = Math.Max(0, totalCount);
            if (TotalCount > 0)
            {
                // Fetch page 0 synchronously so data is ready before the first grid paint.
                // For large datasets use async; for small ones this is instant (<1ms).
                FetchPageSync(0);
                // Queue async prefetch of page 1 onward if needed.
                if (TotalCount > PageSize)
                    BeginFetch(1);
            }
        }

        /// <summary>Đổi sắp xếp — giữ scope + TotalCount, chỉ evict trang để tải lại theo thứ tự mới.</summary>
        public void Resort(string orderBySql, bool orderDesc)
        {
            _generation++;
            EvictAll();
            _orderBySql = string.IsNullOrEmpty(orderBySql) ? "Uid" : orderBySql;
            _orderDesc = orderDesc;
        }

        public Account GetRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= TotalCount) return _placeholder;
            int pageIndex = rowIndex / PageSize;

            // Cùng trang với lần gọi trước (các cell cùng dòng / dòng liền kề): chỉ tra mảng,
            // KHÔNG lặp lại TouchLru + TriggerPrefetch — đã làm khi trang này được phục vụ lần đầu.
            if (pageIndex == _lastServedPage && _lastServedPageRef != null)
            {
                _cacheHits++;
                int localFast = rowIndex - _lastServedPageRef.StartIndex;
                return (localFast >= 0 && localFast < _lastServedPageRef.Accounts.Count)
                    ? _lastServedPageRef.Accounts[localFast] : _placeholder;
            }

            if (_pages.TryGetValue(pageIndex, out var page))
            {
                _cacheHits++;
                TouchLru(pageIndex);
                TriggerPrefetch(pageIndex);
                _lastServedPage = pageIndex;
                _lastServedPageRef = page;
                int local = rowIndex - page.StartIndex;
                return (local >= 0 && local < page.Accounts.Count) ? page.Accounts[local] : _placeholder;
            }

            _cacheMisses++;
            BeginFetch(pageIndex);
            TriggerPrefetch(pageIndex);
            return _placeholder;
        }

        /// <summary>Evict hết trang nhưng giữ scope + TotalCount; tải lại xảy ra khi grid repaint.</summary>
        public void InvalidateAll()
        {
            _generation++;
            EvictAll();
            _invalidateRows(0, 0);   // tín hiệu: repaint vùng hiển thị (callback tự xử lý)
        }

        public void Dispose()
        {
            _generation++;
            _scrollDebounceTimer?.Dispose();
            _scrollDebounceTimer = null;
            _scrollCts?.Cancel();
            _scrollCts?.Dispose();
            _scrollCts = null;
            _pendingFetchPages.Clear();
            EvictAll();
        }

        // ── internal ─────────────────────────────────────────────

        private void TriggerPrefetch(int pageIndex)
        {
            int maxPage = TotalCount <= 0 ? 0 : (TotalCount - 1) / PageSize;
            for (int p = pageIndex - PrefetchRadius; p <= pageIndex + PrefetchRadius; p++)
            {
                if (p < 0 || p > maxPage) continue;
                if (_pages.ContainsKey(p) || _inFlight.Contains(p)) continue;
                BeginFetch(p);
            }
        }

        private void BeginFetch(int pageIndex)
        {
            if (_inFlight.Contains(pageIndex) || _pages.ContainsKey(pageIndex)) return;
            Interlocked.Increment(ref _scrollRequestCount);

            // Coalesce: add to pending set and reset debounce timer.
            // Only the latest batch of requested pages fires after ScrollDebounceMs idle.
            _pendingFetchPages.Add(pageIndex);
            ResetScrollDebounceTimer();
        }

        private void ResetScrollDebounceTimer()
        {
            if (_scrollDebounceTimer == null)
            {
                _scrollDebounceTimer = new System.Threading.Timer(
                    OnScrollDebounceElapsed, null, ScrollDebounceMs, Timeout.Infinite);
            }
            else
            {
                _scrollDebounceTimer.Change(ScrollDebounceMs, Timeout.Infinite);
            }
        }

        private void OnScrollDebounceElapsed(object? state)
        {
            // Timer fires on ThreadPool — marshal to UI thread for state mutation.
            _runOnUi(FlushPendingFetches);
        }

        private void FlushPendingFetches()
        {
            if (_pendingFetchPages.Count == 0) return;

            // Cancel any previous batch of fetches that haven't completed yet.
            var oldCts = _scrollCts;
            if (oldCts != null)
            {
                oldCts.Cancel();
                oldCts.Dispose();
                Interlocked.Increment(ref _cancelledFetchCount);
            }
            _scrollCts = new CancellationTokenSource();
            var token = _scrollCts.Token;

            // Snapshot and clear pending set.
            var pagesToFetch = new List<int>(_pendingFetchPages.Count);
            foreach (int p in _pendingFetchPages)
            {
                if (!_inFlight.Contains(p) && !_pages.ContainsKey(p))
                    pagesToFetch.Add(p);
            }
            _pendingFetchPages.Clear();

            // Fire actual fetches for surviving pages.
            for (int i = 0; i < pagesToFetch.Count; i++)
                ExecuteFetch(pagesToFetch[i], token);
        }

        private void FetchPageSync(int pageIndex)
        {
            if (_pages.ContainsKey(pageIndex)) return;
            int offset = pageIndex * PageSize;
            string orderSql = _orderDesc ? $"{_orderBySql} DESC" : _orderBySql;
            List<Account> accounts;
            try
            {
                accounts = _accountContext.GetListPage(_whereClause, _parameters, offset, PageSize, orderSql);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[AccountWindowCache] FetchPageSync error: " + ex);
                return;
            }
            PreparePageAccounts(accounts, offset);
            InsertPage(pageIndex, accounts);
        }

        private void ExecuteFetch(int pageIndex, CancellationToken token)
        {
            if (_inFlight.Contains(pageIndex) || _pages.ContainsKey(pageIndex)) return;
            _inFlight.Add(pageIndex);
            int gen = _generation;
            int offset = pageIndex * PageSize;
            string orderCol = _orderBySql;
            string orderSql = _orderDesc ? $"{orderCol} DESC" : orderCol;
            string where = _whereClause;
            var prms = new Dictionary<string, object>(_parameters);

            // ── Keyset khi trang kề đã nạp (cuộn tuần tự) → O(log n), không SCAN bỏ offset ──
            string? anchorVal = null, anchorId = null;
            bool keysetAfter = false;
            if (_pages.TryGetValue(pageIndex - 1, out var prevPage) && prevPage.Accounts.Count == PageSize)
            {
                var a = prevPage.Accounts[prevPage.Accounts.Count - 1];
                anchorVal = GetAnchorValue(a, orderCol); anchorId = a.Id.ToString(); keysetAfter = true;
            }
            else if (_pages.TryGetValue(pageIndex + 1, out var nextPage) && nextPage.Accounts.Count > 0)
            {
                var a = nextPage.Accounts[0];
                anchorVal = GetAnchorValue(a, orderCol); anchorId = a.Id.ToString(); keysetAfter = false;
            }
            bool useKeyset = anchorVal != null;
            bool orderDesc = _orderDesc;

            _ = Task.Run(() =>
            {
                // Early exit if cancelled (obsolete scroll position).
                if (token.IsCancellationRequested)
                {
                    _runOnUi(() => _inFlight.Remove(pageIndex));
                    return;
                }

                List<Account> accounts;
                var fetchSw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    accounts = useKeyset
                        ? _accountContext.GetListPageKeyset(where, prms, orderCol, orderDesc, PageSize, anchorVal!, anchorId!, keysetAfter)
                        : _accountContext.GetListPage(where, prms, offset, PageSize, orderSql);
                }
                catch (Exception ex) { accounts = new List<Account>(); System.Diagnostics.Trace.TraceError("[AccountWindowCache] Page fetch error: " + ex); }
                fetchSw.Stop();
                Volatile.Write(ref _lastPageFetchMs, fetchSw.ElapsedMilliseconds);

                // Check cancellation after DB query completes.
                if (token.IsCancellationRequested)
                {
                    Interlocked.Increment(ref _cancelledFetchCount);
                    _runOnUi(() => { _inFlight.Remove(pageIndex); UnregisterMany(accounts); });
                    return;
                }

                PreparePageAccounts(accounts, offset);

                _runOnUi(() =>
                {
                    _inFlight.Remove(pageIndex);
                    System.Diagnostics.Trace.TraceInformation($"[Cache] Page {pageIndex}: gen={gen} cur={_generation} cancelled={token.IsCancellationRequested} count={accounts.Count}");
                    if (gen != _generation || token.IsCancellationRequested)
                    {
                        UnregisterMany(accounts);
                        return;
                    }
                    InsertPage(pageIndex, accounts);
                    _invalidateRows(offset, Math.Min(offset + accounts.Count, TotalCount) - 1);
                });
            });
        }

        /// <summary>Giá trị cột sắp xếp (dùng làm mốc keyset). Các cột sortable đều là TEXT.</summary>
        private static string GetAnchorValue(Account a, string orderCol) => orderCol switch
        {
            nameof(Account.Uid) => a.Uid ?? "",
            nameof(Account.FullName) => a.FullName ?? "",
            nameof(Account.NameFolder) => a.NameFolder ?? "",
            nameof(Account.NameScript) => a.NameScript ?? "",
            nameof(Account.Note) => a.Note ?? "",
            nameof(Account.State) => a.State ?? "",
            nameof(Account.Status) => a.Status ?? "",
            nameof(Account.RecentInteraction) => a.RecentInteraction ?? "",
            nameof(Account.TokenJob) => a.TokenJob ?? "",
            _ => a.Uid ?? ""
        };

        private void InsertPage(int pageIndex, List<Account> accounts)
        {
            if (_pages.ContainsKey(pageIndex)) { UnregisterMany(accounts); return; }
            while (_pages.Count >= MaxPages && _lru.Count > 0)
                EvictLruPage();

            var page = new CachePage { Accounts = accounts, StartIndex = pageIndex * PageSize };
            _pages[pageIndex] = page;
            _lru.AddFirst(pageIndex);
            for (int i = 0; i < accounts.Count; i++)
            {
                var acc = accounts[i];
                // Nếu account này đang chạy job → thay bằng instance live để cột Trạng thái
                // hiển thị realtime và PropertyChanged của nó kích repaint đúng dòng.
                var live = _liveResolver?.Invoke(acc.Id);
                if (live != null && !ReferenceEquals(live, acc))
                    accounts[i] = acc = live;
                _idToRow[acc.Id] = page.StartIndex + i;
                acc.PropertyChanged += OnCachedAccountPropertyChanged;
            }
        }

        private void TouchLru(int pageIndex)
        {
            var node = _lru.Find(pageIndex);
            if (node != null) { _lru.Remove(node); _lru.AddFirst(node); }
        }

        private void EvictLruPage()
        {
            var last = _lru.Last;
            if (last == null) return;
            int pageIndex = last.Value;
            _lru.RemoveLast();
            if (_pages.Remove(pageIndex, out var page))
                DetachPage(page.Accounts);
            if (pageIndex == _lastServedPage) { _lastServedPage = -1; _lastServedPageRef = null; }
        }

        private void EvictAll()
        {
            foreach (var page in _pages.Values)
                DetachPage(page.Accounts);
            _pages.Clear();
            _lru.Clear();
            _idToRow.Clear();
            _lastServedPage = -1;
            _lastServedPageRef = null;
            // Clear _inFlight: continuations still running will discard their result via generation mismatch.
            // Keeping stale entries here blocks BeginFetch for the same page in the new generation,
            // causing GetRow to always return _placeholder and the grid to display blank rows forever.
            _inFlight.Clear();
        }

        private void DetachPage(List<Account> accounts)
        {
            for (int i = 0; i < accounts.Count; i++)
            {
                accounts[i].PropertyChanged -= OnCachedAccountPropertyChanged;
                _idToRow.Remove(accounts[i].Id);
            }
            UnregisterMany(accounts);
        }

        private static void UnregisterMany(List<Account> accounts)
        {
            if (accounts.Count > 0)
                ThrottledPropertyNotifier.UnregisterMany(accounts);
        }

        private void OnCachedAccountPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Account acc && _idToRow.TryGetValue(acc.Id, out int rowIdx))
                _onRowChanged(acc, rowIdx);
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
                }
                finally { acc.EndBulkLoad(); }
            }
        }
    }
}
