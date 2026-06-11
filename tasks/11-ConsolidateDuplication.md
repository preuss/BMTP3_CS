# Consolidate Duplication in Core4 + Consoles

**Scope:** Only `BMTP3.Core4` and `BMTP3.Consoles` (Core/Core2/Core3 are archived/readonly).

**Kardinalregel:** Vi retter **aldrig** noget som ikke har direkte Core4-tilhørsforhold. Core2/Core3 cleanup, shared helpers på tværs af archived kode, og dokumentation af ikke-Core4 ting er aldrig fokus.

---

## Phase 1 — Low-hanging fruit (no behavior change) ✅ **DONE**

| # | File(s) | Task | Status |
|---|---------|------|--------|
| 1.1 | `BackupConsoleCommand4.Helpers.cs` + `BackupConsoleCommand2.Helpers.cs` | Extract `WasSupplied<T>()` → `OptionHelpers` in Consoles | ✅ |
| 1.2 | `BackupConsoleCommand2.cs` (line 276-357) | Delete dead `EngineArgumentBuilder()` (duplicate of `BackupConsoleCommand2Helpers.BuildPlan()`) | ✅ |
| 1.3 | `FakeFileTransfer.cs` | Delete — empty class without interface, unused | ✅ |
| 1.4 | `MediaDeviceTraversalTests.cs` + `MediaDeviceContentTests.cs` | Consolidate `FakeGatekeeper` → single shared class in `Fakes/` | ✅ |

## Phase 2 — Config Loader boilerplate ❌ **DROPPED**

> **Årsag:** Når Core4 er 100% færdig, slettes alle tilhørsforhold til Core2 og Core3 inden i Consoles. `BackupPlanLoader` (kun Core2-kald) forsvinder dermed, og `ConfigLoader` ville kun være til `BackupPlan4Loader` — ~20 linjer duplikation der ikke er besværet værd.

| # | File(s) | Task | Status |
|---|---------|------|--------|
| 2.1 | `Consoles/Configs/` → new `ConfigLoader.cs` | Generic `ConfigLoader.Load<T>(...)` | ❌ DROPPED |
| 2.2 | `Consoles/Configs/BackupPlanLoader.cs` | Refactor to use `ConfigLoader` | ❌ DROPPED |
| 2.3 | `Consoles/Configs/BackupPlan4Loader.cs` | Refactor to use `ConfigLoader` | ❌ DROPPED |

## Phase 3 — Console command cleanup ❌ **DROPPED**

> **Årsag:** Alle tasks i Phase 3 handler om Core2/Core3 kode eller shared helpers på tværs af Core2/3/4. Core4's `BackupConsoleCommand4` er allerede ren — Ctrl+C håndteres via SignalInterrupt (engine-internal), validering virker, progress virker. Core2/Core3 commands er archived/readonly og ændres aldrig.

| # | File(s) | Task | Status |
|---|---------|------|--------|
| 3.1 | `ApplicationServiceSetup.cs` | Remove registration of `BackupConsoleCommand` (deprecated v1 "backup", shares name with v2) | ❌ DROPPED |
| 3.2 | `BackupConsoleCommand.cs` | Delete (or `[Obsolete]` + remove from DI) | ❌ DROPPED |
| 3.3 | `BackupConsoleCommand2.cs` + `BackupConsoleCommand3.cs` | Extract Ctrl+C pattern → shared helper | ❌ DROPPED |
| 3.4 | `BackupConsoleCommand4.cs` | Implement Ctrl+C using shared helper | ❌ DROPPED |
| 3.5 | `BackupConsoleCommand2.cs` + `BackupConsoleCommand3.cs` + `BackupConsoleCommand4.cs` | Extract shared `ValidateBackupOptions()` | ❌ DROPPED |

## Phase 4 — Enum mapping ❌ **DROPPED**

> **Årsag:** Alle enum-mapperne i `BackupConsoleCommand4.Helpers.cs` er Core2→Core4 konverteringer. Når Core2 tilhørsforhold slettes (efter Core4 er 100% færdig), forsvinder behovet. At extracte en `EnumMapper` nu ville være spild — den vil alligevel blive slettet senere.

| # | File(s) | Task | Status |
|---|---------|------|--------|
| 4.1 | `BackupConsoleCommand4.Helpers.cs` | Extract 10+ enum mappers → shared `EnumMapper` class | ❌ DROPPED |
| 4.2 | `BackupConsoleCommand3.cs` | Use `EnumMapper` instead of own `MapCollisionStrategy()` | ❌ DROPPED |

## Phase 5 — Documentation ❌ **DROPPED**

> **Årsag:** Handler om Core's `TomlNamingHelper2` — ikke Core4.

| # | File(s) | Task | Status |
|---|---------|------|--------|
| 5.1 | `JsonNamingPolicies.cs` | Note in comment: shares 5 conversions with `TomlNamingHelper2` | ❌ DROPPED |

## Not consolidatable (readonly Core/Core2/Core3)
- `StreamHashGenerator` — triplicated in Core2/Core3/Core4
- `FakeHashGenerator` / `FakeItemHasher` / `FakeBackupScanner` across test projects
- `TomlNamingHelper2` in BMTP3.Core
- `IConfigurationReader` (defined but never implemented, in Core)
- `AbstractCommandBase` / `OptionsBuilder` / empty `ConsoleOptions/` stubs (all in Consoles but in use by whom?)
