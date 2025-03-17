using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Handlers.RefactorNewBackup {
	abstract class AbstractBackupHandler : INewBackupHandler {
		public abstract void PerformBackup(DateTime backupStartDateTime);
	}
}
