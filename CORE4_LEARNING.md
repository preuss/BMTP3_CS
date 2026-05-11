# Core4 - Hybrid Backup Engine Learning Summary

## Overview
Core4 is designed as a hybrid solution that combines the best aspects of Core (feature-rich), Core2 (performance aspirations), and Core3 (stability and simplicity) while avoiding their respective pitfalls. Based on the CORE4_PLAN_en.md and CORE4_ARCHITECTURE_en.md documents, Core4 implements an adaptive approach: sequential for MTP devices and limited parallelism for filesystem sources.

## Key Learnings from Previous Cores

### From Core (Original)
**Preserve:**
- Feature completeness (media device support, drive backup, file patterns, sidecars)
- Safe temp file processing approach
- Comprehensive metadata collection
- Proper resource cleanup and connection handling
- User-friendly progress reporting
- Path sanitization for compatibility

**Avoid:**
- Overly complex refactoring that breaks existing functionality
- Loss of core features during "improvements"
- Code that becomes difficult to maintain

### From Core2 (Multithreaded)
**Preserve:**
- Separation of concerns principle (each component does one thing well)
- Dependency injection for testability and flexibility
- Progress tracking with meaningful metrics (speed, ETA, throughput)
- Cancellation support throughout the pipeline
- Component-based architecture enabling independent testing
- Configuration options for performance tuning (with safe defaults)

**Avoid:**
- Complex channel pipelines with multiple writers to bounded channels (deadlock risk)
- Shared mutable state without proper synchronization (race conditions)
- Manual task orchestration that's error-prone and hard to debug
- Implicit error handling that obscures failure contexts
- Brittle architectures dependent on precise channel capacity tuning
- "Bolted on" device management risking disconnected resource usage
- Over-engineering simple processes with excessive abstraction layers

### From Core3 (Simple Sequential)
**Preserve:**
- Sequential approach as foundation (especially for MTP stability)
- Separation of concerns with distinct interfaces
- Dependency injection for testability and flexibility
- Immutable data flow where each stage transforms items
- Comprehensive progress reporting
- Proper cancellation handling
- Feature toggling via BackupPlan
- Error handling distinguishing critical vs non-critical failures
- Sidecar generation concept
- Testable components with clear interfaces

**Improve/Fix:**
- Make directory structure preservation explicit (use relative paths)
- Change transfer error handling to log and continue rather than throw
- Auto-create destination directories before transfer
- Implement complete progress reporting including BytesTransferred
- Enhance dry-run to simulate directory structure creation
- Make error handling strategy explicit and configurable per operation
- Make hashing truly optional (null means no hashing)
- Generate sidecars immediately after transfer and update later with features
- Separate collision resolution into distinct, testable step
- Ensure true immutability or clearly document mutability
- Standardize cancellation handling pattern throughout
- Test edge cases thoroughly (empty dirs, permissions, disk full, cancellation, etc.)
- Document error policies clearly (which errors stop backup, which are tolerated)

## Core4's Hybrid Architecture Approach

### Sequential Strategy (For MTP Devices)
- Process files one at a time: Scan → Transfer → Sidecar → [Optional: Hash/Meta/Verify/Timestamp]
- Stable for MTP devices that don't support parallel access
- Simple, easy to debug, reliable
- One file at a time prevents device timeouts and disconnections

### Limited Parallelism Strategy (For Filesystem)
- Multiple files processed simultaneously with controlled concurrency
- Scanner (1 thread) → Transfer worker pool (N threads) → Sidecar worker pool (N threads) → [Optional feature worker pools]
- Faster backups for filesystem sources while maintaining stability
- Max workers = min(4, Environment.ProcessorCount / 2) with queue depth limits for backpressure control
- Thread-safe collections prevent race conditions
- No circular dependencies between stages

## Key Architectural Improvements in Core4

### 1. Adaptive Source Strategy
- Automatically detects source type (MTP vs Filesystem)
- Uses SequentialBackupEngine for MTP devices
- Uses LimitedParallelBackupEngine for filesystem sources
- User can override with MaxDegreeOfParallelism = -1 for forced sequential

### 2. Early Sidecar Generation
- Generate minimal sidecar immediately after successful transfer
- Update sidecar later with optional features (hashes, metadata, verification)
- Ensures sidecar exists even if later phases fail
- Provides immediate audit trail and documentation

### 3. Explicit, Configurable Error Handling
- ErrorStrategy enum per operation type (StopOnError, SkipOnError, RetryOnError)
- Transfer errors: Log, mark file failed, continue with next file (don't stop backup)
- Optional feature errors: Log warning, skip enhancement, continue
- Clear distinction between critical (stop backup) and tolerable (continue) errors

### 4. Optional Features
- Hashing: Truly optional (null means no hashing)
- Metadata extraction: Configurable via BackupPlan.ExtractMetadata
- Verification: Configurable via BackupPlan.VerifyIntegrity
- Timestamp correction: Configurable via BackupPlan.CorrectTimestamps
- All optional features are gracefully skippable

### 5. Comprehensive Progress Reporting
- Tracks: Files discovered, processed, succeeded, failed, skipped
- Tracks: Bytes total, processed, per-second rate
- Current file being processed with bytes transferred
- Calculated metrics: Percentage complete, transfer speed, ETA
- Thread-safe progress tracking suitable for both sequential and parallel execution

### 6. Proper Directory Handling
- Preserve relative paths from source to destination (no filename collisions)
- Auto-create destination directories before transfer
- Simulate directory structure creation in dry-run mode

### 7. Testability and Separation of Concerns
- Clear API layer with interfaces (IBackupEngine, IBackupScanner, IFileTransfer, etc.)
- Implementation layer with focused components
- Orchestration layer that coordinates the flow
- Dependency injection for easy mocking and testing
- Each component has single responsibility

### 8. Robust Cancellation Handling
- Consistent pattern using CancellationToken throughout
- Regular checks for ct.IsCancellationRequested or ct.ThrowIfCancellationRequested()
- Clean shutdown with proper resource cleanup
- Returns partial results when cancelled rather than failing completely

## Implementation Priorities (From CORE4_ARCHITECTURE_en.md)

### TIER 1 (FUNDAMENTAL - MTP Blocking)
- Engine: IBackupEngine, SequentialBackupEngine, ProgressTracker
- Scanning: FilesystemItemScanner, MTPItemScanner
- Transfer: FilesystemFileTransfer, MTPFileTransfer
- Data: BackupPlan, IBackupItem, BackupJobResult, IBackupProgress
- Sidecar: ISidecarGenerator (minimal)
- DI + Error handling + Tests

### TIER 2 (IMPORTANT - UX & Usability)
- Progress: IProgressNotifier, SpectreProgressNotifier
- Features: OutputStructure, CollisionResolution, DryRun
- Sidecar: Enrichment phases

### TIER 3 (NICE-TO-HAVE - Advanced)
- Hashing: IItemHasher, SHA256Hasher
- Metadata: IMetadataReader, ExifMetadataReader
- Verification: IIntegrityVerifier
- Timestamps: ITimestampCorrector
- Parallelism: LimitedParallelBackupEngine (FS ONLY, never MTP)

### TIER 4 (LEAST IMPORTANT - Polish)
- Performance tuning, Resume, Scheduling, GUI
- ❌ NOT: Full parallelism for MTP (Core2 mistake)

## Key Takeaways for Core4 Implementation

1. **Stability First**: Ensure MTP backups are rock-solid before optimizing filesystem performance
2. **Adaptive Approach**: Match strategy to source type rather than one-size-fits-all
3. **Explicit Over Implicit**: Clear error policies, data flow, and configuration
4. **Graceful Degradation**: Optional features fail without stopping backup
5. **Early Validation**: Check preconditions, create directories, validate access
6. **Comprehensive Logging**: Structured error reporting with context preservation
7. **Testable Design**: Interfaces and DI enable unit testing of individual components
8. **User Experience**: Clear progress reporting, informative messages, intuitive configuration
9. **Edge Case Focus**: Empty directories, permissions issues, disk full, cancellation, large files, deep paths
10. **Maintainability**: Simple, well-documented code that avoids unnecessary complexity

By combining the feature richness of Core, the performance aspirations of Core2 (implemented safely), and the stability/simplicity of Core3 (with improvements), Core4 aims to be a robust, flexible, and maintainable backup engine suitable for both MTP devices and filesystem sources.