# BMTP3 Backup Core — Compact AI Context

## Purpose

This file is the compact AI context for BMTP3 backup-core development.

Use this file first.
Only use the long audit file `Backup_Pipeline_Comparison_003.md` when detailed class/file comparisons are needed.

---

## Core Roles

### Original Core

Role: functional truth / feature reference.

Original Core historically worked end-to-end and had the important backup features:

```text
MTP discovery
file enumeration
download/staging
EXIF/metadata extraction
hashing
binary compare collision handling
INI sidecar generation
resume persistence
cleanup
```

Use Original Core to answer:

```text
What must the backup program be able to do?
```

Do not copy its architecture.

Problem:

```text
monolithic
tightly coupled
hard to test
hard to safely refactor
large classes with too much responsibility
```

---

### Core2

Role: component and idea bank.

Core2 contains useful ideas:

```text
MTP gatekeeper
GatekeptStream
content abstraction
generic traversal
glob filtering
session lifecycle
pipeline concepts
```

Use Core2 to answer:

```text
Is there a useful component or pattern we can adapt carefully?
```

Do not copy Core2's architecture blindly.

Problem:

```text
too complex too early
async/channel/multithreaded pipeline
highly generic design
never became stable enough
```

Rule:

```text
Core4 must not become Core2 again.
```

---

### Core3

Role: warning.

Core3 was a quick single-threaded attempt but never worked end-to-end.

Lesson:

```text
Single-threaded first is correct, but quick half-implementation is not enough.
```

Do not use Core3 as target architecture.

---

### Core4

Role: new production base.

Core4 must become the clean, stable replacement for Original Core.

Core4 must be:

```text
clean
maintainable
testable
single-threaded first
feature-by-feature
small verified steps
```

Core4 must not:

```text
become monolithic like Original Core
become channel/multithreaded like Core2
repeat Core3's half-finished shortcut style
introduce parallelism before sequential flow works
```

---

## Core4 Guiding Rule

Always follow this rule:

```text
Core4 must first become a correct, stable, feature-complete, single-threaded backup engine.
Only after the sequential engine works end-to-end should parallelism, runners, channels, or advanced performance design be considered.
```

Priority principles:

```text
Correctness before performance.
Feature completeness before parallelism.
Maintainability before cleverness.
A working vertical slice before advanced generalization.
```

---

## Current Core4 State

Core4 has good architecture:

```text
DI with TryAdd
interfaces
sequential BackupEngine
filesystem traversal
scanner mapping
download/staging
timestamp resolution
single-pass multi-hash
sidecar generation
session state
JSON persistence
```

But Core4 is not yet a working replacement.

Current critical blocker:

```text
BackupPlanValidator blocks all valid plans.
```

The validator has contradictory checks around hash algorithms and feature gating.

Until this is fixed, full engine flow cannot run.

---

## Current Priority Order

Do this order:

```text
1. Fix BackupPlanValidator so a minimal valid filesystem plan can reach the engine.
2. Verify filesystem traversal + scanner through the engine.
3. Verify session save/persistence after run.
4. Add timestamp correction on destination files.
5. Fix MoveableFileContent.MoveTo overwrite bug.
6. Add binary compare for collision handling.
7. Add post-write verification.
8. Improve sidecar parity with Original Core.
9. Add MTP support later.
10. Consider parallelism much later, only if needed.
```

Do not jump to MTP yet.
Do not jump to parallelism yet.

---

## Known Core4 Issues

### Critical

```text
C4: BackupPlanValidator blocks all plans.
```

### Important

```text
I1: Destination file timestamps are not corrected.
I2: Binary compare missing in collision handling.
I3: Post-write verification missing.
I4: MoveableFileContent.MoveTo ignores overwrite parameter.
I5: Sidecar does not yet match Original Core metadata richness.
```

### Future

```text
MTP support missing.
JSON sidecar missing.
Parallel runners exist but should not be wired yet.
```

---

## MTP Rule

MTP is required eventually because Original Core supported it.

But MTP is not next.

When MTP is implemented, adapt only the useful Core2 ideas:

```text
IMtpGatekeeper
MtpGatekeeper
GatekeptStream
MediaFileContent
MTP session lifecycle
safe enumeration
```

Do not port the full Core2 channel pipeline.

Preferred MTP strategy for Core4:

```text
Open MTP session.
Scan source.
Download/stage content while session is alive.
Disconnect device.
Continue hashing/move/sidecar on local temp files.
```

---

## Path Rule

Never write user-specific absolute paths.

Do not write:

```text
C:\Users\<user>\source\practice\BMTP3_CS\libs\MediaDevices.dll
```

Write repository-relative paths:

```text
BMTP3_CS\libs\MediaDevices.dll
```

For project references use:

```xml
<Reference Include="MediaDevices">
  <HintPath>..\libs\MediaDevices.dll</HintPath>
</Reference>
```

---

## AI Working Style

When helping with Core4:

```text
Give one safest next step first.
Prefer small changes.
Do not suggest broad rewrites.
Do not add abstractions before needed.
Do not introduce parallelism early.
Do not hide errors silently.
Do not assume missing Core4 features never existed.
Use Original Core as feature reference.
Use Core2 only as selective idea bank.
Use Core3 as warning.
Keep Core4 simple and sequential first.
```

---

## Definition of Done for Core4

Core4 is a real replacement only when it can:

```text
run filesystem backup end-to-end
resume correctly after restart
preserve/correct timestamps
generate useful sidecar metadata
handle collisions safely
avoid unnecessary duplicates
verify written files
eventually support MTP
remain maintainable and non-monolithic
```

---

## Lookup Rules for AI

Use this compact file for strategy and next-step decisions.

Use `Backup_Pipeline_Comparison_003.md` only when detailed facts are needed about:

```text
class names
file names
pipeline comparisons
MTP implementation details
DI registration
sidecar structure
hash implementation
traversal implementation
```

Do not load or summarize the long audit unless the task requires those details.

---

## One-Sentence Summary

Core4 is a disciplined rebuild of a working but monolithic backup engine: use Original Core as the feature reference, Core2 as a selective idea bank, Core3 as a warning, and build Core4 single-threaded, cleanly, and one verified feature at a time.
