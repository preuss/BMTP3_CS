using BMTP3.Core.Configs;
using BMTP3.Core.StringVariableSubstitution;
using MediaDevices;
using MediaDevices.Progress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Handlers.Backup {
	public interface INewBackupHandler {
		void PerformBackup(DateTime backupStartDateTime);
	}
}
