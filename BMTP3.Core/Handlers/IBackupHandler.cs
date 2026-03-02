using BMTP3.Core.Configs;
using MediaDevices;

namespace BMTP3.Core.Handlers {
	public interface IBackupHandler {
		void PerformBackup(MediaDevice device, DeviceSourceConfig config, DateTime backupStartDateTime);
		void BackupDrive(DriveInfo drive, DriveSourceConfig config, DateTime backupStartDateTime);
		string DetermineDeviceRootName(MediaDevice mediaDevice);
	}
}
