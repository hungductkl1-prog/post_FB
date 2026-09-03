using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Utils.Design
{
    /// <summary>
    /// Ép font có đủ glyph tiếng Việt (Segoe UI) cho mọi control mà FontUtil/Designer
    /// lỡ gán font tuỳ biến thiếu dấu (gây "vỡ chữ có dấu" trên label/button/menu/header).
    ///
    /// Nguyên tắc:
    ///   - Giữ nguyên size / style / unit của font hiện tại — chỉ đổi *family*.
    ///   - Chỉ đổi khi family hiện tại KHÔNG nằm trong whitelist các font đã biết là
    ///     render tiếng Việt tốt → tránh đụng các font designer cố ý (mono, YaHei…).
    ///   - KHÔNG dispose font cũ: nó có thể là font static dùng chung của thư viện
    ///     (FontUtil._fontSemiBold) — dispose sẽ làm hỏng control khác.
    ///
    /// Gọi NGAY SAU <c>FontUtil.ApplyFontToAllControls(this)</c> để "thắng" font xấu.
    /// Đây là bản tổng quát hoá của ApplyVietnameseSafeGridFont (đã fix sẵn cho lưới).
    /// </summary>
    public static class VietnameseFont
    {
        /// <summary>Font đích — đủ bộ dấu tiếng Việt, chuẩn Windows desktop.</summary>
        public const string SafeFamily = "Segoe UI";

        /// <summary>
        /// Các family đã biết hiển thị tiếng Việt đầy đủ → giữ nguyên, không đổi.
        /// Mọi family khác (gồm font tuỳ biến thiếu glyph từ thư viện) sẽ bị ép về SafeFamily.
        /// </summary>
        private static readonly HashSet<string> SafeFamilies = new(StringComparer.OrdinalIgnoreCase)
        {
            "Segoe UI", "Segoe UI Semibold", "Segoe UI Semilight", "Segoe UI Light",
            "Segoe UI Symbol", "Tahoma", "Arial", "Microsoft Sans Serif", "Calibri",
            "Verdana", "Times New Roman", "Consolas", "Cascadia Mono", "Cascadia Code",
            "Microsoft YaHei UI", "Microsoft YaHei"
        };

        /// <summary>Ép font Việt-an-toàn cho <paramref name="root"/> và toàn bộ control con.</summary>
        public static void Enforce(Control? root)
        {
            if (root == null) return;
            ApplyToControl(root);
        }

        private static void ApplyToControl(Control c)
        {
            SwapIfNeeded(c.Font, f => c.Font = f);

            if (c is DataGridView dgv)
                ApplyToGrid(dgv);

            if (c is ToolStrip ts)
            {
                SwapIfNeeded(ts.Font, f => ts.Font = f);
                foreach (ToolStripItem item in ts.Items)
                    ApplyToToolStripItem(item);
            }

            var ctx = c.ContextMenuStrip;
            if (ctx != null)
            {
                SwapIfNeeded(ctx.Font, f => ctx.Font = f);
                foreach (ToolStripItem item in ctx.Items)
                    ApplyToToolStripItem(item);
            }

            foreach (Control child in c.Controls)
                ApplyToControl(child);
        }

        private static void ApplyToGrid(DataGridView dgv)
        {
            SwapStyle(dgv.DefaultCellStyle);
            SwapStyle(dgv.ColumnHeadersDefaultCellStyle);
            SwapStyle(dgv.RowsDefaultCellStyle);
            SwapStyle(dgv.RowHeadersDefaultCellStyle);
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col?.HasDefaultCellStyle == true)
                    SwapStyle(col.DefaultCellStyle);
            }
        }

        private static void SwapStyle(DataGridViewCellStyle? style)
        {
            if (style == null) return;
            SwapIfNeeded(style.Font, f => style.Font = f);
        }

        private static void ApplyToToolStripItem(ToolStripItem item)
        {
            SwapIfNeeded(item.Font, f => item.Font = f);
            if (item is ToolStripDropDownItem dd)
            {
                foreach (ToolStripItem sub in dd.DropDownItems)
                    ApplyToToolStripItem(sub);
            }
        }

        private static void SwapIfNeeded(Font? current, Action<Font> assign)
        {
            if (current == null) return;
            if (SafeFamilies.Contains(current.FontFamily.Name)) return;

            // Tạo font mới cùng size/style/unit/charset, chỉ khác family.
            // Không dispose font cũ (có thể là font dùng chung).
            assign(new Font(SafeFamily, current.Size, current.Style, current.Unit, current.GdiCharSet));
        }
    }
}
