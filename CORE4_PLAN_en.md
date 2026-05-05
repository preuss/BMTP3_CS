# Core4 - Hybrid Backup Engine (New Design)

## 📌 Introduction: What is Core4 and Why?

### The Background

Over the years, we have developed several backup engines in BMTP3:

- **Core (Original)**: Works fine, but completely sequential. Worked when we made it, but was refactored and no longer works. It became too complex to maintain.

- **Core2 (Multithreaded)**: We wanted to make it faster, so we added parallel processing with System.Threading.Channels and worker-pools. **Problem**: It was never stable. Race conditions, deadlocks, and unexpected behavior especially with MTP-devices. The code became so complex it was almost impossible to debug.

- **Core3 (Simple Sequential)**: We went back to basics - single-threaded, simple code, easy to understand. **It works!** But it's slow for large backups, and we still need to support all features (hashes, metadata, verification).

### The Vision for Core4

**Core4 is the solution** that combines the best from all three:

- ✅ **Simple to understand** (like Core3) - linear flow, easy to debug
- ✅ **All features** (like Core2) - hash, metadata, EXIF, verification
- ✅ **Intelligent performance** - parallel when it makes sense (filesystem), sequential when it needs to be stable (MTP)
- ✅ **Maintained** - new, clean codebase without legacy baggage

### The Core Idea

**"Build it simple. Build it right. Then optimize smartly."**

Instead of making **everything** parallel like Core2, or **everything** sequential like Core3, Core4 should be **adaptive**:

`
If the source is an MTP device:
  → Use Sequential (simple, stable)
  
If the source is Filesystem:
  → Use Limited Parallelism (fast, but not chaotic)
`

This approach means:
- MTP backups are **stable** (no device-disconnects from parallelism)
- Filesystem backups are **fast** (without CPU waste)
- The code is **understandable** (each strategy is separate and simple)

---

## 🔍 Lessons Learned from Core3 - Pitfalls Core4 Must Avoid

During code review of Core3, we found **5 critical bugs** and **6 design flaws** that Core4 must handle better. Here they are:

### 🔴 Critical Bugs from Core3

**Bug #1: Directory Structure Goes Lost**
- Core3 scanner recursively traverses directories, but sets DestinationPath = fileName (just filename)
- Result: Folder1/File1.txt and Folder2/File1.txt both become File1.txt (collision!)
- **Core4 Fix:** Preserve relative paths. DestinationPath = Path.Combine(destination, relativePath)

**Bug #2: Transfer Errors Kill Entire Backup**
- One file fails → entire backup stops with 	hrow
- If file #500 of 5000 fails, the rest never gets backed up
- **Core4 Fix:** Log error, mark file as failed, **continue with next file**

**Bug #3: Destination Directory Must Pre-Exist**
- Transfer crashes if output-directory doesn't exist
- **Core4 Fix:** Directory.CreateDirectory(destinationPath) before transfer

**Bug #4: Progress Reporting Incomplete**
- IBackupProgress.BytesTransferred defined in interface, but not implemented
- **Core4 Fix:** Complete implementation with actual bytes transferred

**Bug #5: Dry-Run Mode Incomplete**
- Dry-run doesn't create directories, so metadata/hash-phases fail
- **Core4 Fix:** Full simulation - create directory-structure even in dry-run

### 🟡 Design Flaws from Core3

**Design Flaw #1: Error Handling Strategy is Implicit**
- Transfer errors → critical (stop)
- Metadata errors → tolerable (continue)
- But there's no way for user to configure this
- **Core4 Fix:** Explicit ErrorStrategy enum per operation:
`csharp
public enum ErrorStrategy { StopOnError, SkipOnError, RetryOnError }
public ErrorStrategy TransferErrorStrategy { get; init; } = ErrorStrategy.StopOnError;
public ErrorStrategy MetadataErrorStrategy { get; init; } = ErrorStrategy.SkipOnError;
`

**Design Flaw #2: Hashing Always Forced (Slow)**
- Core3 computes 3 hash types **always**, even if user doesn't want them
- Hashing is slow for large files
- **Core4 Fix:** Hashing should be **optional** (null means no hashing)

**Design Flaw #3: Sidecars Generated Last**
- If hashing/metadata fails, sidecar is never generated
- **Core4 Fix:** Generate **minimal sidecar after transfer**, update it later with metadata/hashes

**Design Flaw #4: Collision Resolution Not Testable**
- CollisionStrategy has 4 options (Rename, Skip, Overwrite, Error)
- But scanner and transfer handle it implicitly
- **Core4 Fix:** Separate, testable CollisionResolution step

**Design Flaw #5: Immutable Flow is Actually Mutable**
- Items modified in-place via list indexing
- **Core4 Fix:** Document clearly: items are mutable list, not immutable stream

**Design Flaw #6: Cancellation Handling Inconsistent**
- Mixed use of ct.ThrowIfCancellationRequested() and catch(OperationCanceledException)
- **Core4 Fix:** Consistent pattern throughout

### 📚 10 Lessons for Core4 Implementation

1. **Always preserve directory structures** - relative paths must be preserved from source to destination
2. **Error strategy must be explicit and configurable** - define per operation type
3. **Make features optional** - especially hashing, which is slow
4. **Generate sidecars early** - after transfer, before optional features
5. **Auto-create destination directories** - no throw if destination doesn't exist
6. **Separate collision resolution** - make it testable, clear, explicit
7. **Complete progress reporting** - BytesTransferred, ETA, speed, etc.
8. **Test edge cases thoroughly**:
   - Empty directories
   - Permission denied errors
   - Disk full
   - Cancelled operations at various stages
   - Deep directory structures
   - Very large files
   - Duplicate filenames
9. **Document error policies clearly** - which errors stop backup, which are tolerated, how they're logged
10. **Consistent cancellation handling** - pick one pattern, use everywhere

---

## ⚠️ Why NOT Core2's Approach - What We Learned from Channel/Pipeline Architecture

During review of Core2, we found **5 critical threading-issues** that Core4 must avoid:

### 🔴 Critical Threading-Issues from Core2

**Issue #1: Deadlock Risk - Multiple Concurrent Writers to Bounded Channels**
- Core2 uses System.Threading.Channels with multiple workers writing simultaneously
- Bounded channels (with limited capacity) can deadlock if:
  - Writer thread A fills the channel, blocks
  - Writer thread B also tries to write, blocks
  - But reader is blocked elsewhere
  - **Circular deadlock**
- **Core4 Fix:** Avoid complex channel pipelines. Use simple sequential or carefully-designed parallel.

**Issue #2: Race Conditions in Shared ProgressTracker**
- Core2 ProgressTracker is shared amongst multiple worker threads without synchronization
- Race condition: snapshot may show partially-updated state
- Lost updates on progress counters
- **Core4 Fix:** Use thread-safe collections OR avoid shared mutable state

**Issue #3: Implicit Error Handling Across Stages**
- Core2 forwards all items to next stage, even if they failed
- Next stage must check if (item.ResultState == ItemResultState.Failed)
- Error policy is **implicit** and **unclear**
- **Core4 Fix:** Explicit error policies per operation

**Issue #4: Task Orchestration is Manual and Error-Prone**
- Core2 chains 9+ tasks manually
- Easy to miss an wait Task.WhenAll → deadlock
- Manual task management = fragile
- **Core4 Fix:** Simple orchestration - direct method calls, not magic task chaining

**Issue #5: Debugging is Nearly Impossible**
- 9 channels, 8 worker pools, shared progress tracker, implicit error forwarding
- When something goes wrong, very hard to trace root cause
- "Sophisticated but fragile" system
- **Core4 Fix:** Prioritize simplicity over theoretical efficiency

### 🟡 Design Flaws from Core2

**Design Flaw #1: Coupling Between Stages is Implicit**
- Each stage doesn't know what comes next, must forward all items
- Downstream stages must understand the protocol
- Hard to reason about data flow

**Design Flaw #2: Bounded Channels Make Pipeline Brittle**
- Configuration-dependent: wrong capacity = deadlock or memory explosion
- Unbalanced stages → bottlenecks → deadlock
- Hard to tune correctly

**Design Flaw #3: MTP Session Management is Hacky**
- Managing MTP session lifetime is "bolted on" to pipeline orchestration
- Difficult to reason about lifecycle
- Risk of using device after disconnect

---

### 📚 7 Lessons for Core4 from Core2 Mistakes

1. **Avoid complex channel pipelines** - Don't build 9-channel infrastructure
2. **Thread-safety is hard** - Race conditions are subtle, deadlocks are deadly
3. **Explicit beats implicit** - Define clear error policies, don't forward implicitly
4. **Deadlock risk is real** - Multiple writers to bounded channels = danger
5. **Simplicity aids debugging** - Make data flow obvious, not hidden in task chaining
6. **Device management is tricky** - MTP sessions need explicit lifecycle management
7. **Test with real devices** - Don't rely on mocks, test actual MTP stability

### 🔍 13 Detailed Edge Cases & Design Quirks

Below is a list of 13 specific problems Core2 has, which Core4 must avoid:

**1. Multiple Writers to Bounded Channels (DEADLOCK RISK)**
- Core2 creates channels with SingleWriter=false, but 4+ worker threads write simultaneously
- If the channel is full → writers block
- If reader is also blocked elsewhere → circular deadlock
- **Core4 Fix:** Either single writer per channel OR completely avoid channels

**2. ProgressTracker Race Condition (DATA CORRUPTION)**
- ProgressTracker is shared amongst all worker threads
- No synchronization on reads/writes
- Result: Inconsistent progress snapshots (e.g. 150% done)
- **Core4 Fix:** Thread-safe collections (ConcurrentDictionary) or locks

**3. MTP Session Lifecycle Fragile (DISCONNECT RISK)**
- Session kept open only until bufferingTask completes
- If there's delay between buffering and later use of temp files → error
- If device unplugging during buffering → no explicit error handling
- **Core4 Fix:** Explicit guards and timeout management

**4. Manual Pipeline Dependency Tracking (DEBUGGING NIGHTMARE)**
- 9 tasks, dependencies implicit via channels
- If one task hangs → entire pipeline hangs, but Task.WhenAll sees no problem
- Debugging requires understanding channel topology, not task graph
- **Core4 Fix:** Simple sequential orchestration or explicit dependency tracking

**5. Generic Exception Handling (ERROR CONTEXT LOST)**
- Exceptions converted to strings in GlobalErrors
- Original exception context lost for downstream processing
- **Core4 Fix:** Structured error logging with exception objects

**6. Implicit Error Policies Per Stage (INCONSISTENT BEHAVIOR)**
- What if metadata extraction fails? → implicitly skip and continue
- What if transfer fails? → implicitly mark failed and continue
- User cannot configure this
- **Core4 Fix:** Explicit ErrorStrategy enum per operation

**7. DryRun Incomplete (WASTED RESOURCES)**
- DryRun skips I/O persistence but ONLY that
- Still downloads from MTP, computing 7 hashes, reading metadata
- Result: DryRun almost as slow as real run
- **Core4 Fix:** Skip expensive operations when DryRun=true

**8. Items Mutable Despite "Immutable" Design (CONFUSING API)**
- Items documented as immutable flow
- But items are mutable references shared across pipeline
- Downstream stages see modifications from upstream stages
- **Core4 Fix:** Document that items are mutable OR make them truly immutable

**9. Redundant Result Fields (API CONFUSION)**
- BackupJobResult has both GlobalErrors (List<string>) and GlobalError (string?)
- Both exist, unclear which is used when
- **Core4 Fix:** Single GlobalError field or List<string> GlobalErrors, not both

**10. FileErrors vs FailedItems Confusion (INCONSISTENT POPULATION)**
- FileErrors: List<string> descriptions
- FailedItems: List<ErrorLog?> structured errors
- Unclear when each is populated and by whom
- **Core4 Fix:** Single structured error collection

**11. Channel Capacity Tuning Mystery (OOM RISK)**
- All channels default to capacity=128
- If file count is 10,000 OK; if each file is 100MB → 12.8GB buffered, OOM
- Configuration options exist but undocumented
- **Core4 Fix:** Document capacity tuning or make adaptive

**12. Inconsistent Cancellation Patterns (MAINTENANCE BURDEN)**
- Mixed use of ct.ThrowIfCancellationRequested(), catch(OperationCanceledException), if(ct.IsCancellationRequested)
- Three patterns in same codebase is error-prone
- **Core4 Fix:** Pick ONE pattern, use consistently everywhere

**13. Resume Infrastructure Incomplete (FEATURE DOESN'T WORK)**
- Session state saved to repository for resume support
- But no code to actually resume from saved state
- If backup crashes → restart from scratch
- **Core4 Fix:** Implement fully or remove entirely

---

## Executive Summary

**Core4** is a completely new implementation of the backup engine, combining the best from Core, Core2, and Core3:

- 🎯 **Hybrid architecture**: Sequential for MTP, Limited Parallelism for Filesystem
- 🧩 **Incrementally implementable**: Core-features first, then extended
- 🔧 **New codebase**: Not dependent on Core3's components
- 🚀 **All Core2-features**: But simple and maintainable
- 📊 **Intelligent parallelism**: Based on source-type without complexity

---

## What does "Hybrid" mean?

### The word "Hybrid" means two approaches in one

Instead of **one** strategy that must fit **everything**, Core4 has **two strategies**:

**1. Sequential Strategy (For MTP-devices)**

When you backup from an MTP-device (iPhone, Android phone, digital camera):
`
Scan file 1 → Transfer → Sidecar → Metadata → Hash → Verify → Next
Scan file 2 → Transfer → Sidecar → Metadata → Hash → Verify → Next
Scan file 3 → Transfer → Sidecar → Metadata → Hash → Verify → Next
...
`

**Why sequential?** The MTP protocol is finicky. If you try to read from the device while transferring in parallel, it crashes. Therefore, stability is more important than speed.

**2. Limited Parallelism Strategy (For Filesystem)**

When you backup from a local folder or network drive:
`
Thread 1: Transfer file 1     Thread 2: Transfer file 2     Thread 3: Transfer file 3
  ↓                              ↓                              ↓
Sidecar 1                    Sidecar 2                    Sidecar 3
  ↓                              ↓                              ↓
[Parallel pool for Hash/Meta]   [Parallel pool]              [Parallel pool]
  ├─ Hash 1 ║ Metadata 1        ├─ Hash 2                     ├─ Hash 3
  └─ ...                        └─ ...                        └─ ...
`

**Why parallel?** The filesystem is stable. We can read many files simultaneously without problem. This makes backup **much faster**.

**Controlled parallelism** means we don't go wild - we set a max limit (e.g. 4 threads) so the system doesn't get overloaded.

---

## Architecture Overview

### Overall Flow

`
BackupPlan (Input)
    ↓
[Source Detection] (MTP vs. Filesystem?)
    ↓
┌─────────────────────────────────────┐
│   If MTP → Sequential Strategy      │
│   If Filesystem → Limited Parallel  │
└─────────────────────────────────────┘
    ↓
Core Pipeline:
  1. ScanPhase → Enumerate items
  2. TransferPhase → Copy files to output
  3. GenerateSidecarPhase → Create metadata files
    ↓
Extended Pipeline (if active):
  4. GenerateHashesPhase → SHA256 per file
  5. ExtractMetadataPhase → EXIF, tags, etc.
  6. VerifyIntegrityPhase → Check hashes against sidecar
  7. CorrectTimestampsPhase → Restore original timestamps
    ↓
BackupJobResult (Output)
`

### What are the different phases?

**Phase 1: Scanning** 
First we need to figure out what needs to be backed up. We scan the source (MTP-device or folder) and get a list of all files. Each file has:
- Source-path (where the file is)
- File size (how big is it)
- Timestamps (when was it created)
- Metadata (if we can read it)

**Phase 2: Transfer**
Then we copy each file from the source to the output-directory. This is the most important phase - if transfer fails, there's no backup. We report progress as we go so the UI can show "50% complete".

**Phase 3: Sidecar Generation**
For each file we transferred, we create a .json file (sidecar) that contains metadata about the transfer:
- Where the file came from
- Where the file ended up
- When was it transferred
- Was it successful
- (Later: hashes, metadata, verification status)

Sidecars are **important** because:
- They document what happened
- You can audit backups later
- You can restore without having the source
- They enable verification and forensics

**Phase 4-7: Extended Features (Optional)**
If the user enables it, we can:
- Compute SHA256 hashes (for integrity)
- Extract EXIF from images (for timeline)
- Verify that destination files match source (integrity check)
- Restore original timestamps (so files look like originals)

### Phase Strategy

#### Sequential (MTP)
`
Scan Item 1
  ↓ Transfer
  ↓ GenerateSidecar
  ↓ [Optional: Hash + Metadata + Verify + Timestamps]
Scan Item 2
  ↓ Transfer
  ...
`

One file at a time. Safe, simple, stable. MTP loves this approach.

#### Limited Parallelism (Filesystem)
`
Thread 1: Transfer File 1        Thread 2: Transfer File 2
  ↓                               ↓
GenerateSidecar 1              GenerateSidecar 2
  ↓                               ↓
[Parallel Stages]              [Parallel Stages]
  ├─ Hash 1                      ├─ Hash 2
  ├─ Metadata 1                  ├─ Metadata 2
  └─ Timestamps 1                └─ Timestamps 2
`

Multiple files simultaneously. Faster, but controlled. Filesystem can handle it.

---

## Core4 API Specifications (Must-Have for Implementation)

### Enumerations & Types

**Source Type Detection:**
`csharp
public enum SourceType
{
    Filesystem,      // Local folder or network drive
    MediaDevice      // MTP device (iPhone, Android, camera)
}

public enum BackupPhase
{
    NotStarted = 0,
    Initializing = 1,
    Scanning = 2,
    Transferring = 3,
    Hashing = 4,
    MetadataExtraction = 5,
    Verification = 6,
    TimestampCorrection = 7,
    Completed = 100,
    Failed = 101,
    Cancelled = 102
}

public enum BackupJobStatus
{
    NotStarted,
    Running,
    Completed,
    PartialSuccess,  // Some files failed
    Failed,
    Cancelled
}

public enum ErrorHandlingStrategy
{
    StopOnError,     // Single error stops entire backup
    SkipOnError,     // Skip failed item, continue
    RetryOnError     // Retry N times before skipping
}

public enum BackupErrorCode
{
    // Scan errors (1000-1999)
    ScanDirectoryNotFound = 1001,
    ScanAccessDenied = 1002,
    ScanPathInvalid = 1003,
    ScanIOError = 1004,
    
    // Transfer errors (2000-2999)
    TransferSourceNotFound = 2001,
    TransferDestinationFull = 2002,
    TransferAccessDenied = 2003,
    TransferIOError = 2004,
    TransferTimeout = 2005,
    
    // MTP-specific (3000-3999)
    MTPDeviceNotFound = 3001,
    MTPDeviceDisconnected = 3002,
    MTPSessionTimeout = 3003,
    MTPAuthenticationFailed = 3004,
    
    // System errors (9000+)
    OutOfMemory = 9001,
    DiskFull = 9002,
    UserCancelled = 9003
}
`

### Core Interfaces & Records

**IBackupItem (What flows through pipeline):**
`csharp
public interface IBackupItem
{
    string Id { get; }                              // Unique ID within backup
    string SourcePath { get; }                      // Original path
    string RelativePath { get; }                    // Relative to source root
    long Size { get; }                              // File size in bytes
    DateTime ModifiedDate { get; }                  // Original modified time
    Dictionary<string, object>? Metadata { get; }   // Dynamic metadata container
    BackupItemStatus Status { get; set; }           // Processing status
    List<BackupError>? Errors { get; }              // Per-item errors
}

public enum BackupItemStatus
{
    Pending,       // Discovered, awaiting transfer
    Transferred,   // Successfully copied
    Failed,        // Transfer/processing failed
    Skipped,       // Intentionally skipped
    Verified       // Post-transfer verification passed
}

public record BackupError(
    BackupErrorCode Code,
    string Message,
    string? Details = null,
    Exception? Exception = null
);
`

**Contexts (Parameter objects for phases):**
`csharp
public record ScanContext(
    string SourcePath,
    bool Recursive,
    List<string>? IncludePatterns,
    List<string>? ExcludePatterns
);

public record TransferContext(
    string OutputDirectory,
    CollisionResolutionType CollisionResolution,
    RenameStrategy? RenameStrategy
);

public record SidecarContext(
    string OutputDirectory,
    SidecarFormat Format = SidecarFormat.Json,
    bool IncludeHashes = false,
    bool IncludeMetadata = false,
    bool IncludeVerification = false
);

public enum SidecarFormat { Json, Xml }
public enum CollisionResolutionType { Skip, Rename, Overwrite, Error }
public enum RenameStrategy { Increment, Timestamp, Guid }
`

**BackupProgress (Progress snapshot):**
`csharp
public record BackupProgress(
    // Scanning metrics
    int DirectoriesScanned,
    int FilesDiscovered,
    long BytesTotal,
    
    // Processing metrics
    int FilesProcessed,
    int FilesSucceeded,
    int FilesFailed,
    int FilesSkipped,
    long BytesProcessed,
    
    // Current work
    string? CurrentFilePath,
    long CurrentFileBytes,
    long CurrentFileBytesProcessed,
    
    // Calculated metrics
    double PercentageComplete => 
        FilesDiscovered > 0 ? (double)FilesProcessed / FilesDiscovered * 100 : 0,
    double BytesPerSecond => 
        ElapsedMs > 0 ? (BytesProcessed * 1000) / ElapsedMs : 0,
    TimeSpan EstimatedTimeRemaining =>
        BytesPerSecond > 0 ? TimeSpan.FromSeconds((BytesTotal - BytesProcessed) / BytesPerSecond) : TimeSpan.Zero,
    
    // Status
    BackupPhase CurrentPhase,
    long ElapsedMs
);
`

**BackupJobResult (Final output):**
`csharp
public record BackupJobResult(
    bool Success,
    BackupJobStatus Status,
    string JobName,
    DateTime StartTime,
    DateTime EndTime,
    
    // Statistics
    int TotalFilesScanned,
    int FilesSucceeded,
    int FilesFailed,
    int FilesSkipped,
    long TotalBytesScanned,
    long TotalBytesTransferred,
    
    // Errors
    List<string>? GlobalErrors,              // Fatal errors
    Dictionary<string, List<string>>? FileErrors,  // Per-file errors
    
    // Completion
    double PercentageComplete,
    double AvgTransferSpeedMBps,
    BackupPhase FinalPhase
)
{
    public TimeSpan Duration => EndTime - StartTime;
    public bool IsPartialSuccess => FilesSucceeded > 0 && FilesFailed > 0;
}
`

### DryRun Behavior Specification

`
DryRun Mode Details:

Operation                  | Executes? | Notes
──────────────────────────────────────────────────────────
File scanning              | YES       | User must know scope
Directory enumeration      | YES       | Need to find files
File size calculation      | YES       | For progress/warnings
Directory structure create | SIMULATED | Logged but not created
File transfer              | NO        | Don't modify user filesystem
Sidecar creation           | SIMULATED | Logged but not written
Hash computation           | NO        | Expensive, no point
Metadata extraction        | NO        | Expensive, no point
Verification               | NO        | No transferred files
Database persistence       | NO        | Don't save state

Result: User sees "Would backup 500 files, 2.5 GB" without I/O
Benefits: Verify settings before real backup, estimate time/storage
`

### Error Handling Strategy

`
Error Policy Per Phase:

┌─ Scan Phase
│  ├─ Directory not found → Log warning, skip directory
│  ├─ Permission denied → Log warning, skip directory
│  └─ Action: Continue scanning remaining directories
│
├─ Transfer Phase
│  ├─ Source file deleted → Log error, mark failed, continue
│  ├─ Destination disk full → Log error, mark failed, continue
│  ├─ Permission denied → Log error, mark failed, continue
│  └─ Action: Mark item failed, continue with next file
│
├─ Optional Phases (Hashing, Metadata, Verification)
│  ├─ Timeout → Log warning, skip enhancement
│  ├─ Parse error → Log warning, skip enhancement
│  └─ Action: Continue, item still usable
│
└─ Engine Level
   ├─ On any critical error → Return BackupJobStatus.Failed
   ├─ On per-file errors → Return BackupJobStatus.PartialSuccess
   └─ On user cancel → Return BackupJobStatus.Cancelled

Collection:
├─ GlobalErrors: List<string> - Fatal errors stopping backup
├─ FileErrors: Dictionary<itemId, List<string>> - Per-file errors
└─ Detailed logging via ILogger<T>
`

### MTP Session Lifecycle

`
MTP Device Management:

OpenSession(plan)
  ├─ Verify device connected (timeout: 30 sec)
  ├─ Authenticate if needed
  ├─ Validate device accessible
  └─ Return IMtpDeviceSession

ScanPhase:
  ├─ Keep session alive (periodic keep-alive ping)
  ├─ Enumerate device contents
  └─ On disconnect: Log error, abort scan

TransferPhase:
  ├─ Open file handle on device
  ├─ Stream content to temp file
  ├─ On MTP timeout: Retry with exponential backoff (3 attempts)
  └─ On device disconnect: Abort item, mark failed, continue

CloseSession():
  ├─ Disconnect gracefully
  ├─ Cleanup temp files
  └─ Finally block ensures cleanup on any error

Timeout Strategy:
├─ Per-operation timeout: 60 seconds (configurable)
├─ Session keep-alive: Every 30 seconds
└─ Retry: 3 attempts with 1s, 2s, 4s backoff
`

### Parallelism Strategy (Limited Parallel)

`
Thread Pool Configuration (Filesystem only):

MaxWorkers = Math.Min(4, Environment.ProcessorCount / 2)
QueueDepthLimit = 50                      // Backpressure control
TransferBufferSize = 1 MB                 // Chunk size
ProgressUpdateInterval = 500 ms           // How often to report

Pipeline Stages (Worker Threads):
├─ Scanner (1 thread) → produces items
├─ Transferor (N threads) → reads from TransferQueue
├─ SidecarGenerator (N threads) → reads items, creates sidecars
├─ [Optional] Hasher (N threads) → computes hashes
├─ [Optional] Metadata (N threads) → extracts metadata
└─ [Optional] Verifier (N threads) → checks integrity

Synchronization:
├─ Queue<IBackupItem> for thread-safe handoff
├─ SemaphoreSlim(maxWorkers) to limit active threads
└─ ConcurrentDictionary for progress (thread-safe)

Deadlock Prevention:
├─ No circular dependencies between stages
├─ Unbounded queues (not bounded channels like Core2)
├─ Timeout on queue operations (prevents hanging)
└─ All stages have explicit error handling

Backpressure:
├─ If TransferQueue > 50 items, Scanner waits
├─ Prevents buffering large files in memory
└─ Balances producer/consumer speed
`

---

## What Needs to Be Built: Overview

Core4 consists of several "layers" (layers):

`
┌─────────────────────────────────────────┐
│ API LAYER (Contracts)                   │
│ → Defines what should happen            │
│ → No implementation, only interfaces    │
└─────────────────────────────────────────┘
        ↓ ↓ ↓ ↓ ↓
┌─────────────────────────────────────────┐
│ IMPLEMENTATION LAYER                    │
│ → Scanner, Transfer, Hashing, etc.      │
│ → Each component does one thing well    │
└─────────────────────────────────────────┘
        ↓ ↓ ↓ ↓ ↓
┌─────────────────────────────────────────┐
│ ORCHESTRATION LAYER (The Engine)        │
│ → BackupEngine controls entire flow     │
│ → Sets the strategy (Sequential/Parallel)
└─────────────────────────────────────────┘
`

This structure means:
- If you need a new scanner (e.g. for Google Drive), you only build that, reuse the rest
- If hash-logic needs to change, you only change Hasher, not Transfer or Scanner
- If you need to test Transfer, you can fake Scanner/Hasher

This is called "Separation of Concerns" - each component has **one responsibility**.

### 1. **API Layer** (Contracts & DTOs)

**Files:** Api/ folder

**Responsibility:**
Define the interfaces and data-structures that all of Core4 uses. Think of it as the "contract" between components.

**What should be here:**
- IBackupEngine interface - "How do we run a backup?"
- BackupPlan record - "What is input to a backup?"
- BackupJobResult record - "What is the result?"
- IBackupProgress interface - "How do we report progress?"
- IBackupItem interface - "What is a file in the system?"
- Phase-interfaces: IScanPhase, ITransferPhase, ISidecarPhase, etc.

**Example:**
`csharp
// This is only the interface, no implementation
public interface IBackupEngine
{
    Task<BackupJobResult> RunAsync(
        BackupPlan plan,
        IProgress<IBackupProgress>? progress,
        CancellationToken ct
    );
}

public record BackupPlan(
    string SourcePath,
    string OutputDirectory,
    bool Recursive,
    bool IncludeHashes,
    bool IncludeMetadata
);

public record BackupJobResult(
    bool Success,
    int TotalItems,
    int SuccessfulItems,
    int FailedItems,
    long TotalBytes,
    TimeSpan Duration,
    List<BackupError> Errors
);
`

**Why separate?** Because if you change the API, all implementations must change. By keeping it together in one file, it's easier to see "what must all components follow?"

---

### 2. **Scanner Layer** (Enumeration)

**Files:** Scanner/ folder with separate implementations

**What do scanners do?**

A scanner's job is to **list all files** from a source. It must:
- Go through the folder recursively
- Return info about each file (path, size, timestamps)
- Handle errors if a folder is inaccessible
- Be cancellable (if user says "stop!")

**What scanners should NOT do:**
- They should NOT copy files
- They should NOT compute hashes
- They should NOT read EXIF

Scanners do **only** enumeration.

#### IItemScanner (Interface)
`
AsyncEnumerable<IBackupItem> ScanAsync(ScanContext, CancellationToken)
`

A scanner returns an "async enumerable" - this means you can get files one-by-one without waiting for the entire list first. Useful for very large sources.

**Implementations:**

**FilesystemItemScanner**: Scan local filesystem
- Used when source is a folder on C:, D:, or network
- Walk directory-structure with Directory.EnumerateFiles
- Return path, size, modified-date
- Handle "Access Denied" errors gracefully

**MTPItemScanner**: Scan MTP-device
- Used when source is iPhone, Android, camera
- Communicates via MediaDevices.dll
- Return device-path, size, device-timestamps
- Handle device-disconnect gracefully

**MemoryItemScanner**: Test double
- Used in unit-tests
- Return hardcoded test-data
- No I/O, super fast

---

### 3. **Transfer Layer** (Copy Files)

**Files:** Transfer/ folder

**What do transfers do?**

A transfer-component copies a file from source to destination. It must:
- Read the file from source
- Write to destination
- Report progress (bytes copied)
- Handle errors (disk full, permission denied, read error)
- Preserve filename and structure

**What transfers should NOT do:**
- Compute hashes
- Extract metadata
- Generate sidecars
- Verify integrity

Transfers do **only** copying.

#### IFileTransfer (Interface)
`
Task<TransferResult> TransferAsync(
    IBackupItem item,
    TransferContext context,
    IProgress<TransferProgress> progress,
    CancellationToken ct)
`

Returns a TransferResult containing:
- Bytes copied
- Destination path
- Status (success/error)
- Error message (if failed)

**Implementations:**

**FilesystemFileTransfer**: Copy file to file
- Open source-file
- Read in chunks (e.g. 1MB at a time)
- Write to destination
- Report progress per chunk
- Handle errors (disk full, permission denied)

**MTPFileTransfer**: Download from MTP
- Open file handle on device
- Download to temp-file
- Move temp to final location
- Handle device-disconnect

**TestFileTransfer**: Test double
- Fake copying without I/O
- Return predictable results

---

### 4. **Sidecar Generator Layer** (Metadata Files)

**Files:** Sidecar/ folder

**What is a sidecar?**

A sidecar is a metadata-file that lies **next to** (hence "side-car") the transferred file. For each backed-up file, there's a .json file with info about the transfer.

**Example:**
`
Output/
  ├─ IMG_1234.JPG          (transferred file)
  ├─ IMG_1234.JPG.sidecar.json  (metadata about transfer)
  └─ Folder/
     ├─ Document.PDF       (transferred file)
     └─ Document.PDF.sidecar.json (metadata)
`

**What does the sidecar contain?**
`json
{
  "source_path": "/source/IMG_1234.JPG",
  "destination_path": "/backup/IMG_1234.JPG",
  "transferred_at": "2026-05-05T12:15:00Z",
  "original_size": 2048576,
  "transferred_size": 2048576,
  "transfer_status": "success",
  "hashes": {
    "source_sha256": "abc123...",
    "dest_sha256": "abc123..."
  },
  "metadata": {
    "exif_date": "2024-06-15T10:30:00Z",
    "created_date": "2024-06-15T10:30:00Z",
    "modified_date": "2024-06-15T10:30:00Z"
  },
  "verification": {
    "verified": true,
    "verified_at": "2026-05-05T12:15:30Z"
  }
}
`

**Why are sidecars important?**

1. **Audit trail**: You can later see "when was this file backed up?"
2. **Integrity-check**: Hashes in sidecar let you check if backup is intact
3. **Restore-information**: If original is deleted, you still have metadata
4. **Forensics**: You can build a timeline based on sidecars
5. **Redundancy**: Self-contained documentation per file

#### ISidecarGenerator (Interface)
`
Task<SidecarContent> GenerateAsync(
    IBackupItem item,
    TransferResult transfer,
    SidecarContext context,
    CancellationToken ct)
`

**Implementations:**

**JsonSidecarGenerator**: Generate .json sidecars
- Create structure as shown above
- Serialize with System.Text.Json
- Simple to read/parse
- Compatible with all systems
- **IMPORTANT:** Must be updatable in-place with metadata/hashes later (can be partially-filled)

**XmlSidecarGenerator**: Generate .xml sidecars
- Alternative format
- More verbose, but some prefer XML
- Same information as JSON

**Core4 Flow for Sidecars (Key Difference from Core3):**

Core3 generates sidecars **last** (after hashing). Core4 should do it differently:

`
Transfer ✅ → GenerateSidecar (MINIMAL, NOW)
               ├─ source_path
               ├─ destination_path
               ├─ transferred_at
               ├─ file_size
               ├─ transfer_status: "success"
               └─ (hashes: null, metadata: null, verification: null)
               
[Later, if enabled]
ExtractMetadata → UpdateSidecar (add metadata fields)
GenerateHashes → UpdateSidecar (add hash fields)
VerifyIntegrity → UpdateSidecar (add verification fields)
CorrectTimestamps → UpdateSidecar (timestamps corrected)

Result: Sidecar exists immediately, gradually enriched with features
`

**Benefits:**
- ✅ Sidecar guaranteed to exist even if later phases fail
- ✅ Auditable immediately after transfer
- ✅ User can see that file was backed up even without hashes
- ✅ Can update sidecar as feature-flag

---

### 5. **Hash Layer** (SHA256 Verification) - PHASE 2

**Files:** Hashing/ folder

**What do hashers do?**

A hasher computes a "fingerprint" of a file - a unique string based on the file's contents. If the file changes even one bit, the hash becomes completely different.

**Why is it important?**

If you back up a 500MB movie, and during transfer one bit flips (due to bad RAM, USB-driver bug, etc.), you'd want to know. A hash-check can detect this.

**How does it work:**
`
Source file: [read bytes] → SHA256() → "a1b2c3d4e5..."
Dest file: [read bytes] → SHA256() → "a1b2c3d4e5..."

If they're the same: ✅ File transferred correctly
If they're different: ❌ File has an error
`

#### IItemHasher (Interface)
`
Task<HashResult> ComputeAsync(string filePath, IProgress<long>, CancellationToken)
`

Returns a HashResult with:
- SHA256 hex-string
- Bytes processed
- Time taken

**Implementations:**

**SHA256Hasher**: Compute SHA256
- Open file
- Read in chunks
- Feed to SHA256
- Return hex-string

**TestHasher**: Test double
- Return predictable hashes
- No I/O

---

### 6. **Metadata Layer** (EXIF, Tags, etc.) - PHASE 2

**Files:** Metadata/ folder

**What do metadata readers do?**

A metadata-reader extracts information **about** a file without changing the file. For images, this can be:
- EXIF date (when was the photo taken?)
- Camera model
- GPS coordinates
- ISO, shutter speed, etc.

For all files:
- Created date (Windows)
- Modified date
- Accessed date
- File attributes (read-only, hidden, etc.)

#### IMetadataReader (Interface)
`
Task<FileMetadata> ExtractAsync(
    string sourcePath,
    string destPath,
    MetadataContext context,
    CancellationToken ct)
`

Returns FileMetadata with:
- EXIF data (if image)
- File attributes
- Timestamps

**Implementations:**

**ExifMetadataReader**: Extract EXIF
- Uses an EXIF-parser (e.g. MetadataExtractor)
- Read image
- Parse EXIF-data
- Return relevant fields

**CommonMetadataReader**: File attributes
- Use FileInfo
- Get created/modified/accessed dates
- Get file attributes
- Return basic metadata

---

### 7. **Verification Layer** (Optional) - PHASE 2

**Files:** Verification/ folder

**What do verifiers do?**

A verifier checks that a transferred file is correct by comparing hashes:

`
Sidecar says: source_sha256 = "abc123..."
You re-hash: dest_sha256 = "abc123..."

Match? → ✅ Verified
Mismatch? → ❌ File corrupted!
`

#### IIntegrityVerifier (Interface)
`
Task<VerificationResult> VerifyAsync(
    string destPath,
    HashResult stored,
    IProgress,
    CancellationToken)
`

---

### 8. **Orchestration Layer** (The Engine)

**Files:** Engine/ folder

**What does the engine do?**

The engine is the **conductor** - it coordinates all components:
- Scanner finds the files
- Transfer copies them
- Sidecar documents it
- (Optional: Hash verifies, Metadata extracts, etc.)

The engine does **NOT**:
- Have copy-logic
- Have hash-logic
- Just orchestration

#### IBackupEngine (Interface)
`
Task<BackupJobResult> RunAsync(BackupPlan, IProgress, CancellationToken)
`

#### Implementations:

**SequentialBackupEngine**: For MTP
- One file at a time
- Scan → Transfer → Sidecar → (Optional: Hash/Metadata/Verify)
- Repeated for each file

**LimitedParallelBackupEngine**: For Filesystem
- Multiple files simultaneously
- Coordinates parallel transfers
- Parallel hashing on transferred files
- Smart queuing

**BackupEngineFactory**: Intelligent selection
- Looks at BackupPlan
- Detects source-type (MTP vs. Filesystem)
- Returns correct engine-type

---

### 9. **DI & Composition**

**Files:** DependencyInjection/ folder

**What is Dependency Injection?**

Instead of each class creating its own dependencies, they're passed in. This makes it easy to test (you can pass in fakes) and easy to swap implementations.

**Example without DI (bad):**
`csharp
public class MyEngine
{
    public MyEngine()
    {
        _scanner = new FilesystemItemScanner(); // Hard-coded
        _transfer = new FilesystemFileTransfer(); // Hard-coded
    }
}

// Problem: You can't test with fake-scanner!
`

**Example with DI (good):**
`csharp
public class MyEngine
{
    private IItemScanner _scanner;
    private IFileTransfer _transfer;
    
    public MyEngine(IItemScanner scanner, IFileTransfer transfer)
    {
        _scanner = scanner;
        _transfer = transfer;
    }
}

// You can test with:
// var engine = new MyEngine(new TestScanner(), new TestTransfer());
`

**What does DI-registration do?**

`csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBMTP3Core4(
        this IServiceCollection services)
    {
        // Register implementations
        services.AddScoped<IItemScanner, FilesystemItemScanner>();
        services.AddScoped<IFileTransfer, FilesystemFileTransfer>();
        services.AddScoped<IBackupEngine, BackupEngine>();
        // ...
        
        return services;
    }
}

// In Program.cs or Startup:
var services = new ServiceCollection();
services.AddBMTP3Core4();
var engine = services.BuildServiceProvider().GetRequiredService<IBackupEngine>();
`

---

## Feature Prioritization

### Why prioritize features?

If we tried to build **everything at the same time**, we would:
- Get overwhelmed
- Make mistakes
- Test poorly
- Never finish

Instead, we build **MVP first** (Minimum Viable Product) - what makes the system work. Then we add features in prioritized order.

**Think of it like building a house:**
- MVP: Walls, roof, door, window - you can live there
- Extended: Electricity, water, heat - it's practical
- Nice-to-have: Alarm system, heat pump, smart home - it's luxury

### 🔴 **Core Features (MUST HAVE)**

These features **require** Core4 to work as a backup-engine. Without them, there's no backup.

#### 1. **Basic File Scanning**

**What does it mean?**
We must be able to list all files in a source-folder. For each file we need to know:
- Where the file is (path)
- How big it is (size)
- When it was modified (timestamp)

**Why is it important?**
Without knowing which files exist, we can't back anything up. This is the **first step** in the backup-process.

**Example:**
`
Source: C:\Pictures\
Files found:
  - vacation.jpg (2.5 MB, modified: 2024-06-15)
  - beach.jpg (3.1 MB, modified: 2024-06-16)
  - Folder\sunset.jpg (1.2 MB, modified: 2024-06-17)
`

**Implementation:**
- Filesystem Scanner can enumerate files with Directory.EnumerateFiles
- MTP Scanner can enumerate via MediaDevices.dll
- Test Scanner returns hardcoded test-data

---

#### 2. **File Transfer (Copy)**

**What does it mean?**
We must copy each file from source to output-directory. The file must be **bitwise identical** after copying.

**Why is it important?**
This is the **core** of the backup. If transfer fails or corrupts data, we have no backup.

**Example:**
`
Source: C:\vacation.jpg (2.5 MB)
↓ [Copy 2.5 MB data]
Output: D:\Backups\vacation.jpg (2.5 MB)

Status: ✅ Success - 2.5 MB copied
`

**Implementation:**
- Open source-file
- Read in chunks (e.g. 1 MB at a time)
- Write to destination
- Report progress
- Handle errors (disk full, permission denied, etc.)

**Error Handling:**
`
Error scenario 1: Disk full
  → Log error
  → Mark file as failed
  → Continue with next file (NOT stop!)

Error scenario 2: File not found
  → Log warning
  → Skip file
  → Continue with next file

Error scenario 3: Permission denied
  → Log warning
  → Skip file
  → Continue with next file

Error scenario 4: Destination directory doesn't exist
  → Create it! Directory.CreateDirectory(dest)
  → Then transfer (NOT throw/fail)
`

**IMPORTANT NOTE from Core3 Review:**
Core3 **throws** on transfer errors, which stops the entire backup. Core4 must be robust:
- Log error
- Mark file as failed
- **Continue with next file**
- Return partial success in result

---

#### 3. **Sidecar Generation (Minimal)**

**What does it mean?**
For each transferred file, we create a .json file (sidecar) that documents what happened.

**Minimal sidecar contains:**
`json
{
  "source_path": "C:\\vacation.jpg",
  "destination_path": "D:\\Backups\\vacation.jpg",
  "transferred_at": "2026-05-05T12:15:00Z",
  "file_size": 2621440,
  "transfer_status": "success"
}
`

**Why is it important?**
- You have documentation of what was backed up
- You can audit later: "When was this file backed up?"
- You can restore without having the source: "I deleted it from iPhone, but have the sidecar"
- **IMPORTANT:** Sidecar generated **immediately after successful transfer** (not last like in Core3)

**Implementation:**
- After successful transfer (IMMEDIATELY!)
- Create file: {originalfilename}.sidecar.json
- Write minimal JSON
- Save in same folder as the transferred file
- If later phases (metadata, hashing) activate, **update** sidecar with new fields

**IMPORTANT NOTE from Core3 Review:**
Core3 generates sidecars **last** (after hashing), so if hashing fails, sidecar is never generated.
Core4 must do it **early** and **update** it later with features.

------

#### 4. **Basic Error Handling**

**What does it mean?**
We must handle errors gracefully - not just crash.

**Examples of errors:**
`
- Transfer fails due to disk full
  → Log error
  → Return detailed error message
  → Backup marked as "Partial Success"

- Scanner finds inaccessible folder
  → Log warning
  → Skip folder
  → Continue scanning

- User pressed "Cancel"
  → Stop gracefully
  → Return partial result
  → No corruption
`

**Implementation:**
- Try-catch around critical operations
- Differentiate between critical errors (stop) and tolerable errors (skip)
- Collect all errors in a list
- Return BackupJobResult with error-list

---

#### 5. **Progress Reporting**

**What does it mean?**
The UI must know what's happening. We report:
- How many files are scanned
- How many bytes are transferred
- Which file is being worked on right now
- Estimated time to completion

**Example:**
`
Backup Progress:
  Scanned: 150 files, 2.4 GB total
  Transferred: 45 files, 800 MB
  Current file: beach.jpg (3.1 MB)
  Speed: 25 MB/s
  ETA: 5 minutes remaining
  Status: In progress
`

**Implementation:**
- Use IProgress<IBackupProgress> interface
- Report after each file-transfer
- Report periodically during transfer (e.g. per MB)
- Calculate ETA based on speed

---

### 🟡 **Extended Features (SHOULD HAVE)**

These features make Core4 **robust and powerful**. The system works without them, but isn't full-featured.

#### 6. **Hash-based Integrity Verification** (OPTIONAL - Feature Flag)

**What does it mean?**
We compute a "fingerprint" (SHA256) for each file before and after transfer. If the fingerprint changes, the file got corrupted during transfer.

**Example:**
`
Source file: vacation.jpg
  → SHA256 hash: "a1b2c3d4e5f6..."

Transfer to: D:\Backups\vacation.jpg
  → SHA256 hash: "a1b2c3d4e5f6..." ✅ Match!

Sidecar updated with hashes:
{
  "source_path": "...",
  "source_hash_sha256": "a1b2c3d4e5f6...",
  "dest_hash_sha256": "a1b2c3d4e5f6...",
  "hash_match": true,
  "verified": true
}
`

**Why is it important?**
- Detects bitflips, USB-driver errors, bad RAM
- Gives you confidence that backup is intact
- Enables "Verify Backup" operation later

**IMPORTANT NOTE from Core3 Review:**
Core3 **forces** 3 hash-types always (SHA2_256, SHA3_256_FIPS202, BLAKE3_256). This is **slow**!
Core4 must make it **optional** via BackupPlan:
`csharp
public List<HashType>? HashTypes { get; init; } = null;  // null = no hashing

// In engine:
if (plan.HashTypes?.Count > 0)
{
    items = await GenerateHashes(...);  // Only if requested
}
`

**Implementation:**
- If HashTypes specified: Compute SHA256 (and others) on source-file before transfer
- Compute SHA256 on dest-file after transfer
- Store both in sidecar
- Compare them

---

#### 7. **Metadata Extraction** (OPTIONAL - Feature Flag)

**What does it mean?**
We extract info **about** files without changing them:
- EXIF-data from images (when was the photo taken?)
- Created/modified/accessed dates
- File attributes
- Camera model, ISO, shutter speed, etc.

**Example:**
`
Image file: beach.jpg
  EXIF date: 2024-06-16 14:30:00
  Camera: Canon EOS 5D Mark IV
  ISO: 400
  Shutter: 1/1000
  
Sidecar updated:
{
  "metadata": {
    "exif_date": "2024-06-16T14:30:00Z",
    "camera_model": "Canon EOS 5D Mark IV",
    "iso": 400,
    "shutter_speed": "1/1000"
  }
}
`

**Why is it important?**
- Builds timeline: "All images from June"
- Forensics: "When was this photo taken?"
- Restore: You know when the original was created

**Implementation:**
- Use EXIF-parser for images
- Use FileInfo for standard Windows-attributes
- Store in sidecar

---

#### 8. **Timestamp Correction**

**What does it mean?**
If EXIF says the image was taken 2024-06-16, but the file was modified 2024-06-20, we reset the timestamp to the EXIF-date.

**Example:**
`
Original file: beach.jpg
  EXIF date: 2024-06-16 14:30:00
  Modified date: 2024-06-20 (because it was edited)

After backup with timestamp correction:
  Backup file: beach.jpg
  Modified date: 2024-06-16 14:30:00 (restored from EXIF)
`

**Why is it important?**
- Timeline is correct: "June images show on June-date"
- Authenticity: Original timestamps are preserved
- Forensics: File times match reality

**Implementation:**
- Find EXIF-date if available
- Set file's modified-date to EXIF-date
- If no EXIF, use source-modified-date

---

#### 9. **Limited Parallelism for Filesystem**

**What does it mean?**
Instead of backing up one file at a time, we can back up 3-4 files **simultaneously**. This makes backup much faster.

**Sequential (slow):**
`
Transfer file 1 (5 sec)
Transfer file 2 (5 sec)
Transfer file 3 (5 sec)
Total: 15 sec
`

**Limited Parallel (fast):**
`
Transfer file 1 (5 sec) ⟶ Hash file 1
Transfer file 2 (5 sec) ⟶ Hash file 2
Transfer file 3 (5 sec) ⟶ Hash file 3
Total: 7 sec (approx)
`

**Why only for Filesystem?**
The MTP protocol (iPhone, Android) becomes unstable under parallelism. I can read one file at a time, but if I try parallel, the device crashes.

Filesystem can handle many simultaneous operations without problems.

**Implementation:**
- Use System.Threading.Channels.Channel<T>
- Multi-threaded producer-consumer
- Thread-pool size = CPU cores (or less)
- Monitor queue depth (don't buffer infinitely)

---

#### 10. **Source-Type Detection (Adaptive Strategy)**

**What does it mean?**
The engine must automatically detect **what** you're backing up:
`
if (source is MTP device)
  → Use SequentialBackupEngine
else if (source is Filesystem)
  → Use LimitedParallelBackupEngine
`

**Example:**
`
User: "Backup from iPhone"
→ Engine detects: MTP-source
→ Uses Sequential strategy: ✅ Stable

User: "Backup from C:\Pictures"
→ Engine detects: Filesystem
→ Uses Parallel strategy: ✅ Fast
`

**Implementation:**
- BackupEngineFactory inspects BackupPlan
- Checks SourceType enum
- Returns correct engine

---

### 🟢 **Nice-to-Have Features (COULD HAVE)**

These features are luxury and can be added later.

#### 11. **Device Management for MTP**

Manage connected devices (iPhone, Android, camera).

#### 12. **Advanced Sidecar Features**

Per-file sidecars with hashes, metadata, verification. Directory-level sidecar (summary for entire backup-run).

#### 13-20. **Other Nice-to-Haves**

Delta/Incremental Backup, Compression, Encryption, Parallel Hashing, Bandwidth Throttling, Backup Scheduling, Database of Backups, Verify Mode.

---

## Implementation Prioritization (Tier-Based)

### What does it mean?

We have **MANY** features and components in Core4. But we can't build everything at the same time. **Prioritization means:**

- **Tier 1 (FUNDAMENTAL):** Without these, backup does NOT work. Blocking for MTP.
- **Tier 2 (IMPORTANT):** Important UX or usable backup. Not blocking.
- **Tier 3 (NICE-TO-HAVE):** Optimization and advanced features.
- **Tier 4 (LEAST IMPORTANT):** Performance-tuning, parallel execution, fancy stuff.

**Important philosophy:** We build from the **foundation up**, not from the **top down**.

---

### TIER 1: FUNDAMENTAL (MUST HAVE)

**Without these: NO BACKUP works. Especially MTP-support falls away.**

#### Engine Infrastructure
- ✅ IBackupEngine interface - Entry point
- ✅ SequentialBackupEngine implementation - Single-threaded, stable
- ✅ BackupEngine abstract base - Orchestration
- ✅ ProgressTracker - Thread-safe progress aggregation

**Why:** Without engine we cannot orchestrate backup.

#### Scanning
- ✅ IBackupScanner interface
- ✅ FilesystemItemScanner - Local files
- ✅ MTPItemScanner - **CRITICAL for MTP support**

**Why:** Without scanner we cannot find files to back up.

#### Transfer
- ✅ IFileTransfer interface  
- ✅ FilesystemFileTransfer - Copy local files
- ✅ MTPFileTransfer - **CRITICAL for MTP support**

**Why:** Without transfer we cannot copy files.

#### Data Models
- ✅ BackupPlan record - Configuration
- ✅ IBackupItem interface - Item flow
- ✅ BackupJobResult record - Final result
- ✅ BackupProgress record - Progress snapshot
- ✅ IBackupProgress interface - Progress reporting
- ✅ Error records: BackupError, TransferResult
- ✅ Enums: BackupPhase, BackupItemStatus, BackupErrorCode

**Why:** Without data structures we have nothing to work with.

#### Sidecar Generation (MINIMAL)
- ✅ ISidecarGenerator interface
- ✅ JsonSidecarGenerator - Minimal sidecar for each file

**Why:** Sidecar is critical for auditing/verifying backup later. Without it we have no documentation.

#### Dependency Injection
- ✅ ServiceCollectionExtensions.AddBMTP3Core4() - DI setup
- ✅ BackupEngineFactory - Engine selection (Sequential vs Parallel)

**Why:** DI enables testability and flexibility.

#### Error Handling
- ✅ Graceful error handling - Log, mark failed, continue
- ✅ Cancellation support - CancellationToken throughout

**Why:** Robust backup that doesn't crash.

#### Testing Infrastructure
- ✅ Test implementations: TestItemScanner, TestFileTransfer

**Why:** Without tests we cannot be sure about stability.

**DELIVERABLE (Tier 1):**
`
✅ Core4 can backup from filesystem to output directory
✅ Core4 can backup from MTP device to output directory
✅ Sidecars generated for each file
✅ Errors logged and handled gracefully
✅ Progress reported correctly
✅ 30+ unit tests passing
✅ 20+ integration tests passing
✅ CLI integration working
`

---

### TIER 2: IMPORTANT (SHOULD HAVE)

**Important for usable backup, but backup works without them. Implement after Tier 1.**

#### Progress & Notifications
- ✅ IProgressNotifier interface - Event-driven feedback
- ✅ SpectreProgressNotifier - Spectre.Console integration
- ✅ Enhanced IBackupProgress - Throughput (MB/s), ETA, ActiveWorkerCount

**Why:** UX matters. Users need feedback about what's happening.

#### Sidecar Enrichment
- ✅ Enhanced sidecar generation - Add hashes, metadata, verification status later

**Why:** Documentation of backup quality is important.

#### Output Structure Options
- ✅ OutputStructure enum - Flat vs Hierarchical
- ✅ Relative path preservation - Keep folder structure

**Why:** Users want to control output layout. Core3 lost directory structure (bug!).

#### Collision Handling
- ✅ CollisionResolution enum - Append/Replace/Skip on name conflicts
- ✅ Name conflict resolution - Handle duplicate filenames

**Why:** Real-world backups have filename collisions.

#### Dry-Run Mode
- ✅ BackupPlan.DryRun flag - Scan without transferring
- ✅ Proper dry-run implementation - Don't actually copy files

**Why:** Users want to preview backup before running it.

#### Exit Strategies
- ✅ BackupPlan.StopOnError flag - Stop on first error or continue?
- ✅ BackupPlan.SkipExisting flag - Skip files that already exist?

**Why:** Users need control over behavior.

**DELIVERABLE (Tier 2):**
`
✅ Rich progress output with Spectre formatting
✅ Sidecars enriched with all available metadata
✅ Output structure control (Flat/Hierarchical)
✅ Collision resolution strategies
✅ Proper dry-run implementation
✅ Configurable error handling strategies
✅ 50+ integration tests passing
✅ Konsoles integration with progress display
`

---

### TIER 3: NICE-TO-HAVE (COULD HAVE)

**Advanced features that improve backup, but prioritized after Tier 1+2.**

#### Hashing (SHA256, BLAKE3, etc.)
- ✅ IItemHasher interface
- ✅ SHA256Hasher - SHA256 hashing
- ✅ Optional hash types - Multiple algorithms
- ✅ Hash control - Enable/disable via BackupPlan.HashTypes

**Why:** Integrity verification. Optional feature.

**Cost:** 30-40% slower backup. Users can disable.

#### Metadata Extraction
- ✅ IMetadataReader interface
- ✅ ExifMetadataReader - Extract EXIF from photos
- ✅ BasicMetadataReader - File attributes only
- ✅ Optional metadata - Enable/disable via BackupPlan.ExtractMetadata

**Why:** Photo backup needs EXIF. Optional feature.

#### Integrity Verification
- ✅ IIntegrityVerifier interface
- ✅ Compare hashes after transfer - Detect corruption

**Why:** Verify backup quality. Optional feature.

**Cost:** 50%+ slower backup. Users can disable.

#### Timestamp Correction
- ✅ ITimestampCorrector interface
- ✅ Restore original timestamps from EXIF/attributes
- ✅ Fix the "all files have today's date" problem

**Why:** Photo/document backups need original dates. Optional feature.

#### Limited Parallelism (Filesystem ONLY)
- ✅ LimitedParallelBackupEngine - Parallel worker pool
- ✅ Worker pool - N concurrent transfers (default: 4 workers)
- ✅ Configurable parallelism - BackupPlan.MaxDegreeOfParallelism
- ✅ Thread-safe progress aggregation

**Why:** Filesystem backups are faster with parallelism. MTP stays sequential.

**Important:** Parallel is NEVER used for MTP. Always sequential for device stability.

#### Sidecar Enrichment Phases
- ✅ Hashing phase - Add hashes to sidecar
- ✅ Metadata extraction phase - Add extracted metadata
- ✅ Verification phase - Add verification results
- ✅ Timestamp correction phase - Record timestamp changes

**Why:** Complete documentation of everything done.

**DELIVERABLE (Tier 3):**
`
✅ Optional hashing (SHA256, BLAKE3, etc.)
✅ Optional metadata extraction (EXIF)
✅ Optional integrity verification
✅ Optional timestamp correction
✅ Sidecar enrichment with all features
✅ Limited parallelism for filesystem (4 workers default)
✅ Sequential always for MTP
✅ 100+ integration tests passing
`

---

### TIER 4: LEAST IMPORTANT (NICE-TO-HAVE+)

**Performance tuning, fancy optimization. Implement LAST (or skip if not time).**

#### Aggressive Parallelism Tuning
- ⚠️ Worker pool sizing - Fine-tune beyond "default 4"
- ⚠️ Queue buffer sizing - Optimize channel sizes
- ⚠️ I/O batching - Combine small writes

**Why:** Micro-optimization. Low ROI.

**Note:** Core2 spent lots of time on this. Core4 philosophy: "Simple first, optimize later."

#### Resume on Crash
- ⚠️ IBackupRepository interface - Persist session state
- ⚠️ Resume support - Continue where backup left off
- ⚠️ Item tracking - Database of processed items

**Why:** Nice-to-have. Most users just re-run backup.

**Cost:** Adds persistence complexity. Core3 doesn't have it. Core2's implementation doesn't work properly.

#### Advanced Features (Phase 2+)
- ⚠️ Backup scheduling - "Backup every day at 22:00"
- ⚠️ SQLite database of backups - Track all backup runs
- ⚠️ Verify mode - Re-hash all files after backup
- ⚠️ Incremental backup - Only copy changed files
- ⚠️ Rate limiting - Limit backup speed to X MB/s

**Why:** These are nice but not core to backup functionality.

#### Full Parallelism Everywhere
- ❌ **NOT recommended for MTP**
- ❌ **NOT a goal for Core4**
- ❌ Only use Sequential for MTP devices

**Why:** Core2's biggest mistake. Parallel access to MTP devices causes random failures.

**Core4 philosophy:** "Sequential for stability, Parallel for speed." Not "Parallel everywhere."

#### GUI Implementation
- ⚠️ WPF/WinForms UI
- ⚠️ Real-time progress visualization

**Why:** CLI-first. GUI comes later.

**DELIVERABLE (Tier 4):**
`
⚠️ Fine-tuned parallel worker pool
⚠️ Optional: Backup repository/resume support
⚠️ Optional: Scheduling + database tracking
⚠️ NOT: Full parallelism for MTP
⚠️ NOT: GUI (Phase 2+)
`

---

## Summary: Implementation Order

**PHASE 1: Build Tier 1 (Core Backup)**
- Goal: Stable, working backup (filesystem + MTP)
- Time: 2-3 sprints
- Deliverable: 50+ tests, CLI integration
- When done: Users can backup, basic features work

**PHASE 2: Build Tier 2 (Important Features)**
- Goal: Rich UX, proper feature control
- Time: 1-2 sprints
- Deliverable: 100+ tests, Spectre integration
- When done: Users love the experience

**PHASE 3: Build Tier 3 (Advanced Features)**
- Goal: Complete feature set (hashing, metadata, verification)
- Time: 2-3 sprints
- Deliverable: 150+ tests, all features optional
- When done: Professional backup tool

**PHASE 4+: Tier 4 & Polish**
- Goal: Performance tuning, nice-to-haves
- Time: Ongoing optimization
- When done: Production-ready

---

## What Blocks What?

**MTP Support Blockers:**
- ❌ Can't backup MTP without: IBackupEngine, SequentialBackupEngine, MTPItemScanner, MTPFileTransfer
- ❌ Parallel execution BREAKS MTP (use Sequential always)

**Filesystem Support Blockers:**
- ❌ Can't backup filesystem without: IBackupEngine, FilesystemItemScanner, FilesystemFileTransfer

**Usable Backup Blockers:**
- ❌ Can't have decent UX without: IProgressNotifier, Spectre integration
- ❌ Can't audit backup without: Sidecars (minimal)

**Optional (Non-Blocking):**
- ✅ Hashing - Works without it (slower verification)
- ✅ Metadata - Works without it (loses EXIF)
- ✅ Verification - Works without it (no integrity check)
- ✅ Timestamps - Works without it (files have today's date)
- ✅ Parallelism - Works without it (single-threaded is slower but stable)
- ✅ Resume - Works without it (manual re-run works)

---

## Decision Point for You

**Two strategies:**

**Strategy A: Build Minimum Viable Core (MVP)**
- Implement Tier 1 + Tier 2 first
- Get to working, usable backup ASAP
- Add Tier 3 features incrementally
- Result: Shippable product quickly

**Strategy B: Build Everything at Once**
- Implement all 4 tiers
- Takes longer to get first version
- But all features available from day 1
- Result: Feature-complete from start

**Recommendation:** Strategy A (MVP first). Build working core, then add features. Core2 failed because it tried everything at once and became too complex.

---

## Implementation Pyramid (Visual)

`
                          ┌─────────────────────┐
                          │   TIER 4 (Least)    │
                          │  Performance, GUI   │
                          │     Resume, etc     │
                          └─────────────────────┘
                        ┌──────────────────────────┐
                        │  TIER 3 (Nice-to-Have)   │
                        │   Hash, Metadata, EXIF   │
                        │  Verification, Parallel  │
                        └──────────────────────────┘
                      ┌────────────────────────────────┐
                      │   TIER 2 (Important)           │
                      │  Progress, Output Control      │
                      │ Dry-Run, Error Strategies      │
                      └────────────────────────────────┘
                    ┌──────────────────────────────────────┐
                    │   TIER 1 (Fundamental)               │
                    │   MTP + FS Scanning & Transfer       │
                    │   Sequential Engine + Sidecars       │
                    │   Core DI + Error Handling + Tests   │
                    └──────────────────────────────────────┘
`

**Build from bottom up, not top down.**

---

## Implementation Pyramid (Visual)

```
                          ┌─────────────────────┐
                          │   TIER 4 (Least)    │
                          │  Performance, GUI   │
                          │     Resume, etc     │
                          └─────────────────────┘
                        ┌──────────────────────────┐
                        │  TIER 3 (Nice-to-Have)   │
                        │   Hash, Metadata, EXIF   │
                        │  Verification, Parallel  │
                        └──────────────────────────┘
                      ┌────────────────────────────────┐
                      │   TIER 2 (Important)           │
                      │  Progress, Output Control      │
                      │ Dry-Run, Error Strategies      │
                      └────────────────────────────────┘
                    ┌──────────────────────────────────────┐
                    │   TIER 1 (Fundamental)               │
                    │   MTP + FS Scanning & Transfer       │
                    │   Sequential Engine + Sidecars       │
                    │   Core DI + Error Handling + Tests   │
                    └──────────────────────────────────────┘
```

**Build from bottom up, not top down.**

---

## Component Checklist by Tier

### TIER 1: Fundamental (35 components)

**Engine (3):**
- [ ] IBackupEngine interface
- [ ] BackupEngine abstract base
- [ ] SequentialBackupEngine

**Progress Tracking (1):**
- [ ] ProgressTracker (thread-safe)

**Scanning (3):**
- [ ] IBackupScanner interface
- [ ] FilesystemItemScanner
- [ ] MTPItemScanner

**Transfer (3):**
- [ ] IFileTransfer interface
- [ ] FilesystemFileTransfer
- [ ] MTPFileTransfer

**Data Models (7):**
- [ ] BackupPlan record
- [ ] IBackupItem interface
- [ ] BackupJobResult record
- [ ] BackupProgress record
- [ ] IBackupProgress interface
- [ ] BackupError record
- [ ] TransferResult record

**Enums (6):**
- [ ] BackupPhase enum
- [ ] BackupItemStatus enum
- [ ] BackupJobStatus enum
- [ ] BackupErrorCode enum
- [ ] BackupOutputStructure enum
- [ ] CollisionResolution enum

**Sidecar (1):**
- [ ] ISidecarGenerator interface
- [ ] JsonSidecarGenerator

**DI + Infrastructure (2):**
- [ ] ServiceCollectionExtensions
- [ ] BackupEngineFactory

**Context Records (3):**
- [ ] ScanContext
- [ ] TransferContext
- [ ] SidecarContext

**Test Infrastructure (2):**
- [ ] TestItemScanner
- [ ] TestFileTransfer

**Total Tier 1 Components:** 35

**Estimated effort:** 10-15 days (experienced dev)

---

### TIER 2: Important (10 components)

**Progress & Notifications (2):**
- [ ] IProgressNotifier interface
- [ ] SpectreProgressNotifier

**Features (2):**
- [ ] DryRun implementation
- [ ] OutputStructure implementation

**Sidecar Enrichment (3):**
- [ ] Hashing phase integration
- [ ] Metadata extraction phase
- [ ] Verification phase integration

**Support (3):**
- [ ] ConsolesPrinter integration
- [ ] Enhanced IBackupProgress (throughput, ETA)
- [ ] Error strategy handling

**Total Tier 2 Components:** 10

**Estimated effort:** 5-8 days

---

### TIER 3: Nice-to-Have (12 components)

**Hashing (2):**
- [ ] IItemHasher interface
- [ ] SHA256Hasher

**Metadata (2):**
- [ ] IMetadataReader interface
- [ ] ExifMetadataReader

**Verification (2):**
- [ ] IIntegrityVerifier interface
- [ ] IntegrityVerifier implementation

**Timestamps (2):**
- [ ] ITimestampCorrector interface
- [ ] TimestampCorrector implementation

**Parallelism (2):**
- [ ] LimitedParallelBackupEngine
- [ ] Worker pool + queue management

**Sidecar (0 - already in Tier 1):**

**Total Tier 3 Components:** 12

**Estimated effort:** 8-12 days

---

### TIER 4: Least Important (5+ components)

**Resume/Persistence (2):**
- [ ] IBackupRepository interface
- [ ] FileSystemRepository / SQLiteRepository

**Logging Infrastructure (1):**
- [ ] Structured logging integration

**Performance Tuning (2+):**
- [ ] Worker pool optimization
- [ ] Buffer sizing tuning
- [ ] I/O batching (optional)

**Total Tier 4 Components:** 5+ (many optional)

**Estimated effort:** 10+ days (ongoing)

---

## Summary Statistics

| Tier | Components | Effort | Status | MTP Blocking |
|------|-----------|--------|--------|-------------|
| 1 | 35 | 10-15 days | Critical | ✅ YES |
| 2 | 10 | 5-8 days | Important | ❌ No |
| 3 | 12 | 8-12 days | Nice | ❌ No |
| 4 | 5+ | 10+ days | Polish | ❌ No |
| **TOTAL** | **62+** | **33-45 days** | | |

---

### What does "Implementation Strategy" mean?

A plan for **how** we build Core4. It's not a to-do list - it's **the process**.

A strategy means we divide the work into **manageable phases** where each phase:
- ✅ Produces **working code**
- ✅ Has **testable components**
- ✅ Can **integrate** with existing systems
- ✅ Can be **shipped** to users (if the first phase)

We do **not** do it by making everything at once and hoping it works.

### Phase 1: Core Engine (Sprint 1-2)

**Goal:** A **working** backup engine. Simple, but works.

**What needs to be done:**

1. **Project Setup**
   - Create `BMTP3.Core4` project in Visual Studio
   - Create folder structure (`Api/`, `Scanner/`, `Transfer/`, `Sidecar/`, `Engine/`, `DependencyInjection/`)
   - Create test project `BMTP3.Core4.Tests`

2. **API Layer** (Contracts)
   - Define `IBackupItem` record
   - Define `IBackupEngine` interface
   - Define `BackupPlan` record
   - Define `BackupJobResult` record
   - Define `IBackupProgress` interface

3. **Scanner Implementation (Filesystem only in Phase 1)**
   - Implement `FilesystemItemScanner`
   - Handle recursive directory enumeration
   - Preserve relative paths
   - Filter system files

4. **Transfer Implementation (Filesystem only)**
   - Implement `FilesystemFileTransfer`
   - Copy files with error handling
   - Create destination directories
   - Handle collisions (rename/skip for now)

5. **Sidecar Generation**
   - Implement `JsonSidecarGenerator`
   - Generate minimal sidecars immediately after transfer
   - Include: filename, source path, size, timestamp, error status

6. **Engine Core**
   - Implement `SequentialBackupEngine` (single-threaded)
   - Implement `ProgressTracker` (thread-safe counters)
   - Wire up pipeline: Scan → Transfer → Sidecar
   - Handle errors gracefully (continue on file error)

7. **Dependency Injection**
   - Create `ServiceCollectionExtensions`
   - Register all implementations
   - Factory pattern for scanner/transfer selection

8. **Error Handling & Testing**
   - Define all error codes
   - Create test infrastructure (TestItemScanner, TestFileTransfer)
   - Implement 50+ integration tests

**DELIVERABLE (Phase 1):**
```
✅ BMTP3.Core4 project with all APIs defined
✅ FilesystemItemScanner + FilesystemFileTransfer working
✅ SequentialBackupEngine functioning
✅ Minimal sidecars generated
✅ Progress tracking functional
✅ Error handling (continue on file error)
✅ 50+ integration tests passing
✅ CLI integration verified (backup command works)
```

**Success:** Basic filesystem backup works from start to finish.

---

### Phase 2: Feature Expansion (Sprint 3-4)

**Goal:** Add UX polish + feature control

**What needs to be done:**

1. **Progress Notifications** (IProgressNotifier)
   - Implement event-driven progress
   - Connect SpectreProgressNotifier for rich console output
   - Add throughput (MB/s) calculation
   - Add ETA calculation

2. **Dry-Run Support**
   - Implement dry-run mode (scan only, no transfer)
   - Create directory structure in dry-run
   - Report what WOULD be backed up

3. **Output Structure Control**
   - Allow users to select output format
   - Flat (all files in one folder)
   - Structured (preserve directory hierarchy)
   - Custom patterns

4. **Error Strategy Configuration**
   - Let users choose: StopOnError, SkipOnError, RetryOnError
   - Per-phase error handling
   - Detailed error reporting

5. **MTP Support (Phase 2b)**
   - Implement `MTPItemScanner`
   - Implement `MTPFileTransfer`
   - Use Sequential engine (always)
   - Register MTP scanner via factory

6. **Sidecar Enrichment Phases**
   - Hashing phase (optional, disabled by default)
   - Metadata extraction phase (optional)
   - Verification phase (optional)

7. **ConsolesPrinter Integration**
   - Update Konsoles BackupConsoleCommand2 to use Core4
   - Display progress with Spectre
   - Show real-time throughput

**DELIVERABLE (Phase 2):**
```
✅ Rich progress reporting (real-time with Spectre)
✅ Dry-run mode working
✅ MTP device scanning + transfer
✅ Output structure control
✅ Error strategy configuration
✅ Feature flags for optional phases
✅ CLI fully integrated with Core4
```

**Success:** Professional-grade UX with stable MTP + Filesystem support.

---

### Phase 3: Advanced Features (Sprint 5-6)

**Goal:** Complete feature set

**What needs to be done:**

1. **Hash Computation** (Optional)
   - Implement `SHA256Hasher`
   - Compute hashes during transfer (or after)
   - Add to sidecar enrichment

2. **Metadata Extraction** (Optional)
   - Implement `ExifMetadataReader`
   - Extract EXIF from images
   - Include metadata in sidecar

3. **Verification** (Optional)
   - Implement `IIntegrityVerifier`
   - Verify file integrity post-transfer
   - Compare hashes if available

4. **Timestamp Correction** (Optional)
   - Implement `ITimestampCorrector`
   - Preserve original file timestamps
   - Apply after transfer

5. **Limited Parallelism** (Optional)
   - Implement `LimitedParallelBackupEngine`
   - 3-4 worker threads for filesystem
   - Keep Sequential for MTP
   - Significant speed boost

6. **Resume Support** (Optional)
   - Implement `IBackupRepository`
   - Persist session state
   - Continue from last position
   - Track processed items

**DELIVERABLE (Phase 3):**
```
✅ SHA256 hashing (optional, feature flag)
✅ EXIF metadata extraction (optional)
✅ File verification (optional)
✅ Timestamp correction (optional)
✅ Limited parallel transfer (optional, filesystem only)
✅ Resume on crash (optional)
✅ All features configurable via BackupPlan
```

**Success:** Professional backup tool with complete feature set.

---

## Thread Safety Guidelines

**When Implementing Phase 1-3, follow these rules:**

```
1. Shared state management
   ├─ ProgressTracker: Use ConcurrentDictionary<K, V> for counters
   ├─ Never use simple int/long without lock
   ├─ OR use lock(syncObject) for small critical sections
   └─ Never use += on shared int without synchronization

2. Queue operations are atomic
   ├─ Queue<T>.Enqueue/Dequeue are thread-safe
   └─ Use Queue, not List, for multi-threaded access

3. Logging is thread-safe
   ├─ ILogger<T> is thread-safe (use it)
   └─ Don't share StreamWriter without locks

4. Resource cleanup
   ├─ Each thread manages its own streams/handles
   ├─ Dispose in try/finally or using statement
   └─ Don't share file handles across threads
```

### Logging Strategy

```
Logging Configuration:

Logger: ILogger<T> (Microsoft.Extensions.Logging)

Log Levels:
├─ DEBUG: Detailed activity per file
│  └─ "Scanning file: C:\Pictures\vacation.jpg (2.5 MB)"
│  └─ "Transferring vacation.jpg to D:\Backup"
│  └─ "Generated sidecar for vacation.jpg"
│
├─ INFO: Phase transitions + milestones
│  └─ "Scan complete: Found 500 files, 2.5 GB total"
│  └─ "Transfer phase started"
│  └─ "Transfer phase complete: 450 succeeded, 50 skipped"
│
├─ WARNING: Recoverable errors
│  └─ "Skipping C:\System Volume Information (Access Denied)"
│  └─ "Hash computation timeout for large_file.bin, skipping"
│
└─ ERROR: Failures
   └─ "Transfer failed for vacation.jpg: Disk full"
   └─ "Device disconnected during transfer"

Structured Properties (every log includes):
├─ ItemId: "img_1234"
├─ Phase: BackupPhase.Transferring
├─ ElapsedMs: 1500
├─ ErrorCode: BackupErrorCode.TransferIOError (if error)
```

---

## Constraints, Assumptions & Limitations

### Environment Constraints

```
Target Framework:
├─ .NET 8.0 minimum (.net8.0-windows)
├─ Windows only (MTP/WPD support)
└─ Visual Studio 2022 or dotnet CLI

External Dependencies:
├─ MediaDevices.dll (for MTP)
├─ System.Text.Json (built-in)
└─ Microsoft.Extensions.* (built-in)
```

### Filesystem Constraints

```
Path Handling:
├─ Max path length: 260 characters (Windows limitation)
└─ Longer paths require Windows API workaround or network paths

File Locking:
├─ During transfer: Source file read-locked
├─ Destination: Write-locked until transfer complete
└─ Cannot transfer file while it's open/modified

Directory Handling:
├─ Recursive enumeration follows all subdirectories
├─ Symbolic links: Ignored (not followed)
└─ Junction points: Ignored (not followed)

Permissions:
├─ Source must be readable
├─ Destination parent must be writable
└─ If access denied → skip with warning
```

### MTP Constraints

```
Device Limitations:
├─ Single device per backup (multi-device in Phase 3+)
├─ Device must stay connected during entire backup
├─ Device must have sufficient free space for temp
└─ Some devices require authentication

Stability:
├─ Parallel access causes device disconnects/corruption
├─ Must use Sequential strategy for MTP
└─ Timeout: Device may go to sleep after 60 sec idle

Content Limitations:
├─ MTP exposes limited hierarchy (varies by device)
├─ Some files may be system-protected
└─ EXIF data only available on image files
```

### Performance Expectations

```
Transfer Speed:
├─ Filesystem → Filesystem: 100+ MB/s (SSD to SSD)
├─ Filesystem → USB external: 30-50 MB/s
├─ MTP (iPhone): 10-20 MB/s
└─ MTP (Android): 5-15 MB/s

Scaling:
├─ Scanning 10,000 files: ~2 seconds
├─ Transferring 10,000 small files: 2-3 minutes
├─ Computing SHA256 for 10,000 files: 1-2 minutes
└─ Large files (1+ GB): Linear time (no optimization)

Memory:
├─ Base engine: ~20-30 MB
├─ Per item in progress: ~1-2 MB
├─ With 50-item queue: ~50-100 MB additional
└─ Total for 10,000 item backup: ~200-300 MB

Disk:
├─ Temp staging (MTP): Requires download to disk first
├─ Sidecars: ~1 KB per file
└─ For 10,000 files: ~10 MB sidecar overhead
```

### Phase 1 Limitations (Intentional)

```
NOT Implemented (saved for Phase 2+):
├─ ❌ Hash computation (optional anyway)
├─ ❌ EXIF metadata extraction
├─ ❌ Timestamp correction
├─ ❌ Parallel transfer (Sequential only)
├─ ❌ MTP device support (Filesystem only)
├─ ❌ Incremental/delta backup
├─ ❌ Compression
├─ ❌ Encryption
└─ ❌ Bandwidth throttling

These are intentionally deferred to keep Phase 1 scope small and testable.
```

### Testing Limitations

```
Unit Testing:
├─ Scanner: Test with in-memory test doubles
├─ Transfer: Use temp directories (never real I/O in unit tests)
├─ Sidecar: Serialize/deserialize, compare JSON
└─ Progress: Mock IProgress<T>

Integration Testing:
├─ Use temp directories for all tests
├─ Create test files dynamically
├─ Clean up after each test
└─ No real device testing in Phase 1 (will add Phase 3)

Performance Testing:
├─ Measure transfer speed with various file sizes
├─ Measure parallelism speedup
└─ Profile memory usage
```

---

## Closing Notes

Core4 is designed to be:
- **Simple enough** to maintain (single-threaded first)
- **Flexible enough** to extend (DI + interfaces everywhere)
- **Robust enough** for production (error handling + testing)
- **Testable enough** for high test-coverage (100+ integration tests)

### TIER 1: Fundamental (35 components)

**Engine (3):**
- [ ] IBackupEngine interface
- [ ] BackupEngine abstract base
- [ ] SequentialBackupEngine

**Progress Tracking (1):**
- [ ] ProgressTracker (thread-safe)

**Scanning (3):**
- [ ] IBackupScanner interface
- [ ] FilesystemItemScanner
- [ ] MTPItemScanner

**Transfer (3):**
- [ ] IFileTransfer interface
- [ ] FilesystemFileTransfer
- [ ] MTPFileTransfer

**Data Models (7):**
- [ ] BackupPlan record
- [ ] IBackupItem interface
- [ ] BackupJobResult record
- [ ] BackupProgress record
- [ ] IBackupProgress interface
- [ ] BackupError record
- [ ] TransferResult record

**Enums (6):**
- [ ] BackupPhase enum
- [ ] BackupItemStatus enum
- [ ] BackupJobStatus enum
- [ ] BackupErrorCode enum
- [ ] BackupOutputStructure enum
- [ ] CollisionResolution enum

**Sidecar (1):**
- [ ] ISidecarGenerator interface
- [ ] JsonSidecarGenerator

**DI + Infrastructure (2):**
- [ ] ServiceCollectionExtensions
- [ ] BackupEngineFactory

**Context Records (3):**
- [ ] ScanContext
- [ ] TransferContext
- [ ] SidecarContext

**Test Infrastructure (2):**
- [ ] TestItemScanner
- [ ] TestFileTransfer

**Total Tier 1 Components:** 35

**Estimated effort:** 10-15 days (experienced dev)

---

### TIER 2: Important (10 components)

**Progress & Notifications (2):**
- [ ] IProgressNotifier interface
- [ ] SpectreProgressNotifier

**Features (2):**
- [ ] DryRun implementation
- [ ] OutputStructure implementation

**Sidecar Enrichment (3):**
- [ ] Hashing phase integration
- [ ] Metadata extraction phase
- [ ] Verification phase integration

**Support (3):**
- [ ] ConsolesPrinter integration
- [ ] Enhanced IBackupProgress (throughput, ETA)
- [ ] Error strategy handling

**Total Tier 2 Components:** 10

**Estimated effort:** 5-8 days

---

### TIER 3: Nice-to-Have (12 components)

**Hashing (2):**
- [ ] IItemHasher interface
- [ ] SHA256Hasher

**Metadata (2):**
- [ ] IMetadataReader interface
- [ ] ExifMetadataReader

**Verification (2):**
- [ ] IIntegrityVerifier interface
- [ ] IntegrityVerifier implementation

**Timestamps (2):**
- [ ] ITimestampCorrector interface
- [ ] TimestampCorrector implementation

**Parallelism (2):**
- [ ] LimitedParallelBackupEngine
- [ ] Worker pool + queue management

**Total Tier 3 Components:** 12

**Estimated effort:** 8-12 days

---

### TIER 4: Least Important (5+ components)

**Resume/Persistence (2):**
- [ ] IBackupRepository interface
- [ ] FileSystemRepository / SQLiteRepository

**Logging Infrastructure (1):**
- [ ] Structured logging integration

**Performance Tuning (2+):**
- [ ] Worker pool optimization
- [ ] Buffer sizing tuning
- [ ] I/O batching (optional)

**Total Tier 4 Components:** 5+ (many optional)

**Estimated effort:** 10+ days (ongoing)

---

## Summary Statistics

| Tier | Components | Effort | Status | MTP Blocking |
|------|-----------|--------|--------|-------------|
| 1 | 35 | 10-15 days | Critical | ✅ YES |
| 2 | 10 | 5-8 days | Important | ❌ No |
| 3 | 12 | 8-12 days | Nice | ❌ No |
| 4 | 5+ | 10+ days | Polish | ❌ No |
| **TOTAL** | **62+** | **33-45 days** | | |

---

## UI/Progress Strategy

### Why is this important?

We reviewed how **Core** and **Core2** handle progress-reporting to user:

- **Core:** Old ConsoleProgressBar class - hardcoded to Console, not testable, basic ASCII-bar
- **Core2:** IBackupProgress interface - simple, but minimalist (only phase + numbers)
- **Core3:** Better IBackupProgress - shows currentFile, but still not enough for modern UI

**Core4 must do it FAR better:**

### Core4 Progress Revolution

**What Core4 adds that Core2/Core3 misses:**

1. **Real-time throughput (MB/s)** - not just "X files done"
2. **ETA (Estimated Time Remaining)** - "5 minutes left"
3. **Active worker count** - shows parallelism
4. **Phase transitions** - "Now in Verification phase"
5. **File-level detail** - "Processing: vacation_photos/sunset.jpg (2.5 MB)"
6. **Error events** - not just final report
7. **Spectre.Console integration** - rich colors, tables, progress bars

### IProgressNotifier: NEW Interface (Core4 Innovation)

Core2/Core3 use **passive observation** via IProgress<T> callback.

Core4 adds **active event notification** via IProgressNotifier:

`csharp
IProgressNotifier events:
  • OnPhaseChanged(oldPhase, newPhase)
    → "Switching from Transfer to Hashing..."
  
  • OnFileStarted(item)
    → "Now processing: sunset.jpg (2.5 MB)"
  
  • OnFileCompleted(item, status)
    → "✅ sunset.jpg copied"
    → "❌ corrupted.jpg - Access Denied"
  
  • OnError(error)
    → "⚠️ Disk Full - Only 50 MB free"
    → Backup continues with remaining files
  
  • OnCompleted(result)
    → Final report with summary
`

### Spectre.Console Integration (Konsoles Best Practice)

Core4 must be **UI-framework agnostic** but with **built-in Spectre support**.

### Implementation Requirements for Phase 1

**Core4 Progress MUST support:**

1. ✅ IBackupProgress with throughput + ETA properties
2. ✅ IProgressNotifier interface for events
3. ✅ ProgressTracker that calculates throughput + ETA
4. ✅ Integration point in BackupEngine.RunAsync()
5. ✅ Spectre-compatible output from ConsolesPrinter

### Why This Matters

1. **User Experience:** Progress gives confidence backup is working
2. **Debugging:** Throughput + ETA help identify bottlenecks
3. **Mobile/GUI:** Both can now consume IBackupProgress + IProgressNotifier events
4. **Testing:** Events are testable (unlike Console output)
5. **Logging:** Event-based reporting enables structured logging

---

## Implementation Phases

### Phase 1: Core Engine (Sprint 1-2)

**Goal:** A **working** backup engine. Simple, but works.

**What should be built:**

1. **Project Setup**
   - Create BMTP3.Core4 project in Visual Studio
   - Create folder-structure (Api/, Scanner/, Transfer/, Sidecar/, Engine/, DependencyInjection/)
   - Create test-project BMTP3.Core4.Tests

2. **API Layer** (Contracts)
   - Define IBackupItem record
   - Define IBackupEngine interface
   - Define BackupPlan record
   - Define BackupJobResult record
   - Define IBackupProgress interface
   - Define ScanContext, TransferContext, SidecarContext

3. **Filesystem Scanner**
   - Implement IItemScanner
   - FilesystemItemScanner - enumerate local files
   - Unit test: Can it find files recursively?

4. **Filesystem Transfer**
   - Implement IFileTransfer
   - FilesystemFileTransfer - copy file to destination
   - Unit test: Can it copy a file?
   - Unit test: Can it handle disk-full errors?

5. **Basic Sidecar**
   - Implement ISidecarGenerator
   - JsonSidecarGenerator - create minimal JSON
   - Unit test: Can it create a sidecar?

6. **Sequential Engine**
   - Implement BackupEngine coordinates: Scan → Transfer → Sidecar
   - Implement basic error handling
   - Implement progress reporting

7. **DI Setup**
   - ServiceCollectionExtensions.AddBMTP3Core4()
   - Register implementations

8. **Integration Tests**
   - Test: End-to-end filesystem backup (scan real folder, transfer to temp, generate sidecars)
   - Test: Error handling (simulate disk full)
   - Test: Cancellation (user says "stop!")

9. **CLI Integration**
   - Hook Core4 engine into BMTP3.Consoles
   - Can user run: dotnet run backup --source C:\test --output C:\output?

**Deliverable:**
`
✅ Core4 can backup from filesystem to output dir
✅ Sidecars generated for each file
✅ Errors handled gracefully
✅ Progress reported
✅ 50+ integration tests passing
✅ CLI integration works
`

---

### Phase 2: Hash & Metadata (Sprint 3-4)

**Deliverable:** Verified backups with metadata.

**Steps:**
1. Implement SHA256 Hasher
2. Implement Basic Metadata Reader (file attributes)
3. Extend Sidecar to include hashes + metadata
4. Implement Verification phase
5. Add tests for each component

**Result:** Core4 can verify file integrity, store metadata.

---

### Phase 3: Advanced Features (Sprint 5+)

**Deliverable:** Production-ready engine.

**Steps:**
1. Implement Limited Parallelism for Filesystem
2. Implement MTP Scanner
3. Implement MTP Transfer
4. Implement Adaptive Strategy (detect + select engine)
5. Implement Device enumeration
6. Add EXIF metadata reader
7. Add timestamp correction
8. Create comprehensive test suite
9. Performance tuning + optimization

**Result:** Core4 can handle both filesystem and MTP, parallel or sequential, with full metadata.

---

## Design Decisions

### 1. **Immutable Data Flow**
- Items flow through pipeline without mutations
- Each phase returns a new item with additional data
- Enables simple reasoning + testing

### 2. **Fail-Fast on Critical, Tolerate on Optional**
- **Critical errors**: Transfer failures → stop backup
- **Optional errors**: Hash failure, metadata failure → log, continue
- Configurable via BackupPlan.ErrorHandlingStrategy

### 3. **Progress Reporting via IProgress<T>**
- UI can subscribe to progress updates
- Percentage, current file, ETA, etc.
- No dependency on UI framework

### 4. **DI for All Major Components**
- Easy to mock for testing
- Easy to swap implementations (Filesystem ↔ MTP)
- Supports future extensibility

### 5. **Cancellation-Aware**
- All async operations accept CancellationToken
- Graceful cancellation at phase boundaries
- Clean resource cleanup

### 6. **Sidecar as Source of Truth**
- Sidecar contains all backup metadata
- Enable restore without re-reading source
- Enable audit trail + forensics

---

## Testing Strategy

### Unit Tests

**Per component:**
- Scanner: Test enumeration logic, directory handling
- Transfer: Test copy logic, error handling
- Hasher: Test hash computation
- Metadata Reader: Test extraction logic
- Sidecar: Test serialization/deserialization

### Integration Tests

**End-to-end scenarios:**
- Filesystem backup complete flow
- MTP backup complete flow
- Error handling (disk full, permissions, etc.)
- Cancellation at various phases
- Large file handling
- Deep directory structures

### Performance Tests

- Baseline transfer speed
- Parallelism efficiency
- Memory usage under load

---

## Known Risks & Mitigations

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| MTP instability under parallelism | High | Backup failure | Use Sequential strategy for MTP |
| Hash computation too slow | Medium | Poor UX | Make optional, parallelize if needed |
| Memory overflow from buffering | Low | Crash | Limit queue sizes, use streaming |
| Complex DI setup | Medium | Maintainability | Start simple, document patterns |
| Performance regression vs Core2 | Medium | User complaint | Profile early, optimize bottlenecks |

---

## Success Criteria

- ✅ Core4 can backup filesystem to output dir with sidecars
- ✅ All Core2 features implemented (hash, metadata, verification, timestamps)
- ✅ Adaptive strategy (Sequential for MTP, Parallel for FS)
- ✅ 100+ integration tests passing
- ✅ CLI integration in Consoles
- ✅ Performance >= Core2 on filesystem
- ✅ MTP stable (no random failures)
- ✅ Clear, maintainable codebase

---

## File Structure (Expected)

`
BMTP3.Core4/
├─ Api/
│  ├─ IBackupEngine.cs
│  ├─ BackupPlan.cs
│  ├─ BackupJobResult.cs
│  ├─ IBackupProgress.cs
│  └─ ... (other DTOs)
├─ Scanner/
│  ├─ IItemScanner.cs
│  ├─ FilesystemItemScanner.cs
│  ├─ MTPItemScanner.cs
│  └─ TestScanner.cs
├─ Transfer/
│  ├─ IFileTransfer.cs
│  ├─ FilesystemFileTransfer.cs
│  ├─ MTPFileTransfer.cs
│  └─ TestTransfer.cs
├─ Sidecar/
│  ├─ ISidecarGenerator.cs
│  ├─ JsonSidecarGenerator.cs
│  └─ TestSidecar.cs
├─ Hashing/ (Phase 2)
│  ├─ IItemHasher.cs
│  └─ SHA256Hasher.cs
├─ Metadata/ (Phase 2)
│  ├─ IMetadataReader.cs
│  ├─ ExifMetadataReader.cs
│  └─ CommonMetadataReader.cs
├─ Verification/ (Phase 2)
│  ├─ IIntegrityVerifier.cs
│  └─ IntegrityVerifier.cs
├─ Engine/
│  ├─ IBackupStrategy.cs
│  ├─ BackupEngineFactory.cs
│  ├─ SequentialBackupEngine.cs
│  └─ LimitedParallelBackupEngine.cs
└─ DependencyInjection/
   └─ ServiceCollectionExtensions.cs
`

---

## Documentation & Communication

- **This plan** is the source of truth for Core4 architecture
- **Code comments** explain "why", not "what"
- **PR descriptions** reference this plan + specific features
- **Tests serve as living documentation** of expected behavior

---

## Constraints, Assumptions & Limitations

### Environment Constraints

`
Target Framework:
├─ .NET 8.0 minimum (.net8.0-windows)
├─ Windows only (MTP/WPD support)
└─ Visual Studio 2022 or dotnet CLI

External Dependencies:
├─ MediaDevices.dll (for MTP)
├─ System.Text.Json (built-in)
└─ Microsoft.Extensions.* (built-in)
`

### Filesystem Constraints

`
Path Handling:
├─ Max path length: 260 characters (Windows limitation)
└─ Longer paths require Windows API workaround or network paths

File Locking:
├─ During transfer: Source file read-locked
├─ Destination: Write-locked until transfer complete
└─ Cannot transfer file while it's open/modified

Directory Handling:
├─ Recursive enumeration follows all subdirectories
├─ Symbolic links: Ignored (not followed)
└─ Junction points: Ignored (not followed)

Permissions:
├─ Source must be readable
├─ Destination parent must be writable
└─ If access denied → skip with warning
`

### MTP Constraints

`
Device Limitations:
├─ Single device per backup (multi-device in Phase 3+)
├─ Device must stay connected during entire backup
├─ Device must have sufficient free space for temp
└─ Some devices require authentication

Stability:
├─ Parallel access causes device disconnects/corruption
├─ Must use Sequential strategy for MTP
└─ Timeout: Device may go to sleep after 60 sec idle

Content Limitations:
├─ MTP exposes limited hierarchy (varies by device)
├─ Some files may be system-protected
└─ EXIF data only available on image files
`

### Performance Expectations

`
Transfer Speed:
├─ Filesystem → Filesystem: 100+ MB/s (SSD to SSD)
├─ Filesystem → USB external: 30-50 MB/s
├─ MTP (iPhone): 10-20 MB/s
└─ MTP (Android): 5-15 MB/s

Scaling:
├─ Scanning 10,000 files: ~2 seconds
├─ Transferring 10,000 small files: 2-3 minutes
├─ Computing SHA256 for 10,000 files: 1-2 minutes
└─ Large files (1+ GB): Linear time (no optimization)

Memory:
├─ Base engine: ~20-30 MB
├─ Per item in progress: ~1-2 MB
├─ With 50-item queue: ~50-100 MB additional
└─ Total for 10,000 item backup: ~200-300 MB

Disk:
├─ Temp staging (MTP): Requires download to disk first
├─ Sidecars: ~1 KB per file
└─ For 10,000 files: ~10 MB sidecar overhead
`

---

## Closing Notes

Core4 is designed to be:
- **Simple enough** to maintain (single-threaded first)
- **Flexible enough** to extend (DI + interfaces everywhere)
- **Robust enough** for production (error handling + testing)
- **Testable enough** for high test-coverage (100+ integration tests)

### Key Principles (Apply Always)

1. **Explicit over implicit** - Every decision is documented, no magic
2. **Fail gracefully** - Log errors, return partial success, don't crash
3. **Cancellable** - Every async operation respects CancellationToken
4. **Testable** - All components mockable, clear responsibilities
5. **Observable** - Logging + progress reporting + detailed results
6. **Immutable data** - Items flow through pipeline unchanged (new objects)

### Starting Out

Start with **Phase 1 (Core Engine)**:
1. Get filesystem scanning working
2. Get file transfer working
3. Generate minimal sidecars
4. Handle errors gracefully
5. Report progress
6. Write 50+ integration tests

Once Phase 1 passes all tests and integrates with CLI, you have a **working backup engine**. Then expand in Phase 2 & 3.

SUCCESS = **Working, maintained, expandable backup engine.** 🎯
