# Sidecar Format — Core4

## Format

INI-filer med `#` som kommentartegn. Extension: `.sidecar.ini`.

## Section Order (by weight)

| Weight | Section | Required |
|--------|---------|----------|
| 10 | `[Source]` | Yes |
| 20 | `[SourceDevice]` or `[SourceDrive]` | Only when details exist |
| 30 | `[Backup]` | Yes |
| 40 | `[Path]` | Yes |
| 50 | `[Hashes]` | Yes |

## Rules

- `[SourceDevice]` is ONLY used when `SourceType=MediaDevice` (MTP).
- `[SourceDrive]` is ONLY used when `SourceType=FileSystem`.
- For MTP backups, drive-related fields (`DriveName`, `VolumeLabel`, `DriveFormat`) go at the bottom of `[SourceDevice]`.

## Section Details

### `[Source]`

| Key | Type | Description |
|-----|------|-------------|
| `SourceType` | `BackupSourceType` | Either `MediaDevice` or `FileSystem` |
| `SourceId` | `string?` | Stable identity for the source file |
| `SourceFileName` | `string` | The file name of the source file |
| `MediaTakenDateTime` | `DateTimeOffset?` | The resolved media date for the source file |
| `AuthoredDateTime` | `DateTimeOffset?` | Special source date from MTP device metadata |
| `CreateDateTime` | `DateTimeOffset?` | Standard source filesystem dates |
| `LastWriteDateTime` | `DateTimeOffset?` | (same group) |
| `LastAccessDateTime` | `DateTimeOffset?` | (same group) |

Comments:
- `SourceType`: `# Either MtpDevice or Drive.`
- `SourceId`: `# Stable identity for the source file, if available.`
- `SourceFileName`: `# The file name of the source file.`
- `MediaTakenDateTime`: `# The resolved media date for the source file.` + `# This is the date previously referred to as the resolved media datetime.`
- `AuthoredDateTime`: `# Special source date commonly found in MTP device metadata.` + `# It may not correspond to any regular filesystem date.`
- `CreateDateTime`/`LastWriteDateTime`/`LastAccessDateTime`: only `# Standard source filesystem dates.` on `CreateDateTime` line.

### `[SourceDevice]` (when `SourceType = MediaDevice`)

Device properties (top):

| Key | Type | Description |
|-----|------|-------------|
| `DeviceId` | `string` | Device identifier |
| `Description` | `string` | Device description |
| `FriendlyName` | `string` | User-friendly device name |
| `Manufacturer` | `string` | Device manufacturer |
| `Model` | `string` | Device model |
| `SerialNumber` | `string` | Device serial number |
| `FirmwareVersion` | `string` | Device firmware version |

Drive properties (bottom, from the MTP drive being backed up):

| Key | Type | Description |
|-----|------|-------------|
| `DriveName` | `string` | MTP drive name |
| `VolumeLabel` | `string` | Volume label |
| `DriveFormat` | `string` | File system format (e.g. `DCF`) |

Not included:
- `DeviceType` — irrelevant
- `Protocol` — implicit known (MTP)

### `[SourceDrive]` (when `SourceType = FileSystem`)

| Key | Type | Description |
|-----|------|-------------|
| `DriveName` | `string` | Drive root path (e.g. `C:\`) |
| `VolumeLabel` | `string` | Volume label |
| `DriveFormat` | `string` | File system format (e.g. `NTFS`) |

### `[Backup]`

| Key | Type | Description |
|-----|------|-------------|
| `BackupStartDateTime` | `DateTimeOffset` | When this file was backed up |

### `[Path]`

| Key | Type | Description |
|-----|------|-------------|
| `SourceRelativeFilePath` | `string` | Original source-relative path |
| `SanitizedSourceRelativeFilePath` | `string?` | Windows-compatible version of source path |
| `TargetRelativeFilePath` | `string` | Actual relative path in destination |

### `[Hashes]`

All keys always present, even when empty. Order:

| Key | Algorithm |
|-----|-----------|
| `SHA3_512` | Alias for `SHA3_512_FIPS202` (written first) |
| `SHA3_512_FIPS202` | SHA3-512 (FIPS 202) |
| `SHA3_512_KECCAK` | SHA3-512 (Keccak) |
| `SHA2_512` | SHA2-512 |
| `SHA2_256` | SHA2-256 |
| `MD5` | MD5 (alias for `MD5_128`) |
| `BLAKE3_256` | BLAKE3-256 |
| `BLAKE3_512` | BLAKE3-512 |

## Record Design

The base class `BackupSourceDetails` holds the common drive properties shared by both source types:

```csharp
internal abstract record BackupSourceDetails
{
    public required string DriveName { get; init; }
    public required string VolumeLabel { get; init; }
    public required string DriveFormat { get; init; }
}
```

### `MediaDeviceDriveSourceDetails` (MTP)

Inherits `DriveName`, `VolumeLabel`, `DriveFormat` from `BackupSourceDetails` and adds device-specific properties:

```csharp
internal sealed record MediaDeviceDriveSourceDetails : BackupSourceDetails
{
    public required string DeviceId { get; init; }
    public required string Description { get; init; }
    public required string FriendlyName { get; init; }
    public required string Manufacturer { get; init; }
    public required string Model { get; init; }
    public required string SerialNumber { get; init; }
    public required string FirmwareVersion { get; init; }
}
```

### `FileSystemDriveSourceDetails` (FileSystem)

All three properties are inherited from `BackupSourceDetails` — the record body is empty:

```csharp
internal sealed record FileSystemDriveSourceDetails : BackupSourceDetails
{
}
```

## Fixture Files

| File | Purpose |
|------|---------|
| `Fixtures/sidecar_device.ini` | Template, all values empty |
| `Fixtures/sidecar_drive.ini` | Template, all values empty |
| `Fixtures/sidecar_device_example.ini` | Example with real MTP data |
| `Fixtures/sidecar_drive_example.ini` | Example with real FileSystem data |
