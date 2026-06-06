namespace BMTP3.Core4.Storage;

internal interface IBackupFileSystemDriveInfo : IBackupDriveInfo
{
	string VolumeLabel { get; }

	string DriveFormat { get; }

	DriveType DriveType { get; }
}
