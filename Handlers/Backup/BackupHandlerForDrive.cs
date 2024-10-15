using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Handlers.Backup {
	internal class BackupHandlerForDrive : AbstractBackupHandler {
		public DriveInfo Source { get; init; }

		public BackupHandlerForDrive(DriveInfo source) {
			Source = source;
		}
		public override void PerformBackup() {
			throw new NotImplementedException();
		}
	}
}
