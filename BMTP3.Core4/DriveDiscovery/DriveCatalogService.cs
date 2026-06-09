using BMTP3.Core4.Api;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Storage;

namespace BMTP3.Core4.DriveDiscovery;

internal sealed class DriveCatalogService : IDriveCatalogService
{
	private readonly IDriveProvider _driveProvider;

	public DriveCatalogService(IDriveProvider driveProvider)
	{
		_driveProvider = driveProvider ?? throw new ArgumentNullException(nameof(driveProvider));
	}

	public IReadOnlyList<DriveCatalogEntry> ListDrives()
	{
		IReadOnlyList<IBackupDriveInfo> drives = _driveProvider.ListDrives();

		DriveCatalogEntry[] result = new DriveCatalogEntry[drives.Count];

		for (int i = 0; i < drives.Count; i++)
		{
			IBackupDriveInfo drive = drives[i];

			string? friendlyName = null;
			string? model = null;

			if (drive is IBackupMediaDriveInfo mediaDrive)
			{
				friendlyName = mediaDrive.FriendlyName;
				model = mediaDrive.Model;
			}

			result[i] = new DriveCatalogEntry
			{
				Id = drive.Id,
				Name = drive.DisplayName,
				RootPath = drive.RootPath,
				SourceType = drive.SourceType,
				TotalSize = drive.TotalSize,
				AvailableFreeSpace = drive.AvailableFreeSpace,
				DeviceFriendlyName = friendlyName,
				DeviceModel = model,
			};
		}

		return result;
	}
}
