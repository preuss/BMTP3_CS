using BMTP3.Core4.Traversal;

namespace BMTP3.Core4.Tests.Traversal;

public class MtpUriParserTests
{
	[Fact]
	public void Parse_DeviceAndPath_ReturnsCorrectParts()
	{
		MtpUriParseResult result = MtpUriParser.Parse("mtp://Apple iPad/Internal Storage/DCIM/IMG_001.jpg");
		Assert.Equal("Apple iPad", result.DeviceName);
		Assert.Equal("Internal Storage/DCIM/IMG_001.jpg", result.DevicePath);
	}

	[Fact]
	public void Parse_DeviceOnly_ReturnsEmptyPath()
	{
		MtpUriParseResult result = MtpUriParser.Parse("mtp://Apple iPad");
		Assert.Equal("Apple iPad", result.DeviceName);
		Assert.Equal("", result.DevicePath);
	}

	[Fact]
	public void Parse_DeviceWithTrailingSlash_ReturnsEmptyPath()
	{
		MtpUriParseResult result = MtpUriParser.Parse("mtp://Apple iPad/");
		Assert.Equal("Apple iPad", result.DeviceName);
		Assert.Equal("", result.DevicePath);
	}

	[Fact]
	public void Parse_PathWithTrailingSlash_TrimsTrailingSlash()
	{
		MtpUriParseResult result = MtpUriParser.Parse("mtp://Device/DCIM/");
		Assert.Equal("Device", result.DeviceName);
		Assert.Equal("DCIM", result.DevicePath);
	}

	[Fact]
	public void Parse_DeviceNameWithSpaces_ReturnsCorrectDeviceName()
	{
		MtpUriParseResult result = MtpUriParser.Parse("mtp://My Android Phone/Internal Storage");
		Assert.Equal("My Android Phone", result.DeviceName);
		Assert.Equal("Internal Storage", result.DevicePath);
	}

	[Fact]
	public void Parse_SingleLevelPath_ReturnsCorrectParts()
	{
		MtpUriParseResult result = MtpUriParser.Parse("mtp://Device/DCIM");
		Assert.Equal("Device", result.DeviceName);
		Assert.Equal("DCIM", result.DevicePath);
	}

	[Fact]
	public void Parse_DeepPath_ReturnsCorrectParts()
	{
		MtpUriParseResult result = MtpUriParser.Parse("mtp://Camera/DCIM/100MSDCF/IMG_1234.JPG");
		Assert.Equal("Camera", result.DeviceName);
		Assert.Equal("DCIM/100MSDCF/IMG_1234.JPG", result.DevicePath);
	}

	[Fact]
	public void Parse_EmptyString_Throws()
	{
		Assert.Throws<ArgumentException>(() => MtpUriParser.Parse(""));
	}

	[Fact]
	public void Parse_NullString_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => MtpUriParser.Parse(null!));
	}

	[Fact]
	public void Parse_WhiteSpaceString_Throws()
	{
		Assert.Throws<ArgumentException>(() => MtpUriParser.Parse("   "));
	}

	[Fact]
	public void Parse_NoScheme_Throws()
	{
		Assert.Throws<ArgumentException>(() => MtpUriParser.Parse("Apple iPad/DCIM"));
	}

	[Fact]
	public void Parse_WrongScheme_Throws()
	{
		Assert.Throws<ArgumentException>(() => MtpUriParser.Parse("http://Apple iPad/DCIM"));
	}

	[Fact]
	public void Parse_OnlyScheme_Throws()
	{
		Assert.Throws<ArgumentException>(() => MtpUriParser.Parse("mtp://"));
	}

	[Fact]
	public void Parse_CaseSensitiveScheme_MustBeLowercase()
	{
		Assert.Throws<ArgumentException>(() => MtpUriParser.Parse("MTP://Device/Path"));
	}

	[Fact]
	public void Parse_DeviceNameWithSlashes_TakesFirstSlashAsPathBoundary()
	{
		MtpUriParseResult result = MtpUriParser.Parse("mtp://Device/Path/With/Slashes");
		Assert.Equal("Device", result.DeviceName);
		Assert.Equal("Path/With/Slashes", result.DevicePath);
	}
}