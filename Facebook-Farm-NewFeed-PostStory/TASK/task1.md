# NHIỆM VỤ ĐIỀU TRA VÀ KHẮC PHỤC LỖI DEVICE MẤT KẾT NỐI

## Bối cảnh

Dự án hiện tại là hệ thống Android Automation quy mô lớn sử dụng:

* .NET 9
* WinForms
* ADB
* ATX / UIAutomator2
* Scrcpy
* SQLite
* WebSocket

Hệ thống vận hành đồng thời từ 30 đến hơn 100 Android devices.

Hiện tại xuất hiện lỗi thường xuyên:

"Mở ứng dụng Facebook - Mất kết nối, chờ ExecuteAd..."

Tuy nhiên thực tế:

* Device vẫn sáng màn hình
* Device vẫn có internet
* Dây USB vẫn kết nối
* Nhiều trường hợp adb devices vẫn nhìn thấy serial
* Một số device vẫn chạy bình thường trong cùng thời điểm

Điều này cho thấy rất có thể hệ thống đang đánh đồng nhiều loại lỗi khác nhau thành một trạng thái "Mất kết nối".

---

# Mục tiêu

KHÔNG ĐƯỢC FIX THEO PHỎNG ĐOÁN.

KHÔNG ĐƯỢC THÊM RETRY BỪA BÃI.

KHÔNG ĐƯỢC COMMIT FIX KHI CHƯA XÁC ĐỊNH ROOT CAUSE.

Nhiệm vụ đầu tiên là:

Tìm chính xác nguyên nhân gốc gây ra lỗi.

Sau khi có đầy đủ bằng chứng mới được đề xuất phương án sửa.

---

# Yêu cầu điều tra

## Phase 1 - Truy vết luồng kết nối

Rà soát toàn bộ flow:

MainService
→ FacebookService
→ ADBClient
→ DeviceServices
→ ATXService
→ WebSocket
→ ADB Forward
→ UIAutomator2
→ Android Device

Vẽ sequence diagram thực tế.

Xác định:

* Điểm bắt đầu lỗi
* Điều kiện gây lỗi
* Thời điểm phát sinh
* Tần suất xuất hiện

---

## Phase 2 - Instrumentation

Bổ sung logging chi tiết.

Mọi lần lỗi phải ghi:

* Device Serial
* Account Id
* Thread Id
* Task Id
* Timestamp
* Current Action
* Current Service
* Exception đầy đủ
* Inner Exception
* Stack Trace

Không chấp nhận log kiểu:

"Mất kết nối"

mà không biết mất ở đâu.

---

## Phase 3 - Audit ADB

Kiểm tra:

adb devices

Trong lúc lỗi:

* device
* offline
* unauthorized

thiết bị đang ở trạng thái nào.

Log:

* adb state
* adb response time
* adb reconnect count

Kiểm tra:

* adb server restart
* adb timeout
* adb socket exhaustion

---

## Phase 4 - Audit ATX

Kiểm tra:

* ATX Agent còn sống không
* UIAutomator2 còn chạy không
* Port 7912 còn hoạt động không
* WebSocket còn hoạt động không

Mỗi lần báo mất kết nối phải xác định:

ADB chết
hay

ATX chết
hay

UIAutomator chết
hay

WebSocket chết

---

## Phase 5 - Audit Threading

Rà soát:

* Task.Run
* Task.WhenAny
* async/await
* CancellationToken

Tìm:

* deadlock
* race condition
* lost task
* cancelled task
* orphan task

Đặc biệt kiểm tra:

* ConnectAll()
* SetupDeviceAsync()
* UpdateDeviceOnlineStatus()

---

## Phase 6 - Audit Resource Leak

Khi chạy:

30 devices
50 devices
70 devices
100 devices

Theo dõi:

* RAM
* CPU
* Handle Count
* Thread Count
* Socket Count
* WebSocket Count
* ATX Connection Count

Chu kỳ ghi log:

1 phút/lần.

Mục tiêu:

Xác định có memory leak hoặc resource leak hay không.

---

## Phase 7 - Stress Test

Tạo môi trường test.

Kịch bản:

* mở Facebook
* lướt newsfeed
* xem story
* đóng mở app liên tục

Chạy:

12 giờ
24 giờ
48 giờ

Thu thập:

* disconnect count
* reconnect count
* crash count
* timeout count

---

# Deliverables bắt buộc

Sau khi hoàn thành điều tra phải gửi lại:

## 1. Root Cause Analysis

Mô tả:

* nguyên nhân gốc
* cách tái hiện
* tỷ lệ xảy ra

---

## 2. Technical Report

Bao gồm:

* log
* screenshot
* sequence diagram
* call stack
* performance metrics

---

## 3. Proposed Solution

Đưa ra:

* Solution A
* Solution B
* Solution C

Đánh giá:

* ưu điểm
* nhược điểm
* rủi ro
* ảnh hưởng hiệu năng

---

## 4. Pull Request

KHÔNG merge.

Chỉ commit lên branch riêng.

Gửi:

* source code
* technical report
* test result

để review.

---

# Quy trình phê duyệt

Sau khi hoàn thành:

1. Gửi toàn bộ báo cáo điều tra.
2. Gửi file markdown tổng hợp kết quả.
3. Gửi danh sách file đã sửa.
4. Gửi commit hash.
5. Dừng tại đó.

KHÔNG tự ý merge.
KHÔNG tự ý release.
KHÔNG tự ý publish.

Tôi sẽ review báo cáo, phân tích root cause và lựa chọn phương án tối ưu trước khi cho phép triển khai fix chính thức.

Mục tiêu cuối cùng là xác định chính xác nguyên nhân gây disconnect và xây dựng cơ chế self-healing ổn định cho hệ thống chạy 24/7 với số lượng lớn Android devices.
