using BMTP3.Core4.Utilities;

namespace BMTP3.Core4.Tests.Utilities;

/// <summary>
///     Adversarial edge-case tests for <see cref="GlobMatcher"/>.
///     Targets non-obvious corner cases that the basic tests may miss.
/// </summary>
public class GlobMatcherAdversarialTests
{
	// -----------------------------------------------------------------------
	// Pipe | inside negation patterns
	// -----------------------------------------------------------------------

	[Theory]
	[InlineData("foo", "!(foo|bar)", false)]
	[InlineData("bar", "!(foo|bar)", false)]
	[InlineData("baz", "!(foo|bar)", true)]
	[InlineData("foobar", "!(foo|bar)", true)]
	public void GlobalNegation_WithPipeAlternation(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	[Theory]
	[InlineData("file.gif", "*.!(jpg|png)", true)]
	[InlineData("file.jpg", "*.!(jpg|png)", false)]
	[InlineData("file.png", "*.!(jpg|png)", false)]
	[InlineData("file.jpgg", "*.!(jpg|png)", true)]
	public void SuffixNegation_WithPipeAlternation(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	// -----------------------------------------------------------------------
	// Brace expansion edge cases
	// -----------------------------------------------------------------------

	[Theory]
	[InlineData("a", "{a,b,}", true)]
	[InlineData("b", "{a,b,}", true)]
	[InlineData("", "{a,b,}", true)]   // trailing comma → empty alternative
	[InlineData("c", "{a,b,}", false)]
	public void BraceExpansion_TrailingComma(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	[Theory]
	[InlineData("a", "{a,,b}", true)]
	[InlineData("", "{a,,b}", true)]   // empty middle alternative
	[InlineData("b", "{a,,b}", true)]
	[InlineData("c", "{a,,b}", false)]
	public void BraceExpansion_EmptyMiddleAlternative(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	[Theory]
	[InlineData("a/c.txt", "{a,b}/{c,d}.txt", true)]
	[InlineData("a/d.txt", "{a,b}/{c,d}.txt", true)]
	[InlineData("b/c.txt", "{a,b}/{c,d}.txt", true)]
	[InlineData("b/d.txt", "{a,b}/{c,d}.txt", true)]
	[InlineData("a/e.txt", "{a,b}/{c,d}.txt", false)]
	public void BraceExpansion_MultipleGroups(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	[Theory]
	[InlineData("a_c.txt", "{a,b}_c.txt", true)]
	[InlineData("b_c.txt", "{a,b}_c.txt", true)]
	[InlineData("c_c.txt", "{a,b}_c.txt", false)]
	public void BraceExpansion_UnderscoreJoin(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	// -----------------------------------------------------------------------
	// Character class edge cases
	// -----------------------------------------------------------------------

	[Theory]
	[InlineData("a.txt", "[!a].txt", false)]
	[InlineData("b.txt", "[!a].txt", true)]
	[InlineData("a", "[!a]", false)]
	[InlineData("b", "[!a]", true)]
	public void NegatedCharacterClass(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	[Theory]
	[InlineData("a.txt", "[a-z].txt", true)]
	[InlineData("m.txt", "[a-z].txt", true)]
	[InlineData("z.txt", "[a-z].txt", true)]
	[InlineData("1.txt", "[a-z].txt", false)]
	public void CharacterClassRange(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	[Theory]
	[InlineData("0.txt", "[0-9].txt", true)]
	[InlineData("5.txt", "[0-9].txt", true)]
	[InlineData("9.txt", "[0-9].txt", true)]
	[InlineData("a.txt", "[0-9].txt", false)]
	public void CharacterClassDigitRange(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	// -----------------------------------------------------------------------
	// Extglob edge cases
	// -----------------------------------------------------------------------

	[Theory]
	[InlineData("", "*(a|b)", true)]
	[InlineData("a", "*(a|b)", true)]
	[InlineData("b", "*(a|b)", true)]
	[InlineData("ab", "*(a|b)", true)]
	[InlineData("ba", "*(a|b)", true)]
	[InlineData("c", "*(a|b)", false)]
	public void ExtglobStar_ZeroOrMore(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	[Theory]
	[InlineData("", "+(a|b)", false)]
	[InlineData("a", "+(a|b)", true)]
	[InlineData("b", "+(a|b)", true)]
	[InlineData("ab", "+(a|b)", true)]
	[InlineData("c", "+(a|b)", false)]
	public void ExtglobPlus_OneOrMore(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	[Theory]
	[InlineData("", "?(a|b)", true)]
	[InlineData("a", "?(a|b)", true)]
	[InlineData("b", "?(a|b)", true)]
	[InlineData("c", "?(a|b)", false)]
	public void ExtglobQuestion_ZeroOrOne(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	[Theory]
	[InlineData("ac", "@(a|b)@(c|d)", true)]
	[InlineData("ad", "@(a|b)@(c|d)", true)]
	[InlineData("bc", "@(a|b)@(c|d)", true)]
	[InlineData("bd", "@(a|b)@(c|d)", true)]
	[InlineData("ae", "@(a|b)@(c|d)", false)]
	public void Extglob_MultipleGroups(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	[Theory]
	[InlineData("foo.txt", "foo@(.txt|.csv)", true)]
	[InlineData("foo.csv", "foo@(.txt|.csv)", true)]
	[InlineData("foo.jpg", "foo@(.txt|.csv)", false)]
	public void ExtglobInMiddle_WithDot(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	// -----------------------------------------------------------------------
	// Regex special chars in literal text
	// -----------------------------------------------------------------------

	[Theory]
	[InlineData("file.txt", "file.txt", true)]
	[InlineData("fileXtxt", "file.txt", false)] // . must be literal dot
	public void DotIsLiteral(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	[Theory]
	[InlineData("cost+tax.txt", "cost+tax.txt", true)]
	[InlineData("costtax.txt", "cost+tax.txt", false)]
	public void PlusIsLiteral(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	[Theory]
	[InlineData("price$100.txt", "price$100.txt", true)]
	[InlineData("price100.txt", "price$100.txt", false)]
	public void DollarIsLiteral(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	[Theory]
	[InlineData("readme(v2).txt", "readme(v2).txt", true)]
	public void ParensAreLiteral(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	// -----------------------------------------------------------------------
	// Case sensitivity
	// -----------------------------------------------------------------------

	[Theory]
	[InlineData("FILE.JPG", "*.jpg", true)]
	[InlineData("file.JPG", "*.jpg", true)]
	[InlineData("File.jpg", "*.JPG", true)]
	public void CaseInsensitive(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}

	// -----------------------------------------------------------------------
	// IsIncluded — complex combinations
	// -----------------------------------------------------------------------

	[Fact]
	public void IsIncluded_MixedIncludeAndExclude()
	{
		List<string> include = new() { "**/*.cs", "**/*.md" };
		List<string> exclude = new() { "**/bin/**", "**/obj/**" };
		Assert.True(GlobMatcher.IsIncluded("src/foo.cs", include, exclude));
		Assert.True(GlobMatcher.IsIncluded("readme.md", include, exclude));
		Assert.False(GlobMatcher.IsIncluded("src/bin/foo.cs", include, exclude));
		Assert.False(GlobMatcher.IsIncluded("src/obj/foo.cs", include, exclude));
		Assert.False(GlobMatcher.IsIncluded("src/foo.txt", include, exclude));
	}

	[Fact]
	public void IsIncluded_IncludeOnlyWithoutMatch()
	{
		List<string> include = new() { "**/*.jpg" };
		// No exclude — unmatched paths should be excluded
		Assert.False(GlobMatcher.IsIncluded("doc.txt", include, null));
	}

	// -----------------------------------------------------------------------
	// Pattern normalisation
	// -----------------------------------------------------------------------

	[Theory]
	[InlineData("a/b\\c/d.txt", "**/*.txt", true)]
	public void MixedSeparators(string path, string pattern, bool expected)
	{
		Assert.Equal(expected, GlobMatcher.Matches(path, pattern));
	}
}
