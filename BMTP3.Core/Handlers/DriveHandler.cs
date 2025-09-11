using BMTP3.Core.BackupSource.Drives;
using BMTP3.Core.Configs;
using BMTP3.Core.Configuration;
using Org.BouncyCastle.Math;
using System.Text.RegularExpressions;

namespace BMTP3.Core.Handlers {
	public class DriveHandler : IDriveHandler {
		private CancellationTokenGenerator CancellationTokenGenerator { get; }
		public DriveHandler(CancellationTokenGenerator cancellationTokenGenerator) {
			CancellationTokenGenerator = cancellationTokenGenerator;
		}
		public IEnumerable<DriveInfo> GetDriveInfos() {
			return DriveInfo.GetDrives()
				.Where(driveInfo => driveInfo.IsReady)
				.ToList();
		}
		public IList<DriveBackupJob> GetConfiguredDrives(IEnumerable<DriveInfo> driveInfos, IList<DriveSourceConfig> enabledDriveSourceConfigs) {
			ArgumentNullException.ThrowIfNull(driveInfos, nameof(driveInfos));
			ArgumentNullException.ThrowIfNull(enabledDriveSourceConfigs, nameof(enabledDriveSourceConfigs));

			if(enabledDriveSourceConfigs.Any(c => c is null)) {
				throw new ArgumentException("The List contains null element(s).", nameof(enabledDriveSourceConfigs));
			}
			if(enabledDriveSourceConfigs.Any(config => !config.Enabled)){
				throw new InvalidOperationException("GetConfiguredDrives should only be called with enabled DriveSourceConfigs.");
			}

			List<DriveBackupJob> jobs = new();
			if(driveInfos.Count() == 0) {
				return jobs;
			}
			if(enabledDriveSourceConfigs.Count == 0) {
				return jobs;
			}
			List<string> guardErrors = new();
			foreach(DriveSourceConfig config in enabledDriveSourceConfigs) {
				if(string.IsNullOrWhiteSpace(config.Title)) {
					guardErrors.Add($"DriveSourceConfig has missing or empty title.");
				}
				if(string.IsNullOrWhiteSpace(config.FolderSource)) {
					guardErrors.Add($"DriveSourceConfig has missing or empty folder_source.");
				}
			}
			if(guardErrors.Count > 0) {
				throw new InvalidOperationException("DriveSourceConfig validation failed: " + string.Join("; ", guardErrors));
			}

			Regex driveRootRegex = new(@"^[A-Za-z]:[\\]{0,2}", RegexOptions.Compiled);
			bool HasDriveRoot(string path) => driveRootRegex.IsMatch(path);

			// Starting the matching process
			foreach(DriveSourceConfig config in enabledDriveSourceConfigs) {
				string configTitle = config.Title!; // Guarded against null/empty above
				string configFolderSource = config.FolderSource!; // Guarded against null/empty above
				string configName = string.IsNullOrWhiteSpace(config.Name) ? string.Empty : config.Name.Trim();

				bool configFolderSourceHasDriveRoot = HasDriveRoot(configFolderSource);
				bool configNameHasDriveRoot = HasDriveRoot(configName);

				if(configFolderSourceHasDriveRoot && !string.IsNullOrEmpty(configName)) {
					if(driveRootRegex.Match(configName).Value 
						!= driveRootRegex.Match(configFolderSource).Value) {
						throw new ArgumentOutOfRangeException(configName, $"DriveSourceConfig '{configTitle}' has mismatching drive roots in 'folder_source' and 'name'.");
					}
				}
				if(!configFolderSourceHasDriveRoot && string.IsNullOrEmpty(configName) ) {
					throw new ArgumentOutOfRangeException(configName, $"DriveSourceConfig '{configTitle}' must have either a drive root in 'folder_source' or a non-empty 'name'.");
				}

				DriveInfo? matchedDrive = null;
				if(configFolderSourceHasDriveRoot) {
					string? driveRoot = Path.GetPathRoot(configFolderSource);
					if(string.IsNullOrWhiteSpace(driveRoot)) {
						throw new ArgumentOutOfRangeException(configFolderSource, $"DriveSourceConfig '{configTitle}' has invalid 'folder_source' value.");
					}
					matchedDrive = driveInfos.FirstOrDefault(driveInfo => string.Equals(driveInfo.Name, driveRoot, StringComparison.OrdinalIgnoreCase));
				} else if(configNameHasDriveRoot) {
					string? driveRoot = Path.GetPathRoot(configFolderSource);
					if(string.IsNullOrWhiteSpace(driveRoot)) {
						throw new ArgumentOutOfRangeException(configName, $"DriveSourceConfig '{configTitle}' has invalid 'name' value.");
					}
					matchedDrive = driveInfos.FirstOrDefault(driveInfo => string.Equals(driveInfo.Name, configName, StringComparison.OrdinalIgnoreCase));
				} else {
					// Both FolderSource and Name are without drive roots, match by Name (VolumeLabel or Drive Name)
					// This will be handled in the matching process below
					matchedDrive = driveInfos.FirstOrDefault(driveInfo => string.Equals(driveInfo.VolumeLabel, configName, StringComparison.OrdinalIgnoreCase));
				}
				if(matchedDrive != null) {
					jobs.Add(new DriveBackupJob(matchedDrive, config));
				}
			}
			/*
			var validConfigs = enabledDriveSourceConfigs.Where(config => config.Name != null).ToList();

			var volumeLabelMatches = MatchDrives(
				driveInfos,
				validConfigs,
				driveInfo => driveInfo.VolumeLabel,
				config => config.Name!
			);

			var remainingConfigs = validConfigs.Except(volumeLabelMatches.Select(pair => pair.DriveSourceConfig)).ToList();

			var driveNameMatches = MatchDrives(
				driveInfos,
				remainingConfigs,
				driveInfo => driveInfo.Name.TrimEnd('\\'),
				config => {
					string name = config.Name!;
					name = name.EndsWith('\\') ? name.TrimEnd('\\') : name;
					return name.ToUpperInvariant();
					//config.Name!.EndsWith('\\') ? config.Name.TrimEnd('\\').ToUpper() : config.Name.ToUpper()
				}
			);

			return volumeLabelMatches.Concat(driveNameMatches).ToList();
			*/
			return jobs;
		}
		private List<DriveBackupJob> MatchDrives(IEnumerable<DriveInfo> driveInfos, IList<DriveSourceConfig> configs, Func<DriveInfo, string> driveKeySelector, Func<DriveSourceConfig, string> configKeySelector) {
			return driveInfos.GroupJoin(
				configs,
				driveKeySelector,
				configKeySelector,
				(driveInfo, matchedConfigs) => new { driveInfo, matchedConfigs }
			).SelectMany(
				group => group.matchedConfigs.Where(config => config != null),
				(group, config) => new DriveBackupJob(group.driveInfo, config)
			)
			.ToList();
		}
	}
}
