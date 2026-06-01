# BMTP3.Core4 — Plan

> **Opdateret 1 Jun 2026** — C4 flyttet til Lav prioritet (gøres sidst).

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
- [x] I4/E4 — Fix `MoveableFileContent.MoveTo` overwrite bug — `FileInfo.MoveTo(dest, false)` → `overwrite`
- [x] E1 — Fix `CompositeTimestampReader` CancellationToken + redesign: `ITimestampReader` / `ICompositeTimestampReader` / `TimestampReaderException` / `TryReadCollect`
- [x] E2 — Tilføj `ThrowIfCancellationRequested()` i starten af foreach-loop (BackupEngine.cs:219)
- [x] FileContent cleanup — fjernet Volatile, simplificeret dispose-logik, doc comments genindsat
- [x] Ubrugt `earliest` — nu brugt på linje 301; `ResolveDate()` fjernet; kaster exception hvis null
- [x] Step-number jump (8→10) — rettet til `// 9. Return BackupResult`
- [x] DeleteEmptyDirectories — allerede implementeret via `CleanupSessionTempDirectory` i finally block

## Næste opgaver (prioriteret)

### Høj prioritet

- [x] E6 — Per-item try-catch i processing loop (BackupEngine.cs), markér failed items som Failed, re-throw (fail-fast)
- [x] I6 — Post-write hash verification efter MoveTo (kun Hash, Binary fjernet — giver ikke mening efter MoveTo)
- [x] `PostWriteVerificationType.Binary` fjernet — kun None/Hash tilbage

### Medium prioritet

- [x] I6 — Post-write verification efter fil-flytning

### Lav prioritet (gøres i denne rækkefølge)

- [x] E5 — Fjern redundant timestamp-logik fra `DownloadService`
- [x] E7 — Ikke en bug — `TryDeleteIfEmpty` er korrekt. `.tmp`-filer er forensic evidence
- [ ] — Implement Include/Exclude patterns i `FileSystemTraversal`
- [ ] — Implement DryRun — skip writes når `plan.DryRun` er true
- [ ] — JSON sidecar (currently `NotImplementedException`)
- [ ] I5 — Device metadata i sidecar (`[DeviceDetails]`, `[PathMapping]`)
- [ ] N1 — Destination inspection / sidecar hash read-back (cross-run dedup)
- [ ] N5 — Ryd op: wire eller fjern ubrugte `BackupRunner`-klasser
- [ ] N6 — Skriv tests (HashService, DI, SummaryStore, CollisionHelpers, DiskSpaceValidator)
- [ ] — Wire Core4 into Consoles (Consoles still uses Core2)

### Allersidst (når alt andet er færdigt og testet)

- [ ] — **Fjern validator-gates**: `BackupPlanValidator` blokerer `PostWriteVerification`, `ComparisonHashAlgorithmTypes` m.fl. selvom engine understøtter dem.
- [ ] C4 — Fix `BackupPlanValidator` tier-gating så den matcher hvad engine faktisk understøtter (currently blocks ALL plans)

## Ref

- `Backup_Pipeline_Comparison_003.md` — comprehensive gap analysis (supersedes 001 and 002)
- `mangler.md` — issues and remaining work
