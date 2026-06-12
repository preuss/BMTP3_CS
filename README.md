# BMTP3 — Backup Media Transfer Protocol

**Version:** 0.4.0-beta | **Platform:** Windows (.NET 8, C# 12)

BMTP3 is the third-generation backup solution for Windows, designed to securely back up data from smartphones, tablets, cameras, and external drives via **MTP**, **PTP**, and **MSC** protocols.

## Project Status

| Layer | Status | Notes |
|-------|--------|-------|
| **Core4** (`backup4`) | **Active** | Primary engine with full pipeline, TOML config, progress display |
| Core3 (`backup3`) | Archived | Read-only — intermediate refactor |
| Core2 (`backup2`) | Archived | Read-only — first pipeline attempt |
| Core (`backup`) | Archived | Read-only — original monolithic exe |
| Consoles (`BMTP3.Consoles`) | **Active** | CLI host — updated with Core4 |

## Quick Start

```shell
# List available MTP devices and drives
bmtp3 list-sources

# Backup from a local folder
bmtp3 backup4 --source-path "C:\Photos" --destination "D:\Backup"

# Backup from an MTP device
bmtp3 backup4 --source-path "mtp://MyPhone/Internal Storage/DCIM" --destination "D:\Backup"

# Use a TOML config file
bmtp3 backup4 --config "my-backup.toml"

# Initialize a config template
bmtp3 init-config
```

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    BMTP3.Consoles                        │
│  CLI (System.CommandLine) → DI → Progress (Spectre)     │
├─────────────────────────────────────────────────────────┤
│                    BMTP3.Core4                           │
│  ┌──────────────────────────────────────────────────┐   │
│  │  BackupPipeline                                  │   │
│  │  Validate → Scan → Traverse → Download → Hash   │   │
│  │  → Compare/Skip → Move → Sidecar → Index        │   │
│  └──────────────────────────────────────────────────┘   │
│  ┌──────┐ ┌────────┐ ┌──────────┐ ┌────────────────┐   │
│  │ MTP  │ │Hashing │ │ Timestamp│ │ Collision       │   │
│  │Traver│ │(Blake3,│ │Resolution│ │ Resolution      │   │
│  │sal   │ │SHA-256)│ │(EXIF/GPS)│ │(Rename/Skip/Ov) │   │
│  └──────┘ └────────┘ └──────────┘ └────────────────┘   │
├─────────────────────────────────────────────────────────┤
│                    BMTP3.Common                          │
│           Message formatter, shared utilities            │
└─────────────────────────────────────────────────────────┘
```

### Key Design Decisions

- **One CLI option for source:** `--source-path` accepts both filesystem paths and `mtp://` URIs — prefix detection only, no separate `--source-device` flag
- **Post-write verification is opt-in:** default `None`, enable with `--verify hash`
- **Fail-fast validation:** all pre-flight gates throw exceptions on failure — no silent fallbacks
- **In-memory integration tests:** all service interfaces mocked via custom fakes — no physical files or MTP devices needed
- **DI throughout:** all engine services resolved via `AddBMTP3Core4()` — no `new` in production code

## CLI Commands

| Command | Description | Engine |
|---------|-------------|--------|
| `backup4` **| Core4 backup — primary command** | Core4 |
| `list-sources` | List MTP devices, drives, and folders | Core4 |
| `init-config` | Generate a TOML config template | Core4 |
| `backup2` | Legacy Core2 backup (archived) | Core2 |
| `backup3` | Legacy Core3 backup (archived) | Core3 |
| `backup` | Original Core backup (archived, exe) | Core |
| `backup-test` | Test/experimental command | — |
| `verify` | Verify existing backups | — |

### `backup4` Options

| Option | Alias | Default | Description |
|--------|-------|---------|-------------|
| `--source-path` | `-s` | *required* | Source path (`C:\...` or `mtp://...`) |
| `--destination` | `-d` | *required* | Destination directory |
| `--config` | `-c` | — | TOML config file path |
| `--collision-strategy` | | `Skip` | How to handle filename collisions: `Skip`, `Overwrite`, `Rename`, `RenameWithPattern` |
| `--rename-pattern` | | — | Custom rename pattern (requires `RenameWithPattern`) |
| `--verify` | | `None` | Post-write verification: `None`, `Hash` |
| `--enable-timestamp` | | `false` | Enable earliest-timestamp resolution |
| `--enable-metadata` | | `false` | Enable metadata extraction (sidecar) |
| `--index-type` | | `Json` | Backup index format: `Json`, `Database` |
| `--dry-run` | | `false` | Simulate without copying files |
| `--stop-on-error` | | `true` | Stop pipeline on first error |
| `--include-pattern` | | `**/*` | Glob include filter |
| `--exclude-pattern` | | — | Glob exclude filter |

## Project Structure

```
BMTP3_CS.sln
├── BMTP3.Consoles/          # CLI host (System.CommandLine, Spectre.Console)
│   ├── ConsoleCommands/     # Command definitions (backup2, backup3, backup4...)
│   ├── Configs/             # TOML config loader
│   ├── Progress/            # Spectre progress display
│   ├── Services/            # Printer, notifier, prompter
│   └── Startup/             # DI, logging, config wiring
├── BMTP3.Core4/             # Active backup engine
│   ├── Api/                 # Public interfaces (IBackupEngine, IDriveCatalogService)
│   ├── Engine/              # Pipeline: Compare, Download, Hash, Index, Sidecar, Strategies, Validation
│   ├── Scanner/             # Source scanning
│   ├── Traversal/           # MTP + filesystem traversal
│   ├── Storage/             # Source connectors (MTP, filesystem)
│   ├── DriveDiscovery/      # Drive catalogs (MTP + filesystem providers)
│   ├── Devices/             # MTP device abstractions
│   ├── Hashing/             # Hash generators (Blake3, SHA-256, BLAKE2)
│   └── Models/              # Domain models + enums
├── BMTP3.Core2/             # Archived — second-gen engine
├── BMTP3.Core3/             # Archived — third-gen engine
├── BMTP3.Core/              # Archived — first-gen monolithic exe
├── BMTP3.Common/            # Shared utilities + message formatter
├── BMTP3.MessageFormatter/  # Standalone template formatter
├── BMTP3.Core4.Tests/       # 249 unit/integration tests
├── BMTP3.Consoles.Tests/    # 83 unit tests
└── libs/MediaDevices.dll    # External MTP library
```

## Test Suite

| Project | Count | What it tests |
|---------|-------|---------------|
| `Core4.Tests` | 249 | Engine pipeline, strategies, validation, MtpUriParser, hash, sidecar, collision, integration |
| `Consoles.Tests` | 83 | CLI parsing, BuildPlan, option mapping, config loading |
| `Core2.Tests` | — | Legacy — not actively maintained |
| `Core3.Tests` | — | Legacy — not actively maintained |
| `Core.Tests` | — | Legacy — not actively maintained |

**Run:** `dotnet test`

## License

AGPL-3.0 — see [LICENSE.md](LICENSE.md). Contributions are welcome under the [CLA](CLA.md).
