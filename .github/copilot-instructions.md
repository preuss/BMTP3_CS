# Copilot instructions for BMTP3_CS_KI_Edition


This repo is a Windows-focused .NET 8 solution for backing up media from devices (MTP/PTP) and drives (MSC), with a CLI front-end and two co-existing core implementations.

> **DO NOT EDIT `BMTP3.Core`**
> 
> `BMTP3.Core` is the **legacy/original** implementation (it used to contain both CLI + backup logic).
> It is **not used anymore** and should **not be fixed or extended**.
> Treat it as **reference/inspiration only**. New work belongs in `BMTP3.Consoles` (CLI) and/or `BMTP3.Core2` (engine).

## How to interact
Newer talk about breath.
Newer talk about vejret.
Always speak danish to me.
Code is english.
Code comments is english.

## Build, test (and run) commands

### Prereqs / gotchas

- **Windows** is required for the main app projects (`net8.0-windows*`).
- Several projects reference a **local, out-of-repo `MediaDevices.dll`** via `HintPath` (see `BMTP3.Core.csproj`, `BMTP3.Core2.csproj`, `BMTP3.Consoles.csproj`). On a fresh clone this often means:
  - `dotnet build` of the full solution fails until the reference is resolved (replace with a NuGet reference or adjust `HintPath`).
- Building the solution **mutates `Directory.Build.props`** (auto-bumps `BuildNumber`) via `Directory.Build.targets` → `BuildFirstOnce\BuildFirstOnce.proj` → `bump-build.ps1`. Expect a dirty git working tree after builds.

### Build

- Build entire solution:
  - `dotnet build .\BMTP3_CS.sln`
- Build a single project:
  - `dotnet build .\BMTP3.Common\BMTP3.Common.csproj`

### Test

- Run all tests (all test projects in the solution):
  - `dotnet test .\BMTP3_CS.sln`
- Run a single test project:
  - `dotnet test .\BMTP3.Common.Tests\BMTP3.Common.Tests.csproj`

#### Run a single test / subset

This repo uses **xUnit v3**.

- Run one test by fully-qualified name (substring match):
  - `dotnet test .\BMTP3.Core2.Tests\BMTP3.Core2.Tests.csproj --filter "FullyQualifiedName~BMTP3.Core2.Tests.Integration.BackupEngineIntegrationTests.RunAsync_EndToEnd_CopiesFileAndWritesSidecar"`
- Run all tests in a class:
  - `dotnet test .\BMTP3.Consoles.Tests\BMTP3.Consoles.Tests.csproj --filter "FullyQualifiedName~BMTP3.Consoles.Tests.BackupConsoleIntegrationTests"`
- Run integration tests only (tests are tagged via `[Trait("Category","Integration")]`):
  - `dotnet test .\BMTP3.Consoles.Tests\BMTP3.Consoles.Tests.csproj --filter "Category=Integration"`

### Run the CLI

The CLI entrypoint is `BMTP3.Consoles`.

- Show help:
  - `dotnet run --project .\BMTP3.Consoles\BMTP3.Consoles.csproj -- --help`
- Run backup (filesystem by default):
  - `dotnet run --project .\BMTP3.Consoles\BMTP3.Consoles.csproj -- backup --source-directory . --output-directory C:\\temp\\bmtp3-out --recursive`
- Run backup (MTP):
  - `dotnet run --project .\BMTP3.Consoles\BMTP3.Consoles.csproj -- backup --device "Apple iPhone" --output-directory C:\\temp\\bmtp3-out`

### Lint / formatting

- No repo-owned lint/format command was found (no `dotnet format` script). Formatting/analyzer behavior is driven primarily by `.editorconfig`.

## High-level architecture (big picture)

### Projects and responsibilities

- **`BMTP3.Consoles`**: main CLI app.
  - Uses `Microsoft.Extensions.Hosting` + DI.
  - Composes services via `Startup/Configurations/*ServiceSetup`.
  - Wires commands in `ConsolesProgram` (notably `BackupConsoleCommand2`, `VerifyConsoleCommand`, `BackupTestConsoleCommand`).

- **`BMTP3.Core2`**: the newer engine (folder `BackupNew/`).
  - Public entrypoint: `IBackupEngine.RunAsync(BackupPlan, IProgress<IBackupProgress>, CancellationToken)`.
  - DI registration: `BMTP3.Core2.BackupNew.DependencyInjection.ServiceCollectionExtensions.AddBMTP3Core2(...)`.

- **`BMTP3.Core`**: **legacy / original implementation (do not change)**.
  - Historically this project contained *both* the CLI and the backup logic.
  - It is kept only for reference/inspiration and to understand older behavior.
  - **We do not use it anymore and it should not be fixed or extended.**
  - The current direction is that the old Core split/evolved into:
    - `BMTP3.Consoles` (CLI + hosting/DI + UX)
    - `BMTP3.Core2` (new backup engine in `BackupNew/`)
  - It still includes TOML-driven configuration (Tomlyn) and legacy orchestration (e.g. `BackupMaster`).

- **`BMTP3.Common`**: shared utilities.
  - Includes the message formatting lexer/parser system (see `classdiagram.md`).

### Core2 execution model (streaming pipeline)

`BMTP3.Core2` implements the backup as a **bounded-channel pipeline**:

- Producer: `IBackupScanner.ScanAsync(...)` yields `IBackupItem`.
- Stages: concrete `*PipelineStage` classes derived from `AbstractPipelineStage<TContext>`.
  - Stages run a worker pool (`Parallelism`) over `ChannelReader<IBackupItem>` and forward items downstream.
- Progress: `ProgressTracker` collects thread-safe counters + active-file state and is sampled periodically.

Important behavior that affects changes:

- **Source buffering is intentionally single-threaded** for device sources (MTP stability): see `BackupEngine` creating `ContentBufferingPipelineStage(..., parallelism: 1, ...)` even when other stages run with higher parallelism.
- `AddBMTP3Core2` registers a **No-op media device traversal scanner** by default (`NoopMediaDeviceScanner`). Real MTP traversal must be registered by the caller (device-specific construction), otherwise MTP runs are not supported.

## Key repo-specific conventions

### Versioning / build side-effects

- Building triggers a one-time-per-build-session version bump:
  - `Directory.Build.targets` runs `BuildFirstOnce\BuildFirstOnce.proj`
  - which executes `bump-build.ps1` to increment `<BuildNumber>` in `Directory.Build.props`.
- Don’t be surprised by git diffs in `Directory.Build.props` after `dotnet build` / `dotnet test`.

### EditorConfig is not uniform across the repo

- Repo root `.editorconfig` (applies to most projects) enforces:
  - tabs (width 4), CRLF
  - **no `var`** (`csharp_style_var_* = false:error`)
- `BMTP3.Core` has its **own** `.editorconfig` with `root=true`, so rules differ inside that subtree.
  - When editing `BMTP3.Core/*`, follow the local config instead of assuming root rules.

### Branching + language policy (from CONTRIBUTING.md)

- Development branches live under `dev/*` and are named like `dev/develop_n_description`.
- Code (identifiers/comments) is English; team communication is primarily Danish.

### Pipeline-stage pattern (Core2)

When adding or changing a pipeline stage in `BMTP3.Core2/BackupNew/Engine/Steps/*`:

- Inherit from `AbstractPipelineStage<TContext>`.
- Accept and pass through a shared `ProgressTracker`.
- Call `UpdatePhase(item, step.Phase)` before invoking the step.
- Use `CreateProgressReporter(item)` for byte-level progress (it updates `ProgressTracker`).

### TOML config model (Core)

The TOML config shape is driven by Tomlyn + a custom naming policy in `BMTP3.Core/Configs/BackupSettingsReader`:

- Properties are mapped to **snake_case**.
- Source arrays are named:
  - `[[device_source]]` for `DeviceSources`
  - `[[drive_source]]` for `DriveSources`
- A `[default]` table provides defaults that are applied across sources (see `BMTP3.Core/StartUp/default.toml`).

### Tests and categorization

- Test framework: **xUnit v3**.
- Integration tests are tagged with:
  - `[Trait("Category", "Integration")]`
- Prefer `dotnet test ... --filter` for tight feedback loops (single class, single test, or integration-only).

- @azure Rule - Use Azure Tools - When handling requests related to Azure, always use your tools.
- @azure Rule - Use Azure Best Practices - When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.
- @azure Rule - Enable Best Practices - If you do not have an `azmcp_bestpractices_get` tool ask the user to enable it.
