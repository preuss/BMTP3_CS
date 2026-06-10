namespace BMTP3.Core4.Storage;

internal interface IBackupMediaDriveInfo : IBackupDriveInfo
{
	string DeviceId { get; }

	string Description { get; }

	/// <summary>
	/// FriendlyName is used as DeviceName for better user experience,
	/// as it is more descriptive than Manufacturer + Model.
	/// It is also more stable than Manufacturer + Model,
	/// as it can be customized by the user and does not rely on the device reporting correct Manufacturer and Model information.
	/// </summary>
	string FriendlyName { get; }

	string Manufacturer { get; }

	string Model { get; }

	string SerialNumber { get; }
}