# Core4 Implementeringsguide for udviklere (trin for trin)

**Version:** 3.0 (dyb revision)  
**Formål:** Styredokument for den udvikler, der skal implementere Core4 færdig fra det eksisterende skelet.  
**Tilgang:** Bevidst forklarende. Ikke kun "hvad", men "hvorfor", "hvordan det hænger sammen", og "hvad gør jeg nu".

Guiden er baseret på:

- `docs\CORE4_MASTER_SYNTHESIS.md` — endelige arkitekturbeslutninger (autoritativ)
- `docs\Core4_Master_Architecture.md` — dyb teknisk reference
- Eksisterende `BMTP3.Core4` skeleton-kode (analyseret i detalje)
- Lessons learned fra Core, Core2, Core3

---

## Del 1: Formål og kontekst

### 1.1 Hvad Core4 er — og hvad det ikke er

Core4 er en backup-engine, der kopierer filer fra enten en **MTP-enhed** (telefon, kamera) eller et **filsystem** (disk, netværksdrev) til en destination på det lokale filsystem. Resultatet er en kopi af filerne plus en `*.sidecar.json` per fil, der fungerer som audit trail og sporbarhedsdokument.

Core4 er *ikke* et generelt filsynkroniseringssystem. Det er heller ikke et cloud-backup-system. Det er en veldefineret, enkeltstående backup-kørsel med klare succeskriterier.

Hvorfor genimplementere? Core, Core2 og Core3 fungerede ikke stabilt i alle scenarier — især MTP-baserede kørsel havde problemer med disconnect, hængende tråde og tab af sporbarhed. Core4 løser disse problemer med en hybrid strategi: MTP kører altid sekventielt og atomisk, mens filsystem-kilden kan køres med begrænset parallelisme for performance.

### 1.2 De tre regler du aldrig må bryde

Disse beslutninger er låst. De kan ikke forhandles undervejs i implementeringen:

**Regel 1 — MTP er altid sekventielt.**  
Parallel adgang mod MTP-enheder forårsager disconnect og deadlocks (demonstreret i Core2). Ingen undtagelser, ingen "prøv det alligevel".

**Regel 2 — Minimal sidecar skrives umiddelbart efter succesfuld transfer.**  
Sidecar er sporbarhed. Hvis engine crasher efter transfer men inden sidecar, mister vi audit-sporet. Sidecar skrives atomisk (`.tmp` → rename) direkte efter succesfuld kopi.

**Regel 3 — Optional features (hash/metadata/verify/timestamp) må ikke vælte kerne-backup.**  
Disse features er tilvalg. Fejler de, logges fejlen, og backup fortsætter. De er aldrig blokkende for overførslen.

### 1.3 Fejlpolitik (default-adfærd)

Fra lessons learned i Core3 ved vi, at *implicit* fejlpolitik er kilde til uforudsigelig adfærd. Core4 definerer eksplicit:

- **Per-fil transfer-fejl:** item markeres som `Failed`, fejl logges, *backup fortsætter* med næste fil. Dette er default.
- **StopOnError = true i BackupPlan:** job stopper ved første transfer-fejl. Dette er et bevidst brugervalg.
- **Fatale fejl** (ex: destination-disk utilgængelig for hele job, MTP-enhed disconnectet permanent): job stopper, phase sættes til `Failed` med `FailureReason`.
- **Fejl i optional features:** non-fatal, logges, sidecar opdateres med fejlinfo, backup fortsætter.

---

## Del 2: Den samlede arkitektur

### 2.1 Overblik over lag

Core4 er struktureret i fire lag. Her er den fulde oversigt med ansvar:

```
┌─────────────────────────────────────────────────────────────┐
│  API-LAG  (public kontrakter)                               │
│  IBackupEngine, IBackupProgress, IFileProgress              │
│  BackupPlan (input), BackupResult (output)                  │
└──────────────────────────┬──────────────────────────────────┘
                           │ implementeres af
┌──────────────────────────▼──────────────────────────────────┐
│  ENGINE-LAG  (orkestrering)                                  │
│  BackupEngineFactory                                         │
│  SequentialBackupEngine  ◄── MTP altid + FS forced          │
│  LimitedParallelBackupEngine  ◄── FS med worker-pool        │
│  BackupSessionState  ◄── intern tilstand per kørsel         │
└──┬────────────────────┬────────────────────┬────────────────┘
   │                    │                    │
┌──▼──────────┐  ┌──────▼──────┐  ┌─────────▼────────────────┐
│ SCAN-LAG    │  │TRANSFER-LAG │  │ SIDECAR-LAG              │
│IBackupScanner│  │IFileTransfer│  │ISidecarGenerator         │
│             │  │             │  │                           │
│ Filesystem  │  │ Filesystem  │  │ JsonSidecarGenerator      │
│ ItemScanner │  │ FileTransfer│  │                           │
│             │  │             │  └───────────────────────────┘
│ MTPItem     │  │ MTPFile     │
│ Scanner     │  │ Transfer    │
└─────────────┘  └─────────────┘
┌─────────────────────────────────────────────────────────────┐
│  INFRASTRUKTUR-LAG                                           │
│  ProgressTracker (trådsikker)                                │
│  IBackupSessionStateStore / InMemoryBackupSessionStateStore  │
│  DependencyInjection / ServiceCollectionExtensions           │
│  Optional features: IItemHasher, IMetadataReader,           │
│                     IIntegrityVerifier, ITimestampCorrector   │
└─────────────────────────────────────────────────────────────┘
```

**Forklaring af lagene:**

Det øverste lag (API) er den ydre kontrakt. Den fortæller omverdenen præcis, hvad Core4 kan gøre og hvad der kommer ud af det. Alt i dette lag er `public` og må ikke ændres uden overvejelse.

Engine-laget er orkestratoren. Den ved *rækkefølgen* af trin, men gør ikke arbejdet selv — den delegerer til specialiserede services.

Scan-, Transfer- og Sidecar-lagene udfører det faktiske arbejde: finde filer, kopiere dem, og skrive sporbarhedsdata.

Infrastruktur-laget støtter de andre lag med tværgående ansvar: progress-tracking, session-state, DI-registration.

### 2.2 Eksisterende kode i skeleton (hvad der allerede er lavet)

Inden du begynder at skrive ny kode, forstå hvad der *allerede eksisterer og er korrekt*:

**Fuldstændigt og korrekt — rør ikke:**

| Fil | Hvad den gør |
|-----|-------------|
| `Api/IBackupEngine.cs` | Public kontrakt for engine — `RunAsync(plan, progress, ct)` |
| `Api/IBackupProgress.cs` | Progress snapshot interface med fase + discovery + processing |
| `Api/IFileProgress.cs` | Per-fil progress interface |
| `Models/BackupPlan.cs` | Immutable record med alle backup-konfigurationsfelter |
| `Models/BackupResult.cs` | Immutable record med job-resultat og statistik |
| `Models/BackupItem.cs` | Intern (internal) model for et enkelt backup-item |
| `Models/Enums/BackupPhase.cs` | Starting/Scanning/Transferring/Completed/Cancelled/Failed |
| `Models/Enums/BackupErrorCode.cs` | Struktureret fejlklassifikation |
| `Models/Enums/BackupItemStatus.cs` | Pending/Succeeded/Skipped/Failed |
| `Models/Enums/BackupSourceType.cs` | FileSystem/MediaDevice |
| `Models/Enums/OutputStructure.cs` | Flat/PreserveHierarchy |
| `Models/Enums/CollisionStreategy.cs` | Skip/Overwrite/Rename (note: typo i filnavn, enum hedder CollisionStrategy) |
| `Engine/State/BackupSessionState.cs` | Intern session-state med items, phase, FailureReason |
| `Engine/State/BackupSessionStateKey.cs` | Record med SessionId + SourceIdentity |
| `Engine/State/BackupSessionStateKeyFactory.cs` | Opretter key fra BackupPlan (Guid + sourceType:source) |
| `Engine/State/IBackupSessionStateStore.cs` | Interface for state-store |
| `Engine/State/InMemoryBackupSessionStateStore.cs` | In-memory implementation |
| `Engine/Validation/BackupPlanValidator.cs` | Kaster BackupPlanValidationException ved ugyldige planer |
| `Scanner/IBackupScanner.cs` | `IAsyncEnumerable<BackupItem> ScanAsync(plan, ct)` |
| `Helpers/Guard.cs` | `RequireNonNull<T>` hjælper |

**Delvist lavet — skal færdiggøres:**

| Fil | Problem |
|-----|---------|
| `Engine/Sequential/SequentialBackupEngine.cs` | Scanner korrekt, men slutter med `throw new NotImplementedException()` |
| `Engine/BackupEngine.cs` | Kun kommentarer, ingen implementering |

**Mangler helt — skal oprettes:**

```
Scanner/Filesystem/FilesystemItemScanner.cs
Scanner/MTP/MTPItemScanner.cs
Transfer/IFileTransfer.cs                        ← interface mangler
Transfer/TransferResult.cs                       ← result-type mangler
Transfer/Filesystem/FilesystemFileTransfer.cs
Transfer/MTP/MTPFileTransfer.cs
Sidecar/ISidecarGenerator.cs                     ← interface mangler
Sidecar/SidecarData.cs                           ← data-model mangler
Sidecar/JsonSidecarGenerator.cs
Progress/ProgressTracker.cs
Progress/BackupProgressSnapshot.cs
Engine/BackupEngineFactory.cs
DependencyInjection/ServiceCollectionExtensions.cs
```

### 2.3 Komplet klasserelationsdiagram

Her er alle klasser og deres indbyrdes relationer. Pil "→" betyder "bruger" eller "afhænger af":

```
ServiceCollectionExtensions
    registrerer → BackupEngineFactory
    registrerer → InMemoryBackupSessionStateStore  (som IBackupSessionStateStore)
    registrerer → FilesystemItemScanner            (som IBackupScanner for FS)
    registrerer → MTPItemScanner                   (som IBackupScanner for MTP)
    registrerer → FilesystemFileTransfer           (som IFileTransfer for FS)
    registrerer → MTPFileTransfer                  (som IFileTransfer for MTP)
    registrerer → JsonSidecarGenerator             (som ISidecarGenerator)
    registrerer → ProgressTracker

BackupEngineFactory
    modtager → BackupPlan
    returnerer → SequentialBackupEngine  (hvis MTP eller forced sequential)
    returnerer → LimitedParallelBackupEngine  (hvis FS og ikke forced sequential)

SequentialBackupEngine
    bruger → IBackupScanner
    bruger → IFileTransfer
    bruger → ISidecarGenerator
    bruger → ProgressTracker
    bruger → IBackupSessionStateStore
    returnerer → BackupResult

LimitedParallelBackupEngine  (Tier 2)
    bruger → IBackupScanner
    bruger → IFileTransfer
    bruger → ISidecarGenerator
    bruger → ProgressTracker
    bruger → IBackupSessionStateStore
    bruger → SemaphoreSlim  (concurrency control)
    returnerer → BackupResult

FilesystemItemScanner : IBackupScanner
    scanner → Directory.EnumerateFiles(...)
    returnerer → BackupItem (via IAsyncEnumerable)

MTPItemScanner : IBackupScanner
    bruger → IMediaDevice (fra MediaDevices.dll)
    returnerer → BackupItem (via IAsyncEnumerable)

FilesystemFileTransfer : IFileTransfer
    udfører → File.Copy() i chunks
    returnerer → TransferResult

MTPFileTransfer : IFileTransfer
    bruger → IMediaDevice
    downloader → til temp-fil, derefter atomisk rename
    returnerer → TransferResult

JsonSidecarGenerator : ISidecarGenerator
    skriver → *.sidecar.json (via tmp + rename)
    opdaterer → eksisterende sidecar (ved enrichment)

ProgressTracker
    eksponerer → BackupProgressSnapshot : IBackupProgress
    trådsikker via → Interlocked / lock

BackupSessionState
    indeholder → List<BackupItem>
    tracker → Phase (Starting/Scanning/Transferring/Completed/Cancelled/Failed)
    tracker → FailureReason
```

---

## Del 3: Interface-kontrakter i detalje

Alle interfaces er enten allerede defineret i skeleton, eller skal du selv oprette. Her er den præcise kontrakt for alle.

### 3.1 IBackupEngine (eksisterer — rør ikke)

```csharp
// namespace BMTP3.Core4.Api
public interface IBackupEngine
{
    Task<BackupResult> RunAsync(
        BackupPlan plan,
        IProgress<IBackupProgress>? progress,
        CancellationToken cancellationToken);
}
```

`RunAsync` er den eneste public API for at starte et backup-job. Parametrene:

- `plan` er immutable konfiguration. Alt hvad engine behøver at vide om kilden, destinationen og adfærden er her.
- `progress` er *optional*. Hvis kalderen ønsker løbende feedback, sender de et `IProgress<IBackupProgress>` objekt. Engine kalder `progress.Report(snapshot)` periodisk. Hvis null, kører engine stille.
- `cancellationToken` er det primære mekanisme til at afbryde et job. Engine skal observere den konsekvent.

Engine må **aldrig kaste ukontrollerede exceptions** til kalderen for normale per-item fejl. Kun ved fatale systemiske fejl (se fejlpolitik) er det acceptabelt at kaste.

### 3.2 IBackupProgress (eksisterer — rør ikke)

```csharp
// namespace BMTP3.Core4.Api
public interface IBackupProgress
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

Dette er et *snapshot-interface*. Det beskriver tilstanden på et givet tidspunkt. Du skal lave en konkret klasse `BackupProgressSnapshot` der implementerer dette, og som fyldes ud fra `ProgressTracker`.

### 3.3 IBackupScanner (eksisterer — rør ikke)

```csharp
// namespace BMTP3.Core4.Scanner
internal interface IBackupScanner
{
    IAsyncEnumerable<BackupItem> ScanAsync(
        BackupPlan plan,
        CancellationToken cancellationToken);
}
```

Scanner returnerer et *streaming* sequence med `IAsyncEnumerable<BackupItem>`. Dette er bevidst. Vi materialiserer **ikke** hele fil-listen i hukommelsen. For enheder med 50.000 filer ville en fuld liste bruge unødvendig RAM og forsinke starten.

Engine itererer med `await foreach` og kan begynde at transferere mens scan stadig kører (streamed pipeline).

### 3.4 IFileTransfer (mangler — skal oprettes)

```csharp
// namespace BMTP3.Core4.Transfer
internal interface IFileTransfer
{
    Task<TransferResult> TransferAsync(
        BackupItem item,
        string destinationPath,
        IProgress<long>? bytesProgress,
        CancellationToken cancellationToken);
}
```

`TransferAsync` kopierer én fil fra kilden til destination. Forklaring af parametre:

- `item` indeholder al information om kilde-filen: `SourcePath`, `RelativePath`, `SizeBytes`.
- `destinationPath` er den fuldt opløste sti til destinations-filen (inklusive filnavn). Engine beregner denne og sender den med.
- `bytesProgress` er optional. Kald `bytesProgress.Report(bytesTransferred)` under overførslen for at give live byte-feedback.
- `cancellationToken` — stop øjeblikkeligt og ryd op.

Transfer returnerer et resultat, den kaster **ikke** exceptions for normale fejl:

```csharp
// namespace BMTP3.Core4.Transfer
internal sealed record TransferResult
{
    public bool Succeeded { get; init; }
    public string? DestinationPath { get; init; }    // null ved fejl
    public long BytesTransferred { get; init; }
    public BackupErrorCode? ErrorCode { get; init; } // null ved succes
    public string? ErrorMessage { get; init; }       // null ved succes
    public Exception? Exception { get; init; }       // null ved succes
}
```

Hvorfor returnere et resultat frem for at kaste? Fordi "en fil fejlede" er en **forventet og håndteret tilstand** i en backup. Det er ikke en undtagelse, der skal propagere op og vælte hele job. Engine beslutter ud fra `TransferResult.Succeeded` og `BackupPlan.StopOnError`, om backup kan fortsætte.

### 3.5 ISidecarGenerator (mangler — skal oprettes)

```csharp
// namespace BMTP3.Core4.Sidecar
internal interface ISidecarGenerator
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

Der er to operationer:

- `CreateAsync` opretter den minimale sidecar umiddelbart efter succesfuld transfer. Den skal ikke vente på optional features.
- `UpdateAsync` bruges af optional features (hash, metadata) til at *tilføje* information til en eksisterende sidecar. Skrivning er altid atomisk.

### 3.6 Optional feature interfaces (mangler — skal oprettes i Tier 2)

Disse interfaces er til enrichment-pipeline. De er simple:

```csharp
// namespace BMTP3.Core4.Features
internal interface IItemHasher
{
    Task<HashResult> ComputeAsync(string filePath, CancellationToken cancellationToken);
}

internal interface IMetadataReader
{
    Task<ExtractedMetadata?> ReadAsync(string filePath, CancellationToken cancellationToken);
}

internal interface IIntegrityVerifier
{
    Task<VerificationResult> VerifyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken);
}

internal interface ITimestampCorrector
{
    Task<bool> CorrectAsync(
        string destinationPath,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);
}
```

Alle har det til fælles: de modtager en filsti, udfører ét specialiseret arbejde, og returnerer et struktureret resultat. Fejler de, er det non-fatal som standard.

---

## Del 4: BackupItem — det interne nervesystem

`BackupItem` er den interne datamodel, der følger en fil gennem hele backup-pipeline. Den er `internal` og ikke eksponeret til omverdenen.

```csharp
// Eksisterende klasse — forstå alle felter
internal sealed class BackupItem
{
    public string Id { get; init; }              // Unik per session. Bruges til deduplicering.
    public string SourcePath { get; init; }      // Fuld sti til kildefil (fx C:\Photos\IMG_001.jpg)
    public string RelativePath { get; init; }    // Relativ til kildens rod (fx DCIM\Camera\IMG_001.jpg)
    public string? DestinationPath { get; set; } // Sættes af engine FØR transfer (fx D:\Backup\2024\IMG_001.jpg)
    public long? SizeBytes { get; init; }        // Størrelse i bytes, null hvis ukendt
    public DateTimeOffset? ModifiedAt { get; init; } // Sidst modificeret tidspunkt
    public BackupItemStatus Status { get; set; } // Pending → Succeeded/Skipped/Failed
}
```

**RelativePath er nøglefeltet.** Det er den relative sti, der bruges til at beregne destinationsstien.

Eksempel for et filsystem-kilde med root `C:\Photos`:
- Kilde-fil: `C:\Photos\2024\August\IMG_001.jpg`
- RelativePath: `2024\August\IMG_001.jpg`
- Med `OutputStructure.PreserveHierarchy` og destination `D:\Backup`:
  - DestinationPath = `D:\Backup\2024\August\IMG_001.jpg`

For MTP-kilde (ex: telefon med rod `/storage/emulated/0/DCIM`):
- Kilde-fil: `/storage/emulated/0/DCIM/Camera/IMG_001.jpg`
- RelativePath: `Camera\IMG_001.jpg`
- DestinationPath = `D:\Backup\Camera\IMG_001.jpg`

**Id-feltet** skal være unikt inden for session. En nem konvention: brug `Guid.NewGuid().ToString("N")` i scanner.

---

## Del 5: Implementeringsrækkefølge (trin for trin)

Trin A–E udgør Tier 1 (det der skal virke for at Core4 er brugbart). Trin F–G er Tier 2 (enrichment og parallel FS).

### Trin A: Stabilisér BackupEngine og SequentialBackupEngine

**Hvorfor dette er første trin:**  
`BackupEngine.cs` og `SequentialBackupEngine.cs` er entry points. `BackupEngine.cs` kaster i dag `NotImplementedException` for al kode. `SequentialBackupEngine` scanner items, men slutter med `throw new NotImplementedException()`. Ingen af dem returnerer et gyldigt `BackupResult`.

**Hvad du skal gøre:**

Beslutning om arkitektur: `BackupEngine.cs` og `SequentialBackupEngine.cs` overlapper i ansvar. Vores plan er:

1. `SequentialBackupEngine` er den *rigtige* implementation (den har allerede DI-konstruktor og scan-logik).
2. `BackupEngine.cs` kan enten slettes, eller omdøbes til at være en `BackupEngineFactory`-facade (se Trin B).

Start med at **slette `BackupEngine.cs`** eller lade den være et rent placeholder-alias. Fokus er `SequentialBackupEngine`.

I `SequentialBackupEngine` skal du:
1. Tilføje `IFileTransfer`, `ISidecarGenerator`, og `ProgressTracker` i konstruktøren.
2. Implementere trin 4–9 (transfer-loop, sidecar, counters, cancellation, result).
3. Fjerne `throw new NotImplementedException()`.

Konstruktøren skal se sådan ud når du er færdig:

```csharp
internal sealed class SequentialBackupEngine : IBackupEngine
{
    private readonly IBackupScanner scanner;
    private readonly IFileTransfer fileTransfer;
    private readonly ISidecarGenerator sidecarGenerator;
    private readonly ProgressTracker progressTracker;
    private readonly IBackupSessionStateStore sessionStateStore;

    public SequentialBackupEngine(
        IBackupScanner scanner,
        IFileTransfer fileTransfer,
        ISidecarGenerator sidecarGenerator,
        ProgressTracker progressTracker,
        IBackupSessionStateStore sessionStateStore)
    { ... }
}
```

**Definition of done for Trin A:**
- `SequentialBackupEngine` kompilerer og er klar til at modtage færdige scanner/transfer/sidecar-implementationer.
- Ingen `NotImplementedException` i runtime-path.

---

### Trin B: BackupEngineFactory

**Hvorfor:**  
Engine valget (sequential vs. limited parallel) skal ske ét sted, ikke spredt rundt. En factory giver ét kontrolpunkt.

**Opret `Engine/BackupEngineFactory.cs`:**

```csharp
// namespace BMTP3.Core4.Engine
internal sealed class BackupEngineFactory
{
    private readonly SequentialBackupEngine sequentialEngine;
    private readonly LimitedParallelBackupEngine parallelEngine;

    public BackupEngineFactory(
        SequentialBackupEngine sequentialEngine,
        LimitedParallelBackupEngine parallelEngine)
    {
        this.sequentialEngine = sequentialEngine;
        this.parallelEngine = parallelEngine;
    }

    public IBackupEngine Create(BackupPlan plan)
    {
        // MTP-enheder kører ALTID sekventielt
        if (plan.SourceType == BackupSourceType.MediaDevice)
            return sequentialEngine;

        // Bruger kan tvinge sekventiel via MaxDegreeOfParallelism = 1
        if (plan.MaxDegreeOfParallelism == 1)
            return sequentialEngine;

        // Filesystem med begrænset parallelisme
        return parallelEngine;
    }
}
```

Denne factory bruges i DI-registration til at binde `IBackupEngine` til korrekt engine baseret på plan.

**Definition of done for Trin B:**
- Factory kompilerer. `LimitedParallelBackupEngine` kan midlertidigt delegere til sequential (stub).

---

### Trin C: FilesystemItemScanner

**Hvorfor:**  
Scanner er fundamentet for alt. Uden korrekte `BackupItem`-objekter med korrekt `RelativePath` er destination-beregning og transfer-logik forkert.

**Opret `Scanner/Filesystem/FilesystemItemScanner.cs`:**

```csharp
// namespace BMTP3.Core4.Scanner.Filesystem
internal sealed class FilesystemItemScanner : IBackupScanner
{
    public async IAsyncEnumerable<BackupItem> ScanAsync(
        BackupPlan plan,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string root = plan.Source;
        SearchOption searchOption = plan.Recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        foreach (string filePath in Directory.EnumerateFiles(root, "*", searchOption))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Beregn RelativePath
            string relativePath = Path.GetRelativePath(root, filePath);

            // Anvend include/exclude patterns
            if (!MatchesFilters(relativePath, plan.IncludePatterns, plan.ExcludePatterns))
                continue;

            FileInfo info = new(filePath);

            yield return new BackupItem
            {
                Id = Guid.NewGuid().ToString("N"),
                SourcePath = filePath,
                RelativePath = relativePath,
                SizeBytes = info.Exists ? info.Length : null,
                ModifiedAt = info.Exists ? info.LastWriteTimeUtc : null,
            };

            // Giv kontrol til event loop (undgå blokering af async)
            await Task.Yield();
        }
    }
}
```

**Om pattern-matching:**  
`IncludePatterns` og `ExcludePatterns` i `BackupPlan` er glob-patterns som `*.jpg`, `DCIM/**`. Brug `Microsoft.Extensions.FileSystemGlobbing` NuGet-pakke (er allerede i .NET ecosystem) eller skriv en simpel pattern-matcher. Vigtigt: patterns matches mod `RelativePath`, ikke fuld sti.

**Om stack-baseret traversal:**  
For meget dybt nestede mapper kan `Directory.EnumerateFiles` med `AllDirectories` give `PathTooLongException`. En alternativ, sikrere implementation bruger en eksplicit stack:

```csharp
Stack<string> dirs = new();
dirs.Push(root);

while (dirs.Count > 0)
{
    string current = dirs.Pop();
    cancellationToken.ThrowIfCancellationRequested();

    foreach (string subDir in Directory.EnumerateDirectories(current))
        dirs.Push(subDir);

    foreach (string file in Directory.EnumerateFiles(current))
    {
        // ... process file
    }
}
```

Denne approach er robust mod rekursionsdybde og giver bedre kontrol over fejlhåndtering per mappe.

**Definition of done for Trin C:**
- Scanner returnerer korrekte `BackupItem`-objekter med `RelativePath` korrekt beregnet.
- IncludePatterns/ExcludePatterns filtrerer korrekt.
- Cancellation stoppper scan midtvejs.

---

### Trin D: FilesystemFileTransfer

**Hvorfor:**  
Transfer er den kritiske operation. Fejl her må ikke skabe korrupte destinations-filer. Transfer skal være robust, atomic nok, og returnere strukturerede resultater.

**Opret `Transfer/Filesystem/FilesystemFileTransfer.cs`:**

```csharp
// namespace BMTP3.Core4.Transfer.Filesystem
internal sealed class FilesystemFileTransfer : IFileTransfer
{
    private const int BufferSize = 81920; // 80 KB chunks

    public async Task<TransferResult> TransferAsync(
        BackupItem item,
        string destinationPath,
        IProgress<long>? bytesProgress,
        CancellationToken cancellationToken)
    {
        try
        {
            // Opret destination-mappe hvis den ikke eksisterer
            string? destinationDir = Path.GetDirectoryName(destinationPath);
            if (destinationDir is not null)
                Directory.CreateDirectory(destinationDir);

            // Kopi i chunks med progress
            long bytesTransferred = 0;
            string tempPath = destinationPath + ".tmp";

            using (FileStream source = new(item.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true))
            using (FileStream dest = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
            {
                byte[] buffer = new byte[BufferSize];
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await dest.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    bytesTransferred += read;
                    bytesProgress?.Report(bytesTransferred);
                }
            }

            // Atomisk rename: temp → final
            File.Move(tempPath, destinationPath, overwrite: false);

            return new TransferResult
            {
                Succeeded = true,
                DestinationPath = destinationPath,
                BytesTransferred = bytesTransferred,
            };
        }
        catch (OperationCanceledException)
        {
            // Ryd op temp-fil ved cancel
            TryDeleteFile(destinationPath + ".tmp");
            throw; // Re-throw cancellation — det er ikke en fejl, det er brugerens ønske
        }
        catch (Exception ex)
        {
            TryDeleteFile(destinationPath + ".tmp");
            return new TransferResult
            {
                Succeeded = false,
                ErrorCode = ClassifyException(ex),
                ErrorMessage = ex.Message,
                Exception = ex,
            };
        }
    }

    private static BackupErrorCode ClassifyException(Exception ex) => ex switch
    {
        IOException => BackupErrorCode.TransferFailed,
        UnauthorizedAccessException => BackupErrorCode.AccessDenied,
        _ => BackupErrorCode.TransferFailed,
    };

    private static void TryDeleteFile(string path)
    {
        try { File.Delete(path); } catch { /* ignorer cleanup-fejl */ }
    }
}
```

**Vigtige detaljer:**

1. **`.tmp` → rename mønstret** sikrer at en delvist overført fil aldrig ser ud som komplet. Destinations-filen eksisterer enten komplet eller slet ikke.
2. **`OperationCanceledException` re-throws** — det er brugerens eksplicitte ønske om at stoppe. Det er ikke en fejl.
3. **`IOException` (disk fuld, netværksfejl, fil låst)** returneres som struktureret fejl — backup fortsætter med næste fil.
4. **Buffer-størrelse på 80 KB** er en balance mellem RAM-forbrug og I/O-effektivitet.

**Collision-håndtering (CollisionStrategy):**  
Engine beregner `destinationPath` *inden* transfer kallet. Kollision-logikken hører hjemme i engine, ikke i transfer-klassen. Transfer modtager en allerede-besluttet sti og kaster ikke op hvis filen eksisterer (den overskriver med `.tmp` → rename).

**Definition of done for Trin D:**
- En fil kopieres korrekt til destination.
- `.tmp`-filer ryddes op ved fejl og cancel.
- Struktureret `TransferResult` returneres i alle tilfælde.

---

### Trin E: JsonSidecarGenerator

**Hvorfor:**  
Sidecar er sporbarhed. Vi skal vide hvornår en fil blev overført, fra hvad, til hvad, og med hvilken status. Uden sidecar er backup ikke sporbar.

**Sidecar-format (minimal):**  
Sidecar er en `.sidecar.json`-fil ved siden af den overførte fil. For `D:\Backup\2024\IMG_001.jpg` oprettes `D:\Backup\2024\IMG_001.jpg.sidecar.json`.

```json
{
  "schema_version": "1.0",
  "source_path": "C:\\Photos\\2024\\IMG_001.jpg",
  "relative_path": "2024\\IMG_001.jpg",
  "destination_path": "D:\\Backup\\2024\\IMG_001.jpg",
  "source_type": "FileSystem",
  "size_bytes": 4582912,
  "transferred_at_utc": "2024-08-15T14:32:01Z",
  "transfer_status": "Succeeded",
  "session_id": "a1b2c3d4e5f6..."
}
```

Enriched sidecar (efter optional features) kan yderligere indeholde:
```json
{
  "hash_sha256": "e3b0c44298fc1c149...",
  "exif_date_original": "2024-08-15T10:15:00",
  "gps_latitude": 55.6761,
  "gps_longitude": 12.5683,
  "camera_make": "Apple",
  "camera_model": "iPhone 15 Pro",
  "verification_status": "Verified"
}
```

**Opret `Sidecar/JsonSidecarGenerator.cs`:**

```csharp
// namespace BMTP3.Core4.Sidecar
internal sealed class JsonSidecarGenerator : ISidecarGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task CreateAsync(
        BackupItem item,
        TransferResult transferResult,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(transferResult);

        string sidecarPath = item.DestinationPath + ".sidecar.json";
        string tempPath = sidecarPath + ".tmp";

        SidecarData data = new()
        {
            SchemaVersion = "1.0",
            SourcePath = item.SourcePath,
            RelativePath = item.RelativePath,
            DestinationPath = transferResult.DestinationPath ?? string.Empty,
            SizeBytes = item.SizeBytes,
            TransferredAtUtc = DateTimeOffset.UtcNow,
            TransferStatus = transferResult.Succeeded ? "Succeeded" : "Failed",
            ErrorMessage = transferResult.ErrorMessage,
        };

        // Atomisk skrivning: temp → final
        string json = JsonSerializer.Serialize(data, JsonOptions);
        await File.WriteAllTextAsync(tempPath, json, Encoding.UTF8, cancellationToken);
        File.Move(tempPath, sidecarPath, overwrite: true);
    }

    public async Task UpdateAsync(
        string sidecarPath,
        SidecarEnrichment enrichment,
        CancellationToken cancellationToken)
    {
        string tempPath = sidecarPath + ".tmp";

        string existing = await File.ReadAllTextAsync(sidecarPath, cancellationToken);
        Dictionary<string, object?> data = JsonSerializer.Deserialize<Dictionary<string, object?>>(existing)
            ?? new Dictionary<string, object?>();

        // Tilføj enrichment-felter
        if (enrichment.HashSha256 is not null)
            data["hash_sha256"] = enrichment.HashSha256;
        if (enrichment.ExifDateOriginal is not null)
            data["exif_date_original"] = enrichment.ExifDateOriginal;
        // ... osv.

        string updated = JsonSerializer.Serialize(data, JsonOptions);
        await File.WriteAllTextAsync(tempPath, updated, Encoding.UTF8, cancellationToken);
        File.Move(tempPath, sidecarPath, overwrite: true);
    }
}
```

**Definition of done for Trin E:**
- Sidecar-fil oprettes atomisk ved siden af destinationsfilen.
- Sidecar kan opdateres med enrichment-data uden at miste eksisterende felter.
- Fejl ved sidecar-skrivning stopper ikke backup (log + continue).

---

### Trin F: Færdiggør SequentialBackupEngine (det store trin)

**Hvorfor dette er det vigtigste trin:**  
Alt vi har bygget i Trin A–E samles her. SequentialBackupEngine er motoren der kører det hele.

**Det fulde flow i SequentialBackupEngine:**

```
1. Validate(plan)                       → kast BackupPlanValidationException ved fejl
2. OpenAsync(sessionKey, ct)            → hent/opret BackupSessionState
3. session.SetPhase(Scanning)
4. progressTracker.StartDiscovery()
5. await foreach item in scanner.ScanAsync(plan, ct):
       session.AddItem(item)
       progressTracker.ItemDiscovered(item)
       progress.Report(progressTracker.GetSnapshot())
6. session.SetPhase(Transferring)
7. For hvert item i session.GetPendingItems():
   a. cancellationToken.ThrowIfCancellationRequested()
   b. destinationPath = ResolveDestination(item, plan)
   c. item.DestinationPath = destinationPath
   d. Handle SkipExisting (hvis fil eksisterer og plan.SkipExisting)
   e. progressTracker.StartFile(item)
   f. progress.Report(progressTracker.GetSnapshot())
   g. TransferResult result = await fileTransfer.TransferAsync(item, destinationPath, byteProgress, ct)
   h. progressTracker.CompleteFile(item, result)
   i. Hvis result.Succeeded:
          item.Status = Succeeded
          await sidecarGenerator.CreateAsync(item, result, ct)
   j. Ellers:
          item.Status = Failed
          Log fejlen
          Hvis plan.StopOnError → stop loop (set session.Fail(TransferFailed), break)
   k. progress.Report(progressTracker.GetSnapshot())
8. Hvis OperationCanceledException:
       session.Cancel()
9. Ellers:
       session.Complete()
10. return BuildResult(session, progressTracker)
```

**Destination-beregning (ResolveDestination):**

```csharp
private static string ResolveDestination(BackupItem item, BackupPlan plan)
{
    string relativePath = plan.OutputStructure switch
    {
        OutputStructure.PreserveHierarchy => item.RelativePath,
        OutputStructure.Flat => Path.GetFileName(item.RelativePath),
        _ => throw new ArgumentOutOfRangeException(nameof(plan.OutputStructure)),
    };

    string destination = Path.Combine(plan.Destination, relativePath);

    // CollisionStrategy.Rename: tilføj suffix hvis fil eksisterer
    if (plan.CollisionStrategy == CollisionStrategy.Rename && File.Exists(destination))
    {
        string dir = Path.GetDirectoryName(destination) ?? string.Empty;
        string name = Path.GetFileNameWithoutExtension(destination);
        string ext = Path.GetExtension(destination);
        int counter = 1;
        do
        {
            destination = Path.Combine(dir, $"{name}_{counter++}{ext}");
        } while (File.Exists(destination));
    }

    return destination;
}
```

**Cancellation-håndtering:**

Det er vigtigt at du forstår hvad `ThrowIfCancellationRequested` gør og hvad det *ikke* gør. Det kaster en `OperationCanceledException`. Du *fanger* den i en `try/catch` i den ydre engine-loop:

```csharp
try
{
    // hele kørsel
}
catch (OperationCanceledException)
{
    session.Cancel();
    // returnér partial result — vi stopper ikke med at returnere et resultat
}
catch (BackupPlanValidationException ex)
{
    session.Fail(BackupErrorCode.InvalidConfiguration);
    // log
}
catch (Exception ex) when (IsFatal(ex))
{
    session.Fail(BackupErrorCode.TransferFailed); // eller anden passende kode
    // log
}
finally
{
    return BuildResult(session, progressTracker);
}
```

Nøglepunktet: `RunAsync` returnerer **altid** et `BackupResult`, selvom jobbet fejlede eller blev annulleret. `FinalPhase` og `FailureReason` i resultatet fortæller hvad der skete.

**Dry-run:**

```csharp
if (plan.DryRun)
{
    // Simuler succes uden faktisk transfer
    item.Status = BackupItemStatus.Succeeded;
    progressTracker.CompleteFile(item, bytesTransferred: item.SizeBytes ?? 0);
    // Ingen sidecar-skrivning ved dry-run
    continue;
}
```

**Definition of done for Trin F:**
- End-to-end kørsel virker for FS-kilde: scan → transfer → sidecar → result.
- `BackupResult` indeholder korrekte tællere.
- Cancellation giver `BackupPhase.Cancelled` og partial result.
- `StopOnError` virker.
- Dry-run simulerer uden writes.

---

### Trin G: ProgressTracker og BackupProgressSnapshot

**Hvorfor:**  
`IProgress<IBackupProgress>` er det eneste vindue kalderen har ind i hvad der sker. Uden korrekt tracker er UI og CLI blinde.

**Opret `Progress/ProgressTracker.cs`:**

ProgressTracker er en mutable, trådsikker container for løbende statistik. Den eksponerer en snapshot-metode der returnerer et immutabelt billede.

```csharp
// namespace BMTP3.Core4.Progress
internal sealed class ProgressTracker
{
    private BackupPhase currentPhase = BackupPhase.Starting;
    private int directoriesScanned;
    private int filesDiscovered;
    private long bytesTotal;
    private int filesProcessed;
    private int filesSucceeded;
    private int filesSkipped;
    private int filesFailed;
    private long bytesProcessed;

    private readonly ConcurrentDictionary<string, FileProgressEntry> activeFiles = new();

    public void SetPhase(BackupPhase phase)
    {
        Volatile.Write(ref currentPhase, phase); // trådsikker write
    }

    public void ItemDiscovered(BackupItem item)
    {
        Interlocked.Increment(ref filesDiscovered);
        if (item.SizeBytes.HasValue)
            Interlocked.Add(ref bytesTotal, item.SizeBytes.Value);
    }

    public void StartFile(BackupItem item)
    {
        activeFiles[item.Id] = new FileProgressEntry(item.RelativePath, item.SizeBytes, 0);
    }

    public void UpdateFileBytesTransferred(string itemId, long bytes)
    {
        if (activeFiles.TryGetValue(itemId, out FileProgressEntry? entry))
            activeFiles[itemId] = entry with { BytesProcessed = bytes };
    }

    public void CompleteFile(BackupItem item, TransferResult result)
    {
        activeFiles.TryRemove(item.Id, out _);
        Interlocked.Increment(ref filesProcessed);

        if (result.Succeeded)
        {
            Interlocked.Increment(ref filesSucceeded);
            Interlocked.Add(ref bytesProcessed, result.BytesTransferred);
        }
        else
        {
            Interlocked.Increment(ref filesFailed);
        }
    }

    public BackupProgressSnapshot GetSnapshot()
    {
        return new BackupProgressSnapshot
        {
            CurrentPhase = currentPhase,
            DirectoriesScanned = directoriesScanned,
            FilesDiscovered = filesDiscovered,
            BytesTotal = bytesTotal,
            FilesProcessed = filesProcessed,
            FilesSucceeded = filesSucceeded,
            FilesSkipped = filesSkipped,
            FilesFailed = filesFailed,
            BytesProcessed = bytesProcessed,
            ActiveFiles = activeFiles.Values
                .Select(e => new FileProgressSnapshot(e.Path, e.BytesTotal, e.BytesProcessed))
                .ToList(),
        };
    }
}
```

**Opret `Progress/BackupProgressSnapshot.cs`:**

```csharp
// namespace BMTP3.Core4.Progress
internal sealed class BackupProgressSnapshot : IBackupProgress
{
    public BackupPhase CurrentPhase { get; init; }
    public int DirectoriesScanned { get; init; }
    public int FilesDiscovered { get; init; }
    public long BytesTotal { get; init; }
    public int FilesProcessed { get; init; }
    public int FilesSucceeded { get; init; }
    public int FilesSkipped { get; init; }
    public int FilesFailed { get; init; }
    public long BytesProcessed { get; init; }
    public IReadOnlyList<IFileProgress> ActiveFiles { get; init; } = [];
}
```

**Progress-rapportering i engine:**  
Kald `progress.Report(progressTracker.GetSnapshot())` på disse tidspunkter:
1. Når fase skifter.
2. Per item under scan (hvert Nth item, fx hvert 10. for store scans).
3. Per fil under transfer (ved start, under byte-chunks, ved afslutning).

For meget store jobs er det dyrt at rapportere hvert enkelt chunk. Overvej en debounce: rapportér max hvert 100ms via en `Stopwatch`.

**Definition of done for Trin G:**
- `IProgress<IBackupProgress>` modtager konsistente snapshots.
- Tællere er korrekte under concurrent adgang (selv om sequential engine er ét-trådet er det god vane).
- `ActiveFiles` afspejler korrekt hvilke filer der er under overførslen.

---

### Trin H: MTPItemScanner og MTPFileTransfer

**Hvorfor dette er et separat trin:**  
MTP-implementering kræver `MediaDevices.dll` (out-of-repo reference). Logikken er mere kompleks end FS og kræver eksplicit session-håndtering.

**MTP Session Lifecycle:**

En MTP-session er dyr at åbne og følsom over for parallelisme. Livscyklussen er:

```
1. Åbn forbindelse til device: device.Connect()
2. Opret session: device.BeginOpenSession() / device.OpenSession()
3. Scan alle filer: device.EnumerateFiles(...)
4. Transfer filer én ad gangen: device.DownloadFile(...)
5. Luk session: device.CloseSession() / device.Disconnect()
```

Al scan og transfer foregår inden for *én* åben session. Det er afgørende for stabilitet.

**Opret `Scanner/MTP/MTPItemScanner.cs`:**

```csharp
// namespace BMTP3.Core4.Scanner.MTP
internal sealed class MTPItemScanner : IBackupScanner
{
    public async IAsyncEnumerable<BackupItem> ScanAsync(
        BackupPlan plan,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // MediaDevices.MediaDevice bruges via IMediaDevice-abstraction
        using IMediaDevice device = ConnectDevice(plan.Source);

        string root = GetDeviceRoot(plan, device);

        // Stack-baseret traversal (rekursion er ustabil på MTP)
        Stack<string> directories = new();
        directories.Push(root);

        while (directories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string currentDir = directories.Pop();

            IEnumerable<string> subDirs;
            IEnumerable<string> files;

            try
            {
                subDirs = plan.Recursive
                    ? device.EnumerateDirectories(currentDir)
                    : Enumerable.Empty<string>();
                files = device.EnumerateFiles(currentDir);
            }
            catch (COMException ex) when (IsTransientMtpError(ex))
            {
                // Log advarsel om denne mappe og fortsæt med næste
                continue;
            }

            foreach (string subDir in subDirs)
                directories.Push(subDir);

            foreach (string filePath in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string relativePath = GetRelativePath(root, filePath);

                if (!MatchesFilters(relativePath, plan.IncludePatterns, plan.ExcludePatterns))
                    continue;

                long? size = TryGetFileSize(device, filePath);

                yield return new BackupItem
                {
                    Id = Guid.NewGuid().ToString("N"),
                    SourcePath = filePath,
                    RelativePath = relativePath,
                    SizeBytes = size,
                    ModifiedAt = TryGetModifiedAt(device, filePath),
                };

                await Task.Yield();
            }
        }
    }

    private static bool IsTransientMtpError(COMException ex)
    {
        // MTP-fejlkoder der er transiente (timeout, busy)
        return ex.ErrorCode is 0x800700AA or 0x800700B7;
    }
}
```

**Om COMException-håndtering:**  
MTP drives via COM-interface på Windows. `COMException` kan kastes ved:
- Enhed disconnectet
- Timeout (enhed er busy)
- Ukendt MTP-fejlkode

Skelne mellem *transiente* fejl (timeout — prøv igen) og *fatale* fejl (enhed forsvundet — stop job).

**MTP Retry Pattern:**

```csharp
private static async Task<TransferResult> TransferWithRetry(
    Func<Task<TransferResult>> transferAction,
    int maxAttempts = 3,
    CancellationToken cancellationToken = default)
{
    int[] delaysMs = [1000, 2000, 4000]; // exponential backoff

    for (int attempt = 0; attempt < maxAttempts; attempt++)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TransferResult result = await transferAction();

        if (result.Succeeded)
            return result;

        if (attempt < maxAttempts - 1)
            await Task.Delay(delaysMs[attempt], cancellationToken);
    }

    return new TransferResult { Succeeded = false, ErrorCode = BackupErrorCode.TransferFailed };
}
```

**Definition of done for Trin H:**
- MTP scanner enumererer filer korrekt fra enhed.
- Transiente MTP-fejl under scan logges, scan fortsætter.
- Fatal disconnect sætter session til Failed.
- MTP transfer er sekventiel og bruger retry/backoff.

---

### Trin I: DependencyInjection / ServiceCollectionExtensions

**Hvorfor:**  
DI-registration er det sted hvor alle komponenter samles. Uden korrekt DI er engine ikke brugbar fra Consoles-projektet.

**Opret `DependencyInjection/ServiceCollectionExtensions.cs`:**

```csharp
// namespace BMTP3.Core4.DependencyInjection
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBMTP3Core4(
        this IServiceCollection services,
        Action<Core4Options>? configure = null)
    {
        Core4Options options = new();
        configure?.Invoke(options);

        // Session state (in-memory er default)
        services.AddSingleton<IBackupSessionStateStore, InMemoryBackupSessionStateStore>();

        // Progress
        services.AddTransient<ProgressTracker>();

        // Scanner
        services.AddTransient<FilesystemItemScanner>();
        services.AddTransient<MTPItemScanner>();

        // Transfer
        services.AddTransient<FilesystemFileTransfer>();
        services.AddTransient<MTPFileTransfer>();

        // Sidecar
        services.AddTransient<ISidecarGenerator, JsonSidecarGenerator>();

        // Engines
        services.AddTransient<SequentialBackupEngine>();
        services.AddTransient<LimitedParallelBackupEngine>();
        services.AddTransient<BackupEngineFactory>();

        // IBackupEngine binder til factory-pattern
        // Kaldere der ønsker IBackupEngine resolver via BackupEngineFactory
        services.AddTransient<IBackupEngine>(sp =>
        {
            // Default: sequential engine
            // Kaldere kan bruge BackupEngineFactory direkte for plan-baseret valg
            return sp.GetRequiredService<SequentialBackupEngine>();
        });

        return services;
    }
}
```

**Hvad `Core4Options` kan indeholde:**  
Konfigurationsklasse til at tilpasse Core4 ved registration:

```csharp
public sealed class Core4Options
{
    public int DefaultParallelism { get; set; } = 4;
    public int MtpRetryCount { get; set; } = 3;
    public bool UseExifToolFallback { get; set; } = true;
    public string? ExifToolPath { get; set; }
}
```

**Bruger i Consoles-projektet:**  
I `Startup/Configurations/Core4ServiceSetup.cs` (eller tilsvarende):

```csharp
services.AddBMTP3Core4(options =>
{
    options.DefaultParallelism = 4;
    options.UseExifToolFallback = true;
    options.ExifToolPath = Path.Combine(AppContext.BaseDirectory, "bin", "exiftool.exe");
});
```

**Definition of done for Trin I:**
- Solution bygger.
- Alle services er registreret.
- Ingen cirkulære afhængigheder.

---

### Trin J: Optional features (Tier 2)

Optional features aktiveres via `BackupPlan.EnableHashing`, `EnableMetadata`, `EnableVerification`, `EnableTimestampCorrection`. De kører *efter* transfer og sidecar-oprettelse.

**Flow for optional features:**

```
Per item (efter succesfuld transfer):

1. Hvis plan.EnableHashing:
       HashResult hash = await hasher.ComputeAsync(destinationPath, ct)
       enrichment.HashSha256 = hash.Sha256
       
2. Hvis plan.EnableMetadata:
       ExtractedMetadata? meta = await metadataReader.ReadAsync(destinationPath, ct)
       enrichment.ExifDateOriginal = meta?.DateTimeOriginal
       enrichment.CameraMake = meta?.Make
       enrichment.GpsLatitude = meta?.GpsLatitude
       // ... osv.
       
3. Hvis plan.EnableVerification:
       VerificationResult verify = await verifier.VerifyAsync(sourcePath, destinationPath, ct)
       enrichment.VerificationStatus = verify.Matched ? "Verified" : "Mismatch"
       
4. Hvis plan.EnableTimestampCorrection:
       DateTimeOffset? timestamp = meta?.DateTimeOriginal ?? item.ModifiedAt
       if (timestamp.HasValue)
           await timestampCorrector.CorrectAsync(destinationPath, timestamp.Value, ct)

5. Hvis enrichment har noget:
       await sidecarGenerator.UpdateAsync(sidecarPath, enrichment, ct)
```

**Timestamp-præcedens (vigtigt):**

For `IMetadataReader` gælder denne prioritetsrækkefølge:

1. EXIF `DateTimeOriginal` (hvornår kameraet tog billedet)
2. EXIF `CreateDate` / XMP `CreateDate`
3. QuickTime creation date (for video; normaliser til UTC med offset-håndtering)
4. Filsystem `LastWriteTimeUtc`
5. Filsystem `CreationTimeUtc`

Ugyldig dato (epoch 1970-01-01, år > 2030+5, tomt felt) → gå videre til næste kandidat.

**MetadataExtractor integration:**

```csharp
// namespace BMTP3.Core4.Features.Metadata
internal sealed class MetadataExtractorReader : IMetadataReader
{
    public Task<ExtractedMetadata?> ReadAsync(string filePath, CancellationToken cancellationToken)
    {
        IReadOnlyList<MetadataExtractor.Directory> dirs =
            ImageMetadataReader.ReadMetadata(filePath);

        // Tjek EXIF DateTimeOriginal
        ExifSubIfdDirectory? exif = dirs.OfType<ExifSubIfdDirectory>().FirstOrDefault();
        DateTime? dateOriginal = exif?.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime d) == true ? d : null;

        // Tjek QuickTime
        QuickTimeMovieHeaderDirectory? qt = dirs.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
        DateTime? qtDate = qt?.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out DateTime q) == true ? q : null;

        // Vælg bedst tilgængelig dato
        DateTimeOffset? bestDate = SelectBestDate(dateOriginal, qtDate, ...);

        return Task.FromResult<ExtractedMetadata?>(new ExtractedMetadata
        {
            DateTimeOriginal = bestDate,
            Make = exif?.GetString(ExifDirectoryBase.TagMake),
            Model = exif?.GetString(ExifDirectoryBase.TagModel),
            // ...
        });
    }
}
```

**ExifTool fallback:**

Hvis `MetadataExtractor` returnerer null (dvs. filen er ukendt format), forsøges `exiftool.exe`:

```csharp
// namespace BMTP3.Core4.Features.Metadata
internal sealed class ExifToolMetadataReader : IMetadataReader
{
    private readonly string exifToolPath;

    public async Task<ExtractedMetadata?> ReadAsync(string filePath, CancellationToken cancellationToken)
    {
        ProcessStartInfo psi = new(exifToolPath, $"-json -q \"{filePath}\"")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using Process? process = Process.Start(psi);
        if (process is null) return null;

        string json = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        // Parse JSON array, extract DateTimeOriginal
        // ...

        return ParseExifToolOutput(json);
    }
}
```

**Definition of done for Trin J:**
- Hvert optional feature kan aktiveres uafhængigt.
- Fejler et feature, fortsætter backup.
- Sidecar opdateres atomisk med enrichment-data.

---

### Trin K: LimitedParallelBackupEngine (Tier 2)

**Hvorfor:** Performance for store FS-backup. Sequential er korrekt men langsom for fx 50.000 billeder.

**Design:**  
Brug en producer/consumer med `SemaphoreSlim` som throttle:

```csharp
internal sealed class LimitedParallelBackupEngine : IBackupEngine
{
    private readonly IBackupScanner scanner;
    private readonly IFileTransfer fileTransfer;
    private readonly ISidecarGenerator sidecarGenerator;
    private readonly ProgressTracker progressTracker;
    private readonly IBackupSessionStateStore sessionStateStore;

    public async Task<BackupResult> RunAsync(
        BackupPlan plan,
        IProgress<IBackupProgress>? progress,
        CancellationToken cancellationToken)
    {
        int dop = plan.MaxDegreeOfParallelism ?? Environment.ProcessorCount;
        SemaphoreSlim semaphore = new(dop, dop);

        List<Task> transferTasks = new();
        List<Exception> errors = new();

        await foreach (BackupItem item in scanner.ScanAsync(plan, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await semaphore.WaitAsync(cancellationToken);

            Task task = TransferItemAsync(item, plan, semaphore, errors, cancellationToken);
            transferTasks.Add(task);
        }

        await Task.WhenAll(transferTasks);

        return BuildResult(...);
    }

    private async Task TransferItemAsync(
        BackupItem item,
        BackupPlan plan,
        SemaphoreSlim semaphore,
        List<Exception> errors,
        CancellationToken cancellationToken)
    {
        try
        {
            string destinationPath = ResolveDestination(item, plan);
            item.DestinationPath = destinationPath;

            TransferResult result = await fileTransfer.TransferAsync(
                item, destinationPath, null, cancellationToken);

            if (result.Succeeded)
            {
                item.Status = BackupItemStatus.Succeeded;
                await sidecarGenerator.CreateAsync(item, result, cancellationToken);
            }
            else
            {
                item.Status = BackupItemStatus.Failed;
            }
        }
        finally
        {
            semaphore.Release();
        }
    }
}
```

**Vigtige begrænsninger:**

- Parallel engine bruges **kun** til `BackupSourceType.FileSystem`.
- Standard-DoP: `Math.Min(plan.MaxDegreeOfParallelism ?? 4, Environment.ProcessorCount)`.
- Sidecar-skrivning er altid atomisk — thread-safe fordi `.tmp` er unik per fil.
- `ProgressTracker` skal være trådsikker (brug `Interlocked` og `Volatile`).

---

## Del 6: Fejlhåndtering — komplet taksonomitabel

Denne tabel fortæller dig præcist hvad der skal ske i hvert fejlscenarie:

| Fejltype | Klasse/sted | Default-adfærd | StopOnError=true |
|----------|------------|----------------|-----------------|
| Ugyldig BackupPlan | `Validate()` | Kast `BackupPlanValidationException` | Samme |
| Kilde-mappe eksisterer ikke | Scanner start | Sæt `FailureReason=SourceNotFound`, returnér result | Samme |
| Per-fil scan-fejl (UnauthorizedAccess) | Scanner loop | Log, skip fil | Samme |
| Per-fil transfer-fejl (IOException) | Transfer | `item.Status=Failed`, log, fortsæt | Stop job |
| Per-fil transfer-fejl (disk fuld) | Transfer | `FailureReason=InsufficientDiskSpace`, stop job | Stop job |
| MTP disconnect (transient) | MTPFileTransfer | Retry 3× med backoff | Retry, derefter stop |
| MTP disconnect (permanent) | MTPFileTransfer | `FailureReason=MediaDeviceDisconnected`, stop job | Samme |
| Sidecar-skrivning fejler | JsonSidecarGenerator | Log fejl, **fortsæt backup** | Fortsæt |
| Optional feature fejler | Feature-klasse | Log fejl, **fortsæt backup** | Fortsæt |
| OperationCanceledException | Engine try/catch | `session.Cancel()`, returnér partial result | Samme |
| OutOfMemoryException | Top-level | Propagér op (uhandterbar) | Samme |

---

## Del 7: Test-strategi

God test-dækning kræver at alle services er injicerbare. Brug disse mønstre:

### 7.1 TestItemScanner

```csharp
// Bruges i tests til at injicere foruddefinerede items
internal sealed class TestItemScanner : IBackupScanner
{
    private readonly IReadOnlyList<BackupItem> items;

    public TestItemScanner(params BackupItem[] items)
        => this.items = items;

    public async IAsyncEnumerable<BackupItem> ScanAsync(
        BackupPlan plan,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (BackupItem item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield();
        }
    }
}
```

### 7.2 TestFileTransfer

```csharp
internal sealed class TestFileTransfer : IFileTransfer
{
    private readonly bool shouldSucceed;
    private readonly BackupErrorCode? errorCode;

    public TestFileTransfer(bool shouldSucceed = true, BackupErrorCode? errorCode = null)
    {
        this.shouldSucceed = shouldSucceed;
        this.errorCode = errorCode;
    }

    public Task<TransferResult> TransferAsync(
        BackupItem item,
        string destinationPath,
        IProgress<long>? bytesProgress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TransferResult result = shouldSucceed
            ? new TransferResult { Succeeded = true, DestinationPath = destinationPath, BytesTransferred = item.SizeBytes ?? 0 }
            : new TransferResult { Succeeded = false, ErrorCode = errorCode ?? BackupErrorCode.TransferFailed };

        return Task.FromResult(result);
    }
}
```

### 7.3 Integrationstests

En integrations-test kører end-to-end med rigtige filer men kontrollerede testdata:

```csharp
[Fact]
[Trait("Category", "Integration")]
public async Task RunAsync_SuccessfulTransfer_CreatesSidecarFile()
{
    // Arrange
    using TempDirectory source = TempDirectory.Create();
    using TempDirectory destination = TempDirectory.Create();
    source.CreateFile("photo.jpg", content: "fake jpeg data");

    BackupPlan plan = new()
    {
        Name = "Test",
        Source = source.Path,
        Destination = destination.Path,
        SourceType = BackupSourceType.FileSystem,
        OutputStructure = OutputStructure.PreserveHierarchy,
        CollisionStrategy = CollisionStrategy.Skip,
        Recursive = false,
    };

    SequentialBackupEngine engine = CreateEngine(plan);

    // Act
    BackupResult result = await engine.RunAsync(plan, progress: null, CancellationToken.None);

    // Assert
    Assert.Equal(BackupPhase.Completed, result.FinalPhase);
    Assert.Equal(1, result.FilesSucceeded);
    Assert.True(File.Exists(Path.Combine(destination.Path, "photo.jpg")));
    Assert.True(File.Exists(Path.Combine(destination.Path, "photo.jpg.sidecar.json")));
}
```

---

## Del 8: Prioriteret arbejdsliste (hvad du gør i hvilken rækkefølge)

For den udvikler der starter i dag er dette den konkrete rækkefølge:

**Tier 1 — Core engine virker end-to-end (FS):**

1. `Transfer/IFileTransfer.cs` — opret interface
2. `Transfer/TransferResult.cs` — opret record
3. `Sidecar/ISidecarGenerator.cs` — opret interface
4. `Sidecar/SidecarData.cs` — opret data-klasse
5. `Sidecar/JsonSidecarGenerator.cs` — implementer
6. `Progress/ProgressTracker.cs` — implementer
7. `Progress/BackupProgressSnapshot.cs` — implementer
8. `Scanner/Filesystem/FilesystemItemScanner.cs` — implementer
9. `Transfer/Filesystem/FilesystemFileTransfer.cs` — implementer
10. `Engine/Sequential/SequentialBackupEngine.cs` — tilføj dependencies, implementer komplet flow
11. `Engine/BackupEngineFactory.cs` — implementer factory
12. `DependencyInjection/ServiceCollectionExtensions.cs` — registrer alle services
13. Slet eller konverter `Engine/BackupEngine.cs` (den er tom og forvirrende)

**Tier 1 — Verificer med tests:**

14. Skriv unit tests for `FilesystemItemScanner`
15. Skriv unit tests for `FilesystemFileTransfer`
16. Skriv unit tests for `JsonSidecarGenerator`
17. Skriv integrationstest: end-to-end FS backup

**Tier 2 — MTP og optional features:**

18. `Scanner/MTP/MTPItemScanner.cs`
19. `Transfer/MTP/MTPFileTransfer.cs`
20. `Features/Metadata/MetadataExtractorReader.cs`
21. `Features/Metadata/ExifToolMetadataReader.cs` (fallback)
22. `Features/Hashing/FileHasher.cs`
23. `Features/Verification/FileIntegrityVerifier.cs`
24. `Features/Timestamp/FileTimestampCorrector.cs`
25. Opdater `SequentialBackupEngine` med optional features pipeline

**Tier 3 — Parallel FS og performance:**

26. `Engine/LimitedParallel/LimitedParallelBackupEngine.cs`
27. Opdater `BackupEngineFactory` til at vælge engine korrekt

---

## Del 9: Forhold til øvrige dokumenter

| Dokument | Brug det til |
|----------|-------------|
| `CORE4_MASTER_SYNTHESIS.md` | Endelig beslutningstekst. Konflikt? Følg dette. |
| `CORE4_IMPLEMENTATION_GUIDE_DA.md` | **Denne guide** — operationelt styredokument |
| `Core4_Master_Architecture.md` | Dyb teknisk reference, MTP-session detaljer, edge cases |
| `Lessons_Learned_Comparison.md` | Historisk baggrund for designbeslutninger |

**Prioritet ved konflikt:** CORE4_MASTER_SYNTHESIS.md → denne guide → Core4_Master_Architecture.md.

---

*Guide-version 3.0 — baseret på komplet analyse af BMTP3.Core4 skeleton-kode og alle læringsdokumenter.*
