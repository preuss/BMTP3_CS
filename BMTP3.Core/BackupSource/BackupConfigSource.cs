using BMTP3.Core.Configs;

namespace BMTP3.Core.BackupSource {
	public class BackupConfigSource {
		public ISourceConfig Source { get; }
		public SourceType SourceType { get; }
		public BackupConfigSource(ISourceConfig source, SourceType sourceType) {
			Source = source;
			SourceType = sourceType;
		}
	}
}
