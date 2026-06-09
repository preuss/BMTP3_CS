# Task: Retry / Resilience

> Implementer exponential backoff retry for transient I/O failures og MTP resilience.

---

## Goal

Gør Core4 robust over for transient fejl under backup (netværk, USB disconnect, COMException) ved at tilføje retry-logik med exponential backoff.

---

## Background

Core2 havde `BackupResiliencePipeline` baseret på Polly (NuGet). Core4 har ingen resilience — en transient fejl under download/hash/move/sidecar abort hele backup.

Dette er især kritisk for MTP hvor `COMException 0x802A0001` (device disconnected) kan opstå midlertidigt.

---

## Existing Code

| Artifact | Sti |
|----------|-----|
| Core2 `BackupResiliencePipeline` (Polly) | `BMTP3.Core2/BackupNew/Engine/BackupResiliencePipeline.cs` |
| Core2 `ResiliencePipeline` enum (Default/HighLatency/MTP) | `BMTP3.Core2/BackupNew/Api/Request/Enums/ResiliencePipeline.cs` |
| Gatekeeper timeout (MTP) | `BMTP3.Core4/Traversal/MtpGatekeeper.cs` |
| Per-item try-catch i engine | `BMTP3.Core4/Engine/BackupEngine.cs:485` (re-thrower) |

---

## Implementation Approach

**Mulighed A (anbefalet):** Implementer lightweight retry helper — ingen ekstern NuGet dependency.

```csharp
internal static class RetryHelper
{
    public static async Task<T> RetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        CancellationToken cancellationToken = default)
    {
        // Exponential backoff: baseDelay * 2^attempt
        // Fanger kun transient exceptions (TimeoutException, IOException, COMException)
    }
}
```

**Mulighed B:** Brug Polly NuGet — mere feature-complete men tilføjer dependency.

---

## Scope

| Hvor | Hvad | Prioritet |
|------|------|-----------|
| Download | `IDownloadService` — temp fil download | Høj |
| Hash | `IHashService` — stream hashing | Medium |
| Move | `MoveableFileContent.MoveTo` — File.Move | Lav |
| Sidecar write | `SidecarService.WriteAsync` — File.Write | Lav |
| MTP gatekeeper | COMException → reconnect + retry | Høj |

---

## Files to Create

### `BMTP3.Core4/Engine/Resilience/RetryHelper.cs`

```csharp
namespace BMTP3.Core4.Engine.Resilience;

internal static class RetryHelper
{
    public static async Task<T> RetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default);
}
```

### `BMTP3.Core4/Engine/Resilience/RetryPolicy.cs`

Optional: policy config (max retries, delay, which exceptions to retry).

---

## Files to Modify

### `BMTP3.Core4/Engine/Downloader/DownloadService.cs`

Wrap `DownloadAsync` med retry.

### `BMTP3.Core4/Engine/Hashing/HashService.cs`

Wrap `ComputeHashesAsync` med retry.

### `BMTP3.Core4/Traversal/MtpGatekeeper.cs`

Tilføj retry ved COMException: disconnect → wait → reconnect → retry operation.

### `BMTP3.Core4/Engine/BackupEngine.cs`

Overvej om per-item try-catch skal have retry (i stedet for at re-throw med det samme).

---

## Dependencies

- Self-contained (kan implementeres i vilkårlig rækkefølge)
- Ingen blokering fra andre tasks
