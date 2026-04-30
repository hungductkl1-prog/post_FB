Bạn là Senior UI/UX Designer kiêm Senior WinForms/Desktop UI Engineer.

Nhiệm vụ của bạn là review, đề xuất và chỉnh sửa toàn bộ UI/UX cho phần mềm desktop **LamToolAutoPhonePrime** dựa trên kết quả audit dưới đây. Mục tiêu là nâng cấp giao diện theo hướng hiện đại, dễ nhìn, dễ dùng, thống nhất, thân thiện với người dùng desktop, và có thể maintain lâu dài.

## Bối cảnh
Đây là phần mềm desktop có nhiều form, nhiều bảng dữ liệu, nhiều flow thao tác liên tục. Người dùng sử dụng trong thời gian dài nên cần tối ưu readability, spacing, điều hướng, phản hồi hệ thống và hiệu suất thao tác.

## Kết quả audit hiện tại
Top vấn đề ưu tiên cao:
1. 1380 màu hardcode rải rác, không có color system
2. Không có loading indicator khi import 30s+ (fAddAccount.cs:85)
3. Zero AccessibleName trong 67 forms
4. Layout dày đặc, không có spacing system
5. Error message raw kiểu: "Object reference not set..."
6. 90+ form không có menu điều hướng tổ chức hợp lý
7. Column mapping phải chọn lại mỗi lần import
8. Form fixed size, resize kém, dễ vỡ layout trên laptop
9. DataGridView không styled rõ ràng (header, sort indicator, spacing, row state)
10. Font rất nhỏ, có chỗ 6.75pt, gây khó đọc

Quick wins đã xác định:
- Tạo ColorPalette.cs, thay top màu hardcode
- Thêm PlaceholderText cho TextBox
- Thêm loading state cho import
- Bỏ MaximumSize để form resize được
- Chuẩn hóa TabIndex

Refactor lớn đã xác định:
- NavigationStack + breadcrumb cho fMain
- DataGrid styling framework dùng chung
- ErrorHandler.cs để chuyển exception sang message thân thiện
- Typography scale trong FontUtil
- Form pooling / tái sử dụng form thay vì mở/đóng liên tục

## Yêu cầu bắt buộc
1. **Phải dùng AntdUI 100% cho phần UI mới hoặc phần UI được chỉnh sửa**
2. Không dùng trộn thư viện UI khác cho các thành phần được refactor
3. Ưu tiên tận dụng component, pattern, visual language và interaction style của AntdUI
4. Mọi đề xuất chỉnh sửa phải theo tư duy desktop app thực tế: rõ ràng, dễ scan, ít thao tác, dùng lâu không mỏi mắt
5. Không trả lời chung chung, phải đưa ra hướng chỉnh sửa thực thi được theo từng nhóm vấn đề
6. Nếu đề xuất code, cần viết theo hướng có thể áp dụng dần vào codebase hiện tại
7. Không chỉ nêu vấn đề, phải đưa ra giải pháp cụ thể và thứ tự ưu tiên triển khai

## Mục tiêu cần đạt
- Xây dựng UI system thống nhất cho toàn phần mềm
- Tăng độ dễ nhìn, dễ đọc, dễ thao tác
- Giảm cảm giác rối, dày đặc và lỗi thời
- Cải thiện điều hướng giữa các form
- Chuẩn hóa bảng dữ liệu
- Tăng quality của feedback/loading/error state
- Cải thiện accessibility cơ bản
- Tối ưu trải nghiệm trên desktop/laptop với nhiều kích thước màn hình

## Bạn cần thực hiện các đầu việc sau

### 1. Đánh giá tổng thể UI/UX
Hãy phân tích và đưa ra nhận định tổng quan về:
- Layout & hierarchy
- Color system
- Typography
- Navigation
- Form/input usability
- Data table / DataGridView usability
- Feedback/loading/error states
- Accessibility
- Desktop responsiveness / resize behavior
- Consistency giữa các form

### 2. Đề xuất chiến lược refactor UI/UX
Hãy chia thành 3 nhóm:
- Quick wins (< 1 ngày)
- Refactor trung bình (1–3 ngày)
- Refactor lớn (3–5 ngày hoặc hơn)

Mỗi nhóm phải nêu:
- Vấn đề
- Tác động
- Giải pháp
- Thành phần AntdUI nên dùng
- Mức độ ưu tiên

### 3. Thiết kế lại theo hướng AntdUI 100%
Hãy đề xuất cách áp dụng AntdUI cho các phần sau:
- Main form / điều hướng tổng
- Sidebar / menu / tab điều hướng
- Form nhập liệu
- Nút bấm / action buttons
- Modal / dialog / confirm
- Notification / message / loading
- DataGrid / danh sách dữ liệu / trạng thái rỗng
- Search / filter / toolbar
- Breadcrumb / page header / section header

Nêu rõ:
- Thành phần AntdUI nào nên thay thế thành phần cũ
- Vì sao chọn thành phần đó
- Lợi ích UX thu được

### 4. Xây dựng design foundation
Hãy đề xuất bộ chuẩn UI dùng chung bằng AntdUI, gồm:
- Color palette
- Typography scale
- Spacing system
- Border radius
- Shadow/elevation
- Button hierarchy
- Input states
- Table states
- Empty / loading / error states

Phải trình bày theo hướng có thể chuyển thành:
- ColorPalette.cs
- FontUtil.cs
- Spacing constants
- GridStyleHelper.cs
- ErrorHandler.cs

### 5. Tạo action plan triển khai
Hãy đưa ra roadmap rõ ràng:
- Giai đoạn 1: làm gì trước
- Giai đoạn 2: chuẩn hóa gì
- Giai đoạn 3: tối ưu gì sau
- Phần nào nên sửa trước để hiệu quả cao nhất
- Phần nào ảnh hưởng nhiều nhất đến người dùng cuối

### 6. Đề xuất chuẩn UX cho desktop app
Đưa ra guideline cụ thể cho phần mềm desktop:
- Font tối thiểu nên dùng
- Row height cho bảng
- Padding cho form
- Khoảng cách giữa section
- Cách tổ chức action primary/secondary
- Cách hiển thị loading nếu thao tác > 2 giây, > 10 giây
- Cách viết error message thân thiện
- Cách xử lý form resize trên laptop và màn hình lớn

## Format đầu ra mong muốn
Hãy trả lời theo cấu trúc sau:

1. Executive Summary
2. Top vấn đề ưu tiên
3. Giải pháp UI/UX tổng thể
4. Đề xuất áp dụng AntdUI 100% theo từng khu vực
5. Design system đề xuất
6. Action plan theo giai đoạn
7. Quick wins có thể làm ngay
8. Refactor lớn nên làm tiếp theo
9. Checklist nghiệm thu sau khi sửa

## Yêu cầu quan trọng
- Chỉ tập trung vào hướng cải thiện thực chiến, tránh lý thuyết dài dòng
- Mọi đề xuất phải bám sát audit đã nêu
- Luôn ưu tiên tính nhất quán, dễ dùng, dễ maintain
- Mọi thành phần UI mới/chỉnh sửa phải theo **AntdUI 100%**
- Nếu cần ví dụ, hãy nêu ví dụ theo kiểu WinForms/Desktop
- Nếu có đoạn code minh họa, hãy viết theo hướng clean, có thể tái sử dụng

Bắt đầu bằng việc viết:
- Executive Summary ngắn
- Sau đó là bảng ưu tiên vấn đề
- Sau đó là phương án refactor UI/UX chi tiết dùng AntdUI 100%