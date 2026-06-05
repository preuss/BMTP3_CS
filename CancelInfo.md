# CancelInfo — Console Lifetime & Cancellation

## Oversigt

Dokumentet samler al viden om cancellation på tværs af Core, Core2, Core3, Core4, Consoles, samt en detaljeret plan for en ny generisk `IConsoleLifetime`-komponent i `BMTP3.Common`.

---

## 1. Nuværende cancellation-mønstre på tværs af projekter

### 1.1 CancellationToken-forbrug

| Projekt | Mønster |
|---------|---------|
| **Core** | `CancellationTokenGenerator` wrapper om CTS. `BackupMaster`/`BackupHandler`/`BackupHandlerForDevice` injecter `CancellationTokenGenerator`, kalder `.NewToken()` og `.GetCancellationTokenSource()` |
| **Core2** | Alle async metoder i engine (scanning, transfer, hashing, sidecar, collision resolution) tager `CancellationToken ct`. Pipeline stages bruger `ChannelReader.ReadAllAsync(ct)`. `MtpGatekeeper` laver `CreateLinkedTokenSource` med timeout |
| **Core3** | `BackupEngineSequential.RunAsync(plan, progress, ct)` med `ct.ThrowIfCancellationRequested()` før hver fase. `catch(OperationCanceledException)` per fase. Fakes i tests checker `ct.ThrowIfCancellationRequested()` |
| **Core4** | Alle async metoder tager `CancellationToken`. `BackupEngine.RunAsync` wrapper hele løbet i `try { ... } catch(OperationCanceledException) { return BackupResult { State = Cancelled } }`. Tests bruger `new CancellationTokenSource()` + `cts.Cancel()` |
| **Consoles** | `BackupConsoleCommand2`/`3` laver `CreateLinkedTokenSource(cancellationToken)` + `Console.CancelKeyPress` handler lokalt |

### 1.2 Kernel32 SetConsoleCtrlHandler

**Findes kun i:** `BMTP3.Core/Handlers/EventHandlers/ConsoleEventHandler.cs`

```csharp
[DllImport("kernel32.dll", SetLastError = true)]
private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

public void Register() {
    SetConsoleCtrlHandler(_consoleEventDelegate, true);
    System.Console.CancelKeyPress += CancelKeyPressHandler;
}
```

- Håndterer `CTRL_C_EVENT`, `CTRL_BREAK_EVENT`, `CTRL_CLOSE_EVENT`, `CTRL_LOGOFF_EVENT`, `CTRL_SHUTDOWN_EVENT`
- `HandleShutdownEvent` returnerer `ContinuePropagation` (gør intet — lader systemet lukke)
- `HandleCtrlEvent` kalder `_cancellationTokenSource.Cancel()` og returnerer `StopPropagation`
- **Afhængig af Spectre.Console** (`IAnsiConsole`)
- **Statisk initialisering** via `ConsoleEventHandler.Initialize(cts, console?)`

### 1.3 Console.CancelKeyPress

| Lokation | Stil |
|----------|------|
| `Core/ConsoleEventHandler.cs` | `System.Console.CancelKeyPress += CancelKeyPressHandler` — canceller CTS, sætter `e.Cancel = true` |
| `Core/IO/Consoles/IConsole.cs` | Event definition: `event ConsoleCancelEventHandler? CancelKeyPress { add { Console.CancelKeyPress += value; } ... }` |
| `Core/Extensions/AnsiConsoleExtensions.cs` | Extension methods `AddCancelKeyPressHandler`/`RemoveCancelKeyPressHandler` |
| `Consoles/BackupConsoleCommand2.cs` | Lokal lambda: `(s, e) => { e.Cancel = true; linkedCts.Cancel(); }`. Register i start, unregister i `finally` |
| `Consoles/BackupConsoleCommand3.cs` | Samme mønster som Command2 |

### 1.4 Hjælpe-typer

| Type | Projekt | Purpose |
|------|---------|---------|
| `CtrlType` enum | Core/Handlers/EventHandlers | Kernel32 signal types: `CTRL_C_EVENT(0)`, `CTRL_BREAK_EVENT(1)`, `CTRL_CLOSE_EVENT(2)`, `CTRL_LOGOFF_EVENT(5)`, `CTRL_SHUTDOWN_EVENT(6)` |
| `EventPropagationType` enum | Core/Handlers/EventHandlers | `ContinuePropagation(0)` = return false, `StopPropagation(1)` = return true |
| `CancellationTokenGenerator` | Core/Configuration | Wrapper om CTS: `.NewToken()`, `.GetCancellationTokenSource()`, `IDisposable` |
| `BackupCanceledException` | Core/Exceptions | Custom `OperationCanceledException` med `Operation` property |

### 1.5 Signal mapping: CtrlTypes (Windows) vs PosixSignal (.NET) vs POSIX (Unix)

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│ CtrlTypes (Windows)              │ PosixSignal (.NET)    │ Linux POSIX │ Beskrivelse     │
├──────────────────────────────────┼───────────────────────┼─────────────┼─────────────────┤
│ CTRL_C_EVENT       = 0          │ SIGINT   = -2        │ signo  2    │ Ctrl+C          │
│ CTRL_BREAK_EVENT   = 1          │ SIGQUIT  = -3        │ signo  3    │ Ctrl+Break      │
│ CTRL_CLOSE_EVENT   = 2          │ SIGHUP   = -1        │ signo  1    │ Luk vindue      │
│ CTRL_LOGOFF_EVENT  = 5          │ ❌                   │ N/A         │ Logoff (Win)    │
│ CTRL_SHUTDOWN_EVENT = 6         │ SIGTERM  = -4        │ signo 15    │ System shutdown │
│ ❌                             │ SIGCHLD  = -5        │ signo 17    │ Child stopped   │
│ ❌                             │ SIGCONT  = -6        │ signo 18    │ Fortsæt proces  │
│ ❌                             │ SIGWINCH = -7        │ signo 28    │ Vindue resize   │
│ ❌                             │ SIGTTIN  = -8        │ signo 21    │ Bg terminal in  │
│ ❌                             │ SIGTTOU  = -9        │ signo 22    │ Bg terminal out │
│ ❌                             │ SIGTSTP  = -10       │ signo 20    │ Stop (Ctrl+Z)   │
│ ❌                             │ SIGKILL  = -11       │ signo  9    │ Kill (uhåndterl)│
└──────────────────────────────────┴───────────────────────┴─────────────┴─────────────────┘
```

`PosixSignalRegistration` på Windows bruger `SetConsoleCtrlHandler` internt og mapper:
- `SIGINT`  → `CTRL_C_EVENT` (0)
- `SIGQUIT` → `CTRL_BREAK_EVENT` (1)
- `SIGHUP`  → `CTRL_CLOSE_EVENT` (2)
- `SIGTERM` → `CTRL_SHUTDOWN_EVENT` (6)

På Linux bruger `PosixSignalRegistration` POSIX `sigaction` med egentlige signalnumre.

### 1.6 Entry point wiring (Core's gamle mønster)

#### Core/Program.cs
```csharp
private static readonly CancellationTokenSource _cts;
// Static constructor: _cts = serviceProvider.GetService<CancellationTokenSource>()!
using(_cts) {
    ConsoleEventHandler.Initialize(_cts);
    // ... hele applikationen ...
}
```

#### Core/StartUp.cs
```csharp
services.AddSingleton<CancellationTokenSource>(_cancellationTokenSource);
services.AddSingleton<CancellationTokenGenerator>(sp =>
    new CancellationTokenGenerator(sp.GetRequiredService<CancellationTokenSource>()));
```

#### Consoles/ConsolesProgram.cs
```csharp
// INGEN ConsoleEventHandler eller SetConsoleCtrlHandler
// Kun CancellationToken fra System.CommandLine
// Hver command laver selv sin CancelKeyPress + CreateLinkedTokenSource
```

---

### 1.7 KERN — De to mekanismer og hvorfor begge er nødvendige

Dette er den vigtigste del at forstå. Core's `ConsoleEventHandler` samler **to fundamentalt forskellige** mekanismer:

#### Mekanisme 1: kernel32 `SetConsoleCtrlHandler` (native)

| Egenskab | Værdi |
|----------|-------|
| **Hvor kører den?** | Native OS handler thread (ikke .NET thread pool) |
| **Hvad fanger den?** | Ctrl+C, Ctrl+Break, Window Close, Logoff, Shutdown |
| **Kan man bruge async?** | **Nej** — det er en native callback, async/await vil crashe |
| **Hvad kan man gøre?** | Cancel et CancellationTokenSource, return true/false |
| **Timeout?** | ~5 sekunder for close/logoff/shutdown — så dræber OS processen |

```csharp
// Denne funktion kører på en NATIVE OS handler thread!
private bool ConsoleEventCallback(CtrlType eventType) {
    // ❌ Kan ikke bruge async/await
    // ❌ Kan ikke allokere ret meget
    // ✅ Cancel på CTS (thread-safe)
    // ✅ Return true = "jeg har håndteret det, stop propagation"
    // ✅ Return false = "jeg har ikke håndteret det, kald næste handler"
    _cancellationTokenSource.Cancel();
    return true; // StopPropagation
}
```

Ved `CTRL_CLOSE_EVENT`, `CTRL_LOGOFF_EVENT`, `CTRL_SHUTDOWN_EVENT` har du ~5 sekunder før `ExitProcess` kaldes — **uanset** om du returnerer true eller false. Du kan kun nå at gemme kritisk tilstand.

#### Mekanisme 2: .NET `Console.CancelKeyPress` (managed)

| Egenskab | Værdi |
|----------|-------|
| **Hvor kører den?** | .NET managed thread pool |
| **Hvad fanger den?** | Kun Ctrl+C og Ctrl+Break |
| **Kan man bruge async?** | Teknisk ja, men **ikke** await pga. EventHandler signaturen |
| **Timeout?** | **Ubegrænset** hvis `e.Cancel = true` |
| **Hvad kan man gøre?** | Alt .NET kan — logge, sætte flags, kalde metoder |

```csharp
private void CancelKeyPressHandler(object? sender, ConsoleCancelEventArgs e) {
    // Denne kører på managed thread pool — .NET-venlig
    _cancellationTokenSource.Cancel();
    e.Cancel = true; // ← KRITISK: forhindrer ExitProcess
}                    // Uden dette dræbes processen MOMENTANT
```

#### Race condition ved Ctrl+C

Når brugeren trykker Ctrl+C, sker **begge dele samtidig**:

```
OS sender CTRL_C_EVENT
├── kernel32 callback → native thread → _cts.Cancel()
└── .NET modtager signal
    └── CancelKeyPress → managed thread → _cts.Cancel()
                         → e.Cancel = true (forhindrer ExitProcess)
```

Den ene kommer først — den anden er harmless (CTS er allerede cancelled). Men man ved ikke hvilken. **Derfor skal begge mekanismer håndteres.**

#### Hvorfor `e.Cancel = true` er kritisk

`.NET's `CancelKeyPress` fyrer signalet, og **bagefter** kalder .NET som standard `ExitProcess`. Hvis du sætter `e.Cancel = true`, siger du: "Jeg håndterer det selv — kald ikke ExitProcess". Det giver applikationen tid til at rydde op via CancellationToken-mekanismen.

Uden `e.Cancel = true`:
```
Ctrl+C → CancelKeyPress → (din handler) → ExitProcess → ✅ Processen lukker
```

Med `e.Cancel = true`:
```
Ctrl+C → CancelKeyPress → (din handler, e.Cancel = true) → ExitProcess forhindres
                                                          → CancellationToken propageres
                                                          → Async opgaver får tid til at stoppe
```

#### Opsummering: Hvorfor begge?

| Hvis du KUN har | Så misser du |
|-----------------|--------------|
| `CancelKeyPress` | Window close, Logoff, Shutdown (kernel32-events) |
| `SetConsoleCtrlHandler` | Muligheden for `e.Cancel = true` og ubegrænset cleanup-tid for Ctrl+C |

**Core's `ConsoleEventHandler` gør begge dele — og det var det smarte.**

---

## 2. Hvad mangler: Problemstillinger

1. **Ingen fælles komponent** — Consoles/Commands2/3 duplikerer `CancelKeyPress` logik med linked CTS
2. **Core's ConsoleEventHandler er ikke generisk** — afhængig af Spectre.Console, statisk API, tæt knyttet til Core
3. **Core2/3/4 har ingen console event handling** — de er rene biblioteker, men mangler en måde at få en CancellationToken fra en lifetime-manager
4. **Kernel32 shutdown-events fanges kun i Core** — Core2/3/4 kan ikke reagerer på window close/logoff/shutdown
5. **Consoles har ikke kernel32** — `SetConsoleCtrlHandler` er aldrig kaldt i Consoles projektet
6. **Ingen event args** — nuværende `ConsoleEventHandler` fortæller ikke konsumenten hvilken type signal der kom, eller om det er tidspresset

---

## 3. Plan: Ny generisk komponent `IConsoleLifetime`

### 3.1 Mål

- **Placering:** `BMTP3.Common/Hosting/`
- **Zero dependencies** — ingen Spectre.Console, ingen ekstern CTS injection
- **Platform-sikker** — kernel32 kun på Windows via `OperatingSystem.IsWindows()`
- **DI-venlig** — `Start()` adskilt fra constructor
- **Genbrugelig** — alle projekter kan bruge den (Core, Core4, Consoles, tests)
- **Testbar** — `Cancel()` kan kaldes direkte i tests
- **Event-baseret** — konsumenter får detaljerede event args om hvert signal

### 3.2 API Design

#### ConsoleSignal (ny samlet enum)

```csharp
// BMTP3.Common/Hosting/ConsoleSignal.cs
namespace BMTP3.Common.Hosting;

public enum ConsoleSignal
{
    CtrlC = 0,
    CtrlBreak = 1,
    Close = 2,       // Window close
    Logoff = 5,
    Shutdown = 6
}
```

Værdierne matcher `CtrlType` fra kernel32 API'et, så enum'en kan castes direkte.

#### ConsoleSignalEventArgs

```csharp
// BMTP3.Common/Hosting/ConsoleSignalEventArgs.cs
namespace BMTP3.Common.Hosting;

public class ConsoleSignalEventArgs : EventArgs
{
    /// <summary>Hvilken type signal modtog vi?</summary>
    public ConsoleSignal Signal { get; }

    /// <summary>
    /// True hvis processen vil blive termineret om ~5 sekunder uanset hvad.
    /// Gælder for Close, Logoff, Shutdown.
    /// False for Ctrl+C og Ctrl+Break (ubegrænset tid med e.Cancel = true).
    /// </summary>
    public bool IsTerminationImminent { get; }

    /// <summary>
    /// Ca. sekunder før OS terminerer processen.
    /// null for Ctrl+C / Ctrl+Break (ubegrænset tid).
    /// 5 for Close / Logoff / Shutdown.
    /// </summary>
    public int? TimeoutSeconds { get; }

    /// <summary>
    /// True hvis dette event kom fra kernel32 handleren (native thread).
    /// False hvis det kom fra Console.CancelKeyPress (managed thread).
    /// </summary>
    public bool IsFromKernel32 { get; }

    /// <summary>
    /// Sæt til true for at stoppe propagation / forhindre ExitProcess.
    /// For kernel32: returneres som true fra callback.
    /// For CancelKeyPress: sætter e.Cancel = true.
    /// Default: false (propager videre).
    /// </summary>
    public bool StopPropagation { get; set; }

    public ConsoleSignalEventArgs(ConsoleSignal signal, bool isFromKernel32)
    {
        Signal = signal;
        IsFromKernel32 = isFromKernel32;
        IsTerminationImminent = signal is ConsoleSignal.Close
            or ConsoleSignal.Logoff
            or ConsoleSignal.Shutdown;
        TimeoutSeconds = IsTerminationImminent ? 5 : null;
    }
}
```

#### IConsoleLifetime

```csharp
// BMTP3.Common/Hosting/IConsoleLifetime.cs
namespace BMTP3.Common.Hosting;

public interface IConsoleLifetime : IDisposable
{
    /// <summary>Fires når et console signal modtages (Ctrl+C, Break, Close, Logoff, Shutdown).</summary>
    event EventHandler<ConsoleSignalEventArgs>? SignalReceived;

    /// <summary>Fires lige før disposal — garanterer at CancellationToken er cancelled.</summary>
    event EventHandler? Disposing;

    /// <summary>CancellationToken der bliver cancelled ved ethvert signal.</summary>
    CancellationToken CancellationToken { get; }
}
```

#### ConsoleLifetime

```csharp
// BMTP3.Common/Hosting/ConsoleLifetime.cs
namespace BMTP3.Common.Hosting;

public sealed class ConsoleLifetime : IConsoleLifetime
{
    public event EventHandler<ConsoleSignalEventArgs>? SignalReceived;
    public event EventHandler? Disposing;
    public CancellationToken CancellationToken => _cts.Token;

    public ConsoleLifetime();
    public ConsoleLifetime(CancellationToken externalToken);

    public void Start();
    public void Cancel();

    public void Dispose();
}
```

### 3.3 Implementation details

#### kernel32 handler (Windows only)

```csharp
private delegate bool ConsoleEventDelegate(CtrlType eventType);

[DllImport("kernel32.dll", SetLastError = true)]
private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

// Kører på NATIVE OS handler thread — async forbudt!
private bool Kernel32Handler(CtrlType eventType)
{
    var args = new ConsoleSignalEventArgs((ConsoleSignal)eventType, isFromKernel32: true);
    SignalReceived?.Invoke(this, args);

    _cts.Cancel();

    // Shutdown-events: OS terminerer om ~5 sek uanset hvad
    // Ctrl+C/Break: StopPropagation = true = "vi har håndteret det"
    return args.StopPropagation;
}
```

#### CancelKeyPress (alle platforme)

```csharp
private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    var signal = e.SpecialKey switch
    {
        ConsoleSpecialKey.ControlC => ConsoleSignal.CtrlC,
        ConsoleSpecialKey.ControlBreak => ConsoleSignal.CtrlBreak,
        _ => ConsoleSignal.CtrlC
    };

    var args = new ConsoleSignalEventArgs(signal, isFromKernel32: false);
    SignalReceived?.Invoke(this, args);

    _cts.Cancel();
    e.Cancel = args.StopPropagation; // default false → true hvis subscriber siger stop
}
```

#### Start / Dispose

```csharp
public void Start()
{
    if (_started) return;
    _started = true;

    if (OperatingSystem.IsWindows())
    {
        SetConsoleCtrlHandler(_kernel32Callback, true);
    }

    System.Console.CancelKeyPress += OnCancelKeyPress;
}

public void Dispose()
{
    if (_disposed) return;
    _disposed = true;

    Disposing?.Invoke(this, EventArgs.Empty);

    if (OperatingSystem.IsWindows())
    {
        SetConsoleCtrlHandler(_kernel32Callback, false);
    }

    System.Console.CancelKeyPress -= OnCancelKeyPress;
    _cts.Cancel();
    _cts.Dispose();
}
```

### 3.4 Sådan bruges det

#### Simpel — kun cancellation

```csharp
using var lifetime = new ConsoleLifetime();
lifetime.Start();

await engine.RunAsync(plan, progress, lifetime.CancellationToken);
// Ctrl+C → CTS canceles → engine stopper
```

#### Med event handling — reager på signaltype

```csharp
using var lifetime = new ConsoleLifetime();
lifetime.SignalReceived += (sender, args) =>
{
    if (args.IsTerminationImminent)
    {
        // Window close / Logoff / Shutdown — ~5 sekunder!
        Console.Error.WriteLine("Nødsituation: gemmer kritisk tilstand...");
        SaveEmergencyState();
    }
    else if (args.Signal == ConsoleSignal.CtrlC)
    {
        Console.WriteLine("Ctrl+C modtaget — lukker ned...");
    }

    // For CancelKeyPress: forhindrer ExitProcess
    args.StopPropagation = true;
};

lifetime.Start();
await engine.RunAsync(plan, progress, lifetime.CancellationToken);
```

#### Consoles — linked med System.CommandLine token

```csharp
protected override async Task<int> DoExecuteAsync(ParseResult parseResult, CancellationToken ct)
{
    using var lifetime = new ConsoleLifetime(ct);
    lifetime.SignalReceived += (_, args) =>
    {
        logger.LogInformation("Signal modtaget: {Signal}, shutdown: {IsShutdown}",
            args.Signal, args.IsTerminationImminent);
        args.StopPropagation = true;
    };
    lifetime.Start();

    await engine.RunAsync(plan, progress, lifetime.CancellationToken);
}
```

### 3.5 Tests (i BMTP3.Common.Tests)

```csharp
[Fact]
public void Cancel_SetsCancellationToken()
{
    using var lifetime = new ConsoleLifetime();
    lifetime.Cancel();
    Assert.True(lifetime.CancellationToken.IsCancellationRequested);
}

[Fact]
public void Cancel_FiresSignalReceived()
{
    using var lifetime = new ConsoleLifetime();
    ConsoleSignal? received = null;
    lifetime.SignalReceived += (_, args) => received = args.Signal;

    lifetime.Cancel(); // kalder _cts.Cancel internt

    // Cancel() skal fire event — eller skal den?
    // Cancel er en "manuel" cancellation, ikke et OS-signal.
    // Måske skal vi have en anden mekanisme for Cancel()?
}

[Fact]
public void SignalReceived_StopPropagationTrue_StopsPropagation()
{
    // Svært at teste uden faktisk at trykke Ctrl+C
    // Men Cancel() kan vi teste
}

[Fact]
public void Dispose_CanBeCalledMultipleTimes_NoException()
{
    var lifetime = new ConsoleLifetime();
    lifetime.Dispose();
    lifetime.Dispose();
}

[Fact]
public void Constructor_WithExternalToken_LinksToExternalCts()
{
    using var externalCts = new CancellationTokenSource();
    using var lifetime = new ConsoleLifetime(externalCts.Token);

    externalCts.Cancel();
    Assert.True(lifetime.CancellationToken.IsCancellationRequested);
}
```

### 3.6 Migrationsplan

| Trin | Handling |
|------|----------|
| 1 | Opret `ConsoleSignal`, `ConsoleSignalEventArgs`, `IConsoleLifetime`, `ConsoleLifetime` i `BMTP3.Common/Hosting/` |
| 2 | Tests for `ConsoleLifetime` i `BMTP3.Common.Tests` |
| 3 | Erstat `ConsoleEventHandler` i Core med `ConsoleLifetime` (evt. behold gamle som adapter) |
| 4 | Erstat `CancelKeyPress` duplikation i `BackupConsoleCommand2.cs` + `BackupConsoleCommand3.cs` |
| 5 | Wire `ConsoleLifetime` i Core4 entry point |
| 6 | Overvej at fjerne gamle `CancellationTokenGenerator` til fordel for `IConsoleLifetime` |

### 3.7 Fremtidige muligheder

- **Shutdown callback** — en `Func<CancellationToken, Task>` der kaldes ved termination-imminent, så applikationen kan gemme tilstand asynkront (dog max ~5 sek)
- **ILogger integration** — valgfri `ILogger<ConsoleLifetime>` for diagnostic
- **TimeoutSeconds** — kunne gøres konfigurerbar (selvom det reelt er OS-bestemt)

---

## 4. Filer der påvirkes

### Nye filer
| Fil | Indhold |
|-----|---------|
| `BMTP3.Common/Hosting/ConsoleSignal.cs` | Enum (CtrlC, CtrlBreak, Close, Logoff, Shutdown) |
| `BMTP3.Common/Hosting/ConsoleSignalEventArgs.cs` | EventArgs med Signal, IsTerminationImminent, TimeoutSeconds, StopPropagation |
| `BMTP3.Common/Hosting/IConsoleLifetime.cs` | Interface med event + CancellationToken |
| `BMTP3.Common/Hosting/ConsoleLifetime.cs` | Implementation med kernel32 + CancelKeyPress |

### Eksisterende filer der senere kan refaktorers
| Fil | Hvad |
|-----|------|
| `Core/Handlers/EventHandlers/ConsoleEventHandler.cs` | Kan wrappe eller erstattes |
| `Core/Handlers/EventHandlers/CtrlType.cs` | Kan fjernes (ConsoleSignal erstatter) |
| `Core/Handlers/EventHandlers/EventPropagationType.cs` | Kan fjernes (bool StopPropagation i EventArgs) |
| `Core/Configuration/CancellationTokenGenerator.cs` | Kan erstattes af `IConsoleLifetime` |
| `Consoles/ConsoleCommands/BackupConsoleCommand2.cs` | Brug `ConsoleLifetime` i stedet for lokal CancelKeyPress |
| `Consoles/ConsoleCommands/BackupConsoleCommand3.cs` | Samme |
| `Core4/Program.cs` (entry point) | Brug `ConsoleLifetime` |

---

## 5. Bilag: Core4 cancellation flow (efter Ctrl+C Del 1)

```
BackupEngine.RunAsync(plan, progress, ct)
├── ct.ThrowIfCancellationRequested()
├── ValidatePlan(plan)  →  ct.ThrowIfCancellationRequested()
├── DiskCheck → ct.ThrowIfCancellationRequested()
├── Traverse/Scan → ct.ThrowIfCancellationRequested()
├── await foreach (var item in scanner.ScanAsync(...))
│   └── ct.ThrowIfCancellationRequested() per item
├── SaveSession → ct.ThrowIfCancellationRequested()
├── Runner per item:
│   ├── Download → ct.ThrowIfCancellationRequested()
│   ├── Hash → ct.ThrowIfCancellationRequested()
│   ├── Compare → ct.ThrowIfCancellationRequested()
│   ├── Sidecar → ct.ThrowIfCancellationRequested()
│   └── catch (ex) when (ex is not OperationCanceledException)
├── BuildItemResults(records)
└── catch(OperationCanceledException)
    ├── Log + CurrentPhase = Completed
    └── return BackupResult { State = Cancelled, ItemResults = BuildItemResults(...) }
```
