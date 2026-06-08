using BMTP3.Core4.Storage;
using MediaDevices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.DriveDiscovery;

[SupportedOSPlatform("windows7.0")]
internal sealed class MediaDeviceDriveProvider : IDriveProvider
{
	private const int HResultErrorDeviceNotConnected = unchecked((int)0x802A0001);

	public IReadOnlyList<IBackupDriveInfo> ListDrives()
	{
		List<IBackupDriveInfo> result = new();

		foreach (MediaDevice device in MediaDevice.GetDevices())
		{
			if (!TryConnect(device))
			{
				continue;
			}

			try
			{
				foreach (MediaDriveInfo drive in device.GetDrives())
				{
					result.Add(new BackupMediaDriveInfo(device, drive));
				}
			}
			finally
			{
				device.Disconnect();
			}
		}

		return result;
	}

	private static bool TryConnect(MediaDevice device)
	{
		try
		{
			device.ConnectAsReadonly();
			return true;
		}
		catch (COMException ex) when (ex.HResult == HResultErrorDeviceNotConnected)
		{
			return false;
		}
	}
}
