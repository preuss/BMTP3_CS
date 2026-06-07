using BMTP3.Core4.Storage;

namespace BMTP3.Core4.DriveDiscovery;

internal interface IDriveProvider
{
	IReadOnlyList<IBackupDriveInfo> ListDrives();
}
