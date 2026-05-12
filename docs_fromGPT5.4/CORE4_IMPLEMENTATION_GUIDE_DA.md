# Core4 Implementeringsguide — Kontrakt & Implementeringsrækkefølge

**Version:** 4.1  
**Principper:**  
- Du som koder kan selv skrive kodelogik. Denne guide giver dig kontrakter, typer og rækkefølge.  
- Denne guide bruger både **trinnumre** og fire faste prioritetstags: `[NEED]`, `[SHOULD]`, `[NICE]`, `[LATER]`.  
- Trinnumrene fortæller rækkefølgen. Tags fortæller hvor kritisk noget er.  
- Følg rækkefølgen. Trin 0-13 bygger foundation, Trin 14 lukker den første komplette baseline, og senere trin kommer bagefter.
- Denne revision lukker de vigtigste konflikter mellem `CORE4_MASTER_SYNTHESIS.md`, `CORE4_REQUIREMENTS_TIERS.md`, `Core4_Master_Architecture.md` og det faktiske `BMTP3.Core4` skeleton.

---

## Sektion 0: Normalisering af konflikter mellem dokumenter

Før du skriver kode, skal du læse disse regler som **bindende normalisering**:

1. **Skeleton-kontrakterne er bindende.** Brug den nuværende form af `BackupPlan`, `BackupResult`, `BackupItem`, `IBackupEngine`, `IBackupProgress`, `IFileProgress` og `IBackupScanner`. Ældre docs viser andre DTO'er og feltnavne; de må ikke genintroduceres uden bevidst API-ændring.
2. **Den første komplette Core4-baseline er først færdig når Trin 14 (MTP) også virker.** I denne guide er Trin 0-13 et FS-first foundation-checkpoint. Det er praktisk som arbejdsrytme, men du må ikke kalde baseline færdig før MTP også er grønt.
3. **`MaxDegreeOfParallelism` følger skeleton, ikke de ældre docs.** I det aktuelle skeleton er `BackupPlan.MaxDegreeOfParallelism` `int?`, hvor `null = auto`, `1 = tving sekventiel`, og `> 1 = parallel når det senere parallel-trin findes`. Ignorér ældre beskrivelser med `-1`.
4. **Metadata/timestamp-strategien er fastlåst.** Core4 læser datoer i denne rækkefølge: `MetadataExtractor` → `ExifTool` fallback → filsystem-attributter. Filsystem-attributter udfyldes i metadataresultatet; engine må ikke have sin egen skjulte dato-fallback ved siden af readeren.
5. **Sidecar-navnet er i denne guide `item.DestinationPath + ".sidecar.json"`.** Hvis andre docs nævner `.bmtp3.json`, så er det en ældre navnekonvention. Hold dig til én konvention konsekvent.
6. **`BMTP3.Core4.csproj` er ikke klar som den står.** Skeleton targeter lige nu `net10.0`, mens resten af repoet er .NET 8-orienteret. Det er et preflight-fix, ikke et designvalg. Ret target framework tidligt, men bland det ikke sammen med nye API-ændringer.
7. **Ældre felter som `WriteSidecar`, `HashTypes`, `SourceDirectory`, `DeviceId` og `OperationTimeout` på `BackupPlan` er ikke en del af nuværende skeleton-contract.** Hvis du skal styre den slags i Core4 nu, så gør det via `Core4Options` eller interne policies, ikke ved at mutere `BackupPlan` tilfældigt.

---

## Sektion 0b: Prioritetsniveauer i denne guide

Denne guide bruger fire faste tags på contracts, datatyper, trintrin og krav:

| Tag | Betydning | Praktisk konsekvens |
|-----|-----------|---------------------|
| `[NEED]` | Nødvendigt for første komplette Core4-baseline | Blokerer at Core4 kan kaldes virkende |
| `[SHOULD]` | Meget vigtigt lige efter baseline | Skal på før Core4 kan kaldes robust og troværdig |
| `[NICE]` | Værdifuld udvidelse | Må vente til baseline og robusthed er på plads |
| `[LATER]` | Senere fase | Må ikke blokere den første implementering |

**Regel:** Følg altid trinnumrene i Sektion 5. Brug tags til at vurdere vigtighed.

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
| `Engine/Sequential/SequentialBackupEngine.cs` | Scan virker, transfer/sidecar/result mangler | Færdiggøres i Trin 11 `[NEED]` |
| `Engine/LimitedParallel/LimitedParallelBackupEngine.cs` | Tom klasse, implementerer ikke IBackupEngine | Implementeres i Trin 29 `[LATER]` |

### 1.3 Skal slettes (tomme stubs der ikke hører hjemme)

| Fil | Årsag |
|-----|-------|
| `Engine/BackupEngine.cs` | Duplikat af SequentialBackupEngine — ingen funktion |
| `Engine/Parallel/ParallelBackupEngine.cs` | Overlapper LimitedParallelBackupEngine — forvirrer arkitekturen |
| `Engine/State/BackupSessionStore.cs` | Tom klasse — duplikat af InMemoryBackupSessionStateStore |

**Slet disse filer inden du begynder at implementere.**

---

## Sektion 2: Komplet kontraktoversigt — alle interfaces for Tier 1–4

Alle interfaces er `internal` medmindre andet er angivet.  
Sektion 5 styrer rækkefølgen. Persistence/UI-interfaces (`IBackupRepository`, `IProgressNotifier`) er **planlagte**, men ikke frosset i denne sektion endnu, fordi de kræver nye public DTO-beslutninger. De står derfor som senere trin i dokumentet.

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

### 2.6 IFileTransfer [NEED] — opret i Trin 2

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

### 2.7 ISidecarGenerator [NEED] — opret i Trin 4

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

### 2.8 IItemHasher [NICE] — opret i de senere enrichment-trin

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

### 2.9 IMetadataReader [NICE] — opret i de senere enrichment-trin

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

### 2.10 IIntegrityVerifier [NICE] — opret i de senere enrichment-trin

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

### 2.11 ITimestampCorrector [NICE] — opret i de senere enrichment-trin

```csharp
// fil: Features/Timestamp/ITimestampCorrector.cs
// namespace: BMTP3.Core4.Features.Timestamp  |  access: internal
interface ITimestampCorrector
{
    Task<TimestampCorrectionResult> CorrectAsync(
        string destinationPath,
        ExtractedMetadata metadata,
        CancellationToken cancellationToken);
}
```

Korrektoren kører kun når metadata allerede er læst.  
Korrektoren vælger selv bedste gyldige timestamp fra `ExtractedMetadata` og returnerer detaljeret resultat.

---

## Sektion 3: Komplet datatypeoversigt — alle result/data-klasser

### 3.1 TransferResult [NEED] — opret i Trin 1

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

### 3.2 SidecarData [NEED] — opret i Trin 3

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

### 3.3 SidecarEnrichment [NICE] — opret i de senere enrichment-trin

```csharp
// fil: Sidecar/SidecarEnrichment.cs
// namespace: BMTP3.Core4.Sidecar  |  access: internal  |  type: sealed class
// Tilføjes til eksisterende sidecar via UpdateAsync. Alle felter er nullable (tilvalg).
internal sealed class SidecarEnrichment
{
    string? SourceHashSha256 { get; init; }
    string? DestinationHashSha256 { get; init; }
    DateTimeOffset? ExifDateOriginalUtc { get; init; }
    DateTimeOffset? ExifCreateDateUtc { get; init; }
    DateTimeOffset? QuickTimeCreatedUtc { get; init; }
    DateTimeOffset? FileSystemLastWriteUtc { get; init; }
    DateTimeOffset? FileSystemCreationUtc { get; init; }
    DateTimeOffset? SelectedTimestampUtc { get; init; }
    string? SelectedTimestampSource { get; init; }   // Fx "EXIF:DateTimeOriginal"
    string? MetadataSource { get; init; }            // "MetadataExtractor", "ExifTool", "FileSystem"
    double? GpsLatitude { get; init; }
    double? GpsLongitude { get; init; }
    string? CameraMake { get; init; }
    string? CameraModel { get; init; }
    string? VerificationStatus { get; init; } // "Verified" eller "Mismatch"
    string? VerificationErrorMessage { get; init; }
    string? TimestampCorrectionStatus { get; init; } // "Applied", "Skipped", "Failed"
    string? TimestampCorrectionErrorMessage { get; init; }
}
```

### 3.4 HashResult [NICE] — opret i de senere enrichment-trin

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

### 3.5 ExtractedMetadata [NICE] — opret i de senere enrichment-trin

```csharp
// fil: Features/Metadata/ExtractedMetadata.cs
// namespace: BMTP3.Core4.Features.Metadata  |  access: internal  |  type: sealed class
internal sealed class ExtractedMetadata
{
    DateTimeOffset? DateTimeOriginal { get; init; }   // EXIF DateTimeOriginal (prioritet 1)
    DateTimeOffset? CreateDate { get; init; }          // EXIF/XMP CreateDate (prioritet 2)
    DateTimeOffset? QuickTimeCreated { get; init; }   // QuickTime creation (prioritet 3)
    DateTimeOffset? FileSystemLastWriteUtc { get; init; }   // Filsystem fallback (prioritet 4)
    DateTimeOffset? FileSystemCreationUtc { get; init; }    // Filsystem fallback (prioritet 5)
    string? Make { get; init; }                        // Kamera-producent
    string? Model { get; init; }                       // Kamera-model
    double? GpsLatitude { get; init; }
    double? GpsLongitude { get; init; }
    string? Source { get; init; }                      // "MetadataExtractor", "ExifTool" eller "FileSystem"
}
```

`ReadAsync()` skal altid udfylde filsystem-attributter først (`FileInfo`), derefter forsøge managed metadata, derefter ExifTool hvis aktiveret og nødvendigt.  
Brug hjælpemetode `GetBestDate()`: `DateTimeOriginal` → `CreateDate` → `QuickTimeCreated` → `FileSystemLastWriteUtc` → `FileSystemCreationUtc` → `null`.  
Hvis en kandidat er tom, epoch-agtig, `DateTimeOffset.MinValue`, `DateTimeOffset.MaxValue` eller uden for rimeligt område, skal den ignoreres.

### 3.6 VerificationResult [NICE] — opret i de senere enrichment-trin

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

### 3.7 BackupProgressSnapshot [NEED] — opret i Trin 6

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

### 3.8 FileProgressSnapshot [NEED] — opret i Trin 5

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

### 3.9 Core4Options [NEED] — opret i Trin 13

```csharp
// fil: DependencyInjection/Core4Options.cs
// namespace: BMTP3.Core4.DependencyInjection  |  access: public
// Konfigureres af kalderen ved AddBMTP3Core4(...).
public sealed class Core4Options
{
    int DefaultParallelism { get; set; }      // default: 4
    int ParallelQueueDepth { get; set; }      // default: 50 (Tier 4)
    int MtpRetryCount { get; set; }           // default: 3
    TimeSpan MtpOperationTimeout { get; set; } // default: 00:01:00
    TimeSpan MtpKeepAliveInterval { get; set; } // default: 00:00:30
    int TransferBufferSizeBytes { get; set; } // default: 1_048_576 (1 MB)
    bool UseExifToolFallback { get; set; }    // default: true
    string? ExifToolPath { get; set; }        // null = autodetect fra bin/
}
```

---

### 3.10 TimestampCorrectionResult [NICE] — opret i de senere enrichment-trin

```csharp
// fil: Features/Timestamp/TimestampCorrectionResult.cs
// namespace: BMTP3.Core4.Features.Timestamp  |  access: internal  |  type: sealed record
internal sealed record TimestampCorrectionResult
{
    bool Succeeded { get; init; }
    DateTimeOffset? OriginalTimestamp { get; init; }   // Det der stod på filen før correction
    DateTimeOffset? AppliedTimestamp { get; init; }    // Den timestamp der blev forsøgt/applied
    string? Source { get; init; }                      // Fx "EXIF:DateTimeOriginal" eller "FileSystemLastWriteUtc"
    string? ErrorMessage { get; init; }
}
```

---

## Sektion 4: Komplet liste over klasser der skal oprettes

**Denne sektion grupperer klasserne efter den rækkefølge de bygges i. Den styrende implementeringsliste er stadig Sektion 5. Tags viser vigtighed.**

### Trinblok 1 — Sequential foundation (Trin 0-13; FS-first checkpoint) `[NEED]`

| # | Tag | Fil | Klasse | Implementerer |
|---|-----|-----|--------|--------------|
| 1 | `[NEED]` | `Transfer/IFileTransfer.cs` | `IFileTransfer` | — (interface) |
| 2 | `[NEED]` | `Transfer/TransferResult.cs` | `TransferResult` | — (record) |
| 3 | `[NEED]` | `Sidecar/ISidecarGenerator.cs` | `ISidecarGenerator` | — (interface) |
| 4 | `[NEED]` | `Sidecar/SidecarData.cs` | `SidecarData` | — (klasse) |
| 5 | `[NEED]` | `Sidecar/JsonSidecarGenerator.cs` | `JsonSidecarGenerator` | `ISidecarGenerator` |
| 6 | `[NEED]` | `Progress/BackupProgressSnapshot.cs` | `BackupProgressSnapshot` | `IBackupProgress` |
| 7 | `[NEED]` | `Progress/FileProgressSnapshot.cs` | `FileProgressSnapshot` | `IFileProgress` |
| 8 | `[NEED]` | `Progress/ProgressTracker.cs` | `ProgressTracker` | — (konkret klasse) |
| 9 | `[NEED]` | `Scanner/Filesystem/FilesystemItemScanner.cs` | `FilesystemItemScanner` | `IBackupScanner` |
| 10 | `[NEED]` | `Transfer/Filesystem/FilesystemFileTransfer.cs` | `FilesystemFileTransfer` | `IFileTransfer` |
| 11 | `[NEED]` | `Engine/BackupEngineFactory.cs` | `BackupEngineFactory` | — (konkret klasse) |
| 12 | `[NEED]` | `DependencyInjection/Core4Options.cs` | `Core4Options` | — (options klasse) |
| 13 | `[NEED]` | `DependencyInjection/ServiceCollectionExtensions.cs` | `ServiceCollectionExtensions` | — (static class) |

Derudover: **færdiggør** `Engine/Sequential/SequentialBackupEngine.cs` (scanner-del eksisterer, transfer/sidecar/result mangler).  
Dette er kun et delcheckpoint. Den første komplette baseline er først færdig når Trin 14 også virker.

### Trinblok 2 — MTP completion (Trin 14) `[NEED]`

| # | Tag | Fil | Klasse | Implementerer |
|---|-----|-----|--------|--------------|
| 14 | `[NEED]` | `Scanner/MTP/MTPItemScanner.cs` | `MTPItemScanner` | `IBackupScanner` |
| 15 | `[NEED]` | `Transfer/MTP/MTPFileTransfer.cs` | `MTPFileTransfer` | `IFileTransfer` |

### Trinblok 3 — Robusthed på foundation (efter Trin 14) `[SHOULD]`

| Tag | Fil | Klasse | Handling |
|-----|-----|--------|---------|
| `[SHOULD]` | (ingen nye filer) | — | Udvid foundation-koden med patterns, collision handling, ærlig dry-run, logging og progress-debounce |

### Trinblok 4 — Senere enrichment-trin (Trin 15-28) `[NICE]`

| # | Tag | Fil | Klasse | Implementerer |
|---|-----|-----|--------|--------------|
| 16 | `[NICE]` | `Sidecar/SidecarEnrichment.cs` | `SidecarEnrichment` | — (klasse) |
| 17 | `[NICE]` | `Features/Hashing/IItemHasher.cs` | `IItemHasher` | — (interface) |
| 18 | `[NICE]` | `Features/Hashing/HashResult.cs` | `HashResult` | — (record) |
| 19 | `[NICE]` | `Features/Hashing/FileHasher.cs` | `FileHasher` | `IItemHasher` |
| 20 | `[NICE]` | `Features/Metadata/IMetadataReader.cs` | `IMetadataReader` | — (interface) |
| 21 | `[NICE]` | `Features/Metadata/ExtractedMetadata.cs` | `ExtractedMetadata` | — (klasse) |
| 22 | `[NICE]` | `Features/Metadata/MetadataExtractorReader.cs` | `MetadataExtractorReader` | `IMetadataReader` |
| 23 | `[NICE]` | `Features/Metadata/ExifToolMetadataReader.cs` | `ExifToolMetadataReader` | `IMetadataReader` |
| 24 | `[NICE]` | `Features/Verification/IIntegrityVerifier.cs` | `IIntegrityVerifier` | — (interface) |
| 25 | `[NICE]` | `Features/Verification/VerificationResult.cs` | `VerificationResult` | — (record) |
| 26 | `[NICE]` | `Features/Verification/FileIntegrityVerifier.cs` | `FileIntegrityVerifier` | `IIntegrityVerifier` |
| 27 | `[NICE]` | `Features/Timestamp/ITimestampCorrector.cs` | `ITimestampCorrector` | — (interface) |
| 28 | `[NICE]` | `Features/Timestamp/TimestampCorrectionResult.cs` | `TimestampCorrectionResult` | — (record) |
| 29 | `[NICE]` | `Features/Timestamp/FileTimestampCorrector.cs` | `FileTimestampCorrector` | `ITimestampCorrector` |

### Trinblok 5 — Parallel filesystem senere `[LATER]`

| # | Tag | Fil | Klasse | Implementerer |
|---|-----|-----|--------|--------------|
| 30 | `[LATER]` | `Engine/LimitedParallel/LimitedParallelBackupEngine.cs` | `LimitedParallelBackupEngine` | `IBackupEngine` |

### Trinblok 6 — Persistence/resume senere `[LATER]`

| # | Tag | Fil | Klasse | Implementerer |
|---|-----|-----|--------|--------------|
| 31 | `[LATER]` | `Engine/State/IBackupRepository.cs` | `IBackupRepository` | — (interface) |
| 32 | `[LATER]` | `Engine/State/NoOpBackupRepository.cs` | `NoOpBackupRepository` | `IBackupRepository` |
| 33 | `[LATER]` | `Engine/State/FileSystemBackupRepository.cs` | `FileSystemBackupRepository` | `IBackupRepository` |
| 34 | `[LATER]` | `Engine/State/SqliteBackupRepository.cs` | `SqliteBackupRepository` | `IBackupRepository` |

### Trinblok 7 — Rich progress/UI senere `[LATER]`

| # | Tag | Fil | Klasse | Implementerer |
|---|-----|-----|--------|--------------|
| 35 | `[LATER]` | `Progress/IProgressNotifier.cs` | `IProgressNotifier` | — (interface) |
| 36 | `[LATER]` | `Progress/SpectreProgressNotifier.cs` | `SpectreProgressNotifier` | `IProgressNotifier` |

---

## Sektion 5: Implementeringsrækkefølge — skridt for skridt

**Sektion 5 er den styrende implementeringsliste. Hvert trin har én opgave. Byg, kompilér og gå først videre når trinnet er grønt. Tags viser vigtighed, ikke rækkefølge.**

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
Note: `SidecarEnrichment` kommer først i de senere enrichment-trin — `UpdateAsync` kan stub-implementeres for nu.

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
- Foundation-versionen yield'er alle filer. Glob-filtering via `plan.IncludePatterns` og `plan.ExcludePatterns` kommer først i udvidelserne efter Trin 14.
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
- Brug `Core4Options.TransferBufferSizeBytes` som standardbuffer (default 1 MB).
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

Krav til `UpdateAsync` (stub OK i foundation):
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

**Vigtigt:** Trin 0-13 er stadig kun foundation. Den første komplette baseline kan først markeres som færdig når Trin 14 (MTP) også virker end-to-end.

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
- Ellers → returner parallel senere; midlertidigt returneres sequential

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

### Trin 14 — Implementer MTPItemScanner og MTPFileTransfer `[NEED]`

**Filer:**
- `Scanner/MTP/MTPItemScanner.cs` implementerer `IBackupScanner`
- `Transfer/MTP/MTPFileTransfer.cs` implementerer `IFileTransfer`

Begge klasser modtager MediaDevices-interface via konstruktør (DI).

MTP-specifikke krav:
- Scan: stack-baseret traversal (ingen rekursion), håndter `COMException` per mappe.
- Transfer: download til temp-fil, atomisk rename. Retry 3× med 1s/2s/4s backoff.
- MTP-session åbnes af den kaldende klasse/service — scanners og transfers åbner ikke selv sessionen.
- Sessionen holdes aktiv gennem hele scan+transfer-fasen. Lange loops sender keepalive mindst hver `Core4Options.MtpKeepAliveInterval` (default 30s).
- Hver device-read/device-copy respekterer `Core4Options.MtpOperationTimeout` (default 60s).

### Efter Trin 14 — gør foundation robust før senere features `[SHOULD]`

Ingen nye contracts. Udvid de eksisterende foundation-klasser med:

1. Include/exclude pattern-matching
2. `CollisionStrategy.Rename` og `CollisionStrategy.Overwrite`
3. Ærlig dry-run (ingen writes, men samme destination/collision-logik)
4. `ILogger`-baseret logging
5. Progress-debounce
6. Ærlig tier-gating af endnu ikke implementerede feature-flags

Først når både Trin 14 og disse udvidelser er grønne, er sequential Core4 klar til at bære senere features.

---

### Trin 15–28 — Senere udvidelser efter baseline `[NICE]`

Implementer i denne rækkefølge (hver er uafhængig):

1. `SidecarEnrichment` (Trin 16)
2. Hashing: `IItemHasher` + `HashResult` + `FileHasher` (SHA-256 via `System.Security.Cryptography.SHA256`)
3. Metadata: `IMetadataReader` + `ExtractedMetadata` + `MetadataExtractorReader` (managed primary via MetadataExtractor NuGet) + `ExifToolMetadataReader` (fallback via `exiftool.exe -json`)
4. Verification: `IIntegrityVerifier` + `VerificationResult` + `FileIntegrityVerifier` (sammenligner hash af source og destination)
5. Timestamp: `ITimestampCorrector` + `TimestampCorrectionResult` + `FileTimestampCorrector` (sætter `File.SetLastWriteTimeUtc`)

Metadata/timestamp-regler:
1. Læs altid filsystem-attributter først (`FileInfo`) og udfyld fallback-felterne i `ExtractedMetadata`.
2. Forsøg derefter managed metadata via `MetadataExtractorReader`.
3. Hvis managed parse fejler, er tvetydig eller mangler de ønskede tags, og `Core4Options.UseExifToolFallback == true`, så brug `ExifToolMetadataReader`.
4. `GetBestDate()` følger: `DateTimeOriginal` → `CreateDate` → `QuickTimeCreated` → `FileSystemLastWriteUtc` → `FileSystemCreationUtc` → `null`.
5. `FileTimestampCorrector` må kun køre når metadata findes og `GetBestDate()` returnerer en gyldig dato.

Alle optional features:
- Aktiveres kun hvis tilsvarende flag i `BackupPlan` er `true`.
- Fejler de: log, sidecar opdateres med fejlstatus, backup fortsætter.
- Kører *efter* `CreateAsync` — sidecar eksisterer allerede.

---

### Trin 29 — LimitedParallelBackupEngine `[LATER]`

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

## Sektion 7: Kritiske kravspecifikationer

Disse krav er IKKE lister — de skal implementeres præcis:

### 7.1 Pattern-matching for Include/ExcludePatterns `[SHOULD]`

`FilesystemItemScanner` skal matche `RelativePath` mod patterns. Eksempler:
- `*.jpg` → matcher `photo.jpg`
- `DCIM/**` → matcher `DCIM/Camera/IMG.jpg`
- `!temp/**` → exclude (hvis implementeret via prefix)

**Tool:** Brug `Microsoft.Extensions.FileSystemGlobbing.Matcher` (allerede i .NET ecosystem).

### 7.2 Atomisk sidecar-skrivning `[NEED]`

Begge `CreateAsync` og `UpdateAsync` skal bruge samme mønster:
1. Skriv til `{sidecarPath}.tmp`
2. `File.Move({sidecarPath}.tmp, {sidecarPath}, overwrite: true)`
3. Ved exception: slet `.tmp`, kast aldrig til engine (log kun)

### 7.3 Thread-safety i ProgressTracker `[NEED]`

`ProgressTracker` bruges kun af én tråd per engine-instans. Men den skal stadig være sikker:
- Tæller: `Interlocked.Increment`, `Interlocked.Add`
- Phase: `Volatile.Write`, `Volatile.Read`
- ActiveFiles: `ConcurrentDictionary<string, FileProgressSnapshot>`

### 7.4 Kollisions-håndtering ved Rename `[SHOULD]`

```csharp
// Pseudo-kode — implementér præcist dette
if (plan.CollisionStrategy == CollisionStrategy.Rename && File.Exists(destination))
{
    string dir = Path.GetDirectoryName(destination) ?? "";
    string name = Path.GetFileNameWithoutExtension(destination);
    string ext = Path.GetExtension(destination);
    int counter = 1;
    do
    {
        destination = Path.Combine(dir, $"{name}_{counter++}{ext}");
    } while (File.Exists(destination));
}
```

### 7.5 Tempfil-cleanup ved fejl `[NEED]`

```csharp
// I FilesystemFileTransfer.TransferAsync() catch-blok:
try
{
    // ... copy logic ...
}
catch (Exception ex)
{
    try { File.Delete(destinationPath + ".tmp"); } catch { /* ignore */ }
    return TransferResult { Succeeded = false, ErrorCode = ..., ErrorMessage = ex.Message };
}
```

### 7.6 Dry-run skal være fuld simulering `[SHOULD]`

Dry-run skal køre hele flowet — scan, destination-beregning, collision-check og simulering af mappeoprettelse — men uden writes:

```csharp
// I SequentialBackupEngine transfer-loop:
if (plan.DryRun)
{
    // Beregn destination (som hvis rigtig transfer), tjek collision og noter "ville oprette mappe",
    // men skriv ikke filer, sidecars eller mapper til disk
    string dest = ResolveDestination(item, plan);
    item.DestinationPath = dest;
    item.Status = BackupItemStatus.Succeeded;
    progressTracker.CompleteFile(item, fakeSuccessResult);
    // Ingen sidecar-skrivning ved dry-run
    continue;
}
```

### 7.7 Decimal-præcision ved tælling `[NEED]`

- `FilesDiscovered`, `FilesSucceeded`, etc. er `int` — max ~2 mia. filer
- `BytesTotal`, `BytesProcessed` er `long` — max ~9 exabyte
- Brug `checked` hvis du forventer overflow (sandsynligvis ikke)

### 7.8 MTP-session lifecycle `[NEED]`

MTP-session skal åbnes *uden for* scanner og transfer. Engine eller en dedikeret sessionmanager skal håndtere:

```csharp
using (IMediaDevice device = MediaDevice.GetDevices().FirstOrDefault(d => d.ID == plan.Source))
{
    device.Connect();
    try
    {
        // scanner og transfer bruger device, men åbner/lukker den ikke
        await foreach (var item in mtp_scanner.ScanAsync(plan, ct)) { ... }
    }
    finally
    {
        device.Disconnect();
    }
}
```

**Scanner/Transfer modtager device via konstruktør, de "åbner" den ikke.**

Derudover gælder:
- Keepalive mindst hver `Core4Options.MtpKeepAliveInterval` (default 30s) under lange scan/transfer-forløb.
- Hver læse-/copy-operation respekterer `Core4Options.MtpOperationTimeout` (default 60s).
- Cleanup sker altid i `finally`, også ved cancellation.

### 7.9 Retry-strategi for MTP `[NEED]`

```csharp
// MTPFileTransfer.TransferAsync():
int[] backoffMs = [1000, 2000, 4000];
for (int attempt = 0; attempt < 3; attempt++)
{
    try
    {
        // Download til temp, rename, return TransferResult { Succeeded = true }
        return success_result;
    }
    catch (COMException ex) when (IsTransient(ex))
    {
        if (attempt < 2) await Task.Delay(backoffMs[attempt], ct);
    }
    catch (COMException ex) when (!IsTransient(ex))
    {
        return TransferResult { Succeeded = false, ErrorCode = MediaDeviceDisconnected, ... };
    }
}
return TransferResult { Succeeded = false, ErrorCode = TransferFailed, ... };
```

### 7.10 SkipExisting logik `[NEED]`

```csharp
// I SequentialBackupEngine transfer-loop:
if (plan.SkipExisting && File.Exists(item.DestinationPath))
{
    item.Status = BackupItemStatus.Skipped;
    progressTracker.SkipFile(item);
    // Ingen transfer, ingen sidecar for skipped fil
    progress?.Report(progressTracker.GetSnapshot());
    continue;
}
```

### 7.11 StopOnError logik `[NEED]`

```csharp
// Efter transfer:
if (!result.Succeeded)
{
    item.Status = BackupItemStatus.Failed;
    // Log fejlen
    if (plan.StopOnError)
    {
        session.Fail(result.ErrorCode ?? BackupErrorCode.TransferFailed);
        break; // Exit transfer-loop
    }
}
```

### 7.12 Chunk-based I/O og bufferstørrelse `[NEED]`

- Transfer og hashing skal være streaming-baseret. Ingen `ReadAllBytes`, ingen hel-fil buffering i RAM.
- Standardbuffer er `Core4Options.TransferBufferSizeBytes` (default 1 MB).
- Samme bufferregel gælder `FilesystemFileTransfer`, `MTPFileTransfer` og `FileHasher`.
- Byte-progress rapporteres pr. chunk.

### 7.13 Logging-strategi `[SHOULD]`

Du vil bruge `ILogger` fra `Microsoft.Extensions.Logging`. Dette skal logges:

**Minimum når du udvider foundation efter Trin 14:**
- Scan start: `_logger.LogInformation("Scanning {source}", plan.Source)`
- Scan fejl per item: `_logger.LogWarning("Skipped {path} — {reason}", item.RelativePath, reason)`
- Transfer fejl per item: `_logger.LogError("Transfer failed: {path} — {code}: {msg}", item.RelativePath, result.ErrorCode, result.ErrorMessage)`
- Transfer succes summary: `_logger.LogInformation("Transfer complete: {succeeded}/{total} files")`
- Job cancelled: `_logger.LogWarning("Backup cancelled by user")`
- Job failed: `_logger.LogError("Backup failed: {reason}", session.FailureReason)`

**Enrichment-fejl når de senere trin er aktiveret:**
- Hashing fejl: `_logger.LogWarning("Hash computation failed for {path} — continuing")`
- Metadata fejl: `_logger.LogWarning("Metadata extraction failed for {path} — continuing")`
- Verification fejl: `_logger.LogWarning("Verification mismatch for {path} — continuing")`
- Timestamp fejl: `_logger.LogWarning("Timestamp correction failed for {path} — continuing")`

**Hvad der IKKE skal logges:**
- Hver enkelt fil under normal succes (for 50.000 filer ville det være støj)
- Stack traces for expected fejl (fx IOException ved disk fuld) — kun ErrorMessage
- Stack trace under cancellation

### 7.14 ProgressReporter-konfiguration `[SHOULD]`

`IProgress<IBackupProgress>` skal rapporteres *ikke for hver enkelt fil*, men periodisk. Eksempel:

```csharp
private long lastProgressReportMs = 0;
private const long ProgressReportIntervalMs = 500; // Rapportér max hver 500ms

// I transfer-loop, efter hver fil:
long nowMs = Environment.TickCount64;
if (nowMs - lastProgressReportMs > ProgressReportIntervalMs)
{
    progress?.Report(progressTracker.GetSnapshot());
    lastProgressReportMs = nowMs;
}
```

Dette forhindrer at UI'en bliver oversvømmet med updates.

### 7.15 Gating af endnu ikke implementerede features `[NEED]`

En minimal version må aldrig lade som om en senere-tier feature virker. **Silent ignore er ikke tilladt.**

Tilladt adfærd er kun:
1. Afvis optionen tidligt med tydelig validation-fejl.
2. Nedgrader den kun hvis nedgraderingen er sikker og dokumenteret.

Konkrete regler:
- Før du har implementeret foundation-udvidelserne efter Trin 14: `IncludePatterns`, `ExcludePatterns` og `CollisionStrategy != Skip` må afvises tydeligt, hvis du endnu ikke har implementeret dem.
- Før du har implementeret Trin 15-28: `EnableHashing`, `EnableMetadata`, `EnableVerification`, `EnableTimestampCorrection` må ikke bare ignoreres; de skal give tydelig validation-fejl.
- Før du har implementeret Trin 29: `MaxDegreeOfParallelism > 1` må gerne nedgraderes til sekventiel kørsel, men det skal logges tydeligt.

---

## Sektion 7b: Tre arkitekturregler der aldrig brydes

1. **MTP er altid sekventielt.** Ingen parallel adgang mod MTP-enheder.
2. **Sidecar skrives atomisk direkte efter succesfuld transfer.** Aldrig samlet til sidst.
3. **Optional features vælter aldrig core-backup.** De er non-fatal per definition.

---

## Sektion 8: Dokumenthierarki

| Dokument | Brug |
|----------|------|
| `docs_fromGPT5.4\CORE4_MASTER_SYNTHESIS.md` | Endelig beslutningstekst — konflikt? Følg dette. |
| `docs_fromGPT5.4\CORE4_REQUIREMENTS_TIERS.md` | Tier-definitioner og exit-kriterier |
| `docs_fromGPT5.4\CORE4_IMPLEMENTATION_GUIDE_DA.md` | **Denne guide** — kontrakter og implementeringsrækkefølge |
| `docs_fromGPT5.4\Core4_Master_Architecture.md` | Dyb teknisk reference til edge cases og discrepansnoter |

---

_Version 4.1 — normaliseret mod `CORE4_MASTER_SYNTHESIS.md`, `CORE4_REQUIREMENTS_TIERS.md` og det faktiske `BMTP3.Core4` skeleton pr. maj 2026._
