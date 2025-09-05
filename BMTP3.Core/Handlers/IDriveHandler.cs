using BMTP3.Core.BackupSource.Drives;
using BMTP3.Core.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Handlers {
	public interface IDriveHandler {
		IEnumerable<DriveInfo> GetDriveInfos();
		IList<DriveBackupJob> GetConfiguredDrives(IEnumerable<DriveInfo> driveInfos, IList<DriveSourceConfig> enabledDriveSourceConfigs);

	}
}
