# BMTP3.Core4 — Plan

> **Opdateret 9 Jun 2026** — Gap-analyse gennemført (Core, Core2, Core3 → Core4). Nye huller identificeret: TOML config, retry/resilience, Console UI progress, ISidecarService public.
> Næste: Implementer BackupIndexType.Json, TOML config reader, Consoles CLI cleanup, Integration tests.
> 
> ⚠️ **FAIL-FIRST:** Alle gates/tjek i traversal og engine skal kaste exception ved fejl — aldrig `yield break`, `return` eller `continue` for at tie stille om problemer. Source der ikke findes = throw. Eneste undtagelse: per-item try-catch der markerer failed items men re-thrower (fail-fast).

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
- [x] — `IMtpDeviceSession` + `MtpDeviceSession` — connect/disconnect, `[SupportedOSPlatform("windows7.0")]`
- [x] — **IFileStore redesign → `IBackupDriveInfo`** — `IBackupDriveInfo` (base), `IBackupFileSystemDriveInfo`, `IBackupMediaDriveInfo` (specialized). `BackupFileSystemDriveInfo` (fail-first med `IsReady` guard, `long` i stedet for `ulong?`). `BackupMediaDriveInfo` (`MediaDevice` + `MediaDriveInfo`, `VolumeLabel`-baseret `DriveName`). Omdøbt: `FileSystemFileStore` → `BackupFileSystemDriveInfo`, `MediaDeviceFileStore` → `BackupMediaDriveInfo`.)
- [x] — **MTP test cleanup** — Fjernet 16 tests der kaldte `MediaDevice.GetDevices()` direkte (kræver real MTP device). Kun constructor null-check tests tilbage. Opgraderet til xunit.v3 3.2.2.
- [x] — **`IFileSystemSourceDiscovery`** — `FileSystemSourceDiscovery`: `DriveInfo.GetDrives()`, filter `IsReady`, return `BackupFileSystemDriveInfo[]`
- [x] — **`IMediaDeviceSourceDiscovery`** — `MediaDeviceSourceDiscovery`: connect → `GetDrives()` → disconnect, skip ghost devices (`COMException 0x802A0001`)
- [x] — **`ICombinedSourceDiscovery`** — `CombinedSourceDiscovery`: merge filesystem + MTP. Null-safe for non-Windows.
- [x] — **DI registrering:** Discovery services registreret i `ServiceCollectionExtensions`. `MediaDeviceSourceDiscovery` kun på Windows 7+.
- [x] — **Build:** 0 errors, 0 warnings. **Tests:** 249 passed.
- [x] — **Session redesign:** `IMediaDeviceSession` → `ISession`, `IConnectedSource : ISession`. Slettet gamle session-filer.
- [x] — **Factory rename:** `IConnectedSourceFactory` → `ISourceConnector`, `Create()` → `Connect()`. Slettet `ConnectedSourceFactoryCreateRequest.cs`, `FileSystemTraversalFactoryStub.cs`.
- [x] — **Source type redesign:** `IConnectedMediaDeviceSource` → `IConnectedMediaDriveSource` (både `IMediaDevice Device` + `IMediaDrive Drive`).
- [x] — **SourceConnector:** `FindMediaDeviceSource()` matcher `IMediaDrive` via `DriveName` fra `Device.Drives`.
- [x] — **SourceTraversalFactory:** `Create(IConnectedSource)` — 1 param, pattern-matching på source types, injecter `IMediaDeviceGatekeeper`.
- [x] — **MediaDeviceTraversal:** Bruger `IMediaDirectory`/`IMediaFile` — ingen NuGet typer i body.
- [x] — **IMediaFile.OpenRead()** tilføjet, `MediaDeviceContent` opdateret til at bruge `IMediaFile`.
- [x] — **BackupEngine.Create(connectedSource):** 1 arg (ingen `IBackupDriveInfo`).
- [x] — **Build:** 0 errors, 0 warnings. **Tests:** 248 passed.
- [x] — **Validator gates relaxed**: 16 af 20 `FeatureNotImplementedException` gates fjernet. Resterer: `EnableMetadata` (T3), `BackupIndexType.Json` (T3), `StopOnError=false` (T3), `BackupIndexType.Database` (T4), `MaxDegreeOfParallelism` (T4).
- [x] — **Core4 i Consoles:** `BackupConsoleCommand4.cs` + helpers, `ConsolesPrinter` opdateret med Core4 overloads, `ApplicationServiceSetup` registrerer `AddBMTP3Core4()`, `backup4` subcommand tilgængelig.
- [x] — **Drive matching fix:** `BackupEngine.MatchDrive()` bruger `StartsWith` i stedet for `Equals`. Relative sub-path extracted til `SourceTraversalRequest.SubPath`.
- [x] — **MediaDeviceTraversal sub-path:** `NavigateToSubDirectory()` navigerer gennem `IMediaDirectory.Directories` baseret på `SubPath`.
- [x] — **MtpUriParser genindsat:** Restored + tests. Parser krævet af produktion.
- [x] — **DriveCatalog API**: `IDriveCatalogService` + `DriveCatalogEntry` + `DriveCatalogService` + DI registration
- [x] — **Build:** 0 errors, 0 warnings. **Tests:** 248 passed.

## Næste opgaver (prioriteret)

### ✅ Komplet gap-analyse (Core, Core2, Core3 → Core4)

- [x] — **Gennemgang: Find alle manglende dele** — Krydsrefereret alle features fra Core, Core2 og Core3 mod Core4.
  Resultat: Feature audit indarbejdet i `mangler.md` (§ Feature Audit, linje 176-280). Nye huller føjet nedenfor.

### Høj prioritet — BackupIndexType.Json implementering

- [ ] — **`IBackupIndexWriter`** interface: metode `WriteAsync(Stream, IReadOnlyList<BackupItem>, BackupPlan, BackupResult, CancellationToken)`
- [ ] — **`JsonBackupIndexWriter`**: skriver `backup_catalog.json` med alle filer, hashes, metadata, timestamps
- [ ] — **Wire i `BackupEngine`**: efter processing loop, før result returneres
- [ ] — **DI registration**: `AddScoped<IBackupIndexWriter, JsonBackupIndexWriter>()`
- [ ] — **Fjern Tier 3 gate** for `BackupIndexType.Json` i `BackupPlanValidator` (linje 79-80)
- [ ] — **Spec**: definér JSON schema for katalog-filen (felter, struktur, eksempel)

### Høj prioritet — Public DriveCatalog API

- [x] — **DriveCatalog API**: `IDriveCatalogService` (Api/), `DriveCatalogEntry` (Api/Models/), `DriveCatalogService` (DriveDiscovery/), DI registration — **implementeret**

### Høj prioritet — Consoles CLI cleanup (før release)

- [ ] — **`Delay` / `VerificationRetryCount` / `VerificationRetryDelayMs` / `VerificationTimeoutMs` / `VerificationDeleteOnFailure`**: 
  Disse options valideres i `BackupConsoleCommand4.ValidateBackupOptions` men findes ikke i Core4's `BackupPlan`. 
  To valg: (a) tilføj properties til Core4 `BackupPlan` + implementer i engine, (b) fjern validering og ignorer options med warning.
- [ ] — **MTP sourcePath format**: `--source-device` sætter bart device navn (f.eks. "Apple iPhone"), men Core4 forventer `mtp://Apple iPhone/Internal Storage/DCIM`. 
  Løsning: konstruer `mtp://{deviceName}/{subPath}` URI i `BuildPlan` når `sourceType == MediaDevice`.
- [ ] — **`--backup-index` default**: behold `Json` (når implementeret), men sørg for at CLI ikke sender Json før writer er klar
- [ ] — **Fjern Core2 `BackupEngineOptions` config**: `ApplicationServiceSetup` linje 37 sætter Core2 options der ingen effekt har på Core4
- [ ] — **SignalInterrupt cancel-wiring**: brug `SignalInterrupt.On(Interrupt).Bind(cts).Create()` i stedet for `Console.CancelKeyPress`
- [ ] — **ConsolesPrinter progress**: vis `BytesProcessed`, `TotalFilesSelected`, `FilesSkipped` fra Core4's `BackupProgress`
- [ ] — **No tests for backup4**: tilføj tests for `BackupConsoleCommand4Helpers.BuildPlan` enum-mapping

### Høj prioritet — TOML config reader

- [ ] — **`--config` fil support**: Implementer TOML-reader (genbrug Core3's `ConfigModel`/`BackupSettingsReader` mønster)
- [ ] — **Map TOML til `BackupPlan`**: oversæt settings-felter til Core4's `BackupPlan` properties
- [ ] — **CLI integration**: `--config` option i `list-sources` og `backup4` commands
- [ ] — **Overvej**: TOML → `BackupPlan` mapping i stedet for at genoprette Core's handler-hierarki

### Høj prioritet — Retry / Resilience (især MTP)

- [ ] — **Retry strategy**: exponential backoff for transient I/O failures (download, hash, move, sidecar write)
- [ ] — **MTP resilience**: gatekeeper timeout + retry ved COMException/disconnect mid-session
- [ ] — **Overvej**: genbrug Core2's Polly `BackupResiliencePipeline` eller implementer lightweight retry

### Medium prioritet — Console UI progress

- [ ] — **ProgressBar / Spinner**: implementer visuel progress i Consoles under backup (Core havde 20+ UI-filer)
- [ ] — **`BackupProgress` integration**: vis `BytesProcessed`, `TotalFilesSelected`, `FilesSkipped` live

### Medium prioritet — Public API overvejelser

- [ ] — **`ISidecarService` public?**: var public i Core3, er `internal` i Core4. Overvej om eksterne forbrugere har brug for sidecar generation.

### Høj prioritet — Integration test

- [ ] — Integration test: Full MTP traversal pipeline (gatekeeper → connector → traversal → content)
- [ ] — Integration test: BackupEngine end-to-end (filesystem → download → hash → sidecar → verify)

### Senere

- [ ] — **`StopOnError=false`**: continue-on-error (Tier 3 gate). Per-item try-catch og Failed status findes, men `throw` på linje 485 forhindrer continuation.
- [ ] — **`BackupIndexType.Database`**: SQLite catalog (feature guard allerede på plads, linje 83-84)
- [ ] — **`EnableMetadata`**: metadata extraction (Tier 3)
- [ ] — **`MaxDegreeOfParallelism`**: parallel execution (Tier 4)
- [ ] — **Hash algorithm CLI options**: expose comparison/verification hash valg
- [ ] — **Overvej**: Erstat Core2 `backup` med Core4 som default

### Arkitekturforskelle (ikke 1:1 — bevidste valg)

| Core / Core2 feature | Core4 status | Begrundelse |
|---|---|---|
| Handler-hierarki (IBackupHandler, IDriveHandler) | ➡️ Samlet i BackupEngine | Enklere arkitektur; engine dispatcher selv |
| Pipeline stages (8 step-klasser med kanaler) | ➡️ Strategi-baseret loop | Mindre kompleksitet, færre allocations |
| State machines (JobStateMachine, ItemStateMachine) | ➡️ Inline status i record | State machines overkill for sekventielt flow |
| ScannerGatherer / IFileScanner / IMediaFileScanner | ⚠️ Core4 har FileSystemTraversal | Dækker filesystem; MTP traversal via MediaDeviceTraversal |

## Ref

- `Backup_Pipeline_Comparison_003.md` — comprehensive gap analysis (supersedes 001 and 002)
- `mangler.md` — issues and remaining work
