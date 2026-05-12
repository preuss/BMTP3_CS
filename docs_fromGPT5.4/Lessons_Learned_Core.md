# Lessons Learned: BMTP3.Core (Legacy)

**Formål:** Beskrive hvad den oprindelige Core lærte os, og hvordan de læringer omsættes til konkrete Core4-valg.

**Sidst opdateret:** 2026-05-12

---

## 1. Hovedlære fra den gamle Core

Den gamle Core viste, at backupdomænet er rigtigt og nyttigt, men også at for meget samlet ansvar i få store klasser gør systemet svært at vedligeholde og svært at videreføre sikkert.

Den vigtigste lære er:

**Feature-rigdom er ikke nok. Strukturen skal også være bæredygtig.**

---

## 2. Hvad den gamle Core gjorde rigtigt

### 2.1 Den dækkede virkelige backup-behov

Den gamle Core arbejdede med rigtige problemer:

- rigtige kilder
- rigtige filer
- metadata
- sidecars
- temp-filer og cleanup

Det er vigtigt. Core4 må ikke blive så "ren", at den mister de praktiske behov af syne.

### 2.2 Den afslørede de rigtige domæneproblemer

Legacy-koden viste tydeligt, at disse områder betyder noget:

- scan og source-identitet
- metadata-kvalitet
- sidecar-konsistens
- cleanup ved fejl
- eksplicit state

Core4 skal tage alle disse områder alvorligt.

---

## 3. Hvad den gamle Core gjorde forkert

### 3.1 For meget ansvar i få klasser

Store orkestreringsklasser blandede:

- source-valg
- scanning
- transfer
- metadata
- sammenligning
- sidecars
- fejlbehandling

**Core4-konsekvens:** Ansvar skal deles op i små services og tydelige contracts.

### 3.2 Fail-fast uden god model for delvis succes

Når én fil eller ét step fejlede, var det svært at håndtere delvis fremdrift pænt.

**Core4-konsekvens:** Core4 skal kunne skelne mellem per-fil fejl og job-niveau fejl.

### 3.3 Sidecar og metadata var for tæt koblet til hovedflowet

Når hjælpefunktioner er hårdt bundet til hovedflowet, bliver udskiftning og test vanskeligt.

**Core4-konsekvens:** Sidecar og metadata skal ligge bag egne interfaces og egne modeller.

### 3.4 State var for implicit

Når tilstand gemmes via nullable felter og blandet logik, bliver systemet svært at forstå og fejlfinde.

**Core4-konsekvens:** Core4 skal have tydelig session-state og eksplicit item-status.

### 3.5 Metadata var for skrøbeligt

Metadata-læsning på for mange filtyper og uden tydelig fallback skabte unødige fejl.

**Core4-konsekvens:** Metadata skal læses via en kontrolleret strategi med normaliseret fallback.

---

## 4. Hvad Core4 skal arve fra legacy

Core4 bør arve:

- respekt for det praktiske backup-domæne
- fokus på cleanup
- behovet for sidecars
- behovet for metadata og timestamps
- behovet for klare brugerrelevante resultater

Core4 bør **ikke** arve den monolitiske struktur.

---

## 5. Hvad Core4 konkret skal gøre anderledes

Ud fra legacy-læringerne skal Core4:

- splitte ansvaret mellem scanner, transfer, sidecar, metadata, validation og engine
- holde contracts små og tydelige
- bruge eksplicit `BackupSessionState`
- skrive sidecar som separat service
- bruge en tydelig metadata-strategi
- håndtere cleanup og delvise fejl mere bevidst

Det er også derfor Core4 nu bygger på en mindre, mere stabil public contract.

---

## 6. Metadata- og sidecar-læringen

Legacy-koden viser især to ting:

1. metadata er vigtigt, men skrøbeligt
2. sidecars er nyttige, men skal kunne udvikles uden at være hårdt bundet til hovedflowet

Den normaliserede Core4-konsekvens er derfor:

- metadata læses via `MetadataExtractor` først
- `ExifTool` bruges som fallback
- filsystem-attributter udfylder minimumsdata
- sidecar skrives som `.sidecar.json`
- sidecar skrives direkte efter succesfuld transfer

---

## 7. Kort beslutningsresumé

Hvis man kun skal tage én ting med fra legacy Core ind i Core4, er det dette:

- bevar de praktiske domænebehov
- smid den monolitiske struktur væk
- gør state, metadata og sidecar eksplicit
- gør fejl og cleanup mere kontrolleret

Det er den rigtige måde at lære af den gamle Core på.
