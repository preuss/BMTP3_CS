using BMTP3.Core.Configs;
using MediaDevices;
using System.Diagnostics;

namespace BMTP3.Core.BackupSource.PortableDevices {
	[DebuggerDisplay("MediaDevice: {MediaDevice.FriendlyName}, DeviceSourceConfig:{DeviceSourceConfig.Title}")]
	public class DeviceBackupJob : BackupJob {
		public DeviceBackupJob(MediaDevice mediaDevice, DeviceSourceConfig deviceSourceConfig) : base(deviceSourceConfig) {
			MediaDevice = mediaDevice;
		}
		public MediaDevice MediaDevice { get; }
		public DeviceSourceConfig DeviceSourceConfig => (DeviceSourceConfig)SourceConfig;
	}
}
