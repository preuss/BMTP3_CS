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
	public class ConfigDrivePair {
		public ConfigDrivePair(DriveInfo driveInfo, DriveSourceConfig driveSourceConfig) {
			DriveInfo = driveInfo;
			DriveSourceConfig = driveSourceConfig;
		}
		public DriveInfo DriveInfo { get; set; }
		public DriveSourceConfig DriveSourceConfig { get; set; }
	}
}
