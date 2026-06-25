# Mangler / Issues

> **Seneste opdatering:** 25 Jun 2026 (Dybdeanalyse: 11 nye bugs fundet i Core4)
> **Tests:** 1668/1668 passing (Core4: 932, MessageFormatter: 351, Common: 281, Consoles: 104)
> **Docs cleanup:** 24 forældede docs slettet — værdifuld viden ekstraheret til plan.md, AGENTS.md, mangler.md

---

## 🔴 Bugs (latente fejl)

| # | Fil | Linje | Problem |
|---|---|---|---|
| **1** | `FileSystemTraversal.cs` | 89-120 | `SafeGetFiles`, `SafeGetDirectories`, `SafeGetDate` — **alle exceptions swallows** (`catch` → return null/tom). Hvis en mappe giver `UnauthorizedAccessException` eller `PathTooLongException`, får brugeren bare færre filer. Intet log, intet fail. **Bryder fail-first princippet.** | ✅ **FIXET** — `catch{}` fjernet (fail-first), `SafeGetDate` filtrerer specifikke exceptions |
| **2** | `BackupEngine.cs` | 693-708 | `FilterPendingRecords` switch — `Active` falder stille igennem. | ✅ **FIXET** — silent `break` → `throw new UnreachableException(...)`. `default:` case tilføjet som guard mod fremtidige enum-værdier. |
| **4** | `BackupEngine.cs` | 461 | **Uforsikret cast** til `IMoveableContent` — `(IMoveableContent)record.Item.Content`. Hvis `DownloadService` ikke har kørt (eller fejlede), crasher det med `InvalidCastException`. | ✅ **FIXET** — `is not IMoveableContent` pattern match med `InvalidOperationException` |
| **5** | `BackupPlan4Config.cs`, `BackupPlanBuilder.cs` | 15, 80-86 | `Source.Type` property **læses aldrig i production**. Config-feltet `source.type = "MediaDevice"` ignoreres — typen udledes altid fra path prefix. `ApplyConfig()` brugte path prefix i stedet for config'en. `ApplyCliOverrides()` havde samme pattern — ingen `--source-type` CLI option. | ✅ **FIXET** — `ApplyConfig()` bruger `ParseEnum<BackupSourceType>(config.Source.Type)`, `--source-type` CLI option tilføjet, path detection fjernet fra config-flow og CLI |
| **6** | `MediaDeviceTraversal.cs` | 224 | `DateTimeKind.Unspecified` fra MTP antages at være **maskinens lokale tidszone**. Hvis kameraet var i UTC+8 og PC'en i UTC-5, forskydes datoer med 13 timer. | 🟢 **Ikke en bug** — når MTP giver `Unspecified` er `Local` det bedste gæt. Rettet `DateTimeKind.Local` til renere form: `new DateTimeOffset(dateTime).ToUniversalTime()` i stedet for `ToUniversalTime()` + manuel offset. Fjernet fra bugs. |
| **7** | `BackupPlanBuilder.cs` | 38-39 | Default til ALLE 9 hash-algoritmer. | 🟢 **Ikke en bug** — single-pass arkitektur (1× I/O), brugeren vælger selv via CLI. Flere hashes i sidecar = bedre fremtidig verifikation. Fjernet fra bugs. |
| **8** | `BackupPlan.cs` vs `BackupPlanBuilder.cs` | flere | **Default mismatch** — `EnableTimestampCorrection` (model=false, builder=true) og `StopOnError` (model=false, builder=true). | ✅ **FIXET** — `BackupPlan.cs:154,170` tilføjet `= true` på begge properties. Matcher nu builderens defaults. |
| **9** | `BackupEngine.cs` | 348-351 | Timestamp resolution fejl **altid fatal** — `throw InvalidOperationException` selv når `EnableTimestampCorrection = false`. | 🟢 **Ikke en bug** — `createFileDate` er nødvendig for path resolution (linje 393) og collision resolution (linje 408), uanset `EnableTimestampCorrection`. Et korrekt timestamp kan ikke erstattes (UtcNow ville give forkert mappenavn). Throw + per-item catch håndterer korrekt: Failed item + StopOnError gating. Dette er **identisk fail-first adfærd** med alle andre engine steps. |

---

## 🔴 Bugs (dybdeanalyse 25 Jun 2026 — 11 nye fund)

Systematisk gennemgang af Core4 ud over de 9 oprindelige bugs. Fokuseret på logiske fejl, ressource leaks, race conditions og exception håndtering.

### 🔴 CRITICAL

| # | Fil | Linje | Problem |
|---|---|---|---|
| **B1** | `BackupEngine.cs` | 557 | `SaveAsync` i `finally` uden try-catch — hvis `SaveAsync` kaster (I/O fejl, serialisering), **maskeres den originale exception** (OCE eller processing exception). `CleanupSessionTempDirectory` nedenfor er korrekt wrapped, hvilket beviser at pattern var kendt men `SaveAsync` blev misset. |
| **B2** | `PipelinedDownloadService.cs` | 112-129 | **Deadlock ved consumer-fejl** — hvis `destStream.WriteAsync` kaster (disk fuld), fejler consumer task. Producer fortsætter uvidende og blokerer på `channel.Writer.WriteAsync` når bounded channel (kapacitet 2) er fuld. `Task.WhenAll` venter for evigt. |
| **B3** | `PipelinedDownloadService.cs` | 102 | **Buffer leak ved WriteAsync-fejl** — når `WriteAsync` kaster, er buffer leaset fra `ArrayPool` men returneres aldrig. Producerens catch har ingen `Return(buffer)`. |
| **B4** | `PipelinedDownloadService.cs` | 106-108 | **Buffers efterladt i channel ved producer cancellation** — `channel.Writer.Complete(ex)` forlader alle `BufferChunk`-instanser i kanalen. Consumerens `finally` (der returnerer buffers) kører aldrig. Op til 4MB læk per fejlet download. |

### 🟠 HIGH

| # | Fil | Linje | Problem |
|---|---|---|---|
| **B5** | `BackupEngine.cs` | 114 | `CancellationTokenSource` aldrig disposed — holder kernel wait handle. Lækker for processens levetid. |
| **B6** | `BackupEngine.cs` | 545-550 | **Temp file læk på item failure** — `tempFile` ryddes kun på Skip (437) og OCE (543), **ikke** på generel exception (545-549). Med `StopOnError=false` akkumuleres temp-filer. `CleanupSessionTempDirectory` nægter at slette ikke-tomme dirs → permanente orphans. |
| **B7** | `BackupEngine.cs` | 436,530,547 | `StatusChangedAt` **aldrig sat** i engine — kun læst. Alle summaries skriver `"CompletedAt": null`. Feltet har nul værdi. |
| **B8** | `BackupJsonSummaryStore.cs` | 40-41 | **Korrupt session JSON crasher hele backup** — `File.ReadAllText` + `JsonSerializer.Deserialize` uden try-catch. Trunkeret/korrupt session file → `JsonException` → ubehandlet crash. |
| **B9** | `BackupEngine.cs` | 210 | `Progress<T>` i Core4 bryder arkitekturregel — dispatcher via ThreadPool, handler kører konkurrent med main loop. AGENTS.md forbyder eksplicit. |
| **B10** | `FileContent.cs` | 110-123 | `OpenReadAsync` ignorerer `CancellationToken` — `FileStream` constructor kaldes synkront uanset cancellation state. |

### 🟡 MEDIUM

| # | Fil | Linje | Problem |
|---|---|---|---|
| **B11** | `BackupEngine.cs` | 557 | `CancellationToken.None` i finally → Ctrl+C blokerer hvis save er langsom (netværksshare, antivirus) |
| **B12** | `BackupEngine.cs` | 284-553 | Ingen checkpoint saves. Hard-kill (power loss, StackOverflowException) mister **hele run** |
| **B13** | `ParallelStreamHashGenerator.cs`, `PooledStreamHashGenerator.cs` | 120, 108 | `ArrayPool.Return` uden `clearArray:true` → fil-data lækker i shared pool |
| **B14** | `Hashing/Crypto/BouncyCastle*.cs`, `SharpHashMD5.cs` | alle | **4 ubrugte** Crypto-wrappers — dead code. Ingen references i produktion. |
| **B15** | `BackupJsonSummaryStore.cs` | 36-42 | `LoadAsync` ikke cancellable, sync `File.ReadAllText`. `ApplyResumeAsync` har `CancellationToken` men sender den ikke ned. |

### 🟢 LOW

| # | Fil | Linje | Problem |
|---|---|---|---|
| **B16** | `BackupEngine.cs` | 252 | `ulong`→`long` overflow ved cast af `Content.Length` (teoretisk, >9 EB) |
| **B17** | `SessionStateService.cs` | 52-53 | `Continue` resume strategy → orphan destination files. Items fjernet fra source efterlades på disk uden tracking. |
| **B18** | `ParallelStreamHashGenerator.cs` vs AGENTS.md | 20 | DOP `/3` i kode vs `/4` i docs (commit 72c6e54 ændrede til `/3`, docs ikke opdateret) |

### ❌ AFKRÆFTET

| # | Hvad | Begrundelse |
|---|---|---|
| SharpHash Dispose | SharpHashSHA3_* mangler Dispose override | `IHash` fra SharpHash **arver ikke** `IDisposable` — base `HashAlgorithm.Dispose()` er sufficient ✅ |
| MediaDeviceTraversal deadlock | Gatekeeper holdes under traversal | Korrekt dokumenteret i XML-doc: traversal skal færdig før content consumption ✅ |
| GatekeptStream constructor | Resource leak | Allerede fikset i tidligere session — try-catch med cleanup ✅ |

---

## ⚠️ Mistænkelige (værd at kigge på)

| # | Fil | Linje | Problem |
|---|---|---|---|
| **10** | `DownloadService.cs` | 27-30 | `.LocalDateTime` på UTC-datoer — korrekt rountrip, men wall clock i Windows Explorer viser PC-tidszone, ikke kildens tidszone. |
| **11** | `EarliestTimestampResolutionService.cs` | 112-147 | `PickBetter` — 1 dags tolerance. Hvis EXIF siger 23 Jun, FS siger 22 Jun, vinder EXIF (den mest præcise). Men hvis EXIF-datoen er forkert, overrider den en korrekt FS-dato. |
| **12** | `TimestampCandidateFactory.cs` | 92, 224, 596 | `dto.DateTime` (lokal dato) bruges i stedet for `dto.UtcDateTime`. 1-dags tolerancen absorberer normalt forskellen, men det er skrøbeligt. |
| **13** | `GatekeptStream.cs` | 9-22 | Resource leak — hvis `GatekeptStream` constructor fejler, lækkes `rawStream`. | ✅ **FIXET** — try-catch i `GatekeptStream` konstruktør: `inner?.Dispose()` + `lease?.Dispose()` ved fejl |
| **14** | `SidecarSection.cs` | 23 | Case-sensitive key match (`Ordinal`) — virker nu, men hvis nogen senere tilføjer key med anden casing, duplikeres entries. |
| **15** | `BackupEngine.cs` | 442, 548 | Asymmetrisk throttling — skipped filer får 1x throttle kald, normale filer 3x. |
| **17** | `BackupSummaryItem.cs` | 11 | `LastModified` i stedet for `DateModified` — bryder `DateXxx` konventionen. |

---

## ✅ Fikset i denne session

| # | Problem | Fix |
|---|---|---|
| 1 | `PathHelper.GetRelativePath` — hardcoded `Ordinal` i `StartsWith` → crash ved casing-mismatch | `Ordinal` → `OrdinalIgnoreCase`. Doc opdateret. |
| 2 | `PathDisplayCasing` var `public` i `Helpers` namespace — brød Api-regel | `public` → `internal`. Ny `IFileSystemPathResolver` + `FileSystemPathResolver` i `Api/`. |
| 3 | `BackupPlanBuilder` brugte `Path.GetFullPath` — bevarede brugerens casing | `Path.GetFullPath` → `IFileSystemPathResolver.ResolveExistingPathDisplayCasing`. |
| 4 | Consoles tests brugte fake paths → `FileNotFoundException` | `IFileSystemPathResolver` interface + `StubFileSystemPathResolver` i tests. |
| 5 | GlobMatcher: 3 Both-mode tests brugte `ForwardSlash` default | Tilføjet `GlobSeparatorMode.Both` parameter. |
| 6 | GlobMatcher: 3 invalid-pattern tests forventede tolerant adfærd | Ændret til `Assert.Throws<ArgumentException>` (fail-fast er korrekt). Testnavne opdateret. |
| 7 | Consoles GlobMatcher: `BothSlashesAreSeparatorsByDefault` brugte `ForwardSlash` | Tilføjet `GlobSeparatorMode.Both`. Navn → `BothSlashesAreSeparators`. |
| 8 | Consoles GlobMatcher: 7 xUnit2008 warnings (`Assert.True(Regex.IsMatch)`) | `Assert.Matches` / `Assert.DoesNotMatch`. |
| 9 | `BackupPlanValidator` — 0 tests (høj prioritet i MANGLER.md 1A) | `BackupPlanValidatorTests.cs` — 45 tests: null/empty paths, invalid enums, hash+algorithms, backslash patterns, feature gate, happy paths |
| 10 | `BinaryFileComparerSelector` — chunked algoritmer aldrig testet (1B) | `BinaryFileComparerSelectorTests.cs` — 10 tests: constructor null guards, Select null/argument guards, small files → WholeFile, large files → chunked comparers |
| 11 | `BackupScanner` progress race — `Progress<T>` wrapper dispatcher async via ThreadPool, `SynchronousProgress<T>` utilstrækkelig | `TaskCompletionSource` i test — `await tcs.Task` efter enumeration venter på async dispatch |
| 12 | `BackupEngine` error paths — 0 tests (1C) | `BackupEngineErrorPathTests.cs` — 10 tests: Download, TargetPathResolver, SidecarService, HashService, TS resolution failures (StopOnError true/false); mixed success; empty/no-matching drive |
| 13 | `Progress<T>` i Core4 — dispatcher async via ThreadPool, giver race conditions i tests og uforudsigelig adfærd | `TransformProgress<TInner,TOuter>` (transform) + `ActionProgress<T>` (action) — synkrone erstatninger. `BackupScanner` + `BackupEngine` opdateret. `CountingProgress<T>` i test. |
| 14 | TimeStamp parsers (14 klasser) — 0 tests | 284 tests i `Parsers/` — alle 14 parser-klasser med edge cases, kebab-case aliases, null/empty, garbage input |
| 15 | TimeStamp candidates — 0 tests | 198 tests i `Candidates/` — 7 klasser med factory-metoder, formatering, validering, debug output |
| 16 | TimeStamp parser/candidate tests — 17 failing assertions | IsAllNull (empty != null), Normalize (`with` returnerer ny instans), Full clock offset format (altid sekunder), Formatter vs candidate.ToString semantik, Factory.FromUtcDateAndTime bug (FullDate uden date), ToDebugString da-DK kulturformat |
| 17 | **Bug #1:** `SafeGetFiles`/`SafeGetDirectories`/`SafeGetDate` — silent catch{} → fail-first | `FileSystemTraversal.cs`: `catch{}` fjernet i SafeGetFiles/Directories; SafeGetDate filtrerer specifikke exceptions (UnauthorizedAccessException, IOException, NotSupportedException). `BackupEngineErrorPathTests`: +2 tests (TraversalFailure_FailFast, ScanPhaseCancellation_ReturnsCancelledResult). |
| 18 | `JsonBackupIndexWriterTests` — `.bmpt` → `.bmtp3` stavefejl (5 tests failed) | ` .bmpt` rettet til `.bmtp3` i 5 test-metoder. |
| 19| **Bug #3 (fejlklassificering):** `BinaryFileComparerBase.cs:13-14` — `!Exists && !Exists → true` | **Ikke en bug.** `BinaryFileComparerBase` er en generisk base class. Hvis begge filer mangler, er de i samme tilstand → `true` er logisk korrekt. `BinaryFileComparerSelector.Select()` garanterer allerede existence med `ArgumentException`, og callers (`RenameCollisionResolver`, `BackupEngine`) har egne `File.Exists`-guards. Fjernet fra bugs-listen. |
| 20| **Bug #5:** `Source.Type` ignoreret i config-flow | `ApplyConfig()` bruger `ParseEnum<BackupSourceType>(config.Source.Type)`, `--source-type` CLI option tilføjet, path detection fjernet. `BaseOptionsModel` urørt — `Option<BackupSourceType?>` binder korrekt til `BackupSourceType?` property. |
| 21| **Bug #9 (fejlklassificering):** Timestamp resolution fatal — `throw` når `Timestamp` er null | **Ikke en bug.** `createFileDate` er nødvendig for path resolution + collision resolution uanset `EnableTimestampCorrection` — UtcNow ville give forkert mappenavn. Throw + per-item catch (Failed + StopOnError gating) er korrekt fail-first. Defensivt guard — fjernet fra bugs. |
| 22| **Bug #8:** Default mismatch mellem `BackupPlan` model og `BackupPlanBuilder` | `BackupPlan.cs:154,170` — `= true` på `EnableTimestampCorrection` og `StopOnError`. Matcher nu builder. |
| 23| **Bug #7 (fejlklassificering):** Default alle 9 hash-algoritmer | **Ikke en bug.** Single-pass arkitektur (1× I/O). Brugeren vælger selv. Fjernet fra bugs. |
| 24| **Bug #2 (fejlklassificering):** `Active` case i `FilterPendingRecords` switch | **Ikke en bug.** `Active` sættes aldrig på records; `SessionStateService` kaster hvis `Active` dukker op. Fjernet fra bugs. |
| 25| **Bug #6 (forbedring):** `ToUtcOffsetOrNull` — `DateTimeKind.Local` case | `new DateTimeOffset(dateTime).ToUniversalTime()` i stedet for `new DateTimeOffset(dateTime.ToUniversalTime(), TimeSpan.Zero)`. Funktionsmæssigt identisk, stilmæssigt renere. `Unspecified` → `Local` er korrekt (bedste gæt). |
| 26| **SourceType nullable:** `BackupPlan.SourceType` → `BackupSourceType?` | Validator tjekker nu `SourceType == null` → kaster med "SourceType must be specified". Fanger glemt `--source-type` eller manglende `source.type` i config. |
| 27| **Bug #2 — `FilterPendingRecords`:** `Active` silent `break` | `break` → `throw new UnreachableException(...)`. `default:` guard tilføjet mod fremtidige enum-værdier. |
| 28| **#13 — MediaDeviceContent resource leak:** `rawStream` ikke disposed ved constructor-fejl | Allerede fikset af bruger i `GatekeptStream.cs` — try-catch i konstruktør: `inner?.Dispose()` + `lease?.Dispose()`. `MediaDeviceContent.cs` try-catch beholdes (lease cleanup ved `OpenRead()` fejl). |
| B1 | `SaveAsync` i finally maskerer originale exceptions | Wrapped i try-catch med `LogWarning` |
| B2 | PipelinedDownloadService deadlock ved consumer-fejl | `using var cts` deles mellem producer/consumer; consumer kalder `cts.Cancel()` |
| B3 | Buffer leak ved WriteAsync-fejl | Egen try-catch om `WriteAsync` med `ArrayPool.Return(buffer)` før throw |
| B4 | Buffers efterladt i channel ved producer cancellation | Consumer dræner kanal via `while(TryRead(out ...)) { Return(chunk.Buffer) }` |
| B5 | `CancellationTokenSource` aldrig disposed | `using` på CTS-deklarationen |

---

## 🔴 Test coverage huller i Core4

### Prioritet 1 — Bør testes nu

#### ~~1A. BackupPlanValidator — 0 tests~~ ✅ FIXET
**Fil:** `BMTP3.Core4/Engine/Validation/BackupPlanValidator.cs`

**45 tests** i `BackupPlanValidatorTests.cs`: null/empty/whitespace Name, SourcePath, Destination; invalid path characters; 9 invalid enum values; CollisionComparisonType.Hash uden algorithms; PostWriteVerification.Hash uden algorithms; backslash i include/exclude patterns; BackupIndexType.Database → FeatureNotImplementedException; valid minimal/full plan.

**Mangler stadig:**
- Cross-field validering (f.eks. `CustomOutputPattern` kræver `OutputStructureStrategy.Custom` — endnu ikke implementeret i produktionskode)
- Kombinationer af valideringsfejl

---

#### ~~1B. BinaryFileComparerSelector — chunked algoritmer aldrig testet~~ ✅ FIXET
**Filer:** `Engine/Compare/Algorithms/` (4 chunked comparers) + `BinaryFileComparerSelector.cs`

**10 tests** i `BinaryFileComparerSelectorTests.cs`: constructor null guards (5), Select null/argument guards (4), small files ≤ 10 MB → `WholeFileSequenceEqualBinaryComparer` (2), large files > 10 MB → chunked comparer (2, via sparse files — instant allocation).

**Mangler stadig:**
- Chunked-algoritmerne selv (de 4 `IBinaryFileComparer` implementeringer) har kun indirekte dækning via `FileCompareServiceTests` (som kun bruger små filer)
- `FileCompareService` med store filer der aktiverer chunked algorithms

---

#### ~~1C. Error paths i BackupEngine — recovery utestet~~ ✅ FIXET
**Fil:** `BMTP3.Core4/Engine/BackupEngine.cs`

Eksisterende tests dækker kun happy path + cancellation. Ingen test verificerer hvad der sker når engine-komponenter fejler midt i en backup:

**Hvad mangler:**
- `TargetPathResolver` kaster → engine logger og fortsætter? Stopper?
- `CollisionResolver` kaster → korrekt fejlhåndtering?
- `DiskSpaceValidator` kaster midlertidig fejl → retry?
- `HashService` fejler efter delvis download → cleanup?
- Sidecar write fejler → engine fortsætter eller stopper?
- `StopOnError=false` med mixed success/failure (> 3 items)

---

### Prioritet 2 — Større indsats

#### 2A. TimeStamp subsystem — ~45 filer, delvist testet
**Namespace:** `BMTP3.Core4/Engine/TimeStamp/`

Komplet metadata extraction pipeline (Exif, XMP, GPS, IPTC, QuickTime, filesystem timestamps; parsere; candidate resolution; `EarliestTimestampResolutionService`).

**Testet (482 tests, alle passer):**
- **Parsers** (14 klasser) — 284 tests i `BMTP3.Core4.Tests/Engine/TimeStamp/Parsers/`
- **Candidates** (7 klasser: TimestampSources, TimestampFormatStyleParser, TimestampFormatDescriptor, TimestampFormatter, TimestampCandidate, TimestampCandidateFactory, Parsed) — 198 tests

**Mangler:**
- **Readers** (13 filer) — kræver reelle filer med EXIF/XMP metadata, ikke testbare med pure logic
- **`EarliestTimestampResolutionService`** — kræver reelle filer

---

#### 2B. Error path tests i øvrige komponenter

| Komponent | Hvad mangler |
|---|---|
| `DownloadService` | Content `OpenReadAsync` kaster, pre-cancelled token — ✅ **FIXET** |
| `DownloadService` | Destination allerede låst, cancellation midt i write — kræver I/O (FileInfo.Create) |
| `FileCompareService` | Store filer (> 100 MB) der aktiverer chunked algoritmer |
| `DiskSpaceValidator` | Utilstrækkelig plads, ikke-eksisterende drev — kræver I/O (DriveInfo) |
| `SessionStateService` | Concurrent save/delete, meget store resume sets |
| `HashService` | Blandede hash families — ✅ allerede testet; null stream fra content — ✅ **FIXET** |
| `TempDirectoryHelper` | Concurrent cleanup, locked files, nested `.tmp` |
| `BackupScanner` | Items med null Content (guarded af BackupItem ctor), null dates (virker), ekstreme paths (virker) — intet at teste |

**Mapping audit 23 Jun 2026 — field gaps opdaget under 2B error path gennemgang:**

| # | Hvor | Hvad mangler | Alvor |
|---|---|---|---|
| **F1** | `SessionStateService.cs:63-67` | **Resume mister datoer** — `DateCreated`, `LastModified`, `DateAuthored`, `DateAccessed` skrives ikke tilbage til `BackupItem` | 🟡 Fragilt (afhænger af scan-før-resume flow) |
| **F2** | `BackupSummaryItem` record | **Hashes/metadata persisteres ikke** — `ComputedHashes`, `MediaTakenDateTime`, 4 metadata-datoer mangler | 🟢 Designvalg |
| **F3** | `BackupOptionsModel4.cs` | **`EnableTimestampCorrection` ikke på CLI** — kun via config | 🟢 Feature gap |

Se `plan.md § 🔴 P0 — Field mapping gaps` for detaljer.

---

#### 2C. Integration tests — 0 tests med rigtig I/O

Alle engine tests bruger fakes. Ingen test kører en ægte pipeline:
- FS traversal → scanner → engine → download → verify
- MTP pipeline: `MtpUriParser` → `MediaDeviceTraversal` → scanner → engine
- Real hashing: `DownloadService` → `HashService` → `StreamHashGenerator`
- Full roundtrip: backup → læs sidecar/index → verificer indhold

---

#### 2D. SignalInterruptEngine — dispatch logik utestet

`SignalInterruptEngine` (signal dispatch) — 0 tests. Kun builder, kind, context er testet.

---

#### 2E. Drive providers — 3 filer, 0 tests

`DriveProvider`, `MediaDeviceDriveProvider`, `FileSystemDriveProvider` — platform-afhængig kode, utestet.

---

### Prioritet 3 — Nice-to-have

| Komponent | Hvad mangler |
|---|---|
| `MediaDeviceContent.OpenReadAsync` | Kun 1 test (constructor null-check). MTP stream + lease mangler |
| `FileContent` / `MoveableFileContent` | File I/O wrapper — 0 tests |
| Model/enum default-værdier | Ingen test verificerer at modeller har korrekte defaults |
| `PathHelper` edge cases | UNC paths, `\\?\` prefix, platform case-sensitivity |
| `Guard` edge cases | `RequireZeroOrGreater` med long.MaxValue, strings med kun newlines |
| `Throttler` edge cases | DelayThrottler cancellation efter delay start, concurrent waits |

---

## 🔶 Kosmetisk — kan fixes når tid

#### CollisionStreategy.cs — stavefejl i filnavn
**Fil:** `BMTP3.Core4/Engine/Strategies/CollisionStreategy.cs`

Stavefejl (`Streategy` → `Strategy`). **Do not fix** — eksisterer i både docs og kode; rename ville give kaskaderende ændringer i git blame og references.

---

#### Sidecar TargetRelativeFilePath — blander separator-stil
**Fil:** `BMTP3.Core4/Engine/BackupEngine.cs:483`

```csharp
TargetRelativeFilePath = Path.GetRelativePath(plan.Destination, targetPath),
```

`Path.GetRelativePath` returnerer `\` på Windows. Sidecar får:
- `SourceRelativeFilePath` = forward slashes (fra kanonisk URI)
- `TargetRelativeFilePath` = backslashes (fra `Path.GetRelativePath`)

Ikke en runtime-fejl, men inkonsistent.

---

## Resume

| Prioritet | Antal | Område |
|---|---|---|
| 🔴 Bugs (latente — audit 23 Jun) | 0 | ✅ Alle 9 bugs gennemgået — 5 fikset, 4 re-evalueret som ikke-bugs |
| 🔴 Bugs (dybdeanalyse 25 Jun) | 18 | **4 CRITICAL, 6 HIGH, 5 MEDIUM, 3 LOW** — se § 🔴 Bugs (dybdeanalyse 25 Jun 2026) |
| 🟡 Bør testes (større) | ~60 filer | TimeStamp (~18 filer: 13 readers + EarliestTimestampResolutionService), integration tests, error paths i øvrige komponenter, SignalInterruptEngine, Drive providers |
| 🟢 Nice-to-have | ~15 items | MediaDeviceContent, model defaults, edge cases |
| 🔶 Kosmetisk | 2 | Sidecar separator style, CollisionStreategy filename |
