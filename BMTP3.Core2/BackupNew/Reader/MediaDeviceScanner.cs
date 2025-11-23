using MediaDevices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Reader;

/// <summary>
/// Scans a MediaDevice and enumerates all MediaFileInfo entries.
/// Prints progress to console every 100 files.
/// </summary>
public class MediaDeviceScanner : IFileSourceScanner<MediaFileInfo>
{
	public readonly MediaDevice _device;

	public MediaDeviceScanner()
	{
		var mediaDevice = MediaDevice.GetDevices().FirstOrDefault()
			?? throw new InvalidOperationException("No media devices found.");
		mediaDevice.ConnectAsReadonly();
		_device = mediaDevice;
	}

	public MediaDeviceScanner(MediaDevice device)
	{
		_device = device ?? throw new ArgumentNullException(nameof(device));
	}

	public IEnumerable<MediaFileInfo> ScanAll()
	{
		int count = 0;
		const int progressInterval = 100;
		const int maxFiles = 5000;

		foreach(var file in ScanRecursive(_device.GetRootDirectory()))
		{
			count++;
			if(count % progressInterval == 0)
			{
				Console.WriteLine($"Scanned {count} files so far...");
			}

			yield return file;

			if(count > maxFiles) {
				Console.WriteLine($"Stopping scan after {count} files.");
				yield break;
			}
		}

		Console.WriteLine($"Scan complete. Total files: {count}");
	}

	private IEnumerable<MediaFileInfo> ScanRecursive(MediaDirectoryInfo dir)
	{
		// yield files in current directory
		foreach(var file in dir.EnumerateFiles())
		{
			yield return file;
		}

		// recurse into subdirectories
		foreach(var subDir in dir.EnumerateDirectories())
		{
			foreach(var file in ScanRecursive(subDir))
			{
				yield return file;
			}
		}
	}
}
