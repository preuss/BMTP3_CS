# Core4 – Skeleton Implementation TODO
## Detaljeret fil-for-fil guide til en udvikler

Dette dokument er en præcis, handlingsrettet liste over hvad der skal implementeres i Core4.
Hvert punkt angiver: fil, formål, hvad der MÅ gøres, og hvilke fejl fra Core/Core2/Core3 der fixes.

**Konventioner:**
- `[ ]` = Ikke startet
- `[x]` = Implementeret
- `[~]` = Delvist/skeleton oprettet (mangler krop)
- ⚠️ = Kritisk rettelse fra tidligere Core (ikke glem dette)
- 🚫 = Eksplicit forbud (lær fra Core2-fejl)

---

## STATUS PÅ NUVÆRENDE KODEBASE

Følgende filer eksisterer allerede og er **fuldstændigt implementerede** (behøver ikke røres):

- `Models/Enums/BackupPhase.cs` ✅
- `Models/Enums/BackupItemStatus.cs` ✅
- `Models/Enums/BackupErrorCode.cs` ✅
- `Models/Enums/BackupSourceType.cs` ✅
- `Models/Enums/CollisionStreategy.cs` ✅ *(note: filnavn har stavefejl – se TODO nedenfor)*
- `Models/Enums/OutputStructure.cs` ✅
- `Models/BackupPlan.cs` ✅
- `Models/BackupItem.cs` ✅ *(men se mangler nedenfor)*
- `Models/BackupResult.cs` ✅
- `Api/IBackupEngine.cs` ✅
- `Api/IBackupProgress.cs` ✅
- `Api/IFileProgress.cs` ✅
- `Scanner/IBackupScanner.cs` ✅
- `Helpers/Guard.cs` ✅
- `Engine/Validation/BackupPlanValidator.cs` ✅
- `Engine/Validation/BackupPlanValidationException.cs` ✅ *(antages komplet)*
- `Engine/State/BackupSessionState.cs` ✅
- `Engine/State/BackupSessionStateKey.cs` ✅
- `Engine/State/BackupSessionStateKeyFactory.cs` ✅
- `Engine/State/IBackupSessionStateStore.cs` ✅
- `Engine/State/InMemoryBackupSessionStateStore.cs` ✅

Følgende filer eksisterer men er **tomme skeletons** (skal implementeres):

- `Engine/BackupEngine.cs` `[~]`
- `Engine/Sequential/SequentialBackupEngine.cs` `[~]`
- `Engine/LimitedParallel/LimitedParallelBackupEngine.cs` `[~]`
- `Engine/Parallel/ParallelBackupEngine.cs` `[~]`
- `Engine/State/BackupSessionStore.cs` `[~]`
- `BMTP3.Core4.Tests/Fakes/FakeBackupScanner.cs` `[~]`
- `BMTP3.Core4.Tests/Fakes/FakeFileTransfer.cs` `[~]`
- `BMTP3.Core4.Tests/Fakes/FakeSidecarGenerator.cs` `[~]`

Følgende filer skal **oprettes fra bunden**:

*(Se TODO-listerne nedenfor)*

---

## TIER 1 – FUNDAMENTALS (Start her. MTP er blokeret indtil dette er færdigt.)

### A. MANGLER & RETTELSER I EKSISTERENDE FILER

---

#### `[ ]` Models/Enums/CollisionStreategy.cs → Overvej omdøb til `CollisionStrategy.cs`
- Filnavn har stavefejl (`Streategy` i stedet for `Strategy`)
- Enum hedder allerede `CollisionStrategy` korrekt
- Beslut: rename filen eller lad det stå – vælg konsekvent og opdater alle referencer

---

#### `[ ]` Models/BackupItem.cs – Tilføj manglende felter
Nuværende `BackupItem` mangler felter til at understøtte pipeline-berigelse.
Tilføj følgende properties (se `IBackupItem` i CORE4_ARCHITECTURE_en.md):
```
public List<BackupError>? Errors { get; set; }
public TransferResult? TransferResult { get; set; }
public Dictionary<string, string>? Hashes { get; set; }
public ExtractedMetadata? ExtractedMetadata { get; set; }
public VerificationResult? VerificationResult { get; set; }
```
⚠️ `BackupItem` er intern klasse – disse er interne enrichment-felter. Ikke eksponér dem offentligt.

---

#### `[ ]` Api/IBackupProgress.cs – Tilføj manglende properties
Nuværende `IBackupProgress` mangler:
```
string? CurrentFilePath { get; }
long CurrentFileBytes { get; }
long CurrentFileBytesProcessed { get; }
long ElapsedMilliseconds { get; }
double PercentageComplete { get; }
double BytesPerSecond { get; }
TimeSpan EstimatedTimeRemaining { get; }
```
⚠️ FIX fra Core3 Bug #4: `BytesProcessed` er defineret men ikke implementeret i Core3 – her SKAL alle disse properties faktisk implementeres i `BackupProgress`-recorden.

---

#### `[ ]` Models/BackupResult.cs – Overvej rename til BackupJobResult
Arkitekturdokumentet kalder den `BackupJobResult`. Nuværende `BackupResult` mangler:
```
bool Success { get; }
BackupJobStatus Status { get; }     // Completed/PartialSuccess/Failed/Cancelled
string JobName { get; }
DateTime StartTime { get; }
DateTime EndTime { get; }
double AverageTransferSpeedMBps { get; }
double PercentageComplete { get; }
List<BackupError>? GlobalErrors { get; }
```
Og beregnet:
```
TimeSpan Duration => EndTime - StartTime;
bool IsPartialSuccess => FilesSucceeded > 0 && FilesFailed > 0;
```

---

### B. NYE FILER – API LAG

---

#### `[ ]` Api/IBackupItem.cs
**Formål:** Kontrakt for en fil der flyder igennem pipeline.
```
IBackupItem:
  string Id { get; }
  string SourcePath { get; }
  string RelativePath { get; }
  long? SizeBytes { get; }
  DateTimeOffset? ModifiedAt { get; }
  BackupItemStatus Status { get; set; }
  string? DestinationPath { get; set; }
  List<BackupError>? Errors { get; set; }
  TransferResult? TransferResult { get; set; }
  Dictionary<string, string>? Hashes { get; set; }
  ExtractedMetadata? ExtractedMetadata { get; set; }
  VerificationResult? VerificationResult { get; set; }
```
Sørg for at `BackupItem` implementerer `IBackupItem`.

---

#### `[ ]` Api/BackupProgress.cs
**Formål:** Immutable record der implementerer `IBackupProgress`.
```
public record BackupProgress(
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
```
Beregnede properties:
```
PercentageComplete = FilesDiscovered > 0 ? FilesProcessed / (double)FilesDiscovered * 100 : 0
BytesPerSecond = ElapsedMilliseconds > 0 ? (BytesProcessed * 1000.0) / ElapsedMilliseconds : 0
EstimatedTimeRemaining = BytesPerSecond > 0 ? TimeSpan.FromSeconds((BytesTotal - BytesProcessed) / BytesPerSecond) : TimeSpan.Zero
```
⚠️ Brug den eksisterende `IFileProgress`/`ActiveFiles`-liste fra `IBackupProgress.cs` eller beslut om den skal fjernes til fordel for `CurrentFilePath`.

---

#### `[ ]` Api/IProgressNotifier.cs
**Formål:** Real-time event-rapportering til UI/CLI.
```
IProgressNotifier:
  void OnPhaseChanged(BackupPhase newPhase, BackupPhase previousPhase)
  void OnFileStarted(IBackupItem item)
  void OnFileCompleted(IBackupItem item, BackupItemStatus status)
  void OnError(BackupError error)
  void OnCompleted(BackupJobResult result)
```
Notifier er valgfri (null = ingen rapportering).
🚫 Notifier-metoder ALDRIG async – fire-and-forget, fejl fanges og logges.

---

#### `[ ]` Api/BackupError.cs
**Formål:** Struktureret fejlinformation til både job-niveau og fil-niveau.
```
public record BackupError(
  BackupErrorCode Code,
  string Message,
  string? Details = null,
  Exception? SourceException = null
)
```

---

#### `[ ]` Api/TransferResult.cs
**Formål:** Resultat af én fils overførsel.
```
public record TransferResult(
  bool Success,
  long BytesTransferred,
  string DestinationPath,
  DateTime TransferTime,
  string? ErrorMessage = null
)
```
⚠️ FIX fra Core3 Bug #2: Transfer MÅ returnere dette i stedet for at kaste exception.

---

#### `[ ]` Models/Enums/BackupJobStatus.cs
**Formål:** Samlet jobstatus.
```
NotStarted, Running, Completed, PartialSuccess, Failed, Cancelled
```

---

### C. NYE FILER – TRANSFER LAG

---

#### `[ ]` Transfer/IFileTransfer.cs
**Formål:** Kontrakt for kopiering af én fil.
```
IFileTransfer:
  Task<TransferResult> TransferAsync(
    IBackupItem item,
    string destinationPath,
    IProgress<long>? progress,
    CancellationToken ct
  )
```
⚠️ Returnerer `TransferResult` – KASTER IKKE exception ved fejl.

---

#### `[ ]` Transfer/Filesystem/FilesystemFileTransfer.cs
**Formål:** Kopier fil fra lokalt/netværks-filsystem til destination.

Regler:
- `Directory.CreateDirectory(destinationDir)` FØR kopiering ⚠️ FIX Core3 Bug #3
- Kopier i chunks (f.eks. 81920 bytes = 80 KB buffer)
- Rapportér fremskridt via `IProgress<long>` (bytes kopieret) ⚠️ FIX Core3 Bug #4
- Ved fejl: returner `TransferResult` med `Success=false` – KAST IKKE ⚠️ FIX Core3 Bug #2
- Ryd op partial fil ved fejl/annullering
- Understøt `CancellationToken` – tjek mellem chunks

---

#### `[ ]` Transfer/Mtp/MtpFileTransfer.cs
**Formål:** Download fil fra MTP-enhed (iPhone/Android/kamera) til destination.

Regler:
- Download til temp-fil FØR flytning til endelig destination
- Brug `MediaDevices` API til at åbne fil-stream fra device
- Retry med exponential backoff ved transiente fejl: 3 forsøg (1s, 2s, 4s)
- Tjek at device stadig er forbundet FØR hvert forsøg
- `Directory.CreateDirectory(destinationDir)` FØR flytning ⚠️ FIX Core3 Bug #3
- Ryd op temp-fil ved fejl/annullering (finally-blok)
- Rapportér fremskridt via `IProgress<long>` ⚠️ FIX Core3 Bug #4
- Returner `TransferResult` med `Success=false` – KAST IKKE ⚠️ FIX Core3 Bug #2
🚫 MÅ ALDRIG køres parallelt for MTP – kun Sequential engine bruger denne

---

#### `[ ]` Transfer/Test/TestFileTransfer.cs  *(i Core4.Tests eller som intern fake)*
**Formål:** Fake til unit tests.

Regler:
- Ingen faktisk I/O
- Kan konfigureres til at returnere succes, fejl, eller kaste OperationCanceledException
- Registrerer kald (hvilke items blev overført, med hvilken destinationPath)

---

### D. NYE FILER – SCANNER LAG

---

#### `[ ]` Scanner/Filesystem/FilesystemItemScanner.cs
**Formål:** Enumerér filer fra lokalt/netværks-filsystem.

Regler:
- Brug `Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)` (streaming)
- Bevar relativ sti: `RelativePath = Path.GetRelativePath(plan.Source, fullPath)` ⚠️ FIX Core3 Bug #1
- Ved `UnauthorizedAccessException`: log advarsel, skip mappe, fortsæt
- Respektér `plan.Recursive`, `plan.IncludePatterns`, `plan.ExcludePatterns`
- Understøt `CancellationToken` (tjek for hvert item)
- Returnér items via `IAsyncEnumerable<IBackupItem>` (stream, ikke buffer hele listen)

---

#### `[ ]` Scanner/Mtp/MtpItemScanner.cs
**Formål:** Enumerér filer fra MTP-enhed.

Regler:
- Brug `MediaDevices` API til at gennemgå enhedens filsystem
- Bevar relativ sti fra devices rod-sti ⚠️ FIX Core3 Bug #1
- Hvis enhed ikke kan findes: kast meningsfuld exception (ikke NullReferenceException)
- Hvis enhed afbrydes under scanning: log fejl, stop scanning med passende fejlkode
- Understøt `CancellationToken`
🚫 Åbn IKKE MTP-session i scanner – session-livscyklus styres af engine

---

#### `[ ]` Scanner/Test/TestItemScanner.cs  *(i Core4.Tests eller som intern fake)*
**Formål:** Fake til unit tests.

Regler:
- Returnerer prædefinerede `BackupItem`-objekter
- Ingen faktisk I/O
- Kan konfigureres med liste af items og valgfri fejl

---

### E. NYE FILER – SIDECAR LAG

---

#### `[ ]` Sidecar/ISidecarGenerator.cs
**Formål:** Kontrakt for generering af sidecar-metadatafiler.
```
ISidecarGenerator:
  Task<bool> GenerateAsync(
    IBackupItem item,
    string sidecarPath,
    CancellationToken ct
  )
```

---

#### `[ ]` Sidecar/Json/JsonSidecarGenerator.cs
**Formål:** Opret og opdatér JSON-sidecar-filer.

⚠️ FIX Core3 Design Flaw #3: Sidecar genereres STRAKS efter overførsel – IKKE sidst.

Minimalt indhold lige efter transfer:
```json
{
  "source_path": "...",
  "destination_path": "...",
  "transferred_at": "...",
  "file_size": 0,
  "transfer_status": "success"
}
```

Opdaterbart indhold når optional features kører:
```json
{
  ...minimal felter...,
  "hashes": { "source_sha256": "...", "dest_sha256": "..." },
  "metadata": { "exif_date": "...", "camera_model": "..." },
  "verification": { "verified": true, "verified_at": "..." }
}
```

Regler:
- Brug `System.Text.Json` til serialisering
- `Directory.CreateDirectory` FØR skrivning
- Ved skrivefejl: log advarsel, returner `false`, STOP IKKE backup
- Understøt `CancellationToken`
- Fil overskriver eksisterende sidecar (idempotent)

---

### F. NYE FILER – ENGINE LAG

---

#### `[ ]` Engine/Progress/ProgressTracker.cs
**Formål:** Thread-safe akkumulering af fremskridtsdata fra alle workers.

Properties (thread-safe via Interlocked/lock):
```
int DirectoriesScanned
int FilesDiscovered
long BytesTotal
int FilesProcessed
int FilesSucceeded
int FilesFailed
int FilesSkipped
long BytesProcessed
string? CurrentFilePath
long CurrentFileBytes
long CurrentFileBytesProcessed
BackupPhase CurrentPhase
Stopwatch Elapsed (started ved opstart)
```

Metoder:
```
void IncrementDirectoriesScanned()
void IncrementFilesDiscovered(long bytes)
void IncrementFilesSucceeded(long bytes)
void IncrementFilesFailed()
void IncrementFilesSkipped()
void SetCurrentFile(string path, long bytes)
void UpdateCurrentFileBytesProcessed(long bytes)
void SetPhase(BackupPhase phase)
BackupProgress GetSnapshot()
```

Regler:
- Alle tæller-incrementer bruger `Interlocked.Increment` / `Interlocked.Add`
- `CurrentFilePath`/`CurrentFileBytes` beskyttes med `lock`
- `GetSnapshot()` returnerer et immutabelt `BackupProgress`-record
⚠️ FIX Core2: Delt mutable state SKAL have korrekt sync – ingen race conditions

---

#### `[~]` Engine/Sequential/SequentialBackupEngine.cs
**Formål:** Kør backup sekventielt, én fil ad gangen. ALTID brugt til MTP.

Nuværende skeleton har:
- Constructor med `IBackupScanner` og `IBackupSessionStateStore` ✅
- Scan-loop der kalder `scanner.ScanAsync` og `session.AddItem` ✅
- `BackupPlanValidator.Validate(plan)` ✅

**Mangler at implementere (i rækkefølge):**

```
[ ] 1. Initialiser ProgressTracker
[ ] 2. Rapportér progress til IProgress<IBackupProgress> (med interval, f.eks. hvert 500ms)
[ ] 3. Scan alle items og opdatér ProgressTracker (IncrementFilesDiscovered, IncrementDirectoriesScanned)
[ ] 4. For hvert pending item:
    [ ] a. ct.ThrowIfCancellationRequested()
    [ ] b. SetCurrentFile i ProgressTracker
    [ ] c. Kald IFileTransfer.TransferAsync → TransferResult
    [ ] d. Hvis Success=false: marker item Failed, log, fortsæt (KAST IKKE) ⚠️ FIX Core3 Bug #2
    [ ] e. Hvis Success=true:
           - Opdatér item.Status = Transferred
           - Opdatér item.TransferResult
           - Kald ISidecarGenerator.GenerateAsync (STRAKS) ⚠️ FIX Core3 Design Flaw #3
           - Kald optional features (hvis aktiveret i BackupPlan):
             * IItemHasher (hvis HashTypes != null) ⚠️ FIX Core3 Design Flaw #2
             * IMetadataReader (hvis EnableMetadata)
             * IIntegrityVerifier (hvis EnableVerification)
             * ITimestampCorrector (hvis EnableTimestampCorrection, KUN hvis metadata lykkedes)
           - Opdatér sidecar igen med optional feature-resultater
    [ ] f. IncrementFilesSucceeded eller IncrementFilesFailed
[ ] 5. Byg og returnér BackupJobResult med EndTime, statistikker, fejlsamling
[ ] 6. Håndtér OperationCanceledException → sæt status Cancelled, returnér partial result
[ ] 7. Kald IProgressNotifier ved fase-skift og fil-events (hvis ikke null)
```

Dependency injection:
```
Tilføj til constructor:
  IFileTransfer fileTransfer
  ISidecarGenerator sidecarGenerator
  IProgressNotifier? progressNotifier
  IItemHasher? hasher          (optional)
  IMetadataReader? metadataReader    (optional)
  IIntegrityVerifier? verifier       (optional)
  ITimestampCorrector? timestampCorrector  (optional)
```

---

#### `[~]` Engine/BackupEngine.cs
**Formål:** Nuværende implementering er lavet om til en samlet orchestrator.

**Valg:** Enten gøres denne til en fabrik der delegerer til SequentialBackupEngine eller LimitedParallelBackupEngine, eller fjernes til fordel for `BackupEngineFactory`.

Anbefaling: Omdøb til `BackupEngineFactory` og lad den implementere `IBackupEngine` som facade:
```csharp
// Beslutningstræ:
// plan.SourceType == MediaDevice → SequentialBackupEngine
// plan.MaxDegreeOfParallelism == -1 → SequentialBackupEngine
// ellers → LimitedParallelBackupEngine
```

---

#### `[ ]` Engine/BackupEngineFactory.cs  *(NY fil eller omdøb BackupEngine.cs)*
**Formål:** Vælg korrekt engine baseret på BackupPlan.

```csharp
public static class BackupEngineFactory
{
  public static IBackupEngine Create(BackupPlan plan, IServiceProvider serviceProvider)
  // Logik:
  // MediaDevice → SequentialBackupEngine
  // MaxDegreeOfParallelism == -1 → SequentialBackupEngine
  // Filesystem → LimitedParallelBackupEngine
}
```

---

### G. DEPENDENCY INJECTION

---

#### `[ ]` DependencyInjection/ServiceCollectionExtensions.cs
**Formål:** Registrér alle Core4-services i Microsoft.Extensions.DependencyInjection.

Registreringer (minimum Tier 1):
```
IBackupScanner → FilesystemItemScanner (default, kan overrides)
IFileTransfer → FilesystemFileTransfer (default)
ISidecarGenerator → JsonSidecarGenerator
IBackupSessionStateStore → InMemoryBackupSessionStateStore
IBackupEngine → SequentialBackupEngine (eller via factory)
IProgressNotifier → null/LoggingProgressNotifier (default)
```

---

### H. FAKES TIL UNIT TESTS

---

#### `[~]` BMTP3.Core4.Tests/Fakes/FakeBackupScanner.cs
**Formål:** Test-dobbeltgænger for `IBackupScanner`.

Implementér:
```
- Constructor modtager List<BackupItem> itemsToReturn
- ScanAsync returnerer disse items via yield return
- Kan konfigureres til at kaste exception eller simulere tomme resultater
- Registrér at ScanAsync er kaldt (til assertions i tests)
```
Skal implementere `IBackupScanner`.

---

#### `[~]` BMTP3.Core4.Tests/Fakes/FakeFileTransfer.cs
**Formål:** Test-dobbeltgænger for `IFileTransfer`.

Implementér:
```
- Returnerer succesfuld TransferResult som standard
- Kan konfigureres til at returnere fejl for specifikke items
- Kan konfigureres til at kaste OperationCanceledException
- Registrér hvilke items der er forsøgt overført (til assertions)
- Udfør INGEN faktisk I/O
```
Skal implementere `IFileTransfer`.

---

#### `[~]` BMTP3.Core4.Tests/Fakes/FakeSidecarGenerator.cs
**Formål:** Test-dobbeltgænger for `ISidecarGenerator`.

Implementér:
```
- Returnerer true som standard (sidecar genereret)
- Kan konfigureres til at returnere false (simuler skrivefejl)
- Registrér hvilke items og stier der er kaldt med
- Udfør INGEN faktisk I/O
```
Skal implementere `ISidecarGenerator`.

---

## TIER 2 – UX OG BRUGERVENLIGHED (Når Tier 1 er stabil)

---

#### `[ ]` Api/IProgressNotifier.cs *(allerede listet i Tier 1 – implementér nu)*

---

#### `[ ]` Progress/LoggingProgressNotifier.cs
**Formål:** Simpel progress-notifier der logger til ILogger.
- Implementér alle `IProgressNotifier`-metoder
- Log fase-skift som `LogInformation`
- Log fil-fejl som `LogWarning`
- Ingen Spectre-afhængighed

---

#### `[ ]` Progress/SpectreProgressNotifier.cs
**Formål:** Rig progress-visning via Spectre.Console.
- Brug `AnsiConsole.Progress()` til live opdateringer
- Vis: fase, nuværende fil, %, MB/s, ETA
- Log fejl som `Markup`-farvekodet tekst

---

#### `[ ]` Engine/CollisionResolution/CollisionResolver.cs
**Formål:** Håndtér navne-kollisioner på destination som separat, testbart step.

⚠️ FIX Core3 Design Flaw #4: Collision resolution skal IKKE ske implicit i scanner/transfer.

Logik:
- Tjek om destinationsfil eksisterer
- Baseret på `BackupPlan.CollisionStrategy`:
  - `Skip`: marker item Skipped, sæt destinationPath til null
  - `Overwrite`: behold destinationPath som er
  - `Rename`: generer nyt unikt navn (Increment: fil_1.jpg, fil_2.jpg / Timestamp / Guid)
- Returnér opdateret destinationPath

---

#### `[ ]` Engine/OutputStructure/DestinationPathBuilder.cs
**Formål:** Byg destinations-sti baseret på OutputStructure-strategi.

Logik:
- `OutputStructure.PreserveHierarchy`:
  `destinationPath = Path.Combine(plan.Destination, item.RelativePath)`
- `OutputStructure.Flat`:
  `destinationPath = Path.Combine(plan.Destination, Path.GetFileName(item.SourcePath))`
⚠️ FIX Core3 Bug #1: Relativ sti SKAL bevares i Hierarchical mode

---

#### `[ ]` Engine/DryRun/DryRunFileTransfer.cs
**Formål:** Simulér overførsel uden faktisk I/O.

⚠️ FIX Core3 Bug #5: Dry-run skal simulere KOMPLET – inklusive katalogstruktur og fremskridt.

Regler:
- Implementér `IFileTransfer`
- Udfør INGEN faktisk fil-kopiering
- Simulér fremskridt (rapportér bytes som om de kopieres)
- Simulér `Directory.CreateDirectory` (men udfør den ikke)
- Returnér altid succesfuld `TransferResult`

---

## TIER 3 – AVANCEREDE FEATURES (Når Tier 1+2 er stabile)

---

#### `[ ]` Hashing/IItemHasher.cs
```
Task<Dictionary<string, string>> ComputeAsync(
  string filePath, List<HashType> hashTypes,
  IProgress<long>? progress, CancellationToken ct)
```

#### `[ ]` Hashing/Sha256Hasher.cs
- Beregn SHA-256 i chunks
- Rapportér progress
- Returnér dictionary: `"SHA2_256" → "hexstreng"`
- Håndtér fil slettet/access denied gracefully

#### `[ ]` Models/Enums/HashType.cs
```
SHA2_256, SHA2_512, SHA3_256_FIPS202, SHA3_512_KECCAK, BLAKE3_256, BLAKE3_512, MD5_128
```

---

#### `[ ]` Metadata/IMetadataReader.cs
```
Task<ExtractedMetadata> ExtractAsync(
  string sourcePath, string destinationPath, CancellationToken ct)
```

#### `[ ]` Metadata/Models/ExtractedMetadata.cs
```
record ExtractedMetadata(
  DateTime? CreatedDate, DateTime? ModifiedDate, DateTime? AccessedDate,
  FileAttributes? Attributes,
  string? CameraModel, DateTime? PhotoTakenDate,
  double? Latitude, double? Longitude,
  int? ISO, string? ShutterSpeed, string? FocalLength,
  Dictionary<string, string>? CustomProperties)
```

#### `[ ]` Metadata/ExifMetadataReader.cs
- Brug MetadataExtractor NuGet-pakke
- Returnér EXIF-felter for billeder, kun filattributter for andre filer
- Håndtér korrupt EXIF gracefully (log, returner kun filattributter)

---

#### `[ ]` Verification/IIntegrityVerifier.cs
```
Task<VerificationResult> VerifyAsync(
  IBackupItem item, string destinationPath,
  IProgress<long>? progress, CancellationToken ct)
```

#### `[ ]` Verification/Models/VerificationResult.cs
```
record VerificationResult(
  bool Success, string Message, DateTime VerifiedAt,
  long VerificationTimeMs,
  bool FileDeleted = false, bool HashMismatch = false)
```

#### `[ ]` Verification/IntegrityVerifier.cs
- Beregn hash af destinationsfil
- Sammenlign med `item.Hashes` (source hash)
- Returner `VerificationResult` med detaljer
- Kræver at hashing er aktiveret (ellers returner false med besked)

---

#### `[ ]` Timestamps/ITimestampCorrector.cs
```
Task<TimestampCorrectionResult> CorrectAsync(
  IBackupItem item, string destinationPath, CancellationToken ct)
```

#### `[ ]` Timestamps/Models/TimestampCorrectionResult.cs
```
record TimestampCorrectionResult(
  bool Success,
  DateTime? OriginalModified, DateTime? CorrectedModified,
  string? Reason = null)
```

#### `[ ]` Timestamps/TimestampCorrector.cs
- Kræv at `item.ExtractedMetadata != null` ⚠️ KRAV fra arkitektur: metadata-success er forudsætning
- Brug `PhotoTakenDate` (EXIF) hvis tilgængeligt, ellers `ModifiedDate`
- Sæt `File.SetLastWriteTime(destinationPath, originalDate)`
- Håndtér permission denied gracefully

---

#### `[ ]` Engine/LimitedParallel/LimitedParallelBackupEngine.cs
**Formål:** Paralleliseret backup KUN til filsystem-kilder.

🚫 MÅ ALDRIG bruges til MTP – fabrikken sikrer dette.

Design (producer-consumer):
```
Scanner (1 tråd) → [TransferQueue depth=50] → Transfer workers (N tråde)
                                              → [SidecarQueue] → Sidecar workers (N tråde)
                                                              → [Optional queues] → Feature workers
```

Regler:
- `N = Math.Min(4, Environment.ProcessorCount / 2)`, maks 8
- Kø-dybde = 50 (backpressure – scanner venter hvis køen er fuld)
- Thread-safe ProgressTracker (allerede designet til dette)
- Brug `SemaphoreSlim` eller `Channel<T>` (UNBOUNDED) – ikke Bounded Channel med multiple writers ⚠️ FIX Core2 deadlock-risiko
- Alle workers har eksplicit fejlhåndtering (ingen ubehandlede exceptions fra tasks)
- Timeout på kø-operationer (10 sekunder) – forhindrer hang
🚫 Ingen cirkulære afhængigheder mellem stages

---

## TIER 4 – INFRASTRUKTUR OG PERSISTENS

---

#### `[ ]` Repository/IBackupRepository.cs
```
Task SaveAsync(BackupSessionState session, CancellationToken ct)
Task<BackupSessionState?> LoadAsync(BackupSessionStateKey key, CancellationToken ct)
```

#### `[ ]` Repository/NoOpBackupRepository.cs
- Implementér `IBackupRepository`
- Gør ingenting (default – ingen persistens)

#### `[ ]` Repository/FileSystemBackupRepository.cs
- Gem session-tilstand som JSON i output-mappen
- Håndtér I/O-fejl gracefully (log, fortsæt uden persistens)

---

## MTP SESSION MANAGEMENT (Kritisk – implementér tidligt)

---

#### `[ ]` Scanner/Mtp/MtpSessionManager.cs (eller tilsvarende)
**Formål:** Eksplicit livscyklus-styring af MTP-enhedssession.

Regler:
- `OpenAsync()`: Opret forbindelse til enhed, valider at den er klar
- `KeepAliveAsync()`: Ping enhed hvert 30. sekund (timer eller baggrundstask)
- `CloseAsync()`: Luk forbindelsen i finally-blok
- Per-operation timeout: 60 sekunder (konfigurerbar via BackupPlan)
- Retry med exponential backoff ved transiente fejl: 3 forsøg (1s, 2s, 4s)
- Guard: tjek at session er åben FØR enhver operation (undgå brug af afbrudt enhed)
- Håndtér enhedsafbrydelse gracefully under alle faser

---

## TEST-KLASSER (BMTP3.Core4.Tests)

---

#### `[ ]` Tests/SequentialBackupEngineTests.cs
Minimum test-cases:
```
[ ] Tomt scan → returnér Completed med 0 filer
[ ] Enkelt fil, succes → returnér Completed, sidecar genereret STRAKS
[ ] Enkelt fil, transfer fejler → returnér PartialSuccess, backup STOPPER IKKE ⚠️
[ ] Annullering under scan → returnér Cancelled med partial result
[ ] Annullering under transfer → returnér Cancelled med partial result
[ ] Plan-validering fejler → kast BackupPlanValidationException
[ ] DryRun = true → ingen faktisk overførsel, korrekt statistik
[ ] Destination-mappe oprettes automatisk ⚠️ FIX Core3 Bug #3
[ ] RelativePath bevares korrekt i destination ⚠️ FIX Core3 Bug #1
[ ] BytesProcessed er korrekt efter transfer ⚠️ FIX Core3 Bug #4
```

---

#### `[ ]` Tests/BackupPlanValidatorTests.cs
```
[ ] Manglende Name → fejl
[ ] Manglende Source → fejl
[ ] Manglende Destination → fejl
[ ] Ugyldig SourceType → fejl
[ ] MaxDegreeOfParallelism = 0 → fejl (skal være > 0 eller null)
[ ] Valid plan → ingen fejl
```

---

#### `[ ]` Tests/ProgressTrackerTests.cs
```
[ ] GetSnapshot() returnerer korrekte værdier
[ ] Increment-metoder er thread-safe (kør fra multiple tråde)
[ ] PercentageComplete beregnes korrekt
[ ] BytesPerSecond beregnes korrekt
[ ] EstimatedTimeRemaining beregnes korrekt
```

---

#### `[ ]` Tests/FilesystemItemScannerTests.cs
```
[ ] Tom mappe → 0 items
[ ] Flad mappe med filer → items med korrekt RelativePath
[ ] Nested mapper → items med relative stier der inkluderer undermapper ⚠️ FIX Core3 Bug #1
[ ] Permission denied mappe → skippes, andre mapper scannes stadig
[ ] Annullering → stopper scanning
```

---

#### `[ ]` Tests/FilesystemFileTransferTests.cs
```
[ ] Kopier fil til eksisterende mappe → succes
[ ] Kopier fil, destination-mappe eksisterer ikke → oprettes automatisk ⚠️ FIX Core3 Bug #3
[ ] Kildefil eksisterer ikke → returnér fejl, KAST IKKE ⚠️ FIX Core3 Bug #2
[ ] Annullering under kopiering → ryd partial fil op, returnér fejl
[ ] Progress-events sendes under kopiering ⚠️ FIX Core3 Bug #4
```

---

## OPSUMMERING AF RÆKKEFØLGE

```
1. [ ] Ret BackupItem til at implementere IBackupItem (tilføj manglende felter)
2. [ ] Opret Api/IBackupItem.cs
3. [ ] Opret Api/BackupProgress.cs (med beregnede properties)
4. [ ] Opret Api/BackupError.cs
5. [ ] Opret Api/TransferResult.cs
6. [ ] Opret Api/IProgressNotifier.cs
7. [ ] Opdatér Models/BackupResult.cs → BackupJobResult
8. [ ] Opret Models/Enums/BackupJobStatus.cs
9. [ ] Opret Transfer/IFileTransfer.cs
10. [ ] Opret Sidecar/ISidecarGenerator.cs
11. [ ] Implementér Engine/Progress/ProgressTracker.cs
12. [ ] Implementér Transfer/Filesystem/FilesystemFileTransfer.cs
13. [ ] Implementér Transfer/Mtp/MtpFileTransfer.cs
14. [ ] Implementér Scanner/Filesystem/FilesystemItemScanner.cs
15. [ ] Implementér Scanner/Mtp/MtpItemScanner.cs
16. [ ] Implementér Sidecar/Json/JsonSidecarGenerator.cs
17. [ ] Implementér Engine/Sequential/SequentialBackupEngine.cs (fuld krop)
18. [ ] Opret/tilpas Engine/BackupEngineFactory.cs
19. [ ] Implementér DependencyInjection/ServiceCollectionExtensions.cs
20. [ ] Implementér Fakes i Tests-projektet
21. [ ] Skriv unit tests (se test-liste ovenfor)
--- Tier 1 komplet → valider MTP-stabilitet ---
22. [ ] Implementér CollisionResolver.cs
23. [ ] Implementér DestinationPathBuilder.cs
24. [ ] Implementér DryRunFileTransfer.cs
25. [ ] Implementér LoggingProgressNotifier.cs og SpectreProgressNotifier.cs
--- Tier 2 komplet → valider UX ---
26. [ ] Implementér Hashing-lag (IItemHasher, SHA256Hasher)
27. [ ] Implementér Metadata-lag (IMetadataReader, ExifMetadataReader)
28. [ ] Implementér Verification-lag (IIntegrityVerifier)
29. [ ] Implementér Timestamps-lag (ITimestampCorrector)
30. [ ] Implementér LimitedParallelBackupEngine (KUN filesystem)
--- Tier 3 komplet → valider performance ---
31. [ ] Implementér Repository-lag (IBackupRepository, NoOp, FileSystem)
32. [ ] Implementér MTP session-management (keep-alive, timeouts, retry)
```

---

## KRITISKE FORBUDSZONER (Lær af Core2)

- 🚫 Brug ALDRIG Bounded `Channel<T>` med multiple writers → deadlock-risiko
- 🚫 Parallelism MÅ ALDRIG bruges til MTP-kilder
- 🚫 Transfer MÅ IKKE kaste exception ved filoverførsels-fejl → returnér TransferResult
- 🚫 Sidecar MÅ IKKE genereres sidst → generer straks efter transfer
- 🚫 Hashing MÅ IKKE forceres → kun hvis BackupPlan.HashTypes != null
- 🚫 Directory-sti MÅ IKKE mangle → brug altid RelativePath fra scanner
- 🚫 ProgressTracker MÅ IKKE tilgås uden sync fra multiple tråde
- 🚫 MTP-session MÅ IKKE åbnes i scanner (åbnes i engine/session-manager)
