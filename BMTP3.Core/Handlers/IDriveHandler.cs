using BMTP3.Core.BackupSource.Drives;
using BMTP3.Core.Configs;

namespace BMTP3.Core.Handlers {
	public interface IDriveHandler {
		IEnumerable<DriveInfo> GetDriveInfos();
		IList<DriveBackupJob> GetConfiguredDrives(IEnumerable<DriveInfo> driveInfos, IList<DriveSourceConfig> enabledDriveSourceConfigs);

	}
}
