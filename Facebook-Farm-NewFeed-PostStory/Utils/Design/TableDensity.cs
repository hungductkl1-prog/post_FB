using System;
using System.IO;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Utils.Design
{
    /// <summary>
    /// Table density mode — Power user thường muốn nhìn được nhiều rows.
    /// 3 mức:
    ///   Normal  = 36px row, 40px header (default — comfort)
    ///   Compact = 28px row, 32px header (~25% nhiều rows hơn)
    ///   Ultra   = 22px row, 26px header (max throughput — 50% nhiều rows hơn)
    /// </summary>
    public enum TableDensityMode
    {
        Normal  = 0,
        Compact = 1,
        Ultra   = 2,
    }

    public static class TableDensity
    {
        private static readonly string _settingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LamToolAutoPhonePrime", "table_density.txt");

        private static TableDensityMode? _cached;

        public static TableDensityMode Current
        {
            get
            {
                if (_cached.HasValue) return _cached.Value;
                _cached = Load();
                return _cached.Value;
            }
            set
            {
                _cached = value;
                Save(value);
                ModeChanged?.Invoke(null, value);
            }
        }

        /// <summary>Raised when user toggles density. Subscribers should re-apply to their DataGridViews.</summary>
        public static event EventHandler<TableDensityMode>? ModeChanged;

        public static int RowHeight(TableDensityMode mode) => mode switch
        {
            TableDensityMode.Compact => 28,
            TableDensityMode.Ultra   => 22,
            _                        => 36,
        };

        public static int HeaderHeight(TableDensityMode mode) => mode switch
        {
            TableDensityMode.Compact => 32,
            TableDensityMode.Ultra   => 26,
            _                        => 40,
        };

        public static string Label(TableDensityMode mode) => mode switch
        {
            TableDensityMode.Compact => "Compact",
            TableDensityMode.Ultra   => "Ultra",
            _                        => "Normal",
        };

        public static void Apply(DataGridView dgv, TableDensityMode mode)
        {
            if (dgv == null) return;
            dgv.RowTemplate.Height  = RowHeight(mode);
            dgv.ColumnHeadersHeight = HeaderHeight(mode);
            foreach (DataGridViewRow row in dgv.Rows)
            {
                row.Height = RowHeight(mode);
            }
            dgv.Invalidate();
        }

        public static TableDensityMode Cycle(TableDensityMode mode) => mode switch
        {
            TableDensityMode.Normal  => TableDensityMode.Compact,
            TableDensityMode.Compact => TableDensityMode.Ultra,
            _                        => TableDensityMode.Normal,
        };

        // ── Persistence ──────────────────────────────────────────
        private static TableDensityMode Load()
        {
            try
            {
                if (!File.Exists(_settingsFile)) return TableDensityMode.Normal;
                var txt = File.ReadAllText(_settingsFile).Trim();
                if (Enum.TryParse<TableDensityMode>(txt, true, out var m)) return m;
            }
            catch { /* fall through */ }
            return TableDensityMode.Normal;
        }

        private static void Save(TableDensityMode mode)
        {
            try
            {
                var dir = Path.GetDirectoryName(_settingsFile);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_settingsFile, mode.ToString());
            }
            catch { /* ignore — preference not critical */ }
        }
    }
}
