using BMTP3_CS.BackupSource;
using BMTP3_CS.BackupSource.Drives;
using BMTP3_CS.BackupSource.PortableDevices;
using BMTP3_CS.Configs;
using Fclp.Internals.Extensions;
using MediaDevices;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Handlers {
	[SupportedOSPlatform("windows7.0")]
	public class PrintHandler {
		private readonly IAnsiConsole Console;
		private CancellationToken? _cancellationToken;

		public PrintHandler(IAnsiConsole console, CancellationTokenGenerator tokenGenerator) {
			this.Console = console;
			this._cancellationToken = tokenGenerator.Token;
		}
		public void PrintDisabledSources(IList<ISourceConfig> sourceConfigs, SourceType sourceType) {
			PrintSources(sourceConfigs, sourceType, false);
		}
		public void PrintEnabledSources(IList<ISourceConfig> sourceConfigs, SourceType sourceType) {
			PrintSources(sourceConfigs, sourceType, true);
		}
		public void PrintDisabledDeviceSources(IList<DeviceSourceConfig> disabledDeviceSourceConfigs) {
			PrintSources(disabledDeviceSourceConfigs.Cast<ISourceConfig>().ToList(), SourceType.Device, false);
		}
		public void PrintEnabledDeviceSources(IList<DeviceSourceConfig> enabledDeviceSourceConfigs) {
			PrintSources(enabledDeviceSourceConfigs.Cast<ISourceConfig>().ToList(), SourceType.Device, true);
		}
		public void PrintDisabledDriveSources(IList<DriveSourceConfig> disabledDriveSourceConfigs) {
			PrintSources(disabledDriveSourceConfigs.Cast<ISourceConfig>().ToList(), SourceType.Drive, false);
		}
		public void PrintEnabledDriveSources(IList<DriveSourceConfig> enabledDriveSourceConfigs) {
			PrintSources(enabledDriveSourceConfigs.Cast<ISourceConfig>().ToList(), SourceType.Drive, true);
		}
		public void PrintSources(IList<ISourceConfig> sourceConfigs, SourceType sourceType, bool isEnabled) {
			IList<ISourceConfig> filteredSourceConfigs = sourceConfigs
				.Where(sourceConfig => sourceConfig.Enabled == isEnabled)
				.Where(sourceConfig => sourceConfig.SourceType == sourceType)
				.ToList();
			string status = isEnabled ? "[green]ENABLED[/]" : "[red]DISABLED[/]";
			string sourceTypeName;
			switch(sourceType) {
				case SourceType.Device:
					sourceTypeName = "DeviceSource";
					break;
				case SourceType.Drive:
					sourceTypeName = "DriveSource";
					break;
				default:
					throw new Exception("Unknown SourceType: " + sourceType);
			}
			string markup = $"Alle Config [blue bold]{sourceTypeName}[/] som er {status}: ";
			PrintSourceConfigs(markup, filteredSourceConfigs);
		}
		private void PrintSourceConfigs(string markup, IList<ISourceConfig> sourceConfigs) {
			Console.MarkupLine(markup);
			PrintConfigs(sourceConfigs);
			Console.WriteLine();
		}
		private void PrintConfigs(IList<ISourceConfig> sourceConfigs) {
			int maxTitleSize = sourceConfigs
				.Select(config => config.Title?.Length ?? 0)
				.DefaultIfEmpty(0)
				.Max();
			sourceConfigs
				.ForEach(config =>
					Console.WriteLine($"{config.GetSourceConfigType()} Title: {(config.Title ?? string.Empty).PadRight(maxTitleSize)}, Name: {config.Name}"
			));
			if(sourceConfigs.Count == 0) {
				Console.MarkupLine("[green]Ingen[/]");
			}
		}

		public void PrintDeviceDetails(IEnumerable<MediaDevice> mediaDevices) {
			int deviceCount = mediaDevices.Count();
			Console.MarkupLine($"Alle MTP Devices som er fundet (Count: [bold]{deviceCount}[/]):");

			// Find den maksimale længde for hver felt
			int maxFriendlyNameLength = 0;
			int maxDescriptionLength = 0;
			int maxManufacturerLength = 0;
			int maxFirmwareVersionLength = 0;

			foreach(var mediaDevice in mediaDevices) {
				// We need to connect because of FirmwareVersion needs to be Connected.
				using(MediaDevice md = mediaDevice) {
					const bool enableCache = false;
					mediaDevice.Connect(MediaDeviceAccess.GenericRead, MediaDeviceShare.Read, enableCache);
					maxFriendlyNameLength = Math.Max(maxFriendlyNameLength, mediaDevice.FriendlyName.Length);
					maxDescriptionLength = Math.Max(maxDescriptionLength, mediaDevice.Description.Length);
					maxManufacturerLength = Math.Max(maxManufacturerLength, mediaDevice.Manufacturer.Length);
					maxFirmwareVersionLength = Math.Max(maxFirmwareVersionLength, mediaDevice.FirmwareVersion.Length);
					mediaDevice.Disconnect();
				}
			}

			// Print enhederne med padding
			foreach(var mediaDevice in mediaDevices) {
				using(mediaDevice) {
					const bool enableCache = false;
					mediaDevice.Connect(MediaDeviceAccess.GenericRead, MediaDeviceShare.Read, enableCache);
					Console.WriteLine($"Name: {mediaDevice.FriendlyName.PadRight(maxFriendlyNameLength)}, " +
									  $"Description: {mediaDevice.Description.PadRight(maxDescriptionLength)}, " +
									  $"Manufacturer: {mediaDevice.Manufacturer.PadRight(maxManufacturerLength)}, " +
									  $"FirmwareVersion: {mediaDevice.FirmwareVersion.PadRight(maxFirmwareVersionLength)}");
					mediaDevice.Disconnect();
				}
			}
			Console.WriteLine();
		}

		public void PrintDriveDetails(IEnumerable<DriveInfo> driveInfos) {
			int driveCount = driveInfos.Count();
			Console.MarkupLine($"Alle Drives som er fundet (Count: [bold]{driveCount}[/]):");

			// Find den maksimale længde for hver felt
			int maxNameLength = 0;
			int maxVolumeLabelLength = 0;
			int maxDriveFormatLength = 0;
			int maxTypeLength = 0;

			foreach(var driveInfo in driveInfos) {
				maxNameLength = Math.Max(maxNameLength, driveInfo.Name.Length);
				maxVolumeLabelLength = Math.Max(maxVolumeLabelLength, driveInfo.VolumeLabel.Length);
				maxDriveFormatLength = Math.Max(maxDriveFormatLength, driveInfo.DriveFormat.Length);
				maxTypeLength = Math.Max(maxTypeLength, driveInfo.DriveType.ToString().Length);
			}

			// Print enhederne med padding
			foreach(var driveInfo in driveInfos) {
				Console.WriteLine($"Drive Location: {driveInfo.Name.PadRight(maxNameLength)}, " +
								  $"VolumeLabel: {driveInfo.VolumeLabel.PadRight(maxVolumeLabelLength)}, " +
								  $"DriveFormat: {driveInfo.DriveFormat.PadRight(maxDriveFormatLength)}, " +
								  $"DriveType: {driveInfo.DriveType.ToString().PadRight(maxTypeLength)}");
			}
			Console.WriteLine();
		}
		public void PrintFoundDevicesAndConfig(IList<ConfigDevicePair> foundDevices) {
			Console.WriteLine($"Alle MTP Devices som er fundet Config DeviceSource til (Count = {foundDevices.Count()}):");

			// Find den maksimale længde for Title og Name
			int maxTitleLength = foundDevices
				.Select(pair => pair.DeviceSourceConfig.Title?.Length ?? 0)
				.DefaultIfEmpty(0)
				.Max();
			int maxNameLength = foundDevices
				.Select(pair => pair.DeviceSourceConfig.Name?.Length ?? 0)
				.DefaultIfEmpty(0)
				.Max();

			foreach(ConfigDevicePair pair in foundDevices) {
				Console.WriteLine($"Media Device: {pair.MediaDevice.FriendlyName}");
				Console.WriteLine($"\tConfig {pair.DeviceSourceConfig.GetSourceConfigType()} - Title: {(pair.DeviceSourceConfig.Title ?? string.Empty).PadRight(maxTitleLength)}, Name: {(pair.DeviceSourceConfig.Name ?? string.Empty).PadRight(maxNameLength)}");
			}
			Console.WriteLine();
		}
		public void PrintFoundDrivesAndConfig(IList<ConfigDrivePair> foundDevices) {
			Console.WriteLine($"Alle Drives som er fundet Config DriveSource til (Count = {foundDevices.Count()}):");

			// Find den maksimale længde for Title og Name
			int maxTitleLength = foundDevices
				.Select(pair => pair.DriveSourceConfig.Title?.Length ?? 0)
				.DefaultIfEmpty(0)
				.Max();
			int maxNameLength = foundDevices
				.Select(pair => pair.DriveSourceConfig.Name?.Length ?? 0)
				.DefaultIfEmpty(0)
				.Max();

			foreach(ConfigDrivePair pair in foundDevices) {
				Console.WriteLine($"Drive Info: {pair.DriveInfo.Name}");
				Console.WriteLine($"\tConfig {pair.DriveSourceConfig.GetSourceConfigType()}");
				Console.WriteLine($"\t\tTitle : {(pair.DriveSourceConfig.Title ?? string.Empty).PadRight(maxTitleLength)}");
				Console.WriteLine($"\t\tName  : {(pair.DriveSourceConfig.Name ?? string.Empty).PadRight(maxNameLength)}");
				Console.WriteLine($"\t\tSource: {pair.DriveSourceConfig.FolderSource}");
			}
			Console.WriteLine();
		}

	}
}
