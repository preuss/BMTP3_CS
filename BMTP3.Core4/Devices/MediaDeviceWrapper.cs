using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Devices;

[SupportedOSPlatform("windows7.0")]
internal class MediaDeviceWrapper : IMediaDevice
{
	private readonly MediaDevice _device;

	// Only Cached for not create multiple instances of MediaDeviceInfo for the same device.
	private readonly IMediaDeviceInfo _mediaDeviceInfo;

	internal MediaDeviceWrapper(MediaDevice device, IMediaDeviceInfo mediaDeviceInfo)
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

	public IReadOnlyList<IMediaDrive> Drives
	{
		get
		{
			List<IMediaDrive> drives = new List<IMediaDrive>();
			foreach (MediaDriveInfo mediaDriveInfo in _device.GetDrives())
			{
				drives.Add(new MediaDrive(mediaDriveInfo));
			}
			return drives;
		}
	}

	public IMediaDevice Connect()
	{
		/*
		 // TODO: I do not know, if this should be able to connect or not.
		if (!_device.IsConnected)
			_device.ConnectAsReadonly();
		*/

		// Enforce exclusive ownership of the MediaDevice connection for the lifetime of this session.
		// Even if MediaDevice permits repeated Connect calls, this abstraction treats an already
		// diconnected device as invalid input to avoid ambiguous ownership and session misuse.
		if (!_device.IsConnected)
		{
			throw new MediaDeviceException("Device is not connected, and should never be able to connect a closed Wrapper.");
		}


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