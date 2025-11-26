using MediaDevices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;

namespace BMTP3.Core2.BackupNew.Reader;

[SupportedOSPlatform("windows7.0")]
/// <summary>
/// Scans a MediaDevice and enumerates all MediaFileInfo entries.
/// </summary>
public class MediaDeviceScanner : IFileSourceScanner<MediaFileInfo>
{
	private readonly MediaDevice _device;
	private readonly bool _useBulkReportProgress;
	private readonly uint _comReadFilesCache;

	public MediaDeviceScanner(MediaDevice device, bool useBulkReportProgressFilesPerDirectory = false, uint comReadFilesCache = 32)
	{
		ArgumentNullException.ThrowIfNull(device);
		_device = device;

		_useBulkReportProgress = useBulkReportProgressFilesPerDirectory;
		_comReadFilesCache = comReadFilesCache;
	}

	public MediaDeviceScanner(MediaDevice mediaDevice) : this(mediaDevice, false, 32)
	{ }

	/// <summary>
	/// Traverses the device and returns all files as a fully materialized list.
	/// </summary>
	public IEnumerable<MediaFileInfo> TraverseFiles(bool recursive = true, Events.TraversalProgressCounter? progress = null)
	{
		var root = _device.GetRootDirectory();
		var files = Traverse(root, recursive, progress);
		return files;
	}

	/// <summary>
	/// Recursively collects files and returns a new list for each directory.
	/// </summary>
	private List<MediaFileInfo> Traverse(MediaDirectoryInfo dir, bool recursive, Events.TraversalProgressCounter? progress)
	{
		var result = new List<MediaFileInfo>();

		if (_useBulkReportProgress)
		{
			while (true)
			{
				var batch = dir.EnumerateFiles(_comReadFilesCache).ToList();
				result.AddRange(batch);
				progress?.IncrementFileCount(batch.Count);

			}
		}
		else
		{
			foreach (var file in dir.EnumerateFiles(_comReadFilesCache))
			{
				result.Add(file);
				progress?.IncrementFileCount();
			}
		}

		if (recursive)
		{
			foreach (var subDir in dir.EnumerateDirectories())
			{
				progress?.IncrementDirectoryCount();
				var subFiles = Traverse(subDir, true, progress);
				result.AddRange(subFiles);
			}
		}

		return result;
	}
}
