# Technology Stack

## Core Sections (Required)

### 1) Runtime Summary

| Area               | Value         | Evidence                |
|--------------------|--------------|-------------------------|
| Primary language   | C#           | BMTP3_CS.sln            |
| Runtime + version  | .NET 8       | BMTP3_CS.sln, csproj    |
| Package manager    | dotnet CLI   | BMTP3_CS.sln, csproj    |
| Module/build system| MSBuild      | BMTP3_CS.sln, csproj    |

### 2) Production Frameworks and Dependencies

| Dependency         | Version      | Role in system          | Evidence                |
|--------------------|-------------|-------------------------|-------------------------|
| MediaDevices.dll   | [TODO]       | Device communication    | BMTP3.Core2.csproj      |
| Tomlyn             | [TODO]       | TOML config parsing     | BMTP3.Core.csproj       |
| Microsoft.Extensions.Hosting | [TODO] | DI/Hosting         | BMTP3.Consoles.csproj   |

### 3) Development Toolchain

| Tool               | Purpose      | Evidence                |
|--------------------|-------------|-------------------------|
| dotnet test        | Test runner  | BMTP3_CS.sln            |
| .editorconfig      | Formatting   | .editorconfig           |

### 4) Key Commands

```bash
dotnet build
dotnet test
[TODO: lint command]
```

### 5) Environment and Config

- Config sources: appsettings.json, Directory.Build.props, TOML files
- Required env vars: [TODO]

### Evidence
- BMTP3_CS.sln
- BMTP3.Core2.csproj
- BMTP3.Consoles.csproj
- .editorconfig
- Directory.Build.props
