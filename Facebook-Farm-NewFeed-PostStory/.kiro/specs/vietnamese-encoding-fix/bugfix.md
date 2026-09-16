# Bugfix Requirements Document

## Introduction

Vietnamese text throughout the .NET 9 WinForms application (`Facebook-Farm-NewFeed-PostStory`) is rendered as corrupted output (mojibake). Strings that should read `Thiết bị không có internet! Vui lòng tự cấu hình internet cho thiết bị` instead appear as `Thi?t b? kh�ng c� internet! Vui l�ng t? c?u h�nh internet cho thi?t b?`.

The corruption shows up as replacement characters (`�`), question marks (`?`) replacing diacritics, and Vietnamese text that has lost its Unicode diacritical marks. The root cause is inconsistent encoding handling: UTF-8 source content being read or written through ANSI / Windows-1252 / Windows-1258 code pages instead of UTF-8, and string I/O paths (file read/write, JSON, logging, database, API responses, console output) that omit an explicit `Encoding.UTF8` / `new UTF8Encoding(false)`.

This bug affects user-visible labels, notifications, logs, persisted config, and any persisted or transmitted Vietnamese content. The goal is to fix the corruption at its source across all I/O boundaries while preserving the behavior of all correctly-encoded, non-Vietnamese, and already-UTF-8 content.

## Bug Analysis

### Current Behavior (Defect)

The system currently mishandles Vietnamese (UTF-8) text at encoding boundaries, producing corrupted output.

1.1 WHEN a source or content file (`*.cs`, `*.xaml`, `*.resx`, `*.json`, `*.config`, `*.txt`, `*.md`) containing UTF-8 Vietnamese text is interpreted using a non-UTF-8 encoding (ANSI, Windows-1252, or Windows-1258) THEN the system renders the diacritical characters as `�` or `?`

1.2 WHEN text is read via `StreamReader`, `File.ReadAllText`, or `File.ReadAllLines` without an explicit UTF-8 encoding and the underlying byte stream is UTF-8 without BOM THEN the system may decode it using the platform default code page, corrupting Vietnamese diacritics

1.3 WHEN text is written via `StreamWriter`, `File.WriteAllText`, or `File.AppendAllText` without an explicit UTF-8 encoding THEN the system persists Vietnamese content using the platform default code page, producing bytes that later decode as mojibake

1.4 WHEN Vietnamese content is serialized or deserialized as JSON without UTF-8 enforcement THEN the round-tripped text loses or corrupts its diacritical marks

1.5 WHEN Vietnamese content is written through the logging path (e.g. `LogHelper` / append-to-log helpers) without UTF-8 encoding THEN log entries contain corrupted Vietnamese text

1.6 WHEN Vietnamese content is read from or written to the database without UTF-8-consistent handling THEN stored or retrieved values are corrupted

1.7 WHEN an API response containing Vietnamese text is read without honoring its UTF-8 charset THEN the parsed string is corrupted

1.8 WHEN Vietnamese text is sent to standard output / console (including child-process stdout) without UTF-8 console encoding THEN diacritic characters are replaced with `?`

1.9 WHEN a source/content file is saved on disk in a non-UTF-8 encoding THEN the literal Vietnamese strings stored in that file are already corrupted at rest

### Expected Behavior (Correct)

For the same conditions, the system should consistently use UTF-8 so Vietnamese text is preserved exactly.

2.1 WHEN a scan is run over the source/content files (`*.cs`, `*.xaml`, `*.resx`, `*.json`, `*.config`, `*.txt`, `*.md`) THEN the system SHALL identify every occurrence of corrupted Vietnamese text (replacement characters, `?`-substituted diacritics, broken Unicode, or non-UTF-8 file encoding) and report its location

2.2 WHEN UTF-8 Vietnamese content is read via `StreamReader`, `File.ReadAllText`, or `File.ReadAllLines` THEN the system SHALL decode it using UTF-8 and return the original Vietnamese text intact

2.3 WHEN Vietnamese content is written via `StreamWriter`, `File.WriteAllText`, or `File.AppendAllText` THEN the system SHALL encode it as UTF-8 without BOM (`new UTF8Encoding(false)`) and produce bytes that decode back to the original text

2.4 WHEN Vietnamese content is serialized or deserialized as JSON THEN the system SHALL preserve Vietnamese characters exactly across a round trip (`serialize` then `deserialize` yields the original value)

2.5 WHEN Vietnamese content is written through the logging path THEN the system SHALL write log entries as UTF-8 so the persisted text matches the original

2.6 WHEN Vietnamese content is read from or written to the database THEN the system SHALL handle it as UTF-8 so a write-then-read round trip preserves the original text

2.7 WHEN an API response containing UTF-8 Vietnamese text is read THEN the system SHALL decode it as UTF-8 and return the original text intact

2.8 WHEN Vietnamese text is sent to standard output / console THEN the system SHALL configure UTF-8 console encoding so the text is emitted without `?` substitution

2.9 WHEN a source/content file is found to be stored in a non-UTF-8 encoding with corrupted Vietnamese literals THEN the system SHALL re-encode the file to UTF-8 (without BOM) with the Vietnamese text restored to its correct form

### Unchanged Behavior (Regression Prevention)

The fix must not alter behavior for content that is already correct or that does not involve Vietnamese/UTF-8 text.

3.1 WHEN a file or string contains only ASCII characters THEN the system SHALL CONTINUE TO read, write, and render it identically (UTF-8 is byte-compatible with ASCII)

3.2 WHEN a file is already correctly encoded as UTF-8 (with or without BOM) THEN the system SHALL CONTINUE TO read and render its content identically, with no re-encoding-induced changes

3.3 WHEN content already uses explicit UTF-8 handling (e.g. `File.WriteAllText(..., Encoding.UTF8)`, `Console.OutputEncoding = Encoding.UTF8`, `new UTF8Encoding(false)`) THEN the system SHALL CONTINUE TO behave identically

3.4 WHEN binary or non-text files (`.png`, `.exe`, `.dll`, `.resources`, build artifacts under `obj/` and `bin/`) are present THEN the system SHALL CONTINUE TO leave them untouched and SHALL NOT scan or re-encode them

3.5 WHEN non-Vietnamese text containing valid Unicode (e.g. other languages, emoji, symbols) is read or written THEN the system SHALL CONTINUE TO preserve it unchanged

3.6 WHEN existing application logic, control flow, layout, and non-encoding behavior is executed THEN the system SHALL CONTINUE TO function identically, with the change limited to encoding correctness

## Bug Condition Derivation

### Bug Condition Function

`X` represents a content unit: either a file (with its byte content and on-disk encoding) or an I/O operation (the bytes/string crossing an encoding boundary).

```pascal
FUNCTION isBugCondition(X)
  INPUT: X of type ContentUnit  // a text file or a string-I/O operation
  OUTPUT: boolean

  // X is buggy when it carries Vietnamese/UTF-8 text AND is handled
  // through a non-UTF-8 encoding (default code page / ANSI / 1252 / 1258),
  // OR its current rendered form already contains mojibake.
  RETURN containsVietnameseOrNonAscii(X)
         AND ( handledWithNonUtf8Encoding(X)
               OR containsMojibake(X) )   // '�', '?'-substituted diacritics, broken Unicode
END FUNCTION
```

### Property Specification (Fix Checking)

```pascal
// Property: Fix Checking - Vietnamese text round-trips correctly under UTF-8
FOR ALL X WHERE isBugCondition(X) DO
  result ← F'(X)
  ASSERT isUtf8Handled(result)
     AND containsNoMojibake(result)
     AND renderedText(result) = intendedVietnameseText(X)
END FOR
```

### Preservation Goal (Preservation Checking)

```pascal
// Property: Preservation Checking - non-buggy content is unchanged
FOR ALL X WHERE NOT isBugCondition(X) DO
  ASSERT F(X) = F'(X)
END FOR
```

**Key Definitions:**
- **F**: The original code, before encoding correction.
- **F'**: The fixed code, with UTF-8 (`new UTF8Encoding(false)`) enforced across all string-I/O boundaries.
- **Counterexample**: Reading `xml.xml` containing `Thiết bị` with the default code page yields `Thi?t b?` — `isBugCondition` is true and `F` violates the fix property.
