using BMTP3.Core4.Devices;

namespace BMTP3.Core4.Storage;
internal interface IConnectedMediaDriveSource : IConnectedSource
{
	IMediaDevice Device { get; }
	IMediaDrive Drive { get; }
}