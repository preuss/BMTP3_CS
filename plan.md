# BMTP3.Core4 — Plan

> **Opdateret 31 May 2026** — efter fuld cross-reference audit. Mange items er nu implementeret.

## Færdige opgaver

- [x] I1 — Disk space check (service: `IDiskSpaceValidator` + `DiskSpaceValidator`)
- [x] I3 — Timestamp preservation på temp-fil efter download (basal: `CreationTime` + `LastWriteTime`)
- [x] I3a — Timestamp preservation: sæt ALLE datoer på temp-fil (`DateCreated`, `DateModified`, `DateAuthored`, `DateAccessed`) — ✅ done in `EarliestTimestampResolutionService`
- [x] I4 — Timestamp correction på destinationsfil efter `MoveTo()` — ✅ done (applied in `ResolveAndApplyEarliestAsync` before move; file timestamps survive the move)
- [x] C1 — Post-run persistence: kald `_sessionState.SaveAsync()` efter foreach-loop — ✅ done in `BackupEngine.cs:400` (finally block)
- [x] C2 — File-backed `SummaryStore`: gem `BackupSummary` som JSON i `.bmtp3/` — ✅ `BackupJsonSummaryStore`
- [x] C3 — Implementer real filesystem traversal og scanner — ✅ `FileSystemTraversal` + `BackupScanner`
- [x] I5 — Binary compare (identisk fil-detektion) i collision resolution — ✅ `FileCompareService` + 5 algorithms + `BinaryFileComparerSelector` + wired in `RenameCollisionResolver`
- [x] N4 — Ryd op: fjern `EarliestTimestampResolutionServiceAnother.cs` — ✅ deleted

## Næste opgaver (prioriteret)

### Høj prioritet

- [ ] C4 — Fix `BackupPlanValidator` tier-gating så den matcher hvad engine faktisk understøtter (currently blocks ALL plans)
- [ ] I4/E4 — Fix `MoveableFileContent.MoveTo` overwrite bug (`FileInfo.MoveTo(dest, false)` hardcoded)
- [ ] E1 — Fix `CompositeTimestampReader` så CancellationToken faktisk passes til sub-readers
- [ ] E6 — Tilføj per-item try-catch i processing loop (BackupEngine.cs:219), markér failed items som Failed, fortsæt med næste
- [ ] I2 — Source/output access probe i pre-flight
- [ ] N3 — Cleanup af tomme temp-mapper i post-run

### Medium prioritet

- [ ] E3 — Tilføj `catch(OperationCanceledException)` i BackupEngine så cancellation returnerer `BackupResult.Cancelled` i stedet for unhandled exception
- [ ] I6 — Post-write verification efter fil-flytning
- [ ] E2 — Overvej `ThrowIfCancellationRequested()` i starten af foreach-loop (BackupEngine.cs:219)

### Lav prioritet / fremtid

- [ ] E5 — Fjern redundant timestamp-logik fra `DownloadService` (overrides af `EarliestTimestampResolutionService`)
- [ ] E7 — Overvej at slette individuelle `.tmp`-filer i `CleanupSessionTempDirectory` i stedet for kun tomme dirs
- [ ] N1 — Destination inspection / sidecar hash read-back (cross-run dedup)
- [ ] N2 — Device metadata i sidecar (`[DeviceDetails]`, `[PathMapping]`)
- [ ] N5 — Ryd op: wire eller fjern ubrugte `BackupRunner`-klasser
- [ ] N6 — Skriv tests (HashService, DI, SummaryStore, CollisionHelpers, DiskSpaceValidator)
- [ ] — JSON sidecar (currently `NotImplementedException`)
- [ ] — Wire Core4 into Consoles (Consoles still uses Core2)
- [ ] — Implement DryRun (plan field exists but engine ignores it)
- [ ] — Implement Include/Exclude patterns (plan fields exist but traversal ignores them)

## Ref

- `Backup_Pipeline_Comparison_003.md` — comprehensive gap analysis (supersedes 001 and 002)
- `mangler.md` — issues and remaining work
