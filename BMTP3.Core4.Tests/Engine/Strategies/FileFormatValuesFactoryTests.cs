using BMTP3.Core4.Engine.Strategies;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Tests.Engine.Strategies;

public class FileFormatValuesFactoryTests
{
	private static readonly DateTimeOffset TestDate = new(2026, 6, 18, 14, 30, 45, 123, TimeSpan.Zero);
	private readonly FileFormatValuesFactory _factory = new();

	private static readonly MediaDeviceDriveSourceDetails TestDevice = new()
	{
		DeviceId = "test-device",
		Description = "Test Device",
		FriendlyName = "MyPhone",
		Manufacturer = "TestCorp",
		Model = "X100",
		SerialNumber = "SN123",
		FirmwareVersion = "1.0",
		DriveName = "Phone",
		VolumeLabel = "Internal Storage",
		DriveFormat = "FAT32",
	};

	[Fact]
	public void Create_BasicFields_Populated()
	{
		FileFormatValuesRequest request = new(
			FileName: "photo.jpg",
			RelativeFilePath: "vacation\\photo.jpg",
			CreateFileDate: TestDate,
			ItemId: "abc123",
			StrongHash: "a1b2c3d4e5f6a7b8c9d0e1f2",
			SourceDetails: TestDevice
		);

		Dictionary<string, object> result = _factory.Create(request);

		Assert.Equal("photo", result["fileName"]);
		Assert.Equal("jpg", result["ext"]);
		Assert.Equal("jpg", result["extension"]);
		Assert.Equal("vacation/photo.jpg", result["relativePath"]);
		Assert.Equal("2026", result["YYYY"]);
		Assert.Equal("06", result["MM"]);
		Assert.Equal("18", result["DD"]);
		Assert.Equal("14", result["hh"]);
		Assert.Equal("30", result["mm"]);
		Assert.Equal("45", result["ss"]);
		Assert.Equal("abc123", result["itemId"]);
		Assert.Equal("MyPhone", result["deviceName"]);
		Assert.Equal("X100", result["deviceModel"]);
	}

	[Fact]
	public void Create_HashFields_Populated()
	{
		FileFormatValuesRequest request = new(
			FileName: "f.txt",
			RelativeFilePath: null,
			CreateFileDate: TestDate,
			ItemId: "id",
			StrongHash: "abcdef1234567890",
			SourceDetails: TestDevice
		);

		Dictionary<string, object> result = _factory.Create(request);

		Assert.Equal("abcdef", result["hashShort"]);
		Assert.Equal("abcdef123456", result["hashMedium"]);
		Assert.Equal("abcdef1234567890", result["hashLong"]);
	}

	[Fact]
	public void Create_EmptyHash_ReturnsEmptyStrings()
	{
		FileFormatValuesRequest request = new(
			FileName: "f.txt",
			RelativeFilePath: null,
			CreateFileDate: TestDate,
			ItemId: "id",
			StrongHash: null,
			SourceDetails: TestDevice
		);

		Dictionary<string, object> result = _factory.Create(request);

		Assert.Equal("", result["hashShort"]);
		Assert.Equal("", result["hashMedium"]);
		Assert.Equal("", result["hashLong"]);
	}

	[Fact]
	public void Create_FileNameWithoutExtension_Works()
	{
		FileFormatValuesRequest request = new(
			FileName: "vacation.2026.photo.jpeg",
			RelativeFilePath: null,
			CreateFileDate: TestDate,
			ItemId: "id",
			StrongHash: null,
			SourceDetails: TestDevice
		);

		Dictionary<string, object> result = _factory.Create(request);

		Assert.Equal("vacation.2026.photo", result["fileName"]);
		Assert.Equal("jpeg", result["ext"]);
	}

	[Fact]
	public void Create_FractionalSeconds_AllPrecisions()
	{
		DateTimeOffset precise = new DateTimeOffset(2026, 1, 1, 0, 0, 0, 0, TimeSpan.Zero) + TimeSpan.FromTicks(123_456_7);
		FileFormatValuesRequest request = new(
			FileName: "f.txt",
			RelativeFilePath: null,
			CreateFileDate: precise,
			ItemId: "id",
			StrongHash: null,
			SourceDetails: TestDevice
		);

		Dictionary<string, object> result = _factory.Create(request);

		Assert.Equal("1", result["f"]);
		Assert.Equal("12", result["ff"]);
		Assert.Equal("123", result["fff"]);
		Assert.Equal("1234", result["ffff"]);
		Assert.Equal("12345", result["fffff"]);
		Assert.Equal("123456", result["ffffff"]);
		Assert.Equal("1234567", result["fffffff"]);
	}

	[Fact]
	public void Create_DeviceFields_NotAddedForNonDeviceSource()
	{
		FileFormatValuesRequest request = new(
			FileName: "f.txt",
			RelativeFilePath: null,
			CreateFileDate: TestDate,
			ItemId: "id",
			StrongHash: null,
			SourceDetails: null
		);

		Dictionary<string, object> result = _factory.Create(request);

		Assert.False(result.ContainsKey("deviceName"));
		Assert.False(result.ContainsKey("deviceModel"));
	}

	[Fact]
	public void Create_DeviceFields_NotAddedForFileSystemSource()
	{
		FileFormatValuesRequest request = new(
			FileName: "f.txt",
			RelativeFilePath: null,
			CreateFileDate: TestDate,
			ItemId: "id",
			StrongHash: null,
			SourceDetails: new FileSystemDriveSourceDetails
			{
				DriveName = "C:",
				VolumeLabel = "OS",
				DriveFormat = "NTFS",
			}
		);

		Dictionary<string, object> result = _factory.Create(request);

		Assert.False(result.ContainsKey("deviceName"));
		Assert.False(result.ContainsKey("deviceModel"));
	}

	[Fact]
	public void Create_RelativePath_Normalized()
	{
		FileFormatValuesRequest request = new(
			FileName: "f.txt",
			RelativeFilePath: "\\a\\b\\c\\",
			CreateFileDate: TestDate,
			ItemId: "id",
			StrongHash: null,
			SourceDetails: TestDevice
		);

		Dictionary<string, object> result = _factory.Create(request);

		Assert.Equal("a/b/c", result["relativePath"]);
	}
}
