# External Integrations

## Core Sections (Required)

### 1) Integration Inventory
| System            | Type (API/DB/Queue/etc) | Purpose            | Auth model | Criticality | Evidence                |
|-------------------|-------------------------|--------------------|------------|-------------|-------------------------|
| MediaDevices.dll  | DLL                     | Device comms (MTP) | [TODO]     | High        | BMTP3.Core2.csproj      |
| Tomlyn            | NuGet                   | TOML config        | [TODO]     | Medium      | BMTP3.Core.csproj       |

### 2) Data Stores
| Store             | Role                    | Access layer       | Key risk   | Evidence                |
|-------------------|-------------------------|--------------------|------------|-------------------------|
| Filesystem        | Backup storage          | Core2/Engine      | Data loss  | Core2/BackupNew/Engine  |

### 3) Secrets and Credentials Handling
- Credential sources: [TODO]
- Hardcoding checks: [TODO]
- Rotation or lifecycle notes: [TODO]

### 4) Reliability and Failure Behavior
- Retry/backoff behavior: [TODO]
- Timeout policy: [TODO]
- Circuit-breaker or fallback behavior: [TODO]

### 5) Observability for Integrations
- Logging around external calls: [TODO]
- Metrics/tracing coverage: [TODO]
- Missing visibility gaps: [TODO]

### 6) Evidence
- BMTP3.Core2.csproj
- BMTP3.Core.csproj
