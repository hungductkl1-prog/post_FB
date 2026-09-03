# Hướng dẫn xóa dữ liệu account Facebook trên Android một cách sạch sẽ

## Tổng quan

Khi xóa dữ liệu Facebook trên Android, `pm clear` chỉ xóa dữ liệu app-level (/data/data/) nhưng **không xóa account đã đăng ký** trong Android Account System. Account này tồn tại ở system-level và persist qua nhiều lần cài đặt/gỡ app.

## Kiến trúc Android Account System

### 1. Cấu trúc dữ liệu
Android lưu account thông qua `AccountManagerService` trong 2 database:

```
/data/system_ce/0/accounts_ce.db    # Credential-encrypted storage
/data/system_de/0/accounts_de.db    # Device-encrypted storage
```

**Phân biệt CE vs DE:**
- **CE (Credential Encrypted)**: Chỉ decrypt sau khi user unlock device (nhập PIN/password lần đầu)
- **DE (Device Encrypted)**: Decrypt ngay khi boot, trước khi user unlock

### 2. Schema database

#### accounts_ce.db
```sql
-- Bảng chính lưu account
CREATE TABLE accounts (
    _id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,           -- Email/username
    type TEXT NOT NULL,           -- "com.facebook.auth.login"
    password TEXT,                -- Access token (encrypted)
    UNIQUE(name,type)
);

-- Bảng metadata
CREATE TABLE extras (
    _id INTEGER PRIMARY KEY AUTOINCREMENT,
    accounts_id INTEGER,
    key TEXT NOT NULL,
    value TEXT,
    FOREIGN KEY(accounts_id) REFERENCES accounts(_id)
);

-- Auth tokens cache
CREATE TABLE authtokens (
    _id INTEGER PRIMARY KEY AUTOINCREMENT,
    accounts_id INTEGER NOT NULL,
    type TEXT NOT NULL,
    authtoken TEXT,
    FOREIGN KEY(accounts_id) REFERENCES accounts(_id)
);

-- Sequence tracking
CREATE TABLE sqlite_sequence (
    name TEXT,
    seq INTEGER
);
```

#### accounts_de.db
```sql
-- Mirror của accounts table (không có password)
CREATE TABLE accounts (
    _id INTEGER,
    name TEXT NOT NULL,
    type TEXT NOT NULL
);

-- Debug info
CREATE TABLE debug_table (...);

-- Metadata
CREATE TABLE meta (
    key TEXT PRIMARY KEY,
    value TEXT
);
```

### 3. Facebook account types
Facebook đăng ký nhiều loại account:
- `com.facebook.auth.login` - Main account
- `com.facebook.messenger` - Messenger (nếu có)
- `com.facebook.pages` - Pages (nếu có)
- `com.facebook.workchat` - Work chat

## Phương pháp xóa account

### ⭐ Phương pháp 1: Xóa trực tiếp database (Code hiện tại)

**Ưu điểm**: Nhanh, xóa sạch 100%, không cần AccountManager API  
**Nhược điểm**: Cần root, có thể gây crash nếu AccountManagerService đang cache

#### Implementation
```bash
# 1. Query tất cả account IDs
sqlite3 /data/system_ce/0/accounts_ce.db \
  "SELECT _id FROM accounts;"

# 2. Với mỗi account ID, xóa từ cả 2 database
# accounts_de.db
sqlite3 /data/system_de/0/accounts_de.db \
  "DELETE FROM accounts WHERE _id = {id};"

sqlite3 /data/system_de/0/accounts_de.db \
  "DELETE FROM debug_table;"

sqlite3 /data/system_de/0/accounts_de.db \
  "DELETE FROM meta;"

# accounts_ce.db
sqlite3 /data/system_ce/0/accounts_ce.db \
  "DELETE FROM accounts WHERE _id = {id};"

sqlite3 /data/system_ce/0/accounts_ce.db \
  "DELETE FROM authtokens WHERE accounts_id = {id};"

sqlite3 /data/system_ce/0/accounts_ce.db \
  "DELETE FROM extras WHERE accounts_id = {id};"

sqlite3 /data/system_ce/0/accounts_ce.db \
  "DELETE FROM sqlite_sequence WHERE name = 'accounts';"
```

#### Code trong tool
Xem [ADBClient.cs:767-804](../AutoAndroid/Clients/ADBClient.cs#L767-L804)

**⚠️ Vấn đề hiện tại code:**
- Chỉ xóa từ `accounts` table
- **THIẾU**: Không xóa `authtokens`, `extras` → orphaned data
- **THIẾU**: Không filter theo account type → xóa ALL accounts (không chỉ Facebook)

#### ✅ Code cải tiến (Recommended)
```csharp
private void DeleteFacebookAccounts()
{
    try
    {
        string sqlite = ResolveSqlite3();
        if (string.IsNullOrEmpty(sqlite))
        {
            LogHelper.Log("[DeleteFacebookAccounts] Không có sqlite3.");
            return;
        }

        // Query chỉ Facebook accounts (filter theo type)
        string query = @"SELECT _id FROM accounts WHERE type LIKE 'com.facebook%';";
        string output = Shell($"su -c \"{sqlite} /data/system_ce/0/accounts_ce.db '{query}'\"", 5);
        
        string[] ids = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string row in ids)
        {
            if (!int.TryParse(row.Trim(), out int id))
                continue;

            LogHelper.Log($"[DeleteFacebookAccounts] Xóa Facebook account ID={id}");

            // 1. Xóa từ accounts_de.db
            Shell($"su -c \"{sqlite} /data/system_de/0/accounts_de.db 'DELETE FROM accounts WHERE _id = {id};'\"", 5);
            
            // 2. Xóa từ accounts_ce.db (cascade: accounts + extras + authtokens)
            Shell($"su -c \"{sqlite} /data/system_ce/0/accounts_ce.db 'DELETE FROM authtokens WHERE accounts_id = {id};'\"", 5);
            Shell($"su -c \"{sqlite} /data/system_ce/0/accounts_ce.db 'DELETE FROM extras WHERE accounts_id = {id};'\"", 5);
            Shell($"su -c \"{sqlite} /data/system_ce/0/accounts_ce.db 'DELETE FROM accounts WHERE _id = {id};'\"", 5);
        }

        // 3. Reset sequence counter
        Shell($"su -c \"{sqlite} /data/system_ce/0/accounts_ce.db 'DELETE FROM sqlite_sequence WHERE name = \\\"accounts\\\";'\"", 5);
        
        // 4. QUAN TRỌNG: Kill AccountManagerService để reload database
        Shell("su -c \"killall system_server\"", 5); // Hoặc reboot nhẹ hơn
        
        LogHelper.SUCCESS($"Đã xóa {ids.Length} Facebook accounts.");
    }
    catch (Exception ex)
    {
        LogHelper.Log($"[DeleteFacebookAccounts] Lỗi: {ex.Message}");
    }
}
```

### 🔄 Phương pháp 2: Sử dụng AccountManager API

**Ưu điểm**: An toàn, không cần root, Android tự cleanup  
**Nhược điểm**: Cần user confirmation dialog (không thể silent), chậm hơn

#### Via ADB (Android 4.2+)
```bash
# List accounts
adb shell dumpsys account

# Remove account (cần user confirm nếu không root)
adb shell am broadcast -a android.accounts.LOGIN_ACCOUNTS_CHANGED

# Hoặc dùng service call (undocumented, có thể không stable)
adb shell service call account 8 s16 "com.facebook.auth.login" s16 "user@email.com"
```

#### Via Java code (nếu có app helper)
```java
AccountManager accountManager = AccountManager.get(context);
Account[] accounts = accountManager.getAccountsByType("com.facebook.auth.login");

for (Account account : accounts) {
    // Android 5.0+ (API 21+)
    accountManager.removeAccount(account, null, null, null);
    
    // Hoặc với callback
    accountManager.removeAccount(account, new AccountManagerCallback<Bundle>() {
        @Override
        public void run(AccountManagerFuture<Bundle> future) {
            try {
                Bundle result = future.getResult();
                boolean success = result.getBoolean(AccountManager.KEY_BOOLEAN_RESULT);
                Log.d("Account", "Removed: " + success);
            } catch (Exception e) {
                e.printStackTrace();
            }
        }
    }, null);
}
```

### 🗑️ Phương pháp 3: Xóa thủ công file database

**Ưu điểm**: Đơn giản nhất  
**Nhược điểm**: Nguy hiểm - xóa TẤT CẢ accounts (Google, Samsung, v.v.), cần reboot

```bash
su -c "rm -f /data/system_ce/0/accounts_ce.db*"
su -c "rm -f /data/system_de/0/accounts_de.db*"
reboot
```

⚠️ **KHÔNG khuyến khích** - sẽ xóa cả Google account → mất Google Play, Sync, v.v.

### 🔧 Phương pháp 4: Kết hợp pm clear + disable/enable

**Hiện tại code đang dùng** - xem [ADBClient.cs:753-758](../AutoAndroid/Clients/ADBClient.cs#L753-L758)

```bash
# 1. Clear app data (5 lần để chắc chắn)
pm clear com.facebook.katana
pm clear com.facebook.katana
pm clear com.facebook.katana
pm clear com.facebook.katana
pm clear com.facebook.katana

# 2. Xóa accounts (phương pháp 1)
# ... DeleteAccounts() ...

# 3. Disable rồi enable lại để reset state
pm disable-user --user 0 com.facebook.katana
pm enable --user 0 com.facebook.katana
```

**Tại sao disable/enable?**
- Clear cache của PackageManager
- Force re-register BroadcastReceivers, ContentProviders
- Reset app permissions state
- Xóa runtime cache trong system_server

## Các vùng dữ liệu Facebook khác cần xóa

### 1. App data (/data/data/)
```bash
pm clear com.facebook.katana       # Main app
pm clear com.facebook.lite          # Lite
pm clear com.facebook.orca          # Messenger
pm clear com.facebook.mlite         # Messenger Lite
```

### 2. External storage
```bash
rm -rf /sdcard/Android/data/com.facebook.katana/
rm -rf /sdcard/Android/media/com.facebook.katana/
rm -rf /sdcard/DCIM/.facebook/      # Cached images
rm -rf /sdcard/Pictures/Facebook/
```

### 3. Shared storage (Android 11+)
```bash
rm -rf /sdcard/Android/media/com.facebook.katana/
```

### 4. OBB files
```bash
rm -rf /sdcard/Android/obb/com.facebook.katana/
```

### 5. System cache
```bash
rm -rf /data/system/users/0/app_idle_stats.xml    # App usage stats
rm -rf /data/system/users/0/package-restrictions.xml  # App permissions
```

### 6. WebView cache (quan trọng!)
```bash
# Facebook sử dụng WebView để login → cookies/tokens có thể persist
pm clear com.android.webview
pm clear com.google.android.webview
pm clear org.chromium.webview_shell

# Hoặc xóa trực tiếp
rm -rf /data/data/com.facebook.katana/app_webview/
```

### 7. Sync adapter data
```bash
# List sync adapters
dumpsys account | grep -A5 "com.facebook"

# Stop sync trước khi xóa
content call --uri content://settings/system --method DELETE \
  --arg "sync_enabled" --extra string:package com.facebook.katana
```

## Checklist xóa hoàn toàn

```bash
#!/system/bin/sh
# Complete Facebook data removal script

PACKAGES=(
    "com.facebook.katana"
    "com.facebook.lite"
    "com.facebook.orca"
    "com.facebook.mlite"
)

for pkg in "${PACKAGES[@]}"; do
    echo "Cleaning $pkg..."
    
    # 1. Stop app
    am force-stop $pkg
    
    # 2. Clear app data
    pm clear $pkg
    
    # 3. Clear external storage
    rm -rf /sdcard/Android/data/$pkg/
    rm -rf /sdcard/Android/media/$pkg/
    rm -rf /sdcard/Android/obb/$pkg/
    
    # 4. Disable then enable
    pm disable-user --user 0 $pkg
    pm enable --user 0 $pkg
done

# 5. Remove accounts from system database
sqlite3 /data/system_ce/0/accounts_ce.db \
  "DELETE FROM accounts WHERE type LIKE 'com.facebook%';"
sqlite3 /data/system_ce/0/accounts_ce.db \
  "DELETE FROM authtokens WHERE accounts_id NOT IN (SELECT _id FROM accounts);"
sqlite3 /data/system_ce/0/accounts_ce.db \
  "DELETE FROM extras WHERE accounts_id NOT IN (SELECT _id FROM accounts);"
sqlite3 /data/system_de/0/accounts_de.db \
  "DELETE FROM accounts WHERE type LIKE 'com.facebook%';"

# 6. Clear WebView cache
pm clear com.android.webview

# 7. Clear system cache
rm -rf /data/system/users/0/app_idle_stats.xml
rm -rf /data/system/users/0/package-restrictions.xml

# 8. Restart system_server để reload account database
killall system_server

echo "Facebook data cleaned completely."
```

## Verify xóa thành công

### 1. Kiểm tra database
```bash
# Không được thấy Facebook accounts
sqlite3 /data/system_ce/0/accounts_ce.db \
  "SELECT * FROM accounts WHERE type LIKE 'com.facebook%';"

# Kết quả phải rỗng
```

### 2. Kiểm tra qua dumpsys
```bash
dumpsys account | grep -i facebook
# Không được thấy output nào
```

### 3. Kiểm tra app data
```bash
du -sh /data/data/com.facebook.katana
# Phải rất nhỏ (~1-2MB) - chỉ APK base structure
```

### 4. Test thực tế
- Mở Facebook app → phải hiện màn hình login từ đầu
- Không auto-fill email/password
- Không thấy thông báo "Welcome back"

## Các vấn đề thường gặp

### 1. Accounts vẫn còn sau khi xóa
**Nguyên nhân**: AccountManagerService đang cache trong memory

**Giải pháp**:
```bash
# Option 1: Kill system_server (sẽ restart zygote)
killall system_server

# Option 2: Reboot (chậm hơn nhưng đảm bảo hơn)
reboot

# Option 3: Clear accounts service cache (Android 9+)
cmd account clear-all
```

### 2. SQLite3 binary không chạy được
**Nguyên nhân**: Sai ABI (32-bit binary trên device 64-bit hoặc ngược lại)

**Giải pháp**: Xem [SQLITE3_FIX_SUMMARY.md](SQLITE3_FIX_SUMMARY.md) - đã fix trong tool

### 3. Permission denied khi xóa database
**Nguyên nhân**: 
- Chưa có root
- SELinux đang enforce mode
- Database đang được lock bởi AccountManagerService

**Giải pháp**:
```bash
# Check root
su -c "id"

# Check SELinux
getenforce
# Nếu Enforcing:
setenforce 0

# Stop services đang lock file
stop
# Xóa database
rm /data/system_ce/0/accounts_ce.db*
# Start lại
start
```

### 4. Facebook tự động login lại
**Nguyên nhân**:
- Token còn trong WebView cookies
- Token còn trong Keystore
- Sync từ Google account backup

**Giải pháp**:
```bash
# Clear WebView
pm clear com.android.webview

# Clear Keystore (nếu cần)
su -c "rm -rf /data/misc/keystore/user_0/*"

# Disable Google backup
bmgr disable
```

## Best practices cho automation farm

### 1. Sequence đúng
```
1. Stop Facebook app (am force-stop)
2. Clear app data 5 lần (pm clear × 5)
3. Xóa accounts từ database (sqlite3 DELETE)
4. Xóa external storage (rm -rf /sdcard/Android/data/...)
5. Clear WebView cache
6. Disable/Enable app
7. Kill system_server HOẶC reboot
8. Verify không còn account (dumpsys account)
```

### 2. Logging đầy đủ
```csharp
LogHelper.Log($"[Cleanup] Bước 1: Stop app");
LogHelper.Log($"[Cleanup] Bước 2: Clear data");
// ... mỗi bước log rõ ràng
LogHelper.SUCCESS($"[Cleanup] Hoàn thành - verified clean");
```

### 3. Retry logic
```csharp
int maxRetry = 3;
for (int i = 0; i < maxRetry; i++)
{
    DeleteFacebookAccounts();
    if (VerifyAccountsRemoved())
    {
        LogHelper.SUCCESS("Accounts removed successfully.");
        break;
    }
    LogHelper.Log($"Retry {i+1}/{maxRetry}...");
    Thread.Sleep(2000);
}
```

### 4. Rollback safe
- Backup database trước khi xóa (nếu cần recovery)
- Không xóa Google accounts (filter `WHERE type LIKE 'com.facebook%'`)
- Log account IDs trước khi xóa để debug

## References

### Android Source Code
- [AccountManagerService.java](https://cs.android.com/android/platform/superproject/+/master:frameworks/base/services/core/java/com/android/server/accounts/AccountManagerService.java)
- [AccountManager.java](https://cs.android.com/android/platform/superproject/+/master:frameworks/base/core/java/android/accounts/AccountManager.java)

### Database paths (Android 7.0+)
- Direct Boot mode: `/data/system_de/0/`
- Credential Encrypted: `/data/system_ce/0/`

### Account types
- Facebook Main: `com.facebook.auth.login`
- Facebook Messenger: `com.facebook.messenger`
- Google: `com.google`
- Samsung: `com.osp.app.signin`

## Tóm tắt

**Phương pháp khuyến nghị cho farm automation:**
1. **Dùng SQLite3 direct delete** (phương pháp 1 cải tiến) - nhanh nhất, control hoàn toàn
2. **Filter theo account type** - chỉ xóa Facebook, giữ nguyên Google/system accounts
3. **Xóa cascade** - accounts + authtokens + extras
4. **Clear WebView cache** - tránh token persist
5. **Kill system_server** - force reload database
6. **Verify sau khi xóa** - đảm bảo sạch 100%

Code implementation: Xem phần "Code cải tiến" ở trên.
