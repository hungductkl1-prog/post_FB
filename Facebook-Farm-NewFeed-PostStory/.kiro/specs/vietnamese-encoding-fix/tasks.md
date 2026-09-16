# Implementation Plan

## Overview

This plan fixes Vietnamese text mojibake in the .NET 9 WinForms application by enforcing UTF-8 (without BOM) at every string-I/O boundary that currently relies on the platform default code page, and by re-encoding content files corrupted at rest. It follows the bugfix two-phase testing strategy: an exploratory bug-condition test written first (fails on unfixed code), preservation property tests written first (pass on unfixed code), then the minimal targeted fix, followed by fix checking and preservation checking.

## Task Dependency Graph

- Task 1 (bug condition exploration test) and Task 2 (preservation property tests) come first and are independent of each other; both run against the UNFIXED code.
- Task 3 (the fix) depends on Tasks 1 and 2 being written and run.
  - Sub-tasks 3.1 → 3.2–3.7 (3.1 defines the shared encoding used by the write sites; 3.2–3.7 are otherwise independent of each other).
  - Sub-task 3.8 depends on the fix sub-tasks (3.1–3.7) and re-runs the Task 1 test.
  - Sub-task 3.9 depends on the fix sub-tasks (3.1–3.7) and re-runs the Task 2 tests.
- Task 4 (checkpoint) depends on all prior tasks.

```json
{
  "waves": [
    { "wave": 1, "tasks": ["1", "2"] },
    { "wave": 2, "tasks": ["3.1"] },
    { "wave": 3, "tasks": ["3.2", "3.3", "3.4", "3.5", "3.6", "3.7"] },
    { "wave": 4, "tasks": ["3.8", "3.9"] },
    { "wave": 5, "tasks": ["4"] }
  ]
}
```

## Tasks

- [x] 1. Write bug condition exploration test (BEFORE implementing the fix)
  - **Property 1: Bug Condition** - Vietnamese Text Round-Trips Correctly Under UTF-8
  - **CRITICAL**: This test MUST FAIL on unfixed code - failure confirms the bug exists
  - **DO NOT attempt to fix the test or the code when it fails** - failure is the expected and correct outcome at this stage
  - **NOTE**: This test encodes the expected behavior - it will validate the fix when it passes after implementation
  - **GOAL**: Surface counterexamples that demonstrate the bug exists and confirm the root-cause hypothesis (string-I/O calls omitting explicit UTF-8). If counterexamples do not appear, re-hypothesize the root cause.
  - **Scoped PBT Approach**: For deterministic boundaries, scope the property to concrete failing cases (e.g. the string `Thiết bị không có internet`) for reproducibility, then generalize over generated Vietnamese / non-ASCII strings
  - Set up a test project if none exists (xUnit + FsCheck or CsCheck for property-based testing), referencing the application assembly
  - Encode the bug condition from the design: a ContentUnit carries Vietnamese / non-ASCII UTF-8 text AND is handled through a non-UTF-8 encoding (default code page), so `isBugCondition(X)` is true
  - Test implementation details from the Bug Condition / Examples in design:
    - **File read counterexample**: write a UTF-8-without-BOM file containing `Thiết bị không có internet`, read it through the unfixed `File.ReadAllLines`/`File.ReadAllText` path (default code page, as in `ucdgvAccount.cs`, `fSelectByUid.cs`), assert the result equals the original
    - **File write counterexample**: write `Vui lòng tự cấu hình` through the unfixed `File.WriteAllText` path (`fSyncOtherTool.cs`, `fInputWifiCredentials.cs`), read the bytes back as UTF-8, assert they decode to the original
    - **JSON round-trip counterexample**: serialize a `PhoneSetupOptions`-like object with a Vietnamese field, persist/reload via the unfixed `fPhoneSetupOptions.cs` path, assert the field is unchanged
    - **Log append counterexample**: append a Vietnamese log line via the unfixed `File.AppendAllText` path (`Program.cs`), read the log as UTF-8, assert it matches
    - **File-at-rest edge case**: provide a fixture saved in Windows-1258 with corrupted literals, assert the scan flags it as `containsMojibake`
  - The test assertions should match the Expected Behavior Properties from design: `isUtf8Handled(result) AND containsNoMojibake(result) AND renderedText(result) = intendedVietnameseText(X)`
  - Run test on UNFIXED code
  - **EXPECTED OUTCOME**: Test FAILS (this is correct - it proves the bug exists; round-tripped Vietnamese contains `�` or `?`-substituted diacritics)
  - Document counterexamples found to understand the root cause (e.g. "`File.ReadAllLines` of a UTF-8 file decoded `Thiết bị` as `Thi?t b?`")
  - Mark task complete when test is written, run, and failure is documented
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.8, 1.9, 2.1, 2.2, 2.3, 2.4, 2.5, 2.7, 2.8, 2.9_

- [ ] 2. Write preservation property tests (BEFORE implementing the fix)
  - **Property 2: Preservation** - Non-Buggy Content Behavior Unchanged
  - **IMPORTANT**: Follow the observation-first methodology - record real behavior on the UNFIXED code, do not assume it
  - Encode the non-bug condition from the design: ContentUnits where `isBugCondition(X)` is false (ASCII-only content, already-correct UTF-8 content, already-explicit UTF-8 handling, binary/non-text files, valid non-Vietnamese Unicode, non-encoding logic)
  - Observe behavior on UNFIXED code for non-buggy inputs and record the actual outputs:
    - **ASCII preservation**: observe that ASCII-only files (e.g. `TableDensity` writing `"Normal"`) read/write identically on unfixed code
    - **Already-UTF-8 preservation**: observe that files already written with `Encoding.UTF8` (`ucManagerDevices`, `GridMetrics`, `AccountGridPerf`) round-trip identically
    - **Binary file preservation**: observe that binary/non-text files and `obj/`+`bin/` artifacts are neither scanned nor re-encoded
    - **Non-Vietnamese Unicode preservation**: observe read/write behavior for other-language scripts/emoji/symbols
  - Write property-based tests capturing the observed behavior patterns from the Preservation Requirements:
    - Generate random ASCII strings; assert write-then-read is byte-identical between original and fixed behavior
    - Generate random already-valid UTF-8 strings; assert behavior is unchanged
    - Generate random byte buffers representing binary content; assert they are left untouched by the scan/re-encode step
  - Property-based testing generates many test cases for stronger preservation guarantees (empty strings, whitespace-only, mixed scripts, emoji)
  - Run tests on UNFIXED code
  - **EXPECTED OUTCOME**: Tests PASS (this confirms the baseline behavior to preserve)
  - Mark task complete when tests are written, run, and passing on unfixed code
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

- [ ] 3. Fix for Vietnamese text mojibake at UTF-8 encoding boundaries

  - [ ] 3.1 Define a shared UTF-8-without-BOM encoding convention
    - Add `private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);` (or a shared helper) for use across the affected write sites
    - UTF-8 without BOM is byte-compatible with ASCII and preserves Vietnamese diacritics
    - _Bug_Condition: isBugCondition(X) where containsVietnameseOrNonAscii(X) AND handledWithNonUtf8Encoding(X)_
    - _Expected_Behavior: expectedBehavior(result) — isUtf8Handled(result) from design_
    - _Preservation: Preservation Requirements from design (UTF-8 byte-compatible with ASCII)_
    - _Requirements: 2.2, 2.3, 3.1, 3.3_

  - [ ] 3.2 Add explicit UTF-8 to all encoding-bare file read paths
    - `ucdgvAccount.cs`: `File.ReadAllLines(fileGmail, Encoding.UTF8)`
    - `fViewDataGridView.cs`: `File.ReadAllLines(configFile, Encoding.UTF8)`
    - `fUpdateData.cs`: `File.ReadAllLines($"{NameJson}.txt", Encoding.UTF8)`
    - `fSyncOtherTool.cs` (`SafeReadConfig`): `File.ReadAllText(ConfigFileName, Encoding.UTF8)`
    - `fSelectMultiFolder.cs`: `File.ReadAllLines(folderPath, Encoding.UTF8)`
    - `fSelectByUid.cs`: `File.ReadAllText(uidPath, Encoding.UTF8)`
    - `fInputWifiCredentials.cs` (`SafeReadConfig`): `File.ReadAllText(ConfigPath, Encoding.UTF8)`
    - `TableDensity.cs`: `File.ReadAllText(_settingsFile, Encoding.UTF8)` (no behavioral change — ASCII-only — applied for consistency)
    - _Bug_Condition: isBugCondition(X) on the read path — UTF-8 bytes decoded via default code page_
    - _Expected_Behavior: expectedBehavior(result) — reads decode UTF-8 to the original text_
    - _Preservation: ASCII-only and already-UTF-8 reads remain identical_
    - _Requirements: 1.2, 2.2, 3.1, 3.2_

  - [ ] 3.3 Add explicit UTF-8-without-BOM to all encoding-bare file write paths
    - `fSyncOtherTool.cs` (`SafeWriteConfig`): `File.WriteAllText(ConfigFileName, content ?? "", Utf8NoBom)`
    - `fInputWifiCredentials.cs`: `File.WriteAllText(ConfigPath, content ?? "", Utf8NoBom)`
    - `TableDensity.cs`: `File.WriteAllText(_settingsFile, mode.ToString(), Utf8NoBom)`
    - _Bug_Condition: isBugCondition(X) on the write path — text encoded via default code page_
    - _Expected_Behavior: expectedBehavior(result) — writes encode as UTF-8 without BOM, bytes decode back to original_
    - _Preservation: ASCII-only writes produce byte-identical output (no BOM)_
    - _Requirements: 1.3, 2.3, 3.1, 3.3_

  - [ ] 3.4 Enforce UTF-8 at the JSON persistence boundary
    - In `fPhoneSetupOptions.cs`, read/write the config JSON with explicit UTF-8: `File.ReadAllText(ConfigPath, Encoding.UTF8)` / `File.WriteAllText(ConfigPath, json, Utf8NoBom)`
    - `System.Text.Json` already emits UTF-8 internally; enforce it at the file boundary
    - _Bug_Condition: isBugCondition(X) — JSON round trip without UTF-8 enforcement corrupts Vietnamese fields_
    - _Expected_Behavior: expectedBehavior(result) — serialize then deserialize yields the original Vietnamese value_
    - _Preservation: non-Vietnamese / ASCII JSON fields unchanged_
    - _Requirements: 1.4, 2.4, 3.5_

  - [ ] 3.5 Enforce UTF-8 on the logging path
    - In `Program.cs` crash logger, change `File.AppendAllText(file, text)` to `File.AppendAllText(file, text, Utf8NoBom)`
    - _Bug_Condition: isBugCondition(X) — Vietnamese log text appended via default code page_
    - _Expected_Behavior: expectedBehavior(result) — log entries are UTF-8 and match the original text_
    - _Preservation: ASCII log entries unchanged_
    - _Requirements: 1.5, 2.5_

  - [ ] 3.6 Re-encode content files that are corrupted at rest
    - Scan source/content files (`*.cs`, `*.xaml`, `*.resx`, `*.json`, `*.config`, `*.txt`, `*.md`) for non-UTF-8 encoding and corrupted Vietnamese literals (replacement chars, `?`-substituted diacritics, broken Unicode), and report each location
    - Re-save any flagged file as UTF-8 without BOM with the Vietnamese text restored to its correct form
    - Leave already-UTF-8 and ASCII-only files untouched; do NOT scan or re-encode binary/non-text files or `obj/`+`bin/` artifacts
    - _Bug_Condition: isBugCondition(X) via containsMojibake — file stored at rest in non-UTF-8_
    - _Expected_Behavior: expectedBehavior(result) — file re-encoded to UTF-8 without BOM with Vietnamese restored_
    - _Preservation: binary/non-text files and build artifacts untouched; already-UTF-8/ASCII files unchanged_
    - _Requirements: 1.1, 1.9, 2.1, 2.9, 3.2, 3.4_

  - [ ] 3.7 Confirm console / child-process output encoding
    - Confirm the existing `Console.OutputEncoding = Encoding.UTF8` / `InputEncoding` setup and code-pages provider registration in `Program.cs` remain in place
    - Ensure any `ProcessStartInfo` reading child stdout that may carry Vietnamese sets `StandardOutputEncoding = Encoding.UTF8`
    - _Bug_Condition: isBugCondition(X) — Vietnamese console output without UTF-8 encoding_
    - _Expected_Behavior: expectedBehavior(result) — console emits Vietnamese without `?` substitution_
    - _Preservation: existing already-correct console setup behaves identically_
    - _Requirements: 1.8, 2.8, 3.3, 3.6_

  - [ ] 3.8 Verify bug condition exploration test now passes
    - **Property 1: Expected Behavior** - Vietnamese Text Round-Trips Correctly Under UTF-8
    - **IMPORTANT**: Re-run the SAME test from task 1 - do NOT write a new test
    - The test from task 1 encodes the expected behavior; when it passes it confirms the expected behavior is satisfied
    - Run the bug condition exploration test from step 1
    - **EXPECTED OUTCOME**: Test PASSES (confirms the bug is fixed and `renderedText(result) = intendedVietnameseText(X)` with no mojibake)
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.7, 2.8, 2.9 (Expected Behavior Properties from design)_

  - [ ] 3.9 Verify preservation tests still pass
    - **Property 2: Preservation** - Non-Buggy Content Behavior Unchanged
    - **IMPORTANT**: Re-run the SAME tests from task 2 - do NOT write new tests
    - Run the preservation property tests from step 2
    - **EXPECTED OUTCOME**: Tests PASS (confirms no regressions for ASCII-only, already-UTF-8, binary, and non-Vietnamese Unicode content)
    - Confirm all tests still pass after the fix (no regressions)
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

- [ ] 4. Checkpoint - Ensure all tests pass
  - Run the full test suite (exploration/fix-checking test from task 1, preservation tests from task 2, plus any unit/integration tests)
  - Build the .NET 9 project and confirm it compiles with no errors
  - Confirm fix checking passes (Vietnamese round-trips correctly under UTF-8) and preservation checking passes (non-buggy content unchanged)
  - Ensure all tests pass; ask the user if questions arise

## Notes

- Tasks 1 and 2 MUST be written and run against the UNFIXED code first: Task 1 is expected to FAIL (confirming the bug), Task 2 is expected to PASS (capturing baseline behavior to preserve).
- A test project is required (none exists yet). Use xUnit with a property-based testing library (FsCheck or CsCheck) referencing the application assembly.
- The fix is deliberately minimal: it only changes encoding arguments on I/O calls and re-encodes files corrupted at rest. It must not alter application logic, control flow, layout, or any non-encoding behavior.
- Use `new UTF8Encoding(false)` (UTF-8 without BOM) for write paths; `Encoding.UTF8` is acceptable for read decoding.
- Do NOT scan or re-encode binary/non-text files or build artifacts under `obj/` and `bin/`.
- Recommend running long-running test/watch commands manually; use a single-run invocation (e.g. `dotnet test`) rather than watch mode.
