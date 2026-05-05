# Core4 - Hybrid Backup Engine (New Design)

## 📌 Introduktion: Hvad er Core4 og Hvorfor?

### Baggrunden

Gennem årene har vi udviklet flere backup-engine'er i BMTP3:

- **Core (Original)**: Virker fint, men helt sekventiel. Virkede da vi lavede det, men blev refaktureret og virker ikke mere. Det blev for komplekst at vedligeholde.

- **Core2 (Multithreaded)**: Vi ville gøre det hurtigere, så vi tilføjede parallel processing med `System.Threading.Channels` og worker-pools. **Problem**: Det blev aldrig stabilt. Race conditions, deadlocks, og uventet adfærd særligt med MTP-devices. Koden blev så kompleks at det blev næsten umuligt at debugge.

- **Core3 (Simpel Sequential)**: Vi gik tilbage til basics - single-threaded, enkel kode, let at forstå. **Det virker!** Men det er langsomt for store backup'er, og vi skal stadig have support for alle features (hashes, metadata, verification).

### Visionen for Core4

**Core4 er løsningen** der kombinerer det bedste fra alle tre:

- ✅ **Simpel at forstå** (som Core3) - lineært flow, let at debugge
- ✅ **Alle features** (som Core2) - hash, metadata, EXIF, verification
- ✅ **Intelligent performance** - parallel når det gør mening (filesystem), sequential når det skal være stabil (MTP)
- ✅ **Vedligeholdt** - ny, ren kodebase uden legacy-ballast

### Kerneidéen

**"Build it simple. Build it right. Then optimize smartly."**

I stedet for at gøre **alt** parallelt som Core2, eller **alt** sekventielt som Core3, skal Core4 være **adaptivt**:

```
Hvis kilden er MTP-device:
  → Brug Sequential (enkel, stabil)
  
Hvis kilden er Filesystem:
  → Brug Limited Parallelism (hurtig, men ikke chaotisk)
```

Denne tilgang betyder at:
- MTP-backups er **stabile** (ingen device-disconnects ved parallelisme)
- Filesystem-backups er **hurtige** (uden CPU-spild)
- Koden er **forståelig** (hver strategi er separat og enkel)

---

## 🔍 Lessons Learned from Core3 - Faldgruber Core4 Skal Undgå

Under code review af Core3 blev der fundet **5 kritiske bugs** og **6 design flaws** som Core4 skal håndtere bedre. Her er dem:

### 🔴 Kritiske Bugs fra Core3

**Bug #1: Directory Structure Goes Lost**
- Core3 scanner går rekursivt gennem mapper, men sætter `DestinationPath = fileName` (kun filnavn)
- Resultat: `Folder1/File1.txt` og `Folder2/File1.txt` bliver begge `File1.txt` (collision!)
- **Core4 Fix:** Preserve relative paths. `DestinationPath = Path.Combine(destination, relativePath)`

**Bug #2: Transfer Errors Kill Entire Backup**
- En fil fejler → hele backup'en stoppes med `throw`
- Hvis fil #500 af 5000 fejler, bliver resten aldrig backup'ed
- **Core4 Fix:** Log error, mark file as failed, **continue med næste fil**

**Bug #3: Destination Directory Must Pre-Exist**
- Transfer krasjer hvis output-directory ikke eksisterer
- **Core4 Fix:** `Directory.CreateDirectory(destinationPath)` før transfer

**Bug #4: Progress Reporting Incomplete**
- `IBackupProgress.BytesTransferred` defineret i interface, men ikke implementeret
- **Core4 Fix:** Complete implementation med actual bytes transferred

**Bug #5: Dry-Run Mode Incomplete**
- Dry-run laver ikke directories, så metadata/hash-faser fejler
- **Core4 Fix:** Full simulation - lav directory-struktur selv i dry-run

### 🟡 Design Flaws fra Core3

**Design Flaw #1: Error Handling Strategy is Implicit**
- Transfer errors → kritisk (stop)
- Metadata errors → tolerabel (continue)
- Men der's ingen way for user at configure dette
- **Core4 Fix:** Explicit ErrorStrategy enum per operation:
```csharp
public enum ErrorStrategy { StopOnError, SkipOnError, RetryOnError }
public ErrorStrategy TransferErrorStrategy { get; init; } = ErrorStrategy.StopOnError;
public ErrorStrategy MetadataErrorStrategy { get; init; } = ErrorStrategy.SkipOnError;
```

**Design Flaw #2: Hashing Always Forced (Slow)**
- Core3 beregner 3 hash-typer **altid**, selv hvis user ikke vil have dem
- Hashing er langsomt for store files
- **Core4 Fix:** Hashing skal være **optional** (null means no hashing)

**Design Flaw #3: Sidecars Generated Last**
- Hvis hashing/metadata fejler, sidecar genereres aldrig
- **Core4 Fix:** Generate **minimal sidecar efter transfer**, update det senere med metadata/hashes

**Design Flaw #4: Collision Resolution Not Testable**
- CollisionStrategy har 4 options (Rename, Skip, Overwrite, Error)
- Men scanner og transfer handter det implicitly
- **Core4 Fix:** Separate, testable CollisionResolution step

**Design Flaw #5: Immutable Flow is Actually Mutable**
- Items modified in-place via list indexing
- **Core4 Fix:** Document klart: items er mutable list, not immutable stream

**Design Flaw #6: Cancellation Handling Inconsistent**
- Blandet brug af `ct.ThrowIfCancellationRequested()` og `catch(OperationCanceledException)`
- **Core4 Fix:** Konsistent pattern hele vejen

### 📚 10 Lessons for Core4 Implementation

1. **Always preserve directory structures** - relative paths skal bevares fra source til destination
2. **Error strategy must be explicit and configurable** - define per operation type
3. **Make features optional** - especially hashing, which is slow
4. **Generate sidecars early** - after transfer, before optional features
5. **Auto-create destination directories** - no throw if destination doesn't exist
6. **Separate collision resolution** - make it testable, clear, explicit
7. **Complete progress reporting** - BytesTransferred, ETA, speed, etc.
8. **Test edge cases thoroughly**:
   - Empty directories
   - Permission denied errors
   - Disk full
   - Cancelled operations at various stages
   - Deep directory structures
   - Very large files
   - Duplicate filenames
9. **Document error policies clearly** - which errors stop backup, which are tolerated, how they're logged
10. **Consistent cancellation handling** - pick one pattern, use everywhere

---

## ⚠️ Why NOT Core2's Approach - Hvad vi lærte fra Channel/Pipeline Architecture

Under review af Core2 blev der fundet **5 kritiske threading-issues** som Core4 skal undgå:

### 🔴 Kritiske Threading-Issues fra Core2

**Issue #1: Deadlock Risk - Multiple Concurrent Writers to Bounded Channels**
- Core2 bruger `System.Threading.Channels` med multiple workers writing simultaneously
- Bounded channels (med limited capacity) kan deadlock hvis:
  - Writer thread A fylder kanalen, blocks
  - Writer thread B også tries to write, blocks
  - Men reader er blockeret andre steder
  - **Circular deadlock**
- **Core4 Fix:** Avoid complex channel pipelines. Use simple sequential or carefully-designed parallel.

**Issue #2: Race Conditions in Shared ProgressTracker**
- Core2 ProgressTracker er delt blandt multiple worker threads uden synchronization
- Race condition: snapshot may show partially-updated state
- Lost updates på progress counters
- **Core4 Fix:** Use thread-safe collections OR avoid shared mutable state

**Issue #3: Implicit Error Handling Across Stages**
- Core2 forwards alle items til næste stage, selv hvis de failed
- Next stage must check `if (item.ResultState == ItemResultState.Failed)`
- Error policy er **implicit** og **uklart**
- **Core4 Fix:** Explicit error policies per operation

**Issue #4: Task Orchestration is Manual and Error-Prone**
- Core2 chains 9+ tasks manually
- Easy to miss an `await Task.WhenAll` → deadlock
- Manual task management = fragile
- **Core4 Fix:** Simple orchestration - direct method calls, not magic task chaining

**Issue #5: Debugging is Nearly Impossible**
- 9 channels, 8 worker pools, shared progress tracker, implicit error forwarding
- When something goes wrong, very hard to trace root cause
- "Sophisticated but fragile" system
- **Core4 Fix:** Prioritize simplicity over theoretical efficiency

### 🟡 Design Flaws fra Core2

**Design Flaw #1: Coupling Between Stages is Implicit**
- Each stage doesn't know what comes next, must forward all items
- Downstream stages must understand the protocol
- Hard to reason about data flow

**Design Flaw #2: Bounded Channels Make Pipeline Brittle**
- Configuration-dependent: wrong capacity = deadlock or memory explosion
- Unbalanced stages → bottlenecks → deadlock
- Hard to tune correctly

**Design Flaw #3: MTP Session Management is Hacky**
- Managing MTP session lifetime is "bolted on" to pipeline orchestration
- Difficult to reason about lifecycle
- Risk of using device after disconnect

---

### 📚 7 Lessons for Core4 from Core2 Mistakes

1. **Avoid complex channel pipelines** - Don't build 9-channel infrastructure
2. **Thread-safety is hard** - Race conditions are subtle, deadlocks are deadly
3. **Explicit beats implicit** - Define clear error policies, don't forward implicitly
4. **Deadlock risk is real** - Multiple writers to bounded channels = danger
5. **Simplicity aids debugging** - Make data flow obvious, not hidden in task chaining
6. **Device management is tricky** - MTP sessions need explicit lifecycle management
7. **Test with real devices** - Don't rely on mocks, test actual MTP stability

---

## Executive Summary

**Core4** er en helt ny implementering af backup-engine'en, der kombinerer det bedste fra Core, Core2, og Core3:

- 🎯 **Hybrid architecture**: Sequential for MTP, Limited Parallelism for Filesystem
- 🧩 **Inkrementelt implementerbar**: Core-features først, derefter extended
- 🔧 **Ny kodebase**: Ikke afhængig af Core3's komponenter
- 🚀 **Alle Core2-features**: Men simple og vedligeholdbare
- 📊 **Intelligent parallelisme**: Baseret på source-type uden kompleksitet

---

## Hvad betyder "Hybrid"?

### Ordet "Hybrid" betyder to tilgange i én

I stedet for **én** strategi der skal passe til **alt**, har Core4 **to strategier**:

**1. Sequential Strategy (For MTP-devices)**

Når du backup'er fra en MTP-device (iPhone, Android telefon, digitalkamera):
```
Scan fil 1 → Transfer → Sidecar → Metadata → Hash → Verify → Next
Scan fil 2 → Transfer → Sidecar → Metadata → Hash → Verify → Next
Scan fil 3 → Transfer → Sidecar → Metadata → Hash → Verify → Next
...
```

**Hvorfor sekventielt?** MTP-protokollen er finicky. Hvis du prøver at læse fra device'et mens det overføres parallelt, crasher det. Derfor er sikkerhed vigtigere end hastighed.

**2. Limited Parallelism Strategy (For Filesystem)**

Når du backup'er fra en lokal folder eller netværksdrev:
```
Thread 1: Transfer fil 1     Thread 2: Transfer fil 2     Thread 3: Transfer fil 3
  ↓                              ↓                              ↓
Sidecar 1                    Sidecar 2                    Sidecar 3
  ↓                              ↓                              ↓
[Parallel pool for Hash/Meta]   [Parallel pool]              [Parallel pool]
  ├─ Hash 1 ║ Metadata 1        ├─ Hash 2                     ├─ Hash 3
  └─ ...                        └─ ...                        └─ ...
```

**Hvorfor parallel?** Filsystemet er stabilt. Vi kan læse mange filer samtidigt uden problem. Det gør backup'en **meget hurtigere**.

**Kontroleret parallelisme** betyder at vi ikke går vildt løs - vi sætter en max-grænse (f.eks. 4 threads) så systemet ikke bliver overbelastet.

---

## Arkitektur Overview

### Overordnet Flow

```
BackupPlan (Input)
    ↓
[Source Detection] (MTP vs. Filesystem?)
    ↓
┌─────────────────────────────────────┐
│   If MTP → Sequential Strategy      │
│   If Filesystem → Limited Parallel  │
└─────────────────────────────────────┘
    ↓
Core Pipeline:
  1. ScanPhase → Enumerate items
  2. TransferPhase → Copy files to output
  3. GenerateSidecarPhase → Create metadata files
    ↓
Extended Pipeline (hvis aktivt):
  4. GenerateHashesPhase → SHA256 per file
  5. ExtractMetadataPhase → EXIF, tags, etc.
  6. VerifyIntegrityPhase → Tjek hashes mod sidecar
  7. CorrectTimestampsPhase → Gendan original timestamps
    ↓
BackupJobResult (Output)
```

### Overordnet Flow

```
BackupPlan (Input)
    ↓
[Source Detection] (MTP vs. Filesystem?)
    ↓
┌─────────────────────────────────────┐
│   If MTP → Sequential Strategy      │
│   If Filesystem → Limited Parallel   │
└─────────────────────────────────────┘
    ↓
Core Pipeline:
  1. ScanPhase → Enumerate items
  2. TransferPhase → Copy files to output
  3. GenerateSidecarPhase → Create metadata files
    ↓
Extended Pipeline (hvis aktivt):
  4. GenerateHashesPhase → SHA256 per file
  5. ExtractMetadataPhase → EXIF, tags, etc.
  6. VerifyIntegrityPhase → Tjek hashes mod sidecar
  7. CorrectTimestampsPhase → Gendan original timestamps
    ↓
BackupJobResult (Output)
```

### Hvad er de forskellige faser?

**Fase 1: Scanning** 
Først skal vi finde ud af hvad skal backup'es. Vi scanner kilden (MTP-device eller folder) og får en liste af alle filerne. Hver fil har:
- Source-path (hvor filen er)
- File size (hvor stor er den)
- Timestamps (hvornår blev den lavet)
- Metadata (hvis vi kan læse det)

**Fase 2: Transfer**
Så kopierer vi hver fil fra kilden til output-directory. Dette er den vigtigste fase - hvis transfer fejler, er der ingen backup. Vi rapporterer progress undervejs så UI'et kan vise "50% færdig".

**Fase 3: Sidecar Generation**
For hver fil vi transferede, laver vi en `.json` fil (sidecar) der indeholder metadata om transfer'en:
- Hvor filen kom fra
- Hvor filen endte
- Hvornår blev den transfered
- Var det succesfuldt
- (Senere: hashes, metadata, verification status)

Sidecars er **vigtige** fordi:
- De dokumenterer hvad der skete
- Du kan audite backups senere
- Du kan restore uden at have kilden
- De enables verification og forensics

**Fase 4-7: Extended Features (Valgfri)**
Hvis brugeren aktiverer det, kan vi:
- Compute SHA256 hashes (for integritet)
- Extract EXIF fra billeder (for timeline)
- Verify at destinationsfiler matcher source (integrity check)
- Restore original timestamps (så filer ser ud som oprindelige)

### Fase-strategi

#### Sequential (MTP)
```
Scan Item 1
  ↓ Transfer
  ↓ GenerateSidecar
  ↓ [Optional: Hash + Metadata + Verify + Timestamps]
Scan Item 2
  ↓ Transfer
  ...
```

En fil ad gangen. Sikker, enkel, stabil. MTP elsker denne tilgang.

#### Limited Parallelism (Filesystem)
```
Thread 1: Transfer File 1        Thread 2: Transfer File 2
  ↓                               ↓
GenerateSidecar 1              GenerateSidecar 2
  ↓                               ↓
[Parallel Stages]              [Parallel Stages]
  ├─ Hash 1                      ├─ Hash 2
  ├─ Metadata 1                  ├─ Metadata 2
  └─ Timestamps 1                └─ Timestamps 2
```

Flere filer samtidigt. Hurtigere, men kontrolleret. Filesystem kan håndtere det.

---

## Komponenter & Ansvar (High-Level)

### Oversigt: Hvad der skal laves

Core4 består af flere "lag" (layers):

```
┌─────────────────────────────────────────┐
│ API LAG (Kontrakter)                    │
│ → Definerer hvad der skal ske           │
│ → Ingen implementering, kun interfaces  │
└─────────────────────────────────────────┘
           ↓ ↓ ↓ ↓ ↓
┌─────────────────────────────────────────┐
│ IMPLEMENTATION LAG                       │
│ → Scanner, Transfer, Hashing, osv.      │
│ → Hver komponent gør én ting godt       │
└─────────────────────────────────────────┘
           ↓ ↓ ↓ ↓ ↓
┌─────────────────────────────────────────┐
│ ORCHESTRATION LAG (The Engine)          │
│ → BackupEngine kontrollerer hele flow   │
│ → Sætter strategien (Sequential/Parallel)
└─────────────────────────────────────────┘
```

Den strukturering betyder:
- Hvis du skal lave en ny scanner (f.eks. for Google Drive), du laver kun det nye, rebrugrer resten
- Hvis hash-logik skal ændres, du ændrer kun Hasher, ikke Transfer eller Scanner
- Hvis du skal teste Transfer, du kan fake Scanner/Hasher

Det kalder "Separation of Concerns" - hver komponent har **én ansvar**.

### 1. **API Layer** (Kontrakter & DTOs)

**Filer:** `Api/` folder

**Ansvar:**
Definere interfacerne og data-strukturerne som hele Core4 bruger. Tænk på det som "kontrakten" mellem komponenter.

**Hvad skal være her:**
- `IBackupEngine` interface - "Hvordan køres en backup?"
- `BackupPlan` record - "Hvad er input til en backup?"
- `BackupJobResult` record - "Hvad er resultatet?"
- `IBackupProgress` interface - "Hvordan rapporteres fremskridt?"
- `IBackupItem` interface - "Hvad er en fil i systemet?"
- Fase-interfaces: `IScanPhase`, `ITransferPhase`, `ISidecarPhase`, osv.

**Eksempel:**
```csharp
// Dette er kun interfacet, ingen implementering
public interface IBackupEngine
{
    Task<BackupJobResult> RunAsync(
        BackupPlan plan,
        IProgress<IBackupProgress>? progress,
        CancellationToken ct
    );
}

public record BackupPlan(
    string SourcePath,
    string OutputDirectory,
    bool Recursive,
    bool IncludeHashes,
    bool IncludeMetadata
);

public record BackupJobResult(
    bool Success,
    int TotalItems,
    int SuccessfulItems,
    int FailedItems,
    long TotalBytes,
    TimeSpan Duration,
    List<BackupError> Errors
);
```

**Hvorfor separat?** Fordi hvis ændringer API'en, skal alle implementationer ændres. Ved at holde det samlet i én fil, er det lettet at se "hvad skal alle komponenter følge?"

---

### 2. **Scanner Layer** (Enumeration)

**Fil:** `Scanner/` folder med separate implementationer

**Hvad gør scanners?**

En scanner's job er at **liste alle filerne** fra en kilde. Den skal:
- Gå gennem mappen rekursivt
- Returnere info om hver fil (path, size, timestamps)
- Håndtere fejl hvis en mappe er utilgængelig
- Være annullerbar (hvis brugeren siger "stop!")

**Hvad skal **IKKE** scanner gøre:**
- Den skal IKKE kopiere filer
- Den skal IKKE compute hashes
- Den skal IKKE læse EXIF

Scanners gør **kun** enumeration.

#### `IItemScanner` (Interface)
```
AsyncEnumerable<IBackupItem> ScanAsync(ScanContext, CancellationToken)
```

En scanner returnerer en "async enumerable" - det betyder du kan få filerne en-efter-en uden at vente på hele listen først. Brugbart for meget store kilder.

**Implementationer:**

**`FilesystemItemScanner`**: Scan lokalt filesystem
- Bruges når kilde er en folder på C:, D:, eller netværk
- Walk directory-struktur med `Directory.EnumerateFiles`
- Return path, size, modificeret-dato
- Handle "Access Denied" fejl gracefully

**`MTPItemScanner`**: Scan MTP-device
- Bruges når kilde er iPhone, Android, kamera
- Kommunikerer via `MediaDevices.dll`
- Return device-path, size, device-timestamps
- Handle device-disconnect gracefully

**`MemoryItemScanner`**: Test double
- Bruges i unit-tests
- Return hardkodet test-data
- Ingen I/O, super hurtig

**Eksempel implementation skitse:**
```csharp
public class FilesystemItemScanner : IItemScanner
{
    public async IAsyncEnumerable<IBackupItem> ScanAsync(
        ScanContext context,
        CancellationToken ct)
    {
        // Gå gennem alle filer i mappen
        foreach (var file in Directory.EnumerateFiles(
            context.SourcePath,
            "*",
            SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            
            var info = new FileInfo(file);
            yield return new BackupItem(
                SourcePath: file,
                Size: info.Length,
                ModifiedDate: info.LastWriteTime
            );
        }
    }
}
```

---

### 3. **Transfer Layer** (Copy Files)

**Fil:** `Transfer/` folder

**Hvad gør transfers?**

En transfer-komponent kopierer en fil fra kilden til destinationen. Den skal:
- Læse filen fra kilde
- Skrive til destination
- Rapportere progress (bytes copied)
- Handle fejl (disk full, permission denied, read error)
- Bevare filnavn og struktur

**Hvad skal IKKE transfer gøre:**
- Compute hashes
- Extract metadata
- Generate sidecars
- Verify integritet

Transfers gør **kun** kopiering.

#### `IFileTransfer` (Interface)
```
Task<TransferResult> TransferAsync(
    IBackupItem item,
    TransferContext context,
    IProgress<TransferProgress> progress,
    CancellationToken ct)
```

Returnerer en `TransferResult` der indeholder:
- Bytes copied
- Destination path
- Status (success/error)
- Error message (hvis fejlede)

**Implementationer:**

**`FilesystemFileTransfer`**: Copy fil til fil
- Åbn source-fil
- Læs i chunks (f.eks. 1MB ad gangen)
- Skriv til destination
- Report progress per chunk
- Handle errors (disk full, permission denied)

**`MTPFileTransfer`**: Download fra MTP
- Åbn file handle på device
- Download til temp-file
- Move temp til final location
- Handle device-disconnect

**`TestFileTransfer`**: Test double
- Fake kopiering uden I/O
- Return predictable results

**Eksempel:**
```csharp
public class FilesystemFileTransfer : IFileTransfer
{
    public async Task<TransferResult> TransferAsync(
        IBackupItem item,
        TransferContext context,
        IProgress<TransferProgress> progress,
        CancellationToken ct)
    {
        var destPath = Path.Combine(
            context.OutputDirectory,
            item.RelativePath);
        
        Directory.CreateDirectory(Path.GetDirectoryName(destPath));
        
        using (var sourceStream = File.OpenRead(item.SourcePath))
        using (var destStream = File.Create(destPath))
        {
            byte[] buffer = new byte[1024 * 1024]; // 1MB chunks
            int bytesRead;
            long totalRead = 0;
            
            while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                await destStream.WriteAsync(buffer, 0, bytesRead, ct);
                totalRead += bytesRead;
                
                // Report progress
                progress.Report(new TransferProgress(
                    BytesCopied: totalRead,
                    TotalBytes: item.Size,
                    Percentage: (totalRead * 100) / item.Size
                ));
            }
        }
        
        return new TransferResult(
            Success: true,
            DestinationPath: destPath,
            BytesCopied: totalRead
        );
    }
}
```

---

### 4. **Sidecar Generator Layer** (Metadata Files)

**Fil:** `Sidecar/` folder

**Hvad er en sidecar?**

En sidecar er en metadata-fil der ligger **ved siden af** (hence "side-car") den transferede fil. For hver backup'ed fil, er der en `.json` fil med info om transfer'en.

**Eksempel:**
```
Output/
  ├─ IMG_1234.JPG          (transfered file)
  ├─ IMG_1234.JPG.sidecar.json  (metadata om transfer)
  └─ Folder/
     ├─ Document.PDF       (transfered file)
     └─ Document.PDF.sidecar.json (metadata)
```

**Hvad indeholder sidecaren?**
```json
{
  "source_path": "/source/IMG_1234.JPG",
  "destination_path": "/backup/IMG_1234.JPG",
  "transferred_at": "2026-05-05T12:15:00Z",
  "original_size": 2048576,
  "transferred_size": 2048576,
  "transfer_status": "success",
  "hashes": {
    "source_sha256": "abc123...",
    "dest_sha256": "abc123..."
  },
  "metadata": {
    "exif_date": "2024-06-15T10:30:00Z",
    "created_date": "2024-06-15T10:30:00Z",
    "modified_date": "2024-06-15T10:30:00Z"
  },
  "verification": {
    "verified": true,
    "verified_at": "2026-05-05T12:15:30Z"
  }
}
```

**Hvorfor er sidecars vigtige?**

1. **Audit trail**: Du kan senere se "hvornår blev denne fil backup'ed?"
2. **Integritet-check**: Hashes i sidecaren giver dig mulighed for at tjekke om backup'en er intakt
3. **Restore-information**: Hvis originalen slettes, har du stadig metadata
4. **Forensics**: Du kan bygge et timeline baseret på sidecars
5. **Redundancy**: Self-contained documentation per file

#### `ISidecarGenerator` (Interface)
```
Task<SidecarContent> GenerateAsync(
    IBackupItem item,
    TransferResult transfer,
    SidecarContext context,
    CancellationToken ct)
```

**Implementationer:**

**`JsonSidecarGenerator`**: Generate `.json` sidecars
- Lavet struktur som vist ovenfor
- Serialiseres med `System.Text.Json`
- Enkel at læse/parse
- Kompatibel med alle systemer
- **VIGTIG:** Skal kunne opdateres på stedet med metadata/hashes senere (kan være partially-filled)

**`XmlSidecarGenerator`**: Generate `.xml` sidecars
- Alternativ format
- Mere verbose, men nogle foretrækker XML
- Samme information som JSON

**`TestSidecarGenerator`**: Test double
- Return dummy sidecar
- Ingen I/O

**Core4 Flow for Sidecars (Key Difference from Core3):**

Core3 genererer sidecars **sidst** (efter hashing). Core4 skal gøre det anderledes:

```
Transfer ✅ → GenerateSidecar (MINIMAL, NU)
                 ├─ source_path
                 ├─ destination_path
                 ├─ transferred_at
                 ├─ file_size
                 ├─ transfer_status: "success"
                 └─ (hashes: null, metadata: null, verification: null)
                 
[Later, if enabled]
ExtractMetadata → UpdateSidecar (add metadata fields)
GenerateHashes → UpdateSidecar (add hash fields)
VerifyIntegrity → UpdateSidecar (add verification fields)
CorrectTimestamps → UpdateSidecar (timestamps corrected)

Result: Sidecar exists immediately, gradually enriched with features
```

**Fordele:**
- ✅ Sidecar garanteret at eksistere selv hvis senere faser fejler
- ✅ Auditable straks efter transfer
- ✅ User kan se at fil blev backup'ed selv uden hashes
- ✅ Kan update sidecaren som feature-flag

---

### 5. **Hash Layer** (SHA256 Verification) - FASE 2

**Fil:** `Hashing/` folder

**Hvad gør hashers?**

En hasher beregner en "fingerprint" af en fil - altså en unik string baseret på filens indhold. Hvis filen ændres selv blot én bit, bliver hashen helt anderledes.

**Hvorfor er det vigtig?**

Hvis du backup'er en 500MB film, og under transfer'en en bit får flipped (pga. dårlig RAM, USB-driver bug, osv.), ville du gerne vide det. En hash-check kan detektere dette.

**Hvordan virker det:**
```
Source file: [read bytes] → SHA256() → "a1b2c3d4e5..."
Dest file: [read bytes] → SHA256() → "a1b2c3d4e5..."

Hvis de er ens: ✅ File transfered korrekt
Hvis de er forskellige: ❌ File har fejl
```

#### `IItemHasher` (Interface)
```
Task<HashResult> ComputeAsync(string filePath, IProgress<long>, CancellationToken)
```

Returnerer en `HashResult` med:
- SHA256 hex-string
- Bytes processed
- Time taken

**Implementationer:**

**`SHA256Hasher`**: Compute SHA256
- Åbn fil
- Læs in chunks
- Feeds til SHA256
- Return hex-string

**`TestHasher`**: Test double
- Return predictable hashes
- Ingen I/O

---

### 6. **Metadata Layer** (EXIF, Tags, etc.) - FASE 2

**Fil:** `Metadata/` folder

**Hvad gør metadata readers?**

En metadata-reader ekstrakt information **om** en fil uden at ændre filen. For billeder kan det være:
- EXIF date (hvornår blev foto taget?)
- Camera model
- GPS coordinates
- ISO, shutter speed, osv.

For alle filer:
- Created date (Windows)
- Modified date
- Accessed date
- File attributes (read-only, hidden, osv.)

#### `IMetadataReader` (Interface)
```
Task<FileMetadata> ExtractAsync(
    string sourcePath,
    string destPath,
    MetadataContext context,
    CancellationToken ct)
```

Returnerer `FileMetadata` med:
- EXIF data (hvis billede)
- File attributes
- Timestamps

**Implementationer:**

**`ExifMetadataReader`**: Extract EXIF
- Bruger en EXIF-parser (f.eks. `MetadataExtractor`)
- Læs billede
- Parse EXIF-data
- Return relevant fields

**`CommonMetadataReader`**: File attributes
- Brug `FileInfo`
- Get created/modified/accessed dates
- Get file attributes
- Return basic metadata

---

### 7. **Verification Layer** (Optional) - FASE 2

**Fil:** `Verification/` folder

**Hvad gør verifiers?**

En verifier tjekker at en transfered fil er korrekt ved at sammenligne hashes:

```
Sidecar siger: source_sha256 = "abc123..."
Du re-hashes: dest_sha256 = "abc123..."

Match? → ✅ Verified
Mismatch? → ❌ File corrupted!
```

#### `IIntegrityVerifier` (Interface)
```
Task<VerificationResult> VerifyAsync(
    string destPath,
    HashResult stored,
    IProgress,
    CancellationToken)
```

---

### 8. **Orchestration Layer** (The Engine)

**Fil:** `Engine/` folder

**Hvad gør engine'en?**

Engine'en er **dirigenten** - den koordinerer alle komponenter:
- Scanner finder filerne
- Transfer kopierer dem
- Sidecar dokumenterer det
- (Optional: Hash verificerer, Metadata ekstrakter, osv.)

Engine'en **IKKE**:
- Dér er ikke copy-logik
- Dér er ikke hash-logik
- Den er bare orkestrering

#### `IBackupEngine` (Interface)
```
Task<BackupJobResult> RunAsync(BackupPlan, IProgress, CancellationToken)
```

#### Implementationer:

**`SequentialBackupEngine`**: For MTP
- En fil ad gangen
- Scan → Transfer → Sidecar → (Optional: Hash/Metadata/Verify)
- Repeateres for hver fil

**`LimitedParallelBackupEngine`**: For Filesystem
- Multiple files simultaneously
- Koordinerer parallel transfers
- Parallele hashing på transferred files
- Smart queuing

**`BackupEngineFactory`**: Intelligentvalg
- Ser på BackupPlan
- Detects kilde-type (MTP vs. Filesystem)
- Returnerer rigtig engine-type

---

### 9. **DI & Composition**

**Fil:** `DependencyInjection/` folder

**Hvad er Dependency Injection?**

I stedet for at hver klasse laver sine egne dependencies, passes de ind. Dette gør det nemt at teste (du kan pass in fakes) og nemt at skifte implementationer.

**Eksempel uden DI (dårligt):**
```csharp
public class MyEngine
{
    public MyEngine()
    {
        _scanner = new FilesystemItemScanner(); // Hard-coded
        _transfer = new FilesystemFileTransfer(); // Hard-coded
    }
}

// Problem: Du kan ikke teste med fake-scanner!
```

**Eksempel med DI (godt):**
```csharp
public class MyEngine
{
    private IItemScanner _scanner;
    private IFileTransfer _transfer;
    
    public MyEngine(IItemScanner scanner, IFileTransfer transfer)
    {
        _scanner = scanner;
        _transfer = transfer;
    }
}

// Du kan test med:
// var engine = new MyEngine(new TestScanner(), new TestTransfer());
```

**Hvad gør DI-registration?**

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBMTP3Core4(
        this IServiceCollection services)
    {
        // Register implementations
        services.AddScoped<IItemScanner, FilesystemItemScanner>();
        services.AddScoped<IFileTransfer, FilesystemFileTransfer>();
        services.AddScoped<IBackupEngine, BackupEngine>();
        // ...
        
        return services;
    }
}

// I Program.cs eller Startup:
var services = new ServiceCollection();
services.AddBMTP3Core4();
var engine = services.BuildServiceProvider().GetRequiredService<IBackupEngine>();
```

---

## Feature-prioritering

### Hvorfor prioritere features?

Hvis vi prøvede at lave **alt på samme tid**, ville vi:
- Blive overvældet
- Lave fejl
- Teste dårligt
- Aldrig blive færdig

I stedet laver vi **MVP først** (Minimum Viable Product) - det der gør systemet fungere. Derefter tilføjer vi features i prioriteret rækkefølge.

**Tænk på det som at bygge et hus:**
- MVP: Væg, tag, dør, vindue - du kan bo der
- Extended: El, vand, varme - det er praktisk
- Nice-to-have: Alarmanlæg, varmepumpe, smart home - det er luksus

### 🔴 **Core Features (MUST HAVE)**

Disse features **kræves** for at Core4 virker som backup-engine. Uden dem er der ingen backup.

#### 1. **Basic File Scanning**

**Hvad betyder det?**
Vi skal kunne liste alle filerne i en kilde-mappe. For hver fil skal vi kende:
- Hvor filen er (path)
- Hvor stor den er (size)
- Hvornår blev den modificeret (timestamp)

**Hvorfor er det vigtigt?**
Uden at vide hvilke filer findes, kan vi ikke backup'e noget. Dette er **første skridt** i backup-processen.

**Eksempel:**
```
Source: C:\Pictures\
Files found:
  - vacation.jpg (2.5 MB, modified: 2024-06-15)
  - beach.jpg (3.1 MB, modified: 2024-06-16)
  - Folder\sunset.jpg (1.2 MB, modified: 2024-06-17)
```

**Implementering:**
- Filesystem Scanner kan enumere filer med `Directory.EnumerateFiles`
- MTP Scanner kan enumerate via `MediaDevices.dll`
- Test Scanner returnerer hardkodet test-data

---

#### 2. **File Transfer (Copy)**

**Hvad betyder det?**
Vi skal kopiere hver fil fra kilde til output-directory. Filen skal være **bitwise identisk** efter kopiering.

**Hvorfor er det vigtigt?**
Dette er **kernen** i backup'en. Hvis transfer'en mislykkes eller ødelægger data, har vi ingen backup.

**Eksempel:**
```
Source: C:\vacation.jpg (2.5 MB)
↓ [Copy 2.5 MB data]
Output: D:\Backups\vacation.jpg (2.5 MB)

Status: ✅ Success - 2.5 MB copied
```

**Implementering:**
- Åbn source-fil
- Læs i chunks (f.eks. 1 MB ad gangen)
- Skriv til destination
- Report progress
- Handle fejl (disk full, permission denied, osv.)

**Fejlhåndtering:**
```
Fejl scenario 1: Disk full
  → Log error
  → Mark file as failed
  → Continue med næste fil (IKKE stop!)

Fejl scenario 2: File not found
  → Log warning
  → Skip file
  → Continue med næste fil

Fejl scenario 3: Permission denied
  → Log warning
  → Skip file
  → Continue med næste fil

Fejl scenario 4: Destination directory doesn't exist
  → Create it! Directory.CreateDirectory(dest)
  → Then transfer (IKKE throw/fail)
```

**VIGTIG NOTE fra Core3 Review:**
Core3 **throws** på transfer errors, hvilket stopper hele backup'en. Core4 skal være robust:
- Log error
- Mark file as failed
- **Continue med næste fil**
- Return partial success in result

---

#### 3. **Sidecar Generation (Minimal)**

**Hvad betyder det?**
For hver transfered fil, laver vi en `.json` fil (sidecar) der dokumenterer hvad der skete.

**Minimal sidecar indeholder:**
```json
{
  "source_path": "C:\\vacation.jpg",
  "destination_path": "D:\\Backups\\vacation.jpg",
  "transferred_at": "2026-05-05T12:15:00Z",
  "file_size": 2621440,
  "transfer_status": "success"
}
```

**Hvorfor er det vigtigt?**
- Du har dokumentation af hvad der blev backup'ed
- Du kan audite senere: "Hvornår blev denne fil backup'ed?"
- Du kan restore uden at have kilden: "Jeg slettede den fra iPhone, men har sidecaren"
- **VIGTIG:** Sidecar genereres **straks efter successful transfer** (ikke sidst som i Core3)

**Implementering:**
- Efter succesfuld transfer (STRAKS!)
- Opret fil: `{originalfilename}.sidecar.json`
- Skriv minimal JSON
- Gem i samme mappe som den transferede fil
- Hvis senere faser (metadata, hashing) aktiveres, **update** sidecaren med nye felter

**VIGTIG NOTE fra Core3 Review:**
Core3 genererer sidecars **sidst** (efter hashing), så hvis hashing fejler, sidecar genereres aldrig.
Core4 skal gøre det **tidligt** og **update** det senere med features.

------

#### 4. **Basic Error Handling**

**Hvad betyder det?**
Vi skal håndtere fejl gracefully - ikke blot crash.

**Eksempler på fejl:**
```
- Transfer fejler pga. disk full
  → Log error
  → Return detailed error message
  → Backup marked as "Partial Success"

- Scanner finder utilgængelig mappe
  → Log warning
  → Skip mappe
  → Continue scanning

- Brugeren pressede "Cancel"
  → Stop gracefully
  → Return partial result
  → No corruption
```

**Implementering:**
- Try-catch omkring kritiske operationer
- Differentiere mellem kritiske fejl (stop) og tolerabile fejl (skip)
- Collect alle fejl i en liste
- Return `BackupJobResult` med error-liste

---

#### 5. **Progress Reporting**

**Hvad betyder det?**
UI'et skal vide hvad der sker. Vi rapporterer:
- Hvor mange filer er scanned
- Hvor mange bytes er transfered
- Hvilken fil arbejdes der på nu
- Estimeret tid til færdig

**Eksempel:**
```
Backup Progress:
  Scanned: 150 files, 2.4 GB total
  Transferred: 45 files, 800 MB
  Current file: beach.jpg (3.1 MB)
  Speed: 25 MB/s
  ETA: 5 minutes remaining
  Status: In progress
```

**Implementering:**
- Brug `IProgress<IBackupProgress>` interface
- Report efter hver file-transfer
- Report periodisk under transfer (f.eks. per MB)
- Beregn ETA baseret på speed

---

### 🟡 **Extended Features (SHOULD HAVE)**

Disse features gør Core4 **robust og powerful**. Systemet virker uden dem, men er ikke fuld-featured.

#### 6. **Hash-baseret Integrity Verification** (OPTIONAL - Feature Flag)

**Hvad betyder det?**
Vi beregner en "fingerprint" (SHA256) for hver fil før og efter transfer. Hvis fingerprintet ændrer sig, blev filen korrupt under transfer.

**Eksempel:**
```
Source file: vacation.jpg
  → SHA256 hash: "a1b2c3d4e5f6..."

Transfer to: D:\Backups\vacation.jpg
  → SHA256 hash: "a1b2c3d4e5f6..." ✅ Match!

Sidecar updated med hashes:
{
  "source_path": "...",
  "source_hash_sha256": "a1b2c3d4e5f6...",
  "dest_hash_sha256": "a1b2c3d4e5f6...",
  "hash_match": true,
  "verified": true
}
```

**Hvorfor er det vigtigt?**
- Detekterer bitflips, USB-driver fejl, dårlig RAM
- Giver dig tillid til at backup'en er intakt
- Enables "Verify Backup" operation senere

**VIGTIG NOTE fra Core3 Review:**
Core3 **forces** 3 hash-typer altid (SHA2_256, SHA3_256_FIPS202, BLAKE3_256). Dette er **langsomt**!
Core4 skal gøre det **optional** via BackupPlan:
```csharp
public List<HashType>? HashTypes { get; init; } = null;  // null = no hashing

// In engine:
if (plan.HashTypes?.Count > 0)
{
    items = await GenerateHashes(...);  // Only if requested
}
```

**Implementering:**
- If HashTypes specified: Compute SHA256 (og andre) på source-fil før transfer
- Compute SHA256 på dest-fil efter transfer
- Store begge i sidecar
- Compare dem

---

#### 7. **Metadata Extraction** (OPTIONAL - Feature Flag)

**Hvad betyder det?**
Vi ekstrakter info **om** filerne uden at ændre dem:
- EXIF-data fra billeder (hvornår blev foto taget?)
- Created/modified/accessed dates
- File attributes
- Camera model, ISO, shutter speed, osv.

**Eksempel:**
```
Image file: beach.jpg
  EXIF date: 2024-06-16 14:30:00
  Camera: Canon EOS 5D Mark IV
  ISO: 400
  Shutter: 1/1000
  
Sidecar updated:
{
  "metadata": {
    "exif_date": "2024-06-16T14:30:00Z",
    "camera_model": "Canon EOS 5D Mark IV",
    "iso": 400,
    "shutter_speed": "1/1000"
  }
}
```

**Hvorfor er det vigtigt?**
- Builds timeline: "Alle billeder fra juni"
- Forensics: "Hvornår blev denne foto taget?"
- Restore: Du ved når originalen blev lavet

**Implementering:**
- Brug EXIF-parser for billeder
- Brug FileInfo for standard Windows-attributes
- Store i sidecar

---

#### 8. **Timestamp Correction**

**Hvad betyder det?**
Hvis EXIF siger billedet blev taget 2024-06-16, men filen blev modificeret 2024-06-20, nulstiller vi timestamp til EXIF-datoen.

**Eksempel:**
```
Original file: beach.jpg
  EXIF date: 2024-06-16 14:30:00
  Modified date: 2024-06-20 (fordi den blev editeret)

After backup with timestamp correction:
  Backup file: beach.jpg
  Modified date: 2024-06-16 14:30:00 (restored from EXIF)
```

**Hvorfor er det vigtigt?**
- Timeline er korrekt: "Juni billeder vises på juni-datoen"
- Authenticity: Originale timestamps bliver preserveret
- Forensics: Tiden på filerne matcher virkelighed

**Implementering:**
- Find EXIF-dato hvis tilgængelig
- Set fil's modified-date til EXIF-dato
- Hvis ingen EXIF, brug source-modified-date

---

#### 9. **Limited Parallelism for Filesystem**

**Hvad betyder det?**
I stedet for at backup'e en fil ad gangen, kan vi backup'e 3-4 filer **samtidigt**. Dette gør backup'en meget hurtigere.

**Sequential (langsomt):**
```
Transfer file 1 (5 sec)
Transfer file 2 (5 sec)
Transfer file 3 (5 sec)
Total: 15 sec
```

**Limited Parallel (hurtigt):**
```
Transfer file 1 (5 sec) ⟶ Hash file 1
Transfer file 2 (5 sec) ⟶ Hash file 2
Transfer file 3 (5 sec) ⟶ Hash file 3
Total: 7 sec (approx)
```

**Hvorfor kun for Filesystem?**
MTP-protokollen (iPhone, Android) bliver ustabil under parallelisme. Jeg kan læse én fil ad gangen, men hvis jeg prøver parallelt, crasher device'et.

Filesystem kan håndtere mange samtidige operationer uden problemer.

**Implementering:**
- Brug `System.Threading.Channels.Channel<T>`
- Multi-threaded producer-consumer
- Thread-pool size = CPU cores (eller less)
- Monitor queue depth (ikke buffer uendelig)

---

#### 10. **Source-Type Detection (Adaptive Strategy)**

**Hvad betyder det?**
Engine'en skal automatisk detektere **hvad** du backup'er:
```
if (source is MTP device)
  → Use SequentialBackupEngine
else if (source is Filesystem)
  → Use LimitedParallelBackupEngine
```

**Eksempel:**
```
User: "Backup fra iPhone"
→ Engine detects: MTP-source
→ Uses Sequential strategy: ✅ Stable

User: "Backup fra C:\Pictures"
→ Engine detects: Filesystem
→ Uses Parallel strategy: ✅ Fast
```

**Implementering:**
- `BackupEngineFactory` inspekterer BackupPlan
- Checks `SourceType` enum
- Returns rigtig engine

---

#### 11. **Device Management for MTP**

**Hvad betyder det?**
Brugeren skal kunne:
- Se liste af tilsluttede devices (iPhone, Android, kamera)
- Vælge hvilken device at backup'e fra
- Handle hvis device bliver disconnected

**Eksempel CLI:**
```
$ dotnet run backup --device "Apple iPhone"
$ dotnet run backup --device "Samsung Galaxy S21"

List devices:
$ dotnet run list-devices
Connected devices:
  1. Apple iPhone (Model A2341, Serial: ABC123)
  2. Canon EOS 5D Mark IV (Model 1234, Serial: DEF456)
```

**Implementering:**
- Query `MediaDevices.dll` for liste af devices
- Let user select by name
- Handle disconnect gracefully

---

#### 12. **Advanced Sidecar Features**

**Hvad betyder det?**
I stedet for kun minimal sidecar, kan vi lave:

- Per-file sidecars med hashes, metadata, verification
- Directory-level sidecar (summary for hele backup-run)
- Encryption metadata (hvis vi senere tilføjer encryption)

**Eksempel directory-sidecar:**
```json
{
  "backup_id": "backup-2026-05-05-001",
  "started_at": "2026-05-05T12:15:00Z",
  "completed_at": "2026-05-05T12:45:30Z",
  "total_files": 150,
  "total_bytes": 2684354560,
  "successful_files": 150,
  "failed_files": 0,
  "source": "iPhone",
  "strategy": "sequential",
  "files": [
    {
      "filename": "vacation.jpg",
      "size": 2621440,
      "hash": "a1b2c3d4...",
      "verified": true
    }
  ]
}
```

---

### 🟢 **Nice-to-Have Features (COULD HAVE)**

Disse features er luksus og kan tilføjes senere.

#### 13. **Delta / Incremental Backup**
Kun backup filer der er **nyere** end sidste backup.

#### 14. **Compression**
Gzip files under transfer for at spare båndbredde.

#### 15. **Encryption**
AES-256 encryption af transferred files.

#### 16. **Parallel Hashing**
Hash flere files samtidigt (hvis CPU-bound).

#### 17. **Bandwidth Throttling**
Limit transfer-hastighed til X MB/s.

#### 18. **Backup Scheduling**
Cron-like scheduling - "Backup hver dag kl. 22:00".

#### 19. **Database of Backups**
SQLite database der tracker alle backup-runs.

#### 20. **Verify Mode**
Re-hash alle files efter backup for at sikre integritet.

---

## Implementation Strategy

### Hvad betyder "Implementation Strategy"?

En plan for **hvordan** vi bygger Core4. Det er ikke en todolist - det er **processen**.

En strategi betyder vi deler arbejdet i **overskuelige faser** hvor hver fase:
- ✅ Producerer **fungerende kode**
- ✅ Har **testable komponenter**
- ✅ Kan **integreres** med eksisterende systemer
- ✅ Kan **shippes** til brugere (hvis den første fase)

Vi gør det **ikke** som at lave alt på én gang og håbe det virker.

### Fase 1: Core Engine (Sprint 1-2)

**Mål:** En **fungerende** backup engine. Simpel, men virker.

**Hvad skal laves:**

1. **Project Setup**
   - Opret `BMTP3.Core4` projekt i Visual Studio
   - Opret folder-struktur (`Api/`, `Scanner/`, `Transfer/`, `Sidecar/`, `Engine/`, `DependencyInjection/`)
   - Opret test-projekt `BMTP3.Core4.Tests`

2. **API Layer** (Kontrakter)
   - Define `IBackupItem` record
   - Define `IBackupEngine` interface
   - Define `BackupPlan` record
   - Define `BackupJobResult` record
   - Define `IBackupProgress` interface
   - Define `ScanContext`, `TransferContext`, `SidecarContext`

3. **Filesystem Scanner**
   - Implement `IItemScanner`
   - `FilesystemItemScanner` - enumerer lokale filer
   - Unit test: Kan den find filer rekursivt?

4. **Filesystem Transfer**
   - Implement `IFileTransfer`
   - `FilesystemFileTransfer` - kopier fil til destination
   - Unit test: Kan den kopiere en fil?
   - Unit test: Kan den håndtere disk-full fejl?

5. **Basic Sidecar**
   - Implement `ISidecarGenerator`
   - `JsonSidecarGenerator` - lav minimal JSON
   - Unit test: Kan den lave en sidecar?

6. **Sequential Engine**
   - Implement `BackupEngine` koordinerer: Scan → Transfer → Sidecar
   - Implement basic error handling
   - Implement progress reporting

7. **DI Setup**
   - `ServiceCollectionExtensions.AddBMTP3Core4()`
   - Register implementationer

8. **Integration Tests**
   - Test: End-to-end filesystem backup (scan real folder, transfer to temp, generate sidecars)
   - Test: Error handling (simulate disk full)
   - Test: Cancellation (brugeren siger "stop!")

9. **CLI Integration**
   - Hook Core4 engine i `BMTP3.Consoles`
   - Kan brugeren køre: `dotnet run backup --source C:\test --output C:\output`?

**Deliverable:**
```
✅ Core4 kan backup'e fra filesystem til output dir
✅ Sidecars genereres for hver fil
✅ Errors håndteles gracefully
✅ Progress rapporteres
✅ 50+ integration tests passing
✅ CLI integration fungerer
```

---

### Fase 2: Hash & Metadata (Sprint 3-4)

**Deliverable:** Verified backups med metadata.

**Steps:**
1. Implement SHA256 Hasher
2. Implement Basic Metadata Reader (file attributes)
3. Extend Sidecar to include hashes + metadata
4. Implement Verification phase
5. Add tests for each component

**Result:** Core4 kan verify file integrity, store metadata.

---

### Fase 3: Advanced Features (Sprint 5+)

**Deliverable:** Production-ready engine.

**Steps:**
1. Implement Limited Parallelism for Filesystem
2. Implement MTP Scanner
3. Implement MTP Transfer
4. Implement Adaptive Strategy (detect + select engine)
5. Implement Device enumeration
6. Add EXIF metadata reader
7. Add timestamp correction
8. Create comprehensive test suite
9. Performance tuning + optimization

**Result:** Core4 kan handle både filesystem og MTP, parallel eller sequential, med full metadata.

---

## Design Decisions

### 1. **Immutable Data Flow**
- Items flow through pipeline without mutations
- Each phase returns a new item with additional data
- Enables simple reasoning + testing

### 2. **Fail-Fast on Critical, Tolerate on Optional**
- **Critical errors**: Transfer failures → stop backup
- **Optional errors**: Hash failure, metadata failure → log, continue
- Configurable via `BackupPlan.ErrorHandlingStrategy`

### 3. **Progress Reporting via IProgress<T>**
- UI can subscribe to progress updates
- Percentage, current file, ETA, etc.
- No dependency on UI framework

### 4. **DI for All Major Components**
- Easy to mock for testing
- Easy to swap implementations (Filesystem ↔ MTP)
- Supports future extensibility

### 5. **Cancellation-Aware**
- All async operations accept `CancellationToken`
- Graceful cancellation at phase boundaries
- Clean resource cleanup

### 6. **Sidecar as Source of Truth**
- Sidecar contains all backup metadata
- Enable restore without re-reading source
- Enable audit trail + forensics

---

## Testing Strategy

### Unit Tests

**Per component:**
- Scanner: Test enumeration logic, directory handling
- Transfer: Test copy logic, error handling
- Hasher: Test hash computation
- Metadata Reader: Test extraction logic
- Sidecar: Test serialization/deserialization

### Integration Tests

**End-to-end scenarios:**
- Filesystem backup complete flow
- MTP backup complete flow
- Error handling (disk full, permissions, etc.)
- Cancellation at various phases
- Large file handling
- Deep directory structures

### Performance Tests

- Baseline transfer speed
- Parallelism efficiency
- Memory usage under load

---

## Known Risks & Mitigations

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| MTP instability under parallelism | High | Backup failure | Use Sequential strategy for MTP |
| Hash computation too slow | Medium | Poor UX | Make optional, parallelize if needed |
| Memory overflow from buffering | Low | Crash | Limit queue sizes, use streaming |
| Complex DI setup | Medium | Maintainability | Start simple, document patterns |
| Performance regression vs Core2 | Medium | User complaint | Profile early, optimize bottlenecks |

---

## Success Criteria

- ✅ Core4 can backup filesystem to output dir with sidecars
- ✅ All Core2 features implemented (hash, metadata, verification, timestamps)
- ✅ Adaptive strategy (Sequential for MTP, Parallel for FS)
- ✅ 100+ integration tests passing
- ✅ CLI integration in Consoles
- ✅ Performance >= Core2 on filesystem
- ✅ MTP stable (no random failures)
- ✅ Clear, maintainable codebase

---

## File Structure (Expected)

```
BMTP3.Core4/
├─ Api/
│  ├─ IBackupEngine.cs
│  ├─ BackupPlan.cs
│  ├─ BackupJobResult.cs
│  ├─ IBackupProgress.cs
│  └─ ... (other DTOs)
├─ Scanner/
│  ├─ IItemScanner.cs
│  ├─ FilesystemItemScanner.cs
│  ├─ MTPItemScanner.cs
│  └─ TestScanner.cs
├─ Transfer/
│  ├─ IFileTransfer.cs
│  ├─ FilesystemFileTransfer.cs
│  ├─ MTPFileTransfer.cs
│  └─ TestTransfer.cs
├─ Sidecar/
│  ├─ ISidecarGenerator.cs
│  ├─ JsonSidecarGenerator.cs
│  └─ TestSidecar.cs
├─ Hashing/ (Phase 2)
│  ├─ IItemHasher.cs
│  └─ SHA256Hasher.cs
├─ Metadata/ (Phase 2)
│  ├─ IMetadataReader.cs
│  ├─ ExifMetadataReader.cs
│  └─ CommonMetadataReader.cs
├─ Verification/ (Phase 2)
│  ├─ IIntegrityVerifier.cs
│  └─ IntegrityVerifier.cs
├─ Engine/
│  ├─ IBackupStrategy.cs
│  ├─ BackupEngineFactory.cs
│  ├─ SequentialBackupEngine.cs
│  └─ LimitedParallelBackupEngine.cs
└─ DependencyInjection/
   └─ ServiceCollectionExtensions.cs
```

---

## Dokumentation & Kommunikation

- **This plan** is the source of truth for Core4 architecture
- **Code comments** explain "why", not "what"
- **PR descriptions** reference this plan + specific features
- **Tests serve as living documentation** of expected behavior

---

## Afsluttende Notater

Core4 er designet til at være:
- **Simple nok** til at vedligehold
- **Fleksibel nok** til at udvide
- **Robust nok** til production
- **Testbar nok** til høj test-coverage

Start med Fase 1 (Core engine), få det virket, så udvid i Fase 2+.

SUCCESS = Fungerende, vedligeholdt, udvidbar backup engine. 🎯
