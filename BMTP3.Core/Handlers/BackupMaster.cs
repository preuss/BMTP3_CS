using BMTP3.Core.BackupSource;
using BMTP3.Core.BackupSource.Drives;
using BMTP3.Core.BackupSource.PortableDevices;
using BMTP3.Core.CompareFiles;
using BMTP3.Core.Configs;
using BMTP3.Core.Configuration;
using BMTP3.Core.Exceptions;
using BMTP3.Core.Services;
using MediaDevices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ZLogger;

namespace BMTP3.Core.Handlers {
	/// <summary>
	/// BackupMaster is the main class of BMTP3.
	/// //TODO: I am thinking of refactor rename this to BackupController
	/// </summary>
	[SupportedOSPlatform("windows10.0")]
	internal class BackupMaster {
		private static readonly ILogger<BackupMaster> logger = LogManager.GetLogger<BackupMaster>();

		private readonly BackupExceptionHandlerService backupExceptionHandlerService;
		private readonly IServiceProvider serviceProvider;
		private readonly IAnsiConsole Console; // Use Spectre.Console instead of System.Console, but keep the same caseing.
		private readonly CancellationTokenGenerator tokenGenerator;

		private readonly IStorageHandler storageHandler;
		private readonly IDriveHandler driveHandler;
		private readonly IBackupHandler backupHandler;
		private readonly IPrintHandler printHandler;

		public BackupMaster(
			IServiceProvider serviceProvider,
			BackupExceptionHandlerService backupExceptionHandlerService,
			IAnsiConsole console,
			CancellationTokenSource cts,
			IStorageHandler storageHandler,
			IDriveHandler driveHandler,
			IBackupHandler backupHandler,
			IPrintHandler printHandler
		) {
			this.serviceProvider = serviceProvider;
			this.Console = console;
			this.tokenGenerator = new CancellationTokenGenerator(cts);
			this.backupExceptionHandlerService = backupExceptionHandlerService;

			this.storageHandler = storageHandler;
			this.driveHandler = driveHandler;
			this.backupHandler = backupHandler;
			this.printHandler = printHandler;
		}

		public void StartBackup(ConfigurationHandler configHandler) {
			using(tokenGenerator) {
				CancellationToken cancellationToken = tokenGenerator.NewToken();

				// Print configuration overview for each source type (enabled / disabled)
				IList<ISourceConfig> configs = configHandler.GetConfigs();
				foreach(SourceType sourceType in Enum.GetValues(typeof(SourceType))) {
					printHandler.PrintDisabledSources(configs, sourceType);
					printHandler.PrintEnabledSources(configs, sourceType);
				}

				IList<DeviceSourceConfig> disabledDeviceSourceConfigs = configHandler.GetDisabledDeviceConfigs();
				IList<DeviceSourceConfig> enabledDeviceSourceConfigs = configHandler.GetEnabledDeviceConfigs();

				IList<DriveSourceConfig> disabledDriveSourceConfigs = configHandler.GetDisabledDriveConfigs();
				IList<DriveSourceConfig> enabledDriveSourceConfigs = configHandler.GetEnabledDriveConfigs();

				// Keeping your commented debug print hooks (uncomment if needed):
				//				printHandler.PrintDisabledDeviceSources(disabledDeviceSourceConfigs);
				//				printHandler.PrintEnabledDeviceSources(enabledDeviceSourceConfigs);

				//				printHandler.PrintDisabledDriveSources(disabledDriveSourceConfigs);
				//				printHandler.PrintEnabledDriveSources(enabledDriveSourceConfigs);

				// Discover portable devices
				IEnumerable<MediaDevice> mediaDevices = storageHandler.GetMediaDevices();
				printHandler.PrintDeviceDetails(mediaDevices);

				IList<DeviceBackupJob> foundDeviceJobs = storageHandler.GetConfiguredDevices(mediaDevices, enabledDeviceSourceConfigs);
				printHandler.PrintFoundDevicesAndConfig(foundDeviceJobs);

				// Discover drives
				IEnumerable<DriveInfo> foundDriveInfos = driveHandler.GetDriveInfos();
				printHandler.PrintDriveDetails(foundDriveInfos);

				IList<DriveBackupJob> foundDriveJobs = driveHandler.GetConfiguredDrives(foundDriveInfos, enabledDriveSourceConfigs);
				printHandler.PrintFoundDrivesAndConfig(foundDriveJobs);

				// Execute backup phases
				BackupDevices(storageHandler, backupHandler, printHandler, cancellationToken, foundDeviceJobs);
				BackupDrives(driveHandler, backupHandler, printHandler, cancellationToken, foundDriveJobs);
			}
		}
		/// <summary>
		/// Executes backup for each configured device (fail-fast).
		/// </summary>
		/// <param name="deviceHandler">Storage handler (not used directly here, retained for signature stability).</param>
		/// <param name="backupHandler">Backup handler performing the actual device backup.</param>
		/// <param name="printHandler">Print handler (not used here).</param>
		/// <param name="cancellationToken">Cancellation token. If signaled before or during iteration the method throws immediately.</param>
		/// <param name="deviceJobs">List of device backup jobs to process.</param>
		/// <exception cref="OperationCanceledException">
		/// Thrown if <paramref name="cancellationToken"/> is signaled before starting or between jobs
		/// (via <c>ThrowIfCancellationRequested()</c>).
		/// </exception>
		/// <exception cref="ComBackupException">
		/// Thrown when a <see cref="COMException"/> occurs during device backup and is wrapped to provide device context.
		/// </exception>
		/// <exception cref="BackupCanceledException">
		/// Propagated if the underlying backup operation signals an internal backup cancellation.
		/// </exception>
		/// <exception cref="Exception">
		/// Any other unexpected exception occurring inside the backup pipeline (fail-fast).
		/// </exception>
		private void BackupDevices(IStorageHandler deviceHandler, IBackupHandler backupHandler, IPrintHandler printHandler, CancellationToken cancellationToken, IList<DeviceBackupJob> deviceJobs) {
			if(deviceJobs.Count == 0) {
				logger.ZLogDebug($"No portable devices found with enabled configuration.");
				return;
			}

			cancellationToken.ThrowIfCancellationRequested();

			foreach(DeviceBackupJob deviceJob in deviceJobs) {
				string friendlyName = deviceJob.MediaDevice.FriendlyName;
				if(cancellationToken.IsCancellationRequested) {
					logger.ZLogTrace($"Cancellation requested – aborting device backup loop before job {friendlyName}.");
					Console.WriteLine($"Backup afbrudt (devices) lige før udførsel af job {friendlyName}.");
					cancellationToken.ThrowIfCancellationRequested();
					return;
				}

				logger.ZLogTrace($"Starting device backup: {friendlyName}");

				try {
					DateTime backupStartDateTime = DateTime.Now;
					backupHandler.PerformBackup(deviceJob.MediaDevice, deviceJob.DeviceSourceConfig, backupStartDateTime);
					// TODO: Implement using IBackupHandler
					//IBackupHandler backupHandler = new BackupHandlerForDevice(devicePair.MediaDevice, devicePair.DeviceSourceConfig, backupStartDateTime);

					//backupHandler.BackupDevices(foundDevicesAndConfig);
					//backupHandler.BackupDevicesAsync(foundDevicesAndConfig).GetAwaiter().GetResult();
					if(cancellationToken.IsCancellationRequested) {
						logger.ZLogTrace($"Cancelled during device bakcup: {friendlyName}");
						Console.WriteLine("Backup afbrudt.");
						cancellationToken.ThrowIfCancellationRequested();
						return;
					}

					logger.ZLogTrace($"Finished device backup: {friendlyName}");
				} catch(BackupCanceledException e) {
					Console.WriteLine($"Backup blev annulleret under operationen: {e.Operation}");
					logger.ZLogDebug(e, $"BackupCanceledException on device: {friendlyName}");
					throw;
				} catch(OperationCanceledException e) {
					Console.WriteLine($"Backup blev annulleret under operationen: {e.Message}");
					logger.ZLogDebug(e, $"OperationCanceledException on device: {friendlyName}");
					cancellationToken.ThrowIfCancellationRequested();
					throw;
				} catch(COMException e) {
					// Device-specific COM issues (e.g., disconnected device)
					string rootName = backupHandler.DetermineDeviceRootName(deviceJob.MediaDevice);
					backupExceptionHandlerService.HandleCOMException(e, rootName);
					// Re-throw wrapped to keep original behavior
					throw new ComBackupException(rootName, e);
				} catch(Exception e) {
					// Generic fallback (optional)
					logger.ZLogError(e, $"Unexpected error during device backup: {friendlyName}");
					Console.WriteLine($"Uventet fejl ved backup af {friendlyName}: {e.Message}");
					throw;
				}
			}
		}
		/// <summary>
		/// Executes backup for each configured drive (fail-fast).
		/// </summary>
		/// <param name="driveHandler">Drive handler (not used directly here, retained for signature stability).</param>
		/// <param name="backupHandler">Backup handler performing the actual drive backup.</param>
		/// <param name="printHandler">Print handler (not used here).</param>
		/// <param name="cancellationToken">Cancellation token. If signaled before or between jobs the method throws.</param>
		/// <param name="driveJobs">List of drive backup jobs to process.</param>
		/// <exception cref="OperationCanceledException">
		/// Thrown if <paramref name="cancellationToken"/> is signaled before starting or between jobs.
		/// </exception>
		/// <exception cref="BackupCanceledException">
		/// Propagated if the underlying drive backup logic signals an internal cancellation.
		/// </exception>
		/// <exception cref="Exception">
		/// Any unexpected exception occurring during a drive backup (fail-fast). 
		/// (Drive backup does not wrap COM exceptions distinctly here.)
		/// </exception>
		private void BackupDrives(IDriveHandler driveHandler, IBackupHandler backupHandler, IPrintHandler printHandler, CancellationToken cancellationToken, IList<DriveBackupJob> driveJobs) {
			if(driveJobs.Count == 0) {
				logger.ZLogDebug($"No portable drives found with enabled configuration.");
				return;
			}
			cancellationToken.ThrowIfCancellationRequested();

			foreach(DriveBackupJob driveJob in driveJobs) {
				string driveName = driveJob.DriveInfo.Name; 
				if(cancellationToken.IsCancellationRequested) {
					logger.ZLogTrace($"Cancellation requested – aborting drive backup loop before job {driveName}.");
					Console.WriteLine($"Backup afbrudt (drev) lige før udførsel af job {driveName}.");
					cancellationToken.ThrowIfCancellationRequested();
				}

				try {
					DateTime backupStartDateTime = DateTime.Now;
					logger.ZLogTrace($"Starting drive backup: {driveName}");
					backupHandler.BackupDrive(driveJob.DriveInfo, driveJob.DriveSourceConfig, backupStartDateTime);

					logger.ZLogTrace($"Finished drive backup: {driveName}");
				} catch(BackupCanceledException e) {
					Console.WriteLine($"Backup blev annulleret under operationen: {e.Operation}");
					logger.ZLogDebug(e, $"BackupCanceledException on drive: {driveName}");
					throw;
				} catch(OperationCanceledException e) {
					Console.WriteLine($"Backup blev annulleret under operationen: {e.Message}");
					logger.ZLogDebug(e, $"OperationCanceledException on drive: {driveName}");
					cancellationToken.ThrowIfCancellationRequested();
					throw;
				} catch(Exception e) {
					// No drive-specific COM handling was present originally – log generic error.
					logger.ZLogError(e, $"Unexpected error during drive backup: {driveName}");
					Console.WriteLine($"Uventet fejl ved backup af {driveName}: {e.Message}");
					throw;
				}
			}
		}
	}
}
