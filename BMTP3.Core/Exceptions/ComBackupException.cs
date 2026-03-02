namespace BMTP3.Core.Exceptions {
	public class ComBackupException : BackupException {
		public ComBackupException(string message) : base(message) { }
		public ComBackupException(string message, Exception innerException) : base(message, innerException) { }
	}
}
