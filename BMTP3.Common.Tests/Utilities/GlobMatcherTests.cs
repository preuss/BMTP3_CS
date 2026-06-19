using BMTP3.Common.Utilities;

namespace BMTP3.Common.Tests.Utilities;

/// <summary>
///     Unit tests for <see cref="GlobMatcher" />.
///     Each test category corresponds to a documented glob feature.
/// </summary>
public class GlobMatcherTests
{
    // -----------------------------------------------------------------------
    // GlobToRegex / Matches — basic wildcard patterns
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("file.txt", "*.txt", true)]
    [InlineData("file.jpg", "*.txt", false)]
    [InlineData("sub/file.txt", "*.txt", false)] // * must not cross separator
    public void Star_MatchesFilenameOnly(string path, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
    }

    [Theory]
    [InlineData("a/b/c.txt", "**/*.txt", true)]
    [InlineData("file.txt", "**/*.txt", true)] // root-level match
    [InlineData("a/b/c.jpg", "**/*.txt", false)]
    public void DoubleStar_MatchesAcrossDirectories(string path, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
    }

    [Theory]
    [InlineData("sub/file.txt", "sub/*.txt", true)]
    [InlineData("other/file.txt", "sub/*.txt", false)]
    public void GlobsMatchWithForwardSlash(string path, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
    }

    // -----------------------------------------------------------------------
    // Extglob operators
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("foo.txt", "@(foo|bar).txt", true)]
    [InlineData("bar.txt", "@(foo|bar).txt", true)]
    [InlineData("baz.txt", "@(foo|bar).txt", false)]
    public void ExtglobAlternation(string path, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
    }

    [Theory]
    [InlineData("file.png", "*.!(jpg)", true)]
    [InlineData("file.jpg", "*.!(jpg)", false)]
    public void ExtglobSuffixNegation(string path, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
    }

    [Theory]
    [InlineData("bar", "!(foo)", true)]
    [InlineData("foo", "!(foo)", false)]
    public void GlobalNegation(string path, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
    }

    // -----------------------------------------------------------------------
    // IsIncluded — semantic rules
    // -----------------------------------------------------------------------

    [Fact]
    public void IsIncluded_NoPatternsIncludesEverything()
    {
        Assert.True(GlobMatcher.IsIncluded("any/path/file.txt", null, null));
        Assert.True(GlobMatcher.IsIncluded("any/path/file.txt", new List<string>(), new List<string>()));
    }

    [Fact]
    public void IsIncluded_ExcludePatternTakesPrecedence()
    {
        List<string> include = new() { "**/*.txt" };
        List<string> exclude = new() { "**/*.txt" };

        // Exclude wins even when include also matches.
        Assert.False(GlobMatcher.IsIncluded("notes/todo.txt", include, exclude));
    }

    [Fact]
    public void IsIncluded_IncludePatternsFilterUnmatched()
    {
        List<string> include = new() { "**/*.jpg" };

        Assert.True(GlobMatcher.IsIncluded("photos/img.jpg", include, null));
        Assert.False(GlobMatcher.IsIncluded("docs/readme.txt", include, null));
    }

    [Fact]
    public void IsIncluded_ExcludeOnlyFiltersMatched()
    {
        List<string> exclude = new() { "**/*.tmp" };

        Assert.False(GlobMatcher.IsIncluded("work/scratch.tmp", null, exclude));
        Assert.True(GlobMatcher.IsIncluded("work/document.pdf", null, exclude));
    }

    [Fact]
    public void IsIncluded_MultipleExcludePatterns()
    {
        List<string> exclude = new() { "**/*.tmp", "**/.git/**", "**/*.log" };

        Assert.False(GlobMatcher.IsIncluded("repo/.git/config", null, exclude));
        Assert.False(GlobMatcher.IsIncluded("app/debug.log", null, exclude));
        Assert.True(GlobMatcher.IsIncluded("app/main.cs", null, exclude));
    }

    [Fact]
    public void IsIncluded_BlankPatternsAreIgnored()
    {
        List<string> include = new() { "", "  ", "**/*.jpg" };

        Assert.True(GlobMatcher.IsIncluded("photo.jpg", include, null));

        List<string> blanksOnly = new() { "", "   " };

        // All patterns are blank, so there are no effective include patterns.
        // That means include filtering is disabled and everything is included.
        Assert.True(GlobMatcher.IsIncluded("photo.txt", blanksOnly, null));
    }

    // -----------------------------------------------------------------------
    // Brace expansion: {a,b}
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("file.jpg", "*.{jpg,png}", true)]
    [InlineData("file.png", "*.{jpg,png}", true)]
    [InlineData("file.gif", "*.{jpg,png}", false)]
    public void BraceExpansion_SimpleAlternatives(string path, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
    }

    [Theory]
    [InlineData("foo.txt", "{foo,bar}.txt", true)]
    [InlineData("bar.txt", "{foo,bar}.txt", true)]
    [InlineData("baz.txt", "{foo,bar}.txt", false)]
    public void BraceExpansion_Prefix(string path, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
    }

    [Theory]
    [InlineData("src/a/file.cs", "src/{a,b}/file.cs", true)]
    [InlineData("src/b/file.cs", "src/{a,b}/file.cs", true)]
    [InlineData("src/c/file.cs", "src/{a,b}/file.cs", false)]
    public void BraceExpansion_DirectorySegment(string path, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
    }

    [Fact]
    public void BraceExpansion_WithStar()
    {
        Assert.True(GlobMatcher.Matches("data.txt", "*.{csv,txt}"));
        Assert.True(GlobMatcher.Matches("data.csv", "*.{csv,txt}"));
        Assert.False(GlobMatcher.Matches("data.pdf", "*.{csv,txt}"));
    }

    [Theory]
    [InlineData("3", "{1,2,3}", true)]
    [InlineData("1", "{1,2,3}", true)]
    [InlineData("4", "{1,2,3}", false)]
    public void BraceExpansion_ThreeAlternatives(string path, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
    }

    [Theory]
    [InlineData("photo{img}.jpg", "photo{img}.jpg", true)] // no comma → literal braces
    [InlineData("file.jpg", "*.{jpg}", false)] // no comma → {jpg} is literal text
    public void BraceExpansion_SingleAlternativeNoComma(string path, string pattern, bool expected)
    {
        // Without a comma the braces are treated as literal characters.
        Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
    }

    // -----------------------------------------------------------------------
    // Windows absolute paths in default Both mode
    // -----------------------------------------------------------------------

    [Fact]
    public void IsIncluded_WindowsAbsolutePath_DefaultBothMode_TreatsBackslashAsSeparator()
    {
        List<string> include = new() { @"**\*.jpg" };

        Assert.True(GlobMatcher.IsIncluded(@"C:\Photos\2024\img.jpg", include, null));
        Assert.False(GlobMatcher.IsIncluded(@"C:\Docs\readme.txt", include, null));
    }

    // -----------------------------------------------------------------------
    // Backslash in patterns is literal when ForwardSlash mode is used
    // -----------------------------------------------------------------------

    [Fact]
    public void BackslashInPattern_IsLiteral_NotSeparator()
    {
        // In ForwardSlash mode, '\' is a literal character, not a path separator.
        // Callers that require GlobMatcher-format patterns should normalize '\' to '/' before matching.
        var mode = GlobSeparatorMode.ForwardSlash;

        Assert.False(GlobMatcher.Matches(@"sub\file.txt", "sub/*.txt", mode));
        Assert.False(GlobMatcher.Matches("sub/file.txt", @"sub\*.txt", mode));
    }

    [Theory]
    [InlineData("a.txt", "?.txt", true)]
    [InlineData("ab.txt", "?.txt", false)]
    [InlineData(".txt", "?.txt", false)]
    [InlineData("a/b.txt", "?/b.txt", true)]
    [InlineData("ab/b.txt", "?/b.txt", false)]
    public void QuestionMark_MatchesExactlyOneNonSeparatorCharacter(string path, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
    }

    [Fact]
    public void IsIncluded_InvalidIncludePatternIsSkipped()
    {
        List<string> include = new() { "[", "**/*.txt" };

        Assert.True(GlobMatcher.IsIncluded("docs/readme.txt", include, null));
    }

    [Fact]
    public void IsIncluded_InvalidExcludePatternRejectsPath()
    {
        List<string> exclude = new() { "[" };

        Assert.False(GlobMatcher.IsIncluded("docs/readme.txt", null, exclude));
    }

    [Fact]
    public void IsIncluded_OnlyInvalidIncludePatternsDoNotIncludePath()
    {
        List<string> include = new() { "[" };

        Assert.False(GlobMatcher.IsIncluded("docs/readme.txt", include, null));
    }

    // -----------------------------------------------------------------------
    // Separator equivalence (Both mode): / and \ produce identical match results.
    // This was a deliberate design choice — not standard POSIX glob.
    // GlobSeparatorMode allows narrowing to ForwardSlash or Backslash.
    // -----------------------------------------------------------------------

    public static IEnumerable<object[]> ForwardBackslashGlobData()
    {
        // (path, patternWithForwardSlash, patternWithBackslash)
        // Each pair must produce identical match results.

        yield return Wrap("a/b/c.jpg", "**/*.jpg", @"**\*.jpg");
        yield return Wrap(@"a\b\c.jpg", "**/*.jpg", @"**\*.jpg");
        yield return Wrap("file.jpg", "**/*.jpg", @"**\*.jpg");
        yield return Wrap("DCIM/IMG001.jpg", "DCIM/*.jpg", @"DCIM\*.jpg");
        yield return Wrap(@"DCIM\IMG001.jpg", "DCIM/*.jpg", @"DCIM\*.jpg");
        yield return Wrap("DCIM/2024/IMG001.jpg", "DCIM/**/*.jpg", @"DCIM\**\*.jpg");
        yield return Wrap(@"DCIM\2024\IMG001.jpg", "DCIM/**/*.jpg", @"DCIM\**\*.jpg");
        yield return Wrap("photo.jpg", "*.jpg", "*.jpg");
        yield return Wrap("sub/photo.jpg", "*.jpg", "*.jpg");
        yield return Wrap("a/b/c/d/e/file.txt", "a/**/*.txt", @"a\**\*.txt");

        static object[] Wrap(string path, string fs, string bs) => new object[] { path, fs, bs };
    }

    [Theory]
    [MemberData(nameof(ForwardBackslashGlobData))]
    public void ForwardAndBackslashPatterns_ProduceIdenticalMatch(string path, string forwardSlashPattern, string backslashPattern)
    {
        bool forwardResult = GlobMatcher.Matches(path, forwardSlashPattern);
        bool backslashResult = GlobMatcher.Matches(path, backslashPattern);

        Assert.Equal(forwardResult, backslashResult);
    }

    [Theory]
    [MemberData(nameof(ForwardBackslashGlobData))]
    public void ForwardAndBackslashPatterns_ProduceIdenticalIsIncluded(string path, string forwardSlashPattern, string backslashPattern)
    {
        bool forwardResult = GlobMatcher.IsIncluded(path, new[] { forwardSlashPattern }, null);
        bool backslashResult = GlobMatcher.IsIncluded(path, new[] { backslashPattern }, null);

        Assert.Equal(forwardResult, backslashResult);
    }

    // -----------------------------------------------------------------------
    // GlobSeparatorMode controls separator behavior:
    //   Both         — '/' and '\' are separators
    //   ForwardSlash — only '/' is a separator
    //   Backslash    — only '\' is a separator
    // -----------------------------------------------------------------------

    [Fact]
    public void BothMode_TreatsBothSlashesAsSeparators()
    {
        Assert.True(GlobMatcher.Matches(@"sub\file.txt", "sub/*.txt"));
        Assert.True(GlobMatcher.Matches("sub/file.txt", "sub/*.txt"));
        Assert.True(GlobMatcher.Matches(@"sub\file.txt", @"sub\*.txt"));
        Assert.True(GlobMatcher.Matches("sub/file.txt", @"sub\*.txt"));
    }

    [Fact]
    public void ForwardSlashMode_TreatsOnlyForwardSlashAsSeparator()
    {
        var mode = GlobSeparatorMode.ForwardSlash;

        Assert.True(GlobMatcher.Matches("sub/file.txt", "sub/*.txt", mode));
        Assert.False(GlobMatcher.Matches(@"sub\file.txt", "sub/*.txt", mode));
        Assert.True(GlobMatcher.Matches(@"sub\file.txt", @"sub\*.txt", mode));
        Assert.False(GlobMatcher.Matches("sub/file.txt", @"sub\*.txt", mode));
    }

    [Fact]
    public void BackslashMode_TreatsOnlyBackslashAsSeparator()
    {
        var mode = GlobSeparatorMode.Backslash;

        Assert.True(GlobMatcher.Matches(@"sub\file.txt", @"sub\*.txt", mode));
        Assert.False(GlobMatcher.Matches("sub/file.txt", @"sub\*.txt", mode));
        Assert.True(GlobMatcher.Matches("sub/file.txt", "sub/*.txt", mode));
        Assert.False(GlobMatcher.Matches(@"sub\file.txt", "sub/*.txt", mode));
    }

    // -----------------------------------------------------------------------
    // Systematic mode × separator matrix — every mode with every path/pattern
    // separator combination for * and ** patterns.
    // -----------------------------------------------------------------------

    public static IEnumerable<object[]> SeparatorModeMatrixData()
    {
        // (mode, path, pattern, expected)
        // Each row tests a specific mode/separator combination.

        var both = GlobSeparatorMode.Both;
        var fs   = GlobSeparatorMode.ForwardSlash;
        var bs   = GlobSeparatorMode.Backslash;

        // ── Star pattern: sub/*.txt ──────────────────────────────────────────
        // Both mode: both separators work either way.
        yield return Mk(both, "sub/file.txt",  "sub/*.txt", true);
        yield return Mk(both, @"sub\file.txt", "sub/*.txt", true);
        yield return Mk(both, "sub/file.txt",  @"sub\*.txt", true);
        yield return Mk(both, @"sub\file.txt", @"sub\*.txt", true);

        // ForwardSlash mode: only / is a separator.
        yield return Mk(fs,   "sub/file.txt",  "sub/*.txt", true);   // / matches /
        yield return Mk(fs,   @"sub\file.txt", "sub/*.txt", false);  // \ is literal, not a separator
        yield return Mk(fs,   "sub/file.txt",  @"sub\*.txt", false); // pattern \ is literal, not a separator
        yield return Mk(fs,   @"sub\file.txt", @"sub\*.txt", true);  // both \ are literal → match

        // Backslash mode: only \ is a separator.
        yield return Mk(bs,   "sub/file.txt",  "sub/*.txt", true);   // both / are literal → match
        yield return Mk(bs,   @"sub\file.txt", "sub/*.txt", false);  // path separator \, pattern uses /
        yield return Mk(bs,   "sub/file.txt",  @"sub\*.txt", false); // path uses /, pattern separator \
        yield return Mk(bs,   @"sub\file.txt", @"sub\*.txt", true);  // \ matches \

        // ── Double-star pattern: **/*.jpg vs **\*.jpg ────────────────────────
        // Both mode: both separators work for **/ and **\.
        yield return Mk(both, "a/b/c.jpg",     "**/*.jpg", true);
        yield return Mk(both, @"a\b\c.jpg",    "**/*.jpg", true);
        yield return Mk(both, "a/b/c.jpg",     @"**\*.jpg", true);
        yield return Mk(both, @"a\b\c.jpg",    @"**\*.jpg", true);

        // ForwardSlash mode: **/ is recursive with / separator,
        // **\ is literal because backslash is not a separator in this mode.
        // The recursive prefix is optional, so paths with no forward slash can still
        // match the filename part as a root-level match.
        yield return Mk(fs,   "a/b/c.jpg",     "**/*.jpg", true);
        yield return Mk(fs,   @"a\b\c.jpg",    "**/*.jpg", true);
        yield return Mk(fs,   "a/b/c.jpg",     @"**\*.jpg", false);
        yield return Mk(fs,   @"a\b\c.jpg",    @"**\*.jpg", true);

        // Backslash mode: **\ is recursive with \ separator,
        // **/ is literal because forward slash is not a separator in this mode.
        yield return Mk(bs,   "a/b/c.jpg",     "**/*.jpg", true);
        yield return Mk(bs,   @"a\b\c.jpg",    "**/*.jpg", false);
        yield return Mk(bs,   "a/b/c.jpg",     @"**\*.jpg", true);
        yield return Mk(bs,   @"a\b\c.jpg",    @"**\*.jpg", true);

        // ── Negative: wrong extension in every mode ─────────────────────────
        yield return Mk(both, "file.txt", "*.jpg", false);
        yield return Mk(fs,   "file.txt", "*.jpg", false);
        yield return Mk(bs,   "file.txt", "*.jpg", false);

        static object[] Mk(GlobSeparatorMode mode, string path, string pattern, bool expected)
            => new object[] { mode, path, pattern, expected };
    }

    [Theory]
    [MemberData(nameof(SeparatorModeMatrixData))]
    public void SeparatorMode_Matches_Matrix(GlobSeparatorMode mode, string path, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.Matches(path, pattern, mode));
    }

    [Theory]
    [MemberData(nameof(SeparatorModeMatrixData))]
    public void SeparatorMode_IsIncluded_Matrix(GlobSeparatorMode mode, string path, string pattern, bool expected)
    {
        bool result = GlobMatcher.IsIncluded(path, new[] { pattern }, null, mode);

        Assert.Equal(expected, result);
    }
}