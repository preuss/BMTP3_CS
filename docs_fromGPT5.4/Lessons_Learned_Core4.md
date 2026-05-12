# Lessons Learned for Core4

**Version:** 2.0  
**Dato:** 2026-05-12  
**Formål:** Samle de vigtigste lessons learned, som nu styrer den normaliserede Core4-retning.

---

## 1. Hovedkonklusion

Core4 skal ikke være endnu et forsøg på at redde Core2's pipeline eller at udvide Core3 direkte uden styring.  
Core4 skal være en ny, lille og korrekt kerne, som derefter udvides i lag.

Den vigtigste lære er:

**Byg først en korrekt sekventiel kerne. Udvid bagefter.**

---

## 2. Hvad vi tager med fra tidligere versioner

### 2.1 Fra Core

**Bevar:**

- bred backup-ambition
- praktiske features
- forståelse for rigtige kilder og rigtige filer

**Undgå:**

- monolitiske God objects
- tæt koblet logik
- refaktorering uden klare kontrakter

**Konsekvens for Core4:**  
Core4 skal deles op i tydelige ansvar: scanner, transfer, sidecar, state, engine, features.

### 2.2 Fra Core2

**Bevar:**

- ambitionen om bedre performance
- separation of concerns
- DI og testbarhed
- ønsket om optionelle features som hash, metadata og verification

**Undgå:**

- bounded-channel-kompleksitet
- skjult kobling mellem stages
- race conditions i shared state
- parallel MTP-adgang
- fejl som kun kan forstås ved at kende hele pipeline-topologien

**Konsekvens for Core4:**  
Parallelisme er ikke fundamentet. Den er en senere, kontrolleret udvidelse for filsystem.

### 2.3 Fra Core3

**Bevar:**

- sekventiel læsbarhed
- nemmere debugging
- enklere orkestrering

**Ret:**

- relative paths skal bevares korrekt
- én filfejl må ikke ødelægge hele backupen unødigt
- destination directories skal oprettes eksplicit
- sidecars skal skrives tidligt
- dry-run skal være ærlig og billigere end rigtig kørsel

**Konsekvens for Core4:**  
Core3 er tættere på det rigtige fundament end Core2, men mangler stadig den færdige struktur og de sidste korrekte valg.

---

## 3. Fastlåste Core4-beslutninger

Disse valg er ikke længere åbne diskussioner.

### 3.1 Public contracts følger skeleton

Core4 bygger nu på:

- `BackupPlan`
- `BackupResult`
- `IBackupEngine`
- `IBackupProgress`
- `IFileProgress`

Ikke på ældre kontrakter som:

- `BackupJobResult`
- `IBackupItem` som public baseline
- `DeviceId`
- `SourceDirectory`
- `OutputDirectory`
- `WriteSidecar`
- `HashTypes`

### 3.2 Kildetype er eksplicit

Brug:

- `BackupPlan.SourceType`
- `BackupPlan.Source`

Ikke path-heuristik eller gamle særfelter.

### 3.3 MTP er altid sekventiel

Det er en af de vigtigste Core4-beslutninger.

MTP skal:

- scannes sekventielt
- overføres sekventielt
- holdes stabil via keepalive, timeout og retry

Parallel MTP er ikke et mål.

### 3.4 Filsystem kan senere få begrænset parallelisme

`MaxDegreeOfParallelism` findes allerede i planen, men:

- `null = auto`
- `1 = sekventiel`
- `> 1 = parallel sti senere`

Begrænset parallelisme hører til senere filesystem-only tiers.

### 3.5 Sidecar skrives tidligt

Sidecar skal skrives direkte efter succesfuld transfer.

Normaliseret navn:

- `.sidecar.json`

Den må ikke først blive genereret til sidst i hele jobbet.

### 3.6 Metadata-strategien er fast

Datoer og metadata læses i denne rækkefølge:

1. `MetadataExtractor`
2. `ExifTool` fallback
3. filsystem-attributter

Filsystem-fallback skal ligge i metadataresultatet, ikke som skjult engine-logik.

### 3.7 Progress skal være faktuel

Public progress skal være stabil og nøgtern:

- fase
- antal mapper/filer
- bytes total/processeret
- succeeded/skipped/failed
- aktive filer

ETA, speed og andre rigere UI-data er senere lag eller afledt præsentation.

---

## 4. Hvad Core4 faktisk skal lære af fejlene

### 4.1 Fra Core2's deadlocks

Læringen er ikke bare "brug ikke channels".  
Læringen er: **introducer ikke kompleks koordinering før baseline er bevist korrekt**.

### 4.2 Fra Core3's mangler

Læringen er ikke bare "Core3 mangler features".  
Læringen er: **simple flows skal færdiggøres korrekt, ikke bare udvides hurtigt**.

### 4.3 Fra tidligere metadata-problemer

Læringen er ikke bare "EXIF kan være svært".  
Læringen er: **metadata skal ligge i en dedikeret reader-strategi med tydelig fallback-rækkefølge**.

### 4.4 Fra sidecar-problemer

Læringen er: **sidecar er en del af succes-stien, ikke et ekstra trin bagefter**.

---

## 5. Konsekvens for implementeringsrækkefølgen

Lessons learned fører direkte til denne rækkefølge:

1. kontrakter, validering og session-state
2. sekventiel filsystem-backup
3. sidecar + progress
4. sekventiel MTP-understøttelse
5. robustness/hærdning
6. metadata, timestamp, hashing, verification
7. begrænset parallel filsystem-engine
8. persistence/resume
9. avanceret rapportering/UI

Det er denne rækkefølge, der gør Core4 færdiggørbar.

---

## 6. Typiske fejlspor vi ikke må falde tilbage i

Undgå disse mønstre:

- at starte med parallel engine før sekventiel baseline er færdig
- at lade MTP vente til senere
- at gøre ExifTool til eneste metadata-løsning
- at gøre progress-contract tungere end skeleton kræver
- at genindføre gamle planfelter bare fordi de "var praktiske"
- at gøre sidecar afhængig af senere optionelle faser

---

## 7. Hvornår lessons learned er omsat korrekt i Core4

Lessons learned er først virkelig indarbejdet, når:

- filsystem og MTP begge virker i Tier 1
- sidecars skrives med det samme
- metadata læses med den normaliserede prioritet
- public contracts stadig matcher skeleton
- senere tiers bygger ovenpå baseline i stedet for at redefinere den

---

## 8. Relaterede dokumenter

Brug dette dokument sammen med:

- `docs\CORE4_IMPLEMENTATION_GUIDE_DA.md` - den detaljerede coder-guide
- `docs\CORE4_MASTER_SYNTHESIS.md` - konfliktløseren
- `docs\Core4_Master_Architecture.md` - dybere reference
- `docs\CORE4_ARCHITECTURE_en.md` - normaliseret engelsk arkitektur
- `docs\CORE4_PLAN_en.md` - normaliseret engelsk plan

---

## 9. Kort slutresumé

Det vigtigste Core4 har lært er:

- sekventiel korrekthed før performance
- MTP-stabilitet før ambitioner
- tydelige contracts før ekstra features
- sidecar og metadata som eksplicitte lag
- begrænset filsystem-parallelisme som senere udvidelse

Det er den retning Core4 nu skal holdes på.
