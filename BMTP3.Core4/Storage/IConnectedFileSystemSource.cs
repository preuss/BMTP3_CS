namespace BMTP3.Core4.Storage;
internal interface IConnectedFileSystemSource : IConnectedSource
{
	IBackupFileSystemDriveInfo FileSystemDriveInfo { get; }
	DriveInfo Drive { get; }
}