Bạn là Senior WinForms Architect + Senior UI/UX Engineer.

Nhiệm vụ của bạn KHÔNG phải là audit nữa — mà là:
👉 THIẾT KẾ + TRIỂN KHAI + VIẾT CODE REFACTOR UI/UX THỰC TẾ
cho project desktop LamToolAutoPhonePrime.

Bạn phải làm việc như một dev senior đang trực tiếp refactor codebase lớn.

---

## 🧠 CONTEXT

Project:
- WinForms desktop app lớn (67+ form, 90+ action form fHD*)
- Đã dùng AntdUI một phần nhưng đang bị:
  - trộn WinForms + AntdUI
  - màu/font hardcode (~1380 chỗ)
  - thiếu design system
  - UX rời rạc, khó dùng

ĐÃ CÓ sẵn:
- AntdUI
- AntdHelper.cs (Spin, Notification, Confirm)
- VietnameseLocalization

---

## ⚠️ PROBLEMS BẮT BUỘC PHẢI GIẢI QUYẾT

1. Không có color system (1380 hardcode màu)
2. Không có loading khi thao tác dài (>30s import)
3. Font quá nhỏ (6.75pt)
4. DataGridView cực kỳ khó đọc
5. Navigation rối (90+ form không tổ chức)
6. Error message raw (technical)
7. Form không resize được
8. UX desktop yếu (layout dày đặc, spacing kém)
9. Accessibility gần như = 0
10. Flow thao tác lặp (column mapping import)

---

## 🔥 REQUIREMENT QUAN TRỌNG NHẤT

⚠️ BẮT BUỘC:
👉 DÙNG ANTDUI 100% CHO MỌI UI MỚI HOẶC REFRACTOR

KHÔNG ĐƯỢC:
- Dùng WinForms control mặc định nếu đã có AntdUI tương đương
- Viết UI custom nếu AntdUI đã hỗ trợ

---

## 🎯 GOAL

Sau khi refactor:
- UI đồng nhất (design system)
- Dễ nhìn – không mỏi mắt
- Flow nhanh – ít thao tác
- Không cần giải thích vẫn dùng được
- Có loading / feedback rõ ràng
- Code dễ maintain

---

## 📦 TASK BẠN PHẢI LÀM

---

# 1. 🚨 PRIORITY BREAKDOWN (PHẢI LÀM TRƯỚC)

Hãy phân tích và chọn thứ tự refactor theo impact:

- P0: sửa ngay (ảnh hưởng UX trực tiếp)
- P1: cải thiện mạnh
- P2: tối ưu thêm

👉 Output: bảng priority + lý do

---

# 2. 🧱 BUILD DESIGN FOUNDATION (CODE THẬT)

Viết code đầy đủ cho:

## 2.1 ColorPalette.cs
- Replace toàn bộ màu hardcode
- Đồng bộ với AntdUI theme

## 2.2 FontUtil.cs
- Không cho phép < 9pt
- Define Title / Body / Caption

## 2.3 Spacing.cs
- Padding / margin chuẩn

## 2.4 GridStyleHelper.cs
- Apply style cho DataGridView:
  - row height
  - zebra row
  - header style
  - selection state

## 2.5 ErrorHandler.cs
- Map exception → message thân thiện

👉 Code phải production-ready, có thể copy dùng ngay

---

# 3. 🎨 REFACTOR UI THEO ANTDUI

Cho từng khu vực, bạn PHẢI:

- Chỉ rõ component AntdUI thay thế
- Viết code mẫu
- Giải thích UX improvement

---

## 3.1 Main Form (fMain)

Refactor:
- Sidebar → AntdUI.Menu
- Header → AntdUI.PageHeader
- Navigation → breadcrumb

👉 Viết code skeleton

---

## 3.2 Form nhập liệu

Refactor:
- TextBox → AntdUI.Input
- ComboBox → AntdUI.Select
- Validation inline

👉 Code ví dụ form

---

## 3.3 Button & Action

- Primary / Secondary hierarchy
- Icon button
- Toolbar

👉 Code mẫu

---

## 3.4 Modal / Dialog

- MessageBox → AntdUI.Modal
- Notification → AntdUI.Notification

👉 Code thay thế cụ thể

---

## 3.5 DataGrid

- Áp dụng GridStyleHelper
- Thêm Empty state
- Thêm Loading state

👉 Code ví dụ ucdgvAccount

---

## 3.6 Loading / Feedback

- <2s: no UI
- 2–10s: Spin
- >10s: overlay + progress

👉 Code AntdHelper usage

---

# 4. 🧭 NAVIGATION REFACTOR (QUAN TRỌNG NHẤT)

Thiết kế lại:

## fActionCenter (thay 90+ form fHD*)

- Tabs theo category
- Search action
- Load UserControl thay Form

👉 Viết:
- kiến trúc
- code skeleton

---

# 5. ⚡ QUICK WINS (PHẢI LÀM NGAY)

Liệt kê:
- danh sách việc <1 ngày
- file cụ thể
- impact

---

# 6. 🧠 UX RULES (DESKTOP)

Define rõ:

- Font size chuẩn
- Table spacing
- Button hierarchy
- Error message format
- Loading behavior
- Form resize rules

---

# 7. 🗺️ ROADMAP TRIỂN KHAI

Chia thành:

- Phase 1 (foundation)
- Phase 2 (core screens)
- Phase 3 (full refactor)

---

# 8. ✅ CHECKLIST NGHIỆM THU

- Không còn hardcode màu
- Không còn font < 9pt
- Có loading cho action dài
- DataGrid readable
- Navigation rõ ràng
- Error thân thiện

---

## ⚠️ OUTPUT REQUIREMENTS

- KHÔNG viết chung chung
- PHẢI có code ví dụ
- PHẢI có hướng implement thực tế
- PHẢI bám sát project WinForms + AntdUI
- Ưu tiên giải pháp có thể apply ngay

---

## 🎯 START

Bắt đầu với:

1. Executive Summary (ngắn)
2. Priority table
3. Design Foundation (code)
4. Refactor từng phần UI bằng AntdUI (có code)