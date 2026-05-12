# CORE3 - Minimalist Single-Threaded Backup Engine

> **Status: Historical reference only**
>
> This document describes the planned direction for **Core3**, not the active target architecture for Core4.
> Use it only as historical context or inspiration.
>
> For the current Core4 direction, use:
> - `docs\CORE4_IMPLEMENTATION_GUIDE_DA.md`
> - `docs\CORE4_ARCHITECTURE.md`
> - `docs\CORE4_PLAN.md`
> - `docs\CORE4_MASTER_SYNTHESIS.md`

## Executive Summary

**Core3** replaces the over-engineered Core2 with a **simple, single-threaded, step-based backup engine** that is:
- 🎯 **Easy to understand** - linear execution, no hidden concurrency
- 🧪 **Testable** - each step is a discrete unit that can be tested in isolation
- 🔧 **Maintainable** - SOLID principles applied throughout
- 🚀 **Reliable** - no channels, no thread pools, no race conditions

---

## Problem Analysis

### Issues with Core2

| Issue | Root Cause | Impact |
|-------|-----------|--------|
| **Complexity** | System.Threading.Channels + parallel worker pools | Hard to debug, understand, extend |
| **Hidden Bugs** | Concurrency issues, race conditions, deadlocks | Unpredictable failures |
| **Poor Testability** | Async pipelines, stateful channels | Difficult to test step in isolation |
| **Maintenance** | Spread across multiple pipeline stages, handlers | Changes ripple across layers |

### Root Cause
Core2 premature optimization: built parallel pipeline infrastructure before proving need for parallelization.

---

## Core3 Philosophy

**"Make it simple. Make it work. Then optimize if needed."**

### Design Principles

1. **Pragmatic Simplicity** - No abstraction without proven need
2. **Explicit Data Flow** - Methods have clear input/output, no hidden mutations
3. **Testability First** - Test doubles over mocks, real implementations in tests
4. **Linear Execution** - No hidden async, no channels, trace with debugger
5. **Fail Fast on Critical** - Stop on transfer errors, tolerate metadata/hash failures
6. **Immutable Flow** - Items transform through pipeline, nothing mutates shared state

---

## Architecture Overview

### High-Level Flow

```
Input: BackupPlan
         │
         ▼
┌─────────────────────────────────┐
│   BackupEngine.RunAsync()       │
│   (Single method, ~300 lines)   │
│                                 │
│ 1. items = Scan()               │
│ 2. items = Transfer(items)      │──→ Per-file + immediate sidecar
│ 3. items = ExtractMetadata()    │
│ 4. items = GenerateHashes()     │──→ Can parallelize internally
│ 5. items = CorrectTimestamps()  │
│ 6. return BuildResult(items)    │
│                                 │
└─────────────────────────────────┘
         │
         ▼
Output: BackupJobResult
  ├─ Success: bool
  ├─ TotalItems: int
  ├─ SuccessfulItems: int
  ├─ FailedItems: int
  ├─ TotalBytes: long
  ├─ Duration: TimeSpan
  └─ Errors: List<BackupError>
```

**Key Points:**
- **No abstractions**: Direct method calls in BackupEngine
- **Immutable data flow**: `items = await Scan()` → `items = await Transfer(items)` → ...
- **One responsibility**: BackupEngine orchestrates, delegates to services
- **Services injected**: IBackupScanner, IFileTransfer, IHashGenerator, etc.

### Layers & Responsibilities

```
┌────────────────────────────────────────────────────┐
│ API LAYER (Contracts)                              │
│ • IBackupEngine                                    │
│ • IBackupStep                                      │
│ • IBackupContext                                   │
│ • IBackupProgress                                  │
│ • BackupPlan, BackupJobResult, DTOs               │
└────────────────────────────────────────────────────┘
                            △
                            │ depends on
                            │
┌────────────────────────────────────────────────────┐
│ APPLICATION LAYER (Use Cases)                      │
│ • BackupEngine (orchestrator)                      │
│ • Step implementations (ScanStep, TransferStep...) │
│ • BackupSession (state holder)                     │
└────────────────────────────────────────────────────┘
                            △
                            │ depends on
                            │
┌────────────────────────────────────────────────────┐
│ DOMAIN LAYER (Business Logic)                      │
│ • BackupItem (file/folder to backup)              │
│ • BackupJob (configuration)                        │
│ • Domain value objects                             │
└────────────────────────────────────────────────────┘
                            △
                            │ depends on
                            │
┌────────────────────────────────────────────────────┐
│ INFRASTRUCTURE LAYER (Implementation Details)      │
│ • FileSystemScanner                                │
│ • DeviceScanner (MTP/PTP)                         │
│ • FileTransfer                                     │
│ • FileHasher                                       │
│ • MetadataReader                                   │
│ • Repositories                                     │
└────────────────────────────────────────────────────┘
```

---

## Sequential Backup Workflow

### Sequential Backup Workflow (Simplified)

### Step-by-Step Execution

```
Input: BackupPlan
  └─ source: filesystem or MTP device
  └─ destination: output directory
  └─ dryRun: if true, simulate without writing
  └─ options: collision strategy, etc.

                         │
                         ▼
         ┌───────────────────────────────┐
         │ 1. SCAN (Immutable Input)     │
         │ • Traverse source             │
         │ • Return List<BackupItem>     │
         └────────────┬───────────────────┘
                      │ items: List<BackupItem>
                      ▼
         ┌───────────────────────────────┐
         │ 2. TRANSFER + SIDECAR         │
         │ • Per-file operation:         │
         │   a) Copy file to dest        │
         │   b) Generate sidecar         │
         │   c) Resolve collision        │
         │ • If transfer fails → STOP    │
         │ • Return transformed items    │
         └────────────┬───────────────────┘
                      │ items: List<BackupItem> (transferred)
                      ▼
         ┌───────────────────────────────┐
         │ 3. EXTRACT METADATA           │
         │ • Read file timestamps        │
         │ • Get file properties         │
         │ • Extract EXIF (images)       │
         │ • Non-critical failures OK    │
         └────────────┬───────────────────┘
                      │ items: List<BackupItem> (with metadata)
                      ▼
         ┌───────────────────────────────┐
         │ 4. GENERATE HASHES            │
         │ • Compute SHA-256             │
         │ • Can parallelize internally  │
         │ • Non-critical failures OK    │
         └────────────┬───────────────────┘
                      │ items: List<BackupItem> (with hash)
                      ▼
         ┌───────────────────────────────┐
         │ 5. CORRECT TIMESTAMPS         │
         │ • Restore original times      │
         │ • Set access/write times      │
         │ • Non-critical failures OK    │
         └────────────┬───────────────────┘
                      │ items: List<BackupItem> (corrected)
                      ▼
         ┌───────────────────────────────┐
         │ 6. BUILD RESULT               │
         │ • Aggregate statistics        │
         │ • Collect errors              │
         │ • Return BackupJobResult      │
         └────────────┬───────────────────┘
                      │
                      ▼
         Output: BackupJobResult
           └─ success, files, bytes, errors, duration
```

**Key Design Decisions:**

1. **Immutable Data Flow**: Each step takes items, returns transformed items
   - No hidden mutations
   - Clear input/output
   - Easy to test: `var result = await Scan(); Assert.NotEmpty(result);`

2. **Per-File Transfer + Sidecar** (not separate step)
   - File = transferred + sidecar generated (atomic per-file operation)
   - If transfer fails, stop immediately (critical error)
   - If sidecar fails, tolerate (file still backed up)

3. **Non-Critical Errors Are Tolerated**
   - Metadata extraction fails? Log it, continue (file is backed up)
   - Hash generation fails? Log it, continue
   - Timestamp correction fails? Log it, continue
   - Transfer fails? **STOP** (critical error)

4. **Dry-Run Support**
   - `plan.DryRun = true` → simulate without writing
   - Useful for testing backup plans without data loss risk

---

## Key Interfaces & Contracts

### IBackupEngine (Main Entry Point)

```csharp
public interface IBackupEngine
{
    /// Execute a backup plan
    /// Single method, simple contract
    Task<BackupJobResult> RunAsync(
        BackupPlan plan, 
        IProgress<IBackupProgress> progress, 
        CancellationToken ct
    );
}
```

### BackupEngine (Implementation, ~300 lines)

```csharp
public class BackupEngine : IBackupEngine
{
    private readonly IBackupScanner _scanner;
    private readonly IFileTransfer _fileTransfer;
    private readonly IHashGenerator _hashGenerator;
    private readonly IMetadataReader _metadataReader;
    private readonly ISidecarGenerator _sidecarGenerator;
    private readonly ILogger<BackupEngine> _logger;
    
    public async Task<BackupJobResult> RunAsync(
        BackupPlan plan, 
        IProgress<IBackupProgress> progress, 
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try {
            // Step 1: Scan
            var items = await Scan(plan, progress, ct);
            _logger.LogInformation($"Scanned {items.Count} items");
            
            // Step 2: Transfer + Sidecar (per-file operation)
            items = await TransferAndGenerateSidecars(items, plan, progress, ct);
            _logger.LogInformation($"Transferred {items.Count} items");
            
            // Step 3: Metadata (non-critical failures tolerated)
            items = await ExtractMetadata(items, progress, ct);
            
            // Step 4: Hashing (non-critical failures tolerated)
            items = await GenerateHashes(items, progress, ct);
            
            // Step 5: Timestamps (non-critical failures tolerated)
            items = await CorrectTimestamps(items, progress, ct);
            
            return BuildSuccessResult(items, stopwatch.Elapsed);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Backup failed");
            return BuildFailureResult(ex, stopwatch.Elapsed);
        }
    }
    
    // Scan returns items
    private async Task<List<BackupItem>> Scan(BackupPlan plan, IProgress<IBackupProgress> progress, CancellationToken ct) {
        // Input: plan.Source
        // Output: List<BackupItem>
    }
    
    // Transfer takes items, returns items with transfer state
    private async Task<List<BackupItem>> TransferAndGenerateSidecars(
        List<BackupItem> items, 
        BackupPlan plan, 
        IProgress<IBackupProgress> progress, 
        CancellationToken ct) {
        
        var results = new List<BackupItem>();
        
        for (int i = 0; i < items.Count; i++) {
            var item = items[i];
            
            // Report progress
            progress?.Report(new BackupProgress {
                Phase = BackupPhase.Transferring,
                CurrentFile = item.Name,
                FilesProcessed = i,
                FilesTotal = items.Count
            });
            
            try {
                // Critical: Transfer the file
                if (!plan.DryRun) {
                    await _fileTransfer.CopyAsync(item, plan.Destination, ct);
                }
                
                // Non-critical: Generate sidecar
                try {
                    await _sidecarGenerator.GenerateAsync(item, plan.Destination, ct);
                } catch (Exception ex) {
                    _logger.LogWarning(ex, $"Sidecar generation failed for {item.Name}");
                    // Continue: file is backed up, sidecar is bonus
                }
                
                results.Add(item.WithTransferSuccess());
            }
            catch (Exception ex) {
                _logger.LogError(ex, $"Transfer failed for {item.Name}");
                throw;  // Critical error: stop immediately
            }
        }
        
        return results;
    }
    
    // Other steps follow same pattern: take items, return items
    private async Task<List<BackupItem>> ExtractMetadata(List<BackupItem> items, IProgress<IBackupProgress> progress, CancellationToken ct) { /*...*/ }
    private async Task<List<BackupItem>> GenerateHashes(List<BackupItem> items, IProgress<IBackupProgress> progress, CancellationToken ct) { /*...*/ }
    private async Task<List<BackupItem>> CorrectTimestamps(List<BackupItem> items, IProgress<IBackupProgress> progress, CancellationToken ct) { /*...*/ }
}
```

**Key Pattern:**
- Each method: `Task<List<BackupItem>> MethodAsync(List<BackupItem> items, ...)`
- Immutable flow: items transform through methods
- Easy to test: `var result = await TransferAndGenerateSidecars(testItems, testPlan, ...)`
- No context mutation, no side effects

### Supporting Interfaces (Injected Services)

```csharp
public interface IBackupScanner {
    Task<IEnumerable<BackupItem>> ScanAsync(string source, CancellationToken ct);
}

public interface IFileTransfer {
    Task CopyAsync(BackupItem item, string destination, CancellationToken ct);
}

public interface IHashGenerator {
    Task<string> GenerateAsync(BackupItem item, CancellationToken ct);
}

public interface IMetadataReader {
    Task<FileMetadata> ReadAsync(BackupItem item, CancellationToken ct);
}

public interface ISidecarGenerator {
    Task GenerateAsync(BackupItem item, string destination, CancellationToken ct);
}
```

### Data Models

```csharp
public class BackupPlan {
    public string Source { get; init; }
    public string Destination { get; init; }
    public bool DryRun { get; init; }  // Simulate without writing
    public CollisionStrategy Collision { get; init; } = CollisionStrategy.Rename;
}

public class BackupItem {
    public string Name { get; init; }
    public string SourcePath { get; init; }
    public string DestinationPath { get; init; }
    public long SizeInBytes { get; init; }
    public BackupItemType Type { get; init; }  // File or Folder
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; init; }
    
    // State (non-critical, can fail)
    public FileMetadata? Metadata { get; init; }
    public string? Hash { get; init; }
    public string? SidecarPath { get; init; }
    
    // Helpers
    public BackupItem WithTransferSuccess() => this with { DestinationPath = /* resolved */ };
    public BackupItem WithMetadata(FileMetadata m) => this with { Metadata = m };
    public BackupItem WithHash(string h) => this with { Hash = h };
}

public class BackupJobResult {
    public bool Success { get; init; }
    public int TotalItems { get; init; }
    public int SuccessfulItems { get; init; }
    public int FailedItems { get; init; }
    public long TotalBytes { get; init; }
    public TimeSpan Duration { get; init; }
    public List<BackupError> Errors { get; init; }
}

public class BackupError {
    public string ItemName { get; init; }
    public string Message { get; init; }
    public Exception? Exception { get; init; }
}

public interface IBackupProgress {
    string CurrentFile { get; }
    long BytesTransferred { get; }
    int FilesProcessed { get; }
    int FilesTotal { get; }
    BackupPhase Phase { get; }
}

public enum BackupPhase {
    Scanning,
    Transferring,
    ExtractingMetadata,
    GeneratingHashes,
    CorrectingTimestamps,
    Complete
}
```

---

## Step Implementation Pattern

### All Steps Follow Same Contract

Each method in BackupEngine:
1. **Input**: `List<BackupItem>` (items from previous step)
2. **Processing**: Transform items, handle errors
3. **Output**: `Task<List<BackupItem>>` (transformed items for next step)

```csharp
private async Task<List<BackupItem>> GenerateHashes(
    List<BackupItem> items,
    IProgress<IBackupProgress> progress,
    CancellationToken ct)
{
    var results = new List<BackupItem>();
    
    _logger.LogInformation($"Generating hashes for {items.Count} items");
    
    foreach (var item in items) {
        try {
            progress?.Report(new BackupProgress {
                Phase = BackupPhase.GeneratingHashes,
                CurrentFile = item.Name,
            });
            
            // IMPORTANT: Compute ALL hash types in single pass
            var hashes = await _itemHasher.ComputeHashesAsync(
                item,
                _backupPlan.HashTypes,  // All configured hash types
                progress,
                ct
            );
            
            results.Add(item.WithHashes(hashes));  // Dictionary<HashType, string>
        }
        catch (Exception ex) {
            // Non-critical: log and continue
            _logger.LogWarning(ex, $"Hash generation failed for {item.Name}");
            results.Add(item);  // Add item even without hashes
        }
    }
    
    return results;
}
```

**Why This Works:**
- ✓ Immutable: input not modified, new list returned
- ✓ Testable: `await GenerateHashes(testItems, progress, ct)` - no mocking
- ✓ Clear error handling: decide per-method if error is critical or non-critical
- ✓ Composable: methods chain naturally

## Multiple Hash Types (Core Requirement)

**From Core2, Core3 Must Support All Hash Algorithms in Single Pass:**

```csharp
public enum HashType {
    SHA3_512_FIPS202,    // SHA-3 512 (FIPS 202)
    SHA3_256_FIPS202,    // SHA-3 256 (FIPS 202)
    SHA3_512_KECCAK,     // SHA-3 512 (Keccak original)
    SHA3_256_KECCAK,     // SHA-3 256 (Keccak original)
    SHA2_256,            // SHA-256
    SHA2_512,            // SHA-512
    MD5_128,             // MD5 (legacy, but supported)
    BLAKE3_256,          // BLAKE3 256-bit
    BLAKE3_512           // BLAKE3 512-bit
}
```

**Hash Generation Contract:**
```csharp
public interface IItemHasher {
    /// Compute ALL requested hash types for item in SINGLE pass through file
    Task<Dictionary<HashType, string>> ComputeHashesAsync(
        BackupItem item,
        List<HashType> hashTypes,
        IProgress<ulong> progress,
        CancellationToken ct
    );
}

public interface IHashGenerator {
    /// Compute multiple hashes from stream efficiently
    Task<Dictionary<HashType, string>> ComputeHashesAsync(
        Stream stream,
        IEnumerable<HashType> hashTypes,
        IProgress<ulong> progress,
        CancellationToken ct
    );
}
```

**Key Design Points:**
- **Single-pass hashing**: Read file once, compute all hashes simultaneously
- **Efficient**: 9 algorithms × 1 file read = better than 9 file reads
- **Progress**: Report bytes processed, not per-algorithm
- **Configuration**: BackupPlan specifies which hash types to compute

**BackupPlan Extension:**
```csharp
public class BackupPlan {
    public string Source { get; init; }
    public string Destination { get; init; }
    public bool DryRun { get; init; }
    public List<HashType> HashTypes { get; init; } = new() {
        HashType.SHA2_256,
        HashType.SHA3_256_FIPS202,
        HashType.BLAKE3_256
    };  // Default: 3 hash types, user can configure
}
```

**Sidecar Format (Includes All Hashes):**
```ini
[Metadata]
FileName=photo.jpg
SourcePath=/phone/DCIM/photo.jpg
TransferredAt=2026-05-02T15:20:00Z
FileSize=5242880

[Hashes]
SHA2_256=abc123def456...
SHA3_256_FIPS202=def789ghi012...
BLAKE3_256=xyz789abc123...
```

---

## Directory Structure (Pragmatic, Flat)

```
BMTP3.Core3/
│
├── BackupEngine.cs                    (Main class, ~300 lines)
├── BackupPlan.cs                      (Input configuration)
├── BackupItem.cs                      (Domain model)
├── BackupJobResult.cs                 (Output result)
├── BackupError.cs                     (Error type)
├── BackupPhase.cs                     (Progress phases enum)
├── IBackupProgress.cs                 (Progress contract)
├── IBackupEngine.cs                   (Main contract)
│
├── Scanning/                          (Scanners: filesystem, devices)
│   ├── IBackupScanner.cs
│   ├── FileSystemScanner.cs
│   └── DeviceScanner.cs               (MTP/PTP implementation)
│
├── Transfer/                          (File transfer)
│   ├── IFileTransfer.cs
│   ├── SimpleFileTransfer.cs
│   └── TransferProgress.cs
│
## Hashing/                           (Hash generation, can parallelize)
│   ├── IHashGenerator.cs
│   ├── IItemHasher.cs
│   ├── HashType.cs                    (Enum: SHA2_256, SHA3, MD5, BLAKE3, etc.)
│   ├── StreamHashGenerator.cs         (Compute multiple hashes in single pass)
│   ├── ItemHasher.cs
│   └── Crypto/                        (Algorithm implementations)
│       ├── SHA256Algorithm.cs
│       ├── SHA512Algorithm.cs
│       ├── MD5Algorithm.cs
│       ├── SHA3_256Keccak.cs
│       ├── SHA3_512Keccak.cs
│       ├── SHA3_256FIPS202.cs
│       ├── SHA3_512FIPS202.cs
│       ├── BLAKE3_256Algorithm.cs
│       └── BLAKE3_512Algorithm.cs
│
├── Metadata/                          (Metadata extraction)
│   ├── IMetadataReader.cs
│   ├── FileMetadata.cs
│   └── FileMetadataReader.cs
│
├── Sidecar/                           (Sidecar generation)
│   ├── ISidecarGenerator.cs
│   └── SimpleSidecarGenerator.cs
│
├── DependencyInjection/
│   └── ServiceCollectionExtensions.cs
│
└── BMTP3.Core3.csproj
```

**Why Flat:**
- ✓ <250 types total, easy to navigate
- ✓ Folders only when >3 related implementations
- ✓ No deep Api/Domain/Infrastructure hierarchy
- ✓ Clear ownership per folder
- ✓ Can find anything in 2-3 clicks

**No Need For:**
- ✗ Api/ folder (contracts live with implementations)
- ✗ Engine/ subfolder (BackupEngine is in root)
- ✗ Domain/Models/Enums (flat is clearer for small codebase)
- ✗ Infrastructure/Repositories (too much layering)

---

## Feature Scope

### ✅ INCLUDED (Core Backup Workflow)

| Feature | Implementation |
|---------|-----------------|
| **Scan** | Filesystem + MTP/PTP device traversal |
| **Transfer** | Copy files with progress reporting, conflict detection |
| **Metadata** | Extract timestamps, file properties, EXIF |
| **Hashing** | **ALL 9 hash types** (SHA2, SHA3, MD5, BLAKE3) computed in single file pass |
| **Timestamp** | Restore original modification times |
| **Sidecar** | Generate metadata + **all hash types** in sidecar file |
| **Progress** | Explicit phases (Scanning, Transferring, Hashing, etc.) |
| **Error Handling** | Fail-fast on transfer, tolerate non-critical failures |
| **Logging** | Microsoft.Extensions.Logging throughout |
| **Dry-Run** | Simulate without writing (useful for testing) |

### ❌ EXCLUDED (Out of Scope)

| Feature | Reason |
|---------|--------|
| **Incremental** | Future enhancement |
| **Cloud sources** | Future enhancement |
| **Advanced audit** | Keep simple for now |
| **Custom pipeline** | Fixed workflow fits all cases |
| **Parallelization** | Single-threaded by design (except internal hash) |

**Rationale**: Focus on core workflow that handles 99% of use cases. Future versions can add advanced features.

---

## Design Patterns & Principles Applied

### Patterns Used

1. **Immutable Data Flow Pattern**
   - Items transform through methods: `items = await Step1(items)` → `items = await Step2(items)`
   - No shared mutable state, clear input/output
   - Testable without complex setup

2. **Strategy Pattern** (for pluggable implementations)
   - IBackupScanner: FileSystemScanner, DeviceScanner
   - IFileTransfer: SimpleFileTransfer (can add others later)
   - IHashGenerator: SHA256Generator (can add others later)

3. **Dependency Injection**
   - All services injected into BackupEngine
   - Easy to swap for testing or different implementations

4. **Observer Pattern** (for progress)
   - IProgress<IBackupProgress> for caller-driven progress updates
   - Caller subscribes, engine reports updates

### SOLID Principles (Applied Pragmatically)

| Principle | Applied As | Where |
|-----------|-----------|-------|
| **S** (Single Resp.) | Each class has one job: BackupEngine orchestrates, SHA256Generator hashes, etc. | Everywhere |
| **O** (Open/Closed) | Open for extension: new scanners, new hash generators | Services can be added without changing BackupEngine |
| **L** (Liskov) | Interface contracts are clear and substitutable | IBackupScanner implementations work interchangeably |
| **I** (Interface Seg.) | Small interfaces: IFileTransfer has 1 method, IHashGenerator has 1 method | Services folder |
| **D** (Dependency Inv.) | BackupEngine depends on interfaces, not implementations | Constructor injection |

**Not Applied Dogmatically**: We skip abstractions (no IBackupStep) when they add no value for our 7-8 known steps.

### Clean Architecture Principles

- ✓ **Dependency Rule**: Core logic doesn't depend on infrastructure (IFileTransfer abstraction)
- ✓ **Testability**: No external dependencies in unit tests (test doubles)
- ✓ **Framework Independence**: Can swap logging provider, DI container, etc.
- ✓ **Independent**: Backup logic is independent of CLI framework (Consoles uses BackupEngine via interface)

---

## Testing Strategy (Comprehensive)

**Philosophy**: Professional test-driven development with edge cases, unit tests, integration tests, and test doubles.

---

### Level 1: Unit Tests (Isolated Components)

Each service tested independently with test doubles.

**Pattern**: Arrange → Act → Assert

#### A. Scanner Tests (FileSystemScanner, DeviceScanner)

**Normal Cases:**
```csharp
[Fact]
public async Task FileSystemScanner_WithEmptyDirectory_ReturnsEmptyList() {
    var dir = CreateEmptyDirectory();
    var scanner = new FileSystemScanner();
    
    var result = await scanner.ScanAsync(dir, CancellationToken.None);
    
    Assert.Empty(result);
}

[Fact]
public async Task FileSystemScanner_WithMultipleFiles_ReturnsAllItems() {
    var dir = CreateDirectoryWithFiles("file1.jpg", "file2.jpg", "file3.jpg");
    var scanner = new FileSystemScanner();
    
    var result = await scanner.ScanAsync(dir, CancellationToken.None);
    
    Assert.Equal(3, result.Count());
    Assert.Contains(result, item => item.Name == "file1.jpg");
}

[Fact]
public async Task FileSystemScanner_WithNestedFolders_ReturnsRecursiveItems() {
    var dir = CreateDirectoryWithStructure(
        "file1.jpg",
        "folder1/file2.jpg",
        "folder1/folder2/file3.jpg"
    );
    var scanner = new FileSystemScanner();
    
    var result = await scanner.ScanAsync(dir, CancellationToken.None);
    
    Assert.Equal(3, result.Count());
}
```

**Edge Cases:**
```csharp
[Fact]
public async Task FileSystemScanner_WithNonExistentPath_ThrowsFileNotFoundException() {
    var scanner = new FileSystemScanner();
    
    var ex = await Assert.ThrowsAsync<FileNotFoundException>(
        () => scanner.ScanAsync("/nonexistent/path", CancellationToken.None)
    );
    
    Assert.Contains("/nonexistent/path", ex.Message);
}

[Fact]
public async Task FileSystemScanner_WithSpecialCharactersInFileName_ReturnsItem() {
    var dir = CreateDirectoryWithFiles("file-#1_[test].jpg");
    var scanner = new FileSystemScanner();
    
    var result = await scanner.ScanAsync(dir, CancellationToken.None);
    
    Assert.Single(result);
    Assert.Equal("file-#1_[test].jpg", result.First().Name);
}

[Fact]
public async Task FileSystemScanner_WithSymlinks_HandlesCorrectly() {
    var dir = CreateDirectoryWithSymlink("file1.jpg", "link_to_file1.jpg");
    var scanner = new FileSystemScanner();
    
    var result = await scanner.ScanAsync(dir, CancellationToken.None);
    
    // Should return both or handle symlink according to config
    Assert.NotEmpty(result);
}

[Fact]
public async Task FileSystemScanner_WithVeryDeepNesting_ReturnsAllItems() {
    var dir = CreateDirectoryWithDeepNesting(depth: 50, filesPerLevel: 2);
    var scanner = new FileSystemScanner();
    
    var result = await scanner.ScanAsync(dir, CancellationToken.None);
    
    Assert.Equal(100, result.Count());  // 50 levels × 2 files
}

[Fact]
public async Task FileSystemScanner_WithCancellation_StopsEarly() {
    var dir = CreateDirectoryWithFiles(Enumerable.Range(0, 1000).Select(i => $"file{i}.jpg").ToArray());
    var scanner = new FileSystemScanner();
    var cts = new CancellationTokenSource();
    
    var task = Task.Run(async () => {
        await Task.Delay(10);  // Let it scan a bit
        cts.Cancel();
        return await scanner.ScanAsync(dir, cts.Token);
    });
    
    var ex = await Assert.ThrowsAsync<OperationCanceledException>(() => task);
}
```

#### B. Transfer Tests (File Copy)

**Normal Cases:**
```csharp
[Fact]
public async Task SimpleFileTransfer_WithSingleFile_CopiesSuccessfully() {
    var sourceFile = CreateTestFile("test.jpg", sizeInBytes: 1024);
    var destDir = CreateEmptyDirectory();
    var item = new BackupItem { Name = "test.jpg", SourcePath = sourceFile, SizeInBytes = 1024 };
    var transfer = new SimpleFileTransfer();
    
    await transfer.CopyAsync(item, destDir, CancellationToken.None);
    
    Assert.True(File.Exists(Path.Combine(destDir, "test.jpg")));
    Assert.Equal(1024, new FileInfo(Path.Combine(destDir, "test.jpg")).Length);
}

[Fact]
public async Task SimpleFileTransfer_WithProgress_ReportsBytes() {
    var sourceFile = CreateTestFile("test.jpg", sizeInBytes: 10_000_000);  // 10 MB
    var destDir = CreateEmptyDirectory();
    var item = new BackupItem { Name = "test.jpg", SourcePath = sourceFile, SizeInBytes = 10_000_000 };
    var transfer = new SimpleFileTransfer();
    var progressReports = new List<long>();
    var progress = new Progress<long>(bytes => progressReports.Add(bytes));
    
    await transfer.CopyAsync(item, destDir, progress, CancellationToken.None);
    
    Assert.NotEmpty(progressReports);
    Assert.Equal(10_000_000, progressReports.Last());  // Final report = total bytes
}
```

**Edge Cases:**
```csharp
[Fact]
public async Task SimpleFileTransfer_WithDestinationFull_ThrowsIOException() {
    var sourceFile = CreateTestFile("test.jpg", sizeInBytes: 1024);
    var destDir = CreateFullDirectory();  // No space
    var item = new BackupItem { Name = "test.jpg", SourcePath = sourceFile, SizeInBytes = 1024 };
    var transfer = new SimpleFileTransfer();
    
    var ex = await Assert.ThrowsAsync<IOException>(
        () => transfer.CopyAsync(item, destDir, CancellationToken.None)
    );
}

[Fact]
public async Task SimpleFileTransfer_WithReadOnlySource_ThrowsUnauthorizedAccessException() {
    var sourceFile = CreateTestFile("test.jpg", sizeInBytes: 1024);
    File.SetAttributes(sourceFile, FileAttributes.ReadOnly);
    var destDir = CreateEmptyDirectory();
    var item = new BackupItem { Name = "test.jpg", SourcePath = sourceFile, SizeInBytes = 1024 };
    var transfer = new SimpleFileTransfer();
    
    // Should either succeed (we can read read-only files) or handle gracefully
    // Depending on implementation
    var result = await transfer.CopyAsync(item, destDir, CancellationToken.None);
    // Verify based on actual behavior
}

[Fact]
public async Task SimpleFileTransfer_WithLargeFile_TransferCompletesSuccessfully() {
    var largeFile = CreateTestFile("large.bin", sizeInBytes: 1_000_000_000);  // 1 GB
    var destDir = CreateEmptyDirectory();
    var item = new BackupItem { Name = "large.bin", SourcePath = largeFile, SizeInBytes: 1_000_000_000 };
    var transfer = new SimpleFileTransfer();
    
    var stopwatch = Stopwatch.StartNew();
    await transfer.CopyAsync(item, destDir, CancellationToken.None);
    stopwatch.Stop();
    
    Assert.True(File.Exists(Path.Combine(destDir, "large.bin")));
    // Verify reasonable performance (not spending >30s for in-memory test)
    Assert.True(stopwatch.ElapsedMilliseconds < 30_000);
}

[Fact]
public async Task SimpleFileTransfer_WithCancellation_StopsTransfer() {
    var largeFile = CreateTestFile("large.bin", sizeInBytes: 100_000_000);  // 100 MB
    var destDir = CreateEmptyDirectory();
    var item = new BackupItem { Name = "large.bin", SourcePath = largeFile, SizeInBytes: 100_000_000 };
    var transfer = new SimpleFileTransfer();
    var cts = new CancellationTokenSource();
    
    var task = Task.Run(async () => {
        await Task.Delay(5);  // Let transfer start
        cts.Cancel();
        return await transfer.CopyAsync(item, destDir, cts.Token);
    });
    
    var ex = await Assert.ThrowsAsync<OperationCanceledException>(() => task);
}
```

#### C. Hash Generation Tests (All 9 Algorithms)

**Normal Cases - Each Algorithm:**
```csharp
[Fact]
public async Task StreamHashGenerator_ComputesSHA256Correctly() {
    var stream = CreateStreamWithContent("test content");
    var generator = new StreamHashGenerator(logger);
    
    var result = await generator.ComputeHashesAsync(
        stream,
        new[] { HashType.SHA2_256 },
        progress: null,
        CancellationToken.None
    );
    
    Assert.Single(result);
    Assert.Equal("9f86d081884c7d6d9ffa37d8ac3c1e8e3b8eab6b3d8e8c0c0d0e0f0e0f0f0f0f", result[HashType.SHA2_256]);
}

[Fact]
public async Task StreamHashGenerator_ComputesMD5Correctly() {
    var stream = CreateStreamWithContent("test");
    var generator = new StreamHashGenerator(logger);
    
    var result = await generator.ComputeHashesAsync(
        stream,
        new[] { HashType.MD5_128 },
        progress: null,
        CancellationToken.None
    );
    
    Assert.Equal("098f6bcd4621d373cade4e832627b4f6", result[HashType.MD5_128]);
}

[Fact]
public async Task StreamHashGenerator_ComputesBLAKE3Correctly() {
    var stream = CreateStreamWithContent("test");
    var generator = new StreamHashGenerator(logger);
    
    var result = await generator.ComputeHashesAsync(
        stream,
        new[] { HashType.BLAKE3_256 },
        progress: null,
        CancellationToken.None
    );
    
    Assert.NotNull(result[HashType.BLAKE3_256]);
    Assert.Equal(64, result[HashType.BLAKE3_256].Length);  // 256 bits = 64 hex chars
}
```

**Edge Cases - Hash Generation:**
```csharp
[Fact]
public async Task StreamHashGenerator_WithAllNineTypes_ComputesInSinglePass() {
    var stream = CreateStreamWithLargeContent(100_000_000);  // 100 MB
    var generator = new StreamHashGenerator(logger);
    var allTypes = new[] {
        HashType.SHA2_256, HashType.SHA2_512,
        HashType.SHA3_256_FIPS202, HashType.SHA3_512_FIPS202,
        HashType.SHA3_256_KECCAK, HashType.SHA3_512_KECCAK,
        HashType.MD5_128,
        HashType.BLAKE3_256, HashType.BLAKE3_512
    };
    
    var stopwatch = Stopwatch.StartNew();
    var result = await generator.ComputeHashesAsync(stream, allTypes, null, CancellationToken.None);
    stopwatch.Stop();
    
    Assert.Equal(9, result.Count);
    // Should be ~1 file read, not 9 × file read
    Assert.True(stopwatch.ElapsedMilliseconds < 5000);  // 100 MB in <5 sec
}

[Fact]
public async Task StreamHashGenerator_WithEmptyStream_ComputesZeroHash() {
    var stream = CreateEmptyStream();
    var generator = new StreamHashGenerator(logger);
    
    var result = await generator.ComputeHashesAsync(
        stream,
        new[] { HashType.SHA2_256 },
        progress: null,
        CancellationToken.None
    );
    
    // SHA256 of empty = e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
    Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", result[HashType.SHA2_256]);
}

[Fact]
public async Task StreamHashGenerator_WithNonSeekableStream_ComputesCorrectly() {
    var nonSeekableStream = new NonSeekableStreamWrapper(CreateStreamWithContent("test"));
    var generator = new StreamHashGenerator(logger);
    
    var result = await generator.ComputeHashesAsync(
        nonSeekableStream,
        new[] { HashType.SHA2_256 },
        progress: null,
        CancellationToken.None
    );
    
    Assert.NotNull(result[HashType.SHA2_256]);
}

[Fact]
public async Task StreamHashGenerator_WithCancellation_StopsComputation() {
    var largeStream = CreateStreamWithLargeContent(500_000_000);  // 500 MB
    var generator = new StreamHashGenerator(logger);
    var cts = new CancellationTokenSource();
    
    var task = Task.Run(async () => {
        await Task.Delay(10);
        cts.Cancel();
        return await generator.ComputeHashesAsync(
            largeStream,
            new[] { HashType.SHA2_256 },
            null,
            cts.Token
        );
    });
    
    var ex = await Assert.ThrowsAsync<OperationCanceledException>(() => task);
}
```

#### D. Metadata Extraction Tests

**Normal Cases:**
```csharp
[Fact]
public async Task FileMetadataReader_ExtractsTimestamps() {
    var file = CreateTestFile("test.jpg", modifiedAt: DateTime(2024, 5, 1, 10, 30, 0));
    var reader = new FileMetadataReader();
    
    var metadata = await reader.ReadAsync(file, CancellationToken.None);
    
    Assert.Equal(DateTime(2024, 5, 1, 10, 30, 0), metadata.ModifiedAt);
}

[Fact]
public async Task FileMetadataReader_WithImageFile_ExtractsEXIF() {
    var imageFile = CreateTestImageWithEXIF("photo.jpg", width: 1920, height: 1080);
    var reader = new FileMetadataReader();
    
    var metadata = await reader.ReadAsync(imageFile, CancellationToken.None);
    
    Assert.Equal(1920, metadata.ImageWidth);
    Assert.Equal(1080, metadata.ImageHeight);
}
```

**Edge Cases:**
```csharp
[Fact]
public async Task FileMetadataReader_WithCorruptedImageFile_HandlesGracefully() {
    var corruptedFile = CreateCorruptedImageFile("broken.jpg");
    var reader = new FileMetadataReader();
    
    var metadata = await reader.ReadAsync(corruptedFile, CancellationToken.None);
    
    // Should not throw, return partial metadata
    Assert.NotNull(metadata);
    Assert.Null(metadata.ImageWidth);  // Can't read EXIF
}

[Fact]
public async Task FileMetadataReader_WithNoReadPermission_ThrowsUnauthorizedAccessException() {
    var file = CreateTestFile("test.jpg");
    File.SetAttributes(file, FileAttributes.Hidden | FileAttributes.ReadOnly);
    var reader = new FileMetadataReader();
    
    var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
        () => reader.ReadAsync(file, CancellationToken.None)
    );
}
```

---

### Level 2: Integration Tests (Multiple Components)

Test 2-3 components together with real file system.

```csharp
[Fact]
public async Task BackupEngine_ScanAndTransfer_CreatesFilesWithCorrectContent() {
    // Arrange
    var sourceDir = CreateDirectoryWithFiles("file1.jpg", "file2.jpg");
    var destDir = CreateEmptyDirectory();
    var plan = new BackupPlan { Source = sourceDir, Destination = destDir, DryRun = false };
    
    var scanner = new FileSystemScanner();
    var transfer = new SimpleFileTransfer();
    var engine = new BackupEngine(scanner, transfer, /*...*/);
    
    // Act
    var result = await engine.RunAsync(plan, new Progress<IBackupProgress>(), CancellationToken.None);
    
    // Assert
    Assert.True(result.Success);
    Assert.Equal(2, result.SuccessfulItems);
    Assert.True(File.Exists(Path.Combine(destDir, "file1.jpg")));
    Assert.True(File.Exists(Path.Combine(destDir, "file2.jpg")));
}

[Fact]
public async Task BackupEngine_WithHashGeneration_ComputesAllNineHashTypes() {
    var sourceDir = CreateDirectoryWithFiles("photo.jpg");
    var destDir = CreateEmptyDirectory();
    var plan = new BackupPlan {
        Source = sourceDir,
        Destination = destDir,
        HashTypes = new() {
            SHA2_256, SHA2_512, SHA3_256_FIPS202, SHA3_512_FIPS202,
            SHA3_256_KECCAK, SHA3_512_KECCAK, MD5_128, BLAKE3_256, BLAKE3_512
        }
    };
    
    var engine = new BackupEngine(/*all services*/);
    
    var result = await engine.RunAsync(plan, new Progress<IBackupProgress>(), CancellationToken.None);
    
    Assert.True(result.Success);
    var item = result.Items.First();
    Assert.Equal(9, item.Hashes.Count);
    Assert.Contains(HashType.SHA2_256, item.Hashes.Keys);
    Assert.Contains(HashType.BLAKE3_512, item.Hashes.Keys);
}

[Fact]
public async Task BackupEngine_SidecarGeneration_IncludesAllHashTypes() {
    var sourceDir = CreateDirectoryWithFiles("photo.jpg");
    var destDir = CreateEmptyDirectory();
    var plan = new BackupPlan {
        Source = sourceDir,
        Destination = destDir,
        HashTypes = new() { SHA2_256, SHA3_256_FIPS202, BLAKE3_256 }
    };
    
    var engine = new BackupEngine(/*all services*/);
    var result = await engine.RunAsync(plan, new Progress<IBackupProgress>(), CancellationToken.None);
    
    var sidecarFile = Path.Combine(destDir, "photo.jpg.sidecar");
    Assert.True(File.Exists(sidecarFile));
    
    var sidecarContent = File.ReadAllText(sidecarFile);
    Assert.Contains("SHA2_256=", sidecarContent);
    Assert.Contains("SHA3_256_FIPS202=", sidecarContent);
    Assert.Contains("BLAKE3_256=", sidecarContent);
}
```

---

### Level 3: End-to-End Tests (Full Backup Workflow)

Complete backup from scan to sidecar generation.

```csharp
[Fact]
public async Task FullBackup_WithMixedFileTypes_CompletesSuccessfully() {
    var sourceDir = CreateDirectoryWithFiles(
        "photo1.jpg",
        "photo2.png",
        "document.pdf",
        "video.mp4",
        "archive.zip"
    );
    var destDir = CreateEmptyDirectory();
    var plan = new BackupPlan {
        Source = sourceDir,
        Destination = destDir,
        HashTypes = new() { SHA2_256, BLAKE3_256 }
    };
    
    var engine = new BackupEngine(/*all services*/);
    var progressReports = new List<IBackupProgress>();
    var progress = new Progress<IBackupProgress>(p => progressReports.Add(p));
    
    var result = await engine.RunAsync(plan, progress, CancellationToken.None);
    
    Assert.True(result.Success);
    Assert.Equal(5, result.SuccessfulItems);
    Assert.NotEmpty(progressReports);
    Assert.Contains(progressReports, p => p.Phase == BackupPhase.Scanning);
    Assert.Contains(progressReports, p => p.Phase == BackupPhase.Transferring);
    Assert.Contains(progressReports, p => p.Phase == BackupPhase.GeneratingHashes);
    
    // Verify all files + sidecars exist
    Assert.Equal(10, Directory.GetFiles(destDir).Length);  // 5 files + 5 sidecars
}

[Fact]
public async Task FullBackup_DryRun_DoesNotCreateFiles() {
    var sourceDir = CreateDirectoryWithFiles("file1.jpg", "file2.jpg");
    var destDir = CreateEmptyDirectory();
    var plan = new BackupPlan {
        Source = sourceDir,
        Destination = destDir,
        DryRun = true
    };
    
    var engine = new BackupEngine(/*all services*/);
    var result = await engine.RunAsync(plan, new Progress<IBackupProgress>(), CancellationToken.None);
    
    Assert.True(result.Success);
    Assert.Equal(2, result.TotalItems);
    Assert.Empty(Directory.GetFiles(destDir));  // No files actually written
}

[Fact]
public async Task FullBackup_WithCollisionHandling_RenamesExistingFiles() {
    var sourceDir = CreateDirectoryWithFiles("photo.jpg");
    var destDir = CreateEmptyDirectory();
    var existingFile = Path.Combine(destDir, "photo.jpg");
    File.WriteAllBytes(existingFile, new byte[1024]);
    
    var plan = new BackupPlan {
        Source = sourceDir,
        Destination = destDir,
        Collision = CollisionStrategy.Rename
    };
    
    var engine = new BackupEngine(/*all services*/);
    var result = await engine.RunAsync(plan, new Progress<IBackupProgress>(), CancellationToken.None);
    
    Assert.True(result.Success);
    Assert.True(File.Exists(existingFile));
    Assert.True(File.Exists(Path.Combine(destDir, "photo_1.jpg")));  // Renamed
}
```

---

### Test Doubles (Fake Implementations)

Reusable across all tests (located in `BMTP3.Core3.Tests/Fakes/` folder):

```csharp
public class FakeBackupScanner : IBackupScanner { /* ... */ }
public class FakeFileTransfer : IFileTransfer { /* ... */ }
public class FakeItemHasher : IItemHasher { /* ... */ }
public class FakeMetadataReader : IMetadataReader { /* ... */ }
public class FakeSidecarGenerator : ISidecarGenerator { /* ... */ }
public class TestBackupItemBuilder { /* Builder for test items */ }
```

**Location**: `BMTP3.Core3.Tests/Fakes/`
**Namespace**: `BMTP3.Core3.Tests.Fakes`

These are simple, self-documenting implementations that replace real services in tests. Better than mocks because they:
- Show intent clearly (FakeX = deterministic test implementation)
- Are reusable across all test classes
- Can catch real integration bugs
- Easy to add behavior (e.g., `FakeFileTransfer.ShouldFail = true`)

---

### Test Coverage Goals

| Component | Unit Tests | Integration Tests | E2E Tests | Edge Cases |
|-----------|-----------|-----------------|-----------|-----------|
| **Scanner** | ✓ | ✓ | ✓ | ✓ (symlinks, deep nesting, permissions) |
| **Transfer** | ✓ | ✓ | ✓ | ✓ (cancellation, large files, full disk) |
| **Hash** | ✓ | ✓ | ✓ | ✓ (all 9 types, empty stream, cancellation) |
| **Metadata** | ✓ | ✓ | ✓ | ✓ (corrupted files, no permissions) |
| **Sidecar** | ✓ | ✓ | ✓ | ✓ (write failures, disk full) |
| **BackupEngine** | ✓ | ✓ | ✓ | ✓ (dry-run, collisions, mixed types) |



---

## Integration with Consoles

### Current State
- `BMTP3.Consoles` uses `IBackupEngine` from Core2
- `BackupConsoleCommand2` orchestrates the backup

### Core3 Integration
1. Register Core3 services in DI
2. BackupConsoleCommand2 uses Core3's `IBackupEngine`
3. Progress reporting flows through `IProgress<IBackupProgress>`
4. No CLI code changes needed (abstraction handles it)

```csharp
// In ConsolesProgram.cs DI setup
public void ConfigureServices(IServiceCollection services)
{
    // Remove Core2 registration
    // services.AddBMTP3Core2(...);
    
    // Add Core3
    services.AddBMTP3Core3(configuration);
}
```

---

## Implementation Roadmap (Simplified)

### Phase 1: Foundation & Contracts
- [ ] Create BMTP3.Core3 project (.NET 8)
- [ ] Set up DI registration
- [ ] Define BackupPlan, BackupItem, BackupJobResult (input/output)
- [ ] Define IBackupEngine, IBackupProgress contracts
- [ ] Define service interfaces: IBackupScanner, IFileTransfer, etc.

### Phase 2: Infrastructure Services
- [ ] Implement FileSystemScanner
- [ ] Implement SimpleFileTransfer
- [ ] **Implement HashType enum (9 types from Core2)**
- [ ] **Implement IHashGenerator - single-pass multi-hash computation**
- [ ] **Implement IItemHasher - wraps IHashGenerator for files**
- [ ] **Implement 9 hash algorithm classes (SHA2, SHA3, MD5, BLAKE3 variants)**
- [ ] Implement FileMetadataReader
- [ ] Implement SimpleSidecarGenerator (with all hash types)

### Phase 3: BackupEngine Orchestrator
- [ ] Implement BackupEngine class (~300 lines)
- [ ] Scan method
- [ ] TransferAndGenerateSidecars method (per-file operation)
- [ ] ExtractMetadata method
- [ ] GenerateHashes method
- [ ] CorrectTimestamps method
- [ ] Error handling & result building

### Phase 4: Testing Foundation
- [ ] Create test doubles: FakeFileSystemScanner, InMemoryFileStorage
- [ ] Unit tests: each method in BackupEngine
- [ ] Integration tests: end-to-end with real files
- [ ] Test dry-run mode

### Phase 5: Integration with Consoles
- [ ] Wire Core3 into BMTP3.Consoles DI
- [ ] Update BackupConsoleCommand2 to use Core3
- [ ] Test CLI end-to-end

### Phase 6: Refinement
- [ ] Performance profiling
- [ ] Bug fixes from real-world testing
- [ ] MTP device testing (DeviceScanner implementation)
- [ ] Documentation

**Estimated Effort**: 
- Phase 1-3: ~4-6 hours (core engine)
- Phase 4-5: ~3-4 hours (testing + integration)
- Phase 6: ~2-3 hours (refinement)

---

## Success Criteria

| Criterion | How We Verify |
|-----------|--------------|
| **Single-threaded** | No `Task.Run`, no channels, no `Parallel.*` in main flow. Can step through code sequentially. |
| **Immutable data flow** | Each step: `items = await Step(items, ...)`. Input never modified. |
| **Testable** | Unit tests with test doubles (no mocks), integration tests with real files. |
| **Maintainable** | <300 lines in BackupEngine, <100 lines per service. Clear method names. |
| **Debuggable** | Can set breakpoint in BackupEngine and trace through steps. |
| **Complete** | All Core2 features (including dry-run). |
| **Reliable** | No race conditions, no hidden concurrency bugs. Passes end-to-end test. |
| **Compatible** | Consoles can use via IBackupEngine. CLI functions correctly. |
| **Error Handling** | Fail-fast on critical errors, graceful degradation on non-critical. Errors reported clearly. |
| **Pragmatic** | Use SOLID/Clean Architecture where it helps, simplicity wins over dogma. |

---

## Key Differences: Core3 vs Core2

| Aspect | Core2 | Core3 |
|--------|-------|-------|
| **Threading** | Multithreaded (channels, bounded buffers, workers) | Single-threaded main flow |
| **Abstractions** | IBackupStep × 8 classes | Direct methods in BackupEngine |
| **Data Flow** | Mutable BackupSession through steps | Immutable List<BackupItem> transforms |
| **State Management** | Shared context mutates Items/Results/Errors | Input → method → output |
| **Testability** | Requires mocking context, channels | Real test doubles, no mocking |
| **Code Complexity** | High (pipelines, orchestration, state) | Low (sequential methods) |
| **Lines of Code** | ~2000 | ~1000 |
| **Debuggability** | Hard (parallel execution, channels) | Easy (step through code) |
| **Error Handling** | Collect all, no clear strategy | Fail-fast on critical, tolerate non-critical |
| **Dry-Run Support** | Separate implementation | Simple `plan.DryRun` flag |
| **Performance** | Faster (parallel) | Slower (but sufficient) |

**Trade-off**: Core3 sacrifices ~30-50% performance for 10x simpler, more maintainable code.
For typical backups (10-500 MB), this is acceptable. If performance becomes issue, optimize specific step.

---

## Notes & Conventions

### Naming
- Classes: `PascalCase` (BackupEngine, FileSystemScanner)
- Methods: `PascalCase` (RunAsync, TransferAndGenerateSidecars)
- Interfaces: `IPascalCase` (IBackupEngine, IFileTransfer)
- Properties: `PascalCase` (CurrentFile, TotalItems)
- Private fields: `_camelCase` (_logger, _fileTransfer)

### Error Handling Philosophy

**Critical Errors (Stop Immediately):**
- File transfer failed (data loss risk)
- Source cannot be read (bad input)
- Destination is read-only (can't write)

**Non-Critical Errors (Log & Continue):**
- Metadata extraction failed (file is backed up, metadata is bonus)
- Hash generation failed (file is backed up, hash is verification)
- Timestamp correction failed (file is backed up)
- Sidecar generation failed (file is backed up, sidecar is aid)

### Logging Levels
- **Error**: Critical failures (transfer failed, source inaccessible)
- **Warning**: Non-critical failures (metadata extraction failed, hash failed)
- **Information**: Progress milestones (scan completed, transfer started)
- **Debug**: Per-item activity (transferring file X, hashing file Y)

### File Organization in Code
- **Keep related code together**: Transfer logic in Transfer/, not spread across files
- **One public class per file** (interface + implementation OK)
- **Simple ⟶ Complex reading order** (interfaces first, then implementations)

### Progress Reporting
- Report phase transitions (Scanning → Transferring → Hashing)
- Report per-item progress (file 23 of 1000)
- Don't report too frequently (every 100ms or per-item is fine)
- Allow caller to ignore progress if they want

---

## Conclusion

**Core3 Design Philosophy**: Pragmatic simplicity over theoretical purity. Use SOLID where it adds value (interfaces for services), ignore it where it doesn't (no IBackupStep abstraction). Result: a backup engine that's easy to understand, test, maintain, and extend.

**Expected Outcome**: A robust, single-threaded backup engine in ~1000 lines of code that can handle 95% of backup scenarios without the complexity of Core2's multithreaded pipeline.

---

**Status**: Architecture Review Complete - Ready for Implementation
**Created**: May 2, 2026
**Last Updated**: May 2, 2026
**Version**: 2.0 (Revised after Critical Architecture Review)
