using BMTP3_CS.BackupSource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Configs {

	public class DriveSourceConfig : ISourceConfig {
		/// <summary>
		/// Default should be false
		/// </summary>
		public bool Enabled { get; set; } = false;
		public bool? CompareByBinary { get; set; }
		public string? Title { get; set; }
		public string? Name { get; set; }
		public string? FolderSource { get; set; }
		/// <summary>
		/// Default should be false
		/// </summary>
		public bool Recursive { get; set; } = false;
		public string? FolderOutput { get; set; }
		public bool? UseFilePattern { get; set; }
		public string? FilePattern { get; internal set; }
		public string? FilePatternIfExist { get; internal set; }
		public SourceType SourceType { get; } = SourceType.Drive;
	}
}
