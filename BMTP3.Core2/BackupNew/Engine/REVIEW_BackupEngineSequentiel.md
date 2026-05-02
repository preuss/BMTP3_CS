# BackupEngineSequentiel Review & Fixes

## Critical Issues Found

### 1. **WRONG: Calling CompleteItem() after every step**
   - **Current code (lines 236-243 & 255-262 in BackupEngineSequentiel.cs)**:
     ```csharp
     await bufferingStep.ExecuteAsync(item, itemProgress, ct);
     await metadataStep.ExecuteAsync(item, itemProgress, ct);
     // ... after each step...
     tracker.CompleteItem(item.Id, item.ResultState, (long)size);
     ```
   
   - **Problem**: CompleteItem() should NOT be called after each step. It's a **final** operation that marks item as done across the entire pipeline.
   
   - **How BackupEngine does it (line 306-325)**:
     - Completionask reads from persistenceChannel (OUTPUT of last stage)
     - Only THEN calls CompleteItem() once per item
     - CompleteItem() records final state (Success/Failed/Skipped) in tracker
   
   - **Issue in BackupEngineSequentiel**: 
     - Calling CompleteItem() in the middle of pipeline (after buffering, metadata, etc.)
     - This COMPLETES the item prematurely
     - If later steps fail, item is already marked as complete
     - Progress tracking gets confused

### 2. **WRONG: Catching exceptions and still calling CompleteItem()**
   - **Current code (lines 250-264)**:
     ```csharp
     catch(Exception ex)
     {
         _logger.LogError("Error processing item {ItemId}: {Error}", item.Id, ex.Message);
         item.AddLog($"Error: {ex.Message}", "Pipeline");
         // ...
         tracker.CompleteItem(item.Id, item.ResultState, (long)size);
     }
     ```
   
   - **Problem**: Step exceptions should NOT bubble up and be caught here. That's wrong.
   - **How steps work (AbstractPipelineStage.cs line 125-137)**:
     - Steps catch exceptions themselves
     - Call `item.Fail()` to mark item as failed
     - Forward the failed item to next stage
     - Never throw to caller
   
   - **What happens in BackupEngineSequentiel**:
     - If step throws, we catch it
     - But step ALREADY called item.Fail()
     - We log it again (redundant)
     - We complete item

### 3. **MISSING: Phase tracking during item processing**
   - **Current code**: No UpdateItemPhase() calls
   - **How BackupEngine does it**: Pipeline stages call tracker internally via their step pattern
   - **What's missing**: We need to manually track which phase each item is in
   
   - **Solution**: Call tracker.UpdateItemPhase() for each step:
     ```csharp
     tracker.UpdateItemPhase(item.Id, sourcePath, fileName, relativePath, FilePhase.Staging, totalBytes);
     await bufferingStep.ExecuteAsync(item, itemProgress, ct);
     
     tracker.UpdateItemPhase(item.Id, sourcePath, fileName, relativePath, FilePhase.Extracting, totalBytes);
     await metadataStep.ExecuteAsync(item, itemProgress, ct);
     // etc...
     ```

### 4. **MISSING: Phase set to Transferring at right time**
   - **Current code**: Never sets phase to Transferring
   - **BackupEngine (line 308)**: `tracker.SetPhase(BackupPhase.Transferring);` in completionTask
   - **When it happens**: Only AFTER all pipeline stages complete
   - **BackupEngineSequentiel**: Should set it AFTER all items' steps but BEFORE processing completion

### 5. **WRONG: No per-item sourcePath/fileName tracking**
   - **Current code**: Doesn't track item metadata for progress display
   - **Should have**: sourcePath, fileName, relativePath from item.Metadata
   - **Purpose**: Progress reporter shows which file is being processed and phase
   
   - **Fix**: Extract these from item and pass to UpdateItemPhase()

### 6. **WRONG: Item length extraction**
   - **Current code (lines 204-208 & 236-244)**:
     ```csharp
     ulong length = 0;
     if(item.Metadata.Has(MetadataKey.Length))
     {
         length = item.Metadata.Get<ulong>(MetadataKey.Length);
     }
     // Later:
     if(item.Metadata.Has(MetadataKey.Length))
     {
         ulong size = item.Metadata.Get<ulong>(MetadataKey.Length);
         tracker.CompleteItem(item.Id, item.ResultState, (long)size);
     }
     ```
   
   - **Problem**: Extracting length twice and using 0 if missing is wrong
   - **BackupEngine (line 319)**: `ulong size = item.Metadata.Get<ulong>(MetadataKey.Length);` - assumes it exists
   - **Why**: By the time we get here (after MetadataExtractionStep), length MUST be set
   
   - **Fix**: Extract length ONCE at discovery, use it everywhere

### 7. **WRONG: MTP session double-dispose in catch AND finally**
   - **Current code (lines 306-317 catch + 323-334 finally)**:
     ```csharp
     catch(Exception ex)
     {
         // ...
         if(mtpSession != null)
         {
             try { mtpSession.Dispose(); }
             catch { }
         }
         return result;
     }
     finally
     {
         if(mtpSession != null)
         {
             try { mtpSession.Dispose(); }
             catch { }
         }
     }
     ```
   
   - **Problem**: In catch, we dispose AND return. Then finally still tries to dispose.
   - **BackupEngine (lines 381-391 + 407-416)**: Similar but catch does return BEFORE finally
     - So it's okay because return exits the method
     - But second dispose is harmless (Dispose() is idempotent)
   
   - **In BackupEngineSequentiel**: Actually, this is OK because catch returns before finally.

### 8. **WRONG: Step execution never fails - steps handle errors internally**
   - **Current code**: Wraps step calls in try-catch
   - **Problem**: Steps don't throw on item errors - they call item.Fail()
   - **Result**: This catch block would only catch infrastructure errors (OOM, etc.)
   - **Question**: Should we catch those? Probably not - let them bubble.
   
   - **Fix**: Remove the catch block. If infrastructure fails, job should fail.
     - Only catch OperationCanceledException

## Summary of Required Fixes

1. **Remove double CompleteItem() calls** - only call ONCE at end of all steps
2. **Remove exception handling for steps** - they handle errors internally
3. **Add UpdateItemPhase() calls** - for progress tracking per step
4. **Extract item metadata once** - sourcePath, fileName, relativePath, length
5. **Set phase to Transferring at right time** - after scanning, before persistence
6. **Use metadata length correctly** - MetadataExtractionStep guarantees it exists

## Correct Sequential Flow Should Be

```
FOR EACH ITEM from scanner:
    ├→ Extract metadata (sourcePath, fileName, relativePath, length)
    ├→ UpdateItemPhase(Staging)
    ├→ Buffer content
    ├→ UpdateItemPhase(Extracting)
    ├→ Extract metadata
    ├→ UpdateItemPhase(Correcting)
    ├→ Correct timestamps
    ├→ UpdateItemPhase(Hashing)
    ├→ Compute hashes
    ├→ UpdateItemPhase(Transferring)
    ├→ Transfer to output
    ├→ UpdateItemPhase(Inspecting)
    ├→ Inspect destination
    ├→ UpdateItemPhase(Sidecaring)
    ├→ Generate sidecar
    └→ CompleteItem() [ONCE, at the very end]
    └→ Persist item state
```

Note: UpdateItemPhase() is called by items as they move through pipeline stages in BackupEngine (because stages manage it). In sequential, we need to call it manually.

## Phase Definition

From FilePhase enum:
- Staging: Downloading/buffering content
- Extracting: Reading metadata
- Correcting: Correcting timestamps
- Hashing: Computing hashes
- Transferring: Copying to output
- Inspecting: Validating destination
- Sidecaring: Generating metadata

## How to Access Item Metadata

```csharp
string sourcePath = item.Metadata.Get<string>(MetadataKey.SourcePath);
string fileName = item.Metadata.Get<string>(MetadataKey.SourceFileName);
string relativePath = item.Metadata.Get<string>(MetadataKey.RelativePath);
ulong length = item.Metadata.Get<ulong>(MetadataKey.Length); // Set by MetadataExtractionStep
```
