# HttpRequestLib Build Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ensure building `LamToolAutoPhonePrime` also produces and copies `HttpRequestLib.dll` for all supported configurations.

**Architecture:** Add a solution-level dependency for build order, then add focused MSBuild targets in the app project to resolve, optionally build, and copy the native DLL. This stays localized to build configuration files and avoids code changes in runtime callers.

**Tech Stack:** Visual Studio solution metadata, SDK-style MSBuild project configuration, native VC++ project build via `MSBuild.exe`

---

### Task 1: Wire Build Order And Native DLL Copy

**Files:**
- Modify: `LamToolAutoPhonePrime.sln`
- Modify: `LamToolAutoPhonePrime/LamToolAutoPhonePrime.csproj`

- [ ] **Step 1: Add the solution dependency**

Update the `LamToolAutoPhonePrime` project entry in the solution so it depends on `HttpRequestLib`.

- [ ] **Step 2: Add managed-to-native platform mapping**

Define shared properties in [`LamToolAutoPhonePrime.csproj`](E:\LamToolAutoPhonePrime\LamToolAutoPhonePrime\LamToolAutoPhonePrime.csproj) for:
- native platform selection
- native project path
- native DLL candidate paths
- `vswhere.exe` discovery

- [ ] **Step 3: Add fallback native build target**

Add an MSBuild target that:
- discovers Visual Studio `MSBuild.exe` through `vswhere`
- invokes the native project build only when the DLL is missing
- fails clearly if the DLL still cannot be produced

- [ ] **Step 4: Add copy target**

Add an MSBuild target that copies the resolved `HttpRequestLib.dll` into `$(OutDir)` with `SkipUnchangedFiles="true"`.

### Task 2: Verify The Build Wiring

**Files:**
- Verify: `LamToolAutoPhonePrime/LamToolAutoPhonePrime.csproj`
- Verify: `LamToolAutoPhonePrime.sln`

- [ ] **Step 1: Run XML-safe project evaluation**

Run a project evaluation command to ensure the edited SDK-style project still loads.

- [ ] **Step 2: Run the native-copy target**

Run the app project through MSBuild and verify the new target resolves or copies `HttpRequestLib.dll`.

- [ ] **Step 3: Record blockers accurately**

If local verification is blocked by missing Visual C++ toolsets, report that explicitly instead of claiming a full successful build.
