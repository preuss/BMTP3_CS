# Architecture

## Core Sections (Required)

### 1) Architectural Style
- Primary style: Layered, with multiple co-existing engine generations
- Why: The repository currently contains legacy, active, and in-progress engine implementations that must be kept distinct
- Primary constraints: Windows/.NET, device-oriented backup domain, maintainability across generations

### 2) System Flow
```text
CLI (BMTP3.Consoles)
  -> Active engine today: BMTP3.Core2
  -> Reimplementation target: BMTP3.Core4
  -> Shared utilities: BMTP3.Common
```

### 3) Layer/Module Responsibilities
| Layer/Module | Owns | Must not own | Evidence |
|---|---|---|---|
| BMTP3.Consoles | CLI, DI, user interaction, command wiring | Core backup engine internals | `ConsolesProgram.cs`, `Startup/Configurations/` |
| BMTP3.Core2 | Current working backup engine and pipeline-based flow | CLI concerns | `BackupNew/Engine/` |
| BMTP3.Core4 | New reimplementation target and future engine direction | CLI concerns, legacy compatibility hacks as baseline | `BMTP3.Core4/` |
| BMTP3.Common | Shared utilities and reusable low-level helpers | Engine-specific orchestration | `BMTP3.Common/` |
| BMTP3.Core / BMTP3.Core3 | Historical reference and lessons source | New feature development | `BMTP3.Core/`, `BMTP3.Core3/` |

### 4) Reused Patterns
| Pattern | Where found | Why it exists |
|---|---|---|
| Dependency Injection | Consoles, Core2, planned Core4 composition | Testability and replaceable services |
| Pipeline/Stages | Core2 | Historical high-throughput design, now treated cautiously |
| Sequential orchestration | Core3, Core4 direction | Simplicity, debuggability, MTP stability |

### 5) Known Architectural Risks
- Multiple co-existing generations can confuse contributors
- Core2 pipeline patterns are not the same as the intended Core4 baseline
- Windows/device dependencies make the repo architecture platform-specific

### 6) Evidence
- `BMTP3.Consoles\ConsolesProgram.cs`
- `BMTP3.Core2\BackupNew\Engine\`
- `BMTP3.Core4\`
- `BMTP3.Common\`
