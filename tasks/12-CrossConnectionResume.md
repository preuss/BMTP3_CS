# Cross-Connection Resume for MTP

## Problem

Apple MTP-enheder regenererer både `PersistentUniqueId` og `file.Id` ved reconnect/restart.
`SessionStateService` matcher på `BackupItem.Id` (= `PersistentUniqueId`), hvilket fejler efter
cross-connection → filer gen-scannes som nye → dubletter.

## Løsning

Brug `MediaDeviceTraversal.GenerateAlmostUniqueId()` som fallback i `SessionStateService`.
Kombinerer path + size + 3 timestamps til et næsten-unikt ID der er stabilt på tværs af WPD-reconnects.

## Steps

### Step 1: Tilføj `ContentHash` til `SourceTraversalItem`

- Nyt felt: `string? ContentHash` på `SourceTraversalItem`
- I `MediaDeviceTraversal.TraverseAsync`: kald `GenerateAlmostUniqueId(file, size, dates)` efter `Id`
- Files: `SourceTraversalItem.cs`, `MediaDeviceTraversal.cs`

### Step 2: Tilføj `ContentHash` til `BackupItem`

- Nyt felt: `string? ContentHash` på `BackupItem`
- `BackupScanner`: passthrough `ContentHash = sourceItem.ContentHash`
- Files: `BackupItem.cs`, `BackupScanner.cs`

### Step 3: Fallback matching i `SessionStateService`

- I `FindExistingRecord` / `TryMatchAsync`: prøv først `PersistentUniqueId` (nuværende logik)
- Hvis intet match → slå op på `ContentHash`
- Files: `SessionStateService.cs`

### Step 4: Tests

- Mock en Apple-enhed der regenererer IDs — verify at ContentHash match virker
- Test at non-Apple flow (PersistentUniqueId match) stadig virker først
