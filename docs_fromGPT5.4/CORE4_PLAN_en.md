# Core4 - Implementation Plan

**Purpose:** This document translates the Core4 architecture into a practical implementation sequence. It is the English planning companion to the normalized Core4 architecture and implementation guide.

**Last Updated:** 2026-05-12

**Status:** Normalized against the current `BMTP3.Core4` skeleton and the stronger Core4 master documents.

---

## 1. How this plan should be used

This file is a **planning and prioritization document**.

Use it for:

- deciding what to build first
- understanding which classes belong to which tier
- checking what is need-to-have vs nice-to-have
- keeping implementation order aligned with the normalized Core4 decisions

Do **not** use this file as the only source for exact code signatures. For exact contract details and step-by-step implementation order, also use:

1. `docs\CORE4_IMPLEMENTATION_GUIDE_DA.md`
2. `docs\CORE4_ARCHITECTURE_en.md`
3. `docs\CORE4_MASTER_SYNTHESIS.md`
4. `docs\Core4_Master_Architecture.md`

---

## 2. Why Core4 exists

Core4 exists because the earlier generations each got something important right, but none of them are the final answer:

- **Core** proved the product idea, but it is legacy and no longer the target implementation.
- **Core2** aimed for power and features, but the channel-heavy parallel model became too fragile.
- **Core3** brought simplicity back, but it is too incomplete and too limited for the long-term goal.

Core4 should combine the good parts:

- Core3-style clarity
- Core2-style feature ambitions
- explicit source handling
- selective performance improvements only where they are safe

The fundamental slogan is still:

**Build it simple. Build it right. Then optimize smartly.**

---

## 3. Fixed decisions before implementation starts

These decisions are already normalized and should not be re-debated during implementation.

| Topic | Decision |
|---|---|
| Source selection | Use `BackupPlan.SourceType` plus `BackupPlan.Source` |
| Result type | Use `BackupResult`, not `BackupJobResult` |
| Progress contract | Use the current `IBackupProgress` + `IFileProgress` skeleton contracts |
| Scanner contract | Use `IAsyncEnumerable<BackupItem>` |
| Sidecar naming | Use `.sidecar.json` |
| Metadata strategy | `MetadataExtractor` first, `ExifTool` fallback, filesystem attributes last |
| Timestamp strategy | Use the normalized metadata result, not hidden engine fallback logic |
| MTP execution | Always sequential |
| Parallel execution | Filesystem only, and only when Tier 4 is reached |
| `MaxDegreeOfParallelism` | `null = auto`, `1 = sequential`, `> 1 = limited parallel later` |
| Feature flags | Use boolean feature flags on `BackupPlan` |
| Old plan fields | Do not revive `DeviceId`, `SourceDirectory`, `OutputDirectory`, `WriteSidecar`, `HashTypes`, or `-1` semantics |

If a feature needs configuration that does not belong in the current `BackupPlan`, place it in `Core4Options` or an internal policy object rather than mutating the public contract.

---

## 4. Priority model

| Priority | Meaning | Typical examples |
|---|---|---|
| **NEED** | Required for a minimal correct Core4 | sequential engine, filesystem backup, MTP backup, sidecar writing, progress snapshots |
| **SHOULD** | Strongly recommended after the minimum works | error shaping, retries, dry-run correctness, better diagnostics |
| **NICE** | Valuable features, but not part of the first working Core4 | hashing, metadata, verification, timestamp correction |
| **LATER** | Important only after the earlier layers are stable | limited parallel filesystem, persistence/resume, advanced UI/reporting |

Tier mapping:

- **Tier 1** = NEED
- **Tier 2** = SHOULD
- **Tier 3** = NICE
- **Tier 4-6** = LATER

---

## 5. Condensed lessons from previous cores

This plan intentionally avoids repeating every historical bug list. The lessons are already absorbed into the implementation order below.

### 5.1 From Core2

Core4 must not repeat these mistakes:

- over-engineered channel pipelines
- hidden cross-stage coupling
- hard-to-debug parallel orchestration
- unsafe MTP lifecycle handling
- concurrency before correctness

### 5.2 From Core3

Core4 must explicitly fix:

- lost relative paths
- backup-wide failure on single-item errors
- incomplete progress behavior
- incomplete dry-run behavior
- sidecar generation happening too late

### 5.3 Core4 consequence

Therefore Core4 starts with:

- a clear sequential engine
- a streaming scanner contract
- explicit source typing
- explicit sidecar generation
- optional advanced features only after the minimum works

---

## 6. Implementation stages

The stages below are the recommended build order.

### Stage 0 - Contract alignment and state baseline `[NEED]`

**Goal:** Ensure the public and internal Core4 baseline is consistent before completing the engine.

Implement or normalize:

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

Done when:

- the current skeleton contracts are internally coherent
- validation catches invalid plan shape up front
- the engine can open a session state and accept discovered items

### Stage 1 - Minimal filesystem backup `[NEED]`

**Goal:** Make Core4 able to complete a real sequential filesystem backup.

Implement:

- `Scanner/IBackupScanner.cs` usage in the engine
- `Scanner/Filesystem/FileSystemScanner.cs`
- `Transfer/IFileTransfer.cs`
- `Transfer/Filesystem/FilesystemFileTransfer.cs`
- `Engine/Sequential/SequentialBackupEngine.cs` end-to-end transfer path
- destination resolution inside the engine (`ResolveDestination(...)` in the normalized guide)

Behavior requirements:

- preserve relative paths
- create destination directories as needed
- handle `SkipExisting`, `CollisionStrategy`, and `StopOnError`
- mark individual item success/skip/failure cleanly
- return a correct `BackupResult`

Done when:

- a filesystem source can be scanned and copied sequentially
- one failed file does not silently corrupt session state
- `BackupResult` reflects actual work performed

### Stage 2 - Sidecar and progress baseline `[NEED]`

**Goal:** Complete the minimal user-visible workflow around each successful file.

Implement:

- `Sidecar/SidecarData.cs`
- `Sidecar/ISidecarGenerator.cs`
- `Sidecar/JsonSidecarGenerator.cs`
- `Sidecar/SidecarEnrichment.cs` as a minimal placeholder if needed
- `Progress/ProgressTracker.cs`
- `DependencyInjection/Core4Options.cs`
- `Engine/BackupEngineFactory.cs`
- `DependencyInjection/ServiceCollectionExtensions.cs`

Rules:

- sidecar path is `item.DestinationPath + ".sidecar.json"`
- sidecar is written immediately after successful transfer
- progress reporting uses the current public `IBackupProgress` shape
- update frequency should be bounded to avoid noisy flooding

Done when:

- each successful transfer can emit a sidecar
- the engine produces stable progress snapshots
- basic DI setup can resolve a working Core4 engine

### Stage 3 - Sequential MTP support `[NEED]`

**Goal:** Finish Tier 1 by supporting media devices safely.

Implement:

- `Scanner/MTP/MtpScanner.cs`
- `Transfer/MTP/MTPFileTransfer.cs`
- source selection through `BackupPlan.SourceType`
- any internal MTP session helper classes required for STA/COM-safe access

MTP rules:

- sequential only
- keepalive at least every 30 seconds
- per-operation timeout of 60 seconds
- retry backoff of 1s, 2s, 4s

Done when:

- MTP scan and transfer work without requiring a parallel engine
- Core4 Tier 1 is complete for both filesystem and media-device sources

### Stage 4 - Hardening and trustworthiness `[SHOULD]`

**Goal:** Make the minimal engine reliable enough for real-world use.

Focus areas:

- robust error mapping
- consistent cancellation handling
- clearer diagnostics
- correct dry-run semantics
- cleaner collision behavior
- atomic sidecar writing
- more explicit failure handling around device disconnects and IO faults

Important constraint:

- do not add new public DTOs just to solve internal robustness problems

Done when:

- the engine behaves predictably under normal operational failures
- diagnostics are understandable
- dry-run is meaningfully cheaper and safer than a real run

### Stage 5 - Optional enrichment features `[NICE]`

**Goal:** Add the advanced features that make Core4 more complete without changing the Tier 1 baseline.

Implement:

- `Features/Hashing/IItemHasher.cs`
- `Features/Hashing/HashResult.cs`
- `Features/Hashing/FileHasher.cs`
- `Features/Metadata/IMetadataReader.cs`
- `Features/Metadata/ExtractedMetadata.cs`
- `Features/Metadata/MetadataExtractorReader.cs`
- `Features/Metadata/ExifToolMetadataReader.cs`
- `Features/Verification/IIntegrityVerifier.cs`
- `Features/Verification/VerificationResult.cs`
- `Features/Verification/FileIntegrityVerifier.cs`
- `Features/Timestamp/ITimestampCorrector.cs`
- `Features/Timestamp/TimestampCorrectionResult.cs`
- `Features/Timestamp/FileTimestampCorrector.cs`

Feature order:

1. metadata
2. timestamp correction
3. hashing
4. verification
5. richer sidecar enrichment

Metadata rule:

- first populate filesystem-derived values
- then try managed parsing with `MetadataExtractorReader`
- if needed and enabled via `Core4Options.UseExifToolFallback`, use `ExifToolMetadataReader`

Done when:

- Tier 3 features are explicit, testable, and optional
- the engine never hides feature failure behind silent fallback behavior

### Stage 6 - Limited parallel filesystem engine `[LATER]`

**Goal:** Add performance safely without repeating Core2 mistakes.

Implement:

- `Engine/LimitedParallel/LimitedParallelBackupEngine.cs`
- factory routing from `BackupEngineFactory`
- controlled use of `MaxDegreeOfParallelism`
- progress handling for multiple active files

Rules:

- filesystem only
- scanner still streams sequentially
- MTP remains sequential
- no channel-heavy architecture recreation from Core2

Done when:

- filesystem backups can use limited concurrency without changing the public API
- MTP behavior is unchanged and still sequential

### Stage 7 - Persistence and resume `[LATER]`

**Goal:** Make backup sessions durable across process boundaries.

Planned components:

- `Engine/State/IBackupRepository.cs`
- `Engine/State/NoOpBackupRepository.cs`
- `Engine/State/FileSystemBackupRepository.cs`
- `Engine/State/SqliteBackupRepository.cs`

This stage should build on the existing session-state model, not replace it.

Done when:

- session state can be loaded and persisted deliberately
- resume behavior is explicit and testable

### Stage 8 - Advanced reporting and UI integration `[LATER]`

**Goal:** Improve presentation without destabilizing the engine contract.

Examples:

- richer CLI views
- exports and summaries
- optional event-driven UI helpers
- GUI/mobile-friendly adapters

Important normalization:

- `IProgressNotifier` belongs here as a possible later-tier concern, not as the Tier 1 public baseline

Done when:

- richer UX exists on top of the stable engine contract rather than inside it

---

## 7. What not to build first

Do **not** start Core4 with these:

- full parallelism
- MTP parallelism
- channel-based pipeline orchestration
- public ETA/speed DTO expansion before the baseline works
- `HashTypes`-style public plan redesign
- `DeviceId`/`SourceDirectory`/`OutputDirectory` plan variants
- a hidden engine-level metadata fallback that bypasses the metadata reader

If a design choice makes the engine more complex before Tier 1 is complete, it is probably the wrong next step.

---

## 8. Definitions of done by tier

| Tier | Done means |
|---|---|
| Tier 1 | Sequential filesystem + MTP backup works, writes sidecars, reports basic progress, returns correct `BackupResult` |
| Tier 2 | The baseline is trustworthy in real-world failure cases |
| Tier 3 | Optional enrichment features are implemented and gated cleanly |
| Tier 4 | Filesystem-only limited parallelism works without harming correctness |
| Tier 5 | Session persistence and resume are explicit and durable |
| Tier 6 | Rich reporting/UI exists without destabilizing the engine core |

---

## 9. Recommended execution order for a coder

If a coder wants the shortest reliable path, follow this order:

1. Finish the state and validation baseline
2. Finish sequential filesystem transfer
3. Add sidecar generation
4. Add progress tracking
5. Add DI and factory composition
6. Add sequential MTP support
7. Harden errors, cancellation, and dry-run
8. Add metadata and timestamp correction
9. Add hashing and verification
10. Add limited parallel filesystem support
11. Add persistence and resume
12. Add advanced reporting/UI helpers

This order is deliberate. It gives a working Core4 early and delays complexity until the baseline is stable.

---

## 10. Final summary

The Core4 plan is simple in principle:

- build a correct sequential engine first
- make both filesystem and MTP work
- write sidecars and report factual progress
- add robustness next
- add enrichment after that
- add limited filesystem parallelism only when the baseline is already trustworthy

If any older document suggests a different contract shape, prefer the normalized Core4 documents and the current skeleton.
