# Coding Conventions

## Core Sections (Required)

### 1) Naming Rules
| Item | Rule | Example | Evidence |
|---|---|---|---|
| Files | PascalCase for C# source files | `BackupEngine.cs` | Repository layout |
| Methods | PascalCase | `RunAsync` | C# codebase style |
| Types/interfaces | PascalCase, interfaces prefixed with `I` | `IBackupEngine` | C# codebase style |
| Projects | `BMTP3.<Area>` / `BMTP3.<Area>.Tests` | `BMTP3.Core4`, `BMTP3.Core4.Tests` | Solution structure |
| Branches | `dev/develop_n_description` | `dev/develop_4_core4-docs` | `docs/CONTRIBUTING.md` |

### 2) Formatting and Linting
- Primary formatter/analyzer source: repository `.editorconfig`
- Root conventions: tabs, CRLF, explicit local types instead of `var` in most folders
- Important exception: `BMTP3.Core` has its own local `.editorconfig`
- Build command: `dotnet build .\BMTP3_CS.sln`
- Test command: `dotnet test .\BMTP3_CS.sln`
- Important repo behavior: builds/tests can bump `Directory.Build.props` via build-number tooling

### 3) Import and Module Conventions
- Standard C# `using` directives at file top
- Avoid unnecessary aliases unless they remove real ambiguity
- Keep responsibilities in the correct project:
  - `BMTP3.Consoles` = CLI/DI/wiring
  - `BMTP3.Core2` = active engine today
  - `BMTP3.Core4` = reimplementation target
  - `BMTP3.Common` = shared utilities
  - `BMTP3.Core` / `BMTP3.Core3` = historical/reference code

### 4) Error and Logging Conventions
- Prefer explicit domain/validation errors over silent fallback behavior
- Validation belongs early (`BackupPlanValidator` in Core4)
- Do not swallow errors silently; either surface them, record them, or handle them deliberately
- Use `ILogger<T>` in service layers where the implementation pattern already expects logging
- Treat MTP/device failures as operationally important and keep their handling explicit

### 5) Testing Conventions
- Test framework: xUnit v3
- Test projects live in sibling `*.Tests` projects
- Integration tests use `[Trait("Category", "Integration")]`
- Prefer focused test runs with `dotnet test ... --filter`
- Use fakes/test doubles where appropriate for engine/service isolation

### 6) Evidence
- `.editorconfig`
- `docs/CONTRIBUTING.md`
- solution structure under repository root
- test project naming under `BMTP3.*.Tests`
