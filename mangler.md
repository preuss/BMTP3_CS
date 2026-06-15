# Core4 — Mangler / Issues

> **Opdateret 15 Jun 2026** — C-V21 + C-V26 + BackupConsoleCommand4 + BackupProgressDisplay refactored. 243 tests pass (104 Consoles + 139 Core4).
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

## Kodekategorisering

> **Ubrugt kode (unused code)** — kode som ikke bruges lige nu, men som kan være korrekt og potentielt nyttig (fx generisk utility skrevet men endnu ikke kaldt). Ikke nødvendigvis forkert — bare ikke aktiveret.

> **Død kode (dead code)** — kode som reelt ikke har nogen funktion i systemet længere. Erstattet, uopnåelig, ubrugelig eller forældet. Skal slettes.

> **Legacy kode** — gammel kode som produktionen stadig afhænger af. Kan være svær at ændre, dårligt dokumenteret/testet, men er stadig i aktiv brug. Skal håndteres forsigtigt.

## Resolved since last update

| Item | Status | Evidence |
|------|--------|----------|
| C-V01: BuildPlan refactor | ✅ **DONE** | Splittet i `BackupPlanBuilder` + `CreateDefault()` / `ApplyConfig()` / `ApplyCliOverrides()`. `BuildPlan` ~10 linjer. |
| C-V02: ParseXxx DRY | ✅ **DONE** | 8 parsere → `ParseEnum<T>()`. `ParseHashAlgorithm` beholdt som specifik. |
| C-V03: Fail-fast config enums | ✅ **DONE** | Ukendte config-værdier kaster `ArgumentException` i stedet for silent fallback. |
| C-V04: Dead null checks | ✅ **DONE** | `if (config.Source != null)` etc. fjernet — altid sande. |
| C-V05: IsNullOrWhiteSpace guards | ✅ **DONE** | Fjernet på enum-strenge. ParseEnum kaster på tomme/ugyldige værdier. |
| C-V06: Path.GetFullPath double | ✅ **DONE** | Fjernet første kald i CLI block. Global normalization fanger den. |
| C-V09: `GetRequiredService` for IBackupEngine + ConsolesPrinter | ✅ **DONE** | `GetService` → `GetRequiredService` i `BackupConsoleCommand4.cs`. |
| C-V10: `BackupResult? result = null` | ✅ **DONE** | `null!` + `ThrowIfNull` removed. |
| C-V11: DRY result-tælling | ✅ **DONE** | `BackupResultCounts` record shared af Printer + logger. |
| C-V12: Tre-trins logger | ✅ **DONE** | `GetRequiredService<ILogger<T>>()` i begge commands. |
| C-V13: Static AnsiConsole.Console | ✅ **DONE** | `ServiceProvider.GetRequiredService<IAnsiConsole>()`. |
| C-V14: `ExecuteAsyncForTests` public | ✅ **DONE** | `internal` + `InternalsVisibleTo`. |
| C-V15: Private constructor | ⚠️ **WONTFIX** | Bevidst mønster — `_optionsModels` forbliver `private`. |
| C-V16: CLI --delay override | ✅ **DONE** | `WasSupplied(DelayOption)` tilføjet i `ApplyCliOverrides()`. |
| C-V18/C-V19: `ConfigOptionResult` + `DoAddValidators` | ✅ **RETAINED** | Kommentarer tilføjet — plads til fremtidig validering. |
| C-V24: DRY result-tælling (dup) | ✅ **DONE** | Automatisk fikset af C-V11. |
| C-V25: `MarkRemainingCompletedTasksAsInactive` | ✅ **DONE** | Dead method slettet. |
| C-V21: ParseEnum string-strip → KebabCaseToPascalCase | ✅ **DONE** | `ParseEnum<T>()` bruger nu `NamingPolicyHelper.KebabCaseToPascalCase()` + `Enum.TryParse`. Ingen string-strip/loop. |
| BackupConsoleCommand4 refactor | ✅ **DONE** | `reportAction` capture + null-guard fjernet. `RunBackupWithProgressAsync` = 1-liner. `PrintAndLogStart` + `LogResult` extracted. `ValidateBackupOptions` simplified (hasConfig). DI lookups samlet. |
| BackupProgressDisplay refactor | ✅ **DONE** | `RunAsync<TResult>` med `Func<IProgress<BackupProgress>, Task<TResult>>`. Internal `BackupProgressRenderer`. `EscapeMarkup()` på filnavne. `CreateColumns` → collection expression. `RemoveExpiredFileTasks` → tuple deconstruction. Null guards. `sealed`. |
| BackupProgressDisplay concurrency bug | ✅ **FIXED** | Root cause: `Progress<T>` + parallel `Report()` fra engine. Midlertidig `lock + isClosed` → fjernet. Display antager nu serial progress. `IBackupEngine` XML-doc opdateret med kontrakt. |
| `ActionProgress<T>` slettet | ✅ **DONE** | Erstattet af `Progress<T>` (standard .NET) |
| `ProgressReportMapper` → `internal` | ✅ **DONE** | Flyttet til `BMTP3.Consoles.Progress` namespace. |
| `ProgressReport` → `internal sealed record` | ✅ **DONE** | Kun displayets view-model. |
| C-V07: Navn-fallback død kode | ✅ **DONE** | `backupOptions.Name` branch fjernet — kunne aldrig nås. |
| C-V08: ExecutionConfig tom klasse | ✅ **DONE** | Klasse + property slettet. Test `Load_TomlFile_EmptyExecution_DefaultsToNull` fjernet (16→15 tests). |
| C-V26: WriteDebugLine System.Console → IAnsiConsole | ✅ **DONE** | `WriteDebugLine(ProgressReport, IAnsiConsole)`. `IAnsiConsole` injectet i `BackupProgressRenderer`. Parameterrækkefølge: `console` før `debug`. | `BuildDryRunResult` helper, short-circuit før processing loop. `BackupResult.IsDryRun = true`. |
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
| **K-V32(1): SidecarRequest SourceType string → enum** | ✅ **DONE** | `string` → `BackupSourceType`. `SidecarService.BuildDocument` + `BackupEngine` opdateret. |
| **K-V32(2)(4): SourcePersistentUniqueId → SourceId + sat i engine** | ✅ **DONE** | Omdøbt til `SourceId`. `SourceId = record.Item.Id` tilføjet i `BackupEngine.cs:440-461`. |
| **K-V33: WPD-leak — MediaDeviceTraversal.SourcePath** | ✅ **DONE** | `file.FullName` → `BuildMtpSourcePath()` konstruerer `mtp://{device}/{drive}/{subPath}/{file}`. |
| **K-V34: Default hash kun SHA2_256** | ✅ **DONE** | `Enum.GetValues<HashAlgorithmType>()` — alle hash-typer som default. |
| **K-V35: Mangler --comparison-hash / --verification-hash CLI options** | ✅ **DONE** | `Option<List<string>>` med `Arity = OneOrMore` i `BackupOptionsModel4`. |
| **K-V36: ApplyCliOverrides mangler hash CLI** | ✅ **DONE** | Hash CLI overrider config i `ApplyCliOverrides()`. |
| **K-V37: Core4 hedder backup4, Core2 er default backup** | ✅ **DONE** | Core4 er nu `backup`, Core2 er `backup2`. |
| **SidecarRequest super-refactor: dictionary → polymorphic records** | ✅ **DONE** | `BackupSourceDetails` abstract record + `MediaDeviceDriveSourceDetails`/`FileSystemDriveSourceDetails`. `SidecarService.BuildDocument` bruger `switch`. |
| **BackupSourceDetails + derived flyttet til Models/** | ✅ **DONE** | `Engine/` → `Models/`. Namespace `BMTP3.Core4.Models`. |
| **SourceType fjernet fra BackupSourceDetails** | ✅ **DONE** | Polymorfi bærer typen — redundant enum property. |
| **SidecarServiceTests opdateret til polymorphic API** | ✅ **DONE** | `WriteAsync_WithAllOptionalFields` + `SampleRequest` bruger typed records. |
| **BackupEngineHappyPathIntegrationTests — FakeBackupDriveInfo** | ✅ **DONE** | Implementerer `IBackupFileSystemDriveInfo`. |
| **Core4 code smell analysis** | ✅ **DONE** | 25+ fund (H1-H7 High, M1-M14 Medium, L1-L11 Low). Se § Code Quality Audit. |
| **Build: 0 errors, 353 tests** | ✅ **DONE** | 249 Core4 + 104 Consoles. |
| **Progress display: Core4 scanning counts fixed, Consoles display rewritten** | ✅ **DONE** | `BackupScanner` bridger `SourceTraversalProgress` (var `null`). `FileSystemTraversal` tæller directories. `ProgressReport` har `DirectoriesTraversed` + `FilesDiscovered`. **Consoles display:** `BackupProgressDisplay` omskrevet — ingen polling loop, ingen lock/gate/version counter. `Progress<T>` skabes før `StartAsync` (null SyncContext). Se `§ Progress Display Bugs`. |

---

### ✅ Progress Display Bugs — `BackupProgressDisplay.cs` Rewritten

| # | Problem | Fix |
|---|---------|-----|
| 1 | **Manuelt polling loop (100ms)** | **Fjernet.** Spectre.Console renderer selv. `engineRunAsync` awaites direkte. |
| 2 | **`lock(gate)` + version counter** | **Fjernet.** `report` action opdaterer Spectre tasks direkte — thread-safe (Spectre bruger internal locks). |
| 3 | **`Progress<T>` dispatcher via SyncContext fanget inde i StartAsync** | **Fjernet.** `Progress<T>` skabes før `StartAsync` (linje 67 i `BackupConsoleCommand4.cs`). Capturer `null` context → dispatcher via `ThreadPool.QueueUserWorkItem`. |
| 4 | **`await Task.Delay(100)` fanger context** | **Fjernet.** Ingen polling loop — `await engineRunAsync(...)` awaites direkte i `StartAsync` callback. |
| 5 | **`RemoveExpiredInactiveTasks` i polling loop** | **Omskrevet.** File task expiry tjekkes inline i `UpdateFileTasks` (timestamp vs. 5s threshold). `RemoveAllFileTasks` kører efter engine completion. |

**Hvad ændret:**

| Fil | Før | Nu |
|-----|-----|-----|
| `BackupProgressDisplay.cs` | 183 linjer, polling loop, lock/gate, version counter | ~120 linjer, event-driven, direkte task updates, timestamp-based expiry |
| `BackupConsoleCommand4.cs` | `Progress<T>` inde i `StartAsync` callback | `Progress<T>` før `StartAsync` (null SyncContext), closure `reportAction` |

---

## Remaining Issues

### ✅ SIDECARREQUEST SUPER-REFACTOR — DONE

| # | Issue | Severity | Detail | Status |
|---|-------|----------|--------|--------|
| 1 | **Redesign `SidecarRequest`** — erstat utypet dictionary med proper typed records | ~~**🔴 HIGHEST**~~ | `SourceDetails` (`IReadOnlyDictionary<string, string>`) → polymorphic `BackupSourceDetails` med `MediaDeviceDriveSourceDetails`/`FileSystemDriveSourceDetails`. `SidecarService.BuildDocument` matcher på type. Population i `BackupEngine` ved connection (linje 166-192). | ✅ **DONE** |

### ✅ Smelly Code #4 — Progress — alle beholdes

| Fil | Beslutning | Grund |
|-----|-----------|-------|
| `ProgressBar/ProgressBar.cs` | **Beholdes** | Custom `IProgress<double>` ASCII bar i aktiv brug |
| `Progress/ConsoleProgressBar.cs` + `IProgressBar` | **Beholdes** | Custom `IProgressBar` i aktiv brug |
| `Progress/FileAndDirectoryCounter.cs` | **Beholdes** | Custom counter i aktiv brug |
| `Progress/Columns/ElapsedTimeAdvancedColumn.cs` | **Beholdes** | Aktivt brugt af `BackupProgressDisplay.cs:80` |
| `Progress/Columns/CounterColumn.cs` | **Beholdes** | Aktivt brugt af `BackupProgressDisplay.cs:75` |

### ✅ Smelly Code #5 — ProgressStatus — alle beholdes

| Fil | Beslutning | Grund |
|-----|-----------|-------|
| `ProgressStatus/ProgressStatusContext.cs` | **Beholdes** | Custom progress status context i aktiv brug |
| `ProgressStatus/ProgressStatusTask.cs` | **Beholdes** | Custom progress status task i aktiv brug |
| `ProgressStatus/ProgressStatus.cs` | **Beholdes** | Custom progress status i aktiv brug |

| Fil | Beslutning | Grund |
|-----|-----------|-------|
| `ProgressBar/ProgressBar.cs` | **Beholdes** | Custom `IProgress<double>` ASCII bar i aktiv brug |
| `Progress/ConsoleProgressBar.cs` + `IProgressBar` | **Beholdes** | Custom `IProgressBar` i aktiv brug |
| `Progress/FileAndDirectoryCounter.cs` | **Beholdes** | Custom counter i aktiv brug |
| `Progress/Columns/ElapsedTimeAdvancedColumn.cs` | **Beholdes** | Aktivt brugt af `BackupProgressDisplay.cs:80` |
| `Progress/Columns/CounterColumn.cs` | **Beholdes** | Aktivt brugt af `BackupProgressDisplay.cs:75` |

### ❌ MTP Cross-Connection Resume — GenerateAlmostUniqueId ikke koblet ind

| # | Issue | Severity | Detail |
|---|-------|----------|--------|
| 1 | **Kobl `GenerateAlmostUniqueId` ind i `SessionStateService`** som fallback match for MTP reconnect | **Medium** | Apple MTP regenererer `PersistentUniqueId` ved reconnect → match fejler → dubletter. Løsning: `ContentHash` felt på `SourceTraversalItem`/`BackupItem` + fallback matching i `SessionStateService`. Se `tasks/12-CrossConnectionResume.md`. |

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
| 2 | **Fjern eller ignorer `VerificationRetry`/`Timeout` options** | ❌ **WONTFIX** — `VerificationRetryCount`, `VerificationRetryDelayMs`, `VerificationTimeoutMs`, `VerificationDeleteOnFailure` implementeres ikke. Validering bør fjernes eller options ignoreres med warning. |
| 3 | **Fjern `--source-device`** | ❌ **WONTFIX** — Core4 bruger `--source-path "mtp://Device/Path"`. `--source-device` hører til Core2 (archived). |
| 4 | **Sæt `--backup-index` default** | ✅ **DONE** | `IBackupIndexWriter`/`JsonBackupIndexWriter` implementeret, T3 gate fjernet, catalog gemmes som `{dest}\.bmpt\{sessionId}\backup_catalog.json`. Default er `None` (som ønsket). |
| 5 | **Ryd op i Core2 options i `ApplicationServiceSetup`** | ✅ **DONE** | Udkommenteret — dødt fra Core4's side, Core2 kan stadig bruge det. |
| 6 | **Kobl SignalInterrupt cancel-wiring** | ✅ **DONE** | `Console.CancelKeyPress` fjernet fra `BackupConsoleCommand4`. Engine styrer selv cancellation via `SignalInterrupt` internt. |
| 7 | **Refaktorér ConsolesPrinter progress til `AnsiConsole.Progress()`** | Flyttet til Task 09 (Spectre Console). Skal bruge `AnsiConsole.Progress()` widget |
| 8 | **Tilføj tests for backup4 BuildPlan enum-mapping** | Tilføj tests for `BackupConsoleCommand4Helpers.BuildPlan` enum-mapping |


### ✅ Høj prioritet — TOML config reader — DONE

| # | Task | Status |
|---|------|--------|
| 1 | `--config` fil support | ✅ **DONE** | `BackupPlan4Config.cs` (nested model: Source/Destination/Collision/Metadata/Behavior/Execution), `BackupPlan4Loader.cs` (TOML via PascalToKebab, JSON via PropertyNameCaseInsensitive) |
| 2 | Map TOML til `BackupPlan` | ✅ **DONE** | Overfører alle felter inkl. `MaxDegreeOfParallelism`, `EnableMetadata`, `EnableTimestampCorrection`, `ResumeBehavior`; string-to-enum parsers for alle værdier |
| 3 | CLI integration | ✅ **DONE** | `BuildPlan()` loader config først som base, derefter `WasSupplied()` overrider eksplicitte CLI-args — CLI vinder altid |

### Høj prioritet — Implementer retry/resilience (især MTP)

| # | Task | Detail |
|---|------|--------|
| 1 | **Implementer exponential backoff** for transient I/O failures (download, hash, move, sidecar write) |
| 2 | **Implementér MTP resilience** — Gatekeeper timeout + retry ved COMException/disconnect mid-session |
| 3 | **Beslut** — Genbrug Core2's Polly `BackupResiliencePipeline` eller implementer lightweight retry |

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

### Høj prioritet — Skriv integration tests

| # | Task | Severity |
|---|------|----------|
| 1 | **Test fuld MTP traversal pipeline** (gatekeeper → connector → traversal → content) | **Høj** |
| 2 | **Test BackupEngine end-to-end** (filesystem → download → hash → sidecar → verify) | **Høj** |
| 3 | **Apple MTP reconnect** med ContentHash fallback matching | **Medium** |
| 4 | **`backup4` BuildPlan** enum-mapping (alle options til plan felter) | **Medium** |

### Feature gates — implementér når behov opstår

| # | Task | Feature gate |
|---|------|-------------|
| 1 | **Implementér `StopOnError=false`** — per-item catch findes men `throw` på linje 485 forhindrer continue-on-error | Linje 81 (Tier 3) |
| 2 | **Implementér `BackupIndexType.Database`** — SQLite catalog | Linje 84-85 (Tier 4) |
| 3 | **Implementér `EnableMetadata`** — metadata extraction | Linje 77-78 (Tier 3) |
| 4 | **Implementér `MaxDegreeOfParallelism`** — parallel execution | Linje 88-89 (Tier 4) |


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
| C-V09 | `BackupConsoleCommand4.cs` | 61–66 | Fail-Fast / YAGNI | **High** | ✅ **FIXED** — `GetService<T>()` → `GetRequiredService<T>()` for `IBackupEngine` og `ConsolesPrinter`. |
| ~~C-V10~~ | `BackupConsoleCommand4.cs` | 70–91 | Fail-Fast / KISS | Medium | ✅ **FIXED** — `BackupResult? result = null` → `BackupResult result = null!`, `ThrowIfNull(result)` removed. |
| ~~C-V11~~ | `BackupConsoleCommand4.cs` | 94–98 | SOLID-SRP / DRY | Medium | ✅ **FIXED** — `BackupResultCounts` record i Core4.Api.Models. `result.Counts` shared af `ConsolesPrinter` + logger. |
| ~~C-V12~~ | `BackupConsoleCommand4.cs` | 49–52 | KISS / DRY | Medium | ✅ **FIXED** — `GetRequiredService<ILogger<T>>()` i både `BackupConsoleCommand4` og `BackupConsoleCommand4ListDrives`. |
| ~~C-V13~~ | `BackupConsoleCommand4.cs` | 68 | Clean Architecture / SOLID-D | Medium | ✅ **FIXED** — `new BackupProgressDisplay(ServiceProvider.GetRequiredService<IAnsiConsole>())`. |
| ~~C-V14~~ | `BackupConsoleCommand4.cs` | 121–124 | YAGNI | Low | ✅ **FIXED** — `ExecuteAsyncForTests` → `internal` (via `InternalsVisibleTo`). |
| C-V15 | `BackupConsoleCommand4.cs` | 17–34 | KISS | Low | ⚠️ WONTFIX — Private constructor/`_optionsModels` pattern er bevidst. |
| ~~C-V16~~ | `BackupOptionsModel4.cs` | 155–162 | YAGNI | Medium | ✅ **FIXED** — `WasSupplied(DelayOption)` tilføjet i `ApplyCliOverrides()`. |
| C-V17 | `BackupOptionsModel4.cs` + `BackupConsoleCommand4.Helpers.cs` | 183 / 33 | Fail-Fast | Medium | ~~`PostWriteVerificationOption` default `Hash`, men `BuildPlan` initialiserer til `None`. `WasSupplied` returnerer false for implicit default → plan får `None` selvom option er `Hash`. Silent mismatch.~~ ✅ **FIXED** — Begge defaults ændret til `None` så de matcher. Brugeren vælger eksplicit `--verify hash`. |
| ~~C-V18~~ | `BackupOptionsModel4.cs` | 20 | YAGNI | Low | ✅ **RETAINED** — `ConfigOptionResult` beholdt med `// Reserved for future...` comment. |
| ~~C-V19~~ | `BackupOptionsModel4.cs` | 192–194 | SOLID-SRP | Low | ✅ **RETAINED** — `DoAddValidators()` beholdt med `// Placeholder for future...` comment. |
| C-V20 | `BackupPlan4Config.cs` | 57–59 | YAGNI | Low | `ExecutionConfig` er en tom sealed class uden properties. Deserialiseres og instantieres uden formål. |
| ~~C-V21~~ | `BackupPlan4Config.cs` | 1–59 | Clean Architecture / KISS | Medium | ✅ **IMPROVED** — `ParseEnum<T>()` bruger `NamingPolicyHelper.KebabCaseToPascalCase()` + `Enum.TryParse` i stedet for string-strip/loop. Config-modellen forbliver `string` (KISS — ingen converters på tværs af JSON/TOML). |
| C-V22 | `BackupPlan4Loader.cs` | 9–50 | DRY | Low | Strukturelt identisk med Core2's `BackupPlanLoader.Load` — extension/branch/deserialize/null-check/throw. Bør deles. |
| C-V23 | `ConsolesPrinter.cs` | 44–54 | SOLID-SRP / Clean Architecture | Medium | Én klasse håndterer output for Core2, Core3 og Core4 — tre uafhængige grunde til at ændre klassen. Core4-printer bør separeres. |
| ~~C-V24~~ | `ConsolesPrinter.cs` + `BackupConsoleCommand4.cs` | 87–90 / 95–98 | DRY | Medium | ✅ **FIXED** — By C-V11: `BackupResultCounts` record + `result.Counts` brugt begge steder. |
| C-V25 | `BackupProgressDisplay.cs` | 100–112 | YAGNI | **High** | ~~`MarkRemainingCompletedTasksAsInactive` er defineret men aldrig kaldt. Same logik inlineat i `UpdateFileTasks`. Dead method.~~ ✅ **FIXED** — slettet. |
| ~~C-V26~~ | `BackupProgressDisplay.cs` | 65–69 | Clean Architecture | Medium | ✅ **FIXED** — `WriteDebugLine` tager `(ProgressReport, IAnsiConsole)`, `if(_debug)` enkeltlinje, `IAnsiConsole` injectet i `BackupProgressRenderer`. Parameterrækkefølge: `console` før `debug`. |
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
| K-V16 | `Engine/Validation/BackupPlanValidator.cs` | 61–62 | Fail-Fast (forkert regel) | Medium | ✅ **FIXED** — Valideringen er nu betinget: `PostWriteVerification == Hash` kræver `VerificationHashAlgorithmTypes`. Samme mønster anvendt for `ComparisonHashAlgorithmTypes` (kun krævet når `CollisionComparisonType == Hash`). |
| K-V17 | `Engine/BackupEngine.cs` | 608–612 | Fail-Fast / Clean Architecture | Medium | ❌ **WONTFIX** — `BackupProgressPhase` er et livscyklus-indikator (Starting→Scanning→Transferring→Completed). `Completed` betyder "engine er færdig med at processere", ikke "succes". `BackupResultState.Cancelled` på linje 603 er den korrekte cancellation-indikator. |
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
| K-V38 | `Engine/Sidecar/SidecarRequest.cs` | 12 | DRY | **High** | `SourceType` er redundant — `SourceDetails` er polymorfisk, typen kan udledes via pattern match. |
| K-V39 | `Engine/BackupEngine.cs` | 287–288, 369–370, 492–493 | YAGNI / KISS | **High** | 3 identiske null-tjek på `required string`. Kommentaren "Stupid Visual Studio thinks that..." er uprofessionel. |
| K-V40 | `Engine/BackupEngine.cs` | 82–627 | SOLID-SRP | **High** | `RunAsync` er ~545 linjer. `foreach` over `pendingRecords` (280–538) bør ekstraheres. |
| K-V41 | `Engine/Sidecar/Document/SidecarDocument.cs`, `SidecarSection.cs`, `SidecarProperty.cs` | 3 (alle) | Encapsulation | **High** | `public` men er interne implementeringsdetaljer i `Engine.Sidecar.Document` — skal være `internal`. |
| K-V42 | `Engine/BackupEngine.cs` | 34, 51 | Consistency | **High** | `public sealed` med `internal` constructor — modsigelse. Bør være `internal sealed`. |
| K-V43 | `InternalsVisibleTo.cs` | 1–5 | Dead code | **High** | 4 ubrugte `using` directives: `System.Collections.Generic`, `System.Linq`, `System.Text`, `System.Threading.Tasks`. |
| K-V44 | `Models/IContent.cs` | 2–4 | Dead code | **High** | 3 ubrugte `using` directives: `System.Collections.Generic`, `System.Linq`, `System.Text`. |
| K-V45 | `Storage/BackupMediaDriveInfo.cs` | 37–38, 101, 111 | DRY | Medium | `DeviceName` == `FriendlyName` altid — redundant property. |
| K-V46 | `Engine/TempDirectoryHelper.cs` | 321 | YAGNI | Medium | `CleanupTempFiles` anden parameter `tempSidecarPath` altid `null`. |
| K-V47 | `Models/FileContent.cs` | 96–98, 111 | Correctness | Medium | `CancellationToken` accepteres men anvendes ikke — dokumenteret som "not currently used". |
| K-V48 | `Engine/BackupEngine.cs` | 135–136 | Magic string | Medium | `.bmtp3` hardcoded to gange — bør være `private const`. |
| K-V49 | `Engine/Sidecar/SidecarService.cs` | 29–36 | Magic string | Medium | `.ini`, `.json`, `.sidecar` extensions hardcoded — bør være `private const`. |
| K-V50 | `Engine/BackupEngine.cs` | 114 | Maintainability | Medium | Parameter `cancellationToken` reassignes til `linkedToken`. Brug separat variabel. |
| K-V51 | `Engine/BackupEngine.cs` | 103 | Naming | Medium | `_currentProgress` bruger felt-præfiks men er lokal variabel. |
| K-V52 | `Traversal/SourceTraversalFactory.cs` | 43–45 | Maintainability | Medium | `#pragma warning disable CA1416` bør scopes smallere. |
| K-V53 | `Engine/Validation/BackupPlanValidator.cs` | 80 | Design | Medium | `StopOnError=false` kaster `FeatureNotImplementedException`. Implementér eller fjern. |
| K-V54 | `Engine/Strategies/RenameCollisionResolver.cs` + `Engine/Hashing/HashService.cs` | 286 / 43 | DRY | Medium | `ToHashType` switch med 9 identiske cases i to klasser. |
| K-V55 | `Engine/BackupEngine.cs` | 751 | YAGNI | Medium | `Guard.RequireNonNull(drive)` inde i foreach — unødigt per-iteration check. |
| K-V56 | `Engine/Strategies/RenameCollisionResolver.cs` | 213, 214, 223 | Maintainability | Medium | 3 TODO-kommentarer i production code. |
| K-V57 | `Devices/MediaDeviceWrapper.cs` | 48 | Maintainability | Medium | TODO-kommentar i production code. |
| K-V58 | `SignalInterrupts/SignalInterruptEngine.cs` | 200 | Maintainability | Medium | TODO-kommentar i production code. |
| K-V59 | Flere filer (`FileContent.cs`, `RenameCollisionResolver.cs`, `SourceConnector.cs`) | diverse | Maintainability | Low | Exception messages mangler identifiers — gør debugging sværere. |
| K-V60 | `Engine/Sidecar/SidecarRequest.cs` | 9 | Naming | Low | `Format` bør hedde `SidecarFormat` for konsistens. |
| K-V61 | `Engine/BackupEngine.cs` | 753 | Magic string | Low | `"mtp://"` hardcoded inline — bør være delt konstant. |
| K-V62 | `Api/Models/DriveCatalogEntry.cs` | 13 | Consistency | Low | `SourceType` mangler `required` — alle andre properties har det. |
| K-V63 | `Engine/Session/BackupSessionKeyFactory.cs` | 23 | Null safety | Low | `plan.SourcePath.Trim()` uden null-check. |
| K-V64 | `Engine/BackupEngine.cs` | diverse | Cleanliness | Low | Verbose step-kommentarer, double blank lines. |
| K-V65 | `Engine/Sidecar/Document/SidecarSection.cs` + `SidecarProperty.cs` | 33–67 / 8–24 | DRY | Low | `WithProperty` overloads duplikerer `SidecarProperty.From` factories. |
| K-V66 | `Engine/Sidecar/Writers/JsonSidecarWriter.cs` | 36, 40 | Consistency | Low | `OrdinalIgnoreCase` dictionary — INI writer er case-sensitive. Bør matches. |
| K-V67 | `Engine/Strategies/RenameCollisionResolver.cs` | 51 | Formatting | Low | Double blank line. |
| K-V68 | `DependencyInjection/ServiceCollectionExtensions.cs` | 136–139 | KISS | Low | `AddIfNotNull` helper med kun én caller — inline hellere. |

---

## S Smelly Code -- Consoles reimplementerer Core4

Consoles indeholder kode der manuelt reimplementerer hvad Core4 allerede tilbyder -- DRY-kraenkelser, SOLID-problemer og KISS-overtraedelser.

> **Principper:** FIXME -> Slet Consoles' version, brug Core4's. Aldrig duplikere. Aldrig reimplementere. Aldrig laane kode paa tvaers af projekter.

### 🔴 Direkte duplikater (Consoles = Core4 kopi)

| Consoles fil | Core4 original | Risiko |
|---|---|---|
| \Utilities/GlobConverter.cs\ (200+ linjer) | \Core4/Utilities/GlobMatcher.cs\ | **Identisk algoritme.** Consoles mangler brace expansion som Core4 har. Flippet: rettelser skal goeres to steder. |
| \MetadataDirectoryExtensions.cs\ (2 metoder) | \Core4/Engine/TimeStamp/Extensions/MetadataDirectoryExtensions.cs\ | **Identisk kode.** \SafeGetString\ / \SafeTryGetDateTime\ -- kopieret ordret. |
| \xifreader/ExifDateTimeParser.cs\ (3 formater) | \Core4/Engine/TimeStamp/Parsers/DateTimeParser.cs\ (40+ formater) | **Samme formaal, inferior.** Consoles parser kun 3 formater. Fejler paa \/\, \-\, \.\, ms, ISO8601. |
| \xifreader/ExifReader2.cs\ (3 hardcodede tags) | \Core4/Engine/TimeStamp/Readers/ExifTimestampReader.cs\ + \TimestampCandidateFactory.cs\ | **Samme EXIF-extraction.** Consoles hardcoder 3 tags. Core4 er generisk via \TagGroups.Exif\. |


---

### Build Warnings (non-archived projects)

~45 warnings i Core4.Tests + Consoles + Consoles.Tests. Fikses i takt med andet arbejde.

| Område | Antal | Typiske fejl |
|--------|-------|-------------|
| `Core4.Tests` | ~14 | `CS8625`: null literal til non-nullable reference type (test data) |
| `Consoles` | ~5 | `CS8604`: null reference argument for `IServiceProvider` |
| `Consoles.Tests` | ~27 | `CS8604`/`CS8602` null reference + `xUnit2008` regex patterns |
| **I alt** | **~45** | |

### Dokumentation huller

| # | Mangler | Severity |
|---|---------|----------|
| 1 | JSON schema for `backup_catalog.json` — felter, struktur, eksempel | Low |

---

## § Smelly Code — Consoles reimplementerer Core4

Consoles indeholder kode der manuelt reimplementerer hvad Core4 allerede tilbyder — DRY-kraenkelser, SOLID-problemer og KISS-overtraedelser.

> **Principper:** FIXME → Slet Consoles' version, brug Core4's. Aldrig duplikere. Aldrig reimplementere. Aldrig "laane" kode paa tvaers af projekter.

### 🔴 Direkte duplikater (Consoles = Core4 kopi)

| Consoles fil | Core4 original | Risiko |
|---|---|---|
| `Utilities/GlobConverter.cs` (200+ linjer) | `Core4/Utilities/GlobMatcher.cs` | **Identisk algoritme.** Consoles mangler brace expansion som Core4 har. Flippet: rettelser skal goeres to steder. |
| `MetadataDirectoryExtensions.cs` (2 metoder) | `Core4/Engine/TimeStamp/Extensions/MetadataDirectoryExtensions.cs` | **Identisk kode.** `SafeGetString` / `SafeTryGetDateTime` — kopieret ordret. |
| `exifreader/ExifDateTimeParser.cs` (3 formater) | `Core4/Engine/TimeStamp/Parsers/DateTimeParser.cs` (40+ formater) | **Samme formaal, inferior.** Consoles parser kun 3 formater. Fejler paa `/`, `-`, `.`, ms, ISO8601. |
| `exifreader/ExifReader2.cs` (3 hardcodede tags) | `Core4/Engine/TimeStamp/Readers/ExifTimestampReader.cs` + `TimestampCandidateFactory.cs` | **Samme EXIF-extraction.** Consoles hardcoder 3 tags. Core4 er generisk via `TagGroups.Exif`. |

### 🟡 Delvise/inferiøre reimplementeringer

| Consoles | Core4 | Problemet |
|---|---|---|
| `Configs/BackupPlan4Loader.cs` — `NormalizeJson5()` (140 linjer) | — (boer vaere NuGet) | **Manuel JSON5 parser** med index-baseret string walking. Haandterer ikke escaped chars ordentligt. |
| `Configs/BackupPlan4Config.cs` (DTO) | `Core4/Api/Models/BackupPlan.cs` (record) | **DTO spejler `BackupPlan` felter.** Manuel mapping i `BackupPlanBuilder.ToBackupPlan()`. |

### 🟠 Over-engineered / unødvendige konstruktioner (KISS)

| Fil | Problem |
|---|---|
| `IO/Consoles/Progress/ConsoleProgressBar.cs` | Tredje progress bar i projektet. Manuelt `Console.Write(" ")`, `Console.CursorVisible = false`. Spectre.Console har `AnsiConsole.Progress()` built-in. |
| `IO/Consoles/ProgressBar/ProgressBar.cs` | Fjerde progress bar. `Timer` + `Interlocked.Exchange` + manuelle `\b` backspace chars. 8Hz tick. Demo-kode (`UsageTest()`) i klassen. |
| `IO/Consoles/ProgressStatus/ProgressStatusContext.cs` + `ProgressStatusTask.cs` | ~287 linjer zero-value pass-through wrappers om `Spectre.Console.ProgressContext`/`ProgressTask`. **Bug: `ProgressStatusTask.cs:43`** — `set => ProgressTask.Value = Value;` (skal vaere `value`). |
| `IO/Consoles/Progress/FileAndDirectoryCounter.cs` | `Task.Run` med `while(!cancel)` busy-wait loop. `lock` for integer increment. |
| `ConsoleCommands/OptionsBuilder.cs` | Reflection + `CallerArgumentExpression` + `MakeGenericMethod` for at undgaa `parseResult.GetValue(option)`. |
| `ConsoleCommands/AbstractCommandBase.cs` — `PopulateOptions<T>()` | Tung reflection til option binding. `OptionNameMatchesProperty` har bug: `--output` kan aldrig matche `OutputDirectory`. |
| `ConsoleCommands/ConsoleOptions/BackupOptions.cs` + `GlobalOptions.cs` | To tomme classes — dead code. |

### 🔵 SOLID-problemer

| Princip | Overtraedelser |
|---|---|
| **SRP** | `BackupConsoleCommand2.cs`: command execution + `ValidateBackupOptions` + programmatic `TryRunAsync`. `ProgressStatus`: single-task AND multi-task rendering. |
| **OCP** | 4 backup commands (1/2/3/Test) i stedet for eén generisk command med `IBackupEngine` injection — hver ny Core version kraever ny klasse. |
| **DIP** | Flere commands bruger `ServiceProvider.GetService<T>()` (service locator). `ConsoleProgressBar`: `System.Console` direkte. |

### 🟢 Fix-plan (Consoles → Core4 konvertering)

| # | Slet i Consoles | Erstat med |
|---|---|---|
| 1 | `Utilities/GlobConverter.cs` | `Core4.Utilities.GlobMatcher` — tilfoej `using BMTP3.Core4.Utilities` |
| 2 | `MetadataDirectoryExtensions.cs` | Brug Core4's version — den er `public` |
| 3 | `exifreader/` mappe | `Core4.Engine.TimeStamp.Readers.ExifTimestampReader` + `DateTimeParser` |
| 4 | `IO/Consoles/Progress/ConsoleProgressBar.cs` | `AnsiConsole.Progress()` |
| 5 | `IO/Consoles/ProgressBar/ProgressBar.cs` + `FileAndDirectoryCounter.cs` | `AnsiConsole.Progress()` |
| 6 | `IO/Consoles/ProgressStatus/ProgressStatusContext.cs` + `ProgressStatusTask.cs` | Brug `ProgressContext`/`ProgressTask` direkte |
| 7 | `ConsoleCommands/OptionsBuilder.cs` | `parseResult.GetValue(option)` | ✅ Slettet |
| 8 | `ConsoleCommands/AbstractCommandBase.cs` | `BaseConsoleCommand` daekker samme formaal | ✅ Slettet |
| 9 | `ConsoleCommands/ConsoleOptions/BackupOptions.cs` + `GlobalOptions.cs` + `RootOptions.cs` + `VerifyOptions.cs` | Slet — tomme classes | ✅ Slettet (hele mappen) |
| 10 | `Configs/BackupPlan4Config.cs` (DTO) | Serialiser direkte til `BackupPlan` |

Se `plan.md (plan.md Smelly Code Cleanup)` for task-management.
