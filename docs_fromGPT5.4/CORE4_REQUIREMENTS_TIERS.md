# Core4 Implementering — Iterativ Tier-struktur

**Formål:** Klart definere hvilke krav der hører til hver tier, så du kan implementere iterativt og have en "virker"-version efter hvert trin.

**Vigtigt:** I implementeringsguiden bruges et internt arbejds-checkpoint hvor Trin 0–13 leverer en FS-first foundation. Det er kun en arbejdsrytme. **Tier 1 er først lukket når MTP-delen også virker** (Trin 14 i implementeringsguiden).

---

## Tier 1: Minimal virkende backup (FS + MTP sekventiel)

**Mål:** En bruger kan køre en fuldstændig backup fra start til slut. Alle filer bliver overført. Der er sporbarhed via sidecar.

**Hvad der virker:**
- Filsystem-backup (sekventiel)
- MTP-backup (sekventiel, simpelt)
- Scan, transfer, sidecar-oprettelse, resultat

**Hvad der IKKE er:**
- Parallel FS-transfer
- Hashing, metadata, verification, timestamp-correction
- Persistent session-state (resume)
- Spectre progress-UI
- Kollisions-strategier (kun Skip)
- Include/exclude patterns
- Enriched sidecars

**Klasserne der skal være:**

| Fil | Klasse | Status |
|-----|--------|--------|
| `Transfer/IFileTransfer.cs` | `IFileTransfer` | Opret |
| `Transfer/TransferResult.cs` | `TransferResult` | Opret |
| `Sidecar/ISidecarGenerator.cs` | `ISidecarGenerator` | Opret |
| `Sidecar/SidecarData.cs` | `SidecarData` | Opret |
| `Sidecar/JsonSidecarGenerator.cs` | `JsonSidecarGenerator` | Opret |
| `Progress/BackupProgressSnapshot.cs` | `BackupProgressSnapshot` | Opret |
| `Progress/FileProgressSnapshot.cs` | `FileProgressSnapshot` | Opret |
| `Progress/ProgressTracker.cs` | `ProgressTracker` | Opret |
| `Scanner/Filesystem/FilesystemItemScanner.cs` | `FilesystemItemScanner` | Opret |
| `Transfer/Filesystem/FilesystemFileTransfer.cs` | `FilesystemFileTransfer` | Opret |
| `Engine/BackupEngineFactory.cs` | `BackupEngineFactory` | Opret |
| `DependencyInjection/Core4Options.cs` | `Core4Options` | Opret |
| `DependencyInjection/ServiceCollectionExtensions.cs` | `ServiceCollectionExtensions` | Opret |
| `Engine/Sequential/SequentialBackupEngine.cs` | `SequentialBackupEngine` | Færdiggør |

**Hvad der skal teste:**
- FS kilde → destination: 5 filer, alle overføres korrekt
- Sidecar oprettes for hver fil
- Per-fil transfer-fejl stopper ikke hele backup
- Cancellation stopper job gracefully
- Dry-run simulerer uden writes
- `BackupResult` har korrekte tællere

**Exit-kriterier for Tier 1:**
1. Kompilerer uden fejl
2. End-to-end test: FS scan → transfer → sidecar → result
3. Fejl per fil → fortsætter, ikke stopper
4. Cancellation returnerer partial result
5. Dry-run virker uden sidecar-skrivning

---

## Tier 1.5: MTP-support (sekventiel, stabil)

**Mål:** MTP-backup fungerer som FS-backup — sekventielt, stabilt, helt uden disconnect-problemer.

**Hvad der ny:**
- MTP scanner
- MTP transfer
- Retry-strategi (1s/2s/4s backoff)
- COMException-håndtering
- Session-lifecycle management

**Klasserne der skal være:**

| Fil | Klasse | Status |
|-----|--------|--------|
| `Scanner/MTP/MTPItemScanner.cs` | `MTPItemScanner` | Opret |
| `Transfer/MTP/MTPFileTransfer.cs` | `MTPFileTransfer` | Opret |

**Krav:**
- Stack-baseret traversal (ingen rekursion)
- Retry 3× med exponential backoff
- Session åbnes uden for scanner/transfer (af calling code)
- Keepalive mindst hver 30. sekund under lange MTP-forløb
- Operation-timeout pr. device-read/device-copy (default 60 sekunder)
- Per-mappe fejl logges, scan fortsætter
- Permanent disconnect stopper job med `MediaDeviceDisconnected`

**Exit-kriterier for Tier 1.5:**
1. MTP scanner enumererer filer stabilt
2. MTP transfer bruges retry-backoff korrekt
3. Transiente fejl retry'es, permanente fejl stoppes
4. Sessionmanagement er eksplicit (åbnet uden for transfer)
5. E2E test: MTP kilde med 5 filer → alle overføres

---

## Tier 2: Robust og fleksibel

**Mål:** Core backup virker korrekt i alle situationer. Bruger kan konfigurere adfærd.

**Hvad der ny:**
- Pattern-matching (include/exclude)
- Kollisions-strategier (Skip, Overwrite, Rename)
- Enhanced dry-run (full simulering)
- ILogger-integration (korrekt logging)
- ProgressReporter debounce (max 500ms rapportering)
- SkipExisting logik
- StopOnError override
- OutputStructure: Flat + PreserveHierarchy

**Klasserne der skal være:**

| Fil | Klasse | Status |
|-----|--------|--------|
| (ingen nye klasser) | — | (Tier 1-kode udvidelse) |

**Krav:**
- `BackupPlan.IncludePatterns` / `ExcludePatterns` filtrerer korrekt
- `CollisionStrategy.Rename` tilføjer `_1`, `_2` osv.
- `OutputStructure.PreserveHierarchy` bevarer mapper
- `OutputStructure.Flat` sætter alle i root
- `plan.SkipExisting` springer over eksisterende
- `plan.StopOnError` stopper ved første fejl
- Logging: scan start, transfer fejl per item, summary
- Progress rapporteres max hver 500ms (ikke hver fil)
- Dry-run simulerer destination-mappestruktur og collisions, men skriver intet til disk

**Exit-kriterier for Tier 2:**
1. Include/exclude patterns virker (glob-matching)
2. Collision-strategies testes (Rename især)
3. DryRun er fuld simulering
4. Logging er informativ (scan start, fejl, summary)
5. Progress rapportering vælter ikke UI

---

## Tier 3: Optional features (enrichment pipeline)

**Mål:** Backup kan berige sidecar med metadata uden at påvirke kernekørsel.

**Hvad der ny:**
- Hashing (SHA-256)
- Metadata extraction (EXIF/XMP/QuickTime)
- Integrity verification
- Timestamp correction

**Klasserne der skal være:**

| Fil | Klasse | Status |
|-----|--------|--------|
| `Sidecar/SidecarEnrichment.cs` | `SidecarEnrichment` | Opret |
| `Features/Hashing/IItemHasher.cs` | `IItemHasher` | Opret |
| `Features/Hashing/HashResult.cs` | `HashResult` | Opret |
| `Features/Hashing/FileHasher.cs` | `FileHasher` | Opret |
| `Features/Metadata/IMetadataReader.cs` | `IMetadataReader` | Opret |
| `Features/Metadata/ExtractedMetadata.cs` | `ExtractedMetadata` | Opret |
| `Features/Metadata/MetadataExtractorReader.cs` | `MetadataExtractorReader` | Opret |
| `Features/Metadata/ExifToolMetadataReader.cs` | `ExifToolMetadataReader` | Opret |
| `Features/Verification/IIntegrityVerifier.cs` | `IIntegrityVerifier` | Opret |
| `Features/Verification/VerificationResult.cs` | `VerificationResult` | Opret |
| `Features/Verification/FileIntegrityVerifier.cs` | `FileIntegrityVerifier` | Opret |
| `Features/Timestamp/ITimestampCorrector.cs` | `ITimestampCorrector` | Opret |
| `Features/Timestamp/TimestampCorrectionResult.cs` | `TimestampCorrectionResult` | Opret |
| `Features/Timestamp/FileTimestampCorrector.cs` | `FileTimestampCorrector` | Opret |

**Krav per feature:**

**Hashing:**
- Beregn SHA-256 per fil (efter transfer)
- `BackupPlan.EnableHashing` styrer
- Fejl: log warning, sidecar opdateres med fejlstatus, backup **fortsætter**

**Metadata extraction:**
- Læs altid filsystem-attributter først
- `MetadataExtractorReader` forsøger derefter (bruger MetadataExtractor NuGet)
- `ExifToolMetadataReader` fallback (bruger exiftool.exe -json) når managed parse fejler eller er tvetydig
- Timestamp-præcedens: EXIF DateTimeOriginal → CreateDate → QuickTime → FileSystemLastWriteUtc → FileSystemCreationUtc → null
- `BackupPlan.EnableMetadata` styrer
- Fejl: log warning, sidecar opdateres med fejlstatus, backup **fortsætter**

**Verification:**
- Sammenlign hash af source og destination (kræver hashing)
- `BackupPlan.EnableVerification` styrer
- Fejl: log warning, sidecar opdateres med fejlstatus, backup **fortsætter**

**Timestamp correction:**
- Sæt `File.SetLastWriteTimeUtc` til extracted dato
- `BackupPlan.EnableTimestampCorrection` styrer
- Fejl: log warning, sidecar opdateres med fejlstatus, backup **fortsætter**

**Exit-kriterier for Tier 3:**
1. Hashing virker, fejl stopper det ikke
2. Metadata ekstraheres korrekt (EXIF prioritet)
3. ExifTool fallback virker
4. Verification sammenligner hashes
5. Timestamp korrigeres
6. Sidecar opdateres atomisk med enrichment

---

## Tier 4: Parallel FS-engine (performance)

**Mål:** Filsystem-backup kan køre hurtigere via begrænset parallelisme uden at miste robusthed.

**Klasserne der skal være:**

| Fil | Klasse | Status |
|-----|--------|--------|
| `Engine/LimitedParallel/LimitedParallelBackupEngine.cs` | `LimitedParallelBackupEngine` | Opret |

**Krav:**
- Bruges **kun** til `BackupSourceType.FileSystem`
- Bruges **aldrig** til MTP
- `SemaphoreSlim(dop, dop)` throttle (default 4)
- Producer-consumer pattern
- `plan.MaxDegreeOfParallelism = 1` → force sequential (ældre docs med `-1` gælder ikke for nuværende skeleton)
- Samme fejlpolitik som sequential
- Samme sidecar-kontrakt
- Trådsikker progress-tracking

**Exit-kriterier for Tier 4:**
1. FS backup faster med parallel end sequential
2. MTP er **aldrig** parallel
3. MaxDegreeOfParallelism=1 force'r sequential
4. Fejlpolitik identisk til sequential

---

## Tier 5: Persistence & resume (ikke MVP)

**Mål:** Backup kan pauses og genoptages senere fra samme punkt.

**Klasserne der skal være:**

| Fil | Klasse | Status |
|-----|--------|--------|
| `Engine/State/IBackupRepository.cs` | `IBackupRepository` | Opret |
| `Engine/State/NoOpBackupRepository.cs` | `NoOpBackupRepository` | Opret |
| `Engine/State/FileSystemBackupRepository.cs` | `FileSystemBackupRepository` | Opret |
| `Engine/State/SqliteBackupRepository.cs` | `SqliteBackupRepository` | Opret |

**Krav:**
- `IBackupSessionStateStore` kan persist til disk / SQLite
- Track processed items
- Resume fra same session
- I/O fejl logges, backup fortsætter uden persistence

**Status:** Udskudt til senere version.

---

## Tier 6: Advanced UI & reporting (nice-to-have)

**Mål:** Bruger får real-time progress, ETA, speed, active workers.

**Klasserne der skal være:**

| Fil | Klasse | Status |
|-----|--------|--------|
| `Progress/IProgressNotifier.cs` | `IProgressNotifier` | Opret |
| `Progress/SpectreProgressNotifier.cs` | `SpectreProgressNotifier` | Opret |

**Krav:**
- Real-time progress via Spectre.Console
- ETA estimation
- Bytes/sec speed
- Active workers display

**Status:** Udskudt til senere version.

---

## Implementeringsplan som koder

**Start her:**

1. **Ugen 1:** Foundation-checkpoint (Trin 0–13)
   - Slet tomme filer
   - Opret alle Tier 1-klasser
   - End-to-end test: FS backup virker
   - **Checkpoint:** "Minimal FS-backup virker"

2. **Ugen 2:** Tier 1 afsluttes (Trin 14 / MTP completion)
   - MTP scanner + transfer
   - Retry-strategi
   - End-to-end test: MTP backup virker
   - **Checkpoint:** "Tier 1 er lukket: MTP-backup virker stabilt"

3. **Ugen 3:** Tier 2 (Trin ingen nye, extension af Tier 1)
   - Pattern-matching
   - Collision-strategier
   - DryRun, logging, SkipExisting, StopOnError
   - **Checkpoint:** "Robust backup med optioner"

4. **Ugen 4–5:** Tier 3 (Trin 15–28)
   - Hashing, metadata, verification, timestamp
   - UpdateAsync i sidecar
   - **Checkpoint:** "Enriched backup med optional features"

5. **Ugen 6:** Tier 4 (Trin 29)
   - LimitedParallelBackupEngine
   - Performance test
   - **Checkpoint:** "FS-backup er hurtig, MTP er stabil"

---

## Afgørende principper (ubrydelige)

1. **Tier 1 skal være minimal.** Intet optional, intet nice-to-have.
2. **Fejl handler ikke på niveau.** Tier 3-fejl stopper ikke Tier 1.
3. **Hver tier er testbar.** Før næste tier begynder, skal forrige tier være testet og godkendt.
4. **Ingen bagud-inkompatibilitet.** Hver tier bygger på forrige, ændrer ikke tidligere kode.

---

_Dokument version 1.1 — normaliseret mod `CORE4_IMPLEMENTATION_GUIDE_DA.md` v4.1 samt `CORE4_COMPLETION_SUMMARY.md`._
