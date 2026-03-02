using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core.Services {
	[SupportedOSPlatform("windows10.0")]
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
