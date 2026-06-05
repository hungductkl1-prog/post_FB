using System.Drawing;
using System.Reflection;

namespace ViewControl;

/// <summary>
/// Tải logo QN từ embedded resource và gắn vào Form Icon + AntdUI PageHeader.
/// Cache 1 lần để tránh decode PNG nhiều lần.
/// </summary>
internal static class AppBranding
{
    private static Image? _logoImage;
    private static Icon? _logoIcon;
    private static readonly object _lock = new();

        private const string PngResourceName = "ViewControl.Resources.logo_qn_auto_phone.png";

    public static Image? GetLogoImage()
    {
        if (_logoImage != null) return _logoImage;
        lock (_lock)
        {
            if (_logoImage != null) return _logoImage;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream(PngResourceName);
                if (stream == null)
                {
                    Program.LogLine($"AppBranding: resource {PngResourceName} không tồn tại.");
                    return null;
                }
                _logoImage = Image.FromStream(stream);
            }
            catch (Exception ex)
            {
                Program.LogLine($"AppBranding load PNG lỗi: {ex.Message}");
            }
            return _logoImage;
        }
    }

    public static Icon? GetLogoIcon()
    {
        if (_logoIcon != null) return _logoIcon;
        lock (_lock)
        {
            if (_logoIcon != null) return _logoIcon;
            var img = GetLogoImage();
            if (img == null) return null;
            try
            {
                using var bmp = new Bitmap(img, 64, 64);
                _logoIcon = Icon.FromHandle(bmp.GetHicon());
            }
            catch (Exception ex)
            {
                Program.LogLine($"AppBranding tạo Icon lỗi: {ex.Message}");
            }
            return _logoIcon;
        }
    }

    /// <summary>
    /// Áp dụng icon + show icon trên AntdUI PageHeader cho form.
    /// </summary>
    public static void ApplyTo(Form form, AntdUI.PageHeader windowBar)
    {
        try
        {
            var icon = GetLogoIcon();
            if (icon != null) form.Icon = icon;

            var img = GetLogoImage();
            if (img != null && windowBar != null)
            {
                windowBar.ShowIcon = true;
                windowBar.Icon = img;
            }
        }
        catch (Exception ex)
        {
            Program.LogLine($"AppBranding ApplyTo lỗi: {ex.Message}");
        }
    }
}
