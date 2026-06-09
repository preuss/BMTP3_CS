# Task: BackupIndexType.Json

> Implementer `IBackupIndexWriter` + `JsonBackupIndexWriter` så `BackupIndexType.Json` producerer en `backup_catalog.json` per session.

---

## Goal

Fjern T3 feature-gate for `BackupIndexType.Json` ved at implementere en JSON catalog writer der skriver en oversigt over alle backed up filer til en `{sessionId}.catalog.json` fil.

---

## Design Decisions (aftalt)

| Spørgsmål | Valg |
|-----------|------|
| **Placering** | `{destination}\.bmtp3\{sessionId}.catalog.json` |
| **Indhold** | Backup metadata + alle filer med hashes og timestamps |
| **Overskriv/akkumuler** | Per-session fil — aldrig overskrivning af historik |
| **Hashes** | Alle hashes fra `ItemMetadata.ComputedHashes` (MD5, SHA256, BLAKE3 mv.) |
| **Timestamps** | mediaTaken, created, modified, authored, accessed |

---

## Existing Code (allerede implementeret)

| Artifact | Sti |
|----------|-----|
| `BackupIndexType` enum (None, Json, Database) | `Api/Models/Enums/BackupIndexType.cs` |
| `BackupPlan.BackupIndexType` property | `Api/Models/BackupPlan.cs:131` |
| T3 gate (blocker Json) | `Engine/Validation/BackupPlanValidator.cs:79-80` |
| T4 gate (blocker Database) | `Engine/Validation/BackupPlanValidator.cs:86-87` |
| `BackupEngine` processing loop (step 7 → step 8) | `Engine/BackupEngine.cs:516-586` |
| `BackupItem` (internal, har SourcePath, RelativePath, FileName, Content) | `Models/BackupItem.cs` |
| `BackupRecord` (internal, har Item, DestinationPath, Status, Metadata) | `Models/BackupRecord.cs` |
| `ItemMetadata` (internal, har ComputedHashes, timestamps) | `Models/ItemMetadata.cs` |
| `BackupResult` (public, har Name, State, ItemResults) | `Api/Models/BackupResult.cs` |
| `BackupResultItem` (public, har Id, SourcePath, DestinationPath, Length, State) | `Api/Models/BackupResultItem.cs` |
| `BackupPlan` (public record) | `Api/Models/BackupPlan.cs` |
| `.bmtp3` directory oprettes i `BackupEngine.cs:126-131` | `Engine/BackupEngine.cs` |
| DI registration pattern (TryAddSingleton) | `DependencyInjection/ServiceCollectionExtensions.cs` |
| Json writer pattern (reference: `JsonSidecarWriter`) | `Engine/Sidecar/Writers/JsonSidecarWriter.cs` |
| Session state fil (reference: `{sessionId}.json`) | `State/BackupJsonSummaryStore.cs` |

---

## Files to Create

### 1. `BMTP3.Core4/Engine/Index/IBackupIndexWriter.cs`

```csharp
namespace BMTP3.Core4.Engine.Index;

internal interface IBackupIndexWriter
{
    Task WriteAsync(
        string destinationDirectory,
        IReadOnlyList<BackupRecord> records,
        BackupPlan plan,
        BackupResult result,
        CancellationToken cancellationToken);
}
```

- Internal interface (ligesom `ISidecarService`)
- Modtager `destinationDirectory` (plan.Destination) for at finde `.bmtp3` stien
- Modtager `records` (alle `BackupRecord` objekter med fuld data)
- Modtager `plan` + `result` for metadata

### 2. `BMTP3.Core4/Engine/Index/BackupIndexCatalog.cs`

Document model for JSON serialization:

```csharp
namespace BMTP3.Core4.Engine.Index;

// Internal — kun brugt af JsonBackupIndexWriter
internal sealed record BackupIndexCatalog
{
    public required string BackupName { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string SessionId { get; init; }
    public required string SourcePath { get; init; }
    public required string Destination { get; init; }
    public required int TotalFiles { get; init; }
    public required long TotalBytes { get; init; }
    public required int CompletedFiles { get; init; }
    public required string State { get; init; }
    public required IReadOnlyList<BackupIndexFileEntry> Files { get; init; }
}

internal sealed record BackupIndexFileEntry
{
    public required string Id { get; init; }
    public required string SourcePath { get; init; }
    public required string RelativePath { get; init; }
    public required string FileName { get; init; }
    public string? DestinationPath { get; init; }
    public required long Length { get; init; }
    public required string Status { get; init; }
    public Dictionary<string, string>? Hashes { get; init; }
    public BackupIndexTimestamps? Timestamps { get; init; }
}

internal sealed record BackupIndexTimestamps
{
    public DateTimeOffset? MediaTaken { get; init; }
    public DateTimeOffset? Created { get; init; }
    public DateTimeOffset? Modified { get; init; }
    public DateTimeOffset? Authored { get; init; }
    public DateTimeOffset? Accessed { get; init; }
}
```

### 3. `BMTP3.Core4/Engine/Index/JsonBackupIndexWriter.cs`

```csharp
namespace BMTP3.Core4.Engine.Index;

internal sealed class JsonBackupIndexWriter : IBackupIndexWriter
{
    private readonly ILogger<JsonBackupIndexWriter> _logger;

    public JsonBackupIndexWriter(ILogger<JsonBackupIndexWriter> logger)
    {
        _logger = logger;
    }

    public async Task WriteAsync(
        string destinationDirectory,
        IReadOnlyList<BackupRecord> records,
        BackupPlan plan,
        BackupResult result,
        CancellationToken cancellationToken)
    {
        // 1. Build catalog model from records + plan + result
        // 2. Serialize to JSON (System.Text.Json)
        // 3. Write to {destination}\.bmtp3\{sessionId}.catalog.json
        // 4. Use temp-file + atomic replace pattern (samme som BackupJsonSummaryStore)
    }
}
```

---

## Files to Modify

### 4. `BMTP3.Core4/Engine/BackupEngine.cs`

**Constructor:** Tilføj `IBackupIndexWriter` parameter

```csharp
private readonly IBackupIndexWriter _backupIndexWriter;

internal BackupEngine(
    ...
    IBackupIndexWriter backupIndexWriter,
    ...)
{
    ...
    _backupIndexWriter = backupIndexWriter;
}
```

**Processing loop — efter step 7 (progress) før step 8 (build result):**

Indsæt kaldet omkring linje 530-540, hvor `allRecords` er tilgængelig og `sessionKey` kendes:

```csharp
// Step 7.5 — Write backup catalog
if (plan.BackupIndexType == BackupIndexType.Json)
{
    string metadataPath = Path.Combine(plan.Destination, ".bmtp3");
    Directory.CreateDirectory(metadataPath);

    await _backupIndexWriter.WriteAsync(
        plan.Destination, allRecords, plan, result, cancellationToken);
}
```

**Vigtigt:** `result` bygges EFTER catalog write, så catalog skal have adgang til records + plan direkte, ikke result.

**Alternativ:** Byg et midlertidigt `BackupResult`-light eller kald catalog write EFTER result er bygget (mellem step 8 og 9). Det er mest logisk — så catalog kan inkludere endelig state.

### 5. `BMTP3.Core4/DependencyInjection/ServiceCollectionExtensions.cs`

Tilføj:

```csharp
using BMTP3.Core4.Engine.Index;

// Index writer
services.TryAddSingleton<IBackupIndexWriter, JsonBackupIndexWriter>();
```

Og tilføj `IBackupIndexWriter` parameter i `BackupEngine` factory lambdaen.

### 6. `BMTP3.Core4/Engine/Validation/BackupPlanValidator.cs`

Fjern linje 79-80:

```csharp
// FJERN:
// if(plan.BackupIndexType == BackupIndexType.Json)
//     throw new FeatureNotImplementedException(3, "Backup index: Json");
```

---

## Implementation Steps

1. Opret `Engine/Index/` mappe i Core4
2. Opret `IBackupIndexWriter.cs` interface
3. Opret `BackupIndexCatalog.cs` + `BackupIndexFileEntry.cs` + `BackupIndexTimestamps.cs` dokumentmodeller
4. Opret `JsonBackupIndexWriter.cs` implementation
   - Brug `System.Text.Json` med `WriteIndented = true`
   - Brug temp-file + `File.Move` atomic replace (ligesom `BackupJsonSummaryStore`)
   - Filnavn: `{sessionId}.catalog.json`
   - Log med `_logger.LogInformation`
5. Rediger `BackupEngine.cs`: tilføj constructor parameter + kald efter step 8
6. Rediger `ServiceCollectionExtensions.cs`: DI registration
7. Rediger `BackupPlanValidator.cs`: fjern T3 gate
8. Build → test

---

## JSON Eksempel (output)

```json
{
  "backupName": "My Backup",
  "createdAt": "2026-06-09T14:30:00+02:00",
  "sessionId": "abc-123-def-456",
  "sourcePath": "D:\\Photos",
  "destination": "E:\\Backup",
  "totalFiles": 847,
  "totalBytes": 4294967296,
  "completedFiles": 847,
  "state": "Completed",
  "files": [
    {
      "id": "rec-001",
      "sourcePath": "D:\\Photos\\IMG_0001.jpg",
      "relativePath": "IMG_0001.jpg",
      "fileName": "IMG_0001.jpg",
      "destinationPath": "E:\\Backup\\2026\\06\\IMG_0001.jpg",
      "length": 4194304,
      "status": "Succeeded",
      "hashes": {
        "MD5": "d41d8cd98f00b204e9800998ecf8427e",
        "SHA256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
        "BLAKE3": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
      },
      "timestamps": {
        "mediaTaken": "2026-06-01T12:00:00+02:00",
        "created": "2026-06-01T12:00:00+02:00",
        "modified": "2026-06-01T12:30:00+02:00",
        "authored": null,
        "accessed": "2026-06-01T14:00:00+02:00"
      }
    }
  ]
}
```

---

## Notes

- `BackupRecord` + `ItemMetadata` er `internal` — det er fint, for `IBackupIndexWriter` er også `internal`
- `BackupIndexType` enum har `None = 0` som default — `BackupPlan` default er `None`
- `HashType` enum har både `MD5`, `SHA256`, `BLAKE3` etc. — nøgler i hashes dictionary kan være `HashType.ToString()` eller custom mapping ligesom `SidecarService` gør

## Afhængigheder

- Ingen — self-contained task
- Låser op for CLI item 2.3 (`--backup-index default Json`)
