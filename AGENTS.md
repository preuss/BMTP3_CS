# BMTP3 — Agent Session Context

> **Sidst opdateret:** 16 Jun 2026
> **Tests:** 104 Consoles + 139 Core4 = **243 passed**
> **Build:** 0 errors, 5 warnings (Consoles), 0 warnings (Core4)

---

## Arkitektur (Core4 + Consoles)

- **Core4** (`BMTP3.Core4/`): Backup engine — traversal, download, hash, collision, sidecar, metadata. 0 warnings.
- **Consoles** (`BMTP3.Consoles/`): CLI commands, progress display, config loading, DI setup. 5 warnings (CS8604 i Core2-archived kode + CS8604 i `BaseOptionsModel.cs:225`).
- **Core/Core2/Core3**: Archived/readonly — må ikke redigeres.
- **Config**: TOML (kebab-case), JSON (camelCase case-insensitive), JSON5 (unquoted keys, kommentarer, trailing commas). Config loads først; `WasSupplied()` overrider.

### Progress Display Design

- `BackupProgressDisplay` med `RunAsync<TResult>(string, Func<IProgress<BackupProgress>, Task<TResult>>)`
- `Progress<T>` oprettes før `StartAsync` (null SyncContext → ThreadPool dispatch)
- Interne: `BackupProgressRenderer`, `ProgressReport` (`internal sealed record`), `ProgressReportMapper`
- `WriteDebugLine(ProgressReport, IAnsiConsole)` — caller tjekker `_debug` før kald
- **Sequential progress contract** — `IBackupEngine.RunAsync` XML-doc kræver serial progress (display er ikke thread-safe)
- `ActionProgress<T>` slettet — bruger `Progress<T>` (standard .NET)

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

## Vigtige beslutninger

| Beslutning | Status |
|---|---|
| `DoAddValidators()` — beholdt med placeholder-kommentar | ✅ RETAINED |
| `ConfigOptionResult` — beholdt med future-kommentar | ✅ RETAINED |
| `ActionProgress<T>` slettet til fordel for `Progress<T>` | ✅ DONE |
| Parameterorden: vigtige params først, infrastructure (`IAnsiConsole`) før config (`bool debug`) | ✅ Regel |
| Fail-first i CLI — returner error code + message, kast aldrig i command handler | ✅ Regel |
| Fail-first i traversal/engine — kast exception ved fejl (aldrig `yield break`, `return`, `continue`) | ✅ Regel |
| Core/Core2/Core3 archived — kun Core4 + Consoles må redigeres | ✅ Regel |
| `private` constructor/`_optionsModels` i `BaseConsoleCommand` | ⚠️ WONTFIX |

---

## Session State

### Done (seneste session — 16 Jun 2026)

| ID | Hvad | Fil(er) |
|---|---|---|
| C-V26 | `WriteDebugLine` fixed: `(ProgressReport, IAnsiConsole)`, caller tjekker `_debug` | `BackupProgressDisplay.cs` |
| C-V30/V31 | `BaseOptionsModel.cs` omskrevet: `ModelDefinition`+`OptionBinding`, Lazy cache, ingen `this`-capture | `BaseOptionsModel.cs` |
| K-V47 | `FileContent.OpenReadAsync`: `CancellationToken` tjekket før `FileStream` | `FileContent.cs` |
| Smelly #5 | `ProgressStatusTask.cs:43`: `Value = Value` → `Value = value` | `ProgressStatusTask.cs` |
| Fix | `KebabCaseToPascalCase` understøtter nu `_` (underscore) — fixture `preserve_hierarchy` | `JsonNamingPolicies.cs` |
| Tests | 243/243 passed efter alle ændringer | — |

### In Progress

*(ingen)*

### Næste — prioriteret

1. **C-V22**: DRY loaders — `BackupPlan4Loader` vs `BackupPlanLoader` merge/refactor
2. **C-V23**: Split `ConsolesPrinter` — isoler Core4 printing fra Core2/Core3
3. **Smelly Code #4–10**: `ConsoleProgressBar`, `OptionsBuilder`, etc.
6. **Build warnings**: ~45 warnings i non-archived projekter (Core4.Tests, Consoles, Consoles.Tests)
7. **Feature gates**: `StopOnError=false`, `EnableMetadata`, `MaxDegreeOfParallelism`, `BackupIndexType.Database`
8. **Integration tests**: MTP pipeline, BackupEngine E2E
9. **Retry/Resilience**: Exponential backoff, MTP resilience

---

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
| C-V22 | ⏳ TODO | Loaders | DRY loaders |
| C-V23 | ⏳ TODO | `ConsolesPrinter` | Split Core4 printing |
| C-V24 | ✅ FIXED | — | Af C-V11 |
| C-V25 | ✅ FIXED | `BackupProgressDisplay.cs` | `MarkRemainingCompletedTasksAsInactive` slettet |
| C-V26 | ✅ DONE | `BackupProgressDisplay.cs` | `WriteDebugLine` parameter fix |
| C-V28 | ✅ FIXED | `BackupConsoleCommand4ListDrives.cs` | `ServiceProvider` null guard |
| C-V29 | ✅ FIXED | `BackupConsoleCommand4ListDrives.cs` | `Task.FromResult(0)` → `return 0` |

### Core4 — High

| ID | Status | Fil | Problem |
|---|---|---|---|
| K-V38 | ⏳ TODO | `SidecarRequest.cs:12` | `SourceType` redundant |
| K-V39 | ⏳ TODO | `BackupEngine.cs:287–288,369–370,492–493` | Null-tjek på `required string` |
| K-V40 | ⏳ TODO | `BackupEngine.cs:82–627` | `RunAsync` for lang (~545 linjer) |
| K-V41 | ⏳ TODO | `SidecarDocument.cs:3` m.fl. | `public` men bør være `internal` |
| K-V42 | ⏳ TODO | `BackupEngine.cs:34,51` | `public sealed` + `internal` constructor |
| K-V43 | ✅ DONE | `InternalsVisibleTo.cs` | 4 ubrugte `using` fjernet |
| K-V44 | ✅ DONE | `Models/IContent.cs` | 3 ubrugte `using` fjernet |
| K-V47 | ✅ DONE | `FileContent.cs:96–98,111` | `CancellationToken` tjek før `FileStream` |

---

## Warnings (5 Consoles — Core2 archived)

| Linje | Warning | Scope |
|---|---|---|
| `BackupConsoleCommand2.cs:46,145,212` | CS8604 — `ConsolesPrinter` null reference | Archived — ignoreres |
| `BackupConsoleCommand3.cs:45` | CS8604 — `ConsolesPrinter` null reference | Archived — ignoreres |
| `BaseOptionsModel.cs:225` | CS8604 — `valueType` kan være null i `CreateBinding` | Skal fixes |

---

## Relevante filer (ændret i seneste session)

| Fil | Ændring |
|---|---|
| `BMTP3.Consoles/ConsoleCommands/BaseOptionsModel.cs` | **Omskrevet** — `ModelDefinition`+`OptionBinding`, Lazy cache, `DoAddValidators()` i factory, nullable value type support |
| `BMTP3.Consoles/ConsoleCommands/Core4/BackupOptionsModel4.cs` | Uændret i denne session — C-V18/C-V19 intakt |
| `BMTP3.Consoles/Progress/BackupProgressDisplay.cs` | `WriteDebugLine` parameter fix + `IAnsiConsole` injection |
| `BMTP3.Consoles/Progress/ProgressReportMapper.cs` | `internal` |
| `BMTP3.Core4/Models/FileContent.cs` | `CancellationToken` tjek før `FileStream` |
| `BMTP3.Consoles/IO/Consoles/ProgressStatus/ProgressStatusTask.cs` | `Value = Value` → `Value = value` |
| `BMTP3.Consoles/Configs/JsonNamingPolicies.cs` | `KebabCaseToPascalCase` understøtter `_` |
