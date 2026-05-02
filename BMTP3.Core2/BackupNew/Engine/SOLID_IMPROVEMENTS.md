# BackupEngineSequentiel - SOLID Refactor (Minimalist)

## The Problem: Boilerplate DRY Violation

Current BackupEngineSequentiel (lines 228-256) has **7x repeated** pattern:

```csharp
// Same 7 times with different step:
tracker.UpdateItemPhase(item.Id, sourcePath, fileName, relativePath, FilePhase.X, length);
await someStep.ExecuteAsync(item, itemProgress, ct);
```

**Result**: 30 lines of pure boilerplate that obscures intent.

---

## The Solution: Extract SequentialItemPipeline

### Single Responsibility Principle
- **Before**: RunAsync does scanning + item processing + persistence + result compilation
- **After**: RunAsync does orchestration; SequentialItemPipeline does item processing

### Dry Principle
Eliminate repetition:

```csharp
// AFTER: One call instead of 30 lines
await pipeline.ProcessItemAsync(item, sourcePath, fileName, relativePath, length, ct);
```

---

## Implementation

### Step 1: Create PipelineStep Record

```csharp
public record PipelineStep(
    string Name,
    FilePhase Phase,
    Func<IBackupItem, IProgress<ulong>, CancellationToken, Task> Execute
);
```

### Step 2: Create SequentialItemPipeline Class

**Location**: `BMTP3.Core2\BackupNew\Engine\SequentialItemPipeline.cs`

```csharp
public class SequentialItemPipeline
{
    private readonly ProgressTracker _tracker;
    private readonly List<PipelineStep> _steps;
    private readonly IProgress<ulong> _itemProgress;

    public SequentialItemPipeline(
        ProgressTracker tracker,
        IEnumerable<PipelineStep> steps,
        IProgress<ulong> itemProgress)
    {
        _tracker = tracker;
        _steps = steps.ToList();
        _itemProgress = itemProgress;
    }

    /// <summary>
    ///     Processes a single item through the entire pipeline sequentially.
    ///     - Updates tracking before each step (for UI progress)
    ///     - Executes step (which handles errors internally)
    ///     - Completes item after all steps succeed
    /// </summary>
    public async Task ProcessItemAsync(
        IBackupItem item,
        string sourcePath,
        string fileName,
        string relativePath,
        ulong length,
        CancellationToken ct)
    {
        foreach (PipelineStep step in _steps)
        {
            ct.ThrowIfCancellationRequested();

            // Update tracking BEFORE executing step
            // This tells UI which phase/file is currently being processed
            _tracker.UpdateItemPhase(item.Id, sourcePath, fileName, relativePath, step.Phase, length);

            // Execute step (steps handle their own errors via item.Fail())
            await step.Execute(item, _itemProgress, ct).ConfigureAwait(false);

            // If step failed, item.ResultState is already set to Failed
            // Continue to next step (which might also fail)
        }

        // All steps complete: mark item as done globally
        _tracker.CompleteItem(item.Id, item.ResultState, (long)length);
    }
}
```

### Step 3: Refactor BackupEngineSequentiel.RunAsync()

**Before** (412 lines):
```csharp
public async Task<BackupJobResult> RunAsync(BackupPlan plan, ...)
{
    // ... 50 lines validation & setup ...

    // ... 200+ lines: scanner loop with 7x step execution boilerplate ...
    foreach (item in items)
    {
        tracker.UpdateItemPhase(..., FilePhase.Staging, ...);
        await bufferingStep.ExecuteAsync(...);
        tracker.UpdateItemPhase(..., FilePhase.Metadata, ...);
        await metadataStep.ExecuteAsync(...);
        // ... 5 more times
    }

    // ... 100 lines: result compilation ...
}
```

**After** (~200 lines, much clearer):
```csharp
public async Task<BackupJobResult> RunAsync(BackupPlan plan, ...)
{
    // Validation & setup
    _validator.ValidateJobAsync(plan, ct);
    ValidateDiskSpace(plan);
    ValidateSourceAndOutput(plan);

    // Session & tracking
    Guid sessionId = Guid.NewGuid();
    var session = new BackupSessionEntity { SessionId = sessionId, ... };
    var tracker = new ProgressTracker();

    if (!plan.DryRun)
    {
        await _repository.SaveAsync(session, ct);
    }

    // Create pipeline ONCE (not per item)
    var steps = new[]
    {
        new PipelineStep("Buffering", FilePhase.Staging,
            (item, prog, ct) => bufferingStep.ExecuteAsync(item, prog, ct)),
        new PipelineStep("Metadata", FilePhase.Metadata,
            (item, prog, ct) => metadataStep.ExecuteAsync(item, prog, ct)),
        new PipelineStep("Timestamp", FilePhase.Metadata,
            (item, prog, ct) => timestampStep.ExecuteAsync(item, prog, ct)),
        new PipelineStep("Hashing", FilePhase.Hashing,
            (item, prog, ct) => hashStep.ExecuteAsync(item, prog, ct)),
        new PipelineStep("Transfer", FilePhase.Planning,
            (item, prog, ct) => transferStep.ExecuteAsync(item, prog, ct)),
        new PipelineStep("Inspector", FilePhase.Transferring,
            (item, prog, ct) => inspectorStep.ExecuteAsync(item, prog, ct)),
        new PipelineStep("Sidecar", FilePhase.Transferring,
            (item, prog, ct) => sidecarStep.ExecuteAsync(item, prog, ct)),
    };

    var pipeline = new SequentialItemPipeline(tracker, steps, itemProgress);

    try
    {
        // Scan & process items
        int itemsSinceLastSave = 0;
        await foreach (IBackupItem item in _backupScanner.ScanAsync(plan, ct))
        {
            ct.ThrowIfCancellationRequested();

            // Extract metadata once (instead of repeating 7x)
            ulong length = item.Metadata.Get<ulong>(MetadataKey.Length);
            string sourcePath = item.Metadata.Get<string>(MetadataKey.SourceFullPath) ?? "";
            string fileName = item.Metadata.Get<string>(MetadataKey.SourceFileName) ?? "";
            string relativePath = item.Metadata.Get<string>(MetadataKey.SourceRelativePath) ?? "";

            tracker.AddDiscovery(false, (long)length);

            // ← Single line instead of 30+
            await pipeline.ProcessItemAsync(item, sourcePath, fileName, relativePath, length, ct);

            // Persist & batch save
            if (!plan.DryRun)
            {
                await _repository.PersistItemStateAsync(item, ct);
                itemsSinceLastSave++;
                if (itemsSinceLastSave >= 10)
                {
                    await _repository.SaveAsync(session, ct);
                    itemsSinceLastSave = 0;
                }
            }
        }

        // Final session save
        if (!plan.DryRun)
        {
            await _repository.SaveAsync(session, ct);
        }
    }
    finally
    {
        // Device cleanup if needed
        if (scanner is IMtpCapableScanner mtpScanner && plan.SourceType == SourceType.MediaDevice)
        {
            mtpScanner.Dispose();
        }
    }

    // Compile result (simple helper method, not a whole class)
    return CompileResult(tracker.GetSnapshot(), startTime, ct);
}

private BackupJobResult CompileResult(
    BackupProgress progress,
    DateTime startTime,
    CancellationToken ct)
{
    var result = new BackupJobResult { StartTime = startTime };
    result.EndTime = DateTime.UtcNow;

    // Status determination logic
    if (ct.IsCancellationRequested)
    {
        result.Status = JobState.Cancelled;
        tracker.SetPhase(BackupPhase.Cancelled);
    }
    else if (progress.FilesFailed > 0)
    {
        result.Status = JobState.Failed;
        tracker.SetPhase(BackupPhase.Completed);
        result.GlobalErrors.Add($"{progress.FilesFailed} file(s) failed during the run.");
    }
    else
    {
        result.Status = JobState.Completed;
        tracker.SetPhase(BackupPhase.Completed);
    }

    // Populate summary fields
    result.FilesCopied = progress.FilesSucceeded;
    result.FilesFailed = progress.FilesFailed;
    result.FilesSkipped = progress.FilesSkipped;
    result.TotalFilesScanned = progress.FilesDiscovered;
    result.TotalBytesCopied = progress.BytesProcessed;

    return result;
}
```

---

## Benefits

| Metric | Before | After |
|--------|--------|-------|
| **Lines in RunAsync** | 412 | ~200 |
| **Boilerplate repetition** | 7x same pattern | 0x |
| **Item processing logic** | Embedded & hard to test | Isolated & testable |
| **ReadabilityOf RunAsync** | ⚠️ Lost in details | ✅ Clear intent |
| **Step definition** | Scattered | Declarative array |

---

## SOLID Principles

| Principle | How it's satisfied |
|-----------|-------------------|
| **SRP** | SequentialItemPipeline = single responsibility (item processing) |
| **OCP** | Can add steps without modifying RunAsync |
| **LSP** | SequentialItemPipeline could swap with ParallelItemPipeline |
| **ISP** | PipelineStep is simple interface (just a Func) |
| **DIP** | RunAsync depends on abstraction (IBackupItem, tracker) |

---

## Testing

```csharp
[Test]
public async Task SequentialItemPipeline_UpdatesPhaseBeforeEachStep()
{
    var phasesUpdated = new List<FilePhase>();
    var mockTracker = new MockProgressTracker();
    mockTracker.OnUpdateItemPhase = phase => phasesUpdated.Add(phase);

    var steps = new[]
    {
        new PipelineStep("Step1", FilePhase.Staging, 
            async (item, prog, ct) => { }),
        new PipelineStep("Step2", FilePhase.Metadata, 
            async (item, prog, ct) => { }),
        new PipelineStep("Step3", FilePhase.Hashing, 
            async (item, prog, ct) => { }),
    };

    var pipeline = new SequentialItemPipeline(mockTracker, steps, new Progress<ulong>());
    var mockItem = new MockBackupItem { ResultState = ItemResultState.Succeeded };

    // Act
    await pipeline.ProcessItemAsync(mockItem, "path", "file", "rel", 100, CancellationToken.None);

    // Assert: phases in order
    Assert.Equal(new[] { FilePhase.Staging, FilePhase.Metadata, FilePhase.Hashing }, phasesUpdated);
    
    // Assert: item completed once
    Assert.Equal(1, mockTracker.CompleteItemCallCount);
}

[Test]
public async Task SequentialItemPipeline_ContinuesOnStepFailure()
{
    var stepsExecuted = new List<string>();
    
    var steps = new[]
    {
        new PipelineStep("Step1", FilePhase.Staging,
            async (item, prog, ct) => { stepsExecuted.Add("Step1"); }),
        new PipelineStep("Step2", FilePhase.Metadata,
            async (item, prog, ct) => { 
                stepsExecuted.Add("Step2");
                item.Fail("Simulated failure");
            }),
        new PipelineStep("Step3", FilePhase.Hashing,
            async (item, prog, ct) => { stepsExecuted.Add("Step3"); }),
    };

    var pipeline = new SequentialItemPipeline(tracker, steps, progress);

    // Act
    await pipeline.ProcessItemAsync(item, "path", "file", "rel", 100, ct);

    // Assert: all steps executed (steps don't early-exit)
    Assert.Equal(new[] { "Step1", "Step2", "Step3" }, stepsExecuted);
    
    // Assert: item marked failed after all steps
    Assert.Equal(ItemResultState.Failed, item.ResultState);
}
```

---

## Implementation Notes

1. **SequentialItemPipeline is local** (not DI-registered)
   - Instantiated inside RunAsync
   - Not reused across calls
   - Simpler than DI registration

2. **CompileResult is a helper method** (not a separate class)
   - 30 lines of straightforward logic
   - Not complex enough to warrant own class
   - Easier to understand inline

3. **No SessionManager**
   - Session persistence stays in RunAsync
   - Simpler without extra abstraction layer
   - Only 10 lines of logic anyway

4. **PipelineStep is a record** (not an interface)
   - Simple, lightweight, one-liner usage
   - No DI overhead
   - Easy to understand

---

## Summary: Minimal, Focused Refactor

- ✅ **Extract SequentialItemPipeline** - Eliminates boilerplate, improves testability
- ✅ **Helper method CompileResult()** - Keeps RunAsync clean without over-engineering
- ❌ **No SessionManager** - Over-engineering
- ❌ **No BackupResultCompiler** - Over-engineering
- ❌ **No DI complexity** - Pipeline instantiated locally

**Result**: Cleaner, more testable code without sacrificing simplicity.
