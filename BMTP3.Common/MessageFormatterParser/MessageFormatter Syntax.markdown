# 1. Fuld syntaks for MessageFormatter

Her er den komplette syntaks for `MessageFormatter`, 
der afspejler alle beslutninger for navngivne og indeksbaserede pladsholdere, 
funktionskald, betingede formateringer, indlejrede pladsholdere og escape-regler.

Vi kalder udtrykket på dansk "pladsholdere" og på engelsk "placeholders".

## 1.1. Pladsholderudtryk (Placeholder Expressions)

Pladsholderudtryk definerer, hvordan data hentes og behandles, før de indsættes i beskeden. 
Der er to hovedtyper af pladsholderudtryk: Formateringsudtryk og Evalueringsudtryk.

## 1.2. Syntaksen
Dette er syntaksen for formatteringudtryk "Message Format Expression":
- ${name[.function()]*[, formatType[, formatStyle]][: customPattern]}
- #{index[.function()]*[, formatType[, formatStyle]][: customPattern]}

Dette er syntaksen for evalueringsudtryk "Message Eval Expression":
- ${name[.function()]*[§ evalType[, evalPattern]]}
- #{index[.function()]*[§ evalType[, evalPattern]]}

Definering af evalueringsudtryk til evalTyp, som if bruger tegnet § eller ¶ (0182 / paragraftegnet).

Alle literaler som int og double er bare sig selv skal ikke omsluttes som string literal.

String literal:
- ''' (trippel enkelte anførselstegn)
- """ (trippel dobbelte anførselstegn)
- ´ (acute accent / tick)
- ` (gravis accent / backtick)
- / (slash)
- Escape er: med \ (backslash)

---

## 1.3. Detaljer om syntaks-elementerne

### 1.3.1. Pladsholderudtryk

#### 1.3.1.1. Navngivet pladsholder (Named Placeholder)

Ved navngivet pladsholder refererer `name` til et navn, der kan udskiftes med en værdi.

**Navngivne pladsholdere**: `${name}`.
- Det betyder at `name` erstattes med en variabel som indeholder en værdi.
- Eksempel: `${filename}` → `photo.jpg`

#### 1.3.1.2. Indeksbaseret pladsholder (Indexed Placeholder)

Ved indeksbaseret pladsholder refererer `index` til et positivt heltal, der kan udskiftes med en værdi.

**Indeksbaserede pladsholdere**: `#{index}`.
- Det betyder at `index` erstattes med en værdi fra argumenter med den angivne position.
- Eksempel: `#{0}` → `photo.jpg` (hvis argument index 0 er `"photo.jpg"`).
- Eksempel: `#{ 1 }` → `1024` (hvis argument index 1 er `"1024"`).

#### 1.3.1.3. Indlejrede pladsholdere

Inden for et `customPattern` eller evaluerede strenge bruges `${name}` eller `#{index}` til at referere til andre pladsholdere.
- Eksempel: `${argument:yyyy,MM/dd:EEE ${filename}}` → `2025,04/17:Thu photo.jpg`.

### 1.3.2. Funktioner (Functions)

Funktionskald er ligesom funktionskald i programmering, der eksisterer nogle funktioner man kan kalde.
Flere funktioner kan kædes sammen. Og kaldes i serie, hver funktion kaldes på den forriges resultat.
Funktioner udføres i rækkefølge, før formatering (type, stil, mønster) eller evaluering anvendes.
Parametre i parenteser sendes som argumenter til funktionen 
(f.eks. `.toString(4,5)` eller `.toUpper(4,5)` sender `4,5` som er to argumenter til denne funktion).

Funktionskald anvendes før formatering eller betingelser.

- Eksempel:
- Syntaks: `.<funktion>()`
    - Beskrivelse: Udfører en funktion på pladsholderens værdi. 
    - Flere funktioner kan kædes sammen (`.func1().func2()`). Parametre i parenteser sendes som en streng.
- Eksempel: `.toUpper()`
- Eksempel: `${filename.toUpper().trim()}` → `PHOTO.JPG`.
- Eksempel med parametre: `.toString(4,5)` sender `4,5` til `toString`.

### 1.3.3. Type (formatType)
Dette er type (formatType) i syntaksen `${name,formatType,formatStyle}` eller `#{index,formatType,formatStyle}`.
Angiver en foruddefineret type for den specificerede `formatType` (f.eks. `number` for tal, `date` for datoer).
	- Eksempel: `${size,number}` → `1024`.
	- Eksempel: `${currentTimeOfDay,time}` → `16:23`.

### 1.3.4. Stil (formatStyle)
Dette er stil (formatStyle) i syntaksen `${name,formatType,formatStyle}` eller `#{index,formatType,formatStyle}`.
Angiver en foruddefineret stil for typen (f.eks. `short` for `date`, `integer` for `number`).
- Eksempel med date : `${date,date,short}` → `17.04.25`.
- Eksempel med date : `${date,date,medium}` → `17. April 2025`.
	

### 1.3.4. Brugerdefineret mønster (customPattern)
Med brugerdefineret mønster (automatisk typedetektion).
Et brugerdefineret formateringsmønster, der anvendes på pladsholderens værdi, 
i syntaksen `${name:customPattern}` eller `#{index:customPattern}`.
Hvis `type` ikke er specificeret, forsøges typen at blive detekteret automatisk.
- Eksempel med date: `${date:dd.MM.yy}` → `17.04.25`.
- Eksempel: `${date:yyyy,MM/dd:EEE}` → `2025,04/17:Thu`.

Hvis `formatType`er specificeret, bruges typen, og det betyder at kun stil (formatStyle) understøttes.
- Eksempel med dato: `${currentDate, date:dd.MM.yy}` → `17.04.25`.
- Fejler, hvis mønstret ikke understøttes af typen (f.eks. `${date,number:yyyy-MM-dd}`).
	Denne vil fejle, fordi type: number understøtter ikke dato formattering.

Hvis `formatType`, `formatStyle` og `customPattern` er defineret:
- `${name,type,style:customPattern}` eller `#{index,type,style:customPattern}`.
- Formaterer værdien med et mønster, hvor stilen kan påvirke, hvordan mønstret fortolkes.
- Eksempel: `${amount,number,decimal:2:#,##0.00}` → `1,234.56`.
	- Ville fejlefejle hvis stilen ikke understøtter mønstret.

### 1.3.5. Evaluering-type (Eval Type)

- Syntaks: `§ evalType`
- Beskrivelse: Angiver den specifikke type af evaluering, der skal udføres på pladsholderens værdi. De definerede `evalType`s er `if`, `plural`, og `select`.
- Eksempel: `§ if`

### 1.3.6. Evaluerings-mønster (Eval Pattern)
- Syntaks: `[, evalPattern]`
- Beskrivelse: Indeholder de specifikke regler eller betingelser, der anvendes af den valgte `evalType`. Formatet af mønstret afhænger af `evalType`.
- Eksempel: `, condition?trueString:falseString` (for `if`)

### 1.4. Evalueringsudtryk i detaljer (Message Eval Expressions)

Denne sektion beskriver de specifikke `evalType`s og deres `evalPattern`-formater.

Evalueringsudtryk bruges til at vælge et tekstsegment baseret på den tilhørende pladsholders værdi. 
De giver en klar og programmerervenlig måde at håndtere betinget logik på.

#### 1.4.1. Betinget valg (conditional statement)

Syntaks for betinget kontrolstruktur (conditional control structure):
- Syntaks: `${name[.<funktion>()]* § if,condition?trueValue:falseValue}`
- Formålet er at lave en ternary operator.
- Hvis `condition` er sand, vælges `trueValue`.
- Hvis `condition` er falsk, vælges `falseValue`.
- Begge kan indeholde indlejrede pladsholdere.

Liste af betingelse (confition):
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
- Eksempel: `${filename.toUpper(),string,lower}` → `photo.jpg`.
- Eksempel: `${count.abs(),if,eq0?no files:${other.toUpper()} files}` → `TWO files`.

Jeg har lidt mere til denne:
- Fuld med betinget: ${ <placeholderName> [.<functionName>( [<argument>,]* )]* § <evalType>, <evalPattern> }
- Med if: ${ <placeholderName> [.<functionName>( [<argument>,]* )]* § if, <conditionOperator> <conditionComparator>?<trueValue>:<falseValue> }
	- eval pattern er så med if: <conditionOperator> <conditionComparator>?<trueValue>:<falseValue>

## 1.5. Escape-regler

- **I `customPattern` og `evalPattern`**:
	- `}` escapes med `}}` for at angive en literal `}`.
		- Eksempel: `${argument:yyyy}}MM}` → `yyyy}MM`.
	- `{` escapes med `{{` for at angive en literal `{`, især for literale pladsholdere.
		- Eksempel: `${argument:yyyy,MM/dd:EEE {{filename}}}}` → `2025,04/17:Thu {filename}`.
	- Andre tegn (`:`, `,`, `/`, osv.) kræver ikke escaping, da de håndteres af kontekstbevidst parsing.
- **I funktionskald**:
	- Alt mellem `(` og `)` behandles som parametre og kræver ikke escaping.
	- Eksempel: `${argument.toString(4,5)}` sender `4,5` til `toString`.
	- Eksempel: `${argument.toUpper(4,5)}` sender `4,5` til `toUpper`.
- **I `type` og `style`**:
	- Ingen escaping nødvendig, da de er foruddefinerede og fri for specialtegn (f.eks. `number`, `date`, `short`).
- **I evaluerede strenge (`if`)**:
	- `trueString` og `falseString` kan indeholde pladsholdere (`${name}`, `#{index}`).
	- Literale `{` og `}` escapes med `{{` og `}}`, hvis de skal udskrives.
		- Eksempel: `${count,if,eq0?no {{files}}:files}` → `no {files}` (med `count=0`).

## 1.6. Formateringstyper og stilarter

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

## 1.7. Funktioner

Funktionskald udføres på pladsholderens værdi, før formatering eller evaluering anvendes.
Funktionskald udføres før formatering eller betingelser.

Standardiserede funktioner (porterbare):
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

## 1.8. Eksempler

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

## 1.9. Fejlhåndtering (eksempler)
- `${date:yyyy}` → Fejl: “Ubalanceret `}`”.
- `${date.toUpper()}` → Fejl: “Funktionen `toUpper` understøttes ikke for type `DateTime`”.
- `${count,if,eq0?no files}` med `count=2` → Fejl: “Ingen matchende betingelse for værdi 2”.
- `${date,number:yyyy-MM-dd}` → Fejl: “Mønster `yyyy-MM-dd` understøttes ikke for type `number`”.