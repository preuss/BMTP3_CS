# Task: MTP Source Path Format — Research & 3 Approaches

> **Afgørelse:** Formatet er `mtp://{deviceName}/{driveName}/{directoryPath}`.
> `directoryPath` = path-element i URI'en, IKKE relativ.
> Filesystem: `absoluteSourceDirectoryPath` (f.eks. `C:\temp\source`).
> **`relativeDirectoryPath`** = hvad traversal producerer relativt til source root.
> CLI/Caller sender den fulde `mtp://` URI + `BackupSourceType.MediaDevice`.
> `MtpUriParser.Parse()` splitter i `deviceName`, `driveName`, `directoryPath`.

> Hvordan skal CLI options map til `BackupPlan.SourcePath` for MTP (`mtp://` URI)?

---

## 1. Baggrund — hvordan gør de andre projekter?

### Oversigt over MTP path handling på tværs af alle projekter

| Projekt | MTP URI format | Authority | Escaping | MTP traversal |
|---------|---------------|-----------|----------|---------------|
| **Core** (original) | Ingen `mtp://` URI — raw MTP paths | `MediaDevice.FriendlyName` | Ingen | Inline i BackupHandler, fuld materialisering |
| **Core2** | `mtp://{deviceId}/{segments}` | `device.DeviceId` (GUID, maskinstabil) | `Uri.EscapeDataString()` per segment | `MediaDeviceScanner` + gatekeeper |
| **Core3** | Ingen MTP support | N/A | N/A | N/A |
| **Core4** | `mtp://{FriendlyName}/{driveName}/{path}` | `device.FriendlyName` (læsbar) | Raw/unescaped | `MediaDeviceTraversal` + gatekeeper + abstraktioner |
| **Common** | Ingen MTP | N/A | N/A | N/A |
| **MessageFormatter** | Ingen MTP | N/A | N/A | N/A |

### Core2 — detaljer

**`PathNormalizer.NormalizeMtpUri(string path, string deviceId)`** (Utility, ubrugt i produktion):
```csharp
string escapedDevice = Uri.EscapeDataString(deviceId);
// path: replace \ → /, trim /, split, resolve . og .., NFC-normalize, escape hver segment
return $"mtp://{escapedDevice}/{string.Join('/', escapedSegments)}";
```

**`BackupScanner.ScanMediaDeviceAsync`** (inline URI construction, linje 185-189):
```csharp
string mtpUrl = $"mtp://{Uri.EscapeDataString(device.DeviceId)}/{string.Join('/', segments)}";
// Bruger device.DeviceId (GUID), IKKE FriendlyName
```

**CLI flow (Core2):**
1. `--source-device "Apple iPhone"` → `plan.SourceId = "Apple iPhone"`, `plan.SourceType = MediaDevice`
2. `plan.SourcePath` forbliver `string.Empty` (MTP starter altid fra root)
3. `BackupScanner.OpenSession(plan)` matcher `plan.SourceId` mod `MediaDevice.FriendlyName`
4. Scanning bruger `plan.SourcePath` (tom = root) som startpunkt

**Vigtig forskel:** Core2 bruger `device.DeviceId` (GUID) som URI authority, IKKE FriendlyName. CLI'en bruger FriendlyName kun til device-matching.

### Core4 — detaljer

**`BackupMediaDriveInfo.BuildRootPath`:**
```csharp
private static string BuildRootPath(string friendlyName, string driveName)
    => $"mtp://{friendlyName}/{driveName}";
// Eksempel: "mtp://Apple iPad/Internal Storage"
// INGEN escaping
```

**`MtpUriParser.Parse`** (parser, ubrugt i produktion):
```csharp
// Forventer: "mtp://DeviceName/Path/To/Item"
// Split på første / efter scheme → DeviceName + DevicePath
// INGEN unescaping — forventer raw strings
```

**`BackupEngine.MatchDrive`** (BUG — ødelægger MTP URIs):
```csharp
sourcePath = sourcePath.Replace("/", "\\");  // Muterer sourcePath!
// "mtp://Apple iPad/Internal Storage" → "mtp:\\Apple iPad\\Internal Storage"
// Matcher aldrig drive.RootPath som bruger /
```

**CLI flow (Core4, nuværende):**
1. `--source-device "Apple iPhone"` → `sourcePath = "Apple iPhone"` (kun device navn, intet `mtp://`)
2. `ValidateBackupOptions` kræver `--source-directory`, så MTP-only backup fejler
3. `MatchDrive` får `"Apple iPhone"` — matcher intet

### Core (original) — detaljer

- Ingen `mtp://` URI koncept overhovedet
- `SourceType` = `Device` eller `Drive`
- MTP paths = raw `MediaFileInfo.FullName` (f.eks. `\Internal Storage\DCIM\IMG_001.jpg`)
- Device identificeres via `MediaDevice.FriendlyName` direkte

### Konklusion på research

**Core2** og **Core4** har hver deres `mtp://` URI format:
- Core2: `mtp://{escapedDeviceId}/{escapedSegments}` — maskinorienteret, GUID som authority
- Core4: `mtp://{FriendlyName}/{driveName}/{path}` — brugerorienteret, FriendlyName som authority

### Besluttet format

```
mtp://{deviceName}/{driveName}/{directoryPath}
```

- `deviceName` = `FriendlyName` (f.eks. `"Apple iPad"`)
- `driveName` = storage navn (f.eks. `"Internal Storage"`)
- `directoryPath` = path-element i URI'en (f.eks. `"DCIM/Camera"`)
  - **Kun directory** — IKKE relativ, men et absolut path-element inden for device-hierarkiet
  - `relativeDirectoryPath` er hvad traversal **producerer** relativt til source root

**Eksempler:**
- Hele devicet: `mtp://Apple iPad/Internal Storage` (directoryPath = tom)
- Sub-folder: `mtp://Apple iPad/Internal Storage/DCIM/Camera` (directoryPath = `"DCIM/Camera"`)
- SD kort: `mtp://Canon Camera/SD Card/DCIM/100CANON`

**Sammenligning med filesystem:**
| Koncept | Filesystem | MTP |
|---------|-----------|-----|
| Source root (absolut) | `absoluteSourceDirectoryPath` = `C:\temp\source` | `mtp://{deviceName}/{driveName}/{directoryPath}` |
| Traversal output | `relativeDirectoryPath` = `2024\jan` | `relativeDirectoryPath` = `2024/jan` |
| Traversal output | `relativeFilePath` = `2024\jan\picture.jpg` | `relativeFilePath` = `2024/jan/picture.jpg` |

**Hvorfor `directoryPath` (ikke `relativeDirectoryPath`):**
- `mtp://` URI'en definerer et absolut startpunkt på devicet
- `directoryPath` er et path-element i URI strukturen, ikke en relativ sti
- `relativeDirectoryPath` reserveres til hvad traversal producerer (relativt til source root)
- Følger logik: filesystem `C:\temp\source` = absolut, MTP `mtp://device/drive/dir` = absolut

**Core4's format er det rigtige valg** for vores CLI fordi:
1. FriendlyName er hvad brugeren ser i `list-sources`
2. Brugeren skriver `--source-device "Apple iPad"` — det skal kunne genkendes
3. `MtpUriParser` forventer netop dette format

---

## 2. Tre tilgange

### Tilgang A: Konstruér `mtp://` URI i `BuildPlan` (CLI layer)

**Hvad:**
CLI/Caller sender den fulde `mtp://` URI som `SourcePath`:
```
--source-path "mtp://Apple iPad/Internal Storage/DCIM/Camera" --source-type MediaDevice
```

Eller hvis vi beholder `--source-device` + `--source-directory`:
`BuildPlan` konstruerer URI'en:
```
sourcePath = $"mtp://{deviceName}/{driveName}/{relativeDirectoryPath}"
```

1. `deviceName` fra `--source-device`
2. `driveName` + `directoryPath` fra `--source-directory` (første segment = drive navn)
3. Hvis `--source-directory` udelades: brug `list-sources` for at finde `RootPath`
4. Fjern `--source-directory` kravet i `ValidateBackupOptions` når `--source-device` er givet

```csharp
if (WasSupplied(parseResult, BackupOptionsModel4.SourceDeviceOption) && ...)
{
    sourceType = Core4BackupSourceType.MediaDevice;
    string deviceName = backupOptions.SourceDevice;
    string? directoryPath = backupOptions.SourceDirectory;
    sourcePath = string.IsNullOrWhiteSpace(directoryPath)
        ? $"mtp://{deviceName}"
        : $"mtp://{deviceName}/{directoryPath.TrimStart('/')}";
}
```

**Skal også fikses i `BackupEngine.MatchDrive`:**
- Fjern `sourcePath.Replace("/", "\\")` mutationen
- Normaliser både `sourcePath` og `drive.RootPath` til samme separator før sammenligning

**Fordele:**
- Enkelt og centraliseret — al URI-logik i CLI layer
- `BackupEngine` forventer allerede `mtp://` format i `SourcePath`
- `MtpUriParser` kan parse resultatet
- Let at teste — `BuildPlan` tests kan verificere URI konstruktion

**Ulemper:**
- CLI layer skal kende `mtp://` URI formatet
- Hvis formatet ændres, skal CLI opdateres
- `--source-directory` betyder noget andet for MTP end for filesystem (sub-path på device vs. absolut sti)
- Skal finde `RootPath` (drive navn) hvis `--source-directory` ikke er givet — kræver discovery

### Tilgang B: Behold CLI simpelt — parse i engine layer

**Hvad:**
CLI sender `SourceType=MediaDevice` + `SourcePath` = device name (rå). Engine layer (i `BackupEngine` eller ny `IMtpPathResolver`) står for:
1. Modtag `SourcePath` = device name + valgfri sti adskilt af `/` (f.eks. `Apple iPad/Internal Storage/DCIM`)
2. Lav `list-sources` for at finde `RootPath` (`mtp://Apple iPad/Internal Storage`)
3. Kombiner til fuld `mtp://` URI internt
4. Match drive og start traversal

```csharp
// I BackupEngine eller IMtpPathResolver
if (plan.SourceType == BackupSourceType.MediaDevice)
{
    string rawPath = plan.SourcePath; // "Apple iPad/Internal Storage/DCIM"
    MtpUriParseResult parsed = MtpUriParser.Parse($"mtp://{rawPath}");
    // Find drive der matcher parsed.DeviceName
    // Resolve sub-path
}
```

**Fordele:**
- CLI layer forbliver "dum" — sender kun brugerinput videre
- Al MTP URI logik samlet i engine layer
- Kan genbruge `MtpUriParser` (som i dag er ubrugt)
- `list-sources` kører i engine layer hvor discovery allerede findes

**Ulemper:**
- `BackupEngine` skal have adgang til `IDriveProvider` (har den allerede)
- `SourcePath` får blandet betydning: filesystem paths vs. MTP device names
- Skal stadig fikse `MatchDrive` bug (uafhængig af denne beslutning)
- Sværere at teste — kræver mock af discovery

### Tilgang C: To separate options, kombiner i CLI

**Hvad:**
Behold `--source-device` og `--source-directory` som separate options. `BuildPlan` kombinerer dem til én `mtp://` URI:
1. `--source-device "Apple iPad"` — påkrævet for MTP
2. `--source-directory "Internal Storage/DCIM"` — valgfri, default = roden af første fundne drive
3. `BuildPlan` slår `RootPath` op via `IDriveProvider` for at finde drive navnet

```csharp
if (WasSupplied(parseResult, BackupOptionsModel4.SourceDeviceOption) && ...)
{
    sourceType = Core4BackupSourceType.MediaDevice;
    string deviceName = backupOptions.SourceDevice;
    string subPath = backupOptions.SourceDirectory ?? string.Empty;
    
    // Find drive RootPath for denne device
    string rootPath = FindMediaDriveRootPath(deviceName); // f.eks. "mtp://Apple iPad/Internal Storage"
    
    sourcePath = string.IsNullOrWhiteSpace(subPath)
        ? rootPath
        : $"{rootPath}/{subPath.TrimStart('/')}";
}
```

**`FindMediaDriveRootPath`** kunne:
- Indsprøjte `IDriveProvider` i `BuildPlan`
- Eller være en separat service `IMtpDriveResolver`
- Cache resultatet så discovery kun kører én gang

**Fordele:**
- Renest separation — `--source-device` = device, `--source-directory` = sub-path
- Brugeren kan specificere præcist hvor på devicet
- Genbruger eksisterende `--source-directory` option
- `list-sources` viser `RootPath` → bruger kan kopiere `--source-directory` værdien

**Ulemper:**
- Kræver discovery i CLI layer (eller shared service)
- `--source-directory` har to betydninger: absolut sti for filesystem, relativ sti for MTP
- Mere kompleks `BuildPlan`
- Discovery kan fejle (device ikke tilsluttet) — skal håndteres pænt

---

## 3. Sammenfatning

| Aspekt | Tilgang A (CLI URI) | Tilgang B (Engine parse) | Tilgang C (To options) |
|--------|-------------------|------------------------|----------------------|
| **Kompleksitet** | Medium | Lav (CLI), Medium (Engine) | Høj |
| **CLI layer ændring** | `BuildPlan` + validator | Minimal | `BuildPlan` + evt. service |
| **Engine ændring** | Fix `MatchDrive` bug | Fix `MatchDrive` + ny resolver | Fix `MatchDrive` bug |
| **`MtpUriParser` genbrug** | Ja (parser resultat) | Ja (parser i engine) | Ja (parser resultat) |
| **Discovery i CLI** | Ja (hvis intet `--source-directory`) | Nej | Ja |
| **Testbarhed** | God | Medium | Kompleks |
| **Anbefaling** | ⭐ **Anbefales** | ⚠️ Mulig | ❌ For kompleks |

### 📌 Beslutning: Format = `mtp://{deviceName}/{driveName}/{directoryPath}`

- `directoryPath` = path-element i URI'en (absolut inden for device-hierarkiet)
- `relativeDirectoryPath` = hvad traversal producerer (relativt til source root)
- `MtpUriParser.Parse()` splitter URI'en i `deviceName`, `driveName`, `directoryPath`
- CLI/Caller sender den fulde `mtp://` URI + `SourceType=MediaDevice`
- `BackupEngine.MatchDrive` skal fikses (fjern `/`→`\\` mutation)
- Overvej escaping: `FriendlyName` kan indeholde specialtegn

### Open questions

| # | Spørgsmål | Status |
|---|-----------|--------|
| 1 | Skal `FriendlyName` URI-escapes i `mtp://` URI? | Core4's `MtpUriParser` forventer raw strings. Men `FriendlyName` kan indeholde `#`, `?`, `%`. **Ikke besluttet.** |
| 2 | Hvad hvis `directoryPath` indeholder `../` (path traversal)? | Skal valideres/normaliseres |
| 3 | Bør `--source-directory` for MTP inkludere drive navn (første segment) eller være ren sub-path? | `"Internal Storage/DCIM"` (inkl. drive navn) — så parseren kan validere at drive matcher |
| 4 | Hvordan håndteres devices med flere drives (Internal Storage + SD Card)? | `directoryPath` starter med drive navn → `"Internal Storage/..."` eller `"SD Card/..."` |

### Files to modify (Tilgang A)

| File | Ændring |
|------|---------|
| `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4.Helpers.cs` | Konstruér `mtp://` URI i `BuildPlan` |
| `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4.cs` | Fix `ValidateBackupOptions` — acceptér `--source-device` alene |
| `BMTP3.Consoles/ConsoleCommands/Core4/BackupOptionsModel4.cs` | Opdater `--source-directory` description for MTP |
| `BMTP3.Core4/Engine/BackupEngine.cs` (linje 743) | Fjern `sourcePath.Replace("/", "\\")` mutation i `MatchDrive` |
| `BMTP3.Core4/Engine/BackupEngine.cs` (`GetRelativeDirectoryPath`) | Normaliser separatorer før substring |

### Test cases (nye)

| Test | Forventet |
|------|-----------|
| `BuildPlan_WithSourceDeviceOnly_CreatesMtpUriWithDeviceName` | `SourcePath` = `"mtp://Apple iPad"` |
| `BuildPlan_WithSourceDeviceAndDirectory_CreatesFullMtpUri` | `SourcePath` = `"mtp://Apple iPad/Internal Storage/DCIM"` |
| `BuildPlan_WithSourceDeviceAndDirectoryWithoutLeadingSlash` | `SourcePath` = `"mtp://Apple iPad/Internal Storage/DCIM"` |
| `MtpUriParser_Parse_FullUri_ReturnsCorrectParts` | `DeviceName="Apple iPad"`, `DevicePath="Internal Storage/DCIM"` |
| `MatchDrive_WithMtpUri_MatchesCorrectDrive` | Finder `IBackupMediaDriveInfo` med `RootPath` = `"mtp://Apple iPad/Internal Storage"` |
| `MatchDrive_WithMtpUriAndSubPath_ReturnsRelativeDirectoryPath` | `GetRelativeDirectoryPath` = `"DCIM"` |

### Reference: nøglefiler

| Fil | Ansvarlig |
|-----|-----------|
| `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4.Helpers.cs:52-55` | Nuværende `--source-device` → `sourcePath` mapping (skal ændres) |
| `BMTP3.Core4/Engine/BackupEngine.cs:738-757` | `MatchDrive` (skal fikses) |
| `BMTP3.Core4/Engine/BackupEngine.cs:759-765` | `GetRelativeDirectoryPath` (skal muligvis fikses) |
| `BMTP3.Core4/Traversal/MtpUriParser.cs` | Eksisterende parser (kan genbruges) |
| `BMTP3.Core4/Storage/BackupMediaDriveInfo.cs:140-143` | `BuildRootPath` — format reference |
| `BMTP3.Core2/BackupNew/Utilities/PathNormalizer.cs:67-113` | Core2's escaping (reference) |
| `BMTP3.Core2/BackupNew/Engine/Traversal/BackupScanner.cs:185-189` | Core2's URI construction (reference) |
| `BMTP3.Core4.Tests/Traversal/MtpUriParserTests.cs` | Eksisterende parser tests |
