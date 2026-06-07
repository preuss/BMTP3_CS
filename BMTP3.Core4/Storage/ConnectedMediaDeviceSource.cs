using BMTP3.Core4.Devices;

namespace BMTP3.Core4.Storage;

internal sealed class ConnectedMediaDeviceSource : IConnectedMediaDeviceSource
{
	private readonly IBackupMediaDriveInfo _mediaDriveInfo;
	private readonly IMediaDevice _device;

	private bool _disposed;

	public ConnectedMediaDeviceSource(IBackupMediaDriveInfo mediaDriveInfo, IMediaDevice device)
	{
		_mediaDriveInfo = mediaDriveInfo ?? throw new ArgumentNullException(nameof(mediaDriveInfo));
		_device = device ?? throw new ArgumentNullException(nameof(device));
	}

	public IBackupDriveInfo DriveInfo => _mediaDriveInfo;

	public IBackupMediaDriveInfo MediaDriveInfo => _mediaDriveInfo;

	public IMediaDevice Device
	{
		get
		{
			ThrowIfDisposed();
			return _device;
		}
	}

	public void Dispose()
	{
		if(_disposed)
			return;

		_device.Disconnect();
		_device.Dispose();

		_disposed = true;
	}

	private void ThrowIfDisposed()
	{
		if(_disposed)
			throw new ObjectDisposedException(nameof(ConnectedMediaDeviceSource));
	}
}