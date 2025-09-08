using BMTP3.Core.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
