# BMTP3 — Agent Session Context

> **Sidst opdateret:** 18 Jun 2026
> **Tests:** 1064/1064 passed
> **Build:** 0 errors, 0 warnings (Core4), 4 warnings (Consoles — archived Core2/Core3)

---

## Arkitektur (Core4 + Consoles)

- **Core4** (`BMTP3.Core4/`): Backup engine — traversal, download, hash, collision, sidecar, metadata. 0 warnings. **Dette er den kritiske kode.**
- **Consoles** (`BMTP3.Consoles/`): CLI commands, progress display, config loading, DI setup. 4 warnings (CS8604 i Core2-archived kode, alle ignoreret). **Ikke kritisk — "if it ain't broke don't fix it".**
- **Core/Core2/Core3**: Archived/readonly — må ikke redigeres. Dette gælder ALLE filer i Core2/Core3-flowet, uanset hvilket projekt de ligger i. Det inkluderer `BackupConsoleCommand2.cs`, `BackupConsoleCommand3.cs`, `ConsolesPrinter` (Core2/Core3-metoder), og alle filer i `BMTP3.Core2/`, `BMTP3.Core3/`, `BMTP3.Core/`.
- **Consoles** (`BMTP3.Consoles/`): Core4-specifikke CLI commands, progress display, config loading, DI setup.
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
| `BackupPlan4Config.cs` (DTO med nested config-klasser) — beholdes, manuel mapping er eksplicit og testbar. Consoles er ikke kritisk. | ✅ RETAINED |
| Core/Core2/Core3 archived — ALLE filer i Core2/Core3-flowet (uanset projekt) er readonly | ✅ Regel |
| `private` constructor/`_optionsModels` i `BaseConsoleCommand` | ⚠️ WONTFIX |

---

## Session State

### Done (seneste session — 18 Jun 2026)

| ID | Hvad | Fil(er) |
|---|---|---|
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

### In Progress

*(ingen)*

### Næste — prioriteret

1. **K-V40**: `BackupEngine.RunAsync` for lang (~545 linjer) — extract `SequentialBackupRunner`
2. **Feature gates**: `MaxDegreeOfParallelism`, `BackupIndexType.Database`
3. **Integration tests**: MTP pipeline, BackupEngine E2E
4. **Retry/Resilience**: Exponential backoff, MTP resilience

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
| K-V40 | ⏳ TODO | `BackupEngine.cs:82–627` | `RunAsync` for lang (~545 linjer) |
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
| `BMTP3.Consoles/ConsoleCommands/BaseOptionsModel.cs` | **Omskrevet** — `ModelDefinition`+`OptionBinding`, Lazy cache, `DoAddValidators()` i factory, nullable value type support |
| `BMTP3.Consoles/ConsoleCommands/Core4/BackupOptionsModel4.cs` | Uændret i denne session — C-V18/C-V19 intakt |
| `BMTP3.Consoles/Progress/BackupProgressDisplay.cs` | `WriteDebugLine` parameter fix + `IAnsiConsole` injection |
| `BMTP3.Consoles/Progress/ProgressReportMapper.cs` | `internal` |
| `BMTP3.Core4/Models/FileContent.cs` | `CancellationToken` tjek før `FileStream` |
| `BMTP3.Consoles/IO/Consoles/ProgressStatus/ProgressStatusTask.cs` | `Value = Value` → `Value = value` |
| `BMTP3.Consoles/Configs/JsonNamingPolicies.cs` | `KebabCaseToPascalCase` understøtter `_` |
| `BMTP3.Consoles/Services/ConsolesPrinter4.cs` | **Ny** — Core4-specifik printer (kopi af Core4-metoder fra original) |
| `BMTP3.Consoles/Services/ConsolesPrinter2.cs` | **Ny** — Core2-specifik printer |
| `BMTP3.Consoles/Services/ConsolesPrinter3.cs` | **Ny** — Core3-specifik printer |
| `BMTP3.Consoles/Startup/Configurations/ConsolesServiceSetup.cs` | DI-registrering af `ConsolesPrinter4/2/3` |
| `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4.cs` | Bruger `ConsolesPrinter4` i stedet for `ConsolesPrinter` |
| `BMTP3.Core4/Api/Models/Enums/ItemIdStrategy.cs` | **Omdøbt til/erstattet af** `ItemIdScope.cs` — enum `ItemIdStrategy` → `ItemIdScope` |
| `BMTP3.Core4/Api/Models/BackupPlan.cs` | `ItemIdStrategy` → `ItemIdScope` |
| `BMTP3.Core4/Traversal/SourceTraversalRequest.cs` | `ItemIdStrategy` → `ItemIdScope` |
| `BMTP3.Core4/Traversal/MediaDeviceTraversal.cs` | Switch `ItemIdStrategy` → `ItemIdScope` |
| `BMTP3.Core4/Scanner/BackupScanRequest.cs` | `ItemIdStrategy` → `ItemIdScope` |
| `BMTP3.Core4/Scanner/BackupScanner.cs` | `ItemIdStrategy` → `ItemIdScope` |
| `BMTP3.Core4/Engine/BackupEngine.cs` | `ItemIdStrategy` → `ItemIdScope` |
| `BMTP3.Consoles/Configs/BackupPlan4Config.cs` | `ItemIdStrategy` → `ItemIdScope` |
| `BMTP3.Consoles/ConsoleCommands/Core4/BackupOptionsModel4.cs` | `ItemIdStrategy` → `ItemIdScope`, `--item-id-strategy` → `--item-id-scope` |
| `BMTP3.Consoles/ConsoleCommands/Core4/BackupPlanBuilder.cs` | `ItemIdStrategy` → `ItemIdScope` i felt, config, CLI og ToPlan |
| `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4InitConfig.cs` | `enable-metadata/enableMetadata` fjernet fra TOML/JSON/JSON5 templates |
| `BMTP3.Core4/Api/Models/BackupPlan.cs` | `EnableMetadata` property fjernet |
| `BMTP3.Core4/Engine/Validation/BackupPlanValidator.cs` | `EnableMetadata` gate (Tier 3) fjernet |
| `BMTP3.Consoles/Configs/BackupPlan4Config.cs` | `MetadataConfig.EnableMetadata` fjernet |
| `BMTP3.Consoles.Tests/BackupPlan4ConfigTests.cs` | `EnableMetadata` assertions fjernet (6 stk) |
| `BMTP3.Consoles.Tests/BackupPlan4BuildPlanTests.cs` | `EnableMetadata` fra templates + assertions fjernet (6 stk) |
| `BMTP3.Consoles.Tests/Fixtures/config.json` | `enableMetadata` fjernet |
| `BMTP3.Consoles.Tests/TestData/backup_config_test.json` | `enableMetadata` fjernet |
| `BMTP3.Consoles.Tests/Fixtures/config.json5` | `enableMetadata` fjernet |
| `BMTP3.Consoles.Tests/TestData/backup_config_test.json5` | `enableMetadata` fjernet |
| `BMTP3.Core4.Tests/Engine/BackupEngineHappyPathIntegrationTests.cs` | `EnableMetadata = false` fjernet |
