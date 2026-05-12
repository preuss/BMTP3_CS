# Lessons Learned: BMTP3.Core2

**Formål:** Beskrive hvilke læringer Core2 gav, og hvilke af dem Core4 faktisk skal omsætte til konkrete designvalg.

**Sidst opdateret:** 2026-05-12

---

## 1. Hovedlære fra Core2

Core2 beviste, at performance og feature-rigdom er relevante mål, men også at en kompleks pipeline-arkitektur hurtigt bliver dyr i stabilitet, debugging og vedligehold.

Den vigtigste lære er:

**Kompleks parallel orkestrering er ikke et godt fundament for Core4.**

---

## 2. Hvad Core2 gjorde rigtigt

### 2.1 Ambitionen var korrekt

Core2 forsøgte at løse reelle behov:

- bedre throughput
- tydelig opdeling i komponenter
- mulighed for hashing, metadata, verification og andre ekstra lag
- DI-baseret sammensætning

Det er værd at tage med videre.

### 2.2 Den viste hvor performance kan give mening

Core2 viste især, at filsystem-backups kan have nytte af kontrolleret parallelisme.

Det er en relevant idé for Core4 - men **kun senere** og **kun for filsystem**.

---

## 3. Hvad Core2 gjorde forkert

### 3.1 Pipeline-kompleksiteten blev for høj

Channels, worker pools og implicit stage-kobling gjorde flowet svært at forstå og svært at bevise korrekt.

**Core4-konsekvens:** Start med sekventiel orkestrering, ikke kanal-topologi.

### 3.2 Concurrency gav race conditions

Når shared state, progress og device-ressourcer opdateres fra flere steder, bliver fejlene svære at forstå og reproducere.

**Core4-konsekvens:** Del kun parallelisme ind, når baseline allerede er korrekt og enkel.

### 3.3 MTP blev behandlet som om det var et normalt parallelt workload

Det gav ustabilitet, timeouts og risiko for disconnects.

**Core4-konsekvens:** MTP er altid sekventiel i Core4.

### 3.4 Fejlpolitikker blev for implicitte

Når pipeline-stages bare sender items videre, bliver det uklart hvilke fejl der stopper, hvilke der fortsætter, og hvem der ejer beslutningen.

**Core4-konsekvens:** Fejlpolitikker skal være eksplicitte og lette at følge i engine-flowet.

### 3.5 Debugging blev for dyrt

Når man skal forstå kanaler, stage-protokoller og worker-livscyklus bare for at debugge et enkelt problem, er arkitekturen blevet for dyr.

**Core4-konsekvens:** Dataflow skal være synligt og lineært så længe som muligt.

---

## 4. Hvad Core4 skal arve fra Core2

Core4 bør arve disse ting - men i enklere form:

- separation of concerns
- optionelle feature-lag
- DI og udskiftelige implementeringer
- ideen om at filsystem kan få senere performance-forbedringer

Core4 bør **ikke** arve selve pipeline-arkitekturen.

---

## 5. Hvad Core4 ikke skal kopiere

Core4 skal ikke:

- genopbygge channel-pipeline som fundament
- lade MTP indgå i parallel engine
- gøre progress afhængig af kompleks shared-state-synkronisering
- skjule fejlpolitikker i stage-overgange
- bruge pipeline-genbrug som mål i sig selv

Det er vigtigt: den rigtige lære af Core2 er **ikke** "lav en bedre pipeline".  
Den rigtige lære er "lad være med at gøre pipeline-kompleksitet til baseline".

---

## 6. Den korrekte Core4-konklusion

Core2 peger derfor på denne normaliserede Core4-retning:

- sekventiel engine som fundament
- streaming scanner-contract
- MTP = sekventiel
- filsystem = sekventiel først, begrænset parallelisme senere
- optionelle features oven på baseline

---

## 7. Parallelisme-læringen fra Core2

Hvis man vil udlede én præcis regel fra Core2, er det denne:

**Parallelisme må kun introduceres dér hvor gevinsten er reel, og hvor kompleksiteten ikke vælter stabiliteten.**

I Core4 betyder det:

- ikke i Tier 1
- ikke for MTP
- først senere for filsystem
- uden at ændre public contracts

---

## 8. Kort beslutningsresumé

Core2 lærte os:

- at features og performance er relevante mål
- at pipeline-kompleksitet er et dårligt fundament
- at MTP ikke tåler samme strategi som filsystem
- at Core4 skal vokse i lag, ikke starte med maksimal sofistikering

Det er den rigtige måde at lære af Core2 på.
