# Task: Console UI Progress

> Implementer visuel progress feedback under backup (ProgressBar, Spinner).

---

## Goal

Giv brugeren visuel feedback under backup-forløb — i stedet for et tomt terminalvindue.

---

## Background

Core havde 20+ UI-filer til progress visualisering (ProgressBar, Spinner, status lines). Core4 har intet — `BackupProgress` findes men bruges ikke i Consoles.

---

## Existing Code

| Artifact | Sti |
|----------|-----|
| `BackupProgress` (Core4) | `BMTP3.Core4/Engine/BackupProgress.cs` |
| `BackupProgressItem` (Core4) | `BMTP3.Core4/Engine/BackupProgressItem.cs` |
| Core UI files (reference) | `BMTP3.Core/UserInterface/` (20+ filer) |
| ConsolesPrinter (skal opdateres) | `BMTP3.Consoles/ConsolesPrinter.cs` |

---

## Implementation

### 1. ProgressBar

```csharp
internal sealed class ProgressBar : IDisposable
{
    private readonly int _totalWidth;
    private int _current;
    private int _total;

    public void Report(int current, int total)
    {
        // Tegn: [====>    ] 45%
        // Brug Console.CursorLeft, Console.Write
    }
}
```

### 2. Spinner

```csharp
internal sealed class Spinner : IDisposable
{
    private static readonly char[] Frames = { '|', '/', '-', '\\' };
    private int _frame;

    public void Tick()
    {
        // Rotér frame i samme position
    }
}
```

### 3. Integration med BackupProgress

```csharp
// I BackupConsoleCommand4 handler:
var progress = new BackupProgress();
var progressBar = new ProgressBar();
var spinner = new Spinner();

// Subscribe på progress events
progress.OnProgressChanged += (_, _) =>
{
    progressBar.Report(progress.FilesProcessed, progress.TotalFilesSelected);
};
```

---

## Files to Create

- `BMTP3.Consoles/Progress/ProgressBar.cs`
- `BMTP3.Consoles/Progress/Spinner.cs`

## Files to Modify

- `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4.cs` — tilføj progress UI

---

## Dependencies

- Self-contained (ingen afhængighed af andre tasks)
- Kan implementeres når som helst
