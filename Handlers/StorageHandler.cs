using BMTP3_CS.BackupSource.PortableDevices;
using BMTP3_CS.Configs;
using BMTP3_CS.Services;
using MediaDevices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Handlers {
	[SupportedOSPlatform("windows7.0")]
	internal class StorageHandler {
		private IMediaDeviceService _mediaDeviceService;
		private CancellationTokenGenerator _cancellationTokenGenerator;

		public StorageHandler(IMediaDeviceService mediaDeviceService, CancellationTokenGenerator cancellationTokenGenerator) {
			this._mediaDeviceService = mediaDeviceService;
			this._cancellationTokenGenerator = cancellationTokenGenerator;
		}
		public IEnumerable<MediaDevice> GetMediaDevices() {
			return this._mediaDeviceService.GetDevices();
		}

		public IEnumerable<MediaDevice> GetPrivateDevices() {
			return this._mediaDeviceService.GetPrivateDevices();
		}

		public IList<ConfigDevicePair> GetConfiguredDevices(IEnumerable<MediaDevice> mediaDevices, IList<DeviceSourceConfig> enabledDeviceSourceConfigs) {
			return mediaDevices.Join(
				enabledDeviceSourceConfigs,
				mediaDevice => mediaDevice.FriendlyName,
				config => config.Name,
				(mediaDevice, config) => new ConfigDevicePair(mediaDevice, config)
			).ToList();
		}
	}
}
