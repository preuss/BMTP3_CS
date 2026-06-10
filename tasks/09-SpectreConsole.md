# Task: Spectre.Console proper usage — progress, logging, output

> Architecture & design investigation. Replace manual `WriteLine`/`\r` progress with Spectre's built-in `Progress` widget.
> Updated 10 Jun 2026: NO CORE/CORE2/CORE3 CHANGES constraint added; porting decision → Option C (built-in columns only); Finding 2 updated; Acceptance Criteria fixed.
> See also: `06-Console-UI-Progress.md` (superseded by this document).

---

## Goal

Make all console output in `BMTP3.Consoles` use Spectre.Console correctly:
- `AnsiConsole.Progress()` for progress during backup (not `WriteLine` or `\r`)
- `IAnsiConsole` injection everywhere (already done)
- No `Console.WriteLine`, `Console.Write`, or manual cursor manipulation
- Only Consoles files may be modified. Core/Core2/Core3 are reference-only.
- Same for Core/Core2/Core3 console commands: `BackupConsoleCommand.cs`, `BackupConsoleCommand2.cs`, `BackupConsoleCommand3.cs` — not updated.

> ⚠️ **NO CORE/CORE2/CORE3 CHANGES:** Only Consoles files may be modified. Core/Core2/Core3 are reference-only. This includes their console commands: `BackupConsoleCommand.cs` (Core2), `BackupConsoleCommand2.cs`, `BackupConsoleCommand3.cs`.

---

## Spectre Package Matrix

| Project | Spectre.Console | Spectre.Console.Cli | Notes |
|---------|----------------|---------------------|-------|
| `BMTP3.Consoles` | 0.55.2 | 0.55.0 | Target for all new code |
| `BMTP3.Core` | 0.54.0 | 0.53.1 | Legacy — not changing |

**Decision:** Keep both versions. Core is legacy and won't be updated. Consoles uses 0.55.2.

---

## Existing Spectre Usage — Complete Map

### 1. Consoles — `IAnsiConsole` injection via DI

- `LoggingServiceSetup.cs:14` — `services.AddSingleton<IAnsiConsole>(_ => AnsiConsole.Console)`
- `LoggingServiceSetup.cs:21` — `SpectreAnsiConsoleLoggerProvider` registered as logging provider

### 2. Consoles — `SpectreAnsiConsoleLogger`

- `Logging/SpectreAnsiConsoleLogger.cs` — Custom `ILogger` that writes through `_console.MarkupLineInterpolated`.
  Uses `MarkupLineInterpolated` (not `EscapeMarkup`!) for log levels (Critical=bold red, Error=red, Warning=yellow, Info=default, Debug=blue).
  **Problem:** `MarkupLineInterpolated` parses markup in interpolated strings — if log messages contain `[`, `]` they will crash.
- `Logging/SpectreAnsiConsoleLoggerProvider.cs` — Simple `ILoggerProvider` factory.

### 3. Consoles — `SpectreConsolePrompter`

- `UI/SpectreConsolePrompter.cs` — Wraps `_console.Confirm(message)` for `IUserPrompter`. Simple, correct.

### 4. Consoles — `SpectreConsoleNotifier`

- `UI/SpectreConsoleNotifier.cs` — Wraps `_console.MarkupLine` for `IUserNotifier`. Simple, correct.

### 5. Consoles — `AnsiConsoleWriter`

- `Services/AnsiConsoleWriter.cs` — `TextWriter` subclass writing to `AnsiConsole`. Used by `StreamingBackupExample`.

### 6. Consoles — `ConsolesPrinter`

- `Services/ConsolesPrinter.cs` — The main output class. **Target for refactoring.**
  Current state:
  - All three `PrintProgress` overloads use `_console.WriteLine()` — floods terminal
  - `PrintResult` uses `_console.MarkupLine` with proper `.EscapeMarkup()` — correct
  - `PrintOptionsModel` uses `_console.MarkupLine` with `.EscapeMarkup()` — correct
  - `PrintError` uses `_console.MarkupLine` with `.EscapeMarkup()` — correct

### 7. Consoles — `ProgramSpectreExample`

- `Examples/StreamingBackupExample/ProgramSpectreExample.cs` — Uses `AnsiConsole.Clear()` + `AnsiConsole.Write(table)` pattern.
  Renders progress as a table with rows for each active file. **Not** using `AnsiConsole.Progress()` widget.
  Uses `IAsyncEnumerable<BackupProgress>` + `ChannelBackupEngine` — Core2 pattern, not Core4.

### 8. Core (legacy) — `AnsiConsole.Progress()` × 4 occurrences

- **`Handlers/BackupHandler.cs:135`** — Full Spectre Progress for device backup:
  ```csharp
  AnsiConsole.Progress()
      .AutoClear(true).AutoRefresh(true).HideCompleted(true)
      .Columns(new ProgressColumn[] {
          new SpinnerColumn(new SequenceSpinner(SequenceSpinner.Sequence7)),
          new TaskDescriptionColumn(),
          new ProgressBarColumn() {Width=10},
          new PercentageColumn(),
          new RemainingTimeColumn(),
          new ValueOfMaxColumn(),
      })
      .Start(ctx => { var task = ctx.AddTask("...",
          new ProgressTaskSettings { AutoStart = true, MaxValue = ... }); ... });
  ```
  This pattern repeats at lines 325-382 (drive backup) and 1142-1172 (`GetAllMediaFiles`).

- **`Handlers/BackupHandler.cs:255`** — File/dir counting:
  ```csharp
  AnsiConsole.Progress()
      .AutoRefresh(true)
      .Columns(new ProgressColumn[] {
          new SpinnerColumn(new SequenceSpinner(SequenceSpinner.Sequence6)),
          new TaskDescriptionColumn(),
          new CounterColumn<int>("Count") { CounterStyle = new Style(decoration: Decoration.Bold) },
          new ElapsedTimeAdvancedColumn(),
      })
  ```

- **`Handlers/RefactorNewBackup/BackupHandlerForDevice.cs:83`** — Same counting pattern as above.
- **`Handlers/RefactorNewBackup/BackupHandlerForDevice.cs:163`** — Same full progress pattern.

This is the **reference pattern** for Spectre progress in the codebase.

### 9. Core (legacy) — `ProgressStatus` abstraction layer

- `IO/Consoles/ProgressStatus/ProgressStatus.cs` — Wraps `AnsiConsole.Live(new Panel("...")).StartAsync(...)` with a `Table` inside for multiple task rows.
  Manual update loop: `ctx.UpdateTarget(table)`.
  **Not** recommended for progress — use `Progress()` instead of `Live()`.

- `IO/Consoles/ProgressStatus/ProgressStatusContext.cs` — Wraps `ProgressContext` with `AddTask`, `AddTaskBefore`, `AddTaskAfter`, `Refresh`.

- `IO/Consoles/ProgressStatus/ProgressStatusTask.cs` — Wraps `ProgressTask` with `Increment`, `Value`, `MaxValue`, `Percentage`, `Speed`, `RemainingTime`, etc.

- `IO/Consoles/ProgressStatus/AnsiConsoleExtensions.cs` — `console.ProgressStatus()` extension method.

- `IO/Consoles/ProgressStatus/MyAnsiConsole.cs` — Static helper `MyAnsiConsole.ProgressStatus()`.

### 10. Core (legacy) — `AnsiConsoleWrapper`

- `IO/Consoles/AnsiConsoleWrapper.cs:7` — Implements `Spectre.Console.IConsole` (not `IAnsiConsole`).
  Wraps `System.Console` members through `AnsiConsole` for `Clear()` and `WriteLine()` overloads.
  Adapter between app's `IConsole` interface and Spectre.

### 11. Core (legacy) — `FileAndDirectoryCounter` bridge

- `IO/Consoles/Progress/FileAndDirectoryCounter.cs` — Accepts either `ProgressTask` or `StatusContext` in constructor.
  Updates task description via delegate: `task.Description = update`.
  Bridges Spectre Progress and Status contexts with a unified counter.

### 12. Core (legacy) — Custom `ProgressColumn` implementations (6 classes)

All extend `Spectre.Console.ProgressColumn`, override `Render(RenderOptions, ProgressTask, TimeSpan)`:

| File | Class | Renders |
|------|-------|---------|
| `ProgressStatus/ValueOfMaxColumn.cs` | `ValueOfMaxColumn` | `[blue]{Value}/{MaxValue}[/]` via `Markup` |
| `ProgressStatus/CounterColumn.cs` | `CounterColumn<T>` | Generic counter with `INumber<T>` via `Paragraph` |
| `ProgressStatus/CustomColumn.cs` | `CustomColumn` | Custom text via `Func<string>` + `Markup` |
| `ProgressStatus/CustomTextColumn.cs` | `CustomTextColumn` | Custom text via `Func<ProgressTaskState, string>` + `Markup` |
| `Progress/Columns/ElapsedTimeAdvancedColumn.cs` | `ElapsedTimeAdvancedColumn` | Elapsed time with/without ms via `Text` |
| `Progress/Columns/CounterColumn.cs` | `CounterColumn` | Task description via `Markup` |

**Decision:** These are in Core (legacy). Should we **copy** relevant ones to Consoles, or **move** them to a shared project? `ValueOfMaxColumn` and `ElapsedTimeAdvancedColumn` are the most useful.

### 13. Core (legacy) — Custom `Spinner` implementations (2 classes)

Both extend `Spectre.Console.Spinner`:

| File | Class | Frames | Interval |
|------|-------|--------|----------|
| `Spinner/SimpleSpinner.cs` | `SimpleSpinner` | `["A", "B", "C"]` | 100ms |
| `Spinner/SequenceSpinner.cs` | `SequenceSpinner` | 7 presets: `"/-\|"`, `".o0o"`, `"<^>v"`, `"#■."`, `"▄▀"`, `"└┘┐┌"`, `".-|+oOØ"` | 200ms |

`SequenceSpinner.Sequence7` is used in `BackupHandler.cs:135` as the reference pattern.

### 14. Consoles — `ConsolesServiceSetup` NOT wired in production

- `Startup/Configurations/ConsolesServiceSetup.cs` registers `ConsolesPrinter`, `SpectreConsoleNotifier`, `SpectreConsolePrompter`.
- BUT `ApplicationStartup.cs:32-39` only calls `LoggingServiceSetup` and `ApplicationServiceSetup`:
  ```csharp
  List<IServiceSetup> serviceSetupList = [
      new LoggingServiceSetup(),       // IAnsiConsole + LoggerProvider
      new ApplicationServiceSetup()    // Engines, SystemConsoleWriter
      // ⚠️ ConsolesServiceSetup is NOT included
  ];
  ```
- **Consequence:** `ServiceProvider.GetService<ConsolesPrinter>()` returns `null` in production.
  All `consolePrinter?.PrintXxx()` calls are silently no-ops.
  This blocks ANY progress refactoring from working in production.

### 15. Consoles — Spectre test configuration pattern

`BackupConsoleIntegrationTests.cs:38-44`:
```csharp
services.AddSingleton<IAnsiConsole>(_ => AnsiConsole.Create(new AnsiConsoleSettings
{
    Ansi = AnsiSupport.No,
    ColorSystem = ColorSystemSupport.NoColors,
    Out = new AnsiConsoleOutput(new StringWriter())
}));
```
This creates a testable no-ANSI console writing to a `StringWriter` — essential for CI/testing.

---

## Key Findings

### Finding 1: ConsolesPrinter.PrintProgress floods terminal with WriteLine

All three overloads emit one line per progress report. For a backup with many files, this produces hundreds of lines.
**Fix:** Use `AnsiConsole.Progress()` with `ProgressTask` instead.

### Finding 2: `AnsiConsole.Progress()` fluent API is the canonical pattern

Spectre.Console's `Progress()` fluent API with `SpinnerColumn`, `ProgressBarColumn`, `PercentageColumn`, `RemainingTimeColumn` is the canonical way. Core's `BackupHandler` is reference-only — no changes allowed.

### Finding 3: Progress must work across 3 engine types

| Engine | Progress type | Current printer method | Refactor target |
|--------|--------------|----------------------|-----------------|
| Core2 | `IBackupProgress` (channel-based, `IAsyncEnumerable`) | `PrintProgress(IBackupProgress)` | `AnsiConsole.Progress()` with ProgressTask per phase |
| Core3 | `IBackupProgress` (simple callback) | `PrintProgress(Core3.IBackupProgress)` | Same widget, different data binding |
| Core4 | `BackupProgress` (`IProgress<T>` callback) | `PrintProgress(Core4BackupProgress)` | Same widget, different data binding |

### Finding 4: `SpectreAnsiConsoleLogger` uses `MarkupLineInterpolated` unsafely

If any log message contains `[` or `]` characters, it will crash with an unhandled markup parse exception.
**Fix:** Use `MarkupLine($"{text.EscapeMarkup()}")` instead of `MarkupLineInterpolated($"{text}")`.

### Finding 5: Version mismatch (0.55.2 vs 0.54.0)

Minor — APIs are compatible. Consoles should standardize on 0.55.2.

### Finding 6 (CRITICAL BLOCKER): ConsolesServiceSetup not wired in production

`ApplicationStartup.cs` calls only `LoggingServiceSetup` + `ApplicationServiceSetup`. `ConsolesServiceSetup` is **never called**, which means:
- `ConsolesPrinter` is not registered → `GetService<ConsolesPrinter>()` returns `null`
- All `consolePrinter?.PrintXxx()` calls are no-ops
- **Any progress refactoring will NOT work in production** until this is fixed

**Fix:** Add `new ConsolesServiceSetup()` to `ApplicationStartup.cs` service list.

### Finding 7: Core has 6 custom `ProgressColumn` classes we can reuse

Core's custom columns (`ValueOfMaxColumn`, `CounterColumn<T>`, `ElapsedTimeAdvancedColumn`, etc.) provide richer progress display. Decision needed:
- **Option A:** Copy relevant columns (ValueOfMaxColumn, ElapsedTimeAdvancedColumn) into Consoles project
- **Option B:** Move to a shared project (BMTP3.Common)
- **Option C:** Ignore and use only built-in Spectre columns (simpler, less rich)

### Finding 8: Core has 2 custom `Spinner` classes we can reuse

`SequenceSpinner` with 7 presets is used in Core's BackupHandler. Consoles could use these for variety.
Same decision options as Finding 7.

### Finding 9: Core's `FileAndDirectoryCounter` pattern

Bridges `ProgressTask.Description` updates with file counting. Useful pattern for showing "Processing file X of Y" during backup scan vs backup copy phases.

### Finding 10: Test configuration pattern exists in BackupConsoleIntegrationTests

The test project already configures a no-ANSI `IAnsiConsole` with `StringWriter` for testable output.
This pattern should be documented and reused when writing tests for the new progress widget.

---

## Design: ConsolesPrinter Progress Refactoring

### Option A: `AnsiConsole.Progress()` in ConsolesPrinter

```csharp
// ConsolesPrinter owns the Progress context lifecycle.
// Caller (BackupConsoleCommand4) calls Start/Stop.

private ProgressTask? _currentTask;
private Progress? _currentProgress;

public IDisposable StartProgress(string description, int maxValue)
{
    _currentProgress = AnsiConsole.Progress()
        .AutoClear(true)
        .AutoRefresh(true)
        .HideCompleted(true)
        .Columns(new ProgressColumn[]
        {
            new SpinnerColumn(),
            new TaskDescriptionColumn(),
            new ProgressBarColumn() { Width = 10 },
            new PercentageColumn(),
            new RemainingTimeColumn(),
        });

    return _currentProgress.Start(ctx =>
    {
        _currentTask = ctx.AddTask(
            description.EscapeMarkup(),
            new ProgressTaskSettings { AutoStart = true, MaxValue = maxValue });
    });
}

public void ReportProgress(int value, string? status = null)
{
    if (_currentTask is not null)
    {
        _currentTask.Value = value;
        if (status is not null)
            _currentTask.Description = status.EscapeMarkup();
    }
}
```

**Problem:** `Progress.Start()` is blocking (sync) and takes a delegate. The `StartAsync` overload exists but the delegate pattern makes it awkward to wire with `IProgress<T>` callbacks.

### Option B: Dedicated `ProgressDisplay` class

A separate class that wraps `AnsiConsole.Progress()` lifecycle:
- `Start(int maxValue, string description)` — creates Progress + ProgressTask
- `Report(int value)` — updates ProgressTask.Value
- `Stop()` — completes the progress

BackupConsoleCommand4 creates one instance, calls Start before engine.RunAsync, updates via the progress callback, and calls Stop in finally.

### Option C: Use `AnsiConsole.Live()` with a Table (like ProgramSpectreExample)

Simpler but less feature-rich than Progress widget.
- `AnsiConsole.Live(table).StartAsync(ctx => { ... })`
- Manual update loop: `ctx.UpdateTarget(table)`
- Good for showing multiple metrics per row, but requires explicit refresh loop

### Decision

**Prefer Option B** — a dedicated non-static `BackupProgressDisplay` class that wraps `AnsiConsole.Progress()` correctly.

The key insight is that `Progress.Start()` is synchronous, but we can use `Progress.StartAsync()` with an async delegate that:
1. Creates the ProgressTask
2. Awaits a signal that the backup is complete
3. On signal, completes the task and returns

Or more practically: use `Progress.StartAsync` with a loop that checks a completion flag:

```csharp
public async Task RunProgressAsync(
    int maxValue,
    string description,
    IProgress<int> progress,
    CancellationToken ct)
{
    await AnsiConsole.Progress()
        .StartAsync(async ctx =>
        {
            ProgressTask task = ctx.AddTask(
                description.EscapeMarkup(),
                new ProgressTaskSettings { AutoStart = true, MaxValue = maxValue });

            // The caller reports progress via the progress parameter
            // We just pump updates until cancellation
            while (!ct.IsCancellationRequested && task.Value < task.MaxValue)
            {
                await Task.Delay(100, ct);
            }

            task.Value = task.MaxValue;
        });
}
```

But this doesn't work because we need the caller to be able to report progress. The correct Spectre pattern for this is:

```csharp
await AnsiConsole.Progress()
    .StartAsync(async ctx =>
    {
        ProgressTask task = ctx.AddTask("Backing up files...");
        // Do work here, inside the delegate
        foreach (var file in files)
        {
            // process file
            task.Increment(1);
            task.Description = $"Processing {file.Name}";
        }
    });
```

For `IProgress<T>` callback pattern, the cleanest approach is to use a `TaskCompletionSource`:

```csharp
public async Task RunWithProgressAsync(
    BackupPlan plan,
    IBackupEngine engine,
    IProgress<BackupProgress> engineProgress,
    CancellationToken ct)
{
    TaskCompletionSource completed = new();

    Progress<BackupProgress> progress = new(p =>
    {
        // Update progress widget
        // ...
        if (p.CurrentPhase == BackupPhase.Completed)
            completed.TrySetResult();
    });

    await Task.WhenAll(
        engine.RunAsync(plan, progress, ct),
        AnsiConsole.Progress().StartAsync(async ctx =>
        {
            ProgressTask task = ctx.AddTask("Backup");
            while (!ct.IsCancellationRequested && !completed.Task.IsCompleted)
            {
                await Task.Delay(100, ct);
            }
        })
    );
}
```

This is the recommended approach.

---

## Files to Create / Modify

### Create:
- `BMTP3.Consoles/Progress/BackupProgressDisplay.cs` — Wraps `AnsiConsole.Progress()` lifecycle

### Modify:
- **`BMTP3.Consoles/Startup/ApplicationStartup.cs`** — Add `new ConsolesServiceSetup()` to service list (CRITICAL BLOCKER)
- `BMTP3.Consoles/Services/ConsolesPrinter.cs` — Remove `WriteLine` from PrintProgress overloads; delegate to BackupProgressDisplay
- `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4.cs` — Wire BackupProgressDisplay into progress handler
- `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand2.cs` — Same wiring for Core2
- `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand3.cs` — Same wiring for Core3
- `BMTP3.Consoles/Logging/SpectreAnsiConsoleLogger.cs` — Fix `MarkupLineInterpolated` unsafe calls (escape markup)
- `BMTP3.Consoles/Logging/SpectreAnsiConsoleLogger.cs` — Fix `MarkupLineInterpolated` unsafe calls (escape markup)

### Optional (port custom assets from Core):
- `BMTP3.Consoles/Progress/Columns/ValueOfMaxColumn.cs` — Copy from Core (if Option A chosen)
- `BMTP3.Consoles/Progress/Columns/ElapsedTimeAdvancedColumn.cs` — Copy from Core (if Option A chosen)

### Delete (superseded):
- None

---

## Dependencies

- **Independent** of other tasks (can be done anytime)
- Does not affect Core2/Core3/Core4 engine code
- Only Consoles project changes
- **⚠️ Blocked until `ConsolesServiceSetup` is wired in `ApplicationStartup.cs`** — otherwise progress is a no-op in production

---

## Core Reusable Assets — Catalog

These are Spectre-related files in `BMTP3.Core` that can be ported/copied to Consoles:

| Asset | Path in Core | Type | Reuse? |
|-------|-------------|------|--------|
| `ValueOfMaxColumn` | `IO/Consoles/ProgressStatus/ValueOfMaxColumn.cs` | `ProgressColumn` | **Recommended** — shows `Value/MaxValue` |
| `ElapsedTimeAdvancedColumn` | `IO/Consoles/Progress/Columns/ElapsedTimeAdvancedColumn.cs` | `ProgressColumn` | **Recommended** — elapsed time with ms |
| `CounterColumn<T>` | `IO/Consoles/ProgressStatus/CounterColumn.cs` | `ProgressColumn<T>` | Optional — generic counter |
| `CustomColumn` | `IO/Consoles/ProgressStatus/CustomColumn.cs` | `ProgressColumn` | Low value — simple `Func<string>` |
| `CustomTextColumn` | `IO/Consoles/ProgressStatus/CustomTextColumn.cs` | `ProgressColumn` | Low value — `Func<ProgressTaskState, string>` |
| `CounterColumn` (descr) | `IO/Consoles/Progress/Columns/CounterColumn.cs` | `ProgressColumn` | Low value — renders task description |
| `SequenceSpinner` | `IO/Consoles/Spinner/SequenceSpinner.cs` | `Spinner` | **Recommended** — 7 presets |
| `SimpleSpinner` | `IO/Consoles/Spinner/SimpleSpinner.cs` | `Spinner` | Low value — basic A/B/C frames |
| `FileAndDirectoryCounter` | `IO/Consoles/Progress/FileAndDirectoryCounter.cs` | Bridge class | **Reference pattern** — task desc updates |
| `ProgressStatus` (entire folder) | `IO/Consoles/ProgressStatus/` | Wrapper layer | **Not recommended** — use `Progress()` directly |
| `AnsiConsoleWrapper` | `IO/Consoles/AnsiConsoleWrapper.cs` | `IConsole` impl | Low value — Consoles already uses `IAnsiConsole` |
| Test console config | `BMTP3.Consoles.Tests/BackupConsoleIntegrationTests.cs:38-44` | Test pattern | **Document** for testing progress widget |

### Porting strategy for columns & spinners

**Decision:** Option C — use only built-in Spectre columns (`SpinnerColumn`, `TaskDescriptionColumn`, `ProgressBarColumn`, `PercentageColumn`, `RemainingTimeColumn`). Core/Core2/Core3 are reference-only — custom columns/spinners will not be ported.

---

## Acceptance Criteria

1. `ConsolesServiceSetup` is wired in `ApplicationStartup.cs` — progress works in production
2. `backup4` shows a proper progress bar (with spinner, description, percentage, remaining time) during file processing
3. No `WriteLine` flood — progress updates render in-place
4. No console crash from unescaped markup characters
5. Core4 shows proper progress via `BackupProgressDisplay`
6. Backward compatible — existing functionality unchanged
7. `SpectreAnsiConsoleLogger` no longer crashes on `[` or `]` in log messages
