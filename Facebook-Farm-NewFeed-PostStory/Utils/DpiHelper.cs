using System;
using System.Drawing;

namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    /// <summary>Thay AntdUI.Config.Dpi / AntdUI.Config.IsDark.</summary>
    public static class DpiHelper
    {
        /// <summary>Tỉ lệ DPI hiện tại (1.0 = 96 DPI).</summary>
        public static float Dpi
        {
            get
            {
                try
                {
                    using var g = Graphics.FromHwnd(IntPtr.Zero);
                    return g.DpiX / 96f;
                }
                catch
                {
                    return 1f;
                }
            }
        }

        /// <summary>Dark mode đã bị bỏ khi gỡ AntdUI — luôn light.</summary>
        public static bool IsDark => false;
    }
}
