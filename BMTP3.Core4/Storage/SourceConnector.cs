using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Devices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Storage;


[SupportedOSPlatform("windows7.0")]
internal sealed class SourceConnector : ISourceConnector
{
	public IConnectedSource Connect(IBackupDriveInfo backupDriveInfo)
	{
		ArgumentNullException.ThrowIfNull(backupDriveInfo);

		return backupDriveInfo switch
		{
			IBackupFileSystemDriveInfo fsi => ConnectFileSystemSource(fsi),
			IBackupMediaDriveInfo mdi => ConnectMediaDeviceSource(mdi),
			_ => throw new NotSupportedException($"Unsupported backup drive info type: {backupDriveInfo.GetType().FullName}")
		};
	}

	private static IConnectedSource ConnectFileSystemSource(IBackupFileSystemDriveInfo fileSystemDriveInfo)
	{
		if (fileSystemDriveInfo.SourceType != BackupSourceType.FileSystem)
		{
			throw new InvalidOperationException($"Expected source type '{BackupSourceType.FileSystem}', but got '{fileSystemDriveInfo.SourceType}'.");
		}

		DriveInfo drive = new(fileSystemDriveInfo.RootPath);

		if (!drive.IsReady)
		{
			throw new InvalidOperationException($"File system drive '{fileSystemDriveInfo.RootPath}' is not ready.");
		}

		return new ConnectedFileSystemSource(drive);
	}

	private static IConnectedSource ConnectMediaDeviceSource(IBackupMediaDriveInfo backupMediaDriveInfo)
	{
		if (backupMediaDriveInfo.SourceType != BackupSourceType.MediaDevice)
		{
			throw new MediaDeviceException($"Expected source type '{BackupSourceType.MediaDevice}', but got '{backupMediaDriveInfo.SourceType}'.");
		}

		IMediaDeviceInfo deviceInfo = MediaDeviceInfo
			.GetDevices()
			.Where(d => d.FriendlyName == backupMediaDriveInfo.FriendlyName)
			.FirstOrDefault(d => d.DeviceId == backupMediaDriveInfo.DeviceId)
			?? throw new MediaDeviceException(
				$"Media device with DeviceId '{backupMediaDriveInfo.DeviceId}' not found and FriendlyName '{backupMediaDriveInfo.FriendlyName}' does not match."
			);

		IMediaDevice mediaDevice = deviceInfo.Connect();
		IMediaDrive? foundMediaDrive = null;

		foreach (IMediaDrive drive in mediaDevice.Drives)
		{
			if (BuildDriveName(drive.Name) == BuildDriveName(backupMediaDriveInfo.DriveName))
			{
				foundMediaDrive = drive;
				break;
			}
		}
		if (foundMediaDrive == null)
		{
			throw new MediaDeviceException($"Media drive with Name '{backupMediaDriveInfo.DriveName}' not found.");
		}

		return new ConnectedMediaDriveSource(mediaDevice, foundMediaDrive);
	}

	private static string BuildDriveName(string driveName)
	{
		string name = driveName.TrimStart('\\');
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new InvalidOperationException("Media drive Name is missing. Cannot create a stable MTP drive identity.");
		}

		return name;
	}
}
