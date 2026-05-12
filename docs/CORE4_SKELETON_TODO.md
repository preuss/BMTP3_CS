# Core4 – Skeleton Implementation TODO
## Detaljeret fil-for-fil guide til en (begynder) udvikler

> **Hvad er dette dokument?**
> En præcis, handlingsrettet liste over HVAD der skal implementeres, HVORFOR det er nødvendigt,
> og HVORDAN det hænger sammen. Hvert punkt angiver fil, formål, signatur og specifikke regler.
>
> **Baseret på:** CORE4_PLAN_en.md, CORE4_ARCHITECTURE_en.md, CORE3_PLAN.md,
> CORE_LEARNING.md, CORE2_LEARNING.md, CORE3_LEARNING.md, CORE4_LEARNING.md

---

## LÆSEVEJLEDNING FOR BEGYNDERE

### Hvad betyder de forskellige symboler?
- `[ ]` = Ikke startet
- `[x]` = Fuldt implementeret
- `[~]` = Skeleton oprettet, men kroppen mangler
- ⚠️ = Kritisk rettelse fra Core3-bug eller Core2-fejl — MÅ implementeres korrekt
- 🚫 = Eksplicit forbud — lær af Core2's fejl
- 💡 = Begynder-tip — forklaring af "hvorfor"

### Hvad er arkitekturen kort fortalt?

```
BackupPlan (input fra bruger)
    ↓
BackupEngineFactory (vælger strategi)
    ↓                    ↓
SequentialBackupEngine   LimitedParallelBackupEngine
(MTP-enheder)            (Filsystem ONLY)
    ↓
Scan → Transfer → GenerateSidecar → [Hash] → [Metadata] → [Verify] → [Timestamps]
    ↓
BackupJobResult (output til bruger)
```

### Hvad er lagene?

```
Api/              → Interfaces og kontrakter (hvad der SKAL ske)
Models/           → Dataobjekter (BackupPlan, BackupItem, osv.)
Scanner/          → Finder filer i kilde (FilesystemItemScanner, MtpItemScanner)
Transfer/         → Kopierer filer til destination
Sidecar/          → Opretter metadata-filer ved siden af de kopierede filer
Hashing/          → Beregner fil-fingeraftryk (SHA256 osv.) – valgfrit
Metadata/         → Læser EXIF og filattributter – valgfrit
Verification/     → Bekræfter at filer er kopieret korrekt – valgfrit
Timestamps/       → Gendanner originale timestamps – valgfrit
Engine/           → Orkestrerer hele flowet
Progress/         → Sporer fremskridt thread-safe
DependencyInjection/ → Registrerer alle services
```

---

## NUVÆRENDE STATUS

### Fuldt implementerede filer (rør ikke ved dem)

| Fil | Status |
|-----|--------|
| `Models/Enums/BackupItemStatus.cs` | ✅ Komplet |
| `Models/Enums/CollisionStreategy.cs` | ✅ Komplet *(stavefejl i filnavn – se TODO A1)* |
| `Models/Enums/OutputStructure.cs` | ✅ Komplet |
| `Models/BackupItem.cs` | ✅ Delvist *(se TODO A3 – mangler felter)* |
| `Api/IFileProgress.cs` | ✅ Komplet |
| `Scanner/IBackupScanner.cs` | ✅ Komplet |
| `Helpers/Guard.cs` | ✅ Komplet |
| `Engine/Validation/BackupPlanValidator.cs` | ✅ Komplet |
| `Engine/Validation/BackupPlanValidationException.cs` | ✅ Antages komplet |
| `Engine/State/BackupSessionState.cs` | ✅ Komplet |
| `Engine/State/BackupSessionStateKey.cs` | ✅ Komplet |
| `Engine/State/BackupSessionStateKeyFactory.cs` | ✅ Komplet |
| `Engine/State/IBackupSessionStateStore.cs` | ✅ Komplet |
| `Engine/State/InMemoryBackupSessionStateStore.cs` | ✅ Komplet |

### Tomme skeletons (skal implementeres)

| Fil | Status |
|-----|--------|
| `Api/IBackupEngine.cs` | ✅ Interface OK, men returntype er `BackupResult` → skal være `BackupJobResult` |
| `Api/IBackupProgress.cs` | ✅ Delvist – mangler beregnede properties |
| `Models/BackupPlan.cs` | ✅ Delvist – bruger `Source` (ét felt) i stedet for `SourceDirectory?` + `DeviceId?` |
| `Models/BackupResult.cs` | ✅ Delvist – mangler felter, bør omdøbes |
| `Models/Enums/BackupPhase.cs` | ✅ Delvist – mangler faser |
| `Models/Enums/BackupErrorCode.cs` | ✅ Delvist – mangler numeriske koder |
| `Engine/BackupEngine.cs` | `[~]` Har kun kommentarer – ingen implementering |
| `Engine/Sequential/SequentialBackupEngine.cs` | `[~]` Scan-loop OK, resten mangler |
| `Engine/LimitedParallel/LimitedParallelBackupEngine.cs` | `[~]` Tom klasse |
| `Engine/Parallel/ParallelBackupEngine.cs` | `[~]` Tom – sandsynligvis ikke nødvendig, se TODO |
| `Engine/State/BackupSessionStore.cs` | `[~]` Tom – sandsynligvis ikke nødvendig |
| `Tests/Fakes/FakeBackupScanner.cs` | `[~]` Tom |
| `Tests/Fakes/FakeFileTransfer.cs` | `[~]` Tom |
| `Tests/Fakes/FakeSidecarGenerator.cs` | `[~]` Tom |

---

## SEKTION A: RETTELSER I EKSISTERENDE FILER

---

### `[ ]` A1 – Rename `Models/Enums/CollisionStreategy.cs`
**Hvad:** Filnavn har stavefejl (`Streategy` i stedet for `Strategy`).
**Enum-navnet** er korrekt (`CollisionStrategy`). Kun filnavnet er forkert.
**Handling:** Beslut om du vil rename filen eller lade det stå. Vigtigst: vær konsekvent.
*Tip: I Visual Studio kan du rename via højreklik → Rename.*

---

### `[ ]` A2 – Opdatér `Models/Enums/BackupPhase.cs`
**Hvad:** Mangler faser som arkitekturddokumentet kræver.

Nuværende:
```
Starting, Scanning, Transferring, Completed, Cancelled, Failed
```

Skal være (fra CORE4_PLAN_en.md):
```csharp
public enum BackupPhase
{
    NotStarted = 0,
    Initializing = 1,
    Scanning = 2,
    Transferring = 3,
    GeneratingSidecars = 4,      // NY – sidecar-fasen er separat
    Hashing = 5,                 // NY – valgfri hash-fase
    MetadataExtraction = 6,      // NY – valgfri metadata-fase
    Verification = 7,            // NY – valgfri verify-fase
    TimestampCorrection = 8,     // NY – valgfri timestamp-fase
    Completed = 100,
    Failed = 101,
    Cancelled = 102
}
```
💡 *Numeriske værdier (100/101/102) bruges, fordi Completed/Failed/Cancelled er "terminal states" – nemmere at sortere og vise i UI.*

---

### `[ ]` A3 – Opdatér `Models/Enums/BackupErrorCode.cs`
**Hvad:** Mangler numeriske koder og flere fejltyper.

Nuværende koder mangler: specifik kategorisering og numeriske ranges.

Skal inkludere (fra CORE4_PLAN_en.md):
```csharp
public enum BackupErrorCode
{
    // Konfiguration (0-999)
    InvalidConfiguration = 100,
    SourceAndDestinationSame = 101,

    // Scan-fejl (1000-1999)
    ScanDirectoryNotFound = 1001,
    ScanAccessDenied = 1002,
    ScanPathInvalid = 1003,
    ScanIOError = 1004,

    // Transfer-fejl (2000-2999)
    TransferSourceNotFound = 2001,
    TransferDestinationFull = 2002,
    TransferAccessDenied = 2003,
    TransferIOError = 2004,
    TransferTimeout = 2005,

    // MTP-specifik (3000-3999)
    MTPDeviceNotFound = 3001,
    MTPDeviceDisconnected = 3002,
    MTPSessionTimeout = 3003,
    MTPAuthenticationFailed = 3004,

    // Feature-fejl (4000-4999)
    HashComputationFailed = 4001,
    MetadataExtractionFailed = 4002,
    VerificationFailed = 4003,
    TimestampCorrectionFailed = 4004,

    // System (9000+)
    OutOfMemory = 9001,
    DiskFull = 9002,
    UserCancelled = 9003,
    Unknown = 9999
}
```
💡 *Numeriske ranges gør det nemt for en bruger/log at se "det er en MTP-fejl" bare ved at kigge på tallet.*

---

### `[ ]` A4 – Opdatér `Models/BackupPlan.cs`
**Hvad:** Bruger ét `Source`-felt, men arkitekturen kræver separate felter for filsystem vs. MTP-enhed. `BackupEngineFactory` bruger `DeviceId` til at vælge engine.

Udskift:
```csharp
public BackupSourceType SourceType { get; init; }
public string Source { get; init; } = string.Empty;
```

Med:
```csharp
// Kilde – præcis ÉT af disse er udfyldt
public string? SourceDirectory { get; init; }   // F.eks. "C:\Fotos"
public string? DeviceId { get; init; }           // F.eks. "iPhone (12345)"

// Afledt kildetype (beregnet fra ovenstående)
public BackupSourceType SourceType =>
    DeviceId != null ? BackupSourceType.MediaDevice : BackupSourceType.FileSystem;
```

Tilføj desuden manglende felter:
```csharp
// Fejlhåndteringsstrategi
public ErrorHandlingStrategy TransferErrorStrategy { get; init; } = ErrorHandlingStrategy.SkipOnError;
public ErrorHandlingStrategy FeatureErrorStrategy { get; init; } = ErrorHandlingStrategy.SkipOnError;

// Timeout
public TimeSpan OperationTimeout { get; init; } = TimeSpan.FromSeconds(60);

// Sidecar format
public SidecarFormat SidecarFormat { get; init; } = SidecarFormat.Json;
```

⚠️ `BackupEngineFactory` afhænger af `DeviceId != null` til at vælge `SequentialBackupEngine`.

---

### `[ ]` A5 – Opdatér `Models/BackupResult.cs` → omdøb til `BackupJobResult`
**Hvad:** Arkitekturen kalder den `BackupJobResult`. Nuværende `BackupResult` mangler adskillige felter.

Tilføj/omdøb til:
```csharp
public sealed record BackupJobResult
{
    // Identifikation
    public string BackupId { get; init; } = Guid.NewGuid().ToString("N");
    public string JobName { get; init; } = string.Empty;

    // Status
    public BackupJobStatus Status { get; init; }          // NY
    public BackupPhase FinalPhase { get; init; }
    public BackupErrorCode? FailureReason { get; init; }

    // Tid
    public DateTime StartTime { get; init; }               // NY
    public DateTime EndTime { get; init; }                 // NY
    public TimeSpan Duration => EndTime - StartTime;       // Beregnet

    // Scanning
    public int DirectoriesScanned { get; init; }
    public int FilesDiscovered { get; init; }
    public long BytesTotal { get; init; }

    // Behandling
    public int FilesProcessed { get; init; }
    public int FilesSucceeded { get; init; }
    public int FilesSkipped { get; init; }
    public int FilesFailed { get; init; }
    public long BytesProcessed { get; init; }

    // Fejl
    public IReadOnlyList<BackupError> Errors { get; init; } = [];  // NY

    // Performance
    public double AvgTransferSpeedMBps { get; init; }      // NY

    // Afledte
    public bool IsSuccess => Status == BackupJobStatus.Completed;
    public bool IsPartialSuccess => FilesSucceeded > 0 && FilesFailed > 0;
}
```

---

### `[ ]` A6 – Opdatér `Api/IBackupEngine.cs`
**Hvad:** Returntype bruger `BackupResult` – skal bruge `BackupJobResult`.

```csharp
Task<BackupJobResult> RunAsync(
    BackupPlan plan,
    IProgress<IBackupProgress>? progress,
    CancellationToken cancellationToken);
```

---

### `[ ]` A7 – Opdatér `Api/IBackupProgress.cs`
**Hvad:** Mangler beregnede properties og nuværende fil-detaljer.

Tilføj til interfacet:
```csharp
// Nuværende fil (til visning)
string? CurrentFilePath { get; }
long CurrentFileBytes { get; }
long CurrentFileBytesProcessed { get; }

// Timing
long ElapsedMilliseconds { get; }

// Beregnede metrics (implementeres i BackupProgress-recorden)
double PercentageComplete { get; }
double BytesPerSecond { get; }
TimeSpan EstimatedTimeRemaining { get; }
```

⚠️ FIX fra Core3 Bug #4: BytesTransferred var defineret men ikke implementeret i Core3. Her SKAL alle disse properties implementeres rigtigt.

---

### `[ ]` A8 – Opdatér `Models/BackupItem.cs`
**Hvad:** Mangler felter til pipeline-berigelse (resultater fra transfer, hashing, metadata osv.)

Tilføj:
```csharp
// Fejl samlet op undervejs
public List<BackupError>? Errors { get; set; }

// Resultater fra hvert pipeline-trin
public TransferResult? TransferResult { get; set; }
public Dictionary<string, string>? Hashes { get; set; }     // algortime → hex-streng
public ExtractedMetadata? ExtractedMetadata { get; set; }
public VerificationResult? VerificationResult { get; set; }
```

💡 *BackupItem er intern. Disse felter er "enrichment" – fyldes ud ét ad gangen efterhånden som hvert pipeline-trin kører.*

---

## SEKTION B: NYE ENUMS OG KONTRAKTER

---

### `[ ]` B1 – `Models/Enums/BackupJobStatus.cs`
**Hvad:** Samlet status for hele backup-jobbet (ikke per fil).
```csharp
public enum BackupJobStatus
{
    NotStarted,
    Running,
    Completed,           // Alt lykkedes
    PartialSuccess,      // Nogle filer fejlede, men jobbet fortsatte
    Failed,              // Kritisk fejl – jobbet stoppede
    Cancelled,           // Brugeren annullerede
    DryRunCompleted      // Dry-run fuldført (ingen filer kopieret)
}
```

---

### `[ ]` B2 – `Models/Enums/ErrorHandlingStrategy.cs`
**Hvad:** Styrer hvad der sker når en fejl opstår i et bestemt trin.
```csharp
public enum ErrorHandlingStrategy
{
    StopOnError,   // Stop hele backuppen ved første fejl
    SkipOnError,   // Log fejlen, spring filen over, fortsæt
    RetryOnError   // Prøv N gange før du springer over
}
```
⚠️ FIX fra Core3 Design Flaw #1: Fejlhåndtering var implicit og ikke konfigurerbar.

---

### `[ ]` B3 – `Models/Enums/SidecarFormat.cs`
```csharp
public enum SidecarFormat
{
    Json,   // Opretter .json sidecar-filer
    Xml     // Opretter .xml sidecar-filer
}
```

---

### `[ ]` B4 – `Models/Enums/HashType.cs`
**Hvad:** Understøttede hash-algoritmer. Bruges i `BackupPlan.HashTypes`.
```csharp
public enum HashType
{
    SHA2_256,            // SHA-256 (mest brugt, hurtig)
    SHA2_512,            // SHA-512 (stærkere)
    SHA3_256_FIPS202,    // SHA3-256 FIPS variant
    SHA3_512_FIPS202,    // SHA3-512 FIPS variant
    SHA3_256_KECCAK,     // SHA3-256 Keccak variant
    SHA3_512_KECCAK,     // SHA3-512 Keccak variant
    BLAKE3_256,          // BLAKE3-256 (moderne, hurtig)
    BLAKE3_512,          // BLAKE3-512
    MD5_128              // MD5 (kun legacy-kompatibilitet)
}
```
⚠️ FIX fra Core3 Design Flaw #2: I Core3 var 3 hash-typer altid tvunget. Her er hashing slået fra som standard (`BackupPlan.HashTypes = null` = ingen hashing).

---

### `[ ]` B5 – `Api/IBackupItem.cs`
**Hvad:** Offentlig kontrakt for en fil der flyder igennem pipeline.

💡 *`BackupItem` (intern klasse) skal implementere dette interface, så engine og tests kan bruge interfacet i stedet for den konkrete klasse.*

```csharp
public interface IBackupItem
{
    string Id { get; }
    string SourcePath { get; }
    string RelativePath { get; }        // Relativ sti fra kildens rod
    long? SizeBytes { get; }
    DateTimeOffset? ModifiedAt { get; }

    // Mutable – opdateres under pipeline-kørsel
    BackupItemStatus Status { get; set; }
    string? DestinationPath { get; set; }
    List<BackupError>? Errors { get; set; }
    TransferResult? TransferResult { get; set; }
    Dictionary<string, string>? Hashes { get; set; }
    ExtractedMetadata? ExtractedMetadata { get; set; }
    VerificationResult? VerificationResult { get; set; }
}
```

Sørg for at `BackupItem` implementerer `IBackupItem`.

---

### `[ ]` B6 – `Api/BackupProgress.cs`
**Hvad:** Immutable record der implementerer `IBackupProgress`. Skabt af `ProgressTracker.GetSnapshot()`.

```csharp
public sealed record BackupProgress(
    int DirectoriesScanned,
    int FilesDiscovered,
    long BytesTotal,
    int FilesProcessed,
    int FilesSucceeded,
    int FilesFailed,
    int FilesSkipped,
    long BytesProcessed,
    string? CurrentFilePath,
    long CurrentFileBytes,
    long CurrentFileBytesProcessed,
    BackupPhase CurrentPhase,
    long ElapsedMilliseconds
) : IBackupProgress
{
    // Beregnede properties
    public double PercentageComplete =>
        FilesDiscovered > 0 ? (double)FilesProcessed / FilesDiscovered * 100.0 : 0;

    public double BytesPerSecond =>
        ElapsedMilliseconds > 0 ? (BytesProcessed * 1000.0) / ElapsedMilliseconds : 0;

    public TimeSpan EstimatedTimeRemaining =>
        BytesPerSecond > 0
            ? TimeSpan.FromSeconds((BytesTotal - BytesProcessed) / BytesPerSecond)
            : TimeSpan.Zero;
}
```

---

### `[ ]` B7 – `Api/IProgressNotifier.cs`
**Hvad:** Real-time events til UI/CLI om hvad der sker under backup.

💡 *Forskellen fra `IProgress<IBackupProgress>`: IProgress rapporterer et snapshot hvert 500ms. IProgressNotifier sender events præcist når noget sker (fil startet, fil fejlede, fase skiftet).*

```csharp
public interface IProgressNotifier
{
    void OnPhaseChanged(BackupPhase newPhase, BackupPhase previousPhase);
    void OnFileStarted(IBackupItem item);
    void OnFileCompleted(IBackupItem item, BackupItemStatus status);
    void OnError(BackupError error);
    void OnCompleted(BackupJobResult result);
}
```

Regler:
- Alle metoder er **synkrone** (ingen async/await)
- Fejl i notifier fanges og logges – de stopper IKKE backuppen
- Notifier er valgfri (null = ingen events)

---

### `[ ]` B8 – `Api/BackupError.cs`
**Hvad:** Struktureret fejlinformation. Bruges både på job-niveau og per-fil.

```csharp
public sealed record BackupError(
    BackupErrorCode Code,
    string Message,
    string? SourcePath = null,          // Hvilken fil fejlede?
    string? DestinationPath = null,     // Hvor kopierede vi til?
    BackupPhase Phase = BackupPhase.Transferring,
    Exception? InnerException = null,   // Original .NET exception
    DateTime Timestamp = default
);
```

---

### `[ ]` B9 – `Api/TransferResult.cs`
**Hvad:** Resultat af én fils overførsel. RETURNERES fra `IFileTransfer` – KASTES IKKE.

```csharp
public sealed record TransferResult(
    bool Success,
    long BytesTransferred,
    string DestinationPath,
    DateTime TransferTime,
    long SourceFileSize = 0,
    string? ErrorMessage = null,
    BackupError? Error = null
);
```

⚠️ FIX fra Core3 Bug #2: `IFileTransfer` KASTEDE exception ved fejl → stoppede hele backuppen. Nu RETURNERER vi `TransferResult` med `Success = false` og backup **fortsætter**.

---

### `[ ]` B10 – `Models/Contexts.cs`
**Hvad:** Parameter-objekter der sendes igennem pipeline-faserne. Bærer konfiguration, ProgressTracker, og værktøjer.

💡 *I stedet for at sende 7 parametre til hver metode, pakker vi det i ét context-objekt.*

```csharp
public sealed record ScanContext(
    BackupPlan Plan,
    ProgressTracker ProgressTracker,
    CancellationToken CancellationToken
);

public sealed record TransferContext(
    ScanContext ScanContext,
    IFileTransfer Transfer
);

public sealed record OptionalFeatureContext(
    TransferContext TransferContext,
    IItemHasher? Hasher,
    IMetadataReader? MetadataReader,
    IIntegrityVerifier? Verifier,
    ITimestampCorrector? TimestampCorrector
);

public sealed record SidecarContext(
    string OutputDirectory,
    SidecarFormat Format,
    ISidecarGenerator SidecarGenerator
);
```

---

## SEKTION C: TRANSFER-LAG

---

### `[ ]` C1 – `Transfer/IFileTransfer.cs`
**Hvad:** Kontrakt for kopiering af én fil fra kilde til destination.

```csharp
internal interface IFileTransfer
{
    Task<TransferResult> TransferAsync(
        IBackupItem item,
        string destinationPath,
        IProgress<long>? progress,     // bytes kopieret så langt
        CancellationToken ct
    );
}
```

⚠️ Returnerer `TransferResult` – kaster ALDRIG exception ved fil-fejl.

---

### `[ ]` C2 – `Transfer/Filesystem/FilesystemFileTransfer.cs`
**Hvad:** Kopier fil fra lokalt/netværks-filsystem.

Implementationsregler:
```
1. Beregn destinations-mappe fra destinationPath
2. Directory.CreateDirectory(destinationsmappe)   ← ⚠️ FIX Core3 Bug #3
3. Åbn source-fil med FileStream (ingen lock)
4. Opret destination-fil med FileStream
5. Kopier i chunks (81920 bytes = 80 KB anbefales)
6. For hvert chunk: progress?.Report(bytesKopieret)  ← ⚠️ FIX Core3 Bug #4
7. Tjek ct.IsCancellationRequested HVERT chunk
8. Ved fejl: slet partial destination-fil, returner TransferResult(Success=false)
                                                  ← ⚠️ FIX Core3 Bug #2
9. Returner TransferResult(Success=true) ved succes
```

Eksempel på chunk-loop (pseudokode):
```csharp
var buffer = new byte[81920];
int bytesRead;
long totalCopied = 0;
while ((bytesRead = await source.ReadAsync(buffer, ct)) > 0)
{
    ct.ThrowIfCancellationRequested();
    await dest.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
    totalCopied += bytesRead;
    progress?.Report(totalCopied);
}
```

---

### `[ ]` C3 – `Transfer/Mtp/MtpFileTransfer.cs`
**Hvad:** Download fil fra MTP-enhed (iPhone, Android, kamera).

Implementationsregler:
```
1. Definer temp-fil sti: Path.GetTempFileName()
2. Åbn fil-stream fra MTP-enhed via MediaDevices API
3. Kopier til temp-fil (med progress-rapportering)
4. Flyt temp-fil til endelig destination
   (Directory.CreateDirectory() FØR flytning)       ← ⚠️ FIX Core3 Bug #3
5. Ved MTP-timeout: retry med eksponentiel backoff
   – Forsøg 1: vent 1s
   – Forsøg 2: vent 2s
   – Forsøg 3: vent 4s
   – Herefter: returner TransferResult(Success=false)  ← ⚠️ FIX Core3 Bug #2
6. Slet altid temp-fil i finally-blok (selv ved fejl/annullering)
7. Returner TransferResult(Success=true) ved succes
```

🚫 MÅ ALDRIG køres parallelt – kun `SequentialBackupEngine` bruger denne klasse.

---

### `[ ]` C4 – `Transfer/Test/TestFileTransfer.cs`
**Hvad:** Fake til unit tests – ingen faktisk I/O.

```csharp
internal sealed class TestFileTransfer : IFileTransfer
{
    // Konfigurer hvad der skal returneres
    public bool ShouldSucceed { get; set; } = true;
    public bool ShouldThrowCancelled { get; set; } = false;

    // Registrér kald (til assertions i tests)
    public List<(IBackupItem item, string dest)> Calls { get; } = new();

    public Task<TransferResult> TransferAsync(
        IBackupItem item, string destinationPath,
        IProgress<long>? progress, CancellationToken ct)
    {
        if (ShouldThrowCancelled) ct.ThrowIfCancellationRequested();
        Calls.Add((item, destinationPath));
        // Returner succes eller fejl baseret på ShouldSucceed
    }
}
```

---

## SEKTION D: SCANNER-LAG

---

### `[ ]` D1 – `Scanner/Filesystem/FilesystemItemScanner.cs`
**Hvad:** Enumerér filer fra lokalt/netværks-filsystem.

Implementationsregler:
```
1. Brug Directory.EnumerateFiles(sti, "*.*") for streaming (ikke buffering)
2. For hvert fundet element:
   a. Beregn relativePath = Path.GetRelativePath(plan.SourceDirectory, fullPath)
                                        ← ⚠️ FIX Core3 Bug #1 (directory structure goes lost)
   b. Opret BackupItem med Id, SourcePath, RelativePath, SizeBytes, ModifiedAt
   c. yield return item (IAsyncEnumerable)
3. Fang UnauthorizedAccessException per mappe: log advarsel, fortsæt
4. Respektér plan.Recursive – brug SearchOption.AllDirectories eller TopDirectoryOnly
5. Respektér plan.IncludePatterns og plan.ExcludePatterns (glob-matching)
6. Tjek ct.IsCancellationRequested for hvert item
```

💡 *`yield return` betyder: "giv dette item tilbage til kalderen MED DET SAMME uden at vente på at hele listen er klar". Dette er vigtigt for store backups.*

---

### `[ ]` D2 – `Scanner/Mtp/MtpItemScanner.cs`
**Hvad:** Enumerér filer fra MTP-enhed.

Implementationsregler:
```
1. Verificér at enhed er forbundet (via MediaDevices API)
   – Hvis ikke fundet: kast meningsfuld exception med BackupErrorCode.MTPDeviceNotFound
2. Åbn IKKE session her – session styres af engine/session-manager
3. Traversér enhedens filstruktur rekursivt
4. Bevar relativ sti fra enhedens rod ← ⚠️ FIX Core3 Bug #1
5. Opret BackupItem for hvert fundet element
6. Håndtér enhedsafbrydelse: log fejl, stop scanning, kast BackupErrorCode.MTPDeviceDisconnected
7. Understøt CancellationToken
```

🚫 Åbn ALDRIG MTP-session i scanner – session-livscyklus styres af engine.

---

### `[ ]` D3 – `Scanner/Test/TestItemScanner.cs`
**Hvad:** Fake til unit tests.

```csharp
internal sealed class TestItemScanner : IBackupScanner
{
    private readonly List<BackupItem> items;
    public bool WasCalled { get; private set; }

    public TestItemScanner(List<BackupItem> items) => this.items = items;

    public async IAsyncEnumerable<BackupItem> ScanAsync(
        BackupPlan plan, CancellationToken ct)
    {
        WasCalled = true;
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield(); // Simulér async
        }
    }
}
```

---

## SEKTION E: SIDECAR-LAG

---

### `[ ]` E1 – `Sidecar/ISidecarGenerator.cs`
```csharp
internal interface ISidecarGenerator
{
    Task<bool> GenerateAsync(
        IBackupItem item,
        string sidecarPath,
        CancellationToken ct
    );
}
```

---

### `[ ]` E2 – `Sidecar/Json/JsonSidecarGenerator.cs`
**Hvad:** Opret og opdatér JSON-sidecar-filer ved siden af de kopierede filer.

**Sidecar-filnavn konvention:** `IMG_1234.JPG.sidecar.json` (original filnavn + `.sidecar.json`)

Minimalt indhold (oprettet STRAKS efter transfer):
```json
{
  "source_path": "/source/vacation/IMG_1234.JPG",
  "destination_path": "/backup/vacation/IMG_1234.JPG",
  "transferred_at": "2026-05-05T12:15:00Z",
  "file_size": 2048576,
  "transfer_status": "success"
}
```

Beriget indhold (opdateret efter optional features):
```json
{
  "source_path": "...",
  "destination_path": "...",
  "transferred_at": "...",
  "file_size": 2048576,
  "transfer_status": "success",
  "hashes": {
    "source_sha256": "abc123...",
    "dest_sha256": "abc123..."
  },
  "metadata": {
    "photo_taken_date": "2024-06-15T10:30:00Z",
    "camera_model": "Canon EOS 5D"
  },
  "verification": {
    "verified": true,
    "verified_at": "2026-05-05T12:16:00Z"
  }
}
```

Implementationsregler:
```
1. Beregn sidecar-sti: destinationPath + ".sidecar.json"
2. Directory.CreateDirectory() FØR skrivning
3. Serialisér med System.Text.Json (JsonSerializerOptions.WriteIndented = true)
4. Gem til fil
5. Ved fejl: log advarsel, returner false – STOP IKKE backup
6. Understøt CancellationToken
7. Overskriver eksisterende sidecar (idempotent)
```

⚠️ FIX fra Core3 Design Flaw #3: I Core3 blev sidecars genereret SIDST. Nu oprettes de STRAKS efter transfer. Selv hvis hashing eller metadata fejler, eksisterer sidecaren allerede.

---

## SEKTION F: ENGINE-LAG

---

### `[ ]` F1 – `Engine/Progress/ProgressTracker.cs`
**Hvad:** Thread-safe akkumulering af fremskridt fra alle workers.

💡 *`Interlocked.Increment` er en CPU-instruktion der er 100% thread-safe uden at bruge locks. Brug det til alle tæller-operationer.*

```csharp
internal sealed class ProgressTracker
{
    // Tællere (thread-safe via Interlocked)
    private int directoriesScanned;
    private int filesDiscovered;
    private long bytesTotal;
    private int filesProcessed;
    private int filesSucceeded;
    private int filesFailed;
    private int filesSkipped;
    private long bytesProcessed;

    // Nuværende fil (thread-safe via lock)
    private readonly object currentFileLock = new();
    private string? currentFilePath;
    private long currentFileBytes;
    private long currentFileBytesProcessed;

    private BackupPhase currentPhase;
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();

    public void IncrementDirectoriesScanned() =>
        Interlocked.Increment(ref directoriesScanned);

    public void IncrementFilesDiscovered(long bytes)
    {
        Interlocked.Increment(ref filesDiscovered);
        Interlocked.Add(ref bytesTotal, bytes);
    }

    public void IncrementFilesSucceeded(long bytes)
    {
        Interlocked.Increment(ref filesSucceeded);
        Interlocked.Increment(ref filesProcessed);
        Interlocked.Add(ref bytesProcessed, bytes);
    }

    public void IncrementFilesFailed()
    {
        Interlocked.Increment(ref filesFailed);
        Interlocked.Increment(ref filesProcessed);
    }

    public void IncrementFilesSkipped()
    {
        Interlocked.Increment(ref filesSkipped);
        Interlocked.Increment(ref filesProcessed);
    }

    public void SetCurrentFile(string path, long bytes)
    {
        lock (currentFileLock)
        {
            currentFilePath = path;
            currentFileBytes = bytes;
            currentFileBytesProcessed = 0;
        }
    }

    public void UpdateCurrentFileBytesProcessed(long bytes)
    {
        lock (currentFileLock)
            currentFileBytesProcessed = bytes;
    }

    public void SetPhase(BackupPhase phase) =>
        currentPhase = phase; // enum-assignment er atomisk på .NET

    public BackupProgress GetSnapshot() => new(
        DirectoriesScanned: directoriesScanned,
        FilesDiscovered: filesDiscovered,
        BytesTotal: bytesTotal,
        FilesProcessed: filesProcessed,
        FilesSucceeded: filesSucceeded,
        FilesFailed: filesFailed,
        FilesSkipped: filesSkipped,
        BytesProcessed: bytesProcessed,
        CurrentFilePath: currentFilePath,
        CurrentFileBytes: currentFileBytes,
        CurrentFileBytesProcessed: currentFileBytesProcessed,
        CurrentPhase: currentPhase,
        ElapsedMilliseconds: stopwatch.ElapsedMilliseconds
    );
}
```

⚠️ FIX fra Core2: ProgressTracker var delt mutable state uden synkronisering → race conditions. Her bruger vi `Interlocked` for tæller og `lock` for nuværende fil.

---

### `[ ]` F2 – `Engine/BackupEngine.cs` → Gøres **abstract**
**Hvad:** Den nuværende `BackupEngine.cs` skal omskrives til en abstract base class der definerer det overordnede orkestreringsflow.

💡 *Abstract betyder: klassen kan ikke bruges direkte (du kan ikke `new BackupEngine()`). Den definerer et "template" for hvad alle engines SKAL gøre, men lader subklasser bestemme HOW (via abstract metoder).*

```csharp
public abstract class BackupEngine : IBackupEngine
{
    // Subklasser SKAL implementere disse
    protected abstract IBackupScanner CreateScanner(BackupPlan plan);
    protected abstract IFileTransfer CreateTransfer(BackupPlan plan);

    public async Task<BackupJobResult> RunAsync(
        BackupPlan plan,
        IProgress<IBackupProgress>? progress,
        CancellationToken ct)
    {
        // 1. Valider plan (kast BackupPlanValidationException ved ugyldig)
        // 2. Start ProgressTracker og stopwatch
        // 3. Opret scanner og transfer via abstract metoder
        // 4. Kald ScanPhase → TransferPhase → SidecarPhase → OptionalFeatures
        // 5. Returnér BackupJobResult
        // 6. Håndtér OperationCanceledException → status = Cancelled
    }

    // Delte hjælpemetoder som begge subklasser bruger
    protected async Task ScanPhaseAsync(...)
    protected async Task<TransferResult> TransferSingleItemAsync(...)
    protected async Task GenerateSidecarAsync(...)
    protected async Task RunOptionalFeaturesAsync(...)
}
```

---

### `[~]` F3 – `Engine/Sequential/SequentialBackupEngine.cs` → Fuldt implementeret
**Hvad:** Kør backup sekventielt, én fil ad gangen. ALTID til MTP.

Nuværende skeleton har scan-loop. Mangler alt herefter.

Komplet flow (pseudokode):
```
Constructor modtager:
  IBackupScanner scanner
  IFileTransfer fileTransfer
  ISidecarGenerator sidecarGenerator
  IProgressNotifier? progressNotifier    (valgfri)
  IItemHasher? hasher                    (valgfri)
  IMetadataReader? metadataReader        (valgfri)
  IIntegrityVerifier? verifier           (valgfri)
  ITimestampCorrector? timestampCorrector (valgfri)
  IBackupSessionStateStore sessionStore

RunAsync(plan, progress, ct):
  1. BackupPlanValidator.Validate(plan)
  2. var tracker = new ProgressTracker()
  3. var startTime = DateTime.UtcNow
  4. tracker.SetPhase(BackupPhase.Scanning)

  5. SCAN:
     await foreach item in scanner.ScanAsync(plan, ct):
       ct.ThrowIfCancellationRequested()
       tracker.IncrementFilesDiscovered(item.SizeBytes ?? 0)
       session.AddItem(item)

  6. tracker.SetPhase(BackupPhase.Transferring)
  7. START progress-rapportering (timer hvert 500ms: progress?.Report(tracker.GetSnapshot()))

  8. TRANSFER LOOP (for hvert pending item):
     a. ct.ThrowIfCancellationRequested()
     b. progressNotifier?.OnFileStarted(item)
     c. tracker.SetCurrentFile(item.SourcePath, item.SizeBytes ?? 0)

     d. Byg destinationPath ud fra OutputStructure og RelativePath
        – PreserveHierarchy: Path.Combine(plan.Destination, item.RelativePath)
        – Flat: Path.Combine(plan.Destination, Path.GetFileName(item.SourcePath))
        ← ⚠️ FIX Core3 Bug #1

     e. Collision resolution (tjek om destination eksisterer)

     f. result = await fileTransfer.TransferAsync(item, destinationPath, progressCallback, ct)

     g. Hvis result.Success = false:
          item.Status = Failed
          item.Errors ??= []; item.Errors.Add(result.Error ?? ...)
          tracker.IncrementFilesFailed()
          progressNotifier?.OnFileCompleted(item, BackupItemStatus.Failed)
          continue  ← ⚠️ FIX Core3 Bug #2 (stop IKKE hele backup)

     h. Sidecar STRAKS:
          item.Status = BackupItemStatus.Transferred
          item.TransferResult = result
          sidecarPath = destinationPath + ".sidecar.json"
          await sidecarGenerator.GenerateAsync(item, sidecarPath, ct)
          ← ⚠️ FIX Core3 Design Flaw #3 (sidecar genereres NU, ikke sidst)

     i. Optional features (kun hvis aktiveret i plan):
          – Hash: if (plan.HashTypes?.Count > 0) → await hasher.ComputeAsync(...)
                  → item.Hashes = result; await sidecarGenerator.GenerateAsync(item, ...)
          – Metadata: if (plan.EnableMetadata) → await metadataReader.ExtractAsync(...)
          – Verify: if (plan.EnableVerification && item.Hashes != null) → ...
          – Timestamps: if (plan.EnableTimestampCorrection && item.ExtractedMetadata != null) → ...

     j. tracker.IncrementFilesSucceeded(result.BytesTransferred)
     k. progressNotifier?.OnFileCompleted(item, BackupItemStatus.Succeeded)

  9. Stop progress-timer
  10. Byg og returnér BackupJobResult med:
       – Status = DetermineStatus(tracker)
       – StartTime, EndTime = DateTime.UtcNow
       – Alle tæller fra tracker
       – Samlede fejl fra alle items

  11. Catch OperationCanceledException:
       – returnér BackupJobResult med Status = Cancelled, partial data
```

---

### `[ ]` F4 – `Engine/BackupEngineFactory.cs`
**Hvad:** Vælg korrekt engine baseret på BackupPlan. Erstatter den nuværende tomme `BackupEngine.cs` (eller tilføjes som ny fil).

```csharp
public static class BackupEngineFactory
{
    public static IBackupEngine Create(
        BackupPlan plan,
        IServiceProvider serviceProvider)
    {
        // MTP-enhed → ALTID sekventiel
        if (plan.DeviceId != null)
            return serviceProvider.GetRequiredService<SequentialBackupEngine>();

        // Bruger har eksplicit anmodet om sekventiel
        if (plan.MaxDegreeOfParallelism == -1)
            return serviceProvider.GetRequiredService<SequentialBackupEngine>();

        // Filsystem → parallel (standard)
        return serviceProvider.GetRequiredService<LimitedParallelBackupEngine>();
    }
}
```

💡 *`plan.DeviceId != null` er den centrale check. Hvis DeviceId er sat, er det MTP – brug altid sekventiel.*

---

### `[ ]` F5 – `Engine/CollisionResolution/CollisionResolver.cs`
**Hvad:** Håndtér navne-kollisioner som et separat, testbart trin.

⚠️ FIX fra Core3 Design Flaw #4: Collision resolution skete implicit i scanner/transfer.

```csharp
internal static class CollisionResolver
{
    // Returner endelig destinations-sti baseret på CollisionStrategy
    public static string Resolve(
        string desiredPath,
        CollisionStrategy strategy,
        Func<string, bool> destinationExists)
    {
        if (!destinationExists(desiredPath))
            return desiredPath;  // Ingen kollision

        return strategy switch
        {
            CollisionStrategy.Skip => null!,  // null = spring over
            CollisionStrategy.Overwrite => desiredPath,
            CollisionStrategy.Rename => GenerateUniqueName(desiredPath),
            _ => desiredPath
        };
    }

    private static string GenerateUniqueName(string path)
    {
        // Tilføj _1, _2, _3 osv. indtil et unikt navn findes
        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        int counter = 1;
        string candidate;
        do { candidate = Path.Combine(dir, $"{name}_{counter++}{ext}"); }
        while (File.Exists(candidate));
        return candidate;
    }
}
```

---

### `[ ]` F6 – `Engine/LimitedParallel/LimitedParallelBackupEngine.cs`
**Hvad:** Paralleliseret backup KUN til filsystem-kilder.

🚫 MÅ ALDRIG bruges til MTP – `BackupEngineFactory` sikrer dette.

Producer-consumer design:
```
Scanner (1 tråd)
    ↓ [Channel<IBackupItem>, unbounded]
Transfer workers (N tråde, N = Math.Min(4, CPU/2), maks 8)
    ↓ [Channel<IBackupItem>, unbounded]
Sidecar workers (N tråde)
    ↓ [Channel<IBackupItem>, unbounded]
Optional feature workers (N tråde per feature)
```

Backpressure-kontrol:
- Hvis transfer-queue > 50 items: scanner venter (SemaphoreSlim)
- Prevents at scanneren bufferer hele filsystemet i hukommelsen

Vigtige regler:
```
1. Brug Channel.CreateUnbounded<IBackupItem>() – ALDRIG Bounded med multiple writers
   ← 🚫 FIX Core2: Bounded channels med multiple writers = deadlock-risiko

2. Brug SemaphoreSlim(N) for at begrænse antal samtidige workers

3. Alle workers har try/catch – ingen ubehandlede exceptions fra tasks

4. ProgressTracker er thread-safe (se F1)

5. Timeout på channel-operationer (10 sekunder) – forhindrer hang

6. Ingen cirkulære afhængigheder mellem stages
```

---

## SEKTION G: OPTIONAL FEATURES

---

### `[ ]` G1 – `Hashing/IItemHasher.cs`
```csharp
internal interface IItemHasher
{
    Task<Dictionary<string, string>> ComputeAsync(
        string filePath,
        List<HashType> hashTypes,
        IProgress<long>? progress,
        CancellationToken ct
    );
    // Returnerer f.eks.: { "SHA2_256" → "a1b2c3...", "BLAKE3_256" → "d4e5f6..." }
}
```

---

### `[ ]` G2 – `Hashing/Sha256Hasher.cs`
**Hvad:** Beregn SHA-256 hash af en fil i chunks (single-pass).

```
1. Åbn fil med FileStream
2. Opret SHA256-instans (System.Security.Cryptography.SHA256.Create())
3. Læs i chunks (81920 bytes)
4. For hvert chunk: sha256.TransformBlock(chunk)
5. progress?.Report(bytesLæst)
6. sha256.TransformFinalBlock(...)
7. Konvertér hash til hex-streng (BitConverter.ToString(...).Replace("-", "").ToLower())
8. Returnér { "SHA2_256" → hexStreng }
9. Håndtér FileNotFoundException gracefully → returner tom dictionary
```

💡 *"Single-pass" betyder at vi læser filen én gang og beregner hash. Hvis vi skal beregne flere hash-typer, kan vi gøre det simultant i samme fil-gennemlæb.*

---

### `[ ]` G3 – `Metadata/IMetadataReader.cs`
```csharp
internal interface IMetadataReader
{
    Task<ExtractedMetadata> ExtractAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken ct
    );
}
```

---

### `[ ]` G4 – `Metadata/Models/ExtractedMetadata.cs`
```csharp
public sealed record ExtractedMetadata(
    DateTime? CreatedDate,
    DateTime? ModifiedDate,
    DateTime? AccessedDate,
    FileAttributes? Attributes,
    // EXIF (kun billeder)
    string? CameraModel,
    DateTime? PhotoTakenDate,
    double? Latitude,
    double? Longitude,
    int? ISO,
    string? ShutterSpeed,
    string? FocalLength,
    Dictionary<string, string>? CustomProperties = null
);
```

---

### `[ ]` G5 – `Metadata/ExifMetadataReader.cs`
- Brug `MetadataExtractor` NuGet-pakke
- Returnér EXIF for billeder, kun filattributter for andre filer
- Håndtér korrupt EXIF: log advarsel, returnér kun filattributter

---

### `[ ]` G6 – `Verification/IIntegrityVerifier.cs`
```csharp
internal interface IIntegrityVerifier
{
    Task<VerificationResult> VerifyAsync(
        IBackupItem item,
        string destinationPath,
        IProgress<long>? progress,
        CancellationToken ct
    );
}
```

---

### `[ ]` G7 – `Verification/Models/VerificationResult.cs`
```csharp
public sealed record VerificationResult(
    bool Success,
    string Message,
    DateTime VerifiedAt,
    long VerificationTimeMs,
    bool FileDeleted = false,
    bool HashMismatch = false
);
```

---

### `[ ]` G8 – `Verification/IntegrityVerifier.cs`
- Beregn hash af destinations-filen
- Sammenlign med `item.Hashes` (source hash)
- Kræv at hashing er aktiveret (ellers returner false med besked)

---

### `[ ]` G9 – `Timestamps/ITimestampCorrector.cs`
```csharp
internal interface ITimestampCorrector
{
    Task<TimestampCorrectionResult> CorrectAsync(
        IBackupItem item,
        string destinationPath,
        CancellationToken ct
    );
}
```

---

### `[ ]` G10 – `Timestamps/Models/TimestampCorrectionResult.cs`
```csharp
public sealed record TimestampCorrectionResult(
    bool Success,
    DateTime? OriginalModified,
    DateTime? CorrectedModified,
    string? Reason = null
);
```

---

### `[ ]` G11 – `Timestamps/TimestampCorrector.cs`
⚠️ Kræv at `item.ExtractedMetadata != null` – metadata-success er FORUDSÆTNING. FIX fra Core3.

```
1. Tjek item.ExtractedMetadata != null → ellers returner TimestampCorrectionResult(false, "No metadata")
2. Brug PhotoTakenDate (EXIF) hvis tilgængeligt, ellers ModifiedDate
3. File.SetLastWriteTime(destinationPath, originalDate)
4. Returner TimestampCorrectionResult(true, originalDate, newDate)
5. Håndtér UnauthorizedAccessException gracefully
```

---

## SEKTION H: PROGRESS-NOTIFIERS

---

### `[ ]` H1 – `Progress/LoggingProgressNotifier.cs`
**Hvad:** Simpel notifier der logger til konsol/ILogger.
```csharp
internal sealed class LoggingProgressNotifier : IProgressNotifier
{
    private readonly ILogger logger;
    // OnPhaseChanged → logger.LogInformation("Phase: {prev} → {new}")
    // OnFileStarted → logger.LogDebug("Processing: {path}")
    // OnFileCompleted → logger.LogInformation("✓ {path}") eller logger.LogWarning("✗ {path}")
    // OnError → logger.LogError(...)
    // OnCompleted → logger.LogInformation("Backup complete: {status}")
}
```

---

### `[ ]` H2 – `Progress/SpectreProgressNotifier.cs`
**Hvad:** Rig konsol-visning via Spectre.Console.
- Vis live: fase, nuværende fil, %, MB/s, ETA
- Fejl vises som farvekodet tekst

---

## SEKTION I: DRY-RUN SUPPORT

---

### `[ ]` I1 – `Engine/DryRun/DryRunFileTransfer.cs`
**Hvad:** Simulér transfer uden faktisk I/O.

⚠️ FIX fra Core3 Bug #5: I Core3 simulerede dry-run ikke katalogstruktur, så hash/metadata-faserne fejlede.

```csharp
internal sealed class DryRunFileTransfer : IFileTransfer
{
    // Ingen faktisk fil-kopiering
    // Simulér progress (rapportér bytes som om de kopieres, med lille forsinkelse)
    // Simulér Directory.CreateDirectory (log men kør ikke)
    // Returnér altid TransferResult(Success=true) med korrekte stier
}
```

DryRun-tabel fra CORE4_PLAN_en.md:
| Operation | Kører i DryRun? |
|-----------|----------------|
| Fil-scanning | JA |
| Mappe-enumeration | JA |
| Fil-størrelse beregning | JA |
| Katalogstruktur oprettelse | SIMULERET (logger) |
| Fil-kopiering | NEJ |
| Sidecar-oprettelse | SIMULERET (logger) |
| Hash-beregning | NEJ |
| Metadata-udtræk | NEJ |
| Verifikation | NEJ |
| Database-persistens | NEJ |

---

## SEKTION J: DEPENDENCY INJECTION

---

### `[ ]` J1 – `DependencyInjection/ServiceCollectionExtensions.cs`
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBMTP3Core4(
        this IServiceCollection services,
        Action<Core4Options>? configure = null)
    {
        var options = new Core4Options();
        configure?.Invoke(options);

        // Obligatoriske services (Tier 1)
        services.AddScoped<IBackupSessionStateStore, InMemoryBackupSessionStateStore>();
        services.AddScoped<ISidecarGenerator, JsonSidecarGenerator>();
        services.AddScoped<SequentialBackupEngine>();

        // Default scanner og transfer (kan overrides)
        if (options.SourceType == BackupSourceType.MediaDevice)
        {
            services.AddScoped<IBackupScanner, MtpItemScanner>();
            services.AddScoped<IFileTransfer, MtpFileTransfer>();
        }
        else
        {
            services.AddScoped<IBackupScanner, FilesystemItemScanner>();
            services.AddScoped<IFileTransfer, FilesystemFileTransfer>();
        }

        // Optional features (kun registrér hvis aktiveret)
        if (options.EnableHashing)
            services.AddScoped<IItemHasher, Sha256Hasher>();

        if (options.EnableMetadata)
            services.AddScoped<IMetadataReader, ExifMetadataReader>();

        // Factory som IBackupEngine
        services.AddScoped<IBackupEngine>(sp =>
            BackupEngineFactory.Create(sp.GetRequiredService<BackupPlan>(), sp));

        return services;
    }
}
```

---

## SEKTION K: FAKES OG UNIT TESTS

---

### `[~]` K1 – `Tests/Fakes/FakeBackupScanner.cs`
Implementér `IBackupScanner`. Se D3 for mønster.

---

### `[~]` K2 – `Tests/Fakes/FakeFileTransfer.cs`
Implementér `IFileTransfer`. Se C4 for mønster.

---

### `[~]` K3 – `Tests/Fakes/FakeSidecarGenerator.cs`
Implementér `ISidecarGenerator`.

```csharp
internal sealed class FakeSidecarGenerator : ISidecarGenerator
{
    public List<(IBackupItem item, string path)> Calls { get; } = new();
    public bool ShouldFail { get; set; } = false;

    public Task<bool> GenerateAsync(IBackupItem item, string path, CancellationToken ct)
    {
        Calls.Add((item, path));
        return Task.FromResult(!ShouldFail);
    }
}
```

---

### `[ ]` K4 – `Tests/SequentialBackupEngineTests.cs`

| Test | Hvad testes | Hvilken bug fixes |
|------|-------------|-------------------|
| `EmptyScan_ReturnsCompleted` | 0 items → Completed, 0 fejl | – |
| `SingleFile_Success_SidecarGeneratedImmediately` | Sidecar genereres FØR optional features | Core3 Design Flaw #3 |
| `SingleFile_TransferFails_BackupContinues` | Transfer fejl → PartialSuccess, IKKE exception | Core3 Bug #2 |
| `MultipleFiles_OneFails_RestContinue` | 1 af 5 fejler → 4 succeeds, 1 fejlet | Core3 Bug #2 |
| `CancelDuringScan_ReturnsCancelled` | Annullering i scan → Cancelled | – |
| `CancelDuringTransfer_ReturnsCancelled` | Annullering i transfer → Cancelled + partial result | – |
| `InvalidPlan_ThrowsValidationException` | Tom plan → BackupPlanValidationException | – |
| `DryRun_NoActualTransfer` | DryRun=true → ingen faktiske filer kopieret | Core3 Bug #5 |
| `HierarchyPreserved_InDestination` | RelativePath bevares i destination | Core3 Bug #1 |
| `DestDirCreatedAutomatically` | Destination eksisterer ikke → oprettes automatisk | Core3 Bug #3 |
| `BytesProcessed_ReportedCorrectly` | BytesProcessed i progress er korrekt | Core3 Bug #4 |
| `HashingOptional_NullHashTypes_NoHashing` | HashTypes = null → hasher kaldes ikke | Core3 Design Flaw #2 |

---

### `[ ]` K5 – `Tests/ProgressTrackerTests.cs`
```
- GetSnapshot_ReturnsCorrectValues
- IncrementFromMultipleThreads_IsThreadSafe (Task.WhenAll med 100 Tasks)
- PercentageComplete_CalculatedCorrectly
- BytesPerSecond_CalculatedCorrectly
- EstimatedTimeRemaining_CalculatedCorrectly
```

---

### `[ ]` K6 – `Tests/FilesystemItemScannerTests.cs`
```
- EmptyDirectory_ReturnsNoItems
- FlatDirectory_ReturnsItemsWithCorrectRelativePath  ← FIX Core3 Bug #1
- NestedDirectories_RelativePathIncludesSubfolders   ← FIX Core3 Bug #1
- PermissionDenied_SkipsDirectory_ContinuesScanning
- Cancellation_StopsScanning
```

---

### `[ ]` K7 – `Tests/FilesystemFileTransferTests.cs`
```
- CopyToExistingDir_Succeeds
- DestDirNotExist_CreatedAutomatically              ← FIX Core3 Bug #3
- SourceNotFound_ReturnsFailed_DoesNotThrow         ← FIX Core3 Bug #2
- Cancellation_CleansUpPartialFile
- ProgressEvents_ReportedDuringCopy                 ← FIX Core3 Bug #4
```

---

### `[ ]` K8 – `Tests/BackupPlanValidatorTests.cs`
```
- MissingName_ReturnsError
- MissingSourceAndDeviceId_ReturnsError
- MissingDestination_ReturnsError
- InvalidSourceType_ReturnsError
- MaxDegreeOfParallelism_Zero_ReturnsError
- ValidPlan_NoErrors
```

---

### `[ ]` K9 – `Tests/CollisionResolverTests.cs`
```
- NoCollision_ReturnsSamePath
- CollisionWithSkip_ReturnsNull
- CollisionWithOverwrite_ReturnsSamePath
- CollisionWithRename_ReturnsIncrementedName (fil_1.jpg)
- MultipleCollisions_IncrementsCorrectly (fil_1.jpg → fil_2.jpg → fil_3.jpg)
```

---

## SEKTION L: MTP SESSION MANAGEMENT

---

### `[ ]` L1 – `Scanner/Mtp/MtpSessionManager.cs`
**Hvad:** Eksplicit livscyklus-styring af MTP-enhedssession.

```csharp
internal sealed class MtpSessionManager : IAsyncDisposable
{
    // OpenAsync() – verificér enhed, åbn session
    // KeepAliveAsync() – ping enhed hvert 30. sekund
    // CloseAsync() – luk session (i finally-blok)

    // Per-operation timeout: 60 sekunder (fra BackupPlan.OperationTimeout)
    // Retry: 3 forsøg med 1s, 2s, 4s backoff
    // Guard: tjek IsConnected FØR enhver operation
}
```

---

## IMPLEMENTERINGSRÆKKEFØLGE (start her)

```
TIER 1 – MTP kan bruges når disse er færdige:

[ ] 1.  A1  Rename CollisionStreategy.cs (5 min)
[ ] 2.  A2  Opdatér BackupPhase enum (10 min)
[ ] 3.  A3  Opdatér BackupErrorCode enum (15 min)
[ ] 4.  B1  Opret BackupJobStatus enum (5 min)
[ ] 5.  B2  Opret ErrorHandlingStrategy enum (5 min)
[ ] 6.  B3  Opret SidecarFormat enum (5 min)
[ ] 7.  B4  Opret HashType enum (10 min)
[ ] 8.  B8  Opret Api/BackupError.cs (10 min)
[ ] 9.  B9  Opret Api/TransferResult.cs (10 min)
[ ] 10. A4  Opdatér BackupPlan.cs (20 min)
[ ] 11. A5  Opdatér BackupResult.cs → BackupJobResult (20 min)
[ ] 12. A6  Opdatér IBackupEngine.cs (5 min)
[ ] 13. A7  Opdatér IBackupProgress.cs (10 min)
[ ] 14. A8  Opdatér BackupItem.cs (10 min)
[ ] 15. B5  Opret Api/IBackupItem.cs + BackupItem implementerer det (15 min)
[ ] 16. B6  Opret Api/BackupProgress.cs (15 min)
[ ] 17. B7  Opret Api/IProgressNotifier.cs (10 min)
[ ] 18. B10 Opret Models/Contexts.cs (15 min)
[ ] 19. C1  Opret Transfer/IFileTransfer.cs (5 min)
[ ] 20. E1  Opret Sidecar/ISidecarGenerator.cs (5 min)
[ ] 21. F1  Implementér Engine/Progress/ProgressTracker.cs (30 min)
[ ] 22. D1  Implementér Scanner/Filesystem/FilesystemItemScanner.cs (30 min)
[ ] 23. C2  Implementér Transfer/Filesystem/FilesystemFileTransfer.cs (45 min)
[ ] 24. E2  Implementér Sidecar/Json/JsonSidecarGenerator.cs (30 min)
[ ] 25. F5  Implementér Engine/CollisionResolution/CollisionResolver.cs (20 min)
[ ] 26. F2  Omskriv Engine/BackupEngine.cs til abstract (30 min)
[ ] 27. F3  Implementér Engine/Sequential/SequentialBackupEngine.cs – fuld krop (90 min)
[ ] 28. F4  Opret Engine/BackupEngineFactory.cs (15 min)
[ ] 29. J1  Implementér DependencyInjection/ServiceCollectionExtensions.cs (20 min)
[ ] 30. D3  Implementér TestItemScanner (15 min)
[ ] 31. C4  Implementér TestFileTransfer (15 min)
[ ] 32. K3  Implementér FakeSidecarGenerator (10 min)
[ ] 33. K4  Skriv SequentialBackupEngineTests (60 min)
[ ] 34. K5  Skriv ProgressTrackerTests (30 min)
[ ] 35. K6  Skriv FilesystemItemScannerTests (30 min)
[ ] 36. K7  Skriv FilesystemFileTransferTests (30 min)
[ ] 37. K8  Skriv BackupPlanValidatorTests (20 min)
[ ] 38. K9  Skriv CollisionResolverTests (20 min)

--- TIER 1 KOMPLET → Bekræft MTP-stabilitet ---

[ ] 39. D2  Implementér Scanner/Mtp/MtpItemScanner.cs (60 min)
[ ] 40. C3  Implementér Transfer/Mtp/MtpFileTransfer.cs (60 min)
[ ] 41. L1  Implementér MtpSessionManager.cs (45 min)

--- MTP KOMPLET ---

[ ] 42. B7  Opret IProgressNotifier.cs (allerede listet) – implementér:
[ ] 43. H1  LoggingProgressNotifier (20 min)
[ ] 44. H2  SpectreProgressNotifier (45 min)
[ ] 45. I1  DryRunFileTransfer (20 min)

--- TIER 2 KOMPLET → Bekræft UX ---

[ ] 46. G1  IItemHasher + G2 Sha256Hasher (45 min)
[ ] 47. G3  IMetadataReader + G4 ExtractedMetadata + G5 ExifMetadataReader (60 min)
[ ] 48. G6  IIntegrityVerifier + G7 VerificationResult + G8 IntegrityVerifier (45 min)
[ ] 49. G9  ITimestampCorrector + G10 TimestampCorrectionResult + G11 TimestampCorrector (30 min)
[ ] 50. F6  LimitedParallelBackupEngine (120 min)

--- TIER 3 KOMPLET → Bekræft performance ---
```

---

## KRITISKE FORBUDSZONER

```
🚫 Bounded Channel<T> med multiple writers → DEADLOCK-RISIKO (Core2-fejl)
🚫 Parallelism for MTP-kilder → ENHEDS-AFBRYDELSER (Core2-fejl)
🚫 IFileTransfer kaster exception ved fejl → STOP IKKE backup (Core3 Bug #2)
🚫 Sidecar genereres sidst → generes STRAKS efter transfer (Core3 Design Flaw #3)
🚫 Hashing er tvunget → kun hvis HashTypes != null (Core3 Design Flaw #2)
🚫 DestinationPath = kun filnavn → BEVAR RelativePath (Core3 Bug #1)
🚫 Ingen Directory.CreateDirectory() → OPRET altid destination-mappe (Core3 Bug #3)
🚫 ProgressTracker uden sync fra multiple tråde → INTERLOCKED/LOCK (Core2-fejl)
🚫 MTP-session åbnes i scanner → ÅBNES I ENGINE (Core2-fejl)
🚫 Dry-run springer alt over → SIMULÉR katalogstruktur (Core3 Bug #5)
```
