using MediaDevices;

namespace BMTP3.Core.Handlers.RefactorNewBackup {
	interface IBackupStrategy {
		INewBackupHandler GetBackupHandler(MediaDevice devices);
		INewBackupHandler GetBackupHandler(DriveInfo driveInfo);
	}
}
