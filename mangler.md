# Core4 — Mangler / Issues

> **Opdateret 3 Jun 2026** — Sidecar redesign completed. DryRun done. ItemMetadata udvidet med originale datoer. Writer Stream refactoring + tests completed. Ctrl+C Del 1 completed.

> ⚠️ **REGEL: Ingen validator-gates må fjernes før BackupEngine er erklæret færdig.** `BackupPlanValidator` kaster `FeatureNotImplementedException(N, ...)` for inaktive features — linje 99-103 (include/exclude patterns), 105-106 (custom output pattern), 111-112 (dry run), 114-118 (hash algorithm selection) m.fl. Disse gates blokerer testindtilingsforsøg på features der ikke er implementationse. De røres **sidst** — når engine-loopen er verificeret stabil.

---

## Resolved since last update

| Item | Status | Evidence |
|------|--------|----------|
| DryRun not implemented | ✅ **DONE** | `BuildDryRunResult` helper, short-circuit før processing loop. `BackupResult.IsDryRun = true`. |
| ParallelBackupRunner cleanup | ✅ **DONE** | `ParallelBackupRunner` + `LimitedParallelBackupRunner` slettet. `BackupRunner` beholdt. |
| N5: Ryd op ubrugte `BackupRunner`-klasser | ✅ **DONE** | `ParallelBackupRunner`/`LimitedParallelBackupRunner` slettet. |
| Sidecar redesign: Document/Section/Property model | ✅ **DONE** | `SidecarProperty`, `SidecarSection`, `SidecarDocument` (fluent API + weight-sortering). |
| `#` comment support i INI sidecar | ✅ **DONE** | `IniSidecarWriter` skriver `#` kommentarer (multi-line split). |
| INI sidecar `NotImplementedException` | ✅ **DONE** | INI writer fully implemented. |
| JSON sidecar `NotImplementedException` | ✅ **DONE** | `JsonSidecarWriter` implemented. |
| Sidecar format: `[Source]`, `[SourceDevice]`, `[SourceDrive]`, `[Backup]`, `[Path]`, `[Hashes]` | ✅ **DONE** | Matcher brugerens spec. |
| Sidecar: alle hashes altid til stede | ✅ **DONE** | Alle HashType keys + `SHA3_512` alias for `SHA3_512_FIPS202`. |
| Sidecar: `MD5` i stedet for `MD5_128` | ✅ **DONE** | Key name mapping i `SidecarService`. |
| I5: Sidecar metadata richness (device/drive details) | ✅ **DONE** | `SourceDetailsSectionName` + `SourceDetails` i `SidecarRequest`. |
| I5: `[Path]` med SourceRelativePath / SanitizedSourceRelativePath / TargetRelativePath | ✅ **DONE** | All three paths in sidecar. |
| `ResolvedDateTime` → `MediaTakenDateTime` rename (Core4) | ✅ **DONE** | `ItemMetadata.cs`, `EarliestTimestampResolutionService.cs`, `BackupEngine.cs`. |
| `ItemMetadata` udvidet med originale datoer | ✅ **DONE** | `AuthoredDateTime`, `CreatedDateTime`, `ModifiedDateTime`, `AccessedDateTime` added. Captured before timestamp correction. |
| Sidecar læser datoer fra `ItemMetadata` i stedet for `Item` | ✅ **DONE** | `BackupEngine` sidecar construction uses `record.Metadata.*`. |
| Sidecar comments på engelsk | ✅ **DONE** | All comments translated to English. |
| I7: Include/Exclude patterns | ✅ **DONE** | `GlobMatcher.IsIncluded` i `FileSystemTraversal`. Gates beholdt (regel). |
| Writer Stream refactoring | ✅ **DONE** | `ISidecarWriter.WriteToFileAsync` → `WriteToStreamAsync(Stream)`. `IniSidecarWriter`/`JsonSidecarWriter` skriver til Stream. `SidecarService` åbner `FileStream`. |
| IniSidecarWriter forbedret | ✅ **DONE** | `WriteCommentBlock` ekstraheret med `StringReader.ReadLine()`. `IniSidecarWriterOptions` (PreserveEmptyCommentLines, WriteKeysWithNullValues). CRLF line endings. |
| JsonSidecarWriter forbedret | ✅ **DONE** | `CreateSerializableModel` ekstraheret. `SerializeAsync(stream)` — ingen mellemstring. |
| N6: SidecarServiceTests | ✅ **DONE** | SidecarServiceTests (6), IniSidecarWriterTests (14), JsonSidecarWriterTests (10) = 30 nye tests. I alt 160 tests. |
| Ctrl+C Del 1: BackupEngine catch OCE → Cancelled | ✅ **DONE** | `try { ... } catch(OperationCanceledException)` returnerer `BackupResult` med `State = BackupResultState.Cancelled`. `BuildItemResults` udtrukket. |

---

## Remaining Issues

### Important

- **Ctrl+C — Del 1 ✅ (Engine), Del 2 mangler**
  - Del 1 ✅: `BackupEngine.RunAsync` fanger `OperationCanceledException` → returnerer `BackupResult` med `State = Cancelled`.
  - Del 2 ❌: `ConsoleEventHandler` (kernel32 + CancelKeyPress) mangler i Core4.
  - Del 3 ❌: Wiring i Consoles entry point.
  - Del 4 ❌: Tests (blokeret af validator-gates).

### Medium

- **E5: DownloadService redundant timestamp-logik** — ✅ **FIXED**
  - `DownloadService.cs` — sætter nu temp-filens `CreationTime`/`LastWriteTime`/`LastAccessTime` individuelt fra `Item.Date*` med `backupStartTime` som per-field fallback.
  - Ingen `TimestampHelpers.FindEarliestValidDate` — ingen overskrivning af `Item.Date*`.

- **E7: Temp cleanup sletter kun tomme dirs** — ❌ **WONTFIX (korrekt adfærd)**
  - `TryDeleteIfEmpty` gør præcis hvad navnet siger — sletter kun hvis tom.
  - `.tmp`-filer er **forensic evidence** efter crash — at slette dem ville ødelægge debug-sporet.
  - Mappen bevares bevidst til fejlfinding.

- **N6: Tests** ✅ **DONE** — 30 nye tests
  - HashServiceTests ✅, DITests ✅, CollisionResolverTests ✅, RenameCollisionResolverTests ✅
  - SidecarServiceTests ✅ (6 tests)
  - IniSidecarWriterTests ✅ (14 tests)
  - JsonSidecarWriterTests ✅ (10 tests)
  - I alt: **160 tests**

- **Wire Core4 into Consoles**
  - Consoles programmet bruger stadig Core2.

### Low / Deferred

- **N1: Destination inspection / cross-run dedup**
  - Læs sidecar hash ved collision for at skip identiske filer.
  - TODO i `RenameCollisionResolver.cs:213` — prøv sidecar før `FileContent`/`ComputeHashesAsync`.
  - Se Core2 reference: `Engine/Strategies/SidecarReader.cs`, `IDestinationInspector.cs`, `DestinationInspectorItemStep.cs`.
  - Kræver: `ISidecarReader` interface, INI/JSON parser for `[Hashes]`, injection i `RenameCollisionResolver`.
- **Validator-gates: BackupPlanValidator blokerer implementerede features** — `PostWriteVerification`, `ComparisonHashAlgorithmTypes` m.fl. er blokeret på trods af at engine understøtter dem. **Røres ikke før alt andet er færdigt.**
- **C4: BackupPlanValidator blocks all plans** — tier-gating blokerer selv minimale plans. **Gøres allersidst**, når engine er testet og alle features bekræftet virker.
- **MTP/MediaDevice support** — `NotSupportedException` in `SourceTraversalFactory`
- **Progress reporting** — per-item `BytesProcessed` only updated during download/hash, not final state

---

## Edge Case Audit (31 May 2026)

Gennemgang af CancellationToken-flow, error recovery, temp cleanup og I/O edge cases.

### CancellationToken Issues

| # | Issue | File | Severity | Status |
|:--|-------|------|----------|--------|
| E1 | `CompositeTimestampReader` ignores CancellationToken | ~~`CompositeTimestampReader.cs:44-48` → `:73`~~ | **High** | ✅ **FIXED** — `CancellationToken` passes nu. Nyt interface-design: `ICompositeTimestampReader` + `TryReadCollect`. |
| E2 | No `ThrowIfCancellationRequested()` at processing loop top | ~~`Engine/BackupEngine.cs:219`~~ | Low | ✅ **FIXED** — tilføjet i starten af foreach-loop. |

### Bug Fixes Needed

| # | Issue | File | Severity | Status |
|:--|-------|------|----------|--------|
| E4 | `MoveableFileContent.MoveTo` hardcodes `FileInfo.MoveTo(destinationPath, false)` ignoring the `overwrite` parameter | `Models/MoveableFileContent.cs:26` | **Critical** (same as I4 above) | ✅ **FIXED** — `overwrite` parameteren bruges nu. |
| E5 | `DownloadService` uses `TimestampHelpers.FindEarliestValidDate` with `backupStartTime` as fallback — redundant now that `EarliestTimestampResolutionService` handles all timestamp logic. Redundant filesystem + item date writes. | `Engine/Downloader/DownloadService.cs:23-38` | Low (cosmetic/redundant work) | ✅ **FIXED** |
| E6 | No per-item try-catch in processing loop — any exception (download, hash, move, sidecar) aborts the entire backup, not just that one item | ~~`Engine/BackupEngine.cs:219-397`~~ | **High** | ✅ **FIXED** — per-item try-catch tilføjet. Failed items markeres som Failed, exception re-thrown (fail-fast). |

### Temp Cleanup

| # | Issue | File | Severity |
|---|-------|------|----------|
| E7 | `CleanupSessionTempDirectory` only deletes empty directories — orphaned `.tmp` files from failed moves accumulate if `MoveTo` fails mid-session | `Engine/TempDirectoryHelper.cs:270` | Low (session cleanup is best-effort) |
| E8 | `CleanupTempFiles` swallows all exceptions — orphaned temp files possible after crash | `Engine/TempDirectoryHelper.cs:293-314` | Low (acceptable) |

### Resilience

| # | Issue | Severity |
|---|-------|----------|
| E9 | No retry/backoff for any I/O (download, hash, move, sidecar write) | **Medium** (deferred — Core4 design choice) |
| E10 | Locked source files — `IContent.OpenReadStreamAsync` will throw; no graceful handling | Low (fail-fast acceptable) |
| E11 | Disk full during download — no pre-check per-item, `FileStream.WriteAsync` throws IOE | Low (rare, fail-fast acceptable) |

---

## Feature Audit — Core / Core2 / Core3

Gennemgang af alle features i Core (136 .cs), Core2 (150 .cs) og Core3 (19 .cs) krydsrefereret mod Core4.
Status: ✅ = Implementeret, ❌ = Mangler, ⚠️ = Delvist/anderledes, ➡️ = Arkitekturforskellig (ikke 1:1)

### Core

| # | Feature | Status | Noter |
|---|---|---|---|
| 1 | `IBackupHandler` / handler-hierarki (Device, Drive, MediaDevice, Print, Verify) | ➡️ | Core4 har samlet `BackupEngine` i stedet for per-source handlers |
| 2 | `IBackupScanner` / `ScannerGatherer` | ⚠️ | Core4 har `IBackupScanner` men `ScannerGathererStub` — scanner-logik mangler |
| 3 | `IFileComparer` + 8 chunked compare algoritmer + `Md5Comparer` | ✅ | Core4 har `IFileCompareService` + `BinaryFileComparerSelector` + 5 algoritmer |
| 4 | `ISideCarDocumentBuilder` / `ISideCarMetaDataBuilder` | ➡️ | Core4 har `ISidecarService` / `SidecarService` — Document/Section/Property model |
| 5 | `BackupTimeStamp` / `BackupTimeStampForDevice` / `BackupTimeStampForDrive` | ➡️ | Core4 har `IEarliestTimestampResolutionService` — langt mere avanceret |
| 6 | `IHashCode` / `IHashCodeStringBuilder` / `HashCalculator` / `HashCode` | ✅ | Core4 har `IHashGenerator` / `IHashService` / `StreamHashGenerator` |
| 7 | `IMessageFormatter` + `StringVariableSubstitution.Template` | ✅ | Fælles i `BMTP3.Common.MessageFormatterParser` |
| 8 | `IBackupPathExtension` | ✅ | Core4 har `ITargetPathResolver` |
| 9 | `IEventLogger` | ➡️ | Core4 bruger `ILogger<T>` fra MS.Extensions |
| 10 | `IGetLatestItem` / `IGetLatestItemFromIndex` | ➡️ | Core4 har `ISessionStateService` / `IBackupRecordRepository` |
| 11 | `BackupMaster` / `BackupHelper` / `ConfigurationHandler` | ❌ | Orchestrator/helper — nogle dele mangler i Core4 |
| 12 | `MediaDeviceServiceProd` / `IMediaDeviceService` | ❌ | MTP kaster `NotSupportedException` i Core4 |
| 13 | `NExifTool` / `MetadataExtractorFileInfo` / `AbstractMetadataFileInfo` | ➡️ | Core4 bruger MetadataExtractor i stedet for ExifTool |
| 14 | `VerifyBackupHandler` | ⚠️ | Core4 har inline hash verification i `BackupEngine` (`plan.PostWriteVerification == Hash`). Ingen separat handler/interface. |
| 15 | `BackupRecordDataStore` / `BackupRecordDataStorePathResolver` | ✅ | Core4 har `BackupJsonSummaryStore` / `SessionStateService` |
| 16 | TOML config (`BackupSettingsImpl`, `BackupSettingsReader`, `ConfigModel`) | ❌ | Core4 bruger programmatisk `BackupPlan` — ingen TOML-reader |
| 17 | `IMasterTypeRegistrar` / `ServiceLocator` (custom DI) | ➡️ | Core4 bruger MS.DependencyInjection |
| 18 | Crypto helpers (8 x SharpHash + BouncyCastle) | ✅ | Core4 har samme i `Hashing/Crypto/` |
| 19 | `RenameStrategyDefault` / `RenameStrategyWithTimestamp` / `RenameStrategyNumbering` | ✅ | Core4 har `RenameCollisionResolver` med 4 strategier + Custom |
| 20 | `CopyStrategy` / `MoveStrategy` / `SymlinkStrategy` | ➡️ | Core4 har `IMoveableContent.MoveTo()` — flytning, ikke copy |

### Core2

| # | Feature | Status | Noter |
|---|---|---|---|
| 1 | `BackupEngine` / `BackupEngineSequentiel` | ➡️ | Core2 har 7-step pipeline; Core4 har strategi-baseret loop |
| 2 | `SequentialItemPipeline` + 7 `PipelineStep` (Init→Hash→Compare→Copy→Verify→Sidecar→Finalize) | ⚠️ | Core4 har lignende flow men ikke step-klasser; verify-step er inline hash check |
| 3 | `PathGenerator` (path resolution) | ✅ | Core4 har `TargetPathResolver` med PreserveHierarchy/Flat/Custom |
| 4 | `CollisionResolver` (rename/overwrite/skip) | ✅ | Core4 har `CollisionResolver` + `RenameCollisionResolver` |
| 5 | `DestinationInspector` | ❌ | Destination inspection/scoping mangler i Core4 |
| 6 | `MetadataReader` / `TimestampWaterfall` | ⚠️ | Core4 har `EarliestTimestampResolutionService` — mere avanceret |
| 7 | `ItemHasher` | ✅ | Core4 har `HashService` / `StreamHashGenerator` |
| 8 | `SidecarGenerators` (`.hash` sidecar) | ✅ | Core4 har `SidecarService` — INI/JSON/TEXT |
| 9 | `IHashAdapter` + 8 implementeringer (MD5, SHA1, SHA256, SHA512, BLAKE3, XXH3, CRC32, CRC64) | ⚠️ | Core4 har 9 hash-typer (incl. 2 x SHA3, 2 x BLAKE3) men ikke CRC/XXH3/SHA1 |
| 10 | `ITransferEngine` / `FileTransferEngine` | ➡️ | Core4 har `IDownloadService` + `IMoveableContent` |
| 11 | `IFileTraversalService` / `FileTraversalService` / `TraversalConfig` | ⚠️ | Core4 har `ISourceTraversal` + `FileSystemTraversal` — ligner |
| 12 | `BackupResiliencePipeline` (Polly retry/circuit-breaker) | ❌ | Core4 har ingen resilience/retry |
| 13 | `BackupStateMachine` / `ProcessingStateMachine` / `ProcessingStateMachineFactory` | ❌ | Core4 har ingen state machines — inline status i record |
| 14 | `ItemStagingArea` / `StagingItem` | ❌ | Core4 har `TempDirectoryHelper` men ikke staging abstraction |
| 15 | `ParallelTransferOrchestrator` | ❌ | Core4 har `BackupRunner` framework men ikke implementeret |
| 16 | `IBackupItem` / `BackupItem` / `BackupMetadata` | ➡️ | Core4 har `BackupItem` / `ItemMetadata` — lignende men forskellige |
| 17 | `IContent` / `FileContent` / `MediaFileContent` / `GatekeptStream` / `IMoveableContent` | ✅ | Core4 har samme mønster: `IContent` / `FileContent` / `IMoveableContent` / `MoveableFileContent` |
| 18 | `IFileScanner` / `IMediaFileScanner` / `DirectoryScanner` / `MediaFileScanner` | ⚠️ | Core4 har `FileSystemTraversal` — scanner-logik mangler for MTP/media |
| 19 | `MTPGatekeeperService` / `MTPMtpDeviceSession` / `MTPMtpDeviceSessionFactory` / `MtpDeviceUtils` | ❌ | Core4: MTP kaster `NotSupportedException` |
| 20 | `PathNormalizer` / `GlobMatcher` | ⚠️ | Core4 har `NormalizeRelativeDirectory`/`NormalizeCustomRelativePath` + `GlobMatcher` |
| 21 | `exifreader` (15 parsers, 11 readers, 14 candidates, tag definitions, formatter) | ✅ | Core4 har timestamp subsystem i `Engine/TimeStamp/` — samme kodebase flyttet |
| 22 | `IBackupItemRepository` / `BackupItemRepository` / `IFileAttributeRepository` / `ITimestampRepository` | ❌ | Core4 har `IBackupRecordRepository` / `SessionStateService` — anderledes scope |
| 23 | `BackupJob` / `JobState` / `BackupError` / `BackupErrorType` | ⚠️ | Core4 har `BackupPlan` / `BackupResult` / `BackupResultItem` — lignende |
| 24 | `BackupMode` (Full/Incremental/Differential/Snapshot) | ❌ | Core4 understøtter kun full backup |
| 25 | `CollisionResolutionStrategy` (Skip, Overwrite, Rename, Compare, Prompt) | ⚠️ | Core4 har `CollisionStrategy` + `CollisionComparisonType` — ikke `Prompt` |
| 26 | `DuplicateHandling` (KeepAll/SkipDuplicates/Replace) | ❌ | Core4 har ikke dedup på tværs af items |
| 27 | `ResiliencePipeline` (Default/HighLatency/MTP) | ❌ | Core4 har ingen resilience |
| 28 | `FileCategory` (Document/Image/Video/Audio etc.) | ❌ | Core4 har ikke file kategorisering |
| 29 | `ServiceCollectionExtensions.AddBMTP3Core2()` | ✅ | Core4 har `AddBMTP3Core4()` |

### Core3

Core3 er en minimal sekventiel reference-implementation (19 filer). Core4 dækker næsten alt.

| # | Feature | Status | Noter |
|---|---|---|---|
| 1 | `IBackupEngine` / `BackupEngineSequential` | ✅ | Core4 har `IBackupEngine` / `BackupEngine` — mere avanceret |
| 2 | `IBackupProgress` / `BackupProgress` | ✅ | Core4 har `BackupProgress` / `BackupProgressItem` |
| 3 | `BackupPhase` (6 faser: Scan→Transfer→Metadata→Hash→Timestamp→Sidecar) | ✅ | Core4 har samme flow men ikke som enum |
| 4 | `IBackupScanner` / `FileSystemScanner` | ⚠️ | Core4 har scanner men `ScannerGathererStub` |
| 5 | `IFileTransfer` / `SimpleFileTransfer` (buffered copy + collision) | ➡️ | Core4 har `IDownloadService` + `IMoveableContent.MoveTo()` |
| 6 | `IHashGenerator` / `IItemHasher` / `StreamHashGenerator` + `Blake3Digest` | ✅ | Core4 har samme mønster i `Engine/Hashing/` |
| 7 | `IMetadataReader` / `FileMetadataReader` (basic FileInfo metadata) | ➡️ | Core4 har `EarliestTimestampResolutionService` — langt mere |
| 8 | `ISidecarGenerator` / `SimpleSidecarGenerator` (INI .sidecar) | ✅ | Core4 har `SidecarService` — INI + JSON + Document model |
| 9 | `BackupItem` (immutable record with With* helpers) | ➡️ | Core4 har `BackupItem` (mutable class) — forskelligt mønster |
| 10 | `BackupJobResult` / `BackupError` | ✅ | Core4 har `BackupResult` / `BackupResultItem` |
| 11 | `BackupPlan` / `CollisionStrategy` / `HashType` | ✅ | Core4 har alle tre, mere udvidede |
| 12 | `ServiceCollectionExtensions.AddBMTP3Core3()` | ✅ | Core4 har `AddBMTP3Core4()` |

### Sammenfatning — væsentlige huller i Core4

| Område | Hvad mangler |
|---|---|
| **Scanner** | `ScannerGathererStub` — ikke implementeret; MTP traversal kaster `NotSupportedException` |
| **Verify** | ⚠️ | Hash verification implementeret inline i `BackupEngine`. Ingen separat `IPostWriteVerification`. |
| **Resilience** | Ingen retry/circuit-breaker (Core2 har Polly pipeline) |
| **MTP** | `NotSupportedException` — ingen MTP device support |
| **TOML config** | Ingen TOML-reader; kun programmatisk `BackupPlan` |
| **INI/JSON sidecar** | ✅ **DONE** — Full Document/Section/Property model + Ini + Json writers |
| **DryRun** | ✅ **DONE** — `BuildDryRunResult` helper, short-circuit |
| **Parallel runner** | `BackupRunner` beholdt. `ParallelBackupRunner`/`LimitedParallelBackupRunner` slettet. |
| **Include/Exclude patterns** | `GlobMatcher.IsIncluded` i `FileSystemTraversal`. Gates ikke fjernet (regel). |
| **Dedup/FileCategory** | Ingen dedup på tværs af sessioner |
| **State machines** | Core4 har ikke eksplicit state machine (inline status) |
