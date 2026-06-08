namespace BMTP3.Core4.Storage;

internal interface ISourceConnector
{
	IConnectedSource Connect(IBackupDriveInfo backupDriveInfo);
}
