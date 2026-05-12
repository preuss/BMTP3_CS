# Lessons Learned Comparison

**Purpose:** Compare the earlier core generations and make the resulting Core4 direction explicit.

**Last Updated:** 2026-05-12

---

## Sammenligning

| Kategori | Core (legacy) | Core2 (pipeline) | Core3 (simpel) | Core4-konklusion |
|---|---|---|---|---|
| Arkitektur | Monolitisk og tæt koblet | Pipeline med høj kompleksitet | Sekventiel og lettere at forstå | Bevar simpel orkestrering som baseline |
| Fejlhåndtering | Fail-fast og stop på fejl | Kompliceret og implicit mellem stages | Mere læsbar, men stadig ujævn | Gør fejlpolitikker eksplicitte og hold dem enkle |
| State management | Implicit og spredt | Eksplicit, men tungt og skrøbeligt | Enklere og mere tydeligt | Brug tydelig session-state som intern sandhed |
| Sidecar | Tæt koblet til øvrig logik | Adskilt, men præget af pipeline-flow | God idé, men ikke helt konsekvent | Skriv sidecar direkte efter succesfuld transfer |
| Metadata | Feature-rig, men tæt bundet | Mere modulært | Delvist enklere | Brug `MetadataExtractor` primært og `ExifTool` som fallback |
| Progress | Praktisk, men ujævn | Rige data, men kompleks trådsikkerhed | Enklere, men ikke komplet nok | Hold public progress faktuel og stabil |
| Performance | Begrænset | Høj ambition, men ustabil | Stabil, men langsom | Tilføj kun begrænset filesystem-parallelisme senere |
| MTP | Understøttet, men gammel struktur | Særligt sårbar ved parallelisme | Sekventiel retning er bedre | MTP skal være sekventiel i Core4 |
| Testbarhed | Lav | Middel, men kompleks | Højere | Bevar små komponenter og DI |
| Udvidelsesmuligheder | Svære pga. kobling | Mulige, men dyre pga. pipeline | Bedre | Lagdel features oven på en stabil kerne |

---

## Hovedkonklusioner

### 1. Core4 skal ligne Core3 mere end Core2

Den vigtigste lære er, at Core4's fundament skal være enkelt og sekventielt, ikke pipeline-tungt og aggressivt parallelt.

### 2. Core2 havde rigtige ambitioner, men forkert fundament

Core2 viste, at hashing, metadata, verification og bedre performance er relevante mål.  
Det, der ikke skal genbruges, er måden de blev koblet sammen på.

### 3. MTP er den styrende begrænsning

Når designvalg kolliderer med MTP-stabilitet, skal MTP-stabilitet vinde.  
Derfor er Core4's normaliserede beslutning:

- MTP = sekventiel
- filesystem = sekventiel først, begrænset parallelisme senere

### 4. Sidecar og metadata skal være tidlige og eksplicitte

Core4 bør ikke vente med sidecar til sidst i forløbet.  
Og metadata-strategien skal være fast:

1. `MetadataExtractor`
2. `ExifTool` fallback
3. filsystem-attributter

### 5. Public contracts skal holdes små og stabile

Det er en læring på tværs af alle tidligere versioner, at for meget bevægelig kontrakt gør systemet sværere at færdiggøre.

Derfor bygger Core4 nu på:

- `BackupPlan`
- `BackupResult`
- `IBackupProgress`
- `IFileProgress`
- streaming scanner-kontrakt

og ikke på ældre mellemformer som `BackupJobResult`, `HashTypes`, `DeviceId` eller `-1`-semantik.

---

## Kort beslutningsresumé for Core4

Hvis man kun skal tage én ting med fra sammenligningen, er det dette:

- byg en lille korrekt sekventiel kerne
- få både filesystem og MTP til at virke
- skriv sidecar med det samme
- læs metadata via managed reader først og ExifTool som fallback
- tilføj optional features bagefter
- tilføj kun begrænset filesystem-parallelisme når baseline er stabil

Det er den retning alle de opdaterede Core4-dokumenter nu peger på.
