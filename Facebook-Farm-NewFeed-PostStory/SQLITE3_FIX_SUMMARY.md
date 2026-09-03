# Fix: SQLite3 ABI Detection for 32-bit Devices

## Vấn đề
Máy Android 32-bit (armeabi-v7a, x86) không xóa được database account Facebook khi AppClear do thiếu binary sqlite3 phù hợp với ABI của device.

## Root Cause
1. **Code không detect ABI**: `LocalSqlite3BinaryPath()` chỉ return đường dẫn cố định `resources/sqlite3` thay vì resolve theo ABI như comment mô tả
2. **Thiếu binary 32-bit**: Chỉ có 1 file binary x86_64 duy nhất, không support ARM 32-bit
3. **Structure sai**: Binary nằm ở `resources/sqlite3` (file) thay vì `resources/sqlite3/{abi}/sqlite3` (folder structure)

## Solution Implemented

### 1. Code Changes
**File**: `AutoAndroid/Clients/ADBClient.cs`

Sửa `LocalSqlite3BinaryPath()` để:
- Detect ABI từ `GetProp("ro.product.cpu.abi")` (giống pattern của `InitHelper.cs`)
- Map ABI → subfolder path với fallback logic:
  - `arm64-v8a` → fallback về `armeabi-v7a`
  - `x86_64` → fallback về `x86`
  - `armeabi` → dùng `armeabi-v7a`
- Return đường dẫn đầy đủ: `{BaseDirectory}/resources/sqlite3/{abi}/sqlite3`
- Log rõ ràng khi thiếu binary

### 2. Resource Structure
```
resources/sqlite3/
├── README.md                    # Hướng dẫn build/download binary
├── arm64-v8a/
│   ├── .placeholder             # Chờ binary (QUAN TRỌNG NHẤT)
│   └── sqlite3                  # [CẦN BỔ SUNG]
├── armeabi-v7a/
│   ├── .placeholder             # Chờ binary (máy 32-bit)
│   └── sqlite3                  # [CẦN BỔ SUNG]
├── x86/
│   ├── .placeholder             # Chờ binary (emulator)
│   └── sqlite3                  # [CẦN BỔ SUNG]
└── x86_64/
    └── sqlite3                  # ✅ Đã có (ELF 64-bit x86-64)
```

### 3. Build Configuration
**File**: `Facebook-Farm-NewFeed-PostStory.csproj`

Thêm target `CopySqlite3Binaries` để copy toàn bộ folder structure vào build output:
```xml
<Target Name="CopySqlite3Binaries" AfterTargets="Build">
  <ItemGroup>
    <Sqlite3Binary Include="$(MSBuildProjectDirectory)\resources\sqlite3\**\*.*" />
  </ItemGroup>
  <Copy SourceFiles="@(Sqlite3Binary)"
        DestinationFolder="$(OutDir)resources\sqlite3\%(RecursiveDir)"
        SkipUnchangedFiles="true"
        Condition="'@(Sqlite3Binary)' != ''" />
</Target>
```

## Verification

### Build Output
```bash
bin/Debug/net9.0-windows/resources/sqlite3/
├── arm64-v8a/.placeholder
├── armeabi-v7a/.placeholder  
├── x86/.placeholder
├── x86_64/sqlite3             # ✅
└── README.md
```

### Logic Flow
1. `AppClear("com.facebook.katana")` → gọi `DeleteAccounts()`
2. `DeleteAccounts()` → gọi `ResolveSqlite3()`
3. `ResolveSqlite3()` → gọi `LocalSqlite3BinaryPath()`
4. **MỚI**: `LocalSqlite3BinaryPath()` detect ABI từ device → return đúng binary path
5. Nếu có binary → push lên `/data/local/tmp/sqlite3` → xóa account database
6. Nếu thiếu binary → log warning → skip xóa account (như cũ, nhưng giờ có log rõ ràng)

## Action Items

### ✅ Completed
- [x] Fix `LocalSqlite3BinaryPath()` với ABI detection + fallback
- [x] Tạo folder structure cho 4 ABI
- [x] Copy binary x86_64 hiện có vào structure mới
- [x] Update csproj để copy resources
- [x] Tạo README hướng dẫn build binary
- [x] Verify build output

### ⚠️ Pending (CẦN BỔ SUNG BINARY)
- [ ] **Priority 1**: Build/download binary `arm64-v8a/sqlite3` (90%+ device hiện đại)
- [ ] **Priority 2**: Build/download binary `armeabi-v7a/sqlite3` (máy 32-bit)
- [ ] **Priority 3**: Build/download binary `x86/sqlite3` (emulator)

### 📋 Hướng dẫn build binary
Xem chi tiết trong `resources/sqlite3/README.md`:
- Option 1: Build từ source dùng Android NDK (recommended)
- Option 2: Download prebuilt từ nguồn tin cậy
- Option 3: Extract từ ROM có sẵn sqlite3

### Kiểm tra binary
```bash
file sqlite3        # Phải match architecture: ARM aarch64 / ARMv7 / x86
ldd sqlite3         # Dependencies phải tối thiểu (static linking best)
./sqlite3 -version  # Test chạy được
```

## Impact

### Trước khi fix
- **x86_64 emulator**: ✅ Xóa được (có binary)
- **arm64-v8a device**: ❌ Không xóa được (thiếu binary)
- **armeabi-v7a device**: ❌ Không xóa được (thiếu binary)
- **x86 emulator**: ❌ Không xóa được (thiếu binary)
- **Log**: Không có thông báo lỗi rõ ràng

### Sau khi fix + có đủ binary
- **Tất cả ABI**: ✅ Xóa được account database đúng cách
- **Thiếu binary**: Log warning rõ ràng: `[LocalSqlite3BinaryPath] Không tìm thấy binary cho ABI 'arm64-v8a'`
- **Farm quality**: Tăng độ sạch khi reset account, giảm nguy cơ bị phát hiện

## Testing Checklist
Khi có đủ binary, test trên device thật:
- [ ] arm64-v8a device (ví dụ: Samsung Galaxy S21+)
- [ ] armeabi-v7a device (máy cũ)
- [ ] x86_64 emulator
- [ ] x86 emulator

**Test case**: 
1. Login Facebook
2. AppClear → verify account bị xóa khỏi `/data/system_ce/0/accounts_ce.db`
3. Mở Facebook → phải login lại từ đầu

## Related Files
- `AutoAndroid/Clients/ADBClient.cs` - Logic chính
- `Facebook-Farm-NewFeed-PostStory.csproj` - Build config
- `resources/sqlite3/README.md` - Hướng dẫn binary
- `resources/sqlite3/{abi}/sqlite3` - Binary files

## References
- ABI detection pattern từ `AutoAndroid/Helpers/InitHelper.cs` (atx-agent deployment)
- Android ABI docs: https://developer.android.com/ndk/guides/abis
- SQLite build guide: https://www.sqlite.org/howtocompile.html
