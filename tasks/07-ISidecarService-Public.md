# Task: ISidecarService Public API

> Overvej og implementér om `ISidecarService` skal være `public` i Core4.

---

## Goal

Beslut om `ISidecarService` (og evt. relaterede typer) skal være `public` som i Core3, eller forblive `internal` som nu.

---

## Background

- **Core3:** `ISidecarGenerator` / `SimpleSidecarGenerator` var `public` — eksterne API-brugere kunne generere sidecars selv.
- **Core4:** `ISidecarService` er `internal` — kun `BackupEngine` kan generere sidecars.

---

## Overvejelser

| For public | For internal |
|------------|--------------|
| Ekstern API-konsistens med Core3 | YAGNI — ingen kendt ekstern consumer |
| Brugere kan generere sidecars til egne formål | Holder API flade mindre |
| Framework/library scenario | Kan altid gøres public senere |

---

## Hvis public — hvad skal ændres

| Type | Nuværende | Skal være |
|------|-----------|-----------|
| `ISidecarService` | `internal interface` | `public interface` |
| `SidecarService` | `internal sealed class` | `public sealed class` |
| `SidecarRequest` | `internal sealed record` | `public sealed record` |
| `ISidecarWriter` | `internal interface` | `public interface` (?) |
| `SidecarFormat` (enum) | `public enum` | Allerede public — OK |
| `IniSidecarWriter` | `internal` | Skal forblive internal? |
| `JsonSidecarWriter` | `internal` | Skal forblive internal? |

---

## Anbefaling

**Lad være internal indtil videre.** Hvis en ekstern consumer opstår, kan det gøres public på få minutter. Core4 er stadig i udvikling.

---

## Dependencies

- Self-contained (beslutning + evt. access modifier ændringer)
