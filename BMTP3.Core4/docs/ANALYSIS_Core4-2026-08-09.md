# Core4 Analyse — 2026-08-09

> **Formål:** Stor gennemgang af hele Core4. Hvert subsystem er analyseret af en selvstændig agent.
> **Metode:** 15 parallelle explore-agenter, en per subsystem. Alle fund verificeret mod koden.
> **Princip:** "Koden vinder" (AGENTS.md doc-conflict-resolution).

---

## Indholdsfortegnelse

- [1. Overblik](#1-overblik)
- [2. Analyserede subsystemer](#2-analyserede-subsystemer)
- [3. 🔴 Høje fund (prioriteret)](#3--høje-fund-prioriteret)
- [4. 🟠 Mellem fund](#4--mellem-fund)
- [5. 🟡 Lave fund / KISS / DRY / test-huller](#5--lave-fund--kiss--dry--test-huller)
- [6. ✅ Solidt — intet skal ændres](#6--solidt--intet-skal-ændres)
- [7. 📋 Detaljeret handlings-plan for høje fund](#7--detaljeret-handlings-plan-for-høje-fund)
- [8. Status / næste skridt](#8-status--næste-skridt)

---

## 1. Overblik

Core4 (`BMTP3.Core4/`) er backup-motoren. Agendaen blev analyseret subsystem for subsystem:

| Subsystem | Mapper/filer | Agent |
|---|---|---|
| Engine (orkestrering) | `Engine/BackupEngine.cs` | Agent 01 |
| Traversal | `Traversal/` + `Storage/SourceConnector.cs`, `ConnectedFileSystemSource.cs` | Agent 02 |
| Hashing | `Hashing/` inkl. `Crypto/` | Agent 03 |
| Downloader | `Engine/Downloader/` | Agent 04 |
| Compare | `Engine/Compare/` inkl. `Algorithms/` | Agent 05 |
| Sidecar | `Engine/Sidecar/` inkl. `Writers/`, `Document/` | Agent 06 |
| Session/State | `Engine/Session/` + `State/` | Agent 07 |
| Strategies | `Engine/Strategies/` | Agent 08 |
| Index | `Engine/Index/` | Agent 09 |
| TimeStamp | `Engine/TimeStamp/` (Candidates, Parsers, Readers, Definitions) | Agent 10 |
| Scanner/Validation/DiskSpace | `Scanner/`, `Validation/`, `DiskSpace/`, `TempDirectoryHelper` | Agent 11 |
| Storage/Devices/DriveDiscovery | `Storage/`, `Devices/`, `DriveDiscovery/` | Agent 12 |
| Helpers/DI/Infra/Signals | `Helpers/`, `DependencyInjection/`, `Infrastructure/`, `SignalInterrupts/` | Agent 13 |
| Api/Models/HashService | `Api/Models/`, `Api/`, `Engine/Hashing/` | Agent 14 |

---

## 2. 🔴 Høje fund (prioriteret)

| # | Fund | Sted |
|---|------|------|
| **H1** | **MTP-disconnect når aldrig `MtpDeviceDisconnectedException`**: Den indre catch (`catch (IOException ex) when (ex.InnerException is COMException comEx)` → `throw;`) rethrower som rå `IOException`, men den ydre COM-catch matcher kun `COMException` direkte. Derudover virker "stop straks" kun når `StopOnError=true` — med `StopOnError=false` markeres hver efterfølgende vare bare som Failed. | `BackupEngine.cs:331-334` + `:673` |
| **H2** | **Throttler ER tilbage i hashing hot loop** i alle 3 generatorer — modsiger AGENTS.md ("fjernet") og arkitekturreglen ("Throttler fjernet fra hot loop ... styres af BackupEngine"). Per 4–8 MB chunk der dobbelt-throttler med engine-laget. | `ParallelStreamHashGenerator.cs:107`, `StreamHashGenerator.cs:79`, `PooledStreamHashGenerator.cs:94` |
| **H3** | **ChunkedEightByteBinaryComparer kaster** `ArgumentOutOfRangeException` på sidste chunk når fil-størrelse ikke er et multiplum af 8: `MemoryMarshal.Cast<byte,long>` på en span med længde 1-7 bytes → exception. Latent (næppe valgt på moderne HW), men korrektheds-feji. | `Compare/Algorithms/ChunkedEightByteBinaryComparer.cs:7` |
| **H4** | **QuickTime-vejen er død** i TimeStamp: `GetString(DateTime)` → format `"ddd MMM dd HH:mm:ss yyyy"`, som IKKE findes i nogen parser. "EXIF → CreateDate → QuickTimeCreated → filesystem"-kæden er kun nominel — QuickTime giver 0 konkurrenter. | `Engine/TimeStamp/Readers/QuickTime*` + `TagGroups.cs` |
| **H5** | **PipelinedDownloadService er INAKTIV** — aktiv DI er `DownloadService` (sekventiel). AGENTS.md:210 er forældet. Pipeline har ZERO tests + manglende COMException-wrap ved open + `PreallocationSize` kan kaste på non-NTFS. | `ServiceCollectionExtensions.cs:38-39`, `PipelinedDownloadService.cs:16` |
| **H6** | **Test-gab på Readers + EarliestTimestampResolutionService**: ingen unit/integration tests findes — H4 ville være opdaget om der var. | `BMTP3.Core4.Tests/Engine/TimeStamp/` |

---

## 3. 🟠 Mellem fund

| # | Fund | Sted |
|---|------|------|
| **M1** | MTP-skip øger ikke `FilesSkipped` (line 344) — inkonsistent med collision-skip (line 461-464) | `BackupEngine.cs:344` |
| **M2** | Catch-all per item (571-587) sluger FATAL-fejl (OOM, engine-invariant) ved `StopOnError=false` | `BackupEngine.cs:571-587` |
| **M3** | **Sidecar-fejl bliver item-Failed** (optional-taxonomy-brud): `SidecarService` logger warning men rethrower; engine markerer varen `Failed`. Skulle være "log + fortsæt". (Samme brud på index — se M6.) | `BackupEngine.cs:518,571-583` + `SidecarService.cs:42-49` |
| **M4** | Chunked test-hul: alle test-filer < 1MB, grænsen er 10MB → chunked-stien og alle 5 `Algorithms/` er **uprøvede**. `MemoryMarshal.Cast`-bug H3 ville blive opdaget her. | `FileCompareServiceTests.cs` |
| **M5** | Compare-Selector: `GetType().Assembly != null` altid sandt no-op; AVX1 får AVX2-comparer (fallback til SequenceEqual i stedet for Vector); EightByte+Default unreachable → KISS til 3 stier | `BinaryFileComparerSelector.cs:47-57` |
| **M6** | **Index-skrivefejl fejler hele backupen** (optional-taxonomy-brud): `WriteAsync` kaldes uden try/catch i engine — både happy path og cancel-banen kan maskere OCE | `BackupEngine.cs:662-665, 699-710` |
| **M7** | **`summary.SessionId` verificeres ikke** mod `sessionKey.SessionId` ved load — 48-bit filkollision kan anvende forkert summary | `SessionStateService.cs:31` |
| **M8** | **O(n²)-duplicate-guard** i `BackupMemoryRecordRepository.Add` (`_records.Any()` pr. add) — hot-spot ved store mapper | `BackupMemoryRecordRepository.cs:12` |
| **M9** | MTP per-fil `mid-read COMException` i aktiv `DownloadService` er rå → kan påvirke hele session (Pipelined wrapper alt). Inkonsistens mellem de to services. | `Downloader/DownloadService.cs:34` |
| **M10** | **SourceConnector kan ciffer**: `MediaDevice.GetDevices().First` → `InvalidOperationException` i stedet for `MediaDeviceException` | `MediaDeviceInfo.cs:41`, `SourceConnector.cs:44-51` |
| **M11** | MTP-I/O-dedrag (`0x802A0000` detektion) kategoriserer ALLE WPD-errors som "device disconnected" — per-file `WPD_E_OBJECT_NOT_FOUND` inkl. aborter | `BackupEngine.cs:860-863` |
| **M12** | `Enum.IsDefined` mangler på `ItemIdScope` i BackupPlanValidator (de 9 andre enums er valideret) | `BackupPlanValidator.cs` |
| **M13** | Semantik-drift: `{relativePath}`/`{sourceRelativePath}`/`{sourceStructure}` har forskellig betydning afhængig af caller (kun mappe vs fuld relativ dog) | `TargetPathResolver.cs:55` vs `RenameCollisionResolver.cs:144` |

---

## 4. 🟡 Lave / KISS / DRY / test-huller

- **Dead code ~500 linjer**: `Uuid7.cs` (kun test), `Crypto/BouncyCastle*` + `SharpHashMD5` (ingen reference), `Models/Enums/BackupPhase.cs`, `FileFormatValuesFactory.NotYetImplemented`, `Parsed<T>`, `TimestampCandidateFactory.FromRaw*`.
- **`CreateAlgorithm`-switch tripliceret** i 3 hash-generatorer (DRY).
- `ActionProgress` mangler null-guard på `action`.
- `Guard` underudnyttet (2 call-sites) — adopt or remove.
- `ArrayPool.Return` uden `clearArray:true` i `ChunkedBinaryFileComparer` (inkonsistent med B13).
- INI-writer mangler escaping (`=`, nye linjer, `#` i værdier).
- Hash-tests er selvreferentielle for 3/9 algoritmer (ingen known-answer vectors — FIPS202 vs Keccak ikke bevist).
- `ParallelStreamHashGenerator` + `PooledStreamHashGenerator` har ZERO direkte tests.
- `AGENTS.md:140` (DI), throttle, DOP `/4` vs kode `/3` — alle stale.
- `BackupJsonSummaryStore` mangler fsync; ikke-cancellation-tjek ved OpenRead.
- Status-mapping inkonsistens: catalog `Pending→"Pending"` vs engine→`Skipped`.
- `SignalInterruptEngine` dispatch-logik 0 tests (mangler.md 2D).
- `DiskSpaceValidator` utilstrækkelig-space-branch 0 tests (I/O fake nødvendig).
- DI-tests kun smoke (konkrete type ikke assert).
- `ProgressStartup` mangler test for `ResultNeverExceedsMaxLength` edge-liste — dækkes dog af FileNameColumnTests (tilføjet 2026-08-09).

---

## 5. ✅ Solidt — intet skal ændres

- **Progress-determinisme**: 0 × `new Progress<T>` i Core4 — kun `ActionProgress<T>`/`TransformProgress<T>`. Verificeret af 3 agenter.
- **Session-atomicitet**: `SaveAsync` i `finally` med exception-guard.
- **Content-må-al-drig-være-null**: garanten via konstruktor (`BackupItem.cs`) + engine-guard.
- **B7/B9/B10/B13/B15/B47** — verificeret til stede i koden.
- **9 hash-algoritmer korrekt**: FIPS202 padding 0x06 vs Keccak 0x01 — korrekt skene via SharpHash.
- **Downloader**: buffer-ownership, SingleReader/SingleWriter, timestamps efter dispose — korrekt.
- **TimeStamp-parsere/fabrik**: +340 parser-tests + ~210 candidate-tests, invariante tests i orden.
- **BackupPlan defaults, SourceType nullable, ItemIdScope, Resume (dest-in-key) med works — korrekt.
- **45 validator-tests, 17 TempDirectoryHelper-tests, 230+ TimeStamp-tests** — stærke pakker.
- **HashService** null-stream-wrap → `BackupHashException` + test.

---

## 6. 📈 Detaljeret handlings-plan for høje fund

### H1 — MTP-disconnect når aldrig `MtpDeviceDisconnectedException`

**Sted:** `BackupEngine.cs:331-334` (indre catch → `rethrow as IOException`), `:673` (ydre `catch (COMException)`).

**Analyse:**
1. Den indre catch fanger `IOException` med `InnerException is COMException`, og ved `IsMtpDeviceDisconnectError` kalder den `throw;` — det giver den ydre catch en `IOException`, ikke en `COMException`.
2. Den ydre catch (line ~673) matcher kun rå `COMException` → den indpakkede sag kategoriseres aldrig som MTP-disconnect.
3. Ved `StopOnError=false` (line 571-587) kommer per-item catch-all til at tage den og kun markere `Failed` → alle efterfølgende vare prøver vil fejle en for én.

**Fix:**
```csharp
catch (IOException ex) when (ex.InnerException is COMException comEx && _isMtpDeviceDisconnectError(comEx))
{
    throw new MtpDeviceDisconnectedException("MTP device disconnected.", ex);
}
```

**Verificeres:**
- Ny test i `BackupEngineErrorPathTests.cs` that masks: fake content OpenReadAsync kaster `IOException` med `inner COMException` + `HResult 0x802A0000-...` → forventet `MtpDeviceDisconnectedException` i result/catch.
- Genbru: `MtpDeviceDisconnectedException` er final path i result-aggregeringen.

### H2 — Throttler er i hashing hot loop

**Sted:** `ParallelStreamHashGenerator.cs:107`, `StreamHashGenerator.cs:79`, `PooledStreamHashGenerator.cs:94`.

**Analyse:** AGENTS.md (koden vinder^) siger "Throttler fjernet fra hot loop", men git-commit `b649fd1` re-enablede den. Arkitekturreglen: throttling skal styres af `BackupEngine` per item, ikke per chunk.

**Beslutning at træffe:** Er per-chunk throttle en feature eller et bug?
- Hvis det er design (rate-limit per chunk): opdater AGENTS.md + mangler.md til at matche koden.
- Hvis det er et al: fjern `await throttler.WaitAsync()` fra alle 3 generator-hotloops, behold kun per-item-kald i `BackupEngine.cs:348,405`.

**Verificeres med:** eksisterende `ThrottlerTests` + nye hash-generator-tests der assert ordder ingen pause pr. chunk (fake delay = 0 → NoOp).

### H3 — ChunkedEightByteBinaryComparer kaster på sidste chunk

**Stil:** `EighthByteBinaryComparer.cs:7` (`MemoryMarshal.Cast<byte,long>`).

**Analyse:** Sidste chunk kan være 1–7 bytes for enhver fil hvis total længde ikke er delelig med 8 → `MemoryMarshal.Cast<byte,long>` kaster `ArgumentOutOfRangeException`.

**Løsning (foretrukket): Slet EightByte-compareren helt.** Den er unreachable i production (`BinaryFileComparerSelector` aldrig giver den — modern HW får AVX2/Vector; fallback kan tage `ChunkedSequenceEqual`). Dette er convex med KISS-fix #M5 (reducér selector til 3 stier).

**Alternativ fix:** Guard på `span.Length % 8 != 0` → fallback til scalar sammenligning.

**Verificeres:** Ny test med fil-længde fx. `64KB + 3 bytes` (via direkt comparer call) → ingen exception.

### H4 — QuickTime-vejen er død

**Stil:** `Readers/QuickTimeTimestampReader.cs`, `QuickTimeMovieHeaderTimestampReader.cs`, `QuickTimeMetadataHeaderTimestampReader.cs`, `Definitions/TagGroups.cs:71-86`.

**Analyse:**
- `QuickTimeMetadataReader` gemmer `DateTime` via `_epoch.AddTicks(...)`.
- `DirectoryExtensions.GetString(DateTime)` → `dt.ToString("ddd MMM dd HH:mm:ss yyyy")`.
- Det formatst findes ikke i nogen `DateTimeParser` → QuickTime-headers producerer 0 kandide.
- Killoint: hver metode i movie/meta reader skal seja 'computing' korrekt (TimestampTag + EpochType / Typed desc).

**Løs (2A-komm):**
- Movie: brug `TimestampTag` (`TagCreated`/`TagModified` fra MetadataReader) med `EpochType.MacLegacy` (QuickTime-basis 1904) — vil som `FromRawTimestamp` i `TimestampCandidateFactory`.
- Metadata: udlæses `Timestamp` som seconds-integer og bygges konkurrenmen via `Factory.FromRawTimestamp(...)`.
- Efter: test med reelt `reale filer` (mangler.md 2A) + unit-test med mock directory-entry.

### H5 — PipelinedDownloadService INAKTIV + ulåst

**Stil:** `Engine/Downloader/PipelinedDownloadService.cs`, `ServiceCollectionExtensions.cs:35=39`, `DownloadService.cs`.

**Analyse genkendels:**
- Aktiv DI = `DownloadService` (sekventiel), ikke Pipeline.
- AGENTS.md:140 sier Pipeline er aktiv — DOKUMENT-feil.
- Pipeline: Zero direkte tests. Bugs in pipeline-er kendt men utestet.

**Handlingsplan:**
1. **Test-first**: `PipelinedDownloadServiceTests.cs` — dæk: copy-init-+progress, timestamps-after-dispose, COMException-open → wrapped IOException, consumer-fail → producer-fail (B2), buffer-ownership (B3/B4 drain), pre-cancelled token, non-mult8-chunks.
2. **Fix**: wrap COMException ved open `(PipelinedDownloadService.cs:16)` — matcher `DownloadService.cs:16-26`.
3. **Optional**: beslut alle i agent.md (opentage).
4. Før du gø-svigt i produktion → kør pipeline live (sammel benchmark `PlayAroundProject` fjerne side-by-side).

### H6 — Test-gap på Readers + EarliestTimestampResolutionService (TimeStamp)

**Sted:** `Engine/TimeStamp/Readers/*`, `EarliestTimestampResolutionService.cs`.

**Analyse:** +230 tests eksister for Parsers + Candidates, men:
- ingens `Readers/`-test nogen st.
- Ingen `CompositeTimestampReader`/`EarliestTimestampResolutionService`-test.
- H4 fej len er derfor skjult.

****Handlingsplan:**
1. `CompositeTimestampReaderTests.cs` med fake `ITimestampReader`-implementering header (stub/throw → fallback til FS).
2. `EarliestTimestampResolutionServiceTests`: PickBest/MostIn class, HasValidDate, SameDayTolerance 1, mutation-priority (offset>time, etc.).
3. `Exif/Iptc/Xmp/Gps`-reader tests — brug min. små prøver i GIF/JPG/TIFF (ikke-fi-lere filer, fake binary via MemoryStream).
4. QuickTime fix (H4) — en integration-test med en reekst MP4 med `creation_time` global + `media_header` creation_time.

---

## 7. Status / næste skridt

### Rækkefølge (prioritert)

1. 🔴 **H1** — MTP-wrap → `MtpDeviceDisconnectedException` (kort; 10 min; test)
2. 🔴 **H3** — EightByte-comparer: slet + KISS-selector til 3 stier (kort)
3. 🔴 **H2** — throttle-beslutning: re-basér til engine-of-chunk (kræver beslutning)
4. 🔴 **H6+H4** — Readers + ResolutionService-tests → QuickTime-fix (kræver reelle filer/2A-work)
5. 🔴 **H5** — PipelinedDownloadService-tests + COM-wrap + AGENT-status
6. 🟠 **M3+M6** — optional-taxonomy (sidecar+index) som "log+continue"
7. 🟠 **M7+M8** — SessionId-guard + O(1)-dedup
8. 🟠 **M9-M12** — DownloadService COM-wrap, MediaDeviceException, WPD-detection (whitelist), ItemIdScope-validation
9. 🟡 Renhed — slet >950 lin ded-node; opdatér AGENTS.md (DI, throttle, DOP)
10. 🟡 Tests — Parallel/Poool dir; chunked < ; DiskSpace I/O; SignalInterruptEngine dispatch

### Doc-opdateringer nødvendige (AGENTS.md)

- `IDownloadService` → DownloadService (aktiv DI) — endre `Pipeline` → reuploader.
- Throttler ST value i hot-loop per chunk.
- DOP forkert (`/3` i kode vs `/4` i doc).
- `Pipelined` tests 1668 total status → opdateres efter H1-H6.

---

*Auto-genererede: 2026-08-09. Kode last-check: 25 Jun 2026 (AGENTS.md).*