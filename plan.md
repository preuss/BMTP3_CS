# BMTP3.Core4 — Plan

> **Opdateret 22 Jun 2026** — 1179 tests pass. Docs cleanup: værdifuld viden ekstraheret fra slettede docs og merget ind her.
> 
> ⚠️ **NO IMPLEMENTATION WITHOUT PERMISSION:** Spørg altid først. Implementér aldrig før brugeren siger "go" / "do it" / "implementér" / "execute" / "kør". Indtil da: research, read, grep, spørg.
> 
> ⚠️ **FAIL-FIRST:** Alle gates/tjek i traversal og engine skal kaste exception ved fejl — aldrig `yield break`, `return` eller `continue` for at tie stille om problemer. Source der ikke findes = throw. Eneste undtagelse: per-item try-catch der markerer failed items men re-thrower (fail-fast).
> 
> ⚠️ **NO CORE/CORE2/CORE3 CHANGES:** `BMTP3.Core`, `BMTP3.Core2`, `BMTP3.Core3` og deres Consoles commands (`BackupConsoleCommand.cs`, `BackupConsoleCommand2.cs`, `BackupConsoleCommand3.cs`) er **archived/readonly** — de ændres aldrig. Kun `BMTP3.Core4` og `BMTP3.Consoles` må redigeres.
>
> ⚠️ **CORE4-ONLY FOKUS:** Vi retter **aldrig** noget som ikke har direkte Core4-tilhørsforhold. Core2/Core3 cleanup, shared helpers på tværs af archived kode, og dokumentation af ikke-Core4 ting er aldrig fokus. Når Core4 er 100% færdig, slettes alle tilhørsforhold til Core2/Core3 i Consoles.
> 
> ⚠️ **PATH NAMING STANDARD:** Se `## Path Naming Standard` nedenfor.

## Kodekategorisering

> **Ubrugt kode (unused code)** — kode som ikke bruges lige nu, men som kan være korrekt og potentielt nyttig (fx generisk utility skrevet men endnu ikke kaldt). Ikke nødvendigvis forkert — bare ikke aktiveret.

> **Død kode (dead code)** — kode som reelt ikke har nogen funktion i systemet længere. Erstattet, uopnåelig, ubrugelig eller forældet. Skal slettes.

> **Legacy kode** — gammel kode som produktionen stadig afhænger af. Kan være svær at ændre, dårligt dokumenteret/testet, men er stadig i aktiv brug. Skal håndteres forsigtigt.

## Path Naming Standard

### Format
```
[Context][Relative|Absolute][File|Directory]Path
```

### `source`-prefix regel

`source` bruges **kun** når noget er i en source-kontekst (`BackupPlan`, `BackupScanner`, traversal).
Discovery-laget (`IBackupDriveInfo`) ved ikke hvad en "source" er — det kender kun drives og devices. Derfor:

| Lag | Eksempel | Forklaring |
|-----|----------|------------|
| **Discovery** | `RootPath`, `DeviceName`, `DriveName` | Ingen `source`-prefix — pre-source |
| **Source** | `SourcePath`, `sourceDirectoryPath` | Først når brugeren vælger det som source |

### Eksempler — Filesystem

| Navn | Indhold | Betydning |
|------|---------|-----------|
| `relativeFilePath` | `"2026\jan\picture.jpg"` | Relativ sti inkl. filnavn |
| `relativeDirectoryPath` | `"2026\jan"` | Relativ sti, kun mappe |
| `sourceRootDirectoryPath` | `@"C:\temp\source"` | Absolut rodmappe for source |
| `backupRootDirectoryPath` | `@"C:\temp\target"` | Absolut rodmappe for backup |
| `sourceAbsoluteFilePath` | `@"C:\temp\source\2026\jan\picture.jpg"` | Fuld absolut sti til fil |
| `sourceAbsoluteDirectoryPath` | `@"C:\temp\source\2026\jan"` | Fuld absolut sti til mappe |
| `sourceRelativeFilePath` | `"2026\jan\picture.jpg"` | Relativ sti med source-kontekst |
| `sourceRelativeDirectoryPath` | `"2026\jan"` | Relativ mappe med source-kontekst |

### Eksempler — MTP

| Navn | Indhold | Betydning |
|------|---------|-----------|
| `rootPath` (discovery) | `"mtp://Apple iPad/Internal Storage"` | Device + drive, pre-source |
| `deviceName` | `"Apple iPad"` | Device FriendlyName |
| `driveName` | `"Internal Storage"` | Storage navn |
| `sourcePath` (plan) | `"mtp://Apple iPad/Internal Storage/DCIM/Camera"` | Fuld source sti inkl. directoryPath |
| `directoryPath` | `"DCIM/Camera"` | Path-element i URI efter `{deviceName}/{driveName}/` |
| `sourceRelativeFilePath` | `"2024/jan/picture.jpg"` | Relativ til sourcePath |
| `sourceRelativeDirectoryPath` | `"2024/jan"` | Relativ mappe til sourceRootPath |

### Regler

1. **Suffix `Path` er altid påkrævet** — `RelativeFilePath` ✓, `RelativeFile` ✗
2. **`File` = inkl. filnavn** — `"2026\jan\picture.jpg"`
3. **`Directory` = kun mappe-sti** — `"2026\jan"`
4. **Context prefix ved tvivl** — `sourceRelativeFilePath`, `targetRelativeDirectoryPath`
5. **Undgå `Path` alene** — sig altid `File` eller `Directory` + `Path`
6. **Local variables følger samme mønster** — `string relativeFilePath`, `string sourceRootDirectoryPath`
7. **Metoder der returnerer path følger samme mønster** — `GetRelativeDirectoryPath()`, `NormalizeRelativeDirectoryPath()`
8. **Template tokens (custom patterns) bruger camelCase** — `{sourceRelativeFilePath}`, `{relativeDirectoryPath}`
9. **Sidecar property names følger pascalCase** — `SourceRelativeFilePath`, `TargetRelativeFilePath`

### Forbudte navne i projektet

Må ikke bruges fremover i `BMTP3.Core4` eller `BMTP3.Consoles`:

- `path` (alene uden kontekst)
- `sourcePath`
- `targetPath`
- `relativePath`
- `folderPath`
- `targetRelativePath`
- `FilePath` (uden `Relative`/`Absolute` prefix)
- `DirectoryPath` (uden `Relative`/`Absolute` prefix)

## Færdige opgaver

- [x] I1 — Disk space check (service: `IDiskSpaceValidator` + `DiskSpaceValidator`)
- [x] I3 — Timestamp preservation på temp-fil efter download (basal: `CreationTime` + `LastWriteTime`)
- [x] I3a — Timestamp preservation: sæt ALLE datoer på temp-fil (`DateCreated`, `DateModified`, `DateAuthored`, `DateAccessed`) — ✅ done in `EarliestTimestampResolutionService`
- [x] I4 — Timestamp correction på destinationsfil efter `MoveTo()` — ✅ done (applied in `ResolveAndApplyEarliestAsync` before move; file timestamps survive the move)
- [x] C1 — Post-run persistence: kald `_sessionState.SaveAsync()` efter foreach-loop — ✅ done in `BackupEngine.cs:400` (finally block)
- [x] C2 — File-backed `SummaryStore`: gem `BackupSummary` som JSON i `.bmtp3/` — ✅ `BackupJsonSummaryStore`
- [x] C3 — Implementer real filesystem traversal og scanner — ✅ `FileSystemTraversal` + `BackupScanner`
- [x] I5 — Binary compare (identisk fil-detektion) i collision resolution — ✅ `FileCompareService` + 5 algorithms + `BinaryFileComparerSelector` + wired in `RenameCollisionResolver`
- [x] N4 — Ryd op: fjern `EarliestTimestampResolutionServiceAnother.cs` — ✅ deleted
- [x] I4/E4 — Fix `MoveableFileContent.MoveTo` overwrite bug — `FileInfo.MoveTo(dest, false)` → `overwrite`
- [x] E1 — Fix `CompositeTimestampReader` CancellationToken + redesign: `ITimestampReader` / `ICompositeTimestampReader` / `TimestampReaderException` / `TryReadCollect`
- [x] E2 — Tilføj `ThrowIfCancellationRequested()` i starten af foreach-loop (BackupEngine.cs:219) — ✅ senere fjernet som redundant
- [x] FileContent cleanup — fjernet Volatile, simplificeret dispose-logik, doc comments genindsat
- [x] Ubrugt `earliest` — nu brugt på linje 301; `ResolveDate()` fjernet; kaster exception hvis null
- [x] Step-number jump (8→10) — rettet til `// 9. Return BackupResult`
- [x] DeleteEmptyDirectories — allerede implementeret via `CleanupSessionTempDirectory` i finally block
- [x] GlobMatcher (Core4) — brace expansion + bugfixes. Samme fixes backportet til GlobConverter (Consoles) og GlobMatcher (Core2)
- [x] E6 — Per-item try-catch i processing loop (BackupEngine.cs), markér failed items som Failed, re-throw (fail-fast)
- [x] I6 — Post-write hash verification efter MoveTo (kun Hash, Binary fjernet — giver ikke mening efter MoveTo)
- [x] `PostWriteVerificationType.Binary` fjernet — kun None/Hash tilbage
- [x] E5 — Fjern redundant timestamp-logik fra `DownloadService`
- [x] I7 — Implement Include/Exclude patterns i `FileSystemTraversal` — `GlobMatcher.IsIncluded` i loopet, gates ikke fjernet
- [x] — DryRun — short-circuit med `BuildDryRunResult` helper før processing loop
- [x] — Sidecar redesign: Document/Section/Property model (fluent API + weight-sortering)
- [x] — INI sidecar writer med `#` kommentar-støtte (multi-line)
- [x] — JSON sidecar writer
- [x] — Sidecar format: `[Source]`, `[SourceDevice]`/`[SourceDrive]`, `[Backup]`, `[Path]`, `[Hashes]` (matcher brugerens spec)
- [x] — `MD5` i stedet for `MD5_128` i hashes sektion
- [x] — Alle hashes altid til stede i `[Hashes]` + `SHA3_512` alias
- [x] — `ResolvedDateTime` → `MediaTakenDateTime` rename i hele Core4
- [x] — `ItemMetadata` udvidet med originale datoer: `AuthoredDateTime`, `CreatedDateTime`, `ModifiedDateTime`, `AccessedDateTime`
- [x] — Originale datoer captured før timestamp correction
- [x] — Sidecar læser datoer fra `ItemMetadata` i stedet for `Item`
- [x] — Writer Stream refactoring: `ISidecarWriter.WriteToFileAsync` → `WriteToStreamAsync(Stream)`
- [x] — `IniSidecarWriter` forbedret: `WriteCommentBlock` + options (CRLF, PreserveEmptyCommentLines, WriteKeysWithNullValues)
- [x] — `JsonSidecarWriter` forbedret: `CreateSerializableModel` + `SerializeAsync(stream)` — ingen mellemstring
- [x] — N6: SidecarServiceTests (6), IniSidecarWriterTests (14), JsonSidecarWriterTests (10) = 30 nye tests. I alt 160.
- [x] — Ctrl+C Del 1: `BackupEngine.RunAsync` fanger `OperationCanceledException` → `BackupResult` med `State = Cancelled`
- [x] — Ctrl+C Del 2: SignalInterruptEngine (singleton subscription engine) + SignalInterrupt static entry + SignalInterruptRegistrationBuilder. 6 filer. Gammel event-kode slettet. `ISignalSubscription` interface tilføjet.
- [x] — Ctrl+C Del 3: Shadow af `cancellationToken = cancellationTokenSource.Token`. Handler-baseret registrering: `SignalInterrupt.On(All).Handler(ctx => { cts.Cancel(); }).Create()`. `using ISignalSubscription` sikrer cleanup. TODO: "Finally saves when Cancel() is called."
- [x] — Ctrl+C Del 4: SignalInterrupts-tests (37): enum (5), context (10), builder (12), entry point (10). + BackupEngine cancellation pattern tests (5). I alt 204 tests.
- [x] — `ISignalSubscription : IDisposable` interface med `Signals` + `Handler` properties
- [x] — `Create()`/`Register()` returnerer `ISignalSubscription` i stedet for `IDisposable`
- [x] — `using static` fjernet fra BackupEngine.cs (ubrugt)
- [x] — Doc comments i `SignalInterrupt.cs` og `SignalInterruptEngine.cs` opdateret til `ISignalSubscription`
- [x] — `BytesProcessed` akkumuleres nu live i Completed-fasen
- [x] — `MediaDevices.dll` reference tilføjet til Core4.csproj
- [x] — `FileSystemTraversal`: `yield break` → `throw DirectoryNotFoundException` når source ikke findes (fail-first)
- [x] — `SourceTraversalItem`: `FileName` + `RelativePath` (begge `required`) — traversal leverer alt, `BackupScanner` mapper kun properties
- [x] — `BackupScanner` renset: ingen `Path.*` kald, `Id = sourceItem.Id` i stedet for `relativePath`
- [x] — `MtpUriParser` + `MtpUriParseResult` — parse `mtp://Device/Path`. 15 tests.
- [x] — `IMtpGatekeeper` → `IMediaDeviceGatekeeper` — `Func<CancellationToken, Task<T>>`, `AcquireAsync(TimeSpan, ...)`, `ThrowIfDisposed`, `Interlocked` dispose. 9 tests.
- [x] — `IMediaDeviceSession` erstattet af `ISession`/`IConnectedSource`/`IConnectedMediaDriveSource`. `MediaDeviceSession`/`IMediaDeviceSession` slettet.
- [x] — **IFileStore redesign → `IBackupDriveInfo`** — `IBackupDriveInfo` (base), `IBackupFileSystemDriveInfo`, `IBackupMediaDriveInfo` (specialized). `BackupFileSystemDriveInfo` (fail-first med `IsReady` guard, `long` i stedet for `ulong?`). `BackupMediaDriveInfo` (`MediaDevice` + `MediaDriveInfo`, `Name.TrimStart('\\')` som `DriveName`). Omdøbt: `FileSystemFileStore` → `BackupFileSystemDriveInfo`, `MediaDeviceFileStore` → `BackupMediaDriveInfo`.)
- [x] — **MTP test cleanup** — Fjernet 16 tests der kaldte `MediaDevice.GetDevices()` direkte (kræver real MTP device). Kun constructor null-check tests tilbage. Opgraderet til xunit.v3 3.2.2.
- [x] — **`IDriveProvider` architecture**: `IDriveProvider` + `FileSystemDriveProvider` + `MediaDeviceDriveProvider` + `DriveProvider` (composite). Erstattede `IFileSystemSourceDiscovery`/`IMediaDeviceSourceDiscovery`/`ICombinedSourceDiscovery`.
- [x] — **DI registrering:** `FileSystemDriveProvider`, `MediaDeviceDriveProvider` (kun Win 7+), `DriveProvider` (composite) registreret i `ServiceCollectionExtensions`.
- [x] — **Build:** 0 errors, 0 warnings. **Tests:** 249 passed.
- [x] — **Session redesign:** `IMediaDeviceSession` → `ISession`, `IConnectedSource : ISession`. Slettet gamle session-filer.
- [x] — **Factory rename:** `IConnectedSourceFactory` → `ISourceConnector`, `Create()` → `Connect()`. Slettet `ConnectedSourceFactoryCreateRequest.cs`, `FileSystemTraversalFactoryStub.cs`.
- [x] — **Source type redesign:** `IConnectedMediaDeviceSource` → `IConnectedMediaDriveSource` (både `IMediaDevice Device` + `IMediaDrive Drive`).
- [x] — **SourceConnector:** Bruger `IMediaDeviceInfo.GetDevices()` → `IMediaDeviceInfo.Connect()` → `IMediaDevice.Drives` → `ConnectedMediaDriveSource(IMediaDevice, IMediaDrive)`.
- [x] — **SourceTraversalFactory:** `Create(IConnectedSource)` — 1 param, pattern-matching på source types, injecter `IMediaDeviceGatekeeper`.
- [x] — **MediaDeviceTraversal:** Bruger `IMediaDirectory`/`IMediaFile` — ingen NuGet typer i body.
- [x] — **IMediaFile.OpenRead()** tilføjet, `MediaDeviceContent` opdateret til at bruge `IMediaFile`.
- [x] — **BackupEngine.Create(connectedSource):** 1 arg (ingen `IBackupDriveInfo`).
- [x] — **Build:** 0 errors, 0 warnings. **Tests:** 249 passed.
- [x] — **Validator gates relaxed**: 16 af 20 `FeatureNotImplementedException` gates fjernet. Resterer (4): `EnableMetadata` (T3), `StopOnError=false` (T3), `BackupIndexType.Database` (T4), `MaxDegreeOfParallelism` (T4).
- [x] — **Core4 i Consoles:** `BackupConsoleCommand4.cs` + helpers, `ConsolesPrinter` opdateret med Core4 overloads, `ApplicationServiceSetup` registrerer `AddBMTP3Core4()`, `backup4` subcommand tilgængelig.
- [x] — **Drive matching fix:** `BackupEngine.MatchDrive()` bruger `StartsWith` i stedet for `Equals`. Relative sub-path extracted til `SourceTraversalRequest.SubPath`.
- [x] — **MediaDeviceTraversal sub-path:** `NavigateToSubDirectory()` navigerer gennem `IMediaDirectory.Directories` baseret på `SubPath`.
- [x] — **MtpUriParser genindsat:** Restored + tests. Parser krævet af produktion.
- [x] — **DriveCatalog API**: `IDriveCatalogService` + `DriveCatalogEntry` + `DriveCatalogService` + DI registration
- [x] — **Devices wrapper-lag:** `IMediaDeviceInfo`/`MediaDeviceInfo`, `IMediaDevice`/`MediaDeviceWrapper`, `IMediaDrive`/`MediaDrive`, `IMediaDirectory`/`MediaDirectory`, `IMediaFile`/`MediaFile`, `IMediaItem`, `MediaFileAttribute` — 12 files, komplet abstraktion over MediaDevices.dll
- [x] — **MTP pipeline NuGet-free:** `MediaDeviceTraversal` bruger `IMediaDirectory`/`IMediaFile`, `MediaDeviceContent` bruger `IMediaFile`
- [x] — **ConnectedSource redesign:** `ISession`/`IConnectedSource`/`ISourceConnector`/`IConnectedMediaDriveSource`. `MediaDeviceSession`, `IMediaDeviceSession` slettet
- [x] — **Delay feature:** `BackupPlan.Delay` (int), `Helpers/BackupDelay.cs` struct, validering i `BackupPlanValidator`, implementeret i `BackupEngine` loop, `--delay` CLI option + `BuildPlan` mapping
- [x] — **Build:** 0 errors, 0 warnings. **Tests:** 249 passed.
- [x] — **PreserveHierarchy default:** Enum-ordning (PreserveHierarchy=0). Eksplicit default i `BackupPlan.OutputStructureStrategy`.
- [x] — **Custom columns/spinners til Consoles:** `ValueOfMaxColumn`, `ElapsedTimeAdvancedColumn`, `SequenceSpinner` kopieret fra Core, wired i `BackupProgressDisplay`.
- [x] — **BackupIndexType.Json catalog:** `IBackupIndexWriter`/`JsonBackupIndexWriter`, `BackupIndexCatalog`/`BackupIndexFileEntry`/`BackupIndexTimestamps`, gemmes som `{dest}\.bmpt\{sessionId}\backup_catalog.json`. T3 gate fjernet.
- [x] — **Temp dir cleanup:** `.tmp`-rod slettes når tom efter session cleanup. Temp-filer har ikke længere `.tmp` suffiks.
- [x] — **SignalInterrupt cancel-wiring:** `Console.CancelKeyPress` fjernet fra `BackupConsoleCommand4`. Engine styrer selv cancellation.
- [x] — **Skip cleanup:** Temp-fil slettes ved `CollisionResolutionAction.Skip`.
- [x] — **TOML config reader:** `BackupPlan4Config.cs` (nested model: Source/Destination/Collision/Metadata/Behavior/Execution), `BackupPlan4Loader.cs` (TOML via PascalToKebab, JSON via PropertyNameCaseInsensitive). `BuildPlan()` loader config først, derefter `WasSupplied()` overrider CLI-args.
- [x] — **Consolidation Phase 1:** `OptionHelpers.WasSupplied<T>()` extracted to shared file; `EngineArgumentBuilder()` dead code deleted from `BackupConsoleCommand2.cs`; `FakeFileTransfer.cs` (empty, unused) deleted; `FakeGatekeeper` consolidated to single shared class in `Fakes/`. **0 errors, 249 tests.**
- [x] — **SidecarRequest super-refactor:** Dictionary + section name fjernet. Polymorphic `BackupSourceDetails` abstract record med `MediaDeviceDriveSourceDetails` (MTP) og `FileSystemDriveSourceDetails` (FileSystem). `BackupRecord` fik `required BackupSourceDetails SourceDetails`. `SidecarService.BuildDocument` bruger `switch` på `SourceDetails`. `BackupEngine` pattern matcher `matchedDrive` én gang ved connection, sætter `SourceDetails` på alle records og sidecar.
- [x] — **BackupSourceDetails + derived flyttet til Models/:** `Engine/BackupSourceDetails.cs` → `Models/BackupSourceDetails.cs`. `Engine/Sidecar/MediaDeviceDriveSourceDetails.cs` → `Models/MediaDeviceDriveSourceDetails.cs`. `Engine/Sidecar/FileSystemDriveSourceDetails.cs` → `Models/FileSystemDriveSourceDetails.cs`. Namespace: `BMTP3.Core4.Models`.
- [x] — **SourceType fjernet fra BackupSourceDetails:** Polymorfi bærer typen — redundant enum property.
- [x] — **SidecarServiceTests + BackupEngineHappyPathIntegrationTests fixed:** Opdateret til polymorphic API. 353 tests total (249 Core4 + 104 Consoles).
- [x] — **Core4 code smell analysis:** 25+ fund dokumenteret — H1-H7 (High), M1-M14 (Medium), L1-L11 (Low). Se `mangler.md § Code Quality Audit` for detaljer.
- [x] — **Progress display rewrite:** `BackupProgressDisplay.cs` omskrevet — event-driven, direkte Spectre task updates, ingen polling/lock/gate. `BackupConsoleCommand4.cs` skaber `Progress<T>` før `StartAsync` (null SyncContext). `BackupScanner` bridger `SourceTraversalProgress` (var `null`). `ProgressReport` har `DirectoriesTraversed` + `FilesDiscovered`. Traversal tæller directories.

## Næste opgaver (prioriteret)

### 0. 🧹 Smelly Code Cleanup — Consoles reimplementerer Core4 (DRY)

Se `mangler.md § Smelly Code` for fuld analyse og fix-plan. Kort:

| # | Slet i Consoles | Erstat med | Prio |
|---|---|---|---|
| 1 | `Utilities/GlobConverter.cs` | `Core4.Utilities.GlobMatcher` | Høj |
| 2 | `MetadataDirectoryExtensions.cs` | Brug Core4's version | Høj |
| 3 | `exifreader/` mappe | Core4's `ExifTimestampReader` + `DateTimeParser` | Høj |
| 4 | `ProgressBar.cs`, `ConsoleProgressBar.cs`, `FileAndDirectoryCounter.cs` | **Alle beholdes** — custom progress-komponenter i aktiv brug | Lav |
| 5 | `ProgressStatusContext.cs`, `ProgressStatusTask.cs`, `ProgressStatus.cs` | **Alle beholdes** — custom progress status i aktiv brug | Lav |
| 6 | `OptionsBuilder.cs` | `parseResult.GetValue(option)` | Medium |
| 7 | `AbstractCommandBase.cs` | `BaseConsoleCommand` | Medium | ✅ Slettet |
| 8 | `BackupOptions.cs` + `GlobalOptions.cs` + `RootOptions.cs` + `VerifyOptions.cs` (hele `ConsoleOptions/` mappen) | Slet — tomme classes | Lav | ✅ Slettet |
| 9 | `BackupPlan4Config.cs` (DTO) | Serialiser direkte til `BackupPlan` | Lav |

### 1. 🥇 Consoles CLI cleanup + BackupPlan Delay

| # | Task | Status |
|---|------|--------|
| 1 | **Tilføj `Delay` til `BackupPlan`** | ✅ |
| 2 | MTP sourcePath format | ✅ |
| 3 | `--backup-index` default guard | ✅ |
| 4 | Core2 `BackupEngineOptions` config | ✅ |
| 5 | SignalInterrupt cancel-wiring | ✅ |
| 6 | Tests for backup4 BuildPlan enum-mapping | ✅ |
| 7 | `--comparison-hash` / `--verification-hash` CLI options | ✅ |
| 8 | Omdøb backup4 → backup, backup (Core2) → backup2 | ✅ |
| 9 | BackupPlanBuilder defaults til ALLE hash-typer | ✅ |

### 2. Integrér sidste wrapper-holdere (småopgave)

| # | File | Nuværende | Skal ændres til |
|---|------|-----------|-----------------|
| 1 | `MediaDeviceDriveProvider` | `MediaDevice` / `MediaDriveInfo` | `MediaDeviceInfo` / `MediaDrive` |
| 2 | `BackupMediaDriveInfo` | `(MediaDevice, MediaDriveInfo)` | `(IMediaDeviceInfo, IMediaDrive)` |

### 3. 🔧 Byg advarsler — ryd op (~45 warnings i non-archived projekter)

| Område | Antal | Typiske fejl |
|--------|-------|-------------|
| `Core4.Tests` | ~14 | `CS8625`: null literal til non-nullable reference type |
| `Consoles` | ~5 | `CS8604`: null reference argument for `IServiceProvider` |
| `Consoles.Tests` | ~27 | `CS8604`/`CS8602` null reference + `xUnit2008` regex patterns |
| **I alt** | **~45** | Fikses i takt med andet arbejde |

### 4. TOML config reader — ✅ DONE

`BackupPlan4Config.cs` + `BackupPlan4Loader.cs` + `WasSupplied()` merge i `BuildPlan()`. TOML (kebab-case via PascalToKebab), JSON (camelCase via PropertyNameCaseInsensitive). Alle felter mappet inkl. `MaxDegreeOfParallelism`, `EnableMetadata`, `EnableTimestampCorrection`, `ResumeBehavior`.

### 5. Retry / Resilience (især MTP)

- Exponential backoff helper
- MTP resilience (COMException, disconnect)
- Lightweight retry uden Polly dependency

### 6. Integration test

- MTP traversal pipeline test (mock via NSubstitute)
- BackupEngine end-to-end test

### 6.1 🧪 Specifikke test huller

| # | Manglende test | Severity |
|---|---------------|----------|
| 1 | MTP traversal pipeline (gatekeeper → connector → traversal → content) | Høj |
| 2 | BackupEngine end-to-end (filesystem → download → hash → sidecar → verify) | Høj |
| 3 | Apple MTP reconnect med ContentHash fallback matching | Medium |
| 4 | `backup4` `BuildPlan` enum-mapping (alle options → plan felter) | Medium |

### 6.2 📖 Dokumentation huller

| # | Mangler |
|---|---------|
| 1 | JSON schema for `backup_catalog.json` — felter, struktur, eksempel |

### 7. 🥈 MTP Cross-Connection Resume

| # | Task | Status |
|---|------|--------|
| 1 | Tilføj `ContentHash` til `SourceTraversalItem` + `MediaDeviceTraversal` | ❌ |
| 2 | Tilføj `ContentHash` til `BackupItem` + `BackupScanner` passthrough | ❌ |
| 3 | Fallback matching i `SessionStateService` (prøv `PersistentUniqueId` først, så `ContentHash`) | ❌ |
| 4 | Tests: Apple-enhed der regenererer IDs → ContentHash match | ❌ |

Se `tasks/12-CrossConnectionResume.md` for detaljer.

### 8. Public API overvejelser

- `ISidecarService` public? → ✅ **WONTFIX** — Forbliver `internal`. Core4 bruges internt via `BackupEngine.RunAsync()`. Ingen eksterne forbrugere har brug for direkte sidecar-generering. Sidecar-funktionalitet eksponeres via `BackupPlan.SidecarFormat` og kører automatisk i engine.

### 9. Senere (feature gates)

- `StopOnError=false` (T3), `BackupIndexType.Database` (T4), `EnableMetadata` (T3), `MaxDegreeOfParallelism` (T4)

### Arkitekturforskelle (ikke 1:1 — bevidste valg)

| Core / Core2 feature | Core4 status | Begrundelse |
|---|---|---|
| Handler-hierarki (IBackupHandler, IDriveHandler) | ➡️ Samlet i BackupEngine | Enklere arkitektur; engine dispatcher selv |
| Pipeline stages (8 step-klasser med kanaler) | ➡️ Strategi-baseret loop | Mindre kompleksitet, færre allocations |
| State machines (JobStateMachine, ItemStateMachine) | ➡️ Inline status i record | State machines overkill for sekventielt flow |
| ScannerGatherer / IFileScanner / IMediaFileScanner | ✅ Filesystem + MTP traversal | Begge dækket (`FileSystemTraversal` + `MediaDeviceTraversal`) |

## Code Quality Audit (14 Jun 2026)

Fuld gennemgang af Consoles og Core4 mod SOLID, Clean Architecture, Fail-Fast, DRY, KISS, YAGNI.
Se `mangler.md § Code Quality Audit` for alle fund. Kort prioriteret overblik:

### Consoles — kritiske fund (High)

| ID | Fil | Problem |
|----|-----|---------|
| C-V01 | `BackupConsoleCommand4.Helpers.cs` | ~~`BuildPlan` 237 linjer~~ ✅ **DONE** — Splittet i `BackupPlanBuilder.cs` + `Helpers.cs` (~29 linjer). |
| C-V02 | `BackupConsoleCommand4.Helpers.cs` | ~~9 næsten-identiske `ParseXxx`~~ ✅ **DONE** — `ParseEnum<T>()`. |
| C-V03 | `BackupConsoleCommand4.Helpers.cs` | ~~`ParseXxx` silent fallback~~ ✅ **DONE** — `_ => throw`. |
| C-V09 | `BackupConsoleCommand4.cs:61` | ~~`GetService<IBackupEngine>`~~ ✅ **DONE** — `GetRequiredService<T>()`. |
| C-V25 | `BackupProgressDisplay.cs:100` | ~~`MarkRemainingCompletedTasksAsInactive`~~ ✅ **FIXED** — slettet. |
| C-V10 | `BackupConsoleCommand4.cs:70–91` | ~~`BackupResult? result = null` + `ThrowIfNull`~~ ✅ **FIXED** — `null!`, `ThrowIfNull` removed. |
| C-V11 | `BackupConsoleCommand4.cs:94–98` | ~~DRY result-tælling~~ ✅ **FIXED** — `BackupResultCounts` record. |
| C-V12 | `BackupConsoleCommand4.cs:49–52` | ~~Tre-trins logger~~ ✅ **FIXED** — `GetRequiredService<ILogger<T>>()`. |
| C-V13 | `BackupConsoleCommand4.cs:68` | ~~Static `AnsiConsole.Console`~~ ✅ **FIXED** — DI `IAnsiConsole`. |
| C-V14 | `BackupConsoleCommand4.cs:121–124` | ~~`ExecuteAsyncForTests` public~~ ✅ **FIXED** — `internal`. |
| C-V15 | `BackupConsoleCommand4.cs:17–34` | Private constructor — ⚠️ **WONTFIX** (bevidst pattern). |
| C-V16 | `BackupOptionsModel4.cs:155–162` | ~~CLI --delay override~~ ✅ **FIXED** — `WasSupplied(DelayOption)`. |
| C-V17 | `BackupOptionsModel4.cs + Helpers.cs:183/33` | ~~Default mismatch~~ ✅ **FIXED** — begge `None`. |
| C-V18 | `BackupOptionsModel4.cs:20` | `ConfigOptionResult` — ✅ **RETAINED** med future-kommentar. |
| C-V19 | `BackupOptionsModel4.cs:192–194` | `DoAddValidators()` — ✅ **RETAINED** med future-kommentar. |
| C-V24 | `ConsolesPrinter.cs + BackupConsoleCommand4.cs` | ~~DRY result-tælling~~ ✅ **FIXED** — af C-V11. |
| C-V21 | `BackupPlan4Config.cs` + `BackupPlanBuilder.cs` | `ParseEnum<T>()` forbedret — bruger `KebabCaseToPascalCase()` + `Enum.TryParse` i stedet for string-strip/loop. Config-modellen forbliver `string` (KISS — ingen converters på tværs af JSON/TOML). |
| Refactor | `BackupConsoleCommand4.cs` + `BackupProgressDisplay.cs` | `reportAction` capture, null-guard, `ActionProgress<T>` slettet. `RunAsync<TResult>` med `IProgress<BackupProgress>`. `PrintAndLogStart`/`LogResult` extracted. ValidateBackupOptions simplified. DI samlet. `EscapeMarkup()`. `sealed`. `internal` records. `BackupProgressRenderer`. |

### Core4 — nye kritiske fund (High) — 14 Jun 2026

| ID | Fil | Problem | Severity |
|----|-----|---------|----------|
| K-V38 | `SidecarRequest.cs:12` | `SourceType` bruges til at skelne `mtp://` vs `C:\` parsing af `SourceFullPath` og i `SidecarService` til conditional kommentarer. Ikke redundant. | ✅ **RETAINED** |
| K-V39 | `BackupEngine.cs:287-288,369-370,492-493` | Null-tjek ryddet: uprofessionel kommentar fjernet, 2 guards beholdt. Ingen `!`. | ✅ **FIXED** |
| K-V40 | `BackupEngine.cs:82-627` | `RunAsync` er ~545 linjer. `foreach` over `pendingRecords` (280-538) bør ekstraheres | **High** |
| K-V41 | `SidecarDocument.cs:3`, `SidecarSection.cs:3`, `SidecarProperty.cs:3` | `public` men er interne implementeringsdetaljer — skal være `internal` | **High** |
| K-V42 | `BackupEngine.cs:34,51` | `public sealed` → `internal sealed`. `IBackupEngine` forbliver `public`. | ✅ **FIXED** |
| K-V43 | `InternalsVisibleTo.cs:1-5` | 4 ubrugte `using` directives | **High** |
| K-V44 | `Models/IContent.cs:2-4` | 3 ubrugte `using` directives | **High** |

### Core4 — medium fund (14 Jun 2026)

| ID | Fil | Problem |
|----|-----|---------|
| K-V45 | `BackupMediaDriveInfo.cs:37-38,101,111` | `DeviceName` == `FriendlyName` altid — redundant property |
| K-V46 | `TempDirectoryHelper.cs:321` | `CleanupTempFiles` anden parameter altid `null` |
| K-V47 | `FileContent.cs:96-98,111` | `CancellationToken` accepteres men anvendes ikke |
| K-V48 | `BackupEngine.cs:135-136` | `.bmtp3` magic string bør være konstant |
| K-V49 | `SidecarService.cs:29-36` | Sidecar file extensions som magic strings |
| K-V50 | `BackupEngine.cs:114` | Parameter `cancellationToken` reassignes |
| K-V51 | `BackupEngine.cs:103` | `_currentProgress` er lokal variabel med felt-præfiks |
| K-V52 | `SourceTraversalFactory.cs:43-45` | `#pragma warning disable CA1416` for bred |
| K-V53 | `BackupPlanValidator.cs:80` | `StopOnError=false` kaster `FeatureNotImplementedException` |
| K-V54 | `RenameCollisionResolver.cs:286` + `HashService.cs:43` | `ToHashType` switch duplikeret |
| K-V55 | `BackupEngine.cs:751` | Unødigt null-guard i loop |
| K-V56 | `RenameCollisionResolver.cs:213,214,223` | 3 TODO-kommentarer i production |
| K-V57 | `MediaDeviceWrapper.cs:48` | TODO i production |
| K-V58 | `SignalInterruptEngine.cs:200` | TODO i production |

### Core4 — low fund (14 Jun 2026)

| ID | Fil | Problem |
|----|-----|---------|
| K-V59 | Flere filer | Exception messages mangler identifiers |
| K-V60 | `SidecarRequest.cs:9` | `Format` bør hedde `SidecarFormat` |
| K-V61 | `BackupEngine.cs:753` | `"mtp://"` magic string — bør være delt konstant |
| K-V62 | `DriveCatalogEntry.cs:13` | `SourceType` mangler `required` |
| K-V63 | `BackupSessionKeyFactory.cs:23` | `plan.SourcePath.Trim()` uden null-check |
| K-V64 | `BackupEngine.cs` diverse | Verbose step-kommentarer, double blank lines |
| K-V65 | `SidecarSection.cs:33-67` / `SidecarProperty.cs:8-24` | `WithProperty` overloads duplikerer `From` factories |
| K-V66 | `JsonSidecarWriter.cs:36,40` | `OrdinalIgnoreCase` dictionary — inkonsistent med INI writer |
| K-V67 | `RenameCollisionResolver.cs:51` | Double blank line |
| K-V68 | `ServiceCollectionExtensions.cs:136-139` | `AddIfNotNull` helper med én caller

## ItemIdScope Specification (fra MTP_PTP_ID_STRATEGIES.md)

**Tre scopes (enum `ItemIdScope`):**
- `Session` (default) — `SHA256(SessionId + SourceRelativeFilePath)`. Resumable inden for samme session, nye IDs ved ny session.
- `Connection` — `SHA256(DeviceId + SourceRelativeFilePath)`. Stabil per device connection, ændres ved genforbindelse.
- `Persistent` — `SHA256(DeviceUniqueId + SourceRelativeFilePath)`. Stabil på tværs af disconnects.

**GenerateDeviceUniqueId():** `SHA256(DeviceFriendlyName + Manufacturer + Model + SerialNumber)` → truncated to 8 chars hex.

**Device PUID behavior:** Apple-enheder returnerer unik serial per connection; Android kan returnere null. Ved null PUID → fallback til ConnectionId adfærd.

---

## DryRun Behavior Table (fra CORE4_PLAN_en.md)

| Komponent | DryRun kører? | Notes |
|-----------|--------------|-------|
| Traversal | ✅ Ja | Finder filer/items |
| Scanning | ✅ Ja | Opretter BackupItems |
| Hashing | ✅ Ja | Beregner hashes |
| Collision detection | ✅ Ja | Resolver kører fuldt |
| Disk space check | ✅ Ja | Validerer plads |
| **File transfer** | ❌ **Skip** | Ingen download/move |
| **Sidecar write** | ❌ **Skip** | Ingen sidecar filer |
| **Timestamp correction** | ❌ **Skip** | Ingen filændring |
| **Index write** | ❌ **Skip** | Ingen katalog |

---

## MTP Session Lifecycle (fra CORE4_PLAN_en.md)

```
Connect → OpenSession → Enumerate → Transfer → CloseSession → Disconnect
```

Hvert trin kan fejle med COMException (transient) → retry med backoff. Ved disconnect → genforbind + resume.

---

## Parallelism Strategy (fremtidig — fra CORE4_PLAN_en.md)

- `MaxWorkers = Environment.ProcessorCount`
- `QueueDepthLimit = MaxWorkers * 3`
- Dedicated scheduler for MTP operations (Avoid thread pool starvation)
- Collision resolution prioritet: Timestamp match → Binary comparison config check → Whole-file hash → Whole-file binary → (hvis stadig ens) skip

---

## Definition of Done (11-punkts checklist — fra CORE4_MASTER_SYNTHESIS.md)

1. ✅ Traversal (filesystem + MTP) komplet med glob patterns
2. ❌ Transfer (download + move) implementeret
3. ❌ Sidecar (INI + JSON) skrevet for alle items
4. ❌ Timestamp preservation (læs + sæt) korrekt
5. ❌ Index (JSON catalog) skrevet
6. ❌ Summary store opdateret
7. ❌ Session state persisteret og genindlæselig
8. ❌ Error paths testet (transient, persistent, fatal)
9. ❌ Cancellation (Ctrl+C) ren afbrydelse
10. ❌ Resume cross-connection (MTP ContentHash fallback)
11. ❌ Integration test med ægte I/O

---

## Context Records Design (future — fra CORE4_SKELETON_TODO.md)

Fremtidig ekstraktion af `BackupEngine.RunAsync` kan bruge context records til at sende state gennem pipelinen:
- `ScanContext` — traversal resultater, scanner options
- `TransferContext` — download state, temp files, destination
- `OptionalFeatureContext` — sidecar, index, metadata config
- `SidecarContext` — document builders, format options

`DryRunFileTransfer` — no-op implementation til dry-run mode (registreret via DI).

`BackupError` record: `BackupError(BackupItemId ItemId, BackupErrorStage Stage, string Message, Exception? Exception)`.

---

## Hash Performance (fra CORE4_ARCHITECTURE_en.md)

| Algoritme | Relativ hastighed | Notes |
|-----------|------------------|-------|
| BLAKE3 | ~15x SHA2-256 | Hurtigst, ikke standardiseret |
| SHA2-256 | 1x (baseline) | Standard, langsom på store filer |
| SHA3-256 | ~0.5x SHA2 | Langsommere, nyere standard |

BLAKE3 er **ikke implementeret** i Core4 — kun SHA2/SHA3 familier. Notér til fremtidig optimering.

---

## Ref

- `mangler.md` — issues and remaining work
