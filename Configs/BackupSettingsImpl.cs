using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Configs {
	internal class BackupSettingsImpl : IBackupSettings {
		public FileInfo? BackupConfigFile { get; init; }
		public FileInfo? VerifyConfigFile { get; init; }
		public DirectoryInfo? VerifyPath { get; init; }
		public DefaultSourceConfig? DefaultSource { get; init; }
		public IList<ISourceConfig>? Sources { get; init; }
		public BackupSettingsImpl() { }
		public BackupSettingsImpl(FileInfo? backupConfigFile, FileInfo? VerifyConfigFile, DirectoryInfo? verifyPath, DefaultSourceConfig? defaultSource, IList<DeviceSourceConfig>? deviceSources) {
			this.BackupConfigFile = backupConfigFile;
			this.VerifyConfigFile = VerifyConfigFile;
			this.VerifyPath = verifyPath;
			this.DefaultSource = defaultSource;
			this.Sources = new List<ISourceConfig>();
			foreach(var deviceSource in deviceSources?.ToImmutableList() ?? ImmutableList<DeviceSourceConfig>.Empty) {
				this.Sources.Add(deviceSource);
			}
		}
		public IList<ISourceConfig> GetAllDeviceSources() => Sources ?? ImmutableList<ISourceConfig>.Empty;
	}
}
