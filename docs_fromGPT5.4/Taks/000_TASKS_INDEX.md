# Core4 task index

## Regler
- Prefix: `001` til `999`
- Rækkefølge: implementér i nummerorden — hver task kan implementeres alene
- Tags: `[NEED]`, `[SHOULD]`, `[NICE]`, `[LATER]`
- Når du rammer et **⭐ MILESTONE** har du et virkende system. Alt efter tilføjer kun features.

## Inkrementel rækkefølge

```
001–008   Oprydning og preflight (inkl. test-projekt setup)
009–018   Kontrakter (TransferResult, IFileTransfer, Sidecar, Options, Progress)
019–034   Filesystem-engine (Scanner, Transfer, Sidecar, SequentialEngine)
035–044   Tests: filesystem-engine (unit + integration)
045–048   DI / Factory
049       Consoles-integration
          ⭐ MILESTONE 1: Filesystem-backup virker end-to-end fra CLI
050–057   MTP-engine (Scanner, Transfer, retry, timeout, lifecycle)
058–060   Enforcement (validator, IO-regler, sidecar-navn)
061–062   Tests: MTP-engine
          ⭐ MILESTONE 2: MTP-backup virker end-to-end
063–071   [SHOULD] Features (patterns, collision, output, logging)
          ⭐ MILESTONE 3: Komplet SHOULD feature-set
072–080   [NICE] Metadata, hashing, verifikation, timestamp
081–095   [LATER] Parallel engine, repository, UI, afslutning
```

## Opgaver

### Oprydning og preflight

001_[NEED]_delete-class1-placeholder.md
  Slet `BMTP3.Core4/Class1.cs`.

002_[NEED]_delete-unittest1-and-empty-fakes.md
  Slet `BMTP3.Core4.Tests/UnitTest1.cs` og de tre tomme Fakes-stubs
  (`FakeBackupScanner.cs`, `FakeFileTransfer.cs`, `FakeSidecarGenerator.cs`).

003_[NEED]_fix-collisionstrategy-filename-typo.md
  Omdøb `Models/Enums/CollisionStreategy.cs` → `CollisionStrategy.cs`.
  Enum-navnet indeni er korrekt — kun filnavnet har en typo.

004_[NEED]_preflight-align-target-framework.md
  Ret TargetFramework i **begge** csproj'er:
  `BMTP3.Core4.csproj` og `BMTP3.Core4.Tests.csproj` (`net10.0` → match repo).

005_[NEED]_add-projectreference-tests-to-core4.md
  Tilføj `<ProjectReference>` fra `BMTP3.Core4.Tests` → `BMTP3.Core4`.
  Tilføj `[InternalsVisibleTo("BMTP3.Core4.Tests")]` i Core4 (kræves
  fordi de fleste Core4-typer er `internal`).

006_[NEED]_delete-backupengine-public-stub.md
  Slet `Engine/BackupEngine.cs` (den public stub med kun `NotImplementedException`).

007_[NEED]_delete-parallelbackupengine-stub.md
  Slet KUN `Engine/Parallel/ParallelBackupEngine.cs`.
  **Behold** `Engine/LimitedParallel/LimitedParallelBackupEngine.cs` — den bruges i [LATER] task 081.

008_[NEED]_delete-empty-backupsessionstore-stub.md
  Slet `Engine/State/BackupSessionStore.cs` (tom klasse).
  **Behold** `InMemoryBackupSessionStateStore.cs` — den er implementeret og bruges.

### Kontrakter

009_[NEED]_create-transferresult.md
  Opret `Transfer/TransferResult.cs` (sealed record).
  Afhænger af: `BackupErrorCode` (eksisterer).

010_[NEED]_create-ifiletransfer.md
  Opret `Transfer/IFileTransfer.cs` (internal interface).
  Afhænger af: `BackupItem` (eksisterer), `TransferResult` (009).

011_[NEED]_create-sidecardata.md
  Opret `Sidecar/SidecarData.cs` (sealed class, JSON-serialiserbar).

012_[NEED]_create-sidecarenrichment-stub.md
  Opret `Sidecar/SidecarEnrichment.cs` (sealed class, alle felter nullable).
  Denne klasse skal eksistere nu fordi `ISidecarGenerator.UpdateAsync` (task 013)
  refererer til den i sin signatur. Felterne udfyldes først i [NICE]-fasen.

013_[NEED]_create-isidecargenerator.md
  Opret `Sidecar/ISidecarGenerator.cs` (internal interface).
  Afhænger af: `BackupItem`, `TransferResult` (009), `SidecarEnrichment` (012).

014_[NEED]_create-core4options.md
  Opret `DependencyInjection/Core4Options.cs` (public class med defaults).
  Flyttes hertil fordi `FilesystemFileTransfer` (020) bruger
  `Core4Options.TransferBufferSizeBytes`. Tilføj også NuGet-ref til
  `Microsoft.Extensions.Options` hvis nødvendigt.

015_[NEED]_create-fileprogresssnapshot.md
  Opret `Progress/FileProgressSnapshot.cs` (implementerer `IFileProgress`).

016_[NEED]_create-backupprogresssnapshot.md
  Opret `Progress/BackupProgressSnapshot.cs` (implementerer `IBackupProgress`).
  Afhænger af: `FileProgressSnapshot` (015).

017_[NEED]_create-progresstracker-counters.md
  Opret `Progress/ProgressTracker.cs` med tæller-felter og `SetPhase`,
  `ItemDiscovered`, `DirectoryScanned`.
  Brug `Interlocked` for thread-safety.

018_[NEED]_create-progresstracker-activefiles.md
  Udvid `ProgressTracker` med `StartFile`, `UpdateFileBytesTransferred`,
  `CompleteFile`, `SkipFile`, `GetSnapshot()`.
  Brug `ConcurrentDictionary` for active files.

### Filesystem-engine

019_[NEED]_implement-filesystemitemscanner-basic.md
  Opret `Scanner/Filesystem/FilesystemItemScanner.cs` (implementerer `IBackupScanner`).
  Brug `Directory.EnumerateFiles`. Yield `BackupItem` per fil.
  Foundation: ingen glob-filter endnu — yield alle filer.

020_[NEED]_implement-filesystemfiletransfer-temp-copy.md
  Opret `Transfer/Filesystem/FilesystemFileTransfer.cs` (implementerer `IFileTransfer`).
  Kopiér kilde → `dest.tmp` i chunks (buffer fra `Core4Options`, default 1 MB).
  Atomisk rename `.tmp` → endelig sti.

021_[NEED]_implement-filesystemfiletransfer-progress.md
  Tilføj `bytesProgress?.Report(totalBytesTransferred)` efter hvert chunk.

022_[NEED]_implement-filesystemfiletransfer-cleanup.md
  Ryd `.tmp` op ved Exception. `OperationCanceledException` re-throws altid.
  Alle andre exceptions → `TransferResult { Succeeded = false }`.

023_[NEED]_implement-jsonsidecargenerator-create.md
  Opret `Sidecar/JsonSidecarGenerator.cs` (implementerer `ISidecarGenerator`).
  `CreateAsync`: skriv `SidecarData` som JSON til `item.DestinationPath + ".sidecar.json"`.
  Atomisk: skriv til `.tmp`, rename til final.

024_[NEED]_implement-jsonsidecargenerator-update-stub.md
  `UpdateAsync`: stub (log + return) — virkelig implementation i [NICE]-fasen.

025_[NEED]_implement-sequentialengine-scan-phase.md
  Udvid `SequentialBackupEngine` med `ProgressTracker` i scan-fasen.
  Tilføj `IFileTransfer` og `ISidecarGenerator` til konstruktør.
  Scan-loopet kalder `progressTracker.ItemDiscovered(item)` og
  `progress?.Report(progressTracker.GetSnapshot())`.

026_[NEED]_implement-sequentialengine-transfer-loop.md
  Implementér transfer-loop: `session.GetPendingItems()` →
  `ResolveDestination` → `fileTransfer.TransferAsync` →
  `sidecarGenerator.CreateAsync` ved succes.

027_[NEED]_implement-sequentialengine-resolvedestination.md
  Privat `ResolveDestination(item, plan)` baseret på `OutputStructure`.
  Foundation: `PreserveHierarchy` og `Flat`. Collision-rename kommer i [SHOULD].

028_[NEED]_implement-sequentialengine-buildresult.md
  Privat `BuildResult(session, tracker)` → `BackupResult` fra snapshot + session.

029_[NEED]_implement-sequentialengine-cancellation.md
  Catch `OperationCanceledException` → `session.Cancel()` → return partial result.

030_[NEED]_implement-sequentialengine-validation-failure.md
  Catch `BackupPlanValidationException` → `session.Fail(InvalidConfiguration)`.

031_[NEED]_implement-sequentialengine-fatal-failure.md
  Catch generel `Exception` → `session.Fail(TransferFailed)` → return result.

032_[NEED]_implement-sequentialengine-skipexisting.md
  `if (plan.SkipExisting && File.Exists(dest))` → skip item.

033_[NEED]_implement-sequentialengine-stoponerror.md
  `if (plan.StopOnError && !result.Succeeded)` → `session.Fail(...)` → break.

034_[NEED]_implement-sequentialengine-dryrun.md
  `if (plan.DryRun)` → mark succeeded, skip transfer/sidecar.

### Tests: filesystem-engine

035_[NEED]_unit-test-transferresult-and-contracts.md
036_[NEED]_unit-test-progresstracker.md
037_[NEED]_unit-test-filesystemitemscanner.md
038_[NEED]_unit-test-filesystemfiletransfer.md
039_[NEED]_unit-test-jsonsidecargenerator.md
040_[NEED]_unit-test-sequentialengine-core-flow.md
041_[NEED]_integration-test-fs-scan-transfer-sidecar.md
042_[NEED]_integration-test-fs-per-file-failure-continues.md
043_[NEED]_integration-test-fs-cancellation-partial-result.md
044_[NEED]_integration-test-fs-dryrun-no-writes.md

### DI / Factory

045_[NEED]_create-backupenginefactory.md
  `Engine/BackupEngineFactory.cs` — vælger engine baseret på plan.
  Foundation: returner altid `SequentialBackupEngine`.

046_[NEED]_create-servicecollectionextensions.md
  `DependencyInjection/ServiceCollectionExtensions.cs` — `AddBMTP3Core4()`.
  Tilføj NuGet-ref `Microsoft.Extensions.DependencyInjection.Abstractions`.

047_[NEED]_wire-dependencyinjection-registrations.md
  Registrér alle services: Scanner, Transfer, Sidecar, ProgressTracker,
  SessionStateStore, Engine, Factory.

048_[NEED]_wire-ibackupengine-factory-resolution.md
  `IBackupEngine` resolved via `BackupEngineFactory.Create(plan)`.

### Consoles-integration

049_[NEED]_wire-core4-into-bmtp3-consoles.md
  Tilføj `services.AddBMTP3Core4()` i Consoles' DI-setup.
  Tilføj ny ConsoleCommand (eller udvid eksisterende) der kalder
  `IBackupEngine.RunAsync`.

---
⭐ MILESTONE 1 — efter task 049:
Core4 kan køres fra BMTP3.Consoles CLI.
Filesystem-backup virker end-to-end (scan → transfer → sidecar → result).
Alt herefter tilføjer features — intet i det eksisterende skal brydes.
---

### MTP-engine

050_[NEED]_create-mtpitemscanner-skeleton.md
  `Scanner/MTP/MTPItemScanner.cs` (implementerer `IBackupScanner`).
  Tilføj MediaDevices DLL-reference til csproj.

051_[NEED]_create-mtpfiletransfer-skeleton.md
  `Transfer/MTP/MTPFileTransfer.cs` (implementerer `IFileTransfer`).

052_[NEED]_implement-mtp-scanner-stack-traversal.md
  Stack-baseret (ingen rekursion), håndter `COMException` per mappe.

053_[NEED]_implement-mtp-transfer-temp-copy.md
  Download til temp-fil, atomisk rename.

054_[NEED]_implement-mtp-transfer-retry-backoff.md
  Retry 3× med 1s/2s/4s exponential backoff (fra `Core4Options`).

055_[NEED]_implement-mtp-timeout-handling.md
  Timeout per operation via `Core4Options.MtpOperationTimeout`.

056_[NEED]_implement-mtp-session-lifecycle-integration.md
  MTP-session åbnes af kaldende klasse, ikke af scanner/transfer.
  Keepalive mindst hver `Core4Options.MtpKeepAliveInterval`.

057_[NEED]_enforce-mtp-sequential-only.md
  Validator/factory: `SourceType == MediaDevice` → altid sequential.

### Enforcement

058_[NEED]_enforce-feature-gating-in-validator.md
  Validator: flag-kombinations der kræver endnu ikke implementerede features
  → ValidationException med klar besked.

059_[NEED]_enforce-buffered-streaming-io-rules.md
  Alle I/O bruger buffered streams og chunk-baseret læsning.

060_[NEED]_enforce-sidecar-name-sidecar-json.md
  Sidecar-sti er altid `item.DestinationPath + ".sidecar.json"`.

### Tests: MTP-engine

061_[NEED]_integration-test-mtp-basic-flow.md
062_[NEED]_integration-test-mtp-retry-and-disconnect.md

---
⭐ MILESTONE 2 — efter task 062:
MTP-backup (kamera/telefon) virker end-to-end.
Alt herefter er feature-udvidelse — ingen NEED-krav mangler.
---

### [SHOULD] Features

063_[SHOULD]_implement-include-exclude-pattern-matching.md
  Brug `Microsoft.Extensions.FileSystemGlobbing.Matcher` i scanner.

064_[SHOULD]_implement-collisionstrategy-rename.md
  `ResolveDestination`: tilføj `_1`, `_2` etc. ved navnekollision.

065_[SHOULD]_implement-collisionstrategy-overwrite.md
  `File.Move(tmp, dest, overwrite: true)` ved `Overwrite`-strategi.

066_[SHOULD]_implement-outputstructure-flat-and-hierarchy.md
  Verificér at `Flat` og `PreserveHierarchy` virker korrekt end-to-end.

067_[SHOULD]_implement-progress-debounce-500ms.md
  Rapportér progress højst hver 500ms (Stopwatch-baseret guard).

068_[SHOULD]_integrate-ilogger-structured-logging.md
  Tilføj `ILogger<T>` til engine, scanner, transfer, sidecar.

069_[SHOULD]_harden-errorcode-mapping.md
  Alle kendte exception-typer → korrekt `BackupErrorCode`.

070_[SHOULD]_harden-dryrun-full-simulation.md
  Dry-run kører collision/destination-logik, bare ingen I/O.

071_[SHOULD]_tests-for-patterns-collision-dryrun.md

---
⭐ MILESTONE 3 — efter task 071:
Komplet SHOULD feature-set. Klar til produktion.
---

### [NICE] Metadata, hashing, verifikation, timestamp

072_[NICE]_create-hashing-contracts-and-filehasher.md
  `Features/Hashing/`: `IItemHasher`, `HashResult`, `FileHasher` (SHA-256).

073_[NICE]_create-metadata-contracts-and-models.md
  `Features/Metadata/`: `IMetadataReader`, `ExtractedMetadata`.

074_[NICE]_implement-metadataextractorreader.md
  `MetadataExtractorReader` via MetadataExtractor NuGet (managed primary).

075_[NICE]_implement-exiftoolmetadatareader-fallback.md
  `ExifToolMetadataReader` via `exiftool.exe -json` (CLI fallback).

076_[NICE]_implement-bestdate-selection-rules.md
  `GetBestDate()`: DateTimeOriginal → CreateDate → QuickTimeCreated →
  FileSystemLastWrite → FileSystemCreation → null.

077_[NICE]_create-verification-contracts-and-verifier.md
  `Features/Verification/`: `IIntegrityVerifier`, `VerificationResult`,
  `FileIntegrityVerifier`.

078_[NICE]_create-timestamp-contracts-and-corrector.md
  `Features/Timestamp/`: `ITimestampCorrector`, `TimestampCorrectionResult`,
  `FileTimestampCorrector`.

079_[NICE]_wire-enrichment-pipeline-after-sidecar.md
  Kald enrichment (hash → metadata → verify → timestamp → sidecar.UpdateAsync)
  efter succesfuld transfer. Brug `SidecarEnrichment` (oprettet i task 012).

080_[NICE]_tests-metadata-hash-verify-timestamp.md

### [LATER] Parallel engine, repository, UI, afslutning

081_[LATER]_implement-limitedparallelbackupengine.md
082_[LATER]_wire-factory-to-parallel-for-filesystem.md
083_[LATER]_parallel-engine-threadsafe-progress.md
084_[LATER]_parallel-engine-consistency-tests.md
085_[LATER]_create-ibackuprepository.md
086_[LATER]_create-noopbackuprepository.md
087_[LATER]_create-filesystembackuprepository.md
088_[LATER]_create-sqlitebackuprepository.md
089_[LATER]_wire-session-persistence-and-resume.md
090_[LATER]_resume-integration-tests.md
091_[LATER]_create-iprogressnotifier.md
092_[LATER]_create-spectreprogressnotifier.md
093_[LATER]_wire-rich-ui-progress-events.md
094_[LATER]_reporting-eta-throughput-workers.md
095_[LATER]_final-regression-and-completion-checklist.md
