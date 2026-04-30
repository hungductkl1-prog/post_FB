# PLAN: Map form hành động + logic thực thi kịch bản

Nguồn tham khảo: `E:\MaxPhoneFarm_v23.06.20` (185 file `fHD*.cs` + runner trong `fMain.cs`)
Đích: `e:\LamToolAutoPhonePrime` (chỉ lập plan, chưa code)

---

## 1. Tổng quan kiến trúc hiện tại

### MaxPhoneFarm (tham khảo, .NET Framework 4.8 + Bunifu UI)
- Mỗi hành động = 1 form `fHD<Tên>.cs` (UI cấu hình) + 1 method `HD<Tên>(...)` trong [fMain.cs](file://E:/MaxPhoneFarm_v23.06.20/fMain.cs) (logic thực thi).
- Runner: `fMain.cs` lines ~2400-2900 — vòng lặp `switch (HashString.ScrollPage(text12))` map từng `TenTuongTac` → gọi method `HD<Tên>(int_1, text, GetElementAttribute, f72FAFBC2, text10)`.
- Tham số chuẩn: `(int deviceIndex, string statusPrefix, DeviceWorker worker, JsonHelper config, string actionName)` → trả về `int` (0 fail / 1 done / -1 die / -2 wrongpass / -3 banned).
- Cấu hình mỗi action lưu dạng JSON, đọc qua `JsonHelper.GetIntType`/`GetBooleanValue`/`GetValue`.

### LamToolAutoPhonePrime (đích, .NET 6+ + AntdUI)
- Form action: [LamToolAutoPhonePrime/Views/Forms/Actions/](file://e:/LamToolAutoPhonePrime/LamToolAutoPhonePrime/Views/Forms/Actions/) — đã có 45 form.
- Runner: [Sunny.Subd.Core/Facebook/FacebookFarming.cs](file://e:/LamToolAutoPhonePrime/Sunny.Subd.Core/Facebook/FacebookFarming.cs) — switch theo [FacebookFarmingType](file://e:/LamToolAutoPhonePrime/Sunny.Subdy.Common/Models/FacebookFarmingType.cs).
- Handlers riêng lẻ: [Sunny.Subd.Core/Facebook/ScriptActions/](file://e:/LamToolAutoPhonePrime/Sunny.Subd.Core/Facebook/ScriptActions/) — `IActionHandler`, `ActionExecutor`, vài handler (Avatar, Cover, Mail, 2FA).
- Lưu kịch bản trong SQLite: bảng `Script` + `ScriptAction` (Json), gán account qua `Account.NameScript` (hiện `[NotMapped]`).

---

## 2. Bảng map form hành động

### 2A. Form đã có ở cả 2 project (chỉ cần kiểm tra/đồng bộ field cấu hình)

| FacebookFarmingType | Form đích (LamTool) | Form nguồn (MaxPhoneFarm) | Runner đã code? |
|---|---|---|---|
| HDDocThongBao | fHDDocThongBao.cs | fHDDocThongBao.cs | ✅ |
| HDXemReel | fHDXemReel.cs | fHDXemReel.cs | ✅ |
| HDXemStory | fHDXemStory.cs | fHDXemStory.cs | ❌ stub |
| HDXemWatch | fHDXemWatch.cs | fHDXemWatch.cs | ✅ |
| HDTuongTacNewfeed | fHDTuongTacNewfeed.cs | fHDTuongTacNewsfeed.cs | ✅ |
| HDTuongTacBanBe | fHDTuongTacBanBe.cs | fHDTuongTacBanBe.cs | ✅ |
| HDTuongTacNhom | fHDTuongTacNhom.cs | fHDTuongTacNhom.cs / V2 | ✅ |
| HDTuongTacPage | fHDTuongTacPage.cs | fHDTuongTacPage.cs | ✅ |
| HDTuongTacWall | fHDTuongTacWall.cs | fHDTuongTacWall.cs | ✅ |
| HDTuongTacBaiViet | fHDTuongTacBaiViet.cs | fHDTuongTacBaiVietChiDinh / TuKhoa / IA | ✅ |
| HDTuongTacVideoLivestream | fHDTuongTacLivestream.cs | fHDTuongTacLivestream / Video | ✅ |
| HDGuiLoiMoiKetBan | fHDGuiLoiMoiKetBan.cs | fHDKetBanGoiY / TepUid / TheoTuKhoa / VoiBanCuaBanBe | ✅ |
| HDXacNhanKetBan | fHDXacNhanKetBan.cs | fHDXacNhanKetBan.cs | ✅ |
| HDHuyKetBan | fHDHuyKetBan.cs | fHDHuyKetBan.cs / fHDHuyLoiMoiKetBan | ✅ |
| HDThamGiaNhom | fHDThamGiaNhom.cs | fHDThamGiaNhomGoiY / TuKhoa / Uid | ✅ |
| HDRoiNhom | fHDRoiNhom.cs | fHDRoiNhom.cs | ❌ stub |
| HDTaoNhom | fHDTaoNhom.cs | fHDTaoNhom.cs | ❌ stub |
| HDTaoPage | fHDTaoPage.cs | fHDTaoPage.cs | ❌ stub |
| HDDangBaiTuong | fHDDangBaiTuong.cs | fHDDangBaiTuong / BaiVietBanBe | ✅ |
| HDDangBaiNhom | fHDDangBaiNhom.cs | fHDDangBaiNhom / BaiVietNhom / SpamNhom | ✅ |
| HDDangBaiPage | fHDDangBaiPage.cs | fHDDangBaiPage / BaiVietFanpage | ❌ stub |
| HDShareBaiNangCao | fHDShareBaiNangCao.cs | fHDShareBaiNangCao / ChiaSeLivestream | ✅ |
| HDDangReel | fHDDangReel.cs | fHDDangReel.cs | ✅ |
| HDDangStory | fHDDangStory.cs | fHDDangStory.cs | ❌ stub |
| HDDanhGiaPage | fHDDanhGiaPage.cs | fHDDanhGiaPage.cs | ❌ stub |
| HDBuffLikePage | fHDBuffLikePage.cs | fHDBuffLikePage / BuffFollowLikePage | ❌ stub |
| HDBuffFollowUID | fHDBuffFollowUID.cs | fHDBuffFollowUID.cs | ❌ stub |
| HDMoiBanBeLikePage | fHDMoiBanBeLikePage.cs | fHDMoiBanBeLikePage.cs | ❌ stub |
| HDMoiBanBeVaoNhom | fHDMoiBanBeVaoNhom.cs | fHDMoiBanBeVaoNhom.cs | ❌ stub |
| HDDoiTen | fHDDoiTen.cs | fHDDoiTen.cs | ❌ stub |
| HDDoiMatKhau | fHDDoiMatKhau.cs | fHDDoiMatKhau.cs | ❌ stub |
| HDUpAvatar | fHDUpAvatar.cs | fHDUpAvatar.cs | ❌ stub (handler riêng đã có: FbChangeAvatarHandler) |
| HDUpCover | fHDUpCover.cs | fHDUpCover.cs | ❌ stub (handler riêng đã có: FbChangeCoverHandler) |
| HDOnOff2FA | fHDOnOff2FA.cs | fHDOnOff2FA / fHDKhangSpam | ❌ stub (handler riêng đã có: FbTurn2FAHandler) |
| HDXoaSdt | fHDXoaSdt.cs | fHDXoaSdt.cs | ❌ stub |
| HDAddMail | fHDAddMail.cs | fHDAddMail.cs | ❌ stub (handler riêng đã có: FbChangeMail) |
| HDCapNhatThongTin | fHDCapNhatThongTin.cs | fHDCapNhatThongTin / CauHinhTaiKhoan | ❌ stub |
| HDDangXuatThietBiCu | fHDDangXuatThietBiCu.cs | fHDDangXuatThietBiCu.cs | ❌ stub |
| HDXoaThietBiTinCay | fHDXoaThietBiTinCay.cs | fHDXoaThietBiTinCay.cs | ❌ stub |
| HDBatCheDoChuyenNghiep | fHDBatCheDoChuyenNghiep.cs | fHDBatCheDoChuyenNghiep.cs | ❌ stub |
| HDNghiGiaiLao | fHDNghiGiaiLao.cs | fHDNghiGiaiLao.cs | ❌ stub |
| HDDongBoDanhBa | (chưa có) | fHDDongBoDanhBa.cs | ❌ stub |
| HDTimKiemGoogle | fHDTimKiemGoogle.cs | fHDTimKiemGoogle / TruyCapWebsite | ❌ stub |
| HDNhanTinBanBe | fHDNhanTinBanBe.cs | fHDNhanTinBanBe / NhanTinPage / PhanHoiTinNhan | ❌ stub |
| HDTuongTacEvent | fHDTuongTacEvent.cs | fHDSeedingEvents.cs (gần nhất) | ✅ |

### 2B. Form có ở MaxPhoneFarm nhưng CHƯA có ở LamTool — nên import (cần xác nhận có dùng không trước khi port)

| Form nguồn | Mục đích | Đề xuất FacebookFarmingType mới |
|---|---|---|
| fHDChocBanBe.cs | Chọc bạn bè (poke) | HDChocBanBe |
| fHDChucMungSinhNhat.cs | Chúc mừng sinh nhật bạn bè | HDChucMungSinhNhat |
| fHDChaySeeding.cs | Chạy seeding theo job | HDChaySeeding |
| fHDSeedingByVideo.cs | Seeding theo video | HDSeedingByVideo |
| fHDSeedingEvents.cs | Seeding theo event (đã ánh xạ HDTuongTacEvent) | — |
| fHDSpamBaiViet / SpamBanBe / SpamNewfeed / SpamNhom | Spam | HDSpamBaiViet … (4 type) |
| fHDReport / fHDReportVideo | Report | HDReport, HDReportVideo |
| fHDBackupData.cs | Backup data | HDBackupData |
| fHDXoaReel.cs | Xóa reel | HDXoaReel |
| fHDVerifyAccount.cs | Verify account | HDVerifyAccount |
| fHDKhangSpam.cs | Kháng spam | HDKhangSpam |
| fHDXemWatchTheoTuKhoa.cs | Xem watch theo từ khóa | HDXemWatchTheoTuKhoa hoặc gộp vào HDXemWatch (radio "theo từ khóa") |
| fHDTuongTacReelChiDinh / TuKhoa | Tương tác reel cụ thể | gộp vào HDXemReel hoặc thêm 2 type |
| fHDTuongTacBaiVietChiDinh / TuKhoa / IA | Đã gộp vào HDTuongTacBaiViet | — |
| fHDKetBanGoiY / TepUid / TepUidNew / TheoTuKhoa / VoiBanCuaBanBe / VoiBanBeCuaUid / ThanhVienNhom / KetBanNewfeed | Đã gộp vào HDGuiLoiMoiKetBan | — |
| fHDThamGiaNhomGoiY / TuKhoa / Uid | Đã gộp vào HDThamGiaNhom | — |
| fHDPhanHoiTinNhan / NhanTinPage | Có thể gộp vào HDNhanTinBanBe | — |
| fHDBuffTinNhanProfile / BuffLikeComment / BuffFollowLikePage | Buff dịch vụ | HDBuff* (3 type) |

**Quy ước port form**: với form mục “gộp”, bổ sung radio/select trong form đích để chọn sub-mode (chỉ định / từ khóa / gợi ý) thay vì tạo nhiều form riêng — nhất quán với cách LamTool đang làm.

---

## 3. Form QuanLyKichBan & ChonKichBan (đối chiếu yêu cầu giao diện)

| Khái niệm | MaxPhoneFarm | LamTool đích | Khoảng trống |
|---|---|---|---|
| List kịch bản + add/sửa/xóa | fDanhSachKichBan_Old + fThemKichBan | fQuanLyKichBan + fFolder("AddScript") | OK (đã có) |
| List hành động trong kịch bản | fThemHanhDong | fChiTietKichBan + fActions | OK (đã có) |
| Form gán kịch bản cho account | fChonKichBan | Context menu trong ucdgvAccount | OK (chỉ thiếu form hiển thị riêng — không bắt buộc) |
| Form cấu hình "tương tác chung" (giới hạn thời gian, random thứ tự, lặp lại…) | fCauHinhTuongTac | fQuanLyKichBan (đã có panel checkbox/radio + ConfigHelper) | OK |

---

## 4. Logic thực thi kịch bản — gap hiện tại

### Hiện trạng [FacebookFarming.cs:190-301](file://e:/LamToolAutoPhonePrime/Sunny.Subd.Core/Facebook/FacebookFarming.cs#L190)
- 17 case **đã có method** (HDDocThongBao, HDXemReel, HDXemWatch, HDTuongTacNewfeed/BanBe/Nhom/Page/Wall/Event/BaiViet, HDTuongTacLivestream, HDGuiLoiMoiKetBan, HDXacNhanKetBan, HDHuyKetBan, HDThamGiaNhom, HDDangBaiTuong, HDDangBaiNhom, HDShareBaiNangCao, HDDangReel).
- 28 case **stub `break;`** — không thực thi gì khi gặp action loại đó.

### Cần làm theo từng nhóm ưu tiên

**P0 — đã có UI form, chưa có runner (port từ MaxPhoneFarm fMain.cs):**
1. HDXemStory ← `HDXemStory` line 13965
2. HDRoiNhom ← `HDRoiNhom`
3. HDTaoNhom ← `HDTaoNhom` (line ~tìm theo case 2419354813u)
4. HDTaoPage
5. HDDangBaiPage
6. HDDangStory
7. HDDanhGiaPage
8. HDBuffLikePage / HDBuffFollowUID
9. HDMoiBanBeLikePage / HDMoiBanBeVaoNhom
10. HDDoiTen / HDDoiMatKhau
11. HDOnOff2FA / HDXoaSdt / HDAddMail
12. HDCapNhatThongTin
13. HDDangXuatThietBiCu / HDXoaThietBiTinCay
14. HDBatCheDoChuyenNghiep
15. HDNghiGiaiLao (chỉ là `Task.Delay(rand)` — đơn giản)
16. HDTimKiemGoogle / HDNhanTinBanBe / HDDongBoDanhBa
17. HDUpAvatar / HDUpCover (đã có handler riêng — chỉ cần wrap vào FacebookFarming switch)

**P1 — cần thêm FacebookFarmingType + UI form + runner:**
- HDChocBanBe, HDChucMungSinhNhat, HDChaySeeding, HDSeedingByVideo
- HDSpam* (4 type) — cân nhắc có nên đưa vào tool farm hay không (rủi ro account)
- HDReport / HDReportVideo / HDXoaReel
- HDBackupData / HDVerifyAccount / HDKhangSpam
- HDBuffTinNhanProfile / HDBuffLikeComment

---

## 5. Sự khác biệt kỹ thuật giữa runner cũ và mới

| Khía cạnh | MaxPhoneFarm | LamTool |
|---|---|---|
| Thiết bị | `DeviceWorker` (custom, monolithic) | `ADBClient` + `FacebookService` |
| Cấu hình action | `JsonHelper` (plain) | `JsonHelper` (đã có wrapper trong Common) |
| Trả về | `int` (1=done, 0=fail, -1=die, -2=wrongpass, -3=banned) | `Task` + ghi `JobHistory` qua `JobHistoryContext` |
| Login | `Login(worker, idx, status)` chung | `FacebookService.LoginAsync(...)` |
| Status display | `SetStatusAccount(idx, msg)` | `SetStatusAccount(account, msg)` qua callback |
| Cancel | check `bool_0` global | `CancellationToken` |
| Retry / proxy / IP check | trong cùng method | đã tách `CheckLiveService`, `MainService.CheckProxyAndIp` |

**Quy ước khi port method HD<Tên>**:
- Đổi chữ ký: `private async Task<ActionResult> HD<Tên>(JsonHelper cfg, ScriptAction action)` (theo pattern các method đã code).
- Thay `worker.TapBoundsByXPath` → API tương ứng của `ADBClient` / `FacebookService`.
- Thay `SetStatusAccount(idx, ...)` → `_statusCallback(account, ...)`.
- Bỏ logic proxy/login lặp lại — đã có ở `MainService` cấp ngoài.
- Map mã trả về cũ → enum mới (`Done`, `Skip`, `Die`, `Checkpoint`, `Banned`).

---

## 6. Roadmap thực hiện (sau khi user chốt plan)

**Phase 0 — chuẩn bị (1 PR)**
- Chốt danh sách `FacebookFarmingType` mới cần thêm (P1) → bổ sung vào [FacebookFarmingType.cs](file://e:/LamToolAutoPhonePrime/Sunny.Subdy.Common/Models/FacebookFarmingType.cs) + 2 dictionary `DictionariesAction`/`DescriptionAction`.
- Bổ sung mapping form mới vào `fActions` (categorize) và `fChiTietKichBan.OpenPage` switch.

**Phase 1 — port action UI form còn thiếu (1 form / commit)**
- Tạo `fHD<Tên>.cs` trong `LamToolAutoPhonePrime/Views/Forms/Actions/` theo template form đã có (vd `fHDDocThongBao`).
- Map field từ MaxPhoneFarm form (đọc `f<Tên>.cs` → port các `nud*`, `ckb*`, `txt*` thành AntdUI tương đương) → ghi/đọc vào `ScriptAction.Json` qua `JsonHelper`.

**Phase 2 — port runner method (theo block 4-5 method / commit)**
- Mỗi method copy từ `fMain.cs` → đặt trong `FacebookFarming.cs` (hoặc tách thành file riêng `FacebookFarming.HD<Nhóm>.cs` partial class nếu file quá to).
- Thay API thiết bị + status callback theo bảng 5.
- Thêm `case` tương ứng vào switch chính (gỡ `break;` rỗng).

**Phase 3 — handler riêng đã có (Avatar/Cover/Mail/2FA)**
- Wrap vào switch case của FacebookFarming → gọi qua `IActionHandler` đã có.

**Phase 4 — testing**
- Chạy thử kịch bản FarmXu (4 action mặc định) trên 1 device → đảm bảo không regression.
- Chạy thử 1 kịch bản custom có 5-10 action mới port để verify.

---

## 7. Quyết định đã chốt với user (2026-04-20)

| # | Câu hỏi | Quyết định |
|---|---|---|
| 1 | Port các form 2B (Spam/Report/Verify/Backup/KhangSpam/Buff/ChocBanBe/ChucMungSinhNhat/Seeding) | **Có** — tích hợp đầy đủ |
| 2 | `Account.NameScript` persist xuống DB | **Có** — bỏ `[NotMapped]`, thêm cột `NameScript` vào schema + `MapToAccount` |
| 3 | Reel/Watch/KetBan/ThamGiaNhom có sub-mode (ChiDinh/TuKhoa/IA/GoiY) | **Giống MaxPhoneFarm** — tách thành type riêng (HDTuongTacReelChiDinh, HDTuongTacReelTuKhoa, …), KHÔNG gộp |
| 4 | API thiết bị | **Dùng `DeviceWorker`** (port nguyên xi từ MaxPhoneFarm vào AutoAndroid) — đỡ phải rewrite từng XPath/tap |
| 5 | Phase ưu tiên | **Cả 2 song song** — UI form + runner method theo từng nhóm hoàn chỉnh |

### 7B. Yêu cầu bổ sung quan trọng (account → kịch bản routing)

> **"User set account nào chạy kịch bản name nào thì account đó theo kịch bản chứa logic đó"**

Hiện trạng [FacebookFarming.cs:95](file://e:/LamToolAutoPhonePrime/Sunny.Subd.Core/Facebook/FacebookFarming.cs#L95):
```csharp
_script = _scriptContext.GetByName(_config.JobService, _mainService._platform);  // ← HARDCODED, dùng config chung
```
Và call site đang **bị comment** ([MainService.cs:776-783](file://e:/LamToolAutoPhonePrime/Sunny.Subd.Core/Services/MainService.cs#L776)):
```csharp
//_farmxu = new SpamXuHandler(...);   // route 1
//FacebookFarming farming = new FacebookFarming(this);   // route 2
//await farming.ExecuteAsync();
```

**Phải sửa thành**:
1. Bỏ comment block ở `MainService.RunAsync`.
2. Trong `FacebookFarming.ExecuteAsync()`, đổi:
   ```csharp
   string scriptName = !string.IsNullOrEmpty(_account.NameScript)
                       ? _account.NameScript
                       : "FarmXu";   // fallback default
   _script = _scriptContext.GetByName(scriptName, _mainService._platform);
   if (_script == null)   // user xóa kịch bản giữa chừng → fallback FarmXu
       _script = _scriptContext.GetByName("FarmXu", _mainService._platform);
   ```
3. Vì `Account.NameScript` sẽ persist (decision 2), giá trị này tồn tại sau reload và đi cùng account vào runner.

**Hiệu ứng**: Mỗi account chạy đúng kịch bản đã gán trong UI. Nếu user không gán → chạy FarmXu mặc định (đã đảm bảo bởi 2 thay đổi trước: default khi add + remap khi xóa script).

---

## 8. Roadmap chi tiết sau khi chốt (override mục 6)

### Phase A — DB + Routing (foundation, làm trước)
- A1. Bỏ `[NotMapped]` khỏi `Account.NameScript` ([Account.cs:78-79](file://e:/LamToolAutoPhonePrime/Sunny.Subdy.Data/Models/Account.cs#L78)) + thêm `NameScript = reader["NameScript"]?.ToString()` vào `MapToAccount` ([AccountContext.cs:30](file://e:/LamToolAutoPhonePrime/Sunny.Subdy.Data/Context/AccountContext.cs#L30)).
- A2. `EnsureTable<Account>` chạy lúc startup phải tự `ALTER TABLE Account ADD COLUMN NameScript TEXT DEFAULT ''` (kiểm tra `AppDbContext.EnsureTable` xem có handle migration không; nếu không, viết migration thủ công).
- A3. Bỏ comment block ở [MainService.cs:776-783](file://e:/LamToolAutoPhonePrime/Sunny.Subd.Core/Services/MainService.cs#L776) — gọi `FacebookFarming.ExecuteAsync()`.
- A4. Sửa [FacebookFarming.cs:95](file://e:/LamToolAutoPhonePrime/Sunny.Subd.Core/Facebook/FacebookFarming.cs#L95) đọc `_account.NameScript` (xem 7B).

### Phase B — Port DeviceWorker từ MaxPhoneFarm
- B1. Copy `DeviceWorker.cs` (hoặc tên tương đương trong MaxPhoneFarm — cần grep xác nhận class này nằm ở file nào: `Class*.cs` / `GClass*.cs`) sang `AutoAndroid/Services/DeviceWorker.cs`.
- B2. Đổi namespace + xử lý reference (FacebookHelper, EA98BF20, Common, Language…).
- B3. Adapter: `DeviceWorker` cần wrap quanh `ADBClient` hiện có (hoặc dùng raw ADB call như MaxPhoneFarm).
- B4. Port `JsonHelper`, `SettingsTool`, `Language`, `Base.rd` → đảm bảo các API helper được dùng trong HD method tồn tại bên LamTool.

### Phase C — Mở rộng FacebookFarmingType (decision 1 + 3)
- C1. Bổ sung vào [FacebookFarmingType.cs](file://e:/LamToolAutoPhonePrime/Sunny.Subdy.Common/Models/FacebookFarmingType.cs):
  - **Sub-mode tách (decision 3)**: `HDXemWatchTheoTuKhoa`, `HDTuongTacReelChiDinh`, `HDTuongTacReelTuKhoa`, `HDTuongTacBaiVietChiDinh`, `HDTuongTacBaiVietTuKhoa`, `HDTuongTacBaiVietIA`, `HDKetBanGoiY`, `HDKetBanTepUid`, `HDKetBanTepUidNew`, `HDKetBanTheoTuKhoa`, `HDKetBanVoiBanCuaBanBe`, `HDKetBanVoiBanBeCuaUid`, `HDKetBanThanhVienNhom`, `HDKetBanNewfeed`, `HDThamGiaNhomGoiY`, `HDThamGiaNhomTuKhoa`, `HDThamGiaNhomUid`, `HDHuyLoiMoiKetBan`, `HDBaiVietBanBe`, `HDBaiVietFanpage`, `HDBaiVietNewsfeed`, `HDBaiVietNewsfeedv2`, `HDBaiVietNhom`, `HDChiaSeLivestream`, `HDTuongTacVideo`, `HDNhanTinPage`, `HDPhanHoiTinNhan`, `HDTruyCapWebsite`, `HDXemWatchOld`.
  - **Form 2B (decision 1)**: `HDChocBanBe`, `HDChucMungSinhNhat`, `HDChaySeeding`, `HDSeedingByVideo`, `HDSeedingEvents`, `HDSpamBaiViet`, `HDSpamBanBe`, `HDSpamNewfeed`, `HDSpamNhom`, `HDReport`, `HDReportVideo`, `HDXoaReel`, `HDBackupData`, `HDVerifyAccount`, `HDKhangSpam`, `HDBuffTinNhanProfile`, `HDBuffLikeComment`, `HDBuffFollowLikePage`, `HDCauHinhTaiKhoan`.
- C2. Thêm cặp key/value tương ứng vào `DictionariesAction` + `DescriptionAction`.

### Phase D — Port form UI (~60 form còn thiếu/khác biệt)
Theo template form đã có (vd [fHDDocThongBao.cs](file://e:/LamToolAutoPhonePrime/LamToolAutoPhonePrime/Views/Forms/Actions/fHDDocThongBao.cs)):
- D1. Chia thành 8-10 batch (~6 form/PR), mỗi form 1 commit.
- D2. Mỗi form: copy controls/field từ MaxPhoneFarm `f<Tên>.Designer.cs` → port sang AntdUI controls; binding `JsonHelper` đọc/ghi `ScriptAction.Json`.
- D3. Bổ sung vào `fActions.cs` (UI categorize) + `fChiTietKichBan.OpenPage` switch (line ~398-535).

### Phase E — Port runner method (HD<Tên>) — số lượng lớn nhất
Lấy từ [fMain.cs](file://E:/MaxPhoneFarm_v23.06.20/fMain.cs) (28 method stub + ~30 method mới từ Phase C):
- E1. Tạo file partial `FacebookFarming.HDInteract.cs`, `FacebookFarming.HDFriend.cs`, `FacebookFarming.HDPost.cs`, `FacebookFarming.HDInfo.cs`, `FacebookFarming.HDOther.cs` để file không quá to.
- E2. Mỗi method: copy nguyên xi từ `fMain.cs` → đổi chữ ký:
  - Cũ: `private int HD<Tên>(int F2258791, string string_0, DeviceWorker ac28BD29_0, JsonHelper DF9FA792, string string_1)`
  - Mới: `private async Task<int> HD<Tên>(JsonHelper cfg, ScriptAction action)` — `_client` (DeviceWorker) lấy từ field, `statusPrefix`/`actionName` lấy từ `_mainService._sate` + `action.Name`.
- E3. Thay `SetStatusAccount(idx, msg)` → `_mainService.SetStatus(msg, color)`.
- E4. Thêm case vào switch chính ở [FacebookFarming.cs:190-301](file://e:/LamToolAutoPhonePrime/Sunny.Subd.Core/Facebook/FacebookFarming.cs#L190).

### Phase F — Wire-up handler riêng đã có
- F1. Thay 4 case còn `break;` rỗng (HDUpAvatar/HDUpCover/HDOnOff2FA/HDAddMail) bằng gọi `FbChangeAvatarHandler/FbChangeCoverHandler/FbTurn2FAHandler/FbChangeMail.ExecuteAsync(...)`.

### Phase G — Test
- G1. Test FarmXu mặc định trên 1 device (4 action: DocThongBao, XemWatch, TuongTacNewfeed, NghiGiaiLao) — đảm bảo không regression.
- G2. Tạo 1 account → gán kịch bản custom có 5 action mới port → verify đúng kịch bản chạy.
- G3. Tạo 2 account gán 2 kịch bản khác nhau → verify routing đúng (account A chạy script A, account B chạy script B).

---

## 9. Ước lượng khối lượng

| Phase | Mô tả | LOC | PR/commit |
|---|---|---|---|
| A | DB + routing | ~50 | 1 |
| B | Port DeviceWorker | ~2000 | 1-2 |
| C | Mở rộng type + dictionary | ~150 | 1 |
| D | Port ~60 form UI | ~6000 | 8-10 |
| E | Port ~58 HD method | ~7000 | 10-12 |
| F | Wire 4 handler riêng | ~50 | 1 |
| G | Test | — | — |
| **Tổng** | | **~15000** | **~22-27 PR** |

---

**Kết luận tổng**: Sau decisions của user, scope tăng đáng kể (port toàn bộ chứ không gộp). Quan trọng nhất là Phase A (foundation routing per-account) phải làm đầu tiên và đúng — các Phase sau là việc cơ học port code từ MaxPhoneFarm. Đề xuất bắt đầu Phase A ngay khi user OK.

---

## 10. Tiến độ thực hiện (2026-04-20)

### ✅ Đã xong
- **Phase A — Foundation routing per-account** ([ScriptContext.cs:82](file://e:/LamToolAutoPhonePrime/Sunny.Subdy.Data/Context/ScriptContext.cs#L82), [FacebookFarming.cs:93](file://e:/LamToolAutoPhonePrime/Sunny.Subd.Core/Facebook/FacebookFarming.cs#L93), [MainService.cs:773](file://e:/LamToolAutoPhonePrime/Sunny.Subd.Core/Services/MainService.cs#L773), [Account.cs:78](file://e:/LamToolAutoPhonePrime/Sunny.Subdy.Data/Models/Account.cs#L78))
  - `Account.NameScript` bỏ `[NotMapped]` → persist xuống DB, auto migrate qua `EnsureTable`.
  - `MainService.RunAsync` bỏ comment runner, gọi `FacebookFarming.ExecuteAsync()`.
  - `FacebookFarming` đọc `_account.NameScript`, fallback FarmXu, fallback "không tồn tại kịch bản".
  - `ScriptContext.EnsureFarmXu(platform)` helper idempotent — tạo FarmXu + 4 default action. Gọi ở cả fQuanLyKichBan + FacebookFarming.ExecuteAsync.
- **Phase B — Wire existing HD methods** ([FacebookFarming.cs:190](file://e:/LamToolAutoPhonePrime/Sunny.Subd.Core/Facebook/FacebookFarming.cs#L190))
  - 26 case `break;` rỗng → gọi HD method có sẵn với adapter signature (0, "", jsonHelper, action.Name, action.Id.ToString() làm dataKey, local ref var).
  - `SetStatusAccount` stub → forward sang `_mainService.SetStatus` (2 overload).
  - Hủy bỏ đề xuất port DeviceWorker — không cần vì HD method đã dùng `_client` (ADBClient).
- **Phase C — Mở rộng type** ([FacebookFarmingType.cs](file://e:/LamToolAutoPhonePrime/Sunny.Subdy.Common/Models/FacebookFarmingType.cs))
  - Thêm 49 const (29 sub-mode + 20 form 2B).
  - Populate 49 entry vào `DictionariesAction` + `DescriptionAction`.
  - Wire 9 case thêm vào switch (HDKetBanGoiY/TheoTuKhoa, HDSpamBanBe/Nhom/BaiViet/Newfeed, HDXoaReel, HDVerifyAccount, HDCauHinhTaiKhoan).
- **Phase F — Assess handler pattern**
  - Phát hiện handler pattern (`IActionHandler` / `ActionExecutor`) là **code song song không active** trong runtime. `FacebookFarming.HDUpAvatar/Cover/OnOff2FA/AddMail` đã có body inline và đã wire → không cần refactor sang handler pattern.
- **Phase G — Test foundation**
  - Build main app + Core project: 0 errors.
  - Trace flow end-to-end: `ucdgvAccount.RunningThread → MainService.RunAsync → FacebookFarming.ExecuteAsync → EnsureFarmXu → LoadScript (by NameScript) → loop action → switch dispatch → HD<Type>()` — OK.
  - Thực test trên device thật: **còn lại, cần user chạy**.

### ⏳ Chưa làm (không gấp)
- **Phase D — Port form UI 40 form mới** (decision 3 sub-mode + decision 1 form 2B). Chỉ cần khi user chủ động tạo action thuộc các type mới này; hiện tại chưa có form UI để tạo, type mới chưa bị kích hoạt trong runtime.
- **Phase E — Implement HD method body còn thiếu** (~45 method chưa có body khi switch gọi vào): HDTimKiemGoogle, HDNhanTinBanBe, HDKhangSpam, HDChocBanBe, HDChucMungSinhNhat, HDReport, HDReportVideo, HDBackupData, HDChaySeeding, HDSeedingByVideo, HDSeedingEvents, HDBuffTinNhanProfile, HDBuffLikeComment, HDBuffFollowLikePage, + toàn bộ sub-mode Reel/BaiViet/KetBan/ThamGiaNhom/BaiViet*. Port từ `fMain.cs` MaxPhoneFarm (line ~3865+) theo pattern đã có.

### 🧪 Checklist test trên device thật
1. **Foundation FarmXu**:
   - Chạy tool lần đầu (DB trống) → tool tự tạo FarmXu + 4 action khi login account.
   - Tài khoản mới add không gán script → chạy FarmXu.
   - Xóa FarmXu → tool tự tạo lại ở lần run kế tiếp.
2. **Per-account routing**:
   - Tạo script "Custom1" có 2 action, gán cho account A.
   - Account B không gán → chạy FarmXu, account A → chạy Custom1.
   - Xóa script Custom1 → account A remap về FarmXu (Phase A đã làm).
3. **HD method wired**:
   - FarmXu 4 action: HDDocThongBao, HDXemWatch, HDTuongTacNewfeed, HDNghiGiaiLao — verify mỗi action chạy và status hiển thị.
   - 26 action wire mới (HDXemStory, HDRoiNhom, HDTaoNhom, HDTaoPage, HDDangStory, HDDanhGiaPage, HDBuffFollowUID, HDMoiBanBeLikePage/VaoNhom, HDDoiTen, HDDoiMatKhau, HDOnOff2FA, HDXoaSdt, HDAddMail, HDCapNhatThongTin, HDDangXuatThietBiCu, HDXoaThietBiTinCay, HDBatCheDoChuyenNghiep, HDDongBoDanhBa, HDUpAvatar, HDUpCover, HDDangBaiPage, HDBuffLikePage) — test 1 action tiêu biểu mỗi nhóm.
4. **Status feedback**:
   - Verify `SetStatusAccount` forward đúng sang UI account status column (trước đây empty stub → giờ có log).
