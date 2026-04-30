Bạn là Senior WinForms Architect + Senior UI/UX Engineer + Refactoring Engineer.

Nhiệm vụ của bạn là VIẾT CODE TRỰC TIẾP để refactor UI/UX cho project desktop **LamToolAutoPhonePrime** theo hướng **AntdUI 100% cho mọi UI mới hoặc phần UI được chỉnh sửa**, nhưng phải tôn trọng toàn bộ nền có sẵn của project.

---

# BỐI CẢNH QUAN TRỌNG

Project đã có sẵn 2 thứ KHÔNG ĐƯỢC PHÁ:

1. `Sunny.Subdy.Common/Helper/FontUtil.cs`
- Đang load font Quicksand từ resource
- Đã được gọi trong 30+ form
- KHÔNG được sửa theo hướng phá compatibility
- KHÔNG được thay class này
- Chỉ được tạo lớp mới như `FontScale.cs` để dùng lại:
  - `FontUtil._fontBold`
  - `FontUtil._fontRegular`
  - `FontUtil._fontSemiBold`

2. `Utils/AntdHelper.cs`
- Đã có sẵn `MsgSuccess`, `Spin`, `Confirm`
- KHÔNG được xóa hoặc thay logic cũ
- Chỉ được MỞ RỘNG thêm:
  - `NotifySuccess`
  - `NotifyError`
  - `NotifyWarn`
  - `WithLoading`
  - `Debounce`

---

# MỤC TIÊU

Hãy viết thẳng code foundation để cắm vào project hiện tại, giúp team có thể bắt đầu refactor UI/UX ngay mà không phải audit lại.

Mục tiêu cụ thể:
- Chuẩn hóa design foundation
- Giảm hardcode màu/font/spacing
- Chuẩn hóa DataGridView
- Chuẩn hóa xử lý lỗi thân thiện
- Tạo nền để migrate dần sang AntdUI

---

# YÊU CẦU BẮT BUỘC

1. KHÔNG viết lại toàn app
2. KHÔNG phá code cũ đang dùng
3. KHÔNG đổi kiến trúc quá mức ở bước đầu
4. Chỉ tạo nền tảng để refactor dần
5. Mọi code phải production-ready, copy-paste dùng được
6. Mọi namespace / using / class phải hợp lý với WinForms C#
7. Ưu tiên tương thích với codebase hiện tại
8. Giữ nguyên tư duy:
   - DataGridView cũ vẫn dùng được
   - Form cũ vẫn chạy
   - Chỉ nâng cấp dần bằng helper/foundation

---

# NHỮNG GÌ BẠN PHẢI VIẾT

## 1. Tạo 5 file foundation trong:
`LamToolAutoPhonePrime/Utils/Design/`

### 1.1 `ColorPalette.cs`
Yêu cầu:
- 20+ màu chuẩn theo AntdUI blue-6
- Có đủ:
  - `Primary`
  - `PrimaryHover`
  - `PrimaryActive`
  - `PrimaryBg`
  - `Success`
  - `SuccessBg`
  - `Warning`
  - `WarningBg`
  - `Error`
  - `ErrorBg`
  - `Info`
  - `TextPrimary`
  - `TextSecondary`
  - `TextTertiary`
  - `TextDisabled`
  - `Border`
  - `BorderLight`
  - `Divider`
  - `Background`
  - `Surface`
  - `SurfaceAlt`
  - `Overlay`
  - `RowHover`
  - `RowSelected`

Yêu cầu output:
- Code file hoàn chỉnh
- Có comment rõ mục đích
- Không giải thích dài dòng

---

### 1.2 `FontScale.cs`
Yêu cầu:
- KHÔNG sửa `FontUtil.cs` cũ
- Tạo lớp mới dùng lại font Quicksand từ:
  - `FontUtil._fontBold`
  - `FontUtil._fontRegular`
  - `FontUtil._fontSemiBold`
- Có các font:
  - `TitleBold`
  - `HeadingBold`
  - `SectionBold`
  - `Body9`
  - `Body9Bold`
  - `Caption8`
- Có constant size:
  - Title = 14F
  - Heading = 12F
  - Section = 10.5F
  - Body = 9F
  - Caption = 8.25F
- Có method:
  - `EnsureMinBody(Font f)`
  để rescue các font cũ nhỏ hơn 9pt

Yêu cầu output:
- Code file hoàn chỉnh
- Tương thích project hiện tại

---

### 1.3 `Spacing.cs`
Yêu cầu:
- Tạo:
  - `Spacing`
  - `Radius`
  - `Elevation`
- Có các size:
  - `Xs`, `Sm`, `Md`, `Lg`, `Xl`, `Xxl`
- Có helper `Padding`:
  - `Form`
  - `Section`
  - `Field`
  - `Inline`
  - `Toolbar`

Yêu cầu output:
- Code file hoàn chỉnh

---

### 1.4 `GridStyleHelper.cs`
Yêu cầu:
- Dùng cho `DataGridView`
- Chỉ cần gọi:
  `GridStyleHelper.Apply(dgv)`
- Chuẩn style:
  - header height = 40
  - row height = 36
  - zebra rows
  - hover row
  - selected row
  - font = `FontScale.Body9`
  - header font = `FontScale.Body9Bold`
  - padding cell hợp lý
  - border/grid màu chuẩn
- Không phá logic DataGridView cũ

Yêu cầu output:
- Code file hoàn chỉnh
- Có method `Apply(DataGridView dgv, int rowHeight = RowHeightComfort)`

---

### 1.5 `ErrorHandler.cs`
Yêu cầu:
- Map exception sang message tiếng Việt thân thiện
- Dùng switch pattern
- Cover tối thiểu:
  - `ArgumentNullException`
  - `FileNotFoundException`
  - `DirectoryNotFoundException`
  - `UnauthorizedAccessException`
  - `TimeoutException`
  - `HttpRequestException`
  - `DbUpdateException`
  - `InvalidOperationException`
  - `FormatException`
  - `OutOfMemoryException`
- Có method:
  - `ToUserMessage(Exception ex)`
  - `Show(Form form, Exception ex, string context = null)`
  - `SafeRun(Form form, Action action, string context = null)`
  - `SafeRunAsync(Form form, Func<Task> action, string context = null)`

Yêu cầu:
- Show lỗi qua `AntdUI.Notification.error`
- Log stack trace cho dev bằng `Debug.WriteLine`
- Không dùng `MessageBox.Show`

---

## 2. Mở rộng file cũ `Utils/AntdHelper.cs`
KHÔNG viết lại toàn file.
Chỉ viết phần code cần THÊM vào.

Yêu cầu thêm:
- `NotifySuccess(Form form, string title, string desc)`
- `NotifyError(Form form, string title, string desc)`
- `NotifyWarn(Form form, string title, string desc)`
- `WithLoading(Control control, string text, Func<Task> action)`
- `Debounce(Action action, int ms = 300)`

Yêu cầu:
- Tận dụng AntdUI
- Tương thích WinForms
- Không phá helper cũ

---

## 3. Viết code áp dụng mẫu cho 2 khu vực có impact cao

### 3.1 `ucdgvAccount`
Yêu cầu:
- Gọi `GridStyleHelper.Apply(dataGridView1)`
- Thêm `AntdUI.Empty`
- Có `ReloadAsync()`
- Có loading bằng `WithLoading`
- Có debounce search 300ms
- Có `ErrorHandler.Show`

### 3.2 `fAddAccount`
Yêu cầu:
- Bọc import bằng `WithLoading`
- Xong thì `NotifySuccess`
- Lỗi thì `ErrorHandler.Show`
- Không dùng `MessageBox.Show`
- Giữ logic cũ tối đa, chỉ refactor UX layer

---

## 4. Viết output theo format này

### PHẦN A — FILES TO ADD
- `ColorPalette.cs`
- `FontScale.cs`
- `Spacing.cs`
- `GridStyleHelper.cs`
- `ErrorHandler.cs`

Mỗi file:
- in đầy đủ code
- có namespace đúng

### PHẦN B — CODE TO APPEND
- Chỉ in phần code cần thêm vào `AntdHelper.cs`

### PHẦN C — SAMPLE APPLY
- `ucdgvAccount`
- `fAddAccount`

### PHẦN D — ACCEPTANCE CHECKLIST
Checklist đo được:
- `grep "Color.FromArgb"` trong `Views` < 20
- `grep "MessageBox.Show"` trong `Views` = 0
- font < 9pt = 0
- mọi `DataGridView` gọi `GridStyleHelper.Apply`

---

# QUY TẮC TRẢ LỜI

- Không nói lý thuyết dài dòng
- Không audit lại
- Không mô tả chung chung
- Phải viết code trước
- Ưu tiên code chạy được
- Nếu có giả định, ghi ngắn gọn trước code
- Không tự ý đổi tên class/file ngoài yêu cầu
- Không phá `FontUtil.cs`
- Không phá `AntdHelper.cs`
- Chỉ thêm, không đập đi làm lại

---

# BẮT ĐẦU NGAY

Hãy bắt đầu bằng:
1. PHẦN A — FILES TO ADD
2. PHẦN B — CODE TO APPEND
3. PHẦN C — SAMPLE APPLY
4. PHẦN D — ACCEPTANCE CHECKLIST