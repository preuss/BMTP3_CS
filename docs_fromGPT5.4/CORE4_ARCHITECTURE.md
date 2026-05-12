# Core4 - Arkitekturdokument

**Formål:** Arkitektonisk baseline for Core4. Dokumentet beskriver de aktuelle public contracts, den tilsigtede interne struktur og den lagdelte vækststi mod en færdig Core4-implementation.

**Sidst opdateret:** 2026-05-12

**Status:** Normaliseret mod det nuværende `BMTP3.Core4`-skeleton og de opdaterede Core4-masterdokumenter.

---

## 1. Autoritet og scope

Dette dokument er et arkitekturdokument, ikke den detaljerede implementeringsguide.

Læs det med disse regler:

1. Det aktuelle `BMTP3.Core4`-skeleton er baseline for public contracts.
2. `docs\CORE4_IMPLEMENTATION_GUIDE_DA.md` er den autoritative implementeringsguide.
3. `docs\CORE4_MASTER_SYNTHESIS.md` er konfliktløseren, hvis ældre dokumenter siger noget andet.
4. `docs\Core4_Master_Architecture.md` er den dybe reference.

Dokumentet må derfor **ikke** genindføre ældre Core4-former som:

- `DeviceId`
- `SourceDirectory` / `OutputDirectory`
- `WriteSidecar`
- `HashTypes`
- `MaxDegreeOfParallelism = -1`
- path-baseret MTP-detektion
- tungere public progress-contracts end skeleton kræver

---

## 2. Designprincipper

Core4 bygger på nogle få faste principper:

- **Korrekthed før hastighed.** En lille korrekt sekventiel implementation kommer først.
- **MTP-stabilitet før concurrency.** Medieenheder behandles som skrøbelige kilder og holdes sekventielle.
- **Streaming før fuld buffering.** Discovery skal streame items via `IAsyncEnumerable<BackupItem>`.
- **Eksplicit tiering.** Avanceret adfærd tilføjes i lag og må ikke antages stiltiende.
- **Ingen skjulte engine-fallbacks.** Metadata- og timestamp-fallback hører hjemme i metadata-laget.
- **Små public contracts.** CLI/UI må gerne blive rigere, men engine-contracten skal forblive stabil og faktuel.

---

## 3. Tier-roadmap

| Tier | Mål | Påkrævet resultat |
|---|---|---|
| Tier 1 | Virkende backup-kerne | Sekventiel filsystem + MTP backup virker ende-til-ende |
| Tier 2 | Robusthed og brugbarhed | Bedre diagnostics, stabil progress, retries, error shaping |
| Tier 3 | Optionel enrichment | Hashing, metadata, verification, timestamp correction |
| Tier 4 | Performance | Begrænset parallel filsystem-engine |
| Tier 5 | Persistence og resume | Holdbar session/repository-state |
| Tier 6 | Avanceret rapportering/UI | Rigere CLI/UI-visning og eksporter |

Vigtige normaliseringer:

- **Tier 1 er først færdig når både filsystem og MTP virker.**
- `MaxDegreeOfParallelism` findes allerede i `BackupPlan`, men den reelle parallelle sti hører til **Tier 4**.
- Tier 3-features skal **gates eksplicit**, indtil de er implementeret.

---

## 4. Aktuelle public contracts

Dette er de contracts, som skal regnes som sandheden, fordi de allerede findes i Core4-skeletonet.

### 4.1 `IBackupEngine`

**Placering:** `Api\IBackupEngine.cs`

```csharp
public interface IBackupEngine
{
    Task<BackupResult> RunAsync(
        BackupPlan plan,
        IProgress<IBackupProgress>? progress,
        CancellationToken cancellationToken);
}
```

Ansvar:

- validere planen
- orkestrere scan og processing
- rapportere faktiske progress snapshots
- returnere et afsluttende `BackupResult`

### 4.2 `IBackupProgress`

**Placering:** `Api\IBackupProgress.cs`

```csharp
public interface IBackupProgress
{
    BackupPhase CurrentPhase { get; }
    int DirectoriesScanned { get; }
    int FilesDiscovered { get; }
    long BytesTotal { get; }
    int FilesProcessed { get; }
    int FilesSucceeded { get; }
    int FilesSkipped { get; }
    int FilesFailed { get; }
    long BytesProcessed { get; }
    IReadOnlyList<IFileProgress> ActiveFiles { get; }
}
```

Normalisering:

- `IBackupProgress` er bevidst lille og faktuel.
- Public ETA, throughput, `CurrentFilePath` og active worker counts er **ikke** del af den aktuelle public contract.
- Rigere rapportering kan tilføjes senere i CLI/UI-lag uden at redefinere Tier 1-baseline.

### 4.3 `IFileProgress`

**Placering:** `Api\IFileProgress.cs`

```csharp
public interface IFileProgress
{
    string Path { get; }
    long? BytesTotal { get; }
    long BytesProcessed { get; }
}
```

`ActiveFiles` virker både i sekventielle og senere begrænsede parallelle flows.

### 4.4 `BackupPlan`

**Placering:** `Models\BackupPlan.cs`

```csharp
public sealed record BackupPlan
{
    public string Name { get; init; } = string.Empty;
    public BackupSourceType SourceType { get; init; }
    public string Source { get; init; } = string.Empty;
    public bool Recursive { get; init; } = true;
    public IReadOnlyList<string>? IncludePatterns { get; init; }
    public IReadOnlyList<string>? ExcludePatterns { get; init; }
    public string Destination { get; init; } = string.Empty;
    public OutputStructure OutputStructure { get; init; }
    public CollisionStrategy CollisionStrategy { get; init; }
    public bool DryRun { get; init; }
    public bool StopOnError { get; init; }
    public bool SkipExisting { get; init; }
    public bool EnableHashing { get; init; }
    public bool EnableMetadata { get; init; }
    public bool EnableVerification { get; init; }
    public bool EnableTimestampCorrection { get; init; }
    public int? MaxDegreeOfParallelism { get; init; }
}
```

Semantik:

- `SourceType` er eksplicit og styrer source-valg.
- `Source` er den samlede kilde-identitet eller rodsti.
- `Destination` er output-directory.
- Feature-flags er booleans, ikke type-lister.
- `MaxDegreeOfParallelism` betyder:
  - `null` = engine vælger selv
  - `1` = tving sekventiel
  - `> 1` = parallel sti senere i Tier 4

### 4.5 `BackupResult`

**Placering:** `Models\BackupResult.cs`

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

Normalisering:

- Resulttypen er `BackupResult`, ikke `BackupJobResult`.
- Resultatet er et faktuelt summary, ikke et performance-dashboard.

### 4.6 Centrale enums

Aktuelle baseline-enums:

- `BackupSourceType`: `MediaDevice`, `FileSystem`
- `OutputStructure`: `Flat`, `PreserveHierarchy`
- `CollisionStrategy`: `Skip`, `Overwrite`, `Rename`
- `BackupPhase`: `Starting`, `Scanning`, `Transferring`, `Completed`, `Cancelled`, `Failed`

Genindfør ikke ældre fase-navne som `Idle`, `Copying`, `Finalizing` eller `Done` som public baseline.

---

## 5. Aktuel intern model

### 5.1 `BackupItem`

**Placering:** `Models\BackupItem.cs`

```csharp
internal sealed class BackupItem
{
    public string Id { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string RelativePath { get; init; } = string.Empty;
    public string? DestinationPath { get; set; }
    public long? SizeBytes { get; init; }
    public DateTimeOffset? ModifiedAt { get; init; }
    public BackupItemStatus Status { get; set; } = BackupItemStatus.Pending;
}
```

Dette er engine'ens interne working item. Den starter som discovery-data og kan enriches, efterhånden som Core4 modnes.

### 5.2 `BackupItemStatus`

Aktuelle interne states:

- `Pending`
- `Succeeded`
- `Skipped`
- `Failed`

### 5.3 `IBackupScanner`

**Placering:** `Scanner\IBackupScanner.cs`

```csharp
internal interface IBackupScanner
{
    IAsyncEnumerable<BackupItem> ScanAsync(
        BackupPlan plan,
        CancellationToken cancellationToken);
}
```

Normalisering:

- Scanner output er streaming.
- Scanner må ikke returnere `Task<IReadOnlyList<BackupItem>>`.
- Både filsystem og MTP kan ligge bag samme scanner-abstraktion.

### 5.4 Backup-session-state

Aktuelle state-relaterede contracts i skeletonet:

- `BackupSessionState`
- `BackupSessionStateKey`
- `BackupSessionStateKeyFactory`
- `IBackupSessionStateStore`

Store-contracten er:

```csharp
internal interface IBackupSessionStateStore
{
    Task<BackupSessionState> OpenAsync(
        BackupSessionStateKey key,
        CancellationToken cancellationToken);
}
```

Dette er kimen til Tier 5 persistence/resume, men er allerede nyttigt i Tier 1 som intern sandhed.

### 5.5 Validering

Aktuel entrypoint:

- `BackupPlanValidator.Validate(BackupPlan plan)`

Aktuel adfærd:

- kræver `Name`
- kræver `Source`
- kræver `Destination`
- validerer enum-værdier
- afviser `MaxDegreeOfParallelism <= 0` når den er sat
- kaster `BackupPlanValidationException` med samlede validation-fejl

---

## 6. Engine-arkitektur

### 6.1 Aktuel engine-baseline

Aktuelle engine-klasser i skeletonet:

- `SequentialBackupEngine` - aktiv baseline
- `LimitedParallelBackupEngine` - placeholder til senere

Aktuel status:

- `SequentialBackupEngine` binder allerede validation, session-state og streaming scan sammen.
- `LimitedParallelBackupEngine` eksisterer kun som skelet og må ikke definere nuværende adfærd endnu.

### 6.2 Planlagt dispatch-model

Den anbefalede dispatch-regel er:

1. Hvis `plan.SourceType == BackupSourceType.MediaDevice`, brug sekventiel engine.
2. Ellers hvis `plan.MaxDegreeOfParallelism == 1`, brug sekventiel engine.
3. Ellers brug sekventiel engine indtil Tier 4 findes.
4. Når Tier 4 findes, kan filsystem-planer med `MaxDegreeOfParallelism > 1` bruge `LimitedParallelBackupEngine`.

Hvis `BackupEngineFactory` eller en resolver introduceres, skal den følge disse regler.

### 6.3 Sekventiel execution-flow

Den tilsigtede Tier 1-flow er:

1. validér `BackupPlan`
2. åbn eller opret `BackupSessionState`
3. sæt phase til `Scanning`
4. stream items fra `IBackupScanner`
5. føj items til session state
6. sæt phase til `Transferring`
7. processér pending items én ad gangen
8. beregn destination path
9. udfør transfer
10. skriv sidecar
11. opdatér progress og item-state
12. complete, cancel eller fail session
13. byg `BackupResult`

### 6.4 Begrænset parallel execution

Begrænset parallelisme er en **filesystem-only Tier 4** feature.

Regler:

- parallelisér aldrig MTP
- scanner streamer stadig sekventielt
- transfer workers må senere processere filsystem-items parallelt
- progress rapporteres stadig gennem samme public `IBackupProgress`

---

## 7. Kildeadaptere

### 7.1 Filesystem

Planlagte hovedkomponenter:

- `FileSystemScanner`
- `FilesystemFileTransfer`

Ansvar:

- enumerere filer
- respektere include/exclude patterns
- beregne relative paths
- bevare cancellation-responsiveness
- overflade IO-fejl tydeligt

### 7.2 Media device

Planlagte hovedkomponenter:

- `MtpScanner`
- `MTPFileTransfer`

Core4-regler for MTP:

- altid sekventiel
- STA/COM-krav skal respekteres
- keepalive mindst hver 30. sekund
- operation timeout 60 sekunder
- retry-backoff 1s, 2s, 4s

Kildevalg skal bruge `BackupPlan.SourceType`, ikke path-heuristik.

---

## 8. Optionel enrichment

Disse capabilities hører til senere tiers og skal tilføjes eksplicit.

### 8.1 Tier 3-komponenter

Anbefalede interfaces/klasser:

- `IFileTransfer`
- `ISidecarGenerator`
- `IMetadataReader`
- `IItemHasher`
- `IIntegrityVerifier`
- `ITimestampCorrector`
- `TimestampCorrectionResult`

### 8.2 Metadata- og timestamp-strategi

Den normaliserede prioritet for metadata/datoer er:

1. `MetadataExtractor`
2. `ExifTool` fallback
3. filsystem-attributter

Vigtigt:

- Core4 skal bruge **begge** værktøjer i en styret strategi, ikke ExifTool-only.
- Filsystem-fallback hører hjemme i metadataresultatet.
- Timestamp correction skal bruge det normaliserede metadataresultat.

### 8.3 Sidecar-generering

Sidecars er del af den normale succes-sti.

Regler:

- filnavnssuffix: `.sidecar.json`
- skrives efter succesfuld transfer
- kan senere enriches med hashes, metadata, verification og timestamp-resultat

Genindfør ikke `.bmtp3.json` som aktiv Core4-konvention.

---

## 9. Progress og DI

### 9.1 Engine-progress

Engine rapporterer snapshots via:

```csharp
IProgress<IBackupProgress>?
```

Normalized guidance er at debounce hyppige opdateringer til omtrent maks. hver 500 ms under aktivt arbejde.

### 9.2 CLI/UI-rapportering

Rig CLI-output er værdifuldt, men skal ligge oven på engine-contracten.

Det betyder:

- en CLI må gerne aflede ETA selv
- UI-specifikke notifiers kan komme senere
- `IProgressNotifier` er **ikke** del af den aktuelle Core4 public baseline

### 9.3 Dependency Injection

Skeletonet indeholder endnu ikke den endelige DI-composition root, så arkitekturen skal være konservativ her.

Anbefalet retning:

- registrér `SequentialBackupEngine`
- registrér `LimitedParallelBackupEngine`
- registrér source-specifikke scannere og transfer services
- registrér `IBackupSessionStateStore`
- tilføj en lille resolver/factory, som vælger engine ud fra `BackupPlan`

---

## 10. Anbefalet implementeringsrækkefølge

Den anbefalede rækkefølge for en koder er:

1. færdiggør `SequentialBackupEngine`
2. implementér filsystem-scanner og transfer
3. implementér sidecar-generering
4. implementér progress snapshots
5. implementér MTP-scanner og MTP-transfer
6. hærd validation og error mapping
7. tilføj Tier 3-features
8. tilføj `LimitedParallelBackupEngine` for filsystem
9. tilføj persistence/resume
10. tilføj rigere CLI/UI-rapportering

Dette bevarer en lille fungerende Core4 tidligt og udskyder kompleksitet til senere.

---

## 11. Slut-normalisering

Hvis et andet dokument er uenig med dette, så foretræk disse sandheder:

- `BackupPlan` bruger `SourceType`, `Source`, `Destination` og boolean feature flags
- `BackupResult` er resulttypen
- `IBackupScanner` streamer `BackupItem`
- MTP er sekventiel
- metadata-datoer læses via `MetadataExtractor` først og `ExifTool` som fallback
- sidecars bruger `.sidecar.json`
- `MaxDegreeOfParallelism` er nullable og bruger ikke `-1`
- rigere UI-rapportering er senere tier, ikke Tier 1-baseline

For detaljeret step-by-step implementation, fortsæt i `docs\CORE4_IMPLEMENTATION_GUIDE_DA.md`.
