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
	public class ConfigDevicePair {
		public ConfigDevicePair(MediaDevice mediaDevice, DeviceSourceConfig deviceSourceConfig) {
			MediaDevice = mediaDevice;
			DeviceSourceConfig = deviceSourceConfig;
		}
		public MediaDevice MediaDevice { get; set; }
		public DeviceSourceConfig DeviceSourceConfig { get; set; }
	}
}
