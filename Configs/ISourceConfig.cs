using BMTP3_CS.BackupSource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Configs {
	public interface ISourceConfig : IEquatable<ISourceConfig> {
		/// <summary>
		/// Default should be false
		/// </summary>
		bool Enabled { get; set; }
		bool? CompareByBinary { get; set; }
		string? Title { get; set; }
		string? Name { get; set; }
		string? FolderSource { get; set; }
		/// <summary>
		/// Default should be false
		/// </summary>
		bool Recursive { get; set; }
		string? FolderOutput { get; set; }
		bool? UseFilePattern { get; set; }
		string? FilePattern { get; }
		string? FilePatternIfExist { get; }
		SourceType SourceType { get; }
	}
}
