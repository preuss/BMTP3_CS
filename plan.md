# BMTP3.Core4 — Plan

> **Opdateret 5 Jun 2026** — MTP Del 1 (gatekeeper) done. `SourceTraversalItem` redesignet med `RelativePath` + `FileName`. 228 tests.
> 
> ⚠️ **FAIL-FIRST:** Alle gates/tjek i traversal og engine skal kaste exception ved fejl — aldrig `yield break`, `return` eller `continue` for at tie stille om problemer. Source der ikke findes = throw. Eneste undtagelse: per-item try-catch der markerer failed items men re-thrower (fail-fast).

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
- [x] — `using static` fjernet fra BackupEngine.cs (ubrugt)
- [x] — Doc comments i `SignalInterrupt.cs` og `SignalInterruptEngine.cs` opdateret til `ISignalSubscription`
- [x] — `BytesProcessed` akkumuleres nu live i Completed-fasen
- [x] — `MediaDevices.dll` reference tilføjet til Core4.csproj
- [x] — `FileSystemTraversal`: `yield break` → `throw DirectoryNotFoundException` når source ikke findes (fail-first)
- [x] — `SourceTraversalItem`: `FileName` + `RelativePath` (begge `required`) — traversal leverer alt, `BackupScanner` mapper kun properties
- [x] — `BackupScanner` renset: ingen `Path.*` kald, `Id = sourceItem.Id` i stedet for `relativePath`
- [x] — `MtpUriParser` + `MtpUriParseResult` — parse `mtp://Device/Path`. 15 tests.
- [x] — `IMtpGatekeeper` + `MtpGatekeeper` — `Func<CancellationToken, Task<T>>`, `AcquireAsync(TimeSpan, ...)`, `ThrowIfDisposed`, `Interlocked` dispose. 9 tests.
- [x] — `IMtpDeviceSession` + `MtpDeviceSession` — connect/disconnect, `[SupportedOSPlatform("windows7.0")]`

## Næste opgaver (prioriteret)

### Høj prioritet — MTP/MediaDevice support

- [x] **MTP Del 0:** `MtpUriParser` + `MtpUriParseResult`
- [x] **MTP Del 1:** `IMtpGatekeeper` + `MtpGatekeeper`
- [x] **MTP Del 2:** `IMtpDeviceSession` + `MtpDeviceSession` (connect/disconnect)
- [x] **MTP Del 3:** `MediaDeviceContent : IContent` + `GatekeptStream` (MTP streaming)
- [ ] **MTP Del 4:** `MediaDeviceTraversal : ISourceTraversal` (MTP traversal)
- [ ] **MTP Del 5:** `MediaDeviceTraversalFactory` + opdater `SourceTraversalFactory`
- [ ] **MTP Del 6:** DI registration + `MtpDeviceService`
- [ ] **MTP Del 7:** Tests
- [x] **MTP arkitektur:** `SourceTraversalItem.RelativePath` tilføjet — eksplicit relativ sti. `MediaDeviceTraversal` sætter den, `BackupScanner` bruger `sourceItem.RelativePath ?? Path.GetRelativePath(...)`

### Allersidst

- [ ] — Wire Core4 into Consoles (incl. SignalInterrupts cancel-wiring)
- [ ] — **Fjern validator-gates**: `BackupPlanValidator` blokerer `PostWriteVerification`, `ComparisonHashAlgorithmTypes` m.fl. selvom engine understøtter dem.

## Ref

- `Backup_Pipeline_Comparison_003.md` — comprehensive gap analysis (supersedes 001 and 002)
- `mangler.md` — issues and remaining work
