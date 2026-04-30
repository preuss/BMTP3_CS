# MessageFormatter Syntax v4

## 1. Introduction

This document defines the complete syntax for `MessageFormatter`.
It covers named and indexed placeholders, function calls, format expressions,
eval expressions (conditional logic), nested placeholders, and escape rules.

For available FormatTypes, FormatStyles, CustomPatterns, and Functions,
see the separate **MessageFormatter Function Reference** document.

---

## 2. Terminology

|Term|Description|
|-|-|
|**Placeholder**|An expression inside a message that gets replaced with a value at runtime|
|**Variable**|The runtime value referenced by a placeholder's name or index|
|**Function**|A registered operation that transforms a variable's value|
|**FormatType**|A type declaration that can cast/convert the value and determines available FormatStyles and CustomPatterns|
|**FormatStyle**|A predefined named formatter registered for a specific FormatType (produces string)|
|**CustomPattern**|A user-defined formatting pattern supported by the FormatType (produces string)|
|**EvalType**|A type of evaluation expression (`if`, `plural`, `select`)|
|**EvalPattern**|The pattern/rules used by the EvalType to select output|

---

## 3. Placeholder Syntax Overview

There are two main categories of placeholder expressions:

### 3.1. Format Expression

Used for value formatting (functions, type conversion, style/pattern output).

&#x20;   ${name\[.function()]\* \[, FormatType \[, FormatStyle]] \[: CustomPattern]}
#{index\[.function()]\* \[, FormatType \[, FormatStyle]] \[: CustomPattern]}



**Rule:** FormatStyle and CustomPattern are **mutually exclusive** — you cannot use both.

Valid combinations:

&#x20;   ${name}
${name.function()}
${name, FormatType}
${name, FormatType, FormatStyle}
${name : CustomPattern}
${name, FormatType : CustomPattern}
${name.function(), FormatType, FormatStyle}
${name.function() : CustomPattern}



### 3.2. Eval Expression

Used for conditional/selection logic based on the variable's value.

&#x20;   ${name\[.function()]\* § EvalType, EvalPattern}
#{index\[.function()]\* § EvalType, EvalPattern}



The eval separator is `§` (section sign) or `¶` (pilcrow sign, Alt+0182). Both are equivalent.

&#x20;   ${name § EvalType, EvalPattern}
${name ¶ EvalType, EvalPattern}



---

## 4. Processing Flow

When a placeholder is evaluated, the following steps occur in order:

&#x20;   Variable (arg)
→ \[Type Resolution] (determine runtime type)
→ \[Functions] (execute chained functions on current type; type may change)
→ \[FormatType cast/convert] (optional type conversion)
→ \[FormatStyle OR CustomPattern] (format to string — never both)
→ Output (string)



For Eval Expressions:

&#x20;   Variable (arg)
→ \[Type Resolution] (determine runtime type)
→ \[Functions] (execute chained functions on current type; type may change)
→ \[EvalType + EvalPattern] (evaluate condition, select output segment)
→ Output (string, may contain nested placeholders)



---

## 5. Placeholders

### 5.1. Named Placeholder

References a variable by name.

&#x20;   ${name}



* `name` is an identifier: starts with `a-z`, `A-Z`, or `\\\_`, followed by `a-z`, `A-Z`, `0-9`, or `\\\_`.
* Whitespace inside the braces is allowed and ignored: `${ name }` is equivalent to `${name}`.

**Example:**

&#x20;   ${filename} → "photo.jpg"
${userName} → "John"



### 5.2. Indexed Placeholder

References a variable by positional index.

&#x20;   #{index}



* `index` is a zero-based positive integer.
* Whitespace inside the braces is allowed and ignored: `#{ 0 }` is equivalent to `#{0}`.

**Example:**

&#x20;   #{0} → "photo.jpg" (if argument at index 0 is "photo.jpg")
#{1} → "1024" (if argument at index 1 is "1024")



### 5.3. Nested Placeholders

Within a CustomPattern or EvalPattern output strings, placeholders can be nested
to reference other variables.

**Example:**

&#x20;   ${date : yyyy-MM-dd ${filename}} → "2025-04-17 photo.jpg"



---

## 6. Functions

Functions are registered operations that can be called on a variable's current type.
Multiple functions can be chained. Each function executes on the result of the previous one.

### 6.1. Syntax

&#x20;   .functionName()
.functionName(arg1)
.functionName(arg1, arg2)



* Function names follow identifier rules.
* Arguments inside parentheses are passed to the function.
* Arguments are separated by commas.
* Functions execute **before** FormatType conversion and **before** FormatStyle/CustomPattern/EvalType.

### 6.2. Chaining

Functions are called in sequence, left to right. The return type of each function
determines what functions are available next.

&#x20;   ${name.function1().function2().function3()}



**Example:**

&#x20;   ${filename.trim().toUpper()} → "PHOTO.JPG"
${amount.abs().toString()} → "1024"



### 6.3. Type Safety

A function can only be called if it is registered for the current type of the value.
If a function is not available for the current type, a runtime error occurs.

**Example:**

&#x20;   ${filename.toUpper()} → "PHOTO.JPG" (toUpper is registered for string)
${count.toUpper()} → Error: "Function 'toUpper' is not registered for type 'integer'"



### 6.4. Type-Changing Functions

Some functions change the type of the value. Subsequent functions must be
registered for the new type.

**Example:**

&#x20;   ${count.toString().padLeft(5)} → "  100"
(count is integer → toString() returns string → padLeft is registered for string)



---

## 7. FormatType

FormatType declares what type the value should be treated as after functions have executed.
If the current value is not already of this type, a cast or conversion is attempted.

### 7.1. Syntax

&#x20;   ${name, FormatType}



* FormatType is an identifier (e.g., `integer`, `float`, `date`, `time`, `datetime`, `bool`, `string`).
* FormatType determines which FormatStyles and CustomPattern handlers are available.
* If the value cannot be converted to the specified FormatType, a runtime error occurs.

**Example:**

&#x20;   ${size, integer} → "1024"
${price, float} → "19.99"
${created, date} → "2025-04-17"



### 7.2. Implicit Type

If no FormatType is specified, the runtime type of the value (after functions) is used
to determine available CustomPattern handlers.

**Example:**

&#x20;   ${date : yyyy-MM-dd} → "2025-04-17"
(variable 'date' is already DateTime at runtime, so date CustomPattern handler is used)



---

## 8. FormatStyle

FormatStyle is a predefined named formatter registered for a specific FormatType.
It always produces a string as output.

### 8.1. Syntax

&#x20;   ${name, FormatType, FormatStyle}



* FormatStyle is an identifier.
* FormatStyle requires FormatType to be specified (it defines which styles are available).
* FormatStyle and CustomPattern **cannot be used together**.

**Example:**

&#x20;   ${size, integer, currency} → "$1,024"
${created, date, short} → "17.04.25"
${created, date, iso} → "2025-04-17"
${active, bool, yesno} → "yes"



---

## 9. CustomPattern

CustomPattern is a user-defined formatting pattern. The FormatType (explicit or implicit)
determines which CustomPattern handler processes the pattern and what tokens are valid.

### 9.1. Syntax

&#x20;   ${name : CustomPattern}
${name, FormatType : CustomPattern}



* The colon `:` separates the CustomPattern from the rest of the expression.
* FormatType is optional — if omitted, the runtime type determines the handler.
* CustomPattern and FormatStyle **cannot be used together**.
* CustomPattern content depends on the FormatType. See the Type Registry for available patterns.

**Example:**

&#x20;   ${created : yyyy-MM-dd} → "2025-04-17"
${created, date : dd/MM/yyyy} → "17/04/2025"
${now, time : HH:mm} → "16:23"
${amount, float : #,##0.00} → "1,234.57"



### 9.2. Error on Incompatible Pattern

If the CustomPattern is not supported by the FormatType, a runtime error occurs.

**Example:**

&#x20;   ${count, integer : yyyy-MM-dd} → Error: "Pattern 'yyyy-MM-dd' is not supported for type 'integer'"



---

## 10. Eval Expressions

Eval expressions select a text segment based on the variable's value.
They provide conditional and selection logic within a message.

The eval separator is `§` (section sign, Unicode U+00A7) or `¶` (pilcrow sign, Unicode U+00B6, Alt+0182).
Both are equivalent and interchangeable.

### 10.1. General Syntax

&#x20;   ${name § EvalType, EvalPattern}
${name ¶ EvalType, EvalPattern}
#{index § EvalType, EvalPattern}



* Whitespace around `§` / `¶` is allowed and ignored.
* Functions can be used before the eval separator.

  ${name.function() § EvalType, EvalPattern}

  ### 10.2. EvalType: `if` (Conditional)

  Selects between two strings based on a condition. Works like a ternary operator.

  #### Syntax

  &#x20;   ${name § if, condition ? trueValue : falseValue}



* If `condition` is true, `trueValue` is output.
* If `condition` is false, `falseValue` is output.
* Both `trueValue` and `falseValue` can contain nested placeholders.

  #### Conditions

|Condition|Description|Example|
|-|-|-|
|`eq` N|Equal to N|`eq0`, `eq1`, `eq100`|
|`ne` N|Not equal to N|`ne0`|
|`gt` N|Greater than N|`gt0`, `gt1000`|
|`gte` N|Greater than or equal to N|`gte1`|
|`lt` N|Less than N|`lt10`|
|`lte` N|Less than or equal to N|`lte100`|
|`in(list)`|Value is in list|`in(1,2,3)`|
|`nin(list)`|Value is not in list|`nin(0,1)`|

#### Examples

&#x20;   ${count § if, eq0 ? no files : ${count} files}
→ "no files" (when count=0)
→ "5 files" (when count=5)

&#x20;   ${size § if, gt1000 ? large : small}
    → "large" (when size=1024)

    ${status § if, in(1,2,3) ? active : inactive}
    → "active" (when status=2)

    ${count.abs() § if, eq0 ? nothing : ${count} items}
    → "nothing" (when count=0)





### 10.3. EvalType: `plural` (Pluralization)

Selects a string based on a numeric value using count rules.

#### Syntax

&#x20;   ${name § plural, countRule # value | countRule # value | ... \[| other # defaultValue]}



* `countRule` determines when the associated `value` is selected.
* Rules are evaluated in order; first match wins.
* `other` is an optional fallback that matches if no other rule does. Must be last.
* `value` can contain nested placeholders.
* The separator between rules is `|` (pipe).
* The separator between countRule and value is `#`.

#### Count Rules

|Rule type|Description|Example|
|-|-|-|
|Exact number|Matches specific value|`0`, `1`, `2`|
|CLDR category|Language-specific plural category|`one`, `few`, `many`, `zero`|
|Range (inclusive)|Matches values within range (inclusive bounds)|`\\\[0;10]`|
|Range (exclusive)|Matches values within range (exclusive bounds)|`]0;10\\\[`|
|Range (mixed)|Inclusive start, exclusive end (or vice versa)|`\\\[0;10\\\[`, `]0;10]`|
|`other`|Default fallback|`other`|

#### Examples

&#x20;   ${numFiles § plural, 0 # no files | 1 # one file | other # ${numFiles} files}
→ "no files" (when numFiles=0)
→ "one file" (when numFiles=1)
→ "5 files" (when numFiles=5)

&#x20;   ${age § plural, \\\[0;12] # child | ]12;18] # teenager | other # adult}
    → "child" (when age=10)
    → "teenager" (when age=15)
    → "adult" (when age=25)

    ${items § plural, 0 # no items | one # ${items} item | other # ${items} items}
    → "no items" (when items=0)
    → "1 item" (when items=1, language has CLDR 'one' for 1)
    → "5 items" (when items=5)





### 10.4. EvalType: `select` (Category Selection)

Selects a string based on a categorical (string) match.

#### Syntax

&#x20;   ${name § select, category # value | category # value | ... \[| other # defaultValue]}



* `category` is matched directly against the variable's string value (case-sensitive).
* Rules are evaluated in order; first match wins.
* `other` is an optional fallback. Must be last.
* `value` can contain nested placeholders.

#### Examples

&#x20;   ${gender § select, male # he | female # she | other # they}
→ "she" (when gender="female")

&#x20;   ${status § select, active # user is active | inactive # user is inactive | other # unknown status}
    → "user is active" (when status="active")

    ${role § select, admin # Administrator: ${userName} | other # User: ${userName}}
    → "Administrator: John" (when role="admin", userName="John")





---

## 11. Combinations

Functions can be combined with FormatType/FormatStyle, CustomPattern, or Eval expressions.

### 11.1. Functions + FormatStyle

&#x20;   ${amount.abs(), float, currency} → "$1,234.57"



### 11.2. Functions + CustomPattern

&#x20;   ${created.addDays(1) : yyyy-MM-dd} → "2025-04-18"



### 11.3. Functions + Eval

&#x20;   ${count.abs() § if, eq0 ? no items : ${count} items}
${name.trim().toLower() § select, admin # Admin | other # User}



---

## 12. Escape Rules

### 12.1. In CustomPattern and EvalPattern output strings

|Escape|Produces|Description|
|-|-|-|
|`}}`|`}`|Literal closing brace|
|`{{`|`{`|Literal opening brace|

Other characters (`:`, `,`, `/`, etc.) do not require escaping — they are handled by context-aware parsing.

**Example:**

&#x20;   ${arg : yyyy}}MM} → "2025}04"
${arg : {{filename}}}} → "{filename}"



### 12.2. In Function Arguments

Everything between `(` and `)` is treated as function arguments. No escaping needed.

&#x20;   ${arg.replace(},x)} → replaces "}" with "x"



### 12.3. In FormatType and FormatStyle

No escaping needed — these are predefined identifiers without special characters.

### 12.4. In Eval output strings (trueValue, falseValue, plural values, select values)

* Can contain nested placeholders (`${name}`, `#{index}`).
* Literal `{` and `}` are escaped with `{{` and `}}`.

**Example:**

&#x20;   ${count § if, eq0 ? no {{files}} : has files} → "no {files}" (when count=0)



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

&#x20;   ${filename} → "photo.jpg"
#{0} → "1024"



### 15.2. Functions

&#x20;   ${filename.toUpper()} → "PHOTO.JPG"
${filename.trim().toUpper()} → "PHOTO.JPG"
${count.toString().padLeft(5)} → "  100"



### 15.3. FormatType and FormatStyle

&#x20;   ${size, integer, currency} → "$1,024"
${created, date, short} → "17.04.25"
${created, date, iso} → "2025-04-17"
${price, float, currency} → "$19.99"
${active, bool, yesno} → "yes"



### 15.4. CustomPattern

&#x20;   ${created : yyyy-MM-dd} → "2025-04-17"
${created, date : dd/MM/yyyy} → "17/04/2025"
${created : yyyy,MM/dd:EEE} → "2025,04/17:Thu"
${now, time : HH:mm} → "16:23"
${amount, float : #,##0.00} → "1,234.57"



### 15.5. Eval: if

&#x20;   ${count § if, eq0 ? no files : ${count} files} → "no files" / "5 files"
${size § if, gt1000 ? large : small} → "large"
${count § if, in(1,2) ? selected : not selected} → "selected"



### 15.6. Eval: plural

&#x20;   ${n § plural, 0 # no items | 1 # one item | other # ${n} items}
${age § plural, \[0;12] # child | ]12;18] # teenager | other # adult}



### 15.7. Eval: select

&#x20;   ${gender § select, male # he | female # she | other # they}
${role § select, admin # Administrator | other # User}



### 15.8. Combinations

&#x20;   ${amount.abs(), float, currency} → "$1,234.57"
${created.addDays(1) : yyyy-MM-dd} → "2025-04-18"
${count.abs() § if, eq0 ? nothing : ${count} items}
${name.trim() § select, admin # Admin: ${name} | other # User: ${name}}



### 15.9. Nested Placeholders

&#x20;   ${created : yyyy-MM-dd ${filename}} → "2025-04-17 photo.jpg"
${created : {{filename}}}} → "{filename}"



### 15.10. Escape

&#x20;   ${arg : yyyy}}MM} → "2025}04"
${arg : {{name}}}} → "{name}"
${count § if, eq0 ? no {{files}} : files} → "no {files}"



---

## 16. Syntax Summary (Quick Reference)

### Format Expression

&#x20;   ${name\[.func()]\* \[, FormatType \[, FormatStyle]] \[: CustomPattern]}
#{index\[.func()]\* \[, FormatType \[, FormatStyle]] \[: CustomPattern]}

&#x20;   Rule: FormatStyle and CustomPattern are mutually exclusive.





### Eval Expression

&#x20;   ${name\[.func()]\* §|¶ EvalType, EvalPattern}
#{index\[.func()]\* §|¶ EvalType, EvalPattern}

&#x20;   EvalTypes: if, plural, select
    Eval separator: § (U+00A7) or ¶ (U+00B6, Alt+0182)





### Processing Flow

&#x20;   Variable → Functions → FormatType → FormatStyle|CustomPattern → String Output
Variable → Functions → EvalType + EvalPattern → String Output

