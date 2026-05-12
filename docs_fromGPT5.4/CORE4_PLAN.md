# Core4 - Implementeringsplan

**Formål:** Dette dokument oversætter Core4-arkitekturen til en praktisk implementeringsrækkefølge på højt niveau.

**Sidst opdateret:** 2026-05-12

**Status:** Normaliseret mod det nuværende `BMTP3.Core4`-skeleton og de opdaterede Core4-dokumenter.

---

## 1. Hvordan planen skal bruges

Denne fil er et **plan- og prioriteringsdokument**.

Brug den til at:

- afgøre hvad der skal bygges først
- se hvilke klasser der hører til hvilke tiers
- skelne mellem need-to-have og nice-to-have
- holde implementeringsrækkefølgen på linje med de normaliserede Core4-beslutninger

Brug ikke denne fil som eneste kilde til præcise signaturer. Til præcise contracts og step-by-step implementation skal den læses sammen med:

1. `docs\CORE4_IMPLEMENTATION_GUIDE_DA.md`
2. `docs\CORE4_ARCHITECTURE.md`
3. `docs\CORE4_MASTER_SYNTHESIS.md`
4. `docs\Core4_Master_Architecture.md`

---

## 2. Hvorfor Core4 findes

Core4 findes fordi de tidligere generationer hver især gjorde noget rigtigt, men ingen af dem er den endelige løsning:

- **Core** viste produktets retning, men er legacy og ikke længere målarkitekturen.
- **Core2** havde de rigtige feature-ambitioner, men for skrøbeligt parallelt fundament.
- **Core3** bragte enkelheden tilbage, men er for ufuldstændig og begrænset som slutløsning.

Core4 skal kombinere:

- Core3's klarhed
- Core2's feature-ambition
- eksplicit source-håndtering
- kontrollerede performance-forbedringer kun hvor det er sikkert

Kernesætningen er stadig:

**Build it simple. Build it right. Then optimize smartly.**

---

## 3. Fastlåste beslutninger før implementering

Disse valg er allerede normaliseret og skal ikke genforhandles under implementering.

| Emne | Beslutning |
|---|---|
| Source selection | Brug `BackupPlan.SourceType` og `BackupPlan.Source` |
| Result type | Brug `BackupResult`, ikke `BackupJobResult` |
| Progress contract | Brug det aktuelle `IBackupProgress` + `IFileProgress` skeleton |
| Scanner contract | Brug `IAsyncEnumerable<BackupItem>` |
| Sidecar-navn | Brug `.sidecar.json` |
| Metadata-strategi | `MetadataExtractor` først, `ExifTool` fallback, filsystem-attributter sidst |
| Timestamp-strategi | Brug det normaliserede metadataresultat, ikke skjult engine-fallback |
| MTP execution | Altid sekventiel |
| Parallel execution | Kun filsystem, og først når Tier 4 nås |
| `MaxDegreeOfParallelism` | `null = auto`, `1 = sekventiel`, `> 1 = begrænset parallel senere` |
| Feature flags | Brug boolean flags på `BackupPlan` |
| Gamle planfelter | Genindfør ikke `DeviceId`, `SourceDirectory`, `OutputDirectory`, `WriteSidecar`, `HashTypes` eller `-1`-semantik |

Hvis en feature kræver mere konfiguration, end der hører hjemme i `BackupPlan`, skal det ligge i `Core4Options` eller intern policy - ikke i en muteret public contract.

---

## 4. Prioritetsmodel

| Prioritet | Betydning | Typiske eksempler |
|---|---|---|
| **NEED** | Nødvendigt for en minimal korrekt Core4 | sekventiel engine, filsystem-backup, MTP-backup, sidecars, progress snapshots |
| **SHOULD** | Meget vigtig efter minimum virker | error shaping, retries, dry-run-korrekthed, bedre diagnostics |
| **NICE** | Værdifulde features, men ikke første virkende Core4 | hashing, metadata, verification, timestamp correction |
| **LATER** | Først relevant når de tidligere lag er stabile | begrænset parallel filsystem, persistence/resume, avanceret UI/rapportering |

Tier-mapping:

- **Tier 1** = NEED
- **Tier 2** = SHOULD
- **Tier 3** = NICE
- **Tier 4-6** = LATER

---

## 5. Kondenserede lessons fra tidligere cores

### 5.1 Fra Core2

Core4 må ikke gentage:

- channel-tung pipeline-kompleksitet
- skjult kobling mellem stages
- svært debugbar task-orkestrering
- MTP-håndtering der afhænger af parallel infrastruktur
- performance før korrekthed

### 5.2 Fra Core3

Core4 skal eksplicit rette:

- mistede relative paths
- backup-stop ved enkelte filfejl
- mangelfuld progress-adfærd
- mangelfuld dry-run-adfærd
- for sen sidecar-generering

### 5.3 Konsekvens for Core4

Derfor starter Core4 med:

- en klar sekventiel engine
- en streaming scanner-contract
- eksplicit source-typing
- eksplicit sidecar-generering
- optionelle avancerede features først efter minimum virker

---

## 6. Implementeringsfaser

Faserne nedenfor er den anbefalede build order.

### Fase 0 - Contract alignment og state baseline `[NEED]`

**Mål:** Sørg for at public og intern Core4-baseline er konsistent, før engine færdiggøres.

Implementér eller normalisér:

- `Api/IBackupEngine.cs`
- `Api/IBackupProgress.cs`
- `Api/IFileProgress.cs`
- `Models/BackupPlan.cs`
- `Models/BackupResult.cs`
- `Models/BackupItem.cs`
- `Models/Enums/*`
- `Engine/Validation/BackupPlanValidator.cs`
- `Engine/Validation/BackupPlanValidationException.cs`
- `Engine/State/BackupSessionState.cs`
- `Engine/State/BackupSessionStateKey.cs`
- `Engine/State/BackupSessionStateKeyFactory.cs`
- `Engine/State/IBackupSessionStateStore.cs`
- `Engine/State/InMemoryBackupSessionStateStore.cs`

Færdig når:

- skeleton-contracts er internt sammenhængende
- validation fanger ugyldig planform tidligt
- engine kan åbne en session state og acceptere discovered items

### Fase 1 - Minimal filsystem-backup `[NEED]`

**Mål:** Gør Core4 i stand til at gennemføre en reel sekventiel filsystem-backup.

Implementér:

- `Scanner/IBackupScanner.cs` i engine-flowet
- `Scanner/Filesystem/FileSystemScanner.cs`
- `Transfer/IFileTransfer.cs`
- `Transfer/Filesystem/FilesystemFileTransfer.cs`
- `Engine/Sequential/SequentialBackupEngine.cs` ende-til-ende transfer-sti
- destination-beregning i engine (`ResolveDestination(...)` i den normaliserede guide)

Adfærdskrav:

- bevar relative paths
- opret destinationsmapper efter behov
- håndter `SkipExisting`, `CollisionStrategy` og `StopOnError`
- markér individuel item success/skip/failure tydeligt
- returnér korrekt `BackupResult`

Færdig når:

- en filsystem-kilde kan scannes og kopieres sekventielt
- én fejlet fil ikke stiltiende ødelægger session state
- `BackupResult` afspejler det faktiske arbejde

### Fase 2 - Sidecar- og progress-baseline `[NEED]`

**Mål:** Færdiggør det minimale bruger-synlige flow omkring hver succesfuld fil.

Implementér:

- `Sidecar/SidecarData.cs`
- `Sidecar/ISidecarGenerator.cs`
- `Sidecar/JsonSidecarGenerator.cs`
- `Sidecar/SidecarEnrichment.cs` som minimal placeholder hvis nødvendigt
- `Progress/ProgressTracker.cs`
- `DependencyInjection/Core4Options.cs`
- `Engine/BackupEngineFactory.cs`
- `DependencyInjection/ServiceCollectionExtensions.cs`

Regler:

- sidecar-sti er `item.DestinationPath + ".sidecar.json"`
- sidecar skrives direkte efter succesfuld transfer
- progress bruger den aktuelle public `IBackupProgress`-shape
- update-frekvens skal være begrænset, så output ikke støjer

Færdig når:

- hver succesfuld transfer kan afgive sidecar
- engine producerer stabile progress snapshots
- basal DI kan resolve en fungerende Core4-engine

### Fase 3 - Sekventiel MTP-support `[NEED]`

**Mål:** Luk Tier 1 rigtigt ved at understøtte media devices sikkert.

Implementér:

- `Scanner/MTP/MtpScanner.cs`
- `Transfer/MTP/MTPFileTransfer.cs`
- source-selection via `BackupPlan.SourceType`
- interne MTP session/helper-klasser efter behov for STA/COM-sikker adgang

MTP-regler:

- altid sekventiel
- keepalive mindst hver 30. sekund
- per-operation timeout på 60 sekunder
- retry-backoff 1s, 2s, 4s

Færdig når:

- MTP scan og transfer virker uden parallel engine
- Core4 Tier 1 er færdig for både filsystem og media-device-kilder

### Fase 4 - Robusthed og tillid `[SHOULD]`

**Mål:** Gør den minimale engine robust nok til realistisk brug.

Fokusområder:

- stærkere error mapping
- konsistent cancellation handling
- klarere diagnostics
- korrekt dry-run-semantik
- tydelig collision-adfærd
- mere robust sidecar-skrivning

Vigtig begrænsning:

- tilføj ikke nye public DTOs bare for at løse interne robusthedsproblemer

Færdig når:

- engine opfører sig forudsigeligt under normale driftsfejl
- diagnostics er forståelige
- dry-run er meningsfuldt billigere og sikrere end en reel kørsel

### Fase 5 - Optionelle enrichment-features `[NICE]`

**Mål:** Tilføj de features som gør Core4 mere komplet uden at ændre Tier 1-baseline.

Implementér:

- `Features/Hashing/IItemHasher.cs`
- `Features/Hashing/HashResult.cs`
- `Features/Hashing/FileHasher.cs`
- `Features/Metadata/IMetadataReader.cs`
- `Features/Metadata/ExtractedMetadata.cs`
- `Features/Metadata/MetadataExtractorReader.cs`
- `Features/Metadata/ExifToolMetadataReader.cs`
- `Features/Verification/IIntegrityVerifier.cs`
- `Features/Verification/VerificationResult.cs`
- `Features/Verification/FileIntegrityVerifier.cs`
- `Features/Timestamp/ITimestampCorrector.cs`
- `Features/Timestamp/TimestampCorrectionResult.cs`
- `Features/Timestamp/FileTimestampCorrector.cs`

Feature-rækkefølge:

1. metadata
2. timestamp correction
3. hashing
4. verification
5. rigere sidecar enrichment

Metadata-regel:

- udfyld først filsystem-afledte værdier
- forsøg derefter managed parsing via `MetadataExtractorReader`
- hvis nødvendigt og aktiveret via `Core4Options.UseExifToolFallback`, brug `ExifToolMetadataReader`

Færdig når:

- Tier 3-features er optionelle, testbare og klart gated
- engine skjuler ikke feature-fejl bag stille fallback-adfærd

### Fase 6 - Begrænset parallel filsystem-engine `[LATER]`

**Mål:** Tilføj performance sikkert uden at gentage Core2's fejl.

Implementér:

- `Engine/LimitedParallel/LimitedParallelBackupEngine.cs`
- factory-routing via `BackupEngineFactory`
- kontrolleret brug af `MaxDegreeOfParallelism`
- progress-håndtering for flere aktive filer

Regler:

- kun filsystem
- scanner streamer stadig sekventielt
- MTP forbliver sekventiel
- ingen genopbygning af Core2's kanal-tunge arkitektur

Færdig når:

- filsystem-backups kan køre med begrænset concurrency
- MTP-adfærd er uændret og stadig sekventiel

### Fase 7 - Persistence og resume `[LATER]`

**Mål:** Gør backup-sessioner holdbare på tværs af procesgrænser.

Planlagte komponenter:

- `Engine/State/IBackupRepository.cs`
- `Engine/State/NoOpBackupRepository.cs`
- `Engine/State/FileSystemBackupRepository.cs`
- `Engine/State/SqliteBackupRepository.cs`

Fasen skal bygge oven på den eksisterende session-state-model, ikke erstatte den.

Færdig når:

- session state kan lagres bevidst
- resume-adfærd er eksplicit og testbar

### Fase 8 - Avanceret rapportering og UI-integration `[LATER]`

**Mål:** Forbedr præsentationen efter at engine allerede er stabil.

Eksempler:

- rigere CLI-visninger
- eksporter og summaries
- optionelle event-drevne UI-hjælpere
- GUI/mobile-venlige adapters

Vigtig normalisering:

- `IProgressNotifier` hører til her som en mulig senere concern, ikke som Tier 1 public baseline

Færdig når:

- rigere UX ligger oven på en stabil engine-contract i stedet for inde i den

---

## 7. Hvad man ikke skal bygge først

Start **ikke** Core4 med disse:

- fuld parallelisme
- parallel MTP
- kanal-baseret pipeline-orkestrering
- public ETA/speed DTO-udvidelser før baseline virker
- `HashTypes`-agtig redesign af public plan
- `DeviceId`/`SourceDirectory`/`OutputDirectory`-varianter af planen
- skjult engine-niveau metadata-fallback der omgår metadata-reader

Hvis et designvalg gør engine mere kompleks før Tier 1 er komplet, er det sandsynligvis det forkerte næste skridt.

---

## 8. Done-definition pr. tier

| Tier | Done betyder |
|---|---|
| Tier 1 | Sekventiel filsystem + MTP backup virker, skriver sidecars, rapporterer basic progress og returnerer korrekt `BackupResult` |
| Tier 2 | Baseline er troværdig under realistiske fejlscenarier |
| Tier 3 | Optionelle enrichment-features er implementeret og gated korrekt |
| Tier 4 | Filesystem-only begrænset parallelisme virker uden at skade korrekthed |
| Tier 5 | Session persistence og resume er eksplicit og holdbar |
| Tier 6 | Rig rapportering/UI eksisterer uden at destabilisere engine-kernen |

---

## 9. Anbefalet rækkefølge for en koder

Hvis en koder vil have den korteste pålidelige vej, så følg denne rækkefølge:

1. færdiggør state- og validation-baseline
2. færdiggør sekventiel filsystem-transfer
3. tilføj sidecar-generering
4. tilføj progress tracking
5. tilføj DI og factory composition
6. tilføj sekventiel MTP-support
7. hærd fejl, cancellation og dry-run
8. tilføj metadata og timestamp correction
9. tilføj hashing og verification
10. tilføj begrænset parallel filsystem-support
11. tilføj persistence og resume
12. tilføj avanceret rapportering/UI-hjælpere

Denne rækkefølge er bevidst. Den giver en fungerende Core4 tidligt og udskyder kompleksitet til senere.

---

## 10. Kort slutresumé

Core4-planen er enkel i princippet:

- byg en korrekt sekventiel engine først
- få både filsystem og MTP til at virke
- skriv sidecars og rapportér faktuel progress
- tilføj robusthed bagefter
- tilføj enrichment-features derefter
- tilføj begrænset filsystem-parallelisme først når baseline er troværdig

Hvis et ældre dokument foreslår en anden contract-shape, så foretræk de normaliserede Core4-dokumenter og det aktuelle skeleton.
