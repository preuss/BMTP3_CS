# Codebase Structure

## Core Sections (Required)

### 1) Top-Level Map

| Path                | Purpose                        | Evidence           |
|---------------------|-------------------------------|--------------------|
| BMTP3.Common/       | Shared utilities, formatter   | Directory tree     |
| BMTP3.Core2/        | New backup engine             | Directory tree     |
| BMTP3.Consoles/     | CLI app                       | Directory tree     |
| BMTP3.Core/         | Legacy/original implementation| Directory tree     |
| BMTP3.MessageFormatter/ | Message formatting         | Directory tree     |
| BMTP3.Core3/        | Experimental/alt core         | Directory tree     |
| libs/               | External libraries            | Directory tree     |

### 2) Entry Points
- Main runtime entry: BMTP3.Consoles/ConsolesProgram.cs
- Secondary entry points: [TODO]
- How entry is selected: CLI command selection

### 3) Module Boundaries
| Boundary           | What belongs here              | What must not be here         |
|--------------------|-------------------------------|------------------------------|
| BMTP3.Core2        | Backup logic, pipeline stages  | CLI, legacy code             |
| BMTP3.Consoles     | CLI, DI, UX                   | Backup engine logic          |
| BMTP3.Common       | Shared code, formatter         | App-specific logic           |

### 4) Naming and Organization Rules
- File naming pattern: PascalCase for C# files
- Directory organization: By responsibility (core, consoles, common)
- Import aliasing: [TODO]

### 5) Evidence
- Directory tree
- BMTP3.Consoles/ConsolesProgram.cs
