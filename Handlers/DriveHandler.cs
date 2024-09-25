using BMTP3_CS.BackupSource.Drives;
using BMTP3_CS.Configs;
using MediaDevices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Handlers {
	internal class DriveHandler {
		private CancellationTokenGenerator CancellationTokenGenerator { get; }
		public DriveHandler(CancellationTokenGenerator cancellationTokenGenerator) {
			CancellationTokenGenerator = cancellationTokenGenerator;
		}
		public IEnumerable<DriveInfo> GetDriveInfos() {
			return DriveInfo.GetDrives()
				.Where(driveInfo => driveInfo.IsReady)
				.ToList();
		}
		public IList<ConfigDrivePair> GetConfiguredDrives(IEnumerable<DriveInfo> driveInfos, IList<DriveSourceConfig> enabledDriveSourceConfigs) {
			var validConfigs = enabledDriveSourceConfigs.Where(config => config.Name != null).ToList();

			var volumeLabelMatches = MatchDrives(
				driveInfos,
				validConfigs,
				driveInfo => driveInfo.VolumeLabel,
				config => config.Name!
			);

			var remainingConfigs = validConfigs.Except(volumeLabelMatches.Select(pair => pair.DriveSourceConfig)).ToList();

			var driveNameMatches = MatchDrives(
				driveInfos, 
				remainingConfigs, 
				driveInfo => driveInfo.Name.TrimEnd('\\'), 
				config => config.Name!.EndsWith('\\') ? config.Name.TrimEnd('\\').ToUpper() : config.Name.ToUpper()
			);

			return volumeLabelMatches.Concat(driveNameMatches).ToList();
		}
		private List<ConfigDrivePair> MatchDrives(IEnumerable<DriveInfo> driveInfos, IList<DriveSourceConfig> configs, Func<DriveInfo, string> driveKeySelector, Func<DriveSourceConfig, string> configKeySelector) {
			return driveInfos.GroupJoin(
				configs,
				driveKeySelector,
				configKeySelector,
				(driveInfo, matchedConfigs) => new { driveInfo, matchedConfigs }
			).SelectMany(
				group => group.matchedConfigs.Where(config => config != null),
				(group, config) => new ConfigDrivePair(group.driveInfo, config)
			)
			.ToList();
		}
	}
}
