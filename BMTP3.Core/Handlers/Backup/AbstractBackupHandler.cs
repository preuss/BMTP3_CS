using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Handlers.Backup {
	abstract class AbstractBackupHandler : IBackupHandler {
		public abstract void PerformBackup(DateTime backupStartDateTime);
	}
}
