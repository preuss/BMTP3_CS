using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BMTP3_CS.Configs;
using MediaDevices;

namespace BMTP3_CS.BackupSource.PortableDevices {
	[DebuggerDisplay("MediaDevice: {MediaDevice.FriendlyName}, DeviceSourceConfig:{DeviceSourceConfig.Title}")]
	public class ConfigDevicePair {
		public ConfigDevicePair(MediaDevice mediaDevice, DeviceSourceConfig deviceSourceConfig) {
			MediaDevice = mediaDevice;
			DeviceSourceConfig = deviceSourceConfig;
		}
		public MediaDevice MediaDevice { get; set; }
		public DeviceSourceConfig DeviceSourceConfig { get; set; }
	}
}
