namespace BMTP3.Core4.Devices;
internal interface IMediaDevice : IMediaDeviceInfo, IDisposable
{
	string FirmwareVersion { get; }
	string Protocol { get; }
	string Model { get; }
	string SerialNumber { get; }
	string DeviceType { get; }
	byte[]? FunctionalUniqueId { get; }
	byte[]? ModelUniqueId { get; }

	IReadOnlyList<IMediaDrive> Drives { get; }

	IMediaDeviceInfo Disconnect();
}
