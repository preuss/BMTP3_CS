using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Handlers.Backup {
	internal class BackupCanceledException : OperationCanceledException {
		public string Operation { get; }
		public BackupCanceledException(string operation, CancellationToken token)
			: base("Backup operation was canceled.", token) {
			Operation = operation;
		}
	}
}
