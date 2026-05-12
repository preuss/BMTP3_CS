# Testing Patterns

## Core Sections (Required)

### 1) Test Stack and Commands
- Primary test framework: xUnit v3
- Assertion style: normal xUnit assertions
- Isolation style: fakes/test doubles where helpful, especially for engine/service boundaries
- Commands:

```powershell
dotnet test .\BMTP3_CS.sln
dotnet test .\BMTP3.Core2.Tests\BMTP3.Core2.Tests.csproj --filter "Category=Integration"
dotnet test .\BMTP3.Consoles.Tests\BMTP3.Consoles.Tests.csproj --filter "FullyQualifiedName~SomeTestClass"
```

### 2) Test Layout
- Tests live in sibling `*.Tests` projects
- Examples:
  - `BMTP3.Common.Tests`
  - `BMTP3.Consoles.Tests`
  - `BMTP3.Core.Tests`
  - `BMTP3.Core2.Tests`
  - `BMTP3.Core3.Tests`
  - `BMTP3.Core4.Tests` when/if expanded
- Use focused test project runs or filtered runs for tight feedback loops

### 3) Test Scope Matrix
| Scope | Covered? | Typical target | Notes |
|---|---|---|---|
| Unit | Yes | Services, helpers, models, engine pieces | Main fast feedback loop |
| Integration | Yes | Engine flows and CLI flows | Tagged with `[Trait("Category", "Integration")]` |
| End-to-end | Limited/ad hoc | Full backup scenarios | Often approximated through integration-style tests |

### 4) Mocking and Isolation Strategy
- Prefer simple fakes/test doubles over heavy mocking where engine orchestration is involved
- Keep external/device-dependent behavior isolated behind interfaces
- For Core4 direction, scanners, transfer services, sidecar generators, and session-state services are natural fake boundaries
- Common failure mode to watch: tests that accidentally depend on real filesystem/device assumptions without making that explicit

### 5) Coverage and Quality Signals
- No repo-wide coverage threshold is clearly enforced from the current docs/codebase view
- Primary quality signal is meaningful test coverage in the relevant `*.Tests` project plus passing solution test runs
- Integration tests are especially important for backup flows and device-sensitive behavior

### 6) Evidence
- `BMTP3.*.Tests` projects in solution root
- xUnit v3 usage described in repo guidance
- integration trait usage documented in repository instructions
