# Påvirkede Klasser ved SequentialItemPipeline Refactor

## Oversigt

Refaktoreringen er **MINIMAL** - vi ekstraheerer KUN SequentialItemPipeline.

**Påvirket**: 2 filer
**Nye klasser**: 1 klasse + 1 record
**DI ændringer**: Ingen (pipeline instantieres lokalt)

---

## Påvirkede Filer

### 1. **BackupEngineSequentiel.cs** ← REFACTOR ✅
**Lokation**: `BMTP3.Core2\BackupNew\Engine\BackupEngineSequentiel.cs`

**Hvad ændres**:
- Import `SequentialItemPipeline`
- Create pipeline in RunAsync
- Replace 30 lines of boilerplate with 1 call: `await pipeline.ProcessItemAsync(...)`
- Add helper method `CompileResult()` for result compilation

**Før/efter størrelse**:
- Før: 412 linjer
- Efter: ~200 linjer
- **Reduktion**: ~50% (uden funktionsmæssig ændring)

---

### 2. **SequentialItemPipeline.cs** ← OPRET (NEW) ✅
**Lokation**: `BMTP3.Core2\BackupNew\Engine\SequentialItemPipeline.cs`

Ny fil med:
```csharp
public record PipelineStep(
    string Name,
    FilePhase Phase,
    Func<IBackupItem, IProgress<ulong>, CancellationToken, Task> Execute
);

public class SequentialItemPipeline
{
    public async Task ProcessItemAsync(
        IBackupItem item,
        string sourcePath,
        string fileName,
        string relativePath,
        ulong length,
        CancellationToken ct)
    {
        // Process item through 7 steps
        // Update phase before each step
        // Complete item after all steps
    }
}
```

**Størrelse**: ~100 linjer

---

## Filer der IKKE ændres

| Fil | Hvorfor |
|-----|---------|
| **BackupEngine.cs** | Anden implementering - lad være |
| **ServiceCollectionExtensions.cs** | Ingen DI-registrering nødvendig (instantieres lokalt) |
| **IBackupEngine.cs** | Interface ændres ikke |
| **Test files** | Virker uden ændringer (lever bag interface) |
| **Command files** | Virker uden ændringer (lever bag interface) |
| **ApplicationServiceSetup.cs** | Virker uden ændringer |

---

## Opsummering: Påvirkede Filer

| Fil | Ændringer? | Type |
|-----|-----------|------|
| **BackupEngineSequentiel.cs** | ✅ JA | Refactor (linjer 228-256 erstattet) |
| **SequentialItemPipeline.cs** | ✅ OPRET | Ny klasse |
| Alle andre | ❌ NEJ | Ingen påvirkning |

---

## Implementerings orden

1. **Create** `SequentialItemPipeline.cs`
   - Define `PipelineStep` record
   - Implement `SequentialItemPipeline` class
   - Write unit tests

2. **Refactor** `BackupEngineSequentiel.cs`
   - Import `SequentialItemPipeline`
   - Create pipeline in `RunAsync`
   - Replace boilerplate with `await pipeline.ProcessItemAsync(...)`
   - Add `CompileResult()` helper method
   - Run tests

3. **Verify**
   - All 690 tests still pass
   - No behavior change
   - RunAsync is now readable

---

## Non-Breaking Change

✅ **Lives behind IBackupEngine interface**
- Commands don't change
- Tests don't change
- DI doesn't change
- No breaking changes

---

## Estimat

**Effort**: 1-2 timer
- 30 min: Create SequentialItemPipeline
- 30 min: Refactor BackupEngineSequentiel
- 30 min: Write/run tests
- 0 min: DI changes (none needed)
