using System.Drawing;

namespace Facebook_Farm_NewFeed_PostStory.Utils.Design
{
    /// <summary>
    /// Typography scale — Segoe UI (chuẩn Windows desktop).
    /// Floor 9pt. Caption / Caption8 được giữ lại để tương thích source nhưng đều clamp lên 9pt.
    /// </summary>
    public static class FontScale
    {
        public const string FamilyName = "Segoe UI";

        // ── Sizes (pt) ────────────────────────────────────────
        public const float Title   = 14F;
        public const float Heading = 12F;
        public const float Section = 10.5F;
        public const float Body    = 9F;   // ← MINIMUM

        /// <summary>Legacy caption — clamp lên Body để không xuất hiện chữ &lt; 9pt.</summary>
        public const float Caption = Body;

        // ── Pre-built fonts (mỗi lần gọi là Font mới — tránh shared disposal) ──
        public static Font TitleBold   => new Font(FamilyName, Title,   FontStyle.Bold);
        public static Font HeadingBold => new Font(FamilyName, Heading, FontStyle.Bold);
        public static Font SectionBold => new Font(FamilyName, Section, FontStyle.Bold);
        public static Font Body9       => new Font(FamilyName, Body,    FontStyle.Regular);
        public static Font Body9Bold   => new Font(FamilyName, Body,    FontStyle.Bold);

        /// <summary>Legacy alias — hiện tại = Body9 (clamp lên 9pt).</summary>
        public static Font Caption8    => Body9;

        /// <summary>
        /// Rescue Font cũ có size &lt; 9pt → trả về Font mới size = 9pt, family Segoe UI.
        /// </summary>
        public static Font EnsureMinBody(Font f)
        {
            if (f == null) return Body9;
            float size = f.Size < Body ? Body : f.Size;
            return new Font(FamilyName, size, f.Style);
        }
    }
}
