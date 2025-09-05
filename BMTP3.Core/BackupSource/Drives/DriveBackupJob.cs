using BMTP3.Core.Configs;
using MediaDevices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.BackupSource.Drives {
	[DebuggerDisplay("DriveInfo: {DriveInfo.Name}, DriveSourceConfig:{DriveSourceConfig.Title}")]
	public class DriveBackupJob : BackupJob{
		public DriveBackupJob(DriveInfo driveInfo, DriveSourceConfig driveSourceConfig): base(driveSourceConfig) {
			DriveInfo = driveInfo;
		}
		public DriveInfo DriveInfo { get; }
		public DriveSourceConfig DriveSourceConfig => (DriveSourceConfig)SourceConfig;
	}
}
