using BMTP3.Core2.BackupNew2.Models.Configuration;
using BMTP3.Core2.BackupNew2.Models.Configuration.Enums;

namespace BMTP3.Core2.BackupNew2.Configs
{
	public static class SourceConfigExtensions
	{
		public static bool HasFilePattern(this ISourceConfig sourceConfig)
		{
			if(sourceConfig.UseFilePattern == null || sourceConfig.UseFilePattern == false)
			{
				return false;
			}
			return !string.IsNullOrWhiteSpace(sourceConfig.FilePattern) && !string.IsNullOrEmpty(sourceConfig.FilePatternIfExist);
		}
		public static SourceType GetSourceConfigType(this ISourceConfig sourceConfig)
		{
			if(sourceConfig is DeviceSourceConfig)
			{
				return SourceType.MediaDevice;
			}
			if(sourceConfig is DriveSourceConfig)
			{
				return SourceType.FileSystem;
			}
			// Handle the case when the type is neither DeviceSourceConfig nor DriveSourceConfig
			throw new InvalidOperationException("Invalid source configuration type");
		}
	}
}
