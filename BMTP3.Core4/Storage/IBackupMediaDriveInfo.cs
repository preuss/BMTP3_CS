using MediaDevices;

namespace BMTP3.Core4.Storage;

internal interface IBackupMediaDriveInfo : IBackupDriveInfo
{
	string DeviceId { get; }

	string? Description { get; }

	/// <summary>
	/// Same as FriendlyName of MediaDevice, e.g. "My Phone"
	/// </summary>
	string FriendlyName { get; }

	string? Manufacturer { get; }

	string? Model { get; }

	string? SerialNumber { get; }
}