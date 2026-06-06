# IFileStore — Research & Architectural Decisions

> **Dato:** 6 Jun 2026
> **Formål:** Dokumentere designet af `IFileStore` — en fælles metadata-wrapper over `DriveInfo` (filesystem) og `MediaDevice` (MTP). Sikre at fremtidige beslutninger træffes med fuld forståelse af tidligere overvejelser.

---

## 1. Problembeskrivelse

Core4 har to fundamentalt forskellige source-typer:
- **FileSystem** — lokale/network drives, repræsenteret ved `System.IO.DriveInfo`
- **MediaDevice** — MTP-enheder (telefoner, kameraer), repræsenteret ved `MediaDevices.MediaDevice`

Begge er "steder med filer" — de har et navn, en kapacitet, ledig plads, og en rod man kan traverse fra. Men deres API'er er helt forskellige.

Der mangler en **fælles, letvægtsrepræsentation** som:
- Kan præsenteres for brugeren i en liste over valgbare kilder
- Kan bruges til at matche en `BackupPlan.SourcePath` mod den rigtige enhed
- Bærer metadata nok til at brugeren kan træffe et valg
- **IKKE** indeholder traversal-logik, session management, eller andre tunge operationer

---

## 2. Designfilosofi: Top-down, ikke bottom-up

### Hvad vi lærte af Del 5-fejlen

Første forsøg på MTP Del 5 var en factory-tilgang:
```
SourceTraversalFactory → MediaDeviceTraversalFactory → MediaDeviceTraversal
```

Problemet: vi byggede **nedefra og op**. Factory'en skulle oprette en session og et traversal, men hvem ejer session lifetime? Løsningen blev at gøre `MediaDeviceTraversal` disposable — hvilket modsiger den tidligere beslutning om at `ISourceTraversal` **ikke** skal være disposable.

**Konklusion:** Man skal designe oppefra og ned. Først definere hvad brugeren/calleen ser, derefter hvordan det opdages, og til sidst hvordan det traverseres.

### Løsningen: IFileStore

`IFileStore` er en **lille metadata-wrapper**. Ikke mere. Den løser et afgrænset problem: "hvordan repræsenterer jeg en valgbare filenhed på tværs af filesystem og MTP?"

Den er IKKE:
- En traversal
- Et root directory
- En session/connection
- En storage platform
- En provider/service

---

## 3. IFileStore — interfacet

### Designregler

1. **Kun metadata** — ingen metoder, kun properties
2. **Immutable snapshot** — værdier bestemmes ved oprettelse, ingen live-opslag i getters
3. **Valgbart objekt** — repræsenterer kun aktuelt opdagede, brugbare stores
4. **Ingen traversal-logik** — `Open()`, `CreateTraversal()`, `GetRootDirectory()` hører ikke til her
5. **Tynd wrapper** — den underliggende platformstype gemmes internt, men eksponeres ikke gennem interfacet

### Interfacet

```csharp
internal interface IFileStore
{
    /// <summary>
    /// Stable identity for this file store.
    /// Currently identical to <see cref="StorePathPrefix"/>.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Short name for this file store.
    /// Examples: "C:" or "Pixel 7".
    /// </summary>
    string Name { get; }

    /// <summary>
    /// User-friendly display name.
    /// Examples: "Local Disk (C:)" or "Google Pixel 7 (Internal Storage)".
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// The underlying source type for this file store.
    /// </summary>
    BackupSourceType SourceType { get; }

    /// <summary>
    /// Canonical path prefix that identifies this file store.
    /// Examples: "C:\" or "mtp://Pixel 7/Internal Storage".
    /// </summary>
    string StorePathPrefix { get; }

    /// <summary>
    /// Total capacity in bytes, if known.
    /// </summary>
    ulong? TotalSize { get; }

    /// <summary>
    /// Available free space in bytes, if known.
    /// </summary>
    ulong? AvailableFreeSpace { get; }
}
```

### Diskussion af hvert medlem

#### `Id`
- **Hvad:** Maskinel identitet, stabil og kanonisk
- **Hvorfor:** `Name` og `DisplayName` er ikke unikke nok — to MTP-enheder kan have samme friendly name
- **Beslutning:** `Id = StorePathPrefix`. Én kilde til sandhed. Ingen separat ID-syntaks.
- **Begrundelse:** Undgår duplikat semantik, parsing-problemer, og "to kilder til sandhed"-risiko.
- **Fremtid:** Hvis det senere viser sig nødvendigt med en separat intern nøgle (fx et device-GUID), kan de skilles ad. Interfacet har begge properties.

#### `Name`
- **Kort navn** til identifikation i UI og logs
- Eksempler: `"C:"`, `"Pixel 7"`
- For filesystem: `DriveInfo.Name.TrimEnd('\\')`
- For MTP: `MediaDevice.FriendlyName`

#### `DisplayName`
- **Brugervenligt navn** til præsentation for brugeren
- Eksempler: `"Local Disk (C:)"`, `"Google Pixel 7 (Internal Storage)"`
- For filesystem: `"{VolumeLabel} ({Name})"` med fallback til `Name` hvis VolumeLabel er tom
- For MTP: `"{deviceName} ({storageName})"`

#### `SourceType`
- Diskriminator: `BackupSourceType.FileSystem` eller `BackupSourceType.MediaDevice`
- Gør det muligt at dispatche til korrekt traversal uden `is`/`as` checks

#### `StorePathPrefix`
- **Det vigtigste felt i interfacet.**
- Den kanoniske sti der identificerer denne file store som udgangspunkt for traversal.
- Eksempler: `"C:\"`, `"mtp://Pixel 7/Internal Storage"`
- **Bruges til:**
  1. Prefix-match mod `BackupPlan.SourcePath` for at finde hvilken `IFileStore` der matcher
  2. Udledning af relativ sti under store'en (til traversal)
  3. Stabil identitet (via `Id`)
- **Krav:** Skal være strengt kanonisk — ens format hver gang, ingen tilfældig variation

#### `TotalSize` / `AvailableFreeSpace`
- `ulong?` — null betyder "ukendt / ikke tilgængelig"
- For filesystem: læses fra `DriveInfo` (defensivt via try/catch)
- For MTP: modtages som argument i konstruktøren (hentet fra `MediaDriveInfo` eller `MediaStorageInfo` under discovery)
- **Vigtigt:** Aldrig 0 som fake-unknown — null er semantisk korrekt

### Hvad der IKKE er i IFileStore

| Feature | Fravalgt fordi |
|---------|---------------|
| `Open()` / `Connect()` | IFileStore er metadata, ikke en aktiv forbindelse |
| `CreateTraversal()` | Traversal er et separat lag, ikke en del af store'en |
| `GetRootDirectory()` | Store'en er ikke et directory, men en container |
| `EnumerateChildren()` | Det er traversals opgave |
| `Dispose()` | IFileStore ejer ingen resources — session ejes af `IOpenedSource` / `ISourceScope` |
| Platform-specifikke members | Den underliggende type er en intern implementation detail |

---

## 4. Wrappers

### 4.1 DriveInfoFileStore

**Formål:** Tynd adapter over `System.IO.DriveInfo`.

```csharp
internal sealed class DriveInfoFileStore : IFileStore
{
    private readonly DriveInfo _drive;

    public DriveInfoFileStore(DriveInfo drive)
    {
        // Pre-computer alle snapshot-værdier i konstruktøren
        Name = BuildName(drive);
        DisplayName = BuildDisplayName(drive);
        StorePathPrefix = BuildStorePathPrefix(drive);
        Id = StorePathPrefix;
        TotalSize = TryGetUInt64(() => (ulong)drive.TotalSize);
        AvailableFreeSpace = TryGetUInt64(() => (ulong)drive.AvailableFreeSpace);
    }

    internal DriveInfo DriveInfo => _drive; // Kun til internt brug i næste lag
}
```

**Designbeslutninger:**

| Beslutning | Begrundelse |
|-----------|-------------|
| `Name` = `drive.Name` trimmed for separator | Kort, entydigt, matcher Explorer-visning |
| `DisplayName` = `"{VolumeLabel} ({Name})"` | Mest informative for brugeren |
| `StorePathPrefix` = `drive.RootDirectory.FullName` | Inkluderer trailing separator, kanonisk for .NET |
| `TryGetUInt64` for størrelser | `DriveInfo` kaster exceptions på nogle drevtyper (cd-rom, netværk) |
| `internal DriveInfo` property | Næste lag har brug for den underliggende type til traversal |

### 4.2 MediaDeviceFileStore

**Formål:** Tynd adapter over `MediaDevices.MediaDevice` + en konkret storage root.

**Vigtig beslutning:** `MediaDeviceFileStore` repræsenterer **device + storage root** (fx "Pixel 7 / Internal Storage"), ikke hele device'et. Det giver den rigtige granularitet for `StorePathPrefix`.

```csharp
[SupportedOSPlatform("windows7.0")]
internal sealed class MediaDeviceFileStore : IFileStore
{
    private readonly MediaDevice _device;

    public MediaDeviceFileStore(
        MediaDevice device,
        string storageName,           // "Internal Storage" eller "SD Card"
        ulong? totalSize,             // Snapshot fra discovery
        ulong? availableFreeSpace)    // Snapshot fra discovery
    {
        // Pre-computer alle snapshot-værdier i konstruktøren
        Name = device.FriendlyName;
        DisplayName = BuildDisplayName(device.FriendlyName, storageName);
        StorePathPrefix = BuildStorePathPrefix(device.FriendlyName, storageName);
        Id = StorePathPrefix;
        StorageName = storageName;
        TotalSize = totalSize;
        AvailableFreeSpace = availableFreeSpace;
    }

    internal MediaDevice Device => _device;      // Kun til internt brug i næste lag
    internal string StorageName { get; }          // Kun til internt brug i næste lag
}
```

**Designbeslutninger:**

| Beslutning | Begrundelse |
|-----------|-------------|
| Storage metadata som argumenter, ikke live-opslag | Wrapperen er et passivt snapshot — discovery-laget henter metadata |
| `MediaDevice` gemmes internt | Næste lag skal bruge den til at åbne session |
| `StorageName` gemmes internt | Næste lag skal vide hvilket storage root der traverseres |
| `TotalSize`/`AvailableFreeSpace` kan være `null` | Hvis metadata er upålideligt, er null bedre end opdigtede værdier |
| `[SupportedOSPlatform("windows7.0")]` | MediaDevices library kræver Windows |

### Hvorfor `MediaDeviceFileStore` ikke selv kalder `Connect()` / `GetDrives()`

1. **Wrapperen er et passivt snapshot.** Den skal kunne oprettes uden at forbinde til device'et.
2. **Discovery-laget har allerede forbindelsen.** Det er mere effektivt at hente metadata i én tur.
3. **Separation of concerns.** Hvis metadata-hentning ændrer sig (fx fra `MediaDriveInfo` til `MediaStorageInfo`), skal kun discovery-laget opdateres.

---

## 5. Traversal-modellen: IFileStore + relativ sti

### Hvordan traversal starter fra en IFileStore

Traversal starter **ikke** fra `IFileStore` alene. Den starter fra:

```
IFileStore + en relativ startsti
```

Processen:

1. **`BackupPlan.SourcePath`** indeholder den fulde sti, fx:
   - `C:\Users\Jesper\Pictures`
   - `mtp://Pixel 7/Internal Storage/DCIM/Camera`

2. **Match mod `IFileStore.StorePathPrefix`** for at finde hvilken store der matcher:
   - `C:\` ← matcher `C:\Users\Jesper\Pictures`
   - `mtp://Pixel 7/Internal Storage` ← matcher `mtp://Pixel 7/Internal Storage/DCIM/Camera`

3. **Udled relativ sti** under store'en:
   - Filesystem: `Users\Jesper\Pictures`
   - MTP: `DCIM/Camera`

4. **Traversal får:** `IFileStore` (til at åbne session) + relativ sti (hvor at starte)

Denne model passer med at brugeren kan vælge et hvilket som helst subdirectory under store'en — ikke kun roden.

### Hvorfor `StorePathPrefix` er vigtigere end en `Open()`-metode

`StorePathPrefix` er en **streng**, ikke en metode. Det betyder:
- Den kan matches, sammenlignes, gemmes, logges
- Den kræver ingen forbindelse eller resource
- Den gør interfacet rent og testbart
- Den kan bruges til prefix-match uden at åbne enheden

---

## 6. Discovery-modellen (fremtidig)

`IFileStore` oprettes af et discovery-lag. Dette lag:

1. Lister alle relevante kilder (drives + MTP-enheder)
2. For hver kilde, afgør om den er "brugbar nok" til at blive en `IFileStore`
3. Henter metadata (size, free space, navn)
4. Opretter `DriveInfoFileStore` eller `MediaDeviceFileStore` med snapshot-data

Discovery-laget er **ikke** en del af `IFileStore`-designet. Det vil senere blive til én eller flere services:
- `IDriveDiscovery` — returnerer `IReadOnlyList<IFileStore>` for filesystem
- `IMediaDeviceDiscovery` — returnerer `IReadOnlyList<IFileStore>` for MTP
- Evt. en kombineret service

Men det er en separat beslutning. Først `IFileStore`.

---

## 7. Hvorfor vi IKKE gør følgende

### 7.1 IFileStore er ikke disposable

`IFileStore` er et snapshot — den ejer ingen native resources, åbne forbindelser, eller locks. `DriveInfo` og `MediaDevice` referencer opbevares kun til internt brug i næste lag. Session-lifetime håndteres af `IOpenedSource` / `ISourceScope` (separat abstraktion, ikke besluttet endnu).

### 7.2 IFileStore har ikke traversal-metoder

Hvis `IFileStore` havde `CreateTraversal()`, ville den skulle kende til `IMtpGatekeeper`, session management, og traversal-konfiguration. Det er for meget ansvar. Traversal er et separat lag der forbruger `IFileStore`.

### 7.3 IFileStore repræsenterer ikke ikke-brugbare enheder

Discovery-laget filtrerer. Hvis et device har tom `FriendlyName`, eller en storage root ikke kan identificeres, bliver det bare ikke til en `IFileStore`. Wrapperen skal ikke "redde" dårlige data.

### 7.4 Id er ikke et separat format

`Id = StorePathPrefix` — ikke `mtp:{friendlyName}:{storageName}`. Undgår:
- Escaping/parsing-problemer med `:`, `/`, og specialtegn
- Duplikat semantik
- To kilder til sandhed

---

## 8. Designregler (gør det nemt at sige nej)

| Regel | Begrundelse |
|-------|-------------|
| `IFileStore` må kun have property-medlemmer | Metoder tilføjer adfærd, hvilket gør interfacet tungere og sværere at implementere korrekt |
| Wrappers skal pre-compute i konstruktøren | Ingen live-opslag i getters. Gør objekterne stabile og forudsigelige |
| `Id = StorePathPrefix` som default | Én kilde til sandhed. Kan skilles ad senere hvis nødvendigt |
| `ulong?` for størrelser, aldrig 0 | 0 er en gyldig størrelse. Null betyder "ukendt" |
| Try/catch om DriveInfo property-læsning | DriveInfo kaster IOException for CD-rom, netværksdrev, etc. |
| MTP-størrelser ind fra discovery, ikke fra wrapper | Wrapperen laver ikke device-opslag |
| Kun connected/brugbare stores bliver til IFileStore | Ingen "måske-brugbare" objekter |
| Intern `DriveInfo`/`MediaDevice`-reference er OK | Næste lag skal bruge den — men den er ikke en del af interfacet |

---

## 9. Referencer

- `BMTP3.Core4\Storage\IFileStore.cs` — interfacet
- `BMTP3.Core4\Storage\DriveInfoFileStore.cs` — filesystem wrapper
- `BMTP3.Core4\Storage\MediaDeviceFileStore.cs` — MTP wrapper
- `BMTP3.Core\BackupSource\` — tidligere forsøg på samme abstraktion (BackupJob, IBackupSource, SourceType)
- `mangler.md` — overblik over hvad der mangler i Core4
- `plan.md` — overordnet plan for Core4

### Arkitekturmæssige forgængere

- `BackupJob` + `DriveBackupJob` / `DeviceBackupJob` i Core — samme idé, men bundet til config og handler-arkitektur
- `IBackupSource` (Core) — tomt interface, aldrig implementeret. Starten på samme tanke
- `SourceType` (Core) — `enum { Device, Drive }`. Genbrugt som `BackupSourceType` i Core4
