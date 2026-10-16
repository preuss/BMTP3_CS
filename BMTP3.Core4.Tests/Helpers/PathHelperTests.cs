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

	[Theory]
	[InlineData(@"\folder\file\", '\\', @"folder\file")]
	[InlineData(@"/folder/file/", '/', "folder/file")]
	public void TrimSeparators_TrimsLeadingAndTrailing(string input, char separator, string expected)
	{
		string result = PathHelper.TrimSeparators(input, separator);
		Assert.Equal(expected, result);
	}

	[Fact]
	public void TrimSeparators_NoSeparators_Unchanged()
	{
		string result = PathHelper.TrimSeparators(@"folder\file", '\\');
		Assert.Equal(@"folder\file", result);
	}

	[Fact]
	public void TrimSeparators_OnlyTrimsSpecifiedSeparator()
	{
		string result = PathHelper.TrimSeparators(@"\folder\file/", '\\');
		Assert.Equal(@"folder\file/", result);
	}

	[Fact]
	public void TrimSeparators_AllSeparators_ReturnsEmpty()
	{
		string result = PathHelper.TrimSeparators(@"\\\\", '\\');
		Assert.Equal("", result);
	}

	[Fact]
	public void TrimSeparators_Null_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => PathHelper.TrimSeparators(null!, '\\'));
	}

	[Fact]
	public void TrimSeparators_InvalidSeparator_Throws()
	{
		Assert.Throws<ArgumentException>(() => PathHelper.TrimSeparators("a/b", '|'));
	}

	// ------------------------------------------------------------
	// SanitizeFileNameComponent
	// ------------------------------------------------------------

	[Theory]
	[InlineData("a*b.log", "a_b.log")]
	[InlineData("a?b.log", "a_b.log")]
	[InlineData("a<b>c.log", "a_b_c.log")]
	[InlineData("a|b.log", "a_b.log")]
	[InlineData("a:b.log", "a_b.log")]
	[InlineData("a\"b.log", "a_b.log")]
	[InlineData("a/b.log", "a_b.log")]
	[InlineData("a\\b.log", "a_b.log")]
	[InlineData("a\tb.log", "a_b.log")]
	[InlineData("a\u0001b.log", "a_b.log")]
	[InlineData("***", "___")]
	public void SanitizeFileNameComponent_ReplacesReservedCharacters(string input, string expected)
	{
		Assert.Equal(expected, PathHelper.SanitizeFileNameComponent(input));
	}

	[Theory]
	[InlineData("img_100@Phone.jpg")]
	[InlineData("ferie æøå bil.jpg")]
	[InlineData("navn mellemrum.jpg")]
	[InlineData(".gitignore")]
	[InlineData("photo (2026) [kopi].jpg")]
	public void SanitizeFileNameComponent_LegalCharacters_Unchanged(string name)
	{
		Assert.Equal(name, PathHelper.SanitizeFileNameComponent(name));
	}

	[Theory]
	[InlineData("foo.", "foo")]
	[InlineData("foo ", "foo")]
	[InlineData("foo. ", "foo")]
	[InlineData("folder.v1.", "folder.v1")]
	public void SanitizeFileNameComponent_TrailingDotOrSpace_Trimmed(string input, string expected)
	{
		Assert.Equal(expected, PathHelper.SanitizeFileNameComponent(input));
	}

	[Fact]
	public void SanitizeFileNameComponent_LeadingDotOrSpace_Preserved()
	{
		Assert.Equal(".gitignore", PathHelper.SanitizeFileNameComponent(".gitignore"));
		Assert.Equal(" .gitignore", PathHelper.SanitizeFileNameComponent(" .gitignore"));
	}

	[Theory]
	[InlineData("")]
	[InlineData("...")]
	[InlineData("..")]
	[InlineData("   ")]
	public void SanitizeFileNameComponent_EmptyAfterSanitize_ReturnsUnderscore(string input)
	{
		Assert.Equal("_", PathHelper.SanitizeFileNameComponent(input));
	}

	[Theory]
	[InlineData("CON", "CON_")]
	[InlineData("con", "con_")]
	[InlineData("NUL.txt", "NUL_.txt")]
	[InlineData("CON.file", "CON_.file")]
	[InlineData("COM1.log", "COM1_.log")]
	[InlineData("lpt1.dat", "lpt1_.dat")]
	[InlineData("aux", "aux_")]
	[InlineData("CON.", "CON_")]
	public void SanitizeFileNameComponent_ReservedDeviceName_GetsSuffix(string input, string expected)
	{
		Assert.Equal(expected, PathHelper.SanitizeFileNameComponent(input));
	}

	[Theory]
	[InlineData("CONSOLE.txt")]
	[InlineData("COM10")]
	[InlineData("NULX")]
	[InlineData("LPT10")]
	public void SanitizeFileNameComponent_NonReservedNames_Unchanged(string name)
	{
		Assert.Equal(name, PathHelper.SanitizeFileNameComponent(name));
	}

	[Theory]
	[InlineData("a*b.log")]
	[InlineData("CON")]
	[InlineData("foo.")]
	[InlineData("NUL.txt")]
	[InlineData("a_b.log")]
	public void SanitizeFileNameComponent_Idempotent(string input)
	{
		string firstPass = PathHelper.SanitizeFileNameComponent(input);
		string secondPass = PathHelper.SanitizeFileNameComponent(firstPass);

		Assert.Equal(firstPass, secondPass);
	}

	[Fact]
	public void SanitizeFileNameComponent_Null_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => PathHelper.SanitizeFileNameComponent(null!));
	}

	[Fact]
	public void SanitizeFileNameComponent_RealWorldMtpName_OnlyAsteriskReplaced()
	{
		string raw = "HilinkSvc_null_evt_17.0.4.313_2844af23b5bc39a0f4961aab67d8ddf*c4bc558b533201209b84f4415ee7b3_@1789845320822.log";

		string sanitized = PathHelper.SanitizeFileNameComponent(raw);

		Assert.DoesNotContain("*", sanitized);
		Assert.Contains("@1789845320822.log", sanitized);
		Assert.Contains("ddf_c4bc558b", sanitized);
		Assert.Equal(raw.Length, sanitized.Length);
	}

	// ------------------------------------------------------------
	// SanitizeRelativePath
	// ------------------------------------------------------------

	[Theory]
	[InlineData("a*/b?/c.txt", @"a_/b_/c.txt")]
	[InlineData("Android/data/com.huawei.hilink.framework/files/Log", @"Android/data/com.huawei.hilink.framework/files/Log")]
	[InlineData(@"vacation\2026", @"vacation\2026")]
	[InlineData("/lead/", "/lead/")]
	[InlineData("", "")]
	public void SanitizeRelativePath_SanitizesEachSegment(string input, string expected)
	{
		Assert.Equal(expected, PathHelper.SanitizeRelativePath(input));
	}

	[Fact]
	public void SanitizeRelativePath_TraversalSegments_CollapseToUnderscore()
	{
		Assert.Equal(@"a/_/b", PathHelper.SanitizeRelativePath("a/../b"));
		Assert.Equal(@"_/b", PathHelper.SanitizeRelativePath("./b"));
	}

	[Theory]
	[InlineData("CON/file.txt", @"CON_/file.txt")]
	[InlineData("aux\\sub\\nul.dat", @"aux_\sub\nul_.dat")]
	public void SanitizeRelativePath_ReservedDeviceNameSegment_GetsSuffix(string input, string expected)
	{
		Assert.Equal(expected, PathHelper.SanitizeRelativePath(input));
	}

	[Fact]
	public void SanitizeRelativePath_MixedSeparatorsAndEmptyComponents_Preserved()
	{
		Assert.Equal(@"a/b\c", PathHelper.SanitizeRelativePath("a/b\\c"));
		Assert.Equal(@"a//b", PathHelper.SanitizeRelativePath("a//b"));
	}

	[Fact]
	public void SanitizeRelativePath_LegalCharactersAndUnicode_Unchanged()
	{
		Assert.Equal(@"ferie æøå/@name", PathHelper.SanitizeRelativePath("ferie æøå/@name"));
	}

	[Fact]
	public void SanitizeRelativePath_Null_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => PathHelper.SanitizeRelativePath(null!));
	}
}
