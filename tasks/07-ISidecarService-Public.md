# Task: ISidecarService Public API — ✅ WONTFIX

> **Beslutning truffet 11 Jun 2026:** `ISidecarService` forbliver `internal`.

---

## Baggrund

- **Core3:** `ISidecarGenerator` / `SimpleSidecarGenerator` var `public` — eksterne API-brugere kunne generere sidecars selv.
- **Core4:** `ISidecarService` er `internal` — kun `BackupEngine` kan generere sidecars.

---

## Afgørelse

**WONTFIX** — Forbliver `internal`. Begründung:

1. **YAGNI** — Ingen kendte eksterne consumere af Core4 biblioteket
2. **Core4 bruges internt** via `BackupEngine.RunAsync()` — sidecar-generering sker automatisk baseret på `BackupPlan.SidecarFormat`
3. **API-overflade holdes minimal** — kan altid gøres public senere hvis behov opstår
4. **Anbefaling i task-fil** (linje 46): "Lad være internal indtil videre" — beslutning bekræftet

---

## Status

✅ **Afsluttet** — Ingen kodeændringer nødvendige. Dokumenteret i `plan.md` §8 og `mangler.md` § "Public API overvejelser" + Feature Audit tabel.

## Note om `SidecarFormat`

Kun `SidecarFormat` (enum) er `public` — den ligger i `BMTP3.Core4.Api.Models.Enums` namespace som er Core4's public API surface. Alle andre sidecar-typer (`ISidecarService`, `SidecarService`, `SidecarRequest`, `ISidecarWriter`, `IniSidecarWriter`, `JsonSidecarWriter`) er `internal` og forbliver det.
