# Core4 Completion Roadmap
## High-Level Implementation Plan Based on Architecture Documents

This document outlines the systematic steps needed to complete the Core4 backup engine implementation, based on the architectural specifications and lessons learned from previous versions.

## IMPLEMENTATION PHASES

### PHASE 0: FOUNDATION & PROJECT SETUP
**Goal:** Establish the basic project structure and dependencies

1. **Project Structure Creation**
   - Create the layered folder structure as defined in CORE4_ARCHITECTURE_en.md:
     - Api/ (interfaces and contracts)
     - Scanner/ (IBackupScanner implementations)
     - Transfer/ (IFileTransfer implementations)
     - Sidecar/ (ISidecarGenerator implementations)
     - Hashing/ (IItemHasher implementations)
     - Metadata/ (IMetadataReader implementations)
     - Verification/ (IIntegrityVerifier implementations)
     - Timestamps/ (ITimestampCorrector implementations)
     - Engine/ (orchestration classes)
     - Progress/ (progress tracking)
     - Results/ (result DTOs)
     - Models/ (data objects)
     - Repository/ (persistence interfaces)
     - DependencyInjection/ (service registration)

2. **Dependency Setup**
   - Configure .NET project with required packages:
     - System.Text.Json (for sidecar serialization)
     - Microsoft.Extensions.DependencyInjection
     - Microsoft.Extensions.Logging
     - MediaDevices (for MTP support)
     - Spectre.Console (for progress reporting)
     - ZLogger (for logging)
     - Any EXIF parsing library (e.g., MetadataExtractor)

3. **Basic Configuration**
   - Set up appsettings.json for configuration
   - Create basic logging configuration
   - Establish cancellation token source pattern

### PHASE 1: API LAYER (CONTRACTS & DTOs)
**Goal:** Define all interfaces and data transfer objects that form the contract between components

1. **Core Interfaces** (from CORE4_ARCHITECTURE_en.md):
   - IBackupEngine (main entry point)
   - IBackupProgress (progress reporting)
   - IProgressNotifier (real-time event reporting)
   - IBackupItem (file flowing through pipeline)
   - IBackupScanner (source enumeration)
   - IFileTransfer (file copying)
   - ISidecarGenerator (sidecar creation)
   - IItemHasher (hash computation)
   - IMetadataReader (metadata extraction)
   - IIntegrityVerifier (integrity verification)
   - ITimestampCorrector (timestamp correction)
   - IBackupRepository (persistence)

2. **Data Transfer Objects**:
   - BackupPlan (configuration)
   - BackupJobResult (final result)
   - BackupProgress (progress snapshot)
   - BackupError (error information)
   - TransferResult (single file transfer outcome)
   - ExtractedMetadata (metadata extraction result)
   - VerificationResult (integrity verification result)
   - TimestampCorrectionResult (timestamp correction result)

3. **Enumerations**:
   - SourceType (Filesystem/MediaDevice)
   - BackupPhase (pipeline stages)
   - BackupJobStatus (overall job outcome)
   - BackupItemStatus (per-item status)
   - ErrorHandlingStrategy (error policies)
   - BackupErrorCode (categorized errors)
   - SidecarFormat (Json/Xml)
   - CollisionResolutionType (skip/rename/overwrite/error)
   - RenameStrategy (increment/timestamp/guid)
   - HashType (SHA2_256, SHA2_512, SHA3_*, BLAKE3_*, MD5_128)
   - BackupOutputStructure (Flat/Hierarchical)
   - CollisionResolution (append/replace/skip)

### PHASE 2: TIER 1 IMPLEMENTATION (FUNDAMENTAL - MTP BLOCKING)
**Goal:** Implement the core backup functionality that works for MTP devices (sequential approach)

1. **Data Layer Implementation**:
   - BackupPlan (immutable configuration record)
   - BackupError (categorized error record)
   - TransferResult (single file transfer result)
   - ExtractedMetadata (metadata extraction result)
   - BackupProgress (progress snapshot record)
   - BackupJobResult (final result record)
   - Context records (ScanContext, TransferContext, etc.)

2. **Scanner Layer** (MTP and Filesystem):
   - FilesystemItemScanner (Directory.EnumerateFiles-based)
   - MTPItemScanner (MediaDevices.dll-based)
   - TestItemScanner (for unit testing)
   - Both must preserve relative paths and handle errors gracefully

3. **Transfer Layer** (MTP and Filesystem):
   - FilesystemFileTransfer (chunk-based copying with progress)
   - MTPFileTransfer (device streaming to temp file then move)
   - TestFileTransfer (for unit testing)
   - Both must:
     - Auto-create destination directories
     - Handle errors gracefully (return failed TransferResult, not throw)
     - Report progress via IProgress<long>
     - Support cancellation
     - Preserve filename and directory structure

4. **Sidecar Generation Layer**:
   - JsonSidecarGenerator (create minimal sidecar after transfer)
   - XmlSidecarGenerator (alternative format)
   - TestSidecarGenerator (for unit testing)
   - Must create sidecar immediately after transfer (not last)
   - Must be updatable in-place with metadata/hashes later
   - Must handle errors gracefully

5. **Engine Layer** (Sequential only for now):
   - BackupEngine (abstract base class with orchestration template)
   - SequentialBackupEngine (processes items one-by-one)
   - ProgressTracker (thread-safe progress aggregation)
   - BackupEngineFactory (selects SequentialBackupEngine for now)

6. **Dependency Injection**:
   - ServiceCollectionExtensions with registrations for:
     - IItemScanner → FilesystemItemScanner
     - IFileTransfer → FilesystemFileTransfer
     - IBackupEngine → BackupEngine (will use Sequential initially)
     - ISidecarGenerator → JsonSidecarGenerator
     - IProgressNotifier → LoggingProgressNotifier (or similar)
     - IBackupRepository → NoOpRepository (default, no persistence)

### PHASE 3: CORE FUNCTIONALITY VALIDATION
**Goal:** Verify that the basic backup workflow works correctly

1. **Basic Workflow Testing**:
   - Scan → Transfer → Sidecar generation for both filesystem and MTP sources
   - Verify sidecars are created immediately after transfer
   - Test error handling (disk full, permission denied, etc.)
   - Test cancellation handling
   - Test dry-run mode (simulates without I/O)
   - Verify progress reporting works correctly

2. **Key Validation Points**:
   - Directory structure preservation (relative paths)
   - Error continuation (one file failure doesn't stop backup)
   - Auto-creation of destination directories
   - Complete progress reporting (BytesTransferred, ETA, speed)
   - Dry-run directory structure simulation
   - Consistent cancellation handling

### PHASE 4: TIER 2 IMPLEMENTATION (UX & USABILITY ENHANCEMENTS)
**Goal:** Add user experience and usability features

1. **Progress Enhancements**:
   - IProgressNotifier implementation (SpectreProgressNotifier)
   - Real-time phase transition reporting
   - File start/completion event reporting
   - Error event reporting
   - Completion event reporting

2. **Output Structure Features**:
   - BackupOutputStructure (Flat vs Hierarchical)
   - Custom output path patterns
   - Proper handling of both modes in transfer layer

3. **Collision Resolution**:
   - CollisionResolutionType (Append, Replace, Skip)
   - RenameStrategy (Increment, Timestamp, Guid)
   - Custom collision path patterns
   - Separate, testable collision resolution step

4. **Enhanced Dry-Run**:
   - Full simulation including directory structure creation
   - Proper handling of all phases in dry-run mode
   - No actual I/O but accurate progress reporting

### PHASE 5: TIER 3 IMPLEMENTATION (OPTIONAL FEATURES)
**Goal:** Add advanced features that are OFF by default

1. **Hashing Layer**:
   - SHA256Hasher (compute SHA256 only)
   - MultiHasher (compute multiple algorithms)
   - TestHasher (for unit testing)
   - Make hashing truly optional (null HashTypes = no hashing)
   - Compute hashes on source and destination files
   - Store both hashes in sidecar
   - Progress reporting during hash computation

2. **Metadata Extraction Layer**:
   - ExifMetadataReader (full EXIF support)
   - BasicMetadataReader (file attributes only)
   - TestMetadataReader (for unit testing)
   - Extract EXIF data from images
   - Extract file attributes (created/modified/accessed dates, attributes)
   - Store metadata in sidecar
   - Handle errors gracefully (partial data on error)

3. **Verification Layer**:
   - IntegrityVerifier (standard implementation)
   - TestVerifier (for unit testing)
   - Verify integrity by comparing pre/post-transfer hashes
   - Handle file deleted during verification
   - Handle hash mismatch (corruption detection)
   - Progress reporting during verification
   - Store verification results in sidecar

4. **Timestamp Correction Layer**:
   - TimestampCorrector (standard implementation)
   - TestTimestampCorrector (for unit testing)
   - Restore original timestamps from EXIF or file attributes
   - Require successful metadata extraction first
   - Set destination file's modified date to original
   - Return before/after timestamps
   - Handle errors gracefully

### PHASE 6: TIER 3 PARALLELISM (FILESYSTEM ONLY)
**Goal:** Implement limited parallelism for filesystem sources only

1. **LimitedParallelBackupEngine**:
   - Inherits from BackupEngine (abstract base)
   - Uses producer-consumer pattern with channels/queues
   - Scanner (1 thread) → Transfer worker pool (N threads) → Sidecar worker pool (N threads) → [Optional feature worker pools]
   - Respect MaxDegreeOfParallelism from BackupPlan
   - Default parallelism: Math.Min(4, Environment.ProcessorCount / 2)
   - Capped at 8 to prevent resource exhaustion
   - Queue depth limit for backpressure control (50 items)
   - Thread-safe collections for progress tracking
   - No circular dependencies between stages
   - Timeout on queue operations (prevents hanging)
   - Explicit error handling in all stages

2. **Parallelism Configuration**:
   - BackupPlan.MaxDegreeOfParallelism (0=auto, -1=sequential, N=limit to N)
   - BackupEngineFactory logic:
     - MTP device → Always SequentialBackupEngine
     - MaxDegreeOfParallelism == -1 → SequentialBackupEngine (user requested)
     - Filesystem → LimitedParallelBackupEngine (default)

3. **MTP Session Management** (Critical):
   - Explicit session lifecycle management
   - Open session before scanning, keep alive during transfer, close after
   - Periodic keep-alive pings (every 30 seconds)
   - Per-operation timeout (60 seconds, configurable)
   - Retry with exponential backoff (3 attempts: 1s, 2s, 4s)
   - Guard against using device after disconnect
   - Proper cleanup on any error (finally block)

### PHASE 7: INFRASTRUCTURE & PERSISTENCE (PHASE 2+ FEATURE)
**Goal:** Add resume/recovery support and other infrastructure features

1. **Backup Repository**:
   - IBackupRepository interface
   - FileSystemRepository (store in output directory)
   - SQLiteRepository (store in SQLite DB)
   - NoOpRepository (ignore persistence - default)
   - Save/load backup session state
   - Track processed items for resume
   - Handle I/O errors gracefully (log, continue without persistence)
   - Support cancellation

2. **Additional Infrastructure**:
   - Comprehensive logging throughout
   - Structured error logging with exception objects
   - Performance monitoring and metrics
   - Configuration validation
   - Resource cleanup patterns

### PHASE 8: TESTING & VALIDATION
**Goal:** Ensure reliability and correctness through comprehensive testing

1. **Unit Testing**:
   - Test each component in isolation using mocks/fakes
   - Test error conditions and edge cases
   - Verify interface contracts are honored
   - Test dependency injection container

2. **Integration Testing**:
   - Test complete workflows for both MTP and filesystem sources
   - Test all feature combinations
   - Test error recovery scenarios
   - Test cancellation at various stages
   - Test dry-run mode accuracy
   - Test progress reporting accuracy

3. **Edge Case Testing** (From CORE4_PLAN_en.md):
   - Empty directories
   - Permission denied errors
   - Disk full conditions
   - Cancelled operations at various stages
   - Deep directory structures
   - Very large files
   - Duplicate filenames
   - MTP device disconnection during operations
   - Network interruptions

4. **Performance Validation**:
   - Verify MTP backups use sequential approach (stability)
   - Verify filesystem backups use limited parallelism (performance)
   - Measure throughput and efficiency
   - Verify no resource exhaustion under load
   - Verify backpressure mechanisms work correctly

### PHASE 9: DOCUMENTATION & POLISH
**Goal:** Finalize documentation and prepare for release

1. **API Documentation**:
   - XML documentation comments on all public interfaces and classes
   - Clear explanation of responsibilities and contracts
   - Usage examples for key components

2. **User Documentation**:
   - Configuration guide (BackupPlan options)
   - Feature enablement guide
   - Error code reference
   - Progress reporting explanation
   - Performance characteristics

3. **Final Validation**:
   - Run all test suites
   - Verify against requirements from CORE4_PLAN_en.md
   - Confirm all lessons from Core/Core2/Core3 are addressed
   - Ensure no regression in core functionality

## KEY ARCHITECTURAL PRINCIPLES TO FOLLOW THROUGHOUT

1. **Separation of Concerns**: Each component has exactly one responsibility
2. **Dependency Injection**: Enable testability and flexibility
3. **Immutable Data Flow**: State flows forward via new context objects
4. **Explicit Over Implicit**: Clear error policies, data flow, and configuration
5. **Graceful Degradation**: Optional features fail without stopping backup
6. **Early Validation**: Check preconditions, create directories, validate access
7. **Comprehensive Logging**: Structured error reporting with context preservation
8. **Testable Design**: Interfaces and DI enable unit testing of individual components
9. **User Experience**: Clear progress reporting, informative messages, intuitive configuration
10. **Edge Case Focus**: Empty directories, permissions issues, disk full, cancellation, large files, deep paths
11. **Maintainability**: Simple, well-documented code that avoids unnecessary complexity

## IMPLEMENTATION PRIORITIES (FROM CORE4_ARCHITECTURE_en.md)

Follow this order to ensure MTP stability is achieved first:

**TIER 1 (FUNDAMENTAL - MTP Blocking)** - Complete first
- Engine: IBackupEngine, SequentialBackupEngine, ProgressTracker
- Scanning: FilesystemItemScanner, MTPItemScanner
- Transfer: FilesystemFileTransfer, MTPFileTransfer
- Data: BackupPlan, IBackupItem, BackupJobResult, IBackupProgress
- Sidecar: ISidecarGenerator (minimal)
- DI + Error handling + Tests

**TIER 2 (IMPORTANT - UX & Usability)** - After Tier 1 is solid
- Progress: IProgressNotifier, SpectreProgressNotifier
- Features: OutputStructure, CollisionResolution, DryRun
- Sidecar: Enrichment phases

**TIER 3 (NICE-TO-HAVE - Advanced)** - After Tiers 1-2
- Hashing: IItemHasher, SHA256Hasher
- Metadata: IMetadataReader, ExifMetadataReader
- Verification: IIntegrityVerifier
- Timestamps: ITimestampCorrector
- Parallelism: LimitedParallelBackupEngine (FS ONLY, never MTP)

**TIER 4 (LEAST IMPORTANT - Polish)** - Last
- Performance tuning, Resume, Scheduling, GUI
- ❌ NOT: Full parallelism for MTP (Core2 mistake)

## SUCCESS CRITERIA

When Core4 is complete, it should:

1. **For MTP Devices**:
   - Be completely stable (no device disconnects from parallelism)
   - Work reliably with iPhone, Android, cameras, etc.
   - Provide sequential, predictable performance
   - Generate immediate sidecars after transfer
   - Handle errors gracefully (continue with next file)
   - Preserve directory structures correctly

2. **For Filesystem Sources**:
   - Provide significantly faster backups through controlled parallelism
   - Maintain stability and reliability
   - Utilize system resources efficiently (not excessive)
   - Offer all the same features as MTP mode
   - Scale appropriately with hardware capabilities

3. **For Both Sources**:
   - Be simple to understand and debug
   - Include all features from Core2 but implemented maintainably
   - Provide comprehensive progress reporting
   - Handle all edge cases gracefully
   - Allow feature toggling via configuration
   - Provide clear error reporting and recovery options
   - Be maintainable and extensible for future enhancements

This roadmap provides a systematic approach to building Core4 that addresses all the lessons learned from previous versions while implementing the hybrid architecture specified in the design documents.