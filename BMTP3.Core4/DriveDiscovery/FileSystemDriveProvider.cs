using BMTP3.Core4.Storage;

namespace BMTP3.Core4.DriveDiscovery;

internal sealed class FileSystemDriveProvider : IDriveProvider
{
	public IReadOnlyList<IBackupDriveInfo> ListDrives()
	{
		return DriveInfo.GetDrives()
			.Where(static d => d.IsReady)
			.Select(static d => new BackupFileSystemDriveInfo(d))
			.ToList();
	}
}
