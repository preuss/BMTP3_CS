# Core4 Implementeringsguide — Kontrakt & Implementeringsrækkefølge

**Version:** 4.0  
**Principper:**  
- Du som koder kan selv skrive kodelogik. Denne guide giver dig kontrakter, typer og rækkefølge.  
- Hvert trin er mærket `[NEED]` (core virker ikke uden) eller `[NICE]` (tilvalg, kan udskydes).  
- Følg rækkefølgen. Hvert trin kompilerer og testes inden næste.

---

## Sektion 1: Status på eksisterende skeleton

### 1.1 Beholdes uændret (rør ikke)

| Fil | Namespace | Formål |
|-----|-----------|--------|
| `Api/IBackupEngine.cs` | `BMTP3.Core4.Api` | Public entry-point |
| `Api/IBackupProgress.cs` | `BMTP3.Core4.Api` | Progress snapshot |
| `Api/IFileProgress.cs` | `BMTP3.Core4.Api` | Per-fil progress |
| `Models/BackupPlan.cs` | `BMTP3.Core4.Models` | Job-konfiguration (input) |
| `Models/BackupResult.cs` | `BMTP3.Core4.Models` | Job-resultat (output) |
| `Models/BackupItem.cs` | `BMTP3.Core4.Models` | Intern fil-model |
| `Models/Enums/BackupPhase.cs` | `BMTP3.Core4.Models.Enums` | Starting/Scanning/Transferring/Completed/Cancelled/Failed |
| `Models/Enums/BackupErrorCode.cs` | `BMTP3.Core4.Models.Enums` | Fejlklassifikation |
| `Models/Enums/BackupItemStatus.cs` | `BMTP3.Core4.Models.Enums` | Pending/Succeeded/Skipped/Failed |
| `Models/Enums/BackupSourceType.cs` | `BMTP3.Core4.Models.Enums` | FileSystem/MediaDevice |
| `Models/Enums/OutputStructure.cs` | `BMTP3.Core4.Models.Enums` | Flat/PreserveHierarchy |
| `Models/Enums/CollisionStreategy.cs` | `BMTP3.Core4.Models.Enums` | Skip/Overwrite/Rename _(filnavn har typo, enum hedder CollisionStrategy)_ |
| `Engine/State/BackupSessionState.cs` | `BMTP3.Core4.Engine.State` | Intern session-tilstand |
| `Engine/State/BackupSessionStateKey.cs` | `BMTP3.Core4.Engine.State` | Session-nøgle |
| `Engine/State/BackupSessionStateKeyFactory.cs` | `BMTP3.Core4.Engine.State` | Opretter nøgle fra plan |
| `Engine/State/IBackupSessionStateStore.cs` | `BMTP3.Core4.Engine.State` | State-store interface |
| `Engine/State/InMemoryBackupSessionStateStore.cs` | `BMTP3.Core4.Engine.State` | In-memory implementation |
| `Engine/Validation/BackupPlanValidationException.cs` | `BMTP3.Core4.Engine.Validation` | Kaster ved ugyldig plan |
| `Engine/Validation/BackupPlanValidator.cs` | `BMTP3.Core4.Engine.Validation` | Statisk validator |
| `Scanner/IBackupScanner.cs` | `BMTP3.Core4.Scanner` | Scanner interface |
| `Helpers/Guard.cs` | `BMTP3.Core4.Helpers` | Null-guard hjælper |

### 1.2 Skal implementeres (eksisterer som stub)

| Fil | Problem | Handling |
|-----|---------|---------|
| `Engine/Sequential/SequentialBackupEngine.cs` | Scan virker, transfer/sidecar/result mangler | Færdiggøres i Trin 8 |
| `Engine/LimitedParallel/LimitedParallelBackupEngine.cs` | Tom klasse, implementerer ikke IBackupEngine | Implementeres i Trin 14 `[NICE]` |

### 1.3 Skal slettes (tomme stubs der ikke hører hjemme)

| Fil | Årsag |
|-----|-------|
| `Class1.cs` | Auto-generated placeholder |
| `Engine/BackupEngine.cs` | Duplikat af SequentialBackupEngine — ingen funktion |
| `Engine/Parallel/ParallelBackupEngine.cs` | Overlapper LimitedParallelBackupEngine — forvirrer arkitekturen |
| `Engine/State/BackupSessionStore.cs` | Tom klasse — duplikat af InMemoryBackupSessionStateStore |

**Slet disse filer inden du begynder at implementere.**

---

## Sektion 2: Komplet kontraktoversigt — alle interfaces

Alle interfaces er `internal` medmindre andet er angivet.

### 2.1 IBackupEngine (eksisterer)

```csharp
// namespace: BMTP3.Core4.Api  |  access: public
interface IBackupEngine
{
    Task<BackupResult> RunAsync(
        BackupPlan plan,
        IProgress<IBackupProgress>? progress,
        CancellationToken cancellationToken);
}
```

### 2.2 IBackupProgress (eksisterer)

```csharp
// namespace: BMTP3.Core4.Api  |  access: public
interface IBackupProgress
{
    BackupPhase CurrentPhase { get; }
    int DirectoriesScanned { get; }
    int FilesDiscovered { get; }
    long BytesTotal { get; }
    int FilesProcessed { get; }
    int FilesSucceeded { get; }
    int FilesSkipped { get; }
    int FilesFailed { get; }
    long BytesProcessed { get; }
    IReadOnlyList<IFileProgress> ActiveFiles { get; }
}
```

### 2.3 IFileProgress (eksisterer)

```csharp
// namespace: BMTP3.Core4.Api  |  access: public
interface IFileProgress
{
    string Path { get; }
    long? BytesTotal { get; }
    long BytesProcessed { get; }
}
```

### 2.4 IBackupScanner (eksisterer)

```csharp
// namespace: BMTP3.Core4.Scanner  |  access: internal
interface IBackupScanner
{
    IAsyncEnumerable<BackupItem> ScanAsync(
        BackupPlan plan,
        CancellationToken cancellationToken);
}
```

### 2.5 IBackupSessionStateStore (eksisterer)

```csharp
// namespace: BMTP3.Core4.Engine.State  |  access: internal
interface IBackupSessionStateStore
{
    Task<BackupSessionState> OpenAsync(
        BackupSessionStateKey key,
        CancellationToken cancellationToken);
}
```

### 2.6 IFileTransfer [NEED] — mangler

```csharp
// fil: Transfer/IFileTransfer.cs
// namespace: BMTP3.Core4.Transfer  |  access: internal
interface IFileTransfer
{
    Task<TransferResult> TransferAsync(
        BackupItem item,
        string destinationPath,
        IProgress<long>? bytesProgress,
        CancellationToken cancellationToken);
}
```

`bytesProgress.Report(bytesTransferred)` kaldes løbende under overførslen (per chunk).  
Returnerer altid `TransferResult` — kaster aldrig for normale fejl.

### 2.7 ISidecarGenerator [NEED] — mangler

```csharp
// fil: Sidecar/ISidecarGenerator.cs
// namespace: BMTP3.Core4.Sidecar  |  access: internal
interface ISidecarGenerator
{
    Task CreateAsync(
        BackupItem item,
        TransferResult transferResult,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        string sidecarPath,
        SidecarEnrichment enrichment,
        CancellationToken cancellationToken);
}
```

`CreateAsync` kaldes direkte efter succesfuld transfer.  
`UpdateAsync` bruges af optional features til at tilføje felter til eksisterende sidecar.

### 2.8 IItemHasher [NICE] — mangler

```csharp
// fil: Features/Hashing/IItemHasher.cs
// namespace: BMTP3.Core4.Features.Hashing  |  access: internal
interface IItemHasher
{
    Task<HashResult> ComputeAsync(
        string filePath,
        CancellationToken cancellationToken);
}
```

### 2.9 IMetadataReader [NICE] — mangler

```csharp
// fil: Features/Metadata/IMetadataReader.cs
// namespace: BMTP3.Core4.Features.Metadata  |  access: internal
interface IMetadataReader
{
    Task<ExtractedMetadata?> ReadAsync(
        string filePath,
        CancellationToken cancellationToken);
}
```

Returnerer `null` hvis ingen metadata kunne udtrækkes.

### 2.10 IIntegrityVerifier [NICE] — mangler

```csharp
// fil: Features/Verification/IIntegrityVerifier.cs
// namespace: BMTP3.Core4.Features.Verification  |  access: internal
interface IIntegrityVerifier
{
    Task<VerificationResult> VerifyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken);
}
```

### 2.11 ITimestampCorrector [NICE] — mangler

```csharp
// fil: Features/Timestamp/ITimestampCorrector.cs
// namespace: BMTP3.Core4.Features.Timestamp  |  access: internal
interface ITimestampCorrector
{
    Task<bool> CorrectAsync(
        string destinationPath,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);
}
```

Returnerer `true` ved succes, `false` ved fejl (non-fatal).

---

## Sektion 3: Komplet datatypeoversigt — alle result/data-klasser

### 3.1 TransferResult [NEED] — mangler

```csharp
// fil: Transfer/TransferResult.cs
// namespace: BMTP3.Core4.Transfer  |  access: internal  |  type: sealed record
internal sealed record TransferResult
{
    bool Succeeded { get; init; }
    string? DestinationPath { get; init; }    // Fuld sti til destinations-filen. null ved fejl.
    long BytesTransferred { get; init; }      // Antal bytes kopieret. 0 ved fejl.
    BackupErrorCode? ErrorCode { get; init; } // null ved succes.
    string? ErrorMessage { get; init; }       // Menneskelig fejlbesked. null ved succes.
    Exception? Exception { get; init; }       // Underliggende exception. null ved succes.
}
```

### 3.2 SidecarData [NEED] — mangler

```csharp
// fil: Sidecar/SidecarData.cs
// namespace: BMTP3.Core4.Sidecar  |  access: internal  |  type: sealed class
// Serialiseres som JSON. Felter skrives som snake_case via JsonNamingPolicy.SnakeCaseLower.
internal sealed class SidecarData
{
    string SchemaVersion { get; init; }       // "1.0"
    string SourcePath { get; init; }          // Fuld kilde-sti
    string RelativePath { get; init; }        // Relativ til kildens rod
    string DestinationPath { get; init; }     // Fuld destinations-sti
    string SourceType { get; init; }          // "FileSystem" eller "MediaDevice"
    long? SizeBytes { get; init; }            // null hvis ukendt
    DateTimeOffset TransferredAtUtc { get; init; }
    string TransferStatus { get; init; }      // "Succeeded" eller "Failed"
    string? ErrorMessage { get; init; }       // null ved succes
    string? SessionId { get; init; }          // Fra BackupSessionState.SessionId
}
```

### 3.3 SidecarEnrichment [NICE] — mangler

```csharp
// fil: Sidecar/SidecarEnrichment.cs
// namespace: BMTP3.Core4.Sidecar  |  access: internal  |  type: sealed class
// Tilføjes til eksisterende sidecar via UpdateAsync. Alle felter er nullable (tilvalg).
internal sealed class SidecarEnrichment
{
    string? HashSha256 { get; init; }
    DateTimeOffset? ExifDateOriginal { get; init; }
    DateTimeOffset? ExifCreateDate { get; init; }
    double? GpsLatitude { get; init; }
    double? GpsLongitude { get; init; }
    string? CameraMake { get; init; }
    string? CameraModel { get; init; }
    string? VerificationStatus { get; init; } // "Verified" eller "Mismatch"
    string? VerificationErrorMessage { get; init; }
}
```

### 3.4 HashResult [NICE] — mangler

```csharp
// fil: Features/Hashing/HashResult.cs
// namespace: BMTP3.Core4.Features.Hashing  |  access: internal  |  type: sealed record
internal sealed record HashResult
{
    bool Succeeded { get; init; }
    string? Sha256 { get; init; }             // Hex-streng. null ved fejl.
    string? ErrorMessage { get; init; }
}
```

### 3.5 ExtractedMetadata [NICE] — mangler

```csharp
// fil: Features/Metadata/ExtractedMetadata.cs
// namespace: BMTP3.Core4.Features.Metadata  |  access: internal  |  type: sealed class
internal sealed class ExtractedMetadata
{
    DateTimeOffset? DateTimeOriginal { get; init; }   // EXIF DateTimeOriginal (prioritet 1)
    DateTimeOffset? CreateDate { get; init; }          // EXIF/XMP CreateDate (prioritet 2)
    DateTimeOffset? QuickTimeCreated { get; init; }   // QuickTime creation (prioritet 3)
    string? Make { get; init; }                        // Kamera-producent
    string? Model { get; init; }                       // Kamera-model
    double? GpsLatitude { get; init; }
    double? GpsLongitude { get; init; }
    string? Source { get; init; }                      // "MetadataExtractor" eller "ExifTool"
}
```

`DateTimeOriginal` er den anbefalede dato. Brug hjælpemetode `GetBestDate()`:  
Prioritet: `DateTimeOriginal` → `CreateDate` → `QuickTimeCreated` → `null`.

### 3.6 VerificationResult [NICE] — mangler

```csharp
// fil: Features/Verification/VerificationResult.cs
// namespace: BMTP3.Core4.Features.Verification  |  access: internal  |  type: sealed record
internal sealed record VerificationResult
{
    bool Succeeded { get; init; }
    bool HashesMatch { get; init; }
    string? SourceHash { get; init; }
    string? DestinationHash { get; init; }
    string? ErrorMessage { get; init; }
}
```

### 3.7 BackupProgressSnapshot [NEED] — mangler

```csharp
// fil: Progress/BackupProgressSnapshot.cs
// namespace: BMTP3.Core4.Progress  |  access: internal
// Implementerer IBackupProgress. Snapshots er immutable.
internal sealed class BackupProgressSnapshot : IBackupProgress
{
    BackupPhase CurrentPhase { get; init; }
    int DirectoriesScanned { get; init; }
    int FilesDiscovered { get; init; }
    long BytesTotal { get; init; }
    int FilesProcessed { get; init; }
    int FilesSucceeded { get; init; }
    int FilesSkipped { get; init; }
    int FilesFailed { get; init; }
    long BytesProcessed { get; init; }
    IReadOnlyList<IFileProgress> ActiveFiles { get; init; }  // = []
}
```

### 3.8 FileProgressSnapshot [NEED] — mangler

```csharp
// fil: Progress/FileProgressSnapshot.cs
// namespace: BMTP3.Core4.Progress  |  access: internal
// Implementerer IFileProgress. Bruges i BackupProgressSnapshot.ActiveFiles.
internal sealed class FileProgressSnapshot : IFileProgress
{
    string Path { get; init; }
    long? BytesTotal { get; init; }
    long BytesProcessed { get; init; }
}
```

### 3.9 Core4Options [NEED] — mangler

```csharp
// fil: DependencyInjection/Core4Options.cs
// namespace: BMTP3.Core4.DependencyInjection  |  access: public
// Konfigureres af kalderen ved AddBMTP3Core4(...).
public sealed class Core4Options
{
    int DefaultParallelism { get; set; }      // default: 4
    int MtpRetryCount { get; set; }           // default: 3
    bool UseExifToolFallback { get; set; }    // default: true
    string? ExifToolPath { get; set; }        // null = autodetect fra bin/
}
```

---

## Sektion 4: Komplet liste over klasser der skal oprettes

### Tier 1 — NEED (core kørsel virker kun med disse)

| # | Fil | Klasse | Implementerer |
|---|-----|--------|--------------|
| 1 | `Transfer/IFileTransfer.cs` | `IFileTransfer` | — (interface) |
| 2 | `Transfer/TransferResult.cs` | `TransferResult` | — (record) |
| 3 | `Sidecar/ISidecarGenerator.cs` | `ISidecarGenerator` | — (interface) |
| 4 | `Sidecar/SidecarData.cs` | `SidecarData` | — (klasse) |
| 5 | `Sidecar/JsonSidecarGenerator.cs` | `JsonSidecarGenerator` | `ISidecarGenerator` |
| 6 | `Progress/BackupProgressSnapshot.cs` | `BackupProgressSnapshot` | `IBackupProgress` |
| 7 | `Progress/FileProgressSnapshot.cs` | `FileProgressSnapshot` | `IFileProgress` |
| 8 | `Progress/ProgressTracker.cs` | `ProgressTracker` | — (konkret klasse) |
| 9 | `Scanner/Filesystem/FilesystemItemScanner.cs` | `FilesystemItemScanner` | `IBackupScanner` |
| 10 | `Transfer/Filesystem/FilesystemFileTransfer.cs` | `FilesystemFileTransfer` | `IFileTransfer` |
| 11 | `Engine/BackupEngineFactory.cs` | `BackupEngineFactory` | — (konkret klasse) |
| 12 | `DependencyInjection/Core4Options.cs` | `Core4Options` | — (options klasse) |
| 13 | `DependencyInjection/ServiceCollectionExtensions.cs` | `ServiceCollectionExtensions` | — (static class) |

Derudover: **færdiggør** `Engine/Sequential/SequentialBackupEngine.cs` (scanner-del eksisterer, transfer/sidecar/result mangler).

### Tier 1.5 — NEED for MTP-support

| # | Fil | Klasse | Implementerer |
|---|-----|--------|--------------|
| 14 | `Scanner/MTP/MTPItemScanner.cs` | `MTPItemScanner` | `IBackupScanner` |
| 15 | `Transfer/MTP/MTPFileTransfer.cs` | `MTPFileTransfer` | `IFileTransfer` |

### Tier 2 — NICE (enrichment, kan udskydes til core er stabil)

| # | Fil | Klasse | Implementerer |
|---|-----|--------|--------------|
| 16 | `Sidecar/SidecarEnrichment.cs` | `SidecarEnrichment` | — (klasse) |
| 17 | `Features/Hashing/IItemHasher.cs` | `IItemHasher` | — (interface) |
| 18 | `Features/Hashing/HashResult.cs` | `HashResult` | — (record) |
| 19 | `Features/Hashing/FileHasher.cs` | `FileHasher` | `IItemHasher` |
| 20 | `Features/Metadata/IMetadataReader.cs` | `IMetadataReader` | — (interface) |
| 21 | `Features/Metadata/ExtractedMetadata.cs` | `ExtractedMetadata` | — (klasse) |
| 22 | `Features/Metadata/MetadataExtractorReader.cs` | `MetadataExtractorReader` | `IMetadataReader` |
| 23 | `Features/Metadata/ExifToolMetadataReader.cs` | `ExifToolMetadataReader` | `IMetadataReader` |
| 24 | `Features/Verification/IIntegrityVerifier.cs` | `IIntegrityVerifier` | — (interface) |
| 25 | `Features/Verification/VerificationResult.cs` | `VerificationResult` | — (record) |
| 26 | `Features/Verification/FileIntegrityVerifier.cs` | `FileIntegrityVerifier` | `IIntegrityVerifier` |
| 27 | `Features/Timestamp/ITimestampCorrector.cs` | `ITimestampCorrector` | — (interface) |
| 28 | `Features/Timestamp/FileTimestampCorrector.cs` | `FileTimestampCorrector` | `ITimestampCorrector` |

### Tier 3 — NICE (parallel FS-engine, kan udskydes)

| # | Fil | Klasse | Implementerer |
|---|-----|--------|--------------|
| 29 | `Engine/LimitedParallel/LimitedParallelBackupEngine.cs` | `LimitedParallelBackupEngine` | `IBackupEngine` |

---

## Sektion 5: Implementeringsrækkefølge — skridt for skridt

**Hvert trin har én opgave. Byg, kompilér, verificér inden næste.**

---

### Trin 0 — Slet tomme stubs `[NEED]`

Slet disse filer. De har meningsfulde navne men er enten tomme eller duplikater der forvirrer arkitekturen:

```
Engine/BackupEngine.cs              ← duplikat af SequentialBackupEngine, ingen funktion
Engine/Parallel/ParallelBackupEngine.cs  ← overlapper LimitedParallelBackupEngine
Engine/State/BackupSessionStore.cs  ← duplikat af InMemoryBackupSessionStateStore
```

---

### Trin 1 — Opret TransferResult `[NEED]`

**Fil:** `Transfer/TransferResult.cs`  
**Type:** `internal sealed record`  
**Felter:** Se Sektion 3.1.

Ingen afhængigheder bortset fra `BackupErrorCode` (eksisterer).

---

### Trin 2 — Opret IFileTransfer `[NEED]`

**Fil:** `Transfer/IFileTransfer.cs`  
**Type:** `internal interface`  
**Signatur:** Se Sektion 2.6.

Afhænger af: `BackupItem`, `TransferResult` (Trin 1).

---

### Trin 3 — Opret SidecarData `[NEED]`

**Fil:** `Sidecar/SidecarData.cs`  
**Type:** `internal sealed class`  
**Felter:** Se Sektion 3.2.

Ingen afhængigheder udenfor Models.

---

### Trin 4 — Opret ISidecarGenerator `[NEED]`

**Fil:** `Sidecar/ISidecarGenerator.cs`  
**Type:** `internal interface`  
**Signatur:** Se Sektion 2.7.

Afhænger af: `BackupItem`, `TransferResult` (Trin 1).  
Note: `SidecarEnrichment` er Tier 2 — `UpdateAsync` kan stub-implementeres for nu.

---

### Trin 5 — Opret FileProgressSnapshot `[NEED]`

**Fil:** `Progress/FileProgressSnapshot.cs`  
**Type:** `internal sealed class`  
**Implementerer:** `IFileProgress`  
**Felter:** Se Sektion 3.8.

---

### Trin 6 — Opret BackupProgressSnapshot `[NEED]`

**Fil:** `Progress/BackupProgressSnapshot.cs`  
**Type:** `internal sealed class`  
**Implementerer:** `IBackupProgress`  
**Felter:** Se Sektion 3.7.

`ActiveFiles` initialiseres til `[]` (tom liste).

---

### Trin 7 — Opret ProgressTracker `[NEED]`

**Fil:** `Progress/ProgressTracker.cs`  
**Type:** `internal sealed class`

Trådsikker tracker med følgende public API:

```csharp
// namespace: BMTP3.Core4.Progress
internal sealed class ProgressTracker
{
    void SetPhase(BackupPhase phase);

    void ItemDiscovered(BackupItem item);
    void DirectoryScanned();

    void StartFile(BackupItem item);
    void UpdateFileBytesTransferred(string itemId, long totalBytesTransferred);
    void CompleteFile(BackupItem item, TransferResult result);
    void SkipFile(BackupItem item);

    BackupProgressSnapshot GetSnapshot();
}
```

Brug `Interlocked.Increment/Add` for tæller-felter. Brug `ConcurrentDictionary<string, FileProgressSnapshot>` for active files.  
`GetSnapshot()` returnerer immutabelt billede af al tilstand på kaldetidspunktet.

---

### Trin 8 — Implementer FilesystemItemScanner `[NEED]`

**Fil:** `Scanner/Filesystem/FilesystemItemScanner.cs`  
**Type:** `internal sealed class`  
**Implementerer:** `IBackupScanner`

Krav til implementering:
- Brug `Directory.EnumerateFiles()` med `SearchOption` baseret på `plan.Recursive`.
- Beregn `RelativePath` via `Path.GetRelativePath(plan.Source, filePath)`.
- Anvend `plan.IncludePatterns` og `plan.ExcludePatterns` (glob-matching mod RelativePath).
- Hvert `BackupItem` får et `Id = Guid.NewGuid().ToString("N")`.
- Kald `await Task.Yield()` per item for at frigive event-loop.
- `cancellationToken.ThrowIfCancellationRequested()` per item.

---

### Trin 9 — Implementer FilesystemFileTransfer `[NEED]`

**Fil:** `Transfer/Filesystem/FilesystemFileTransfer.cs`  
**Type:** `internal sealed class`  
**Implementerer:** `IFileTransfer`

Krav:
- Opret destination-mappe via `Directory.CreateDirectory`.
- Kopi i chunks til `destinationPath + ".tmp"`.
- Kald `bytesProgress?.Report(totalBytesTransferred)` efter hvert chunk.
- Atomisk rename: `.tmp` → endelig sti (`File.Move(tmp, dest, overwrite: false)`).
- Ryd `.tmp` op ved `Exception` og ved `OperationCanceledException`.
- `OperationCanceledException` re-throws altid (aldrig fanget som fejl).
- Alle andre exceptions returneres som `TransferResult { Succeeded = false }`.

---

### Trin 10 — Implementer JsonSidecarGenerator `[NEED]`

**Fil:** `Sidecar/JsonSidecarGenerator.cs`  
**Type:** `internal sealed class`  
**Implementerer:** `ISidecarGenerator`

Krav til `CreateAsync`:
- Sidecar-sti: `item.DestinationPath + ".sidecar.json"`.
- Skriv til `sidecarPath + ".tmp"`, rename til final.
- JSON: indented, snake_case via `JsonNamingPolicy.SnakeCaseLower`.
- Fejl ved sidecar-skrivning: log og returner (aldrig kast til engine).

Krav til `UpdateAsync` (stub OK for Tier 1):
- Læs eksisterende JSON som `Dictionary<string, object?>`.
- Merge enrichment-felter ind.
- Skriv atomisk tilbage.

---

### Trin 11 — Færdiggør SequentialBackupEngine `[NEED]`

**Fil:** `Engine/Sequential/SequentialBackupEngine.cs`  
**Type:** `internal sealed class`  
**Implementerer:** `IBackupEngine`

Tilføj til konstruktør: `IFileTransfer`, `ISidecarGenerator`, `ProgressTracker`.

Komplet flow som engine skal udføre:

```
1.  BackupPlanValidator.Validate(plan)         → BackupPlanValidationException ved fejl
2.  BackupSessionStateKey key = Factory.Create(plan)
3.  BackupSessionState session = await store.OpenAsync(key, ct)
4.  session.SetPhase(Scanning)
5.  progressTracker.SetPhase(Scanning)
6.  await foreach item in scanner.ScanAsync(plan, ct):
        session.AddItem(item)
        progressTracker.ItemDiscovered(item)
        progress?.Report(progressTracker.GetSnapshot())
7.  session.SetPhase(Transferring)
8.  progressTracker.SetPhase(Transferring)
9.  For hvert item i session.GetPendingItems():
    a.  ct.ThrowIfCancellationRequested()
    b.  string dest = ResolveDestination(item, plan)
    c.  item.DestinationPath = dest
    d.  if (plan.SkipExisting && File.Exists(dest)):
            item.Status = Skipped
            progressTracker.SkipFile(item)
            continue
    e.  if (plan.DryRun):
            item.Status = Succeeded
            progressTracker.CompleteFile(item, fakeSuccessResult)
            continue
    f.  progressTracker.StartFile(item)
    g.  progress?.Report(progressTracker.GetSnapshot())
    h.  IProgress<long> byteProgress = new Progress<long>(bytes =>
            progressTracker.UpdateFileBytesTransferred(item.Id, bytes))
    i.  TransferResult result = await fileTransfer.TransferAsync(item, dest, byteProgress, ct)
    j.  progressTracker.CompleteFile(item, result)
    k.  if (result.Succeeded):
            item.Status = Succeeded
            await sidecarGenerator.CreateAsync(item, result, ct)
        else:
            item.Status = Failed
            Log fejlen
            if (plan.StopOnError):
                session.Fail(result.ErrorCode ?? BackupErrorCode.TransferFailed)
                break
    l.  progress?.Report(progressTracker.GetSnapshot())
10. session.Complete()  (hvis ikke allerede Failed/Cancelled)
11. return BuildResult(session, progressTracker)

Catch OperationCanceledException:
    session.Cancel()
    return BuildResult(session, progressTracker)

Catch BackupPlanValidationException:
    session.Fail(BackupErrorCode.InvalidConfiguration)
    return BuildResult(session, progressTracker)

Catch Exception (fatale systemiske fejl):
    session.Fail(BackupErrorCode.TransferFailed)
    return BuildResult(session, progressTracker)
```

**Destination-beregning (ResolveDestination) — privat static metode:**

```csharp
private static string ResolveDestination(BackupItem item, BackupPlan plan)
{
    // Beregn fil-relativ sti baseret på OutputStructure
    string relativePart = plan.OutputStructure switch
    {
        OutputStructure.PreserveHierarchy => item.RelativePath,
        OutputStructure.Flat              => Path.GetFileName(item.RelativePath),
        _ => throw new ArgumentOutOfRangeException()
    };

    string destination = Path.Combine(plan.Destination, relativePart);

    // CollisionStrategy.Rename: tilføj _1, _2 etc. hvis fil eksisterer
    if (plan.CollisionStrategy == CollisionStrategy.Rename)
    {
        // Loop med counter indtil unik destination
    }

    return destination;
}
```

**BuildResult — privat static metode:**

```csharp
private static BackupResult BuildResult(BackupSessionState session, ProgressTracker tracker)
{
    BackupProgressSnapshot snapshot = tracker.GetSnapshot();
    return new BackupResult
    {
        Name            = session.SourceIdentity,
        FinalPhase      = session.Phase,
        FailureReason   = session.FailureReason,
        DirectoriesScanned = snapshot.DirectoriesScanned,
        FilesDiscovered = snapshot.FilesDiscovered,
        BytesTotal      = snapshot.BytesTotal,
        FilesProcessed  = snapshot.FilesProcessed,
        FilesSucceeded  = snapshot.FilesSucceeded,
        FilesSkipped    = snapshot.FilesSkipped,
        FilesFailed     = snapshot.FilesFailed,
        BytesProcessed  = snapshot.BytesProcessed,
    };
}
```

---

### Trin 12 — Implementer BackupEngineFactory `[NEED]`

**Fil:** `Engine/BackupEngineFactory.cs`  
**Namespace:** `BMTP3.Core4.Engine`  
**Type:** `internal sealed class`

```csharp
internal sealed class BackupEngineFactory
{
    // Konstruktør modtager begge engines via DI
    BackupEngineFactory(
        SequentialBackupEngine sequentialEngine,
        LimitedParallelBackupEngine parallelEngine);

    // Vælger korrekt engine for given plan
    IBackupEngine Create(BackupPlan plan);
}
```

Valg-logik i `Create(plan)`:
- `plan.SourceType == MediaDevice` → returner sequential (ALTID)
- `plan.MaxDegreeOfParallelism == 1` → returner sequential
- Ellers → returner parallel (Tier 3; midlertidigt: returner sequential)

For Tier 1 kan `Create` altid returnere `sequentialEngine`.

---

### Trin 13 — DependencyInjection `[NEED]`

**Filer:**
- `DependencyInjection/Core4Options.cs` — Se Sektion 3.9
- `DependencyInjection/ServiceCollectionExtensions.cs`

```csharp
// namespace: BMTP3.Core4.DependencyInjection  |  access: public static class
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBMTP3Core4(
        this IServiceCollection services,
        Action<Core4Options>? configure = null);
}
```

Skal registrere:
1. `InMemoryBackupSessionStateStore` som `IBackupSessionStateStore` (Singleton)
2. `ProgressTracker` (Transient)
3. `FilesystemItemScanner` som `IBackupScanner` (Transient)
4. `FilesystemFileTransfer` som `IFileTransfer` (Transient)
5. `JsonSidecarGenerator` som `ISidecarGenerator` (Transient)
6. `SequentialBackupEngine` (Transient)
7. `LimitedParallelBackupEngine` (Transient — stub for nu)
8. `BackupEngineFactory` (Transient)
9. `IBackupEngine` → factory-baseret resolution

---

### Trin 14 — MTPItemScanner og MTPFileTransfer `[NEED for MTP]`

**Filer:**
- `Scanner/MTP/MTPItemScanner.cs` implementerer `IBackupScanner`
- `Transfer/MTP/MTPFileTransfer.cs` implementerer `IFileTransfer`

Begge klasser modtager MediaDevices-interface via konstruktør (DI).

MTP-specifikke krav:
- Scan: stack-baseret traversal (ingen rekursion), håndter `COMException` per mappe.
- Transfer: download til temp-fil, atomisk rename. Retry 3× med 1s/2s/4s backoff.
- MTP-session åbnes af den kaldende klasse/service — scanners og transfers åbner ikke selv sessionen.

---

### Trin 15–28 — Optional features `[NICE]`

Implementer i denne rækkefølge (hver er uafhængig):

1. `SidecarEnrichment` (Trin 16)
2. Hashing: `IItemHasher` + `HashResult` + `FileHasher` (SHA-256 via `System.Security.Cryptography.SHA256`)
3. Metadata: `IMetadataReader` + `ExtractedMetadata` + `MetadataExtractorReader` (bruger MetadataExtractor NuGet) + `ExifToolMetadataReader` (fallback via exiftool.exe -json)
4. Verification: `IIntegrityVerifier` + `VerificationResult` + `FileIntegrityVerifier` (sammenligner hash af source og destination)
5. Timestamp: `ITimestampCorrector` + `FileTimestampCorrector` (sætter `File.SetLastWriteTimeUtc`)

Timestamp-præcedens for `MetadataExtractorReader`:
1. EXIF `DateTimeOriginal`
2. EXIF `CreateDate` / XMP `CreateDate`
3. QuickTime creation date
4. `null` (fallback til filsystem sker i engine, ikke i reader)

Alle optional features:
- Aktiveres kun hvis tilsvarende flag i `BackupPlan` er `true`.
- Fejler de: log, sidecar opdateres med fejlstatus, backup fortsætter.
- Kører *efter* `CreateAsync` — sidecar eksisterer allerede.

---

### Trin 29 — LimitedParallelBackupEngine `[NICE]`

**Fil:** `Engine/LimitedParallel/LimitedParallelBackupEngine.cs`  
**Implementerer:** `IBackupEngine`

Bruges kun til `BackupSourceType.FileSystem`.  
Bruger `SemaphoreSlim(dop, dop)` til at begrænse concurrent transfers.  
Samme fejlpolitik og sidecar-kontrakt som `SequentialBackupEngine`.

---

## Sektion 6: Fejlpolitik — fuld tabel

| Fejltype | Sted | Default adfærd | StopOnError=true |
|----------|------|----------------|-----------------|
| Ugyldig BackupPlan | Validator | Kast `BackupPlanValidationException` | Samme |
| Source-sti eksisterer ikke | Scanner start | `Fail(SourceNotFound)`, returnér result | Samme |
| Per-fil adgang nægtet i scan | Scanner loop | Log + skip fil (item aldrig oprettet) | Samme |
| Per-fil transfer IOException | FilesystemFileTransfer | `TransferResult { Succeeded=false }` → engine logger, fortsætter | Engine stopper |
| Disk fuld (IOException+diskspace) | FilesystemFileTransfer | `ErrorCode=InsufficientDiskSpace` → `Fail(...)`, stop job | Samme |
| MTP transient fejl (timeout) | MTPFileTransfer | Retry 3× med backoff | Retry, derefter stop |
| MTP permanent disconnect | MTPFileTransfer | `Fail(MediaDeviceDisconnected)`, stop job | Samme |
| Sidecar skrivning fejler | JsonSidecarGenerator | Log fejl, backup **fortsætter** | Fortsætter |
| Optional feature fejler | Feature-klasse | Log fejl, backup **fortsætter** | Fortsætter |
| `OperationCanceledException` | Engine catch | `session.Cancel()`, returnér partial result | Samme |

---

## Sektion 7: Tre arkitekturregler der aldrig brydes

1. **MTP er altid sekventielt.** Ingen parallel adgang mod MTP-enheder.
2. **Sidecar skrives atomisk direkte efter succesfuld transfer.** Aldrig samlet til sidst.
3. **Optional features vælter aldrig core-backup.** De er non-fatal per definition.

---

## Sektion 8: Dokumenthierarki

| Dokument | Brug |
|----------|------|
| `docs/CORE4_MASTER_SYNTHESIS.md` | Endelig beslutningstekst — konflikt? Følg dette. |
| `docs/CORE4_IMPLEMENTATION_GUIDE_DA.md` | **Denne guide** — kontrakter og implementeringsrækkefølge |
| `docs/Core4_Master_Architecture.md` | Dyb teknisk reference til edge cases |

---

_Version 4.0 — baseret på komplet analyse af BMTP3.Core4 skeleton pr. maj 2026._
