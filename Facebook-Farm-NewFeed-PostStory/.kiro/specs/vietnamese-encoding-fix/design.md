# Vietnamese Encoding Fix Bugfix Design

## Overview

Vietnamese text in the .NET 9 WinForms application (`Facebook-Farm-NewFeed-PostStory`) is rendered as mojibake (e.g. `Thi?t b? kh�ng c� internet` instead of `Thiết bị không có internet`). The corruption originates at encoding boundaries: UTF-8 content is read or written through the platform default code page (ANSI / Windows-1252 / Windows-1258) because the string-I/O calls omit an explicit UTF-8 encoding, and some source/content files are stored at rest in a non-UTF-8 encoding.

The fix strategy is to enforce UTF-8 (specifically `new UTF8Encoding(false)` — UTF-8 without BOM) at every string-I/O boundary that currently relies on the platform default, and to re-encode any content files that are corrupted at rest. The change is deliberately minimal and targeted: it only touches the encoding argument of I/O operations and file-at-rest encoding. It must not alter application logic, layout, control flow, or the behavior of content that is already correct, ASCII-only, already UTF-8, or non-text/binary.

This design formalizes the bug condition, the expected (fixed) behavior, the preservation requirements, the hypothesized root cause grounded in the actual codebase, the specific implementation changes, and a two-phase testing strategy (exploratory bug-condition checking, then fix checking and preservation checking).

## Glossary

- **Bug_Condition (C)**: The condition that triggers the bug — a content unit carries Vietnamese / non-ASCII UTF-8 text AND is handled through a non-UTF-8 encoding (default code page / ANSI / 1252 / 1258), OR its current rendered form already contains mojibake.
- **Property (P)**: The desired behavior — Vietnamese text is handled as UTF-8 and round-trips intact with no mojibake, matching the intended Vietnamese text.
- **Preservation**: Existing behavior that must remain unchanged by the fix — ASCII-only content, already-correct UTF-8 content, already-explicit UTF-8 handling, binary/non-text files, valid non-Vietnamese Unicode, and all non-encoding application logic.
- **ContentUnit (X)**: The unit the bug condition applies to — either a text file (with its byte content and on-disk encoding) or a string-I/O operation (bytes/string crossing an encoding boundary: file read/write, JSON, logging, database, API response, console output).
- **Mojibake**: Corrupted text containing replacement characters (`�`), `?`-substituted diacritics, or otherwise broken Unicode resulting from a decode/encode mismatch.
- **F**: The original code, before encoding correction.
- **F'**: The fixed code, with `new UTF8Encoding(false)` enforced across all string-I/O boundaries and corrupted files re-encoded.
- **Default code page**: The platform's non-UTF-8 encoding used by `StreamReader`/`File.ReadAll*`/`StreamWriter`/`File.WriteAll*`/`File.AppendAllText` when no explicit `Encoding` argument is supplied.

## Bug Details

### Bug Condition

The bug manifests when a content unit carrying Vietnamese (or other non-ASCII UTF-8) text crosses an encoding boundary that does not enforce UTF-8. The I/O call is either decoding UTF-8 bytes using the platform default code page (read path), encoding text using the platform default code page (write path), or the file is already stored at rest in a non-UTF-8 encoding so its literals are corrupted before any read occurs.

**Formal Specification:**
```
FUNCTION isBugCondition(X)
  INPUT: X of type ContentUnit  // a text file or a string-I/O operation
  OUTPUT: boolean

  // X is buggy when it carries Vietnamese/non-ASCII text AND is handled
  // through a non-UTF-8 encoding, OR its current rendered form already
  // contains mojibake.
  RETURN containsVietnameseOrNonAscii(X)
         AND ( handledWithNonUtf8Encoding(X)
               OR containsMojibake(X) )   // '�', '?'-substituted diacritics, broken Unicode
END FUNCTION
```

### Examples

- **File read without encoding**: `File.ReadAllLines(fileGmail)` in `ucdgvAccount.cs` reads a UTF-8-without-BOM file using the default code page. A line `Thiết bị` is decoded as `Thi?t b?`. (`isBugCondition` = true; bug present)
- **Config write without encoding**: `File.WriteAllText(ConfigFileName, content)` in `fSyncOtherTool.cs` persists Vietnamese content using the default code page, producing bytes that later decode as mojibake. (`isBugCondition` = true; bug present)
- **JSON round trip without UTF-8 enforcement**: `fPhoneSetupOptions` writes/reads config JSON via `File.WriteAllText`/`File.ReadAllText` with no encoding; Vietnamese option values lose diacritics across a save/load cycle. (`isBugCondition` = true; bug present)
- **Crash log append without encoding**: `File.AppendAllText(file, text)` in `Program.cs` writes Vietnamese exception/context text using the default code page. (`isBugCondition` = true; bug present)
- **File corrupted at rest**: a `.resx` / `.txt` / `.config` saved in Windows-1258 already stores `Thi?t b?` literally. (`isBugCondition` = true via `containsMojibake`; bug present at rest)
- **Edge case — ASCII-only file**: `File.WriteAllText(_settingsFile, mode.ToString())` writes `"Normal"`. `containsVietnameseOrNonAscii` = false → `isBugCondition` = false; behavior must be preserved exactly.

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- ASCII-only files and strings must continue to read, write, and render identically (UTF-8 is byte-compatible with ASCII).
- Files already correctly encoded as UTF-8 (with or without BOM) must continue to read and render identically, with no re-encoding-induced changes.
- Content already using explicit UTF-8 handling (e.g. `File.WriteAllText(..., Encoding.UTF8)`, `Console.OutputEncoding = Encoding.UTF8`, `new UTF8Encoding(false)`) must continue to behave identically.
- Binary / non-text files (`.png`, `.exe`, `.dll`, `.resources`, build artifacts under `obj/` and `bin/`) must be left untouched and must NOT be scanned or re-encoded.
- Non-Vietnamese valid Unicode (other languages, emoji, symbols) must be preserved unchanged.
- Existing application logic, control flow, layout, and all non-encoding behavior must function identically — the change is limited to encoding correctness.

**Scope:**
All inputs that do NOT satisfy the bug condition should be completely unaffected by this fix. This includes:
- ASCII-only content (no diacritics, no non-ASCII bytes)
- Content already correctly handled with explicit UTF-8
- Binary / non-text files and build artifacts
- Any non-encoding application behavior

**Note:** The actual expected correct behavior for buggy inputs is defined in the Correctness Properties section (Property 1). This section focuses on what must NOT change.

## Hypothesized Root Cause

Based on the bug description and a scan of the codebase, the most likely issues are:

1. **String-I/O calls omitting an explicit UTF-8 encoding**: Several read/write/append calls rely on the platform default code page instead of UTF-8.
   - `Views\Controls\ucdgvAccount.cs` → `File.ReadAllLines(fileGmail)` (read, no encoding)
   - `Views\Forms\fViewDataGridView.cs` → `File.ReadAllLines(configFile)` (read, no encoding)
   - `Views\Forms\fUpdateData.cs` → `File.ReadAllLines($"{NameJson}.txt")` (read, no encoding)
   - `Views\Forms\fSyncOtherTool.cs` → `File.ReadAllText` / `File.WriteAllText(ConfigFileName, ...)` (read + write, no encoding)
   - `Views\Forms\fSelectMultiFolder.cs` → `File.ReadAllLines(folderPath)` (read, no encoding)
   - `Views\Forms\fSelectByUid.cs` → `File.ReadAllText(uidPath)` (read, no encoding)
   - `Views\Forms\fInputWifiCredentials.cs` → `File.ReadAllText` / `File.WriteAllText(ConfigPath, ...)` (read + write, no encoding)
   - `Utils\Design\TableDensity.cs` → `File.ReadAllText`/`File.WriteAllText(_settingsFile, ...)` (read + write, no encoding — but ASCII-only, see preservation)

2. **JSON persistence without UTF-8 enforcement**: `Views\Forms\fPhoneSetupOptions.cs` serializes/deserializes config via `File.WriteAllText`/`File.ReadAllText` with no encoding, so Vietnamese option values can be corrupted across a round trip.

3. **Logging path without UTF-8 encoding**: `Program.cs` crash logger uses `File.AppendAllText(file, text)` with no encoding, corrupting Vietnamese exception/context text in the log.

4. **Files corrupted at rest**: Some source/content files (`*.resx`, `*.txt`, `*.config`, etc.) may be saved on disk in a non-UTF-8 encoding, so their Vietnamese literals are already mojibake before any read occurs and require re-encoding.

**Note on already-correct call sites (preservation evidence):** `Program.cs` already sets `Console.OutputEncoding`/`InputEncoding = Encoding.UTF8` and registers the code-pages provider; `fUpdateAuto.cs` already uses `new UTF8Encoding(false)`; `ucManagerDevices.cs`, `GridMetrics.cs`, and `AccountGridPerf.cs` already pass `Encoding.UTF8`. These sites are already correct and must remain behaviorally identical.

## Correctness Properties

Property 1: Bug Condition - Vietnamese Text Round-Trips Correctly Under UTF-8

_For any_ content unit where the bug condition holds (`isBugCondition` returns true), the fixed code SHALL handle the content as UTF-8 such that the result contains no mojibake and the rendered text equals the intended Vietnamese text — i.e. reads decode UTF-8 to the original text, writes encode as UTF-8 without BOM producing bytes that decode back to the original, JSON/database/API/log/console round trips preserve the Vietnamese characters exactly, and files corrupted at rest are re-encoded to UTF-8 (without BOM) with the Vietnamese text restored.

**Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9**

Property 2: Preservation - Non-Buggy Content Behavior Unchanged

_For any_ content unit where the bug condition does NOT hold (`isBugCondition` returns false), the fixed code SHALL produce exactly the same result as the original code, preserving ASCII-only content, already-correct UTF-8 content, already-explicit UTF-8 handling, binary/non-text files, valid non-Vietnamese Unicode, and all non-encoding application logic.

**Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6**

## Fix Implementation

### Changes Required

Assuming our root cause analysis is correct, define a single shared UTF-8-without-BOM encoding and pass it explicitly to every string-I/O boundary that currently omits it. Re-encode any content files found corrupted at rest.

**Shared encoding convention:**

```
// UTF-8 without BOM — byte-compatible with ASCII, preserves Vietnamese diacritics
private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
```

**Specific Changes:**

1. **File read paths**: Add `Utf8NoBom` (or `Encoding.UTF8` for read decoding) as the encoding argument to every encoding-bare read.
   - `ucdgvAccount.cs`: `File.ReadAllLines(fileGmail, Encoding.UTF8)`
   - `fViewDataGridView.cs`: `File.ReadAllLines(configFile, Encoding.UTF8)`
   - `fUpdateData.cs`: `File.ReadAllLines($"{NameJson}.txt", Encoding.UTF8)`
   - `fSyncOtherTool.cs` (`SafeReadConfig`): `File.ReadAllText(ConfigFileName, Encoding.UTF8)`
   - `fSelectMultiFolder.cs`: `File.ReadAllLines(folderPath, Encoding.UTF8)`
   - `fSelectByUid.cs`: `File.ReadAllText(uidPath, Encoding.UTF8)`
   - `fInputWifiCredentials.cs` (`SafeReadConfig`): `File.ReadAllText(ConfigPath, Encoding.UTF8)`
   - `TableDensity.cs`: `File.ReadAllText(_settingsFile, Encoding.UTF8)` (no behavioral change — ASCII-only — but applied for consistency)

2. **File write paths**: Add `Utf8NoBom` as the encoding argument to every encoding-bare write.
   - `fSyncOtherTool.cs` (`SafeWriteConfig`): `File.WriteAllText(ConfigFileName, content ?? "", Utf8NoBom)`
   - `fInputWifiCredentials.cs`: `File.WriteAllText(ConfigPath, content ?? "", Utf8NoBom)`
   - `TableDensity.cs`: `File.WriteAllText(_settingsFile, mode.ToString(), Utf8NoBom)`

3. **JSON persistence**: In `fPhoneSetupOptions.cs`, read/write the JSON file with explicit UTF-8 (`File.ReadAllText(ConfigPath, Encoding.UTF8)` / `File.WriteAllText(ConfigPath, json, Utf8NoBom)`). `System.Text.Json` already emits UTF-8 internally; enforce it at the file boundary.

4. **Logging path**: In `Program.cs` crash logger, change `File.AppendAllText(file, text)` to `File.AppendAllText(file, text, Utf8NoBom)`.

5. **Files corrupted at rest**: For any scanned content file identified as non-UTF-8 with corrupted Vietnamese literals, re-save it as UTF-8 without BOM with the Vietnamese text restored to its correct form. Leave already-UTF-8 and ASCII-only files untouched.

6. **Console / child-process output**: Confirm the existing `Console.OutputEncoding = Encoding.UTF8` setup in `Program.cs` remains, and ensure any `ProcessStartInfo` reading child stdout sets `StandardOutputEncoding = Encoding.UTF8` where Vietnamese output is expected.

## Testing Strategy

### Validation Approach

The testing strategy follows a two-phase approach: first, surface counterexamples that demonstrate the bug on the unfixed code, then verify the fix works correctly (fix checking) and preserves existing behavior (preservation checking).

### Exploratory Bug Condition Checking

**Goal**: Surface counterexamples that demonstrate the bug BEFORE implementing the fix. Confirm or refute the root cause analysis. If we refute it, we re-hypothesize.

**Test Plan**: Write tests that exercise each encoding boundary with Vietnamese content, then assert the round-tripped/rendered text matches the original. Run them against the UNFIXED code (using the default code page) to observe corruption and confirm the root cause.

**Test Cases**:
1. **File read counterexample**: Write a UTF-8-without-BOM file containing `Thiết bị không có internet`, read it via the unfixed `File.ReadAllLines`/`ReadAllText` path, assert the result equals the original (will fail on unfixed code).
2. **File write counterexample**: Write `Vui lòng tự cấu hình` via the unfixed `File.WriteAllText` path, read the bytes back as UTF-8, assert they decode to the original (will fail on unfixed code).
3. **JSON round-trip counterexample**: Serialize a `PhoneSetupOptions`-like object with a Vietnamese field, persist and reload via the unfixed path, assert the field is unchanged (will fail on unfixed code).
4. **Log append counterexample**: Append a Vietnamese log line via the unfixed `File.AppendAllText`, read the log as UTF-8, assert it matches (will fail on unfixed code).
5. **File-at-rest edge case**: Provide a fixture file saved in Windows-1258 with corrupted literals, assert the scan flags it as `containsMojibake` (may fail/needs the scan to exist).

**Expected Counterexamples**:
- Round-tripped Vietnamese text contains `�` or `?`-substituted diacritics instead of the original characters.
- Possible causes: read decode using default code page, write encode using default code page, file corrupted at rest.

### Fix Checking

**Goal**: Verify that for all inputs where the bug condition holds, the fixed function produces the expected behavior.

**Pseudocode:**
```
FOR ALL X WHERE isBugCondition(X) DO
  result := F_fixed(X)
  ASSERT isUtf8Handled(result)
     AND containsNoMojibake(result)
     AND renderedText(result) = intendedVietnameseText(X)
END FOR
```

### Preservation Checking

**Goal**: Verify that for all inputs where the bug condition does NOT hold, the fixed function produces the same result as the original function.

**Pseudocode:**
```
FOR ALL X WHERE NOT isBugCondition(X) DO
  ASSERT F_original(X) = F_fixed(X)
END FOR
```

**Testing Approach**: Property-based testing is recommended for preservation checking because:
- It generates many test cases automatically across the input domain (random ASCII strings, random already-valid UTF-8 strings, random byte buffers).
- It catches edge cases that manual unit tests might miss (empty strings, whitespace-only, mixed scripts, emoji).
- It provides strong guarantees that behavior is unchanged for all non-buggy inputs.

**Test Plan**: Observe behavior on the UNFIXED code for ASCII-only and already-UTF-8 content, then write property-based tests capturing that behavior and assert it is identical after the fix.

**Test Cases**:
1. **ASCII preservation**: Observe that ASCII-only files (e.g. `TableDensity` `"Normal"`) read/write identically on unfixed code, then assert the fixed code produces byte-identical results.
2. **Already-UTF-8 preservation**: Observe that files already written with `Encoding.UTF8` (e.g. via `ucManagerDevices`, `GridMetrics`, `AccountGridPerf`) round-trip identically, then assert the fix introduces no change.
3. **Binary file preservation**: Assert binary/non-text files and `obj/`+`bin/` artifacts are neither scanned nor re-encoded.
4. **Non-Vietnamese Unicode preservation**: Generate strings containing other-language scripts/emoji/symbols and assert read/write is unchanged.

### Unit Tests

- UTF-8 read decoding for each affected read site (returns original Vietnamese text).
- UTF-8-without-BOM write encoding for each affected write site (no BOM bytes, decodes back to original).
- JSON serialize/deserialize round trip preserving Vietnamese fields.
- Log append produces UTF-8 bytes for Vietnamese content.
- Edge cases: empty file, ASCII-only file, file with BOM, file corrupted at rest.

### Property-Based Tests

- Generate random Vietnamese / non-ASCII strings; assert write-then-read round trip equals the original (fix checking).
- Generate random ASCII and already-valid UTF-8 strings; assert fixed and original behavior are identical (preservation checking).
- Generate random byte buffers representing binary content; assert they are left untouched by the scan/re-encode step.

### Integration Tests

- Full config save/load flow (`fSyncOtherTool`, `fInputWifiCredentials`, `fPhoneSetupOptions`) with Vietnamese values, asserting values survive a persist/reload cycle.
- Crash-logging flow with a Vietnamese exception message, asserting the persisted log renders correctly as UTF-8.
- Child-process stdout flow producing Vietnamese status text, asserting console/output encoding emits it without `?` substitution.
