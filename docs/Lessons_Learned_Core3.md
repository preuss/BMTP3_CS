# Lessons Learned: BMTP3.Core3

## 1. Sekventiel Backup (BackupEngineSequential)
- Designvalg: Hele backup-processen er sekventiel og single-threaded (BackupEngineSequential.cs), hvilket sikrer forudsigelighed og enkel fejlhåndtering, men kan være langsomt ved store datamængder.
- Eksempel: Dataflow: Scan → Transfer → ExtractMetadata → GenerateHashes → CorrectTimestamps → GenerateSidecar.
- Fejltyper: Kritiske fejl (fx under transfer) stopper hele processen, mens ikke-kritiske fejl (fx metadata, hash, sidecar) logges og processen fortsætter.
- Forbedringsforslag: Overvej parallelisering for store backups, evt. med throttling og batch-størrelser.

## 2. Simplicity
- Designvalg: Simpel, modulær opbygning med dependency injection og klare grænseflader (interfaces).
- Eksempel: Hver fase (scan, transfer, metadata, hash, sidecar) har eget interface og implementering.
- Fordel: Let at teste og udskifte komponenter.

## 3. Scanning
- Edge Cases: Scanner (FileSystemScanner.cs) fejler hvis source-mappen ikke findes, eller hvis adgang nægtes.
- Eksempel: Kaster FileNotFoundException hvis source ikke findes.
- Forbedringsforslag: Tilføj håndtering/logning af adgangsfejl og mulighed for at ignorere utilgængelige filer.

## 4. Transfer
- Fejltyper: Kaster exception hvis kildefil eller destinationsmappe mangler. Overwrite-strategi sletter eksisterende fil før kopiering.
- Eksempel: SimpleFileTransfer.cs linje 31-39.
- Forbedringsforslag: Tilføj retry-logik og mulighed for at springe fejlbehæftede filer over uden at stoppe hele backup.

## 5. Error Handling
- Designvalg: Skelner mellem kritiske og ikke-kritiske fejl. Kritiske fejl (fx under transfer) stopper processen, mens ikke-kritiske fejl (fx metadata, hash, sidecar) logges og processen fortsætter.
- Eksempel: catch(Exception ex) i metadata/hash/sidecar-faser tilføjer fejl til error-listen, men kaster ikke videre.
- Forbedringsforslag: Gør det konfigurerbart hvilke fejl der er kritiske.

## 6. Metadata
- Edge Cases: Metadata læses kun hvis filen findes på destinationen. Fejl i metadataudtræk stopper ikke backup.
- Eksempel: ExtractMetadata-fasen i BackupEngineSequential.cs.
- Forbedringsforslag: Udvid metadataudtræk til at inkludere flere filtyper og håndtere fejltyper mere detaljeret.

## 7. Sidecar
- Designvalg: Simpel INI-lignende sidecar-fil genereres for hver backup-fil.
- Fejltyper: Fejl i sidecar-generering logges, men stopper ikke backup.
- Eksempel: SimpleSidecarGenerator.cs linje 25-28.
- Forbedringsforslag: Tilføj validering af sidecar-indhold og mulighed for at vælge format.

## 8. Dependency Injection (DI)
- Designvalg: Alt bindes op via DI (ServiceCollectionExtensions.cs), hvilket gør det let at udskifte komponenter.
- Eksempel: services.AddSingleton<IBackupEngine, BackupEngineSequential>().
- Fordel: Understøtter testbarhed og fleksibilitet.

## 9. Edge Cases og Problemer
- Eksempler:
  - Manglende kilde/destination stopper backup.
  - Adgangsfejl under scanning/transfer.
  - Fejl i metadata/hash/sidecar logges, men kan give ufuldstændig backup-verifikation.
- Forbedringsforslag: Mere robust håndtering af filsystemfejl, mulighed for at genoptage backup, og bedre rapportering af delvise succeser.

---

**Konklusion:**
BMTP3.Core3 er robust og simpelt designet, men kunne forbedres på performance, fejltolerance og fleksibilitet i error handling. Parallelisering, bedre edge case-håndtering og konfigurerbarhed anbefales.

---

**Kildeeksempler:**
- BackupEngineSequential.cs linje 12-407
- FileSystemScanner.cs linje 7-40
- SimpleFileTransfer.cs linje 7-40
- SimpleSidecarGenerator.cs linje 7-40
- ServiceCollectionExtensions.cs linje 11-40
