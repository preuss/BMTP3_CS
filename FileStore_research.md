# IBackupDriveInfo — Research & Architectural Decisions

> **Dato:** 7 Jun 2026
> **Formål:** Dokumentere designet af `IBackupDriveInfo` — en fælles metadata-wrapper over `DriveInfo` (filesystem) og `MediaDevice`/`MediaDriveInfo` (MTP). Sikre at fremtidige beslutninger træffes med fuld forståelse af tidligere overvejelser. Tidligere kendt som `IFileStore` (redesignet efter review).

---

## 1. Problembeskrivelse

Core4 har to fundamentalt forskellige source-typer:
- **FileSystem** — lokale/network drives, repræsenteret ved `System.IO.DriveInfo`
- **MediaDevice** — MTP-enheder (telefoner, kameraer), repræsenteret ved `MediaDevices.MediaDevice` + `MediaDriveInfo`

Begge er "steder med filer" — de har et navn, en kapacitet, ledig plads, og en rod man kan traverse fra. Men deres API'er er helt forskellige.

Der mangler en **fælles, letvægtsrepræsentation** som:
- Kan præsenteres for brugeren i en liste over valgbare kilder
- Kan bruges til at matche en `BackupPlan.SourcePath` mod den rigtige enhed
- Bærer metadata nok til at brugeren kan træffe et valg
- **IKKE** indeholder traversal-logik, session management, eller andre tunge operationer

---

## 2. Interface-hierarki

### 2.1 Base: IBackupDriveInfo

Fælles properties for alle source-typer.

```csharp
internal interface IBackupDriveInfo
{
    string Id { get; }
    string DriveName { get; }
    string DisplayName { get; }
    BackupSourceType SourceType { get; }
    string RootPath { get; }
    long TotalSize { get; }
    long AvailableFreeSpace { get; }
}
```

### 2.2 Filesystem: IBackupFileSystemDriveInfo

```csharp
internal interface IBackupFileSystemDriveInfo : IBackupDriveInfo
{
    string VolumeLabel { get; }
    string DriveFormat { get; }
    DriveType DriveType { get; }
}
```

### 2.3 MTP: IBackupMediaDriveInfo

```csharp
internal interface IBackupMediaDriveInfo : IBackupDriveInfo
{
    string DeviceId { get; }
    string FriendlyName { get; }
    string? Description { get; }
    string? Manufacturer { get; }
    string? Model { get; }
    string? SerialNumber { get; }
}
```

### Designbeslutninger

### Id vs RootPath

Tidligere (`IFileStore`) var `Id = StorePathPrefix`. Det er ændret:

| Felt | Filesystem | MTP |
|------|-----------|-----|
| `Id` | `"C:"` | `"Apple iPad/Internal Storage"` |
| `RootPath` | `"C:\"` | `"mtp://Apple iPad/Internal Storage"` |

**`Id` er til intern matching** — ren og uden scheme-præfiks.
**`RootPath` er til traversal** — fuld sti med scheme.

### long frem for ulong?

`TotalSize`/`AvailableFreeSpace` er `long`, ikke `ulong?`. Begrundelse:
- `DriveInfo.TotalSize` og `MediaDriveInfo.TotalSize` er begge `long` — direkte assignment, intet cast
- Nullable giver ikke mening: en drive der er klar har altid en størrelse
- Fail-first: hvis data er utilgængeligt, kaster discovery-laget — wrapperen tier ikke stille

### DriveName for MTP

MTP `DriveName` bruger `MediaDriveInfo.Name.TrimStart('\\')` (fx `\Internal Storage` → `Internal Storage`). `Name` er altid sat af MTP-protokollen (`StorageID`), i modsætning til `VolumeLabel` som kan være tom på mange Android-enheder. `TrimStart('\\')` fjerner det leading separator som MTP tilføjer.

---

## 3. Implementations

### 3.1 BackupFileSystemDriveInfo

```csharp
internal sealed class BackupFileSystemDriveInfo : IBackupFileSystemDriveInfo
{
    private readonly DriveInfo _drive;

    public BackupFileSystemDriveInfo(DriveInfo drive)
    {
        _drive = drive ?? throw new ArgumentNullException(nameof(drive));
        if(!drive.IsReady)
            throw new InvalidOperationException($"Drive '{drive.Name}' is not ready.");

        DriveName = BuildDriveName(drive);
        DisplayName = BuildDisplayName(drive);
        RootPath = BuildRootPath(drive);
        Id = DriveName;

        VolumeLabel = drive.VolumeLabel;
        DriveFormat = drive.DriveFormat;
        DriveType = drive.DriveType;
        TotalSize = drive.TotalSize;
        AvailableFreeSpace = drive.AvailableFreeSpace;
    }
}
```

**Designbeslutninger:**

| Beslutning | Begrundelse |
|-----------|-------------|
| `IsReady` guard i konstruktør | Ægte fail-first — discovery skal ikke kunne oprette en wrapper for et ikke-klar drev |
| `long` direkte fra `DriveInfo` | Ingen unødvendig casting eller try/catch — `TotalSize`/`AvailableFreeSpace` er værdityper |
| `VolumeLabel`/`DriveFormat` som `string` (ikke `string?`) | `DriveInfo` returnerer aldrig null for disse |
| `Id = DriveName` | For filesystem er drevenavnet unikt nok som identitet |

### 3.2 BackupMediaDriveInfo

```csharp
[SupportedOSPlatform("windows7.0")]
internal sealed class BackupMediaDriveInfo : IBackupMediaDriveInfo
{
    private readonly MediaDevice _device;
    private readonly MediaDriveInfo _driveInfo;

    public BackupMediaDriveInfo(MediaDevice device, MediaDriveInfo driveInfo)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _driveInfo = driveInfo ?? throw new ArgumentNullException(nameof(driveInfo));

        DeviceName = BuildDeviceName(device);
        DriveName = BuildDriveName(driveInfo);
        DisplayName = BuildDisplayName(DeviceName, DriveName);
        RootPath = BuildRootPath(DeviceName, DriveName);
        Id = BuildId(DeviceName, DriveName);
        FriendlyName = device.FriendlyName;

        DeviceId = device.DeviceId;
        Description = device.Description;
        Manufacturer = device.Manufacturer;
        Model = device.Model;
        SerialNumber = device.SerialNumber;
        TotalSize = driveInfo.TotalSize;
        AvailableFreeSpace = driveInfo.AvailableFreeSpace;
    }
}
```

**Designbeslutninger:**

| Beslutning | Begrundelse |
|-----------|-------------|
| `(MediaDevice, MediaDriveInfo)` konstruktør | `MediaDriveInfo` bærer `TotalSize`, `AvailableFreeSpace`, `VolumeLabel`, `RootDirectory` — renere end 4 løse parametre |
| `DriveName = Name.TrimStart('\\')` | `Name` er altid sat af MTP (`StorageID`), i modsætning til `VolumeLabel` som kan være tom |
| `Id = "{FriendlyName}/{DriveName}"` | Unik identitet på tværs af devices — `DriveName` alene er ikke unik på tværs |
| Ingen try/catch nogen steder | Fail-first — discovery styrer præ-kvalitet |
| `MediaDevice` gemmes internt | Næste lag (traversal) skal bruge `Device` til at åbne session |

### Hvorfor `BackupMediaDriveInfo` ikke selv kalder `Connect()` / `GetDrives()`

1. **Wrapperen er et passivt snapshot.** Den skal kunne oprettes uden at forbinde til device'et.
2. **Discovery-laget har allerede forbindelsen.** Det er mere effektivt at hente metadata i én tur.
3. **Separation of concerns.** Hvis metadata-hentning ændrer sig, skal kun discovery-laget opdateres.

---

## 4. Traversal-modellen: IBackupDriveInfo + relativ sti

### Hvordan traversal starter fra en IBackupDriveInfo

Traversal starter **ikke** fra `IBackupDriveInfo` alene. Den starter fra:

```
IBackupDriveInfo + en relativ startsti
```

Processen:

1. **`BackupPlan.SourcePath`** indeholder den fulde sti, fx:
   - `C:\Users\Jesper\Pictures`
   - `mtp://Apple iPad/Internal Storage/DCIM/Camera`

2. **Match mod `IBackupDriveInfo.RootPath`** for at finde hvilken store der matcher:
   - `C:\` ← matcher `C:\Users\Jesper\Pictures`
   - `mtp://Apple iPad/Internal Storage` ← matcher `mtp://Apple iPad/Internal Storage/DCIM/Camera`

3. **Udled relativ sti** under store'en:
   - Filesystem: `Users\Jesper\Pictures`
   - MTP: `DCIM/Camera`

4. **Traversal får:** `IBackupDriveInfo` (til at åbne session) + relativ sti (hvor at starte)

### Hvorfor `RootPath` er vigtigere end en `Open()`-metode

`RootPath` er en **streng**, ikke en metode. Det betyder:
- Den kan matches, sammenlignes, gemmes, logges
- Den kræver ingen forbindelse eller resource
- Den gør interfacet rent og testbart
- Den kan bruges til prefix-match uden at åbne enheden

---

## 5. Discovery-modellen (fremtidig)

`IBackupDriveInfo` oprettes af et discovery-lag. Dette lag:

1. Lister alle relevante kilder (drives + MTP-enheder)
2. For hver kilde, filtrerer ikke-klar drev (fail-first i konstruktør)
3. Opretter `BackupFileSystemDriveInfo` eller `BackupMediaDriveInfo`

Discovery-laget er **ikke** en del af `IBackupDriveInfo`-designet. Det vil senere blive til én eller flere services:
- `IFileSystemSourceDiscovery` — returnerer `IReadOnlyList<IBackupDriveInfo>` for filesystem
- `IMtpDeviceDiscovery` — returnerer `IReadOnlyList<IBackupDriveInfo>` for MTP
- `ICombinedSourceDiscovery` — begge

Men det er en separat beslutning. Først `IBackupDriveInfo`.

### Åbne spørgsmål — discovery

Følgende spørgsmål er **ikke besluttet endnu** og afventer afklaring:

| # | Spørgsmål | Noter |
|---|-----------|-------|
| 1 | **Flere drives pr. device** — skal et device med både Internal Storage og SD Card give én eller to `IBackupDriveInfo`? | Hælder til én per `MediaDriveInfo` (to IBackupDriveInfo). |
| 2 | **Discovery-flow** — connect → hent `MediaDriveInfo[]` → disconnect (mister `MediaDevice` reference), eller hold forbindelsen? | Hvis vi disconnecter, har vi ikke `MediaDevice` til senere session-opening. |
| 3 | **MTP `RootPath` escaping** — skal `mtp://{FriendlyName}/{VolumeLabel}` escape specialtegn/mellemrum? | `FriendlyName` kan indeholde mellemrum. Raw strings eller normaliseret? |

---

## 6. Hvorfor vi IKKE gør følgende

### 6.1 IBackupDriveInfo er ikke disposable

`IBackupDriveInfo` er et snapshot — den ejer ingen native resources, åbne forbindelser, eller locks. `DriveInfo` og `MediaDevice` referencer opbevares kun til internt brug i næste lag. Session-lifetime håndteres af `IOpenedSource` / `ISourceScope` (separat abstraktion, ikke besluttet endnu).

### 6.2 IBackupDriveInfo har ikke traversal-metoder

Hvis `IBackupDriveInfo` havde `CreateTraversal()`, ville den skulle kende til `IMtpGatekeeper`, session management, og traversal-konfiguration. Det er for meget ansvar. Traversal er et separat lag der forbruger `IBackupDriveInfo`.

### 6.3 IBackupDriveInfo repræsenterer ikke ikke-brugbare enheder

Discovery-laget filtrerer. Hvis et device har tom `FriendlyName`, eller `VolumeLabel` mangler, kaster konstruktøren — og discovery vælger bare at springe den over. Wrapperen skal ikke "redde" dårlige data.

### 6.4 ulong? blev forkastet

Tidligere design (`IFileStore`) brugte `ulong?` for at signalere "ukendt størrelse". Erfaringen viste:
- `DriveInfo.TotalSize` er aldrig null, og undtagelser skal propageres (fail-first)
- `MediaDriveInfo.TotalSize` er aldrig null, og undtagelser skal propageres (fail-first)
- `long` er den naturlige type for både `DriveInfo` og `MediaDriveInfo`
- Nullable størrelser giver en falsk fornemmelse af "det er ok hvis vi ikke ved det" — det er det ikke

---

## 7. Designregler (gør det nemt at sige nej)

| Regel | Begrundelse |
|-------|-------------|
| `IBackupDriveInfo` må kun have property-medlemmer | Metoder tilføjer adfærd, hvilket gør interfacet tungere og sværere at implementere korrekt |
| Wrappers skal pre-compute i konstruktøren | Ingen live-opslag i getters. Gør objekterne stabile og forudsigelige |
| `Id` ≠ `RootPath` | `Id` er til intern matching (rent, uden scheme); `RootPath` er til traversal (fuld sti) |
| `long` for størrelser, aldrig `ulong?` | Source-typen returnerer altid `long`; null er en falsk "det ved vi ikke" |
| Fail-first i konstruktør | `IsReady` guard for filesystem; `Name` krav for MTP — ingen silent skipping |
| MTP `Name.TrimStart('\\')` som `DriveName` | `Name` er altid sat (`StorageID`), `VolumeLabel` kan være tom på Android |
| Kun connected/brugbare stores bliver til IBackupDriveInfo | Ingen "måske-brugbare" objekter |
| Intern `DriveInfo`/`MediaDevice`-reference er OK | Næste lag skal bruge den — men den er ikke en del af interfacet |

---

## 8. Arkitekturhistorik

### IFileStore → IBackupDriveInfo

Det tidligere design (`IFileStore`) havde:
- `ulong?` for `TotalSize`/`AvailableFreeSpace`
- `TryGetUInt64` try/catch wrapper
- `Id = StorePathPrefix`
- `FileSystemFileStore` / `MediaDeviceFileStore` navne
- Ét enkelt interface uden arv
- `string? VolumeLabel` / `string? DriveFormat`

Efter review blev det erstattet med det nuværende `IBackupDriveInfo`-design. De væsentligste ændringer:

| Før (IFileStore) | Efter (IBackupDriveInfo) | Begrundelse |
|------------------|--------------------------|-------------|
| `ulong?` | `long` | Værdityper, aldrig null — `ulong?` var over-engineering |
| `TryGetUInt64` | Direkte assignment | Fail-first — undtagelser propagerer |
| `Id = StorePathPrefix` | `Id` ≠ `RootPath` | Forskellige formål — `Id` til matching, `RootPath` til traversal |
| Ét interface | Interface-hierarki (base + specialized) | Consumers kan vælge abstraktionsniveau |
| `string? VolumeLabel` | `string VolumeLabel` | `DriveInfo.VolumeLabel` er aldrig null |
| Ingen konstruktør-guard | `IsReady` guard + `Name` krav | Ægte fail-first |
| `FileSystemFileStore` | `BackupFileSystemDriveInfo` | Navnet reflekterer formålet bedre |

---

## 9. Referencer

- `BMTP3.Core4\Storage\IBackupDriveInfo.cs` — base interface ✅
- `BMTP3.Core4\Storage\IBackupFileSystemDriveInfo.cs` — filesystem-specifikt interface ✅
- `BMTP3.Core4\Storage\IBackupMediaDriveInfo.cs` — MTP-specifikt interface ✅
- `BMTP3.Core4\Storage\BackupFileSystemDriveInfo.cs` — filesystem implementation ✅
- `BMTP3.Core4\Storage\BackupMediaDriveInfo.cs` — MTP implementation ✅
- `BMTP3.Core\BackupSource\` — tidligere forsøg på samme abstraktion (BackupJob, IBackupSource, SourceType)
- `mangler.md` — overblik over hvad der mangler i Core4
- `plan.md` — overordnet plan for Core4

### Arkitekturmæssige forgængere

- `BackupJob` + `DriveBackupJob` / `DeviceBackupJob` i Core — samme idé, men bundet til config og handler-arkitektur
- `IBackupSource` (Core) — tomt interface, aldrig implementeret. Starten på samme tanke
- `SourceType` (Core) — `enum { Device, Drive }`. Genbrugt som `BackupSourceType` i Core4
- `IFileStore` (Core4, forkastet) — første forsøg på `ulong?` + `TryGetUInt64` + `Id = StorePathPrefix`. Erstattet af `IBackupDriveInfo` efter review.

---

## 10. Epilog — arkitekturændringer efter research dokumentet

Dette dokument beskrev `IBackupDriveInfo` og den oprindelige plan for traversal. Siden da er følgende ændret:

### 10.1 Traversal får ikke længere `IBackupDriveInfo`

Planen i §4 lød: "Traversal får: IBackupDriveInfo + relativ sti". I stedet får traversal nu **`IConnectedSource`** (via `ISourceTraversalFactory.Create(IConnectedSource)`). `IBackupDriveInfo` bruges kun op til `SourceConnector.Connect()` — derefter er al information tilgængelig via `IConnectedSource` subtypes.

### 10.2 `IOpenedSource`/`ISourceScope` blev ikke implementeret

§6.1 nævnte `IOpenedSource`/`ISourceScope` som en fremtidig abstraktion. Den blev aldrig implementeret — i stedet:
- `IConnectedSource : ISession` (hvor `ISession` kun har `Name`)
- `SourceConnector.Connect()` returnerer `ConnectedFileSystemSource` eller `ConnectedMediaDriveSource`
- `IConnectedMediaDriveSource` bærer både `IMediaDevice Device` + `IMediaDrive Drive`
- `SourceTraversalFactory` pattern-matches på runtime-typen og injecter `IMediaDeviceGatekeeper`

### 10.3 Discovery-spørgsmål besvaret

| # | Spørgsmål | Svar |
|---|-----------|------|
| 1 | Flere drives pr. device — én eller to IBackupDriveInfo? | Én per `MediaDriveInfo` (to `IBackupDriveInfo`) |
| 2 | Discovery-flow — disconnect eller hold forbindelse? | Discovery disconnecter. `SourceConnector` genfinder device via `DeviceId` i `MediaDeviceDriveProvider` |
| 3 | MTP `RootPath` escaping? | Stadig åbent — ikke implementeret endnu |

### 10.4 Relevant nu

- `IBackupDriveInfo`-designet er stadig korrekt og uændret
- `SourceConnector.cs` + `SourceTraversalFactory.cs` + `MediaDeviceTraversal.cs` er de centrale filer der implementerer forbindelsen mellem `IBackupDriveInfo` og traversal
- `MediaDeviceDriveProvider` opretter `BackupMediaDriveInfo(device, drive)` — stadig relevant
