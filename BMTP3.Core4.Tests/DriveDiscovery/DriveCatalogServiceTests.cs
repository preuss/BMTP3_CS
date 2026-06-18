using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.DriveDiscovery;
using BMTP3.Core4.Storage;

namespace BMTP3.Core4.Tests.DriveDiscovery;

public class DriveCatalogServiceTests
{
	[Fact]
	public void ListDrives_WithDrives_ReturnsCatalogEntries()
	{
		FakeDriveProvider provider = new(new[]
		{
			new FakeDriveInfo("fs-1", "C:\\", BackupSourceType.FileSystem, 500_000_000_000, 100_000_000_000, "C"),
			new FakeDriveInfo("mtp-1", "D:\\", BackupSourceType.MediaDevice, 1_000_000_000_000, 500_000_000_000, "Phone"),
		});

		DriveCatalogService service = new(provider);
		IReadOnlyList<DriveCatalogEntry> entries = service.ListDrives();

		Assert.Equal(2, entries.Count);
		Assert.Equal("fs-1", entries[0].Id);
		Assert.Equal("C", entries[0].Name);
		Assert.Equal(BackupSourceType.FileSystem, entries[0].SourceType);
		Assert.Equal("mtp-1", entries[1].Id);
		Assert.Equal("Phone", entries[1].Name);
		Assert.Equal(BackupSourceType.MediaDevice, entries[1].SourceType);
	}

	[Fact]
	public void ListDrives_Empty_ReturnsEmpty()
	{
		FakeDriveProvider provider = new(Array.Empty<IBackupDriveInfo>());
		DriveCatalogService service = new(provider);

		Assert.Empty(service.ListDrives());
	}

	[Fact]
	public void ListDrives_NullProvider_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => new DriveCatalogService(null!));
	}

	private sealed class FakeDriveProvider : IDriveProvider
	{
		private readonly IReadOnlyList<IBackupDriveInfo> _drives;
		public FakeDriveProvider(IReadOnlyList<IBackupDriveInfo> drives) => _drives = drives;
		public IReadOnlyList<IBackupDriveInfo> ListDrives() => _drives;
	}

	private sealed class FakeDriveInfo : IBackupDriveInfo
	{
		public FakeDriveInfo(string id, string rootPath, BackupSourceType sourceType, long totalSize, long availableFreeSpace, string displayName)
		{
			Id = id;
			RootPath = rootPath;
			SourceType = sourceType;
			TotalSize = totalSize;
			AvailableFreeSpace = availableFreeSpace;
			DisplayName = displayName;
			DriveName = displayName;
		}
		public string Id { get; }
		public string DriveName { get; }
		public string DisplayName { get; }
		public BackupSourceType SourceType { get; }
		public string RootPath { get; }
		public long TotalSize { get; }
		public long AvailableFreeSpace { get; }
	}
}
