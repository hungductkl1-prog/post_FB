# NHIỆM VỤ TỐI ƯU HÓA HỆ THỐNG KẾT NỐI DEVICE VÀ LOẠI BỎ CASCADE FAILURE

## Bối cảnh

Sau khi hoàn thành phân tích source code và Root Cause Analysis, chúng ta đã xác định một số vấn đề nghiêm trọng trong kiến trúc hiện tại:

* Global Semaphore gây nghẽn toàn hệ thống.
* ConnectAdb() có khả năng tạo vòng lặp reconnect vô hạn.
* Health Check đang tạo lượng lớn ADB command không cần thiết.
* Không phân biệt các loại lỗi (ADB, ATX, WebSocket, UIAutomator, Internet).
* Có nguy cơ cascade failure khi một số device gặp sự cố.
* Hệ thống chưa có cơ chế self-healing đúng nghĩa.
* Chưa có metric để xác định chính xác nguyên nhân gây nghẽn.

Mục tiêu lần này KHÔNG PHẢI FIX TỪNG BUG RIÊNG LẺ.

Mục tiêu là tối ưu lại toàn bộ kiến trúc quản lý kết nối để hệ thống có thể vận hành ổn định 24/7 với 50-100+ devices.

---

# YÊU CẦU THỰC HIỆN

## Giai đoạn 1 - Đo đạc trước khi sửa

Không được sửa code trước.

Trước tiên phải bổ sung metric và telemetry.

Thu thập:

* ADB Queue Length
* Semaphore Wait Time
* ADB Command Execution Time
* ADB Retry Count
* ConnectAdb Retry Count
* ATX Reconnect Count
* Socket Count
* Active Thread Count
* Active Task Count
* Device State Transition Count

Log theo thời gian thực.

Mục tiêu:

Chứng minh được chính xác điểm nghẽn đang nằm ở đâu.

---

## Giai đoạn 2 - Loại bỏ Global Bottleneck

Rà soát toàn bộ:

ProcessHelper
ADBClient
ADBHelper
DeviceServices
ATXService

Tìm tất cả điểm đang dùng chung tài nguyên toàn hệ thống.

Yêu cầu:

Không để một device lỗi có thể ảnh hưởng tới device khác.

Ưu tiên kiến trúc:

Device Isolation

Ví dụ:

* Device A lỗi
* Device B vẫn chạy
* Device C vẫn chạy

Không được phép tạo hiệu ứng domino.

---

## Giai đoạn 3 - Thiết kế lại Connection Management

Xây dựng Device Connection Manager mới.

Mỗi device phải có trạng thái riêng:

ADB Connected
ATX Connected
UIAutomator Running
Internet Connected
Facebook Running

Không được gộp thành một trạng thái:

"Mất kết nối"

Mỗi trạng thái phải có:

* thời gian cập nhật cuối
* số lần reconnect
* trạng thái hiện tại
* nguyên nhân lỗi gần nhất

---

## Giai đoạn 4 - Xây dựng Circuit Breaker

Mọi kết nối bên dưới phải có Circuit Breaker:

* ADB
* ATX
* JsonRpc
* WebSocket

Yêu cầu:

Nếu liên tục thất bại:

Fail x lần
↓
Open Circuit
↓
Tạm dừng
↓
Half Open
↓
Test lại
↓
Khôi phục

Không được reconnect vô hạn.

Không được spam ADB.

Không được spam log.

---

## Giai đoạn 5 - Self Healing

Thiết kế cơ chế phục hồi nhiều tầng.

Level 1

ATX mất kết nối

→ Reconnect ATX

Level 2

ATX không phản hồi

→ Restart UIAutomator

Level 3

UIAutomator không khởi động được

→ Reinstall Agent

Level 4

ADB lỗi

→ adb reconnect

Level 5

ADB daemon lỗi

→ Restart Device Session

Level 6

Device thật sự offline

→ Đánh dấu Offline

Không được restart toàn bộ hệ thống chỉ vì một device.

---

## Giai đoạn 6 - Tối ưu Health Check

Health Check hiện tại đang tạo áp lực quá lớn.

Yêu cầu:

* Giảm số lượng ADB command
* Cache trạng thái phù hợp
* Chỉ kiểm tra khi cần
* Tách riêng ADB Check và ATX Check

Mục tiêu:

Giảm tối thiểu 70% lượng lệnh health-check hiện tại.

---

## Giai đoạn 7 - Dashboard Debug

Xây dựng màn hình Debug nội bộ.

Hiển thị realtime:

Device Count
ADB Queue Length
Average Command Time
Reconnect Count
ATX Fail Count
Circuit Breaker State
Memory Usage
Thread Count
Socket Count

Mục tiêu:

Có thể nhìn vào dashboard và xác định ngay nguyên nhân khi lỗi xảy ra.

---

## Giai đoạn 8 - Stress Test

Bắt buộc thực hiện.

Kịch bản:

30 devices
50 devices
70 devices
100 devices

Test:

* mở Facebook
* lướt newsfeed
* mở profile
* xem story
* tương tác bài viết

Thời gian:

12 giờ
24 giờ
48 giờ

Thu thập:

* disconnect rate
* reconnect rate
* memory growth
* thread growth
* queue growth

---

# KẾT QUẢ PHẢI BÀN GIAO

Không commit trực tiếp.

Không merge.

Không release.

Phải gửi:

1. Technical Report
2. Benchmark Before / After
3. Kiến trúc mới đề xuất
4. Danh sách file thay đổi
5. Sequence Diagram
6. Stress Test Result
7. Risk Assessment

Sau khi hoàn thành:

Xuất toàn bộ báo cáo thành file markdown.

Dừng tại đó.

Tôi sẽ review báo cáo, benchmark và kiến trúc trước khi quyết định phương án triển khai cuối cùng.

---

# MỤC TIÊU CUỐI CÙNG

Hệ thống phải đáp ứng:

* Chạy ổn định 24/7.
* 100+ devices hoạt động đồng thời.
* Không cascade failure.
* Không reconnect vô hạn.
* Không false offline.
* Có khả năng tự phục hồi.
* Có đầy đủ metric để truy vết nguyên nhân trong vòng vài phút khi sự cố xảy ra.
