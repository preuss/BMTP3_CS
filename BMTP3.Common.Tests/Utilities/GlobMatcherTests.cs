
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
	[InlineData("sub\\file.txt", "sub/*.txt", true)] // backslash path
	[InlineData("other/file.txt", "sub/*.txt", false)]
	public void Separator_Flexibility(string path, string pattern, bool expected)
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
		// Only blank patterns → treated as having effective patterns → non-.jpg excluded
		List<string> blanksOnly = new() { "", "   " };
		// All patterns are blank, so hasAny stays false → include everything
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
		// Without a comma the braces are treated as literal characters
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	// -----------------------------------------------------------------------
	// Windows absolute paths (backslash)
	// -----------------------------------------------------------------------

	[Fact]
	public void IsIncluded_WindowsAbsolutePath()
	{
		List<string> include = new() { "**\\*.jpg" };
		// Path uses backslash — GlobMatcher normalises to forward slash internally
		Assert.True(GlobMatcher.IsIncluded(@"C:\Photos\2024\img.jpg", include, null));
		Assert.False(GlobMatcher.IsIncluded(@"C:\Docs\readme.txt", include, null));
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
}
