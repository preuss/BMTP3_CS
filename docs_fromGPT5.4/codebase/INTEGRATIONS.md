# External Integrations

## Core Sections (Required)

### 1) Integration Inventory
| System | Type | Purpose | Auth model | Criticality | Evidence |
|---|---|---|---|---|---|
| MediaDevices.dll | Local DLL reference | Device communication for MTP/media-device access | No explicit app auth; device/session access | High | project `HintPath` references in Core/Consoles/Core2 |
| MetadataExtractor | NuGet library | Managed metadata/date extraction from files | None | High for metadata features | Core4 normalized docs and legacy metadata usage |
| ExifTool | External CLI executable | Metadata/date fallback when managed parsing is insufficient | None | Medium/High for richer metadata support | Core4 normalized docs, bundled tool usage |
| Microsoft.Extensions.Hosting / DI / Logging | Framework packages | Hosting, composition root, logging, service wiring | None | High | `BMTP3.Consoles`, planned Core4 DI |
| Tomlyn | NuGet library | TOML configuration in legacy Core | None | Low/legacy | `BMTP3.Core` |

### 2) Data Stores
| Store | Role | Access layer | Key risk | Evidence |
|---|---|---|---|---|
| Filesystem | Primary backup destination and source for non-device backups | Core2 engine, planned Core4 transfer layer | Data loss or partial writes if failures are mishandled | active engine structure, Core4 docs |
| Temporary staging on disk | Safe intermediate writes / device download staging | transfer implementations | Orphaned temp files if cleanup is weak | legacy lessons, transfer design notes |
| In-memory session state | Active backup session tracking | `IBackupSessionStateStore` / `InMemoryBackupSessionStateStore` | No durability until repository layer exists | Core4 skeleton |
| Future repository storage | Resume/persistence layer | planned `IBackupRepository` implementations | Drift between runtime state and durable state | Core4 guide/tier docs |

### 3) Secrets and Credentials Handling
- The repo does not appear to revolve around traditional app secrets or service credentials
- Main sensitivity is operational: user files, device contents, paths, and backup destinations
- The biggest integration safety issue is not secret rotation, but avoiding accidental leakage of file/device data in logs and external tools
- Contributors should avoid sending proprietary file contents or device data to external services/tools

### 4) Reliability and Failure Behavior
- MTP is the highest-risk integration and is treated as sequential in the normalized Core4 direction
- Normalized Core4 MTP rules:
  - keepalive every 30 seconds
  - per-operation timeout of 60 seconds
  - retry backoff of 1s, 2s, 4s
- Metadata reliability strategy:
  - `MetadataExtractor` first
  - `ExifTool` fallback
  - filesystem attributes as last fallback data source
- Build reliability risk:
  - local `MediaDevices.dll` references may fail on fresh clones

### 5) Observability for Integrations
- Logging is expected through `ILogger<T>` where service-based implementations already use logging patterns
- Device operations, file transfer failures, and metadata fallback paths are especially important to log clearly
- There is no strong evidence of broad tracing/metrics infrastructure across the repo
- Biggest visibility gap: diagnosing device/session failures and local dependency-resolution failures

### 6) Evidence
- `BMTP3.Consoles\`
- `BMTP3.Core2\`
- `BMTP3.Core4\`
- csproj `HintPath` references for `MediaDevices.dll`
- normalized Core4 docs under `docs\`
