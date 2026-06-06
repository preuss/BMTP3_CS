using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Traversal;

[SupportedOSPlatform("windows7.0")]
internal sealed class MtpDeviceSession : IMtpDeviceSession
{
	private readonly MediaDevice _device;
	private int _disposed;

	private readonly string _friendlyName;

	private MtpDeviceSession(MediaDevice connectedDevice)
	{
		_device = connectedDevice ?? throw new ArgumentNullException(nameof(connectedDevice));
		_friendlyName = _device.FriendlyName;
	}

	internal static MtpDeviceSession Open(MediaDevice device)
	{
		ArgumentNullException.ThrowIfNull(device);

		// Enforce exclusive ownership of the MediaDevice connection for the lifetime of this session.
		// Even if MediaDevice permits repeated Connect calls, this abstraction treats an already
		// connected device as invalid input to avoid ambiguous ownership and session misuse.
		if(device.IsConnected)
		{
			throw new InvalidOperationException("Device is already connected.");
		}

		device.ConnectAsReadonly();
		try
		{
			return new MtpDeviceSession(device);
		} catch
		{
			device.Disconnect();
			throw;
		}
	}

	public string DeviceName
	{
		get
		{
			return _friendlyName;
		}
	}

	public void Dispose()
	{
		if(Interlocked.Exchange(ref _disposed, 1) == 0)
		{
			_device.Disconnect();
		}
	}

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(_disposed != 0, this);
	}
}