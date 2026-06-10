using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Devices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Storage;

[SupportedOSPlatform("windows7.0")]
internal sealed class BackupMediaDriveInfo : IBackupMediaDriveInfo
{
	private BackupMediaDriveInfo(
		string id,
		string driveName,
		string displayName,
		string rootPath,
		long totalSize,
		long availableFreeSpace,
		string deviceId,
		string description,
		string friendlyName,
		string manufacturer,
		string model,
		string serialNumber
	)
	{
		Id = id;
		DriveName = driveName;
		DisplayName = displayName;
		RootPath = rootPath;
		TotalSize = totalSize;
		AvailableFreeSpace = availableFreeSpace;

		DeviceId = deviceId;
		Description = description;
		DeviceName = friendlyName;
		FriendlyName = friendlyName;
		Manufacturer = manufacturer;
		Model = model;
		SerialNumber = serialNumber;
	}

	public static BackupMediaDriveInfo FromDeviceAndDrive(IMediaDevice device, IMediaDrive drive)
	{
		ArgumentNullException.ThrowIfNull(device);
		ArgumentNullException.ThrowIfNull(drive);

		string friendlyName = BuildDeviceName(device.FriendlyName);
		string driveName = BuildDriveName(drive.Name);

		return new BackupMediaDriveInfo(
			id: BuildId(friendlyName, driveName),

			driveName: driveName,
			displayName: BuildDisplayName(friendlyName, driveName),
			rootPath: BuildRootPath(friendlyName, driveName),
			totalSize: drive.TotalSize,
			availableFreeSpace: drive.AvailableFreeSpace,

			deviceId: device.DeviceId,
			description: device.Description,
			friendlyName: friendlyName,
			manufacturer: device.Manufacturer,
			model: device.Model,
			serialNumber: device.SerialNumber
		);
	}

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

	public string Description { get; }

	/// <summary>
	/// FriendlyName is used as DeviceName for better user experience,
	/// as it is more descriptive than Manufacturer + Model.
	/// It is also more stable than Manufacturer + Model,
	/// as it can be customized by the user and does not rely on the device reporting correct Manufacturer and Model information.
	/// </summary>
	public string FriendlyName { get; }

	public string Manufacturer { get; }

	public string Model { get; }

	public string SerialNumber { get; }

	#endregion IBackupMediaDriveInfo_Specific

	#region Static_Helpers

	private static string BuildDeviceName(string friendlyName)
	{
		if(string.IsNullOrWhiteSpace(friendlyName))
		{
			throw new InvalidOperationException("Media device FriendlyName is missing.");
		}

		return friendlyName;
	}

	private static string BuildDriveName(string driveName)
	{
		string name = driveName.TrimStart('\\');
		if(string.IsNullOrWhiteSpace(name))
		{
			throw new InvalidOperationException("Media drive Name is missing. Cannot create a stable MTP drive identity.");
		}

		return name;
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