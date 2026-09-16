# PROJECT HANDOFF

Last Updated:
2026-06-19

---

## Repository Summary

Project Name:
Facebook-Farm-NewFeed-PostStory (QN PhoneFarm)

Solution:
e:\LamToolAutoPhonePrime (multi-project; main csproj = Facebook-Farm-NewFeed-PostStory.csproj)

Framework:
.NET 9 (net9.0-windows), WinForms, Native AOT (publish), AntdUI

Language:
C#

Architecture:
- UI: WinForms + AntdUI. Main forms in Views/Forms, controls in Views/Controls, action dialogs in Views/Forms/Actions (fHD*/fIG*/fTR*).
- Core libs (siblings, OUTSIDE this workspace): AutoAndroid, ScrcpyNet, Sunny.Subd.Core, Sunny.Subdy.Common, Sunny.Subdy.Data, Sunny.Subdy.Server.
- Data layer: Sunny.Subdy.Data — Microsoft.Data.Sqlite micro-ORM (AppDbContext), per-entity Context classes, strongly-typed Models. Multiple DB files: LT_Account.db, LT_Device.db, LT_JobHistory.db, LT_Scipt.db. WAL, keyset pagination, composite indexes, bulk transactions (tuned for 50k+ accounts).

---

## Current Objective

Đồng bộ dữ liệu tài khoản từ các tool ngoài (trọng tâm: MaxPhoneFarm / MaxCare)
vào project hiện tại, GIỮ NGUYÊN core + UI + database hiện đại của project này.

## Workspace Constraint (QUAN TRỌNG)

- Kiro workspace chỉ gồm thư mục `Facebook-Farm-NewFeed-PostStory`.
- Các sibling lib (Sunny.Subdy.Data, AntdUI, AutoAndroid, ScrcpyNet, ...) nằm
  NGOÀI workspace → công cụ edit file của agent bị chặn; chỉ sửa được qua shell.
- Muốn agent sửa trực tiếp các lib này: mở thư mục cha `e:\LamToolAutoPhonePrime`
  làm workspace.

---

## Current Status

Build Status: Success (Sunny.Subdy.Data build riêng: 0 error). Main app chưa
build full (AOT + native HttpRequestLib + sibling solution rất nặng).

Current Branch:
[Auto Detect]

---

## Active Work Item

Mở rộng đồng bộ MaxPhoneFarm để map ĐẦY ĐỦ cột schema MaxCare thay vì 9 cột.

---

## Completed Tasks

### 2026-06-19

- Mở rộng `OtherToolAccount` (Sunny.Subdy.Data/Context/OtherTools/OtherToolNames.cs)
  từ 10 → 25 field: thêm MailRecovery, FullName, Phone, Birthday, Gender, Avatar,
  Bio, Friends, Groups, Follow, Status, Device, DateCreate, Note, RecentInteraction.
- Viết lại `MaxCareReader` (.../OtherTools/MaxCareReader.cs):
  - Đọc cột phòng thủ bằng PRAGMA table_info(accounts) → chỉ SELECT cột thực có
    (tương thích nhiều phiên bản db_maxcare.sqlite).
  - Map đầy đủ accounts: uid, pass, fa2, cookie/cookie1, token, proxy, email,
    passmail, mailrecovery, useragent, name, phone, birthday, gender, avatar,
    info(Bio), friends, groups, follow, status, device, dateCreateAcc/dateImport,
    ghiChu(Note), interactEnd(RecentInteraction).
  - Giữ NormalizeProxy (bỏ "*0") và NormalizeCookie (UrlDecode).
- Mở rộng `fSyncOtherTool.ApplyTo` (Views/Forms/fSyncOtherTool.cs) để map toàn bộ
  field mới vào model `Account` (chỉ ghi đè khi giá trị nguồn khác rỗng).

### 2026-06-19 (đợt 2 — "tiếp tục")

- Thêm field `MailRecovery` vào model `Account` (Sunny.Subdy.Data/Models/Account.cs):
  property + backing field. AppDbContext (reflection) tự tạo cột khi EnsureTable.
- `AccountContext.MapToAccount`: đọc thêm `MailRecovery` từ `SELECT *` để tránh
  mất dữ liệu (UpdateEntity ghi đè toàn bộ prop → nếu không hydrate sẽ ghi rỗng).
- Viết lại `MetaMaxReader` (Context/OtherTools/MetaMaxReader.cs) theo kiểu phòng
  thủ PRAGMA giống MaxCareReader: SELECT cột thực có + map đầy đủ
  (UID, Password, TwoFA, Cookie, Token, Proxy, Email, PassMail, MailRecovery,
  UserAgent, FullName/Name, Phone, Birthday, Gender, Avatar, Bio/Info, Friends,
  Groups, Follow, Status, Device, DateCreate, Note, RecentInteraction).
- `fSyncOtherTool.ApplyTo`: map thêm `MailRecovery` vào Account.
- FPlusReader GIỮ NGUYÊN: file text pipe-delimited, không có spec vị trí cho các
  cột mở rộng → đoán vị trí sẽ sai dữ liệu, nên không mở rộng.

## Verification

- `dotnet build Sunny.Subdy.Data.csproj`: **0 Error, 0 Warning** (sau đợt 2).
- `fSyncOtherTool.cs`: no diagnostics.
- Full main build: **PASS (0 Error)** — đã build lại thành công sau khi ổ E: ổn định.

---

### 2026-06-19 (đợt 3 — UI lưới + load DB theo MaxPG)

- Thêm `ApplyMaxPgColumnLayout()` vào `Views/Controls/ucdgvAccount.cs`: sắp xếp
  lại thứ tự + đặt nhãn cột lưới tài khoản GIỐNG MaxPhoneFarm (Chọn, STT, UID,
  Token, Cookie, Email, Phone, Tên, Theo dõi, Bạn bè, Nhóm, Ngày sinh, Giới Tính,
  Mật khẩu, Email khôi phục, Mật khẩu mail, Mã 2FA, Useragent, Proxy, Ngày tạo,
  Avatar, Profile, Thư mục, Lần tương tác cuối, Device Info, Tình trạng, Ghi chú,
  Thiết bị, Trạng thái). Chỉ đổi DisplayIndex + HeaderText → KHÔNG đụng VirtualMode,
  windowed cache hay logic hiệu năng (tham chiếu cột theo Name/Index).
- Gọi `ApplyMaxPgColumnLayout()` cuối constructor + trong `Load` handler (sau
  LoadConfigColums) để layout MaxPG luôn được áp dụng.
- Cập nhật danh sách `hideHeaders` (ẩn mặc định lần đầu) với nhãn MaxPG mới để
  giữ nguyên hành vi ẩn cột nặng mặc định.
- Cột MaxPG không map được (bỏ qua an toàn): IP, Backup (không có trong
  ListSelectColumns/slim query nên không thêm để tránh cột rỗng ở chế độ windowed).
- "Cách call DB load": ĐÃ khớp MaxPG sẵn — chọn nhóm ở dropdown `select1`
  ("[ Tất cả các nhóm ]" / "[ Tài khoản đã xóa ]" / "[ Chọn nhiều nhóm ]" /
  "[ Chọn theo uid ]") → `LoadAccounts()` nạp account theo nhóm từ DB vào lưới.
  Tương đương MaxPG (cbbThuMuc → GetAccountsWithFile → PopulateGrid), engine nhanh hơn.

## Verification (đợt 3)

- `ucdgvAccount.cs`: no diagnostics.
- `dotnet build Facebook-Farm-NewFeed-PostStory.csproj` (full app) → Build succeeded, 0 Error.

### 2026-06-19 (đợt 4 — cơ chế cập nhật status theo MaxPG)

Cơ chế status giờ khớp MaxPhoneFarm theo 3 giai đoạn:
- LOAD (có sẵn): `ListSelectColumns` chứa Status; `MapToAccount` đọc Status/State/RecentInteraction khi nạp lưới.
- LIVE UPDATE (có sẵn): core `FacebookRegsiner.UpdateStatus` set `_account.Status/State/RecentInteraction/ColorType` → ThrottledPropertyNotifier → lưới cập nhật trực tiếp (không ghi DB mỗi lần). Tương đương MaxPG `SetStatusEntry` + `SetCellValueByName`.
- DEFERRED FLUSH (MỚI): `AccountContext.UpdateStatusBatch(IReadOnlyList<Account>)` — chỉ UPDATE 3 cột Status/State/RecentInteraction theo Id, prepared statement trong 1 transaction (nhẹ, giống MaxPG `FlushStatusToDb`/BulkInsert "status").
  - `ucdgvAccount`: `_statusFlushTimer` (7s) + flush khi bấm Dừng (button8_Click) + flush sync khi Dispose. Chỉ flush account đang chạy/có status; bỏ qua chế độ windowed (core checkpoint tự persist).

## Verification (đợt 4)

- `dotnet build Sunny.Subdy.Data.csproj` → 0 Error.
- `dotnet build Facebook-Farm-NewFeed-PostStory.csproj` (full) → Build succeeded, 0 Error.
- `ucdgvAccount.cs` / `.Designer.cs` → no diagnostics.

## Technical Constraints

Do Not:
- Break APIs / remove existing functionality.
- Hạ cấp DB/UI hiện tại về kiểu MaxPhoneFarm cũ (System.Data.SQLite + SQL nối
  chuỗi + DataTable) — đó là bước lùi về hiệu năng & an toàn.

Must:
- Giữ core logic + Microsoft.Data.Sqlite + tối ưu grid hiện có.
- Query luôn tham số hóa.

---

## Important Modules

- UI: Views/Forms, Views/Controls (ucdgvAccount = account grid 50k+).
- Data: Sunny.Subdy.Data (AppDbContext, AccountContext, Context/OtherTools/*).
- Other-tool sync: fSyncOtherTool + MaxCareReader / FPlusReader / MetaMaxReader.
- Device/automation: AutoAndroid, ScrcpyNet.

---

## Known Issues

- FPlusReader / MetaMaxReader vẫn chỉ map tập cột cơ bản (chưa mở rộng như MaxCare).
- `fSyncOtherTool.GetMaxCareFolders` có chuỗi lỗi thiếu nội suy: "Lỗi đọc dữ liệu {_tool}".
- Account model không có field MailRecovery → cột mailrecovery của MaxCare đọc được
  nhưng chưa có chỗ lưu (hiện bỏ qua khi ApplyTo).

---

## Pending Tasks

1. (Tùy chọn) Mở rộng FPlusReader / MetaMaxReader map đầy đủ như MaxCare.
2. (Tùy chọn) Thêm field MailRecovery vào Account nếu muốn lưu email khôi phục.
3. Build full main app khi có sẵn toolchain C++ (HttpRequestLib) + AOT.

---

## Notes For Next Agent

Before coding:
1. Read this file.
2. Workspace chỉ là Facebook-Farm-NewFeed-PostStory; sibling lib sửa qua shell
   hoặc yêu cầu user mở workspace cha.
3. Giữ kiến trúc hiện tại, KHÔNG hạ cấp về MaxPhoneFarm cũ.

---

### 2026-06-20 (đợt 5 — AUTO-FIX MODE: UI freeze root cause)

**Findings (evidence từ logs/account_grid_perf.log + grid_metrics.log):**
- Grid hot paths đều khỏe: CellValueNeeded 0.002ms, CellFormatting ~0ms,
  CellPainting 0.3ms, Paint 10–47ms. Virtualization cũ hoạt động tốt.
- ANOMALY: `probeDispatchAvg ≈ 1100–1900ms`, `probeDispatchMax tới 4875ms`
  trong KHI `UIBusy ≈ 0%` ở mọi cửa sổ 10s — kể cả lúc idle hoàn toàn.
  Avg ≈ ½ max ⇒ message pump bị chặn ~2.4s, lặp lại mỗi ~2s. Chữ ký của
  "blocking wait trên UI thread" (không phải CPU-bound).
- GC nặng bất thường khi app idle: Gen0=139 Gen1=138 Gen2=45, LOH→57MB,
  Fragmented 28MB, dù grid không hoạt động.

**Root cause (PROVEN by code inspection):**
`Views/Controls/ucSystemStatusBar.cs` — timer 2s gọi `Invalidate()` → `OnPaint`.
Trong OnPaint, 2 getter `CountErrorAccounts()` + `CountQueueAccounts()` MỖI cái
gọi `new AccountContext().GetAll(..., platform, true)` cho 3 platform =
**6 lần quét/materialize toàn bộ bảng account (30k dòng) trên UI thread mỗi 2s**.
→ giải thích đủ cả 3 triệu chứng: pump stall ~2.4s, UIBusy 0% (không nằm trong
UiThreadProfiler scope của grid), và GC churn khi idle. Control này được mount
live qua `SsaTheme.MountStatusBar`.

**Fix Applied (ucSystemStatusBar.cs):**
- Đưa toàn bộ DB count ra khỏi UI thread: thêm `System.Threading.Timer`
  (`_metricsTimer`, 5s) chạy `RefreshAccountMetrics()` trên threadpool, cache kết
  quả vào `_cachedErrors`/`_cachedQueue` (volatile), chỉ `BeginInvoke(Invalidate)`
  khi giá trị đổi.
- `OnPaint` giờ chỉ đọc int cache → 0 DB call, 0 cấp phát Account trên UI thread.
- Gộp 6 lần GetAll → 3 (một pass/platform đếm cả error + queue). Giảm 50%
  materialization.
- Gate theo visibility (`_isVisible`, cập nhật ở UI qua VisibleChanged) +
  Interlocked guard chống quét chồng. Timer UI 2s vẫn repaint (giờ rất nhẹ).

**Benchmark:**
- BEFORE: OnPaint mỗi 2s = 6× GetAll(30k) trên UI thread; probeDispatchAvg
  ~1200ms, max 4875ms; UIBusy 0% nhưng pump chết ~2.4s; GC Gen0=139/LOH 57MB.
- AFTER (theo kiến trúc): UI thread 0 DB call trong paint; quét chuyển sang
  background 5s, 3 GetAll thay vì 6. Dispatch latency dự kiến về mức bình thường
  (chục ms). Verify số thực cần chạy lại app với ACCOUNT_GRID_DIAG=1 + chạy
  tools/analyze-grid-log.ps1 (peak ProbeDispatch phải tụt mạnh).

**Verification:**
- `dotnet build Facebook-Farm-NewFeed-PostStory.csproj -c Debug` → Build succeeded, 0 Error.
- `ucSystemStatusBar.cs` → no diagnostics.
- Semantics giữ nguyên (cùng định nghĩa error: status chứa err/fail/ban; queue: Running).

**Remaining bottlenecks (đã đo, ưu tiên thấp hơn — đều one-shot do user thao tác):**
1. `GetCheckAllIncludedIds` = 77ms trên UI thread (khi chọn tất cả). High prio.
2. `LoadScope.UIApply` = 125ms / `UIWork` = 64ms (khi đổi nhóm). High prio.
3. `Handler.ShowContextMenu(build+show)` = 40ms (chuột phải). Medium.
4. `Grid.OnPaint(full)` thỉnh thoảng 47ms avg/window. Medium.

**Next recommended action:** chạy app với DIAG=1 để lấy số AFTER, xác nhận
ProbeDispatch peak giảm; sau đó cân nhắc đẩy GetCheckAllIncludedIds + bind
(LoadScope.UIApply) sang background nếu vẫn thấy giật khi đổi nhóm / chọn tất cả.
