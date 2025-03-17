using BMTP3.Core.BackupSource.Drives;
using BMTP3.Core.BackupSource.PortableDevices;
using BMTP3.Core.BackupSource;
using BMTP3.Core.Configs;
using MediaDevices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Handlers {
	public interface IPrintHandler {
		void PrintDisabledSources(IList<ISourceConfig> sourceConfigs, SourceType sourceType);
		void PrintEnabledSources(IList<ISourceConfig> sourceConfigs, SourceType sourceType);
		void PrintDisabledDeviceSources(IList<DeviceSourceConfig> disabledDeviceSourceConfigs);
		void PrintEnabledDeviceSources(IList<DeviceSourceConfig> enabledDeviceSourceConfigs);
		void PrintDisabledDriveSources(IList<DriveSourceConfig> disabledDriveSourceConfigs);
		void PrintEnabledDriveSources(IList<DriveSourceConfig> enabledDriveSourceConfigs);
		void PrintSources(IList<ISourceConfig> sourceConfigs, SourceType sourceType, bool isEnabled);
		void PrintDeviceDetails(IEnumerable<MediaDevice> mediaDevices);
		void PrintDriveDetails(IEnumerable<DriveInfo> driveInfos);
		void PrintFoundDevicesAndConfig(IList<ConfigDevicePair> foundDevices);
		void PrintFoundDrivesAndConfig(IList<ConfigDrivePair> foundDevices);
	}
}
