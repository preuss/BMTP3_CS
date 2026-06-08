namespace BMTP3.Core4.Storage;

internal sealed class ConnectedFileSystemSource : IConnectedFileSystemSource
{
	private readonly DriveInfo _drive;

	private bool _disposed;

	public ConnectedFileSystemSource(DriveInfo drive)
	{
		_drive = drive ?? throw new ArgumentNullException(nameof(drive));
	}

	public DriveInfo Drive
	{
		get
		{
			ThrowIfDisposed();
			return _drive;
		}
	}

	public string Name
	{
		get
		{
			ThrowIfDisposed();
			return _drive.Name;
		}
	}

	public void Dispose()
	{
		_disposed = true;
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(ConnectedFileSystemSource));
	}
}
