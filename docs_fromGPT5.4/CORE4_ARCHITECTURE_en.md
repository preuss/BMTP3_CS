# Core4 - Architecture Document

**Purpose:** Architecture baseline for Core4. This document describes the current public contracts, the intended internal structure, and the tiered growth path for a complete Core4 implementation.

**Last Updated:** 2026-05-12

**Status:** Normalized against the current `BMTP3.Core4` skeleton and the stronger Core4 master documents.

---

## 1. Authority and scope

This file is an architecture document, not the step-by-step implementation guide.

Use these rules when reading it:

1. The current `BMTP3.Core4` skeleton is the baseline for public contracts.
2. `docs\CORE4_IMPLEMENTATION_GUIDE_DA.md` is the authoritative implementation workflow.
3. `docs\CORE4_MASTER_SYNTHESIS.md` is the conflict resolver when older documents disagree.
4. `docs\Core4_Master_Architecture.md` is the deep reference document.

This means the document must **not** reintroduce obsolete Core4 shapes such as:

- `DeviceId`
- `SourceDirectory` / `OutputDirectory`
- `WriteSidecar`
- `HashTypes`
- `MaxDegreeOfParallelism = -1`
- path-based MTP detection heuristics
- richer public progress contracts than the current skeleton exposes

---

## 2. Design principles

Core4 is built around a small set of non-negotiable rules:

- **Correctness before speed.** A complete, correct sequential implementation comes first.
- **MTP stability before concurrency.** Media devices are always treated as fragile sources. MTP stays sequential.
- **Streaming over bulk buffering.** Discovery should stream items via `IAsyncEnumerable<BackupItem>`.
- **Explicit tiering.** Advanced behavior must be added in layers, not silently assumed.
- **No hidden fallbacks in the engine.** Metadata and timestamp fallback logic belongs in the metadata layer, not inside transfer orchestration.
- **Current public API stays small.** The CLI can grow richer UX, but the engine contract should remain factual and stable.

---

## 3. Tier roadmap

| Tier | Goal | Required outcome |
|---|---|---|
| Tier 1 | Working backup core | Sequential filesystem + MTP backup works end-to-end |
| Tier 2 | Robustness and usability | Better diagnostics, progress stability, retries, error shaping |
| Tier 3 | Optional enrichment | Hashing, metadata, verification, timestamp correction |
| Tier 4 | Performance | Limited parallel filesystem engine only |
| Tier 5 | Persistence and resume | Durable session/repository state |
| Tier 6 | Advanced reporting/UI | Rich CLI/UI reporting, exports, advanced views |

Important normalization:

- **Tier 1 is not complete until both filesystem and MTP work.**
- `MaxDegreeOfParallelism` is already in the public plan, but the real parallel path belongs to **Tier 4**.
- Tier 3 features must be **explicitly gated** until implemented.

---

## 4. Current public contracts

These are the contracts that should be treated as current truth because they already exist in the Core4 skeleton.

### 4.1 `IBackupEngine`

**Location:** `Api\IBackupEngine.cs`

```csharp
public interface IBackupEngine
{
    Task<BackupResult> RunAsync(
        BackupPlan plan,
        IProgress<IBackupProgress>? progress,
        CancellationToken cancellationToken);
}
```

Responsibilities:

- validate the plan
- orchestrate scan and processing
- report factual progress snapshots
- return a final `BackupResult`

### 4.2 `IBackupProgress`

**Location:** `Api\IBackupProgress.cs`

```csharp
public interface IBackupProgress
{
    BackupPhase CurrentPhase { get; }
    int DirectoriesScanned { get; }
    int FilesDiscovered { get; }
    long BytesTotal { get; }
    int FilesProcessed { get; }
    int FilesSucceeded { get; }
    int FilesSkipped { get; }
    int FilesFailed { get; }
    long BytesProcessed { get; }
    IReadOnlyList<IFileProgress> ActiveFiles { get; }
}
```

Normalization:

- `IBackupProgress` is intentionally factual and compact.
- Public ETA, throughput, `CurrentFilePath`, and active worker counts are **not** part of the current public contract.
- Richer reporting can be added later in CLI/UI layers or in later tiers without redefining the Tier 1 baseline.

### 4.3 `IFileProgress`

**Location:** `Api\IFileProgress.cs`

```csharp
public interface IFileProgress
{
    string Path { get; }
    long? BytesTotal { get; }
    long BytesProcessed { get; }
}
```

`ActiveFiles` supports both sequential and future limited parallel flows.

### 4.4 `BackupPlan`

**Location:** `Models\BackupPlan.cs`

```csharp
public sealed record BackupPlan
{
    public string Name { get; init; } = string.Empty;
    public BackupSourceType SourceType { get; init; }
    public string Source { get; init; } = string.Empty;
    public bool Recursive { get; init; } = true;
    public IReadOnlyList<string>? IncludePatterns { get; init; }
    public IReadOnlyList<string>? ExcludePatterns { get; init; }
    public string Destination { get; init; } = string.Empty;
    public OutputStructure OutputStructure { get; init; }
    public CollisionStrategy CollisionStrategy { get; init; }
    public bool DryRun { get; init; }
    public bool StopOnError { get; init; }
    public bool SkipExisting { get; init; }
    public bool EnableHashing { get; init; }
    public bool EnableMetadata { get; init; }
    public bool EnableVerification { get; init; }
    public bool EnableTimestampCorrection { get; init; }
    public int? MaxDegreeOfParallelism { get; init; }
}
```

Semantics:

- `SourceType` is explicit and must drive source selection.
- `Source` is the single source identifier or root path.
- `Destination` is the output directory.
- `EnableHashing`, `EnableMetadata`, `EnableVerification`, and `EnableTimestampCorrection` are booleans, not type lists.
- `MaxDegreeOfParallelism` semantics are:
  - `null` = engine default
  - `1` = force sequential
  - `> 1` = limited parallel path once Tier 4 exists

### 4.5 `BackupResult`

**Location:** `Models\BackupResult.cs`

```csharp
public sealed record BackupResult
{
    public string Name { get; init; } = string.Empty;
    public BackupPhase FinalPhase { get; init; }
    public BackupErrorCode? FailureReason { get; init; }
    public int DirectoriesScanned { get; init; }
    public int FilesDiscovered { get; init; }
    public long BytesTotal { get; init; }
    public int FilesProcessed { get; init; }
    public int FilesSucceeded { get; init; }
    public int FilesSkipped { get; init; }
    public int FilesFailed { get; init; }
    public long BytesProcessed { get; init; }
}
```

Normalization:

- The current result type is `BackupResult`, not `BackupJobResult`.
- The current result is a factual summary, not a performance dashboard.

### 4.6 Current enums

Current baseline enums from the skeleton:

- `BackupSourceType`: `MediaDevice`, `FileSystem`
- `OutputStructure`: `Flat`, `PreserveHierarchy`
- `CollisionStrategy`: `Skip`, `Overwrite`, `Rename`
- `BackupPhase`: `Starting`, `Scanning`, `Transferring`, `Completed`, `Cancelled`, `Failed`

Do **not** reintroduce older phase names such as `Idle`, `Copying`, `Finalizing`, or `Done` as the public baseline.

---

## 5. Current internal model

### 5.1 `BackupItem`

**Location:** `Models\BackupItem.cs`

```csharp
internal sealed class BackupItem
{
    public string Id { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string RelativePath { get; init; } = string.Empty;
    public string? DestinationPath { get; set; }
    public long? SizeBytes { get; init; }
    public DateTimeOffset? ModifiedAt { get; init; }
    public BackupItemStatus Status { get; set; } = BackupItemStatus.Pending;
}
```

This is the internal working item for the backup engine. It starts as discovered data and is enriched as Core4 matures.

### 5.2 `BackupItemStatus`

Current internal states:

- `Pending`
- `Succeeded`
- `Skipped`
- `Failed`

### 5.3 `IBackupScanner`

**Location:** `Scanner\IBackupScanner.cs`

```csharp
internal interface IBackupScanner
{
    IAsyncEnumerable<BackupItem> ScanAsync(
        BackupPlan plan,
        CancellationToken cancellationToken);
}
```

Normalization:

- Scanner output is streaming.
- The scanner must not return `Task<IReadOnlyList<BackupItem>>`.
- Filesystem and MTP both fit behind the same scanner abstraction.

### 5.4 Backup session state

Current state-related contracts already present in the skeleton:

- `BackupSessionState`
- `BackupSessionStateKey`
- `BackupSessionStateKeyFactory`
- `IBackupSessionStateStore`

Current store contract:

```csharp
internal interface IBackupSessionStateStore
{
    Task<BackupSessionState> OpenAsync(
        BackupSessionStateKey key,
        CancellationToken cancellationToken);
}
```

This is the seed for Tier 5 persistence/resume, but it is already useful in Tier 1 because the engine needs a clear internal source of truth.

### 5.5 Validation

Current validation entry point:

- `BackupPlanValidator.Validate(BackupPlan plan)`

Current validation behavior in the skeleton:

- requires `Name`
- requires `Source`
- requires `Destination`
- validates enum values
- rejects `MaxDegreeOfParallelism <= 0` when set
- throws `BackupPlanValidationException` with collected validation errors

This is stronger than earlier drafts that only threw individual argument exceptions.

---

## 6. Engine architecture

### 6.1 Current engine baseline

Current engine classes in the skeleton:

- `SequentialBackupEngine` - active implementation baseline
- `LimitedParallelBackupEngine` - placeholder for later expansion

Current state:

- `SequentialBackupEngine` already wires validation, session-state opening, and streaming scan into session state.
- `LimitedParallelBackupEngine` exists as a placeholder and should not drive current behavior yet.

### 6.2 Planned dispatch model

The recommended dispatch rule is:

1. If `plan.SourceType == BackupSourceType.MediaDevice`, use sequential.
2. Else if `plan.MaxDegreeOfParallelism == 1`, use sequential.
3. Else use sequential until Tier 4 is implemented.
4. After Tier 4 exists, filesystem plans with `MaxDegreeOfParallelism > 1` may use `LimitedParallelBackupEngine`.

If a `BackupEngineFactory` or resolver class is introduced, it should follow those rules. The name is acceptable, but the rules matter more than the exact class name.

### 6.3 Sequential execution flow

The intended Tier 1 flow is:

1. Validate `BackupPlan`
2. Open or create `BackupSessionState`
3. Set phase to `Scanning`
4. Stream items from `IBackupScanner`
5. Add discovered items to session state
6. Set phase to `Transferring`
7. Process pending items one by one
8. Resolve destination path
9. Transfer file
10. Generate sidecar
11. Update progress and item state
12. Complete, cancel, or fail session
13. Build `BackupResult`

### 6.4 Limited parallel execution

Limited parallelism is a **filesystem-only Tier 4** feature.

Rules:

- never parallelize MTP
- scanner still streams sequentially
- transfer workers may process filesystem items concurrently
- progress reporting still uses the same public `IBackupProgress` shape
- `ActiveFiles` becomes more important once parallel workers exist

---

## 7. Source adapters

### 7.1 Filesystem source

Planned main components:

- `FileSystemScanner`
- `FileSystemFileTransfer`

Responsibilities:

- enumerate files from the filesystem
- respect include/exclude patterns
- calculate relative paths
- preserve cancellation responsiveness
- surface access and IO failures explicitly

### 7.2 Media device source

Planned main components:

- `MtpScanner`
- `MtpFileTransfer`

Core4 MTP rules:

- MTP is always sequential
- device calls must respect STA/COM requirements
- keepalive interval: 30 seconds
- operation timeout: 60 seconds
- retry backoff: 1s, 2s, 4s

Source selection must use `BackupPlan.SourceType`, not path heuristics.

---

## 8. Optional enrichment pipeline

These capabilities belong to later tiers and must be added explicitly.

### 8.1 Tier 3 components to introduce

Recommended internal interfaces/classes:

- `IFileTransfer`
- `ISidecarGenerator`
- `IMetadataReader`
- `IItemHasher`
- `IIntegrityVerifier`
- `ITimestampCorrector`
- `TimestampCorrectionResult`

### 8.2 Metadata and timestamp strategy

The normalized precedence for reading dates and metadata is:

1. `MetadataExtractor`
2. `ExifTool` fallback
3. filesystem attributes fallback

Important:

- Core4 should use **both** libraries in a controlled strategy, not ExifTool-only.
- Filesystem fallback belongs in the metadata output model, not as a hidden engine-level fallback.
- Timestamp correction should consume the normalized metadata result.

### 8.3 Sidecar generation

Sidecars are part of the normal per-file success path.

Normalized sidecar rules:

- filename suffix: `.sidecar.json`
- written after a successful transfer
- may later be enriched with hashes, metadata, verification outcome, and timestamp correction outcome

Do not reintroduce `.bmtp3.json` as the active Core4 convention.

---

## 9. Progress and reporting

### 9.1 Engine progress

The engine reports snapshots through:

```csharp
IProgress<IBackupProgress>?
```

The engine should emit progress on meaningful changes and should avoid noisy flooding. The normalized guidance is to debounce frequent updates to at most roughly every 500 ms when active work is ongoing.

### 9.2 UI and CLI reporting

Rich CLI output is valuable, but it should sit on top of the public engine contract.

That means:

- a CLI can derive ETA heuristics externally if desired
- a UI-specific notifier can exist later
- `IProgressNotifier` is **not** part of the current Core4 public baseline

---

## 10. Dependency injection and composition

The current skeleton does not yet contain the final Core4 DI composition root, so the architecture should stay conservative here.

Recommended composition direction:

- register `SequentialBackupEngine`
- register `LimitedParallelBackupEngine`
- register source-specific scanners and transfer services
- register `IBackupSessionStateStore`
- add a small resolver/factory that chooses engine implementation from `BackupPlan`

Important composition rules:

- MTP registrations must preserve sequential execution
- optional Tier 3 services must only be invoked when their flags are enabled
- not-yet-implemented flags must fail explicitly rather than being silently ignored

---

## 11. Testing implications

The architecture should support these test layers:

1. **Validation tests** for `BackupPlanValidator`
2. **Session state tests** for `BackupSessionState`
3. **Scanner tests** for filesystem and MTP discovery behavior
4. **Sequential engine integration tests** for end-to-end filesystem backup
5. **MTP integration tests** for device-safe transfer behavior
6. **Later-tier tests** for metadata, verification, timestamps, and limited parallelism

The first meaningful milestone is a minimal working Tier 1 backup, not a fully enriched Tier 3/4 engine.

---

## 12. Recommended class relationship map

```text
CLI / UI
  -> IBackupEngine
       -> SequentialBackupEngine
       -> LimitedParallelBackupEngine (Tier 4)

SequentialBackupEngine
  -> BackupPlanValidator
  -> IBackupSessionStateStore
       -> BackupSessionState
  -> IBackupScanner
       -> FileSystemScanner
       -> MtpScanner
  -> IFileTransfer (Tier 1/Tier 3 composition)
  -> ISidecarGenerator
  -> IMetadataReader (Tier 3)
  -> IItemHasher (Tier 3)
  -> IIntegrityVerifier (Tier 3)
  -> ITimestampCorrector (Tier 3)
```

This map is intentionally simpler than older drafts. It reflects the actual skeleton baseline and the intended growth path without mixing old and new plan shapes.

---

## 13. Implementation order

Recommended order for a coder:

1. Finish `SequentialBackupEngine`
2. Implement filesystem scanner and transfer
3. Implement sidecar generation
4. Implement progress snapshots
5. Implement MTP scanner and MTP transfer with the device-safety rules
6. Harden validation and error mapping
7. Add Tier 3 enrichment features
8. Add `LimitedParallelBackupEngine` for filesystem only
9. Add durable session persistence and resume
10. Add richer CLI/UI reporting

This order is deliberate: it preserves a minimal working Core4 before optional power features.

---

## 14. Final normalization summary

If another document disagrees with this one, prefer the following truths:

- `BackupPlan` uses `SourceType`, `Source`, `Destination`, and boolean feature flags
- `BackupResult` is the result type
- `IBackupScanner` streams `BackupItem`
- MTP is sequential
- metadata date reading uses `MetadataExtractor` first and `ExifTool` as fallback
- sidecars use `.sidecar.json`
- `MaxDegreeOfParallelism` is nullable and never uses `-1`
- richer UI reporting is later-tier, not the Tier 1 public baseline

For implementation details and step order, continue in `docs\CORE4_IMPLEMENTATION_GUIDE_DA.md`.
