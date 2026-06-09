# Task: TOML Config Reader

> Implementer `--config` fil support så Core4 kan læse backup settings fra TOML.

---

## Goal

Gør det muligt for brugere at angive `--config backup.toml` i stedet for at skulle angive alle options på kommandolinjen.

---

## Existing Code (reference)

| Artifact | Sti |
|----------|-----|
| Core3 `ConfigModel` (TOML model) | `BMTP3.Core3/Configuration/ConfigModel.cs` |
| Core3 `BackupSettingsReader` (TOML parser) | `BMTP3.Core3/Configuration/BackupSettingsReader.cs` |
| Core3 `BackupSettingsImpl` | `BMTP3.Core3/Configuration/BackupSettingsImpl.cs` |
| Core4 `BackupPlan` (public record) | `BMTP3.Core4/Api/Models/BackupPlan.cs` |
| CLI option parsing | `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4.cs` |
| CLI BuildPlan helper | `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4Helpers.cs` |

---

## Implementation Approach

**Mulighed A (anbefalet):** Genbrug Core3's TOML model men map til Core4's `BackupPlan`.

1. Kopier/refaktor `ConfigModel` til Core4 (eller del som `Common`)
2. Implementer TOML parser (genbrug `TomlSettingsReader` logik)
3. Map TOML settings → Core4 `BackupPlan` properties
4. Tilføj `--config` option til CLI
5. I `BuildPlan` eller før: hvis `--config` angivet, load TOML → merge med CLI args

**Mulighed B:** Tilføj TOML NuGet pakke og skriv ny reader fra bunden.

---

## Key Decisions

| Spørgsmål | Overvej |
|-----------|---------|
| TOML NuGet vs egen parser | Core3 har egen TOML læser — genbrug hvis muligt |
| Merge strategi | CLI args → override TOML → override defaults? |
| Config skema | Skal matche `BackupPlan` properties 1:1 |

---

## Files to Create

### `BMTP3.Core4/Configuration/BackupConfigModel.cs`

```csharp
namespace BMTP3.Core4.Configuration;

// TOML model — matcher BackupPlan structure
internal sealed record BackupConfigModel
{
    public string? Name { get; init; }
    public string? SourcePath { get; init; }
    public string? Destination { get; init; }
    public string? SourceType { get; init; }
    // ... alle properties fra BackupPlan
}
```

### `BMTP3.Core4/Configuration/TomlConfigReader.cs`

```csharp
namespace BMTP3.Core4.Configuration;

internal static class TomlConfigReader
{
    public static BackupConfigModel ReadFromFile(string filePath)
    {
        // Læs TOML → return model
    }

    public static BackupPlan MapToPlan(BackupConfigModel config, BackupPlan defaults)
    {
        // Map TOML model → BackupPlan, kun override ikke-null værdier
    }
}
```

### `BMTP3.Core4/Configuration/TomlSettingsReader.cs`

Kopier/refaktor fra Core3's `BackupSettingsReader.cs`.

---

## Files to Modify

### `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4.cs`

Tilføj option:

```csharp
new Option<string>("--config", "Path to TOML configuration file")
```

I handler: hvis `--config` angivet, load TOML → map til plan → CLI args overskriver.

### `BMTP3.Consoles/ConsoleCommands/BackupConsoleCommand4Helpers.cs`

Opdater `BuildPlan` til at acceptere en `BackupConfigModel` parameter til merge.

---

## Dependencies

- Kræver forståelse af Core3's TOML reader
- Ingen blokering fra andre tasks
