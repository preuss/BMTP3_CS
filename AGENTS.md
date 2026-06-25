# BMTP3 — Agent Session Context

> **Sidst opdateret:** 25 Jun 2026
> **Tests:** 1668/1668 passed (Core4: 932, MessageFormatter: 351, Common: 281, Consoles: 104)
> **Build:** 0 errors, 0 warnings (Core4), ~160 warnings (Consoles/archived/third-party)
> **Docs:** 24 forældede slettet, værdi merget ind i plan.md / AGENTS.md / mangler.md
> **Bugs:** 9 gennemgået (5 fikset, 4 ikke-bugs). Dybdeanalyse: B1–B10, B13 fikset, B11 afkræftet — 2 MEDIUM, 3 LOW tilbage

---

## Arkitektur (Core4 + Consoles)

- **Core4** (`BMTP3.Core4/`): Backup engine — traversal, download, hash, collision, sidecar, metadata. 0 warnings. **Dette er den kritiske kode.**
- **Synchronous progress** (Core4): `TransformProgress<TInner,TOuter>` (transform), `ActionProgress<T>` (action/side-effect) — erstatning for `Progress<T>` som aldrig dispatcher via ThreadPool.
- **Consoles** (`BMTP3.Consoles/`): CLI commands, progress display, config loading, DI setup. 4 warnings (CS8604 i Core2-archived kode, alle ignoreret). **Ikke kritisk — "if it ain't broke don't fix it".**
- **Core/Core2/Core3**: Archived/readonly — må ikke redigeres. Dette gælder ALLE filer i Core2/Core3-flowet, uanset hvilket projekt de ligger i. Det inkluderer `BackupConsoleCommand2.cs`, `BackupConsoleCommand3.cs`, `ConsolesPrinter` (Core2/Core3-metoder), og alle filer i `BMTP3.Core2/`, `BMTP3.Core3/`, `BMTP3.Core/`.
- **Consoles** (`BMTP3.Consoles/`): Core4-specifikke CLI commands, progress display, config loading, DI setup.
- **Config**: TOML (kebab-case), JSON (camelCase case-insensitive), JSON5 (unquoted keys, kommentarer, trailing commas). Config loads først; `WasSupplied()` overrider.

### Progress Display Design

- `BackupProgressDisplay` med `RunAsync<TResult>(string, Func<IProgress<BackupProgress>, Task<TResult>>)`
- `BackupProgressDisplay` med `RunAsync<TResult>(string, Func<IProgress<BackupProgress>, Task<TResult>>)`
- Interne: `BackupProgressRenderer`, `ProgressReport` (`internal sealed record`), `ProgressReportMapper`
- `WriteDebugLine(ProgressReport, IAnsiConsole)` — caller tjekker `_debug` før kald
- **Sequential progress contract** — `IBackupEngine.RunAsync` XML-doc kræver serial progress (display er ikke thread-safe)
- `Progress<T>` bruges KUN i Consoles (UI-lag) — Core4 bruger `TransformProgress<TInner,TOuter>` og `ActionProgress<T>`

### Download Pipeline

- **PipelinedDownloadService** (`IDownloadService`): Producer/consumer Channel pipeline — ReadAsync/WriteAsync overlapper via `Channel<BufferChunk>`.
  - Buffer: 2MB / Queue: 2 buffers (~4MB read-ahead)
  - `ArrayPool<byte>.Shared.Rent()` for buffer reuse
  - `PreallocationSize` seed for NTFS fragmenteringsminimering
  - `SingleReader=true, SingleWriter=true` — MTP/PTP-safe (kun én reader ad gangen)
  - File timestamps sættes efter stream Dispose (korrekt rækkefølge)

### Hashing Pipeline

- **ParallelStreamHashGenerator** (`IHashGenerator`): Parallel.ForEach over hash-algoritmer — alle N algoritmer processerer samme chunk på forskellige CPU cores.
  - Buffer: 8MB via `ArrayPool<byte>.Shared.Rent()`
  - `MaxDegreeOfParallelism = max(2, ProcessorCount / 4)` — konservativt for hyper-threading
  - Throttler fjernet fra hot loop (styres af BackupEngine per-item)
  - `PooledStreamHashGenerator` (4MB, sequential, med throttler) som fallback — kommenteret i DI

### Folder Layering (Api → Models → Engine → Scanner → Helpers)

```
Api/         → Public interfaces (IBackupEngine, IFileSystemPathResolver)
Models/      → Domain records (BackupPlan, BackupItem, BackupResult)
Engine/      → Orchestration (BackupEngine, Sidecar, Download, Compare, Session)
Scanner/     → Scanning (BackupScanner)
Helpers/     → Utilities (PathHelper, Guard, TransformProgress)
```

- `BackupItem` er en **class** (ikke record) — properties populeres inkrementelt gennem pipelinen (nogle af Scanner, andre af Engine). En record ville kræve builder pattern.
- `CollisionStreategy.cs` — stavefejl i filnavn. **Do not fix.** Eksisterer i både docs og kode; rename ville give kaskaderende ændringer.

### BaseOptionsModel (omskrevet 16 Jun 2026)

- Ingen delegates der fanger `this` i cache
- `ModelDefinition` + `OptionBinding` record — binder `Option<T>` til instance property
- `EnsureOptionsDefined()` med `Lazy<ModelDefinition>` + `LazyThreadSafetyMode.ExecutionAndPublication`
- `Action<BaseOptionsModel, ParseResult>` binder-signatur — korrekt instance binding
- `DoAddValidators()` kaldes i Lazy factory (én gang per type — ikke i `ApplyOptions`)
- `DoPopulate()` bruger cached definition
- `SetPropertyValue` med `IsInstanceOfType` for nullable value types
- `_getValueOpenMethod` cached som static
- `TryGetOptionValueType`, `FormatTypeName`, `FormatValue` som rene helpers
- `ValidateNoDuplicateOptionInstances` + `ValidateNoDuplicateNamesOrAliases`

---

## Arkitekturregler (må aldrig brydes)

1. **BackupItem.Content må aldrig være null under traversal** — Scanner sætter altid Content.
2. **Session state skal persisteres atomisk** — `SessionStateService.SaveAsync` i `finally` block.
3. **Progress må aldrig dispatche async** — brug `TransformProgress<TInner,TOuter>` og `ActionProgress<T>`, aldrig `Progress<T>` i Core4.

## Error Taxonomy

| Type | Eksempel | Håndtering |
|------|----------|-----------|
| **Transient** | IOException (disk full), COMException (MTP disconnect) | Retry med backoff |
| **Persistent** | FileNotFoundException, UnauthorizedAccessException | Fail item, log, stop hvis `StopOnError` |
| **Fatal** | OutOfMemoryException, engine invariant broken | Kast straks, stop engine |
| **Optional** | Sidecar write fejl, index update fejl | Log warning, fortsæt |

## Doc Conflict Resolution

Hierarki (fra `CORE4_IMPLEMENTATION_GUIDE_DA.md`):
1. **Koden vinder** — hvis kode og doc siger forskelligt, gælder koden
2. **plan.md** — aktiv plan, næste opgaver, beslutninger
3. **mangler.md** — issues, test huller, kosmetiske fund
4. **AGENTS.md** — session context, arkitektur, regler
5. **Slettede docs** — historisk reference i git history

Normaliseringsregler:
- Hvis spec og guide siger forskelligt → spec vinder
- Hvis engelsk og dansk doc siger forskelligt → dansk vinder (primært sprog)
- Hvis gammel og ny doc siger forskelligt → ny vinder
- Hvis abstrakt og konkret siger forskelligt → konkret vinder (kode tæller som konkret)
- Hvis kode og doc siger forskelligt → koden vinder altid

## Core4 Design Principles

| Princip | Anvendelse |
|---------|-----------|
| **Fail-first** | Traversal/engine kaster exception — aldrig `yield break`, `return`, `continue` |
| **KISS** | Sequential pipeline — undtaget PipelinedDownloadService (Channel er nødvendig for MTP-safe I/O overlap). `BackupPlan4Config.cs` DTO beholdes (manuel mapping er eksplicit) |
| **YAGNI** | `MaxDegreeOfParallelism` postponed, custom output pattern postponed, `EnableMetadata` fjernet |
| **DRY** | `TransformProgress<T>` genbruges til scanning + download. `ParseEnum<T>` central |
| **Progress determinisme** | `Progress<T>` forbudt i Core4. Kun synkrone wrappers |
| **Forensic evidence** | Temp-filer ved fejl bevares (slettes ikke blindt) |

---

## Vigtige beslutninger

| Beslutning | Status |
|---|---|
| `DoAddValidators()` — beholdt med placeholder-kommentar | ✅ RETAINED |
| `ConfigOptionResult` — beholdt med future-kommentar | ✅ RETAINED |
| `Progress<T>` fjernet fra Core4 — `TransformProgress<TInner,TOuter>`/`ActionProgress<T>` i stedet | ✅ DONE |
| Parameterorden: vigtige params først, infrastructure (`IAnsiConsole`) før config (`bool debug`) | ✅ Regel |
| Fail-first i CLI — returner error code + message, kast aldrig i command handler | ✅ Regel |
| Fail-first i traversal/engine — kast exception ved fejl (aldrig `yield break`, `return`, `continue`) | ✅ Regel |
| `BackupPlan4Config.cs` (DTO med nested config-klasser) — beholdes, manuel mapping er eksplicit og testbar. Consoles er ikke kritisk. | ✅ RETAINED |
| Core/Core2/Core3 archived — ALLE filer i Core2/Core3-flowet (uanset projekt) er readonly | ✅ Regel |
| `private` constructor/`_optionsModels` i `BaseConsoleCommand` | ⚠️ WONTFIX |

---

## Session State

### Done (seneste session — 25 Jun 2026)

| ID | Hvad | Fil(er) |
|---|---|---|
| Fix | **B7** — `StatusChangedAt` sat efter Skipped, Succeeded, Failed | `BackupEngine.cs:437,532,561` |
| Fix | **B8** — `LoadAsync` try-catch med typed exceptions (fail-first) | `BackupJsonSummaryStore.cs:43-57` |
| Feat | **B8b** — `LoadAsync` async pipeline (`File.OpenRead` + `DeserializeAsync`) | `BackupJsonSummaryStore.cs:45-46` |
| Fix | **B9** — `Progress<BackupScanProgress>` → `ActionProgress<BackupScanProgress>` | `BackupEngine.cs:210` |
| Fix | **B10** — `ct.ThrowIfCancellationRequested()` i `OpenReadAsync` | `FileContent.cs:113` |
| Fix | **B13** — `ArrayPool.Return` med `clearArray:true` | `ParallelStreamHashGenerator.cs:129`, `PooledStreamHashGenerator.cs:117` |
| Audit | **B11** — ❌ AFKRÆFTET: `CancellationToken.None` i `finally` er designvalg | `BackupEngine.cs:576` |
| Audit | **OCE catch** — `Pending` er korrekt status for cancelled items (resume prøver igen) | `BackupEngine.cs:541-548` |
| Doc | mangler.md opdateret med sessionens rettelser | `mangler.md` |
| Doc | AGENTS.md opdateret med session context | `AGENTS.md` |

### Done (23 Jun 2026)

| ID | Hvad | Fil(er) |
|---|---|---|
| Test | `BackupPlanValidatorTests` — 45 tests: null/empty paths, invalid enums, hash+algorithms, backslash patterns, feature gate, happy paths | `BackupPlanValidatorTests.cs` |
| Test | `BinaryFileComparerSelectorTests` — 10 tests: constructor null guards, Select guards, small files → WholeFile, large files → chunked | `BinaryFileComparerSelectorTests.cs` |
| Test | `BackupEngineErrorPathTests` — 10 tests: Download, TargetPathResolver, Sidecar, HashService, TS resolution failures (StopOnError true/false); mixed success; empty/no-matching drive | `BackupEngineErrorPathTests.cs` |
| Feat | `TransformProgress<TInner,TOuter>` — synkron IProgress&lt;T&gt; transform wrapper | `Helpers/TransformProgress.cs` |
| Feat | `ActionProgress<T>` — synkron IProgress&lt;T&gt; action wrapper | `Helpers/ActionProgress.cs` |
| Feat | `ProgressExtensions.Transform()` — extension method for TransformProgress | `Helpers/TransformProgress.cs` |
| Feat | `ActionProgressExtensions.ToProgress()` — extension method for ActionProgress | `Helpers/ActionProgress.cs` |
| Fix | `BackupScanner.cs` — `new Progress<T>(...)` → `TransformProgress<TInner,TOuter>` | `BackupScanner.cs` |
| Fix | `BackupEngine.cs:304,356` — `new Progress<ulong>(...)` → `ActionProgress<ulong>` | `BackupEngine.cs` |
| Fix | `BackupScannerTests` — `CountingProgress<T>` (deterministisk, ingen race) | `BackupScannerTests.cs` |
| C-V26 | `WriteDebugLine` fixed: `(ProgressReport, IAnsiConsole)`, caller tjekker `_debug` | `BackupProgressDisplay.cs` |
| C-V30/V31 | `BaseOptionsModel.cs` omskrevet: `ModelDefinition`+`OptionBinding`, Lazy cache, ingen `this`-capture | `BaseOptionsModel.cs` |
| K-V47 | `FileContent.OpenReadAsync`: `CancellationToken` tjekket før `FileStream` | `FileContent.cs` |
| Smelly #5 | `ProgressStatusTask.cs:43`: `Value = Value` → `Value = value` | `ProgressStatusTask.cs` |
| Fix | `KebabCaseToPascalCase` understøtter nu `_` (underscore) — fixture `preserve_hierarchy` | `JsonNamingPolicies.cs` |
| C-V23 | `ConsolesPrinter` split: `ConsolesPrinter4.cs`, `ConsolesPrinter2.cs`, `ConsolesPrinter3.cs` oprettet, DI registreret, `BackupConsoleCommand4.cs` updated | `ConsolesServiceSetup.cs`, `BackupConsoleCommand4.cs` |
| Fix | `BaseOptionsModel.cs:225` CS8604 — `ArgumentNullException.ThrowIfNull(valueType)` | `BaseOptionsModel.cs` |
| Smelly #10 | `BackupPlan4Config.cs` — ✅ RETAINED. Eksplicit DTO + manuel mapping beholdes. Consoles er ikke kritisk; Core4/BackupEngine er vigtigst. | `BackupPlan4Config.cs` |
| Tests | 1064/1064 passed | — |
| Rename | `ItemIdStrategy` → `ItemIdScope` — enum, properties, CLI option, config property, file rename | Alle 14 filer |
| Remove | `EnableMetadata` removed — property, validator gate, config DTO, builder, CLI template, test data/assertions | 14 filer på tværs af Core4 + Consoles + tests |
| Gate | `StopOnError=false` Tier 3 gate removed — engine implementation var allerede på plads; validator blokerede unødigt | `BackupPlanValidator.cs:75-77` |
| Fix | SidecarService bug — manglende `SHA3_256_FIPS202`/`SHA3_256_KECCAK` i `allHashTypes` | `SidecarService.cs:128-137` |
| Add | CLI aliases for KECCAK hash varianter — `keccak-256`, `keccak-512`, `sha3-256-keccak`, `sha3-512-keccak` | `BackupPlanBuilder.cs:265-266` |
| Test | Reelle `StreamHashGenerator` tests i Core4 — alle 9 algoritmer med reelle hash-implementationer | `StreamHashGeneratorTests.cs` (7 tests) |
| Fix V2 | All 25 runtime test failures fixed — 11 categories across Guard, PathHelper, DiskSpaceValidator, SourceConnector, FileFormatValuesFactory, DownloadService, FileCompareService, JsonBackupIndexWriter, TargetPathResolver, TempDirectoryHelper, progress race tests | Se nedenfor |
| Doc | `NormalizePath` XML-doc advarsel: "pure separator normalizer — validerer ikke `:`, `..`, tomme stier" | `PathHelper.cs` |
| Feat | `IniSidecarWriterOptions.WriteComments` (default `false`), class-level `<remarks>` på `PathHelper` | `IniSidecarWriterOptions.cs`, `IniSidecarWriter.cs`, `PathHelper.cs` |
| Fix | Session ID kollision på tværs af plans — inkluderer `Destination` i source identity | `BackupSessionKeyFactory.cs:27-30`, `BackupSessionKeyFactoryTests.cs` |
| Docs | 24 forældede docs slettet. Værdi ekstraheret: ItemIdScope spec, DryRun table, MTP lifecycle, definition of done, context records, hash perf, error taxonomy, doc conflict resolution, architekturregler, design principles | `plan.md`, `AGENTS.md`, `mangler.md` opdateret |
| Test | TimeStamp candidate tests — 198 tests (7 filer): TimestampSources (14), TimestampFormatStyleParser (8), TimestampFormatDescriptor (18), TimestampFormatter (22), TimestampCandidate (14), TimestampCandidateFactory (34), Parsed (2) | `BMTP3.Core4.Tests/Engine/TimeStamp/Candidates/` |
| Test | TimeStamp parser tests — 284 tests (10 filer) for alle 14 parser-klasser | `BMTP3.Core4.Tests/Engine/TimeStamp/Parsers/` |
| Fix | 15 failing timestamp tests: IsAllNull (empty/whitespace ≠ null), Normalize (returnerer ny instans med `with`), Full clock offset inkluderer sekunder, Formatter vs candidate.ToString, Factory.FromUtcDateAndTime bug (FullDate når date er null) | Se testfiler |
| Fix | 2 remaining failing tests: Format_InvalidCandidate → subSeconds uden Time, ToDebugString da-DK kulturformat | `TimestampCandidateTests.cs`, `TimestampFormatterTests.cs` |
| Test | DownloadService error paths — Content OpenReadAsync kaster, pre-cancelled token | `DownloadServiceTests.cs` (2 tests) |
| Test | HashService error path — null stream fra content wrappes i BackupHashException | `HashServiceTests.cs` (1 test) |
| Audit | **Field mapping audit** — systematisk gennemgang af alle property mappings i Core4/Consoles. Fund: Resume mister ikke datoer (scan-før-resume er korrekt), session state gemmer ikke hashes (designvalg), `EnableTimestampCorrection` mangler på CLI | Se `plan.md § P0` |
| Feat | `--enable-timestamp-correction` CLI option tilføjet — Option<bool> + WasSupplied check | `BackupOptionsModel4.cs`, `BackupPlanBuilder.cs` |
| Fix | **Bug #1**: `SafeGetFiles`/`SafeGetDirectories` — silent catch{} fjernet (fail-first) | `FileSystemTraversal.cs` |
| Fix | `SafeGetDate` — catch{} → specifikke exception typer (UnauthorizedAccessException, IOException, NotSupportedException) | `FileSystemTraversal.cs` |
| Test | +2 error path tests: TraversalFailure_FailFast, ScanPhaseCancellation_ReturnsCancelledResult | `BackupEngineErrorPathTests.cs` |
| Fix | `JsonBackupIndexWriterTests` — `.bmpt` → `.bmtp3` stavefejl (5 tests fixed) | `JsonBackupIndexWriterTests.cs` |
| Audit | **Bug #3 re-evalueret:** `BinaryFileComparerBase.CompareAsync` - `!Exists && !Exists → true` er **korrekt** for en generisk comparer (begge mangler = samme tilstand). Callers har egne existence guards. Fjernet fra bugs. | `BinaryFileComparerBase.cs` |
| Fix | **Bug #4**: `(IMoveableContent)` → `is not IMoveableContent` pattern match med `InvalidOperationException` | `BackupEngine.cs:461` |
| Fix | **Bug #5**: `Source.Type` læses nu via `ParseEnum<BackupSourceType>()`, `--source-type` CLI option tilføjet, path detection fjernet fra config-flow | `BackupPlanBuilder.cs:83-86,138-144`, `BackupOptionsModel4.cs:45-50` |
| Fix | **Bug #6**: `ToUtcOffsetOrNull` — `DateTimeKind.Local` case forenklet til `new DateTimeOffset(dateTime).ToUniversalTime()`. `Unspecified` → `Local` er korrekt (bedste gæt). | `MediaDeviceTraversal.cs:223` |
| Fix | **Bug #8**: Default mismatch — `EnableTimestampCorrection` og `StopOnError` sat til `= true` i `BackupPlan` som matcher builder | `BackupPlan.cs:154,170` |
| Audit | **Bug #2/#7/#9 re-evalueret:** Ikke-bugs — alle 9 bugs gennemgået, 0 tilbage | Se mangler.md |
| Fix | **SourceType nullable:** `BackupPlan.SourceType` → `BackupSourceType?`. Validator tjekker null — fanger glemt `--source-type`. | `BackupPlan.cs:29`, `BackupPlanValidator.cs:37-42`, `BackupEngine.cs:159,477`, `BackupSessionKeyFactory.cs:31` |
| Feat | **PipelinedDownloadService** — Channel producer/consumer, 2MB buffer, ArrayPool, PreallocationSize, timestamps efter Dispose | `Engine/Downloader/PipelinedDownloadService.cs` |
| Feat | **ParallelStreamHashGenerator** — 8MB buffer, Parallel.ForEach over algoritmer, ArrayPool, konservativ DOP | `Hashing/ParallelStreamHashGenerator.cs` |
| Feat | **PooledStreamHashGenerator** — 4MB buffer, ArrayPool, sequential (fallback) | `Hashing/PooledStreamHashGenerator.cs` |
| Fix | **Throttler** fjernet fra StreamHashGenerator hot loop (var kaldt per 80KB buffer) | `Hashing/StreamHashGenerator.cs:78` |
| Fix | **Throttler** kommenteret ud i ParallelStreamHashGenerator — styres af BackupEngine | `Hashing/ParallelStreamHashGenerator.cs:96` |
| DI | `IHashGenerator` → `ParallelStreamHashGenerator`, `IDownloadService` → `PipelinedDownloadService` | `DependencyInjection/ServiceCollectionExtensions.cs:31,39` |
| Test | Download buffer benchmark — 80KB→16MB→128MB, File.Copy reference. Sweet spot: 256KB-16MB | `PlayAroundProject/Program.cs` |
| Audit | **Bug #9 re-evalueret (dybdeanalyse):** `createFileDate` er nødvendig for path + collision resolution uanset `EnableTimestampCorrection` — throw + per-item catch er korrekt fail-first. ❌ Ikke-bug. | `mangler.md`, `plan.md` |
| Fix | **Bug #2 — `FilterPendingRecords`:** silent `break` på `Active` → `throw new UnreachableException()`. `default:` guard tilføjet mod fremtidige enum-værdier. | `BackupEngine.cs` |
| Fix | **#13 — MediaDeviceContent resource leak:** try-catch i `GatekeptStream` konstruktør — `inner?.Dispose()` + `lease?.Dispose()` ved fejl (allerede fikset af bruger) | `GatekeptStream.cs` |

### In Progress

*(ingen)*

### Næste — prioriteret

0. **🔴 Latente bugs — ingen tilbage** ✅. Se `mangler.md § 🔴 Bugs (latente fejl)` og `§ ⚠️ Mistænkelige`
1. **🔴 Dybdeanalyse bugs**: B1–B10 ✅ **ALLE FIXET**, B11 afkræftet. Resterer: 2 MEDIUM (B14-B15), 3 LOW (B16-B18). Se `mangler.md § 🔴 Bugs (dybdeanalyse 25 Jun 2026)`
2. **Feature gates**: `MaxDegreeOfParallelism`, `BackupIndexType.Database`
3. **Integration tests**: MTP pipeline, BackupEngine E2E
4. **Retry/Resilience**: Exponential backoff, MTP resilience
5. **2B error paths**: DownloadService cancellation/locks, HashService null stream — ✅ **FIXET**. Resterer: destination locked (kræver I/O), store filer (I/O), TempDirectoryHelper (I/O), SessionStateService (I/O)
6. **2A TimeStamp**: Readers (13 files) + `EarliestTimestampResolutionService` — kræver reelle filer med EXIF/XMP metadata

## Code Quality Audit — Status

### Consoles — High

| ID | Status | Fil | Problem |
|---|---|---|---|
| C-V01–C-V14 | ✅ FIXED | Diverse | BuildPlan split, ParseEnum, fail-fast, DI, result counts, logger, progress display |
| C-V15 | ⚠️ WONTFIX | `BackupConsoleCommand4.cs` | Private constructor pattern |
| C-V16 | ✅ FIXED | `BackupOptionsModel4.cs` | `WasSupplied(DelayOption)` |
| C-V17 | ✅ FIXED | `BackupOptionsModel4.cs` | Default mismatch `Hash`/`None` |
| C-V18 | ✅ RETAINED | `BackupOptionsModel4.cs:22` | `ConfigOptionResult` med future-kommentar |
| C-V19 | ✅ RETAINED | `BackupOptionsModel4.cs:220–223` | `DoAddValidators()` — extension point til cross-option validering |
| C-V20 | ✅ FIXED | — | `ExecutionConfig` slettet |
| C-V21 | ✅ DONE | `BackupPlanBuilder.cs` | `ParseEnum<T>` forbedret (kebab-case + underscore → PascalCase) |
| C-V22 | ✅ DONE | `BackupPlanLoader` → `BackupPlan2Loader` + archived header | Renamed to show Core2 ownership |
| C-V23 | ✅ DONE | `ConsolesPrinter4.cs`, `ConsolesPrinter2.cs`, `ConsolesPrinter3.cs`, `ConsolesServiceSetup.cs` | Split Core4 printing |
| C-V24 | ✅ FIXED | — | Af C-V11 |
| C-V25 | ✅ FIXED | `BackupProgressDisplay.cs` | `MarkRemainingCompletedTasksAsInactive` slettet |
| C-V26 | ✅ DONE | `BackupProgressDisplay.cs` | `WriteDebugLine` parameter fix |
| C-V28 | ✅ FIXED | `BackupConsoleCommand4ListDrives.cs` | `ServiceProvider` null guard |
| C-V29 | ✅ FIXED | `BackupConsoleCommand4ListDrives.cs` | `Task.FromResult(0)` → `return 0` |

### Core4 — High

| ID | Status | Fil | Problem |
|---|---|---|---|
| K-V38 | ✅ RETAINED | `SidecarRequest.cs:12` | `SourceType` bruges til at skelne `mtp://` vs `C:\` parsing af `SourceFullPath` og i `SidecarService` til conditional kommentarer. Ikke redundant. |
| K-V39 | ✅ DONE | `BackupEngine.cs` | Null-tjek ryddet: uprofessionel kommentar fjernet, 2 guards beholdt (1 i try-scope, 1 i nested if-block). Ingen `!` operator. |
| K-V40 | ⏳ LOW | `BackupEngine.cs:82–627` | `RunAsync` for lang (~545 linjer) — extract hvis tid |
| K-V41 | ✅ DONE | `SidecarDocument.cs`, `SidecarSection.cs`, `SidecarProperty.cs` | `public` → `internal` |
| K-V42 | ✅ DONE | `BackupEngine.cs:34,51` | `public sealed` → `internal sealed`. `IBackupEngine` forbliver `public`. |
| K-V43 | ✅ DONE | `InternalsVisibleTo.cs` | 4 ubrugte `using` fjernet |
| K-V44 | ✅ DONE | `Models/IContent.cs` | 3 ubrugte `using` fjernet |
| K-V47 | ✅ DONE | `FileContent.cs:96–98,111` | `CancellationToken` tjek før `FileStream` |

---

## Warnings (4 Consoles — Core2 archived)

| Linje | Warning | Scope |
|---|---|---|
| `BackupConsoleCommand2.cs:46,145,212` | CS8604 — `ConsolesPrinter` null reference | Archived — ignoreres |
| `BackupConsoleCommand3.cs:45` | CS8604 — `ConsolesPrinter` null reference | Archived — ignoreres |

---

## Relevante filer (ændret i seneste session)

| Fil | Ændring |
|---|---|
| `BMTP3.Core4/Engine/BackupEngine.cs` | B7 — `StatusChangedAt` sat efter Skipped, Succeeded, Failed. B9 — `Progress<T>` → `ActionProgress<T>`. |
| `BMTP3.Core4/State/BackupJsonSummaryStore.cs` | B8/B8b — `LoadAsync` try-catch med typed exceptions + async pipeline |
| `BMTP3.Core4/Models/FileContent.cs` | B10 — `ct.ThrowIfCancellationRequested()` før `FileStream` |
| `BMTP3.Core4/Hashing/ParallelStreamHashGenerator.cs` | B13 — `clearArray: true` tilføjet |
| `BMTP3.Core4/Hashing/PooledStreamHashGenerator.cs` | B13 — `clearArray: true` tilføjet |
| `mangler.md` | B7/B8/B9/B10/B13 → ✅ FIXET. B11 → ❌ AFKRÆFTET. Resume opdateret. |
| `AGENTS.md` | Session context opdateret for 25 Jun 2026 |

---

## Test-kørsel — vigtig regel

**Kør ALDRIG Core2- eller Core3-tests med mindre vi aktivt ændrer i disse projekter.** De er archived/readonly. Brug `dotnet test` med filter:

```
dotnet test --no-restore --filter "FullyQualifiedName!~Core2&FullyQualifiedName!~Core3"
```

Eller kør specifikke projekter:

```
dotnet test BMTP3.Common.Tests/BMTP3.Common.Tests.csproj --no-restore
dotnet test BMTP3.Core4.Tests/BMTP3.Core4.Tests.csproj --no-restore
dotnet test BMTP3.Consoles.Tests/BMTP3.Consoles.Tests.csproj --no-restore
```
