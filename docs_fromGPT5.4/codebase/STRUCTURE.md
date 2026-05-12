# Codebase Structure

## Core Sections (Required)

### 1) Top-Level Map

| Path | Purpose | Evidence |
|---|---|---|
| `BMTP3.Consoles/` | Main CLI app and composition root | solution structure |
| `BMTP3.Core2/` | Current active backup engine implementation | solution structure |
| `BMTP3.Core4/` | New reimplementation target | solution structure |
| `BMTP3.Common/` | Shared utilities and common helpers | solution structure |
| `BMTP3.MessageFormatter/` | Message-formatting subsystem | solution structure |
| `BMTP3.Core/` | Legacy/original implementation kept for reference | solution structure |
| `BMTP3.Core3/` | Historical/simple sequential implementation kept for reference | solution structure |
| `libs/` | External libraries and local dependencies | repository structure |

### 2) Entry Points
- Main runtime entry: `BMTP3.Consoles\ConsolesProgram.cs`
- Main user-facing flow: CLI command selection inside `BMTP3.Consoles`
- Engine selection today: wired through the consoles layer and current engine registrations

### 3) Module Boundaries
| Boundary | What belongs here | What must not be here |
|---|---|---|
| `BMTP3.Consoles` | CLI, DI, command parsing, UX/reporting | Core backup engine internals |
| `BMTP3.Core2` | Active backup engine and pipeline-based logic | CLI concerns |
| `BMTP3.Core4` | Reimplementation contracts and future engine direction | CLI concerns, legacy behavior as baseline |
| `BMTP3.Common` | Shared helpers and reusable low-level logic | Engine-specific orchestration |
| `BMTP3.Core` / `BMTP3.Core3` | Historical/reference material | New active feature work |

### 4) Naming and Organization Rules
- C# source files use PascalCase
- Projects are organized by responsibility (`Consoles`, `Core2`, `Core4`, `Common`, etc.)
- Tests live in sibling `*.Tests` projects
- Keep the distinction between active, future, and historical modules explicit in docs and code reviews

### 5) Evidence
- repository directory structure
- `BMTP3.Consoles\ConsolesProgram.cs`
- solution projects under `BMTP3_CS.sln`
