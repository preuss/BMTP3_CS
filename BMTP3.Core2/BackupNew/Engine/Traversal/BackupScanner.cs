using System.Runtime.CompilerServices;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Infrastructure.Traversal;
using BMTP3.Common.Utilities;
using MediaDevices;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Engine.Traversal;

/// <summary>
///     Standard implementation of <see cref="IBackupScanner" />.
///     Supports both FileSystem and MTP device sources.
///     ## MTP device lifecycle
///     For MTP sources, device lifecycle (Connect/Disconnect) is controlled by
///     <see cref="BackupEngine" /> via the <see cref="IMtpCapableScanner" /> interface:
///     1. <see cref="BackupEngine" /> calls <see cref="OpenSession" /> to connect the device
///     and obtain an <see cref="IMtpDeviceSession" />.
///     2. <see cref="ScanAsync" /> uses that already-connected device instance (set by
///     <see cref="OpenSession" />). A fresh <see cref="ITraversalScanner{MediaFileInfo}" />
///     is created via <see cref="IMediaDeviceScannerFactory" /> bound to that device, so
///     the scanner and BackupScanner always share the exact same object.
///     3. <see cref="BackupEngine" /> disposes the session only after
///     <c>ContentBufferingPipelineStage</c> completes — guaranteeing the device stays
///     connected for the full duration that <see cref="MediaFileContent.OpenRead" /> may
///     be called.
/// </summary>
public class BackupScanner : IBackupScanner, IMtpCapableScanner
{
	private readonly ITraversalScanner<FileInfo> _fileSystemScanner;
	private readonly IMtpGatekeeper _gatekeeper;
	private readonly ILogger<BackupScanner>? _logger;
	private readonly IMediaDeviceScannerFactory _mediaDeviceScannerFactory;

	// Set by OpenSession() before ScanAsync is called for MTP plans.
	// Thread-safety: set once before the scan starts, read during scan.
	private MediaDevice? _activeDevice;

	public BackupScanner(
		ITraversalScanner<FileInfo> fileSystemScanner,
		IMediaDeviceScannerFactory mediaDeviceScannerFactory,
		IMtpGatekeeper gatekeeper,
		ILogger<BackupScanner>? logger = null)
	{
		_fileSystemScanner = fileSystemScanner ?? throw new ArgumentNullException(nameof(fileSystemScanner));
		_mediaDeviceScannerFactory = mediaDeviceScannerFactory ??
		                             throw new ArgumentNullException(nameof(mediaDeviceScannerFactory));
		_gatekeeper = gatekeeper ?? throw new ArgumentNullException(nameof(gatekeeper));
		_logger = logger;
	}

	/// <inheritdoc />
	/// <remarks>
	///     For MTP plans, <see cref="OpenSession" /> must be called before <see cref="ScanAsync" />
	///     so that the device is already connected when scanning begins.
	/// </remarks>
	public async IAsyncEnumerable<IBackupItem> ScanAsync(BackupPlan plan, [EnumeratorCancellation] CancellationToken ct)
	{
		if (plan.SourceType == SourceType.FileSystem)
		{
			await foreach (IBackupItem item in ScanFileSystemAsync(plan, ct))
			{
				yield return item;
			}
		}
		else if (plan.SourceType == SourceType.MediaDevice)
		{
			await foreach (IBackupItem item in ScanMediaDeviceAsync(plan, ct))
			{
				yield return item;
			}
		}
		else
		{
			throw new NotSupportedException($"SourceType '{plan.SourceType}' is not supported.");
		}
	}

	/// <inheritdoc />
	public IMtpDeviceSession OpenSession(BackupPlan plan)
	{
		ArgumentNullException.ThrowIfNull(plan);

		IEnumerable<MediaDevice> devices = MediaDevice.GetDevices();
		MediaDevice? device = devices.FirstOrDefault(d =>
			d.FriendlyName.Equals(plan.SourceId, StringComparison.OrdinalIgnoreCase));

		if (device == null)
		{
			throw new DirectoryNotFoundException(
				$"Media device '{plan.SourceId}' not found. " +
				$"Available: {string.Join(", ", devices.Select(d => d.FriendlyName))}");
		}

		device.Connect();
		_logger?.LogInformation("Connected to MTP device '{DeviceName}' (Id={DeviceId}).", device.FriendlyName,
			device.DeviceId);

		// Store the connected device so ScanAsync can use it.
		_activeDevice = device;

		return new MtpDeviceSession(device, _logger);
	}

	private async IAsyncEnumerable<IBackupItem> ScanFileSystemAsync(BackupPlan plan,
		[EnumeratorCancellation] CancellationToken ct)
	{
		string rootPath = plan.SourcePath;
		if (!Path.IsPathRooted(rootPath) && !string.IsNullOrEmpty(plan.SourceId))
		{
			rootPath = Path.Combine(plan.SourceId, rootPath);
		}

		await foreach (FileInfo fileInfo in _fileSystemScanner.ScanAsync(rootPath, plan.Recursive, null, ct))
		{
			if (!IsIncluded(fileInfo.FullName, plan))
			{
				continue;
			}

			string relativePath = Path.GetRelativePath(rootPath, fileInfo.DirectoryName ?? rootPath);
			if (relativePath == ".")
			{
				relativePath = "";
			}

			FileContent content = new(fileInfo);
			BackupItem item = BackupItem.Create(content, fileInfo.Name, relativePath);

			item.Metadata.Set(MetadataKey.SourceId, fileInfo.FullName);
			item.Metadata.Set(MetadataKey.SourceFullPath, fileInfo.FullName);
			item.Metadata.Set(MetadataKey.SourceRelativePath, relativePath);
			item.Metadata.Set(MetadataKey.SourceFileName, fileInfo.Name);
			item.Metadata.Set(MetadataKey.Length, (ulong)fileInfo.Length);

			item.Metadata.Set(MetadataKey.DeviceName, Environment.MachineName);
			item.Metadata.Set(MetadataKey.DeviceFileUrl, new Uri(fileInfo.FullName).AbsoluteUri);
			item.Metadata.Set(MetadataKey.DeviceUniqueId, Environment.MachineName);

			item.Metadata.CreatedDateTime = fileInfo.CreationTime;
			item.Metadata.ModifiedDateTime = fileInfo.LastWriteTime;
			item.Metadata.AccessedDateTime = fileInfo.LastAccessTime;

			yield return item;
		}
	}

	private async IAsyncEnumerable<IBackupItem> ScanMediaDeviceAsync(BackupPlan plan,
		[EnumeratorCancellation] CancellationToken ct)
	{
		MediaDevice device = _activeDevice
		                     ?? throw new InvalidOperationException(
			                     "OpenSession() must be called before ScanAsync() for MTP sources. " +
			                     "BackupEngine is responsible for calling OpenSession and managing the device lifetime.");

		// Create a scanner bound to THIS connected device instance.
		ITraversalScanner<MediaFileInfo> scanner = _mediaDeviceScannerFactory.Create(device);

		string rootPath = plan.SourcePath;

		await foreach (MediaFileInfo mediaFileInfo in scanner.ScanAsync(rootPath, plan.Recursive, null, ct))
		{
			if (!IsIncluded(mediaFileInfo.FullName, plan))
			{
				continue;
			}

			string dirName = Path.GetDirectoryName(mediaFileInfo.FullName) ?? rootPath;
			string relativePath = GetRelativePath(rootPath, dirName);

			// Device stays connected until BackupEngine disposes the IMtpDeviceSession,
			// which happens only after ContentBufferingPipelineStage has finished.
			MediaFileContent content = new(mediaFileInfo, _gatekeeper);
			BackupItem item = BackupItem.Create(content, mediaFileInfo.Name, relativePath);

			item.Metadata.Set(MetadataKey.SourceId, mediaFileInfo.PersistentUniqueId);
			item.Metadata.Set(MetadataKey.SourceFullPath, mediaFileInfo.FullName);
			item.Metadata.Set(MetadataKey.SourceRelativePath, relativePath);
			item.Metadata.Set(MetadataKey.SourceFileName, mediaFileInfo.Name);
			item.Metadata.Set(MetadataKey.Length, content.Length);

			item.Metadata.Set(MetadataKey.DeviceName, device.FriendlyName);
			string mtpPath = mediaFileInfo.FullName.TrimStart('\\', '/').Replace('\\', '/');
			IEnumerable<string> segments = mtpPath.Split('/', StringSplitOptions.RemoveEmptyEntries)
				.Select(s => Uri.EscapeDataString(s));
			string mtpUrl = $"mtp://{Uri.EscapeDataString(device.DeviceId)}/{string.Join('/', segments)}";
			item.Metadata.Set(MetadataKey.DeviceFileUrl, mtpUrl);
			item.Metadata.Set(MetadataKey.DeviceUniqueId, device.DeviceId);

			if (mediaFileInfo.DateAuthored.HasValue)
			{
				item.Metadata.Set(MetadataKey.RawMtpAuthoredDate, mediaFileInfo.DateAuthored.Value);
			}

			item.Metadata.AuthoredDateTime = mediaFileInfo.DateAuthored;
			item.Metadata.CreatedDateTime = mediaFileInfo.CreationTime;
			item.Metadata.ModifiedDateTime = mediaFileInfo.LastWriteTime;

			yield return item;
		}
	}

	private static bool IsIncluded(string fullPath, BackupPlan plan)
	{
		return GlobMatcher.IsIncluded(fullPath, plan.IncludePatterns, plan.ExcludePatterns);
	}

	private string GetRelativePath(string root, string? fullDirectory)
	{
		if (string.IsNullOrEmpty(fullDirectory))
		{
			return "";
		}

		if (!fullDirectory.StartsWith(root, StringComparison.OrdinalIgnoreCase))
		{
			return fullDirectory;
		}

		return fullDirectory.Substring(root.Length).TrimStart('\\', '/');
	}
}