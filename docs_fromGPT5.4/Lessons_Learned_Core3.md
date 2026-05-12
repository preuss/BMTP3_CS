# Lessons Learned: BMTP3.Core3

**Formål:** Beskrive hvad Core3 gjorde rigtigt, hvad der stadig var forkert, og hvad Core4 konkret skal tage med videre.

**Sidst opdateret:** 2026-05-12

---

## 1. Hovedlære fra Core3

Core3 kom tættere på den rigtige retning end Core2, fordi den gjorde flowet sekventielt, mere læsbart og lettere at debugge.

Den vigtigste lære er:

**En enkel sekventiel backup-motor er et bedre fundament end en kompleks parallel pipeline.**

Det betyder dog ikke, at Core3 er nok som slutløsning.  
Core3 er først og fremmest et godt fundament med nogle væsentlige mangler.

---

## 2. Hvad Core3 gjorde rigtigt

### 2.1 Sekventiel orkestrering

Core3's lineære flow er let at forstå:

- scan
- transfer
- feature-faser
- sidecar/afslutning

Det gør debugging, tests og fejlanalyse lettere.

### 2.2 Simpel komponentopdeling

Core3 har en mere overskuelig opdeling end ældre versioner:

- scanner
- transfer
- metadata/hash/sidecar
- DI-opsætning

Det er en retning Core4 skal bevare.

### 2.3 Færre concurrency-problemer

Fordi Core3 er sekventiel, undgår den mange af Core2's deadlocks og race conditions.

Det er især vigtigt som grundlag for MTP.

---

## 3. Hvad Core3 stadig gjorde forkert

### 3.1 Relative paths var ikke stærkt nok fastholdt

Det gav risiko for tab af mappestruktur og filnavnskollisioner.

**Core4-konsekvens:** Relative paths skal være en central del af item- og destination-logikken.

### 3.2 Transfer-fejl var for hårde

Én filfejl kunne stoppe hele backupen unødigt.

**Core4-konsekvens:** Per-fil fejl skal håndteres tydeligt og som udgangspunkt ikke vælte hele kørslen.

### 3.3 Sidecar kom for sent i flowet

Hvis sidecar først laves sent, mister man dokumentation når senere feature-faser fejler.

**Core4-konsekvens:** Sidecar skal skrives direkte efter succesfuld transfer.

### 3.4 Dry-run var ikke stærkt nok defineret

Dry-run kunne stadig være for dyr eller for upræcis.

**Core4-konsekvens:** Dry-run skal være bevidst, billigere end rigtig kørsel og semantisk tydelig.

### 3.5 Progress var ikke tilstrækkeligt gennemført

Der var stadig mismatch mellem hvad der blev ønsket rapporteret, og hvad der faktisk var sikkert og stabilt at rapportere.

**Core4-konsekvens:** Public progress skal være faktuel og stabil før rigere UI-data overvejes.

---

## 4. Hvad Core4 skal arve fra Core3

Core4 bør direkte arve disse ting:

- sekventiel baseline
- læsbart kontrolflow
- relativt enkel DI og komponentstruktur
- klar cancellation-håndtering
- forståelig fejlanalyse

Core4 skal med andre ord ligne Core3 mere end Core2 i sit fundament.

---

## 5. Hvad Core4 skal ændre i forhold til Core3

Core4 må ikke bare være "Core3 plus flere features".  
Den skal også rette de strukturelle mangler.

Core4 skal derfor:

- bevare relative paths korrekt
- skrive `.sidecar.json` efter transfer
- gøre MTP til en eksplicit sekventiel strategi
- holde feature-flags eksplicitte
- bruge `MetadataExtractor` først og `ExifTool` som fallback
- holde public contracts på `BackupPlan`, `BackupResult`, `IBackupProgress` og `IFileProgress`

---

## 6. Parallelisme-læringen fra Core3

Det er vigtigt at udlede den rigtige lære her.

Core3 viser **ikke**, at Core4 bare bør paralleliseres generelt.  
Den viser, at den sekventielle baseline er den rigtige start.

Derfor er den normaliserede Core4-konklusion:

- MTP = sekventiel
- filsystem = sekventiel først, begrænset parallelisme senere

---

## 7. Kort beslutningsresumé

Hvis man kun skal tage én ting med fra Core3 ind i Core4, er det dette:

- behold enkelheden
- ret de konkrete correctness-fejl
- flyt sidecar tidligere
- hold MTP sekventiel
- tilføj først parallelisme når baseline er troværdig

Det er den rigtige måde at lære af Core3 på.
