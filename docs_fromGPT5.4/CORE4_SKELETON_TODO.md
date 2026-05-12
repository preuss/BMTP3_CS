# Core4 Skeleton TODO
## Kort, synkroniseret TODO-liste for den aktuelle Core4-retning

**Formål:** Give en enkel fil-for-fil checkliste, som matcher det nuværende `BMTP3.Core4`-skeleton og den normaliserede implementeringsguide.

**Sidst opdateret:** 2026-05-12

**Vigtigt:** Denne fil er **ikke** en alternativ arkitektur. Hvis den kolliderer med `CORE4_IMPLEMENTATION_GUIDE_DA.md`, er det guiden der gælder.

---

## 1. Før du går i gang

Disse ting er allerede fastlåst og må ikke "rettes tilbage" til ældre Core4-planer:

- behold `BackupPlan`
- behold `BackupResult`
- behold `BackupPlan.SourceType` + `BackupPlan.Source`
- behold boolean feature flags (`EnableHashing`, `EnableMetadata`, `EnableVerification`, `EnableTimestampCorrection`)
- behold `IBackupProgress` + `IFileProgress` i den aktuelle skeleton-form
- behold `IAsyncEnumerable<BackupItem>` i `IBackupScanner`
- behold `.sidecar.json`
- behold MTP som sekventiel

Genindfør **ikke**:

- `BackupJobResult`
- `DeviceId`
- `SourceDirectory` / `OutputDirectory`
- `WriteSidecar`
- `HashTypes`
- `IBackupItem` som public baseline
- `IProgressNotifier` som Tier 1-baseline
- `MaxDegreeOfParallelism = -1`

---

## 2. Hvad der allerede findes i skeletonet

Disse filer er allerede nyttige baseline-byggesten:

| Fil | Rolle |
|---|---|
| `Api/IBackupEngine.cs` | Public engine-entrypoint |
| `Api/IBackupProgress.cs` | Public progress-contract |
| `Api/IFileProgress.cs` | Public active-file progress |
| `Models/BackupPlan.cs` | Public plan-contract |
| `Models/BackupResult.cs` | Public result-contract |
| `Models/BackupItem.cs` | Intern working item |
| `Models/Enums/*` | Baseline enums |
| `Scanner/IBackupScanner.cs` | Streaming scanner-contract |
| `Engine/Validation/BackupPlanValidator.cs` | Plan-validering |
| `Engine/Validation/BackupPlanValidationException.cs` | Validation-fejl |
| `Engine/State/BackupSessionState.cs` | Intern session-state |
| `Engine/State/BackupSessionStateKey.cs` | State-key |
| `Engine/State/BackupSessionStateKeyFactory.cs` | Opretter state-key |
| `Engine/State/IBackupSessionStateStore.cs` | State-store-contract |
| `Engine/State/InMemoryBackupSessionStateStore.cs` | Minimal in-memory store |
| `Engine/Sequential/SequentialBackupEngine.cs` | Baseline engine, men ikke færdig |
| `Engine/LimitedParallel/LimitedParallelBackupEngine.cs` | Placeholder til senere |

---

## 3. NEED - næste filer og klasser at bygge

Dette er den korteste vej til en virkende Tier 1 Core4.

### 3.1 Færdiggør sekventiel filsystem-backup

| Rækkefølge | Fil | Klasse | Note |
|---|---|---|---|
| 1 | `Transfer/IFileTransfer.cs` | `IFileTransfer` | Basal transfer-contract |
| 2 | `Sidecar/SidecarData.cs` | `SidecarData` | Minimal sidecar-model |
| 3 | `Sidecar/ISidecarGenerator.cs` | `ISidecarGenerator` | Sidecar-contract |
| 4 | `Progress/ProgressTracker.cs` | `ProgressTracker` | Samler tællere og active files |
| 5 | `Transfer/Filesystem/FilesystemFileTransfer.cs` | `FilesystemFileTransfer` | Kopierer filsystem-filer |
| 6 | `Sidecar/JsonSidecarGenerator.cs` | `JsonSidecarGenerator` | Skriver `.sidecar.json` |
| 7 | `Engine/Sequential/SequentialBackupEngine.cs` | `SequentialBackupEngine` | Færdiggør scan -> transfer -> sidecar -> result |

Resultat efter dette trin:

- filsystem-backup virker sekventielt
- relative paths bevares
- destination directories oprettes
- sidecar skrives efter succesfuld transfer
- `BackupResult` bygges korrekt

### 3.2 Færdiggør composition og options

| Rækkefølge | Fil | Klasse | Note |
|---|---|---|---|
| 8 | `DependencyInjection/Core4Options.cs` | `Core4Options` | Interne options, ikke nye `BackupPlan`-felter |
| 9 | `Engine/BackupEngineFactory.cs` | `BackupEngineFactory` | Vælger engine efter `SourceType` og `MaxDegreeOfParallelism` |
| 10 | `DependencyInjection/ServiceCollectionExtensions.cs` | `ServiceCollectionExtensions` | `AddBMTP3Core4(...)` |

Factory-regel:

1. `MediaDevice` -> sekventiel engine
2. `FileSystem` + `MaxDegreeOfParallelism == 1` -> sekventiel engine
3. ellers sekventiel engine indtil Tier 4 findes

### 3.3 Færdiggør MTP Tier 1

| Rækkefølge | Fil | Klasse | Note |
|---|---|---|---|
| 11 | `Scanner/MTP/MtpScanner.cs` | `MtpScanner` | MTP discovery, streaming |
| 12 | `Transfer/MTP/MTPFileTransfer.cs` | `MTPFileTransfer` | MTP download til filsystem |

MTP-regler:

- altid sekventiel
- keepalive mindst hver 30. sekund
- timeout 60 sekunder
- retry 1s, 2s, 4s

Tier 1 er først færdig når både filsystem og MTP virker.

---

## 4. SHOULD - gør baseline troværdig

Når Tier 1 virker, skal disse ting strammes op:

- konsistent cancellation handling
- klar error mapping
- korrekt dry-run-semantik
- tydelig collision-adfærd
- robust sidecar-skrivning
- bedre diagnostics

Disse ligger primært i de allerede eksisterende klasser og i engine-flowet.  
De kræver ikke at du opfinder nye store public DTOs.

---

## 5. NICE - optionelle features bagefter

Disse features kommer **efter** en virkende Tier 1.

| Fil | Klasse | Rolle |
|---|---|---|
| `Features/Hashing/IItemHasher.cs` | `IItemHasher` | Hash-contract |
| `Features/Hashing/HashResult.cs` | `HashResult` | Hash-output |
| `Features/Hashing/FileHasher.cs` | `FileHasher` | SHA-256 hashing |
| `Features/Metadata/IMetadataReader.cs` | `IMetadataReader` | Metadata-contract |
| `Features/Metadata/ExtractedMetadata.cs` | `ExtractedMetadata` | Normaliseret metadata-model |
| `Features/Metadata/MetadataExtractorReader.cs` | `MetadataExtractorReader` | Managed primary reader |
| `Features/Metadata/ExifToolMetadataReader.cs` | `ExifToolMetadataReader` | CLI fallback-reader |
| `Features/Verification/IIntegrityVerifier.cs` | `IIntegrityVerifier` | Verification-contract |
| `Features/Verification/VerificationResult.cs` | `VerificationResult` | Verification-output |
| `Features/Verification/FileIntegrityVerifier.cs` | `FileIntegrityVerifier` | Hash-sammenligning |
| `Features/Timestamp/ITimestampCorrector.cs` | `ITimestampCorrector` | Timestamp-contract |
| `Features/Timestamp/TimestampCorrectionResult.cs` | `TimestampCorrectionResult` | Timestamp-output |
| `Features/Timestamp/FileTimestampCorrector.cs` | `FileTimestampCorrector` | Sætter timestamps |
| `Sidecar/SidecarEnrichment.cs` | `SidecarEnrichment` | Enrichment til sidecars |

Metadata-regel:

1. filsystem-attributter
2. `MetadataExtractorReader`
3. `ExifToolMetadataReader` hvis nødvendigt og aktiveret

---

## 6. LATER - først når baseline er stabil

### 6.1 Tier 4 - begrænset parallel filsystem-engine

| Fil | Klasse | Rolle |
|---|---|---|
| `Engine/LimitedParallel/LimitedParallelBackupEngine.cs` | `LimitedParallelBackupEngine` | Begrænset parallel backup for filsystem |

Regler:

- kun filsystem
- scanner streamer stadig sekventielt
- MTP bruger aldrig denne engine
- public contracts ændres ikke

### 6.2 Tier 5 - persistence og resume

| Fil | Klasse | Rolle |
|---|---|---|
| `Engine/State/IBackupRepository.cs` | `IBackupRepository` | Repository-contract |
| `Engine/State/NoOpBackupRepository.cs` | `NoOpBackupRepository` | Minimal default |
| `Engine/State/FileSystemBackupRepository.cs` | `FileSystemBackupRepository` | Filbaseret persistence |
| `Engine/State/SqliteBackupRepository.cs` | `SqliteBackupRepository` | SQLite persistence |

### 6.3 Tier 6 - avanceret rapportering/UI

Eksempler:

- rigere CLI-visninger
- eksport/summaries
- event-drevne UI-hjælpere

Hvis `IProgressNotifier` introduceres senere, hører det hjemme her - ikke i Tier 1-baseline.

---

## 7. Kort "hvad du IKKE skal gøre"

Undgå disse fejlspor:

- omdøb ikke `BackupResult` til `BackupJobResult`
- split ikke `Source` op i `SourceDirectory` og `DeviceId`
- lav ikke `IBackupItem` som ny public baseline
- tilføj ikke `HashTypes` på `BackupPlan`
- gør ikke ExifTool til eneste metadata-læser
- byg ikke parallel MTP
- flyt ikke sidecar til slutningen af hele jobbet

---

## 8. Hvornår denne TODO-liste er "grøn"

TODO-listen er reelt grøn når:

- sekventiel filsystem-backup virker
- sekventiel MTP-backup virker
- sidecars skrives korrekt
- progress snapshots er troværdige
- de optionelle features kan lægges oven på baseline uden at ændre contracts

---

## 9. Hvis du er i tvivl

Når denne fil og andre dokumenter siger forskellige ting, så brug:

1. `docs\CORE4_IMPLEMENTATION_GUIDE_DA.md`
2. `docs\CORE4_MASTER_SYNTHESIS.md`
3. det aktuelle `BMTP3.Core4`-skeleton

Denne TODO-fil skal kun hjælpe dig med rækkefølgen - ikke definere en alternativ Core4.
