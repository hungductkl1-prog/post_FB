using System.Drawing;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Sunny.Subdy.Common.Helper
{
    public partial class FontUtil
    {
        private static PrivateFontCollection _pfc = new PrivateFontCollection();
        private static FontFamily _fontRegular;
        public static FontFamily _fontBold;
        private static FontFamily _fontLight;
        private static FontFamily _fontMedium;
        public static FontFamily _fontSemiBold;
        [LibraryImport("gdi32")]
        private static partial IntPtr AddFontMemResourceEx(
    IntPtr pbFont,
    uint cbFont,
    IntPtr pdv,
    ref uint pcFonts);
        private static void AddFontFromResource(byte[] fontData)
        {
            IntPtr fontPtr = Marshal.AllocCoTaskMem(fontData.Length);
            Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
            _pfc.AddMemoryFont(fontPtr, fontData.Length);
            uint dummy = 0;
            IntPtr result = AddFontMemResourceEx(fontPtr, (uint)fontData.Length, IntPtr.Zero, ref dummy);
            if (result == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to add font resource.");
            }
            Marshal.FreeCoTaskMem(fontPtr);
        }
        public static void LoadCustomFonts()
        {
            try
            {
                AddFontFromResource(Properties.Resources.Quicksand_Regular);
                _fontRegular = _pfc.Families[_pfc.Families.Length - 1];
                AddFontFromResource(Properties.Resources.Quicksand_Bold);
                _fontBold = _pfc.Families[_pfc.Families.Length - 1];
                AddFontFromResource(Properties.Resources.Quicksand_Light);
                _fontLight = _pfc.Families[_pfc.Families.Length - 1];
                AddFontFromResource(Properties.Resources.Quicksand_Medium);
                _fontMedium = _pfc.Families[_pfc.Families.Length - 1];
                AddFontFromResource(Properties.Resources.Quicksand_SemiBold);
                _fontSemiBold = _pfc.Families[_pfc.Families.Length - 1];
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể load font tuỳ chỉnh:\n" + ex.Message,
                    "FontUtil", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }
        private static readonly PropertyInfo _doubleBufferedProp =
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void ApplyFontToAllControls(Control parent)
        {
            parent.SuspendLayout();
            try
            {
                // Bật double-buffering cho form cha để giảm nhấp nháy
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
                FontFamily newFamily = _fontSemiBold;
                FontStyle style = FontStyle.Bold;
                if (oldFont.Style == FontStyle.Bold && _fontBold != null)
                {
                    newFamily = _fontBold;
                    style = FontStyle.Bold;
                }
                ctrl.Font = new Font(newFamily, oldFont.Size, style);

                if (ctrl is MenuStrip menuStrip)
                {
                    foreach (ToolStripMenuItem item in menuStrip.Items)
                        ApplyFontToMenuItem(item, newFamily);
                }
                if (ctrl is ContextMenuStrip cms)
                {
                    foreach (ToolStripItem item in cms.Items)
                        ApplyFontToToolStripItem(item, newFamily);
                }

                if (ctrl is DataGridView dgv)
                {
                    dgv.ColumnHeadersDefaultCellStyle.Font = new Font(newFamily, oldFont.Size, oldFont.Style);
                    dgv.DefaultCellStyle.Font = new Font(newFamily, oldFont.Size, oldFont.Style);
                    dgv.RowHeadersDefaultCellStyle.Font = new Font(newFamily, oldFont.Size, oldFont.Style);
                }

                if (ctrl.HasChildren)
                {
                    ctrl.SuspendLayout();
                    ApplyFontRecursive(ctrl);
                    ctrl.ResumeLayout(false);
                }
            }
        }

        private static void ApplyFontToMenuItem(ToolStripMenuItem menuItem, FontFamily family)
        {
            menuItem.Font = new Font(family, menuItem.Font.Size, menuItem.Font.Style);
            foreach (ToolStripItem subItem in menuItem.DropDownItems)
                ApplyFontToToolStripItem(subItem, family);
        }

        private static void ApplyFontToToolStripItem(ToolStripItem item, FontFamily family)
        {
            item.Font = new Font(family, item.Font.Size, item.Font.Style);
            if (item is ToolStripMenuItem subMenuItem)
                ApplyFontToMenuItem(subMenuItem, family);
        }
    }
}
