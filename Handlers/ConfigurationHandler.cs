using BMTP3_CS.Configs;
using BMTP3_CS.Exceptions;
using BMTP3_CS.Options;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Handlers {
	internal class ConfigurationHandler {
		public CommandOptions Arguments { get; private set; }
		public IBackupSettings? BackupSettings { get; private set; }

		public ConfigurationHandler(string[] args, IConfiguration defaultOptions, BackupSettingsReader backupSettingsReader) {
			Arguments = ArgumentParser.GetArguments(args, defaultOptions.GetSection("defaultArgs"));

			BackupSettings = backupSettingsReader.GetBackupSettingsFrom(Arguments.CommandLineArguments.Backup, Arguments.CommandLineArguments.Verify, Arguments.CommandLineArguments.VerifyPath);
		}
		public IList<ISourceConfig> GetConfigs() {
			return ((BackupSettings?.GetEffectiveSources()) ?? ImmutableList<ISourceConfig>.Empty);
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
