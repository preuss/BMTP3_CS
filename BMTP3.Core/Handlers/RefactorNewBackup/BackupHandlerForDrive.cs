namespace BMTP3.Core.Handlers.RefactorNewBackup {
	internal class BackupHandlerForDrive : AbstractBackupHandler {
		public DriveInfo Source { get; init; }

		public BackupHandlerForDrive(DriveInfo source) {
			Source = source;
		}
		public override void PerformBackup(DateTime backupStartDateTime) {
			throw new NotImplementedException();
		}
	}
}
