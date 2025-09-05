using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BMTP3.Core.Configs;
using MediaDevices;

namespace BMTP3.Core.BackupSource.PortableDevices {
	[DebuggerDisplay("MediaDevice: {MediaDevice.FriendlyName}, DeviceSourceConfig:{DeviceSourceConfig.Title}")]
	public class DeviceBackupJob : BackupJob{
		public DeviceBackupJob(MediaDevice mediaDevice, DeviceSourceConfig deviceSourceConfig) :base(deviceSourceConfig) {
			MediaDevice = mediaDevice;
		}
		public MediaDevice MediaDevice { get; }
		public DeviceSourceConfig DeviceSourceConfig => (DeviceSourceConfig)SourceConfig;
	}
}
