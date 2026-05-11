# Core4 Completion Summary
## Essential Steps to Complete the Hybrid Backup Engine

Based on thorough analysis of CORE4_PLAN_en.md and CORE4_ARCHITECTURE_en.md, here are the critical steps to complete Core4 implementation:

## CORE IMPLEMENTATION PRINCIPLES
1. **Hybrid Architecture**: Sequential for MTP devices, Limited Parallelism for filesystem
2. **Separation of Concerns**: Each component has exactly one responsibility
3. **Explicit Error Handling**: Configurable strategies per operation type (Stop/Skip/Retry)
4. **Early Sidecar Generation**: Create minimal sidecar immediately after transfer, update later
5. **Immutable Data Flow**: State flows forward via new context objects
6. **Dependency Injection**: Enable testability and flexibility
7. **Comprehensive Progress Reporting**: BytesTransferred, ETA, speed, active workers
8. **Graceful Degradation**: Optional features fail without stopping backup
9. **Consistent Cancellation**: Single pattern throughout codebase
10. **Testable Design**: Interfaces allow mocking/faking for unit tests

## IMPLEMENTATION PHASES (IN ORDER)

### PHASE 1: TIER 1 FUNDAMENTALS (MTP-FOCUSED)
**Must complete first for MTP stability:**
- Api layer: All interfaces (IBackupEngine, IBackupProgress, IBackupItem, etc.)
- Data layer: BackupPlan, BackupJobResult, BackupProgress, BackupError, context records
- Scanner layer: FilesystemItemScanner & MTPItemScanner (preserve relative paths)
- Transfer layer: FilesystemFileTransfer & MTPFileTransfer (auto-create dirs, graceful errors)
- Sidecar layer: JsonSidecarGenerator (create IMMEDIATELY after transfer)
- Engine layer: BackupEngine (abstract) + SequentialBackupEngine (one file at a time)
- Progress layer: ProgressTracker (thread-safe aggregation)
- Factory: BackupEngineFactory (selects Sequential for MTP)
- DI: Basic service registrations
- Validation: Test basic scan→transfer→sidecar workflow with error handling

### PHASE 2: TIER 2 UX ENHANCEMENTS
**After Tier 1 is solid:**
- Progress notifications: IProgressNotifier & SpectreProgressNotifier (real-time events)
- Output structure: Flat vs Hierarchical handling
- Collision resolution: Separate, testable step with strategies (skip/rename/overwrite)
- Enhanced dry-run: Full simulation including directory structure
- Custom path/pattern support

### PHASE 3: TIER 3 OPTIONAL FEATURES
**After Tiers 1-2:**
- Hashing layer: IItemHasher & SHA256Hasher (truly optional via BackupPlan.HashTypes)
- Metadata layer: IMetadataReader & ExifMetadataReader (EXIF + file attributes)
- Verification layer: IIntegrityVerifier (hash comparison for integrity)
- Timestamp layer: ITimestampCorrector (restore from EXIF, require metadata success)

### PHASE 4: TIER 3 PARALLELISM (FILESYSTEM ONLY)
**Critical: NEVER for MTP:**
- LimitedParallelBackupEngine (inherits from BackupEngine)
- Producer-consumer pattern: Scanner(1) → Transfer(N) → Sidecar(N) → [Optional features](N)
- MaxWorkers = Min(4, ProcessorCount/2), capped at 8
- Queue depth limit = 50 (backpressure control)
- Thread-safe collections & explicit error handling in all stages
- Factory logic: MTP → Sequential, MaxDoP=-1 → Sequential, Filesystem → LimitedParallel

### PHASE 5: MTP SESSION MANAGEMENT (CRITICAL)
**Must be rock-solid:**
- Explicit lifecycle: Open session before scanning, keep alive, close after transfer
- Keep-alive pings every 30 seconds
- Per-operation timeout: 60 seconds (configurable)
- Retry with exponential backoff: 1s, 2s, 4s (3 attempts)
- Guard against using device after disconnect
- Proper cleanup in finally blocks
- Handle device disconnection gracefully during all phases

### PHASE 6: INFRASTRUCTURE & PERSISTENCE
**Phase 2+ feature:**
- IBackupRepository interface
- FileSystemRepository (output directory storage)
- SQLiteRepository (SQLite DB storage)
- NoOpRepository (default - no persistence)
- Save/load backup session state
- Track processed items for resume capability
- Handle I/O errors gracefully (log, continue without persistence)

## KEY SPECIFICATIONS FROM DOCUMENTS

### Sidecar Flow (CRITICAL DIFFERENT FROM CORE3)
```
Transfer ✅ → GenerateSidecar (MINIMAL, NOW)
           ├─ source_path
           ├─ destination_path
           ├─ transferred_at
           ├─ file_size
           └─ transfer_status: "success"
           
[Later, if enabled]
ExtractMetadata → UpdateSidecar (add metadata fields)
GenerateHashes → UpdateSidecar (add hash fields)
VerifyIntegrity → UpdateSidecar (add verification fields)
CorrectTimestamps → UpdateSidecar (timestamps corrected)

Result: Sidecar exists immediately, gradually enriched with features
```

### Error Handling Policy
```
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
```

### Progress Tracking Must Include
- DirectoriesScanned, FilesDiscovered, BytesTotal
- FilesProcessed, FilesSucceeded, FilesFailed, FilesSkipped, BytesProcessed
- CurrentFilePath, CurrentFileBytes, CurrentFileBytesProcessed
- Calculated: PercentageComplete, BytesPerSecond, EstimatedTimeRemaining
- BackupPhase CurrentPhase, ElapsedMs

## AVOIDING PAST MISTAKES

### From Core (Original)
- Don't let refactoring break existing functionality
- Prioritize maintainability from the start
- Keep feature richness but with clean architecture

### From Core2 (Multithreaded)
- ❌ NO complex channel pipelines (9-channel infrastructure)
- ❌ NO shared mutable state without synchronization (race conditions)
- ❌ NO manual task chaining (error-prone Task.WhenAll)
- ❌ NO implicit error handling (make policies explicit)
- ❌ NO brittle architectures dependent on channel capacity tuning
- ❌ NO "bolted on" MTP session management
- ❌ NO over-engineering simple processes

### From Core3 (Simple Sequential)
- ✅ Preserve sequential approach as foundation (for MTP stability)
- ✅ Preserve separation of concerns with distinct interfaces
- ✅ Preserve dependency injection for testability
- ✅ Preserve immutable data flow where stages transform items
- ✅ Preserve comprehensive progress reporting
- ✅ Preserve proper cancellation handling
- ✅ Preserve feature toggling via BackupPlan
- ✅ Preserve error handling that distinguishes critical vs non-critical
- ✅ PRESERVE sidecar generation concept
- ✅ PRESERVE testable components with clear interfaces
- ❌ FIX: Directory structure preservation (use relative paths)
- ❌ FIX: Transfer errors → log & continue (don't throw/stop backup)
- ❌ FIX: Auto-create destination directories before transfer
- ❌ FIX: Implement COMPLETE progress reporting (BytesTransferred, etc.)
- ❌ FIX: Enhance dry-run to simulate directory structure creation
- ❌ FIX: Make error handling strategy EXPLICIT & CONFIGURABLE per operation
- ❌ FIX: Make hashing TRULY OPTIONAL (null means no hashing)
- ❌ FIX: Generate sidecars IMMEDIATELY after transfer, update later
- ❌ FIX: Separate collision resolution into DISTINCT, testable step
- ❌ FIX: Ensure TRUE immutability OR clearly document mutability
- ❌ FIX: Standardize cancellation handling PATTERN throughout
- ❌ FIX: Test edge cases THOROUGHLY (empty dirs, permissions, disk full, etc.)
- ❌ FIX: Document error policies CLEARLY (which errors stop vs tolerate)

## VALIDATION CHECKPOINTS

Before moving to next phase, verify:
1. **MTP Stability**: Can backup iPhone/Android without device disconnects
2. **Basic Workflow**: Scan → Transfer → Sidecar works for both source types
3. **Error Continuity**: One file failure doesn't stop entire backup
4. **Progress Accuracy**: BytesTransferred, ETA, speed calculations correct
5. **Path Preservation**: Directory structures maintained correctly
6. **Dry-run Accuracy**: Simulates correctly without actual I/O
7. **Cancellation**: Stops gracefully, returns partial results
8. **Sidecar Timing**: Created immediately after transfer, updatable later
9. **DI Functionality**: Can replace implementations with test fakes
10. **Error Handling**: Explicit strategies work as configured

This summary captures the essence of what needs to be built to complete Core4 according to the architectural specifications and lessons learned from previous versions. Follow the implementation phases in order, with special attention to MTP stability and the hybrid approach that gives you sequential reliability where needed and parallel performance where possible.