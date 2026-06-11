# BMTP3.Core4 — Plan

> **Opdateret 11 Jun 2026** — Spectre Console progress redesign ✅. Custom columns/spinners kopieret til Consoles. Core/Core2/Core3 officielt archived/readonly.
> 
> ⚠️ **NO IMPLEMENTATION WITHOUT PERMISSION:** Spørg altid først. Implementér aldrig før brugeren siger "go" / "do it" / "implementér" / "execute" / "kør". Indtil da: research, read, grep, spørg.
> 
> ⚠️ **FAIL-FIRST:** Alle gates/tjek i traversal og engine skal kaste exception ved fejl — aldrig `yield break`, `return` eller `continue` for at tie stille om problemer. Source der ikke findes = throw. Eneste undtagelse: per-item try-catch der markerer failed items men re-thrower (fail-fast).
> 
> ⚠️ **NO CORE/CORE2/CORE3 CHANGES:** `BMTP3.Core`, `BMTP3.Core2`, `BMTP3.Core3` og deres Consoles commands (`BackupConsoleCommand.cs`, `BackupConsoleCommand2.cs`, `BackupConsoleCommand3.cs`) er **archived/readonly** — de ændres aldrig. Kun `BMTP3.Core4` og `BMTP3.Consoles` må redigeres.
> 
> ⚠️ **PATH NAMING STANDARD:** Se `## Path Naming Standard` nedenfor.

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
- [x] — `IMtpGatekeeper` + `MtpGatekeeper` — `Func<CancellationToken, Task<T>>`, `AcquireAsync(TimeSpan, ...)`, `ThrowIfDisposed`, `Interlocked` dispose. 9 tests.
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

## Næste opgaver (prioriteret)

### 1. 🥇 Consoles CLI cleanup + BackupPlan Delay

| # | Task | Status |
|---|------|--------|
| 1 | **Tilføj `Delay` til `BackupPlan`** — `int` property: `< 0` = disabled, `0` = 0ms, `> 0` = N ms. Validér i `BackupPlanValidator`. `BackupDelay` struct i `Helpers/`. Implementér i `BackupEngine` processing loop (efter hvert item). `--delay` CLI option. | ✅ |
| 2 | MTP sourcePath format — `--source-path` CLI, prefix-detect i BuildPlan | ✅ |
| 3 | `--backup-index` default guard — `IBackupIndexWriter`/`JsonBackupIndexWriter` implementeret | ✅ |
| 4 | Core2 `BackupEngineOptions` config — udkommenteret, død for Core4 | ✅ |
| 5 | SignalInterrupt cancel-wiring — `Console.CancelKeyPress` fjernet, engine styrer selv | ✅ |
| 6 | Tests for backup4 BuildPlan enum-mapping | ❌ |

### 2. Integrér sidste wrapper-holdere (småopgave)

| # | File | Nuværende | Skal ændres til |
|---|------|-----------|-----------------|
| 1 | `MediaDeviceDriveProvider` | `MediaDevice` / `MediaDriveInfo` | `MediaDeviceInfo` / `MediaDrive` |
| 2 | `BackupMediaDriveInfo` | `(MediaDevice, MediaDriveInfo)` | `(IMediaDeviceInfo, IMediaDrive)` |

### 4. TOML config reader

- Genbrug Core3's ConfigModel/BackupSettingsReader
- CLI `--config` option + merge med CLI args

### 5. Retry / Resilience (især MTP)

- Exponential backoff helper
- MTP resilience (COMException, disconnect)
- Lightweight retry uden Polly dependency

### 6. Integration test

- MTP traversal pipeline test (mock via NSubstitute)
- BackupEngine end-to-end test
### 8. Public API overvejelser

- `ISidecarService` public? → ✅ **WONTFIX** — Forbliver `internal`. Core4 bruges internt via `BackupEngine.RunAsync()`. Ingen eksterne forbrugere har brug for direkte sidecar-generering. Sidecar-funktionalitet eksponeres via `BackupPlan.SidecarFormat` og kører automatisk i engine.

### 9. Senere (feature gates + default)

- `StopOnError=false` (T3), `BackupIndexType.Database` (T4), `EnableMetadata` (T3), `MaxDegreeOfParallelism` (T4)
- Hash algorithm CLI options
- Erstat Core2 `backup` med Core4 som default

### Arkitekturforskelle (ikke 1:1 — bevidste valg)

| Core / Core2 feature | Core4 status | Begrundelse |
|---|---|---|
| Handler-hierarki (IBackupHandler, IDriveHandler) | ➡️ Samlet i BackupEngine | Enklere arkitektur; engine dispatcher selv |
| Pipeline stages (8 step-klasser med kanaler) | ➡️ Strategi-baseret loop | Mindre kompleksitet, færre allocations |
| State machines (JobStateMachine, ItemStateMachine) | ➡️ Inline status i record | State machines overkill for sekventielt flow |
| ScannerGatherer / IFileScanner / IMediaFileScanner | ✅ Filesystem + MTP traversal | Begge dækket (`FileSystemTraversal` + `MediaDeviceTraversal`) |

## Ref

- `Backup_Pipeline_Comparison_003.md` — comprehensive gap analysis (supersedes 001 and 002)
- `mangler.md` — issues and remaining work
