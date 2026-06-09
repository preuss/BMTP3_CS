# Task: Consoles CLI Cleanup

> Ryd op i 7 identificerede CLI problemer før release af `backup4`.

---

## Goal

Gør Consoles `backup4` command produktionsklar ved at fikse 7 huller mellem CLI og Core4.

---

## Item 1: Delay / VerificationRetry / Timeout

**Problem:** `BackupConsoleCommand4.ValidateBackupOptions` validerer `--delay`, `--verification-retry-count`, `--verification-retry-delay-ms`, `--verification-timeout-ms`, `--verification-delete-on-failure`, men disse properties findes **ikke** i Core4's `BackupPlan`.

**Løsning A (anbefalet):** Fjern validering og ignorer options med warning.

```csharp
// I ValidateBackupOptions — fjern validering for disse options
// I BuildPlan — slet mapping af disse options
```

**Løsning B:** Tilføj properties til `BackupPlan` og implementer i engine.

```csharp
// I BackupPlan.cs — tilføj:
public int? DelayMs { get; init; }
public int? VerificationRetryCount { get; init; }
public int? VerificationRetryDelayMs { get; init; }
public int? VerificationTimeoutMs { get; init; }
public bool? VerificationDeleteOnFailure { get; init; }
```

**Anbefaling:** Løsning A — enklere, delay/retry hører under Retry/Resilience task (04).

### Files to modify:
- `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4.cs` — `ValidateBackupOptions`
- `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4Helpers.cs` — `BuildPlan`

---

## Item 2: MTP sourcePath format

**Problem:** `--source-device "Apple iPhone"` sætter bart device navn, men Core4 forventer `mtp://Apple iPhone/Internal Storage/DCIM`.

**Løsning:** Konstruer `mtp://{deviceName}/{subPath}` URI i `BuildPlan` når `sourceType == MediaDevice`.

```csharp
// I BackupConsoleCommand4Helpers.BuildPlan
string sourcePath = planArgs.SourceType switch
{
    BackupSourceType.MediaDevice => $"mtp://{deviceName}/{subPath}",
    _ => planArgs.SourcePath
};
```

### Files to modify:
- `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4Helpers.cs` — `BuildPlan`
- `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4.cs` — extract deviceName + subPath

---

## Item 3: `--backup-index` default guard

**Problem:** CLI default er `Json`, men writer er ikke implementeret endnu.

**Løsning:** Når `BackupIndexType.Json` er implementeret (Task 01), sæt default til `Json`. Indtil da, sæt default til `None`.

Efter Task 01 er done: skift default til `Json`.

### Files to modify:
- `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4.cs` — `--backup-index` default

---

## Item 4: Core2 options i ApplicationServiceSetup

**Problem:** Linje 37 i `ApplicationServiceSetup`:

```csharp
services.Configure<BackupEngineOptions>(options => { ... });
```

Dette konfigurerer **Core2's** `BackupEngineOptions`, ikke Core4. Har ingen effekt på `backup4`.

**Løsning:** Fjern linjen.

### Files to modify:
- `BMTP3.Consoles/ApplicationServiceSetup.cs:37`

---

## Item 5: SignalInterrupt cancel-wiring

**Problem:** `BackupConsoleCommand4` bruger `Console.CancelKeyPress` i stedet for Core4's `SignalInterrupt` system.

**Nuværende:**

```csharp
Console.CancelKeyPress += (sender, args) =>
{
    args.Cancel = true;
    cts.Cancel();
};
```

**Ønsket:**

```csharp
using ISignalSubscription subscription = SignalInterrupt
    .On(SignalInterruptKind.Interrupt)
    .Handler(ctx => { cts.Cancel(); })
    .Create();
```

### Files to modify:
- `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4.cs` — erstat `Console.CancelKeyPress` med `SignalInterrupt`
- Sikr `using` scope så subscription cleanup sker automatisk

---

## Item 6: ConsolesPrinter progress

**Problem:** ConsolesPrinter viser ikke Core4 progress data (`BytesProcessed`, `TotalFilesSelected`, `FilesSkipped`).

**Løsning:** Opdater `ConsolesPrinter` overloads til at håndtere Core4's `BackupProgress`:

```csharp
// I ConsolesPrinter.cs — tilføj eller opdater overload:
public void PrintProgress(BackupProgress progress)
{
    WriteLine($"  Processed: {progress.BytesProcessed} bytes");
    WriteLine($"  Files: {progress.FilesProcessed}/{progress.TotalFilesSelected}");
}
```

### Files to modify:
- `BMTP3.Consoles/ConsolesPrinter.cs` — tilføj Core4 progress overload
- `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4.cs` — brug printer progress

---

## Item 7: No tests for backup4

**Problem:** Ingen tests for `BackupConsoleCommand4Helpers.BuildPlan` enum-mapping.

**Løsning:** Tilføj tests i `BMTP3.Consoles.Tests`:

```csharp
[Fact]
public void BuildPlan_Maps_BackupSourceType()
{
    // Arrange
    // Act
    // Assert
}
```

Dæk:
- `BackupSourceType` → `SourceType` mapping
- `OutputStructureStrategy` mapping
- `CollisionStrategy`, `CollisionComparisonType`, `RenameStrategy` mapping
- `SidecarFormat` mapping
- `BackupIndexType` mapping
- `HashAlgorithmType` mapping
- `PostWriteVerificationType` mapping

### Files to create:
- `BMTP3.Consoles.Tests/BackupConsoleCommand4HelpersTests.cs`

---

## Dependencies

- Item 3 afhænger af Task 01 (BackupIndexType.Json implementeret)
- Item 5 kræver `using` forståelse — subscription skal leve hele command level
- Item 6 kan gøres uafhængigt
