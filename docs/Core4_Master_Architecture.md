# BMTP3.Core4 — Master Architecture Document

> **Status:** Living design document. Updated as implementation progresses.
> **Version:** 1.0.0 (initial)
> **Audience:** Contributors, maintainers, and anyone onboarding to the Core4 backup engine.

---

## Table of Contents

1. [Introduction and Purpose](#1-introduction-and-purpose)
2. [Project Structure](#2-project-structure)
3. [Public API Design](#3-public-api-design)
4. [BackupPlan — the Input Contract](#4-backupplan--the-input-contract)
5. [BackupResult — the Output Contract](#5-backupresult--the-output-contract)
6. [BackupItem — Internal Per-Item State](#6-backupitem--internal-per-item-state)
7. [BackupPhase — Execution Phases](#7-backupphase--execution-phases)
8. [Engine Architecture — Three Engines](#8-engine-architecture--three-engines)
9. [SequentialBackupEngine — Implementation Plan](#9-sequentialbackupengine--implementation-plan)
10. [BackupSessionState](#10-backupsessionstate)
11. [BackupPlanValidator](#11-backupplanvalidator)
12. [Guard — Null Safety Utilities](#12-guard--null-safety-utilities)
13. [IBackupScanner — Source Discovery](#13-ibackupscanner--source-discovery)
14. [Sidecar File Design](#14-sidecar-file-design)
15. [Progress Reporting Design](#15-progress-reporting-design)
16. [Collision Handling](#16-collision-handling)
17. [MTP Sources — Threading Constraints](#17-mtp-sources--threading-constraints)
18. [DependencyInjection Registration](#18-dependencyinjection-registration)
19. [Lessons Learned from Core2 and Core3](#19-lessons-learned-from-core2-and-core3)
20. [Testing Strategy](#20-testing-strategy)
21. [Roadmap and Open Questions](#21-roadmap-and-open-questions)

---

## ⚠️ Skeleton vs. Document Discrepancies

> **Read this section first.** This document synthesizes the ideal Core4 design from `CORE4_ARCHITECTURE_en.md`, `CORE4_PLAN_en.md`, and lessons learned. However, the actual skeleton code that was checked in diverges from parts of the idealized design in a few places. This section documents those exact discrepancies, what the skeleton actually contains, and what action implementers should take.

### 1. `BackupResult` — Field Set Mismatch

**Skeleton (actual, in `Models/BackupResult.cs`):**
```csharp
public sealed record BackupResult
{
	public string Name { get; init; } = string.Empty;
	public BackupPhase FinalPhase { get; init; }
	public BackupErrorCode? FailureReason { get; init; }
	public int DirectoriesScanned { get; init; }
	public int FilesDiscovered { get; init; }
	public long BytesTotal { get; init; }
	public int FilesProcessed { get; init; }
	public int FilesSucceeded { get; init; }
	public int FilesSkipped { get; init; }
	public int FilesFailed { get; init; }
	public long BytesProcessed { get; init; }
}
```

**This document's design (Section 5):** uses `TotalItems`, `CopiedItems`, `SkippedItems`, `FailedItems`, `Elapsed`, `Errors`.

**Action:** The **skeleton is richer and better**. It explicitly tracks discovery (`FilesDiscovered`, `BytesTotal`, `DirectoriesScanned`) separate from processing, includes `FinalPhase` and `FailureReason` for diagnostics, and tracks bytes processed. The section-5 design in this document describes an earlier, simpler idea that was superseded. Use the skeleton's field set when implementing. When reading Section 5 of this document, treat the field names as conceptual — the skeleton's actual fields are the authoritative contract.

---

### 2. `BackupPhase` — Missing Phases

**Skeleton (actual, in `Models/Enums/BackupPhase.cs`):**
```csharp
public enum BackupPhase
{
	Starting,
	Scanning,
	Transferring,
	Completed,
	Cancelled,
	Failed
}
```

**This document proposes (Section 7):** `Idle, Scanning, Copying, Hashing, MetadataExtraction, Verification, TimestampCorrection, Finalizing, Done`

**Action:** The skeleton is **missing the optional-feature phases** (`Hashing`, `MetadataExtraction`, `Verification`, `TimestampCorrection`). Add them when implementing Tier 3. Also rename `Transferring` → `Copying` is a preference; either name is acceptable, but the skeleton uses `Transferring`. When implementing, add:
```csharp
// Phases to ADD to BackupPhase when implementing Tier 3:
Hashing,               // Computing hashes of source/destination files
MetadataExtraction,    // Reading EXIF and file attributes
Verification,          // Comparing source/destination hashes
TimestampCorrection,   // Applying original timestamps to destination
```
Keep `Starting`, `Scanning`, `Transferring`, `Completed`, `Cancelled`, `Failed` as-is (they match the session state lifecycle exactly).

---

### 3. `BackupItem` — Field Set Mismatch

**Skeleton (actual, in `Models/BackupItem.cs`):**
```csharp
internal sealed class BackupItem
{
	public string Id { get; init; } = string.Empty;       // Unique ID per session
	public string SourcePath { get; init; } = string.Empty;
	public string RelativePath { get; init; } = string.Empty;   // KEY: used for destination calculation
	public string? DestinationPath { get; set; }          // Nullable: set after collision resolution
	public long? SizeBytes { get; init; }                 // Nullable: may be unknown for MTP
	public DateTimeOffset? ModifiedAt { get; init; }      // Nullable: may be unknown
	public BackupItemStatus Status { get; set; } = BackupItemStatus.Pending;
}
```

**This document's design (Section 6):** shows `required string DestinationPath`, no `Id`, no `RelativePath`, no `ModifiedAt`.

**Action:** The **skeleton is more complete and correct**. `RelativePath` is critical (see Design Principle 4). `Id` is required by `BackupSessionState.AddItem` (duplicate detection). `DestinationPath` is nullable because it is computed AFTER collision resolution, not during scanning. Use the skeleton's field set exactly.

---

### 4. `BackupItemStatus` — Slightly Different Values

**Skeleton (actual, in `Models/Enums/BackupItemStatus.cs`):**
```csharp
internal enum BackupItemStatus
{
	Pending,
	Succeeded,
	Skipped,
	Failed
}
```

**This document's design (Section 6):** shows `Pending, Copying, Copied, Skipped, Failed`

**Action:** The skeleton uses `Succeeded` (not `Copied`), and does not have a `Copying` intermediate status. Either is acceptable. If you want an in-flight status for crash recovery (recommended for MTP), add `Transferring` between `Pending` and `Succeeded`. Use the skeleton as the baseline; `Copying/Copied` in this document should be read as `Transferring/Succeeded`.

---

### 5. `IBackupScanner` — Return Type Mismatch

**Skeleton (actual, in `Scanner/IBackupScanner.cs`):**
```csharp
internal interface IBackupScanner
{
	IAsyncEnumerable<BackupItem> ScanAsync(
		BackupPlan plan,
		CancellationToken cancellationToken);
}
```

**This document's design (Section 13):** shows `Task<IReadOnlyList<BackupItem>> ScanAsync(...)`.

**Action:** The **skeleton is correct**. `IAsyncEnumerable<BackupItem>` enables streaming — items are yielded one by one as they are discovered, avoiding buffering the entire list in memory. This is especially important for MTP sources where the device must be kept responsive. When reading Section 13, treat all `Task<IReadOnlyList<BackupItem>>` references as `IAsyncEnumerable<BackupItem>` with `[EnumeratorCancellation]`.

Note: `IBackupScanner` is currently `internal`. It may remain internal (only used within the engine, never exposed to CLI) — this is the recommended approach. The scanner is an implementation detail of the engine.

---

### 6. `CollisionStreategy.cs` — Filename Typo

The file `Models/Enums/CollisionStreategy.cs` has a **typo in the filename** ("Streategy" instead of "Strategy"). The **enum itself** is correctly named `CollisionStrategy`. Do not fix the filename without coordinating with all consumers, as it will break the build. Leave the typo as-is until a dedicated cleanup task is scheduled.

---

### 7. `BackupPlan` — Field Mapping

The actual skeleton uses a **single `Source` field** (not `SourceDirectory` + `DeviceId`). For filesystem sources, `Source` is the root directory path. For MTP sources, `Source` is the device name (e.g., `"Apple iPhone"`). The `SourceType` enum (`BackupSourceType.FileSystem` or `BackupSourceType.MediaDevice`) tells the scanner which interpretation to use.

This document uses `Source` consistently throughout — this matches the skeleton. Disregard any references to `SourceDirectory`, `SourcePath`, or `DeviceId` as separate fields; they were from earlier design iterations.

---

### Summary Table

| Skeleton item | Skeleton reality | Document says | Action |
|---|---|---|---|
| `BackupResult` fields | `Name`, `FinalPhase`, `FailureReason?`, `DirectoriesScanned`, `FilesDiscovered`, `BytesTotal`, `FilesProcessed`, `FilesSucceeded`, `FilesSkipped`, `FilesFailed`, `BytesProcessed` | `TotalItems`, `CopiedItems`, `Elapsed`, `Errors` | **Use skeleton** |
| `BackupPhase` values | `Starting`, `Scanning`, `Transferring`, `Completed`, `Cancelled`, `Failed` | `Idle`, `Copying`, `Hashing`, `Finalizing`, `Done` etc. | **Add Hashing/Metadata/Verification phases in Tier 3** |
| `BackupItem` fields | `Id`, `SourcePath`, `RelativePath`, `DestinationPath?`, `SizeBytes?`, `ModifiedAt?`, `Status` | Missing `Id`, `RelativePath`; non-nullable `DestinationPath` | **Use skeleton** |
| `BackupItemStatus` values | `Pending`, `Succeeded`, `Skipped`, `Failed` | `Pending`, `Copying`, `Copied`, `Skipped`, `Failed` | **Use skeleton; optionally add `Transferring`** |
| `IBackupScanner.ScanAsync` return | `IAsyncEnumerable<BackupItem>` | `Task<IReadOnlyList<BackupItem>>` | **Use skeleton (streaming is correct)** |
| `CollisionStreategy.cs` filename | Has typo "Streategy" | N/A | **Leave typo; `CollisionStrategy` enum name is correct** |
| `BackupPlan.Source` | Single `string Source` | Varies | **Use skeleton's `Source` + `SourceType`** |

---

## 1. Introduction and Purpose

`BMTP3.Core4` is the fourth generation backup engine in the BMTP3 family of projects. It was created to provide a clean, well-structured, and thoroughly testable foundation for backing up media files from both filesystem (MSC, Mass Storage Class) sources and MTP (Media Transfer Protocol) devices such as smartphones and cameras running over USB.

### Why a Fourth Generation?

The BMTP3 project has gone through several evolutionary cycles, each teaching important lessons about the right balance between power and simplicity. `BMTP3.Core` (the original implementation) was a monolith that combined both the command-line interface and the backup logic in a single project. This made it difficult to test the engine in isolation and impossible to swap out the CLI front-end. `BMTP3.Core2` introduced the pipeline-based approach — a sophisticated streaming architecture built on `System.Threading.Channels` that parallelised work across bounded channels. It was correct and fast, but the channel-based pipeline introduced significant conceptual overhead: contributors had to understand `ChannelReader<T>`, `ChannelWriter<T>`, `AbstractPipelineStage<TContext>`, progress tracking via `Interlocked` counters, and the nuances of bounded vs unbounded channels before they could make any meaningful contribution. `BMTP3.Core3` attempted to simplify Core2 but did not ship fully.

`BMTP3.Core4` is the response to all of these lessons. It starts from the simplest thing that can possibly work — a sequential, single-threaded engine — and adds complexity incrementally and deliberately. The guiding principle is: **correctness first, clarity over performance, testability at every layer**.

### Scope and Platform Requirements

Core4 is a **Windows-only** library targeting `net8.0-windows`. This is a hard constraint driven by two factors:

1. The `MediaDevices` library used for MTP communication relies on Windows COM infrastructure and cannot run on Linux or macOS.
2. The broader BMTP3 solution is already Windows-only, and there is no active effort to port it.

Within Windows, Core4 supports two source categories:

- **MSC (Mass Storage Class)**: standard filesystem paths, USB drives, SD cards mounted as drive letters, network shares. These behave like ordinary directories.
- **MTP (Media Transfer Protocol)**: smartphones, cameras, and other devices that present a virtual filesystem over MTP rather than appearing as a drive letter. These require special handling (see [Section 17](#17-mtp-sources--threading-constraints)).

The destination is always a local filesystem path.

### Key Design Goals

**Correctness first.** Every file that is scheduled to be copied must either be copied correctly or have its failure recorded. Partial writes must be detected and cleaned up. Progress tracking must be accurate.

**Clarity over performance.** When there is a trade-off between an approach that is faster but harder to understand and one that is slower but obviously correct, Core4 chooses the obvious approach. Performance can be improved later; incorrect behaviour is much harder to detect and fix.

**Testability at every layer.** All non-trivial logic is behind interfaces. Scanners, sidecar writers, and progress reporters are all injected. The engine itself can be tested without touching real files by providing mock implementations.

**Idiomatic C# 12.** Core4 uses modern C# features throughout: `sealed record` for immutable data, primary constructors where they reduce noise, collection expressions (`[]`) for empty collections, `required` properties to enforce initialization at compile time, and `[CallerArgumentExpression]` in guard utilities.

**Incremental complexity.** The engine starts as a single sequential implementation. Parallelism is added in subsequent phases, controlled by the same `BackupPlan.Parallelism` property. The public API does not change between phases.

### How to Use This Document

This document serves two audiences simultaneously.

If you are **onboarding** to Core4 for the first time, read Sections 1 through 7 in order. They cover the public API, the data model, and the execution phases. By the end of Section 7 you will have a complete mental model of what Core4 does and how it is structured.

If you are **contributing** to Core4, continue with Sections 8 through 16. These cover the internal engine architecture, the scanner, sidecar writing, progress reporting, and collision handling. Section 17 covers the special case of MTP sources. Section 18 covers dependency injection registration.

If you are **reviewing the design history** or making architectural decisions, read Sections 19 through 21. Section 19 distills the lessons from Core2 and Core3. Section 20 describes the testing strategy. Section 21 is the roadmap.

Cross-references between sections are provided throughout. When a concept is introduced briefly in one section but explained fully elsewhere, a reference is given.

---

## 2. Project Structure

The Core4 project follows a deliberate folder hierarchy that separates public API contracts from internal implementation details. This separation is enforced by C# `internal` access modifiers: only types in the `Api/` and `Models/` folders (plus the `Models/Enums/` subfolder) are `public`. Everything else is `internal`.

### Folder Layout

```
BMTP3.Core4/
│
├── Api/                        ← Public contracts (interfaces)
│   ├── IBackupEngine.cs
│   ├── IBackupProgress.cs
│   └── IFileProgress.cs
│
├── Models/                     ← Public data types
│   ├── BackupPlan.cs
│   ├── BackupResult.cs
│   ├── BackupItem.cs           ← internal (not public)
│   └── Enums/
│       ├── BackupPhase.cs
│       ├── BackupItemStatus.cs ← internal (not public)
│       └── CollisionStreategy.cs  ← filename typo; enum name correct
│
├── Engine/                     ← Execution engines
│   ├── BackupEngine.cs         ← Public facade
│   ├── Sequential/
│   │   └── SequentialBackupEngine.cs
│   ├── LimitedParallel/        ← planned (empty)
│   ├── Parallel/               ← planned (empty)
│   ├── State/
│   │   └── BackupSessionState.cs
│   └── Validation/
│       └── BackupPlanValidator.cs
│
├── Scanner/                    ← Source file discovery
│   └── IBackupScanner.cs
│
├── Sidecar/                    ← Sidecar file writing (planned, empty)
│
├── Progress/                   ← Progress tracking (planned, empty)
│
├── DependencyInjection/        ← Service registration (planned, empty)
│
└── Helpers/
    └── Guard.cs
```

### Api/ — Public Contracts

The `Api/` folder contains the three public interfaces that callers (such as `BMTP3.Consoles`) depend on. These interfaces form the stable boundary between Core4 and its consumers. Callers should only reference these interfaces and the types in `Models/`; they should never reference engine-internal types.

- **`IBackupEngine`**: the single entry point. Accepts a `BackupPlan`, an `IProgress<IBackupProgress>`, and a `CancellationToken`. Returns a `BackupResult`.
- **`IBackupProgress`**: the progress snapshot interface. Consumers implement progress UI against this.
- **`IFileProgress`**: per-file byte-level progress for large file transfers (consumed by UI components that show a per-file progress bar).

### Models/ — Data Types

The `Models/` folder holds the data types exchanged across the API boundary. Most are public; two (`BackupItem` and `BackupItemStatus`) are internal because they represent per-item mutable tracking state that has no business leaking into the caller's domain.

Enums live in `Models/Enums/` to keep the top-level `Models/` clean.

### Engine/ — Execution Engines

The `Engine/` folder contains all execution logic. The public `BackupEngine` class is a dispatcher facade. Internal engines live in subfolders:

- `Sequential/`: the first concrete implementation. Processes items one at a time. Safe for both MTP and filesystem sources.
- `LimitedParallel/`: planned. Uses `SemaphoreSlim` to bound concurrency. Filesystem only.
- `Parallel/`: planned. Unrestricted concurrency or `Task.WhenAll`-based. Filesystem only, future work.

Supporting infrastructure lives in further subfolders:

- `State/`: `BackupSessionState`, which owns all mutable counters for a run.
- `Validation/`: `BackupPlanValidator`, which checks the plan before any IO begins.

### Scanner/ — Source Discovery

The `Scanner/` folder defines `IBackupScanner`, the internal interface for discovering files to back up. Concrete implementations (planned: `FileSystemScanner`, `MtpScanner`) will live here as well. The scanner is injected into the engine, making the engine testable without real filesystem or device access.

### Sidecar/, Progress/, DependencyInjection/ — Planned

These three folders are empty in the initial skeleton. Their presence signals clear intent:

- `Sidecar/`: will contain `ISidecarWriter` and `JsonSidecarWriter`. See [Section 14](#14-sidecar-file-design).
- `Progress/`: will contain `ProgressTracker` and `BackupProgressSnapshot`. See [Section 15](#15-progress-reporting-design).
- `DependencyInjection/`: will contain `ServiceCollectionExtensions` with `AddBMTP3Core4(...)`. See [Section 18](#18-dependencyinjection-registration).

Contributors who want to implement any of these areas should start with the corresponding section in this document, then create the appropriate files in the matching folder.

### Helpers/ — Guard Utilities

A single static class, `Guard`, provides null-safety helpers used throughout the internal codebase. These are not public because they are implementation utilities, not contracts. See [Section 12](#12-guard--null-safety-utilities) for full details.

### Rationale for the Layering

The dependency direction is strictly one-way:

```
Callers (BMTP3.Consoles)
        ↓
    Api/ + Models/     ← stable, public, versioned
        ↓
    Engine/            ← internal, may change
        ↓
    Scanner/           ← internal, injected
    Sidecar/           ← internal, injected
    Progress/          ← internal, injected
        ↓
    Helpers/           ← internal, stateless
```

`Engine/` depends on the abstractions in `Scanner/`, `Sidecar/`, and `Progress/` via interfaces. It does not know about specific implementations. This makes all of Engine testable with mocks.

---

## 3. Public API Design

The public API of Core4 is intentionally minimal. Three interfaces and a handful of data types are all a caller ever needs to reference. This minimalism is a deliberate design decision: the fewer types a caller must understand, the easier it is to upgrade Core4 without breaking callers.

### IBackupEngine — The Single Entry Point

```csharp
namespace BMTP3.Core4.Api;

public interface IBackupEngine
{
	Task<BackupResult> RunAsync(BackupPlan plan, IProgress<IBackupProgress> progress, CancellationToken cancellationToken = default);
}
```

`IBackupEngine` has exactly one method: `RunAsync`. This is intentional. A backup engine does one thing — it runs a backup. The method signature follows the standard async pattern established by `Microsoft.Extensions.Hosting`: a `CancellationToken` as the last parameter with a default value, and a `Task<TResult>` return type.

The `plan` parameter carries all configuration for the run (see [Section 4](#4-backupplan--the-input-contract)). It is a `sealed record`, which means it is immutable: the engine cannot accidentally mutate its configuration mid-run.

The `progress` parameter is an `IProgress<IBackupProgress>`. This interface, defined in the BCL, decouples the engine from any specific UI or logging framework. The engine calls `progress.Report(snapshot)` after each significant event (typically after each file is processed). The caller decides what to do with the snapshot — log it, update a progress bar, write to a console, etc. If the caller does not care about progress, they can pass `new Progress<IBackupProgress>(_ => { })` or `Progress<IBackupProgress>.None` equivalent.

The `cancellationToken` parameter follows standard .NET conventions. If the token is cancelled, the engine should stop processing new items as soon as it next checks the token. It should not attempt to roll back already-completed work, but it should clean up any partially written files before returning.

The return type is `Task<BackupResult>`. A backup run always produces a result (even if everything failed), which allows callers to inspect what happened without catching exceptions for normal flow control. Exceptions from `RunAsync` indicate programming errors or unrecoverable conditions (e.g., the destination directory is not accessible at all), not per-item failures.

### IBackupProgress — Progress Snapshots

```csharp
namespace BMTP3.Core4.Api;

public interface IBackupProgress
{
	int TotalItems { get; }
	int CompletedItems { get; }
	int FailedItems { get; }
	int SkippedItems { get; }
	string? CurrentFile { get; }
	BackupPhase CurrentPhase { get; }
	double OverallPercent { get; }
}
```

`IBackupProgress` is a read-only snapshot of the engine's state at a point in time. Every property is a getter only; there is no way to modify a progress snapshot. Callers receive a series of these snapshots via `IProgress<IBackupProgress>.Report(...)`.

`TotalItems` is the total number of files discovered during the scan phase. It becomes available after `BackupPhase.Scanning` completes. Before that it may be 0.

`CompletedItems` is the count of items that have been fully processed, regardless of outcome. An item is "completed" when it has been either copied, skipped, or failed.

`FailedItems` and `SkippedItems` are subsets of `CompletedItems`. `OverallPercent` is derived: `CompletedItems / (double)TotalItems * 100.0`.

`CurrentFile` is the `SourcePath` of the file currently being processed. It is `null` during `Scanning` and after `Done`.

`CurrentPhase` is a `BackupPhase` enum value. See [Section 7](#7-backupphase--execution-phases) for the complete phase lifecycle.

### IFileProgress — Per-File Byte Progress

```csharp
namespace BMTP3.Core4.Api;

public interface IFileProgress
{
	string FileName { get; }
	long BytesTransferred { get; }
	long TotalBytes { get; }
	double Percent { get; }
}
```

`IFileProgress` provides byte-level progress for a single file transfer. This is used by UI components that want to display a per-file progress bar (e.g., `[=====>   ] 45% of photo.jpg (12.3 MB / 27.4 MB)`). The engine will report `IFileProgress` snapshots at regular intervals during the copy of large files. For small files, a single report at completion is sufficient.

### Why sealed records for BackupPlan and BackupResult?

Both `BackupPlan` and `BackupResult` are `sealed record` types. `sealed` prevents inheritance, which prevents callers from creating subtypes with unexpected behaviour. `record` provides value semantics (structural equality), `with`-expression support, and compiler-generated `ToString()`. The combination of `sealed record` with `required` properties enforces that all meaningful fields are provided at construction time, making it impossible to create an instance in an invalid partial state.

### Why BackupPhase is a Public Enum

`BackupPhase` is public because it appears in `IBackupProgress.CurrentPhase`, which is a public interface. If `BackupPhase` were internal, callers would receive an `IBackupProgress` object with a `CurrentPhase` property they could not name or switch on — which would be useless.

### Summary Table of Public Types

| Type | Kind | Namespace | Purpose |
|---|---|---|---|
| `IBackupEngine` | Interface | `BMTP3.Core4.Api` | Entry point for running a backup |
| `IBackupProgress` | Interface | `BMTP3.Core4.Api` | Progress snapshot consumed by UI |
| `IFileProgress` | Interface | `BMTP3.Core4.Api` | Per-file byte progress |
| `BackupPlan` | sealed record | `BMTP3.Core4.Models` | Input configuration for a run |
| `BackupResult` | sealed record | `BMTP3.Core4.Models` | Output summary of a completed run |
| `BackupPhase` | enum | `BMTP3.Core4.Models` | Execution phase reported via progress |
| `CollisionStrategy` | enum | `BMTP3.Core4.Models` | What to do when a destination file already exists |

---

## 4. BackupPlan — the Input Contract

`BackupPlan` is the configuration object passed to `IBackupEngine.RunAsync`. It carries everything the engine needs to know about a backup run before it begins. Once constructed, it is immutable — the engine cannot change it, and neither can the caller.

```csharp
namespace BMTP3.Core4.Models;

public sealed record BackupPlan
{
	public required string Source { get; init; }
	public required string Destination { get; init; }
	public bool Recursive { get; init; } = true;
	public CollisionStrategy CollisionStrategy { get; init; } = CollisionStrategy.Skip;
	public bool WriteSidecar { get; init; } = true;
	public int Parallelism { get; init; } = 1;
}
```

### Source — The Input Location

`Source` is a single string that identifies where files should be read from. It is `required`, meaning a `BackupPlan` cannot be constructed without specifying it. The engine interprets this string differently depending on what it looks like:

- If it looks like an absolute filesystem path (`C:\`, `D:\`, `\\server\share\`), it is treated as an MSC source and the `FileSystemScanner` is used.
- If it does not look like a filesystem path (e.g., `"Apple iPhone"`, `"Samsung Galaxy S23"`), it is treated as an MTP device name and the `MtpScanner` is used.

This single-string approach is a deliberate simplification. The original `BMTP3.Core` supported multiple sources (an array of `DeviceSources` and `DriveSources` defined in TOML). Core4 explicitly does not support multiple sources in a single `BackupPlan`. If a caller wants to back up from multiple sources, they construct multiple `BackupPlan` objects and call `RunAsync` for each one. This keeps the engine's state machine simple: there is always exactly one source, one destination, and one run.

The `BackupPlanValidator` (see [Section 11](#11-backupplanvalidator)) rejects null or whitespace `Source` values before any IO begins.

### Destination — Where Files Go

`Destination` is always a local filesystem path. It is `required`. The engine will create the destination directory if it does not exist. All files are placed under this root, maintaining the relative structure of the source.

For example, if `Source` is `D:\DCIM` and `Destination` is `C:\Backup`, a file at `D:\DCIM\Camera\IMG_1234.jpg` ends up at `C:\Backup\Camera\IMG_1234.jpg`.

### Recursive — Include Subdirectories

`Recursive` controls whether the scanner descends into subdirectories. The default is `true` because the overwhelming majority of real backup scenarios involve the entire source tree. Setting `Recursive = false` limits the scan to the immediate children of `Source`.

For MTP devices, "recursion" means traversing the virtual filesystem tree exposed by the device. For filesystem sources, it maps directly to `SearchOption.AllDirectories` vs `SearchOption.TopDirectoryOnly` in `Directory.EnumerateFiles`.

### CollisionStrategy — What to Do When a File Already Exists

`CollisionStrategy` tells the engine what to do when the computed destination path already exists. The default is `CollisionStrategy.Skip`, which is the safest option: if a file is already there, leave it alone and record the item as skipped.

See [Section 16](#16-collision-handling) for a complete description of all three strategies and the code that implements them.

### WriteSidecar — Companion JSON Files

`WriteSidecar` controls whether the engine writes a small JSON companion file alongside each successfully copied media file. The default is `true` because sidecars are Core4's primary mechanism for making a backup run idempotent: if a run is interrupted and restarted, the sidecar files let a future run (or a separate verification pass) confirm which files were already processed.

Setting `WriteSidecar = false` is appropriate when integrating Core4 into a context where sidecar files would be unwanted (e.g., writing directly to a read-only NAS share, or in test scenarios where sidecar noise is undesirable).

See [Section 14](#14-sidecar-file-design) for the full sidecar design.

### Parallelism — Degree of Concurrency

`Parallelism` controls how many files the engine processes concurrently. The default is `1`, meaning strictly sequential processing. This is the safest default: sequential processing works correctly for both MTP and filesystem sources, and the performance is usually adequate for the data volumes involved in media backup.

Callers who want faster throughput from filesystem sources can increase `Parallelism`. The engine will clamp this to `1` for MTP sources regardless of the value provided (see [Section 17](#17-mtp-sources--threading-constraints)).

The `BackupPlanValidator` rejects `Parallelism < 1`.

### Constructing a BackupPlan

Because all optional properties have defaults, a minimal `BackupPlan` can be constructed with just `Source` and `Destination`:

```csharp
BackupPlan plan = new BackupPlan
{
	Source = @"D:\DCIM",
	Destination = @"C:\Backup\Photos"
};
```

A fully explicit plan looks like this:

```csharp
BackupPlan plan = new BackupPlan
{
	Source = @"D:\DCIM",
	Destination = @"C:\Backup\Photos",
	Recursive = true,
	CollisionStrategy = CollisionStrategy.Rename,
	WriteSidecar = true,
	Parallelism = 4
};
```

The `with` expression (from `record`) allows deriving a modified copy:

```csharp
BackupPlan fastPlan = plan with { Parallelism = 8, WriteSidecar = false };
```

---

## 5. BackupResult — the Output Contract

`BackupResult` is the value returned by `IBackupEngine.RunAsync` when the run completes (normally or after cancellation). It is a `sealed record`, making it structurally equal and immutable. The engine constructs exactly one `BackupResult` per call to `RunAsync`.

```csharp
namespace BMTP3.Core4.Models;

public sealed record BackupResult
{
	public required int TotalItems { get; init; }
	public required int CopiedItems { get; init; }
	public required int SkippedItems { get; init; }
	public required int FailedItems { get; init; }
	public required TimeSpan Elapsed { get; init; }
	public IReadOnlyList<string> Errors { get; init; } = [];
}
```

### Immutable Output Rationale

Making `BackupResult` a `sealed record` with `required init` properties serves two purposes. First, it prevents the engine from accidentally modifying the result after construction. Second, it prevents callers from modifying the result either — the counters they receive are final. This makes test assertions straightforward: compare the `BackupResult` directly using structural equality, or inspect individual properties.

### TotalItems, CopiedItems, SkippedItems, FailedItems

These four counters are the primary summary of the run. They satisfy the invariant:

```
CopiedItems + SkippedItems + FailedItems == CompletedItems (from progress)
TotalItems == CopiedItems + SkippedItems + FailedItems (if run completed normally)
```

If the run was cancelled mid-way, `TotalItems` will be greater than `CopiedItems + SkippedItems + FailedItems`, because not all items were processed.

All four are `required`, so the engine must explicitly supply them. This makes it impossible to accidentally return a default-value result.

### Elapsed

`Elapsed` is a `TimeSpan` measuring the wall-clock duration of the run, from the moment `RunAsync` begins (after validation) to the moment the last item is processed and the result is constructed. It does not include time spent in `BackupPlanValidator.Validate`.

### Errors — Per-Item Error Messages

`Errors` is an `IReadOnlyList<string>` containing human-readable error messages for items that failed. The default value is `[]` (an empty collection expression, a C# 12 feature). If no items failed, `Errors` is empty and `FailedItems` is zero.

Each error message in `Errors` should include the source path of the file that failed and the reason for the failure. A reasonable format is:

```
"Failed to copy 'D:\DCIM\Camera\IMG_1234.jpg': Access to the path is denied."
```

The `Errors` list is read-only but is constructed as a `List<string>` by the engine and then exposed via the `IReadOnlyList<string>` interface. This avoids the allocation cost of `ImmutableList<T>` while still preventing callers from modifying the list.

### How the Engine Builds BackupResult

At the end of a run, `SequentialBackupEngine` (and future parallel engines) will construct the result from the `BackupSessionState`. This is a design sketch of how that construction looks:

```csharp
// Design sketch — not yet implemented
BackupResult BuildResult(BackupSessionState state, List<string> errors, TimeSpan elapsed)
{
	return new BackupResult
	{
		TotalItems = state.Items.Count,
		CopiedItems = state.Copied,
		SkippedItems = state.Skipped,
		FailedItems = state.Failed,
		Elapsed = elapsed,
		Errors = errors
	};
}
```

The `BackupSessionState` (see [Section 10](#10-backupsessionstate)) maintains the counters in a thread-safe way during the run. At the end, the engine reads the final counter values and freezes them into the immutable `BackupResult`.

---

## 6. BackupItem — Internal Per-Item State

`BackupItem` represents a single file to be backed up. It is `internal`, which means it is invisible to callers. It exists only within the engine and the scanner, used to track the per-file lifecycle from discovery through completion.

```csharp
namespace BMTP3.Core4.Models;

internal sealed class BackupItem
{
	public required string SourcePath { get; init; }
	public required string DestinationPath { get; init; }
	public required long SizeBytes { get; init; }
	public BackupItemStatus Status { get; set; } = BackupItemStatus.Pending;
	public string? ErrorMessage { get; set; }
}
```

### Why a Class, Not a Record?

`BackupItem` is a `class` (not a `record`) because it has mutable properties: `Status` and `ErrorMessage`. A `record` with mutable properties is possible but misleading — the `record` keyword implies value semantics and `with`-expression support, neither of which is appropriate for a mutable per-item tracker. Using a `class` makes the mutability explicit and expected.

It is `sealed` to prevent inheritance, which would complicate the status machine reasoning.

### SourcePath and DestinationPath

`SourcePath` is the full, absolute path of the source file as discovered by the scanner. For filesystem sources, this is a standard Windows path. For MTP sources, this is the virtual path within the device's filesystem (e.g., `\Phone\DCIM\Camera\IMG_1234.jpg`).

`DestinationPath` is the full, absolute path of the destination file. It is computed by the scanner based on `BackupPlan.Destination` and the relative portion of `SourcePath`. The collision handler may modify `DestinationPath` if `CollisionStrategy.Rename` is in effect (see [Section 16](#16-collision-handling)).

### SizeBytes

`SizeBytes` is the size of the source file in bytes. The scanner reads this from the filesystem or MTP device metadata at scan time. It is used for two purposes:

1. **Progress calculation**: the engine can compute byte-weighted overall progress rather than just item-count-weighted progress.
2. **Post-copy verification**: after copying, the engine can confirm that the destination file has the same size as the source, as a quick sanity check before computing a hash.

### BackupItemStatus — The Item Lifecycle

`BackupItemStatus` is an internal enum that tracks where an item is in its lifecycle:

```csharp
namespace BMTP3.Core4.Models;

internal enum BackupItemStatus
{
	Pending,
	Copying,
	Copied,
	Skipped,
	Failed
}
```

The lifecycle is a finite state machine:

```
Pending → Copying → Copied
                  ↘ Failed

Pending → Skipped  (collision strategy = Skip, file already exists)
```

The engine sets `Status = BackupItemStatus.Copying` before beginning the file transfer, and sets it to `Copied`, `Skipped`, or `Failed` when the outcome is known. Setting `Status = Copying` before the transfer ensures that if a crash occurs during transfer, the status is clearly not `Pending` — which could help a future crash-recovery pass distinguish in-progress items from unstarted ones.

### ErrorMessage

`ErrorMessage` is set when `Status = Failed`. It contains the exception message (and possibly stack trace information) describing why the copy failed. This message is collected by the engine and appended to the `BackupResult.Errors` list at the end of the run.

### How BackupSessionState Aggregates Items

`BackupSessionState` (see [Section 10](#10-backupsessionstate)) holds the canonical list of `BackupItem` objects for a run. The scanner returns items to the engine, which hands them to `BackupSessionState.AddItems(...)`. From that point on, the engine iterates over `state.Items`, updating each item's `Status` and calling `state.RecordCopied()`, `state.RecordSkipped()`, or `state.RecordFailed()` as appropriate.

---

## 7. BackupPhase — Execution Phases

`BackupPhase` is a public enum that describes the high-level phase of a backup run. It is published via `IBackupProgress.CurrentPhase`, allowing callers to display meaningful status messages like "Scanning…", "Copying…", or "Verifying…".

```csharp
namespace BMTP3.Core4.Models;

public enum BackupPhase
{
	Idle,
	Scanning,
	Copying,
	Hashing,
	MetadataExtraction,
	Verification,
	TimestampCorrection,
	Finalizing,
	Done
}
```

### Phase Descriptions

**Idle**: The engine has been constructed but `RunAsync` has not been called, or the engine has finished a previous run. This is the initial state reported in the very first progress snapshot (before any work begins).

**Scanning**: `IBackupScanner.ScanAsync` is executing. The engine is discovering files in the source. During this phase, `TotalItems` is not yet known (it is reported as 0 or a running count, depending on the scanner implementation). `CurrentFile` is null.

**Copying**: The engine is transferring a file from source to destination. `CurrentFile` is set to the `SourcePath` of the file being copied. `IFileProgress` snapshots may be reported during this phase for large files.

**Hashing**: A cryptographic hash (MD5 or SHA-256, to be decided — see [Section 21](#21-roadmap-and-open-questions)) is being computed over the source and/or destination file. This may happen before copying (to detect unchanged files), after copying (to verify the copy), or both.

**MetadataExtraction**: EXIF metadata or filesystem timestamps are being read from the source file. This typically happens before copying, to gather information that will be written to the sidecar or applied to the destination.

**Verification**: The hash computed over the destination file is being compared to the hash computed over the source file. If they do not match, the item is marked as `Failed` and the destination file is deleted.

**TimestampCorrection**: The original creation/modification timestamps from the source are being applied to the destination file. Windows filesystem timestamps are not always preserved during a copy, especially for MTP sources. This phase corrects them.

**Finalizing**: The sidecar file is being written (if `WriteSidecar = true`), temporary resources are being cleaned up, and the engine is preparing to return `BackupResult`.

**Done**: The run has completed. No more items will be processed. `CurrentFile` is null. The final `IBackupProgress` snapshot is reported with this phase.

### Why Phases Matter for Progress Reporting

A caller that simply counts items can display a rough percentage bar, but it cannot display meaningful status text. The `CurrentPhase` value allows the caller to map each phase to a user-friendly string:

```csharp
// Design sketch — caller-side phase display
string PhaseToString(BackupPhase phase)
{
	return phase switch
	{
		BackupPhase.Idle => "Ready",
		BackupPhase.Scanning => "Scanning source…",
		BackupPhase.Copying => "Copying files…",
		BackupPhase.Hashing => "Computing hashes…",
		BackupPhase.MetadataExtraction => "Reading metadata…",
		BackupPhase.Verification => "Verifying copies…",
		BackupPhase.TimestampCorrection => "Fixing timestamps…",
		BackupPhase.Finalizing => "Finalizing…",
		BackupPhase.Done => "Complete",
		_ => phase.ToString()
	};
}
```

### Order of Phases in a Full Run

Not all phases are mandatory. The minimal run (no hashing, no metadata extraction, no verification) goes through:

```
Idle → Scanning → Copying → Finalizing → Done
```

A full run with all features enabled goes through:

```
Idle → Scanning → MetadataExtraction → Hashing (source) → Copying → Hashing (destination)
     → Verification → TimestampCorrection → Finalizing → Done
```

In the `SequentialBackupEngine`, these phases transition per-item: for each item, the engine transitions through the applicable phases before moving to the next item. The progress snapshot always reflects the current item's phase.

---

## 8. Engine Architecture — Three Engines

The engine layer in Core4 is designed around a public facade (`BackupEngine`) that dispatches to one of several internal engines depending on the `BackupPlan`. This design keeps the public API surface stable while allowing the internal engines to evolve independently.

### BackupEngine — The Public Facade

```csharp
namespace BMTP3.Core4.Engine;

public sealed class BackupEngine : IBackupEngine
{
	public Task<BackupResult> RunAsync(BackupPlan plan, IProgress<IBackupProgress> progress, CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}
}
```

`BackupEngine` is the only `public` class in the `Engine/` folder. Callers construct it (typically via DI) and call `RunAsync`. The `BackupEngine` is responsible for:

1. Validating the plan (calling `BackupPlanValidator.Validate`).
2. Selecting the appropriate internal engine based on the plan.
3. Forwarding the call to the selected internal engine.
4. Returning the result.

The internal engine selection logic will look at two things:

- **Source type**: is `BackupPlan.Source` a filesystem path or an MTP device name?
- **Parallelism**: is `BackupPlan.Parallelism` 1, or greater than 1?

MTP sources always use `SequentialBackupEngine`, regardless of `Parallelism`. Filesystem sources with `Parallelism = 1` use `SequentialBackupEngine`. Filesystem sources with `Parallelism > 1` use `LimitedParallelBackupEngine` (once implemented). The full parallel engine is reserved for future use.

This dispatch logic is not yet implemented (the current code throws `NotImplementedException`). When it is implemented, it will look approximately like this:

```csharp
// Design sketch — planned dispatch logic in BackupEngine
public async Task<BackupResult> RunAsync(BackupPlan plan, IProgress<IBackupProgress> progress, CancellationToken cancellationToken = default)
{
	BackupPlanValidator.Validate(plan);

	bool isMtpSource = !Path.IsPathRooted(plan.Source);
	int effectiveParallelism = isMtpSource ? 1 : plan.Parallelism;

	IBackupEngine internalEngine = effectiveParallelism <= 1
		? _sequentialEngine
		: _limitedParallelEngine;

	return await internalEngine.RunAsync(plan, progress, cancellationToken);
}
```

### SequentialBackupEngine — Single-Threaded, Always Correct

`SequentialBackupEngine` is the first concrete implementation and the only one that currently has a skeleton. It processes items one at a time, in the order returned by the scanner. It is correct for both MTP and filesystem sources. See [Section 9](#9-sequentialbackupengine--implementation-plan) for the full implementation plan.

### LimitedParallelBackupEngine — Bounded Concurrency (Planned)

`LimitedParallelBackupEngine` will use `SemaphoreSlim` to allow up to `BackupPlan.Parallelism` concurrent file copies. It will be implemented in `Engine/LimitedParallel/`. This engine is safe only for filesystem sources; it must never be used with MTP sources. Its internal structure will mirror `SequentialBackupEngine` but wrap the per-item processing in `Task.Run` guarded by a semaphore.

### ParallelBackupEngine — Unrestricted Concurrency (Planned)

`ParallelBackupEngine` (in `Engine/Parallel/`) is reserved for future phases. It may use `Task.WhenAll`, `Parallel.ForEachAsync`, or a channel-based approach. Its design will be informed by the lessons learned from Core2's channel pipeline (see [Section 19](#19-lessons-learned-from-core2-and-core3)).

### Why the Public API Hides Internal Engine Selection

Callers of `IBackupEngine` should not care which internal engine is used. They provide a plan with their desired `Parallelism` setting, and the engine makes the right choice. If the caller asks for `Parallelism = 8` with an MTP source, the engine silently clamps to 1 and logs a diagnostic message. This is a "be conservative" principle: the caller's intent (fast backup) is partially honoured (items are still backed up), but safety is not compromised.

This also means that if a new internal engine is introduced in a future phase (e.g., a channel-based pipeline), the public API does not change. The dispatch logic in `BackupEngine` is updated, and existing callers continue to work without modification.

---

## 9. SequentialBackupEngine — Implementation Plan

`SequentialBackupEngine` is the heart of Core4's first phase. Its current state is a skeleton that scans items but then throws `NotImplementedException`. This section describes the full planned implementation in detail.

```csharp
namespace BMTP3.Core4.Engine.Sequential;

internal sealed class SequentialBackupEngine : IBackupEngine
{
	private readonly IBackupScanner _scanner;

	public SequentialBackupEngine(IBackupScanner scanner)
	{
		_scanner = scanner;
	}

	public async Task<BackupResult> RunAsync(BackupPlan plan, IProgress<IBackupProgress> progress, CancellationToken cancellationToken = default)
	{
		IReadOnlyList<BackupItem> items = await _scanner.ScanAsync(plan, cancellationToken);
		throw new NotImplementedException();
	}
}
```

### Step 1: Validate the Plan

Before any IO, the engine validates the plan. Although `BackupEngine` (the public facade) calls `BackupPlanValidator.Validate` before dispatching, `SequentialBackupEngine` should also validate as a defensive measure — it may be constructed and called directly in tests.

```csharp
// Design sketch — validation in SequentialBackupEngine
BackupPlanValidator.Validate(plan);
```

### Step 2: Start the Stopwatch

The elapsed time is measured using `System.Diagnostics.Stopwatch`, which is the most accurate timer available in .NET for short durations. The stopwatch starts before scanning and stops after the last item is processed.

```csharp
// Design sketch — timing
System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
```

### Step 3: Report Scanning Phase

Before calling the scanner, the engine reports a progress snapshot with `CurrentPhase = BackupPhase.Scanning` and `TotalItems = 0`.

```csharp
// Design sketch — initial progress report
progress.Report(new BackupProgressSnapshot
{
	TotalItems = 0,
	CompletedItems = 0,
	FailedItems = 0,
	SkippedItems = 0,
	CurrentFile = null,
	CurrentPhase = BackupPhase.Scanning,
	OverallPercent = 0.0
});
```

### Step 4: Scan

The scanner is called and returns the full list of items. Scanning is always awaited to completion before processing begins. This is a deliberate simplification: it avoids the complexity of a streaming producer-consumer model. The trade-off is that for very large sources (tens of thousands of files), there is a delay before copying begins. In practice, for media backup scenarios, this delay is acceptable.

```csharp
// Design sketch — scan
IReadOnlyList<BackupItem> items = await _scanner.ScanAsync(plan, cancellationToken);
BackupSessionState state = new BackupSessionState();
state.AddItems(items);
```

After scanning, `TotalItems` is known and the engine reports an updated progress snapshot.

### Step 5: Process Each Item

The engine loops over `state.Items` in order. Before each item, the cancellation token is checked:

```csharp
// Design sketch — per-item loop
List<string> errors = [];

foreach (BackupItem item in state.Items)
{
	cancellationToken.ThrowIfCancellationRequested();

	item.Status = BackupItemStatus.Copying;

	progress.Report(BuildSnapshot(state, item, BackupPhase.Copying));

	try
	{
		bool destinationExists = File.Exists(item.DestinationPath);

		if (destinationExists)
		{
			CollisionOutcome outcome = CollisionHandler.Resolve(item, plan.CollisionStrategy);
			if (outcome == CollisionOutcome.Skip)
			{
				item.Status = BackupItemStatus.Skipped;
				state.RecordSkipped();
				progress.Report(BuildSnapshot(state, item, BackupPhase.Copying));
				continue;
			}
			// If Rename, item.DestinationPath has been updated by CollisionHandler.Resolve
		}

		Directory.CreateDirectory(Path.GetDirectoryName(item.DestinationPath)!);
		File.Copy(item.SourcePath, item.DestinationPath, overwrite: plan.CollisionStrategy == CollisionStrategy.Overwrite);

		if (plan.WriteSidecar)
		{
			// ISidecarWriter.Write(item) — planned
		}

		item.Status = BackupItemStatus.Copied;
		state.RecordCopied();
	}
	catch (Exception ex)
	{
		item.Status = BackupItemStatus.Failed;
		item.ErrorMessage = ex.Message;
		errors.Add($"Failed to copy '{item.SourcePath}': {ex.Message}");
		state.RecordFailed();
	}

	progress.Report(BuildSnapshot(state, item, BackupPhase.Copying));
}
```

### Step 6: Finalizing and Result

After the loop, the engine reports a `Finalizing` snapshot, then a `Done` snapshot, stops the stopwatch, and builds the `BackupResult`:

```csharp
// Design sketch — result construction
stopwatch.Stop();
progress.Report(BuildSnapshot(state, null, BackupPhase.Done));

return new BackupResult
{
	TotalItems = state.Items.Count,
	CopiedItems = state.Copied,
	SkippedItems = state.Skipped,
	FailedItems = state.Failed,
	Elapsed = stopwatch.Elapsed,
	Errors = errors
};
```

### Sidecar Must Be Written Immediately After Each File

A critical correctness requirement: the sidecar file is written immediately after each file is copied, inside the try/catch block. It is not batched at the end of the run. This ensures that if the process is killed mid-run, every file that was successfully copied has a corresponding sidecar. Without this guarantee, a subsequent verification pass cannot determine which files were copied in the interrupted run.

### Error Handling Strategy

Errors are caught per-item. A failure on one item does not stop the run; the engine continues to the next item. This "best-effort with error collection" strategy is appropriate for backup scenarios: it is more valuable to copy 999 out of 1000 files and report one failure than to abort the entire run on the first error.

The only exceptions that propagate out of `RunAsync` are:
- `OperationCanceledException` (when the cancellation token is cancelled).
- Exceptions from `BackupPlanValidator.Validate` (programming errors, not IO errors).
- Exceptions that indicate the destination is completely inaccessible (e.g., `UnauthorizedAccessException` on the root destination directory).

---

## 10. BackupSessionState

`BackupSessionState` is the mutable accumulator for a single backup run. It is owned exclusively by the engine; no other component holds a reference to it.

```csharp
namespace BMTP3.Core4.Engine.State;

internal sealed class BackupSessionState
{
	private readonly List<BackupItem> _items = [];
	private int _copied;
	private int _skipped;
	private int _failed;

	public IReadOnlyList<BackupItem> Items => _items;
	public int Copied => _copied;
	public int Skipped => _skipped;
	public int Failed => _failed;

	public void AddItems(IReadOnlyList<BackupItem> items) => _items.AddRange(items);

	public void RecordCopied() => Interlocked.Increment(ref _copied);
	public void RecordSkipped() => Interlocked.Increment(ref _skipped);
	public void RecordFailed() => Interlocked.Increment(ref _failed);
}
```

### Purpose and Ownership

`BackupSessionState` has one job: own all mutable state for a run. This includes:

1. The list of `BackupItem` objects discovered during scanning.
2. Three counters: `_copied`, `_skipped`, `_failed`.

By isolating all mutable state in one class, the engine's logic (in `SequentialBackupEngine` and future engines) can be written in a largely functional style that reads from the state and updates it via explicit method calls.

### Thread-Safe Counters via Interlocked

The three counters are `int` fields, incremented via `Interlocked.Increment`. This is thread-safe without locks. In `SequentialBackupEngine`, only one thread is active at a time, so the `Interlocked` calls are not strictly necessary. However, they are used anyway because:

1. They make `BackupSessionState` safe to use with parallel engines without modification.
2. `Interlocked.Increment` on an uncontested `int` is essentially free (a single CPU instruction on x86/x64).
3. Using `Interlocked` makes the thread-safety intent explicit and self-documenting.

### Items List — Not Thread-Safe

The `_items` list is a `List<BackupItem>`, which is not thread-safe. This is acceptable because `AddItems` is called exactly once, during the scan phase, which is always single-threaded (even in parallel engines). After `AddItems` returns, the list is never modified — individual items' `Status` and `ErrorMessage` properties change, but the list itself does not grow or shrink.

In a future parallel engine, items would be processed concurrently but the list would still be populated before any parallel processing begins. The `IReadOnlyList<BackupItem>` return type of `Items` reinforces that callers cannot add or remove items from the outside.

### How It Feeds BackupResult

At the end of a run, the engine reads the final counter values from the state:

```csharp
// Design sketch — reading final state
int total = state.Items.Count;
int copied = state.Copied;
int skipped = state.Skipped;
int failed = state.Failed;
```

Because `Interlocked.Increment` provides a memory barrier, these reads are guaranteed to see the most recent values even in a multi-threaded context.

### Future Extension: ActiveFile Tracking

In a parallel engine, it would be useful to track which files are currently being processed (for display in the progress UI). A future version of `BackupSessionState` might add a `ConcurrentDictionary<string, BackupItem> ActiveItems` field. This is intentionally left out of the initial design to keep the class simple.

---

## 11. BackupPlanValidator

`BackupPlanValidator` is a static class responsible for validating a `BackupPlan` before the engine begins any IO. It is called early in the execution path — in `BackupEngine.RunAsync` (the facade) and defensively in `SequentialBackupEngine.RunAsync`.

```csharp
namespace BMTP3.Core4.Engine.Validation;

internal static class BackupPlanValidator
{
	public static void Validate(BackupPlan plan)
	{
		Guard.NotNull(plan);
		Guard.NotNullOrWhiteSpace(plan.Source, nameof(plan.Source));
		Guard.NotNullOrWhiteSpace(plan.Destination, nameof(plan.Destination));

		if (plan.Parallelism < 1)
			throw new ArgumentOutOfRangeException(nameof(plan.Parallelism), "Parallelism must be at least 1.");
	}
}
```

### Design Rationale: Static Class

`BackupPlanValidator` is a static class with a static `Validate` method. It has no state and no dependencies. Making it a static class rather than an instance class with an `IBackupPlanValidator` interface is a deliberate simplification: validation rules are not expected to vary at runtime, and injecting a validator would add DI complexity without benefit.

If future requirements demand configurable validation (e.g., validating that the source path actually exists on disk), the class can be converted to an injected interface at that point. Premature abstraction here would complicate the engine unnecessarily.

### Guard.NotNull — Plan-Level Null Check

The first call is `Guard.NotNull(plan)`, which checks that the plan itself is not null. This uses `[CallerArgumentExpression]` to automatically capture the expression `"plan"` as the parameter name in the thrown `ArgumentNullException`. See [Section 12](#12-guard--null-safety-utilities) for details.

### Guard.NotNullOrWhiteSpace — Required String Properties

`plan.Source` and `plan.Destination` are checked to be non-null and non-whitespace. An empty string or a string of only spaces is not a valid source or destination path. The explicit `nameof(plan.Source)` and `nameof(plan.Destination)` arguments are passed because the `[CallerArgumentExpression]` attribute captures the expression at the call site, which in this case would be `"plan.Source"` — a reasonable parameter name for the exception message.

### Parallelism >= 1

The only numeric validation is `Parallelism >= 1`. `Parallelism = 0` would mean no workers, which would result in an infinite loop or a deadlock in a semaphore-based parallel engine. The engine enforces this with an explicit `ArgumentOutOfRangeException` that includes a clear message.

### What Validation Does NOT Do (Yet)

The current validator is deliberately minimal. It does not:

- Check that `plan.Source` is a valid filesystem path that exists.
- Check that `plan.Destination` is accessible and writable.
- Check that `plan.Parallelism` is below some reasonable ceiling.
- Check that `plan.Source` and `plan.Destination` are not the same path.

These validations would require IO, which is inappropriate in a validation method (the principle of "fail fast without side effects"). They are candidates for a separate "pre-flight check" method or a `ValidateAsync` that is explicitly IO-permitted. This is listed as an open question in [Section 21](#21-roadmap-and-open-questions).

### Failure Behaviour

Validation failure throws one of three BCL exception types:

| Condition | Exception Type |
|---|---|
| `plan` is null | `ArgumentNullException` |
| `plan.Source` is null or whitespace | `ArgumentException` |
| `plan.Destination` is null or whitespace | `ArgumentException` |
| `plan.Parallelism < 1` | `ArgumentOutOfRangeException` |

These are all programming errors, not runtime errors. They indicate that the caller constructed an invalid `BackupPlan`. The engine does not catch them; they propagate to the caller of `RunAsync`, where they should trigger a bug report or a fix in the calling code.

---

## 12. Guard — Null Safety Utilities

`Guard` is a small internal static helper class that centralises null-safety checks. It is the single place where `ArgumentNullException` and `ArgumentException` (for null/whitespace strings) are thrown.

```csharp
namespace BMTP3.Core4.Helpers;

internal static class Guard
{
	public static void NotNull<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
		where T : class
	{
		if (value is null)
			throw new ArgumentNullException(paramName);
	}

	public static void NotNullOrWhiteSpace([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
	{
		if (string.IsNullOrWhiteSpace(value))
			throw new ArgumentException("Value cannot be null or whitespace.", paramName);
	}
}
```

### CallerArgumentExpression — Automatic Parameter Name Capture

`[CallerArgumentExpression(nameof(value))]` is a C# 10 attribute that causes the compiler to inject the text of the `value` argument expression at the call site as the `paramName` parameter. For example:

```csharp
Guard.NotNull(plan);
// paramName is automatically set to "plan" by the compiler

Guard.NotNullOrWhiteSpace(plan.Source, nameof(plan.Source));
// paramName is explicitly "plan.Source" (the nameof expression)
```

Without `[CallerArgumentExpression]`, every call to `Guard.NotNull` would require passing the parameter name manually:

```csharp
// Without CallerArgumentExpression — verbose and error-prone
if (plan is null) throw new ArgumentNullException(nameof(plan));
```

With `[CallerArgumentExpression]`, the call site is clean:

```csharp
// With CallerArgumentExpression — concise and correct
Guard.NotNull(plan);
```

This removes a common source of copy-paste errors where the wrong `nameof(...)` is passed to `ArgumentNullException`.

### [NotNull] — Roslyn Flow Analysis

The `[NotNull]` attribute (from `System.Diagnostics.CodeAnalysis`) tells Roslyn that after `Guard.NotNull(value)` returns normally, `value` is guaranteed to be non-null. This allows Roslyn's nullable reference type analysis to track that the value is safe to use without the `?` null-conditional operator after the guard call.

```csharp
// Without [NotNull], Roslyn would warn here:
Guard.NotNull(plan);
string source = plan.Source; // warning: 'plan' may be null

// With [NotNull], Roslyn knows plan is non-null after Guard.NotNull returns:
Guard.NotNull(plan);
string source = plan.Source; // no warning
```

### NotNull<T> — Generic, Class Constraint

`NotNull<T>` has a `where T : class` constraint because nullable value types (`int?`, `bool?`, etc.) use `Nullable<T>` and are handled differently. Value types are never null in the traditional reference-type sense; the constraint ensures this method is only used for reference types where null is a meaningful concern.

### NotNullOrWhiteSpace — String-Specific

`NotNullOrWhiteSpace` is a string-specific overload that uses `string.IsNullOrWhiteSpace`, which catches three conditions: null, empty string, and strings containing only whitespace characters (spaces, tabs, newlines). This is the appropriate check for configuration strings like file paths, where `"   "` is just as invalid as `null` or `""`.

### Usage Pattern

The Guard class is used at the top of any method that receives parameters it cannot tolerate being null or whitespace:

```csharp
// Design sketch — Guard usage pattern in an internal class
internal sealed class SomeService
{
	private readonly IBackupScanner _scanner;
	private readonly ISidecarWriter _sidecarWriter;

	public SomeService(IBackupScanner scanner, ISidecarWriter sidecarWriter)
	{
		Guard.NotNull(scanner);
		Guard.NotNull(sidecarWriter);

		_scanner = scanner;
		_sidecarWriter = sidecarWriter;
	}
}
```

### Why Guard is Internal

`Guard` is an implementation utility, not a public contract. Exposing it publicly would encourage callers to use it in their own code, creating an unintended dependency on an internal helper. If callers need null-checking utilities, they should use the BCL directly or use a library like `CommunityToolkit.Diagnostics`. Core4 does not export its internal utilities.

---

## 13. IBackupScanner — Source Discovery

`IBackupScanner` is the internal interface that abstracts source file discovery. It is the component that knows how to talk to a filesystem or an MTP device and enumerate the files that need to be backed up.

```csharp
namespace BMTP3.Core4.Scanner;

internal interface IBackupScanner
{
	Task<IReadOnlyList<BackupItem>> ScanAsync(BackupPlan plan, CancellationToken cancellationToken);
}
```

### Why Internal

`IBackupScanner` is internal because source discovery is an implementation detail of the engine. Callers do not need to know how files are discovered; they provide a `BackupPlan` and receive a `BackupResult`. If `IBackupScanner` were public, callers might be tempted to implement it themselves and pass instances directly to the engine — but the engine's DI registration already handles scanner selection transparently.

The only exception to this would be if a caller has a deeply custom source (e.g., a proprietary device protocol). In that case, the caller can register their own `IBackupScanner` implementation via the DI container. The fact that the interface is internal does not prevent this — internal interfaces are visible within the assembly and can be implemented by types in other assemblies that reference the assembly and have access via `InternalsVisibleTo`.

### ScanAsync — The Single Method

`ScanAsync` returns `Task<IReadOnlyList<BackupItem>>`. The scan completes fully before returning — it does not stream items. This is a deliberate design choice (contrast with Core2's `IBackupScanner.ScanAsync(...)` which yielded items asynchronously via `IAsyncEnumerable<T>` in some iterations). The trade-off: simpler consumer code in the engine (a simple foreach loop), at the cost of holding all items in memory simultaneously.

For typical media backup scenarios (thousands of files, not millions), holding all items in memory is not a concern. Each `BackupItem` is lightweight: two strings and a long.

### Planned: FileSystemScanner

`FileSystemScanner` will be the default `IBackupScanner` implementation for filesystem sources. Its core logic:

```csharp
// Design sketch — FileSystemScanner
internal sealed class FileSystemScanner : IBackupScanner
{
	public Task<IReadOnlyList<BackupItem>> ScanAsync(BackupPlan plan, CancellationToken cancellationToken)
	{
		SearchOption searchOption = plan.Recursive
			? SearchOption.AllDirectories
			: SearchOption.TopDirectoryOnly;

		List<BackupItem> items = [];

		foreach (string sourceFile in Directory.EnumerateFiles(plan.Source, "*", searchOption))
		{
			cancellationToken.ThrowIfCancellationRequested();

			FileInfo fileInfo = new FileInfo(sourceFile);
			string relativePath = Path.GetRelativePath(plan.Source, sourceFile);
			string destinationPath = Path.Combine(plan.Destination, relativePath);

			items.Add(new BackupItem
			{
				SourcePath = sourceFile,
				DestinationPath = destinationPath,
				SizeBytes = fileInfo.Length
			});
		}

		return Task.FromResult<IReadOnlyList<BackupItem>>(items);
	}
}
```

Note the use of `Task.FromResult` because `Directory.EnumerateFiles` is synchronous. There is no async IO here — filesystem enumeration is fast and synchronous on Windows. Wrapping in `Task.FromResult` satisfies the `Task<T>` return type without the overhead of `Task.Run`.

### Planned: MtpScanner

`MtpScanner` will use the `MediaDevices` library to enumerate files on an MTP device. Because MediaDevices uses COM, all calls must happen on the COM STA thread. See [Section 17](#17-mtp-sources--threading-constraints) for the threading details.

The `MtpScanner` will not be registered by default in `AddBMTP3Core4`. Callers that want MTP support must register it explicitly. This mirrors the pattern established in Core2 (where `NoopMediaDeviceScanner` was the default, and real MTP support required caller registration).

### How DestinationPath is Computed

The scanner computes `DestinationPath` by combining `plan.Destination` with the path of the source file relative to `plan.Source`. This ensures that the directory structure of the source is preserved in the destination.

For example:
- `Source = D:\DCIM`
- `Destination = C:\Backup`
- `sourceFile = D:\DCIM\Camera\IMG_1234.jpg`
- `relativePath = Camera\IMG_1234.jpg` (via `Path.GetRelativePath`)
- `destinationPath = C:\Backup\Camera\IMG_1234.jpg` (via `Path.Combine`)

### Why Scanner is Injected

Injecting the scanner into `SequentialBackupEngine` (rather than constructing it inside the engine) is essential for testability. In unit tests, a mock scanner that returns a fixed list of items can be used, eliminating any dependency on real filesystems or MTP devices. This also supports the single responsibility principle: the engine processes items, the scanner discovers them.

---

## 14. Sidecar File Design

A sidecar file is a small JSON companion file written alongside each successfully copied media file. It records provenance information: where the file came from, when it was copied, how large it was, and what hash values were computed. Sidecar files are Core4's primary mechanism for making backup runs auditable and for enabling safe re-runs after interruptions.

### Naming Convention

The sidecar file is placed in the same directory as the destination file. Its filename is the destination filename with `.bmtp3.json` appended:

```
C:\Backup\Camera\IMG_1234.jpg          ← the copied media file
C:\Backup\Camera\IMG_1234.jpg.bmtp3.json  ← the sidecar
```

This naming convention ensures that the sidecar is always findable given the destination path, and that it does not clash with any standard file extension.

### Planned Sidecar Fields

The sidecar is a JSON object with the following planned fields:

```json
{
  "sourcePath": "D:\\DCIM\\Camera\\IMG_1234.jpg",
  "destinationPath": "C:\\Backup\\Camera\\IMG_1234.jpg",
  "copiedAtUtc": "2024-03-15T14:23:45.123Z",
  "sizeBytes": 4567890,
  "sourceHash": "abc123...",
  "destinationHash": "abc123...",
  "collisionStrategyApplied": "Skip",
  "sourceIdentifier": "D:\\DCIM"
}
```

- `sourcePath`: the full source path, for tracing back to origin.
- `destinationPath`: the full destination path (may differ from what was originally planned if `CollisionStrategy.Rename` was applied).
- `copiedAtUtc`: the UTC timestamp of the copy operation.
- `sizeBytes`: the size of the file in bytes.
- `sourceHash`: the hash of the source file (null if hashing was not performed).
- `destinationHash`: the hash of the destination file after copy (null if verification was not performed).
- `collisionStrategyApplied`: which strategy was used for this item (`Skip`, `Overwrite`, or `Rename`). Useful if the plan's strategy changes between runs.
- `sourceIdentifier`: `BackupPlan.Source`, identifying which source produced this file.

### Why Sidecar is Written Immediately After Each File

This is a **crash safety requirement**. If the process is killed between copying file N and file N+1, the sidecar for file N must already be on disk. Without this guarantee, a subsequent run cannot distinguish "file was copied but sidecar was not written (process died)" from "file was not copied at all".

Writing sidecars at the end of the run (in a Finalizing batch) would be simpler to implement but catastrophically unsafe: a crash anywhere in the run would leave zero sidecar files, making the entire run unauditable.

The rule is: **the sidecar is the last thing written per item, inside the per-item try/catch, immediately after `File.Copy` succeeds.**

### Planned ISidecarWriter Interface

```csharp
// Design sketch — ISidecarWriter
namespace BMTP3.Core4.Sidecar;

internal interface ISidecarWriter
{
	Task WriteAsync(BackupItem item, string? sourceHash, string? destinationHash, CancellationToken cancellationToken);
}
```

### Planned JsonSidecarWriter Implementation

```csharp
// Design sketch — JsonSidecarWriter
namespace BMTP3.Core4.Sidecar;

internal sealed class JsonSidecarWriter : ISidecarWriter
{
	private static readonly System.Text.Json.JsonSerializerOptions _options = new System.Text.Json.JsonSerializerOptions
	{
		WriteIndented = true,
		PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
	};

	public async Task WriteAsync(BackupItem item, string? sourceHash, string? destinationHash, CancellationToken cancellationToken)
	{
		SidecarData data = new SidecarData
		{
			SourcePath = item.SourcePath,
			DestinationPath = item.DestinationPath,
			CopiedAtUtc = DateTimeOffset.UtcNow,
			SizeBytes = item.SizeBytes,
			SourceHash = sourceHash,
			DestinationHash = destinationHash
		};

		string sidecarPath = item.DestinationPath + ".bmtp3.json";
		string json = System.Text.Json.JsonSerializer.Serialize(data, _options);
		await File.WriteAllTextAsync(sidecarPath, json, cancellationToken);
	}
}
```

### When WriteSidecar = false

If `BackupPlan.WriteSidecar` is `false`, the engine skips the `ISidecarWriter.WriteAsync` call entirely. No sidecar files are created. This is appropriate for:

- Integration tests where sidecar noise is undesirable.
- Scenarios where the destination is a read-only share.
- Quick "verify what would be copied" passes where no output files should be created.

---

## 15. Progress Reporting Design

Progress reporting in Core4 follows the standard .NET `IProgress<T>` pattern. The engine calls `progress.Report(snapshot)` with an immutable snapshot after each significant event. The caller receives snapshots on the thread that called `Progress<T>.Report` (which, for `System.Progress<T>`, is the synchronization context thread — typically the UI thread in GUI apps or the calling thread in console apps).

### IBackupProgress — Snapshot Fields

Refer to [Section 3](#3-public-api-design) for the interface definition. The key design decision is that every `IBackupProgress` instance is an **immutable snapshot**. The engine does not pass the same mutable object to every `Report` call; it constructs a new snapshot each time. This eliminates race conditions between the engine updating fields and the caller reading them.

### Planned BackupProgressSnapshot

```csharp
// Design sketch — BackupProgressSnapshot
namespace BMTP3.Core4.Progress;

internal sealed class BackupProgressSnapshot : IBackupProgress
{
	public required int TotalItems { get; init; }
	public required int CompletedItems { get; init; }
	public required int FailedItems { get; init; }
	public required int SkippedItems { get; init; }
	public required string? CurrentFile { get; init; }
	public required BackupPhase CurrentPhase { get; init; }

	public double OverallPercent => TotalItems > 0
		? CompletedItems / (double)TotalItems * 100.0
		: 0.0;
}
```

`OverallPercent` is computed as a property rather than stored, because it is derived from `CompletedItems` and `TotalItems`. This avoids the possibility of an inconsistent snapshot where `OverallPercent` does not match the item counts.

### Progress Report Timing

The engine reports progress at the following moments:

1. Before scanning begins (`Phase = Scanning`, `TotalItems = 0`).
2. After scanning completes (`Phase = Scanning` or first `Copying`, `TotalItems = N`).
3. Before each item begins processing (`Phase = Copying`, `CurrentFile = item.SourcePath`).
4. After each item completes processing (regardless of outcome).
5. After all items are processed (`Phase = Done`, `CurrentFile = null`).

This gives callers enough granularity to display a meaningful progress indicator without flooding them with reports. For very large files, additional `IFileProgress` reports may be emitted during the copy (see below).

### IFileProgress — Per-File Byte Progress

```csharp
// Design sketch — FileProgressSnapshot
namespace BMTP3.Core4.Progress;

internal sealed class FileProgressSnapshot : IFileProgress
{
	public required string FileName { get; init; }
	public required long BytesTransferred { get; init; }
	public required long TotalBytes { get; init; }

	public double Percent => TotalBytes > 0
		? BytesTransferred / (double)TotalBytes * 100.0
		: 0.0;
}
```

Per-file progress is reported via a separate `IProgress<IFileProgress>` parameter. This parameter is not currently present in `IBackupEngine.RunAsync` — it may be added in a future phase, or passed as part of a richer options object. The design is not finalised.

### Thread Safety

Because `BackupProgressSnapshot` uses `required init` properties, it is immutable by construction. The engine creates a new instance for each `Report` call. There are no shared mutable fields between the engine thread and the caller's progress handler. This means progress snapshots are inherently thread-safe.

### Planned ProgressTracker (Analogous to Core2)

In Core2, a `ProgressTracker` class maintained the running counters and provided a `GetSnapshot()` method. Core4 will likely adopt the same pattern, especially when the `LimitedParallelBackupEngine` is implemented:

```csharp
// Design sketch — ProgressTracker
namespace BMTP3.Core4.Progress;

internal sealed class ProgressTracker
{
	private int _totalItems;
	private int _completedItems;
	private int _failedItems;
	private int _skippedItems;
	private volatile string? _currentFile;
	private volatile BackupPhase _currentPhase = BackupPhase.Idle;

	public void SetTotalItems(int total) => Interlocked.Exchange(ref _totalItems, total);
	public void IncrementCompleted() => Interlocked.Increment(ref _completedItems);
	public void IncrementFailed() => Interlocked.Increment(ref _failedItems);
	public void IncrementSkipped() => Interlocked.Increment(ref _skippedItems);
	public void SetCurrentFile(string? file) => _currentFile = file;
	public void SetPhase(BackupPhase phase) => _currentPhase = phase;

	public BackupProgressSnapshot GetSnapshot()
	{
		return new BackupProgressSnapshot
		{
			TotalItems = _totalItems,
			CompletedItems = _completedItems,
			FailedItems = _failedItems,
			SkippedItems = _skippedItems,
			CurrentFile = _currentFile,
			CurrentPhase = _currentPhase
		};
	}
}
```

---

## 16. Collision Handling

Collision handling is the logic that decides what to do when the computed destination path for a file already exists on disk. The decision is governed by `BackupPlan.CollisionStrategy`, which has three values: `Skip`, `Overwrite`, and `Rename`.

### CollisionStrategy.Skip — Safe Default

When `CollisionStrategy = Skip`, if the destination file exists, the engine does not copy the source file. The `BackupItem.Status` is set to `BackupItemStatus.Skipped`, `BackupSessionState.RecordSkipped()` is called, and the item is counted in `BackupResult.SkippedItems`.

`Skip` is the default because it is the safest strategy. In most incremental backup scenarios, a file that already exists at the destination was copied in a previous run and should not be overwritten. `Skip` ensures idempotency: running the backup multiple times produces the same destination without duplicating or overwriting anything.

### CollisionStrategy.Overwrite — Replace Unconditionally

When `CollisionStrategy = Overwrite`, if the destination file exists, it is overwritten without any check. The `File.Copy` call uses `overwrite: true`. This strategy is appropriate when the caller knows the source is authoritative and wants to refresh the destination unconditionally (e.g., a re-sync after source edits).

Edge cases to handle:
- If the destination file is read-only (`FileAttributes.ReadOnly` is set), `File.Copy` with `overwrite: true` will throw `UnauthorizedAccessException`. The engine should catch this and record a failure.
- If the destination file is in use by another process, `File.Copy` will throw `IOException`. Same handling.

### CollisionStrategy.Rename — Add Counter Suffix

When `CollisionStrategy = Rename`, if the destination file exists, the engine generates a new destination path by appending a counter suffix to the filename (before the extension). It increments the counter until it finds a path that does not exist:

```
IMG_1234.jpg         → exists → try IMG_1234 (1).jpg
IMG_1234 (1).jpg     → exists → try IMG_1234 (2).jpg
IMG_1234 (2).jpg     → does not exist → use this
```

The `BackupItem.DestinationPath` property is updated to the renamed path. The sidecar is then written to the renamed path as well (the `destinationPath` field in the sidecar JSON reflects the actual destination used).

### Planned CollisionHandler

```csharp
// Design sketch — CollisionHandler
namespace BMTP3.Core4.Engine.Sequential;

internal enum CollisionOutcome
{
	Proceed,
	Skip
}

internal static class CollisionHandler
{
	public static CollisionOutcome Resolve(BackupItem item, CollisionStrategy strategy)
	{
		if (!File.Exists(item.DestinationPath))
			return CollisionOutcome.Proceed;

		switch (strategy)
		{
			case CollisionStrategy.Skip:
				return CollisionOutcome.Skip;

			case CollisionStrategy.Overwrite:
				return CollisionOutcome.Proceed;

			case CollisionStrategy.Rename:
				item.DestinationPath = FindRenamedPath(item.DestinationPath);
				return CollisionOutcome.Proceed;

			default:
				throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
		}
	}

	private static string FindRenamedPath(string destinationPath)
	{
		string directory = Path.GetDirectoryName(destinationPath)!;
		string nameWithoutExtension = Path.GetFileNameWithoutExtension(destinationPath);
		string extension = Path.GetExtension(destinationPath);

		int counter = 1;
		string candidate;

		do
		{
			candidate = Path.Combine(directory, $"{nameWithoutExtension} ({counter}){extension}");
			counter++;
		}
		while (File.Exists(candidate));

		return candidate;
	}
}
```

### Where Collision Detection Happens

Collision detection happens inside the per-item loop in `SequentialBackupEngine`, before the `File.Copy` call. The order is:

1. `CollisionHandler.Resolve(item, plan.CollisionStrategy)` is called.
2. If the outcome is `Skip`, the item is recorded as skipped and the loop continues to the next item.
3. If the outcome is `Proceed`, the copy is attempted. The `DestinationPath` may have been updated by `Resolve` if the strategy was `Rename`.

### Edge Case: Rename Counter Overflow

The `FindRenamedPath` loop has no upper bound, which is theoretically unbounded. In practice, this is not a concern for media backup scenarios. A defensive upper bound (e.g., `counter <= 9999`) could be added, throwing an `InvalidOperationException` if exceeded. This is an open question listed in [Section 21](#21-roadmap-and-open-questions).

---

## 17. MTP Sources — Threading Constraints

MTP (Media Transfer Protocol) support is one of the most technically constrained aspects of Core4. Understanding these constraints is essential for anyone working on MTP-related code.

### What is MTP?

MTP is a protocol used by smartphones, cameras, and other devices to expose their storage over a USB connection without appearing as a drive letter. Windows provides a built-in MTP stack, and the `MediaDevices` NuGet library wraps this stack with a managed API. The key characteristic of the Windows MTP stack is that it is built on COM.

### COM STA Requirements

The Windows COM infrastructure requires that certain objects be created and used on a specific thread — specifically, a thread initialized as a Single-Threaded Apartment (COM STA). The `MediaDevices` library's connection objects are COM STA objects. This means:

- The `MediaDevice` must be created on a COM STA thread.
- All method calls on `MediaDevice` must happen on the same COM STA thread.
- Calling `MediaDevice` methods from a different thread (including thread pool threads used by `Task.Run`) will produce COM exceptions (`COMException`, `InvalidCastException`, or simply incorrect behaviour).

This is not a limitation of the `MediaDevices` library itself — it is a fundamental constraint of the Windows MTP COM stack.

### Implication: MTP Always Requires SequentialBackupEngine

Because all MTP calls must happen on a single COM STA thread, MTP backups cannot be parallelised across threads. The `LimitedParallelBackupEngine` and `ParallelBackupEngine` would spread work across thread pool threads, which are not COM STA threads. Therefore, MTP sources must always use `SequentialBackupEngine`.

This was also the approach in Core2: the `ContentBufferingPipelineStage` was created with `parallelism: 1` even when other stages ran with higher parallelism, specifically to prevent MTP calls from happening on multiple threads.

### Dispatch Logic: Force Sequential for MTP

The `BackupEngine` facade will detect MTP sources and force sequential processing:

```csharp
// Design sketch — MTP detection and clamping in BackupEngine
bool isMtpSource = !Path.IsPathRooted(plan.Source);

if (isMtpSource && plan.Parallelism > 1)
{
	// Log a diagnostic: "MTP source detected; clamping Parallelism from N to 1"
}

int effectiveParallelism = isMtpSource ? 1 : plan.Parallelism;
```

The source type detection heuristic is: if `plan.Source` is not a rooted path (does not start with a drive letter or UNC prefix), it is treated as an MTP device name. This is a simplification; future versions may use a more explicit discriminated union type to distinguish source types.

### MtpScanner Must Run on the STA Thread

The `MtpScanner` itself must also run its scan on the COM STA thread. Because `ScanAsync` is called from the engine (which may be on any thread), the `MtpScanner` must marshal its work to the STA thread:

```csharp
// Design sketch — MtpScanner STA marshalling
internal sealed class MtpScanner : IBackupScanner
{
	public Task<IReadOnlyList<BackupItem>> ScanAsync(BackupPlan plan, CancellationToken cancellationToken)
	{
		TaskCompletionSource<IReadOnlyList<BackupItem>> tcs = new TaskCompletionSource<IReadOnlyList<BackupItem>>();

		Thread staThread = new Thread(() =>
		{
			try
			{
				IReadOnlyList<BackupItem> items = ScanOnStaThread(plan, cancellationToken);
				tcs.SetResult(items);
			}
			catch (Exception ex)
			{
				tcs.SetException(ex);
			}
		});

		staThread.SetApartmentState(ApartmentState.STA);
		staThread.Start();

		return tcs.Task;
	}

	private IReadOnlyList<BackupItem> ScanOnStaThread(BackupPlan plan, CancellationToken cancellationToken)
	{
		// MediaDevices calls go here — all on the STA thread
		throw new NotImplementedException();
	}
}
```

### MediaDevices.dll Local Reference

The `MediaDevices` library is currently referenced via a local `HintPath` pointing to a DLL in the `libs/` directory of the repository. This is a known limitation: the HintPath may not resolve on a fresh clone if the DLL is not present. A NuGet reference is the preferred long-term solution. Contributors working on MTP integration should verify the reference resolves before building.

### Contrast with MSC (Filesystem) Sources

Filesystem sources have none of these constraints. `File.Copy`, `Directory.EnumerateFiles`, and related BCL methods are thread-safe and can be called from any thread. This is why the parallel engines (`LimitedParallelBackupEngine`, `ParallelBackupEngine`) are restricted to filesystem sources.

---

## 18. DependencyInjection Registration

Core4 will follow the established .NET extension method pattern for service registration. The public entry point will be `AddBMTP3Core4(IServiceCollection services)`, defined in `DependencyInjection/ServiceCollectionExtensions.cs`.

### Planned ServiceCollectionExtensions

```csharp
// Design sketch — DI registration
namespace BMTP3.Core4.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddBMTP3Core4(this IServiceCollection services)
	{
		services.AddSingleton<IBackupScanner, FileSystemScanner>();
		services.AddSingleton<ISidecarWriter, JsonSidecarWriter>();
		services.AddSingleton<IBackupEngine, BackupEngine>();
		return services;
	}
}
```

### What Gets Registered

The default registration provides:

- `IBackupEngine` → `BackupEngine` (the public facade, which internally uses `SequentialBackupEngine`).
- `IBackupScanner` → `FileSystemScanner` (filesystem discovery, the default).
- `ISidecarWriter` → `JsonSidecarWriter`.

`SequentialBackupEngine`, `BackupSessionState`, and other internal types are not registered individually — they are constructed by `BackupEngine` as needed.

### MTP Scanner Registration by Caller

As in Core2, MTP scanner support is opt-in. A caller that wants MTP support registers `MtpScanner` before calling `AddBMTP3Core4`, or overrides the registration afterwards:

```csharp
// Design sketch — caller registering MTP scanner
services.AddBMTP3Core4();
// Override the default scanner with MTP scanner:
services.AddSingleton<IBackupScanner, MtpScanner>();
```

Or, using .NET 8 keyed services:

```csharp
// Design sketch — keyed scanner registration
services.AddKeyedSingleton<IBackupScanner>("filesystem", new FileSystemScanner());
services.AddKeyedSingleton<IBackupScanner>("mtp", new MtpScanner());
```

The engine would then resolve the appropriate scanner based on the source type. Keyed services were introduced in .NET 8 and are the idiomatic way to register multiple implementations of the same interface.

### Integration with BMTP3.Consoles

`BMTP3.Consoles` uses `Microsoft.Extensions.Hosting` and composes its services via `Startup/Configurations/*ServiceSetup` classes. Core4's `AddBMTP3Core4()` will be called from one of these setup classes, analogously to how Core2's `AddBMTP3Core2()` is currently called.

---

## 19. Lessons Learned from Core2 and Core3

Core4 is not designed in a vacuum. It is the direct product of lessons learned from Core2 and Core3. Understanding what worked, what did not, and why Core4 makes different choices is essential context for anyone maintaining this codebase over time.

### Core2: What Worked

**Channel-based pipeline correctness.** The Core2 pipeline (bounded `Channel<T>` with worker pools) is functionally correct. Files are processed, progress is tracked, and cancellation works. The architecture handled the MTP STA constraint correctly by pinning the content-buffering stage to a single worker.

**Interlocked counters in ProgressTracker.** Using `Interlocked.Increment` for all counter updates is the correct approach. It avoids locks, avoids `volatile` hairiness, and is correct under all threading models. Core4 adopts this pattern directly in `BackupSessionState`.

**NoopMediaDeviceScanner as default.** Registering a no-op scanner as the default, with real MTP support opt-in, was a good pattern. It meant the engine worked correctly (for filesystem) without any MTP dependencies, and the MTP code path was only activated when explicitly configured. Core4 adopts the same principle.

**Separating scan from process.** Core2's scanner (`IBackupScanner.ScanAsync`) returned items that were then fed into the pipeline. This separation is good: the scanner and the engine have clear, separate responsibilities.

### Core2: What Did Not Work

**Channel pipeline complexity.** The `AbstractPipelineStage<TContext>` abstraction, combined with `ChannelReader<T>` and `ChannelWriter<T>`, created a significant cognitive barrier for contributors. To add a new processing step, a contributor had to understand channels, bounded vs unbounded, back-pressure, worker pools, and the stage lifecycle. This was too much complexity for a system that, at its core, copies files from one place to another.

**Debugging pipeline failures.** When something went wrong inside a pipeline stage, the exception propagation path was non-obvious. Exceptions inside a worker task were captured by the channel infrastructure, but tracking them back to the original item and the original exception was difficult without extensive logging.

**Premature parallelism.** Core2 was designed for parallelism from day one, even though the most common source (MTP) required sequential processing anyway. The sequential case was effectively forced on MTP users by clamping parallelism to 1, making the pipeline complexity largely invisible overhead for them.

### Core3: What Was Planned vs What Shipped

Core3 was intended to simplify Core2. Based on the `CORE3_PLAN.md` document, the ambitions were sound, but the implementation did not reach a shippable state. The key lesson from Core3's trajectory is:

**Over-engineering before concrete implementation is costly.** Core3 designed abstractions before having a concrete implementation to inform them. The result was architecture that looked right on paper but was hard to validate without running code. Core4 inverts this: start with the simplest concrete implementation (`SequentialBackupEngine`) and extract abstractions only when the concrete implementation reveals the right boundaries.

**Too many abstractions too early confuses contributors.** If every concept has an interface and every interface has a factory, contributors spend their time navigating indirection rather than writing business logic. Core4's rule: introduce an interface only when there are (or will imminently be) two different implementations, or when testability requires it.

### Core4's Response to These Lessons

**Start sequential, add complexity incrementally.** The first concrete implementation is `SequentialBackupEngine`. It does not use channels, worker pools, or semaphores. It is a simple loop. Once it is correct and fully tested, `LimitedParallelBackupEngine` will be added alongside it — not replacing it.

**Public API is stable; internal engines evolve.** The `IBackupEngine` interface does not change between engine implementations. Callers are insulated from internal complexity.

**Separate validation from execution.** `BackupPlanValidator.Validate` runs before any IO. This is a Core2 lesson: validation exceptions are programming errors, not runtime errors, and should surface immediately rather than mid-run.

**Sidecar written per-file.** Core2 did not have a per-file sidecar mechanism. Core4 treats sidecar writing as part of the per-item lifecycle, written immediately after each successful copy. This is a correctness improvement over any approach that batches sidecar writing.

**Keep scanner injected and testable.** Core2's scanner was injectable; Core4 maintains this. Test suites can provide mock scanners that return fixed item lists, enabling fast, deterministic unit tests.

---

## 20. Testing Strategy

Core4 is designed to be fully testable at every layer. The test project (`BMTP3.Core4.Tests`) will mirror the Core4 project structure. Tests use **xUnit v3**, consistent with the rest of the solution.

### Unit Tests

Unit tests do not touch the filesystem or any device. They use mock or stub implementations of interfaces. They run fast (milliseconds per test).

#### BackupPlanValidator — Every Invalid Input

```csharp
// Design sketch — BackupPlanValidator tests
public sealed class BackupPlanValidatorTests
{
	[Fact]
	public void Validate_NullPlan_ThrowsArgumentNullException()
	{
		Action act = () => BackupPlanValidator.Validate(null!);
		Assert.Throws<ArgumentNullException>(act);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Validate_NullOrWhitespaceSource_ThrowsArgumentException(string? source)
	{
		BackupPlan plan = new BackupPlan
		{
			Source = source!,
			Destination = @"C:\Backup"
		};

		Action act = () => BackupPlanValidator.Validate(plan);
		Assert.Throws<ArgumentException>(act);
	}

	[Fact]
	public void Validate_ParallelismZero_ThrowsArgumentOutOfRangeException()
	{
		BackupPlan plan = new BackupPlan
		{
			Source = @"D:\DCIM",
			Destination = @"C:\Backup",
			Parallelism = 0
		};

		Action act = () => BackupPlanValidator.Validate(plan);
		Assert.Throws<ArgumentOutOfRangeException>(act);
	}

	[Fact]
	public void Validate_ValidPlan_DoesNotThrow()
	{
		BackupPlan plan = new BackupPlan
		{
			Source = @"D:\DCIM",
			Destination = @"C:\Backup"
		};

		// Should not throw
		BackupPlanValidator.Validate(plan);
	}
}
```

#### Guard — Null and Whitespace

```csharp
// Design sketch — Guard tests
public sealed class GuardTests
{
	[Fact]
	public void NotNull_NullValue_ThrowsArgumentNullException()
	{
		object? value = null;
		Action act = () => Guard.NotNull(value);
		Assert.Throws<ArgumentNullException>(act);
	}

	[Fact]
	public void NotNull_NonNullValue_DoesNotThrow()
	{
		object value = new object();
		Guard.NotNull(value); // Should not throw
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("  ")]
	public void NotNullOrWhiteSpace_InvalidInput_ThrowsArgumentException(string? value)
	{
		Action act = () => Guard.NotNullOrWhiteSpace(value);
		Assert.Throws<ArgumentException>(act);
	}
}
```

#### BackupSessionState — Thread Safety of Counters

```csharp
// Design sketch — BackupSessionState concurrency test
public sealed class BackupSessionStateTests
{
	[Fact]
	public void RecordCopied_CalledConcurrently_CountIsExact()
	{
		BackupSessionState state = new BackupSessionState();
		int iterations = 1000;

		Parallel.For(0, iterations, _ => state.RecordCopied());

		Assert.Equal(iterations, state.Copied);
	}

	[Fact]
	public void RecordFailed_CalledConcurrently_CountIsExact()
	{
		BackupSessionState state = new BackupSessionState();
		int iterations = 500;

		Parallel.For(0, iterations, _ => state.RecordFailed());

		Assert.Equal(iterations, state.Failed);
	}
}
```

#### CollisionHandler — All Three Strategies

```csharp
// Design sketch — CollisionHandler tests (filesystem-touching, using temp dir)
public sealed class CollisionHandlerTests : IDisposable
{
	private readonly string _tempDir;

	public CollisionHandlerTests()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(_tempDir);
	}

	[Fact]
	public void Resolve_Skip_WhenDestinationExists_ReturnsSkip()
	{
		string destPath = Path.Combine(_tempDir, "file.jpg");
		File.WriteAllText(destPath, "existing");

		BackupItem item = new BackupItem
		{
			SourcePath = "source.jpg",
			DestinationPath = destPath,
			SizeBytes = 100
		};

		CollisionOutcome outcome = CollisionHandler.Resolve(item, CollisionStrategy.Skip);

		Assert.Equal(CollisionOutcome.Skip, outcome);
		Assert.Equal(destPath, item.DestinationPath); // unchanged
	}

	[Fact]
	public void Resolve_Rename_WhenDestinationExists_UpdatesDestinationPath()
	{
		string destPath = Path.Combine(_tempDir, "file.jpg");
		File.WriteAllText(destPath, "existing");

		BackupItem item = new BackupItem
		{
			SourcePath = "source.jpg",
			DestinationPath = destPath,
			SizeBytes = 100
		};

		CollisionOutcome outcome = CollisionHandler.Resolve(item, CollisionStrategy.Rename);

		Assert.Equal(CollisionOutcome.Proceed, outcome);
		Assert.EndsWith("file (1).jpg", item.DestinationPath);
	}

	public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
```

### Integration Tests

Integration tests exercise the full engine stack with real filesystem operations. They are tagged with `[Trait("Category","Integration")]` and can be run selectively:

```
dotnet test .\BMTP3.Core4.Tests\BMTP3.Core4.Tests.csproj --filter "Category=Integration"
```

#### SequentialBackupEngine — End-to-End Copy

```csharp
// Design sketch — integration test
[Trait("Category", "Integration")]
public sealed class SequentialBackupEngineIntegrationTests : IDisposable
{
	private readonly string _sourceDir;
	private readonly string _destDir;

	public SequentialBackupEngineIntegrationTests()
	{
		_sourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		_destDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(_sourceDir);
		Directory.CreateDirectory(_destDir);
	}

	[Fact]
	public async Task RunAsync_FilesInSource_CopiesAllFilesToDestination()
	{
		File.WriteAllText(Path.Combine(_sourceDir, "photo1.jpg"), "fake jpeg 1");
		File.WriteAllText(Path.Combine(_sourceDir, "photo2.jpg"), "fake jpeg 2");

		BackupPlan plan = new BackupPlan
		{
			Source = _sourceDir,
			Destination = _destDir,
			WriteSidecar = false
		};

		IBackupScanner scanner = new FileSystemScanner();
		SequentialBackupEngine engine = new SequentialBackupEngine(scanner);
		List<IBackupProgress> progressReports = [];

		BackupResult result = await engine.RunAsync(plan, new Progress<IBackupProgress>(p => progressReports.Add(p)));

		Assert.Equal(2, result.TotalItems);
		Assert.Equal(2, result.CopiedItems);
		Assert.Equal(0, result.FailedItems);
		Assert.Equal(0, result.SkippedItems);
		Assert.True(File.Exists(Path.Combine(_destDir, "photo1.jpg")));
		Assert.True(File.Exists(Path.Combine(_destDir, "photo2.jpg")));
	}

	[Fact]
	public async Task RunAsync_CancellationRequested_StopsProcessing()
	{
		for (int i = 0; i < 100; i++)
		{
			File.WriteAllText(Path.Combine(_sourceDir, $"photo{i}.jpg"), $"content {i}");
		}

		BackupPlan plan = new BackupPlan
		{
			Source = _sourceDir,
			Destination = _destDir,
			WriteSidecar = false
		};

		CancellationTokenSource cts = new CancellationTokenSource();
		IBackupScanner scanner = new FileSystemScanner();
		SequentialBackupEngine engine = new SequentialBackupEngine(scanner);

		// Cancel after a brief delay
		cts.CancelAfter(TimeSpan.FromMilliseconds(1));

		await Assert.ThrowsAsync<OperationCanceledException>(
			() => engine.RunAsync(plan, new Progress<IBackupProgress>(_ => { }), cts.Token)
		);
	}

	public void Dispose()
	{
		Directory.Delete(_sourceDir, recursive: true);
		Directory.Delete(_destDir, recursive: true);
	}
}
```

### Test Naming Convention

All tests follow the `MethodName_Scenario_ExpectedResult` convention:

- `Validate_NullPlan_ThrowsArgumentNullException`
- `RunAsync_FilesInSource_CopiesAllFilesToDestination`
- `Resolve_Rename_WhenDestinationExists_UpdatesDestinationPath`
- `RecordCopied_CalledConcurrently_CountIsExact`

This convention makes test names self-documenting. Reading the test name tells you exactly what is being tested, under what conditions, and what the expected outcome is.

### Test Organization

Tests will be organized to mirror the production code structure:

```
BMTP3.Core4.Tests/
├── Engine/
│   ├── Validation/
│   │   └── BackupPlanValidatorTests.cs
│   ├── State/
│   │   └── BackupSessionStateTests.cs
│   └── Sequential/
│       ├── SequentialBackupEngineTests.cs       ← unit tests
│       └── SequentialBackupEngineIntegrationTests.cs
├── Scanner/
│   └── FileSystemScannerTests.cs
├── Helpers/
│   └── GuardTests.cs
└── Collision/
    └── CollisionHandlerTests.cs
```

---

## 21. Roadmap and Open Questions

This section tracks the phased implementation plan and documents questions that require a decision before the corresponding code can be written.

### Phase 1: Complete SequentialBackupEngine (Immediate Priority)

Phase 1 turns the current skeleton into a fully functional sequential backup engine. All components below must be implemented and unit/integration tested before Phase 2 begins.

**CollisionHandler** (`Engine/Sequential/CollisionHandler.cs`):
- Implement `Resolve(BackupItem item, CollisionStrategy strategy)`.
- Implement `FindRenamedPath(string destinationPath)` with counter suffix logic.
- Write unit tests for all three strategies and edge cases.

**FileSystemScanner** (`Scanner/FileSystemScanner.cs`):
- Implement `ScanAsync` using `Directory.EnumerateFiles`.
- Compute `DestinationPath` from `BackupPlan.Destination` + relative path.
- Handle `Recursive` flag via `SearchOption`.
- Write unit and integration tests.

**ISidecarWriter + JsonSidecarWriter** (`Sidecar/`):
- Define `ISidecarWriter` interface.
- Implement `JsonSidecarWriter` using `System.Text.Json`.
- Write unit tests (mock filesystem or use temp directory).

**ProgressTracker + BackupProgressSnapshot** (`Progress/`):
- Implement `ProgressTracker` with `Interlocked` counters.
- Implement `BackupProgressSnapshot` as immutable `IBackupProgress`.
- Write unit tests for counter accuracy.

**Complete SequentialBackupEngine.RunAsync** (`Engine/Sequential/SequentialBackupEngine.cs`):
- Replace `throw new NotImplementedException()` with full implementation (as described in [Section 9](#9-sequentialbackupengine--implementation-plan)).
- Wire up `CollisionHandler`, `ISidecarWriter`, `ProgressTracker`.
- Write integration tests (end-to-end copy, cancellation, sidecar verification).

**AddBMTP3Core4 DI registration** (`DependencyInjection/ServiceCollectionExtensions.cs`):
- Implement `AddBMTP3Core4(IServiceCollection)`.
- Register `IBackupEngine`, `IBackupScanner`, `ISidecarWriter`.

**Wire BackupEngine dispatch** (`Engine/BackupEngine.cs`):
- Replace `throw new NotImplementedException()` with dispatch logic.
- Call `BackupPlanValidator.Validate`.
- Detect source type, select engine, forward call.

### Phase 2: LimitedParallelBackupEngine (Filesystem Sources)

Phase 2 adds bounded concurrency for filesystem sources. This phase begins only after Phase 1 is fully complete and all tests pass.

**LimitedParallelBackupEngine** (`Engine/LimitedParallel/LimitedParallelBackupEngine.cs`):
- Use `SemaphoreSlim(plan.Parallelism)` to gate concurrent copies.
- Each item is processed via `Task.Run` on the thread pool.
- The `Items` list is shared read-only; per-item state writes (Status, ErrorMessage) are confined to one task per item (no contention).
- Progress reporting must be thread-safe (use `ProgressTracker` with `Interlocked`).
- Integration test: verify that items are processed concurrently (measure elapsed time vs sequential).

### Phase 3: MTP Integration

Phase 3 adds real MTP device support. This phase requires a Windows machine with a physical MTP device or a reliable MTP emulator.

**MtpScanner** (`Scanner/MtpScanner.cs`):
- Implement `ScanAsync` on a COM STA thread (see design sketch in [Section 17](#17-mtp-sources--threading-constraints)).
- Use `MediaDevices` library to enumerate files.
- Compute `DestinationPath` from device virtual path.

**STA Thread Management**:
- Ensure all `MediaDevice` calls happen on the same STA thread.
- Consider a dedicated `StaThreadPool` (a thread with `ApartmentState.STA` that processes a queue of delegates).

**NuGet Reference for MediaDevices**:
- Replace the local `HintPath` DLL reference with a proper NuGet reference.
- Update `BMTP3.Core4.csproj` accordingly.

### Phase 4: ParallelBackupEngine (Future)

Phase 4 explores unrestricted or channel-based concurrency for filesystem sources. The exact approach (Task.WhenAll, Parallel.ForEachAsync, or channels) will be determined based on lessons learned from Phase 2.

### Open Questions

The following questions require explicit decisions before the corresponding code can be written. They are documented here so they do not get lost.

**Q1: Should IBackupScanner become public?**
Currently `internal`. If callers want to provide custom scanners (e.g., for a proprietary device protocol), they need access to the interface. One option: expose it publicly. Another: require callers to use the DI container and register their scanner via `InternalsVisibleTo`. Decision pending.

**Q2: Should BackupItem become a record?**
`BackupItem` is a class because `Status` and `ErrorMessage` are mutable. If these fields were changed to a separate "result" object (computed at end of processing rather than mutated in place), `BackupItem` could become a `record`. This would simplify equality comparisons in tests. Requires redesign of the per-item status tracking.

**Q3: Should the filename CollisionStreategy.cs be corrected?**
The file `BMTP3.Core4/Models/Enums/CollisionStreategy.cs` has a typo in the filename (should be `CollisionStrategy.cs`). The enum name `CollisionStrategy` is correct. Renaming the file is a trivial change but requires a git rename to avoid history confusion. Decision: yes, fix the filename, but do it in a dedicated commit with a clear message.

**Q4: Hash Algorithm — MD5 vs SHA-256?**
MD5 is fast (suitable for "quick sanity check" verification) but not collision-resistant. SHA-256 is slower but cryptographically strong. For backup verification, collision resistance is not strictly needed — the goal is detecting corruption or copy errors, not adversarial hash collisions. MD5 or even CRC32 would be adequate. However, SHA-256 aligns with modern security expectations. Recommendation: make the hash algorithm configurable in `BackupPlan`, defaulting to SHA-256.

**Q5: Should OverallPercent be byte-weighted rather than item-count-weighted?**
Currently, `OverallPercent = CompletedItems / TotalItems * 100.0`. This means a run with one 4 GB file and 999 tiny files shows 99.9% complete after the 999 tiny files, even though most of the data is in the last file. Byte-weighted progress (`BytesTransferred / TotalBytes * 100.0`) would be more accurate for time estimates. Requires tracking `BytesTransferred` and `TotalBytes` in `BackupSessionState` and `ProgressTracker`. Added complexity but better UX.

**Q6: Should BackupPlan support multiple sources?**
The current design is one source per `BackupPlan`. Support for multiple sources would require `Source` to become `IReadOnlyList<string>`, and the scanner would need to handle enumeration across multiple sources. This is a significant API change. Recommendation: keep single-source for now; callers can call `RunAsync` multiple times for multiple sources.

**Q7: Should there be a dry-run mode?**
A dry-run mode would scan and report what would be copied (including collision outcomes) without writing any files. This is useful for preview/audit. Implementation: add `bool DryRun { get; init; } = false;` to `BackupPlan`. When true, the engine skips `File.Copy` and sidecar writing but still reports progress and returns a `BackupResult`. Would require careful handling so that `CopiedItems` in a dry-run result is clearly labelled as "would have been copied".

**Q8: Rename counter upper bound?**
The `FindRenamedPath` loop in `CollisionHandler` has no upper bound. In theory, if the destination directory contains `file.jpg`, `file (1).jpg`, ..., `file (N).jpg` for every N, the loop would run forever. A practical upper bound of 9999 with an `InvalidOperationException` on overflow would be safe. This is almost certainly unnecessary in practice but would make the code defensively correct.

**Q9: BackupEngine dispatch — path-based or explicit SourceType?**
The current design detects MTP sources by checking `!Path.IsPathRooted(plan.Source)`. This is fragile: a device named `C:\MyDevice` (contrived but possible) would be misdetected as filesystem. A more robust approach: add a `SourceType` property to `BackupPlan` with an enum `{ FileSystem, Mtp }`. This is a breaking change to the record but would eliminate the heuristic. Decision pending.

**Q10: Should BackupResult include a list of SkippedItems (paths)?**
`BackupResult.Errors` lists failed items with their paths. Should there be a corresponding `SkippedItems` list of paths? This would allow callers to display what was skipped (useful for audit logs). Trade-off: larger result object, higher memory usage for runs with many skipped files. Recommendation: add `IReadOnlyList<string> SkippedPaths { get; init; } = [];` as an opt-in (populated only if `BackupPlan.RecordSkippedPaths = true`).

---

## Appendix A: Verified Source Files Index

The following source files were verified to exist in the repository at the time this document was written. All code blocks in this document that are marked as actual code (not design sketches) are derived from these files.

| File | Status | Public |
|---|---|---|
| `BMTP3.Core4/Api/IBackupEngine.cs` | Exists | Yes |
| `BMTP3.Core4/Api/IBackupProgress.cs` | Exists | Yes |
| `BMTP3.Core4/Api/IFileProgress.cs` | Exists | Yes |
| `BMTP3.Core4/Models/BackupPlan.cs` | Exists | Yes |
| `BMTP3.Core4/Models/BackupResult.cs` | Exists | Yes |
| `BMTP3.Core4/Models/BackupItem.cs` | Exists | No (internal) |
| `BMTP3.Core4/Models/Enums/BackupPhase.cs` | Exists | Yes |
| `BMTP3.Core4/Models/Enums/BackupItemStatus.cs` | Exists | No (internal) |
| `BMTP3.Core4/Models/Enums/CollisionStreategy.cs` | Exists (filename typo) | Yes |
| `BMTP3.Core4/Engine/BackupEngine.cs` | Exists | Yes (class) |
| `BMTP3.Core4/Engine/Sequential/SequentialBackupEngine.cs` | Exists | No (internal) |
| `BMTP3.Core4/Engine/State/BackupSessionState.cs` | Exists | No (internal) |
| `BMTP3.Core4/Engine/Validation/BackupPlanValidator.cs` | Exists | No (internal) |
| `BMTP3.Core4/Scanner/IBackupScanner.cs` | Exists | No (internal) |
| `BMTP3.Core4/Helpers/Guard.cs` | Exists | No (internal) |

All code blocks labelled as "Design sketch" or "Planned" do not correspond to existing files. They represent the intended implementation and may differ from the final code.

---

## Appendix B: Glossary

| Term | Definition |
|---|---|
| **MSC** | Mass Storage Class — a USB protocol where the device appears as a standard drive letter |
| **MTP** | Media Transfer Protocol — a USB protocol for device-hosted virtual filesystems |
| **STA** | Single-Threaded Apartment — a COM threading model where all calls to an object occur on one thread |
| **Sidecar** | A small JSON file written alongside each copied media file to record provenance |
| **BackupPlan** | The immutable input configuration for a backup run |
| **BackupResult** | The immutable output summary of a completed backup run |
| **BackupItem** | Internal mutable tracker for a single file's lifecycle during a run |
| **BackupSessionState** | Internal accumulator for counters and item list during a run |
| **CollisionStrategy** | How the engine handles a destination file that already exists |
| **SequentialBackupEngine** | The first concrete engine implementation; processes items one at a time |
| **IBackupScanner** | Internal interface for discovering files to back up |
| **ISidecarWriter** | Internal interface (planned) for writing sidecar files |
| **ProgressTracker** | Internal class (planned) for maintaining thread-safe progress counters |
| **BackupProgressSnapshot** | Immutable snapshot of progress state (planned implementation of IBackupProgress) |

---

*Document generated as part of the BMTP3.Core4 architecture initiative. For questions or corrections, open an issue or pull request against the `BMTP3_CS` repository.*
