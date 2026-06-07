using BMTP3.Core4.Api.Models.Enums;
using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Storage;

[SupportedOSPlatform("windows7.0")]
internal sealed class BackupMediaDriveInfo : IBackupMediaDriveInfo
{
	private readonly MediaDevice _device;
	private readonly MediaDriveInfo _driveInfo;

	private readonly string _id;
	private readonly string _driveName;
	private readonly string _displayName;
	private readonly string _rootPath;
	private readonly long _totalSize;
	private readonly long _availableFreeSpace;

	private readonly string _deviceId;
	//private readonly string _deviceName;
	private readonly string? _description;
	private readonly string _friendlyName;
	private readonly string? _manufacturer;
	private readonly string? _model;
	private readonly string? _serialNumber;


	public BackupMediaDriveInfo(MediaDevice device, MediaDriveInfo driveInfo)
	{
		_device = device ?? throw new ArgumentNullException(nameof(device));
		_driveInfo = driveInfo ?? throw new ArgumentNullException(nameof(driveInfo));

		string friendlyName = BuildDeviceName(device.FriendlyName);
		string driveName = BuildDriveName(driveInfo.Name);

		_id = BuildId(friendlyName, driveName);
		_driveName = driveName;
		_displayName = BuildDisplayName(friendlyName, driveName);
		_rootPath = BuildRootPath(friendlyName, driveName);
		_totalSize = driveInfo.TotalSize;
		_availableFreeSpace = driveInfo.AvailableFreeSpace;

		_deviceId = device.DeviceId;
		//_deviceName = friendlyName;
		_description = device.Description;
		_friendlyName = friendlyName;
		_manufacturer = device.Manufacturer;
		_model = device.Model;
		_serialNumber = device.SerialNumber;

	}

	internal MediaDevice Device => _device;

	internal MediaDriveInfo DriveInfo => _driveInfo;

	#region IBackupDriveInfo_Specific

	public string Id => _id;

	public string DriveName => _driveName;

	public string DisplayName => _displayName;

	public BackupSourceType SourceType => BackupSourceType.MediaDevice;

	public string RootPath => _rootPath;

	public long TotalSize => _totalSize;

	public long AvailableFreeSpace => _availableFreeSpace;

	#endregion IBackupDriveInfo_Specific

	#region IBackupMediaDriveInfo_Specific

	public string DeviceId => _deviceId;

	/// <summary>
	/// Same as FriendlyName of MediaDevice, e.g. "My Phone"
	/// </summary>
	public string DeviceName => _friendlyName;

	public string? Description => _description;

	public string FriendlyName => _friendlyName;

	public string? Manufacturer => _manufacturer;

	public string? Model => _model;

	public string? SerialNumber => _serialNumber;


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