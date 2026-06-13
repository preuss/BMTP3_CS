# SidecarRequest Super-Refactor

## Problem

`SidecarRequest` har to properties der er død kode:

- `SourceDetailsSectionName` — `string?`, kun brugt som section navn
- `SourceDetails` — `IReadOnlyDictionary<string, string>?`, aldrig population nogen steder

`SidecarService.BuildDocument` har en gren (linje 80-95) der tjekker `SourceDetails is { Count: > 0 }` — kører aldrig.

Dictionary er u-type-sikker, u-testbar, og giver ingen garanti om hvilke nøgler der findes.

## Løsning

Erstat dictionary med to proper typed records:

- `MediaDeviceSourceDetails` — device-specifik info (device name, model, serial)
- `FileSystemDriveSourceDetails` — drive-specifik info (drive name, volume label)

`SidecarRequest` får nullable properties for hver type i stedet for dictionary.

`BackupEngine` populationslogik sætter den relevante type baseret på `plan.SourceType`.

`SidecarService.BuildDocument` matcher på typen og skriver korrekt section.

## Steps

### Step 1: Opret `MediaDeviceSourceDetails` record

Ny fil `Engine/Sidecar/MediaDeviceSourceDetails.cs`:

```csharp
namespace BMTP3.Core4.Engine.Sidecar;

internal sealed record MediaDeviceSourceDetails
{
    public required string DeviceName { get; init; }
    public string? Model { get; init; }
    public string? SerialNumber { get; init; }
}
```

### Step 2: Opret `FileSystemDriveSourceDetails` record

Ny fil `Engine/Sidecar/FileSystemDriveSourceDetails.cs`:

```csharp
namespace BMTP3.Core4.Engine.Sidecar;

internal sealed record FileSystemDriveSourceDetails
{
    public required string DriveName { get; init; }
    public string? VolumeLabel { get; init; }
}
```

### Step 3: Opdater `SidecarRequest`

| Før | Efter |
|-----|-------|
| `SourceDetailsSectionName` | ❌ fjern |
| `SourceDetails` dictionary | ❌ fjern |
| — | `MediaDeviceSourceDetails? MediaDeviceDetails { get; init; }` |
| — | `FileSystemDriveSourceDetails? FileSystemDriveDetails { get; init; }` |

Files: `SidecarRequest.cs`

### Step 4: Opdater `SidecarService.BuildDocument`

Erstat:

```csharp
if(request.SourceDetails is { Count: > 0 } && request.SourceDetailsSectionName is not null)
{
    string? sectionComment = request.SourceDetailsSectionName switch
    {
        "SourceDevice" => "...",
        "SourceDrive" => "...",
        _ => null,
    };
    SidecarSection detailsSection = doc.WithSection(request.SourceDetailsSectionName, weight: 20, comment: sectionComment);
    foreach(KeyValuePair<string, string> detail in request.SourceDetails)
        detailsSection.WithProperty(detail.Key, detail.Value);
}
```

Med:

```csharp
if(request.MediaDeviceDetails is not null)
{
    doc.WithSection("SourceDevice", weight: 20,
        comment: "SourceDevice is used only when SourceType=MtpDevice. It contains device details for the MTP source.")
        .WithProperty("DeviceName", request.MediaDeviceDetails.DeviceName)
        .WithProperty("Model", request.MediaDeviceDetails.Model)
        .WithProperty("SerialNumber", request.MediaDeviceDetails.SerialNumber);
}

if(request.FileSystemDriveDetails is not null)
{
    doc.WithSection("SourceDrive", weight: 20,
        comment: "SourceDrive is used only when SourceType=Drive. It contains drive information.")
        .WithProperty("DriveName", request.FileSystemDriveDetails.DriveName)
        .WithProperty("VolumeLabel", request.FileSystemDriveDetails.VolumeLabel);
}
```

Files: `SidecarService.cs`

### Step 5: Population i `BackupEngine`

I `BackupEngine.cs` sidecar-konstruktion (omkring linje 440-461), tilføj:

```csharp
MediaDeviceSourceDetails? mediaDeviceDetails = null;
FileSystemDriveSourceDetails? fileSystemDriveDetails = null;

if(plan.SourceType == BackupSourceType.MediaDevice)
{
    // Hent device info fra IConnectedMediaDriveSource
    mediaDeviceDetails = new()
    {
        DeviceName = connectedMediaDriveSource.Device.Name,
        Model = connectedMediaDriveSource.Device.Model,
        SerialNumber = connectedMediaDriveSource.Device.SerialNumber,
    };
} else
{
    // Hent drive info fra IConnectedFileSystemSource
    fileSystemDriveDetails = new()
    {
        DriveName = Path.GetPathRoot(plan.SourcePath) ?? "Unknown",
        VolumeLabel = DriveInfo.GetDrives()
            .FirstOrDefault(d => d.Name.StartsWith(Path.GetPathRoot(plan.SourcePath) ?? ""))
            ?.VolumeLabel,
    };
}

SidecarRequest request = new()
{
    // ... eksisterende properties ...
    MediaDeviceDetails = mediaDeviceDetails,
    FileSystemDriveDetails = fileSystemDriveDetails,
};
```

Files: `BackupEngine.cs`

### Step 6: Build + tests

- `dotnet build` — 0 errors, 0 warnings
- `dotnet test` — alle bestående

### Step 7: Opdater `mangler.md` + `plan.md`

- Markér K-V32(3) som ✅ DONE
- Opdater resolved-sektionen

## Afhængigheder

```
Step 1 ──┐
Step 2 ──┤
         ├── Step 3 ──┐
         │            ├── Step 4 ──┐
         │            │            ├── Step 5
         │            │            │
         └────────────┴────────────┴── Step 6 → Step 7
```

Step 1 og 2 er uafhængige. Step 3 afhænger af 1+2. Step 4 afhænger af 3. Step 5 afhænger af 4. Step 6+7 sidst.

## Files berørt

| Fil | Step | Ændring |
|-----|------|---------|
| `Engine/Sidecar/MediaDeviceSourceDetails.cs` | 1 | **Ny fil** — typed record |
| `Engine/Sidecar/FileSystemDriveSourceDetails.cs` | 2 | **Ny fil** — typed record |
| `Engine/Sidecar/SidecarRequest.cs` | 3 | Fjern dictionary, tilføj typed records |
| `Engine/Sidecar/SidecarService.cs` | 4 | Erstat død gren med type-matchet section |
| `Engine/BackupEngine.cs` | 5 | Population af details baseret på SourceType |
| `mangler.md`, `plan.md` | 7 | Status update |
