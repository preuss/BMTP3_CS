using MediaDevices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Handlers.Backup {
	interface IBackupStrategy {
		IBackupHandler GetBackupHandler(MediaDevice devices);
		IBackupHandler GetBackupHandler(DriveInfo driveInfo);
	}
}
