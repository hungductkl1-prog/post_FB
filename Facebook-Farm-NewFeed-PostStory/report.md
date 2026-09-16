# BÁO CÁO DỰ ÁN — QN Auto Phone (Facebook Farm New Feed Post Story)

---

## 1. CHI TIẾT DỰ ÁN

### 1.1. Tên dự án
**QN Auto Phone** (tên mã: `Facebook-Farm-NewFeed-PostStory`)

### 1.2. Mục tiêu
Xây dựng nền tảng desktop tự động hóa thiết bị Android hàng loạt để vận hành các tài khoản mạng xã hội (Facebook, Instagram, Threads) nhằm mục đích:
- Tương tác tự động (newsfeed, story, reel, livestream, group, page)
- Đăng bài tự động (tường, nhóm, page, story, reel)
- Buff tương tác (follow, like page, kết bạn)
- Quản lý thiết bị Android tập trung từ xa qua ADB + scrcpy
- Farm xu / spam tương tác chéo

### 1.3. Kiến trúc tổng quan
Dự án là **mono-repo .NET** gồm nhiều project:

```
LamToolAutoPhonePrime/
├── Facebook-Farm-NewFeed-PostStory/   # WinForms App chính
├── AutoAndroid/                        # Core thư viện Android automation
├── Sunny.Subd.Core/                    # Engine chạy kịch bản (farming logic)
├── Sunny.Subdy.Common/                 # Common utilities, API clients
├── Sunny.Subdy.Data/                   # Data layer (SQLite models, contexts)
├── Sunny.Subdy.Server/                 # HTTP API server
├── Sunny.Subdy.AutoUpdate/             # Auto-update module
├── ScrcpyNet/                          # Scrcpy .NET binding (stream Android screen)
├── AntdUI/                             # UI component library
├── AntdUI.EmojiFluentFlat/
├── HttpRequestLib/                     # Native C++ HTTP library
├── LTPhoneHelper/                      # Python-based môi trường setup
├── ORC-Helper-Python/                  # Python OCR helper (Tesseract)
└── View-Control/                       # Ứng dụng view Android từ xa
```

### 1.4. Quy mô
- ~500+ file mã nguồn C#
- File lớn nhất: `FacebookFarming.cs` (~762KB) — xử lý toàn bộ tương tác Facebook
- 7 project .NET + 2 project Python + 1 project C++ native

---

## 2. LOGIC DỰ ÁN

### 2.1. Luồng hoạt động chính

```
KHỞI ĐỘNG → KIỂM TRA MÔI TRƯỜNG → LICENSE CHECK → MAIN FORM
    ↓                                                        ↓
    ├─ ADB + ATX ─── Kết nối thiết bị Android ──────────────┤
    ├─ Quản lý tài khoản Facebook/IG/Threads ────────────────┤
    ├─ Tạo kịch bản (Script) ─── Gán tài khoản ─────────────┤
    └─ Chạy Job ─── Theo dõi trạng thái real-time ──────────┘
```

### 2.2. Flow chi tiết

#### A. Khởi động (Program.cs)
1. Set UTF-8 encoding cho process con (tránh lỗi tiếng Việt)
2. Gắn global exception handlers (ThreadException, UnhandledException, UnobservedTaskException)
3. Kiểm tra môi trường (ADB + Node.js)
4. Nếu thiếu → chạy `QNHelper.exe` để cài đặt
5. Load font, kiểm tra license (Google Sheets-based), hiển thị splash screen
6. Mở form chính `fMain`

#### B. Kết nối thiết bị
1. **Phase 1 - Fast scan**: Gọi `adb devices` lấy danh sách serial, tạo `DeviceModel` tối thiểu, hiển thị ngay lên UI
2. **Phase 2 - Setup song song**: Với mỗi device, song song:
   - Forward ADB port (7912)
   - Lấy device name + OS version
   - Force portrait mode (tránh xoay màn hình)
   - Cài đặt & kết nối **ATX (UI Automator 2)** — timeout 30s
   - Kiểm tra kết nối internet
3. **Health check nền**: Chạy vòng lặp kiểm tra ADB + internet, cập nhật trạng thái

#### C. Quản lý tài khoản
- Lưu SQLite: username, password, 2FA, proxy, cookies, trạng thái
- Import/export hàng loạt
- Kiểm tra live (còn sống) định kỳ
- Hỗ trợ Facebook, Instagram, Threads

#### D. Thực thi kịch bản (MainService)
1. Duyệt danh sách tài khoản đã gán kịch bản
2. Với mỗi tài khoản:
   - Mở ứng dụng Facebook (hoặc IG/Threads) trên thiết bị
   - Dùng **XPath UI Automator** để dò tìm element trên màn hình
   - Xử lý theo state machine (login, captcha, checkpoint, action...)
   - Ghi log + cập nhật trạng thái real-time lên UI
3. Hỗ trợ timeout, delay, sleep theo khung giờ

---

## 3. FRAMEWORK SỬ DỤNG

### 3.1. Core
| Framework | Mục đích |
|---|---|
| **.NET 9.0** | Nền tảng chính, target `net9.0-windows` |
| **WinForms** | Giao diện desktop (GUI) |
| **Native AOT** | Biên dịch native (PublishAot=true), giảm kích thước, tăng performance |
| **WinFormsComInterop** | Hỗ trợ COM interop cho WinForms AOT |

### 3.2. Android Automation
| Framework | Mục đích |
|---|---|
| **ADB (Android Debug Bridge)** | Giao tiếp với thiết bị Android qua CLI/socket |
| **ATX (UI Automator 2)** | Tự động hóa UI Android (bấm, vuốt, tìm element theo XPath) — chạy trên thiết bị qua `com.github.uiautomator` |
| **scrcpy** | Stream màn hình Android qua USB/WiFi (video H.264) |
| **SDL2** | Render video scrcpy + xử lý touch event |
| **OpenCV / Emgu.CV** | Computer vision: template matching, nhận diện màu sắc |
| **Tesseract** | OCR (quét text từ màn hình Android) |

### 3.3. Giao tiếp & Data
| Framework | Mục đích |
|---|---|
| **SQLite** (qua custom context) | Lưu trữ: tài khoản, thiết bị, kịch bản, lịch sử job |
| **WebSocket** | Giao tiếp với ATX agent trên thiết bị |
| **HttpRequestLib** (C++ native) | HTTP requests hiệu năng cao |
| **xNet** | HTTP client thay thế (legacy) |

### 3.4. UI & Design
| Framework | Mục đích |
|---|---|
| **AntdUI** | UI component library (Ant Design style cho WinForms) |
| **SDL2** | Render video + overlay text lên device view |
| **GDI+ / System.Drawing** | Render tùy chỉnh DataGridView, badge, icon |

### 3.5. Services bên thứ ba
| Service | Mục đích |
|---|---|
| **GuruCaptcha** | Giải captcha tự động |
| **GoLike / VipIG / TuongTacCheo** | API buff tương tác chéo |
| **Google Sheets** | License/activation check |

---

## 4. NGÔN NGỮ SỬ DỤNG

| Ngôn ngữ | Vai trò | Tỷ lệ |
|---|---|---|
| **C#** | Toàn bộ logic chính, GUI, automation engine | ~95% |
| **Python** | Environment setup (QNHelper), OCR helper | ~3% |
| **C++** | HttpRequestLib native library | ~2% |
| **XML** | Designer files, cấu hình WinForms | — |

---

## 5. QUÁ TRÌNH VẬN HÀNH

### 5.1. User flow

```
1. Mở app → Splash screen → License check
2. Cắm/Hiển thị danh sách thiết bị Android
3. Mỗi thiết bị tự động kết nối ATX (UI Automator)
4. Import tài khoản Facebook / Instagram / Threads
5. Tạo kịch bản (kết hợp nhiều hành động):
   ├─ Đăng bài (tường, nhóm, page, story, reel)
   ├─ Tương tác (newfeed, story, reel, livestream, group, page, wall)
   ├─ Kết bạn / Hủy kết bạn
   ├─ Follow/Unfollow
   ├─ Buff like page, buff follow UID
   ├─ Nhắn tin hàng loạt
   ├─ Đổi tên, đổi avatar, đổi mật khẩu
   └─ View watch, xem reel, xem story ...
6. Gán kịch bản cho tài khoản → Chạy job
7. Theo dõi real-time: trạng thái, FPS device view, history log
8. Dashboard tổng quan: số tài khoản đang chạy, hoàn thành, lỗi
```

### 5.2. Hệ thống theo dõi

- **Status bar**: RAM, CPU, số thiết bị online, tài khoản đang chạy
- **Device view**: Stream real-time từng thiết bị (SDL2 rendering, ~30fps)
- **History grid**: Log chi tiết từng job (thời gian, trạng thái, kết quả)
- **Dashboard**: Biểu đồ tổng quan hoạt động

---

## 6. ĐẶC BIỆT CHI TIẾT: VẬN HÀNH AUTOMATION ANDROID

### 6.1. Kiến trúc Automation Stack

```
┌─────────────────────────────────────────────────────────────┐
│                      QN Auto Phone App (WinForms)           │
├─────────────────────────────────────────────────────────────┤
│  MainService (Job Executor) → FacebookService/Instagram     │
│       ↓                                                     │
│  ADBClient (Giao tiếp thiết bị)                            │
│       ↓                                                     │
│  ┌─────────────────┬──────────────────┬──────────────────┐  │
│  │ ATXService       │ ADBHelper        │ ImageScanOpenCV │  │
│  │ (UI Automator 2) │ (ADB Shell cmd)  │ (OpenCV Vision) │  │
│  └────────┬─────────┴────────┬─────────┴────────┬─────────┘  │
│           ↓                  ↓                  ↓            │
│  ┌──────────────────────────────────────────────────────┐    │
│  │           Android Device (qua USB/WiFi)              │    │
│  │  ┌──────────────┐ ┌─────────────┐ ┌──────────────┐ │    │
│  │  │ ATX Agent    │ │ adb shell   │ │ Scrcpy       │ │    │
│  │  │ (uiautomator)│ │ (commands)  │ │ (screen rec) │ │    │
│  │  └──────────────┘ └─────────────┘ └──────────────┘ │    │
│  └──────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

### 6.2. Chi tiết các tầng automation

#### A. ADBClient — Lớp điều khiển trung tâm
- **File**: `AutoAndroid/Clients/ADBClient.cs` (~120KB)
- Chứa hầu hết các thao tác với thiết bị:
  - `AppStart/AppStop/AppWait`: Quản lý ứng dụng
  - `InstallApp/UninstallApp`: Cài/gỡ APK
  - `Shell`: Gửi lệnh ADB shell
  - `FindElement`: Tìm UI element bằng XPath (qua ATX)
  - `ElementWithAttributes`: Click element theo XPath
  - `Swipe/Click/LongClick`: Thao tác cảm ứng
  - `GetClipboardText/Input`: Thao tác clipboard
  - `EnableWifi/DisableWifi/ConnectWifi`: Quản lý kết nối
  - `ChangInfo`: Spoof thông tin thiết bị (MaxChange)
- **Cơ chế dừng an toàn**: mọi vòng lặp dài đều kiểm tra `ThrowIfStopped()`, `InterruptibleSleep()` chia nhỏ 100ms để thoát ngay khi user bấm Dừng

#### B. ATXService — UI Automator 2 Agent
- **File**: `AutoAndroid/Services/ATXService.cs` (~35KB)
- **Luồng kết nối**:
  1. **Install**: Cài `com.github.uiautomator` APK lên thiết bị (nếu chưa có hoặc outdated)
  2. **Connect**: Kết nối WebSocket tới `http://127.0.0.1:{port}`
  3. **RunUiautomator**: Khởi chạy UIAutomator trên thiết bị (forward port 7912)
  4. **Fallback**: Nếu thất bại → reinstall + retry
- **API quan trọng**:
  - `GetWindowHierarchy()`: Lấy cây UI XML của màn hình hiện tại
  - `FindNodeByXPath()`: Duyệt cây UI tìm element
  - `Click(region)`: Click vào tọa độ
  - `Swipe()`: Vuốt màn hình
  - `Input(text)`: Nhập text

#### C. DeviceServices — Quản lý devices trung tâm
- **File**: `AutoAndroid/Services/DeviceServices.cs` (~32KB)
- **2-phase loading**:
  - Phase 1: `GetSerialsFast()` — chỉ gọi `adb devices`, không shell, trả về ngay
  - Phase 2: `SetupDeviceAsync()` / `ConnectAll()` — song song hóa setup từng device
- **ConnectAll()**:
  - Mỗi device chạy trong `Task.Run()` riêng
  - Race giữa connect ATX vs timeout 30s (`Task.WhenAny`)
  - Set `IsLive`, `TypeColor`, `Status` cho UI binding
- **HealthCheck nền**: `UpdateDeviceOnlineStatus()` chạy định kỳ, probe ADB + internet

#### D. MainService — Job Executor Engine
- **File**: `Sunny.Subd.Core/Services/MainService.cs` (~49KB)
- **Luồng xử lý job**:
  1. Load kịch bản (Script) từ DB (chuỗi các hành động)
  2. Với mỗi tài khoản được gán:
     - Tạo `ADBClient` + `FacebookService` / `InstagramService`
     - Mở app → chờ load → phát hiện trạng thái
     - Vòng lặp login (phát hiện checkpoint, captcha, block...)
     - Thực thi từng hành động trong kịch bản
     - Delay ngẫu nhiên giữa các hành động (giả lập hành vi người)
  3. Cập nhật `Account.Status` + `Device.Status` real-time (data-binding WinForms)
  4. Ghi `JobHistory` vào SQLite

#### E. FacebookService — Xử lý tương tác Facebook
- **File**: `Sunny.Subd.Core/Facebook/FacebookService.cs` (~34KB)
- **State machine đăng nhập**:
  ```
  Mở app → Tìm cửa sổ đăng nhập
    ├─ Nếu loading → chờ
    ├─ Nếu checkpoint → báo lỗi (CP_282, CP_956...)
    ├─ Nếu captcha → gọi GuruCaptcha → giải → tiếp tục
    ├─ Nếu block → click "Dismiss" hoặc báo lỗi
    ├─ Nếu đã login → vào main screen
    └─ Nếu chưa → nhập user/pass → xử lý 2FA → hoàn tất
  ```
- **Phát hiện trạng thái qua XPath**: Dùng `FindElement()` với thư viện XPath đồ sộ (`XpathHelper` / `FacebookHander`) để ánh xạ màn hình hiện tại → hành động tương ứng
- **Hỗ trợ checkpoint hiện tại**: 282, 956, review, block, etc.

#### F. FacebookFarming — Thực thi hành động Facebook
- **File**: `Sunny.Subd.Core/Facebook/FacebookFarming.cs` (~762KB) — file lớn nhất dự án
- Chứa toàn bộ logic thao tác Facebook trên giao diện:
  - `DangBaiTuong()`: Đăng bài lên tường
  - `DangBaiNhom()`: Đăng bài vào nhóm
  - `DangBaiPage()`: Đăng bài lên page
  - `DangStory()`: Đăng story
  - `DangReel()`: Đăng reel
  - `TuongTacNewfeed()`: Tương tác newsfeed (like, comment, share)
  - `TuongTacStory()`: Xem/like story
  - `TuongTacLivestream()`: Tương tác livestream
  - `GuiLoiMoiKetBan()`: Gửi lời mời kết bạn
  - `NhanTinBanBe()`: Nhắn tin bạn bè
  - ... và 30+ hành động khác
- **Mỗi action đều**:
  1. Dùng XPath xác định element cần tương tác (dựa trên giao diện Facebook thực tế)
  2. Delay + scroll nếu cần
  3. Click hoặc input text
  4. Kiểm tra kết quả (popup, dialog, xác nhận)
  5. Xử lý lỗi (timeout, crash, unexpected dialog)
  6. Ghi log chi tiết

#### G. XPath UI Detection
- Hệ thống phát hiện element Android bằng **XPath** (không phải coordinate cố định)
- Cách hoạt động:
  1. ATX lấy toàn bộ cây UI XML từ thiết bị
  2. Dùng regex pattern để match XPath custom (VD: `//android.widget.TextView[contains(@text, 'Đăng')]`)
  3. Hỗ trợ compound class (android.*, androidx.*)
  4. Click an toàn qua region (tránh element di chuyển)
- Thư viện XPath pattern được maintain riêng: `FacebookHander.cs`, `XpathHelper.cs`

#### H. Device Spoofing (MaxChange)
- **File**: `AutoAndroid/Services/MaxChangeService.cs` (~19KB)
- Cho phép thay đổi thông tin thiết bị Android:
  - Brand / Model / Device name
  - IMEI, MAC, Android ID (qua Xposed module)
  - Reset để tránh fingerprint tracking
- Sử dụng APK riêng: `LamToolChanger.apk` + `DeviceInfoHW.apk`

#### I. Computer Vision & OCR
- **ImageScanOpenCV** (`AutoAndroid/Vision/`):
  - Template matching: tìm hình ảnh trên màn hình Android
  - Color detection: tìm vùng màu (dùng cho auto-click)
  - Nhận diện region
- **OCR (Tesseract)**:
  - Đọc text từ screenshot thiết bị
  - Dùng cho captcha, verify text, detect trạng thái

#### J. Scrcpy — Screen Streaming
- **File**: `ucDeviceView.cs` (~44KB) — Device view control
- Hiển thị real-time screen Android trên WinForms:
  - **SDL2 rendering** (accelerated, fallback software)
  - ~30fps mặc định (cấu hình 1-120fps)
  - **Touch forwarding**: Mouse → Android touch events
  - **Clipboard sync**: Ctrl+C / Ctrl+V qua lại
  - **Key forwarding**: Bàn phím → Android key events
  - **Overlay**: Device name, ID, bottom text
  - **Multi-view support**: Nhiều device tiles trên cùng form
  - **Sync mode**: Broadcast touch event sang tile khác

### 6.3. Cơ chế đồng bộ & Safety

- **Cancellation pattern**: `CancellationToken` + `Running` flag + `InterruptibleSleep()`
- **Thread safety**: `lock` cho shared resources, `Task.Run` cho I/O
- **Timeout cứng**: Connect ATX timeout 30s, mỗi action có timeout riêng
- **Error handling**: Global exception handlers + first-chance exception logging + crash dumps
- **Auto-retry**: ATX setup retry, captcha retry (5 lần), login retry
- **DB persistence**: Device state, account state, job history được lưu SQLite

### 6.4. Security & License

- **License check**: Google Sheets-based activation (DeviceId + WMI)
- **Obfuscation**: Native AOT biên dịch trước, obfuscate binaries
- **Registry startup**: Tự động khởi động cùng Windows (tùy chọn)

---

## TỔNG KẾT

**QN Auto Phone** là một hệ thống tự động hóa Android hoàn chỉnh cho social media farming, với:
- **Giao diện WinForms** chuyên nghiệp (AntdUI)
- **Kiến trúc đa tầng** rõ ràng (UI → Service → Automation → Device)
- **Xử lý song song** tối đa (async/await, Task.WhenAll)
- **State machine linh hoạt** cho Facebook login (phát hiện checkpoint, captcha, block)
- **XPath-based UI automation** (không coordinate cứng — chống chọi với Facebook update)
- **Device view real-time** qua scrcpy + SDL2
- **Công nghệ đa dạng**: ADB, ATX, OpenCV, Tesseract OCR, WebSocket, SQLite, Native AOT
