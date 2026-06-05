using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Utils.Design
{
    /// <summary>Spacing token — scale 4px.</summary>
    public static class Spacing
    {
        public const int Xs  = 4;
        public const int Sm  = 8;
        public const int Md  = 12;
        public const int Lg  = 16;
        public const int Xl  = 24;
        public const int Xxl = 32;

        // ── Padding presets ───────────────────────────────────
        public static Padding Form    => new Padding(Lg);                  // container chính
        public static Padding Section => new Padding(0, Xl, 0, Xl);        // giữa các section dọc
        public static Padding Field   => new Padding(0, 0, 0, Md);         // gap giữa 2 field
        public static Padding Inline  => new Padding(Sm, 0, 0, 0);         // icon + text
        public static Padding Toolbar => new Padding(Lg, Sm, Lg, Sm);      // top bar
    }

    /// <summary>Border radius token.</summary>
    public static class Radius
    {
        public const int Sm = 4;
        public const int Md = 6;
        public const int Lg = 8;
    }

    /// <summary>Elevation (shadow depth) token — dùng cho AntdUI shadow.</summary>
    public static class Elevation
    {
        public const int Level0 = 0;
        public const int Level1 = 4;
        public const int Level2 = 8;
        public const int Level3 = 16;
    }
}
