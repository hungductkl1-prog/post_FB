# HttpRequestLib Build Integration Design

**Goal:** When `LamToolAutoPhonePrime` is built or started in Visual Studio, `HttpRequestLib` should also be built as needed and `HttpRequestLib.dll` should be present in the app output directory for every configuration.

**Context:** `LamToolAutoPhonePrime` loads `HttpRequestLib.dll` through `DllImport`, but [`LamToolAutoPhonePrime.csproj`](E:\LamToolAutoPhonePrime\LamToolAutoPhonePrime\LamToolAutoPhonePrime.csproj) does not currently ensure the native DLL is built or copied. The solution already contains the native C++ project, so solution-level builds can compile it, but the app project still needs explicit coordination and copy behavior.

## Decision

Use a hybrid integration:

1. Add a solution dependency from `LamToolAutoPhonePrime` to `HttpRequestLib` so Visual Studio project builds and full solution builds respect native-first build order.
2. Add MSBuild targets in [`LamToolAutoPhonePrime.csproj`](E:\LamToolAutoPhonePrime\LamToolAutoPhonePrime\LamToolAutoPhonePrime.csproj) to:
   - map managed platforms to native platforms (`AnyCPU`/`x64` -> `x64`, `x86` -> `Win32`)
   - locate the expected native DLL output
   - fall back to invoking Visual Studio `MSBuild.exe` for the native project if the DLL is missing
   - copy `HttpRequestLib.dll` into `$(OutDir)` before the app build finishes

## Build Flow

1. `LamToolAutoPhonePrime` starts building.
2. The solution dependency builds `HttpRequestLib` first when the build is driven by Visual Studio or the solution.
3. The app project target resolves the expected native DLL path.
4. If the DLL is missing, the app project invokes `MSBuild.exe` for [`HttpRequestLib.vcxproj`](E:\LamToolAutoPhonePrime\HttpRequestLib\HttpRequestLib\HttpRequestLib.vcxproj) as a fallback.
5. The app project copies the resolved DLL into the app output directory.
6. If the native DLL still cannot be found, the build fails with a clear error instead of producing a broken app output.

## Error Handling

- Missing Visual Studio MSBuild path: fail with an explicit error message.
- Missing native DLL after fallback build: fail with an explicit error message.
- Unsupported platform values: default to native `x64` unless the managed build explicitly requests `x86`.

## Verification

- Build the app project with Visual Studio MSBuild for `Debug|x64`.
- Run the new target path in isolation and confirm `HttpRequestLib.dll` is copied into the app output directory.
- Note any environment blockers separately if the local machine cannot compile the native project because of missing VC toolsets.
