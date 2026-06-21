# Mangler / Issues

> Dato: 21 Jun 2026
> Scope: GlobMatcher, PathHelper, Core4 traversal/path flow

---

## 🔴 1. GlobMatcher — test failures (19 stk)

### 1A. Both-mode tests bruger `ForwardSlash` default

GlobMatcher default er `GlobSeparatorMode.ForwardSlash`. Flere tests kalder `Matches`/`IsIncluded` **uden mode-parameter** men forventer Both-mode opførsel (dvs. at `/` og `\` er ligeværdige separatorer).

**Fil:** `BMTP3.Common.Tests/Utilities/GlobMatcherTests.cs`

| Test | Linje | Problem |
|---|---|---|
| `BothMode_TreatsBothSlashesAsSeparators` | 305 | Navn siger "BothMode" men kalder `Matches(path, pattern)` uden mode. `@"sub\file.txt"` vs `"sub/*.txt"` → `\` er literal i ForwardSlash → `false`, test forventer `true`. |
| `ForwardAndBackslashPatterns_ProduceIdenticalMatch` | 278 | Kommentar: "Separator equivalence (Both mode)" — kalder `Matches` uden mode. Backslash-patterns som `@"**\*.jpg"` matcher ikke i ForwardSlash. |
| `ForwardAndBackslashPatterns_ProduceIdenticalIsIncluded` | 288 | Samme problem som ovenfor — `IsIncluded` uden mode. |

**Fix:** Tilføj `GlobSeparatorMode.Both` som sidste argument til alle `Matches`/`IsIncluded`-kald i disse tre tests.

---

### 1B. Invalid pattern tests forventer tolerant adfærd — implementation er fail-fast

`IsIncluded` bruger `matchOnError: null` → `MatchesIncludePatterns`/`MatchesAnyExcludePattern` **kaster `ArgumentException`** ved ugyldige patterns. Testene forventer en boolean returværdi.

**Fil:** `BMTP3.Common.Tests/Utilities/GlobMatcherTests.cs`

| Test | Linje | Input | Forventer | Faktisk |
|---|---|---|---|---|
| `IsIncluded_InvalidIncludePatternIsSkipped` | 229 | `include: ["[", "**/*.txt"]` | `true` | `ArgumentException` |
| `IsIncluded_InvalidExcludePatternRejectsPath` | 237 | `exclude: ["["]` | `false` | `ArgumentException` |
| `IsIncluded_OnlyInvalidIncludePatternsDoNotIncludePath` | 246 | `include: ["["]` | `false` | `ArgumentException` |

**Fix:** Ændr tests til `Assert.Throws<ArgumentException>()` — implementationens fail-fast er korrekt.

---

### 1C. Consoles test — "Both slashes by default" passer ikke med ForwardSlash default

**Fil:** `BMTP3.Consoles.Tests/GlobMatcherTests.cs:28` — `BothSlashesAreSeparatorsByDefault`

```csharp
string re = GlobMatcher.GlobToRegex("sub/*.txt");
Assert.True(Regex.IsMatch(@"sub\file.txt", re)); // ← false i ForwardSlash mode
```

Regex output: `^sub/[^/]*\.txt$` — kræver `/` separator, men test-input bruger `\`.

**Fix:** Tilføj `GlobSeparatorMode.Both` til kaldet, eller ret forventningen.

---

### 1D. xUnit2008 warnings i Consoles GlobMatcherTests

**Fil:** `BMTP3.Consoles.Tests/GlobMatcherTests.cs` — 7 steder

Bruger `Assert.True(Regex.IsMatch(...))` og `Assert.False(Regex.IsMatch(...))` i stedet for `Assert.Matches`/`Assert.DoesNotMatch`. Ikke blokerende, men bør rettes for at fjerne warnings.

---

## 🔴 2. PathHelper.GetRelativePath — case-sensitivity bug

**Fil:** `BMTP3.Core4/Helpers/PathHelper.cs:278`

```csharp
if(!sourcePath.StartsWith(rootDirectory, StringComparison.Ordinal))
```

XML-doc siger: *"Default path comparison for the active platform will be used (OrdinalIgnoreCase for Windows or Mac, Ordinal for Unix)"*

Men implementationen bruger **hardcoded `StringComparison.Ordinal`**.

### Scenario der fejler:

1. Bruger angiver `plan.SourcePath = "C:\\USERS\\JOHN\\PICTURES"` (upper case)
2. `ToCanonicalFileUri` → `Path.GetFullPath("C:\\USERS\\JOHN\\PICTURES")` **bevarer input casing** på Windows → `file:///C:/USERS/JOHN/PICTURES`
3. `FileSystemTraversal` → `file.FullName` = `C:\Users\John\Pictures` (actual FS casing) → `file:///C:/Users/John/Pictures`
4. `GetRelativePath("file:///C:/USERS/JOHN/PICTURES", "file:///C:/Users/John/Pictures/photo.jpg")`
5. `"file:///C:/Users/John/Pictures/photo.jpg".StartsWith("file:///C:/USERS/JOHN/PICTURES", Ordinal)` → **`false`**
6. `ArgumentException` kastes — backup bryder sammen

### Fix:

```csharp
StringComparison comparison = OperatingSystem.IsWindows()
    ? StringComparison.OrdinalIgnoreCase
    : StringComparison.Ordinal;
```

Brug `comparison` i stedet for `Ordinal` i `StartsWith`-kaldet.

---

## 🔶 3. Sidecar TargetRelativeFilePath — blander separator-stil

**Fil:** `BMTP3.Core4/Engine/BackupEngine.cs:483`

```csharp
TargetRelativeFilePath = Path.GetRelativePath(plan.Destination, targetPath),
```

`Path.GetRelativePath` returnerer OS-native separators (`\` på Windows). Sidecar-filen får:
- `SourceRelativeFilePath` = forward slashes (fra kanonisk URI)  
- `TargetRelativeFilePath` = backslashes (fra `Path.GetRelativePath`)

Ikke en runtime-fejl, men inkonsistent. Overvej at normalisere `TargetRelativeFilePath` til forward slashes.

---

## ✅ 4. Core4 — gennemgået og OK

| Komponent | Status | Noter |
|---|---|---|
| `GlobMatcher` implementation | ✅ Korrekt | ForwardSlash default, Both/Backslash modes, extglobs, brace expansion, negation, regex caching |
| `FileSystemTraversal` | ✅ Korrekt | Canonical URI flow, relative path derivation |
| `MediaDeviceTraversal` | ✅ Korrekt | Sub-path extraction, relative path opbygning |
| `BackupScanner` | ✅ Korrekt | Proxy mellem traversal og engine |
| `SourceConnector` | ✅ Korrekt | Dispatch til FS/MTP |
| `TempDirectoryHelper` | ✅ Korrekt | Temp dir, filnavn, cleanup |
| `BackupEngine.MatchDrive` | ✅ Korrekt | `OrdinalIgnoreCase` — platform-correct |
| `BackupEngine` main loop | ✅ Korrekt | Null guards, error handling, progress reporting |
| `PathHelper.ToInternalCanonicalUri` | ✅ Korrekt | FS → `file:///`, MTP → normalized |
| `PathHelper.FromCanonicalFileUri` | ✅ Korrekt | `file:///` → OS path |
| `PathHelper.FromCanonicalUriSubDrivePath` | ✅ Korrekt | Kun brugt af MTP traversal |
| `PathHelper.NormalizeSeparators` | ✅ Korrekt | Ren character replacement |

---

## Resume

| Prioritet | Antal | Hvad |
|---|---|---|
| 🔴 Skal fixes | 19 test + 1 bug | GlobMatcher tests (18 Common + 1 Consoles) + PathHelper case-sensitivity |
| 🟡 Kan fixes | 1 | Sidecar separator style (kosmetisk) |
| 🟢 Warnings | 7 | xUnit2008 i Consoles GlobMatcherTests |
