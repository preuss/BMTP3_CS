using BMTP3.Core.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.BackupSource {
	public abstract class BackupJob {
		public ISourceConfig SourceConfig { get; }

		protected BackupJob(ISourceConfig sourceConfig) {
			SourceConfig = sourceConfig;
		}
	}
}
