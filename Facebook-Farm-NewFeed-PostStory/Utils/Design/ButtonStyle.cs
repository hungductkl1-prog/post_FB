using AntdUI;
using System.Drawing;
using System.Windows.Forms;

namespace Facebook_Farm_NewFeed_PostStory.Utils.Design
{
    /// <summary>
    /// Button visual hierarchy tokens — 3 cấp ưu tiên:
    ///   Primary   = action chính (Chạy / Kết nối / Bắt đầu) — solid + glow nhẹ
    ///   Secondary = action phụ (Reload / Hiển thị / Tương tác) — outline ghost
    ///   Tertiary  = action ngoài lề (settings / filter / config) — icon-only ghost
    ///   Danger    = destructive (Dừng / Xóa) — solid error
    ///   Success   = confirm (Chạy) — solid success
    ///
    /// Mọi UI mới PHẢI dùng các method này thay vì set property thủ công,
    /// để bảo đảm hierarchy đồng nhất toàn app.
    /// </summary>
    public static class ButtonStyle
    {
        public const int HeightPrimary   = 38;
        public const int HeightSecondary = 34;
        public const int HeightTertiary  = 32;

        public static void ApplyPrimary(AntdUI.Button b)
        {
            if (b == null) return;
            b.Type        = TTypeMini.Primary;
            b.DefaultBack = ColorPalette.Primary;
            b.ForeColor   = Color.White;
            b.Radius      = Radius.Md;
            b.Shape       = TShape.Default;
            b.Font        = FontScale.Body9Bold;
            b.BorderWidth = 0;
            b.Ghost       = false;
            b.WaveSize    = 4;
            if (b.Height < HeightPrimary) b.Height = HeightPrimary;
        }

        public static void ApplySuccess(AntdUI.Button b)
        {
            if (b == null) return;
            b.Type        = TTypeMini.Success;
            b.DefaultBack = ColorPalette.Success;
            b.ForeColor   = Color.White;
            b.Radius      = Radius.Md;
            b.Shape       = TShape.Default;
            b.Font        = FontScale.Body9Bold;
            b.BorderWidth = 0;
            b.Ghost       = false;
            b.WaveSize    = 4;
            if (b.Height < HeightPrimary) b.Height = HeightPrimary;
        }

        public static void ApplyDanger(AntdUI.Button b)
        {
            if (b == null) return;
            b.Type        = TTypeMini.Error;
            b.DefaultBack = ColorPalette.Error;
            b.ForeColor   = Color.White;
            b.Radius      = Radius.Md;
            b.Shape       = TShape.Default;
            b.Font        = FontScale.Body9Bold;
            b.BorderWidth = 0;
            b.Ghost       = false;
            b.WaveSize    = 4;
            if (b.Height < HeightPrimary) b.Height = HeightPrimary;
        }

        public static void ApplyAccent(AntdUI.Button b)
        {
            if (b == null) return;
            b.Type        = TTypeMini.Warn;
            b.DefaultBack = ColorPalette.Accent;
            b.ForeColor   = Color.White;
            b.Radius      = Radius.Md;
            b.Shape       = TShape.Default;
            b.Font        = FontScale.Body9Bold;
            b.BorderWidth = 0;
            b.Ghost       = false;
            b.WaveSize    = 4;
            if (b.Height < HeightPrimary) b.Height = HeightPrimary;
        }

        public static void ApplySecondary(AntdUI.Button b)
        {
            if (b == null) return;
            b.Type        = TTypeMini.Default;
            b.Ghost       = true;
            b.BorderWidth = 1F;
            b.Radius      = Radius.Md;
            b.Shape       = TShape.Default;
            b.Font        = FontScale.Body9Bold;
            b.ForeColor   = ColorPalette.TextSecondary;
            if (b.Height < HeightSecondary) b.Height = HeightSecondary;
        }

        public static void ApplyTertiary(AntdUI.Button b)
        {
            if (b == null) return;
            b.Type        = TTypeMini.Default;
            b.Ghost       = true;
            b.BorderWidth = 0;
            b.Radius      = Radius.Md;
            b.Shape       = TShape.Default;
            b.Font        = FontScale.Body9;
            b.ForeColor   = ColorPalette.TextTertiary;
            if (b.Height < HeightTertiary) b.Height = HeightTertiary;
        }
    }
}
