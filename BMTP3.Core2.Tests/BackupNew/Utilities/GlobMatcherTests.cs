using BMTP3.Common.Utilities;
using BMTP3.Core2.BackupNew.Utilities;

namespace BMTP3.Core2.Tests.BackupNew.Utilities;

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
	// Character classes
	// -----------------------------------------------------------------------

	[Theory]
	[InlineData("file.jpg", "[jJ][pP][gG]", false)] // pattern is extension only
	[InlineData("a.txt", "[abc].txt", true)]
	[InlineData("d.txt", "[abc].txt", false)]
	[InlineData("a.txt", "[!xyz].txt", true)]
	[InlineData("x.txt", "[!xyz].txt", false)]
	public void CharacterClasses(string path, string pattern, bool expected)
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
}