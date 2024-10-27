using BMTP3.Core.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
