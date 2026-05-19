using System.Drawing;

namespace LamToolAutoPhonePrime.Utils.Design
{
    /// <summary>
    /// Bảng màu chuẩn đồng bộ với AntdUI blue-6 theme.
    /// Mọi UI mới / refactor trong Views/ PHẢI dùng constant ở đây — KHÔNG dùng Color.FromArgb(...) rải rác.
    /// </summary>
    public static class ColorPalette
    {
        // ── Brand ──────────────────────────────────────────────
        public static readonly Color Primary       = Color.FromArgb(22, 119, 255); // AntdUI blue-6
        public static readonly Color PrimaryHover  = Color.FromArgb(64, 150, 255); // blue-5
        public static readonly Color PrimaryActive = Color.FromArgb(9, 88, 217);   // blue-7
        public static readonly Color PrimaryBg     = Color.FromArgb(230, 244, 255); // blue-1

        // ── Accent (CTA distinct from Primary) ─────────────────
        public static readonly Color Accent        = Color.FromArgb(249, 115, 22);  // orange-500
        public static readonly Color AccentHover   = Color.FromArgb(234, 88, 12);   // orange-600
        public static readonly Color AccentActive  = Color.FromArgb(194, 65, 12);   // orange-700

        // ── Semantic ───────────────────────────────────────────
        public static readonly Color Success   = Color.FromArgb(82, 196, 26);
        public static readonly Color SuccessBg = Color.FromArgb(246, 255, 237);
        public static readonly Color Warning   = Color.FromArgb(250, 173, 20);
        public static readonly Color WarningBg = Color.FromArgb(255, 251, 230);
        public static readonly Color Error     = Color.FromArgb(255, 77, 79);
        public static readonly Color ErrorBg   = Color.FromArgb(255, 241, 240);
        public static readonly Color Info      = Color.FromArgb(22, 119, 255);

        // ── Neutral (gray-10 → gray-1) ─────────────────────────
        public static readonly Color TextPrimary   = Color.FromArgb(38, 38, 38);   // gray-10
        public static readonly Color TextSecondary = Color.FromArgb(89, 89, 89);   // gray-8
        public static readonly Color TextTertiary  = Color.FromArgb(140, 140, 140); // gray-7
        public static readonly Color TextDisabled  = Color.FromArgb(191, 191, 191); // gray-6

        public static readonly Color Border      = Color.FromArgb(217, 217, 217); // gray-5
        public static readonly Color BorderLight = Color.FromArgb(240, 240, 240); // gray-4
        public static readonly Color Divider     = Color.FromArgb(240, 240, 240);

        public static readonly Color Background = Color.FromArgb(250, 250, 250); // gray-2
        public static readonly Color Surface    = Color.White;                    // gray-1
        public static readonly Color SurfaceAlt = Color.FromArgb(248, 249, 250); // zebra
        public static readonly Color Overlay    = Color.FromArgb(120, 0, 0, 0);

        // ── DataGrid row state ─────────────────────────────────
        public static readonly Color RowHover    = Color.FromArgb(245, 250, 255);
        public static readonly Color RowSelected = Color.FromArgb(230, 244, 255);

        // ── Device / Job states ────────────────────────────────
        public static readonly Color StateRunning    = Color.FromArgb(82, 196, 26);    // success green
        public static readonly Color StateIdle       = Color.FromArgb(140, 140, 140);  // gray-7
        public static readonly Color StateError      = Color.FromArgb(255, 77, 79);    // error red
        public static readonly Color StateCooldown   = Color.FromArgb(250, 173, 20);   // warning amber
        public static readonly Color StateWaitingOtp = Color.FromArgb(22, 119, 255);   // info blue
        public static readonly Color StateBanned     = Color.FromArgb(38, 38, 38);     // gray-10

        // ── Sidebar states ─────────────────────────────────────
        public static readonly Color SidebarBg          = Color.White;
        public static readonly Color SidebarItemHover   = Color.FromArgb(245, 247, 250);
        public static readonly Color SidebarItemActive  = Color.FromArgb(230, 244, 255); // primary-1
        public static readonly Color SidebarAccent      = Color.FromArgb(22, 119, 255);  // border-left accent
        public static readonly Color SidebarGroupLabel  = Color.FromArgb(140, 140, 140); // gray-7
    }
}
