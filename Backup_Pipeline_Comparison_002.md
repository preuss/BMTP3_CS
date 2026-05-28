# Backup Pipeline Comparison 002: Core4 Gap Analysis

> Genereret 28. maj 2026 baseret på audit af BMTP3.Core4 kodebase mod
> `Backup_Pipeline_Comparison_001.md` + implementeringshistorik.

---

## 1. Overordnet status

Core4 har **solid arkitektur** — DI (TryAdd), interfaces, single-pass multi-hash (9 algoritmer),
timestamp-resolution med EXIF/XMP/IPTC/GPS/QuickTime, collision resolution (Overwrite/Rename/Skip/Error),
sidecar generation (INI), og session state management med resume-strategier (Abort/Restart/Continue).

Dog er der **4 kritiske huller** der gør at engine ikke kan udføre en rigtig backup.

---

## 2. Pipeline Steps — faktisk implementeringsstatus

### Pre-flight

| Step | Core4 | Detaljer |
|---|---|---|
| Plan validation | ✅ Ja | `BackupPlanValidator.Validate(plan)`, men tier-gating blokerer n十二sten alt (se C4) |
| Initialize state | ✅ Ja | `BackupMemoryRecordRepository`, `BackupSessionKey`, `BackupProgress` |
| Prepare destination | ✅ Ja | `Directory.CreateDirectory` + `.bmtp3` |
| Open source traversal | ⚠️ Stub | `FileSystemTraversalFactoryStub` → `NotImplementedException` |
| Scan source | ⚠️ Stub | `BackupScannerStub` → `NotImplementedException` |
| Resume / Apply | ✅ Ja | `_sessionState.ApplyResumeAsync()` — fuldt implementeret |
| Filter pending | ✅ Ja | `FilterPendingRecords()` |
| Prepare temp directory | ✅ Ja | `TempDirectoryHelper` |
| Disk space check | ❌ Nej | |
| Source/output access probe | ❌ Nej | |

### Per-item

| Step | Core4 | Detaljer |
|---|---|---|
| Download/staging | ✅ Ja | `_downloadService.DownloadAsync()` — fuldt implementeret |
| Timestamp preservation på temp-fil | ❌ Nej | Valgfrit; metadata readers læser EXIF direkte |
| Earliest timestamp resolution | ✅ Ja | `_earliestTimestampService.ResolveEarliestAsync()` — EXIF/XMP/IPTC/GPS/QuickTime |
| Multi-hash computation | ✅ Ja | `_hashService.ComputeHashesAsync()` — 9 algoritmer i et pass |
| Collision resolution (rename med counter) | ✅ Ja | `CollisionHelpers.ResolveTargetPath()` — Overwrite/Rename/Skip/Error |
| Opret destinationsdirectory | ✅ Ja | `Directory.CreateDirectory` |
| Move file | ✅ Ja | `content.MoveTo()` via `MoveableFileContent` |
| Sidecar generation | ✅ Ja | `_sidecarService.WriteAsync()` — INI med timestamps + hashes |
| Update record status | ✅ Ja | `record.Status = Succeeded` |
| **Timestamp correction på destinationsfil** | ❌ **Nej** | `File.SetCreationTime`/`SetLastWriteTime` kaldes aldrig |
| Binary compare (identisk fil-detektion) | ❌ **Nej** | Ved Rename tjekkes ikke om eksisterende fil er identisk |
| Post-write verification | ❌ **Nej** | Ingen verifikation efter fil-flytning |

### Post-run

| Step | Core4 | Detaljer |
|---|---|---|
| **Post-run persistence** | ❌ **Mangler** | `_sessionState.SaveAsync()` **kaldes aldrig** efter loopet |
| Build result | ✅ Ja | `BackupResult` samles fra alle records |
| Cleanup tomme temp-mapper | ❌ Nej | |

---

## 3. Kritiske huller (C1–C4)

### C1 — Post-run persistence mangler

`_sessionState.SaveAsync()` kaldes aldrig efter `foreach`-loopet i `BackupEngine.RunAsync()`.
Records får `Status = Succeeded` men det gemmes ikke.

**Konsekvens:** Resume vil gen-behandle alle items næste gang. Session state er ubrugeligt.

**Fil:** `Engine/BackupEngine.cs` — ingen kald efter linje ~303

---

### C2 — SummaryStore er in-memory kun

`BackupMemorySummaryStore` gemmer `BackupSummary` i en `BackupSummary? _summary` field.
Der skrives aldrig til disk.

**Konsekvens:** Selv hvis C1 fixes, overlever data ikke proces-genstart.
Resume på tværs af kørsler er umuligt.

**Fil:** `State/BackupMemorySummaryStore.cs`

---

### C3 — Scanner og Traversal er stubs

`BackupScannerStub` og `FileSystemTraversalFactoryStub` kaster begge `NotImplementedException`.

**Konsekvens:** Engine kan ikke scanne nogen rigtig kilde. Ingen backup kan udføres.

**Filer:**
- `Scanner/BackupScannerStub.cs`
- `Traversal/FileSystemTraversalFactoryStub.cs`

---

### C4 — Validator blokerer næsten alle features

`BackupPlanValidator` har tier-gating (linje 75–137) der kaster `FeatureNotImplementedException`
for features der **faktisk er implementeret** i engine:

| Feature | Engine understøtter? | Validator tillader? |
|---|---|---|
| `CollisionStrategy.Overwrite` | ✅ Ja | ❌ Kun `Error` |
| `CollisionStrategy.Rename` | ✅ Ja | ❌ Kun `Error` |
| `CollisionStrategy.Skip` | ✅ Ja | ❌ Kun `Error` |
| `SidecarFormat.Ini` | ✅ Ja | ❌ |
| `IncludePatterns` | Sends til scanner | ❌ |
| `ExcludePatterns` | Sends til scanner | ❌ |
| `ComparisonHashAlgorithmTypes` | Sends til HashService | ❌ |
| `VerificationHashAlgorithmTypes` | Sends til HashService | ❌ |

Derudover er der **modstridende checks** på hash-algoritmer:
- Linje 58: kræver `ComparisonHashAlgorithmTypes` non-null + non-empty → ellers `BackupPlanArgumentException`
- Linje 114: kaster `FeatureNotImplementedException` hvis `ComparisonHashAlgorithmTypes` er non-null + non-empty

**Konsekvens:** Intet fungerende plan kan komme igennem til engine. Hele engine er låst.

**Fil:** `Engine/Validation/BackupPlanValidator.cs`

---

## 4. Vigtige huller (I1–I7)

### I1 — Timestamp correction på destinationsfil

Efter `MoveTo()` sættes `File.SetCreationTime`/`SetLastWriteTime` aldrig på destinationsfilen.
Filer får backup-tidspunkt i stedet for original authored/created time.

**Fil:** `Engine/BackupEngine.cs` — efter `content.MoveTo()`

---

### I2 — Binary compare i collision resolution

Når `CollisionStrategy.Rename` bruges og en fil findes på target-stien, tjekkes ikke
om den eksisterende fil er byte-identisk. Der oprettes unødigt dubletter (`fil_1.jpg`, `fil_2.jpg`).

**Fil:** `Engine/CollisionHelpers.cs`

---

### I3 — Post-write verification

Ingen verifikation efter fil-flytning. Korruption under transfer opdages ikke.

---

### I4 — Disk space pre-flight check

Backup kan starte og fejle midtvejs på fuld disk.

**Fil:** `Engine/BackupEngine.cs` — pre-flight sektion

---

### I5 — Source/output access probe

Backup kan starte med inaccessibel source/destination og fejle sent.

---

### I6 — Cleanup af tomme temp-mapper

Temp-mapper akkumuleres i `.tmp/`, renses aldrig.

---

### I7 — BackupMemoryRecordRepository har kun Add + GetAll

Ingen `Update` eller `Remove`. Blokerer ikke nu, men nødvendigt for repair/correction.

**Fil:** `Engine/Session/BackupMemoryRecordRepository.cs`

---

## 5. Nice-to-have / fremtid (N1–N7)

| # | Hul | Note |
|---|---|---|
| N1 | Destination inspection / sidecar hash read-back | Cross-run deduplication |
| N2 | Device metadata i sidecar (`[DeviceDetails]`) | Først når MTP understøttes |
| N3 | Timestamp preservation på temp-fil | Lav prioritet — metadata readers læser EXIF |
| **N4** | **Duplicate fil: `EarliestTimestampResolutionServiceAnother.cs`** | Identisk med `EarliestTimestampResolutionService.cs`, død kode |
| **N5** | **BackupRunner / ParallelBackupRunner ikke wired** | Implementeret + DI-registreret, men aldrig brugt af BackupEngine |
| N6 | Ingen tests | 3 tomme fake-filer i testprojekt |
| N7 | JSON sidecar mangler (kun INI) | `SidecarFormat.Json` kaster `NotImplementedException` |

---

## 6. Overraskelser / uoverensstemmelser med tidligere plan

1. **C4 (Validator blokerer alt)** — tidligere antaget som midlertidig skeleton,
   men i praksis gør den hele engine ude af stand til at køre nogen plan.

2. **C1 (Post-run persistence)** — aldrig nævnt i tidligere planlægning.
   001.md fangede det som eneste kritiske hul.

3. **N4 (Duplicate timestamp-fil)** — `EarliestTimestampResolutionServiceAnother.cs`
   er en 1:1 kopi, ikke registreret i DI.

4. **N5 (Runners ikke wired)** — engine kører sekventielt inline, men
   `BackupRunner`, `ParallelBackupRunner`, `LimitedParallelBackupRunner` er
   fuldt implementeret og DI-registreret, bare aldrig brugt.

---

## 7. Anbefalet rækkefølge

1. **C4 — Fix validator** så engine kan modtage en fungerende plan
2. **C1 — Post-run persistence** (`_sessionState.SaveAsync()` efter loop)
3. **C2 — File-backed SummaryStore** (JSON i `.bmtp3/`)
4. **C3 — Implementer scanner + traversal** (filesystem)
5. **I1 — Timestamp correction** på destinationsfil
6. **I2 — Binary compare** i collision resolution
7. Resten (I3–I7, N1–N7) efter behov
