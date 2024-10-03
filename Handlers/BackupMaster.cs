using BMTP3_CS.BackupSource;
using BMTP3_CS.BackupSource.Drives;
using BMTP3_CS.BackupSource.PortableDevices;
using BMTP3_CS.CompareFiles;
using BMTP3_CS.Configs;
using BMTP3_CS.Handlers.Backup;
using BMTP3_CS.Services;
using Fclp.Internals.Extensions;
using MediaDevices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZLogger;

namespace BMTP3_CS.Handlers {
	[SupportedOSPlatform("windows7.0")]
	internal class BackupMaster {
		private static readonly ILogger<BackupMaster> logger = LogManager.GetLogger<BackupMaster>();

		private IServiceProvider? ServiceProvider { get; }
		private IAnsiConsole Console { get; }
		private CancellationTokenSource cts { get; }
		private CancellationTokenGenerator TokenGenerator { get; }

		public BackupMaster(IAnsiConsole console, CancellationTokenSource cts) : this(null, console, cts) { }
		public BackupMaster(IServiceProvider? serviceProvider, IAnsiConsole console, CancellationTokenSource cts) {
			this.ServiceProvider = serviceProvider;
			this.Console = console;
			this.cts = cts;
			TokenGenerator = new CancellationTokenGenerator(cts);
		}
		public void StartBackup(ConfigurationHandler configHandler) {
			using(cts) {
				CancellationToken cancellationToken = cts.Token;

				StorageHandler deviceHandler = InitializeDeviceHandler();
				DriveHandler driveHandler = InitializeDriveHandler();
				BackupHelper backupHelper = InitializeBackupHelper();
				FileComparer fileComparer = InitializeFileComparer();
				BackupHandler backupHandler = InitializeBackupHandler(backupHelper, fileComparer);
				PrintHandler printHandler = InitializePrintHandler();

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
		private StorageHandler InitializeDeviceHandler() {
			IMediaDeviceService mediaDeviceService = new MediaDeviceServiceProd();
			return ServiceProvider?.GetService<StorageHandler>() ?? new StorageHandler(mediaDeviceService, TokenGenerator);
		}
		private DriveHandler InitializeDriveHandler() {
			return ServiceProvider?.GetService<DriveHandler>() ?? new DriveHandler(TokenGenerator);
		}
		private BackupHelper InitializeBackupHelper() {
			return ServiceProvider?.GetService<BackupHelper>() ?? new BackupHelper();
		}
		private FileComparer InitializeFileComparer() {
			return ServiceProvider?.GetService<FileComparer>() ?? new ReadFileInChunksAndCompareSequenceEqual(512 * 1024);
		}
		private BackupHandler InitializeBackupHandler(BackupHelper backupHelper, FileComparer fileComparer) {
			return ServiceProvider?.GetService<BackupHandler>() ?? new BackupHandler(Console, TokenGenerator, backupHelper, fileComparer);
		}
		private PrintHandler InitializePrintHandler() {
			return ServiceProvider?.GetService<PrintHandler>() ?? new PrintHandler(Console, TokenGenerator);
		}

		private void BackupDevices(StorageHandler deviceHandler, BackupHandler backupHandler, PrintHandler printHandler, CancellationToken cancellationToken, IList<ConfigDevicePair> foundDevicesAndConfig) {
			foreach(var devicePair in foundDevicesAndConfig) {
				try {
					DateTime backupStartDateTime = DateTime.Now;
					backupHandler.BackupDevice(devicePair.MediaDevice, devicePair.DeviceSourceConfig, backupStartDateTime);

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
