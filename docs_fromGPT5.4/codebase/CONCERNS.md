# Codebase Concerns

## Core Sections (Required)

### 1) Top Risks (Prioritized)
| Severity | Concern | Evidence | Impact | Suggested action |
|---|---|---|---|---|
| High | Multiple co-existing engine generations | `BMTP3.Core`, `BMTP3.Core2`, `BMTP3.Core3`, `BMTP3.Core4` all exist | Contributors may edit the wrong implementation | Keep active direction explicit in docs and contributor guidance |
| High | Local `MediaDevices.dll` dependency | Project references rely on local `HintPath` | Fresh clones/builds may fail or behave inconsistently | Replace or normalize dependency handling |
| High | Core4 is an in-progress reimplementation, not the active engine yet | `BMTP3.Core4` skeleton exists beside active Core2 | Architecture drift and false assumptions | Keep Core4 contracts/documentation tightly normalized |
| Medium | Build/test side effects modify repo files | Build-number tooling mutates `Directory.Build.props` | Dirty working tree, confusing diffs | Document the behavior and review diffs carefully |
| Medium | Windows/device coupling | Device backup projects target Windows-specific behavior | Portability and CI options are constrained | Keep platform assumptions explicit |

### 2) Technical Debt
| Debt item | Why it exists | Where | Risk if ignored | Suggested fix |
|---|---|---|---|---|
| Legacy engines kept for reference | Earlier generations are still useful as lessons source | `BMTP3.Core`, `BMTP3.Core3` | New work may drift into legacy code | Keep historical/reference status explicit |
| Pipeline-heavy complexity in active engine | Core2 evolved around channel/pipeline patterns | `BMTP3.Core2\BackupNew\Engine\` | Harder debugging and maintenance | Continue extracting lessons into Core4 and keep scope clear |
| Documentation drift across generations | Many docs were written across multiple iterations | `docs\` | Conflicting guidance | Keep one normalized Core4 document set |

### 3) Security / Reliability Concerns
| Risk | Evidence | Current mitigation | Gap |
|---|---|---|---|
| External tool and DLL provenance | Bundled/locally referenced tools and libraries | Manual project knowledge | No single normalized dependency story |
| Device and filesystem operations are high-impact | Repo performs real backup/copy operations | Tests and explicit plans | Stronger operational guidance is still valuable |

### 4) Performance and Scaling Concerns
| Concern | Evidence | Current symptom | Scaling risk | Suggested improvement |
|---|---|---|---|---|
| Core2 performance path is complex | Pipeline/stage architecture in Core2 | Hard to reason about behavior under load | MTP instability and maintenance cost | Keep Core4 sequential-first and add limited filesystem parallelism later |
| Core4 performance path is not finished yet | `LimitedParallelBackupEngine` is still skeletal | No finished future-safe performance path | Pressure to optimize too early | Finish Tier 1 and Tier 2 before Tier 4 |

### 5) Fragile / High-Churn Areas
| Area | Why fragile | Safe change strategy |
|---|---|---|
| `Directory.Build.props` / `Directory.Build.targets` | Build-number tooling mutates files | Expect diffs after builds/tests and review carefully |
| `BMTP3.Consoles` startup/DI wiring | Active entrypoint into the engine stack | Keep wiring changes explicit and test command paths |
| `BMTP3.Core2\BackupNew\Engine\` | Active engine with historical complexity | Prefer focused changes and preserve behavior carefully |
| `BMTP3.Core4\` | Reimplementation target with evolving docs/contracts | Always cross-check against normalized Core4 docs before changing |

### 6) Open Questions
1. Should local `MediaDevices.dll` references be replaced with a more reproducible dependency story?
2. At what point should Core4 become the primary engine target in `BMTP3.Consoles`?
3. Which parts of Core2 should remain active long-term versus become reference-only?
