using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Utils.Design
{
    /// <summary>
    /// Apply style chuẩn lên DataGridView. Gọi 1 lần ngay sau InitializeComponent().
    /// KHÔNG phá logic cũ — chỉ thiết lập style + hover handlers.
    /// </summary>
    public static class GridStyleHelper
    {
        private static readonly HashSet<DataGridView> RowHoverDisabled = new();

        public const int RowHeightComfort = 36;
        public const int RowHeightDense   = 32;
        public const int RowHeightRelaxed = 44;
        public const int HeaderHeight     = 40;

        public static void Apply(DataGridView dgv, int rowHeight = RowHeightComfort)
        {
            if (dgv == null) return;

            dgv.EnableHeadersVisualStyles    = false;
            dgv.BorderStyle                  = BorderStyle.None;
            dgv.BackgroundColor              = ColorPalette.Surface;
            dgv.GridColor                    = ColorPalette.BorderLight;
            dgv.RowHeadersVisible            = false;
            dgv.AllowUserToAddRows           = false;
            dgv.AllowUserToResizeRows        = false;
            dgv.AllowUserToResizeColumns     = true;
            dgv.SelectionMode                = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect                  = true;
            dgv.AutoSizeRowsMode             = DataGridViewAutoSizeRowsMode.None;
            dgv.RowTemplate.Height           = rowHeight;
            dgv.ColumnHeadersHeight          = HeaderHeight;
            dgv.ColumnHeadersHeightSizeMode  = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgv.DefaultCellStyle                = BuildCellStyle();
            dgv.ColumnHeadersDefaultCellStyle   = BuildHeaderStyle();
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor          = ColorPalette.SurfaceAlt,
                ForeColor          = ColorPalette.TextPrimary,
                SelectionBackColor = ColorPalette.RowSelected,
                SelectionForeColor = ColorPalette.TextPrimary
            };

            // Hover row (nhẹ, không phá AlternatingRows)
            dgv.CellMouseEnter -= OnCellMouseEnter;
            dgv.CellMouseLeave -= OnCellMouseLeave;
            dgv.CellMouseEnter += OnCellMouseEnter;
            dgv.CellMouseLeave += OnCellMouseLeave;

            // Khi grid bị dispose: gỡ handler + xóa khỏi static set để không pin instance đã chết.
            dgv.Disposed -= OnGridDisposed;
            dgv.Disposed += OnGridDisposed;
        }

        private static void OnGridDisposed(object? sender, EventArgs e)
        {
            if (sender is not DataGridView dgv) return;
            dgv.CellMouseEnter -= OnCellMouseEnter;
            dgv.CellMouseLeave -= OnCellMouseLeave;
            dgv.Disposed -= OnGridDisposed;
            lock (RowHoverDisabled)
                RowHoverDisabled.Remove(dgv);
        }

        /// <summary>
        /// VirtualMode + truy cập dgv.Rows[index] sẽ materialize row → đơ với 50k+ dòng.
        /// Gọi false trong large-list perf mode.
        /// </summary>
        public static void SetRowHoverEnabled(DataGridView dgv, bool enabled)
        {
            if (dgv == null) return;
            lock (RowHoverDisabled)
            {
                if (enabled) RowHoverDisabled.Remove(dgv);
                else RowHoverDisabled.Add(dgv);
            }
        }

        // ── private ───────────────────────────────────────────
        private static DataGridViewCellStyle BuildHeaderStyle() => new DataGridViewCellStyle
        {
            BackColor          = ColorPalette.Background,
            ForeColor          = ColorPalette.TextPrimary,
            Font               = FontScale.Body9Bold,
            Alignment          = DataGridViewContentAlignment.MiddleLeft,
            Padding            = new Padding(Spacing.Md, 0, Spacing.Md, 0),
            SelectionBackColor = ColorPalette.Background,
            SelectionForeColor = ColorPalette.TextPrimary,
            WrapMode           = DataGridViewTriState.False
        };

        private static DataGridViewCellStyle BuildCellStyle() => new DataGridViewCellStyle
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

        private static bool IsRowHoverEnabled(DataGridView dgv)
        {
            lock (RowHoverDisabled)
                return !RowHoverDisabled.Contains(dgv);
        }

        private static void OnCellMouseEnter(object? sender, DataGridViewCellEventArgs e)
        {
            if (sender is not DataGridView dgv) return;
            if (!IsRowHoverEnabled(dgv)) return;
            if (dgv.VirtualMode) return;
            if (e.RowIndex < 0 || e.RowIndex >= dgv.Rows.Count) return;
            var row = dgv.Rows[e.RowIndex];
            if (row.Selected) return;
            row.DefaultCellStyle.BackColor = ColorPalette.RowHover;
        }

        private static void OnCellMouseLeave(object? sender, DataGridViewCellEventArgs e)
        {
            if (sender is not DataGridView dgv) return;
            if (!IsRowHoverEnabled(dgv)) return;
            if (dgv.VirtualMode) return;
            if (e.RowIndex < 0 || e.RowIndex >= dgv.Rows.Count) return;
            var row = dgv.Rows[e.RowIndex];
            if (row.Selected) return;
            row.DefaultCellStyle.BackColor =
                (e.RowIndex % 2 == 1) ? ColorPalette.SurfaceAlt : ColorPalette.Surface;
        }
    }
}
