# Lessons Learned – BMTP3.Core2

## 1. Pipeline-arkitektur
- Designvalg: To engine-varianter: BackupEngine (parallel, channels, workers) og BackupEngineSequentiel (simpel, sekventiel).
- Eksempel: BackupEngine bruger Channels til at forbinde pipeline-stages (fx buffering, metadata, hash, transfer), mens BackupEngineSequentiel kører alle steps for ét item ad gangen.
- Fordel: Parallel engine giver høj throughput på store backups.
- Ulempe: Øget kompleksitet, især i error handling og progress-tracking.
- Forbedring: Udtræk fælles pipeline-mønster (se SequentialItemPipeline) for at undgå boilerplate og sikre DRY/SOLID.

## 2. Parallelisme & Race Conditions
- Problem: Parallel pipeline kræver omhyggelig håndtering af shared state og ressourcer (fx MTP device sessions).
- Eksempel: MTP-session skal åbnes før pipeline starter og først lukkes, når buffering er færdig, ellers opstår race condition hvor device kan disconnecte midt i transfer (BackupEngine.cs linje 241-293).
- Løsning: Brug ContinueWith på bufferingTask for at sikre korrekt dispose-timing.
- Edge case: Channels skal lukkes korrekt for at undgå deadlocks.
- Forbedring: Overvej at isolere device-adgang i dedikeret stage med single-threaded execution.

## 3. Scanning
- Designvalg: Scanning sker i producerTask, som skriver til første channel.
- Fejltype: Hvis scanning fejler, skal alle downstream channels lukkes for at undgå pipeline-hæng.
- Eksempel: scanChannel.Writer.Complete() i finally-blok.

## 4. Transfer
- Problem: Transfer kan fejle pga. IO, netværk, eller destination-collisions.
- Eksempel: TransferStep håndterer collisions via ICollisionResolver.
- Forbedring: Implementér retry-policy for transient errors (se evt. Engine.Resilience).

## 5. Error Handling
- Fejl: Tidligere blev CompleteItem() kaldt efter hvert step (forkert), hvilket markerede item som færdig for tidligt (se REVIEW_BackupEngineSequentiel.md).
- Rigtigt: Kun ét kald til CompleteItem() efter ALLE steps.
- Eksempel: Steps skal selv håndtere exceptions og sætte item til Failed, ikke kaste videre.
- Forbedring: Centraliser error logging og undgå dobbelt-logging.

## 6. Metadata & Sidecar
- Designvalg: Metadata ekstraheres i dedikeret step og bruges til progress, hashing, sidecar mv.
- Problem: Manglende eller inkonsistent metadata kan føre til fejl senere i pipeline.
- Eksempel: Længde (MetadataKey.Length) skal være sat efter metadata-step, ellers fejler progress og transfer.
- Sidecar: Genereres i separat step, med mulighed for forskellige formater (JSON, INI).
- Forbedring: Validér og normalisér metadata tidligt.

## 7. Dependency Injection (DI)
- Designvalg: DI bruges til at injicere alle services i engine-konstruktører.
- Eksempel: Se ServiceCollectionExtensions.cs.
- Problem: For pipeline-stages, der oprettes dynamisk, kan DI ikke altid bruges direkte.
- Forbedring: Overvej factory-patterns eller scopes for stages med afhængigheder.

## 8. Progress Tracking
- Fejl: Manglende eller forkert brug af UpdateItemPhase() forvirrer UI-progress.
- Eksempel: Skal kaldes før hvert step for at UI kan vise korrekt status.
- Forbedring: Indkapsl i pipeline-mønster (se SequentialItemPipeline).

## 9. Refactoring & SOLID
- Problem: Gentagelse af step-mønster (update progress, execute step) i sekventiel engine.
- Løsning: Udtræk til SequentialItemPipeline for DRY og testbarhed.
- Eksempel: Før: 7x gentagelse, efter: én pipeline-metode.

## 10. Edge Cases & Pitfalls
- MTP: Parallel adgang kan give timeouts – kræver single-threaded buffering.
- Channels: Forkert channel-lukning kan give deadlocks.
- Exception Handling: Steps må ikke kaste – skal markere item som failed og sende videre.
- Session Resume: Husk at persistere state periodisk for crash-resume.

---

## Konkrete forbedringsforslag
- Udtræk pipeline-mønster til genbrug for både sekventiel og parallel engine.
- Centralisér error handling og logging.
- Implementér retry-policy for transfer og IO.
- Validér metadata tidligt og fail fast.
- Brug DI-factory/scopes for pipeline-stages.
- Dokumentér alle edge cases i kode og tests.

---

**Eksempler og linjereferencer:**
- BackupEngineSequentiel.cs (linje 236-262): Forkert CompleteItem()-kald.
- BackupEngine.cs (linje 241-293): MTP-session race condition.
- REVIEW_BackupEngineSequentiel.md: Uddybning af fejl og fixes.
- SequentialItemPipeline.cs: DRY pipeline-mønster.

---

Dette dokument kan bruges som basis for fremtidige refaktoreringer og onboarding.
