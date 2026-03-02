using BMTP3.Core.Configs;

namespace BMTP3.Core.Handlers {
	public class BackupRecordData {
		private readonly ISourceConfig sourceConfig;
		private readonly IList<BackupRecordInfo> records;

		public BackupRecordData(ISourceConfig sourceConfig, IList<BackupRecordInfo> records) {
			this.sourceConfig = sourceConfig;
			this.records = records;
		}

		public ISourceConfig SourceConfig { get { return sourceConfig; } }
		public IList<BackupRecordInfo> Records { get { return records; } }
	}
}
