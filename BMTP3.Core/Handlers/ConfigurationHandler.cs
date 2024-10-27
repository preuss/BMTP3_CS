using BMTP3.Core.Configs;
using BMTP3.Core.Options;
using Microsoft.Extensions.Configuration;
using System.Collections.Immutable;

namespace BMTP3.Core.Handlers {
	internal class ConfigurationHandler {
		public CommandOptions Arguments { get; private set; }
		public IBackupSettings? BackupSettings { get; private set; }

		public ConfigurationHandler(string[] args, IConfiguration defaultOptions, BackupSettingsReader backupSettingsReader) {
			Arguments = ArgumentParser.GetArguments(args, defaultOptions.GetSection("defaultArgs"));

			BackupSettings = backupSettingsReader.GetBackupSettingsFrom(Arguments.CommandLineArguments.Backup, Arguments.CommandLineArguments.Verify, Arguments.CommandLineArguments.VerifyPath);
		}
		public IList<ISourceConfig> GetConfigs() {
			return (BackupSettings?.GetEffectiveSources()) ?? ImmutableList<ISourceConfig>.Empty;
		}
		public IList<DeviceSourceConfig> GetDisabledDeviceConfigs() {
			return ((BackupSettings?.GetDisabledSources()) ?? ImmutableList<ISourceConfig>.Empty)
				.OfType<DeviceSourceConfig>()
				.ToList();
		}
		public IList<DeviceSourceConfig> GetEnabledDeviceConfigs() {
			return ((BackupSettings?.GetEnabledSources()) ?? ImmutableList<ISourceConfig>.Empty)
				.OfType<DeviceSourceConfig>()
				.ToList();
		}
		public IList<DriveSourceConfig> GetDisabledDriveConfigs() {
			return ((BackupSettings?.GetDisabledSources()) ?? ImmutableList<ISourceConfig>.Empty)
				.OfType<DriveSourceConfig>()
				.ToList();
		}
		public IList<DriveSourceConfig> GetEnabledDriveConfigs() {
			return ((BackupSettings?.GetEnabledSources()) ?? ImmutableList<ISourceConfig>.Empty)
				.OfType<DriveSourceConfig>()
				.ToList();
		}
		public bool VerifyBackups() {
			bool verifyBackups = false;

			if(Arguments.CommandLineArguments.VerifyPath != null && Arguments.CommandLineArguments.VerifyPath != string.Empty) {
				verifyBackups = true;
			}

			return verifyBackups;
		}
	}
}
