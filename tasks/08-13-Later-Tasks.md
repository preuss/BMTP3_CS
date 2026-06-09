# Tasks: Later (Tier 3/4 gates + deferred)

> Feature-gates for features der ikke er implementeret endnu.

---

## Task 08: StopOnError=false (T3 gate)

**Gate:** `Engine/Validation/BackupPlanValidator.cs:81`
**Niveau:** Tier 3

**Problem:** Per-item try-catch og `Failed` status findes, men `throw` på linje 485 i `BackupEngine.cs` forhindrer continuation.

```csharp
// Linje 485 — re-thrower altid:
record.Status = BackupItemStatus.Failed;
throw; // <-- SKAL KUN KASTE HVIS plan.StopOnError == true
```

**Løsning:** Ændr til betinget throw:

```csharp
record.Status = BackupItemStatus.Failed;
if (plan.StopOnError)
    throw;
// else: fortsæt til næste item
```

**Files to modify:**
- `Engine/BackupEngine.cs:485`
- `Engine/Validation/BackupPlanValidator.cs:81` (fjern gate)

---

## Task 09: BackupIndexType.Database (T4 gate)

**Gate:** `Engine/Validation/BackupPlanValidator.cs:86-87`
**Niveau:** Tier 4

**Problem:** SQLite catalog writer mangler. Kræver:
- SQLite NuGet dependency
- `IBackupIndexWriter` interface (samme som Task 01)
- `SqliteBackupIndexWriter` implementation
- Database schema design

**Løsning:** Udvid `IBackupIndexWriter` med en SQLite implementation når T4 er i scope.

**Files to modify:**
- `Engine/Validation/BackupPlanValidator.cs:86-87` (fjern gate)
- Ny: `Engine/Index/SqliteBackupIndexWriter.cs`

---

## Task 10: EnableMetadata (T3 gate)

**Gate:** `Engine/Validation/BackupPlanValidator.cs:77-78`
**Niveau:** Tier 3

**Problem:** Metadata extraction (EXIF, fil-attributter) er ikke integreret i processing loop.

**Løsning:** Implementer metadata reader service og kald i loop:
- `IMetadataReader` interface
- `FileMetadataReader` (EXIF via MetadataExtractor)
- Kald i processing loop efter download

**Files to modify:**
- `Engine/Validation/BackupPlanValidator.cs:77-78` (fjern gate)

---

## Task 11: MaxDegreeOfParallelism (T4 gate)

**Gate:** `Engine/Validation/BackupPlanValidator.cs:88-89`
**Niveau:** Tier 4

**Problem:** Parallel execution framework (`BackupRunner`) findes men er ikke implementeret.

**Løsning:** Implementer parallel processing loop med `Parallel.ForEachAsync` eller `ActionBlock`.

**Files to modify:**
- `Engine/Validation/BackupPlanValidator.cs:88-89` (fjern gate)

---

## Task 12: Hash Algorithm CLI Options

**Problem:** CLI har ikke options til at vælge comparison/verification hash algoritmer.

**Løsning:** Tilføj `--comparison-hash` og `--verification-hash` options i CLI.

**Files to modify:**
- `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4.cs`
- `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4Helpers.cs`

---

## Task 13: Erstat Core2 backup med Core4 som default

**Problem:** `backup` command bruger Core2. `backup4` er en separat command.

**Løsning:** Når Core4 er feature-complete, erstatt `backup` med Core4 og fjern `backup4` alias.

**Files to modify:**
- `BMTP3.Consoles/ConsolesProgram.cs` — omdøb `backup4` til `backup`
