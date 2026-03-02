namespace BMTP3.Core.Exceptions {
	internal class BackupConfigFileNotFoundException : Exception {
		public BackupConfigFileNotFoundException(string message) : base(message) {
		}
		public BackupConfigFileNotFoundException(string message, Exception inner) : base(message, inner) {
		}
	}
}
