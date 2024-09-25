using MediaDevices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Services {
	[SupportedOSPlatform("windows7.0")]
	public class MediaDeviceServiceProd : IMediaDeviceService {
		public MediaDeviceServiceProd() {
		}
		public IEnumerable<MediaDevice> GetDevices() {
			return MediaDevice.GetDevices();
		}
		public IEnumerable<MediaDevice> GetPrivateDevices() {
			return MediaDevice.GetPrivateDevices();
		}
	}
}
