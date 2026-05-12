# Coding Conventions

## Core Sections (Required)

### 1) Naming Rules
| Item              | Rule                  | Example             | Evidence         |
|-------------------|----------------------|---------------------|------------------|
| Files             | PascalCase           | BackupEngine.cs     | Directory tree   |
| Functions/methods | PascalCase           | RunAsync            | C# conventions   |
| Types/interfaces  | PascalCase           | IBackupEngine       | C# conventions   |
| Constants/env vars| UPPER_SNAKE_CASE     | BUILD_NUMBER        | Directory.Build.props |

### 2) Formatting and Linting
- Formatter: .editorconfig (tabs, CRLF, no var)
- Linter: [TODO]
- Most relevant enforced rules: explicit types, braces, CRLF
- Run commands: [TODO]

### 3) Import and Module Conventions
- Import grouping/order: [TODO]
- Alias vs relative import policy: [TODO]
- Public exports/barrel policy: [TODO]

### 4) Error and Logging Conventions
- Error strategy by layer: [TODO]
- Logging style and required context fields: [TODO]
- Sensitive-data redaction rules: [TODO]

### 5) Testing Conventions
- Test file naming/location rule: [TODO]
- Mocking strategy norm: [TODO]
- Coverage expectation: [TODO]

### 6) Evidence
- .editorconfig
- Directory.Build.props
