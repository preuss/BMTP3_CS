using BMTP3.Core4.Devices;
using BMTP3.Core4.Storage;
using System.Runtime.Versioning;

namespace BMTP3.Core4.DriveDiscovery;

[SupportedOSPlatform("windows7.0")]
internal sealed class MediaDeviceDriveProvider : IDriveProvider
{
	public IReadOnlyList<IBackupDriveInfo> ListDrives()
	{
		List<IBackupDriveInfo> result = new();

		foreach(IMediaDeviceInfo deviceInfo in MediaDeviceInfo.GetDevices())
		{
			using(IMediaDevice device = deviceInfo.Connect())
			{
				foreach(IMediaDrive drive in device.Drives)
				{
					// Skip drives with a drive letter (e.g. "E:") — these are USB mass storage
					// devices already covered by FileSystemDriveProvider.
					// This is to avoid duplicate entries for the same physical drive.
					if(drive.Name is { Length: 2 } && drive.Name[1] == ':' && drive.Name[0] is >= 'A' and <= 'Z')
						continue;

					result.Add(BackupMediaDriveInfo.FromDeviceAndDrive(device, drive));
				}
			}
		}

		return result;
	}
}
