using BMTP3.Core.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Handlers {
	public class BackupRecordDataStorePathResolver {
		private readonly ISourceConfig _sourceConfig;

		public BackupRecordDataStorePathResolver(ISourceConfig sourceConfig) {
			_sourceConfig = sourceConfig;
		}

		public FileInfo Resolve() {
			string folderPath = ResolveFolderFrom(_sourceConfig);
			string fileName = ResolveFileNameFrom(_sourceConfig);
			string fullPath = Path.Combine(folderPath, fileName);

			return new FileInfo(fullPath);
		}

		private string ResolveFolderFrom(ISourceConfig config) {
			if(string.IsNullOrWhiteSpace(config.FolderOutput)) {
				throw new ArgumentException("Output folder is not set in the source configuration.");
			}
			foreach(char invalidChar in Path.GetInvalidPathChars()) {
				if(config.FolderOutput.Contains(invalidChar)) {
					throw new ArgumentException($"Output folder contains invalid character: {invalidChar}");
				}
			}
			return config.FolderOutput;
		}

		private string ResolveFileNameFrom(ISourceConfig config) {
			string fileName = $"Progress_{config.Title}_{config.Name}.json";
			foreach(char invalidChar in Path.GetInvalidFileNameChars()) {
				fileName = fileName.Replace(invalidChar.ToString(), string.Empty);
			}
			return fileName;
		}
	}
}
