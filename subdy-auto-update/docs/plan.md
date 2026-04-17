# Plan: SubdyAutoUpdate — Standalone Auto-Update Tool

## Mục tiêu

Tool **SubdyAutoUpdate.exe** là một file exe độc lập (AOT), không phụ thuộc vào phần mềm chính.
Khi `SubdyPhoneFarm.exe` bị lỗi không mở được, user chạy `SubdyAutoUpdate.exe` để tải và cài bản mới nhất.

---

## Cấu trúc project

```
subdy-auto-update/
├── docs/
│   └── plan.md                  ← file này
├── SubdyAutoUpdate/
│   ├── SubdyAutoUpdate.csproj   ← net9.0-windows, PublishAot=true, WinForms
│   ├── Program.cs               ← entry point, STAThread
│   ├── MainForm.cs              ← Form chính (AntdUI.Window)
│   ├── MainForm.Designer.cs
│   ├── UpdateService.cs         ← logic: check version, download, extract, restart
│   ├── LamToolApiClient.cs      ← gọi lamtool.net/api/license/check
│   ├── rd.xml                   ← AOT reflection config
│   └── app.manifest             ← requestedExecutionLevel = asInvoker
```

---

## Logic hoạt động

### 1. Khởi động
- `Program.cs` → `[STAThread]` → khởi tạo AntdUI, mở `MainForm`
- `MainForm` hiển thị ngay, **tự động bắt đầu toàn bộ quá trình** (không cần user nhấn nút)
- Không cần login

### 2. Kill process app chính (nếu đang chạy)
- Tìm process tên `SubdyPhoneFarm` bằng `Process.GetProcessesByName("SubdyPhoneFarm")`
- Nếu tìm thấy → `process.Kill()` → `process.WaitForExit(5000)`
- Hiển thị trạng thái: "Đang dừng SubdyPhoneFarm..."

### 3. Check version
- Lấy HWID bằng `DeviceId.Windows`
- Gọi API: `GET https://lamtool.net/api/license/check?tool_slug=subdyfarm&device_code={hwid}`
- Parse JSON: `license.tool.version` và `license.tool.updateUrl`
- So sánh với version hiện tại của `SubdyPhoneFarm.exe` trong cùng thư mục:
  - Đọc `FileVersionInfo.GetVersionInfo(exePath).FileVersion`
  - Nếu `updateUrl` rỗng hoặc không newer → hiển thị "Phần mềm đã là phiên bản mới nhất", dừng

### 4. Download
- Download `updateUrl` → `%TEMP%\SubdyUpdate\update.zip`
- Progress bar (AntdUI `Progress`) hiển thị % theo `Content-Length`
- Nếu không có `Content-Length` → indeterminate (`Loading = true`)
- Hiển thị trạng thái: "Đang tải bản cập nhật... X%"

### 5. Extract & Replace (BAT script — giống fUpdateAuto.cs hiện tại)
- Tạo file `.bat` trong `Path.GetTempPath()`
- BAT logic:
  1. `timeout /t 2` — chờ SubdyAutoUpdate.exe thoát
  2. Rename `SubdyPhoneFarm.exe` → `SubdyPhoneFarm-{oldVersion}.exe` (backup)
  3. `Expand-Archive` zip vào thư mục app
  4. `Start-Process SubdyPhoneFarm.exe -Verb RunAs`
  5. Xóa `%TEMP%\SubdyUpdate\`
  6. Tự xóa BAT
- Sau khi tạo BAT: `Process.Start(batPath)` → `Environment.Exit(0)`

### 6. Phát hiện thư mục cài đặt
- Ưu tiên: thư mục chứa `SubdyAutoUpdate.exe` (user đặt cùng chỗ với app chính)
- Nếu không tìm thấy `SubdyPhoneFarm.exe` → vẫn extract vào thư mục hiện tại

---

## UI (AntdUI)

```
MainForm (AntdUI.Window, fixed 480x320, no resize, center screen)
├── WindowBar — title "Subdy Auto Update", shadow
├── Avatar/Logo — icon app (64x64, centered)
├── Label — "SubdyPhoneFarm — Auto Update" (tiêu đề lớn)
├── Divider — text = trạng thái step hiện tại (VD: "Đang kiểm tra phiên bản...")
├── Progress — Value 0-100, Loading=true khi indeterminate
└── Label nhỏ — version cũ → version mới (hiện sau khi fetch xong)
```

**Không có button** — toàn bộ tự động chạy khi mở form.
Khi hoàn tất (thành công hoặc lỗi) hiện `AntdUI.Message` rồi tự đóng.
Toàn bộ UI dùng AntdUI controls, theme mặc định AntdUI (không cần embed font riêng).

---

## Cấu hình Project (.csproj)

```xml
<TargetFramework>net9.0-windows</TargetFramework>
<OutputType>WinExe</OutputType>
<UseWindowsForms>true</UseWindowsForms>
<PublishAot>true</PublishAot>
<InvariantGlobalization>false</InvariantGlobalization>
<AssemblyName>SubdyAutoUpdate</AssemblyName>
<_SuppressWinFormsTrimError>true</_SuppressWinFormsTrimError>
<BuiltInComInteropSupport>true</BuiltInComInteropSupport>
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
<!-- AOT size optimization -->
<OptimizationPreference>Size</OptimizationPreference>
<PublishLzmaCompressed>true</PublishLzmaCompressed>
<!-- Versioning tự động -->
<Version>$([System.DateTime]::Now.ToString("HH.dd.MM.yyyy"))</Version>
```

**Dependencies:**
- `AntdUI` (ProjectReference — dùng source cục bộ)
- `WinFormsComInterop` (NuGet)
- `VC-LTL` + `YY-Thunks` (NuGet — Windows compat)
- `DeviceId.Windows` (NuGet — lấy HWID)

**Không** reference: Sunny.Subdy.*, AutoAndroid, ScrcpyNet, HttpRequestLib.dll
→ Dùng `HttpClient` thuần thay vì `HttpRequestLib` native

---

## API dùng lại (không copy code, tự viết gọn)

```csharp
// LamToolApiClient.cs — chỉ cần 2 method:

// 1. Lấy version + url từ lamtool.net
static (string version, string url) GetUpdateInfo(string hwid)
// GET https://lamtool.net/api/license/check?tool_slug=subdyfarm&device_code={hwid}
// parse: license.tool.version, license.tool.updateUrl

// 2. So sánh version (copy logic IsNewerVersion từ LamToolClient.cs)
static bool IsNewerVersion(string current, string remote)
```

`tool_slug = "subdyfarm"` — giống `Globals.NameApp` trong project chính.

---

## AOT constraints

- Không dùng `dynamic`, `reflection` tự do
- JSON parse bằng `System.Text.Json.Nodes.JsonNode` (không cần Source Generator cho use case đơn giản này)
- AntdUI đã tương thích AOT (project chính đã chứng minh)
- Thêm `rd.xml` để preserve WinForms types nếu linker trim

---

## Output

Publish command:
```
dotnet publish -c Release -r win-x64 --self-contained
```

Output: `SubdyAutoUpdate.exe` (~5-15MB AOT, single file)
→ Copy vào cùng thư mục với `SubdyPhoneFarm.exe` khi đóng gói release

---

## Các bước implement (thứ tự)

1. Tạo `SubdyAutoUpdate.csproj` (copy cấu hình AOT từ project chính, bỏ deps không cần)
2. Tạo `Program.cs` — STAThread, init AntdUI, ApplicationConfiguration, open MainForm
3. Tạo `LamToolApiClient.cs` — GetUpdateInfo + IsNewerVersion
4. Tạo `UpdateService.cs` — DownloadFileAsync + CreateUpdateBatAndRestart
5. Tạo `MainForm.cs` + Designer — UI AntdUI, gọi UpdateService
6. Thêm `rd.xml` + `app.manifest`
7. Test publish AOT, verify exe chạy được
8. Copy `SubdyAutoUpdate.exe` vào output folder chính khi release

---

## Điểm khác biệt so với fUpdateAuto.cs trong app chính

| fUpdateAuto.cs (app chính) | SubdyAutoUpdate.exe (tool này) |
|---|---|
| Chạy bên trong app đang chạy | Chạy độc lập khi app chính lỗi |
| Nhận `link` + `version` từ caller | Tự fetch version từ API |
| User phải nhấn đồng ý update | **Tự động hoàn toàn**, không cần tương tác |
| App chính tự đóng trước khi patch | **Kill SubdyPhoneFarm** nếu đang chạy |
| Restart lại app chính sau update | Restart app chính sau update |
| Dùng `HttpRequestLib.dll` native | Dùng `HttpClient` thuần .NET |
| Nhiều dependency (Sunny.*, AutoAndroid) | Zero dependency ngoài AntdUI |
