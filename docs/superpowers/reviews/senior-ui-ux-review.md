# 🎨 SENIOR UI/UX REVIEW — LamToolAutoPhonePrime
**AntdUI 100% Refactor Strategy**

> Audit-based, thực chiến, đưa ra path triển khai dần vào codebase.
> Tài liệu này dùng để dẫn dắt toàn bộ quá trình refactor UI/UX.

---

## 1. Executive Summary

LamToolAutoPhonePrime là một desktop tool WinForms quy mô lớn (67+ form, 90+ action form `fHD*`, nhiều DataGrid, flow nhập liệu liên tục). Codebase **đã dùng AntdUI** ở khoảng 30 file (235 occurrences), nhưng **trộn lẫn** với WinForms gốc và màu/font hardcode (~1380 instance). Hệ quả: UI trông cũ, thiếu nhất quán, khó điều hướng, khó maintain.

Điểm tốt: đã có `Utils/AntdHelper.cs` (MsgSuccess / MsgError / Spin / Confirm) và đã có `VietnameseLocalization` cho AntdUI — đây là nền rất tốt để chuẩn hoá.

**Chiến lược đề xuất**: không viết lại — **chuẩn hoá dần** theo 3 giai đoạn. Xây Design Foundation (ColorPalette + FontUtil + Spacing + GridStyleHelper + ErrorHandler) trước, sau đó migrate form theo thứ tự ưu tiên user-facing.

**Mục tiêu đo được sau refactor**:
- 0 hardcode `Color.FromArgb(...)` trong `Views/`
- 100% form dùng AntdUI cho button/input/modal/notification
- Row height DataGrid ≥ 36px, font tối thiểu 9pt (≈12px)
- Loading indicator cho mọi thao tác > 2s
- Mọi form hỗ trợ resize, `MinimumSize` hợp lý, bỏ `MaximumSize`

---

## 2. Top vấn đề ưu tiên (bảng quyết định)

| # | Vấn đề | Tác động người dùng | Khối lượng | Priority |
|---|--------|--------------------|-----------|----------|
| 1 | 1380 màu hardcode, không có color system | Mỗi form một kiểu → cảm giác thiếu chuyên nghiệp | 2 ngày | **P0** |
| 2 | Không có loading khi import 30s+ (`fAddAccount.cs:85`) | User nghĩ app crash, click lại → lỗi | 0.5 ngày | **P0** |
| 3 | Font 6.75pt tại `fQuanLyKichBan.Designer.cs:96` | Không đọc được, mỏi mắt | 0.5 ngày | **P0** |
| 4 | DataGridView không styled (header, zebra, sort) | Scan data chậm, sai dòng | 1.5 ngày | **P0** |
| 5 | 90+ form action không có menu tổ chức | User lạc trong biển form | 3 ngày | **P1** |
| 6 | Error message raw exception | User không biết fix kiểu gì | 1 ngày | **P1** |
| 7 | Form fixed size (MaxSize = MinSize) | Vỡ layout trên 1366px và 4K | 1 ngày | **P1** |
| 8 | Zero AccessibleName / TabIndex lộn xộn | Không tab được theo thứ tự | 1 ngày | **P2** |
| 9 | Column mapping import phải chọn lại mỗi lần | Thao tác lặp, dễ sai | 0.5 ngày | **P2** |
| 10 | Layout dày đặc, không có spacing system | Mỏi mắt khi dùng lâu | 2 ngày | **P1** |

---

## 3. Giải pháp UI/UX tổng thể

### 3.1. Layout & Hierarchy
- **Pattern mới**: Page header (title + action bar) → Toolbar (search, filter) → Content (grid/form) → Footer status. Dùng `AntdUI.PageHeader` + `AntdUI.Divider`.
- **Spacing baseline**: padding container 16px, gap giữa group 24px, gap giữa label+input 8px.
- **Section rõ ràng**: `AntdUI.Divider` với title (`Text = "Thông tin cơ bản"`) thay cho GroupBox xám.

### 3.2. Color System
- Xoá 100% `Color.FromArgb(...)` trong `Views/`, thay bằng `ColorPalette.*`.
- Dùng `AntdUI.Style.Db` / Theme token cho primary/success/error/warning để đồng bộ với AntdUI components tự render.

### 3.3. Typography
- Font tối thiểu: **9pt (12px)** body, **14pt** title form, **11pt** section header.
- Bỏ sạch các font < 9pt. Chuẩn hoá qua `FontUtil.Body / Title / Section / Caption`.

### 3.4. Navigation
- **fMain** → chuyển sang `AntdUI.Menu` (sidebar) + `AntdUI.PageHeader` (breadcrumb).
- **90+ form action** → gom thành `AntdUI.Tabs` theo nhóm (Tương tác / Đăng bài / Bảo mật / Profile...), kết hợp search action theo tên.
- **Modal** → `AntdUI.Modal.open(...)` thay cho `form.ShowDialog()`.

### 3.5. Form / Input
- Thay `TextBox` → `AntdUI.Input` (có PlaceholderText, PrefixIcon, Status validation).
- Thay `ComboBox` → `AntdUI.Select` (đã có sẵn rải rác).
- Validation **inline**: set `Status = Red` + `helpText` thay cho MessageBox.

### 3.6. DataGrid
- **Không bỏ DataGridView gốc** (quá nhiều chỗ đang dùng), thay vào đó **viết `GridStyleHelper.Apply(dgv)`** gắn style thống nhất: header AntdUI blue, zebra rows, row height 36, font 9pt, sort indicator.
- Các form mới → dùng thẳng `AntdUI.Table` (virtualization tốt hơn nhiều).

### 3.7. Feedback / Loading / Error
- < 2s: không cần feedback
- 2–10s: `AntdUI.Spin` inline trên button/panel
- \> 10s: `AntdUI.Spin` full panel + `AntdUI.Progress` nếu đếm được progress
- Toast: `AntdUI.Notification` (không chặn, tự biến mất) thay `MessageBox`

### 3.8. Accessibility
- Set `AccessibleName` cho tất cả button/input quan trọng
- TabIndex đánh lại tuần tự theo thứ tự đọc (trái-trên → phải-dưới)
- Alt-hotkey: thêm `&` vào text button (`&Thêm`, `&Lưu`, `&Hủy`)

### 3.9. Responsiveness
- Bỏ `MaximumSize` trên tất cả form nội dung
- `MinimumSize` hợp lý (thường 800×500 cho form phụ, 1280×720 cho fMain)
- Dùng `TableLayoutPanel` / `AntdUI.StackPanel` để form tự co giãn

---

## 4. Đề xuất áp dụng AntdUI 100% theo từng khu vực

### 4.1. Main Form / Sidebar / Navigation
| Thành phần cũ | Thay bằng AntdUI | Lý do |
|---|---|---|
| Panel menu tự vẽ (`fMain` leftBar) | `AntdUI.Menu` (mode=Inline) | Active state sẵn có, hover animation, sub-menu |
| Tab cứng ("Tài khoản/Thiết bị/Lịch sử") | `AntdUI.Tabs` hoặc `AntdUI.Segmented` | Hiện đại, support badge count |
| Không có breadcrumb | `AntdUI.PageHeader` (có `Breadcrumb` slot) | User luôn biết đang ở đâu |
| Title bar tự code | `AntdUI.Window` (form kế thừa) | Dark mode, shadow, min/max/close chuẩn |

### 4.2. Form nhập liệu
| Cũ | AntdUI | Lý do |
|---|---|---|
| `TextBox` / `MaskedTextBox` | `AntdUI.Input` / `InputNumber` / `InputPassword` | PlaceholderText, Status, PrefixIcon, AllowClear |
| `ComboBox` | `AntdUI.Select` | Search, multi-select, custom render |
| `CheckBox` grouped | `AntdUI.Checkbox` / `CheckboxGroup` | Label alignment chuẩn |
| `DateTimePicker` | `AntdUI.DatePicker` | Preset, range, locale VI |
| `GroupBox` | `AntdUI.Divider` với title hoặc `AntdUI.Collapse` | Nhẹ hơn, gọn hơn |

### 4.3. Button / Action
| Cũ | AntdUI | Lý do |
|---|---|---|
| `Button` WinForms | `AntdUI.Button` với `Type = Primary/Default/Dashed/Link/Text` | Hierarchy rõ |
| Toolbar `Panel + Button` | `AntdUI.StackPanel` + `AntdUI.Button` size=Small | Consistent |
| Icon button tự chế | `AntdUI.Button Ghost=true IconSvg="..."` | AntdUI icon set sẵn |

**Quy tắc hierarchy**: 1 primary / form, ≤ 2 default, còn lại là link/text.

### 4.4. Modal / Dialog / Confirm
| Cũ | AntdUI |
|---|---|
| `MessageBox.Show(...)` confirm | `AntdUI.Modal.open({Title, Content, OkText, CancelText})` |
| `form.ShowDialog()` cho dialog nhỏ | `AntdUI.Modal.open` với `Form` content |
| `MessageBox.Show` thông báo | `AntdUI.Notification.open` (toast) |

### 4.5. Notification / Message / Loading
- **Spin inline** (button import): `AntdHelper.Spin(this, "Đang import...", async cfg => { await ... })`
- **Toast success**: `AntdUI.Notification.success(this, "Thành công", "Đã thêm 120 tài khoản", TAlignFrom.BR)`
- **Progress đếm được**: `AntdUI.Progress` trong panel bottom form
- **Skeleton cho list load**: `AntdUI.Skeleton` khi `LoadList()` của ucdgvAccount chạy

### 4.6. DataGrid / Danh sách
| Cũ | AntdUI | Ghi chú |
|---|---|---|
| `DataGridView` + style tay | `DataGridView` + `GridStyleHelper.Apply(dgv)` | Giữ logic, nâng cấp visual |
| Scroll list tự code (virtualPanel) | `AntdUI.VirtualPanel` (đã có) | Giữ, chuẩn hoá item template |
| Empty state tự vẽ Paint (ucdgvAccount:109) | `AntdUI.Empty` control | Có icon + text + action |
| Không loading | `AntdUI.Spin` wrap grid | |

### 4.7. Search / Filter / Toolbar
- `AntdUI.Input` với `PrefixText = "🔍"` + `AllowClear = true`
- Filter: `AntdUI.Select` multi-tag hoặc `AntdUI.Dropdown` với checkbox
- Debounce 300ms (viết helper `DebounceHelper.Debounce(300, () => Search())`)

### 4.8. Breadcrumb / Page Header / Section Header
- Mọi form con mở ra trong fMain nên có `AntdUI.PageHeader` (`Title` + `SubTitle` + `BackIcon`).
- Section trong form dài: `AntdUI.Divider Text="Cấu hình nâng cao" OrientationMargin=0`.

---

## 5. Design System đề xuất (chuyển thẳng thành file code)

### 5.1. `Utils/Design/ColorPalette.cs`
```csharp
public static class ColorPalette
{
    // Brand
    public static readonly Color Primary        = Color.FromArgb(22, 119, 255);   // AntdUI blue-6
    public static readonly Color PrimaryHover   = Color.FromArgb(64, 150, 255);
    public static readonly Color PrimaryActive  = Color.FromArgb(9,  88,  217);

    // Semantic
    public static readonly Color Success = Color.FromArgb(82, 196, 26);
    public static readonly Color Warning = Color.FromArgb(250, 173, 20);
    public static readonly Color Error   = Color.FromArgb(255, 77, 79);
    public static readonly Color Info    = Color.FromArgb(22, 119, 255);

    // Neutral
    public static readonly Color TextPrimary   = Color.FromArgb(38,  38,  38);   // gray-10
    public static readonly Color TextSecondary = Color.FromArgb(89,  89,  89);   // gray-8
    public static readonly Color TextDisabled  = Color.FromArgb(191, 191, 191);  // gray-6
    public static readonly Color Border        = Color.FromArgb(217, 217, 217);  // gray-5
    public static readonly Color BorderLight   = Color.FromArgb(240, 240, 240);
    public static readonly Color Background    = Color.FromArgb(250, 250, 250);
    public static readonly Color Surface       = Color.White;
    public static readonly Color SurfaceAlt    = Color.FromArgb(248, 249, 250); // zebra
    public static readonly Color Overlay       = Color.FromArgb(120, 0, 0, 0);
}
```

### 5.2. `Utils/Design/FontUtil.cs`
```csharp
public static class FontUtil
{
    private const string Family = "Segoe UI";
    public static readonly Font Title    = new Font(Family, 14F, FontStyle.Bold);   // 18.6px
    public static readonly Font Heading  = new Font(Family, 12F, FontStyle.Bold);
    public static readonly Font Section  = new Font(Family, 10.5F, FontStyle.Bold);
    public static readonly Font Body     = new Font(Family, 9F,  FontStyle.Regular);// 12px – TỐI THIỂU
    public static readonly Font BodyBold = new Font(Family, 9F,  FontStyle.Bold);
    public static readonly Font Caption  = new Font(Family, 8.25F, FontStyle.Regular);
    public static readonly Font Mono     = new Font("Consolas", 9F);
}
```

### 5.3. `Utils/Design/Spacing.cs`
```csharp
public static class Spacing
{
    public const int Xs = 4;
    public const int Sm = 8;
    public const int Md = 12;
    public const int Lg = 16;
    public const int Xl = 24;
    public const int Xxl = 32;

    public static Padding FormPadding    => new Padding(Lg);
    public static Padding SectionPadding => new Padding(0, Xl, 0, Xl);
    public static Padding FieldGap       => new Padding(0, 0, 0, Md);
}

public static class Radius
{
    public const int Sm = 4;
    public const int Md = 6;
    public const int Lg = 8;
}
```

### 5.4. `Utils/Design/GridStyleHelper.cs`
```csharp
public static class GridStyleHelper
{
    public static void Apply(DataGridView dgv)
    {
        dgv.EnableHeadersVisualStyles = false;
        dgv.BorderStyle = BorderStyle.None;
        dgv.BackgroundColor = ColorPalette.Surface;
        dgv.GridColor = ColorPalette.BorderLight;
        dgv.RowHeadersVisible = false;
        dgv.AllowUserToAddRows = false;
        dgv.AllowUserToResizeRows = false;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.RowTemplate.Height = 36;

        // Header
        dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.Background,
            ForeColor = ColorPalette.TextPrimary,
            Font      = FontUtil.BodyBold,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding   = new Padding(Spacing.Md, 0, Spacing.Md, 0),
            SelectionBackColor = ColorPalette.Background,
            SelectionForeColor = ColorPalette.TextPrimary
        };
        dgv.ColumnHeadersHeight = 40;

        // Rows
        dgv.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.Surface,
            ForeColor = ColorPalette.TextPrimary,
            Font      = FontUtil.Body,
            Padding   = new Padding(Spacing.Md, 0, Spacing.Md, 0),
            SelectionBackColor = Color.FromArgb(230, 244, 255),
            SelectionForeColor = ColorPalette.TextPrimary
        };
        dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorPalette.SurfaceAlt
        };
    }
}
```

### 5.5. `Utils/ErrorHandler.cs`
```csharp
public static class ErrorHandler
{
    public static string ToUserMessage(Exception ex) => ex switch
    {
        ArgumentNullException          => "Vui lòng điền đầy đủ thông tin.",
        FileNotFoundException f        => $"Không tìm thấy file: {Path.GetFileName(f.FileName)}.",
        UnauthorizedAccessException    => "Không có quyền truy cập. Thử chạy với quyền Administrator.",
        TimeoutException               => "Hết thời gian chờ. Kiểm tra mạng hoặc thử lại.",
        HttpRequestException h         => $"Lỗi kết nối: {h.Message}. Kiểm tra mạng.",
        DbUpdateException              => "Không lưu được dữ liệu. Thử lại hoặc khởi động lại app.",
        InvalidOperationException      => "Thao tác không hợp lệ trong trạng thái hiện tại.",
        _                              => $"Đã xảy ra lỗi: {ex.Message}"
    };

    public static void Show(Form form, Exception ex, string context = null)
    {
        var msg = ToUserMessage(ex);
        if (!string.IsNullOrEmpty(context)) msg = $"{context}\n{msg}";
        AntdUI.Notification.error(form, "Lỗi", msg, AntdUI.TAlignFrom.BR);
        // log full stack để dev đọc
        System.Diagnostics.Debug.WriteLine(ex.ToString());
    }
}
```

### 5.6. Input / Table / Empty / Loading state spec
| State | Visual |
|---|---|
| Input default | border `Border`, radius `Md`, padding 8×12 |
| Input focus | border `Primary`, shadow `0 0 0 2px rgba(22,119,255,.1)` |
| Input error | border `Error`, helpText red 12px |
| Input disabled | bg `Background`, text `TextDisabled` |
| Table row hover | bg `Background` |
| Table row selected | bg `rgba(22,119,255,.08)` + left border `Primary` 3px |
| Empty state | `AntdUI.Empty` icon 64px + text `TextSecondary` + 1 primary action |
| Loading > 2s | `AntdUI.Spin` inline |
| Loading > 10s | Full panel spin + `AntdUI.Progress` |

---

## 6. Action Plan theo giai đoạn

### 🟢 Giai đoạn 1 — Foundation (Tuần 1, 3–5 ngày)
Mục tiêu: **có sẵn hạ tầng để các giai đoạn sau chỉ việc “gọi”**.
1. Tạo 5 file: `ColorPalette.cs`, `FontUtil.cs` (mở rộng file cũ), `Spacing.cs`, `GridStyleHelper.cs`, `ErrorHandler.cs`
2. Mở rộng `AntdHelper.cs`: thêm `Notify.Success/Error/Warn/Info` (non-blocking), `LoadingScope(form, text)` dạng `IDisposable`
3. Viết `DebounceHelper`, `FormPool<T>` (tái sử dụng dialog)
4. Set theme toàn app: `AntdUI.Style.Load()` + dark/light toggle ở `Program.cs`

### 🟡 Giai đoạn 2 — Chuẩn hoá các form user-facing cao (Tuần 2–3)
Thứ tự theo tần suất user chạm vào:
1. **fMain** — Menu AntdUI, PageHeader, breadcrumb, bỏ hardcode màu
2. **ucdgvAccount** — `GridStyleHelper.Apply` + `AntdUI.Empty` + debounce search
3. **fAddAccount / fImportProxy** — Spin loading, Notification thay MessageBox, Input validation inline, nhớ column mapping
4. **fQuanLyKichBan / fChiTietKichBan** — PageHeader, Spin, bỏ font 6.75pt
5. **fLogin / fRegister** — AntdUI.Input, validation inline, Alt-hotkey

### 🔵 Giai đoạn 3 — Refactor lớn (Tuần 4–6)
1. Gom 90+ form `fHD*` thành 1 **fActionCenter** dùng `AntdUI.Tabs` + search — mỗi action chỉ là 1 UserControl embed
2. Form pooling cho các dialog hay dùng (fChiTietKichBan, fAddUsercontrol)
3. Toàn bộ DataGridView trong `Views/` → `GridStyleHelper.Apply`
4. Accessibility pass: set AccessibleName, TabIndex, Alt-hotkey cho toàn bộ form P0/P1
5. Responsiveness pass: bỏ MaximumSize, set MinimumSize chuẩn, dùng TableLayoutPanel

### Tác động người dùng cuối (ranking)
1. 🔥 **fAddAccount + ucdgvAccount** — dùng hàng ngày, nhiều pain point nhất
2. 🔥 **fMain navigation** — “gương mặt” của app
3. 🔥 **Action forms consolidation** — giảm cảm giác “rối loạn 90 form”
4. ⚡ **DataGrid styling + loading feedback** — tăng perceived performance
5. ⚡ **Error handler** — giảm support ticket

---

## 7. Quick Wins — làm ngay (< 1 ngày mỗi item)

| # | Việc | File/Line | Thời gian |
|---|---|---|---|
| 1 | Tạo `ColorPalette.cs` và replace 20 màu nổi bật nhất | `fLogin.Designer.cs:77-97`, `fChiTietKichBan.Designer.cs:78` | 2h |
| 2 | Thêm `AntdHelper.Spin` vào nút import của `fAddAccount` | `fAddAccount.cs:81-127` | 1h |
| 3 | Sửa font 6.75pt thành `FontUtil.Caption` (8.25pt) | `fQuanLyKichBan.Designer.cs:96` | 15p |
| 4 | Bỏ `MaximumSize` trong các form dialog | `fFolder.Designer.cs:280-282`, fSelectByUid, fUpdateAuto | 30p |
| 5 | Replace `MessageBox.Show` lỗi → `ErrorHandler.Show` | grep `MessageBox.Show` trong `Views/` | 3h |
| 6 | Gọi `GridStyleHelper.Apply(dgv)` trong `ucdgvAccount` + `ucManagerDevices` | ucdgvAccount.cs constructor | 1h |
| 7 | PlaceholderText cho mọi `AntdUI.Input` chưa set | `fImportProxy.Designer.cs:49`, fAddAccount | 1h |
| 8 | Debounce search 300ms | `fQuanLyKichBan.cs:113` | 30p |
| 9 | `AccessibleName` + `&`-hotkey cho 10 form chính | fMain, fAddAccount, fLogin | 3h |
| 10 | Thêm `AntdUI.Empty` thay Paint empty state | `ucdgvAccount.cs:109-136` | 1h |

---

## 8. Refactor lớn — nên làm tiếp theo

### 8.1. `fActionCenter` thay 90+ form `fHD*`
- **Vấn đề**: User không biết có action nào tồn tại; phải code cứng menu gọi từng form
- **Giải pháp**:
  - Giữ nguyên logic `fHD*` nhưng biến `Form` → `UserControl` (1 lần)
  - Tạo `fActionCenter` với `AntdUI.Tabs` nhóm theo category: *Tương tác / Đăng bài / Bảo mật / Profile / Nhóm / Page*
  - Thêm `AntdUI.Input` search — user gõ tên action (VD "xem story") → highlight tab
  - Sidebar phải hiện danh sách action trong category đang mở; click → load UserControl vào content panel
- **Lợi ích**: 1 cửa sổ, 1 flow nhất quán; dễ thêm action mới

### 8.2. `AntdUI.Menu` Sidebar cho `fMain`
- Thay khối panel+button+leftBar tự code (`fMain.cs:69-99`) bằng `AntdUI.Menu` Inline
- Badge count trên menu item ("Tài khoản 1,245", "Job đang chạy 3")
- Collapse mode khi cửa sổ < 1200px

### 8.3. Global Theme & Dark Mode
- Đăng ký `AntdUI.Style.Load(AntdUI.Style.Dark)` trong `Program.cs`
- Toggle button trên PageHeader
- Tất cả color phải đi qua `ColorPalette` (đã refactor ở Giai đoạn 1)

### 8.4. Form Pooling
- Singleton cho `fChiTietKichBan`, `fAddUsercontrol`, `fActions`
- `ShowDialog()` → `Show()` + lock via AntdUI.Modal overlay
- Giảm GC pressure và perceived latency khi mở lại form

### 8.5. Accessibility & Keyboard Shortcuts Center
- File `Shortcuts.cs` đăng ký shortcut global: `Ctrl+N` thêm account, `Ctrl+F` search, `F5` reload grid, `Esc` close modal
- `&`-hotkey cho mọi button trong P0/P1 form
- TabIndex đánh lại theo thứ tự reading

---

## 9. Checklist nghiệm thu sau khi sửa

### Foundation
- [ ] 5 file design foundation tồn tại và có unit test
- [ ] `grep Color.FromArgb` trong `Views/` trả về < 20 match (còn 1380)
- [ ] Toàn bộ `MessageBox.Show` trong `Views/` đã thay bằng `AntdUI.Notification` hoặc `ErrorHandler.Show`
- [ ] Font size < 9pt: 0 match
- [ ] `MaximumSize` trên content form: 0 match

### Component
- [ ] Mọi `DataGridView` trong `Views/` đều gọi `GridStyleHelper.Apply`
- [ ] Mọi thao tác > 2s có Spin / Progress
- [ ] Mọi form input có ít nhất 1 inline validation (Status=Error + helpText)
- [ ] Mọi form con có `PageHeader` với title + back
- [ ] Mọi empty state dùng `AntdUI.Empty`

### Navigation
- [ ] fMain dùng `AntdUI.Menu` cho sidebar
- [ ] Breadcrumb hiện path hiện tại
- [ ] 90+ `fHD*` gom vào `fActionCenter`
- [ ] Search action hoạt động, debounce 300ms

### Accessibility
- [ ] Tab order logic trên 10 form P0
- [ ] Alt-hotkey cho button chính (Thêm/Lưu/Hủy/Đóng)
- [ ] AccessibleName cho button/input P0
- [ ] Contrast ratio ≥ 4.5:1 cho text trên nền

### Responsiveness
- [ ] Mọi form resize được đến 1920×1080 không vỡ
- [ ] Mọi form mở được trên 1366×768 không scroll ngang
- [ ] DPI 125%/150% hiển thị đúng

### UX đo lường
- [ ] Import 100 account: có Spin ≥ 1 lần, có Notification success
- [ ] Search trong grid 500 dòng: < 300ms sau khi gõ xong
- [ ] Mở lại `fChiTietKichBan` lần 2: < 100ms (nhờ pooling)
- [ ] Error khi mất mạng: hiện message thân thiện, không expose stack trace

---

## Phụ lục — Chuẩn UX cho desktop app (áp dụng xuyên suốt)

| Hạng mục | Chuẩn |
|---|---|
| Font tối thiểu | **9pt / 12px** (body), **8.25pt** chỉ cho caption |
| Row height bảng | **36px** (comfort), 32px (dense), 44px (relaxed) |
| Padding form | **16px** container, 24px giữa section |
| Khoảng cách label–input | **8px** (dọc), 12px (ngang) |
| Primary vs Secondary | 1 primary / form, ≤ 2 default, còn lại link/text |
| Loading 0–2s | Không feedback (instant) |
| Loading 2–10s | `AntdUI.Spin` trên button hoặc panel |
| Loading > 10s | Full overlay + `AntdUI.Progress` + option cancel |
| Error message | *Chuyện gì + Vì sao + Làm gì tiếp* (3 câu tối đa) |
| Resize | MinimumSize có, MaximumSize KHÔNG (trừ dialog confirm) |
| Multi-monitor | StartPosition = CenterParent (dialog), Manual + nhớ vị trí (main) |
| DPI | Per-monitor V2, set trong app.manifest |

---

**Kết luận**: Project có nền AntdUI tốt, chỉ thiếu **design foundation** và **discipline**. Làm xong Giai đoạn 1 (1 tuần), tốc độ refactor các form sau sẽ tăng 3× và kết quả trông đồng bộ hẳn. Ưu tiên: **ColorPalette → GridStyleHelper → ErrorHandler → Spin loading → fMain navigation**. Đó là 5 việc mang lại *cảm nhận khác biệt lớn nhất* với công sức thấp nhất.
