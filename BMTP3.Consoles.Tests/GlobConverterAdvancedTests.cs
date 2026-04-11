using System.Text.RegularExpressions;
using BMTP3.Consoles.Utilities;

namespace BMTP3.Consoles.Tests;

public class GlobConverterAdvancedTests
{
	// ----------------------------------------------------------------
	// Character classes  [abc]
	// ----------------------------------------------------------------

	[Fact]
	public void CharacterClass_abc_MatchesAOrBOrC()
	{
		string re = GlobConverter.GlobToRegex("[abc]test");
		Assert.True(Regex.IsMatch("atest", re), "Expected 'atest' to match '[abc]test'");
		Assert.True(Regex.IsMatch("btest", re), "Expected 'btest' to match '[abc]test'");
		Assert.True(Regex.IsMatch("ctest", re), "Expected 'ctest' to match '[abc]test'");
		Assert.False(Regex.IsMatch("dtest", re), "Expected 'dtest' NOT to match '[abc]test'");
	}

	// ----------------------------------------------------------------
	// Negated character classes  [!xyz]
	// ----------------------------------------------------------------

	[Fact]
	public void NegatedCharacterClass_xyz_DoesNotMatchX()
	{
		string re = GlobConverter.GlobToRegex("[!xyz]test");
		Assert.False(Regex.IsMatch("xtest", re), "Expected 'xtest' NOT to match '[!xyz]test'");
		Assert.False(Regex.IsMatch("ytest", re), "Expected 'ytest' NOT to match '[!xyz]test'");
		Assert.False(Regex.IsMatch("ztest", re), "Expected 'ztest' NOT to match '[!xyz]test'");
		Assert.True(Regex.IsMatch("atest", re), "Expected 'atest' to match '[!xyz]test'");
	}

	// ----------------------------------------------------------------
	// Extglob  +(pattern)  – one or more repetitions
	// ----------------------------------------------------------------

	[Fact]
	public void PlusExtglob_OneOrMore_MatchesOneOrMoreRepetitions()
	{
		string re = GlobConverter.GlobToRegex("+(ab)");
		Assert.True(Regex.IsMatch("ab", re), "Expected 'ab' to match '+(ab)'");
		Assert.True(Regex.IsMatch("abab", re), "Expected 'abab' to match '+(ab)'");
		Assert.False(Regex.IsMatch("", re), "Expected '' NOT to match '+(ab)'");
		Assert.False(Regex.IsMatch("abcd", re), "Expected 'abcd' NOT to match '+(ab)'");
	}

	// ----------------------------------------------------------------
	// Extglob  ?(pattern)  – zero or one repetition
	// ----------------------------------------------------------------

	[Fact]
	public void QuestionExtglob_ZeroOrOne_MatchesZeroOrOneRepetition()
	{
		string re = GlobConverter.GlobToRegex("?(ab)");
		Assert.True(Regex.IsMatch("ab", re), "Expected 'ab' to match '?(ab)'");
		Assert.True(Regex.IsMatch("", re), "Expected '' to match '?(ab)'");
		Assert.False(Regex.IsMatch("abab", re), "Expected 'abab' NOT to match '?(ab)'");
	}
}