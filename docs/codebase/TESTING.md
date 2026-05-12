# Testing Patterns

## Core Sections (Required)

### 1) Test Stack and Commands
- Primary test framework: xUnit v3
- Assertion/mocking tools: [TODO]
- Commands:
```bash
dotnet test
[TODO: run unit/integration/e2e/coverage]
```

### 2) Test Layout
- Test file placement pattern: Separate test projects (e.g., BMTP3.Core2.Tests)
- Naming convention: [TODO]
- Setup files and where they run: [TODO]

### 3) Test Scope Matrix
| Scope        | Covered? | Typical target         | Notes |
|-------------|----------|-----------------------|-------|
| Unit        | Yes      | Modules/services      |       |
| Integration | Yes      | API/data boundaries   |       |
| E2E         | [TODO]   | User flows            |       |

### 4) Mocking and Isolation Strategy
- Main mocking approach: [TODO]
- Isolation guarantees: [TODO]
- Common failure mode in tests: [TODO]

### 5) Coverage and Quality Signals
- Coverage tool + threshold: [TODO]

### Evidence
- BMTP3.Core2.Tests
- BMTP3.Consoles.Tests
