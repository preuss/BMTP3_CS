namespace BMTP3.Core4.Devices;
internal interface IMediaDeviceInfo
{
	/// <summary>
	/// Unique and stable identifier for the device.
	/// Critical for identifying an active device instance.
	/// </summary>
	string DeviceId { get; }
	string FriendlyName { get; }
	string Description { get; }
	string Manufacturer { get; }

	IMediaDevice Connect();
}