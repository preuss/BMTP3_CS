using BMTP3_CS.BackupSource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Configs {
	public abstract class BaseSourceConfig : ISourceConfig {
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
		public string? FilePattern { get; set; }
		public string? FilePatternIfExist { get; set; }
		public abstract SourceType SourceType { get; }
		public override bool Equals(object? obj) {
			return Equals(obj as ISourceConfig);
		}
		public bool Equals(ISourceConfig? other) {
			return other != null &&
				   Enabled == other.Enabled &&
				   CompareByBinary == other.CompareByBinary &&
				   Title == other.Title &&
				   Name == other.Name &&
				   FolderSource == other.FolderSource &&
				   Recursive == other.Recursive &&
				   FolderOutput == other.FolderOutput &&
				   UseFilePattern == other.UseFilePattern &&
				   FilePattern == other.FilePattern &&
				   FilePatternIfExist == other.FilePatternIfExist &&
				   SourceType == other.SourceType;
		}
		public override int GetHashCode() {
			var hash = new HashCode();
			hash.Add(Enabled);
			hash.Add(CompareByBinary);
			hash.Add(Title);
			hash.Add(Name);
			hash.Add(FolderSource);
			hash.Add(Recursive);
			hash.Add(FolderOutput);
			hash.Add(UseFilePattern);
			hash.Add(FilePattern);
			hash.Add(FilePatternIfExist);
			hash.Add(SourceType);
			return hash.ToHashCode();
		}
	}
}
