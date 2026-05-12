# Core4 Master Synthesis (endelig konsolidering)

Dette dokument er den samlede, opdaterede syntese af:

- `CORE4_ARCHITECTURE_en.md` + `CORE4_PLAN_en.md`
- `CORE4_ARCHITECTURAL_COMPLETION_GUIDE.md`
- `COMPLETION_ROADMAP.md`
- `CORE4_COMPLETION_SUMMARY.md`
- `CORE_LEARNING.md`, `CORE2_LEARNING.md`, `CORE3_LEARNING.md`, `CORE4_LEARNING.md`
- `Lessons_Learned_Core*.md` + `Lessons_Learned_Comparison.md`
- Aktuel `BMTP3.Core4` skeleton-kode

Målet er at give én entydig beslutningstekst for Core4, så vi undgår modsatrettede regler fra de andre dokumenter.

## 1. Ikke-forhandlingsbare beslutninger

1. **MTP er altid sekventiel.** Ingen parallel transfer/buffering mod MTP-enheder.
2. **Filesystem kan være begrænset parallel.** Kontrolleret worker-pool, aldrig ubegrænset.
3. **Minimal sidecar oprettes umiddelbart efter succesfuld transfer.** Senere faser opdaterer sidecar.
4. **Error-policy er eksplicit og fase-specifik.** Ingen implicit “throw-and-pray”.
5. **Dry-run følger samme scan-/plan-path, men uden writes.**
6. **Progress er central, trådsikker og konsistent mellem sequential/parallel engine.**
7. **Design for idempotens og resume.** Sidecars og state bruges til sikker genkørsel.

## 2. Lessons learned, kondenseret

| Version | Bevar | Undgå |
|---|---|---|
| Core | Feature-bredde, temp-staging, path-sanitizing | God objects, tæt kobling, fail-fast uden recovery |
| Core2 | SoC, DI, metrics, cancel-støtte | Over-kompleks channel-arkitektur, race/deadlocks, “bolted-on” MTP-session |
| Core3 | Simpel sekventiel flow, robusthed, testbarhed | Sidecar for sent, utilstrækkelig dry-run, implicitte policies, manglende edge-case dækning |
| Core4 (mål) | Hybrid strategi + tydelige kontrakter | Modstridende regler mellem docs, halv-implementerede features |

## 3. Afklarede konflikter fra kildedokumenter

Der var modstrid mellem dokumenter (fx “transfer-fejl stopper alt” vs “log+continue”). Core4 følger disse **endelige regler**:

1. **Per-fil transfer-fejl:** markeres som failed, logges, backup fortsætter som default.
2. **Job stopper kun ved fatal/systemiske fejl** (fx invalid config, destination utilgængelig for hele job, konsekvent MTP-session-fail).
3. **StopOnError** er tilladt som override, men default er tolerant per-fil continuation.
4. **Sidecar skrives kun ved succesfuld transfer** (minimal sidecar), og “failed” registreres enten i job-run summary eller dedikeret failure-sidecar/tombstone efter valgt strategi.
5. **MTP session lifetime** dækker hele scan+transfer-fasen for enheden, ikke kun delmængder.

## 4. Datolæsning / metadata-strategi (final)

For datoer/timestamps i Core4 bruges:

1. **Primær:** managed metadata-læsning via `MetadataExtractor` (EXIF/XMP/QuickTime hvor muligt).
2. **Fallback:** `exiftool.exe -json` for filer hvor managed parsing fejler/er tvetydig.
3. **Sidste fallback:** filsystem-attributter.

### Timestamp-præcedens (anbefalet standard)

1. EXIF `DateTimeOriginal`
2. EXIF `CreateDate` / XMP `CreateDate`
3. QuickTime creation date (normaliseret til UTC med offset-håndtering)
4. Filsystem `LastWriteTimeUtc`
5. Filsystem `CreationTimeUtc`

Hvis valgt kandidat er invalid (epoch-artefakt, tom, out-of-range), gå videre til næste kandidat.

## 5. Arkitektur: lag og ansvar

### 5.1 API/Model-lag

- Kontrakter: `IBackupEngine`, `IBackupProgress`, `IFileProgress`
- Modeller: `BackupPlan`, `BackupResult`, `BackupItem`, enums
- Krav: klare, stabile contracts før implementation-detaljer

### 5.2 Scanner/Transfer/Sidecar-lag

- Scanner: `FilesystemItemScanner`, `MTPItemScanner`
- Transfer: `FilesystemFileTransfer`, `MTPFileTransfer`
- Sidecar: `JsonSidecarGenerator` (primær), evt. XML som alternativ
- Krav: relative paths bevares; destination dirs oprettes automatisk; atomic writes med `.tmp` + rename

### 5.3 Engine-lag

- `SequentialBackupEngine`: primær for MTP, fallback for FS ved forced sequential
- `LimitedParallelBackupEngine`: kun FS, begrænset DoP + backpressure
- `BackupEngineFactory`: vælger engine baseret på source type + plan

### 5.4 Infrastruktur

- `ProgressTracker` + evt. notifier til CLI/UI
- Session/repository for resume (tieret indføring)
- Retry/backoff-politikker

## 6. Eksekveringsflow (normativt)

## 6.1 MTP (sekventielt)

1. Validate plan + preflight
2. Open MTP session
3. Scan items (`await foreach`)
4. For hvert item: Transfer -> Minimal sidecar -> Optional enrichment (hash/meta/verify/timestamp)
5. Close session + finalize result

## 6.2 Filesystem (begrænset parallel)

1. Validate + scan producer
2. Transfer workers (N, bounded queue)
3. Sidecar write/update pr. item
4. Optional enrichments (konfigurerbart)
5. Finalize

## 7. Sidecar-kontrakt (minimal + enrichment)

### Minimal (skrives straks efter transfer)

- source path / relative path
- destination path
- bytes
- transfer timestamp (UTC)
- transfer status
- source type (filesystem/mtp)

### Enrichment (opdateres senere)

- hashes (source/destination)
- metadata (EXIF/XMP/QuickTime/file attributes)
- verification status
- timestamp correction result
- fejlhistorik pr. fase

**Skrivekrav:** altid atomisk (`.tmp` -> rename) for at undgå halvskrevne sidecars.

## 8. Error taxonomy og håndtering

| Fejlklasse | Eksempler | Default handling |
|---|---|---|
| Transient | timeout, busy device, midlertidig lock | retry med exponential backoff + jitter |
| Persistent item-level | corrupt fil, access denied på enkeltfil | mark failed, fortsæt |
| Fatal job-level | invalid config, destination utilgængelig globalt | stop job med tydelig failure reason |
| Optional feature failure | metadata/hash/verify fejl | log + fortsæt (item er stadig overført) |

Standard retry for MTP/transient transfer: **3 forsøg (1s, 2s, 4s)**, derefter fail for item.

## 9. MTP-session regler (kritisk)

1. Én aktiv session pr. device-job.
2. Keepalive under lange operationer.
3. Operation timeout pr. read.
4. Reconnect policy ved disconnect (begrænset antal forsøg).
5. Altid cleanup i `finally`.
6. Ingen parallel device reads.

## 10. Dry-run kontrakt

Dry-run må:

- scanne kilder
- evaluere collisions/policies
- simulere output paths og mappeoprettelse

Dry-run må ikke:

- kopiere filer
- skrive sidecars
- mutere destination/state

Output skal tydeligt vise “would copy / would write sidecar / would skip”.

## 11. Resume + idempotens

1. Item med succes-sidecar kan springes over ved resume (med policy-flag).
2. Failed/partial items kan genkøres selektivt.
3. Resume må aldrig antage implicit state; alt afgørende skal kunne udledes af sidecar + session store.
4. “Halv-implementeret resume” er ikke acceptabelt; tieres først når end-to-end scenarier er grønne.

## 12. Progress-kontrakt

Mindst disse metrics skal være korrekte:

- directories scanned, files discovered
- files processed/succeeded/failed/skipped
- bytes total/processed
- active file + active worker count
- phase + elapsed + throughput + ETA

Sampling ca. 1s som standard.

## 13. Realitetstjek: nuværende Core4 skeleton (vigtige gaps)

Aktuel kode viser bl.a.:

1. `BMTP3.Core4.csproj` targeter `net10.0` (ikke alignet med repo’s .NET 8/windows-retning).
2. `BackupEngine` er stadig `NotImplementedException`.
3. `SequentialBackupEngine` stopper efter scan + session state population.
4. `LimitedParallelBackupEngine`/`ParallelBackupEngine` er placeholders.
5. Scanner-interface findes, men fulde scanner-/transfer-/sidecar-implementeringer mangler.
6. Phases/enums mangler endnu de fulde optional-feature faser.

Konsekvens: dokumentet her er designmæssigt klarhed; implementation skal stadig bringes op til tier-krav.

## 14. Prioriteret roadmap (endelig)

## Tier 1 (blokkerende, “backup virker”)

- Plan validation + preflight
- `FilesystemItemScanner` + `MTPItemScanner`
- `FilesystemFileTransfer` + `MTPFileTransfer`
- `SequentialBackupEngine` end-to-end
- Minimal JSON sidecar lige efter transfer
- Grundlæggende progress + cancellation
- DI registration (`AddBMTP3Core4`)

**Exit-kriterie:** stabil scan->transfer->sidecar for FS + MTP.

## Tier 2 (robusthed + UX)

- Collision resolution som separat testbar komponent
- Dry-run komplet simulation
- Bedre progress events/notifier
- Tydelig error policy per fase

**Exit-kriterie:** edge-cases håndteres uden job-kollaps.

## Tier 3 (feature enrichment)

- Hashing (truly optional)
- Metadata extraction (MetadataExtractor primær)
- Verification
- Timestamp correction med præcedenspolitikken
- ExifTool fallback-path

**Exit-kriterie:** sidecars kan enriches deterministisk; fallback virker på problemfiler.

## Tier 4 (performance + resume)

- `LimitedParallelBackupEngine` for FS only
- Backpressure + global I/O limiter
- Resume persistence (fuldt implementeret)
- Profilering/tuning

**Exit-kriterie:** målbar FS speedup uden regressions i correctness.

## 15. Teststrategi (obligatorisk dækning)

1. **Unit tests** pr. komponent (scanner, transfer, sidecar, policy).
2. **Integration tests** for:
   - FS end-to-end
   - MTP flow med emulator/stub
   - cancellation mid-transfer
   - disk full / permission denied
   - sidecar atomicity
3. **Regression corpus** med kendte metadata edge cases:
   - EXIF mangler
   - QuickTime skæve dates
   - corrupt metadata
   - fallback til ExifTool

## 16. Definition of done for Core4

Core4 er “klar” når:

1. MTP backups er stabile i sekventiel mode.
2. FS backups virker både sequential og limited parallel.
3. Sidecars oprettes tidligt, opdateres korrekt, og skrives atomisk.
4. Metadata/timestamp-politik er implementeret inkl. fallback-regler.
5. Progress, cancellation, error handling og resume opfører sig konsistent.
6. Testsuite dækker de kritiske failure paths og edge cases.

---

Denne version erstatter tidligere, kortere syntese og indeholder nu både lessons, konfliktopløsning, metadata/timestamp-politik, skeleton-gap-analyse og en entydig implementeringsrækkefølge.
