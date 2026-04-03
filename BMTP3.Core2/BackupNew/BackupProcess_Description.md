# Backup pipeline — detaljeret design og kontrakter

Formål: beskrive en fuldt specificeret pipeline så implementering, tests og reviews er entydige. Indeholder kontrakter for metadata, hashing, collision‑policy, concurrency og eksempler på hvordan steps køres.

## Overblik — trin og ansvar
1. Scan (single‑thread)
   - `IDeviceScanner.ScanAsync(sourceId, sourcePath, recursive, ct)` enumererer MTP/device entries og producerer `MediaFileInfo`.
   - Kør kun ét scanner‑loop pr. device for at undgå driver‑races.

2. Convert (single‑thread)
   - `IMediaToBackupItemConverter.Convert(MediaFileInfo)` opretter `BackupItem` med minimal metadata (name, relative path, device ids).
   - Ingen tung I/O her.

3. Staging / download (begrænset concurrency)
   - `IStagingDownloader.DownloadToStagingAsync(item, stagingRoot, progress, ct)` downloader MTP‑objekt til lokal tempfil.
   - Begræns samtidige downloads med `SemaphoreSlim(maxConcurrentDownloads)` (fx 3).
   - Efter download: `item.ReplaceContent(new FileContent(tempPath))`.
   - Seed alle MTP‑metadata (DateAuthored, CreationTime, LastWriteTime, Length, PersistentUniqueId, DeviceId etc.) i `item.Metadata` i samme tråd.

4. Lightweight seed (same thread som download)
   - Gem MTP‑felter i `BackupMetadata` (MetadataKey.*). Dette bruges straks til path generation og filter.

5. Extended metadata extraction (parallel)
   - Kør `IMetadataExtractor.EnrichMetadataAsync(item, ct)` som parser EXIF/XMP/IPTC på den lokale fil.
   - Regler for timestamps (prioritet): XMP datetime > EXIF Date Taken > MTP DateAuthored > File LastWriteTime > Now.
   - Når bedste dato er fundet, sæt filsystemets timestamps på den lokale fil (use UTC setters):
     - `File.SetCreationTimeUtc(path, dt);`
     - `File.SetLastWriteTimeUtc(path, dt);`
     - `File.SetLastAccessTimeUtc(path, dt);`
   - Opdater `item.Metadata` felterne (AuthoredDateTime, CreatedDateTime, ModifiedDateTime).

6. Hashing (parallel, on‑demand)
   - Beregn hashes kun hvis nødvendigt (fx `plan.ComparisonType == CollisionComparisonType.Hash` eller ved request fra resolver).
   - Brug `IHashGenerator.ComputeHashesAsync(Stream dataStream, IEnumerable<HashType> algorithms, ct)` over lokal tempfil.
   - Gem resultater i metadata under `MetadataKey.Hashes` som `Dictionary<HashType,string>` (nøgle = enum value, værdi = lowercase hex uden præfiks).
     - Eksempel: `{ HashType.SHA2_256: "a3b..." }`
   - Normaliser hex til lowercase og uden `0x` eller `-`.

7. Destination prepare / inspection
   - Kør en destination‑inspektør (fx `IDestinationInspector.InspectAsync(proposedPath, plan, ct)`) for at sikre sidecars/metadata på destination er læsbare.
   - Sidecar candidates (læses af resolver): `file + ".bmtp3.json"`, `file + ".meta.json"`, `file + ".json"`, `file + ".meta"`.
   - Hvis sidecar ikke findes, indiker inspectoren og beslut fallback‑adfærd.

8. Collision resolution & flyt (grupper pr. target)
   - Group items by proposed final target (relative path under `plan.OutputPath`).
   - For hver gruppe: kør sekventielt per‑target (brug per‑target semaphore/lock).
   - `ICollisionResolver.ResolveAsync(item, proposedFullPath, plan, ct)` returnerer `CollisionResult` (Action: Copy/Skip/Rename/Error + TargetPath).
   - `CompareHashesAsync` (i resolver) forudsætter `MetadataKey.Hashes` i source metadata og tilgængelig dest sidecar hash. Hvis disse mangler:
     - Først: trigger destination‑inspektor to fetch sidecars.
     - Hvis dest hash stadig mangler og plan kræver hash -> enten fallback til binary compare eller markér som ikke‑sammenlignbar (konfigurerbart).
   - Ved rename: generér unique name via `GenerateUniquePathAsync` og skriv sidecar.

9. Flyt / skriv og final persist
   - Flyt/skriv lokal tempfil til final destination (atomisk copy/replace når muligt).
   - Opret/skriv sidecar i valgt format (`plan.SidecarFormat`) med `item.Metadata.ToDictionary()` inkl. `hashes`.
   - Persist item state i `IBackupRepository.PersistItemStateAsync(item, ct)` ved succes eller fejl.
   - Rapportér progression via `IProgress<BackupProgress>`.

## Kontrakter og formatkrav (præcise)
- Hash metadata:
  - Type: `Dictionary<HashType, string>`
  - Value: lowercase hex (example: `e3b0c442...`)
  - Placering: `item.Metadata.Set(MetadataKey.Hashes, dict)`

- Timestamps:
  - Lagring i metadata som `DateTime?` UTC i `MetadataKey.AuthoredDateTime`, `MetadataKey.CreatedDateTime`, `MetadataKey.ModifiedDateTime`.
  - Filtimestamps skal sættes i UTC med `File.Set*TimeUtc`.

- Sidecar format:
  - Standard JSON med top‑level `hashes` object and other keys.
  - Resolver læser sidecar søgekandidater i prioriteret rækkefølge (se collision resolver implementation).

- Failure contract for missing data:
  - Hvis en step mangler nødvendige fakta (fx hashes) skal step enten:
    - returnere en klar result (e.g., `NeedsPreparation`), eller
    - kaste en specialiseret exception som orkestratoren fanger og håndterer (log + markér item + persist).
  - Undgå ukontrolleret throw som stopper hele job.

## Concurrency primitives (konkret)
- Channels:
  - Brug `Channel<T>` mellem stages. Bounded size (fx 128) for backpressure.
  - Pipeline eksempel: `Channel<MediaFileInfo> scan -> Channel<BackupItem> convert -> Channel<BackupItem> staged -> Channel<BackupItem> enriched -> ...`

- Staging limiter:
  - `SemaphoreSlim stagingSemaphore = new SemaphoreSlim(maxConcurrentDownloads);`
  - `await stagingSemaphore.WaitAsync(ct); try { await _stagingDownloader.Download(...); } finally { stagingSemaphore.Release(); }`

- Per‑target serialization:
  - `ConcurrentDictionary<string, SemaphoreSlim> targetLocks`
  - For hver item: `var sem = targetLocks.GetOrAdd(targetDirOrFileKey, _ => new SemaphoreSlim(1,1)); await sem.WaitAsync(ct); try { /* move */ } finally { sem.Release(); }`

## Step / processor model
- `IBackupItemStep<TIn,TOut>` — per‑item business logic (du har `IBackupItemStep`).
  - Execute only affects a single item.
  - Should be pure w.r.t. item scope (update metadata/persist but not manage concurrency).

- `BackupStepProcessor<TIn,TOut>` (ny — anbefalet)
  - Generic runner that:
    - Takes `IBackupItemStep<TIn,TOut> step`, `ChannelReader<TIn> input`, `ChannelWriter<TOut> output`, `degreeOfParallelism`, `IRetryPolicy`.
    - Starts worker tasks that `await input.ReadAllAsync(ct)`, call `await step.ExecuteAsync(inputValue, item, ct)`, write result to output.
    - Handles transient retry via `IRetryPolicy` around step.ExecuteAsync.
    - Ensures `CancellationToken` propagation.
  - Benefits: samme processor type kan bruges for Enrich/Hash/Prepare steps with different parallelism.

## Error handling & retry
- Use `IRetryPolicy.ExecuteAsync(func, ct)` for network/read/write operations.
- Log clear audit entries into `item.AuditTrail` for major events (staged, hashed, resolved, moved, failed).
- When a non‑transient error occurs for an item: mark `item.Fail(msg, stepName, ex)` and persist via repository; continue processing other items.

## Tests / Integration
- Integration:
  - Simulated non‑seekable MTP stream that forces staging. Verify no step attempts direct MTP reads aside from staging.
  - End‑to‑end test: small dataset with intentional collisions (same target) to verify rename behavior and sidecar creation.

- Unit tests:
  - `IMetadataExtractor` (EXIF/XMP parsing cases: only XMP present, XMP+EXIF, missing dates).
  - `IHashGenerator` (multiple algorithms, cancellation, large stream handling).
  - `ICollisionResolver` (hash vs binary, missing dest sidecar behavior).
  - `BackupStepProcessor` concurrency (bounded channel behavior, retry usage).
  - Per‑target locking correctness.

## Mapping til eksisterende kode (konkret)
- Orchestration / pipeline builder: `BackupEngineImpl` — ansvarlig for kanaler, step processor instantiation og overall job lifecycle.
- Scan: `BackupScanner` / `IDeviceScanner`
- Convert: `MediaToBackupItemConverter`
- Staging: `IStagingDownloader` (single‑threaded or limited concurrency via semaphore)
- Enrich: `IMetadataExtractor.EnrichMetadataAsync` (must operate on staged file)
- Hash: `IHashGenerator`
- Resolve / Path: `IPathGenerator`, `ICollisionResolver`
- Persist: `IBackupRepository`

## Migration‑udkast (kort)
1. Implementér `BackupStepProcessor<TIn,TOut>`.
2. Refactorér `BackupItemProcessor` body til konkrete `IBackupItemStep` implementeringer (EnrichStep, HashStep, PrepareStep, MoveStep).
3. I `BackupEngineImpl`, sammensæt pipeline som kanaler + `BackupStepProcessor` instanser med passende parallelisme.
4. Tilføj guards: hver step skal validere at `item.Content` er staged før heavy I/O.

---

Hold dette dokument som kontrakt — hver ændring der ændrer pipeline skal referere til denne fil og forklare hvordan den respekterer invariants (staging, hash‑format, sidecar, concurrency). Hvis du ønsker, kan jeg nu generere en konkret `BackupStepProcessor<TIn,TOut>` implementeringsskitse og et kort eksempel i `BackupEngineImpl` der kæder tre steps.  
