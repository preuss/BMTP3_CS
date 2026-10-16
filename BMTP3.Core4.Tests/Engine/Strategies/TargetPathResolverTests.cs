using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Strategies;
using BMTP3.Core4.Models;
using BMTP3.Core4.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BMTP3.Core4.Tests.Engine.Strategies;

public class TargetPathResolverTests
{
	private static readonly DateTimeOffset TestDate = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
	private readonly TargetPathResolver _resolver = new(
		new FakeMessageFormatter(),
		new FakeFileFormatValuesFactory(),
		NullLogger<TargetPathResolver>.Instance
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
			new FileFormatValuesFactory(),
			NullLogger<TargetPathResolver>.Instance
		);
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
			new FileFormatValuesFactory(),
			NullLogger<TargetPathResolver>.Instance
		);
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

	private const string RawMtpFileName =
		"HilinkSvc_null_evt_17.0.4.313_2844af23b5bc39a0f4961aab67d8ddf*c4bc558b533201209b84f4415ee7b3_@1789845320822.log";

	private const string SanitizedMtpFileName =
		"HilinkSvc_null_evt_17.0.4.313_2844af23b5bc39a0f4961aab67d8ddf_c4bc558b533201209b84f4415ee7b3_@1789845320822.log";

	[Fact]
	public void Resolve_PreserveHierarchy_FileNameWithInvalidChars_Sanitized()
	{
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: @"Android\data\com.huawei.hilink.framework\files\Log",
			FileName: RawMtpFileName,
			OutputStructureStrategy: OutputStructureStrategy.PreserveHierarchy,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: null,
			SourceDetails: null
		);

		string result = _resolver.Resolve(request);

		// '*' is illegal on NTFS and must become '_'. '@' is legal and must survive.
		Assert.Equal(
			@"D:\Backup\Android\data\com.huawei.hilink.framework\files\Log\" + SanitizedMtpFileName,
			result);
	}

	[Fact]
	public void Resolve_Flat_FileNameWithInvalidChars_Sanitized()
	{
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: @"vacation\2026",
			FileName: "photo*.jpg",
			OutputStructureStrategy: OutputStructureStrategy.Flat,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: null,
			SourceDetails: null
		);

		string result = _resolver.Resolve(request);

		Assert.Equal(@"D:\Backup\photo_.jpg", result);
	}

	[Fact]
	public void Resolve_PreserveHierarchy_RelativeDirWithInvalidChars_Sanitized()
	{
		// Source-derived relative directory: every illegal character is replaced.
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: @"vacation*2026\day?.jpg",
			FileName: "photo.jpg",
			OutputStructureStrategy: OutputStructureStrategy.PreserveHierarchy,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: null,
			SourceDetails: null
		);

		string result = _resolver.Resolve(request);

		Assert.Equal(@"D:\Backup\vacation_2026\day_.jpg\photo.jpg", result);
	}

	[Fact]
	public void Resolve_CustomPathPattern_OutputWithInvalidChars_Sanitized()
	{
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: null,
			FileName: "photo.jpg",
			OutputStructureStrategy: OutputStructureStrategy.CustomPathPattern,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: "abcdef123456",
			CustomPattern: "x*y/{fileName}",
			SourceDetails: null
		);

		string result = _resolver.Resolve(request);

		Assert.Equal(@"D:\Backup\x_y\photo", result);
	}

	[Fact]
	public void Resolve_CustomPathPattern_SourceFileNameWithColon_Sanitized()
	{
		// A source file name may legally contain ':' (e.g. on MTP). The custom pattern output is
		// sanitized before it is normalized, so the colon never reaches path validation.
		TargetPathResolver realResolver = new(
			new FakeMessageFormatter(),
			new FileFormatValuesFactory(),
			NullLogger<TargetPathResolver>.Instance
		);
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: null,
			FileName: "IMG:1.jpg",
			OutputStructureStrategy: OutputStructureStrategy.CustomPathPattern,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: "{fileName}.{ext}",
			SourceDetails: null
		);

		string result = realResolver.Resolve(request);

		Assert.Equal("D:\\Backup\\IMG_1.jpg", result);
	}

	[Fact]
	public void Resolve_CustomPathPattern_PatternWithColon_Sanitized()
	{
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: null,
			FileName: "photo.jpg",
			OutputStructureStrategy: OutputStructureStrategy.CustomPathPattern,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: "a:b/{fileName}",
			SourceDetails: null
		);

		string result = _resolver.Resolve(request);

		Assert.Equal("D:\\Backup\\a_b\\photo", result);
	}

	[Fact]
	public void Resolve_DestinationRoot_DriveColonPreserved()
	{
		// The user-supplied root must never be sanitized: its drive colon is legal there.
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: null,
			FileName: "f*.jpg",
			OutputStructureStrategy: OutputStructureStrategy.PreserveHierarchy,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: null,
			SourceDetails: null
		);

		string result = _resolver.Resolve(request);

		Assert.Equal(@"D:\Backup\f_.jpg", result);
	}

	[Fact]
	public void Resolve_PreserveHierarchy_RelativeDirWithColon_Sanitized()
	{
		// The relative directory is source-derived: it is sanitized, never validated.
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: "weird:folder",
			FileName: "photo.jpg",
			OutputStructureStrategy: OutputStructureStrategy.PreserveHierarchy,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: null,
			SourceDetails: null
		);

		string result = _resolver.Resolve(request);

		Assert.Equal(@"D:\Backup\weird_folder\photo.jpg", result);
	}

	[Fact]
	public void Resolve_FileNameSanitized_LogsDiagnostic()
	{
		CapturingLogger<TargetPathResolver> logger = new();
		TargetPathResolver resolver = new(new FakeMessageFormatter(), new FakeFileFormatValuesFactory(), logger);
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: @"vacation\2026",
			FileName: "photo*.jpg",
			OutputStructureStrategy: OutputStructureStrategy.PreserveHierarchy,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: null,
			SourceDetails: null
		);

		resolver.Resolve(request);

		Assert.Contains(logger.Messages, message =>
			message.Contains("photo*.jpg", StringComparison.Ordinal) &&
			message.Contains("photo_.jpg", StringComparison.Ordinal));
	}

	[Fact]
	public void Resolve_NothingSanitized_LogsNothing()
	{
		CapturingLogger<TargetPathResolver> logger = new();
		TargetPathResolver resolver = new(new FakeMessageFormatter(), new FakeFileFormatValuesFactory(), logger);
		TargetPathResolveRequest request = new(
			DestinationRoot: "D:\\Backup",
			RelativeDirectoryPath: @"vacation\2026",
			FileName: "photo.jpg",
			OutputStructureStrategy: OutputStructureStrategy.PreserveHierarchy,
			CreateFileDate: TestDate,
			ItemId: "abc",
			StrongHash: null,
			CustomPattern: null,
			SourceDetails: null
		);

		resolver.Resolve(request);

		Assert.Empty(logger.Messages);
	}

	private sealed class CapturingLogger<T> : ILogger<T>
	{
		public List<string> Messages { get; } = new();

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			Messages.Add(formatter(state, exception));
		}
	}
}
