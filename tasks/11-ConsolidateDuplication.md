# Consolidate Duplication in Core4 + Consoles

**Scope:** Only `BMTP3.Core4` and `BMTP3.Consoles` (Core/Core2/Core3 are archived/readonly).

---

## Phase 1 — Low-hanging fruit (no behavior change) ✅ **DONE**

| # | File(s) | Task | Status |
|---|---------|------|--------|
| 1.1 | `BackupConsoleCommand4.Helpers.cs` + `BackupConsoleCommand2.Helpers.cs` | Extract `WasSupplied<T>()` → `OptionHelpers` in Consoles | ✅ |
| 1.2 | `BackupConsoleCommand2.cs` (line 276-357) | Delete dead `EngineArgumentBuilder()` (duplicate of `BackupConsoleCommand2Helpers.BuildPlan()`) | ✅ |
| 1.3 | `FakeFileTransfer.cs` | Delete — empty class without interface, unused | ✅ |
| 1.4 | `MediaDeviceTraversalTests.cs` + `MediaDeviceContentTests.cs` | Consolidate `FakeGatekeeper` → single shared class in `Fakes/` | ✅ |

## Phase 2 — Config Loader boilerplate

| # | File(s) | Task | Status |
|---|---------|------|--------|
| 2.1 | `Consoles/Configs/` → new `ConfigLoader.cs` | Generic `ConfigLoader.Load<T>(FileInfo, JsonNamingPolicy)` with null-check, exists-check, extension dispatch (`.json`/`.json5`/`.toml`), JSON5 normalizer moved in | ❌ |
| 2.2 | `Consoles/Configs/BackupPlanLoader.cs` | Refactor to call `ConfigLoader.Load<Core2.BackupPlan>(file, SnakeCasePolicy)` | ❌ |
| 2.3 | `Consoles/Configs/BackupPlan4Loader.cs` | Refactor to call `ConfigLoader.Load<BackupPlan4Config>(file, KebabCasePolicy)` | ❌ |

## Phase 3 — Console command cleanup

| # | File(s) | Task | Status |
|---|---------|------|--------|
| 3.1 | `ApplicationServiceSetup.cs` | Remove registration of `BackupConsoleCommand` (deprecated v1 "backup", shares name with v2) | ❌ |
| 3.2 | `BackupConsoleCommand.cs` | Delete (or `[Obsolete]` + remove from DI). Shares "backup" name with v2, has TODO/dead code. | ❌ |
| 3.3 | `BackupConsoleCommand2.cs` + `BackupConsoleCommand3.cs` | Extract Ctrl+C pattern (linkedCts + CancelKeyPress) → shared helper | ❌ |
| 3.4 | `BackupConsoleCommand4.cs` | Implement Ctrl+C using the new shared helper (currently missing) | ❌ |
| 3.5 | `BackupConsoleCommand2.cs` + `BackupConsoleCommand3.cs` + `BackupConsoleCommand4.cs` | Extract shared `ValidateBackupOptions()` logic | ❌ |

## Phase 4 — Enum mapping

| # | File(s) | Task | Status |
|---|---------|------|--------|
| 4.1 | `BackupConsoleCommand4.Helpers.cs` | Extract 10+ enum mappers → shared `EnumMapper` class (Core2↔Core4 collision, sidecar, comparison types) | ❌ |
| 4.2 | `BackupConsoleCommand3.cs` | Use `EnumMapper` instead of own `MapCollisionStrategy()` | ❌ |

## Phase 5 — Documentation

| # | File(s) | Task | Status |
|---|---------|------|--------|
| 5.1 | `JsonNamingPolicies.cs` | Note in comment: shares 5 conversions with `TomlNamingHelper2` (Core — readonly, cannot consolidate) | ❌ |

## Not consolidatable (readonly Core/Core2/Core3)
- `StreamHashGenerator` — triplicated in Core2/Core3/Core4
- `FakeHashGenerator` / `FakeItemHasher` / `FakeBackupScanner` across test projects
- `TomlNamingHelper2` in BMTP3.Core
- `IConfigurationReader` (defined but never implemented, in Core)
- `AbstractCommandBase` / `OptionsBuilder` / empty `ConsoleOptions/` stubs (all in Consoles but in use by whom?)
