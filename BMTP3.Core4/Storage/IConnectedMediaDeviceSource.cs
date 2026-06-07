using BMTP3.Core4.Devices;

namespace BMTP3.Core4.Storage;
internal interface IConnectedMediaDeviceSource : IConnectedSource
{
	IBackupMediaDriveInfo MediaDriveInfo { get; }

	IMediaDevice Device { get; }
}