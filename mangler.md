# Mangler / Issues

> **Seneste opdatering:** 23 Jun 2026
> **Tests:** 1661/1661 passing (Common: 281, MessageFormatter: 351, Core4: 925, Consoles: 104)
> **Docs cleanup:** 24 forældede docs slettet — værdifuld viden ekstraheret til plan.md, AGENTS.md, mangler.md

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
| `DownloadService` | Content `OpenReadAsync` kaster, destination eksisterer/allerede låst, cancellation midt i write |
| `FileCompareService` | Store filer (> 100 MB) der aktiverer chunked algoritmer |
| `DiskSpaceValidator` | Utilstrækkelig plads, ikke-eksisterende drev, negativ capacity |
| `SessionStateService` | Concurrent save/delete, meget store resume sets |
| `HashService` | Blandede hash families, null stream fra content |
| `TempDirectoryHelper` | Concurrent cleanup, locked files, nested `.tmp` |
| `BackupScanner` | Items med null Content, null dates, ekstremt dybe paths |

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
| 🔴 Bør testes nu | 0 | ~~BackupPlanValidator, BinaryFileComparerSelector, Error paths i engine~~ ✅ ALLE FIXET |
| 🟡 Bør testes (større) | ~60 filer | TimeStamp (~18 filer: 13 readers + EarliestTimestampResolutionService), integration tests, error paths i øvrige komponenter, SignalInterruptEngine, Drive providers |
| 🟢 Nice-to-have | ~15 items | MediaDeviceContent, model defaults, edge cases |
| 🔶 Kosmetisk | 2 | Sidecar separator style, CollisionStreategy filename |
