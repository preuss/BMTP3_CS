# Mangler / Issues

> **Seneste opdatering:** 23 Jun 2026 (Bug #9 re-evalueret)
> **Tests:** 1666/1666 passing (Common: 281, MessageFormatter: 351, Core4: 930, Consoles: 104)
> **Docs cleanup:** 24 forældede docs slettet — værdifuld viden ekstraheret til plan.md, AGENTS.md, mangler.md

---

## 🔴 Bugs (latente fejl)

| # | Fil | Linje | Problem |
|---|---|---|---|
| **1** | `FileSystemTraversal.cs` | 89-120 | `SafeGetFiles`, `SafeGetDirectories`, `SafeGetDate` — **alle exceptions swallows** (`catch` → return null/tom). Hvis en mappe giver `UnauthorizedAccessException` eller `PathTooLongException`, får brugeren bare færre filer. Intet log, intet fail. **Bryder fail-first princippet.** | ✅ **FIXET** — `catch{}` fjernet (fail-first), `SafeGetDate` filtrerer specifikke exceptions |
| **2** | `BackupEngine.cs` | 693-708 | `FilterPendingRecords` switch på `BackupItemStatus` — håndterer **kun** `Succeeded`, `Skipped`, `Pending`. `Active` og `Failed` **falder stille igennem** — items ignoreres uden warning. |
| **4** | `BackupEngine.cs` | 461 | **Uforsikret cast** til `IMoveableContent` — `(IMoveableContent)record.Item.Content`. Hvis `DownloadService` ikke har kørt (eller fejlede), crasher det med `InvalidCastException`. | ✅ **FIXET** — `is not IMoveableContent` pattern match med `InvalidOperationException` |
| **5** | `BackupPlan4Config.cs`, `BackupPlanBuilder.cs` | 15, 80-86 | `Source.Type` property **læses aldrig i production**. Config-feltet `source.type = "MediaDevice"` ignoreres — typen udledes altid fra path prefix. `ApplyConfig()` brugte path prefix i stedet for config'en. `ApplyCliOverrides()` havde samme pattern — ingen `--source-type` CLI option. | ✅ **FIXET** — `ApplyConfig()` bruger `ParseEnum<BackupSourceType>(config.Source.Type)`, `--source-type` CLI option tilføjet, path detection fjernet fra config-flow og CLI |
| **6** | `MediaDeviceTraversal.cs` | 224 | `DateTimeKind.Unspecified` fra MTP antages at være **maskinens lokale tidszone**. Hvis kameraet var i UTC+8 og PC'en i UTC-5, forskydes datoer med 13 timer. |
| **7** | `BackupPlanBuilder.cs` | 38-39 | Default til ALLE 9 hash-algoritmer. Hver fil hashes 9 gange som standard, selv når `CollisionComparisonType = Binary`. **Voldsom performance-omkostning** for store backups. |
| **8** | `BackupPlan.cs` vs `BackupPlanBuilder.cs` | flere | **Default mismatch** — `EnableTimestampCorrection` (model=false, builder=true) og `StopOnError` (model=false, builder=true). Hvis nogen bruger `BackupPlan` direkte (uden builder), får de modsatte defaults. |
| **9** | `BackupEngine.cs` | 348-351 | Timestamp resolution fejl **altid fatal** — `throw InvalidOperationException` selv når `EnableTimestampCorrection = false`. | 🟢 **Ikke en bug** — `Content` er altid `FileContent` efter download (temp-fil). Temp-filer har altid gyldige filesystem-timestamps > Unix epoch, så `Timestamp` er aldrig null. Defensivt guard — fjernet fra bugs. |

---

## ⚠️ Mistænkelige (værd at kigge på)

| # | Fil | Linje | Problem |
|---|---|---|---|
| **10** | `DownloadService.cs` | 27-30 | `.LocalDateTime` på UTC-datoer — korrekt rountrip, men wall clock i Windows Explorer viser PC-tidszone, ikke kildens tidszone. |
| **11** | `EarliestTimestampResolutionService.cs` | 112-147 | `PickBetter` — 1 dags tolerance. Hvis EXIF siger 23 Jun, FS siger 22 Jun, vinder EXIF (den mest præcise). Men hvis EXIF-datoen er forkert, overrider den en korrekt FS-dato. |
| **12** | `TimestampCandidateFactory.cs` | 92, 224, 596 | `dto.DateTime` (lokal dato) bruges i stedet for `dto.UtcDateTime`. 1-dags tolerancen absorberer normalt forskellen, men det er skrøbeligt. |
| **13** | `MediaDeviceContent.cs` | 33-43 | Resource leak — hvis `GatekeptStream` constructor fejler, lækkes `rawStream`. |
| **14** | `SidecarSection.cs` | 23 | Case-sensitive key match (`Ordinal`) — virker nu, men hvis nogen senere tilføjer key med anden casing, duplikeres entries. |
| **15** | `BackupEngine.cs` | 442, 548 | Asymmetrisk throttling — skipped filer får 1x throttle kald, normale filer 3x. |
| **16** | `SessionStateService.cs` | 140-143 | Gemmer **korrigerede** datoer i session summary. Resume med `EnableTimestampCorrection=false` får de korrigerede datoer tilbage, ikke originalerne. |
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
| 21| **Bug #9 (fejlklassificering):** Timestamp resolution fatal — `throw` når `Timestamp` er null | **Ikke en bug.** Efter download er `Content` altid `FileContent` (temp-fil). Temp-filer har altid gyldige filesystem-timestamps > Unix epoch, så `Timestamp` er aldrig null. Defensivt guard — fjernet fra bugs. |

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
|---|---|---|---|
| 🔴 Bugs (latente) | 4 | #2 FilterPendingRecords switch, #6 MTP tidszone, #7 Default hash-algoritmer, #8 Default mismatch |
| 🟡 Bør testes (større) | ~60 filer | TimeStamp (~18 filer: 13 readers + EarliestTimestampResolutionService), integration tests, error paths i øvrige komponenter, SignalInterruptEngine, Drive providers |
| 🟢 Nice-to-have | ~15 items | MediaDeviceContent, model defaults, edge cases |
| 🔶 Kosmetisk | 2 | Sidecar separator style, CollisionStreategy filename |
