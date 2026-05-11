# Core3 - Sequential Backup Engine Learning Summary

## Overview
Core3 was a return to basics - a single-threaded, sequential implementation designed to be simple, stable, and easy to understand. While it worked reliably, it had specific limitations and flaws that Core4 needs to address.

## What Worked Well in Core3
- **Stability**: Single-threaded approach eliminated race conditions and deadlocks
- **Simplicity**: Easy-to-understand linear pipeline flow
- **Separation of concerns**: Distinct interfaces for each function (scanning, transfer, hashing, etc.)
- **Dependency injection**: Clean DI pattern for testability and flexibility
- **Immutable data flow**: Clear transformation stages without shared mutable state
- **Proper error handling**: Non-critical failures didn't stop the entire backup
- **Progress reporting**: Comprehensive IBackupProgress implementation
- **Cancellation handling**: Consistent use of cancellation tokens
- **Sidecar generation**: Created metadata files after transfer
- **Feature toggling**: Optional features controlled via BackupPlan
- **Testability**: Easy to mock individual components for unit testing

## What Still Had Issues (Per CORE4_PLAN_en.md)
Despite its strengths, Core3 had 5 critical bugs and 6 design flaws that Core4 must fix:

### Critical Bugs to Fix
1. **Directory Structure Goes Lost**: DestinationPath = fileName (just filename) causing filename collisions
2. **Transfer Errors Kill Entire Backup**: One file failure throws exception and stops entire backup
3. **Destination Directory Must Pre-Exist**: Transfer crashes if output directory doesn't exist
4. **Progress Reporting Incomplete**: BytesTransferred defined in interface but not implemented
5. **Dry-Run Mode Incomplete**: Doesn't create directory structure, causing metadata/hash phases to fail

### Design Flaws to Fix
1. **Implicit Error Handling Strategy**: No user-configurable error tolerance per operation type
2. **Hashing Always Forced**: Computes 3 hash types always, even when not wanted
3. **Sidecars Generated Last**: If hashing/metadata fails, sidecar is never generated
4. **Collision Resolution Not Testable**: Handled implicitly in scanner/transfer rather than as separate step
5. **Immutable Flow is Actually Mutable**: Items modified in-place via list indexing rather than true immutability
6. **Cancellation Handling Inconsistent**: Mixed use of ct.ThrowIfCancellationRequested() and catch(OperationCanceledException)

## Specific Technical Insights from Code Analysis
### Pipeline Architecture
Core3 used a clear sequential pipeline:
1. **Scan**: Enumerate all files from source
2. **Transfer**: Copy files from source to destination (critical operation)
3. **ExtractMetadata**: Read EXIF/file attributes (non-critical failures)
4. **GenerateHashes**: Compute cryptographic hashes (non-critical failures)
5. **CorrectTimestamps**: Restore original timestamps (non-critical failures)
6. **GenerateSidecar**: Create metadata files (non-critical failures)

### Key Implementation Details
- Used interfaces: IBackupScanner, IFileTransfer, IItemHasher, IMetadataReader, ISidecarGenerator
- BackupEngineSequential orchestrated the pipeline stages
- Each stage processed items one-at-a-time in strict sequence
- Errors in non-critical stages were logged but didn't halt processing
- Transfer failures were treated as critical and stopped the backup (to be fixed in Core4)
- Progress reported through IBackupProgress with phase, current file, and counters
- Proper cancellation handling with ct.ThrowIfCancellationRequested()
- Immutable BackupItem flowed through pipeline with enrichment at each stage
- Sidecar generation happened after transfer but before optional features (still not early enough)

### Strengths of the Approach
- Easy to follow data flow: Scan → Transfer → Metadata → Hash → Timestamps → Sidecar
- Each component had single responsibility
- Dependencies injected via constructor for testability
- Clear phase separation made debugging straightforward
- Configuration driven through BackupPlan object
- Comprehensive progress reporting for UI feedback
- Proper resource management and cleanup

## Lessons for Core4 Implementation
### What to Preserve
- Sequential approach as foundation (especially for MTP stability)
- Separation of concerns with distinct interfaces
- Dependency injection for testability and flexibility
- Immutable data flow where each stage transforms items
- Comprehensive progress reporting
- Proper cancellation handling
- Feature toggling via BackupPlan
- Error handling that distinguishes critical vs non-critical failures
- Sidecar generation concept (but needs to happen earlier)
- Testable components with clear interfaces

### What to Improve/Fix
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
- Test edge cases thoroughly (empty dirs, permissions, disk full, etc.)
- Document error policies clearly (which errors stop backup, which are tolerated)

### Key Takeaway
Core3 demonstrated that simplicity and stability are valuable foundations, but a production backup engine needs more sophisticated error handling, feature flexibility, and attention to edge cases. Core4 should keep Core3's architectural strengths while addressing its specific limitations through targeted improvements.