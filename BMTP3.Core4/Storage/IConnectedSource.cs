namespace BMTP3.Core4.Storage;
internal interface IConnectedSource : IDisposable
{
	IBackupDriveInfo DriveInfo { get; }
}