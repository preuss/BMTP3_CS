using MediaDevices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Devices;

[SupportedOSPlatform("windows7.0")]
internal class MediaDrive : IMediaDrive
{
	private readonly MediaDriveInfo _driveInfo;

	private readonly long _availableFreeSpace;
	private readonly string _driveFormat;
	private readonly string _driveType;
	private readonly bool _isReady;
	private readonly string _name;
	private readonly long _totalFreeSpace;
	private readonly long _totalSize;
	private readonly string _volumeLabel;

	// Cache RootDirectory, to not make a new instance IMediaDirectory every time
	private IMediaDirectory? _rootDirectory;
	private bool _rootDirectoryInitialized;


	public MediaDrive(MediaDriveInfo driveInfo)
	{
		_driveInfo = driveInfo;

		_availableFreeSpace = driveInfo.AvailableFreeSpace;
		_driveFormat = driveInfo.DriveFormat;
		_driveType = driveInfo.DriveType.ToString();
		_isReady = driveInfo.IsReady;
		_name = driveInfo.Name.TrimStart('\\');
		_totalFreeSpace = driveInfo.TotalFreeSpace;
		_totalSize = driveInfo.TotalSize;
		_volumeLabel = driveInfo.VolumeLabel;
	}

	public long AvailableFreeSpace => _availableFreeSpace;
	public string DriveFormat => _driveFormat;
	public string DriveType => _driveType;
	public bool IsReady => _isReady;
	public string Name => _name;
	public IMediaDirectory? RootDirectory
	{
		get
		{
			if(_rootDirectoryInitialized) return _rootDirectory;

			_rootDirectory = _driveInfo.RootDirectory != null
				? new MediaDirectory(_driveInfo.RootDirectory)
				: null;

			_rootDirectoryInitialized = true;
			return _rootDirectory;
		}
	}

	public long TotalFreeSpace => _totalFreeSpace;
	public long TotalSize => _totalSize;
	public string VolumeLabel => _volumeLabel;
}
