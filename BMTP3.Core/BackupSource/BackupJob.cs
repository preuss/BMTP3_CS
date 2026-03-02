using BMTP3.Core.Configs;

namespace BMTP3.Core.BackupSource {
	public abstract class BackupJob {
		public ISourceConfig SourceConfig { get; }

		protected BackupJob(ISourceConfig sourceConfig) {
			SourceConfig = sourceConfig;
		}
	}
}
