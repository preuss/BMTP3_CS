# BackupEngineSequentiel - Requirements Specification

## Overview

`BackupEngineSequentiel` is a simplified, sequential implementation of `IBackupEngine` that processes backup items **one at a time** in a linear pipeline, without parallelization or bounded channels. This implementation prioritizes simplicity, debuggability, and correctness over performance.

While the current `BackupEngine` uses a complex multi-stage bounded-channel pipeline with concurrent workers, `BackupEngineSequentiel` reduces this to a straightforward sequential flow: **Scan → Stage → Metadata → Timestamp → Hash → Transfer → Inspector → Sidecar → Persist**.

---

## Core Behavior Analysis (from BackupEngine.cs)

### High-Level Execution Flow

The current `BackupEngine` executes these phases:

1. **Pre-flight Validation** (lines 101-108)
   - Validate backup plan
   - Check disk space (thresholds: >10GB no log, 1-10GB info, 100MB-1GB warning, <100MB fail)
   - Validate source exists and output is writable

2. **Session Initialization** (lines 110-125)
   - Create a `BackupSessionEntity` with unique `sessionId`
   - Save session state to repository (unless DryRun)
   - Used for resume support on crash/cancellation

3. **Progress Tracking Setup** (lines 127-160)
   - Initialize `ProgressTracker` with `BackupPhase.Starting`
   - Launch a background reporting task that samples tracker every 250ms
   - Report progress via `IProgress<IBackupProgress>` to caller

4. **Pipeline Construction** (lines 162-238)
   - Determine parallelism degree (debug single-thread, or processor-based, or configured)
   - Create bounded channels for inter-stage queuing
   - Instantiate pipeline stages: Buffering, Metadata, Timestamp, Hash, Transfer, Inspector, Sidecar
   - **Key constraint**: Buffering stage forced to parallelism=1 (MTP device stability)

5. **MTP Device Session Management** (lines 247-251)
   - If source is MTP and scanner supports it, open device session before pipeline
   - Keep session alive until buffering completes (all file content read)
   - Dispose after bufferingTask completes (via ContinueWith with ExecuteSynchronously)

6. **Pipeline Execution** (lines 253-357)
   - **Producer**: Scans source, yields items, tracks discoveries, writes to scanChannel
   - **Buffering**: ContentBufferingPipelineStage stages file content (reads from source)
   - **Metadata**: MetadataExtractionPipelineStage reads metadata
   - **Timestamp**: TimestampCorrectionPipelineStage corrects timestamps
   - **Hash**: HashPipelineStage computes hashes
   - **Transfer**: TransferPipelineStage copies files to output
   - **Inspector**: DestinationInspectorPipelineStage validates transferred files
   - **Sidecar**: SidecarGenerationPipelineStage generates metadata sidecars
   - **Completion**: Persists item state, saves session periodically (every 10 items)

7. **Result Compilation** (lines 421-448)
   - Decide final status: Cancelled > Failed (any item failed) > Completed
   - Populate `BackupJobResult` with counts and timing
   - Save final session state to repository (unless DryRun)

8. **Error Handling & Cleanup** (lines 345-416)
   - Catch and convert pipeline exceptions to job result failures
   - Dispose MTP session gracefully on errors
   - Cancel reporting task safely

---

## Requirements for BackupEngineSequentiel

### 1. Interface Compliance

- **MUST** implement `IBackupEngine` interface:
  ```csharp
  Task<BackupJobResult> RunAsync(BackupPlan job, IProgress<IBackupProgress> progress, CancellationToken ct)
  ```
- **MUST** accept same dependencies as `BackupEngine` (scanner, staging, metadata, hashing, transfer, etc.)
- **MUST** return a properly populated `BackupJobResult` with status, counts, and timing

### 2. Pre-flight Validation

- **MUST** validate the backup plan using `IJobValidator`
- **MUST** validate disk space with same thresholds:
  - Throw if < 100 MB free
  - Warn if 100 MB - 1 GB
  - Info if 1 GB - 10 GB
- **MUST** validate source exists (filesystem) or is accessible (MTP)
- **MUST** validate output directory is writable (create if needed, test write access)

### 3. Cancellation & Error Handling

- **MUST** respect `CancellationToken` at every checkpoints (scan, each item processing, persistence)
- **MUST** gracefully handle `OperationCanceledException` and mark result as `Cancelled`
- **MUST** catch and report pipeline exceptions as job-level errors (not per-item)
- **MUST** dispose MTP session on errors and cancellation
- **MUST** not propagate exceptions—always return a valid `BackupJobResult`

### 4. Progress Reporting

- **MUST** initialize progress tracker with phases: Starting → Traversing → Transferring → Completed
- **MUST** report progress asynchronously (background task sampling every 250ms)
- **MUST** delegate to the same `ProgressTracker` logic as `BackupEngine`
- **MUST** include per-file staging updates via `stagingProgress` adapter

### 5. Sequential Pipeline Execution

Unlike the current multi-stage parallel pipeline, the sequential engine processes items **linearly**:

```
FOR EACH item from scanner:
  1. Buffer (stage) content from source
  2. Extract metadata
  3. Correct timestamp
  4. Compute hash(es)
  5. Transfer to output
  6. Inspect destination
  7. Generate sidecar
  8. Persist state
```

- **MUST** process one item at a time (no channels, no worker pools)
- **MUST** instantiate the same step classes (e.g., `ContentBufferingItemStep`, `MetadataExtractionItemStep`, etc.) but call them sequentially
- **MUST** call `tracker.UpdatePhase(item, step.Phase)` and `CreateProgressReporter(item)` as before
- **MUST NOT** use bounded channels or concurrent workers

### 6. MTP Device Session Handling

- **MUST** support MTP sources via scanner's `OpenSession()` method (if `IMtpCapableScanner`)
- **MUST** keep session alive for entire sequential pipeline (all items need device content)
- **MUST** dispose session after all items are processed (or on error/cancellation)
- **SHOULD** avoid device timeouts by staying connected throughout

### 7. Session & Resume Support

- **MUST** create `BackupSessionEntity` with unique `sessionId` at startup
- **MUST** save initial session state to repository before processing (unless DryRun)
- **MUST** persist item state after each item completes (unless DryRun)
- **SHOULD** batch persistence every N items (e.g., every 10) to reduce I/O
- **MUST** save final session state to repository on completion (unless DryRun)

### 8. Per-Item State Management

- **MUST** track discoveries (files found during scan) with byte counts
- **MUST** track per-item outcomes: succeeded, failed, skipped
- **MUST** extract and store item metadata (length, date, etc.) from source
- **MUST** handle item result states (Success, Skipped, Failed, etc.)
- **MUST** populate `BackupJobResult` with final counts:
  - `FilesCopied` = files succeeded
  - `FilesFailed` = files failed
  - `FilesSkipped` = files skipped
  - `TotalFilesScanned` = total discoveries
  - `TotalBytesCopied` = bytes processed

### 9. Item Processing Steps

Each item must be processed through these steps (in order):

| Step | Class | Input | Output | Key Behavior |
|------|-------|-------|--------|--------------|
| Buffer | `ContentBufferingItemStep` | Unprocessed item | Staged item | Downloads/reads file content from source |
| Metadata | `MetadataExtractionItemStep` | Staged item | Item with metadata | Reads file metadata (dates, size, attributes) |
| Timestamp | `TimestampCorrectionItemStep` | Item + metadata | Item + corrected timestamp | Adjusts file timestamps if configured |
| Hash | `HashItemStep` | Item + corrected data | Item + hash values | Computes configured hash types (MD5, SHA256, etc.) |
| Transfer | `TransferItemStep` | Item + hash | Transferred item | Copies file to output, resolves collisions |
| Inspector | `DestinationInspectorItemStep` | Transferred item | Validated item | Verifies transferred file integrity (hash check, size) |
| Sidecar | `SidecarGenerationItemStep` | Validated item | Item + sidecar | Generates metadata XML/JSON sidecar file |
| Persist | (inline) | Complete item | Result state | Saves item state to repository (unless DryRun) |

### 10. DryRun Mode

- **MUST** support `plan.DryRun` flag
- **MUST NOT** write to output filesystem in DryRun mode
- **MUST NOT** write to repository in DryRun mode
- **MUST** still process all items through the pipeline
- **MUST** still report progress as if real

### 11. Result Status Logic

Final status determination (in order of precedence):

1. If `ct.IsCancellationRequested` → **Cancelled**
2. Else if any items failed (`tracker.FilesFailed > 0`) → **Failed**
3. Else → **Completed**

- **MUST** include failure summary in `result.GlobalErrors`
- **MUST** set `result.Status` and `result.EndTime` correctly
- **MUST** log job outcome to `_logger` (including sessionId, file counts, status)

### 12. Dependency Injection

Same constructor as `BackupEngine`:

```csharp
public BackupEngineSequentiel(
    IBackupScanner backupScanner,
    IStagingDownloader stagingDownloader,
    IMetadataReader metadataReader,
    IItemHasher itemHasher,
    IPathGenerator pathGenerator,
    ICollisionResolver collisionResolver,
    IFileTransfer fileTransfer,
    IHashGenerator hashGenerator,
    IBackupRepository repository,
    ISidecarGeneratorFactory sidecarGeneratorFactory,
    IJobValidator validator,
    IOptions<BackupEngineOptions> options,
    ILoggerFactory loggerFactory,
    IDestinationInspector destinationInspector
)
```

- **MUST** validate all parameters (null check)
- **MUST** create logger from factory
- **MAY** simplify or remove some dependencies if not used in sequential flow (but keep for now for compatibility)

---

## Simplifications Over Current BackupEngine

1. **No Channels**: Remove all `Channel<T>` creation—process items directly
2. **No Worker Pools**: No `ContentBufferingPipelineStage`, etc.—call steps directly
3. **No Parallelism**: Single-threaded per-item processing
4. **Same Progress Reporting**: Keep background reporting task (250ms samples)
5. **Same MTP Handling**: Open/close device session same way
6. **Same Validation & Cleanup**: Same disk space checks, source/output validation
7. **Same Result Compilation**: Same status logic, counts, logging

---

## Testing Strategy (Not Scope of This Spec)

Tests will verify:

- Proper completion with all items processed
- Correct status transitions (Ready → Running → Completed/Failed/Cancelled)
- Per-item state tracking
- MTP session lifecycle (open before scan, close after sidecar)
- Cancellation at various points
- DryRun mode (no files written, repository untouched)
- Disk space validation
- Error handling and logging

---

## Known Limitations & Open Questions

1. **Performance**: Sequential processing will be slower than parallel. This is intentional for debugging.
2. **DegreeOfParallelism**: Not used in sequential flow. May remove from constructor or ignore.
3. **Locale-Dependent Date Formatting**: Date/time formatting depends on system locale.
4. **CLDR Plural Categories**: Plural formatting uses simplified numeric ranges (zero, one, few, many).

---

## Implementation Approach

1. Create `BackupEngineSequentiel.cs` with same structure as `BackupEngine`
2. Replace pipeline channel logic with direct sequential method calls
3. Keep all validation, session management, and progress reporting identical
4. Verify all tests pass
5. Document any behavioral differences from `BackupEngine`

