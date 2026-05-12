# Architecture

## Core Sections (Required)

### 1) Architectural Style
- Primary style: Layered
- Why: Clear separation between CLI, engine, and shared utilities (see structure)
- Primary constraints: Windows/.NET 8, modularity, extensibility

### 2) System Flow
```text
CLI (BMTP3.Consoles) -> Engine (BMTP3.Core2) -> Shared (BMTP3.Common) -> Output/Verification
```

### 3) Layer/Module Responsibilities
| Layer/Module      | Owns                | Must not own         | Evidence                |
|-------------------|---------------------|----------------------|-------------------------|
| BMTP3.Consoles    | CLI, DI, UX         | Backup logic         | ConsolesProgram.cs      |
| BMTP3.Core2       | Backup logic, pipeline | CLI, UX           | Core2/BackupNew/Engine  |
| BMTP3.Common      | Shared utilities     | App-specific logic   | Common/                 |

### 4) Reused Patterns
| Pattern           | Where found         | Why it exists        |
|-------------------|--------------------|----------------------|
| Pipeline/Stages   | Core2/BackupNew/Engine | For backup flow  |
| Dependency Injection | Consoles, Core2  | Testability, modular |

### 5) Known Architectural Risks
- Tight coupling to Windows/.NET
- Legacy code in BMTP3.Core may confuse maintainers

### 6) Evidence
- ConsolesProgram.cs
- Core2/BackupNew/Engine/
- Common/
