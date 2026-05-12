# Core4 – Komplet Lessons Learned & Implementeringsguide

**Version:** 1.0  
**Dato:** 2026-05-11  
**Formål:** Dette dokument samler alle lessons learned fra Core, Core2 og Core3 og kobler dem direkte til designbeslutninger og implementeringskrav i Core4. Det er tænkt som den primære reference under implementering af Core4.

---

## Indholdsfortegnelse

1. [Baggrund – Hvorfor Core4?](#1-baggrund--hvorfor-core4)
2. [Arkitekturel Vision for Core4](#2-arkitekturel-vision-for-core4)
3. [Lessons fra Core (Legacy)](#3-lessons-fra-core-legacy)
4. [Lessons fra Core2 (Parallel Pipeline)](#4-lessons-fra-core2-parallel-pipeline)
5. [Lessons fra Core3 (Simpel Sequential)](#5-lessons-fra-core3-simpel-sequential)
6. [Samlet Lessons Learned Tabel](#6-samlet-lessons-learned-tabel)
7. [Core4 Designbeslutninger – Drevet af Lessons](#7-core4-designbeslutninger--drevet-af-lessons)
8. [Hvad er implementeret i Core4 skelettet](#8-hvad-er-implementeret-i-core4-skelettet)
9. [Hvad mangler – og i hvilken rækkefølge](#9-hvad-mangler--og-i-hvilken-rækkefølge)
10. [Detaljeret teknisk guide per modul](#10-detaljeret-teknisk-guide-per-modul)
11. [Kritiske edge cases og pitfalls](#11-kritiske-edge-cases-og-pitfalls)
12. [Anbefalinger og gode mønstre](#12-anbefalinger-og-gode-mønstre)

---

## 1. Baggrund – Hvorfor Core4?

### Historik

Vi har over tid udviklet tre versioner af backup-motoren:

- **Core (Original):** Fungerede i starten, men blev for monolitisk og kompleks at vedligeholde. Bruger TOML-konfiguration og "God objects". Virker ikke korrekt mere.
- **Core2 (Multithreaded):** Forsøgte at løse Core's performance-problemer med parallel pipeline og System.Threading.Channels. **Problem:** Aldrig stabilt. Race conditions, deadlocks og uforudsigelig adfærd særligt med MTP-devices. Koden er næsten umulig at debugge.
- **Core3 (Simple Sequential):** Gik tilbage til basics – enkelt, sekventielt, forståeligt. **Det virker!** Men er langsomt for store backups, og mangler features.

### Core4's Mission

**"Byg det simpelt. Byg det rigtigt. Optimer intelligent."**

Core4 kombinerer det bedste fra alle tre:
- ✅ **Simpelt at forstå** (som Core3) – lineær flow, let at debugge
- ✅ **Alle features** (som Core2) – hash, metadata, EXIF, verifikation
- ✅ **Intelligent performance** – parallel ved filsystem, sekventiel ved MTP
- ✅ **Vedligeholdeligt** – nyt, rent codebase uden legacy-ballast

---

## 2. Arkitekturel Vision for Core4

### Hybrid Strategi

```
Hvis kilden er MTP-device:
  → Brug SequentialBackupEngine (simpelt, stabilt)

Hvis kilden er Filesystem:
  → Brug LimitedParallelBackupEngine (hurtigt, men kontrolleret)
```

### Pipeline Flow

```
BackupPlan (Input)
    ↓
[Validation]
    ↓
[Source Detection: MTP vs Filesystem]
    ↓
┌──────────────────────────────────────┐
│ MTP → SequentialBackupEngine         │
│ FS  → LimitedParallelBackupEngine    │
└──────────────────────────────────────┘
    ↓
Fase 1: Scan    → Enumerate items, byg BackupItem liste
Fase 2: Transfer → Kopiér filer til destination
Fase 3: Sidecar  → Opret .json sidecar umiddelbart efter transfer
    ↓
Valgfrie faser (hvis aktiveret i BackupPlan):
Fase 4: Hashing        → SHA256/BLAKE3 per fil
Fase 5: Metadata       → EXIF, QuickTime, fil-attributter
Fase 6: Verification   → Re-hash destination, sammenlign
Fase 7: Timestamps     → Gendan originale tidsstempler
    ↓
BackupResult (Output)
```

### Tier-prioritering (fra CORE4_ARCHITECTURE_en.md)

**Tier 1 (Fundamentalt – MTP-blokerende):**
- IBackupEngine, SequentialBackupEngine, ProgressTracker
- FilesystemItemScanner, MTPItemScanner
- FilesystemFileTransfer, MTPFileTransfer
- BackupPlan, IBackupItem, BackupResult, IBackupProgress
- ISidecarGenerator (minimal)
- DI + Error handling + Tests

**Tier 2 (Vigtigt – UX & Brugervenlighed):**
- IProgressNotifier, SpectreProgressNotifier
- OutputStructure, CollisionResolution, DryRun

**Tier 3 (Nice-to-have – Avanceret):**
- IItemHasher, SHA256Hasher
- IMetadataReader, ExifMetadataReader
- IIntegrityVerifier
- ITimestampCorrector
- LimitedParallelBackupEngine (KUN FS, aldrig MTP)

**Tier 4 (Mindst vigtigt – Polish):**
- Performance-tuning, Resume, Scheduling, GUI
- ❌ ALDRIG: Fuld parallelisme for MTP (Core2's fejl)

---

## 3. Lessons fra Core (Legacy)

### 3.1 Backup-flow og Orkestrering

**Problem:** `BackupHandler` og `BackupMaster` er "God objects" – de håndterer kildevalg, destination, filiterering, tilstand, kopiering, sammenligning og sidecar-generering alt i én klasse.

**Konsekvens:** Svær at teste, svær at vedligeholde, umulig at udvide enkeltdele uden at bryde hele klassen.

**Core4-løsning:** Hvert ansvar er i sin egen klasse/interface:
- `IBackupScanner` – scanning alene
- `IFileTransfer` – transfer alene  
- `ISidecarGenerator` – sidecar alene
- `IBackupEngine` – orkestrering alene

---

### 3.2 Scanning og Metadata

**Problem:** Metadata-udtræk sker på ALLE filer – også ikke-billeder – hvilket giver unødvendige exceptions og fejl.

**Problem:** `ImageMetadataReader` kaster exceptions på korrupte filer eller filer uden metadata.

**Problem:** QuickTime-metadata kan returnere EPOCH-dato (1970-01-01) hvis feltet er tomt.

**Core4-løsning:**
- Filtrer på filtype FØR metadata-udtræk
- Wrappet metadata-udtræk i try/catch med logging – aldrig propagér exception opad
- Valider datoer: Ignorer datoer < 2000-01-01 og > Now+1år som sandsynligvis EPOCH eller klokkefejl
- Metadata-udtræk er VALGFRIT (EnableMetadata = false default)

---

### 3.3 Transfer og Sidecar

**Problem:** Sidecar-logik er tæt koblet til backup-flowet og hardkodet til INI-format.

**Problem:** Manglende cleanup af temp-filer ved fejl eller afbrydelse.

**Problem:** Risiko for inkonsistens mellem fil og sidecar ved fejl midt i processen.

**Core4-løsning:**
- Sidecar genereres UMIDDELBART efter transfer (ikke til sidst)
- Sidecar er JSON-format (udvidbart via ISidecarGenerator interface)
- Temp-fil cleanup i finally-blok
- Sidecar indeholder transfer-status, så delvis succes dokumenteres

---

### 3.4 Error Handling

**Problem:** Fejl håndteres med fail-fast – én fil fejler og hele backup stopper.

**Problem:** Exceptions kan maskere den originale fejl, hvis cleanup også fejler.

**Problem:** Ingen central logging eller rapportering.

**Core4-løsning:**
- Transfer-fejl → mark item som Failed, FORTSÆT med næste fil (ikke throw)
- Struktureret `BackupErrorCode` enum for alle kendte fejltyper
- `BackupResult.FilesFailed` tæller fejlede filer
- Central logging via `ILogger<T>` i alle services

---

### 3.5 State Management

**Problem:** `BackupItem` bruger nullable properties til at repræsentere pipeline-stadier, hvilket giver implicit og usikker tilstand.

**Problem:** Risiko for NullReferenceExceptions og svær fejlfinding.

**Core4-løsning:**
- Eksplicit `BackupItemStatus` enum: `Pending`, `Transferred`, `Failed`, `Skipped`, `Verified`
- `BackupSessionState` ejer alle items og phase-tracking
- Phase-overgange via tydslige metoder: `SetPhase()`, `Fail()`, `Cancel()`, `Complete()`

---

### 3.6 DI og Afhængigheder

**Problem:** DI anvendes, men mange afhængigheder injiceres direkte i store klasser.

**Core4-løsning:**
- Alt er interface-baseret
- DI-konfiguration i `ServiceCollectionExtensions` i Core4 projektet
- Consoles-projektet registrerer de specifikke implementeringer

---

## 4. Lessons fra Core2 (Parallel Pipeline)

### 4.1 Deadlock-risiko (KRITISK)

**Problem:** Core2 bruger System.Threading.Channels med bounded capacity og multiple concurrent writers.

```
Writer Thread A → Channel (fuld) → blokerer
Writer Thread B → Channel (fuld) → blokerer
Reader → venner et andet sted
= Cirkulær deadlock
```

**Core4-løsning:**
- Undgå komplekse channel-pipelines
- SequentialBackupEngine: Direkte metodekald, ingen channels
- LimitedParallelBackupEngine: Kun ét niveau af parallelisme med `SemaphoreSlim` – ingen bounded channels

---

### 4.2 Race Conditions i ProgressTracker (KRITISK)

**Problem:** Core2's ProgressTracker deles af multiple worker-threads uden synkronisering.

**Konsekvens:** Inkonsistente progress-snapshots (fx "150% done"), tabte opdateringer.

**Core4-løsning:**
- ProgressTracker bruger `Interlocked.Increment/Add` for tællere
- Snapshots tages atomisk
- `IBackupProgress` er et immutabelt snapshot (record)

---

### 4.3 MTP Session Race Condition (KRITISK)

**Problem:** MTP-sessionen holdes kun åben indtil buffering er færdig. Hvis der er delay mellem buffering og brug af temp-filer → session-fejl.

**Core4-løsning:**
- MTP-session holdes åben under HELE scanning + transfer
- Explicit session-lifecycle management med `using` eller `try/finally`
- Timeout-strategi: 30 sek connect, 5 min per-file transfer

---

### 4.4 Manuel Task-orkestrering (KRITISK)

**Problem:** Core2 kæder 9+ tasks manuelt. Én hængt task → hele pipeline hænger.

**Core4-løsning:**
- SequentialBackupEngine: `await foreach` over scanner, direkte metodekald per item
- Ingen manuel task-kædering
- Klart, lineært flow der er let at debugge

---

### 4.5 Implicit Error-politikker

**Problem:** Hvad sker der hvis metadata-udtræk fejler? Implicit: skip og fortsæt. Men brugeren kan ikke konfigurere det.

**Core4-løsning:**
- `BackupPlan.StopOnError` (bool) for transfer-fejl
- Metadata/hash/verify fejl er ALTID non-fatal (log og fortsæt)
- Transfer-fejl: konfigurerbart via `StopOnError`

---

### 4.6 DryRun Ineffektivitet

**Problem:** Core2's DryRun springer I/O over, men downloader stadig fra MTP, beregner 7 hashes og læser metadata. DryRun er næsten lige så langsomt som en rigtig kørsel.

**Core4-løsning:**

| Operation                | DryRun? | Kommentar |
|--------------------------|---------|-----------|
| File scanning            | JA      | Bruger skal kende scope |
| Directory enumeration    | JA      | Skal finde filer |
| File size calculation     | JA      | Til progress/warnings |
| Directory oprettelse     | NEJ     | Simuleret, logges |
| File transfer            | NEJ     | Ingen I/O |
| Sidecar oprettelse       | NEJ     | Simuleret, logges |
| Hash beregning           | NEJ     | Dyrt, ingen pointe |
| Metadata-udtræk          | NEJ     | Dyrt, ingen pointe |
| Verifikation             | NEJ     | Ingen overførte filer |

---

### 4.7 Resume-infrastruktur der ikke virker

**Problem:** Core2 gemmer session-state til disk for resume-support. Men der er ingen kode til at genoptage fra gemt state. Backup crasher → start forfra.

**Core4-løsning:**
- Session-state infrastruktur er bygget (`IBackupSessionStateStore`, `BackupSessionState`)
- **Implementér resume fuldt ud ELLER fjern det** – halvfærdigt er værre
- I første omgang: `InMemoryBackupSessionStateStore` (ingen persistens) er fint
- Persisteret resume er Tier 4

---

### 4.8 Inkonsistente Cancellation-mønstre

**Problem:** Core2 bruger tre forskellige mønstre på samme tid:
- `ct.ThrowIfCancellationRequested()`
- `catch(OperationCanceledException)`
- `if(ct.IsCancellationRequested)`

**Core4-løsning (ét mønster, konsekvent overalt):**
```csharp
// I loop: ThrowIfCancellationRequested ved starten af hvert iteration
cancellationToken.ThrowIfCancellationRequested();

// I async operationer: Videresend token til alle async kald
await scanner.ScanAsync(plan, cancellationToken);

// Top-level: Catch OperationCanceledException ét sted (i BackupEngine)
catch (OperationCanceledException)
{
    session.Cancel();
    // Returner result med Cancelled-status
}
```

---

### 4.9 Redundante Result-felter

**Problem:** Core2's BackupJobResult har både `GlobalErrors (List<string>)` og `GlobalError (string?)` – uklart hvornår hvad bruges.

**Core4-løsning:**
- `BackupResult` har kun ét fejlfelt: `BackupErrorCode? FailureReason` for fatale fejl
- Per-fil fejl håndteres via `BackupItem.Status = Failed` og `FilesFailed` tæller

---

## 5. Lessons fra Core3 (Simpel Sequential)

### 5.1 Bug: Directory-struktur går tabt

**Problem:** Core3's scanner sætter `DestinationPath = fileName` (bare filnavnet). Resultat: `Folder1/File1.txt` og `Folder2/File1.txt` bliver begge til `File1.txt` → kollision!

**Core4-løsning:**
```csharp
// FORKERT (Core3):
string destinationPath = Path.GetFileName(item.SourcePath);

// RIGTIGT (Core4):
string destinationPath = Path.Combine(plan.Destination, item.RelativePath);
// item.RelativePath = "Folder1/File1.txt" (relativt til source-root)
```

---

### 5.2 Bug: Transfer-fejl stopper hele backup

**Problem:** Core3 kaster exception ved transfer-fejl → backup stopper.

**Core4-løsning:**
```csharp
// FORKERT (Core3):
File.Copy(source, dest); // Kaster ved fejl

// RIGTIGT (Core4):
TransferResult result = await fileTransfer.TransferAsync(item, destPath, progress, ct);
if (!result.Success)
{
    item.Status = BackupItemStatus.Failed;
    logger.LogError("Transfer failed: {Path} - {Error}", item.RelativePath, result.ErrorMessage);
    // FORTSÆT med næste fil
    continue;
}
```

---

### 5.3 Bug: Destination-mappe skal eksistere

**Problem:** Core3 crasher hvis output-mappen ikke eksisterer.

**Core4-løsning:**
```csharp
// I BackupEngine eller FileTransfer:
Directory.CreateDirectory(destinationDirectory);
// CreateDirectory er idempotent – fejler ikke hvis mappen allerede eksisterer
```

---

### 5.4 Bug: Progress Reporting ufuldstændig

**Problem:** `IBackupProgress.BytesTransferred` er defineret i interface men ikke implementeret i Core3.

**Core4-løsning:**
- `IBackupProgress.BytesProcessed` er fuldt implementeret
- `IFileProgress` tracker per-fil bytes
- `ActiveFiles` liste viser hvad der er aktivt

---

### 5.5 Bug: DryRun opretter ikke mappestruktur

**Problem:** Core3's DryRun opretter ikke mapper, så metadata/hash-faser fejler.

**Core4-løsning:**
- DryRun simulerer mappestruktur (logger "Would create: X") men hopper over I/O
- Metadata/hash/verify hopper HELT over i DryRun

---

### 5.6 Design Flaw: Error handling strategi er implicit

**Problem:** Transfer-fejl er kritiske (stopper), metadata-fejl er tolererede (fortsætter). Men der er ingen måde at konfigurere det.

**Core4-løsning:**
```csharp
// I BackupPlan:
public bool StopOnError { get; init; } // Gælder transfer-fejl

// Metadata/Hash/Verify er ALTID non-fatal:
// Fejl logges, men backup fortsætter
```

---

### 5.7 Design Flaw: Hashing altid tvunget (langsomt)

**Problem:** Core3 beregner 3 hash-typer ALTID, selvom brugeren ikke vil have dem.

**Core4-løsning:**
```csharp
// I BackupPlan:
public bool EnableHashing { get; init; } // Default: false
// null = ingen hashing
```

---

### 5.8 Design Flaw: Sidecars genereres til sidst

**Problem:** Hvis hashing/metadata fejler, genereres sidecar aldrig. En fil kan være overført men have ingen sidecar.

**Core4-løsning:**
- **Minimal sidecar genereres UMIDDELBART efter transfer**
- Sidecar opdateres bagefter med hashes/metadata hvis aktiveret
- Selv ved fejl har hver overført fil en sidecar

---

### 5.9 Design Flaw: Collision resolution ikke testbar

**Problem:** CollisionStrategy håndteres implicitly i scanner og transfer.

**Core4-løsning:**
- Separat `CollisionStrategy` enum: `Skip`, `Overwrite`, `Rename`
- Håndteres eksplicit i `FilesystemFileTransfer` baseret på `BackupPlan.CollisionStrategy`

---

## 6. Samlet Lessons Learned Tabel

| Kategori | Core (Legacy) | Core2 (Pipeline) | Core3 (Simpel) | Core4 (Løsning) |
|----------|--------------|------------------|----------------|-----------------|
| **Arkitektur** | God objects, monolitisk | Channel pipeline, 9+ tasks | Sekventiel, modulær | Hybrid: sequential (MTP) + limited parallel (FS) |
| **Fejlhåndtering** | Fail-fast, stopper | Kompleks, implicit | Kritisk/ikke-kritisk | Eksplicit per operation, struktureret |
| **Error reporting** | Ingen central logging | GlobalErrors + FileErrors (forvirret) | Simpel liste | BackupErrorCode enum, struktureret |
| **State management** | Nullable properties, implicit | Kompleks, shared mutable | Enkel, eksplicit | BackupItemStatus enum, BackupSessionState |
| **MTP stabilitet** | N/A | Deadlocks, race conditions | N/A | Sequential only, explicit session lifecycle |
| **Parallelisme** | Ingen | Channels, ubounded | Ingen | LimitedParallel med SemaphoreSlim (FS only) |
| **Progress** | Minimal | Race conditions | Ufuldstændig | IBackupProgress + IFileProgress, thread-safe |
| **Sidecar** | INI, tæt koblet | JSON, pipeline-koblet | Genereres til sidst | JSON, genereres umiddelbart efter transfer |
| **DryRun** | N/A | Dyrt (downloader fra MTP) | Mangelfuldt | Fuldt simuleret, springer I/O over |
| **Cancellation** | Inkonsistent | 3 mønstre | Inkonsistent | Ét mønster overalt |
| **Directory-struktur** | Delvist bevaret | Bevaret | TABT (bug!) | RelativePath bevaret altid |
| **Destination-mappe** | Skal eksistere | Oprettes | CRASHER | Auto-oprettes |
| **Hashing** | Altid | Altid (7 typer) | Altid (3 typer) | Valgfrit (EnableHashing = false default) |
| **Metadata** | Altid, fejlbehæftet | Valgfrit | Valgfrit | Valgfrit, fejl er non-fatal |
| **Testbarhed** | Lav | Medium | Høj | Høj (interfaces overalt) |
| **Resume** | Ingen | Halv-implementeret | Ingen | InMemory (fuldt resume er Tier 4) |

---

## 7. Core4 Designbeslutninger – Drevet af Lessons

### 7.1 "Return result, never throw" for file-level errors

**Beslutning:** Transfer, hashing og metadata kaster ALDRIG exceptions til engine-laget. De returnerer altid et result-objekt.

**Rationale:** 
- Core og Core3's fail-fast stopper backup ved første fejl
- Core2's implicit exception-forwarding giver uklare fejlscenarier

**Implementering:**
```csharp
// ALDRIG dette:
void TransferFile(BackupItem item) { throw new IOException("Disk full"); }

// ALTID dette:
Task<TransferResult> TransferAsync(BackupItem item, ...) 
{
    try 
    { 
        // ... kopier fil ...
        return new TransferResult(Success: true, ...);
    }
    catch (IOException ex)
    {
        logger.LogError(ex, "Transfer failed: {Path}", item.RelativePath);
        return new TransferResult(Success: false, ErrorMessage: ex.Message, ...);
    }
}
```

---

### 7.2 Sidecar genereres umiddelbart efter transfer

**Beslutning:** Minimal sidecar oprettes KUN for filer der er succesfuldt transfereret. Det sker umiddelbart efter transfer – ikke til sidst.

**Rationale:**
- Core3: Sidecar mangler hvis hashing fejler
- En transfereret fil skal ALTID have en sidecar (audit trail)

**Implementering:**
```csharp
// I SequentialBackupEngine:
TransferResult transfer = await fileTransfer.TransferAsync(item, destPath, progress, ct);
if (transfer.Success)
{
    item.Status = BackupItemStatus.Transferred;
    await sidecarGenerator.GenerateAsync(item, transfer, plan, ct); // Umiddelbart!
}
```

---

### 7.3 MTP = Sequential, altid

**Beslutning:** MTP-backup kører ALTID sekventielt. Ingen parallelisme, heller ikke "begrænset".

**Rationale:**
- Core2's parallel MTP-kørsel gav deadlocks og device disconnects
- MTP-protokollen er designet til enkelt-trådet adgang

---

### 7.4 RelativePath er hellig

**Beslutning:** `BackupItem.RelativePath` sættes af scanner og må ALDRIG ændres af engine eller transfer.

**Rationale:**
- Core3 mister mappestrukturen pga. denne bug

**Implementering:**
```csharp
// Scanner opretter item:
new BackupItem 
{
    SourcePath = fullSourcePath,
    RelativePath = Path.GetRelativePath(plan.Source, fullSourcePath),
    // Fx: "Bilder/2024/IMG_001.jpg"
}

// Engine beregner destination:
string destPath = Path.Combine(plan.Destination, item.RelativePath);
// Fx: "C:\backup\Bilder\2024\IMG_001.jpg"
```

---

### 7.5 BackupPlan er immutabel record

**Beslutning:** `BackupPlan` er en `sealed record` med `init`-only properties.

**Rationale:**
- Configuration skal ikke muteres under backup-kørslen
- Records er let at kopiere og teste

---

### 7.6 Destination-mappe oprettes automatisk

**Beslutning:** Engine (eller FileTransfer) opretter destination-mappen hvis den ikke eksisterer.

**Implementering:**
```csharp
string destDir = Path.GetDirectoryName(destPath)!;
Directory.CreateDirectory(destDir); // Idempotent
```

---

### 7.7 Ét cancellation-mønster

**Beslutning:** `cancellationToken.ThrowIfCancellationRequested()` i starten af hvert loop-iteration. `OperationCanceledException` fanges KUN i engine'ens øverste try/catch.

---

## 8. Hvad er implementeret i Core4 skelettet

### ✅ Fuldt implementeret

| Komponent | Fil | Status |
|-----------|-----|--------|
| `IBackupEngine` | `Api/IBackupEngine.cs` | ✅ Defineret |
| `IBackupProgress` | `Api/IBackupProgress.cs` | ✅ Defineret (inkl. ActiveFiles, BytesTotal etc.) |
| `IFileProgress` | `Api/IFileProgress.cs` | ✅ Defineret |
| `BackupPlan` | `Models/BackupPlan.cs` | ✅ Komplet (Name, Source, Destination, features, parallelism) |
| `BackupResult` | `Models/BackupResult.cs` | ✅ Komplet (statistik, FinalPhase, FailureReason) |
| `BackupItem` | `Models/BackupItem.cs` | ✅ (Id, SourcePath, RelativePath, DestinationPath, SizeBytes, ModifiedAt, Status) |
| `BackupPhase` enum | `Models/Enums/BackupPhase.cs` | ✅ (Starting, Scanning, Transferring, Completed, Cancelled, Failed) |
| `BackupErrorCode` enum | `Models/Enums/BackupErrorCode.cs` | ✅ (alle relevante fejlkoder) |
| `BackupItemStatus` enum | `Models/Enums/BackupItemStatus.cs` | ✅ |
| `CollisionStrategy` enum | `Models/Enums/CollisionStreategy.cs` | ✅ (Skip, Overwrite, Rename) |
| `OutputStructure` enum | `Models/Enums/OutputStructure.cs` | ✅ |
| `BackupSourceType` enum | `Models/Enums/BackupSourceType.cs` | ✅ |
| `BackupSessionState` | `Engine/State/BackupSessionState.cs` | ✅ (items, phase, AddItem, GetPendingItems, Fail, Cancel, Complete) |
| `BackupSessionStateKey` | `Engine/State/BackupSessionStateKey.cs` | ✅ (record med SessionId + SourceIdentity) |
| `BackupSessionStateKeyFactory` | `Engine/State/BackupSessionStateKeyFactory.cs` | ✅ (Create fra BackupPlan) |
| `IBackupSessionStateStore` | `Engine/State/IBackupSessionStateStore.cs` | ✅ (OpenAsync) |
| `InMemoryBackupSessionStateStore` | `Engine/State/InMemoryBackupSessionStateStore.cs` | ✅ (opretter ny session) |
| `BackupPlanValidator` | `Engine/Validation/BackupPlanValidator.cs` | ✅ (Name, Source, Destination, enums, MaxParallelism) |
| `BackupPlanValidationException` | `Engine/Validation/BackupPlanValidationException.cs` | ✅ (med Errors liste) |
| `IBackupScanner` | `Scanner/IBackupScanner.cs` | ✅ (IAsyncEnumerable<BackupItem> ScanAsync) |
| `Guard` | `Helpers/Guard.cs` | ✅ (RequireNonNull) |
| `SequentialBackupEngine` (delvist) | `Engine/Sequential/SequentialBackupEngine.cs` | ⚠️ Validering + scanning implementeret, transfer mangler |
| `BackupEngine` (orkestrering) | `Engine/BackupEngine.cs` | ⚠️ Skelet med kommenterede steps, NotImplementedException |

### ⚠️ Delvist implementeret (skeleton/stub)

| Komponent | Fil | Mangler |
|-----------|-----|---------|
| `BackupEngine` | `Engine/BackupEngine.cs` | Al logik – kun kommentarer |
| `LimitedParallelBackupEngine` | `Engine/LimitedParallel/LimitedParallelBackupEngine.cs` | Helt tom |
| `BackupSessionStore` | `Engine/State/BackupSessionStore.cs` | Helt tom (persistence-variant) |
| Scanner/Filesystem | `Scanner/Filesystem/` | Tom mappe |
| Transfer/Filesystem | `Transfer/Filesystem/` | Tom mappe |
| Sidecar | `Sidecar/` | Tom mappe |
| Progress | `Progress/` | Tom mappe |
| DependencyInjection | `DependencyInjection/` | Tom mappe |

### ❌ Ikke startet

| Komponent | Prioritet | Hvad mangler |
|-----------|-----------|--------------|
| `FilesystemItemScanner` | Tier 1 | IBackupScanner for lokale filer |
| `FilesystemFileTransfer` | Tier 1 | IFileTransfer for lokale filer |
| `ISidecarGenerator` | Tier 1 | Interface |
| `JsonSidecarGenerator` | Tier 1 | Implementering |
| `ProgressTracker` | Tier 1 | Thread-safe tæller og snapshot-generator |
| `ServiceCollectionExtensions` | Tier 1 | DI-registrering |
| `MTPItemScanner` | Tier 1 | MTP device scanner |
| `MTPFileTransfer` | Tier 1 | MTP file download |
| `SpectreProgressNotifier` | Tier 2 | UI progress |
| `IItemHasher` | Tier 3 | Hashing interface |
| `SHA256Hasher` | Tier 3 | Implementering |
| `IMetadataReader` | Tier 3 | Metadata interface |
| `ExifMetadataReader` | Tier 3 | EXIF + MetadataExtractor |
| `IIntegrityVerifier` | Tier 3 | Verifikation |
| `ITimestampCorrector` | Tier 3 | Tidsstempel-korrektion |

---

## 9. Hvad mangler – og i hvilken rækkefølge

### Fase 1: Grundlæggende backup virker (Tier 1)

**Mål:** En fuldt fungerende sekventiel backup fra filsystem til filsystem.

**Trin:**

1. **`FilesystemItemScanner`** – scanner lokale filer
   - `IAsyncEnumerable<BackupItem> ScanAsync(BackupPlan plan, CancellationToken ct)`
   - Brug `Directory.EnumerateFiles()` med `SearchOption.AllDirectories` if `Recursive`
   - Sæt `RelativePath = Path.GetRelativePath(plan.Source, fullPath)`
   - Håndtér `UnauthorizedAccessException` (log + skip, fortsæt)

2. **`FilesystemFileTransfer`** – kopiér filer
   - `Task<TransferResult> TransferAsync(BackupItem item, string destPath, IProgress<long>? progress, CancellationToken ct)`
   - `Directory.CreateDirectory(destDir)` FØR kopiering
   - Brug `FileStream` med `CopyToAsync` for chunk-baseret kopiering og progress-rapportering
   - Returner `TransferResult` – kast ALDRIG

3. **`ISidecarGenerator` + `JsonSidecarGenerator`** – sidecar
   - Interface: `Task GenerateAsync(BackupItem item, TransferResult transfer, BackupPlan plan, CancellationToken ct)`
   - Skriv .json fil ved siden af destination-filen (fx `IMG_001.jpg.bmtp.json`)
   - Indhold: SourcePath, RelativePath, DestinationPath, TransferredAt, Size, Status

4. **`ProgressTracker`** – thread-safe progress
   - Felter med `Interlocked.Increment/Add`: FilesDiscovered, FilesProcessed, FilesSucceeded, FilesFailed, BytesProcessed
   - `GetSnapshot()` → returnerer immutabelt `BackupProgress` record

5. **`SequentialBackupEngine` – fuldfør implementering**
   - Scan → per item: Transfer → Sidecar → [valgfrie faser]
   - Fejlhåndtering: item.Status = Failed → log → fortsæt
   - Cancellation: `ct.ThrowIfCancellationRequested()` i loop
   - Byg `BackupResult` fra `BackupSessionState` til sidst

6. **`BackupEngine` – orkestrering**
   - Vælg `SequentialBackupEngine` (MTP eller FS, Tier 1 = altid sequential)
   - Validering → session → kørsel → result

7. **`ServiceCollectionExtensions`** – DI
   - `AddBMTP3Core4(this IServiceCollection services)` metode
   - Registrér alle services

8. **Integration tests** – verificér end-to-end
   - Scan reelle filer → transfer til temp-mappe → verificér at filer er korrekt kopieret
   - Verificér RelativePath bevares
   - Verificér sidecar oprettes

---

### Fase 2: UX og robusthed (Tier 2)

1. **`SpectreProgressNotifier`** – rich UI med Spectre.Console
2. **Collision resolution** – implementér Skip/Overwrite/Rename i `FilesystemFileTransfer`
3. **DryRun** – spring I/O over, men simulér + log
4. **Output structure** – `OutputStructure` enum implementeret i transfer

---

### Fase 3: Avancerede features (Tier 3)

1. **`IItemHasher` + `SHA256Hasher`** – beregn SHA256 efter transfer
2. **`IMetadataReader` + `ExifMetadataReader`** – læs EXIF/QuickTime/MetadataExtractor
3. **`IIntegrityVerifier`** – re-hash destination, sammenlign med kilde
4. **`ITimestampCorrector`** – gendan originale tidsstempler fra EXIF
5. **`LimitedParallelBackupEngine`** – parallel FS backup (kun FS!)
6. **`MTPItemScanner` + `MTPFileTransfer`** – MTP device support

---

## 10. Detaljeret teknisk guide per modul

### 10.1 FilesystemItemScanner

```csharp
internal sealed class FilesystemItemScanner : IBackupScanner
{
    private readonly ILogger<FilesystemItemScanner> logger;

    public async IAsyncEnumerable<BackupItem> ScanAsync(
        BackupPlan plan,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string sourcePath = plan.Source;
        SearchOption searchOption = plan.Recursive 
            ? SearchOption.AllDirectories 
            : SearchOption.TopDirectoryOnly;

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(sourcePath, "*", searchOption);
        }
        catch (DirectoryNotFoundException ex)
        {
            logger.LogError(ex, "Source directory not found: {Path}", sourcePath);
            yield break; // Tom enumeration = engine håndterer fejl
        }

        foreach (string file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            BackupItem? item = TryCreateItem(file, plan);
            if (item is not null)
                yield return item;
        }
    }

    private BackupItem? TryCreateItem(string fullPath, BackupPlan plan)
    {
        try
        {
            FileInfo fi = new(fullPath);
            return new BackupItem
            {
                Id = Guid.NewGuid().ToString("N"),
                SourcePath = fullPath,
                RelativePath = Path.GetRelativePath(plan.Source, fullPath),
                SizeBytes = fi.Length,
                ModifiedAt = fi.LastWriteTimeUtc
            };
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            logger.LogWarning(ex, "Cannot access file: {Path}", fullPath);
            return null; // Skip denne fil
        }
    }
}
```

**Vigtige punkter:**
- Brug `[EnumeratorCancellation]` attribut på `CancellationToken` parameter i `IAsyncEnumerable`
- `ThrowIfCancellationRequested()` i hvert loop-iteration
- `TryCreateItem` swallower access-fejl og returnerer null (skip)
- `yield break` ved manglende source-mappe (engine skal håndtere dette)

---

### 10.2 FilesystemFileTransfer

```csharp
internal sealed class FilesystemFileTransfer : IFileTransfer
{
    private const int BufferSize = 81920; // 80 KB buffer

    public async Task<TransferResult> TransferAsync(
        BackupItem item,
        string destinationPath,
        IProgress<long>? progress,
        CancellationToken cancellationToken)
    {
        DateTime startTime = DateTime.UtcNow;
        
        try
        {
            string destDir = Path.GetDirectoryName(destinationPath)!;
            Directory.CreateDirectory(destDir); // Idempotent

            string? tempPath = null;
            try
            {
                tempPath = destinationPath + ".tmp";
                
                using FileStream source = new(
                    item.SourcePath, FileMode.Open, FileAccess.Read, 
                    FileShare.Read, BufferSize, FileOptions.SequentialScan);
                    
                using FileStream dest = new(
                    tempPath, FileMode.Create, FileAccess.Write, 
                    FileShare.None, BufferSize);

                byte[] buffer = new byte[BufferSize];
                long totalCopied = 0;
                int bytesRead;

                while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await dest.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    totalCopied += bytesRead;
                    progress?.Report(totalCopied);
                }

                // Atomic rename: .tmp → final
                File.Move(tempPath, destinationPath, overwrite: false);
                tempPath = null; // Succesfuldt – intet at cleanup

                return new TransferResult(
                    Success: true,
                    BytesTransferred: totalCopied,
                    DestinationPath: destinationPath,
                    TransferTime: startTime);
            }
            finally
            {
                // Cleanup temp-fil ved fejl eller cancellation
                if (tempPath is not null && File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }
        catch (OperationCanceledException)
        {
            throw; // Lad cancellation propagere til engine
        }
        catch (Exception ex)
        {
            return new TransferResult(
                Success: false,
                BytesTransferred: 0,
                DestinationPath: destinationPath,
                TransferTime: startTime,
                ErrorMessage: ex.Message);
        }
    }
}
```

**Vigtige punkter:**
- Skriv til `.tmp` fil → atomic rename til destination (ingen korrupte filer ved fejl)
- Cleanup temp-fil i `finally` blok
- `OperationCanceledException` re-throwes (engine håndterer cancellation)
- Alle andre exceptions returneres som `TransferResult(Success: false)`
- `BufferSize = 81920` (80 KB) er optimal for de fleste filsystemer

---

### 10.3 ProgressTracker

```csharp
internal sealed class ProgressTracker
{
    private int filesDiscovered;
    private int filesProcessed;
    private int filesSucceeded;
    private int filesFailed;
    private int filesSkipped;
    private long bytesTotal;
    private long bytesProcessed;
    private int directoriesScanned;
    private BackupPhase currentPhase;
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();

    public void IncrementFilesDiscovered() => Interlocked.Increment(ref filesDiscovered);
    public void AddBytesTotal(long bytes) => Interlocked.Add(ref bytesTotal, bytes);
    public void IncrementFilesSucceeded() 
    { 
        Interlocked.Increment(ref filesProcessed); 
        Interlocked.Increment(ref filesSucceeded); 
    }
    public void IncrementFilesFailed() 
    { 
        Interlocked.Increment(ref filesProcessed); 
        Interlocked.Increment(ref filesFailed); 
    }
    public void AddBytesProcessed(long bytes) => Interlocked.Add(ref bytesProcessed, bytes);
    public void SetPhase(BackupPhase phase) => currentPhase = phase; // Interlocked ikke nødvendig for enum

    public IBackupProgress GetSnapshot() => new BackupProgress(
        CurrentPhase: currentPhase,
        DirectoriesScanned: Volatile.Read(ref directoriesScanned),
        FilesDiscovered: Volatile.Read(ref filesDiscovered),
        BytesTotal: Volatile.Read(ref bytesTotal),
        FilesProcessed: Volatile.Read(ref filesProcessed),
        FilesSucceeded: Volatile.Read(ref filesSucceeded),
        FilesFailed: Volatile.Read(ref filesFailed),
        FilesSkipped: Volatile.Read(ref filesSkipped),
        BytesProcessed: Volatile.Read(ref bytesProcessed),
        ActiveFiles: GetActiveFiles()
    );
}
```

---

### 10.4 SequentialBackupEngine – komplet flow

```csharp
public async Task<BackupResult> RunAsync(
    BackupPlan plan,
    IProgress<IBackupProgress>? progress,
    CancellationToken cancellationToken)
{
    // 1. Valider plan (kaster BackupPlanValidationException ved fejl)
    BackupPlanValidator.Validate(plan);

    // 2. Init state og tracker
    BackupSessionStateKey sessionKey = BackupSessionStateKeyFactory.Create(plan);
    BackupSessionState session = await sessionStateStore.OpenAsync(sessionKey, cancellationToken);
    ProgressTracker tracker = new();

    // 3. Timer til rapportering
    using CancellationTokenSource reportingCts = new();
    Task reportingTask = StartProgressReporting(progress, tracker, reportingCts.Token);

    try
    {
        // 4. Scan
        session.SetPhase(BackupPhase.Scanning);
        tracker.SetPhase(BackupPhase.Scanning);

        await foreach (BackupItem item in scanner.ScanAsync(plan, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            session.AddItem(item);
            tracker.IncrementFilesDiscovered();
            if (item.SizeBytes.HasValue) tracker.AddBytesTotal(item.SizeBytes.Value);
        }

        // 5. Transfer
        session.SetPhase(BackupPhase.Transferring);
        tracker.SetPhase(BackupPhase.Transferring);

        foreach (BackupItem item in session.GetPendingItems())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Beregn destination-sti
            string destPath = Path.Combine(plan.Destination, item.RelativePath);
            item.DestinationPath = destPath;

            // Progress per fil
            IProgress<long> fileProgress = new Progress<long>(bytes =>
            {
                tracker.AddBytesProcessed(bytes);
                progress?.Report(tracker.GetSnapshot());
            });

            // Transfer
            TransferResult transfer = await fileTransfer.TransferAsync(
                item, destPath, fileProgress, cancellationToken);

            if (transfer.Success)
            {
                item.Status = BackupItemStatus.Transferred;
                tracker.IncrementFilesSucceeded();

                // Sidecar umiddelbart efter transfer
                await sidecarGenerator.GenerateAsync(item, transfer, plan, cancellationToken);

                // Valgfrie faser
                if (plan.EnableHashing && hasher is not null)
                    await RunHashingAsync(item, hasher, cancellationToken);

                if (plan.EnableMetadata && metadataReader is not null)
                    await RunMetadataAsync(item, metadataReader, cancellationToken);
            }
            else
            {
                item.Status = BackupItemStatus.Failed;
                tracker.IncrementFilesFailed();
                logger.LogError("Transfer failed: {Path} – {Error}", 
                    item.RelativePath, transfer.ErrorMessage);

                if (plan.StopOnError)
                    return BuildResult(session, tracker, BackupErrorCode.TransferFailed);
            }
        }

        // 6. Fuldfør
        session.Complete();
        return BuildResult(session, tracker, null);
    }
    catch (OperationCanceledException)
    {
        session.Cancel();
        return BuildResult(session, tracker, null);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Backup engine encountered a fatal error");
        session.Fail(BackupErrorCode.TransferFailed);
        return BuildResult(session, tracker, BackupErrorCode.TransferFailed);
    }
    finally
    {
        reportingCts.Cancel();
        await reportingTask;
    }
}
```

---

## 11. Kritiske edge cases og pitfalls

### 11.1 Tomme mapper

**Situation:** Source-mappen eksisterer men er tom.

**Forventet adfærd:** Backup gennemføres succesfuldt. `FilesDiscovered = 0`, `FilesSucceeded = 0`. `FinalPhase = Completed`.

**Faldgrube:** Sørg for at engine ikke fejler fordi der ikke er nogen items at processere.

---

### 11.2 Fil slettes under scanning

**Situation:** En fil opdages under scanning, men er slettet inden transfer starter.

**Forventet adfærd:** `FileTransfer` returnerer `TransferResult(Success: false, ErrorMessage: "File not found")`. Item markeres Failed. Backup fortsætter.

**Faldgrube:** Checker du `File.Exists()` i transfer? Nej – lad `FileStream` kaste og fang exception.

---

### 11.3 Filnavn-kollision ved destination

**Situation:** Både `Folder1/photo.jpg` og `Folder2/photo.jpg` kopieres til flad destination.

**Forventet adfærd:** Afhænger af `CollisionStrategy`:
- `Skip`: Anden fil skippes, item.Status = Skipped
- `Overwrite`: Anden fil overskriver første
- `Rename`: Anden fil omdøbes til `photo (2).jpg`

**Note:** Core3 har ikke denne håndtering → kollision giver tabt fil uden fejlmelding!

---

### 11.4 Destination disk fuld

**Situation:** Disk løber tør under transfer.

**Forventet adfærd:** `IOException` med "Disk full" → `TransferResult(Success: false)` → item Failed → temp-fil cleanet op.

**Faldgrube:** Tjek at temp-fil altid cleanup'es, selvom der er fejl under cleanup (ignore cleanup-exceptions).

---

### 11.5 MTP device disconnects under backup

**Situation:** Brugeren trækker telefonen ud midt i backup.

**Forventet adfærd:** `MTPFileTransfer` fanger disconnect exception → retry 3 gange med 2s delay → returnerer `TransferResult(Success: false)` → item Failed → backup fortsætter med næste.

**Faldgrube:** Session skal eksplicit lukkes/genåbnes ved disconnect. Aldrig antag at session stadig er gyldig.

---

### 11.6 Meget dyb mappestruktur

**Situation:** Windows MAX_PATH (260 tegn) overskrides ved destination-sti.

**Forventet adfærd:** `FilesystemFileTransfer` returnerer failed result med klar fejlbesked om sti-længde.

**Løsning:** Brug `\\?\` præfiks på Windows for lange stier:
```csharp
string longPath = @"\\?\" + destinationPath;
```

---

### 11.7 Filer med identiske relative stier (case-insensitivitet)

**Situation:** Windows filsystem er case-insensitiv. `Photo.jpg` og `photo.jpg` er samme fil på Windows, men forskellige på Linux.

**Forventet adfærd:** Scanner på Windows finder KUN én af dem. Scanner på Linux finder begge.

**Faldgrube:** Hardkod ikke antagelse om case-sensitivitet.

---

### 11.8 Cancellation midt i sidecar-generering

**Situation:** Bruger cancellerer backup mens sidecar skrives.

**Forventet adfærd:** Sidecar-filen er muligvis ufuldstændig. Dette er acceptabelt – sidecar-generering er ikke atomisk.

**Løsning:** Brug `.tmp` + rename mønster for sidecar-filer også.

---

### 11.9 Progress-rapportering med periodisk timer

**Situation:** For at undgå at spamme UI med Progress.Report() for hver byte, brug en periodisk timer:

```csharp
private static async Task StartProgressReporting(
    IProgress<IBackupProgress>? progress,
    ProgressTracker tracker,
    CancellationToken ct)
{
    if (progress is null) return;
    
    using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(250)); // 4x/sek
    while (await timer.WaitForNextTickAsync(ct))
    {
        progress.Report(tracker.GetSnapshot());
    }
}
```

---

## 12. Anbefalinger og gode mønstre

### 12.1 Test-strategi

```
Unit tests: Hver service isoleret (mock IFileTransfer, mock IBackupScanner)
Integration tests: Rigtige filer i temp-mappe (TestContext.TempDir)
End-to-end tests: Scan → Transfer → verificér output
```

**Trait-kategorisering:**
```csharp
[Trait("Category", "Integration")]
public class SequentialBackupEngineIntegrationTests { }

[Trait("Category", "Unit")]
public class BackupPlanValidatorTests { }
```

---

### 12.2 Logging-konventioner

```csharp
// Scanning
logger.LogInformation("Scanning source: {Source}", plan.Source);
logger.LogDebug("Discovered file: {RelativePath} ({Size} bytes)", item.RelativePath, item.SizeBytes);
logger.LogWarning("Cannot access: {Path}", path);

// Transfer
logger.LogInformation("Transferring {Count} files to {Destination}", itemCount, plan.Destination);
logger.LogDebug("Transfer: {RelativePath} → {DestPath}", item.RelativePath, destPath);
logger.LogError("Transfer failed: {RelativePath} – {Error}", item.RelativePath, result.ErrorMessage);

// Faser
logger.LogInformation("Phase: {Phase}", phase);
```

---

### 12.3 Navnekonventioner i Core4

| Element | Konvention | Eksempel |
|---------|------------|---------|
| Interfaces | `I` prefix | `IBackupScanner`, `IFileTransfer` |
| Sealed classes | `sealed class` | `SequentialBackupEngine` |
| Records (immutable) | `sealed record` | `BackupPlan`, `BackupResult` |
| Enums | PascalCase | `BackupPhase.Scanning` |
| Private fields | camelCase, ingen `_` prefix | `this.scanner` |
| Internal services | `internal sealed class` | `FilesystemItemScanner` |
| Public API | `public interface/record` | `IBackupEngine`, `BackupPlan` |

---

### 12.4 DI-registrering mønster

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBMTP3Core4(
        this IServiceCollection services)
    {
        // Engine
        services.AddTransient<IBackupEngine, BackupEngine>();
        services.AddTransient<SequentialBackupEngine>();
        services.AddTransient<LimitedParallelBackupEngine>();
        
        // Scanner
        services.AddTransient<IBackupScanner, FilesystemItemScanner>();
        
        // Transfer
        services.AddTransient<IFileTransfer, FilesystemFileTransfer>();
        
        // Sidecar
        services.AddTransient<ISidecarGenerator, JsonSidecarGenerator>();
        
        // State
        services.AddSingleton<IBackupSessionStateStore, InMemoryBackupSessionStateStore>();
        
        // Valgfrie features (kun hvis aktiveret)
        // services.AddTransient<IItemHasher, SHA256Hasher>();
        // services.AddTransient<IMetadataReader, ExifMetadataReader>();
        
        return services;
    }
}
```

---

### 12.5 Sidecar JSON format (anbefalet)

```json
{
  "schema_version": "4.0",
  "backup_item": {
    "id": "abc123",
    "source_path": "/DCIM/Camera/IMG_001.jpg",
    "relative_path": "Camera/IMG_001.jpg",
    "destination_path": "C:/backup/Camera/IMG_001.jpg",
    "size_bytes": 4521088,
    "modified_at": "2024-06-15T14:22:33Z",
    "status": "Transferred"
  },
  "transfer": {
    "transferred_at": "2026-05-11T09:41:34Z",
    "bytes_transferred": 4521088,
    "success": true
  },
  "hashes": null,
  "metadata": null,
  "verification": null
}
```

---

### 12.6 Hvornår MTP vs Filesystem

```csharp
// I BackupEngine.cs (orkestrering):
IBackupEngine strategy = plan.SourceType switch
{
    BackupSourceType.Filesystem => filesystemEngine,
    BackupSourceType.MediaDevice => sequentialEngine, // ALTID sequential for MTP
    _ => throw new InvalidOperationException($"Unknown source type: {plan.SourceType}")
};

return await strategy.RunAsync(plan, progress, cancellationToken);
```

---

*Sidst opdateret: 2026-05-11*  
*Reference: CORE4_ARCHITECTURE_en.md, CORE4_PLAN_en.md, Lessons_Learned_Core.md, Lessons_Learned_Core2.md, Lessons_Learned_Core3.md*
