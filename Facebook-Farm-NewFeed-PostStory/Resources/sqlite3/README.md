# SQLite3 Binary Resources

Thư mục này chứa binary `sqlite3` cho các ABI khác nhau của Android. Tool sẽ tự động detect ABI của device và push binary phù hợp lên `/data/local/tmp/sqlite3`.

## Cấu trúc thư mục

```
sqlite3/
├── arm64-v8a/sqlite3      # Android 64-bit ARM (phổ biến nhất)
├── armeabi-v7a/sqlite3    # Android 32-bit ARM (máy cũ)
├── x86/sqlite3            # Android 32-bit x86 (emulator, tablet)
└── x86_64/sqlite3         # Android 64-bit x86 (emulator, một số tablet)
```

## Fallback logic

- `arm64-v8a` → fallback về `armeabi-v7a` nếu thiếu
- `x86_64` → fallback về `x86` nếu thiếu
- `armeabi` → dùng `armeabi-v7a`

## Cách lấy binary

### Option 1: Build từ source (recommended)
```bash
# Clone SQLite source
git clone https://github.com/sqlite/sqlite.git
cd sqlite

# Build cho từng ABI dùng Android NDK
# Cần cài Android NDK trước: https://developer.android.com/ndk

# arm64-v8a (64-bit ARM)
export NDK_ROOT=/path/to/ndk
$NDK_ROOT/toolchains/llvm/prebuilt/linux-x86_64/bin/aarch64-linux-android21-clang \
    -o sqlite3 shell.c sqlite3.c -lpthread -ldl

# armeabi-v7a (32-bit ARM)
$NDK_ROOT/toolchains/llvm/prebuilt/linux-x86_64/bin/armv7a-linux-androideabi21-clang \
    -o sqlite3 shell.c sqlite3.c -lpthread -ldl

# x86 (32-bit x86)
$NDK_ROOT/toolchains/llvm/prebuilt/linux-x86_64/bin/i686-linux-android21-clang \
    -o sqlite3 shell.c sqlite3.c -lpthread -ldl

# x86_64 (64-bit x86)
$NDK_ROOT/toolchains/llvm/prebuilt/linux-x86_64/bin/x86_64-linux-android21-clang \
    -o sqlite3 shell.c sqlite3.c -lpthread -ldl
```

### Option 2: Download prebuilt (nếu có nguồn tin cậy)
Tìm prebuilt binaries từ:
- https://github.com/nkk71/busybox-arm64/releases (có các tool tương tự)
- ROM custom (LineageOS, AOSP) thường đi kèm sqlite3
- Termux packages: `apt download sqlite && dpkg -x sqlite*.deb .`

### Option 3: Extract từ ROM
```bash
# Nếu có device đã root với sqlite3 sẵn:
adb shell "su -c 'which sqlite3'"
adb pull /system/xbin/sqlite3 ./arm64-v8a/sqlite3
```

## Verify binary
```bash
file sqlite3  # Kiểm tra architecture
ldd sqlite3   # Kiểm tra dependencies (phải static hoặc chỉ libc)
```

## Hiện trạng
- ✅ `x86_64/sqlite3` - Đã có (ELF 64-bit x86-64)
- ❌ `arm64-v8a/sqlite3` - **CẦN BỔ SUNG** (quan trọng nhất - 90%+ device hiện đại)
- ❌ `armeabi-v7a/sqlite3` - **CẦN BỔ SUNG** (máy 32-bit)
- ❌ `x86/sqlite3` - Cần bổ sung (emulator)

## Impact khi thiếu binary
Nếu thiếu binary cho ABI của device:
1. `ResolveSqlite3()` trả về chuỗi rỗng
2. `DeleteAccounts()` bỏ qua xóa account database
3. Facebook account data **không bị xóa hoàn toàn** khi AppClear
4. Farm có thể bị phát hiện do dữ liệu cũ còn sót

**Priority**: Cần bổ sung `arm64-v8a` và `armeabi-v7a` ngay để support máy Android thật.
