using BMTP3.Core4.Devices;

namespace BMTP3.Core4.Storage;

internal sealed class ConnectedMediaDriveSource : IConnectedMediaDriveSource
{
	private readonly IMediaDevice _device;
	private readonly IMediaDrive _drive;
	private readonly string _friendlyName;

	private bool _disposed;

	public ConnectedMediaDriveSource(IMediaDevice device, IMediaDrive drive)
	{
		_device = device ?? throw new ArgumentNullException(nameof(device));
		_drive = drive ?? throw new ArgumentNullException(nameof(drive));
		_friendlyName = device.FriendlyName;
	}

	public IMediaDevice Device
	{
		get
		{
			ThrowIfDisposed();
			return _device;
		}
	}

	public IMediaDrive Drive
	{
		get
		{
			ThrowIfDisposed();
			return _drive;
		}
	}

	public string Name => _friendlyName;

	public void Dispose()
	{
		if (_disposed)
			return;

		_device.Disconnect();
		_device.Dispose();

		_disposed = true;
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(ConnectedMediaDriveSource));
	}
}
