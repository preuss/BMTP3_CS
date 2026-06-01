# Core4 — Mangler / Issues

> **Opdateret 1 Jun 2026** — C4 degraderet til Lav prioritet (gøres sidst).

---

## Resolved since last update

| Item | Status | Evidence |
|------|--------|----------|
| Binary/hash comparison in collision handling (I2) | ✅ **DONE** | `FileCompareService` + `BinaryFileComparerSelector` + 5 algorithms (WholeFile, ChunkedSequenceEqual, ChunkedVector, ChunkedEightByte, ChunkedAvx2). `RenameCollisionResolver.ContentCompareAsync` uses it. All in `Engine/Compare/` and DI-registered. |
| Timestamp correction (ResolveAndApplyEarliestAsync) | ✅ Applied to file + Item.Date* + Metadata | `EarliestTimestampResolutionService.ResolveAndApplyEarliestAsync` — sets all 4 timestamps on item + 3 filesystem timestamps on target file when `enableTimestampCorrection` is true. |
| BackupPlanValidator `EnableTimestampCorrection` gate | ✅ Gate removed | Confirmed: no tier-gate for timestamp correction. |
| `EarliestTimestampResolutionServiceAnother.cs` | ✅ Deleted (dead code) | Confirmed deleted per earlier session. |
| `ConvertToDateTimeOffset` dead method | ✅ Removed | Confirmed removed. |
| Post-run persistence (C1) | ✅ **DONE** | `sessionState.SaveAsync()` in `finally` block at BackupEngine.cs:400. |
| File-backed SummaryStore (C2) | ✅ **DONE** | `BackupJsonSummaryStore` — writes JSON to `.bmtp3/{sessionId}.json` with temp-file + replace atomicity. |
| Real filesystem traversal (C3) | ✅ **DONE** | `FileSystemTraversal` — recursive walk with SafeGetFiles/SafeGetDirectories/SafeGetDate. |
| All dates on temp file (I3a) | ✅ **DONE** | `EarliestTimestampResolutionService` sets `CreationTimeUtc`, `LastWriteTimeUtc`, `LastAccessTimeUtc` on target file. |
| Timestamp correction after MoveTo (I4 in old plan.md — not to be confused with I4 below) | ✅ **DONE** | Applied in `ResolveAndApplyEarliestAsync` — not in the engine's MoveTo step (the file is corrected pre-move at temp stage, survives move). |
| I4/E4: MoveableFileContent.MoveTo overwrite bug | ✅ **FIXED** | `FileInfo.MoveTo(dest, false)` → `FileInfo.MoveTo(dest, overwrite)` — parameteren bruges nu. |
| E1: CompositeTimestampReader CancellationToken | ✅ **FIXED + REDESIGNED** | `CancellationToken` passes nu til sub-readers. Nyt interface-design: `ITimestampReader` (fail-fast), `ICompositeTimestampReader` (TryReadCollect med bool + out errors), `TimestampReaderException` (custom exception med ReaderType). Gammel `out (Type, Exception)` overload fjernet. |
| E2: ThrowIfCancellationRequested | ✅ **DONE** | `cancellationToken.ThrowIfCancellationRequested()` tilføjet i starten af foreach-loop (BackupEngine.cs). |
| FileContent cleanup | ✅ **DONE** | Volatile fjernet fra `_disposed`. Dispose simplificeret (ingen indirektion). Doc comments genindsat. |
| Ubrugt variable `earliest` | ✅ **FIXED** | `earliest.Timestamp` bruges nu direkte på linje 301 i stedet for redundant `ResolveDate()`. Kaster exception hvis null. `ResolveDate()` fjernet. |
| Step numbering jump (8→10) | ✅ **FIXED** | `// 10.` → `// 9. Return BackupResult`. |
| DeleteEmptyDirectories | ✅ **ALREADY DONE** | Allerede implementeret via `CleanupSessionTempDirectory` + `TryDeleteIfEmpty` i finally block. |
| E6: Per-item try-catch | ✅ **FIXED** | Per-item try-catch i processing loop. Failed items markeres som Failed, exception re-thrown (fail-fast). |
| I6: Post-write verification | ✅ **FIXED** | Hash verification efter sidecar, før Success. `PostWriteVerificationType.Binary` fjernet — kun None/Hash. |

---

## Remaining Issues

### Important

- **I3: Post-write verification missing**
  - No `IPostWriteVerification` interface exists in Core4.
  - After `MoveTo`, no re-read or re-hash of the destination file.
  - Cannot detect silent corruption or failed writes.
  - `BackupPlan.PostWriteVerification` field exists but is not implemented.

- **I5: Sidecar does not match Original Core metadata richness**
  - Missing `[DeviceDetails]` / `[DriveDetails]` sections.
  - Missing `[PathMapping]` section (original → sanitized path).
  - Missing backup timestamp `BackupDateTime` (uses `StartTime`).

### Medium

- **E5: DownloadService redundant timestamp-logik** — ✅ **FIXED**
  - `DownloadService.cs` — sætter nu temp-filens `CreationTime`/`LastWriteTime`/`LastAccessTime` individuelt fra `Item.Date*` med `backupStartTime` som per-field fallback.
  - Ingen `TimestampHelpers.FindEarliestValidDate` — ingen overskrivning af `Item.Date*`.

- **E7: Temp cleanup sletter kun tomme dirs** — ❌ **WONTFIX (korrekt adfærd)**
  - `TryDeleteIfEmpty` gør præcis hvad navnet siger — sletter kun hvis tom.
  - `.tmp`-filer er **forensic evidence** efter crash — at slette dem ville ødelægge debug-sporet.
  - Mappen bevares bevidst til fejlfinding.

- **Include/Exclude patterns not implemented**
  - `SourceTraversalRequest` has `IncludePatterns`/`ExcludePatterns` fields, but `FileSystemTraversal` ignores them.

- **DryRun not implemented**
  - `BackupPlan.DryRun` exists but engine doesn't check it; would still download and move.

- **JSON sidecar** — `NotImplementedException` i `SidecarService` (linje 14)

- **I5: Sidecar metadata richness**
  - Missing `[DeviceDetails]` / `[DriveDetails]` sections.
  - Missing `[PathMapping]` section (original → sanitized path).
  - Missing backup timestamp `BackupDateTime` (uses `StartTime`).

- **N1: Destination inspection / cross-run dedup**
  - Læs sidecar hash ved collision for at skip identiske filer.

- **N5: Ryd op ubrugte `BackupRunner`-klasser**
  - Wire eller fjern `BackupRunner`, `ParallelBackupRunner`, `LimitedParallelBackupRunner`.

- **N6: Tests**
  - Skriv tests for HashService, DI, SummaryStore, CollisionHelpers, DiskSpaceValidator.

- **Wire Core4 into Consoles**
  - Consoles programmet bruger stadig Core2.

### Low / Deferred

- **Validator-gates: BackupPlanValidator blokerer implementerede features** — `PostWriteVerification`, `ComparisonHashAlgorithmTypes` m.fl. er blokeret på trods af at engine understøtter dem. **Røres ikke før alt andet er færdigt.**
- **C4: BackupPlanValidator blocks all plans** — tier-gating blokerer selv minimale plans. **Gøres allersidst**, når engine er testet og alle features bekræftet virker.
- **JSON sidecar** — `NotImplementedException` in `SidecarService` (line 14)
- **MTP/MediaDevice support** — `NotSupportedException` in `SourceTraversalFactory`
- **Runner subsystem** — `IBackupRunnerFactory`/`IBackupRunner` exist but not wired (intentional — parallelism deferred)
- **Progress reporting** — per-item `BytesProcessed` only updated during download/hash, not final state
- **Wire Core4 into Consoles** — `ServiceCollectionExtensions.AddBMTP3Core4` exists but Consoles program still uses Core2

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
| E5 | `DownloadService` uses `TimestampHelpers.FindEarliestValidDate` with `backupStartTime` as fallback — redundant now that `EarliestTimestampResolutionService` handles all timestamp logic. Redundant filesystem + item date writes. | `Engine/Downloader/DownloadService.cs:23-38` | Low (cosmetic/redundant work) | ❌ **Pending** |
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
| 4 | `ISideCarDocumentBuilder` / `ISideCarMetaDataBuilder` | ➡️ | Core4 har `ISidecarService` / `SidecarService` — anderledes interface |
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
| 8 | `SidecarGenerators` (`.hash` sidecar) | ⚠️ | Core4 har `SidecarService` — JSON/INI/TEXT, INI ikke implementeret |
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
| 20 | `PathNormalizer` / `GlobMatcher` | ⚠️ | Core4 har `NormalizeRelativeDirectory`/`NormalizeCustomRelativePath` — GlobMatcher mangler |
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
| 8 | `ISidecarGenerator` / `SimpleSidecarGenerator` (INI .sidecar) | ⚠️ | Core4 har `SidecarService` — INI skrivning er NotImplemented |
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
| **INI sidecar** | `NotImplementedException` |
| **DryRun** | `BackupPlan.DryRun` ignoreres |
| **Parallel runner** | `BackupRunner` framework eksisterer men ikke implementeret |
| **Include/Exclude patterns** | `FileSystemTraversal` ignorerer dem |
| **Dedup/FileCategory** | Ingen dedup på tværs af sessioner |
| **State machines** | Core4 har ikke eksplicit state machine (inline status) |
