using BMTP3.Core.Configs;
using System.Diagnostics;

namespace BMTP3.Core.BackupSource.Drives {
	[DebuggerDisplay("DriveInfo: {DriveInfo.Name}, DriveSourceConfig:{DriveSourceConfig.Title}")]
	public class DriveBackupJob : BackupJob {
		public DriveBackupJob(DriveInfo driveInfo, DriveSourceConfig driveSourceConfig) : base(driveSourceConfig) {
			DriveInfo = driveInfo;
		}
		public DriveInfo DriveInfo { get; }
		public DriveSourceConfig DriveSourceConfig => (DriveSourceConfig)SourceConfig;
	}
}
