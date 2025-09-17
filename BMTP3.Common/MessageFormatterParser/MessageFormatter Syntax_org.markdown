# Fuld syntaks for MessageFormatter

Her er den komplette syntaks for `MessageFormatter`, der afspejler alle beslutninger for navngivne og indeksbaserede pladsholdere, funktionskald, betingede formateringer, indlejrede pladsholdere og escape-regler.

Vi kalder udtrykket på dansk er "pladsholdere" og på engelsk "placeholders".
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
	- Inden for et `customPattern` eller betingede strenge bruges `${name}` eller `#{index}` til at referere til andre pladsholdere.
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
	- Udfører funktioner i rækkefølge, før formatering (type, stil, mønster) anvendes.
	- Parametre i parenteser sendes som en streng (f.eks. `.toUpper(4,5)` sender `4,5`).
		- Eksempel: `${filename.toUpper().trim()}` → `PHOTO.JPG`, `${size.toString(4,5)}` sender `4,5` til `toString`.
8. **Med betingede formateringer**:
	- `${name,if,condition?trueString:falseString}` eller `#{index,if,condition?trueString:falseString}`.
	- Vælger `trueString` eller `falseString` baseret på betingelsen; begge kan indeholde indlejrede pladsholdere.
	- **Betingelser**:
		- `eqN`: Lighed med værdi N (f.eks. `eq0`, `eq1`).
		- `gtN`: Større end N (f.eks. `gt0`).
		- `gteN`: Større end eller lig med N.
		- `ltN`: Mindre end N.
		- `lteN`: Mindre end eller lig med N.
		- `neN`: Ikke lig med N.
		- `in(list)`: Værdien er i listen (f.eks. `in(1,2,3)`).
		- `nin(list)`: Værdien er ikke i listen (f.eks. `nin(0,1)`).
	- Eksempel: `${count,if,eq0?no files:${other} files}` → `two files` (med `count=2`, `other="two"`).
	- Eksempel: `${size,if,gt1000?large:small}` → `large` (med `size=1024`).
9. **Kombinationer**:
	- Funktionskald anvendes før formatering eller betingelser.
		- Eksempel: `${filename.toUpper(),string,lower}` → `photo.jpg`.
		- Eksempel: `${count.abs(),if,eq0?no files:${other.toUpper()} files}` → `TWO files`.

## Escape-regler
- **I `customPattern`**:
	- `}` escapes med `}}` for at angive en literal `}`.
		- Eksempel: `${argument:yyyy}}MM}` → `yyyy}MM`.
	- `{` escapes med `{{` for at angive en literal `{`, især for literale pladsholdere.
		- Eksempel: `${argument:yyyy,MM/dd:EEE {{filename}}}}` → `2025,04/17:Thu {filename}`.
	- Andre tegn (`:`, `,`, `/`, osv.) kræver ikke escaping, da de håndteres af kontekstbevidst parsing.
- **I funktionskald**:
	- Alt mellem `(` og `)` behandles som parametre og kræver ikke escaping.
	- Eksempel: `${argument.toUpper(4,5)}` sender `4,5` til `toUpper`.
- **I `type` og `style`**:
	- Ingen escaping nødvendig, da de er foruddefinerede og fri for specialtegn (f.eks. `number`, `date`, `short`).
- **I betingede strenge (`if`)**:
	- `trueString` og `falseString` kan indeholde pladsholdere (`${name}`, `#{index}`).
	- Literale `{` og `}` escapes med `{{` og `}}`, hvis de skal udskrives.
		- Eksempel: `${count,if,eq0?no {{files}}:files}` → `no {files}` (med `count=0`).

## Formateringstyper og stilarter
1. **number**:
	- Stilarter: `integer`, `currency`, `percent`, `decimal:N` (N = antal decimaler).
	- Mønstre: `'#,##0.00'`, `'0.00'`.
	- Eksempel: `${size,number,integer}` → `1024`, `${amount:#,##0.00}` → `1,234.56`.
2. **date**:
	- Stilarter: `short`, `long`.
	- Mønstre: `'yyyy-MM-dd'`, `'yyyy,MM/dd:EEE'`.
	- Eksempel: `${date,date,short}` → `4/17/25`, `${date:yyyy,MM/dd:EEE}` → `2025,04/17:Thu`.
3. **string**:
	- Stilarter: `upper`, `lower`, `title` (kan fjernes, hvis funktioner som `toUpper()` foretrækkes).
	- Eksempel: `${filename,string,upper}` → `PHOTO.JPG`.
4. **if**:
	- Stil: `condition?trueString:falseString` (f.eks. `eq0?no files:${other} files`).
	- Betingelser: `eqN`, `gtN`, `gteN`, `ltN`, `lteN`, `neN`, `in(list)`, `nin(list)`.
	- Eksempel: `${count,if,eq0?no files:${other} files}` → `two files`.

## Funktioner
- Funktionskald udføres før formatering eller betingelser.
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
	4. **Datomanipulation**:
		- `addDays(n)`: Tilføj n dage (f.eks. `${date.addDays(1)}` → næste dag).
		- `toUnix()`: Konverter til Unix-timestamp (f.eks. `${date.toUnix()}` → `1744934400`).
- Eksempel: `${filename.toUpper().trim()}` → `PHOTO.JPG`, `${size.toString(4,5)}` sender `4,5` til `toString`.

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
- **Betingede formateringer**:
	- `${count,if,eq0?no files:${other} files}` → `two files` (med `count=2`, `other="two"`).
	- `${size,if,gt1000?large:small}` → `large` (med `size=1024`).
	- `${count,if,in(1,2)?selected:not selected}` → `selected` (med `count=2`).
- **Indlejrede pladsholdere**:
	- `${argument:yyyy,MM/dd:EEE ${filename}}` → `2025,04/17:Thu photo.jpg`.
	- `${argument:yyyy,MM/dd:EEE {{filename}}}}` → `2025,04/17:Thu {filename}`.
- **Kombinationer**:
	- `${filename.toUpper(),string,lower}` → `photo.jpg`.
	- `${count.abs(),if,eq0?no files:${other.toUpper()} files}` → `TWO files`.
- **Escape**:
	- `${argument:yyyy}}MM}` → `yyyy}MM`.
	- `${argument:{{filename}}}}` → `{filename}`.
	- `${argument:yyyy,MM}}/dd:EEE {{filename}}}}` → `2025,04/17:Thu {filename}`.

## Fejlhåndtering (eksempler)
- `${date:yyyy}` → Fejl: “Ubalanceret `}`”.
- `${date.toUpper()}` → Fejl: “Funktionen `toUpper` understøttes ikke for type `DateTime`”.
- `${count,if,eq0?no files}` med `count=2` → Fejl: “Ingen matchende betingelse for værdi 2”.
- `${date,number:yyyy-MM-dd}` → Fejl: “Mønster `yyyy-MM-dd` understøttes ikke for type `number`”.