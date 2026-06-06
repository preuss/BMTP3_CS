using BMTP3.Core4.Api.Models.Enums;
using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Storage;

[SupportedOSPlatform("windows7.0")]
internal sealed class BackupMediaDriveInfo : IBackupMediaDriveInfo
{
	private readonly MediaDevice _device;
	private readonly MediaDriveInfo _driveInfo;

	public BackupMediaDriveInfo(MediaDevice device, MediaDriveInfo driveInfo)
	{
		// TODO: Sort all in the same order as the properties are declared, for better readability.

		_device = device ?? throw new ArgumentNullException(nameof(device));
		_driveInfo = driveInfo ?? throw new ArgumentNullException(nameof(driveInfo));

		DeviceName = BuildDeviceName(device);
		DriveName = BuildDriveName(driveInfo);
		DisplayName = BuildDisplayName(DeviceName, DriveName);
		RootPath = BuildRootPath(DeviceName, DriveName);

		DeviceId = device.DeviceId;
		Description = device.Description;
		Manufacturer = device.Manufacturer;
		Model = device.Model;
		SerialNumber = device.SerialNumber;

		TotalSize = driveInfo.TotalSize;
		AvailableFreeSpace = driveInfo.AvailableFreeSpace;
		Id = BuildId(DeviceName, DriveName);
		FriendlyName = device.FriendlyName;
	}

	internal MediaDevice Device => _device;

	internal MediaDriveInfo DriveInfo => _driveInfo;

	#region IBackupDriveInfo_Specific

	public string Id { get; }

	public string DriveName { get; }

	public string DisplayName { get; }

	public BackupSourceType SourceType => BackupSourceType.MediaDevice;

	public string RootPath { get; }

	public long TotalSize { get; }

	public long AvailableFreeSpace { get; }

	#endregion IBackupDriveInfo_Specific

	#region IBackupMediaDriveInfo_Specific

	public string DeviceId { get; }

	/// <summary>
	/// Same as FriendlyName of MediaDevice, e.g. "My Phone"
	/// </summary>
	public string DeviceName { get; }

	public string? Description { get; }

	public string FriendlyName { get;}

	public string? Manufacturer { get; }

	public string? Model { get; }

	public string? SerialNumber { get; }

	#endregion IBackupMediaDriveInfo_Specific

	#region Static_Helpers

	private static string BuildDeviceName(MediaDevice device)
	{
		if(string.IsNullOrWhiteSpace(device.FriendlyName))
		{
			throw new InvalidOperationException("Media device FriendlyName is missing.");
		}

		return device.FriendlyName.Trim();
	}

	private static string BuildDriveName(MediaDriveInfo driveInfo)
	{
		string name = driveInfo.Name.TrimStart('\\');
		return string.IsNullOrWhiteSpace(name)
			? throw new InvalidOperationException("Media drive Name is missing. Cannot create a stable MTP drive identity.")
			: name;
	}

	private static string BuildDisplayName(string deviceName, string driveName)
	{
		return $"{deviceName} ({driveName})";
	}

	private static string BuildRootPath(string friendlyName, string driveName)
	{
		return $"mtp://{friendlyName}/{driveName}";
	}
	private static string BuildId(string friendlyName, string driveName)
	{
		return $"{friendlyName}/{driveName}";
	}

	#endregion Static_Helpers
}