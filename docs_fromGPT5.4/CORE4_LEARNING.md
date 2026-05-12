# Core4 Learning Summary

**Purpose:** Summarize the lessons that shape the current Core4 direction.

**Last Updated:** 2026-05-12

**Status:** Normalized against the current `BMTP3.Core4` skeleton and the updated Core4 documents.

---

## 1. Overall conclusion

Core4 should not be a copy of Core, Core2, or Core3.

It should be a cleaner synthesis:

- keep the useful feature ambition from earlier versions
- keep the clarity of the sequential designs
- reject unsafe complexity
- add performance only where it is safe

The strongest architectural lesson is:

**A small, correct sequential core is worth more than a sophisticated engine that is hard to trust.**

---

## 2. What Core4 learns from Core

### Keep

- broad backup ambition
- real-world source support
- sidecar/value-added backup behavior
- cleanup discipline
- practical user-facing behavior

### Do not repeat

- architectural drift during refactoring
- large legacy surface with unclear ownership
- feature preservation without enough structural clarity

### Core4 consequence

Core4 should preserve useful behavior, but with explicit contracts and a clearer internal structure.

---

## 3. What Core4 learns from Core2

### Keep

- separation of concerns
- DI-friendly structure
- ambition for optional enrichment features
- awareness that filesystem work can benefit from controlled performance improvements

### Do not repeat

- channel-heavy pipeline complexity
- fragile concurrency
- hard-to-debug orchestration
- hidden stage coupling
- device handling that depends on parallel infrastructure

### Core4 consequence

Core4 should **not** start with a complicated multistage parallel engine. It should start with a sequential engine and add limited filesystem-only parallelism later.

---

## 4. What Core4 learns from Core3

### Keep

- sequential clarity
- understandable control flow
- simpler debugging story
- cancellation awareness
- component boundaries

### Improve

- preserve relative paths correctly
- continue after per-file failures where appropriate
- create destination directories automatically
- write sidecars immediately after successful transfer
- make dry-run behavior more honest
- keep progress reporting factual and correct

### Core4 consequence

Core3's simplicity is closer to the right foundation, but Core4 must finish the job and remove the remaining correctness gaps.

---

## 5. Core4 architectural lessons now considered fixed

These lessons have become active Core4 decisions:

### 5.1 Source handling must be explicit

Use `BackupPlan.SourceType` and `BackupPlan.Source`.

Do not rely on path heuristics or older split fields such as `DeviceId` or `SourceDirectory`.

### 5.2 MTP must stay sequential

Media-device access is the fragile case. Core4 should optimize around that fact instead of fighting it.

### 5.3 Filesystem parallelism is later, not foundational

Parallelism belongs to a later filesystem-only tier.

It is not the definition of Core4.

### 5.4 Sidecars belong to the normal success path

Sidecars should be written directly after successful transfer, not collected and generated at the end.

Normalized naming:

- `.sidecar.json`

### 5.5 Metadata date reading should use both tools, in order

The normalized read order is:

1. `MetadataExtractor`
2. `ExifTool` fallback
3. filesystem attributes

This keeps Core4 practical without making ExifTool the only strategy.

### 5.6 Public progress must stay factual

The current public progress model should expose:

- phase
- discovery counts
- processed counts
- bytes processed
- active files

Rich ETA/speed/event reporting can be added later, but it should not redefine the Tier 1 baseline.

### 5.7 Public contracts must stay aligned with the skeleton

Current baseline examples:

- `BackupResult`, not `BackupJobResult`
- internal `BackupItem`, not public `IBackupItem` baseline
- boolean feature flags, not `HashTypes`
- nullable `MaxDegreeOfParallelism`, not `-1` semantics

---

## 6. What Core4 should prioritize first

The practical learning is not just architectural. It also affects build order.

Core4 should prioritize:

1. contract alignment
2. sequential filesystem backup
3. sidecar and progress baseline
4. sequential MTP support
5. robustness and operational trust
6. optional enrichment features
7. limited parallel filesystem
8. persistence/resume
9. advanced reporting/UI helpers

This order reflects the lessons from all earlier versions.

---

## 7. What Core4 should explicitly avoid

Avoid these traps:

- treating MTP stability as a later problem
- adding parallelism before the sequential baseline is trustworthy
- reviving old plan shapes such as `DeviceId`, `HashTypes`, or `WriteSidecar`
- letting sidecars depend on later optional phases
- building progress/UI abstractions into the core too early
- placing hidden metadata fallback logic inside the engine

---

## 8. Final learning summary

The best summary of Core4 learning is:

- build the simple path first
- make that path work for both filesystem and MTP
- preserve correctness before performance
- use layered optional features
- treat parallelism as a controlled extension, not the identity of the engine

That is the direction Core4 should keep.
