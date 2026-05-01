# MessageFormatter Function Reference

## Om dette dokument

Dette dokument definerer de tilgængelige **FormatTypes**, **FormatStyles**, **CustomPatterns** og **Functions** for MessageFormatter.
Disse er implementeringsdetaljer som kan udvides per projekt.
Syntaksen for hvordan de bruges er defineret i MessageFormatter Syntax-dokumentet.

---

## FormatTypes

| FormatType | Beskrivelse | Konverterings-function |
|-----------|-------------|----------------------|
| `string` | Tekststreng | `toString()` |
| `integer` | Heltal | `toInteger()` |
| `float` | Decimaltal | `toFloat()` |
| `date` | Dato (uden tid) | `toDate()` |
| `time` | Tidspunkt (uden dato) | `toTime()` |
| `datetime` | Dato og tid | `toDateTime()` |
| `bool` | Boolean (true/false) | `toBool()` |

---

## FormatStyles per FormatType

### `string`

Ingen FormatStyles — strengmanipulation håndteres via Functions.

### `integer`

| FormatStyle | Beskrivelse | Eksempel (value=1024) |
|-------------|-------------|----------------------|
| `currency` | Valutaformat (locale-afhængig) | `$1,024` / `1.024 kr.` |
| `percent` | Procentformat | `102400%` |
| `hex` | Hexadecimal | `400` |
| `binary` | Binær | `10000000000` |
| `thousands` | Med tusindtal-separator | `1,024` |

### `float`

| FormatStyle | Beskrivelse | Eksempel (value=1234.5678) |
|-------------|-------------|---------------------------|
| `currency` | Valutaformat (locale-afhængig) | `$1,234.57` |
| `percent` | Procentformat | `123456.78%` |
| `thousands` | Med tusindtal-separator | `1,234.57` |
| `scientific` | Videnskabelig notation | `1.23E+03` |

### `date`

| FormatStyle | Beskrivelse | Eksempel (value=2025-04-17) |
|-------------|-------------|----------------------------|
| `short` | Kort datoformat (locale) | `17.04.25` / `4/17/25` |
| `medium` | Medium datoformat (locale) | `17. apr. 2025` |
| `long` | Langt datoformat (locale) | `17. april 2025` |
| `iso` | ISO 8601 | `2025-04-17` |

### `time`

| FormatStyle | Beskrivelse | Eksempel (value=16:23:45) |
|-------------|-------------|--------------------------|
| `short` | Timer og minutter | `16:23` / `4:23 PM` |
| `medium` | Timer, minutter, sekunder | `16:23:45` |
| `long` | Med tidszone | `16:23:45 CET` |

### `datetime`

| FormatStyle | Beskrivelse | Eksempel (value=2025-04-17 16:23:45) |
|-------------|-------------|--------------------------------------|
| `short` | Kort dato + kort tid | `17.04.25 16:23` |
| `medium` | Medium dato + medium tid | `17. apr. 2025 16:23:45` |
| `long` | Lang dato + lang tid | `17. april 2025 16:23:45 CET` |
| `iso` | ISO 8601 | `2025-04-17T16:23:45` |

### `bool`

| FormatStyle | Beskrivelse | Eksempel (true / false) |
|-------------|-------------|------------------------|
| `truefalse` | Standard | `true` / `false` |
| `yesno` | Ja/nej (locale) | `yes` / `no` |
| `onoff` | Til/fra | `on` / `off` |
| `numeric` | Som tal | `1` / `0` |

---

## CustomPattern per FormatType

### `string`

Ingen CustomPattern support.

### `integer`

| Mønster | Beskrivelse | Eksempel (value=1024) |
|---------|-------------|----------------------|
| `#,##0` | Tusindtal-separator, ingen decimaler | `1,024` |
| `00000` | Nul-padded til N cifre | `01024` |
| `#,##0;(#,##0)` | Positive;negative format | `1,024` / `(1,024)` |

### `float`

| Mønster | Beskrivelse | Eksempel (value=1234.5678) |
|---------|-------------|---------------------------|
| `#,##0.00` | Tusindtal, 2 decimaler | `1,234.57` |
| `0.000` | Mindst 3 decimaler | `1234.568` |
| `#,##0.00;(#,##0.00)` | Positive;negative | `1,234.57` / `(1,234.57)` |
| `0.##` | Op til 2 decimaler, ingen trailing zeros | `1234.57` |

### `date`

| Token | Beskrivelse | Eksempel (2025-04-17) |
|-------|-------------|----------------------|
| `yyyy` | 4-cifret år | `2025` |
| `yy` | 2-cifret år | `25` |
| `MMMM` | Fuldt månedsnavn | `April` |
| `MMM` | Kort månedsnavn | `Apr` |
| `MM` | Måned med leading zero | `04` |
| `M` | Måned uden leading zero | `4` |
| `dd` | Dag med leading zero | `17` |
| `d` | Dag uden leading zero | `17` |
| `EEEE` | Fuldt ugedag | `Thursday` |
| `EEE` | Kort ugedag | `Thu` |

**Eksempler:**

- `yyyy-MM-dd` → `2025-04-17`
- `dd/MM/yyyy` → `17/04/2025`
- `EEEE, d. MMMM yyyy` → `Thursday, 17. April 2025`
- `yyyy,MM/dd:EEE` → `2025,04/17:Thu`

### `time`

| Token | Beskrivelse | Eksempel (16:23:45.123) |
|-------|-------------|------------------------|
| `HH` | 24-timers med leading zero | `16` |
| `H` | 24-timers uden leading zero | `16` |
| `hh` | 12-timers med leading zero | `04` |
| `h` | 12-timers uden leading zero | `4` |
| `mm` | Minutter med leading zero | `23` |
| `m` | Minutter uden leading zero | `23` |
| `ss` | Sekunder med leading zero | `45` |
| `s` | Sekunder uden leading zero | `45` |
| `tt` | AM/PM | `PM` |
| `fff` | Millisekunder | `123` |

**Eksempler:**

- `HH:mm` → `16:23`
- `h:mm tt` → `4:23 PM`
- `HH:mm:ss.fff` → `16:23:45.123`

### `datetime`

Kombinerer alle tokens fra `date` og `time`.

**Eksempler:**

- `yyyy-MM-dd HH:mm:ss` → `2025-04-17 16:23:45`
- `dd/MM/yyyy h:mm tt` → `17/04/2025 4:23 PM`
- `EEEE, d. MMMM yyyy 'kl.' HH:mm` → `Thursday, 17. April 2025 kl. 16:23`

### `bool`

Ingen CustomPattern support.

---

## Functions per FormatType

### Functions for `string`

| Function | Parametre | Return type | Beskrivelse | Eksempel |
|----------|-----------|-------------|-------------|----------|
| `toUpper()` | — | string | Alle bogstaver store | `"hello"` → `"HELLO"` |
| `toLower()` | — | string | Alle bogstaver små | `"HELLO"` → `"hello"` |
| `trim()` | — | string | Fjern whitespace begge sider | `" hi "` → `"hi"` |
| `trimStart()` | — | string | Fjern whitespace i start | `" hi "` → `"hi "` |
| `trimEnd()` | — | string | Fjern whitespace i slutning | `" hi "` → `" hi"` |
| `padLeft(n)` | n: integer | string | Udfyld til venstre til længde n | `"hi".padLeft(5)` → `"   hi"` |
| `padRight(n)` | n: integer | string | Udfyld til højre til længde n | `"hi".padRight(5)` → `"hi   "` |
| `substring(start,length)` | start: integer, length: integer | string | Udtag delstreng | `"hello".substring(0,3)` → `"hel"` |
| `replace(old,new)` | old: string, new: string | string | Erstat alle forekomster | `"hello".replace(l,r)` → `"herro"` |
| `length()` | — | integer | Antal tegn | `"hello".length()` → `5` |
| `toCamelCase()` | — | string | Konverter til camelCase | `"hello world"` → `"helloWorld"` |
| `toPascalCase()` | — | string | Konverter til PascalCase | `"hello world"` → `"HelloWorld"` |
| `contains(str)` | str: string | bool | Indeholder delstreng | `"hello".contains(ell)` → `true` |
| `startsWith(str)` | str: string | bool | Starter med | `"hello".startsWith(he)` → `true` |
| `endsWith(str)` | str: string | bool | Slutter med | `"hello".endsWith(lo)` → `true` |
| `toInteger()` | — | integer | Parse til heltal | `"44"` → `44` |
| `toFloat()` | — | float | Parse til decimaltal | `"3.14"` → `3.14` |
| `toDate()` | — | date | Parse til dato | `"2025-04-17"` → date |
| `toTime()` | — | time | Parse til tid | `"16:23:00"` → time |
| `toDateTime()` | — | datetime | Parse til datetime | `"2025-04-17 16:23"` → datetime |
| `toBool()` | — | bool | Parse til boolean | `"true"` → `true` |

### Functions for `integer`

| Function | Parametre | Return type | Beskrivelse | Eksempel |
|----------|-----------|-------------|-------------|----------|
| `abs()` | — | integer | Absolut værdi | `-5` → `5` |
| `add(n)` | n: integer | integer | Adder | `10.add(5)` → `15` |
| `subtract(n)` | n: integer | integer | Subtraher | `10.subtract(3)` → `7` |
| `multiply(n)` | n: integer | integer | Multiplicer | `10.multiply(3)` → `30` |
| `divide(n)` | n: integer | integer | Heltalsdivision | `10.divide(3)` → `3` |
| `mod(n)` | n: integer | integer | Modulo | `10.mod(3)` → `1` |
| `max(n)` | n: integer | integer | Begræns til max | `1024.max(1000)` → `1000` |
| `min(n)` | n: integer | integer | Begræns til min | `5.min(10)` → `10` |
| `clamp(min,max)` | min: integer, max: integer | integer | Begræns til interval | `1024.clamp(0,100)` → `100` |
| `toString()` | — | string | Konverter til string | `44` → `"44"` |
| `toFloat()` | — | float | Konverter til float | `44` → `44.0` |

### Functions for `float`

| Function | Parametre | Return type | Beskrivelse | Eksempel |
|----------|-----------|-------------|-------------|----------|
| `abs()` | — | float | Absolut værdi | `-3.14` → `3.14` |
| `round(n)` | n: integer | float | Afrund til n decimaler | `3.14159.round(2)` → `3.14` |
| `ceil()` | — | integer | Rund op | `3.2.ceil()` → `4` |
| `floor()` | — | integer | Rund ned | `3.9.floor()` → `3` |
| `add(n)` | n: float | float | Adder | `1.5.add(2.3)` → `3.8` |
| `subtract(n)` | n: float | float | Subtraher | `5.0.subtract(1.5)` → `3.5` |
| `multiply(n)` | n: float | float | Multiplicer | `2.5.multiply(4)` → `10.0` |
| `divide(n)` | n: float | float | Divider | `10.0.divide(3)` → `3.333...` |
| `max(n)` | n: float | float | Begræns til max | `99.9.max(50.0)` → `50.0` |
| `min(n)` | n: float | float | Begræns til min | `1.5.min(10.0)` → `10.0` |
| `clamp(min,max)` | min: float, max: float | float | Begræns til interval | `99.9.clamp(0,50)` → `50.0` |
| `toString()` | — | string | Konverter | `3.14` → `"3.14"` |
| `toInteger()` | — | integer | Truncate | `3.9` → `3` |

### Functions for `date`

| Function | Parametre | Return type | Beskrivelse | Eksempel |
|----------|-----------|-------------|-------------|----------|
| `addDays(n)` | n: integer | date | Tilføj dage | `2025-04-17.addDays(1)` → `2025-04-18` |
| `addMonths(n)` | n: integer | date | Tilføj måneder | `2025-04-17.addMonths(1)` → `2025-05-17` |
| `addYears(n)` | n: integer | date | Tilføj år | `2025-04-17.addYears(1)` → `2026-04-17` |
| `year()` | — | integer | Udtræk år | `→ 2025` |
| `month()` | — | integer | Udtræk måned | `→ 4` |
| `day()` | — | integer | Udtræk dag | `→ 17` |
| `dayOfWeek()` | — | integer | Ugedag (0=søndag) | `→ 4` |
| `dayOfYear()` | — | integer | Dag i året | `→ 107` |
| `toDateTime()` | — | datetime | Konverter (midnight) | `→ 2025-04-17 00:00:00` |
| `toUnix()` | — | integer | Unix timestamp | `→ 1744848000` |
| `toString()` | — | string | Default format | `→ "2025-04-17"` |

### Functions for `time`

| Function | Parametre | Return type | Beskrivelse | Eksempel |
|----------|-----------|-------------|-------------|----------|
| `addHours(n)` | n: integer | time | Tilføj timer | `16:23 → 18:23` |
| `addMinutes(n)` | n: integer | time | Tilføj minutter | `16:23 → 16:53` |
| `addSeconds(n)` | n: integer | time | Tilføj sekunder | `16:23:45 → 16:24:00` |
| `hour()` | — | integer | Udtræk time | `→ 16` |
| `minute()` | — | integer | Udtræk minut | `→ 23` |
| `second()` | — | integer | Udtræk sekund | `→ 45` |
| `toString()` | — | string | Default format | `→ "16:23:45"` |

### Functions for `datetime`

| Function | Parametre | Return type | Beskrivelse |
|----------|-----------|-------------|-------------|
| `addDays(n)` | n: integer | datetime | Tilføj dage |
| `addMonths(n)` | n: integer | datetime | Tilføj måneder |
| `addYears(n)` | n: integer | datetime | Tilføj år |
| `addHours(n)` | n: integer | datetime | Tilføj timer |
| `addMinutes(n)` | n: integer | datetime | Tilføj minutter |
| `addSeconds(n)` | n: integer | datetime | Tilføj sekunder |
| `date()` | — | date | Udtræk dato-del |
| `time()` | — | time | Udtræk tids-del |
| `year()` | — | integer | Udtræk år |
| `month()` | — | integer | Udtræk måned |
| `day()` | — | integer | Udtræk dag |
| `hour()` | — | integer | Udtræk time |
| `minute()` | — | integer | Udtræk minut |
| `second()` | — | integer | Udtræk sekund |
| `dayOfWeek()` | — | integer | Ugedag (0=søndag) |
| `dayOfYear()` | — | integer | Dag i året |
| `toUnix()` | — | integer | Unix timestamp |
| `toDate()` | — | date | Samme som `.date()` |
| `toTime()` | — | time | Samme som `.time()` |
| `toString()` | — | string | Default format |

### Functions for `bool`

| Function | Parametre | Return type | Beskrivelse | Eksempel |
|----------|-----------|-------------|-------------|----------|
| `not()` | — | bool | Negering | `true` → `false` |
| `toString()` | — | string | Konverter | `true` → `"true"` |
| `toInteger()` | — | integer | true=1, false=0 | `true` → `1` |

---

## Extension-mekanisme

Projekter kan registrere egne **FormatTypes** med tilhørende FormatStyles, CustomPattern-handlers og Functions.

**Eksempel: Custom FormatType `person`**

| FormatStyle | Beskrivelse | Eksempel |
|-------------|-------------|----------|
| `formal` | Titel + efternavn | `Mr. Smith` |
| `informal` | Fornavn | `John` |
| `full` | Fuldt navn | `John Smith` |

| Function | Return type | Beskrivelse | Eksempel |
|----------|-------------|-------------|----------|
| `firstName()` | string | Udtræk fornavn | `→ "John"` |
| `lastName()` | string | Udtræk efternavn | `→ "Smith"` |
| `fullName()` | string | Sammensæt fuldt navn | `→ "John Smith"` |
| `initials()` | string | Initialer | `→ "J.S."` |

---

## Bemærkninger

- Functions er **kun tilgængelige hvis de er eksplicit registreret** for den pågældende FormatType.
- FormatStyles producerer **altid string** som output.
- CustomPattern-handlers er **type-specifikke** — en date-pattern virker ikke på en integer.
- Konverterings-functions (`toString()`, `toInteger()`, osv.) ændrer den aktuelle type og dermed hvilke functions der efterfølgende er tilgængelige.
- FormatStyle og CustomPattern er **mutually exclusive** — de kan ikke bruges samtidig.
- FormatType bestemmer hvilke FormatStyles og hvilken CustomPattern-handler der er tilgængelig.

---

## Processing Flow

    Variable (arg)
      → [Type Resolution] (runtime-type)
      → [Functions] (kædet, type kan ændre sig per function-kald)
      → [FormatType cast/convert] (valgfri)
      → [FormatStyle ELLER CustomPattern] (producerer string — aldrig begge)
      → Output (string)

