# 🛠️ UI/UX REFACTOR — IMPLEMENTATION GUIDE
**LamToolAutoPhonePrime · AntdUI 100% · Production-ready code**

> Tài liệu này KHÔNG phải audit. Đây là **bản thiết kế + code refactor trực tiếp**
> để dev senior cắm vào codebase hiện tại và apply dần.

---

## 1. Executive Summary

Project đã có nền AntdUI (`Utils/AntdHelper.cs` + `VietnameseLocalization` + 235 occurrences của AntdUI trong Views) và đã có `Sunny.Subdy.Common/Helper/FontUtil.cs` load font Quicksand. Việc refactor KHÔNG phải viết lại — là **xây 5 file foundation** rồi **apply tuần tự** theo priority.

**Nguyên tắc refactor**:
1. KHÔNG phá class `FontUtil` cũ (đang dùng font Quicksand từ resource) — chỉ *thêm* constant mới.
2. KHÔNG bỏ `DataGridView` gốc — apply `GridStyleHelper.Apply(dgv)` để nâng cấp style.
3. Mọi UI mới HOẶC control được sửa → **AntdUI 100%** (không trộn WinForms native).
4. `MessageBox.Show` trong `Views/` → `AntdHelper.Msg*` hoặc `AntdUI.Notification.*`.
5. Mỗi PR refactor 1 khu vực (không mega-PR).

---

## 2. 🚨 Priority Breakdown

| Priority | Vấn đề | Lý do đặt priority | Effort |
|---|---|---|---|
| **P0** | Foundation (ColorPalette, FontUtil.Extended, Spacing, GridStyleHelper, ErrorHandler) | Chặn mọi refactor sau — không có = tiếp tục hardcode | 1–2 ngày |
| **P0** | Loading indicator cho `fAddAccount` import 30s+ | User nghĩ app crash → click lại → duplicate | 2h |
| **P0** | Font 6.75pt ở `fQuanLyKichBan.Designer.cs:96` | Không đọc được | 15p |
| **P0** | `GridStyleHelper.Apply` cho `ucdgvAccount`, `ucManagerDevices`, `ucHistoriesJob` | Data là công cụ chính của app | 4h |
| **P0** | Replace `MessageBox.Show` lỗi → `ErrorHandler.Show` | Raw stack trace làm user hoang mang | 3h |
| **P1** | Navigation fMain → `AntdUI.Menu` + `PageHeader` | Gương mặt app, đang custom code rối | 1–2 ngày |
| **P1** | Bỏ `MaximumSize` trên form dialog | Vỡ layout trên 1366px / 4K | 30p |
| **P1** | Form input chuẩn hoá AntdUI.Input (placeholder + inline validation) | Giảm submit-rồi-lỗi | 1–2 ngày |
| **P1** | Nhớ column mapping import | Flow lặp đi lặp lại | 4h |
| **P2** | `fActionCenter` gom 90+ `fHD*` | Tác động lớn nhưng refactor nặng | 3–5 ngày |
| **P2** | Accessibility pass (AccessibleName, TabIndex, &-hotkey) | Tốt cho SLA, ít impact ngay | 1 ngày |
| **P2** | Dark mode toggle | Nice-to-have | 1 ngày |

---

## 3. 🧱 Design Foundation — Code Production-ready

### 3.1. `LamToolAutoPhonePrime/Utils/Design/ColorPalette.cs`

```csharp
using System.Drawing;

namespace LamToolAutoPhonePrime.Utils.Design
{
    /// <summary>
    /// Bảng màu chuẩn — đồng bộ với AntdUI blue-6 theme.
    /// MỌI màu trong Views/ PHẢI dùng constant ở đây, không dùng Color.FromArgb(...).
    /// </summary>
    public static class ColorPalette
    {
        // ── Brand ──────────────────────────────────────────────
        public static readonly Color Primary        = Color.FromArgb(22, 119, 255);   // AntdUI blue-6
        public static readonly Color PrimaryHover   = Color.FromArgb(64, 150, 255);
        public static readonly Color PrimaryActive  = Color.FromArgb( 9,  88, 217);
        public static readonly Color PrimaryBg      = Color.FromArgb(230, 244, 255);

        // ── Semantic ───────────────────────────────────────────
        public static readonly Color Success        = Color.FromArgb( 82, 196,  26);
        public static readonly Color SuccessBg      = Color.FromArgb(246, 255, 237);
        public static readonly Color Warning        = Color.FromArgb(250, 173,  20);
        public static readonly Color WarningBg      = Color.FromArgb(255, 251, 230);
        public static readonly Color Error          = Color.FromArgb(255,  77,  79);
        public static readonly Color ErrorBg        = Color.FromArgb(255, 241, 240);
        public static readonly Color Info           = Color.FromArgb(22, 119, 255);

        // ── Neutral (gray-10 → gray-1) ─────────────────────────
        public static readonly Color TextPrimary    = Color.FromArgb( 38,  38,  38);   // gray-10
        public static readonly Color TextSecondary  = Color.FromArgb( 89,  89,  89);   // gray-8
        public static readonly Color TextTertiary   = Color.FromArgb(140, 140, 140);   // gray-7
        public static readonly Color TextDisabled   = Color.FromArgb(191, 191, 191);   // gray-6
        public static readonly Color Border         = Color.FromArgb(217, 217, 217);   // gray-5
        public static readonly Color BorderLight    = Color.FromArgb(240, 240, 240);   // gray-4
        public static readonly Color Divider        = Color.FromArgb(240, 240, 240);
        public static readonly Color Background     = Color.FromArgb(250, 250, 250);   // gray-2
        public static readonly Color Surface        = Color.White;                     // gray-1
        public static readonly Color SurfaceAlt     = Color.FromArgb(248, 249, 250);   // zebra row
        public static readonly Color Overlay        = Color.FromArgb(120,   0,   0, 0);

        // ── DataGrid selection ─────────────────────────────────
        public static readonly Color RowHover       = Color.FromArgb(245, 250, 255);
        public static readonly Color RowSelected    = Color.FromArgb(230, 244, 255);
    }
}
```

### 3.2. `LamToolAutoPhonePrime/Utils/Design/FontScale.cs`

> KHÔNG sửa `Sunny.Subdy.Common/Helper/FontUtil.cs`. Thêm file mới dùng Quicksand cùng `FontUtil._fontRegular` / `_fontBold`.

```csharp
using System.Drawing;
using Sunny.Subdy.Common.Helper;

namespace LamToolAutoPhonePrime.Utils.Design
{
    /// <summary>
    /// Typography scale cho desktop. Minimum 9pt cho body, 8.25pt chỉ cho caption.
    /// Mọi Font trong Views/ mới PHẢI dùng scale này.
    /// </summary>
    public static class FontScale
    {
        // Sizes (pt)
        public const float Title    = 14F;   // 18.6px
        public const float Heading  = 12F;   // 16px
        public const float Section  = 10.5F; // 14px
        public const float Body     = 9F;    // 12px  ← MINIMUM
        public const float Caption  = 8.25F; // 11px  ← chỉ cho chú thích
        // < 8.25F BANNED

        // Pre-built fonts (dùng Quicksand đã load)
        public static Font TitleBold    => new(FontUtil._fontBold,     Title,   FontStyle.Bold);
        public static Font HeadingBold  => new(FontUtil._fontBold,     Heading, FontStyle.Bold);
        public static Font SectionBold  => new(FontUtil._fontSemiBold, Section, FontStyle.Bold);
        public static Font Body9        => new(FontUtil._fontRegular ?? FontUtil._fontBold, Body);
        public static Font Body9Bold    => new(FontUtil._fontBold,     Body,    FontStyle.Bold);
        public static Font Caption8     => new(FontUtil._fontRegular ?? FontUtil._fontBold, Caption);

        /// <summary>Đảm bảo Font truyền vào có size ≥ 9pt. Dùng để “rescue” các Font cũ hardcode nhỏ.</summary>
        public static Font EnsureMinBody(Font f) =>
            f == null ? Body9 : (f.Size < Body ? new Font(f.FontFamily, Body, f.Style) : f);
    }
}
```

### 3.3. `LamToolAutoPhonePrime/Utils/Design/Spacing.cs`

```csharp
using System.Windows.Forms;

namespace LamToolAutoPhonePrime.Utils.Design
{
    public static class Spacing
    {
        public const int Xs  = 4;
        public const int Sm  = 8;
        public const int Md  = 12;
        public const int Lg  = 16;
        public const int Xl  = 24;
        public const int Xxl = 32;

        public static Padding Form         => new(Lg);
        public static Padding Section      => new(0, Xl, 0, Xl);
        public static Padding Field        => new(0, 0, 0, Md);     // khoảng cách giữa các field dọc
        public static Padding Inline       => new(Sm, 0, 0, 0);     // khoảng cách giữa icon + text
        public static Padding Toolbar      => new(Lg, Sm, Lg, Sm);
    }

    public static class Radius
    {
        public const int Sm = 4;
        public const int Md = 6;
        public const int Lg = 8;
    }

    public static class Elevation
    {
        // Dùng cho AntdUI shadow depth
        public const int Level0 = 0;
        public const int Level1 = 4;
        public const int Level2 = 8;
        public const int Level3 = 16;
    }
}
```

### 3.4. `LamToolAutoPhonePrime/Utils/Design/GridStyleHelper.cs`

```csharp
using System.Drawing;
using System.Windows.Forms;

namespace LamToolAutoPhonePrime.Utils.Design
{
    /// <summary>
    /// Áp style chuẩn lên DataGridView. Gọi 1 lần sau InitializeComponent().
    /// </summary>
    public static class GridStyleHelper
    {
        public const int RowHeightComfort = 36;
        public const int RowHeightDense   = 32;
        public const int HeaderHeight     = 40;

        public static void Apply(DataGridView dgv, int rowHeight = RowHeightComfort)
        {
            dgv.EnableHeadersVisualStyles    = false;
            dgv.BorderStyle                  = BorderStyle.None;
            dgv.BackgroundColor              = ColorPalette.Surface;
            dgv.GridColor                    = ColorPalette.BorderLight;
            dgv.RowHeadersVisible            = false;
            dgv.AllowUserToAddRows           = false;
            dgv.AllowUserToResizeRows        = false;
            dgv.AllowUserToResizeColumns     = true;
            dgv.MultiSelect                  = true;
            dgv.SelectionMode                = DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoSizeRowsMode             = DataGridViewAutoSizeRowsMode.None;
            dgv.RowTemplate.Height           = rowHeight;
            dgv.ColumnHeadersHeight          = HeaderHeight;
            dgv.ColumnHeadersHeightSizeMode  = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.DefaultCellStyle             = BuildCellStyle();
            dgv.ColumnHeadersDefaultCellStyle= BuildHeaderStyle();
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = ColorPalette.SurfaceAlt };
            dgv.CellMouseEnter += (s, e) => HoverRow(dgv, e.RowIndex, true);
            dgv.CellMouseLeave += (s, e) => HoverRow(dgv, e.RowIndex, false);
        }

        private static DataGridViewCellStyle BuildHeaderStyle() => new()
        {
            BackColor          = ColorPalette.Background,
            ForeColor          = ColorPalette.TextPrimary,
            Font               = FontScale.Body9Bold,
            Alignment          = DataGridViewContentAlignment.MiddleLeft,
            Padding            = new Padding(Spacing.Md, 0, Spacing.Md, 0),
            SelectionBackColor = ColorPalette.Background,
            SelectionForeColor = ColorPalette.TextPrimary,
            WrapMode           = DataGridViewTriState.False
        };

        private static DataGridViewCellStyle BuildCellStyle() => new()
        {
            BackColor          = ColorPalette.Surface,
            ForeColor          = ColorPalette.TextPrimary,
            Font               = FontScale.Body9,
            Alignment          = DataGridViewContentAlignment.MiddleLeft,
            Padding            = new Padding(Spacing.Md, 0, Spacing.Md, 0),
            SelectionBackColor = ColorPalette.RowSelected,
            SelectionForeColor = ColorPalette.TextPrimary,
            WrapMode           = DataGridViewTriState.False
        };

        private static void HoverRow(DataGridView dgv, int rowIndex, bool enter)
        {
            if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
            var row = dgv.Rows[rowIndex];
            if (row.Selected) return;
            row.DefaultCellStyle.BackColor =
                enter ? ColorPalette.RowHover
                      : (rowIndex % 2 == 1 ? ColorPalette.SurfaceAlt : ColorPalette.Surface);
        }
    }
}
```

### 3.5. `LamToolAutoPhonePrime/Utils/Design/ErrorHandler.cs`

```csharp
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;

namespace LamToolAutoPhonePrime.Utils.Design
{
    /// <summary>
    /// Map exception → user-friendly message (VI).
    /// Luôn show qua AntdUI.Notification (non-blocking), log stack trace cho dev.
    /// </summary>
    public static class ErrorHandler
    {
        public static string ToUserMessage(Exception ex) => ex switch
        {
            ArgumentNullException       => "Vui lòng điền đầy đủ thông tin bắt buộc.",
            FileNotFoundException f     => $"Không tìm thấy file: {Path.GetFileName(f.FileName ?? "")}. Kiểm tra đường dẫn.",
            DirectoryNotFoundException  => "Không tìm thấy thư mục. Kiểm tra đường dẫn.",
            UnauthorizedAccessException => "Không có quyền truy cập. Thử chạy với quyền Administrator.",
            TimeoutException            => "Hết thời gian chờ. Kiểm tra kết nối mạng và thử lại.",
            HttpRequestException h      => $"Lỗi kết nối: {h.Message}. Kiểm tra mạng hoặc proxy.",
            DbUpdateException           => "Không lưu được dữ liệu. Thử lại hoặc khởi động lại app.",
            InvalidOperationException   => "Thao tác không hợp lệ trong trạng thái hiện tại.",
            FormatException             => "Dữ liệu nhập sai định dạng. Kiểm tra lại.",
            OutOfMemoryException        => "Thiếu bộ nhớ. Thử giảm số lượng xử lý cùng lúc.",
            _                           => $"Đã xảy ra lỗi: {ex.Message}"
        };

        public static void Show(Form form, Exception ex, string context = null)
        {
            if (ex == null) return;
            var msg = ToUserMessage(ex);
            var title = string.IsNullOrEmpty(context) ? "Lỗi" : context;

            AntdUI.Notification.error(form, title, msg, AntdUI.TAlignFrom.BR);
            Debug.WriteLine($"[ERROR] {context} | {ex}");
        }

        /// <summary>Bọc action an toàn, tự show error nếu lỗi, trả về true nếu thành công.</summary>
        public static bool SafeRun(Form form, Action action, string context = null)
        {
            try { action(); return true; }
            catch (Exception ex) { Show(form, ex, context); return false; }
        }

        public static async System.Threading.Tasks.Task<bool> SafeRunAsync(
            Form form, Func<System.Threading.Tasks.Task> action, string context = null)
        {
            try { await action(); return true; }
            catch (Exception ex) { Show(form, ex, context); return false; }
        }
    }
}
```

### 3.6. Mở rộng `AntdHelper.cs` (thêm vào file cũ, không thay)

```csharp
// ── Notification (toast góc dưới phải, không chặn UI) ───────────
public static void NotifySuccess(Form form, string title, string desc) =>
    AntdUI.Notification.success(form, title, desc, AntdUI.TAlignFrom.BR);

public static void NotifyError(Form form, string title, string desc) =>
    AntdUI.Notification.error(form, title, desc, AntdUI.TAlignFrom.BR);

public static void NotifyWarn(Form form, string title, string desc) =>
    AntdUI.Notification.warn(form, title, desc, AntdUI.TAlignFrom.BR);

// ── LoadingScope: using-pattern cho async không cần callback ───
public static async System.Threading.Tasks.Task WithLoading(
    Control control, string text, Func<System.Threading.Tasks.Task> action)
{
    await AntdUI.Spin.open(control, new AntdUI.Spin.Config { Text = text }, async _ => await action());
}

// ── Debounce helper (dùng cho search box) ───────────────────────
public static System.Windows.Forms.Timer Debounce(Action action, int ms = 300)
{
    var t = new System.Windows.Forms.Timer { Interval = ms };
    t.Tick += (_, __) => { t.Stop(); action(); };
    return t; // caller gọi t.Stop(); t.Start(); mỗi lần gõ
}
```

---

## 4. 🎨 Refactor UI theo AntdUI (code mẫu từng khu vực)

### 4.1. Main Form — Sidebar + PageHeader

**Cũ** (`fMain.cs:69-99`): panel tự vẽ + leftBar tự ẩn hiện.
**Mới**: `AntdUI.Menu` Inline + `AntdUI.PageHeader`.

```csharp
// fMain.cs — refactor skeleton
public partial class fMain : AntdUI.Window
{
    private AntdUI.Menu _sideMenu;
    private AntdUI.PageHeader _header;
    private Panel _content;

    public fMain()
    {
        InitializeComponent();
        BuildLayout();
        FontUtil.ApplyFontToAllControls(this);
    }

    private void BuildLayout()
    {
        BackColor = ColorPalette.Background;

        _sideMenu = new AntdUI.Menu
        {
            Dock       = DockStyle.Left,
            Width      = 220,
            Mode       = AntdUI.TMenuMode.Inline,
            BackColor  = ColorPalette.Surface,
            Font       = FontScale.Body9,
            Items      =
            {
                new AntdUI.MenuItem("account",  "Tài khoản")   { IconSvg = SvgIcons.User },
                new AntdUI.MenuItem("device",   "Thiết bị")    { IconSvg = SvgIcons.Mobile },
                new AntdUI.MenuItem("script",   "Kịch bản")    { IconSvg = SvgIcons.Flow },
                new AntdUI.MenuItem("history",  "Lịch sử")     { IconSvg = SvgIcons.Clock },
                new AntdUI.MenuItem("setting",  "Cài đặt")     { IconSvg = SvgIcons.Gear  }
            }
        };
        _sideMenu.SelectChanged += (_, e) => NavigateTo((string)e.Value.Tag);

        _header = new AntdUI.PageHeader
        {
            Dock           = DockStyle.Top,
            Height         = 56,
            ShowIcon       = false,
            UseTitleFont   = true,
            Font           = FontScale.HeadingBold,
            ForeColor      = ColorPalette.TextPrimary,
            DividerShow    = true,
            Text           = "Tài khoản",
            SubText        = "Quản lý danh sách Facebook account"
        };

        _content = new Panel { Dock = DockStyle.Fill, Padding = Spacing.Form, BackColor = ColorPalette.Background };

        Controls.Add(_content);
        Controls.Add(_header);
        Controls.Add(_sideMenu);
    }

    private void NavigateTo(string key)
    {
        _content.Controls.Clear();
        UserControl uc = key switch
        {
            "account"  => new ucdgvAccount("facebook") { Dock = DockStyle.Fill },
            "device"   => new ucManagerDevices()       { Dock = DockStyle.Fill },
            "history"  => new ucHistoriesJob()         { Dock = DockStyle.Fill },
            "script"   => new ucScriptManager()        { Dock = DockStyle.Fill },
            _          => null
        };
        if (uc != null) _content.Controls.Add(uc);
        _header.Text = _sideMenu.SelectItem?.Text ?? "";
    }
}
```

**UX improvement**: 1 sidebar duy nhất, highlight active tự động, breadcrumb title đổi theo menu, không flicker khi chuyển.

---

### 4.2. Form nhập liệu — AntdUI.Input + inline validation

**Cũ** (`fLogin.Designer.cs`): `TextBox` + `MessageBox.Show("Sai mật khẩu")` sau submit.
**Mới**: `AntdUI.Input` với `Status = Error` + `helpText`.

```csharp
// fLogin.cs — snippet
private AntdUI.Input _txtEmail, _txtPass;
private AntdUI.Button _btnLogin;

private void BuildForm()
{
    _txtEmail = new AntdUI.Input
    {
        Location       = new Point(Spacing.Lg, 80),
        Size           = new Size(320, 40),
        PlaceholderText= "email@example.com",
        PrefixText     = "✉",
        AllowClear     = true,
        Font           = FontScale.Body9
    };
    _txtEmail.LostFocus += (_, __) => ValidateEmail();

    _txtPass = new AntdUI.Input
    {
        Location       = new Point(Spacing.Lg, 140),
        Size           = new Size(320, 40),
        PlaceholderText= "Mật khẩu",
        PrefixText     = "🔒",
        UseSystemPasswordChar = true,
        Font           = FontScale.Body9
    };

    _btnLogin = new AntdUI.Button
    {
        Location  = new Point(Spacing.Lg, 200),
        Size      = new Size(320, 40),
        Text      = "&Đăng nhập",
        Type      = AntdUI.TTypeMini.Primary,
        Font      = FontScale.Body9Bold
    };
    _btnLogin.Click += async (_, __) => await OnLoginAsync();

    Controls.AddRange(new Control[] { _txtEmail, _txtPass, _btnLogin });
}

private bool ValidateEmail()
{
    var v = _txtEmail.Text?.Trim();
    if (string.IsNullOrEmpty(v) || !v.Contains('@'))
    {
        _txtEmail.Status  = AntdUI.TType.Error;
        _txtEmail.Suffix  = "Email không hợp lệ";
        return false;
    }
    _txtEmail.Status = AntdUI.TType.None;
    _txtEmail.Suffix = null;
    return true;
}

private async System.Threading.Tasks.Task OnLoginAsync()
{
    if (!ValidateEmail()) return;

    await AntdHelper.WithLoading(this, "Đang đăng nhập...", async () =>
    {
        try
        {
            await SubdyClient.LoginAsync(_txtEmail.Text, _txtPass.Text);
            AntdHelper.NotifySuccess(this, "Thành công", "Đăng nhập thành công");
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            ErrorHandler.Show(this, ex, "Đăng nhập thất bại");
        }
    });
}
```

**UX improvement**: lỗi hiện inline ngay khi rời field, không MessageBox chặn, loading rõ ràng, Alt+Đ hotkey login.

---

### 4.3. Button hierarchy

```csharp
// 1 primary duy nhất cho action chính
var save = new AntdUI.Button { Text = "&Lưu",   Type = AntdUI.TTypeMini.Primary,  IconSvg = SvgIcons.Save };
// default cho action phụ
var cancel = new AntdUI.Button { Text = "&Hủy", Type = AntdUI.TTypeMini.Default };
// link cho thao tác gián tiếp
var help  = new AntdUI.Button { Text = "?",     Type = AntdUI.TTypeMini.Text };

// Toolbar icon button
var refresh = new AntdUI.Button
{
    Size    = new Size(32, 32),
    Ghost   = true,
    IconSvg = SvgIcons.Reload,
    ShowArrow = false,
    ToolTip = "Làm mới (F5)"
};
```

**Quy tắc**: tối đa 1 primary/form, đặt bên phải footer; Hủy/back bên trái primary.

---

### 4.4. Modal / Confirm / Notification

**Replace pattern** (search-replace trên toàn `Views/`):

| Cũ | Mới |
|---|---|
| `MessageBox.Show(this, msg, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error)` | `ErrorHandler.Show(this, ex)` |
| `MessageBox.Show(this, msg, "Thông báo")` | `AntdHelper.NotifySuccess/Warn/Info(this, title, desc)` |
| `if (MessageBox.Show(..., "OKCancel") == DialogResult.OK)` | `if (AntdHelper.Confirm(this, title, content))` |
| `form.ShowDialog()` (dialog nhỏ) | `AntdUI.Modal.open(new AntdUI.Modal.Config(this, title, panel))` |

Ví dụ confirm xoá:
```csharp
if (AntdHelper.Confirm(this, "Xác nhận xoá",
    $"Bạn chắc chắn muốn xoá {count} tài khoản? Thao tác không thể hoàn tác."))
{
    await AntdHelper.WithLoading(this, "Đang xoá...", async () =>
    {
        await _svc.DeleteAsync(ids);
        AntdHelper.NotifySuccess(this, "Đã xoá", $"Đã xoá {count} tài khoản");
        ReloadGrid();
    });
}
```

---

### 4.5. DataGrid — ucdgvAccount refactor

**Cũ** (`ucdgvAccount.cs:109-136`): Paint event vẽ text empty state, không có loading.
**Mới**: `GridStyleHelper.Apply` + `AntdUI.Empty` + Spin khi load.

```csharp
// ucdgvAccount.cs — sau InitializeComponent()
public ucdgvAccount(string platform)
{
    InitializeComponent();
    _platform = platform;

    GridStyleHelper.Apply(dataGridView1, rowHeight: 36);
    BuildEmptyState();

    FontUtil.ApplyFontToAllControls(this);
    _ = ReloadAsync();
}

private AntdUI.Empty _empty;
private void BuildEmptyState()
{
    _empty = new AntdUI.Empty
    {
        Dock       = DockStyle.Fill,
        Text       = "Chưa có tài khoản nào",
        Description= "Nhấn \"Thêm tài khoản\" để bắt đầu",
        Visible    = false
    };
    dataGridView1.Parent.Controls.Add(_empty);
    _empty.BringToFront();
}

public async System.Threading.Tasks.Task ReloadAsync()
{
    await AntdHelper.WithLoading(this, "Đang tải dữ liệu...", async () =>
    {
        try
        {
            var data = await System.Threading.Tasks.Task.Run(() =>
                ScriptContext.GetByPlatform(_platform));
            BindData(data);
            _empty.Visible = data.Count == 0;
            dataGridView1.Visible = data.Count > 0;
        }
        catch (Exception ex)
        {
            ErrorHandler.Show(FindForm(), ex, "Tải danh sách tài khoản");
        }
    });
}

// Search debounce
private System.Windows.Forms.Timer _searchTimer;
private void txt_search_TextChanged(object s, EventArgs e)
{
    _searchTimer?.Stop();
    _searchTimer = AntdHelper.Debounce(() => LoadSearchList(txt_search.Text), 300);
    _searchTimer.Start();
}
```

**UX improvement**:
- Header xanh, zebra row, row 36px → scan nhanh
- Empty state có icon + hint action
- Search không giật mỗi keystroke
- Loading rõ ràng, lỗi không crash

---

### 4.6. Loading / Feedback rules

```csharp
// < 2s: KHÔNG feedback (instant)
var n = await _svc.CountAsync();

// 2–10s: Spin inline
await AntdHelper.WithLoading(this, "Đang import...", async () =>
{
    await _svc.ImportAsync(lines);
});

// > 10s: overlay + progress đếm được
await AntdUI.Spin.open(this, new AntdUI.Spin.Config
{
    Text = "Đang xử lý 0/1000..."
}, async cfg =>
{
    for (int i = 0; i < total; i++)
    {
        await ProcessOneAsync(items[i]);
        cfg.Text = $"Đang xử lý {i + 1}/{total}...";
        cfg.Invalidate();
    }
});
```

**Áp ngay cho `fAddAccount.cs:81-127`**: wrap `uiSymbolButton1_ClickSafe` trong `WithLoading` + `NotifySuccess` sau khi xong + `ErrorHandler.Show` thay cho MessageBox.

---

## 5. 🧭 Navigation Refactor — fActionCenter

**Vấn đề**: 90+ form `fHD*.cs` mỗi form 1 cửa sổ, user không biết tìm đâu.

**Kiến trúc mới**:

```
fActionCenter (1 cửa sổ duy nhất)
├── Top: AntdUI.Input (search action)
├── Left:  AntdUI.Tabs (Tương tác | Đăng bài | Bảo mật | Profile | Nhóm | Page)
│          mỗi tab = AntdUI.Menu list các action trong nhóm
└── Right: Content panel, load UserControl uHD*
```

**Migration plan**:
1. Tạo base `UserControl ucHDBase` có method `Run(AccountSelection sel)`.
2. Script PowerShell biến `fHD*.cs` (Form) → `ucHD*.cs` (UserControl): chỉ đổi inherit + bỏ form properties.
3. `fActionCenter` load UC bằng reflection theo category.

```csharp
public partial class fActionCenter : AntdUI.Window
{
    private AntdUI.Tabs    _tabs;
    private AntdUI.Input   _search;
    private Panel          _content;
    private ucHDBase       _current;

    private static readonly Dictionary<string, List<ActionDef>> Categories = new()
    {
        ["Tương tác"] = new() {
            new("Xem Story",     typeof(ucHDXemStory)),
            new("Xem Reel",      typeof(ucHDXemReel)),
            new("Tương tác Wall",typeof(ucHDTuongTacWall)),
        },
        ["Đăng bài"] = new() {
            new("Đăng bài Tường",typeof(ucHDDangBaiTuong)),
            new("Đăng bài Nhóm", typeof(ucHDDangBaiNhom)),
            new("Đăng Reel",     typeof(ucHDDangReel)),
        },
        ["Bảo mật"] = new() {
            new("Bật 2FA",        typeof(ucHDOnOff2FA)),
            new("Xoá thiết bị tin cậy", typeof(ucHDXoaThietBiTinCay)),
        },
        // ... 6 nhóm × ~15 action = 90
    };

    private record ActionDef(string Name, Type ControlType);

    public fActionCenter()
    {
        InitializeComponent();
        BuildLayout();
    }

    private void BuildLayout()
    {
        Padding = Spacing.Form;

        _search = new AntdUI.Input
        {
            Dock = DockStyle.Top, Height = 36,
            PlaceholderText = "🔍 Tìm action (VD: xem story)",
            AllowClear = true,
            Font = FontScale.Body9
        };
        _search.TextChanged += (_, __) => FilterActions(_search.Text);

        _tabs = new AntdUI.Tabs { Dock = DockStyle.Left, Width = 280 };
        foreach (var cat in Categories)
        {
            var menu = BuildMenu(cat.Value);
            _tabs.Pages.Add(new AntdUI.TabPage { Text = cat.Key, Control = menu });
        }

        _content = new Panel { Dock = DockStyle.Fill, Padding = Spacing.Form };

        Controls.Add(_content);
        Controls.Add(_tabs);
        Controls.Add(_search);
    }

    private AntdUI.Menu BuildMenu(List<ActionDef> actions)
    {
        var menu = new AntdUI.Menu { Dock = DockStyle.Fill, Font = FontScale.Body9 };
        foreach (var a in actions)
            menu.Items.Add(new AntdUI.MenuItem(a.Name, a.Name) { Tag = a });
        menu.SelectChanged += (_, e) => LoadAction((ActionDef)e.Value.Tag);
        return menu;
    }

    private void LoadAction(ActionDef def)
    {
        _current?.Dispose();
        _content.Controls.Clear();
        _current = (ucHDBase)Activator.CreateInstance(def.ControlType);
        _current.Dock = DockStyle.Fill;
        _content.Controls.Add(_current);
    }

    private void FilterActions(string keyword) { /* filter menu items, highlight tab có match */ }
}
```

**UX improvement**:
- 1 cửa sổ thay 90, Alt-Tab sạch
- Search action theo tên VI
- Back/forward được (stack UC)
- Consistency tuyệt đối — mọi action cùng pattern

---

## 6. ⚡ Quick Wins — apply ngay hôm nay

| # | Việc | File / Line | Impact | Thời gian |
|---|---|---|---|---|
| 1 | Tạo 5 file design foundation (mục 3) | `Utils/Design/*` (new) | Chặn hardcode mới, nền cho mọi refactor | 2h |
| 2 | `GridStyleHelper.Apply(dataGridView1)` sau InitializeComponent | `ucdgvAccount.cs:28`, `ucManagerDevices.cs`, `ucHistoriesJob.cs` | Grid đẹp ngay, user thấy khác biệt | 1h |
| 3 | Wrap import bằng `AntdHelper.WithLoading` | `fAddAccount.cs:81-127` | Không còn cảm giác crash | 1h |
| 4 | Sửa font 6.75pt → `FontScale.Caption8` | `fQuanLyKichBan.Designer.cs:96` | Đọc được | 15p |
| 5 | Bỏ `MaximumSize = MinimumSize` | `fFolder.Designer.cs:280-282`, `fSelectByUid`, `fUpdateAuto` | Resize được | 30p |
| 6 | Thay mọi `MessageBox.Show` lỗi → `ErrorHandler.Show` | grep `MessageBox.Show` trong `Views/` | Message thân thiện | 2–3h |
| 7 | PlaceholderText cho `AntdUI.Input` rỗng | `fImportProxy.Designer.cs:49`, `fAddAccount` | Giảm confusion | 1h |
| 8 | Debounce search 300ms | `fQuanLyKichBan.cs:113`, `ucdgvAccount.cs` search | Hết giật khi gõ | 30p |
| 9 | `AntdUI.Empty` thay Paint empty state | `ucdgvAccount.cs:109-136` | Chuẩn, có action suggest | 1h |
| 10 | `&`-hotkey + `AccessibleName` cho 10 button chính | fMain, fAddAccount, fLogin | Keyboard-first | 1h |

**Tổng ~ 1 ngày = user thấy khác biệt rõ rệt.**

---

## 7. 🧠 UX Rules (Desktop) — bắt buộc tuân thủ

| Hạng mục | Chuẩn | Note |
|---|---|---|
| **Font size** | Body ≥ 9pt (12px), Caption 8.25pt là minimum | < 8.25pt cấm |
| **Row height bảng** | 36px comfort, 32px dense, 44px relaxed | Set qua `GridStyleHelper.RowHeightComfort` |
| **Form padding** | 16px container, 24px giữa section | `Spacing.Form`, `Spacing.Section` |
| **Label-input gap** | 8px dọc, 12px ngang | `Spacing.Sm`, `Spacing.Md` |
| **Button hierarchy** | 1 primary / form, ≤ 2 default, còn lại text/link | Primary bên phải footer |
| **Loading < 2s** | Không feedback | Tránh flicker |
| **Loading 2–10s** | `AntdHelper.WithLoading` Spin | |
| **Loading > 10s** | Spin + progress đếm được + Cancel | User biết còn bao lâu |
| **Error message** | 3 câu tối đa: *Chuyện gì + Vì sao + Làm gì tiếp* | `ErrorHandler.ToUserMessage` |
| **Form resize** | `MinimumSize` có, `MaximumSize` KHÔNG | Trừ dialog confirm nhỏ |
| **Multi-monitor** | Dialog `CenterParent`, main `Manual + nhớ vị trí` | Lưu qua ConfigHelper |
| **DPI** | Per-monitor V2 trong `app.manifest` | |
| **Notification** | `Notification` cho thành công/lỗi, `Modal` cho confirm | Không MessageBox |

---

## 8. 🗺️ Roadmap Triển Khai

### Phase 1 — Foundation (Tuần 1, 3–5 ngày)
**Mục tiêu**: có sẵn hạ tầng, mọi refactor sau chỉ việc gọi.

- [ ] D1: Tạo 5 file mục 3 (ColorPalette, FontScale, Spacing, GridStyleHelper, ErrorHandler)
- [ ] D1: Mở rộng `AntdHelper.cs` (Notify*, WithLoading, Debounce)
- [ ] D2: Đăng ký `VietnameseLocalization` + `AntdUI.Style.Load` trong `Program.cs`
- [ ] D2: Quick wins #2–#10 (mục 6)
- [ ] D3: Search-replace `MessageBox.Show` → `ErrorHandler.Show` / `AntdHelper.Msg*`
- [ ] D3: Apply `GridStyleHelper` cho TẤT CẢ DataGridView trong `Views/`
- [ ] D4–D5: Test regression, chốt design token

### Phase 2 — Core Screens (Tuần 2–3)
Thứ tự theo impact user:

1. **fMain** — AntdUI.Menu sidebar + PageHeader (mục 4.1)
2. **ucdgvAccount + fAddAccount** — Empty state, Spin, inline validation (4.2 + 4.5)
3. **fQuanLyKichBan + fChiTietKichBan** — PageHeader, bỏ font 6.75pt, debounce
4. **fLogin + fRegister** — AntdUI.Input + validation + hotkey
5. **fImportProxy + fAddUsercontrol** — Nhớ column mapping, WithLoading

### Phase 3 — Full Refactor (Tuần 4–6)
1. **fActionCenter** thay 90+ `fHD*` (mục 5)
2. Form pooling cho `fChiTietKichBan`, `fAddUsercontrol`
3. Accessibility pass (AccessibleName, TabIndex, &-hotkey) cho toàn bộ form P0/P1
4. Responsiveness pass (bỏ MaximumSize, set MinimumSize, TableLayoutPanel)
5. Dark mode toggle + persist preference

---

## 9. ✅ Checklist Nghiệm Thu

### Foundation
- [ ] 5 file design foundation tồn tại tại `Utils/Design/`
- [ ] `grep "Color.FromArgb"` trong `Views/` < 20 match (baseline 1380)
- [ ] `grep "MessageBox.Show"` trong `Views/` = 0 match
- [ ] `grep "F,"` font size < 9pt trong `*.Designer.cs` = 0 match
- [ ] `MaximumSize` trong content form = 0 match

### Component
- [ ] Mọi DataGridView trong `Views/` gọi `GridStyleHelper.Apply` trong constructor
- [ ] Mọi thao tác > 2s có `AntdHelper.WithLoading`
- [ ] Mọi form input có ít nhất 1 inline validation (`Status = Error` + `Suffix`)
- [ ] Mọi form con có `AntdUI.PageHeader` với title + back icon
- [ ] Mọi empty state dùng `AntdUI.Empty`

### Navigation
- [ ] `fMain` dùng `AntdUI.Menu` cho sidebar
- [ ] `fActionCenter` hoạt động, gom tối thiểu 50 action
- [ ] Search action hoạt động, debounce 300ms
- [ ] Back/Esc đóng modal đúng

### Accessibility
- [ ] Tab order logic trên 10 form P0 (kiểm thủ công)
- [ ] Alt-hotkey cho button chính (Thêm / Lưu / Hủy / Đóng)
- [ ] `AccessibleName` trên button/input P0
- [ ] Contrast ≥ 4.5:1 cho text trên nền (kiểm với tool)

### Responsiveness
- [ ] Mọi form resize được đến 1920×1080 không vỡ
- [ ] Mọi form mở được trên 1366×768 không scroll ngang
- [ ] DPI 125%/150% hiển thị đúng
- [ ] Start position hợp lý, multi-monitor OK

### UX đo lường
- [ ] Import 100 account: có Spin, có Notification success
- [ ] Search grid 500 dòng: < 300ms sau khi user dừng gõ
- [ ] Mở `fChiTietKichBan` lần 2: < 100ms (pooling)
- [ ] Lỗi mất mạng: hiện message thân thiện, không expose stack trace
- [ ] User test mù (không training): hoàn thành luồng "thêm 10 account + chạy 1 action" < 3 phút

---

## 🎯 Next Step khuyến nghị

Apply ngay theo thứ tự:
1. **Copy 5 file mục 3** vào `Utils/Design/` (30 phút)
2. **Mở rộng AntdHelper** (15 phút)
3. **Apply `GridStyleHelper` + `AntdUI.Empty` cho `ucdgvAccount`** (1h) → user thấy khác biệt lớn nhất
4. **Wrap fAddAccount import** bằng `WithLoading` + `Notify*` (1h)
5. **Sửa font 6.75pt** (15p)

Tổng: **~3 giờ** = tác động thị giác + UX rõ rệt. Sau đó đi theo roadmap Phase 2.
