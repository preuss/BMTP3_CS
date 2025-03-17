using BMTP3.Core.Configs;
using BMTP3.Core.StringVariableSubstitution;
using MediaDevices;
using MediaDevices.Progress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Handlers {
	public interface IBackupHandler {
		void PerformBackup(MediaDevice device, DeviceSourceConfig config, DateTime backupStartDateTime);
		void BackupDrive(DriveInfo drive, DriveSourceConfig config, DateTime backupStartDateTime);
		string DetermineDeviceRootName(MediaDevice mediaDevice);
	}
}
