# Mangler / Issues

> **Seneste opdatering:** 21 Jun 2026
> **Tests:** 773/773 passing (Common: 281, Core4: 388, Consoles: 104)

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

---

## 🔴 Test coverage huller i Core4

### Prioritet 1 — Bør testes nu

#### 1A. BackupPlanValidator — 0 tests
**Fil:** `BMTP3.Core4/Engine/Validation/BackupPlanValidator.cs`

Tier 1-3 validering af `BackupPlan`. For nylig ændret (validator gates fjernet). Utestet — kan lade ugyldige plans igennem.

**Hvad mangler:**
- Tier 1: null/empty `SourcePath`, `Destination`, invalid `SourceType`, missing required fields
- Tier 2: cross-field validering (f.eks. source path eksisterer, destination gyldig)
- Tier 3: business rules (f.eks. `CustomOutputPattern` kræver `OutputStructureStrategy.Custom`)
- Kombinationer af valideringsfejl

---

#### 1B. BinaryFileComparerSelector — chunked algoritmer aldrig testet
**Filer:** `Engine/Compare/Algorithms/` (4 chunked comparers) + `BinaryFileComparerSelector.cs`

Eksisterende `FileCompareServiceTests` laver altid små filer (< 1 MB) → `WholeFileSequenceEqualBinaryComparer` vælges altid. De 4 chunked algoritmer er **aldrig blevet eksekveret** i en test:
- `ChunkedVectorBinaryComparer`
- `ChunkedSequenceEqualBinaryComparer`
- `ChunkedEightByteBinaryComparer`
- `ChunkedAvx2BinaryComparer`

**Hvad mangler:**
- Test der tvinger selection til hver chunked algoritme (f.eks. via filstørrelse)
- Test af selection logic i `BinaryFileComparerSelector` direkte
- Edge cases: filstørrelse præcis på grænsen mellem algoritmer

---

#### 1C. Error paths i BackupEngine — recovery utestet
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

#### 2A. TimeStamp subsystem — ~45 filer, 0 tests
**Namespace:** `BMTP3.Core4/Engine/TimeStamp/`

Komplet metadata extraction pipeline (Exif, XMP, GPS, IPTC, QuickTime, filesystem timestamps; parsere; candidate resolution; `EarliestTimestampResolutionService`).

**Helt utestet:** alle readers, parsers, candidates, resolution service.

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
| 🔴 Bør testes nu | 3 | BackupPlanValidator, BinaryFileComparerSelector, Error paths i engine |
| 🟡 Bør testes (større) | ~60 filer | TimeStamp (~45), integration tests, error paths i øvrige komponenter, SignalInterruptEngine, Drive providers |
| 🟢 Nice-to-have | ~15 items | MediaDeviceContent, model defaults, edge cases |
| 🔶 Kosmetisk | 1 | Sidecar separator style |
