# MessageFormatter Syntax v4

## 1. Introduction

This document defines the complete syntax for `MessageFormatter`. It covers named and
indexed placeholders, function calls, format expressions, eval expressions (conditional
logic), nested placeholders, and escape rules.

For available FormatTypes, FormatStyles, CustomPatterns, and Functions, see the separate
**MessageFormatter Function Reference** document.

### 1.1. Message Format Expression

    ${name[.function()]* [, FormatType [, FormatStyle]] [: CustomPattern]}
    #{index[.function()]* [, FormatType [, FormatStyle]] [: CustomPattern]}

FormatStyle and CustomPattern are **mutually exclusive** — only one can be used.

### 1.2. Message Eval Expression

    ${name[.function()]* § EvalType, EvalPattern}
    #{index[.function()]* § EvalType, EvalPattern}

The eval separator is `§` (U+00A7) or `¶` (U+00B6, Alt+0182). Both are equivalent.

---

## 2. Terminology

| Term | Description |
|------|-------------|
| **Placeholder** | An expression inside a message that gets replaced with a value at runtime |
| **Variable** | The runtime value referenced by a placeholder's name or index |
| **Function** | A registered operation that transforms a variable's value |
| **FormatType** | A type assertion that declares the expected type of the value and determines available FormatStyles and CustomPatterns |
| **FormatStyle** | A predefined named formatter registered for a specific FormatType (produces string) |
| **CustomPattern** | A user-defined formatting pattern supported by the FormatType (produces string) |
| **EvalType** | A type of evaluation expression (`if`, `plural`, `select`) |
| **EvalPattern** | The pattern/rules used by the EvalType to select output |

---

## 3. Placeholder Syntax Overview

There are two main categories of placeholder expressions:

### 3.1. Format Expression

Used for value formatting (functions, type assertion, style/pattern output).

    ${name[.function()]* [, FormatType [, FormatStyle]] [: CustomPattern]}
    #{index[.function()]* [, FormatType [, FormatStyle]] [: CustomPattern]}

**Rule:**
FormatStyle and CustomPattern are **mutually exclusive** — you cannot use both.
CustomPattern **requires** FormatType to be specified.

Valid combinations:

    ${name}
    ${name.function()}
    ${name, FormatType}
    ${name, FormatType, FormatStyle}
    ${name, FormatType : CustomPattern}
    ${name.function(), FormatType, FormatStyle}
    ${name.function(), FormatType : CustomPattern}


### 3.2. Eval Expression

Used for conditional/selection logic based on the variable's value.

    ${name[.function()]* § EvalType, EvalPattern}
    #{index[.function()]* § EvalType, EvalPattern}

The eval separator is `§` (section sign) or `¶` (pilcrow sign, Alt+0182). Both are
equivalent.

    ${name § EvalType, EvalPattern}
    ${name ¶ EvalType, EvalPattern}

---

## 4. Processing Flow

When a placeholder is evaluated, the following steps occur in order:

### Format Expression

    [Variable] → [Type Resolution] → [Functions] → [FormatType Assertion] → [FormatStyle|CustomPattern] → [String]

Detailed:

    Variable (arg)
    → [Type Resolution] (determine runtime type)
    → [Functions] (execute chained functions on current type; type may change)
    → [FormatType Assertion] (assert value matches declared type — error if not)
    → [FormatStyle OR CustomPattern] (format to string — never both)
    → Output (string)

### Eval Expression

    [Variable] → [Type Resolution] → [Functions] → [EvalType + EvalPattern] → [String]

Detailed:

    Variable (arg)
    → [Type Resolution] (determine runtime type)
    → [Functions] (execute chained functions on current type; type may change)
    → [EvalType + EvalPattern] (evaluate condition, select output segment)
    → Output (string, may contain nested placeholders)

---

## 5. Placeholders

A placeholder is a marker in a text string that gets replaced with a value at runtime.
The placeholder syntax defines how the formatter identifies which value to insert.

Every placeholder value has a **type** determined at runtime (e.g., string, number,
datetime, boolean). The type controls which functions, FormatTypes, FormatStyles, and
CustomPatterns are available. Functions may change the type.

### 5.1. Named Placeholder

A named placeholder references a variable by name. When the message is formatted,
the placeholder is replaced with the value of the variable that matches the name.

    ${name}

- `name` is an identifier: starts with `a-z`, `A-Z`, or `_`, followed by `a-z`, `A-Z`,
  `0-9`, or `_`.
- Whitespace inside the braces is allowed and ignored: `${ name }` is equivalent to
  `${name}`.

**Example:**

    "Hello ${userName}" → "Hello John"

### 5.2. Indexed Placeholder

An indexed placeholder references a variable by its position in the argument list.
When the message is formatted, the placeholder is replaced with the value at that
position.

    #{index}

- `index` is a zero-based positive integer.
- Whitespace inside the braces is allowed and ignored: `#{ 0 }` is equivalent to `#{0}`.

**Example:**

    "File #{0} is #{1} bytes" → "File photo.jpg is 1024 bytes"

### 5.3. Nested Placeholders

Within a CustomPattern or EvalPattern output strings, placeholders can be
nested to reference other variables.

**Example:**

    ${created, datetime : YYYY-MM-DD ${filename}}   → "2025-04-17 photo.jpg"

---

## 6. Functions

Functions are registered operations that can be called on a variable's resolved value.
Multiple functions can be chained, where each function executes on the result of the
previous one.

### 6.1. Syntax

    ${variable.functionName()}
    ${variable.functionName(arg1)}
    ${variable.functionName(arg1, arg2)}
    ${variable .functionName()}
    ${variable. functionName()}
    ${variable.functionName ()}

- Function names follow identifier rules: starts with a letter (a-z, A-Z) or underscore,
  followed by letters, digits, or underscores.
- Whitespace is allowed:
  - Between the variable name and `.` (dot)
  - Between `.` (dot) and the function name
  - Between the function name and `(` (opening parenthesis)
- Arguments inside parentheses are passed to the function.
- Arguments are separated by `,` (comma).
- `)` (closing parenthesis) ends the argument list.
- Functions execute before FormatType assertion and before FormatStyle/CustomPattern/EvalType.

### 6.2. Arguments

An argument is either **quoted** or **unquoted**. These cannot be mixed —
if quotes do not enclose the entire argument, it is a syntax error.

#### 6.2.1. Unquoted Arguments

- Leading and trailing whitespace is trimmed.
- `,` (comma) separates arguments.
- `)` (closing parenthesis) ends the argument list.
- Use `\` (backslash) to escape special characters.

**Examples:**

    .function(hello world)              → "hello world"
    .function( hello world )            → "hello world"
    .function( hello world , second )   → "hello world", "second"
    .function(hello\, world)            → "hello, world"
    .function(hello\) world)            → "hello) world"
    .function(path\\to\\file)           → "path\to\file"

#### 6.2.2. Quoted Arguments

The entire argument must be enclosed in `"..."` (double quotes) or `'...'` (single quotes).
Whitespace is preserved exactly as written. `,` (comma) and `)` (closing parenthesis)
inside quotes are treated as plain text.

**Examples:**

    .function("hello world")            → "hello world"
    .function(" hello world ")          → " hello world "
    .function('hello, world')           → "hello, world"
    .function("hello) world")           → "hello) world"
    .function("hello world", 'second')  → "hello world", "second"

#### 6.2.3. Escape Sequences

A `\` (backslash) followed by a character that is not in the allowed list is a syntax error.
This ensures forward compatibility — new escape sequences can be added later without
breaking existing templates.

**In unquoted arguments, allowed escapes:**

| Escape | Result                  |
|--------|-------------------------|
| `\\`   | literal `\` (backslash) |
| `\,`   | literal `,` (comma)     |
| `\)`   | literal `)` (closing parenthesis) |
| `\"`   | literal `"` (double quote) |
| `\'`   | literal `'` (single quote) |

**In double-quoted arguments (`"..."`), allowed escapes:**

| Escape | Result                  |
|--------|-------------------------|
| `\\`   | literal `\` (backslash) |
| `\"`   | literal `"` (double quote) |

Note: `'` (single quote) does not need escaping inside double quotes.

**In single-quoted arguments (`'...'`), allowed escapes:**

| Escape | Result                  |
|--------|-------------------------|
| `\\`   | literal `\` (backslash) |
| `\'`   | literal `'` (single quote) |

Note: `"` (double quote) does not need escaping inside single quotes.

**Invalid escapes:**

    .function(hello\a world)   → Error: invalid escape sequence '\a'
    .function("hello\n")       → Error: invalid escape sequence '\n'

#### 6.2.4. Invalid Argument Syntax

    .function("hello" world)   → Error: quotes must enclose the entire argument
    .function('hello' world)   → Error: quotes must enclose the entire argument

### 6.3. Chaining

Functions are called in sequence, left to right. The return type of each function
determines what functions are available next.

    ${variable.function1().function2().function3()}

**Examples:**

    ${filename.trim().toUpper()}   → "PHOTO.JPG"
    ${amount.abs().toString()}     → "1024"

### 6.4. Type Safety

Every value has a type determined at runtime. A function can only be called if it is
registered for the current type of the value. If not, a runtime error occurs.

**Examples:**

    ${filename.toUpper()}   → "PHOTO.JPG"
        (toUpper is registered for string)

    ${count.toUpper()}      → Error: function 'toUpper' is not registered for type 'integer'

### 6.5. Type-Changing Functions

Some functions change the type of the value. Subsequent functions in the chain
must be registered for the new type.

**Example:**

    ${count.toString().padLeft(5)}   → "  100"
        (count is integer → toString() returns string → padLeft is registered for string)

---

## 7. FormatType

FormatType declares what type the value is expected to be. It is not a conversion —
it is an assertion. The runtime value must already be compatible with the declared
FormatType. If it is not, a runtime error occurs.

FormatType determines which FormatStyles and CustomPattern handlers are available
for the value.

### 7.1. Syntax

    ${name, FormatType}
    ${name, FormatType, FormatStyle}
    ${name, FormatType : CustomPattern}

- FormatType follows identifier rules: starts with a letter (a-z, A-Z) or underscore,
  followed by letters, digits, or underscores.
- Whitespace is allowed before and after `,` (comma) and is ignored.
- FormatType is **case insensitive**.

### 7.2. Available FormatTypes

| FormatType   | Accepts        | Description                  |
|--------------|----------------|------------------------------|
| `number`     | integer, float/double | Numeric values          |
| `date`       | date           | Date values (date only)      |
| `datetime`   | datetime       | Date and time values         |
| `time`       | time           | Time values (time only)      |

If no FormatType is specified, no FormatStyles or CustomPatterns are available —
the value is converted directly to its default string representation.


### 7.3. Assertion, Not Conversion

FormatType does **not** convert the value. It asserts that the value is already
of a compatible type. If you need to convert a value, use Functions.

**Valid — value matches FormatType:**

    ${price, number}           → price is a double, number accepts double ✓
    ${count, number}           → count is an integer, number accepts integer ✓
    ${birthday, date}          → birthday is a date ✓
    ${created, datetime}       → created is a datetime ✓
    ${now, time}               → now is a time ✓

**Invalid — value does not match FormatType:**

    ${name, number}            → Error: 'name' is string, expected number
    ${count, date}             → Error: 'count' is integer, expected date
    ${birthday, datetime}      → Error: 'birthday' is date, expected datetime
    ${created, time}           → Error: 'created' is datetime, expected time

**Conversion via Functions instead:**

    ${count.toString()}        → converts integer to string via function
    ${text.toNumber()}         → converts string to number via function


### 7.4. FormatType Without FormatStyle or CustomPattern

FormatType can be used alone. This asserts the type but uses the default
string representation for that type.

    ${price, number}           → "1234.56" (default number-to-string)
    ${birthday, date}          → "2025-04-17" (default date-to-string)
    ${created, datetime}       → "2025-04-17 16:23:45" (default datetime-to-string)
    ${now, time}               → "16:23:45" (default time-to-string)


### 7.5. Implicit Type (No FormatType)

If no FormatType is specified, the value's runtime type (after functions) determines
the output. The value is converted to its default string representation.
No FormatStyles or CustomPatterns are available without a FormatType declaration.

    ${name}                    → "John" (string, default representation)
    ${count}                   → "1024" (integer, default representation)
    ${created}                 → "2025-04-17 16:23:45" (datetime, default representation)


---

## 8. FormatStyle

FormatStyle is a predefined named formatter registered for a specific FormatType.
It always produces a string as output.

### 8.1. Syntax

    ${name, FormatType, FormatStyle}

- FormatStyle follows identifier rules: starts with a letter (a-z, A-Z) or underscore,
  followed by letters, digits, or underscores.
- FormatStyle requires FormatType to be specified — FormatType determines which
  FormatStyles are available.
- FormatStyle and CustomPattern **cannot be used together**.
- Whitespace is allowed before and after `,` (comma) and is ignored.
- FormatStyle is **case insensitive**.

### 8.2. Available FormatStyles

FormatStyles are registered per FormatType. See the **MessageFormatter Function Reference**
document for the complete list.

#### `number` FormatStyles

| FormatStyle    | Description                                      | Example (value=1024) | Example (value=1234.5678) |
|----------------|--------------------------------------------------|----------------------|---------------------------|
| `integer`      | Whole number, rounded (half-even/banker's)       | `1024`               | `1235`                    |
| `currency`     | Currency format, 2 decimals (locale-dependent)   | `$1,024.00`          | `$1,234.57`               |
| `percent`      | Percentage format                                | `102400%`            | `123456.78%`              |
| `thousands`    | Thousands separator                              | `1,024`              | `1,234.57`                |
| `scientific`   | Scientific notation                              | `1.024E+03`          | `1.2346E+03`              |

Note: The `integer` FormatStyle uses **half-even rounding** (banker's rounding).
Example: `2.5` → `2`, `3.5` → `4`, `1234.5` → `1234`, `1235.5` → `1236`.

Note: The `currency` FormatStyle always displays exactly 2 decimal places.

#### `date` FormatStyles

| FormatStyle | Description              | Example (2025-04-17)      |
|-------------|--------------------------|---------------------------|
| `short`     | Short date (locale)      | `17.04.25` / `4/17/25`    |
| `medium`    | Medium date (locale)     | `17. apr. 2025`           |
| `long`      | Long date (locale)       | `17. april 2025`          |
| `full`      | Full date (locale)       | `torsdag 17. april 2025`  |
| `iso`       | ISO 8601                 | `2025-04-17`              |

#### `datetime` FormatStyles

| FormatStyle | Description                    | Example (2025-04-17 16:23:45)       |
|-------------|--------------------------------|-------------------------------------|
| `short`     | Short date + short time        | `17.04.25 16:23`                    |
| `medium`    | Medium date + medium time      | `17. apr. 2025 16:23:45`            |
| `long`      | Long date + long time          | `17. april 2025 16:23:45 CET`      |
| `full`      | Full date + full time          | `torsdag 17. april 2025 16:23:45 Central European Time` |
| `iso`       | ISO 8601                       | `2025-04-17T16:23:45`               |

#### `time` FormatStyles

| FormatStyle | Description              | Example (16:23:45)                     |
|-------------|--------------------------|----------------------------------------|
| `short`     | Hours and minutes        | `16:23` / `4:23 PM`                   |
| `medium`    | Hours, minutes, seconds  | `16:23:45`                             |
| `long`      | With timezone            | `16:23:45 CET`                         |
| `full`      | Full time (locale)       | `16:23:45 Central European Time`       |

### 8.3. Examples

    ${price, number, currency}         → "$1,234.57"
    ${count, number, integer}          → "1024"
    ${ratio, number, percent}          → "75%"
    ${fileSize, number, thousands}     → "1,048,576"
    ${birthday, date, short}           → "17.04.25"
    ${birthday, date, iso}             → "2025-04-17"
    ${created, datetime, short}        → "17.04.25 16:23"
    ${created, datetime, iso}          → "2025-04-17T16:23:45"
    ${now, time, short}                → "16:23"
    ${now, time, long}                 → "16:23:45 CET"

### 8.4. Error: FormatStyle Not Registered

If a FormatStyle is not registered for the given FormatType, a runtime error occurs.

    ${name, number, iso}       → Error: FormatStyle 'iso' is not registered for FormatType 'number'
    ${count, date, currency}   → Error: FormatStyle 'currency' is not registered for FormatType 'date'

### 8.5. Error: FormatType Required

FormatStyle cannot be used without FormatType — the parser would not know which
set of styles to look up.

    ${price, , currency}       → Error: FormatType is required when using FormatStyle



---

## 9. CustomPattern

CustomPattern is a user-defined formatting pattern. The FormatType determines which
CustomPattern handler processes the pattern and what tokens are valid.

### 9.1. Syntax

    ${name, FormatType : CustomPattern}

- The `:` (colon) separates the CustomPattern from the FormatType.
- FormatType is **required** — it determines which CustomPattern handler is used.
- CustomPattern and FormatStyle **cannot be used together**.
- Whitespace is allowed before and after `:` (colon) and is ignored.
- CustomPattern content is **case sensitive** (`MM` ≠ `mm`).
- CustomPattern starts after `:` (and optional whitespace) and ends at `}` (closing brace).
- Any character in the pattern that is not a recognized token is treated as **literal text**
  (e.g., `-`, `/`, `:`, `.`, spaces, words).
- Token parsing uses **longest match** (greedy) — `hhmm` is parsed as `hh` + `mm`,
  not `h` + `h` + `m` + `m`.

### 9.2. Available CustomPattern Tokens

Tokens are handled per FormatType. See the **MessageFormatter Function Reference**
document for the complete list.

#### `number` CustomPattern Tokens

| Token | Description               |
|-------|---------------------------|
| `#`   | Digit (omit if zero)      |
| `0`   | Digit (show if zero)      |
| `,`   | Thousands separator       |
| `.`   | Decimal separator         |
| `;`   | Positive;negative section |

**Examples:**

    ${price, number : #,##0.00}              → "1,234.57"
    ${price, number : 0.000}                 → "1234.568"
    ${price, number : #,##0.00;(#,##0.00)}   → "1,234.57" or "(1,234.57)"
    ${count, number : 00000}                 → "01024"
    ${price, number : 0.##}                  → "1234.57"
    ${price, number : Total: #,##0.00 kr.}   → "Total: 1,234.57 kr."

#### `date` CustomPattern Tokens

| Token  | Description                  | Example (2025-04-09) |
|--------|------------------------------|----------------------|
| `YYYY` | 4-digit year                 | `2025`               |
| `YY`   | 2-digit year                 | `25`                 |
| `MM`   | Month with leading zero      | `04`                 |
| `M`    | Month without leading zero   | `4`                  |
| `DD`   | Day with leading zero        | `09`                 |
| `D`    | Day without leading zero     | `9`                  |

**Examples:**

    ${created, date : YYYY-MM-DD}            → "2025-04-09"
    ${created, date : DD/MM/YYYY}            → "09/04/2025"
    ${created, date : D. MM YYYY}            → "9. 04 2025"
    ${created, date : YYYY.MM.DD}            → "2025.04.09"

#### `datetime` CustomPattern Tokens

Combines all tokens from `date` and `time`.

| Token       | Description                      | Example (2025-04-09 08:05:03.1234567) |
|-------------|----------------------------------|---------------------------------------|
| `YYYY`      | 4-digit year                     | `2025`                                |
| `YY`        | 2-digit year                     | `25`                                  |
| `MM`        | Month with leading zero          | `04`                                  |
| `M`         | Month without leading zero       | `4`                                   |
| `DD`        | Day with leading zero            | `09`                                  |
| `D`         | Day without leading zero         | `9`                                   |
| `hh`        | Hours 24h with leading zero      | `08`                                  |
| `h`         | Hours 24h without leading zero   | `8`                                   |
| `mm`        | Minutes with leading zero        | `05`                                  |
| `m`         | Minutes without leading zero     | `5`                                   |
| `ss`        | Seconds with leading zero        | `03`                                  |
| `s`         | Seconds without leading zero     | `3`                                   |
| `f`-`fffffff` | Fractional seconds (1-7 digits) | `1` to `1234567`                     |

**Examples:**

    ${created, datetime : YYYY-MM-DD hh:mm:ss}       → "2025-04-09 08:05:03"
    ${created, datetime : DD/MM/YYYY hh:mm}          → "09/04/2025 08:05"
    ${created, datetime : YYYY-MM-DD hh:mm:ss.fff}   → "2025-04-09 08:05:03.123"
    ${created, datetime : D. M YYYY h:mm}            → "9. 4 2025 8:05"

#### `time` CustomPattern Tokens

| Token       | Description                      | Example (08:05:03.1234567) |
|-------------|----------------------------------|----------------------------|
| `hh`        | Hours 24h with leading zero      | `08`                       |
| `h`         | Hours 24h without leading zero   | `8`                        |
| `mm`        | Minutes with leading zero        | `05`                       |
| `m`         | Minutes without leading zero     | `5`                        |
| `ss`        | Seconds with leading zero        | `03`                       |
| `s`         | Seconds without leading zero     | `3`                        |
| `f`         | Fractional seconds (1 digit)     | `1`                        |
| `ff`        | Fractional seconds (2 digits)    | `12`                       |
| `fff`       | Fractional seconds (3 digits)    | `123`                      |
| `ffff`      | Fractional seconds (4 digits)    | `1234`                     |
| `fffff`     | Fractional seconds (5 digits)    | `12345`                    |
| `ffffff`    | Fractional seconds (6 digits)    | `123456`                   |
| `fffffff`   | Fractional seconds (7 digits)    | `1234567`                  |

**Examples:**

    ${now, time : hh:mm:ss}                  → "08:05:03"
    ${now, time : h:mm}                      → "8:05"
    ${now, time : hh:mm:ss.fff}              → "08:05:03.123"
    ${now, time : hh:mm:ss.fffffff}          → "08:05:03.1234567"

### 9.3. Literal Text in Patterns

Any character that is not a recognized token is treated as literal text.
Separators like `-`, `/`, `:`, `.`, spaces, and words are output as-is.

**Examples:**

    ${created, date : YYYY-MM-DD}            → "2025-04-09"   (hyphens are literal)
    ${created, date : DD/MM/YYYY}            → "09/04/2025"   (slashes are literal)
    ${now, time : hh:mm:ss}                  → "08:05:03"     (colons are literal)
    ${created, date : YYYY年MM月DD日}         → "2025年04月09日"  (kanji are literal)
    ${price, number : Total: #,##0.00 kr.}   → "Total: 1,234.57 kr."

### 9.4. Error: FormatType Required

CustomPattern cannot be used without FormatType — the parser would not know which
handler to use for the pattern.

    ${created : YYYY-MM-DD}    → Error: FormatType is required when using CustomPattern

### 9.5. Error: Incompatible Pattern

If the CustomPattern contains tokens not supported by the FormatType's handler,
a runtime error occurs.

    ${count, number : YYYY-MM-DD}    → Error: token 'YYYY' is not supported for FormatType 'number'
    ${created, date : #,##0.00}      → Error: token '#' is not supported for FormatType 'date'
    ${created, date : hh:mm:ss}      → Error: token 'hh' is not supported for FormatType 'date'
    ${now, time : YYYY-MM-DD}        → Error: token 'YYYY' is not supported for FormatType 'time'


---

## 10. Eval Expressions

Eval expressions select a text segment based on the variable's value. They provide conditional
and selection logic within a message.

The eval separator is `§` (section sign, Unicode U+00A7) or `¶` (pilcrow sign, Unicode U+00B6, Alt+0182). Both are equivalent and interchangeable.

### 10.1. General Syntax

```
    ${name § EvalType, EvalPattern}
    ${name ¶ EvalType, EvalPattern}
    #{index § EvalType, EvalPattern}
    #{index ¶ EvalType, EvalPattern}
```

* Whitespace around `§` / `¶` is allowed and ignored.
* Functions can be used before the eval separator.

```
    ${name.function() § EvalType, EvalPattern}
```

### 10.2. EvalType: `if` (Conditional)

Selects between two strings based on a condition. Works like a ternary operator.

#### Syntax

```
    ${name § if, condition ? trueValue : falseValue}
```

* If `condition` is true, `trueValue` is output.
* If `condition` is false, `falseValue` is output.
* Both `trueValue` and `falseValue` can contain nested placeholders.

#### Conditions

| Condition   | Description                    | Example            |
|-------------|--------------------------------|--------------------|
| `eq` N      | Equal to N                     | `eq0`, `eq1`, `eq100` |
| `ne` N      | Not equal to N                 | `ne0`              |
| `gt` N      | Greater than N                 | `gt0`, `gt1000`    |
| `gte` N     | Greater than or equal to N     | `gte1`             |
| `lt` N      | Less than N                    | `lt10`             |
| `lte` N     | Less than or equal to N        | `lte100`           |
| `in(list)`  | Value is in list               | `in(1,2,3)`        |
| `nin(list)` | Value is not in list           | `nin(0,1)`         |

#### Examples

```
    ${count § if, eq0 ? no files : ${count} files}
    → "no files"  (when count=0)
    → "5 files"   (when count=5)

    ${size § if, gt1000 ? large : small}
    → "large"  (when size=1024)

    ${status § if, in(1,2,3) ? active : inactive}
    → "active"  (when status=2)

    ${count.abs() § if, eq0 ? nothing : ${count} items}
    → "nothing"  (when count=0)
```

### 10.3. EvalType: `plural` (Pluralization)

Selects a string based on a numeric value using count rules.

#### Syntax

```
    ${name § plural, countRule # value | countRule # value | ... [| other # defaultValue]}
```

* `countRule` determines when the associated `value` is selected.
* Rules are evaluated in order; first match wins.
* `other` is an optional fallback that matches if no other rule does. Must be last.
* `value` can contain nested placeholders.
* The separator between rules is `|` (pipe).
* The separator between countRule and value is `#`.

#### Count Rules

| Rule type        | Description                                       | Example                                 |
|------------------|---------------------------------------------------|-----------------------------------------|
| Exact number     | Matches specific value                            | `0`, `1`, `2`                           |
| CLDR category    | Language-specific plural category                 | `one`, `few`, `many`, `zero`            |
| Range (inclusive)| Matches values within range (inclusive bounds)    | `[0;10]`                                |
| Range (exclusive)| Matches values within range (exclusive bounds)    | `]0;10[`                                |
| Range (mixed)    | Inclusive start, exclusive end (or vice versa)    | `[0;10[`, `]0;10]`                      |
| `other`          | Default fallback                                  | `other`                                 |

#### Examples

```
    ${numFiles § plural, 0 # no files | 1 # one file | other # ${numFiles} files}
    → "no files"   (when numFiles=0)
    → "one file"   (when numFiles=1)
    → "5 files"    (when numFiles=5)

    ${age § plural, [0;12] # child | ]12;18] # teenager | other # adult}
    → "child"      (when age=10)
    → "teenager"   (when age=15)
    → "adult"      (when age=25)

    ${items § plural, 0 # no items | one # ${items} item | other # ${items} items}
    → "no items"   (when items=0)
    → "1 item"     (when items=1, language has CLDR 'one' for 1)
    → "5 items"    (when items=5)
```

### 10.4. EvalType: `select` (Category Selection)

Selects a string based on a categorical (string) match.

#### Syntax

```
    ${name § select, category # value | category # value | ... [| other # defaultValue]}
```

* `category` is matched directly against the variable's string value (case-sensitive).
* Rules are evaluated in order; first match wins.
* `other` is an optional fallback. Must be last.
* `value` can contain nested placeholders.

#### Examples

```
    ${gender § select, male # he | female # she | other # they}
    → "she"  (when gender="female")

    ${status § select, active # user is active | inactive # user is inactive | other # unknown status}
    → "user is active"  (when status="active")

    ${role § select, admin # Administrator: ${userName} | other # User: ${userName}}
    → "Administrator: John"  (when role="admin", userName="John")
```



---

## 11. Combinations

Functions can be combined with FormatType/FormatStyle, CustomPattern, or Eval expressions.

### 11.1. Functions + FormatStyle

```
    ${amount.abs(), float, currency}              → "$1,234.57"
```

### 11.2. Functions + CustomPattern

```
    ${created.addDays(1), datetime : YYYY-MM-DD}  → "2025-04-18"
```

### 11.3. Functions + Eval

```
    ${count.abs() § if, eq0 ? no items : ${count} items}
    ${name.trim().toLower() § select, admin # Admin | other # User}
```


---

## 12. Escape Rules

### 12.1. In CustomPattern and EvalPattern output strings

|Escape|Produces|Description|
|-|-|-|
|`}}`|`}`|Literal closing brace|
|`{{`|`{`|Literal opening brace|

Other characters (`:`, `,`, `/`, etc.) do not require escaping — they are handled by context-aware parsing.

**Example:**

```
    ${arg, datetime : YYYY}}MM}
    ${arg, datetime : {{filename}}}}
```


### 12.2. In Function Arguments

Everything between `(` and `)` is treated as function arguments. No escaping needed.

```
    ${arg.replace(},x)} → replaces "}" with "x"
```



### 12.3. In FormatType and FormatStyle

No escaping needed — these are predefined identifiers without special characters.

### 12.4. In Eval output strings (trueValue, falseValue, plural values, select values)

* Can contain nested placeholders (`${name}`, `#{index}`).
* Literal `{` and `}` are escaped with `{{` and `}}`.

**Example:**

```

    ${count § if, eq0 ? no {{files}} : has files} → "no {files}" (when count=0)
```



---

## 13. Whitespace Rules

|Location|Whitespace handling|
|-|-|
|Inside placeholder braces `${ name }`|Allowed, trimmed/ignored|
|Around eval separator `§` / `¶`|Allowed, ignored|
|Around commas (FormatType, FormatStyle)|Allowed, ignored|
|Around colon (CustomPattern)|Allowed, ignored|
|In function name|**Not allowed**|
|In function arguments|Preserved (passed to function)|
|In EvalPattern values (trueValue, etc.)|Preserved (part of output)|
|In CustomPattern content|Preserved (part of pattern)|

---

## 14. Error Handling

|Scenario|Error|
|-|-|
|Unbalanced `}` in placeholder|"Unbalanced '}' in expression"|
|Function not registered for type|"Function 'X' is not registered for type 'Y'"|
|Cannot convert to FormatType|"Cannot convert value to type 'X'"|
|FormatStyle not registered for FormatType|"FormatStyle 'X' is not registered for FormatType 'Y'"|
|CustomPattern not supported by type|"Pattern 'X' is not supported for type 'Y'"|
|FormatStyle and CustomPattern both specified|"FormatStyle and CustomPattern cannot be used together"|
|No matching condition in `if` (missing falseValue)|"No matching condition for value X"|
|No matching rule in `plural`/`select` without `other`|"No matching rule for value 'X' and no 'other' fallback defined"|
|Unknown EvalType|"Unknown eval type 'X'"|
|Invalid function arguments|"Invalid arguments for function 'X': Y"|

---

## 15. Complete Examples

### 15.1. Simple Placeholders

```
    ${filename} → "photo.jpg"
    #{0} → "1024"
```



### 15.2. Functions

```
    ${filename.toUpper()} → "PHOTO.JPG"
    ${filename.trim().toUpper()} → "PHOTO.JPG"
    ${count.toString().padLeft(5)} → "  100"
```



### 15.3. FormatType and FormatStyle

```
    ${size, number, currency}                      → "$1,024.00"
    ${created, date, short}                        → "17.04.25"
    ${created, date, iso}                          → "2025-04-17"
    ${created, datetime, short}                    → "17.04.25 16:23"
    ${created, datetime, iso}                      → "2025-04-17T16:23:45"
    ${price, number, currency}                     → "$19.99"
    ${now, time, short}                            → "16:23"
```



### 15.4. CustomPattern

```
    ${created, datetime : YYYY-MM-DD}              → "2025-04-17"
    ${created, date : DD/MM/YYYY}                  → "17/04/2025"
    ${created, datetime : YYYY-MM-DD hh:mm:ss}     → "2025-04-17 16:23:45"
    ${now, time : hh:mm}                           → "16:23"
    ${amount, number : #,##0.00}                   → "1,234.57"
    ${price, number : Total: #,##0.00 kr.}         → "Total: 1,234.57 kr."
```



### 15.5. Eval: if

```
    ${count § if, eq0 ? no files : ${count} files} → "no files" / "5 files"
    ${size § if, gt1000 ? large : small} → "large"
    ${count § if, in(1,2) ? selected : not selected} → "selected"
```



### 15.6. Eval: plural

```
    ${n § plural, 0 # no items | 1 # one item | other # ${n} items}
    ${age § plural, [0;12] # child | ]12;18] # teenager | other # adult}
```



### 15.7. Eval: select

```
    ${gender § select, male # he | female # she | other # they}
    ${role § select, admin # Administrator | other # User}
```



### 15.8. Combinations

```
    ${amount.abs(), number, currency}              → "$1,234.57"
    ${created.addDays(1), datetime : YYYY-MM-DD}   → "2025-04-18"
    ${count.abs() § if, eq0 ? nothing : ${count} items}
    ${name.trim() § select, admin # Admin: ${name} | other # User: ${name}}
```



### 15.9. Nested Placeholders

```
    ${created, datetime : YYYY-MM-DD ${filename}}  → "2025-04-17 photo.jpg"
    ${created, datetime : {{filename}}}}           → "{filename}"
```



### 15.10. Escape

```
    ${arg, datetime : YYYY}}MM}                    → "2025}04"
    ${arg, datetime : {{name}}}}                   → "{name}"
    ${count § if, eq0 ? no {{files}} : files} → "no {files}"
```


---

## 16. Syntax Summary (Quick Reference)

### Format Expression

```
    ${name[.func()]\* [, FormatType [, FormatStyle]] [: CustomPattern]}
    #{index[.func()]\* [, FormatType [, FormatStyle]] [: CustomPattern]}
```

```
    Rule: FormatStyle and CustomPattern are mutually exclusive.
    Rule: CustomPattern requires FormatType.
```





### Eval Expression

```
    ${name[.func()]* §|¶ EvalType, EvalPattern}
    #{index[.func()]* §|¶ EvalType, EvalPattern}
    ${name[.func()]* §|¶ EvalType, EvalPattern}
    #{index[.func()]* §|¶ EvalType, EvalPattern}
```

```
    EvalTypes: if, plural, select
    Eval separator: § (U+00A7) or ¶ (U+00B6, Alt+0182)
```





### Processing Flow

```
    Variable → Functions → FormatType Assertion → FormatStyle|CustomPattern → String Output
    Variable → Functions → EvalType + EvalPattern → String Output
```

