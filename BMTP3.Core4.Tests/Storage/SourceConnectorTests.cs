using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Storage;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Tests.Storage;

public class SourceConnectorTests
{
	[SupportedOSPlatform("windows7.0")]
	[Fact]
	public void Connect_FileSystemDrive_ReturnsConnectedFileSystemSource()
	{
		SourceConnector connector = new();
		FakeFileSystemDriveInfo drive = new("C:\\");

		IConnectedSource source = connector.Connect(drive);

		Assert.IsType<ConnectedFileSystemSource>(source);
	}

	[SupportedOSPlatform("windows7.0")]
	[Fact]
	public void Connect_NullDrive_Throws()
	{
		SourceConnector connector = new();
		Assert.Throws<ArgumentNullException>(() => connector.Connect(null!));
	}

	[SupportedOSPlatform("windows7.0")]
	[Fact]
	public void Connect_UnknownDriveType_Throws()
	{
		SourceConnector connector = new();
		FakeUnknownDriveInfo drive = new();

		Assert.Throws<NotSupportedException>(() => connector.Connect(drive));
	}

	[SupportedOSPlatform("windows7.0")]
	[Fact]
	public void ConnectedFileSystemSource_Name_ReturnsDriveName()
	{
		SourceConnector connector = new();
		FakeFileSystemDriveInfo drive = new("C:\\");

		IConnectedFileSystemSource source = (IConnectedFileSystemSource)connector.Connect(drive);

		Assert.Equal("C:\\", source.Name);
	}

	private sealed class FakeFileSystemDriveInfo : IBackupFileSystemDriveInfo
	{
		public FakeFileSystemDriveInfo(string rootPath)
		{
			RootPath = rootPath;
			DriveName = rootPath.TrimEnd('\\');
			Id = rootPath;
			DisplayName = rootPath;
		}
		public string Id { get; }
		public string DriveName { get; }
		public string DisplayName { get; }
		public BackupSourceType SourceType => BackupSourceType.FileSystem;
		public string RootPath { get; }
		public long TotalSize => 1_000_000_000_000;
		public long AvailableFreeSpace => 500_000_000_000;
		public string VolumeLabel => DriveName;
		public string DriveFormat => "NTFS";
		public DriveType DriveType => DriveType.Fixed;
	}

	private sealed class FakeUnknownDriveInfo : IBackupDriveInfo
	{
		public string Id => "unknown";
		public string DriveName => "unknown";
		public string DisplayName => "unknown";
		public BackupSourceType SourceType => (BackupSourceType)999;
		public string RootPath => "Z:\\";
		public long TotalSize => 0;
		public long AvailableFreeSpace => 0;
	}
}
