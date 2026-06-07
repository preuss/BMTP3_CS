using BMTP3.Core4.MediaDevices;
using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Devices;

[SupportedOSPlatform("windows7.0")]
internal class MediaDeviceWrapper : IMediaDevice
{
	private readonly MediaDevice _device;
	private readonly IMediaDeviceInfo _mediaDeviceInfo;

	public MediaDeviceWrapper(MediaDevice device, IMediaDeviceInfo mediaDeviceInfo)
	{
		_device = device;
		_mediaDeviceInfo = mediaDeviceInfo;
	}

	public string DeviceId => _mediaDeviceInfo.DeviceId;
	public string? Description => _mediaDeviceInfo.Description;
	public string FriendlyName => _device.FriendlyName;
	public string? Manufacturer => _mediaDeviceInfo.Manufacturer;




	public string FirmwareVersion => _device.FirmwareVersion;
	public string? Protocol => _device.Protocol;
	public string? Model => _device.Model;
	public string? SerialNumber => _device.SerialNumber;
	public string DeviceType => _device.DeviceType.ToString();
	public byte[]? FunctionalUniqueId => _device.FunctionalUniqueId;
	public byte[]? ModelUniqueId => _device.ModelUniqueId;

	public IMediaDevice Connect()
	{
		if(!_device.IsConnected)
			_device.ConnectAsReadonly();

		return this;
	}

	public IMediaDeviceInfo Disconnect()
	{
		_device.Disconnect();
		return _mediaDeviceInfo;
	}

	public void Dispose()
	{
		_device.Dispose();
	}
}