# Lessons Learned – BMTP3.Core (Legacy)

## 1. Backup-flow og Orkestrering

**Designvalg og problemer:**
- `BackupHandler` og `BackupMaster` er "God objects" med for mange ansvarsområder ([NewArchitecture.md](C:\Users\JesperPreuss\source\repos\BMTP3_CS\BMTP3.Core\NewArchitecture.md), linje 20).
- Backup-flowet håndterer både kilde- og destinationsvalg, filiteration, tilstandsstyring, kopiering, sammenligning og sidecar-generering i én klasse.
- Manglende separation gør test og vedligeholdelse svær.

**Fejltyper og edge cases:**
- Fejl i én fil kan stoppe hele backup-processen (fail-fast, men uden mulighed for delvis succes eller rollback, [Architecture_2.md](C:\Users\JesperPreuss\source\repos\BMTP3_CS\BMTP3.Core\Architecture_2.md), linje 32-34).
- Manglende strategi for håndtering af delvise fejl og rollback ved fx disk-udfyldning.

**Forbedringsforslag:**
- Indfør pipeline-arkitektur med små, testbare stages og eksplicit error handling.
- Implementér rollback og cleanup for delvist kopierede filer.

---

## 2. Scanning og Metadata

**Designvalg og problemer:**
- Metadata-udtræk sker via `MetadataExtractorFileInfo`, men der er kendte problemer med tredjepartsbiblioteket ([MetadataExtractorFileInfo.cs](C:\Users\JesperPreuss\source\repos\BMTP3_CS\BMTP3.Core\Metadata\MetadataExtractorFileInfo.cs), linje 21-23).
- Koden forsøger at læse metadata på alle filer, også ikke-billeder, hvilket giver undtagelser og fejl.

**Fejltyper og edge cases:**
- ImageMetadataReader kan kaste exceptions på ikke-billedfiler eller ved korrupt metadata.
- QuickTime-metadata kan returnere EPOCH-dato, hvis feltet er tomt.

**Forbedringsforslag:**
- Skift til mere robust metadata-bibliotek.
- Filtrér på filtype før metadata-udtræk.
- Indfør fallback-strategier for manglende eller fejlbehæftet metadata.

---

## 3. Transfer og Sidecar

**Designvalg og problemer:**
- Sidecar-filer genereres for hver backup-fil, men logikken er tæt koblet til backup-flowet.
- Sidecar-formatet er hardkodet (INI), hvilket gør udvidelse svært.

**Fejltyper og edge cases:**
- Manglende cleanup af temp-filer ved fejl eller afbrydelse.
- Risiko for inkonsistens mellem fil og sidecar ved fejl midt i processen.

**Forbedringsforslag:**
- Udskil sidecar-logik i egne services/interfaces.
- Understøt flere sidecar-formater via strategi-mønster.
- Implementér cleanup-service for temp-filer og sidecars.

---

## 4. Error Handling

**Designvalg og problemer:**
- Fejl håndteres ofte med fail-fast og exceptions, men uden differentiering mellem kritiske og ikke-kritiske fejl ([BackupHandler.cs](C:\Users\JesperPreuss\source\repos\BMTP3_CS\BMTP3.Core\Handlers\BackupHandler.cs), linje 31-60).
- Manglende central error logging og rapportering.

**Fejltyper og edge cases:**
- Exceptions kan maskere oprindelig fejl, hvis cleanup også fejler.
- Manglende retry-mekanismer for midlertidige fejl (fx netværk).

**Forbedringsforslag:**
- Indfør centraliseret error handler/service.
- Differentier mellem recoverable og fatal errors.
- Implementér retries og exponential backoff hvor relevant.

---

## 5. Dependency Injection (DI)

**Designvalg og problemer:**
- DI anvendes, men mange afhængigheder injiceres direkte i store klasser, hvilket gør test og udskiftning besværlig.
- Manglende brug af interfaces for alle afhængigheder.

**Forbedringsforslag:**
- Brug interfaces og små services for alle afhængigheder.
- Konfigurer DI-container til at understøtte udskiftning og testbarhed.
- Overvej at flytte DI-konfiguration ud af core og ind i konsol-applikationen.

---

## 6. State Management og Pipeline

**Designvalg og problemer:**
- `BackupItem` bruger nullable properties til at repræsentere pipeline-stadier, hvilket giver implicit og usikker tilstand ([Architecture_2.md](C:\Users\JesperPreuss\source\repos\BMTP3_CS\BMTP3.Core\Architecture_2.md), linje 22-29).
- Risiko for NullReferenceExceptions og svær fejlfinding.

**Forbedringsforslag:**
- Brug state pattern, builder pattern eller separate DTOs for hvert pipeline-stadie.
- Gør tilstand eksplicit og typesikker.

---

## 7. Konkrete eksempler på fejl og TODOs

- `// TODO: Switch to a better metadata reader` ([MetadataExtractorFileInfo.cs](C:\Users\JesperPreuss\source\repos\BMTP3_CS\BMTP3.Core\Metadata\MetadataExtractorFileInfo.cs), linje 22)
- `// TODO: Fix retry because if temp file does not exist or is 0 length but media has length fix problem.` ([BackupHandlerOld.cs](C:\Users\JesperPreuss\source\repos\BMTP3_CS\BMTP3.Core\Handlers\BackupHandlerOld.cs))
- `// TODO - Should be used when backupProgressTracker should be changed because newer files needs to be added` ([BackupHandler.cs](C:\Users\JesperPreuss\source\repos\BMTP3_CS\BMTP3.Core\Handlers\BackupHandler.cs))

---

## 8. Overordnede forbedringsforslag

- Modularisér og refaktorér til pipeline-arkitektur.
- Gør error handling, logging og cleanup robust og centraliseret.
- Brug DI og interfaces konsekvent.
- Gør state management eksplicit og typesikker.
- Udskil sidecar og metadata til egne, udskiftelige services.

---

**Se også:**  
- [Architecture_2.md](C:\Users\JesperPreuss\source\repos\BMTP3_CS\BMTP3.Core\Architecture_2.md)  
- [NewArchitecture.md](C:\Users\JesperPreuss\source\repos\BMTP3_CS\BMTP3.Core\NewArchitecture.md)  
- [BackupHandler.cs](C:\Users\JesperPreuss\source\repos\BMTP3_CS\BMTP3.Core\Handlers\BackupHandler.cs)  
- [MetadataExtractorFileInfo.cs](C:\Users\JesperPreuss\source\repos\BMTP3_CS\BMTP3.Core\Metadata\MetadataExtractorFileInfo.cs)  

---
