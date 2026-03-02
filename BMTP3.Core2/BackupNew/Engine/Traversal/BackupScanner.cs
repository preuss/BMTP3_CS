using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Infrastructure.Traversal;
using MediaDevices;
using System;
using System.Linq;
using System.IO;
using System.Threading;

namespace BMTP3.Core2.BackupNew.Engine.Traversal;

/// <summary>
/// Standard implementation of IBackupScanner.
/// Supports both FileSystem and MTP devices based on the BackupPlan.
/// </summary>
public class BackupScanner : IBackupScanner
{
	private readonly IMtpGatekeeper? _gatekeeper;

	public BackupScanner(IMtpGatekeeper? gatekeeper = null)
	{
		_gatekeeper = gatekeeper;
	}

	public async IAsyncEnumerable<IBackupItem> ScanAsync(BackupPlan plan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
	{
		if(plan.SourceType == SourceType.FileSystem)
		{
			await foreach(IBackupItem item in ScanFileSystemAsync(plan, ct))
			{
				yield return item;
			}
		} else if(plan.SourceType == SourceType.MediaDevice)
		{
			await foreach(IBackupItem item in ScanMediaDeviceAsync(plan, ct))
			{
				yield return item;
			}
		} else
		{
			throw new NotSupportedException($"SourceType '{plan.SourceType}' is not supported.");
		}
	}

	private async IAsyncEnumerable<IBackupItem> ScanFileSystemAsync(BackupPlan plan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
	{
		FileSystemScanner scanner = new();

		string rootPath = plan.SourcePath;
		if(!Path.IsPathRooted(rootPath) && !string.IsNullOrEmpty(plan.SourceId))
		{
			rootPath = Path.Combine(plan.SourceId, rootPath);
		}

		await foreach(FileInfo fileInfo in scanner.ScanAsync(rootPath, plan.Recursive, null, ct))
		{
			if(!IsIncluded(fileInfo.FullName, plan)) continue;

			string relativePath = Path.GetRelativePath(rootPath, fileInfo.DirectoryName ?? rootPath);
			if(relativePath == ".") relativePath = "";

			FileContent content = new(fileInfo);
			BackupItem item = BackupItem.Create(content, fileInfo.Name, relativePath);

			item.Metadata.Set(MetadataKey.SourceId, fileInfo.FullName); // Using full path as SourceId for file system
			item.Metadata.Set(MetadataKey.SourceFullPath, fileInfo.FullName);
			item.Metadata.Set(MetadataKey.SourceRelativePath, relativePath);
			item.Metadata.Set(MetadataKey.SourceFileName, fileInfo.Name);
			item.Metadata.Set(MetadataKey.Length, fileInfo.Length);

			item.Metadata.Set(MetadataKey.DeviceName, Environment.MachineName);
			// Use file:// URI for device file url "file://server/share/file.jpg" or "file:///C:/folder/file.jpg" for local to make it explicit and portable
			item.Metadata.Set(MetadataKey.DeviceFileUrl, new Uri(fileInfo.FullName).AbsoluteUri);
			item.Metadata.Set(MetadataKey.DeviceUniqueId, Environment.MachineName);

			item.Metadata.CreatedDateTime = fileInfo.CreationTime;
			item.Metadata.ModifiedDateTime = fileInfo.LastWriteTime;
			item.Metadata.AccessedDateTime = fileInfo.LastAccessTime;

			yield return item;
		}
	}

	private async IAsyncEnumerable<IBackupItem> ScanMediaDeviceAsync(BackupPlan plan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
	{
		IEnumerable<MediaDevice> devices = MediaDevice.GetDevices();
		MediaDevice? device = devices.FirstOrDefault(d => d.FriendlyName.Equals(plan.SourceId, StringComparison.OrdinalIgnoreCase));

		if(device == null)
		{
			throw new DirectoryNotFoundException($"Media device '{plan.SourceId}' not found. Available: {string.Join(", ", devices.Select(d => d.FriendlyName))}");
		}

		// Use injected gatekeeper if available; otherwise fall back to the default MtpGatekeeper implementation.
		IMtpGatekeeper gatekeeper;
		if(_gatekeeper == null)
		{
			Console.WriteLine("Warning: No IMtpGatekeeper provided. Using default MtpGatekeeper implementation. Consider injecting an IMtpGatekeeper for better control and testability.");
			gatekeeper = new MtpGatekeeper();
		} else
		{
			gatekeeper = _gatekeeper;
		}

		device.Connect();
		try
		{
			MediaDeviceScanner scanner = new(device, gatekeeper);
			string rootPath = plan.SourcePath;

			await foreach(MediaFileInfo mediaFileInfo in scanner.ScanAsync(rootPath, plan.Recursive, null, ct))
			{
				if(!IsIncluded(mediaFileInfo.FullName, plan)) continue;

				string dirName = Path.GetDirectoryName(mediaFileInfo.FullName) ?? rootPath;
				string relativePath = GetRelativePath(rootPath, dirName);

				MediaFileContent content = new(mediaFileInfo, gatekeeper);
				BackupItem item = BackupItem.Create(content, mediaFileInfo.Name, relativePath);

				item.Metadata.Set(MetadataKey.SourceId, mediaFileInfo.PersistentUniqueId);
				item.Metadata.Set(MetadataKey.SourceFullPath, mediaFileInfo.FullName);
				item.Metadata.Set(MetadataKey.SourceRelativePath, relativePath);
				item.Metadata.Set(MetadataKey.SourceFileName, mediaFileInfo.Name);
				item.Metadata.Set(MetadataKey.Length, content.Length);

				item.Metadata.Set(MetadataKey.DeviceName, device.FriendlyName);
				// Normalize media device path to a scheme-based URI (mtp://deviceId/escaped-path)
				string mtpPath = mediaFileInfo.FullName.TrimStart('\\', '/').Replace('\\', '/');
				IEnumerable<string> segments = mtpPath.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(s => Uri.EscapeDataString(s));
				string mtpUrl = $"mtp://{Uri.EscapeDataString(device.DeviceId)}/{string.Join('/', segments)}";
				item.Metadata.Set(MetadataKey.DeviceFileUrl, mtpUrl);
				item.Metadata.Set(MetadataKey.DeviceUniqueId, device.DeviceId);

				if(mediaFileInfo.DateAuthored.HasValue)
				{
					item.Metadata.Set(MetadataKey.RawMtpAuthoredDate, mediaFileInfo.DateAuthored.Value);
				}
				item.Metadata.AuthoredDateTime = mediaFileInfo.DateAuthored;
				item.Metadata.CreatedDateTime = mediaFileInfo.CreationTime;
				item.Metadata.ModifiedDateTime = mediaFileInfo.LastWriteTime;

				yield return item;
			}
		} finally
		{
			device.Disconnect();
		}
	}

	private bool IsIncluded(string fullPath, BackupPlan plan)
	{
		return true;
	}

	private string GetRelativePath(string root, string? fullDirectory)
	{
		if(string.IsNullOrEmpty(fullDirectory)) return "";
		if(!fullDirectory.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return fullDirectory;

		string rel = fullDirectory.Substring(root.Length).TrimStart('\\', '/');
		return rel;
	}

	// No local GetGatekeeperForDevice helper needed when gatekeeper is injected via DI.
}
