using BMTP3.Core2.BackupNew2.Models.Configuration;
using BMTP3.Core2.BackupNew2.Models.Configuration.Enums;

namespace BMTP3.Core2.BackupNew2.Configs
{
	public class DriveSourceConfig : BaseSourceConfig
	{
		public override SourceType SourceType { get; } = SourceType.FileSystem;
	}
}
