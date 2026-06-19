using BMTP3.Common.Utilities;
using System.Text.RegularExpressions;

namespace BMTP3.Consoles.Tests;

public class GlobMatcherTests
{
	[Fact]
	public void StarMatchesFilename()
	{
		string re = GlobMatcher.GlobToRegex("*.txt");
		Assert.True(Regex.IsMatch("file.txt", re));
		Assert.False(Regex.IsMatch("file.jpg", re));
		Assert.False(Regex.IsMatch("sub/file.txt", re));
	}

	[Fact]
	public void RecursiveMatches()
	{
		string re = GlobMatcher.GlobToRegex("**/*.txt");
		Assert.True(Regex.IsMatch("a/b/c.txt", re));
		Assert.True(Regex.IsMatch("file.txt", re));
	}

	[Fact]
	public void BothSlashesAreSeparatorsByDefault()
	{
		string re = GlobMatcher.GlobToRegex("sub/*.txt");
		Assert.True(Regex.IsMatch("sub/file.txt", re));
		Assert.True(Regex.IsMatch(@"sub\file.txt", re));
	}

	[Fact]
	public void ExtglobAlternation()
	{
		string re = GlobMatcher.GlobToRegex("@(foo|bar).txt");
		Assert.True(Regex.IsMatch("foo.txt", re));
		Assert.True(Regex.IsMatch("bar.txt", re));
		Assert.False(Regex.IsMatch("baz.txt", re));
	}

	[Fact]
	public void SuffixNegation()
	{
		string re = GlobMatcher.GlobToRegex("*.!(jpg)");
		Assert.True(Regex.IsMatch("file.png", re));
		Assert.False(Regex.IsMatch("file.jpg", re));
	}

	[Fact]
	public void GlobalNegation()
	{
		string re = GlobMatcher.GlobToRegex("!(foo)");
		Assert.True(Regex.IsMatch("bar", re));
		Assert.False(Regex.IsMatch("foo", re));
	}
}