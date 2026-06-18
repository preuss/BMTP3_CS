using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Strategies;
using BMTP3.Core4.Models;
using BMTP3.Core4.Tests.Fakes;

namespace BMTP3.Core4.Tests.Engine.Strategies;

public class TargetPathResolverTests
{
	private static readonly DateTimeOffset TestDate = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
	private readonly TargetPathResolver _resolver = new(
		new FakeMessageFormatter(),
		new FakeFileFormatValuesFactory()
	);

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
	public void Resolve_PreserveHierarchy_WithRelativeDir()
	{
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: "vacation\\2026",
			FileName: "photo.jpg",
			OutputStructureStrategy: OutputStructureStrategy.PreserveHierarchy,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: null,
			SourceDetails: null
		);

		string result = _resolver.Resolve(request);

		Assert.Equal("D:\\Backup\\vacation\\2026\\photo.jpg", result);
	}

	[Fact]
	public void Resolve_PreserveHierarchy_WithoutRelativeDir()
	{
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: null,
			FileName: "photo.jpg",
			OutputStructureStrategy: OutputStructureStrategy.PreserveHierarchy,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: null,
			SourceDetails: null
		);

		string result = _resolver.Resolve(request);

		Assert.Equal("D:\\Backup\\photo.jpg", result);
	}

	[Fact]
	public void Resolve_Flat()
	{
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: "vacation\\2026",
			FileName: "photo.jpg",
			OutputStructureStrategy: OutputStructureStrategy.Flat,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: null,
			SourceDetails: null
		);

		string result = _resolver.Resolve(request);

		Assert.Equal("D:\\Backup\\photo.jpg", result);
	}

	[Fact]
	public void Resolve_CustomPathPattern_UsesFormatter()
	{
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: null,
			FileName: "myphoto.jpg",
			OutputStructureStrategy: OutputStructureStrategy.CustomPathPattern,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: "abcdef123456",
			CustomPattern: "{fileName}_{hashShort}.{ext}",
			SourceDetails: null
		);

		string result = _resolver.Resolve(request);

		Assert.Equal("D:\\Backup\\myphoto_abcdef.jpg", result);
	}

	[Fact]
	public void Resolve_CustomPathPattern_WithoutPattern_Throws()
	{
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: null,
			FileName: "f.jpg",
			OutputStructureStrategy: OutputStructureStrategy.CustomPathPattern,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: null,
			SourceDetails: null
		);

		Assert.Throws<InvalidOperationException>(() => _resolver.Resolve(request));
	}

	[Fact]
	public void Resolve_CustomPathPattern_EmptyPattern_Throws()
	{
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: null,
			FileName: "f.jpg",
			OutputStructureStrategy: OutputStructureStrategy.CustomPathPattern,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: "   ",
			SourceDetails: null
		);

		Assert.Throws<InvalidOperationException>(() => _resolver.Resolve(request));
	}

	[Fact]
	public void Resolve_PreserveHierarchy_RelativeDirOnlySlashes_Trims()
	{
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: "///",
			FileName: "f.jpg",
			OutputStructureStrategy: OutputStructureStrategy.PreserveHierarchy,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: null,
			SourceDetails: null
		);

		string result = _resolver.Resolve(request);

		Assert.Equal("D:\\Backup\\f.jpg", result);
	}

	[Fact]
	public void Resolve_UnknownStrategy_Throws()
	{
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: null,
			FileName: "f.jpg",
			OutputStructureStrategy: (OutputStructureStrategy)999,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: null,
			SourceDetails: null
		);

		Assert.Throws<ArgumentOutOfRangeException>(() => _resolver.Resolve(request));
	}

	[Fact]
	public void Resolve_CustomPathPattern_WithDeviceName()
	{
		TargetPathResolver realResolver = new(
			new FakeMessageFormatter(),
			new FileFormatValuesFactory());
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: null,
			FileName: "photo.jpg",
			OutputStructureStrategy: OutputStructureStrategy.CustomPathPattern,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: "{deviceName}/{fileName}.{ext}",
			SourceDetails: TestDevice
		);

		string result = realResolver.Resolve(request);

		Assert.Equal("D:\\Backup\\MyPhone\\photo.jpg", result);
	}

	[Fact]
	public void Resolve_CustomPathPattern_WithoutDeviceData_LeavesTokenUnformatted()
	{
		TargetPathResolver realResolver = new(
			new FakeMessageFormatter(),
			new FileFormatValuesFactory());
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: null,
			FileName: "photo.jpg",
			OutputStructureStrategy: OutputStructureStrategy.CustomPathPattern,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: "{deviceName}/{fileName}.{ext}",
			SourceDetails: null
		);

		string result = realResolver.Resolve(request);

		Assert.Equal("D:\\Backup\\{deviceName}\\photo.jpg", result);
	}
}
