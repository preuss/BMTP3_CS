using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Devices;

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

		// Enforce exclusive ownership of the MediaDevice connection for the lifetime of this session.
		// Even if MediaDevice permits repeated Connect calls, this abstraction treats an already
		// connected device as invalid input to avoid ambiguous ownership and session misuse.
		if (device.IsConnected)
		{
			throw new MediaDeviceException("Device is already connected.");
		}

		try
		{
			device.ConnectAsReadonly();
		}
		catch (Exception ex)
		{
			throw new MediaDeviceException($"Failed to connect to media device '{_deviceId}'.", ex);
		}

		return new MediaDeviceWrapper(device, this);
	}

	public static IReadOnlyList<IMediaDeviceInfo> GetDevices()
	{
		return MediaDevice
			.GetDevices()
			.Select(device => (IMediaDeviceInfo)new MediaDeviceInfo(device))
			.ToArray();
	}
}