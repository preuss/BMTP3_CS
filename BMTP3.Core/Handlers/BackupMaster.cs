using BMTP3.Core.BackupSource;
using BMTP3.Core.BackupSource.Drives;
using BMTP3.Core.BackupSource.PortableDevices;
using BMTP3.Core.CompareFiles;
using BMTP3.Core.Configs;
using BMTP3.Core.Configuration;
using BMTP3.Core.Exceptions;
using BMTP3.Core.Handlers.Backup;
using BMTP3.Core.Services;
using MediaDevices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ZLogger;

namespace BMTP3.Core.Handlers {
	[SupportedOSPlatform("windows7.0")]
	internal class BackupMaster {
		private static readonly ILogger<BackupMaster> logger = LogManager.GetLogger<BackupMaster>();

		private readonly BackupExceptionHandlerService backupExceptionHandlerService;
		private readonly IServiceProvider serviceProvider;
		private readonly IAnsiConsole Console; // Use Spectre.Console instead of System.Console, but keep the same caseing.
		private readonly CancellationTokenGenerator tokenGenerator;

		public BackupMaster(
			IServiceProvider serviceProvider,
			BackupExceptionHandlerService backupExceptionHandlerService,
			IAnsiConsole console,
			CancellationTokenSource cts
		) {
			this.serviceProvider = serviceProvider;
			this.Console = console;
			this.tokenGenerator = new CancellationTokenGenerator(cts);
			this.backupExceptionHandlerService = backupExceptionHandlerService;
		}

		public void StartBackup(ConfigurationHandler configHandler) {
			using(tokenGenerator) {
				CancellationToken cancellationToken = tokenGenerator.NewToken();

				StorageHandler deviceHandler = serviceProvider.GetRequiredService<StorageHandler>();
				DriveHandler driveHandler = serviceProvider.GetRequiredService<DriveHandler>();
				BackupHelper backupHelper = serviceProvider.GetRequiredService<BackupHelper>();
				FileComparer fileComparer = serviceProvider.GetRequiredService<FileComparer>();
				BackupHandler backupHandler = serviceProvider.GetRequiredService<BackupHandler>();
				PrintHandler printHandler = serviceProvider.GetRequiredService<PrintHandler>();

				IList<ISourceConfig> configs = configHandler.GetConfigs();
				foreach(SourceType sourceType in Enum.GetValues(typeof(SourceType))) {
					printHandler.PrintDisabledSources(configs, sourceType);
					printHandler.PrintEnabledSources(configs, sourceType);
				}

				IList<DeviceSourceConfig> disabledDeviceSourceConfigs = configHandler.GetDisabledDeviceConfigs();
				IList<DeviceSourceConfig> enabledDeviceSourceConfigs = configHandler.GetEnabledDeviceConfigs();

				IList<DriveSourceConfig> disabledDriveSourceConfigs = configHandler.GetDisabledDriveConfigs();
				IList<DriveSourceConfig> enabledDriveSourceConfigs = configHandler.GetEnabledDriveConfigs();

				//				printHandler.PrintDisabledDeviceSources(disabledDeviceSourceConfigs);
				//				printHandler.PrintEnabledDeviceSources(enabledDeviceSourceConfigs);

				//				printHandler.PrintDisabledDriveSources(disabledDriveSourceConfigs);
				//				printHandler.PrintEnabledDriveSources(enabledDriveSourceConfigs);

				IEnumerable<MediaDevice> mediaDevices = deviceHandler.GetMediaDevices();
				printHandler.PrintDeviceDetails(mediaDevices);

				IList<ConfigDevicePair> foundDevicesAndConfig = deviceHandler.GetConfiguredDevices(mediaDevices, enabledDeviceSourceConfigs);
				printHandler.PrintFoundDevicesAndConfig(foundDevicesAndConfig);

				IEnumerable<DriveInfo> foundDriveInfos = driveHandler.GetDriveInfos();
				printHandler.PrintDriveDetails(foundDriveInfos);

				IList<ConfigDrivePair> foundDrivesAndConfig = driveHandler.GetConfiguredDrives(foundDriveInfos, enabledDriveSourceConfigs);
				printHandler.PrintFoundDrivesAndConfig(foundDrivesAndConfig);

				BackupDevices(deviceHandler, backupHandler, printHandler, cancellationToken, foundDevicesAndConfig);
				BackupDrives(driveHandler, backupHandler, printHandler, cancellationToken, foundDrivesAndConfig);
			}
		}
		private void BackupDevices(StorageHandler deviceHandler, BackupHandler backupHandler, PrintHandler printHandler, CancellationToken cancellationToken, IList<ConfigDevicePair> foundDevicesAndConfig) {
			foreach(var devicePair in foundDevicesAndConfig) {
				try {
					DateTime backupStartDateTime = DateTime.Now;
					backupHandler.PerformBackup(devicePair.MediaDevice, devicePair.DeviceSourceConfig, backupStartDateTime);
					// TODO: Implement using IBackupHandler
					//IBackupHandler backupHandler = new BackupHandlerForDevice(devicePair.MediaDevice, devicePair.DeviceSourceConfig, backupStartDateTime);

					//backupHandler.BackupDevices(foundDevicesAndConfig);
					//backupHandler.BackupDevicesAsync(foundDevicesAndConfig).GetAwaiter().GetResult();
					if(cancellationToken.IsCancellationRequested) {
						logger.ZLogTrace($"Cancelled while backing up MediaDevice {devicePair.MediaDevice.FriendlyName}");
						Console.WriteLine("Backup afbrudt.");
						break;
					}
				} catch(BackupCanceledException e) {
					Console.WriteLine($"Backup blev annulleret under operationen: {e.Operation}");
				} catch(OperationCanceledException e) {
					Console.WriteLine($"Backup blev annulleret under operationen: {e.Message}");
				} catch(COMException e) {
					backupExceptionHandlerService.HandleCOMException(e, backupHandler.DetermineDeviceRootName(devicePair.MediaDevice));
					throw new ComBackupException(backupHandler.DetermineDeviceRootName(devicePair.MediaDevice), e);
				}
			}
		}
		private void BackupDrives(DriveHandler driveHandler, BackupHandler backupHandler, PrintHandler printHandler, CancellationToken cancellationToken, IList<ConfigDrivePair> foundDrivesAndConfig) {
			foreach(var drivePair in foundDrivesAndConfig) {
				try {
					DateTime backupStartDateTime = DateTime.Now;
					backupHandler.BackupDrive(drivePair.DriveInfo, drivePair.DriveSourceConfig, backupStartDateTime);

					if(cancellationToken.IsCancellationRequested) {
						logger.ZLogTrace($"Cancelled while backing up DriveInfo {drivePair.DriveInfo.Name}");
						Console.WriteLine("Backup afbrudt.");
						break;
					}
				} catch(BackupCanceledException e) {
					Console.WriteLine($"Backup blev annulleret under operationen: {e.Operation}");
				} catch(OperationCanceledException e) {
					Console.WriteLine($"Backup blev annulleret under operationen: {e.Message}");
				}
			}
		}
	}
}
