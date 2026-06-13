# Refactor: Fix WPD leak + SidecarRequest redesign

## Problem

To WPD-specifikke værdier lækker til det generiske lag:

1. **`MediaDeviceTraversal.SourcePath = file.FullName`** — giver `\Internal Storage\DCIM\image.jpg` (WPD-sti uden device name, uden `mtp://`)
2. **`SidecarRequest` bruger `string` i stedet for enums og MTP-specifikke navne på top-level**

## Steps

### Phase 1 — Fix WPD leak i traversal

**Step 1: `MediaDeviceTraversal.cs:94` — `SourcePath` → `mtp://` URI**

`SourcePath = file.FullName` → konstruer `mtp://{deviceName}/{driveName}/{subPath}/{relativeFilePath}`.

Traversal har allerede alt på linje 32-33:
- `_mediaDevice.Name` (= "Apple iPad")
- `_mediaDrive.Name` (= "Internal Storage")
- `request.SubPath` (= "DCIM/Camera")
- `relativeFilePath` (= "image.jpg")

**Step 1a:** Normaliser `request.SubPath` og `relativeFilePath` til `/` i stedet for `\`. Byg URI:
```
mtp://Apple iPad/Internal Storage/DCIM/Camera/image.jpg
```

**Files:** `BMTP3.Core4/Traversal/MediaDeviceTraversal.cs`

**Tests:** `MediaDeviceTraversalTests` — eksisterende tests skal stadig pass (SourcePath format ændres, men det er ikke assertet i tests).

---

### Phase 2 — Fix SidecarRequest

**Step 2: `SidecarRequest.cs` — `SourceType`: `string` → `BackupSourceType` enum**

Ændringer:
- `SidecarRequest.SourceType`: `string` → `BackupSourceType`
- `SidecarService.BuildDocument` linje 70: brug `request.SourceType.ToString()` i stedet for hardcoded string
- `BackupEngine.cs` linje 443-448: fjern switch — sæt direkte `SourceType = plan.SourceType`

**Files:** `SidecarRequest.cs`, `SidecarService.cs`, `BackupEngine.cs`

**Step 3: `SidecarRequest.cs` — `SourcePersistentUniqueId` → `SourceId`**

- Omdøb property
- Tilføj `SourceId = record.Item.Id` i `BackupEngine.cs` sidecar-konstruktion

**Files:** `SidecarRequest.cs`, `SidecarService.cs`, `BackupEngine.cs`

**Step 4: `SidecarRequest.cs` — fjern `SourceDetails`/`SourceDetailsSectionName` (dead code)**

`SourceDetails` og `SourceDetailsSectionName` er aldrig sat nogen steder. Hele grenen i `SidecarService.BuildDocument` (linje 82-97) er død kode. Fjern properties + død gren.

**Files:** `SidecarRequest.cs`, `SidecarService.cs`

---

### Phase 3 — Verify

**Step 5: Build + tests**
- `dotnet build` — 0 errors, 0 warnings
- `dotnet test` — 332 passed (249 Core4 + 83 Consoles)

---

## Afhængigheder

```
Step 1 ──┐
         ├── Step 2 ──┐
         │            ├── Step 3 ──┐
         │            │            ├── Step 4
         │            │            │
         └────────────┴────────────┴── Step 5
```

Step 1 og Step 2 er uafhængige. Step 3 og 4 afhænger af Step 2 (alle i `SidecarRequest.cs`). Step 5 er sidst.

## Files berørt

| Fil | Step | Ændring |
|-----|------|---------|
| `Traversal/MediaDeviceTraversal.cs` | 1 | `SourcePath` → `mtp://` URI |
| `Engine/Sidecar/SidecarRequest.cs` | 2, 3, 4 | Enum + rename + remove dead code |
| `Engine/Sidecar/SidecarService.cs` | 2, 4 | Match nyt model |
| `Engine/BackupEngine.cs` | 2, 3 | Opdater sidecar-konstruktion |

## Design-beslutninger udskudt

- **`MediaDeviceSourceDetails` / `FileSystemSourceDetails`** — proper typed records til device/drive-specifik info (device name, model, serial, volume label, etc.). Kræver design-session. Ikke del af denne refactor.
- **Sidecar `SourceFullPath`** — hedder det noget andet? Skal det være `SourcePath`? Kræver beslutning sammen med `SourceId`.
