using BMTP3.Core;
using BMTP3.Core.BackupSource.PortableDevices;
using BMTP3.Core.Configs;
using BMTP3.Core.Services;
using MediaDevices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Handlers {
	[SupportedOSPlatform("windows7.0")]
	internal class StorageHandler {
		private IMediaDeviceService _mediaDeviceService;
		private CancellationTokenGenerator _cancellationTokenGenerator;

		public StorageHandler(IMediaDeviceService mediaDeviceService, CancellationTokenGenerator cancellationTokenGenerator) {
			_mediaDeviceService = mediaDeviceService;
			_cancellationTokenGenerator = cancellationTokenGenerator;
		}
		public IEnumerable<MediaDevice> GetMediaDevices() {
			return _mediaDeviceService.GetDevices();
		}

		public IEnumerable<MediaDevice> GetPrivateDevices() {
			return _mediaDeviceService.GetPrivateDevices();
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
