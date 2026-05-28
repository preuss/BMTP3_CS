# BMTP3.Core4 — Plan

## Færdige opgaver

- [x] I1 — Disk space check (service: `IDiskSpaceValidator` + `DiskSpaceValidator`)
- [x] I3 — Timestamp preservation på temp-fil efter download (basal: `CreationTime` + `LastWriteTime`)

## Næste opgaver (prioriteret)

### Høj prioritet

- [ ] I3a — Timestamp preservation: sæt ALLE datoer på temp-fil (`DateCreated`, `DateModified`, `DateAuthored`, `DateAccessed`)
- [ ] I4 — Timestamp correction på destinationsfil efter `MoveTo()`
- [ ] C4 — Fix `BackupPlanValidator` tier-gating så den matcher hvad engine faktisk understøtter
- [ ] C1 — Post-run persistence: kald `_sessionState.SaveAsync()` efter foreach-loop
- [ ] C2 — File-backed `SummaryStore`: gem `BackupSummary` som JSON i `.bmtp3/`
- [ ] C3 — Implementer real filesystem traversal og scanner
- [ ] I2 — Source/output access probe i pre-flight
- [ ] N3 — Cleanup af tomme temp-mapper i post-run

### Medium prioritet

- [ ] I5 — Binary compare (identisk fil-detektion) i collision resolution
- [ ] I6 — Post-write verification efter fil-flytning

### Lav prioritet / fremtid

- [ ] N1 — Destination inspection / sidecar hash read-back (cross-run dedup)
- [ ] N2 — Device metadata i sidecar (`[DeviceDetails]`)
- [ ] N4 — Ryd op: fjern `EarliestTimestampResolutionServiceAnother.cs`
- [ ] N5 — Ryd op: wire eller fjern ubrugte `BackupRunner`-klasser
- [ ] N6 — Skriv tests (HashService, DI, SummaryStore, CollisionHelpers, DiskSpaceValidator)

## Ref

- `Backup_Pipeline_Comparison_001.md` — original analyse
- `Backup_Pipeline_Comparison_002.md` — opdateret gap-analyse
