using BMTP3.Core4.Helpers;

namespace BMTP3.Core4.Tests.Helpers;

public class PathHelperTests
{
	[Theory]
	[InlineData(null, true, "")]
	[InlineData("", true, "")]
	[InlineData("   ", true, "")]
	[InlineData("Folder/File.jpg", false, "Folder\\File.jpg")]
	[InlineData("\\Folder\\File.jpg", false, "Folder\\File.jpg")]
	[InlineData("/Folder/File.jpg", false, "Folder\\File.jpg")]
	[InlineData("a/b/c", false, "a\\b\\c")]
	[InlineData("single", false, "single")]
	public void NormalizeCustomRelativePath_Valid(string? input, bool normalizeNull, string expected)
	{
		string result = PathHelper.NormalizeCustomRelativePath(input, normalizeNull);
		Assert.Equal(expected, result);
	}

	[Fact]
	public void NormalizeCustomRelativePath_NullWithoutNormalize_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => PathHelper.NormalizeCustomRelativePath(null, false));
	}

	[Fact]
	public void NormalizeCustomRelativePath_EmptyWithoutNormalize_Throws()
	{
		Assert.Throws<ArgumentException>(() => PathHelper.NormalizeCustomRelativePath("", false));
	}

	[Fact]
	public void NormalizeCustomRelativePath_WhitespaceWithoutNormalize_Throws()
	{
		Assert.Throws<ArgumentException>(() => PathHelper.NormalizeCustomRelativePath("   ", false));
	}

	[Theory]
	[InlineData("C:\\path")]
	[InlineData("mtp://device/path")]
	[InlineData("file:with:colons")]
	public void NormalizeCustomRelativePath_ContainsColon_Throws(string input)
	{
		Assert.Throws<ArgumentException>(() => PathHelper.NormalizeCustomRelativePath(input, false));
	}

	[Theory]
	[InlineData("../escape")]
	[InlineData("a/../../b")]
	[InlineData("..\\relative")]
	[InlineData("./Folder")]
	[InlineData("Folder/./File")]
	[InlineData("./././file")]
	public void NormalizeCustomRelativePath_TraversalSegments_Throws(string input)
	{
		Assert.Throws<ArgumentException>(() => PathHelper.NormalizeCustomRelativePath(input, false));
	}

	[Fact]
	public void NormalizeCustomRelativePath_TrailingSeparator_Trimmed()
	{
		string result = PathHelper.NormalizeCustomRelativePath("folder\\file\\", false);
		Assert.Equal("folder\\file", result);
	}

	[Fact]
	public void NormalizeCustomRelativePath_LeadingSeparator_Trimmed()
	{
		string result = PathHelper.NormalizeCustomRelativePath("\\folder\\file", false);
		Assert.Equal("folder\\file", result);
	}

	[Fact]
	public void NormalizeCustomRelativePath_MixedSlashes_Normalized()
	{
		string result = PathHelper.NormalizeCustomRelativePath("a/b\\c/d", false);
		Assert.Equal("a\\b\\c\\d", result);
	}

	[Fact]
	public void NormalizeCustomRelativePath_OnlySeparators_Throws()
	{
		Assert.Throws<ArgumentException>(() => PathHelper.NormalizeCustomRelativePath("///", false));
	}
}
