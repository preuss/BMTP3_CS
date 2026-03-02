namespace BMTP3.Core.Handlers.RefactorNewBackup {
	abstract class AbstractBackupHandler : INewBackupHandler {
		public abstract void PerformBackup(DateTime backupStartDateTime);
	}
}
