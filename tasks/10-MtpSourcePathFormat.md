# Task: MTP Source Path Format — Research & Implementation

> **Formål:** Fastlæg MTP URI format (`mtp://{deviceName}/{driveName}/{directoryPath}`) og implementer i BuildPlan, MatchDrive, MtpUriParser.

---

## 1. Besluttet arkitektur

### Format

```
mtp://{deviceName}/{driveName}/{directoryPath}
```

| Segment | Eksempel | Betydning |
|---------|----------|-----------|
| `deviceName` | `"Apple iPad"` | `MediaDevice.FriendlyName` |
| `driveName` | `"Internal Storage"` | Storage navn (`MediaDriveInfo.Name.TrimStart('\\')`) |
| `directoryPath` | `"DCIM/Camera"` | Path-element i URI efter `{deviceName}/{driveName}/` |

**Komplet URI:** `mtp://Apple iPad/Internal Storage/DCIM/Camera`

### Navnestandard

**`source`-prefix bruges KUN i source-kontekst** — `BackupPlan`, `BackupScanner`, traversal.

Discovery-laget (`IBackupDriveInfo`) er pre-source og har IKKE `source`:

| Lag | Property | Indhold |
|-----|----------|---------|
| Discovery | `IBackupDriveInfo.RootPath` | `"mtp://Apple iPad/Internal Storage"` |
| Discovery | `IBackupDriveInfo.DeviceName` | `"Apple iPad"` |
| Discovery | `IBackupDriveInfo.DriveName` | `"Internal Storage"` |
| Source | `BackupPlan.SourcePath` | `"mtp://Apple iPad/Internal Storage/DCIM/Camera"` |
| Source | `directoryPath` | `"DCIM/Camera"` (`SourcePath` minus `{deviceName}/{driveName}/`) |
| Traversal output | `sourceRelativeFilePath` | `"2024/jan/picture.jpg"` |
| Traversal output | `sourceRelativeDirectoryPath` | `"2024/jan"` |

**`BackupPlan.SourcePath`** = den fulde sti. `MtpUriParser` udtrækker `deviceName`, `driveName`, `directoryPath`. Separate `SourceDeviceName`/`SourceDriveName` properties på `BackupPlan` er **ikke nødvendige** — det er redundant når parseren kan splitte.

### Sammenligning: Filesystem vs. MTP

| Koncept | Filesystem | MTP |
|---------|-----------|-----|
| Discovery | `RootPath` = `"C:\"` | `RootPath` = `"mtp://Apple iPad/Internal Storage"` |
| Source root | `SourcePath` = `@"C:\temp\source"` | `SourcePath` = `"mtp://Apple iPad/Internal Storage/DCIM/Camera"` |
| Traversal relative | `sourceRelativeFilePath` = `"2024\jan\picture.jpg"` | `sourceRelativeFilePath` = `"2024/jan/picture.jpg"` |

---

## 2. Hvordan gør de andre projekter?

### Oversigt

| Projekt | MTP URI format | Authority | Escaping | MTP traversal |
|---------|---------------|-----------|----------|---------------|
| **Core** (original) | Ingen `mtp://` URI | `MediaDevice.FriendlyName` | Ingen | Inline i BackupHandler |
| **Core2** | `mtp://{deviceId}/{segments}` | `device.DeviceId` (GUID) | `Uri.EscapeDataString()` per segment | MediaDeviceScanner + gatekeeper |
| **Core3** | Ingen MTP support | N/A | N/A | N/A |
| **Core4** | **Vores beslutning:** `mtp://{deviceName}/{driveName}/{directoryPath}` | `device.FriendlyName` | Raw/unescaped | MediaDeviceTraversal + gatekeeper |
| **Common** | Ingen MTP | N/A | N/A | N/A |
| **MessageFormatter** | Ingen MTP | N/A | N/A | N/A |

### Core2 — detaljer

- `PathNormalizer.NormalizeMtpUri(path, deviceId)` — Utility, ubrugt i produktion
- `BackupScanner.ScanMediaDeviceAsync` — inline URI construction med `device.DeviceId` (GUID)
- CLI: `--source-device` → `plan.SourceId = deviceName`, `plan.SourcePath` = tom (root)
- **Forskel:** Core2 bruger `DeviceId` som authority, vi bruger `FriendlyName`

### Core4 — nuværende (før fiks)

- `BackupMediaDriveInfo.BuildRootPath` = `$"mtp://{friendlyName}/{driveName}"` ✅ korrekt
- `MtpUriParser.Parse()` — eksisterer men er ubrugt i produktion
- **BUG 1:** `BackupEngine.MatchDrive` muterer `sourcePath.Replace("/", "\\")` → ødelægger `mtp://`
- **BUG 2:** CLI `BuildPlan` sætter `sourcePath = backupOptions.SourceDevice` (uden `mtp://` prefix)
- **BUG 3:** `ValidateBackupOptions` kræver `--source-directory` — afviser MTP-only

---

## 3. CLI mapping (endelig)

### CLI options

| Option | Purpose | Eksempel |
|--------|---------|----------|
| `--source-path` | Source sti — både MTP og filesystem | `"mtp://Apple iPad/Internal Storage/DCIM/Camera"` eller `"C:\Users\John\Pictures"` |

CLI'en gør **kun**:
- Modtager rå `--source-path` værdi
- Detekterer type via `mtp://` prefix → sæt `SourceType`
- For filesystem: `Path.GetFullPath` (gør relative paths absolute)
- For MTP: send råt videre — al parsing er engine-ansvar

### BuildPlan logik

```csharp
if (WasSupplied(parseResult, BackupOptionsModel4.SourcePathOption) && !string.IsNullOrWhiteSpace(backupOptions.SourcePath))
{
    sourcePath = backupOptions.SourcePath;

    if (sourcePath.StartsWith("mtp://", StringComparison.Ordinal))
    {
        sourceType = Core4BackupSourceType.MediaDevice;
    }
    else
    {
        sourceType = Core4BackupSourceType.FileSystem;
        sourcePath = Path.GetFullPath(sourcePath);
    }
}
```

### BackupPlan output

```csharp
return new BMTP3.Core4.Api.Models.BackupPlan
{
    SourceType = BackupSourceType.MediaDevice,
    SourcePath = "mtp://Apple iPad/Internal Storage/DCIM/Camera",
    ...
};
```

### Engine flow

1. `BackupEngine` modtager `plan.SourcePath` = `"mtp://Apple iPad/Internal Storage/DCIM/Camera"`
2. `MtpUriParser.Parse(sourcePath)` → `(DeviceName="Apple iPad", DriveName="Internal Storage", DirectoryPath="DCIM/Camera")`
3. `MatchDrive` matcher `drive.RootPath` (`"mtp://Apple iPad/Internal Storage"`) mod `sourcePath`
4. `GetRelativeDirectoryPath` udleder `"DCIM/Camera"` som traversal startpunkt

---

## 4. Udførte ændringer

| File | Ændring |
|------|---------|
| `BackupOptionsModel4.cs` | `--source-device` fjernet. `--source-directory` → `--source-path`. Property `SourceDirectory` → `SourcePath`. |
| `BackupConsoleCommand4.Helpers.cs` | `BuildPlan` simplificeret: kun prefix-detect + `Path.GetFullPath`. Hele MTP-URI-konstruktion fjernet (er engine-ansvar). |
| `BackupConsoleCommand4.cs` | Validator rettet: tjekker kun `--source-path`. |
| `BMTP3.Core4/Engine/BackupEngine.cs` | Fix `MatchDrive` — fjern `/`→`\\` mutation |
| `BMTP3.Core4/Traversal/MtpUriParser.cs` | Opdateret til `DeviceName`, `DriveName`, `DirectoryPath` |

---

## 5. Open questions

| # | Spørgsmål | Status |
|---|-----------|--------|
| 1 | Skal `FriendlyName` URI-escapes? | **Ikke besluttet** |
| 2 | Skal `directoryPath` valideres for `../` path traversal? | Bør valideres |

---

## 6. Reference: nøglefiler

| Fil | Ansvarlig |
|-----|-----------|
| `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4.Helpers.cs` | `BuildPlan` — prefix-detect + `Path.GetFullPath` |
| `BMTP3.Core4/Engine/BackupEngine.cs` | `MatchDrive` + `GetRelativeDirectoryPath` — MTP `/` separators |
| `BMTP3.Core4/Traversal/MtpUriParser.cs` | Parser for `mtp://` URI |
| `BMTP3.Core4/Traversal/MtpUriParser.cs` | Eksisterende parser |
| `BMTP3.Core4/Storage/BackupMediaDriveInfo.cs:140-143` | `BuildRootPath` — format reference |
| `BMTP3.Core2/BackupNew/Utilities/PathNormalizer.cs:67-113` | Core2's escaping (reference) |
| `BMTP3.Core2/BackupNew/Engine/Traversal/BackupScanner.cs:185-189` | Core2's URI construction (reference) |
| `BMTP3.Core4.Tests/Traversal/MtpUriParserTests.cs` | Eksisterende parser tests |
