# Core4 Completion Summary

**Purpose:** Short summary of what it actually means to complete Core4, in the correct order.

**Last Updated:** 2026-05-12

**Status:** Normalized against the current `BMTP3.Core4` skeleton and the updated Core4 master documents.

---

## 1. What "complete" means

Core4 is **not** complete when a few classes exist, when filesystem-only backup works, or when a parallel engine skeleton compiles.

Core4 is only meaningfully complete when:

1. the public Core4 contracts are coherent
2. sequential filesystem backup works end-to-end
3. sequential MTP backup works end-to-end
4. sidecars are written as part of normal success flow
5. progress snapshots are correct and stable
6. advanced features are added in later tiers without breaking the baseline

The most important normalization is:

**Tier 1 is not complete until both filesystem and MTP work.**

---

## 2. Core4 baseline that must not drift

These are the baseline truths that define the current Core4 direction:

- `BackupPlan` uses `SourceType`, `Source`, `Destination`, and boolean feature flags
- `BackupResult` is the result type
- `IBackupProgress` is factual and compact
- `IBackupScanner` streams `BackupItem` via `IAsyncEnumerable<BackupItem>`
- MTP is always sequential
- sidecars use `.sidecar.json`
- metadata date reading uses `MetadataExtractor` first and `ExifTool` as fallback
- `MaxDegreeOfParallelism` is nullable and does not use `-1`

If a document or sketch conflicts with these points, it is outdated.

---

## 3. Completion order

Core4 should be completed in this order:

1. contracts, validation, and session-state baseline
2. sequential filesystem backup
3. sidecar generation and progress tracking
4. sequential MTP support
5. hardening and failure behavior
6. optional enrichment features
7. limited parallel filesystem engine
8. persistence and resume
9. advanced reporting and UI helpers

This order matters. It prevents Core4 from repeating Core2's mistake of adding complexity before the baseline is trustworthy.

---

## 4. Completion by tier

### Tier 1 - Working backup core

Tier 1 includes the minimum Core4 that must work before anything else matters.

Required components:

- `IBackupEngine`
- `IBackupProgress`
- `IFileProgress`
- `BackupPlan`
- `BackupResult`
- `BackupItem`
- `BackupPlanValidator`
- `BackupPlanValidationException`
- `BackupSessionState`
- `BackupSessionStateKey`
- `BackupSessionStateKeyFactory`
- `IBackupSessionStateStore`
- `InMemoryBackupSessionStateStore`
- `IBackupScanner`
- `FileSystemScanner`
- `MtpScanner`
- `IFileTransfer`
- `FilesystemFileTransfer`
- `MTPFileTransfer`
- `SequentialBackupEngine`
- `ProgressTracker`
- `ISidecarGenerator`
- `SidecarData`
- `JsonSidecarGenerator`
- `BackupEngineFactory`
- `Core4Options`
- `ServiceCollectionExtensions`

Tier 1 is done when:

- filesystem backup works sequentially
- MTP backup works sequentially
- relative paths are preserved
- destination directories are created correctly
- sidecars are written immediately after successful transfer
- progress snapshots match actual work
- `BackupResult` is accurate

### Tier 2 - Trustworthy behavior

Tier 2 strengthens the baseline without redefining public contracts.

Focus:

- clearer diagnostics
- stronger error mapping
- clean cancellation behavior
- correct dry-run semantics
- robust collision behavior
- device-safe timeout and retry handling

Tier 2 is done when the baseline can be trusted under real operational failures.

### Tier 3 - Optional enrichment

Tier 3 adds features that improve quality and completeness but are not required for the first working Core4.

Components:

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

Tier 3 is done when these features are optional, explicit, and cleanly integrated into sidecars and result handling.

### Tier 4 - Limited parallel filesystem

Tier 4 adds performance, but only where safe.

Required direction:

- implement `LimitedParallelBackupEngine`
- route only filesystem workloads into the parallel path
- keep MTP sequential
- keep the same public `BackupPlan` / `BackupResult` / `IBackupProgress` contract

Tier 4 is done when filesystem parallelism exists without destabilizing the Tier 1 baseline.

### Tier 5 - Persistence and resume

Planned persistence components:

- `IBackupRepository`
- `NoOpBackupRepository`
- `FileSystemBackupRepository`
- `SqliteBackupRepository`

Tier 5 is done when session state can be persisted and resume behavior is explicit.

### Tier 6 - Advanced reporting and UI

This tier is about presentation, not core correctness.

Examples:

- richer CLI views
- exported reports
- UI-oriented notification helpers
- optional event-driven reporting abstractions

`IProgressNotifier` belongs here if introduced later. It is not part of the Tier 1 public baseline.

---

## 5. MTP completion requirements

Because earlier versions failed here, MTP has its own explicit completion rules.

MTP is only considered complete when:

- scanning works safely on the device source
- transfer works safely on the device source
- no parallel MTP execution is required
- keepalive is maintained during long operations
- each operation respects timeout limits
- retry behavior is explicit
- disconnects fail cleanly

If MTP only works in a fragile or partially parallel design, Core4 is not complete.

---

## 6. Metadata and timestamp completion requirements

Metadata and timestamp handling are complete only when:

- filesystem attributes are available as fallback data
- managed parsing is attempted first through `MetadataExtractor`
- ExifTool is used as fallback, not as the only reader
- timestamp correction consumes the normalized metadata result
- the engine does not hide its own date fallback logic outside the metadata reader

---

## 7. Signs that Core4 is still incomplete

Core4 is still incomplete if any of these are true:

- only filesystem works
- MTP is still deferred
- sidecars are generated only at the end
- progress depends on older ETA/speed public DTO fields
- old plan fields like `DeviceId`, `HashTypes`, or `WriteSidecar` are still treated as active contracts
- `BackupJobResult` is still used as the main result type
- the parallel engine exists before the sequential baseline is solid

---

## 8. Final checkpoint

The shortest honest definition of completion is:

**Core4 is complete when it has a trustworthy sequential core for both filesystem and MTP, writes sidecars correctly, reports factual progress, and then layers optional enrichment and filesystem-only parallelism on top without breaking that baseline.**

For the detailed build order, use `docs\CORE4_IMPLEMENTATION_GUIDE_DA.md` and `docs\CORE4_PLAN_en.md`.
