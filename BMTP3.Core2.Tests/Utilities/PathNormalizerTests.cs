using BMTP3.Core2.BackupNew.Utilities;

namespace BMTP3.Core2.Tests.Utilities;

public class PathNormalizerTests
{
	// ── NormalizeFileUri ─────────────────────────────────────────────────────

	[Fact]
	public void NormalizeFileUri_NormalWindowsPath_ReturnsFileSchemeUri()
	{
		// A well-formed absolute path should become a file:///... URI.
		string path = @"C:\Users\test\file.txt";
		string result = PathNormalizer.NormalizeFileUri(path);

		Assert.StartsWith("file:///", result, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("Users", result);
		Assert.Contains("file.txt", result);
	}

	[Fact]
	public void NormalizeFileUri_NullInput_ReturnsEmpty()
	{
		string result = PathNormalizer.NormalizeFileUri(null!);
		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public void NormalizeFileUri_EmptyString_ReturnsEmpty()
	{
		string result = PathNormalizer.NormalizeFileUri(string.Empty);
		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public void NormalizeFileUri_WhitespaceOnly_ReturnsEmpty()
	{
		string result = PathNormalizer.NormalizeFileUri("   ");
		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public void NormalizeFileUri_UncPath_ReturnsFileSchemeUriWithAuthority()
	{
		// UNC paths like \\server\share\file.txt should produce file://server/share/file.txt
		string path = @"\\server\share\file.txt";
		string result = PathNormalizer.NormalizeFileUri(path);

		// Must start with file:// (authority = server)
		Assert.StartsWith("file://", result, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("server", result);
		Assert.Contains("file.txt", result);
	}

	[Fact]
	public void NormalizeFileUri_OutputIsAbsoluteFileUri()
	{
		string path = @"C:\Temp\photo.jpg";
		string result = PathNormalizer.NormalizeFileUri(path);

		// Result must be parseable as an absolute file URI
		Assert.False(string.IsNullOrEmpty(result));
		Uri uri = new(result);
		Assert.True(uri.IsAbsoluteUri);
		Assert.Equal(Uri.UriSchemeFile, uri.Scheme, true);
	}

	// ── NormalizeMtpUri ──────────────────────────────────────────────────────

	[Fact]
	public void NormalizeMtpUri_DotSegments_AreResolvedFromPath()
	{
		// Dot segments ("." and "..") must be collapsed by the normalizer.
		string deviceId = "MyPhone";
		string path = @"DCIM\100APPLE\..\200APPLE\./IMG_0001.JPG";

		string result = PathNormalizer.NormalizeMtpUri(path, deviceId);

		// Should NOT contain raw "." or ".." segments in the URI output
		Assert.DoesNotContain("/..", result);
		Assert.DoesNotContain("/./", result);
		Assert.Contains("MyPhone", result);
		Assert.Contains("IMG_0001.JPG", result);
		// "100APPLE" was navigated away from via ".."
		Assert.DoesNotContain("100APPLE", result);
		// "200APPLE" should survive (the ".." went back from it but "./" is a no-op)
		Assert.Contains("200APPLE", result);
	}

	[Fact]
	public void NormalizeMtpUri_EmptyDeviceId_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() =>
			PathNormalizer.NormalizeMtpUri("DCIM/photo.jpg", string.Empty));
	}

	[Fact]
	public void NormalizeMtpUri_NullDeviceId_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() =>
			PathNormalizer.NormalizeMtpUri("DCIM/photo.jpg", null!));
	}

	[Fact]
	public void NormalizeMtpUri_EmptyPath_ReturnsMtpRootUri()
	{
		string result = PathNormalizer.NormalizeMtpUri(string.Empty, "device1");

		// Should at minimum contain the mtp scheme and device authority
		Assert.StartsWith("mtp://", result, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("device1", result);
	}

	[Fact]
	public void NormalizeMtpUri_NormalPath_ReturnsMtpSchemeUri()
	{
		string result = PathNormalizer.NormalizeMtpUri(@"DCIM\100APPLE\IMG_0001.JPG", "Apple iPhone");

		Assert.StartsWith("mtp://", result, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("IMG_0001.JPG", result);
	}
}