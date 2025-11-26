using MediaDevices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;

namespace BMTP3.Core2.BackupNew.Reader;

[SupportedOSPlatform("windows7.0")]
/// <summary>
/// Scans a MediaDevice and enumerates all MediaFileInfo entries.
/// </summary>
public class MediaDeviceScanner : IFileSourceScanner<MediaFileInfo>
{
	private readonly MediaDevice _device;
	private readonly bool _useBulkReportProgress;

	public MediaDeviceScanner(MediaDevice device, bool useBulkReportProgressFilesPerDirectory = false)
	{
		ArgumentNullException.ThrowIfNull(device);
		_device = device;

		_useBulkReportProgress = useBulkReportProgressFilesPerDirectory;
	}

	public MediaDeviceScanner(MediaDevice mediaDevice) : this(mediaDevice, false)
	{ }

	/// <summary>
	/// Traverses the device and returns all files as a fully materialized list.
	/// </summary>
	public IEnumerable<MediaFileInfo> TraverseFiles(bool recursive = true, Events.TraversalProgressCounter? progress = null, CancellationToken cancellationToken = default)
	{
		var root = _device.GetRootDirectory();
		var files = Traverse(root, recursive, progress, cancellationToken);
		return files;
	}

	/// <summary>
	/// Recursively collects files and returns a new list for each directory.
	/// </summary>
	private List<MediaFileInfo> Traverse(MediaDirectoryInfo dir, bool recursive, Events.TraversalProgressCounter? progress, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var result = new List<MediaFileInfo>();

		if(_useBulkReportProgress)
		{
			result = dir.EnumerateFiles().ToList();
			progress?.IncrementFileCount(result.Count);
		} else
		{
			foreach(var file in dir.EnumerateFiles())
			{
				result.Add(file);
				progress?.IncrementFileCount();
			}
		}

		if(recursive)
		{
			foreach(var subDir in dir.EnumerateDirectories())
			{
				progress?.IncrementDirectoryCount();
				var subFiles = Traverse(subDir, true, progress, cancellationToken);
				result.AddRange(subFiles);
			}
		}

		return result;
	}
}
