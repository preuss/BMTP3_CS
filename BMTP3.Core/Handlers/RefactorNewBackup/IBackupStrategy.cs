using MediaDevices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Handlers.RefactorNewBackup {
	interface IBackupStrategy {
		INewBackupHandler GetBackupHandler(MediaDevice devices);
		INewBackupHandler GetBackupHandler(DriveInfo driveInfo);
	}
}
