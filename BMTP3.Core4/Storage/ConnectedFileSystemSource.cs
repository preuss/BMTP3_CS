using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core4.Storage;

internal sealed class ConnectedFileSystemSource : IConnectedFileSystemSource
{
	private readonly IBackupFileSystemDriveInfo _fileSystemDriveInfo;
	private readonly DriveInfo _drive;

	private bool _disposed;

	public ConnectedFileSystemSource(IBackupFileSystemDriveInfo fileSystemDriveInfo, DriveInfo drive)
	{
		_fileSystemDriveInfo = fileSystemDriveInfo ?? throw new ArgumentNullException(nameof(fileSystemDriveInfo));
		_drive = drive ?? throw new ArgumentNullException(nameof(drive));
	}

	public IBackupDriveInfo DriveInfo => _fileSystemDriveInfo;

	public IBackupFileSystemDriveInfo FileSystemDriveInfo => _fileSystemDriveInfo;

	public DriveInfo Drive
	{
		get
		{
			ThrowIfDisposed();
			return _drive;
		}
	}

	public void Dispose()
	{
		_disposed = true;
	}

	private void ThrowIfDisposed()
	{
		if(_disposed)
			throw new ObjectDisposedException(nameof(ConnectedFileSystemSource));
	}
}