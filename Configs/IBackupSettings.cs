using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Configs {
	public interface IBackupSettings {
		public FileInfo? BackupConfigFile { get; }
		public FileInfo? VerifyConfigFile { get; }
		public DirectoryInfo? VerifyPath { get; }
		public DefaultSourceConfig? DefaultSource { get; }
		public IList<ISourceConfig>? Sources { get; }

		public IList<ISourceConfig> GetEffectiveSources() {
			List<ISourceConfig> effectiveDeviceSources = new();

			DefaultSourceConfig defaultSourceConfig = DefaultSource ?? new DefaultSourceConfig();
			foreach(ISourceConfig source in GetAllDeviceSources()) {
				bool enabled = defaultSourceConfig.Disabled ? false : source.Enabled;
				bool? compareByBinary = source.CompareByBinary ?? defaultSourceConfig.CompareByBinary;
				string? title = source.Title;
				string? name = source.Name;
				string? folderSource = source.FolderSource;
				bool recursive = false == defaultSourceConfig.Recursive ? false : source.Recursive;
				string? folderOutput = source.FolderOutput;
				bool? useFilePattern = source.UseFilePattern ?? defaultSourceConfig.UseFilePattern;
				string? filePattern = string.IsNullOrWhiteSpace(source.FilePattern) ? defaultSourceConfig.FilePattern : source.FilePattern;
				string? filePatternIfExist = string.IsNullOrWhiteSpace(source.FilePatternIfExist) ? defaultSourceConfig.FilePatternIfExist : source.FilePatternIfExist;
				ISourceConfig effectiveSource;
				if(source is DeviceSourceConfig) {
					effectiveSource = new DeviceSourceConfig() {
						Enabled = enabled,
						CompareByBinary = compareByBinary,
						Title = title,
						Name = name,
						FolderSource = folderSource,
						Recursive = recursive,
						FolderOutput = folderOutput,
						UseFilePattern = useFilePattern,
						FilePattern = filePattern,
						FilePatternIfExist = filePatternIfExist
					};
				} else if(source is DriveSourceConfig) {
					effectiveSource = new DriveSourceConfig() {
						Enabled = enabled,
						CompareByBinary = compareByBinary,
						Title = title,
						Name = name,
						FolderSource = folderSource,
						Recursive = recursive,
						FolderOutput = folderOutput,
						UseFilePattern = useFilePattern,
						FilePattern = filePattern,
						FilePatternIfExist = filePatternIfExist
					};
				} else {
					throw new Exception("Unhandled source type");
				}
				effectiveDeviceSources.Add(effectiveSource);
			}
			return effectiveDeviceSources;
		}

		public IList<ISourceConfig> GetAllDeviceSources();

		public IList<ISourceConfig> GetDisabledSources() => GetEffectiveSources().Where(d => !d.Enabled).ToList();
		public IList<ISourceConfig> GetEnabledSources() => GetEffectiveSources().Where(d => d.Enabled).ToList();
	}
}
