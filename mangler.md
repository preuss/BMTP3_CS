# Core4 — Mangler / Issues

> **Opdateret 11 Jun 2026** — Spectre Console progress redesign ✅. Custom columns/spinners kopieret til Consoles. Core/Core2/Core3 officielt archived/readonly.
> **#1 prioritet:** Spectre Console — Fix ConsolesServiceSetup wiring, progress redesign (Task 09), CLI cleanup (Task 02).
> 
> ⚠️ **NO IMPLEMENTATION WITHOUT PERMISSION:** Spørg altid først. Implementér aldrig før brugeren siger "go" / "do it" / "implementér" / "execute" / "kør". Indtil da: research, read, grep, spørg.
> 
> ⚠️ **FAIL-FIRST:** Alle gates/tjek i traversal og engine skal kaste exception ved fejl — aldrig `yield break`, `return` eller `continue` for at tie stille om problemer. Source der ikke findes = throw. Eneste undtagelse: per-item try-catch der markerer failed items men re-thrower (fail-fast).
> 
> ⚠️ **NO CORE/CORE2/CORE3 CHANGES:** `BMTP3.Core`, `BMTP3.Core2`, `BMTP3.Core3` og deres Consoles commands (`BackupConsoleCommand.cs`, `BackupConsoleCommand2.cs`, `BackupConsoleCommand3.cs`) er **archived/readonly** — de ændres aldrig. Kun `BMTP3.Core4` og `BMTP3.Consoles` må redigeres.
>
> ⚠️ **CORE4-ONLY FOKUS:** Vi retter **aldrig** noget som ikke har direkte Core4-tilhørsforhold. Core2/Core3 cleanup, shared helpers på tværs af archived kode, og dokumentation af ikke-Core4 ting er aldrig fokus. Når Core4 er 100% færdig, slettes alle tilhørsforhold til Core2/Core3 i Consoles.
> 
> ⚠️ **PATH NAMING STANDARD:** Se `plan.md` § Path Naming Standard. Forbudte navne: `path`, `sourcePath`, `targetPath`, `relativePath`, `folderPath`, `targetRelativePath`, `FilePath`, `DirectoryPath` (uden Relative/Absolute prefix).

## Resolved since last update

| Item | Status | Evidence |
|------|--------|----------|
| C-V01: BuildPlan refactor | ✅ **DONE** | Splittet i `BackupPlanBuilder` + `CreateDefault()` / `ApplyConfig()` / `ApplyCliOverrides()`. `BuildPlan` ~10 linjer. |
| C-V02: ParseXxx DRY | ✅ **DONE** | 8 parsere → `ParseEnum<T>()`. `ParseHashAlgorithm` beholdt som specifik. |
| C-V03: Fail-fast config enums | ✅ **DONE** | Ukendte config-værdier kaster `ArgumentException` i stedet for silent fallback. |
| C-V04: Dead null checks | ✅ **DONE** | `if (config.Source != null)` etc. fjernet — altid sande. |
| C-V05: IsNullOrWhiteSpace guards | ✅ **DONE** | Fjernet på enum-strenge. ParseEnum kaster på tomme/ugyldige værdier. |
| C-V06: Path.GetFullPath double | ✅ **DONE** | Fjernet første kald i CLI block. Global normalization fanger den. |
| C-V16: CLI --delay override | ✅ **DONE** | `WasSupplied(DelayOption)` tilføjet i `ApplyCliOverrides()`. |
| C-V07: Navn-fallback død kode | ✅ **DONE** | `backupOptions.Name` branch fjernet — kunne aldrig nås. |
| C-V08: ExecutionConfig tom klasse | ✅ **DONE** | Klasse + property slettet. Test `Load_TomlFile_EmptyExecution_DefaultsToNull` fjernet (16→15 tests). |
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
| Ctrl+C Del 2: SignalInterruptEngine (subscription engine) | ✅ **DONE** | 6 filer: `SignalInterruptContext`, `SignalInterruptKind`, `SignalInterruptEngine`, `SignalInterruptRegistrationBuilder`, `SignalInterrupt` (static entry), `WindowsCtrlType`. Gammel event-kode slettet. |
| Ctrl+C Del 3: Shadow + handler-baseret registrering | ✅ **DONE** | Shadow af `cancellationToken = cancellationTokenSource.Token`. `SignalInterrupt.On(All).Handler(ctx => { cts.Cancel(); }).Create()` i stedet for `Bind(cts)`. `using ISignalSubscription` sikrer cleanup. TODO om at finally gemmer state. |
| Ctrl+C Del 4: SignalInterrupts tests | ✅ **DONE** | 37 tests: `SignalInterruptKindTests` (5), `SignalInterruptContextTests` (10), `SignalInterruptRegistrationBuilderTests` (12), `SignalInterruptsEntryPointTests` (10). |
| `ISignalSubscription` interface | ✅ **DONE** | `ISignalSubscription : IDisposable` med `Signals` + `Handler`. `Create()`/`Register()` returnerer `ISignalSubscription`. |
| BackupEngine cancellation pattern tests | ✅ **DONE** | 5 tests i `Engine/BackupEngineCancellationTests.cs` — linked token source pattern. |
| SessionState SaveAsync bruger `default(CancellationToken)` | ✅ **DONE** | Finally-blokken kalder `SaveAsync` med `default(CancellationToken)` så state altid gemmes — også ved cancel. |
| Fjernet redundante `ThrowIfCancellationRequested()` | ✅ **DONE** | Både i scan-foreach og processing-foreach — async kaldene har selv token. |
| `using static` fjernet fra BackupEngine.cs | ✅ **DONE** | Ubrugt import ryddet. |
| Doc comments: SignalInterrupt.cs + SignalInterruptEngine.cs | ✅ **DONE** | XML kommentarer opdateret fra `IDisposable` til `ISignalSubscription`. |
| FileSystemTraversal: yield break ved manglende source → fail-first | ✅ **DONE** | `yield break` → `throw DirectoryNotFoundException`. Linje 17-18. |
| MTP arkitektur: `SourceTraversalItem.RelativePath` + `FileName` | ✅ **DONE** | Begge `required`. `BackupScanner` mapper properties — ingen `Path.*` kald. |
| `BackupItem.Id` fiks | ✅ **DONE** | `Id = sourceItem.Id` i stedet for `relativePath` (unik på tværs af source roots). |
| MTP Del 0: `MtpUriParser` | ✅ **DONE** | `MtpUriParser` + `MtpUriParseResult`. Parse `mtp://Device/Path`. 15 tests. |
| MTP Del 1: `IMtpGatekeeper` → `IMediaDeviceGatekeeper` | ✅ **DONE** | Omdøbt til `IMediaDeviceGatekeeper`. `Func<CancellationToken, Task<T>>`, `AcquireAsync(TimeSpan, ...)` med `TimeoutException`, `ThrowIfDisposed`, `Interlocked` dispose. 15 tests. |
| MTP Del 2: Session redesign (erstattede `MtpDeviceSession`) | ✅ **DONE** | `IMtpDeviceSession`/`MtpDeviceSession` slettet. Erstattet af `ISession`/`IConnectedSource`/`IConnectedMediaDriveSource`. Connect/disconnect håndteres af `SourceConnector` + `BackupEngine`. |
| MTP Del 5: factory approach (erstattet af SourceConnector) | ✅ **DONE** | `ISourceConnector.Connect()` + `ISourceTraversalFactory.Create()` — traversal er ikke disposable, session ejes af engine. "Rolled back"-notatet er forældet. |
| **BackupMediaDriveInfo** | ✅ **DONE** | `IBackupDriveInfo` (base), `IBackupFileSystemDriveInfo`, `IBackupMediaDriveInfo` (specialized). `BackupFileSystemDriveInfo` (fail-first med `IsReady` guard, `long` i stedet for `ulong?`). `BackupMediaDriveInfo` (`MediaDevice` + `MediaDriveInfo`, `Name.TrimStart('\\')` som `DriveName`). Omdøbt fra `FileSystemFileStore`/`MediaDeviceFileStore`. `Id ≠ RootPath`. |
| **MTP test cleanup** | ✅ **DONE** | 16 tests fjernet der kaldte `MediaDevice.GetDevices()` direkte (kræver real MTP device). Kun constructor null-check tests tilbage. |
| **NSubstitute 5.3.0** | ✅ **GENINDSAT** | Kortvarigt fjernet, men genindsat. Bruges til mock af wrapper-interfaces (IMediaDeviceInfo, IMediaDrive, IMediaFile, etc.) — 5.3.0 i test .csproj. |
| **xunit.v3 3.2.2** | ✅ **DONE** | Opgraderet. `Microsoft.NET.Test.Sdk` 18.6.0, `coverlet.collector` 10.0.1. |
| **Discovery: IDriveProvider architecture** | ✅ **DONE** | `IDriveProvider` + `FileSystemDriveProvider` + `MediaDeviceDriveProvider` + `DriveProvider` (composite). Erstattede `IFileSystemSourceDiscovery`/`IMediaDeviceSourceDiscovery`/`ICombinedSourceDiscovery`. Filtrerer `IsReady`, skip ghost devices (`COMException 0x802A0001`). `[SupportedOSPlatform("windows7.0")]` for MTP. |
| **IMediaDeviceSession → ISession** | ✅ **DONE** | `ISession` (simplificeret med `Name`). `IConnectedSource : ISession`. `MediaDeviceSession.cs`, `IMediaDeviceSession.cs`, `MediaDeviceSessionTests.cs` slettet. |
| **IConnectedSourceFactory → ISourceConnector** | ✅ **DONE** | Omdøbt: `IConnectedSourceFactory` → `ISourceConnector`, `ConnectedSourceFactory` → `SourceConnector`, `Create()` → `Connect()`. `ConnectedSourceFactoryCreateRequest.cs`, `FileSystemTraversalFactoryStub.cs` slettet. |
| **IConnectedMediaDeviceSource → IConnectedMediaDriveSource** | ✅ **DONE** | Ny: `IConnectedMediaDriveSource`/`ConnectedMediaDriveSource` med både `IMediaDevice Device` + `IMediaDrive Drive`. Gammel `IConnectedMediaDeviceSource`/`ConnectedMediaDeviceSource` slettet. |
| **SourceConnector matcher IMediaDrive via DriveName** | ✅ **DONE** | `FindMediaDeviceSource()` finder `IMediaDrive` fra `Device.Drives` ved at matche `mediaDriveInfo.DriveName`. Returnerer `ConnectedMediaDriveSource(device, drive)`. |
| **SourceTraversalFactory opdateret** | ✅ **DONE** | `Create(IConnectedSource)` — 1 param. `SourceTraversalFactory` injecter `IMediaDeviceGatekeeper`, pattern-matches på `IConnectedFileSystemSource`/`IConnectedMediaDriveSource`. |
| **MediaDeviceTraversal — NuGet-free body** | ✅ **DONE** | Bruger `IMediaDirectory`/`IMediaFile` udelukkende — ingen `MediaDevices.dll` typer i body. Kører via `IConnectedMediaDriveSource.RootDirectory`. |
| **IMediaFile.OpenRead() + MediaDeviceContent** | ✅ **DONE** | `IMediaFile.OpenRead()` tilføjet, `MediaFile` implementerer det. `MediaDeviceContent` bruger `IMediaFile` i stedet for `MediaFileInfo`. |
| **BackupEngine.Create(connectedSource)** | ✅ **DONE** | `Create(connectedSource)` — 1 arg (ingen `IBackupDriveInfo`). Flow: list drives → match → connect → create traversal. |
| **Build: 0 errors, 249 tests** | ✅ **DONE** | 0 errors, 0 warnings. 249 tests pass. |
| **Drive matching fix (Equals→StartsWith)** | ✅ **DONE** | `BackupEngine.MatchDrive()` bruger `StartsWith` + separator-check. `GetRelativePath()` udregner sub-path. |
| **MediaDeviceTraversal sub-path navigation** | ✅ **DONE** | `NavigateToSubDirectory()` via `IMediaDirectory.Directories` baseret på `SubPath`. |
| **MtpUriParser genindsat** | ✅ **DONE** | Restored — parser krævet af produktion (drive matching + traversal navigation). |
| **BackupScanRequest/SourceTraversalRequest.SubPath** | ✅ **DONE** | `SubPath` property tilføjet. `BackupScanner` mapper den videre. |
| **StopOnError bypass** | ✅ **DONE** | `BuildPlan()` sætter `StopOnError = true` så Tier 3 gate ikke slår til |
| **MatchDrive separator fix** | ✅ **DONE** | Accepterer `C:\` (root paths ending with `\`). `Guard.RequireNonNull(drive)` + `sourcePath.Replace("/", "\\")` |
| **ConsolesPrinter markup crash** | ✅ **DONE** | Alle interpolerede values `.EscapeMarkup()`. `[bold]` scope fikset |
| **Progress logger silenced** | ✅ **DONE** | `logger.LogInformation` i progress handler kommenteret ud |
| **Directory.Build.props restored** | ✅ **DONE** | Genoprettet fra git efter accidental truncation |
| **list-sources layout fikset** | ✅ **DONE** | Id column, split tables, column order/gaps |
| **Backup4 end-to-end test** | ✅ **DONE** | 7 files discovered, 7 copied, 0 errors |
| **Spectre Console deep-dive** | ✅ **DONE** | Complete map of all Spectre usage (10 locations). task-fil: 09-SpectreConsole.md |
| **Devices wrapper-lag (12 files)** | ✅ **DONE** | `IMediaDeviceInfo`/`MediaDeviceInfo`, `IMediaDevice`/`MediaDeviceWrapper`, `IMediaDrive`/`MediaDrive`, `IMediaDirectory`/`MediaDirectory`, `IMediaFile`/`MediaFile`, `IMediaItem`, `MediaFileAttribute` — komplet abstraktion over MediaDevices.dll |
| **SourceConnector bruger wrappers** | ✅ **DONE** | Connecter via `MediaDeviceInfo.GetDevices()` → `IMediaDeviceInfo.Connect()` → `IMediaDevice.Drives` → `ConnectedMediaDriveSource(IMediaDevice, IMediaDrive)` |
| **ConnectedMediaDriveSource bruger wrappers** | ✅ **DONE** | `ConnectedMediaDriveSource(IMediaDevice, IMediaDrive)` i stedet for concrete `MediaDevice`/`MediaDriveInfo` |
| **MediaDeviceTraversal bruger wrappers** | ✅ **DONE** | `IMediaDevice`, `IMediaDrive`, `IMediaDirectory.Directories`/`Files` — ingen `MediaDevices.dll` typer i body |
| **MediaDeviceContent bruger IMediaFile** | ✅ **DONE** | `IMediaFile.OpenRead()` i stedet for concrete `MediaFileInfo` |

---

## Remaining Issues

### ✅ Consolidation Phase 1 — Duplication cleanup

| # | Task | Status | Evidence |
|---|------|--------|----------|
| 1 | Extract `WasSupplied<T>()` → shared `OptionHelpers` | ✅ **DONE** | `Consoles/ConsoleCommands/OptionHelpers.cs`. `BackupConsoleCommand2.Helpers.cs` + `BackupConsoleCommand4.Helpers.cs` refactored to use it. |
| 2 | Delete dead `EngineArgumentBuilder()` | ✅ **DONE** | Removed from `BackupConsoleCommand2.cs` (lines 276-357, duplicate of `BuildPlan()`). |
| 3 | Delete empty `FakeFileTransfer.cs` | ✅ **DONE** | File deleted — empty class without interface, unused. |
| 4 | Consolidate `FakeGatekeeper` | ✅ **DONE** | Moved to `Core4.Tests/Fakes/FakeGatekeeper.cs` with shared `TrackingDisposable`. Both test files updated. |

### ✅ Devices wrapper-integration i discovery (småopgave — sidste 2 filer)

**Status:** Pipeline (SourceConnector → ConnectedMediaDriveSource → MediaDeviceTraversal → MediaDeviceContent) er fuldt integreret. Kun discovery-laget mangler:

| # | File | Status | Konklusion |
|---|------|--------|------------|
| 1 | `DriveDiscovery/MediaDeviceDriveProvider.cs` | ✅ **Allerede korrekt** | Bruger `MediaDeviceInfo.GetDevices()` + `IMediaDrive` + `BackupMediaDriveInfo(IMediaDevice, IMediaDrive)`. `IMediaDevice` er den abstraherede connected interface — kræves for `Model`/`SerialNumber` som ikke findes på `IMediaDeviceInfo`. |
| 2 | `Storage/BackupMediaDriveInfo.cs` | ✅ **Allerede korrekt** | `FromDeviceAndDrive(IMediaDevice, IMediaDrive)` er korrekt. `IMediaDevice` har alt (Drives + Model + SerialNumber). Forslaget `(IMediaDeviceInfo, IMediaDrive)` ville være forkert. |

### ✅ Komplet gap-analyse (Core, Core2, Core3 → Core4)

- [x] **Gennemgang: Find alle manglende dele** — Krydsrefereret alle features. Resultat: Feature audit nedenfor (§ Feature Audit) + nye huller tilføjet i dette dokument.

### Deferred — BackupIndexType.Json (laveste prioritet)

Overflødig — `BackupJsonSummaryStore` + sidecars dækker samme behov.
**Validator-gate beholdes** indtil vi beslutter at implementere eller fjerne helt.

| # | Task | Detail |
|---|------|--------|
| 1 | `IBackupIndexWriter` interface | `WriteAsync(Stream, IReadOnlyList<BackupItem>, BackupPlan, BackupResult, CancellationToken)` |
| 2 | `JsonBackupIndexWriter` | Skriver `backup_catalog.json` |
| 3 | Wire i `BackupEngine` | Efter processing loop, før result returneres |
| 4 | DI registration | `AddScoped<IBackupIndexWriter, JsonBackupIndexWriter>()` |
| 5 | ~~Fjern Tier 3 gate~~ | **Beholdes.** `if(plan.BackupIndexType == BackupIndexType.Json)` i `BackupPlanValidator` |
| 6 | JSON schema | Definer felter, struktur, eksempel |

### Høj prioritet — Public DriveCatalog API

**Implementeret.** `IDriveCatalogService` + `DriveCatalogEntry` + `DriveCatalogService` + DI registration. 0 errors, 249 tests.

### Høj prioritet — Consoles CLI cleanup (før release)

| # | Issue | Detail |
|---|-------|--------|
| 1 | **Tilføj `Delay` til `BackupPlan`** | ✅ **DONE** | `BackupPlan.Delay` (int), `IThrottler` / `ThrottlerFactory` / `DelayThrottler`, implementeret i `BackupEngine` loop + threadet gennem hash pipeline og collision resolution, `--delay` CLI option i `BackupOptionsModel4`. |
| 2 | **Delay/VerificationRetry/Timeout** — guarded men ikke implementeret | `Delay` ✅ **DONE** (se #1 — throttler). `VerificationRetryCount`, `VerificationRetryDelayMs`, `VerificationTimeoutMs`, `VerificationDeleteOnFailure` — **❌ WONTFIX — implementeres ikke.** Validering bør fjernes eller options ignoreres med warning. |
| 3 | **MTP source path format** | ❌ **WONTFIX** — Core4 bruger `--source-path "mtp://Device/Path"`. `--source-device` hører til Core2 (archived). |
| 4 | **`--backup-index` default** | ✅ **DONE** | `IBackupIndexWriter`/`JsonBackupIndexWriter` implementeret, T3 gate fjernet, catalog gemmes som `{dest}\.bmpt\{sessionId}\backup_catalog.json`. Default er `None` (som ønsket). |
| 5 | **Core2 options i ApplicationServiceSetup** | ✅ **DONE** | Udkommenteret — dødt fra Core4's side, Core2 kan stadig bruge det. |
| 6 | **SignalInterrupt cancel-wiring** | ✅ **DONE** | `Console.CancelKeyPress` fjernet fra `BackupConsoleCommand4`. Engine styrer selv cancellation via `SignalInterrupt` internt. |
| 7 | **ConsolesPrinter progress** | Flyttet til Task 09 (Spectre Console). Skal bruge `AnsiConsole.Progress()` widget |
| 8 | **No tests for backup4** | Tilføj tests for `BackupConsoleCommand4Helpers.BuildPlan` enum-mapping |


### ✅ Høj prioritet — TOML config reader — DONE

| # | Task | Status |
|---|------|--------|
| 1 | `--config` fil support | ✅ **DONE** | `BackupPlan4Config.cs` (nested model: Source/Destination/Collision/Metadata/Behavior/Execution), `BackupPlan4Loader.cs` (TOML via PascalToKebab, JSON via PropertyNameCaseInsensitive) |
| 2 | Map TOML til `BackupPlan` | ✅ **DONE** | Overfører alle felter inkl. `MaxDegreeOfParallelism`, `EnableMetadata`, `EnableTimestampCorrection`, `ResumeBehavior`; string-to-enum parsers for alle værdier |
| 3 | CLI integration | ✅ **DONE** | `BuildPlan()` loader config først som base, derefter `WasSupplied()` overrider eksplicitte CLI-args — CLI vinder altid |

### Høj prioritet — Retry / Resilience (især MTP)

| # | Task | Detail |
|---|------|--------|
| 1 | Retry strategy | Exponential backoff for transient I/O failures (download, hash, move, sidecar write) |
| 2 | MTP resilience | Gatekeeper timeout + retry ved COMException/disconnect mid-session |
| 3 | Overvej | Genbrug Core2's Polly `BackupResiliencePipeline` eller implementer lightweight retry |

### ✅ Spectre Console progress redesign — DONE

> Se `tasks/09-SpectreConsole.md` for fuld arkitekturdesign + reusable assets catalog.

| # | Issue | Status |
|---|-------|--------|
| 1 | `ConsolesPrinter.PrintProgress` — Core2/Core3 overloads med `WriteLine` | 🔒 Wontfix — Core2/Core3 er archived/readonly |
| 2 | `SpectreAnsiConsoleLogger` — unsafe `MarkupLineInterpolated` | ✅ EscapeMarkup() fixed |
| 3 | `ProgramSpectreExample` — Core2 eksempel | 🔒 Wontfix — archived |
| 4 | Progress for 3 engines | 🔒 Wontfix — Core2/Core3 er archived/readonly. Kun Core4 understøttes |
| 5 | Version mismatch (0.55.2 vs 0.54.0) | ✅ Acceptable |
| 6 | `ConsolesServiceSetup` NOT wired | ✅ Wired i `ApplicationStartup.cs` |
| 7 | `PreserveHierarchy` default | ✅ |
| 8 | Custom columns/spinners fra Core | ✅ `ValueOfMaxColumn`, `ElapsedTimeAdvancedColumn`, `SequenceSpinner` kopieret til `Consoles/Progress/Columns/` + `Spinners/` og wired i `BackupProgressDisplay` |

### Medium prioritet — Public API overvejelser

| # | Task | Status |
|---|------|--------|
| 1 | `ISidecarService` public? | ✅ **WONTFIX** — Forbliver `internal`. Core4 bruges internt via `BackupEngine.RunAsync()`. Ingen eksterne forbrugere har brug for direkte sidecar-generering. Sidecar-funktionalitet eksponeres via `BackupPlan.SidecarFormat` og kører automatisk i engine. |

### Høj prioritet — Integration test

| # | Task |
|---|------|
| 1 | Full MTP traversal pipeline (gatekeeper → connector → traversal → content) |
| 2 | BackupEngine end-to-end (filesystem → download → hash → sidecar → verify) |

### Senere

| # | Task | Feature gate |
|---|------|-------------|
| 1 | **`StopOnError=false`** — continue-on-error (per-item catch + Failed status findes, men `throw` på linje 485 forhindrer det) | Linje 81 (Tier 3) |
| 2 | **`BackupIndexType.Database`** — SQLite catalog | Linje 84-85 (Tier 4) |
| 3 | **`EnableMetadata`** — metadata extraction | Linje 77-78 (Tier 3) |
| 4 | **`MaxDegreeOfParallelism`** — parallel execution | Linje 88-89 (Tier 4) |
| 5 | Hash algorithm CLI options | Expose comparison/verification hash valg |
| 6 | Erstat Core2 `backup` med Core4 som default | Når Core4 er feature-complete |


### Low / Deferred

- ~~**N1: Cross-run dedup** — skipped by design. Hash fra backup records (session state), ikke sidecar.~~
- ~~**Progress reporting** — `BytesProcessed` akkumuleres nu live i Completed-fasen.~~

---

## Edge Case Audit (31 May 2026)

Gennemgang af CancellationToken-flow, error recovery, temp cleanup og I/O edge cases.

### CancellationToken Issues

| # | Issue | File | Severity | Status |
|:--|-------|------|----------|--------|
| E1 | `CompositeTimestampReader` ignores CancellationToken | ~~`CompositeTimestampReader.cs:44-48` → `:73`~~ | **High** | ✅ **FIXED** — `CancellationToken` passes nu. Nyt interface-design: `ICompositeTimestampReader` + `TryReadCollect`. |
| E2 | No `ThrowIfCancellationRequested()` at processing loop top | ~~`Engine/BackupEngine.cs:219`~~ | Low | ✅ **FIXED & REMOVED** — tilføjet, senere fjernet som redundant (async kald har selv token). |

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
| 2 | `IBackupScanner` / `ScannerGatherer` | ✅ | Core4 har `IBackupScanner` / `BackupScanner` — real implementation (ingen stub). `ScannerGathererStub` slettet. |
| 3 | `IFileComparer` + 8 chunked compare algoritmer + `Md5Comparer` | ✅ | Core4 har `IFileCompareService` + `BinaryFileComparerSelector` + 5 algoritmer |
| 4 | `ISideCarDocumentBuilder` / `ISideCarMetaDataBuilder` | ➡️ | Core4 har `ISidecarService` / `SidecarService` — Document/Section/Property model |
| 5 | `BackupTimeStamp` / `BackupTimeStampForDevice` / `BackupTimeStampForDrive` | ➡️ | Core4 har `IEarliestTimestampResolutionService` — langt mere avanceret |
| 6 | `IHashCode` / `IHashCodeStringBuilder` / `HashCalculator` / `HashCode` | ✅ | Core4 har `IHashGenerator` / `IHashService` / `StreamHashGenerator` |
| 7 | `IMessageFormatter` + `StringVariableSubstitution.Template` | ✅ | Fælles i `BMTP3.Common.MessageFormatterParser` |
| 8 | `IBackupPathExtension` | ✅ | Core4 har `ITargetPathResolver` |
| 9 | `IEventLogger` | ➡️ | Core4 bruger `ILogger<T>` fra MS.Extensions |
| 10 | `IGetLatestItem` / `IGetLatestItemFromIndex` | ➡️ | Core4 har `ISessionStateService` / `IBackupRecordRepository` |
| 11 | `BackupMaster` / `BackupHelper` / `ConfigurationHandler` | ❌ | Orchestrator/helper — nogle dele mangler i Core4 |
| 12 | `MediaDeviceServiceProd` / `IMediaDeviceService` | ➡️ | Core4 har `IMediaDeviceGatekeeper` + `MediaDeviceTraversal` i stedet. MTP understøttet. |
| 13 | `NExifTool` / `MetadataExtractorFileInfo` / `AbstractMetadataFileInfo` | ➡️ | Core4 bruger MetadataExtractor i stedet for ExifTool |
| 14 | `VerifyBackupHandler` | ⚠️ | Core4 har inline hash verification i `BackupEngine` (`plan.PostWriteVerification == Hash`). Ingen separat handler/interface. |
| 15 | `BackupRecordDataStore` / `BackupRecordDataStorePathResolver` | ✅ | Core4 har `BackupJsonSummaryStore` / `SessionStateService` |
| 16 | TOML config (`BackupSettingsImpl`, `BackupSettingsReader`, `ConfigModel`) | ✅ | Core4 har `BackupPlan4Config.cs` + `BackupPlan4Loader.cs` (TOML via Tomlyn + PascalToKebab, JSON/JSON5 via JsonSerializer) |
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
| 19 | `MTPGatekeeperService` / `MTPMtpDeviceSession` / `MTPMtpDeviceSessionFactory` / `MtpDeviceUtils` | ➡️ | Core4 har `IMediaDeviceGatekeeper`/`MediaDeviceGatekeeper` + `ISourceConnector`/`SourceConnector`. MTP pipeline fuldt implementeret. |
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
| 4 | `IBackupScanner` / `FileSystemScanner` | ✅ | Core4 har `IBackupScanner` / `BackupScanner` — real implementation. `ScannerGathererStub` slettet. |
| 5 | `IFileTransfer` / `SimpleFileTransfer` (buffered copy + collision) | ➡️ | Core4 har `IDownloadService` + `IMoveableContent.MoveTo()` |
| 6 | `IHashGenerator` / `IItemHasher` / `StreamHashGenerator` + `Blake3Digest` | ✅ | Core4 har samme mønster i `Engine/Hashing/` |
| 7 | `IMetadataReader` / `FileMetadataReader` (basic FileInfo metadata) | ➡️ | Core4 har `EarliestTimestampResolutionService` — langt mere |
| 8 | `ISidecarGenerator` / `SimpleSidecarGenerator` (INI .sidecar) | ✅ | Core4 har `SidecarService` — INI + JSON + Document model |
| 9 | `BackupItem` (immutable record with With* helpers) | ➡️ | Core4 har `BackupItem` (mutable class) — forskelligt mønster |
| 10 | `BackupJobResult` / `BackupError` | ✅ | Core4 har `BackupResult` / `BackupResultItem` |
| 11 | `BackupPlan` / `CollisionStrategy` / `HashType` | ✅ | Core4 har alle tre, mere udvidede |
| 12 | `ServiceCollectionExtensions.AddBMTP3Core3()` | ✅ | Core4 har `AddBMTP3Core4()` |

### Sammenfatning — væsentlige huller i Core4

| Område | Status | Hvad mangler |
|---|---|---|
| **Scanner** | ✅ | `BackupScanner` implementeret. `ScannerGathererStub` slettet. Scanner-gap er lukket. |
| **Verify** | ⚠️ | Hash verification implementeret inline i `BackupEngine`. Ingen separat `IPostWriteVerification`. |
| **BackupIndexType.Json** | ❌ | `IBackupIndexWriter` + `JsonBackupIndexWriter` mangler. Feature gate (Tier 3). |
| **BackupIndexType.Database** | ❌ | SQLite catalog. Feature gate (Tier 4). |
| **EnableMetadata** | ❌ | Metadata extraction. Feature gate (Tier 3). |
| **MaxDegreeOfParallelism** | ❌ | Parallel execution. Feature gate (Tier 4). |
| **StopOnError=false** | ❌ | Continue-on-error. Feature gate (Tier 3). Engine re-thrower altid (linje 485). |
| **TOML config** | ✅ | `BackupPlan4Config.cs` + `BackupPlan4Loader.cs` (TOML, JSON, JSON5) |
| **Resilience** | ❌ | Ingen retry/circuit-breaker (Core2 har Polly pipeline) |
| **Console UI progress** | ⚠️ | Spectre deep-dive done (Task 09). `ConsolesPrinter` bruger stadig `WriteLine` — skal refactores til `AnsiConsole.Progress()` |
| **ConsolesServiceSetup NOT wired** | ❌ | `ApplicationStartup.cs` mangler `ConsolesServiceSetup` → `ConsolesPrinter` er null → alt progress er no-ops. **Blokerer Task 09.** |
| **SpectreAnsiConsoleLogger markup safety** | ❌ | `MarkupLineInterpolated` parser markup — crash på `[`/`]` i log messages |
| **Custom ProgressColumns/Spinners** | ❌ | 6 custom columns + 2 custom spinners i Core. Beslut: port eller skip? |
| **ISidecarService public** | ✅ **WONTFIX** | Forbliver `internal` — ingen eksterne forbrugere, funktionalitet eksponeres via `BackupPlan.SidecarFormat` i engine |
| **MTP Discovery + Traversal** | ✅ **DONE** | Hele pipeline: `SourceConnector` (via `IMediaDeviceInfo`/`IMediaDrive`), `ConnectedMediaDriveSource` (`IMediaDevice`+`IMediaDrive`), `MediaDeviceTraversal` (`IMediaDirectory`/`IMediaFile`), `MediaDeviceContent` (`IMediaFile`). Kun `MediaDeviceDriveProvider` + `BackupMediaDriveInfo` mangler wrapper-opdatering. |
| **Consoles CLI cleanup** | ⚠️ | Ubrugte options valideres, MTP path format, Core2 options config |
| **INI/JSON sidecar** | ✅ **DONE** | Full Document/Section/Property model + Ini + Json writers |
| **DryRun** | ✅ **DONE** | `BuildDryRunResult` helper, short-circuit |
| **Parallel runner** | ⚠️ | `BackupRunner` beholdt. `ParallelBackupRunner`/`LimitedParallelBackupRunner` slettet. |
| **Include/Exclude patterns** | ✅ **DONE** | `GlobMatcher.IsIncluded` i `FileSystemTraversal`. |
| **Dedup/FileCategory** | ❌ | Ingen dedup på tværs af sessioner |
| **State machines** | ➡️ | Bevidst arkitekturvalg — inline status i record i stedet for state machine |
| **Pipeline stages** | ➡️ | Bevidst arkitekturvalg — strategi-baseret loop i stedet for step-klasser |
| **Handler hierarchy** | ➡️ | Bevidst arkitekturvalg — samlet i `BackupEngine` |

---

## Code Quality Audit (11 Jun 2026)

Fuld gennemgang af `BMTP3.Consoles` og `BMTP3.Core4` mod SOLID, Clean Architecture, Fail-Fast, DRY, KISS, YAGNI.

---

### Consoles — alle fund

| ID | Fil | Linje | Princip | Sværhed | Beskrivelse |
|----|-----|-------|---------|---------|-------------|
| ~~C-V01~~ | `BackupConsoleCommand4.Helpers.cs` | 11–237 | SOLID-SRP / KISS | **High** | ✅ **FIXED** — `BuildPlan` splittet i `BackupPlanBuilder.CreateDefault()` + `ApplyConfig()` + `ApplyCliOverrides()`. `BuildPlan` nu ~10 linjer. |
| ~~C-V02~~ | `BackupConsoleCommand4.Helpers.cs` | 243–357 | DRY | **High** | ✅ **FIXED** — 8 parsere erstattet af én generisk `ParseEnum<T>()`. `ParseHashAlgorithm` beholdt som specifik (multi-alias). |
| ~~C-V03~~ | `BackupConsoleCommand4.Helpers.cs` | 243–357 | Fail-Fast | **High** | ✅ **FIXED** — `_ =>` silent fallback erstattet af `throw new ArgumentException(...)`. |
| ~~C-V04~~ | `BackupConsoleCommand4.Helpers.cs` | 41–128 | YAGNI | Medium | ✅ **FIXED** — `if (config.Source != null)` etc. fjernet. Alle sektioner initialiseres med `= new()`. |
| ~~C-V05~~ | `BackupConsoleCommand4.Helpers.cs` | 82–93 | YAGNI / Fail-Fast | Medium | ✅ **FIXED** — `IsNullOrWhiteSpace`-guards på enum-strenge fjernet. ParseEnum kaster på tomme/ugyldige værdier. |
| ~~C-V06~~ | `BackupConsoleCommand4.Helpers.cs` | 133–197 | DRY | Medium | ✅ **FIXED** — `Path.GetFullPath` fjernet fra CLI block (linje 152). Global normalization (linje 204) fanger den. |
| ~~C-V07~~ | `BackupConsoleCommand4.Helpers.cs` | 200–208 | KISS / DRY | Low | ✅ **FIXED** — Død `backupOptions.Name` branch fjernet fra fallback. |
| ~~C-V08~~ | `BackupConsoleCommand4.Helpers.cs` | 127 | YAGNI | Low | ✅ **FIXED** — `ExecutionConfig` klasse + `Execution` property slettet. Tom og uden formål. |
| C-V09 | `BackupConsoleCommand4.cs` | 61–66 | Fail-Fast / YAGNI | **High** | `GetService<IBackupEngine>()` (returner null) i stedet for `GetRequiredService<>()` (kaster). `IBackupEngine` er obligatorisk. |
| C-V10 | `BackupConsoleCommand4.cs` | 70–91 | Fail-Fast / KISS | Medium | `BackupResult? result = null` + `ThrowIfNull(result)` er unødigt komplekst. `RunAsync` returnerer aldrig null. |
| C-V11 | `BackupConsoleCommand4.cs` | 94–98 | SOLID-SRP / DRY | Medium | Tæller `ItemResults` per state for logging — duplikerer samme tælling i `ConsolesPrinter.PrintResult`. |
| C-V12 | `BackupConsoleCommand4.cs` | 49–52 | KISS / DRY | Medium | Tre-trins logger-resolution kopieret i `BackupConsoleCommand4ListDrives.cs`. Bør erstattes af `GetRequiredService<ILogger<T>>()`. |
| C-V13 | `BackupConsoleCommand4.cs` | 68 | Clean Architecture / SOLID-D | Medium | `BackupProgressDisplay` instantieres med `AnsiConsole.Console` (static) i stedet for `IAnsiConsole` fra DI. |
| C-V14 | `BackupConsoleCommand4.cs` | 121–124 | YAGNI | Low | `ExecuteAsyncForTests` er en public production-metode der kun eksisterer som test-seam. Lækker testinfrastruktur i production API. |
| C-V15 | `BackupConsoleCommand4.cs` | 17–34 | KISS | Low | Public default constructor delegerer til privat 2-parameter constructor udelukkende for at gemme referencer som base class allerede holder. |
| ~~C-V16~~ | `BackupOptionsModel4.cs` | 155–162 | YAGNI | Medium | ✅ **FIXED** — `WasSupplied(DelayOption)` tilføjet i `ApplyCliOverrides()`. |
| C-V17 | `BackupOptionsModel4.cs` + `BackupConsoleCommand4.Helpers.cs` | 183 / 33 | Fail-Fast | Medium | ~~`PostWriteVerificationOption` default `Hash`, men `BuildPlan` initialiserer til `None`. `WasSupplied` returnerer false for implicit default → plan får `None` selvom option er `Hash`. Silent mismatch.~~ ✅ **FIXED** — Begge defaults ændret til `None` så de matcher. Brugeren vælger eksplicit `--verify hash`. |
| C-V18 | `BackupOptionsModel4.cs` | 20 | YAGNI | Low | `ConfigOptionResult` property deklareres men læses aldrig i produktion. Overflødigt. |
| C-V19 | `BackupOptionsModel4.cs` | 192–194 | SOLID-SRP | Low | `DoAddValidators()` er tom hook mens al validering sker i `BackupConsoleCommand4.ValidateBackupOptions()`. Inkonsistent mønster. |
| C-V20 | `BackupPlan4Config.cs` | 57–59 | YAGNI | Low | `ExecutionConfig` er en tom sealed class uden properties. Deserialiseres og instantieres uden formål. |
| C-V21 | `BackupPlan4Config.cs` | 1–59 | Clean Architecture / KISS | Medium | Alle enum-lignende værdier er `string`-typer (`"rename"`, `"binary"` etc.) i stedet for de enums der allerede findes i Core4. Tvinger 9 `ParseXxx`-metoder til at eksistere. |
| C-V22 | `BackupPlan4Loader.cs` | 9–50 | DRY | Low | Strukturelt identisk med Core2's `BackupPlanLoader.Load` — extension/branch/deserialize/null-check/throw. Bør deles. |
| C-V23 | `ConsolesPrinter.cs` | 44–54 | SOLID-SRP / Clean Architecture | Medium | Én klasse håndterer output for Core2, Core3 og Core4 — tre uafhængige grunde til at ændre klassen. Core4-printer bør separeres. |
| C-V24 | `ConsolesPrinter.cs` + `BackupConsoleCommand4.cs` | 87–90 / 95–98 | DRY | Medium | `result.ItemResults.Count(r => r.State == X)` udføres uafhængigt to steder. Bør ligge ét sted. |
| C-V25 | `BackupProgressDisplay.cs` | 100–112 | YAGNI | **High** | ~~`MarkRemainingCompletedTasksAsInactive` er defineret men aldrig kaldt. Same logik inlineat i `UpdateFileTasks`. Dead method.~~ ✅ **FIXED** — slettet. |
| C-V26 | `BackupProgressDisplay.cs` | 65–69 | Clean Architecture | Medium | `Console.WriteLine` (System.Console) bruges direkte til debug i stedet for injiceret `IAnsiConsole`. Untestbar og inkonsistent. |
| C-V27 | `BackupProgressDisplay.cs` | 50–55 | KISS | Low | Triple-variabel polling (`latestReport`/`latestReportVersion`/`processedReportVersion`) er mere kompleks end nødvendigt ved 100ms poll-interval. |
| C-V28 | `BackupConsoleCommand4ListDrives.cs` | 26–29 | Fail-Fast | Medium | `ServiceProvider` tilgås uden null-guard selv om det er nullable. `BackupConsoleCommand4` har guard; søster-klassen mangler den. |
| C-V29 | `BackupConsoleCommand4ListDrives.cs` | 62 | KISS / YAGNI | Low | `return await Task.FromResult(0)` i async metode. `return 0` er identisk og allokerer intet. |
| C-V30 | `BaseOptionsModel.cs` | 25–35 | SOLID-SRP / KISS | Medium | `DoAddValidators()` kaldes som side-effect inde i `ConcurrentDictionary.GetOrAdd` factory — kan køre flere gange ved concurrency. TODO-kommentar om thread safety er stadig tilstede. |
| C-V31 | `BaseOptionsModel.cs` | 153–159 | DRY / KISS | Medium | `DoPopulate` kalder `DoDefineOptions()` direkte i stedet for `GetOrCreateOptionBinders()` — bypasser cachen og re-kører reflection ved hvert kald. |

---

### Core4 — alle fund

| ID | Fil | Linje | Princip | Sværhed | Beskrivelse |
|----|-----|-------|---------|---------|-------------|
| K-V01 | `Engine/Runner/BackupRunner.cs` | hele filen | YAGNI / SOLID-SRP | **High** | ~~Kommenteret dead code — "no longer called by BackupEngine." Indeholder sin egen sidecar/collision/move logik. `IBackupRunner` interface er også dead. Slet begge.~~ ✅ **FIXED** — hele `Engine/Runner/` mappen slettet (4 filer + interface). |
| K-V02 | `Engine/Runner/BackupRunnerProgress.cs` + `BackupRunnerRequest.cs` | hele filer | YAGNI | **High** | ~~DTO'er der kun eksisterer for `BackupRunner` (dead). Slet begge med runner.~~ ✅ **FIXED** — slettet med Runner. |
| K-V03 | `Engine/Session/OldState/BackupSessionState.cs` | hele filen | YAGNI | **High** | ~~`OldState/`-mappe. Ingen referencer i codebase. Duplicerer guard-logik fra `BackupMemoryRecordRepository`. Slet.~~ ✅ **FIXED** — fil + `OldState/` mappe slettet. |
| K-V04 | `Scanner/BackupScannerStub.cs` | hele filen | YAGNI | Medium | ~~Kaster `NotImplementedException` på alt. Fuld `BackupScanner` eksisterer. Aldrig registreret i DI. Slet.~~ ✅ **FIXED** — slettet. |
| K-V05 | `Helpers/BackupDelay.cs` | hele filen | YAGNI | Medium | ~~Dead struct — ingen kaldere. `IThrottler`/`ThrottlerFactory` bruges overalt. Slet.~~ ✅ **FIXED** — slettet. |
| K-V06 | `Engine/Helpers/TimestampHelpers.cs` | hele filen | YAGNI | Medium | ~~`FindEarliestValidDate` har nul kaldere. `EarliestTimestampResolutionService` håndterer alt. Slet.~~ ✅ **FIXED** — fil + `Engine/Helpers/` mappe slettet. |
| K-V07 | `Hashing/HashCalculator.cs` | hele filen | YAGNI / DRY | **High** | ~~Aldrig kaldt. Duplikerer algorithm-switch fra `StreamHashGenerator`. Slet.~~ ✅ **FIXED** — slettet. |
| K-V08 | `Hashing/NoopHashGenerator.cs` | 8–18 | Fail-Fast / YAGNI | Medium | Returnerer tom dictionary uden exception. Aldrig registreret i DI, men hvis det sker kører engine videre med tomme hash-resultater — silent data-integrity bug. |
| K-V09 | `Engine/Strategies/RenameCollisionResolver.cs` + `TargetPathResolver.cs` | 163–195 / 83–118 | DRY | **High** | `NormalizeCustomRelativePath` er tegn-for-tegn identisk i begge klasser. Bør udtrækkes til fælles `PathNormalizer`. |
| K-V10 | `Engine/Hashing/HashService.cs` + `Engine/Strategies/RenameCollisionResolver.cs` | 43–55 / 308–320 | DRY | **High** | `ToHashType(HashAlgorithmType → HashType)` switch med 9 identiske cases i to klasser. Bør ligge ét sted. |
| K-V11 | `Engine/BackupEngine.cs` | 565–580 / 623–639 / 700–713 | DRY | Medium | `new BackupResultItem { Id=…, State=… }` konstrueres tre steder. `BuildItemResults` blev udtrukket men bruges kun i cancellation-path. Normalt completion-path har sit eget verbatim copy. |
| K-V12 | `Engine/BackupEngine.cs` | 138–139 | SOLID-D / Clean Architecture | **High** | WONTFIX — `SessionStateService` kræver `metadataPath` (afhænger af `plan.Destination`, runtime-værdi). DI-scoped service ville kræve `Initialize()` metode eller factory — begge er værre end `new`. Korrekt som den er. |
| K-V13 | `Engine/BackupEngine.cs` | 124 | SOLID-D | Medium | WONTFIX — `BackupMemoryRecordRepository` er en session-scoped in-memory store. Skal starte tom per `RunAsync()` kald. Hvis injectet som singleton akkumulerer records på tværs af kald. Korrekt som den er. |
| K-V14 | `Engine/BackupEngine.cs` | 113–120 | SOLID-SRP / Clean Architecture | Medium | WONTFIX — Ctrl+C signal-håndtering er kernefunktionalitet som alle forbrugere skal have automatisk. At flytte den til application host ville tvinge alle forbrugere til at huske at sætte det op selv. Kan testes via `CancellationToken` direkte. Korrekt som den er. |
| K-V15 | `Engine/BackupEngine.cs` | 261, 342, 468 | KISS / DRY | Low | Samme null-check + kommentar ("Stupid Visual Studio...") copy-pastat tre gange. Udtruk til `AssertRelativeFilePathNotNull(record)`. |
| K-V16 | `Engine/Validation/BackupPlanValidator.cs` | 61–62 | Fail-Fast (forkert regel) | Medium | `VerificationHashAlgorithmTypes` kræves altid — men feltet er valgfrit når `PostWriteVerification == None`. Validator bør kun tjekke dette når verification er aktiveret. |
| K-V17 | `Engine/BackupEngine.cs` | 608–612 | Fail-Fast / Clean Architecture | Medium | Ved cancellation sættes progress-phase til `Completed`. Burde være `Cancelled` (eller uændret). `BackupResultState.Cancelled` og `BackupProgressPhase.Completed` modsiger hinanden. |
| K-V18 | `Engine/Strategies/RenameCollisionResolver.cs` | 278–281, 302–304 | Fail-Fast | **High** | ~~Bare `catch { return false }` i `HashCompareAsync` og `SizeAndModifiedTimeCompareAsync`. I/O-fejl → engine behandler filer som unikke → duplikater skrives til disk.~~ ✅ **FIXED** — `OperationCanceledException` re-throwes, alle andre exceptions kaster `InvalidOperationException` med klar besked. |
| K-V19 | `Engine/DiskSpace/DiskSpaceValidator.cs` | 84–93 | Fail-Fast | Medium | `GetFreeSpace` swallower alle exceptions og returnerer `0`. Efterfølgende `< 100MB` check kaster `IOException("Insufficient disk space")` — forvirrer årsag og symptom. |
| K-V20 | `Engine/Compare/BinaryFileComparerSelector.cs` | 37 | KISS | Low | `_chunkedAvx2.GetType().Assembly != null` er altid true i .NET. Dead condition. |
| K-V21 | `Engine/Compare/Algorithms/WholeFileSequenceEqualBinaryComparer.cs` | 7–8 | KISS | Low | `File.ReadAllBytesAsync` loader begge filer (op til 20 MB) i hukommelsen. `ChunkedBinaryFileComparer` er strengt bedre. `WholeFile`-varianten er overflødig. |
| K-V22 | `Engine/TimeStamp/Readers/XmpTimestampReader.cs` | 12–16, 112–138 | DRY | Medium | 5 parser-felter + `TryParseDateWithResolution` er tegn-for-tegn identisk med `BaseDirectoryTimestampReader`. Bør udtrækkes til `TimestampReaderHelper`. |
| K-V23 | `Engine/TimeStamp/EarliestTimestampResolutionService.cs` | 38–41 | SOLID-D / Clean Architecture | Medium | Parameterløs constructor hard-coder `new CompositeTimestampReader()`. DI registrerer servicen som singleton — men reader-kæden kan ikke injiceres/erstattes udefra. |
| K-V24 | `Engine/Sidecar/SidecarService.cs` | 150–154 | SOLID-O / SOLID-D | Low | `ResolveWriter` instantierer `new IniSidecarWriter()` / `new JsonSidecarWriter()` inline. Tilføjelse af nyt format kræver ændring af `SidecarService` (Open-Closed violation). |
| K-V25 | `Engine/BackupEngine.cs` | 443–446 | DRY / Clean Architecture | Low | `BackupSourceType` → sidecar-string (`"MtpDevice"`, `"Drive"`) mappes inline i engine. Bør være extension method på enum. |
| K-V26 | `Engine/Index/JsonBackupIndexWriter.cs` | 81 | Fail-Fast (data bug) | **High** | ~~`SessionId = plan.Name` — bruger planens navn i stedet for den faktiske `sessionId` parameter. Catalog-filen skrives med forkert session-ID. Silent data corruption.~~ ✅ **FIXED** — `SessionId = sessionId`, parameter tilføjet til `BuildCatalog`. |
| K-V27 | `State/BackupJsonSummaryStore.cs` | 38–40, 46 | KISS / Clean Architecture | Low | `File.ReadAllText` (synkron) + `Task.FromResult` i async metode. Bør bruge `await File.ReadAllTextAsync`. |
| K-V28 | `Engine/Session/BackupMemoryRecordRepository.cs` | 20–23 | KISS | Low | `GetAll()` returnerer `_records.ToList()` — ny kopi ved hvert kald. Returnér `IReadOnlyList<T>` via `AsReadOnly()` i stedet. |
| K-V29 | `Engine/BackupEngine.cs` | 761–763 | KISS | Low | Sti-normalisering i `MatchDrive` udføres inde i `foreach`-løkken. Afhænger ikke af loop-variablen — bør hejses ud. |
| K-V30 | `DependencyInjection/ServiceCollectionExtensions.cs` | 102–131 | KISS / SOLID-D | Medium | `IBackupEngine` registreres via håndskrevet factory-lambda der manuelt resolver alle afhængigheder. Tilføjelse/fjernelse af constructor-parameter kræver opdatering tre steder. |
| K-V31 | `Engine/TimeStamp/Readers/QuickTimeTimestampReader.cs` | 7–8 | SOLID-D | Low | `QuickTimeMetadataHeaderTimestampReader` og `QuickTimeMovieHeaderTimestampReader` instantieres som private felter — ikke injicerbare. |
