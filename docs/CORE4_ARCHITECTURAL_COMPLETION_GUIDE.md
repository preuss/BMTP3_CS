# Core4 Architectural Completion Guide
## High-Level Overview of Steps to Complete the Hybrid Backup Engine

This guide provides a comprehensive architectural overview of what needs to be implemented to complete Core4, based on the specifications in CORE4_PLAN_en.md and CORE4_ARCHITECTURE_en.md, and lessons learned from Core, Core2, and Core3.

## CORE ARCHITECTURAL PRINCIPLES

Before detailing the implementation steps, it's essential to understand the foundational architectural principles that guide Core4:

### 1. Hybrid Adaptive Strategy
Core4's defining characteristic is its adaptive approach:
- **MTP Sources (iPhone, Android, cameras)**: Use **Sequential Strategy** for stability
- **Filesystem Sources (local/network drives)**: Use **Limited Parallelism Strategy** for performance
- **Decision Logic**: Automatic source-type detection with user override capability

### 2. Separation of Concerns (SoC)
Each architectural layer has a single, well-defined responsibility:
- **API Layer**: Defines contracts (interfaces, DTOs, enums)
- **Implementation Layer**: Contains functional components (scanners, transfers, etc.)
- **Orchestration Layer**: Coordinates the workflow and selects strategy

### 3. Explicit Over Implicit
All behaviors, policies, and data flows are made explicit:
- Error handling strategies are configurable per operation type
- Data flow is visible and traceable
- Configuration options are clearly defined and documented
- No hidden state or implicit assumptions

### 4. Graceful Degradation & Fault Tolerance
The system continues operating despite failures:
- Transfer errors log and continue with next file (don't stop backup)
- Optional feature failures are logged but don't affect core functionality
- Sidecars are generated early to ensure they exist even if later phases fail
- Errors are collected and reported, not thrown as exceptions

### 5. Testability Through Abstraction
Design enables comprehensive testing:
- Interfaces allow mocking/faking dependencies
- Dependency injection enables component substitution
- Clear boundaries between layers facilitate isolation testing
- Stateless components where possible reduce test complexity

## IMPLEMENTATION BLUEPRINT BY LAYER

### LAYER 1: API LAYER (CONTRACTS)
*Location: Api/ folder*

**Purpose**: Define the immutable contracts between all components and external consumers.

**Components to Implement**:
1. **Interfaces**:
   - `IBackupEngine` - Main entry point with `RunAsync(BackupPlan, IProgress<IBackupProgress>, CancellationToken)`
   - `IBackupProgress` - Progress reporting snapshot with comprehensive metrics
   - `IProgressNotifier` - Real-time event reporting for phase transitions/file processing
   - `IBackupItem` - Mutable file entity flowing through pipeline
   - `IBackupScanner` - Source enumeration (returns IAsyncEnumerable<IBackupItem>)
   - `IFileTransfer` - File copying with progress reporting and graceful error handling
   - `ISidecarGenerator` - Sidecar creation (JSON/XML format)
   - `IItemHasher` - Optional hash computation (configurable algorithms)
   - `IMetadataReader` - Optional metadata extraction (EXIF + file attributes)
   - `IIntegrityVerifier` - Optional integrity verification (hash comparison)
   - `ITimestampCorrector` - Optional timestamp restoration (from EXIF/attributes)
   - `IBackupRepository` - Optional persistence for resume support

2. **Data Transfer Objects (Immutable Records)**:
   - `BackupPlan` - Complete configuration (source, destination, features, options)
   - `BackupJobResult` - Final execution result with statistics, errors, performance
   - `BackupProgress` - Real-time progress snapshot (calculated metrics included)
   - `BackupError` - Structured error information with codes, messages, context
   - `TransferResult` - Single file transfer outcome (success/failure, bytes, path)
   - `ExtractedMetadata` - Result of metadata extraction (file attributes + EXIF)
   - `VerificationResult` - Result of integrity verification (success/failure with reason)
   - `TimestampCorrectionResult` - Result of timestamp correction (before/after timestamps)

3. **Enumerations**:
   - `SourceType` (Filesystem, MediaDevice)
   - `BackupPhase` (NotStarted, Scanning, Transferring, Hashing, MetadataExtraction, Verification, TimestampCorrection, Completed, Failed, Cancelled)
   - `BackupJobStatus` (NotStarted, Running, Completed, PartialSuccess, Failed, Cancelled)
   - `BackupItemStatus` (Pending, Transferred, Failed, Skipped, Verified)
   - `ErrorHandlingStrategy` (StopOnError, SkipOnError, RetryOnError)
   - `BackupErrorCode` - Categorized errors (scan, transfer, MTP-specific, feature, device, configuration, unknown)
   - `SidecarFormat` (Json, Xml)
   - `CollisionResolutionType` (Skip, Rename, Overwrite, Error)
   - `RenameStrategy` (Increment, Timestamp, Guid)
   - `HashType` (SHA2_256, SHA2_512, SHA3_*, BLAKE3_*, MD5_128)
   - `BackupOutputStructure` (Flat, Hierarchical)
   - `CollisionResolution` (Append, Replace, Skip)

### LAYER 2: IMPLEMENTATION LAYER (FUNCTIONAL COMPONENTS)
*Location: Feature-specific folders (Scanner/, Transfer/, etc.)*

**Purpose**: Provide concrete implementations of API layer interfaces, each with a single responsibility.

#### 2.1 Scanner Layer
**Location**: Scanner/ folder with Filesystem/ and MTP/ subfolders

**Responsibility**: Enumerate files from source and create IBackupItem instances.

**Implementations**:
- `FilesystemItemScanner`:
  - Uses `Directory.EnumerateFiles` for recursive traversal
  - Preserves relative paths using `GetRelativePath`
  - Handles `UnauthorizedAccessException` gracefully (logs and continues)
  - Supports cancellation via `CancellationToken`
  - Returns items via `IAsyncEnumerable<IBackupItem>` (streaming)
  - Respects `Recursive`, `IncludePatterns`, `ExcludePatterns`
  
- `MTPItemScanner`:
  - Uses `MediaDevices.dll` to access device file system
  - Preserves device paths and relative structure
  - Handles device disconnection gracefully (throws meaningful exceptions)
  - Supports cancellation and recursive traversal
  - Returns items via `IAsyncEnumerable<IBackupItem>`
  
- `TestItemScanner`:
  - Returns predefined test items for unit testing
  - No actual I/O operations

**Key Requirements**:
- Must preserve directory structure via relative paths (critical fix from Core3 Bug #1)
- Must handle access errors gracefully without stopping enumeration
- Must support cancellation at any point
- Must return items one-by-one (not buffer entire list)

#### 2.2 Transfer Layer
**Location**: Transfer/ folder with Filesystem/ and MTP/ subfolders

**Responsibility**: Copy individual files from source to destination with progress reporting.

**Implementations**:
- `FilesystemFileTransfer`:
  - Opens source file with `FileStream`
  - Creates destination directory with `Directory.CreateDirectory` if needed
  - Copies in configurable chunks (e.g., 1MB) using buffered streams
  - Reports progress via `IProgress<long>` (bytes transferred)
  - Handles errors gracefully (returns `TransferResult` with `Success=false`, does NOT throw)
  - Cleans up partial files on error
  - Supports cancellation
  - Preserves filename and directory structure
  
- `MTPFileTransfer`:
  - Opens file handle on MTP device using MediaDevices API
  - Downloads to temporary file
  - Moves temp file to final destination
  - Handles device timeouts with exponential backoff retry (3 attempts: 1s, 2s, 4s)
  - Detects and handles device disconnection during transfer
  - Reports progress during download
  - Cleans up temporary files on error/cancellation
  - Returns `TransferResult` with status and details

**Key Requirements** (Critical fixes from Core3):
- **Auto-create destination directories** before transfer (fixes Bug #3)
- **Return failure status instead of throwing** on errors (fixes Bug #2)
- **Report actual bytes transferred** via progress mechanism (fixes Bug #4)
- **Handle disk full, permission denied, file not found** gracefully
- **Preserve directory structure** using relative paths from scanner
- **Support cancellation** with proper cleanup

#### 2.3 Sidecar Generator Layer
**Location**: Sidecar/ folder

**Responsibility**: Create metadata files (sidecars) that document the transfer process.

**Implementations**:
- `JsonSidecarGenerator`:
  - Creates/updates `.json` sidecar files
  - Generates **minimal sidecar immediately after successful transfer** (critical fix from Core3 Design Flaw #3)
  - Structure: `{ source_path, destination_path, transferred_at, file_size, transfer_status }`
  - Designed to be **updatable in-place** with metadata/hashes/verification/timestamps later
  - Serializes with `System.Text.Json`
  - Handles write errors gracefully (logs and continues)
  - Supports cancellation
  
- `XmlSidecarGenerator`:
  - Alternative XML format implementation
  - Same responsibilities as JsonSidecarGenerator
  
- `TestSidecarGenerator`:
  - Test double that records calls without actual file I/O

**Key Requirements** (Critical fixes from Core3):
- **Generate sidecar IMMEDIATELY after transfer** (not last) - ensures existence even if later phases fail
- **Make sidecar updatable in-place** - allows gradual enrichment with optional features
- **Handle errors gracefully** - log and continue rather than stopping backup
- **Support cancellation** during generation
- **Create directory if needed** before writing sidecar file

#### 2.4 Optional Feature Layers
*Location: Hashing/, Metadata/, Verification/, Timestamps/ folders*

**Responsibility**: Provide advanced features that are OFF by default and can be enabled via BackupPlan.

**Implementations**:
- **Hashing Layer** (`IItemHasher`):
  - `SHA256Hasher`: Computes SHA256 hash of files in chunks
  - `MultiHasher`: Computes multiple hash algorithms
  - `TestHasher`: Returns predictable test hashes
  - **Critical requirement**: Make hashing TRULY OPTIONAL (null `BackupPlan.HashTypes` = no hashing) - fixes Core3 Design Flaw #2
  - Compute hash on source file (before transfer) AND destination file (after transfer)
  - Store both hashes in sidecar for verification
  - Report progress via `IProgress<long>`
  - Handle file errors gracefully (return partial results or empty dictionary)
  - Support cancellation

- **Metadata Layer** (`IMetadataReader`):
  - `ExifMetadataReader`: Extracts EXIF data from images using library like MetadataExtractor
  - `BasicMetadataReader`: Extracts standard file attributes (created/modified/accessed dates, attributes)
  - `TestMetadataReader`: Returns hardcoded test data
  - Extract information WITHOUT modifying files
  - Handle non-image files gracefully (return file attributes only)
  - Handle permission denied/corrupt files gracefully (return partial data)
  - Support cancellation
  - Store results in `ExtractedMetadata` record for use by other components

- **Verification Layer** (`IIntegrityVerifier`):
  - `IntegrityVerifier`: Compares source hash (from item) with destination hash (computed on demand)
  - `TestVerifier`: Returns predictable test results
  - Compute destination file hash and compare with stored source hash
  - Return detailed result indicating success or failure reason (file deleted, hash mismatch, etc.)
  - Report progress during hash computation
  - Handle file deleted during verification
  - Support cancellation
  - Store verification results in sidecar

- **Timestamp Correction Layer** (`ITimestampCorrector`):
  - `TimestampCorrector`: Sets destination file's modified date to original from EXIF or file attributes
  - `TestTimestampCorrector`: Returns test results
  - **Critical requirement**: Require SUCCESSFUL metadata extraction first - fixes Core3 Key Difference
  - Extract original date from EXIF (if image) or file attributes
  - Set destination file's modified time to match original
  - Return before/after timestamps and success status
  - Handle file not found, permission denied, no metadata available gracefully
  - Support cancellation

### LAYER 3: ORCHESTRATION LAYER (THE ENGINE)
*Location: Engine/ folder*

**Purpose**: Coordinate all components, manage workflow, and select appropriate strategy based on source type.

**Components to Implement**:
1. **BackupEngine** (Abstract Base Class):
   - Implements the core orchestration template method
   - Manages complete backup lifecycle: start → scan → transfer → sidecar → [optional features] → cleanup
   - Coordinates progress tracking and reporting
   - Handles cancellation consistently throughout
   - Aggregates results and errors from all stages
   - **Abstract methods** that subclasses must implement:
     - `CreateScanner(ScanContext)` → `IBackupScanner`
     - `CreateTransfer(TransferContext)` → `IFileTransfer`
   - Contains shared logic for validation, context creation, result building

2. **SequentialBackupEngine** (For MTP Devices):
   - Inherits from `BackupEngine`
   - Processes items **one at a time** in strict sequence
   - No parallelism whatsoever
   - **Key behavior**:
     - Scanner runs sequentially
     - Each item: Transfer → Sidecar → [Hash → Metadata → Verify → Timestamp] → Next item
     - All features run sequentially for each item
   - **When to use**: MTP devices (all sources), testing, when parallelism causes device errors
   - **Advantage**: Maximum stability and predictability for finicky MTP protocol

3. **LimitedParallelBackupEngine** (For Filesystem Sources ONLY):
   - Inherits from `BackupEngine`
   - Implements **controlled parallelism** with worker pools
   - **Key behavior**:
     - Scanner runs on **single thread** (producer) - feeds items to Transfer workers
     - Transfer pool: N threads consuming from transfer queue
     - Sidecar pool: N threads consuming from sidecar queue
     - Optional feature pools: N threads each for hashing, metadata, verification, timestamp correction
     - ProgressTracker collects metrics from all worker threads
     - Thread-safe collections prevent race conditions
   - **Configuration**:
     - Default parallelism: `Math.Min(4, Environment.ProcessorCount / 2)`
     - Can be overridden via `BackupPlan.MaxDegreeOfParallelism`
     - Hard cap at 8 threads to prevent resource exhaustion
     - Queue depth limit = 50 items (backpressure control)
   - **Critical restriction**: **NEVER used for MTP sources** - fixes Core2 mistake
   - **Advantage**: Significant performance improvement for stable filesystem sources

4. **BackupEngineFactory**:
   - **Static class** with `Create(BackupPlan plan, IServiceProvider serviceProvider)` method
   - **Decision logic**:
     - IF `plan.DeviceId != null` → RETURN `SequentialBackupEngine` (ALWAYS sequential for MTP)
     - ELSE IF `plan.MaxDegreeOfParallelism == -1` → RETURN `SequentialBackupEngine` (user-requested sequential)
     - ELSE → RETURN `LimitedParallelBackupEngine` (default parallel for filesystem)
   - Ensures correct engine selection based on source type and user preferences

5. **ProgressTracker**:
   - Thread-safe progress aggregation component
   - **Responsibilities**:
     - Update counters from multiple worker threads using `Interlocked` operations
     - Track current file being transferred with thread-safe locking
     - Calculate rates (bytes/second) and time estimates (ETA)
     - Provide thread-safe snapshots via `GetSnapshot()` method
   - **Key methods**:
     - `IncrementItemsFound(count)` - called by scanner for each discovered file
     - `IncrementItemsTransferred(bytes)` - called by transfer workers on success
     - `IncrementItemsFailed(bytes)` - called by transfer workers on failure
     - `SetCurrentFile(name, size)` - called by transfer worker before processing file
     - `UpdateBytesTransferred(bytes)` - called periodically during transfer
     - `GetSnapshot()` - returns immutable `BackupProgress` record for reporting
   - **Thread safety**: All operations use atomic operations or locks; snapshots are lock-free reads

## CRITICAL ARCHITECTURAL DECISIONS & FIXES

Based on the lessons learned documents, Core4 must implement these specific fixes:

### Fixes from Core3 Critical Bugs:
1. **Bug #1: Directory Structure Goes Lost**
   - **Fix**: Preserve relative paths. `DestinationPath = Path.Combine(destination, relativePath)`
   - **Implementation**: Both scanners must set `RelativePath` correctly; transfer layer uses it to build destination path

2. **Bug #2: Transfer Errors Kill Entire Backup**
   - **Fix**: Log error, mark file as failed, **continue with next file**
   - **Implementation**: Transfer layer returns `TransferResult` with `Success=false` instead of throwing; engine continues processing

3. **Bug #3: Destination Directory Must Pre-Exist**
   - **Fix**: `Directory.CreateDirectory(destinationPath)` before transfer
   - **Implementation**: Transfer layer creates destination directory if it doesn't exist

4. **Bug #4: Progress Reporting Incomplete**
   - **Fix**: Complete implementation with actual bytes transferred
   - **Implementation**: Transfer layer reports actual bytes via `IProgress<long>`; ProgressTracker aggregates; BackupProgress includes `BytesTransferred` and calculates `BytesPerSecond`, `ETA`

5. **Bug #5: Dry-Run Mode Incomplete**
   - **Fix**: Full simulation - create directory-structure even in dry-run
   - **Implementation**: All layers simulate their operations in dry-run mode (no actual I/O but accurate progress reporting and state tracking)

### Fixes from Core3 Design Flaws:
1. **Design Flaw #1: Error Handling Strategy is Implicit**
   - **Fix**: Explicit `ErrorStrategy` enum per operation (`StopOnError`, `SkipOnError`, `RetryOnError`)
   - **Implementation**: `BackupPlan` includes `ErrorHandlingStrategy`; engine respects it per operation type

2. **Design Flaw #2: Hashing Always Forced (Slow)**
   - **Fix**: Hashing should be **optional** (null means no hashing)
   - **Implementation**: `BackupPlan.HashTypes` is nullable; engine only calls hasher if `HashTypes?.Count > 0`

3. **Design Flaw #3: Sidecars Generated Last**
   - **Fix**: Generate **minimal sidecar after transfer**, update it later with metadata/hashes
   - **Implementation**: Sidecar layer called immediately after transfer; optional feature layers update existing sidecar

4. **Design Flaw #4: Collision Resolution Not Testable**
   - **Fix**: Separate, testable `CollisionResolution` step
   - **Implementation**: Distinct collision resolution logic that can be unit tested independently

5. **Design Flaw #5: Immutable Flow is Actually Mutable**
   - **Fix**: Document clearly: items are mutable list, not immutable stream
   - **Implementation**: `IBackupItem` has mutable setters for status, errors, metadata, etc.; documented as mutable enrichment

6. **Design Flaw #6: Cancellation Handling Inconsistent**
   - **Fix**: Consistent pattern throughout
   - **Implementation**: Standardized use of `ct.ThrowIfCancellationRequested()` at logical checkpoints OR `if (ct.IsCancellationRequested)` pattern consistently applied

### Fixes from Core2 Critical Threading-Issues:
1. **Issue #1: Deadlock Risk - Multiple Concurrent Writers to Bounded Channels**
   - **Fix**: Avoid complex channel pipelines. Use simple sequential or carefully-designed parallel.
   - **Implementation**: LimitedParallelBackupEngine uses unbounded queues with semaphores, not bounded channels with multiple writers

2. **Issue #2: Race Conditions in Shared ProgressTracker**
   - **Fix**: Use thread-safe collections OR avoid shared mutable state
   - **Implementation**: ProgressTracker uses `Interlocked` operations for counters and locks for current file state

3. **Issue #3: Implicit Error Handling Across Stages**
   - **Fix**: Explicit error policies per operation
   - **Implementation**: Error handling strategy is explicit and configurable per operation type via BackupPlan

4. **Issue #4: Task Orchestration is Manual and Error-Prone**
   - **Fix**: Simple orchestration - direct method calls, not magic task chaining
   - **Implementation**: BackupEngine uses clear sequential steps; LimitedParallelBackupEngine uses well-defined worker pools

5. **Issue #5: Debugging is Nearly Impossible**
   - **Fix**: Prioritize simplicity over theoretical efficiency
   - **Implementation**: Clear, linear data flow; each component has single responsibility; minimal abstraction layers

6. **Design Flaw #1: Coupling Between Stages is Implicit**
   - **Fix**: Each stage knows what comes next; no need to forward all items blindly
   - **Implementation**: Orchestration layer explicitly calls each stage in sequence; stages don't need to forward items

7. **Design Flaw #2: Bounded Channels Make Pipeline Brittle**
   - **Fix**: Avoid configuration-dependent deadlock/memory explosion risks
   - **Implementation**: Unbounded queues with backpressure controls (queue depth limits, semaphores) instead of bounded channels

8. **Design Flaw #3: MTP Session Management is Hacky**
   - **Fix**: Managing MTP session lifetime needs explicit lifecycle management
   - **Implementation**: MTPFileTransfer opens/closes sessions explicitly; engine manages session lifetime for scanning/transfer phases; keep-alive pings and timeouts

## ARCHITECTURAL VALIDATION CHECKPOINTS

Before considering Core4 complete, validate these architectural aspects:

### 1. Hybrid Strategy Validation
- [ ] MTP sources use SequentialBackupEngine (no parallelism)
- [ ] Filesystem sources use LimitedParallelBackupEngine (controlled parallelism)
- [ ] User can override with MaxDegreeOfParallelism = -1 to force sequential
- [ ] Engine selection logic in BackupEngineFactory works correctly

### 2. Separation of Concerns Validation
- [ ] Each component has exactly one responsibility
- [ ] No component knows internal workings of others
- [ ] Dependencies flow inward (outer layers depend on inner layers, not vice versa)
- [ ] Interfaces are stable and well-defined

### 3. Error Handling Validation
- [ ] Transfer errors don't stop backup (continue with next file)
- [ ] Optional feature failures don't stop backup
- [ ] Errors are logged and collected in results
- [ ] Error strategies are configurable per operation type
- [ ] Critical errors (e.g., engine failure) stop backup appropriately

### 4. Sidecar Flow Validation
- [ ] Sidecar created IMMEDIATELY after successful transfer
- [ ] Sidecar contains minimal data initially (paths, timestamps, size, status)
- [ ] Sidecar updated later by optional features (hashes, metadata, verification, timestamps)
- [ ] Sidecar exists even if all optional features fail
- [ ] Sidecar format is updatable in-place

### 5. Progress Reporting Validation
- [ ] Includes all required metrics (files discovered/processed/succeeded/failed/skipped, bytes total/processed/rate)
- [ ] Tracks current file being processed
- [ ] Calculates percentage complete, transfer speed, ETA accurately
- [ ] Thread-safe and suitable for both sequential and parallel execution
- [ ] Reported periodically via IProgress<IBackupProgress>

### 6. Cancellation Handling Validation
- [ ] Consistent pattern used throughout codebase
- [ ] Responds promptly to cancellation requests
- [ ] Cleans up resources properly (files, connections, temp files)
- [ ] Returns partial results when cancelled rather than failing completely
- [ ] Doesn't leave system in inconsistent state

### 7. Testability Validation
- [ ] All interfaces can be mocked/faked for unit testing
- [ ] Dependency injection allows component substitution
- [ ] Components can be tested in isolation
- [ ] No tight coupling to specific implementations
- [ ] Clear boundaries enable integration testing of layers

### 8. MTP-Specific Validation
- [ ] Session lifecycle managed explicitly (open before scanning, keep alive, close after)
- [ ] Keep-alive pings sent periodically (every 30 seconds)
- [ ] Per-operation timeouts implemented (60 seconds, configurable)
- [ ] Retry with exponential backoff for transient errors (1s, 2s, 4s)
- [ ] Guards against using device after disconnect
- [ ] Proper cleanup in finally blocks
- [ ] Handles device disconnection gracefully during all phases

### 9. Filesystem-Specific Validation
- [ ] Limited parallelism respects MaxDegreeOfParallelism setting
- [ ] Default parallelism = Min(4, ProcessorCount/2)
- [ ] Hard cap at 8 threads to prevent resource exhaustion
- [ ] Queue depth limit = 50 items provides backpressure control
- [ ] Scanner remains single-threaded (producer-consumer pattern)
- [ ] Worker pools process items concurrently with thread-safe coordination

### 10. Resource Management Validation
- [ ] All streams, files, and connections properly disposed
- [ ] Temporary files cleaned up on error/cancellation
- [ ] Media device connections properly closed
- [ ] No memory leaks or resource exhaustion under load
- [ ] Efficient chunk-based processing for large files

## COMPLETION ROADMAP SUMMARY

To complete Core4, implement in this order:

### PHASE 1: API LAYER & TIER 1 FUNDAMENTALS
1. Define all interfaces (IBackupEngine, IBackupProgress, IBackupItem, etc.)
2. Define all DTOs (BackupPlan, BackupJobResult, BackupProgress, etc.)
3. Define all enumerations (SourceType, BackupPhase, ErrorHandlingStrategy, etc.)
4. Implement core scanners (FilesystemItemScanner, MTPItemScanner)
5. Implement core transfers (FilesystemFileTransfer, MTPFileTransfer)
6. Implement sidecar generator (JsonSidecarGenerator - immediate creation)
7. Implement sequential engine (BackupEngine abstract + SequentialBackupEngine)
8. Implement progress tracking (ProgressTracker)
9. Implement engine factory (BackupEngineFactory)
10. Set up basic dependency injection
11. Validate: basic scan→transfer→sidecar workflow works for both source types

### PHASE 2: TIER 2 UX ENHANCEMENTS
1. Implement progress notifier (IProgressNotifier + SpectreProgressNotifier)
2. Implement output structure handling (Flat vs Hierarchical)
3. Implement collision resolution (separate, testable step)
4. Implement enhanced dry-run (full simulation)
5. Add custom path/pattern support
6. Validate: user experience features work correctly

### PHASE 3: TIER 3 OPTIONAL FEATURES
1. Implement hashing layer (IItemHasher + SHA256Hasher - truly optional)
2. Implement metadata layer (IMetadataReader + ExifMetadataReader)
3. Implement verification layer (IIntegrityVerifier)
4. Implement timestamp layer (ITimestampCorrector - requires metadata success)
5. Validate: optional features work correctly and can be toggled via BackupPlan

### PHASE 4: TIER 3 PARALLELISM (FILESYSTEM ONLY)
1. Implement LimitedParallelBackupEngine (producer-consumer with worker pools)
2. Implement proper queue depth limits and backpressure control
3. Implement thread-safe progress tracking
4. Ensure MTP sources NEVER use this engine (factory logic)
5. Validate: filesystem performance improvement without instability

### PHASE 5: MTP SESSION MANAGEMENT (CRITICAL)
1. Implement explicit session lifecycle management
2. Add keep-alive pings (every 30 seconds)
3. Add per-operation timeouts (60 seconds, configurable)
4. Add retry with exponential backoff (1s, 2s, 4s)
5. Implement guards against using device after disconnect
6. Ensure proper cleanup in finally blocks
7. Validate: rock-solid MTP stability without device disconnects

### PHASE 6: INFRASTRUCTURE & PERSISTENCE
1. Implement IBackupRepository interface
2. Implement FileSystemRepository (output directory storage)
3. Implement SQLiteRepository (SQLite DB storage)
4. Keep NoOpRepository as default (no persistence)
5. Implement session save/load for resume capability
6. Implement processed items tracking
7. Validate: persistence works correctly and handles errors gracefully

### PHASE 7: COMPREHENSIVE TESTING & VALIDATION
1. Unit test each component in isolation
2. Integration test complete workflows
3. Test all feature combinations and error scenarios
4. Validate edge cases (empty dirs, permissions, disk full, cancellation, etc.)
5. Test MTP stability under various conditions
6. Test filesystem performance and resource usage
7. Verify all fixes from Core/Core2/Core3 are working
8. Confirm no regressions in core functionality

## KEY TAKEAWAYS

Core4's architectural success depends on:

1. **Stability First**: Ensure MTP backups are completely reliable before optimizing filesystem performance
2. **Adaptive Approach**: Match implementation strategy to source type rather than one-size-fits-all
3. **Explicit Contracts**: Clear interfaces, data flows, and configuration options
4. **Graceful Error Handling**: Continue operating despite failures where possible
5. **Early Validation**: Check preconditions, create directories, validate access before operations
6. **Progressive Enhancement**: Build core functionality first, then add features
7. **Test-Driven Design**: Interfaces and DI enable comprehensive testing
8. **Resource Awareness**: Manage memory, threads, and device connections carefully
9. **User Experience**: Provide meaningful feedback, clear errors, and intuitive configuration
10. **Maintainability**: Simple, well-documented code that avoids unnecessary complexity

By following this architectural guide and implementing the layers in the specified order, Core4 will achieve its goal of being a stable, feature-rich, performant, and maintainable backup engine that combines the best lessons from all previous versions while avoiding their pitfalls.

The result will be a hybrid backup engine that:
- Is **rock-solid** for MTP devices (sequential strategy)
- Is **performant** for filesystem sources (limited parallelism)
- Is **simple to understand and debug** (clear separation of concerns)
- Includes **all features** from previous versions but implemented maintainably
- Is **easy to test and extend** (dependency injection and interfaces)
- Provides **excellent user experience** (progress reporting, error handling, configuration)