# Backup Pipeline Comparison: Core / Core2 / Core3 vs Core4

## 1. Backup Engine Classes per Project

| Project | Klasse(r) | Fil |
|---|---|---|
| **Core** | `BackupHandlerForDevice` (device path) / `BackupHandlerForDrive` (stub) | `Handlers/RefactorNewBackup/BackupHandlerForDevice.cs` |
| **Core2** | `BackupEngine` (parallel channels) + `BackupEngineSequentiel` (sequential) | `BackupNew/Engine/BackupEngine.cs` + `BackupEngineSequentiel.cs` |
| **Core3** | `BackupEngineSequential` | `BackupEngineSequential.cs` |
| **Core4** | `BackupEngine` | `Engine/BackupEngine.cs` |

Note: Core har ingen `BackupEngine`-klasse. `BackupHandlerForDrive` er en `NotImplementedException` stub. Den eneste reelle pipeline i Core er `BackupHandlerForDevice`, som håndterer MTP/bærbare enheder.

---

## 2. Sammenlignet Pipeline Steps

### Core — `BackupHandlerForDevice.BackupDevice()`

Per-item pipeline (inline, ikke navngivne steps):

1. **Connect device** — Åbner MTP device session via `MediaDevice.Connect()`
2. **Scan / enumerate all files** — Rekursivt gennemløb af `MediaDirectoryInfo`, bygger `List<BackupRecordInfo>` med `PersistentUniqueId`, sti, størrelse, timestamps
3. **Sort by path** — `allDeviceFiles.Sort(...)` for deterministisk progressionsrækkefølge
4. **Load/initialize resume state** — `BackupRecordDataStore.LoadDataStore()` eller opretter ny; kaster hvis device-fillisten har ændret sig siden sidste kørsel
5. **Skip already-saved items** — Tjekker `pendingFileInfo.IsSaved`
6. **Download to temp file** (`DownloadToTempFile`) — `MediaFileInfo.CopyTo()` til temp-mappe; derefter `BackupHelper.UpdateFileTimestamp()` for at bevare source-timestamps på temp-filen
7. **Extract metadata / timestamp** — `MetadataExtractorFileInfo.GetCreatedMediaFileDateTime()` + `File.GetCreationTime()` på temp-filen → `FindEarliestValidDateTime()` vælger det ældste gyldige timestamp
8. **Generate path from template** (`filePattern`) — `Template.Replace()` substituerer `{yyyy}`, `{MM}`, `{dd}`, `{HH}`, `{mm}`, `{ss}`, `{fff}`, `{originalName}`, `{ext}`, `{count}` i det konfigurerede filnavnemønster
9. **Collision resolution** — Hvis target eksisterer: binary-sammenlign source vs. target; hvis identisk → slet temp + skip; hvis forskellig → increment `{count}` counter og prøv alternativ sti indtil en fri plads findes
10. **Create sidecar (.ini)** (`CreateSideCarFileInfo`) — Skriver INI-format sidecar til temp-mappe med:
    - `[Settings]`: original filename, CreateDateTime, LastAccessDateTime, LastWriteDateTime, MediaTakenDateTime
    - `[BackupInfo]`: BackupDateTime
    - `[FileHash]`: SHA3-512 (Keccak + FIPS202), SHA2-512, SHA2-256, MD5, BLAKE3-256, BLAKE3-512 (7 hashes total)
    - `[DeviceFileDetails]`: PersistentUniqueId, FullName
    - `[DeviceDetails]`: DeviceId, Description, FriendlyName, Manufacturer, Model, SerialNumber
11. **Move file from temp to destination** — `FileInfo.MoveTo(newTargetFilePath)`
12. **Move sidecar from temp to destination** — `FileInfo.MoveTo(newTargetSideCarFilePath)`
13. **Persist resume state** — `BackupRecordDataStore.SaveDataStore()` i `finally` blok
14. **Cleanup empty temp directories** — `BackupHelper.DeleteEmptyDirectoriesRecursive()`

---

### Core2 — `BackupEngineSequentiel.RunAsync()` (Sequential)

`BackupEngine` (parallel) kører samme steps men som bounded `Channel<IBackupItem>` stages.

**Pre-flight (en gang, før items):**
1. **Validate plan** — `IJobValidator.ValidateAsync(plan)`
2. **Validate disk space** — tjekker ledige bytes på output-drev; kaster hvis < 100 MB
3. **Validate source and output** — directory eksistens + read/write access probe
4. **Initialize resume session** — `IBackupRepository.SaveAsync(session)` (opretter session.json)
5. **Open MTP device session** (hvis relevant) — `IMtpCapableScanner.OpenSession(plan)`

**Per-item pipeline (i `pipelineSteps[]` rækkefølge):**

1. **Buffering / Staging** (`ContentBufferingItemStep`) — Downloader source content (MTP eller filesystem) til en lokal temp-fil; opdaterer staging progress. Serial (1 thread) for at forhindre MTP timeouts.
2. **Metadata extraction** (`MetadataExtractionItemStep`) — Læser EXIF/metadata fra den stagede fil via `IMetadataReader`; udfylder `MetadataKey` entries (AuthoredDateTime, CreatedDateTime, Length, etc.)
3. **Timestamp correction** (`TimestampCorrectionItemStep`) — Resolver det tidligste gyldige authored timestamp via `DefaultTimestampWaterfall` (EXIF → filesystem → fallback); skriver korrigerede timestamps ind i item metadata
4. **Hashing** (`HashItemStep`) — Beregner konfigurerede hash-typer (f.eks. SHA2-256, SHA2-512, BLAKE3) via `IItemHasher` på den stagede fil
5. **Transfer / path resolution** (`TransferItemStep`) — Genererer relativ destinationssti via `IPathGenerator`; kører `ICollisionResolver` (Skip / Rename with counter); opretter destinationsdirectory; kalder `IFileTransfer.TransferAsync()` (move from temp); anvender destination timestamps; kører eventuelt **post-write verification** (hash eller binary comparison med retry/timeout)
6. **Destination inspection** (`DestinationInspectorItemStep`) — Efter transfer, læser hashes fra destination sidecar (hvis til stede) eller beregner dem fresh via `IDestinationInspector`; gemmer i `MetadataKey.DestinationHashes` for cross-run deduplication
7. **Sidecar generation** (`SidecarGenerationItemStep`) — Skriver sidecar-fil (JSON eller INI format via `ISidecarGeneratorFactory`) ved siden af destinationsfilen

**Per-item persistence (efter hvert item):**

8. **Persist item state** — `IBackupRepository.PersistItemStateAsync(item)` + periodisk `SaveAsync(session)` hver 10. item

**Post-pipeline cleanup:**

9. **Dispose MTP session** — efter buffering stage er færdig
10. **Compile final result** — mapper tracker snapshot til `BackupJobResult`

---

### Core3 — `BackupEngineSequential.RunAsync()`

Ren, simpel sekventiel pipeline. Alle steps kører på den **allerede overførte destinationsfil** (metadata/hashing er post-transfer, modsat Core2):

**Pre-flight:**
- Ingen eksplicit validering eller disk space check (intet implementeret)
- Ingen resume/session initialisering

**Per-batch pipeline (faser opererer over hele item-listen):**

1. **Scan** — `IBackupScanner.ScanAsync(plan.Source)` → bygger `List<BackupItem>` med source-stier og størrelser; rapporterer `BackupPhase.Scanning` progress
2. **Transfer** — `IFileTransfer.CopyAsync()` per item; håndterer `CollisionStrategy.Overwrite` (delete-then-copy); updaterer item med `DestinationPath`; critical failure (kaster og stopper kørslen)
3. **Extract Metadata** — `IMetadataReader.ReadAsync(destPath)` på **destinationsfilen**; udfylder `item.Metadata` dictionary; non-critical (fejl logges og tilføjes til error-listen, stopper ikke kørslen)
4. **Generate Hashes** — `IItemHasher.ComputeHashesAsync(item, plan.HashTypes)` på destinationsfilen; non-critical
5. **Correct Timestamps** — `File.SetCreationTime(destPath, item.CreatedAt)` + `File.SetLastWriteTime(destPath, item.ModifiedAt)` på destinationsfilen; non-critical
6. **Generate Sidecars** — `ISidecarGenerator.GenerateAsync(item, plan.Destination)` skriver `{item.Name}.sidecar`; non-critical

**Post-run:**
7. **Build result** — `BuildSuccessResult()` / `BuildFailureResult()` aggregerer counts og error-liste

---

### Core4 — `BackupEngine.RunAsync()` (nuværende)

**Pre-flight:**
1. **BackupPlanValidator.Validate(plan)** — Validerer backup-planen
2. **Initialize backup state** — Opretter `BackupProgress`, `BackupMemoryRecordRepository`, `BackupSessionKey`
3. **Prepare destination** — Opretter destinationsmappe + `.bmtp3` undermappe
4. **Open source traversal** — `ISourceTraversalFactory.Create()` (MTP eller filesystem)
5. **Scan source** — `_scanner.ScanAsync()` med include/exclude patterns og progress reporting
6. **Resume** — `_sessionState.ApplyResumeAsync()` genopretter `DestinationPath` og `Status` for tidligere processerede items
7. **Filter pending** — `FilterPendingRecords()` udskiller kun `Pending` items
8. **Prepare temp directory** — `TempDirectoryHelper` opretter temp-mappe

**Per-item loop:**

1. **Download** — `_downloadService.DownloadAsync()` skriver content til temp-fil
2. **Earliest timestamp resolution** — `_earliestTimestampService.ResolveEarliestAsync()` udtrækker tidligste gyldige timestamp → `record.Metadata.AuthoredDateTime` + `CreatedDateTime`
3. **Hash computation** — `_hashService.ComputeHashesAsync()` beregner alle konfigurerede hash-algoritmer → `record.Metadata.ComputedHashes`
4. **Collision resolution** — `CollisionHelpers.ResolveTargetPath()` håndterer Skip / Error / Overwrite / Rename
5. **Directory.CreateDirectory** — Opretter destinationsdirectory
6. **Move file** — `content.MoveTo(finalPath, overwrite)` via `IMoveableContent`
7. **Sidecar generation** — `_sidecarService.WriteAsync()` skriver `.ini` med timestamps + hashes
8. **Update record status** — `record.Status = Succeeded`

**Post-run:**
9. **Build result** — Samler alle records til `BackupResult`

---

## 3. Steps Core har som Core4 mangler (eller kun har stub)

| Step | Core | Core2 | Core3 | Core4 | Noter |
|---|---|---|---|---|---|
| Pre-flight plan validation | (implicit) | Ja (`IJobValidator`) | Nej | Ja (`BackupPlanValidator.Validate`) | ✓ |
| Disk space check | Nej | Ja (100 MB minimum) | Nej | **Nej** | Kan tilføjes som pre-flight |
| Source/output access probe | Nej | Ja | Nej | **Nej** | Kan tilføjes som pre-flight |
| Resume state / session persistence | Ja (JSON datastore) | Ja (`IBackupRepository`) | Nej | Ja (`ISessionStateService`) | ✓ |
| Temp directory staging | Ja | Ja | Nej | Ja (`TempDirectoryHelper`) | ✓ |
| File download (MTP/content) | Ja (`MediaFileInfo.CopyTo`) | Ja | Nej (filesystem only) | Ja (`_downloadService.DownloadAsync`) | ✓ |
| Timestamp preservation on temp file | Ja (`BackupHelper.UpdateFileTimestamp`) | Ja (i staging step) | Nej | **Nej** | Kan overvejes hvis temp-filens timestamp påvirker metadata extraction |
| EXIF/metadata extraction → earliest timestamp | Ja (`MetadataExtractorFileInfo` → `FindEarliestValidDateTime`) | Ja (pre-transfer, via `IMetadataReader`) | Ja (post-transfer, via `IMetadataReader`) | Ja (via `IEarliestTimestampResolutionService`) | ✓ |
| Multi-hash computation | Ja (7 algoritmer i sidecar) | Ja (konfigurerbar) | Ja (konfigurerbar) | Ja (konfigurerbar) | ✓ |
| **Timestamp correction on destination file** | Nej (applied to temp) | Ja (`TryApplyDestinationTimestamp` i Transfer) | Ja (eksplicit step: `File.SetCreationTime` + `SetLastWriteTime`) | **Markeret som "Future"** | Skal implementeres: sæt `File.SetCreationTime`/`SetLastWriteTime` på destinationsfilen fra `record.Metadata` |
| Collision resolution (rename with counter) | Ja (counter loop) | Ja (`ICollisionResolver` → Rename) | Overwrite only | Ja (`CollisionHelpers.ResolveTargetPath` med Rename) | ✓ |
| Binary compare (identical file detection) | Ja (i collision step) | Ja (i `ICollisionResolver`) | Nej | **Nej** | Kan tilføjes som del af collision: hvis identisk → skip |
| Post-write verification (hash or binary) | Ja (binary compare i collision) | Ja (eksplicit, retryable med timeout) | Nej | **Nej** | Kan tilføjes som valgfrit step |
| Destination inspection / sidecar hash read-back | Nej | Ja (`DestinationInspectorItemStep`) | Nej | **Nej** | Bruges til cross-run deduplication |
| Sidecar generation | Ja (altid INI, 7 hashes + device info) | Ja (INI eller JSON, konfigurerbar) | Ja (`.sidecar` format) | Ja (INI, konfigurerbar) | ✓ |
| Device metadata i sidecar (`[DeviceDetails]`) | Ja (DeviceId, SerialNumber, Manufacturer, Model) | Nej | Nej | **Nej** | Ikke relevant endnu — Core4 har ikke MTP |
| Cleanup af tomme temp-mapper | Ja (`BackupHelper.DeleteEmptyDirectoriesRecursive`) | Delvist (per-item staging cleanup) | Nej | **Nej** | Kan tilføjes som post-run cleanup |
| **Post-run persistence** | Ja (`BackupRecordDataStore.SaveDataStore` i `finally`) | Ja (`IBackupRepository.SaveAsync` hver 10. item) | Nej | **Nej (mangler)** | Der kaldes aldrig `_sessionState.SaveAsync()` efter loopet — items markeres som `Succeeded` men gemmes ikke |

---

## 4. Nøgle huller i Core4 (prioriteret)

### Kritisk
1. **Post-run persistence mangler** — `_sessionState.SaveAsync()` kaldes aldrig efter loopet. Items får `Status = Succeeded` men dette gemmes ikke til næste session. Resume vil ikke kunne se at items allerede er behandlet. **Dette skal fixes.**

### Vigtigt
2. **Timestamp correction** — Sæt `File.SetCreationTime` og `File.SetLastWriteTime` på destinationsfilen efter flytning, baseret på `record.Metadata.CreatedDateTime`/`ModifiedDateTime`/`AuthoredDateTime`

### Kan vente
3. **Disk space pre-flight check**
4. **Source/output access probe**
5. **Post-write verification** (valgfrit, hash eller binary compare)
6. **Binary compare i collision** (hvis identisk → skip i stedet for at lave en kopi)
7. **Cleanup af tomme temp-mapper**

### Fremtid
8. **Destination inspection** (cross-run deduplication)
9. **Device metadata i sidecar** (når MTP understøttes)
