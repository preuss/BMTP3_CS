using BMTP3.Core4.Api.Models.Enums;
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

	[Fact]
	public void ToInternalCanonicalUri_FileSystem_AbsolutePath_BecomesCanonicalFileUri()
	{
		string result = PathHelper.ToInternalCanonicalUri(@"C:\source\root", BackupSourceType.FileSystem);
		Assert.Equal("file:///c:/source/root", result);
	}

	[Fact]
	public void ToInternalCanonicalUri_FileSystem_ForwardSlashes_Normalized()
	{
		string result = PathHelper.ToInternalCanonicalUri("C:/source/root", BackupSourceType.FileSystem);
		Assert.Equal("file:///c:/source/root", result);
	}

	[Fact]
	public void ToInternalCanonicalUri_FileSystem_CanonicalInput_Unchanged()
	{
		string result = PathHelper.ToInternalCanonicalUri("file:///c:/source/root", BackupSourceType.FileSystem);
		Assert.Equal("file:///c:/source/root", result);
	}

	[Fact]
	public void ToInternalCanonicalUri_FileSystem_UppercaseDriveLowered()
	{
		string result = PathHelper.ToInternalCanonicalUri("file:///C:/source/root", BackupSourceType.FileSystem);
		Assert.Equal("file:///c:/source/root", result);
	}

	[Fact]
	public void ToInternalCanonicalUri_FileSystem_RelativePath_Throws()
	{
		Assert.Throws<ArgumentException>(() => PathHelper.ToInternalCanonicalUri(@"source\root", BackupSourceType.FileSystem));
	}

	[Fact]
	public void ToInternalCanonicalUri_FileSystem_ParentSegments_Resolved()
	{
		string result = PathHelper.ToInternalCanonicalUri(@"C:\source\..\root", BackupSourceType.FileSystem);
		Assert.Equal("file:///c:/root", result);
	}

	[Fact]
	public void ToInternalCanonicalUri_Null_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => PathHelper.ToInternalCanonicalUri(null!, BackupSourceType.FileSystem));
	}

	[Fact]
	public void ToInternalCanonicalUri_SourceTypeMismatch_Throws()
	{
		Assert.Throws<ArgumentException>(() => PathHelper.ToInternalCanonicalUri(@"C:\source", BackupSourceType.MediaDevice));
	}

	[Fact]
	public void ToInternalCanonicalUri_InvalidEnum_Throws()
	{
		Assert.Throws<ArgumentException>(() => PathHelper.ToInternalCanonicalUri("mtp://device/path", (BackupSourceType)999));
	}

	[Fact]
	public void ToInternalCanonicalUri_FileSystem_UncPath_BecomesCanonicalShareUri()
	{
		string result = PathHelper.ToInternalCanonicalUri(@"\\server\share\dir", BackupSourceType.FileSystem);
		Assert.Equal("file://server/share/dir", result);
	}

	[Fact]
	public void ToInternalCanonicalUri_FileSystem_UncRoot_BecomesCanonicalShareUri()
	{
		string result = PathHelper.ToInternalCanonicalUri(@"\\server\share", BackupSourceType.FileSystem);
		Assert.Equal("file://server/share", result);
	}

	[Fact]
	public void ToInternalCanonicalUri_FileSystem_ShareUri_CanonicalInputUnchanged()
	{
		string result = PathHelper.ToInternalCanonicalUri("file://server/share/dir", BackupSourceType.FileSystem);
		Assert.Equal("file://server/share/dir", result);
	}

	[Fact]
	public void ToInternalCanonicalUri_FileSystem_ShareUri_BackslashSeparators_Normalized()
	{
		string result = PathHelper.ToInternalCanonicalUri(@"file://server/share\dir", BackupSourceType.FileSystem);
		Assert.Equal("file://server/share/dir", result);
	}

	[Fact]
	public void ToInternalCanonicalUri_FileSystem_UncWithoutShare_Throws()
	{
		Assert.Throws<ArgumentException>(() => PathHelper.ToInternalCanonicalUri(@"\\server", BackupSourceType.FileSystem));
	}

	[Fact]
	public void ToInternalCanonicalUri_MediaDevice_CanonicalUriUnchanged()
	{
		string result = PathHelper.ToInternalCanonicalUri("mtp://Apple iPad/Internal Storage/DCIM", BackupSourceType.MediaDevice);
		Assert.Equal("mtp://Apple iPad/Internal Storage/DCIM", result);
	}

	[Fact]
	public void ToInternalCanonicalUri_MediaDevice_MixedSeparators_Normalized()
	{
		string result = PathHelper.ToInternalCanonicalUri(@"mtp://Apple iPad\Internal Storage\DCIM", BackupSourceType.MediaDevice);
		Assert.Equal("mtp://Apple iPad/Internal Storage/DCIM", result);
	}

	[Fact]
	public void ToInternalCanonicalUri_MediaDevice_WithoutScheme_Throws()
	{
		Assert.Throws<ArgumentException>(() => PathHelper.ToInternalCanonicalUri(@"Device\Storage", BackupSourceType.MediaDevice));
	}

	[Theory]
	[InlineData("a\\b/c", '/', "a/b/c")]
	[InlineData("a\\b/c", '\\', "a\\b\\c")]
	[InlineData("C:\\Temp\\root.jpg", '/', "C:/Temp/root.jpg")]
	[InlineData("already/canonical", '/', "already/canonical")]
	public void NormalizeSeparators_ReplacesBothSeparators(string input, char separator, string expected)
	{
		string result = PathHelper.NormalizeSeparators(input, separator);
		Assert.Equal(expected, result);
	}

	[Fact]
	public void NormalizeSeparators_DefaultSeparator_IsCanonical()
	{
		string result = PathHelper.NormalizeSeparators(@"C:\Temp\root.jpg");
		Assert.Equal("C:/Temp/root.jpg", result);
	}

	[Fact]
	public void NormalizeSeparators_Null_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => PathHelper.NormalizeSeparators(null!));
	}

	[Theory]
	[InlineData('|')]
	[InlineData(':')]
	[InlineData('.')]
	[InlineData('?')]
	[InlineData('a')]
	public void NormalizeSeparators_InvalidSeparator_Throws(char separator)
	{
		Assert.Throws<ArgumentException>(() => PathHelper.NormalizeSeparators("a/b", separator));
	}
}
