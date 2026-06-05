# BMTP3.Core4 — Plan

> **Opdateret 5 Jun 2026** — Ctrl+C Del 3 completed. Shadow af `cancellationToken`, `ISignalSubscription` interface, handler-baseret registrering i BackupEngine. I alt 204 tests.

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
- [x] E2 — Tilføj `ThrowIfCancellationRequested()` i starten af foreach-loop (BackupEngine.cs:219) — ✅ senere fjernet som redundant
- [x] FileContent cleanup — fjernet Volatile, simplificeret dispose-logik, doc comments genindsat
- [x] Ubrugt `earliest` — nu brugt på linje 301; `ResolveDate()` fjernet; kaster exception hvis null
- [x] Step-number jump (8→10) — rettet til `// 9. Return BackupResult`
- [x] DeleteEmptyDirectories — allerede implementeret via `CleanupSessionTempDirectory` i finally block
- [x] GlobMatcher (Core4) — brace expansion + bugfixes. Samme fixes backportet til GlobConverter (Consoles) og GlobMatcher (Core2)
- [x] E6 — Per-item try-catch i processing loop (BackupEngine.cs), markér failed items som Failed, re-throw (fail-fast)
- [x] I6 — Post-write hash verification efter MoveTo (kun Hash, Binary fjernet — giver ikke mening efter MoveTo)
- [x] `PostWriteVerificationType.Binary` fjernet — kun None/Hash tilbage
- [x] E5 — Fjern redundant timestamp-logik fra `DownloadService`
- [x] I7 — Implement Include/Exclude patterns i `FileSystemTraversal` — `GlobMatcher.IsIncluded` i loopet, gates ikke fjernet
- [x] — DryRun — short-circuit med `BuildDryRunResult` helper før processing loop
- [x] — Sidecar redesign: Document/Section/Property model (fluent API + weight-sortering)
- [x] — INI sidecar writer med `#` kommentar-støtte (multi-line)
- [x] — JSON sidecar writer
- [x] — Sidecar format: `[Source]`, `[SourceDevice]`/`[SourceDrive]`, `[Backup]`, `[Path]`, `[Hashes]` (matcher brugerens spec)
- [x] — `MD5` i stedet for `MD5_128` i hashes sektion
- [x] — Alle hashes altid til stede i `[Hashes]` + `SHA3_512` alias
- [x] — `ResolvedDateTime` → `MediaTakenDateTime` rename i hele Core4
- [x] — `ItemMetadata` udvidet med originale datoer: `AuthoredDateTime`, `CreatedDateTime`, `ModifiedDateTime`, `AccessedDateTime`
- [x] — Originale datoer captured før timestamp correction
- [x] — Sidecar læser datoer fra `ItemMetadata` i stedet for `Item`
- [x] — Writer Stream refactoring: `ISidecarWriter.WriteToFileAsync` → `WriteToStreamAsync(Stream)`
- [x] — `IniSidecarWriter` forbedret: `WriteCommentBlock` + options (CRLF, PreserveEmptyCommentLines, WriteKeysWithNullValues)
- [x] — `JsonSidecarWriter` forbedret: `CreateSerializableModel` + `SerializeAsync(stream)` — ingen mellemstring
- [x] — N6: SidecarServiceTests (6), IniSidecarWriterTests (14), JsonSidecarWriterTests (10) = 30 nye tests. I alt 160.
- [x] — Ctrl+C Del 1: `BackupEngine.RunAsync` fanger `OperationCanceledException` → `BackupResult` med `State = Cancelled`
- [x] — Ctrl+C Del 2: SignalInterruptEngine (singleton subscription engine) + SignalInterrupt static entry + SignalInterruptRegistrationBuilder. 6 filer. Gammel event-kode slettet. `ISignalSubscription` interface tilføjet.
- [x] — Ctrl+C Del 3: Shadow af `cancellationToken = cancellationTokenSource.Token`. Handler-baseret registrering: `SignalInterrupt.On(All).Handler(ctx => { cts.Cancel(); }).Create()`. `using ISignalSubscription` sikrer cleanup. TODO: "Finally saves when Cancel() is called."
- [x] — Ctrl+C Del 4: SignalInterrupts-tests (37): enum (5), context (10), builder (12), entry point (10). + BackupEngine cancellation pattern tests (5). I alt 204 tests.
- [x] — `ISignalSubscription : IDisposable` interface med `Signals` + `Handler` properties
- [x] — `Create()`/`Register()` returnerer `ISignalSubscription` i stedet for `IDisposable`

## Næste opgaver (prioriteret)

### Medium prioritet

- [ ] — Wire Core4 into Consoles (incl. SignalInterrupts cancel-wiring)
- [ ] — Opdater SignalInterrupt doc comments til at reflektere `ISignalSubscription` return type

### Allersidst (når alt andet er færdigt og testet)

- [ ] N1 — Destination inspection / sidecar hash read-back (cross-run dedup). TODO i `RenameCollisionResolver.cs:213`. Kræver `ISidecarReader` + INI/JSON parser. Core2 ref: `SidecarReader.cs`, `DestinationInspector.cs`.
- [ ] — **Fjern validator-gates**: `BackupPlanValidator` blokerer `PostWriteVerification`, `ComparisonHashAlgorithmTypes` m.fl. selvom engine understøtter dem. Inkl. C4: Fix tier-gating så den matcher hvad engine faktisk understøtter (currently blocks ALL plans).

## Ref

- `Backup_Pipeline_Comparison_003.md` — comprehensive gap analysis (supersedes 001 and 002)
- `mangler.md` — issues and remaining work
