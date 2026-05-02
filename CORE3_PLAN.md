# CORE3 - Minimalist Single-Threaded Backup Engine

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

1. **Single Responsibility** - Each step does ONE thing
2. **Dependency Inversion** - Depend on abstractions, not implementations
3. **Testability First** - Design for unit tests from the start
4. **Linear Execution** - No hidden async operations, no channels
5. **Fail Fast** - Exceptions propagate, no silent failures
6. **Minimal State** - Immutable data flows through steps

---

## Architecture Overview

### High-Level Flow

```
┌─────────────────────────────────────────────────────────────┐
│ BackupEngine (IBackupEngine)                                │
│ Orchestrates: Scan → Transfer → Hash → Sidecar             │
└────────────────────────────────┬────────────────────────────┘
                                 │
        ┌────────────────────────┼────────────────────────────┐
        │                        │                            │
        ▼                        ▼                            ▼
   ┌─────────┐           ┌─────────────┐            ┌──────────────┐
   │IBackup  │           │IBackupContext│           │ BackupSession│
   │Step     │           │              │           │              │
   │         │           │ • Plan       │           │• Items[]     │
   │Execute()│           │ • Session    │           │• Results[]   │
   │         │           │ • Progress   │           │• Errors[]    │
   └─────────┘           └─────────────┘            └──────────────┘
        △
        │ implements
        │
   ┌────────────────────────────────────────┐
   │ Concrete Steps:                        │
   │ • ScanStep                             │
   │ • TransferStep                         │
   │ • HashStep                             │
   │ • MetadataExtractionStep               │
   │ • TimestampCorrectionStep              │
   │ • SidecarGenerationStep                │
   │ • CollisionResolutionStep              │
   └────────────────────────────────────────┘
```

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

### Step-by-Step Execution

```
Input: BackupPlan
  └─ source: filesystem or MTP device
  └─ destination: output directory
  └─ options: collision strategy, etc.

                         │
                         ▼
         ┌───────────────────────────┐
         │ 1. SCAN STEP              │
         │ • Traverse source         │
         │ • Yield BackupItem[]      │
         │ • Detect folders/files    │
         └────────────┬──────────────┘
                      │ items: List<BackupItem>
                      ▼
         ┌───────────────────────────┐
         │ 2. COLLISION CHECK        │
         │ • Check destination       │
         │ • Rename if conflict      │
         │ • Update item paths       │
         └────────────┬──────────────┘
                      │ items: List<BackupItem> (paths resolved)
                      ▼
         ┌───────────────────────────┐
         │ 3. TRANSFER STEP          │
         │ • Copy file data          │
         │ • Write to destination    │
         │ • Report progress/errors  │
         └────────────┬──────────────┘
                      │ items: List<BackupItem> (transferred)
                      ▼
         ┌───────────────────────────┐
         │ 4. METADATA EXTRACTION    │
         │ • Read file properties    │
         │ • Get timestamps          │
         │ • Extract EXIF (images)   │
         └────────────┬──────────────┘
                      │ items: List<BackupItem> (with metadata)
                      ▼
         ┌───────────────────────────┐
         │ 5. HASH STEP              │
         │ • Compute SHA-256         │
         │ • Can parallelize         │
         │   (file hashing only)     │
         └────────────┬──────────────┘
                      │ items: List<BackupItem> (with hash)
                      ▼
         ┌───────────────────────────┐
         │ 6. TIMESTAMP CORRECTION   │
         │ • Restore original times  │
         │ • Set access/write times  │
         └────────────┬──────────────┘
                      │ items: List<BackupItem> (corrected)
                      ▼
         ┌───────────────────────────┐
         │ 7. SIDECAR GENERATION     │
         │ • Write metadata files    │
         │ • Hash files              │
         │ • Verification data       │
         └────────────┬──────────────┘
                      │ items: List<BackupItem> (sidecars created)
                      ▼
         ┌───────────────────────────┐
         │ 8. PERSIST RESULTS        │
         │ • Save backup record      │
         │ • Audit log               │
         │ • Session state           │
         └────────────┬──────────────┘
                      │
                      ▼
         Output: BackupJobResult
           └─ summary: files copied, bytes, time
           └─ errors: failed items
           └─ statistics: hash matches, metadata extracted
```

---

## Key Interfaces & Contracts

### IBackupStep

```csharp
public interface IBackupStep
{
    /// Friendly name for logging/debugging
    string StepName { get; }
    
    /// Main execution logic
    /// Reads from context.Session.Items
    /// Mutates items in-place or adds to context.Session.Results
    Task ExecuteAsync(IBackupContext context, CancellationToken ct);
    
    /// Optional: validate configuration before execution
    Task ValidateAsync(BackupPlan plan, CancellationToken ct);
}
```

**Key Points:**
- Simple: one method to implement
- Testable: inject mock context, verify mutations
- Synchronous progression: no channels, no buffering

### IBackupContext

```csharp
public interface IBackupContext
{
    /// Shared state across all steps
    BackupSession Session { get; }
    
    /// Report progress to caller
    IProgress<IBackupProgress> Progress { get; }
    
    /// Original input plan
    BackupPlan Plan { get; }
    
    /// Logging
    ILogger Logger { get; }
}
```

**Key Points:**
- Single source of truth: `Session` holds all state
- Progress: integrated progress reporting
- Logging: integrated logging

### BackupSession (State Holder)

```csharp
public class BackupSession
{
    public List<BackupItem> Items { get; set; }           // Files to backup
    public List<BackupResult> Results { get; set; }       // Completed items
    public List<BackupError> Errors { get; set; }         // Failed items
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public BackupJobResult? FinalResult { get; set; }
}
```

**Key Points:**
- Single mutable object passed through steps
- Steps read from Items, write to Results/Errors
- Clear state flow

### IBackupEngine

```csharp
public interface IBackupEngine
{
    /// Execute a backup plan
    /// Returns summary of what was backed up
    Task<BackupJobResult> RunAsync(
        BackupPlan plan, 
        IProgress<IBackupProgress> progress, 
        CancellationToken ct
    );
}
```

**Key Points:**
- Simple contract: plan in, result out
- Async but single-threaded
- Familiar from Core2

---

## Step Implementation Template

### Example: ScanStep

```csharp
public class ScanStep : IBackupStep
{
    private readonly IBackupScanner _scanner;
    private readonly ILogger<ScanStep> _logger;
    
    public string StepName => "Scan";
    
    public ScanStep(IBackupScanner scanner, ILogger<ScanStep> logger)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task ValidateAsync(BackupPlan plan, CancellationToken ct)
    {
        // Validate source exists, is readable
        if (plan.Source == null)
            throw new ArgumentException("Source is required");
    }
    
    public async Task ExecuteAsync(IBackupContext context, CancellationToken ct)
    {
        _logger.LogInformation($"{StepName}: Starting scan of {context.Plan.Source}");
        
        // Scan source
        var items = await _scanner.ScanAsync(context.Plan.Source, ct);
        
        // Store in session
        context.Session.Items = items.ToList();
        
        _logger.LogInformation($"{StepName}: Found {context.Session.Items.Count} items");
    }
}
```

**Pattern:**
1. Depend on interfaces (IScanStep)
2. Log at key points
3. Read from context
4. Mutate session state
5. Report progress

---

## Directory Structure

```
BMTP3.Core3/
│
├── Api/
│   ├── IBackupEngine.cs               # Main contract
│   ├── IBackupStep.cs                 # Step contract
│   ├── IBackupContext.cs              # Shared context
│   ├── IBackupProgress.cs             # Progress contract
│   ├── Request/
│   │   ├── BackupPlan.cs              # Input configuration
│   │   ├── BackupSource.cs
│   │   └── Enums/
│   ├── Response/
│   │   ├── BackupJobResult.cs         # Output summary
│   │   ├── BackupResult.cs            # Per-item result
│   │   └── BackupError.cs
│   └── Progress/
│       ├── BackupProgress.cs
│       └── IBackupProgress.cs
│
├── Domain/
│   ├── BackupItem.cs                  # File/folder to backup
│   ├── BackupJob.cs                   # Job configuration
│   ├── BackupSession.cs               # Session state
│   ├── Models/
│   │   ├── FileMetadata.cs
│   │   ├── FileHash.cs
│   │   └── TransferResult.cs
│   └── Enums/
│       ├── BackupItemType.cs
│       └── CollisionStrategy.cs
│
├── Engine/
│   ├── BackupEngine.cs                # Main orchestrator (IBackupEngine)
│   ├── BackupContext.cs               # IBackupContext implementation
│   │
│   └── Steps/
│       ├── IBackupStep.cs             # Step interface
│       ├── ScanStep.cs                # Traverse source
│       ├── CollisionResolutionStep.cs # Handle naming conflicts
│       ├── TransferStep.cs            # Copy files
│       ├── MetadataExtractionStep.cs  # Extract metadata
│       ├── HashStep.cs                # Compute hashes
│       ├── TimestampCorrectionStep.cs # Restore timestamps
│       └── SidecarGenerationStep.cs   # Write metadata files
│
├── Infrastructure/
│   ├── Scanners/
│   │   ├── IBackupScanner.cs
│   │   ├── FileSystemScanner.cs       # Local filesystem
│   │   └── DeviceScanner.cs           # MTP/PTP devices
│   ├── Transfer/
│   │   ├── IFileTransfer.cs
│   │   └── SimpleFileTransfer.cs
│   ├── Hashing/
│   │   ├── IHashGenerator.cs
│   │   └── SHA256HashGenerator.cs
│   ├── Metadata/
│   │   ├── IMetadataReader.cs
│   │   ├── FileMetadataReader.cs
│   │   └── MetadataExtractor.cs
│   ├── Sidecar/
│   │   ├── ISidecarGenerator.cs
│   │   └── SimpleSidecarGenerator.cs
│   └── Repositories/
│       ├── IBackupRepository.cs
│       └── FileBackupRepository.cs
│
├── DependencyInjection/
│   └── ServiceCollectionExtensions.cs # DI registration
│
└── BMTP3.Core3.csproj
```

---

## Feature Scope

### ✅ INCLUDED (Core Backup Workflow)

| Feature | Notes |
|---------|-------|
| **Scan** | Filesystem + MTP/PTP device traversal |
| **Transfer** | Copy files with progress reporting |
| **Metadata** | Extract timestamps, file properties |
| **Hashing** | SHA-256 or similar (internal parallelization allowed) |
| **Timestamp** | Restore original modification times |
| **Collision** | Detect naming conflicts, rename strategy |
| **Sidecar** | Generate metadata + hash verification files |
| **Progress** | Report files, bytes, current item |
| **Error Handling** | Collect errors, don't stop on single failure |
| **Logging** | Microsoft.Extensions.Logging |

### ❌ EXCLUDED (Non-Core or Future)

| Feature | Reason |
|---------|--------|
| **Dry-run** | Non-core, complicates logic |
| **Incremental** | Future enhancement |
| **Cloud sources** | Future enhancement |
| **Advanced audit** | Keep simple for now |
| **Custom strategies** | Use sensible defaults |
| **Parallelization** | Single-threaded by design |

---

## Design Patterns Used

### 1. **Strategy Pattern**
- Different scanners: FileSystemScanner, DeviceScanner
- Different collision strategies: Rename, Skip, Error

### 2. **Dependency Injection**
- All services injected via DI container
- Easy to swap implementations for testing

### 3. **Pipeline (Sequential)**
- Steps executed one after another
- No state mutation across unrelated steps

### 4. **Factory Pattern**
- MetadataReader, SidecarGenerator factories
- Polymorphic creation based on file type

### 5. **Observer Pattern**
- IProgress<IBackupProgress> for progress reporting
- Caller subscribes to progress updates

---

## Testing Strategy

### Unit Tests: Step Isolation

```csharp
[Fact]
public async Task ScanStep_WithValidSource_PopulatesSessionItems()
{
    // Arrange
    var mockScanner = new Mock<IBackupScanner>();
    var mockContext = new Mock<IBackupContext>();
    var session = new BackupSession { Items = new() };
    mockContext.Setup(c => c.Session).Returns(session);
    mockContext.Setup(c => c.Plan).Returns(new BackupPlan { /* ... */ });
    
    var items = new[] { new BackupItem { /* ... */ } };
    mockScanner.Setup(s => s.ScanAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(items);
    
    var step = new ScanStep(mockScanner.Object, /* logger */);
    
    // Act
    await step.ExecuteAsync(mockContext.Object, CancellationToken.None);
    
    // Assert
    Assert.NotEmpty(session.Items);
    Assert.Equal(1, session.Items.Count);
}
```

### Integration Tests: End-to-End

```csharp
[Fact]
public async Task BackupEngine_WithSmallDataset_ProducesCorrectOutput()
{
    // Arrange
    var source = CreateTestDirectory(10);  // 10 test files
    var dest = Path.Combine(Path.GetTempPath(), "bmtp3-test-out");
    
    var plan = new BackupPlan { Source = source, Destination = dest };
    var engine = CreateBackupEngine();
    
    // Act
    var result = await engine.RunAsync(plan, new Progress<IBackupProgress>(), CancellationToken.None);
    
    // Assert
    Assert.True(result.Success);
    Assert.Equal(10, result.FilesBackedUp);
    Assert.Empty(result.Errors);
    Assert.True(VerifyDestinationIntegrity(dest));  // All hashes match
}
```

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

## Implementation Roadmap

### Phase 1: Foundation
- [x] Create project & set up DI
- [ ] Api contracts (IBackupEngine, IBackupStep, IBackupContext)
- [ ] Domain models (BackupItem, BackupSession, BackupJob)

### Phase 2: Infrastructure
- [ ] Scanners (FileSystemScanner, DeviceScanner)
- [ ] File operations (transfer, hashing, metadata)
- [ ] Repositories & persistence

### Phase 3: Steps
- [ ] ScanStep, CollisionResolutionStep
- [ ] TransferStep, MetadataExtractionStep
- [ ] HashStep, TimestampCorrectionStep
- [ ] SidecarGenerationStep

### Phase 4: Engine & Orchestration
- [ ] BackupEngine (orchestrator)
- [ ] BackupContext (state container)
- [ ] Error handling & recovery

### Phase 5: Testing & Integration
- [ ] Unit tests (all steps)
- [ ] Integration tests (end-to-end)
- [ ] Consoles integration
- [ ] Validation & bug fixes

---

## Success Criteria

| Criterion | Verification |
|-----------|--------------|
| **Single-threaded** | No `Task.Run`, no channels, no `Parallel.*` in main flow |
| **Testable** | Each step has unit tests, can run in isolation |
| **Maintainable** | <200 lines per step, clear responsibilities |
| **Debuggable** | Can step through code linearly, logs show progression |
| **Complete** | All Core2 features except dry-run |
| **Reliable** | Passes end-to-end test, no race conditions |
| **Compatible** | Works with Consoles, CLI functions |

---

## Key Differences from Core2

| Aspect | Core2 | Core3 |
|--------|-------|-------|
| **Threading** | Multithreaded (channels, workers) | Single-threaded |
| **Complexity** | High (bounded channels, orchestration) | Low (simple step sequence) |
| **Testability** | Difficult (async state, channels) | Easy (step isolation) |
| **Debuggability** | Hard (parallel execution) | Easy (linear flow) |
| **Performance** | Faster (parallel) | Slower (but sufficient) |
| **Feature Set** | Full (including dry-run) | Core only (no dry-run) |

---

## Notes

- **Error Handling**: Fail fast, collect errors in session, report at end
- **Progress**: Report per-file and aggregated metrics
- **Logging**: Use Microsoft.Extensions.Logging consistently
- **Validation**: Validate before starting, fail early
- **Immutability**: Where practical, prefer immutable values
- **Reusability**: Reuse Core2/Core infrastructure where applicable

---

**Status**: Ready for implementation
**Created**: May 2, 2026
**Last Updated**: May 2, 2026
