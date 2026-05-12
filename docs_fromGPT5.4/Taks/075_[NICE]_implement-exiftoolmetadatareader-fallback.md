# Task 075: implement exiftoolmetadatareader fallback

**Filnavn:** 075_[NICE]_implement-exiftoolmetadatareader-fallback.md  
**Prioritet:** [NICE]  
**Milestone-spor:** Post-baseline feature expansion

## Formaal
`ExifToolMetadataReader` via `exiftool.exe -json` (CLI fallback).

## Hvorfor denne task findes
Vaerdifuld udvidelse uden at blokere baseline.

## Afhaenger af
- Ingen eksplicitte, men forudsaetter alle foregaaende tasks i nummerorden.

## Implementeringsvejledning
1. Start med at afgraense praecis hvad task-navnet kraever, og undgaa at blande senere features ind.
2. Opdater eller opret kun de filer tasken angiver direkte, plus eventuelle noedvendige wiring-kald.
3. Hold eksisterende kontrakter stabile, medmindre tasken eksplicit handler om kontraktaendring.
4. Tilfoej eller opdater relevante tests for netop denne adfaerd (unit/integration afhaengigt af tasktype).
5. Dokumenter kort i commit-beskrivelsen hvilke acceptance-kriterier der nu er opfyldt.

## Beroerte omraader (forventet)
- BMTP3.Core4 (primaert)
- BMTP3.Core4.Tests (test tasks og kontraktvalidering)
- BMTP3.Consoles (kun naar tasken handler om CLI/DI wiring)
- docs_fromGPT5.4/Taks (kun dokumentopdateringer)

## Acceptkriterier
- Scope i tasknavn + taskbeskrivelse er fuldt implementeret.
- Koden kan bygges og passer med eksisterende Core4-kontrakter.
- Eventuelle tests for omraadet afspejler den nye adfaerd.
- Ingen utilsigtede aendringer udenfor taskens scope.

## Klasse-/flow-note
Brug foelgende tommelfingerregel for denne task:
- Kontrakt-task: design foerst signatur og invariants, derefter implementation.
- Engine-task: dataflow skal vaere scan -> resolve -> transfer -> sidecar -> result.
- Feature-task: udvid eksisterende flow uden at bryde baseline-kontrakter.
- Test-task: en tydelig adfaerdsforventning per test-case.

## Noter
- Implementer i strikt nummerorden.
- Hvis tasken afdaekker et skjult afhaengighedshul, opret kun den minimale noedvendige pre-task justering i indexet.