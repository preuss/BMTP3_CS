namespace BMTP3.Core.Exceptions {
	internal class BackupCanceledException : OperationCanceledException {
		public string Operation { get; }
		public BackupCanceledException(string operation, CancellationToken token)
			: base("Backup operation was canceled.", token) {
			Operation = operation;
		}
	}
}
