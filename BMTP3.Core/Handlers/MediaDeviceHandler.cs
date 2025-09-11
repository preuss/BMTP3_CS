using BMTP3.Core.BackupSource.PortableDevices;
using BMTP3.Core.Configs;
using BMTP3.Core.Configuration;
using BMTP3.Core.Services;
using MediaDevices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Handlers {
	[SupportedOSPlatform("windows10.0")]
	public class MediaDeviceHandler : IStorageHandler {
		private IMediaDeviceService _mediaDeviceService;
		private CancellationTokenGenerator _cancellationTokenGenerator;

		public MediaDeviceHandler(IMediaDeviceService mediaDeviceService, CancellationTokenGenerator cancellationTokenGenerator) {
			_mediaDeviceService = mediaDeviceService;
			_cancellationTokenGenerator = cancellationTokenGenerator;
		}
		public IEnumerable<MediaDevice> GetMediaDevices() {
			return _mediaDeviceService.GetDevices();
		}

		public IEnumerable<MediaDevice> GetPrivateDevices() {
			return _mediaDeviceService.GetPrivateDevices();
		}
		/// <summary>
		/// Matches connected MediaDevices with enabled DeviceSourceConfig entries.
		/// Matching rule: device.FriendlyName == config.Name (case-insensitive).
		/// Allows:
		///  - 1:N (same config name more than once)
		///  - N:1 (multiple devices sharing same FriendlyName—rare, but not blocked)
		/// </summary>
		public IList<DeviceBackupJob> GetConfiguredDevices(IEnumerable<MediaDevice> mediaDevices, IList<DeviceSourceConfig> enabledDeviceSourceConfigs) {
			List<DeviceBackupJob> jobs = new();
			if(mediaDevices == null) {
				return jobs;
			}
			if(enabledDeviceSourceConfigs == null || enabledDeviceSourceConfigs.Count == 0) {
				return jobs;
			}

			static string Normalize(string? s) => string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim();

			// Filter configs with a usable Name (empty names cannot match).
			List<DeviceSourceConfig> configs = enabledDeviceSourceConfigs
				.Where(c => !string.IsNullOrWhiteSpace(c.Name))
				.ToList();

			if(configs.Count == 0) {
				return jobs;
			}

			// Debug help, register duplicates
			// Optional diagnostics (set to false if you do not want console noise)
			const bool logDuplicateConfigNames = true;
			if(logDuplicateConfigNames) {
				var duplicates = configs
					.GroupBy(c => Normalize(c.Name), StringComparer.OrdinalIgnoreCase)
					.Where(g => g.Count() > 1)
					.ToList();
				if(duplicates.Count > 0) {
					Console.WriteLine("Advarsel: Duplikerede Device config 'Name' værdier fundet:");
					foreach(var g in duplicates) {
						Console.WriteLine($"  '{g.Key}' -> {g.Count()} configs");
					}
				}
			}

			// Group configs by Name (allow multiple configs with same name)
			foreach(var device in mediaDevices) {
				string friendlyName = device.FriendlyName;

				// Skip devices without a FriendlyName.
				if(string.IsNullOrWhiteSpace(device.FriendlyName)) continue;

				foreach(var config in configs) {
					if(string.Equals(friendlyName, config.Name, StringComparison.OrdinalIgnoreCase)) {
						jobs.Add(new DeviceBackupJob(device, config));
					}
				}
			}
			return jobs;
			/*
			return mediaDevices.Join(
				enabledDeviceSourceConfigs,
				mediaDevice => mediaDevice.FriendlyName,
				config => config.Name,
				(mediaDevice, config) => new DeviceBackupJob(mediaDevice, config)
			).ToList();
			*/
		}
	}
}
