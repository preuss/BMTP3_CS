using BMTP3.Core.BackupSource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Configs {
	public static class SourceConfigExtensions {
		public static bool HasFilePattern(this ISourceConfig sourceConfig) {
			if(sourceConfig.UseFilePattern == null || sourceConfig.UseFilePattern == false) {
				return false;
			}
			return !string.IsNullOrWhiteSpace(sourceConfig.FilePattern) && !string.IsNullOrEmpty(sourceConfig.FilePatternIfExist);
		}
		public static SourceType GetSourceConfigType(this ISourceConfig sourceConfig) {
			if(sourceConfig is DeviceSourceConfig) {
				return SourceType.Device;
			}
			if(sourceConfig is DriveSourceConfig) {
				return SourceType.Drive;
			}
			// Handle the case when the type is neither DeviceSourceConfig nor DriveSourceConfig
			throw new InvalidOperationException("Invalid source configuration type");
		}
	}
}
