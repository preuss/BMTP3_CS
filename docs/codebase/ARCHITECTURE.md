# Architecture

## 1) Architectural Style
- Primary style: **Layered with pipeline**
- Why: Clear separation between CLI (Consoles), engine (Core4), and shared utilities (Common)
- Primary constraints: Windows/.NET 8, modularity, extensibility, cross-device (MTP + filesystem)

## 2) System Flow
```text
CLI (BMTP3.Consoles) → Engine (BMTP3.Core4) → Shared (BMTP3.Common) → Output/Verification
                                    ↑
                    (Core/Core2/Core3 — archived/read-only)
```

## 3) Layer/Module Responsibilities
| Layer/Module      | Owns                     | Must not own          | Evidence                |
|-------------------|--------------------------|-----------------------|-------------------------|
| BMTP3.Consoles    | CLI, DI, UX, Progress    | Backup logic          | ConsolesProgram.cs      |
| BMTP3.Core4       | Backup engine pipeline   | CLI, UX               | Core4/Engine/BackupEngine.cs |
| BMTP3.Common      | Shared utilities         | App-specific logic    | Common/                 |

## 4) Core4 Pipeline Stages
| Stage              | Service                  | Purpose                       |
|--------------------|--------------------------|-------------------------------|
| Validation         | BackupPlanValidator      | Pre-flight gates (T3, T4)     |
| Scanning           | BackupScanner            | Enumerate source files        |
| Traversal          | SourceTraversalFactory   | MTP or filesystem walker      |
| Download           | DownloadService          | Read source → temp            |
| Hashing            | HashService              | Compute content hash          |
| Collision          | RenameCollisionResolver  | Skip/Overwrite/Rename         |
| Move               | IMoveableContent         | Temp → destination            |
| Sidecar            | SidecarService           | Metadata sidecar files        |
| Index              | BackUpIndexWriter        | JSON catalog persistence      |
| Verify             | (PostWriteVerification)  | Optional hash verify          |

## 5) Key Patterns
| Pattern           | Where found                          | Purpose                  |
|-------------------|--------------------------------------|--------------------------|
| Pipeline/Stages   | Core4/Engine/BackupEngine.cs         | Sequential file processing |
| Strategy          | Core4/Engine/Strategies/             | Pluggable collision resolution |
| Repository        | Core4/Engine/Session/                | Session state persistence |
| DI                | Consoles + Core4                     | Testability, modularity  |
| Fail-fast         | Core4/Engine/Validation/             | Catch errors at startup  |

## 6) Known Risks
- MTP library (`MediaDevices.dll`) is external, binary-referenced via HintPath
- Platform-bound: `net8.0-windows` for Consoles (DriveInfo, MTP)
- `BackupIndexType.Database` is T4 gated — not yet implemented

## 7) Evidence
- `BMTP3.Consoles/ConsolesProgram.cs` — entry point, DI wiring
- `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4.cs` — Core4 CLI command
- `BMTP3.Core4/Engine/BackupEngine.cs` — pipeline orchestrator (786 lines)
- `BMTP3.Core4/DependencyInjection/ServiceCollectionExtensions.cs` — DI registration
- `BMTP3.Consoles/Configs/BackupPlan4Loader.cs` — TOML config loader
