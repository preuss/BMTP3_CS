using MediaDevices;
using System.Collections.Generic;

namespace BMTP3.Core2.BackupNew.Reader;

/// <summary>
/// Scans a MediaDevice and enumerates all MediaFileInfo entries.
/// </summary>
public class MediaDeviceScanner : IFileSourceScanner<MediaFileInfo>
{
	private readonly MediaDevice _device;

	public MediaDeviceScanner()
	{
		var mediaDevice = MediaDevice.GetDevices().FirstOrDefault() ?? throw new InvalidOperationException("No media devices found.");
		mediaDevice.ConnectAsReadonly();
		_device = mediaDevice;
	}

	public MediaDeviceScanner(MediaDevice device)
	{
		_device = device ?? throw new ArgumentNullException(nameof(device));
	}

	public IEnumerable<MediaFileInfo> TraverseFiles(bool recursive = true, Events.TraversalProgressCounter? progress = null)
	{
		var files = new List<MediaFileInfo>();
		Traverse(_device.GetRootDirectory(), recursive, progress, files);
		//files = _device.GetRootDirectory().EnumerateFiles(null, SearchOption.AllDirectories).ToList();
		return files;
	}

	private void Traverse(MediaDirectoryInfo dir, bool recursive, Events.TraversalProgressCounter? progress, List<MediaFileInfo> files)
	{
		const uint MaxFilesPerQuery = 32;
		const bool UseBulkEnumeration = true;
		if(UseBulkEnumeration)
		{
			var fileList = dir.EnumerateFiles(MaxFilesPerQuery).ToList();
			progress?.IncrementFileCount(fileList.Count);
			files.AddRange(fileList);
		} else
		{
			// Only works if EnumerateFiles supports yielding
			foreach(var file in dir.EnumerateFiles(MaxFilesPerQuery))
			{
				progress?.IncrementFileCount();
				files.Add(file);
			}
		}

		if (recursive)
		{
			foreach (var subDir in dir.EnumerateDirectories())
			{
				progress?.IncrementDirectoryCount();
				Traverse(subDir, true, progress, files);
			}
		}
	}
}
