using BMTP3.Core.BackupSource.PortableDevices;
using BMTP3.Core.Configs;
using MediaDevices;

namespace BMTP3.Core.Handlers {
	public interface IStorageHandler {
		IEnumerable<MediaDevice> GetMediaDevices();
		IEnumerable<MediaDevice> GetPrivateDevices();
		IList<DeviceBackupJob> GetConfiguredDevices(IEnumerable<MediaDevice> mediaDevices, IList<DeviceSourceConfig> enabledDeviceSourceConfigs);
	}
}
