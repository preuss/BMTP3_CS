namespace BMTP3.Core4.Storage;
internal interface IConnectedFileSystemSource : IConnectedSource
{
	DriveInfo Drive { get; }
}