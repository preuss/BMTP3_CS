# Dokumentation: Dato/Tid Formatteringsstandard

Denne standard definerer et sæt formattegn, der er optimeret til parsing og udskrivning af dato/tid-strenge. Standarden er designet med fokus på **filnavnssikkerhed** og **fleksibel præcision** ved at kombinere ISO 8601-principper med unikke offset-notationer.

We as an example using $\{YYYY\} as replacements in pattern.

## 1. Dato, Tid og Høj Præcision (Samlet Oversigt)

Denne tabel kombinerer alle elementer fra år til nanosekunder. Dato-elementer bruger store bogstaver (YYYY, MM, DD), og tids-elementer bruger små bogstaver (hh, mm, ss) for at matche ISO 8601-konventionen.

| Funktion        | Format Tegn               | Præcision       | Eksempel    | Bemærkninger              |
| :-------------- | :------------------------ | :-------------- | :---------- | :------------------------ |
| **År**          | YYYY                      | 4 cifre         | `2025`      | Kalenderår.               |
| **Måned**       | MM                        | 2 cifre         | `07`        | Måned i året.             |
| **Dag**         | DD                        | 2 cifre         | `09`        | Dag i måneden.            |
| **Time (24h)**  | hh                        | 2 cifre (00-23) | `18`        | 24-timers ur.             |
| **Minut**       | mm                        | 2 cifre         | `59`        |                           |
| **Sekund**      | ss                        | 2 cifre         | `23`        |                           |
| **Millisekund** | SSS eller fff             | 3 cifre         | `987`       | **Fraktion af sekundet**. |
| **Mikrosekund** | SSSSSS eller ffffff       | 6 cifre         | `987416`    | **Fraktion af sekundet**. |
| **Nanosekund**  | SSSSSSSSS eller fffffffff | 9 cifre         | `987416732` | **Fraktion af sekundet**. |

---

## 2. Tidszone Offset (Filnavnssikker og Logisk)

Offsettet angiver forskellen fra UTC (UTC ±hh:mm:ss). **Fortegn (+ eller -) er obligatorisk.** Logikken er baseret på antallet af formattegn: 1 tegn for timer, 2 for timer/minutter, 3 for timer/minutter/sekunder.

| Funktion             | Format ID (Type Z/O) | Format ID (Type ±)     | Output Format | Eksempel  | Logik                               |
| :------------------- | :------------------- | :--------------------- | :------------ | :-------- | :---------------------------------- |
| **Timer**            | O eller z            | ±hh                    | ±hh           | `+06`     | 1 tegn = Timer                      |
| **Timer & Minutter** | OO eller zz          | ±hhmm                  | ±hhmm         | `+0630`   | 2 tegn = Timer & Minutter           |
| **Timer, Min & Sek** | OOO eller zzz        | ±hhmmss                | ±hhmmss       | `+063036` | 3 tegn = Timer, Minutter & Sekunder |

### Vigtig Bemærkning: UTC Nul-Offset (Simuleret Z)

Da det standardiserede Z (Zulu/Nul-offset) er droppet for at forenkle, skal UTC-tid (tiden uden offset) repræsenteres ved eksplicit at specificere nul-offset med 4 eller 6 cifre.

- _Eksempel:_ For at vise UTC-tid, brug `OO` → `+0000`

---

## 3. Eksempler på Formatstrenge

Disse eksempler illustrerer, hvordan formattegnene kombineres.

| Formål                           | Formatstreng              | Eksempel på Output          |
| :------------------------------- | :----------------------   | :------------------------   |
| **Standard Tid/Dato**            | YYYYMMDD hhmmss           | `202507 09185923`           |
| **Høj Præcision (Nano)**         | YYYYMMDD hhmmss.fffffffff | `202507 09185923.987416732` |
| **Med Tidszone Offset (±hhmm)**  | YYYYMMDD hhmmssOO         | `202507 09185923+0630`      |
| **UTC Tid (Simuleret Z)**        | YYYYMMDD hhmmssOO         | `202507 09185923+0000`      |
| **Læsevenligt (med underscore)** | YYYY-MM-DD_hhmmss         | `2025-07-09_185923`         |


## 3. Other replacement keywords


| Function                | Keyword        | Example                            | Notes                                                                                           |
| :---------------------- | :------------- | :--------------------------------- | :-------------------------------------------------------------------------------------------    |
| **Source File Name**    | `originalName` | `photo_01`                         | The file name **without** its extension (e.g., `file.txt` → `file`).                            |
| **File Extension**      | `ext`          | `jpg`                              | The file extension **without** the leading dot.                                                 |
| **Relative Path**       | `relativePath` | `Documents/ProjectX`               | The path from the source root to the file.                                                      |
| **Sequential Counter**  | `count`        | `3`                                | Incremental number for duplicated files (starts at 1). The original file is implicitly count=0. |
| **Source Device Name**  | `deviceName`   | `MyLaptop`                         | Name of the device/computer that generated the source file.                                     |
| **Content Hash Short**  | `hashShort`    | `a7c8d9`                           | Shortened hash value of the file content. 6 chars                                               |
| **Content Hash Medium** | `hashMedium`   | `a7c8d9e1f2g3`                     | Shortened hash value of the file content. 12 chars.                                             |
| **Content Hash Long**   | `hashLong`     | `a7c8d9e1f2g3h4i5j6k7l8m9n0p1q2r3` | Shortened hash value of the file content. 32 chars                                              |
