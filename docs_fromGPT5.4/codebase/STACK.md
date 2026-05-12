# Technology Stack

## Core Sections (Required)

### 1) Runtime Summary

| Area | Value | Evidence |
|---|---|---|
| Primary language | C# | `BMTP3_CS.sln` |
| Runtime family | .NET | solution and project files |
| Main repo target direction | .NET 8 / Windows-focused solution | repo conventions and active projects |
| Build system | MSBuild via `dotnet` CLI | solution and csproj files |
| Primary OS assumption | Windows | device/MTP usage and project targets |

### 2) Production Frameworks and Dependencies

| Dependency | Role in system | Status | Evidence |
|---|---|---|---|
| `Microsoft.Extensions.Hosting` | Hosting/composition in CLI app | Active | `BMTP3.Consoles` |
| `Microsoft.Extensions.DependencyInjection` | DI/service registration | Active | Consoles/Core docs and project references |
| `Microsoft.Extensions.Logging` | Logging abstraction | Active | Consoles/engine service patterns |
| `MediaDevices.dll` | Media-device/MTP communication | Active but locally referenced/risky | project `HintPath` references |
| `MetadataExtractor` | Managed metadata/date extraction | Active/planned Core4 metadata strategy | normalized Core4 docs |
| `ExifTool` | External metadata fallback CLI | Bundled/planned Core4 fallback path | normalized Core4 docs |
| Tomlyn | TOML parsing | Legacy/Core only | `BMTP3.Core` |

### 3) Development Toolchain

| Tool | Purpose | Evidence |
|---|---|---|
| `dotnet build` | Build solution/projects | repo workflow |
| `dotnet test` | Run test projects | repo workflow |
| xUnit v3 | Test framework | test projects and docs |
| `.editorconfig` | Formatting/style rules | repo root and local overrides |
| PowerShell scripts | Build number/version helpers | repository scripts and build targets |

### 4) Key Commands

```powershell
dotnet build .\BMTP3_CS.sln
dotnet test .\BMTP3_CS.sln
dotnet run --project .\BMTP3.Consoles\BMTP3.Consoles.csproj -- --help
```

Notes:

- full solution builds/tests can mutate `Directory.Build.props` because of build-number tooling
- targeted `dotnet test ... --filter` is the preferred tight feedback loop for this repo

### 5) Environment and Config

- Primary configuration sources:
  - `appsettings.json`-style hosting config where applicable
  - `Directory.Build.props` / `Directory.Build.targets`
  - TOML files in legacy Core
- Primary environment constraint:
  - Windows is required for the main device-oriented projects
- No central required environment-variable story is obvious from the current repo shape

### 6) Evidence
- `BMTP3_CS.sln`
- `BMTP3.Consoles\`
- `BMTP3.Core2\`
- `BMTP3.Core4\`
- `.editorconfig`
- `Directory.Build.props`
- `Directory.Build.targets`
