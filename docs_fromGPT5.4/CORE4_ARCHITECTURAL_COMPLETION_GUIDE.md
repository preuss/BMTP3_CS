# Core4 Architectural Completion Guide

**Purpose:** Explain which architectural pieces must exist, and in which order, for Core4 to be considered structurally complete.

**Last Updated:** 2026-05-12

**Status:** Normalized against the current `BMTP3.Core4` skeleton and the updated Core4 master documents.

---

## 1. What this guide is for

This document sits between the architecture document and the detailed implementation guide:

- `CORE4_ARCHITECTURE_en.md` explains the normalized structure
- `CORE4_PLAN_en.md` explains the prioritized implementation order
- `CORE4_IMPLEMENTATION_GUIDE_DA.md` gives the detailed coder workflow

This file answers a narrower question:

**What architectural pieces must be present before Core4 is genuinely complete?**

---

## 2. Frozen architectural decisions

These decisions are already fixed and should not drift while finishing Core4:

- `BackupPlan` uses `SourceType`, `Source`, `Destination`, and boolean feature flags
- `BackupResult` is the final result type
- `IBackupProgress` is factual and compact
- `IFileProgress` is the active-file progress model
- `IBackupScanner` streams `BackupItem`
- `BackupItem` is an internal working item, not a public `IBackupItem` baseline
- sidecars use `.sidecar.json`
- metadata reading uses `MetadataExtractor` first and `ExifTool` as fallback
- MTP is always sequential
- `MaxDegreeOfParallelism` is nullable and never uses `-1`
- richer event-driven UI reporting is later-tier, not the Tier 1 public contract

If an older sketch conflicts with any of these points, the sketch is obsolete.

---

## 3. The architectural layers Core4 must have

Core4 is complete only when these layers exist in a coherent form.

### 3.1 Contract layer

Required baseline contracts:

- `IBackupEngine`
- `IBackupProgress`
- `IFileProgress`
- `BackupPlan`
- `BackupResult`
- Core enums such as `BackupSourceType`, `OutputStructure`, `CollisionStrategy`, `BackupPhase`, and `BackupErrorCode`

This layer defines what callers see. It must remain stable and small.

### 3.2 Validation and state layer

Required baseline classes:

- `BackupPlanValidator`
- `BackupPlanValidationException`
- `BackupSessionState`
- `BackupSessionStateKey`
- `BackupSessionStateKeyFactory`
- `IBackupSessionStateStore`
- `InMemoryBackupSessionStateStore`

This layer is the internal source of truth for a running backup session.

### 3.3 Source and transfer layer

Required baseline components:

- `IBackupScanner`
- `FileSystemScanner`
- `MtpScanner`
- `IFileTransfer`
- `FilesystemFileTransfer`
- `MTPFileTransfer`

This layer is responsible for discovery and movement of file content.

### 3.4 Orchestration layer

Required baseline components:

- `SequentialBackupEngine`
- `BackupEngineFactory`

Planned later component:

- `LimitedParallelBackupEngine`

The orchestration layer decides how the session flows, not the scanners or transfer services.

### 3.5 Sidecar and progress layer

Required baseline components:

- `ProgressTracker`
- `SidecarData`
- `ISidecarGenerator`
- `JsonSidecarGenerator`
- `Core4Options`
- `ServiceCollectionExtensions`

This layer turns raw transfer into usable backup behavior.

### 3.6 Optional feature layer

Tier 3 components:

- `IItemHasher`
- `HashResult`
- `FileHasher`
- `IMetadataReader`
- `ExtractedMetadata`
- `MetadataExtractorReader`
- `ExifToolMetadataReader`
- `IIntegrityVerifier`
- `VerificationResult`
- `FileIntegrityVerifier`
- `ITimestampCorrector`
- `TimestampCorrectionResult`
- `FileTimestampCorrector`
- `SidecarEnrichment`

### 3.7 Persistence and reporting layer

Later-tier components:

- `IBackupRepository`
- `NoOpBackupRepository`
- `FileSystemBackupRepository`
- `SqliteBackupRepository`
- optional UI/reporting helpers such as `IProgressNotifier`

This layer must sit on top of the stable baseline, not redefine it.

---

## 4. Architectural completion phases

### Phase A - Core skeleton coherence

The architecture is not ready to complete until the public contracts and internal state model agree with each other.

Phase A is done when:

- validation uses the current `BackupPlan`
- session state can be opened and populated
- the engine can build toward a correct `BackupResult`

### Phase B - Sequential filesystem architecture

This is the first full end-to-end architecture slice.

It must include:

- streaming filesystem scan
- destination path resolution
- sequential transfer
- item status updates
- sidecar creation
- progress snapshots

Phase B is done when filesystem backup works end-to-end without any optional features.

### Phase C - Sequential MTP architecture

This is the architectural milestone that closes Tier 1.

It must include:

- explicit MTP source routing via `BackupPlan.SourceType`
- safe MTP scan and transfer
- STA/COM-safe device interaction where required
- keepalive, timeout, and retry policy
- no dependency on parallel execution

Phase C is done when MTP works with the same overall orchestration shape as filesystem, but always sequentially.

### Phase D - Hardening architecture

This phase strengthens the baseline without changing the public contract shape.

It must include:

- clean cancellation behavior
- clear per-item failure handling
- better dry-run semantics
- explicit collision behavior
- reliable sidecar writing
- predictable error propagation

Phase D is done when the baseline architecture is trustworthy, not merely functional.

### Phase E - Enrichment architecture

This phase adds optional feature layers.

It must include:

- metadata reading
- timestamp correction
- hashing
- verification
- sidecar enrichment updates

Phase E is done when these features are optional, explicit, and layered cleanly on top of the Tier 1 core.

### Phase F - Limited parallel filesystem architecture

This phase introduces performance carefully.

It must include:

- `LimitedParallelBackupEngine`
- controlled routing from `BackupEngineFactory`
- filesystem-only parallel transfer
- unchanged MTP behavior
- unchanged public API

Phase F is done when filesystem performance improves without regressing correctness or making MTP unstable.

### Phase G - Persistence and advanced reporting

This is the final architectural expansion phase.

It must include:

- explicit repository abstractions for persistence/resume
- durable session state
- richer reporting or UI adapters layered on top of the stable engine

Phase G is done when resume and advanced reporting are clean extensions rather than architectural rewrites.

---

## 5. The engine-selection architecture

The engine-selection rule should remain simple:

1. `MediaDevice` source -> `SequentialBackupEngine`
2. filesystem source with `MaxDegreeOfParallelism == 1` -> `SequentialBackupEngine`
3. filesystem source before Tier 4 is complete -> `SequentialBackupEngine`
4. filesystem source after Tier 4 with `MaxDegreeOfParallelism > 1` -> `LimitedParallelBackupEngine`

This means:

- MTP is always protected by the sequential path
- parallelism is a later optimization, not the foundation

---

## 6. Architectural red lines

Core4 should be considered architecturally wrong if it reintroduces any of these patterns:

- abstract `BackupEngine` hierarchy as the required baseline before the sequential core is finished
- `BackupJobResult` instead of `BackupResult`
- `IBackupItem` as the public baseline instead of internal `BackupItem`
- `DeviceId`, `SourceDirectory`, `OutputDirectory`, or `HashTypes` as active public plan fields
- `MaxDegreeOfParallelism = -1` semantics
- path-based source-type detection
- ExifTool as the only metadata reader
- sidecar generation only at the end
- early dependence on `IProgressNotifier` for core correctness
- parallel MTP behavior

---

## 7. Completion checklist

Core4 is architecturally complete only when all of the following are true:

- [ ] public contracts match the current skeleton
- [ ] validation and session state are in place
- [ ] sequential filesystem backup works end-to-end
- [ ] sequential MTP backup works end-to-end
- [ ] sidecars are written as part of normal success flow
- [ ] progress snapshots are stable and factual
- [ ] optional features are layered, not entangled
- [ ] filesystem-only limited parallelism is optional and safe
- [ ] persistence and richer reporting are extensions, not hidden dependencies

---

## 8. Final architectural summary

The essential architectural story of Core4 is simple:

- build a small, correct sequential core
- make that core work for both filesystem and MTP
- add sidecars and progress as part of the normal flow
- harden the baseline
- add optional enrichment
- add limited parallelism only for filesystem
- add persistence and reporting last

That is the architecture Core4 must complete.
