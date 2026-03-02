using BMTP3.Core.BackupSource;

namespace BMTP3.Core.Configs {
	public class DriveSourceConfig : BaseSourceConfig {
		public override SourceType SourceType { get; } = SourceType.Drive;
	}
}
