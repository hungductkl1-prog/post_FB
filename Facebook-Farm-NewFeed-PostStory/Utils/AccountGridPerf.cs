using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    /// <summary>Lightweight timing hooks for grid load/search. Logs to Debug + a rolling file.</summary>
    public static class AccountGridPerf
    {
        private static readonly object _fileLock = new();
        private static readonly string _logPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "account_grid_perf.log");

        public static Stopwatch Start() => Stopwatch.StartNew();

        // ── Phase 5 runtime instrumentation ────────────────────────────────
        // Toggle via ACCOUNT_GRID_DIAG=1 or call EnableDiagnostics().
        public static bool DiagnosticsEnabled { get; private set; }

        // ── A/B render bypass (UI-render hot-path isolation) ────────────────
        // Toggle via ACCOUNT_GRID_NO_PAINT=1 or call EnableNoPaintMode().
        // When on: custom checkbox CellPainting, CellFormatting body, and the
        // status-badge painter are skipped so the framework paints with defaults.
        // Lets you A/B the cost of the custom render handlers vs the grid itself.
        public static bool DisableCustomRendering { get; private set; }

        public static void EnableNoPaintMode(bool enabled = true)
        {
            DisableCustomRendering = enabled;
            if (enabled) Emit("[Diag] NO-PAINT mode active → custom checkbox/CellFormatting/status-badge bypassed");
        }

        private static long _cvnCalls;
        private static long _cvnTicks;
        private static long _paintCalls;
        private static long _paintTicks;
        private static long _uiMarshalCalls;

        // Independent per-event counters for the GridMetrics 30s consumer
        // (separate from the EmitSnapshot counters above so the two timers don't race on reset).
        private static long _gmCvnCalls;
        private static long _gmCellPaintingCalls;
        private static long _gmCellFormattingCalls;

        public static void RecordCellPainting()
        {
            if (!DiagnosticsEnabled) return;
            Interlocked.Increment(ref _gmCellPaintingCalls);
        }

        public static void RecordCellFormatting()
        {
            if (!DiagnosticsEnabled) return;
            Interlocked.Increment(ref _gmCellFormattingCalls);
        }

        /// <summary>Read+reset CellValueNeeded / CellPainting / CellFormatting counts for GridMetrics.</summary>
        public static (long cvn, long cellPainting, long cellFormatting) TakeUiEventCounts()
        {
            long cvn = Interlocked.Exchange(ref _gmCvnCalls, 0);
            long cp = Interlocked.Exchange(ref _gmCellPaintingCalls, 0);
            long cf = Interlocked.Exchange(ref _gmCellFormattingCalls, 0);
            return (cvn, cp, cf);
        }

        private static readonly string BoolTrue = "True";
        private static readonly string BoolFalse = "False";

        public static void EnableDiagnostics(bool enabled = true)
        {
            DiagnosticsEnabled = enabled;
            if (enabled)
            {
                // Flush buffered lines on process exit (logger batches writes; this guarantees
                // the final partial batch reaches disk). Self-contained so callers don't have to.
                try { AppDomain.CurrentDomain.ProcessExit += static (_, _) => Flush(); } catch { }

                Emit("[Diag] Phase 5 instrumentation active → logs\\account_grid_perf.log");
                Emit("[Diag] Phase 6 acceptance: 30k rows, scroll 10min, jump top/mid/bottom, refresh — watch RAM + stutter");
            }
        }

        public static void RecordCellValueNeeded(long elapsedTicks)
        {
            if (!DiagnosticsEnabled) return;
            Interlocked.Increment(ref _cvnCalls);
            Interlocked.Add(ref _cvnTicks, elapsedTicks);
            Interlocked.Increment(ref _gmCvnCalls);
        }

        public static void RecordPaint(long elapsedTicks)
        {
            if (!DiagnosticsEnabled) return;
            Interlocked.Increment(ref _paintCalls);
            Interlocked.Add(ref _paintTicks, elapsedTicks);
        }

        public static void RecordUiMarshal()
        {
            if (!DiagnosticsEnabled) return;
            Interlocked.Increment(ref _uiMarshalCalls);
        }

        /// <summary>Format bool without per-cell string allocation.</summary>
        public static string FormatBool(bool value) => value ? BoolTrue : BoolFalse;

        private static double TicksToMs(long ticks, long calls)
            => calls <= 0 ? 0 : (double)ticks / calls / Stopwatch.Frequency * 1000.0;

        public readonly struct MemoryAudit
        {
            public readonly int CacheAccountInstances;
            public readonly int ScopeAccountInstances;
            public readonly int InMemoryViewInstances;
            public readonly int CheckedMaterialized;
            public readonly int CachePages;
            public readonly int InFlightPages;
            public readonly int MaxRetainedRows;
            public readonly bool ViewAliasesScope;

            public MemoryAudit(
                int CacheAccountInstances,
                int ScopeAccountInstances,
                int InMemoryViewInstances,
                int CheckedMaterialized,
                int CachePages,
                int InFlightPages,
                int MaxRetainedRows,
                bool ViewAliasesScope)
            {
                this.CacheAccountInstances = CacheAccountInstances;
                this.ScopeAccountInstances = ScopeAccountInstances;
                this.InMemoryViewInstances = InMemoryViewInstances;
                this.CheckedMaterialized = CheckedMaterialized;
                this.CachePages = CachePages;
                this.InFlightPages = InFlightPages;
                this.MaxRetainedRows = MaxRetainedRows;
                this.ViewAliasesScope = ViewAliasesScope;
            }
        }

        /// <summary>Emit [Grid] + [Memory] + [Database] + [VirtualMode] blocks. Resets rolling counters.</summary>
        public static void EmitSnapshot(
            int visibleRows,
            int cachePages,
            int cacheRows,
            long cacheHits,
            long cacheMisses,
            long lastPageFetchMs,
            MemoryAudit audit,
            long uiMarshals)
        {
            if (!DiagnosticsEnabled) return;

            long cvnCalls = Interlocked.Exchange(ref _cvnCalls, 0);
            long cvnTicks = Interlocked.Exchange(ref _cvnTicks, 0);
            long paintCalls = Interlocked.Exchange(ref _paintCalls, 0);
            long paintTicks = Interlocked.Exchange(ref _paintTicks, 0);
            long marshals = Interlocked.Exchange(ref _uiMarshalCalls, 0);
            marshals += uiMarshals;

            long totalLookups = cacheHits + cacheMisses;
            double hitPct = totalLookups <= 0 ? 0 : (double)cacheHits / totalLookups * 100.0;
            double missPct = totalLookups <= 0 ? 0 : (double)cacheMisses / totalLookups * 100.0;

            var proc = Process.GetCurrentProcess();
            long workingSetMb = proc.WorkingSet64 / (1024 * 1024);
            long managedHeapMb = GC.GetTotalMemory(forceFullCollection: false) / (1024 * 1024);
            int gen2 = GC.CollectionCount(2);
            int gen1 = GC.CollectionCount(1);
            int gen0 = GC.CollectionCount(0);

            long lohBytes = 0;
            long fragmentedBytes = 0;
            try
            {
                var info = GC.GetGCMemoryInfo();
                lohBytes = info.HeapSizeBytes;
                fragmentedBytes = info.FragmentedBytes;
            }
            catch { /* older runtimes */ }

            int estimatedAlive = audit.CacheAccountInstances + audit.ScopeAccountInstances
                + (audit.ViewAliasesScope ? 0 : audit.InMemoryViewInstances) + audit.CheckedMaterialized;

            var sb = new StringBuilder(640);
            sb.Append("[Grid] ")
              .Append("VisibleRows=").Append(visibleRows)
              .Append(" CachePages=").Append(cachePages)
              .Append(" CacheRows=").Append(cacheRows)
              .Append(" CellValueNeededAvg=").Append(TicksToMs(cvnTicks, cvnCalls).ToString("0.000")).Append("ms")
              .Append(" CellValueNeededCalls=").Append(cvnCalls)
              .Append(" PaintAvg=").Append(TicksToMs(paintTicks, paintCalls).ToString("0.000")).Append("ms")
              .Append(" PaintCalls=").Append(paintCalls)
              .Append(" UiMarshals=").Append(marshals);
            Emit(sb.ToString());

            sb.Clear();
            sb.Append("[Memory] ")
              .Append("WorkingSet=").Append(workingSetMb).Append("MB")
              .Append(" ManagedHeap=").Append(managedHeapMb).Append("MB")
              .Append(" LOH=").Append(lohBytes / (1024 * 1024)).Append("MB")
              .Append(" Fragmented=").Append(fragmentedBytes / (1024 * 1024)).Append("MB")
              .Append(" Gen0=").Append(gen0)
              .Append(" Gen1=").Append(gen1)
              .Append(" Gen2Collections=").Append(gen2);
            Emit(sb.ToString());

            sb.Clear();
            sb.Append("[Database] ")
              .Append("PageFetch=").Append(lastPageFetchMs).Append("ms")
              .Append(" CacheHit=").Append(hitPct.ToString("0.0")).Append('%')
              .Append(" CacheMiss=").Append(missPct.ToString("0.0")).Append('%')
              .Append(" Hits=").Append(cacheHits)
              .Append(" Misses=").Append(cacheMisses);
            Emit(sb.ToString());

            sb.Clear();
            sb.Append("[VirtualMode] ")
              .Append("AccountInstancesEst=").Append(estimatedAlive)
              .Append(" CacheAccounts=").Append(audit.CacheAccountInstances)
              .Append(" ScopeAccounts=").Append(audit.ScopeAccountInstances)
              .Append(" ViewAccounts=").Append(audit.InMemoryViewInstances)
              .Append(" ViewAliasesScope=").Append(audit.ViewAliasesScope)
              .Append(" CheckedMaterialized=").Append(audit.CheckedMaterialized)
              .Append(" InFlightPages=").Append(audit.InFlightPages)
              .Append(" MaxRetainedRows=").Append(audit.MaxRetainedRows);
            Emit(sb.ToString());
        }

        public static void Log(Stopwatch sw, string phase, int rowCount = 0)
        {
            sw.Stop();
            string line = rowCount > 0
                ? $"[AccountGrid] {phase}: {sw.ElapsedMilliseconds} ms ({rowCount:N0} rows)"
                : $"[AccountGrid] {phase}: {sw.ElapsedMilliseconds} ms";
            Emit(line);
        }

        public static void LogUiState(
            Control host,
            DataGridView grid,
            Control? emptyOverlay,
            int viewRows,
            bool perfMode)
        {
            int tid = Environment.CurrentManagedThreadId;
            bool uiThread = host.IsHandleCreated && !host.InvokeRequired;
            string overlay = emptyOverlay == null
                ? "overlay=none"
                : $"overlay visible={emptyOverlay.Visible} enabled={emptyOverlay.Enabled} dock={emptyOverlay.Dock} size={emptyOverlay.Size}";
            Emit(
                $"[AccountGrid] UiState: tid={tid} uiThread={uiThread} perf={perfMode} " +
                $"viewRows={viewRows:N0} gridEnabled={grid.Enabled} gridRows={grid.RowCount:N0} " +
                $"hostEnabled={host.Enabled} {overlay}");
        }

        /// <summary>Public passthrough to the rolling perf log (diagnostic lines).</summary>
        public static void LogLine(string line) => Emit(line);

        /// <summary>
        /// Instrument a UI event handler. Always feeds the UiThreadProfiler aggregate
        /// (so calls/sec = storm detection, max ms = slowest invocation), but only writes a
        /// [UiHandler] line when the single invocation is slow (>= thresholdMs) to avoid flooding
        /// the log with thousands of fast SelectionChanged/CellValueNeeded-style calls.
        /// </summary>
        public static void LogHandler(string name, long startTimestamp, double thresholdMs = 8.0)
        {
            if (!DiagnosticsEnabled || startTimestamp == 0) return;
            double ms = (double)(Stopwatch.GetTimestamp() - startTimestamp) / Stopwatch.Frequency * 1000.0;
            UiThreadProfiler.End("Handler." + name, startTimestamp);
            if (ms >= thresholdMs)
                Emit($"[UiHandler] Name={name} DurationMs={ms:0.0}");
        }

        /// <summary>[MemoryStats] managed heap / private memory / LOH / GC gen counts.</summary>
        public static void LogMemoryStats(int accounts, string phase = "")
        {
            if (!DiagnosticsEnabled) return;
            long managed = GC.GetTotalMemory(false);
            long priv = 0;
            try { priv = Process.GetCurrentProcess().PrivateMemorySize64; } catch { }
            long loh = 0;
            double pausePct = 0;
            try
            {
                var info = GC.GetGCMemoryInfo();
                var gi = info.GenerationInfo;
                if (gi.Length > 3) loh = gi[3].SizeAfterBytes; // [0]gen0 [1]gen1 [2]gen2 [3]LOH [4]POH
                pausePct = info.PauseTimePercentage;
            }
            catch { }
            Emit($"[MemoryStats]{(string.IsNullOrEmpty(phase) ? "" : " Phase=" + phase)} Accounts={accounts}" +
                 $" ManagedHeap={managed / (1024 * 1024)}MB PrivateMemory={priv / (1024 * 1024)}MB LOH={loh / (1024 * 1024)}MB" +
                 $" Gen0={GC.CollectionCount(0)} Gen1={GC.CollectionCount(1)} Gen2={GC.CollectionCount(2)} GCPauseTime={pausePct:0.0}%");
        }

        /// <summary>[GC] gen counts + last pause for a named UI operation. Pass the pre-op gen counts to get deltas.</summary>
        public static (int g0, int g1, int g2) GcCounts() => (GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));

        public static void LogGc(string phase, (int g0, int g1, int g2) before)
        {
            if (!DiagnosticsEnabled) return;
            int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
            double pausePct = 0; double lastPauseMs = 0;
            try
            {
                var info = GC.GetGCMemoryInfo();
                pausePct = info.PauseTimePercentage;
                var pd = info.PauseDurations;
                if (pd.Length > 0) lastPauseMs = pd[0].TotalMilliseconds;
            }
            catch { }
            Emit($"[GC] Phase={phase} Gen0+={g0 - before.g0} Gen1+={g1 - before.g1} Gen2+={g2 - before.g2}" +
                 $" (abs G0={g0} G1={g1} G2={g2}) LastPause={lastPauseMs:0.0}ms PauseTime={pausePct:0.0}%");
        }

        /// <summary>Log an n-scaling operation in the requested format: [Perf] Function= Accounts= Elapsed=.</summary>
        public static void LogN(string function, int accounts, long startTimestamp)
        {
            if (!DiagnosticsEnabled || startTimestamp == 0) return;
            double ms = (double)(Stopwatch.GetTimestamp() - startTimestamp) / Stopwatch.Frequency * 1000.0;
            Emit($"[Perf] Function={function} Accounts={accounts} Elapsed={ms:0.000}ms");
            UiThreadProfiler.End(function, startTimestamp);
        }

        // Persistent writer: open once and reuse instead of open/append/close per line.
        // The old per-line File.AppendAllText churned a file handle thousands of times under
        // diagnostics, which can surface Windows' "Delayed Write Failed" cache error (and gets
        // amplified by antivirus scanning on every close). One buffered handle avoids that.
        private static StreamWriter? _writer;
        private static bool _fileLoggingDisabled;
        private static int _pendingSinceFlush;

        private static void Emit(string line)
        {
            Debug.WriteLine(line);
            if (_fileLoggingDisabled) return;
            try
            {
                lock (_fileLock)
                {
                    if (_fileLoggingDisabled) return;

                    if (_writer == null)
                    {
                        var dir = Path.GetDirectoryName(_logPath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        var fs = new FileStream(
                            _logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite,
                            bufferSize: 1 << 16);
                        _writer = new StreamWriter(fs, new UTF8Encoding(false)) { AutoFlush = false };
                    }

                    _writer.Write(DateTime.Now.ToString("HH:mm:ss.fff"));
                    _writer.Write(' ');
                    _writer.Write(line);
                    _writer.Write(Environment.NewLine);

                    // Flush in batches to keep the data safe without per-line disk pressure.
                    if (++_pendingSinceFlush >= 32)
                    {
                        _writer.Flush();
                        _pendingSinceFlush = 0;
                    }
                }
            }
            catch
            {
                // A failed write (removed media, write-protected, disk error) must never break the
                // UI nor keep retrying — disable file logging so the OS dialog can't reappear.
                _fileLoggingDisabled = true;
                try { lock (_fileLock) { _writer?.Dispose(); _writer = null; } } catch { }
            }
        }

        /// <summary>Flush any buffered log lines to disk (call on app shutdown).</summary>
        public static void Flush()
        {
            if (_fileLoggingDisabled) return;
            try
            {
                lock (_fileLock)
                {
                    _writer?.Flush();
                    _pendingSinceFlush = 0;
                }
            }
            catch { }
        }
    }
}
