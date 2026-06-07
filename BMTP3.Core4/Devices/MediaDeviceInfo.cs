using BMTP3.Core4.Devices;
using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.MediaDevices;

[SupportedOSPlatform("windows7.0")]
internal sealed class MediaDeviceInfo : IMediaDeviceInfo
{
	// Only a value normally not used in reading.
	private readonly bool _isCaseSensitive;

	private readonly string _deviceId;
	private readonly string _description;
	private readonly string _friendlyName;
	private readonly string _manufacturer;

	public MediaDeviceInfo(MediaDevice device)
	{
		_isCaseSensitive = device.IsCaseSensitive;

		_deviceId = device.DeviceId;
		_friendlyName = device.FriendlyName;
		_description = device.Description;
		_manufacturer = device.Manufacturer;
	}

	private bool IsCaseSensitive => _isCaseSensitive;

	public string DeviceId => _deviceId;
	public string FriendlyName => _friendlyName;
	public string Description => _description;
	public string Manufacturer => _manufacturer;

	public IMediaDevice Connect()
	{
		MediaDevice device = MediaDevice
			.GetDevices()
			.First(d => d.DeviceId == _deviceId);

		device.ConnectAsReadonly();

		return new MediaDeviceWrapper(device, this);
	}
}