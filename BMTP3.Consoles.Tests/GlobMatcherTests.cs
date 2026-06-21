using BMTP3.Common.Utilities;

namespace BMTP3.Consoles.Tests;

public class GlobMatcherTests
{
	[Fact]
	public void StarMatchesFilename()
	{
		string re = GlobMatcher.GlobToRegex("*.txt");
		Assert.Matches(re, "file.txt");
		Assert.DoesNotMatch(re, "file.jpg");
		Assert.DoesNotMatch(re, "sub/file.txt");
	}

	[Fact]
	public void RecursiveMatches()
	{
		string re = GlobMatcher.GlobToRegex("**/*.txt");
		Assert.Matches(re, "a/b/c.txt");
		Assert.Matches(re, "file.txt");
	}

	[Fact]
	public void BothSlashesAreSeparators()
	{
		string re = GlobMatcher.GlobToRegex("sub/*.txt", GlobSeparatorMode.Both);
		Assert.Matches(re, "sub/file.txt");
		Assert.Matches(re, @"sub\file.txt");
	}

	[Fact]
	public void ExtglobAlternation()
	{
		string re = GlobMatcher.GlobToRegex("@(foo|bar).txt");
		Assert.Matches(re, "foo.txt");
		Assert.Matches(re, "bar.txt");
		Assert.DoesNotMatch(re, "baz.txt");
	}

	[Fact]
	public void SuffixNegation()
	{
		string re = GlobMatcher.GlobToRegex("*.!(jpg)");
		Assert.Matches(re, "file.png");
		Assert.DoesNotMatch(re, "file.jpg");
	}

	[Fact]
	public void GlobalNegation()
	{
		string re = GlobMatcher.GlobToRegex("!(foo)");
		Assert.Matches(re, "bar");
		Assert.DoesNotMatch(re, "foo");
	}
}