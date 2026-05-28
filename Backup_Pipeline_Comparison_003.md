# Backup Pipeline Comparison 003: Core / Core2 / Core3 vs Core4

> Genereret 28. maj 2026 baseret på fuld audit af BMTP3.Core, BMTP3.Core2, BMTP3.Core3 og BMTP3.Core4.
> Formål: Én reference til at forstå arkitekturforskelle, designvalg, og hvad der mangler i Core4 — med projektkontekst.

---

## 0. Hvert Projekts Rolle og Lekser

Før den tekniske sammenligning: en forståelse af **hvorfor hver version findes** og hvad den har lært os.

### 0.1 Original Core — Den Funktionelle Sandhed

Original Core er **den funktionelle reference**. Den har historisk virket end-to-end og har alle vigtige backup-funktioner implementeret: MTP device discovery, filenumeration, download, EXIF-metadata, hash-beregning (7 algoritmer), collision resolution med binary compare, sidecar-generering (INI med device info), resume persistence (Newtonsoft JSON), og cleanup.

**Styrke:** Beviser hvad programmet skal kunne.

**Problem:** Monolitisk (`BackupHandler` ~1350 linjer), tightly coupled, synkront, fuldt materialiseret (`List<T>` før processering), svært at teste og refactorere.

**Brug som:** Svar på "Hvad skal backup-programmet kunne?" — ikke som arkitektur-model for Core4.

### 0.2 Core2 — Komponent- og Idébank

Core2 var et ambitiøst rebuild med bedre arkitektur: generisk `ITraversalScanner<TEntry>`, `IContent` abstraction, `IMtpGatekeeper` (SemaphoreSlim), `GatekeptStream`, pipeline stages med `Channel<T>`, fuld async/await, og feature-complete plan.

**Styrke:** Introducerede gode patterns: gatekeeper, content abstraction, generisk traversal, glob filtering (`GlobMatcher`), session lifecycle.

**Problem:** Blev for kompleks for tidligt — fuld async, multithreaded, channel-based, højt generisk. Blev aldrig stabil nok.

**Brug som:** Selektiv idébank — genbrug gatekeeper, content patterns, og design-ideer, men **ikke** som target arkitektur.

**Regel:** Core4 må ikke blive Core2 igen.

### 0.3 Core3 — Advarsel og Lærestreg

Core3 var et hurtigt single-threaded forsøg på at reducere Core2's kompleksitet. Det blev aldrig færdigt og virkede aldrig end-to-end.

**Lektie:** Single-threaded er det rigtige udgangspunkt, men en hurtig halv-implementering er ikke nok. Spring ikke arkitekturen over bare fordi motoren er single-threaded. Lav ikke midlertidige implementationer.

**Brug som:** Advarsel — ikke som target arkitektur.

### 0.4 Core4 — Det Nye Produktionsgrundlag

Core4 er den sidste seriøse indsats for at skabe en ren, stabil, vedligeholdelig backup-engine.

**Core4 skal være:**
- Clean, understandable, maintainable, testable
- Single-threaded **først** (korrekthed før performance)
- Bygget feature by feature i små verificerede skridt
- I stand til at erstatte Original Core i reel brug

**Core4 må ikke:**
- Blive en monolyt som Original Core
- Blive en channel-baseret multipipeline som Core2
- Lave hurtige halve implementationer som Core3
- Indføre parallelisme før den sekventielle engine virker end-to-end

**Nuværende tilstand:** Core4 har solid arkitektur med interfaces, DI (TryAdd), single-pass multi-hash, timestamp-resolution, delvist implementeret collision resolution, sidecar generation, og session state.

Bemærk: `CollisionStrategy.Overwrite` er ikke fuldt fungerende endnu, fordi `MoveableFileContent.MoveTo` accepterer en `overwrite` parameter men aktuelt kalder `FileInfo.MoveTo(dest, false)`.

Men den har et kritisk problem: `BackupPlanValidator` blokerer ALLE plans (C4), så engine kan ikke modtage en fungerende plan.

---

## 1. Top-Level Architecture Overview

| Dimension | Core (legacy) | Core2 | Core3 | Core4 |
|-----------|---------------|-------|-------|-------|
| **Status** | ✅ Historisk fungerende | ❌ Blev for kompleks, ustabil | ❌ Aldrig færdig | 🏗 Under aktiv udvikling |
| **Orchestrator** | `BackupHandler` (~1350 linjer, monolithic) | `BackupEngine` (parallel channels) + `BackupEngineSequentiel` | `BackupEngineSequential` (sequential, partial) | `BackupEngine` (sequential, inline) |
| **Abstraktionsgrad** | Minimal — få interfaces, mange konkrete klasser | Høj — interfaces overalt, DI-first | Medium — nogle interfaces | Høj — interfaces overalt, DI-first |
| **Async** | Synkron (bortset fra `ScanAsync` unused) | Fuld async/await | Delvis async | Fuld async/await |
| **Streaming** | Full materialization (`IList`, `FrozenDictionary`) | Lazy (`IAsyncEnumerable`, `Channel<T>`) | Lazy (`IAsyncEnumerable`) | Lazy (`IAsyncEnumerable`) |
| **MTP support** | ✅ Fuld — Connect, enumerate, CopyTo, sidecar | ✅ Fuld — Gatekeeper, session, channel pipeline | ❌ Filesystem only | ❌ `NotSupportedException` |
| **DI** | `TryAdd` singleton/transient | `TryAdd` singleton/transient | `TryAdd` singleton/transient | `TryAdd` singleton/transient |
| **Target** | `net8.0` (exe) | `net8.0-windows7.0` | — | (unknown) |
| **Resume/persistence** | Newtonsoft JSON per-source | JSON session fil | Ingen | JSON i `.bmtp3/` |
| **Pipeline model** | Monolithic inline steps | `Channel<T>` bounded pipeline stages | Sequential, phases over liste | Sequential inline `foreach` |
| **Formål (ifølge AI instructions)** | Funktionel reference — viser hvad programmet skal kunne | Idébank — selektiv genbrug af patterns | Advarsel — halve implementationer duer ikke | Nyt produktionsgrundlag — clean, stabil, single-threaded first |

---

## 2. Traversal Architecture — Bottom to Top

### 2a. Low-Level Traversal (`IAsyncEnumerable<TEntry>`)

| Lag | Core | Core2 | Core4 |
|-----|------|-------|-------|
| **Interface** | Ingen — `MediaDirectoryInfo` / `DirectoryInfo` direkte | `ITraversalScanner<TEntry>` | `ISourceTraversal : IAsyncDisposable` |
| **Method** | `ReadAllFiles(MediaDirectoryInfo, ...)` → `IList` | `ScanAsync(rootPath, recursive, progress, ct)` → `IAsyncEnumerable<TEntry>` | `TraverseAsync(request, progress, ct)` → `IAsyncEnumerable<SourceTraversalItem>` |
| **Entry type** | `MediaFileInfo` / `FileInfo` | `MediaFileInfo` / `FileInfo` | `SourceTraversalItem` (record) |
| **Returns** | `List<T>` — fully materialized | `IAsyncEnumerable<TEntry>` — lazy | `IAsyncEnumerable<SourceTraversalItem>` — lazy |
| **Constructor deps** | N/A (inline) | `MediaDevice`, `IMtpGatekeeper` | None (for FileSystem) |
| **Disposable** | Nej | Nej | ✅ `IAsyncDisposable` |
| **SafeGet pattern** | Ingen (kaster ved access-denied) | `SafeEnumerateFiles`/`SafeEnumerateDirectories` (try/catch → empty) | `SafeGetFiles`/`SafeGetDirectories` (try/catch → empty) |
| **Progress** | Spectre `Progress` + counter callbacks | `IProgress<TraversalProgress>` | `IProgress<SourceTraversalProgress>` |
| **Cancellation** | `CancellationToken` | `CancellationToken` | `CancellationToken` |
| **Filtering** | 🔴 I traversal — `FilePattern`/`FilePatternIfExist` templates | 🟢 I `BackupScanner` via `GlobMatcher.IsIncluded` | 🔴 Planlagt til scanner (endnu ikke implementeret i `FileSystemTraversal`) |

### 2b. Traversal Implementations

#### FileSystem (local disk)

| Aspekt | Core | Core2 | Core4 |
|--------|------|-------|-------|
| **Klasse** | Ingen — inline i `BackupHandler.ReadAllFiles(DirectoryInfo)` | `FileSystemScanner : ITraversalScanner<FileInfo>` | `FileSystemTraversal : ISourceTraversal` |
| **Metode** | `DirectoryInfo.EnumerateFiles()` + `EnumerateDirectories()` (recursive) | Same, med `SafeGetFiles`/`SafeGetDirectories` wrappers | Same, med `SafeGetFiles`/`SafeGetDirectories` wrappers |
| **Materialisering** | `filesFound.Add(file)` → `List<FileInfo>` | `yield return file` via `IAsyncEnumerable` | `yield return` via `IAsyncEnumerable` |
| **Content wrapper** | `FileInfo` direkte | `FileContent(fileInfo)` | `FileContent(fileInfo)` |
| **Timestamps** | `FileInfo.CreationTime`, `LastWriteTime` | `SafeGetDate` via try/catch | `SafeGetDate` via try/catch |
| **Yield/return** | `yield return` | `yield return` | `yield return` |
| **Dispose** | N/A | N/A | `ValueTask.CompletedTask` (no-op) |

#### MediaDevice (MTP)

| Aspekt | Core | Core2 | Core4 |
|--------|------|-------|-------|
| **Klasse** | Ingen — inline i `BackupHandler.ReadAllFiles(MediaDirectoryInfo)` | `MediaDeviceScanner : ITraversalScanner<MediaFileInfo>` | ❌ `NotSupportedException` i `SourceTraversalFactory` |
| **Library** | `MediaDevices.dll` via COM | `MediaDevices.dll` via COM | ❌ Ikke refereret |
| **Metode** | `MediaDirectoryInfo.EnumerateFiles()` | `MediaDeviceScanner.ScanAsync` (wrapper med gatekeeper) | ❌ |
| **Gatekeeper** | ❌ Ingen — single-threaded af natur | ✅ `IMtpGatekeeper` (SemaphoreSlim) | ❌ |
| **Materialisering** | `filesFound.Add(file)` → `List<MediaFileInfo>` | Materialiserer `List` inde i gatekeeper lock, itererer udenfor | ❌ |
| **Content wrapper** | `MediaFileInfo.CopyTo()` direkte | `MediaFileContent(mediaFileInfo, gatekeeper)` | ❌ |
| **Timestamps** | `MediaFileInfo.DateAuthored`, `CreationTime`, `LastWriteTime` | Same | ❌ |

### 2c. High-Level Scanner (produces BackupItems)

| Aspekt | Core | Core2 | Core4 |
|--------|------|-------|-------|
| **Interface** | Ingen — inline | `IBackupScanner` | `IBackupScanner` |
| **Klasse** | `BackupHandler` inline | `BackupScanner : IBackupScanner, IMtpCapableScanner` | `BackupScanner : IBackupScanner` |
| **Input** | `MediaDevice` + `DeviceSourceConfig` | `BackupPlan` | `ISourceTraversal` + `BackupScanRequest` |
| **Output** | `BackupRecordInfo` (custom model) | `IAsyncEnumerable<IBackupItem>` | `IAsyncEnumerable<BackupItem>` |
| **Filtering** | `FilePattern`/`FilePatternIfExist` templates | `GlobMatcher.IsIncluded(includePatterns, excludePatterns)` | Planlagt (request har patterns, men `FileSystemTraversal` ignorerer dem) |
| **Metadata** | `PersistentUniqueId`, `Path`, `Size`, timestamps | `MetadataKey` enum (53 keys) + `BackupMetadata` ConcurrentDictionary | `ItemMetadata` (enkle properties) |
| **MTP device session** | `using(device.Connect())` | `IMtpCapableScanner.OpenSession(plan)` → `IMtpDeviceSession` | ❌ |
| **Content creation** | `MediaFileInfo.CopyTo` temp → sidecar → move | `MediaFileContent(mediaFileInfo, gatekeeper)` | `FileContent(fileInfo)` direkte |

### 2d. Core — Traversal Details (original)

**`BackupHandler.ReadAllFiles(MediaDirectoryInfo, ...)`** (linje ~1280):
```csharp
private List<MediaFileInfo> ReadAllFiles(MediaDirectoryInfo fromDirectoryInfo,
    Action<int> fileIncrementCallback, Action<int> dirIncrementCallback,
    CancellationToken cancellationToken)
{
    List<MediaFileInfo> filesFound = new();
    foreach (var file in fromDirectoryInfo.EnumerateFiles()) {
        filesFound.Add(file);
        fileIncrementCallback(1);
    }
    foreach (MediaDirectoryInfo directory in fromDirectoryInfo.EnumerateDirectories()) {
        dirIncrementCallback(1);
        foreach (var file in ReadAllFiles(directory, fileIncrementCallback, dirIncrementCallback, cancellationToken))
            filesFound.Add(file);
    }
    return filesFound;
}
```

**Key characteristics:**
- **Fully materialized** — alle filer i en `List<MediaFileInfo>` før backup starter
- **Progress via callbacks** — Spectre `Progress` + counter delegates
- **Efter enumeration:** bygger `FrozenDictionary<string, MediaFileInfo>` keyed by `PersistentUniqueId`
- **Resume matching:** `BackupRecordInfo.PersistentUniqueId == MediaFileInfo.PersistentUniqueId`
- **Drive version:** Same mønster med `DirectoryInfo` → `List<FileInfo>` keyed by `FullName`

### 2e. Core2 — Traversal Details

**`MediaDeviceScanner.ScanAsync`** (gatekeeper-wrapped enumeration):
```csharp
private async IAsyncEnumerable<MediaFileInfo> ScanInternalAsync(MediaDirectoryInfo dir,
    bool recursive, Action<MediaFileInfo> onFile, Action<MediaDirectoryInfo> onDir,
    [EnumeratorCancellation] CancellationToken ct)
{
    // Files: enumerate under gatekeeper lock, materialize to List, iterate outside
    List<MediaFileInfo> files = await _gatekeeper.ExecuteAsync(
        () => Task.FromResult(SafeEnumerateFiles(dir).ToList()), ct);
    foreach (var file in files) {
        onFile(file);
        yield return file;
    }

    // Directories: same pattern
    if (recursive) {
        List<MediaDirectoryInfo> dirs = await _gatekeeper.ExecuteAsync(
            () => Task.FromResult(SafeEnumerateDirectories(dir).ToList()), ct);
        foreach (var subDir in dirs) {
            onDir(subDir);
            await foreach (var file in ScanInternalAsync(subDir, recursive, onFile, onDir, ct))
                yield return file;
        }
    }
}
```

**Key characteristics:**
- **Gatekeeper lock per directory** — `ExecuteAsync` acquire → materialize → release
- **Materialization INSIDE lock** — holder lock kort, itererer udenfor
- **Lazy streaming** — `IAsyncEnumerable` yield per file
- **Two scanning layers:** Low-level `ITraversalScanner<TEntry>` + high-level `IBackupScanner`

### 2f. Core4 — Traversal Details

**`FileSystemTraversal.TraverseAsync`** (nuværende implementering):
```csharp
public async IAsyncEnumerable<SourceTraversalItem> TraverseAsync(
    SourceTraversalRequest request,
    IProgress<SourceTraversalProgress>? progress = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var dirInfo = new DirectoryInfo(request.SourcePath);
    if (!dirInfo.Exists) yield break;

    await foreach (var file in TraverseDirectoryAsync(dirInfo, request.Recursive, progress, cancellationToken))
        yield return file;
}
```

**Key characteristics:**
- **No gatekeeper** — filesystem only, ingen MTP
- **Safe wrappers** — `SafeGetFiles`/`SafeGetDirectories` (try/catch → empty)
- **Lazy streaming** — `IAsyncEnumerable`
- **Disposable** — `IAsyncDisposable` (no-op for filesystem, vital for future MTP)
- **Include/Exclude patterns** — `SourceTraversalRequest` har dem, men `FileSystemTraversal` ignorerer dem

---

## 3. SourceTraversalItem → BackupItem Mapping

### Core4 mapping (i `BackupScanner.ScanAsync`):

| SourceTraversalItem | → | BackupItem |
|---------------------|---|------------|
| `Id` | | `Id = Path.GetRelativePath(request.SourcePath, sourceItem.SourcePath)` |
| `SourcePath` | | `SourcePath = sourceItem.SourcePath` |
| (computed) | | `RelativePath = Path.GetRelativePath(request.SourcePath, sourceItem.SourcePath)` |
| (computed) | | `FileName = Path.GetFileName(sourceItem.SourcePath)` |
| `Content` | | **`Content = sourceItem.Content`** (same object, no copy) |
| `DateCreated` | | `DateCreated = sourceItem.DateCreated` |
| `DateModified` | | `DateModified = sourceItem.DateModified` |
| `DateAuthored` | | `DateAuthored = sourceItem.DateAuthored` |
| `DateAccessed` | | `DateAccessed = sourceItem.DateAccessed` |

### Core2 mapping (i `BackupScanner.ScanMediaDeviceAsync`):

| MediaFileInfo | → | IBackupItem |
|---------------|---|-------------|
| `PersistentUniqueId` | | `MetadataKey.SourceId` |
| `FullName` | | `MetadataKey.SourceFullPath` + `SourceRelativePath` |
| `Name` | | `MetadataKey.SourceFileName` |
| `Length` | | `MetadataKey.Length` + `Content.Length` |
| (computed) | | `DeviceFileUrl` = `mtp://<deviceId>/<escaped-path>` |
| `DateAuthored` | | `MetadataKey.RawMtpAuthoredDate` |
| `DateAuthored` | | `Metadata.AuthoredDateTime` |
| `CreationTime` | | `Metadata.CreatedDateTime` |
| `LastWriteTime` | | `Metadata.ModifiedDateTime` |

### Core mapping (inline i `BackupHandler`):

| MediaFileInfo | → | BackupRecordInfo |
|---------------|---|------------------|
| `PersistentUniqueId` | | `PersistentUniqueId` |
| `FullName` | | `Path` |
| `Name` | | `Name` |
| `Length` | | `Size` |
| `CreationTime` | | `DateCreated` |
| `LastWriteTime` | | `DateModified` |
| `DateAuthored` | | `DateAuthored` |

---

## 4. Content Abstraction

### 4a. Interface Comparison

| Aspekt | Core | Core2 | Core4 |
|--------|------|-------|-------|
| **Interface** | Ingen — bruger `MediaFileInfo` / `FileInfo` direkte | `IContent : IDisposable` | `IContent : IDisposable, IAsyncDisposable` |
| **OpenRead** | `MediaFileInfo.CopyTo()` / `FileInfo.OpenRead()` | `Stream OpenRead()` + `Task<Stream> OpenReadStreamAsync(CancellationToken)` | `Stream OpenRead()` + `Task<Stream> OpenReadStreamAsync(CancellationToken)` |
| **Length** | `MediaFileInfo.Length` / `FileInfo.Length` | `ulong Length { get; }` | `ulong Length { get; }` |
| **MoveTo** | `FileInfo.MoveTo()` | `IMoveableContent : IContent` — `IContent MoveTo(string)` | `IMoveableContent : IContent, IFileInfoSource` — `IContent MoveTo(string, bool overwrite)` |
| **FileInfo access** | N/A (direkte reference) | Ingen | `IFileInfoSource.TryGetFileInfo(out FileInfo)` |
| **Stream ownership** | Caller disposer `FileStream` | Caller disposer returned `Stream` | Caller disposer returned `Stream` |

### 4b. FileContent (local file)

| Aspekt | Core | Core2 | Core4 |
|--------|------|-------|-------|
| **OpenRead** | `FileInfo.OpenRead()` (8KB buffer) | `new FileStream(..., FileShare.Read)` | `new FileStream(..., FileShare.Read, bufferSize: 128*1024)` |
| **Async variant** | Ingen | `OpenReadStreamAsync` | `OpenReadStreamAsync` med `FileOptions.Asynchronous | FileOptions.SequentialScan` |
| **MoveTo** | `FileInfo.MoveTo(dest, overwrite)` | `FileInfo.MoveTo(dest, true)` / ny `FileContent` | `FileInfo.MoveTo(dest, false)` — 🔴 `overwrite` parameter ignoreres! |
| **Dispose pattern** | `using` på `FileStream` | `IDisposable` | `IDisposable + IAsyncDisposable` med `Volatile` flag |

### 4c. MediaFileContent (MTP)

| Aspekt | Core | Core2 | Core4 |
|--------|------|-------|-------|
| **Eksisterer?** | Nei — bruger `MediaFileInfo.CopyTo()` direkte | ✅ `MediaFileContent : IContent` | ❌ Mangler |
| **OpenRead** | `MediaFileInfo.CopyTo(tempPath, progressReporter)` | `AcquireAsync()` → `MediaFileInfo.OpenRead()` → `GatekeptStream(raw, lease)` | ❌ |
| **Gatekeeper** | ❌ | ✅ `IMtpGatekeeper.AcquireAsync()` — lease holdes for hele stream lifetime | ❌ |
| **Stream type** | `MediaFileInfo.CopyTo` med `IProgress<FileProgressReport>` | `GatekeptStream : Stream` (wrapper) | ❌ |
| **Progress** | `IProgress<FileProgressReport>` under CopyTo | `Stream.CopyToAsync` — progress via custom | ❌ |

### 4d. GatekeptStream (Core2 only)

```csharp
public sealed class GatekeptStream : Stream
{
    private readonly Stream _inner;       // Raw MTP stream from MediaFileInfo.OpenRead()
    private readonly IDisposable _lease;  // Semaphore lease from MtpGatekeeper.AcquireAsync()

    protected override void Dispose(bool disposing)
    {
        _inner.Dispose();   // Close MTP stream
        _lease.Dispose();   // Release semaphore — now other threads can use MTP
    }
}
```

**Design rationale:** Gatekeeper semaphore acquires ONCE per file, held for entire `CopyToAsync`. Eliminates per-chunk acquire/release (var 1280+ ops per 100MB). `ContentBufferingPipelineStage` kører med `parallelism = 1`, så ingen contention.

---

## 5. MTP Stack Comparison

### 5a. Complete MTP Component Map

| Component | Core | Core2 | Core4 |
|-----------|------|-------|-------|
| **Library reference** | `..\libs\MediaDevices.dll` | `..\libs\MediaDevices.dll` | ❌ Ikke refereret |
| **Device discovery** | `MediaDevice.GetDevices()` i `MediaDeviceHandler` | `MediaDevice.GetDevices()` i `BackupScanner.OpenSession()` | ❌ |
| **Device connection** | `using(device.Connect())` | `device.Connect()` i `OpenSession()` | ❌ |
| **Device disconnection** | `Dispose()` af `using` block | `MtpDeviceSession.Dispose()` → `device.Disconnect()` | ❌ |
| **Thread safety** | ❌ Ingen — implicit single-thread | ✅ `MtpGatekeeper` (SemaphoreSlim(1,1)) | ❌ |
| **Directory enumeration** | `MediaDirectoryInfo.EnumerateFiles()` direkte | `MediaDeviceScanner` wrapper med gatekeeper | ❌ |
| **File download** | `MediaFileInfo.CopyTo(dest, progress)` | `MediaFileContent.OpenRead()` → `CopyToAsync()` | ❌ |
| **Session lifecycle** | `using(device.Connect())` scope = hele backup | `IMtpDeviceSession` scope = indtil buffering færdig | ❌ |
| **Resilience** | COMException handler (0x800710D2, 0x8007001E) | `ExponentialBackoffRetryPolicy` (3 attempts) | ❌ |

### 5b. Core2 Gatekeeper Architecture

```
IMtpGatekeeper
  ├── ExecuteAsync<T>(action, ct)    — Short ops: acquire semaphore, run, release
  │     Used for: EnumerateFiles, EnumerateDirectories, metadata reads
  │     Timeout: 60s (configurable via MtpOperationTimeoutMs)
  │
  └── AcquireAsync(ct) → IDisposable — Long ops: acquire and hold
        Used for: Stream.Read (file download)
        Returns SemaphoreLease (inner class, thread-safe via Interlocked.Exchange)

MtpGatekeeper
  └── SemaphoreSlim(1, 1)           — Single-threaded access til MTP device
  └── SemaphoreLease                 — IDisposable wrapper, double-dispose safe
```

### 5c. Core2 MTP Device Session Lifecycle

```
BackupEngine.RunAsync()
  │
  ├── 1. OpenSession(plan)
  │       device.Connect()
  │       _activeDevice = device
  │       return MtpDeviceSession(device)
  │
  ├── 2. ScanAsync() — producer task
  │       MediaDeviceScanner.ScanAsync()
  │         └── Per directory: gatekeeper.ExecuteAsync(EnumerateFiles)
  │         └── Creates MediaFileContent per file
  │
  ├── 3. ContentBufferingPipelineStage — parallelism = 1
  │       Per item:
  │         └── item.Content.OpenRead() → GatekeptStream
  │               ├── gatekeeper.AcquireAsync()  ← LEASE ACQUIRED
  │               ├── MediaFileInfo.OpenRead()
  │               └── Return GatekeptStream(raw, lease)
  │         └── CopyToAsync(destStream)
  │               └── Reads under lease, no per-read locking
  │         └── sourceStream.Dispose()
  │               └── GatekeptStream.Dispose()
  │                     ├── _inner.Dispose()     ← MTP stream closed
  │                     └── _lease.Dispose()     ← Semaphore released
  │         └── item.ReplaceContent(FileContent) ← Now local temp file
  │
  ├── 4. bufferingTask.ContinueWith(_ => mtpSession.Dispose())
  │       └── Device disconnects HERE — NOT after full pipeline
  │
  └── 5. Remaining stages (hashing, transfer, sidecar...) operate on FileContent
```

### 5d. Core MTP Lifecycle (simplistic)

```
BackupMaster.StartBackup()
  └── For each DeviceBackupJob:
        using(device.Connect())                    ← Scope = hele backup
          backupHandler.PerformBackup(device, config, startTime)
            ├── device.EnsureConnected()
            ├── GetAllMediaFiles(dirInfo)          ← Enumerates ALL files
            ├── Build FrozenDictionary
            ├── Load/Create BackupRecordDataStore
            ├── For each record:
            │     ├── MediaFileInfo.CopyTo(temp)   ← MTP download (inline)
            │     ├── MetadataExtractor (EXIF)
            │     ├── CreateSideCarFileInfo (hashes)
            │     └── File.Move (temp → dest)
            └── backupDataStore.SaveDataStore()    ← JSON persistence
          └── device.Disconnect()                   ← implicit via using
```

---

## 6. Pipeline Flow Comparison

### 6a. Core (BackupHandlerForDevice)

```
Pre-flight:
  1. Connect device (using block)
  2. Enumerate ALL files → List<MediaFileInfo> + FrozenDictionary
  3. Load/create BackupRecordDataStore (JSON progress)
  4. Create temp directory

Per-item (inline, sorted by path):
  5. Skip if already saved
  6. DownloadToTempFile (MediaFileInfo.CopyTo)
  7. MetadataExtractorFileInfo.GetCreatedMediaFileDateTime()
  8. FindEarliestValidDateTime()
  9. Template path resolution (file pattern)
  10. Collision resolution (binary compare → skip or rename)
  11. CreateSideCarFileInfo (7 hashes + device info)
  12. File.MoveTo (temp → dest)
  13. File.MoveTo (sidecar temp → dest)

Post-run:
  14. SaveDataStore() (finally block)
  15. DeleteEmptyDirectoriesRecursive()
```

### 6b. Core2 (BackupEngine — parallel)

```
Pre-flight (sequential):
  1. IJobValidator.ValidateAsync(plan)
  2. Disk space check (100 MB minimum)
  3. Source/output access probe
  4. Initialize resume session
  5. Open MTP session (if applicable)

Pipeline (9 bounded Channel<T> stages, each with N workers):
  [ScanChannel]   Producer: IBackupScanner.ScanAsync()
  → [Buffering]   Staging: download MTP/file → temp (parallelism=1)
  → [Metadata]    EXIF/metadata extraction
  → [Timestamp]   Timestamp waterfall resolution
  → [Hash]        Multi-hash computation
  → [Transfer]    Path resolution + collision + move + verify
  → [Inspector]   Destination inspection (sidecar hash read-back)
  → [Sidecar]     Sidecar generation (JSON or INI)
  → [Completion]  Persist + finalize

Post-run:
  6. Dispose MTP session (after buffering completes)
  7. Compile BackupJobResult
```

### 6c. Core4 (BackupEngine — sequential)

```
Pre-flight:
  1. BackupPlanValidator.Validate(plan)
  2. Initialize state (BackupMemoryRecordRepository, BackupSessionKey, BackupProgress)
  3. Prepare destination (Directory.CreateDirectory + .bmtp3)
  4. Open source traversal (await using ISourceTraversal)
  5. Scan source (foreach item in _scanner.ScanAsync(traversal, ...))
  6. Apply resume (_sessionState.ApplyResumeAsync)
  7. Filter pending records
  8. Prepare temp directory

Per-item (inline foreach):
  9.  DownloadAsync (stream content → temp file)
  10. ResolveEarliestAsync (EXIF/XMP/IPTC/GPS/QuickTime metadata)
  11. ComputeHashesAsync (single-pass multi-hash)
  12. CollisionHelpers.ResolveTargetPath (Skip/Error/Overwrite/Rename)
  13. Directory.CreateDirectory
  14. MoveTo (temp → destination)
  15. SidecarService.WriteAsync (INI)
  16. Record.Status = Succeeded

Post-run:
  17. SaveAsync (finally block)
  18. CleanupSessionTempDirectory (finally block, best-effort)
  19. Final progress report (CurrentPhase = Completed)
  20. Build BackupResult
  ═══ await using traversal.DisposeAsync() ═══
```

### 6d. Core4 Content Provider Replacement Chain

```
Step 5:  FileContent (from traversal, wraps original file)
            │
Step 9b:  DownloadService.ReplaceContentProvider
            │
            ▼
         MoveableFileContent (wraps temp file)
            │
Step 14:  MoveTo (disposes current, returns new)
            │
            ▼
         MoveableFileContent (wraps destination file)
```

---

## 7. Hash Computation

| Aspekt | Core | Core2 | Core4 |
|--------|------|-------|-------|
| **Klasse** | `HashCalculator` | `IItemHasher` / `IHashGenerator` | `HashService` / `IHashGenerator` |
| **Streaming** | ✅ Single pass (TransformBlock + Blake3.Hasher.Update) | ✅ Single pass | ✅ Single pass |
| **Algoritmer** | 9: SHA3-512/256 FIPS202, SHA3-512/256 Keccak, SHA2-512/256, MD5, BLAKE3-256/512 | Konfigurerbar via `HashTypes` | Konfigurerbar via `HashAlgorithmType` (samme 9) |
| **Buffer auto-tune** | Ja: 8KB (<256KB), 32KB (<4MB), 64KB (<32MB), 256KB (<256MB), 512KB (>=256MB) | — | — |
| **Library** | SharpHash, BouncyCastle, System.Security.Cryptography, Blake3 | — | StreamHashGenerator |
| **Output format** | Hex lowercase | Hex lowercase | Hex lowercase |
| **Input** | File path (`string filePath`) | `IContent` (stream) | `IContent` (stream) |
| **Hash wrapper** | `SharpHashSHA3_512 : HashAlgorithm` (extends SHA512) | — | — |

**Note:** Original Core kunne beregne flere hash-algoritmer end den nødvendigvis skrev til sidecar. Derfor er der ikke nødvendigvis modstrid mellem "9 algoritmer" i hash-komponenten og "7 hashes" i sidecar-output. Sidecar parity bør vurderes ud fra det ønskede output-format, ikke kun ud fra HashCalculatorens tekniske kapabilitet.

---

## 8. Sidecar Generation

| Aspekt | Core | Core2 | Core4 |
|--------|------|-------|-------|
| **Format** | INI altid | INI eller JSON (konfigurerbar) | INI (JSON kaster `NotImplementedException`) |
| **Writer** | `IniSideCarWriter` | `ISidecarGeneratorFactory` → `IniSidecarGenerator` / `JsonSidecarGenerator` | `SidecarService` |
| **Sections** | `[Settings]`, `[BackupInfo]`, `[FileHash]`, `[DeviceFileDetails]`/`[DriveFileDetails]`, `[DeviceDetails]`/`[DriveDetails]`, `[PathMapping]` | Tilsvarende | `[BackupInfo]`, `[FileHashes]`, `[Timestamps]` |
| **Hashes i sidecar** | 7: SHA3-512 Keccak + FIPS202, SHA2-512/256, MD5, BLAKE3-256/512 | Konfigurerbar | Alle `ComputedHashes` |
| **Device info** | `DeviceId`, `Description`, `FriendlyName`, `Manufacturer`, `Model`, `SerialNumber` | — | ❌ Ikke relevant (ingen MTP) |
| **Drive info** | `DriveId`, `Description`, `VolumeLabel` | — | — |
| **Path mapping** | `OriginalRelativeDirectoryPath` → `SanitizedRelativeDirectoryPath` | — | — |
| **Backup timestamp** | `BackupDateTime` | — | `StartTime` |

---

## 9. Progress Reporting

| Aspekt | Core | Core2 | Core4 |
|--------|------|-------|-------|
| **Phase enum** | Ingen (implicit via tekst) | `BackupPhase` (`Starting`, `Traversing`, `Transferring`, `Completed`, `Cancelled`) | `BackupProgressPhase` (`Starting`, `Scanning`, `Transferring`, `Completed`, `Cancelled`, `Failed`) |
| **Per-file phase** | Ingen | `FilePhase` (`Staging`, `Metadata`, `Hashing`, `Planning`, `Transferring`) | `BackupProgressItemPhase` (`Transferring`, `Hashing`, `ProcessingMetadata`, `Finalizing`) |
| **Reporting** | Spectre `Progress` + `ProgressTask` | `IProgress<BackupProgress>` hver 250ms | `IProgress<BackupProgress>` callback |
| **Counts** | `filesFound`, `filesProcessed` | `FilesDiscovered`, `FilesSucceeded`, `FilesFailed`, `FilesSkipped`, `BytesTotal`, `BytesProcessed` | `FilesDiscovered`, `FilesSucceeded`, `FilesSkipped`, `FilesFailed` |
| **Active files** | Spectre progress bars | `IReadOnlyList<FileProgress>` (bytes per fil) | `IReadOnlyList<BackupProgressItem>` |
| **Thread safety** | `lock` i `FileAndDirectoryCounter` | `ConcurrentDictionary` + `Interlocked` i `ProgressTracker` | Ingen (sekventiel) |

---

## 10. Session State / Persistence

| Aspekt | Core | Core2 | Core4 |
|--------|------|-------|-------|
| **Format** | JSON via Newtonsoft | JSON | JSON via System.Text.Json |
| **Filnavn** | `Progress_{Title}_{Name}.json` | `session.json` i backup root | `{destination}\.bmtp3\{sessionId}.json` |
| **Per-item record** | `BackupRecordInfo` (`IsSaved` bool) | `BackupResumeRecord` | `BackupSummaryItem` (`Status` enum) |
| **Resume strategy** | Ingen — kaster hvis fillisten ændrede sig | `Abort` / `Continue` / `Restart` | `Abort` / `Continue` / `Restart` |
| **Save timing** | `SaveDataStore()` i `finally` | Hver 10. item + ved shutdown | `SaveAsync()` i `finally` block |
| **Write atomicity** | Direkte serialisering | — | Temp-file + replace (`BackupJsonSummaryStore`) |

---

## 11. DI Registration Comparison

### Core DI (StartUp.cs)
```
Singleton:  IConfiguration
Singleton:  BackupSettingsReader
Singleton:  IBackupHandler → BackupHandler
Singleton:  IStorageHandler → MediaDeviceHandler
Singleton:  IDriveHandler → DriveHandler
Singleton:  BackupMaster
Singleton:  FileComparer → ReadFileInChunksAndCompareSequenceEqual
Singleton:  HashCalculator
Singleton:  CancellationTokenSource
Singleton:  IAnsiConsole
Transient:  BackupHelper
Transient:  IMediaDeviceService → MediaDeviceServiceProd
Transient:  IPrintHandler → PrintHandler
Transient:  BackupExceptionHandlerService
```

### Core2 DI (ServiceCollectionExtensions.cs)
```
Singleton:  IMediaDeviceScannerFactory → MediaDeviceScannerFactory
Singleton:  IMtpGatekeeper → MtpGatekeeper(timeout)
Singleton:  ITraversalScanner<FileInfo> → FileSystemScanner
Singleton:  ITraversalScanner<MediaFileInfo> → NoopMediaDeviceScanner  (fallback)
Singleton:  IBackupScanner → BackupScanner
Singleton:  IBackupRepository → FileBackupRepository
Singleton:  IDestinationInspector → DestinationInspector
Singleton:  ISidecarGeneratorFactory
Singleton:  BackupEngineOptions (configured)
Transient:  IStagingDownloader → StagingDownloader
Transient:  IHashGenerator → StreamHashGenerator
Transient:  IFileTransfer → LocalFileTransfer
Transient:  IPathGenerator → PathGenerator
Transient:  ICollisionResolver → CollisionResolver
Transient:  IMetadataReader → MetadataReader
Transient:  ITimestampWaterfall → DefaultTimestampWaterfall
Transient:  IItemHasher → ItemHasher
Transient:  ISidecarGenerator → JsonSidecarGenerator / IniSidecarGenerator
Transient:  IRetryPolicy → ExponentialBackoffRetryPolicy
Transient:  IJobValidator → JobValidator
Transient:  IBackupEngine → BackupEngine
```

### Core4 DI (ServiceCollectionExtensions.cs)
```
Singleton:  IHashGenerator → StreamHashGenerator
Singleton:  IHashService → HashService
Singleton:  IDiskSpaceValidator → DiskSpaceValidator
Singleton:  IDownloadService → DownloadService
Singleton:  IEarliestTimestampResolutionService → EarliestTimestampResolutionService
Singleton:  ISidecarService → SidecarService
Singleton:  IBackupRunnerFactory → BackupRunnerFactory
Singleton:  ISourceTraversalFactory → SourceTraversalFactory
Singleton:  IBackupScanner → BackupScanner
Singleton:  IBackupEngine → BackupEngine
Transient:  IBackupRunner → BackupRunner
```

**Not registered in DI (created manually in BackupEngine):**
- `ISessionStateService` + `SessionStateService`
- `ISummaryStore` → `BackupJsonSummaryStore` (created with destination + sessionId)
- `IBackupRecordRepository` → `BackupMemoryRecordRepository`

---

## 12. Core4 Current Implementation (Hvad vi har bygget)

### 12a. Færdige komponenter

| Komponent | Fil | Status |
|-----------|-----|--------|
| `BackupEngine` — orchestrator med try/finally, manual store/session creation | `Engine/BackupEngine.cs` | ✅ |
| `TempDirectoryHelper` — cleanup returnerer `bool`, inner try/catch | `Engine/TempDirectoryHelper.cs` | ✅ |
| `ISessionStateService` — SaveAsync/DeleteAsync uden CancellationToken | `Engine/Session/ISessionStateService.cs` | ✅ |
| `SessionStateService` — bruger ISummaryStore, manuelt oprettet | `Engine/Session/SessionStateService.cs` | ✅ |
| `ISummaryStore` — SaveAsync/LoadAsync/DeleteAsync uden CancellationToken | `State/ISummaryStore.cs` | ✅ |
| `BackupJsonSummaryStore` — JSON i `.bmtp3\{sessionId}.json`, temp-file + replace | `State/BackupJsonSummaryStore.cs` | ✅ |
| `BackupMemorySummaryStore` — in-memory fallback | `State/BackupMemorySummaryStore.cs` | ✅ |
| `FileSystemTraversal` — recursive walk, SafeGetFiles, SafeGetDirectories, SafeGetDate | `Traversal/FileSystemTraversal.cs` | ✅ |
| `SourceTraversalFactory` — dispatcher på BackupSourceType | `Traversal/SourceTraversalFactory.cs` | ✅ |
| `BackupScanner` — mapper SourceTraversalItem → BackupItem | `Scanner/BackupScanner.cs` | ✅ |
| `BackupProgressPhase.Completed` — final phase | `Api/Models/Enums/BackupProgressPhase.cs` | ✅ |
| Final progress report — CurrentPhase = Completed, counts fra allRecords | `Engine/BackupEngine.cs` | ✅ |
| ServiceCollectionExtensions — real DI wiring, ingen stubs | `DependencyInjection/ServiceCollectionExtensions.cs` | ✅ |
| Unused usings fjernet (BMTP3.Core4.Engine.Session, BMTP3.Core4.State) | `DependencyInjection/ServiceCollectionExtensions.cs` | ✅ |
| Unused `itemLength` variabel fjernet | `Engine/BackupEngine.cs` | ✅ |

### 12b. Prioriteret rækkefølge (fra AI Instructions)

Det **umiddelbare mål** er en stabil sekventiel filesystem backup vertical slice — ikke MTP, ikke parallelisme.

```text
1. 🔴 Fix BackupPlanValidator — så en valid minimal plan kan nå engine
2. ✅ Ensure filesystem traversal + scanner virker end-to-end
3. ✅ Ensure session state gemmes korrekt (post-run persistence)
4. ✅ Ensure persistence overlever proces-genstart (BackupJsonSummaryStore)
5. ❌ Ensure destination timestamps korrigeres (File.SetCreationTime/SetLastWriteTime)
6. ❌ Add binary compare til collision handling (undgå falske dubletter)
7. ❌ Add post-write verification
8. 🟡 Forbedre sidecar parity med Original Core
9. 🔵 Først DA overvej MTP support
10. 🔵 Først MEGET senere overvej parallelisme, hvis nødvendigt
```

### 12c. Status på hvert item

| # | Item | Priority | Status | Dependency |
|---|------|----------|--------|------------|
| 1 | BackupPlanValidator contradictory tier-gating | 🔴 **Kritisk** | Blokeret (user undid fix) | Ingen |
| 2 | Filesystem traversal + scanner | ✅ Færdig | Virker isoleret; fuldt engine-flow er stadig blokeret af `BackupPlanValidator` | — |
| 3 | Session state gemmes efter loop | ✅ Færdig | I `finally` block | — |
| 4 | Persistence overlever genstart | ✅ Færdig | `BackupJsonSummaryStore` | — |
| 5 | Timestamp correction på destinationsfil | 🔴 **Vigtig** | Mangler | — |
| 6 | Binary compare i collision resolution | 🔴 **Vigtig** | Mangler | — |
| 7 | Post-write verification | 🟡 Mellem | Mangler | — |
| 8 | Sidecar parity med Original Core | 🟡 Mellem | Mangler device details, path mapping | — |
| 9 | MTP/MediaDevice traversal | 🔵 **Fremtid** | Mangler | MediaDevices.dll + porting |
| 10 | Parallelisme / runners | 🔵 **Fremtid** | Implementeret men ikke wired | Først når sekventiel er stabil |
| — | Source/output access probe | 🟡 Mellem | Mangler | — |
| — | Cleanup af tomme temp-mapper | 🟡 Mellem | Mangler | — |
| — | `EarliestTimestampResolutionServiceAnother.cs` | 🟢 Lav | Død kode, skal slettes | — |
| — | JSON sidecar | 🟢 Lav | `NotImplementedException` | — |

---

## 13. MTP Support — Fremtidigt Arbejde (Ikke Nu)

> **⚠️ Vigtigt:** MTP er #9 på prioriteringslisten. Først når filesystem backup virker end-to-end, inklusiv timestamp correction, binary compare, post-write verification, og sidecar parity.
>
> Core4 må ikke trækkes ind i Core2-niveau kompleksitet for tidligt. Gatekeeper og session patterns er værdifulde, men de skal porte's til Core4's enklere sekventielle arkitektur — ikke blindt kopieres med channels og parallelisme.

### 13a. Dependency

Tilføj `MediaDevices.dll` reference til `BMTP3.Core4.csproj`:
```xml
<Reference Include="MediaDevices">
  <HintPath>..\libs\MediaDevices.dll</HintPath>
</Reference>
```
DLL findes på: `BMTP3_CS\libs\MediaDevices.dll` (v1.12.3.73-beta11)

### 13b. Nye klasser at porte fra Core2

| # | Klasse | Core2 fil | Purpose |
|---|--------|-----------|---------|
| 1 | `IMtpGatekeeper` + `MtpGatekeeper` | `Infrastructure/Traversal/IMtpGatekeeper.cs`, `MtpGatekeeper.cs` | SemaphoreSlim(1,1) for single-threaded MTP access |
| 2 | `GatekeptStream` | `Content/GatekeptStream.cs` | Stream wrapper der holder gatekeeper lease |
| 3 | `MediaFileContent : IContent` | `Content/MediaFileContent.cs` | MTP file content — OpenRead via gatekeeper |
| 4 | `MediaDeviceTraversal : ISourceTraversal` | `Infrastructure/Traversal/MediaDeviceScanner.cs` | Recursive MTP directory walk med gatekeeper |
| 5 | `IMediaDeviceScannerFactory` + impl | `Engine/Traversal/IMediaDeviceScannerFactory.cs`, `Infrastructure/Traversal/MediaDeviceScannerFactory.cs` | Factory for MediaDeviceScanner |
| 6 | `IMtpDeviceSession` + `MtpDeviceSession` | `Engine/Traversal/IMtpDeviceSession.cs`, `Infrastructure/Traversal/MtpDeviceSession.cs` | Device session lifecycle (Connect/Disconnect) |
| 7 | `IMtpCapableScanner` | `Engine/Traversal/IMtpCapableScanner.cs` | Capability interface for MTP scanners |

### 13c. Core4 Anbefalet MTP Lifecycle (Simpel Sekventiel)

Når MTP skal implementeres i Core4, bør det følge en simpel sekventiel lifecycle — ikke Core2's channel-baserede:

```text
BackupEngine.RunAsync()
  │
  ├── 1. Open MTP traversal/session (Connect)
  │       await using ISourceTraversal traversal = factory.Create(request)
  │       └── MTP traversal: Connect() i konstruktor eller OpenAsync()
  │
  ├── 2. Scan source (traversal itereres)
  │       foreach item in _scanner.ScanAsync(traversal, ...)
  │         └── Creates MediaFileContent (holder IContent med MTP stream access)
  │
  ├── 3. Download/stage content mens session er alive
  │       For hver item:
  │         └── item.Content.OpenRead() → GatekeptStream
  │               ├── gatekeeper.AcquireAsync()
  │               ├── MediaFileInfo.OpenRead()
  │               └── Stream.CopyToAsync(tempFile)
  │         └── item.ReplaceContent(MoveableFileContent) ← local temp
  │
  ├── 4. Traversal disposed → device disconnects
  │       ═══ await using traversal.DisposeAsync() ═══
  │       └── MTP: Disconnect() i DisposeAsync()
  │
  └── 5. Continue processing lokale temp-filer (hashing, move, sidecar...)
```

**Key principle:** Device holdes connected indtil alt content er downloaded til temp. Derefter disconnect — resten af pipelinen opererer på lokale filer. Dette er Core2's lifecycle tilpasset Core4's sekventielle `await using` pattern.

### 13d. Livscyklus Problem (Critical)

**Problemet:** I Core4's arkitektur oprettes traversal via:
```csharp
await using ISourceTraversal traversal = factory.Create(request);
```

Traversal itereres i step 5 (scan), og `await using` scopet slutter først ved metode-afslutning. Men `FileSystemTraversal.DisposeAsync` er en no-op, så det fungerer.

For MTP skal traversal `DisposeAsync` disconnecte enheden. Men `MediaFileContent.OpenRead()` kaldes FØRST i step 9 (download) — LÆNGE efter traversal iteration er færdig i step 5.

**Løsningsmuligheder:**

| Mulighed | Beskrivelse | Pros | Cons |
|----------|-------------|------|------|
| **A) Behold `await using` = hele RunAsync** | Traversal lever til slutningen af RunAsync. Device disconnectes først når alle items er færdige. | Enklest, minimale ændringer | Device holdes connected længere end nødvendigt |
| **B) Session-koncept som Core2** | `IMtpCapableScanner.OpenSession(plan)` → `IMtpDeviceSession`. Session dispos'es efter download (content buffering) er færdig. | Optimal — device disconnectes tidligt | Kræver ny interface + engine ændringer |
| **C) Content buffering før MoveTo** | Download alle filer til temp i en tidlig fase, mens traversal er alive. Derefter disconnect. | Løser problemet | Ændrer pipeline flow |

### 13e. Filtering under scanning

Core4s `FileSystemTraversal` har `IncludePatterns`/`ExcludePatterns` i `SourceTraversalRequest` men **ignorerer dem**. Skal implementeres når `GlobMatcher` portes fra Core2.

---

## 14. Design Decisions Log

| # | Decision | Rationale | Dato |
|---|----------|-----------|------|
| D1 | `TryAdd` pattern for DI | Callers kan override registrations | — |
| D2 | `BackupEngine` orkestrerer kun — logik i services | Separation of concerns | — |
| D3 | `DateTimeOffset` exclusively | Ingen `DateTime` properties i Core4 | — |
| D4 | `SaveAsync` må ALDRIG være cancellable | Ingen `CancellationToken` på store/session SaveAsync | — |
| D5 | SummaryStore + SessionStateService oprettes manuelt i engine | De har runtime-parametre (destination, sessionId) — DI uegnet | — |
| D6 | Fail-first: exception propagates | Ingen per-item try/catch | — |
| D7 | Temp-file + replace for atomic writes (BackupJsonSummaryStore) | Undgår korrupt JSON ved crash | — |
| D8 | Source/output access probe udskudt | Fail-first er acceptabelt midlertidigt; access probe kan tilføjes senere hvis det giver bedre fejlbeskeder | — |
| D9 | Cleanup i finally med inner try/catch | Undgår masking af original exception | — |
| D10 | `CleanupSessionTempDirectory` returnerer `bool` | Caller kan logge resultat | — |
| D11 | BackupProgressPhase.Completed tilføjet | Final phase efter processing | — |
| D12 | Final progress report før BackupResult | Consistent state | — |
| D13 | SourceTraversalFactory dispatcher på SourceType (ikke FileSystem-specifik) | Forberedt til MTP | — |
| D14 | MediaDevice kaster NotSupportedException i stedet for stub | Fail early, ikke silent | — |

---

## 15. Complete File Index

### 15a. Core — Relevante Filer

| Fil | Role |
|-----|------|
| `Handlers/BackupMaster.cs` | Main orchestrator — discover devices/drives, dispatch to BackupHandler |
| `Handlers/BackupHandler.cs` | **~1350 linjer** — monolithic backup engine |
| `Handlers/BackupHelper.cs` | Utility: timestamp helpers, ComputeHashes, temp dir, ShortenPath |
| `Handlers/HashCalculator.cs` | Single-pass multi-hash streaming (9 algorithms) |
| `Handlers/BackupRecordInfo.cs` | Per-file record for resume/persistence |
| `Handlers/BackupRecordDataStore.cs` | JSON persistence (Newtonsoft) |
| `Handlers/MediaDeviceHandler.cs` | MTP device discovery + config matching |
| `Handlers/DriveHandler.cs` | Drive discovery + config matching |
| `Handlers/RefactorNewBackup/BackupHandlerForDevice.cs` | Refactored version (594 lines, self-contained) |
| `Handlers/RefactorNewBackup/BackupHandlerForDrive.cs` | Stub (NotImplementedException) |
| `Handlers/RefactorNewBackup/AbstractBackupHandler.cs` | Abstract base for refactored handlers |
| `Metadata/MetadataExtractorFileInfo.cs` | EXIF/XMP/QuickTime metadata reading |
| `Metadata/SideCar/IniSideCarWriter.cs` | INI sidecar writer |
| `Metadata/SideCar/SideCarDocument.cs` | Sidecar document model |
| `CompareFiles/ReadFileInChunksAndCompareVector.cs` | SIMD-accelerated binary file comparison |
| `CompareFiles/FileComparer.cs` | Abstract base for file comparers |
| `Services/IMediaDeviceService.cs` | MTP device service interface |
| `Services/MediaDeviceServiceProd.cs` | Production MTP service (delegates to MediaDevices.dll) |
| `Handlers/BackupExceptionHandlerService.cs` | COMException handling for MTP errors |
| `BackupSource/PortableDevices/DeviceBackupJob.cs` | MTP backup job model |
| `BackupSource/Drives/DriveBackupJob.cs` | Drive backup job model |
| `BackupSource/BackupJob.cs` | Abstract backup job |
| `Configs/BaseSourceConfig.cs` | Source config (Device/Drive) |
| `Configs/DeviceSourceConfig.cs` | Device-specific config |
| `Configs/DriveSourceConfig.cs` | Drive-specific config |
| `BackupSource/SourceType.cs` | `enum SourceType { Device, Drive }` |

### 15b. Core2 — Relevante Filer

| Fil | Role |
|-----|------|
| `Content/IContent.cs` | Content abstraction interface |
| `Content/IMoveableContent.cs` | Moveable content interface |
| `Content/FileContent.cs` | Local file content implementation |
| `Content/MediaFileContent.cs` | MTP file content implementation |
| `Content/GatekeptStream.cs` | Lease-holding stream wrapper |
| `Infrastructure/Traversal/ITraversalScanner.cs` | Generic traversal scanner interface |
| `Infrastructure/Traversal/FileSystemScanner.cs` | Local disk scanner |
| `Infrastructure/Traversal/MediaDeviceScanner.cs` | MTP device scanner |
| `Infrastructure/Traversal/NoopMediaDeviceScanner.cs` | No-op MTP scanner fallback |
| `Infrastructure/Traversal/MediaDeviceScannerFactory.cs` | Factory for MTP scanners |
| `Infrastructure/Traversal/IMtpGatekeeper.cs` | MTP gatekeeper interface |
| `Infrastructure/Traversal/MtpGatekeeper.cs` | Semaphore-based gatekeeper |
| `Infrastructure/Traversal/MtpDeviceSession.cs` | MTP device connection lifecycle |
| `Infrastructure/Traversal/TraversalProgress.cs` | Progress snapshot record |
| `Engine/Traversal/IBackupScanner.cs` | High-level scanner interface |
| `Engine/Traversal/BackupScanner.cs` | Orchestrating scanner (FileSystem + MTP) |
| `Engine/Traversal/NoopBackupScanner.cs` | No-op fallback scanner |
| `Engine/Traversal/IMtpCapableScanner.cs` | MTP session capability interface |
| `Engine/Traversal/IMediaDeviceScannerFactory.cs` | Factory interface for MTP scanners |
| `Engine/Traversal/IMtpDeviceSession.cs` | MTP session interface |
| `Engine/BackupEngine.cs` | Parallel pipeline orchestrator (channels) |
| `Engine/BackupEngineSequentiel.cs` | Sequential pipeline orchestrator |
| `Engine/Steps/AbstractPipelineStage.cs` | Base pipeline stage with worker pool |
| `Engine/Steps/StagingStep/ContentBufferingPipelineStage.cs` | Staging stage (download to temp) |
| `Engine/Steps/StagingStep/ContentBufferingItemStep.cs` | Staging step |
| `Engine/Staging/IStagingDownloader.cs` | Staging downloader interface |
| `Engine/Staging/StagingDownloader.cs` | Downloads source to temp, replaces content |
| `Engine/Resilience/ExponentialBackoffRetryPolicy.cs` | Retry policy (3 attempts) |
| `Engine/Orchestration/BackupEngineOptions.cs` | Engine configuration |
| `Engine/Orchestration/JobValidator.cs` | Plan validation |
| `Utilities/GlobMatcher.cs` | Glob pattern matching |
| `Domain/Item/IBackupItem.cs` | Core item interface |
| `Domain/Item/BackupItem.cs` | Item implementation |
| `Domain/Item/BackupMetadata.cs` | Thread-safe property bag |
| `Domain/Item/MetadataKey.cs` | All metadata keys (53 entries) |
| `Api/Request/BackupPlan.cs` | Input DTO |
| `Api/Request/Enums/SourceType.cs` | `enum SourceType { FileSystem, MediaDevice }` |
| `Api/Progress/IBackupProgress.cs` | Progress interface |
| `Api/Progress/Enums/BackupPhase.cs` | Phase enum |
| `Api/Progress/Enums/FilePhase.cs` | Per-file phase enum |
| `DependencyInjection/ServiceCollectionExtensions.cs` | DI registration |

### 15c. Core4 — Relevante Filer

| Fil | Role |
|-----|------|
| `Traversal/ISourceTraversal.cs` | Source traversal interface (IAsyncDisposable) |
| `Traversal/ISourceTraversalFactory.cs` | Factory interface |
| `Traversal/SourceTraversalItem.cs` | Traversal entry record |
| `Traversal/SourceTraversalRequest.cs` | Traversal request |
| `Traversal/SourceTraversalProgress.cs` | Traversal progress |
| `Traversal/SourceTraversalFactoryCreateRequest.cs` | Factory create request |
| `Traversal/SourceTraversalFactory.cs` | Factory — dispatcher på SourceType |
| `Traversal/FileSystemTraversal.cs` | File system traversal implementation |
| `Scanner/IBackupScanner.cs` | Scanner interface |
| `Scanner/BackupScanRequest.cs` | Scan request |
| `Scanner/BackupScanProgress.cs` | Scan progress |
| `Scanner/BackupScanner.cs` | Scanner — mapper SourceTraversalItem → BackupItem |
| `Models/IContent.cs` | Content interface |
| `Models/IBackupItem.cs` | Backup item interface |
| `Models/BackupItem.cs` | Backup item implementation |
| `Models/FileContent.cs` | File content |
| `Models/MoveableFileContent.cs` | Moveable file content |
| `Models/IMoveableContent.cs` | Moveable content interface |
| `Models/IFileInfoSource.cs` | File info access interface |
| `Models/ItemMetadata.cs` | Per-item metadata |
| `Models/BackupRecord.cs` | Processing record (item + status + metadata) |
| `Engine/BackupEngine.cs` | Sequential orchestrator |
| `Engine/CollisionHelpers.cs` | Collision resolution |
| `Engine/TempDirectoryHelper.cs` | Temp directory management |
| `Engine/Validation/BackupPlanValidator.cs` | **Plan validation (blokerer alt — C4)** |
| `Engine/Downloader/DownloadService.cs` | Download content to temp |
| `Engine/Hashing/HashService.cs` | Multi-hash computation |
| `Engine/Sidecar/SidecarService.cs` | INI sidecar generation |
| `Engine/Session/ISessionStateService.cs` | Session state interface |
| `Engine/Session/SessionStateService.cs` | Session state implementation |
| `Engine/Session/IBackupRecordRepository.cs` | Record repository interface |
| `Engine/Session/BackupMemoryRecordRepository.cs` | In-memory record repository |
| `Engine/DiskSpace/IDiskSpaceValidator.cs` | Disk space validator interface |
| `Engine/DiskSpace/DiskSpaceValidator.cs` | Disk space validator |
| `Engine/Runner/IBackupRunner.cs` | Runner interface (unused) |
| `Engine/Runner/BackupRunner.cs` | Sequential runner (unused) |
| `Engine/Runner/ParallelBackupRunner.cs` | Parallel runner (unused) |
| `Engine/Runner/LimitedParallelBackupRunner.cs` | Limited parallel runner (unused) |
| `Engine/TimeStamp/IEarliestTimestampResolutionService.cs` | Timestamp resolution interface |
| `Engine/TimeStamp/EarliestTimestampResolutionService.cs` | EXIF/XMP/IPTC/GPS/QuickTime resolution |
| `State/ISummaryStore.cs` | Summary store interface |
| `State/BackupJsonSummaryStore.cs` | JSON file-backed summary store |
| `State/BackupMemorySummaryStore.cs` | In-memory summary store |
| `State/BackupSummary.cs` | Summary model |
| `State/BackupSummaryItem.cs` | Per-item summary |
| `Api/IBackupEngine.cs` | Engine interface |
| `Api/Models/BackupPlan.cs` | Input DTO |
| `Api/Models/BackupProgress.cs` | Progress model |
| `Api/Models/BackupResult.cs` | Result model |
| `Api/Models/Enums/*.cs` | All enums |
| `DependencyInjection/ServiceCollectionExtensions.cs` | DI registration |
| `Hashing/IHashGenerator.cs` | Hash generator interface |
| `Hashing/StreamHashGenerator.cs` | Single-pass multi-hash streaming |

---

## 16. Key Architectural Insights

1. **Core → Core2 → Core3 → Core4** viser en tydelig evolution: monolithic → generic/pipeline → hastejob → streamlined/sequential. Core4 er designet som den sidste seriøse indsats — enkel men komplet.

2. **Original Core = funktionel sandhed.** Den viser præcis hvad programmet skal kunne: MTP discovery, enumeration, download, metadata, hashing (7 algoritmer), collision med binary compare, sidecar med device info, resume persistence, cleanup. Brug Core som feature-reference, ikke som arkitektur-model.

3. **Core2 = komponent- og idébank.** Genbrug gatekeeper, content abstraction, generisk traversal, glob filtering — men undgå channels, parallelisme, og over-generic design.

4. **Core3 = advarsel.** Halve implementationer uden solid arkitektur duer ikke. Core4 må ikke gentage dette.

5. **Core4's styrke:** Enklere end Core2, renere pipeline, bedre separation. `ISourceTraversal : IAsyncDisposable` forbereder korrekt til MTP. Fail-first giver predictable fejlhåndtering. Single-threaded inline `foreach` er det rigtige udgangspunkt.

6. **Core4's nuværende kritisk blokering:** `BackupPlanValidator` (C4) har modstridende checks — kræver `ComparisonHashAlgorithmTypes` non-empty i fase 1, men kaster `FeatureNotImplementedException` hvis den er non-empty i fase 2. **Intet plan kan passere.**

7. **MTP gatekeeper mønsteret** (Core2) er essentielt når MTP skal implementeres: `SemaphoreSlim(1,1)` sikrer single-threaded MTP access. `GatekeptStream` holder leasen for stream lifetime — undgår per-chunk acquire/release overhead.

8. **Core4's `MoveableFileContent.MoveTo` har en bug:** `overwrite` parameteren accepteres men ignoreres — `FileInfo.MoveTo(dest, false)` kaldes altid med `false`. `Overwrite` collision strategy virker ikke.

9. **Content replacement chain** i Core4: `FileContent` (fra traversal) → `MoveableFileContent` (efter download) → `MoveableFileContent` (efter MoveTo). Hvert step skifter `IContent` ud, så den gamle bliver eligble for GC.

10. **Core4's definition of done:** Kan køre en komplet backup end-to-end, kan resume korrekt efter genstart, bevarer timestamps, producerer brugbar sidecar metadata, håndterer collisions sikkert, undgår unødige dubletter, verificerer skrevne filer, understøtter filesystem kilder, og på sigt MTP kilder — alt sammen uden at blive en monolyt.

---

## 17. Nøgleforskelle i Enums

| Koncept | Core | Core2 | Core4 |
|---------|------|-------|-------|
| Source type | `Device`, `Drive` | `FileSystem`, `MediaDevice` | `FileSystem`, `MediaDevice` |
| Collision | Implicit (binary compare → skip/rename) | `Skip`, `Rename`, `Overwrite` | `Skip`, `Rename`, `Overwrite`, `Error` |
| Sidecar format | INI only | INI, JSON | INI, JSON (stub) |
| Hash types | 7 (hardcoded) | Konfigurerbar (samme 9) | Konfigurerbar (samme 9) |
| Backup phase | Ingen | `Starting`, `Traversing`, `Transferring`, `Completed`, `Cancelled` | `Starting`, `Scanning`, `Transferring`, `Completed`, `Cancelled`, `Failed` |
| Resume strategy | Ingen (exception hvis ændret) | `Abort`, `Continue`, `Restart` | `Abort`, `Continue`, `Restart` |
| Item status | `IsSaved` (bool) | `Pending`, `Success`, `Failed`, `Skipped` | `Pending`, `Active`, `Succeeded`, `Skipped`, `Failed` |
