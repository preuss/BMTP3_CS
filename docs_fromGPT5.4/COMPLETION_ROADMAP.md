# Core4 Completion Roadmap

**Purpose:** High-level roadmap for completing Core4 in the right architectural order.

**Last Updated:** 2026-05-12

**Status:** Normalized against the current `BMTP3.Core4` skeleton and the updated Core4 documents.

---

## 1. Roadmap principles

This roadmap follows a few strict rules:

1. **Tier 1 first.** Core4 is not complete until both filesystem and MTP work.
2. **Sequential first.** Performance comes after correctness.
3. **MTP stays sequential.** Parallelism is never a requirement for media-device stability.
4. **Public contracts stay small.** Do not expand the baseline contract just to support richer UI.
5. **Optional features come later.** Metadata, hashing, verification, and timestamp correction are layered on top of the working core.

---

## 2. Roadmap at a glance

| Phase | Tier | Goal | Main deliverables |
|---|---|---|---|
| 0 | Tier 1 | Contract/state baseline | `BackupPlan`, `BackupResult`, `IBackupEngine`, `IBackupProgress`, validation, session state |
| 1 | Tier 1 | Sequential filesystem backup | `FileSystemScanner`, `FilesystemFileTransfer`, `SequentialBackupEngine` |
| 2 | Tier 1 | Sidecar/progress/DI baseline | `SidecarData`, `ISidecarGenerator`, `JsonSidecarGenerator`, `ProgressTracker`, `Core4Options`, DI |
| 3 | Tier 1 | Sequential MTP backup | `MtpScanner`, `MTPFileTransfer`, safe source routing |
| 4 | Tier 2 | Hardening | retries, timeouts, cancellation, dry-run, clearer failure behavior |
| 5 | Tier 3 | Optional enrichment | metadata, timestamp correction, hashing, verification, sidecar enrichment |
| 6 | Tier 4 | Limited parallel filesystem | `LimitedParallelBackupEngine`, factory routing, multi-file progress |
| 7 | Tier 5 | Persistence/resume | `IBackupRepository` and durable repository implementations |
| 8 | Tier 6 | Advanced reporting/UI | richer CLI/UI layers and optional notification helpers |

---

## 3. Phase-by-phase roadmap

### Phase 0 - Contracts and state

Finish the structural baseline first.

Deliverables:

- `Api/IBackupEngine.cs`
- `Api/IBackupProgress.cs`
- `Api/IFileProgress.cs`
- `Models/BackupPlan.cs`
- `Models/BackupResult.cs`
- `Models/BackupItem.cs`
- `Models/Enums/*`
- `Engine/Validation/BackupPlanValidator.cs`
- `Engine/Validation/BackupPlanValidationException.cs`
- `Engine/State/BackupSessionState.cs`
- `Engine/State/BackupSessionStateKey.cs`
- `Engine/State/BackupSessionStateKeyFactory.cs`
- `Engine/State/IBackupSessionStateStore.cs`
- `Engine/State/InMemoryBackupSessionStateStore.cs`

Exit criteria:

- public contracts match the current skeleton
- validation reflects the current `BackupPlan`
- session state can be opened and populated

### Phase 1 - Sequential filesystem backup

Build the first complete end-to-end slice.

Deliverables:

- `Scanner/IBackupScanner.cs` usage in engine flow
- `Scanner/Filesystem/FileSystemScanner.cs`
- `Transfer/IFileTransfer.cs`
- `Transfer/Filesystem/FilesystemFileTransfer.cs`
- `Engine/Sequential/SequentialBackupEngine.cs`

Exit criteria:

- filesystem scan works
- relative paths are preserved
- transfer works sequentially
- destination directories are created correctly
- `BackupResult` is correct

### Phase 2 - Sidecar, progress, and composition

Turn the minimal transfer path into a real backup flow.

Deliverables:

- `Sidecar/SidecarData.cs`
- `Sidecar/ISidecarGenerator.cs`
- `Sidecar/JsonSidecarGenerator.cs`
- `Sidecar/SidecarEnrichment.cs` placeholder if needed
- `Progress/ProgressTracker.cs`
- `DependencyInjection/Core4Options.cs`
- `Engine/BackupEngineFactory.cs`
- `DependencyInjection/ServiceCollectionExtensions.cs`

Exit criteria:

- successful transfers produce `.sidecar.json`
- progress snapshots are stable and factual
- DI can resolve a working Core4 setup

### Phase 3 - Sequential MTP support

Close Tier 1 properly.

Deliverables:

- `Scanner/MTP/MtpScanner.cs`
- `Transfer/MTP/MTPFileTransfer.cs`
- explicit source routing via `BackupPlan.SourceType`
- MTP keepalive, timeout, and retry policy

Exit criteria:

- MTP works without parallel execution
- filesystem and MTP now share the same overall Core4 baseline

### Phase 4 - Hardening

Make the core trustworthy.

Focus areas:

- clear failure handling
- predictable cancellation
- correct dry-run semantics
- collision behavior
- reliable sidecar writes
- better diagnostics

Exit criteria:

- one-off failures no longer make the engine feel fragile
- the baseline is trustworthy for real use

### Phase 5 - Optional enrichment

Add the advanced features without changing the baseline contract.

Deliverables:

- `IItemHasher`, `HashResult`, `FileHasher`
- `IMetadataReader`, `ExtractedMetadata`, `MetadataExtractorReader`, `ExifToolMetadataReader`
- `IIntegrityVerifier`, `VerificationResult`, `FileIntegrityVerifier`
- `ITimestampCorrector`, `TimestampCorrectionResult`, `FileTimestampCorrector`
- `SidecarEnrichment`

Exit criteria:

- features are optional and gated
- metadata follows `MetadataExtractor -> ExifTool -> filesystem`
- sidecars can be enriched after transfer

### Phase 6 - Limited parallel filesystem

Add performance where it is safe.

Deliverables:

- `Engine/LimitedParallel/LimitedParallelBackupEngine.cs`
- factory routing for filesystem-only parallel execution
- support for multi-file active progress

Exit criteria:

- filesystem can run with limited concurrency
- MTP remains sequential
- the public API is unchanged

### Phase 7 - Persistence and resume

Extend the architecture with durable state.

Deliverables:

- `IBackupRepository`
- `NoOpBackupRepository`
- `FileSystemBackupRepository`
- `SqliteBackupRepository`

Exit criteria:

- session state can be stored deliberately
- resume behavior is explicit

### Phase 8 - Advanced reporting and UI

Polish the experience after the engine is already stable.

Examples:

- richer CLI views
- exports and summaries
- optional notification helpers such as `IProgressNotifier`

Exit criteria:

- richer reporting sits on top of the stable baseline

---

## 4. Roadmap guardrails

Do not treat the roadmap as complete if it drifts back toward old Core4 shapes.

Avoid these anti-patterns:

- `BackupJobResult` instead of `BackupResult`
- `IBackupItem` as the public baseline
- `DeviceId`, `SourceDirectory`, `OutputDirectory`, `WriteSidecar`, or `HashTypes` as active contract fields
- `MaxDegreeOfParallelism = -1`
- path-based source detection
- `.bmtp3.json` as the active sidecar naming
- ExifTool-only metadata reading
- MTP parallelism
- early dependency on `IProgressNotifier` for core behavior

---

## 5. When the roadmap is complete

The roadmap is complete when:

- Tier 1 is complete for both filesystem and MTP
- Tier 2 makes the baseline trustworthy
- Tier 3 adds optional enrichment cleanly
- Tier 4 improves filesystem performance without destabilizing MTP
- Tier 5 adds persistence deliberately
- Tier 6 improves presentation without redefining the engine contract

That is the intended path to a finished Core4.
