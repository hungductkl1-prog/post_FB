# SYSTEM AGENT RULES

## PRIMARY OBJECTIVE

Deliver working code.

Code first.
Verify second.
Explain third.

Avoid long discussions when implementation is possible.

---

# PROJECT DISCOVERY (MANDATORY)

Before making any change:

1. Scan repository structure.
2. Detect:

   * Solution files (*.sln)
   * Project files (*.csproj)
   * Build scripts
   * Docker files
   * CI/CD files
   * Configuration files
3. Determine:

   * Programming language
   * Framework version
   * Architecture pattern
   * Dependency injection approach
   * Database technology
   * UI technology

Generate a mental project map before coding.

Never assume architecture.

---

# IMPLEMENTATION WORKFLOW

For every task:

STEP 1
Read relevant source files.

STEP 2
Locate actual implementation.

STEP 3
Identify impact scope.

STEP 4
Implement code.

STEP 5
Build project.

STEP 6
Fix compilation issues.

STEP 7
Report result.

Do not stop at analysis.

---

# BUILD VALIDATION

Every change must:

* Compile successfully
* Preserve existing features
* Avoid introducing warnings
* Follow current coding style

If build cannot be executed:

Explain why.

---

# PERFORMANCE RULES

Always prioritize:

1. UI responsiveness
2. Memory efficiency
3. Thread safety
4. Scalability

Never block UI thread.

Use:

* async/await
* background workers
* task scheduling
* batching
* virtualization

where applicable.

---

# LARGE DATASET RULES

If dataset > 10,000 rows:

Prefer:

* VirtualMode
* Lazy loading
* Incremental rendering
* Background loading
* Cache reuse

Avoid:

* Full refresh
* UI freezes
* Large allocations

---

# FILE MODIFICATION POLICY

Modify existing code before creating new abstractions.

Prefer minimal diff.

Do not refactor unrelated modules.

---

# DEBUG POLICY

For bugs:

1. Reproduce
2. Locate root cause
3. Implement fix
4. Verify fix
5. Check regressions

Never submit analysis without code changes.

---

# HANDOFF POLICY

Always read:

SKILL/handoff.md

before starting work.

Always update:

SKILL/handoff.md

after completing work.

---

# RESPONSE FORMAT

## Completed

What was implemented.

## Files Changed

Modified files.

## Verification

Build/Test status.

## Next Task

Recommended next action.

Keep responses concise.
