# BÁO CÁO PHÂN TÍCH NGUYÊN NHÂN GỐC — LỖI DEVICE MẤT KẾT NỐI

## 1. KIẾN TRÚC TỔNG THỂ — LUỒNG KẾT NỐI

```
MainService.RunAsync()
  → ConnectAndPrepareDeviceAsync()
    → ConnectDeviceAsync() → ADBClient.Connect()
      → ConnectAdb()              [ADBClient.cs:689] — loop kiểm tra ADB online
      → ATX.Connect()             [ATXService.cs:91] — forward port + chạy UIAutomator

  → FacebookFarming.ExecuteAsync()
    → ScriptAction loop
      → ADBClient.Shell()         [ADBClient.cs:767] — gọi ADB, retry 3 lần
      → ATX.JsonRpc()             [ATXService.cs:273] — gọi ATX agent HTTP
      → ATX.Screenshot/Click/Swipe [ATXService.cs:744+] — tương tác UI

  → AdbTrackDevicesService        [AdbTrackDevicesService.cs]
    → poll adb devices mỗi 5s
    → DeviceServices.UpdateDeviceOnlineStatus()
      → service check settings per device [DeviceServices.cs:406]

  → ADBHelper.Shell()             [ADBHelper.cs:283]
    → retry 3 lần, gọi Connect() nếu device offline
```

## 2. PHÂN TÍCH NGUYÊN NHÂN GỐC (ROOT CAUSE)

### ⚡ Root Cause #1 — Semaphore Contention (CAUSE CHÍNH)

**File:** `ProcessHelper.cs:10`
```csharp
public const int MaxConcurrentCmdProcesses = 20;
private static readonly SemaphoreSlim CmdSemaphore = new SemaphoreSlim(20, 20);
```

**Vấn đề:** Toàn bộ hệ thống chỉ có **20 slot semaphore** cho MỌI lệnh ADB — bao gồm:
- `AdbTrackDevicesService` (poll mỗi 5 giây: N device × 1 lệnh `service check settings`)
- `DeviceServices.UpdateDeviceOnlineStatus` (N device × 1 lệnh)
- `FacebookFarming` script actions (Shell(), Click(), Swipe(), Screenshot(), ...)
- `MainService` operations (AppStart, ElementWithAttributes, GetXMLSource, ...)
- `DeviceHealthCheckService` pings

**Cascade failure với 50-100 devices:**
1. Mỗi device chạy script có thể giữ 1-2 slot semaphore liên tục
2. `AdbTrackDevicesService` chiếm thêm N slot mỗi 5 giây
3. Semaphore cạn → các lệnh ADB mới **xếp hàng đợi**
4. Lệnh `service check settings` timeout (mất >5s chờ semaphore) → báo **"Mất kết nối"** sai
5. Device bị đánh dấu offline → `ADBHelper.Shell()` gọi `Connect()` 
6. `ConnectAdb()` vào **vòng lặp vô hạn** (`while(Running)`), liên tục giữ semaphore
7. Càng nhiều device vào vòng lặp, semaphore càng khan hiếm, càng nhiều device bị timeout

**Bằng chứng:**
- `timeout/no response` là lý do phổ biến nhất trong log `ConnectAdb()` [ADBClient.cs:708]
- Device vẫn sáng màn hình, vẫn có ADB — chỉ là lệnh ADB không kịp chạy vì semaphore

### ⚡ Root Cause #2 — Vòng lặp vô hạn trong ConnectAdb()

**File:** `ADBClient.cs:689-719`
```csharp
while (Running)  // ← KHÔNG có giới hạn số lần thử
{
    index++;
    string text = ProcessHelper.RunAdbWithTimeout($"-s {Device.Serial} shell service check settings", 5);
    ...
    LogHelper.Log($"Mất kết nối, chờ ExecuteAdb [{index}] cmd: {reason}");
    ...
    if (index > 10_000) { index = 0; }  // reset đếm nhưng KHÔNG THOÁT
}
```

**Hậu quả:**
- Một device mất kết nối thật → log spam hàng ngàn dòng
- Chiếm semaphore vô thời hạn → các device khác không chạy được ADB
- Không có cơ chế backoff, không có max retry, không có exponential delay
- `reconnect` được gọi mỗi lần loop — gây thêm áp lực lên ADB server

### ⚡ Root Cause #3 — ADBHelper.Shell() gọi Connect() đệ quy

**File:** `ADBHelper.cs:314-327`
```csharp
if (error.Contains("device offline") || error.Contains("not found"))
{
    retryCount++;
    if (!cmd.Contains("reconnect"))
        Shell("reconnect");  // ← gọi lại Shell() !!
    if (!GetDevices().Contains(_client.Device.Serial))
        _client.Connect();   // ← Connect() gọi ConnectAdb() vòng lặp vô hạn
    continue;
}
```

**Vấn đề:** Khi Shell() gặp lỗi:
1. Nó gọi lại `Shell("reconnect")` → đệ quy
2. Nếu vẫn lỗi, gọi `_client.Connect()` → vào `ConnectAdb()` infinite loop
3. Trong thời gian này, **semaphore bị chiếm giữ**, các device khác không chạy được ADB

### ⚡ Root Cause #4 — SocketHelper tạo socket mới mỗi request + Dispose lỗi

**File:** `SocketHelper.cs:41-85, 269-273`
```csharp
public SocketHelper(string url)
{
    ...
    socket = new Socket(...);
    socket.BeginConnect(_host, _port, null, null);
    bool success = result.AsyncWaitHandle.WaitOne(2000, true);  // 2s timeout
    
    if (!socket.Connected)
    {
        socket?.Dispose();
        throw new Exception($"Failed to connect to {_host}:{_port}.");
    }
    _socket = socket;
}

public void Dispose()
{
    _socket.Close();     // ← Close() trước
    _socket?.Dispose();  // ← rồi Dispose(): nếu Close() throw, Dispose() không chạy
}
```

**Vấn đề:**
- Mỗi lần gọi JsonRpc, Info, DumpHierarchy, Screenshot → tạo socket mới
- 2 giây timeout cho mỗi connection
- Nếu ATX agent crash, mỗi request mất 2 giây rồi mới fail
- Dispose gọi `Close()` trước `Dispose()` — nếu Close() throw exception, socket không được Dispose
- Với 100 devices × nhiều request, có thể rò rỉ socket

### ⚡ Root Cause #5 — ADB Restart() không làm gì

**File:** `ADBHelper.cs:46-53`
```csharp
public static void Restart()
{
    // KillAllAdbProcesses();    ← COMMENTED OUT
    //   _serverStarted = false;
    //  Thread.Sleep(500);
    // ProcessHelper.RunAdbWithTimeout($"start-server", 10);
    //_serverStarted = true;
}
```

**Vấn đề:** Khi `CheckAndFixConnectionLeak()` phát hiện >15 CLOSE_WAIT connection trên port 5037, nó gọi `Restart()` — nhưng method này hoàn toàn rỗng (comment hết). Leak không được xử lý, tích tụ đến khi ADB server không phản hồi.

### ⚡ Root Cause #6 — CheckAndFixConnectionLeak() chạy netstat trên mỗi lệnh ADB

**File:** `ProcessHelper.cs:85-86`
```csharp
if (!adbCommand.Contains("start-server") && !adbCommand.Contains("kill-server"))
    ADBHelper.CheckAndFixConnectionLeak();
```

**Vấn đề:** `RunAdbWithTimeout()` được gọi cho MỌI lệnh ADB. Mỗi lần chạy `netstat -ano | findstr :5037` là một process riêng. Với 20+ process ADB chạy song song, đây là overhead không cần thiết.

### ⚡ Root Cause #7 — AdbTrackDevicesService tạo áp lực tuần hoàn

**File:** `AdbTrackDevicesService.cs:37-57`

Mỗi 5 giây:
1. Gọi `adb devices` (chiếm semaphore)
2. Gọi `DeviceServices.UpdateDeviceOnlineStatus()` 
3. UpdateDeviceOnlineStatus chạy N device × 1 `service check settings` (chiếm N slot semaphore)

Với 100 devices: mỗi 5 giây, 100 lệnh ADB được ném vào semaphore queue. Nếu semaphore đang bận (do các device đang chạy script), các lệnh này timeout → báo "Mất kết nối" sai.

### ⚡ Root Cause #8 — Không phân biệt loại lỗi

Mọi lỗi đều thành "Mất kết nối" — nhưng thực tế có nhiều loại:
| Triệu chứng | Nguyên nhân thật |
|---|---|
| `service check settings` timeout | Semaphore contention / ADB server overload |
| `service check settings` returns "not found" | Device settings provider chưa ready |
| JsonRpc "Failed to connect" | ATX agent dead / port forward broken |
| `adb devices` không thấy serial | USB disconnect / ADB daemon restart |
| Shell() "device offline" | Device ADB daemon temporarily busy |
| JsonRpc null response | ATX agent overload / OOM |

## 3. DỮ LIỆU — TẦN SUẤT & THỜI ĐIỂM

### Tần suất (ước tính dựa trên code):
- **AdbTrackDevicesService:** poll mỗi 5 giây → 12 lần/phút × số lượng device
- **UpdateDeviceOnlineStatus:** mỗi poll chạy N `service check settings` song song
- **ConnectAdb() loop:** log "Mất kết nối" mỗi ~5 giây khi trong loop
- **Script actions:** tần suất tùy thuộc kịch bản, có thể 5-20 ADB/cmd mỗi phút

### Thời điểm phát sinh:
- **Khởi động system:** lúc tất cả device cùng connect → semaphore saturation
- **Sau khi một device fail:** nó vào ConnectAdb() loop, chiếm semaphore, kéo theo device khác fail
- **Khi AdbTrackDevicesService poll + script actions overlap:** peak semaphore demand

## 4. TÁI HIỆN LỖI

### Cách tái hiện (dựa trên code analysis):

1. **Kịch bản 1 — Semaphore exhaustion:** 
   - Chạy 50+ devices cùng lúc
   - Mỗi device chạy script action (giữ 1-2 semaphore slots)
   - `AdbTrackDevicesService` poll mỗi 5s → thêm 50 requests vào queue
   - Các lệnh `service check settings` timeout → báo "Mất kết nối"

2. **Kịch bản 2 — Cascade failure:**
   - Chặn kết nối USB của 1 device (hoặc tắt ATX agent)
   - Device → `ConnectAdb()` infinite loop → giữ semaphore
   - Device khác gọi Shell() → timeout → cũng vào loop
   - Effect domino

3. **Kịch bản 3 — ATX agent crash:**
   - Device ATX agent crash (OOM, exception, killed)
   - `JsonRpc()` fail → gọi `Connect()` → `RunUiautomator()` → lại fail → trả về false
   - Device báo "Không connect được ATX"

## 5. KẾT LUẬN — CHUỖI NGUYÊN NHÂN

```
Semaphore 20 slots
    ↓
ADB command queue dài (50-100 devices × nhiều cmd)
    ↓
service check settings timeout (>5s chờ semaphore)
    ↓
Báo "Mất kết nối" — FALSE POSITIVE
    ↓
ConnectAdb() infinite loop — chiếm semaphore vô thời hạn
    ↓
Các device khác cũng timeout → cũng vào loop
    ↓
CASCADE FAILURE — càng ngày càng nhiều device bị "Mất kết nối"
```

## 6. PHƯƠNG ÁN KHẮC PHỤC

### Solution A — Tăng semaphore + backoff (Low risk, immediate)

- Tăng `MaxConcurrentCmdProcesses` từ 20 lên 100-200
- Thêm max retry vào `ConnectAdb()` — thoát sau 5-10 lần thất bại liên tiếp
- Thêm exponential backoff (1s, 2s, 4s, 8s, ...)
- **Ưu điểm:** Đơn giản, giảm cascade ngay lập tức
- **Nhược điểm:** Không giải quyết root cause gốc

### Solution B — Resource pool per device (Medium risk)

- Mỗi device có semaphore riêng (1-2 slot)
- AdbTrackDevicesService dùng semaphore riêng
- Loại bỏ global semaphore 20 slots
- ConnectAdb() có max retry + backoff
- **Ưu điểm:** Cô lập failure, không cascade
- **Nhược điểm:** Cần refactor ProcessHelper, thay đổi kiến trúc

### Solution C — Health check chuyên biệt + self-healing (Medium risk)

- Thay `service check settings` bằng check cụ thể:
  - ADB online: `adb -s serial get-state` (nhanh hơn)
  - ATX alive: HTTP GET `/` thay vì JSON-RPC (nhẹ hơn)
  - WebSocket: kiểm tra port forward còn hoạt động
- Mỗi loại lỗi có trạng thái riêng:
  - "ADB offline" ≠ "ATX disconnected" ≠ "No internet"
- Giới hạn số lần reconnect
- **Ưu điểm:** Chẩn đoán chính xác, giảm false positive
- **Nhược điểm:** Cần thay đổi nhiều file, kiểm tra kỹ

## 7. FILE CẦN THAY ĐỔI

| File | Lý do |
|------|-------|
| `AutoAndroid/Helpers/ProcessHelper.cs` | Tăng semaphore, thêm backoff |
| `AutoAndroid/Clients/ADBClient.cs` | Giới hạn ConnectAdb() loop |
| `AutoAndroid/Helpers/ADBHelper.cs` | Fix Restart() method |
| `AutoAndroid/Services/DeviceServices.cs` | Phân biệt loại lỗi |
| `AutoAndroid/Services/ATXService.cs` | Giới hạn retry trong JsonRpc |
| `AutoAndroid/Helpers/SocketHelper.cs` | Fix Dispose pattern |

---

**Báo cáo dựa trên phân tích source code. Chưa có test thực tế.**
**Cần phê duyệt trước khi triển khai bất kỳ fix nào.**
