# Codebase Structure

## 1) Top-Level Map

| Path                | Purpose                            | Evidence           |
|---------------------|------------------------------------|--------------------|
| BMTP3.Core4/        | **Active backup engine**           | Directory tree     |
| BMTP3.Consoles/     | CLI host (entry point)             | Directory tree     |
| BMTP3.Common/       | Shared utilities, formatter        | Directory tree     |
| BMTP3.MessageFormatter/ | Message formatting library     | Directory tree     |
| BMTP3.Core2/        | Archived — second-gen engine       | Directory tree     |
| BMTP3.Core3/        | Archived — third-gen engine        | Directory tree     |
| BMTP3.Core/         | Archived — original implementation | Directory tree     |
| libs/               | External libraries (MediaDevices)  | Directory tree     |

## 2) Entry Points
- **Main runtime entry:** `BMTP3.Consoles/ConsolesProgram.cs`
- **Primary commands:** `backup4` (Core4), `list-sources`, `init-config`
- **Legacy commands:** `backup`, `backup2`, `backup3`

## 3) Module Boundaries
| Boundary           | What belongs here              | What must not be here         |
|--------------------|-------------------------------|------------------------------|
| BMTP3.Core4        | Backup logic, pipeline stages  | CLI, legacy code             |
| BMTP3.Consoles     | CLI, DI, UX, progress display  | Backup engine logic          |
| BMTP3.Common       | Shared code, formatter         | App-specific logic           |

## 4) Naming and Organization Rules
- **File naming:** PascalCase for C# files
- **Directory organization:** By responsibility (Engine, Scanner, Traversal, Storage)
- **Tests:** `*Tests.cs` files in corresponding `*.Tests/` project
- **Fakes:** Located in `Core4.Tests/Fakes/`

## 5) Key Files

| File | Role |
|------|------|
| `Consoles/ConsolesProgram.cs` | CLI entry point, command registration |
| `Consoles/ConsoleCommands/BackupConsoleCommand4.cs` | Core4 command handler |
| `Consoles/ConsoleCommands/Core4/BackupOptionsModel4.cs` | Option model (no Core2 enums) |
| `Consoles/Configs/BackupPlan4Loader.cs` | TOML/JSON/JSON5 config loader |
| `Consoles/Progress/BackupProgressDisplay.cs` | Spectre progress display |
| `Core4/Api/IBackupEngine.cs` | Engine entry interface |
| `Core4/Engine/BackupEngine.cs` | Pipeline orchestrator |
| `Core4/Engine/Validation/BackupPlanValidator.cs` | Pre-flight validation gates |
| `Core4/Engine/Strategies/RenameCollisionResolver.cs` | Collision resolution |
| `Core4/Engine/Index/JsonBackupIndexWriter.cs` | Catalog persistence |
| `Core4/Engine/Sidecar/SidecarService.cs` | Metadata sidecar generation |
| `Core4/Traversal/MtpUriParser.cs` | MTP URI parsing |
| `Core4/DependencyInjection/ServiceCollectionExtensions.cs` | DI wiring |

## 6) Evidence
- Directory tree
- `BMTP3.Consoles/ConsolesProgram.cs`
- `BMTP3.Core4/Engine/BackupEngine.cs`
