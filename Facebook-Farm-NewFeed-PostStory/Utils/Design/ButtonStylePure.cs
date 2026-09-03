using System.Drawing;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Utils.Design
{
    /// <summary>
    /// Phiên bản WinForms thuần của ButtonStyle (thay AntdUI.Button bằng System.Windows.Forms.Button).
    /// Áp hierarchy màu/độ ưu tiên giống ButtonStyle cũ.
    /// </summary>
    public static class ButtonStylePure
    {
        public const int HeightPrimary = 38;
        public const int HeightSecondary = 34;
        public const int HeightTertiary = 32;

        private static void ApplySolid(Button b, Color back, Color fore, int minHeight)
        {
            if (b == null) return;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = back;
            b.ForeColor = fore;
            b.Font = FontScale.Body9Bold;
            b.UseVisualStyleBackColor = false;
            b.Cursor = Cursors.Hand;
            if (b.Height < minHeight) b.Height = minHeight;
        }

        public static void ApplyPrimary(Button b) => ApplySolid(b, ColorPalette.Primary, Color.White, HeightPrimary);
        public static void ApplySuccess(Button b) => ApplySolid(b, ColorPalette.Success, Color.White, HeightPrimary);
        public static void ApplyDanger(Button b) => ApplySolid(b, ColorPalette.Error, Color.White, HeightPrimary);
        public static void ApplyAccent(Button b) => ApplySolid(b, ColorPalette.Accent, Color.White, HeightPrimary);

        public static void ApplySecondary(Button b)
        {
            if (b == null) return;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = ColorPalette.Border;
            b.BackColor = ColorPalette.Surface;
            b.ForeColor = ColorPalette.TextSecondary;
            b.Font = FontScale.Body9Bold;
            b.UseVisualStyleBackColor = false;
            b.Cursor = Cursors.Hand;
            if (b.Height < HeightSecondary) b.Height = HeightSecondary;
        }

        public static void ApplyTertiary(Button b)
        {
            if (b == null) return;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Color.Transparent;
            b.ForeColor = ColorPalette.TextTertiary;
            b.Font = FontScale.Body9;
            b.UseVisualStyleBackColor = true;
            b.Cursor = Cursors.Hand;
            if (b.Height < HeightTertiary) b.Height = HeightTertiary;
        }
    }
}
