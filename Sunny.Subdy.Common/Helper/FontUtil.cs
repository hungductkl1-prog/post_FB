using System.Drawing;
using System.Drawing.Text;
using System.Reflection;
using System.Windows.Forms;

namespace Sunny.Subdy.Common.Helper
{
    /// <summary>
    /// Font utility — đã chuyển sang Segoe UI (chuẩn Windows desktop).
    /// Các field _font* vẫn giữ để giữ ABI cho code cũ; tất cả đều trỏ về Segoe UI.
    /// </summary>
    public partial class FontUtil
    {
        private const string SegoeUI = "Segoe UI";

        private static FontFamily _fontRegular;
        public static FontFamily _fontBold;
        private static FontFamily _fontLight;
        private static FontFamily _fontMedium;
        public static FontFamily _fontSemiBold;

        public static void LoadCustomFonts()
        {
            var family = new FontFamily(SegoeUI);
            _fontRegular  = family;
            _fontBold     = family;
            _fontLight    = family;
            _fontMedium   = family;
            _fontSemiBold = family;
        }

        private static readonly PropertyInfo _doubleBufferedProp =
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void ApplyFontToAllControls(Control parent)
        {
            if (_fontSemiBold == null) LoadCustomFonts();

            parent.SuspendLayout();
            try
            {
                _doubleBufferedProp?.SetValue(parent, true, null);
                ApplyFontRecursive(parent);
            }
            finally
            {
                parent.ResumeLayout(false);
                parent.PerformLayout();
            }
        }

        private static void ApplyFontRecursive(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                Font oldFont = ctrl.Font;
                // Segoe UI: bỏ ép Bold — giữ style gốc của control để text không dày quá ở size nhỏ
                FontStyle style = oldFont.Style;
                // Floor 9pt để text luôn đọc rõ
                float size = oldFont.Size < 9F ? 9F : oldFont.Size;
                ctrl.Font = new Font(SegoeUI, size, style);

                if (ctrl is MenuStrip menuStrip)
                {
                    foreach (ToolStripMenuItem item in menuStrip.Items)
                        ApplyFontToMenuItem(item);
                }
                if (ctrl is ContextMenuStrip cms)
                {
                    foreach (ToolStripItem item in cms.Items)
                        ApplyFontToToolStripItem(item);
                }

                if (ctrl is DataGridView dgv)
                {
                    float dgvSize = dgv.DefaultCellStyle.Font?.Size is float s && s >= 9F ? s : 9F;
                    dgv.ColumnHeadersDefaultCellStyle.Font = new Font(SegoeUI, dgvSize, FontStyle.Bold);
                    dgv.DefaultCellStyle.Font              = new Font(SegoeUI, dgvSize, FontStyle.Regular);
                    dgv.RowHeadersDefaultCellStyle.Font    = new Font(SegoeUI, dgvSize, FontStyle.Regular);
                }

                if (ctrl.HasChildren)
                {
                    ctrl.SuspendLayout();
                    ApplyFontRecursive(ctrl);
                    ctrl.ResumeLayout(false);
                }
            }
        }

        private static void ApplyFontToMenuItem(ToolStripMenuItem menuItem)
        {
            float size = menuItem.Font.Size < 9F ? 9F : menuItem.Font.Size;
            menuItem.Font = new Font(SegoeUI, size, menuItem.Font.Style);
            foreach (ToolStripItem subItem in menuItem.DropDownItems)
                ApplyFontToToolStripItem(subItem);
        }

        private static void ApplyFontToToolStripItem(ToolStripItem item)
        {
            float size = item.Font.Size < 9F ? 9F : item.Font.Size;
            item.Font = new Font(SegoeUI, size, item.Font.Style);
            if (item is ToolStripMenuItem subMenuItem)
                ApplyFontToMenuItem(subMenuItem);
        }
    }
}
