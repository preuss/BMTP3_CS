# Fuld syntaks for MessageFormatter

Her er den komplette syntaks for `MessageFormatter`, 
der afspejler alle beslutninger for navngivne og indeksbaserede pladsholdere, 
funktionskald, betingede formateringer, indlejrede pladsholdere og escape-regler.

Vi kalder udtrykket på dansk "pladsholdere" og på engelsk "placeholders".

## Syntaks
Dette er syntaksen ""Message Format Expression"":
- ${name[.function1()]*[, type[, style]][: customPattern]}
- #{index[.function1()]*[, type[, style]][: customPattern]}

Dette er syntaksen for ""Message Eval Expression"":
- ${name[.function1()]*[§ evalType[, evalPattern]]}
- #{index[.function1()]*[§ evalType[, evalPattern]]}


## Pladsholdere
- **Navngivne pladsholdere**: `${name}`.
    - Erstatter `name` med dens værdi.
    - Eksempel: `${filename}` → `photo.jpg`.
- **Indeksbaserede pladsholdere**: `#{index}`.
    - Erstatter `index` med værdien på den angivne position.
    - Eksempel: `#{0}` → `photo.jpg` (hvis argument 0 er `"photo.jpg"`).
- **Indlejrede pladsholdere**:
    - Inden for et `customPattern` eller evaluerede strenge bruges `${name}` eller `#{index}` til at referere til andre pladsholdere.
    - Eksempel: `${argument:yyyy,MM/dd:EEE ${filename}}` → `2025,04/17:Thu photo.jpg`.

## Syntaks-elementer
1. **Simple pladsholdere**:
    - `${name}` eller `#{index}`.
    - Eksempel: `${filename}` → `photo.jpg`, `#{0}` → `1024`.
2. **Med type**:
    - `${name,type}` eller `#{index,type}`.
    - Angiver typen af værdien (f.eks. `number`, `date`, `string`).
    - Eksempel: `${size,number}` → `1024`.
3. **Med type og stil**:
    - `${name,type,style}` eller `#{index,type,style}`.
    - Angiver en foruddefineret stil for typen (f.eks. `short` for `date`, `integer` for `number`).
    - Eksempel: `${date,date,short}` → `4/17/25`.
4. **Med brugerdefineret mønster (automatisk typedetektion)**:
    - `${name:customPattern}` eller `#{index:customPattern}`.
    - Formaterer værdien med et brugerdefineret mønster; typen detekteres automatisk.
    - Eksempel: `${date:yyyy,MM/dd:EEE}` → `2025,04/17:Thu`.
5. **Med eksplicit type og brugerdefineret mønster**:
    - `${name,type:customPattern}` eller `#{index,type:customPattern}`.
    - Formaterer værdien med et mønster, valideret mod den angivne type.
    - Eksempel: `${date,date:yyyy-MM-dd}` → `2025-04-17`.
    - Fejler, hvis mønstret ikke understøttes af typen (f.eks. `${date,number:yyyy-MM-dd}`).
6. **Med type, stil og brugerdefineret mønster**:
    - `${name,type,style:customPattern}` eller `#{index,type,style:customPattern}`.
    - Formaterer værdien med et mønster, hvor stilen kan påvirke, hvordan mønstret fortolkes.
    - Eksempel: `${amount,number,decimal:2:#,##0.00}` → `1,234.56`.
    - Fejler, hvis stilen ikke understøtter mønstret.
7. **Med funktionskald**:
    - `${name.function1().function2()}` eller `#{index.function1().function2()}`.
    - Udfører funktioner i rækkefølge, før formatering (type, stil, mønster) eller evaluering anvendes.
    - Parametre i parenteser sendes som en streng (f.eks. `.toString(4,5)` sender `4,5`).
    - Eksempel: `${filename.toUpper().trim()}` → `PHOTO.JPG`, `${size.toString(4,5)}` sender `4,5` til `toString`.

## Evalueringsudtryk (Message Eval Expressions)
Evalueringsudtryk bruges til at vælge et tekstsegment baseret på den tilhørende pladsholders værdi. De giver en klar og programmerervenlig måde at håndtere betinget logik på.

1. **`if` (betinget valg)**:
    - `${name § if,condition?trueString:falseString}` eller `#{index § if,condition?trueString:falseString}`.
    - Vælger `trueString` hvis betingelsen er sand; ellers `falseString`. Begge kan indeholde indlejrede pladsholdere.
    - **Betingelser**:
        - `eqN`: Lighed med værdi N (f.eks. `eq0`, `eq1`).
        - `gtN`: Større end N (f.eks. `gt0`).
        - `gteN`: Større end eller lig med N.
        - `ltN`: Mindre end N.
        - `lteN`: Mindre end eller lig med N.
        - `neN`: Ikke lig med N.
        - `in(list)`: Værdien er i listen (f.eks. `in(1,2,3)`).
        - `nin(list)`: Værdien er ikke i listen (f.eks. `nin(0,1)`).
    - Eksempel: `${count § if,eq0?ingen filer:${other} filer}` → `to filer` (med `count=2`, `other="to"`).
    - Eksempel: `${size § if,gt1000?stor:lille}` → `stor` (med `size=1024`).
    - Eksempel: `${count § if,in(1,2)?valgt:ikke valgt}` → `valgt` (med `count=2`).

2. **`plural` (flertalshåndtering)**:
    - `${name § plural,countRule#valueRule|countRule#valueRule|...[|other#defaultValue]}` eller `#{index § plural,countRule#valueRule|...}`.
    - Vælger en streng baseret på et numerisk antal og sprogspecifikke flertalsregler.
    - `countRule` kan være:
        - Eksakte tal: `0`, `1`, `2`, osv.
        - CLDR-kategorier: `one`, `few`, `many`, `zero` (afhængig af sprog).
        - Ranges: `[X;Y]` (inklusive X og Y), `]X;Y[` (eksklusive X og Y).
        - **`other`**: En valgfri `countRule` der fungerer som en standardværdi, hvis ingen anden regel matcher. Skal stå sidst i listen.
    - `valueRule` er den tekst, der skal bruges, og kan indeholde indlejrede pladsholdere.
    - Eksempel: `${numItems § plural,0#ingen varer|1#én vare|one#enkel vare|other#${numItems} varer}` → `3 varer` (med `numItems=3`).
    - Eksempel: `${numFiles § plural,0#nul filer|1#en fil|other#${numFiles} filer}` (hvis sproget kun har `one` og `other` som flertalskategorier).
    - Eksempel med intervaller: `${age § plural,[0;10]#barn|]10;18]#teenager|other#voksen}`

3. **`select` (kategorisk valg)**:
    - `${name § select,category1#value1|category2#value2|...[|other#defaultValue]}` eller `#{index § select,category1#value1|...}`.
    - Vælger en streng baseret på en kategorisk (streng) værdi.
    - `category` er en streng, der matches direkte med inputværdien (case-sensitiv).
    - **`other`**: En valgfri `category` der fungerer som en standardværdi, hvis ingen anden kategori matcher. Skal stå sidst i listen.
    - `value` er den tekst, der skal bruges, og kan indeholde indlejrede pladsholdere.
    - Eksempel: `${gender § select,male#han|female#hun|other#ukendt}` → `hun` (med `gender="female"`).
    - Eksempel: `${status § select,active#bruger er aktiv|inactive#bruger er inaktiv|other#ukendt status}`

## Kombinationer
- Funktionskald anvendes før formatering eller evaluering.
    - Eksempel: `${filename.toUpper(),string}` → `PHOTO.JPG`.
    - Eksempel: `${count.abs() § if,eq0?ingen filer:${other.toUpper()} filer}` → `TO filer`.

## Escape-regler
- **I `customPattern` og `evalPattern`**:
    - `}` escapes med `}}` for at angive en literal `}`.
    - Eksempel: `${argument:yyyy}}MM}` → `yyyy}MM`.
    - `{` escapes med `{{` for at angive en literal `{`, især for literale pladsholdere.
    - Eksempel: `${argument:yyyy,MM/dd:EEE {{filename}}}}` → `2025,04/17:Thu {filename}`.
    - Andre tegn (`:`, `,`, `/`, osv.) kræver ikke escaping, da de håndteres af kontekstbevidst parsing.
- **I funktionskald**:
    - Alt mellem `(` og `)` behandles som parametre og kræver ikke escaping.
    - Eksempel: `${argument.toString(4,5)}` sender `4,5` til `toString`.
- **I `type` og `style`**:
    - Ingen escaping nødvendig, da de er foruddefinerede og fri for specialtegn (f.eks. `number`, `date`, `short`).
- **I evaluerede strenge (f.eks. `if`'s `trueString`/`falseString`, `plural`/`select`'s `valueRule`)**:
    - Kan indeholde pladsholdere (`${name}`, `#{index}`).
    - Literale `{` og `}` escapes med `{{` og `}}`, hvis de skal udskrives.
    - Eksempel: `${count § if,eq0?ingen {{filer}}:filer}` → `ingen {filer}` (med `count=0`).

## Formateringstyper og stilarter
1. **`number`**:
    - Stilarter: `integer`, `currency`, `percent`, `decimal:N` (N = antal decimaler).
    - Mønstre: `'#,##0.00'`, `'0.00'`.
    - Eksempel: `${size,number,integer}` → `1024`, `${amount:#,##0.00}` → `1,234.56`.
2. **`date`**:
    - Stilarter: `short`, `long`.
    - Mønstre: `'yyyy-MM-dd'`, `'yyyy,MM/dd:EEE'`.
    - Eksempel: `${date,date,short}` → `4/17/25`, `${date:yyyy,MM/dd:EEE}` → `2025,04/17:Thu`.
3. **`string`**:
    - Har ingen foruddefinerede stilarter. Strengmanipulation håndteres via funktioner.
    - Eksempel: `${filename,string}` → `photo.jpg`.

## Funktioner
- Funktionskald udføres før formatering eller evaluering.
- Standardiserede funktioner (porterbare):
    1. **Generelle**:
        - `toString()`: Konverter til streng (f.eks. `${size.toString()}` → `"1024"`).
    2. **Strengmanipulation**:
        - `toUpper()`: Store bogstaver (f.eks. `${filename.toUpper()}` → `PHOTO.JPG`).
        - `toLower()`: Små bogstaver (f.eks. `${filename.toLower()}` → `photo.jpg`).
        - `toCamelCase()`: Konverter til camelCase (f.eks. `${filename.toCamelCase()}` → `photoJpg`).
        - `trim()`: Fjern mellemrum (f.eks. `${filename.trim()}` → `photo.jpg`).
        - `padLeft(n)`: Udfyld til venstre (f.eks. `${filename.padLeft(10)}` → `   photo.jpg`).
        - `padRight(n)`: Udfyld til højre (f.eks. `${filename.padRight(10)}` → `photo.jpg   `).
        - `substring(start,length)`: Udtag delstreng (f.eks. `${filename.substring(0,5)}` → `photo`).
    3. **Talmanipulation**:
        - `abs()`: Absolut værdi (f.eks. `${size.abs()}` → `1024`).
        - `round(n)`: Rund til n decimaler (f.eks. `${amount.round(2)}` → `123.45`).
        - `max(n)`: Begræns til maksimum n (f.eks. `${size.max(1000)}` → `1000`).
        - `min(n)`: Begræns til minimum n (f.eks. `${size.min(0)}` → `0`).
    4. **Datamanipulation**:
        - `addDays(n)`: Tilføj n dage (f.eks. `${date.addDays(1)}` → næste dag).
        - `toUnix()`: Konverter til Unix-timestamp (f.g. `${date.toUnix()}` → `1744934400`).

## Eksempler
- **Simple pladsholdere**:
    - `${filename}` → `photo.jpg`.
    - `#{0}` → `1024`.
- **Type og stil**:
    - `${size,number,integer}` → `1024`.
    - `${date,date,short}` → `4/17/25`.
- **Brugerdefinerede mønstre**:
    - `${date:yyyy,MM/dd:EEE}` → `2025,04/17:Thu`.
    - `${date,date:yyyy-MM-dd}` → `2025-04-17`.
    - `${amount,number,decimal:2:#,##0.00}` → `1,234.56`.
- **Funktionskald**:
    - `${filename.toUpper().trim()}` → `PHOTO.JPG`.
    - `${size.toString(4,5)}` → `"1024"` (med parametre `4,5`).
- **Evalueringsudtryk (`if`, `plural`, `select`)**:
    - `${count § if,eq0?ingen filer:${other} filer}` → `to filer` (med `count=2`, `other="to"`).
    - `${size § if,gt1000?stor:lille}` → `stor` (med `size=1024`).
    - `${count § if,in(1,2)?valgt:ikke valgt}` → `valgt` (med `count=2`).
    - `${numItems § plural,0#ingen varer|1#én vare|other#${numItems} varer}` → `3 varer` (med `numItems=3`).
    - `${gender § select,male#han|female#hun|other#ukendt}` → `hun` (med `gender="female"`).
- **Indlejrede pladsholdere**:
    - `${argument:yyyy,MM/dd:EEE ${filename}}` → `2025,04/17:Thu photo.jpg`.
    - `${argument:yyyy,MM/dd:EEE {{filename}}}}` → `2025,04/17:Thu {filename}`.
- **Kombinationer**:
    - `${filename.toUpper(),string}` → `PHOTO.JPG`.
    - `${count.abs() § if,eq0?ingen filer:${other.toUpper()} filer}` → `TO filer`.
- **Escape**:
    - `${argument:yyyy}}MM}` → `yyyy}MM`.
    - `${argument:{{filename}}}}` → `{filename}`.
    - `${argument:yyyy,MM}}/dd:EEE {{filename}}}}` → `2025,04/17:Thu {filename}`.

## Fejlhåndtering (eksempler)
- `${date:yyyy}` → Fejl: “Ubalanceret `}`”.
- `${date.toUpper()}` → Fejl: “Funktionen `toUpper` understøttes ikke for type `DateTime`”.
- `${count § if,eq0?ingen filer}` med `count=2` → Fejl: “Ingen matchende betingelse for værdi 2”.
- `${date,number:yyyy-MM-dd}` → Fejl: “Mønster `yyyy-MM-dd` understøttes ikke for type `number`”.
- `${count § ukendt,mønster}` → Fejl: “Ukendt evaluerings-type 'ukendt'”.